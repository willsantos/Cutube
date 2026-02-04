using FluentAssertions;
using Xunit;
using Cutube.ErrorHandling;

namespace Cutube.Tests.Unit.ErrorHandling;

public class ResultTests
{
    [Fact]
    public void Success_ReturnsSuccessResult()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Failure_ReturnsFailureResult()
    {
        var exception = new Exception("Test error");
        var result = Result.Failure(ErrorType.Network, "Network error occurred", exception);

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Network);
        result.ErrorMessage.Should().Be("Network error occurred");
        result.Exception.Should().Be(exception);
    }

    [Fact]
    public void ResultT_Success_ReturnsSuccessWithValue()
    {
        var value = "test value";
        var result = Result<string>.Success(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void ResultT_Failure_ReturnsFailureWithoutValue()
    {
        var result = Result<string>.Failure(ErrorType.Validation, "Invalid input");

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Be("Invalid input");
    }

    [Fact]
    public void ResultT_Failure_ThrowsWhenAccessingValue()
    {
        var result = Result<string>.Failure(ErrorType.Validation, "Invalid input");

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot access Value of failed result*");
    }

    [Fact]
    public void ResultT_SuccessInt_ReturnsIntValue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ResultT_FailureInt_ReturnsDefaultInt()
    {
        var result = Result<int>.Failure(ErrorType.Critical, "Error");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ResultT_SuccessObject_ReturnsObject()
    {
        var obj = new { Name = "Test", Value = 123 };
        var result = Result<object>.Success(obj);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(obj);
    }
}
