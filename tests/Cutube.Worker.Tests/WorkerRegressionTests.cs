using System.Net;
using Cutube.Worker.Configuration;
using Cutube.Contracts.Messages;
using Cutube.Worker.Consumers;
using FluentAssertions;
using MassTransit;
using Polly.CircuitBreaker;

namespace Cutube.Worker.Tests;

public class WorkerRegressionTests
{
    [Theory]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    public void ShouldRetry_ReturnsExpectedResult(HttpStatusCode statusCode, bool expected)
    {
        using var response = new HttpResponseMessage(statusCode);

        var result = HttpRetryPolicyFactory.ShouldRetry(response);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task RetryPolicy_ShouldNotRetryClientErrors()
    {
        var options = new NotificationResilienceOptions { MaxRetries = 3 };
        var attempts = 0;
        var policy = HttpRetryPolicyFactory.CreateRetryPolicy(options);

        using var response = await policy.ExecuteAsync(() =>
        {
            attempts++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        attempts.Should().Be(1);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryServerErrors()
    {
        var options = new NotificationResilienceOptions { MaxRetries = 3, InitialRetryDelaySeconds = 1 };
        var attempts = 0;
        var policy = HttpRetryPolicyFactory.CreateRetryPolicy(options);

        using var response = await policy.ExecuteAsync(() =>
        {
            attempts++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        });

        attempts.Should().Be(4);
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task CircuitBreaker_ShouldOpenAfterConfiguredFailures()
    {
        var options = new NotificationResilienceOptions
        {
            ConsecutiveFailuresBeforeBreak = 2,
            CircuitBreakSeconds = 60
        };
        var policy = HttpRetryPolicyFactory.CreateCircuitBreakerPolicy(options);

        await policy.ExecuteAsync(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)));
        await policy.ExecuteAsync(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)));

        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(
            () => policy.ExecuteAsync(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));
    }

    [Fact]
    public void DlqConsumer_ShouldConsumeFaultMessagesOnly()
    {
        var implementedInterfaces = typeof(DlqConsumer).GetInterfaces();

        implementedInterfaces.Should().Contain(typeof(IConsumer<Fault<DownloadMessage>>));
        implementedInterfaces.Should().NotContain(typeof(IConsumer<DownloadMessage>));
    }

    [Fact]
    public void Dockerfiles_ShouldIncludeDenoInstallation()
    {
        var root = GetRepositoryRoot();

        var apiDockerfile = File.ReadAllText(Path.Combine(root, "src", "Cutube.Api", "Dockerfile"));
        var workerDockerfile = File.ReadAllText(Path.Combine(root, "src", "Cutube.Worker", "Dockerfile"));

        apiDockerfile.Should().Contain("deno");
        workerDockerfile.Should().Contain("deno");
    }

    private static string GetRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current != null && !File.Exists(Path.Combine(current.FullName, "cutube.sln")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new InvalidOperationException("Could not locate repository root from test runtime path.");
    }
}
