# CRITICAL: Master the Atom — Refactoring Plan v2

**Date:** 2026-02-07
**Version:** Plan v2 (supersedes v1)
**Scope:** GOLD standard elevation for all modules, separation of concerns, deduplication, legacy cleanup
**Constraint:** Maintain full functionality. All renames tracked with reference updates.

---

## 1. What Is GOLD Standard?

Based on analysis of the existing GOLD modules in this project, GOLD standard means:

### GOLD Standard Criteria

| # | Criterion | Description |
|---|-----------|-------------|
| G1 | **Single Responsibility** | Module does one thing. Physics modules own physics. Coordinators coordinate. GUI renders. |
| G2 | **Documented Header** | File header states: purpose, physics basis, NRC/industry sources, units, and architecture role |
| G3 | **No Inline Physics in Engines** | Simulation engines delegate to physics modules; they never calculate physics inline |
| G4 | **Result Structs** | Physics modules return typed result structs, not modify engine state directly |
| G5 | **Constants from PlantConstants** | Module-specific constants are acceptable only when domain-isolated (e.g., safety system setpoints). Shared constants come from PlantConstants. |
| G6 | **Validated Parameters** | All NRC/Westinghouse-sourced values cite their source document |
| G7 | **Namespace Compliance** | Physics in `Critical.Physics`, Controllers in `Critical.Controllers`, UI in `Critical.UI` |
| G8 | **Manageable Size** | Target: < 30 KB per file. Hard limit: 40 KB. Files above this are split using `partial class`. |
| G9 | **No Dead Code** | No obsolete methods, unused variables, or commented-out blocks |
| G10 | **No Duplication** | Shared logic extracted to common utilities. Constants defined once. |

### Current GOLD Module Pattern (exemplified by SolidPlantPressure.cs, WaterProperties.cs)

```
// Header: Purpose, physics equations, NRC sources, units
namespace Critical.Physics
{
    public struct ModuleState { /* typed fields */ }
    
    public static class Module
    {
        // Constants (module-specific or from PlantConstants)
        // Public API: Initialize(), Update(), Calculate*()
        // Returns result structs
        // No engine state mutation
    }
}
```

---

## 2. Current State — Complete File Audit

### 2.1 File Inventory by GOLD Compliance

**Legend:** ✅ GOLD | ⚠️ Near-GOLD (minor issues) | 🔶 Needs Work | ❌ Non-compliant | 🗑️ Remove

#### Physics/ (21 source files, 442 KB)

| File | Size | GOLD? | Issues |
|------|------|-------|--------|
| PlantConstants.cs | 62 KB | 🔶 | G8: oversized, G10: overlap with PlantConstantsHeatup |
| PlantConstantsHeatup.cs | 13 KB | 🔶 | G10: duplicates values in PlantConstants, separate class |
| PressurizerPhysics.cs | 37 KB | ⚠️ | G8: borderline oversized |
| CVCSController.cs | 34 KB | ⚠️ | G8: borderline (grew with v0.2.0 heater modes) |
| HeatTransfer.cs | 29 KB | ✅ | GOLD |
| CoupledThermo.cs | 28 KB | ✅ | GOLD |
| SolidPlantPressure.cs | 28 KB | ✅ | GOLD |
| WaterProperties.cs | 27 KB | ✅ | GOLD |
| ReactorKinetics.cs | 24 KB | ✅ | GOLD |
| FluidFlow.cs | 19 KB | ✅ | GOLD |
| VCTPhysics.cs | 18 KB | ✅ | GOLD |
| SteamThermodynamics.cs | 17 KB | ✅ | GOLD |
| ThermalExpansion.cs | 16 KB | ✅ | GOLD |
| ThermalMass.cs | 16 KB | ✅ | GOLD |
| RCSHeatup.cs | 15 KB | ✅ | GOLD |
| LoopThermodynamics.cs | 13 KB | ✅ | GOLD |
| TimeAcceleration.cs | 12 KB | ✅ | GOLD |
| RCPSequencer.cs | 11 KB | ✅ | GOLD |
| AlarmManager.cs | 10 KB | ✅ | GOLD |
| RVLISPhysics.cs | 9 KB | ✅ | GOLD |
| Critical.Physics.asmdef | <1 KB | ✅ | — |

#### Reactor/ (7 source files, 183 KB)

| File | Size | GOLD? | Issues |
|------|------|-------|--------|
| FuelAssembly.cs | 32 KB | ⚠️ | G8: borderline |
| ReactorSimEngine.cs | 30 KB | ⚠️ | G8: borderline |
| ReactorController.cs | 30 KB | ⚠️ | G8: borderline |
| ControlRodBank.cs | 29 KB | ✅ | GOLD |
| ReactorCore.cs | 24 KB | ✅ | GOLD |
| FeedbackCalculator.cs | 20 KB | ✅ | GOLD |
| PowerCalculator.cs | 18 KB | ✅ | GOLD |

#### Validation/ (3 source files, 169 KB)

| File | Size | GOLD? | Issues |
|------|------|-------|--------|
| HeatupSimEngine.cs | 97 KB | ❌ | G1: 7+ responsibilities, G3: inline physics, G8: 4× limit, G10: alarm duplication |
| HeatupValidationVisual.cs | 53 KB | 🔶 | G1: GUI+styles+graphs+panels, G8: 2× limit |
| HeatupValidation.cs | 19 KB | 🗑️ | Marked `[Obsolete]` in v0.1.0. Dead code. |

#### Tests/ (6 source files, 157 KB)

| File | Size | GOLD? | Issues |
|------|------|-------|--------|
| Phase1TestRunner.cs | 49 KB | 🔶 | G8: oversized, G10: assertion boilerplate duplicated |
| Phase2TestRunner.cs | 39 KB | 🔶 | G8: oversized, G10: assertion boilerplate duplicated |
| HeatupIntegrationTests.cs | 38 KB | 🔶 | G8: borderline, G10: assertion boilerplate duplicated |
| IntegrationTests.cs | 19 KB | ⚠️ | OK size, G10: shares assertion patterns |
| Phase2UnityTestRunner.cs | 11 KB | ✅ | OK |
| UnityTestRunner.cs | <1 KB | ✅ | OK |

#### UI/ (9 source files, 106 KB)

| File | Size | GOLD? | Issues |
|------|------|-------|--------|
| MosaicBoard.cs | 20 KB | ✅ | GOLD |
| MosaicBoardBuilder.cs | 18 KB | ✅ | GOLD |
| MosaicControlPanel.cs | 15 KB | ✅ | GOLD |
| MosaicAlarmPanel.cs | 12 KB | ✅ | GOLD |
| MosaicIndicator.cs | 11 KB | ✅ | GOLD |
| MosaicRodDisplay.cs | 11 KB | ✅ | GOLD |
| MosaicGauge.cs | 11 KB | ✅ | GOLD |
| MosaicBoardSetup.cs | 7 KB | ✅ | GOLD |
| MosaicTypes.cs | 1 KB | ✅ | GOLD |

### 2.2 Summary

| Category | Files | Total KB | GOLD | Near-GOLD | Needs Work | Non-Compliant | Remove |
|----------|-------|----------|------|-----------|------------|---------------|--------|
| Physics | 21 | 442 | 16 | 2 | 2 | 0 | 0 |
| Reactor | 7 | 183 | 4 | 3 | 0 | 0 | 0 |
| Validation | 3 | 169 | 0 | 0 | 1 | 1 | 1 |
| Tests | 6 | 157 | 2 | 1 | 3 | 0 | 0 |
| UI | 9 | 106 | 9 | 0 | 0 | 0 | 0 |
| **Total** | **46** | **1,057** | **31** | **6** | **6** | **1** | **1** |

**67% of files are already GOLD.** The problem is concentrated in 8 files.

---

## 3. Refactoring Plan

### Phase 1: HeatupSimEngine Decomposition (Priority 1 — Critical)

**Current:** 97 KB monolith with 7+ responsibilities.
**Target:** 6 partial class files, each < 25 KB, each single-responsibility.

All files remain `partial class HeatupSimEngine : MonoBehaviour`. Unity treats them as one class. Zero API changes — all public fields and methods maintain their exact names and types.

#### New File Structure

| File | Contents | Est. Size | GOLD Criteria Met |
|------|----------|-----------|-------------------|
| `HeatupSimEngine.cs` | Class declaration, inspector fields, public state fields (~80 `[HideInInspector]`), `Start()`, `StartSimulation()`, `StopSimulation()`, `RunSimulation()` coroutine skeleton, `StepSimulation()` dispatch | ~18 KB | G1, G2, G7, G8 |
| `HeatupSimEngine.Init.cs` | `InitializeColdShutdown()`, `InitializeWarmStart()`, common init (history, events, time accel) | ~15 KB | G1 |
| `HeatupSimEngine.BubbleFormation.cs` | `BubbleFormationPhase` enum, CCP/aux spray state fields, `ProcessBubbleDetection()`, `UpdateBubbleFormation()` (full state machine) | ~22 KB | G1 |
| `HeatupSimEngine.CVCS.cs` | `UpdateCVCSFlows()`, `UpdateHeaterControl()`, `UpdateRCSInventory()`, `UpdateVCT()` | ~15 KB | G1 |
| `HeatupSimEngine.Alarms.cs` | `prev_*` fields, `UpdateAnnunciators()` (table-driven), `UpdateRVLIS()` | ~8 KB | G1, G10 |
| `HeatupSimEngine.Logging.cs` | `EventSeverity`, `EventLogEntry`, `LogEvent()`, history lists, `AddHistory()`, `SaveIntervalLog()`, `SaveReport()`, helpers | ~14 KB | G1 |

**Alarm edge detection refactor (G10):** Replace 15 identical `if (new && !prev)` / `if (!new && prev)` pairs (~90 lines) with a table-driven pattern (~25 lines). Each alarm is a struct with getter, severity, on/off messages. A single loop handles all edge detection.

**Key rule:** All `[HideInInspector]` serialized fields stay in the main `HeatupSimEngine.cs` file so Unity serialization is unaffected.

#### Reference Updates Required: None

All code stays within the same partial class. No renames. No namespace changes.

---

### Phase 2: PlantConstants Consolidation (Priority 2 — Large)

**Current:** PlantConstants.cs (62 KB) + PlantConstantsHeatup.cs (13 KB) = 75 KB in two separate classes.
**Target:** Single `public static partial class PlantConstants` split across 6 files totalling ~68 KB (after PlantConstantsHeatup merge).

#### New File Structure

| File | Domain | Contents | Est. Size |
|------|--------|----------|-----------|
| `PlantConstants.cs` | Core | RCS geometry (vessel/piping dimensions, volumes), unit conversions, metal mass, general thermal properties, fundamental physical constants | ~12 KB |
| `PlantConstants.CVCS.cs` | CVCS | All CVCS flows, orifice sizes, CCP capacity, seal flows, purification, letdown/charging, VCT parameters, drain constants | ~10 KB |
| `PlantConstants.Pressurizer.cs` | Pressurizer | PZR geometry (volume, height, heater power), bubble formation timings, heater control setpoints, aux spray parameters, level program | ~12 KB |
| `PlantConstants.Pressure.cs` | Pressure Control | Pressure setpoints (normal, solid plant, safety), spray setpoints, trip setpoints, LTOP, relief valves | ~8 KB |
| `PlantConstants.Nuclear.cs` | Nuclear | Boron, reactivity coefficients, rod worth, xenon, power levels, RCP heat, fuel data | ~8 KB |
| `PlantConstants.Heatup.cs` | Heatup-Specific | Heatup rates, temperature targets, RHR limits, mode transition temps — merged content from PlantConstantsHeatup.cs | ~8 KB |
| `PlantConstants.Validation.cs` | Validation | `ValidateConstants()` method and all assertion blocks | ~8 KB |

All constants remain accessible as `PlantConstants.CONSTANT_NAME`. Zero API change.

#### PlantConstantsHeatup.cs Merge — Reference Tracking

`PlantConstantsHeatup` is a separate class (`PlantConstantsHeatup.CONSTANT`), not a partial. Merging it into `PlantConstants` changes the access pattern.

| Constant in PlantConstantsHeatup | Destination | References to Update |
|----------------------------------|-------------|---------------------|
| `PlantConstantsHeatup.RCP_HEAT_PER_PUMP_MW` | `PlantConstants.RCP_HEAT_MW_EACH` (already exists) | Remove duplicate, update any refs |
| `PlantConstantsHeatup.RCP_HEAT_TOTAL_MW` | `PlantConstants.RCP_HEAT_MW` (already exists) | Remove duplicate, update any refs |
| `PlantConstantsHeatup.TYPICAL_HEATUP_RATE_F_HR` | `PlantConstants.Heatup.cs` | All files referencing `PlantConstantsHeatup.TYPICAL_HEATUP_RATE_F_HR` → `PlantConstants.TYPICAL_HEATUP_RATE_F_HR` |
| `PlantConstantsHeatup.MIN_RCP_PRESSURE_PSIA` | `PlantConstants.Pressure.cs` (as `PlantConstants.MIN_RCP_PRESSURE_PSIA` — already may exist) | Update refs |
| `PlantConstantsHeatup.NORMAL_OPERATING_PRESSURE` | `PlantConstants.Pressure.cs` | Update refs |
| `PlantConstantsHeatup.GetPZRLevelSetpointUnified()` | `PlantConstants.Pressurizer.cs` (as `PlantConstants.GetPZRLevelSetpointUnified()`) | Update all callers |
| (all remaining) | Appropriate partial file | Search all `.cs` files for `PlantConstantsHeatup.` and update |

**Process:**
1. Create all partial files with constants moved from PlantConstants.cs
2. Move each PlantConstantsHeatup constant into the appropriate partial file under `PlantConstants`
3. Global search-replace: `PlantConstantsHeatup.` → `PlantConstants.` in all `.cs` files
4. Verify no compile errors
5. Delete `PlantConstantsHeatup.cs` and its `.meta`
6. Run `ValidateConstants()` to confirm

**Files that reference `PlantConstantsHeatup` (to be updated):**
- HeatupSimEngine.cs (likely)
- Phase1TestRunner.cs
- Phase2TestRunner.cs
- HeatupIntegrationTests.cs
- HeatupValidation.cs (obsolete — being removed anyway)
- Possibly: RCSHeatup.cs, CVCSController.cs

---

### Phase 3: Legacy Cleanup (Priority 3 — Quick Win)

#### 3.1 Remove HeatupValidation.cs

**Status:** Marked `[System.Obsolete]` since v0.1.0. Contains inline physics inconsistent with GOLD modules. 19 KB of dead code.

**Action:** Delete `HeatupValidation.cs` and `HeatupValidation.cs.meta`.

**Reference check:** Grep all `.cs` files for `HeatupValidation`. If any references exist beyond the obsolete attribute, they must be removed.

#### 3.2 Remove Deprecated Methods

Audit all files for `[Obsolete]` methods. Candidates:
- `HeatTransfer.SurgeLineHTC()` — deprecated in v0.1.0, replaced by stratified model. If no callers remain, remove.

---

### Phase 4: HeatupValidationVisual Decomposition (Priority 4 — Large)

**Current:** 53 KB single file with GUI layout, graph drawing, gauge rendering, panel drawing, and style definitions.
**Target:** 5 partial class files, each < 15 KB.

| File | Contents | Est. Size |
|------|----------|-----------|
| `HeatupValidationVisual.cs` | Core `OnGUI()`, layout manager, engine reference, main dispatch | ~10 KB |
| `HeatupValidationVisual.Gauges.cs` | Arc gauge renderer, gauge math, gauge color logic | ~10 KB |
| `HeatupValidationVisual.Graphs.cs` | Trend graph drawing, axis autoscaling, graph labels | ~12 KB |
| `HeatupValidationVisual.Panels.cs` | Annunciator tiles, RCP status, CVCS/VCT strips, phase display | ~12 KB |
| `HeatupValidationVisual.Styles.cs` | All `GUIStyle` definitions, color constants, layout metrics | ~6 KB |

#### Reference Updates Required: None

Partial class. No renames.

---

### Phase 5: Test Infrastructure (Priority 5 — Moderate)

#### 5.1 Extract TestBase

Create `TestBase.cs` (~5 KB) with shared test infrastructure:

```csharp
namespace Critical.Tests
{
    public abstract class TestBase
    {
        protected int _totalTests, _passedTests, _failedTests;
        
        protected void AssertRange(string name, float value, float min, float max) { ... }
        protected void AssertTrue(string name, bool condition) { ... }
        protected void AssertEqual(string name, float actual, float expected, float tolerance) { ... }
        protected void Log(string message) { ... }
        protected void LogResult(string testId, string name, bool passed) { ... }
        protected void RunSection(string name, Action tests) { ... }
    }
}
```

#### 5.2 Refactor Test Runners

Both `Phase1TestRunner` and `Phase2TestRunner` extend `TestBase`, eliminating ~200 lines of duplicated assertion/logging infrastructure from each.

- `Phase1TestRunner.cs`: 49 KB → ~38 KB (eliminate ~180 lines of boilerplate)
- `Phase2TestRunner.cs`: 39 KB → ~30 KB (eliminate ~150 lines of boilerplate)
- `HeatupIntegrationTests.cs`: 38 KB → ~32 KB (eliminate ~100 lines of boilerplate)

**Reference Updates Required:** None — internal inheritance only, test method names and signatures unchanged.

---

### Phase 6: Near-GOLD Elevation (Priority 6 — Polish)

These files are 28–37 KB. They meet all GOLD criteria except G8 (size). They're under the 40 KB hard limit so splitting is optional but recommended for the largest ones.

| File | Size | Action | Approach |
|------|------|--------|----------|
| PressurizerPhysics.cs | 37 KB | **Split** | Partial class: `PressurizerPhysics.cs` (state, API, core solver) + `PressurizerPhysics.Correlations.cs` (property lookups, level/volume conversions) |
| CVCSController.cs | 34 KB | **Split** | Partial class: `CVCSController.cs` (PI controller, letdown path, seals) + `CVCSController.HeaterControl.cs` (multi-mode heater controller) |
| FuelAssembly.cs | 32 KB | **Split** | Partial class: `FuelAssembly.cs` (state, API, update) + `FuelAssembly.RadialProfile.cs` (radial temperature distribution calculations) |
| ReactorSimEngine.cs | 30 KB | **Monitor** | Under hard limit. Review at next feature addition. |
| ReactorController.cs | 30 KB | **Monitor** | Under hard limit. Review at next feature addition. |
| ControlRodBank.cs | 29 KB | **OK** | Under hard limit. Single responsibility (rod mechanics). |

#### Reference Updates Required: None

All partial class splits. No renames.

---

## 4. Rename Tracking Register

All renames are tracked here with their reference update requirements.

### 4.1 Class Renames

| Old Name | New Name | Reason | Files Requiring Update |
|----------|----------|--------|----------------------|
| `PlantConstantsHeatup` (class) | Merged into `PlantConstants` (partial) | G10: eliminate dual-class constants | All files containing `PlantConstantsHeatup.` — full list in Phase 2 |

### 4.2 File Renames

| Old Path | New Path | Reason |
|----------|----------|--------|
| `PlantConstantsHeatup.cs` | **DELETED** (contents merged into PlantConstants partials) | G10: single source of truth |
| `HeatupValidation.cs` | **DELETED** (dead code, marked obsolete) | G9: no dead code |

### 4.3 Constant Renames

| Old Access Pattern | New Access Pattern | Files to Update |
|-------------------|-------------------|-----------------|
| `PlantConstantsHeatup.RCP_HEAT_PER_PUMP_MW` | `PlantConstants.RCP_HEAT_MW_EACH` | Already exists, remove duplicate |
| `PlantConstantsHeatup.RCP_HEAT_TOTAL_MW` | `PlantConstants.RCP_HEAT_MW` | Already exists, remove duplicate |
| `PlantConstantsHeatup.TYPICAL_HEATUP_RATE_F_HR` | `PlantConstants.TYPICAL_HEATUP_RATE_F_HR` | grep + replace |
| `PlantConstantsHeatup.MIN_RCP_PRESSURE_PSIA` | `PlantConstants.MIN_RCP_PRESSURE_PSIA` | Check if duplicate, merge |
| `PlantConstantsHeatup.NORMAL_OPERATING_PRESSURE` | `PlantConstants.NORMAL_OPERATING_PRESSURE` | grep + replace |
| `PlantConstantsHeatup.GetPZRLevelSetpointUnified()` | `PlantConstants.GetPZRLevelSetpointUnified()` | grep + replace |
| (All remaining `PlantConstantsHeatup.*`) | `PlantConstants.*` | Global search-replace |

### 4.4 No Other Renames

All other refactoring uses `partial class` which requires zero renames. Existing public APIs (variable names, method signatures, field names) remain identical throughout.

---

## 5. Implementation Sequence

Each step is independently compilable and testable. Compile after every step. Run full test suite after each phase.

| Step | Phase | Target | Action | Risk | Verification |
|------|-------|--------|--------|------|-------------|
| 1.1 | 1 | HeatupSimEngine | Extract `.Logging.cs` partial | Lowest | Logs write, history populates |
| 1.2 | 1 | HeatupSimEngine | Extract `.Alarms.cs` partial + table-driven refactor | Low | All annunciators fire correctly |
| 1.3 | 1 | HeatupSimEngine | Extract `.Init.cs` partial | Low | Cold/warm start both run |
| 1.4 | 1 | HeatupSimEngine | Extract `.CVCS.cs` partial | Medium | CVCS flows, VCT, mass conservation |
| 1.5 | 1 | HeatupSimEngine | Extract `.BubbleFormation.cs` partial | Medium | Full bubble formation sequence |
| — | — | — | **Full test suite run** | — | 251+ tests pass |
| 2.1 | 2 | PlantConstants | Create partial files from PlantConstants.cs | Low | `ValidateConstants()` passes |
| 2.2 | 2 | PlantConstants | Merge PlantConstantsHeatup into partials | Medium | Global search-replace, compile, test |
| 2.3 | 2 | PlantConstants | Delete PlantConstantsHeatup.cs + .meta | Low | Clean compile |
| — | — | — | **Full test suite run** | — | 251+ tests pass |
| 3.1 | 3 | Legacy | Delete HeatupValidation.cs + .meta | Lowest | Confirm no references remain |
| 3.2 | 3 | Legacy | Audit/remove deprecated methods | Low | Compile clean |
| — | — | — | **Full test suite run** | — | 251+ tests pass |
| 4.1 | 4 | HeatupValidationVisual | Extract `.Styles.cs` partial | Lowest | Dashboard renders |
| 4.2 | 4 | HeatupValidationVisual | Extract `.Gauges.cs` partial | Low | Gauges render |
| 4.3 | 4 | HeatupValidationVisual | Extract `.Graphs.cs` partial | Low | Graphs render |
| 4.4 | 4 | HeatupValidationVisual | Extract `.Panels.cs` partial | Low | Panels render |
| — | — | — | **Full test suite run** | — | 251+ tests pass |
| 5.1 | 5 | Tests | Create TestBase.cs | Low | Exists, compiles |
| 5.2 | 5 | Tests | Refactor Phase1TestRunner to use TestBase | Medium | All 156 Phase 1 tests pass |
| 5.3 | 5 | Tests | Refactor Phase2TestRunner to use TestBase | Medium | All 95 Phase 2 tests pass |
| 5.4 | 5 | Tests | Refactor HeatupIntegrationTests to use TestBase | Medium | All integration tests pass |
| — | — | — | **Full test suite run** | — | 251+ tests pass |
| 6.1 | 6 | PressurizerPhysics | Split into 2 partial files | Low | Physics unchanged |
| 6.2 | 6 | CVCSController | Split into 2 partial files | Low | Physics unchanged |
| 6.3 | 6 | FuelAssembly | Split into 2 partial files | Low | Physics unchanged |
| — | — | — | **Final full test suite run** | — | 251+ tests pass |

---

## 6. Final File Structure

```
Assets/Scripts/
├── Physics/                           (27 source files, was 21)
│   ├── PlantConstants.cs              (core — ~12 KB, was 62 KB)
│   ├── PlantConstants.CVCS.cs         (new partial — ~10 KB)
│   ├── PlantConstants.Pressurizer.cs  (new partial — ~12 KB)
│   ├── PlantConstants.Pressure.cs     (new partial — ~8 KB)
│   ├── PlantConstants.Nuclear.cs      (new partial — ~8 KB)
│   ├── PlantConstants.Heatup.cs       (new partial, merged from PlantConstantsHeatup — ~8 KB)
│   ├── PlantConstants.Validation.cs   (new partial — ~8 KB)
│   ├── PlantConstantsHeatup.cs        *** DELETED ***
│   ├── PressurizerPhysics.cs          (trimmed — ~22 KB)
│   ├── PressurizerPhysics.Correlations.cs (new partial — ~15 KB)
│   ├── CVCSController.cs              (trimmed — ~20 KB)
│   ├── CVCSController.HeaterControl.cs (new partial — ~14 KB)
│   ├── WaterProperties.cs             (unchanged — GOLD)
│   ├── CoupledThermo.cs               (unchanged — GOLD)
│   ├── SolidPlantPressure.cs          (unchanged — GOLD)
│   ├── HeatTransfer.cs                (unchanged — GOLD)
│   ├── ReactorKinetics.cs             (unchanged — GOLD)
│   ├── FluidFlow.cs                   (unchanged — GOLD)
│   ├── VCTPhysics.cs                  (unchanged — GOLD)
│   ├── SteamThermodynamics.cs         (unchanged — GOLD)
│   ├── ThermalExpansion.cs            (unchanged — GOLD)
│   ├── ThermalMass.cs                 (unchanged — GOLD)
│   ├── RCSHeatup.cs                   (unchanged — GOLD)
│   ├── LoopThermodynamics.cs          (unchanged — GOLD)
│   ├── TimeAcceleration.cs            (unchanged — GOLD)
│   ├── RCPSequencer.cs                (unchanged — GOLD)
│   ├── AlarmManager.cs                (unchanged — GOLD)
│   ├── RVLISPhysics.cs                (unchanged — GOLD)
│   └── Critical.Physics.asmdef
│
├── Validation/                        (11 source files, was 3)
│   ├── HeatupSimEngine.cs             (trimmed core — ~18 KB, was 97 KB)
│   ├── HeatupSimEngine.Init.cs        (new partial — ~15 KB)
│   ├── HeatupSimEngine.BubbleFormation.cs (new partial — ~22 KB)
│   ├── HeatupSimEngine.CVCS.cs        (new partial — ~15 KB)
│   ├── HeatupSimEngine.Alarms.cs      (new partial — ~8 KB)
│   ├── HeatupSimEngine.Logging.cs     (new partial — ~14 KB)
│   ├── HeatupValidationVisual.cs      (trimmed core — ~10 KB, was 53 KB)
│   ├── HeatupValidationVisual.Gauges.cs (new partial — ~10 KB)
│   ├── HeatupValidationVisual.Graphs.cs (new partial — ~12 KB)
│   ├── HeatupValidationVisual.Panels.cs (new partial — ~12 KB)
│   ├── HeatupValidationVisual.Styles.cs (new partial — ~6 KB)
│   └── HeatupValidation.cs            *** DELETED ***
│
├── Tests/                             (7 source files, was 6)
│   ├── TestBase.cs                    (new — ~5 KB)
│   ├── Phase1TestRunner.cs            (refactored — ~38 KB, was 49 KB)
│   ├── Phase2TestRunner.cs            (refactored — ~30 KB, was 39 KB)
│   ├── HeatupIntegrationTests.cs      (refactored — ~32 KB, was 38 KB)
│   ├── IntegrationTests.cs            (unchanged — ~19 KB)
│   ├── Phase2UnityTestRunner.cs       (unchanged — ~11 KB)
│   └── UnityTestRunner.cs             (unchanged — <1 KB)
│
├── Reactor/                           (8 source files, was 7)
│   ├── FuelAssembly.cs                (trimmed — ~20 KB, was 32 KB)
│   ├── FuelAssembly.RadialProfile.cs  (new partial — ~12 KB)
│   ├── ReactorSimEngine.cs            (unchanged — 30 KB, monitored)
│   ├── ReactorController.cs           (unchanged — 30 KB, monitored)
│   ├── ControlRodBank.cs              (unchanged — GOLD)
│   ├── ReactorCore.cs                 (unchanged — GOLD)
│   ├── FeedbackCalculator.cs          (unchanged — GOLD)
│   └── PowerCalculator.cs             (unchanged — GOLD)
│
├── UI/                                (unchanged — all GOLD)
│   ├── MosaicBoard.cs
│   ├── MosaicBoardBuilder.cs
│   ├── MosaicControlPanel.cs
│   ├── MosaicAlarmPanel.cs
│   ├── MosaicIndicator.cs
│   ├── MosaicRodDisplay.cs
│   ├── MosaicGauge.cs
│   ├── MosaicBoardSetup.cs
│   └── MosaicTypes.cs
```

---

## 7. Expected Outcome

### Size Metrics

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| Files > 50 KB | 3 | 0 | -3 |
| Files > 40 KB (hard limit) | 5 | 0 | -5 |
| Files > 30 KB | 10 | 4 | -6 |
| Largest file | 97 KB | ~38 KB | -61% |
| Total source files | 42 | 55 | +13 (splits) |
| Total source KB | ~1,057 | ~1,000 | -5% (dedup + dead code) |
| Deleted files | 0 | 2 | PlantConstantsHeatup, HeatupValidation |

### GOLD Standard Compliance

| Category | Before | After |
|----------|--------|-------|
| ✅ GOLD | 31 / 42 (74%) | 53 / 55 (96%) |
| ⚠️ Near-GOLD | 6 / 42 (14%) | 2 / 55 (4%) — ReactorSimEngine, ReactorController (monitored) |
| 🔶 Needs Work | 6 / 42 (14%) | 0 / 55 (0%) |
| ❌ Non-compliant | 1 / 42 (2%) | 0 / 55 (0%) |

### Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Partial class compilation error | Low | Low | Each step compiles independently |
| Unity serialization loss | Very Low | Medium | All `[HideInInspector]` fields stay in main file |
| PlantConstantsHeatup merge breaks refs | Medium | Low | Global search-replace, then compile |
| Test regression | Low | Medium | Full suite after each phase |
| GOLD module accidentally modified | Very Low | High | Phases explicitly list which files are touched |

---

## 8. GOLD Standard Modules — Protected List

These files are NOT modified in any phase of this refactoring:

```
Physics/WaterProperties.cs          Physics/SteamThermodynamics.cs
Physics/CoupledThermo.cs            Physics/ThermalMass.cs
Physics/ThermalExpansion.cs          Physics/FluidFlow.cs
Physics/ReactorKinetics.cs          Physics/SolidPlantPressure.cs
Physics/HeatTransfer.cs             Physics/RCSHeatup.cs
Physics/VCTPhysics.cs               Physics/LoopThermodynamics.cs
Physics/TimeAcceleration.cs         Physics/RCPSequencer.cs
Physics/AlarmManager.cs             Physics/RVLISPhysics.cs
Reactor/ReactorCore.cs              Reactor/ControlRodBank.cs
Reactor/FeedbackCalculator.cs       Reactor/PowerCalculator.cs
UI/MosaicBoard.cs                   UI/MosaicBoardBuilder.cs
UI/MosaicControlPanel.cs            UI/MosaicAlarmPanel.cs
UI/MosaicIndicator.cs               UI/MosaicRodDisplay.cs
UI/MosaicGauge.cs                   UI/MosaicBoardSetup.cs
UI/MosaicTypes.cs
```

Files that are SPLIT (partial class) but whose PHYSICS ARE UNCHANGED:
```
Physics/PressurizerPhysics.cs       → split, no physics change
Physics/CVCSController.cs           → split, no physics change
Reactor/FuelAssembly.cs             → split, no physics change
```

Files that are REFACTORED (internal restructuring):
```
Validation/HeatupSimEngine.cs       → split into 6 partials
Validation/HeatupValidationVisual.cs → split into 5 partials
Physics/PlantConstants.cs            → split into 7 partials
Tests/Phase1TestRunner.cs            → inherit TestBase
Tests/Phase2TestRunner.cs            → inherit TestBase
Tests/HeatupIntegrationTests.cs      → inherit TestBase
```

Files that are DELETED:
```
Physics/PlantConstantsHeatup.cs      → merged into PlantConstants
Validation/HeatupValidation.cs       → dead code (obsolete since v0.1.0)
```
