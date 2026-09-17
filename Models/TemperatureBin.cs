namespace FellrnrHeatMonitor.Models;

internal sealed class TemperatureBin
{
    public TemperatureBin(string label, double? minTemp, double? maxTemp)
    {
        Label = label;
        MinTemp = minTemp;
        MaxTemp = maxTemp;
    }

    public string Label { get; }
    public double? MinTemp { get; }
    public double? MaxTemp { get; }
    public TimeSpan CumulativeTime { get; set; }

    public bool IsInRange(double temperature)
    {
        if (MinTemp.HasValue && temperature < MinTemp.Value)
            return false;
        if (MaxTemp.HasValue && temperature >= MaxTemp.Value)
            return false;
        return true;
    }
}