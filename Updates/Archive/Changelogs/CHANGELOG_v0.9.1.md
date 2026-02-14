# Changelog v0.9.1 — Dashboard BRS Display Enhancement

**Date:** 2026-02-07  
**Type:** Patch (UI Enhancement)  
**Priority:** LOW  
**Scope:** Heatup Dashboard — Left Column Gauge Layout

---

## Summary

Consolidated the separate VCT and BRS gauge groups into a single **"VCT & BRS — LIQUID INVENTORY"** group featuring three arc gauges side-by-side. Added a new **bidirectional arc gauge** for BRS flow that shows inflow/outflow direction via needle deflection from center-zero.

---

## Changes

### New Bidirectional Arc Gauge

Added `DrawGaugeArcBidirectional()` function that renders a center-zero arc gauge:

- **Needle at 12 o'clock** = 0 gpm (no flow)
- **Needle deflects RIGHT** = Positive flow (inflow to BRS from VCT divert) — Orange
- **Needle deflects LEFT** = Negative flow (outflow from BRS to VCT) — Blue
- **270° arc sweep** from bottom-left through top to bottom-right
- **Signed value display**: "+5.0", "-3.2", or "0.0" gpm

### Consolidated Liquid Inventory Group

Merged VCT and BRS into single gauge group with:

| Gauge | Position | Description |
|-------|----------|-------------|
| VCT Level | Left | 0-100% with band-based coloring |
| BRS Holdup | Center | 0-100% with processing status coloring |
| BRS Flow | Right | ±40 gpm bidirectional center-zero |

Mini bars below:
- VCT BORON (ppm)
- MASS CONS (gal) — Conservation error tracking

### Removed

- `DrawVCTGauges()` method — Functionality moved to `DrawLiquidInventoryGauges()`
- `DrawBRSGauges()` method — Functionality moved to `DrawLiquidInventoryGauges()`
- `VCT_GROUP_H` constant
- `BRS_GROUP_H` constant

### Height Savings

- **Before:** VCT group (177px) + BRS group (177px) = 354px
- **After:** Liquid Inventory group = 157px
- **Saved:** ~197px vertical space

---

## Files Modified

| File | Changes |
|------|---------|
| `Assets/Scripts/Validation/HeatupValidationVisual.Gauges.cs` | Added bidirectional gauge, consolidated VCT+BRS, removed old methods |

---

## Technical Details

### Bidirectional Arc Geometry

The arc spans 270° total:
- **225° (bottom-left)** = Minimum value (-40 gpm)
- **90° (top/12 o'clock)** = Zero (0 gpm)
- **-45° (bottom-right)** = Maximum value (+40 gpm)

Normalized mapping:
- `normalised = 0.0` → 225° (min)
- `normalised = 0.5` → 90° (zero)
- `normalised = 1.0` → -45° (max)

### BRS Flow Physics

Per NRC HRTD 4.1, BRS inflow and outflow are **mutually exclusive**:

| VCT Level | BRS Flow Direction |
|-----------|-------------------|
| > 70% (Divert Setpoint) | **Inflow** to BRS (positive, needle right) |
| < 27% (Makeup Setpoint) | **Outflow** from BRS (negative, needle left) |
| 27–70% | **No flow** (needle at center) |

This justifies the single bidirectional gauge rather than separate in/out gauges.

---

## Visual Layout

### Before (v0.9.0)
```
┌─────────────────────────────────┐
│ VCT — VOLUME CONTROL TANK       │
│         ┌────────┐              │
│         │VCT LVL │              │
│         └────────┘              │
│ VCT BORON, DIVERT, MASS CONS    │
└─────────────────────────────────┘
┌─────────────────────────────────┐
│ BRS — BORON RECYCLE SYSTEM      │
│         ┌────────┐              │
│         │BRS HU  │              │
│         └────────┘              │
│ DISTILLATE, EVAP, BRS IN/RET    │
└─────────────────────────────────┘
```

### After (v0.9.1)
```
┌─────────────────────────────────────────────┐
│ VCT & BRS — LIQUID INVENTORY                │
│  ┌────────┐  ┌────────┐  ┌────────┐         │
│  │VCT LVL │  │BRS HU  │  │BRS FLOW│         │
│  │ 62.6%  │  │ 15.2%  │  │ +5.0   │←needle  │
│  └────────┘  └────────┘  └────────┘  at right│
│ VCT BORON ████████████████████    2000 ppm  │
│ MASS CONS ██                       2.1 gal  │
└─────────────────────────────────────────────┘
```

---

## Validation Criteria

| Test | Expected Result | Status |
|------|-----------------|--------|
| VCT arc gauge displays correctly | Shows level %, proper band coloring | ✓ |
| BRS Holdup arc gauge displays correctly | Shows holdup %, orange when processing | ✓ |
| BRS Flow at zero | Needle points straight up, gray color, "0.0" | ✓ |
| BRS Flow during divert | Needle right, orange, "+X.X gpm" | ✓ |
| BRS Flow during return | Needle left, blue, "-X.X gpm" | ✓ |
| Vertical space reduced | ~197px saved | ✓ |
| No compilation errors | Clean build | ✓ |

---

## References

- Implementation Plan: `Updates and Changelog/IMPL_PLAN_v0.9.1.md`
- NRC HRTD 4.1 Section 4.1.2.6 — BRS flow description
- Callaway FSAR Chapter 11 — BRS design parameters
