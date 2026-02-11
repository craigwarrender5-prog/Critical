# Changelog v0.9.3 — Dashboard Layout Reorganization

**Date:** 2026-02-07  
**Type:** Minor (UI Enhancement)  
**Priority:** MEDIUM  
**Scope:** Dashboard Layout

---

## Summary

Reorganized dashboard layout to eliminate scrolling on the left gauge column by using full-height columns and moving Annunciators/Event Log to center and right columns respectively.

---

## Previous Layout

```
┌──────────────────────────────────────────────────────┐
│  HEADER BAR                                          │
├──────────┬────────────────────────┬──────────────────┤
│          │                        │                  │
│ GAUGES   │    TREND GRAPHS        │  STATUS PANELS   │   62% height
│ (scroll) │                        │  (scroll)        │
│          │                        │                  │
├──────────┴────────────────────────┴──────────────────┤
│  ANNUNCIATORS (45%)     │    EVENT LOG (55%)         │   32% height
└──────────────────────────────────────────────────────┘
```

**Issues:**
- Gauge column required scrolling
- Wasted vertical space

---

## New Layout

```
┌──────────────────────────────────────────────────────┐
│  HEADER BAR (6%)                                     │
├──────────┬────────────────────────┬──────────────────┤
│          │                        │                  │
│ GAUGES   │    TREND GRAPHS        │  STATUS PANELS   │
│ (22%)    │    (65% of column)     │  (75% of column) │
│          │                        │  (scrollable)    │
│ FULL     ├────────────────────────┤                  │
│ HEIGHT   │                        ├──────────────────┤
│          │    ANNUNCIATORS        │  EVENT LOG       │
│ NO       │    (35% of column)     │  (25% of column) │
│ SCROLL   │                        │                  │
└──────────┴────────────────────────┴──────────────────┘
```

**Benefits:**
- All gauges visible without scrolling
- Larger trend graph area
- Annunciators in dedicated center section
- Status panels retain scroll (as needed)
- Event log compact but usable

---

## Technical Changes

### Constants

```csharp
// Removed:
const float MAIN_FRAC = 0.62f;
const float FOOTER_FRAC = 0.32f;

// Added:
const float CENTER_GRAPH_FRAC = 0.65f;  // Graphs: 65% of center
const float CENTER_ANN_FRAC = 0.35f;    // Annunciators: 35% of center
const float RIGHT_STATUS_FRAC = 0.75f;  // Status: 75% of right
const float RIGHT_LOG_FRAC = 0.25f;     // Event Log: 25% of right
```

### New Methods

- `DrawCenterColumn(Rect area)` — Stacks Graphs + Annunciators
- `DrawRightColumn(Rect area)` — Stacks Status + Event Log

### Modified Methods

- `OnGUI()` — Full-height columns, no footer
- `DrawGaugeColumn()` — Conditional scroll (only if content exceeds available height)

### Removed

- `DrawFooter()` method removed (no longer needed)

---

## Height Calculations (1080p)

| Component | Height |
|-----------|--------|
| Screen | 1080px |
| Header (6%) | 65px |
| Available | 1015px |
| Gauge content | ~888px |
| **Result** | Fits without scroll ✓ |

---

## Files Modified

| File | Changes |
|------|---------|
| `HeatupValidationVisual.cs` | Complete layout restructure |

---

## Validation

| Test | Expected |
|------|----------|
| Gauge column | No scroll needed at 1080p+ |
| Trend graphs | Top 65% of center column |
| Annunciators | Bottom 35% of center column |
| Status panels | Top 75% of right column (scrollable) |
| Event log | Bottom 25% of right column |
| Small screen fallback | Scroll appears if gauge content doesn't fit |
