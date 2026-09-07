using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using FluentAssertions;
using Moq;

namespace Cutube.Domain.Tests.Services;

public class DownloadServiceTests
{
    private readonly Mock<IVideoDownloader> _downloaderMock;
    private readonly Mock<IVideoProcessor> _processorMock;
    private readonly Mock<IDownloadValidator> _validatorMock;
    private readonly DownloadService _service;

    public DownloadServiceTests()
    {
        _downloaderMock = new Mock<IVideoDownloader>();
        _processorMock = new Mock<IVideoProcessor>();
        _validatorMock = new Mock<IDownloadValidator>();
        _service = new DownloadService(
            _downloaderMock.Object,
            _processorMock.Object,
            _validatorMock.Object
        );
    }

    public class DownloadAsyncWithoutTimeRange : DownloadServiceTests
    {
        [Fact]
        public async Task ValidRequest_DownloadsSuccessfully()
        {
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
                Duration = TimeSpan.FromMinutes(10),
                ErrorMessage = null
            };

            _validatorMock
                .Setup(v => v.ValidateUrl(request.Url))
                .Returns(ValidationResult.Success());

            _validatorMock
                .Setup(v => v.ValidateDirectory(It.IsAny<string>()))
                .Returns(ValidationResult.Success());

            _downloaderMock
                .Setup(d => d.DownloadAsync(request, It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(downloadResult);

            var result = await _service.DownloadAsync(request);

            result.Success.Should().BeTrue();
            result.OutputPath.Should().Be("/tmp/video.mp4");
            result.FileSizeBytes.Should().Be(1024000);
        }

        [Fact]
        public async Task WithAudioOnly_DownloadsSuccessfully()
        {
            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/audio.mp3",
                TimeRange = null,
                AudioOnly = true
            };

            var downloadResult = new DownloadResult
            {
                Success = true,
                OutputPath = "/tmp/audio.mp3",
                FileSizeBytes = 512000,
                Duration = TimeSpan.FromMinutes(5),
                ErrorMessage = null
            };

            _validatorMock
                .Setup(v => v.ValidateUrl(request.Url))
                .Returns(ValidationResult.Success());

            _validatorMock
                .Setup(v => v.ValidateDirectory(It.IsAny<string>()))
                .Returns(ValidationResult.Success());

            _downloaderMock
                .Setup(d => d.DownloadAsync(request, It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(downloadResult);

            var result = await _service.DownloadAsync(request);

            result.Success.Should().BeTrue();
            request.AudioOnly.Should().BeTrue();
        }

        [Fact]
        public async Task InvalidUrl_ThrowsArgumentException()
        {
            var request = new DownloadRequest
            {
                Url = "invalid-url",
                OutputPath = "/tmp/video.mp4",
                TimeRange = null,
                AudioOnly = false
            };

            _validatorMock
                .Setup(v => v.ValidateUrl(request.Url))
                .Returns(ValidationResult.Failure("URL inválida"));

            var act = async () => await _service.DownloadAsync(request);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("URL inválida");
        }

        [Fact]
        public async Task InvalidDirectory_ThrowsArgumentException()
        {
            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/nonexistent/video.mp4",
                TimeRange = null,
                AudioOnly = false
            };

            _validatorMock
                .Setup(v => v.ValidateUrl(request.Url))
                .Returns(ValidationResult.Success());

            _validatorMock
                .Setup(v => v.ValidateDirectory(It.IsAny<string>()))
                .Returns(ValidationResult.Failure("Diretório não existe"));

            var act = async () => await _service.DownloadAsync(request);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Diretório não existe");
        }

        [Fact]
        public async Task DownloaderFailure_PropagatesFailureResult()
        {
            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/video.mp4",
                TimeRange = null,
                AudioOnly = false
            };

            var downloadResult = new DownloadResult
            {
                Success = false,
                OutputPath = "/tmp/video.mp4",
                FileSizeBytes = 0,
                Duration = TimeSpan.Zero,
                ErrorMessage = "Download failed"
            };

            _validatorMock
                .Setup(v => v.ValidateUrl(request.Url))
                .Returns(ValidationResult.Success());

            _validatorMock
                .Setup(v => v.ValidateDirectory(It.IsAny<string>()))
                .Returns(ValidationResult.Success());

            _downloaderMock
                .Setup(d => d.DownloadAsync(request, It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(downloadResult);

            var result = await _service.DownloadAsync(request);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Download failed");
        }
    }

    public class DownloadAsyncWithTimeRange : DownloadServiceTests
    {
        [Fact]
        public async Task ValidRequest_DownloadsAndProcessesSuccessfully()
        {
            var timeRange = new TimeRange
            {
                StartSeconds = 30,
                EndSeconds = 90
            };

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/clip.mp4",
                TimeRange = timeRange,
                AudioOnly = false
            };

            var downloadResult = new DownloadResult
            {
                Success = true,
                OutputPath = "/tmp/video.mp4",
                FileSizeBytes = 1024000,
                Duration = TimeSpan.FromMinutes(10),
                ErrorMessage = null
            };

            var processingResult = new ProcessingResult
            {
                Success = true,
                OutputPath = "/tmp/clip.mp4",
                FileSizeBytes = 512000,
                ErrorMessage = null
            };

            _validatorMock
                .Setup(v => v.ValidateUrl(request.Url))
                .Returns(ValidationResult.Success());

            _validatorMock
                .Setup(v => v.ValidateDirectory(It.IsAny<string>()))
                .Returns(ValidationResult.Success());

            _downloaderMock
                .Setup(d => d.DownloadAsync(It.IsAny<DownloadRequest>(), It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(downloadResult);

            _processorMock
                .Setup(p => p.ProcessAsync(It.IsAny<ProcessingRequest>(), It.IsAny<IProgress<ProcessingProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(processingResult);

            var result = await _service.DownloadAsync(request);

            result.Success.Should().BeTrue();
            result.OutputPath.Should().Be("/tmp/clip.mp4");
            result.FileSizeBytes.Should().Be(512000);
            result.Duration.Should().Be(TimeSpan.FromSeconds(60));
        }

        [Fact]
        public async Task WithAudioOnly_ProcessesSuccessfully()
        {
            var timeRange = new TimeRange
            {
                StartSeconds = 30,
                EndSeconds = 90
            };

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/clip.mp3",
                TimeRange = timeRange,
                AudioOnly = true
            };

            var downloadResult = new DownloadResult
            {
                Success = true,
                OutputPath = "/tmp/video.mp4",
                FileSizeBytes = 1024000,
                Duration = TimeSpan.FromMinutes(10),
                ErrorMessage = null
            };

            var processingResult = new ProcessingResult
            {
                Success = true,
                OutputPath = "/tmp/clip.mp3",
                FileSizeBytes = 256000,
                ErrorMessage = null
            };

            _validatorMock
                .Setup(v => v.ValidateUrl(request.Url))
                .Returns(ValidationResult.Success());

            _validatorMock
                .Setup(v => v.ValidateDirectory(It.IsAny<string>()))
                .Returns(ValidationResult.Success());

            _downloaderMock
                .Setup(d => d.DownloadAsync(It.IsAny<DownloadRequest>(), It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(downloadResult);

            _processorMock
                .Setup(p => p.ProcessAsync(It.IsAny<ProcessingRequest>(), It.IsAny<IProgress<ProcessingProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(processingResult);

            var result = await _service.DownloadAsync(request);

            result.Success.Should().BeTrue();
            result.OutputPath.Should().Be("/tmp/clip.mp3");
        }

        [Fact]
        public async Task DownloadFailure_ReturnsFailureResult()
        {
            var timeRange = new TimeRange
            {
                StartSeconds = 30,
                EndSeconds = 90
            };

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/clip.mp4",
                TimeRange = timeRange,
                AudioOnly = false
            };

            var downloadResult = new DownloadResult
            {
                Success = false,
                OutputPath = "/tmp/video.mp4",
                FileSizeBytes = 0,
                Duration = TimeSpan.Zero,
                ErrorMessage = "Download failed"
            };

            _validatorMock
                .Setup(v => v.ValidateUrl(request.Url))
                .Returns(ValidationResult.Success());

            _validatorMock
                .Setup(v => v.ValidateDirectory(It.IsAny<string>()))
                .Returns(ValidationResult.Success());

            _downloaderMock
                .Setup(d => d.DownloadAsync(It.IsAny<DownloadRequest>(), It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(downloadResult);

            var result = await _service.DownloadAsync(request);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Download failed");

            _processorMock.Verify(p => p.ProcessAsync(
                It.IsAny<ProcessingRequest>(),
                It.IsAny<IProgress<ProcessingProgress>>(),
                It.IsAny<CancellationToken>()
            ), Times.Never);
        }

        [Fact]
        public async Task ProcessingFailure_ReturnsFailureResult()
        {
            var timeRange = new TimeRange
            {
                StartSeconds = 30,
                EndSeconds = 90
            };

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/clip.mp4",
                TimeRange = timeRange,
                AudioOnly = false
            };

            var downloadResult = new DownloadResult
            {
                Success = true,
                OutputPath = "/tmp/video.mp4",
                FileSizeBytes = 1024000,
                Duration = TimeSpan.FromMinutes(10),
                ErrorMessage = null
            };

            var processingResult = new ProcessingResult
            {
                Success = false,
                OutputPath = "/tmp/clip.mp4",
                FileSizeBytes = 0,
                ErrorMessage = "Processing failed"
            };

            _validatorMock
                .Setup(v => v.ValidateUrl(request.Url))
                .Returns(ValidationResult.Success());

            _validatorMock
                .Setup(v => v.ValidateDirectory(It.IsAny<string>()))
                .Returns(ValidationResult.Success());

            _downloaderMock
                .Setup(d => d.DownloadAsync(It.IsAny<DownloadRequest>(), It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(downloadResult);

            _processorMock
                .Setup(p => p.ProcessAsync(It.IsAny<ProcessingRequest>(), It.IsAny<IProgress<ProcessingProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(processingResult);

            var result = await _service.DownloadAsync(request);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Processing failed");
        }

        [Fact]
        public async Task CancellationDuringDownload_CleansUpTempFile()
        {
            var timeRange = new TimeRange
            {
                StartSeconds = 30,
                EndSeconds = 90
            };

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/clip.mp4",
                TimeRange = timeRange,
                AudioOnly = false
            };

            var cts = new CancellationTokenSource();

            _validatorMock
                .Setup(v => v.ValidateUrl(request.Url))
                .Returns(ValidationResult.Success());

            _validatorMock
                .Setup(v => v.ValidateDirectory(It.IsAny<string>()))
                .Returns(ValidationResult.Success());

            _downloaderMock
                .Setup(d => d.DownloadAsync(It.IsAny<DownloadRequest>(), It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()))
                .Callback<DownloadRequest, IProgress<DownloadProgress>?, CancellationToken>(
                    (req, progress, ct) => cts.Cancel()
                )
                .ThrowsAsync(new OperationCanceledException());

            var act = async () => await _service.DownloadAsync(request, null, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task CancellationDuringProcessing_CleansUpFiles()
        {
            var timeRange = new TimeRange
            {
                StartSeconds = 30,
                EndSeconds = 90
            };

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/clip.mp4",
                TimeRange = timeRange,
                AudioOnly = false
            };

            var downloadResult = new DownloadResult
            {
                Success = true,
                OutputPath = "/tmp/video.mp4",
                FileSizeBytes = 1024000,
                Duration = TimeSpan.FromMinutes(10),
                ErrorMessage = null
            };

            var cts = new CancellationTokenSource();

            _validatorMock
                .Setup(v => v.ValidateUrl(request.Url))
                .Returns(ValidationResult.Success());

            _validatorMock
                .Setup(v => v.ValidateDirectory(It.IsAny<string>()))
                .Returns(ValidationResult.Success());

            _downloaderMock
                .Setup(d => d.DownloadAsync(It.IsAny<DownloadRequest>(), It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(downloadResult);

            _processorMock
                .Setup(p => p.ProcessAsync(It.IsAny<ProcessingRequest>(), It.IsAny<IProgress<ProcessingProgress>>(), It.IsAny<CancellationToken>()))
                .Callback<ProcessingRequest, IProgress<ProcessingProgress>?, CancellationToken>(
                    (req, progress, ct) => cts.Cancel()
                )
                .ThrowsAsync(new OperationCanceledException());

            var act = async () => await _service.DownloadAsync(request, null, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task ProcessingRequest_ContainsCorrectParameters()
        {
            var timeRange = new TimeRange
            {
                StartSeconds = 30,
                EndSeconds = 90
            };

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/clip.mp4",
                TimeRange = timeRange,
                AudioOnly = true
            };

            var downloadResult = new DownloadResult
            {
                Success = true,
                OutputPath = "/tmp/video.mp4",
                FileSizeBytes = 1024000,
                Duration = TimeSpan.FromMinutes(10),
                ErrorMessage = null
            };

            var processingResult = new ProcessingResult
            {
                Success = true,
                OutputPath = "/tmp/clip.mp4",
                FileSizeBytes = 512000,
                ErrorMessage = null
            };

            ProcessingRequest? capturedRequest = null;

            _validatorMock
                .Setup(v => v.ValidateUrl(request.Url))
                .Returns(ValidationResult.Success());

            _validatorMock
                .Setup(v => v.ValidateDirectory(It.IsAny<string>()))
                .Returns(ValidationResult.Success());

            _downloaderMock
                .Setup(d => d.DownloadAsync(It.IsAny<DownloadRequest>(), It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(downloadResult);

            _processorMock
                .Setup(p => p.ProcessAsync(It.IsAny<ProcessingRequest>(), It.IsAny<IProgress<ProcessingProgress>>(), It.IsAny<CancellationToken>()))
                .Callback<ProcessingRequest, IProgress<ProcessingProgress>?, CancellationToken>(
                    (req, progress, ct) => capturedRequest = req
                )
                .ReturnsAsync(processingResult);

            await _service.DownloadAsync(request);

            capturedRequest.Should().NotBeNull();
            capturedRequest!.TimeRange.Should().Be(timeRange);
            capturedRequest.AudioOnly.Should().BeTrue();
            capturedRequest.OutputPath.Should().Be("/tmp/clip.mp4");
        }

        /// <summary>
        /// Regression test for FFmpeg exit code 183 bug.
        /// When yt-dlp returns a different OutputPath than requested (e.g., appends .mp4),
        /// the processor must receive the actual path, not the originally requested temp path.
        /// </summary>
        [Fact]
        public async Task ProcessorReceivesActualDownloadedPath_NotRequestedTempPath()
        {
            var timeRange = new TimeRange
            {
                StartSeconds = 30,
                EndSeconds = 90
            };

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/clip.mp4",
                TimeRange = timeRange,
                AudioOnly = false
            };

            // Simulate yt-dlp returning a DIFFERENT path than requested
            _downloaderMock
                .Setup(d => d.DownloadAsync(It.IsAny<DownloadRequest>(), It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((DownloadRequest req, IProgress<DownloadProgress>? _, CancellationToken _) =>
                    new DownloadResult
                    {
                        Success = true,
                        OutputPath = req.OutputPath + ".webm", // yt-dlp changed the extension!
                        FileSizeBytes = 50_000_000,
                        Duration = TimeSpan.FromMinutes(10),
                        ErrorMessage = null
                    });

            ProcessingRequest? capturedRequest = null;

            var processingResult = new ProcessingResult
            {
                Success = true,
                OutputPath = "/tmp/clip.mp4",
                FileSizeBytes = 512000,
                ErrorMessage = null
            };

            _validatorMock
                .Setup(v => v.ValidateUrl(request.Url))
                .Returns(ValidationResult.Success());

            _validatorMock
                .Setup(v => v.ValidateDirectory(It.IsAny<string>()))
                .Returns(ValidationResult.Success());

            _processorMock
                .Setup(p => p.ProcessAsync(It.IsAny<ProcessingRequest>(), It.IsAny<IProgress<ProcessingProgress>>(), It.IsAny<CancellationToken>()))
                .Callback<ProcessingRequest, IProgress<ProcessingProgress>?, CancellationToken>(
                    (req, _, _) => capturedRequest = req
                )
                .ReturnsAsync(processingResult);

            await _service.DownloadAsync(request);

            capturedRequest.Should().NotBeNull();
            capturedRequest!.InputPath.Should().EndWith(".webm",
                "processor must receive the actual path returned by the downloader, not the requested temp path");
        }

        [Fact]
        public async Task TempFileUsesProperExtension_NotTmp()
        {
            var timeRange = new TimeRange
            {
                StartSeconds = 30,
                EndSeconds = 90
            };

            var request = new DownloadRequest
            {
                Url = "https://www.youtube.com/watch?v=test",
                OutputPath = "/tmp/clip.mp4",
                TimeRange = timeRange,
                AudioOnly = false
            };

            DownloadRequest? capturedDownloadRequest = null;

            _downloaderMock
                .Setup(d => d.DownloadAsync(It.IsAny<DownloadRequest>(), It.IsAny<IProgress<DownloadProgress>>(), It.IsAny<CancellationToken>()))
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

            var processingResult = new ProcessingResult
            {
                Success = true,
                OutputPath = "/tmp/clip.mp4",
                FileSizeBytes = 512000,
                ErrorMessage = null
            };

            _validatorMock
                .Setup(v => v.ValidateUrl(request.Url))
                .Returns(ValidationResult.Success());

            _validatorMock
                .Setup(v => v.ValidateDirectory(It.IsAny<string>()))
                .Returns(ValidationResult.Success());

            _processorMock
                .Setup(p => p.ProcessAsync(It.IsAny<ProcessingRequest>(), It.IsAny<IProgress<ProcessingProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(processingResult);

            await _service.DownloadAsync(request);

            capturedDownloadRequest.Should().NotBeNull();
            capturedDownloadRequest!.OutputPath.Should().EndWith(".mp4",
                "temp file must use .mp4 extension to prevent yt-dlp from renaming");
            capturedDownloadRequest.OutputPath.Should().NotEndWith(".tmp",
                ".tmp causes yt-dlp to append .mp4, leaving the original file empty");
        }
    }
}
