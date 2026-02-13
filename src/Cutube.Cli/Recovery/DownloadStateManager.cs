using Cutube.Cli.Logging;
using System.Text.Json;

namespace Cutube.Cli.Recovery;

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
            environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Cutube",
            "state"
        );

        if (!Directory.Exists(_stateDirectory))
        {
            Directory.CreateDirectory(_stateDirectory);
            logger.LogInfo("Created state directory", ("path", _stateDirectory));
        }

        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    public Task<string> CreateStateAsync(DownloadState state)
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

            return Task.FromResult(state.StateId);
        }
    }

    public Task<DownloadState?> GetStateAsync(string stateId)
    {
        lock (_lock)
        {
            var filePath = GetStateFilePath(stateId);

            if (!File.Exists(filePath))
            {
                _logger.LogDebug("State not found", ("stateId", stateId));
                return Task.FromResult<DownloadState?>(null);
            }

            var json = File.ReadAllText(filePath);
            var state = JsonSerializer.Deserialize<DownloadState>(json, _jsonOptions);

            return Task.FromResult(state);
        }
    }

    public Task UpdateStateAsync(string stateId, Action<DownloadState> update)
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

            return Task.CompletedTask;
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

        _logger.LogError(new Exception(errorMessage), "State marked as failed", ("stateId", stateId));
    }

    public async Task MarkCancelledAsync(string stateId)
    {
        await UpdateStateAsync(stateId, state =>
        {
            state.Status = DownloadStatus.Cancelled;
        });

        _logger.LogInfo("State marked as cancelled", ("stateId", stateId));
    }

    public Task DeleteStateAsync(string stateId)
    {
        lock (_lock)
        {
            var filePath = GetStateFilePath(stateId);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInfo("State deleted", ("stateId", stateId));
            }

            return Task.CompletedTask;
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

    public Task<List<DownloadState>> GetAllStatesAsync()
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

            return Task.FromResult(states.OrderByDescending(s => s.UpdatedAt).ToList());
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
