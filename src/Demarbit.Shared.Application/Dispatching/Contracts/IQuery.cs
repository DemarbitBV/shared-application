namespace Demarbit.Shared.Application.Dispatching.Contracts;

/// <summary>
/// Marker interface for queries — requests that read state without side effects.
/// Queries are NOT transactional by default.
/// </summary>
/// <typeparam name="TResponse">The type returned by the query handler (e.g. a DTO, a Result&lt;T&gt;).</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>;