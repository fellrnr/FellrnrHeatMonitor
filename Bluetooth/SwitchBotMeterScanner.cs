using FellrnrHeatMonitor.Models;
using FellrnrHeatMonitor.Services;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace FellrnrHeatMonitor.Bluetooth;

internal sealed class SwitchBotMeterScanner : ISwitchBotMeterScanner
{
    private readonly object _syncRoot = new();
    private Dictionary<string, SensorPairConfig> _sensorsByMac = new(StringComparer.OrdinalIgnoreCase);
    private BluetoothLEAdvertisementWatcher? _watcher;

    public event EventHandler<BluetoothReadingReceivedEventArgs>? ReadingReceived;
    public event EventHandler<string>? StatusChanged;

    public bool IsRunning { get; private set; }

    public void Configure(IEnumerable<SensorPairConfig> sensors)
    {
        lock (_syncRoot)
        {
            _sensorsByMac = sensors
                .Where(s => s.Enabled && MacAddressHelper.TryNormalize(s.BluetoothMacAddress, out _))
                .GroupBy(s => s.NormalizedBluetoothMacAddress, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Clone(), StringComparer.OrdinalIgnoreCase);
        }
    }

    public void Start()
    {
        lock (_syncRoot)
        {
            if (IsRunning)
            {
                return;
            }

            if (_sensorsByMac.Count == 0)
            {
                StatusChanged?.Invoke(this, "Bluetooth scanner not started; no valid SwitchBot MAC addresses are configured.");
                return;
            }

            _watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };
            _watcher.Received += WatcherOnReceived;
            _watcher.Stopped += WatcherOnStopped;
            _watcher.Start();
            IsRunning = true;
        }

        StatusChanged?.Invoke(this, "Bluetooth scan started. Waiting for SwitchBot advertisements...");
    }

    public void Stop()
    {
        BluetoothLEAdvertisementWatcher? watcherToStop;
        lock (_syncRoot)
        {
            watcherToStop = _watcher;
            if (watcherToStop is null)
            {
                IsRunning = false;
                return;
            }

            _watcher = null;
            IsRunning = false;
        }

        watcherToStop.Received -= WatcherOnReceived;
        watcherToStop.Stopped -= WatcherOnStopped;
        watcherToStop.Stop();
        StatusChanged?.Invoke(this, "Bluetooth scan stopped.");
    }

    public void Dispose()
    {
        Stop();
    }

    private void WatcherOnStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
    {
        lock (_syncRoot)
        {
            if (ReferenceEquals(_watcher, sender))
            {
                IsRunning = false;
                _watcher = null;
            }
        }

        var message = args.Error == BluetoothError.Success ? "Bluetooth scan stopped." : $"Bluetooth scan stopped: {args.Error}.";
        StatusChanged?.Invoke(this, message);
    }

    private void WatcherOnReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
    {
        if (!SwitchBotAdvertisementParser.TryParse(args, out var advertisement) || advertisement is null)
        {
            return;
        }

        SensorPairConfig? sensor = null;
        lock (_syncRoot)
        {
            foreach (var candidate in advertisement.MacCandidates)
            {
                var normalized = MacAddressHelper.Normalize(candidate);
                if (_sensorsByMac.TryGetValue(normalized, out sensor))
                {
                    break;
                }
            }
        }

        if (sensor is null)
        {
            return;
        }

        var dewPoint = DewPointCalculator.CalculateCelsius(advertisement.TemperatureC, advertisement.HumidityPercent);
        var reading = new BluetoothReading
        {
            Time = DateTimeOffset.Now,
            SensorIndex = sensor.Index,
            SensorName = sensor.Name,
            MacAddress = MacAddressHelper.FormatWithColons(sensor.BluetoothMacAddress),
            TemperatureC = advertisement.TemperatureC,
            HumidityPercent = advertisement.HumidityPercent,
            DewPointC = dewPoint,
            BatteryPercent = advertisement.BatteryPercent,
            Rssi = advertisement.Rssi
        };

        ReadingReceived?.Invoke(this, new BluetoothReadingReceivedEventArgs(reading));
    }
}
