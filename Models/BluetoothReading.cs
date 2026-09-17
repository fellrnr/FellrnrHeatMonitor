using System;
using System.Linq;

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

    public static BluetoothReading? Average(params BluetoothReading?[] readings)
    {
        if (readings == null || readings.Length == 0)
            return null;

        var validReadings = readings.Where(r => r != null).ToList();
        if (validReadings.Count == 0)
            return null;

        double avgTemp = validReadings.Average(r => r!.TemperatureC);
        double avgHumidity = validReadings.Average(r => r!.HumidityPercent);
        double avgDewPoint = validReadings.Average(r => r!.DewPointC);

        // Use the timestamp of the most recent reading
        var mostRecentTime = validReadings.Max(r => r!.Time);

        return new BluetoothReading
        {
            // leave identifying fields blank/default as requested
            Time = mostRecentTime,
            SensorIndex = -1,
            SensorName = "Average",
            MacAddress = string.Empty,

            // averaged values
            TemperatureC = avgTemp,
            HumidityPercent = (int)Math.Round(avgHumidity),

            DewPointC = avgDewPoint,

            // other fields left blank/default
            BatteryPercent = null,
            Rssi = 0
        };
    }
}
