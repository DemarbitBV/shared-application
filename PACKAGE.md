# Demarbit.Shared.Application

Opinionated shared application layer for .NET projects following Clean Architecture principles. Provides a lightweight CQRS dispatcher with pipeline behaviors (logging, validation, transactions), structured exceptions, and common application models.

- **Target Framework:** `net10.0`
- **Key Dependencies:** `Demarbit.Shared.Domain` (1.0.4), `Microsoft.Extensions.DependencyInjection.Abstractions` (10.0.3), `Microsoft.Extensions.Logging.Abstractions` (10.0.3)

---

## Quick Start

### Register services in DI

```csharp
using Demarbit.Shared.Application.Extensions;

services.AddSharedApplication(typeof(MyCommand).Assembly);
```

This registers the dispatcher, the default pipeline behaviors (logging, validation, transactions), and scans the provided assemblies for request handlers, event handlers, and validators.

### Define a command and handler

```csharp
using Demarbit.Shared.Application.Dispatching.Contracts;

// Command — automatically transactional
public record CreateOrderCommand(string CustomerName, decimal Total) : ICommand<Guid>;

// Handler
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = Order.Create(request.CustomerName, request.Total);
        // persist, raise domain events, etc.
        return order.Id;
    }
}
```

### Dispatch through the pipeline

```csharp
public class OrdersController(IDispatcher dispatcher)
{
    public async Task<Guid> CreateOrder(CreateOrderCommand command, CancellationToken ct)
    {
        return await dispatcher.SendAsync(command, ct);
    }
}
```

---

## Core Concepts

### CQRS Dispatcher

All requests flow through `IDispatcher.SendAsync`. Commands (`ICommand<TResponse>`) represent state-mutating operations and are automatically transactional. Queries (`IQuery<TResponse>`) represent read operations and skip the transaction behavior.

### Pipeline Behaviors

Requests pass through a middleware pipeline before reaching their handler. The default pipeline order is:

1. **LoggingBehavior** (outermost) — logs request entry, exit, timing, and errors using source-generated `[LoggerMessage]` methods.
2. **ValidationBehavior** — runs all registered `IValidator<T>` instances, aggregates errors, and throws `ValidationFailedException` if any fail.
3. **TransactionBehavior** (innermost) — wraps `ITransactional` requests in a database transaction via `IUnitOfWork`. After commit, dispatches pending domain events.

### Domain Event Dispatch

After a command's transaction commits, the `TransactionBehavior` collects pending domain events from `IUnitOfWork.GetAndClearPendingEvents()` and dispatches them via `IDispatcher.NotifyAsync`. Each event is handled in an isolated DI scope with its own transaction, preventing DbContext concurrency issues. Events raised within event handlers are **not** recursively dispatched — they are cleared after the handler's transaction commits.

### Exception Hierarchy

All application-layer errors derive from `AppException`. The `LoggingBehavior` logs `AppException` subtypes as warnings (expected errors) and wraps unexpected exceptions in `AppException` (logged as errors). The hierarchy maps to HTTP status codes:

| Exception | HTTP Status |
|---|---|
| `NotFoundException` | 404 |
| `ConflictException` | 409 |
| `ForbiddenException` | 403 |
| `ValidationFailedException` | 400 / 422 |

### Assembly Scanning

`AddSharedApplication` scans provided assemblies for concrete, non-abstract, non-generic types implementing `IRequestHandler<,>`, `IEventHandler<>`, and `IValidator<>`, then registers them as scoped services. The library's own assembly is always included in the scan.

---

## Public API Reference

### Dispatching Contracts

**Namespace: `Demarbit.Shared.Application.Dispatching.Contracts`**

#### `IRequest<out TResponse>`

```csharp
public interface IRequest<out TResponse> { }
```

Base marker interface for all requests. `TResponse` is covariant.

#### `ICommand<out TResponse>`

```csharp
public interface ICommand<out TResponse> : IRequest<TResponse>, ITransactional;
```

Marker for commands (state-mutating requests). Inherits `ITransactional`, so commands always execute inside a transaction.

#### `IQuery<out TResponse>`

```csharp
public interface IQuery<out TResponse> : IRequest<TResponse>;
```

Marker for queries (read-only requests). Does not implement `ITransactional`.

#### `ITransactional`

```csharp
public interface ITransactional;
```

Marker interface signaling that a request should execute within a database transaction. `ICommand<TResponse>` inherits this automatically. Can be applied directly to any `IRequest<TResponse>` if a query also needs transactional behavior.

#### `IRequestHandler<in TRequest, TResponse>`

```csharp
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
```

Exactly one handler per request type. Resolved from DI.

#### `IEventHandler<in TEvent>`

```csharp
public interface IEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
```

Multiple handlers can be registered per event type. `IDomainEvent` is from `Demarbit.Shared.Domain`.

#### `IDispatcher`

```csharp
public interface IDispatcher
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    Task NotifyAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
}
```

- `SendAsync` — dispatches a command or query through the pipeline to its handler.
- `NotifyAsync` — publishes domain events to all registered `IEventHandler<T>` instances, each in an isolated scope.

#### `IPipelineBehavior<in TRequest, TResponse>`

```csharp
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(
        TRequest request,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken);
}
```

Middleware-like behavior wrapping request handling. Call `next(cancellationToken)` to invoke the next behavior or the handler.

#### `IValidator<in T>`

```csharp
public interface IValidator<in T>
{
    Task<IReadOnlyList<ValidationError>> ValidateAsync(T request, CancellationToken cancellationToken = default);
}
```

Multiple validators per request type. All are executed and errors are aggregated. Return an empty list if validation passes.

#### `IScopeContextPropagator`

```csharp
public interface IScopeContextPropagator
{
    void Propagate(IServiceProvider targetScope);
}
```

Propagates ambient context (e.g., user ID, tenant, correlation ID) from the current scope into the new DI scope created for each domain event handler. Optional — if not registered, context propagation is skipped.

### Dispatching Services

**Namespace: `Demarbit.Shared.Application.Dispatching.Services`**

#### `IdempotentEventHandler<TEvent>`

```csharp
public abstract class IdempotentEventHandler<TEvent>(IEventIdempotencyService idempotencyService)
    : IEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    public async Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
    protected abstract Task HandleCoreAsync(TEvent @event, CancellationToken cancellationToken);
}
```

Base class for event handlers that need idempotency protection. Uses `IEventIdempotencyService` (from `Demarbit.Shared.Domain`) to check if an event has already been processed by this handler, and marks it as processed after successful handling. Subclasses implement `HandleCoreAsync` with the actual logic.

### Application Contracts

**Namespace: `Demarbit.Shared.Application.Contracts`**

#### `ITimeZoneConverter`

```csharp
public interface ITimeZoneConverter
{
    TimeOnly ConvertFromUtc(TimeOnly utcTime, DateOnly? referenceDate = null);
    TimeOnly ConvertToUtc(TimeOnly localTime, DateOnly? referenceDate = null);
    (DateOnly Date, TimeOnly Time) GetLocalDateAndTime(DateTime utcDateTime);
}
```

Contract for converting between UTC and a user's local timezone. Implementation is left to the consuming project (resolve timezone from session, HTTP headers, config, etc.). Not registered by this library.

### Exceptions

**Namespace: `Demarbit.Shared.Application.Exceptions`**

#### `AppException`

```csharp
public class AppException : Exception
{
    public AppException(string message);
    public AppException(string message, Exception innerException);
}
```

Base class for all application-layer exceptions. Unexpected exceptions caught by the pipeline are wrapped in `AppException`.

#### `NotFoundException`

```csharp
public class NotFoundException : AppException
{
    public NotFoundException(string entityType);
    public NotFoundException(string entityType, object id);
    public NotFoundException(string entityType, string propertyName, object? propertyValue);
}
```

Messages: `"{entityType} not found."`, `"{entityType} with ID '{id}' not found."`, `"{entityType} with {propertyName} '{propertyValue}' not found."`

#### `ConflictException`

```csharp
public class ConflictException : AppException
{
    public ConflictException(string message);
    public ConflictException(string message, Exception innerException);
    public ConflictException(string entityName, object identifier);
}
```

Entity/identifier overload message: `"A {entityName} with identifier '{identifier}' already exists."`

#### `ForbiddenException`

```csharp
public class ForbiddenException : AppException
{
    public ForbiddenException(string message);
    public ForbiddenException(string entityType, object id);
}
```

Entity/id overload message: `"You do not have permission to access {entityType} with ID '{id}'."`

#### `ValidationFailedException`

```csharp
public class ValidationFailedException : AppException
{
    public string RequestName { get; }
    public IReadOnlyList<ValidationError> Errors { get; }

    public ValidationFailedException(string requestName, IReadOnlyList<ValidationError> errors);
}
```

Thrown by `ValidationBehavior` when one or more validators return errors. Contains the structured list of `ValidationError` instances.

### Models

**Namespace: `Demarbit.Shared.Application.Models`**

#### `Optional<T>` (readonly struct)

```csharp
public readonly struct Optional<T>
{
    public bool HasValue { get; }
    public T Value { get; }  // throws InvalidOperationException if !HasValue

    public static Optional<T> Some(T value);
    public static Optional<T> None();
    public T GetValueOrDefault(T defaultValue = default!);
    public void Apply(Action<T> action);  // executes action only if HasValue
}
```

For PATCH semantics: distinguishes "field absent from request" (`None`) from "field explicitly set to null" (`Some(null)`).

#### `PagedResult<T>` (sealed class)

```csharp
public sealed class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }       // 1-based
    public required int PageSize { get; init; }
    public int TotalPages { get; }                 // computed: ceil(TotalCount / PageSize)
    public bool HasNextPage { get; }               // computed: Page < TotalPages
    public bool HasPreviousPage { get; }           // computed: Page > 1

    public static PagedResult<T> Empty(int page = 1, int pageSize = 10);
}
```

Standard paged result for list/overview queries.

#### `ValidationError` (sealed record)

```csharp
public sealed record ValidationError(
    string PropertyName,
    string ErrorMessage,
    string? ErrorCode = null)
{
    public static ValidationError General(string errorMessage, string? errorCode = null);
}
```

`General` creates an error with an empty `PropertyName` (not tied to a specific field).

#### `SortDirection` (enum)

```csharp
public enum SortDirection
{
    Asc,
    Desc
}
```

### JSON Converters

**Namespace: `Demarbit.Shared.Application.Json`**

#### `OptionalJsonConverterFactory`

```csharp
public sealed class OptionalJsonConverterFactory : JsonConverterFactory
```

Register in `JsonSerializerOptions.Converters` to enable `Optional<T>` deserialization with PATCH semantics.

#### `OptionalJsonConverter<T>`

```csharp
public sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
```

Created automatically by the factory. JSON `null` token deserializes to `Optional<T>.Some(default!)` (field present, value is null). Absent JSON properties remain the default `Optional<T>` which is `None`.

---

## Usage Patterns & Examples

### Implementing a command with validation

```csharp
// Command
public record CreateProductCommand(string Name, decimal Price) : ICommand<Guid>;

// Validator
public class CreateProductCommandValidator : IValidator<CreateProductCommand>
{
    public Task<IReadOnlyList<ValidationError>> ValidateAsync(
        CreateProductCommand request, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add(new ValidationError(nameof(request.Name), "Name is required."));

        if (request.Price <= 0)
            errors.Add(new ValidationError(nameof(request.Price), "Price must be positive."));

        return Task.FromResult<IReadOnlyList<ValidationError>>(errors);
    }
}

// Handler
public class CreateProductCommandHandler(IProductRepository repository)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = Product.Create(request.Name, request.Price);
        await repository.AddAsync(product, cancellationToken);
        return product.Id;
    }
}
```

### Implementing a query with paged results

```csharp
public record GetProductsQuery(int Page, int PageSize, SortDirection Sort)
    : IQuery<PagedResult<ProductDto>>;

public class GetProductsQueryHandler(IProductReadRepository repository)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> HandleAsync(
        GetProductsQuery request, CancellationToken cancellationToken)
    {
        return await repository.GetPagedAsync(request.Page, request.PageSize, request.Sort, cancellationToken);
    }
}
```

### PATCH command with `Optional<T>`

```csharp
public record UpdateProductCommand(Guid Id, Optional<string> Name, Optional<decimal?> Price)
    : ICommand<Unit>;

public class UpdateProductCommandHandler(IProductRepository repository)
    : IRequestHandler<UpdateProductCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        // Only applies the update if the field was present in the request
        request.Name.Apply(name => product.UpdateName(name));
        request.Price.Apply(price => product.UpdatePrice(price));

        return Unit.Value;
    }
}
```

Register the JSON converter for `Optional<T>` in your API project:

```csharp
builder.Services.Configure<JsonOptions>(options =>
    options.JsonSerializerOptions.Converters.Add(new OptionalJsonConverterFactory()));
```

### Implementing a domain event handler

```csharp
public class OrderCreatedEventHandler(IEmailService emailService)
    : IEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event, CancellationToken cancellationToken)
    {
        await emailService.SendOrderConfirmationAsync(@event.OrderId, cancellationToken);
    }
}
```

### Implementing an idempotent event handler

```csharp
public class SendWelcomeEmailHandler(
    IEventIdempotencyService idempotencyService,
    IEmailService emailService)
    : IdempotentEventHandler<UserRegisteredEvent>(idempotencyService)
{
    protected override async Task HandleCoreAsync(
        UserRegisteredEvent @event, CancellationToken cancellationToken)
    {
        // This will only execute once per EventId, even if the event is re-delivered
        await emailService.SendWelcomeEmailAsync(@event.UserId, cancellationToken);
    }
}
```

### Implementing a custom pipeline behavior

```csharp
public class AuthorizationBehavior<TRequest, TResponse>(ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        if (request is IRequireAuthorization authRequest)
        {
            if (!currentUser.HasPermission(authRequest.RequiredPermission))
                throw new ForbiddenException("Insufficient permissions.");
        }

        return await next(cancellationToken);
    }
}

// Register it
services.AddPipelineBehavior(typeof(AuthorizationBehavior<,>));
```

### Implementing a scope context propagator

```csharp
public class ScopeContextPropagator(ICurrentUser currentUser)
    : IScopeContextPropagator
{
    public void Propagate(IServiceProvider targetScope)
    {
        var scopedUser = targetScope.GetRequiredService<ICurrentUser>();
        scopedUser.SetFrom(currentUser);
    }
}
```

Register as a scoped service. When domain events are dispatched to isolated scopes, the propagator copies the ambient context (user, tenant, etc.) into each new scope.

### Throwing structured exceptions

```csharp
// In handlers:
throw new NotFoundException(nameof(Product), productId);
// => "Product with ID '...' not found."

throw new ConflictException(nameof(Product), product.Sku);
// => "A Product with identifier '...' already exists."

throw new ForbiddenException(nameof(Order), orderId);
// => "You do not have permission to access Order with ID '...'."
```

---

## Integration Points

### DI Registration

| Extension Method | What It Registers |
|---|---|
| `services.AddSharedApplication(assemblies)` | `IDispatcher` (scoped), `LoggingBehavior`, `ValidationBehavior`, `TransactionBehavior` (all scoped), plus assembly-scanned `IRequestHandler<,>`, `IEventHandler<>`, `IValidator<>` (all scoped) |
| `services.AddPipelineBehavior(typeof(MyBehavior<,>))` | A custom `IPipelineBehavior<,>` (scoped). Appended after the built-in behaviors in the pipeline. |

### Pipeline Execution Order

```
Request → LoggingBehavior → ValidationBehavior → TransactionBehavior → Handler
```

Built-in behaviors are registered first. Custom behaviors added via `AddPipelineBehavior` execute between the `TransactionBehavior` and the handler (they are appended to the behavior list, then the list is reversed so first-registered = outermost).

### Domain Event Lifecycle

1. Command handler raises domain events on entities.
2. `TransactionBehavior` calls `IUnitOfWork.SaveChangesAsync` then `CommitTransactionAsync`.
3. After commit, pending events are collected via `IUnitOfWork.GetAndClearPendingEvents()`.
4. Each event is dispatched in an **isolated DI scope** with its own transaction.
5. `IScopeContextPropagator.Propagate` is called on each new scope (if registered).
6. Events raised within event handlers are cleared after their transaction commits — no recursive dispatch.

### Required External Registrations

The following services must be registered by the consuming project:

| Interface | Source Package | Required By |
|---|---|---|
| `IUnitOfWork` | `Demarbit.Shared.Domain` | `TransactionBehavior`, `Dispatcher` (event handling) |
| `IScopeContextPropagator` | This package | `Dispatcher` (event handling) — **optional**, nullable |
| `ITimeZoneConverter` | This package | Application code — **optional**, no built-in consumer |
| `IEventIdempotencyService` | `Demarbit.Shared.Domain` | `IdempotentEventHandler<T>` — **only if used** |

---

## Dependencies & Compatibility

### Runtime Dependencies

| Package | Version | Notes |
|---|---|---|
| `Demarbit.Shared.Domain` | 1.0.4 | Provides `IDomainEvent`, `IUnitOfWork`, `IEventIdempotencyService`, `Guard`. Must be registered before or alongside this package. |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.3 | DI abstractions only — no concrete container dependency. |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.3 | Logging abstractions only — bring your own logging provider. |

### Peer Dependencies

- **`Demarbit.Shared.Domain`** must have its `IUnitOfWork` implementation registered in DI. Without it, `TransactionBehavior` and domain event dispatch will fail at runtime.
- A concrete logging provider (e.g., `Microsoft.Extensions.Logging.Console`, Serilog) should be registered for pipeline logging to produce output.
