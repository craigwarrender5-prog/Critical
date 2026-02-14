# CRITICAL: Master the Atom — Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
using a four-part scheme: **Major.Minor.Revision.Patch**.

- **Major** — Breaking changes; not backwards compatible.
- **Minor** — Backwards-compatible new features or structural additions.
- **Revision** — Backwards-compatible fixes, corrections, and parameter updates.
- **Patch** — Documentation, audits, and non-code changes.

> **Versioning Reset Notice:** This changelog consolidates all development history
> into a unified record starting at **v0.1.0**. Previous update files used a
> `v1.x.x.x` numbering scheme during initial development; those identifiers are
> preserved in parentheses for traceability. The semantic reset reflects that the
> project is in pre-release development (Major version 0).

---

## [Unreleased]

No pending changes.

---

## [0.2.0] — 2026-02-07 (was v1.4.0.0)

### Overview
Major rework of bubble formation physics and pressurizer heater control.
Full changelog: `Changelogs/CHANGELOG v0.2.0.md`

### Summary — Corrected Bubble Formation Physics & Pressurizer Heater Control
- Reworked bubble formation from mechanical drain to thermodynamic steam-displacement model per NRC HRTD 19.2.2 / 2.1
- Implemented multi-mode pressurizer heater controller with pressure-rate feedback per NRC HRTD 6.1 / 10.2
- Corrected CVCS flow sequence: charging 0→44 gpm (was 75), letdown 75 gpm (was 120), CCP starts at <80% level
- Added aux spray test model during bubble verification phase per NRC HRTD 19.2.2
- Added 25+ new PlantConstants for CCP, heater control, and aux spray test parameters
- GOLD STANDARD modules unchanged
- Design document: `DESIGN_BubbleFormation_HeaterControl.md`

### Files Modified
- PlantConstants.cs — 3 new constant regions, corrected drain flow constants, new validation checks
- CVCSController.cs — HeaterMode enum, multi-mode CalculateHeaterState with pressure-rate feedback
- HeatupSimEngine.cs — Thermodynamic drain, CCP start logic, aux spray test, heater mode transitions, VCT flow correction

---

## [0.1.0] — 2026-02-06

### Overview
Initial consolidated development release encompassing the complete Stage 1 codebase audit
(43 source files), Stage 2 parameter audit (130+ constants), critical physics model
corrections, bug fixes for the heatup simulation cascade failure, Reactor Operator GUI
design specification, and Unity implementation manual.

---

### Added

#### Test Infrastructure
- 9 heatup integration tests (HINT-01 through HINT-09) verifying cross-module mass conservation, surge flow propagation, and RCS mass tracking during solid plant operations *(was v1.0.1.0)*
- 35 support module tests wired into Phase 1 test runner (CV-01–07, RH-01–09, RS-01–08, LT-01–06, RV-01–05) covering CVCSController, RCSHeatup, RCPSequencer, LoopThermodynamics, and RVLISPhysics *(was v1.0.1.6)*
- 6 new HeatTransfer validation tests (tests 11–16) for stratified surge line model *(was v1.0.3.0)*
- 3 new RCSHeatup validation tests (tests 7–9) for stratified model integration *(was v1.0.3.0)*
- Phase 1 exit gate expanded: 121 → 156 tests
- Grand total across both runners: 216 → 251 tests

#### Physics Models
- Pressurizer surge line stratified natural convection model based on NRC Bulletin 88-11, replacing Churchill-Chu full-pipe correlation that overpredicted heat transfer by 10–20× *(was v1.0.3.0)*
  - New `StratificationFactor()` with buoyancy-enhanced mixing
  - New `SurgeLineEffectiveUA()` for effective UA with stratification
  - Deprecated `SurgeLineHTC()` with `[Obsolete]` attribute (retained for backward compatibility)
  - Grashof/Rayleigh/Nusselt correlations retained for use by other modules (SG, containment)
- Multi-phase bubble formation state machine with 7 phases: NONE → DETECTION → VERIFICATION → DRAIN → STABILIZE → PRESSURIZE → COMPLETE *(was v1.1.0.0)*
  - Realistic ~60 sim-minute drain procedure replacing instant single-timestep PZR snap
  - 7 new PlantConstants for bubble formation drain procedure (drain flows, phase durations, pressure limits) sourced from NRC HRTD 19.2.2, 2.1, and 4.1
  - CVCS override during drain phase (letdown 120 gpm, charging 75 gpm)
  - `bubbleFormed` flag only set TRUE at COMPLETE phase, correctly gating RCP start
- `CVCSController.PreSeedForRCPStart()` method pre-loading PI integral with charging bias for RCP thermal transient recovery *(was v1.1.0.0)*
- `VCTPhysics.CalculateBalancedChargingForPurification()` implemented (was returning stub 0) per NRC HRTD 4.1 purification balance *(was v1.2.0.0)*
- `PlantConstantsHeatup.GetPZRLevelSetpointUnified()` providing seamless full-range PZR level program coverage (heatup + at-power regimes) *(was v1.2.0.0)*

#### Alarm System
- `SolidPressurizer` flag added to `AlarmInputs` struct, suppressing PZR Level High/Low alarms during solid plant operations per NRC HRTD 19.2.1 *(was v1.0.2.0)*

#### Documentation
- Reactor Operator GUI design specification (`ReactorOperatorGUI_Design_v1.0.0.0.docx`) — 10-page document covering 193-assembly core mosaic map, instrument gauges, control panels, alarm annunciators, and data architecture *(was v1.3.0.0)*
- Unity implementation manual (`Unity_Implementation_Manual_v1.0.0.0.docx`) — ~30-page manual covering Unity fundamentals, step-by-step GUI construction, wiring, and testing *(was v1.3.0.1)*
- Complete Stage 1 audit documentation (sub-stages 1A through 1G) for all 43 source files *(was v1.0.1.2 through v1.0.1.5)*
- Stage 2 parameter audit reports (Parts 1 and 2) *(was v1.0.4.0, v1.0.5.0)*
- Handover document for PZR bubble formation and RCP startup bugs *(created during v1.1.0.0 investigation)*

#### Audit Artifacts
- `AUDIT_Stage1A_Constants_Properties.md` — 6 files (PlantConstants, WaterProperties, SteamThermodynamics, PlantConstantsHeatup, PlantConstantsReactor, LoopThermodynamics)
- `AUDIT_Stage1B_Heat_Flow.md` — 4 files (HeatTransfer, ThermalMass, ThermalExpansion, FluidFlow)
- `AUDIT_Stage1C_Pressurizer_Kinetics.md` — 4 files (PressurizerPhysics, SolidPlantPressure, CoupledThermo, ReactorKinetics)
- `AUDIT_Stage1D_Support_Systems.md` — 6 files (CVCSController, VCTPhysics, RCSHeatup, RCPSequencer, TimeAcceleration, AlarmManager)
- `AUDIT_Stage1E_Reactor_Core.md` — 7 files (ReactorCore, ControlRodBank, FuelAssembly, FeedbackCalculator, PowerCalculator, ReactorController, ReactorSimEngine)
- `AUDIT_Stage1F_Validation_Engine.md` — 4 files (HeatupSimEngine, HeatupValidationVisual, HeatupValidation, AlarmManager)
- `AUDIT_Stage1G_Tests_UI.md` — 15 files (6 test runners + 9 Mosaic UI components)
- `AUDIT_Stage2_ParameterAudit_Part1.md` — 130+ constants in PlantConstants.cs and PlantConstantsHeatup.cs
- `AUDIT_Stage2_ParameterAudit_Part2.md` — WaterProperties polynomial verification against NIST

---

### Changed

#### Physics Corrections
- **WaterProperties.cs** — 5 polynomial corrections validated against NIST Chemistry WebBook (IAPWS-IF97) *(was v1.0.5.0)*:
  - Sub-atmospheric saturation temperature: quadratic → cubic polynomial (error: ±18°F → ±0.04°F)
  - Sub-atmospheric saturation pressure: quadratic → cubic exponential (error: 83% → <0.1%)
  - Latent heat of vaporization: 2-range → 3-range polynomial system with near-critical floor clamp (error at 2250 psia: 14% → 1.8%)
  - Liquid water density: quadratic → cubic polynomial at 2250 psia reference (error: ±19% → ±2.3%)
  - Validation target corrected: hfg(2250 psia) from 465 → 390 BTU/lb per NIST

#### Parameter Corrections
- **PlantConstants.cs** — NRC setpoint corrections *(was v1.0.4.0)*:
  - `P_SPRAY_FULL`: 2280 → 2310 psig (NRC HRTD 10.2, 75 psig above setpoint)
  - `P_TRIP_LOW`: 1885 → 1865 psig (NRC HRTD 10.2.3.2)
- **PlantConstantsHeatup.cs** — Value and naming corrections *(was v1.0.4.0)*:
  - `MAX_HEATUP_RATE_F_HR` renamed to `TYPICAL_HEATUP_RATE_F_HR` (value unchanged at 50°F/hr; actual Tech Spec max is 100°F/hr in PlantConstants)
  - `MIN_RCP_PRESSURE_PSIA`: 350 → 334.7 psia (= 320 psig + 14.7, per NRC ML11223A342)
  - `NORMAL_OPERATING_PRESSURE`: 2235 → 2250 psia (was psig value used in psia context, NRC HRTD 2.1)

#### Constant Consolidation
- **VCTPhysics.cs** — 17 duplicate `const` fields replaced with `static` properties delegating to PlantConstants; `MIXING_TAU_SEC` retained as local (VCT-specific) *(was v1.2.0.0)*
- **ControlRodBank.cs** — `BANK_COUNT`, `STEPS_TOTAL`, `STEPS_PER_MINUTE` replaced with delegates to PlantConstants *(was v1.2.0.0)*
- **PowerCalculator.cs** — `NOMINAL_POWER_MWT` replaced with delegate to `PlantConstants.THERMAL_POWER_MWT` *(was v1.2.0.0)*

#### Improved Calculations
- **ReactorController.cs** — Power ascension reactivity estimate replaced: rough linear `1000 pcm/fraction` → `FeedbackCalculator.EstimatePowerDefect()` with Doppler and MTC components *(was v1.2.0.0)*

#### Test Corrections
- **Phase2TestRunner.cs** *(was v1.0.1.1)*:
  - SS-04: Power threshold test replaced with relative power increase + supercritical state assertion (original 0.001 threshold unreachable from source level in 20 seconds)
  - TR-06: Post-trip cooldown duration extended from 25 → 100 seconds (28s insufficient for precursor decay below 1% threshold)

#### Infrastructure
- Log output directory changed from `%APPDATA%/LocalLow/` to `<project_root>/HeatupLogs/` for external tool accessibility *(was v1.0.1.0)*
- Log interval changed from 15 → 30 sim-minutes (reduces file count 50% while maintaining data fidelity) *(was v1.0.1.0)*

---

### Fixed

- **Pressurizer bubble formation cascade failure** — Root cause: instant single-timestep PZR level snap (100% → 25%) violated mass conservation and left CVCS with no time to equilibrate. Replaced with multi-phase state machine per NRC procedures *(was v1.1.0.0)*
- **VCT mass conservation failure at RCP start** — Missing `AccumulateRCSChange()` call for two-phase operations caused mass conservation error to explode from 1.46 gal to 649 gal when RCPs started *(was v1.0.5.1)*
- **Surge line heat transfer overprediction** — Churchill-Chu full-pipe correlation overpredicted by 10–20× at relevant ΔT, preventing PZR from reaching Tsat (bubble formation impossible). Replaced with stratified model per NRC Bulletin 88-11 *(was v1.0.3.0)*
- **PZR energy balance error** — `IsolatedHeatingStep` did not subtract surge line heat loss from PZR side, creating energy from nothing. Added `Q_net_pzr = Q_heaters - Q_surge - Q_pzr_insulation` *(was v1.0.3.0)*
- **False PZR level alarms during solid plant** — PZR Level High firing continuously when level is intentionally 100% water-solid. Added `SolidPressurizer` flag to suppress level alarms during solid ops *(was v1.0.2.0)*
- **CVCS controller unable to recover after RCP start** — PI integral was zero when RCPs introduced thermal transient. Added `PreSeedForRCPStart()` pre-loading recovery capacity *(was v1.1.0.0)*

---

### Deprecated

- `HeatupValidation.cs` — Marked `[System.Obsolete]` with warning message. Legacy prototype with inline physics superseded by HeatupSimEngine + GOLD STANDARD modules. Retained for historical reference *(was v1.2.0.0)*
- `HeatTransfer.SurgeLineHTC()` — Marked `[Obsolete]`. Full-pipe correlation replaced by stratified model. Retained for backward compatibility *(was v1.0.3.0)*

---

### Known Issues (Deferred — Low Priority)

| ID | Description | Notes |
|----|-------------|-------|
| #4 | VCTPhysics missing `ValidateCalculations()` | Would need new test infrastructure |
| #5 | RCPSequencer timing constants local | Defensible — sequencer-specific |
| #8 | ReactorCore trip setpoints local | Defensible — safety-system isolation |
| #10 | `UpdateXenon` dt/3600 magic number | Cosmetic |
| #13 | Simplified rod drop model | Adequate for simulation |
| #19 | `FUEL_THERMAL_TAU` dual definition | Intentional — different contexts |
| #21 | Phase 1 / Phase 2 time systems independent | By design |
| #23, #35 | `FindObjectOfType` deprecated in Unity 6.3 | Migrate when Unity version updated |
| #28 | History buffer O(n) `RemoveAt(0)` | Negligible at current size |
| #36 | MosaicBoard alarm thresholds vs AlarmManager | Different purposes (UI vs safety) |

### Unverified — Requires Simulation Run

- Bug #3 (excessive PZR level drop at RCP start) — Expected to self-resolve via multi-phase bubble formation; needs full heatup run to confirm
- v1.1.0.0 bubble formation end-to-end sequence — Full heatup simulation not yet run with multi-phase state machine

---

## Version Cross-Reference

Maps consolidated changelog entries to original update file identifiers for traceability.

| Changelog | Original ID | Type | Scope |
|-----------|-------------|------|-------|
| 0.1.0 | v1.0.1.0 | Infrastructure | Log path, log interval, heatup integration tests |
| 0.1.0 | v1.0.1.1 | Test Fix | Phase 2 test runner SS-04 & TR-06 corrections |
| 0.1.0 | v1.0.1.2 | Audit | Stage 1D — Support Systems (6 files) |
| 0.1.0 | v1.0.1.3 | Audit | Stage 1E — Reactor Core Modules (7 files) |
| 0.1.0 | v1.0.1.4 | Audit | Stage 1F — Validation & Heatup Engine (4 files) |
| 0.1.0 | v1.0.1.5 | Audit | Stage 1G — Tests & UI (15 files), Stage 1 complete |
| 0.1.0 | v1.0.1.6 | Test Infrastructure | Wire 35 support module tests into Phase 1 runner |
| 0.1.0 | v1.0.2.0 | Bug Fix | PZR level alarm suppression during solid plant ops |
| 0.1.0 | v1.0.3.0 | Physics Fix (Breaking) | Surge line stratified convection model |
| 0.1.0 | v1.0.4.0 | Parameter Fix (Breaking) | NRC setpoint corrections (PlantConstants, PlantConstantsHeatup) |
| 0.1.0 | v1.0.5.0 | Physics Fix | WaterProperties NIST polynomial corrections |
| 0.1.0 | v1.0.5.1 | Bug Fix | VCT mass conservation tracking in two-phase ops |
| 0.1.0 | v1.1.0.0 | Structural (Breaking) | Multi-phase bubble formation state machine + CVCS pre-seed |
| 0.1.0 | v1.2.0.0 | Consolidation (Breaking) | Constant deduplication, legacy marking, API enhancements |
| 0.1.0 | v1.3.0.0 | Documentation | Reactor Operator GUI design specification |
| 0.1.0 | v1.3.0.1 | Documentation | Unity implementation manual |

---

## GOLD Standard Module Status

All core physics modules maintain GOLD STANDARD status. No physics calculations were modified
except where explicitly documented above (WaterProperties polynomials, HeatTransfer surge line model,
RCSHeatup energy balance). All other changes are to infrastructure, test wiring, constant sourcing,
and documentation.

### GOLD Standard Modules (unchanged physics)
PlantConstants.cs, SteamThermodynamics.cs, ThermalMass.cs, ThermalExpansion.cs,
FluidFlow.cs, ReactorKinetics.cs, PressurizerPhysics.cs, CoupledThermo.cs,
SolidPlantPressure.cs, FuelAssembly.cs, ControlRodBank.cs, ReactorCore.cs,
FeedbackCalculator.cs, PowerCalculator.cs, ReactorController.cs, ReactorSimEngine.cs,
RCPSequencer.cs, TimeAcceleration.cs, AlarmManager.cs, LoopThermodynamics.cs,
RVLISPhysics.cs, HeatupValidationVisual.cs, MosaicBoard.cs, MosaicBoardBuilder.cs,
MosaicControlPanel.cs, MosaicAlarmPanel.cs, MosaicIndicator.cs, MosaicRodDisplay.cs,
MosaicGauge.cs, MosaicBoardSetup.cs, MosaicTypes.cs

### GOLD Standard Modules (physics corrected with validation)
WaterProperties.cs, HeatTransfer.cs, RCSHeatup.cs, VCTPhysics.cs, CVCSController.cs

### GOLD Standard Modules (structural changes)
HeatupSimEngine.cs (bubble formation state machine, VCT tracking, alarm wiring)

---

## NRC / Industry References

Key source documents used for validation across all updates:

| Document | ADAMS # | Usage |
|----------|---------|-------|
| NRC HRTD 2.1 — Pressurizer System | ML11251A014 | Operating pressure, bubble formation description |
| NRC HRTD 4.1 — CVCS | ML11223A214 | CVCS flow balance, purification |
| NRC HRTD 6.1 — Pressurizer Heaters | — | Two-group heater control (design reference) |
| NRC HRTD 10.2 — PZR Pressure Control | ML11223A287 | Authoritative setpoint diagram |
| NRC HRTD 19.0 — Plant Operations | ML11223A342 | Heatup limits, RCP pressure requirements |
| NRC Bulletin 88-11 | — | Surge line thermal stratification |
| NRC IN 88-80 | — | Piping movement from stratification |
| NRC IN 93-84 | — | RCP seal flow verification |
| NUREG/CR-5757 | — | Thermal stratification in RCS-connected lines |
| NUREG-0737 Supp. 1 | — | RVLIS requirements |
| NIST Chemistry WebBook | — | IAPWS-IF97 steam tables, saturation data |
| Fink (2000) | — | UO2 thermal conductivity correlation |
| Kang & Jo, PVP2008-61204 | — | CFD analysis of surge line stratification |
| Qiao et al., Ann. Nucl. Energy 73 (2014) | — | Experimental surge line stratification data |
| Palisades LTOP Analysis | — | PZR max heating rate (100°F/hr) |
