using FellrnrHeatMonitor.Models;
using System.Drawing;
using System.Windows.Forms;

namespace FellrnrHeatMonitor.Controls;

internal sealed class TemperatureBinsPanel : Panel
{
    private readonly TemperatureBinsModel _model = new();
    //private readonly FlowLayoutPanel _binsContainer = new();
    private readonly TableLayoutPanel _binsContainer = new();

    public TemperatureBinsPanel()
    {
        //Width = 550;
        //Height = 255;
        AutoSize = true;
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
            Text = "Time in °C",
            AutoSize = true,
            //Width = 500,
            //Height = 24,
            Font = new Font(Font.FontFamily, Font.Size, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Black,
            //Margin = new Padding(0, 0, 0, 8)
        };

        _binsContainer.AutoSize = true;

        //_binsContainer.Dock = DockStyle.Fill;
        //_binsContainer.FlowDirection = FlowDirection.TopDown;
        //_binsContainer.AutoScroll = true;
        //_binsContainer.WrapContents = false;
        _binsContainer.BackColor = Color.Black;
        _binsContainer.Padding = new Padding(0);
        _binsContainer.Margin = new Padding(0);

        //Controls.Add(titleLabel);
        Controls.Add(_binsContainer);

        _binsContainer.Controls.Add(titleLabel);


        int i = 0;
        foreach (var bin in _model.Bins)
        {
            int column = i++ < 5 ? 0 : 1;
            var binLabel = new Label
            {
                Text = $"{bin.Label}: {FormatTimeSpan(bin.CumulativeTime)}",
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Black,
                Margin = new Padding(0, 2, 0, 2),
                Font = new Font(Font.FontFamily, Font.Size+4),
                Tag = bin,
            };

            _binsContainer.Controls.Add(binLabel, column, column == 0 ? i : i - 5);
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