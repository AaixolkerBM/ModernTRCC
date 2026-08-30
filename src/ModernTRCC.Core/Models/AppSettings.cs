namespace ModernTRCC.Core.Models;

public class AppSettings
{
    public DisplaySettings Display { get; set; } = new();
    public AutoStartSettings AutoStart { get; set; } = new();
    public PlaybackSettings Playback { get; set; } = new();
    public AutoPlaySettings AutoPlay { get; set; } = new();
    public UiSettings Ui { get; set; } = new();
    public HardwareSettings Hardware { get; set; } = new();
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
