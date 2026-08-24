# IUBAT – INTERNATIONAL UNIVERSITY OF BUSINESS AGRICULTURE AND TECHNOLOGY
### College of Engineering and Technology (CEAT)
### Department of Computer Science & Engineering

# ASSIGNMENT REPORT
**Autonomous Vehicle Operational Control and Incident Response Simulation**

**Student Name:** Mostak Shahriar  
**Student ID:** 23303076  
**Course:** Visual Programming  
**Course Code:** CSC 439  
**Semester:** Summer 2026  
**Instructor:** Sheekar Banerjee, Lecturer, Dept. of CSE, IUBAT  

**Submission Type:** Assignment Documentation and C# Source Code  
*(Prepared for academic submission)*

---

## Table of Contents
1. Introduction
2. System Objectives and Requirements Mapping
3. Program Structure and Architecture
4. Data Integrity and Data Annotation
5. Exception Handling and Incident Response
6. Delegate-Based Event Notification
7. LINQ and Dynamic Vehicle Data Management
8. User-Centric Control Panel (GUI)
9. Operational Safety, Reliability and Design Justification
10. Testing Scenarios and Expected Results
11. Conclusion

---

## 1. Introduction
The assignment brief asks for a prototype Self-driving Car Simulation System in C# that can manage autonomous vehicle statuses, simulate dynamic driving conditions, respond to system exceptions, and communicate critical operational events to mission control in real time. The intended system is for internal simulation use and must demonstrate operational safety, exception handling, flexible event management, dynamic data operations, and data integrity validation.

## 2. System Objectives and Requirements Mapping
The proposed solution is designed as a modular Windows Forms simulation application. Each vehicle is represented by a `Car` object. A central in-memory dictionary manages active vehicles, while LINQ is used for filtering, sorting, and summarizing data. A delegate-based alert mechanism sends incident and mission events to a mission-control panel. Data annotations and explicit validation rules protect the integrity of critical vehicle fields.

| Assignment Requirement | Implementation Choice | Purpose |
| --- | --- | --- |
| Car entity definition | `Car` class + enums + DataAnnotations | Represents identity, range, battery, priority, and log data. |
| Incident handling | Custom exceptions + safe-state logic | Prevents crashes and moves unsafe cars to controlled states. |
| Event delegation | `AlertDispatcher` + event notifications | Sends route, obstacle, and restricted-zone alerts. |
| Dynamic data management | `FleetManager` with LINQ queries | Provides expressive filtering and real-time fleet summaries. |
| User interface | `MainForm` with TabControl & DataGridView | Offers a user-centric dashboard to monitor and command the fleet. |

## 3. Program Structure and Architecture
The system is divided into clear functional components to ensure separation of concerns:
* **Models (`Car.cs`, `Enums.cs`):** Defines the domain entities, enumerations (Status, Priority, Events), and internal operational logs.
* **Business Logic (`FleetManager.cs`):** Manages the fleet collection, exposes LINQ queries, and acts as the central executor for all simulated incidents.
* **Alert System (`AlertSystem.cs`):** Decouples the simulation events from the GUI using C# delegates and events.
* **Incident Hierarchy (`Incidents.cs`):** Defines a common `SimulationException` base class to safely handle all failures.
* **User Interface (`Program.cs`):** The `MainForm` acts as the mission control panel, rendering data grids, alerts, and vehicle logs across intuitive tabs.

## 4. Data Integrity and Data Annotation
To ensure that corrupted or illogical vehicle data cannot enter the simulation, the `Car` class uses `System.ComponentModel.DataAnnotations`.
* Properties such as `OperationalRangeKm` and `BatteryLevelPercent` use `[Range]` attributes to enforce physical limits.
* The `CarId` and `ModelName` properties are marked `[Required]`.
* The `Validate()` method ensures data is checked at the time of instantiation and after any manual updates from the control panel. If a rule is violated, a `CarDataValidationException` is thrown and gracefully caught by the UI.

## 5. Exception Handling and Incident Response
Robust error handling is critical for mission-control systems. All simulated incidents inherit from the `SimulationException` base class, which carries a `SeverityLevel` and a timestamp. 
* A central `SimulateIncident` method in the `FleetManager` uses a unified `try-catch` block.
* When a `CriticalBatteryException` occurs, the vehicle's state is safely transitioned to `Charging`.
* When a `NavigationErrorException` or `UnauthorizedOverrideException` occurs, the vehicle is forced to `Stopped`.
* This ensures the simulation itself never crashes, but instead resolves incidents into safe states and alerts the operators.

## 6. Delegate-Based Event Notification
The simulation logic must remain decoupled from the Windows Forms UI. To achieve this, the `AlertSystem` relies on C# delegates.
* An `AlertDispatcher` publishes `OnAlert` events carrying a `ControlRoomAlertEventArgs` payload.
* The `MainForm` subscribes to this event. When an incident occurs (e.g., entering a restricted zone), the `FleetManager` tells the dispatcher to raise an alert, which asynchronously populates the Mission Control alert history list without the simulation knowing about the UI components.

## 7. LINQ and Dynamic Vehicle Data Management
The `FleetManager` exposes several methods to dynamically query the live fleet data using Language Integrated Query (LINQ):
* **Filtering:** `GetCarsByStatus()` filters vehicles by their operational state.
* **Sorting:** `GetPriorityMissions()` chains `Where`, `OrderByDescending`, and `ThenBy` to find critical missions with low batteries.
* **Aggregation:** `GetFleetSummary()` uses grouping and averaging to provide a real-time dashboard of the fleet's average battery, total vehicles, and mission priorities.

## 8. User-Centric Control Panel (GUI)
The graphical interface is built using standard WinForms controls arranged in a responsive `MainForm`.
* **Tab Navigation:** The dashboard separates concerns into "Fleet" (grid view), "Alert history" (mission events), and "Operational logs" (per-vehicle history).
* **Control Actions:** A dedicated right-hand side panel provides quick controls to add, update, and remove vehicles, as well as trigger dynamic simulation events like "Critical Battery" and "Restricted Zone".
* **Real-time Feedback:** The DataGridView and summary labels are automatically refreshed whenever the fleet state changes.

> **[PLACE SCREENSHOT HERE: Main Dashboard]**
> *(Insert a screenshot showing the main GUI with the populated DataGridView and summary labels.)*

## 9. Operational Safety, Reliability and Design Justification
The application is designed around safety and reliability. By using a strict domain model (`Car`), we guarantee data integrity. By centralizing exception handling within the `FleetManager`, we guarantee that no incident goes unhandled. The event-driven architecture ensures that the underlying simulation could theoretically run headlessly or be connected to a different user interface (e.g., WPF or a web API) without modification.

## 10. Testing Scenarios and Expected Results
1. **Adding Valid/Invalid Vehicles:** Adding a car with 105% battery will trigger a validation error, preventing addition.
> **[PLACE SCREENSHOT HERE: Validation Error MessageBox]**

2. **Executing an Incident:** Clicking "Navigation error" will halt the selected car, append a warning to its operational log, update the fleet grid, and broadcast a critical alert to the mission control panel.
> **[PLACE SCREENSHOT HERE: Alert History Tab]**

3. **Filtering:** Selecting a specific operational status from the filter drop-down will immediately update the DataGridView and recalculate the fleet averages via LINQ.
4. **Log Retention:** Selecting a car from the grid will instantly populate the "Operational logs" tab with that specific car's timestamped history.
> **[PLACE SCREENSHOT HERE: Operational Logs Tab]**

## 11. Conclusion
The implemented Self-Driving Car Simulation successfully meets all requirements of the CSC 439 assignment. It demonstrates a robust grasp of object-oriented C# principles, including data annotations, custom exception hierarchies, delegate-driven event notifications, and dynamic LINQ queries, all wrapped within a responsive WinForms graphical user interface.

---

## Appendix A. Core Class Implementation Overview
*(Note: Full source code files are available in the project repository. Key snippets are highlighted here.)*

**The Car Model (Data Annotation and Logging):**
```csharp
public class Car
{
    [Required]
    public string CarId { get; }
    
    [Range(0, 100)]
    public double BatteryLevelPercent { get; set; }
    
    // Validates object constraints programmatically
    public void Validate()
    {
        var validationContext = new ValidationContext(this);
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(this, validationContext, validationResults, validateAllProperties: true))
            throw new CarDataValidationException(validationResults.First().ErrorMessage);
    }
}
```

**Fleet Manager (LINQ Aggregation):**
```csharp
public FleetSummary GetFleetSummary()
{
    var cars = _fleet.Values.ToList();
    return new FleetSummary
    {
        TotalCars = cars.Count,
        AverageBatteryPercent = cars.Count == 0 ? 0 : cars.Average(c => c.BatteryLevelPercent),
        LowBatteryCount = cars.Count(c => c.BatteryLevelPercent <= LowBatteryThreshold)
    };
}
```

**Exception Handling (Safe Incident Resolution):**
```csharp
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
```

---

## Appendix B. Application Screenshots

*(This section is reserved for additional high-resolution screenshots of the application running, to satisfy the documentation requirements.)*

> **[PLACE ADDITIONAL SCREENSHOTS HERE]**
> - *Figure 1: Main Dashboard displaying active autonomous fleet.*
> - *Figure 2: Real-time Mission Control Alerts triggered by custom SimulationExceptions.*
> - *Figure 3: Individual vehicle operational log view.*
