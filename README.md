# Tesla Autonomous Fleet — Self-Driving Car Simulation (C# / .NET 8 WinForms)

A prototype simulation and control-panel application for managing autonomous
vehicle statuses, simulating driving incidents, and delegating real-time
alerts to "mission control."

## How to run

Requires the **.NET 8 SDK** with the Windows Desktop workload (WinForms), on Windows.

```bash
cd SelfDrivingCarSimulation
dotnet run
```

The app opens with four demo cars pre-loaded (`TX-001`..`TX-004`).

> Note: WinForms is Windows-only. If you need to run this on macOS/Linux,
> port `UI/MainForm.cs` and `UI/CarEditForm.cs` to Avalonia UI or a console
> menu — every other layer (`Models`, `Exceptions`, `Events`, `Services`) is
> plain, platform-independent C# and can be reused as-is.

## Project layout & design notes

| Folder / File | Responsibility |
|---|---|
| `Models/Car.cs` | Car entity: identity, range, battery, priority, status, and an append-only operational log. Validates its own invariants (`Validate()`). |
| `Models/Enums.cs` | `OperationalStatus`, `MissionPriority`, `ControlRoomEventType`, `SeverityLevel`. |
| `Exceptions/SimulationExceptions.cs` | `SimulationException` base + `CriticalBatteryException`, `NavigationErrorException`, `UnauthorizedOverrideException`, `CarDataValidationException`, `CarNotFoundException`. One consistent catch surface (`catch (SimulationException)`) is used everywhere incidents are handled, so the control system never crashes. |
| `Events/AlertSystem.cs` | `ControlRoomAlertHandler` delegate + `AlertDispatcher` (raises alerts) + `ControlRoomMonitor` (a sample always-on subscriber that keeps history). Any number of independent listeners — GUI, logging, telemetry — can subscribe. |
| `Services/FleetManager.cs` | Owns the live car collection; add/get/update/remove; LINQ-based querying (`GetCarsByStatus`, `GetPriorityMissions`, `GetLowBatteryCars`, `GetFleetSummary`); autonomous event methods (`CompleteRoute`, `EncounterObstacle`, `EnterRestrictedZone`); incident simulation (`SimulateIncident` + trigger helpers) that catches exceptions, updates car state/log, and raises a control-room alert. |
| `UI/MainForm.cs` | The control panel: grid of cars (add/edit/remove/refresh), status filter, priority/low-battery queries, incident-simulation buttons, live fleet summary, and a live alert feed bound to `AlertDispatcher.OnAlert`. |
| `UI/CarEditForm.cs` | Modal dialog for add/edit, with basic pre-flight validation before deeper `Car.Validate()` runs. |

### Why these choices

- **Validation lives on the entity** (`Car.Validate()`), so integrity rules
  (range ≥ 0, battery 0–100, valid enum values) can never be bypassed
  regardless of which code path creates or mutates a car.
- **A single exception hierarchy** (`SimulationException`) lets every
  incident — of whatever kind — be caught, logged, and turned into an alert
  through one code path (`FleetManager.SimulateIncident`), rather than
  scattering `try/catch` logic per incident type.
- **Delegates/events, not direct calls**, connect the simulation to
  "mission control." `FleetManager` has no idea the GUI exists — it just
  raises `OnAlert`. This keeps the alerting mechanism reusable (e.g. a
  logger or a remote telemetry client could subscribe too, unchanged).
- **LINQ** is used for all fleet-wide queries and the summary/statistics
  view, favoring expressive, declarative data operations over manual loops.
