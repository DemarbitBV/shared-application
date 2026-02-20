using Demarbit.Shared.Application.Tests.Fakes;
using Demarbit.Shared.Domain.Contracts;
using FluentAssertions;
using NSubstitute;

namespace Demarbit.Shared.Application.Tests;

public class IdempotentEventHandlerTests
{
    private readonly IEventIdempotencyService _idempotencyService = Substitute.For<IEventIdempotencyService>();

    [Fact]
    public async Task Should_call_HandleCoreAsync_when_event_has_not_been_processed()
    {
        _idempotencyService
            .HasBeenProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new TestIdempotentEventHandler(_idempotencyService);
        var @event = new TestDomainEvent { Payload = "hello" };

        await handler.HandleAsync(@event, CancellationToken.None);

        handler.HandledPayloads.Should().ContainSingle().Which.Should().Be("hello");
    }

    [Fact]
    public async Task Should_skip_HandleCoreAsync_when_event_has_already_been_processed()
    {
        _idempotencyService
            .HasBeenProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new TestIdempotentEventHandler(_idempotencyService);
        var @event = new TestDomainEvent { Payload = "hello" };

        await handler.HandleAsync(@event, CancellationToken.None);

        handler.HandledPayloads.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_mark_event_as_processed_after_successful_handling()
    {
        _idempotencyService
            .HasBeenProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new TestIdempotentEventHandler(_idempotencyService);
        var @event = new TestDomainEvent { Payload = "hello" };

        await handler.HandleAsync(@event, CancellationToken.None);

        await _idempotencyService.Received(1).MarkAsProcessedAsync(
            @event.EventId,
            @event.EventType,
            handler.GetType().FullName!,
            CancellationToken.None);
    }

    [Fact]
    public async Task Should_not_mark_event_as_processed_when_already_processed()
    {
        _idempotencyService
            .HasBeenProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new TestIdempotentEventHandler(_idempotencyService);
        var @event = new TestDomainEvent { Payload = "hello" };

        await handler.HandleAsync(@event, CancellationToken.None);

        await _idempotencyService.DidNotReceive().MarkAsProcessedAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_pass_handler_full_name_to_idempotency_check()
    {
        _idempotencyService
            .HasBeenProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new TestIdempotentEventHandler(_idempotencyService);
        var @event = new TestDomainEvent { Payload = "hello" };

        await handler.HandleAsync(@event, CancellationToken.None);

        await _idempotencyService.Received(1).HasBeenProcessedAsync(
            @event.EventId,
            handler.GetType().FullName!,
            CancellationToken.None);
    }

    [Fact]
    public async Task Should_pass_event_type_name_to_mark_as_processed()
    {
        _idempotencyService
            .HasBeenProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new TestIdempotentEventHandler(_idempotencyService);
        var @event = new TestDomainEvent { Payload = "hello" };

        await handler.HandleAsync(@event, CancellationToken.None);

        await _idempotencyService.Received(1).MarkAsProcessedAsync(
            @event.EventId,
            nameof(TestDomainEvent),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_not_mark_as_processed_when_HandleCoreAsync_throws()
    {
        _idempotencyService
            .HasBeenProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new TestIdempotentEventHandler(_idempotencyService)
        {
            OnHandleCore = (_, _) => throw new InvalidOperationException("Core handler failed")
        };
        var @event = new TestDomainEvent { Payload = "hello" };

        var act = () => handler.HandleAsync(@event, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Core handler failed");
        await _idempotencyService.DidNotReceive().MarkAsProcessedAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_propagate_cancellation_token_to_all_calls()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _idempotencyService
            .HasBeenProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        CancellationToken receivedToken = default;
        var handler = new TestIdempotentEventHandler(_idempotencyService)
        {
            OnHandleCore = (_, ct) =>
            {
                receivedToken = ct;
                return Task.CompletedTask;
            }
        };
        var @event = new TestDomainEvent { Payload = "hello" };

        await handler.HandleAsync(@event, token);

        receivedToken.Should().Be(token);
        await _idempotencyService.Received(1).HasBeenProcessedAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), token);
        await _idempotencyService.Received(1).MarkAsProcessedAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), token);
    }
}
