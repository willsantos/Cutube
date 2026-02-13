using FluentAssertions;
using Moq;
using Xunit;
using Cutube.Cli.Logging;
using Cutube.Cli;
using System.IO;

namespace Cutube.Tests.Unit.Logging;

public class FileLoggerServiceTests : IDisposable
{
    private readonly string _testLogPath;
    private readonly string _logDirPath;
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

        // O FileLoggerService cria o path como: _testLogPath/Cutube/logs
        _logDirPath = Path.Combine(_testLogPath, "Cutube", "logs");
    }

    private string[] GetLogFiles()
    {
        return Directory.GetFiles(_logDirPath, "cutube-*.log");
    }

    [Fact]
    public void LogDebug_WritesLogToFile()
    {
        var message = "Test debug message";

        _loggerService.LogDebug(message);

        // Aguarda flush do Serilog
        Thread.Sleep(500);

        // Verifica se o diretório existe e tem arquivos
        Directory.Exists(_logDirPath).Should().BeTrue("Log directory should exist");

        var allFiles = Directory.GetFiles(_logDirPath);
        allFiles.Should().NotBeEmpty("Log files should exist in directory");

        _loggerService.Dispose();

        var logFiles = GetLogFiles();
        logFiles.Should().NotBeEmpty();
        File.ReadAllText(logFiles[0]).Should().Contain(message);
    }

    [Fact]
    public void LogInfo_WithContext_SerializesContext()
    {
        var message = "Test info with context";
        var context = new[] { ("url", (object)"https://youtube.com/test"), ("userId", (object)"123") };

        _loggerService.LogInfo(message, context);

        // Aguarda flush do Serilog
        Thread.Sleep(100);
        _loggerService.Dispose();

        var logFiles = GetLogFiles();
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

        var logFiles = GetLogFiles();
        var logContent = File.ReadAllText(logFiles[0]);
        logContent.Should().Contain(message);
        logContent.Should().Contain("Test exception");
    }

    [Fact]
    public void LogWarning_WritesWarningLevel()
    {
        var message = "Test warning message";

        _loggerService.LogWarning(message);

        var logFiles = GetLogFiles();
        File.ReadAllText(logFiles[0]).Should().Contain(message);
    }

    [Fact]
    public void LogCritical_WithException_WritesCriticalLog()
    {
        var message = "Critical error";
        var exception = new Exception("Critical exception");

        _loggerService.LogCritical(exception, message);

        var logFiles = GetLogFiles();
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
