using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FellrnrHeatMonitor.Models;

namespace FellrnrHeatMonitor.Controls;

internal sealed class TemperatureGraphControl : Control
{
    private IReadOnlyList<GraphSeries> _series = Array.Empty<GraphSeries>();

    public TemperatureGraphControl()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        ForeColor = Color.Black;
        History = TimeSpan.FromMinutes(60);
    }

    public TimeSpan History { get; set; }

    public void SetSeries(IEnumerable<GraphSeries> series)
    {
        _series = series.ToList();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        var plot = new Rectangle(62, 22, Math.Max(10, Width - 220), Math.Max(10, Height - 70));
        using var axisPen = new Pen(Color.FromArgb(80, 80, 80), 1);
        using var gridPen = new Pen(Color.FromArgb(225, 225, 225), 1);
        using var textBrush = new SolidBrush(ForeColor);
        using var labelBrush = new SolidBrush(Color.DimGray);

        var now = DateTimeOffset.Now;
        var historySeconds = Math.Max(60.0, History.TotalSeconds);
        var cutoff = now.AddSeconds(-historySeconds);
        var pointsInWindow = _series
            .SelectMany(s => s.Points.Where(p => p.Time >= cutoff && p.Time <= now.AddSeconds(10)))
            .ToList();

        var minY = 20.0;
        var maxY = 45.0;
        if (pointsInWindow.Count > 0)
        {
            minY = Math.Min(minY, Math.Floor(pointsInWindow.Min(p => p.TemperatureC) - 2));
            maxY = Math.Max(maxY, Math.Ceiling(pointsInWindow.Max(p => p.TemperatureC) + 2));
        }

        if (maxY <= minY)
        {
            maxY = minY + 1;
        }

        DrawGridAndAxes(g, plot, minY, maxY, historySeconds, axisPen, gridPen, textBrush, labelBrush);
        DrawSeries(g, plot, minY, maxY, cutoff, historySeconds);
        DrawLegend(g, plot.Right + 14, plot.Top + 6);
    }

    private void DrawGridAndAxes(Graphics g, Rectangle plot, double minY, double maxY, double historySeconds, Pen axisPen, Pen gridPen, Brush textBrush, Brush labelBrush)
    {
        g.DrawRectangle(axisPen, plot);

        for (var i = 0; i <= 5; i++)
        {
            var y = plot.Bottom - (float)(i / 5.0 * plot.Height);
            g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
            var value = minY + (maxY - minY) * i / 5.0;
            g.DrawString($"{value:F0} C", Font, textBrush, 6, y - 8);
        }

        for (var i = 0; i <= 6; i++)
        {
            var x = plot.Left + (float)(i / 6.0 * plot.Width);
            g.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
            var minutesAgo = (historySeconds / 60.0) * (1.0 - i / 6.0);
            var label = i == 6 ? "now" : $"-{minutesAgo:F0}m";
            g.DrawString(label, Font, labelBrush, x - 16, plot.Bottom + 6);
        }

        using var titleFont = new Font(Font.FontFamily, Font.Size + 2, FontStyle.Bold);
        g.DrawString("USB and Bluetooth temperatures", titleFont, textBrush, plot.Left, 2);
    }

    private void DrawSeries(Graphics g, Rectangle plot, double minY, double maxY, DateTimeOffset cutoff, double historySeconds)
    {
        foreach (var series in _series)
        {
            var points = series.Points
                .Where(p => p.Time >= cutoff)
                .OrderBy(p => p.Time)
                .Select(p => Project(p, plot, minY, maxY, cutoff, historySeconds))
                .ToArray();

            if (points.Length == 0)
            {
                continue;
            }

            using var pen = new Pen(series.Color, 2.0f)
            {
                DashStyle = series.Dashed ? DashStyle.Dash : DashStyle.Solid
            };

            if (points.Length == 1)
            {
                using var pointBrush = new SolidBrush(series.Color);
                g.FillEllipse(pointBrush, points[0].X - 3, points[0].Y - 3, 6, 6);
            }
            else
            {
                g.DrawLines(pen, points);
            }
        }
    }

    private static PointF Project(GraphPoint point, Rectangle plot, double minY, double maxY, DateTimeOffset cutoff, double historySeconds)
    {
        var xRatio = Math.Clamp((point.Time - cutoff).TotalSeconds / historySeconds, 0.0, 1.0);
        var yRatio = Math.Clamp((point.TemperatureC - minY) / (maxY - minY), 0.0, 1.0);
        var x = plot.Left + (float)(xRatio * plot.Width);
        var y = plot.Bottom - (float)(yRatio * plot.Height);
        return new PointF(x, y);
    }

    private void DrawLegend(Graphics g, int x, int y)
    {
        var lineHeight = 18;
        using var textBrush = new SolidBrush(ForeColor);
        using var titleFont = new Font(Font.FontFamily, Font.Size, FontStyle.Bold);
        g.DrawString("Legend", titleFont, textBrush, x, y);
        y += 22;

        foreach (var series in _series)
        {
            using var pen = new Pen(series.Color, 2.0f)
            {
                DashStyle = series.Dashed ? DashStyle.Dash : DashStyle.Solid
            };
            g.DrawLine(pen, x, y + 8, x + 28, y + 8);
            g.DrawString(series.Title, Font, textBrush, x + 34, y);
            y += lineHeight;
        }
    }
}
