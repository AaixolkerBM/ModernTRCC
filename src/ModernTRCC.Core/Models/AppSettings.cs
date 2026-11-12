namespace ModernTRCC.Core.Models;

public class AppSettings
{
    /// <summary>每个屏幕一套完全独立的设置（显示/播放/自动播放）</summary>
    public List<ScreenSettings> Screens { get; set; } = new() { new ScreenSettings() };

    // ===== 以下为 v1.0.x 旧版根级字段，仅用于迁移与兼容 =====
    // 加载时：Screens 为空则用旧字段填充 Screens[0]；
    // 保存时：Screens[0] 镜像回旧字段，保证 settings.json 兼容可读。
    public DisplaySettings Display { get; set; } = new();
    public PlaybackSettings Playback { get; set; } = new();
    public AutoPlaySettings AutoPlay { get; set; } = new();
    public AutoStartSettings AutoStart { get; set; } = new();
    public UiSettings Ui { get; set; } = new();
    public HardwareSettings Hardware { get; set; } = new();
}

/// <summary>单块屏幕的设置（完全分离管理：每屏独立）</summary>
public class ScreenSettings
{
    /// <summary>设备标识（VID:PID），用于把设置稳定绑定到物理屏幕</summary>
    public string? DeviceKey { get; set; }
    public int Index { get; set; }
    public string Name { get; set; } = "屏幕 1";
    public DisplaySettings Display { get; set; } = new();
    public PlaybackSettings Playback { get; set; } = new();
    public AutoPlaySettings AutoPlay { get; set; } = new();
}

public class DisplaySettings
{
    public int Brightness { get; set; } = 100;
    public int Contrast { get; set; } = 100;
    public int Saturation { get; set; } = 100;
    public int Rotation { get; set; } = 0;
    public int TargetFps { get; set; } = 30;
}

public class AutoStartSettings
{
    public bool Enabled { get; set; }
    public bool StartMinimized { get; set; }
    public bool HighPriority { get; set; }
}

public class PlaybackSettings
{
    public int VideoFps { get; set; } = 30;
    public bool LoopVideo { get; set; } = true;
    public string FitMode { get; set; } = "cover";
}

/// <summary>开机自动播放：自启动后自动播放选定的媒体</summary>
public class AutoPlaySettings
{
    public bool Enabled { get; set; }
    public string? MediaId { get; set; }
}

public class UiSettings
{
    public string Theme { get; set; } = "dark";
    public string Language { get; set; } = "zh-CN";
    public bool MinimizeToTray { get; set; } = true;
    public bool EnableMica { get; set; } = true; // 云母/毛玻璃效果
}

public class HardwareSettings
{
    public string? SelectedDeviceId { get; set; }
}