using Cutube.Domain.Models;
using FluentAssertions;

namespace Cutube.Domain.Tests.Models;

public class ValidationResultTests
{
    [Fact]
    public void Success_CreatesValidResult()
    {
        var result = ValidationResult.Success();

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Failure_CreatesInvalidResult_WithErrorMessage()
    {
        var errorMessage = "Test error message";
        var result = ValidationResult.Failure(errorMessage);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public void Failure_WithEmptyMessage_SetsErrorMessage()
    {
        var result = ValidationResult.Failure("");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("");
    }

    [Fact]
    public void Failure_WithNullMessage_SetsErrorMessage()
    {
        var result = ValidationResult.Failure(null!);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
    }
}
