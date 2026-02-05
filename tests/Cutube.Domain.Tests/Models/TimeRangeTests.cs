using Cutube.Domain.Models;
using FluentAssertions;

namespace Cutube.Domain.Tests.Models;

public class TimeRangeTests
{
    [Fact]
    public void FromStrings_ValidTimeStrings_CreatesTimeRange()
    {
        var result = TimeRange.FromStrings("1:30", "5:00");

        result.StartSeconds.Should().Be(90);
        result.EndSeconds.Should().Be(300);
    }

    [Fact]
    public void FromStrings_SecondsFormat_CreatesTimeRange()
    {
        var result = TimeRange.FromStrings("90s", "300s");

        result.StartSeconds.Should().Be(90);
        result.EndSeconds.Should().Be(300);
    }

    [Fact]
    public void FromStrings_HoursFormat_CreatesTimeRange()
    {
        var result = TimeRange.FromStrings("1:00:00", "2:30:00");

        result.StartSeconds.Should().Be(3600);
        result.EndSeconds.Should().Be(9000);
    }

    [Fact]
    public void FromStrings_StartLessThanOrEqualToZero_ThrowsArgumentException()
    {
        var act = () => TimeRange.FromStrings("0", "60");

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Start must be greater than 0*");
    }

    [Fact]
    public void FromStrings_EndLessThanOrEqualToStart_ThrowsArgumentException()
    {
        var act = () => TimeRange.FromStrings("60", "60");

        act.Should().Throw<ArgumentException>()
           .WithMessage("*End must be greater than start*");
    }

    [Fact]
    public void FromStrings_EndBeforeStart_ThrowsArgumentException()
    {
        var act = () => TimeRange.FromStrings("120", "60");

        act.Should().Throw<ArgumentException>()
           .WithMessage("*End must be greater than start*");
    }

    [Fact]
    public void FromStrings_InvalidFormat_ThrowsFormatException()
    {
        var act = () => TimeRange.FromStrings("invalid", "60");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void DurationSeconds_CalculatesCorrectly()
    {
        var timeRange = new TimeRange
        {
            StartSeconds = 90,
            EndSeconds = 300
        };

        timeRange.DurationSeconds.Should().Be(210);
    }

    [Fact]
    public void DurationSeconds_HandlesLargeValues()
    {
        var timeRange = new TimeRange
        {
            StartSeconds = 3600,
            EndSeconds = 7200
        };

        timeRange.DurationSeconds.Should().Be(3600);
    }
}
