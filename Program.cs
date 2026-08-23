using System.Drawing;
using System.Windows.Forms;
using SelfDrivingCarSimulation;

ApplicationConfiguration.Initialize();
Application.Run(new MainForm());

// Main WinForms control panel for managing the simulated fleet.
public sealed class MainForm : Form
{
	private readonly FleetManager _fleetManager = new();
	private readonly ControlRoomMonitor _controlRoom;
	private readonly DataGridView _fleetGrid = new();
	private readonly ListBox _alertList = new();
	private readonly ListBox _logList = new();
	private readonly Label _summaryLabel = new();
	private readonly TextBox _modelInput = new();
	private readonly NumericUpDown _rangeInput = new() { Minimum = 0, Maximum = 1000, Value = 400 };
	private readonly NumericUpDown _batteryInput = new() { Minimum = 0, Maximum = 100, Value = 75 };
	private readonly ComboBox _priorityInput = new();
	private readonly ComboBox _statusInput = new();
	private readonly ComboBox _statusFilter = new();

	// Load a small fleet so the dashboard is useful immediately after startup.
	private static void SeedDemoData(FleetManager fleetManager)
	{
		fleetManager.AddCar(new Car("TX-001", "Model X Autonomy", 450, 92, MissionPriority.High, OperationalStatus.EnRoute));
		fleetManager.AddCar(new Car("TX-002", "Model 3 Autonomy", 358, 67, MissionPriority.Medium));
		fleetManager.AddCar(new Car("TX-003", "Model Y Autonomy", 400, 18, MissionPriority.Critical, OperationalStatus.EnRoute));
		fleetManager.AddCar(new Car("TX-004", "Cybertruck Autonomy", 500, 100, MissionPriority.Low, OperationalStatus.Charging));
	}

	public MainForm()
	{
		_controlRoom = new ControlRoomMonitor(_fleetManager.Alerts);
		_fleetManager.Alerts.OnAlert += (_, alert) => BeginInvoke(() => _alertList.Items.Add(alert.ToString()));
		SeedDemoData(_fleetManager);
		Text = "Self-Driving Car Simulation";
		Size = new Size(1100, 700);
		MinimumSize = new Size(900, 550);
		BuildLayout();
		RefreshFleet();
	}

	private void BuildLayout()
	{
		// The tabs separate fleet data, alerts, and each car's operational log.
		var tabs = new TabControl { Dock = DockStyle.Fill };
		var fleetTab = new TabPage("Fleet");
		var alertsTab = new TabPage("Alert history");
		var logsTab = new TabPage("Operational logs");
		_fleetGrid.Dock = DockStyle.Fill;
		_fleetGrid.ReadOnly = true;
		_fleetGrid.AutoGenerateColumns = true;
		_fleetGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		_fleetGrid.SelectionChanged += (_, _) => ShowSelectedLog();
		fleetTab.Controls.Add(_fleetGrid);
		_alertList.Dock = DockStyle.Fill;
		alertsTab.Controls.Add(_alertList);
		_logList.Dock = DockStyle.Fill;
		logsTab.Controls.Add(_logList);
		tabs.TabPages.Add(fleetTab);
		tabs.TabPages.Add(alertsTab);
		tabs.TabPages.Add(logsTab);

		var rightPanel = new Panel { Dock = DockStyle.Right, Width = 270, AutoScroll = true, Padding = new Padding(12) };
		rightPanel.VerticalScroll.SmallChange = 24;
		rightPanel.VerticalScroll.LargeChange = 96;
		var actionPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
		rightPanel.Controls.Add(actionPanel);
		actionPanel.Resize += (_, _) => actionPanel.Width = rightPanel.ClientSize.Width - rightPanel.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;
		var controlsPanel = actionPanel;
		controlsPanel.Controls.Add(new Label { Text = "Add car", AutoSize = true, Font = new Font(Font, FontStyle.Bold) });
		AddField(controlsPanel, "Model", _modelInput);
		AddField(controlsPanel, "Range (km)", _rangeInput);
		AddField(controlsPanel, "Battery (%)", _batteryInput);
		_priorityInput.DataSource = Enum.GetValues<MissionPriority>();
		_statusInput.DataSource = Enum.GetValues<OperationalStatus>();
		_statusFilter.Items.Add("All statuses");
		_statusFilter.Items.AddRange(Enum.GetNames<OperationalStatus>());
		_statusFilter.SelectedIndex = 0;
		_statusFilter.SelectedIndexChanged += (_, _) => RefreshFleet();
		AddField(controlsPanel, "Filter status", _statusFilter);
		AddField(controlsPanel, "Priority", _priorityInput);
		AddField(controlsPanel, "Status", _statusInput);
		var addButton = new Button { Text = "Add car", AutoSize = true };
		addButton.Click += AddCar;
		controlsPanel.Controls.Add(addButton);
		AddAction(controlsPanel, "Update selected", UpdateSelected);
		controlsPanel.Controls.Add(new Label { Text = "Actions", AutoSize = true, Font = new Font(Font, FontStyle.Bold), Margin = new Padding(3, 20, 3, 3) });
		AddAction(controlsPanel, "Remove selected", RemoveSelected);
		AddAction(controlsPanel, "Complete route", CompleteRoute);
		AddAction(controlsPanel, "Critical battery", TriggerCriticalBattery);
		AddAction(controlsPanel, "Navigation error", TriggerNavigationError);
		AddAction(controlsPanel, "Restricted zone", EnterRestrictedZone);
		AddAction(controlsPanel, "Refresh summary", (_, _) => RefreshFleet());

		_summaryLabel.Dock = DockStyle.Bottom;
		_summaryLabel.Height = 50;
		_summaryLabel.Padding = new Padding(12, 8, 0, 0);
		_summaryLabel.BorderStyle = BorderStyle.FixedSingle;
		Controls.Add(tabs);
		Controls.Add(rightPanel);
		Controls.Add(_summaryLabel);
	}

	private static void AddField(Control parent, string label, Control input)
	{
		parent.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(3, 10, 3, 2) });
		input.Width = 210;
		parent.Controls.Add(input);
	}

	private static void AddAction(Control parent, string text, EventHandler handler)
	{
		var button = new Button { Text = text, Width = 210, AutoSize = true };
		button.Click += handler;
		parent.Controls.Add(button);
	}

	private Car? SelectedCar
	{
		get
		{
			var id = _fleetGrid.CurrentRow?.Cells["CarId"].Value?.ToString();
			return id == null ? null : _fleetManager.GetCar(id);
		}
	}

	private void RefreshFleet()
	{
		// Rebuild the grid from the current fleet and refresh the summary footer.
		var selectedCarId = _fleetGrid.CurrentRow?.Cells["CarId"].Value?.ToString();
		var cars = _fleetManager.Cars.AsEnumerable();
		if (_statusFilter.SelectedIndex > 0 && Enum.TryParse<OperationalStatus>(_statusFilter.Text, out var status))
			cars = _fleetManager.GetCarsByStatus(status);
		_fleetGrid.DataSource = cars.OrderBy(car => car.CarId).Select(car => new
		{
			car.CarId, car.ModelName, car.OperationalRangeKm, car.BatteryLevelPercent, car.Priority, car.Status
		}).ToList();
		if (!string.IsNullOrEmpty(selectedCarId))
		{
			foreach (DataGridViewRow row in _fleetGrid.Rows)
			{
				if (row.Cells["CarId"].Value?.ToString() == selectedCarId)
				{
					row.Selected = true;
					_fleetGrid.CurrentCell = row.Cells[0];
					break;
				}
			}
		}
		var summary = _fleetManager.GetFleetSummary();
		_summaryLabel.Text = $"Cars: {summary.TotalCars}    Average battery: {summary.AverageBatteryPercent:0.#}%    Average range: {summary.AverageRangeKm:0.#} km    Low battery: {summary.LowBatteryCount}";
		ShowSelectedLog();
	}

	private void ShowSelectedLog()
	{
		// Show log entries for the car currently selected in the fleet grid.
		var selectedCar = SelectedCar;
		if (selectedCar != null)
		{
			_modelInput.Text = selectedCar.ModelName;
			_rangeInput.Value = (decimal)selectedCar.OperationalRangeKm;
			_batteryInput.Value = (decimal)selectedCar.BatteryLevelPercent;
			_priorityInput.SelectedItem = selectedCar.Priority;
			_statusInput.SelectedItem = selectedCar.Status;
			_logList.Items.Clear();
			_logList.Items.Add($"Operational log: {selectedCar.CarId}");
			_logList.Items.AddRange(selectedCar.OperationalLog.ToArray());
		}
		else
		{
			_logList.Items.Clear();
		}
	}

	private void AddCar(object? sender, EventArgs e)
	{
		// DataAnnotations validation runs inside the Car constructor.
		try
		{
			_fleetManager.AddCar(new Car(_fleetManager.GenerateCarId(), _modelInput.Text, (double)_rangeInput.Value, (double)_batteryInput.Value,
				(MissionPriority)_priorityInput.SelectedItem!, (OperationalStatus)_statusInput.SelectedItem!));
			_modelInput.Clear();
			RefreshFleet();
		}
		catch (SimulationException exception) { MessageBox.Show(exception.Message, "Unable to add car", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
	}

	private void RemoveSelected(object? sender, EventArgs e)
	{
		try { _fleetManager.RemoveCar(SelectedCar?.CarId ?? throw new CarNotFoundException("selected")); RefreshFleet(); }
		catch (SimulationException exception) { MessageBox.Show(exception.Message, "Remove car", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
	}

	private void UpdateSelected(object? sender, EventArgs e)
	{
		try
		{
			var car = SelectedCar ?? throw new CarNotFoundException("selected");
			_fleetManager.UpdateCar(car.CarId, updatedCar =>
			{
				updatedCar.ModelName = _modelInput.Text;
				updatedCar.OperationalRangeKm = (double)_rangeInput.Value;
				updatedCar.BatteryLevelPercent = (double)_batteryInput.Value;
				updatedCar.Priority = (MissionPriority)_priorityInput.SelectedItem!;
				updatedCar.Status = (OperationalStatus)_statusInput.SelectedItem!;
			});
			RefreshFleet();
		}
		catch (SimulationException exception) { MessageBox.Show(exception.Message, "Update car", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
	}

	private void CompleteRoute(object? sender, EventArgs e) => RunSelected(carId => _fleetManager.CompleteRoute(carId));
	private void TriggerCriticalBattery(object? sender, EventArgs e) => RunSelected(carId => _fleetManager.TriggerCriticalBattery(carId, 3));
	private void TriggerNavigationError(object? sender, EventArgs e) => RunSelected(carId => _fleetManager.TriggerNavigationError(carId, "GPS signal lost in tunnel."));
	private void EnterRestrictedZone(object? sender, EventArgs e) => RunSelected(carId => _fleetManager.EnterRestrictedZone(carId, "Downtown Security Perimeter"));

	private void RunSelected(Action<string> action)
	{
		try { action(SelectedCar?.CarId ?? throw new CarNotFoundException("selected")); RefreshFleet(); }
		catch (SimulationException exception) { MessageBox.Show(exception.Message, "Simulation event", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
	}
}

