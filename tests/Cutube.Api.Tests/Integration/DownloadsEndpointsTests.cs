using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Cutube.Api.Tests.Helpers;

namespace Cutube.Api.Tests.Integration;

public class DownloadsEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DownloadsEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDownloads_WhenCalled_ReturnsOkWithDownloadsList()
    {
        // Act
        var response = await _client.GetAsync("/api/downloads");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<object>();
        content.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateDownload_WithInvalidUrl_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Url = "not-a-valid-url",
            OutputPath = "/tmp/test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/downloads", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDownload_WithMissingUrl_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            OutputPath = "/tmp/test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/downloads", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDownload_WithMissingOutputPath_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Url = "https://youtube.com/watch?v=test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/downloads", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDownloadById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/downloads/non-existent-id");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteDownload_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/downloads/non-existent-id");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDownloads_WithStatusFilter_ReturnsFilteredResults()
    {
        // Act
        var response = await _client.GetAsync("/api/downloads?status=queued");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDownloads_WithPagination_ReturnsPagedResults()
    {
        // Act
        var response = await _client.GetAsync("/api/downloads?limit=10&offset=0");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateDownload_WithValidRequest_ReturnsAccepted()
    {
        // Arrange
        var request = new
        {
            Url = "https://www.youtube.com/watch?v=test",
            OutputPath = "/tmp/test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/downloads", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);

        var content = await response.Content.ReadFromJsonAsync<object>();
        content.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateDownload_WithTimeRange_ReturnsAccepted()
    {
        // Arrange
        var request = new
        {
            Url = "https://www.youtube.com/watch?v=test",
            OutputPath = "/tmp/test",
            StartTime = "00:01:00",
            EndTime = "00:02:00"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/downloads", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task CreateDownload_WithInvalidTimeRange_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Url = "https://www.youtube.com/watch?v=test",
            OutputPath = "/tmp/test",
            StartTime = "00:02:00",  // Greater than EndTime
            EndTime = "00:01:00"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/downloads", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDownload_WithAudioOnly_ReturnsAccepted()
    {
        // Arrange
        var request = new
        {
            Url = "https://www.youtube.com/watch?v=test",
            OutputPath = "/tmp/test",
            AudioOnly = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/downloads", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task CreateDownload_WithAllOptions_ReturnsAccepted()
    {
        // Arrange
        var request = new
        {
            Url = "https://www.youtube.com/watch?v=test",
            OutputPath = "/tmp/test",
            StartTime = "00:00:30",
            EndTime = "00:01:30",
            AudioOnly = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/downloads", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task GetDownloads_WithInvalidStatus_ReturnsOk()
    {
        // Act - Invalid status should just return empty list
        var response = await _client.GetAsync("/api/downloads?status=invalid-status");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
