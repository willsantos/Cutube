using Cutube.Domain.Services;
using FluentAssertions;

namespace Cutube.Domain.Tests.Services;

public class ValidationServiceTests
{
    private readonly ValidationService _validator;

    public ValidationServiceTests()
    {
        _validator = new ValidationService();
    }

    public class ValidateUrl : ValidationServiceTests
    {
        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://youtube.com/watch?v=test")]
        [InlineData("https://m.youtube.com/watch?v=test")]
        [InlineData("https://youtu.be/dQw4w9WgXcQ")]
        [InlineData("http://www.youtube.com/watch?v=test")]
        public void ValidYouTubeUrl_ReturnsSuccess(string url)
        {
            var result = _validator.ValidateUrl(url);

            result.IsValid.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void EmptyUrl_ReturnsFailure(string? url)
        {
            var result = _validator.ValidateUrl(url!);

            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("URL não pode ser vazia");
        }

        [Theory]
        [InlineData("not-a-url")]
        [InlineData("youtube")]
        public void InvalidUrlFormat_ReturnsFailure(string url)
        {
            var result = _validator.ValidateUrl(url);

            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("URL inválida");
        }

        [Theory]
        [InlineData("htp://invalid.com")]
        [InlineData("ftp://youtube.com/watch?v=test")]
        [InlineData("file://localhost/test")]
        public void InvalidUrlScheme_ReturnsFailure(string url)
        {
            var result = _validator.ValidateUrl(url);

            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("URL deve usar http ou https");
        }

        [Theory]
        [InlineData("ftp://youtube.com/watch?v=test")]
        [InlineData("file://localhost/test")]
        public void NonHttpScheme_ReturnsFailure(string url)
        {
            var result = _validator.ValidateUrl(url);

            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("URL deve usar http ou https");
        }

        [Theory]
        [InlineData("https://vimeo.com/123456")]
        [InlineData("https://www.google.com")]
        [InlineData("https://example.com/watch?v=test")]
        public void NonYouTubeDomain_ReturnsFailure(string url)
        {
            var result = _validator.ValidateUrl(url);

            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("URL deve ser do YouTube");
        }
    }

    public class ValidateFileName : ValidationServiceTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void EmptyFileName_ReturnsSuccess(string? name)
        {
            var result = _validator.ValidateFileName(name!);

            result.IsValid.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
        }

        [Theory]
        [InlineData("video.mp4")]
        [InlineData("My Video")]
        [InlineData("video_with_underscores.mp3")]
        public void ValidFileName_ReturnsSuccess(string name)
        {
            var result = _validator.ValidateFileName(name);

            result.IsValid.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
        }

        [Theory]
        [InlineData("video<test>.mp4")]
        [InlineData("video:test")]
        [InlineData("video\"test")]
        [InlineData("video|test")]
        [InlineData("video?test")]
        [InlineData("video*test")]
        public void FileNameWithInvalidChars_ReturnsFailure(string name)
        {
            var result = _validator.ValidateFileName(name);

            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Nome contém caracteres inválidos");
        }

        [Theory]
        [InlineData("path/to/video.mp4")]
        [InlineData("path\\to\\video.mp4")]
        public void FileNameWithPathSeparator_ReturnsFailure(string name)
        {
            var result = _validator.ValidateFileName(name);

            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Nome contém caracteres inválidos");
        }
    }

    public class ValidateTimeRange : ValidationServiceTests
    {
        [Theory]
        [InlineData("1:30", "5:00")]
        [InlineData("90s", "300s")]
        [InlineData("01:00", "02:30")]
        [InlineData("1:00:00", "2:30:00")]
        public void ValidTimeRange_ReturnsSuccess(string start, string end)
        {
            var result = _validator.ValidateTimeRange(start, end);

            result.IsValid.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
        }

        [Theory]
        [InlineData("0", "60")]
        [InlineData("-30", "60")]
        [InlineData("-1:30", "5:00")]
        public void StartLessThanOrEqualToZero_ReturnsFailure(string start, string end)
        {
            var result = _validator.ValidateTimeRange(start, end);

            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Tempo de início deve ser maior que zero");
        }

        [Theory]
        [InlineData("60", "60")]
        [InlineData("120", "60")]
        [InlineData("5:00", "1:30")]
        public void EndLessThanOrEqualToStart_ReturnsFailure(string start, string end)
        {
            var result = _validator.ValidateTimeRange(start, end);

            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Tempo de fim deve ser maior que o início");
        }

        [Theory]
        [InlineData("invalid", "60")]
        [InlineData("1:30", "invalid")]
        [InlineData("bad", "bad")]
        public void InvalidTimeFormat_ReturnsFailure(string start, string end)
        {
            var result = _validator.ValidateTimeRange(start, end);

            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().StartWith("Formato de tempo inválido");
        }
    }

    public class ValidateDirectory : ValidationServiceTests
    {
        [Fact]
        public void ExistingDirectoryWithWritePermission_ReturnsSuccess()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var result = _validator.ValidateDirectory(tempDir);

                result.IsValid.Should().BeTrue();
                result.ErrorMessage.Should().BeNull();
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir);
            }
        }

        [Fact]
        public void NonExistingDirectory_ReturnsFailure()
        {
            var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            var result = _validator.ValidateDirectory(nonExistentDir);

            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Diretório não existe");
        }

        [Fact(Skip = "Requires special OS permissions")]
        [Trait("Category", "RequiresRoot")]
        public void DirectoryWithoutWritePermission_ReturnsFailure()
        {
            if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            {
                return;
            }

            var readOnlyDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(readOnlyDir);

            try
            {
                File.SetAttributes(readOnlyDir, FileAttributes.ReadOnly);

                var result = _validator.ValidateDirectory(readOnlyDir);

                result.IsValid.Should().BeFalse();
                result.ErrorMessage.Should().Be("Sem permissão de escrita no diretório");
            }
            finally
            {
                try
                {
                    File.SetAttributes(readOnlyDir, FileAttributes.Normal);
                    Directory.Delete(readOnlyDir);
                }
                catch { }
            }
        }
    }
}
