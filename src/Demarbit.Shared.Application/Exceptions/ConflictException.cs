namespace Demarbit.Shared.Application.Exceptions;

/// <summary>
/// Thrown when an operation conflicts with the current state (e.g. duplicate entity).
/// Maps to HTTP 409 Conflict.
/// </summary>
public class ConflictException : AppException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException" /> class
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public ConflictException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException" /> class
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified</param>
    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException" /> class
    /// </summary>
    /// <param name="entityName">The name of the entity for which the conflict occurred</param>
    /// <param name="identifier">The unique identifier of the conflicting entity</param>
    public ConflictException(string entityName, object identifier)
        : base($"A {entityName} with identifier '{identifier}' already exists.")
    {
    }
}