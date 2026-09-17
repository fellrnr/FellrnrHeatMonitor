namespace FellrnrHeatMonitor.Models;

internal sealed class TemperatureBinsModel
{
    private readonly List<TemperatureBin> _bins = new();
    private double? _lastTemperature;
    private DateTimeOffset? _lastTime;

    public TemperatureBinsModel()
    {
        InitializeBins();
    }

    public IReadOnlyList<TemperatureBin> Bins => _bins.AsReadOnly();

    private void InitializeBins()
    {
        _bins.Clear();
        _bins.Add(new TemperatureBin("Up to 38°C", null, 38.0));
        _bins.Add(new TemperatureBin("38-40°C", 38.0, 40.0));
        _bins.Add(new TemperatureBin("40-42°C", 40.0, 42.0));
        _bins.Add(new TemperatureBin("42-44°C", 42.0, 44.0));
        _bins.Add(new TemperatureBin("44-46°C", 44.0, 46.0));
        _bins.Add(new TemperatureBin("46-48°C", 46.0, 48.0));
        _bins.Add(new TemperatureBin("48-50°C", 48.0, 50.0));
        _bins.Add(new TemperatureBin("50-52°C", 50.0, 52.0));
        _bins.Add(new TemperatureBin("Over 52°C", 52.0, null));
    }

    public void Update(double temperature, DateTimeOffset now)
    {
        if (_lastTime.HasValue && _lastTemperature.HasValue)
        {
            var elapsed = now - _lastTime.Value;
            var bin = _bins.FirstOrDefault(b => b.IsInRange(_lastTemperature.Value));
            if (bin != null)
            {
                bin.CumulativeTime += elapsed;
            }
        }

        _lastTemperature = temperature;
        _lastTime = now;
    }

    public void Reset()
    {
        foreach (var bin in _bins)
        {
            bin.CumulativeTime = TimeSpan.Zero;
        }
        _lastTemperature = null;
        _lastTime = null;
    }
}