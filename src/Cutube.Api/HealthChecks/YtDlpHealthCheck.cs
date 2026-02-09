using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cutube.Api.HealthChecks;

/// <summary>
/// Health check for yt-dlp installation
/// </summary>
public class YtDlpHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if yt-dlp is installed
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(processStartInfo);
            await process!.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                var version = await process.StandardOutput.ReadToEndAsync();
                return HealthCheckResult.Healthy($"yt-dlp is available: {version.Trim()}");
            }

            return HealthCheckResult.Unhealthy("yt-dlp is not installed or not working");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("yt-dlp check failed", ex);
        }
    }
}
