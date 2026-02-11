# Implementation Plan v0.8.2 — Graph X-Axis and Rendering Fix

**Date:** 2026-02-07  
**Type:** Patch (Bug Fix)  
**Priority:** HIGH  
**Scope:** Dashboard Visualization

---

## Problem Summary

The trend graphs are fundamentally broken:

1. **X-axis represents absolute simulation time** instead of a fixed rolling window
2. **As values change, the entire line moves vertically** rather than showing historical progression
3. **Time window is 12 hours** (144 points × 5 min) instead of the required **240 minutes (4 hours)**

### Current Broken Behavior

- X-axis labels show absolute sim time (0:00, 1:00, 2:00, etc.)
- When the graph redraws, all points shift together if the Y value changes
- Time range expands from 0 to current simTime, not a fixed rolling window

### Expected Behavior

A proper **rolling strip chart recorder**:
- Fixed 240-minute (4-hour) X-axis window
- X-axis labeled as relative time: "-4:00" to "NOW" (or "0" to "240 min")
- New data enters on the right edge
- Old data scrolls off the left edge
- Each data point is independent - the trace shows actual historical values

---

## Root Cause Analysis

### Issue 1: Wrong History Buffer Size

```csharp
// Current (HeatupSimEngine.cs)
const int MAX_HISTORY = 144;  // 144 × 5 min = 720 min = 12 hours

// Should be for 240-minute window:
// 240 min ÷ 5 min/sample = 48 samples
const int MAX_HISTORY = 48;
```

### Issue 2: X-Axis Range Calculation

```csharp
// Current (Graphs.cs DrawPlotArea)
float tMin = timeData[0];           // First recorded time (e.g., 0.0833 hr)
float tMax = timeData[timeData.Count - 1];  // Last recorded time (e.g., 5.5 hr)

// This means X-axis constantly expands/shifts as simulation runs
```

**Fix:** Use a fixed 4-hour window relative to current time:
```csharp
float tMax = engine.simTime;           // Current simulation time (right edge)
float tMin = tMax - 4.0f;              // 4 hours ago (left edge)
if (tMin < 0f) tMin = 0f;              // Clamp to start
```

### Issue 3: Trace Rendering

The trace rendering itself looks correct - it maps each (time, value) pair to screen coordinates. The issue is that ALL the data points have the same time-spacing, so they're being squeezed/stretched incorrectly as the X range changes.

---

## Proposed Fix

### Stage 1: Fix History Buffer Size

**File:** `Assets/Scripts/Validation/HeatupSimEngine.cs`

```csharp
// Change from:
const int MAX_HISTORY = 144;

// To (48 samples × 5 min = 240 min = 4 hours):
const int MAX_HISTORY = 48;
```

### Stage 2: Fix X-Axis to Fixed 240-Minute Window

**File:** `Assets/Scripts/Validation/HeatupValidationVisual.Graphs.cs`

Change `DrawPlotArea` to use fixed window:

```csharp
void DrawPlotArea(Rect area, List<float> timeData, TraceDescriptor[] traces,
    float yMin, float yMax, string yLabel,
    bool dualAxis, float y2Min, float y2Max, string y2Label)
{
    // ... existing setup code ...

    // v0.8.2: Fixed 4-hour (240-minute) rolling window
    const float WINDOW_HOURS = 4.0f;  // 240 minutes
    float tMax = engine.simTime;       // Current time = right edge
    float tMin = tMax - WINDOW_HOURS;  // 4 hours ago = left edge
    if (tMin < 0f) tMin = 0f;          // Don't go negative at start

    // ... rest of rendering ...
}
```

### Stage 3: Fix X-Axis Labels to Show Relative Time

Change `DrawXAxisLabels` to show relative time (minutes ago):

```csharp
void DrawXAxisLabels(Rect plotRect, float tMin, float tMax)
{
    int divs = (int)GRAPH_GRID_LINES_X;
    float windowMinutes = (tMax - tMin) * 60f;  // Convert hours to minutes
    
    for (int i = 0; i <= divs; i++)
    {
        float frac = (float)i / divs;
        float minutesAgo = windowMinutes * (1f - frac);  // Right edge = 0, left edge = windowMinutes
        float px = plotRect.x + frac * plotRect.width;

        string text;
        if (minutesAgo < 1f)
            text = "NOW";
        else
            text = $"-{minutesAgo:F0}m";

        Rect labelRect = new Rect(px - 20f, plotRect.yMax + 2f, 40f, 14f);
        var style = new GUIStyle(_graphAxisStyle) { alignment = TextAnchor.UpperCenter };
        GUI.Label(labelRect, text, style);
    }

    // X-axis title
    Rect xTitleRect = new Rect(plotRect.x, plotRect.yMax + 14f, plotRect.width, 14f);
    var xStyle = new GUIStyle(_graphLabelStyle) { alignment = TextAnchor.UpperCenter };
    GUI.Label(xTitleRect, "TIME (minutes ago)", xStyle);
}
```

### Stage 4: Update File Header Comments

Update the header to reflect 48-point buffers and 4-hour window.

---

## Files to Modify

| File | Changes |
|------|---------|
| `Assets/Scripts/Validation/HeatupSimEngine.cs` | Change `MAX_HISTORY` from 144 to 48 |
| `Assets/Scripts/Validation/HeatupValidationVisual.Graphs.cs` | Fix X-axis to 4-hour rolling window, fix labels |

---

## Validation

After fix:
- [ ] X-axis shows "-240m" to "NOW" (or similar)
- [ ] Graph window is fixed at 4 hours regardless of simulation time
- [ ] Traces show actual historical values, not uniform lines
- [ ] New data appears on right edge, old data scrolls off left
- [ ] Y-axis auto-ranges correctly based on visible data in window

---

## Version

This fix will be versioned as **v0.8.2** (patch release).
