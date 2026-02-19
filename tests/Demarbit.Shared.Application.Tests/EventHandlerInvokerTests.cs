using Demarbit.Shared.Application.Dispatching.Contracts;
using Demarbit.Shared.Application.Dispatching.Internals;
using Demarbit.Shared.Application.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Demarbit.Shared.Application.Tests;

public class EventHandlerInvokerTests
{
    [Fact]
    public async Task Should_invoke_all_registered_event_handlers()
    {
        var handler1 = new TestEventHandler();
        var handler2 = new SecondTestEventHandler();

        var services = new ServiceCollection()
            .AddSingleton<IEventHandler<TestDomainEvent>>(handler1)
            .AddSingleton<IEventHandler<TestDomainEvent>>(handler2)
            .BuildServiceProvider();

        var invoker = new EventHandlerInvoker<TestDomainEvent>();
        var domainEvent = new TestDomainEvent { Payload = "hello" };

        await invoker.InvokeAsync(domainEvent, services, CancellationToken.None);

        handler1.HandledPayloads.Should().ContainSingle().Which.Should().Be("hello");
        handler2.HandledPayloads.Should().ContainSingle().Which.Should().Be("hello");
    }

    [Fact]
    public async Task Should_do_nothing_when_no_handlers_registered()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var invoker = new EventHandlerInvoker<TestDomainEvent>();
        var domainEvent = new TestDomainEvent { Payload = "ignored" };

        var act = () => invoker.InvokeAsync(domainEvent, services, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Should_invoke_handlers_sequentially()
    {
        var callOrder = new List<int>();

        var handler1 = new OrderTrackingEventHandler(callOrder, 1);
        var handler2 = new OrderTrackingEventHandler(callOrder, 2);

        var services = new ServiceCollection()
            .AddSingleton<IEventHandler<TestDomainEvent>>(handler1)
            .AddSingleton<IEventHandler<TestDomainEvent>>(handler2)
            .BuildServiceProvider();

        var invoker = new EventHandlerInvoker<TestDomainEvent>();
        var domainEvent = new TestDomainEvent { Payload = "order-test" };

        await invoker.InvokeAsync(domainEvent, services, CancellationToken.None);

        callOrder.Should().ContainInOrder(1, 2);
    }

    [Fact]
    public async Task Should_propagate_handler_exception()
    {
        var services = new ServiceCollection()
            .AddSingleton<IEventHandler<TestDomainEvent>>(new FailingEventHandler())
            .BuildServiceProvider();

        var invoker = new EventHandlerInvoker<TestDomainEvent>();
        var domainEvent = new TestDomainEvent { Payload = "boom" };

        var act = () => invoker.InvokeAsync(domainEvent, services, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Event handler failed");
    }

    private class OrderTrackingEventHandler(List<int> callOrder, int id) : IEventHandler<TestDomainEvent>
    {
        public async Task HandleAsync(TestDomainEvent @event, CancellationToken cancellationToken)
        {
            await Task.Yield(); // ensure async path
            callOrder.Add(id);
        }
    }
}
