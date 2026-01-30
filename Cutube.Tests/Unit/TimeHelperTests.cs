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
    public void GetStartSeconds_ValidFormat_ReturnsCorrectSeconds(string time, int expected)
    {
        var result = cutube.TimeHelper.GetStartSeconds(time);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("00:30")]
    [InlineData("invalid")]
    public void GetStartSeconds_InvalidFormat_ThrowsFormatException(string time)
    {
        Action act = () => cutube.TimeHelper.GetStartSeconds(time);
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void GetStartSeconds_OutOfRange_ThrowsOverflowException()
    {
        Action act = () => cutube.TimeHelper.GetStartSeconds("25:00:00");
        act.Should().Throw<OverflowException>();
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
}
