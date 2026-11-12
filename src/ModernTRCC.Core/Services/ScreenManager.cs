using ModernTRCC.Core.Models;
using System.Collections.ObjectModel;

namespace ModernTRCC.Core.Services;

/// <summary>
/// 多屏幕管理器：扫描全部已知 USB 设备，为每块屏幕创建独立的
/// <see cref="LcdScreenSession"/>（各自控制器/预渲染/播放状态/设置），
/// 实现两块屏幕完全分离的管理。
/// </summary>
public class ScreenManager : IDisposable
{
    private readonly SettingsService _settings;
    private readonly LogService _log;

    /// <summary>当前已发现的屏幕会话（按发现顺序）</summary>
    public ObservableCollection<LcdScreenSession> Screens { get; } = new();

    /// <summary>屏幕列表变化（连接/断开/扫描完成）时触发</summary>
    public event EventHandler? ScreensChanged;

    public int ConnectedCount => Screens.Count(s => s.Controller.IsConnected);

    public ScreenManager(SettingsService settings, LogService log)
    {
        _settings = settings;
        _log = log;
    }

    /// <summary>
    /// 扫描并连接所有已知设备：每个设备一个会话（按 VID:PID 稳定绑定设置），
    /// 已移除的设备断开并移除会话。
    /// </summary>
    public void ScanAndConnectAll(MediaLibraryService mediaLib)
    {
        try
        {
            _log.Info("开始扫描 USB 设备...");

            // 1. 用临时控制器枚举全部已知设备（去重后每个 VID:PID 一个条目）
            List<LcdDeviceInfo> found;
            using (var scanner = new LcdController())
            {
                int count = scanner.ScanDevices();
                _log.Info($"LibUSB 扫描到 {count} 个已知设备");
                found = scanner.Devices.ToList();
            }

            var keysFound = new HashSet<string>();
            foreach (var devInfo in found)
            {
                string key = $"{devInfo.Vid:X4}:{devInfo.Pid:X4}";
                keysFound.Add(key);

                var session = GetOrCreateSession(devInfo);
                if (session.Controller.IsConnected)
                {
                    _log.Info($"设备已连接，跳过重复连接: {devInfo.Name} {key}");
                    continue;
                }

                _log.Info($"尝试连接: {devInfo.Name} (VID:0x{devInfo.Vid:X4} PID:0x{devInfo.Pid:X4})");

                // 重试连接：设备可能被短暂占用（如刚插拔/其他程序正在枚举）
                bool connected = false;
                for (int attempt = 1; attempt <= 3 && !connected; attempt++)
                {
                    if (attempt > 1)
                    {
                        _log.Info($"[屏幕 {session.Index + 1}] 连接失败，{attempt}/3 重试中...");
                        Thread.Sleep(400);
                    }
                    connected = session.Controller.Connect(devInfo);
                }

                if (connected)
                {
                    _log.Success($"[屏幕 {session.Index + 1}] 连接成功! 分辨率: {devInfo.Width}x{devInfo.Height}");
                    session.TryStartAutoPlay(mediaLib);
                }
                else
                {
                    string reason = string.IsNullOrEmpty(session.Controller.LastError)
                        ? "未知原因" : session.Controller.LastError;
                    _log.Error($"[屏幕 {session.Index + 1}] 连接失败 - {reason}");
                }
            }

            // 2. 移除已断开的设备会话
            foreach (var session in Screens.ToList())
            {
                if (!keysFound.Contains(session.DeviceKey))
                {
                    _log.Info($"设备已移除: {session.DeviceKey}");
                    session.Dispose();
                    Screens.Remove(session);
                }
            }

            // 3. 同步会话与其设置条的 Index：确保唯一且从 0 连续。
            //    设备移除/重插后若不同步，Index 会重复，导致设置保存/回退时匹配错乱
            for (int i = 0; i < Screens.Count; i++)
            {
                Screens[i].Settings.Index = i;
            }
            _settings.Save();

            ScreensChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _log.Error($"扫描失败: {ex.Message}");
            ScreensChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>按屏幕索引取会话，越界返回 null</summary>
    public LcdScreenSession? GetByIndex(int index)
    {
        if (index >= 0 && index < Screens.Count) return Screens[index];
        return null;
    }

    /// <summary>
    /// 获取（或创建）指定设备的会话。
    /// 设置按 DeviceKey（VID:PID）稳定绑定：重新插拔/重启后仍保持该屏的设置。
    /// </summary>
    private LcdScreenSession GetOrCreateSession(LcdDeviceInfo devInfo)
    {
        string key = $"{devInfo.Vid:X4}:{devInfo.Pid:X4}";

        var existing = Screens.FirstOrDefault(s => s.DeviceKey == key);
        if (existing != null) return existing;

        // 查找该设备的设置项：优先按 DeviceKey，其次取首个未绑定的旧设置项，最后新建
        var settings = _settings.Settings.Screens.FirstOrDefault(s => s.DeviceKey == key);
        if (settings == null)
        {
            settings = _settings.Settings.Screens.FirstOrDefault(s => string.IsNullOrEmpty(s.DeviceKey));
            if (settings == null)
            {
                settings = new ScreenSettings
                {
                    Name = $"屏幕 {_settings.Settings.Screens.Count + 1}",
                };
                _settings.Settings.Screens.Add(settings);
            }
        }

        settings.DeviceKey = key;
        settings.Index = Screens.Count;
        if (string.IsNullOrEmpty(settings.Name))
            settings.Name = $"屏幕 {Screens.Count + 1}";

        var session = new LcdScreenSession(Screens.Count, settings, _log);
        Screens.Add(session);
        _settings.Save();
        return session;
    }

    public void Dispose()
    {
        foreach (var session in Screens)
        {
            try { session.Dispose(); } catch { }
        }
        Screens.Clear();
    }
}