using Cutube.Api.HealthChecks;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cutube.Api.Tests.Unit.HealthChecks;

public class DiskSpaceHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WithSufficientDiskSpace_ReturnsHealthy()
    {
        // Arrange
        var options = Options.Create(new DiskSpaceHealthCheckOptions
        {
            DiskSpaceMinRequiredGB = 1 // Most systems have at least 1GB free
        });
        var healthCheck = new DiskSpaceHealthCheck(options);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Degraded);
        result.Description.Should().Contain("Disk space");
    }

    [Fact]
    public async Task CheckHealthAsync_WithVeryHighRequirement_ReturnsDegradedOrUnhealthy()
    {
        // Arrange
        var options = Options.Create(new DiskSpaceHealthCheckOptions
        {
            DiskSpaceMinRequiredGB = 1000000 // Unrealistically high requirement
        });
        var healthCheck = new DiskSpaceHealthCheck(options);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

        // Assert - Should be Degraded or Unhealthy since no system has 1PB free
        result.Should().NotBeNull();
        result.Status.Should().BeOneOf(HealthStatus.Degraded, HealthStatus.Unhealthy);
        result.Description.Should().Match(x => x.Contains("Low disk space") || x.Contains("Disk space"));
    }

    [Fact]
    public async Task CheckHealthAsync_WithZeroGBRequired_ReturnsHealthy()
    {
        // Arrange
        var options = Options.Create(new DiskSpaceHealthCheckOptions
        {
            DiskSpaceMinRequiredGB = 0
        });
        var healthCheck = new DiskSpaceHealthCheck(options);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("Disk space OK");
    }

    [Fact]
    public async Task CheckHealthAsync_Description_ContainsAvailableSpaceInformation()
    {
        // Arrange
        var options = Options.Create(new DiskSpaceHealthCheckOptions
        {
            DiskSpaceMinRequiredGB = 1
        });
        var healthCheck = new DiskSpaceHealthCheck(options);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().NotBeNullOrEmpty();
        result.Description.Should().Contain("GB");
    }

    [Fact]
    public async Task CheckHealthAsync_WithNegativeGBRequired_HandlesGracefully()
    {
        // Arrange - Edge case: negative value
        var options = Options.Create(new DiskSpaceHealthCheckOptions
        {
            DiskSpaceMinRequiredGB = -1
        });
        var healthCheck = new DiskSpaceHealthCheck(options);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

        // Assert - Should not throw, should return a result
        result.Should().NotBeNull();
    }
}
