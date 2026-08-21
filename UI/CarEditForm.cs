using SelfDrivingCarSimulation.Exceptions;
using SelfDrivingCarSimulation.Models;

namespace SelfDrivingCarSimulation.UI
{
    /// <summary>
    /// Modal dialog used to capture input for adding a new car or editing
    /// an existing one. Validation errors are shown to the user without
    /// closing the dialog or crashing the application.
    /// </summary>
    public class CarEditForm : Form
    {
        private readonly TextBox _idBox = new();
        private readonly TextBox _modelBox = new();
        private readonly NumericUpDown _rangeBox = new();
        private readonly NumericUpDown _batteryBox = new();
        private readonly ComboBox _priorityBox = new();
        private readonly ComboBox _statusBox = new();
        private readonly Button _okButton = new() { Text = "Save", DialogResult = DialogResult.None };
        private readonly Button _cancelButton = new() { Text = "Cancel", DialogResult = DialogResult.Cancel };

        public bool IsEditMode { get; }

        public string CarIdValue => _idBox.Text.Trim();
        public string ModelValue => _modelBox.Text.Trim();
        public double RangeValue => (double)_rangeBox.Value;
        public double BatteryValue => (double)_batteryBox.Value;
        public MissionPriority PriorityValue => (MissionPriority)_priorityBox.SelectedItem!;
        public OperationalStatus StatusValue => (OperationalStatus)_statusBox.SelectedItem!;

        public CarEditForm(Car? existingCar = null)
        {
            IsEditMode = existingCar != null;
            Text = IsEditMode ? "Edit Car" : "Add New Car";
            BuildLayout();

            if (existingCar != null)
            {
                _idBox.Text = existingCar.CarId;
                _idBox.Enabled = false; // identity is immutable once created
                _modelBox.Text = existingCar.ModelName;
                _rangeBox.Value = (decimal)existingCar.OperationalRangeKm;
                _batteryBox.Value = (decimal)existingCar.BatteryLevelPercent;
                _priorityBox.SelectedItem = existingCar.Priority;
                _statusBox.SelectedItem = existingCar.Status;
            }
            else
            {
                _priorityBox.SelectedItem = MissionPriority.Medium;
                _statusBox.SelectedItem = OperationalStatus.Idle;
                _rangeBox.Value = 100;
                _batteryBox.Value = 100;
            }
        }

        private void BuildLayout()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(360, 300);
            MinimumSize = new Size(340, 300);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(12),
                AutoSize = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            for (var row = 0; row < layout.RowCount; row++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _rangeBox.Maximum = 10000;
            _rangeBox.DecimalPlaces = 1;
            _batteryBox.Maximum = 100;
            _batteryBox.DecimalPlaces = 1;
            _priorityBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _priorityBox.DataSource = Enum.GetValues(typeof(MissionPriority));
            _statusBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _statusBox.DataSource = Enum.GetValues(typeof(OperationalStatus));

            void AddRow(string label, Control control)
            {
                layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 0, 0) });
                control.Dock = DockStyle.Fill;
                control.Margin = new Padding(3);
                layout.Controls.Add(control);
            }

            AddRow("Car ID:", _idBox);
            AddRow("Model Name:", _modelBox);
            AddRow("Range (km):", _rangeBox);
            AddRow("Battery (%):", _batteryBox);
            AddRow("Priority:", _priorityBox);
            AddRow("Status:", _statusBox);

            Controls.Add(layout);

            if (!IsEditMode)
            {
                _idBox.Visible = false;
                layout.GetControlFromPosition(0, 0)!.Visible = false;
                layout.RowStyles[0].Height = 0;
            }

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 45,
                Padding = new Padding(10)
            };
            _okButton.Click += OkButton_Click;
            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(_okButton);
            Controls.Add(buttonPanel);

            AcceptButton = _okButton;
            CancelButton = _cancelButton;
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            // Basic pre-flight check; deeper integrity validation happens in
            // Car.Validate() / FleetManager, which the caller invokes.
            if (IsEditMode && string.IsNullOrWhiteSpace(CarIdValue) || string.IsNullOrWhiteSpace(ModelValue))
            {
                MessageBox.Show(this, IsEditMode ? "Car ID and Model Name are required." : "Model Name is required.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
