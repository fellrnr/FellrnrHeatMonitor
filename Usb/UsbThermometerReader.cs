namespace FellrnrHeatMonitor.Usb;

internal sealed class UsbThermometerReader : IDisposable
{
    private readonly object _sync = new();
    private CancellationTokenSource? _cts;
    private Task? _pollingTask;
    private CD50Thermometer? _thermometer;
    private string _configuredPort = "AUTO";
    private int _pollIntervalMs = 500;

    public event EventHandler<UsbTemperaturesReceivedEventArgs>? TemperaturesReceived;
    public event EventHandler<string>? StatusChanged;

    public bool IsRunning { get; private set; }
    public string CurrentPort { get; private set; } = string.Empty;

    public void Configure(string port, int pollIntervalMs)
    {
        _configuredPort = string.IsNullOrWhiteSpace(port) ? "AUTO" : port.Trim();
        _pollIntervalMs = Math.Clamp(pollIntervalMs, 200, 60_000);
    }

    public void Start()
    {
        lock (_sync)
        {
            if (IsRunning)
            {
                return;
            }

            var port = ResolvePort();
            _thermometer = new CD50Thermometer(port);
            _thermometer.Connect();
            CurrentPort = port;
            _cts = new CancellationTokenSource();
            IsRunning = true;
            _pollingTask = Task.Run(() => PollingLoop(_cts.Token));
        }

        StatusChanged?.Invoke(this, $"USB thermometer connected on {CurrentPort}.");
    }

    public void Stop()
    {
        CancellationTokenSource? cts;
        CD50Thermometer? thermometer;
        lock (_sync)
        {
            cts = _cts;
            _cts = null;
            thermometer = _thermometer;
            _thermometer = null;
            IsRunning = false;
        }

        try
        {
            cts?.Cancel();
        }
        catch
        {
            // Ignore cancellation race.
        }

        thermometer?.Dispose();
        StatusChanged?.Invoke(this, "USB thermometer stopped.");
    }

    public void Dispose()
    {
        Stop();
    }

    private string ResolvePort()
    {
        if (!string.Equals(_configuredPort, "AUTO", StringComparison.OrdinalIgnoreCase))
        {
            return _configuredPort;
        }

        var port = CD50Thermometer.FindAvailablePort();
        if (!string.IsNullOrWhiteSpace(port))
        {
            return port;
        }

        throw new InvalidOperationException("Could not find a CD50 thermometer on any COM port. Enable Test mode or choose a COM port in Configuration.");
    }

    private async Task PollingLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            CD50Thermometer? thermometer;
            lock (_sync)
            {
                thermometer = _thermometer;
            }

            if (thermometer is null || !thermometer.Connected)
            {
                break;
            }

            var temperatures = thermometer.ReadTemperatures();
            if (temperatures is { Length: 4 })
            {
                TemperaturesReceived?.Invoke(this, new UsbTemperaturesReceivedEventArgs(DateTimeOffset.Now, temperatures, CurrentPort));
            }

            try
            {
                await Task.Delay(_pollIntervalMs, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
