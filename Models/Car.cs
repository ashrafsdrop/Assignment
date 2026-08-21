using SelfDrivingCarSimulation.Exceptions;

namespace SelfDrivingCarSimulation.Models
{
    /// <summary>
    /// Represents a single self-driving car within the fleet simulation.
    /// Encapsulates identity, range, energy, mission priority and an
    /// autonomous operational log. Enforces its own data-integrity rules.
    /// </summary>
    public class Car
    {
        public string CarId { get; }
        public string ModelName { get; set; }
        public double OperationalRangeKm { get; set; }
        public double BatteryLevelPercent { get; set; }
        public MissionPriority Priority { get; set; }
        public OperationalStatus Status { get; set; }

        private readonly List<string> _operationalLog = new();
        public IReadOnlyList<string> OperationalLog => _operationalLog.AsReadOnly();

        public Car(string carId, string modelName, double operationalRangeKm,
                   double batteryLevelPercent, MissionPriority priority,
                   OperationalStatus status = OperationalStatus.Idle)
        {
            CarId = carId?.Trim() ?? throw new CarDataValidationException("CarId cannot be null.");
            ModelName = modelName?.Trim() ?? throw new CarDataValidationException("ModelName cannot be null.");
            OperationalRangeKm = operationalRangeKm;
            BatteryLevelPercent = batteryLevelPercent;
            Priority = priority;
            Status = status;

            Validate();
            LogEvent($"Car '{CarId}' registered with fleet. Status={Status}, Battery={BatteryLevelPercent}%.");
        }

        /// <summary>
        /// Validates the car's current data against operational constraints.
        /// Throws CarDataValidationException when a rule is violated so that
        /// invalid state can never persist in the fleet.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(CarId))
                throw new CarDataValidationException("CarId is required.");

            if (string.IsNullOrWhiteSpace(ModelName))
                throw new CarDataValidationException("ModelName is required.");

            if (OperationalRangeKm < 0)
                throw new CarDataValidationException($"Operational range cannot be negative (got {OperationalRangeKm} km).");

            if (BatteryLevelPercent < 0 || BatteryLevelPercent > 100)
                throw new CarDataValidationException($"Battery level must be between 0 and 100 (got {BatteryLevelPercent}%).");

            if (!Enum.IsDefined(typeof(MissionPriority), Priority))
                throw new CarDataValidationException("Mission priority is not a recognized value.");

            if (!Enum.IsDefined(typeof(OperationalStatus), Status))
                throw new CarDataValidationException("Operational status is not a recognized value.");
        }

        /// <summary>
        /// Appends a timestamped entry to this car's autonomous operational log.
        /// </summary>
        public void LogEvent(string message)
        {
            _operationalLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
        }

        public override string ToString() =>
            $"{CarId} ({ModelName}) - {Status}, {BatteryLevelPercent:0.#}% battery, {OperationalRangeKm:0.#} km range, Priority={Priority}";
    }
}
