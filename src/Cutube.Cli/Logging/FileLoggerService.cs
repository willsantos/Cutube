using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Cutube.Cli.Logging;

namespace Cutube.Cli.Logging;

public class FileLoggerService : ILoggerService, IDisposable
{
    private readonly Serilog.Core.Logger _logger;
    private readonly string _logBasePath;

    public FileLoggerService(IEnvironmentService environmentService)
    {
        _logBasePath = Path.Combine(
            environmentService.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Cutube",
            "logs"
        );

        Directory.CreateDirectory(_logBasePath);

        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                new CompactJsonFormatter(),
                Path.Combine(_logBasePath, "cutube-.log"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 10_000_000,
                retainedFileCountLimit: 7,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1)
            )
            .CreateLogger();
    }

    public void LogDebug(string message, params (string key, object value)[] context)
    {
        WriteLog(LogEventLevel.Debug, null, message, context);
    }

    public void LogInfo(string message, params (string key, object value)[] context)
    {
        WriteLog(LogEventLevel.Information, null, message, context);
    }

    public void LogWarning(string message, params (string key, object value)[] context)
    {
        WriteLog(LogEventLevel.Warning, null, message, context);
    }

    public void LogError(Exception exception, string message, params (string key, object value)[] context)
    {
        WriteLog(LogEventLevel.Error, exception, message, context);
    }

    public void LogCritical(Exception exception, string message, params (string key, object value)[] context)
    {
        WriteLog(LogEventLevel.Fatal, exception, message, context);
    }

    private void WriteLog(LogEventLevel level, Exception? exception, string message, (string key, object value)[] context)
    {
        if (context.Length == 0)
        {
            _logger.Write(level, exception, message);
        }
        else
        {
            var contextDict = context.ToDictionary(x => x.key, x => x.value);
            _logger.Write(level, exception, "{@Message} {@Context}", new { Message = message, Context = contextDict });
        }
    }

    public void Dispose()
    {
        _logger?.Dispose();
    }
}
