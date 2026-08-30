using ModernTRCC.Core.Models;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace ModernTRCC.Core.Services;

public class MediaLibraryService
{
    private readonly FFmpegService _ffmpeg;
    private readonly LogService _log;
    private readonly string _mediaDir;
    private readonly string _thumbDir;

    private static readonly HashSet<string> ImageExts = new(StringComparer.OrdinalIgnoreCase)
        { ".png", ".jpg", ".jpeg", ".bmp", ".webp" };
    private static readonly HashSet<string> VideoExts = new(StringComparer.OrdinalIgnoreCase)
        { ".mp4", ".avi", ".mkv", ".mov", ".webm", ".gif" };

    public ObservableCollection<MediaItem> MediaItems { get; } = new();

    /// <summary>媒体库内容变化（新增/删除/启动扫描完成）时触发</summary>
    public event EventHandler? MediaLibraryChanged;

    public MediaLibraryService(FFmpegService ffmpeg, LogService log)
    {
        _ffmpeg = ffmpeg;
        _log = log;
        _mediaDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "media");
        _thumbDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "thumbnails");
        Directory.CreateDirectory(_mediaDir);
        Directory.CreateDirectory(_thumbDir);
    }

    /// <summary>
    /// 启动时扫描 media 目录，加载已有文件（不会重复复制）
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var files = Directory.GetFiles(_mediaDir);
            int loaded = 0;
            foreach (var file in files)
            {
                if (MediaItems.Any(m => string.Equals(m.FilePath, file, StringComparison.OrdinalIgnoreCase)))
                    continue;

                string ext = Path.GetExtension(file);
                MediaItem? item = null;
                if (ImageExts.Contains(ext))
                    item = await CreateImageItemAsync(file);
                else if (VideoExts.Contains(ext))
                    item = await CreateVideoItemAsync(file);

                if (item != null)
                {
                    MediaItems.Add(item);
                    loaded++;
                }
            }
            if (loaded > 0)
                _log.Success($"媒体库已加载 {loaded} 个已有文件");
            RaiseChanged();
        }
        catch (Exception ex)
        {
            _log.Error($"扫描媒体目录失败: {ex.Message}");
        }
    }

    private void RaiseChanged() => MediaLibraryChanged?.Invoke(this, EventArgs.Empty);

    public async Task<MediaItem?> AddImageAsync(string filePath, string? name = null)
    {
        try
        {
            string ext = Path.GetExtension(filePath).ToLower();
            string baseName = name ?? Path.GetFileNameWithoutExtension(filePath);
            string destPath = GetUniqueDestPath(baseName, ext);
            File.Copy(filePath, destPath, true);

            var item = await CreateImageItemAsync(destPath);
            if (item != null)
            {
                item.Name = baseName;
                MediaItems.Add(item);
                RaiseChanged();
                _log.Info($"已添加图片: {item.Name} ({item.Width}x{item.Height})");
            }
            return item;
        }
        catch (Exception ex)
        {
            _log.Error($"添加图片失败: {ex.Message}");
            return null;
        }
    }

    public async Task<MediaItem?> AddVideoAsync(string filePath, string? name = null)
    {
        try
        {
            _log.Info($"正在导入视频: {Path.GetFileName(filePath)}");

            string ext = Path.GetExtension(filePath).ToLower();
            string baseName = name ?? Path.GetFileNameWithoutExtension(filePath);
            string destPath = GetUniqueDestPath(baseName, ext);
            File.Copy(filePath, destPath, true);

            var item = await CreateVideoItemAsync(destPath);
            if (item != null)
            {
                item.Name = baseName;
                MediaItems.Add(item);
                RaiseChanged();
                _log.Success($"视频导入完成: {item.Name}");
            }
            return item;
        }
        catch (Exception ex)
        {
            _log.Error($"添加视频失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 生成不冲突的目标路径：保留原始文件名，重名时自动追加 (1)、(2)…
    /// </summary>
    private string GetUniqueDestPath(string baseName, string ext)
    {
        string destPath = Path.Combine(_mediaDir, baseName + ext);
        if (!File.Exists(destPath)) return destPath;

        for (int i = 1; ; i++)
        {
            destPath = Path.Combine(_mediaDir, $"{baseName} ({i}){ext}");
            if (!File.Exists(destPath)) return destPath;
        }
    }

    /// <summary>
    /// 基于文件路径计算稳定 ID（MD5 哈希），重启后同一媒体保持相同 ID
    /// </summary>
    private static string ComputeMediaId(string filePath)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(filePath.ToLowerInvariant()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task<MediaItem?> CreateImageItemAsync(string filePath)
    {
        try
        {
            string thumbBase64 = "";
            int width = 0, height = 0;
            using (var bmp = new Bitmap(filePath))
            {
                width = bmp.Width;
                height = bmp.Height;
                thumbBase64 = BitmapToBase64(bmp, 200, 200);
            }

            return new MediaItem
            {
                Id = ComputeMediaId(filePath),
                Type = MediaType.Image,
                Name = Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath,
                ThumbnailPath = thumbBase64,
                Width = width,
                Height = height,
                FileSize = new FileInfo(filePath).Length,
            };
        }
        catch (Exception ex)
        {
            _log.Error($"读取图片失败 {filePath}: {ex.Message}");
            return null;
        }
    }

    private async Task<MediaItem?> CreateVideoItemAsync(string filePath)
    {
        try
        {
            int width = 0, height = 0;
            double fps = 0, duration = 0;

            if (_ffmpeg.IsFFmpegAvailable)
            {
                var info = await _ffmpeg.GetVideoInfoAsync(filePath);
                if (info != null)
                {
                    width = info.Width;
                    height = info.Height;
                    fps = info.Fps;
                    duration = info.DurationSeconds;
                }
            }

            string thumbBase64 = "";
            if (_ffmpeg.IsFFmpegAvailable)
            {
                var thumb = await _ffmpeg.GetVideoThumbnailAsync(filePath, 200);
                if (thumb != null)
                {
                    thumbBase64 = BitmapToBase64(thumb, 200, 200);
                    thumb.Dispose();
                }
            }

            string ext = Path.GetExtension(filePath).ToLower();
            return new MediaItem
            {
                Id = ComputeMediaId(filePath),
                Type = ext == ".gif" ? MediaType.Gif : MediaType.Video,
                Name = Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath,
                ThumbnailPath = thumbBase64,
                Width = width,
                Height = height,
                Fps = fps,
                DurationMs = duration * 1000,
                FileSize = new FileInfo(filePath).Length,
            };
        }
        catch (Exception ex)
        {
            _log.Error($"读取视频失败 {filePath}: {ex.Message}");
            return null;
        }
    }

    public bool RemoveMedia(MediaItem item)
    {
        try
        {
            if (File.Exists(item.FilePath)) File.Delete(item.FilePath);
            MediaItems.Remove(item);
            RaiseChanged();
            return true;
        }
        catch { return false; }
    }

    /// <summary>
    /// Bitmap 转 base64 data URI
    /// </summary>
    private static string BitmapToBase64(Bitmap src, int maxWidth, int maxHeight)
    {
        float ratio = Math.Min((float)maxWidth / src.Width, (float)maxHeight / src.Height);
        if (ratio > 1) ratio = 1;
        int w = Math.Max(1, (int)(src.Width * ratio));
        int h = Math.Max(1, (int)(src.Height * ratio));

        using var thumb = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(thumb);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.DrawImage(src, 0, 0, w, h);

        using var ms = new MemoryStream();
        thumb.Save(ms, ImageFormat.Png);
        return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
    }
}
