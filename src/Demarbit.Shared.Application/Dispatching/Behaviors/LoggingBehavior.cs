using System.Diagnostics;
using Demarbit.Shared.Application.Dispatching.Contracts;
using Demarbit.Shared.Application.Exceptions;
using Microsoft.Extensions.Logging;

namespace Demarbit.Shared.Application.Dispatching.Behaviors;

/// <summary>
/// Pipeline behavior that logs request entry, exit, timing, and errors.
/// Application exceptions (extending <see cref="AppException"/>) are logged as warnings
/// and re-thrown. Unexpected exceptions are logged as errors and wrapped in <see cref="AppException"/>.
/// </summary>
internal sealed partial class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        LogHandlingRequestName(logger, requestName);

        try
        {
            var response = await next(cancellationToken);

            sw.Stop();
            LogHandledRequestNameInElapsedMs(logger, requestName, sw.ElapsedMilliseconds);

            return response;
        }
        catch (AppException ex)
        {
            sw.Stop();
            LogRequestNameFailedAfterElapsedMsMessage(logger, ex, requestName, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogRequestNameFailedAfterElapsedMs(logger, ex, requestName, sw.ElapsedMilliseconds);
            throw new AppException($"An unexpected error occurred executing {requestName}.", ex);
        }
    }

    [LoggerMessage(LogLevel.Information, "Handling {RequestName}")]
    static partial void LogHandlingRequestName(ILogger<LoggingBehavior<TRequest, TResponse>> logger, string requestName);

    [LoggerMessage(LogLevel.Information, "Handled {RequestName} in {ElapsedMs}ms")]
    static partial void LogHandledRequestNameInElapsedMs(ILogger<LoggingBehavior<TRequest, TResponse>> logger, string requestName, long elapsedMs);

    [LoggerMessage(LogLevel.Warning, "{RequestName} failed after {ElapsedMs}ms: {Message}")]
    static partial void LogRequestNameFailedAfterElapsedMsMessage(ILogger<LoggingBehavior<TRequest, TResponse>> logger, Exception ex, string requestName, long elapsedMs, string message);

    [LoggerMessage(LogLevel.Error, "{RequestName} failed after {ElapsedMs}ms")]
    static partial void LogRequestNameFailedAfterElapsedMs(ILogger<LoggingBehavior<TRequest, TResponse>> logger, Exception ex, string requestName, long elapsedMs);
}