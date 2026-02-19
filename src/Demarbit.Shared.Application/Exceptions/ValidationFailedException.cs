using Demarbit.Shared.Application.Models;

namespace Demarbit.Shared.Application.Exceptions;

/// <summary>
/// Thrown when request validation fails. Carries structured <see cref="ValidationError"/> items
/// with property names and error codes for client-facing error mapping.
/// Maps to HTTP 400 Bad Request / 422 Unprocessable Entity.
/// </summary>
public class ValidationFailedException : AppException
{
    /// <summary>
    /// The name of the request that failed validation.
    /// </summary>
    public string RequestName { get; }

    /// <summary>
    /// The validation errors that caused the failure.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationFailedException" /> class
    /// </summary>
    /// <param name="requestName">The name of the request for which one or more validators reported errors</param>
    /// <param name="errors">The validation errors that occurred</param>
    public ValidationFailedException(string requestName, IReadOnlyList<ValidationError> errors)
        : base($"Validation failed for {requestName}: {FormatErrors(errors)}")
    {
        RequestName = requestName;
        Errors = errors;
    }

    private static string FormatErrors(IReadOnlyList<ValidationError> errors)
        => string.Join("; ", errors.Select(e =>
            string.IsNullOrEmpty(e.PropertyName) ? e.ErrorMessage : $"{e.PropertyName}: {e.ErrorMessage}"));
}