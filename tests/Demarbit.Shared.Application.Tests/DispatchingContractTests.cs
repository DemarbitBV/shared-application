using Demarbit.Shared.Application.Dispatching.Contracts;
using Demarbit.Shared.Application.Tests.Fakes;
using FluentAssertions;

namespace Demarbit.Shared.Application.Tests;

public class DispatchingContractTests
{
    [Fact]
    public void Commands_should_be_transactional()
    {
        var command = new TestCommand("test");

        command.Should().BeAssignableTo<ITransactional>();
    }

    [Fact]
    public void Queries_should_not_be_transactional()
    {
        var query = new TestQuery("filter");

        query.Should().NotBeAssignableTo<ITransactional>();
    }

    [Fact]
    public void Commands_should_implement_IRequest()
    {
        var command = new TestCommand("test");

        command.Should().BeAssignableTo<IRequest<int>>();
    }

    [Fact]
    public void Queries_should_implement_IRequest()
    {
        var query = new TestQuery("filter");

        query.Should().BeAssignableTo<IRequest<string>>();
    }
}