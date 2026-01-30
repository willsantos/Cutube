using FluentAssertions;
using Xunit;

namespace Cutube.Tests.Unit;

public class TitleHelperTests
{
    [Theory]
    [InlineData("Vídeo Simples", "Video Simples")]
    [InlineData("Título com Acentuação", "Titulo com Acentuacao")]
    [InlineData("Teste @2024! #Hash", "Teste 2024 Hash")]
    [InlineData("日本語 Test", " Test")]
    public void FormatTitle_RemovesAccentsAndSpecialChars(string input, string expected)
    {
        var result = cutube.TitleHelper.FormatTitle(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatTitle_EmptyString_ReturnsEmpty()
    {
        var result = cutube.TitleHelper.FormatTitle("");
        result.Should().Be("");
    }

    [Fact]
    public void FormatTitle_PreservesSpaces()
    {
        var result = cutube.TitleHelper.FormatTitle("  Vídeo  Com  Espaços  ");
        result.Should().Be("  Video  Com  Espacos  ");
    }

    [Fact]
    public void Constructor_DoesNotThrow()
    {
        var helper = new cutube.TitleHelper("Teste");
        helper.Should().NotBeNull();
    }
}
