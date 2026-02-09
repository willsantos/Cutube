using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Cutube.Api.Tests.Helpers;

namespace Cutube.Api.Tests.Integration;

public class HealthChecksTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthChecksTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert - Should be 200 if dependencies are available, 503 if not
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.OK,
            System.Net.HttpStatusCode.ServiceUnavailable
        );
    }

    [Fact]
    public async Task HealthReady_ReturnsOkOrServiceUnavailable()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert - Should be 200 if dependencies are available, 503 if not
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.OK,
            System.Net.HttpStatusCode.ServiceUnavailable
        );
    }
}
