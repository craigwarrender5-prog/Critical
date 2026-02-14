# Implementation Plan v5.0.2 — Fix Solid Pressurizer Mass Conservation Drift

**Date:** 2026-02-11 (Final — Post-Implementation)  
**Version:** 5.0.2  
**Type:** PATCH — Correctness Fix (Mass Conservation)  
**Changelog:** CHANGELOG_v5.0.2.md  
**Priority:** CRITICAL — blocks all downstream PZR/inventory logic  

---

## Problem Summary

During solid pressurizer operations (cold shutdown through bubble formation), the inventory audit reports a monotonically growing mass conservation error (~6,000 lbm by 3.25 hr). This is not a display-only issue — it results from inconsistent mass accounting between flow-integrated state variables and density-recalculated audit fields.

### Root Cause (Verified)

Three independent mass recalculations overwrite or bypass conserved state:

| # | Location | Bug Pattern | Effect |
|---|----------|-------------|--------|
| 1 | `SolidPlantPressure.Update()` line ~277 | `state.PzrWaterMass = PZR_TOTAL_VOLUME × ρ(T_pzr, P)` | PZR mass decreases as T rises (density drops) without any flow |
| 2 | `PressurizerPhysics.SolidPressurizerUpdate()` line ~100 | `state.WaterMass = state.WaterVolume × ρ(T, P)` | Same pattern, reachable via guard clause |
| 3 | `HeatupSimEngine.Logging.cs — UpdateInventoryAudit()` lines ~293–305 | Both RCS and PZR mass recalculated from V×ρ | Audit independently recalculates masses, ignoring state. Two different accounting bases (V×ρ vs flow-integrated) diverge as temperature changes. |

### Pre-Implementation Verification Findings (Critical)

1. **`solidPlantState.PzrWaterMass` is never consumed by the engine or audit.** Fixing it alone would NOT fix audit drift.
2. **`state.SurgeFlow` is purely diagnostic** — never applied to any mass variable.
3. **The audit's RCS mass is ALSO recalculated from V×ρ** — separate drift source.
4. **`physicsState.RCSWaterMass` is loops-only (11,500 ft³ × ρ), NOT total primary.** Confirmed by `V_total = RCS_WATER_VOLUME + PZR_TOTAL_VOLUME` in `SolidPlantPressure.cs` line 380.
5. **`physicsState.PZRWaterMass` is stale during solid ops** — set once at init, never updated.

---

## Correct Behavior (Acceptance Criteria)

1. During solid PZR ops, mass must be **state-conserved** — changed only by explicit mass flows
2. Thermal expansion manifests as **internal surge transfer** (PZR → RCS) and **pressure change**, NOT mass creation/destruction
3. Inventory audit must use **state masses** during solid ops, not V×ρ recalculations
4. One canonical **total primary mass** variable, affected only by CVCS boundary flow
5. Loops mass derived implicitly: `Total − PZR` (never independently stored or updated)
6. Surge mass computed from single source (`dV_pzr_ft3`), same as `SurgeFlow`

---

## Design: Total-Primary with Derived Loops

### Semantic Model (Confirmed by Code Tracing)

```
physicsState.RCSWaterMass      = 11,500 × ρ  (loops-only, excludes PZR)
physicsState.PZRWaterMass      = 1,800 × ρ   (PZR-only)
Total primary at init          = 13,300 × ρ   (loops + PZR)
```

### Architecture

```
SolidPlantPressure.Update():
  surgeMass_lb = dV_pzr_ft3 × ρ_pzr         ← from same dV used for SurgeFlow
  PzrWaterMass -= surgeMass_lb               ← PZR loses mass via surge

Engine solid branch:
  PZRWaterMassSolid = solidPlantState.PzrWaterMass    ← sync conserved PZR mass
  TotalPrimaryMassSolid += CVCS_net_mass               ← boundary-only
  RCSWaterMass += CVCS_net_mass                        ← legacy, for two-phase transition

Inventory audit (solid ops):
  PZR_Mass = PZRWaterMassSolid                         ← state-based
  RCS_Mass = TotalPrimaryMassSolid - PZRWaterMassSolid ← derived loops
  (Two-phase: unchanged V×ρ approach)
```

### Mass Conservation Proof

- `TotalPrimaryMassSolid` initialized to `RCSWaterMass + PZRWaterMass` at t=0
- Only modified by `+= CVCS_net_mass` (boundary flow to/from VCT)
- Surge is internal: only `PZRWaterMassSolid` decreases; loops increase implicitly via `Total − PZR`
- Audit: `RCS + PZR = (Total − PZR) + PZR = Total` — exact identity ✓
- Total audit change = CVCS boundary only ✓
- No double-application of surge — applied in exactly one place (`SolidPlantPressure.Update()`)

---

## Implementation Stages (As Executed)

### Stage 1: `SolidPlantPressure.cs` (GOLD Module)

**Struct (`SolidPlantState`):** Added 4 diagnostic fields:
- `PzrDensity` — current PZR water density (lbm/ft³)
- `PzrVolumeImplied` — PzrWaterMass / PzrDensity (display only)
- `PzrMassFlowRate` — net mass flow rate from surge (lbm/hr)
- `SurgeMassTransfer_lb` — mass transferred PZR→RCS this step (lbm)

**`Initialize()`:** New fields initialized to zero/initial values.

**`Update()` Section 1 (thermal capacity):** Replaced `pzrWaterMass = PZR_TOTAL_VOLUME × rho_pzr` with `pzrWaterMassForCp = state.PzrWaterMass` (conserved mass). Removed the V×ρ mass overwrite.

**`Update()` Section 7 (diagnostics):** Added surge-based mass conservation block after `SurgeFlow` calculation. Both `SurgeFlow` and `surgeMass_lb` derive from same `dV_pzr_ft3` (single source):
```csharp
float rho_pzr_post = WaterProperties.WaterDensity(state.T_pzr, state.Pressure);
float surgeMass_lb = dV_pzr_ft3 * rho_pzr_post;
state.PzrWaterMass -= surgeMass_lb;
state.SurgeMassTransfer_lb = surgeMass_lb;
```

**Validation Test 11:** Verifies mass delta matches cumulative surge transfer within 1 lbm, and total change < 500 lbm over 100 steps.

### Stage 2: `PressurizerPhysics.cs` (GOLD Module)

**`SolidPressurizerUpdate()`:** Removed `state.WaterMass = state.WaterVolume × WaterProperties.WaterDensity(...)`. This method has no flow inputs — mass is invariant here.

**Validation Test 14:** Verifies `WaterMass` drift < 0.01 lbm after 50 update calls.

### Stage 3: `SystemState` struct + Engine Solid Branch

**3a. `CoupledThermo.cs` — `SystemState` struct:**
- Added `TotalPrimaryMassSolid` (total primary mass, boundary-only)
- Added `PZRWaterMassSolid` (conserved PZR mass from SolidPlantPressure)

**3b. `HeatupSimEngine.Init.cs`:**
- Cold start: `TotalPrimaryMassSolid = rcsWaterMass + pzrWaterMass`
- Cold start: `PZRWaterMassSolid = pzrWaterMass`
- Hot start: both zeroed (not used during two-phase ops)

**3c. `HeatupSimEngine.cs` — Solid branch (~line 1070):**
- `PZRWaterMassSolid` synced from `solidPlantState.PzrWaterMass`
- `TotalPrimaryMassSolid += massChange_lb` (CVCS boundary only)
- Legacy `RCSWaterMass` still updated by CVCS for two-phase transition compatibility

### Stage 4: `HeatupSimEngine.Logging.cs`

**`UpdateInventoryAudit()`:** Added `if (solidPressurizer && !bubbleFormed)` branch:
- `PZR_Water_Mass_lbm = physicsState.PZRWaterMassSolid`
- `RCS_Mass_lbm = TotalPrimaryMassSolid − PZRWaterMassSolid`
- `PZR_Steam_Mass_lbm = 0` (solid = no steam)
- Two-phase `else` branch preserves original V×ρ (unchanged)

**`SaveIntervalLog()`:** Added conditional forensic block during solid ops showing TotalPrimaryMass, PZR conserved mass, derived loops, density, implied volume, mass flow rate, and surge transfer per step.

### Stage 5: Validation & Documentation

- Updated implementation plan to reflect final design
- Created changelog (CHANGELOG_v5.0.2.md)
- Validation criteria documented below

---

## Validation Criteria

| # | Criterion | Threshold |
|---|-----------|-----------|
| 1 | PZR mass does NOT drift monotonically during solid ops | Δm < 500 lbm total over 3+ hr (absent net outflow) |
| 2 | Inventory audit total mass error NOT monotonically growing | Error bounded, direction reverses |
| 3 | Mass change matches CVCS boundary flow | Within ±100 lbm |
| 4 | Surge transfer NOT double-applied | PZR decrease implicit via Total−PZR; no explicit RCS surge addition |
| 5 | Post-bubble two-phase behavior unchanged | No regression in Regime 2/3 |
| 6 | Validation Tests 11 (SolidPlantPressure) and 14 (PressurizerPhysics) pass | Green |

---

## Files Modified

| File | Stage | Changes |
|------|-------|---------|
| `SolidPlantPressure.cs` | 1 | Remove V×ρ overwrite, surge-based conservation, 4 diagnostic fields, Test 11 |
| `PressurizerPhysics.cs` | 2 | Remove V×ρ overwrite in SolidPressurizerUpdate, Test 14 |
| `CoupledThermo.cs` | 3a | Add `TotalPrimaryMassSolid`, `PZRWaterMassSolid` to SystemState |
| `HeatupSimEngine.Init.cs` | 3b | Initialize new fields (cold + hot start) |
| `HeatupSimEngine.cs` | 3c | Sync PZR mass, apply CVCS to TotalPrimaryMassSolid |
| `HeatupSimEngine.Logging.cs` | 4 | State-based audit branch for solid ops, forensic logging |

---

## Unaddressed Issues (Deferred)

| Issue | Disposition | Target |
|-------|-------------|--------|
| Reverse transition (two-phase → solid) | Not applicable to startup | v5.5.0 cooldown |
| Full volume-first inventory architecture | Planned per roadmap | v5.1.0 |
| Audit RCS V×ρ drift in two-phase | V×ρ is standard for fixed geometry; monitor | v5.1.0 if needed |

---

## Risk Assessment

**Low risk.** No changes to pressure calculation, temperature calculation, CVCS controller logic, bubble formation detection, VCT/BRS tracking, or any post-bubble (Regime 2/3) physics. All changes narrowly scoped to mass accounting during solid ops only.
