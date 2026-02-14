# CRITICAL: Master the Atom — Structural Map

**Version:** 1.0.1
**Date:** 2026-02-14

---

## 1. Project Directory Structure

```text
Critical/
|-- Assets/
|   |-- Scripts/
|   |   |-- Physics/      <- Core physics modules (GOLD and support)
|   |   |-- Reactor/      <- Reactor simulation engines
|   |   |-- Validation/   <- Heatup validation and telemetry visualizations
|   |   |-- UI/           <- Operator interface components
|   |   `-- Tests/        <- Unit and integration tests
|   |-- Scenes/
|   |-- Resources/
|   `-- Documentation/
|
|-- Governance/           <- Canonical governance root
|   |-- DomainPlans/
|   |-- ImplementationPlans/
|   |-- ImplementationReports/
|   |-- IssueRegister/
|   |-- Issues/
|   `-- Changelogs/
|
|-- Documentation/        <- Design docs and historical implementation docs
|   |-- Implementation/
|   |-- Updates/
|   |-- PROJECT_OVERVIEW.md
|   |-- PROJECT_TREE.md
|   `-- STRUCTURAL_MAP.md
|
|-- TechnicalDocumentation/   <- GOLD standards and lifecycle contracts
|-- Technical_Documentation/  <- NRC and engineering reference library
|-- HeatupLogs/
|-- Manuals/
|-- Updates/              <- Legacy archive and forensics (deprecated authority)
|-- ProjectSettings/
`-- UserSettings/
```

---
## 2. Code Architecture Overview

### 2.1 Architectural Layers

```
┌─────────────────────────────────────────────────────────────┐
│                   PRESENTATION LAYER                        │
│  (Unity MonoBehaviour classes in Validation/ and UI/)      │
│                                                             │
│  - HeatupValidationVisual.cs (heatup dashboard)            │
│  - ReactorOperatorScreen.cs (operator GUI)                 │
│  - MosaicBoard.cs (gauge coordinator)                      │
│  - CoreMosaicMap.cs (193-assembly display)                 │
└─────────────────────────────────────────────────────────────┘
                              ▲
                              │ Reads public state
                              │
┌─────────────────────────────────────────────────────────────┐
│                   COORDINATION LAYER                        │
│         (Simulation Engines - Unity MonoBehaviour)          │
│                                                             │
│  - HeatupSimEngine.cs (heatup simulation coordinator)      │
│  - ReactorSimEngine.cs (reactor simulation coordinator)    │
│                                                             │
│  Responsibilities:                                          │
│    • Manage simulation lifecycle (start/stop/pause)        │
│    • Call physics modules in correct order each timestep   │
│    • Maintain public state for UI consumption              │
│    • Handle time acceleration                              │
│    • NO INLINE PHYSICS (delegates to Physics layer)        │
└─────────────────────────────────────────────────────────────┘
                              ▲
                              │ Calls physics modules
                              │ Receives result structs
                              │
┌─────────────────────────────────────────────────────────────┐
│                     PHYSICS LAYER                           │
│            (Static classes in Physics/ namespace)           │
│                                                             │
│  Core Modules (GOLD standard):                             │
│    • ReactorKinetics.cs — Point kinetics equations         │
│    • WaterProperties.cs — IAPWS-IF97 correlations          │
│    • PressurizerPhysics.cs — Two-phase equilibrium         │
│    • RCSHeatup.cs — Thermal expansion & pressure           │
│    • CVCSController.cs — VCT level control                 │
│    • HeatTransfer.cs — Conduction, convection              │
│    • ThermalMass.cs — Bulk thermal inertia                 │
│    • SolidPlantPressure.cs — Solid-plant pressure rise     │
│    • AlarmManager.cs — Annunciator logic                   │
│                                                             │
│  Responsibilities:                                          │
│    • Implement physics calculations                        │
│    • Return results via typed structs                      │
│    • NO direct mutation of engine state                    │
│    • All constants from PlantConstants with sources        │
└─────────────────────────────────────────────────────────────┘
                              ▲
                              │ References constants
                              │
┌─────────────────────────────────────────────────────────────┐
│                   CONSTANTS LAYER                           │
│         (Static partial class PlantConstants.*)             │
│                                                             │
│  - PlantConstants.cs — RCS, SGs, physical constants        │
│  - PlantConstants.CVCS.cs — CVCS flows, VCT                │
│  - PlantConstants.Pressurizer.cs — PZR geometry, heaters   │
│  - PlantConstants.Pressure.cs — Setpoints, RCPs, RHR       │
│  - PlantConstants.Nuclear.cs — Core, reactivity, rods      │
│  - PlantConstants.Heatup.cs — Heatup rates, thermal mass   │
│  - PlantConstants.BRS.cs — Boron recycle system            │
│  - PlantConstants.Validation.cs — ValidateConstants()      │
│                                                             │
│  ALL values cite NRC/Westinghouse sources                  │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. Physics Module Dependency Graph

### 3.1 Heatup Simulation Dependencies

```
HeatupSimEngine (coordinator)
    │
    ├─→ TimeAcceleration          (time compression control)
    │
    ├─→ RCSHeatup                 (thermal expansion + pressure dynamics)
    │   ├─→ WaterProperties       (density, expansion coefficient)
    │   ├─→ ThermalExpansion      (volumetric thermal expansion)
    │   ├─→ HeatTransfer          (surge line heat loss)
    │   └─→ SolidPlantPressure    (compressibility)
    │
    ├─→ CVCSController            (VCT level control, heater control)
    │   ├─→ VCTPhysics            (VCT mass balance)
    │   └─→ PressurizerPhysics    (bubble formation detection)
    │
    ├─→ RCPSequencer              (RCP start sequence)
    │   ├─→ LoopThermodynamics    (natural circulation)
    │   └─→ RCSHeatup             (pressure check)
    │
    ├─→ AlarmManager              (annunciator evaluation)
    │   └─→ PlantConstants        (alarm setpoints)
    │
    └─→ RVLISPhysics              (vessel level indication)
        └─→ WaterProperties       (differential pressure conversion)
```

### 3.2 Reactor Simulation Dependencies

```
ReactorSimEngine (coordinator)
    │
    ├─→ ReactorController         (reactivity management)
    │   ├─→ ReactorKinetics       (point kinetics solver)
    │   │   └─→ PlantConstants    (β_eff, Λ, delayed neutron fractions)
    │   │
    │   ├─→ FeedbackCalculator    (reactivity coefficients)
    │   │   ├─→ PlantConstants    (α_D, α_M reference values)
    │   │   └─→ WaterProperties   (moderator density)
    │   │
    │   ├─→ PowerCalculator       (decay heat, fission power split)
    │   │   └─→ PlantConstants    (ANS decay heat curve)
    │   │
    │   └─→ ControlRodBank        (rod motion, worth curves)
    │       └─→ PlantConstants    (bank worth, steps, speeds)
    │
    ├─→ ReactorCore               (fuel assemblies, thermal-hydraulics)
    │   ├─→ FuelAssembly          (fuel pellet heat transfer)
    │   │   ├─→ HeatTransfer      (cylindrical conduction)
    │   │   └─→ WaterProperties   (coolant properties)
    │   │
    │   ├─→ LoopThermodynamics    (coolant energy balance)
    │   │   ├─→ WaterProperties   (enthalpy, cp)
    │   │   └─→ FluidFlow         (natural circulation correlations)
    │   │
    │   └─→ ThermalMass           (metal heat capacity)
    │       └─→ PlantConstants    (metal mass, cp)
    │
    ├─→ PressurizerPhysics        (two-phase pressure control)
    │   ├─→ WaterProperties       (saturation properties)
    │   ├─→ SteamThermodynamics   (steam properties)
    │   └─→ CoupledThermo         (flashing/condensation rates)
    │
    ├─→ SGSecondaryThermal        (steam generator heat removal)
    │   ├─→ HeatTransfer          (NTU-effectiveness)
    │   ├─→ WaterProperties       (primary side)
    │   └─→ SteamThermodynamics   (secondary side)
    │
    └─→ AlarmManager              (trip logic, annunciators)
        └─→ PlantConstants        (trip setpoints)
```

---

## 4. File Organization by Functional Domain

### 4.1 Physics Modules (Assets/Scripts/Physics/)

**Thermodynamics & Properties** (7 files)
- `WaterProperties.cs` — IAPWS-IF97 water/steam correlations
- `SteamThermodynamics.cs` — Steam-specific calculations
- `ThermalMass.cs` — Bulk heat capacity
- `ThermalExpansion.cs` — Volumetric expansion coefficient
- `FluidFlow.cs` — Natural circulation, pressure drop
- `HeatTransfer.cs` — Conduction, convection, NTU
- `CoupledThermo.cs` — Flashing/condensation kinetics

**Reactor Physics** (7 files)
- `ReactorKinetics.cs` — Point kinetics solver
- `FeedbackCalculator.cs` — Doppler, MTC, xenon, boron
- `PowerCalculator.cs` — Decay heat, fission power split
- `ControlRodBank.cs` — Bank motion, worth curves
- `FuelAssembly.cs` — Fuel pellet heat transfer
- `ReactorCore.cs` — 193-assembly core coordinator
- `LoopThermodynamics.cs` — RCS loop energy balance

**Primary System Components** (5 files)
- `PressurizerPhysics.cs` — Two-phase equilibrium
- `SolidPlantPressure.cs` — Solid-plant compressibility
- `RCSHeatup.cs` — Thermal expansion + pressure transients
- `CVCSController.cs` — VCT level control, heater control
- `VCTPhysics.cs` — Volume Control Tank mass balance

**Support Systems** (4 files)
- `RCPSequencer.cs` — Reactor coolant pump start sequence
- `RVLISPhysics.cs` — Vessel level indication
- `SGSecondaryThermal.cs` — Steam generator secondary side
- `BRSPhysics.cs` — Boron recycle system (future)

**Utilities** (3 files)
- `AlarmManager.cs` — Annunciator logic, trip conditions
- `TimeAcceleration.cs` — Simulation time compression
- `PlantConstants.*.cs` — 8 partial files with all constants

**Total Physics Files:** 26 modules + 8 constant files = **34 files**

---

### 4.2 Reactor Simulation Engines (Assets/Scripts/Reactor/)

**Main Coordinators** (2 files)
- `ReactorSimEngine.cs` — Phase 2 reactor simulation
- `ReactorController.cs` — Reactivity/power/rod coordinator

**Total Reactor Files:** 2 coordinators

---

### 4.3 Validation Engines (Assets/Scripts/Validation/)

**Heatup Simulation** (7 files)
- `HeatupSimEngine.cs` — Main heatup coordinator (partial class root)
- `HeatupSimEngine.Init.cs` — Initialization logic
- `HeatupSimEngine.BubbleFormation.cs` — 7-phase bubble state machine
- `HeatupSimEngine.Alarms.cs` — Alarm evaluation
- `HeatupSimEngine.Logging.cs` — CSV logging, history buffers
- `HeatupValidationVisual.cs` — Dashboard rendering (partial class root)
- `HeatupValidation.cs` — [DEPRECATED] Legacy heatup prototype

**Total Validation Files:** 7 files (1 deprecated)

---

### 4.4 User Interface (Assets/Scripts/UI/)

**Operator GUI Components** (14 files)
- `ReactorOperatorScreen.cs` — Master screen controller
- `CoreMosaicMap.cs` — 193-assembly interactive core map
- `AssemblyDetailPanel.cs` — Selected assembly info panel
- `CoreMapData.cs` — Static 193-assembly layout data
- `OperatorScreenBuilder.cs` — Editor menu tool for scene setup
- `MosaicBoard.cs` — Gauge data coordinator
- `MosaicBoardBuilder.cs` — Legacy builder (superseded by OperatorScreenBuilder)
- `MosaicBoardSetup.cs` — Legacy setup script
- `MosaicGauge.cs` — Individual gauge renderer
- `MosaicControlPanel.cs` — Rod control interface
- `MosaicRodDisplay.cs` — Bank position bars
- `MosaicAlarmPanel.cs` — Alarm annunciator strip
- `MosaicIndicator.cs` — Generic status light
- `MosaicTypes.cs` — Enums (GaugeType, AlarmStatus, BankType)

**Total UI Files:** 14 files

---

### 4.5 Test Infrastructure (Assets/Scripts/Tests/)

**Test Runners** (2 files)
- `Phase1TestRunner.cs` — Core physics validation (156 tests)
- `Phase2TestRunner.cs` — Reactor kinetics/core tests (95 tests)

**Integration Tests** (2 files)
- `HeatupIntegrationTests.cs` — HINT-01 through HINT-09
- `ReactorOperatorGUI_IntegrationTests.cs` — Operator GUI validation

**Base Classes** (1 file)
- `TestBase.cs` — Shared assertion infrastructure

**Total Test Files:** 5 files (251 total tests)

---

## 5. Unity Scene Hierarchy

### 5.1 Typical Heatup Validation Scene

```
Scene: HeatupValidation
├── HeatupSimEngine (MonoBehaviour)
│   ├── Public State Fields (read by Visual)
│   └── Coroutine: RunSimulation()
│
├── HeatupValidationVisual (MonoBehaviour)
│   ├── References HeatupSimEngine
│   └── OnGUI(): Renders dashboard
│
├── Phase1TestRunner (MonoBehaviour)
│   └── Menu: "Critical > Run Phase 1 Tests"
│
└── Main Camera
```

### 5.2 Typical Reactor Operation Scene

```
Scene: ReactorOperation
├── ReactorSimEngine (MonoBehaviour)
│   ├── Public State Fields
│   └── Coroutine: RunSimulation()
│
├── ReactorController (MonoBehaviour)
│   ├── Manages reactivity, rods, power
│   └── Called by ReactorSimEngine
│
├── ReactorOperatorCanvas (UI Canvas, 1920x1080)
│   └── ReactorOperatorScreen (MonoBehaviour)
│       ├── MosaicBoard (data coordinator)
│       ├── CoreMosaicMap (193 assembly cells)
│       ├── AssemblyDetailPanel (floating info)
│       ├── LeftGaugePanel (9 nuclear gauges)
│       ├── RightGaugePanel (8 thermal gauges)
│       └── BottomPanel
│           ├── RodControlSection
│           ├── BankDisplaySection (8 position bars)
│           ├── BoronControlSection
│           ├── TripControlSection
│           ├── TimeControlSection
│           └── AlarmSection
│
├── Phase2TestRunner (MonoBehaviour)
│   └── Menu: "Critical > Run Phase 2 Tests"
│
└── Main Camera
```

---

## 6. Data Flow Patterns

### 6.1 Simulation Engine → Physics Module → Result Struct

**Pattern:** Engines call physics modules, receive typed structs, update public state.

**Example: Solid Plant Pressure Rise**

```csharp
// In HeatupSimEngine.cs:

// 1. Prepare inputs
float currentTemp = rcsTemperature;
float currentPressure = rcsPressure;
float heaterPower = cvcsState.HeaterPowerKW;

// 2. Call physics module
SolidPlantResult result = SolidPlantPressure.CalculatePressureRise(
    currentTemp, 
    currentPressure, 
    heaterPower, 
    dt
);

// 3. Read result struct, update engine state
rcsPressure = result.NewPressure;
pressureRiseRate = result.DpDt;

// 4. Expose to UI
public float RCSPressure => rcsPressure;  // Read by HeatupValidationVisual
```

**Key Principle:** Physics module never sees `HeatupSimEngine` reference. It only knows inputs and returns outputs.

---

### 6.2 UI Component → Engine Public State → Gauge Display

**Pattern:** UI components read engine public state, never call physics modules directly.

**Example: Core Power Gauge**

```csharp
// In MosaicGauge.cs:

void Update()
{
    // Read from ReactorController public state
    float neutronPower = reactorController.NeutronPowerPercent;
    
    // Update gauge needle position
    needleRotation = Mathf.Lerp(minAngle, maxAngle, neutronPower / 100f);
    
    // Update digital readout
    valueText.text = neutronPower.ToString("F1");
}
```

**Key Principle:** UI never calls `ReactorKinetics.Calculate()` directly. It only reads `ReactorController.NeutronPowerPercent`.

---

### 6.3 CVCS PI Controller Feedback Loop

**Pattern:** Controller maintains internal state (PI integral), engine passes state by reference.

**Example: VCT Level Control**

```csharp
// In HeatupSimEngine.cs:

void InitializeSimulation()
{
    // Create CVCS controller state
    cvcsState = CVCSController.Initialize(
        initialVCTLevel: 50f,  // percent
        initialRCSPressure: rcsPressure
    );
}

void StepSimulation(float dt)
{
    // Pass state by reference, controller modifies it
    CVCSController.Update(
        ref cvcsState,          // PI integral updated here
        vctLevel,               // current measurement
        rcsPressure,            // used for bubble detection
        dt
    );
    
    // Read controller outputs
    float chargingFlow = cvcsState.ChargingFlowGPM;
    float letdownFlow = cvcsState.LetdownFlowGPM;
    
    // Apply flows to VCT
    VCTPhysics.Update(ref vctState, chargingFlow, letdownFlow, dt);
}
```

**Key Principle:** Controller owns its state (PI integral term). Engine owns VCT state. Controller reads VCT level, returns flow commands.

---

## 7. GOLD Standard Module Categories

### 7.1 Tier 1 — Core Physics (Immutable)

**These modules are LOCKED.** Any modification requires full re-validation.

- `PlantConstants.cs` (and all partials)
- `WaterProperties.cs`
- `SteamThermodynamics.cs`
- `ReactorKinetics.cs`
- `ThermalMass.cs`
- `ThermalExpansion.cs`
- `FluidFlow.cs`

**Rationale:** Fundamental physics. If wrong, entire simulation is invalid.

---

### 7.2 Tier 2 — Validated Component Models

**These modules are GOLD but can be enhanced with proper changelog documentation.**

- `PressurizerPhysics.cs`
- `SolidPlantPressure.cs`
- `RCSHeatup.cs`
- `CVCSController.cs`
- `VCTPhysics.cs`
- `HeatTransfer.cs`
- `FeedbackCalculator.cs`
- `PowerCalculator.cs`
- `ControlRodBank.cs`

**Allowed Changes:** 
- Add new features (e.g., new heater control mode)
- Fix bugs with validation
- Improve efficiency without changing results

**Forbidden Changes:**
- Change existing physics behavior without re-validation
- Remove public API methods (breaks references)

---

### 7.3 Tier 3 — Coordination Logic

**These are not GOLD (by definition — engines coordinate, they don't calculate physics).**

- `HeatupSimEngine.cs` (and partials)
- `ReactorSimEngine.cs`
- `ReactorController.cs`
- `RCPSequencer.cs`
- `AlarmManager.cs`
- `TimeAcceleration.cs`

**Rules:**
- Must delegate all physics to Tier 1/2 modules
- Can be refactored freely (no physics impact)
- Should follow GOLD template structure

---

### 7.4 Tier 4 — User Interface

**Not GOLD (no physics content).**

- All files in `UI/` directory
- All files in `Validation/` with "Visual" in name

**Rules:**
- Never perform physics calculations
- Read engine public state only
- Can be redesigned freely

---

## 8. Partial Class Split Patterns

### 8.1 PlantConstants (Split by Domain)

**Pattern:** One partial file per functional subsystem.

```
PlantConstants.cs              — Core RCS, SGs, physical constants
PlantConstants.CVCS.cs         — CVCS flows, VCT, CCP
PlantConstants.Pressurizer.cs  — PZR geometry, bubble, heaters
PlantConstants.Pressure.cs     — Setpoints, RCPs, RHR
PlantConstants.Nuclear.cs      — Core, reactivity, rods, xenon, boron
PlantConstants.Heatup.cs       — Heatup rates, mode temps, thermal mass
PlantConstants.BRS.cs          — Boron recycle (future system)
PlantConstants.Validation.cs   — ValidateConstants() method
```

**Why:** Each domain expert can work on their subsystem independently. All accessed as `PlantConstants.CONSTANT_NAME`.

---

### 8.2 HeatupSimEngine (Split by Function)

**Pattern:** One partial file per major subsystem.

```
HeatupSimEngine.cs               — Core state, lifecycle, main loop
HeatupSimEngine.Init.cs          — Initialization, setpoint calculations
HeatupSimEngine.BubbleFormation.cs — 7-phase bubble state machine
HeatupSimEngine.Alarms.cs        — Alarm evaluation logic
HeatupSimEngine.Logging.cs       — CSV output, history buffers
```

**Why:** Main file stays under 30 KB. Each partial handles one concern.

---

### 8.3 HeatupValidationVisual (Split by UI Layer)

**Pattern:** One partial file per rendering layer (not yet implemented, future if needed).

```
HeatupValidationVisual.cs        — OnGUI root, layout zones
HeatupValidationVisual.Graphs.cs — Trend plots
HeatupValidationVisual.Gauges.cs — Digital readouts, bars
HeatupValidationVisual.Styles.cs — Colors, fonts, GUIStyle definitions
```

---

## 9. Namespace Organization

### 9.1 Physics Modules

**Namespace:** `Critical.Physics`

**All files in `Assets/Scripts/Physics/`**

**Rationale:** Physics is pure calculation. No Unity dependencies (except `[System.Serializable]` for structs).

---

### 9.2 Simulation Engines

**Namespace:** None (root namespace)

**Files:** `ReactorSimEngine.cs`, `ReactorController.cs`

**Rationale:** Unity MonoBehaviour classes. Top-level coordinator role.

---

### 9.3 Validation Engines

**Namespace:** None (root namespace)

**Files:** `HeatupSimEngine.cs`, `HeatupValidationVisual.cs`

**Rationale:** Unity MonoBehaviour classes.

---

### 9.4 UI Components

**Namespace:** `Critical.UI`

**All files in `Assets/Scripts/UI/`**

**Rationale:** UI layer. Imports UnityEngine.UI.

---

### 9.5 Tests

**Namespace:** `Critical.Tests`

**All files in `Assets/Scripts/Tests/`**

**Rationale:** Test infrastructure. Inherits from `TestBase`.

---

## 10. Assembly Definition Files (asmdef)

### 10.1 Critical.Physics.asmdef

**Location:** `Assets/Scripts/Physics/Critical.Physics.asmdef`

**Purpose:** Compiles all physics modules into a separate assembly.

**Benefits:**
- Faster compile times (Unity only recompiles changed assemblies)
- Enforces dependency rules (physics cannot reference UI)
- Enables future NuGet packaging

**References:** None (pure C# + System)

---

### 10.2 Future: Critical.Reactor.asmdef

**Planned for:** v1.1.0

**Would contain:** ReactorSimEngine, ReactorController

**References:** Critical.Physics

---

### 10.3 Future: Critical.UI.asmdef

**Planned for:** v1.1.0

**Would contain:** All UI components

**References:** UnityEngine.UI, Critical.Physics

---

## 11. Key Entry Points for Developers

### 11.1 "I want to run the heatup simulation"

1. Open scene: `Scenes/HeatupValidation`
2. Press Play in Unity
3. Observe dashboard (HeatupValidationVisual renders OnGUI)
4. Logs written to: `HeatupLogs/HeatupLog_YYYY-MM-DD_HH-MM-SS.csv`

---

### 11.2 "I want to run the reactor simulation"

1. Open scene: `Scenes/ReactorOperation`
2. Press Play in Unity
3. Press '1' key to show Reactor Operator Screen
4. Use rod controls to manipulate power
5. Observe core mosaic map, gauges, alarms

---

### 11.3 "I want to run all unit tests"

1. Menu: **Critical > Run Phase 1 Tests** (156 tests)
2. Menu: **Critical > Run Phase 2 Tests** (95 tests)
3. Check console for pass/fail results

---

### 11.4 "I want to create a new operator screen"

1. Create or open a scene
2. Menu: **Critical > Create Operator Screen**
3. Hierarchy automatically built
4. ReactorController reference auto-assigned
5. Press Play, press '1' to toggle screen

---

### 11.5 "I want to add a new physics module"

1. Create file in `Assets/Scripts/Physics/`
2. Use template from `Documentation/GOLD_STANDARD_TEMPLATE.md` (Section 2.1)
3. Add constants to appropriate `PlantConstants.*.cs` file
4. Write unit tests in `Phase1TestRunner.cs` or `Phase2TestRunner.cs`
5. Validate against NRC sources
6. Document in changelog before commit

---

### 11.6 "I want to add a new constant"

1. Identify domain (CVCS? Pressurizer? Nuclear? etc.)
2. Open appropriate `PlantConstants.*.cs` file
3. Add constant in correct `#region`
4. Add XML doc comment with source citation: `/// <summary>Description. Source: NRC HRTD X.X</summary>`
5. Run `PlantConstants.ValidateConstants()` to check for conflicts

---

## 12. Build Output and Logs

### 12.1 Simulation Logs

**Directory:** `<project_root>/HeatupLogs/`

**Format:** CSV with 30-minute intervals

**Filename:** `HeatupLog_YYYY-MM-DD_HH-MM-SS.csv`

**Columns:**
- Sim Time (hr)
- RCS Temp (°F)
- RCS Pressure (psia)
- PZR Level (%)
- VCT Level (%)
- Charging Flow (gpm)
- Letdown Flow (gpm)
- RCP Status (0/1)
- Bubble Formation Phase (0-7)
- Heater Power (kW)

---

### 12.2 Unity Build

**Target Platforms:**
- Windows Standalone (primary)
- macOS Standalone (secondary)
- Linux Standalone (future)

**Build Output:** `<project_root>/Build/`

**Executable:** `Critical.exe` (Windows)

---

## 13. Version Control and Branching

### 13.1 Main Branch

**Branch:** `main`

**Status:** Stable releases only

**Merge Criteria:**
- All tests pass (251/251)
- Changelog updated
- GOLD modules unchanged or re-validated

---

### 13.2 Development Branch

**Branch:** `develop`

**Status:** Active development

**Merge Criteria:**
- Feature complete
- Unit tests added for new features
- No regressions

---

### 13.3 Feature Branches

**Pattern:** `feature/v0.X.X-description`

**Example:** `feature/v1.1.0-per-assembly-power`

**Merge To:** `develop` when complete

---

## 14. External Dependencies

### 14.1 Unity Packages

- **Unity UI** (com.unity.ugui) — GUI rendering
- **Unity Test Framework** (com.unity.test-framework) — Unit testing
- **Text Mesh Pro** (com.unity.textmeshpro) — Enhanced text rendering

**All standard Unity packages. No third-party plugins.**

---

### 14.2 NRC Reference Documents

**ADAMS Database:** https://adams.nrc.gov/

**Key Documents:**
- ML11223A342 (HRTD 19.0 — Plant Operations)
- ML11223A213 (HRTD 3.2 — RCS)
- ML11223A214 (HRTD 4.1 — CVCS)
- ML11251A014 (HRTD 2.1 — Pressurizer)
- ML11223A287 (HRTD 10.2 — Pressure Control)
- ML11223A289 (HRTD 8.1 — Control Rods)
- ML11223A291 (HRTD 8.3 — Reactor Kinetics)

**Storage:** Reference PDFs stored in `Manuals/` directory (not in Git due to size).

---

### 14.3 Thermodynamic Data

**NIST Chemistry WebBook:** https://webbook.nist.gov/chemistry/

**Used for:** IAPWS-IF97 validation data (saturation pressure, saturation temperature, latent heat).

---

## 15. Future Structural Enhancements

### Planned for v1.1.0
- Per-assembly power distribution (nodal diffusion or simplified)
- Individual rod position tracking (vs. bank-averaged)
- Enhanced ReactorCore with 193 FuelAssembly objects

### Planned for v1.2.0
- Secondary side detailed modeling (SG recirculation)
- Turbine-generator control
- Condenser/feedwater systems

### Planned for v2.0.0
- Accident scenarios (LOCA, SGTR, SBO)
- Emergency core cooling systems (ECCS)
- Containment modeling

---

**End of Structural Map**

