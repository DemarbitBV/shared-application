namespace Demarbit.Shared.Application.Dispatching.Contracts;

/// <summary>
/// Marker interface for commands — requests that modify state.
/// Commands are automatically transactional via <see cref="ITransactional"/>.
/// <para>
/// The <typeparamref name="TResponse"/> is typically a Result type (e.g. <c>Result</c>,
/// <c>Result&lt;Guid&gt;</c>) but can be any type.
/// </para>
/// </summary>
/// <typeparam name="TResponse">
/// The type returned by the command handler. For commands that don't produce data,
/// use a simple result/success type (e.g. <c>Result</c>, <c>bool</c>).
/// </typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>, ITransactional;