# UI Toolkit Validation Dashboard — Feasibility Analysis

**Project:** Critical: Master the Atom  
**Date:** February 18, 2026  
**Unity Version:** 6.3 LTS  
**Status:** ANALYSIS COMPLETE

---

## Executive Summary

Based on Unity 6.3 documentation review, **UI Toolkit is now viable for the Validation Dashboard**, but the previous IP-0042 failure was due to incorrect `Arc()` usage, not a fundamental limitation.

### Root Cause of IP-0042 Failure

The gauge drew "a straight line and a green dot" because:

1. **Missing `BeginPath()` before Arc** — Without this, the arc has no path context
2. **Possible angle confusion** — Earlier Unity versions used different Arc signatures
3. **Missing `MoveTo()` for filled sectors** — Required when drawing pie slices

### Unity 6.3 Correct Arc Pattern

```csharp
// CORRECT - This draws a proper arc
painter.BeginPath();
painter.Arc(center, radius, startAngleDegrees, endAngleDegrees);
painter.Stroke();

// CORRECT - This draws a filled pie slice
painter.BeginPath();
painter.MoveTo(center);  // Required for fill!
painter.Arc(center, radius, startAngleDegrees, endAngleDegrees);
painter.ClosePath();
painter.Fill();
```

---

## What Changed in Unity 6.x

| Feature | Unity 2022 | Unity 6.x |
|---------|------------|-----------|
| Arc signature | `Arc(center, radius, startAngle, endAngle)` floats | Same, plus `Angle` struct support |
| UxmlFactory | Required `UxmlFactory<T>` inner class | `[UxmlElement]` attribute |
| UxmlTraits | Required for each property | `[UxmlAttribute]` attribute |
| Data binding | Manual only | Native runtime binding with `[CreateProperty]` |
| SVG support | Package only | Built into core |

---

## Feasibility Assessment

### Arc Gauges — ✅ VIABLE

Unity 6.3 documentation includes working examples:
- Pie charts with `Arc()` + `Fill()`
- Radial progress indicators with `Arc()` + `Stroke()`
- Both use the exact pattern needed for instrument gauges

**Recommended implementation:**
```csharp
void DrawGauge(Painter2D painter, Vector2 center, float radius, float value, float min, float max)
{
    const float StartAngle = 135f;
    const float EndAngle = 405f;
    float sweep = EndAngle - StartAngle;
    float valueAngle = StartAngle + sweep * ((value - min) / (max - min));
    
    // Track
    painter.strokeColor = trackColor;
    painter.lineWidth = 8f;
    painter.lineCap = LineCap.Round;
    painter.BeginPath();
    painter.Arc(center, radius, StartAngle, EndAngle);
    painter.Stroke();
    
    // Value
    painter.strokeColor = valueColor;
    painter.BeginPath();
    painter.Arc(center, radius, StartAngle, valueAngle);
    painter.Stroke();
}
```

### Strip Charts — ✅ VIABLE

Use `MoveTo()` + `LineTo()` for line charts:
```csharp
void DrawTrace(Painter2D painter, float[] data, Color color)
{
    if (data.Length < 2) return;
    
    painter.strokeColor = color;
    painter.lineWidth = 1.5f;
    painter.BeginPath();
    
    float xStep = contentRect.width / (data.Length - 1);
    painter.MoveTo(new Vector2(0, MapY(data[0])));
    
    for (int i = 1; i < data.Length; i++)
    {
        painter.LineTo(new Vector2(i * xStep, MapY(data[i])));
    }
    
    painter.Stroke();
}
```

### Data Binding — ✅ VIABLE

Unity 6 native binding with `[CreateProperty]` eliminates manual synchronization:
```csharp
// ViewModel updates at 5Hz
[CreateProperty] public float T_avg { get; private set; }

// UI automatically updates when property changes
```

### Performance — ⚠️ REQUIRES VALIDATION

The documentation examples are simple. A dashboard with 15+ gauges and 8 strip charts needs profiling:
- Target: <2ms UI update at 5Hz
- Risk: Multiple `Stroke()` calls may accumulate
- Mitigation: Pre-allocate buffers, minimize layout invalidation

---

## Recommended Spike Implementation

Before committing to full rebuild, validate with a minimal test:

### Stage 0A: Single Arc Gauge (1 hour)

1. Create `ArcGaugeElement.cs` with the correct pattern
2. Bind to live `HeatupSimEngine.T_avg`
3. Verify arc draws correctly
4. Measure CPU cost

### Stage 0B: Strip Chart (1 hour)

1. Create `StripChartElement.cs` with ring buffer
2. 3 traces, 4-hour history (720 points at 20s intervals)
3. Measure rendering cost

### Stage 0C: Scale Test (1 hour)

1. 6 gauges + 2 charts in test panel
2. 5Hz update rate
3. Profile full scenario

---

## Key Implementation Notes

### Arc Angle Convention

Unity's `Arc()` uses **degrees**, measured:
- 0° = Right (3 o'clock)
- 90° = Down (6 o'clock)
- 180° = Left (9 o'clock)
- 270° = Up (12 o'clock)

For a standard gauge (bottom-left to bottom-right):
- Start: 135° (7:30 position)
- End: 405° (4:30 position, or 45° + 360°)
- Sweep: 270°

### MarkDirtyRepaint() Usage

Only call when visual data changes:
```csharp
public float value
{
    get => m_Value;
    set
    {
        if (Mathf.Abs(m_Value - value) > 0.01f)  // Threshold
        {
            m_Value = value;
            MarkDirtyRepaint();
        }
    }
}
```

### Allocation-Free Drawing

```csharp
// BAD - allocates every frame
painter.MoveTo(new Vector2(x, y));

// GOOD - reuse cached vectors
Vector2 m_CachedPosition;
m_CachedPosition.x = x;
m_CachedPosition.y = y;
painter.MoveTo(m_CachedPosition);
```

---

## Decision Matrix

| Approach | Effort | Risk | Quality |
|----------|--------|------|---------|
| UI Toolkit rebuild | High (2 weeks) | Medium (perf unknown) | High |
| OnGUI expansion (IP-0043) | Low (3 days) | Low (proven) | Medium |
| Hybrid (layout + OnGUI draw) | Medium | Low | Medium-High |

### Recommendation

**Execute Stage 0 spike first.** If arc gauges work correctly in 1 hour, proceed with UI Toolkit. If not, fall back to OnGUI expansion.

---

## Files Created

Documentation saved to `Documentation/UI_Documentation/`:
- `UIToolkit_Painter2D_Reference.md` — Vector drawing API
- `UIToolkit_CustomControls.md` — Creating custom elements
- `UIToolkit_DataBinding.md` — Runtime data binding
- `UIToolkit_Feasibility_Analysis.md` — This document

---

## Next Steps

1. **Read `UIToolkit_Painter2D_Reference.md`** for correct Arc pattern
2. **Implement Stage 0A spike** with single ArcGaugeElement
3. **Verify arc draws correctly** (not a line/dot)
4. **If successful**, proceed with full IP

---

*Analysis prepared by Claude based on Unity 6.3 LTS documentation review*
