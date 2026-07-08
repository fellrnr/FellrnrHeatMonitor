using System.Text.Json;
using System.Text.Json.Serialization;
using FellrnrHeatMonitor.Models;

namespace FellrnrHeatMonitor.Services;

internal sealed class AppConfigService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string AppDirectory { get; }
    public string ConfigPath { get; }
    public string CsvPath { get; }

    public AppConfigService()
    {
        AppDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FellrnrHeatMonitor");
        ConfigPath = Path.Combine(AppDirectory, "config.json");
        CsvPath = Path.Combine(AppDirectory, "heat_monitor_readings.csv");
    }

    public AppConfig Load()
    {
        Directory.CreateDirectory(AppDirectory);
        if (!File.Exists(ConfigPath))
        {
            var defaultConfig = new AppConfig();
            defaultConfig.EnsureFourPairs();
            Save(defaultConfig);
            return defaultConfig;
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions) ?? new AppConfig();
            config.EnsureFourPairs();
            return config;
        }
        catch
        {
            var fallback = new AppConfig();
            fallback.EnsureFourPairs();
            return fallback;
        }
    }

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(AppDirectory);
        config.EnsureFourPairs();
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        File.WriteAllText(ConfigPath, json);
    }
}
