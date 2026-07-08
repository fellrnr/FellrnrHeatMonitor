using FellrnrHeatMonitor.Models;

namespace FellrnrHeatMonitor.Bluetooth;

internal sealed class BluetoothReadingReceivedEventArgs : EventArgs
{
    public BluetoothReadingReceivedEventArgs(BluetoothReading reading)
    {
        Reading = reading;
    }

    public BluetoothReading Reading { get; }
}
