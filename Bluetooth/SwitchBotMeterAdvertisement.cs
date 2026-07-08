namespace FellrnrHeatMonitor.Bluetooth;

internal sealed class SwitchBotMeterAdvertisement
{
    public List<string> MacCandidates { get; } = new();
    public double TemperatureC { get; init; }
    public int HumidityPercent { get; init; }
    public int? BatteryPercent { get; init; }
    public int Rssi { get; init; }
    public string Source { get; init; } = string.Empty;
}
