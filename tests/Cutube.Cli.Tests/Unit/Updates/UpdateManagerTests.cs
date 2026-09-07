using FluentAssertions;
using Moq;
using Xunit;
using Cutube.Cli;
using Cutube.Cli.Updates;

namespace Cutube.Tests.Unit.Updates;

public class UpdateManagerTests
{
    private readonly Mock<IUpdateChecker> _checker = new();
    private readonly Mock<ISelfUpdater> _updater = new();
    private readonly Mock<IUpdateCheckCache> _cache = new();
    private readonly Mock<IConsoleService> _console = new();
    private readonly Func<bool> _interactive = () => false;

    public UpdateManagerTests()
    {
        // Defaults "caminho feliz": build publicada, cache vencido, sem update
        _cache.Setup(c => c.ShouldCheck(It.IsAny<DateTime>())).Returns(true);
        _updater.Setup(u => u.ManualInstallCommand).Returns("install-manually");
        _checker.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(false, "1.2.3", null));
    }

    private UpdateManager CreateManager(Func<bool>? interactive = null, Func<string?>? currentVersion = null)
        => new(
            _checker.Object,
            _updater.Object,
            _cache.Object,
            _console.Object,
            interactive ?? _interactive,
            currentVersion ?? (() => "1.2.3"));

    // ---------- ShouldSkip ----------

    [Theory]
    [InlineData("--version")]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("help")]
    [InlineData("update")]
    public void ShouldSkip_ForInstantOrSelfManagedCommands_ReturnsTrue(string command)
    {
        UpdateManager.ShouldSkip([command]).Should().BeTrue();
    }

    [Theory]
    [InlineData("download")]
    [InlineData("config")]
    public void ShouldSkip_ForRegularCommands_ReturnsFalse(string command)
    {
        UpdateManager.ShouldSkip([command]).Should().BeFalse();
    }

    [Fact]
    public void ShouldSkip_WithNoArgs_InteractiveMode_DoesNotSkip()
    {
        UpdateManager.ShouldSkip([]).Should().BeFalse();
    }

    // ---------- RunAsync: guardas ----------

    [Fact]
    public async Task RunAsync_WhenCheckForUpdatesDisabled_DoesNotCheck()
    {
        var result = await CreateManager().RunAsync(["download"], checkForUpdates: false);

        result.Should().BeNull();
        _checker.Verify(c => c.CheckAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenNotInteractive_SkipsEntireCheck()
    {
        var result = await CreateManager(interactive: () => true).RunAsync(["download"], checkForUpdates: true);

        result.Should().BeNull();
        _checker.Verify(c => c.CheckAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenCacheIsFresh_DoesNotCheck()
    {
        _cache.Setup(c => c.ShouldCheck(It.IsAny<DateTime>())).Returns(false);

        var result = await CreateManager().RunAsync(["download"], checkForUpdates: true);

        result.Should().BeNull();
        _checker.Verify(c => c.CheckAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenDevBuild_DoesNotCheck()
    {
        var result = await CreateManager(currentVersion: () => "unknown").RunAsync(["download"], checkForUpdates: true);

        result.Should().BeNull();
        _checker.Verify(c => c.CheckAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenNoUpdateAvailable_ContinuesSilently()
    {
        var result = await CreateManager().RunAsync(["download"], checkForUpdates: true);

        result.Should().BeNull();
        _console.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("Nova versão"))), Times.Never);
    }

    // ---------- RunAsync: prompt ----------

    [Fact]
    public async Task RunAsync_WhenUserConfirms_UpdatesAndRestarts()
    {
        _checker.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(true, "1.2.3", "v99.0.0"));
        _updater.Setup(u => u.UpdateAsync("v99.0.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SelfUpdateResult.Ok());
        _updater.Setup(u => u.RestartWithArgs(It.IsAny<IReadOnlyList<string>>()))
            .ReturnsAsync(42);
        SetupReadLine("s");

        var result = await CreateManager().RunAsync(["download", "url"], checkForUpdates: true);

        result.Should().Be(42);
        _updater.Verify(u => u.RestartWithArgs(
            It.Is<IReadOnlyList<string>>(a => a.Count == 2 && a[0] == "download" && a[1] == "url")), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenUserPressesEnter_ConfirmsUpdate()
    {
        _checker.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(true, "1.2.3", "v99.0.0"));
        _updater.Setup(u => u.UpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SelfUpdateResult.Ok());
        _updater.Setup(u => u.RestartWithArgs(It.IsAny<IReadOnlyList<string>>()))
            .ReturnsAsync(0);
        SetupReadLine("");

        var result = await CreateManager().RunAsync(["download"], checkForUpdates: true);

        result.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_WhenUserDeclines_ContinuesOnCurrentVersion()
    {
        _checker.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(true, "1.2.3", "v99.0.0"));
        SetupReadLine("n");

        var result = await CreateManager().RunAsync(["download"], checkForUpdates: true);

        result.Should().BeNull();
        _updater.Verify(u => u.UpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenStdinReturnsEof_ContinuesWithoutUpdating()
    {
        _checker.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(true, "1.2.3", "v99.0.0"));
        SetupReadLine((string?)null);

        var result = await CreateManager().RunAsync(["download"], checkForUpdates: true);

        result.Should().BeNull();
        _updater.Verify(u => u.UpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- RunAsync: falhas do subsistema ----------

    [Fact]
    public async Task RunAsync_WhenUpdateFails_ShowsManualCommandAndContinues()
    {
        _checker.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(true, "1.2.3", "v99.0.0"));
        _updater.Setup(u => u.UpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SelfUpdateResult.Fail("falha ao baixar.\n  Atualize manualmente: install-manually"));
        SetupReadLine("s");

        var result = await CreateManager().RunAsync(["download"], checkForUpdates: true);

        result.Should().BeNull();
        _console.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("install-manually"))), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenCheckerThrows_ContinuesSilently()
    {
        _checker.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("network down"));

        var result = await CreateManager().RunAsync(["download"], checkForUpdates: true);

        result.Should().BeNull();
    }

    // ---------- RunForcedAsync (cutube update) ----------

    [Fact]
    public async Task RunForcedAsync_IgnoresCacheAndChecks()
    {
        _cache.Setup(c => c.ShouldCheck(It.IsAny<DateTime>())).Returns(false);

        await CreateManager().RunForcedAsync(["update"]);

        _checker.Verify(c => c.CheckAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunForcedAsync_WhenUpToDate_InformsAndReturnsZero()
    {
        _checker.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(false, "1.2.3", "v1.2.3"));

        var exitCode = await CreateManager().RunForcedAsync(["update"]);

        exitCode.Should().Be(0);
        _console.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("versão mais recente"))), Times.Once);
    }

    [Fact]
    public async Task RunForcedAsync_WhenCheckFails_ReturnsOne()
    {
        _checker.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(false, "1.2.3", null));

        var exitCode = await CreateManager().RunForcedAsync(["update"]);

        exitCode.Should().Be(1);
    }

    [Fact]
    public async Task RunForcedAsync_WhenNotInteractive_UpdatesWithoutPrompting()
    {
        _checker.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(true, "1.2.3", "v99.0.0"));
        _updater.Setup(u => u.UpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SelfUpdateResult.Ok());
        _updater.Setup(u => u.RestartWithArgs(It.IsAny<IReadOnlyList<string>>()))
            .ReturnsAsync(5);

        var exitCode = await CreateManager(interactive: () => true).RunForcedAsync(["update", "download", "url"]);

        exitCode.Should().Be(5);
        _console.Verify(c => c.ReadLine(), Times.Never);
        _updater.Verify(u => u.RestartWithArgs(
            It.Is<IReadOnlyList<string>>(a => a.Count == 2 && a[0] == "download" && a[1] == "url")), Times.Once);
    }

    [Fact]
    public async Task RunForcedAsync_WhenInteractiveAndConfirmed_UpdatesAndRestarts()
    {
        _checker.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(true, "1.2.3", "v99.0.0"));
        _updater.Setup(u => u.UpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SelfUpdateResult.Ok());
        _updater.Setup(u => u.RestartWithArgs(It.IsAny<IReadOnlyList<string>>()))
            .ReturnsAsync(0);
        SetupReadLine("s");

        var exitCode = await CreateManager(interactive: () => true).RunForcedAsync(["update"]);

        exitCode.Should().Be(0);
        _updater.Verify(u => u.RestartWithArgs(It.IsAny<IReadOnlyList<string>>()), Times.Never);
    }

    [Fact]
    public async Task RunForcedAsync_WhenUpdateFails_ReturnsOne()
    {
        _checker.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(true, "1.2.3", "v99.0.0"));
        _updater.Setup(u => u.UpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SelfUpdateResult.Fail("boom"));

        // stdin redirecionado: pula o prompt e tenta atualizar direto
        var exitCode = await CreateManager(interactive: () => true).RunForcedAsync(["update"]);

        exitCode.Should().Be(1);
    }

    [Fact]
    public async Task RunForcedAsync_WhenDevBuild_InformsManualInstallAndReturnsOne()
    {
        var exitCode = await CreateManager(currentVersion: () => "unknown").RunForcedAsync(["update"]);

        exitCode.Should().Be(1);
        _console.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("instalação") || s.Contains("Instale"))), Times.Once);
        _updater.Verify(u => u.UpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunForcedAsync_CleansUpOldBinary()
    {
        await CreateManager().RunForcedAsync(["update"]);

        _updater.Verify(u => u.CleanupOldBinary(), Times.Once);
    }

    private void SetupReadLine(string? answer)
    {
        _console.Setup(c => c.ReadLine()).Returns(answer);
    }
}
