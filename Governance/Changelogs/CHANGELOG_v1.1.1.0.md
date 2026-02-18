# Changelog - v1.1.1.0

## Version 1.1.1.0 - PZR Temperature Visualization Enhancement

**Date:** 2026-02-18  
**Status:** COMPLETE  
**Implementation Plan:** IP-0051  
**Domain Plan:** DP-0008 — Operator Interface & Scenarios  
**CS Resolved:** CS-0108, CS-0111  
**Impact Classification:** PATCH (display-only changes, no physics impact)

---

## Summary

IP-0051 addressed critical operator visibility gaps for pressurizer temperature (T_pzr) monitoring during cold startup operations. Per NRC HRTD 17.0, operators must monitor T_pzr approach to saturation temperature to determine bubble formation readiness. The dashboard now provides primary T_pzr visualization via arc gauge and sparkline trend in the appropriate columns.

---

## Changes

### Stage 1: T_pzr Arc Gauge Added to Pressurizer Column

**File Modified:**

| File | Description |
|------|-------------|
| `Assets/Scripts/Validation/Tabs/OverviewTab.cs` | Added T_pzr arc gauge after LEVEL gauge in `DrawPressurizerColumn()` method. Range 50-600°F, color-coded (green at saturation, cyan subcooled). |

### Stage 2: T_pzr Readout Removed from RCS Column

**File Modified:**

| File | Description |
|------|-------------|
| `Assets/Scripts/Validation/Tabs/OverviewTab.cs` | Removed misplaced T_pzr digital readout from `DrawRCSColumn()`. T_pzr is now displayed exclusively via PZR column arc gauge. |

### Stage 3: T_pzr Sparkline Replaces NET HEAT

**File Modified:**

| File | Description |
|------|-------------|
| `Assets/Scripts/Validation/ValidationDashboard.Sparklines.cs` | Renamed `IDX_NET_HEAT` → `IDX_T_PZR`. Configured T_pzr sparkline with 50-600°F range. Updated `PushValues()` to push `snapshot.T_pzr`. |
| `Assets/Scripts/Validation/Tabs/OverviewTab.cs` | Updated fallback labels array to show "T_PZR" instead of "NET HEAT". |

---

## Technical Details

### T_pzr Arc Gauge Specification

| Property | Value |
|----------|-------|
| Location | Pressurizer column, after LEVEL gauge |
| Range | 50-600°F |
| Color (subcooled) | Cyan (`_cCyanInfo`) |
| Color (at saturation) | Green (`_cNormalGreen`) |
| Condition | `s.PzrAtSaturation` determines color |

### T_pzr Sparkline Specification

| Property | Value |
|----------|-------|
| Slot | 7 (formerly NET HEAT) |
| Range | 50-600°F (fixed, matches arc gauge) |
| Color | Amber (`_cWarningAmber`) |
| Format | F1 (one decimal place) |
| Unit | °F |

---

## Validation Evidence

- Code inspection confirms all changes are display-only
- No physics modules modified
- Build verification pending user confirmation

---

## Version Justification

Classified as `PATCH` under constitution rules because IP-0051 contains only display rendering changes with no physics or behavioral impact. No new capabilities added; existing data now visualized more appropriately.

Version increment applied from `1.1.0.0` to `1.1.1.0`.

---

## Governance

- `IP-0051`: CLOSED  
- `CS-0108`: CLOSED (FIXED)  
- `CS-0111`: CLOSED (FIXED)
