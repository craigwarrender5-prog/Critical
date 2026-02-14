# Implementation Plan v5.4.1 — PZR Drain Mass Spike Fix + Mass-Based Inventory

**Date:** 2026-02-13
**Version:** 5.4.1
**Changelog:** Changelogs/CHANGELOG_v5.4.1.md

---

## Overview

This patch fixes two related conservation violations:
1. A 3,755 lbm mass spike during PZR DRAIN/STABILIZE caused by volume-based mass recalculation
2. An inventory audit type mismatch using gallons (temperature-dependent) instead of mass (conserved)

## Architecture Decision

**Why mass-based, not volume-based?**

In a two-phase (water + steam) system, volume is NOT conserved:
- 1 lbm of water at 365 psia occupies 0.0217 ft3
- 1 lbm of steam at 365 psia occupies 0.667 ft3 (30x larger)
- When heaters convert water to steam, the MASS is conserved but the VOLUME changes dramatically

Using `mass = volume * density` to recalculate after volume changes double-counts the phase change and creates/destroys mass. The correct approach is:
1. Track mass transfers explicitly (dm_steam = Q/h_fg, dm_cvcs = flow * rho * dt)
2. Apply transfers to mass fields (conserving total)
3. Derive volumes FROM masses (not the other way)

## Implementation Stages

### Stage 0: Forensic Drain Logging
**File:** `HeatupSimEngine.BubbleFormation.cs`
- Snapshot all masses before `UpdateDrainPhase()` modifies anything
- After modifications, compute deltas and flag violations > 0.1 lbm
- Log every 5 sim-minutes with full mass breakdown
- Purpose: instrument the fix so we can verify conservation in-game

### Stage 1: Mass-Based Transfer Semantics
**File:** `HeatupSimEngine.BubbleFormation.cs`
- Replace the volume-based drain calculation with three explicit steps:
  1. Phase change: `PZRWaterMass -= dm_steam`, `PZRSteamMass += dm_steam`
  2. CVCS drain: `PZRWaterMass -= dm_cvcs`, `RCSWaterMass += dm_cvcs`
  3. Volume derivation: `PZRWaterVolume = PZRWaterMass / rhoWater`
- Steam reconciliation: After deriving water volume from mass, steam mass is reconciled to the remaining PZR volume. This is the one place where a small Dm can occur (logged as ALERT).

### Stage 2: Update Ordering / Double-Count Guard
**Files:** `HeatupSimEngine.CVCS.cs`, `HeatupSimEngine.BubbleFormation.cs`
- Add `bubbleDrainActive` early-return in `UpdateRCSInventory()` to prevent CVCS net flow being applied twice (once in DRAIN, once in CVCS update)
- Move `VCTPhysics.AccumulateRCSChange()` into DRAIN phase since `UpdateRCSInventory()` is now skipped
- Audit confirms Regime 2/3 sync points (CoupledThermo solver input setup) are correct — they set initial conditions, not outputs

### Fix B: Mass-Based Inventory Validation
**Files:** `HeatupSimEngine.cs`, `HeatupSimEngine.Init.cs`, `HeatupSimEngine.CVCS.cs`, `HeatupValidationVisual.Panels.cs`, `HeatupValidationVisual.TabValidation.cs`, `MainScene.unity`, `_Recovery/0.unity`
- Replace three `_gal` fields with four `_lbm` fields: `initialSystemMass_lbm`, `totalSystemMass_lbm`, `externalNetMass_lbm`, `massError_lbm`
- Initialization uses tracked masses from `physicsState` (already computed by Init paths)
- Runtime audit uses tracked masses directly (not recomputed from V*rho, which would mask transfers)
- Visual panels updated: header "SYSTEM INVENTORY (MASS)", all values in lbm, new PZR STEAM row
- Validation check: "Inventory Mass Balance" with 500 lbm limit

## Files Changed

| # | File | Stage | Change |
|---|------|-------|--------|
| 1 | `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs` | 0,1,2 | Forensic logging, mass transfers, VCT tracking |
| 2 | `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs` | 2,B | Double-count guard, mass-based audit |
| 3 | `Assets/Scripts/Validation/HeatupSimEngine.Init.cs` | B | Mass-based baseline initialization |
| 4 | `Assets/Scripts/Validation/HeatupSimEngine.cs` | B | Field declarations (3 _gal -> 4 _lbm) |
| 5 | `Assets/Scripts/Validation/HeatupValidationVisual.Panels.cs` | B | Mass-based inventory panel display |
| 6 | `Assets/Scripts/Validation/HeatupValidationVisual.TabValidation.cs` | B | Mass-based validation check |
| 7 | `Assets/Scenes/MainScene.unity` | B | Serialized field rename |
| 8 | `Assets/_Recovery/0.unity` | B | Serialized field rename |

## Risk Assessment

**Low risk:**
- All changes are confined to the DRAIN phase mass calculation and the inventory audit display
- No changes to the CoupledThermo solver, solid plant pressure, or RCP startup logic
- Existing `totalSystemMass` field (used in DRAIN phase for display) is separate from the new `totalSystemMass_lbm` audit field
- Forensic logging is additive (Debug.Log + EventLog) with no side effects

**Steam reconciliation residual:**
- After deriving water volume from mass, steam mass is reconciled to `PZRSteamVolume * rhoSteam`. This can produce a small Dm because the total mass in the PZR may not exactly equal `waterVolume * rhoWater + steamVolume * rhoSteam` (density varies with local conditions). This is logged as an ALERT for monitoring but is expected to be < 1 lbm per timestep.
