using System.Reflection;
using Cutube.Cli.Theming;
using Cutube.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Cutube.Tests.Unit.Theming;

/// <summary>
/// Manipula NO_COLOR: roda fora do paralelismo para não interferir em
/// outros testes que dependem do estado do tema
/// </summary>
[Collection("Sequential")]
public class ConsoleThemeTests : IDisposable
{
    private readonly string? _originalNoColor;

    public ConsoleThemeTests()
    {
        _originalNoColor = Environment.GetEnvironmentVariable("NO_COLOR");
        Environment.SetEnvironmentVariable("NO_COLOR", null);
    }

    public void Dispose()
        => Environment.SetEnvironmentVariable("NO_COLOR", _originalNoColor);

    [Fact]
    public void IsColorEnabled_OutputRedirected_ReturnsFalse()
    {
        var console = new FakeConsoleService { IsOutputRedirected = true };

        ConsoleTheme.IsColorEnabled(console).Should().BeFalse();
    }

    [Fact]
    public void IsColorEnabled_InteractiveTerminal_ReturnsTrue()
    {
        var console = new FakeConsoleService { IsOutputRedirected = false };

        ConsoleTheme.IsColorEnabled(console).Should().BeTrue();
    }

    [Fact]
    public void IsColorEnabled_NoColorSet_ReturnsFalse()
    {
        Environment.SetEnvironmentVariable("NO_COLOR", "1");
        var console = new FakeConsoleService { IsOutputRedirected = false };

        ConsoleTheme.IsColorEnabled(console).Should().BeFalse();
    }

    [Fact]
    public void Colorize_OutputRedirected_ReturnsPlainText()
    {
        var console = new FakeConsoleService { IsOutputRedirected = true };

        var result = ConsoleTheme.Colorize(console, ConsoleTheme.Primary, "✓ salvo em: /tmp/a.mp4");

        result.Should().Be("✓ salvo em: /tmp/a.mp4");
    }

    [Fact]
    public void Colorize_InteractiveTerminal_WrapsTextWithColorAndReset()
    {
        var console = new FakeConsoleService { IsOutputRedirected = false };

        var result = ConsoleTheme.Colorize(console, ConsoleTheme.Primary, "mensagem");

        result.Should().Be(ConsoleTheme.Primary + "mensagem" + ConsoleTheme.Reset);
    }

    [Fact]
    public void Colorize_NoColorSet_ReturnsPlainText()
    {
        Environment.SetEnvironmentVariable("NO_COLOR", "1");
        var console = new FakeConsoleService { IsOutputRedirected = false };

        var result = ConsoleTheme.Colorize(console, ConsoleTheme.Error, "❌ erro");

        result.Should().Be("❌ erro");
    }

    public static TheoryData<string, string> SemanticWriters => new()
    {
        { nameof(ConsoleThemeExtensions.WriteTitle), ConsoleTheme.Primary },
        { nameof(ConsoleThemeExtensions.WriteSuccess), ConsoleTheme.Success },
        { nameof(ConsoleThemeExtensions.WriteWarning), ConsoleTheme.Warning },
        { nameof(ConsoleThemeExtensions.WriteError), ConsoleTheme.Error },
        { nameof(ConsoleThemeExtensions.WriteProgress), ConsoleTheme.Primary },
    };

    [Theory]
    [MemberData(nameof(SemanticWriters))]
    public void SemanticWriters_InteractiveTerminal_ApplyPaletteColor(string method, string ansiColor)
    {
        var console = new FakeConsoleService { IsOutputRedirected = false };

        InvokeWriter(method, console, "mensagem");

        console.GetOutput()
            .Should().Be(ansiColor + "mensagem" + ConsoleTheme.Reset + Environment.NewLine);
    }

    [Theory]
    [MemberData(nameof(SemanticWriters))]
    public void SemanticWriters_OutputRedirected_WritePlainTextWithoutEscapeCodes(string method, string _)
    {
        var console = new FakeConsoleService { IsOutputRedirected = true };

        InvokeWriter(method, console, "mensagem");

        console.GetOutput()
            .Should().Be("mensagem" + Environment.NewLine)
            .And.NotContain("\x1b");
    }

    private static void InvokeWriter(string method, Cutube.Cli.IConsoleService console, string message)
        => typeof(ConsoleThemeExtensions)
            .GetMethod(method, BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, new object[] { console, message });
}
