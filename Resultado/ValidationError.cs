using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Resultado;

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
    public string? Pointer { get; init; }

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