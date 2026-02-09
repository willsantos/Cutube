using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Cutube.Api.HealthChecks;

/// <summary>
/// Configuration for disk space health check
/// </summary>
public class DiskSpaceHealthCheckOptions
{
    public const string SectionName = "HealthChecks";
    public int DiskSpaceMinRequiredGB { get; set; } = 1;
}

/// <summary>
/// Health check for available disk space
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly DiskSpaceHealthCheckOptions _options;

    public DiskSpaceHealthCheck(IOptions<DiskSpaceHealthCheckOptions> options)
    {
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var drive = new DriveInfo(Directory.GetCurrentDirectory());
            var availableSpace = drive.AvailableFreeSpace;
            var minRequiredSpaceBytes = (long)_options.DiskSpaceMinRequiredGB * 1024 * 1024 * 1024;

            if (availableSpace >= minRequiredSpaceBytes)
            {
                var availableGB = availableSpace / (1024.0 * 1024.0 * 1024.0);
                return Task.FromResult(HealthCheckResult.Healthy(
                    $"Disk space OK: {availableGB:F2} GB available"));
            }

            var availableMB = availableSpace / (1024.0 * 1024.0);
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Low disk space: {availableMB:F2} MB available (min {_options.DiskSpaceMinRequiredGB} GB required)"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Disk space check failed", ex));
        }
    }
}
