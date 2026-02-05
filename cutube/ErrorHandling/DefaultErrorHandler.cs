namespace Cutube.ErrorHandling;

public class DefaultErrorHandler : IErrorHandler
{
    public async Task<Result<T>> TryExecuteAsync<T>(
        Func<Task<T>> operation,
        ErrorType errorType = ErrorType.Unknown,
        string? context = null)
    {
        try
        {
            var result = await operation();
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(errorType, ex.Message, ex);
        }
    }

    public Result<T> TryExecute<T>(
        Func<T> operation,
        ErrorType errorType = ErrorType.Unknown,
        string? context = null)
    {
        try
        {
            var result = operation();
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(errorType, ex.Message, ex);
        }
    }

    public string GetUserFriendlyMessage(Exception exception)
    {
        return exception.Message;
    }

    public bool ShouldRetry(Exception exception)
    {
        return false;
    }
}
