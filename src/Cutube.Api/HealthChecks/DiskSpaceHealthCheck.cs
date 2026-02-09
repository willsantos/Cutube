using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cutube.Api.HealthChecks;

/// <summary>
/// Health check for available disk space
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly long _minRequiredSpaceBytes = 1024 * 1024 * 1024; // 1GB

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var drive = new DriveInfo(Directory.GetCurrentDirectory());
            var availableSpace = drive.AvailableFreeSpace;

            if (availableSpace >= _minRequiredSpaceBytes)
            {
                var availableGB = availableSpace / (1024.0 * 1024.0 * 1024.0);
                return Task.FromResult(HealthCheckResult.Healthy(
                    $"Disk space OK: {availableGB:F2} GB available"));
            }

            var availableMB = availableSpace / (1024.0 * 1024.0);
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Low disk space: {availableMB:F2} MB available (min 1 GB required)"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Disk space check failed", ex));
        }
    }
}
