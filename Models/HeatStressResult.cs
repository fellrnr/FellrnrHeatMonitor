using System.Drawing;

namespace FellrnrHeatMonitor.Models;

internal sealed class HeatStressResult
{
    public string Message { get; init; } = string.Empty;
    public double FeelsLikeTempC { get; init; }
    public Color DisplayColor { get; init; } = Color.Black;
    public string SodiumMessage { get; init; } = string.Empty;
}
