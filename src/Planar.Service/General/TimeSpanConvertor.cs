using System;

namespace TimeSpanConverter;

/// <summary>
/// A utility class to convert TimeSpan objects to cron expression strings.
/// </summary>
public static class CronConverter
{
    /// <summary>
    /// Converts a TimeSpan to a standard 5-part cron expression.
    /// </summary>
    /// <remarks>
    /// This conversion has limitations and is designed for simple, regular intervals
    /// that divide evenly into the next largest unit (e.g., minutes into an hour, hours into a day).
    /// It makes the following assumptions:
    /// - The cron format is the standard 5-part: MINUTE HOUR DAY-OF-MONTH MONTH DAY-OF-WEEK.
    /// - The minimum precision is 1 minute. TimeSpans with seconds are not supported.
    /// - Intervals over 24 hours are not supported due to the ambiguity of "every X days" in cron.
    /// - Irregular intervals (e.g., 7 minutes, 3 hours) that don't divide evenly into 60 or 24 will be rejected.
    /// </remarks>
    /// <param name="timeSpan">The TimeSpan to convert.</param>
    /// <returns>A cron expression string.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown for TimeSpans that are zero, negative, or cannot be cleanly represented by a standard cron expression.
    /// </exception>
    public static string ToCronExpression(this TimeSpan timeSpan)
    {
        // --- Validation ---
        if (timeSpan <= TimeSpan.Zero)
        {
            throw new ArgumentException("TimeSpan must be positive.", nameof(timeSpan));
        }

        if (timeSpan.Seconds > 0)
        {
            // Standard 5-part cron doesn't support seconds. While some schedulers use a 6-part format,
            // it's not universal. To ensure compatibility, we disallow second-level precision.
            throw new ArgumentException("Cron expressions do not support second-level precision. The minimum interval is 1 minute.", nameof(timeSpan));
        }

        // --- Conversion Logic ---

        // Case 1: The interval is in whole minutes (and less than 1 hour).
        // Example: TimeSpan.FromMinutes(15) -> "0 */15 * ? * *"
        if (timeSpan.TotalHours < 1)
        {
            if (60 % timeSpan.Minutes != 0)
            {
                throw new ArgumentException($"Cannot create a cron expression for an irregular minute interval like {timeSpan.Minutes}. The interval must divide 60 evenly.", nameof(timeSpan));
            }

            return $"0 */{timeSpan.Minutes} * ? * *";
        }

        // Case 2: The interval is in whole hours (and less than 1 day).
        // Example: TimeSpan.FromHours(6) -> "0 0 */6 ? * *"
        if (timeSpan.TotalDays < 1)
        {
            if (timeSpan.Minutes > 0)
            {
                throw new ArgumentException("Cannot create a cron expression for an interval with both hours and minutes (e.g., 1.5 hours). Please use whole hours.", nameof(timeSpan));
            }
            if (24 % timeSpan.Hours != 0)
            {
                throw new ArgumentException($"Cannot create a cron expression for an irregular hour interval like {timeSpan.Hours}. The interval must divide 24 evenly.", nameof(timeSpan));
            }

            // "At minute 0, every Xth hour."
            return $"0 0 */{timeSpan.Hours} ? * *";
        }

        // Case 3: The interval is exactly one day.
        // Example: TimeSpan.FromDays(1) -> "0 0 * * *"
        if (Convert.ToInt32(timeSpan.TotalDays) == 1 && timeSpan.Hours == 0 && timeSpan.Minutes == 0)
        {
            // "At minute 0 of hour 0, every day." (i.e., at midnight)
            return "0 0 0 * * *";
        }

        // --- Catch-all for unsupported intervals ---
        throw new ArgumentException($"The TimeSpan of '{timeSpan}' is too complex or long to be represented as a standard cron expression. This converter only supports regular intervals of minutes, hours, or a single day.", nameof(timeSpan));
    }
}