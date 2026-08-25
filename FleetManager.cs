namespace SelfDrivingCarSimulation;

/// <summary>
/// Aggregate summary statistics for the current fleet, produced via LINQ.
/// </summary>
public class FleetSummary
{
	public int TotalCars { get; init; }
	public double AverageBatteryPercent { get; init; }
	public double AverageRangeKm { get; init; }
	public int LowBatteryCount { get; init; }
	public Dictionary<OperationalStatus, int> CountByStatus { get; init; } = new();
	public Dictionary<MissionPriority, int> CountByPriority { get; init; } = new();
}

/// <summary>
/// Manages the live collection of autonomous vehicles, exposes expressive
/// LINQ-based querying, and provides a single, consistent path for
/// simulating and gracefully handling exceptional incidents.
/// </summary>
public class FleetManager
{
	private readonly Dictionary<string, Car> _fleet = new();
	public AlertDispatcher Alerts { get; } = new();

	public const double LowBatteryThreshold = 20.0;

	public void SaveToJson(string filePath)
	{
		var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
		var json = System.Text.Json.JsonSerializer.Serialize(_fleet.Values.ToList(), options);
		System.IO.File.WriteAllText(filePath, json);
	}

	public void LoadFromJson(string filePath)
	{
		if (!System.IO.File.Exists(filePath)) return;
		var json = System.IO.File.ReadAllText(filePath);
		var cars = System.Text.Json.JsonSerializer.Deserialize<List<Car>>(json);
		if (cars != null)
		{
			_fleet.Clear();
			foreach (var car in cars)
			{
				_fleet[car.CarId] = car;
			}
		}
	}

	public IReadOnlyCollection<Car> Cars => _fleet.Values;

	public string GenerateCarId()
	{
		var nextNumber = 1;
		while (_fleet.ContainsKey($"TX-{nextNumber:000}"))
			nextNumber++;

		return $"TX-{nextNumber:000}";
	}

	public Car AddCar(Car car)
	{
		car.Validate();
		if (_fleet.ContainsKey(car.CarId))
			throw new CarDataValidationException($"A car with ID '{car.CarId}' already exists.");

		_fleet[car.CarId] = car;
		return car;
	}

	public Car GetCar(string carId) => _fleet.TryGetValue(carId, out var car) ? car : throw new CarNotFoundException(carId);

	public void RemoveCar(string carId)
	{
		if (!_fleet.Remove(carId))
			throw new CarNotFoundException(carId);
	}

	public void UpdateCar(string carId, Action<Car> updateAction)
	{
		var car = GetCar(carId);
		updateAction(car);
		car.Validate(); // re-validate after mutation to preserve data integrity
		car.LogEvent("Car record updated via control panel.");
	}

	// LINQ queries used by the dashboard's fleet views.
	public IEnumerable<Car> GetCarsByStatus(OperationalStatus status) =>
		_fleet.Values.Where(c => c.Status == status).OrderBy(c => c.CarId);

	public IEnumerable<Car> GetPriorityMissions() =>
		_fleet.Values
			  .Where(c => c.Priority is MissionPriority.High or MissionPriority.Critical)
			  .OrderByDescending(c => c.Priority)
			  .ThenBy(c => c.BatteryLevelPercent);

	public IEnumerable<Car> GetLowBatteryCars(double threshold = LowBatteryThreshold) =>
		_fleet.Values.Where(c => c.BatteryLevelPercent <= threshold)
					 .OrderBy(c => c.BatteryLevelPercent);

	public FleetSummary GetFleetSummary()
	{
		var cars = _fleet.Values.ToList();

		return new FleetSummary
		{
			TotalCars = cars.Count,
			AverageBatteryPercent = cars.Count == 0 ? 0 : cars.Average(c => c.BatteryLevelPercent),
			AverageRangeKm = cars.Count == 0 ? 0 : cars.Average(c => c.OperationalRangeKm),
			LowBatteryCount = cars.Count(c => c.BatteryLevelPercent <= LowBatteryThreshold),
			CountByStatus = cars.GroupBy(c => c.Status)
							    .ToDictionary(g => g.Key, g => g.Count()),
			CountByPriority = cars.GroupBy(c => c.Priority)
								  .ToDictionary(g => g.Key, g => g.Count())
		};
	}

	// Successful autonomous events update the vehicle log and raise an alert.

	public void CompleteRoute(string carId)
	{
		var car = GetCar(carId);
		car.Status = OperationalStatus.Idle;
		car.LogEvent("Route completed successfully.");
		Alerts.RaiseAlert(carId, ControlRoomEventType.RouteCompleted,
			"Route completed successfully; vehicle is now idle.", SeverityLevel.Info);
	}

	public void EncounterObstacle(string carId, string obstacleDescription)
	{
		var car = GetCar(carId);
		car.LogEvent($"Obstacle encountered: {obstacleDescription}");
		Alerts.RaiseAlert(carId, ControlRoomEventType.ObstacleEncountered,
			$"Obstacle detected and avoided: {obstacleDescription}", SeverityLevel.Warning);
	}

	public void EnterRestrictedZone(string carId, string zoneName)
	{
		var car = GetCar(carId);
		car.Status = OperationalStatus.Stopped;
		car.LogEvent($"Entered restricted zone '{zoneName}'. Vehicle halted.");
		Alerts.RaiseAlert(carId, ControlRoomEventType.RestrictedZoneEntered,
			$"Vehicle entered restricted zone '{zoneName}' and has been halted for safety.",
			SeverityLevel.Critical);
	}

	// Incident exceptions are contained, logged, and sent to mission control.

	/// <summary>
	/// Central, consistent exception-handling strategy: every simulated
	/// incident is executed here, caught as a SimulationException, logged
	/// to the car's own log, and broadcast to the control room - the
	/// simulation itself never crashes as a result.
	/// </summary>
	public void SimulateIncident(string carId, Action incidentTrigger)
	{
		try
		{
			incidentTrigger();
		}
		catch (SimulationException ex)
		{
			HandleSimulationException(carId, ex);
		}
		catch (Exception ex)
		{
			// Safety net: any unexpected error is also contained, not thrown further.
			HandleSimulationException(carId,
				new CarDataValidationException($"Unexpected error: {ex.Message}"));
		}
	}

	private void HandleSimulationException(string carId, SimulationException ex)
	{
		if (_fleet.TryGetValue(carId, out var car))
		{
			car.LogEvent($"INCIDENT [{ex.Severity}]: {ex.Message}");
			if (ex is CriticalBatteryException)
				car.Status = OperationalStatus.Charging;
			else if (ex is NavigationErrorException or UnauthorizedOverrideException)
				car.Status = OperationalStatus.Stopped;
		}

		Alerts.RaiseAlert(carId, ControlRoomEventType.IncidentRaised, ex.Message, ex.Severity);
	}

	// Convenience triggers used by the GUI's "simulate incident" controls.

	public void TriggerCriticalBattery(string carId, double batteryLevel) =>
		SimulateIncident(carId, () =>
		{
			var car = GetCar(carId);
			car.BatteryLevelPercent = batteryLevel;
			if (batteryLevel <= 5.0)
				throw new CriticalBatteryException(carId, batteryLevel);
		});

	public void TriggerNavigationError(string carId, string details) =>
		SimulateIncident(carId, () => throw new NavigationErrorException(carId, details));

	public void TriggerUnauthorizedOverride(string carId, string requestedBy) =>
		SimulateIncident(carId, () => throw new UnauthorizedOverrideException(carId, requestedBy));
}
