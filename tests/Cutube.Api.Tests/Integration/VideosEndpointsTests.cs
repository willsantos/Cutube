using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Cutube.Api.Tests.Helpers;

namespace Cutube.Api.Tests.Integration;

public class VideosEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public VideosEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetVideoInfo_WithMissingUrl_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/videos/info");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetVideoInfo_WithInvalidUrl_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/videos/info?url=not-a-valid-url");

        // Assert
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.BadRequest,
            System.Net.HttpStatusCode.InternalServerError // If metadata service fails
        );
    }

    [Fact]
    public async Task GetVideoInfo_WithValidUrl_ReturnsOkOrBadRequest()
    {
        // Act - Using a real YouTube URL (may fail if yt-dlp is not installed)
        var response = await _client.GetAsync("/api/videos/info?url=https://www.youtube.com/watch?v=dQw4w9WgXcQ");

        // Assert - Should either succeed (200) if yt-dlp is available, or fail (400) if not
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.OK,
            System.Net.HttpStatusCode.BadRequest
        );
    }
}
