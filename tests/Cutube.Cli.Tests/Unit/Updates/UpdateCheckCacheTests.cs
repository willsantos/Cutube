using FluentAssertions;
using Xunit;
using Cutube.Cli.Updates;

namespace Cutube.Tests.Unit.Updates;

public class UpdateCheckCacheTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _cachePath;
    private readonly UpdateCheckCache _cache;

    public UpdateCheckCacheTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"cutube-cache-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _cachePath = Path.Combine(_tempDir, "update-check.json");
        _cache = new UpdateCheckCache(_cachePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void ShouldCheck_WithoutCache_ReturnsTrue()
    {
        _cache.ShouldCheck(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void ShouldCheck_Within24Hours_ReturnsFalse()
    {
        var now = DateTime.UtcNow;
        _cache.MarkChecked(now.AddHours(-23));

        _cache.ShouldCheck(now).Should().BeFalse();
    }

    [Fact]
    public void ShouldCheck_After24Hours_ReturnsTrue()
    {
        var now = DateTime.UtcNow;
        _cache.MarkChecked(now.AddHours(-25));

        _cache.ShouldCheck(now).Should().BeTrue();
    }

    [Fact]
    public void ShouldCheck_WithFutureTimestamp_ReturnsTrue()
    {
        var now = DateTime.UtcNow;
        _cache.MarkChecked(now.AddHours(5)); // relógio atrasado / cache no futuro

        _cache.ShouldCheck(now).Should().BeTrue();
    }

    [Fact]
    public void ShouldCheck_WithCorruptedCache_ReturnsTrue()
    {
        File.WriteAllText(_cachePath, "{ this is not json !!!");

        _cache.ShouldCheck(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void MarkChecked_PersistsTimestamp()
    {
        var now = DateTime.UtcNow;

        _cache.MarkChecked(now);

        File.Exists(_cachePath).Should().BeTrue();
        // Nova instância lê o mesmo arquivo
        new UpdateCheckCache(_cachePath).ShouldCheck(now).Should().BeFalse();
    }

    [Fact]
    public void MarkChecked_WithInvalidPath_DoesNotThrow()
    {
        var invalidCache = new UpdateCheckCache(Path.Combine(_tempDir, "no-such-dir", "sub", "update-check.json"));

        var act = () => invalidCache.MarkChecked(DateTime.UtcNow);

        act.Should().NotThrow();
    }
}
