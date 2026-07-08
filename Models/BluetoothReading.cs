namespace FellrnrHeatMonitor.Models;

internal sealed class BluetoothReading
{
    public DateTimeOffset Time { get; init; }
    public int SensorIndex { get; init; }
    public string SensorName { get; init; } = string.Empty;
    public string MacAddress { get; init; } = string.Empty;
    public double TemperatureC { get; init; }
    public int HumidityPercent { get; init; }
    public double DewPointC { get; init; }
    public int? BatteryPercent { get; init; }
    public int Rssi { get; init; }
}
