namespace Resultado.Test;

public class ResultTests
{
    [Theory]
    [InlineData(Kind.Ok)]
    [InlineData(Kind.Accepted)]
    [InlineData(Kind.NoContent)]
    [InlineData(Kind.Created)]
    public void Fail_WithSuccessStatus_Throws(Kind value)
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() => Result.Fail(string.Empty) with { Kind = value });

        Assert.Equal("Cannot set non-error status to a failure result. (Parameter 'value')", exception.Message);
    }

    [Theory]
    [InlineData(Kind.Error)]
    [InlineData(Kind.Critical)]
    [InlineData(Kind.FailedDependency)]
    public void Success_WithFailureStatus_Throws(Kind value)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Result.Succeed() with { Kind = value });

        Assert.Equal("Cannot set non-success status to a success result. (Parameter 'value')", exception.Message);
    }

    [Fact]
    public void Fail_WithValidationError_MapsDetailToErrors()
    {
        var result = Result.Fail(new ValidationError("Some error 1"), new ValidationError("Some error 2"));

        Assert.True(result.Errors.SequenceEqual(result.ValidationErrors.Select(x => x.Detail)));
    }

    [Fact]
    public void Result_BothFailureAndSuccess_AreDiscernible()
    {
        Result failure = Result.Fail("Some error");
        Result success = Result.Succeed("Some success");

        Assert.True(failure is Result.Failure);
        Assert.True(success is Result.Success);

        Assert.True(failure is not Result.Success);
        Assert.True(success is not Result.Failure);
    }

    [Fact]
    public void Success_WithValue_IsAccessible()
    {
        var example = new Example();
        Result success = Result.Succeed(example);
        Assert.IsType<Example>((success as Result.Success<Example>)!.Value);
    }

    [Fact]
    public void Success_IsDiscriminated()
    {
        Result result = Result.Succeed(new Example());
        switch (result)
        {
            case Result.Success<Example> success:
                Assert.Equal(2, success.Value.Num);
                break;
            case Result.Failure:
                Assert.Fail("Result should not have failed.");
                break;
            default:
                Assert.Fail("Result should have been success.");
                break;
        }
    }

    [Fact]
    public void Result_WithType_IsDiscriminated()
    {
        Result<Example> result = Result.Succeed(new Example());

        var isSuccess = result switch
        {
            Result.Success<Example> success => success.Value.Num == 2,
            _ => false
        };

        Assert.True(isSuccess);
    }

    [Fact]
    public void Result_SuccessWithVar_IsDeferredCorrectly()
    {
        var result = Result.Succeed(new Example());

        Assert.IsType<Result.Success<Example>>(result);
    }

    [Fact]
    public void Result_Failure_ConstructsWithStringCollection()
    {
        var failure = Result.Fail("Error 1", "Error 2");

        Assert.IsType<Result.Failure>(failure);
    }

    [Fact]
    public void Result_Failure_ConstructsWithValidationError()
    {
        var failure = Result.Fail(new ValidationError("Some error"));

        Assert.IsType<Result.Failure>(failure);
    }

    [Fact]
    public void Result_WithNull()
    {
        var result = Result.Succeed<int?>(null);
        
        Assert.False(result.Value.HasValue);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Result_WithValue()
    {
        Result<int> result = Result.Succeed(1);

        var content = result switch
        {
            Result.Success<int> success => success.Value,
            _ => 0
        };

        Assert.Equal(1, content);
        Assert.IsType<Result.Success<int>>(result, exactMatch: false);
    }

    [Fact]
    public void Result_FailureCanBeImplicitlyConverted()
    {
        Result<Example> example = new Result.Failure
        {
            Title = "Test"
        };
        
        Assert.IsAssignableFrom<Result.Failure<Example>>(example);
    }

    private record Example(int Num = 2);
}