using Demarbit.Shared.Application.Models;
using FluentAssertions;

namespace Demarbit.Shared.Application.Tests;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_should_calculate_correctly()
    {
        var result = new PagedResult<string>
        {
            Items = ["a", "b", "c"],
            TotalCount = 25,
            Page = 1,
            PageSize = 10
        };

        result.TotalPages.Should().Be(3); // ceil(25/10) = 3
    }

    [Fact]
    public void TotalPages_should_handle_exact_division()
    {
        var result = new PagedResult<string>
        {
            Items = ["a"],
            TotalCount = 20,
            Page = 1,
            PageSize = 10
        };

        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public void HasNextPage_should_be_true_when_not_on_last_page()
    {
        var result = new PagedResult<string>
        {
            Items = ["a"],
            TotalCount = 25,
            Page = 1,
            PageSize = 10
        };

        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_should_be_false_on_last_page()
    {
        var result = new PagedResult<string>
        {
            Items = ["a"],
            TotalCount = 25,
            Page = 3,
            PageSize = 10
        };

        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_should_be_false_on_first_page()
    {
        var result = new PagedResult<string>
        {
            Items = ["a"],
            TotalCount = 25,
            Page = 1,
            PageSize = 10
        };

        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_should_be_true_on_subsequent_pages()
    {
        var result = new PagedResult<string>
        {
            Items = ["a"],
            TotalCount = 25,
            Page = 2,
            PageSize = 10
        };

        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void Empty_should_return_empty_result()
    {
        var result = PagedResult<string>.Empty(page: 1, pageSize: 20);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void TotalPages_should_be_zero_when_pageSize_is_zero()
    {
        var result = new PagedResult<string>
        {
            Items = [],
            TotalCount = 10,
            Page = 1,
            PageSize = 0
        };

        result.TotalPages.Should().Be(0);
    }
}
