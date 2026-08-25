using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SelfDrivingCarSimulation;

/// <summary>
/// Represents a single self-driving car within the fleet simulation.
/// Encapsulates identity, range, energy, mission priority and an
/// autonomous operational log. Enforces its own data-integrity rules.
/// </summary>
public class Car
{
	[Required]
	public string CarId { get; set; }

	[Required]
	public string ModelName { get; set; }

	[Range(0, 1000)]
	public double OperationalRangeKm { get; set; }

	[Range(0, 100)]
	public double BatteryLevelPercent { get; set; }

	[EnumDataType(typeof(MissionPriority))]
	public MissionPriority Priority { get; set; }

	[EnumDataType(typeof(OperationalStatus))]
	public OperationalStatus Status { get; set; }

	public List<string> OperationalLog { get; set; } = new();

	public Car() { }

	public Car(string carId, string modelName, double operationalRangeKm, double batteryLevelPercent,
		MissionPriority priority, OperationalStatus status = OperationalStatus.Idle)
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

		var validationContext = new ValidationContext(this);
		var validationResults = new List<ValidationResult>();

		if (!Validator.TryValidateObject(this, validationContext, validationResults, validateAllProperties: true))
		{
			var message = validationResults.First().ErrorMessage ?? "Car data is invalid.";
			throw new CarDataValidationException(message);
		}
	}

	/// <summary>
	/// Appends a timestamped entry to this car's autonomous operational log.
	/// </summary>
	public void LogEvent(string message)
	{
		OperationalLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
	}

	public override string ToString() =>
		$"{CarId} ({ModelName}) - {Status}, {BatteryLevelPercent:0.#}% battery, {OperationalRangeKm:0.#} km range, Priority={Priority}";
}
