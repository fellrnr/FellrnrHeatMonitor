using FellrnrHeatMonitor.Models;
using System.Drawing;
using System.Windows.Forms;

namespace FellrnrHeatMonitor.Controls;

internal sealed class TemperatureBinsPanel : Panel
{
    private readonly TemperatureBinsModel _model = new();
    private readonly FlowLayoutPanel _binsContainer = new();

    public TemperatureBinsPanel()
    {
        Width = 550;
        //Height = 255;
        Height = 305;
        Margin = new Padding(8);
        Padding = new Padding(8);
        BorderStyle = BorderStyle.FixedSingle;
        BackColor = Color.Black;
        AutoScroll = true;
        BuildUi();
    }

    public TemperatureBinsModel Model => _model;

    private void BuildUi()
    {
        var titleLabel = new Label
        {
            Text = "Temperature Bins (Cumulative Time)",
            AutoSize = true,
            //Width = 500,
            //Height = 24,
            Font = new Font(Font.FontFamily, Font.Size, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Black,
            //Margin = new Padding(0, 0, 0, 8)
        };

        _binsContainer.Dock = DockStyle.Fill;
        _binsContainer.FlowDirection = FlowDirection.TopDown;
        _binsContainer.AutoScroll = true;
        _binsContainer.WrapContents = false;
        _binsContainer.BackColor = Color.Black;
        _binsContainer.Padding = new Padding(0);
        _binsContainer.Margin = new Padding(0);

        //Controls.Add(titleLabel);
        Controls.Add(_binsContainer);

        _binsContainer.Controls.Add(titleLabel);


        foreach (var bin in _model.Bins)
        {
            var binLabel = new Label
            {
                Text = $"{bin.Label}: {FormatTimeSpan(bin.CumulativeTime)}",
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Black,
                Margin = new Padding(0, 2, 0, 2),
                Font = new Font(Font.FontFamily, Font.Size),
                Tag = bin
            };

            _binsContainer.Controls.Add(binLabel);
        }
    }

    public void UpdateBin(double temperature, DateTimeOffset now)
    {
        _model.Update(temperature, now);
        UpdateDisplay();
    }

    public void ResetBins()
    {
        _model.Reset();
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        foreach (Control control in _binsContainer.Controls)
        {
            if (control.Tag is TemperatureBin bin)
            {
                control.Text = $"{bin.Label}: {FormatTimeSpan(bin.CumulativeTime)}";
            }
        }
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
        {
            return $"{ts.TotalHours:F1}h";
        }
        if (ts.TotalMinutes >= 1)
        {
            return $"{ts.TotalMinutes:F1}m";
        }
        return $"{ts.TotalSeconds:F0}s";
    }
}