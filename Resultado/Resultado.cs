using System.Collections.ObjectModel;
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

public abstract record Result<T> : Result
{
    public static implicit operator Result<T>(Failure failure) => new Failure<T>(failure);
}

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

    public partial record Failure<T> : Result<T>, IFailedResult
    {
        private readonly IReadOnlyCollection<string> _errors = ReadOnlyCollection<string>.Empty;
        private readonly Kind _kind = Kind.Error;

        public IReadOnlyCollection<ValidationError> ValidationErrors { get; init; } =
            ReadOnlyCollection<ValidationError>.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TraceId { get; init; }

        public required string Title { get; init; } = string.Empty;

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
                    ValidationErrors.Count > 0 ? ValidationErrors.Select(va => va.Detail).ToArray() : [];
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

        [SetsRequiredMembers]
        public Failure(Failure failure)
        {
            ValidationErrors = failure.ValidationErrors;
            Kind = failure.Kind;
            Title = failure.Title;
            Detail = failure.Detail;
            _errors = failure.Errors;
        }
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

        public Failure()
        {
        }
    }

    public static Success<T> Succeed<T>(T value, Kind kind = Kind.Ok) => new(value) { Kind = kind };

    public static Success Succeed(string? message = null, Kind kind = Kind.Ok) => new(message) { Kind = kind };

    public static Failure Fail(string title, string error, Kind kind = Kind.Error) =>
        new(error) { Title = title, Kind = kind };

    public static Failure Fail(params string[] errors) => new(errors) { Title = string.Empty };

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