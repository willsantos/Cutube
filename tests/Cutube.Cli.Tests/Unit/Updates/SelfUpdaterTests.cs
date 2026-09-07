using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using FluentAssertions;
using Moq;
using Xunit;
using Cutube.Cli;
using Cutube.Cli.Updates;

namespace Cutube.Tests.Unit.Updates;

public class SelfUpdaterTests : IDisposable
{
    private readonly string _workDir;
    private readonly Mock<IHttpClientService> _http = new();
    private readonly Mock<IEnvironmentService> _env = new();
    private readonly ProcessService _realProcesses = new();

    private string _processPath;

    public SelfUpdaterTests()
    {
        _workDir = Path.Combine(Path.GetTempPath(), $"cutube-updater-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workDir);
        _processPath = Path.Combine(_workDir, "cutube");

        // Default: plataforma Unix-like (o host de teste tem tar disponível)
        _env.Setup(e => e.IsWindows()).Returns(false);
        _env.Setup(e => e.IsLinux()).Returns(true);
        _env.Setup(e => e.IsMacOS()).Returns(false);
        _env.Setup(e => e.OSArchitecture).Returns("X64");
    }

    public void Dispose()
    {
        if (Directory.Exists(_workDir))
            Directory.Delete(_workDir, true);
    }

    private SelfUpdater CreateUpdater()
        => new(_http.Object, _realProcesses, _env.Object, _processPath);

    // ---------- GetAssetName ----------

    [Theory]
    [InlineData("X64", false, true, false, "cutube-linux-amd64.tar.gz")]
    [InlineData("Arm64", false, true, false, "cutube-linux-arm64.tar.gz")]
    [InlineData("X64", false, false, true, "cutube-macos-amd64.tar.gz")]
    [InlineData("Arm64", false, false, true, "cutube-macos-arm64.tar.gz")]
    [InlineData("X64", true, false, false, "cutube-windows-amd64.exe.zip")]
    public void GetAssetName_ForReleaseMatrixCombinations_ReturnsOfficialAssetName(
        string arch, bool isWindows, bool isLinux, bool isMacOS, string expected)
    {
        _env.Setup(e => e.OSArchitecture).Returns(arch);
        _env.Setup(e => e.IsWindows()).Returns(isWindows);
        _env.Setup(e => e.IsLinux()).Returns(isLinux);
        _env.Setup(e => e.IsMacOS()).Returns(isMacOS);

        CreateUpdater().GetAssetName().Should().Be(expected);
    }

    [Theory]
    [InlineData("X64", false, false, false)] // SO desconhecido
    [InlineData("X86", true, false, false)]  // Windows x86 não tem asset
    [InlineData("Arm", false, true, false)]  // Linux Armv7 não tem asset
    [InlineData("Arm64", true, false, false)] // Windows ARM não tem asset
    public void GetAssetName_ForUnsupportedPlatforms_ReturnsNull(
        string arch, bool isWindows, bool isLinux, bool isMacOS)
    {
        _env.Setup(e => e.OSArchitecture).Returns(arch);
        _env.Setup(e => e.IsWindows()).Returns(isWindows);
        _env.Setup(e => e.IsLinux()).Returns(isLinux);
        _env.Setup(e => e.IsMacOS()).Returns(isMacOS);

        CreateUpdater().GetAssetName().Should().BeNull();
    }

    // ---------- ManualInstallCommand ----------

    [Fact]
    public void ManualInstallCommand_OnUnix_PointsToInstallScript()
    {
        CreateUpdater().ManualInstallCommand.Should().Contain("install.sh");
    }

    [Fact]
    public void ManualInstallCommand_OnWindows_PointsToInstallScript()
    {
        _env.Setup(e => e.IsWindows()).Returns(true);

        CreateUpdater().ManualInstallCommand.Should().Contain("install.ps1");
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_OnUnix_ReplacesBinaryWithDownloadedAsset()
    {
        File.WriteAllText(_processPath, "old-binary");
        _http.Setup(h => h.TryGetByteArrayAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(CreateTarGzAsset("cutube", "new-binary"u8.ToArray()));

        var result = await CreateUpdater().UpdateAsync("v99.0.0");

        result.Success.Should().BeTrue();
        File.ReadAllText(_processPath).Should().Be("new-binary");
        File.Exists(_processPath + ".new").Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_OnWindows_MovesCurrentBinaryToOld()
    {
        _env.Setup(e => e.IsWindows()).Returns(true);
        _processPath = Path.Combine(_workDir, "cutube.exe");
        File.WriteAllText(_processPath, "old-binary");

        _http.Setup(h => h.TryGetByteArrayAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(CreateZipAsset("cutube.exe", "new-binary"));

        var result = await CreateUpdater().UpdateAsync("v99.0.0");

        result.Success.Should().BeTrue();
        File.ReadAllText(_processPath).Should().Be("new-binary");
        File.Exists(_processPath + ".old").Should().BeTrue();
        File.ReadAllText(_processPath + ".old").Should().Be("old-binary");
    }

    [Fact]
    public async Task UpdateAsync_WhenDownloadFails_ReturnsFailureWithManualCommandAndKeepsBinary()
    {
        File.WriteAllText(_processPath, "old-binary");
        _http.Setup(h => h.TryGetByteArrayAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync((byte[]?)null);

        var result = await CreateUpdater().UpdateAsync("v99.0.0");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain(ManualInstallCommandFragment());
        File.ReadAllText(_processPath).Should().Be("old-binary");
    }

    [Fact]
    public async Task UpdateAsync_WhenRunningViaDotnetRun_ReturnsFailureWithoutTouchingHost()
    {
        _processPath = "/usr/lib/dotnet/dotnet";

        var result = await CreateUpdater().UpdateAsync("v99.0.0");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("dotnet run");
    }

    [Fact]
    public async Task UpdateAsync_WithoutProcessPath_ReturnsFailure()
    {
        _processPath = null!;

        var result = await CreateUpdater().UpdateAsync("v99.0.0");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WhenPlatformHasNoAsset_ReturnsFailure()
    {
        _processPath = Path.Combine(_workDir, "cutube");
        File.WriteAllText(_processPath, "old-binary");
        _env.Setup(e => e.OSArchitecture).Returns("X86");

        var result = await CreateUpdater().UpdateAsync("v99.0.0");

        result.Success.Should().BeFalse();
        File.ReadAllText(_processPath).Should().Be("old-binary");
    }

    // ---------- RestartWithArgs ----------

    [Fact]
    public async Task RestartWithArgs_LaunchesBinaryAndPropagatesExitCode()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            return; // script shebang é específico de Unix

        _processPath = Path.Combine(_workDir, "fake-cutube");
        File.WriteAllText(_processPath, "#!/bin/sh\nexit 7\n");
        MakeExecutable(_processPath);

        var exitCode = await CreateUpdater().RestartWithArgs(["download", "https://example.com", "--audio"]);

        exitCode.Should().Be(7);
    }

    [Fact]
    public async Task RestartWithArgs_WithoutProcessPath_ReturnsOne()
    {
        _processPath = null!;

        var exitCode = await CreateUpdater().RestartWithArgs(["download"]);

        exitCode.Should().Be(1);
    }

    // ---------- CleanupOldBinary ----------

    [Fact]
    public void CleanupOldBinary_RemovesResidualOldBinary()
    {
        File.WriteAllText(_processPath + ".old", "residual");

        CreateUpdater().CleanupOldBinary();

        File.Exists(_processPath + ".old").Should().BeFalse();
    }

    [Fact]
    public void CleanupOldBinary_WithoutResidualFile_DoesNotThrow()
    {
        var act = () => CreateUpdater().CleanupOldBinary();

        act.Should().NotThrow();
    }

    // ---------- Helpers ----------

    private string ManualInstallCommandFragment()
        => _env.Object.IsWindows() ? "install.ps1" : "install.sh";

    private static void MakeExecutable(string path)
    {
        using var process = System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"+x \"{path}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        process?.WaitForExit();
    }

    /// <summary>
    /// Cria um .tar.gz real com uma única entrada (mesmo formato do release.yml em Unix)
    /// </summary>
    private static byte[] CreateTarGzAsset(string entryName, byte[] content)
    {
        var workDir = Path.Combine(Path.GetTempPath(), $"cutube-asset-{Guid.NewGuid():N}");
        var outDir = Path.Combine(workDir, "out");
        Directory.CreateDirectory(outDir);
        try
        {
            var payloadDir = Path.Combine(workDir, "payload");
            Directory.CreateDirectory(payloadDir);
            File.WriteAllBytes(Path.Combine(payloadDir, entryName), content);

            var tarPath = Path.Combine(outDir, entryName + ".tar");
            TarFile.CreateFromDirectory(payloadDir, tarPath, includeBaseDirectory: false);
            var tarBytes = File.ReadAllBytes(tarPath);

            using var compressed = new MemoryStream();
            using (var gzip = new GZipStream(compressed, CompressionMode.Compress))
            {
                gzip.Write(tarBytes);
            }

            return compressed.ToArray();
        }
        finally
        {
            Directory.Delete(workDir, true);
        }
    }

    /// <summary>
    /// Cria um .zip real com uma única entrada (mesmo formato do release.yml em Windows)
    /// </summary>
    private static byte[] CreateZipAsset(string entryName, string content)
    {
        var workDir = Path.Combine(Path.GetTempPath(), $"cutube-asset-{Guid.NewGuid():N}");
        var outDir = Path.Combine(workDir, "out");
        Directory.CreateDirectory(outDir);
        try
        {
            var payloadDir = Path.Combine(workDir, "payload");
            Directory.CreateDirectory(payloadDir);
            File.WriteAllText(Path.Combine(payloadDir, entryName), content, Encoding.UTF8);

            var zipPath = Path.Combine(outDir, "asset.zip");
            ZipFile.CreateFromDirectory(payloadDir, zipPath);
            return File.ReadAllBytes(zipPath);
        }
        finally
        {
            Directory.Delete(workDir, true);
        }
    }
}
