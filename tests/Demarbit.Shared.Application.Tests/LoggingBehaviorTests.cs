using Demarbit.Shared.Application.Dispatching.Behaviors;
using Demarbit.Shared.Application.Dispatching.Contracts;
using Demarbit.Shared.Application.Exceptions;
using Demarbit.Shared.Application.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Demarbit.Shared.Application.Tests;

public class LoggingBehaviorTests
{
    private static LoggingBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
        => new(NullLogger<LoggingBehavior<TRequest, TResponse>>.Instance);

    [Fact]
    public async Task Should_Pass_Without_Errors()
    {
        var behavior = CreateBehavior<TestCommand, int>();

        var result = await behavior.HandleAsync(
            new TestCommand("LogTest"),
            _ => Task.FromResult(10),
            CancellationToken.None);

        result.Should().Be(10);
    }

    [Fact]
    public async Task Should_Rethrow_AppError()
    {
        var behavior = CreateBehavior<FailingCommand, int>();
        
        var act = () => behavior.HandleAsync(
            new FailingCommand("FailedLogTest"),
            _ => throw new AppException("Application error"),
            CancellationToken.None);
        
        await act.Should().ThrowAsync<AppException>();
    }

    [Fact]
    public async Task Should_Throw_AppException()
    {
        var behavior = CreateBehavior<GenericFailingCommand, int>();
        
        var act = () => behavior.HandleAsync(
            new GenericFailingCommand("FailedLogTest"),
            _ => throw new Exception("Application error"),
            CancellationToken.None);
        
        await act.Should().ThrowAsync<AppException>();
    }
}