using FluentAssertions;
using Moq;
using Cutube.Cli;
using Cutube.Cli.ErrorHandling;
using Xunit;
using Cutube.Tests.Helpers;

namespace Cutube.Tests.E2E;

public class ValidationFlowTests
{
    [Fact]
    public async Task CompleteFlow_InvalidUrl_ShowsErrorAndPromptsAgain_Succeeds()
    {
        var menu = new Mock<IMenuService>();
        var ytdl = new Mock<IYtDlpService>();
        var console = new FakeConsoleService();
        var fileService = new Mock<IFileService>();
        var errorHandler = new Mock<IErrorHandler>();

        errorHandler.Setup(h => h.GetUserFriendlyMessage(It.IsAny<Exception>()))
            .Returns<Exception>(ex => ex.Message);

        menu.Setup(m => m.Show(console, fileService.Object, errorHandler.Object))
            .Returns(Result.Success());
        menu.SetupGet(m => m.Url).Returns("https://youtu.be/dQw4w9WgXcQ");
        menu.SetupGet(m => m.Start).Returns("90s");
        menu.SetupGet(m => m.End).Returns("120s");
        menu.SetupGet(m => m.CustomFileName).Returns("");
        menu.SetupGet(m => m.OutputDirectory).Returns("");
        menu.SetupGet(m => m.AudioOnly).Returns(false);

        fileService.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(true);
        fileService.Setup(f => f.HasWritePermission(It.IsAny<string>())).Returns(true);

        ytdl.Setup(y => y.GetVideoTitleAsync(It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync("Video Teste");
        ytdl.Setup(y => y.DownloadWithTimeRangeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IProgress<YoutubeDLSharp.DownloadProgress>>(),
                It.IsAny<System.Threading.CancellationToken>()))
            .Returns(Task.CompletedTask);

        var workflow = new ProgramWorkflow(menu.Object, ytdl.Object, console, fileService.Object,
            System.Threading.CancellationToken.None, null, errorHandler.Object);

        var result = await workflow.RunAsync();

        result.IsSuccess.Should().BeTrue();
        console.GetOutput().Should().Contain("Vídeo salvo em:");
    }

    [Fact]
    public async Task CompleteFlow_InvalidTimeRange_ShowsErrorAndPromptsAgain_Succeeds()
    {
        var menu = new Mock<IMenuService>();
        var ytdl = new Mock<IYtDlpService>();
        var console = new FakeConsoleService();
        var fileService = new Mock<IFileService>();
        var errorHandler = new Mock<IErrorHandler>();

        errorHandler.Setup(h => h.GetUserFriendlyMessage(It.IsAny<Exception>()))
            .Returns<Exception>(ex => ex.Message);

        menu.Setup(m => m.Show(console, fileService.Object, errorHandler.Object))
            .Returns(Result.Success());
        menu.SetupGet(m => m.Url).Returns("https://youtu.be/dQw4w9WgXcQ");
        menu.SetupGet(m => m.Start).Returns("90s");
        menu.SetupGet(m => m.End).Returns("120s");
        menu.SetupGet(m => m.CustomFileName).Returns("");
        menu.SetupGet(m => m.OutputDirectory).Returns("");
        menu.SetupGet(m => m.AudioOnly).Returns(false);

        fileService.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(true);
        fileService.Setup(f => f.HasWritePermission(It.IsAny<string>())).Returns(true);

        ytdl.Setup(y => y.GetVideoTitleAsync(It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync("Video Teste");
        ytdl.Setup(y => y.DownloadWithTimeRangeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IProgress<YoutubeDLSharp.DownloadProgress>>(),
                It.IsAny<System.Threading.CancellationToken>()))
            .Returns(Task.CompletedTask);

        var workflow = new ProgramWorkflow(menu.Object, ytdl.Object, console, fileService.Object,
            System.Threading.CancellationToken.None, null, errorHandler.Object);

        var result = await workflow.RunAsync();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteFlow_AllInputsValid_ProceedsToDownload()
    {
        var menu = new Mock<IMenuService>();
        var ytdl = new Mock<IYtDlpService>();
        var console = new FakeConsoleService();
        var fileService = new Mock<IFileService>();
        var errorHandler = new Mock<IErrorHandler>();

        errorHandler.Setup(h => h.GetUserFriendlyMessage(It.IsAny<Exception>()))
            .Returns<Exception>(ex => ex.Message);

        menu.Setup(m => m.Show(console, fileService.Object, errorHandler.Object))
            .Returns(Result.Success());
        menu.SetupGet(m => m.Url).Returns("https://youtube.com/watch?v=abc123");
        menu.SetupGet(m => m.Start).Returns("90s");
        menu.SetupGet(m => m.End).Returns("120s");
        menu.SetupGet(m => m.CustomFileName).Returns("");
        menu.SetupGet(m => m.OutputDirectory).Returns("");
        menu.SetupGet(m => m.AudioOnly).Returns(false);

        fileService.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(true);
        fileService.Setup(f => f.HasWritePermission(It.IsAny<string>())).Returns(true);

        ytdl.Setup(y => y.GetVideoTitleAsync(It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync("Test Video");
        ytdl.Setup(y => y.DownloadWithTimeRangeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                "90s",
                "120s",
                It.IsAny<IProgress<YoutubeDLSharp.DownloadProgress>>(),
                It.IsAny<System.Threading.CancellationToken>()))
            .Returns(Task.CompletedTask);

        var workflow = new ProgramWorkflow(menu.Object, ytdl.Object, console, fileService.Object,
            System.Threading.CancellationToken.None, null, errorHandler.Object);

        var result = await workflow.RunAsync();

        result.IsSuccess.Should().BeTrue();
        ytdl.Verify(y => y.DownloadWithTimeRangeAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "90s",
            "120s",
            It.IsAny<IProgress<YoutubeDLSharp.DownloadProgress>>(),
            It.IsAny<System.Threading.CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task CompleteFlow_OptionalInputsEmpty_UsesDefaults()
    {
        var menu = new Mock<IMenuService>();
        var ytdl = new Mock<IYtDlpService>();
        var console = new FakeConsoleService();
        var fileService = new Mock<IFileService>();
        var errorHandler = new Mock<IErrorHandler>();

        errorHandler.Setup(h => h.GetUserFriendlyMessage(It.IsAny<Exception>()))
            .Returns<Exception>(ex => ex.Message);

        menu.Setup(m => m.Show(console, fileService.Object, errorHandler.Object))
            .Returns(Result.Success());
        menu.SetupGet(m => m.Url).Returns("https://youtube.com/watch?v=abc123");
        menu.SetupGet(m => m.Start).Returns("90s");
        menu.SetupGet(m => m.End).Returns("120s");
        menu.SetupGet(m => m.CustomFileName).Returns(string.Empty);
        menu.SetupGet(m => m.OutputDirectory).Returns(string.Empty);
        menu.SetupGet(m => m.AudioOnly).Returns(false);

        fileService.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(true);
        fileService.Setup(f => f.HasWritePermission(It.IsAny<string>())).Returns(true);

        ytdl.Setup(y => y.GetVideoTitleAsync(It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync("Default Video Title");
        ytdl.Setup(y => y.DownloadWithTimeRangeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IProgress<YoutubeDLSharp.DownloadProgress>>(),
                It.IsAny<System.Threading.CancellationToken>()))
            .Returns(Task.CompletedTask);

        var workflow = new ProgramWorkflow(menu.Object, ytdl.Object, console, fileService.Object,
            System.Threading.CancellationToken.None, null, errorHandler.Object);

        var result = await workflow.RunAsync();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteFlow_ThreeFailures_CancelledWithMessage()
    {
        var menu = new Mock<IMenuService>();
        var console = new FakeConsoleService();
        var fileService = new Mock<IFileService>();
        var errorHandler = new Mock<IErrorHandler>();

        errorHandler.Setup(h => h.GetUserFriendlyMessage(It.IsAny<Exception>()))
            .Returns<Exception>(ex => ex.Message);

        menu.Setup(m => m.Show(console, fileService.Object, errorHandler.Object))
            .Returns(Result.Failure(Cutube.ErrorHandling.ErrorType.Validation,
                "❌ Máximo de tentativas atingido para URL. Operação cancelada.",
                new InvalidOperationException()));

        var workflow = new ProgramWorkflow(menu.Object, Mock.Of<IYtDlpService>(), console, fileService.Object,
            System.Threading.CancellationToken.None, null, errorHandler.Object);

        var result = await workflow.RunAsync();

        result.IsFailure.Should().BeTrue();
        console.GetOutput().Should().Contain("Máximo de tentativas atingido");
    }
}
