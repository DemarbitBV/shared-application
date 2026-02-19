namespace Demarbit.Shared.Application.Dispatching.Contracts;

/// <summary>
/// Marker interface indicating that a request should execute within a database transaction.
/// The <see cref="ICommand{TResponse}"/> interface inherits this by default.
/// <para>
/// Can also be applied directly to any <see cref="IRequest{TResponse}"/> that requires
/// transactional behavior without being semantically a command.
/// </para>
/// </summary>
public interface ITransactional;