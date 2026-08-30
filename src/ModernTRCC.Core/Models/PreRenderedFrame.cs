namespace ModernTRCC.Core.Models;

/// <summary>
/// 预渲染帧缓存 - 预编译好的RGB565数据，可直接发送到屏幕
/// </summary>
public class PreRenderedFrame : IDisposable
{
    /// <summary>
    /// 帧数据 (RGB565 big-endian)
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// 帧宽度
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// 帧高度
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// 帧时长（毫秒），用于视频/动画
    /// </summary>
    public int DurationMs { get; set; } = 33;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.Now;

    /// <summary>
    /// 分块传输的预分块数据（性能优化）
    /// 预先计算好每个块的数据，发送时无需再分块
    /// </summary>
    public byte[][]? Chunks { get; private set; }

    /// <summary>
    /// 块大小
    /// </summary>
    public int ChunkSize { get; private set; }

    public PreRenderedFrame(int width, int height)
    {
        Width = width;
        Height = height;
        Data = new byte[width * height * 2];
    }

    public PreRenderedFrame(int width, int height, byte[] rgb565Data)
    {
        Width = width;
        Height = height;
        Data = rgb565Data;
    }

    /// <summary>
    /// 预分块 - 提前将帧数据分割为传输块
    /// 这是关键性能优化：避免每次发送时都重新分块
    /// </summary>
    public void PreChunk(int chunkSize)
    {
        if (chunkSize <= 0) return;

        ChunkSize = chunkSize;
        int totalChunks = (int)Math.Ceiling((double)Data.Length / chunkSize);
        Chunks = new byte[totalChunks][];

        for (int i = 0; i < totalChunks; i++)
        {
            int offset = i * chunkSize;
            int size = Math.Min(chunkSize, Data.Length - offset);
            byte[] chunk = new byte[size];
            Buffer.BlockCopy(Data, offset, chunk, 0, size);
            Chunks[i] = chunk;
        }
    }

    public void Dispose()
    {
        // 大对象优化：可以考虑使用 ArrayPool
        Chunks = null;
    }
}
