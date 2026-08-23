# Tesla Autonomous Fleet — Self-Driving Car Simulation

A .NET WinForms application that simulates an autonomous vehicle fleet, tracks vehicle status and battery health, raises mission-control alerts, and supports user-driven fleet management through a desktop control panel.

## Overview

This project models a self-driving car fleet where each vehicle is represented as a validated domain entity and managed by a central fleet controller. It includes:

- vehicle registration, update, and removal
- battery and range monitoring
- operational status tracking
- incident simulation for critical failures
- alert dispatching to a control-room observer
- a GUI dashboard for fleet overview and operational logs

## Features

- Fleet dashboard with a grid of vehicles and live filtering by operational status
- Vehicle validation using DataAnnotations on the model
- Real-time alert events via delegates and event handlers
- Incident handling for battery, navigation, and unauthorized override scenarios
- Mission and fleet summary queries using LINQ
- Demo seed data for testing and presentation

## Requirements

- .NET SDK 10 or later
- Windows environment for WinForms

## Run the app

```bash
dotnet run
```

The app starts with a set of demo cars preloaded, including `TX-001` through `TX-004`.

## Project structure

- [Program.cs](Program.cs) — WinForms UI and app entry point
- [Car.cs](Car.cs) — vehicle entity, validation logic, and operational log
- [FleetManager.cs](FleetManager.cs) — fleet control, queries, and incident simulation
- [AlertSystem.cs](AlertSystem.cs) — alert publishing and monitoring system
- [Incidents.cs](Incidents.cs) — exception hierarchy for simulation incidents
- [Enums.cs](Enums.cs) — operational and mission-related enums
- [SelfDrivingCarSimulation.csproj](SelfDrivingCarSimulation.csproj) — project configuration

## Design notes

### 1. DataAnnotations on the model
The `Car` entity validates required fields, numeric ranges, and enum values by using `System.ComponentModel.DataAnnotations`.

This keeps data rules close to the model and avoids scattering validation logic across the UI.

### 2. Incident handling through a shared exception hierarchy
All simulation issues inherit from `SimulationException`, allowing the fleet manager to handle them uniformly through one catch path.

This keeps the logic consistent for:

- critical battery failures
- navigation errors
- restricted-zone entries
- unauthorized overrides

### 3. Event-based alert system
The fleet raises alerts using a delegate-based event model rather than directly calling UI methods.

This makes the alert system reusable and keeps the business logic independent from the presentation layer.

### 4. Fleet queries with LINQ
The fleet manager uses LINQ queries for status filtering, priority analysis, summary aggregation, and low-battery detection.

This provides a clean, readable way to inspect and summarize fleet conditions.

## Assignment requirement coverage

This implementation addresses the requested assignment elements:

- Car entity definition — implemented in [Car.cs](Car.cs)
- Incident handling mechanism — implemented in [Incidents.cs](Incidents.cs) and [FleetManager.cs](FleetManager.cs)
- Autonomous event delegation — implemented in [AlertSystem.cs](AlertSystem.cs)
- Dynamic vehicle data management — implemented in [FleetManager.cs](FleetManager.cs)
- User-centric control panel — implemented in [Program.cs](Program.cs)

## Notes

This version is structured as a multi-file WinForms project instead of a single monolithic source file, making the code easier to maintain and extend while preserving the same functionality.
