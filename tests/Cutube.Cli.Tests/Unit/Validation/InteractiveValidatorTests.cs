using FluentAssertions;
using Moq;
using Xunit;
using cutube.Validation;
using Cutube.ErrorHandling;
using Cutube.Cli;

namespace Cutube.Tests.Unit.Validation;

public class InteractiveValidatorTests
{
    private Mock<IConsoleService> _console;
    private Mock<IErrorHandler> _errorHandler;
    private Mock<IFileService> _fileService;

    public InteractiveValidatorTests()
    {
        _console = new Mock<IConsoleService>();
        _errorHandler = new Mock<IErrorHandler>();
        _fileService = new Mock<IFileService>();

        _errorHandler.Setup(h => h.GetUserFriendlyMessage(It.IsAny<Exception>()))
            .Returns<Exception>(ex => ex.Message);
    }

    #region URL Tests

    [Fact]
    public void GetValidUrl_ValidUrl_ReturnsUrl()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("https://youtube.com/watch?v=abc123");

        var result = InteractiveValidator.GetValidUrl(_console.Object, _errorHandler.Object);

        result.Should().Be("https://youtube.com/watch?v=abc123");
        _console.Verify(c => c.Write("URL do vídeo: "), Times.Once);
        _console.Verify(c => c.WriteLine(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetValidUrl_InvalidUrl_PromptsAgain_SucceedsOnSecondAttempt()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("invalid-url")
            .Returns("https://youtu.be/abc123");

        var result = InteractiveValidator.GetValidUrl(_console.Object, _errorHandler.Object);

        result.Should().Be("https://youtu.be/abc123");
        _console.Verify(c => c.Write("URL do vídeo: "), Times.Exactly(2));
        _console.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("❌"))), Times.Once);
    }

    [Fact]
    public void GetValidUrl_EmptyInput_ThrowsAfter3Attempts()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("")
            .Returns("   ")
            .Returns("");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            InteractiveValidator.GetValidUrl(_console.Object, _errorHandler.Object)
        );

        ex.Message.Should().Contain("Máximo de tentativas atingido");
        _console.Verify(c => c.Write("URL do vídeo: "), Times.Exactly(3));
        _console.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("❌"))), Times.AtLeast(2));
    }

    #endregion

    #region StartTime Tests

    [Fact]
    public void GetValidStartTime_ValidTime_ReturnsTime()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("90s");

        var result = InteractiveValidator.GetValidStartTime(_console.Object, _errorHandler.Object);

        result.Should().Be("90s");
    }

    [Fact]
    public void GetValidStartTime_InvalidFormat_PromptsAgain()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("invalid")
            .Returns("90s");

        var result = InteractiveValidator.GetValidStartTime(_console.Object, _errorHandler.Object);

        result.Should().Be("90s");
        _console.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("❌"))), Times.Once);
    }

    [Fact]
    public void GetValidStartTime_ZeroOrNegative_ThrowsAfter3Attempts()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("0")
            .Returns("-10")
            .Returns("0");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            InteractiveValidator.GetValidStartTime(_console.Object, _errorHandler.Object)
        );

        ex.Message.Should().Contain("Máximo de tentativas atingido");
    }

    #endregion

    #region EndTime Tests

    [Fact]
    public void GetValidEndTime_ValidTimeGreaterThanStart_ReturnsTime()
    {
        _console.Setup(c => c.ReadLine()).Returns("120s");

        var result = InteractiveValidator.GetValidEndTime(_console.Object, _errorHandler.Object, "90s");

        result.Should().Be("120s");
    }

    [Fact]
    public void GetValidEndTime_LessThanStart_PromptsAgain()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("60s")
            .Returns("120s");

        var result = InteractiveValidator.GetValidEndTime(_console.Object, _errorHandler.Object, "90s");

        result.Should().Be("120s");
        _console.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("maior que o tempo de início"))), Times.Once);
    }

    [Fact]
    public void GetValidEndTime_EqualToStart_PromptsAgain()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("90s")
            .Returns("120s");

        var result = InteractiveValidator.GetValidEndTime(_console.Object, _errorHandler.Object, "90s");

        result.Should().Be("120s");
    }

    #endregion

    #region FileName Tests

    [Fact]
    public void GetValidFileName_ValidName_ReturnsName()
    {
        _console.Setup(c => c.ReadLine()).Returns("meu-video");

        var result = InteractiveValidator.GetValidFileName(_console.Object, _errorHandler.Object);

        result.Should().Be("meu-video");
    }

    [Fact]
    public void GetValidFileName_Empty_ReturnsEmpty()
    {
        _console.Setup(c => c.ReadLine()).Returns("");

        var result = InteractiveValidator.GetValidFileName(_console.Object, _errorHandler.Object);

        result.Should().Be("");
    }

    [Fact]
    public void GetValidFileName_InvalidChars_PromptsAgain_SkipsOnEmpty()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("video/invalid")
            .Returns("");

        var result = InteractiveValidator.GetValidFileName(_console.Object, _errorHandler.Object);

        result.Should().Be("");
        _console.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("❌"))), Times.Once);
    }

    [Fact]
    public void GetValidFileName_InvalidChars_ThrowsAfter3Attempts()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("video|1")
            .Returns("video<test>")
            .Returns("video*invalid");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            InteractiveValidator.GetValidFileName(_console.Object, _errorHandler.Object)
        );

        ex.Message.Should().Contain("Máximo de tentativas atingido");
    }

    #endregion

    #region Directory Tests

    [Fact]
    public void GetValidDirectory_ValidDirectory_ReturnsPath()
    {
        _console.Setup(c => c.ReadLine()).Returns("/tmp");
        _fileService.Setup(f => f.DirectoryExists("/tmp")).Returns(true);
        _fileService.Setup(f => f.HasWritePermission("/tmp")).Returns(true);

        var result = InteractiveValidator.GetValidDirectory(_console.Object, _errorHandler.Object, _fileService.Object);

        result.Should().Be("/tmp");
    }

    [Fact]
    public void GetValidDirectory_Empty_ReturnsEmpty()
    {
        _console.Setup(c => c.ReadLine()).Returns("");

        var result = InteractiveValidator.GetValidDirectory(_console.Object, _errorHandler.Object, _fileService.Object);

        result.Should().Be("");
    }

    [Fact]
    public void GetValidDirectory_NotFound_PromptsAgain_SkipsOnEmpty()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("/nonexistent")
            .Returns("");
        _fileService.Setup(f => f.DirectoryExists("/nonexistent")).Returns(false);

        var result = InteractiveValidator.GetValidDirectory(_console.Object, _errorHandler.Object, _fileService.Object);

        result.Should().Be("");
        _console.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("❌"))), Times.Once);
    }

    [Fact]
    public void GetValidDirectory_NoPermission_ThrowsAfter3Attempts()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("/root")
            .Returns("/restricted")
            .Returns("/protected");
        _fileService.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(true);
        _fileService.Setup(f => f.HasWritePermission(It.IsAny<string>())).Returns(false);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            InteractiveValidator.GetValidDirectory(_console.Object, _errorHandler.Object, _fileService.Object)
        );

        ex.Message.Should().Contain("Máximo de tentativas atingido");
    }

    #endregion
}
