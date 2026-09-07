using FluentAssertions;
using Xunit;
using Cutube.Cli.Configuration;

namespace Cutube.Tests.Unit;

public class ConfigServiceTests
{
    [Fact]
    public async Task LoadAsync_WhenConfigDoesNotExist_ReturnsDefaultConfig()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"cutube-test-{Guid.NewGuid()}");
        var configService = new ConfigService(tempPath);

        // Act
        var config = await configService.LoadAsync();

        // Assert
        config.DefaultOutputPath.Should().Be("~/Downloads");
        config.MaxConcurrentDownloads.Should().Be(3);
        config.TimeoutSeconds.Should().Be(300);
        config.VerboseLogging.Should().BeFalse();

        // Cleanup
        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, true);
    }

    [Fact]
    public async Task SaveAsync_And_LoadAsync_ShouldPersistConfig()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"cutube-test-{Guid.NewGuid()}");
        var configService = new ConfigService(tempPath);

        var originalConfig = new AppConfig
        {
            DefaultOutputPath = "~/Videos",
            MaxConcurrentDownloads = 5,
            TimeoutSeconds = 600,
            VerboseLogging = true
        };

        // Act
        await configService.SaveAsync(originalConfig);
        var loadedConfig = await configService.LoadAsync();

        // Assert
        loadedConfig.DefaultOutputPath.Should().Be(originalConfig.DefaultOutputPath);
        loadedConfig.MaxConcurrentDownloads.Should().Be(originalConfig.MaxConcurrentDownloads);
        loadedConfig.TimeoutSeconds.Should().Be(originalConfig.TimeoutSeconds);
        loadedConfig.VerboseLogging.Should().Be(originalConfig.VerboseLogging);

        // Cleanup
        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, true);
    }

    [Fact]
    public void ConfigExists_WhenConfigDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"cutube-test-{Guid.NewGuid()}");
        var configService = new ConfigService(tempPath);

        // Act & Assert
        configService.ConfigExists().Should().BeFalse();

        // Cleanup
        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, true);
    }

    [Fact]
    public async Task ConfigExists_WhenConfigExists_ReturnsTrue()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"cutube-test-{Guid.NewGuid()}");
        var configService = new ConfigService(tempPath);

        await configService.SaveAsync(new AppConfig());

        // Act & Assert
        configService.ConfigExists().Should().BeTrue();

        // Cleanup
        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, true);
    }
}
