using Demarbit.Shared.Application.Models;
using FluentAssertions;

namespace Demarbit.Shared.Application.Tests;

public class OptionalTests
{
    [Fact]
    public void Some_should_have_value()
    {
        var opt = Optional<string>.Some("hello");

        opt.HasValue.Should().BeTrue();
        opt.Value.Should().Be("hello");
    }

    [Fact]
    public void None_should_not_have_value()
    {
        var opt = Optional<string>.None();

        opt.HasValue.Should().BeFalse();
    }

    [Fact]
    public void None_value_access_should_throw()
    {
        var opt = Optional<string>.None();

        var act = () => opt.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Some_with_null_should_have_value()
    {
        // This is the key PATCH semantic: "field was present but set to null"
        var opt = Optional<string?>.Some(null);

        opt.HasValue.Should().BeTrue();
        opt.Value.Should().BeNull();
    }

    [Fact]
    public void GetValueOrDefault_should_return_value_when_present()
    {
        var opt = Optional<string>.Some("hello");

        opt.GetValueOrDefault("fallback").Should().Be("hello");
    }

    [Fact]
    public void GetValueOrDefault_should_return_default_when_absent()
    {
        var opt = Optional<string>.None();

        opt.GetValueOrDefault("fallback").Should().Be("fallback");
    }

    [Fact]
    public void Apply_should_execute_action_when_value_present()
    {
        var opt = Optional<string>.Some("hello");
        string? captured = null;

        opt.Apply(v => captured = v);

        captured.Should().Be("hello");
    }

    [Fact]
    public void Apply_should_not_execute_action_when_absent()
    {
        var opt = Optional<string>.None();
        var executed = false;

        opt.Apply(_ => executed = true);

        executed.Should().BeFalse();
    }

    [Fact]
    public void Default_struct_should_be_None()
    {
        // Uninitialized Optional<T> (default struct) should behave as None
        Optional<string> opt = default;

        opt.HasValue.Should().BeFalse();
    }
}
