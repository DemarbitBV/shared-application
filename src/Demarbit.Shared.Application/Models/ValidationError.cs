namespace Demarbit.Shared.Application.Models;

/// <summary>
/// Represents a single validation error with optional property name and error code.
/// </summary>
/// <param name="PropertyName">
/// The name of the property that failed validation, or an empty string for cross-property errors.
/// </param>
/// <param name="ErrorMessage">Human-readable description of the validation failure.</param>
/// <param name="ErrorCode">
/// Optional machine-readable error code for client-side mapping (e.g. "DUPLICATE_EMAIL", "TOO_SHORT").
/// </param>
public sealed record ValidationError(
    string PropertyName,
    string ErrorMessage,
    string? ErrorCode = null)
{
    /// <summary>
    /// Creates a validation error without a specific property (cross-property or general error).
    /// </summary>
    public static ValidationError General(string errorMessage, string? errorCode = null)
        => new(string.Empty, errorMessage, errorCode);
}