using FluentAssertions;
using Moq;
using Xunit;
using Cutube.Cli;
using Cutube.Cli.Updates;

namespace Cutube.Tests.Unit.Updates;

public class UpdateCheckerTests
{
    private readonly Mock<IHttpClientService> _http = new();
    private readonly Mock<IUpdateCheckCache> _cache = new();

    private UpdateChecker CreateChecker()
        => new(_http.Object, _cache.Object);

    private static string ReleaseJson(string tagName)
        => $$"""{ "tag_name": "{{tagName}}" }""";

    [Fact]
    public async Task CheckAsync_WhenLatestIsNewer_ReturnsHasUpdate()
    {
        _http.Setup(h => h.TryGetStringAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(ReleaseJson("v99.0.0"));

        var result = await CreateChecker().CheckAsync();

        result.HasUpdate.Should().BeTrue();
        result.LatestVersion.Should().Be("v99.0.0");
        result.CurrentVersion.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckAsync_WhenUpToDate_ReturnsNoUpdate()
    {
        var current = VersionInfo.GetCurrent();
        var currentCore = UpdateChecker.TryParseTag(current)!.Value;
        _http.Setup(h => h.TryGetStringAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(ReleaseJson($"v{currentCore.Major}.{currentCore.Minor}.{currentCore.Patch}"));

        var result = await CreateChecker().CheckAsync();

        result.HasUpdate.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsync_WhenRequestFails_ReturnsNoUpdateWithoutThrowing()
    {
        _http.Setup(h => h.TryGetStringAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync((string?)null);

        var result = await CreateChecker().CheckAsync();

        result.HasUpdate.Should().BeFalse();
        result.LatestVersion.Should().BeNull();
    }

    [Fact]
    public async Task CheckAsync_WhenTagIsNotSemver_ReturnsNoUpdate()
    {
        _http.Setup(h => h.TryGetStringAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(ReleaseJson("not-a-version"));

        var result = await CreateChecker().CheckAsync();

        result.HasUpdate.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsync_MarksCacheCheckedAfterQuery()
    {
        _http.Setup(h => h.TryGetStringAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync((string?)null);

        await CreateChecker().CheckAsync();

        _cache.Verify(c => c.MarkChecked(It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task CheckAsync_SendsUserAgentHeader()
    {
        _http.Setup(h => h.TryGetStringAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync((string?)null);

        await CreateChecker().CheckAsync();

        _http.Verify(h => h.DefaultRequestHeadersAdd("User-Agent", "Cutube"), Times.Once);
    }

    [Theory]
    [InlineData("v1.2.3", 1, 2, 3)]
    [InlineData("1.2.3", 1, 2, 3)]
    [InlineData("v10.20.30", 10, 20, 30)]
    [InlineData("1.2.3-rc.1", 1, 2, 3)]
    [InlineData("1.2.3+abc123", 1, 2, 3)]
    public void TryParseTag_WithValidTag_ParsesCore(string tag, int major, int minor, int patch)
    {
        UpdateChecker.TryParseTag(tag).Should().Be((major, minor, patch));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("latest")]
    [InlineData("1.2")]
    [InlineData("1.2.3.4")]
    [InlineData("1.2.x")]
    [InlineData("v-1.2.3")]
    public void TryParseTag_WithInvalidTag_ReturnsNull(string? tag)
    {
        UpdateChecker.TryParseTag(tag).Should().BeNull();
    }
}
