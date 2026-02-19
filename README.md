# Demarbit.Shared.Domain

Application-layer building blocks for DDD and Clean Architecture in .NET — dispatching, CQRS, pipeline behaviors, validation, and structured exceptions.

[![CI/CD](https://github.com/DemarbitBV/shared-application/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/DemarbitBV/shared-application/actions/workflows/ci-cd.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=DemarbitBV_shared-application&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=DemarbitBV_shared-application)
[![NuGet](https://img.shields.io/nuget/v/Demarbit.Shared.Application)](https://www.nuget.org/packages/Demarbit.Shared.Application)

## What's Included

**Dispatching** — A lightweight in-process dispatcher with `ICommand<TResponse>`, `IQuery<TResponse>`, single `IRequestHandler<TRequest, TResponse>`, and domain event notification via `IEventHandler<TEvent>`. No void handler track, no `Unit` type — every request returns a response.

**Pipeline behaviors** — `IPipelineBehavior<TRequest, TResponse>` with built-in logging, transaction, and validation behaviors. Public interface so consumers can add their own (caching, authorization, rate limiting, etc.).

**Validation** — Async `IValidator<T>` with structured `ValidationError` results (property name, message, error code). Supports database lookups and external service checks.

**Exceptions** — Consistent `AppException` hierarchy: `ConflictException`, `ForbiddenException`, `NotFoundException`, and `ValidationFailedException` all extend a single base. One catch filter handles them all.

**Models** — `Optional<T>` for PATCH semantics, `PagedResult<T>` with pagination helpers, `ValidationError`, and `SortDirection`.

## Quick Start

### Commands and Queries

Commands represent intent to change state. Queries represent intent to read. Both return a response — pair with `Demarbit.Results` for a natural fit, or use any type.

```csharp
// With Demarbit.Results
public record CreateOrder(string CustomerName) : ICommand<Result>;
public record GetOrder(Guid Id) : IQuery<Result>;

// Without Results — any return type works
public record CreateOrder(string CustomerName) : ICommand<Guid>;
public record GetOrder(Guid Id) : IQuery<OrderViewModel>;
```

### Request Handlers

One handler interface for all requests. No separate void handler.

```csharp
public class CreateOrderHandler : IRequestHandler<CreateOrder, Result>
{
    public async Task<Result> HandleAsync(CreateOrder request, CancellationToken ct)
    {
        var order = new Order(request.CustomerName);
        await _repository.AddAsync(order, ct);
        return Result.Success(order.Id);
    }
}
```

### Dispatching

Send commands/queries and notify domain events through `IDispatcher`:

```csharp
// Send a command or query
var result = await dispatcher.SendAsync(new CreateOrder("Acme Corp"), ct);

// Notify domain events after saving
await dispatcher.NotifyAsync(order.DequeueDomainEvents(), ct);
```

### Validation

Validators are async and return structured errors:

```csharp
public class CreateOrderValidator : IValidator
{
    public async Task<IReadOnlyList<ValidationError>> ValidateAsync(CreateOrder request, CancellationToken ct)
    {
        var errors = new List();

        if (string.IsNullOrWhiteSpace(request.CustomerName))
            errors.Add(new ValidationError(nameof(request.CustomerName), "Customer name is required."));

        if (await _customers.ExistsAsync(request.CustomerName, ct) is false)
            errors.Add(new ValidationError(nameof(request.CustomerName), "Customer not found.", "CUSTOMER_NOT_FOUND"));

        return errors;
    }
}
```

The `ValidationBehavior` runs all registered validators before the handler executes and throws `ValidationFailedException` with the collected errors.

### Pipeline Behaviors

Behaviors wrap every request in a pipeline. Three are included out of the box:

```
ValidationBehavior  →  LoggingBehavior  →  TransactionBehavior  →  Handler
```

- **ValidationBehavior** — runs all `IValidator<TRequest>` implementations, throws `ValidationFailedException` on failure.
- **LoggingBehavior** — logs request/response timing. Only logs unexpected exceptions (not `AppException` subtypes).
- **TransactionBehavior** — wraps commands in a `IUnitOfWork.SaveChangesAsync` call. Triggered by the `ITransactional` marker on `ICommand<T>` — no runtime reflection.

Add your own:

```csharp
public class CachingBehavior : IPipelineBehavior
    where TRequest : IQuery
{
    public async Task HandleAsync(
        TRequest request,
        RequestHandlerDelegate next,
        CancellationToken ct)
    {
        var cached = await _cache.GetAsync(request);
        if (cached is not null) return cached;

        var response = await next();
        await _cache.SetAsync(request, response);
        return response;
    }
}
```

### Exceptions

All application exceptions share a single base class:

```csharp
try { ... }
catch (AppException ex)
{
    // Catches ConflictException, ForbiddenException, NotFoundException,
    // ValidationFailedException — one filter, no enumeration needed.
}
```

`ValidationFailedException` carries structured error details:

```csharp
catch (ValidationFailedException ex)
{
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage} ({error.ErrorCode})");
    }
}
```

### Optional for PATCH Semantics

`Optional<T>` distinguishes "field absent" from "field explicitly set to null" in partial updates:

```csharp
public record UpdateCustomer(
    Guid Id,
    Optional Name,        // absent = don't change, Some(value) = update
    Optional PhoneNumber  // absent = don't change, Some(null) = clear
) : ICommand;
```

JSON serialization is handled by the included `OptionalJsonConverterFactory`.

### Event Handlers

Handle domain events dispatched after persistence:

```csharp
public class OrderCreatedHandler : IEventHandler
{
    public async Task HandleAsync(OrderCreatedEvent @event, CancellationToken ct)
    {
        await _emailService.SendOrderConfirmationAsync(@event.OrderId, ct);
    }
}
```

Event dispatching uses cached typed invokers — no reflection at runtime.

### Scope Context Propagation

Event handlers run in their own DI scope. To propagate ambient context (user ID, tenant, correlation ID), implement `IScopeContextPropagator`:

```csharp
public class SessionContextPropagator : IScopeContextPropagator
{
    public void PropagateToScope(IServiceProvider sourceScope, IServiceProvider targetScope)
    {
        var source = sourceScope.GetRequiredService();
        var target = targetScope.GetRequiredService();
        target.UserId = source.UserId;
        target.TenantId = source.TenantId;
    }
}
```

Register it in DI — the dispatcher picks it up automatically. If none is registered, no propagation occurs.

### Registration

One call wires up the dispatcher, behaviors, and all handlers/validators in the given assemblies:

```csharp
services.AddSharedApplication(typeof(CreateOrderHandler).Assembly);
```

## Design Principles

- **Single handler track** — every request returns a response. No `Unit`, no void handlers, no parallel interface hierarchies. This cuts dispatching complexity by ~30%.
- **Consistent exception base** — all application exceptions extend `AppException`. One catch filter handles all expected failures.
- **No infrastructure dependencies** — depends only on `Demarbit.Shared.Domain`, `Microsoft.Extensions.DependencyInjection.Abstractions`, and `Microsoft.Extensions.Logging.Abstractions`. No EF Core, no HTTP, no third-party packages.
- **Extensible pipeline** — `IPipelineBehavior` is public. Consumers add behaviors without forking the library.
- **Results-ready, not Results-required** — designed to pair naturally with `Demarbit.Results` but works with any return type.

## Architecture Fit

```
Demarbit.Shared.Domain          ← zero deps
    ↑
Demarbit.Shared.Application     ← this package
    ↑
Demarbit.Shared.Infrastructure  ← EF Core, repositories, event dispatch
    ↑
[Your Application Project]      ← references what it needs
```

`Demarbit.Results` sits alongside as an independent package — use it as `TResponse` for commands and queries:

```
Demarbit.Results                ← zero deps (standalone)
    ↑
[Your Application Project]      ← ICommand<Result<Guid>>, IQuery<Result<OrderDto>>
```

## License

[MIT](LICENSE)