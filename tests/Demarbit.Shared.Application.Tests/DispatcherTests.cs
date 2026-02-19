using Demarbit.Shared.Application.Dispatching.Contracts;
using Demarbit.Shared.Application.Dispatching.Services;
using Demarbit.Shared.Application.Tests.Fakes;
using Demarbit.Shared.Domain.Contracts;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Demarbit.Shared.Application.Tests;

public class DispatcherTests
{
    // -------------------------------------------------------
    // SendAsync
    // -------------------------------------------------------

    [Fact]
    public async Task SendAsync_should_dispatch_command_to_handler()
    {
        var dispatcher = CreateDispatcher(services =>
            services.AddTransient<IRequestHandler<TestCommand, int>, TestCommandHandler>());

        var result = await dispatcher.SendAsync(new TestCommand("test"));

        result.Should().Be(42);
    }

    [Fact]
    public async Task SendAsync_should_dispatch_query_to_handler()
    {
        var dispatcher = CreateDispatcher(services =>
            services.AddTransient<IRequestHandler<TestQuery, string>, TestQueryHandler>());

        var result = await dispatcher.SendAsync(new TestQuery("world"));

        result.Should().Be("Result: world");
    }

    [Fact]
    public async Task SendAsync_should_throw_ArgumentNullException_for_null_request()
    {
        var dispatcher = CreateDispatcher();

        var act = () => dispatcher.SendAsync<int>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_should_throw_when_handler_not_registered()
    {
        var dispatcher = CreateDispatcher();

        var act = () => dispatcher.SendAsync(new TestCommand("missing"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SendAsync_should_execute_pipeline_behaviors()
    {
        var callLog = new List<string>();

        var dispatcher = CreateDispatcher(services => services
            .AddTransient<IRequestHandler<TestCommand, int>, TestCommandHandler>()
            .AddSingleton<IPipelineBehavior<TestCommand, int>>(new OuterBehavior(callLog)));

        var result = await dispatcher.SendAsync(new TestCommand("piped"));

        result.Should().Be(42);
        callLog.Should().ContainInOrder("Outer:Before", "Outer:After");
    }

    // -------------------------------------------------------
    // NotifyAsync
    // -------------------------------------------------------

    [Fact]
    public async Task NotifyAsync_should_dispatch_event_to_handler_within_transaction()
    {
        var handler = new TestEventHandler();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.GetAndClearPendingEvents().Returns([]);

        var dispatcher = CreateDispatcher(services => services
            .AddSingleton<IEventHandler<TestDomainEvent>>(handler)
            .AddSingleton(unitOfWork));

        var domainEvent = new TestDomainEvent { Payload = "committed" };

        await dispatcher.NotifyAsync([domainEvent]);

        handler.HandledPayloads.Should().ContainSingle().Which.Should().Be("committed");
        await unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAsync_should_dispatch_multiple_events_sequentially()
    {
        var handler = new TestEventHandler();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.GetAndClearPendingEvents().Returns([]);

        var dispatcher = CreateDispatcher(services => services
            .AddSingleton<IEventHandler<TestDomainEvent>>(handler)
            .AddSingleton(unitOfWork));

        var events = new[]
        {
            new TestDomainEvent { Payload = "first" },
            new TestDomainEvent { Payload = "second" }
        };

        await dispatcher.NotifyAsync(events);

        handler.HandledPayloads.Should().ContainInOrder("first", "second");
    }

    [Fact]
    public async Task NotifyAsync_should_propagate_scope_context()
    {
        var handler = new TestEventHandler();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.GetAndClearPendingEvents().Returns([]);
        var propagator = Substitute.For<IScopeContextPropagator>();

        var dispatcher = CreateDispatcher(
            services => services
                .AddSingleton<IEventHandler<TestDomainEvent>>(handler)
                .AddSingleton(unitOfWork),
            propagator);

        await dispatcher.NotifyAsync([new TestDomainEvent { Payload = "ctx" }]);

        propagator.Received(1).Propagate(Arg.Any<IServiceProvider>());
    }

    [Fact]
    public async Task NotifyAsync_should_work_without_scope_context_propagator()
    {
        var handler = new TestEventHandler();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.GetAndClearPendingEvents().Returns([]);

        var dispatcher = CreateDispatcher(services => services
            .AddSingleton<IEventHandler<TestDomainEvent>>(handler)
            .AddSingleton(unitOfWork));

        var act = () => dispatcher.NotifyAsync([new TestDomainEvent { Payload = "no-propagator" }]);

        await act.Should().NotThrowAsync();
        handler.HandledPayloads.Should().ContainSingle();
    }

    [Fact]
    public async Task NotifyAsync_should_rollback_and_rethrow_on_handler_failure()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.GetAndClearPendingEvents().Returns([]);

        var dispatcher = CreateDispatcher(services => services
            .AddSingleton<IEventHandler<TestDomainEvent>>(new FailingEventHandler())
            .AddSingleton(unitOfWork));

        var act = () => dispatcher.NotifyAsync([new TestDomainEvent { Payload = "fail" }]);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await unitOfWork.Received(1).RollbackTransactionAsync(Arg.Any<CancellationToken>());
        unitOfWork.Received().GetAndClearPendingEvents();
    }

    [Fact]
    public async Task NotifyAsync_should_clear_pending_events_after_commit()
    {
        var handler = new TestEventHandler();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.GetAndClearPendingEvents().Returns([]);

        var dispatcher = CreateDispatcher(services => services
            .AddSingleton<IEventHandler<TestDomainEvent>>(handler)
            .AddSingleton(unitOfWork));

        await dispatcher.NotifyAsync([new TestDomainEvent { Payload = "test" }]);

        unitOfWork.Received().GetAndClearPendingEvents();
    }

    [Fact]
    public async Task NotifyAsync_should_handle_empty_event_collection()
    {
        var dispatcher = CreateDispatcher();

        var act = () => dispatcher.NotifyAsync([]);

        await act.Should().NotThrowAsync();
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------

    private static Dispatcher CreateDispatcher(
        Action<IServiceCollection>? configure = null,
        IScopeContextPropagator? propagator = null)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);
        var provider = services.BuildServiceProvider();

        return new Dispatcher(provider, propagator, NullLogger<Dispatcher>.Instance);
    }
}
