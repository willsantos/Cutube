using FluentAssertions;
using Moq;
using Xunit;
using Cutube.Logging;
using cutube;
using System.IO;

namespace Cutube.Tests.Unit.Logging;

public class FileLoggerServiceTests : IDisposable
{
    private readonly string _testLogPath;
    private readonly FileLoggerService _loggerService;
    private readonly Mock<IEnvironmentService> _mockEnvironment;

    public FileLoggerServiceTests()
    {
        _testLogPath = Path.Combine(Path.GetTempPath(), $"cutube_tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testLogPath);

        _mockEnvironment = new Mock<IEnvironmentService>();
        _mockEnvironment.Setup(e => e.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData))
            .Returns(_testLogPath);

        _loggerService = new FileLoggerService(_mockEnvironment.Object);
    }

    [Fact]
    public void LogDebug_WritesLogToFile()
    {
        var message = "Test debug message";
        
        _loggerService.LogDebug(message);

        var logFiles = Directory.GetFiles(_testLogPath, "cutube-*.log");
        logFiles.Should().NotBeEmpty();
        File.ReadAllText(logFiles[0]).Should().Contain(message);
    }

    [Fact]
    public void LogInfo_WithContext_SerializesContext()
    {
        var message = "Test info with context";
        var context = new[] { ("url", "https://youtube.com/test"), ("userId", "123") };

        _loggerService.LogInfo(message, context);

        var logFiles = Directory.GetFiles(_testLogPath, "cutube-*.log");
        var logContent = File.ReadAllText(logFiles[0]);
        logContent.Should().Contain(message);
        logContent.Should().Contain("url");
        logContent.Should().Contain("https://youtube.com/test");
    }

    [Fact]
    public void LogError_WithException_IncludesStackTrace()
    {
        var message = "Test error message";
        var exception = new Exception("Test exception");

        _loggerService.LogError(exception, message);

        var logFiles = Directory.GetFiles(_testLogPath, "cutube-*.log");
        var logContent = File.ReadAllText(logFiles[0]);
        logContent.Should().Contain(message);
        logContent.Should().Contain("Test exception");
    }

    [Fact]
    public void LogWarning_WritesWarningLevel()
    {
        var message = "Test warning message";

        _loggerService.LogWarning(message);

        var logFiles = Directory.GetFiles(_testLogPath, "cutube-*.log");
        File.ReadAllText(logFiles[0]).Should().Contain(message);
    }

    [Fact]
    public void LogCritical_WithException_WritesCriticalLog()
    {
        var message = "Critical error";
        var exception = new Exception("Critical exception");

        _loggerService.LogCritical(exception, message);

        var logFiles = Directory.GetFiles(_testLogPath, "cutube-*.log");
        var logContent = File.ReadAllText(logFiles[0]);
        logContent.Should().Contain(message);
        logContent.Should().Contain("Critical exception");
    }

    [Fact]
    public void Dispose_CleanlyDisposesLogger()
    {
        var loggerService = new FileLoggerService(_mockEnvironment.Object);
        
        var act = () => loggerService.Dispose();
        
        act.Should().NotThrow();
    }

    public void Dispose()
    {
        _loggerService?.Dispose();
        if (Directory.Exists(_testLogPath))
        {
            try
            {
                Directory.Delete(_testLogPath, true);
            }
            catch
            {
            }
        }
    }
}
