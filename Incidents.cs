namespace SelfDrivingCarSimulation;

/// <summary>
/// Base type for every exception raised within the simulation.
/// Carries a severity level and timestamp so all callers can handle
/// incidents through a single, consistent strategy (catch SimulationException).
/// </summary>
public abstract class SimulationException : Exception
{
	public SeverityLevel Severity { get; }
	public DateTime OccurredAt { get; }
	public string? CarId { get; }

	protected SimulationException(string message, SeverityLevel severity, string? carId = null) : base(message)
	{
		Severity = severity;
		OccurredAt = DateTime.Now;
		CarId = carId;
	}
}

/// <summary>
/// Raised when a car's battery drops to a level that endangers safe operation.
/// </summary>
public class CriticalBatteryException(string carId, double batteryLevelPercent)
	: SimulationException($"Critical battery failure on car '{carId}': battery at {batteryLevelPercent:0.#}%.", SeverityLevel.Critical, carId)
{
	public double BatteryLevelPercent { get; } = batteryLevelPercent;
}

/// <summary>
/// Raised when the navigation subsystem cannot resolve a safe route.
/// </summary>
public class NavigationErrorException(string carId, string details)
	: SimulationException($"Navigation error on car '{carId}': {details}", SeverityLevel.Critical, carId);

/// <summary>
/// Raised when an unauthorized party attempts to override autonomous control.
/// </summary>
public class UnauthorizedOverrideException(string carId, string requestedBy)
	: SimulationException($"Unauthorized override attempt on car '{carId}' by '{requestedBy}'.", SeverityLevel.Critical, carId)
{
	public string RequestedBy { get; } = requestedBy;
}

/// <summary>
/// Raised when car data fails integrity/constraint validation.
/// </summary>
public class CarDataValidationException(string message) : SimulationException(message, SeverityLevel.Warning);

/// <summary>
/// Raised when an operation references a car that does not exist in the fleet.
/// </summary>
public class CarNotFoundException(string carId) : SimulationException($"No car with ID '{carId}' was found in the fleet.", SeverityLevel.Warning, carId);
