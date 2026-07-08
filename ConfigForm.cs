using System.Drawing;
using System.Windows.Forms;
using FellrnrHeatMonitor.Models;
using FellrnrHeatMonitor.Services;
using FellrnrHeatMonitor.Usb;

namespace FellrnrHeatMonitor;

internal sealed class ConfigForm : Form
{
    private readonly string _configPath;
    private readonly AppConfig _initialConfig;
    private readonly CheckBox _testModeCheckBox = new() { Text = "Test mode: generate USB and Bluetooth data", AutoSize = true };
    private readonly CheckBox _autoStartCheckBox = new() { Text = "Auto-start monitoring when application opens", AutoSize = true };
    private readonly ComboBox _portComboBox = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 120 };
    private readonly NumericUpDown _pollIntervalNumeric = new() { Minimum = 200, Maximum = 60000, Increment = 100, Width = 90 };
    private readonly NumericUpDown _historyNumeric = new() { Minimum = 1, Maximum = 1440, Increment = 5, Width = 90 };
    private readonly NumericUpDown _sampleIntervalNumeric = new() { Minimum = 1, Maximum = 300, Increment = 1, Width = 90 };
    private readonly NumericUpDown _airVelocityNumeric = new() { Minimum = 0.1M, Maximum = 20, Increment = 0.1M, DecimalPlaces = 1, Width = 90 };
    private readonly NumericUpDown _powerNumeric = new() { Minimum = 0, Maximum = 2000, Increment = 5, DecimalPlaces = 0, Width = 90 };
    private readonly NumericUpDown _heightNumeric = new() { Minimum = 50, Maximum = 260, Increment = 1, DecimalPlaces = 0, Width = 90 };
    private readonly NumericUpDown _weightNumeric = new() { Minimum = 20, Maximum = 250, Increment = 1, DecimalPlaces = 0, Width = 90 };
    private readonly ComboBox _tempUnitsComboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly ComboBox _sodiumComboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly DataGridView _pairsGrid = new() { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false };
    private readonly Label _configPathLabel = new() { Dock = DockStyle.Fill, ForeColor = Color.DimGray, AutoEllipsis = true };

    public ConfigForm(AppConfig config, string configPath)
    {
        _initialConfig = config.Clone();
        _initialConfig.EnsureFourPairs();
        _configPath = configPath;
        Text = "FellrnrHeatMonitor Configuration";
        StartPosition = FormStartPosition.CenterParent;
        Width = 880;
        Height = 700; //AI Guessed 670
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.Sizable;

        BuildUi();
        LoadValues(_initialConfig);
    }

    public AppConfig? ResultConfig { get; private set; }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 160)); //AI guessed 130
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 135));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); //AI guessed 46

        root.Controls.Add(BuildHardwareGroup(), 0, 0);
        root.Controls.Add(BuildHeatGroup(), 0, 1);
        root.Controls.Add(BuildPairsGroup(), 0, 2);

        _configPathLabel.Text = $"Config: {_configPath}";
        root.Controls.Add(_configPathLabel, 0, 3);
        root.Controls.Add(BuildButtons(), 0, 4);
        Controls.Add(root);
    }

    private Control BuildHardwareGroup()
    {
        var group = new GroupBox { Text = "Hardware and graph", Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 3,
            Padding = new Padding(10)
        };
        for (var i = 0; i < 6; i++)
        {
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
        }

        layout.Controls.Add(_testModeCheckBox, 0, 0);
        layout.SetColumnSpan(_testModeCheckBox, 3);
        layout.Controls.Add(_autoStartCheckBox, 3, 0);
        layout.SetColumnSpan(_autoStartCheckBox, 3);

        layout.Controls.Add(new Label { Text = "USB COM port", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        layout.Controls.Add(_portComboBox, 1, 1);
        var refreshPortsButton = new Button { Text = "Refresh", Width = 85, Height = 26 };
        refreshPortsButton.Click += (_, _) => RefreshPorts(_portComboBox.Text);
        layout.Controls.Add(refreshPortsButton, 2, 1);
        var detectButton = new Button { Text = "Auto-detect", Width = 100, Height = 26 };
        detectButton.Click += DetectButtonOnClick;
        layout.Controls.Add(detectButton, 3, 1);

        layout.Controls.Add(new Label { Text = "USB poll ms", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
        layout.Controls.Add(_pollIntervalNumeric, 1, 2);
        layout.Controls.Add(new Label { Text = "Graph history min", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 2, 2);
        layout.Controls.Add(_historyNumeric, 3, 2);
        layout.Controls.Add(new Label { Text = "Graph sample sec", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 4, 2);
        layout.Controls.Add(_sampleIntervalNumeric, 5, 2);

        group.Controls.Add(layout);
        return group;
    }

    private Control BuildHeatGroup()
    {
        var group = new GroupBox { Text = "Heat stress parameters used by FellrnrHeatCalculator", Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 2,
            Padding = new Padding(10)
        };
        for (var i = 0; i < 6; i++)
        {
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
        }

        _tempUnitsComboBox.Items.AddRange(new object[] { "Centigrade", "Fahrenheit" });
        _sodiumComboBox.Items.AddRange(Enum.GetNames(typeof(FellrnrHeatCalculator.SodiumLosses)).Cast<object>().ToArray());

        layout.Controls.Add(new Label { Text = "Air velocity m/s", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        layout.Controls.Add(_airVelocityNumeric, 1, 0);
        layout.Controls.Add(new Label { Text = "Cycling power W", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 2, 0);
        layout.Controls.Add(_powerNumeric, 3, 0);
        layout.Controls.Add(new Label { Text = "Units", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 4, 0);
        layout.Controls.Add(_tempUnitsComboBox, 5, 0);

        layout.Controls.Add(new Label { Text = "Height cm", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        layout.Controls.Add(_heightNumeric, 1, 1);
        layout.Controls.Add(new Label { Text = "Weight kg", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 2, 1);
        layout.Controls.Add(_weightNumeric, 3, 1);
        layout.Controls.Add(new Label { Text = "Sodium losses", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 4, 1);
        layout.Controls.Add(_sodiumComboBox, 5, 1);

        group.Controls.Add(layout);
        return group;
    }

    private Control BuildPairsGroup()
    {
        var group = new GroupBox { Text = "Sensor pairs: each row combines one USB channel with one Bluetooth sensor", Dock = DockStyle.Fill };
        _pairsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _pairsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _pairsGrid.MultiSelect = false;
        _pairsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Enabled", HeaderText = "Enabled", FillWeight = 55 });
        _pairsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Index", HeaderText = "Pair", ReadOnly = true, FillWeight = 45 });
        _pairsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Sensor name", FillWeight = 145 });
        _pairsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "UsbChannel", HeaderText = "USB channel", ReadOnly = true, FillWeight = 70 });
        _pairsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "BluetoothMac", HeaderText = "Bluetooth MAC address", FillWeight = 180 });
        group.Controls.Add(_pairsGrid);
        return group;
    }

    private Control BuildButtons()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0),
            WrapContents = false
        };

        var okButton = new Button { Text = "OK", Width = 92, DialogResult = DialogResult.None };
        okButton.Click += OkButtonOnClick;
        var cancelButton = new Button { Text = "Cancel", Width = 92, DialogResult = DialogResult.Cancel };
        panel.Controls.Add(okButton);
        panel.Controls.Add(cancelButton);
        AcceptButton = okButton;
        CancelButton = cancelButton;
        return panel;
    }

    private void LoadValues(AppConfig config)
    {
        _testModeCheckBox.Checked = config.TestMode;
        _autoStartCheckBox.Checked = config.AutoStart;
        RefreshPorts(config.UsbComPort);
        _pollIntervalNumeric.Value = Math.Clamp(config.UsbPollIntervalMs, (int)_pollIntervalNumeric.Minimum, (int)_pollIntervalNumeric.Maximum);
        _historyNumeric.Value = Math.Clamp(config.GraphHistoryMinutes, (int)_historyNumeric.Minimum, (int)_historyNumeric.Maximum);
        _sampleIntervalNumeric.Value = Math.Clamp(config.GraphSampleIntervalSeconds, (int)_sampleIntervalNumeric.Minimum, (int)_sampleIntervalNumeric.Maximum);
        _airVelocityNumeric.Value = ClampDecimal((decimal)config.HeatAirVelocityMetersPerSecond, _airVelocityNumeric.Minimum, _airVelocityNumeric.Maximum);
        _powerNumeric.Value = ClampDecimal((decimal)config.HeatCyclingPowerWatts, _powerNumeric.Minimum, _powerNumeric.Maximum);
        _heightNumeric.Value = ClampDecimal((decimal)config.HeatHeightCm, _heightNumeric.Minimum, _heightNumeric.Maximum);
        _weightNumeric.Value = ClampDecimal((decimal)config.HeatWeightKg, _weightNumeric.Minimum, _weightNumeric.Maximum);
        _tempUnitsComboBox.SelectedItem = config.HeatTemperatureUnits == "Fahrenheit" ? "Fahrenheit" : "Centigrade";
        _sodiumComboBox.SelectedItem = config.HeatSodiumLosses.ToString();

        _pairsGrid.Rows.Clear();
        foreach (var pair in config.SensorPairs.OrderBy(p => p.Index))
        {
            _pairsGrid.Rows.Add(pair.Enabled, pair.Index, pair.Name, pair.UsbChannel, pair.BluetoothMacAddress);
        }
    }

    private void RefreshPorts(string? selectedPort)
    {
        var selected = string.IsNullOrWhiteSpace(selectedPort) ? "AUTO" : selectedPort.Trim();
        _portComboBox.Items.Clear();
        _portComboBox.Items.Add("AUTO");
        foreach (var port in CD50Thermometer.GetAvailablePorts())
        {
            _portComboBox.Items.Add(port);
        }

        if (!_portComboBox.Items.Contains(selected))
        {
            _portComboBox.Items.Add(selected);
        }

        _portComboBox.SelectedItem = selected;
    }

    private void DetectButtonOnClick(object? sender, EventArgs e)
    {
        try
        {
            Cursor = Cursors.WaitCursor;
            var found = CD50Thermometer.FindAvailablePort();
            if (string.IsNullOrWhiteSpace(found))
            {
                MessageBox.Show(this, "No CD50 thermometer responded on available COM ports.", "Auto-detect", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            RefreshPorts(found);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Auto-detect failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void OkButtonOnClick(object? sender, EventArgs e)
    {
        var result = _initialConfig.Clone();
        result.TestMode = _testModeCheckBox.Checked;
        result.AutoStart = _autoStartCheckBox.Checked;
        result.UsbComPort = string.IsNullOrWhiteSpace(_portComboBox.Text) ? "AUTO" : _portComboBox.Text.Trim();
        result.UsbPollIntervalMs = (int)_pollIntervalNumeric.Value;
        result.GraphHistoryMinutes = (int)_historyNumeric.Value;
        result.GraphSampleIntervalSeconds = (int)_sampleIntervalNumeric.Value;
        result.HeatAirVelocityMetersPerSecond = (double)_airVelocityNumeric.Value;
        result.HeatCyclingPowerWatts = (double)_powerNumeric.Value;
        result.HeatHeightCm = (double)_heightNumeric.Value;
        result.HeatWeightKg = (double)_weightNumeric.Value;
        result.HeatTemperatureUnits = _tempUnitsComboBox.SelectedItem?.ToString() ?? "Centigrade";

        if (!Enum.TryParse<FellrnrHeatCalculator.SodiumLosses>(_sodiumComboBox.SelectedItem?.ToString(), out var sodiumLosses))
        {
            sodiumLosses = FellrnrHeatCalculator.SodiumLosses.None;
        }
        result.HeatSodiumLosses = sodiumLosses;

        var pairs = new List<SensorPairConfig>();
        foreach (DataGridViewRow row in _pairsGrid.Rows)
        {
            var index = Convert.ToInt32(row.Cells["Index"].Value);
            var enabled = Convert.ToBoolean(row.Cells["Enabled"].Value ?? false);
            var name = Convert.ToString(row.Cells["Name"].Value)?.Trim() ?? string.Empty;
            var channel = Convert.ToInt32(row.Cells["UsbChannel"].Value);
            var mac = Convert.ToString(row.Cells["BluetoothMac"].Value)?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this, $"Pair {index} needs a sensor name.", "Configuration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!result.TestMode && enabled && !MacAddressHelper.TryNormalize(mac, out _))
            {
                MessageBox.Show(this, $"Pair {index} needs a valid Bluetooth MAC address when Test mode is off.", "Configuration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            pairs.Add(new SensorPairConfig
            {
                Index = index,
                Enabled = enabled,
                Name = name,
                UsbChannel = channel,
                BluetoothMacAddress = mac
            });
        }

        result.SensorPairs = pairs;
        result.EnsureFourPairs();
        ResultConfig = result;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static decimal ClampDecimal(decimal value, decimal min, decimal max)
    {
        return Math.Min(Math.Max(value, min), max);
    }
}
