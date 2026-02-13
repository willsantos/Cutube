using FluentAssertions;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Moq;
using Cutube.Cli.ErrorHandling;
using Cutube.Cli;

namespace Cutube.Tests.Unit;

[Collection("Sequential")]
public class MenuTests
{
    private readonly Mock<IFileService> _fileService;
    private readonly Mock<IErrorHandler> _errorHandler;

    public MenuTests()
    {
        Cutube.Cli.Menu.Reset();
        _fileService = new Mock<IFileService>();
        _fileService.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(true);
        _fileService.Setup(f => f.HasWritePermission(It.IsAny<string>())).Returns(true);

        _errorHandler = new Mock<IErrorHandler>();
        _errorHandler.Setup(h => h.GetUserFriendlyMessage(It.IsAny<Exception>()))
            .Returns<Exception>(ex => ex.Message);
    }

    [Fact]
    public void Show_ValidInput_SetsPropertiesCorrectly()
    {
        var input = "https://youtube.com/watch?v=abc123\n00:00:10\n00:01:00\n\n\n1\n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        var console = new ConsoleService();
        var result = Cutube.Cli.Menu.Show(console, _fileService.Object, _errorHandler.Object);

        result.IsSuccess.Should().BeTrue();
        Cutube.Cli.Menu.Url.Should().Be("https://youtube.com/watch?v=abc123");
        Cutube.Cli.Menu.Start.Should().Be("00:00:10");
        Cutube.Cli.Menu.End.Should().Be("00:01:00");
    }

    [Fact]
    public void Show_DifferentUrl_SetsUrlCorrectly()
    {
        var input = "https://youtu.be/dQw4w9WgXcQ\n00:01:00\n00:02:00\n\n\n1\n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        var console = new ConsoleService();
        var result = Cutube.Cli.Menu.Show(console, _fileService.Object, _errorHandler.Object);

        result.IsSuccess.Should().BeTrue();
        Cutube.Cli.Menu.Url.Should().Be("https://youtu.be/dQw4w9WgXcQ");
        Cutube.Cli.Menu.Start.Should().Be("00:01:00");
        Cutube.Cli.Menu.End.Should().Be("00:02:00");
    }

    [Fact]
    public void Show_WithCustomFileName_SetsCustomFileName()
    {
        var input = "https://youtube.com/watch?v=abc123\n00:00:10\n00:01:00\nmeu-podcast-01\n\n1\n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        var console = new ConsoleService();
        var result = Cutube.Cli.Menu.Show(console, _fileService.Object, _errorHandler.Object);

        result.IsSuccess.Should().BeTrue();
        Cutube.Cli.Menu.CustomFileName.Should().Be("meu-podcast-01");
    }

    [Fact]
    public void Show_WithCustomFileNameWithExtension_RemovesExtension()
    {
        var input = "https://youtube.com/watch?v=abc123\n00:00:10\n00:01:00\nmeu-video.mp4\n\n1\n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        var console = new ConsoleService();
        var result = Cutube.Cli.Menu.Show(console, _fileService.Object, _errorHandler.Object);

        result.IsSuccess.Should().BeTrue();
        Cutube.Cli.Menu.CustomFileName.Should().Be("meu-video");
    }

    [Fact]
    public void Show_WithoutCustomFileName_RemainsEmpty()
    {
        var input = "https://youtube.com/watch?v=abc123\n00:00:10\n00:01:00\n\n\n1\n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        var console = new ConsoleService();
        var result = Cutube.Cli.Menu.Show(console, _fileService.Object, _errorHandler.Object);

        result.IsSuccess.Should().BeTrue();
        Cutube.Cli.Menu.CustomFileName.Should().BeEmpty();
    }

    [Fact]
    public void Show_WithWhitespaceCustomFileName_RemainsEmpty()
    {
        var input = "https://youtube.com/watch?v=abc123\n00:00:10\n00:01:00\n   \n\n1\n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        var console = new ConsoleService();
        var result = Cutube.Cli.Menu.Show(console, _fileService.Object, _errorHandler.Object);

        result.IsSuccess.Should().BeTrue();
        Cutube.Cli.Menu.CustomFileName.Should().BeEmpty();
    }

    [Fact]
    public void OutputDirectory_DeveSerVazioPorPadrao()
    {
        var input = "https://youtube.com/watch?v=abc123\n00:01:00\n00:02:00\n\n\n1\n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        var console = new ConsoleService();
        var result = Cutube.Cli.Menu.Show(console, _fileService.Object, _errorHandler.Object);

        result.IsSuccess.Should().BeTrue();
        Cutube.Cli.Menu.OutputDirectory.Should().Be("");
    }

    [Fact]
    public void OutputDirectory_DeveAceitarCaminhoCustomizado()
    {
        var input = "https://youtube.com/watch?v=abc123\n00:01:00\n00:02:00\n\n/tmp/downloads\n1\n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        var console = new ConsoleService();
        var result = Cutube.Cli.Menu.Show(console, _fileService.Object, _errorHandler.Object);

        result.IsSuccess.Should().BeTrue();
        Cutube.Cli.Menu.OutputDirectory.Should().Be("/tmp/downloads");
    }

    [Fact]
    public void OutputDirectory_DeveAceitarCaminhoWindows()
    {
        var input = "https://youtube.com/watch?v=abc123\n00:01:00\n00:02:00\n\nC:\\Videos\n1\n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        var console = new ConsoleService();
        var result = Cutube.Cli.Menu.Show(console, _fileService.Object, _errorHandler.Object);

        result.IsSuccess.Should().BeTrue();
        Cutube.Cli.Menu.OutputDirectory.Should().Be("C:\\Videos");
    }

    [Fact]
    public void OutputDirectory_DeveRemoverEspacosEmBranco()
    {
        var input = "https://youtube.com/watch?v=abc123\n00:01:00\n00:02:00\n\n  /tmp/videos  \n1\n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        var console = new ConsoleService();
        var result = Cutube.Cli.Menu.Show(console, _fileService.Object, _errorHandler.Object);

        result.IsSuccess.Should().BeTrue();
        Cutube.Cli.Menu.OutputDirectory.Should().Be("/tmp/videos");
    }

    [Fact]
    public void OutputDirectory_DeveAceitarCaminhoRelativo()
    {
        var input = "https://youtube.com/watch?v=abc123\n00:01:00\n00:02:00\n\n../downloads\n1\n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        var console = new ConsoleService();
        var result = Cutube.Cli.Menu.Show(console, _fileService.Object, _errorHandler.Object);

        result.IsSuccess.Should().BeTrue();
        Cutube.Cli.Menu.OutputDirectory.Should().Be("../downloads");
    }
}
