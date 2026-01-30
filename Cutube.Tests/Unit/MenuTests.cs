using FluentAssertions;
using System.IO;
using Xunit;

namespace Cutube.Tests.Unit;

public class MenuTests
{
    [Fact]
    public void Show_ValidInput_SetsPropertiesCorrectly()
    {
        var input = "https://youtube.com/watch?v=abc123\n00:00:10\n00:01:00\n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        cutube.Menu.Show();

        cutube.Menu.Url.Should().Be("https://youtube.com/watch?v=abc123");
        cutube.Menu.Start.Should().Be("00:00:10");
        cutube.Menu.End.Should().Be("00:01:00");
    }

    [Fact]
    public void Show_DifferentUrl_SetsUrlCorrectly()
    {
        var input = "https://youtu.be/dQw4w9WgXcQ\n00:01:00\n00:02:00\n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        cutube.Menu.Show();

        cutube.Menu.Url.Should().Be("https://youtu.be/dQw4w9WgXcQ");
        cutube.Menu.Start.Should().Be("00:01:00");
        cutube.Menu.End.Should().Be("00:02:00");
    }
}
