# RCS Primary Loop Screen Assembly Guide

**Version:** 1.0.0  
**Date:** 2026-02-09  
**Project:** CRITICAL - Nuclear Reactor Simulator  
**Screen:** RCS Primary Loop (Key 2)

---

## Table of Contents

1. [Overview](#1-overview)
2. [Prerequisites](#2-prerequisites)
3. [Part A: Scene Setup](#3-part-a-scene-setup)
4. [Part B: Layer Configuration](#4-part-b-layer-configuration)
5. [Part C: Canvas Creation](#5-part-c-canvas-creation)
6. [Part D: 3D Visualization Setup](#6-part-d-3d-visualization-setup)
7. [Part E: Gauge Panel Setup](#7-part-e-gauge-panel-setup)
8. [Part F: RCP Control Panel Setup](#8-part-f-rcp-control-panel-setup)
9. [Part G: Script Wiring](#9-part-g-script-wiring)
10. [Part H: Testing Checklist](#10-part-h-testing-checklist)
11. [Appendix: Component Reference](#11-appendix-component-reference)

---

## 1. Overview

This guide provides step-by-step instructions for assembling the RCS Primary Loop operator screen in Unity. The screen displays:

- 3D visualization of the 4-loop RCS system
- 8 temperature gauges (T-hot and T-cold for each loop)
- 8 flow/status gauges
- 4 RCP control panels
- Status displays and alarm panel

### Screen Layout

```
┌─────────────────────────────────────────────────────────────────────┐
│ RCS PRIMARY LOOP                    SIM TIME    MODE    COMPRESSION │ ← Status Bar
├────────┬────────────────────────────────────────┬───────────────────┤
│  LEFT  │              CENTER                    │       RIGHT       │
│  15%   │               50%                      │        35%        │
│        │                                        │                   │
│ T-HOT  │     ┌─────────────────────────┐       │    TOTAL FLOW     │
│ Loop 1 │     │                         │       │                   │
│ Loop 2 │     │    3D RCS Model         │       │    LOOP FLOWS     │
│ Loop 3 │     │    - 4 loops            │       │    1  2  3  4     │
│ Loop 4 │     │    - Animated flow      │       │                   │
│        │     │    - Color-coded temps  │       │    CORE POWER     │
│ T-COLD │     │                         │       │    CORE ΔT        │
│ Loop 1 │     └─────────────────────────┘       │    AVG T-AVG      │
│ Loop 2 │                                        │                   │
│ Loop 3 │                                        │                   │
│ Loop 4 │                                        │                   │
├────────┴────────────────────────────────────────┴───────────────────┤
│                         BOTTOM (26%)                                 │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌───────┐ ┌──────┐│
│  │  RCP-1  │ │  RCP-2  │ │  RCP-3  │ │  RCP-4  │ │STATUS │ │ALARMS││
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘ └───────┘ └──────┘│
└─────────────────────────────────────────────────────────────────────┘
```

---

## 2. Prerequisites

### Required Assets

Verify the following files exist in your project:

#### Scripts (Assets/Scripts/UI/)
```
☐ OperatorScreen.cs          - Base class for screens
☐ ScreenManager.cs           - Screen navigation manager
☐ RCSPrimaryLoopScreen.cs    - Main screen controller
☐ RCSVisualizationController.cs - 3D model controller
☐ RCPControlPanel.cs         - RCP control UI component
☐ RCSGaugeTypes.cs           - Extended gauge type definitions
☐ MosaicGauge.cs             - Gauge display component (existing)
☐ MosaicBoard.cs             - Data provider (existing)
```

#### 3D Models (Assets/Models/RCS/)
```
☐ RCS_Primary_Loop.fbx       - Imported Blender model
```

#### Materials (Assets/Materials/RCS/)
```
☐ MAT_HotLeg.mat
☐ MAT_ColdLeg.mat
☐ MAT_CrossoverLeg.mat
☐ MAT_RCP.mat
☐ MAT_ReactorVessel.mat
☐ MAT_SteamGenerator.mat
☐ MAT_Pressurizer.mat
```

### Required Packages

Ensure these packages are installed via Package Manager:

```
☐ TextMeshPro (com.unity.textmeshpro)
☐ Universal RP (com.unity.render-pipelines.universal)
```

### Scene Requirements

```
☐ ReactorController in scene (or HeatupSimEngine)
☐ MosaicBoard in scene
☐ ScreenManager in scene (create if not present)
```

---

## 3. Part A: Scene Setup

### 3.1 Create ScreenManager (if not present)

1. GameObject → Create Empty
2. Name: `ScreenManager`
3. Add Component: `ScreenManager` (from Critical.UI namespace)
4. Configure in Inspector:
   ```
   Mutual Exclusion:      ☑ (only one screen at a time)
   Allow No Screen:       ☑ (can hide all screens)
   Default Screen Index:  1 (Reactor Core screen)
   Enable Keyboard Input: ☑
   Debug Logging:         ☑ (for testing)
   ```

### 3.2 Verify Existing Components

Check that these GameObjects exist:

| GameObject | Component | Purpose |
|------------|-----------|---------|
| ReactorController | ReactorController | Simulation data source |
| MosaicBoard | MosaicBoard | Gauge data provider |

If using HeatupSimEngine instead of ReactorController, the RCS screen will attempt to find it automatically.

---

## 4. Part B: Layer Configuration

### 4.1 Create Visualization Layer

The 3D visualization uses a separate layer for camera culling.

1. Edit → Project Settings → Tags and Layers
2. Find an unused Layer slot (e.g., Layer 10)
3. Name it: `RCSVisualization`

### 4.2 Note the Layer Number

Record which layer number you used: **Layer ___**

This will be needed when configuring the visualization camera.

---

## 5. Part C: Canvas Creation

### 5.1 Create Main Canvas

1. GameObject → UI → Canvas
2. Name: `Canvas_RCS_Screen`
3. Configure Canvas component:
   ```
   Render Mode:           Screen Space - Overlay
   Pixel Perfect:         ☐
   Sort Order:            10 (above other UI)
   ```

4. Configure Canvas Scaler:
   ```
   UI Scale Mode:         Scale With Screen Size
   Reference Resolution:  1920 x 1080
   Screen Match Mode:     Match Width Or Height
   Match:                 0.5 (balanced)
   ```

### 5.2 Add Required Components

Select Canvas_RCS_Screen and add:

1. Add Component: `RCSPrimaryLoopScreen`
2. Add Component: `Canvas Group` (for visibility control)
3. Add Component: `Image` (for background color)
   - Color: `#1A1A1F` (dark background)

### 5.3 Create Panel Structure

Create child panels under Canvas_RCS_Screen:

```
Canvas_RCS_Screen
├── Panel_StatusBar      (top 3%)
├── Panel_Left           (left 15%, below status)
├── Panel_Center         (center 50%)
├── Panel_Right          (right 35%)
└── Panel_Bottom         (bottom 26%)
```

#### Panel_StatusBar
1. Create: UI → Panel (child of Canvas)
2. Name: `Panel_StatusBar`
3. RectTransform anchors:
   ```
   Anchor Min: (0, 0.97)
   Anchor Max: (1, 1)
   Left/Right/Top/Bottom: 0
   ```
4. Image color: `#1E1E28`

#### Panel_Left
1. Create: UI → Panel
2. Name: `Panel_Left`
3. RectTransform anchors:
   ```
   Anchor Min: (0, 0.26)
   Anchor Max: (0.15, 0.97)
   ```
4. Image color: `#1E1E28`

#### Panel_Center
1. Create: UI → Panel
2. Name: `Panel_Center`
3. RectTransform anchors:
   ```
   Anchor Min: (0.15, 0.26)
   Anchor Max: (0.65, 0.97)
   ```
4. Image color: `#12121A` (darker for contrast)

#### Panel_Right
1. Create: UI → Panel
2. Name: `Panel_Right`
3. RectTransform anchors:
   ```
   Anchor Min: (0.65, 0.26)
   Anchor Max: (1, 0.97)
   ```
4. Image color: `#1E1E28`

#### Panel_Bottom
1. Create: UI → Panel
2. Name: `Panel_Bottom`
3. RectTransform anchors:
   ```
   Anchor Min: (0, 0)
   Anchor Max: (1, 0.26)
   ```
4. Image color: `#1E1E28`

---

## 6. Part D: 3D Visualization Setup

### 6.1 Create RenderTexture

1. Project window → Right-click → Create → Render Texture
2. Name: `RT_RCS_Visualization`
3. Location: `Assets/RenderTextures/`
4. Configure:
   ```
   Size:              960 x 720 (or higher for quality)
   Color Format:      ARGB32
   Depth Buffer:      24 bit
   Anti-aliasing:     4x
   ```

### 6.2 Add RawImage to Center Panel

1. Select `Panel_Center`
2. Create child: UI → Raw Image
3. Name: `Image_Visualization`
4. RectTransform: Stretch to fill panel (anchor 0,0 to 1,1)
5. Assign Texture: `RT_RCS_Visualization`

### 6.3 Create RCS Model Prefab

1. Locate `Assets/Models/RCS/RCS_Primary_Loop.fbx`
2. Drag into Hierarchy temporarily
3. Add Component: `RCSVisualizationController`
4. Configure materials if not auto-assigned
5. Identify and assign (in Inspector):
   - RCP Rotors: `RCP_1_Rotor`, `RCP_2_Rotor`, etc.
   - Flow Arrows: Objects containing "arrow" in name
6. Drag back to Project to create prefab:
   - Location: `Assets/Prefabs/RCS/`
   - Name: `RCS_Primary_Loop.prefab`
7. Delete instance from Hierarchy

### 6.4 Configure RCSPrimaryLoopScreen References

Select `Canvas_RCS_Screen` and in the RCSPrimaryLoopScreen component:

**3D Visualization Section:**
```
RCS Model Prefab:              [Assign RCS_Primary_Loop.prefab]
Visualization Display:         [Assign Image_Visualization]
Visualization Render Texture:  [Assign RT_RCS_Visualization]
Render Texture Resolution:     960 x 720
Camera Distance:               120
Camera Height:                 50
Auto Rotate Camera:            ☐ (off for now)
```

---

## 7. Part E: Gauge Panel Setup

### 7.1 Left Panel - Temperature Gauges

Create 8 gauge instances in Panel_Left:

#### T-HOT Gauges (Top Half)

For each gauge (Loop 1-4):

1. Create child of Panel_Left: Empty GameObject
2. Name: `Gauge_THot_Loop1` (through Loop4)
3. Add Component: `MosaicGauge`
4. Add UI elements (or use MosaicGauge prefab if available):
   - Background Image
   - Value Text (TextMeshPro)
   - Label Text
   - Optional: Analog dial elements

**Layout (vertical stack):**
```
Panel_Left
├── Label_THot ("T-HOT")
├── Gauge_THot_Loop1    (top)
├── Gauge_THot_Loop2
├── Gauge_THot_Loop3
├── Gauge_THot_Loop4
├── Label_TCold ("T-COLD")
├── Gauge_TCold_Loop1
├── Gauge_TCold_Loop2
├── Gauge_TCold_Loop3
└── Gauge_TCold_Loop4   (bottom)
```

Use Vertical Layout Group on Panel_Left for automatic arrangement:
```
Padding:           10 (all sides)
Spacing:           5
Child Alignment:   Upper Center
Control Child Size: ☑ Width, ☐ Height
Child Force Expand: ☑ Width, ☐ Height
```

### 7.2 Right Panel - Flow Gauges

Create gauges in Panel_Right:

```
Panel_Right
├── Gauge_TotalFlow     (large, top)
├── FlowGauges_Row      (horizontal group)
│   ├── Gauge_Flow_Loop1
│   ├── Gauge_Flow_Loop2
│   ├── Gauge_Flow_Loop3
│   └── Gauge_Flow_Loop4
├── Gauge_CorePower
├── Gauge_CoreDeltaT
└── Gauge_AverageTavg
```

### 7.3 Wire Gauge References

In RCSPrimaryLoopScreen Inspector, assign all gauge references:

**Left Panel - Temperature Gauges:**
```
Gauge Loop1 T Hot:    [Gauge_THot_Loop1]
Gauge Loop2 T Hot:    [Gauge_THot_Loop2]
Gauge Loop3 T Hot:    [Gauge_THot_Loop3]
Gauge Loop4 T Hot:    [Gauge_THot_Loop4]
Gauge Loop1 T Cold:   [Gauge_TCold_Loop1]
Gauge Loop2 T Cold:   [Gauge_TCold_Loop2]
Gauge Loop3 T Cold:   [Gauge_TCold_Loop3]
Gauge Loop4 T Cold:   [Gauge_TCold_Loop4]
```

**Right Panel - Flow Gauges:**
```
Gauge Total Flow:     [Gauge_TotalFlow]
Gauge Loop1 Flow:     [Gauge_Flow_Loop1]
Gauge Loop2 Flow:     [Gauge_Flow_Loop2]
Gauge Loop3 Flow:     [Gauge_Flow_Loop3]
Gauge Loop4 Flow:     [Gauge_Flow_Loop4]
Gauge Core Power:     [Gauge_CorePower]
Gauge Core Delta T:   [Gauge_CoreDeltaT]
Gauge Average Tavg:   [Gauge_AverageTavg]
```

---

## 8. Part F: RCP Control Panel Setup

### 8.1 Create RCP Panel Layout

In Panel_Bottom, create the RCP control section:

```
Panel_Bottom
├── RCP_Panels_Container (Horizontal Layout Group)
│   ├── RCPPanel_1
│   ├── RCPPanel_2
│   ├── RCPPanel_3
│   └── RCPPanel_4
├── Panel_Status
└── Panel_Alarms
```

### 8.2 Create Individual RCP Panel

For each RCP panel (create one, then duplicate):

1. Create: UI → Panel
2. Name: `RCPPanel_1`
3. Size: ~200 x 180 pixels
4. Add Component: `RCPControlPanel`

**Child Structure:**
```
RCPPanel_1
├── Text_Label          (TMP: "RCP-1")
├── Image_StatusIndicator (colored circle)
├── Text_Status         (TMP: "STOPPED")
├── Text_Speed          (TMP: "Speed: --- RPM")
├── Text_Flow           (TMP: "Flow: 0.0%")
├── Text_Amps           (TMP: "Amps: --- A")
├── Button_Start        (UI Button)
│   └── Text            (TMP: "START")
└── Button_Stop         (UI Button)
    └── Text            (TMP: "STOP")
```

### 8.3 Configure RCPControlPanel Component

For each panel, assign references in Inspector:

**Labels:**
```
Text Pump Label:       [Text_Label]
Text Status:           [Text_Status]
Text Speed:            [Text_Speed]
Text Flow:             [Text_Flow]
Text Amps:             [Text_Amps]
```

**Indicators:**
```
Indicator Status:      [Image_StatusIndicator]
Indicator Running:     [Optional second indicator]
Indicator Stopped:     [Optional third indicator]
Bar Speed Fill:        [Optional progress bar]
```

**Buttons:**
```
Button Start:          [Button_Start]
Button Stop:           [Button_Stop]
Indicator Interlock:   [Optional interlock indicator]
```

**Colors:**
```
Color Running:         #33E633 (bright green)
Color Stopped:         #808080 (gray)
Color Ramping:         #FFE633 (yellow)
Color Tripped:         #E63333 (red)
```

### 8.4 Duplicate for All 4 RCPs

1. Duplicate RCPPanel_1 three times
2. Rename: RCPPanel_2, RCPPanel_3, RCPPanel_4
3. Update label text in each

### 8.5 Wire RCP Panel References

In RCSPrimaryLoopScreen Inspector:

```
RCP Panel 1:    [RCPPanel_1]
RCP Panel 2:    [RCPPanel_2]
RCP Panel 3:    [RCPPanel_3]
RCP Panel 4:    [RCPPanel_4]
```

### 8.6 Create Status Panel

In Panel_Bottom, create Panel_Status:

```
Panel_Status
├── Text_RCPCount       (TMP: "RCPs: 0/4")
├── Text_CirculationMode (TMP: "NATURAL CIRCULATION")
├── Text_PlantMode      (TMP: "MODE 5")
└── Indicator_NaturalCirc (Image, indicator light)
```

Wire references in RCSPrimaryLoopScreen:
```
Text RCP Count:         [Text_RCPCount]
Text Circulation Mode:  [Text_CirculationMode]
Text Plant Mode:        [Text_PlantMode]
Indicator Natural Circ: [Indicator_NaturalCirc]
```

### 8.7 Create Alarm Panel

In Panel_Bottom, create Panel_Alarms:

```
Panel_Alarms
├── Text_AlarmHeader    (TMP: "ALARMS")
└── ScrollView_Alarms
    └── Viewport
        └── Content     (Vertical Layout Group)
            └── [Alarm entries will be added dynamically]
```

Configure Scroll View:
- Vertical scrolling only
- Content: Vertical Layout Group (spacing: 2)
- Content Size Fitter: Vertical Fit = Preferred Size

Wire references:
```
Alarm List Container:   [Content object from ScrollView]
Alarm Entry Prefab:     [Optional - create a simple text prefab]
Max Alarm Entries:      8
```

---

## 9. Part G: Script Wiring

### 9.1 Final RCSPrimaryLoopScreen Configuration

Select Canvas_RCS_Screen and verify all Inspector fields:

**Screen Settings:**
```
Start Visible:          ☐ (hidden on start)
Allow Keyboard Toggle:  ☑
```

**Color Theme:**
```
Background Color:       #1A1A1F
Panel Color:            #1E1E28
Border Color:           #2A2A35
Text Color:             #E6E6E6
Accent Color:           #0099FF
```

**3D Visualization:** (from Part D)
```
[All fields assigned]
```

**Left Panel Gauges:** (from Part E)
```
[All 8 temperature gauge references]
```

**Right Panel Gauges:** (from Part E)
```
[All 8 flow/power gauge references]
```

**Bottom Panel RCP Controls:** (from Part F)
```
[All 4 RCP panel references]
```

**Status Displays:** (from Part F)
```
[All status text and indicator references]
```

**Alarm Panel:** (from Part F)
```
[Container and prefab references]
```

### 9.2 Verify ScreenManager Registration

The screen will auto-register on Start(), but verify:

1. Play the scene
2. Check Console for: `[ScreenManager] Registered screen 2: 'RCS PRIMARY LOOP'`
3. Press Key 2 - screen should toggle

---

## 10. Part H: Testing Checklist

### 10.1 Basic Functionality

```
☐ Press Key 2 - Screen toggles visible/hidden
☐ Press Key 1 - Switches to Reactor Core screen (if present)
☐ Press Key 2 again - Returns to RCS screen
☐ Screen title shows "RCS PRIMARY LOOP"
☐ Status bar shows simulation time
```

### 10.2 3D Visualization

```
☐ RCS model renders in center panel
☐ Model is positioned correctly (centered, visible)
☐ Camera angle is appropriate
☐ If auto-rotate enabled: model rotates smoothly
☐ Piping colors respond to temperature (if sim running)
```

### 10.3 Temperature Gauges

```
☐ All 8 temperature gauges visible
☐ Labels show correct loop numbers
☐ Values display (may show default if no sim data)
☐ At HZP: T-hot ≈ 557°F, T-cold ≈ 554°F
```

### 10.4 Flow Gauges

```
☐ Total flow gauge visible and labeled
☐ Per-loop flow gauges visible
☐ Core power gauge shows low value during heatup
☐ Delta-T gauge shows ~3°F at HZP
```

### 10.5 RCP Control Panels

```
☐ All 4 RCP panels visible
☐ Labels show RCP-1 through RCP-4
☐ Status shows "STOPPED" initially
☐ START button enabled when pump can start
☐ STOP button enabled when pump is running
☐ Clicking START initiates pump (check console log)
☐ Status changes: STOPPED → RAMPING → RUNNING
☐ Speed and flow displays update during ramp-up
```

### 10.6 Status Displays

```
☐ RCP count shows "RCPs: X/4"
☐ Circulation mode shows NATURAL or FORCED
☐ Plant mode shows MODE 3/4/5 based on temperature
☐ Natural circulation indicator lights when no RCPs running
```

### 10.7 Alarms

```
☐ Alarm panel visible
☐ Starting RCP adds alarm entry
☐ Stopping RCP adds alarm entry
☐ Alarms show timestamp
☐ Old alarms scroll off (max 8)
```

### 10.8 Performance

```
☐ Framerate remains ≥60 FPS with screen visible
☐ No console errors or warnings
☐ Smooth gauge updates (no flickering)
☐ Memory usage stable (check Profiler)
```

---

## 11. Appendix: Component Reference

### RCSPrimaryLoopScreen Properties

| Property | Type | Description |
|----------|------|-------------|
| ToggleKey | KeyCode | Alpha2 (Key 2) |
| ScreenName | string | "RCS PRIMARY LOOP" |
| ScreenIndex | int | 2 |
| rcsModelPrefab | GameObject | Blender model prefab |
| visualizationDisplay | RawImage | Display for 3D render |
| visualizationRenderTexture | RenderTexture | RT for 3D camera |
| gauge_Loop[1-4]_THot | MosaicGauge | T-hot gauges |
| gauge_Loop[1-4]_TCold | MosaicGauge | T-cold gauges |
| gauge_TotalFlow | MosaicGauge | Total RCS flow |
| gauge_Loop[1-4]_Flow | MosaicGauge | Per-loop flow |
| gauge_CorePower | MosaicGauge | Core thermal power |
| gauge_CoreDeltaT | MosaicGauge | Core delta-T |
| gauge_AverageTavg | MosaicGauge | Average T-avg |
| rcpPanel_[1-4] | RCPControlPanel | RCP control panels |

### RCPControlPanel Properties

| Property | Type | Description |
|----------|------|-------------|
| text_PumpLabel | TMP_Text | "RCP-X" label |
| text_Status | TMP_Text | Status text |
| text_Speed | TMP_Text | Speed in RPM |
| text_Flow | TMP_Text | Flow percentage |
| text_Amps | TMP_Text | Motor current |
| indicator_Status | Image | Main status light |
| button_Start | Button | START button |
| button_Stop | Button | STOP button |

### RCSVisualizationController Properties

| Property | Type | Description |
|----------|------|-------------|
| temperatureGradient | Gradient | Color gradient for temp |
| minTemperature | float | 100°F |
| maxTemperature | float | 650°F |
| rcpRotors | Transform[] | 4 rotor transforms |
| maxRotorSpeed | float | 720°/second |
| flowArrows | Transform[] | Flow indicator arrows |
| hotLegMaterials | Material[] | Hot leg materials |
| coldLegMaterials | Material[] | Cold leg materials |

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| 1 | Toggle Reactor Core Screen |
| 2 | Toggle RCS Primary Loop Screen |
| 3 | Toggle Pressurizer Screen (future) |
| 4 | Toggle CVCS Screen (future) |
| Tab | Toggle Plant Overview (future) |

---

## Document History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2026-02-09 | Initial release |

---

**End of Assembly Guide**
