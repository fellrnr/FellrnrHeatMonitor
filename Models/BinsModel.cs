namespace FellrnrHeatMonitor.Models;

internal sealed class BinsModel
{
    private readonly List<Bin> _bins = new();
    private double? _lastValue;
    private DateTimeOffset? _lastTime;

    public BinsModel(string unit, double[] boundaries)
    {
        Unit = unit;
        InitializeBins(boundaries);
    }

    public string Unit { get; }
    public IReadOnlyList<Bin> Bins => _bins.AsReadOnly();

    private void InitializeBins(double[] boundaries)
    {
        _bins.Clear();

        if (boundaries.Length == 0)
        {
            return;
        }

        // Create bin for values below first boundary
        _bins.Add(new Bin(null, boundaries[0], Unit));

        // Create bins between consecutive boundaries
        for (int i = 0; i < boundaries.Length - 1; i++)
        {
            _bins.Add(new Bin(boundaries[i], boundaries[i + 1], Unit));
        }

        // Create bin for values above last boundary
        _bins.Add(new Bin(boundaries[boundaries.Length - 1], null, Unit));
    }

    public void Update(double value, DateTimeOffset now)
    {
        if (_lastTime.HasValue && _lastValue.HasValue)
        {
            var elapsed = now - _lastTime.Value;
            var bin = _bins.FirstOrDefault(b => b.IsInRange(_lastValue.Value));
            if (bin != null)
            {
                bin.CumulativeTime += elapsed;
            }
        }

        _lastValue = value;
        _lastTime = now;
    }

    public void Reset()
    {
        foreach (var bin in _bins)
        {
            bin.CumulativeTime = TimeSpan.Zero;
        }
        _lastValue = null;
        _lastTime = null;
    }
}
