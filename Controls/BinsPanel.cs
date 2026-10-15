using FellrnrHeatMonitor.Models;
using System.Drawing;
using System.Windows.Forms;

namespace FellrnrHeatMonitor.Controls;

internal sealed class BinsPanel : Panel
{
    private readonly BinsModel _model;
    private readonly TableLayoutPanel _binsContainer = new();

    public BinsPanel(BinsModel model, string title)
    {
        _model = model;
        AutoSize = true;
        Margin = new Padding(8);
        Padding = new Padding(8);
        BorderStyle = BorderStyle.FixedSingle;
        BackColor = Color.Black;
        AutoScroll = true;
        BuildUi(title);
    }

    public BinsModel Model => _model;

    private void BuildUi(string title)
    {
        var titleLabel = new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font(Font.FontFamily, Font.Size, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Black,
        };

        _binsContainer.AutoSize = true;
        _binsContainer.BackColor = Color.Black;
        _binsContainer.Padding = new Padding(0);
        _binsContainer.Margin = new Padding(0);

        Controls.Add(_binsContainer);
        _binsContainer.Controls.Add(titleLabel);

        int i = 0;
        foreach (var bin in _model.Bins)
        {
            //change from two columns to one column, so that the bins are displayed in a single column
            //int column = i++ < 5 ? 0 : 1;
            int column = 0; i++;
            var binLabel = new Label
            {
                Text = $"{bin.Label}: {FormatTimeSpan(bin.CumulativeTime)}",
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Black,
                Margin = new Padding(0, 2, 0, 2),
                Font = new Font(Font.FontFamily, Font.Size + 6),
                Tag = bin,
            };

            _binsContainer.Controls.Add(binLabel, column, column == 0 ? i : i - 5);
        }
    }

    public void UpdateBin(double value, DateTimeOffset now)
    {
        _model.Update(value, now);
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
            if (control.Tag is Bin bin)
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
