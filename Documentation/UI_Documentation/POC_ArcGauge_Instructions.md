# UI Toolkit Arc Gauge — Proof of Concept

**Project:** Critical: Master the Atom  
**Date:** February 18, 2026  
**Unity Version:** 6.3 LTS  
**Purpose:** Validate that Painter2D Arc() renders correctly before committing to full dashboard rebuild

---

## Step-by-Step Setup Instructions (Unity 6.3)

### Step 1: Create the Test Scene

1. **File → New Scene** (Basic (Built-in) template)
2. **File → Save As** → `Assets/Scenes/UIToolkitPOC.unity`

---

### Step 2: Create the Panel Settings Asset

Panel Settings controls how the UI renders (like Canvas Scaler in uGUI).

1. In **Project window**, right-click on `Assets` folder
2. Select **Create → UI Toolkit → Panel Settings Asset**
3. Name it `POC_PanelSettings`
4. Select the asset, in **Inspector**:
   - **Scale Mode**: `Scale With Screen Size`
   - **Reference Resolution**: `1920 x 1080`
   - **Match**: `0.5` (blend width/height)

> **Note:** Unity 6.3 auto-creates a default at `Assets/UI Toolkit/PanelSettings.asset` when you add a UIDocument, but creating your own gives you control over the location and settings.

---

### Step 3: Create the UI Document GameObject

1. In **Hierarchy**, right-click → **UI Toolkit → UI Document**
   - This creates a GameObject with UIDocument component attached
   - Unity auto-creates `Assets/UI Toolkit/PanelSettings.asset` if none exists
2. Rename the GameObject to `POC_UIDocument`
3. In **Inspector**, set:
   - **Panel Settings**: Drag your `POC_PanelSettings` asset here
   - **Source Asset**: Leave empty for now (we'll set this via code)

---

### Step 4: Create the Arc Gauge Element Script

Create: `Assets/Scripts/UI/UIToolkit/POC/ArcGaugePOC.cs`

```csharp
// ============================================================================
// ArcGaugePOC.cs — Proof of Concept Arc Gauge
// Unity 6.3 UI Toolkit — CORRECTED Arc() implementation
// ============================================================================
//
// KEY FIX: Arc() takes DEGREES, not radians!
// The previous attempt multiplied angles by Deg2Rad, causing near-zero sweep.
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.POC
{
    [UxmlElement]
    public partial class ArcGaugePOC : VisualElement
    {
        // ====================================================================
        // CONSTANTS — Arc geometry in DEGREES
        // ====================================================================
        
        // Gauge spans from 7:30 position (135°) to 4:30 position (405° = 45° + 360°)
        // This creates a 270° sweep with the gap at the bottom
        private const float ARC_START_ANGLE = 135f;   // degrees
        private const float ARC_END_ANGLE = 405f;     // degrees (wraps past 360)
        private const float ARC_SWEEP = 270f;         // total arc sweep
        private const float ARC_THICKNESS = 10f;
        
        // ====================================================================
        // COLORS
        // ====================================================================
        
        private static readonly Color COLOR_TRACK = new Color(0.25f, 0.25f, 0.3f, 1f);
        private static readonly Color COLOR_VALUE = new Color(0f, 1f, 0.533f, 1f);  // Green
        private static readonly Color COLOR_NEEDLE = Color.white;
        private static readonly Color COLOR_CENTER = new Color(0.1f, 0.1f, 0.15f, 1f);
        
        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        private float _minValue = 0f;
        private float _maxValue = 100f;
        private float _value = 50f;
        private string _label = "TEST";
        
        [UxmlAttribute]
        public float minValue
        {
            get => _minValue;
            set { _minValue = value; MarkDirtyRepaint(); }
        }
        
        [UxmlAttribute]
        public float maxValue
        {
            get => _maxValue;
            set { _maxValue = value; MarkDirtyRepaint(); }
        }
        
        [UxmlAttribute]
        public float value
        {
            get => _value;
            set { _value = Mathf.Clamp(value, _minValue, _maxValue); MarkDirtyRepaint(); }
        }
        
        [UxmlAttribute]
        public string label
        {
            get => _label;
            set { _label = value; MarkDirtyRepaint(); }
        }
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public ArcGaugePOC()
        {
            // Set minimum size
            style.minWidth = 120;
            style.minHeight = 120;
            style.backgroundColor = new Color(0.06f, 0.06f, 0.1f, 1f);
            
            // Register the visual content generator
            generateVisualContent += OnGenerateVisualContent;
        }
        
        // ====================================================================
        // RENDERING — The critical fix is here!
        // ====================================================================
        
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            float width = contentRect.width;
            float height = contentRect.height;
            
            // Safety check
            if (width < 20f || height < 20f)
            {
                Debug.LogWarning($"[ArcGaugePOC] Content rect too small: {width}x{height}");
                return;
            }
            
            var painter = mgc.painter2D;
            if (painter == null)
            {
                Debug.LogError("[ArcGaugePOC] painter2D is null!");
                return;
            }
            
            // Calculate center and radius
            Vector2 center = new Vector2(width * 0.5f, height * 0.55f);
            float radius = Mathf.Min(width, height) * 0.4f;
            
            // Calculate normalized value (0-1)
            float range = _maxValue - _minValue;
            float normalizedValue = (range > 0f) ? (_value - _minValue) / range : 0f;
            normalizedValue = Mathf.Clamp01(normalizedValue);
            
            // Calculate value angle — KEY: Use DEGREES directly!
            float valueAngle = ARC_START_ANGLE + (ARC_SWEEP * normalizedValue);
            
            // ================================================================
            // Draw background track arc
            // ================================================================
            painter.strokeColor = COLOR_TRACK;
            painter.lineWidth = ARC_THICKNESS;
            painter.lineCap = LineCap.Round;
            
            painter.BeginPath();
            // Arc() takes DEGREES in Unity 6.3 — do NOT multiply by Deg2Rad!
            painter.Arc(center, radius, ARC_START_ANGLE, ARC_END_ANGLE);
            painter.Stroke();
            
            // ================================================================
            // Draw value arc (partial fill)
            // ================================================================
            if (normalizedValue > 0.001f)
            {
                painter.strokeColor = COLOR_VALUE;
                painter.lineWidth = ARC_THICKNESS;
                painter.lineCap = LineCap.Round;
                
                painter.BeginPath();
                painter.Arc(center, radius, ARC_START_ANGLE, valueAngle);
                painter.Stroke();
            }
            
            // ================================================================
            // Draw needle
            // ================================================================
            // Convert the value angle to radians for trig calculation
            // (Painter2D uses degrees, but Mathf.Cos/Sin need radians)
            float needleAngleRad = valueAngle * Mathf.Deg2Rad;
            float needleLength = radius * 0.75f;
            
            // Unity's coordinate system: Y increases downward
            // Standard trig: cos for X, sin for Y
            Vector2 needleTip = new Vector2(
                center.x + Mathf.Cos(needleAngleRad) * needleLength,
                center.y + Mathf.Sin(needleAngleRad) * needleLength
            );
            
            painter.strokeColor = COLOR_NEEDLE;
            painter.lineWidth = 3f;
            painter.lineCap = LineCap.Round;
            
            painter.BeginPath();
            painter.MoveTo(center);
            painter.LineTo(needleTip);
            painter.Stroke();
            
            // ================================================================
            // Draw center cap (filled circle)
            // ================================================================
            // Outer cap
            painter.fillColor = COLOR_NEEDLE;
            painter.BeginPath();
            painter.Arc(center, 6f, 0f, 360f);  // Full circle: 0° to 360°
            painter.ClosePath();
            painter.Fill();
            
            // Inner cap
            painter.fillColor = COLOR_CENTER;
            painter.BeginPath();
            painter.Arc(center, 3f, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
            
            // ================================================================
            // Draw value text
            // ================================================================
            // Note: For text, we'd normally use a Label child element.
            // Painter2D doesn't have text drawing. This is just for the arc test.
        }
    }
}
```

---

### Step 5: Create the Test Controller Script

Create: `Assets/Scripts/UI/UIToolkit/POC/ArcGaugePOCController.cs`

```csharp
// ============================================================================
// ArcGaugePOCController.cs — Test harness for Arc Gauge POC
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;
using Critical.UI.POC;

namespace Critical.UI.POC
{
    [RequireComponent(typeof(UIDocument))]
    public class ArcGaugePOCController : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool animateValue = true;
        [SerializeField] private float animationSpeed = 20f;
        
        private UIDocument _uiDocument;
        private ArcGaugePOC _gauge;
        private Label _valueLabel;
        private Label _statusLabel;
        
        private float _testValue = 50f;
        private float _direction = 1f;
        
        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            
            // Build UI programmatically (no UXML needed for POC)
            BuildUI();
        }
        
        private void BuildUI()
        {
            var root = _uiDocument.rootVisualElement;
            root.Clear();
            
            // Root container with dark background
            root.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            root.style.flexDirection = FlexDirection.Column;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;
            root.style.paddingTop = 50;
            root.style.paddingBottom = 50;
            
            // Title
            var title = new Label("UI Toolkit Arc Gauge — Proof of Concept");
            title.style.fontSize = 24;
            title.style.color = new Color(0f, 1f, 0.533f, 1f);
            title.style.marginBottom = 30;
            root.Add(title);
            
            // Status label
            _statusLabel = new Label("Initializing...");
            _statusLabel.style.fontSize = 14;
            _statusLabel.style.color = Color.gray;
            _statusLabel.style.marginBottom = 20;
            root.Add(_statusLabel);
            
            // Gauge container
            var gaugeContainer = new VisualElement();
            gaugeContainer.style.width = 200;
            gaugeContainer.style.height = 200;
            gaugeContainer.style.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
            gaugeContainer.style.borderTopLeftRadius = 10;
            gaugeContainer.style.borderTopRightRadius = 10;
            gaugeContainer.style.borderBottomLeftRadius = 10;
            gaugeContainer.style.borderBottomRightRadius = 10;
            root.Add(gaugeContainer);
            
            // Create the arc gauge
            _gauge = new ArcGaugePOC();
            _gauge.style.flexGrow = 1;
            _gauge.minValue = 0f;
            _gauge.maxValue = 100f;
            _gauge.value = 50f;
            _gauge.label = "TEST";
            gaugeContainer.Add(_gauge);
            
            // Value label below gauge
            _valueLabel = new Label("Value: 50.0");
            _valueLabel.style.fontSize = 18;
            _valueLabel.style.color = Color.white;
            _valueLabel.style.marginTop = 20;
            root.Add(_valueLabel);
            
            // Instructions
            var instructions = new Label("If you see a 270° arc with a moving needle, the POC is successful!");
            instructions.style.fontSize = 12;
            instructions.style.color = Color.gray;
            instructions.style.marginTop = 30;
            root.Add(instructions);
            
            var instructions2 = new Label("Previous failure: Arc drew as line/dot due to passing radians instead of degrees");
            instructions2.style.fontSize = 11;
            instructions2.style.color = new Color(1f, 0.5f, 0.5f, 0.7f);
            instructions2.style.marginTop = 5;
            root.Add(instructions2);
            
            _statusLabel.text = "✓ UI Built Successfully";
            _statusLabel.style.color = new Color(0f, 1f, 0.533f, 1f);
            
            Debug.Log("[ArcGaugePOC] UI built. Gauge should be visible.");
        }
        
        private void Update()
        {
            if (!animateValue || _gauge == null) return;
            
            // Animate value back and forth
            _testValue += _direction * animationSpeed * Time.deltaTime;
            
            if (_testValue >= 100f)
            {
                _testValue = 100f;
                _direction = -1f;
            }
            else if (_testValue <= 0f)
            {
                _testValue = 0f;
                _direction = 1f;
            }
            
            _gauge.value = _testValue;
            _valueLabel.text = $"Value: {_testValue:F1}";
        }
    }
}
```

---

### Step 6: Set Up the Scene

1. Select the `POC_UIDocument` GameObject
2. **Add Component** → search for `ArcGaugePOCController` → Add it
3. In the Inspector:
   - **Animate Value**: ✓ (checked)
   - **Animation Speed**: 20

---

### Step 7: Enter Play Mode

1. Press **Play**
2. You should see:
   - A dark background
   - A title "UI Toolkit Arc Gauge — Proof of Concept"
   - A **270° arc gauge** with:
     - Gray track (background arc)
     - Green value arc (fills based on value)
     - White needle pointing to current value
     - White center cap
   - Value label showing the current value
   - The needle and value arc should animate smoothly between 0-100

---

## Success Criteria

| Check | Expected Result |
|-------|-----------------|
| Arc visible? | 270° arc from 7:30 to 4:30 position |
| Track visible? | Gray background arc |
| Value arc visible? | Green partial arc matching value |
| Needle visible? | White line from center to value position |
| Animation smooth? | Value sweeps 0→100→0 smoothly |
| No errors? | Console shows only the initialization log |

---

## What Was Fixed

### Previous Code (WRONG):
```csharp
// Converting to radians, but Arc() expects degrees!
float startAngle = (ARC_START_ANGLE - startNorm * ARC_SWEEP) * Mathf.Deg2Rad;
float endAngle = (ARC_START_ANGLE - endNorm * ARC_SWEEP) * Mathf.Deg2Rad;
painter.Arc(center, radius, startAngle, endAngle, ...);
```

When `ARC_START_ANGLE = 180`, this becomes:
- `startAngle = 180 * 0.0174533 = 3.14` (interpreted as 3.14°)
- Result: Tiny ~3° arc = looks like a dot

### Fixed Code (CORRECT):
```csharp
// Pass degrees directly — Arc() handles them correctly
painter.Arc(center, radius, ARC_START_ANGLE, ARC_END_ANGLE);  // 135° to 405°
```

---

## Next Steps After Successful POC

1. **Add threshold coloring** (green/amber/red based on value)
2. **Test with live HeatupSimEngine data** (bind to T_avg)
3. **Create StripChartPOC** for trend lines
4. **Scale test** with multiple gauges
5. **If all pass**: Proceed with full Validation Dashboard rebuild

---

## Troubleshooting

### "Nothing renders"
- Check UIDocument has Panel Settings assigned
- Check ArcGaugePOCController is attached to same GameObject
- Check Console for errors

### "Arc still draws as line/dot"
- Verify you're using the corrected script (degrees, not radians)
- Check that contentRect has reasonable size (>20px)
- Add Debug.Log in OnGenerateVisualContent to verify it's called

### "UI appears but no gauge"
- The gauge element might have zero size
- Check style.minWidth/minHeight are set
- Try setting explicit width/height on the gauge container

---

*POC prepared by Claude — February 18, 2026*
