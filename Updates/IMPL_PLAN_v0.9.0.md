# Implementation Plan v0.9.0 — Dashboard Complete Redesign

**Date:** 2026-02-07  
**Type:** Minor Release (Breaking Change - UI Overhaul)  
**Scope:** Complete dashboard redesign from scratch

---

## Problem Statement

The current dashboard (v0.8.x) has critical failures:

1. **Broken Graphs** — T_PZR and T_SAT displayed as dots, not historical traces
2. **Exit Crash** — Closing overlay creates Texture2D every frame, crashes Unity
3. **One Graph Per Tab** — Wastes space, forces unnecessary clicking
4. **Scroll-Dependent Layout** — Key information requires scrolling

---

## Design Philosophy

1. **Multiple Related Graphs Per Tab** — Show complete picture of each system
2. **Every Parameter Gets History** — If we track it, we graph it
3. **No Scrolling for Critical Data** — All visible at a glance
4. **Clean Exit** — No crashes

---

## New Tab Layout — Multiple Graphs Per Tab

### Tab 0: TEMPERATURES
```
┌─────────────────────────────────────────────────────────────┐
│  LOOP TEMPERATURES                                          │
│  ───────────────────────────────────────────────────────── │
│  Traces: T_RCS (green), T_HOT (red), T_COLD (blue)         │
│  Y: Auto-scale to data                                      │
├─────────────────────────────────────────────────────────────┤
│  PRESSURIZER vs SATURATION                                  │
│  ───────────────────────────────────────────────────────── │
│  Traces: T_PZR (orange), T_SAT (magenta)                   │
│  Shows bubble margin (T_PZR approaching T_SAT)              │
├─────────────────────────────────────────────────────────────┤
│  SG SECONDARY (Heat Sink)                                   │
│  ───────────────────────────────────────────────────────── │
│  Traces: T_SG_SECONDARY (cyan), T_RCS (green, reference)   │
│  Shows thermal lag between primary and secondary            │
└─────────────────────────────────────────────────────────────┘
```

### Tab 1: PRESSURE/LEVEL
```
┌─────────────────────────────────────────────────────────────┐
│  RCS PRESSURE                                               │
│  ───────────────────────────────────────────────────────── │
│  Trace: Pressure (psia)                                     │
│  Y: 0-2500 psia (or auto-scale)                            │
├─────────────────────────────────────────────────────────────┤
│  PRESSURIZER LEVEL                                          │
│  ───────────────────────────────────────────────────────── │
│  Trace: PZR Level (%)                                       │
│  Y: 0-100%                                                  │
├─────────────────────────────────────────────────────────────┤
│  SURGE FLOW                                                 │
│  ───────────────────────────────────────────────────────── │
│  Trace: Surge Flow (gpm)                                    │
│  Y: -50 to +50 gpm, zero line shown                        │
│  Positive = outsurge, Negative = insurge                    │
└─────────────────────────────────────────────────────────────┘
```

### Tab 2: RATES & MARGINS
```
┌─────────────────────────────────────────────────────────────┐
│  HEATUP RATE                                                │
│  ───────────────────────────────────────────────────────── │
│  Trace: Heatup Rate (°F/hr)                                │
│  50°F/hr TECH SPEC LIMIT LINE (red)                        │
│  Y: -10 to 60 °F/hr                                        │
├─────────────────────────────────────────────────────────────┤
│  SUBCOOLING MARGIN                                          │
│  ───────────────────────────────────────────────────────── │
│  Trace: Subcooling (°F)                                    │
│  30°F WARNING LINE (amber)                                  │
│  15°F ALARM LINE (red)                                      │
│  Y: 0-200°F                                                │
├─────────────────────────────────────────────────────────────┤
│  PRESSURE RATE                                              │
│  ───────────────────────────────────────────────────────── │
│  Trace: Pressure Rate (psi/hr)                             │
│  Zero line shown                                            │
│  Y: -200 to +200 psi/hr                                    │
└─────────────────────────────────────────────────────────────┘
```

### Tab 3: CVCS
```
┌─────────────────────────────────────────────────────────────┐
│  CHARGING & LETDOWN                                         │
│  ───────────────────────────────────────────────────────── │
│  Traces: Charging (green), Letdown (blue)                  │
│  Y: 0-120 gpm                                              │
├─────────────────────────────────────────────────────────────┤
│  VCT LEVEL                                                  │
│  ───────────────────────────────────────────────────────── │
│  Trace: VCT Level (%)                                       │
│  High/Low setpoint lines                                    │
│  Y: 0-100%                                                  │
├─────────────────────────────────────────────────────────────┤
│  SURGE FLOW                                                 │
│  ───────────────────────────────────────────────────────── │
│  Trace: Surge Flow (gpm)                                    │
│  Zero line, shows CVCS-driven inventory changes            │
└─────────────────────────────────────────────────────────────┘
```

### Tab 4: VCT/BRS
```
┌─────────────────────────────────────────────────────────────┐
│  VCT LEVEL                                                  │
│  ───────────────────────────────────────────────────────── │
│  Trace: VCT Level (%)                                       │
│  Setpoint bands shown                                       │
├─────────────────────────────────────────────────────────────┤
│  BRS HOLDUP TANK                                            │
│  ───────────────────────────────────────────────────────── │
│  Trace: Holdup Volume (gal)                                │
│  Y: 0-50000 gal                                            │
├─────────────────────────────────────────────────────────────┤
│  BRS DISTILLATE                                             │
│  ───────────────────────────────────────────────────────── │
│  Trace: Distillate Available (gal)                         │
│  Shows processed water accumulation                         │
└─────────────────────────────────────────────────────────────┘
```

---

## Overall Screen Layout

```
┌─────────────────────────────────────────────────────────────────────────┐
│ HEADER: Mode │ Phase │ Sim Time │ Wall Time │ Rate │ Subcool │ WARP    │
├─────────────────────────────────────────────────────────────────────────┤
│ TAB BAR: [TEMPS] [PRESS/LVL] [RATES] [CVCS] [VCT/BRS]                  │
├───────────────────────────────────────────────┬─────────────────────────┤
│                                               │                         │
│   GRAPH 1 (largest or equal split)            │   DIGITAL READOUTS      │
│   ─────────────────────────────────────────   │   ┌───────┐ ┌───────┐  │
│                                               │   │ T_PZR │ │ PRESS │  │
│                                               │   │ 245.3 │ │  412  │  │
├───────────────────────────────────────────────┤   └───────┘ └───────┘  │
│                                               │   ┌───────┐ ┌───────┐  │
│   GRAPH 2                                     │   │SUBCOOL│ │ RATE  │  │
│   ─────────────────────────────────────────   │   │ 89.2  │ │ +42.1 │  │
│                                               │   └───────┘ └───────┘  │
├───────────────────────────────────────────────┤   ┌───────┐ ┌───────┐  │
│                                               │   │PZR LVL│ │ RCPs  │  │
│   GRAPH 3                                     │   │ 25.0% │ │  2/4  │  │
│   ─────────────────────────────────────────   │   └───────┘ └───────┘  │
│                                               │                         │
├─────────────────────────────────┬─────────────┴─────────────────────────┤
│     ANNUNCIATOR PANEL           │           EVENT LOG                   │
│  (Keep existing - working)      │  T+01:23 ACTION RCP #1 START...      │
└─────────────────────────────────┴───────────────────────────────────────┘
```

---

## Required History Buffers

### Currently Exist:
- `tempHistory` (T_RCS)
- `tHotHistory` (T_HOT)
- `tColdHistory` (T_COLD)
- `tSgSecondaryHistory` (T_SG_SECONDARY)
- `pressHistory` (Pressure)
- `pzrLevelHistory` (PZR Level)
- `subcoolHistory` (Subcooling)
- `heatRateHistory` (Heatup Rate)
- `chargingHistory` (Charging Flow)
- `letdownHistory` (Letdown Flow)
- `surgeFlowHistory` (Surge Flow)
- `vctLevelHistory` (VCT Level)
- `brsHoldupHistory` (BRS Holdup)
- `brsDistillateHistory` (BRS Distillate)
- `timeHistory` (Time axis)

### MUST ADD:
- `tPzrHistory` (T_PZR) — **CRITICAL MISSING BUFFER**
- `tSatHistory` (T_SAT) — **CRITICAL MISSING BUFFER**
- `pressureRateHistory` (Pressure Rate) — For RATES tab

---

## Implementation Stages

### Stage 1: Add Missing History Buffers

**HeatupSimEngine.cs** — Add declarations:
```csharp
[HideInInspector] public List<float> tPzrHistory = new List<float>();
[HideInInspector] public List<float> tSatHistory = new List<float>();
[HideInInspector] public List<float> pressureRateHistory = new List<float>();
```

**HeatupSimEngine.Logging.cs** — Update AddHistory():
```csharp
void AddHistory()
{
    // ... existing ...
    tPzrHistory.Add(T_pzr);
    tSatHistory.Add(T_sat);
    pressureRateHistory.Add(pressureRate);
    
    // ... in cap section ...
    tPzrHistory.RemoveAt(0);
    tSatHistory.RemoveAt(0);
    pressureRateHistory.RemoveAt(0);
}
```

**HeatupSimEngine.Logging.cs** — Update ClearHistoryAndEvents():
```csharp
void ClearHistoryAndEvents()
{
    // ... existing ...
    tPzrHistory.Clear();
    tSatHistory.Clear();
    pressureRateHistory.Clear();
}
```

### Stage 2: Fix Exit Crash

**HeatupValidationVisual.cs** — Remove broken DrawClosingOverlay():
- Delete `_isClosing` flag
- Delete `DrawClosingOverlay()` method
- Delete `DrawBoxBorder()` method  
- Delete `DelayedQuit()` coroutine
- Return to simple immediate quit on X key press

### Stage 3: Rewrite Graph Rendering

**HeatupValidationVisual.Graphs.cs** — Complete rewrite:
- Each tab draws 3 stacked graphs
- Each graph uses history buffers (no "live annotations")
- Shared DrawGraph() method with parameters for traces, colors, limits
- Proper legend showing trace names and current values

### Stage 4: Simplify Layout

**HeatupValidationVisual.cs** — New layout:
- Remove gauge scroll view
- Add simple digital readout panel (right side)
- Keep annunciator panel (working)
- Keep event log (working)

**DELETE these files:**
- `HeatupValidationVisual.Gauges.cs` — Not needed
- `HeatupValidationVisual.Panels.cs` — Not needed

---

## Graph Rendering Specification

Each graph needs:
1. **Title** — What's being shown
2. **Y-axis** — Labels, range (auto or fixed)
3. **X-axis** — Time in hours
4. **Traces** — Multiple lines from history buffers
5. **Legend** — Color swatch + name + current value
6. **Reference lines** — Limits, setpoints, zero line as appropriate

Graph drawing function signature:
```csharp
void DrawGraph(Rect area, string title, TraceDescriptor[] traces, 
               float yMin, float yMax, string yLabel,
               ReferenceLineDescriptor[] refLines = null)
```

---

## Validation Criteria

| Requirement | Test |
|-------------|------|
| T_PZR shows as trace | 30 min run, verify historical line |
| T_SAT shows as trace | 30 min run, verify historical line |
| 3 graphs visible per tab | Visual inspection |
| No scrolling needed | All graphs visible without scroll |
| Clean exit on X | No crash, no freeze |
| Annunciators work | Verify alarm states |

---

## Files Modified

| File | Action | Stage |
|------|--------|-------|
| `HeatupSimEngine.cs` | Add 3 history buffer declarations | 1 |
| `HeatupSimEngine.Logging.cs` | Update AddHistory(), Clear() | 1 |
| `HeatupValidationVisual.cs` | Remove crash code, new layout | 2, 4 |
| `HeatupValidationVisual.Graphs.cs` | Complete rewrite | 3 |
| `HeatupValidationVisual.Gauges.cs` | DELETE | 4 |
| `HeatupValidationVisual.Panels.cs` | DELETE | 4 |
| `HeatupValidationVisual.Styles.cs` | Keep, ensure textures cached | 2 |
| `HeatupValidationVisual.Annunciators.cs` | Keep unchanged | - |

---

## References

- NRC HRTD 19.2 — Plant Heatup Operations  
- Westinghouse 4-Loop PWR Control Room Instrumentation
