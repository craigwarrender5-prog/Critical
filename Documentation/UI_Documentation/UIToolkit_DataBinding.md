# Unity 6.3 UI Toolkit — Runtime Data Binding Reference

**Source:** Unity 6.3 LTS (6000.3) Documentation  
**Last Updated:** February 2026  
**Purpose:** Binding UI elements to data sources at runtime

---

## Overview

Unity 6 introduced runtime data binding for UI Toolkit, enabling automatic UI updates when data changes. This follows the MVVM (Model-View-ViewModel) pattern.

**Key Benefits:**
- Automatic UI synchronization with data
- Reduced boilerplate code
- Clean separation of concerns
- Works with ScriptableObjects, MonoBehaviours, plain C# objects

---

## Data Source Setup

### ScriptableObject Data Source

```csharp
using Unity.Properties;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/PlayerStats")]
public class PlayerStats : ScriptableObject
{
    [CreateProperty]  // Required for binding
    public float health = 100f;
    
    [CreateProperty]
    public float maxHealth = 100f;
    
    [CreateProperty]
    public string playerName = "Player";
    
    [CreateProperty]
    public float HealthPercent => health / maxHealth * 100f;
}
```

### MonoBehaviour Data Source

```csharp
using Unity.Properties;
using UnityEngine;

public class SimulationData : MonoBehaviour
{
    [CreateProperty]
    public float temperature = 70f;
    
    [CreateProperty]
    public float pressure = 14.7f;
    
    [CreateProperty]
    public bool isRunning = false;
}
```

### Plain C# ViewModel

```csharp
using Unity.Properties;
using System.ComponentModel;

public class DashboardViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    
    private float m_Value;
    
    [CreateProperty]
    public float Value
    {
        get => m_Value;
        set
        {
            if (m_Value != value)
            {
                m_Value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
    }
}
```

---

## Binding in UXML (UI Builder)

### Using UI Builder

1. Select a UI element (e.g., Label)
2. In Inspector, find the **Bindings** section
3. Add a binding for a property (e.g., `text`)
4. Set:
   - **Data Source**: Reference to your data object
   - **Data Source Path**: Property name (e.g., `health`)
   - **Binding Mode**: ToTarget, ToSource, or TwoWay

### UXML Syntax

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:Label name="health-label">
        <Bindings>
            <ui:DataBinding 
                property="text" 
                data-source-path="health" 
                binding-mode="ToTarget" />
        </Bindings>
    </ui:Label>
</ui:UXML>
```

---

## Binding in C# Code

### Basic Binding Setup

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class DashboardController : MonoBehaviour
{
    [SerializeField] UIDocument uiDocument;
    [SerializeField] PlayerStats playerStats;  // ScriptableObject
    
    void OnEnable()
    {
        var root = uiDocument.rootVisualElement;
        
        // Set data source on root (inherited by children)
        root.dataSource = playerStats;
        
        // Or set on specific element
        var healthLabel = root.Q<Label>("health-label");
        healthLabel.dataSource = playerStats;
    }
}
```

### Manual Binding with DataBinding

```csharp
void SetupBindings()
{
    var root = uiDocument.rootVisualElement;
    var healthLabel = root.Q<Label>("health-label");
    
    // Create binding
    var binding = new DataBinding
    {
        dataSource = playerStats,
        dataSourcePath = new PropertyPath("health"),
        bindingMode = BindingMode.ToTarget
    };
    
    // Apply to property
    healthLabel.SetBinding("text", binding);
}
```

### Binding with Converters

When the source type doesn't match the target type:

```csharp
// Float to string converter
public class FloatToStringConverter : IConverter<float, string>
{
    public string Convert(float value) => $"{value:F1}";
    public float ConvertBack(string value) => float.Parse(value);
}

// Usage
var binding = new DataBinding
{
    dataSource = playerStats,
    dataSourcePath = new PropertyPath("health"),
    bindingMode = BindingMode.ToTarget
};
binding.sourceToUiConverters.AddConverter(new FloatToStringConverter());
healthLabel.SetBinding("text", binding);
```

---

## Binding Modes

| Mode | Description |
|------|-------------|
| `ToTarget` | Data → UI only (one-way, most common) |
| `ToSource` | UI → Data only (for input fields) |
| `TwoWay` | Bidirectional sync |

---

## Update Frequency

By default, bindings update every frame. For performance, you can control update timing:

```csharp
// Binding updates can be configured
var binding = new DataBinding
{
    // ... binding setup
    updateTrigger = BindingUpdateTrigger.OnSourceChanged  // Only when source changes
};
```

For high-frequency data (like simulation values), consider:
1. Snapshot pattern at fixed intervals
2. Manual binding updates
3. Direct property assignment without binding

---

## Snapshot Pattern for Simulation Data

For the Validation Dashboard use case, a snapshot ViewModel works well:

```csharp
using Unity.Properties;
using UnityEngine;

public class DashboardViewModel : MonoBehaviour
{
    [SerializeField] HeatupSimEngine engine;
    
    // Snapshot properties (updated periodically)
    [CreateProperty] public float T_avg { get; private set; }
    [CreateProperty] public float Pressure { get; private set; }
    [CreateProperty] public float PzrLevel { get; private set; }
    [CreateProperty] public float Subcooling { get; private set; }
    [CreateProperty] public string Phase { get; private set; }
    [CreateProperty] public bool IsRunning { get; private set; }
    
    float updateInterval = 0.2f;  // 5 Hz
    float nextUpdate;
    
    void Update()
    {
        if (Time.time >= nextUpdate)
        {
            TakeSnapshot();
            nextUpdate = Time.time + updateInterval;
        }
    }
    
    void TakeSnapshot()
    {
        if (engine == null) return;
        
        T_avg = engine.T_avg;
        Pressure = engine.pressure;
        PzrLevel = engine.pzrLevel;
        Subcooling = engine.subcooling;
        Phase = engine.currentPhase.ToString();
        IsRunning = engine.isRunning;
    }
}
```

---

## Binding to Custom Controls

For custom VisualElements that need to receive bound values:

```csharp
[UxmlElement]
public partial class ArcGauge : VisualElement
{
    [UxmlAttribute]
    [CreateProperty]  // Makes it bindable
    public float value
    {
        get => m_Value;
        set
        {
            m_Value = value;
            MarkDirtyRepaint();
        }
    }
    private float m_Value;
}
```

Then in UXML or C#:
```xml
<Critical.UI.ArcGauge name="temp-gauge">
    <Bindings>
        <ui:DataBinding property="value" data-source-path="T_avg" binding-mode="ToTarget" />
    </Bindings>
</Critical.UI.ArcGauge>
```

---

## Complete Example: Bound Dashboard

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class ValidationDashboardController : MonoBehaviour
{
    [SerializeField] UIDocument uiDocument;
    [SerializeField] HeatupSimEngine engine;
    
    DashboardViewModel viewModel;
    
    void OnEnable()
    {
        viewModel = new DashboardViewModel(engine);
        
        var root = uiDocument.rootVisualElement;
        root.dataSource = viewModel;
        
        // All child elements with data-source-path will now bind
        
        // Or manually bind specific elements
        SetupGaugeBindings(root);
    }
    
    void SetupGaugeBindings(VisualElement root)
    {
        var tempGauge = root.Q<ArcGauge>("temp-gauge");
        tempGauge.SetBinding("value", new DataBinding
        {
            dataSource = viewModel,
            dataSourcePath = new PropertyPath(nameof(DashboardViewModel.T_avg)),
            bindingMode = BindingMode.ToTarget
        });
        
        var pressureGauge = root.Q<ArcGauge>("pressure-gauge");
        pressureGauge.SetBinding("value", new DataBinding
        {
            dataSource = viewModel,
            dataSourcePath = new PropertyPath(nameof(DashboardViewModel.Pressure)),
            bindingMode = BindingMode.ToTarget
        });
    }
    
    void Update()
    {
        viewModel.UpdateSnapshot();  // 5 Hz update inside
    }
}

public class DashboardViewModel
{
    HeatupSimEngine engine;
    float nextUpdate;
    const float UpdateInterval = 0.2f;
    
    [CreateProperty] public float T_avg { get; private set; }
    [CreateProperty] public float Pressure { get; private set; }
    [CreateProperty] public float PzrLevel { get; private set; }
    
    public DashboardViewModel(HeatupSimEngine engine)
    {
        this.engine = engine;
    }
    
    public void UpdateSnapshot()
    {
        if (Time.time < nextUpdate) return;
        nextUpdate = Time.time + UpdateInterval;
        
        T_avg = engine.T_avg;
        Pressure = engine.pressure;
        PzrLevel = engine.pzrLevel;
    }
}
```

---

## Performance Considerations

1. **Use [CreateProperty] sparingly** — only on properties that need binding
2. **Snapshot at fixed intervals** — don't bind directly to rapidly changing simulation data
3. **Prefer ToTarget mode** — unless you need user input
4. **Batch updates** — update multiple properties at once rather than individually
5. **Consider manual updates** — for very high-frequency data, direct property assignment may be faster

---

## Common Pitfalls

| Problem | Cause | Solution |
|---------|-------|----------|
| Binding doesn't update | Missing [CreateProperty] | Add attribute to source property |
| Type mismatch error | float → string | Add converter |
| Memory leaks | Event handlers not removed | Unsubscribe in OnDisable |
| Sluggish updates | Binding every frame | Use snapshot pattern at lower frequency |

---

## See Also

- `UIToolkit_CustomControls.md` — Creating bindable custom elements
- `UIToolkit_Painter2D_Reference.md` — Custom drawing
- Unity Docs: [Runtime data binding](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-runtime-binding.html)
