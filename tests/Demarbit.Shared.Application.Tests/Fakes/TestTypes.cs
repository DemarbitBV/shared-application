using Demarbit.Shared.Application.Dispatching.Contracts;
using Demarbit.Shared.Application.Exceptions;
using Demarbit.Shared.Application.Models;
using Demarbit.Shared.Domain.Models;

namespace Demarbit.Shared.Application.Tests.Fakes;

// -- Requests --

public record TestQuery(string Filter) : IQuery<string>;

public record TestCommand(string Name) : ICommand<int>;

public record FailingCommand(string Value) : ICommand<int>;

public record GenericFailingCommand(string Value) : ICommand<int>;

// -- Handlers --

public class TestQueryHandler : IRequestHandler<TestQuery, string>
{
    public Task<string> HandleAsync(TestQuery request, CancellationToken cancellationToken)
        => Task.FromResult($"Result: {request.Filter}");
}

public class TestCommandHandler : IRequestHandler<TestCommand, int>
{
    public Task<int> HandleAsync(TestCommand request, CancellationToken cancellationToken)
        => Task.FromResult(42);
}

public class FailingTestCommandHandler : IRequestHandler<FailingCommand, int>
{
    public Task<int> HandleAsync(FailingCommand request, CancellationToken cancellationToken)
        => throw new AppException("Something went wrong");
}

public class GenericFailingTestCommandHandler : IRequestHandler<GenericFailingCommand, int>
{
    public Task<int> HandleAsync(GenericFailingCommand request, CancellationToken cancellationToken)
        => throw new Exception("Something went wrong");
}

// -- Validators --

public class AlwaysPassValidator : IValidator<TestCommand>
{
    public Task<IReadOnlyList<ValidationError>> ValidateAsync(TestCommand request, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ValidationError>>([]);
}

public class AlwaysFailValidator : IValidator<TestCommand>
{
    public Task<IReadOnlyList<ValidationError>> ValidateAsync(TestCommand request, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ValidationError>>([
            new ValidationError("Name", "Name is required.", "REQUIRED"),
            new ValidationError("Name", "Name must be at least 3 characters.", "MIN_LENGTH")
        ]);
}

public class AsyncUniqueNameValidator : IValidator<TestCommand>
{
    private readonly HashSet<string> _existingNames;

    public AsyncUniqueNameValidator(IEnumerable<string> existingNames)
        => _existingNames = [..existingNames];

    public async Task<IReadOnlyList<ValidationError>> ValidateAsync(TestCommand request, CancellationToken ct = default)
    {
        // Simulate async DB check
        await Task.Delay(1, ct);
        if (_existingNames.Contains(request.Name))
            return [new ValidationError("Name", $"Name '{request.Name}' already exists.", "DUPLICATE")];

        return [];
    }
}

// -- Behaviors --

public class OuterBehavior : IPipelineBehavior<TestCommand, int>
{
    public List<string> CallLog { get; }

    public OuterBehavior(List<string> callLog) => CallLog = callLog;

    public async Task<int> HandleAsync(TestCommand request, Func<CancellationToken, Task<int>> next, CancellationToken cancellationToken)
    {
        CallLog.Add("Outer:Before");
        var result = await next(cancellationToken);
        CallLog.Add("Outer:After");
        return result;
    }
}

public class InnerBehavior : IPipelineBehavior<TestCommand, int>
{
    public List<string> CallLog { get; }

    public InnerBehavior(List<string> callLog) => CallLog = callLog;

    public async Task<int> HandleAsync(TestCommand request, Func<CancellationToken, Task<int>> next, CancellationToken cancellationToken)
    {
        CallLog.Add("Inner:Before");
        var result = await next(cancellationToken);
        CallLog.Add("Inner:After");
        return result;
    }
}

// -- Events --

public sealed record TestDomainEvent : DomainEventBase
{
    public required string Payload { get; init; }
}

public sealed record AnotherDomainEvent : DomainEventBase
{
    public required int Value { get; init; }
}

public class TestEventHandler : IEventHandler<TestDomainEvent>
{
    public List<string> HandledPayloads { get; } = [];

    public Task HandleAsync(TestDomainEvent @event, CancellationToken cancellationToken)
    {
        HandledPayloads.Add(@event.Payload);
        return Task.CompletedTask;
    }
}

public class SecondTestEventHandler : IEventHandler<TestDomainEvent>
{
    public List<string> HandledPayloads { get; } = [];

    public Task HandleAsync(TestDomainEvent @event, CancellationToken cancellationToken)
    {
        HandledPayloads.Add(@event.Payload);
        return Task.CompletedTask;
    }
}

public class FailingEventHandler : IEventHandler<TestDomainEvent>
{
    public Task HandleAsync(TestDomainEvent @event, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Event handler failed");
}
