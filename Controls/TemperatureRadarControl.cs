using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FellrnrHeatMonitor.Models;
using System.ComponentModel;

namespace FellrnrHeatMonitor.Controls;

internal sealed class TemperatureRadarControl : Control
{
    private IReadOnlyList<TemperatureBin> _bins = Array.Empty<TemperatureBin>();
    private const int MaxRadarLevels = 5;

    public TemperatureRadarControl()
    {
        DoubleBuffered = true;
        BackColor = Color.Black;
        ForeColor = Color.White;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public void SetBins(IEnumerable<TemperatureBin> bins)
    {
        _bins = bins.ToList();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        if (_bins.Count == 0)
        {
            return;
        }

        // Calculate center and radius
        var centerX = Width / 2.0f;
        var centerY = Height / 2.0f;
        var radius = Math.Min(Width, Height) / 2.0f - 40;

        if (radius <= 0)
        {
            return;
        }

        // Find max cumulative time for scaling
        var maxTime = _bins.Max(b => b.CumulativeTime.TotalMinutes);
        if (maxTime == 0)
        {
            maxTime = 1; // Avoid division by zero
        }

        // Draw grid and axes
        using var axisPen = new Pen(Color.FromArgb(80, 80, 80), 1);
        using var gridPen = new Pen(Color.FromArgb(60, 60, 60), 1);
        using var textBrush = new SolidBrush(ForeColor);
        using var labelBrush = new SolidBrush(Color.DimGray);

        DrawRadarGrid(g, centerX, centerY, radius, maxTime, axisPen, gridPen, textBrush, labelBrush);
        DrawBins(g, centerX, centerY, radius, maxTime, textBrush);
        DrawTitle(g, textBrush);
    }

    private void DrawRadarGrid(Graphics g, float centerX, float centerY, float radius, double maxTime, 
        Pen axisPen, Pen gridPen, Brush textBrush, Brush labelBrush)
    {
        var binCount = _bins.Count;
        var angleStep = 360.0 / binCount;

        // Draw concentric circles
        for (int level = 1; level <= MaxRadarLevels; level++)
        {
            var levelRadius = (radius / MaxRadarLevels) * level;
            g.DrawEllipse(gridPen, centerX - levelRadius, centerY - levelRadius, 
                levelRadius * 2, levelRadius * 2);

            // Draw level labels
            var levelValue = (maxTime / MaxRadarLevels) * level;
            var labelText = FormatTimeLabel(levelValue);
            var textSize = g.MeasureString(labelText, Font);
            g.DrawString(labelText, Font, labelBrush, 
                centerX + levelRadius - textSize.Width / 2, centerY - textSize.Height / 2);
        }

        // Draw spokes and labels
        for (int i = 0; i < binCount; i++)
        {
            var angle = i * angleStep;
            var rad = angle * Math.PI / 180.0;
            var endX = (float)(centerX + radius * Math.Sin(rad));
            var endY = (float)(centerY - radius * Math.Cos(rad));

            g.DrawLine(axisPen, centerX, centerY, endX, endY);

            // Draw bin labels
            var labelRadius = radius + 20;
            var labelX = (float)(centerX + labelRadius * Math.Sin(rad));
            var labelY = (float)(centerY - labelRadius * Math.Cos(rad));

            var bin = _bins[i];
            var textSize = g.MeasureString(bin.Label, Font);
            g.DrawString(bin.Label, Font, textBrush, 
                labelX - textSize.Width / 2, labelY - textSize.Height / 2);
        }
    }

    private void DrawBins(Graphics g, float centerX, float centerY, float radius, double maxTime, Brush textBrush)
    {
        var binCount = _bins.Count;
        var angleStep = 360.0 / binCount;

        using var binPen = new Pen(Color.FromArgb(150, 150, 150), 2);
        using var binBrush = new SolidBrush(Color.FromArgb(100, 150, 200));

        var points = new PointF[binCount];

        for (int i = 0; i < binCount; i++)
        {
            var angle = i * angleStep;
            var rad = angle * Math.PI / 180.0;
            var value = _bins[i].CumulativeTime.TotalMinutes;
            var distance = (float)(radius * (value / maxTime));
            var x = (float)(centerX + distance * Math.Sin(rad));
            var y = (float)(centerY - distance * Math.Cos(rad));
            points[i] = new PointF(x, y);
        }

        if (points.Length > 2)
        {
            g.FillPolygon(binBrush, points);
            g.DrawPolygon(binPen, points);
        }

        // Draw points at each bin
        using var pointBrush = new SolidBrush(Color.FromArgb(200, 150, 200));
        foreach (var point in points)
        {
            g.FillEllipse(pointBrush, point.X - 4, point.Y - 4, 8, 8);
        }
    }

    private void DrawTitle(Graphics g, Brush textBrush)
    {
        using var titleFont = new Font(Font.FontFamily, Font.Size + 2, FontStyle.Bold);
        g.DrawString("Temperature Distribution", titleFont, textBrush, 10, 10);
    }

    private static string FormatTimeLabel(double minutes)
    {
        if (minutes < 60)
        {
            return $"{minutes:F0}m";
        }

        var hours = minutes / 60.0;
        if (hours < 24)
        {
            return $"{hours:F1}h";
        }

        var days = hours / 24.0;
        return $"{days:F1}d";
    }
}
