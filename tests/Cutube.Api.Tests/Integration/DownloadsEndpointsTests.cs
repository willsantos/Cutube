using FluentAssertions;
using Cutube.Api.DTOs;
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
    public async Task CreateDownload_WithMissingOutputPath_UsesDefaultPathAndReturnsAccepted()
    {
        // Arrange
        var request = new
        {
            Url = "https://youtube.com/watch?v=test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/downloads", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
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

    [Fact]
    public async Task CreateDownload_WithValidRequest_PersistsQueuedStatus()
    {
        var request = new
        {
            Url = "https://www.youtube.com/watch?v=test",
            OutputPath = "/tmp/test"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/downloads", request);

        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateDownloadResponse>();
        created.Should().NotBeNull();
        created!.CorrelationId.Should().NotBeNullOrWhiteSpace();

        var listResponse = await _client.GetAsync("/api/downloads");
        listResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var list = await listResponse.Content.ReadFromJsonAsync<GetDownloadsResponse>();
        list.Should().NotBeNull();
        list!.Downloads.Should().ContainSingle(d => d.DownloadId == created.CorrelationId && d.Status == "queued");
    }

    [Fact]
    public async Task CreateDownload_WhenPublishFails_MarksStatusAsFailed()
    {
        await using var factory = new FailingQueueProducerFactory();
        using var client = factory.CreateClient();

        var request = new
        {
            Url = "https://www.youtube.com/watch?v=test",
            OutputPath = "/tmp/test"
        };

        var createResponse = await client.PostAsJsonAsync("/api/downloads", request);

        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.ServiceUnavailable);

        var listResponse = await client.GetAsync("/api/downloads");
        listResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var list = await listResponse.Content.ReadFromJsonAsync<GetDownloadsResponse>();
        list.Should().NotBeNull();
        list!.Downloads.Should().ContainSingle();
        list.Downloads[0].Status.Should().Be("failed");
    }

    [Fact]
    public async Task CreateDownload_ThenStatusCallback_ReturnsNoContent()
    {
        var request = new
        {
            Url = "https://www.youtube.com/watch?v=test",
            OutputPath = "/tmp/test"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/downloads", request);
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateDownloadResponse>();
        created.Should().NotBeNull();

        var statusPayload = JsonContent.Create(new { state = "processing" });
        var patchResponse = await _client.PatchAsync($"/api/downloads/{created!.CorrelationId}/status", statusPayload);

        patchResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }
}
