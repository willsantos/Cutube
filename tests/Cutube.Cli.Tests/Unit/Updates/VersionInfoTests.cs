using FluentAssertions;
using Xunit;
using Cutube.Cli.Updates;

namespace Cutube.Tests.Unit.Updates;

public class VersionInfoTests
{
    [Theory]
    [InlineData("1.2.3", true)]
    [InlineData("v1.2.3", true)]
    [InlineData("1.2.3+8f3a2b1", true)]
    [InlineData("0.0.0", true)]
    public void IsPublishedBuild_WithSemverCore_ReturnsTrue(string version, bool expected)
    {
        VersionInfo.IsPublishedBuild(version).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("unknown")]
    [InlineData("1.2")]
    [InlineData("1.2.3.4")]
    [InlineData("abc")]
    [InlineData("1.2.x")]
    public void IsPublishedBuild_WithoutValidSemver_ReturnsFalse(string? version)
    {
        VersionInfo.IsPublishedBuild(version).Should().BeFalse();
    }

    [Fact]
    public void GetCurrent_ReturnsAssemblyInformationalVersion()
    {
        var version = VersionInfo.GetCurrent();

        version.Should().NotBeNullOrWhiteSpace();
    }
}
