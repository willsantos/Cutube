using FluentAssertions;
using Moq;
using Cutube.Cli;
using Cutube.Tests.Helpers;
using Cutube.Cli.ErrorHandling;
using Xunit;

namespace Cutube.Tests.E2E;

public class ProgramWorkflowTests
{
    private class ImmediateSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
            d(state);
        }
    }
    private class FakeYtDlpService : IYtDlpService
    {
        public bool ProgressCalled { get; private set; }

        public Task<string> GetVideoTitleAsync(string url, CancellationToken ct = default) => Task.FromResult("Video Teste");

        public Task DownloadWithTimeRangeAsync(
            string url,
            string outputFile,
            string startTime,
            string endTime,
            IProgress<YoutubeDLSharp.DownloadProgress>? progress = null,
            CancellationToken ct = default)
        {
            ProgressCalled = true;
            progress?.Report(new YoutubeDLSharp.DownloadProgress(
                YoutubeDLSharp.DownloadState.Downloading,
                1.0f,
                "",
                "",
                "",
                0,
                ""
            ));
            return Task.CompletedTask;
        }

        public Task DownloadAudioAsync(
            string url,
            string outputFile,
            string startTime,
            string endTime,
            IProgress<YoutubeDLSharp.DownloadProgress>? progress = null,
            CancellationToken ct = default)
        {
            ProgressCalled = true;
            progress?.Report(new YoutubeDLSharp.DownloadProgress(
                YoutubeDLSharp.DownloadState.Downloading,
                1.0f,
                "",
                "",
                "",
                0,
                ""
            ));
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
    [Fact]
    public async Task RunAsync_Success_CallsDownloadAndWritesFinalMessage()
    {
        var menu = new Mock<IMenuService>();
        var ytdl = new Mock<IYtDlpService>();
        var console = new FakeConsoleService();
        var fileService = new Mock<IFileService>();

        menu.Setup(m => m.Show(console, fileService.Object, It.IsAny<IErrorHandler>())).Returns(Result.Success());
        menu.SetupGet(m => m.Url).Returns("https://youtu.be/dQw4w9WgXcQ");
        menu.SetupGet(m => m.Start).Returns("00:00:10");
        menu.SetupGet(m => m.End).Returns("00:00:20");
        menu.SetupGet(m => m.CustomFileName).Returns("");
        menu.SetupGet(m => m.OutputDirectory).Returns("");

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

        var workflow = new ProgramWorkflow(menu.Object, ytdl.Object, console, fileService.Object);

        await workflow.RunAsync();

        ytdl.Verify(y => y.DownloadWithTimeRangeAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "00:00:10",
            "00:00:20",
            It.IsAny<IProgress<YoutubeDLSharp.DownloadProgress>>(),
            It.IsAny<System.Threading.CancellationToken>()
        ), Times.Once);

        console.GetOutput().Should().Contain("Vídeo salvo em:");
    }

    [Fact]
    public async Task RunAsync_DownloadThrows_WritesErrorAndRethrows()
    {
        var menu = new Mock<IMenuService>();
        var ytdl = new Mock<IYtDlpService>();
        var console = new FakeConsoleService();
        var fileService = new Mock<IFileService>();

        menu.Setup(m => m.Show(console, fileService.Object, It.IsAny<IErrorHandler>())).Returns(Result.Success());
        menu.SetupGet(m => m.Url).Returns("https://youtu.be/dQw4w9WgXcQ");
        menu.SetupGet(m => m.Start).Returns("00:00:10");
        menu.SetupGet(m => m.End).Returns("00:00:20");
        menu.SetupGet(m => m.CustomFileName).Returns("");
        menu.SetupGet(m => m.OutputDirectory).Returns("");

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
            .ThrowsAsync(new Exception("Falha no download"));

        var workflow = new ProgramWorkflow(menu.Object, ytdl.Object, console, fileService.Object);

        Func<Task> act = () => workflow.RunAsync();

        await act.Should().ThrowAsync<Exception>();
        console.GetOutput().Should().Contain("Erro: Falha no download");
    }

    [Fact]
    public async Task RunAsync_ReportsProgressAndState()
    {
        var menu = new Mock<IMenuService>();
        var console = new FakeConsoleService();
        var fileService = new Mock<IFileService>();

        menu.Setup(m => m.Show(console, fileService.Object, It.IsAny<IErrorHandler>())).Returns(Result.Success());
        menu.SetupGet(m => m.Url).Returns("https://youtu.be/dQw4w9WgXcQ");
        menu.SetupGet(m => m.Start).Returns("00:00:10");
        menu.SetupGet(m => m.End).Returns("00:00:20");
        menu.SetupGet(m => m.CustomFileName).Returns("");
        menu.SetupGet(m => m.OutputDirectory).Returns("");

        fileService.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(true);
        fileService.Setup(f => f.HasWritePermission(It.IsAny<string>())).Returns(true);

        var fakeYtdl = new FakeYtDlpService();
        var workflow = new ProgramWorkflow(menu.Object, fakeYtdl, console, fileService.Object);
        var originalContext = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(new ImmediateSynchronizationContext());
            await workflow.RunAsync();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }

        console.GetOutput().Should().Contain("Progresso: 100%");
        console.GetOutput().Should().Contain("Estado: Downloading");
        fakeYtdl.ProgressCalled.Should().BeTrue();
    }

    [Fact]
    public void DownloadProgress_Ctor_SetsProperties()
    {
        var progress = new YoutubeDLSharp.DownloadProgress(
            YoutubeDLSharp.DownloadState.Downloading,
            0.5f,
            "",
            "",
            "",
            0,
            ""
        );

        progress.Progress.Should().BeGreaterThan(0);
        progress.State.Should().Be(YoutubeDLSharp.DownloadState.Downloading);
    }
}
