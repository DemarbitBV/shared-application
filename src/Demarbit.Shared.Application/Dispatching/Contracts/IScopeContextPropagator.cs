namespace Demarbit.Shared.Application.Dispatching.Contracts;

/// <summary>
/// Propagates ambient context (user ID, tenant, correlation ID, etc.) from the current
/// DI scope into a newly created scope. Used by the dispatcher when creating scopes
/// for domain event handlers.
/// <para>
/// Register an implementation to carry request-scoped data (e.g. session context, tenant info)
/// into event handler scopes. If no implementation is registered, no propagation occurs.
/// </para>
/// </summary>
/// <example>
/// <code>
/// public class SessionContextPropagator(ISessionContext current) : IScopeContextPropagator
/// {
///     public void Propagate(IServiceProvider targetScope)
///     {
///         var scoped = targetScope.GetRequiredService&lt;ISessionContext&gt;();
///         scoped.SetUserId(current.UserId);
///         scoped.SetTenantId(current.TenantId);
///     }
/// }
/// </code>
/// </example>
public interface IScopeContextPropagator
{
    /// <summary>
    /// Copies relevant ambient state into the target scope's services.
    /// </summary>
    /// <param name="targetScope">The service provider of the newly created scope.</param>
    void Propagate(IServiceProvider targetScope);
}