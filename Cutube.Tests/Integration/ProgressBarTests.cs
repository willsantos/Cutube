using FluentAssertions;
using cutube;
using Cutube.Tests.Helpers;
using Xunit;

namespace Cutube.Tests.Integration;

public class ProgressBarTests
{
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
    public void Animation_CyclesThroughCharacters()
    {
        var console = new FakeConsoleService { IsOutputRedirected = false };
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
