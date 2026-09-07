namespace Cutube.Cli.ErrorHandling;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public ErrorType ErrorType { get; }
    public string? ErrorMessage { get; }
    public Exception? Exception { get; }

    protected Result(bool isSuccess, ErrorType errorType, string? errorMessage, Exception? exception)
    {
        IsSuccess = isSuccess;
        ErrorType = errorType;
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    public static Result Success()
        => new Result(true, ErrorType.Unknown, null, null);

    public static Result Failure(ErrorType errorType, string message, Exception? exception = null)
        => new Result(false, errorType, message, exception);
}

public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of failed result");

    private Result(bool isSuccess, T? value, ErrorType errorType, string? errorMessage, Exception? exception)
        : base(isSuccess, errorType, errorMessage, exception)
    {
        _value = value;
    }

    public static Result<T> Success(T value)
        => new Result<T>(true, value, ErrorType.Unknown, null, null);

    public static new Result<T> Failure(ErrorType errorType, string message, Exception? exception = null)
        => new Result<T>(false, default, errorType, message, exception);
}
