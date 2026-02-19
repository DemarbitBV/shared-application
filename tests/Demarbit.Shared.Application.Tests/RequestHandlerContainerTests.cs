using Demarbit.Shared.Application.Dispatching.Contracts;
using Demarbit.Shared.Application.Dispatching.Internals;
using Demarbit.Shared.Application.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Demarbit.Shared.Application.Tests;

public class RequestHandlerContainerTests
{
    [Fact]
    public async Task Should_resolve_handler_and_return_result()
    {
        var services = new ServiceCollection()
            .AddTransient<IRequestHandler<TestCommand, int>, TestCommandHandler>()
            .BuildServiceProvider();

        var container = new RequestHandlerContainer<TestCommand, int>();

        var result = await container.HandleAsync(
            new TestCommand("test"), services, CancellationToken.None);

        result.Should().Be(42);
    }

    [Fact]
    public async Task Should_resolve_query_handler_and_return_result()
    {
        var services = new ServiceCollection()
            .AddTransient<IRequestHandler<TestQuery, string>, TestQueryHandler>()
            .BuildServiceProvider();

        var container = new RequestHandlerContainer<TestQuery, string>();

        var result = await container.HandleAsync(
            new TestQuery("hello"), services, CancellationToken.None);

        result.Should().Be("Result: hello");
    }

    [Fact]
    public async Task Should_execute_behaviors_in_registration_order()
    {
        var callLog = new List<string>();

        var services = new ServiceCollection()
            .AddTransient<IRequestHandler<TestCommand, int>, TestCommandHandler>()
            .AddSingleton<IPipelineBehavior<TestCommand, int>>(new OuterBehavior(callLog))
            .AddSingleton<IPipelineBehavior<TestCommand, int>>(new InnerBehavior(callLog))
            .BuildServiceProvider();

        var container = new RequestHandlerContainer<TestCommand, int>();

        var result = await container.HandleAsync(
            new TestCommand("test"), services, CancellationToken.None);

        result.Should().Be(42);
        callLog.Should().ContainInOrder(
            "Outer:Before", "Inner:Before", "Inner:After", "Outer:After");
    }

    [Fact]
    public async Task Should_work_without_behaviors()
    {
        var services = new ServiceCollection()
            .AddTransient<IRequestHandler<TestCommand, int>, TestCommandHandler>()
            .BuildServiceProvider();

        var container = new RequestHandlerContainer<TestCommand, int>();

        var result = await container.HandleAsync(
            new TestCommand("no-behaviors"), services, CancellationToken.None);

        result.Should().Be(42);
    }

    [Fact]
    public async Task Should_throw_when_handler_not_registered()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var container = new RequestHandlerContainer<TestCommand, int>();

        var act = () => container.HandleAsync(
            new TestCommand("missing"), services, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Untyped_HandleAsync_should_delegate_to_typed_overload()
    {
        var services = new ServiceCollection()
            .AddTransient<IRequestHandler<TestCommand, int>, TestCommandHandler>()
            .BuildServiceProvider();

        RequestHandlerBase container = new RequestHandlerContainer<TestCommand, int>();

        var result = await container.HandleAsync(
            new TestCommand("untyped"), services, CancellationToken.None);

        result.Should().Be(42);
    }
}
