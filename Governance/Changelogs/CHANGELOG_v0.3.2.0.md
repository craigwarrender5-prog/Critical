# CHANGELOG v0.3.2.0
## Pressurizer Energy Path Correction (CS-0043)

**Date:** 2026-02-14
**Type:** Patch — Critical defect correction within bubble formation domain
**Authorization:** `AUTHORIZED TO IMPLEMENT: CS-0043 corrective fix`
**Implementation Plan:** IP-0014

---

## Summary

Corrects a critical energy double-counting defect (CS-0043) that caused runaway depressurization during bubble formation DRAIN phase. Pressurizer pressure collapsed from ~368 psia to ~154 psia over 2 hours despite 1.8 MW heaters at full power. The root cause was heater energy being consumed twice per timestep: once as sensible heat in `IsolatedHeatingStep` (capped and discarded by T_sat), and again as latent heat in `UpdateDrainPhase` (steam generation). A T_sat cap combined with Psat(T_pzr) override created a one-way pressure ratchet.

## Changes

### RCSHeatup.cs — Two-Phase Bypass in IsolatedHeatingStep

- **Added** `twoPhaseActive` parameter (default `false`) to `IsolatedHeatingStep()`
- **Added** two-phase bypass path: when `twoPhaseActive = true`, PZR sensible heat computation (dT = Q/mCp) is skipped entirely
  - T_pzr locked to T_sat(P) — PZR water at saturation
  - Pressure returned unchanged — no subcooled thermal expansion
  - Surge flow returned as zero — no thermal expansion
- **Added** `PZRConductionLoss_MW` and `PZRInsulationLoss_MW` fields to `IsolatedHeatingResult`
  - PZR losses computed in both paths (subcooled and two-phase)
  - Reported to engine for downstream energy accounting in UpdateDrainPhase
- RCS temperature update runs normally in both paths (surge line conduction still active)
- Subcooled path (twoPhaseActive = false) completely unchanged

### HeatupSimEngine.cs — Regime 1 Energy Routing

- **Moved** `twoPhaseActive` flag computation before `IsolatedHeatingStep` call (was after)
- **Passes** `twoPhaseActive` flag to `IsolatedHeatingStep`
- **Stores** PZR loss values from result for downstream use by `UpdateDrainPhase`
- **Removed** Psat(T_pzr) override block (was v0.3.0.0 Fix 3.2, lines 1134-1148)
  - This override was the ratchet mechanism: conduction/insulation losses pulled T_pzr below T_sat(P_old), so Psat(T_pzr) < P_old, locking in monotonic decline
  - With the two-phase bypass, T_pzr = T_sat(P), so Psat(T_sat(P)) = P (identity — no ratchet)
- Updated order-of-operations comments to reflect new pipeline
- Added `pzrConductionLoss_MW` and `pzrInsulationLoss_MW` private instance fields

### HeatupSimEngine.BubbleFormation.cs — Energy-Conserving Steam Generation

- **Modified** `UpdateDrainPhase()` steam generation to use NET heater power
  - `netHeaterPower = grossHeaterPower - conductionLoss - insulationLoss`
  - Steam generation rate now: `steamGenRate = max(0, netPower_BTU_sec / h_fg)`
- This is now the SOLE consumer of heater energy during two-phase (no double-counting)
- Energy conservation ensured: heater power minus losses = energy available for boiling

## Not Modified

| File | Reason |
|------|--------|
| `PressurizerPhysics.cs` | `TwoPhaseHeatingUpdate` not called during Regime 1 |
| `PlantConstants.Pressurizer.cs` | No new constants needed |
| `SolidPlantPressure.cs` | Pre-bubble path, unaffected |
| `CVCSController.cs` | CVCS flow balance unaffected |
| Regime 2/3 paths | Regime 2 call uses default `twoPhaseActive = false`; Regime 3 doesn't call `IsolatedHeatingStep` |

## Issues Addressed

| Issue | Status |
|-------|--------|
| **CS-0043** | Fixed — Pressure boundary collapse during bubble formation eliminated |
| **CS-0036** | Potentially improved — Excessive DRAIN duration likely caused by same energy double-counting |

## Physics Rationale

In a real PWR pressurizer during bubble formation:
- Heaters maintain saturation conditions at constant T_sat
- Heater energy goes to latent heat (boiling liquid into steam), not sensible heat (temperature rise)
- Pressure is governed by steam mass balance, not liquid thermal expansion
- Conduction and insulation losses reduce the energy available for boiling

The previous code applied a subcooled liquid model (dT = Q/mCp) to a two-phase system, then independently consumed the same energy for steam generation. This violated the first law of thermodynamics (energy conservation).

## Validation Criteria

After fix, Stage E long-run should show:
1. Pressure stable or slowly rising during DRAIN (not collapsing)
2. T_pzr tracking T_sat(P) during two-phase
3. Mass conservation maintained (system mass delta < 0.1 lbm/step)
4. DRAIN completes within reasonable time (~30-60 min)
5. No regression in Regime 0 (pre-bubble behavior identical)
