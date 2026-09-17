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

    public static Color GetTemperatureColor(double temperature)
    {
        Color green = Color.Green;
        Color orange = Color.Orange;
        Color red = Color.Red;
        Color purple = Color.Purple;

        if (temperature <= 30)
            return green;

        if (temperature <= 40)
            return Lerp(green, orange, (temperature - 30) / 10.0);

        if (temperature <= 45)
            return Lerp(orange, red, (temperature - 40) / 5.0);

        if (temperature <= 50)
            return Lerp(red, purple, (temperature - 45) / 5.0);

        return purple;
    }

    private static Color Lerp(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);

        int r = (int)Math.Round(a.R + (b.R - a.R) * t);
        int g = (int)Math.Round(a.G + (b.G - a.G) * t);
        int bl = (int)Math.Round(a.B + (b.B - a.B) * t);

        return Color.FromArgb(r, g, bl);
    }



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
