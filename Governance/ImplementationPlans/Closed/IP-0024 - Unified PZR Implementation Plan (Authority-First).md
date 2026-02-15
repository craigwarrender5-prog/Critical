---
IP ID: IP-0024
Title: Unified Pressurizer (PZR) Implementation Plan (Authority-First)
Status: CLOSED
Changelog Version Association: 0.6.0.0
Date: 2026-02-15
Closure Date: 2026-02-15
Closure Timestamp: 2026-02-15 20:19:32
Final Run Stamp: 2026-02-15_201932
Final Stage D Gate Artifact: Governance/Issues/IP-0024_StageD_ExitGate_SinglePhaseHold_2026-02-15_201932.md
Final Stage H Deterministic Evidence: Governance/Issues/IP-0024_StageH_DeterministicEvidence_2026-02-15_201932.md
Scope Source: Governance/IssueRegister/issue_register.json (active PZR-related CS items)
Constraint: Planning artifact only; no code changes in this step
---

# IP-0024 - Unified Pressurizer (PZR) Implementation Plan

## Closeout Record
- Status: CLOSED
- Closure timestamp: 2026-02-15 20:19:32
- Final deterministic run stamp: `2026-02-15_201932`
- Stage D gate artifact: `Governance/Issues/IP-0024_StageD_ExitGate_SinglePhaseHold_2026-02-15_201932.md`
- Stage H deterministic evidence artifact: `Governance/Issues/IP-0024_StageH_DeterministicEvidence_2026-02-15_201932.md`

## 1) Documentation Baseline Summary (Mandatory First Phase)

### 1.1 PZR-Relevant Documentation Inventory (Technical_Documentation/)

#### A) Primary authority set (used for normative requirements)
- `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md`
- `Technical_Documentation/NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md`
- `Technical_Documentation/NRC_HRTD_Section_10.3_Pressurizer_Level_Control.md`
- `Technical_Documentation/NRC_HRTD_Section_3.2_Reactor_Coolant_System.md`
- `Technical_Documentation/NRC_HRTD_Section_4.1_Chemical_Volume_Control_System.md`
- `Technical_Documentation/NRC_HRTD_Section_5.1_Residual_Heat_Removal_System.md`
- `Technical_Documentation/RCS_Pressure_Temperature_Limit_Curves_Implementation.md`
- `Technical_Documentation/Westinghouse_4Loop_Pressurizer_Specifications_Summary.md`

#### B) Secondary/support set (used for cross-check, not sole authority)
- `Technical_Documentation/RCS_PT_Limits_and_Steam_Tables_Reference.md`
- `Technical_Documentation/PZR_Level_Pressure_Deficit_Analysis_v4.4.0.md`
- `Technical_Documentation/Conformance_Audit_Report_2026-02-15.md`
- `Technical_Documentation/Technical_Documentation_Index.md`
- `Technical_Documentation/NRC_REFERENCE_SOURCES.md`
- `Technical_Documentation/SG_Secondary_Pressurization_During_Heatup_Research.md`

#### C) Archive/supporting references discovered in scan (context only)
- `Technical_Documentation/Archive/Pressurizer_Documentation_Research_Summary_2026-02-15.md`
- `Technical_Documentation/Archive/PZR_CVCS_Documentation_Complete_Summary_2026-02-15.md`
- `Technical_Documentation/Archive/PT_Limits_Documentation_Session_Summary_2026-02-15.md`
- `Technical_Documentation/Archive/PT_Limits_Steam_Tables_Assessment_Summary_2026-02-15.md`
- `Technical_Documentation/Archive/Additional_Documentation_Search_Results_2026-02-14.md`
- `Technical_Documentation/Archive/Documentation_Analysis_2026-02-14.md`
- `Technical_Documentation/Archive/High_Priority_Session_Final_Summary_2026-02-14.md`
- `Technical_Documentation/Archive/Technical_Documentation_Summary_2026-02-14.md`

### 1.2 Consolidated PZR Technical Baseline (Authoritative Requirements)

| Requirement Area | Baseline Requirement |
|---|---|
| Cold shutdown initial state | Mode 5, `T_avg ~120F`, `PZR pressure 50-100 psig`, PZR solid (`100% water`, no steam space), RCPs off, RHR in service |
| Early startup pressurization | Raise pressure by `charging > letdown`; startup pressurization target `400-425 psig` before RHR isolation boundary |
| Solid-plant pressure control philosophy | CVCS flow-balance control band described as `320-400 psig` (charging/letdown balance) |
| Bubble formation | Bubble established at saturation for maintained pressure (example: ~`450F @ 400 psig`), draw by max letdown/min charging, target near `25%` level |
| Pressure control setpoint architecture | Normal pressure setpoint `2235 psig`, adjustment range `1700-2500 psig` |
| Heater/spray/PORV setpoints | Backup heaters on/off `2210/2217 psig`; proportional heaters `2220->2250 psig`; spray start/full `2260/2310 psig`; PORV `2335 psig`; trips at `2385`, `1865`, SI `1807` |
| Pressurizer design constants | PZR total volume `1800 ft^3`; heaters `1794 kW` total (`414 kW` proportional + `1380 kW` backup); spray max typically `840 gpm` (some references cite `900 gpm`) |
| Level control program | Programmed level `25%` at no-load (`~557F`) to `61.5%` at full-power (`~584.7F`); low-level isolate/heater cutoff `17%`; high-level trip `92%`; anticipatory backup heater at `program + 5%` |
| CVCS/PZR interaction | Normal letdown `75 gpm` via one 75-gpm orifice at operating pressure; orifice bank `2x75 + 1x45`; ion-exchanger practical max `120 gpm`; PCV-131 nominal `340 psig`; HCV-128 path used in cold shutdown/low pressure |
| Surge dynamics | PZR surge line provides insurge/outsurge accommodation; pressure response depends on steam/water distribution and compressibility |

### 1.3 Internal Documentation Conflicts (Flagged; Not Resolved Here)

| Conflict ID | Conflicting Statements | Documents |
|---|---|---|
| DOC-CF-01 | RCP start sequencing conflicts: one source says start RCPs before bubble, another after bubble | `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md` |
| DOC-CF-02 | RCP start pressure threshold ambiguity (`400-425 psig` startup wording vs `>=320 psig` post-bubble wording) | `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md` |
| DOC-CF-03 | Solid-plant pressure band wording (`320-400 psig`) vs startup pressurization target (`400-425 psig`) requires staged-governance interpretation | `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md`, `Technical_Documentation/RCS_Pressure_Temperature_Limit_Curves_Implementation.md` |
| DOC-CF-04 | Spray max flow `840 gpm` vs alternate references citing `900 gpm` | `Technical_Documentation/NRC_HRTD_Section_3.2_Reactor_Coolant_System.md`, `Technical_Documentation/Westinghouse_4Loop_Pressurizer_Specifications_Summary.md` |

Governance action required: adjudicate DOC-CF-01..04 and publish one approved baseline profile before code-level closure.

---

## 2) Implementation Crosswalk (Authority -> Current Simulator -> CS/Finding)

| Req ID | Authoritative Requirement | Current Implementation (files) | Current Behavior Assessment | Linked CS / Findings |
|---|---|---|---|---|
| R-01 | Cold shutdown init must be explicit and traceable (`~120F`, `50-100 psig`, solid PZR) | `Assets/Scripts/Validation/HeatupSimEngine.cs`, `Assets/Scripts/Validation/HeatupSimEngine.Init.cs`, `Assets/Scripts/Physics/PlantConstants.Pressure.cs` | Partial: cold init forces `100 psig` and solid PZR, but inspector defaults are non-canonical (`startPressure=400 psia`, `startPZRLevel=25`) and threshold-gated | `CS-0093`, `CS-0094`, `Updates/Investigations/Cold_Shutdown_Baseline_Deviation_Audit_2026-02-15.md` |
| R-02 | Startup sequencing should be pressurize-then-heat (per approved authority) | `Assets/Scripts/Validation/HeatupSimEngine.Init.cs`, `Assets/Scripts/Validation/HeatupSimEngine.cs` | Mismatch risk: cold start sets heater mode `STARTUP_FULL_POWER`; heaters energized at init | `CS-0094`, `CS-0093` |
| R-03 | Solid-plant pressure control envelope alignment | `Assets/Scripts/Physics/PlantConstants.Pressure.cs`, `Assets/Scripts/Physics/SolidPlantPressure.cs` | Mismatch: configured high bound includes `450 psig` path, while documentation cites `320-400 psig` control band | `CS-0081`, `CS-0093` |
| R-04 | Persistent PZR thermodynamic state across two-phase closure | `Assets/Scripts/Validation/HeatupSimEngine.Init.cs`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs`, `Assets/Scripts/Physics/CoupledThermo.cs` | Implemented in bubble path: persistent `PZRTotalEnthalpy_BTU` seeded/updated and used in closure solve; still needs end-to-end validation envelope and solid-to-two-phase profile governance | `CS-0091`, `CS-0093` |
| R-05 | Closure must enforce mass+volume+energy tolerances before commit; no renormalization masking | `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs` | Partially aligned in current code (convergence-gated state commit). Open validation gap remains versus investigation evidence and acceptance targets (`>95%` convergence) | `CS-0091`, `CS-0093`, `Updates/Investigations/PZR_Bubble_Behaviour_Investigation_2026-02-15.md` |
| R-06 | Continuous phase transitions (avoid discontinuous floors) | `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs` | Open issue: phase-specific minimum water floors (`25%` stabilize vs `23%` pressurize guard) still create discontinuity risk | `CS-0091`, `CS-0093` |
| R-07 | Pressure-control setpoints must trace to 10.2 baseline | `Assets/Scripts/Physics/PlantConstants.Pressure.cs`, `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs` | Partial mismatch: overlapping constants with inconsistent values (example: backup heater OFF differs from baseline) | `CS-0093` |
| R-08 | Heater capacities should match approved technical baseline | `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs`, `Assets/Scripts/Physics/PlantConstants.Pressure.cs` | Mismatch/duplication: runtime constants include `1800/300/1500` and `1800/500/1300` families, not single authority value set | `CS-0093` |
| R-09 | Spray capacity and behavior should be authority-traceable | `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs`, `Assets/Scripts/Validation/HeatupSimEngine.cs` | Pending governance due documentation conflict (`840` vs `900`); code currently uses `900` max constant and separate dynamic full-open flow assumptions | `CS-0093`, DOC-CF-04 |
| R-10 | CVCS/orifice causality should be explicit and hydraulic-state-driven | `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs`, `Assets/Scripts/Physics/PlantConstants.CVCS.cs` | Non-DRAIN path is lineup-based; DRAIN path still command-first aggregate policy (`75..120`) then lineup derivation | `CS-0092`, `CS-0093`, `Updates/Investigations/PZR_Orifice_Aggregation_Diagnosis_2026-02-15.md` |
| R-11 | Cold-start CVCS valve/config state should be explicit (HCV-128/PCV-131 causality) | `Assets/Scripts/Validation/HeatupSimEngine.Init.cs`, `Assets/Scripts/Physics/PlantConstants.CVCS.cs` | Partial/implicit: path booleans and flow constants exist, but explicit valve-state model at initialization is not codified | `CS-0093`, `CS-0094` |
| R-12 | PZR-related operator realism/gating at startup | `Assets/Scripts/Validation/HeatupSimEngine.cs`, `Assets/Scripts/UI/PressurizerScreen.cs` | Not implemented: no deterministic startup hold and no OFF/AUTO/MANUAL-disabled heater mode lockout path | `CS-0094` |
| R-13 | PZR-related indicator consistency during drain | `Assets/Scripts/Validation/HeatupSimEngine.Alarms.cs`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs` | Open instrumentation integrity issue | `CS-0040` |

---

## 3) CS Inventory (No Domain Grouping)

Active PZR-related CS scope for this IP:

| CS ID | Severity | Status | Title |
|---|---|---|---|
| CS-0093 | HIGH | INVESTIGATING | Complete Pressurizer (PZR) System Remodel to Align with Technical_Documentation and Replace Current Heuristic Bubble Model |
| CS-0091 | HIGH | INVESTIGATING | PZR bubble closure path shows persistent non-convergence with large residuals and post-residual renormalization masking |
| CS-0092 | HIGH | INVESTIGATING | Drain-phase CVCS letdown uses aggregate command-first logic that can force immediate 1-to-3 orifice opening |
| CS-0040 | HIGH | READY_FOR_FIX | RVLIS indicator stale during PZR drain |
| CS-0081 | MEDIUM | INVESTIGATING | Solid-plant high pressure control band is configured above documented operating band |
| CS-0094 | MEDIUM | INVESTIGATING | Add Cold-Start Stabilization Hold and PZR Heater Off/Auto/Manual Control to Prevent Immediate Pressurization and Improve Operator Realism |

Interface dependency to track during execution (not primary PZR issue scope): `CS-0079` (startup pressure permissive threshold governance).

---

## 4) Dependency Matrix

| Work Item | Depends On | Why |
|---|---|---|
| D-01 Governance baseline freeze (resolve DOC-CF-01..04) | None | Prevents rework and conflicting baseline values |
| D-02 Formal ColdShutdownProfile codification | D-01 | All subsequent tuning depends on correct initial physics |
| D-03 Startup gating (stabilization hold and/or heater mode lockout) | D-02 | Removes frame-1 transients that corrupt solver evaluation |
| D-04 Thermodynamic authority model unification (single source constants + state ownership) | D-01, D-02 | Required before solver replacement and control tuning |
| D-04G Stage-D entry gate validation (stable single-phase equilibrium) | D-04 | Prevents two-phase debugging on unstable base thermodynamics |
| D-05 Two-phase closure solver hardening/replacement (mass+volume+energy) | D-04G | Must run on verified stable thermodynamic formulation |
| D-06 DRAIN CVCS/orifice causal remodel | D-04, D-05 | Requires stable state equations and explicit causality |
| D-07 Pressure/level setpoint reconciliation and control policy cleanup | D-01, D-04 | Setpoint tuning without authority freeze is unsafe |
| D-08 Indicators/telemetry closure (RVLIS + diagnostics) | D-05, D-06 | Must reflect final causal physics paths |
| D-09 Final regression + closure evidence package | D-03 through D-08 | Needed for CS closure and umbrella completion |

---

## 5) Proposed Execution Order

1. Stage A: Authority governance freeze for PZR baseline (resolve DOC-CF-01..04).
2. Stage B: Implement formal `ColdShutdownProfile` and explicit startup initialization ownership.
3. Stage C: Add startup stabilization/gating (`CS-0094` baseline option) before bubble-phase diagnostics.
4. Stage D: Consolidate thermodynamic/state authority and constants into one PZR reference set.
5. Stage D-Gate: Pass stable single-phase equilibrium qualification (hard gate before any two-phase work).
6. Stage E: Replace/harden two-phase closure path with strict mass+volume+energy convergence gating.
7. Stage F: Remodel DRAIN CVCS/orifice causality (derive commanded flow from explicit lineup/hydraulics).
8. Stage G: Reconcile pressure/level/heater/spray setpoints and remove duplicated inconsistent constants.
9. Stage H: Close indicator and diagnostics integrity gaps (`CS-0040`) and finalize regression evidence.

This preserves the required order of operations: baseline integrity -> thermodynamic remodel -> solver robustness -> control/UX refinement.

---

## 6) Stage Acceptance Criteria

### Stage A - Authority Freeze
- Governance decision record exists for DOC-CF-01..04.
- One approved PZR baseline table is published with source traceability.

### Stage B - Cold Shutdown Profile Integrity
- A formal cold-shutdown profile object/config is present and authoritative.
- Init values are explicit (no hidden fallback defaults for PZR-critical fields).
- PZR masses, volumes, pressure, temperature, and CVCS startup config are traceable.

### Stage C - Startup Stabilization/Gating
- No immediate heater energization unless hold release and mode allow.
- Logs/telemetry show hold state and heater control mode deterministically.

### Stage D - Thermodynamic Authority
- Single ownership model for PZR mass/energy/volume state is documented in code and design doc.
- Duplicated conflicting constants removed or explicitly aliased to one authority source.
- Single-phase equilibrium behavior is stable under cold-shutdown and pre-bubble startup conditions.

### Stage D-Gate - Mandatory Entry Gate for Stage E
- A deterministic single-phase equilibrium test suite passes before Stage E starts.
- No nonphysical pressure/level drift under zero-net boundary flow hold conditions.
- Residual drift remains within defined tolerance for the full gate duration.
- Gate evidence is archived and referenced in Stage E kickoff.

### Stage E - Two-Phase Closure
- Entry criteria: Stage D-Gate must be PASSED.
- Closure solves pressure and phase distribution using mass + volume + energy constraints.
- State commit only when both residuals are within tolerance.
- Demonstrated bubble-phase convergence >95% with bounded residuals.

### Stage F - CVCS/Orifice Causality
- DRAIN letdown is derived from explicit orifice lineup/hydraulic state (or equivalent physically causal model).
- No command-first aggregate jump causing nonphysical 1->3 lineup cliff at phase entry.

### Stage G - Control Setpoint Alignment
- Heater/spray/PORV and level setpoint families match approved authority baseline.
- Any intentional deviation is documented with rationale and impact.

### Stage H - System Closure Evidence
- No nonphysical PZR level/pressure cliffs attributable to solver structure.
- RVLIS and PZR inventory indicators remain causally consistent during drain and transitions.
- CS-0091/0092 child closure evidence supports CS-0093 umbrella closure.

---

## 7) Regression Strategy

Required deterministic regression suite for IP-0024 closeout:

1. Cold-shutdown equilibrium hold test.
   - Start in formal cold profile.
   - Validate stable mass/energy state and no residual drift during hold window.
2. Stage D-Gate single-phase equilibrium qualification test.
   - Deterministic run with single-phase physics only (pre-bubble path).
   - Validate bounded pressure/level drift and stable residual behavior.
   - Required pass before enabling Stage E two-phase work.
3. Startup sequence conformance test.
   - Validate pressurize-before-heat (or approved governed sequence), with clear event traces.
4. Bubble formation continuity test.
   - Validate no discontinuous PZR level/pressure jumps at phase boundaries.
5. Two-phase solver convergence test.
   - Track attempt counts, converged counts, max/mean residuals, failure reasons.
   - Gate on >95% convergence during bubble phases.
6. DRAIN orifice causality test.
   - Prove applied letdown derives from lineup/hydraulics and transitions smoothly.
7. Setpoint fidelity test.
   - Assert runtime pressure/level/heater/spray thresholds equal approved baseline constants.
8. Instrumentation consistency test.
   - RVLIS/PZR level/primary inventory consistency checks during DRAIN and STABILIZE.

Evidence artifacts required:
- Run logs, residual summaries, phase-transition plots, and comparison against baseline table.
- Explicit pass/fail matrix mapped to `CS-0040`, `CS-0081`, `CS-0091`, `CS-0092`, `CS-0093`, `CS-0094`.

---

## 8) Plan Notes

- This plan intentionally supersedes fragmented PZR patching and treats `CS-0093` as umbrella scope with child validation via `CS-0091` and `CS-0092`.
- No domain-based grouping was used in CS inventory; ordering is dependency/risk driven.
- This document is a planning and authority-consolidation artifact only; no runtime code was modified in this step.

