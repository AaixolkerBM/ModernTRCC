using ModernTRCC.Core.Models;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace ModernTRCC.Core.Services;

/// <summary>
/// 帧预渲染服务
/// 1. 图片预渲染为RGB888并预分块，发送时零CPU开销
/// 2. 视频统一走优化后的流式播放：ffmpeg 边解码边发送，不缓存帧
/// </summary>
public class FramePrerenderService : IDisposable
{
    private readonly LcdController _controller;
    private readonly Dictionary<string, PreRenderedFrame> _imageCache = new();
    private readonly object _cacheLock = new();
    private long _totalCacheSize;
    private long _maxCacheSize = 1024 * 1024 * 1024; // 1GB

    public long MaxCacheSize
    {
        get => _maxCacheSize;
        set => _maxCacheSize = Math.Max(64 * 1024 * 1024, value);
    }

    public long CurrentCacheSize => _totalCacheSize;

    public int ImageCacheCount => _imageCache.Count;

    public FramePrerenderService(LcdController controller)
    {
        _controller = controller;
    }

    #region 图片预渲染

    /// <summary>
    /// 预渲染图片 - 转换为RGB565并预分块
    /// 结果缓存，重复使用时零开销
    /// </summary>
    public PreRenderedFrame? PrerenderImage(string imagePath,
        int brightness = 100, int contrast = 100, int saturation = 100,
        int rotation = 0, string fitMode = "cover")
    {
        if (_controller.ActiveDeviceInfo == null) return null;

        int width = _controller.ActiveDeviceInfo.Width;
        int height = _controller.ActiveDeviceInfo.Height;

        string cacheKey = $"{imagePath}|{width}x{height}|{brightness}|{contrast}|{saturation}|{rotation}|{fitMode}";

        lock (_cacheLock)
        {
            if (_imageCache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        // 同一文件以不同设置重新预渲染时，先释放旧缓存，避免内存不断叠加
        RemoveCacheForFile(imagePath);

        try
        {
            using var bmp = new Bitmap(imagePath);
            var rgb565 = RenderBitmapToRgb565(bmp, width, height,
                brightness, contrast, saturation, rotation, fitMode);

            var frame = new PreRenderedFrame(width, height, rgb565);
            frame.PreChunk(GetOptimalChunkSize()); // 预分块

            AddToCache(cacheKey, frame);
            return frame;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 从 Bitmap 对象预渲染
    /// </summary>
    public PreRenderedFrame? PrerenderBitmap(Bitmap bitmap,
        int brightness = 100, int contrast = 100, int saturation = 100,
        int rotation = 0, string fitMode = "cover")
    {
        if (_controller.ActiveDeviceInfo == null) return null;

        int width = _controller.ActiveDeviceInfo.Width;
        int height = _controller.ActiveDeviceInfo.Height;

        var rgb565 = RenderBitmapToRgb565(bitmap, width, height,
            brightness, contrast, saturation, rotation, fitMode);

        var frame = new PreRenderedFrame(width, height, rgb565);
        frame.PreChunk(GetOptimalChunkSize());

        return frame;
    }

    #endregion

    #region 视频流式播放

    /// <summary>
    /// 流式播放：ffmpeg 边解码边发送到屏幕，不缓存任何帧。
    /// 按目标帧率节流发送；循环播放时用 -stream_loop -1 在单个 ffmpeg 进程内无限循环，
    /// 避免每轮重启进程导致的中断/黑屏，后台运行时也能稳定循环。
    /// </summary>
    public async Task<bool> StreamVideoAsync(string videoPath,
        int targetFps = 30,
        int brightness = 100, int contrast = 100, int saturation = 100,
        int rotation = 0, string fitMode = "cover",
        bool loop = false,
        CancellationToken cancellationToken = default)
    {
        if (_controller.ActiveDeviceInfo == null) return false;

        int width = _controller.ActiveDeviceInfo.Width;
        int height = _controller.ActiveDeviceInfo.Height;
        int frameDelay = Math.Max(1, 1000 / Math.Max(1, targetFps));

        try
        {
            int totalSent = 0;
            await foreach (var rgb24 in ExtractVideoFramesAsync(
                videoPath, width, height, targetFps, cancellationToken, rotation, loop))
            {
                var rgb888 = LcdController.Rgb24ToRgb888(rgb24, width, height, brightness, contrast, saturation);

                // 队列满（上一帧尚未发送完）时短暂等待；若一直无法入队则丢弃该帧继续，
                // 绝不因瞬时 USB 拥塞中止整个播放（否则后台/隐藏窗口期间会黑屏）
                int wait = 0;
                while (!_controller.SendFrameRgb888(rgb888))
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    if (++wait > 40) break; // 等待 ~200ms 后丢弃本帧
                    await Task.Delay(5, cancellationToken);
                }
                if (cancellationToken.IsCancellationRequested) return totalSent > 0;
                if (wait > 40) continue; // 丢弃本帧，继续下一帧

                totalSent++;
                await Task.Delay(frameDelay, cancellationToken);
            }

            return totalSent > 0;
        }
        catch (OperationCanceledException)
        {
            return true;
        }
    }

    #endregion

    #region 渲染核心

    private byte[] RenderBitmapToRgb565(Bitmap source, int targetWidth, int targetHeight,
        int brightness, int contrast, int saturation, int rotation, string fitMode)
    {
        // 1. 缩放和裁剪
        using var resized = ResizeBitmap(source, targetWidth, targetHeight, fitMode);

        // 2. 旋转
        using var rotated = rotation != 0 ? RotateBitmap(resized, rotation) : new Bitmap(resized);

        // 3. 转换为 RGB888（原版 TRCC 使用 RGB888，不是 RGB565）
        var rect = new Rectangle(0, 0, rotated.Width, rotated.Height);
        var bmpData = rotated.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        int byteCount = bmpData.Stride * bmpData.Height;
        byte[] rgba = new byte[byteCount];
        System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, rgba, 0, byteCount);
        rotated.UnlockBits(bmpData);

        // 输出 RGB888
        return LcdController.ArgbToRgb888(rgba, rotated.Width, rotated.Height,
            brightness, contrast, saturation);
    }

    private static Bitmap ResizeBitmap(Bitmap source, int targetWidth, int targetHeight, string fitMode)
    {
        Rectangle destRect;

        switch (fitMode)
        {
            case "cover":
                // 等比缩放填满，超出部分裁剪
                float ratio = Math.Max((float)targetWidth / source.Width, (float)targetHeight / source.Height);
                int w = (int)(source.Width * ratio);
                int h = (int)(source.Height * ratio);
                destRect = new Rectangle(
                    (targetWidth - w) / 2,
                    (targetHeight - h) / 2,
                    w, h);
                break;

            case "contain":
                // 等比缩放完整显示
                ratio = Math.Min((float)targetWidth / source.Width, (float)targetHeight / source.Height);
                w = (int)(source.Width * ratio);
                h = (int)(source.Height * ratio);
                destRect = new Rectangle(
                    (targetWidth - w) / 2,
                    (targetHeight - h) / 2,
                    w, h);
                break;

            case "fill":
            default:
                // 拉伸填满
                destRect = new Rectangle(0, 0, targetWidth, targetHeight);
                break;
        }

        var result = new Bitmap(targetWidth, targetHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(result);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.DrawImage(source, destRect);

        return result;
    }

    private static Bitmap RotateBitmap(Bitmap source, int angle)
    {
        var result = new Bitmap(source.Width, source.Height);
        using var g = Graphics.FromImage(result);
        g.TranslateTransform((float)source.Width / 2, (float)source.Height / 2);
        g.RotateTransform(angle);
        g.TranslateTransform(-(float)source.Width / 2, -(float)source.Height / 2);
        g.DrawImage(source, 0, 0);
        return result;
    }

    #endregion

    #region FFmpeg 视频帧提取

    private async IAsyncEnumerable<byte[]> ExtractVideoFramesAsync(
        string videoPath, int width, int height, int fps,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct,
        int rotation = 0, bool loop = false)
    {
        string ffmpegPath = FindFFmpeg();
        if (string.IsNullOrEmpty(ffmpegPath))
            yield break;

        // 屏幕旋转：在 ffmpeg 阶段用 transpose 旋转，再 scale 到设备分辨率
        // （transpose 会交换宽高，随后的 scale 归一化，方形/非方形屏均正确）
        string rotationFilter = rotation switch
        {
            90 => "transpose=3",       // 顺时针 90°
            180 => "transpose=3,transpose=3", // 180°
            270 => "transpose=2",      // 逆时针 90° = 顺时针 270°
            _ => ""
        };
        string vf = $"fps={fps}";
        if (!string.IsNullOrEmpty(rotationFilter)) vf += "," + rotationFilter;
        vf += $",scale={width}:{height}:flags=lanczos";

        // 循环播放时用 -stream_loop -1 让 ffmpeg 在单个进程内无限循环（选项须在 -i 之前），
        // 相比每轮重启进程更稳定，后台运行也不中断
        string loopArg = loop ? "-stream_loop -1 " : "";

        // 使用 ffmpeg 输出原始 RGB24 帧（逐帧流式读取，不缓存全部帧）
        // -loglevel error：仅输出错误，避免长时间运行时 stderr 管道被日志填满阻塞进程
        var arguments = $"-loglevel error {loopArg}-i \"{videoPath}\" " +
                       $"-vf \"{vf}\" " +
                       $"-f rawvideo -pix_fmt rgb24 -";

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = System.Diagnostics.Process.Start(psi);
        if (process == null) yield break;

        int frameSize = width * height * 3; // RGB24
        byte[] frameBuffer = new byte[frameSize];
        long frameIndex = 0;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                int bytesRead = 0;
                while (bytesRead < frameSize)
                {
                    int read = await process.StandardOutput.BaseStream.ReadAsync(
                        frameBuffer.AsMemory(bytesRead, frameSize - bytesRead), ct);
                    if (read == 0)
                        yield break; // 流结束

                    bytesRead += read;
                }

                // 返回帧数据的副本（因为 buffer 会被复用）
                byte[] frameData = new byte[frameSize];
                Buffer.BlockCopy(frameBuffer, 0, frameData, 0, frameSize);

                yield return frameData;
                frameIndex++;
            }
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                    process.Kill();
            }
            catch { }
        }
    }

    private static string? FindFFmpeg()
    {
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        // 1. ffmpeg/ 子文件夹
        string subPath = Path.Combine(appDir, "ffmpeg", "ffmpeg.exe");
        if (File.Exists(subPath)) return subPath;
        // 2. 程序目录
        string localPath = Path.Combine(appDir, "ffmpeg.exe");
        if (File.Exists(localPath)) return localPath;
        // 3. PATH 环境变量
        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv != null)
        {
            foreach (var dir in pathEnv.Split(';'))
            {
                string ffmpeg = Path.Combine(dir.Trim(), "ffmpeg.exe");
                if (File.Exists(ffmpeg)) return ffmpeg;
            }
        }
        return null;
    }

    #endregion

    #region 缓存管理

    /// <summary>
    /// 移除某个媒体文件的预渲染图片缓存，并精确扣减已统计的缓存大小。
    /// 设置变化后重新渲染同一文件时调用，防止旧缓存持续占用内存。
    /// </summary>
    public void RemoveCacheForFile(string filePath)
    {
        lock (_cacheLock)
        {
            // 缓存键格式为 "文件路径|参数..."，用 "路径|" 前缀精确匹配，
            // 避免误删路径以该文件名为前缀的其他文件（如 foo.jpg 与 foo.jpg (1)）
            string prefix = filePath + "|";
            var imageKeys = _imageCache.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
            foreach (var key in imageKeys)
            {
                _totalCacheSize -= GetFrameSize(_imageCache[key]);
                _imageCache.Remove(key);
            }
        }
    }

    private static long GetFrameSize(PreRenderedFrame frame)
    {
        long size = frame.Data.Length;
        if (frame.Chunks != null)
            size += frame.Chunks.Length * 64;
        return size;
    }

    private void AddToCache(string key, PreRenderedFrame frame)
    {
        lock (_cacheLock)
        {
            long size = GetFrameSize(frame);

            // 检查是否需要清理
            while (_totalCacheSize + size > _maxCacheSize && _imageCache.Count > 0)
            {
                var first = _imageCache.First();
                _totalCacheSize -= GetFrameSize(first.Value);
                _imageCache.Remove(first.Key);
            }

            _imageCache[key] = frame;
            _totalCacheSize += size;
        }
    }

    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _imageCache.Clear();
            _totalCacheSize = 0;
        }
    }

    #endregion

    #region 辅助方法

    private int GetOptimalChunkSize()
    {
        if (_controller.ActiveDeviceInfo == null) return 4096;

        return _controller.ActiveDeviceInfo.Type switch
        {
            LcdDeviceType.Scsi => 0x10000,  // 64 KiB
            LcdDeviceType.Bulk => 16384,    // 16 KiB
            LcdDeviceType.Hid => 63,        // 63 bytes (64 - 1 report ID)
            LcdDeviceType.Ly => 496,        // 496 bytes (512 - 16 header)
            LcdDeviceType.Ly1 => 496,
            _ => 4096,
        };
    }

    #endregion

    public void Dispose()
    {
        ClearCache();
    }
}
