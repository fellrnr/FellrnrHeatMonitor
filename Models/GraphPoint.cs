namespace FellrnrHeatMonitor.Models;

internal readonly record struct GraphPoint(DateTimeOffset Time, double TemperatureC, int? HumidityPercent = null);
