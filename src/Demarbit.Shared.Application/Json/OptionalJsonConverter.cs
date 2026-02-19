using System.Text.Json;
using System.Text.Json.Serialization;
using Demarbit.Shared.Application.Models;

namespace Demarbit.Shared.Application.Json;

/// <summary>
/// JSON converter for <see cref="Optional{T}"/>. When a JSON property is present (even if null),
/// the value deserializes to <see cref="Optional{T}.Some"/>. When the property is absent from the
/// JSON payload, the default <see cref="Optional{T}"/> (<see cref="Optional{T}.None"/>) is used.
/// <para>
/// Register via <see cref="OptionalJsonConverterFactory"/> in your <see cref="JsonSerializerOptions"/>.
/// </para>
/// </summary>
public sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
{
    /// <summary>
    /// Reads the current token and tries to convert it to the corresponding Optional value
    /// </summary>
    /// <param name="reader">Reference to the current JSON reader</param>
    /// <param name="typeToConvert">The expected target type</param>
    /// <param name="options">Standard JSON serializer options</param>
    /// <returns></returns>
    public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // If the JSON token is null, the field was present but explicitly null.
        // This is a meaningful distinction for PATCH semantics.
        if (reader.TokenType == JsonTokenType.Null)
        {
            return Optional<T>.Some(default!);
        }

        var value = JsonSerializer.Deserialize<T>(ref reader, options);
        return Optional<T>.Some(value!);
    }

    /// <summary>
    /// Writes an optional to the current JSON output stream
    /// </summary>
    /// <param name="writer">The current JSON writer</param>
    /// <param name="value">The optional value that needs to be written</param>
    /// <param name="options">Standard JSON serializer options</param>
    public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            JsonSerializer.Serialize(writer, value.Value, options);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>
/// Factory that creates <see cref="OptionalJsonConverter{T}"/> instances for any <see cref="Optional{T}"/>.
/// Register this in your <see cref="JsonSerializerOptions.Converters"/> collection.
/// </summary>
/// <example>
/// <code>
/// builder.Services.Configure&lt;JsonOptions&gt;(options =>
///     options.JsonSerializerOptions.Converters.Add(new OptionalJsonConverterFactory()));
/// </code>
/// </example>
public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    /// <summary>
    /// Validates whether a type can be converted using the current JSON converter factory
    /// </summary>
    /// <param name="typeToConvert">The type that needs conversion</param>
    /// <returns></returns>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType
            && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);
    }

    /// <summary>
    /// Creates a converter for the specified type
    /// </summary>
    /// <param name="typeToConvert">The type that needs conversion</param>
    /// <param name="options">The standard JSON serializer options to be used</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var innerType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OptionalJsonConverter<>).MakeGenericType(innerType);
        return (JsonConverter)(Activator.CreateInstance(converterType)
            ?? throw new InvalidOperationException(
                $"Could not create OptionalJsonConverter for '{innerType.Name}'."));
    }
}
