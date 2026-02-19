using Demarbit.Shared.Application.Dispatching.Behaviors;
using Demarbit.Shared.Application.Dispatching.Contracts;
using Demarbit.Shared.Application.Exceptions;
using Demarbit.Shared.Application.Tests.Fakes;
using FluentAssertions;

namespace Demarbit.Shared.Application.Tests;

public class ValidationBehaviorTests
{
    private static Func<CancellationToken, Task<int>> NextReturning(int value) =>
        _ => Task.FromResult(value);

    [Fact]
    public async Task Should_proceed_when_no_validators_registered()
    {
        var behavior = new ValidationBehavior<TestCommand, int>(
            Enumerable.Empty<IValidator<TestCommand>>());

        var result = await behavior.HandleAsync(
            new TestCommand("test"), NextReturning(42), CancellationToken.None);

        result.Should().Be(42);
    }

    [Fact]
    public async Task Should_proceed_when_all_validators_pass()
    {
        var behavior = new ValidationBehavior<TestCommand, int>(
            [new AlwaysPassValidator()]);

        var result = await behavior.HandleAsync(
            new TestCommand("test"), NextReturning(42), CancellationToken.None);

        result.Should().Be(42);
    }

    [Fact]
    public async Task Should_throw_ValidationFailedException_when_validators_fail()
    {
        var behavior = new ValidationBehavior<TestCommand, int>(
            [new AlwaysFailValidator()]);

        var act = () => behavior.HandleAsync(
            new TestCommand("test"), NextReturning(42), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationFailedException>();
        ex.Which.Errors.Should().HaveCount(2);
        ex.Which.RequestName.Should().Be("TestCommand");
    }

    [Fact]
    public async Task Should_aggregate_errors_from_multiple_validators()
    {
        var behavior = new ValidationBehavior<TestCommand, int>(
            [new AlwaysFailValidator(), new AlwaysFailValidator()]);

        var act = () => behavior.HandleAsync(
            new TestCommand("test"), NextReturning(42), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationFailedException>();
        ex.Which.Errors.Should().HaveCount(4); // 2 errors × 2 validators
    }

    [Fact]
    public async Task Should_support_async_validators()
    {
        var behavior = new ValidationBehavior<TestCommand, int>(
            [new AsyncUniqueNameValidator(["taken"])]);

        var act = () => behavior.HandleAsync(
            new TestCommand("taken"), NextReturning(42), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationFailedException>();
        ex.Which.Errors.Should().ContainSingle()
            .Which.ErrorCode.Should().Be("DUPLICATE");
    }

    [Fact]
    public async Task Should_preserve_structured_error_details()
    {
        var behavior = new ValidationBehavior<TestCommand, int>(
            [new AlwaysFailValidator()]);

        var act = () => behavior.HandleAsync(
            new TestCommand("test"), NextReturning(42), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationFailedException>();
        var firstError = ex.Which.Errors[0];
        firstError.PropertyName.Should().Be("Name");
        firstError.ErrorMessage.Should().Be("Name is required.");
        firstError.ErrorCode.Should().Be("REQUIRED");
    }
}