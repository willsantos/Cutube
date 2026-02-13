using Cutube.Logging;

namespace Cutube.Cli.ErrorHandling;

/// <summary>
/// Centralized error handling service with retry and user-friendly messages
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Tries to execute async operation, catching exceptions and returning Result
    /// </summary>
    Task<Result<T>> TryExecuteAsync<T>(
        Func<Task<T>> operation,
        ErrorType errorType = ErrorType.Unknown,
        string? context = null,
        RetryPolicy? retryPolicy = null);

    /// <summary>
    /// Tries to execute sync operation, catching exceptions and returning Result
    /// </summary>
    Result<T> TryExecute<T>(
        Func<T> operation,
        ErrorType errorType = ErrorType.Unknown,
        string? context = null);

    /// <summary>
    /// Returns user-friendly message in Portuguese for exception
    /// </summary>
    string GetUserFriendlyMessage(Exception exception);

    /// <summary>
    /// Determines if exception should trigger retry
    /// </summary>
    bool ShouldRetry(Exception exception);

    /// <summary>
    /// Detects error type based on exception
    /// </summary>
    ErrorType DetectErrorType(Exception exception);
}
