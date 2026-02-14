# Changelog v5.0.3 — Inventory Audit Reconciliation Patch

**Version:** v5.0.3 (PATCH)
**Date:** 2026-02-12
**Implementation Plan:** `Critical\Updates\IMPLEMENTATION_PLAN_v5.0.3.md`

---

## Summary

Fixed false 20.5% mass conservation error (~189,000 lbm) in Inventory Audit v1.1.0 that appeared at the solid-to-two-phase pressurizer transition (~8.5 hr). The canonical v5.0.2 mass conservation system was correct throughout; the error was isolated to the audit's independent V×ρ recalculation path.

---

## Changes

### Fixed

- **Inventory Audit two-phase mass source** — Replaced defective V×ρ recalculation in the `else` branch of `UpdateInventoryAudit()` with canonical `physicsState` fields:
  - `inventoryAudit.RCS_Mass_lbm` now reads `physicsState.RCSWaterMass` (was `rcsLoopVolume_ft3 * rcsWaterDensity_audit`)
  - `inventoryAudit.PZR_Water_Mass_lbm` now reads `physicsState.PZRWaterMass` (was `pzrWaterVolume * pzrWaterDensity`)
  - `inventoryAudit.PZR_Steam_Mass_lbm` now reads `physicsState.PZRSteamMass` (was `pzrSteamVolume * pzrSteamDensity`)

### Added

- **`AuditMassSource` field** on `InventoryAuditState` struct — Tracks which canonical regime path was used: `CANONICAL_SOLID` during solid ops, `CANONICAL_TWO_PHASE` after bubble formation.
- **`Mass Source:` annotation** in `SaveIntervalLog()` — Displays the active mass source regime in every interval log, immediately after the `TOTAL MASS` line.

### Removed

- **Unused density locals** in `UpdateInventoryAudit()` — `rcsWaterDensity`, `pzrWaterDensity`, `pzrSteamDensity` (three `WaterProperties` calls eliminated). These were only used by the defective V×ρ branch.

### Unchanged

- Solid ops branch in `UpdateInventoryAudit()` — Already correct (uses `PZRWaterMassSolid` and `TotalPrimaryMassSolid`). Only change is addition of `AuditMassSource = "CANONICAL_SOLID"`.
- All physics modules — No changes to `SolidPlantPressure.cs`, `PressurizerPhysics.cs`, `CoupledThermo.cs`, `RCSHeatup.cs`, or any other physics code.
- VCT and BRS mass calculations in the audit — Unchanged (correct, use their own independent state).
- Cumulative flow tracking — Unchanged.
- Conservation error calculation — Unchanged (now operates on correct mass inputs).
- All visual/display code — No changes. `InventoryAuditState` is additive-only (one new field).

---

## Root Cause

The `else` branch in `UpdateInventoryAudit()` computed RCS loop mass as:

```
RCS_Mass = (RCS_WATER_VOLUME - PZR_TOTAL_VOLUME) × ρ(T_rcs, P)
```

This used a fixed geometric volume constant that did not account for cumulative CVCS boundary flows (charging, letdown, seal injection/return, CBO losses). By bubble formation at ~8.5 hr, cumulative CVCS net flow had removed significant mass from the primary system. The V×ρ recalculation effectively "reinvented" full-volume mass that was never present, producing a phantom ~189,000 lbm conservation error.

---

## Files Modified

| File | Lines Changed |
|------|--------------|
| `Assets\Scripts\Validation\HeatupSimEngine.Logging.cs` | ~20 lines net (struct field + branch rewrite + log annotation + comment updates) |

**No other files modified.**

---

## Validation Checklist

Build in Unity and run heatup simulation 0–16 hr. Verify against v5.0.2 baseline logs:

| # | Criterion | Expected | Status |
|---|-----------|----------|--------|
| V1 | Audit conservation error ≤0.1% throughout 0–16 hr | `Conservation_Error_pct` ≤ 0.1% in all interval logs | ☐ |
| V2 | No discontinuity at bubble formation | `Total_Mass_lbm` changes < 500 lbm between 8.50–9.00 hr | ☐ |
| V3 | VCT conservation unchanged | `Conservation Err` identical (±0.01 gal) to v5.0.2 baseline | ☐ |
| V4 | Thermal behavior unchanged | `T_avg`, `T_pzr`, `T_rcs` identical (±0.01°F) to v5.0.2 | ☐ |
| V5 | SG behavior unchanged | `T_sg_secondary`, `sgHeatTransfer_MW`, boiling state identical | ☐ |
| V6 | CVCS behavior unchanged | `chargingFlow`, `letdownFlow`, `surgeFlow`, `vctState.Level_percent` identical | ☐ |
| V7 | RCP heat balance unchanged | `rcpHeat`, `effectiveRCPHeat`, `rcpCount` identical | ☐ |
| V8 | No new compile warnings | Zero new warnings beyond existing baseline | ☐ |
| V9 | `Conservation_Alarm` never triggers | `Alarm: NO` in all interval logs 0–16 hr | ☐ |
| V10 | Mass source annotated | `Mass Source: CANONICAL_SOLID` or `CANONICAL_TWO_PHASE` in every log | ☐ |

**V3–V7 note:** This patch modifies only the audit's mass *reading* path (not physics). Thermal/pressure/flow values should be bit-identical to v5.0.2. Any deviation = FAIL.
