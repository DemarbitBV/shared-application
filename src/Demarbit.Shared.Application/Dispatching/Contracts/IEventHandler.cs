using Demarbit.Shared.Domain.Contracts;

namespace Demarbit.Shared.Application.Dispatching.Contracts;

/// <summary>
/// Handles a domain event published via the dispatcher.
/// Multiple handlers can be registered for the same event type.
/// Event handlers execute after the originating command's transaction has committed.
/// </summary>
/// <typeparam name="TEvent">The domain event type.</typeparam>
public interface IEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the domain event.
    /// </summary>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}