using FellrnrHeatMonitor.Models;

namespace FellrnrHeatMonitor.Bluetooth;

internal interface ISwitchBotMeterScanner : IDisposable
{
    event EventHandler<BluetoothReadingReceivedEventArgs>? ReadingReceived;
    event EventHandler<string>? StatusChanged;
    bool IsRunning { get; }
    void Configure(IEnumerable<SensorPairConfig> sensors);
    void Start();
    void Stop();
}
