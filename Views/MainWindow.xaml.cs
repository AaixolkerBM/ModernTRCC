using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using ModernTRCC.Core.Services;
using ModernTRCC.Core.Models;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace ModernTRCC.App.Views;

public partial class MainWindow : Window
{
    private readonly ScreenManager _screens;
    private readonly MediaLibraryService _mediaLib;
    private readonly SettingsService _settings;
    private readonly FFmpegService _ffmpeg;
    private readonly LogService _log;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private bool _trayEnabled = true;
    private readonly bool _startMinimized;
    private readonly HashSet<LcdScreenSession> _wiredSessions = new();
    private bool _hideToTrayScheduled;
    private bool _webViewSuspended;

    // 启动画面：首次导航完成后触发（由 App 订阅，用于关闭启动画面并淡入主窗口）
    public event Action? Ready;
    private bool _readyFired;

    // WebView2 子窗口子类化：把 WM_NCHITTEST 转发给顶层窗口，
    // 使整窗铺满的 WebView2 不再吞掉窗口边缘的缩放命中
    private IntPtr _webViewChildHwnd;
    private IntPtr _prevWebViewWndProc;
    private WndProcDelegate? _webViewWndProcKeepAlive;

    public MainWindow()
    {
        InitializeComponent();
        _screens = App.Services.GetRequiredService<ScreenManager>();
        _mediaLib = App.Services.GetRequiredService<MediaLibraryService>();
        _settings = App.Services.GetRequiredService<SettingsService>();
        _ffmpeg = App.Services.GetRequiredService<FFmpegService>();
        _log = App.Services.GetRequiredService<LogService>();

        _startMinimized = Environment.GetCommandLineArgs().Contains("--minimized");

        // 窗口/任务栏图标：从 Resources 目录加载 TRCC.ico（XAML 的相对路径会被当作资源包 URI 解析而失败）
        try
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "TRCC.ico");
            if (File.Exists(iconPath))
                Icon = new BitmapImage(new Uri(iconPath));
        }
        catch { }

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        StateChanged += MainWindow_StateChanged;
        SourceInitialized += MainWindow_SourceInitialized;
        Activated += MainWindow_Activated;
        Deactivated += (s, e) => _wasDeactivated = true;

        // 媒体库变化（启动扫描完成/增删）时刷新 UI；同时各屏幕尝试开机自动播放
        _mediaLib.MediaLibraryChanged += (s, e) =>
        {
            try
            {
                SendMediaList();
                foreach (var session in _screens.Screens)
                    session.TryStartAutoPlay(_mediaLib);
            }
            catch { }
        };

        // 日志事件
        _log.OnLog += OnLogReceived;

        // 屏幕列表变化：绑定各会话事件并推送全部屏幕状态
        _screens.ScreensChanged += (s, e) => Dispatcher.BeginInvoke(new Action(() =>
        {
            foreach (var session in _screens.Screens)
                WireSession(session);
            SendScreensList();
            foreach (var session in _screens.Screens)
            {
                SendDeviceStatus(session);
                SendPlaybackStatus(session);
            }
        }));
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeTray();
        await InitializeWebView();

        // 监听“显示主窗口”信号：第二个实例启动时通知本实例从托盘恢复窗口
        ListenForShowWindowSignal();

        // 周期性检查 WebView2 子窗口子类化是否仍有效（缩放/导航后可能被重建）
        var subclassTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        subclassTimer.Tick += (s, e2) =>
        {
            SubclassWebViewChild();
        };
        subclassTimer.Start();

        // 兜底：若 WebView 长时间未就绪，仍释放启动画面，避免一直卡住
        var readyTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
        readyTimer.Tick += (s, e2) =>
        {
            readyTimer.Stop();
            if (!_readyFired)
            {
                _readyFired = true;
                Ready?.Invoke();
            }
        };
        readyTimer.Start();
    }

    /// <summary>
    /// 后台线程监听单实例“显示主窗口”信号。
    /// 自启动（注册表 Run + 计划任务）可能重复拉起进程，第二个实例通过
    /// 命名事件请求本实例显示窗口后自行退出，避免多个实例竞争 USB 设备。
    /// </summary>
    private void ListenForShowWindowSignal()
    {
        var evt = App.ShowWindowEvent;
        if (evt == null) return;
        Task.Run(() =>
        {
            try
            {
                while (evt.WaitOne())
                {
                    Dispatcher.Invoke(ShowWindow);
                }
            }
            catch { }
        });
    }

    // ==================== 窗口外观 / 消息处理 ====================
    private const int WM_SIZE = 0x0005;
    private const int WM_SYSCOMMAND = 0x0112;
    private const int WM_NCCALCSIZE = 0x0083;
    private const int WM_NCHITTEST = 0x0084;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int SC_MAXIMIZE = 0xF030;
    private const int HTCAPTION = 0x0002;
    private const int HTCLIENT = 0x0001;
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_TRANSITIONS_FORCEDISABLED = 3;
    private const int DWMWCP_ROUND = 2;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const uint GW_CHILD = 5;
    private const int GWLP_WNDPROC = -4;
    private const int GWL_STYLE = -16;
    private const int WS_MAXIMIZEBOX = 0x00010000;
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);

        // Windows 11 原生圆角（最大化时系统自动恢复直角）
        try
        {
            int preference = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            // 确保窗口过渡动画（最大化/还原缩放动画）未被禁用
            int transitions = 0;
            DwmSetWindowAttribute(hwnd, DWMWA_TRANSITIONS_FORCEDISABLED, ref transitions, sizeof(int));
        }
        catch { }

        // 移除 WS_MAXIMIZEBOX：窗口在系统层面禁止最大化。
        // 保留 WS_THICKFRAME，边缘缩放（WM_NCHITTEST 返回 HTLEFT/HTRIGHT 等）依然生效。
        try
        {
            int style = GetWindowLongPtr(hwnd, GWL_STYLE).ToInt32();
            style &= ~WS_MAXIMIZEBOX;
            SetWindowLongPtr(hwnd, GWL_STYLE, new IntPtr(style));
        }
        catch { }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_NCCALCSIZE)
        {
            // 移除非客户区：让客户区填满整个窗口。
            // 否则 WS_THICKFRAME 会在四周留下约 6-7px 非客户区，
            // 顶部被系统画成白条，最大化时四周出现留边。
            // 返回 0 表示客户区 = 窗口矩形（自定义最大化时窗口从不进入系统最大化，
            // 因此无需像常规实现那样为最大化状态缩进边框宽度）
            handled = true;
            return IntPtr.Zero;
        }

        if (msg == WM_SIZE)
        {
            // WebView2 在窗口尺寸变化后可能重建宿主子窗口（Chrome_WidgetWin），
            // 导致边缘缩放命中测试失效、窗口无法调整大小；延迟重新子类化
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
                t.Tick += (s, e) => { t.Stop(); SubclassWebViewChild(); };
                t.Start();
            }), DispatcherPriority.Background);
        }

        if (msg == WM_NCHITTEST)
        {
            // 手动命中测试：边缘 14px 内返回对应缩放方向。
            // WebView2 内容窗口相对主窗口内缩约 6px，故需要较宽的命中区，
            // 才能让鼠标移到 WebView2 上时仍能触发边缘缩放
            long lp = lParam.ToInt64();
            int sx = unchecked((short)(lp & 0xFFFF));
            int sy = unchecked((short)((lp >> 16) & 0xFFFF));
            GetWindowRect(hwnd, out var r);
            const int b = 14;
            bool left = sx >= r.Left && sx < r.Left + b;
            bool right = sx < r.Right && sx >= r.Right - b;
            bool top = sy >= r.Top && sy < r.Top + b;
            bool bottom = sy < r.Bottom && sy >= r.Bottom - b;

            int ht = HTCLIENT;
            if (left && top) ht = HTTOPLEFT;
            else if (right && top) ht = HTTOPRIGHT;
            else if (left && bottom) ht = HTBOTTOMLEFT;
            else if (right && bottom) ht = HTBOTTOMRIGHT;
            else if (left) ht = HTLEFT;
            else if (right) ht = HTRIGHT;
            else if (top) ht = HTTOP;
            else if (bottom) ht = HTBOTTOM;

            handled = true;
            return (IntPtr)ht;
        }

        if (msg == WM_SYSCOMMAND)
        {
            // 屏蔽最大化命令（Win+↑、双击标题栏、系统菜单），窗口不允许最大化
            int cmd = wParam.ToInt32() & 0xFFF0;
            if (cmd == SC_MAXIMIZE)
            {
                handled = true;
                return IntPtr.Zero;
            }
        }
        return IntPtr.Zero;
    }

    private void DragWindow()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            ReleaseCapture();
            // lParam 携带光标屏幕坐标，让标题栏拖动从按下位置开始，避免窗口跳动
            GetCursorPos(out var pt);
            IntPtr lParam = (IntPtr)((pt.Y << 16) | (pt.X & 0xFFFF));
            SendMessage(hwnd, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, lParam);
        };
        timer.Start();
    }

    /// <summary>
    /// 子类化 WebView2 宿主子窗口：把 WM_NCHITTEST 转发给顶层窗口统一处理，
    /// 使窗口边缘缩放命中测试在 WebView2 铺满整窗时依然生效。
    /// 窗口缩放/导航后 WebView2 可能重建宿主子窗口，需重新子类化。
    /// </summary>
    private void SubclassWebViewChild()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            IntPtr target = IntPtr.Zero;
            EnumChildWindows(hwnd, (child, _) =>
            {
                var sb = new System.Text.StringBuilder(256);
                GetClassName(child, sb, sb.Capacity);
                string cls = sb.ToString();
                // Chromium/WebView2 内容宿主窗口：鼠标命中测试会落到它上面
                if (cls.StartsWith("Chrome_WidgetWin"))
                {
                    target = child;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            // 未找到 Chrome 窗口时，退回子类化直接子窗口
            if (target == IntPtr.Zero)
                target = GetWindow(hwnd, GW_CHILD);
            if (target == IntPtr.Zero) return;

            // 子窗口未变化且已子类化则跳过，避免重复子类化
            if (target == _webViewChildHwnd && _prevWebViewWndProc != IntPtr.Zero)
                return;

            _webViewChildHwnd = target;
            _webViewWndProcKeepAlive = WebViewChildWndProc;
            _prevWebViewWndProc = SetWindowLongPtr(target, GWLP_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(_webViewWndProcKeepAlive));
        }
        catch { }
    }

    private IntPtr WebViewChildWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_NCHITTEST)
        {
            // 交给顶层窗口的命中测试（边缘返回缩放码，其余返回 HTCLIENT）
            var parentHwnd = new WindowInteropHelper(this).Handle;
            return SendMessage(parentHwnd, (int)msg, wParam, lParam);
        }
        return CallWindowProc(_prevWebViewWndProc, hWnd, msg, wParam, lParam);
    }

    private async Task InitializeWebView()
    {
        // 用户数据目录固定到用户可写位置：默认目录在 exe 同级（安装到 Program Files 时
        // 非提权进程无法写入），初始化会以 0x800700AA“请求的资源在使用中”失败。
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ModernTRCC", "WebView2");
        Directory.CreateDirectory(userDataFolder);

        // 若残留浏览器进程仍短暂占用该目录，做有限重试
        CoreWebView2Environment? environment = null;
        Exception? lastError = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await WebView.EnsureCoreWebView2Async(environment);
                lastError = null;
                break;
            }
            catch (Exception ex)
            {
                lastError = ex;
                environment = null;
                await Task.Delay(1000);
            }
        }
        if (lastError != null)
            throw lastError;

        var uiPath = Path.Combine(AppContext.BaseDirectory, "ui");
        if (!Directory.Exists(uiPath))
            uiPath = AppContext.BaseDirectory;

        WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "moderntrcc.local",
            uiPath,
            CoreWebView2HostResourceAccessKind.Allow);

        var settings = WebView.CoreWebView2.Settings;
        settings.AreDefaultContextMenusEnabled = false;
        settings.AreDevToolsEnabled = false;
        settings.AreBrowserAcceleratorKeysEnabled = false;
        settings.IsBuiltInErrorPageEnabled = false;
        settings.IsStatusBarEnabled = false;
        settings.IsZoomControlEnabled = false;
        settings.IsGeneralAutofillEnabled = false;

        WebView.CoreWebView2.NavigationCompleted += (s, e) =>
        {
            // 子窗口创建完成后子类化，转发 WM_NCHITTEST 使窗口边缘可缩放
            SubclassWebViewChild();

            // 首次导航完成即认为界面就绪，通知 App 关闭启动画面
            if (!_readyFired)
            {
                _readyFired = true;
                Ready?.Invoke();
            }
            WebView.CoreWebView2.ExecuteScriptAsync(@"
                var style = document.createElement('style');
                style.textContent = `
                    * { -webkit-user-select: none !important; user-select: none !important; -webkit-user-drag: none !important; }
                    input, textarea { -webkit-user-select: text !important; user-select: text !important; }
                `;
                document.head.appendChild(style);
                document.addEventListener('contextmenu', e => e.preventDefault());
                document.addEventListener('keydown', e => {
                    if (e.ctrlKey && (e.key === '+' || e.key === '-' || e.key === '0')) e.preventDefault();
                    if (e.ctrlKey && e.shiftKey && (e.key === 'I' || e.key === 'J' || e.key === 'C')) e.preventDefault();
                });
                document.addEventListener('wheel', e => { if (e.ctrlKey) e.preventDefault(); }, { passive: false });
            ");
        };

        WebView.CoreWebView2.Navigate("https://moderntrcc.local/index.html");
        WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
    }

    private void OnLogReceived(LogEntry entry)
    {
        SendToJs("log", new { entry.Timestamp, entry.Level, entry.Message });
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var raw = e.TryGetWebMessageAsString();
            if (string.IsNullOrEmpty(raw)) return;

            var root = JsonDocument.Parse(raw).RootElement;
            var action = root.GetProperty("action").GetString();
            JsonElement data = root.TryGetProperty("data", out var d) ? d : default;

            _log.Info($"[UI] {action}");

            switch (action)
            {
                case "init": HandleInit(); break;
                case "scanDevices": HandleScanDevices(); break;
                case "getScreens": SendScreensList(); break;
                case "sendTestPattern":
                {
                    var s = GetScreen(data);
                    if (s != null) s.SendTestPattern(data.GetProperty("pattern").GetString()!);
                    break;
                }
                case "clearDisplay":
                {
                    var s = GetScreen(data);
                    if (s != null) s.ClearDisplay();
                    break;
                }
                case "uploadMedia": HandleUploadMedia(data.GetProperty("type").GetString()!); break;
                case "playMedia":
                {
                    var s = GetScreen(data);
                    if (s != null) s.PlayMedia(data.GetProperty("id").GetString()!, _mediaLib);
                    break;
                }
                case "deleteMedia": HandleDeleteMedia(data.GetProperty("id").GetString()!); break;
                case "getMediaList": SendMediaList(); break;
                case "getSettings":
                {
                    SendSettings(GetScreenSettings(data));
                    break;
                }
                case "getPlaybackStatus":
                {
                    var s = GetScreen(data);
                    if (s != null) SendPlaybackStatus(s);
                    break;
                }
                case "getDeviceStatus":
                {
                    var s = GetScreen(data);
                    if (s != null) SendDeviceStatus(s);
                    break;
                }
                case "applySettings": HandleApplySettings(data); break;
                case "getAutoStartStatus": SendAutoStartStatus(); break;
                case "enableAutoStart":
                    _settings.EnableAutoStart(
                        data.GetProperty("startMinimized").GetBoolean(),
                        data.GetProperty("highPriority").GetBoolean());
                    SendAutoStartStatus();
                    SendToastKey("toast.autostartOn", null, "success");
                    break;
                case "disableAutoStart":
                    _settings.DisableAutoStart();
                    SendAutoStartStatus();
                    SendToastKey("toast.autostartOff", null, "info");
                    break;
                case "saveSystemSettings": HandleSaveSystemSettings(data); break;
                case "getDiagnostics": SendDiagnostics(); break;
                case "getLogs": SendAllLogs(); break;
                case "clearLogs": _log.Info("日志已清除"); break;
                case "windowMinimize": Minimize(); break;
                case "windowClose": Close(); break;
                case "windowDrag": DragWindow(); break;
                case "closeConflicts": HandleCloseConflicts(); break;
            }
        }
        catch (Exception ex)
        {
            _log.Error($"[Bridge] {ex.Message}");
        }
    }

    private void SendToJs(string action, object data)
    {
        var json = JsonSerializer.Serialize(new { action, data });
        Dispatcher.Invoke(() =>
        {
            try { WebView.CoreWebView2.PostWebMessageAsString(json); }
            catch { }
        });
    }

    // ==================== 消息处理 ====================
    private void HandleInit()
    {
        _log.Info("=== Modern TRCC 启动 ===");
        _log.Info($"FFmpeg 可用: {_ffmpeg.IsFFmpegAvailable}");
        if (_ffmpeg.IsFFmpegAvailable)
            _log.Info($"FFmpeg 路径: {_ffmpeg.FFmpegPath}");

        PushAllState();

        Task.Run(() => HandleScanDevices());

        // 自启动带 --minimized 时，等设备扫描/自动播放启动后再隐藏到托盘，
        // 保证 WebView 已初始化、后台播放不受影响。
        // 注意：WebView 重载（如从托盘恢复时 RestoreWebViewAsync 触发 Reload）会再次
        // 触发 HandleInit，必须用一次性标志防止窗口显示后又被自动隐藏。
        if (_startMinimized && !_hideToTrayScheduled)
        {
            _hideToTrayScheduled = true;
            Dispatcher.BeginInvoke(async () =>
            {
                await Task.Delay(3000);
                Hide();
                EnterBackground();
            });
        }
    }

    /// <summary>
    /// 将当前所有状态推送到前端（页面刷新/从托盘恢复后调用）
    /// </summary>
    private void PushAllState()
    {
        SendAutoStartStatus();
        SendMediaList();
        SendAllLogs();
        SendScreensList();
        var first = _screens.Screens.FirstOrDefault();
        SendSettings(first?.Settings);
        foreach (var session in _screens.Screens)
        {
            SendDeviceStatus(session);
            SendPlaybackStatus(session);
        }
    }

    private void HandleScanDevices()
    {
        try
        {
            Task.Run(() =>
            {
                _screens.ScanAndConnectAll(_mediaLib);

                // 若有设备连接失败，检测是否有原版 TRCC/USBLCD 软件占用，给出明确提示并支持一键关闭重连
                var failed = _screens.Screens.Where(s => !s.Controller.IsConnected).ToList();
                if (failed.Count > 0)
                {
                    var conflicts = GetConflictingTrccProcesses();
                    if (conflicts.Count > 0)
                    {
                        string names = string.Join("、", conflicts);
                        Dispatcher.Invoke(() => SendToast($"设备被 {names} 占用，请先关闭这些程序再重试", "error", "closeConflicts"));
                    }
                    else
                    {
                        string reason = string.Join("; ", failed.Select(s => $"{s.DisplayName}: {s.Controller.LastError}"));
                        Dispatcher.Invoke(() => SendToast($"连接失败：{reason}", "error"));
                    }
                }

                SendDiagnostics();
            });
        }
        catch (Exception ex)
        {
            _log.Error($"扫描失败: {ex.Message}");
        }
    }

    /// <summary>绑定单屏会话的事件（幂等），把设备错误/连接成功/播放状态/toast 推到前端</summary>
    private void WireSession(LcdScreenSession session)
    {
        if (!_wiredSessions.Add(session)) return;

        session.Controller.ErrorOccurred += (s, msg) =>
            _log.Error($"[屏幕 {session.Index + 1}] USB: {msg}");
        session.Controller.DeviceConnected += (s, dev) =>
            _log.Success($"[屏幕 {session.Index + 1}] 设备已连接: {dev.Name} {dev.Width}x{dev.Height}");
        session.StateChanged += (s, e) =>
        {
            try { Dispatcher.Invoke(() => SendPlaybackStatus(session)); } catch { }
        };
        session.Notice += (s, n) =>
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    switch (n.Type)
                    {
                        case PlaybackNoticeType.Started:
                            SendToastKey("toast.playing", new { name = n.Detail ?? "" }, "info");
                            break;
                        case PlaybackNoticeType.Completed:
                            SendToastKey("toast.playDone", null, "info");
                            break;
                        case PlaybackNoticeType.Failed:
                            SendToastKey("toast.playFail", null, "error");
                            break;
                        case PlaybackNoticeType.SendFailed:
                            SendToastKey("toast.sendFailed", null, "error");
                            break;
                        case PlaybackNoticeType.PatternSent:
                            SendToastKey("toast.patternSent", null, "success");
                            break;
                        case PlaybackNoticeType.DeviceNotConnected:
                            SendToastKey("toast.deviceNotConnected", null, "warning");
                            break;
                    }
                });
            }
            catch { }
        };
    }

    /// <summary>从 JS 消息中解析屏幕索引并取会话（缺省为第一个屏幕）</summary>
    private LcdScreenSession? GetScreen(JsonElement data)
    {
        int idx = data.TryGetProperty("screen", out var sp) ? sp.GetInt32() : 0;
        return _screens.GetByIndex(idx);
    }

    /// <summary>从 JS 消息中解析屏幕索引并取该屏设置（未连接时回退到已保存的设置项）</summary>
    private ScreenSettings GetScreenSettings(JsonElement data)
    {
        int idx = data.TryGetProperty("screen", out var sp) ? sp.GetInt32() : 0;
        var session = _screens.GetByIndex(idx);
        if (session != null) return session.Settings;
        return _settings.Settings.Screens.FirstOrDefault(s => s.Index == idx)
               ?? _settings.Settings.Screens.FirstOrDefault()
               ?? new ScreenSettings();
    }

    /// <summary>推送屏幕列表（前端据此显示/隐藏屏幕切换条并更新已连接统计）</summary>
    private void SendScreensList()
    {
        var list = _screens.Screens.Select(session =>
        {
            var dev = session.Controller.ActiveDeviceInfo;
            return new
            {
                index = session.Index,
                name = session.DisplayName,
                connected = dev != null,
                resolution = dev != null ? $"{dev.Width}x{dev.Height}" : "",
                playing = session.CurrentlyPlaying,
            };
        }).ToList();
        SendToJs("screensList", new
        {
            screens = list,
            connectedCount = _screens.ConnectedCount
        });
    }

    private void TryStartAutoPlayFor(int screenIndex)
    {
        var session = _screens.GetByIndex(screenIndex);
        session?.TryStartAutoPlay(_mediaLib);
    }

    /// <summary>
    /// 检测可能占用 USB 设备的原版 TRCC/USBLCD 相关进程
    /// </summary>
    private static List<string> GetConflictingTrccProcesses()
    {
        var names = new List<string>();
        try
        {
            foreach (var proc in Process.GetProcesses())
            {
                string pn = proc.ProcessName;
                if (pn.Contains("USBLCD", StringComparison.OrdinalIgnoreCase) ||
                    pn.Contains("XProg", StringComparison.OrdinalIgnoreCase) ||
                    pn.Contains("TRCC", StringComparison.OrdinalIgnoreCase) ||
                    pn.Contains("CZTV", StringComparison.OrdinalIgnoreCase))
                {
                    if (proc.Id != Environment.ProcessId)
                        names.Add($"{pn} (PID {proc.Id})");
                }
            }
        }
        catch { }
        return names;
    }

    /// <summary>
    /// 用户点击“关闭并重连”：结束占用 USB 设备的原版 TRCC 进程，等待设备释放后重新扫描连接。
    /// </summary>
    private void HandleCloseConflicts()
    {
        Task.Run(() =>
        {
            try
            {
                int killed = 0;
                foreach (var proc in Process.GetProcesses())
                {
                    string pn = proc.ProcessName;
                    if (pn.Contains("USBLCD", StringComparison.OrdinalIgnoreCase) ||
                        pn.Contains("XProg", StringComparison.OrdinalIgnoreCase) ||
                        pn.Contains("TRCC", StringComparison.OrdinalIgnoreCase) ||
                        pn.Contains("CZTV", StringComparison.OrdinalIgnoreCase))
                    {
                        if (proc.Id == Environment.ProcessId) continue;
                        try { proc.Kill(); killed++; } catch { }
                    }
                }
                _log.Info($"已结束 {killed} 个占用进程，等待设备释放后重新连接...");
                Thread.Sleep(800);
                HandleScanDevices();
            }
            catch (Exception ex)
            {
                _log.Error($"关闭冲突进程失败: {ex.Message}");
            }
        });
    }


    private void HandleUploadMedia(string type)
    {
        Dispatcher.Invoke(() =>
        {
            string filter = type switch
            {
                "image" => "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|所有文件|*.*",
                "video" => "视频文件|*.mp4;*.avi;*.mkv;*.mov;*.webm;*.gif|所有文件|*.*",
                _ => "图片/视频文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.mp4;*.avi;*.mkv;*.mov;*.webm|所有文件|*.*",
            };

            var dialog = new OpenFileDialog { Filter = filter, Multiselect = true, Title = "选择文件" };
            if (dialog.ShowDialog() == true)
            {
                _log.Info($"选择了 {dialog.FileNames.Length} 个文件");
                Task.Run(async () =>
                {
                    foreach (var file in dialog.FileNames)
                    {
                        string ext = System.IO.Path.GetExtension(file).ToLower();
                        bool isImage = ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".webp";
                        if (isImage)
                            await _mediaLib.AddImageAsync(file);
                        else
                            await _mediaLib.AddVideoAsync(file);
                    }
                    SendMediaList();
                    SendToastKey("toast.mediaAdded", new { count = dialog.FileNames.Length }, "success");
                });
            }
        });
    }


    private void HandleDeleteMedia(string id)
    {
        var media = _mediaLib.MediaItems.FirstOrDefault(m => m.Id == id);
        if (media == null)
        {
            _log.Error($"媒体不存在: {id}");
            return;
        }

        try
        {
            // 若正在播放/显示该媒体，先停止并清空屏幕；并释放各屏的预渲染缓存
            foreach (var session in _screens.Screens)
                session.OnMediaDeleted(id, media.FilePath);

            string name = media.Name;
            bool ok = _mediaLib.RemoveMedia(media);
            SendMediaList();
            if (ok)
            {
                _log.Success($"已删除媒体: {name}");
                SendToastKey("toast.mediaDeleted", new { name }, "success");
            }
            else
            {
                _log.Error($"删除媒体失败: {name}");
                SendToastKey("toast.deleteFailed", null, "error");
            }
        }
        catch (Exception ex)
        {
            _log.Error($"删除媒体异常: {ex.Message}");
        }
    }

    private void SendPlaybackStatus(LcdScreenSession session)
    {
        bool playing = session.CurrentlyPlaying;
        int fps = session.CurrentPlayFps;
        // 静态预览：附带当前播放媒体的缩略图与信息（视频显示缩略图+时长/帧率/大小）
        MediaItem? media = null;
        if (!string.IsNullOrEmpty(session.CurrentMediaId))
            media = _mediaLib.MediaItems.FirstOrDefault(m => m.Id == session.CurrentMediaId);
        SendToJs("playbackStatus", new
        {
            screen = session.Index,
            playing,
            fps,
            media = media == null ? null : new
            {
                name = media.Name,
                type = media.Type.ToString().ToLower(),
                resolution = media.Width > 0 ? $"{media.Width}x{media.Height}" : "未知",
                duration = media.DurationMs,
                fps = media.Fps,
                fileSize = media.FileSize,
                thumbnail = media.ThumbnailPath ?? ""
            }
        });
    }

    private void SendMediaList()
    {
        var items = _mediaLib.MediaItems.Select(m => new
        {
            id = m.Id,
            name = m.Name,
            type = m.Type.ToString().ToLower(),
            resolution = m.Width > 0 ? $"{m.Width}x{m.Height}" : "未知",
            thumbnail = m.ThumbnailPath ?? ""
        }).ToList();

        SendToJs("mediaList", new { items });
    }

    private void SendSettings(ScreenSettings? s = null)
    {
        s ??= _settings.Settings.Screens.FirstOrDefault() ?? new ScreenSettings();
        SendToJs("settingsLoaded", new
        {
            screen = s.Index,
            screenName = s.Name,
            brightness = s.Display.Brightness,
            contrast = s.Display.Contrast,
            saturation = s.Display.Saturation,
            rotation = s.Display.Rotation,
            targetFps = s.Display.TargetFps,
            videoFps = s.Playback.VideoFps,
            loopVideo = s.Playback.LoopVideo,
            fitMode = s.Playback.FitMode,
            enableAutoplay = s.AutoPlay.Enabled,
            autoplayMediaId = s.AutoPlay.MediaId,
            startMinimized = _settings.Settings.AutoStart.StartMinimized,
            highPriority = _settings.Settings.AutoStart.HighPriority,
            minimizeToTray = _settings.Settings.Ui.MinimizeToTray,
            enableMica = _settings.Settings.Ui.EnableMica,
            language = _settings.Settings.Ui.Language
        });
    }

    private async void HandleApplySettings(JsonElement data)
    {
        try
        {
            int idx = data.TryGetProperty("screen", out var sp) ? sp.GetInt32() : 0;
            var session = _screens.GetByIndex(idx);
            if (session == null)
            {
                _log.Warn($"屏幕 {idx + 1} 尚未连接，忽略设置");
                return;
            }

            var s = session.Settings;
            s.Display.Brightness = data.GetProperty("brightness").GetInt32();
            s.Display.Contrast = data.GetProperty("contrast").GetInt32();
            s.Display.Saturation = data.GetProperty("saturation").GetInt32();
            s.Display.TargetFps = data.GetProperty("targetFps").GetInt32();
            s.Playback.VideoFps = data.GetProperty("videoFps").GetInt32();
            s.Playback.LoopVideo = data.GetProperty("loopVideo").GetBoolean();
            if (data.TryGetProperty("rotation", out var rotProp))
                s.Display.Rotation = rotProp.GetInt32();
            if (data.TryGetProperty("fitMode", out var fitProp))
                s.Playback.FitMode = fitProp.GetString() ?? "cover";
            _settings.Save();

            // 停旧播放→清本屏缓存→按新参数重渲当前媒体
            await session.ApplyDisplaySettingsAsync(_mediaLib);
            // 回传最新设置，让滑块/开关状态与后端一致
            SendSettings(session.Settings);
        }
        catch (Exception ex)
        {
            _log.Error($"应用显示设置失败: {ex.Message}");
        }
    }

    private void HandleSaveSystemSettings(JsonElement data)
    {
        var s = _settings.Settings;
        s.AutoStart.StartMinimized = data.GetProperty("startMinimized").GetBoolean();
        s.AutoStart.HighPriority = data.GetProperty("highPriority").GetBoolean();
        s.Ui.MinimizeToTray = data.GetProperty("minimizeToTray").GetBoolean();
        if (data.TryGetProperty("enableMica", out var micaProp))
            s.Ui.EnableMica = micaProp.GetBoolean();
        if (data.TryGetProperty("language", out var langProp))
            s.Ui.Language = langProp.GetString() ?? "zh-CN";
        // 开机自动播放设置（按屏幕独立保存；语言切换等旧调用不携带时保持原值）
        if (data.TryGetProperty("enableAutoplay", out var apProp))
        {
            int screenIdx = data.TryGetProperty("screen", out var sp) ? sp.GetInt32() : 0;
            // 优先用会话定位设置（会话与其 DeviceKey 绑定的设置条一一对应），
            // 避免按 Index 匹配到历史遗留的同 Index 垃圾条目而写错设置
            var session = _screens.GetByIndex(screenIdx);
            var settings = session?.Settings
                           ?? _settings.Settings.Screens.FirstOrDefault(x => x.Index == screenIdx)
                           ?? _settings.Settings.Screens.FirstOrDefault();
            if (settings != null)
            {
                settings.AutoPlay.Enabled = apProp.GetBoolean();
                if (data.TryGetProperty("autoplayMedia", out var amProp))
                    settings.AutoPlay.MediaId = amProp.GetString();
                // 重新启用后允许再次触发自动播放，并在设备就绪时立即播放
                if (settings.AutoPlay.Enabled)
                {
                    (session ?? _screens.GetByIndex(settings.Index))?.ResetAutoPlay();
                    TryStartAutoPlayFor(session?.Index ?? settings.Index);
                }
            }
        }
        _settings.Save();
        _trayEnabled = s.Ui.MinimizeToTray;
        if (s.AutoStart.HighPriority)
            SettingsService.SetProcessPriority(System.Diagnostics.ProcessPriorityClass.High);
        _log.Info("系统设置已保存");
    }

    private void SendAutoStartStatus()
    {
        bool enabled = _settings.IsAutoStartEnabled();
        SendToJs("autoStartStatus", new { enabled });
    }


    private void SendToast(string message, string type = "info", string? action = null)
    {
        SendToJs("toast", new { message, type, action });
    }

    // 按 i18n key 发送 toast，前端根据当前语言解析文本
    private void SendToastKey(string key, object? values = null, string type = "info")
    {
        SendToJs("toast", new { key, values, type });
    }

    private void SendAllLogs()
    {
        var logs = _log.GetAll().Select(l => new { l.Timestamp, l.Level, l.Message }).ToList();
        SendToJs("allLogs", new { logs });
    }


    // ==================== 诊断 ====================
    private void SendDiagnostics()
    {
        Task.Run(() =>
        {
            var known = new List<KnownDevice>
            {
                new("0x87CD", "0x70DB", "Thermalright LCD", "Thermalright"),
                new("0x0416", "0x5406", "ALi Corp LCD", "ALi"),
                new("0x0402", "0x3922", "ALi Corp USB PRC", "ALi"),
                new("0x87AD", "0x70DB", "Thermalright LCD (Bulk)", "Thermalright"),
                new("0x0416", "0x5302", "ALi HID LCD Type 2", "ALi"),
                new("0x0418", "0x5303", "ALi HID LCD Type 3", "ALi"),
                new("0x0418", "0x5304", "ALi HID LCD Type 3", "ALi"),
                new("0x0416", "0x5408", "ALi LY LCD", "ALi"),
                new("0x0416", "0x5409", "ALi LY1 LCD", "ALi"),
            };

            var wmiDevices = ScanWmiUsbDevices();

            foreach (var k in known)
            {
                int kVid = Convert.ToInt32(k.vid, 16);
                int kPid = Convert.ToInt32(k.pid, 16);
                k.found = wmiDevices.Any(w => w.Vid == kVid && w.Pid == kPid);
            }

            var usbList = wmiDevices
                .GroupBy(w => (w.Vid, w.Pid))
                .Select(g => g.First())
                .Select(w => new
                {
                    vid = w.Vid,
                    pid = w.Pid,
                    vidStr = $"0x{w.Vid:X4}",
                    pidStr = $"0x{w.Pid:X4}",
                    name = w.Name,
                    status = w.Status
                })
                .ToList();

            SendToJs("diagnosticsData", new { knownDevices = known, usbDevices = usbList });
        });
    }

    private List<UsbDeviceInfo> ScanWmiUsbDevices()
    {
        var list = new List<UsbDeviceInfo>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE PNPDeviceID LIKE 'USB%'");
            foreach (var item in searcher.Get())
            {
                var name = item["Name"]?.ToString() ?? "";
                var deviceId = item["DeviceID"]?.ToString() ?? "";
                var status = item["Status"]?.ToString() ?? "";

                int vid = 0, pid = 0;
                var vidMatch = System.Text.RegularExpressions.Regex.Match(
                    deviceId, @"VID_([0-9A-Fa-f]{4})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var pidMatch = System.Text.RegularExpressions.Regex.Match(
                    deviceId, @"PID_([0-9A-Fa-f]{4})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (vidMatch.Success) vid = Convert.ToInt32(vidMatch.Groups[1].Value, 16);
                if (pidMatch.Success) pid = Convert.ToInt32(pidMatch.Groups[1].Value, 16);

                if (vid > 0)
                    list.Add(new UsbDeviceInfo { Name = name, Vid = vid, Pid = pid, DeviceId = deviceId, Status = status });
            }
        }
        catch (Exception ex)
        {
            _log.Error($"WMI 扫描失败: {ex.Message}");
        }
        return list;
    }

    private void SendDeviceStatus(LcdScreenSession session)
    {
        var dev = session.Controller.ActiveDeviceInfo;
        if (dev != null)
        {
            SendToJs("deviceStatus", new
            {
                screen = session.Index,
                connected = true,
                deviceName = dev.Name,
                resolution = $"{dev.Width}x{dev.Height}"
            });
        }
        else
        {
            SendToJs("deviceStatus", new { screen = session.Index, connected = false, deviceName = "", resolution = "" });
        }
    }

    // ==================== 托盘 ====================
    private void InitializeTray()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = LoadAppIcon(),
            Visible = true,
            Text = "Modern TRCC"
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("显示窗口", null, (s, e) => ShowWindow());
        menu.Items.Add("扫描设备", null, (s, e) => HandleScanDevices());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("退出", null, (s, e) =>
        {
            if (_trayIcon != null) _trayIcon.Visible = false;
            System.Windows.Application.Current.Shutdown();
        });
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (s, e) => ShowWindow();
    }

    private void ShowWindow()
    {
        Dispatcher.Invoke(async () =>
        {
            // 最小化启动时窗口位于屏幕外，从托盘恢复前先移回屏幕中央
            if (Left < -100 || Top < -100 ||
                Left > SystemParameters.VirtualScreenWidth || Top > SystemParameters.VirtualScreenHeight)
            {
                CenterWindow();
            }

            Show();
            Activate();
            WindowState = WindowState.Normal;
            ShowInTaskbar = true;

            // 恢复 WebView（挂起时先 Resume，再重新加载并推送全部状态）
            ResumeWebView();
            await RestoreWebViewAsync();
            PushAllState();
        });
    }

    private void CenterWindow()
    {
        try
        {
            double sx = SystemParameters.PrimaryScreenWidth;
            double sy = SystemParameters.PrimaryScreenHeight;
            Left = (sx - Width) / 2;
            Top = (sy - Height) / 2;
        }
        catch
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    // 从 Resources 目录加载应用图标（TRCC.ico），失败时回退到系统图标
    private static System.Drawing.Icon LoadAppIcon()
    {
        try
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "TRCC.ico");
            if (File.Exists(iconPath))
                return new System.Drawing.Icon(iconPath);
        }
        catch { }
        return SystemIcons.Application;
    }

    private async Task RestoreWebViewAsync()
    {
        try
        {
            if (WebView.CoreWebView2 == null)
            {
                await InitializeWebView();
            }
            else
            {
                try { WebView.CoreWebView2.Reload(); } catch { }
            }
        }
        catch (Exception ex)
        {
            _log.Error($"恢复 WebView 失败: {ex.Message}");
        }
    }

    private void MainWindow_Activated(object? sender, EventArgs e)
    {
        // WebView2 在任务切换（Win+Tab/Alt+Tab）后可能丢失渲染表面导致黑屏，
        // 激活时强制重新合成。仅当窗口确实从后台切回时执行，避免频繁重绘。
        if (!_wasDeactivated) return;
        _wasDeactivated = false;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            // 若 WebView 仍处于挂起状态（此前最小化到托盘），先恢复
            if (_webViewSuspended) ResumeWebView();
            ForceWebViewRedraw();
        }), DispatcherPriority.Background);
    }

    private bool _wasDeactivated;

    private void ForceWebViewRedraw()
    {
        try
        {
            if (WebView.CoreWebView2 == null) return;
            // 轻微调整尺寸强制 WebView2 重新合成渲染表面
            double w = WebView.ActualWidth;
            double h = WebView.ActualHeight;
            WebView.Width = w + 1;
            WebView.UpdateLayout();
            WebView.Width = w;
            WebView.Height = h;
        }
        catch { }
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && _trayEnabled && _trayIcon != null)
        {
            Hide();
            _trayIcon.ShowBalloonTip(2000, "Modern TRCC", "已最小化到托盘", ToolTipIcon.Info);
            EnterBackground();
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_trayEnabled && _trayIcon != null)
        {
            e.Cancel = true;
            Hide();
            EnterBackground();
        }
        else if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
    }

    private void Minimize()
    {
        WindowState = WindowState.Minimized;
    }

    // ==================== 后台内存优化：隐藏时挂起 WebView ====================

    private void EnterBackground()
    {
        // 隐藏到托盘时挂起 WebView，显著降低后台内存/CPU 占用
        SuspendWebView();
    }

    private async void SuspendWebView()
    {
        try
        {
            if (WebView.CoreWebView2 == null || _webViewSuspended) return;
            var ok = await WebView.CoreWebView2.TrySuspendAsync();
            _webViewSuspended = ok;
        }
        catch { _webViewSuspended = false; }
    }

    private void ResumeWebView()
    {
        try
        {
            if (WebView.CoreWebView2 == null) return;
            if (_webViewSuspended)
            {
                WebView.CoreWebView2.Resume();
                _webViewSuspended = false;
            }
        }
        catch { }
    }
}

internal class UsbDeviceInfo
{
    public string Name { get; set; } = "";
    public int Vid { get; set; }
    public int Pid { get; set; }
    public string DeviceId { get; set; } = "";
    public string Status { get; set; } = "";
}

internal class KnownDevice
{
    public string vid { get; set; }
    public string pid { get; set; }
    public string name { get; set; }
    public string brand { get; set; }
    public bool found { get; set; }
    public KnownDevice(string vid, string pid, string name, string brand) { this.vid = vid; this.pid = pid; this.name = name; this.brand = brand; }
}
