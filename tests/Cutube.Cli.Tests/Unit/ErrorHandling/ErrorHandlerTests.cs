using FluentAssertions;
using Moq;
using Xunit;
using Cutube.Cli.ErrorHandling;
using Cutube.Cli.Logging;
using System.Net;

namespace Cutube.Tests.Unit.ErrorHandling;

public class ErrorHandlerTests
{
    private readonly Mock<ILoggerService> _mockLogger;
    private readonly ErrorHandler _errorHandler;

    public ErrorHandlerTests()
    {
        _mockLogger = new Mock<ILoggerService>();
        _errorHandler = new ErrorHandler(_mockLogger.Object);
    }

    [Fact]
    public void TryExecute_Success_ReturnsSuccessResult()
    {
        var result = _errorHandler.TryExecute(() => "success");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("success");
    }

    [Fact]
    public void TryExecute_ThrowsException_ReturnsFailureResult()
    {
        var result = _errorHandler.TryExecute<string>(() => throw new InvalidOperationException("Test error"));

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().NotBeEmpty();
        result.Exception.Should().NotBeNull();
    }

    [Fact]
    public async Task TryExecuteAsync_Success_ReturnsSuccessResult()
    {
        var result = await _errorHandler.TryExecuteAsync(async () => await Task.FromResult("async success"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("async success");
    }

    [Fact]
    public async Task TryExecuteAsync_ThrowsException_ReturnsFailureResult()
    {
        var result = await _errorHandler.TryExecuteAsync<string>(
            async () => throw new HttpRequestException("Network error"),
            ErrorType.Network
        );

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.Network);
    }

    [Fact]
    public void GetUserFriendlyMessage_HttpRequestException_ReturnsNetworkMessage()
    {
        var exception = new HttpRequestException("Connection failed");
        var message = _errorHandler.GetUserFriendlyMessage(exception);

        message.Should().Be("❌ Erro de conexão. Verifique sua internet.");
    }

    [Fact]
    public void GetUserFriendlyMessage_IOException_ReturnsFileSystemMessage()
    {
        var exception = new IOException("Disk full");
        var message = _errorHandler.GetUserFriendlyMessage(exception);

        message.Should().Be("❌ Erro ao acessar arquivo. Verifique permissões e espaço em disco.");
    }

    [Fact]
    public void GetUserFriendlyMessage_ArgumentException_ReturnsValidationMessage()
    {
        var exception = new ArgumentException("Invalid input");
        var message = _errorHandler.GetUserFriendlyMessage(exception);

        message.Should().Be("❌ Entrada inválida. Verifique os dados informados.");
    }

    [Fact]
    public void GetUserFriendlyMessage_DllNotFoundException_ReturnsDependencyMissingMessage()
    {
        var exception = new DllNotFoundException("ffmpeg not found");
        var message = _errorHandler.GetUserFriendlyMessage(exception);

        message.Should().Be("❌ Dependência não encontrada. Instale yt-dlp e FFmpeg.");
    }

    [Fact]
    public void GetUserFriendlyMessage_GenericException_ReturnsUnknownMessage()
    {
        var exception = new Exception("Unknown error");
        var message = _errorHandler.GetUserFriendlyMessage(exception);

        message.Should().Be("❌ Ocorreu um erro inesperado. Tente novamente.");
    }

    [Fact]
    public void ShouldRetry_HttpRequestException_ReturnsTrue()
    {
        var exception = new HttpRequestException("Network error");
        var shouldRetry = _errorHandler.ShouldRetry(exception);

        shouldRetry.Should().BeTrue();
    }

    [Fact]
    public void ShouldRetry_TimeoutException_ReturnsTrue()
    {
        var exception = new TimeoutException("Request timed out");
        var shouldRetry = _errorHandler.ShouldRetry(exception);

        shouldRetry.Should().BeTrue();
    }

    [Fact]
    public void ShouldRetry_IOException_ReturnsTrue()
    {
        var exception = new IOException("File lock");
        var shouldRetry = _errorHandler.ShouldRetry(exception);

        shouldRetry.Should().BeTrue();
    }

    [Fact]
    public void ShouldRetry_ArgumentException_ReturnsFalse()
    {
        var exception = new ArgumentException("Invalid argument");
        var shouldRetry = _errorHandler.ShouldRetry(exception);

        shouldRetry.Should().BeFalse();
    }

    [Fact]
    public async Task TryExecuteAsync_WithNetworkError_RetriesThreeTimes()
    {
        var attemptCount = 0;
        var retryPolicy = new RetryPolicy(maxRetries: 3, logger: _mockLogger.Object);
        var handlerWithRetry = new ErrorHandler(_mockLogger.Object);

        var result = await handlerWithRetry.TryExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 4)
            {
                throw new HttpRequestException("Network error");
            }
            return "success after retries";
        }, ErrorType.Network, null, retryPolicy);

        result.IsSuccess.Should().BeTrue();
        attemptCount.Should().Be(4);
    }

    [Fact]
    public async Task TryExecuteAsync_LogsErrors()
    {
        var exception = new InvalidOperationException("Test error");
        
        await _errorHandler.TryExecuteAsync<string>(() => throw exception, ErrorType.Unknown, "test operation");

        _mockLogger.Verify(
            x => x.LogError(
                It.IsAny<Exception>(),
                It.Is<string>(s => s.Contains("test operation"))
            ),
            Times.Once
        );
    }
}
