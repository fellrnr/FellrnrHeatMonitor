namespace FellrnrHeatMonitor.Models;

public sealed class RestReadingSnapshot
{
    public double? AverageTemperatureC { get; init; }
    public double? AverageHumidityPercent { get; init; }
    public DateTimeOffset? LastReadingTime { get; init; }
}
