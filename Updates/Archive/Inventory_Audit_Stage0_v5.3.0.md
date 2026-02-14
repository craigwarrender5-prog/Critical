# Inventory Audit Stage 0 — Primary Mass Modification Locations

**Version:** v5.3.0  
**Date:** 2026-02-12  
**Stage:** 0 — Preflight / Baseline (Documentation Only)  
**Status:** COMPLETE

---

## Table of Contents

1. [Structured Audit List (Grouped by File)](#1-structured-audit-list-grouped-by-file)
   - [SystemState.cs](#file-systemstatecs-struct-in-coupledthermocs)
   - [CoupledThermo.cs](#file-coupledthermocs)
   - [HeatupSimEngine.cs](#file-heatupsimenginecs-main-file)
   - [HeatupSimEngine.CVCS.cs](#file-heatupsimenginecvcscs)
   - [HeatupSimEngine.Init.cs](#file-heatupsimengineinitcs)
   - [SolidPlantPressure.cs](#file-solidplantpressurecs)
   - [RCSHeatup.cs](#file-rcsheatupcs)
2. [Current Behavior Summary](#2-current-behavior-summary)
3. [Expected Stage 0 Failure Cases (AT-1 to AT-4)](#3-expected-stage-0-failure-cases-at-1-to-at-4)
4. [Summary of Findings](#4-summary-of-findings)
5. [Stage 0 Conclusion](#5-stage-0-conclusion)

---

## 1. Structured Audit List (Grouped by File)

---

### File: `SystemState.cs` (struct in CoupledThermo.cs)

| Line | Method/Context | Code Snippet | Description |
|------|----------------|--------------|-------------|
| N/A (struct) | Property | `public float RCSWaterMass;` | Stores RCS water mass in lbm |
| N/A (struct) | Property | `public float PZRWaterMass;` | Stores PZR water mass in lbm |
| N/A (struct) | Property | `public float PZRSteamMass;` | Stores PZR steam mass in lbm |
| N/A (struct) | Property | `public float TotalPrimaryMassSolid;` | v5.0.2: Canonical total primary mass for solid ops |
| N/A (struct) | Property | `public float PZRWaterMassSolid;` | v5.0.2: Conserved PZR mass from SolidPlantPressure |
| N/A (struct) | Property | `public float TotalMass => RCSWaterMass + PZRWaterMass + PZRSteamMass;` | Derived property (V×ρ sum) |

---

### File: `CoupledThermo.cs`

| Line | Method | Code Snippet | Description |
|------|--------|--------------|-------------|
| ~92 | `SolveEquilibrium()` | `float M_total = state.RCSWaterMass + state.PZRWaterMass + state.PZRSteamMass;` | Initial total mass read (for conservation target) |
| ~141-147 | `SolveEquilibrium()` | `state.RCSWaterMass = V_RCS * rho_RCS;` | **V×ρ OVERWRITE — RCS mass derived from volume × density** |
| ~149-153 | `SolveEquilibrium()` | `state.PZRWaterVolume = (M_PZR_total - V_PZR_total * rho_PZR_steam) / (rho_PZR_water - rho_PZR_steam);` | PZR water volume calculated |
| ~155-156 | `SolveEquilibrium()` | `state.PZRWaterMass = state.PZRWaterVolume * rho_PZR_water;` | **V×ρ ASSIGNMENT — PZR water mass from volume × density** |
| ~157 | `SolveEquilibrium()` | `state.PZRSteamMass = state.PZRSteamVolume * rho_PZR_steam;` | **V×ρ ASSIGNMENT — PZR steam mass from volume × density** |
| ~298-307 | `InitializeAtSteadyState()` | `state.RCSWaterMass = state.RCSVolume * rhoRCS;` | Initialization via V×ρ |
| ~300-307 | `InitializeAtSteadyState()` | `state.PZRWaterMass = state.PZRWaterVolume * rhoPZRWater;` | Initialization via V×ρ |
| ~300-307 | `InitializeAtSteadyState()` | `state.PZRSteamMass = state.PZRSteamVolume * rhoPZRSteam;` | Initialization via V×ρ |
| ~333-343 | `InitializeAtHeatupConditions()` | `state.RCSWaterMass = state.RCSVolume * rhoRCS;` | Initialization via V×ρ |
| ~345-347 | `InitializeAtHeatupConditions()` | `state.PZRWaterMass = state.PZRWaterVolume * rhoPZRWater;` | Initialization via V×ρ |
| ~348 | `InitializeAtHeatupConditions()` | `state.PZRSteamMass = state.PZRSteamVolume * rhoPZRSteam;` | Initialization via V×ρ |

**CRITICAL FINDING:** `SolveEquilibrium()` at lines ~141-157 overwrites `RCSWaterMass`, `PZRWaterMass`, and `PZRSteamMass` using V×ρ calculations. This is the location where CVCS mass adjustments applied prior to the solver are **LOST**.

---

### File: `HeatupSimEngine.cs` (Main File)

| Line | Method | Code Snippet | Description |
|------|--------|--------------|-------------|
| ~615-617 | `StepSimulation()` Regime 1 Solid Ops | `physicsState.PZRWaterMassSolid = solidPlantState.PzrWaterMass;` | Sync PZR conserved mass from SolidPlantPressure |
| ~620-625 | `StepSimulation()` Regime 1 Solid Ops | `physicsState.TotalPrimaryMassSolid += massChange_lb;` | **CVCS boundary += to canonical ledger** (CORRECT v5.0.2 pattern) |
| ~628 | `StepSimulation()` Regime 1 Solid Ops | `physicsState.RCSWaterMass += massChange_lb;` | Legacy RCS mass update for transition |
| ~738-740 | `StepSimulation()` Regime 2 | `physicsState.PZRWaterMass = pzrWaterVolume * WaterProperties.WaterDensity(tSatForMass, pressure);` | V×ρ sync before solver |
| ~741 | `StepSimulation()` Regime 2 | `physicsState.PZRSteamMass = pzrSteamVolume * WaterProperties.SaturatedSteamDensity(pressure);` | V×ρ sync before solver |
| ~754-758 | `StepSimulation()` Regime 2 | `physicsState.RCSWaterMass += cvcsNetMass_lb_r2;` | **CVCS mass += BEFORE solver** |
| ~773-778 | `StepSimulation()` Regime 2 | `physicsState.PZRSteamMass -= sprayState.SteamCondensed_lbm;` | Spray condensation mass transfer |
| ~774 | `StepSimulation()` Regime 2 | `physicsState.PZRWaterMass += sprayState.SteamCondensed_lbm;` | Spray condensation mass transfer |
| ~865-868 | `StepSimulation()` Regime 3 | `physicsState.PZRWaterMass = pzrWaterVolume * WaterProperties.WaterDensity(tSatForMass3, pressure);` | V×ρ sync before solver |
| ~869 | `StepSimulation()` Regime 3 | `physicsState.PZRSteamMass = pzrSteamVolume * WaterProperties.SaturatedSteamDensity(pressure);` | V×ρ sync before solver |
| ~885-889 | `StepSimulation()` Regime 3 | `physicsState.RCSWaterMass += cvcsNetMass_lb;` | **CVCS mass += BEFORE solver** |
| ~906-911 | `StepSimulation()` Regime 3 | `physicsState.PZRSteamMass -= sprayState.SteamCondensed_lbm;` | Spray condensation |
| ~907 | `StepSimulation()` Regime 3 | `physicsState.PZRWaterMass += sprayState.SteamCondensed_lbm;` | Spray condensation |

---

### File: `HeatupSimEngine.CVCS.cs`

| Line | Method | Code Snippet | Description |
|------|--------|--------------|-------------|
| ~127-139 | `UpdateRCSInventory()` | `physicsState.RCSWaterMass += massChange_lb;` | **CVCS mass += to RCS** (when `regime3CVCSPreApplied = false`) |
| ~120-122 | `UpdateRCSInventory()` | Early return if `regime3CVCSPreApplied = true` | Skip to avoid double-counting |

---

### File: `HeatupSimEngine.Init.cs`

| Line | Method | Code Snippet | Description |
|------|--------|--------------|-------------|
| ~108-110 | `InitializeColdShutdown()` | `rcsWaterMass = PlantConstants.RCS_WATER_VOLUME * rhoWater;` | Initial V×ρ calculation |
| ~114 | `InitializeColdShutdown()` | `float pzrWaterMass = pzrWaterVolume * rhoWater;` | Initial V×ρ calculation |
| ~125-128 | `InitializeColdShutdown()` | `physicsState.RCSWaterMass = rcsWaterMass;` | Assignment to state |
| ~129-130 | `InitializeColdShutdown()` | `physicsState.PZRWaterMass = pzrWaterMass;` | Assignment to state |
| ~134-135 | `InitializeColdShutdown()` | `physicsState.TotalPrimaryMassSolid = rcsWaterMass + pzrWaterMass;` | Initialize canonical ledger |
| ~136 | `InitializeColdShutdown()` | `physicsState.PZRWaterMassSolid = pzrWaterMass;` | Initialize PZR conserved mass |
| ~172-173 | `InitializeWarmStart()` | `physicsState.RCSWaterMass = physicsState.RCSVolume * rcsRho;` | V×ρ initialization |
| ~181-183 | `InitializeWarmStart()` | `physicsState.PZRWaterMass = physicsState.PZRWaterVolume * pzrRhoWater;` | V×ρ initialization |
| ~184 | `InitializeWarmStart()` | `physicsState.PZRSteamMass = physicsState.PZRSteamVolume * pzrRhoSteam;` | V×ρ initialization |
| ~187-188 | `InitializeWarmStart()` | `physicsState.TotalPrimaryMassSolid = 0f;` | Not used in two-phase |

---

### File: `SolidPlantPressure.cs`

| Line | Method | Code Snippet | Description |
|------|--------|--------------|-------------|
| ~97-99 | `Initialize()` | `state.PzrWaterMass = PlantConstants.PZR_TOTAL_VOLUME * rho;` | Initial V×ρ for PZR |
| ~262-265 | `Update()` | `state.PzrWaterMass -= surgeMass_lb;` | **Conserved mass -= by surge transfer only** (v5.0.2 correct pattern) |
| ~176-178 | `CalculateReliefFlow()` | Returns `float` flow in gpm | Relief flow CALCULATED but **NOT applied to mass anywhere** |

---

### File: `RCSHeatup.cs`

| Line | Method | Code Snippet | Description |
|------|--------|--------------|-------------|
| ~97 | `BulkHeatupStep()` | Calls `CoupledThermo.SolveEquilibrium(ref state, deltaT)` | **Triggers the V×ρ overwrite in CoupledThermo** |

---

## 2. Current Behavior Summary

### What Happens to a 10 lbm Net Letdown in Solid Ops?
**EXPECTED: TRACKED** ✓
- In Regime 1 (solid ops), CVCS boundary flow updates `physicsState.TotalPrimaryMassSolid += massChange_lb` at line ~620-625 of `HeatupSimEngine.cs`
- The canonical ledger is updated; mass is conserved by construction
- `SolidPlantPressure.Update()` does NOT overwrite mass via V×ρ (v5.0.2 fix)
- **Result: Mass conservation is correct during solid operations**

### What Happens to a 10 lbm Net Letdown in Two-Phase (Regime 2/3)?
**EXPECTED: LOST** ✗
1. CVCS flow is applied BEFORE solver: `physicsState.RCSWaterMass += cvcsNetMass_lb` (lines ~754, ~885)
2. `RCSHeatup.BulkHeatupStep()` is called, which calls `CoupledThermo.SolveEquilibrium()`
3. **CoupledThermo.SolveEquilibrium() at line ~141 overwrites:** `state.RCSWaterMass = V_RCS * rho_RCS`
4. The CVCS adjustment is **COMPLETELY ERASED** — the solver derives mass from V×ρ, ignoring boundary flows
5. There is **no `TotalPrimaryMass_lb` ledger** that persists through two-phase operations
6. **Result: CVCS mass changes are systematically lost every timestep**

### What Happens When Relief Opens?
**EXPECTED: FLOW COMPUTED, MASS NOT REMOVED** ✗
1. `SolidPlantPressure.CalculateReliefFlow()` calculates relief flow in gpm
2. Relief flow is stored in `state.ReliefFlow` and used in CVCS calculations
3. **However:** There is NO code that subtracts relief mass from `TotalPrimaryMassSolid` or any other mass ledger
4. Relief flow contributes to `netCVCS_gpm` calculation (line ~230 in `SolidPlantPressure.Update()`): `float netCVCS_gpm = state.LetdownFlow - state.ChargingFlow + state.ReliefFlow;`
5. But `netCVCS_gpm` affects pressure via volume balance, NOT mass accounting
6. **Result: Relief removes water from the system pressure-wise but mass is NOT actually debited from the ledger**

### Where Does CoupledThermo Overwrite RCS Mass?
**CONFIRMED: Lines ~141-147 in `CoupledThermo.SolveEquilibrium()`**
```csharp
// Line ~141
state.RCSWaterMass = V_RCS * rho_RCS;
// Line ~155
state.PZRWaterMass = state.PZRWaterVolume * rho_PZR_water;
// Line ~157  
state.PZRSteamMass = state.PZRSteamVolume * rho_PZR_steam;
```
All three component masses are derived from V×ρ. The CVCS adjustment applied just before the solver call is completely overwritten.

### Where Is CVCS Mass Applied Before Being Overwritten?
**CONFIRMED:**
- **Regime 2:** Line ~754: `physicsState.RCSWaterMass += cvcsNetMass_lb_r2;`
- **Regime 3:** Line ~885: `physicsState.RCSWaterMass += cvcsNetMass_lb;`
- Both are immediately followed by the `BulkHeatupStep()` call which triggers `SolveEquilibrium()` → **overwrite**

### Is Relief Flow Applied Anywhere to Mass?
**CONFIRMED: NO**
- `SolidPlantPressure.CalculateReliefFlow()` returns the flow rate
- The flow is used in pressure/volume calculations only
- **No code exists that does:** `TotalPrimaryMass -= reliefMass_lb` or equivalent
- Search of entire codebase confirms: relief mass removal is **not implemented**

### Is `GetChargingToRCS()` Used in Boundary Mass Calculations?
**CONFIRMED: NO (not in boundary calculations)**
- `CVCSController.GetChargingToRCS()` exists (line ~674 in CVCSController.cs): `return Math.Max(0f, state.ChargingFlow - state.SealInjection);`
- Used in `HeatupSimEngine.CVCS.cs` line ~211: `chargingToRCS = Mathf.Max(0f, chargingFlow - sealInjection);`
- But this is for **display purposes** only
- The actual CVCS mass calculation uses raw `chargingFlow - letdownFlow`:
  - Line ~885: `float netCVCS_gpm = chargingFlow - letdownFlow;`
- **Seal injection is NOT factored out of the boundary mass calculation**
- This may cause the seal flow double-count issue (Issue #4)

---

## 3. Expected Stage 0 Failure Cases (AT-1 to AT-4)

### AT-1: Two-Phase CVCS Step Test
**EXPECTED: FAIL**
- **Test:** Set net letdown -15 gpm for 10 minutes in two-phase
- **Expected mass loss:** ~1,250 lb
- **Actual behavior:** `TotalPrimaryMass_lb` does not exist; `RCSWaterMass` is overwritten by V×ρ every timestep
- **Failure mechanism:** CVCS mass adjustment lost in `CoupledThermo.SolveEquilibrium()` overwrite
- **Outcome:** Mass will NOT decrease by expected amount

### AT-2: No-Flow Drift Test in Two-Phase
**EXPECTED: FAIL**
- **Test:** Set charging = letdown (net 0) for 4+ hours in two-phase
- **Expected drift:** < 0.01% (~60 lb)
- **Actual behavior:** V×ρ recalculation every timestep introduces cumulative drift from steam table precision limits
- **Failure mechanism:** Mass derived from `V × ρ(T,P)` accumulates small errors over many iterations
- **Outcome:** Drift likely exceeds 0.01% over long simulation

### AT-3: Solid→Two-Phase Transition Continuity
**EXPECTED: FAIL**
- **Test:** Observe mass continuity at bubble formation
- **Expected:** `TotalPrimaryMass_lb` equals `TotalPrimaryMassSolid` within 1 lbm
- **Actual behavior:** `TotalPrimaryMass_lb` does not exist; no handoff mechanism
- **Failure mechanism:** Solid-ops ledger is abandoned; two-phase uses V×ρ derivation
- **Outcome:** No continuity guaranteed; cumulative CVCS history is lost

### AT-4: Relief Open Test
**EXPECTED: FAIL**
- **Test:** Force relief valve open scenario
- **Expected:** Mass decreases by ∫ṁ_relief dt
- **Actual behavior:** Relief flow is calculated but never subtracted from any mass ledger
- **Failure mechanism:** `CalculateReliefFlow()` result used for pressure only, not mass accounting
- **Outcome:** Mass will NOT decrease when relief opens

---

## 4. Summary of Findings

| Issue | Location | Status |
|-------|----------|--------|
| **CVCS mass overwritten by V×ρ** | `CoupledThermo.SolveEquilibrium()` lines ~141-157 | **CONFIRMED** |
| **No canonical two-phase mass ledger** | `SystemState` struct, `HeatupSimEngine.cs` | **CONFIRMED** |
| **Relief flow not applied to mass** | `SolidPlantPressure.cs`, no code to debit | **CONFIRMED** |
| **Seal flow boundary calculation unclear** | `HeatupSimEngine.cs` line ~885 uses raw flows | **NEEDS AUDIT** in Stage 5 |
| **Solid-ops pattern is CORRECT (v5.0.2)** | `HeatupSimEngine.cs` lines ~615-628, `SolidPlantPressure.cs` | **WORKING** |

---

## 5. Stage 0 Conclusion

The Stage 0 audit confirms the following critical issues with primary mass conservation in two-phase operations:

1. **CoupledThermo V×ρ Overwrite Erases CVCS Adjustments:** The `SolveEquilibrium()` method in `CoupledThermo.cs` (lines ~141-157) unconditionally overwrites `RCSWaterMass`, `PZRWaterMass`, and `PZRSteamMass` using volume × density calculations. Any CVCS boundary flow adjustments applied before the solver call are completely lost.

2. **Relief Flow Not Debited from Mass Ledger:** The `CalculateReliefFlow()` function in `SolidPlantPressure.cs` computes relief valve flow rates, but this flow is only used for pressure/volume calculations. No code exists to subtract relief mass from any primary mass ledger. When relief opens, mass is not actually removed from the system.

3. **Two-Phase Has No Canonical Mass Ledger:** The solid-ops pattern (v5.0.2) correctly uses `TotalPrimaryMassSolid` as a canonical ledger updated only by boundary flows. However, this pattern was NOT extended to two-phase operations. When the bubble forms and the system transitions to Regime 2/3, there is no `TotalPrimaryMass_lb` variable to maintain continuity. The cumulative CVCS history is lost.

4. **Seal Flow Boundary Definition Likely Incorrect:** The CVCS mass calculation at line ~885 uses `chargingFlow - letdownFlow` directly, without factoring out seal injection. The 5 gpm/pump seal return to RCS is internal (not a boundary crossing), but the current code may be double-counting it. This issue will be addressed in Stage 5.

**The solid-ops mass conservation pattern (v5.0.2) is architecturally correct and should be extended to two-phase operations in Stages 1-3.**

---

*Stage 0 Audit Complete — 2026-02-12*  
*Ready for Stage 1 Implementation*
