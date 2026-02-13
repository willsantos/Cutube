using FluentAssertions;
using Moq;
using Xunit;
using cutube;

namespace Cutube.Tests.Unit;

public class ValidationHelperTests
{
    #region URL Tests

    [Theory]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("http://youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("http://youtu.be/dQw4w9WgXcQ")]
    public void IsValidUrl_ValidYouTubeUrl_ReturnsTrue(string url)
    {
        var result = cutube.ValidationHelper.IsValidUrl(url);
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsValidUrl_NullOrWhitespace_ReturnsFalse(string? url)
    {
        var result = cutube.ValidationHelper.IsValidUrl(url!);
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("youtube.com")]
    [InlineData("ftp://youtube.com/watch?v=abc")]
    [InlineData("https://google.com")]
    [InlineData("https://vimeo.com/12345")]
    public void IsValidUrl_InvalidUrl_ReturnsFalse(string url)
    {
        var result = cutube.ValidationHelper.IsValidUrl(url);
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateUrl_ValidUrl_DoesNotThrow()
    {
        var act = () => cutube.ValidationHelper.ValidateUrl("https://youtube.com/watch?v=dQw4w9WgXcQ");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("https://google.com")]
    public void ValidateUrl_InvalidUrl_ThrowsArgumentException(string url)
    {
        var act = () => cutube.ValidationHelper.ValidateUrl(url);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*URL inválida*");
    }

    #endregion

    #region TimeRange Tests

    [Theory]
    [InlineData(60, 120)]
    [InlineData(1, 2)]
    [InlineData(90, 5400)]
    public void IsValidTimeRange_ValidRange_ReturnsTrue(int start, int end)
    {
        var result = cutube.ValidationHelper.IsValidTimeRange(start, end);
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(60, 60)]
    [InlineData(120, 60)]
    public void IsValidTimeRange_InvalidRange_ReturnsFalse(int start, int end)
    {
        var result = cutube.ValidationHelper.IsValidTimeRange(start, end);
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("00:01:00", "00:02:00")]
    [InlineData("90s", "2m")]
    [InlineData("1h", "2h")]
    [InlineData("1:30", "2:00")]
    public void ValidateTimeRange_ValidRange_DoesNotThrow(string start, string end)
    {
        var act = () => cutube.ValidationHelper.ValidateTimeRange(start, end);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTimeRange_StartIsZero_ThrowsArgumentException()
    {
        var act = () => cutube.ValidationHelper.ValidateTimeRange("0", "60");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tempo de início deve ser maior que zero*");
    }

    [Fact]
    public void ValidateTimeRange_StartEqualsEnd_ThrowsArgumentException()
    {
        var act = () => cutube.ValidationHelper.ValidateTimeRange("60", "60");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tempo de fim deve ser maior que o tempo de início*");
    }

    [Fact]
    public void ValidateTimeRange_StartGreaterThanEnd_ThrowsArgumentException()
    {
        var act = () => cutube.ValidationHelper.ValidateTimeRange("120", "60");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tempo de fim deve ser maior que o tempo de início*");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("h30m")]
    public void ValidateTimeRange_InvalidFormat_ThrowsFormatException(string time)
    {
        var act = () => cutube.ValidationHelper.ValidateTimeRange(time, "120");
        act.Should().Throw<FormatException>();
    }

    #endregion

    #region FileName Tests

    [Theory]
    [InlineData("valid-name")]
    [InlineData("My Video 2024")]
    [InlineData("video_test-123")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsValidFileName_ValidName_ReturnsTrue(string? name)
    {
        var result = cutube.ValidationHelper.IsValidFileName(name!);
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid<>name")]
    [InlineData("video|test")]
    [InlineData("file*name")]
    [InlineData("test?file")]
    [InlineData("file:name")]
    [InlineData("file\"name")]
    [InlineData("path/to/file")]
    [InlineData("path\\file")]
    [InlineData("../file")]
    public void IsValidFileName_InvalidName_ReturnsFalse(string name)
    {
        var result = cutube.ValidationHelper.IsValidFileName(name);
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("valid-name")]
    [InlineData("My Video 2024")]
    public void ValidateFileName_ValidName_DoesNotThrow(string name)
    {
        var act = () => cutube.ValidationHelper.ValidateFileName(name);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateFileName_EmptyOrWhitespace_DoesNotThrow()
    {
        var act = () => cutube.ValidationHelper.ValidateFileName("");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("invalid<>name")]
    [InlineData("path/to/file")]
    [InlineData("path\\file")]
    public void ValidateFileName_InvalidName_ThrowsArgumentException(string name)
    {
        var act = () => cutube.ValidationHelper.ValidateFileName(name);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Nome do arquivo inválido*");
    }

    #endregion

    #region Directory Tests

    [Fact]
    public void IsDirectoryWritable_ExistingWritableDirectory_ReturnsTrue()
    {
        var mockFileService = new Mock<IFileService>();
        mockFileService.Setup(f => f.DirectoryExists("/valid/path")).Returns(true);
        mockFileService.Setup(f => f.HasWritePermission("/valid/path")).Returns(true);

        var result = cutube.ValidationHelper.IsDirectoryWritable("/valid/path", mockFileService.Object);
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDirectoryWritable_NonExistingDirectory_ReturnsFalse()
    {
        var mockFileService = new Mock<IFileService>();
        mockFileService.Setup(f => f.DirectoryExists("/invalid/path")).Returns(false);

        var result = cutube.ValidationHelper.IsDirectoryWritable("/invalid/path", mockFileService.Object);
        result.Should().BeFalse();
    }

    [Fact]
    public void IsDirectoryWritable_NoWritePermission_ReturnsFalse()
    {
        var mockFileService = new Mock<IFileService>();
        mockFileService.Setup(f => f.DirectoryExists("/readonly/path")).Returns(true);
        mockFileService.Setup(f => f.HasWritePermission("/readonly/path")).Returns(false);

        var result = cutube.ValidationHelper.IsDirectoryWritable("/readonly/path", mockFileService.Object);
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateDirectory_ExistingWritableDirectory_DoesNotThrow()
    {
        var mockFileService = new Mock<IFileService>();
        mockFileService.Setup(f => f.DirectoryExists("/valid/path")).Returns(true);
        mockFileService.Setup(f => f.HasWritePermission("/valid/path")).Returns(true);

        var act = () => cutube.ValidationHelper.ValidateDirectory("/valid/path", mockFileService.Object);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateDirectory_NonExistingDirectory_ThrowsDirectoryNotFoundException()
    {
        var mockFileService = new Mock<IFileService>();
        mockFileService.Setup(f => f.DirectoryExists("/invalid/path")).Returns(false);

        var act = () => cutube.ValidationHelper.ValidateDirectory("/invalid/path", mockFileService.Object);
        act.Should().Throw<DirectoryNotFoundException>()
            .WithMessage("*Diretório não encontrado*");
    }

    [Fact]
    public void ValidateDirectory_NoWritePermission_ThrowsUnauthorizedAccessException()
    {
        var mockFileService = new Mock<IFileService>();
        mockFileService.Setup(f => f.DirectoryExists("/readonly/path")).Returns(true);
        mockFileService.Setup(f => f.HasWritePermission("/readonly/path")).Returns(false);

        var act = () => cutube.ValidationHelper.ValidateDirectory("/readonly/path", mockFileService.Object);
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*Sem permissão de escrita*");
    }

    #endregion
}
