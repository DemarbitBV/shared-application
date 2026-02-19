namespace Demarbit.Shared.Application.Models;

/// <summary>
/// Represents a value that may or may not have been provided in a request.
/// Used for PATCH semantics to distinguish between "field was not included" (<see cref="None"/>)
/// and "field was explicitly set to null" (<see cref="Some"/> with a <c>null</c> value).
/// <para>
/// In JSON deserialization, absent properties produce <see cref="None"/> and present properties
/// (including <c>null</c>) produce <see cref="Some"/>. Use the <c>OptionalJsonConverterFactory</c>
/// to enable this behavior.
/// </para>
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public readonly struct Optional<T>
{
    private readonly T _value;

    /// <summary>
    /// Whether a value was provided (even if the value itself is <c>null</c>).
    /// </summary>
    public bool HasValue { get; }

    /// <summary>
    /// The provided value. Throws if <see cref="HasValue"/> is <c>false</c>.
    /// </summary>
    public T Value => HasValue
        ? _value
        : throw new InvalidOperationException("Optional has no value. Check HasValue before accessing Value.");

    private Optional(T value, bool hasValue)
    {
        _value = value;
        HasValue = hasValue;
    }

    /// <summary>
    /// Creates an Optional that indicates the value was provided.
    /// </summary>
    public static Optional<T> Some(T value) => new(value, true);

    /// <summary>
    /// Creates an Optional that indicates no value was provided (field absent from request).
    /// </summary>
    public static Optional<T> None() => new(default!, false);

    /// <summary>
    /// Returns the value if provided, otherwise the default.
    /// </summary>
    public T GetValueOrDefault(T defaultValue = default!) => HasValue ? _value : defaultValue;

    /// <summary>
    /// Applies the value to the target if a value was provided.
    /// Useful for PATCH update patterns:
    /// <code>
    /// command.Name.Apply(value => entity.UpdateName(value));
    /// </code>
    /// </summary>
    public void Apply(Action<T> action)
    {
        if (HasValue)
        {
            action(_value);
        }
    }
}
