using FellrnrHeatMonitor.Services;

namespace FellrnrHeatMonitor.Models;

public sealed class AppConfig
{
    public bool TestMode { get; set; } = true;
    public bool AutoStart { get; set; } = true;
    public string UsbComPort { get; set; } = "AUTO";
    public int UsbPollIntervalMs { get; set; } = 500;
    public int GraphHistoryMinutes { get; set; } = 60;
    public int GraphSampleIntervalSeconds { get; set; } = 5;

    public double HeatAirVelocityMetersPerSecond { get; set; } = 1.5;
    public double HeatCyclingPowerWatts { get; set; } = 200.0;
    public double HeatHeightCm { get; set; } = 180.0;
    public double HeatWeightKg { get; set; } = 75.0;
    public string HeatTemperatureUnits { get; set; } = "Centigrade";
    public FellrnrHeatCalculator.SodiumLosses HeatSodiumLosses { get; set; } = FellrnrHeatCalculator.SodiumLosses.None;

    public List<SensorPairConfig> SensorPairs { get; set; } = CreateDefaultPairs();

    public AppConfig Clone()
    {
        return new AppConfig
        {
            TestMode = TestMode,
            AutoStart = AutoStart,
            UsbComPort = UsbComPort,
            UsbPollIntervalMs = UsbPollIntervalMs,
            GraphHistoryMinutes = GraphHistoryMinutes,
            GraphSampleIntervalSeconds = GraphSampleIntervalSeconds,
            HeatAirVelocityMetersPerSecond = HeatAirVelocityMetersPerSecond,
            HeatCyclingPowerWatts = HeatCyclingPowerWatts,
            HeatHeightCm = HeatHeightCm,
            HeatWeightKg = HeatWeightKg,
            HeatTemperatureUnits = HeatTemperatureUnits,
            HeatSodiumLosses = HeatSodiumLosses,
            SensorPairs = SensorPairs.Select(p => p.Clone()).ToList()
        };
    }

    public void EnsureFourPairs()
    {
        SensorPairs ??= new List<SensorPairConfig>();

        for (var i = 1; i <= 4; i++)
        {
            if (SensorPairs.All(p => p.Index != i))
            {
                SensorPairs.Add(new SensorPairConfig
                {
                    Index = i,
                    UsbChannel = i,
                    Name = $"Sensor {i}",
                    Enabled = true
                });
            }
        }

        foreach (var pair in SensorPairs)
        {
            if (pair.Index < 1 || pair.Index > 4)
            {
                pair.Index = Math.Clamp(pair.Index, 1, 4);
            }

            if (pair.UsbChannel < 1 || pair.UsbChannel > 4)
            {
                pair.UsbChannel = pair.Index;
            }

            if (string.IsNullOrWhiteSpace(pair.Name))
            {
                pair.Name = $"Sensor {pair.Index}";
            }
        }

        SensorPairs = SensorPairs
            .GroupBy(p => p.Index)
            .Select(g => g.First())
            .OrderBy(p => p.Index)
            .Take(4)
            .ToList();
    }

    private static List<SensorPairConfig> CreateDefaultPairs()
    {
        return Enumerable.Range(1, 4)
            .Select(i => new SensorPairConfig
            {
                Index = i,
                Enabled = true,
                Name = $"Sensor {i}",
                UsbChannel = i,
                BluetoothMacAddress = string.Empty
            })
            .ToList();
    }
}
