namespace SelfDrivingCarSimulation.Models
{
    /// <summary>
    /// Represents the current operational state of an autonomous vehicle.
    /// </summary>
    public enum OperationalStatus
    {
        Idle,
        EnRoute,
        Charging,
        InMaintenance,
        Stopped
    }

    /// <summary>
    /// Represents the mission priority level assigned to a vehicle.
    /// Higher values indicate more urgent/critical missions.
    /// </summary>
    public enum MissionPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Categories of autonomous events that can be delegated to the control room.
    /// </summary>
    public enum ControlRoomEventType
    {
        RouteCompleted,
        ObstacleEncountered,
        RestrictedZoneEntered,
        IncidentRaised,
        IncidentResolved
    }

    /// <summary>
    /// Severity classification used consistently across exceptions and alerts.
    /// </summary>
    public enum SeverityLevel
    {
        Info,
        Warning,
        Critical
    }
}
