namespace Demarbit.Shared.Application.Exceptions;

/// <summary>
/// Base exception for all application-layer errors.
/// All application-specific exceptions should extend this type so that pipeline behaviors
/// can distinguish expected application errors from unexpected infrastructure failures.
/// </summary>
public class AppException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppException" /> class
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public AppException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppException" /> class
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified</param>
    public AppException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}