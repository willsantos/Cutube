# 🎯 Plano de Implementação - Épico 1: Error Handler Resiliente

## 📋 Visão Geral

**Objetivo:** Transformar o Cutube em um sistema que nunca encerra por erros, registra tudo e permite recuperação.

**Duração estimada:** 7-10 dias

**Dependências:** Nenhuma (pode iniciar imediatamente)

**Status atual do projeto:**
- ❌ Nenhum sistema de logging
- ⚠️ Error handling básico (exceções sem recuperação)
- ❌ Sem persistência de estado
- ✅ Boa arquitetura baseada em interfaces (fácil injeção de dependências)

---

## 📦 Fase 1.1: Logging Estruturado (2-3 dias)

### Objetivo
Implementar sistema de logging estruturado com rotação automática e níveis de log.

### Tarefas Detalhadas

#### Tarefa 1.1.1: Adicionar pacote Serilog (1 hora)

```bash
# Adicionar ao cutube.csproj
dotnet add package Serilog
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Formatting.Compact
```

**Critérios:**
- [ ] Pacotes adicionados ao projeto
- [ ] Build sem warnings

---

#### Tarefa 1.1.2: Criar interfaces e enums (2 horas)

**Arquivo:** `cutube/Logging/LogLevel.cs`
```csharp
namespace Cutube.Logging;

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}
```

**Arquivo:** `cutube/Logging/ILogEntry.cs`
```csharp
namespace Cutube.Logging;

public interface ILogEntry
{
    DateTime Timestamp { get; }
    LogLevel Level { get; }
    string Message { get; }
    Exception? Exception { get; }
    Dictionary<string, object> Context { get; }
}
```

**Arquivo:** `cutube/Logging/LogEntry.cs`
```csharp
public class LogEntry : ILogEntry
{
    public DateTime Timestamp { get; init; }
    public LogLevel Level { get; init; }
    public string Message { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
    public Dictionary<string, object> Context { get; init; } = new();
}
```

**Critérios:**
- [ ] Interface e classe criadas
- [ ] Segue padrão de nomenclatura do projeto (I-prefix)

---

#### Tarefa 1.1.3: Criar ILoggerService (2 horas)

**Arquivo:** `cutube/Logging/ILoggerService.cs`
```csharp
using Cutube.Logging;

namespace Cutube.Logging;

public interface ILoggerService
{
    void LogDebug(string message, params (string key, object value)[] context);
    void LogInfo(string message, params (string key, object value)[] context);
    void LogWarning(string message, params (string key, object value)[] context);
    void LogError(Exception exception, string message, params (string key, object value)[] context);
    void LogCritical(Exception exception, string message, params (string key, object value)[] context);
}
```

**Critérios:**
- [ ] Interface segue padrão I-prefix do projeto
- [ ] Métodos para todos os níveis de log
- [ ] Suporte a contexto via tuplas nomeadas

---

#### Tarefa 1.1.4: Implementar FileLoggerService (4 horas)

**Arquivo:** `cutube/Logging/FileLoggerService.cs`

**Requisitos:**
- Usar Serilog internamente
- Path: `~/.local/share/Cutube/logs/cutube-{date}.log`
- Formato JSON compacto
- Rotação: máximo 10MB por arquivo
- Retenção: 7 dias
- Criar diretório se não existir

**Implementação:**
```csharp
public class FileLoggerService : ILoggerService, IDisposable
{
    private readonly Serilog.Core.Logger _logger;
    private readonly string _logBasePath;

    public FileLoggerService(IEnvironmentService environmentService)
    {
        // Usar IEnvironmentService existente
        _logBasePath = Path.Combine(
            environmentService.GetLocalSharePath(),
            "Cutube",
            "logs"
        );

        // Configurar Serilog
        _logger = new LoggerConfiguration()
            .WriteTo.File(
                path: Path.Combine(_logBasePath, "cutube-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception}",
                fileSizeLimitBytes: 10_000_000, // 10MB
                retainedFileCountLimit: 7,
                formatter: new CompactJsonFormatter()
            )
            .CreateLogger();
    }

    // Implementar métodos LogDebug, LogInfo, etc.
}
```

**Critérios:**
- [ ] Usa `IEnvironmentService` existente
- [ ] Rotação automática funcionando
- [ ] JSON estruturado
- [ ] Dispose implementado
- [ ] Testes unitários passando

---

#### Tarefa 1.1.5: Testes unitários para Logging (3 horas)

**Arquivo:** `Cutube.Tests/Unit/Logging/FileLoggerServiceTests.cs`

**Cenários de teste:**
- [ ] Log Debug é escrito no arquivo
- [ ] Log com contexto serializa corretamente
- [ ] Log com Exception inclui stack trace
- [ ] Rotação de arquivo funciona (mock do filesystem)
- [ ] Caminho inválido é tratado (fallback para /tmp)

**Critérios:**
- [ ] Todos os testes passam
- [ ] Cobertura > 80%

---

#### Tarefa 1.1.6: Integrar no Program.cs (1 hora)

**Modificações:**
```csharp
// Program.cs
using Cutube.Logging;
using Cutube.Interfaces;

var loggerService = new FileLoggerService(environmentService);

// Adicionar ao workflow
var app = new ProgramWorkflow(
    menuService: menuService,
    ytdlpService: ytdlpService,
    consoleService: consoleService,
    fileService: fileService,
    loggerService: loggerService  // NOVO
);
```

**Critérios:**
- [ ] Logger injetado no workflow
- [ ] Build sem warnings
- [ ] Log criado em `~/.local/share/Cutube/logs/`

---

### Checklist Fase 1.1

- [ ] Serilog adicionado ao projeto
- [ ] `ILoggerService` interface criada
- [ ] `FileLoggerService` implementado
- [ ] Testes unitários criados e passando
- [ ] Integração com Program.cs completa
- [ ] `dotnet test` passa (100%)
- [ ] `dotnet build` sem warnings

---

## 🛡️ Fase 1.2: Error Handler Centralizado (3-4 dias)

### Objetivo
Criar sistema de tratamento de erros com retry, fallback e mensagens amigáveis.

### Tarefas Detalhadas

#### Tarefa 1.2.1: Criar enums e tipos base (2 horas)

**Arquivo:** `cutube/ErrorHandling/ErrorType.cs`
```csharp
namespace Cutube.ErrorHandling;

public enum ErrorType
{
    Unknown,
    Network,           // Erro de conexão (retry)
    FileSystem,        // Erro de disco (log & continue)
    Validation,        // Input inválido (user error)
    DependencyMissing, // yt-dlp/ffmpeg não encontrado (offer solution)
    Critical           // Erro fatal (graceful shutdown)
}
```

**Arquivo:** `cutube/ErrorHandling/Result.cs`
```csharp
namespace Cutube.ErrorHandling;

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

    public static Result<T> Failure(ErrorType errorType, string message, Exception? exception = null)
        => new Result<T>(false, default, errorType, message, exception);
}
```

**Critérios:**
- [ ] Pattern Result<T> implementado
- [ ] Type-safe (não lança exceções inesperadas)
- [ ] Segue convenção do projeto

---

#### Tarefa 1.2.2: Criar IErrorHandler (2 horas)

**Arquivo:** `cutube/ErrorHandling/IErrorHandler.cs`
```csharp
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
```

**Critérios:**
- [ ] Interface genérica para sync/async
- [ ] Métodos para executar operações com proteção

---

#### Tarefa 1.2.3: Criar RetryPolicy (2 horas)

**Arquivo:** `cutube/ErrorHandling/RetryPolicy.cs`
```csharp
namespace Cutube.ErrorHandling;

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
        MaxRetries = maxRetries;
        InitialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
        MaxDelay = maxDelay ?? TimeSpan.FromSeconds(30);
        ShouldRetryPredicate = shouldRetryPredicate ?? DefaultRetryPredicate;
    }

    private static bool DefaultRetryPredicate(Exception ex)
    {
        return ex is HttpRequestException
            or TimeoutException
            or IOException;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        var delay = InitialDelay;
        Exception? lastException = null;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (ShouldRetryPredicate(ex))
            {
                lastException = ex;
                if (attempt < MaxRetries)
                {
                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(
                        Math.Min(delay.TotalMilliseconds * 2, MaxDelay.TotalMilliseconds)
                    );
                }
            }
        }

        throw lastException!;
    }
}
```

**Critérios:**
- [ ] Exponential backoff implementado
- [ ] Predicate configurável
- [ ] Testes para cenários de retry

---

#### Tarefa 1.2.4: Implementar ErrorHandler (4 horas)

**Arquivo:** `cutube/ErrorHandling/ErrorHandler.cs`

**Requisitos:**
- Detectar tipo de erro automaticamente
- Retornar mensagens amigáveis em português
- Aplicar retry para erros de rede
- Registrar todos os erros no logger

**Mensagens amigáveis:**
```csharp
private static readonly Dictionary<ErrorType, string> UserMessages = new()
{
    [ErrorType.Network] = "Erro de conexão. Verifique sua internet.",
    [ErrorType.FileSystem] = "Erro ao acessar arquivo. Verifique permissões e espaço em disco.",
    [ErrorType.Validation] = "Entrada inválida. Verifique os dados informados.",
    [ErrorType.DependencyMissing] = "Dependência não encontrada. Instale yt-dlp e FFmpeg.",
    [ErrorType.Critical] = "Erro fatal. O aplicativo será encerrado."
};
```

**Critérios:**
- [ ] Detecta automaticamente ErrorType
- [ ] Mensagens em português
- [ ] Retry aplicado para erros de rede
- [ ] Todos os erros logados

---

#### Tarefa 1.2.5: Refatorar ProgramWorkflow para Result pattern (4 horas)

**Modificações em `cutube/ProgramWorkflow.cs`:**

**Antes:**
```csharp
public async Task RunAsync(CancellationToken cancellationToken)
{
    var menu = _menuService.GetMenuInput();
    var info = await _ytdlpService.GetVideoInfoAsync(menu.Url, cancellationToken); // Pode lançar exceção
    // ...
}
```

**Depois:**
```csharp
public async Task<Result> RunAsync(CancellationToken cancellationToken)
{
    var menuResult = _errorHandler.TryExecute(() => _menuService.GetMenuInput());
    if (menuResult.IsFailure)
        return Result.Failure(ErrorType.Validation, menuResult.ErrorMessage!);

    var infoResult = await _errorHandler.TryExecuteAsync(
        () => _ytdlpService.GetVideoInfoAsync(menuResult.Value.Url, cancellationToken),
        ErrorType.Network,
        "Obter informações do vídeo"
    );

    if (infoResult.IsFailure)
        return Result.Failure(ErrorType.Network, infoResult.ErrorMessage!);

    // ...continuar workflow

    return Result.Success();
}
```

**Critérios:**
- [ ] Workflow retorna Result em vez de lançar exceções
- [ ] Erros tratados com mensagens amigáveis
- [ ] Logger recebe todos os erros

---

#### Tarefa 1.2.6: Refatorar YtDlpHelper (3 horas)

**Mudanças em `cutube/YtDlpHelper.cs`:**

**Alterações principais:**
- Adicionar `IErrorHandler` no construtor
- Envolver chamadas de rede com retry automático
- Usar Result pattern para métodos públicos
- Tratar exceções específicas do yt-dlp

```csharp
public async Task<Result<VideoInfo>> GetVideoInfoAsync(string url, CancellationToken ct)
{
    return await _errorHandler.TryExecuteAsync(
        async () => {
            // implementação existente
            var runOutput = await _ytdlp.RunVideoDataFetch(url);
            return ParseVideoInfo(runOutput);
        },
        ErrorType.Network,
        $"Obter informações do vídeo: {url}"
    );
}
```

**Critérios:**
- [ ] Network errors trigger retry
- [ ] Métodos retornam Result<T>
- [ ] Exceções do yt-dlp traduzidas para ErrorType

---

#### Tarefa 1.2.7: Refatorar FfmpegHelper (2 horas)

**Mudanças em `cutube/FfmpegHelper.cs`:**

**Alterações:**
- Remover `Console.WriteLine(e.ToString())` - expõe stack trace
- Usar logger em vez de console
- Verificar dependência FFmpeg com mensagem amigável

```csharp
// ANTES (errado):
catch (Exception e)
{
    _consoleService.WriteLine(e.ToString()); // ❌ expõe stack trace
    throw;
}

// DEPOIS (correto):
catch (Exception e)
{
    _logger.LogError(e, "Erro ao executar FFmpeg");
    throw new Exception(
        _errorHandler.GetUserFriendlyMessage(e),
        e
    );
}
```

**Critérios:**
- [ ] Sem stack traces expostos
- [ ] Logger usado para debug
- [ ] Mensagens amigáveis para usuário

---

#### Tarefa 1.2.8: Testes unitários (4 horas)

**Arquivo:** `Cutube.Tests/Unit/ErrorHandling/ErrorHandlerTests.cs`

**Cenários:**
- [ ] TryExecute retorna sucesso sem erros
- [ ] TryExecute captura exceção e retorna Failure
- [ ] Retry funciona para HttpRequestException
- [ ] Retry NÃO funciona para ArgumentException
- [ ] GetUserFriendlyMessage retorna texto em português
- [ ] ErrorType detectado corretamente por exceção

**Critérios:**
- [ ] Todos os testes passam
- [ ] Cobertura > 80%

---

### Checklist Fase 1.2

- [ ] Result<T> pattern implementado
- [ ] IErrorHandler interface criada
- [ ] ErrorHandler com retry implementado
- [ ] ProgramWorkflow refatorado
- [ ] YtDlpHelper refatorado
- [ ] FfmpegHelper refatorado
- [ ] Testes unitários criados e passando
- [ ] `dotnet test` passa (100%)
- [ ] `dotnet build` sem warnings

---

## 🔄 Fase 1.3: Recovery & Resume (2-3 dias)

### Objetivo
Implementar persistência de estado para permitir retomada de downloads interrompidos.

### Tarefas Detalhadas

#### Tarefa 1.3.1: Criar modelos de estado (2 horas)

**Arquivo:** `cutube/Recovery/DownloadState.cs`
```csharp
namespace Cutube.Recovery;

public class DownloadState
{
    public required string StateId { get; init; } = Guid.NewGuid().ToString();
    public required string Url { get; set; }
    public required string OutputPath { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public bool AudioOnly { get; set; }
    public DownloadStatus Status { get; set; }
    public int ProgressPercent { get; set; }
    public string? TempFilePath { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum DownloadStatus
{
    Pending,
    Downloading,
    Processing,
    Completed,
    Failed,
    Cancelled
}
```

**Critérios:**
- [ ] Modelo com todas as informações necessárias
- [ ] Enum com status relevantes

---

#### Tarefa 1.3.2: Criar IDownloadStateManager (2 horas)

**Arquivo:** `cutube/Recovery/IDownloadStateManager.cs`
```csharp
namespace Cutube.Recovery;

public interface IDownloadStateManager
{
    Task<string> CreateStateAsync(DownloadState state);
    Task<DownloadState?> GetStateAsync(string stateId);
    Task UpdateStateAsync(string stateId, Action<DownloadState> update);
    Task<List<DownloadState>> GetActiveStatesAsync();
    Task MarkCompletedAsync(string stateId);
    Task MarkFailedAsync(string stateId, string error);
    Task DeleteStateAsync(string stateId);
    Task CleanupOldStatesAsync(TimeSpan maxAge);
}
```

**Critérios:**
- [ ] Interface com todos os métodos necessários
- [ ] Pattern de update via Action

---

#### Tarefa 1.3.3: Implementar DownloadStateManager (4 horas)

**Arquivo:** `cutube/Recovery/DownloadStateManager.cs`

**Requisitos:**
- Salvar em `~/.local/share/Cutube/state/{stateId}.json`
- Salvar a cada 10% de progresso
- Thread-safe (lock para concorrência)
- Auto-cleanup de estados antigos (> 7 dias)

```csharp
public class DownloadStateManager : IDownloadStateManager
{
    private readonly string _stateDirectory;
    private readonly ILoggerService _logger;
    private readonly object _lock = new();

    public DownloadStateManager(IEnvironmentService environment, ILoggerService logger)
    {
        _stateDirectory = Path.Combine(
            environment.GetLocalSharePath(),
            "Cutube",
            "state"
        );
        Directory.CreateDirectory(_stateDirectory);
        _logger = logger;
    }

    public async Task<string> CreateStateAsync(DownloadState state)
    {
        lock (_lock)
        {
            var filePath = Path.Combine(_stateDirectory, $"{state.StateId}.json");
            var json = JsonSerializer.Serialize(state, _jsonOptions);
            File.WriteAllText(filePath, json);
            return state.StateId;
        }
    }

    // ... demais métodos
}
```

**Critérios:**
- [ ] Estados salvos em JSON
- [ ] Thread-safe com lock
- [ ] Logger integrado
- [ ] Testes unitários

---

#### Tarefa 1.3.4: Integrar no YtDlpHelper (3 horas)

**Modificações em `cutube/YtDlpHelper.cs`:**

**Adicionar ao construtor:**
```csharp
private readonly IDownloadStateManager _stateManager;

public YtDlpHelper(
    // ... dependências existentes
    IDownloadStateManager stateManager,
    ILoggerService loggerService)
{
    // ... injeções existentes
    _stateManager = stateManager;
    _logger = loggerService;
}
```

**Atualizar método DownloadAsync:**
```csharp
public async Task<Result<string>> DownloadAsync(MenuInput input, CancellationToken ct)
{
    // Criar estado
    var state = new DownloadState
    {
        Url = input.Url,
        OutputPath = input.OutputPath,
        StartTime = input.StartTime,
        EndTime = input.EndTime,
        AudioOnly = input.AudioOnly,
        Status = DownloadStatus.Downloading
    };

    var stateId = await _stateManager.CreateStateAsync(state);

    // Hook de progresso
    Progress<DownloadProgress> progress = new(progressData =>
    {
        // Atualizar estado a cada 10%
        if (progressData.ProgressPercentage % 10 == 0)
        {
            _stateManager.UpdateStateAsync(stateId, s => {
                s.ProgressPercent = progressData.ProgressPercentage;
            }).Wait();
        }
    });

    try
    {
        // download logic...

        await _stateManager.MarkCompletedAsync(stateId);
        return Result<string>.Success(outputPath);
    }
    catch (Exception ex)
    {
        await _stateManager.MarkFailedAsync(stateId, ex.Message);
        throw;
    }
}
```

**Critérios:**
- [ ] Estado criado no início
- [ ] Progresso salvo a cada 10%
- [ ] Falha registrada no estado

---

#### Tarefa 1.3.5: Implementar comando --resume (2 horas)

**Arquivo:** `cutube/Program.cs`

**Adicionar opção:**
```csharp
if (args.Contains("--resume"))
{
    await ResumeLastDownload(cancellationToken);
    return;
}

private static async Task ResumeLastDownload(CancellationToken ct)
{
    var activeStates = await _stateManager.GetActiveStatesAsync();

    if (activeStates.Count == 0)
    {
        _consoleService.WriteLine("✓ Nenhum download para resumir.");
        return;
    }

    var lastState = activeStates.OrderByDescending(s => s.UpdatedAt).First();

    _consoleService.WriteLine($"▶ Retomando download: {lastState.Url}");
    _consoleService.WriteLine($"  Progresso: {lastState.ProgressPercent}%");

    // Lógica de resume...
}
```

**Critérios:**
- [ ] Flag `--resume` implementada
- [ ] Lista estados disponíveis
- [ ] Permite selecionar qual resumir

---

#### Tarefa 1.3.6: StateCleanupService (2 horas)

**Arquivo:** `cutube/Recovery/StateCleanupService.cs`

**Requisitos:**
- Executar cleanup no startup
- Remover estados > 7 dias
- Remover arquivos temporários órfãos
- Log do que foi removido

```csharp
public class StateCleanupService
{
    public async Task CleanupAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var removed = await _stateManager.CleanupOldStatesAsync(TimeSpan.FromDays(7));
        _logger.LogInfo($"Cleanup: {removed} estados antigos removidos");
    }
}
```

**Critérios:**
- [ ] Cleanup automático no startup
- [ ] Log do que foi removido
- [ ] Configurável (7 dias padrão)

---

#### Tarefa 1.3.7: Testes unitários (3 horas)

**Arquivo:** `Cutube.Tests/Unit/Recovery/DownloadStateManagerTests.cs`

**Cenários:**
- [ ] CreateState salva arquivo JSON
- [ ] GetState recupera estado corretamente
- [ ] UpdateState modifica estado atomically
- [ ] CleanupOldStates remove arquivos antigos
- [ ] Concorrência (lock) funciona

**Critérios:**
- [ ] Todos os testes passam
- [ ] Mock de filesystem

---

### Checklist Fase 1.3

- [ ] DownloadState model criado
- [ ] IDownloadStateManager interface criada
- [ ] DownloadStateManager implementado
- [ ] Integração com YtDlpHelper completa
- [ ] Comando --resume funcional
- [ ] Cleanup automático implementado
- [ ] Testes unitários criados e passando
- [ ] `dotnet test` passa (100%)
- [ ] `dotnet build` sem warnings

---

## ✅ Critérios de Aceite do Épico 1

O Épico 1 será considerado completo quando:

### Funcional
- [ ] Logs são escritos em `~/.local/share/Cutube/logs/` em JSON
- [ ] Logs contém: timestamp, level, message, exception, context
- [ ] Rotação automática de logs (10MB, 7 dias)
- [ ] Erros de rede trigger retry automático (3 tentativas)
- [ ] Erros de disco logam e continuam operando
- [ ] Erros críticos oferecem solução ao usuário
- [ ] Usuário vê mensagens amigáveis, nunca stack traces
- [ ] Download interrompido pode ser retomado com `--resume`
- [ ] Estado persistido em `~/.local/share/Cutube/state/`

### Técnico
- [ ] `dotnet test` - 100% dos testes passando
- [ ] `dotnet build` - ZERO warnings
- [ ] Todas as interfaces seguem padrão I-prefix
- [ ] DI mantida (constructor injection)
- [ ] Retrocompatibilidade mantida (CLI ainda funciona)

### Qualidade
- [ ] Cobertura de testes > 80% para código novo
- [ ] Code review aprovado
- [ ] Documentação atualizada

---

## 📊 Dependências entre Tarefas

```
Fase 1.1 (Logging)
├─ 1.1.1 → 1.1.2 → 1.1.3 → 1.1.4 → 1.1.5 → 1.1.6
└─ Conclusão: Base pronta para Fase 1.2

Fase 1.2 (Error Handler)
├─ 1.2.1 → 1.2.2 → 1.2.3 → 1.2.4 → 1.2.5 → 1.2.6 → 1.2.7 → 1.2.8
├─ Depende de: Fase 1.1 (usa ILoggerService)
└─ Conclusão: Base pronta para Fase 1.3

Fase 1.3 (Recovery)
├─ 1.3.1 → 1.3.2 → 1.3.3 → 1.3.4 → 1.3.5 → 1.3.6 → 1.3.7
├─ Depende de: Fase 1.1 (ILoggerService) e Fase 1.2 (Result<T>)
└─ Conclusão: Épico 1 completo!
```

---

## 🎯 Próximos Passos

Após completar Épico 1:

1. ✅ **Marcar Épico 1 como completo** no plano macro
2. 🚀 **Iniciar Épico 2** - Arquitetura Híbrida CLI + API
3. 📝 **Documentar aprendizados** no README

---

## 📁 Estrutura de Arquivos Final (Épico 1)

```
cutube/
├── Logging/
│   ├── ILoggerService.cs          # Interface
│   ├── FileLoggerService.cs       # Implementação
│   ├── LogEntry.cs                # Modelo
│   └── LogLevel.cs                # Enum
│
├── ErrorHandling/
│   ├── IErrorHandler.cs           # Interface
│   ├── ErrorHandler.cs            # Implementação
│   ├── Result.cs                  # Result<T> pattern
│   ├── ErrorType.cs               # Enum de tipos de erro
│   └── RetryPolicy.cs             # Lógica de retry
│
├── Recovery/
│   ├── IDownloadStateManager.cs   # Interface
│   ├── DownloadStateManager.cs    # Implementação
│   ├── DownloadState.cs           # Modelo de estado
│   └── StateCleanupService.cs     # Cleanup automático
│
├── Program.cs                      # Modificado (injeta logger)
├── ProgramWorkflow.cs              # Modificado (usa Result<T>)
├── YtDlpHelper.cs                  # Modificado (retry + state)
└── FfmpegHelper.cs                 # Modificado (sem stack traces)

Cutube.Tests/
├── Unit/
│   ├── Logging/
│   │   └── FileLoggerServiceTests.cs
│   ├── ErrorHandling/
│   │   └── ErrorHandlerTests.cs
│   └── Recovery/
│       └── DownloadStateManagerTests.cs
```

---

**Criado em:** 04/02/2026
**Status:** ✅ Pronto para implementação
**Estimativa:** 7-10 dias
**Baseado em:** `plano-macro-sistema-distribuido.md` - Épico 1