# Resultado

## Como usar

Use:

```csharp
var validationFailure = Result.Fail(new ValidationError("Some error"));
var resultWithNullable = Result.Succeed<int?>(null);

// Typed result
Result<int> typedResult = Result.Succeed(1);
var content = typedResult switch
{
    Result.Success<int> success => success.Value, // OK
    _ => 0
};

var manyValidationErrors = Result.Fail("Error 1", "Error 2");

// With class
var resultWithExample = Result.Succeed(new Example()); // Type inferred is: `Success<Example>`

// Structured switch
Result successResult = Result.Succeed(new Example());
switch (successResult)
{
    case Result.Success<Example> success:
        Assert.Equal(2, success.Value?.Num);
        break;
    case Result.Failure:
        Assert.Fail("Result should not have failed.");
        break;
    default:
        Assert.Fail("Result should have been success.");
        break;
}

// Results are discernible:
// Result failure = Result.Fail("Some error");
// Result success = Result.Succeed("Some success");
// 
// Assert.True(failure is Result.Failure);
// Assert.True(success is Result.Success);
// 
// Assert.True(failure is not Result.Success);
// Assert.True(success is not Result.Failure);

// Validation Errors

var error1 = new ValidationError("Some error");
var error2 = new ValidationError
{
    Detail = "Points can not be null.",
    Pointer = "#/points",
    Severity = ValidationSeverity.Critical | ValidationSeverity.Error, // Can use bit flags to compose
    Code = "SOME_CUSTOM_INTERNAL_CODE"
};

var failureValidations = Result.Fail(error1, error2);

record Example(int Num = 2);
```