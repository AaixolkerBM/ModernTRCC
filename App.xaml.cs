using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ModernTRCC.Core.Services;
using ModernTRCC.App.Views;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ModernTRCC.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    // 单实例互斥体与“显示主窗口”信号（供第二个实例请求首个实例显示窗口）
    public const string SingleInstanceMutexName = "Global\\ModernTRCC_SingleInstance_{8F3C1E2A-5B4D-4C6E-9A7F-2D1E0B3C4A5D}";
    public const string ShowWindowEventName = "Global\\ModernTRCC_ShowWindow_{8F3C1E2A-5B4D-4C6E-9A7F-2D1E0B3C4A5D}";
    private static Mutex? _singleInstanceMutex;
    public static EventWaitHandle? ShowWindowEvent { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        // 单实例检查：重复实例会同时使用同一个 WebView2 用户数据目录，
        // 触发“请求的资源在使用中。(0x800700AA)”。已有实例时退出本实例。
        bool createdNew;
        try
        {
            _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out createdNew);
        }
        catch (UnauthorizedAccessException)
        {
            // 非提权进程可能无法创建 Global 互斥体，退化为按进程名判断
            createdNew = !AnotherInstanceRunning();
        }

        if (!createdNew || AnotherInstanceRunning())
        {
            // 仅用户手动启动（无 --minimized）才请求已有实例显示窗口；
            // 开机自启动的重复实例带 --minimized，必须静默退出，
            // 否则双通道（注册表 Run + 计划任务）拉起时会通知首个实例弹出窗口
            if (!e.Args.Contains("--minimized"))
            {
                try
                {
                    using var evt = EventWaitHandle.OpenExisting(ShowWindowEventName);
                    evt.Set();
                }
                catch
                {
                    // 已有实例以不同权限级别运行，无法通知它，改为提示用户
                    MessageBox.Show(
                        "Modern TRCC 已在运行，请从任务栏托盘图标打开窗口。",
                        "Modern TRCC", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            Shutdown();
            return;
        }

        try
        {
            ShowWindowEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowWindowEventName);
        }
        catch
        {
            // 事件创建失败不影响主流程（仅影响第二个实例唤醒窗口）
        }

        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        try
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            var settings = Services.GetRequiredService<SettingsService>();
            settings.Initialize();

            // 启动时扫描 media 目录，加载已有素材（后台执行，不阻塞启动）
            var mediaLib = Services.GetRequiredService<MediaLibraryService>();
            _ = Task.Run(() => mediaLib.InitializeAsync());

            if (settings.Settings.AutoStart.HighPriority)
                SettingsService.SetProcessPriority(ProcessPriorityClass.High);

            bool startMinimized = e.Args.Contains("--minimized");

            // 启动画面：最小化启动（自启动/后台运行）时不显示
            SplashWindow? splash = null;
            if (!startMinimized)
            {
                splash = new SplashWindow();
                splash.Show();
                splash.SetStatus("正在加载界面...");
            }

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;

            // 有启动画面时，先把主窗口放到屏幕外初始化（WebView2 是独立子窗口，
            // WPF 的 Opacity 不能让它透明），就绪后再移回屏幕中央并淡入
            if (splash != null)
            {
                mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                mainWindow.Left = -10000;
                mainWindow.Top = -10000;
                mainWindow.ShowInTaskbar = false;
            }
            // 最小化启动（自启动/后台运行）：窗口放到屏幕外并隐藏任务栏，
            // 避免开机时窗口闪现后再隐藏；从托盘恢复时再移回屏幕中央
            else if (startMinimized)
            {
                mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                mainWindow.Left = -10000;
                mainWindow.Top = -10000;
                mainWindow.ShowInTaskbar = false;
                mainWindow.ShowActivated = false;
            }

            mainWindow.Ready += () =>
            {
                if (splash == null) return;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    splash.SetStatus("启动完成");
                    splash.Close();
                    splash = null;

                    // 先隐藏再移到屏幕中央，避免闪现，随后淡入
                    mainWindow.Opacity = 0;
                    CenterMainWindow(mainWindow);
                    mainWindow.ShowInTaskbar = true;

                    var fade = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(220),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    mainWindow.BeginAnimation(UIElement.OpacityProperty, fade);
                }));
            };

            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"启动失败：{ex}\n\n{ex.StackTrace}", "Modern TRCC", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<LogService>();
        services.AddSingleton<FFmpegService>();
        services.AddSingleton<MediaLibraryService>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ScreenManager>();
    }

    /// <summary>
    /// 将窗口居中到主显示器工作区（多显示器取当前鼠标所在屏幕）
    /// </summary>
    private static void CenterMainWindow(Window window)
    {
        try
        {
            double sx = SystemParameters.PrimaryScreenWidth;
            double sy = SystemParameters.PrimaryScreenHeight;
            window.Left = (sx - window.Width) / 2;
            window.Top = (sy - window.Height) / 2;
        }
        catch
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    // 按进程名判断是否已有实例（可跨权限级别检测，不依赖内核对象访问权限）
    private static bool AnotherInstanceRunning()
    {
        var currentId = Environment.ProcessId;
        foreach (var process in Process.GetProcessesByName("ModernTRCC"))
        {
            using (process)
            {
                if (process.Id != currentId)
                    return true;
            }
        }
        return false;
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        MessageBox.Show($"未处理异常：{e.Exception.Message}\n\n{e.Exception.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        MessageBox.Show($"致命错误：{ex?.Message ?? "未知"}", "致命错误", MessageBoxButton.OK, MessageBoxImage.Stop);
    }
}
