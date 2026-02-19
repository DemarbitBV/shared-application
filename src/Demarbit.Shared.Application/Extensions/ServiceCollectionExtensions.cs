using System.Reflection;
using Demarbit.Shared.Application.Dispatching.Behaviors;
using Demarbit.Shared.Application.Dispatching.Contracts;
using Demarbit.Shared.Application.Dispatching.Services;
using Demarbit.Shared.Domain.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Demarbit.Shared.Application.Extensions;

/// <summary>
/// Extension methods for registering shared application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the dispatcher, pipeline behaviors, and scans the provided assemblies
    /// for request handlers, event handlers, and validators.
    /// <para>
    /// The dispatcher assembly is always scanned in addition to any assemblies you provide.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan for handlers and validators.</param>
    public static IServiceCollection AddSharedApplication(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        // Core services
        services.AddScoped<IDispatcher, Dispatcher>();

        // Pipeline behaviors (order matters: logging → validation → transaction)
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        // Scan assemblies for handlers and validators
        Assembly[] scanTargets =
        [
            ..assemblies,
            typeof(IDispatcher).Assembly
        ];

        foreach (var assembly in scanTargets.Distinct())
        {
            RegisterImplementations(services, assembly,
                typeof(IRequestHandler<,>),
                typeof(IEventHandler<>),
                typeof(IValidator<>));
        }

        return services;
    }
    
    /// <summary>
    /// Register a custom pipeline behavior in the DI container
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="behaviorType">The type of the behavior (Expected to implement IPipelineBehavior)</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">Thrown if the behaviorType property is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the provided type does not implement IPipelineBehavior</exception>
    public static IServiceCollection AddPipelineBehavior(this IServiceCollection services, Type behaviorType)
    {
        Guard.NotNull(behaviorType);

        if (!behaviorType.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>)))
        {
            throw new ArgumentException(
                $"Type '{behaviorType.Name}' does not implement " +
                $"{nameof(IPipelineBehavior<,>)}<TRequest, TResponse>.",
                nameof(behaviorType));
        }

        services.AddScoped(typeof(IPipelineBehavior<,>), behaviorType);
        return services;
    }

    /// <summary>
    /// Scans an assembly for concrete types implementing any of the specified open generic interfaces
    /// and registers them as scoped services.
    /// </summary>
    private static void RegisterImplementations(
        IServiceCollection services,
        Assembly assembly,
        params Type[] openGenericInterfaces)
    {
        var concreteTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });

        foreach (var implementation in concreteTypes)
        {
            var matchingInterfaces = implementation.GetInterfaces()
                .Where(i => i.IsGenericType && openGenericInterfaces.Contains(i.GetGenericTypeDefinition()));

            foreach (var serviceInterface in matchingInterfaces)
            {
                services.AddScoped(serviceInterface, implementation);
            }
        }
    }
}
