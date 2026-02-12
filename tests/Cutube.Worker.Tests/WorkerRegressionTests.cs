using System.Net;
using Cutube.Contracts.Messages;
using Cutube.Worker.Configuration;
using Cutube.Worker.Consumers;
using FluentAssertions;
using MassTransit;

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
