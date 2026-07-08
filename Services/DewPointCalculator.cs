namespace FellrnrHeatMonitor.Services;

internal static class DewPointCalculator
{
    // Magnus approximation. Temperature and dew point are Celsius; RH is percent.
    public static double CalculateCelsius(double temperatureC, double relativeHumidityPercent)
    {
        var rh = Math.Clamp(relativeHumidityPercent, 0.1, 100.0);
        const double a = 17.62;
        const double b = 243.12;
        var gamma = Math.Log(rh / 100.0) + (a * temperatureC) / (b + temperatureC);
        return (b * gamma) / (a - gamma);
    }
}
