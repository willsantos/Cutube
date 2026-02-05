using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using FluentAssertions;
using Moq;

namespace Cutube.Domain.Tests.Services;

public class MetadataServiceTests
{
    private readonly Mock<IVideoMetadataProvider> _providerMock;
    private readonly Mock<IDownloadValidator> _validatorMock;
    private readonly MetadataService _service;

    public MetadataServiceTests()
    {
        _providerMock = new Mock<IVideoMetadataProvider>();
        _validatorMock = new Mock<IDownloadValidator>();
        _service = new MetadataService(_providerMock.Object, _validatorMock.Object);
    }

    public class GetMetadataAsync : MetadataServiceTests
    {
        [Fact]
        public async Task ValidUrl_ReturnsSanitizedMetadata()
        {
            var url = "https://www.youtube.com/watch?v=test";
            var metadata = new VideoMetadata
            {
                Url = url,
                Title = "Test: Video <with> \"invalid\" chars? | and* spaces",
                Uploader = "Test Uploader",
                Duration = TimeSpan.FromMinutes(10),
                UploadDate = DateTime.Now,
                ThumbnailUrl = "https://example.com/thumb.jpg"
            };

            _validatorMock
                .Setup(v => v.ValidateUrl(url))
                .Returns(ValidationResult.Success());

            _providerMock
                .Setup(p => p.GetMetadataAsync(url, It.IsAny<CancellationToken>()))
                .ReturnsAsync(metadata);

            var result = await _service.GetMetadataAsync(url);

            result.Title.Should().Be("Test Video with invalid chars and spaces");
            result.Url.Should().Be(url);
            result.Uploader.Should().Be("Test Uploader");
            result.Duration.Should().Be(TimeSpan.FromMinutes(10));
        }

        [Fact]
        public async Task InvalidUrl_ThrowsArgumentException()
        {
            var url = "invalid-url";
            var validationResult = ValidationResult.Failure("URL inválida");

            _validatorMock
                .Setup(v => v.ValidateUrl(url))
                .Returns(validationResult);

            var act = async () => await _service.GetMetadataAsync(url);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("URL inválida");
        }

        [Fact]
        public async Task ProviderThrowsException_PropagatesException()
        {
            var url = "https://www.youtube.com/watch?v=test";

            _validatorMock
                .Setup(v => v.ValidateUrl(url))
                .Returns(ValidationResult.Success());

            _providerMock
                .Setup(p => p.GetMetadataAsync(url, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Metadata not available"));

            var act = async () => await _service.GetMetadataAsync(url);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Metadata not available");
        }

        [Fact]
        public async Task WithCancellationToken_RespectsCancellation()
        {
            var url = "https://www.youtube.com/watch?v=test";
            var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            _validatorMock
                .Setup(v => v.ValidateUrl(url))
                .Returns(ValidationResult.Success());

            _providerMock
                .Setup(p => p.GetMetadataAsync(url, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var act = async () => await _service.GetMetadataAsync(url, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task MultipleInvalidCharacters_AllReplacedWithSpaces()
        {
            var url = "https://www.youtube.com/watch?v=test";
            var metadata = new VideoMetadata
            {
                Url = url,
                Title = "<>:\"|?*Test<>:\"|?*",
                Uploader = null,
                Duration = TimeSpan.FromMinutes(5),
                UploadDate = DateTime.Now,
                ThumbnailUrl = "https://example.com/thumb.jpg"
            };

            _validatorMock
                .Setup(v => v.ValidateUrl(url))
                .Returns(ValidationResult.Success());

            _providerMock
                .Setup(p => p.GetMetadataAsync(url, It.IsAny<CancellationToken>()))
                .ReturnsAsync(metadata);

            var result = await _service.GetMetadataAsync(url);

            result.Title.Should().Be("Test");
        }

        [Fact]
        public async Task MultipleSpaces_ConvertedToSingleSpace()
        {
            var url = "https://www.youtube.com/watch?v=test";
            var metadata = new VideoMetadata
            {
                Url = url,
                Title = "Test    Video    With     Spaces",
                Uploader = null,
                Duration = TimeSpan.FromMinutes(5),
                UploadDate = DateTime.Now,
                ThumbnailUrl = "https://example.com/thumb.jpg"
            };

            _validatorMock
                .Setup(v => v.ValidateUrl(url))
                .Returns(ValidationResult.Success());

            _providerMock
                .Setup(p => p.GetMetadataAsync(url, It.IsAny<CancellationToken>()))
                .ReturnsAsync(metadata);

            var result = await _service.GetMetadataAsync(url);

            result.Title.Should().Be("Test Video With Spaces");
        }

        [Fact]
        public async Task TitleWithLeadingTrailingSpaces_Trimmed()
        {
            var url = "https://www.youtube.com/watch?v=test";
            var metadata = new VideoMetadata
            {
                Url = url,
                Title = "   Test Video Title   ",
                Uploader = null,
                Duration = TimeSpan.FromMinutes(5),
                UploadDate = DateTime.Now,
                ThumbnailUrl = "https://example.com/thumb.jpg"
            };

            _validatorMock
                .Setup(v => v.ValidateUrl(url))
                .Returns(ValidationResult.Success());

            _providerMock
                .Setup(p => p.GetMetadataAsync(url, It.IsAny<CancellationToken>()))
                .ReturnsAsync(metadata);

            var result = await _service.GetMetadataAsync(url);

            result.Title.Should().Be("Test Video Title");
        }

        [Fact]
        public async Task PreservesOriginalMetadata_ExceptTitle()
        {
            var url = "https://www.youtube.com/watch?v=test";
            var uploadDate = new DateTime(2024, 1, 1);
            var duration = TimeSpan.FromMinutes(15);
            var uploader = "Test Channel";
            var thumbnail = "https://example.com/thumb.jpg";

            var metadata = new VideoMetadata
            {
                Url = url,
                Title = "Original: Title <with> invalid* chars",
                Uploader = uploader,
                Duration = duration,
                UploadDate = uploadDate,
                ThumbnailUrl = thumbnail
            };

            _validatorMock
                .Setup(v => v.ValidateUrl(url))
                .Returns(ValidationResult.Success());

            _providerMock
                .Setup(p => p.GetMetadataAsync(url, It.IsAny<CancellationToken>()))
                .ReturnsAsync(metadata);

            var result = await _service.GetMetadataAsync(url);

            result.Url.Should().Be(url);
            result.Uploader.Should().Be(uploader);
            result.Duration.Should().Be(duration);
            result.UploadDate.Should().Be(uploadDate);
            result.ThumbnailUrl.Should().Be(thumbnail);
        }
    }
}
