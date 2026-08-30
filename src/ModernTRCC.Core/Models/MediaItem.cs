namespace ModernTRCC.Core.Models;

public class MediaItem
{
    // 基于文件路径生成的稳定 ID：同一文件在每次启动扫描时得到相同 ID，
    // 保证「开机自动播放」保存的 MediaId 在重启后仍能匹配到对应媒体
    public string Id { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double DurationMs { get; set; }
    public double Fps { get; set; }
    public long FileSize { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.Now;
}

public enum MediaType
{
    Image = 0,
    Video = 1,
    Gif = 2,
}
