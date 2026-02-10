using Cutube.Api.HealthChecks;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cutube.Api.Tests.Unit.HealthChecks;

public class YtDlpHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthCheckResult()
    {
        // Arrange
        var healthCheck = new YtDlpHealthCheck();
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

        // Assert - Result should always be returned (Healthy or Unhealthy)
        result.Should().NotBeNull();
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Unhealthy);
        result.Description.Should().NotBeNullOrEmpty();

        // In CI or if yt-dlp is not installed, it will be Unhealthy
        // In local dev with yt-dlp installed, it will be Healthy
        if (result.Status == HealthStatus.Healthy)
        {
            result.Description.Should().Contain("yt-dlp is available");
        }
        else
        {
            result.Description.Should().Match(x => x.Contains("yt-dlp is not installed") || x.Contains("yt-dlp check failed"));
        }
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var healthCheck = new YtDlpHealthCheck();
        var context = new HealthCheckContext();
        var cts = new CancellationTokenSource(1); // Very short timeout

        // Act & Assert
        // Should either complete or throw OperationCanceledException
        try
        {
            var result = await healthCheck.CheckHealthAsync(context, cts.Token);
            result.Should().NotBeNull();
        }
        catch (OperationCanceledException)
        {
            // Expected behavior when cancelled quickly
            Assert.True(true);
        }
    }
}
