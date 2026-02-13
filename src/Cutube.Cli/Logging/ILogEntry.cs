namespace Cutube.Cli.Logging;

public interface ILogEntry
{
    DateTime Timestamp { get; }
    LogLevel Level { get; }
    string Message { get; }
    Exception? Exception { get; }
    Dictionary<string, object> Context { get; }
}
