using Cutube.Domain.Models;
using FluentAssertions;

namespace Cutube.Domain.Tests.Models;

public class DownloadRequestTests
{
    [Fact]
    public void Create_ValidRequest_SetsProperties()
    {
        var url = "https://www.youtube.com/watch?v=test";
        var outputPath = "/tmp/video.mp4";

        var request = new DownloadRequest
        {
            Url = url,
            OutputPath = outputPath,
            TimeRange = null,
            AudioOnly = false
        };

        request.Url.Should().Be(url);
        request.OutputPath.Should().Be(outputPath);
        request.TimeRange.Should().BeNull();
        request.AudioOnly.Should().BeFalse();
    }

    [Fact]
    public void With_TimeRange_SetsTimeRange()
    {
        var timeRange = new TimeRange
        {
            StartSeconds = 10,
            EndSeconds = 60
        };

        var request = new DownloadRequest
        {
            Url = "https://www.youtube.com/watch?v=test",
            OutputPath = "/tmp/video.mp4",
            TimeRange = timeRange,
            AudioOnly = false
        };

        request.TimeRange.Should().Be(timeRange);
    }

    [Fact]
    public void With_AudioOnlyTrue_SetsAudioOnly()
    {
        var request = new DownloadRequest
        {
            Url = "https://www.youtube.com/watch?v=test",
            OutputPath = "/tmp/audio.mp3",
            TimeRange = null,
            AudioOnly = true
        };

        request.AudioOnly.Should().BeTrue();
    }

    [Fact]
    public void RecordType_IsImmutable()
    {
        var request = new DownloadRequest
        {
            Url = "https://www.youtube.com/watch?v=test",
            OutputPath = "/tmp/video.mp4",
            TimeRange = null,
            AudioOnly = false
        };

        var modifiedRequest = request with { AudioOnly = true };

        request.AudioOnly.Should().BeFalse();
        modifiedRequest.AudioOnly.Should().BeTrue();
    }
}
