namespace TitanFitness.Domain.Abstractions;

/// <summary>
/// What kind of failure an <see cref="Error"/> represents.
/// The API layer turns each kind into an HTTP status code.
/// </summary>
public enum ErrorType
{
    /// <summary>A value breaks a business rule (422). Usually tied to one field.</summary>
    Validation = 1,

    /// <summary>A referenced record does not exist (404).</summary>
    NotFound = 2,

    /// <summary>The request clashes with existing state: a duplicate, an overlap, a closed session (409).</summary>
    Conflict = 3
}

/// <summary>
/// An expected business failure. Returned inside a <see cref="Result"/> instead of being thrown.
/// </summary>
/// <param name="Code">Stable machine-readable code, e.g. "Membership.FreezeDaysExceeded".</param>
/// <param name="Message">Human-readable explanation shown to staff.</param>
/// <param name="Type">How the failure should be reported.</param>
/// <param name="Field">The input field the failure belongs to, when there is one (camelCase).</param>
public sealed record Error(string Code, string Message, ErrorType Type, string? Field = null)
{
    public static Error Validation(string code, string message, string? field = null) =>
        new(code, message, ErrorType.Validation, field);

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message, string? field = null) =>
        new(code, message, ErrorType.Conflict, field);
}
