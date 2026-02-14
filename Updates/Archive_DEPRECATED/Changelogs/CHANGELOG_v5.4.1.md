# CHANGELOG v5.4.1 — PZR Drain Mass Spike Fix + Mass-Based Inventory Validation

**Date:** 2026-02-13
**Version:** 5.4.1
**Type:** Critical Bug Fix (Physics Conservation)
**Matching Implementation Plan:** IMPLEMENTATION_PLAN_v5.4.1.md

---

## Problem Summary

Two conservation violations in the pressurizer bubble formation and inventory audit systems:

**A) PZR DRAIN/STABILIZE Mass Spike (3,755 lbm transient)**
During the DRAIN phase of bubble formation, the simulation recalculated PZR water and steam masses from volumes using `mass = volume * density`. This approach does NOT conserve mass because:
- Steam is much less dense than water (~1.5 vs ~46 lbm/ft3 at 365 psia)
- When PZR water volume shrinks by X ft3, steam volume grows by X ft3
- Recalculating `steamMass = steamVolume * rhoSteam` creates mass from nowhere
- The actual phase change (water -> steam via heater energy) converts a fixed mass via latent heat, but the volume-based recalc ignores this

Additionally, the CVCS drain mass was being double-counted: once in `UpdateDrainPhase()` (PZR water removal) and again in `UpdateRCSInventory()` (RCS water addition from CVCS net flow).

**B) Inventory Audit Baseline Type Mismatch**
The system inventory conservation check used gallon-based tracking (`totalSystemInventory_gal`, `initialSystemInventory_gal`, `systemInventoryError_gal`). Volume is temperature- and pressure-dependent, making it an unsuitable conserved quantity. Mass (lbm) is the correct conserved quantity for a closed thermodynamic system.

---

## Stage 0: Forensic Drain Logging

### Files Modified
| File | Change |
|------|--------|
| `HeatupSimEngine.BubbleFormation.cs` | Added mass snapshots before/after each DRAIN timestep with 0.1 lbm precision |

### Details
- Captures `pzrWaterMass_before`, `pzrSteamMass_before`, `rcsMass_before`, `systemMass_before` at start of `UpdateDrainPhase()`
- After all mass transfers, computes `dm_pzr` (PZR total change) and `dm_system` (system total change)
- Flags violations where `|dm_system| > 0.1 lbm` with `*** VIOLATION ***` in Debug.Log
- Fires `EventSeverity.ALERT` log entry with steam reconciliation details on violation
- Logs every 5 sim-minutes: `dm_steam`, `dm_cvcs`, `Dm_PZR`, `Dm_sys`, plus full mass breakdown

---

## Stage 1: Mass-Based Transfer Semantics

### Files Modified
| File | Change |
|------|--------|
| `HeatupSimEngine.BubbleFormation.cs` | Replaced volume-based mass recalculation with mass-conserving transfer semantics |

### Physics Fix

**Old approach (broken):**
```
PZRWaterVolume -= totalDrain_ft3
PZRSteamVolume = PZR_TOTAL_VOLUME - PZRWaterVolume
PZRWaterMass = PZRWaterVolume * rhoWater      // <- recalculated, not conserved
PZRSteamMass = PZRSteamVolume * rhoSteam      // <- recalculated, not conserved
```

**New approach (conserving):**
```
Step 1: Phase change (water -> steam) — mass-conserving within PZR
  dm_steam = Q_heater * dt / h_fg
  PZRWaterMass -= dm_steam
  PZRSteamMass += dm_steam        // PZR total unchanged

Step 2: CVCS drain — mass-conserving system-wide
  dm_cvcs = netOutflow_gpm * GPM_TO_FT3S * dt * rhoWater
  PZRWaterMass -= dm_cvcs
  RCSWaterMass += dm_cvcs          // System total unchanged

Step 3: Derive volumes from masses (canonical direction)
  PZRWaterVolume = PZRWaterMass / rhoWater
  PZRSteamVolume = PZR_TOTAL - PZRWaterVolume
  PZRSteamMass reconciled to available volume (fills remaining space)
```

### Technical Reference
Per NRC HRTD 2.1: Steam displacement is a thermodynamic process. Mass is converted from liquid to vapor phase by heater energy input via latent heat. The total mass in the pressurizer changes only by CVCS net outflow, not by phase change.

---

## Stage 2: Update Ordering / Double-Count Guard

### Files Modified
| File | Change |
|------|--------|
| `HeatupSimEngine.CVCS.cs` | Added `bubbleDrainActive` early-return in `UpdateRCSInventory()` |
| `HeatupSimEngine.BubbleFormation.cs` | Added `VCTPhysics.AccumulateRCSChange()` call during DRAIN |

### Details
- **Double-count guard:** During DRAIN, `UpdateDrainPhase()` already applies `dm_cvcs` to both `PZRWaterMass` (removal) and `RCSWaterMass` (addition). Previously, `UpdateRCSInventory()` would then apply the same CVCS net flow again. Added `bubbleDrainActive` guard analogous to the existing `regime3CVCSPreApplied` guard for Regime 2/3.
- **VCT tracking:** Since `UpdateRCSInventory()` is now skipped during DRAIN, the `VCTPhysics.AccumulateRCSChange()` call moved into `UpdateDrainPhase()` to maintain VCT mass conservation tracking.

---

## Fix B: Mass-Based Inventory Validation

### Files Modified
| File | Change |
|------|--------|
| `HeatupSimEngine.cs` | Replaced `totalSystemInventory_gal`, `initialSystemInventory_gal`, `systemInventoryError_gal` with `initialSystemMass_lbm`, `totalSystemMass_lbm`, `externalNetMass_lbm`, `massError_lbm` |
| `HeatupSimEngine.Init.cs` | `InitializeCommon()`: baseline computed from tracked masses (`physicsState.RCSWaterMass`, `PZRWaterMass`, `PZRSteamMass`) instead of volumes |
| `HeatupSimEngine.CVCS.cs` | `UpdateVCT()`: inventory audit uses tracked mass values directly (not recomputed from V*rho) |
| `HeatupValidationVisual.Panels.cs` | `DrawInventoryPanel()`: displays mass (lbm) with PZR STEAM row; thresholds: 100 lbm green / 500 lbm amber / red |
| `HeatupValidationVisual.TabValidation.cs` | `DrawValidationChecks()`: "Inventory Mass Balance" check with 500 lbm limit |
| `MainScene.unity` | Updated serialized field names |
| `_Recovery/0.unity` | Updated serialized field names |

### Validation Thresholds
| Metric | Green | Amber | Red |
|--------|-------|-------|-----|
| Mass Error | < 100 lbm | < 500 lbm | >= 500 lbm |
| Mass Conservation (PASS/FAIL) | < 500 lbm | — | >= 500 lbm |

---

## Files Changed Summary

| File | Lines | Change Description |
|------|-------|--------------------|
| `HeatupSimEngine.BubbleFormation.cs` | +86/-29 | Stage 0 logging + Stage 1 mass transfers + Stage 2 VCT tracking |
| `HeatupSimEngine.CVCS.cs` | +27/-22 | Stage 2 double-count guard + Fix B mass-based audit |
| `HeatupSimEngine.Init.cs` | +13/-9 | Fix B mass-based initialization |
| `HeatupSimEngine.cs` | +6/-4 | Fix B field declarations |
| `HeatupValidationVisual.Panels.cs` | +24/-16 | Fix B mass-based display panel |
| `HeatupValidationVisual.TabValidation.cs` | +3/-3 | Fix B mass-based validation check |
| `MainScene.unity` | +4/-3 | Serialized field name update |
| `_Recovery/0.unity` | +4/-3 | Serialized field name update |

---

## Testing Checklist

- [ ] Cold shutdown start: verify `massError_lbm` stays near 0 through solid PZR phase
- [ ] Bubble detection: verify mass continuity at solid-to-bubble transition
- [ ] DRAIN phase: verify forensic logs show `Dm_sys < 0.1 lbm` per timestep
- [ ] DRAIN phase: verify no `*** VIOLATION ***` flags in console
- [ ] DRAIN->STABILIZE transition: verify no mass step change
- [ ] STABILIZE phase: verify mass conservation through PI controller operation
- [ ] PRESSURIZE phase: verify mass conservation through heater pressurization
- [ ] Full simulation: verify `massError_lbm` stays < 100 lbm (green band)
- [ ] Validation tab: "Inventory Mass Balance" shows PASS with lbm units
- [ ] Inventory panel: shows "SYSTEM INVENTORY (MASS)" header, all values in lbm
- [ ] PZR STEAM row visible in inventory panel
