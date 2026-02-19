using Demarbit.Shared.Application.Exceptions;
using Demarbit.Shared.Application.Models;
using FluentAssertions;

namespace Demarbit.Shared.Application.Tests;

public class ExceptionHierarchyTests
{
    [Theory]
    [InlineData(typeof(NotFoundException))]
    [InlineData(typeof(ConflictException))]
    [InlineData(typeof(ForbiddenException))]
    [InlineData(typeof(ValidationFailedException))]
    public void All_application_exceptions_should_extend_AppException(Type exceptionType)
    {
        exceptionType.Should().BeDerivedFrom<AppException>();
    }

    [Fact]
    public void NotFoundException_should_format_message_with_id()
    {
        var id = Guid.NewGuid();
        var ex = new NotFoundException("Order", id);

        ex.Message.Should().Contain("Order").And.Contain(id.ToString());
    }

    [Fact]
    public void NotFoundException_should_format_message_with_property()
    {
        var ex = new NotFoundException("User", "Email", "test@example.com");

        ex.Message.Should().Contain("User").And.Contain("Email").And.Contain("test@example.com");
    }

    [Fact]
    public void ConflictException_should_format_entity_message()
    {
        var ex = new ConflictException("User", "john@example.com");

        ex.Message.Should().Contain("User").And.Contain("john@example.com");
    }

    [Fact]
    public void ForbiddenException_should_format_entity_message()
    {
        var id = Guid.NewGuid();
        var ex = new ForbiddenException("Invoice", id);

        ex.Message.Should().Contain("Invoice").And.Contain(id.ToString());
    }

    [Fact]
    public void ValidationFailedException_should_carry_structured_errors()
    {
        var errors = new List<ValidationError>
        {
            new("Name", "Name is required.", "REQUIRED"),
            new("Email", "Invalid email format.")
        };

        var ex = new ValidationFailedException("CreateUser", errors);

        ex.RequestName.Should().Be("CreateUser");
        ex.Errors.Should().HaveCount(2);
        ex.Errors[0].ErrorCode.Should().Be("REQUIRED");
        ex.Errors[1].PropertyName.Should().Be("Email");
    }

    [Fact]
    public void ValidationFailedException_message_should_include_error_details()
    {
        var errors = new List<ValidationError>
        {
            new("Name", "Name is required.")
        };

        var ex = new ValidationFailedException("CreateUser", errors);

        ex.Message.Should().Contain("CreateUser").And.Contain("Name: Name is required.");
    }

    [Fact]
    public void AppException_catch_block_should_catch_all_application_exceptions()
    {
        // This is the key benefit of the fixed hierarchy:
        // a single catch(AppException) handles all application errors
        var exceptions = new Exception[]
        {
            new NotFoundException("Order"),
            new ConflictException("Duplicate"),
            new ForbiddenException("Access denied"),
            new ValidationFailedException("Test", [new ValidationError("x", "y")])
        };

        foreach (var ex in exceptions)
        {
            ex.Should().BeAssignableTo<AppException>(
                because: $"{ex.GetType().Name} should extend AppException");
        }
    }
}