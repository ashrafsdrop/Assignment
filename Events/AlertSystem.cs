using SelfDrivingCarSimulation.Models;

namespace SelfDrivingCarSimulation.Events
{
    /// <summary>
    /// Event data passed to every control-room subscriber when an
    /// autonomous event or incident is raised.
    /// </summary>
    public class ControlRoomAlertEventArgs : EventArgs
    {
        public string CarId { get; }
        public ControlRoomEventType EventType { get; }
        public string Message { get; }
        public SeverityLevel Severity { get; }
        public DateTime Timestamp { get; }

        public ControlRoomAlertEventArgs(string carId, ControlRoomEventType eventType,
                                          string message, SeverityLevel severity)
        {
            CarId = carId;
            EventType = eventType;
            Message = message;
            Severity = severity;
            Timestamp = DateTime.Now;
        }

        public override string ToString() =>
            $"[{Timestamp:HH:mm:ss}] ({Severity}) {EventType} - {CarId}: {Message}";
    }

    /// <summary>
    /// Custom delegate signature used for control room alert notifications.
    /// Declared explicitly (rather than relying only on EventHandler&lt;T&gt;)
    /// to satisfy the delegate-based alert system requirement explicitly.
    /// </summary>
    public delegate void ControlRoomAlertHandler(object sender, ControlRoomAlertEventArgs e);

    /// <summary>
    /// Central dispatcher that raises delegate-based alerts whenever an
    /// autonomous vehicle event occurs. Any number of listeners (GUI,
    /// logging, remote telemetry, etc.) can subscribe independently.
    /// </summary>
    public class AlertDispatcher
    {
        public event ControlRoomAlertHandler? OnAlert;

        public void RaiseAlert(string carId, ControlRoomEventType eventType, string message, SeverityLevel severity)
        {
            var args = new ControlRoomAlertEventArgs(carId, eventType, message, severity);
            // Invoke defensively: a misbehaving subscriber must never crash the simulation.
            OnAlert?.Invoke(this, args);
        }
    }

    /// <summary>
    /// A simple always-on subscriber representing "mission control".
    /// Keeps an in-memory history of every alert it receives.
    /// </summary>
    public class ControlRoomMonitor
    {
        private readonly List<ControlRoomAlertEventArgs> _history = new();
        public IReadOnlyList<ControlRoomAlertEventArgs> History => _history.AsReadOnly();

        public ControlRoomMonitor(AlertDispatcher dispatcher)
        {
            dispatcher.OnAlert += HandleAlert;
        }

        private void HandleAlert(object sender, ControlRoomAlertEventArgs e)
        {
            _history.Add(e);
        }
    }
}
