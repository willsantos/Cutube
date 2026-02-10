using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cutube.Api.HealthChecks;

/// <summary>
/// Health check for ffmpeg installation
/// </summary>
public class FfmpegHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if ffmpeg is installed
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(processStartInfo);
            await process!.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                return HealthCheckResult.Healthy("ffmpeg is available");
            }

            return HealthCheckResult.Unhealthy("ffmpeg is not installed or not working");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("ffmpeg check failed", ex);
        }
    }
}
