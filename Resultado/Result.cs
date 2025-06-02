using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Resultado;

public enum Kind : ushort
{
    Ok,
    Created,
    NoContent,
    Accepted,

    Error,
    Critical,
    Unavailable,
    Invalid,
    Unprocessable,
    Forbidden,
    Unauthorized,
    Conflict,
    NotFound,
    FailedDependency
}

[Flags]
public enum ValidationSeverity : ushort
{
    Error,
    Critical,
    Warning,
    Info
}

public partial record ValidationError
{
    /// <summary>
    /// Also known as "Message" or "ErrorMessage", it should detail what caused the error or what is wrong.
    /// </summary>
    public required string Detail { get; init; }

    /// <summary>
    /// Also known as "Identifier", who was the faulty member.
    /// <example>nameof(BalancesDto.Balance) // Because Balance was negative and it's not allowed.</example>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Pointer { get; set; }

    /// <summary>
    /// Severity can have multiple flags.
    /// <example>ValidationSeverity.Error | ValidationSeverity.Critial</example>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ValidationSeverity? Severity { get; init; } = ValidationSeverity.Error;

    /// <summary>
    /// Generic member, can be anything you like. Maybe an error number/identifier that is mapped to your domain.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Code { get; init; }

    public ValidationError()
    {
    }

    [SetsRequiredMembers]
    public ValidationError(string detail) => Detail = detail;

    [SetsRequiredMembers]
    public ValidationError(string detail, string? pointer = null) => (Detail, Pointer) = (detail, pointer);

    [SetsRequiredMembers]
    public ValidationError(string detail, string? pointer = null, ValidationSeverity? severity = null) =>
        (Detail, Pointer, Severity) = (detail, pointer, severity);

    [SetsRequiredMembers]
    public ValidationError(string detail, string? pointer = null, ValidationSeverity? severity = null,
        string? code = null) =>
        (Detail, Pointer, Severity, Code) = (detail, pointer, severity, code);
}

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

public abstract record Result<T> : Result;

public partial record Result
{
    public partial record Success<T>(
        T Value,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        string? Message = null
    ) : Result<T>, ISuccessfulResult<T>, ISuccessfulResult
    {
        private readonly Kind _kind = Kind.Ok;

        public Kind Kind
        {
            get => _kind;
            init => _kind = value < Kind.Error
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value),
                    "Cannot set non-success status to a success result.");
        }

        public static bool IsSuccess => true;
    }

    public partial record Success(
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        string? Message
    ) : Result<string>, ISuccessfulResult
    {
        private readonly Kind _kind = Kind.Ok;

        public Kind Kind
        {
            get => _kind;
            init => _kind = value < Kind.Error
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value),
                    "Cannot set non-success status to a success result.");
        }

        public static bool IsSuccess => true;
    }

    public partial record Failure : Result, IFailedResult
    {
        private readonly IReadOnlyCollection<string> _errors = [];
        private readonly Kind _kind = Kind.Error;
        public IReadOnlyCollection<ValidationError> ValidationErrors { get; init; } = [];

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TraceId { get; init; } = null;

        public required string Title { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Detail { get; init; }

        public Kind Kind
        {
            get => _kind;
            init => _kind = value >= Kind.Error
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value),
                    "Cannot set non-error status to a failure result.");
        }

        public static bool IsSuccess => false;

        public IReadOnlyCollection<string> Errors
        {
            get
            {
                return _errors.Count > 0 ? _errors :
                    ValidationErrors.Count > 0 ? ValidationErrors.Select(ve => ve.Detail).ToArray() : [];
            }
            init => _errors = value;
        }

        public Failure(string title, string? detail = null) => (Title, Detail) = (title, detail);
        public Failure(string error) => _errors = [error];
        public Failure(IReadOnlyCollection<string> errors) => _errors = errors;
        public Failure(params string[] errors) => _errors = errors;

        public Failure(params ValidationError[] validationErrors) =>
            (ValidationErrors, Kind) = (validationErrors, Kind.Invalid);

        public Failure(ValidationError validationError) => (ValidationErrors, Kind) = ([validationError], Kind.Invalid);
    }

    public static Success<T> Succeed<T>(T value, Kind kind = Kind.Ok) => new(value) { Kind = kind };

    public static Success Succeed(string? message = null, Kind kind = Kind.Ok) => new(message) { Kind = kind };

    public static Failure Fail(string title, string error, Kind kind = Kind.Error) =>
        new(error) { Title = title, Kind = kind };

    public static Failure Fail(params string[] errors) => new(errors) { Title = string.Empty };
    public static Failure Fail(params IReadOnlyCollection<string> errors) => new(errors) { Title = string.Empty };

    public static Failure Fail(params ValidationError[] validationErrors) =>
        new(validationErrors) { Title = string.Empty };

    public static Failure Fail(string title, ValidationError validationError, Kind kind = Kind.Error) =>
        new(validationError) { Title = title, Kind = kind };

    public static Failure Fail(ValidationError validationError, Kind kind = Kind.Error) =>
        new(validationError) { Title = string.Empty, Kind = kind };


    protected Result()
    {
    }
}