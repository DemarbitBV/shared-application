namespace Demarbit.Shared.Application.Dispatching.Contracts;

/// <summary>
/// Handles a request and produces a response.
/// Every request has exactly one handler.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}