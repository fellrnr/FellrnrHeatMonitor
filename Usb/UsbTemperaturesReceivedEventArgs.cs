namespace FellrnrHeatMonitor.Usb;

internal sealed class UsbTemperaturesReceivedEventArgs : EventArgs
{
    public UsbTemperaturesReceivedEventArgs(DateTimeOffset time, double[] temperaturesC, string sourcePort)
    {
        Time = time;
        TemperaturesC = temperaturesC;
        SourcePort = sourcePort;
    }

    public DateTimeOffset Time { get; }
    public double[] TemperaturesC { get; }
    public string SourcePort { get; }
}
