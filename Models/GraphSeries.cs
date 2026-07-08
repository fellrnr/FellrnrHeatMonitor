using System.Drawing;

namespace FellrnrHeatMonitor.Models;

internal sealed class GraphSeries
{
    public GraphSeries(string key, string title, Color color, bool dashed)
    {
        Key = key;
        Title = title;
        Color = color;
        Dashed = dashed;
    }

    public string Key { get; }
    public string Title { get; set; }
    public Color Color { get; }
    public bool Dashed { get; }
    public List<GraphPoint> Points { get; } = new();
}
