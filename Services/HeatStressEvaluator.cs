using FellrnrHeatMonitor.Models;

namespace FellrnrHeatMonitor.Services;

internal static class HeatStressEvaluator
{
    public static HeatStressResult Calculate(double temperatureC, double humidityPercent, AppConfig config)
    {
        var calculator = new FellrnrHeatCalculator(
            temperatureC,
            Math.Clamp(humidityPercent, 0.0, 100.0),
            Math.Max(0.1, config.HeatAirVelocityMetersPerSecond),
            Math.Max(0.0, config.HeatCyclingPowerWatts),
            Math.Max(50.0, config.HeatHeightCm),
            Math.Max(20.0, config.HeatWeightKg),
            config.HeatTemperatureUnits,
            config.HeatSodiumLosses);

        return new HeatStressResult
        {
            Message = calculator.ResultMessage,
            FeelsLikeTempC = calculator.FeelsLikeTemp,
            DisplayColor = ColorUtilities.FromHtmlOrDefault(calculator.TempColor, System.Drawing.Color.Black),
            SodiumMessage = calculator.SodiumMessage
        };
    }
}
