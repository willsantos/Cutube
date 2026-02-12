using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cutube.Worker.Services;

public class WorkerHealthReporterService : BackgroundService
{
    private const string HealthFilePath = "/tmp/cutube-worker-health";

    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<WorkerHealthReporterService> _logger;

    public WorkerHealthReporterService(
        HealthCheckService healthCheckService,
        ILogger<WorkerHealthReporterService> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker health reporter started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var report = await _healthCheckService.CheckHealthAsync(stoppingToken);
                var isHealthy = report.Status == HealthStatus.Healthy;

                await File.WriteAllTextAsync(
                    HealthFilePath,
                    isHealthy ? "healthy" : "unhealthy",
                    stoppingToken);

                if (!isHealthy)
                {
                    _logger.LogWarning("Worker health is unhealthy: {HealthStatus}", report.Status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update worker health file");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await File.WriteAllTextAsync(HealthFilePath, "unhealthy", cancellationToken);
        }
        catch
        {
            // Best effort on shutdown.
        }

        await base.StopAsync(cancellationToken);
    }
}
