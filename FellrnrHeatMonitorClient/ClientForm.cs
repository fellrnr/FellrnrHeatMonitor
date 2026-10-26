using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace FellrnrHeatMonitorClient;

internal sealed class ClientForm : Form
{
    private readonly TextBox _serverTextBox = new() { Width = 220 };
    private readonly Button _refreshButton = new() { Text = "Refresh", Width = 90 };
    private readonly Label _temperatureLabel = new() { AutoSize = true, Text = "Temperature: --" };
    private readonly Label _humidityLabel = new() { AutoSize = true, Text = "Humidity: --" };
    private readonly Label _timeLabel = new() { AutoSize = true, Text = "Last reading: --" };
    private readonly Label _statusLabel = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly TemperatureHumidityGraph _graph = new() { Height = 220, Dock = DockStyle.Fill };
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 5000 };
    private readonly string _configPath;
    private bool _requestInProgress;

    public ClientForm()
    {
        Text = "Fellrnr Heat Monitor Client";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 560;
        Height = 550;
        MinimizeBox = false;

        var appDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FellrnrHeatMonitorClient");
        _configPath = Path.Combine(appDirectory, "config.json");
        _serverTextBox.Text = LoadServerAddress();

        BuildUi();
        _refreshButton.Click += async (_, _) => await RefreshReadingAsync();
        _refreshTimer.Tick += async (_, _) => await RefreshReadingAsync();
        Shown += async (_, _) =>
        {
            _refreshTimer.Start();
            await RefreshReadingAsync();
        };
        FormClosed += (_, _) =>
        {
            _refreshTimer.Stop();
            _httpClient.Dispose();
        };
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(16),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var serverPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        serverPanel.Controls.Add(new Label { Text = "Server IP:", AutoSize = true, Padding = new Padding(0, 6, 8, 0) });
        serverPanel.Controls.Add(_serverTextBox);
        serverPanel.Controls.Add(_refreshButton);
        root.Controls.Add(serverPanel, 0, 0);
        root.Controls.Add(_statusLabel, 0, 1);
        root.Controls.Add(_temperatureLabel, 0, 2);
        root.Controls.Add(_humidityLabel, 0, 3);
        root.Controls.Add(_timeLabel, 0, 4);
        root.Controls.Add(_graph, 0, 5);
        Controls.Add(root);
    }

    private async Task RefreshReadingAsync()
    {
        if (_requestInProgress)
        {
            return;
        }

        var server = _serverTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(server))
        {
            _statusLabel.Text = "Enter the server IP address.";
            return;
        }

        _requestInProgress = true;
        _refreshButton.Enabled = false;
        SaveServerAddress(server);
        try
        {
            var url = $"http://{server}:5080/api/reading";
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync();
            var reading = await JsonSerializer.DeserializeAsync<RestReadingSnapshot>(stream);
            if (reading is null)
            {
                throw new InvalidOperationException("The server returned an empty response.");
            }

            var sampleTime = reading.LastReadingTime ?? DateTimeOffset.Now;
            var temperaureDisplay = reading.AverageTemperatureC is double temperature
                ? $"Temperature: {temperature:F1} °C"
                : "Temperature: no reading";
            var humidityDisplay = reading.AverageHumidityPercent is double humidity
                ? $"Humidity: {humidity:F0}%"
                : "Humidity: no reading";

            _temperatureLabel.Text = temperaureDisplay;
            _humidityLabel.Text = humidityDisplay;
            _timeLabel.Text = reading.LastReadingTime is DateTimeOffset time
                ? $"Last reading: {time.LocalDateTime:G}"
                : "Last reading: no reading";
            _statusLabel.Text = $"Connected to {server}";

            if (reading.AverageTemperatureC is not null || reading.AverageHumidityPercent is not null)
            {
                _graph.AddSample(new GraphSample(sampleTime, reading.AverageTemperatureC, reading.AverageHumidityPercent));
            }

            if (reading.AverageTemperatureC is double temperatureValue && !double.IsNaN(temperatureValue))
            {
                _temperatureLabel.Text = $"Temperature: {temperatureValue:F1} °C";
            }

            if (reading.AverageHumidityPercent is double humidityValue && !double.IsNaN(humidityValue))
            {
                _humidityLabel.Text = $"Humidity: {humidityValue:F0}%";
            }

            if (reading.LastReadingTime is DateTimeOffset timeValue)
            {
                _timeLabel.Text = $"Last reading: {timeValue.LocalDateTime:G}";
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
        {
            _statusLabel.Text = $"Unable to read from {server}: {ex.Message}";
        }
        finally
        {
            _requestInProgress = false;
            _refreshButton.Enabled = true;
        }
    }

    private string LoadServerAddress()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var config = JsonSerializer.Deserialize<ClientConfig>(File.ReadAllText(_configPath));
                if (!string.IsNullOrWhiteSpace(config?.ServerAddress))
                {
                    return config.ServerAddress;
                }
            }
        }
        catch
        {
        }

        return "127.0.0.1";
    }

    private void SaveServerAddress(string serverAddress)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            File.WriteAllText(_configPath, JsonSerializer.Serialize(new ClientConfig { ServerAddress = serverAddress }));
        }
        catch
        {
        }
    }

    private sealed class ClientConfig
    {
        public string ServerAddress { get; set; } = "127.0.0.1";
    }

    private sealed class GraphSample
    {
        public GraphSample(DateTimeOffset time, double? temperatureC, double? humidityPercent)
        {
            Time = time;
            TemperatureC = temperatureC;
            HumidityPercent = humidityPercent;
        }

        public DateTimeOffset Time { get; }
        public double? TemperatureC { get; }
        public double? HumidityPercent { get; }
    }

    private sealed class RestReadingSnapshot
    {
        [JsonPropertyName("averageTemperatureC")]
        public double? AverageTemperatureC { get; set; }

        [JsonPropertyName("averageHumidityPercent")]
        public double? AverageHumidityPercent { get; set; }

        [JsonPropertyName("lastReadingTime")]
        public DateTimeOffset? LastReadingTime { get; set; }
    }

    private sealed class TemperatureHumidityGraph : Control
    {
        private readonly List<GraphSample> _samples = new();
        private readonly object _sampleLock = new();

        public TemperatureHumidityGraph()
        {
            DoubleBuffered = true;
            BackColor = Color.Black;
            ForeColor = Color.White;
            Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Regular);
        }

        public void AddSample(GraphSample sample)
        {
            lock (_sampleLock)
            {
                _samples.Add(sample);
                var cutoff = DateTimeOffset.Now.AddHours(-1);
                _samples.RemoveAll(s => s.Time < cutoff);
                if (_samples.Count > 300)
                {
                    _samples.RemoveRange(0, _samples.Count - 300);
                }
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            var plot = new Rectangle(52, 18, Math.Max(20, Width - 120), Math.Max(20, Height - 56));
            var now = DateTimeOffset.Now;
            var history = TimeSpan.FromHours(1);
            var cutoff = now.Subtract(history);

            lock (_sampleLock)
            {
                var visible = _samples
                    .Where(sample => sample.Time >= cutoff)
                    .OrderBy(sample => sample.Time)
                    .ToList();

                DrawAxes(g, plot, cutoff, history, visible);
                DrawSeries(g, plot, cutoff, history, visible);
            }
        }

        private void DrawAxes(Graphics g, Rectangle plot, DateTimeOffset cutoff, TimeSpan history, IReadOnlyList<GraphSample> visible)
        {
            using var axisPen = new Pen(Color.FromArgb(120, 120, 120), 1);
            using var gridPen = new Pen(Color.FromArgb(60, 60, 60), 1);
            using var textBrush = new SolidBrush(ForeColor);
            using var dimBrush = new SolidBrush(Color.FromArgb(180, 180, 180));

            g.DrawRectangle(axisPen, plot);

            var minTemp = visible.Count > 0 && visible.Any(s => s.TemperatureC.HasValue)
                ? Math.Floor(visible.Where(s => s.TemperatureC.HasValue).Min(s => s.TemperatureC!.Value) - 2)
                : 0;
            var maxTemp = visible.Count > 0 && visible.Any(s => s.TemperatureC.HasValue)
                ? Math.Ceiling(visible.Where(s => s.TemperatureC.HasValue).Max(s => s.TemperatureC!.Value) + 2)
                : 40;
            if (maxTemp <= minTemp)
            {
                maxTemp = minTemp + 10;
            }

            for (var i = 0; i <= 5; i++)
            {
                var y = plot.Bottom - (int)((i / 5.0) * plot.Height);
                g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
                var value = minTemp + (maxTemp - minTemp) * (i / 5.0);
                g.DrawString($"{value:F0}°", Font, textBrush, 4, y - 8);
                var humidity = 100 - (i * 20);
                g.DrawString($"{humidity}%", Font, dimBrush, plot.Right + 8, y - 8);
            }

            for (var i = 0; i <= 6; i++)
            {
                var x = plot.Left + (int)((i / 6.0) * plot.Width);
                g.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
                var elapsed = history.TotalMinutes * (1.0 - i / 6.0);
                var label = i == 6 ? "now" : $"-{elapsed:F0}m";
                g.DrawString(label, Font, dimBrush, x - 14, plot.Bottom + 6);
            }

            using var titleFont = new Font(Font.FontFamily, Font.Size + 2, FontStyle.Bold);
            g.DrawString("Temperature and humidity", titleFont, textBrush, plot.Left, 2);

            using var legendPen = new Pen(Color.Red, 3);
            g.DrawLine(legendPen, plot.Right + 8, 4, plot.Right + 28, 4);
            g.DrawString("Temp", Font, textBrush, plot.Right + 36, 0);

            using var humidityPen = new Pen(Color.DodgerBlue, 3);
            g.DrawLine(humidityPen, plot.Right + 8, 22, plot.Right + 28, 22);
            g.DrawString("Humidity", Font, textBrush, plot.Right + 36, 18);
        }

        private void DrawSeries(Graphics g, Rectangle plot, DateTimeOffset cutoff, TimeSpan history, IReadOnlyList<GraphSample> visible)
        {
            if (visible.Count == 0)
            {
                using var noDataBrush = new SolidBrush(Color.Gray);
                g.DrawString("Waiting for readings...", Font, noDataBrush, plot.Left + 12, plot.Top + 12);
                return;
            }

            var minTemp = visible.Any(s => s.TemperatureC.HasValue)
                ? Math.Floor(visible.Where(s => s.TemperatureC.HasValue).Min(s => s.TemperatureC!.Value) - 2)
                : 0;
            var maxTemp = visible.Any(s => s.TemperatureC.HasValue)
                ? Math.Ceiling(visible.Where(s => s.TemperatureC.HasValue).Max(s => s.TemperatureC!.Value) + 2)
                : 40;
            if (maxTemp <= minTemp)
            {
                maxTemp = minTemp + 10;
            }

            var tempPoints = visible
                .Where(s => s.TemperatureC.HasValue)
                .Select(s => ProjectPoint(s.Time, s.TemperatureC!.Value, plot, cutoff, history, minTemp, maxTemp, isHumidity: false))
                .ToArray();

            var humidityPoints = visible
                .Where(s => s.HumidityPercent.HasValue)
                .Select(s => ProjectPoint(s.Time, s.HumidityPercent!.Value, plot, cutoff, history, 0, 100, isHumidity: true))
                .ToArray();

            if (tempPoints.Length > 1)
            {
                using var tempPen = new Pen(Color.Red, 2.0f);
                g.DrawLines(tempPen, tempPoints);
            }
            else if (tempPoints.Length == 1)
            {
                using var tempBrush = new SolidBrush(Color.Red);
                g.FillEllipse(tempBrush, tempPoints[0].X - 3, tempPoints[0].Y - 3, 6, 6);
            }

            if (humidityPoints.Length > 1)
            {
                using var humidityPen = new Pen(Color.DodgerBlue, 2.0f);
                g.DrawLines(humidityPen, humidityPoints);
            }
            else if (humidityPoints.Length == 1)
            {
                using var humidityBrush = new SolidBrush(Color.DodgerBlue);
                g.FillEllipse(humidityBrush, humidityPoints[0].X - 3, humidityPoints[0].Y - 3, 6, 6);
            }
        }

        private static PointF ProjectPoint(DateTimeOffset time, double value, Rectangle plot, DateTimeOffset cutoff, TimeSpan history, double minValue, double maxValue, bool isHumidity)
        {
            var xRatio = Math.Clamp((time - cutoff).TotalSeconds / history.TotalSeconds, 0.0, 1.0);
            var yRange = maxValue - minValue;
            var yRatio = isHumidity
                ? Math.Clamp(value / 100.0, 0.0, 1.0)
                : Math.Clamp((value - minValue) / Math.Max(0.1, yRange), 0.0, 1.0);
            var x = plot.Left + (float)(xRatio * plot.Width);
            var y = plot.Bottom - (float)(yRatio * plot.Height);
            return new PointF(x, y);
        }
    }
}




















































































































































































































































































































































































































































































































































































































































































































































































































