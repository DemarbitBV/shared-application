using Demarbit.Shared.Application.Dispatching.Contracts;
using Demarbit.Shared.Domain.Contracts;

namespace Demarbit.Shared.Application.Dispatching.Services;

/// <summary>
/// Base class for event handlers that require idempotency protection
/// </summary>
public abstract class IdempotentEventHandler<TEvent>(IEventIdempotencyService idempotencyService)
    : IEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Standard entry point for event handlers. Use <see cref="HandleCoreAsync" /> for implementing the logic for Idempotent Event Handlers
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    public async Task HandleAsync(TEvent @event, CancellationToken cancellationToken)
    {
        var handlerType = GetType().FullName ?? GetType().Name;

        // Check if this event has already been processed by this handler
        var alreadyProcessed = await idempotencyService.HasBeenProcessedAsync(
            @event.EventId,
            handlerType,
            cancellationToken);

        if (alreadyProcessed)
        {
            // Event already processed, skip
            return;
        }

        // Process the event
        await HandleCoreAsync(@event, cancellationToken);

        // Mark as processed
        await idempotencyService.MarkAsProcessedAsync(
            @event.EventId,
            @event.EventType,
            handlerType,
            cancellationToken);
    }

    /// <summary>
    /// Implement the actual event handling logic here
    /// </summary>
    protected abstract Task HandleCoreAsync(TEvent @event, CancellationToken cancellationToken);
}