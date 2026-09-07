using Cutube.Domain.Helpers;

namespace Cutube.Domain.Models;

/// <summary>
/// Represents a time range for video extraction
/// </summary>
public record TimeRange
{
    /// <summary>
    /// Start time in seconds (must be > 0)
    /// </summary>
    public required int StartSeconds { get; init; }

    /// <summary>
    /// End time in seconds (must be > Start)
    /// </summary>
    public required int EndSeconds { get; init; }

    /// <summary>
    /// Duration of the time range in seconds
    /// </summary>
    public int DurationSeconds => EndSeconds - StartSeconds;

    /// <summary>
    /// Creates a TimeRange from time strings (e.g., "1:30", "90s")
    /// </summary>
    /// <param name="start">Start time string</param>
    /// <param name="end">End time string</param>
    /// <returns>A new TimeRange instance</returns>
    /// <exception cref="FormatException">Thrown when time strings are invalid</exception>
    /// <exception cref="ArgumentException">Thrown when time range is invalid</exception>
    public static TimeRange FromStrings(string start, string end)
    {
        var startSec = TimeHelper.ParseToSeconds(start);
        var endSec = TimeHelper.ParseToSeconds(end);

        if (startSec <= 0)
            throw new ArgumentException("Start must be greater than 0", nameof(start));
        if (endSec <= startSec)
            throw new ArgumentException("End must be greater than start", nameof(end));

        return new TimeRange { StartSeconds = startSec, EndSeconds = endSec };
    }
}
