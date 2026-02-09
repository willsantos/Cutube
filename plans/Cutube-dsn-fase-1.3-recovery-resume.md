# Plano de Implementação - Fase 1.3: Recovery & Resume

## Objetivo
Implementar persistência de estado de downloads para permitir retomada de downloads interrompidos (crash, network failure, CTRL+C) com comando `--resume`.

## Problema Atual
- ❌ Downloads interrompidos precisam começar do zero
- ❌ Sem persistência de progresso
- ❌ Impossível retomar após crash
- ❌ Arquivos temporários ficam órfãos
- ❌ Usuário perde tempo e banda

## Solução
Implementar State Persistence com:
- **DownloadState**: Modelo de estado serializável em JSON
- **DownloadStateManager**: CRUD de estados com persistência
- **Comando --resume**: Lista e retoma downloads interrompidos
- **Auto-cleanup**: Remove estados antigos e arquivos órfãos
- **Integração YtDlp**: Salva progresso durante download

## Branch
`feature/Cutube-dsn-fase-1.3-recovery-resume`

## Tarefas Incluídas
- ✅ **Cutube-dsn.23**: Fase 1.3: Recovery & Resume (agrupador)
- ✅ **Cutube-dsn.18**: Criar modelos de estado (DownloadState)
- ✅ **Cutube-dsn.19**: Criar IDownloadStateManager interface
- ✅ **Cutube-dsn.17**: Implementar DownloadStateManager
- ✅ **Cutube-dsn.24**: Integrar DownloadStateManager no YtDlpHelper
- ✅ **Cutube-dsn.20**: Implementar comando --resume
- ✅ **Cutube-dsn.22**: Criar StateCleanupService
- ✅ **Cutube-dsn.21**: Testes unitários para Recovery

**Depende de:** Fase 1.1 (ILoggerService) e Fase 1.2 (Result<T>)

---

## Estrutura Final

```
cutube/
├── Recovery/
│   ├── DownloadState.cs            # Modelo de estado
│   ├── DownloadStatus.cs           # Enum de status
│   ├── IDownloadStateManager.cs    # Interface
│   ├── DownloadStateManager.cs     # Implementação com persistência
│   └── StateCleanupService.cs      # Cleanup automático
├── ErrorHandling/
│   └── Result.cs                   # Usado pelo StateManager
├── Logging/
│   └── ILoggerService.cs           # Usado pelo StateManager
├── Program.cs                       # Modificado (comando --resume)
└── YtDlpHelper.cs                   # Modificado (salva estado durante download)

Cutube.Tests/
└── Unit/
    └── Recovery/
        ├── DownloadStateManagerTests.cs
        └── StateCleanupServiceTests.cs
```

---

## Commits Planejados

### 1. `feat(recovery): criar modelos de estado (DownloadState)` (Cutube-dsn.18)

**Arquivos NOVOS:**

#### `cutube/Recovery/DownloadStatus.cs`
```csharp
namespace Cutube.Recovery;

/// <summary>
/// Status de um download em andamento
/// </summary>
public enum DownloadStatus
{
    /// <summary>
    /// Download criado mas ainda não iniciado
    /// </summary>
    Pending,

    /// <summary>
    /// Baixando conteúdo do YouTube
    /// </summary>
    Downloading,

    /// <summary>
    /// Processando com FFmpeg (corte/extração áudio)
    /// </summary>
    Processing,

    /// <summary>
    /// Download concluído com sucesso
    /// </summary>
    Completed,

    /// <summary>
    /// Download falhou (pode ser retomado)
    /// </summary>
    Failed,

    /// <summary>
    /// Download cancelado pelo usuário (CTRL+C)
    /// </summary>
    Cancelled
}
```

#### `cutube/Recovery/DownloadState.cs`
```csharp
using System.Text.Json.Serialization;

namespace Cutube.Recovery;

/// <summary>
/// Estado persistente de um download para recuperação
/// </summary>
public class DownloadState
{
    /// <summary>
    /// Identificador único do estado (GUID)
    /// </summary>
    public required string StateId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// URL do vídeo do YouTube
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Caminho de saída final do arquivo
    /// </summary>
    public required string OutputPath { get; set; }

    /// <summary>
    /// Tempo de início do recorte (opcional)
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Tempo de fim do recorte (opcional)
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Flag se é download de áudio apenas
    /// </summary>
    public bool AudioOnly { get; set; }

    /// <summary>
    /// Status atual do download
    /// </summary>
    public DownloadStatus Status { get; set; }

    /// <summary>
    /// Progresso atual (0-100)
    /// </summary>
    public int ProgressPercent { get; set; }

    /// <summary>
    /// Caminho do arquivo temporário (se existir)
    /// </summary>
    public string? TempFilePath { get; set; }

    /// <summary>
    /// Mensagem de erro (se falhou)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Data/hora de criação do estado
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Data/hora da última atualização
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Duração total do download em segundos (se conhecido)
    /// </summary>
    public int? TotalDurationSeconds { get; set; }

    /// <summary>
    /// Tamanho baixado em bytes
    /// </summary>
    public long DownloadedBytes { get; set; }

    /// <summary>
    /// Tamanho total em bytes (se conhecido)
    /// </summary>
    public long? TotalBytes { get; set; }
}
```

**Critérios:**
- [ ] DownloadStatus enum criado
- [ ] DownloadState classe criada
- [ ] Propriedades com XML comments
- [ ] JsonPropertyName para snake_case nos campos de data
- [ ] Compila sem erros

---

### 2. `feat(recovery): criar IDownloadStateManager interface` (Cutube-dsn.19)

**Arquivo NOVO:** `cutube/Recovery/IDownloadStateManager.cs`

```csharp
namespace Cutube.Recovery;

/// <summary>
/// Serviço de gerenciamento de estado de downloads com persistência
/// </summary>
public interface IDownloadStateManager
{
    /// <summary>
    /// Cria novo estado de download
    /// </summary>
    Task<string> CreateStateAsync(DownloadState state);

    /// <summary>
    /// Recupera estado por ID
    /// </summary>
    Task<DownloadState?> GetStateAsync(string stateId);

    /// <summary>
    /// Atualiza estado de forma atômica (lock)
    /// </summary>
    Task UpdateStateAsync(string stateId, Action<DownloadState> update);

    /// <summary>
    /// Lista todos os estados ativos (não completados)
    /// </summary>
    Task<List<DownloadState>> GetActiveStatesAsync();

    /// <summary>
    /// Marca estado como completado
    /// </summary>
    Task MarkCompletedAsync(string stateId);

    /// <summary>
    /// Marca estado como falho com mensagem de erro
    /// </summary>
    Task MarkFailedAsync(string stateId, string errorMessage);

    /// <summary>
    /// Marca estado como cancelado
    /// </summary>
    Task MarkCancelledAsync(string stateId);

    /// <summary>
    /// Remove estado do disco
    /// </summary>
    Task DeleteStateAsync(string stateId);

    /// <summary>
    /// Limpa estados antigos (baseado em idade)
    /// </summary>
    Task<int> CleanupOldStatesAsync(TimeSpan maxAge);

    /// <summary>
    /// Lista todos os estados (para debug/admin)
    /// </summary>
    Task<List<DownloadState>> GetAllStatesAsync();
}
```

**Critérios:**
- [ ] Interface IDownloadStateManager criada
- [ ] Métodos CRUD definidos
- [ ] Update via Action (atomic)
- [ ] Cleanup por idade
- [ ] XML comments completos

---

### 3. `feat(recovery): implementar DownloadStateManager` (Cutube-dsn.17)

**Arquivo NOVO:** `cutube/Recovery/DownloadStateManager.cs`

```csharp
using Cutube.Interfaces;
using Cutube.Logging;
using System.Text.Json;

namespace Cutube.Recovery;

/// <summary>
/// Implementação de gerenciador de estado com persistência em JSON
/// </summary>
public class DownloadStateManager : IDownloadStateManager
{
    private readonly string _stateDirectory;
    private readonly ILoggerService _logger;
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _jsonOptions;

    public DownloadStateManager(IEnvironmentService environment, ILoggerService logger)
    {
        _stateDirectory = Path.Combine(
            environment.GetLocalSharePath(),
            "Cutube",
            "state"
        );

        if (!Directory.Exists(_stateDirectory))
        {
            Directory.CreateDirectory(_stateDirectory);
            _logger.LogInfo("Created state directory", ("path", _stateDirectory));
        }

        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    public async Task<string> CreateStateAsync(DownloadState state)
    {
        lock (_lock)
        {
            state.UpdatedAt = DateTime.UtcNow;

            var filePath = GetStateFilePath(state.StateId);
            var json = JsonSerializer.Serialize(state, _jsonOptions);
            File.WriteAllText(filePath, json);

            _logger.LogInfo("State created",
                ("stateId", state.StateId),
                ("url", state.Url),
                ("output", state.OutputPath)
            );

            return state.StateId;
        }
    }

    public async Task<DownloadState?> GetStateAsync(string stateId)
    {
        lock (_lock)
        {
            var filePath = GetStateFilePath(stateId);

            if (!File.Exists(filePath))
            {
                _logger.LogDebug("State not found", ("stateId", stateId));
                return null;
            }

            var json = File.ReadAllText(filePath);
            var state = JsonSerializer.Deserialize<DownloadState>(json, _jsonOptions);

            return state;
        }
    }

    public async Task UpdateStateAsync(string stateId, Action<DownloadState> update)
    {
        lock (_lock)
        {
            var filePath = GetStateFilePath(stateId);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"State not found: {stateId}");
            }

            var json = File.ReadAllText(filePath);
            var state = JsonSerializer.Deserialize<DownloadState>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize state");

            // Aplicar atualização
            update(state);
            state.UpdatedAt = DateTime.UtcNow;

            // Salvar de volta
            var updatedJson = JsonSerializer.Serialize(state, _jsonOptions);
            File.WriteAllText(filePath, updatedJson);

            _logger.LogDebug("State updated", ("stateId", stateId));
        }
    }

    public async Task<List<DownloadState>> GetActiveStatesAsync()
    {
        return await GetStatesByStatusAsync(
            DownloadStatus.Pending,
            DownloadStatus.Downloading,
            DownloadStatus.Processing,
            DownloadStatus.Failed,
            DownloadStatus.Cancelled
        );
    }

    public async Task MarkCompletedAsync(string stateId)
    {
        await UpdateStateAsync(stateId, state =>
        {
            state.Status = DownloadStatus.Completed;
            state.ProgressPercent = 100;
            state.TempFilePath = null; // Limpa referência ao temp
        });

        _logger.LogInfo("State marked as completed", ("stateId", stateId));
    }

    public async Task MarkFailedAsync(string stateId, string errorMessage)
    {
        await UpdateStateAsync(stateId, state =>
        {
            state.Status = DownloadStatus.Failed;
            state.ErrorMessage = errorMessage;
        });

        _logger.LogError("State marked as failed", errorMessage, ("stateId", stateId));
    }

    public async Task MarkCancelledAsync(string stateId)
    {
        await UpdateStateAsync(stateId, state =>
        {
            state.Status = DownloadStatus.Cancelled;
        });

        _logger.LogInfo("State marked as cancelled", ("stateId", stateId));
    }

    public async Task DeleteStateAsync(string stateId)
    {
        lock (_lock)
        {
            var filePath = GetStateFilePath(stateId);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInfo("State deleted", ("stateId", stateId));
            }
        }
    }

    public async Task<int> CleanupOldStatesAsync(TimeSpan maxAge)
    {
        var cutoff = DateTime.UtcNow.Subtract(maxAge);
        var allStates = await GetAllStatesAsync();
        var oldStates = allStates.Where(s => s.UpdatedAt < cutoff).ToList();

        foreach (var state in oldStates)
        {
            // Se ainda tem temp file, remove
            if (!string.IsNullOrEmpty(state.TempFilePath) && File.Exists(state.TempFilePath))
            {
                try
                {
                    File.Delete(state.TempFilePath);
                    _logger.LogInfo("Deleted orphaned temp file", ("path", state.TempFilePath));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to delete temp file: {ex.Message}");
                }
            }

            await DeleteStateAsync(state.StateId);
        }

        _logger.LogInfo("Cleanup completed", ("removed_count", oldStates.Count));
        return oldStates.Count;
    }

    public async Task<List<DownloadState>> GetAllStatesAsync()
    {
        lock (_lock)
        {
            var stateFiles = Directory.GetFiles(_stateDirectory, "*.json");
            var states = new List<DownloadState>();

            foreach (var file in stateFiles)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var state = JsonSerializer.Deserialize<DownloadState>(json, _jsonOptions);
                    if (state != null)
                    {
                        states.Add(state);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load state file", ("path", file));
                }
            }

            return states.OrderByDescending(s => s.UpdatedAt).ToList();
        }
    }

    private string GetStateFilePath(string stateId)
    {
        return Path.Combine(_stateDirectory, $"{stateId}.json");
    }

    private async Task<List<DownloadState>> GetStatesByStatusAsync(params DownloadStatus[] statuses)
    {
        var allStates = await GetAllStatesAsync();
        return allStates.Where(s => statuses.Contains(s.Status)).ToList();
    }
}
```

**Critérios:**
- [ ] Implementa IDownloadStateManager
- [ ] Thread-safe com lock
- [ ] Persistência em JSON
- [ ] Integração com ILoggerService
- [ ] Cleanup de arquivos órfãos

---

### 4. `feat(recovery): integrar DownloadStateManager no YtDlpHelper` (Cutube-dsn.24)

**Arquivo MODIFICADO:** `cutube/YtDlpHelper.cs`

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

**Modificar método DownloadAsync:**
```csharp
public async Task<Result<string>> DownloadAsync(MenuInput input, CancellationToken ct)
{
    // 1. Criar estado inicial
    var state = new DownloadState
    {
        Url = input.Url,
        OutputPath = input.OutputPath,
        StartTime = input.StartTime,
        EndTime = input.EndTime,
        AudioOnly = input.AudioOnly,
        Status = DownloadStatus.Downloading,
        ProgressPercent = 0
    };

    var stateId = await _stateManager.CreateStateAsync(state);
    _logger.LogInfo("Download state created", ("stateId", stateId));

    // 2. Hook de progresso para salvar estado
    Progress<DownloadProgress> progress = new(progressData =>
    {
        // Atualizar estado a cada 10%
        if (progressData.ProgressPercentage % 10 == 0)
        {
            _stateManager.UpdateStateAsync(stateId, s =>
            {
                s.ProgressPercent = progressData.ProgressPercentage;
                s.DownloadedBytes = progressData.BytesDownloaded;
                s.TotalBytes = progressData.TotalBytes;
            }).Wait();
        }
    });

    try
    {
        // 3. Download (lógica existente)
        string outputPath;
        if (input.StartTime.HasValue && input.EndTime.HasValue)
        {
            outputPath = await DownloadWithTimeRangeAsync(input, progress, ct);
        }
        else if (input.AudioOnly)
        {
            outputPath = await DownloadAudioAsync(input, progress, ct);
        }
        else
        {
            outputPath = await DownloadVideoAsync(input, progress, ct);
        }

        // 4. Marcar como completado
        await _stateManager.MarkCompletedAsync(stateId);
        _logger.LogInfo("Download completed successfully", ("stateId", stateId));

        return Result<string>.Success(outputPath);
    }
    catch (OperationCanceledException)
    {
        // CTRL+C
        await _stateManager.MarkCancelledAsync(stateId);
        _logger.LogWarning("Download cancelled by user");
        throw;
    }
    catch (Exception ex)
    {
        // Erro durante download
        await _stateManager.MarkFailedAsync(stateId, ex.Message);
        _logger.LogError(ex, "Download failed", ("stateId", stateId));
        return Result<string>.Failure(ErrorType.Network, "Download failed", ex);
    }
}
```

**Critérios:**
- [ ] IDownloadStateManager injetado
- [ ] Estado criado no início
- [ ] Progresso salvo a cada 10%
- [ ] Falha/Cancelamento registrados
- [ ] Sucesso marca como completado

---

### 5. `feat(recovery): implementar comando --resume` (Cutube-dsn.20)

**Arquivo MODIFICADO:** `cutube/Program.cs`

**Adicionar no Main:**
```csharp
if (args.Contains("--resume"))
{
    await ResumeDownload(cancellationToken);
    return;
}

// ... resto do código

private static async Task ResumeDownload(CancellationToken ct)
{
    var activeStates = await _stateManager.GetActiveStatesAsync();

    if (activeStates.Count == 0)
    {
        _consoleService.WriteLine("✓ Nenhum download para resumir.");
        return;
    }

    _consoleService.WriteLine($"\n📋 Downloads interrompidos ({activeStates.Count}):");

    for (int i = 0; i < activeStates.Count; i++)
    {
        var state = activeStates[i];
        _consoleService.WriteLine($"\n  [{i + 1}] {state.Url}");
        _consoleService.WriteLine($"      Status: {GetStatusEmoji(state.Status)} {state.Status}");
        _consoleService.WriteLine($"      Progresso: {state.ProgressPercent}%");
        _consoleService.WriteLine($"      Saída: {state.OutputPath}");

        if (!string.IsNullOrEmpty(state.ErrorMessage))
        {
            _consoleService.WriteLine($"      Erro: {state.ErrorMessage}");
        }

        _consoleService.WriteLine($"      Atualizado: {state.UpdatedAt:yyyy-MM-dd HH:mm}");
    }

    _consoleService.Write("\nDigite o número do download para resumir (0 para cancelar): ");
    var input = Console.ReadLine();

    if (!int.TryParse(input, out var selection) || selection < 1 || selection > activeStates.Count)
    {
        _consoleService.WriteLine("✓ Nenhum download selecionado.");
        return;
    }

    var selectedState = activeStates[selection - 1];
    _consoleService.WriteLine($"\n▶ Retomando download: {selectedState.Url}");

    // Recriar MenuInput do estado
    var menuInput = new MenuInput
    {
        Url = selectedState.Url,
        OutputPath = selectedState.OutputPath,
        StartTime = selectedState.StartTime,
        EndTime = selectedState.EndTime,
        AudioOnly = selectedState.AudioOnly
    };

    // Executar download normalmente (YtDlpHelper reusa estado se existir)
    await _ytdlpService.DownloadAsync(menuInput, ct);
}

private static string GetStatusEmoji(DownloadStatus status) => status switch
{
    DownloadStatus.Pending => "⏳",
    DownloadStatus.Downloading => "⬇️ ",
    DownloadStatus.Processing => "⚙️ ",
    DownloadStatus.Completed => "✅",
    DownloadStatus.Failed => "❌",
    DownloadStatus.Cancelled => "⏸️ ",
    _ => "❓"
};
```

**Critérios:**
- [ ] Flag --resume implementada
- [ ] Lista estados ativos
- [ ] Permite selecionar qual resumir
- [ ] Interface amigável

---

### 6. `feat(recovery): criar StateCleanupService` (Cutube-dsn.22)

**Arquivo NOVO:** `cutube/Recovery/StateCleanupService.cs`

```csharp
using Cutube.Logging;

namespace Cutube.Recovery;

/// <summary>
/// Serviço de limpeza automática de estados antigos e arquivos órfãos
/// </summary>
public class StateCleanupService
{
    private readonly IDownloadStateManager _stateManager;
    private readonly ILoggerService _logger;
    private readonly TimeSpan _defaultMaxAge = TimeSpan.FromDays(7);

    public StateCleanupService(
        IDownloadStateManager stateManager,
        ILoggerService logger)
    {
        _stateManager = stateManager;
        _logger = logger;
    }

    /// <summary>
    /// Executa cleanup de estados antigos
    /// </summary>
    public async Task<CleanupResult> CleanupAsync(TimeSpan? maxAge = null)
    {
        var age = maxAge ?? _defaultMaxAge;

        _logger.LogInfo("Starting state cleanup", ("max_age_days", age.Days));

        var removedCount = await _stateManager.CleanupOldStatesAsync(age);

        var result = new CleanupResult
        {
            RemovedStatesCount = removedCount,
            MaxAge = age,
            ExecutedAt = DateTime.UtcNow
        };

        _logger.LogInfo("Cleanup completed",
            ("removed_count", removedCount),
            ("max_age_days", age.Days)
        );

        return result;
    }

    /// <summary>
    /// Executa cleanup no startup da aplicação
    /// </summary>
    public async Task CleanupOnStartupAsync()
    {
        try
        {
            var result = await CleanupAsync();
            _logger.LogInfo("Startup cleanup completed", ("removed", result.RemovedStatesCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Startup cleanup failed (non-fatal)");
            // Não falha aplicação se cleanup falhar
        }
    }
}

/// <summary>
/// Resultado da operação de cleanup
/// </summary>
public class CleanupResult
{
    public int RemovedStatesCount { get; init; }
    public TimeSpan MaxAge { get; init; }
    public DateTime ExecutedAt { get; init; }
}
```

**Integrar no Program.cs (Main):**
```csharp
// No início do Main, após criar logger
var stateCleanupService = new StateCleanupService(stateManager, loggerService);
await stateCleanupService.CleanupOnStartupAsync();
```

**Critérios:**
- [ ] StateCleanupService criado
- [ ] Cleanup executado no startup
- [ ] Não falha aplicação se cleanup falhar
- [ ] Log do que foi removido

---

### 7. `test(recovery): testes unitários para Recovery` (Cutube-dsn.21)

**Arquivos NOVOS:**

#### `Cutube.Tests/Unit/Recovery/DownloadStateManagerTests.cs`
```csharp
using Cutube.Interfaces;
using Cutube.Logging;
using Cutube.Recovery;
using Moq;
using Xunit;

namespace Cutube.Tests.Unit.Recovery;

public class DownloadStateManagerTests : IDisposable
{
    private readonly DownloadStateManager _manager;
    private readonly MockEnvironmentService _envService;
    private readonly Mock<ILoggerService> _loggerMock;
    private readonly string _testStatePath;

    public DownloadStateManagerTests()
    {
        _testStatePath = Path.Combine(Path.GetTempPath(), $"cutube-state-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testStatePath);

        _envService = new MockEnvironmentService(_testStatePath);
        _loggerMock = new Mock<ILoggerService>();

        _manager = new DownloadStateManager(_envService, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateState_SavesJsonFile()
    {
        // Arrange
        var state = new DownloadState
        {
            Url = "https://youtube.com/watch?v=test",
            OutputPath = "/tmp/video.mp4",
            Status = DownloadStatus.Downloading
        };

        // Act
        var stateId = await _manager.CreateStateAsync(state);

        // Assert
        var filePath = Path.Combine(_testStatePath, $"{stateId}.json");
        Assert.True(File.Exists(filePath));

        var content = await File.ReadAllTextAsync(filePath);
        Assert.Contains("https://youtube.com/watch?v=test", content);
    }

    [Fact]
    public async Task GetState_ReturnsNull_WhenNotFound()
    {
        // Act
        var state = await _manager.GetStateAsync("nonexistent");

        // Assert
        Assert.Null(state);
    }

    [Fact]
    public async Task GetState_ReturnsState_WhenExists()
    {
        // Arrange
        var original = new DownloadState
        {
            Url = "https://youtube.com/watch?v=test",
            OutputPath = "/tmp/video.mp4",
            Status = DownloadStatus.Downloading
        };
        var stateId = await _manager.CreateStateAsync(original);

        // Act
        var retrieved = await _manager.GetStateAsync(stateId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(stateId, retrieved.StateId);
        Assert.Equal("https://youtube.com/watch?v=test", retrieved.Url);
    }

    [Fact]
    public async Task UpdateState_ModifiesState_Atomically()
    {
        // Arrange
        var state = new DownloadState
        {
            Url = "https://youtube.com/watch?v=test",
            OutputPath = "/tmp/video.mp4",
            Status = DownloadStatus.Downloading,
            ProgressPercent = 0
        };
        var stateId = await _manager.CreateStateAsync(state);

        // Act
        await _manager.UpdateStateAsync(stateId, s =>
        {
            s.ProgressPercent = 50;
            s.Status = DownloadStatus.Processing;
        });

        // Assert
        var updated = await _manager.GetStateAsync(stateId);
        Assert.Equal(50, updated!.ProgressPercent);
        Assert.Equal(DownloadStatus.Processing, updated.Status);
    }

    [Fact]
    public async Task MarkCompleted_UpdatesStatusAndProgress()
    {
        // Arrange
        var state = new DownloadState
        {
            Url = "https://youtube.com/watch?v=test",
            OutputPath = "/tmp/video.mp4",
            Status = DownloadStatus.Downloading
        };
        var stateId = await _manager.CreateStateAsync(state);

        // Act
        await _manager.MarkCompletedAsync(stateId);

        // Assert
        var completed = await _manager.GetStateAsync(stateId);
        Assert.Equal(DownloadStatus.Completed, completed!.Status);
        Assert.Equal(100, completed.ProgressPercent);
    }

    [Fact]
    public async Task CleanupOldStates_RemovesFiles_ThanMaxAge()
    {
        // Arrange
        var oldState = new DownloadState
        {
            Url = "https://youtube.com/watch?v=old",
            OutputPath = "/tmp/old.mp4",
            Status = DownloadStatus.Failed
        };
        var stateId = await _manager.CreateStateAsync(oldState);

        // Modificar UpdatedAt para simular estado antigo
        var filePath = Path.Combine(_testStatePath, $"{stateId}.json");
        var content = await File.ReadAllTextAsync(filePath);
        content = content.Replace($"\"updated_at\":", $"\"updated_at\":\"{DateTime.UtcNow.AddDays(-10):yyyy-MM-ddTHH:mm:ss.fffZ\"");
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var removed = await _manager.CleanupOldStatesAsync(TimeSpan.FromDays(7));

        // Assert
        Assert.Equal(1, removed);
        Assert.False(File.Exists(filePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testStatePath))
        {
            Directory.Delete(_testStatePath, recursive: true);
        }
    }

    private class MockEnvironmentService : IEnvironmentService
    {
        private readonly string _basePath;

        public MockEnvironmentService(string basePath)
        {
            _basePath = basePath;
        }

        public string GetLocalSharePath() => _basePath;
        // Implementar outros métodos com throw new NotImplementedException()
    }
}
```

**Critérios:**
- [ ] Teste de criação de estado
- [ ] Teste de recuperação de estado
- [ ] Teste de atualização atômica
- [ ] Teste de cleanup de estados antigos
- [ ] Teste de marcação como completado
- [ ] Mock de IEnvironmentService
- [ ] Cleanup no Dispose do teste
- [ ] Todos os testes passam

---

## Critérios de Aceite

### Funcional
- [ ] Estados salvos em `~/.local/share/Cutube/state/` em JSON
- [ ] Progresso salvo a cada 10% durante download
- [ ] Comando `--resume` lista downloads interrompidos
- [ ] Downloads podem ser retomados após crash
- [ ] Arquivos temporários são limpos automaticamente
- [ ] Estados > 7 dias são removidos no startup

### Técnico
- [ ] `dotnet build` - ZERO warnings
- [ ] `dotnet test` - 100% dos testes passando
- [ ] Thread-safe com lock para concorrência
- [ ] Integração com YtDlpHelper completa

### Qualidade
- [ ] Cobertura de testes > 80% para código novo
- [ ] XML comments em interfaces/classes públicas

---

## Testes Manuais Sugeridos

### Teste de criação de estado
```bash
# Iniciar download
dotnet run -- download "https://youtube.com/watch?v=test" --output test.mp4

# Verificar se estado foi criado
ls -la ~/.local/share/Cutube/state/
cat ~/.local/share/Cutube/state/*.json | jq .

# Deveria mostrar:
# {
#   "state_id": "...",
#   "url": "...",
#   "status": "downloading",
#   "progress_percent": 0
# }
```

### Teste de --resume
```bash
# Iniciar download e CTRL+C
dotnet run -- download "https://youtube.com/watch?v=test" --output test.mp4
# (pressionar CTRL+C durante download)

# Listar downloads interrompidos
dotnet run -- --resume

# Output esperado:
# 📋 Downloads interrompidos (1):
#   [1] https://youtube.com/watch?v=test
#       Status: ⏸️ Cancelled
#       Progresso: 45%
#       Saída: test.mp4
#       Atualizado: 2025-02-09 10:30

# Digite o número do download para resumir (0 para cancelar): 1

# Download deve retomar do ponto onde parou
```

### Teste de cleanup
```bash
# Criar estado antigo manualmente
echo '{"state_id":"old-test","url":"https://test","status":"failed","created_at":"2025-02-01T00:00:00Z","updated_at":"2025-02-01T00:00:00Z"}' > ~/.local/share/Cutube/state/old-test.json

# Executar app (deve remover estado antigo no startup)
dotnet run -- --help

# Verificar se foi removido
ls ~/.local/share/Cutube/state/
# Não deve conter old-test.json
```

---

## Notas de Implementação

### JSON Serialization
- Snake_case para compatibilidade com ferramentas externas
- Indented para legibilidade humana
- WriteIndented=false em produção (performance)

### Concorrência
- Lock simples (_lock) para serializar acesso
- UpdateStateAsync recebe Action (atomic)
- File locks do SO previnem corrupção

### Cleanup Strategy
- Estados completados são mantidos por 7 dias (histórico)
- Estados falhos/cancelados por 7 dias
- Arquivos temporários são removidos junto

### Resume Logic
- Download normal começa do zero (estado não existe)
- YtDlpHelper detecta estado existente e retoma
- yt-dlp suporta retomada nativa com -c (continue)

### Performance
- Estado salvo a cada 10% (não a cada KB)
- Async I/O para não bloquear thread
- JSON compacto em produção

---

## Breaking Changes

**Nenhum breaking change**
- Resume é feature opcional (comando --resume)
- Downloads normais continuam funcionando
- Estados são criados automaticamente, transparente para usuário

---

## Dependências

**Fase 1.1 (Logging):**
- ILoggerService usado por StateManager e CleanupService

**Fase 1.2 (Error Handler):**
- Result<T> usado por YtDlpHelper (download com resume)

---

## Estimativa

**Total:** ~20 horas (2-3 dias)
- Cutube-dsn.18 (DownloadState model): 2h
- Cutube-dsn.19 (IDownloadStateManager): 2h
- Cutube-dsn.17 (DownloadStateManager): 4h
- Cutube-dsn.24 (YtDlp integração): 3h
- Cutube-dsn.20 (--resume command): 3h
- Cutube-dsn.22 (StateCleanupService): 2h
- Cutube-dsn.21 (testes): 4h

---

## Commit Message Final

```bash
git add .
git commit -m "feat(recovery): implementar persistência de estado e resume command (Cutube-dsn.17-24)

- Criar modelo DownloadState com persistência em JSON
- Implementar DownloadStateManager com CRUD de estados
- Salvar progresso de download a cada 10%
- Adicionar comando --resume para listar e retomar downloads
- Implementar StateCleanupService para limpeza automática (7 dias)
- Integrar StateManager no YtDlpHelper
- Remover arquivos órfãos durante cleanup
- Estados salvos em ~/.local/share/Cutube/state/

Branch: feature/Cutube-dsn-fase-1.3-recovery-resume
Tarefas: Cutube-dsn.17,18,19,20,21,22,23,24"
```
