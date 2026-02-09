namespace Cutube.ErrorHandling;

/// <summary>
/// Extension methods for Result pattern
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Executes action if success, returns failure if error
    /// </summary>
    public static Result OnSuccess(this Result result, Action action)
    {
        if (result.IsSuccess)
        {
            action();
        }
        return result;
    }

    /// <summary>
    /// Executes action if failure
    /// </summary>
    public static Result OnFailure(this Result result, Action<ErrorType, string> action)
    {
        if (result.IsFailure)
        {
            action(result.ErrorType, result.ErrorMessage ?? "");
        }
        return result;
    }

    /// <summary>
    /// Maps value of success to another type
    /// </summary>
    public static Result<TNew> Map<T, TNew>(this Result<T> result, Func<T, TNew> mapper)
    {
        return result.IsSuccess
            ? Result<TNew>.Success(mapper(result.Value))
            : Result<TNew>.Failure(result.ErrorType, result.ErrorMessage ?? "", result.Exception);
    }

    /// <summary>
    /// Chain operations: executes next only if previous success
    /// </summary>
    public static Result<TNew> Then<T, TNew>(
        this Result<T> result,
        Func<T, Result<TNew>> next)
    {
        return result.IsSuccess
            ? next(result.Value)
            : Result<TNew>.Failure(result.ErrorType, result.ErrorMessage ?? "", result.Exception);
    }
}
