namespace FellrnrHeatMonitor.Models;

internal sealed class UsbReading
{
    public DateTimeOffset Time { get; init; }
    public int SensorIndex { get; init; }
    public int Channel { get; init; }
    public string SensorName { get; init; } = string.Empty;
    public double TemperatureC { get; init; }
}
