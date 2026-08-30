using System.Collections.Concurrent;

namespace ModernTRCC.Core.Services;

/// <summary>
/// 全局日志服务 - 实时推送到 UI
/// </summary>
public class LogService
{
    private readonly ConcurrentQueue<LogEntry> _logs = new();
    private const int MaxLogs = 500;

    public event Action<LogEntry>? OnLog;

    public void Log(string message, string level = "INFO")
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
            Level = level,
            Message = message
        };

        _logs.Enqueue(entry);
        while (_logs.Count > MaxLogs && _logs.TryDequeue(out _)) { }

        OnLog?.Invoke(entry);
    }

    public void Info(string message) => Log(message, "INFO");
    public void Warn(string message) => Log(message, "WARN");
    public void Error(string message) => Log(message, "ERROR");
    public void Success(string message) => Log(message, "SUCCESS");

    public List<LogEntry> GetAll() => _logs.ToList();
}

public class LogEntry
{
    public string Timestamp { get; set; } = "";
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
}
