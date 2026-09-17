using System.Linq;

namespace FellrnrHeatMonitor.Models;

internal sealed class UsbReading
{
    public DateTimeOffset Time { get; init; }
    public int SensorIndex { get; init; }
    public int Channel { get; init; }
    public string SensorName { get; init; } = string.Empty;
    public double TemperatureC { get; init; }

    public static UsbReading? Average(params UsbReading?[] readings)
    {
        if (readings == null || readings.Length == 0)
            return null;

        var validReadings = readings.Where(r => r != null).ToList();
        if (validReadings.Count == 0)
            return null;

        double avgTemp = validReadings.Average(r => r!.TemperatureC);

        // Use the timestamp of the most recent reading
        var mostRecentTime = validReadings.Max(r => r!.Time);

        return new UsbReading
        {
            // leave identifying fields blank/default as requested
            Time = mostRecentTime,
            SensorIndex = -1,
            SensorName = string.Empty,

            // averaged values
            TemperatureC = avgTemp,
        };
    }

}
