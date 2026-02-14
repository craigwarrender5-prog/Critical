# RCS Primary Loop Screen and Blender Gauge Creation - Implementation Plan

**Version:** 1.2.0  
**Date:** 2026-02-09  
**Author:** Claude AI Development Assistant  
**Status:** Awaiting Approval

---

## 1. Problem Summary

The user has successfully imported a Blender 3D model of the RCS Primary Loop into Unity (`Assets/Models/RCS/RCS_Primary_Loop.fbx`) with accompanying materials in `Assets/Materials/RCS/`. The following deliverables are requested:

1. **Unity Scripts**: Complete C# scripts to make the RCS Operator Screen functional, following the existing Reactor Core screen pattern (attach components to a GameObject and the system works)

2. **Blender Gauge Scripts**: Python scripts for Blender 5.0 to create realistic arc gauges and temperature gauges that can be imported into Unity for use across all operator screens

3. **Export/Import Manual**: Comprehensive guide for Blender 5.0 to Unity 6.3 workflow

4. **Technical Specifications**: Research actual Westinghouse 4-Loop PWR RCS data for accurate gauge ranges and parameters

---

## 2. Expectations - Correct Behavior

### 2.1 RCS Screen Behavior

When the scripts are attached to a Canvas GameObject:
- Press **Key 2** to toggle RCS Primary Loop screen
- 3D model renders in center panel via RenderTexture
- 16 gauges display real-time simulation data
- 4 RCP control panels show pump status
- Color-coded piping reflects temperatures
- Animated flow arrows indicate flow direction

### 2.2 Blender Gauge Expectations

The Blender scripts should create:

**Arc Gauge:**
- 270° sweep dial (from 7 o'clock to 5 o'clock position)
- Tick marks with numeric labels
- Color-coded warning/danger zones
- Separate needle object for animation
- Properly named hierarchy for Unity

**Temperature Gauge:**
- Vertical bar thermometer style
- Fill level indicator (separate mesh)
- Temperature scale markings
- Color gradient from cold (blue) to hot (red)
- Mercury/fill object for animation

### 2.3 Export/Import Quality

- Clean FBX export with materials preserved
- Proper UV mapping for textures
- Correct scale (1 Blender unit = 1 Unity unit)
- Preserved hierarchy and naming
- Animation-ready pivot points

---

## 3. Technical Specifications - Westinghouse 4-Loop PWR RCS

### 3.1 Verified Technical Data (NRC HRTD / FSAR Sources)

| Parameter | Value | Source |
|-----------|-------|--------|
| Thermal Power | 3,411 MWt | NRC HRTD |
| Operating Pressure | 2,235 psig (2,250 psia) | NRC HRTD 3.2 |
| T-hot (100% power) | 618-620°F | NRC HRTD 3.2 |
| T-cold (100% power) | 555-558°F | NRC HRTD 3.2 |
| T-avg (100% power) | 588.5°F | Calculated |
| T-avg (No-Load/HZP) | 557°F | NRC HRTD 19.2 |
| Core ΔT (100% power) | 61°F | NRC HRTD |
| Total RCS Flow | 390,400 gpm | NRC HRTD 3.2 |
| Flow per RCP | ~97,600 gpm (nominal) | Calculated |
| Design Flow per RCP | 88,500 gpm (rated) | FSAR |
| RCP Speed | 1,189 rpm | FSAR |
| Number of Loops | 4 | Configuration |
| Hot Leg ID | 29.0 inches | NRC HRTD 3.2 |
| Cold Leg ID | 27.5 inches | NRC HRTD 3.2 |
| Crossover Leg ID | 31.0 inches | NRC HRTD 3.2 |
| Core Inlet Temperature | 287.7°C (549.9°F) | MIT 22.06 |
| Core Outlet Temperature | 324°C (615.2°F) | MIT 22.06 |
| Design Pressure | 2,500 psia | FSAR |

### 3.2 Gauge Ranges for RCS Screen

Based on the verified technical data:

| Gauge | Min | Max | Warning Low | Warning High | Danger Low | Danger High | Units |
|-------|-----|-----|-------------|--------------|------------|-------------|-------|
| Loop T-hot | 100 | 700 | - | 620 | - | 650 | °F |
| Loop T-cold | 100 | 700 | - | 560 | - | 580 | °F |
| Average T-avg | 100 | 620 | - | 595 | - | 610 | °F |
| Core ΔT | 0 | 80 | - | 65 | - | 70 | °F |
| Loop Flow | 0 | 120,000 | 70,000 | - | 60,000 | - | gpm |
| Total RCS Flow | 0 | 450,000 | 280,000 | - | 240,000 | - | gpm |
| Core Thermal Power | 0 | 50 | - | 30 | - | 40 | MW |
| RCP Speed | 0 | 1,500 | 1,100 | 1,250 | - | - | rpm |

### 3.3 Temperature Instrument Ranges (Wide/Narrow)

Per NRC HRTD Section 10.1:
- **Wide Range RTD**: 0-700°F (shutdown/startup/post-accident)
- **Narrow Range RTD**: 510-630°F or 530-650°F (normal operations)

For the simulator during heatup (Mode 6 → Mode 1), wide range is appropriate.

---

## 4. Proposed Fix - Technical Implementation

### 4.1 Stage Overview

| Stage | Description | Files Created |
|-------|-------------|---------------|
| 1 | Base Infrastructure Scripts | `OperatorScreen.cs`, `ScreenManager.cs`, `GaugeType_Extended.cs` |
| 2 | RCS Screen Implementation | `RCSPrimaryLoopScreen.cs`, `RCSVisualizationController.cs` |
| 3 | Supporting Components | `RCPControlPanel.cs`, `RCSGaugeTypes.cs` |
| 4 | Blender Arc Gauge Script | `BlenderArcGauge_v1.py` |
| 5 | Blender Temperature Gauge Script | `BlenderTemperatureGauge_v1.py` |
| 6 | Blender-Unity Export/Import Manual | `Blender5_Unity6_Export_Manual.md` |
| 7 | Assembly Instructions | `RCS_Screen_Assembly_Guide.md` |

### 4.2 File Locations

```
Critical/
├── Assets/
│   └── Scripts/
│       └── UI/
│           ├── Base/
│           │   ├── OperatorScreen.cs        [Stage 1]
│           │   └── ScreenManager.cs         [Stage 1]
│           ├── Screens/
│           │   └── RCSPrimaryLoopScreen.cs  [Stage 2]
│           ├── Components/
│           │   ├── RCSVisualizationController.cs [Stage 2]
│           │   └── RCPControlPanel.cs       [Stage 3]
│           └── Types/
│               └── RCSGaugeTypes.cs         [Stage 3]
│
├── Updates/
│   └── Screen2_RCS_Primary_Loop/
│       ├── BlenderArcGauge_v1.py           [Stage 4]
│       ├── BlenderTemperatureGauge_v1.py   [Stage 5]
│       ├── Blender5_Unity6_Export_Manual.md [Stage 6]
│       └── RCS_Screen_Assembly_Guide.md    [Stage 7]
```

---

## 5. Stage 1: Base Infrastructure Scripts

### 5.1 OperatorScreen.cs (Abstract Base Class)

```csharp
// Path: Assets/Scripts/UI/Base/OperatorScreen.cs
// Abstract base class for all operator screens
// Provides common functionality: toggle visibility, keyboard handling, panel management

Features:
- Abstract ToggleKey property (subclass defines Key 1, 2, 3, etc.)
- Abstract ScreenName property
- Abstract ScreenIndex property
- Protected IsVisible property with events
- Virtual Awake/Start/Update for subclass override
- Common panel references (left, center, right, bottom)
- Integration with ScreenManager
```

### 5.2 ScreenManager.cs (Singleton Manager)

```csharp
// Path: Assets/Scripts/UI/Base/ScreenManager.cs
// Manages all operator screens, ensuring only one is visible at a time

Features:
- Singleton pattern (ScreenManager.Instance)
- RegisterScreen(OperatorScreen screen) method
- ShowScreen(int index) method
- HideScreen(int index) method
- HideAllScreens() method
- GetActiveScreen() property
- Keyboard routing for screen toggle keys
```

### 5.3 GaugeType Extension

Extends existing `GaugeType` enum with RCS-specific types.

---

## 6. Stage 2: RCS Screen Implementation

### 6.1 RCSPrimaryLoopScreen.cs

Complete implementation following the existing `ReactorOperatorScreen.cs` pattern:

**Inspector Fields:**
- Panel references (Left, Center, Right, Bottom)
- 3D Visualization references (Camera, RenderTexture, RawImage)
- 16 Gauge references (8 temperature, 8 flow)
- 4 RCP Control Panel references
- Status display references
- Alarm panel references
- Color configuration

**Core Methods:**
- `Awake()`: Build gauge arrays, find simulation references
- `Start()`: Initialize visualization, gauges, RCP controls
- `Update()`: Update gauges, RCP status, visualization, status displays
- `OnScreenShown()`: Refresh when screen becomes visible
- `OnScreenHidden()`: Cleanup when hidden

**Integration:**
- Reads data from `HeatupSimEngine` or `ReactorController`
- Uses existing `MosaicGauge` component
- Follows layout percentages from design doc

### 6.2 RCSVisualizationController.cs

Component attached to the imported Blender model:

**Features:**
- Material caching for runtime color changes
- Temperature gradient shader updates
- RCP rotor animation
- Flow arrow pulsing/movement
- Status light colors

**Methods:**
- `UpdateTemperatures(float tHot, float tCold)`: Set piping colors
- `SetRCPState(int index, bool running, float flowFraction)`: Update RCP visuals
- `SetFlowAnimationSpeed(float speed)`: Control flow arrow animation

---

## 7. Stage 3: Supporting Components

### 7.1 RCPControlPanel.cs

A MonoBehaviour that can be attached to a UI panel:

**Inspector Fields:**
- TextMeshProUGUI: Pump name, speed display, status text
- Image: Status indicator light
- Button: Start, Stop buttons
- Slider: Flow fraction bar
- Color settings for Running/Stopped/Ramping/Tripped

**Public Methods:**
- `Initialize(int pumpNumber, Action onStart, Action onStop)`
- `UpdateStatus(RCPState state, float speed, float flowFraction, bool canStart, bool canStop)`

### 7.2 RCSGaugeTypes.cs

Extended gauge type definitions:

```csharp
public enum RCSGaugeType
{
    // Loop Temperatures
    Loop1_THot, Loop2_THot, Loop3_THot, Loop4_THot,
    Loop1_TCold, Loop2_TCold, Loop3_TCold, Loop4_TCold,
    
    // Flow Rates
    TotalFlow,
    Loop1_Flow, Loop2_Flow, Loop3_Flow, Loop4_Flow,
    
    // Derived
    CorePower_MW,
    CoreDeltaT,
    AverageTAvg,
    
    // RCP Status
    RCP1_Speed, RCP2_Speed, RCP3_Speed, RCP4_Speed
}
```

---

## 8. Stage 4: Blender Arc Gauge Script

### 8.1 BlenderArcGauge_v1.py

A comprehensive Blender 5.0 Python script that creates parametric arc gauges.

**Configuration Parameters:**
- `gauge_radius`: Overall gauge size
- `sweep_angle`: Arc sweep (default 270°)
- `min_value`, `max_value`: Scale range
- `major_tick_interval`, `minor_tick_interval`: Tick spacing
- `warning_zones`: List of (start, end, color) tuples
- `danger_zones`: List of (start, end, color) tuples
- `label_text`: Gauge label
- `units_text`: Unit label

**Generated Objects:**
```
ArcGauge_{name}
├── Bezel (circular frame)
├── Face (background plate)
├── Scale
│   ├── MajorTicks
│   ├── MinorTicks
│   ├── Numbers
│   └── ColorZones
├── Needle (separate for animation)
├── NeedleCenter (pivot hub)
├── Label
├── Units
└── DigitalDisplay (optional)
```

**Key Features:**
- Procedural mesh generation
- PBR materials compatible with Unity URP
- Separate needle object with correct pivot point
- Exported as FBX with proper hierarchy
- Color zones for warning/danger indication

---

## 9. Stage 5: Blender Temperature Gauge Script

### 9.1 BlenderTemperatureGauge_v1.py

Creates vertical bar-style temperature gauges.

**Configuration Parameters:**
- `gauge_height`: Overall height
- `gauge_width`: Bar width
- `min_temp`, `max_temp`: Temperature range
- `tick_interval`: Scale tick spacing
- `fill_color_gradient`: List of (position, color) for gradient
- `label_text`: Gauge label
- `units_text`: Unit label (°F or °C)

**Generated Objects:**
```
TempGauge_{name}
├── Housing (outer frame)
├── Bulb (bottom reservoir - thermometer style)
├── Tube (main vertical tube)
├── Fill (animated level indicator)
├── Scale
│   ├── Ticks
│   └── Numbers
├── Label
└── Units
```

**Key Features:**
- Thermometer visual style
- Animated fill level (separate mesh for shader control)
- Color gradient based on temperature
- Scale with temperature markings

---

## 10. Stage 6: Blender-Unity Export Manual

### 10.1 Blender5_Unity6_Export_Manual.md

Comprehensive guide covering:

**Part A: Blender Model Preparation**
1. Object naming conventions
2. Hierarchy organization
3. Pivot point placement
4. Material setup for Unity URP
5. UV mapping requirements

**Part B: FBX Export Settings**
1. Object selection
2. Scale and units
3. Axis orientation (-Z Forward, Y Up)
4. Geometry settings
5. Animation settings (if applicable)

**Part C: Unity Import Configuration**
1. Model tab settings
2. Rig tab settings
3. Materials tab settings
4. Animation tab settings

**Part D: Material Setup in Unity**
1. URP Lit shader configuration
2. Emission for indicator lights
3. Texture import settings
4. Material property adjustments

**Part E: Prefab Creation**
1. Creating prefabs from imported models
2. Adding components
3. Configuring for runtime use

**Part F: Troubleshooting**
1. Scale issues
2. Material problems
3. Hierarchy issues
4. Performance optimization

---

## 11. Stage 7: Assembly Instructions

### 11.1 RCS_Screen_Assembly_Guide.md

Step-by-step guide for assembling the RCS screen in Unity:

**Part A: Prerequisites**
- Verify all scripts are in place
- Check model import
- Confirm materials configured

**Part B: Layer Setup**
- Create "RCSVisualization" layer
- Configure camera culling

**Part C: Canvas Creation**
- Create Screen Space - Overlay canvas
- Set up panel structure
- Configure layout anchors

**Part D: 3D Visualization Setup**
- Create RenderTexture
- Setup visualization camera
- Instantiate model
- Add RCSVisualizationController
- Configure materials

**Part E: Gauge Panel Setup**
- Create gauge prefab instances
- Configure gauge types and ranges
- Wire up to screen script

**Part F: RCP Control Panel Setup**
- Create 4 RCP panels
- Add buttons and indicators
- Configure callbacks

**Part G: Script Wiring**
- Attach RCSPrimaryLoopScreen to Canvas
- Wire all inspector references
- Test screen toggle

**Part H: Testing Checklist**
- Screen visibility toggle
- Gauge updates
- RCP controls
- 3D model rendering
- Performance verification

---

## 12. Unaddressed Issues

### 12.1 Issues Planned for Future Release

**Per-Loop Temperature Resolution**
- **Issue**: Current simulation uses lumped single-loop model; all 4 loops show identical temperatures
- **Impact**: Cannot simulate asymmetric transients (e.g., single SG tube leak)
- **Plan**: Phase 9 - Advanced Thermal-Hydraulics
- **Action**: Document in Future_Features

**Manual RCP Control**
- **Issue**: RCP starts are currently automatic based on simulation state
- **Impact**: Limited operator training for manual operations
- **Plan**: Phase 7 - Manual Heatup Operations
- **Action**: Control panels prepared but functionality limited

**Gauge Animation Smoothing**
- **Issue**: Needle movement may be jerky at high time compression
- **Impact**: Visual quality
- **Plan**: Future enhancement if needed
- **Action**: Monitor feedback

### 12.2 Issues Not Addressed (Out of Scope)

**Full 3D Control Room**
- Not planned; 2D/2.5D screens match actual control room style

**VR/AR Support**
- Out of scope for current implementation

**Multi-Monitor Support**
- Single screen assumed; enhancement possible later

---

## 13. Dependencies

### 13.1 Required Existing Components

| Component | Location | Status |
|-----------|----------|--------|
| MosaicGauge | Assets/Scripts/UI/MosaicGauge.cs | ✅ Exists |
| MosaicBoard | Assets/Scripts/UI/MosaicBoard.cs | ✅ Exists |
| MosaicTypes | Assets/Scripts/UI/MosaicTypes.cs | ✅ Exists |
| HeatupSimEngine | Assets/Scripts/Reactor/ | ✅ Exists |
| RCPSequencer | Assets/Scripts/Physics/RCPSequencer.cs | ✅ Exists |
| PlantConstants | Assets/Scripts/Physics/PlantConstants*.cs | ✅ Exists |

### 13.2 Required Unity Packages

| Package | Purpose |
|---------|---------|
| Universal Render Pipeline (URP) | Rendering |
| TextMeshPro | Text display |

### 13.3 Required Software

| Software | Version | Purpose |
|----------|---------|---------|
| Blender | 5.0+ | Gauge model creation |
| Unity | 6.3 (2022.3 LTS compatible) | Game engine |

---

## 14. Testing and Validation

### 14.1 Screen Functionality Tests

| Test | Expected Result |
|------|-----------------|
| Press Key 2 | RCS screen appears |
| Press Key 2 again | RCS screen hides |
| Press Key 1 then Key 2 | Reactor Core hides, RCS shows |
| 3D model visible | Renders correctly in center panel |
| Gauges update | Values match simulation state |
| RCP status | Shows correct Running/Stopped state |
| Alarms display | Shows timestamped entries |

### 14.2 Gauge Accuracy Tests

| Gauge | Test Condition | Expected Value |
|-------|----------------|----------------|
| T-hot | HZP conditions | ~557°F ± 5°F |
| T-cold | HZP conditions | ~554°F ± 5°F |
| Total Flow | 4 RCPs running | ~354,000 gpm |
| Core Power | Heatup (RCP heat only) | ~21 MW |
| Core ΔT | HZP | 5-15°F |

### 14.3 Performance Requirements

- Frame rate: ≥60 FPS with screen visible
- Memory: <100 MB for screen resources
- Load time: <1 second screen toggle

---

## 15. Approval and Next Steps

### 15.1 Required Approvals

This Implementation Plan requires explicit user approval before proceeding to Stage 1 implementation.

**User should review and approve:**
1. Stage breakdown and file structure
2. Technical specifications and gauge ranges
3. Blender script approach
4. Export/Import manual scope

### 15.2 Questions for User

1. **Model Status**: Has the `RCS_Primary_Loop.fbx` been fully imported with all materials configured, or does it need additional setup?

2. **Gauge Style Preference**: For the Blender gauges, do you prefer:
   - (A) Realistic industrial gauges (like actual control room)
   - (B) Stylized modern gauges (clean, digital look)
   - (C) Mix of both

3. **Existing Scripts**: The folder `Updates/Screen2_RCS_Primary_Loop/` already contains some scripts. Should I:
   - (A) Replace them entirely with new implementations
   - (B) Update/enhance the existing scripts
   - (C) Review and merge best features from both

4. **ScreenManager**: Does a `ScreenManager.cs` already exist in the project, or should Stage 1 create it?

5. **Priority**: Should I prioritize:
   - (A) Getting the RCS screen working first
   - (B) Creating the Blender gauge scripts first
   - (C) Both in parallel

### 15.3 Post-Approval Actions

Once approved:

1. **Create Changelog**: `Changelogs/CHANGELOG_v1.2.0.md`
2. **Update Future_Features**: Add per-loop resolution item
3. **Begin Stage 1**: Create base infrastructure scripts
4. **Report completion** after each stage for approval before proceeding

---

## 16. Implementation Schedule

| Stage | Estimated Effort | Cumulative |
|-------|------------------|------------|
| Stage 1 | ~200 lines C# | ~200 lines |
| Stage 2 | ~500 lines C# | ~700 lines |
| Stage 3 | ~300 lines C# | ~1,000 lines |
| Stage 4 | ~400 lines Python | ~1,400 lines |
| Stage 5 | ~350 lines Python | ~1,750 lines |
| Stage 6 | ~15 KB documentation | - |
| Stage 7 | ~10 KB documentation | - |

**Total**: ~1,750 lines code + ~25 KB documentation

---

**END OF IMPLEMENTATION PLAN**

**Ready for User Review and Approval**
