using Demarbit.Shared.Application.Dispatching.Contracts;
using Demarbit.Shared.Application.Models;
using Demarbit.Shared.Domain.Models;

namespace Demarbit.Shared.Application.Tests.Fakes;

// -- Requests --

public record TestQuery(string Filter) : IQuery<string>;

public record TestCommand(string Name) : ICommand<int>;

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

// -- Events --

public sealed record TestDomainEvent : DomainEventBase
{
    public required string Payload { get; init; }
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
