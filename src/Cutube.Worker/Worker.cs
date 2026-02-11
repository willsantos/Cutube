using Cutube.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace Cutube.Worker;

/// <summary>
/// Main background service for the Cutube Worker.
/// </summary>
public partial class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkerOptions _options;

    public Worker(
        ILogger<Worker> logger,
        IServiceProvider serviceProvider,
        IOptions<WorkerOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cutube.Worker started at: {Time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Worker is passive - MassTransit consumes messages
            // This loop keeps the service alive
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        _logger.LogInformation("Cutube.Worker stopping at: {Time}", DateTimeOffset.Now);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cutube.Worker is shutting down gracefully...");

        if (_options.EnableGracefulShutdown)
        {
            _logger.LogInformation(
                "Waiting for downloads to finish (timeout: {Timeout}s)...",
                _options.GracefulShutdownTimeoutSeconds);

            // Give time for in-progress downloads to finish
            await Task.Delay(
                TimeSpan.FromSeconds(_options.GracefulShutdownTimeoutSeconds),
                cancellationToken);
        }
        else
        {
            _logger.LogWarning("Graceful shutdown disabled. Downloads will be interrupted.");
        }

        await base.StopAsync(cancellationToken);

        _logger.LogInformation("Cutube.Worker stopped at: {Time}", DateTimeOffset.Now);
    }
}
