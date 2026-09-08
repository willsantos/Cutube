using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using FluentAssertions;
using FluentResults;
using Moq;

namespace Cutube.Domain.Tests.Services;

public class FluentDownloadServiceTests
{
    private readonly Mock<IVideoDownloader> _downloaderMock;
    private readonly Mock<IVideoProcessor> _processorMock;
    private readonly Mock<IValidationService> _validatorMock;
    private readonly FluentDownloadService _service;

    public FluentDownloadServiceTests()
    {
        _downloaderMock = new Mock<IVideoDownloader>();
        _processorMock = new Mock<IVideoProcessor>();
        _validatorMock = new Mock<IValidationService>();
        // Padrão: ffmpeg disponível (a maioria dos testes não exercita o guard)
        _processorMock.Setup(p => p.IsAvailable()).Returns(true);
        _service = new FluentDownloadService(
            _downloaderMock.Object,
            _processorMock.Object,
            _validatorMock.Object
        );
    }

    private void SetupValidUrl()
    {
        _validatorMock
            .Setup(v => v.ValidateUrl(It.IsAny<string>()))
            .Returns(Result.Ok("https://www.youtube.com/watch?v=test"));
    }

    public class FfmpegRequirement : FluentDownloadServiceTests
    {
        [Fact]
        public async Task WhenFfmpegMissing_AndVideoRequested_FailsFastWithoutDownloading()
        {
            SetupValidUrl();
            _processorMock.Setup(p => p.IsAvailable()).Returns(false);

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/video.mp4",
                AudioOnly = false
            };

            var result = await _service.DownloadAsync(request);

            result.IsFailed.Should().BeTrue();
            result.Errors.First().Message.Should().Contain("FFmpeg");
            // Não deve gastar o download para depois falhar
            _downloaderMock.Verify(
                d => d.DownloadAsync(It.IsAny<DownloadRequest>(), It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenFfmpegMissing_AndAudioCutRequested_FailsFast()
        {
            SetupValidUrl();
            _processorMock.Setup(p => p.IsAvailable()).Returns(false);

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/audio.mp3",
                TimeRange = new TimeRange { StartSeconds = 60, EndSeconds = 90 },
                AudioOnly = true
            };

            var result = await _service.DownloadAsync(request);

            result.IsFailed.Should().BeTrue();
            result.Errors.First().Message.Should().Contain("FFmpeg");
            _downloaderMock.Verify(
                d => d.DownloadAsync(It.IsAny<DownloadRequest>(), It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenFfmpegMissing_AndPlainAudioRequested_FailsFast()
        {
            SetupValidUrl();
            _processorMock.Setup(p => p.IsAvailable()).Returns(false);

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/audio.mp3",
                TimeRange = null,
                AudioOnly = true
            };

            var result = await _service.DownloadAsync(request);

            // Extração de áudio também passa pelo ffmpeg
            result.IsFailed.Should().BeTrue();
            result.Errors.First().Message.Should().Contain("FFmpeg");
            _downloaderMock.Verify(
                d => d.DownloadAsync(It.IsAny<DownloadRequest>(), It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }

    public class DirectDownload : FluentDownloadServiceTests
    {
        [Fact]
        public async Task WithoutTimeRangeOrAudioOnly_DownloadsDirect()
        {
            SetupValidUrl();

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/video.mp4",
                TimeRange = null,
                AudioOnly = false
            };

            var downloadResult = new DownloadResult
            {
                Success = true,
                OutputPath = "/tmp/video.mp4",
                FileSizeBytes = 1024000,
                Duration = TimeSpan.FromMinutes(5),
                ErrorMessage = null
            };

            _downloaderMock
                .Setup(d => d.DownloadAsync(request, It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(downloadResult);

            var result = await _service.DownloadAsync(request);

            result.IsSuccess.Should().BeTrue();
            result.Value.FilePath.Should().Be("/tmp/video.mp4");
            result.Value.Size.Should().Be(1024000);

            // Processor should NOT be called for direct download
            _processorMock.Verify(
                p => p.ProcessAsync(It.IsAny<ProcessingRequest>(), It.IsAny<IProgress<ProcessingProgress>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DownloadFailure_ReturnsError()
        {
            SetupValidUrl();

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/video.mp4"
            };

            var downloadResult = new DownloadResult
            {
                Success = false,
                OutputPath = "/tmp/video.mp4",
                FileSizeBytes = 0,
                Duration = TimeSpan.Zero,
                ErrorMessage = "Network error"
            };

            _downloaderMock
                .Setup(d => d.DownloadAsync(request, It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(downloadResult);

            var result = await _service.DownloadAsync(request);

            result.IsFailed.Should().BeTrue();
            result.Errors.First().Message.Should().Contain("Network error");
        }

        [Fact]
        public async Task DownloaderThrowsException_ReturnsError()
        {
            SetupValidUrl();

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/video.mp4"
            };

            _downloaderMock
                .Setup(d => d.DownloadAsync(request, It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Connection refused"));

            var result = await _service.DownloadAsync(request);

            result.IsFailed.Should().BeTrue();
            result.Errors.First().Message.Should().Contain("Connection refused");
        }
    }

    public class Validation : FluentDownloadServiceTests
    {
        [Fact]
        public async Task InvalidUrl_ReturnsValidationError()
        {
            _validatorMock
                .Setup(v => v.ValidateUrl(It.IsAny<string>()))
                .Returns(Result.Fail("Invalid URL format"));

            var request = new DownloadRequest
            {
                Url = "not-a-url",
                OutputPath = "/tmp/video.mp4"
            };

            var result = await _service.DownloadAsync(request);

            result.IsFailed.Should().BeTrue();
            result.Errors.First().Message.Should().Contain("Invalid URL");

            // Neither downloader nor processor should be called
            _downloaderMock.Verify(
                d => d.DownloadAsync(It.IsAny<DownloadRequest>(), It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }

    public class DownloadWithProcessing : FluentDownloadServiceTests
    {
        /// <summary>
        /// THE KEY TEST: Verifies that the processor receives the ACTUAL downloaded
        /// file path (from DownloadResult.OutputPath), not the originally requested
        /// temp path. This is the exact bug that caused FFmpeg exit code 183.
        ///
        /// Scenario: yt-dlp is told to save to "temp.mp4" but actually saves to
        /// "temp.mp4.webm" or any different path. The processor must use the real path.
        /// </summary>
        [Fact]
        public async Task ProcessorReceivesActualDownloadedPath_NotRequestedTempPath()
        {
            SetupValidUrl();

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/final-clip.mp4",
                TimeRange = new TimeRange { StartSeconds = 30, EndSeconds = 90 }
            };

            // Simulate yt-dlp returning a DIFFERENT path than requested
            // (this is the real-world behavior that caused the bug)
            _downloaderMock
                .Setup(d => d.DownloadAsync(
                    It.IsAny<DownloadRequest>(),
                    It.IsAny<IProgress<DownloadProgress>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((DownloadRequest req, IProgress<DownloadProgress>? _, CancellationToken _) =>
                    new DownloadResult
                    {
                        Success = true,
                        OutputPath = req.OutputPath + ".webm", // yt-dlp changed the extension!
                        FileSizeBytes = 50_000_000,
                        Duration = TimeSpan.FromMinutes(10),
                        ErrorMessage = null
                    });

            ProcessingRequest? capturedProcessingRequest = null;

            _processorMock
                .Setup(p => p.ProcessAsync(
                    It.IsAny<ProcessingRequest>(),
                    It.IsAny<IProgress<ProcessingProgress>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<ProcessingRequest, IProgress<ProcessingProgress>?, CancellationToken>(
                    (req, _, _) => capturedProcessingRequest = req)
                .ReturnsAsync(new ProcessingResult
                {
                    Success = true,
                    OutputPath = "/tmp/final-clip.mp4",
                    FileSizeBytes = 5_000_000,
                    ErrorMessage = null
                });

            await _service.DownloadAsync(request);

            // THE CRITICAL ASSERTION: InputPath must be the actual downloaded path,
            // not the temp path that was originally requested
            capturedProcessingRequest.Should().NotBeNull();
            capturedProcessingRequest!.InputPath.Should().EndWith(".webm",
                "processor must receive the actual path returned by the downloader, not the requested temp path");
            capturedProcessingRequest.InputPath.Should().NotBe(capturedProcessingRequest.OutputPath,
                "input and output must be different files");
        }

        [Fact]
        public async Task WithTimeRange_DownloadsFullThenProcesses()
        {
            SetupValidUrl();

            var timeRange = new TimeRange { StartSeconds = 30, EndSeconds = 90 };
            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/clip.mp4",
                TimeRange = timeRange
            };

            DownloadRequest? capturedDownloadRequest = null;

            _downloaderMock
                .Setup(d => d.DownloadAsync(
                    It.IsAny<DownloadRequest>(),
                    It.IsAny<IProgress<DownloadProgress>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<DownloadRequest, IProgress<DownloadProgress>?, CancellationToken>(
                    (req, _, _) => capturedDownloadRequest = req)
                .ReturnsAsync((DownloadRequest req, IProgress<DownloadProgress>? _, CancellationToken _) =>
                    new DownloadResult
                    {
                        Success = true,
                        OutputPath = req.OutputPath,
                        FileSizeBytes = 50_000_000,
                        Duration = TimeSpan.FromMinutes(10),
                        ErrorMessage = null
                    });

            _processorMock
                .Setup(p => p.ProcessAsync(
                    It.IsAny<ProcessingRequest>(),
                    It.IsAny<IProgress<ProcessingProgress>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProcessingResult
                {
                    Success = true,
                    OutputPath = "/tmp/clip.mp4",
                    FileSizeBytes = 5_000_000,
                    ErrorMessage = null
                });

            var result = await _service.DownloadAsync(request);

            result.IsSuccess.Should().BeTrue();

            // Download request should have no TimeRange (downloads full video)
            capturedDownloadRequest.Should().NotBeNull();
            capturedDownloadRequest!.TimeRange.Should().BeNull(
                "download phase should download the full video without time range");
            capturedDownloadRequest.AudioOnly.Should().BeFalse(
                "download phase should get full video regardless of audio-only setting");

            // Output should be a temp path, not the final output
            capturedDownloadRequest.OutputPath.Should().NotBe("/tmp/clip.mp4",
                "download should go to a temp file, not the final output");
        }

        [Fact]
        public async Task WithTimeRange_ProcessingRequestHasCorrectParameters()
        {
            SetupValidUrl();

            var timeRange = new TimeRange { StartSeconds = 30, EndSeconds = 90 };
            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/clip.mp4",
                TimeRange = timeRange,
                AudioOnly = true
            };

            _downloaderMock
                .Setup(d => d.DownloadAsync(
                    It.IsAny<DownloadRequest>(),
                    It.IsAny<IProgress<DownloadProgress>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((DownloadRequest req, IProgress<DownloadProgress>? _, CancellationToken _) =>
                    new DownloadResult
                    {
                        Success = true,
                        OutputPath = req.OutputPath,
                        FileSizeBytes = 50_000_000,
                        Duration = TimeSpan.FromMinutes(10),
                        ErrorMessage = null
                    });

            ProcessingRequest? capturedRequest = null;

            _processorMock
                .Setup(p => p.ProcessAsync(
                    It.IsAny<ProcessingRequest>(),
                    It.IsAny<IProgress<ProcessingProgress>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<ProcessingRequest, IProgress<ProcessingProgress>?, CancellationToken>(
                    (req, _, _) => capturedRequest = req)
                .ReturnsAsync(new ProcessingResult
                {
                    Success = true,
                    OutputPath = "/tmp/clip.mp4",
                    FileSizeBytes = 256_000,
                    ErrorMessage = null
                });

            await _service.DownloadAsync(request);

            capturedRequest.Should().NotBeNull();
            capturedRequest!.TimeRange.Should().Be(timeRange);
            capturedRequest.AudioOnly.Should().BeTrue();
            capturedRequest.OutputPath.Should().Be("/tmp/clip.mp4");
        }

        [Fact]
        public async Task WithTimeRange_CalculatesDurationFromTimeRange()
        {
            SetupValidUrl();

            var timeRange = new TimeRange { StartSeconds = 30, EndSeconds = 90 };
            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/clip.mp4",
                TimeRange = timeRange
            };

            _downloaderMock
                .Setup(d => d.DownloadAsync(
                    It.IsAny<DownloadRequest>(),
                    It.IsAny<IProgress<DownloadProgress>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((DownloadRequest req, IProgress<DownloadProgress>? _, CancellationToken _) =>
                    new DownloadResult
                    {
                        Success = true,
                        OutputPath = req.OutputPath,
                        FileSizeBytes = 50_000_000,
                        Duration = TimeSpan.FromMinutes(10),
                        ErrorMessage = null
                    });

            _processorMock
                .Setup(p => p.ProcessAsync(
                    It.IsAny<ProcessingRequest>(),
                    It.IsAny<IProgress<ProcessingProgress>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProcessingResult
                {
                    Success = true,
                    OutputPath = "/tmp/clip.mp4",
                    FileSizeBytes = 5_000_000,
                    ErrorMessage = null
                });

            var result = await _service.DownloadAsync(request);

            result.IsSuccess.Should().BeTrue();
            result.Value.Duration.Should().Be(TimeSpan.FromSeconds(60),
                "duration should be calculated from TimeRange (90 - 30 = 60s)");
        }

        [Fact]
        public async Task DownloadFailure_ReturnsErrorWithoutProcessing()
        {
            SetupValidUrl();

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/clip.mp4",
                TimeRange = new TimeRange { StartSeconds = 0, EndSeconds = 60 }
            };

            _downloaderMock
                .Setup(d => d.DownloadAsync(
                    It.IsAny<DownloadRequest>(),
                    It.IsAny<IProgress<DownloadProgress>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DownloadResult
                {
                    Success = false,
                    OutputPath = "",
                    FileSizeBytes = 0,
                    Duration = TimeSpan.Zero,
                    ErrorMessage = "Video unavailable"
                });

            var result = await _service.DownloadAsync(request);

            result.IsFailed.Should().BeTrue();
            result.Errors.First().Message.Should().Contain("Video unavailable");

            // Processor should NOT be called if download failed
            _processorMock.Verify(
                p => p.ProcessAsync(It.IsAny<ProcessingRequest>(), It.IsAny<IProgress<ProcessingProgress>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ProcessingFailure_ReturnsError()
        {
            SetupValidUrl();

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/clip.mp4",
                TimeRange = new TimeRange { StartSeconds = 0, EndSeconds = 60 }
            };

            _downloaderMock
                .Setup(d => d.DownloadAsync(
                    It.IsAny<DownloadRequest>(),
                    It.IsAny<IProgress<DownloadProgress>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((DownloadRequest req, IProgress<DownloadProgress>? _, CancellationToken _) =>
                    new DownloadResult
                    {
                        Success = true,
                        OutputPath = req.OutputPath,
                        FileSizeBytes = 50_000_000,
                        Duration = TimeSpan.FromMinutes(10),
                        ErrorMessage = null
                    });

            _processorMock
                .Setup(p => p.ProcessAsync(
                    It.IsAny<ProcessingRequest>(),
                    It.IsAny<IProgress<ProcessingProgress>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProcessingResult
                {
                    Success = false,
                    OutputPath = "/tmp/clip.mp4",
                    FileSizeBytes = 0,
                    ErrorMessage = "FFmpeg failed with exit code 1"
                });

            var result = await _service.DownloadAsync(request);

            result.IsFailed.Should().BeTrue();
            result.Errors.First().Message.Should().Contain("FFmpeg failed");
        }

        [Fact]
        public async Task Cancellation_ThrowsOperationCancelledException()
        {
            SetupValidUrl();

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/clip.mp4",
                TimeRange = new TimeRange { StartSeconds = 0, EndSeconds = 60 }
            };

            _downloaderMock
                .Setup(d => d.DownloadAsync(
                    It.IsAny<DownloadRequest>(),
                    It.IsAny<IProgress<DownloadProgress>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var act = async () => await _service.DownloadAsync(request);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task AudioOnlyWithoutTimeRange_TriggersProcessingPath()
        {
            SetupValidUrl();

            // audio-only without time range should still go through processing path
            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/audio.mp3",
                TimeRange = null,
                AudioOnly = true
            };

            _downloaderMock
                .Setup(d => d.DownloadAsync(
                    It.IsAny<DownloadRequest>(),
                    It.IsAny<IProgress<DownloadProgress>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((DownloadRequest req, IProgress<DownloadProgress>? _, CancellationToken _) =>
                    new DownloadResult
                    {
                        Success = true,
                        OutputPath = req.OutputPath,
                        FileSizeBytes = 10_000_000,
                        Duration = TimeSpan.FromMinutes(5),
                        ErrorMessage = null
                    });

            _processorMock
                .Setup(p => p.ProcessAsync(
                    It.IsAny<ProcessingRequest>(),
                    It.IsAny<IProgress<ProcessingProgress>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProcessingResult
                {
                    Success = true,
                    OutputPath = "/tmp/audio.mp3",
                    FileSizeBytes = 2_000_000,
                    ErrorMessage = null
                });

            var result = await _service.DownloadAsync(request);

            result.IsSuccess.Should().BeTrue();

            // Processor SHOULD be called for audio-only
            _processorMock.Verify(
                p => p.ProcessAsync(It.IsAny<ProcessingRequest>(), It.IsAny<IProgress<ProcessingProgress>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task TempFileUsesProperExtension()
        {
            SetupValidUrl();

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/clip.mp4",
                TimeRange = new TimeRange { StartSeconds = 0, EndSeconds = 60 }
            };

            DownloadRequest? capturedDownloadRequest = null;

            _downloaderMock
                .Setup(d => d.DownloadAsync(
                    It.IsAny<DownloadRequest>(),
                    It.IsAny<IProgress<DownloadProgress>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<DownloadRequest, IProgress<DownloadProgress>?, CancellationToken>(
                    (req, _, _) => capturedDownloadRequest = req)
                .ReturnsAsync((DownloadRequest req, IProgress<DownloadProgress>? _, CancellationToken _) =>
                    new DownloadResult
                    {
                        Success = true,
                        OutputPath = req.OutputPath,
                        FileSizeBytes = 50_000_000,
                        Duration = TimeSpan.FromMinutes(10),
                        ErrorMessage = null
                    });

            _processorMock
                .Setup(p => p.ProcessAsync(
                    It.IsAny<ProcessingRequest>(),
                    It.IsAny<IProgress<ProcessingProgress>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProcessingResult
                {
                    Success = true,
                    OutputPath = "/tmp/clip.mp4",
                    FileSizeBytes = 5_000_000,
                    ErrorMessage = null
                });

            await _service.DownloadAsync(request);

            // Temp file should use .mp4 extension, NOT .tmp
            // (yt-dlp renames .tmp files, causing FFmpeg to get wrong input)
            capturedDownloadRequest.Should().NotBeNull();
            capturedDownloadRequest!.OutputPath.Should().EndWith(".mp4",
                "temp file must use .mp4 extension to prevent yt-dlp from renaming it");
            capturedDownloadRequest.OutputPath.Should().NotEndWith(".tmp",
                ".tmp extension causes yt-dlp to append .mp4, leaving the original empty");
        }
    }
}
