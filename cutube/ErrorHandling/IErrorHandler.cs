using Cutube.Logging;

namespace Cutube.ErrorHandling;

public interface IErrorHandler
{
    Task<Result<T>> TryExecuteAsync<T>(
        Func<Task<T>> operation,
        ErrorType errorType = ErrorType.Unknown,
        string? context = null
    );

    Result<T> TryExecute<T>(
        Func<T> operation,
        ErrorType errorType = ErrorType.Unknown,
        string? context = null
    );

    string GetUserFriendlyMessage(Exception exception);
    bool ShouldRetry(Exception exception);
}
