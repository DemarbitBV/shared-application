using Demarbit.Shared.Application.Dispatching.Contracts;
using Demarbit.Shared.Application.Exceptions;
using Demarbit.Shared.Domain.Contracts;
using Microsoft.Extensions.Logging;

namespace Demarbit.Shared.Application.Dispatching.Behaviors;

/// <summary>
/// Pipeline behavior that wraps <see cref="ITransactional"/> requests in a database transaction.
/// After a successful commit, any domain events collected by the <see cref="IUnitOfWork"/>
/// are dispatched to event handlers.
/// <para>
/// Non-transactional requests (e.g. queries) pass through without transaction overhead.
/// </para>
/// </summary>
internal sealed class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    IDispatcher dispatcher,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        if (request is not ITransactional)
        {
            return await next(cancellationToken);
        }

        var requestName = typeof(TRequest).Name;
        logger.LogDebug("Starting transaction for {RequestName}", requestName);

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            var response = await next(cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            // Dispatch domain events AFTER the transaction commits successfully.
            // The dispatcher creates a new scope per event to avoid DbContext concurrency issues.
            var pendingEvents = unitOfWork.GetAndClearPendingEvents();
            if (pendingEvents.Count > 0)
            {
                await dispatcher.NotifyAsync(pendingEvents, cancellationToken);
            }

            logger.LogDebug("Transaction completed for {RequestName}", requestName);
            return response;
        }
        catch (AppException)
        {
            logger.LogDebug("Rolling back transaction for {RequestName} due to application exception", requestName);
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            unitOfWork.GetAndClearPendingEvents();
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Rolling back transaction for {RequestName}", requestName);
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            unitOfWork.GetAndClearPendingEvents();
            throw new AppException($"An error occurred executing {requestName}.", ex);
        }
    }
}
