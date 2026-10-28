using JewerlySystem.Common.Domain;
using Xunit;

namespace JewerlySystem.Common.UnitTests;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResultWithError()
    {
        // Arrange
        var error = Error.Validation("Test.Validation", "Invalid input value.");

        // Act
        var result = Result.Failure(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Test.Validation", result.Error.Code);
        Assert.Equal("Invalid input value.", result.Error.Message);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void SuccessWithValue_ShouldContainValue()
    {
        // Act
        var result = Result.Success(42);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void FailureWithValue_ShouldThrowOnAccessingValue_AndReturnError()
    {
        // Arrange
        var error = Error.NotFound("Contract.NotFound", "Contract does not exist.");

        // Act
        var result = Result.Failure<string>(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
        Assert.Equal(error, result.Error);
    }
}
