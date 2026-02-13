using FluentAssertions;
using Moq;
using Cutube.Cli;
using Cutube.Tests.Helpers;
using Xunit;

namespace Cutube.Tests.Integration;

public class FfmpegHelperTests
{
    private class TestableFfmpegHelper : FfmpegHelper
    {
        public TestableFfmpegHelper(
            IEnvironmentService environmentService,
            IFileService fileService,
            IProcessRunner processRunner,
            IConsoleService consoleService)
            : base(environmentService, fileService, processRunner, consoleService)
        {
        }

        public string CallGetFfmpegPath() => GetFfmpegPath();
        public string CallGetFfprobePath() => GetFfprobePath();
        public bool CallTryGetFromAppData(string executableName, out string path)
            => TryGetFromAppData(executableName, out path);
        public bool CallTryGetFromSystemPath(string executableName, out string path)
            => TryGetFromSystemPath(executableName, out path);
    }
    [Fact]
    public void Constructor_UsesAppDataPaths_WhenAvailable()
    {
        var env = new Mock<IEnvironmentService>();
        var file = new Mock<IFileService>();
        var console = new Mock<IConsoleService>();
        var runner = new FakeProcessRunner(Array.Empty<string?>());

        env.Setup(x => x.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
           .Returns("/tmp");
        env.Setup(x => x.GetEnvironmentVariable("PATH")).Returns("/usr/bin");
        env.Setup(x => x.GetEnvironmentVariable("TEMP")).Returns("/tmp");

        file.Setup(x => x.Exists("/tmp/ffmpeg")).Returns(true);
        file.Setup(x => x.Exists("/tmp/ffprobe")).Returns(true);

        var helper = new FfmpegHelper(env.Object, file.Object, runner, console.Object);

        helper.ExecuteFfmpeg("-version", new Progress<int>());

        runner.LastStartInfo.Should().NotBeNull();
        runner.LastStartInfo!.FileName.Should().Be("/tmp/ffmpeg");
    }

    [Fact]
    public void Constructor_UsesSystemPath_WhenAppDataMissing()
    {
        var env = new Mock<IEnvironmentService>();
        var file = new Mock<IFileService>();
        var console = new Mock<IConsoleService>();
        var runner = new FakeProcessRunner(Array.Empty<string?>());

        env.Setup(x => x.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
           .Returns("/tmp");
        env.Setup(x => x.GetEnvironmentVariable("PATH")).Returns("/usr/bin");
        env.Setup(x => x.GetEnvironmentVariable("TEMP")).Returns("/tmp");

        file.Setup(x => x.Exists("/tmp/ffmpeg")).Returns(false);
        file.Setup(x => x.Exists("/tmp/ffprobe")).Returns(false);
        file.Setup(x => x.Exists("/usr/bin/ffmpeg")).Returns(true);
        file.Setup(x => x.Exists("/usr/bin/ffprobe")).Returns(true);

        var helper = new FfmpegHelper(env.Object, file.Object, runner, console.Object);

        helper.ExecuteFfmpeg("-version", new Progress<int>());

        runner.LastStartInfo.Should().NotBeNull();
        runner.LastStartInfo!.FileName.Should().Be("/usr/bin/ffmpeg");
    }

    [Fact]
    public void Constructor_NoFfmpeg_ThrowsException()
    {
        var env = new Mock<IEnvironmentService>();
        var file = new Mock<IFileService>();
        var console = new Mock<IConsoleService>();
        var runner = new FakeProcessRunner(Array.Empty<string?>());

        env.Setup(x => x.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
           .Returns("/tmp");
        env.Setup(x => x.GetEnvironmentVariable("PATH")).Returns("/usr/bin");

        file.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);

        Action act = () => new FfmpegHelper(env.Object, file.Object, runner, console.Object);

        act.Should().Throw<Exception>()
           .WithMessage("*Não foi possível encontrar o FFmpeg.*");
    }

    [Fact]
    public void ExecuteFfmpeg_ReportsProgress_WhenDurationAndTimePresent()
    {
        var env = new Mock<IEnvironmentService>();
        var file = new Mock<IFileService>();
        var console = new Mock<IConsoleService>();
        var runner = new FakeProcessRunner(new[]
        {
            "Duration: 00:01:30.00",
            "frame=  123 fps= 30 q=28.0 size=    1234kB time=00:00:45.00 bitrate=1234.5kbits/s"
        });

        env.Setup(x => x.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
           .Returns("/tmp");
        env.Setup(x => x.GetEnvironmentVariable("PATH")).Returns("/usr/bin");
        env.Setup(x => x.GetEnvironmentVariable("TEMP")).Returns("/tmp");

        file.Setup(x => x.Exists("/tmp/ffmpeg")).Returns(true);
        file.Setup(x => x.Exists("/tmp/ffprobe")).Returns(true);

        var progress = new Mock<IProgress<int>>();
        var helper = new FfmpegHelper(env.Object, file.Object, runner, console.Object);

        helper.ExecuteFfmpeg("-version", progress.Object);

        progress.Verify(p => p.Report(It.Is<int>(v => v == 50)), Times.Once);
    }

    [Fact]
    public void ExecuteFfmpeg_NoDuration_DoesNotReportProgress()
    {
        var env = new Mock<IEnvironmentService>();
        var file = new Mock<IFileService>();
        var console = new Mock<IConsoleService>();
        var runner = new FakeProcessRunner(new[]
        {
            "frame=  123 fps= 30 q=28.0 size=    1234kB time=00:00:45.00 bitrate=1234.5kbits/s"
        });

        env.Setup(x => x.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
           .Returns("/tmp");
        env.Setup(x => x.GetEnvironmentVariable("PATH")).Returns("/usr/bin");
        env.Setup(x => x.GetEnvironmentVariable("TEMP")).Returns("/tmp");

        file.Setup(x => x.Exists("/tmp/ffmpeg")).Returns(true);
        file.Setup(x => x.Exists("/tmp/ffprobe")).Returns(true);

        var progress = new Mock<IProgress<int>>();
        var helper = new FfmpegHelper(env.Object, file.Object, runner, console.Object);

        helper.ExecuteFfmpeg("-version", progress.Object);

        progress.Verify(p => p.Report(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void TryGetFromAppData_FileExists_ReturnsTrue()
    {
        var env = new Mock<IEnvironmentService>();
        var file = new Mock<IFileService>();
        var console = new Mock<IConsoleService>();
        var runner = new FakeProcessRunner(Array.Empty<string?>());

        env.Setup(x => x.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
            .Returns("/tmp");
        env.Setup(x => x.GetEnvironmentVariable(It.IsAny<string>())).Returns(string.Empty);

        file.Setup(x => x.Exists("/tmp/ffmpeg")).Returns(true);
        file.Setup(x => x.Exists("/tmp/ffprobe")).Returns(true);

        var helper = new TestableFfmpegHelper(env.Object, file.Object, runner, console.Object);

        helper.CallTryGetFromAppData("ffmpeg", out var path).Should().BeTrue();
        path.Should().Be("/tmp/ffmpeg");
    }

    [Fact]
    public void TryGetFromSystemPath_NoPathEnv_ThrowsException()
    {
        var env = new Mock<IEnvironmentService>();
        var file = new Mock<IFileService>();
        var console = new Mock<IConsoleService>();
        var runner = new FakeProcessRunner(Array.Empty<string?>());

        env.Setup(x => x.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
            .Returns("/tmp");
        env.Setup(x => x.GetEnvironmentVariable("PATH")).Returns((string?)null);

        file.Setup(x => x.Exists("/tmp/ffmpeg.exe")).Returns(true);
        file.Setup(x => x.Exists("/tmp/ffprobe.exe")).Returns(true);

        var helper = new TestableFfmpegHelper(env.Object, file.Object, runner, console.Object);

        Action act = () => helper.CallTryGetFromSystemPath("ffmpeg", out _);
        act.Should().Throw<Exception>()
            .WithMessage("*Não foi possível encontrar o PATH do sistema.*");
    }

    [Fact]
    public void TryGetFromSystemPath_FindsInSecondFolder_ReturnsTrue()
    {
        var env = new Mock<IEnvironmentService>();
        var file = new Mock<IFileService>();
        var console = new Mock<IConsoleService>();
        var runner = new FakeProcessRunner(Array.Empty<string?>());

        env.Setup(x => x.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
            .Returns("/tmp");
        env.Setup(x => x.GetEnvironmentVariable("PATH")).Returns("/bin:/usr/bin");

        file.Setup(x => x.Exists("/bin/ffmpeg")).Returns(false);
        file.Setup(x => x.Exists("/usr/bin/ffmpeg")).Returns(true);
        file.Setup(x => x.Exists("/usr/bin/ffprobe")).Returns(true);

        var helper = new TestableFfmpegHelper(env.Object, file.Object, runner, console.Object);

        helper.CallTryGetFromSystemPath("ffmpeg", out var path).Should().BeTrue();
        path.Should().Be("/usr/bin/ffmpeg");
    }

    [Fact]
    public void ExecuteFfmpeg_InvalidDuration_IgnoresProgress()
    {
        var env = new Mock<IEnvironmentService>();
        var file = new Mock<IFileService>();
        var console = new Mock<IConsoleService>();
        var runner = new FakeProcessRunner(new[]
        {
            "Duration: INVALID",
            "time=00:00:45.00"
        });

        env.Setup(x => x.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
            .Returns("/tmp");
        env.Setup(x => x.GetEnvironmentVariable("PATH")).Returns("/usr/bin");
        env.Setup(x => x.GetEnvironmentVariable("TEMP")).Returns("/tmp");

        file.Setup(x => x.Exists("/tmp/ffmpeg")).Returns(true);
        file.Setup(x => x.Exists("/tmp/ffprobe")).Returns(true);

        var progress = new Mock<IProgress<int>>();
        var helper = new FfmpegHelper(env.Object, file.Object, runner, console.Object);

        helper.ExecuteFfmpeg("-version", progress.Object);

        progress.Verify(p => p.Report(It.IsAny<int>()), Times.Never);
    }
}
