using FellrnrHeatMonitor.Services;

namespace FellrnrHeatMonitor.Models;

public sealed class SensorPairConfig
{
    public int Index { get; set; }
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = string.Empty;
    public int UsbChannel { get; set; }
    public string BluetoothMacAddress { get; set; } = string.Empty;

    public string NormalizedBluetoothMacAddress => MacAddressHelper.Normalize(BluetoothMacAddress);

    public SensorPairConfig Clone()
    {
        return new SensorPairConfig
        {
            Index = Index,
            Enabled = Enabled,
            Name = Name,
            UsbChannel = UsbChannel,
            BluetoothMacAddress = BluetoothMacAddress
        };
    }
}
