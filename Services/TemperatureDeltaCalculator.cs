namespace FellrnrHeatMonitor.Services;

internal sealed class TemperatureDeltaCalculator
{
    private readonly object _sync = new();
    private readonly List<(DateTimeOffset Time, double TempC)> _readings = new();

    public TemperatureDeltaCalculator(TimeSpan? retention = null)
    {
        Retention = retention ?? TimeSpan.FromMinutes(10);
    }

    public TimeSpan Retention { get; }

    public void AddReading(double tempC, DateTimeOffset time)
    {
        lock (_sync)
        {
            if (_readings.Count == 0 || time >= _readings[^1].Time)
            {
                _readings.Add((time, tempC));
            }
            else
            {
                var idx = _readings.BinarySearch((time, tempC),
                    Comparer<(DateTimeOffset Time, double TempC)>.Create((a, b) => a.Time.CompareTo(b.Time)));
                if (idx < 0)
                {
                    idx = ~idx;
                }

                _readings.Insert(idx, (time, tempC));
            }

            TrimOld(time);
        }
    }

    public double? GetDelta(TimeSpan period, DateTimeOffset? now = null)
    {
        lock (_sync)
        {
            if (_readings.Count == 0)
            {
                return null;
            }

            var effectiveNow = now ?? _readings[^1].Time;
            var target = effectiveNow - period;
            TrimOld(effectiveNow);

            var tempNow = InterpolateAt(effectiveNow);
            var tempThen = InterpolateAt(target);
            if (tempNow is null || tempThen is null)
            {
                return null;
            }

            return tempNow.Value - tempThen.Value;
        }
    }

    public double? GetDeltaSeconds(int seconds, DateTimeOffset? now = null)
    {
        return GetDelta(TimeSpan.FromSeconds(seconds), now);
    }

    public void Clear()
    {
        lock (_sync)
        {
            _readings.Clear();
        }
    }

    private double? InterpolateAt(DateTimeOffset t)
    {
        if (_readings.Count == 0)
        {
            return null;
        }

        var first = _readings[0];
        var last = _readings[^1];
        if (t <= first.Time)
        {
            return first.TempC;
        }

        if (t >= last.Time)
        {
            return last.TempC;
        }

        var idx = _readings.BinarySearch((t, 0.0),
            Comparer<(DateTimeOffset Time, double TempC)>.Create((a, b) => a.Time.CompareTo(b.Time)));
        if (idx >= 0)
        {
            return _readings[idx].TempC;
        }

        idx = ~idx;
        var after = _readings[idx];
        var before = _readings[idx - 1];
        var span = (after.Time - before.Time).TotalSeconds;
        if (span <= 0)
        {
            return before.TempC;
        }

        var ratio = (t - before.Time).TotalSeconds / span;
        return before.TempC + (after.TempC - before.TempC) * ratio;
    }

    private void TrimOld(DateTimeOffset readingsNow)
    {
        var cutoff = readingsNow - Retention;
        var removeCount = 0;
        while (removeCount < _readings.Count && _readings[removeCount].Time < cutoff)
        {
            removeCount++;
        }

        if (removeCount > 0)
        {
            _readings.RemoveRange(0, removeCount);
        }
    }
}
