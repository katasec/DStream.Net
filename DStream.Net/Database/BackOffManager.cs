using System;

namespace DStream.Net.Database;

public class BackoffManager
{
    private readonly TimeSpan _initialInterval;
    private readonly TimeSpan _maxInterval;
    private TimeSpan _currentInterval;

    public BackoffManager(TimeSpan initialInterval, TimeSpan maxInterval)
    {
        _initialInterval = initialInterval;
        _maxInterval = maxInterval;
        _currentInterval = initialInterval;
    }

    // Gets the current interval
    public TimeSpan GetCurrentInterval()
    {
        return _currentInterval;
    }

    // Resets the interval back to the initial value
    public void Reset()
    {
        _currentInterval = _initialInterval;
    }

    // Increases the interval, up to the maximum limit
    public void Increase()
    {
        _currentInterval = TimeSpan.FromMilliseconds(Math.Min(_currentInterval.TotalMilliseconds * 2, _maxInterval.TotalMilliseconds));
    }
}
