using System;

namespace DStream.Net.Config;

public class TableConfig
{
    public string? Name { get; set; }
    public string? PollInterval { get; set; }
    public string? MaxPollInterval { get; set; }

    // Method to get PollInterval as TimeSpan
    public TimeSpan GetPollInterval()
    {
        return ParseInterval(PollInterval, TimeSpan.FromSeconds(5)); // Default to 5 seconds if not set
    }

    // Method to get MaxPollInterval as TimeSpan
    public TimeSpan GetMaxPollInterval()
    {
        return ParseInterval(MaxPollInterval, TimeSpan.FromMinutes(1)); // Default to 1 minute if not set
    }

    // Helper method to parse a string interval into TimeSpan
    private static TimeSpan ParseInterval(string? interval, TimeSpan defaultValue)
    {
        if (string.IsNullOrWhiteSpace(interval))
            return defaultValue;

        try
        {
            // Check for suffix and parse accordingly
            if (interval.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                // Handle seconds (e.g., "5s")
                var seconds = int.Parse(interval[..^1]);
                return TimeSpan.FromSeconds(seconds);
            }
            else if (interval.EndsWith("m", StringComparison.OrdinalIgnoreCase))
            {
                // Handle minutes (e.g., "1m")
                var minutes = int.Parse(interval[..^1]);
                return TimeSpan.FromMinutes(minutes);
            }
            else if (interval.EndsWith("h", StringComparison.OrdinalIgnoreCase))
            {
                // Handle hours (e.g., "1h")
                var hours = int.Parse(interval[..^1]);
                return TimeSpan.FromHours(hours);
            }
            else
            {
                // Default to parsing as a full TimeSpan (e.g., "00:01:00")
                return TimeSpan.Parse(interval);
            }
        }
        catch (FormatException)
        {
            throw new FormatException($"Invalid interval format: {interval}");
        }
    }

}
