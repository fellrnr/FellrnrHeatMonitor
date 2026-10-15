namespace FellrnrHeatMonitor.Models;

internal sealed class Bin
{
    public Bin(double? lowerBound, double? upperBound, string unit)
    {
        LowerBound = lowerBound;
        UpperBound = upperBound;
        Unit = unit;
        Label = GenerateLabel();
    }

    public string Label { get; }
    public double? LowerBound { get; }
    public double? UpperBound { get; }
    public string Unit { get; }
    public TimeSpan CumulativeTime { get; set; }

    private string GenerateLabel()
    {
        if (!LowerBound.HasValue && UpperBound.HasValue)
        {
            return $"< {UpperBound:F0}{Unit}";
        }

        if (LowerBound.HasValue && !UpperBound.HasValue)
        {
            return $"> {LowerBound:F0}{Unit}";
        }

        if (LowerBound.HasValue && UpperBound.HasValue)
        {
            return $"{LowerBound:F0}-{UpperBound:F0}{Unit}";
        }

        return "All";
    }

    public bool IsInRange(double value)
    {
        if (LowerBound.HasValue && value < LowerBound.Value)
            return false;
        if (UpperBound.HasValue && value >= UpperBound.Value)
            return false;
        return true;
    }
}
