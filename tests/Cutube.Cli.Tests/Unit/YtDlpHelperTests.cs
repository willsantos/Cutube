using FluentAssertions;
using Moq;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using Xunit;
using Cutube.Cli;
using System.Diagnostics;

namespace Cutube.Tests.Unit;

public class YtDlpHelperTests
{
    private readonly Mock<IFileService> _mockFile;
    private readonly Mock<IHttpClientService> _mockHttp;
    private readonly Mock<IEnvironmentService> _mockEnv;
    private readonly Mock<IProcessService> _mockProcess;
    private readonly Mock<IConsoleService> _mockConsole;

    private class TestableYtDlpHelper : YtDlpHelper
    {
        public int DownloadCalls { get; private set; }
        public string CurrentVersion { get; set; } = "unknown";
        public string LatestVersion { get; set; } = "unknown";

        public TestableYtDlpHelper(
            IFileService fileService,
            IHttpClientService httpClientService,
            IEnvironmentService environmentService,
            IProcessService processService,
            IConsoleService consoleService)
            : base(fileService, httpClientService, environmentService, processService, consoleService, true)
        {
        }

        public string CallGetPlatformIdentifier() => GetPlatformIdentifier();
        public bool CallIsRecentVersion(string path) => IsRecentVersion(path);
        public string? CallGetSystemPath() => GetSystemPath();

        protected internal override Task<string> GetCurrentVersion()
            => Task.FromResult(CurrentVersion);

        protected internal override Task<string> GetLatestVersion()
            => Task.FromResult(LatestVersion);

        protected internal override Task DownloadLatestVersion(string targetPath)
        {
            DownloadCalls++;
            return Task.CompletedTask;
        }
    }

    private class BaseCallYtDlpHelper : YtDlpHelper
    {
        public BaseCallYtDlpHelper(
            IFileService fileService,
            IHttpClientService? httpClientService,
            IEnvironmentService environmentService,
            IProcessService processService,
            IConsoleService consoleService)
            : base(fileService, httpClientService, environmentService, processService, consoleService, true)
        {
        }

        public Task CallDownloadLatestVersion(string path) => base.DownloadLatestVersion(path);

        public Task<string> CallGetCurrentVersion() => base.GetCurrentVersion();
        public Task<string> CallGetLatestVersion() => base.GetLatestVersion();
        public string CallGetBundledPath() => base.GetBundledPath();
        public string CallGetUserPath() => base.GetUserPath();
    }

    private class WorkflowYtDlpHelper : YtDlpHelper
    {
        public string? RawTitle { get; set; }
        public OptionSet? CapturedOptions { get; private set; }
        public string? CapturedFfmpegArguments { get; private set; }
        public string TempFilePath { get; set; } = "/tmp/temp.mp4";

        public WorkflowYtDlpHelper(
            IFileService fileService,
            IHttpClientService httpClientService,
            IEnvironmentService environmentService,
            IProcessService processService,
            IConsoleService consoleService)
            : base(fileService, httpClientService, environmentService, processService, consoleService, true)
        {
        }

        protected internal override Task<string?> FetchVideoTitleRawAsync(string url, CancellationToken ct)
            => Task.FromResult(RawTitle);

        protected internal override Task RunVideoDownloadAsync(string url, OptionSet options, IProgress<DownloadProgress>? progress, CancellationToken ct)
        {
            CapturedOptions = options;
            return Task.CompletedTask;
        }

        protected internal override string CreateTempFile(string extension = ".mp4") => TempFilePath;

        protected internal override void ExecuteFfmpeg(string arguments, IProgress<int> progress, CancellationToken ct)
        {
            CapturedFfmpegArguments = arguments;
        }
    }

    private class AudioWorkflowYtDlpHelper : YtDlpHelper
    {
        public OptionSet? CapturedAudioOptions { get; private set; }
        public string? CapturedAudioFfmpegArguments { get; private set; }

        public AudioWorkflowYtDlpHelper(
            IFileService fileService,
            IHttpClientService httpClientService,
            IEnvironmentService environmentService,
            IProcessService processService,
            IConsoleService consoleService)
            : base(fileService, httpClientService, environmentService, processService, consoleService, true)
        {
        }

        protected internal override Task RunVideoDownloadAsync(
            string url, OptionSet options, IProgress<DownloadProgress>? progress, CancellationToken ct)
        {
            CapturedAudioOptions = options;
            return Task.CompletedTask;
        }

        protected internal override void ExecuteFfmpeg(string arguments, IProgress<int> progress, CancellationToken ct)
        {
            CapturedAudioFfmpegArguments = arguments;
        }
    }

    private class ThrowingDownloadHelper : YtDlpHelper
    {
        public string CurrentVersion { get; set; } = "2026.01.01";
        public string LatestVersion { get; set; } = "2026.01.30";

        public ThrowingDownloadHelper(
            IFileService fileService,
            IHttpClientService httpClientService,
            IEnvironmentService environmentService,
            IProcessService processService,
            IConsoleService consoleService)
            : base(fileService, httpClientService, environmentService, processService, consoleService, true)
        {
        }

        protected internal override Task<string> GetCurrentVersion()
            => Task.FromResult(CurrentVersion);

        protected internal override Task<string> GetLatestVersion()
            => Task.FromResult(LatestVersion);

        protected internal override Task DownloadLatestVersion(string targetPath)
            => throw new Exception("download failed");
    }

    private class SafeOsYtDlpHelper : YtDlpHelper
    {
        public SafeOsYtDlpHelper(
            IFileService fileService,
            IHttpClientService httpClientService,
            IEnvironmentService environmentService,
            IProcessService processService,
            IConsoleService consoleService)
            : base(fileService, httpClientService, environmentService, processService, consoleService, true)
        {
        }

        protected internal override string GetBundledPath() => "/tmp/yt-dlp.exe";

        protected internal override string GetUserPath() => "/tmp/yt-dlp.exe";

        public string? CallGetSystemPath() => base.GetSystemPath();
    }

    private class DownloadSuccessHelper : YtDlpHelper
    {
        public bool DownloadCalled { get; private set; }

        public DownloadSuccessHelper(
            IFileService fileService,
            IHttpClientService httpClientService,
            IEnvironmentService environmentService,
            IProcessService processService,
            IConsoleService consoleService)
            : base(fileService, httpClientService, environmentService, processService, consoleService, true)
        {
        }

        protected internal override string GetUserPath() => "/tmp/user/yt-dlp.exe";
        protected internal override string GetBundledPath() => "/tmp/bundle/yt-dlp.exe";
        protected internal override string? GetSystemPath() => null;

        protected internal override Task DownloadLatestVersion(string targetPath)
        {
            DownloadCalled = true;
            return Task.CompletedTask;
        }
    }

    public YtDlpHelperTests()
    {
        _mockFile = new Mock<IFileService>();
        _mockHttp = new Mock<IHttpClientService>();
        _mockEnv = new Mock<IEnvironmentService>();
        _mockProcess = new Mock<IProcessService>();
        _mockConsole = new Mock<IConsoleService>();

        // Setup default behaviors
        _mockFile.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
        _mockFile.Setup(x => x.GetLastWriteTime(It.IsAny<string>())).Returns(DateTime.Now);
        _mockEnv.Setup(x => x.IsWindows()).Returns(true);
        _mockEnv.Setup(x => x.IsLinux()).Returns(false);
        _mockEnv.Setup(x => x.IsMacOS()).Returns(false);
        _mockEnv.Setup(x => x.OSArchitecture).Returns("X64");
        _mockEnv.Setup(x => x.GetFolderPath(It.IsAny<Environment.SpecialFolder>()))
               .Returns("/tmp/Cutube");
        _mockEnv.Setup(x => x.GetEnvironmentVariable("PATH")).Returns("/usr/bin:/usr/local/bin");
    }

    private YtDlpHelper CreateHelper(bool skipAutoUpdate = true)
    {
        return new YtDlpHelper(
            _mockFile.Object,
            _mockHttp.Object,
            _mockEnv.Object,
            _mockProcess.Object,
            _mockConsole.Object,
            skipAutoUpdate
        );
    }

    private TestableYtDlpHelper CreateTestableHelper()
    {
        return new TestableYtDlpHelper(
            _mockFile.Object,
            _mockHttp.Object,
            _mockEnv.Object,
            _mockProcess.Object,
            _mockConsole.Object
        );
    }

    private BaseCallYtDlpHelper CreateBaseHelper()
    {
        return new BaseCallYtDlpHelper(
            _mockFile.Object,
            _mockHttp.Object,
            _mockEnv.Object,
            _mockProcess.Object,
            _mockConsole.Object
        );
    }

    private WorkflowYtDlpHelper CreateWorkflowHelper()
    {
        return new WorkflowYtDlpHelper(
            _mockFile.Object,
            _mockHttp.Object,
            _mockEnv.Object,
            _mockProcess.Object,
            _mockConsole.Object
        );
    }

    private AudioWorkflowYtDlpHelper CreateAudioWorkflowHelper()
    {
        return new AudioWorkflowYtDlpHelper(
            _mockFile.Object,
            _mockHttp.Object,
            _mockEnv.Object,
            _mockProcess.Object,
            _mockConsole.Object
        );
    }

    private SafeOsYtDlpHelper CreateSafeHelper()
    {
        return new SafeOsYtDlpHelper(
            _mockFile.Object,
            _mockHttp.Object,
            _mockEnv.Object,
            _mockProcess.Object,
            _mockConsole.Object
        );
    }

    [Fact]
    public void Constructor_RecentUserVersion_ReturnsUserPath()
    {
        // Arrange
        _mockFile.Setup(x => x.Exists(It.Is<string>(s => s.Contains("Cutube") && s.Contains("yt-dlp.exe"))))
                .Returns(true);
        _mockFile.Setup(x => x.GetLastWriteTime(It.IsAny<string>()))
                .Returns(DateTime.Now);

        // Act
        var helper = CreateHelper();

        // Assert - helper foi criado sem exceção
        helper.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_OnlyBundledVersion_UsesBundledPath()
    {
        // Arrange
        _mockFile.Setup(x => x.Exists(It.Is<string>(s => s.Contains("Cutube"))))
                .Returns(false);
        _mockFile.Setup(x => x.Exists(It.Is<string>(s => s.Contains("bin") && !s.Contains("Cutube"))))
                .Returns(true);

        // Act
        var helper = CreateHelper();

        // Assert
        helper.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NoYtDlpFound_ThrowsException()
    {
        // Arrange
        _mockFile.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);

        // Act & Assert
        Action act = () => CreateHelper();

        act.Should().Throw<Exception>()
           .WithMessage("*Não foi possível encontrar ou baixar yt-dlp*");
    }



    [Fact]
    public void GetPlatformIdentifier_LinuxArm64_ReturnsCorrectSuffix()
    {
        // Arrange
        _mockEnv.Setup(x => x.IsWindows()).Returns(false);
        _mockEnv.Setup(x => x.IsLinux()).Returns(true);
        _mockEnv.Setup(x => x.IsMacOS()).Returns(false);
        _mockEnv.Setup(x => x.OSArchitecture).Returns("Arm64");

        _mockFile.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);

        // Act
        var helper = CreateHelper();

        // Assert
        helper.Should().NotBeNull();
    }

    [Theory]
    [InlineData("Windows", "X64", ".exe")]
    [InlineData("Windows", "X86", "_x86.exe")]
    [InlineData("Windows", "Arm64", "_arm64.exe")]
    [InlineData("Linux", "X64", "_linux")]
    [InlineData("Linux", "Arm64", "_linux_aarch64")]
    [InlineData("Linux", "Arm", "")] // ARM32 não tem binário único; zipimport oficial
    [InlineData("MacOS", "X64", "_macos")]
    [InlineData("MacOS", "Arm64", "_macos")] // asset único universal (Intel + Apple Silicon)
    public void GetPlatformIdentifier_ReturnsCorrectSuffix(string os, string arch, string expected)
    {
        _mockEnv.Setup(x => x.IsWindows()).Returns(os == "Windows");
        _mockEnv.Setup(x => x.IsLinux()).Returns(os == "Linux");
        _mockEnv.Setup(x => x.IsMacOS()).Returns(os == "MacOS");
        _mockEnv.Setup(x => x.OSArchitecture).Returns(arch);

        var helper = CreateTestableHelper();

        helper.CallGetPlatformIdentifier().Should().Be(expected);
    }

    [Fact]
    public void GetPlatformIdentifier_Windows_UnknownArch_ReturnsDefault()
    {
        _mockEnv.Setup(x => x.IsWindows()).Returns(true);
        _mockEnv.Setup(x => x.IsLinux()).Returns(false);
        _mockEnv.Setup(x => x.IsMacOS()).Returns(false);
        _mockEnv.Setup(x => x.OSArchitecture).Returns("Wasm");

        var helper = CreateTestableHelper();

        helper.CallGetPlatformIdentifier().Should().Be(".exe");
    }

    [Fact]
    public void GetPlatformIdentifier_Linux_UnknownArch_ReturnsDefault()
    {
        _mockEnv.Setup(x => x.IsWindows()).Returns(false);
        _mockEnv.Setup(x => x.IsLinux()).Returns(true);
        _mockEnv.Setup(x => x.IsMacOS()).Returns(false);
        _mockEnv.Setup(x => x.OSArchitecture).Returns("Wasm");

        var helper = CreateTestableHelper();

        helper.CallGetPlatformIdentifier().Should().Be("_linux");
    }

    [Fact]
    public void GetPlatformIdentifier_MacOS_UnknownArch_ReturnsDefault()
    {
        _mockEnv.Setup(x => x.IsWindows()).Returns(false);
        _mockEnv.Setup(x => x.IsLinux()).Returns(false);
        _mockEnv.Setup(x => x.IsMacOS()).Returns(true);
        _mockEnv.Setup(x => x.OSArchitecture).Returns("Wasm");

        var helper = CreateTestableHelper();

        helper.CallGetPlatformIdentifier().Should().Be("_macos");
    }

    [Fact]
    public void IsRecentVersion_RecentFile_ReturnsTrue()
    {
        _mockFile.Setup(x => x.GetLastWriteTime(It.IsAny<string>()))
            .Returns(DateTime.Now);

        var helper = CreateTestableHelper();

        helper.CallIsRecentVersion("/tmp/yt-dlp").Should().BeTrue();
    }

    [Fact]
    public void IsRecentVersion_OldFile_ReturnsFalse()
    {
        _mockFile.Setup(x => x.GetLastWriteTime(It.IsAny<string>()))
            .Returns(DateTime.Now.AddDays(-10));

        var helper = CreateTestableHelper();

        helper.CallIsRecentVersion("/tmp/yt-dlp").Should().BeFalse();
    }

    [Fact]
    public void GetSystemPath_WhenPathIsNull_ReturnsNull()
    {
        _mockEnv.Setup(x => x.GetEnvironmentVariable("PATH")).Returns((string?)null);

        var helper = CreateTestableHelper();

        helper.CallGetSystemPath().Should().BeNull();
    }

    [Fact]
    public async Task UpdateYtDlpIfNeeded_SameVersion_SkipsDownload()
    {
        var helper = CreateTestableHelper();
        helper.CurrentVersion = "2026.01.30";
        helper.LatestVersion = "2026.01.30";

        await helper.UpdateYtDlpIfNeeded();

        helper.DownloadCalls.Should().Be(0);
    }

    [Fact]
    public async Task UpdateYtDlpIfNeeded_DifferentVersion_Downloads()
    {
        var helper = CreateTestableHelper();
        helper.CurrentVersion = "2026.01.01";
        helper.LatestVersion = "2026.01.30";

        await helper.UpdateYtDlpIfNeeded();

        helper.DownloadCalls.Should().Be(1);
    }

    [Fact]
    public async Task DownloadLatestVersion_Windows_SkipsChmod()
    {
        _mockEnv.Setup(x => x.IsWindows()).Returns(true);
        _mockHttp.Setup(x => x.GetByteArrayAsync(It.IsAny<string>())).ReturnsAsync(new byte[] { 1 });
        _mockFile.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Returns(Task.CompletedTask);

        var helper = CreateBaseHelper();

        await helper.CallDownloadLatestVersion("/tmp/yt-dlp");

        _mockProcess.Verify(x => x.Start(It.IsAny<ProcessStartInfo>()), Times.Never);
    }

    [Fact]
    public async Task DownloadLatestVersion_Linux_CallsChmod()
    {
        _mockEnv.Setup(x => x.IsWindows()).Returns(false);
        _mockEnv.Setup(x => x.IsLinux()).Returns(true);
        _mockHttp.Setup(x => x.GetByteArrayAsync(It.IsAny<string>())).ReturnsAsync(new byte[] { 1 });
        _mockFile.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Returns(Task.CompletedTask);
        var processWrapper = new Mock<IProcessWrapper>();
        processWrapper.Setup(x => x.WaitForExitAsync()).ReturnsAsync(0);
        _mockProcess.Setup(x => x.Start(It.IsAny<ProcessStartInfo>()))
            .Returns(processWrapper.Object);

        var helper = CreateBaseHelper();

        await helper.CallDownloadLatestVersion("/tmp/yt-dlp");

        _mockProcess.Verify(x => x.Start(It.IsAny<ProcessStartInfo>()), Times.Once);
    }

    [Fact]
    public void GetBundledPath_Windows_UsesExe()
    {
        _mockEnv.Setup(x => x.IsWindows()).Returns(true);
        _mockEnv.Setup(x => x.IsLinux()).Returns(false);
        _mockEnv.Setup(x => x.IsMacOS()).Returns(false);

        var helper = CreateBaseHelper();

        helper.CallGetBundledPath().Should().EndWith("yt-dlp.exe");
    }

    [Fact]
    public void GetBundledPath_Linux_UsesNoExe()
    {
        _mockEnv.Setup(x => x.IsWindows()).Returns(false);
        _mockEnv.Setup(x => x.IsLinux()).Returns(true);
        _mockEnv.Setup(x => x.IsMacOS()).Returns(false);

        var helper = CreateBaseHelper();

        helper.CallGetBundledPath().Should().EndWith("yt-dlp");
    }

    [Fact]
    public void GetBundledPath_UnsupportedOs_Throws()
    {
        _mockEnv.Setup(x => x.IsWindows()).Returns(false);
        _mockEnv.Setup(x => x.IsLinux()).Returns(false);
        _mockEnv.Setup(x => x.IsMacOS()).Returns(false);

        Action act = () => CreateHelper();

        act.Should().Throw<PlatformNotSupportedException>();
    }

    [Fact]
    public void GetBundledPath_MacOS_UsesNoExe()
    {
        _mockEnv.Setup(x => x.IsWindows()).Returns(false);
        _mockEnv.Setup(x => x.IsLinux()).Returns(false);
        _mockEnv.Setup(x => x.IsMacOS()).Returns(true);

        var helper = CreateBaseHelper();

        helper.CallGetBundledPath().Should().EndWith("yt-dlp");
    }

    [Fact]
    public void GetUserPath_Windows_UsesExe()
    {
        _mockEnv.Setup(x => x.IsWindows()).Returns(true);

        var helper = CreateBaseHelper();

        helper.CallGetUserPath().Should().EndWith("yt-dlp.exe");
    }

    [Fact]
    public void GetUserPath_Linux_UsesNoExe()
    {
        _mockEnv.Setup(x => x.IsWindows()).Returns(false);
        _mockEnv.Setup(x => x.IsLinux()).Returns(true);

        var helper = CreateBaseHelper();

        helper.CallGetUserPath().Should().EndWith("yt-dlp");
    }

    [Fact]
    public void GetSystemPath_WhenFound_ReturnsPath()
    {
        _mockEnv.Setup(x => x.IsWindows()).Returns(true);
        _mockEnv.Setup(x => x.GetEnvironmentVariable("PATH")).Returns("/bin:/usr/bin");
        _mockFile.Setup(x => x.Exists("/usr/bin/yt-dlp.exe")).Returns(true);
        _mockFile.Setup(x => x.Exists("/bin/yt-dlp.exe")).Returns(false);

        var helper = CreateSafeHelper();

        helper.CallGetSystemPath().Should().Be("/usr/bin/yt-dlp.exe");
    }

    [Fact]
    public async Task GetCurrentVersion_WhenProcessThrows_ReturnsUnknown()
    {
        _mockProcess.Setup(x => x.Start(It.IsAny<ProcessStartInfo>()))
            .Throws(new Exception("boom"));

        var helper = CreateBaseHelper();

        var result = await helper.CallGetCurrentVersion();

        result.Should().Be("unknown");
    }

    [Fact]
    public async Task GetCurrentVersion_WhenProcessSucceeds_ReturnsTrimmed()
    {
        var processWrapper = new Mock<IProcessWrapper>();
        processWrapper.Setup(x => x.StandardOutputReadToEndAsync()).ReturnsAsync("2026.01.30\n");
        processWrapper.Setup(x => x.WaitForExitAsync()).ReturnsAsync(0);
        _mockProcess.Setup(x => x.Start(It.IsAny<ProcessStartInfo>()))
            .Returns(processWrapper.Object);

        var helper = CreateBaseHelper();

        var result = await helper.CallGetCurrentVersion();

        result.Should().Be("2026.01.30");
    }

    [Fact]
    public async Task GetLatestVersion_WithNullHttpClient_ReturnsUnknown()
    {
        var helper = new BaseCallYtDlpHelper(
            _mockFile.Object,
            null,
            _mockEnv.Object,
            _mockProcess.Object,
            _mockConsole.Object
        );

        var result = await helper.CallGetLatestVersion();

        result.Should().Be("unknown");
    }

    [Fact]
    public async Task GetLatestVersion_ParsesTagName()
    {
        _mockHttp.Setup(x => x.GetStringAsync(It.IsAny<string>()))
            .ReturnsAsync("{\"tag_name\": \"2026.01.30\"}");

        var helper = CreateBaseHelper();

        var result = await helper.CallGetLatestVersion();

        result.Should().Be("2026.01.30");
    }

    [Fact]
    public void ResolveYtDlpPath_UsesSystemPath_WhenAvailable()
    {
        _mockEnv.Setup(x => x.IsWindows()).Returns(true);
        _mockEnv.Setup(x => x.GetEnvironmentVariable("PATH")).Returns("/bin:/usr/bin");
        _mockFile.Setup(x => x.Exists(It.Is<string>(s => s.Contains("Cutube"))))
            .Returns(false);
        _mockFile.Setup(x => x.Exists("/usr/bin/yt-dlp.exe")).Returns(true);

        var helper = CreateSafeHelper();

        helper.Should().NotBeNull();
    }

    [Fact]
    public void ResolveYtDlpPath_DownloadSucceeds_ReturnsUserPath()
    {
        _mockFile.Setup(x => x.Exists("/tmp/bundle/yt-dlp.exe")).Returns(false);
        _mockFile.SetupSequence(x => x.Exists("/tmp/user/yt-dlp.exe"))
            .Returns(false)
            .Returns(true);

        var helper = new DownloadSuccessHelper(
            _mockFile.Object,
            _mockHttp.Object,
            _mockEnv.Object,
            _mockProcess.Object,
            _mockConsole.Object
        );

        helper.DownloadCalled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateYtDlpIfNeeded_WithNullHttpClient_DoesNotThrow()
    {
        var helper = new BaseCallYtDlpHelper(
            _mockFile.Object,
            null,
            _mockEnv.Object,
            _mockProcess.Object,
            _mockConsole.Object
        );

        await helper.UpdateYtDlpIfNeeded();
    }

    [Fact]
    public async Task UpdateYtDlpIfNeeded_WhenDownloadThrows_IsHandled()
    {
        var helper = new ThrowingDownloadHelper(
            _mockFile.Object,
            _mockHttp.Object,
            _mockEnv.Object,
            _mockProcess.Object,
            _mockConsole.Object
        );

        await helper.UpdateYtDlpIfNeeded();
    }

    [Fact]
    public async Task GetVideoTitleAsync_FormatsTitle()
    {
        var helper = CreateWorkflowHelper();
        helper.RawTitle = "Vídeo @2024";

        var result = await helper.GetVideoTitleAsync("https://youtu.be/test");

        result.Should().Be("Video 2024");
    }

    [Fact]
    public async Task DownloadAsync_SetsOptionsCorrectly()
    {
        var helper = CreateWorkflowHelper();

        await helper.DownloadAsync("https://youtu.be/test", "output.mp4");

        helper.CapturedOptions.Should().NotBeNull();
        helper.CapturedOptions!.Format.Should().Be("bestvideo+bestaudio");
        helper.CapturedOptions.MergeOutputFormat.Should().Be(DownloadMergeFormat.Mp4);
        helper.CapturedOptions.Output.Should().Be("output.mp4");
    }

    [Fact]
    public async Task DownloadWithTimeRangeAsync_ExecutesFfmpegAndDeletesTemp()
    {
        _mockFile.Setup(x => x.Delete("/tmp/temp.mp4"));

        var helper = CreateWorkflowHelper();
        helper.TempFilePath = "/tmp/temp.mp4";

        await helper.DownloadWithTimeRangeAsync(
            "https://youtu.be/test",
            "output.mp4",
            "00:00:10",
            "00:00:20"
        );

        helper.CapturedFfmpegArguments.Should().Contain("-i \"/tmp/temp.mp4\"");
        helper.CapturedFfmpegArguments.Should().Contain("-ss 10");
        helper.CapturedFfmpegArguments.Should().Contain("-t 10");
        helper.CapturedFfmpegArguments.Should().Contain("-c:v libx264 -c:a aac");
        helper.CapturedFfmpegArguments.Should().Contain("\"output.mp4\"");

        _mockFile.Verify(x => x.Delete("/tmp/temp.mp4"), Times.Once);
    }

    [Fact]
    public async Task UpdateYtDlpIfNeeded_VersionMismatch_DownloadsUpdate()
    {
        // Arrange
        _mockHttp.Setup(x => x.GetStringAsync(It.IsAny<string>()))
            .ReturnsAsync("{\"tag_name\": \"2026.01.30\"}");
        _mockHttp.Setup(x => x.GetByteArrayAsync(It.IsAny<string>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        var helper = CreateHelper();

        // Act
        await helper.UpdateYtDlpIfNeeded();

        // Assert
        _mockHttp.Verify(x => x.GetByteArrayAsync(It.IsAny<string>()), Times.Once);
        _mockFile.Verify(x => x.WriteAllBytesAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Once);
    }

    [Fact]
    public async Task DownloadAudioAsync_SetsCorrectOptions()
    {
        var helper = CreateAudioWorkflowHelper();

        await helper.DownloadAudioAsync(
            "https://youtu.be/test",
            "output.mp3",
            "00:00:10",
            "00:00:20"
        );

        helper.CapturedAudioOptions.Should().NotBeNull();
        helper.CapturedAudioOptions!.Format.Should().Be("bestaudio/best");
        helper.CapturedAudioOptions.ExtractAudio.Should().BeTrue();
        helper.CapturedAudioOptions.AudioFormat.Should().Be(AudioConversionFormat.Mp3);
        helper.CapturedAudioOptions.AudioQuality.Should().Be(2);
    }

    [Fact]
    public async Task DownloadAudioAsync_UsesCorrectFfmpegCodec()
    {
        _mockFile.Setup(x => x.Delete(It.IsAny<string>()));

        var helper = CreateAudioWorkflowHelper();

        await helper.DownloadAudioAsync(
            "https://youtu.be/test",
            "output.mp3",
            "00:00:10",
            "00:00:20"
        );

        helper.CapturedAudioFfmpegArguments.Should().Contain("-c:a libmp3lame");
        helper.CapturedAudioFfmpegArguments.Should().Contain("-q:a 2");
        helper.CapturedAudioFfmpegArguments.Should().Contain("-vn");
        helper.CapturedAudioFfmpegArguments.Should().Contain("-ss 10");
        helper.CapturedAudioFfmpegArguments.Should().Contain("-t 10");
        helper.CapturedAudioFfmpegArguments.Should().Contain("\"output.mp3\"");
    }

    [Fact]
    public async Task DownloadAudioAsync_DeletesTempFile()
    {
        _mockFile.Setup(x => x.Delete("/tmp/temp.mp4"));

        var helper = CreateAudioWorkflowHelper();

        await helper.DownloadAudioAsync(
            "https://youtu.be/test",
            "output.mp3",
            "00:00:10",
            "00:00:20"
        );

        _mockFile.Verify(x => x.Delete(It.IsAny<string>()), Times.Once);
    }
}
