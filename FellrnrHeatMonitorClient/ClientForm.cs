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
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 5000 };
    private readonly string _configPath;
    private bool _requestInProgress;

    public ClientForm()
    {
        Text = "Fellrnr Heat Monitor Client";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 460;
        Height = 260;
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
            RowCount = 5,
            Padding = new Padding(16),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        var serverPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        serverPanel.Controls.Add(new Label { Text = "Server IP:", AutoSize = true, Padding = new Padding(0, 6, 8, 0) });
        serverPanel.Controls.Add(_serverTextBox);
        serverPanel.Controls.Add(_refreshButton);
        root.Controls.Add(serverPanel, 0, 0);
        root.Controls.Add(_statusLabel, 0, 1);
        root.Controls.Add(_temperatureLabel, 0, 2);
        root.Controls.Add(_humidityLabel, 0, 3);
        root.Controls.Add(_timeLabel, 0, 4);
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

            _temperatureLabel.Text = reading.AverageTemperatureC is double temperature
                ? $"Temperature: {temperature:F1} °C"
                : "Temperature: no reading";
            _humidityLabel.Text = reading.AverageHumidityPercent is double humidity
                ? $"Humidity: {humidity:F0}%"
                : "Humidity: no reading";
            _timeLabel.Text = reading.LastReadingTime is DateTimeOffset time
                ? $"Last reading: {time.LocalDateTime:G}"
                : "Last reading: no reading";
            _statusLabel.Text = $"Connected to {server}";
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

    private sealed class RestReadingSnapshot
    {
        [JsonPropertyName("averageTemperatureC")]
        public double? AverageTemperatureC { get; set; }

        [JsonPropertyName("averageHumidityPercent")]
        public double? AverageHumidityPercent { get; set; }

        [JsonPropertyName("lastReadingTime")]
        public DateTimeOffset? LastReadingTime { get; set; }
    }
}
