# FellrnrHeatMonitor

A combined C# WinForms application that merges the SwitchBot Bluetooth meter monitor with the CD50 / Landtek four-channel USB thermometer monitor.

## What this project does

- Shows one graph containing eight temperature series:
  - 4 USB thermocouple channels from the CD50 thermometer.
  - 4 Bluetooth SwitchBot meter temperatures.
- Shows four sensor-pair cards. Each card combines one USB channel with one Bluetooth meter.
- Each card displays:
  - Sensor name.
  - Bluetooth temperature with 60 second and 120 second deltas.
  - Bluetooth humidity.
  - Bluetooth dew point.
  - Bluetooth heat-stress result from `FellrnrHeatCalculator`.
  - USB temperature with 60 second and 120 second deltas.
  - USB heat-stress result calculated from USB temperature plus the paired Bluetooth humidity.
- Includes a single Configuration dialog for USB, Bluetooth, graph, heat-stress, and pairing settings.
- Includes Test mode, enabled by default, that generates both USB and Bluetooth readings when hardware is not available.
- Writes a combined CSV file to `%LOCALAPPDATA%\FellrnrHeatMonitor\heat_monitor_readings.csv`.
- Runs a lightweight REST server on all interfaces at port `5080`.
- Includes a separate WinForms client for viewing the REST reading from another PC.

## Build and run

Requirements:

- Windows 10 version 2004 or later, or Windows 11.
 - .NET 10 SDK or Visual Studio 2026 with the .NET desktop workload.
- Bluetooth LE adapter for real SwitchBot readings.
- CD50 / Landtek 4-channel USB thermometer for real USB readings.

Build from a Developer PowerShell:

```powershell
dotnet restore
dotnet build -c Release
dotnet run --project .\FellrnrHeatMonitor.csproj
```

Or open `FellrnrHeatMonitor.sln` in Visual Studio and press F5.

## REST server and client

When the monitor starts, it serves the current average-card values at:

```text
GET http://<monitor-pc>:5080/api/reading
```

The JSON response contains `averageTemperatureC`, `averageHumidityPercent`, and
`lastReadingTime`. The temperature is the average of the current USB readings,
the humidity is the average of the current Bluetooth readings, and the time is
the most recent timestamp contributing to either value. Values are `null` until
the corresponding readings are available.

Build and run `FellrnrHeatMonitorClient\FellrnrHeatMonitorClient.csproj` on the
client PC. Enter the monitor PC's IP address and click **Refresh**; the address
is saved under `%LOCALAPPDATA%\FellrnrHeatMonitorClient\config.json` and is
polled every five seconds thereafter. Windows Firewall may need an inbound TCP
rule for port 5080, and the monitor must be allowed to listen on that port.

## Configuration

The configuration file is stored at:

```text
%LOCALAPPDATA%\FellrnrHeatMonitor\config.json
```

The Configuration dialog includes:

- Test mode on/off.
- Auto-start on/off.
- USB COM port or `AUTO`.
- USB polling interval.
- Graph history and graph sample interval.
- Heat-stress parameters passed to `FellrnrHeatCalculator`.
- Four fixed sensor-pair rows. Each row maps a USB channel to one Bluetooth MAC address.

When Test mode is off, each enabled pair must have a valid Bluetooth MAC address.

## Notes

The project is source-only. I could not run a local `dotnet build` in this environment because the .NET SDK is not installed here, so build it on a Windows machine with the prerequisites above.

## Source attribution

This combined project was built from the structure and logic of these repositories:

- `fellrnr/SwitchBotMeterWinForms`: WinForms BLE SwitchBot monitor, SwitchBot advertisement parser, dew point logic, delta logic, graph/card concepts, and `FellrnrHeatCalculator`.
- `fellrnr/cd50-thermometer-csharp`: CD50 serial thermometer protocol and USB temperature delta behavior.

Check the upstream repository licenses before redistribution. The GitHub page for `fellrnr/cd50-thermometer-csharp` currently identifies the project license as GPL-3.0.
