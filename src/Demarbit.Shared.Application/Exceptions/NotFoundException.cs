namespace Demarbit.Shared.Application.Exceptions;

/// <summary>
/// Thrown when a requested entity does not exist.
/// Maps to HTTP 404 Not Found.
/// </summary>
public class NotFoundException : AppException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException" /> class
    /// </summary>
    /// <param name="entityType">The entity type for which a record was not found</param>
    public NotFoundException(string entityType)
        : base($"{entityType} not found.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException" /> class
    /// </summary>
    /// <param name="entityType">The entity type for which a record was not found</param>
    /// <param name="id">The unique id of the entity thas was not found</param>
    public NotFoundException(string entityType, object id)
        : base($"{entityType} with ID '{id}' not found.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException" /> class
    /// </summary>
    /// <param name="entityType">The entity type for which a record was not found</param>
    /// <param name="propertyName">The name of the property used to look up the entity</param>
    /// <param name="propertyValue">The value of the property user to look up the entity</param>
    public NotFoundException(string entityType, string propertyName, object? propertyValue)
        : base($"{entityType} with {propertyName} '{propertyValue}' not found.")
    {
    }
}