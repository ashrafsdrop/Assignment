# Tesla Autonomous Fleet — Self-Driving Car Simulation

A .NET WinForms application that simulates an autonomous vehicle fleet, tracks vehicle status and battery health, raises mission-control alerts, and supports user-driven fleet management through a desktop control panel.

## Overview

This project models a self-driving car fleet where each vehicle is represented as a validated domain entity and managed by a central fleet controller. The app is designed to show how a real autonomous fleet system might handle:

- vehicle registration and lifecycle
- mission priority and operational states
- battery warnings and risk conditions
- route completion and restricted-zone events
- incident detection and response
- alert routing to a control room

## Features

- Fleet dashboard with DataGridView listing all cars
- Status-based filtering for fleet views
- Add, update, and delete vehicle records
- DataAnnotations-based validation on the car model
- Real-time control-room alerts through delegates/events
- Incident simulation for battery, navigation, and override problems
- Operational logs for each vehicle
- Summary and analysis queries using LINQ

## Requirements

- .NET SDK 10 or later
- Windows environment for WinForms

## Run the app

```bash
dotnet run
```

The app starts with demo vehicles already loaded, including `TX-001` through `TX-004`.

## Project structure

### Core application
- [Program.cs](Program.cs) — main WinForms application, dashboard, controls, and app startup

### Domain and models
- [Car.cs](Car.cs) — vehicle entity model and validation rules
- [Enums.cs](Enums.cs) — operational states, priorities, and alert/event enums
- [Incidents.cs](Incidents.cs) — custom exception hierarchy used for simulation failures

### Business logic
- [FleetManager.cs](FleetManager.cs) — fleet management, queries, route events, and incident logic
- [AlertSystem.cs](AlertSystem.cs) — event publisher, alert args, and control-room monitor

### Project config
- [SelfDrivingCarSimulation.csproj](SelfDrivingCarSimulation.csproj) — .NET project configuration

## File-by-file explanation

### [Program.cs](Program.cs)
This file acts as the UI layer and app entry point.

Main responsibilities:
- starts the WinForms app with `ApplicationConfiguration.Initialize();`
- defines `MainForm`, the main dashboard window
- builds all controls, tabs, and form layout
- binds fleet data to a DataGridView
- handles add/update/remove actions
- displays alert history and selected vehicle logs
- seeds demo data for initial testing

Key functions:
- `MainForm()` — constructor that initializes the dashboard and loads demo data
- `BuildLayout()` — creates the form, tabs, controls, and event handlers
- `RefreshFleet()` — refreshes the displayed fleet list based on selected filter
- `ShowSelectedLog()` — shows the selected car's log entries
- `AddCar()` — validates and adds a new vehicle to the fleet
- `UpdateSelected()` — updates currently selected vehicle values
- `RemoveSelected()` — removes the selected vehicle from the fleet
- `SeedDemoData()` — populates the simulator with example cars
- `RunSelected()` — wrapper method to trigger alert-driven incident logic
- `CompleteRoute()`, `TriggerCriticalBattery()`, `TriggerNavigationError()`, `EnterRestrictedZone()` — UI-level sample actions that simulate fleet events

### [Car.cs](Car.cs)
This file defines the core vehicle model.

Main responsibilities:
- represents each autonomous car in the simulation
- stores vehicle data like ID, model name, battery, range, priority, and status
- validates input data using DataAnnotations
- tracks the operational log for each vehicle

Key members:
- `CarId` — unique vehicle identifier, required
- `ModelName` — vehicle name or model string, required
- `OperationalRangeKm` — maximum range in kilometers
- `BatteryLevelPercent` — current battery level from 0 to 100
- `Priority` — mission priority enum
- `Status` — current operational state enum
- `OperationalLog` — readonly list of timestamped events

Key functions:
- `Car(...)` constructor — creates a new car and validates it immediately
- `Validate()` — runs DataAnnotations validation
- `LogEvent(string message)` — appends a timestamped event to the log
- `ToString()` — returns a readable vehicle summary string

### [Enums.cs](Enums.cs)
This file centralizes the enumerations used throughout the app.

Enum definitions:
- `OperationalStatus` — Idle, EnRoute, Charging, InMaintenance, Stopped
- `MissionPriority` — Low, Medium, High, Critical
- `ControlRoomEventType` — RouteCompleted, ObstacleEncountered, RestrictedZoneEntered, IncidentRaised, IncidentResolved
- `SeverityLevel` — Info, Warning, Critical

These enums are used by the fleet logic, alert system, and UI.

### [Incidents.cs](Incidents.cs)
This file defines the simulation’s exception model.

Main responsibilities:
- handle all incident types in a unified way
- carry severity and timestamp metadata
- allow the fleet manager to catch a common base exception type

Key classes:
- `SimulationException` — abstract base class for all simulation-related failures
- `CriticalBatteryException` — raised when battery is critically low
- `NavigationErrorException` — raised for GPS/routing failures
- `UnauthorizedOverrideException` — raised for invalid override attempts
- `CarDataValidationException` — raised when model data is invalid
- `CarNotFoundException` — raised when a processing request references a missing vehicle

Key properties:
- `Severity` — warning or critical level
- `OccurredAt` — timestamp of the issue
- `CarId` — associated vehicle ID when available

### [AlertSystem.cs](AlertSystem.cs)
This file contains the event-based alerting system used by the simulation.

Main responsibilities:
- create alert payload objects
- publish alerts from the fleet manager
- store alert history in a monitor

Key members:
- `ControlRoomAlertEventArgs` — carries alert details
- `ControlRoomAlertHandler` — delegate signature for event listeners
- `AlertDispatcher` — the publisher of alert events
- `ControlRoomMonitor` — subscriber that stores alert history

Key functions:
- `AlertDispatcher.RaiseAlert(...)` — raises an alert to all subscribers
- `ControlRoomMonitor.HandleAlert(...)` — stores each incoming alert in history
- `ControlRoomAlertEventArgs.ToString()` — formats a log-friendly message

### [FleetManager.cs](FleetManager.cs)
This is the main business-logic file for the self-driving fleet.

Main responsibilities:
- maintain the in-memory fleet collection
- add, update, remove, and retrieve cars
- query vehicle statistics and priorities
- simulate route and incident events
- raise alerts when system conditions change

Key members:
- `_fleet` — dictionary of all cars keyed by ID
- `Alerts` — alert dispatcher for mission-control notifications
- `Cars` — read-only collection of fleet vehicles
- `LowBatteryThreshold` — threshold used in low battery checks

Key functions:
- `GenerateCarId()` — creates a unique identifier such as `TX-005`
- `AddCar(Car car)` — validates and adds a new vehicle
- `GetCar(string carId)` — returns a specific vehicle or throws `CarNotFoundException`
- `RemoveCar(string carId)` — removes a car from the fleet
- `UpdateCar(string carId, Action<Car> updateAction)` — updates the selected car safely
- `GetCarsByStatus(OperationalStatus status)` — filters cars by status
- `GetPriorityMissions()` — returns high/critical priority vehicles
- `GetLowBatteryCars(double threshold = ...)` — lists vehicles with low battery
- `GetFleetSummary()` — provides fleet totals and counts by status and priority
- `CompleteRoute(string carId)` — marks route as complete and raises a route alert
- `EncounterObstacle(string carId, string description)` — logs obstacle data and alerts mission control
- `EnterRestrictedZone(string carId, string zoneName)` — stops the vehicle and raises a critical alert
- `SimulateIncident(string carId, Action trigger)` — safe wrapper for executing and handling incidents
- `HandleSimulationException(...)` — logs the incident and updates vehicle status
- `TriggerCriticalBattery(...)` — sets a battery level and triggers a critical battery exception
- `TriggerNavigationError(...)` — triggers a navigation incident
- `TriggerUnauthorizedOverride(...)` — triggers a security override incident

## Design notes

### 1. DataAnnotations on the model
The `Car` entity validates required fields, numeric ranges, and enum values using `System.ComponentModel.DataAnnotations`.

This keeps data rules close to the model and avoids scattering validation logic across the UI.

### 2. Shared exception hierarchy
All simulation incidents inherit from `SimulationException`, allowing the fleet manager to handle them with one common pattern.

This makes it easier to:
- handle errors consistently
- log them uniformly
- raise alerts without duplicating code

### 3. Event-based alert system
The fleet raises alerts using a delegate-based event model rather than calling UI methods directly.

This keeps the simulation logic independent from the visual layer while allowing any observer, including the GUI, to subscribe.

### 4. LINQ-driven fleet logic
The app uses LINQ extensively to filter vehicles, summarize fleet status, and identify low-battery or high-priority vehicles.

This makes queries declarative and easy to maintain.

## Assignment requirement coverage

This implementation addresses the requested assignment elements:

- Car entity definition — implemented in [Car.cs](Car.cs)
- Incident handling mechanism — implemented in [Incidents.cs](Incidents.cs) and [FleetManager.cs](FleetManager.cs)
- Autonomous event delegation — implemented in [AlertSystem.cs](AlertSystem.cs)
- Dynamic vehicle data management — implemented in [FleetManager.cs](FleetManager.cs)
- User-centric control panel — implemented in [Program.cs](Program.cs)

## Technical summary

This project demonstrates a simple but complete autonomous fleet simulation using clean separation between:

- models
- business logic
- exceptions
- event-driven alerts
- Windows Forms UI

This makes the project easier to understand, test, and extend compared with a single monolithic source file.

## Notes

The project is intentionally organized by responsibility so each file has a distinct purpose and the code remains maintainable even as more vehicle rules, alerts, and UI workflows are added.
