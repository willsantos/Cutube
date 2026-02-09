using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Cutube.Domain.Services;
using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using Cutube.Infrastructure;

namespace Cutube.Tests.Integration;

/// <summary>
/// Integration tests for Domain services
/// </summary>
public class DomainIntegrationTests
{
    [Fact]
    public async Task FluentValidationService_ValidYouTubeUrl_ReturnsSuccess()
    {
        // Arrange
        var validator = new FluentValidationService();
        var validUrl = "https://www.youtube.com/watch?v=test123";

        // Act
        var result = validator.ValidateUrl(validUrl);

        // Assert
        result.IsFailed.Should().BeFalse("valid YouTube URL should pass validation");
        result.Value.Should().Be(validUrl);
    }

    [Fact]
    public void FluentValidationService_InvalidUrl_ReturnsFailure()
    {
        // Arrange
        var validator = new FluentValidationService();
        var invalidUrl = "not-a-valid-url";

        // Act
        var result = validator.ValidateUrl(invalidUrl);

        // Assert
        result.IsFailed.Should().BeTrue("invalid URL should fail validation");
        result.Errors.First().Message.Should().Contain("Invalid");
    }

    [Fact]
    public void FluentValidationService_NonYouTubeUrl_ReturnsFailure()
    {
        // Arrange
        var validator = new FluentValidationService();
        var nonYouTubeUrl = "https://example.com/video";

        // Act
        var result = validator.ValidateUrl(nonYouTubeUrl);

        // Assert
        result.IsFailed.Should().BeTrue("non-YouTube URL should fail validation");
        result.Errors.First().Message.Should().Contain("not supported");
    }

    [Fact]
    public void FluentValidationService_InvalidTimeRange_ReturnsFailure()
    {
        // Arrange
        var validator = new FluentValidationService();
        var timeRange = new TimeRange
        {
            StartSeconds = 100,
            EndSeconds = 50 // End < Start
        };

        var request = new DownloadRequest
        {
            Url = "https://www.youtube.com/watch?v=test",
            OutputPath = "/tmp/test.mp4",
            TimeRange = timeRange
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsFailed.Should().BeTrue("invalid time range should fail validation");
        result.Errors.First().Message.Should().Contain("Start must be less than End");
    }

    [Fact]
    public async Task FluentDownloadService_WithInvalidUrl_ReturnsFailure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IVideoDownloader, YtDlpDownloader>();
        services.AddSingleton<IVideoProcessor, FfmpegProcessor>();
        services.AddSingleton<IValidationService, FluentValidationService>();
        services.AddSingleton<IDownloadService, FluentDownloadService>();

        var serviceProvider = services.BuildServiceProvider();
        var downloadService = serviceProvider.GetRequiredService<IDownloadService>();

        var request = new DownloadRequest
        {
            Url = "invalid-url",
            OutputPath = "/tmp/test.mp4"
        };

        // Act
        var result = await downloadService.DownloadAsync(request);

        // Assert
        result.IsFailed.Should().BeTrue("invalid URL should cause download to fail");
        result.Errors.First().Message.Should().Contain("Invalid");
    }

    [Fact]
    public void TimeRange_FromValidStrings_CreatesCorrectRange()
    {
        // Arrange
        var start = "1:30";  // 90 seconds
        var end = "2:30";    // 150 seconds

        // Act
        var range = TimeRange.FromStrings(start, end);

        // Assert
        range.StartSeconds.Should().Be(90);
        range.EndSeconds.Should().Be(150);
        range.DurationSeconds.Should().Be(60);
    }

    [Fact]
    public void TimeRange_FromSecondsString_CreatesCorrectRange()
    {
        // Arrange
        var start = "90s";
        var end = "150s";

        // Act
        var range = TimeRange.FromStrings(start, end);

        // Assert
        range.StartSeconds.Should().Be(90);
        range.EndSeconds.Should().Be(150);
        range.DurationSeconds.Should().Be(60);
    }

    [Fact]
    public void TimeRange_WithInvalidRange_ThrowsException()
    {
        // Arrange
        var start = "200s";
        var end = "100s";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimeRange.FromStrings(start, end));
    }

    [Fact]
    public void DomainDownloadResult_CanBeCreated()
    {
        // Arrange & Act
        var result = new DomainDownloadResult
        {
            FilePath = "/tmp/test.mp4",
            Size = 1024000,
            Duration = TimeSpan.FromSeconds(100)
        };

        // Assert
        result.FilePath.Should().Be("/tmp/test.mp4");
        result.Size.Should().Be(1024000);
        result.Duration.Should().Be(TimeSpan.FromSeconds(100));
    }

    [Fact]
    public void DownloadResultExtensions_ConvertsToDomainResult()
    {
        // Arrange
        var infraResult = new DownloadResult
        {
            Success = true,
            OutputPath = "/tmp/test.mp4",
            FileSizeBytes = 1024000,
            Duration = TimeSpan.FromSeconds(100),
            ErrorMessage = null
        };

        // Act
        var domainResult = infraResult.ToDomainResult();

        // Assert
        domainResult.FilePath.Should().Be(infraResult.OutputPath);
        domainResult.Size.Should().Be(infraResult.FileSizeBytes);
        domainResult.Duration.Should().Be(infraResult.Duration);
    }

    [Fact]
    public void DownloadResultExtensions_ConvertsToInfraResult()
    {
        // Arrange
        var domainResult = new DomainDownloadResult
        {
            FilePath = "/tmp/test.mp4",
            Size = 1024000,
            Duration = TimeSpan.FromSeconds(100)
        };

        // Act
        var infraResult = domainResult.ToInfraResult();

        // Assert
        infraResult.OutputPath.Should().Be(domainResult.FilePath);
        infraResult.FileSizeBytes.Should().Be(domainResult.Size);
        infraResult.Duration.Should().Be(domainResult.Duration);
        infraResult.Success.Should().BeTrue();
        infraResult.ErrorMessage.Should().BeNull();
    }
}
