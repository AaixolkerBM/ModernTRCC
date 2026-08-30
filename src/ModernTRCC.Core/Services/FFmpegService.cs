using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace ModernTRCC.Core.Services;

/// <summary>
/// FFmpeg 包装服务
/// 负责调用外部 ffmpeg 可执行文件进行视频解码和媒体处理
/// </summary>
public class FFmpegService
{
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;

    public FFmpegService()
    {
        // 搜索 ffmpeg 位置
        var searchPaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "ffmpeg", "ffmpeg.exe"),
            Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin", "ffmpeg.exe"),
            "ffmpeg.exe",
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                _ffmpegPath = path;
                _ffprobePath = Path.Combine(Path.GetDirectoryName(path)!, "ffprobe.exe");
                break;
            }
        }

        _ffmpegPath ??= "ffmpeg.exe";
        _ffprobePath = Path.Combine(Path.GetDirectoryName(_ffmpegPath) ?? "", "ffprobe.exe");
    }

    public bool IsFFmpegAvailable => File.Exists(_ffmpegPath);
    public string FFmpegPath => _ffmpegPath;
    public string FFprobePath => _ffprobePath;

    /// <summary>
    /// 获取视频信息（通过 ffprobe）
    /// </summary>
    public async Task<VideoInfo?> GetVideoInfoAsync(string videoPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(videoPath)) return null;
        if (!File.Exists(_ffprobePath)) return null;

        try
        {
            var args = $"-v error -select_streams v:0 -show_entries stream=width,height,r_frame_rate,duration -of json \"{videoPath}\"";

            var psi = new ProcessStartInfo
            {
                FileName = _ffprobePath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0) return null;

            // 简单解析 JSON
            var widthMatch = System.Text.RegularExpressions.Regex.Match(output, "\"width\"\\s*:\\s*(\\d+)");
            var heightMatch = System.Text.RegularExpressions.Regex.Match(output, "\"height\"\\s*:\\s*(\\d+)");
            var fpsMatch = System.Text.RegularExpressions.Regex.Match(output, "\"r_frame_rate\"\\s*:\\s*\"(\\d+)/(\\d+)\"");
            var durationMatch = System.Text.RegularExpressions.Regex.Match(output, "\"duration\"\\s*:\\s*\"([\\d.]+)\"");

            int width = widthMatch.Success ? int.Parse(widthMatch.Groups[1].Value) : 0;
            int height = heightMatch.Success ? int.Parse(heightMatch.Groups[1].Value) : 0;
            double fps = 30;
            if (fpsMatch.Success && int.Parse(fpsMatch.Groups[2].Value) != 0)
            {
                fps = (double)int.Parse(fpsMatch.Groups[1].Value) / int.Parse(fpsMatch.Groups[2].Value);
            }
            double duration = durationMatch.Success ? double.Parse(durationMatch.Groups[1].Value) : 0;

            return new VideoInfo
            {
                Width = width,
                Height = height,
                Fps = fps,
                DurationSeconds = duration,
                FilePath = videoPath,
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 提取视频帧为 RGB24 原始像素数据
    /// 这是预渲染的核心：直接从 ffmpeg 管道读取原始像素，然后转为 RGB565
    /// </summary>
    public async IAsyncEnumerable<RawFrame> DecodeVideoFramesAsync(
        string videoPath,
        int targetWidth,
        int targetHeight,
        int targetFps = 30,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(videoPath)) yield break;
        if (!IsFFmpegAvailable) yield break;

        // 使用 ffmpeg 输出原始 RGB24 像素
        // 注意：我们直接让 ffmpeg 缩放到目标分辨率，避免在 C# 中缩放（性能优化）
        var args = $"-i \"{videoPath}\" " +
                   $"-vf \"fps={targetFps},scale={targetWidth}:{targetHeight}:flags=lanczos\" " +
                   $"-f rawvideo -pix_fmt rgb24 -";

        var psi = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi);
        if (process == null) yield break;

        var stream = process.StandardOutput.BaseStream;
        int frameSize = targetWidth * targetHeight * 3; // RGB24 = 3 bytes per pixel
        byte[] frameBuffer = new byte[frameSize];
        int frameIndex = 0;
        int frameDurationMs = (int)Math.Round(1000.0 / targetFps);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead = 0;
                while (bytesRead < frameSize)
                {
                    int read = await stream.ReadAsync(
                        frameBuffer.AsMemory(bytesRead, frameSize - bytesRead),
                        cancellationToken);

                    if (read == 0)
                    {
                        // 流结束
                        yield break;
                    }
                    bytesRead += read;
                }

                // 返回帧数据的副本（因为 buffer 会被复用）
                byte[] frameData = new byte[frameSize];
                Buffer.BlockCopy(frameBuffer, 0, frameData, 0, frameSize);

                yield return new RawFrame
                {
                    Width = targetWidth,
                    Height = targetHeight,
                    Data = frameData,
                    PixelFormat = PixelFormat.Rgb24,
                    FrameIndex = frameIndex,
                    DurationMs = frameDurationMs,
                };

                frameIndex++;
            }
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// 获取视频缩略图（第一帧）
    /// </summary>
    public async Task<Bitmap?> GetVideoThumbnailAsync(string videoPath,
        int maxWidth = 320,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(videoPath)) return null;
        if (!IsFFmpegAvailable) return null;

        var info = await GetVideoInfoAsync(videoPath, cancellationToken);
        if (info == null || info.Width == 0) return null;

        int thumbWidth = Math.Min(maxWidth, info.Width);
        int thumbHeight = (int)(thumbWidth * (double)info.Height / info.Width);

        var args = $"-i \"{videoPath}\" -vframes 1 -vf \"scale={thumbWidth}:{thumbHeight}\" -f image2pipe -vcodec bmp -";

        var psi = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null) return null;

            using var ms = new MemoryStream();
            await process.StandardOutput.BaseStream.CopyToAsync(ms, cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0 || ms.Length == 0) return null;

            ms.Position = 0;
            return new Bitmap(ms);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 提取指定时间点的帧
    /// </summary>
    public async Task<Bitmap?> ExtractFrameAsync(string videoPath,
        TimeSpan timestamp,
        int width, int height,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(videoPath)) return null;
        if (!IsFFmpegAvailable) return null;

        var args = $"-ss {timestamp.TotalSeconds} -i \"{videoPath}\" " +
                   $"-vframes 1 -vf \"scale={width}:{height}\" " +
                   $"-f image2pipe -vcodec bmp -";

        var psi = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null) return null;

            using var ms = new MemoryStream();
            await process.StandardOutput.BaseStream.CopyToAsync(ms, cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0 || ms.Length == 0) return null;

            ms.Position = 0;
            return new Bitmap(ms);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// 原始帧数据
/// </summary>
public class RawFrame
{
    public int Width { get; set; }
    public int Height { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public PixelFormat PixelFormat { get; set; }
    public int FrameIndex { get; set; }
    public int DurationMs { get; set; }
}

public enum PixelFormat
{
    Rgb24,
    Bgr24,
    Rgba32,
}

/// <summary>
/// 视频信息
/// </summary>
public class VideoInfo
{
    public int Width { get; set; }
    public int Height { get; set; }
    public double Fps { get; set; }
    public double DurationSeconds { get; set; }
    public string FilePath { get; set; } = string.Empty;

    public string ResolutionText => $"{Width}×{Height}";
    public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);
    public string DurationText => Duration.ToString(@"mm\:ss");
}
