# CRITICAL: Master the Atom — Changelog

## [0.3.0] — 2026-02-07

### Overview
GOLD standard refactoring: Phase 1 (HeatupSimEngine decomposition) and Phase 2
(PlantConstants consolidation + PlantConstantsHeatup merge). No physics changes.
No functional changes. All public APIs preserved.

**GOLD STANDARD modules unchanged:** All physics modules listed in v0.1.0 and v0.2.0
remain untouched. No physics calculations were modified in any phase.

---

### Phase 1 — HeatupSimEngine Decomposition (completed)

Split the 97 KB monolith into 6 partial class files, each single-responsibility
and under the 30 KB GOLD target. Zero API changes — all public fields and methods
retain their exact names, types, and signatures.

#### New Partial Files

| File | Responsibility | Size |
|------|---------------|------|
| `HeatupSimEngine.cs` | Core state, lifecycle, inspector fields, `StepSimulation()` dispatch | ~28 KB |
| `HeatupSimEngine.Init.cs` | `InitializeColdShutdown()`, `InitializeWarmStart()`, common init | ~10 KB |
| `HeatupSimEngine.BubbleFormation.cs` | 7-phase bubble formation state machine, CCP/aux spray state | ~23 KB |
| `HeatupSimEngine.CVCS.cs` | CVCS flow updates, heater control, RCS inventory, VCT tracking | ~10 KB |
| `HeatupSimEngine.Alarms.cs` | Table-driven annunciator edge detection, RVLIS update | ~11 KB |
| `HeatupSimEngine.Logging.cs` | Event log, history buffers, interval/report file output | ~15 KB |

#### Alarm Refactor (G10)
Replaced 15 identical `if (new && !prev)` / `if (!new && prev)` edge detection pairs
(~90 lines) with a table-driven alarm descriptor pattern (~25 lines). Each alarm is a
struct with getter delegate, severity, on/off messages. A single loop handles all
edge detection and event logging.

#### GOLD Certification — HeatupSimEngine (6 files)

```
Module: HeatupSimEngine (partial class, 6 files)
Files: HeatupSimEngine.cs, .Init.cs, .BubbleFormation.cs, .CVCS.cs, .Alarms.cs, .Logging.cs
Date: 2026-02-07

[X] G1  — Single responsibility per file
[X] G2  — GOLD-compliant headers on all 6 files
[X] G3  — No inline physics (delegates to Critical.Physics modules)
[X] G4  — Result/state structs for inter-module communication
[X] G5  — Constants from PlantConstants
[X] G6  — All NRC/Westinghouse values cite their source
[X] G7  — Validation namespace (MonoBehaviour, no explicit namespace)
[X] G8  — All files < 30 KB (largest: BubbleFormation at 23 KB)
[X] G9  — No dead code
[X] G10 — Alarm deduplication via table-driven pattern

Status: [X] GOLD
```

---

### Phase 2 — PlantConstants Consolidation

Split PlantConstants.cs (62 KB) into 7 domain-focused partial files. Merged all
unique constants and methods from PlantConstantsHeatup.cs into the appropriate
partials. Deleted PlantConstantsHeatup.cs (separate class, zero external references).

All constants remain accessible as `PlantConstants.CONSTANT_NAME`. Zero API change
for PlantConstants consumers. PlantConstantsHeatup had zero external references
across the entire codebase, so no search-replace was required.

#### New Partial Files

| File | Domain | Size |
|------|--------|------|
| `PlantConstants.cs` | Core: RCS, SGs, natural circ, surge line, insulation, physical constants, unit conversions | 10.3 KB |
| `PlantConstants.CVCS.cs` | CVCS flows, seal injection, CCP, bubble drain flows, VCT, orifice calculations | 16.6 KB |
| `PlantConstants.Pressurizer.cs` | PZR geometry, bubble formation timing/conditions, heater control, aux spray, level program (heatup + at-power) | 19.1 KB |
| `PlantConstants.Pressure.cs` | Pressure setpoints, solid plant control, RCPs, RHR | 8.3 KB |
| `PlantConstants.Nuclear.cs` | Core, reactivity, rods, xenon, decay heat, boron, turbine | 10.4 KB |
| `PlantConstants.Heatup.cs` | Heatup/cooldown rates, NRC modes, thermal mass breakdown, electrical loads, subcooling targets | 10.7 KB |
| `PlantConstants.Validation.cs` | Merged ValidateConstants() covering all domains | 6.8 KB |

#### PlantConstantsHeatup.cs Merge — Disposition

**Duplicates removed** (identical values already in PlantConstants):

| PlantConstantsHeatup Constant | PlantConstants Equivalent |
|-------------------------------|--------------------------|
| `RCP_HEAT_PER_PUMP_MW` (5.25) | `RCP_HEAT_MW_EACH` (5.25) |
| `RCP_HEAT_TOTAL_MW` (21) | `RCP_HEAT_MW` (21) |
| `TYPICAL_HEATUP_RATE_F_HR` (50) | `TYPICAL_HEATUP_RATE_F_HR` (50) |
| `SOLID_PLANT_INITIAL_PRESSURE_PSIA` (365) | `SOLID_PLANT_INITIAL_PRESSURE_PSIA` (365) |
| `MIN_RCP_PRESSURE_PSIA` (334.7) | `MIN_RCP_PRESSURE_PSIA` (334.7) |
| `NORMAL_OPERATING_PRESSURE` (2250) | `P_NORMAL` (2250) |
| `INSULATION_LOSS_MW` (1.5) | `INSULATION_LOSS_HOT_MW` (1.5) |
| `TOTAL_PRIMARY_METAL_MASS_LB` (2200000) | `RCS_METAL_MASS` (2200000) |
| `MODE_6_TEMP_LIMIT` (140) | `MODE_6_TEMP_MAX_F` (140) |
| `MODE_5_TEMP_LIMIT` (200) | `MODE_5_TEMP_MAX_F` (200) |
| `MODE_4_TEMP_LIMIT` (350) | `MODE_4_TEMP_MAX_F` (350) |
| `MODE_3_TAVG_NORMAL` (557) | `T_AVG_NO_LOAD` (557) |

**Unique constants merged** into PlantConstants partials:

| Constant | Destination Partial | Value |
|----------|-------------------|-------|
| `RCP_ELECTRICAL_PER_PUMP_MW` | Heatup | 6 MW |
| `RCP_ELECTRICAL_TOTAL_MW` | Heatup | 24 MW |
| `AUX_ELECTRICAL_LOADS_MW` | Heatup | 15 MW |
| `SUPPORT_SYSTEMS_MW` | Heatup | 10 MW |
| `TOTAL_GRID_LOAD_HEATUP_MW` | Heatup | 51 MW |
| `EDG_CAPACITY_MW` | Heatup | 7 MW |
| `RV_MASS_LB` | Heatup | 800,000 lb |
| `SG_MASS_EACH_LB` | Heatup | 700,000 lb |
| `RCS_PIPING_MASS_LB` | Heatup | 400,000 lb |
| `SG_SECONDARY_METAL_MASS_LB` | Heatup | 200,000 lb |
| `SG_SECONDARY_WATER_MASS_LB` | Heatup | 415,000 lb |
| `MAX_COOLDOWN_RATE_F_HR` | Heatup | 100 °F/hr |
| `MIN_SUBCOOLING_F` | Heatup | 30 °F |
| `TARGET_SUBCOOLING_F` | Heatup | 50 °F |
| `LETDOWN_HEAT_REMOVAL_MW` | Heatup | 2 MW |
| `PZR_LEVEL_COLD_PERCENT` | Pressurizer | 25% |
| `PZR_LEVEL_HOT_PERCENT` | Pressurizer | 60% |
| `PZR_LEVEL_PROGRAM_T_LOW` | Pressurizer (as `PZR_HEATUP_LEVEL_T_LOW`) | 200 °F |
| `PZR_LEVEL_PROGRAM_T_HIGH` | Pressurizer (as `PZR_HEATUP_LEVEL_T_HIGH`) | 557 °F |

**Methods merged**:

| Method | Destination Partial |
|--------|-------------------|
| `GetPZRLevelSetpoint(T_avg)` | Pressurizer |
| `GetPZRLevelSetpointUnified(T_avg)` | Pressurizer |
| `GetTargetPressure(T_avg)` | Heatup |
| `GetTotalHeatCapacity(T_avg, P)` | Heatup |
| `GetPlantMode(T_avg)` (1-param overload) | Heatup |
| `ValidateConstants()` | Validation (merged with main) |

#### Constant Renames

| Old Access Pattern | New Access Pattern | Reason |
|-------------------|-------------------|--------|
| `PlantConstantsHeatup.PZR_LEVEL_PROGRAM_T_LOW` | `PlantConstants.PZR_HEATUP_LEVEL_T_LOW` | Avoid conflict with at-power `PZR_LEVEL_PROGRAM_TAVG_LOW` |
| `PlantConstantsHeatup.PZR_LEVEL_PROGRAM_T_HIGH` | `PlantConstants.PZR_HEATUP_LEVEL_T_HIGH` | Avoid conflict with at-power `PZR_LEVEL_PROGRAM_TAVG_HIGH` |

No external references required updating (PlantConstantsHeatup had zero callers).

#### Deleted Files

| File | Reason |
|------|--------|
| `PlantConstantsHeatup.cs` | All unique content merged into PlantConstants partials. Zero external references. |
| `PlantConstantsHeatup.cs.meta` | Accompanying Unity metadata |

#### GOLD Certification — PlantConstants (7 files)

```
Module: PlantConstants (partial class, 7 files)
Files: PlantConstants.cs, .CVCS.cs, .Pressurizer.cs, .Pressure.cs,
       .Nuclear.cs, .Heatup.cs, .Validation.cs
Date: 2026-02-07

[X] G1  — Single responsibility: each file covers one domain
[X] G2  — GOLD-compliant headers on all 7 files
[X] G3  — N/A (constants file, not an engine)
[X] G4  — N/A (constants file)
[X] G5  — This IS the constants source
[X] G6  — All NRC/Westinghouse values cite their source
[X] G7  — namespace Critical.Physics
[X] G8  — All files < 20 KB (largest: Pressurizer at 19.1 KB)
[X] G9  — No dead code; PlantConstantsHeatup duplicates removed
[X] G10 — Single source of truth; no duplicate constants

Status: [X] GOLD
```

---

### GOLD Standard Module Status

**Phases 1-2 modules — now GOLD:**
- HeatupSimEngine (6 partial files) — GOLD
- PlantConstants (7 partial files) — GOLD

**Previously GOLD — confirmed unchanged:**
SolidPlantPressure.cs, WaterProperties.cs, VCTPhysics.cs, SteamThermodynamics.cs,
ThermalMass.cs, ThermalExpansion.cs, FluidFlow.cs, ReactorKinetics.cs,
PressurizerPhysics.cs, CoupledThermo.cs, HeatTransfer.cs, RCSHeatup.cs,
LoopThermodynamics.cs, TimeAcceleration.cs, RCPSequencer.cs, AlarmManager.cs,
RVLISPhysics.cs, ControlRodBank.cs, ReactorCore.cs, FeedbackCalculator.cs,
PowerCalculator.cs, MosaicBoard.cs, MosaicBoardBuilder.cs, MosaicControlPanel.cs,
MosaicAlarmPanel.cs, MosaicIndicator.cs, MosaicRodDisplay.cs, MosaicGauge.cs,
MosaicBoardSetup.cs, MosaicTypes.cs

---

### Remaining Refactoring Phases (not started)

| Phase | Description | Status |
|-------|-------------|--------|
| Phase 3 | Legacy cleanup (delete HeatupValidation.cs, deprecated methods) | Pending |
| Phase 4 | HeatupValidationVisual decomposition (53 KB → 5 partials) | Pending |
| Phase 5 | Test infrastructure (TestBase extraction, runner refactoring) | Pending |
| Phase 6 | Near-GOLD elevation (split PressurizerPhysics, CVCSController, FuelAssembly) | Pending |
