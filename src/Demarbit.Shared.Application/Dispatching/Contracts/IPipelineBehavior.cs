namespace Demarbit.Shared.Application.Dispatching.Contracts;

/// <summary>
/// Pipeline behavior that wraps request handling. Behaviors execute in order of registration
/// and can perform cross-cutting concerns like logging, validation, transactions, caching, etc.
/// <para>
/// Behaviors are resolved from DI as <c>IEnumerable&lt;IPipelineBehavior&lt;TRequest, TResponse&gt;&gt;</c>
/// and wrap the handler in registration order (first registered = outermost).
/// </para>
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Executes the behavior. Call <paramref name="next"/> to pass control to the next
    /// behavior in the pipeline (or to the handler if this is the innermost behavior).
    /// </summary>
    /// <param name="request">The request being handled.</param>
    /// <param name="next">Delegate to the next behavior or handler in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TResponse> HandleAsync(
        TRequest request,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken);
}