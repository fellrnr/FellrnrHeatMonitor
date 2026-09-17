using System.Drawing;
using System.Drawing.Drawing2D;

namespace FellrnrHeatMonitor.Models;

internal sealed class GraphSeries
{
    public GraphSeries(string key, string title, Color color, DashStyle dashed, bool useSecondaryAxis = false)
    {
        Key = key;
        Title = title;
        Color = color;
        Dashed = dashed;
        UseSecondaryAxis = useSecondaryAxis;
    }

    public string Key { get; }
    public string Title { get; set; }
    public Color Color { get; }
    public DashStyle Dashed { get; }
    public bool UseSecondaryAxis { get; }
    public List<GraphPoint> Points { get; } = new();
}
