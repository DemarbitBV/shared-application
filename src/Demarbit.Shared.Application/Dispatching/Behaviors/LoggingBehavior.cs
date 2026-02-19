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
internal sealed class LoggingBehavior<TRequest, TResponse>(
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

        logger.LogInformation("Handling {RequestName}", requestName);
        logger.LogDebug("Request payload: {@Request}", request);

        try
        {
            var response = await next(cancellationToken);

            sw.Stop();
            logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
            logger.LogDebug("Response payload: {@Response}", response);

            return response;
        }
        catch (AppException ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "{RequestName} failed after {ElapsedMs}ms: {Message}",
                requestName, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "{RequestName} failed after {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
            throw new AppException($"An unexpected error occurred executing {requestName}.", ex);
        }
    }
}