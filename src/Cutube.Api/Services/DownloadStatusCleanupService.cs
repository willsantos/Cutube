namespace Cutube.Api.Services;

/// <summary>
/// Background service for cleaning up old download records
/// </summary>
public class DownloadStatusCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DownloadStatusCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
    private readonly TimeSpan _maxAge = TimeSpan.FromHours(24);

    public DownloadStatusCleanupService(
        IServiceProvider serviceProvider,
        ILogger<DownloadStatusCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DownloadStatusCleanupService started. Cleanup interval: {Interval}, Max age: {MaxAge}",
            _cleanupInterval, _maxAge);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IDownloadStatusRepository>();

                var removedCount = await repository.CleanupOldRecordsAsync(_maxAge, stoppingToken);

                if (removedCount > 0)
                {
                    _logger.LogInformation("Cleanup removed {Count} old download records", removedCount);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }
        }

        _logger.LogInformation("DownloadStatusCleanupService stopped");
    }
}
