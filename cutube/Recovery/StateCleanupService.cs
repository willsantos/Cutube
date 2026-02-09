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
