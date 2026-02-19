using Demarbit.Shared.Application.Dispatching.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Demarbit.Shared.Application.Dispatching.Internals;

/// <summary>
/// Untyped base for cached handler containers. Enables storing mixed container types
/// in a single <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
internal abstract class RequestHandlerBase
{
    // Prevent derivation outside this assembly — this is a closed hierarchy.
    private protected RequestHandlerBase() { }
    
    public abstract Task<object?> HandleAsync(
        object request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Typed base that exposes the response type for the dispatcher.
/// </summary>
internal abstract class RequestHandlerBase<TResponse> : RequestHandlerBase
{
    public abstract Task<TResponse> HandleAsync(
        IRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

/// <summary>
/// Concrete handler container that resolves the handler and pipeline behaviors from DI,
/// builds the pipeline, and invokes it. One instance is cached per request type.
/// </summary>
internal sealed class RequestHandlerContainer<TRequest, TResponse> : RequestHandlerBase<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override async Task<object?> HandleAsync(
        object request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var response = await HandleAsync((IRequest<TResponse>)request, serviceProvider, cancellationToken);
        return response;
    }

    public override Task<TResponse> HandleAsync(
        IRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        // Innermost: the actual handler
        Func<CancellationToken, Task<TResponse>> handler = ct =>
            serviceProvider
                .GetRequiredService<IRequestHandler<TRequest, TResponse>>()
                .HandleAsync((TRequest)request, ct);

        // Wrap with pipeline behaviors (outermost = first registered)
        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse();

        foreach (var behavior in behaviors)
        {
            var next = handler;
            handler = ct => behavior.HandleAsync((TRequest)request, next, ct);
        }

        return handler(cancellationToken);
    }
}