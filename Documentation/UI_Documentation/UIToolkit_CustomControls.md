# Unity 6.3 UI Toolkit — Custom Controls Reference

**Source:** Unity 6.3 LTS (6000.3) Documentation  
**Last Updated:** February 2026  
**Purpose:** Creating custom VisualElement controls for UI Toolkit

---

## Basic Custom Control Structure

```csharp
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]  // Makes control visible in UI Builder
public partial class MyCustomControl : VisualElement
{
    // USS class names for styling
    public static readonly string ussClassName = "my-custom-control";
    public static readonly string ussLabelClassName = "my-custom-control__label";
    
    // Exposed properties
    private float m_Value;
    
    [UxmlAttribute]  // Exposes in UI Builder Inspector
    public float value
    {
        get => m_Value;
        set
        {
            m_Value = value;
            MarkDirtyRepaint();  // Trigger visual update
        }
    }
    
    public MyCustomControl()
    {
        // Add USS class
        AddToClassList(ussClassName);
        
        // Register for custom style resolution
        RegisterCallback<CustomStyleResolvedEvent>(OnCustomStylesResolved);
        
        // Register visual content generator
        generateVisualContent += OnGenerateVisualContent;
    }
    
    void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        var painter = ctx.painter2D;
        // Draw custom visuals here
    }
    
    static void OnCustomStylesResolved(CustomStyleResolvedEvent evt)
    {
        var element = (MyCustomControl)evt.currentTarget;
        element.UpdateCustomStyles();
    }
    
    void UpdateCustomStyles()
    {
        // Read custom USS properties
        bool repaint = false;
        if (customStyle.TryGetValue(s_CustomColor, out m_CustomColor))
            repaint = true;
        if (repaint)
            MarkDirtyRepaint();
    }
}
```

---

## UxmlElement and UxmlAttribute (Unity 6)

Unity 6 simplified the UXML integration with attributes:

### [UxmlElement]
- Makes the control available in UI Builder's Library panel
- Class must be `partial`
- Replaces the old `UxmlFactory` pattern

### [UxmlAttribute]
- Exposes a property in UI Builder's Inspector
- Automatically generates serialization
- Works with primitive types, strings, enums, Unity objects

```csharp
[UxmlElement]
public partial class GaugeControl : VisualElement
{
    [UxmlAttribute]
    public float minValue { get; set; } = 0f;
    
    [UxmlAttribute]
    public float maxValue { get; set; } = 100f;
    
    [UxmlAttribute]
    public float currentValue 
    { 
        get => m_CurrentValue;
        set { m_CurrentValue = value; MarkDirtyRepaint(); }
    }
    private float m_CurrentValue;
    
    [UxmlAttribute]
    public Color trackColor { get; set; } = Color.gray;
    
    [UxmlAttribute]
    public Color fillColor { get; set; } = Color.green;
}
```

---

## Custom USS Properties

Define custom CSS properties that can be set in USS files:

```csharp
// In the control class
static CustomStyleProperty<Color> s_TrackColor = new CustomStyleProperty<Color>("--track-color");
static CustomStyleProperty<Color> s_ProgressColor = new CustomStyleProperty<Color>("--progress-color");
static CustomStyleProperty<float> s_LineWidth = new CustomStyleProperty<float>("--line-width");

Color m_TrackColor = Color.gray;
Color m_ProgressColor = Color.green;
float m_LineWidth = 10f;

void UpdateCustomStyles()
{
    bool repaint = false;
    
    if (customStyle.TryGetValue(s_TrackColor, out m_TrackColor))
        repaint = true;
    if (customStyle.TryGetValue(s_ProgressColor, out m_ProgressColor))
        repaint = true;
    if (customStyle.TryGetValue(s_LineWidth, out m_LineWidth))
        repaint = true;
        
    if (repaint)
        MarkDirtyRepaint();
}
```

**In USS:**
```css
.my-gauge {
    --track-color: rgb(130, 130, 130);
    --progress-color: rgb(46, 132, 24);
    --line-width: 8;
    min-width: 100px;
    min-height: 100px;
}
```

---

## Composing Controls with Child Elements

```csharp
[UxmlElement]
public partial class LabeledGauge : VisualElement
{
    Label m_Label;
    Label m_ValueLabel;
    
    public string label
    {
        get => m_Label.text;
        set => m_Label.text = value;
    }
    
    public float value
    {
        get => m_Value;
        set 
        { 
            m_Value = value;
            m_ValueLabel.text = $"{value:F1}";
            MarkDirtyRepaint();
        }
    }
    private float m_Value;
    
    public LabeledGauge()
    {
        // Create child elements
        m_Label = new Label("Label");
        m_Label.AddToClassList("labeled-gauge__label");
        Add(m_Label);
        
        m_ValueLabel = new Label("0.0");
        m_ValueLabel.AddToClassList("labeled-gauge__value");
        Add(m_ValueLabel);
        
        // Custom drawing for gauge
        generateVisualContent += DrawGauge;
    }
    
    void DrawGauge(MeshGenerationContext ctx)
    {
        // Draw gauge visuals
    }
}
```

---

## Using contentRect for Drawing

The `contentRect` property gives you the drawable area (excluding padding/borders):

```csharp
void OnGenerateVisualContent(MeshGenerationContext ctx)
{
    float width = contentRect.width;
    float height = contentRect.height;
    
    if (width < 2f || height < 2f)
        return;  // Too small to draw
    
    Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
    float radius = Mathf.Min(width, height) * 0.5f - m_LineWidth * 0.5f;
    
    var painter = ctx.painter2D;
    // Draw using center and radius
}
```

---

## Event Handling

```csharp
public MyControl()
{
    // Mouse events
    RegisterCallback<MouseDownEvent>(OnMouseDown);
    RegisterCallback<MouseUpEvent>(OnMouseUp);
    RegisterCallback<MouseMoveEvent>(OnMouseMove);
    RegisterCallback<MouseEnterEvent>(OnMouseEnter);
    RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
    
    // Keyboard events (requires focusable = true)
    focusable = true;
    RegisterCallback<KeyDownEvent>(OnKeyDown);
    
    // Geometry changes
    RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
}

void OnMouseDown(MouseDownEvent evt)
{
    // Handle click
    evt.StopPropagation();  // Prevent bubbling if needed
}

void OnGeometryChanged(GeometryChangedEvent evt)
{
    // Size changed, may need to recalculate
    MarkDirtyRepaint();
}
```

---

## Complete Arc Gauge Example

```csharp
using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI
{
    [UxmlElement]
    public partial class ArcGauge : VisualElement
    {
        public static readonly string ussClassName = "arc-gauge";
        
        // Custom style properties
        static CustomStyleProperty<Color> s_TrackColor = new("--track-color");
        static CustomStyleProperty<Color> s_FillColor = new("--fill-color");
        static CustomStyleProperty<Color> s_NeedleColor = new("--needle-color");
        
        Color m_TrackColor = new Color(0.3f, 0.3f, 0.3f);
        Color m_FillColor = new Color(0f, 1f, 0.5f);
        Color m_NeedleColor = Color.white;
        
        float m_MinValue = 0f;
        float m_MaxValue = 100f;
        float m_Value = 0f;
        
        // Gauge arc parameters (degrees)
        const float StartAngle = 135f;   // Bottom-left
        const float EndAngle = 405f;     // Bottom-right (270° sweep)
        const float LineWidth = 8f;
        
        [UxmlAttribute]
        public float minValue { get => m_MinValue; set { m_MinValue = value; MarkDirtyRepaint(); } }
        
        [UxmlAttribute]
        public float maxValue { get => m_MaxValue; set { m_MaxValue = value; MarkDirtyRepaint(); } }
        
        [UxmlAttribute]
        public float value 
        { 
            get => m_Value; 
            set { m_Value = Mathf.Clamp(value, m_MinValue, m_MaxValue); MarkDirtyRepaint(); } 
        }
        
        public ArcGauge()
        {
            AddToClassList(ussClassName);
            RegisterCallback<CustomStyleResolvedEvent>(OnStylesResolved);
            generateVisualContent += OnGenerateVisualContent;
        }
        
        static void OnStylesResolved(CustomStyleResolvedEvent evt)
        {
            var gauge = (ArcGauge)evt.currentTarget;
            bool repaint = false;
            
            if (gauge.customStyle.TryGetValue(s_TrackColor, out gauge.m_TrackColor)) repaint = true;
            if (gauge.customStyle.TryGetValue(s_FillColor, out gauge.m_FillColor)) repaint = true;
            if (gauge.customStyle.TryGetValue(s_NeedleColor, out gauge.m_NeedleColor)) repaint = true;
            
            if (repaint) gauge.MarkDirtyRepaint();
        }
        
        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            float width = contentRect.width;
            float height = contentRect.height;
            
            if (width < 20f || height < 20f) return;
            
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
            float radius = Mathf.Min(width, height) * 0.5f - LineWidth;
            
            // Calculate value angle
            float normalizedValue = (m_Value - m_MinValue) / (m_MaxValue - m_MinValue);
            float valueAngle = StartAngle + (EndAngle - StartAngle) * normalizedValue;
            
            var painter = ctx.painter2D;
            painter.lineWidth = LineWidth;
            painter.lineCap = LineCap.Round;
            
            // Draw track (background arc)
            painter.strokeColor = m_TrackColor;
            painter.BeginPath();
            painter.Arc(center, radius, StartAngle, EndAngle);
            painter.Stroke();
            
            // Draw fill (value arc)
            if (normalizedValue > 0.001f)
            {
                painter.strokeColor = m_FillColor;
                painter.BeginPath();
                painter.Arc(center, radius, StartAngle, valueAngle);
                painter.Stroke();
            }
            
            // Draw needle
            float needleLength = radius * 0.8f;
            float needleAngle = valueAngle * Mathf.Deg2Rad;
            Vector2 needleEnd = center + new Vector2(
                Mathf.Cos(needleAngle) * needleLength,
                Mathf.Sin(needleAngle) * needleLength
            );
            
            painter.strokeColor = m_NeedleColor;
            painter.lineWidth = 3f;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.MoveTo(center);
            painter.LineTo(needleEnd);
            painter.Stroke();
            
            // Draw center dot
            painter.fillColor = m_NeedleColor;
            painter.BeginPath();
            painter.Arc(center, 4f, 0f, 360f);
            painter.Fill();
        }
    }
}
```

**USS for the gauge:**
```css
.arc-gauge {
    min-width: 80px;
    min-height: 80px;
    --track-color: rgb(60, 60, 70);
    --fill-color: rgb(0, 255, 136);
    --needle-color: white;
}

.arc-gauge--warning {
    --fill-color: rgb(255, 170, 0);
}

.arc-gauge--alarm {
    --fill-color: rgb(255, 68, 68);
}
```

---

## Best Practices

1. **Always use partial class with [UxmlElement]**
2. **Call MarkDirtyRepaint() when visual properties change**
3. **Use USS custom properties for theming**
4. **Handle CustomStyleResolvedEvent to read USS values**
5. **Check contentRect dimensions before drawing**
6. **Use AddToClassList() for USS class assignment**
7. **Pre-allocate any arrays used in generateVisualContent**

---

## See Also

- `UIToolkit_Painter2D_Reference.md` — Vector drawing API
- `UIToolkit_DataBinding.md` — Runtime binding
- Unity Docs: [Create custom controls](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-create-custom-controls.html)
