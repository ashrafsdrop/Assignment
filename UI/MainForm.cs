using System.ComponentModel;
using SelfDrivingCarSimulation.Events;
using SelfDrivingCarSimulation.Exceptions;
using SelfDrivingCarSimulation.Models;
using SelfDrivingCarSimulation.Services;

namespace SelfDrivingCarSimulation.UI
{
    /// <summary>
    /// Main operational control panel: add/view/remove/update cars, monitor
    /// fleet-wide statistics, simulate incidents, and watch real-time
    /// control-room alerts arrive through the delegate-based event system.
    /// </summary>
    public class MainForm : Form
    {
        private readonly FleetManager _fleetManager = new();
        private readonly ControlRoomMonitor _controlRoom;
        private readonly BindingList<Car> _boundCars = new();

        private readonly DataGridView _grid = new();
        private readonly ListBox _alertLog = new();
        private readonly Label _summaryLabel = new();
        private readonly ComboBox _statusFilter = new();

        public MainForm()
        {
            _controlRoom = new ControlRoomMonitor(_fleetManager.Alerts);
            _fleetManager.Alerts.OnAlert += FleetAlerts_OnAlert;

            Text = "Tesla Autonomous Fleet - Simulation Control Panel";
            ClientSize = new Size(1180, 720);
            MinimumSize = new Size(900, 560);
            StartPosition = FormStartPosition.CenterScreen;

            BuildLayout();
            SeedDemoData();
            RefreshGrid();
            RefreshSummary();
        }

        // ---------------------------------------------------------------
        // Layout construction
        // ---------------------------------------------------------------
        private void BuildLayout()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            Controls.Add(root);

            // ---- Left side: grid + record controls + filters ----
            var left = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var recordButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = true,
                Padding = new Padding(8)
            };
            var addBtn = new Button { Text = "Add Car", Width = 100, Height = 30 };
            var editBtn = new Button { Text = "Edit Selected", Width = 110, Height = 30 };
            var removeBtn = new Button { Text = "Remove Selected", Width = 130, Height = 30 };
            var refreshBtn = new Button { Text = "Refresh", Width = 90, Height = 30 };
            addBtn.Click += (_, _) => AddCar();
            editBtn.Click += (_, _) => EditSelectedCar();
            removeBtn.Click += (_, _) => RemoveSelectedCar();
            refreshBtn.Click += (_, _) => { RefreshGrid(); RefreshSummary(); };
            recordButtons.Controls.AddRange(new Control[] { addBtn, editBtn, removeBtn, refreshBtn });

            left.Controls.Add(recordButtons, 0, 0);

            _grid.Dock = DockStyle.Fill;
            _grid.AutoGenerateColumns = false;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.DataSource = _boundCars;
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CarId", HeaderText = "Car ID", FillWeight = 12 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ModelName", HeaderText = "Model", FillWeight = 20 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", FillWeight = 15 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "BatteryLevelPercent", HeaderText = "Battery %", FillWeight = 13 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OperationalRangeKm", HeaderText = "Range (km)", FillWeight = 15 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Priority", HeaderText = "Priority", FillWeight = 15 });
            left.Controls.Add(_grid, 0, 1);

            var filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = true,
                Padding = new Padding(8)
            };
            filterPanel.Controls.Add(new Label { Text = "Filter by status:", AutoSize = true, Padding = new Padding(0, 6, 4, 0) });
            _statusFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            _statusFilter.Width = 130;
            _statusFilter.Items.Add("All");
            foreach (var s in Enum.GetValues(typeof(OperationalStatus))) _statusFilter.Items.Add(s);
            _statusFilter.SelectedIndex = 0;
            _statusFilter.SelectedIndexChanged += (_, _) => RefreshGrid();
            filterPanel.Controls.Add(_statusFilter);

            var priorityBtn = new Button { Text = "Show Priority Missions", Width = 170 };
            priorityBtn.Click += (_, _) => ShowPriorityMissions();
            filterPanel.Controls.Add(priorityBtn);

            var lowBatteryBtn = new Button { Text = "Show Low Battery (\u226420%)", Width = 170 };
            lowBatteryBtn.Click += (_, _) => ShowLowBattery();
            filterPanel.Controls.Add(lowBatteryBtn);

            left.Controls.Add(filterPanel, 0, 2);
            root.Controls.Add(left, 0, 0);

            // ---- Right side: incident simulation + summary + alert log ----
            var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var incidentGroup = new GroupBox { Text = "Incident / Event Simulation (selected car)", Dock = DockStyle.Fill, Height = 220 };
            var incidentPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                AutoScroll = true
            };
            AddIncidentButton(incidentPanel, "Simulate Critical Battery Failure", () => WithSelectedCar(c => _fleetManager.TriggerCriticalBattery(c.CarId, 3)));
            AddIncidentButton(incidentPanel, "Simulate Navigation Error", () => WithSelectedCar(c => _fleetManager.TriggerNavigationError(c.CarId, "GPS signal lost in tunnel.")));
            AddIncidentButton(incidentPanel, "Simulate Unauthorized Override", () => WithSelectedCar(c => _fleetManager.TriggerUnauthorizedOverride(c.CarId, "unknown-device-42")));
            AddIncidentButton(incidentPanel, "Encounter Obstacle", () => WithSelectedCar(c => _fleetManager.EncounterObstacle(c.CarId, "Pedestrian crossing detected.")));
            AddIncidentButton(incidentPanel, "Enter Restricted Zone", () => WithSelectedCar(c => _fleetManager.EnterRestrictedZone(c.CarId, "Downtown Security Perimeter")));
            AddIncidentButton(incidentPanel, "Mark Route Completed", () => WithSelectedCar(c => _fleetManager.CompleteRoute(c.CarId)));
            incidentGroup.Controls.Add(incidentPanel);
            right.Controls.Add(incidentGroup, 0, 0);

            var summaryGroup = new GroupBox { Text = "Fleet Summary", Dock = DockStyle.Fill, Height = 130 };
            _summaryLabel.Dock = DockStyle.Fill;
            _summaryLabel.Padding = new Padding(10);
            summaryGroup.Controls.Add(_summaryLabel);
            right.Controls.Add(summaryGroup, 0, 1);

            var alertGroup = new GroupBox { Text = "Control Room - Live Alert Feed", Dock = DockStyle.Fill };
            _alertLog.Dock = DockStyle.Fill;
            _alertLog.HorizontalScrollbar = true;
            alertGroup.Controls.Add(_alertLog);
            right.Controls.Add(alertGroup, 0, 2);

            root.Controls.Add(right, 1, 0);
        }

        private static void AddIncidentButton(TableLayoutPanel panel, string text, Action action)
        {
            panel.ColumnCount = 1;
            panel.ColumnStyles.Clear();
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var row = panel.RowCount++;
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var btn = new Button { Text = text, Height = 30, Dock = DockStyle.Fill, Margin = new Padding(3) };
            btn.Click += (_, _) => action();
            panel.Controls.Add(btn, 0, row);
        }

        // ---------------------------------------------------------------
        // Data operations
        // ---------------------------------------------------------------
        private void SeedDemoData()
        {
            TryRun(() =>
            {
                _fleetManager.AddCar(new Car("TX-001", "Model X Autonomy", 450, 92, MissionPriority.High, OperationalStatus.EnRoute));
                _fleetManager.AddCar(new Car("TX-002", "Model 3 Autonomy", 358, 67, MissionPriority.Medium, OperationalStatus.Idle));
                _fleetManager.AddCar(new Car("TX-003", "Model Y Autonomy", 400, 18, MissionPriority.Critical, OperationalStatus.EnRoute));
                _fleetManager.AddCar(new Car("TX-004", "Cybertruck Autonomy", 500, 100, MissionPriority.Low, OperationalStatus.Charging));
            });
        }

        private void RefreshGrid()
        {
            IEnumerable<Car> source = _fleetManager.Cars;

            if (_statusFilter.SelectedItem is OperationalStatus selected)
                source = _fleetManager.GetCarsByStatus(selected);

            _boundCars.RaiseListChangedEvents = false;
            _boundCars.Clear();
            foreach (var car in source.OrderBy(c => c.CarId))
                _boundCars.Add(car);
            _boundCars.RaiseListChangedEvents = true;
            _boundCars.ResetBindings();
        }

        private void RefreshSummary()
        {
            var s = _fleetManager.GetFleetSummary();
            var statusBreakdown = string.Join(", ", s.CountByStatus.Select(kv => $"{kv.Key}={kv.Value}"));
            var priorityBreakdown = string.Join(", ", s.CountByPriority.Select(kv => $"{kv.Key}={kv.Value}"));

            _summaryLabel.Text =
                $"Total cars: {s.TotalCars}\r\n" +
                $"Avg battery: {s.AverageBatteryPercent:0.#}%\r\n" +
                $"Avg range: {s.AverageRangeKm:0.#} km\r\n" +
                $"Low battery (\u2264{FleetManager.LowBatteryThreshold:0}%): {s.LowBatteryCount}\r\n" +
                $"By status: {statusBreakdown}\r\n" +
                $"By priority: {priorityBreakdown}";
        }

        private Car? GetSelectedCar()
        {
            if (_grid.CurrentRow?.DataBoundItem is Car car) return car;
            return null;
        }

        private void WithSelectedCar(Action<Car> action)
        {
            var car = GetSelectedCar();
            if (car == null)
            {
                MessageBox.Show(this, "Select a car in the grid first.", "No Car Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            TryRun(() => action(car));
            RefreshGrid();
            RefreshSummary();
        }

        private void AddCar()
        {
            using var dialog = new CarEditForm();
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            TryRun(() =>
            {
                var car = new Car(_fleetManager.GenerateCarId(), dialog.ModelValue, dialog.RangeValue,
                    dialog.BatteryValue, dialog.PriorityValue, dialog.StatusValue);
                _fleetManager.AddCar(car);
            });

            RefreshGrid();
            RefreshSummary();
        }

        private void EditSelectedCar()
        {
            var car = GetSelectedCar();
            if (car == null)
            {
                MessageBox.Show(this, "Select a car in the grid first.", "No Car Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new CarEditForm(car);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            TryRun(() =>
            {
                _fleetManager.UpdateCar(car.CarId, c =>
                {
                    c.ModelName = dialog.ModelValue;
                    c.OperationalRangeKm = dialog.RangeValue;
                    c.BatteryLevelPercent = dialog.BatteryValue;
                    c.Priority = dialog.PriorityValue;
                    c.Status = dialog.StatusValue;
                });
            });

            RefreshGrid();
            RefreshSummary();
        }

        private void RemoveSelectedCar()
        {
            var car = GetSelectedCar();
            if (car == null)
            {
                MessageBox.Show(this, "Select a car in the grid first.", "No Car Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(this, $"Remove car '{car.CarId}' from the fleet?",
                "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            TryRun(() => _fleetManager.RemoveCar(car.CarId));
            RefreshGrid();
            RefreshSummary();
        }

        private void ShowPriorityMissions()
        {
            var results = _fleetManager.GetPriorityMissions().ToList();
            var text = results.Count == 0
                ? "No High/Critical priority missions active."
                : string.Join(Environment.NewLine, results.Select(c => c.ToString()));
            MessageBox.Show(this, text, "Priority Missions", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowLowBattery()
        {
            var results = _fleetManager.GetLowBatteryCars().ToList();
            var text = results.Count == 0
                ? "No cars currently at or below the low-battery threshold."
                : string.Join(Environment.NewLine, results.Select(c => c.ToString()));
            MessageBox.Show(this, text, "Low Battery Cars", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ---------------------------------------------------------------
        // Cross-cutting concerns: exception handling & alert feed
        // ---------------------------------------------------------------

        /// <summary>
        /// Consistent UI-level exception handling: SimulationExceptions are
        /// shown to the operator as a friendly message; the control system
        /// itself is never allowed to crash from a caught incident.
        /// </summary>
        private void TryRun(Action action)
        {
            try
            {
                action();
            }
            catch (SimulationException ex)
            {
                MessageBox.Show(this, ex.Message, $"{ex.Severity} - Simulation Notice",
                    MessageBoxButtons.OK,
                    ex.Severity == SeverityLevel.Critical ? MessageBoxIcon.Error : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unexpected error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FleetAlerts_OnAlert(object sender, ControlRoomAlertEventArgs e)
        {
            // Marshal onto the UI thread defensively in case alerts ever
            // originate from a background/simulated worker thread.
            if (InvokeRequired)
            {
                Invoke(new ControlRoomAlertHandler(FleetAlerts_OnAlert), sender, e);
                return;
            }

            _alertLog.Items.Insert(0, e.ToString());
            RefreshSummary();
        }
    }
}
