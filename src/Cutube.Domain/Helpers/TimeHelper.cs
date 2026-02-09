using System.Text.RegularExpressions;

namespace Cutube.Domain.Helpers;

/// <summary>
/// Helper for parsing time strings into seconds
/// Supports formats like: "1:30", "90s", "1h30m", "1:30:45"
/// </summary>
public static class TimeHelper
{
    /// <summary>
    /// Parses a time string to seconds
    /// </summary>
    /// <param name="input">Time string to parse</param>
    /// <returns>Time in seconds</returns>
    /// <exception cref="ArgumentException">Thrown when input is empty</exception>
    /// <exception cref="FormatException">Thrown when format is not recognized</exception>
    public static int ParseToSeconds(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Tempo não pode ser vazio");

        input = input.Trim().ToLower();

        if (TryParseCompactNotation(input, out var seconds))
            return seconds;

        if (TryParseColonFormat(input, out seconds))
            return seconds;

        if (double.TryParse(input, out var totalSeconds))
            return (int)totalSeconds;

        throw new FormatException($"Formato de tempo não reconhecido: {input}");
    }

    private static bool TryParseCompactNotation(string input, out int seconds)
    {
        seconds = 0;

        if (TryMatchHoursMinutesSeconds(input, out seconds))
            return true;

        if (TryMatchHoursMinutes(input, out seconds))
            return true;

        if (TryMatchHoursSeconds(input, out seconds))
            return true;

        if (TryMatchMinutesSeconds(input, out seconds))
            return true;

        if (TryMatchOnlyHours(input, out seconds))
            return true;

        if (TryMatchOnlyMinutes(input, out seconds))
            return true;

        if (TryMatchOnlySeconds(input, out seconds))
            return true;

        return false;
    }

    private static bool TryMatchHoursMinutesSeconds(string input, out int seconds)
    {
        seconds = 0;
        var match = Regex.Match(input, @"^(?<hours>\d+)h(?<minutes>\d+)m(?<secs>\d+(?:\.\d+)?)s$", RegexOptions.IgnoreCase);
        if (!match.Success) return false;

        var total = double.Parse(match.Groups["hours"].Value) * 3600;
        total += double.Parse(match.Groups["minutes"].Value) * 60;
        total += double.Parse(match.Groups["secs"].Value);
        seconds = (int)total;
        return true;
    }

    private static bool TryMatchHoursMinutes(string input, out int seconds)
    {
        seconds = 0;
        var match = Regex.Match(input, @"^(?<hours>\d+)h(?<minutes>\d+)m$", RegexOptions.IgnoreCase);
        if (!match.Success) return false;

        var total = double.Parse(match.Groups["hours"].Value) * 3600;
        total += double.Parse(match.Groups["minutes"].Value) * 60;
        seconds = (int)total;
        return true;
    }

    private static bool TryMatchHoursSeconds(string input, out int seconds)
    {
        seconds = 0;
        var match = Regex.Match(input, @"^(?<hours>\d+)h(?<secs>\d+(?:\.\d+)?)s$", RegexOptions.IgnoreCase);
        if (!match.Success) return false;

        var total = double.Parse(match.Groups["hours"].Value) * 3600;
        total += double.Parse(match.Groups["secs"].Value);
        seconds = (int)total;
        return true;
    }

    private static bool TryMatchMinutesSeconds(string input, out int seconds)
    {
        seconds = 0;
        var match = Regex.Match(input, @"^(?<minutes>\d+)m(?<secs>\d+(?:\.\d+)?)s$", RegexOptions.IgnoreCase);
        if (!match.Success) return false;

        var total = double.Parse(match.Groups["minutes"].Value) * 60;
        total += double.Parse(match.Groups["secs"].Value);
        seconds = (int)total;
        return true;
    }

    private static bool TryMatchOnlyHours(string input, out int seconds)
    {
        seconds = 0;
        var match = Regex.Match(input, @"^(?<hours>\d+)h$", RegexOptions.IgnoreCase);
        if (!match.Success) return false;

        seconds = (int)(double.Parse(match.Groups["hours"].Value) * 3600);
        return true;
    }

    private static bool TryMatchOnlyMinutes(string input, out int seconds)
    {
        seconds = 0;
        var match = Regex.Match(input, @"^(?<minutes>\d+)m$", RegexOptions.IgnoreCase);
        if (!match.Success) return false;

        seconds = (int)(double.Parse(match.Groups["minutes"].Value) * 60);
        return true;
    }

    private static bool TryMatchOnlySeconds(string input, out int seconds)
    {
        seconds = 0;
        var match = Regex.Match(input, @"^(?<secs>\d+(?:\.\d+)?)s$", RegexOptions.IgnoreCase);
        if (!match.Success) return false;

        seconds = (int)double.Parse(match.Groups["secs"].Value);
        return true;
    }

    private static bool TryParseColonFormat(string input, out int seconds)
    {
        seconds = 0;
        var parts = input.Split(':');

        if (parts.Length == 2)
        {
            if (int.TryParse(parts[0], out var minutes) &&
                double.TryParse(parts[1], out var secs))
            {
                seconds = minutes * 60 + (int)secs;
                return true;
            }
        }
        else if (parts.Length == 3)
        {
            if (int.TryParse(parts[0], out var hours) &&
                int.TryParse(parts[1], out var minutes) &&
                double.TryParse(parts[2], out var secs))
            {
                seconds = hours * 3600 + minutes * 60 + (int)secs;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Converts a time string to TimeSpan
    /// </summary>
    public static TimeSpan ParseTimeToTimeSpan(string input)
    {
        return TimeSpan.FromSeconds(ParseToSeconds(input));
    }
}
