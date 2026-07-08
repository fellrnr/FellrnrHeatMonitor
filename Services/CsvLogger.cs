using System.Globalization;
using System.Text;
using FellrnrHeatMonitor.Models;

namespace FellrnrHeatMonitor.Services;

internal sealed class CsvLogger : IDisposable
{
    private readonly object _sync = new();
    private StreamWriter? _writer;

    public void StartNew(string path)
    {
        lock (_sync)
        {
            Close();
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            _writer = new StreamWriter(path, append: false, Encoding.UTF8);
            _writer.WriteLine("Timestamp,SensorIndex,SensorName,Source,BluetoothMac,UsbChannel,BluetoothTemperatureC,BluetoothDelta60C,BluetoothDelta120C,BluetoothHumidityPercent,BluetoothDewPointC,BluetoothHeatStress,UsbTemperatureC,UsbDelta60C,UsbDelta120C,UsbHeatStressUsingBluetoothHumidity,BatteryPercent,Rssi");
        }
    }

    public void WriteCombined(
        int sensorIndex,
        string sensorName,
        string source,
        BluetoothReading? bluetooth,
        UsbReading? usb,
        string bluetoothDelta60,
        string bluetoothDelta120,
        HeatStressResult? bluetoothHeatStress,
        string usbDelta60,
        string usbDelta120,
        HeatStressResult? usbHeatStress)
    {
        lock (_sync)
        {
            if (_writer is null)
            {
                return;
            }

            var timestamp = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture);
            var row = new[]
            {
                timestamp,
                sensorIndex.ToString(CultureInfo.InvariantCulture),
                sensorName,
                source,
                bluetooth?.MacAddress ?? string.Empty,
                usb?.Channel.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                FormatNullable(bluetooth?.TemperatureC),
                bluetoothDelta60,
                bluetoothDelta120,
                bluetooth?.HumidityPercent.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                FormatNullable(bluetooth?.DewPointC),
                bluetoothHeatStress?.Message ?? string.Empty,
                FormatNullable(usb?.TemperatureC),
                usbDelta60,
                usbDelta120,
                usbHeatStress?.Message ?? string.Empty,
                bluetooth?.BatteryPercent?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                bluetooth?.Rssi.ToString(CultureInfo.InvariantCulture) ?? string.Empty
            };

            _writer.WriteLine(string.Join(",", row.Select(Escape)));
            _writer.Flush();
        }
    }

    public void Close()
    {
        lock (_sync)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    public void Dispose()
    {
        Close();
    }

    private static string FormatNullable(double? value)
    {
        return value.HasValue ? value.Value.ToString("F2", CultureInfo.InvariantCulture) : string.Empty;
    }

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}
