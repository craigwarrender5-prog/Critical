# Changelog — IP-0050

**IP:** IP-0050  
**Domain Plan:** DP-0008 — Operator Interface & Scenarios  
**CS Resolved:** CS-0108  
**Date:** 2026-02-17  
**Version:** 1.0.0

---

## Summary

Added PZR temperature monitoring for bubble formation readiness during cold startup, per NRC HRTD 17.0 requirements. Operators can now visually determine when the pressurizer has reached saturation temperature and is ready for bubble draw.

---

## Changes

### Stage 1: Snapshot Enhancement
**File:** `Assets/Scripts/Validation/ValidationDashboard.Snapshot.cs`

| Change | Description |
|--------|-------------|
| Added field `PzrSubcooling` | Float field calculating T_sat - T_pzr (°F). Positive = subcooled, zero/negative = at/above saturation. |
| Added field `PzrAtSaturation` | Boolean field, true when PzrSubcooling ≤ 5°F. Indicates bubble formation readiness. |
| Updated `CaptureFrom()` | Added calculation of both fields after existing pressurizer data capture. |

### Stage 2: Annunciator Tile Addition
**File:** `Assets/Scripts/Validation/ValidationDashboard.Annunciators.cs`

| Change | Description |
|--------|-------------|
| Replaced tile slot 26 | Changed from "PERM" (Mode Change Permissive) to "PZR SAT" (PZR At Saturation Temperature). |
| Tile configuration | Severity: INFO, Condition: `s => s.PzrAtSaturation`, Description: "PZR At Saturation Temperature" |

**Rationale:** "PERM" was less operationally critical than PZR saturation monitoring during cold startup. Mode permissives can be inferred from other indicators.

### Stage 3: Pressurizer Column Readout
**File:** `Assets/Scripts/Validation/Tabs/OverviewTab.cs`

| Change | Description |
|--------|-------------|
| Added digital readout "PZR ΔT_SAT" | Displays `s.PzrSubcooling` in °F in the Pressurizer column. |
| Positioned after SURGE flow | Logically placed before BUBBLE state LED (subcooling indicates readiness for bubble). |
| Color-coded display | GREEN when `PzrAtSaturation` is true (≤5°F subcooling), CYAN otherwise. |

---

## Acceptance Criteria Verification

| Criteria | Status |
|----------|--------|
| PZR Subcooling Readout showing T_sat - T_pzr | ✓ Implemented |
| PZR SAT Annunciator (INFO severity, triggers at ≤5°F) | ✓ Implemented |
| Event Log Entry on activation | ✓ Automatic via ISA-18.1 state machine |
| Color-coded T_pzr readout | ✓ Implemented (GREEN/CYAN based on approach) |

---

## Technical References

- NRC HRTD 17.0 (ML023040268): "When pressurizer temperature reaches saturation temperature for the pressure being maintained (450°F at 400 psig), a pressurizer bubble is established."
- NRC HRTD Startup Pressurization Reference (Technical_Documentation)

---

## Files Modified

1. `Assets/Scripts/Validation/ValidationDashboard.Snapshot.cs`
2. `Assets/Scripts/Validation/ValidationDashboard.Annunciators.cs`
3. `Assets/Scripts/Validation/Tabs/OverviewTab.cs`

---

## Governance

- **CS-0108:** CLOSED (FIXED)
- **IP-0050:** CLOSED (COMPLETED)
- **DP-0008:** Updated, CS-0108 removed from backlog
