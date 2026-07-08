using System.Drawing;
using System.Windows.Forms;
using FellrnrHeatMonitor.Models;
using FellrnrHeatMonitor.Services;

namespace FellrnrHeatMonitor.Controls;

internal sealed class SensorPairCard : Panel
{
    private readonly Label _titleLabel = new();
    private readonly Label _pairLabel = new();
    private readonly Label _btTempLabel = new();
    private readonly Label _btHumidityLabel = new();
    private readonly Label _btDewPointLabel = new();
    private readonly Label _btHeatStressLabel = new();
    private readonly Label _usbTempLabel = new();
    private readonly Label _usbHeatStressLabel = new();
    private readonly Label _lastSeenLabel = new();

    public SensorPairCard()
    {
        Width = 550;
        Height = 255;
        Margin = new Padding(8);
        Padding = new Padding(8);
        BorderStyle = BorderStyle.FixedSingle;
        BackColor = Color.White;
        BuildUi();
    }

    public void SetPair(SensorPairConfig pair, Color color)
    {
        _titleLabel.Text = pair.Name;
        _titleLabel.ForeColor = color;
        _pairLabel.Text = $"USB channel {pair.UsbChannel} + BT {MacAddressHelper.FormatWithColons(pair.BluetoothMacAddress)}";
    }

    public void UpdateDisplay(
        SensorPairConfig pair,
        BluetoothReading? bluetooth,
        string bluetoothDeltaText,
        HeatStressResult? bluetoothHeatStress,
        UsbReading? usb,
        string usbDeltaText,
        HeatStressResult? usbHeatStress,
        Color pairColor)
    {
        SetPair(pair, pairColor);

        _btTempLabel.Text = bluetooth is null
            ? "Waiting for Bluetooth"
            : $"{bluetooth.TemperatureC:F1} C {bluetoothDeltaText}".TrimEnd();

        _btHumidityLabel.Text = bluetooth is null ? "--" : $"{bluetooth.HumidityPercent}%";
        _btDewPointLabel.Text = bluetooth is null ? "--" : $"{bluetooth.DewPointC:F1} C";
        SetHeatLabel(_btHeatStressLabel, bluetoothHeatStress, bluetooth is null ? "Waiting for Bluetooth" : null);

        _usbTempLabel.Text = usb is null
            ? "Waiting for USB"
            : $"{usb.TemperatureC:F1} C {usbDeltaText}".TrimEnd();

        SetHeatLabel(_usbHeatStressLabel, usbHeatStress,
            bluetooth is null ? "Waiting for Bluetooth humidity" : usb is null ? "Waiting for USB" : null);

        var btTime = bluetooth?.Time.ToString("HH:mm:ss") ?? "--";
        var usbTime = usb?.Time.ToString("HH:mm:ss") ?? "--";
        var battery = bluetooth?.BatteryPercent is null ? string.Empty : $"  Battery {bluetooth.BatteryPercent}%";
        var rssi = bluetooth is null ? string.Empty : $"  RSSI {bluetooth.Rssi}";
        _lastSeenLabel.Text = $"Last BT {btTime}   Last USB {usbTime}{battery}{rssi}";
    }

    public void ClearValues()
    {
        _btTempLabel.Text = "--";
        _btHumidityLabel.Text = "--";
        _btDewPointLabel.Text = "--";
        SetHeatLabel(_btHeatStressLabel, null, "--");
        _usbTempLabel.Text = "--";
        SetHeatLabel(_usbHeatStressLabel, null, "--");
        _lastSeenLabel.Text = "Last BT --   Last USB --";
    }

    private void BuildUi()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 165)); //135
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _titleLabel.Font = new Font(Font.FontFamily, 12.5f, FontStyle.Bold);
        _titleLabel.AutoEllipsis = true;
        _titleLabel.Dock = DockStyle.Fill;
        layout.Controls.Add(_titleLabel, 0, 0);
        layout.SetColumnSpan(_titleLabel, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        _pairLabel.Dock = DockStyle.Fill;
        _pairLabel.ForeColor = Color.DimGray;
        _pairLabel.AutoEllipsis = true;
        layout.Controls.Add(_pairLabel, 0, 1);
        layout.SetColumnSpan(_pairLabel, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        AddRow(layout, 2, "Bluetooth temp", _btTempLabel);
        AddRow(layout, 3, "Bluetooth RH", _btHumidityLabel);
        AddRow(layout, 4, "Bluetooth dew point", _btDewPointLabel);
        AddRow(layout, 5, "Bluetooth heat stress", _btHeatStressLabel);
        AddRow(layout, 6, "USB temp", _usbTempLabel);
        AddRow(layout, 7, "USB + BT RH stress", _usbHeatStressLabel);

        _lastSeenLabel.Dock = DockStyle.Fill;
        _lastSeenLabel.ForeColor = Color.DimGray;
        _lastSeenLabel.AutoEllipsis = true;
        layout.Controls.Add(_lastSeenLabel, 0, 8);
        layout.SetColumnSpan(_lastSeenLabel, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Controls.Add(layout);
        ClearValues();
    }

    private static void AddRow(TableLayoutPanel layout, int row, string caption, Label valueLabel)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));

        var captionLabel = new Label
        {
            Text = caption,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.DimGray,
            AutoEllipsis = true
        };

        valueLabel.Dock = DockStyle.Fill;
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        valueLabel.AutoEllipsis = true;
        valueLabel.Padding = new Padding(4, 0, 4, 0);

        layout.Controls.Add(captionLabel, 0, row);
        layout.Controls.Add(valueLabel, 1, row);
    }

    private static void SetHeatLabel(Label label, HeatStressResult? heatStress, string? fallback)
    {
        if (heatStress is null)
        {
            label.Text = fallback ?? "--";
            label.BackColor = Color.Transparent;
            label.ForeColor = Color.Black;
            return;
        }

        label.Text = heatStress.Message;
        label.BackColor = heatStress.DisplayColor;
        label.ForeColor = ColorUtilities.ReadableTextColor(heatStress.DisplayColor);
    }
}
