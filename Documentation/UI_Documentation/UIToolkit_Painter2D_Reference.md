# Unity 6.3 UI Toolkit — Painter2D Reference

**Source:** Unity 6.3 LTS (6000.3) Documentation  
**Last Updated:** February 2026  
**Purpose:** Reference for custom 2D vector drawing in UI Toolkit

---

## Overview

`Painter2D` is the Vector API for drawing 2D graphics in UI Toolkit. Access it via `MeshGenerationContext.painter2D` in the `generateVisualContent` callback.

```csharp
public class MyElement : VisualElement
{
    public MyElement()
    {
        generateVisualContent += OnGenerateVisualContent;
    }
    
    void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        var painter = ctx.painter2D;
        // Draw here
    }
}
```

---

## Painter2D Properties

| Property | Type | Description |
|----------|------|-------------|
| `fillColor` | Color | Color used for `Fill()` operations |
| `strokeColor` | Color | Color used for `Stroke()` operations |
| `strokeGradient` | Gradient | Gradient applied along stroke path |
| `lineWidth` | float | Width of stroked lines (pixels) |
| `lineCap` | LineCap | End cap style: `Butt`, `Round`, `Square` |
| `lineJoin` | LineJoin | Join style: `Miter`, `Round`, `Bevel` |
| `miterLimit` | float | Limit for miter joins before converting to bevel |

---

## Path Building Methods

### MoveTo / LineTo
```csharp
painter.BeginPath();
painter.MoveTo(new Vector2(10, 10));   // Start point
painter.LineTo(new Vector2(100, 10));  // Line to point
painter.LineTo(new Vector2(100, 100)); // Continue path
painter.ClosePath();                    // Close back to start
painter.Stroke();  // or Fill()
```

### Arc (CRITICAL FOR GAUGES)

**Signature:**
```csharp
public void Arc(Vector2 center, float radius, Angle startAngle, Angle endAngle, ArcDirection direction = ArcDirection.Clockwise);
```

**IMPORTANT:** In Unity 6, `Arc()` takes `Angle` structs, NOT raw floats!

**Angle Struct:**
```csharp
// Create angles - use static factory methods
Angle.Degrees(90f)    // From degrees
Angle.Radians(1.57f)  // From radians
Angle.Turns(0.25f)    // From turns (0-1)
Angle.Gradians(100f)  // From gradians

// Convert between units
angle.ToDegrees()
angle.ToRadians()
angle.ToTurns()
```

**Arc Example (Pie Slice):**
```csharp
painter.fillColor = Color.blue;
painter.strokeColor = Color.red;
painter.lineWidth = 2.0f;

painter.BeginPath();
painter.MoveTo(new Vector2(100, 100));  // Move to center first!
painter.Arc(new Vector2(100, 100), 50.0f, 10.0f, 95.0f);  // floats work for degrees
painter.ClosePath();
painter.Fill();
painter.Stroke();
```

**CRITICAL DISCOVERY — Why Arc May Draw a Line/Dot:**

In older Unity versions or incorrect usage, `Arc()` parameters differ:
1. Earlier docs showed `Arc(center, radius, startAngle, endAngle)` with raw floats (degrees)
2. Unity 6 API uses `Angle` struct but also accepts implicit float conversion
3. **The issue**: Not calling `MoveTo()` or `BeginPath()` before Arc
4. **The issue**: Start and end angles being nearly equal (zero sweep)

**Correct Arc Gauge Pattern:**
```csharp
void DrawArcGauge(Painter2D painter, Vector2 center, float radius, float valuePercent)
{
    float startAngle = 135f;  // Gauge starts at bottom-left
    float endAngle = 405f;    // Gauge ends at bottom-right (270° sweep)
    float valueAngle = startAngle + (endAngle - startAngle) * (valuePercent / 100f);
    
    // Draw background track
    painter.strokeColor = Color.gray;
    painter.lineWidth = 8f;
    painter.lineCap = LineCap.Round;
    painter.BeginPath();
    painter.Arc(center, radius, startAngle, endAngle);
    painter.Stroke();
    
    // Draw value arc
    painter.strokeColor = Color.green;
    painter.BeginPath();
    painter.Arc(center, radius, startAngle, valueAngle);
    painter.Stroke();
}
```

### ArcTo
Draws an arc that connects two lines with a rounded corner:
```csharp
painter.BeginPath();
painter.MoveTo(new Vector2(100, 100));
painter.ArcTo(new Vector2(150, 150), new Vector2(200, 100), 20.0f);  // corner radius
painter.LineTo(new Vector2(200, 100));
painter.Stroke();
```

### BezierCurveTo / QuadraticCurveTo
```csharp
// Cubic Bezier (two control points)
painter.BezierCurveTo(controlPoint1, controlPoint2, endPoint);

// Quadratic Bezier (one control point)  
painter.QuadraticCurveTo(controlPoint, endPoint);
```

---

## Complete Path Operations

| Method | Description |
|--------|-------------|
| `BeginPath()` | Start new path, clear previous |
| `MoveTo(Vector2)` | Move pen without drawing |
| `LineTo(Vector2)` | Draw line to point |
| `Arc(...)` | Add arc segment |
| `ArcTo(...)` | Add arc between lines |
| `BezierCurveTo(...)` | Add cubic bezier |
| `QuadraticCurveTo(...)` | Add quadratic bezier |
| `ClosePath()` | Close path back to start |
| `Stroke()` | Draw path outline |
| `Fill()` | Fill enclosed path |
| `Fill(FillRule)` | Fill with specific rule (`OddEven` or `NonZero`) |

---

## Working Examples from Unity 6.3 Docs

### Pie Chart
```csharp
[UxmlElement]
public partial class PieChart : VisualElement
{
    float m_Radius = 100.0f;
    float m_Value = 40.0f;

    public float value {
        get => m_Value;
        set { m_Value = value; MarkDirtyRepaint(); }
    }

    public PieChart()
    {
        generateVisualContent += DrawCanvas;
    }

    void DrawCanvas(MeshGenerationContext ctx)
    {
        var painter = ctx.painter2D;
        
        var percentages = new float[] { m_Value, 100 - m_Value };
        var colors = new Color32[] {
            new Color32(182,235,122,255),  // Green
            new Color32(251,120,19,255)    // Orange
        };
        
        float angle = 0.0f;
        float anglePct = 0.0f;
        int k = 0;
        
        foreach (var pct in percentages)
        {
            anglePct += 360.0f * (pct / 100);
            
            painter.fillColor = colors[k++];
            painter.BeginPath();
            painter.MoveTo(new Vector2(m_Radius, m_Radius));
            painter.Arc(new Vector2(m_Radius, m_Radius), m_Radius, angle, anglePct);
            painter.Fill();
            
            angle = anglePct;
        }
    }
}
```

### Radial Progress (Ring Style)
```csharp
void GenerateVisualContent(MeshGenerationContext context)
{
    float width = contentRect.width;
    float height = contentRect.height;
    Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
    float radius = width * 0.5f;

    var painter = context.painter2D;
    painter.lineWidth = 10.0f;
    painter.lineCap = LineCap.Butt;

    // Draw track (background ring)
    painter.strokeColor = m_TrackColor;
    painter.BeginPath();
    painter.Arc(center, radius, 0.0f, 360.0f);
    painter.Stroke();

    // Draw progress (partial ring)
    // Start at top (-90°), sweep based on progress
    painter.strokeColor = m_ProgressColor;
    painter.BeginPath();
    painter.Arc(center, radius, -90.0f, 360.0f * (progress / 100.0f) - 90.0f);
    painter.Stroke();
}
```

---

## Line Cap and Join Styles

```
LineCap.Butt    ──────    (flat end, no extension)
LineCap.Round   ──────●   (semicircle cap)
LineCap.Square  ──────■   (flat end, extends by half lineWidth)

LineJoin.Miter  ─┐        (sharp corner)
LineJoin.Round  ─╮        (rounded corner)
LineJoin.Bevel  ─┘        (flat corner)
```

---

## Performance Notes

1. **Avoid allocations in generateVisualContent** — reuse Vector2 arrays, buffers
2. **Use MarkDirtyRepaint()** — only repaints when values actually change
3. **Fixed update rate** — don't call MarkDirtyRepaint() every frame unless needed
4. **Ring buffers for charts** — pre-allocate fixed-size arrays for trend data
5. **Batch similar draws** — group paths with same stroke/fill settings

---

## Common Pitfalls

| Problem | Cause | Solution |
|---------|-------|----------|
| Nothing draws | Missing `Stroke()` or `Fill()` | Add render call after path |
| Arc draws line/dot | Zero sweep angle | Ensure start ≠ end angle |
| Arc at wrong position | Forgot `BeginPath()` | Always call `BeginPath()` first |
| Choppy animation | Calling repaint too often | Use 5-10 Hz update with interpolation |
| Memory spikes | Allocations in draw | Pre-allocate, avoid new in callback |

---

## See Also

- `UIToolkit_CustomControls.md` — Creating custom VisualElements
- `UIToolkit_DataBinding.md` — Runtime binding to data sources
- Unity Docs: [Generate 2D visual content](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-generate-2d-visual-content.html)
