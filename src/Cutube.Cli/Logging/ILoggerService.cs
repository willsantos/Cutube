namespace Cutube.Cli.Logging;

public interface ILoggerService
{
    void LogDebug(string message, params (string key, object value)[] context);
    void LogInfo(string message, params (string key, object value)[] context);
    void LogWarning(string message, params (string key, object value)[] context);
    void LogError(Exception exception, string message, params (string key, object value)[] context);
    void LogCritical(Exception exception, string message, params (string key, object value)[] context);
}
