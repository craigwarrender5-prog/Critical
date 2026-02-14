# VALIDATION_MAP.md — Complete Validation & Health-Check Inventory
## Recovery Audit 2026-02-13

---

## Summary

This document catalogs **every** runtime validation, health check, conservation assertion, alarm, guard, clamp, and diagnostic in the codebase, organized by category. Each entry cites the exact source file + line range and describes what it checks, when it fires, thresholds, and side effects.

**Total unique checks identified: 54** (across 7 categories)

---

## TABLE OF CONTENTS

1. [Mass Conservation Checks](#1-mass-conservation-checks) (8 checks)
2. [CoupledThermo Solver Validation Tests](#2-coupledthermo-solver-validation-tests) (7 tests)
3. [Acceptance Tests (v5.4.0)](#3-acceptance-tests-v540) (10 tests)
4. [Alarm Setpoint Checks (AlarmManager)](#4-alarm-setpoint-checks-alarmmanager) (14 alarms)
5. [UI PASS/FAIL Validation Checks](#5-ui-passfail-validation-checks) (12 checks)
6. [Guards, Clamps & Bounds](#6-guards-clamps--bounds) (various)
7. [Diagnostic Logging](#7-diagnostic-logging) (various)

---

## 1. MASS CONSERVATION CHECKS

These are the core conservation assertions that run during simulation.

---

### CHECK-001: Primary Mass Ledger Drift (massError_lbm)

- **File:** `HeatupSimEngine.CVCS.cs:290-301`
- **Trigger:** Every timestep, inside `UpdateVCT()` → called from `UpdateCVCSFlows()`
- **Computation:**
  ```
  totalSystemMass_lbm = rcsMass + pzrWaterMass + pzrSteamMass + vctMass + brsMass
  externalNetMass_lbm = net external boundary flows (makeup, CBO, BRS, seal)
  massError_lbm = |totalSystemMass_lbm - initialSystemMass_lbm - externalNetMass_lbm|
  ```
- **Thresholds:**
  - PASS: < 100 lbm (green)
  - WARN: 100–500 lbm (amber)
  - FAIL: > 500 lbm (red)
- **Side Effects:** Display field `massError_lbm` consumed by UI (TabValidation.cs:168, Panels.cs:447, TabCritical.cs:333)
- **Notes:** This is the **primary runtime mass conservation check**. It tracks the full system (RCS + PZR + VCT + BRS).
- **Confidence:** High

---

### CHECK-002: Primary Mass Ledger vs Component Sum (UpdatePrimaryMassLedgerDiagnostics)

- **File:** `HeatupSimEngine.Logging.cs:470-583`
- **Trigger:** **DEAD CODE — NEVER CALLED**
- **Computation:**
  ```
  primaryMassDrift_lb = TotalPrimaryMass_lb - (RCSWaterMass + PZRWaterMass + PZRSteamMass)
  primaryMassDrift_pct = |drift| / componentSum * 100
  primaryMassBoundaryError_lb = |TotalPrimaryMass_lb - expectedFromBoundaryFlows|
  ```
- **Thresholds:**
  - OK: drift_pct ≤ 0.1%
  - WARNING: drift_pct > 0.1%
  - ALARM: drift_pct > 1.0%
- **Side Effects (if called):** Sets `primaryMassStatus`, `primaryMassAlarm`; logs alarm edge events via `LogEvent()`
- **CRITICAL FINDING:** Method is fully implemented at Logging.cs:470 but no call site exists anywhere in the codebase. The comment at Init.cs:184 references it ("read by UpdatePrimaryMassLedgerDiagnostics()") as if it runs, but the function is dead code. This is the independent ledger-vs-components cross-check that should detect solver drift.
- **Confidence:** High (verified via full codebase grep — zero call sites)

---

### CHECK-003: Inventory Audit (UpdateInventoryAudit)

- **File:** `HeatupSimEngine.Logging.cs:261-436`
- **Trigger:** Every timestep, called from StepSimulation:1513
- **Computation:**
  - Reads canonical mass fields (regime-aware):
    - Solid ops: `PZRWaterMassSolid`, `TotalPrimaryMassSolid` (Logging.cs:303-310)
    - Two-phase: `RCSWaterMass`, `PZRWaterMass`, `PZRSteamMass` (Logging.cs:317-320)
  - Adds VCT + BRS masses (at ~100°F atmospheric density)
  - Tracks cumulative flows: charging, letdown, seal injection, seal return, surge, makeup, CBO
  - Conservation: `|Total_current - (Total_initial + netExternal)| = Conservation_Error_lbm`
- **Thresholds:**
  - `CONSERVATION_ERROR_THRESHOLD_LBM = 500 lbm` (Logging.cs:217)
  - `CONSERVATION_ERROR_THRESHOLD_PCT = 0.5%` (Logging.cs:220)
- **Side Effects:**
  - Sets `inventoryAudit.Conservation_Alarm` flag
  - Logs alarm edge: "INVENTORY CONSERVATION ERROR: {error} lbm ({pct}%)"
  - Sets status string: "OK" / "WARN" / "ALARM"
  - AuditMassSource field: "CANONICAL_SOLID" or "CANONICAL_TWO_PHASE"
- **Confidence:** High

---

### CHECK-004: VCT Conservation Cross-Check

- **File:** `VCTPhysics.cs:346-389`
- **Trigger:** Called from `HeatupSimEngine.CVCS.cs` inside `UpdateVCT()`
- **Computation:**
  ```
  vctChange = VCT_current_gal - VCT_initial_gal
  rcsChange = CumulativeRCSChange_gal
  externalNet = CumulativeExternalIn_gal - CumulativeExternalOut_gal
  error = |vctChange + rcsChange - externalNet|
  ```
- **Thresholds:**
  - Display: PASS < 10 gal, WARN 10-50 gal, FAIL > 50 gal (TabValidation.cs:174-176)
  - Diagnostic log trigger: error > 100 gal (VCTPhysics.cs:373)
- **Side Effects:**
  - Returns error value (stored as `massConservationError`)
  - Logs detailed diagnostic at > 100 gal: "[VCT_CONS_DIAG]" with all terms
  - Warns if rcsChange ≈ 0 but vctChange > 100 ("Possibly in solid ops")
- **Confidence:** High

---

### CHECK-005: Ledger Re-Baseline (First-Step Guard)

- **File:** `HeatupSimEngine.cs:1452-1458`
- **Trigger:** Once, after the very first physics step (flag: `firstStepLedgerBaselined`)
- **Computation:**
  ```
  actualTotal = RCSWaterMass + PZRWaterMass + PZRSteamMass
  TotalPrimaryMass_lb = actualTotal
  InitialPrimaryMass_lb = actualTotal
  ```
- **Thresholds:** None (one-time override)
- **Side Effects:** Overwrites both canonical and initial mass fields to match post-first-step solver output
- **Purpose:** Eliminates init-to-first-step density mismatch between V×ρ init and solver output
- **Confidence:** High

---

### CHECK-006: Solid-Phase Canonical Mass Sync

- **File:** `HeatupSimEngine.cs:1080-1085`
- **Trigger:** Every timestep during solid PZR / pre-drain bubble operations (Regime 1)
- **Computation:**
  ```
  PZRWaterMassSolid = PZRWaterMass
  TotalPrimaryMassSolid = RCSWaterMass + PZRWaterMassSolid
  TotalPrimaryMass_lb = TotalPrimaryMassSolid
  ```
- **Side Effects:** Keeps canonical ledger consistent during solid ops
- **Confidence:** High

---

### CHECK-007: Regime 2/3 PZR Mass Sync from V×ρ

- **File:** `HeatupSimEngine.cs:1163-1165` (Regime 2), `HeatupSimEngine.cs:1317-1319` (Regime 3)
- **Trigger:** Every timestep in Regime 2 and 3, BEFORE CoupledThermo solver
- **Computation:**
  ```
  PZRWaterMass = pzrWaterVolume × WaterDensity(T_sat, P)
  PZRSteamMass = pzrSteamVolume × SaturatedSteamDensity(P)
  ```
- **Thresholds:** None (direct overwrite)
- **Side Effects:** **OVERWRITES** physicsState PZR masses from engine volume state using V×ρ
- **POTENTIAL ISSUE:** This is a V×ρ overwrite of canonical fields immediately before the solver runs. The solver then uses these values. If the solver's `SolveEquilibrium` enforces the `totalPrimaryMass_lb` constraint afterward (which it does in Regime 3 via the mass parameter), this overwrite may create an inconsistency: the PZR masses fed into the solver don't necessarily sum to the ledger value. The solver redistributes mass to satisfy the constraint, which may cause the "remainder" RCS mass to absorb the error.
- **Confidence:** High (code verified), impact assessment Medium (needs Stage 3 deep audit)

---

### CHECK-008: CVCS Pre-Application Double-Count Guard

- **File:** `HeatupSimEngine.cs:1187` (Regime 2), `HeatupSimEngine.cs:1351` (Regime 3)
- **Trigger:** Set `regime3CVCSPreApplied = true` before solver; checked in `UpdateRCSInventory()`
- **Computation:** Boolean flag prevents CVCS mass from being applied twice (once before solver, once in CVCS partial)
- **Side Effects:** Skips RCS mass update in CVCS partial if already applied
- **Confidence:** High

---

## 2. COUPLEDTHERMO SOLVER VALIDATION TESTS

These are **static validation methods** — not runtime checks. Called only from `Phase1TestRunner.cs`.

---

### CTEST-001: 10°F Pressure Response Test

- **File:** `CoupledThermo.cs:475-491`
- **Trigger:** Manual test runner only (`Phase1TestRunner.cs:564`)
- **Computation:** Initialize at steady state → apply +10°F → check ΔP
- **Thresholds:** ΔP must be 50–100 psi (expected ~60-80 psi, i.e. 6-8 psi/°F)
- **Side Effects:** Returns bool PASS/FAIL; Console.WriteLine on failure
- **Confidence:** High

### CTEST-002: Coupled < Uncoupled Expansion Test

- **File:** `CoupledThermo.cs:496-511`
- **Trigger:** Manual test runner only
- **Computation:** Compare coupled vs uncoupled surge volume for same ΔT
- **Thresholds:** coupled < uncoupled (strict inequality)
- **Confidence:** High

### CTEST-003: Mass Conservation Through Solver

- **File:** `CoupledThermo.cs:516-529`
- **Trigger:** Manual test runner only
- **Computation:** `|finalMass - initialMass| / initialMass` after +20°F step
- **Thresholds:** error < 0.1% (0.001)
- **Confidence:** High

### CTEST-004: Volume Conservation (Rigid RCS)

- **File:** `CoupledThermo.cs:534-547`
- **Trigger:** Manual test runner only
- **Computation:** `|finalVolume - initialVolume| / initialVolume` after +15°F step
- **Thresholds:** error < 0.01% (0.0001)
- **Confidence:** High

### CTEST-005: Convergence Speed

- **File:** `CoupledThermo.cs:552-559`
- **Trigger:** Manual test runner only
- **Computation:** Must converge AND `IterationsUsed < 20`
- **Thresholds:** < 20 iterations for +10°F at nominal conditions
- **Confidence:** High

### CTEST-006: Steam Space Minimum Clamp

- **File:** `CoupledThermo.cs:564-573`
- **Trigger:** Manual test runner only
- **Computation:** After +50°F rise, `PZRSteamVolume >= PZR_STEAM_MIN`
- **Thresholds:** `PlantConstants.PZR_STEAM_MIN` (value needs verification in Stage 3)
- **Confidence:** High

### CTEST-007: Heatup Range Convergence

- **File:** `CoupledThermo.cs:582-600`
- **Trigger:** Manual test runner only
- **Computation:** Solver at 300°F / 400 psia (Mode 4), +10°F → must converge, ΔP in 5–120 psi
- **Thresholds:** convergence + ΔP ∈ [5, 120] psi
- **Notes:** Tests low-pressure regime (Phase 2 fix for C3 audit finding)
- **Confidence:** High

---

## 3. ACCEPTANCE TESTS (v5.4.0)

These are **architectural validation tests** — not runtime checks. All 10 currently validate calculation correctness but note "REQUIRES SIMULATION" for true end-to-end testing.

**File:** `AcceptanceTests_v5_4_0.cs:38-531`
**Runner:** `AcceptanceTests_v5_4_0.RunAllTests()` (line 473)

| Test | Rule | Criterion | Threshold |
|------|------|-----------|-----------|
| AT-01 | R2 | CVCS step: -15 gpm × 10 min mass change | -1,250 ± 50 lb |
| AT-02 | R1/R3 | No-flow drift: balanced CVCS, 4 hr | drift < 0.01% (< 60 lb) |
| AT-03 | R5 | Solid→Two-phase mass continuity | ± 1 lb at handoff |
| AT-04 | R2 | Relief open: mass = ∫ṁ_relief dt | ± 1% |
| AT-05 | R7 | VCT conservation (full heatup) | error < 10 gal |
| AT-06 | Stage 1 | Drain duration: realistic time | < 60 min for 100%→50% |
| AT-07 | Stage 2 | RVLIS stability: no spurious drops | < 1% drop |
| AT-08 | R6 | RCP start: no PZR level spike | < 0.5%/timestep |
| AT-09 | R7 | VCT conservation (4 hr steady-state) | error < 10 gal |
| AT-10 | R8 | SG boiling pressure rise when isolated | pressure > 17 psia initial |

**Key Observation:** All 10 tests currently pass by validating architectural rules (formula correctness), NOT by running actual simulations. Each test has a "REQUIRES SIMULATION" note indicating full end-to-end validation is pending.

- **Confidence:** High (code verified), but real simulation validation is incomplete

---

## 4. ALARM SETPOINT CHECKS (AlarmManager)

**File:** `AlarmManager.cs:82-193`
**Trigger:** Every timestep via `UpdateAnnunciators()` → `AlarmManager.CheckAlarms()`
**Edge Detection:** `HeatupSimEngine.Alarms.cs:82-209` (table-driven pattern, logs rising/falling edges)

| ID | Alarm | Condition | Setpoint | Unit | Suppression |
|----|-------|-----------|----------|------|-------------|
| ALM-01 | PZR Level Low | `PZRLevel < setpoint` | 20% | % | Suppressed during solid ops |
| ALM-02 | PZR Level High | `PZRLevel > setpoint` | 85% | % | Suppressed during solid ops |
| ALM-03 | Steam Bubble OK | `bubbleFormed && level ∈ (5%, 95%)` | 5%/95% | % | Status indicator |
| ALM-04 | RCS Flow Low | `RCPCount == 0` | 0 | count | None |
| ALM-05 | Pressure Low | `Pressure < setpoint` | 350 psia | psia | None |
| ALM-06 | Pressure High | `Pressure > setpoint` | 2300 psia | psia | None |
| ALM-07 | Subcooling Low | `subcooling < 30°F` | 30°F | °F | None |
| ALM-08 | SMM Low Margin | `subcooling < 15°F && > 0°F` | 15°F | °F | None |
| ALM-09 | SMM No Margin | `subcooling ≤ 0°F` | 0°F | °F | None |
| ALM-10 | RVLIS Level Low | `fullRangeValid && fullRange < 90%` | 90% | % | Only when full range valid |
| ALM-11 | Heatup In Progress | `heatupRate > 1°F/hr` | 1°F/hr | °F/hr | Status indicator |
| ALM-12 | Mode Permissive | `bubbleOK && !subcoolLow && P ≥ 350` | compound | — | Status indicator |
| ALM-13 | Seal Injection OK | `sealInj ≥ RCPCount × 7 gpm` | 7 gpm/RCP | gpm | OK if 0 RCPs |
| ALM-14 | VCT Level Low/High | via VCT state flags | per PlantConstants | % | Edge-detected in Alarms partial |

**Additional Edge-Detected Alarms** (Alarms.cs:92-180):
- VCT Makeup Active
- RWST Suction Activated
- Letdown Isolated (PZR level trigger)
- BRS Distillate Makeup

---

## 5. UI PASS/FAIL VALIDATION CHECKS

**File:** `HeatupValidationVisual.TabValidation.cs:158-242`
**Trigger:** Every GUI frame (OnGUI), displayed on Validation tab

| ID | Check Name | Condition | Threshold | Display |
|----|-----------|-----------|-----------|---------|
| UI-01 | Primary Mass Conservation | `massError_lbm` three-state | PASS<100, WARN 100-500, FAIL>500 lbm | Three-state (PASS/WARN/FAIL) |
| UI-02 | VCT Flow Imbalance | `massConservationError` three-state | PASS<10, WARN 10-50, FAIL>50 gal | Three-state |
| UI-03 | Heatup Rate ≤ 50 °F/hr | `|heatupRate| <= 50` | 50 °F/hr | Binary PASS/FAIL |
| UI-04 | Subcooling ≥ 15 °F | `subcooling >= 15` | 15 °F | Binary |
| UI-05 | PZR Level In Band | `|pzrLevel - setpoint| < 15` | ± 15% from setpoint | Binary |
| UI-06 | Pressure Rate Acceptable | `|pressureRate| < 200` | 200 psi/hr | Binary |
| UI-07 | Seal Injection OK | `sealInjectionOK` flag | per AlarmManager | Binary |
| UI-08 | Letdown Not Isolated | `!letdownIsolatedFlag` | boolean | Binary |
| UI-09 | VCT Level In Normal Band | `VCT_LOW ≤ level ≤ VCT_HIGH` | PlantConstants values | Binary |
| UI-10 | RVLIS Level OK | `!rvlisLevelLow` | per AlarmManager (90%) | Binary |
| UI-11 | RCPs Fully Ramped | `AllFullyRunning` | all started = fully ramped | Binary (only if RCPs > 0) |
| UI-12 | HZP Readiness (4 sub-checks) | T_avg, P, PZR level, all RCPs | per HZP controller | Binary (only if HZP active) |

**Memory/Performance Checks** (TabValidation.cs:117-151):

| Check | Threshold | Color |
|-------|-----------|-------|
| Reserved Memory | < 200 MB green, else amber | Warning indicator |
| Total Memory | < 100 MB green, < 300 MB amber, else red | Warning indicator |

---

## 6. GUARDS, CLAMPS & BOUNDS

### 6A. CoupledThermo Solver Bounds

**File:** `CoupledThermo.cs`

| Guard | Location | Value | Purpose |
|-------|----------|-------|---------|
| P_floor | SolveEquilibrium param | 15 psia (heatup) / 1800 psia (normal) | Minimum pressure bound |
| P_ceiling | SolveEquilibrium param | 2700 psia | Maximum pressure bound |
| MAX_ITERATIONS | CoupledThermo.cs:43 | 50 | Solver iteration limit |
| P_TOLERANCE | CoupledThermo.cs:46 | value in code | Pressure convergence criterion |
| V_TOLERANCE | CoupledThermo.cs:49 | value in code | Volume convergence criterion |
| PZR_STEAM_MIN | PlantConstants | per constants file | Minimum steam space volume |

### 6B. Heater Mode Transition Guard

**File:** `HeatupSimEngine.cs:859-861`
- **Guard:** `currentHeaterMode == PRESSURIZE_AUTO && pressure >= HEATER_MODE_TRANSITION_PRESSURE_PSIA`
- **Effect:** Transitions to AUTOMATIC_PID mode

### 6C. RHR Isolation Pressure Guard

**File:** `HeatupSimEngine.cs:972-976`
- **Guard:** `rcpCount > 0 && rhrState.Mode == RHRMode.Heatup`
- **Effect:** Initiates RHR isolation

### 6D. BRS Capacity Clamp

**File:** `BRSPhysics.cs:189`
- **Guard:** Clamp to usable capacity
- **Purpose:** Prevents holdup volume from exceeding tank capacity

### 6E. BRS Boron Concentration Guard

**File:** `BRSPhysics.cs:260`
- **Guard:** Guard against holdup boron exceeding concentrate target
- **Purpose:** Physical sanity bound

### 6F. PZR Level Low-Level Letdown Isolation

**File:** `HeatupSimEngine.cs:851`
- **Guard:** `pzrLevel < PZR_LOW_LEVEL_ISOLATION`
- **Effect:** Sets `letdownIsolatedForHeater` flag, isolates letdown to protect PZR

### 6G. Bubble Formation Phase Guards

**File:** `HeatupSimEngine.BubbleFormation.cs` (7-phase state machine)
- Multiple transition guards based on temperature, pressure, level, and timing conditions
- Key transitions: DETECTION (T_pzr ≥ T_sat), VERIFICATION (dwell timer), DRAIN (mass-based), STABILIZE, PRESSURIZE, COMPLETE

### 6H. Regime Selection Guard

**File:** `HeatupSimEngine.cs:1009, 1134, 1305`
- **Guard:** `α = min(1.0, totalFlowFraction)` selects physics regime
- Regime 1: α < 0.001 (line 1009)
- Regime 2: 0.001 ≤ α < 1.0, not all fully running (line 1134)
- Regime 3: all fully running (line 1305)

### 6I. Sim Time Budget Cap

**File:** `HeatupSimEngine.cs:674`
- **Guard:** `simTimeBudget = Mathf.Min(simTimeBudget, 5f/60f)` — caps at 5 sim-minutes per frame
- **Purpose:** Prevents runaway after alt-tab or lag spike

### 6J. T_sat Clamp

**File:** `HeatupSimEngine.cs:1522`
- **Guard:** `Mathf.Clamp(P, 14.7f, 3200f)` before calling SaturationTemperature
- **Purpose:** Prevents invalid pressure input to steam tables

---

## 7. DIAGNOSTIC LOGGING

### 7A. Startup Burst Logging

**File:** `HeatupSimEngine.cs:696-721`
- **Trigger:** Every 60 sim-seconds for the first 30 sim-minutes
- **Content:** Pressure, filtered pressure, setpoint, ΔP, control mode, PZR temp, heater power, CVCS flows
- **Format:** `[STARTUP T+{sec}s] P=... SP=... Mode=... Htr=... Chg=... Ltd=...`

### 7B. Regime 1 Heat Balance Debug

**File:** `HeatupSimEngine.cs:1124-1131`
- **Trigger:** First 0.02 hr + every 0.25 hr in Regime 1 (isolated PZR with bubble)
- **Content:** T_pzr, T_rcs, surge line convection, insulation loss, net heat
- **Format:** `[Phase1 Heat Balance] ...`

### 7C. VCT Conservation Diagnostic

**File:** `VCTPhysics.cs:373-389`
- **Trigger:** When VCT conservation error > 100 gal
- **Content:** All terms of conservation equation, suspects if rcsChange ≈ 0
- **Format:** `[VCT_CONS_DIAG] ERROR=... vctChange=... rcsChange=... externalNet=...`

### 7D. Primary Mass Alarm Log

**File:** `HeatupSimEngine.Logging.cs:560-579`
- **Trigger:** Edge-detected: OK→WARNING, OK→ALARM, ALARM→OK transitions
- **Content:** Drift lb, drift %, ledger value, component sum, expected value
- **Format:** `PRIMARY MASS CONSERVATION ALARM: drift=... | Ledger=... | Components=... | Expected=...`
- **NOTE:** Dead code (see CHECK-002)

### 7E. Interval Log Mass Conservation Line

**File:** `HeatupSimEngine.Logging.cs:1198-1199`
- **Trigger:** Every interval log (every 15 sim-minutes)
- **Content:** `Mass Conservation: {PASS|FAIL} ({massError_lbm} lbm)`
- **Threshold:** PASS if massError_lbm < 500

### 7F. RCP Start Event Log

**File:** `HeatupSimEngine.cs:803-804`
- **Trigger:** Each new RCP start
- **Content:** Pump number, T_pzr, T_rcs, ramp duration

### 7G. Heater Mode Transition Log

**File:** `HeatupSimEngine.cs:869-873`
- **Trigger:** PRESSURIZE_AUTO → AUTOMATIC_PID transition
- **Content:** Pressure at transition (psia/psig)

### 7H. Spray Activation Log

**File:** `HeatupSimEngine.cs:933-935`
- **Trigger:** First spray activation (edge-detected)
- **Content:** Pressure, valve position %

---

## CRITICAL FINDINGS FOR STAGE 3

### FINDING-1: UpdatePrimaryMassLedgerDiagnostics() is DEAD CODE

- **Evidence:** `HeatupSimEngine.Logging.cs:470` defines the method. Grep of entire `Assets/Scripts/` finds ZERO call sites. The method is referenced in a comment at `Init.cs:184` as if it runs, but it does not.
- **Impact:** The independent ledger-vs-components cross-check (which would detect solver drift between `TotalPrimaryMass_lb` and `RCSWaterMass + PZRWaterMass + PZRSteamMass`) is not executing. All display fields it would populate (`primaryMassStatus`, `primaryMassAlarm`, etc.) remain at their default values.
- **Severity:** HIGH — this is the most sensitive conservation diagnostic in the system.

### FINDING-2: Regime 2/3 V×ρ PZR Mass Overwrite Before Solver

- **Evidence:** `HeatupSimEngine.cs:1163-1165` and `:1317-1319`
- **Impact:** PZR water and steam masses are recomputed from V×ρ before the solver runs. If these don't exactly match the canonical ledger's allocation, the solver's remainder calculation (`RCS = Total - PZR_water - PZR_steam`) will absorb the error. This is a potential drift vector.
- **Severity:** MEDIUM — needs deep audit of CoupledThermo to see if the solver re-normalizes.

### FINDING-3: Acceptance Tests Are Architecture-Only, Not Simulation-Validated

- **Evidence:** All 10 tests in `AcceptanceTests_v5_4_0.cs` pass by checking formula correctness. Each has "REQUIRES SIMULATION" notes. No actual simulation run has validated these criteria end-to-end.
- **Impact:** The gate "All 10 tests must pass before changelog creation" is satisfied by mathematical validation, not runtime validation.
- **Severity:** MEDIUM — architecture is validated, but runtime behavior under actual simulation conditions is unverified by these tests.
