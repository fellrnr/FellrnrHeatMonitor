using System.Linq;

namespace FellrnrHeatMonitor.Models;

using System;
using System.Collections.Generic;
using System.Linq;

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

        var validReadings = readings.Where(r => r != null).Select(r => r!).ToList();
        if (validReadings.Count == 0)
            return null;

        // If only one valid reading, return it directly
        if (validReadings.Count == 1)
        {
            return new UsbReading
            {
                Time = validReadings[0].Time,
                SensorIndex = -1,
                Channel = validReadings[0].Channel,
                SensorName = string.Empty,
                TemperatureC = validReadings[0].TemperatureC
            };
        }

        // Step 1: Find the median temperature as a robust anchor point
        var sortedTemps = validReadings.Select(r => r.TemperatureC).OrderBy(t => t).ToList();
        double medianTemp;
        int count = sortedTemps.Count;
        if (count % 2 == 0)
        {
            medianTemp = (sortedTemps[count / 2 - 1] + sortedTemps[count / 2]) / 2.0;
        }
        else
        {
            medianTemp = sortedTemps[count / 2];
        }

        // Step 2: Compute distance weights using Gaussian attenuation
        // sigma represents your expected normal temperature fluctuation range.
        // Adjust sigma depending on your sensor precision and environment.
        double sigma = 2.0;
        double weightedSum = 0;
        double totalWeight = 0;

        foreach (var r in validReadings)
        {
            double deviation = Math.Abs(r.TemperatureC - medianTemp);

            // Gaussian decay formula: exp(-d^2 / (2 * sigma^2))
            double weight = Math.Exp(-(deviation * deviation) / (2.0 * sigma * sigma));

            weightedSum += r.TemperatureC * weight;
            totalWeight += weight;
        }

        // Fallback to median if weights somehow sum to zero
        double finalTemp = totalWeight > 0 ? weightedSum / totalWeight : medianTemp;

        // Use the timestamp of the most recent reading
        var mostRecentTime = validReadings.Max(r => r.Time);
        int channel = validReadings.First().Channel;

        return new UsbReading
        {
            Time = mostRecentTime,
            SensorIndex = -1,
            Channel = channel,
            SensorName = string.Empty,
            TemperatureC = finalTemp
        };
    }
}
