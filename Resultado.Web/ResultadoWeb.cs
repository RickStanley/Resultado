using Microsoft.AspNetCore.Mvc;

namespace Resultado.Web;

// Inspirado em: https://github.com/ardalis/Result/issues/193
public static class ResultadoWeb
{
    private const string StatusCodeDocumentationLinkBase =
        "https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status";

    public static int AsHttpStatusCode(this Kind kind) =>
        kind switch
        {
            Kind.Ok => StatusCodes.Status200OK,
            Kind.Created => StatusCodes.Status201Created,
            Kind.NoContent => StatusCodes.Status204NoContent,
            Kind.Accepted => StatusCodes.Status202Accepted,
            Kind.Error => StatusCodes.Status500InternalServerError,
            Kind.Critical => StatusCodes.Status500InternalServerError,
            Kind.Unavailable => StatusCodes.Status503ServiceUnavailable,
            Kind.Invalid => StatusCodes.Status400BadRequest,
            Kind.Unprocessable => StatusCodes.Status422UnprocessableEntity,
            Kind.Forbidden => StatusCodes.Status403Forbidden,
            Kind.Unauthorized => StatusCodes.Status401Unauthorized,
            Kind.Conflict => StatusCodes.Status409Conflict,
            Kind.NotFound => StatusCodes.Status404NotFound,
            Kind.FailedDependency => StatusCodes.Status424FailedDependency,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    public static ProblemDetails AsProblemDetails(
        this IResult result,
        string? detail = null,
        string? instance = null,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        IDictionary<string, object?>? extensions = null
    )
    {
        IDictionary<string, object?> extensionDictionary = extensions ?? new Dictionary<string, object?>();

        if (result is not IFailedResult failedResult)
            throw new ArgumentException($"{nameof(result)} must implement {nameof(IFailedResult)}.", nameof(result));

        if (failedResult.ValidationErrors.Count > 0)
            extensionDictionary.Add("errors",
                failedResult.ValidationErrors.Select(ve => new { ve.Pointer, ve.Detail }));

        return new ProblemDetails
        {
            Title = title ?? failedResult.Title,
            Detail = detail ?? failedResult.Detail ?? failedResult.Errors.FirstOrDefault(),
            Status = statusCode ?? failedResult.Kind.AsHttpStatusCode(),
            Type = type ?? StatusCodeDocumentationLinkBase + "/" + failedResult.Kind.AsHttpStatusCode(),
            Instance = instance,
            Extensions = extensionDictionary
        };
    }
}