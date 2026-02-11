# Changelog v0.9.0 — Dashboard Graph Restoration & Exit Fix

**Date:** 2026-02-07  
**Type:** Minor Release  
**Scope:** Critical bug fixes for graph display and application exit

---

## Summary

This release fixes two critical issues that made the dashboard unusable:
1. T_PZR and T_SAT were displayed as dots instead of historical traces
2. Application crashed on exit (X key or ALT+F4)

Both issues were introduced in v0.8.2. This release restores proper functionality.

---

## Problem Statement

### Issue 1: Broken Temperature Graphs
The TEMPS tab showed T_PZR and T_SAT as "live annotations" (single dots on the right edge) instead of proper historical traces. This was because:
- No history buffers existed for T_PZR or T_SAT
- The `DrawLiveAnnotation()` function only showed current values, not trends
- Operators could not see temperature trends over time

### Issue 2: Exit Crash
Pressing X to quit caused Unity to crash due to:
- `DrawClosingOverlay()` creating `new Texture2D()` objects every frame
- `DrawBoxBorder()` also creating textures every frame
- Massive memory leak exhausting Unity memory within seconds

---

## Changes

### Stage 1: Add Missing History Buffers

**HeatupSimEngine.cs**
- Added three new history buffer declarations:
  - `tPzrHistory` — Pressurizer temperature (T_PZR)
  - `tSatHistory` — Saturation temperature (T_SAT)  
  - `pressureRateHistory` — Pressure rate of change (psi/hr)

**HeatupSimEngine.Logging.cs**
- Updated `AddHistory()` to populate new buffers every sim-minute
- Updated `ClearHistoryAndEvents()` to clear new buffers on init

### Stage 2: Fix Exit Crash

**HeatupValidationVisual.cs**
- Removed `_isClosing` flag
- Removed `DrawClosingOverlay()` method (memory leak source)
- Removed `DrawBoxBorder()` method (memory leak source)
- Removed `DelayedQuit()` coroutine
- Replaced with simple immediate quit on X key press

### Stage 3: Update Graph Rendering

**HeatupValidationVisual.Graphs.cs**
- **TEMPS Tab**: Now displays 6 traces from history buffers:
  - T_RCS (green), T_HOT (red), T_COLD (blue)
  - T_PZR (orange), T_SAT (magenta) — **NOW PROPER TRACES**
  - T_SG_SEC (cyan)
- **RATES Tab**: Added Pressure Rate trace alongside Heatup Rate
- Removed `DrawLiveAnnotation()` calls for T_PZR and T_SAT
- Added 15°F subcooling alarm line (red) in addition to 30°F warning (amber)
- Added zero reference line for rate traces

---

## Files Modified

| File | Changes |
|------|---------|
| `HeatupSimEngine.cs` | +3 history buffer declarations |
| `HeatupSimEngine.Logging.cs` | +6 lines in AddHistory(), +4 lines in Clear() |
| `HeatupValidationVisual.cs` | -95 lines (removed broken overlay code) |
| `HeatupValidationVisual.Graphs.cs` | Updated TEMPS and RATES tabs |

---

## Validation Criteria

| Test | Expected Result | Status |
|------|-----------------|--------|
| T_PZR displays as historical trace | Continuous line over 4-hour window | ✓ |
| T_SAT displays as historical trace | Continuous line over 4-hour window | ✓ |
| Application exits cleanly on X key | No crash, no freeze | ✓ |
| Application exits cleanly on ALT+F4 | No crash, no freeze | ✓ |
| All 6 temperature traces visible | Legend shows all with current values | ✓ |
| Pressure Rate visible on RATES tab | Second trace with psi/hr units | ✓ |

---

## Technical Notes

### History Buffer Architecture
All history buffers now follow the same pattern:
- 240-point rolling window (1-minute samples = 4-hour window)
- Capped by `MAX_HISTORY` constant in main loop
- Cleared on simulation restart
- Populated by `AddHistory()` called from main simulation loop

### Graph Trace Colors
Standard trace color assignments (defined in Styles.cs):
- `_cTrace1` (green): Primary parameter (T_RCS, Pressure, Heatup Rate)
- `_cTrace2` (red): T_HOT
- `_cTrace3` (blue): T_COLD, Subcooling, Letdown
- `_cTrace4` (orange): T_PZR, PZR Level, Pressure Rate
- `_cTrace5` (magenta): T_SAT
- `_cTrace6` (cyan): T_SG_SEC

---

## Rollback Instructions

If issues occur, revert these files to v0.8.2:
- `HeatupSimEngine.cs`
- `HeatupSimEngine.Logging.cs`
- `HeatupValidationVisual.cs`
- `HeatupValidationVisual.Graphs.cs`

Note: v0.8.2 had the exit crash bug, so only rollback if new issues are worse.

---

## References

- Previous transcript: `/mnt/transcripts/2026-02-07-18-11-59-v0-8-2-graph-fix-closing-overlay.txt`
- Implementation plan: `IMPL_PLAN_v0.9.0.md`
