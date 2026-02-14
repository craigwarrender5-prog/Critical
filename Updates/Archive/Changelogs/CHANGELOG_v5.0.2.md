# Changelog v5.0.2 — Fix Solid Pressurizer Mass Conservation Drift

**Date:** 2026-02-11  
**Version:** 5.0.2  
**Type:** PATCH — Correctness Fix (Mass Conservation)  
**Implementation Plan:** IMPLEMENTATION_PLAN_v5.0.2.md  

---

## Summary

Fixed monotonically growing mass conservation error (~6,000 lbm by 3.25 hr) during solid pressurizer operations. Root cause: three independent V×ρ recalculations overwrote or bypassed flow-conserved mass state, creating phantom mass loss as temperature increased and density decreased.

Replaced with state-based mass accounting: one canonical `TotalPrimaryMassSolid` variable updated only by CVCS boundary flow, with PZR mass tracked separately via surge transfer. Loops mass derived implicitly as `Total − PZR`, guaranteeing exact conservation by construction.

---

## Changes

### `SolidPlantPressure.cs` (GOLD Module)

#### Fixed
- **Removed V×ρ mass overwrite in `Update()`** — `state.PzrWaterMass` was being reset to `PZR_TOTAL_VOLUME × ρ(T_pzr, P)` every timestep, destroying flow-conserved mass. Now updated only by surge transfer: `state.PzrWaterMass -= surgeMass_lb`
- **Fixed thermal capacity calculation** — was using recalculated `pzrWaterMass = V × ρ` for heat capacity; now uses conserved `state.PzrWaterMass`

#### Added
- **Surge-based mass conservation** in Section 7 of `Update()` — `surgeMass_lb = dV_pzr_ft3 × ρ_pzr_post`, derived from same `dV_pzr_ft3` used for `SurgeFlow` (single computation source, no double-calculation)
- **4 diagnostic fields** on `SolidPlantState`: `PzrDensity`, `PzrVolumeImplied`, `PzrMassFlowRate`, `SurgeMassTransfer_lb`
- **Validation Test 11** — mass delta matches cumulative surge transfer within 1 lbm; total drift < 500 lbm

### `PressurizerPhysics.cs` (GOLD Module)

#### Fixed
- **Removed V×ρ mass overwrite in `SolidPressurizerUpdate()`** — `state.WaterMass` was being reset from `state.WaterVolume × ρ(T, P)` every call. This method has no flow inputs, so mass must be invariant.

#### Added
- **Validation Test 14** — `WaterMass` drift < 0.01 lbm after 50 update calls

### `CoupledThermo.cs` (GOLD Module)

#### Added
- **`TotalPrimaryMassSolid`** field on `SystemState` — total primary mass (loops + PZR), updated only by CVCS boundary flow during solid ops
- **`PZRWaterMassSolid`** field on `SystemState` — conserved PZR mass synced from `SolidPlantPressure`

### `HeatupSimEngine.Init.cs` (GOLD Module)

#### Added
- Cold start initialization: `TotalPrimaryMassSolid = rcsWaterMass + pzrWaterMass`, `PZRWaterMassSolid = pzrWaterMass`
- Hot start initialization: both zeroed (unused during two-phase ops)

### `HeatupSimEngine.cs` (GOLD Module)

#### Changed
- **Engine solid branch** (~line 1070): added `PZRWaterMassSolid` sync from `solidPlantState.PzrWaterMass` and `TotalPrimaryMassSolid += massChange_lb` (CVCS boundary). Legacy `RCSWaterMass` still updated for two-phase transition compatibility.

### `HeatupSimEngine.Logging.cs` (GOLD Module)

#### Changed
- **`UpdateInventoryAudit()`**: added `if (solidPressurizer && !bubbleFormed)` branch using state-based conserved masses. `PZR = PZRWaterMassSolid`, `RCS = TotalPrimaryMassSolid − PZRWaterMassSolid`. Two-phase `else` branch unchanged.

#### Added
- **Forensic logging** in `SaveIntervalLog()`: conditional block during solid ops showing TotalPrimaryMass, conserved PZR mass, derived loops, density, implied volume, mass flow rate, surge transfer per step.

---

## Not Changed

- Pressure calculation (dP/dt from thermal expansion vs CVCS removal)
- Temperature calculation (PZR heater, surge line heat, RCS heat balance)
- CVCS controller logic (PI controller, letdown/charging flow balance)
- Bubble formation detection or transition logic
- VCT/BRS mass tracking
- Two-phase (Regime 2/3) physics — audit V×ρ approach unchanged for post-bubble
- SurgeFlow diagnostic field — still computed same way, still display-only

---

## Validation Criteria

| # | Criterion | Threshold |
|---|-----------|-----------|
| 1 | PZR mass not monotonically drifting | < 500 lbm total over 3+ hr |
| 2 | Audit total mass error bounded | Not monotonic |
| 3 | Mass change matches CVCS boundary | ±100 lbm |
| 4 | No double-application of surge | Verified by design (single source) |
| 5 | Post-bubble regression | None |
| 6 | Tests 11 and 14 pass | Green |
