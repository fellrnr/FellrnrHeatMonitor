using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using FellrnrHeatMonitor.Bluetooth;
using FellrnrHeatMonitor.Controls;
using FellrnrHeatMonitor.Models;
using FellrnrHeatMonitor.Services;
using FellrnrHeatMonitor.TestMode;
using FellrnrHeatMonitor.Usb;

namespace FellrnrHeatMonitor;

internal sealed class MainForm : Form
{
    private readonly AppConfigService _configService = new();
    private readonly ISwitchBotMeterScanner _scanner = new SwitchBotMeterScanner();
    private readonly UsbThermometerReader _usbReader = new();
    private readonly TestDataService _testData = new();
    private readonly CsvLogger _csvLogger = new();

    private readonly FlowLayoutPanel _cardsPanel = new();
    private readonly TemperatureGraphControl _graph = new();
    private readonly TemperatureBinsPanel _binsPanel = new();
    private readonly Label _statusLabel = new();
    private readonly Label _modeLabel = new();
    private readonly Button _startButton = new();
    private readonly Button _stopButton = new();
    private readonly Button _clearButton = new();
    private readonly Button _resetBinsButton = new();
    private readonly Button _configButton = new();

    private readonly Dictionary<int, SensorPairCard> _cardsByIndex = new();
    private readonly Dictionary<string, GraphSeries> _seriesByKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> _lastGraphPointByKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly SensorPairCard AverageCard = new SensorPairCard();
    private BluetoothReading?[] _lastBluetooth = new BluetoothReading?[4];
    private UsbReading?[] _lastUsb = new UsbReading?[4];
    private TemperatureDeltaCalculator[] _bluetoothDeltas = CreateDeltaCalculators();
    private TemperatureDeltaCalculator[] _usbDeltas = CreateDeltaCalculators();

    private AppConfig _config = new();
    private bool _running;
    private bool _runningInTestMode;

    public MainForm()
    {
        Text = "Fellrnr Heat Monitor";
        InitializeComponent();
        //Icon = Properties.Resources.HeatMonitorIcon;
        //Width = 1540;
        //Height = 900;
        //MinimumSize = new Size(1050, 650);
        //StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        //FormBorderStyle = FormBorderStyle.None;

        _config = _configService.Load();
        BuildUi();
        WireEvents();
        RebuildFromConfiguration();
        UpdateButtonState();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_config.AutoStart)
        {
            BeginInvoke(new Action(StartMonitoring));
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        StopMonitoring(showStatus: false);
        _scanner.Dispose();
        _usbReader.Dispose();
        _testData.Dispose();
        _csvLogger.Dispose();
        base.OnFormClosing(e);
    }

    private static TemperatureDeltaCalculator[] CreateDeltaCalculators()
    {
        return Enumerable.Range(0, 4)
            .Select(_ => new TemperatureDeltaCalculator(TimeSpan.FromMinutes(10)))
            .ToArray();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 335)); //285
        //root.RowStyles.Add(new RowStyle(SizeType.Absolute, 280));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10),
            WrapContents = false
        };

        ConfigureButton(_startButton, "Start", (_, _) => StartMonitoring());
        ConfigureButton(_stopButton, "Stop", (_, _) => StopMonitoring());
        ConfigureButton(_clearButton, "Clear graph", (_, _) => ClearGraphData());
        ConfigureButton(_resetBinsButton, "Reset bins", (_, _) => ResetTemperatureBins());
        ConfigureButton(_configButton, "Configuration", (_, _) => OpenConfigurationDialog());

        _modeLabel.AutoSize = false;
        _modeLabel.Width = 120;
        _modeLabel.Height = 30;
        _modeLabel.Margin = new Padding(12, 2, 4, 2);
        _modeLabel.TextAlign = ContentAlignment.MiddleCenter;
        _modeLabel.Font = new Font(Font.FontFamily, Font.Size, FontStyle.Bold);

        _statusLabel.AutoSize = false;
        _statusLabel.Width = 850;
        _statusLabel.Height = 32;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Margin = new Padding(12, 2, 0, 0);

        toolbar.Controls.Add(_startButton);
        toolbar.Controls.Add(_stopButton);
        toolbar.Controls.Add(_clearButton);
        toolbar.Controls.Add(_resetBinsButton);
        toolbar.Controls.Add(_configButton);
        toolbar.Controls.Add(_modeLabel);
        toolbar.Controls.Add(_statusLabel);

        _cardsPanel.Dock = DockStyle.Fill;
        _cardsPanel.AutoScroll = true;
        _cardsPanel.FlowDirection = FlowDirection.LeftToRight;
        _cardsPanel.WrapContents = false;
        _cardsPanel.Padding = new Padding(8);
        _cardsPanel.BackColor = SystemColors.Control;

        //_binsPanel.Dock = DockStyle.Fill;
        //_binsPanel.Margin = new Padding(8);

        _graph.Dock = DockStyle.Fill;
        _graph.Margin = new Padding(8);

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(_cardsPanel, 0, 1);
        //root.Controls.Add(_binsPanel, 0, 2);
        root.Controls.Add(_graph, 0, 2);
        Controls.Add(root);
    }

    private void WireEvents()
    {
        _scanner.ReadingReceived += (_, e) => RunOnUiThread(() => ApplyBluetoothReading(e.Reading));
        _scanner.StatusChanged += (_, message) => RunOnUiThread(() => SetStatus(message));
        _usbReader.TemperaturesReceived += (_, e) => RunOnUiThread(() => ApplyUsbTemperatures(e));
        _usbReader.StatusChanged += (_, message) => RunOnUiThread(() => SetStatus(message));
        _testData.BluetoothReadingReceived += (_, e) => RunOnUiThread(() => ApplyBluetoothReading(e.Reading));
        _testData.UsbTemperaturesReceived += (_, e) => RunOnUiThread(() => ApplyUsbTemperatures(e));
        _testData.StatusChanged += (_, message) => RunOnUiThread(() => SetStatus(message));
    }

    private static void ConfigureButton(Button button, string text, EventHandler handler)
    {
        button.Text = text;
        button.Width = 118;
        button.Height = 32;
        button.Margin = new Padding(4, 2, 4, 2);
        button.Click += handler;
    }

    private void RebuildFromConfiguration()
    {
        _config.EnsureFourPairs();
        _cardsPanel.Controls.Clear();
        _cardsByIndex.Clear();
        _seriesByKey.Clear();
        _lastGraphPointByKey.Clear();
        _lastBluetooth = new BluetoothReading?[4];
        _lastUsb = new UsbReading?[4];
        _bluetoothDeltas = CreateDeltaCalculators();
        _usbDeltas = CreateDeltaCalculators();

        foreach (var pair in _config.SensorPairs.OrderBy(p => p.Index))
        {
            if (!pair.Enabled)
            {
                continue;
            }

            var color = ColorUtilities.SensorPalette[(pair.Index - 1) % ColorUtilities.SensorPalette.Length];
            var card = new SensorPairCard();
            card.SetPair(pair, color);
            _cardsByIndex[pair.Index] = card;
            _cardsPanel.Controls.Add(card);

            // ? DashStyle.Dash : DashStyle.Solid
            _seriesByKey[SeriesKey(pair.Index, "USB")] = new GraphSeries(SeriesKey(pair.Index, "USB"), $"{pair.Name} USB", color, dashed: System.Drawing.Drawing2D.DashStyle.Solid);
            _seriesByKey[SeriesKey(pair.Index, "BT")] = new GraphSeries(SeriesKey(pair.Index, "BT"), $"{pair.Name} BT", color, dashed: System.Drawing.Drawing2D.DashStyle.Dash);
        }

        _cardsPanel.Controls.Add(AverageCard);
        _cardsPanel.Controls.Add(_binsPanel);

        // Add average humidity series with secondary axis
        _seriesByKey["AvgHumidity"] = new GraphSeries("AvgHumidity", "Average Humidity %", Color.Plum, dashed: System.Drawing.Drawing2D.DashStyle.DashDotDot, useSecondaryAxis: true);

        // Add average humidity series with secondary axis
        _seriesByKey["AvgDewPoint"] = new GraphSeries("AvgDewPoint", "Average Dew Point", Color.Lime, dashed: System.Drawing.Drawing2D.DashStyle.Dot, useSecondaryAxis: true);

        _graph.History = TimeSpan.FromMinutes(Math.Max(1, _config.GraphHistoryMinutes));
        _graph.SetSeries(_seriesByKey.Values);
        UpdateModeLabel();
        SetStatus($"Configured {_cardsByIndex.Count} sensor pair(s). CSV: {_configService.CsvPath}");
    }

    private void StartMonitoring()
    {
        if (_running)
        {
            return;
        }

        if (_config.SensorPairs.All(p => !p.Enabled))
        {
            OpenConfigurationDialog();
            if (_config.SensorPairs.All(p => !p.Enabled))
            {
                return;
            }
        }

        try
        {
            ClearGraphData(clearDisplays: true);
            _csvLogger.StartNew(_configService.CsvPath);
            _runningInTestMode = _config.TestMode;

            if (_runningInTestMode)
            {
                _testData.Configure(_config);
                _testData.Start();
            }
            else
            {
                _scanner.Configure(_config.SensorPairs);
                _scanner.Start();
                _usbReader.Configure(_config.UsbComPort, _config.UsbPollIntervalMs);
                _usbReader.Start();
            }

            _running = true;
            SetStatus(_runningInTestMode
                ? $"Running in test mode. CSV overwritten at {_configService.CsvPath}"
                : $"Running with USB + Bluetooth hardware. CSV overwritten at {_configService.CsvPath}");
        }
        catch (Exception ex)
        {
            StopMonitoring(showStatus: false);
            MessageBox.Show(this, ex.Message, "Unable to start", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("Monitoring did not start.");
        }
        finally
        {
            UpdateButtonState();
        }
    }

    private void StopMonitoring(bool showStatus = true)
    {
        if (_runningInTestMode)
        {
            _testData.Stop();
        }
        else
        {
            _scanner.Stop();
            _usbReader.Stop();
        }

        _csvLogger.Close();
        _running = false;
        if (showStatus)
        {
            SetStatus("Monitoring stopped.");
        }

        UpdateButtonState();
    }

    private void ClearGraphData(bool clearDisplays = false)
    {
        foreach (var series in _seriesByKey.Values)
        {
            series.Points.Clear();
        }

        _lastGraphPointByKey.Clear();
        foreach (var calculator in _bluetoothDeltas.Concat(_usbDeltas))
        {
            calculator.Clear();
        }

        if (clearDisplays)
        {
            _lastBluetooth = new BluetoothReading?[4];
            _lastUsb = new UsbReading?[4];
            foreach (var card in _cardsByIndex.Values)
            {
                card.ClearValues();
            }
        }

        _graph.Invalidate();
        if (!clearDisplays)
        {
            SetStatus(_running ? "Graph data cleared. Logging continues." : "Graph data cleared.");
        }
    }

    private void ResetTemperatureBins()
    {
        _binsPanel.ResetBins();
        SetStatus("Temperature bins reset");
    }

    private void OpenConfigurationDialog()
    {
        var wasRunning = _running;
        if (wasRunning)
        {
            StopMonitoring(showStatus: false);
        }

        using var dialog = new ConfigForm(_config.Clone(), _configService.ConfigPath);
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.ResultConfig is not null)
        {
            _config = dialog.ResultConfig;
            _configService.Save(_config);
            RebuildFromConfiguration();
        }

        UpdateButtonState();
    }

    private void ApplyBluetoothReading(BluetoothReading reading)
    {
        if (reading.SensorIndex is < 1 or > 4)
        {
            return;
        }

        var index = reading.SensorIndex;
        var zero = index - 1;
        _lastBluetooth[zero] = reading;
        _bluetoothDeltas[zero].AddReading(reading.TemperatureC, reading.Time);
        AddGraphPoint(SeriesKey(index, "BT"), reading.TemperatureC, reading.Time);
        UpdateCard(index);
        WriteCsv(index, "Bluetooth");
        SetStatus($"Bluetooth {reading.SensorName}: {reading.TemperatureC:F1} C, {reading.HumidityPercent}% RH, dew point {reading.DewPointC:F1} C");
    }

    private void ApplyUsbTemperatures(UsbTemperaturesReceivedEventArgs e)
    {
        foreach (var pair in _config.SensorPairs.Where(p => p.Enabled).OrderBy(p => p.Index))
        {
            var channelIndex = pair.UsbChannel - 1;
            if (channelIndex < 0 || channelIndex >= e.TemperaturesC.Length)
            {
                continue;
            }

            var temp = e.TemperaturesC[channelIndex];
            if (!IsValidUsbTemperature(temp))
            {
                continue;
            }

            var reading = new UsbReading
            {
                Time = e.Time,
                SensorIndex = pair.Index,
                Channel = pair.UsbChannel,
                SensorName = pair.Name,
                TemperatureC = temp
            };

            var zero = pair.Index - 1;
            _lastUsb[zero] = reading;
            _usbDeltas[zero].AddReading(temp, e.Time);
            AddGraphPoint(SeriesKey(pair.Index, "USB"), temp, e.Time);
            UpdateCard(pair.Index);
            WriteCsv(pair.Index, "USB");
        }

        SetStatus($"USB update from {e.SourcePort}: {string.Join(", ", e.TemperaturesC.Select(t => IsValidUsbTemperature(t) ? t.ToString("F1", CultureInfo.CurrentCulture) + " C" : "invalid"))}");
    }

    private void UpdateCard(int index)
    {
        if (!_cardsByIndex.TryGetValue(index, out var card))
        {
            return;
        }

        var pair = _config.SensorPairs.FirstOrDefault(p => p.Index == index);
        if (pair is null)
        {
            return;
        }

        var zero = index - 1;
        var bluetooth = _lastBluetooth[zero];
        var usb = _lastUsb[zero];
        var color = ColorUtilities.SensorPalette[zero % ColorUtilities.SensorPalette.Length];
        var btDeltas = GetDeltaDisplay(_bluetoothDeltas[zero], bluetooth?.Time);
        var usbDeltas = GetDeltaDisplay(_usbDeltas[zero], usb?.Time);
        var bluetoothHeat = bluetooth is null
            ? null
            : HeatStressEvaluator.Calculate(bluetooth.TemperatureC, bluetooth.HumidityPercent, _config);
        var usbHeat = usb is null || bluetooth is null
            ? null
            : HeatStressEvaluator.Calculate(usb.TemperatureC, bluetooth.HumidityPercent, _config);

        card.UpdateDisplay(pair, bluetooth, btDeltas, bluetoothHeat, usb, usbDeltas, usbHeat, color);

        // Compute averages independently and handle nulls
        var bluetoothAvg = BluetoothReading.Average(_lastBluetooth);
        var usbAvg = UsbReading.Average(_lastUsb);

        HeatStressResult? avgHeat = null;
        if (bluetoothAvg != null && usbAvg != null)
        {
            avgHeat = HeatStressEvaluator.Calculate(usbAvg.TemperatureC, bluetoothAvg.HumidityPercent, _config);
        }

        AverageCard.UpdateDisplay(bluetoothAvg, usbAvg, avgHeat);
        if (usbAvg != null)
        {
            _binsPanel.UpdateBin(usbAvg.TemperatureC, DateTimeOffset.Now);
        }

        if (bluetoothAvg != null)
        {
            // Add average humidity to graph (always add, even if zero)
            AddGraphPoint("AvgHumidity", bluetoothAvg.HumidityPercent, bluetoothAvg.Time, isHumidity: true);
        }

        if (bluetoothAvg != null)
        {
            // Add average dew point to graph (always add, even if zero)
            AddGraphPoint("AvgDewPoint", bluetoothAvg.DewPointC, bluetoothAvg.Time, isHumidity: true); //not humidity, but we want to use the secondary axis
        }
    }

    private void WriteCsv(int index, string source)
    {
        if (index is < 1 or > 4)
        {
            return;
        }

        var pair = _config.SensorPairs.FirstOrDefault(p => p.Index == index);
        if (pair is null)
        {
            return;
        }

        var zero = index - 1;
        var bluetooth = _lastBluetooth[zero];
        var usb = _lastUsb[zero];
        var btDeltas = GetDeltaDisplay(_bluetoothDeltas[zero], bluetooth?.Time);
        var usbDeltas = GetDeltaDisplay(_usbDeltas[zero], usb?.Time);
        var bluetoothHeat = bluetooth is null
            ? null
            : HeatStressEvaluator.Calculate(bluetooth.TemperatureC, bluetooth.HumidityPercent, _config);
        var usbHeat = usb is null || bluetooth is null
            ? null
            : HeatStressEvaluator.Calculate(usb.TemperatureC, bluetooth.HumidityPercent, _config);

        _csvLogger.WriteCombined(
            index,
            pair.Name,
            source,
            bluetooth,
            usb,
            btDeltas,
            bluetoothHeat,
            usbDeltas,
            usbHeat);
    }

    private void AddGraphPoint(string key, double temperatureC, DateTimeOffset time, bool isHumidity = false)
    {
        if (!_seriesByKey.TryGetValue(key, out var series))
        {
            return;
        }

        var minInterval = TimeSpan.FromSeconds(Math.Max(1, _config.GraphSampleIntervalSeconds));
        if (_lastGraphPointByKey.TryGetValue(key, out var last) && time - last < minInterval)
        {
            return;
        }

        _lastGraphPointByKey[key] = time;
        if (isHumidity)
        {
            series.Points.Add(new GraphPoint(time, 0, HumidityPercent: (int)temperatureC));
        }
        else
        {
            series.Points.Add(new GraphPoint(time, temperatureC));
        }
        PruneOldGraphData(series);
        _graph.Invalidate();
    }

    private void PruneOldGraphData(GraphSeries series)
    {
        var cutoff = DateTimeOffset.Now.AddMinutes(-Math.Max(1, _config.GraphHistoryMinutes) - 5);
        series.Points.RemoveAll(p => p.Time < cutoff);
    }

    private static string GetDeltaDisplay(TemperatureDeltaCalculator calculator, DateTimeOffset? now)
    {
        if (now is null)
        {
            return string.Empty;
        }

        return FormatDelta(calculator.GetDeltaSeconds(60, now));

        //var delta60 = FormatDelta(calculator.GetDeltaSeconds(60, now));
        //var delta120 = FormatDelta(calculator.GetDeltaSeconds(120, now));
        //var textParts = new List<string>();
        //if (!string.IsNullOrEmpty(delta60))
        //{
        //    textParts.Add($"d60:{delta60}C");
        //}

        //if (!string.IsNullOrEmpty(delta120))
        //{
        //    textParts.Add($"d120:{delta120}C");
        //}

        //return new DeltaDisplay(delta60, delta120, string.Join(" ", textParts));
    }

    private static string FormatDelta(double? value)
    {
        return value.HasValue ? value.Value.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) : string.Empty;
    }

    private static bool IsValidUsbTemperature(double temperatureC)
    {
        return !double.IsNaN(temperatureC) && !double.IsInfinity(temperatureC) && temperatureC > -100.0 && temperatureC < 280.0;
    }

    private static string SeriesKey(int index, string source)
    {
        return $"{source}{index}";
    }

    private void UpdateModeLabel()
    {
        if (_config.TestMode)
        {
            _modeLabel.Text = "TEST MODE";
            _modeLabel.BackColor = Color.FromArgb(255, 245, 180);
            _modeLabel.ForeColor = Color.Black;
        }
        else
        {
            _modeLabel.Text = "LIVE MODE";
            _modeLabel.BackColor = Color.FromArgb(215, 245, 215);
            _modeLabel.ForeColor = Color.Black;
        }
    }

    private void UpdateButtonState()
    {
        _startButton.Enabled = !_running;
        _stopButton.Enabled = _running;
        _clearButton.Enabled = _seriesByKey.Count > 0;
        _configButton.Enabled = true;
    }

    private void SetStatus(string message)
    {
        if (!IsDisposed)
        {
            _statusLabel.Text = message;
        }
    }

    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        SuspendLayout();
        // 
        // MainForm
        // 
        ClientSize = new Size(278, 244);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Name = "MainForm";
        ResumeLayout(false);

    }

    private void RunOnUiThread(Action action)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired && IsHandleCreated)
        {
            BeginInvoke(action);
            return;
        }

        action();
    }

    //private readonly record struct DeltaDisplay(string Delta60, string Delta120, string Text);
}
