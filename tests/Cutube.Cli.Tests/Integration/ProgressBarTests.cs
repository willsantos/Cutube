using FluentAssertions;
using Cutube.Cli;
using Cutube.Cli.Theming;
using Cutube.Tests.Helpers;
using Xunit;

namespace Cutube.Tests.Integration;

[Collection("Sequential")]
public class ProgressBarTests : IDisposable
{
    private readonly string? _originalNoColor;
    private readonly string? _originalCi;
    private readonly string? _originalTerm;

    public ProgressBarTests()
    {
        // testes de cor precisam de ambiente de terminal determinístico
        _originalNoColor = Environment.GetEnvironmentVariable("NO_COLOR");
        _originalCi = Environment.GetEnvironmentVariable("CI");
        _originalTerm = Environment.GetEnvironmentVariable("TERM");
        Environment.SetEnvironmentVariable("NO_COLOR", null);
        Environment.SetEnvironmentVariable("CI", null);
        Environment.SetEnvironmentVariable("TERM", "xterm-256color");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("NO_COLOR", _originalNoColor);
        Environment.SetEnvironmentVariable("CI", _originalCi);
        Environment.SetEnvironmentVariable("TERM", _originalTerm);
    }

    [Fact]
    public void Constructor_OutputNotRedirected_StartsTimer()
    {
        var console = new FakeConsoleService { IsOutputRedirected = false };
        var timerFactory = new FakeTimerFactory();

        _ = new ProgressBar(console, timerFactory);

        timerFactory.LastTimer.Should().NotBeNull();
        timerFactory.LastTimer!.ChangeCalls.Should().Be(1);
    }

    [Fact]
    public void Constructor_OutputRedirected_DoesNotStartTimer()
    {
        var console = new FakeConsoleService { IsOutputRedirected = true };
        var timerFactory = new FakeTimerFactory();

        _ = new ProgressBar(console, timerFactory);

        timerFactory.LastTimer.Should().NotBeNull();
        timerFactory.LastTimer!.ChangeCalls.Should().Be(0);
    }

    [Fact]
    public void Report_ClampsAbove100()
    {
        var console = new FakeConsoleService { IsOutputRedirected = false };
        var timerFactory = new FakeTimerFactory();
        var bar = new ProgressBar(console, timerFactory);

        bar.Report(150);
        timerFactory.LastTimer!.Trigger();

        console.GetOutput().Should().Contain("100%");
    }

    [Fact]
    public void Report_ClampsBelowZero()
    {
        var console = new FakeConsoleService { IsOutputRedirected = false };
        var timerFactory = new FakeTimerFactory();
        var bar = new ProgressBar(console, timerFactory);

        bar.Report(-10);
        timerFactory.LastTimer!.Trigger();

        console.GetOutput().Should().Contain("  0%");
    }

    [Fact]
    public void TimerHandler_ColorSupported_WritesBarInPrimaryColor()
    {
        var console = new FakeConsoleService { IsOutputRedirected = false };
        var timerFactory = new FakeTimerFactory();
        var bar = new ProgressBar(console, timerFactory);

        bar.Report(40);
        timerFactory.LastTimer!.Trigger();

        console.GetOutput().Should().StartWith(ConsoleTheme.Primary);
        console.GetOutput().Should().Contain("40%");
        console.GetOutput().Should().EndWith(ConsoleTheme.Reset);
    }

    [Fact]
    public void TimerHandler_OutputRedirected_WritesBarWithoutEscapeCodes()
    {
        var console = new FakeConsoleService { IsOutputRedirected = true };
        var timerFactory = new FakeTimerFactory();
        var bar = new ProgressBar(console, timerFactory);

        bar.Report(40);
        timerFactory.LastTimer!.Trigger();

        console.GetOutput().Should().Contain("40%");
        console.GetOutput().Should().NotContain("\x1b");
    }

    [Fact]
    public void Animation_CyclesThroughCharacters()
    {
        var console = new FakeConsoleService { IsOutputRedirected = true };
        var timerFactory = new FakeTimerFactory();
        var bar = new ProgressBar(console, timerFactory);

        bar.Report(10);

        timerFactory.LastTimer!.Trigger();
        console.GetLastNonWhitespaceChar().Should().Be('|');

        timerFactory.LastTimer.Trigger();
        console.GetLastNonWhitespaceChar().Should().Be('/');

        timerFactory.LastTimer.Trigger();
        console.GetLastNonWhitespaceChar().Should().Be('-');

        timerFactory.LastTimer.Trigger();
        console.GetLastNonWhitespaceChar().Should().Be('\\');
    }

    [Fact]
    public void TimerHandler_WhenDisposed_DoesNotUpdate()
    {
        var console = new FakeConsoleService { IsOutputRedirected = false };
        var timerFactory = new FakeTimerFactory();
        var bar = new ProgressBar(console, timerFactory);

        bar.Dispose();
        timerFactory.LastTimer!.Trigger();

        console.GetOutput().Should().BeEmpty();
    }

    [Fact]
    public void Dispose_ClearsConsoleOutput()
    {
        var console = new FakeConsoleService { IsOutputRedirected = false };
        var timerFactory = new FakeTimerFactory();
        var bar = new ProgressBar(console, timerFactory);

        bar.Report(25);
        timerFactory.LastTimer!.Trigger();

        bar.Dispose();

        console.GetOutput().Should().Contain("[");
    }
}
