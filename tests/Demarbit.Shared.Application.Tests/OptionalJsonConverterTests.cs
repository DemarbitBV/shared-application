using System.Text.Json;
using System.Text.Json.Serialization;
using Demarbit.Shared.Application.Json;
using Demarbit.Shared.Application.Models;
using FluentAssertions;

namespace Demarbit.Shared.Application.Tests;

public class OptionalJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new OptionalJsonConverterFactory() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    #region Deserialization — property present with value

    [Fact]
    public void Deserialize_should_produce_some_when_string_property_is_present()
    {
        var json = """{ "name": "Alice" }""";

        var result = JsonSerializer.Deserialize<PatchPerson>(json, Options)!;

        result.Name.HasValue.Should().BeTrue();
        result.Name.Value.Should().Be("Alice");
    }

    [Fact]
    public void Deserialize_should_produce_some_when_int_property_is_present()
    {
        var json = """{ "age": 30 }""";

        var result = JsonSerializer.Deserialize<PatchPerson>(json, Options)!;

        result.Age.HasValue.Should().BeTrue();
        result.Age.Value.Should().Be(30);
    }

    [Fact]
    public void Deserialize_should_produce_some_when_bool_property_is_present()
    {
        var json = """{ "active": true }""";

        var result = JsonSerializer.Deserialize<PatchPerson>(json, Options)!;

        result.Active.HasValue.Should().BeTrue();
        result.Active.Value.Should().BeTrue();
    }

    [Fact]
    public void Deserialize_should_produce_some_when_nested_object_is_present()
    {
        var json = """{ "address": { "city": "Brussels" } }""";

        var result = JsonSerializer.Deserialize<PatchPerson>(json, Options)!;

        result.Address.HasValue.Should().BeTrue();
        result.Address.Value!.City.Should().Be("Brussels");
    }

    [Fact]
    public void Deserialize_should_produce_some_when_enum_property_is_present()
    {
        var json = """{ "status": 1 }""";

        var result = JsonSerializer.Deserialize<PatchPerson>(json, Options)!;

        result.Status.HasValue.Should().BeTrue();
        result.Status.Value.Should().Be(PersonStatus.Active);
    }

    #endregion

    #region Deserialization — property explicitly null (PATCH: "set to null")

    [Fact]
    public void Deserialize_should_produce_some_with_null_when_string_property_is_null()
    {
        var json = """{ "name": null }""";

        var result = JsonSerializer.Deserialize<PatchPerson>(json, Options)!;

        result.Name.HasValue.Should().BeTrue();
        result.Name.Value.Should().BeNull();
    }

    [Fact]
    public void Deserialize_should_produce_some_with_default_when_int_property_is_null()
    {
        var json = """{ "age": null }""";

        var result = JsonSerializer.Deserialize<PatchPerson>(json, Options)!;

        result.Age.HasValue.Should().BeTrue();
        result.Age.Value.Should().Be(default(int));
    }

    [Fact]
    public void Deserialize_should_produce_some_with_null_when_nested_object_is_null()
    {
        var json = """{ "address": null }""";

        var result = JsonSerializer.Deserialize<PatchPerson>(json, Options)!;

        result.Address.HasValue.Should().BeTrue();
        result.Address.Value.Should().BeNull();
    }

    #endregion

    #region Deserialization — property absent (PATCH: "not provided")

    [Fact]
    public void Deserialize_should_produce_none_when_property_is_absent()
    {
        var json = """{ }""";

        var result = JsonSerializer.Deserialize<PatchPerson>(json, Options)!;

        result.Name.HasValue.Should().BeFalse();
        result.Age.HasValue.Should().BeFalse();
        result.Active.HasValue.Should().BeFalse();
        result.Address.HasValue.Should().BeFalse();
        result.Status.HasValue.Should().BeFalse();
    }

    [Fact]
    public void Deserialize_should_distinguish_absent_from_null()
    {
        var json = """{ "name": null }""";

        var result = JsonSerializer.Deserialize<PatchPerson>(json, Options)!;

        // name was explicitly set to null → Some(null)
        result.Name.HasValue.Should().BeTrue();
        result.Name.Value.Should().BeNull();

        // age was not included → None
        result.Age.HasValue.Should().BeFalse();
    }

    [Fact]
    public void Deserialize_should_handle_partial_payload()
    {
        var json = """{ "name": "Bob", "active": false }""";

        var result = JsonSerializer.Deserialize<PatchPerson>(json, Options)!;

        result.Name.HasValue.Should().BeTrue();
        result.Name.Value.Should().Be("Bob");
        result.Active.HasValue.Should().BeTrue();
        result.Active.Value.Should().BeFalse();

        // Absent properties remain None
        result.Age.HasValue.Should().BeFalse();
        result.Address.HasValue.Should().BeFalse();
        result.Status.HasValue.Should().BeFalse();
    }

    #endregion

    #region Serialization

    [Fact]
    public void Serialize_should_write_value_when_some()
    {
        var model = new PatchPerson { Name = Optional<string?>.Some("Alice") };

        var json = JsonSerializer.Serialize(model, Options);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("name").GetString().Should().Be("Alice");
    }

    [Fact]
    public void Serialize_should_write_null_when_some_with_null()
    {
        var model = new PatchPerson { Name = Optional<string?>.Some(null) };

        var json = JsonSerializer.Serialize(model, Options);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("name").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public void Serialize_should_write_null_when_none()
    {
        var model = new PatchPerson { Name = Optional<string?>.None() };

        var json = JsonSerializer.Serialize(model, Options);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("name").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public void Serialize_should_write_nested_object_when_some()
    {
        var model = new PatchPerson
        {
            Address = Optional<Address?>.Some(new Address { City = "Ghent" })
        };

        var json = JsonSerializer.Serialize(model, Options);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("address").GetProperty("city").GetString().Should().Be("Ghent");
    }

    [Fact]
    public void Serialize_should_write_int_value_when_some()
    {
        var model = new PatchPerson { Age = Optional<int>.Some(25) };

        var json = JsonSerializer.Serialize(model, Options);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("age").GetInt32().Should().Be(25);
    }

    #endregion

    #region Round-trip

    [Fact]
    public void Roundtrip_should_preserve_some_value()
    {
        var original = new PatchPerson
        {
            Name = Optional<string?>.Some("Alice"),
            Age = Optional<int>.Some(30),
            Active = Optional<bool>.Some(true)
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PatchPerson>(json, Options)!;

        restored.Name.HasValue.Should().BeTrue();
        restored.Name.Value.Should().Be("Alice");
        restored.Age.HasValue.Should().BeTrue();
        restored.Age.Value.Should().Be(30);
        restored.Active.HasValue.Should().BeTrue();
        restored.Active.Value.Should().BeTrue();
    }

    [Fact]
    public void Roundtrip_should_preserve_some_null()
    {
        var original = new PatchPerson { Name = Optional<string?>.Some(null) };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PatchPerson>(json, Options)!;

        // null was explicitly provided → Some(null) survives round-trip
        restored.Name.HasValue.Should().BeTrue();
        restored.Name.Value.Should().BeNull();
    }

    #endregion

    #region OptionalJsonConverterFactory

    [Fact]
    public void Factory_CanConvert_should_return_true_for_optional_type()
    {
        var factory = new OptionalJsonConverterFactory();

        factory.CanConvert(typeof(Optional<string>)).Should().BeTrue();
    }

    [Fact]
    public void Factory_CanConvert_should_return_true_for_optional_of_value_type()
    {
        var factory = new OptionalJsonConverterFactory();

        factory.CanConvert(typeof(Optional<int>)).Should().BeTrue();
    }

    [Fact]
    public void Factory_CanConvert_should_return_false_for_non_optional_type()
    {
        var factory = new OptionalJsonConverterFactory();

        factory.CanConvert(typeof(string)).Should().BeFalse();
        factory.CanConvert(typeof(int)).Should().BeFalse();
        factory.CanConvert(typeof(List<string>)).Should().BeFalse();
    }

    [Fact]
    public void Factory_CanConvert_should_return_false_for_non_generic_type()
    {
        var factory = new OptionalJsonConverterFactory();

        factory.CanConvert(typeof(object)).Should().BeFalse();
    }

    [Fact]
    public void Factory_CreateConverter_should_return_typed_converter()
    {
        var factory = new OptionalJsonConverterFactory();

        var converter = factory.CreateConverter(typeof(Optional<string>), Options);

        converter.Should().BeOfType<OptionalJsonConverter<string>>();
    }

    [Fact]
    public void Factory_CreateConverter_should_return_converter_for_value_type()
    {
        var factory = new OptionalJsonConverterFactory();

        var converter = factory.CreateConverter(typeof(Optional<int>), Options);

        converter.Should().BeOfType<OptionalJsonConverter<int>>();
    }

    #endregion

    #region Test models

    private sealed class PatchPerson
    {
        public Optional<string?> Name { get; set; }
        public Optional<int> Age { get; set; }
        public Optional<bool> Active { get; set; }
        public Optional<Address?> Address { get; set; }
        public Optional<PersonStatus> Status { get; set; }
    }

    private sealed class Address
    {
        public string City { get; set; } = string.Empty;
    }

    private enum PersonStatus
    {
        Inactive = 0,
        Active = 1
    }

    #endregion
}
