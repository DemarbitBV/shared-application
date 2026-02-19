using Demarbit.Shared.Domain.Contracts;

namespace Demarbit.Shared.Application.Dispatching.Contracts;

/// <summary>
/// Dispatches requests through the pipeline to their handlers,
/// and publishes domain events to registered event handlers.
/// </summary>
public interface IDispatcher
{
    /// <summary>
    /// Sends a request through the pipeline to its handler and returns the response.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes domain events (in order) to all registered event handlers.
    /// Each event is dispatched within its own DI scope.
    /// </summary>
    /// <param name="events">The domain events to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
}