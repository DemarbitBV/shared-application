using System.Collections.Concurrent;
using Demarbit.Shared.Application.Dispatching.Contracts;
using Demarbit.Shared.Application.Dispatching.Internals;
using Demarbit.Shared.Domain.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Demarbit.Shared.Application.Dispatching.Services;

/// <summary>
/// Default dispatcher implementation. Dispatches requests through the pipeline to their handlers,
/// and publishes domain events to registered event handlers within isolated DI scopes.
/// </summary>
internal sealed class Dispatcher(
    IServiceProvider serviceProvider,
    IScopeContextPropagator? scopeContextPropagator,
    ILogger<Dispatcher> logger
) : IDispatcher
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerBase> RequestHandlers = new();
    private static readonly ConcurrentDictionary<Type, EventHandlerInvoker> EventInvokers = new();

    // ---------------------------
    // Requests
    // ---------------------------

    public async Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var handler = (RequestHandlerBase<TResponse>)RequestHandlers.GetOrAdd(
            request.GetType(),
            static requestType =>
            {
                // Find the IRequest<TResponse> implementation to extract TResponse at runtime
                var requestInterface = requestType.GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));
                var responseType = requestInterface.GetGenericArguments()[0];

                var containerType = typeof(RequestHandlerContainer<,>).MakeGenericType(requestType, responseType);
                return (RequestHandlerBase)(Activator.CreateInstance(containerType)
                    ?? throw new InvalidOperationException(
                        $"Could not create request handler container for '{requestType.Name}'."));
            });

        return await handler.HandleAsync(request, serviceProvider, cancellationToken);
    }

    // ---------------------------
    // Domain Events
    // ---------------------------

    public async Task NotifyAsync(
        IEnumerable<IDomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            var eventType = domainEvent.GetType();

            var invoker = EventInvokers.GetOrAdd(
                eventType,
                static evtType =>
                {
                    var invokerType = typeof(EventHandlerInvoker<>).MakeGenericType(evtType);
                    return (EventHandlerInvoker)(Activator.CreateInstance(invokerType)
                        ?? throw new InvalidOperationException(
                            $"Could not create event handler invoker for '{evtType.Name}'."));
                });

            // Each event gets its own scope to avoid DbContext concurrency issues
            using var scope = serviceProvider.CreateScope();

            // Propagate ambient context (user ID, tenant, etc.) into the new scope
            scopeContextPropagator?.Propagate(scope.ServiceProvider);

            var scopedUnitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            try
            {
                await scopedUnitOfWork.BeginTransactionAsync(cancellationToken);

                await invoker.InvokeAsync(domainEvent, scope.ServiceProvider, cancellationToken);

                await scopedUnitOfWork.SaveChangesAsync(cancellationToken);
                await scopedUnitOfWork.CommitTransactionAsync(cancellationToken);

                // Clear any events raised by event handlers (no recursive dispatch)
                scopedUnitOfWork.GetAndClearPendingEvents();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Event handler failed for {EventType} (EventId: {EventId})",
                    domainEvent.EventType, domainEvent.EventId);

                await scopedUnitOfWork.RollbackTransactionAsync(cancellationToken);
                scopedUnitOfWork.GetAndClearPendingEvents();
                throw;
            }
        }
    }
}