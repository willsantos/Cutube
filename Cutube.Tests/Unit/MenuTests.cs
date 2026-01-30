using FluentAssertions;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Cutube.Tests.Unit;

[Collection("Sequential")]
public class MenuTests
{
    public MenuTests()
    {
        cutube.Menu.Reset();
    }

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

    [Fact]
    public void Show_WithCustomFileName_SetsCustomFileName()
    {
        var input = "https://youtube.com/watch?v=abc123\n00:00:10\n00:01:00\nmeu-podcast-01\n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        cutube.Menu.Show();

        cutube.Menu.CustomFileName.Should().Be("meu-podcast-01");
    }

    [Fact]
    public void Show_WithCustomFileNameWithExtension_RemovesExtension()
    {
        var input = "https://youtube.com/watch?v=abc123\n00:00:10\n00:01:00\nmeu-video.mp4\n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        cutube.Menu.Show();

        cutube.Menu.CustomFileName.Should().Be("meu-video");
    }

    [Fact]
    public void Show_WithoutCustomFileName_RemainsEmpty()
    {
        var input = "https://youtube.com/watch?v=abc123\n00:00:10\n00:01:00\n\n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        cutube.Menu.Show();

        cutube.Menu.CustomFileName.Should().BeEmpty();
    }

    [Fact]
    public void Show_WithWhitespaceCustomFileName_RemainsEmpty()
    {
        var input = "https://youtube.com/watch?v=abc123\n00:00:10\n00:01:00\n   \n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        cutube.Menu.Show();

        cutube.Menu.CustomFileName.Should().BeEmpty();
    }
}
