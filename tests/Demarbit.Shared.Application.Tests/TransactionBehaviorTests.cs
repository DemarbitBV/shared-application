using Demarbit.Shared.Application.Dispatching.Behaviors;
using Demarbit.Shared.Application.Dispatching.Contracts;
using Demarbit.Shared.Application.Exceptions;
using Demarbit.Shared.Application.Tests.Fakes;
using Demarbit.Shared.Domain.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Demarbit.Shared.Application.Tests;

public class TransactionBehaviorTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();

    private TransactionBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
        => new(_unitOfWork, _dispatcher, NullLogger<TransactionBehavior<TRequest, TResponse>>.Instance);

    [Fact]
    public async Task Should_wrap_commands_in_transaction()
    {
        var behavior = CreateBehavior<TestCommand, int>();
        _unitOfWork.GetAndClearPendingEvents().Returns([]);

        await behavior.HandleAsync(
            new TestCommand("test"),
            _ => Task.FromResult(42),
            CancellationToken.None);

        await _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_skip_transaction_for_queries()
    {
        var behavior = CreateBehavior<TestQuery, string>();

        var result = await behavior.HandleAsync(
            new TestQuery("filter"),
            _ => Task.FromResult("data"),
            CancellationToken.None);

        result.Should().Be("data");
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_rollback_on_unexpected_exception()
    {
        var behavior = CreateBehavior<TestCommand, int>();

        var act = () => behavior.HandleAsync(
            new TestCommand("test"),
            _ => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AppException>();
        await _unitOfWork.Received(1).RollbackTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_rollback_and_rethrow_on_AppException()
    {
        var behavior = CreateBehavior<TestCommand, int>();

        var act = () => behavior.HandleAsync(
            new TestCommand("test"),
            _ => throw new NotFoundException("Order", Guid.NewGuid()),
            CancellationToken.None);

        // NotFoundException extends AppException, so it should be rethrown as-is
        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.Received(1).RollbackTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_dispatch_domain_events_after_commit()
    {
        var behavior = CreateBehavior<TestCommand, int>();
        var fakeEvent = new TestDomainEvent { Payload = "committed" };
        _unitOfWork.GetAndClearPendingEvents().Returns([fakeEvent]);

        await behavior.HandleAsync(
            new TestCommand("test"),
            _ => Task.FromResult(42),
            CancellationToken.None);

        await _dispatcher.Received(1).NotifyAsync(
            Arg.Is<IEnumerable<IDomainEvent>>(events => events.Any()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_not_dispatch_events_when_none_pending()
    {
        var behavior = CreateBehavior<TestCommand, int>();
        _unitOfWork.GetAndClearPendingEvents().Returns([]);

        await behavior.HandleAsync(
            new TestCommand("test"),
            _ => Task.FromResult(42),
            CancellationToken.None);

        await _dispatcher.DidNotReceive().NotifyAsync(
            Arg.Any<IEnumerable<IDomainEvent>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_clear_pending_events_on_rollback()
    {
        var behavior = CreateBehavior<TestCommand, int>();

        var act = () => behavior.HandleAsync(
            new TestCommand("test"),
            _ => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AppException>();
        _unitOfWork.Received().GetAndClearPendingEvents();
    }
}