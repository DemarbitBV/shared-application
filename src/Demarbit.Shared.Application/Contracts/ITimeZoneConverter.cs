namespace Demarbit.Shared.Application.Contracts;

/// <summary>
/// Converts between UTC and a user's local timezone.
/// <para>
/// The implementation determines how the timezone is resolved (e.g. from session context,
/// request header, or configuration). This interface only defines the conversion operations.
/// </para>
/// </summary>
public interface ITimeZoneConverter
{
    /// <summary>
    /// Converts a UTC time to the user's local time.
    /// </summary>
    /// <param name="utcTime">The UTC time to convert.</param>
    /// <param name="referenceDate">
    /// Optional reference date for DST calculations. Defaults to today (UTC).
    /// </param>
    TimeOnly ConvertFromUtc(TimeOnly utcTime, DateOnly? referenceDate = null);

    /// <summary>
    /// Converts a local time to UTC.
    /// </summary>
    /// <param name="localTime">The local time to convert.</param>
    /// <param name="referenceDate">
    /// Optional reference date for DST calculations. Defaults to today (local).
    /// </param>
    TimeOnly ConvertToUtc(TimeOnly localTime, DateOnly? referenceDate = null);

    /// <summary>
    /// Converts a UTC <see cref="DateTime"/> to a local date and time.
    /// </summary>
    (DateOnly Date, TimeOnly Time) GetLocalDateAndTime(DateTime utcDateTime);
}