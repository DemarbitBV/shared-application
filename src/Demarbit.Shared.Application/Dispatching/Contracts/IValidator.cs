using Demarbit.Shared.Application.Models;

namespace Demarbit.Shared.Application.Dispatching.Contracts;

/// <summary>
/// Validates a request before it reaches its handler.
/// Multiple validators can be registered for the same request type;
/// all are executed and their errors are aggregated.
/// <para>
/// Supports async validation for checks that require database access,
/// external service calls, or other I/O.
/// </para>
/// </summary>
/// <typeparam name="T">The request type to validate.</typeparam>
public interface IValidator<in T>
{
    /// <summary>
    /// Validates the request and returns any validation errors.
    /// Return an empty collection if the request is valid.
    /// </summary>
    Task<IReadOnlyList<ValidationError>> ValidateAsync(T request, CancellationToken cancellationToken = default);
}