using Demarbit.Shared.Application.Dispatching.Contracts;
using Demarbit.Shared.Domain.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Demarbit.Shared.Application.Dispatching.Internals;

/// <summary>
/// Untyped base for cached event handler invokers. Enables storing mixed invoker types
/// in a single <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
internal abstract class EventHandlerInvoker
{
    public abstract Task InvokeAsync(
        IDomainEvent domainEvent,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

/// <summary>
/// Typed event handler invoker that resolves all <see cref="IEventHandler{TEvent}"/> instances
/// from DI and invokes them sequentially. One instance is cached per event type.
/// </summary>
internal sealed class EventHandlerInvoker<TEvent> : EventHandlerInvoker
    where TEvent : IDomainEvent
{
    public override async Task InvokeAsync(
        IDomainEvent domainEvent,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var handlers = serviceProvider.GetServices<IEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync((TEvent)domainEvent, cancellationToken);
        }
    }
}