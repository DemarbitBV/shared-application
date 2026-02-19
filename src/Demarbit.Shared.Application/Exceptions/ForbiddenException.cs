namespace Demarbit.Shared.Application.Exceptions;

/// <summary>
/// Thrown when a user attempts to access a resource they don't have permission to access.
/// Maps to HTTP 403 Forbidden.
/// </summary>
public class ForbiddenException : AppException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenException"/> class
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ForbiddenException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenException"/> class
    /// </summary>
    /// <param name="entityType">The entity that the user does not have access to.</param>
    /// <param name="id">The unique identifier of the entity</param>
    public ForbiddenException(string entityType, object id)
        : base($"You do not have permission to access {entityType} with ID '{id}'.")
    {
    }
}