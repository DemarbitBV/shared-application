using System.Diagnostics.CodeAnalysis;

namespace Demarbit.Shared.Application.Dispatching.Contracts;

/// <summary>
/// Base interface for all requests (commands and queries).
/// Every request has a response type — there is no void variant.
/// <para>
/// Prefer using <see cref="ICommand{TResponse}"/> or <see cref="IQuery{TResponse}"/>
/// instead of implementing this interface directly.
/// </para>
/// </summary>
/// <typeparam name="TResponse">The type returned by the handler.</typeparam>
[SuppressMessage("SonarAnalyzer.CSharp", "S2326", 
    Justification = "TResponse is a generic marker used to bind request types to their handler and pipeline behavior signatures at compile time.")]
public interface IRequest<out TResponse>
{
}