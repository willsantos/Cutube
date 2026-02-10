using FluentAssertions;
using Xunit;
using Cutube.Configuration;

namespace Cutube.Tests.Unit;

/// <summary>
/// Tests for CLI mode determination logic
/// These test the logic from Program.cs without requiring the full program
/// </summary>
public class CliModeTests
{
    [Fact]
    public void DetermineMode_FlagLocalTakesPrecedence()
    {
        // Arrange
        var config = new AppConfig { ApiUrl = "http://localhost:5000" };

        // Act & Assert
        // Flag --local forces local mode even with apiUrl in config
        DetermineMode(null, true, config).Should().BeFalse();

        // Flag --api-url overrides config but --local still wins
        DetermineMode("http://other:5000", true, config).Should().BeFalse();
    }

    [Fact]
    public void DetermineMode_ApiUrlFlagOverridesConfig()
    {
        // Arrange
        var config = new AppConfig { ApiUrl = "http://localhost:5000" };

        // Act & Assert
        // Flag --api-url overrides config file
        DetermineMode("http://other:5000", false, config).Should().BeTrue();
    }

    [Fact]
    public void DetermineMode_UsesConfigWhenNoFlags()
    {
        // Arrange
        var configWithApi = new AppConfig { ApiUrl = "http://localhost:5000" };
        var configWithoutApi = new AppConfig { ApiUrl = null };

        // Act & Assert
        // With apiUrl in config, should use API mode
        DetermineMode(null, false, configWithApi).Should().BeTrue();

        // Without apiUrl in config, should use local mode
        DetermineMode(null, false, configWithoutApi).Should().BeFalse();
    }

    [Fact]
    public void DetermineMode_ApiUrlFlagWithValidUrlEnablesApiMode()
    {
        // Arrange
        var config = new AppConfig { ApiUrl = null };

        // Act & Assert
        // Flag --api-url enables API mode even if config has no apiUrl
        DetermineMode("http://localhost:5000", false, config).Should().BeTrue();
    }

    [Fact]
    public void DetermineMode_EmptyApiUrlFlagIgnoresFlag()
    {
        // Arrange
        var configWithApi = new AppConfig { ApiUrl = "http://localhost:5000" };
        var configWithoutApi = new AppConfig { ApiUrl = null };

        // Act & Assert
        // Empty string apiUrlFlag is treated as not set, so config is used
        DetermineMode("", false, configWithApi).Should().BeTrue(); // Config has API
        DetermineMode("   ", false, configWithApi).Should().BeTrue(); // Whitespace only
        DetermineMode("", false, configWithoutApi).Should().BeFalse(); // Config has no API
    }

    /// <summary>
    /// Helper method that replicates the logic from Program.DetermineMode
    /// </summary>
    private static bool DetermineMode(string? apiUrlFlag, bool forceLocal, AppConfig config)
    {
        // Flag --local takes precedence
        if (forceLocal)
        {
            return false;
        }

        // Flag --api-url overrides config file
        if (!string.IsNullOrWhiteSpace(apiUrlFlag))
        {
            return true;
        }

        // Use config file
        return config.UseApi;
    }
}
