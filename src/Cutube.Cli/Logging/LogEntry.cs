namespace Cutube.Logging;

public class LogEntry : ILogEntry
{
    public DateTime Timestamp { get; init; }
    public LogLevel Level { get; init; }
    public string Message { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
    public Dictionary<string, object> Context { get; init; } = new();
}
