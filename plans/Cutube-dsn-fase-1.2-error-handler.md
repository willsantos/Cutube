# Plano de Implementação - Fase 1.2: Error Handler Centralizado

## Objetivo
Criar sistema centralizado de tratamento de erros com Result pattern, retry automático com exponential backoff, e mensagens amigáveis para o usuário em português.

## Problema Atual
- ❌ Exceções sem tratamento adequado (stack traces expostos)
- ❌ Erros de rede não tem retry automático
- ❌ Mensagens de erro técnicas (sem tradução para usuário)
- ❌ Sem distinção entre erros recuperáveis e fatais
- ❌ Workflow lança exceções em vez de retornar resultados

## Solução
Implementar Error Handler com:
- **Result<T> pattern**: Type-safe, sem exceções para fluxo de controle
- **Retry automático**: Exponential backoff para erros de rede
- **Mensagens amigáveis**: Português, sem jargão técnico
- **Detecção automática**: Classifica tipo de erro pela exceção
- **Integração com Logger**: Todos os erros são logados

## Branch
`feature/Cutube-dsn-fase-1.2-error-handler`

## Tarefas Incluídas
- ✅ **Cutube-dsn.13**: Fase 1.2: Error Handler Centralizado (agrupador)
- ✅ **Cutube-dsn.14**: Criar enums e tipos base (Result pattern)
- ✅ **Cutube-dsn.15**: Criar IErrorHandler interface
- ✅ **Cutube-dsn.8**: Implementar ErrorHandler
- ✅ **Cutube-dsn.9**: Criar RetryPolicy com exponential backoff
- ✅ **Cutube-dsn.16**: Testes unitários para ErrorHandling
- ✅ **Cutube-dsn.10**: Refatorar YtDlpHelper com error handling
- ✅ **Cutube-dsn.11**: Refatorar FfmpegHelper
- ✅ **Cutube-dsn.12**: Refatorar ProgramWorkflow para Result pattern

**Depende de:** Fase 1.1 (usa ILoggerService)

---

## Estrutura Final

```
cutube/
├── ErrorHandling/
│   ├── ErrorType.cs                # Enum de tipos de erro
│   ├── Result.cs                   # Result<T> pattern
│   ├── ResultExtensions.cs         # Extension methods
│   ├── IErrorHandler.cs            # Interface
│   ├── ErrorHandler.cs             # Implementação
│   └── RetryPolicy.cs              # Exponential backoff
├── Logging/
│   └── ILoggerService.cs           # Usado pelo ErrorHandler
├── ProgramWorkflow.cs              # Modificado (usa Result<T>)
├── YtDlpHelper.cs                  # Modificado (usa ErrorHandler)
└── FfmpegHelper.cs                 # Modificado (usa ErrorHandler)

Cutube.Tests/
└── Unit/
    └── ErrorHandling/
        ├── ErrorHandlerTests.cs
        ├── RetryPolicyTests.cs
        └── ResultTests.cs
```

---

## Commits Planejados

### 1. `feat(error-handling): criar enums e tipos base (Result pattern)` (Cutube-dsn.14)

**Arquivos NOVOS:**

#### `cutube/ErrorHandling/ErrorType.cs`
```csharp
namespace Cutube.ErrorHandling;

/// <summary>
/// Classificação de tipos de erro para tratamento diferenciado
/// </summary>
public enum ErrorType
{
    Unknown,            // Erro genérico não classificado
    Network,            // Erro de conexão (retry recomendado)
    FileSystem,         // Erro de disco (log & continue)
    Validation,         // Input inválido (erro de usuário)
    DependencyMissing,  // yt-dlp/ffmpeg não encontrado (oferecer solução)
    Critical            // Erro fatal (graceful shutdown)
}
```

#### `cutube/ErrorHandling/Result.cs`
```csharp
namespace Cutube.ErrorHandling;

/// <summary>
/// Resultado de operação que pode falhar sem lançar exceção
/// </summary>
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

    /// <summary>
    /// Cria resultado de sucesso
    /// </summary>
    public static Result Success()
        => new Result(true, ErrorType.Unknown, null, null);

    /// <summary>
    /// Cria resultado de falha
    /// </summary>
    public static Result Failure(ErrorType errorType, string message, Exception? exception = null)
        => new Result(false, errorType, message, exception);
}

/// <summary>
/// Resultado de operação com valor de retorno
/// </summary>
public class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Valor retornado pela operação (lança se falha)
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access Value of failed result: {ErrorMessage}");

    private Result(bool isSuccess, T? value, ErrorType errorType, string? errorMessage, Exception? exception)
        : base(isSuccess, errorType, errorMessage, exception)
    {
        _value = value;
    }

    /// <summary>
    /// Cria resultado de sucesso com valor
    /// </summary>
    public static Result<T> Success(T value)
        => new Result<T>(true, value, ErrorType.Unknown, null, null);

    /// <summary>
    /// Cria resultado de falha
    /// </summary>
    public static new Result<T> Failure(ErrorType errorType, string message, Exception? exception = null)
        => new Result<T>(false, default, errorType, message, exception);

    /// <summary>
    /// Implicit conversion de T para Result<T>.Success(T)
    /// </summary>
    public static implicit operator Result<T>(T value)
        => Success(value);
}
```

#### `cutube/ErrorHandling/ResultExtensions.cs`
```csharp
namespace Cutube.ErrorHandling;

/// <summary>
/// Extension methods para Result pattern
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Executa ação se sucesso, retorna falha se erro
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
    /// Executa ação se falha
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
    /// Mapeia valor de sucesso para outro tipo
    /// </summary>
    public static Result<TNew> Map<T, TNew>(this Result<T> result, Func<T, TNew> mapper)
    {
        return result.IsSuccess
            ? Result<TNew>.Success(mapper(result.Value))
            : Result<TNew>.Failure(result.ErrorType, result.ErrorMessage ?? "", result.Exception);
    }

    /// <summary>
    /// Chain operations: executa próxima só se anterior sucesso
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
```

**Critérios:**
- [ ] ErrorType enum criado
- [ ] Result e Result<T> classes criadas
- [ ] Extension methods criados
- [ ] Compila sem erros
- [ ] XML comments em membros públicos

---

### 2. `feat(error-handling): criar IErrorHandler interface` (Cutube-dsn.15)

**Arquivo NOVO:** `cutube/ErrorHandling/IErrorHandler.cs`

```csharp
using Cutube.Logging;

namespace Cutube.ErrorHandling;

/// <summary>
/// Serviço centralizado de tratamento de erros com retry e mensagens amigáveis
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Tenta executar operação async, capturando exceções e retornando Result
    /// </summary>
    Task<Result<T>> TryExecuteAsync<T>(
        Func<Task<T>> operation,
        ErrorType errorType = ErrorType.Unknown,
        string? context = null,
        RetryPolicy? retryPolicy = null);

    /// <summary>
    /// Tenta executar operação sync, capturando exceções e retornando Result
    /// </summary>
    Result<T> TryExecute<T>(
        Func<T> operation,
        ErrorType errorType = ErrorType.Unknown,
        string? context = null);

    /// <summary>
    /// Retorna mensagem amigável em português para exceção
    /// </summary>
    string GetUserFriendlyMessage(Exception exception);

    /// <summary>
    /// Determina se exceção deve trigger retry
    /// </summary>
    bool ShouldRetry(Exception exception);

    /// <summary>
    /// Detecta tipo de erro baseado na exceção
    /// </summary>
    ErrorType DetectErrorType(Exception exception);
}
```

**Critérios:**
- [ ] Interface IErrorHandler criada
- [ ] Métodos async e sync definidos
- [ ] XML comments completos
- [ ] Referência a ILoggerService incluída

---

### 3. `feat(error-handling): criar RetryPolicy com exponential backoff` (Cutube-dsn.9)

**Arquivo NOVO:** `cutube/ErrorHandling/RetryPolicy.cs`

```csharp
namespace Cutube.ErrorHandling;

/// <summary>
/// Política de retry com exponential backoff
/// </summary>
public class RetryPolicy
{
    public int MaxRetries { get; }
    public TimeSpan InitialDelay { get; }
    public TimeSpan MaxDelay { get; }
    public Func<Exception, bool> ShouldRetryPredicate { get; }

    public RetryPolicy(
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null,
        Func<Exception, bool>? shouldRetryPredicate = null)
    {
        if (maxRetries < 0)
            throw new ArgumentException("MaxRetries must be >= 0", nameof(maxRetries));

        MaxRetries = maxRetries;
        InitialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
        MaxDelay = maxDelay ?? TimeSpan.FromSeconds(30);
        ShouldRetryPredicate = shouldRetryPredicate ?? DefaultRetryPredicate;
    }

    /// <summary>
    /// Predicado padrão: retry para erros de rede e IO
    /// </summary>
    private static bool DefaultRetryPredicate(Exception ex)
    {
        return ex is HttpRequestException
            or TimeoutException
            or IOException
            or System.Net.WebException;
    }

    /// <summary>
    /// Executa operação com retry automático
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct = default)
    {
        var delay = InitialDelay;
        Exception? lastException = null;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    // Log retry attempt
                    Console.WriteLine($"Retry attempt {attempt}/{MaxRetries} after {delay.TotalSeconds}s");
                }

                return await operation();
            }
            catch (Exception ex) when (ShouldRetryPredicate(ex))
            {
                lastException = ex;

                if (attempt == MaxRetries)
                {
                    // Última tentativa falhou, lança exceção
                    throw new Exception(
                        $"Operation failed after {MaxRetries} retries",
                        lastException);
                }

                // Aguarda com exponential backoff
                await Task.Delay(delay, ct);

                // Calcula próximo delay (exponential backoff)
                delay = TimeSpan.FromMilliseconds(
                    Math.Min(
                        delay.TotalMilliseconds * 2,
                        MaxDelay.TotalMilliseconds
                    )
                );
            }
        }

        // Nunca deve chegar aqui (lança no loop acima)
        throw lastException ?? new InvalidOperationException("Retry logic error");
    }

    /// <summary>
    /// Versão sync de ExecuteAsync
    /// </summary>
    public T Execute<T>(Func<T> operation)
    {
        return ExecuteAsync(() => Task.Run(operation)).GetAwaiter().GetResult();
    }
}
```

**Critérios:**
- [ ] RetryPolicy criada com exponential backoff
- [ ] ExecuteAsync implementado
- [ ] Execute sync implementado
- [ ] Predicado configurável
- [ ] Validação de parâmetros

---

### 4. `feat(error-handling): implementar ErrorHandler` (Cutube-dsn.8)

**Arquivo NOVO:** `cutube/ErrorHandling/ErrorHandler.cs`

```csharp
using Cutube.Logging;

namespace Cutube.ErrorHandling;

/// <summary>
/// Implementação centralizada de tratamento de erros
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
        // Detecta tipo de erro se não informado
        if (errorType == ErrorType.Unknown)
        {
            errorType = DetectErrorType(ex);
        }

        // Log do erro
        _logger.LogError(ex, $"Error in {context ?? "operation"}: {ex.Message}");

        // Retorna falha
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

        // Mensagens específicas por exceção
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
        // Verifica tipo exato
        if (ErrorTypeMapping.TryGetValue(exception.GetType(), out var errorType))
        {
            return errorType;
        }

        // Verifica tipo base
        foreach (var (type, mappedType) in ErrorTypeMapping)
        {
            if (type.IsAssignableFrom(exception.GetType()))
            {
                return mappedType;
            }
        }

        // Verifica mensagem para casos específicos
        if (exception.Message.Contains("yt-dlp", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("ffmpeg", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorType.DependencyMissing;
        }

        return ErrorType.Unknown;
    }
}
```

**Critérios:**
- [ ] ErrorHandler implementa IErrorHandler
- [ ] DetectErrorType funciona para exceções comuns
- [ ] GetUserFriendlyMessage retorna português
- [ ] ShouldRetry identifica erros de rede
- [ ] TryExecute/TryExecuteAsync funcionam
- [ ] Integração com ILoggerService

---

### 5. `test(error-handling): testes unitários para ErrorHandling` (Cutube-dsn.16)

**Arquivos NOVOS:**

#### `Cutube.Tests/Unit/ErrorHandling/ResultTests.cs`
```csharp
using Cutube.ErrorHandling;
using Xunit;

namespace Cutube.Tests.Unit.ErrorHandling;

public class ResultTests
{
    [Fact]
    public void Result_Success_ReturnsTrue()
    {
        var result = Result.Success();
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Result_Failure_ReturnsFalse()
    {
        var result = Result.Failure(ErrorType.Validation, "Invalid input");
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid input", result.ErrorMessage);
    }

    [Fact]
    public void ResultT_Success_ReturnsValue()
    {
        var result = Result<int>.Success(42);
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ResultT_Failure_ThrowsOnValueAccess()
    {
        var result = Result<int>.Failure(ErrorType.Network, "Connection failed");
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void OnSuccess_ExecutesAction_WhenSuccess()
    {
        var executed = false;
        Result.Success()
            .OnSuccess(() => executed = true);

        Assert.True(executed);
    }

    [Fact]
    public void OnFailure_ExecutesAction_WhenFailure()
    {
        ErrorType capturedType = ErrorType.Unknown;
        string capturedMessage = "";

        Result.Failure(ErrorType.Validation, "Test error")
            .OnFailure((type, msg) =>
            {
                capturedType = type;
                capturedMessage = msg;
            });

        Assert.Equal(ErrorType.Validation, capturedType);
        Assert.Equal("Test error", capturedMessage);
    }

    [Fact]
    public void Then_ChainsOperations_WhenAllSuccess()
    {
        var result = Result<int>.Success(10)
            .Then(x => Result<int>.Success(x * 2))
            .Then(x => Result<string>.Success($"Result: {x}"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Result: 20", result.Value);
    }

    [Fact]
    public void Then_StopsOnFirstFailure()
    {
        var result = Result<int>.Success(10)
            .Then(x => Result<int>.Failure(ErrorType.Validation, "Invalid"))
            .Then(x => Result<int>.Success(x * 2)); // Não deve executar

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid", result.ErrorMessage);
    }
}
```

#### `Cutube.Tests/Unit/ErrorHandling/ErrorHandlerTests.cs`
```csharp
using Cutube.ErrorHandling;
using Cutube.Logging;
using Moq;
using Xunit;

namespace Cutube.Tests.Unit.ErrorHandling;

public class ErrorHandlerTests
{
    private readonly Mock<ILoggerService> _loggerMock;
    private readonly IErrorHandler _errorHandler;

    public ErrorHandlerTests()
    {
        _loggerMock = new Mock<ILoggerService>();
        _errorHandler = new ErrorHandler(_loggerMock.Object);
    }

    [Fact]
    public void TryExecute_ReturnsSuccess_WhenNoException()
    {
        var result = _errorHandler.TryExecute(() => 42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void TryExecute_ReturnsFailure_WhenExceptionThrown()
    {
        var result = _errorHandler.TryExecute<int>(() => throw new InvalidOperationException("Test error"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.DependencyMissing, result.ErrorType);
    }

    [Fact]
    public async Task TryExecuteAsync_ReturnsSuccess_WhenNoException()
    {
        var result = await _errorHandler.TryExecuteAsync(async () =>
        {
            await Task.Delay(10);
            return 42;
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void DetectErrorType_ClassifiesNetworkErrors()
    {
        var errorType = _errorHandler.DetectErrorType(new HttpRequestException());
        Assert.Equal(ErrorType.Network, errorType);
    }

    [Fact]
    public void DetectErrorType_ClassifiesValidationErrors()
    {
        var errorType = _errorHandler.DetectErrorType(new ArgumentException("Invalid"));
        Assert.Equal(ErrorType.Validation, errorType);
    }

    [Fact]
    public void GetUserFriendlyMessage_ReturnsPortugueseMessage()
    {
        var message = _errorHandler.GetUserFriendlyMessage(new HttpRequestException());
        Assert.Equal("Erro de conexão. Verifique sua internet.", message);
    }

    [Fact]
    public void ShouldRetry_ReturnsTrue_ForNetworkErrors()
    {
        Assert.True(_errorHandler.ShouldRetry(new HttpRequestException()));
        Assert.True(_errorHandler.ShouldRetry(new TimeoutException()));
    }

    [Fact]
    public void ShouldRetry_ReturnsFalse_ForValidationErrors()
    {
        Assert.False(_errorHandler.ShouldRetry(new ArgumentException());
    }
}
```

#### `Cutube.Tests/Unit/ErrorHandling/RetryPolicyTests.cs`
```csharp
using Cutube.ErrorHandling;
using Xunit;

namespace Cutube.Tests.Unit.ErrorHandling;

public class RetryPolicyTests
{
    [Fact]
    public async Task ExecuteAsync_SucceedsOnFirstAttempt()
    {
        var policy = new RetryPolicy(maxRetries: 3);
        var result = await policy.ExecuteAsync(async () =>
        {
            await Task.Delay(10);
            return 42;
        });

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsync_Retries_OnNetworkError()
    {
        var attempts = 0;
        var policy = new RetryPolicy(maxRetries: 3);

        var result = await policy.ExecuteAsync(async () =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw new HttpRequestException("Network error");
            }
            return "Success";
        });

        Assert.Equal(3, attempts);
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task ExecuteAsync_Throws_WhenMaxRetriesExceeded()
    {
        var policy = new RetryPolicy(maxRetries: 2);

        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await policy.ExecuteAsync<int>(async () =>
            {
                await Task.Delay(10);
                throw new HttpRequestException("Always fails");
            });
        });
    }

    [Fact]
    public void Constructor_Throws_OnNegativeMaxRetries()
    {
        Assert.Throws<ArgumentException>(() => new RetryPolicy(maxRetries: -1));
    }
}
```

**Critérios:**
- [ ] Testes de Result pattern passam
- [ ] Testes de ErrorHandler passam
- [ ] Testes de RetryPolicy passam
- [ ] Mock de ILoggerService usado
- [ ] Cobertura > 80%

---

### 6. `refactor(error-handling): refatorar YtDlpHelper` (Cutube-dsn.10)

**Arquivo MODIFICADO:** `cutube/YtDlpHelper.cs`

**Antes:**
```csharp
public async Task<VideoInfo> GetVideoInfoAsync(string url, CancellationToken ct)
{
    // Lança exceção se falhar
    var runOutput = await _ytdlp.RunVideoDataFetch(url);
    return ParseVideoInfo(runOutput);
}
```

**Depois:**
```csharp
public async Task<Result<VideoInfo>> GetVideoInfoAsync(string url, CancellationToken ct)
{
    return await _errorHandler.TryExecuteAsync(
        async () => {
            var runOutput = await _ytdlp.RunVideoDataFetch(url);
            return ParseVideoInfo(runOutput);
        },
        ErrorType.Network,
        "Obter informações do vídeo",
        new RetryPolicy(maxRetries: 3) // Retry automático para network errors
    );
}
```

**Mudanças principais:**
- Adicionar `IErrorHandler` no construtor
- Envolver chamadas de rede com TryExecuteAsync
- Usar Result<T> em vez de lançar exceções
- Adicionar RetryPolicy para operações de rede

**Critérios:**
- [ ] IErrorHandler injetado no construtor
- [ ] Métodos retornam Result<T>
- [ ] Network errors trigger retry
- [ ] Mensagens de contexto descritivas

---

### 7. `refactor(error-handling): refatorar FfmpegHelper` (Cutube-dsn.11)

**Arquivo MODIFICADO:** `cutube/FfmpegHelper.cs`

**Antes:**
```csharp
catch (Exception e)
{
    _consoleService.WriteLine(e.ToString()); // ❌ Expõe stack trace
    throw;
}
```

**Depois:**
```csharp
catch (Exception e)
{
    _logger.LogError(e, "Erro ao executar FFmpeg");

    // Verifica se é dependência ausente
    if (e.Message.Contains("ffmpeg", StringComparison.OrdinalIgnoreCase) ||
        e.Message.Contains("command not found", StringComparison.OrdinalIgnoreCase))
    {
        throw new Exception(
            _errorHandler.GetUserFriendlyMessage(e),
            e
        );
    }

    throw; // Outros erros são tratados pelo ErrorHandler
}
```

**Mudanças:**
- Remover Console.WriteLine de stack traces
- Usar logger para debug
- Detectar dependência ausente (FFmpeg)
- Lançar exceção com mensagem amigável

**Critérios:**
- [ ] Sem stack traces expostos
- [ ] Logger usado para debug
- [ ] Mensagens amigáveis para dependência ausente

---

### 8. `refactor(error-handling): refatorar ProgramWorkflow para Result pattern` (Cutube-dsn.12)

**Arquivo MODIFICADO:** `cutube/ProgramWorkflow.cs`

**Antes:**
```csharp
public async Task RunAsync(CancellationToken cancellationToken)
{
    var menu = _menuService.GetMenuInput();
    var info = await _ytdlpService.GetVideoInfoAsync(menu.Url, cancellationToken);
    // ...
}
```

**Depois:**
```csharp
public async Task<Result> RunAsync(CancellationToken cancellationToken)
{
    // Validar input
    var menuResult = _errorHandler.TryExecute(() => _menuService.GetMenuInput());
    if (menuResult.IsFailure)
        return Result.Failure(ErrorType.Validation, menuResult.ErrorMessage!);

    // Obter info (com retry)
    var infoResult = await _ytdlpService.GetVideoInfoAsync(menuResult.Value.Url, cancellationToken);
    if (infoResult.IsFailure)
    {
        _consoleService.WriteLine($"❌ {infoResult.ErrorMessage}");
        return Result.Failure(infoResult.ErrorType, infoResult.ErrorMessage!);
    }

    // Continuar workflow...
    return Result.Success();
}
```

**Mudanças:**
- Workflow retorna Result em vez de void
- Cada passo valida Result antes de continuar
- Mensagens amigáveis para usuário

**Critérios:**
- [ ] Workflow retorna Result
- [ ] Valida IsSuccess antes de continuar
- [ ] Mensagens amigáveis exibidas

---

## Critérios de Aceite

### Funcional
- [ ] Erros de rede trigger retry automático (3 tentativas)
- [ ] Mensagens de erro em português (sem jargão técnico)
- [ ] Stack traces nunca expostas ao usuário
- [ ] Result<T> pattern usado em vez de exceções para controle de fluxo
- [ ] Workflow não quebra em erros recuperáveis

### Técnico
- [ ] `dotnet build` - ZERO warnings
- [ ] `dotnet test` - 100% dos testes passando
- [ ] Result pattern implementado corretamente
- [ ] RetryPolicy com exponential backoff

### Qualidade
- [ ] Cobertura de testes > 80% para código novo
- [ ] XML comments em interfaces/classes públicas

---

## Testes Manuais Sugeridos

### Teste de retry
```bash
# Desconectar internet
# Executar download (deve tentar 3x)
dotnet run -- download "https://youtube.com/watch?v=test"

# Output esperado:
# "Retry attempt 1/3 after 1s"
# "Retry attempt 2/3 after 2s"
# "Retry attempt 3/3 after 4s"
# "Erro de conexão. Verifique sua internet."
```

### Teste de Result pattern
```csharp
var result = await _ytdlpService.GetVideoInfoAsync(url, ct);

if (result.IsFailure)
{
    Console.WriteLine($"❌ {result.ErrorMessage}");
    return;
}

var info = result.Value;
Console.WriteLine($"✓ Título: {info.Title}");
```

---

## Notas de Implementação

### Result Pattern
- Type-safe: compilador garante verificação de IsSuccess
- Sem exceções para controle de fluxo (performance)
- Chainable com extension methods (Then, Map)
- Implicit operator para conversão de T

### Retry Policy
- Exponential backoff: 1s, 2s, 4s, 8s, ... (máx 30s)
- Só retry para erros recuperáveis (network, IO)
- Validação nunca tem retry (erro de usuário)

### ErrorType Detection
- Mapeamento por tipo de exceção
- Fallback para análise de mensagem
- Customizável via predicado

---

## Breaking Changes

**Parcialmente breaking:**
- Assinaturas de métodos mudam: `Task<T>` → `Task<Result<T>>`
- Callers precisam verificar `IsSuccess`
- Benefício: código mais robusto e debugável

---

## Dependências

**Fase 1.1 (Logging):**
- ILoggerService usado pelo ErrorHandler
- Logs de erros são estruturados

---

## Estimativa

**Total:** ~22 horas (3 dias)
- Cutube-dsn.14 (Result pattern): 3h
- Cutube-dsn.15 (IErrorHandler): 2h
- Cutube-dsn.9 (RetryPolicy): 3h
- Cutube-dsn.8 (ErrorHandler): 4h
- Cutube-dsn.16 (testes): 4h
- Cutube-dsn.10 (YtDlp refactor): 3h
- Cutube-dsn.11 (Ffmpeg refactor): 2h
- Cutube-dsn.12 (ProgramWorkflow refactor): 3h

---

## Commit Message Final

```bash
git add .
git commit -m "feat(error-handling): implementar error handler centralizado com Result pattern (Cutube-dsn.8-16,10-12)

- Criar Result<T> pattern para type-safe error handling
- Implementar ErrorHandler com detecção automática de tipo de erro
- Adicionar RetryPolicy com exponential backoff para erros de rede
- Refatorar YtDlpHelper para usar Result<T> e retry automático
- Refatorar FfmpegHelper para remover stack traces expostos
- Refatorar ProgramWorkflow para validar Result<T> em cada step
- Adicionar mensagens amigáveis em português
- Integrar com ILoggerService (Fase 1.1)

Branch: feature/Cutube-dsn-fase-1.2-error-handler
Tarefas: Cutube-dsn.8,9,10,11,12,13,14,15,16"
```
