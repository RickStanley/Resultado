using Microsoft.AspNetCore.Mvc;
using Resultado.Web;

namespace Resultado.Test;

public class ResultadoWebTests
{
    [Fact]
    public void Result_WhenSuccess_Throws()
    {
        var result = Result.Succeed();

        var act = () => result.AsProblemDetails();

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Result_WhenFailure_ConvertsToProblemDetails()
    {
        var result = Result.Fail(new ValidationError("Some error"));
        
        var problemDetails = result.AsProblemDetails();
        
        Assert.IsType<ProblemDetails>(problemDetails);
    }
}