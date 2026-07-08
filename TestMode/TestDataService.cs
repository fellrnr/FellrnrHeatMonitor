using System.Threading;
using FellrnrHeatMonitor.Bluetooth;
using FellrnrHeatMonitor.Models;
using FellrnrHeatMonitor.Services;
using FellrnrHeatMonitor.Usb;

namespace FellrnrHeatMonitor.TestMode;

internal sealed class TestDataService : IDisposable
{
    private readonly object _sync = new();
    private readonly Random _random = new();
    private AppConfig _config = new();
    private System.Threading.Timer? _timer;
    private DateTimeOffset _start;

    public event EventHandler<BluetoothReadingReceivedEventArgs>? BluetoothReadingReceived;
    public event EventHandler<UsbTemperaturesReceivedEventArgs>? UsbTemperaturesReceived;
    public event EventHandler<string>? StatusChanged;

    public bool IsRunning { get; private set; }

    public void Configure(AppConfig config)
    {
        lock (_sync)
        {
            _config = config.Clone();
        }
    }

    public void Start()
    {
        lock (_sync)
        {
            if (IsRunning)
            {
                return;
            }

            _start = DateTimeOffset.Now;
            _timer = new System.Threading.Timer(Tick, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
            IsRunning = true;
        }

        StatusChanged?.Invoke(this, "Test mode started. Generated USB and Bluetooth readings are being displayed.");
    }

    public void Stop()
    {
        lock (_sync)
        {
            _timer?.Dispose();
            _timer = null;
            IsRunning = false;
        }

        StatusChanged?.Invoke(this, "Test mode stopped.");
    }

    public void Dispose()
    {
        Stop();
    }

    private void Tick(object? state)
    {
        AppConfig config;
        lock (_sync)
        {
            if (!IsRunning)
            {
                return;
            }

            config = _config.Clone();
        }

        var now = DateTimeOffset.Now;
        var seconds = (now - _start).TotalSeconds;
        var usbTemps = new double[4];

        foreach (var pair in config.SensorPairs.OrderBy(p => p.Index))
        {
            if (!pair.Enabled || pair.Index is < 1 or > 4)
            {
                continue;
            }

            var idx = pair.Index - 1;
            var baseTemp = 25.0 + idx * 2.0 + 4.0 * Math.Sin(seconds / 120.0 + idx * 0.7);
            var humidity = (int)Math.Round(Math.Clamp(50.0 + 18.0 * Math.Sin(seconds / 170.0 + idx) + Noise(2.0), 18.0, 90.0));
            var bluetoothTemp = baseTemp + Noise(0.25);
            var usbTemp = baseTemp + 3.5 + idx * 0.8 + 2.0 * Math.Sin(seconds / 80.0 + idx * 1.3) + Noise(0.35);
            usbTemps[idx] = usbTemp;

            var mac = string.IsNullOrWhiteSpace(pair.BluetoothMacAddress)
                ? $"02:00:00:00:00:{pair.Index:X2}"
                : MacAddressHelper.FormatWithColons(pair.BluetoothMacAddress);

            var bt = new BluetoothReading
            {
                Time = now,
                SensorIndex = pair.Index,
                SensorName = pair.Name,
                MacAddress = mac,
                TemperatureC = bluetoothTemp,
                HumidityPercent = humidity,
                DewPointC = DewPointCalculator.CalculateCelsius(bluetoothTemp, humidity),
                BatteryPercent = 95 - idx * 3,
                Rssi = -55 - idx * 4
            };

            BluetoothReadingReceived?.Invoke(this, new BluetoothReadingReceivedEventArgs(bt));
        }

        UsbTemperaturesReceived?.Invoke(this, new UsbTemperaturesReceivedEventArgs(now, usbTemps, "TEST"));
    }

    private double Noise(double amplitude)
    {
        lock (_random)
        {
            return (_random.NextDouble() * 2.0 - 1.0) * amplitude;
        }
    }
}
