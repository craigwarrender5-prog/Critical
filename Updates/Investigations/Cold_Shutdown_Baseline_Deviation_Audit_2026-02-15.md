# Cold Shutdown Baseline Extraction and Deviation Audit - 2026-02-15

## Scope
Investigation-phase extraction and comparison of Cold Shutdown baseline parameters from `Technical_Documentation` against current simulator defaults and initialization paths in:
- `Assets/Scripts/Validation/HeatupSimEngine.cs`
- `Assets/Scripts/Validation/HeatupSimEngine.Init.cs`
- `Assets/Scripts/Physics/PlantConstants.*.cs`
- `Assets/Scripts/Physics/CoupledThermo.cs`
- `Assets/Scripts/Physics/SGMultiNodeThermal.cs`
- `Assets/Scripts/Physics/VCTPhysics.cs`

## Extracted Cold Shutdown Reference Profile (Technical_Documentation)

| Parameter | Reference Value | Source |
|---|---|---|
| RCS temperature (Mode 5 initial) | ~120 F | `Technical_Documentation/RCS_Pressure_Temperature_Limit_Curves_Implementation.md:420`, `Technical_Documentation/RCS_Pressure_Temperature_Limit_Curves_Implementation.md:421`, `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:13` |
| RCS pressure (Mode 5 initial) | 50-100 psig | `Technical_Documentation/RCS_Pressure_Temperature_Limit_Curves_Implementation.md:422`, `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:14` |
| Pressurization step before RCP start | 400-425 psig via charging > letdown | `Technical_Documentation/RCS_Pressure_Temperature_Limit_Curves_Implementation.md:425`, `Technical_Documentation/RCS_Pressure_Temperature_Limit_Curves_Implementation.md:426`, `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:21`, `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:31` |
| Solid-plant control band | 320-400 psig | `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:41` |
| PZR level/state | PZR solid, 100% water, no steam space | `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:15` |
| PZR steam mass at cold shutdown | None (no steam space) | `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:15` |
| Heater sequencing | Pressure raised by CVCS first; heaters energized to begin PZR heatup after pressure stable in startup band | `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:21`, `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:31`, `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:33` |
| CVCS path/config in cold shutdown | Letdown via HCV-128 fully open, PCV-131 throttled; normal orifices very low flow; no seal injection until RCPs running | `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:24`, `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:45`, `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:163`, `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:164`, `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:165` |
| SG secondary initial state (Mode 5) | Wet layup 100% WR, nitrogen blanket ~2-6 psig, MSIVs closed, sealed/stagnant | `Technical_Documentation/SG_Secondary_Pressurization_During_Heatup_Research.md:24` |

## Current Implementation Comparison (Defaults + Init + First Step Path)

| Parameter | Reference | Current simulator default values | Effective Init/StepSimulation state | Delta | Impact risk |
|---|---|---|---|---|---|
| RCS temperature | ~120 F | `startTemperature = 100F` (`Assets/Scripts/Validation/HeatupSimEngine.cs:102`) | Cold init uses `startTemperature` (`Assets/Scripts/Validation/HeatupSimEngine.Init.cs:245`) | -20 F | Medium: timing shifts for drain/boiling/pressure milestones |
| RCS pressure (initial) | 50-100 psig | `startPressure = 400 psia` (~385 psig) (`Assets/Scripts/Validation/HeatupSimEngine.cs:103`) | Cold init overrides to `114.7 psia` (100 psig) (`Assets/Scripts/Validation/HeatupSimEngine.Init.cs:241`, `Assets/Scripts/Physics/PlantConstants.Pressure.cs:145`) | Default is +285 to +335 psig above reference; init override lands at upper bound | High: defaults are misleading and non-canonical without cold-init override |
| Solid-plant pressure band | 320-400 psig | Constants include low=320 psig, setpoint=350 psig, high=450 psig (`Assets/Scripts/Physics/PlantConstants.Pressure.cs:90`, `Assets/Scripts/Physics/PlantConstants.Pressure.cs:96`, `Assets/Scripts/Physics/PlantConstants.Pressure.cs:114`) | Solid controller initialized with those limits (`Assets/Scripts/Physics/SolidPlantPressure.cs:347`) | High limit +50 psig vs 400 psig reference | High: widens allowable cold-shutdown envelope beyond documented band |
| PZR level (cold shutdown) | 100% solid | `startPZRLevel = 25%` (`Assets/Scripts/Validation/HeatupSimEngine.cs:104`) | Cold init forces `pzrLevel = 100`, `pzrSteamVolume = 0` (`Assets/Scripts/Validation/HeatupSimEngine.Init.cs:255`, `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:257`) | Default -75%; init matches reference | Medium: default can misconfigure non-cold entry and confuse baseline provenance |
| PZR steam mass | No steam space | Not explicit in inspector defaults | Cold init sets `physicsState.PZRSteamMass = 0` (`Assets/Scripts/Validation/HeatupSimEngine.Init.cs:273`) | Match | Low |
| Heater state at cold start | CVCS pressure-rise first; heaters then energized for PZR heatup sequence | Engine field default `STARTUP_FULL_POWER` (`Assets/Scripts/Validation/HeatupSimEngine.cs:479`) | Cold init sets `currentHeaterMode = STARTUP_FULL_POWER`, `pzrHeaterPower = 1.8 MW`, `pzrHeatersOn = true` (`Assets/Scripts/Validation/HeatupSimEngine.Init.cs:232`, `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:142`, `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:455`, `Assets/Scripts/Validation/HeatupSimEngine.cs:625`) | Immediate full-power heater actuation at 100 psig | High: sequence deviates from documented pressurize-then-heat ordering |
| Heater capacities (constants alignment) | 1794 kW total, 414 kW proportional, 1380 kW backup | 1800 / 300 / 1500 kW (`Assets/Scripts/Physics/PlantConstants.Pressurizer.cs:57`, `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs:64`, `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs:71`) | Used for runtime power dispatch | Delta: +6 / -114 / +120 kW | Medium-High: control response and tuning traceability mismatch |
| CVCS cold-shutdown path | HCV-128 open, PCV-131 throttled, ~75 gpm letdown, no seal injection before RCPs | Constants include 75 gpm RHR crossconnect and temp-based path (`Assets/Scripts/Physics/PlantConstants.CVCS.cs:267`, `Assets/Scripts/Physics/PlantConstants.CVCS.cs:273`) | Init hard-sets `letdownFlow = 75`, `chargingFlow = 75`, `letdownViaRHR` boolean by temp threshold (`Assets/Scripts/Validation/HeatupSimEngine.Init.cs:412`, `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:413`, `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:418`); VCT cold init also 75/75 balanced, seal return 0 (`Assets/Scripts/Physics/VCTPhysics.cs:103`, `Assets/Scripts/Physics/VCTPhysics.cs:104`, `Assets/Scripts/Physics/VCTPhysics.cs:105`) | No explicit HCV-128/PCV-131 state variables; pressurization imbalance not explicitly initialized | High: control causality and operator-action traceability are implicit |
| SG secondary: pressure, level, steam inventory | 100% WR, N2 blanket 2-6 psig, no steam | SG constants/init: `SG_INITIAL_PRESSURE_PSIA = 17`, wide range 100%, steam inventory 0 (`Assets/Scripts/Physics/PlantConstants.SG.cs:663`, `Assets/Scripts/Physics/SGMultiNodeThermal.cs:756`, `Assets/Scripts/Physics/SGMultiNodeThermal.cs:798`, `Assets/Scripts/Physics/SGMultiNodeThermal.cs:805`) | Matches pressure/level/steam baseline | In-range match | Low |
| SG boundary openness vs sealed Mode 5 statement | Mode 5 statement says MSIVs closed / sealed stagnant | Engine init starts with `sgBoundaryMode = "OPEN"` (`Assets/Scripts/Validation/HeatupSimEngine.Init.cs:168`) and startup state `OpenPreheat` (`Assets/Scripts/Validation/HeatupSimEngine.cs:2177`) | `ShouldIsolateSGBoundary()` returns false in `OpenPreheat` (`Assets/Scripts/Validation/HeatupSimEngine.cs:2604`), so boundary remains open at cold start | Qualitative mismatch (sealed vs open) | Medium-High: can alter early secondary pressure and steam-path behavior |

## Missing Parameters / Implicit Assumptions / Hard-Coded Values / Uninitialized State

### Missing parameters
- No single formal `ColdShutdownProfile` object exists to define all Mode 5 baseline values in one authoritative structure.
- No explicit modeled state for HCV-128 valve position and PCV-131 throttle position at initialization (only inferred path/flow values).
- No explicit modeled MSIV closed/open state at cold-start boundary in engine init.

### Implicit assumptions
- Cold-shutdown init path is selected by `coldShutdownStart && startTemperature < 200F` (`Assets/Scripts/Validation/HeatupSimEngine.Init.cs:179`, `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:200`), so baseline correctness depends on a temperature threshold gate.
- Pressure ramp and CVCS causality are partly implied by controller behavior rather than explicit startup phase-state variables.

### Hard-coded values
- Multiple hard-coded `75f` charging/letdown initialization points (`Assets/Scripts/Validation/HeatupSimEngine.Init.cs:244`, `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:412`, `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:413`).
- Immediate startup heater mode hard-set to full-power (`Assets/Scripts/Validation/HeatupSimEngine.Init.cs:232`).

### Uninitialized / stale thermodynamic states
- `CoupledThermo.InitializeAtSteadyState()` and `InitializeAtHeatupConditions()` initialize mass/volume but do not initialize `PZRTotalEnthalpy_BTU` or closure residual flags (`Assets/Scripts/Physics/CoupledThermo.cs:703`, `Assets/Scripts/Physics/CoupledThermo.cs:741`, `Assets/Scripts/Physics/CoupledThermo.cs:793`), leaving default struct values.
- In active engine flow, PZR total enthalpy is set at init and in bubble-formation solver paths (`Assets/Scripts/Validation/HeatupSimEngine.Init.cs:274`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:828`), but there is no solid-phase timestep update of that persisted enthalpy field.

## Acceptance Criteria Status (Investigation Phase)

- Formally defined Cold Shutdown state profile in code: **Not met** (state is distributed across constants and init logic, not a single authoritative profile object).
- Initialization values explicitly set (not implicit inheritance): **Partially met** (many values explicit, but key path/control assumptions are implicit and threshold-gated).
- Initial PZR masses/volumes/pressure/temperature traceable to Technical_Documentation: **Partially met** (core mass/volume setup is explicit and traceable, but heater sequencing, pressure-band upper bound, and SG boundary mode show deviations).
- Regression test verifies stable cold-state equilibrium with zero residual drift: **Not met** (no dedicated cold-state equilibrium residual-drift regression found; existing acceptance tests include placeholder "REQUIRES SIMULATION" notes).

## Summary
The current implementation has the core ingredients for cold-start mass/volume initialization, but the baseline is not yet a single codified profile and has critical sequencing/causality deviations (heater timing, pressure-band upper limit, SG boundary openness, implicit CVCS valve-state modeling). This should be corrected before tuning two-phase closure robustness.
