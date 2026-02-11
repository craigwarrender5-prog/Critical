# CRITICAL: Master the Atom — GOLD Standard Module Template

**Version:** 1.0
**Date:** 2026-02-07
**Applies To:** All source files in the CRITICAL project (Physics, Reactor, Validation, UI, Tests)

This document defines the mandatory structure and quality criteria for every module in the project. All new modules must follow this template. All existing modules must be brought into compliance during refactoring.

---

## 1. GOLD Standard Criteria

Every module must satisfy all 10 criteria to be classified as GOLD.

| # | Criterion | Requirement |
|---|-----------|-------------|
| **G1** | **Single Responsibility** | Each file does one thing. Physics modules calculate physics. Engines coordinate modules. GUI renders state. Tests verify behavior. Constants store constants. |
| **G2** | **Documented Header** | File header block states: purpose, physics basis (if applicable), NRC/industry sources, units convention, and architectural role. |
| **G3** | **No Inline Physics in Engines** | Simulation engines (HeatupSimEngine, ReactorSimEngine) delegate all calculations to physics modules. They never perform physics calculations inline. |
| **G4** | **Result Structs** | Physics modules communicate through typed structs. They return results rather than mutating engine state directly. Input structs consolidate parameters; output structs bundle results. |
| **G5** | **Constants from PlantConstants** | Shared physical constants come from `PlantConstants`. Module-specific constants (internal tuning, local setpoints) may be declared locally but must be documented with source citation. |
| **G6** | **Validated Parameters** | Every NRC/Westinghouse-sourced value cites its source document (e.g., "NRC HRTD 19.2.1", "WCAP-12345", "Westinghouse FSAR Table 4.1-1"). |
| **G7** | **Namespace Compliance** | Physics modules: `Critical.Physics`. UI modules: `Critical.UI`. Controllers: `Critical.Controllers`. Tests: `Critical.Tests`. |
| **G8** | **Manageable Size** | Target: < 30 KB per file. Hard limit: 40 KB. Files exceeding the hard limit must be split using `partial class`. |
| **G9** | **No Dead Code** | No `[Obsolete]` methods without an active removal plan. No commented-out code blocks. No unused variables or fields. |
| **G10** | **No Duplication** | Shared logic is extracted to common utilities or base classes. Constants are defined once. Assertion boilerplate is inherited, not copy-pasted. |

---

## 2. Module Templates

### 2.1 Physics Module (Static Class)

Used for: Physics calculations, thermodynamic models, instrument simulation, alarm logic.

Examples: `SolidPlantPressure.cs`, `RCSHeatup.cs`, `AlarmManager.cs`, `WaterProperties.cs`

```csharp
// ============================================================================
// CRITICAL: Master the Atom - Physics Module
// [FileName].cs - [Short Description]
// ============================================================================
//
// PURPOSE:
//   [What this module calculates/controls. One or two sentences.]
//
// PHYSICS:
//   [Key equations and physical principles. Use actual variable names.]
//   [Example:]
//   dP/dt = (dV_thermal/dt - dV_cvcs/dt) / (V_total × κ)
//   where:
//     dV_thermal/dt = V × β × dT/dt   (thermal expansion rate)
//     dV_cvcs/dt    = (letdown - charging) / ρ   (CVCS net removal)
//     κ             = isothermal compressibility of water
//
// SOURCES:
//   - [NRC HRTD Section X.X - Title]
//   - [WCAP-XXXXX or NUREG-XXXX if applicable]
//   - [Westinghouse FSAR reference if applicable]
//
// UNITS:
//   [State the unit convention for this module]
//   Temperature: °F | Pressure: psia | Flow: gpm | Volume: ft³
//   Mass: lb | Power: MW or kW | Time: hours (unless noted)
//
// ARCHITECTURE:
//   [How this module fits into the simulation]
//   - Called by: [which engine or coordinator]
//   - Delegates to: [which other modules it calls]
//   - State owned: [what state this module is responsible for]
//
// GOLD STANDARD: [Yes/No — set to Yes when all G1-G10 criteria are met]
// ============================================================================

using System;

namespace Critical.Physics
{
    // ========================================================================
    // RESULT STRUCT (G4)
    // Returned by public methods. Engine reads results; module never
    // mutates engine state directly.
    // ========================================================================

    /// <summary>
    /// [Description of what this result represents].
    /// Returned by [MethodName]().
    /// </summary>
    public struct [Module]Result
    {
        public float PrimaryValue;       // [unit] — [description]
        public float SecondaryValue;     // [unit] — [description]
        public bool  StatusFlag;         // [description]
    }

    // ========================================================================
    // STATE STRUCT (G4) — Only if the module maintains persistent state
    // Owned by the engine, passed by ref to Update().
    // ========================================================================

    /// <summary>
    /// Persistent state for [module description].
    /// Created by Initialize(), updated by Update(), read by engine.
    /// </summary>
    public struct [Module]State
    {
        // Primary state variables
        public float StateVariable1;     // [unit] — [description]
        public float StateVariable2;     // [unit] — [description]

        // Diagnostic / display outputs
        public float DiagnosticValue;    // [unit] — [description]
    }

    // ========================================================================
    // INPUT STRUCT (G4) — Only if the module has many input parameters
    // Consolidates parameters to avoid long method signatures.
    // ========================================================================

    /// <summary>
    /// Input parameters for [module] calculations.
    /// Consolidates values to keep method signatures clean.
    /// </summary>
    public struct [Module]Inputs
    {
        public float InputParam1;        // [unit] — [description]
        public float InputParam2;        // [unit] — [description]
    }

    // ========================================================================
    // MODULE CLASS
    // ========================================================================

    /// <summary>
    /// [Full module description. What it owns, what it calculates.]
    /// 
    /// Called by [engine name]. Returns [result type].
    /// See file header for physics basis and NRC sources.
    /// </summary>
    public static class [ModuleName]
    {
        // ====================================================================
        // CONSTANTS (G5, G6)
        // Module-specific constants with source citations.
        // Shared constants come from PlantConstants.
        // ====================================================================

        #region Constants

        /// <summary>
        /// [Description]. Source: [NRC HRTD X.X / WCAP-XXXXX / etc.]
        /// </summary>
        private const float MODULE_SPECIFIC_CONSTANT = 123.4f;

        #endregion

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        #region Public API

        /// <summary>
        /// Initialize state for [starting condition].
        /// Called once at simulation start.
        /// </summary>
        /// <param name="initialParam">[Description] ([unit])</param>
        /// <returns>Initialized state struct</returns>
        public static [Module]State Initialize(float initialParam)
        {
            var state = new [Module]State();
            state.StateVariable1 = initialParam;
            // ... initialization logic
            return state;
        }

        /// <summary>
        /// Advance the module by one timestep.
        /// Called every physics step by the simulation engine.
        /// </summary>
        /// <param name="state">Module state (modified in place)</param>
        /// <param name="dt">Timestep (hours)</param>
        public static void Update(ref [Module]State state, float dt)
        {
            // Physics calculations here
            // Delegate to other modules as needed
            // Update state fields with results
        }

        /// <summary>
        /// Calculate [specific value] without modifying state.
        /// Pure function — no side effects.
        /// </summary>
        /// <param name="input">[Description] ([unit])</param>
        /// <returns>[Description] ([unit])</returns>
        public static float Calculate[Value](float input)
        {
            // Stateless calculation
            return result;
        }

        #endregion

        // ====================================================================
        // PRIVATE METHODS
        // ====================================================================

        #region Private Methods

        /// <summary>
        /// [Description of internal helper].
        /// </summary>
        private static float InternalHelper(float param)
        {
            // Implementation
            return result;
        }

        #endregion
    }
}
```

---

### 2.2 Simulation Engine (MonoBehaviour)

Used for: Top-level simulation coordinators that orchestrate physics modules and manage Unity lifecycle.

Examples: `HeatupSimEngine.cs`, `ReactorSimEngine.cs`

```csharp
// ============================================================================
// CRITICAL: Master the Atom - Simulation Engine
// [FileName].cs - [Short Description]
// ============================================================================
//
// PURPOSE:
//   [What simulation this engine runs. What it coordinates.]
//
// ARCHITECTURE:
//   This is a COORDINATOR, not a physics module.
//   All physics are delegated to modules in Critical.Physics.
//   This engine:
//     - Manages simulation lifecycle (start/stop/pause)
//     - Calls physics modules each timestep in correct order
//     - Reads results from modules into public state fields
//     - Exposes state for GUI/dashboard consumption
//     - Handles time acceleration and frame-rate decoupling
//
// PHYSICS MODULES USED:
//   - [ModuleName]   : [what it provides]
//   - [ModuleName]   : [what it provides]
//
// GUI COMPANION: [VisualFile.cs] (reads public state, renders dashboard)
//
// GOLD STANDARD: [Yes/No]
// ============================================================================

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Critical.Physics;

/// <summary>
/// Simulation engine for [description].
/// Coordinates physics modules. Exposes state for dashboard.
/// No inline physics calculations (G3).
/// </summary>
public partial class [EngineName] : MonoBehaviour
{
    // ========================================================================
    // INSPECTOR SETTINGS
    // ========================================================================

    [Header("Simulation Settings")]
    public bool runOnStart = true;

    [Header("Initial Conditions")]
    public float startParameter = 100f;

    // ========================================================================
    // PUBLIC STATE — Read by companion visual/dashboard (G4)
    // All [HideInInspector] fields stay in this file for Unity serialization.
    // ========================================================================

    [HideInInspector] public float primaryValue;
    [HideInInspector] public float secondaryValue;
    [HideInInspector] public bool isRunning;

    // ========================================================================
    // PRIVATE STATE
    // ========================================================================

    private [Module]State moduleState;

    // ========================================================================
    // LIFECYCLE
    // ========================================================================

    void Start()
    {
        if (runOnStart) StartSimulation();
    }

    public void StartSimulation()
    {
        if (isRunning) return;
        StartCoroutine(RunSimulation());
    }

    public void StopSimulation()
    {
        isRunning = false;
    }

    // ========================================================================
    // MAIN LOOP — Delegates to physics modules (G3)
    // ========================================================================

    IEnumerator RunSimulation()
    {
        isRunning = true;
        InitializeSimulation();  // See .Init.cs partial

        float dt = 1f / 360f;  // 10-second steps in hours

        while (isRunning)
        {
            StepSimulation(dt);
            yield return null;
        }
    }

    void StepSimulation(float dt)
    {
        // 1. Update physics modules (G3 — delegate, don't calculate)
        Module.Update(ref moduleState, dt);

        // 2. Read results into public state
        primaryValue = moduleState.PrimaryValue;

        // 3. Update alarms (see .Alarms.cs partial)
        UpdateAnnunciators();

        // 4. Update history (see .Logging.cs partial)
        // ...
    }
}
```

**Partial class files** for large engines follow the naming convention:
- `[EngineName].cs` — Core state, lifecycle, main loop
- `[EngineName].Init.cs` — Initialization methods
- `[EngineName].[Subsystem].cs` — Specific subsystem coordination
- `[EngineName].Alarms.cs` — Annunciator/alarm logic
- `[EngineName].Logging.cs` — History buffers, event log, file output

---

### 2.3 UI Module (MonoBehaviour)

Used for: Dashboard rendering, gauge display, control panels.

Examples: `MosaicGauge.cs`, `MosaicBoard.cs`, `HeatupValidationVisual.cs`

```csharp
// ============================================================================
// CRITICAL: Master the Atom - UI Component
// [FileName].cs - [Short Description]
// ============================================================================
//
// PURPOSE:
//   [What this UI component displays/controls.]
//
// READS FROM:
//   [Which engine/module provides the data this component displays]
//
// REFERENCE:
//   [Westinghouse control room reference, if applicable]
//
// GOLD STANDARD: [Yes/No]
// ============================================================================

using UnityEngine;
using UnityEngine.UI;

namespace Critical.UI
{
    /// <summary>
    /// [Description of UI component].
    /// Reads state from [engine]. Renders [what].
    /// Contains no physics calculations.
    /// </summary>
    public class [ComponentName] : MonoBehaviour
    {
        // Inspector configuration
        // Rendering methods
        // Style/color definitions (or in .Styles.cs partial for large components)
    }
}
```

---

### 2.4 Constants (Static Partial Class)

Used for: Plant parameters, physical constants, setpoints, dimensional data.

Example: `PlantConstants.cs` and its partial files.

```csharp
// ============================================================================
// CRITICAL: Master the Atom - Plant Constants
// PlantConstants.[Domain].cs - [Domain] Constants
// ============================================================================
//
// DOMAIN: [What area these constants cover]
//
// SOURCES:
//   - [Primary NRC/Westinghouse reference]
//   - [Secondary reference if applicable]
//
// UNITS: [Unit convention for this domain]
//
// NOTE: This is a partial class. All constants are accessed as
//       PlantConstants.CONSTANT_NAME regardless of which file they live in.
// ============================================================================

namespace Critical.Physics
{
    public static partial class PlantConstants
    {
        #region [Domain Name]

        /// <summary>
        /// [Description]. Source: [NRC HRTD X.X / WCAP-XXXXX]
        /// </summary>
        public const float CONSTANT_NAME = 123.4f;

        #endregion
    }
}
```

---

### 2.5 Test Module

Used for: Physics validation, integration testing, regression testing.

Examples: `Phase1TestRunner.cs`, `HeatupIntegrationTests.cs`

```csharp
// ============================================================================
// CRITICAL: Master the Atom - Test Suite
// [FileName].cs - [What is being tested]
// ============================================================================
//
// TESTS:
//   [Summary of what test categories are covered]
//   [Number of tests and what they validate]
//
// VALIDATION AGAINST:
//   [NRC/Westinghouse reference data used for pass/fail criteria]
//
// GOLD STANDARD: [Yes/No]
// ============================================================================

using System;
using Critical.Physics;

namespace Critical.Tests
{
    /// <summary>
    /// [Description]. Inherits shared assertion infrastructure from TestBase.
    /// </summary>
    public class [TestName] : TestBase
    {
        // Test methods organized by #region per module being tested
        // Each test uses inherited AssertRange/AssertTrue/AssertEqual
    }
}
```

---

## 3. Naming Conventions

### 3.1 Files

| Category | Pattern | Example |
|----------|---------|---------|
| Physics module | `[SystemName].cs` | `SolidPlantPressure.cs` |
| Physics partial | `[SystemName].[Aspect].cs` | `PressurizerPhysics.Correlations.cs` |
| Constants | `PlantConstants.[Domain].cs` | `PlantConstants.CVCS.cs` |
| Engine | `[Phase]SimEngine.cs` | `HeatupSimEngine.cs` |
| Engine partial | `[Phase]SimEngine.[Subsystem].cs` | `HeatupSimEngine.BubbleFormation.cs` |
| UI component | `Mosaic[Component].cs` | `MosaicGauge.cs` |
| Visual/dashboard | `[Phase]ValidationVisual.cs` | `HeatupValidationVisual.cs` |
| Visual partial | `[Phase]ValidationVisual.[Layer].cs` | `HeatupValidationVisual.Graphs.cs` |
| Test runner | `Phase[N]TestRunner.cs` | `Phase1TestRunner.cs` |
| Test base | `TestBase.cs` | `TestBase.cs` |

### 3.2 Types

| Type | Convention | Example |
|------|-----------|---------|
| State struct | `[Module]State` | `SolidPlantState` |
| Result struct | `[Module]Result` | `BulkHeatupResult` |
| Input struct | `[Module]Inputs` | `AlarmInputs` |
| Enum | `PascalCase` | `BubbleFormationPhase`, `HeaterMode` |
| Enum values | `UPPER_SNAKE_CASE` | `STARTUP_FULL_POWER`, `AUTOMATIC_PID` |

### 3.3 Constants

| Scope | Convention | Example |
|-------|-----------|---------|
| Public shared | `UPPER_SNAKE_CASE` | `PlantConstants.RCS_WATER_VOLUME` |
| Private module | `UPPER_SNAKE_CASE` | `private const float MAX_ITERATIONS = 50f;` |
| Source citation | In `<summary>` XML doc | `/// <summary>... Source: NRC HRTD 4.1</summary>` |

### 3.4 Methods

| Role | Convention | Example |
|------|-----------|---------|
| Initialize state | `Initialize(...)` | `SolidPlantPressure.Initialize(P, T, ...)` |
| Advance one timestep | `Update(ref state, dt)` | `CVCSController.Update(ref state, ...)` |
| Stateless calculation | `Calculate[What](...)` | `WaterProperties.WaterDensity(T, P)` |
| Status string | `Get[What]String(state)` | `VCTPhysics.GetStatusString(state)` |
| Validation | `Validate[What]()` | `PlantConstants.ValidateConstants()` |

---

## 4. Rules and Constraints

### 4.1 Absolute Rules (Never Violate)

1. **GOLD modules are protected.** Never modify the physics or API of a GOLD-certified module without explicit review and re-certification.
2. **Physics lives in physics modules.** Engines coordinate; they do not calculate.
3. **Constants have sources.** Every physical constant cites its origin document.
4. **No silent renames.** Any rename of a public variable, method, class, or constant must be tracked in the changelog with all affected references updated.
5. **Tests pass after every change.** No commit is valid if any test regresses.

### 4.2 Strong Preferences (Deviate Only With Justification)

1. **Prefer `partial class` over new classes** when splitting a file that is too large. This preserves all existing references.
2. **Prefer result structs over `out` parameters** for methods returning multiple values.
3. **Prefer `ref` state structs over class-level fields** for module state that the engine owns.
4. **Prefer explicit units in variable names** when ambiguity is possible (e.g., `_psia`, `_gpm`, `_degF`).
5. **Prefer `#region` blocks** to organize sections within a file, matching the template structure.

### 4.3 Size Guidelines

| Range | Classification | Action |
|-------|---------------|--------|
| < 20 KB | Ideal | No action needed |
| 20–30 KB | Acceptable | Monitor at next feature addition |
| 30–40 KB | Over target | Plan split at next opportunity |
| > 40 KB | Hard limit exceeded | Must split before next release |

---

## 5. GOLD Certification Checklist

Use this checklist when certifying a new or refactored module. All items must be YES.

```
Module: ___________________
File(s): __________________
Date: ____________________
Certified by: _____________

[ ] G1  — Single responsibility: module does one thing
[ ] G2  — Header block: purpose, physics, sources, units, architecture
[ ] G3  — No inline physics in engines (N/A for physics modules)
[ ] G4  — Result/state structs for inter-module communication
[ ] G5  — Constants sourced from PlantConstants or cited locally
[ ] G6  — All NRC/Westinghouse values cite their source
[ ] G7  — Correct namespace (Critical.Physics / Critical.UI / etc.)
[ ] G8  — File size < 40 KB (target < 30 KB)
[ ] G9  — No dead code, no [Obsolete] without removal plan
[ ] G10 — No duplicated logic or constants

Status: [ ] GOLD  [ ] Near-GOLD (note issues)  [ ] Needs Work
```

---

## 6. Version History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-02-07 | Initial GOLD standard template created |
