using Cutube.Logging;
using Cutube.Recovery;
using Moq;
using Xunit;

namespace Cutube.Tests.Unit.Recovery;

public class StateCleanupServiceTests
{
    private readonly Mock<IDownloadStateManager> _stateManagerMock;
    private readonly Mock<ILoggerService> _loggerMock;
    private readonly StateCleanupService _cleanupService;

    public StateCleanupServiceTests()
    {
        _stateManagerMock = new Mock<IDownloadStateManager>();
        _loggerMock = new Mock<ILoggerService>();
        _cleanupService = new StateCleanupService(_stateManagerMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CleanupAsync_UsesDefaultMaxAge_WhenNotSpecified()
    {
        // Arrange
        _stateManagerMock
            .Setup(m => m.CleanupOldStatesAsync(TimeSpan.FromDays(7)))
            .ReturnsAsync(5);

        // Act
        var result = await _cleanupService.CleanupAsync();

        // Assert
        Assert.Equal(5, result.RemovedStatesCount);
        Assert.Equal(TimeSpan.FromDays(7), result.MaxAge);
        _stateManagerMock.Verify(m => m.CleanupOldStatesAsync(TimeSpan.FromDays(7)), Times.Once);
    }

    [Fact]
    public async Task CleanupAsync_UsesCustomMaxAge_WhenSpecified()
    {
        // Arrange
        var customAge = TimeSpan.FromDays(14);
        _stateManagerMock
            .Setup(m => m.CleanupOldStatesAsync(customAge))
            .ReturnsAsync(3);

        // Act
        var result = await _cleanupService.CleanupAsync(customAge);

        // Assert
        Assert.Equal(3, result.RemovedStatesCount);
        Assert.Equal(customAge, result.MaxAge);
        _stateManagerMock.Verify(m => m.CleanupOldStatesAsync(customAge), Times.Once);
    }

    [Fact]
    public async Task CleanupAsync_ReturnsCorrectResult()
    {
        // Arrange
        _stateManagerMock
            .Setup(m => m.CleanupOldStatesAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync(10);

        // Act
        var result = await _cleanupService.CleanupAsync();

        // Assert
        Assert.Equal(10, result.RemovedStatesCount);
        Assert.True(result.ExecutedAt <= DateTime.UtcNow);
        Assert.True(result.ExecutedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task CleanupOnStartupAsync_DoesNotThrow_WhenCleanupFails()
    {
        // Arrange
        _stateManagerMock
            .Setup(m => m.CleanupOldStatesAsync(It.IsAny<TimeSpan>()))
            .ThrowsAsync(new Exception("Cleanup failed"));

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await _cleanupService.CleanupOnStartupAsync());

        // Assert
        Assert.Null(exception);
        _loggerMock.Verify(l => l.LogError(
            It.IsAny<Exception>(),
            "Startup cleanup failed (non-fatal)"
        ), Times.Once);
    }

    [Fact]
    public async Task CleanupOnStartupAsync_LogsSuccess_WhenCleanupSucceeds()
    {
        // Arrange
        _stateManagerMock
            .Setup(m => m.CleanupOldStatesAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync(7);

        // Act
        await _cleanupService.CleanupOnStartupAsync();

        // Assert
        _loggerMock.Verify(l => l.LogInfo(
            "Startup cleanup completed",
            It.IsAny<(string key, object value)[]>()
        ), Times.Once);
    }
}
