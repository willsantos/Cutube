using Cutube.Logging;

namespace Cutube.ErrorHandling;

/// <summary>
/// Centralized error handling implementation
/// </summary>
public class ErrorHandler : IErrorHandler
{
    private readonly ILoggerService _logger;
    private static readonly Dictionary<Type, ErrorType> ErrorTypeMapping = new()
    {
        [typeof(HttpRequestException)] = ErrorType.Network,
        [typeof(TimeoutException)] = ErrorType.Network,
        [typeof(IOException)] = ErrorType.FileSystem,
        [typeof(UnauthorizedAccessException)] = ErrorType.FileSystem,
        [typeof(ArgumentException)] = ErrorType.Validation,
        [typeof(FormatException)] = ErrorType.Validation,
        [typeof(InvalidOperationException)] = ErrorType.DependencyMissing,
    };

    private static readonly Dictionary<ErrorType, string> UserFriendlyMessages = new()
    {
        [ErrorType.Network] = "Erro de conexão. Verifique sua internet.",
        [ErrorType.FileSystem] = "Erro ao acessar arquivo. Verifique permissões e espaço em disco.",
        [ErrorType.Validation] = "Entrada inválida. Verifique os dados informados.",
        [ErrorType.DependencyMissing] = "Dependência não encontrada. Instale yt-dlp e FFmpeg.",
        [ErrorType.Critical] = "Erro fatal. O aplicativo será encerrado.",
        [ErrorType.Unknown] = "Ocorreu um erro inesperado."
    };

    public ErrorHandler(ILoggerService logger)
    {
        _logger = logger;
    }

    public async Task<Result<T>> TryExecuteAsync<T>(
        Func<Task<T>> operation,
        ErrorType errorType = ErrorType.Unknown,
        string? context = null,
        RetryPolicy? retryPolicy = null)
    {
        try
        {
            if (retryPolicy != null)
            {
                var result = await retryPolicy.ExecuteAsync(async () =>
                {
                    _logger.LogDebug($"Executing async operation with retry: {context ?? "unknown"}");
                    return await operation();
                });
                return Result<T>.Success(result);
            }
            else
            {
                _logger.LogDebug($"Executing async operation: {context ?? "unknown"}");
                var result = await operation();
                return Result<T>.Success(result);
            }
        }
        catch (Exception ex)
        {
            return HandleException<T>(ex, errorType, context);
        }
    }

    public Result<T> TryExecute<T>(
        Func<T> operation,
        ErrorType errorType = ErrorType.Unknown,
        string? context = null)
    {
        try
        {
            _logger.LogDebug($"Executing sync operation: {context ?? "unknown"}");
            var result = operation();
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            return HandleException<T>(ex, errorType, context);
        }
    }

    private Result<T> HandleException<T>(Exception ex, ErrorType errorType, string? context)
    {
        // Detect error type if not provided
        if (errorType == ErrorType.Unknown)
        {
            errorType = DetectErrorType(ex);
        }

        // Log error
        _logger.LogError(ex, $"Error in {context ?? "operation"}: {ex.Message}");

        // Return failure
        var userMessage = GetUserFriendlyMessage(ex);
        return Result<T>.Failure(errorType, userMessage, ex);
    }

    public string GetUserFriendlyMessage(Exception exception)
    {
        var errorType = DetectErrorType(exception);

        if (UserFriendlyMessages.TryGetValue(errorType, out var message))
        {
            return message;
        }

        // Specific messages by exception type
        return exception switch
        {
            FileNotFoundException => "Arquivo não encontrado.",
            DirectoryNotFoundException => "Diretório não encontrado.",
            _ => "Ocorreu um erro inesperado. Tente novamente."
        };
    }

    public bool ShouldRetry(Exception exception)
    {
        return exception is HttpRequestException
            or TimeoutException
            or IOException;
    }

    public ErrorType DetectErrorType(Exception exception)
    {
        // Check exact type
        if (ErrorTypeMapping.TryGetValue(exception.GetType(), out var errorType))
        {
            return errorType;
        }

        // Check base type
        foreach (var (type, mappedType) in ErrorTypeMapping)
        {
            if (type.IsAssignableFrom(exception.GetType()))
            {
                return mappedType;
            }
        }

        // Check message for specific cases
        if (exception.Message.Contains("yt-dlp", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("ffmpeg", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorType.DependencyMissing;
        }

        return ErrorType.Unknown;
    }
}
