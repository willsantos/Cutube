using FluentAssertions;
using Xunit;

namespace Cutube.Tests.Unit;

public class TimeHelperTests
{
    [Theory]
    [InlineData("00:00:00", 0)]
    [InlineData("00:01:00", 60)]
    [InlineData("00:05:30", 330)]
    [InlineData("01:00:00", 3600)]
    [InlineData("02:30:45", 9045)]
    [InlineData("1:30", 90)]
    [InlineData("90", 90)]
    [InlineData("90.5", 90)]
    [InlineData("90s", 90)]
    [InlineData("90m", 5400)]
    [InlineData("1h30m", 5400)]
    [InlineData("1h30m15s", 5415)]
    [InlineData("2h", 7200)]
    public void GetStartSeconds_ValidFormat_ReturnsCorrectSeconds(string time, int expected)
    {
        var result = cutube.TimeHelper.GetStartSeconds(time);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("h30m")]
    [InlineData("90x")]
    [InlineData("1h 30m")]
    public void GetStartSeconds_InvalidFormat_ThrowsFormatException(string time)
    {
        Action act = () => cutube.TimeHelper.GetStartSeconds(time);
        act.Should().Throw<FormatException>();
    }



    [Theory]
    [InlineData("00:00:00", 0)]
    [InlineData("00:10:00", 600)]
    [InlineData("01:30:45", 5445)]
    public void GetEndSeconds_ValidFormat_ReturnsCorrectSeconds(string time, int expected)
    {
        var result = cutube.TimeHelper.GetEndSeconds(time);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("00:00:00", "00:05:00", 300)]
    [InlineData("00:58:30", "01:03:45", 315)]
    [InlineData("00:10:00", "00:05:00", -300)]
    public void GetTimeDiff_ValidInputs_ReturnsCorrectDifference(string start, string end, int expected)
    {
        var result = cutube.TimeHelper.GetTimeDiff(start, end);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("90s", 90)]
    [InlineData("90m", 5400)]
    [InlineData("1h", 3600)]
    [InlineData("1h30m", 5400)]
    [InlineData("1h30m15s", 5415)]
    [InlineData("2h15m30s", 8130)]
    public void ParseToSeconds_CompactNotation_ReturnsCorrectSeconds(string input, int expected)
    {
        var result = cutube.TimeHelper.ParseToSeconds(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("90", 90)]
    [InlineData("90.5", 90)]
    [InlineData("3600", 3600)]
    public void ParseToSeconds_DecimalFormat_ReturnsCorrectSeconds(string input, int expected)
    {
        var result = cutube.TimeHelper.ParseToSeconds(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("1:30", 90)]
    [InlineData("5:45", 345)]
    public void ParseToSeconds_MinutesSecondsFormat_ReturnsCorrectSeconds(string input, int expected)
    {
        var result = cutube.TimeHelper.ParseToSeconds(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ParseToSeconds_EmptyInput_ThrowsArgumentException(string? input)
    {
        Action act = () => cutube.TimeHelper.ParseToSeconds(input!);
        act.Should().Throw<ArgumentException>();
    }
}
