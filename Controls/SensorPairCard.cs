using FellrnrHeatMonitor.Models;
using FellrnrHeatMonitor.Services;
using System.Drawing;
using System.Windows.Forms;

namespace FellrnrHeatMonitor.Controls;

internal sealed class SensorPairCard : Panel
{
    private readonly Label _titleLabel = new();
    //private readonly Label _pairLabel = new();
    //private readonly Label _btTempLabel = new();
    private readonly Label _btHumidityLabel = new();
    private readonly Label _btDewPointLabel = new();
    //private readonly Label _btHeatStressLabel = new();
    private readonly Label _usbTempLabel = new();
    private readonly Label _usbHeatStressLabel = new();
    private readonly Label _lastSeenLabel = new();

    public SensorPairCard()
    {
        //Width = 550;
        //Height = 255;
        Width = 550;
        Height = 305;
        //AutoSize = true;
        Margin = new Padding(8);
        Padding = new Padding(8);
        BorderStyle = BorderStyle.FixedSingle;
        //Dark
        //BackColor = Color.White;
        BackColor = Color.Black;
        BuildUi();
    }

    public void SetPair(SensorPairConfig pair, Color color)
    {
        _titleLabel.Text = pair.Name;
        _titleLabel.ForeColor = color;
        //_pairLabel.Text = $"USB channel {pair.UsbChannel} + BT {MacAddressHelper.FormatWithColons(pair.BluetoothMacAddress)}";
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

        //_btTempLabel.Text = bluetooth is null
        //    ? "Waiting for Bluetooth"
        //    : $"{bluetooth.TemperatureC:F1} C {bluetoothDeltaText}".TrimEnd();

        _btHumidityLabel.Text = bluetooth is null ? "--" : $"{bluetooth.HumidityPercent}%";
        _btDewPointLabel.Text = bluetooth is null ? "--" : $"{bluetooth.DewPointC:F1} C";
        //SetHeatLabel(_btHeatStressLabel, bluetoothHeatStress, bluetooth is null ? "Waiting for Bluetooth" : null);

        _usbTempLabel.Text = usb is null
            ? "Waiting for USB"
            : $"{usb.TemperatureC:F1}° Δ{usbDeltaText}".TrimEnd();

        if (usb is not null)
        {
            _usbTempLabel.BackColor = ColorUtilities.GetTemperatureColor(usb.TemperatureC);

            _usbTempLabel.ForeColor = ColorUtilities.ReadableTextColor(_usbTempLabel.BackColor);
        }
        else
        {
            _usbTempLabel.BackColor = Color.Transparent;
            //dark
            //_usbTempLabel.ForeColor = Color.Black;
            _usbTempLabel.ForeColor = Color.White;
        }
        SetHeatLabel(_usbHeatStressLabel, usbHeatStress,
            bluetooth is null ? "Waiting for Bluetooth humidity" : usb is null ? "Waiting for USB" : null);

        var btTime = bluetooth?.Time.ToString("HH:mm:ss") ?? "--";
        var usbTime = usb?.Time.ToString("HH:mm:ss") ?? "--";
        var battery = bluetooth?.BatteryPercent is null ? string.Empty : $"  Battery {bluetooth.BatteryPercent}%";
        var rssi = bluetooth is null ? string.Empty : $"  RSSI {bluetooth.Rssi}";
        _lastSeenLabel.Text = $"Last BT {btTime}   Last USB {usbTime}{battery}{rssi}";
    }

    public void UpdateDisplay(BluetoothReading? bluetooth, UsbReading? usb, HeatStressResult? usbHeatStress)
    {
        _titleLabel.Text = "Average";

        _btHumidityLabel.Text = bluetooth is null ? "--" : $"{bluetooth.HumidityPercent}%";
        _btDewPointLabel.Text = bluetooth is null ? "--" : $"{bluetooth.DewPointC:F1} C";

        _usbTempLabel.Text = usb is null
            ? "Waiting for USB"
            : $"{usb.TemperatureC:F1}°".TrimEnd();

        if (usb is not null)
        {
            _usbTempLabel.BackColor = ColorUtilities.GetTemperatureColor(usb.TemperatureC);

            _usbTempLabel.ForeColor = ColorUtilities.ReadableTextColor(_usbTempLabel.BackColor);
        }
        else
        {
            _usbTempLabel.BackColor = Color.Transparent;
            _usbTempLabel.ForeColor = Color.Black;
        }
        SetHeatLabel(_usbHeatStressLabel, usbHeatStress,
            bluetooth is null ? "Waiting for Bluetooth humidity" : usb is null ? "Waiting for USB" : null);
    }


    public void ClearValues()
    {
        //_btTempLabel.Text = "--";
        _btHumidityLabel.Text = "--";
        _btDewPointLabel.Text = "--";
        //SetHeatLabel(_btHeatStressLabel, null, "--");
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
            Margin = new Padding(0),
            AutoSize = true,
            //BackColor = Color.Blue,
        };
        //layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 165)); //135
        //layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); //135
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _titleLabel.Font = new Font(Font.FontFamily, 12.5f, FontStyle.Bold);
        //_titleLabel.BackColor = Color.White;
        _titleLabel.AutoSize = true;
        //_titleLabel.AutoEllipsis = true;
        //_titleLabel.Dock = DockStyle.Fill;
        layout.Controls.Add(_titleLabel, 0, 0);
        layout.SetColumnSpan(_titleLabel, 2);
        //layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        //_pairLabel.Dock = DockStyle.Fill;
        //_pairLabel.ForeColor = Color.DimGray;
        //_pairLabel.AutoEllipsis = true;
        //layout.Controls.Add(_pairLabel, 0, 1);
        //layout.SetColumnSpan(_pairLabel, 2);
        //layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        //layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        //AddRow(layout, 2, "Bluetooth temp", _btTempLabel, new Font(Font.FontFamily, 12.5f, FontStyle.Regular));
        AddRow(layout, 1, "USB temp", _usbTempLabel, new Font(Font.FontFamily, 16f, FontStyle.Regular));
        AddRow(layout, 2, "Feels Like", _usbHeatStressLabel, new Font(Font.FontFamily, 16f, FontStyle.Regular));
        AddRow(layout, 3, "RH", _btHumidityLabel, new Font(Font.FontFamily, 12.5f, FontStyle.Regular));
        AddRow(layout, 4, "Dew point", _btDewPointLabel, new Font(Font.FontFamily, 12.5f, FontStyle.Regular));
        //AddRow(layout, 5, "Bluetooth heat stress", _btHeatStressLabel, new Font(Font.FontFamily, 12.5f, FontStyle.Regular));

        _lastSeenLabel.Dock = DockStyle.Fill;
        //dark
        //_lastSeenLabel.ForeColor = Color.DimGray;
        _lastSeenLabel.ForeColor = Color.White;
        _lastSeenLabel.AutoEllipsis = true;
        layout.Controls.Add(_lastSeenLabel, 0, 8);
        layout.SetColumnSpan(_lastSeenLabel, 2);
        //layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Controls.Add(layout);
        ClearValues();
    }

    private static void AddRow(TableLayoutPanel layout, int row, string caption, Label valueLabel, Font font)
    {
        //layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var captionLabel = new Label
        {
            Text = caption,
            //Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            //dark
            //ForeColor = Color.DimGray,
            ForeColor = Color.White,
            AutoEllipsis = true,
            AutoSize = true,
            Font = font,
        };

        //valueLabel.Dock = DockStyle.Fill;
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        valueLabel.AutoEllipsis = true;
        valueLabel.Padding = new Padding(4, 0, 4, 0);
        valueLabel.Font = font;
        valueLabel.AutoSize = true;

        layout.Controls.Add(captionLabel, 0, row);
        layout.Controls.Add(valueLabel, 1, row);
    }

    private static void SetHeatLabel(Label label, HeatStressResult? heatStress, string? fallback)
    {
        if (heatStress is null)
        {
            label.Text = fallback ?? "--";
            label.BackColor = Color.Transparent;
            //dark
            //label.ForeColor = Color.Black;
            label.ForeColor = Color.White;
            return;
        }

        label.Text = heatStress.Message;
        //label.BackColor = heatStress.DisplayColor;
        label.BackColor = ColorUtilities.GetTemperatureColor(heatStress.FeelsLikeTempC - 20.0);

        label.ForeColor = ColorUtilities.ReadableTextColor(label.BackColor);
    }
}
