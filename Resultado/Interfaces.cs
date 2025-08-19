namespace Resultado;

public interface IResult
{
    Kind Kind { get; init; }
    static abstract bool IsSuccess { get; }
}

public interface ISuccessfulResult<T> : IResult
{
    /// <summary>
    /// The value of T. It may or not be present, so check for that. 
    /// </summary>
    T Value { get; init; }
}

public interface ISuccessfulResult : IResult
{
    /// <summary>
    /// A message to pass by.
    /// </summary>
    string? Message { get; init; }
}

public interface IFailedResult : IResult
{
    IReadOnlyCollection<string> Errors { get; init; }
    IReadOnlyCollection<ValidationError> ValidationErrors { get; init; }

    /// <summary>
    /// May be useful to trace down a failure's origins.
    /// </summary>
    string? TraceId { get; init; }

    /// <summary>
    /// A short, human-readable summary of the problem type. It SHOULD NOT change from occurrence to occurrence
    /// of the problem, except for purposes of localization(e.g., using proactive content negotiation;
    /// see[RFC7231], Section 3.4).
    /// <example>You do not have enough credit.</example>
    /// </summary>
    string Title { get; init; }

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the problem.
    /// <example>Your current balance is 30, but that costs 50.</example>
    /// </summary>
    string? Detail { get; init; }
}