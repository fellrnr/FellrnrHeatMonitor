using System.Drawing;

namespace FellrnrHeatMonitor.Services;

internal static class ColorUtilities
{
    public static readonly Color[] SensorPalette =
    {
        Color.FromArgb(231, 76, 60),
        Color.FromArgb(243, 156, 18),
        Color.FromArgb(39, 174, 96),
        Color.FromArgb(52, 152, 219)
    };

    public static Color FromHtmlOrDefault(string? html, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return fallback;
        }

        try
        {
            return ColorTranslator.FromHtml(html);
        }
        catch
        {
            return fallback;
        }
    }

    public static Color ReadableTextColor(Color background)
    {
        var brightness = background.R * 0.299 + background.G * 0.587 + background.B * 0.114;
        return brightness > 150 ? Color.Black : Color.White;
    }

    public static Color TemperatureBackColor(double temperatureC)
    {
        const double cool = 20.0;
        const double hot = 50.0;
        var ratio = Math.Clamp((temperatureC - cool) / (hot - cool), 0.0, 1.0);
        var r = (int)Math.Round(80 + ratio * 175);
        var g = (int)Math.Round(180 - ratio * 110);
        var b = (int)Math.Round(90 - ratio * 90);
        return Color.FromArgb(r, Math.Max(0, g), Math.Max(0, b));
    }
}
