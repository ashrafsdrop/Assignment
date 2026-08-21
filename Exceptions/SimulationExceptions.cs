namespace SelfDrivingCarSimulation.Exceptions
{
    /// <summary>
    /// Base type for every exception raised within the simulation.
    /// Carries a severity level and timestamp so all callers can handle
    /// incidents through a single, consistent strategy (catch SimulationException).
    /// </summary>
    public abstract class SimulationException : Exception
    {
        public Models.SeverityLevel Severity { get; }
        public DateTime OccurredAt { get; }
        public string? CarId { get; }

        protected SimulationException(string message, Models.SeverityLevel severity, string? carId = null)
            : base(message)
        {
            Severity = severity;
            OccurredAt = DateTime.Now;
            CarId = carId;
        }
    }

    /// <summary>Raised when a car's battery drops to a level that endangers safe operation.</summary>
    public class CriticalBatteryException : SimulationException
    {
        public double BatteryLevelPercent { get; }

        public CriticalBatteryException(string carId, double batteryLevelPercent)
            : base($"Critical battery failure on car '{carId}': battery at {batteryLevelPercent:0.#}%.",
                   Models.SeverityLevel.Critical, carId)
        {
            BatteryLevelPercent = batteryLevelPercent;
        }
    }

    /// <summary>Raised when the navigation subsystem cannot resolve a safe route.</summary>
    public class NavigationErrorException : SimulationException
    {
        public NavigationErrorException(string carId, string details)
            : base($"Navigation error on car '{carId}': {details}", Models.SeverityLevel.Critical, carId)
        {
        }
    }

    /// <summary>Raised when an unauthorized party attempts to override autonomous control.</summary>
    public class UnauthorizedOverrideException : SimulationException
    {
        public string RequestedBy { get; }

        public UnauthorizedOverrideException(string carId, string requestedBy)
            : base($"Unauthorized override attempt on car '{carId}' by '{requestedBy}'.",
                   Models.SeverityLevel.Critical, carId)
        {
            RequestedBy = requestedBy;
        }
    }

    /// <summary>Raised when car data fails integrity/constraint validation.</summary>
    public class CarDataValidationException : SimulationException
    {
        public CarDataValidationException(string message)
            : base(message, Models.SeverityLevel.Warning)
        {
        }
    }

    /// <summary>Raised when an operation references a car that does not exist in the fleet.</summary>
    public class CarNotFoundException : SimulationException
    {
        public CarNotFoundException(string carId)
            : base($"No car with ID '{carId}' was found in the fleet.", Models.SeverityLevel.Warning, carId)
        {
        }
    }
}
