# CRITICAL: Master the Atom — Changelog

## [0.4.0] — 2026-02-07

### Overview
Physics corrections to pressurizer heater feedback, PZR level setpoint function, and
RCP startup model. Implements all three issues identified in IMPL_PLAN_v0.4.0 from
heatup log analysis (25 log files, T0.5hr–T12.5hr). Common theme: simulation control
systems were disconnected from physics — heater throttling was cosmetic, the wrong
level program was called, and RCPs appeared at full capacity instantly.

**Version type:** Minor (backwards-compatible physics corrections)
**Implementation plan:** `Updates and Changelog/IMPL_PLAN_v0.4.0.md`

---

### Issue #1 — Heater Throttle Output Now Reaches Physics (CRITICAL)

**Bug:** The heater controller correctly calculated throttled power (0.36 MW / 20%)
during bubble formation DRAIN phase, but due to execution ordering in
`StepSimulation()`, the throttled value was written *after* physics had already
consumed the full 1.8 MW. Every timestep reset `pzrHeaterPower = PZR_HEATER_POWER_MW`
before physics ran, then the controller overwrote it afterwards — for logging only.
The 1.44 MW difference between displayed power and actual physics input was the
"extra energy" causing T_pzr rates of 74–104 °F/hr and pressure rates of 636 psi/hr
during DRAIN.

**Fix:** Moved heater control calculation from Section 5 (post-physics, inside
`UpdateCVCSFlows()`) to new **Section 1B** (pre-physics) in `StepSimulation()`.
Removed the unconditional `pzrHeaterPower = PZR_HEATER_POWER_MW` reset. The
controller now uses `pressureRate` from the previous timestep (one-step lag is
physically reasonable — real Westinghouse instrumentation has 1–3 second sensor and
signal processing delay per NRC HRTD 6.1).

**Files modified:**

| File | Change |
|------|--------|
| `HeatupSimEngine.cs` | Added Section 1B: HEATER CONTROL before Section 2: PHYSICS. Removed hard reset of `pzrHeaterPower`. |
| `HeatupSimEngine.CVCS.cs` | Removed `CalculateHeaterState()` call from `UpdateCVCSFlows()`. Retained `pzrHeatersOn` read from already-computed state. |

**Expected improvement:**

| Metric | Before | After (expected) |
|--------|--------|------------------|
| T_pzr rate during DRAIN | 74–104 °F/hr | 15–25 °F/hr |
| Pressure rate during DRAIN | 636 psi/hr | 50–100 psi/hr |
| DRAIN phase duration | ~30 min | ~40–60 min |

---

### Issue #2 — Correct PZR Level Setpoint Function During Heatup (CRITICAL)

**Bug:** Four call sites used `GetPZRLevelProgram()` (at-power program, 557–584.7 °F
range) instead of `GetPZRLevelSetpointUnified()` (full range, 200–584.7 °F). Below
557 °F — the *entire heatup* — the at-power function clamps to its minimum of 25%,
making the heatup level program (NRC HRTD 4.1) completely invisible to the CVCS
controller. At T_avg = 288 °F the correct setpoint is 33.6%, not 25%. The +15.2%
level error caused the PI controller to maximise letdown and minimise charging,
flooding the VCT to 84% and activating the DIVERT valve (LCV-112A) continuously.

**Fix:** Four single-line substitutions replacing `GetPZRLevelProgram(T_avg)` with
`GetPZRLevelSetpointUnified(T_avg)`. The at-power function remains available for
future power operations simulation.

**Call sites updated:**

| # | File | Method |
|---|------|--------|
| 1 | `HeatupSimEngine.CVCS.cs` | `UpdateCVCSFlows()` — main level setpoint calculation |
| 2 | `HeatupSimEngine.BubbleFormation.cs` | `UpdateDrainPhase()` — STABILIZE phase initialiser |
| 3 | `CVCSController.cs` | `Update()` — PI controller level setpoint input |
| 4 | `CVCSController.cs` | `PreSeedForRCPStart()` — pre-seed level target |

**Setpoint correction across heatup range:**

| T_avg (°F) | Before (clamped) | After (correct) | Source |
|------------|-----------------|-----------------|--------|
| 200 | 25% | 25% | Heatup curve start (NRC HRTD 4.1) |
| 288 | 25% | 33.6% | Heatup curve interpolation |
| 400 | 25% | 44.3% | Heatup curve interpolation |
| 557 | 25% | 60% | Heatup curve end / at-power handoff |
| 570 | 42.1% | 42.1% | At-power curve (both functions agree) |

**Expected improvement:**

| Metric | Before | After (expected) |
|--------|--------|------------------|
| PZR level error at 288 °F | +15.2% | +6.6% |
| VCT level during heatup | 84% (DIVERT active) | 30–70% (normal band) |
| Charging/letdown balance | 75 gpm imbalance | ±10 gpm balanced |

---

### Issue #3 — Staged RCP Startup Ramp-Up Model (MODERATE)

**Bug:** RCPs were modelled as binary on/off devices. The moment `rcpCount` changed
from 0 to 1, the engine instantly switched from isolated physics (separate T_pzr and
T_rcs) to fully coupled physics (`CoupledThermo.SolveEquilibrium` with 100% flow).
With ΔT ≈ 362 °F across the surge line, this step change caused a 17% PZR level
crash (to 7.9%, near the 17% low-level isolation interlock), a 311 psi pressure
spike, and 30+ minute recovery — approximately double the expected magnitude for a
real Westinghouse 4-Loop plant.

**Fix:** Three-part implementation replacing binary RCP model with staged ramp-up:

#### Part A — Per-Pump Ramp State Model (RCPSequencer.cs + PlantConstants.Pressure.cs)

New `RCPStartupState` struct tracks each pump through 5 stages (0–4) with linearly
interpolated flow and heat fractions. New `RCPContribution` struct aggregates all
pumps. New methods `UpdatePumpRampState()` and `GetEffectiveRCPContribution()`
compute effective heat/flow across all ramping pumps.

**Staging table (per pump, ~40 min total):**

| Stage | Duration | Speed | Flow Fraction | Heat Fraction | Description |
|-------|----------|-------|---------------|---------------|-------------|
| 0 — Pre-Start | — | 0% | 0% | 0% | Breaker open |
| 1 — Initial | 2 min | 0→30% | 0→10% | 0→5% | Motor energised, shaft accelerating |
| 2 — Low Flow | 7.5 min | 30→60% | 10→30% | 5→20% | Flow patterns establishing |
| 3 — Moderate | 12.5 min | 60→90% | 30→70% | 20→60% | Substantial thermal mixing |
| 4 — Full Speed | 17.5 min | 100% | 70→100% | 60→100% | Rated conditions |

**15 new constants added to `PlantConstants.Pressure.cs`:**
- 4 stage durations (`RCP_STAGE_1_DURATION_HR` through `RCP_STAGE_4_DURATION_HR`)
- 1 total ramp duration (`RCP_TOTAL_RAMP_DURATION_HR` = ~0.66 hr / ~40 min)
- 4 flow fractions (`RCP_STAGE_1_FLOW_FRACTION` through `RCP_STAGE_4_FLOW_FRACTION`)
- 4 heat fractions (`RCP_STAGE_1_HEAT_FRACTION` through `RCP_STAGE_4_HEAT_FRACTION`)
- Flow and heat fractions differ because at low speed, pump mechanical energy goes
  primarily into shaft friction and seal heating rather than bulk coolant heating.

#### Part B — Three-Regime Blended Physics (HeatupSimEngine.cs)

Replaced the binary `if (rcpCount == 0) / else` physics path with three regimes
using coupling factor α = min(1.0, totalFlowFraction):

| Regime | Condition | Physics Path |
|--------|-----------|--------------|
| 1 — No RCPs | α < 0.001 | Isolated: separate T_pzr/T_rcs, no surge line coupling |
| 2 — Ramping | 0 < α < 1 | Blended: runs BOTH isolated and coupled paths, weights by α |
| 3 — Fully Running | α = 1.0, all pumps complete | Coupled: full `CoupledThermo.SolveEquilibrium` |

Regime 2 blending: `T_rcs = T_isolated × (1-α) + T_coupled × α` (same pattern for
pressure, surge flow, PZR temperature, and PZR water volume). This smoothly
transitions from isolated to coupled over ~40 minutes per pump, preventing the
convergence discontinuity that caused the 17% level crash.

New state fields in `HeatupSimEngine.cs`: `rcpStartTimes[4]`, `rcpContribution`
(RCPContribution struct), `effectiveRCPHeat` (float).

#### Part C — Scaled PI Pre-Seed (CVCSController.cs)

Pre-seed charging bias now scales with the temperature differential between PZR and
RCS: `preSeedCharging_gpm = 5 + 10 × (T_pzr − T_rcs) / 400`. At typical first RCP
start (ΔT ≈ 360 °F) this gives ~14 gpm; at subsequent starts (ΔT smaller due to
mixing already underway) the pre-seed is proportionally less aggressive.

`PreSeedForRCPStart()` is now called at **each new pump start event** (inside the
`targetRcpCount > rcpCount` block in Section 1 of `StepSimulation()`), not only once
at bubble formation COMPLETE. Each pump start changes the thermal transient.

**Expected improvement (all three issues combined):**

| Metric | Before (all bugs) | After (all fixed) |
|--------|-------------------|-------------------|
| PZR level drop at RCP #1 start | 17% in 30 min | < 8% over 30 min |
| Pressure step at RCP #1 start | 311 psi instant | < 100 psi over 15 min |
| Time to level recovery | 30+ min | ~15 min |
| Approach to 17% isolation interlock | 7.9% (near trip) | > 20% (safe margin) |
| RCP heat at T+5 min | 5.25 MW (full) | ~1.0 MW (Stage 2) |
| RCP heat at T+20 min | 5.25 MW (full) | ~3.7 MW (Stage 3) |
| RCP heat at T+40 min | 5.25 MW (full) | 5.25 MW (Stage 4) |

---

### Files Modified Summary

| File | Issue #1 | Issue #2 | Issue #3 | Size |
|------|----------|----------|----------|------|
| `HeatupSimEngine.cs` | ✅ Section 1B heater control | — | ✅ Three-regime blended physics, ramp state fields | 35.2 KB |
| `HeatupSimEngine.CVCS.cs` | ✅ Removed heater calc | ✅ Level function (×1) | — | 10.1 KB |
| `HeatupSimEngine.BubbleFormation.cs` | — | ✅ Level function (×1) | — | 23.6 KB |
| `CVCSController.cs` | — | ✅ Level function (×2) | ✅ Scaled pre-seed, per-pump call | 34.9 KB |
| `RCPSequencer.cs` | — | — | ✅ RCPStartupState, RCPContribution, staged ramp methods | 20.1 KB |
| `PlantConstants.Pressure.cs` | — | — | ✅ 15 new RCP staging constants | 11.6 KB |

**No files created or deleted.** All changes are modifications to existing files.

---

### New Public API Additions (Issue #3)

| Type | Name | Location |
|------|------|----------|
| struct | `RCPSequencer.RCPStartupState` | RCPSequencer.cs |
| struct | `RCPSequencer.RCPContribution` | RCPSequencer.cs |
| method | `RCPSequencer.UpdatePumpRampState(int, float, float)` | RCPSequencer.cs |
| method | `RCPSequencer.GetEffectiveRCPContribution(float[], float, int)` | RCPSequencer.cs |
| const | `PlantConstants.RCP_STAGE_{1-4}_DURATION_HR` | PlantConstants.Pressure.cs |
| const | `PlantConstants.RCP_TOTAL_RAMP_DURATION_HR` | PlantConstants.Pressure.cs |
| const | `PlantConstants.RCP_STAGE_{1-4}_FLOW_FRACTION` | PlantConstants.Pressure.cs |
| const | `PlantConstants.RCP_STAGE_{1-4}_HEAT_FRACTION` | PlantConstants.Pressure.cs |

**No existing public APIs changed or removed.** Issues #1 and #2 are purely internal
reordering and function substitutions. Issue #3 adds new public types but does not
modify any existing signatures.

---

### GOLD Certification — HeatupSimEngine (6 partial files)

```
Module: HeatupSimEngine (partial class, 6 files)
Files: HeatupSimEngine.cs, .Init.cs, .BubbleFormation.cs, .CVCS.cs, .Alarms.cs, .Logging.cs
Date: 2026-02-07

[X] G1  — Single responsibility per file (coordinator dispatches to physics modules)
[X] G2  — GOLD-compliant headers on all 6 files
[X] G3  — No inline physics: blended regime calls existing IsolatedHeatingStep/BulkHeatupStep
           and interpolates results; no new physics equations added to engine
[X] G4  — Result structs: RCPContribution struct for ramp-up state communication
[X] G5  — Constants from PlantConstants (staging constants in PlantConstants.Pressure.cs)
[X] G6  — NRC/Westinghouse sources cited (HRTD 6.1, 19.2.2 for heater feedback;
           HRTD 4.1, 10.3 for level program)
[X] G7  — Validation namespace (MonoBehaviour, no explicit namespace)
[~] G8  — HeatupSimEngine.cs at 35.2 KB exceeds 30 KB target but is under 40 KB hard limit.
           Growth from Issue #3 blended physics regime (~7 KB). Candidate for further
           decomposition in Phase 6 (split Regime 2 blending to a new partial).
[X] G9  — No dead code
[X] G10 — No duplication; heater calc runs in exactly one location

Status: [~] GOLD (G8 advisory — target exceeded, hard limit respected)
```

### GOLD Certification — CVCSController.cs

```
Module: CVCSController (single file)
File: CVCSController.cs
Date: 2026-02-07

[X] G1  — Single responsibility: CVCS PI level control, heater control, seal flows
[X] G2  — GOLD-compliant header
[X] G3  — N/A (this IS a physics module)
[X] G4  — Result structs (HeaterControlState, CVCSControllerState)
[X] G5  — Constants from PlantConstants
[X] G6  — Sources cited (NRC HRTD 4.1, 10.3 for level program; 19.0 for CVCS)
[X] G7  — namespace Critical.Physics
[~] G8  — 34.9 KB exceeds 30 KB target but is under 40 KB hard limit.
           Pre-existing size; Issue #2–3 changes added < 1 KB.
           Flagged for Phase 6 decomposition.
[X] G9  — No dead code
[X] G10 — No duplication; level setpoint function called consistently

Status: [~] GOLD (G8 advisory — pre-existing, flagged for Phase 6 split)
```

### GOLD Certification — RCPSequencer.cs

```
Module: RCPSequencer (single file)
File: RCPSequencer.cs
Date: 2026-02-07

[X] G1  — Single responsibility: RCP startup sequencing, timing, and ramp-up state
[X] G2  — GOLD-compliant header
[X] G3  — N/A (physics module — calculates RCP contribution fractions)
[X] G4  — Result structs (RCPStartupState, RCPContribution)
[X] G5  — Constants from PlantConstants.Pressure.cs
[X] G6  — Sources cited (NRC HRTD 13.0 for RCP startup procedure;
           Westinghouse FSAR for rated pump parameters)
[X] G7  — namespace Critical.Physics
[X] G8  — 20.1 KB (under 30 KB target)
[X] G9  — No dead code
[X] G10 — No duplication; staging fractions defined once in PlantConstants

Status: [X] GOLD
```

### GOLD Certification — PlantConstants.Pressure.cs

```
Module: PlantConstants.Pressure (partial class file)
File: PlantConstants.Pressure.cs
Date: 2026-02-07

[X] G1  — Single responsibility: pressure setpoints, solid plant control, RCPs, RHR
[X] G2  — GOLD-compliant header
[X] G3  — N/A (constants file)
[X] G4  — N/A (constants file)
[X] G5  — This IS the constants source
[X] G6  — All 15 new staging constants cite NRC HRTD 13.0 and Westinghouse RCP
           startup procedures
[X] G7  — namespace Critical.Physics
[X] G8  — 11.6 KB (under 30 KB target)
[X] G9  — No dead code
[X] G10 — No duplication; staging values defined once

Status: [X] GOLD
```

---

### GOLD Standard Module Status

**Modified this version — re-certified:**
- HeatupSimEngine (6 partial files) — GOLD (G8 advisory: 35.2 KB, under hard limit)
- CVCSController.cs — GOLD (G8 advisory: 34.9 KB, pre-existing, Phase 6 candidate)
- RCPSequencer.cs — GOLD
- PlantConstants.Pressure.cs — GOLD

**Previously GOLD — confirmed unchanged:**
HeatupSimEngine.Init.cs, HeatupSimEngine.Alarms.cs, HeatupSimEngine.Logging.cs,
PlantConstants.cs, PlantConstants.CVCS.cs, PlantConstants.Pressurizer.cs,
PlantConstants.Nuclear.cs, PlantConstants.Heatup.cs, PlantConstants.Validation.cs,
SolidPlantPressure.cs, WaterProperties.cs, VCTPhysics.cs, SteamThermodynamics.cs,
ThermalMass.cs, ThermalExpansion.cs, FluidFlow.cs, ReactorKinetics.cs,
PressurizerPhysics.cs, CoupledThermo.cs, HeatTransfer.cs, RCSHeatup.cs,
LoopThermodynamics.cs, TimeAcceleration.cs, AlarmManager.cs, RVLISPhysics.cs,
ControlRodBank.cs, ReactorCore.cs, FeedbackCalculator.cs, PowerCalculator.cs,
MosaicBoard.cs, MosaicBoardBuilder.cs, MosaicControlPanel.cs, MosaicAlarmPanel.cs,
MosaicIndicator.cs, MosaicRodDisplay.cs, MosaicGauge.cs, MosaicBoardSetup.cs,
MosaicTypes.cs

---

### Remaining Refactoring Phases (not started)

| Phase | Description | Status | Notes |
|-------|-------------|--------|-------|
| Phase 3 | Legacy cleanup (delete HeatupValidation.cs, deprecated methods) | Pending | |
| Phase 4 | HeatupValidationVisual decomposition (53 KB → 5 partials) | Pending | |
| Phase 5 | Test infrastructure (TestBase extraction, runner refactoring) | Pending | |
| Phase 6 | Near-GOLD elevation (split PressurizerPhysics, CVCSController, FuelAssembly) | Pending | Now also includes HeatupSimEngine.cs G8 remediation |

---

### Observations Resolved

Maps the 5 original observations from heatup log analysis to their resolutions:

| # | Observation | Root Cause | Resolution |
|---|------------|------------|------------|
| 1 | Heaters at 0.4 MW but T_pzr accelerates | Issue #1: execution order | Section 1B heater control |
| 2 | VCT exceeds 80%, DIVERT active | Issue #2: wrong level function | 4× `GetPZRLevelSetpointUnified()` |
| 3 | PZR drops to 7% at RCP start | Issue #3: binary RCP model (amplified by #1, #2) | Staged ramp-up + blended physics |
| 4 | System dumps water instead of building pressure | Issue #2 (primary) + Issue #1 (contributing) | Level program + heater feedback |
| 5 | What is the Programmed Level Curve? | Not a bug — documentation gap | Answered in IMPL_PLAN_v0.4.0; both programs modelled and now correctly called |

### Validation Required

A full 12+ hour heatup simulation run is required to validate all three fixes against
the pre-fix baseline (T0.5hr–T12.5hr logs). Key metrics to compare are documented in
the validation criteria sections of IMPL_PLAN_v0.4.0.md.
