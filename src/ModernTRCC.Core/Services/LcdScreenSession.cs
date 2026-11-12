using ModernTRCC.Core.Models;
using System.Drawing;
using System.Drawing.Imaging;

namespace ModernTRCC.Core.Services;

/// <summary>
/// 单块屏幕的完整会话：独立的 USB 控制器、帧预渲染器、播放状态与屏幕级设置。
/// 两块屏幕各自拥有一个会话实例，实现「完全分离的管理」。
/// </summary>
public class LcdScreenSession : IDisposable
{
    public int Index { get; }
    public ScreenSettings Settings { get; }
    public LcdController Controller { get; }
    public FramePrerenderService Prerender { get; }

    /// <summary>设备标识（VID:PID），用于设置与物理屏幕的稳定绑定</summary>
    public string DeviceKey => Settings.DeviceKey ?? "";

    private readonly LogService _log;
    private CancellationTokenSource? _playbackCts;
    private Task? _playbackTask;

    public string? CurrentMediaId { get; private set; }

    /// <summary>当前显示的测试图案（显示设置变化后按新参数重绘）</summary>
    private string? _currentTestPattern;
    public bool CurrentlyPlaying { get; private set; }
    public int CurrentPlayFps { get; private set; }

    /// <summary>播放状态变化（UI 推送 playbackStatus 用）</summary>
    public event EventHandler? StateChanged;

    /// <summary>播放过程事件（UI 推送 toast 用）</summary>
    public event EventHandler<PlaybackNotice>? Notice;

    public LcdScreenSession(int index, ScreenSettings settings, LogService log)
    {
        Index = index;
        Settings = settings;
        _log = log;
        Controller = new LcdController();
        Prerender = new FramePrerenderService(Controller);
    }

    public string DisplayName => $"{Settings.Name}";

    private void FireStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
    private void FireNotice(PlaybackNoticeType type, string? detail = null)
        => Notice?.Invoke(this, new PlaybackNotice(type, detail));

    // ==================== 播放 ====================

    /// <summary>播放指定媒体（图片/视频），自动停止本屏上一轮播放</summary>
    public void PlayMedia(string mediaId, MediaLibraryService mediaLib)
    {
        var media = mediaLib.MediaItems.FirstOrDefault(m => m.Id == mediaId);
        if (media == null)
        {
            _log.Error($"[屏幕 {Index + 1}] 媒体不存在: {mediaId}");
            return;
        }
        if (Controller.ActiveDeviceInfo == null)
        {
            _log.Error($"[屏幕 {Index + 1}] 设备未连接");
            FireNotice(PlaybackNoticeType.DeviceNotConnected);
            return;
        }

        _currentTestPattern = null;

        // 停止上一轮播放，确保同一屏幕同时只有一个播放循环（否则会越来越快）
        _playbackCts?.Cancel();
        _playbackCts?.Dispose();
        var cts = new CancellationTokenSource();
        _playbackCts = cts;
        var token = cts.Token;
        CurrentMediaId = mediaId;

        int playFps = Settings.Playback.VideoFps;
        CurrentlyPlaying = true;
        CurrentPlayFps = playFps;
        FireStateChanged();
        FireNotice(PlaybackNoticeType.Started, media.Name);

        _playbackTask = Task.Run(async () =>
        {
            try
            {
                if (media.Type == MediaType.Image)
                {
                    // 图片播放：按屏幕分辨率缩放/裁剪/旋转并应用图像调整，再发送
                    Controller.ClearFrameQueue(); // 清空上一轮残留视频帧，避免发送失败

                    var frame = Prerender.PrerenderImage(
                        media.FilePath,
                        Settings.Display.Brightness,
                        Settings.Display.Contrast,
                        Settings.Display.Saturation,
                        Settings.Display.Rotation,
                        Settings.Playback.FitMode);

                    if (frame != null && !token.IsCancellationRequested)
                    {
                        // 静态帧：发送成功后持续显示，保持播放状态
                        if (Controller.SendFrameRgb888(frame.Data))
                        {
                            _log.Success($"[屏幕 {Index + 1}] 图片已显示: {media.Name}");
                        }
                        else
                        {
                            _log.Error($"[屏幕 {Index + 1}] 图片发送失败（队列繁忙）");
                            FireNotice(PlaybackNoticeType.SendFailed);
                            SetIdle();
                        }
                    }
                    else
                    {
                        _log.Error($"[屏幕 {Index + 1}] 图片渲染失败");
                        FireNotice(PlaybackNoticeType.Failed);
                        SetIdle();
                    }
                }
                else
                {
                    // 视频统一走优化后的流式播放：ffmpeg 边解码边发送，不缓存帧
                    bool ok = await Prerender.StreamVideoAsync(
                        media.FilePath, playFps,
                        Settings.Display.Brightness,
                        Settings.Display.Contrast,
                        Settings.Display.Saturation,
                        Settings.Display.Rotation,
                        Settings.Playback.FitMode,
                        Settings.Playback.LoopVideo,
                        token);

                    if (token.IsCancellationRequested) return;

                    if (ok)
                    {
                        _log.Info($"[屏幕 {Index + 1}] 视频播放完成");
                        FireNotice(PlaybackNoticeType.Completed);
                    }
                    else
                    {
                        _log.Error($"[屏幕 {Index + 1}] 视频播放失败");
                        FireNotice(PlaybackNoticeType.Failed);
                    }
                    SetIdle();
                }
            }
            catch (OperationCanceledException)
            {
                // 被清除屏幕或新播放取消
            }
            catch (Exception ex)
            {
                _log.Error($"[屏幕 {Index + 1}] 播放失败: {ex.Message}");
                FireNotice(PlaybackNoticeType.Failed);
                SetIdle();
            }
        });
    }

    private void SetIdle()
    {
        CurrentlyPlaying = false;
        CurrentPlayFps = 0;
        FireStateChanged();
    }

    /// <summary>停止播放并清空屏幕（发送黑色帧）</summary>
    public void ClearDisplay()
    {
        _currentTestPattern = null;
        _playbackCts?.Cancel();
        _playbackCts?.Dispose();
        _playbackCts = null;
        CurrentMediaId = null;
        Controller.ClearFrameQueue(); // 清空残留的视频帧，让黑色帧立即生效
        SetIdle();

        if (Controller.ActiveDeviceInfo == null) return;
        int w = Controller.ActiveDeviceInfo.Width;
        int h = Controller.ActiveDeviceInfo.Height;
        var rgb888 = new byte[w * h * 3];
        Controller.SendFrameRgb888(rgb888);
        _log.Info($"[屏幕 {Index + 1}] 屏幕已清除，播放已停止");
    }

    /// <summary>发送测试图案到本屏</summary>
    public void SendTestPattern(string pattern)
    {
        if (Controller.ActiveDeviceInfo == null)
        {
            _log.Error($"[屏幕 {Index + 1}] 设备未连接，无法发送测试图案");
            FireNotice(PlaybackNoticeType.DeviceNotConnected);
            return;
        }

        int w = Controller.ActiveDeviceInfo.Width;
        int h = Controller.ActiveDeviceInfo.Height;
        _currentTestPattern = pattern;
        var display = Settings.Display;
        int rotation = display.Rotation;
        bool quarterTurn = rotation is 90 or 270;
        _log.Info($"[屏幕 {Index + 1}] 发送测试图案: {pattern} ({w}x{h}, 旋转 {rotation}°)");

        Task.Run(() =>
        {
            try
            {
                // 先按可见方向生成，再走与图片相同的旋转/缩放管线，保证矩形屏不裁切
                using var bmp = GenerateTestPattern(
                    quarterTurn ? h : w, quarterTurn ? w : h, pattern);
                using var rendered = Prerender.PrerenderBitmap(
                    bmp, display.Brightness, display.Contrast, display.Saturation,
                    rotation, "fill")
                    ?? throw new InvalidOperationException("测试图案渲染失败");
                bool sent = Controller.SendFrameRgb888(rendered.Data);
                if (sent)
                {
                    _log.Success($"[屏幕 {Index + 1}] 测试图案已发送");
                    FireNotice(PlaybackNoticeType.PatternSent);
                }
                else
                {
                    _log.Error($"[屏幕 {Index + 1}] 发送失败 - 设备未运行或队列已满");
                    FireNotice(PlaybackNoticeType.SendFailed);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"[屏幕 {Index + 1}] 测试图案异常: {ex.Message}");
                FireNotice(PlaybackNoticeType.Failed);
            }
        });
    }

    /// <summary>
    /// 应用显示设置：先停掉旧播放循环并等待退出，再清除本屏缓存，
    /// 最后按新参数立即重新渲染当前媒体，避免黑屏。
    /// </summary>
    public async Task ApplyDisplaySettingsAsync(MediaLibraryService mediaLib)
    {
        Controller.TargetFps = Settings.Display.TargetFps;

        _playbackCts?.Cancel();
        if (_playbackTask != null)
        {
            try { await _playbackTask; } catch { }
        }

        // 亮度/对比度/饱和度/旋转/缩放变化会使旧图片缓存失效，清除以便下次按新参数重渲
        Prerender.ClearCache();
        _log.Info($"[屏幕 {Index + 1}] 显示设置已应用");

        if (!string.IsNullOrEmpty(CurrentMediaId) && Controller.ActiveDeviceInfo != null)
        {
            PlayMedia(CurrentMediaId!, mediaLib);
        }
        else if (_currentTestPattern != null && Controller.IsConnected)
        {
            // 当前显示的是测试图案：按新参数重绘
            SendTestPattern(_currentTestPattern);
        }
    }

    /// <summary>
    /// 开机自动播放：本屏设备已连接且媒体库就绪时播放配置的媒体。
    /// 每次运行只触发一次；媒体库尚未加载完成时静默等待下一次变更事件。
    /// </summary>
    public void TryStartAutoPlay(MediaLibraryService mediaLib)
    {
        try
        {
            if (_autoPlayTriggered) return;
            if (Controller.ActiveDeviceInfo == null) return;

            if (!Settings.AutoPlay.Enabled || string.IsNullOrEmpty(Settings.AutoPlay.MediaId)) return;

            var media = mediaLib.MediaItems.FirstOrDefault(m => m.Id == Settings.AutoPlay.MediaId);
            if (media == null) return; // 媒体库尚未加载完成，等待后续事件

            _autoPlayTriggered = true;
            _log.Info($"[屏幕 {Index + 1}] 开机自动播放: {media.Name}");
            PlayMedia(media.Id, mediaLib);
        }
        catch (Exception ex)
        {
            _log.Error($"[屏幕 {Index + 1}] 自动播放失败: {ex.Message}");
        }
    }
    private bool _autoPlayTriggered;

    /// <summary>重置自动播放触发标记（用户重新启用自动播放时调用）</summary>
    public void ResetAutoPlay() => _autoPlayTriggered = false;

    /// <summary>媒体被删除时调用：若本屏正在播放该媒体则停止并清屏，并释放缓存</summary>
    public void OnMediaDeleted(string mediaId, string filePath)
    {
        if (CurrentMediaId == mediaId)
        {
            _playbackCts?.Cancel();
            _playbackCts?.Dispose();
            _playbackCts = null;
            CurrentMediaId = null;
            Controller.ClearFrameQueue();
            SetIdle();
        }
        Prerender.RemoveCacheForFile(filePath);
    }

    // ==================== 测试图案生成 ====================


    private static Bitmap GenerateTestPattern(int width, int height, string type)
    {
        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);

        switch (type)
        {
            case "colorbars":
            {
                var colors = new[] {
                    Color.White, Color.Yellow, Color.Cyan, Color.Green,
                    Color.Magenta, Color.Red, Color.Blue, Color.Black
                };
                int barW = width / colors.Length;
                int remainder = width - barW * colors.Length;
                for (int i = 0; i < colors.Length; i++)
                using (var brush = new SolidBrush(colors[i]))
                {
                    int w = (i == colors.Length - 1) ? barW + remainder : barW;
                    g.FillRectangle(brush, i * barW, 0, w, height);
                }
                break;
            }
            case "gradient":
            {
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int r = (int)(x * 255.0 / width);
                    int gV = (int)(y * 255.0 / height);
                    bmp.SetPixel(x, y, Color.FromArgb(r, gV, 128));
                }
                break;
            }
            case "grid":
            {
                g.Clear(Color.Black);
                using var pen = new Pen(Color.White);
                for (int x = 0; x < width; x += 20) g.DrawLine(pen, x, 0, x, height);
                for (int y = 0; y < height; y += 20) g.DrawLine(pen, 0, y, width, y);
                break;
            }
        }
        return bmp;
    }

    public void Dispose()
    {
        _playbackCts?.Cancel();
        try { if (_playbackTask != null) { _playbackTask.Wait(1000); } } catch { }
        try { Prerender.Dispose(); } catch { }
        try { Controller.Dispose(); } catch { }
    }
}

public enum PlaybackNoticeType
{
    Started,
    Completed,
    Failed,
    SendFailed,
    PatternSent,
    DeviceNotConnected,
}

public class PlaybackNotice
{
    public PlaybackNoticeType Type { get; }
    public string? Detail { get; }
    public PlaybackNotice(PlaybackNoticeType type, string? detail = null)
    {
        Type = type;
        Detail = detail;
    }
}