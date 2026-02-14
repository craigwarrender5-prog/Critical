# RCS Primary Loop Screen - Assembly Instructions

## Complete Step-by-Step Guide

**Version:** 1.0.0  
**Date:** 2026-02-09  
**For:** Critical Nuclear Reactor Simulator - Screen 2

---

## Table of Contents

1. [Overview](#1-overview)
2. [Prerequisites](#2-prerequisites)
3. [Part A: Create the Blender Model](#3-part-a-create-the-blender-model)
4. [Part B: Import Model to Unity](#4-part-b-import-model-to-unity)
5. [Part C: Create the UI Canvas](#5-part-c-create-the-ui-canvas)
6. [Part D: Setup the 3D Visualization](#6-part-d-setup-the-3d-visualization)
7. [Part E: Create Gauge Panels](#7-part-e-create-gauge-panels)
8. [Part F: Create RCP Control Panel](#8-part-f-create-rcp-control-panel)
9. [Part G: Wire Up the Scripts](#9-part-g-wire-up-the-scripts)
10. [Part H: Testing and Validation](#10-part-h-testing-and-validation)
11. [Troubleshooting](#11-troubleshooting)

---

## 1. Overview

The RCS Primary Loop Screen (Key 2) displays the reactor coolant system with:

```
┌─────────────────────────────────────────────────────────────────────────┐
│ RCS PRIMARY LOOP                         SIM TIME    MODE 4    16x     │
├──────────┬──────────────────────────────────────────────┬───────────────┤
│          │                                              │               │
│ Loop 1   │                                              │ Total Flow    │
│ T-hot    │                                              │ [====] 354K   │
│ [====]   │         ┌────┐      ┌────┐                   │               │
│ 618°F    │         │SG-1│      │SG-2│                   │ Loop 1 Flow   │
│          │         └──┬─┘      └─┬──┘                   │ [====] 88.5K  │
│ Loop 2   │       HL1 │    ┌─┐   │ HL2                   │               │
│ T-hot    │           └────┤ ├───┘                       │ Loop 2 Flow   │
│ [====]   │                │RV│                          │ [====] 88.5K  │
│ 618°F    │           ┌────┤ ├───┐                       │               │
│          │       HL4 │    └─┘   │ HL3                   │ Loop 3 Flow   │
│ Loop 3   │         ┌─┴──┐      ┌┴───┐                   │ [====] 88.5K  │
│ T-hot    │         │SG-4│      │SG-3│                   │               │
│ [====]   │         └────┘      └────┘                   │ Loop 4 Flow   │
│ 618°F    │                                              │ [====] 88.5K  │
│          │      [3D Visualization with                  │               │
│ Loop 4   │       animated flow arrows]                  │ Core Power    │
│ T-hot    │                                              │ [====] 21 MW  │
│ [====]   │                                              │               │
│ 618°F    │                                              │ Core ΔT       │
│          │                                              │ [====] 63°F   │
│ Loop 1-4 │                                              │               │
│ T-cold   │                                              │ Avg T-avg     │
│ (below)  │                                              │ [====] 588°F  │
├──────────┴──────────────────────────────────────────────┴───────────────┤
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐                     │
│  │ RCP-1   │  │ RCP-2   │  │ RCP-3   │  │ RCP-4   │   NAT CIRC: STBY   │
│  │ RUNNING │  │ RUNNING │  │ RUNNING │  │ RUNNING │   RCPs: 4/4        │
│  │ 1200rpm │  │ 1200rpm │  │ 1200rpm │  │ 1200rpm │   MODE: 3          │
│  │[START]  │  │[START]  │  │[START]  │  │[START]  │                     │
│  │[STOP ]  │  │[STOP ]  │  │[STOP ]  │  │[STOP ]  │   ALARMS:          │
│  └─────────┘  └─────────┘  └─────────┘  └─────────┘   (alarm list)     │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Prerequisites

### Files Required

Make sure you have these files from `Critical\Updates\Screen2_RCS_Primary_Loop\`:

| File | Purpose |
|------|---------|
| `RCS_Technical_Specifications.md` | Technical reference |
| `RCS_Primary_Loop_Blender.py` | Blender model generator script |
| `Blender_Unity_Export_Manual.md` | Export/import guide |
| `RCSPrimaryLoopScreen.cs` | Main screen controller |
| `Screen2_Assembly_Instructions.md` | This document |

### Software Required

- Blender 5.0+
- Unity 2022.3 LTS with URP
- TextMeshPro package

### Existing Project Components

Verify these exist in your project:
- `MosaicGauge.cs` - Gauge component
- `OperatorScreen.cs` - Base screen class
- `ScreenManager.cs` - Screen management system
- `HeatupSimEngine.cs` - Simulation engine

---

## 3. Part A: Create the Blender Model

### Step 1: Open Blender and Run Script

1. Launch **Blender 5.0**
2. Go to **Scripting** workspace (top tab bar)
3. Click **+ New** to create a new text block
4. Open `RCS_Primary_Loop_Blender.py` in a text editor
5. Copy all contents and paste into Blender's text editor
6. Click **Run Script** (or press Alt+P)

### Step 2: Verify Model Creation

Check the Outliner (top-right panel) for this hierarchy:
```
RCS_Primary_Loop
├── Components
│   ├── ReactorVessel
│   ├── SteamGenerator_1
│   ├── SteamGenerator_2
│   ├── SteamGenerator_3
│   ├── SteamGenerator_4
│   ├── RCP_1
│   ├── RCP_2
│   ├── RCP_3
│   ├── RCP_4
│   └── Pressurizer
├── Piping
│   ├── HotLeg_1 ... HotLeg_4
│   ├── ColdLeg_1 ... ColdLeg_4
│   ├── CrossoverLeg_1 ... CrossoverLeg_4
│   └── SurgeLine
├── FlowArrows
│   └── FlowArrow_* (8 total)
└── Labels
    └── Label_Loop_* (4 total)
```

### Step 3: Export as FBX

1. Select **RCS_Primary_Loop** in the Outliner
2. **File > Export > FBX (.fbx)**
3. Navigate to: `Critical\Assets\Models\RCS\`
4. Name: `RCS_Primary_Loop.fbx`
5. Configure export settings (see `Blender_Unity_Export_Manual.md`):
   - Selected Objects: ✅
   - Scale: 1.00
   - Apply Scalings: FBX All
   - Forward: -Z Forward
   - Up: Y Up
6. Click **Export FBX**

---

## 4. Part B: Import Model to Unity

### Step 1: Create Folder Structure

In Unity Project window, create:
```
Assets/
├── Models/
│   └── RCS/
├── Materials/
│   └── RCS/
├── Prefabs/
│   └── Screens/
│       └── RCSPrimaryLoop/
├── RenderTextures/
└── Scripts/
    └── UI/
        └── Screens/
```

### Step 2: Import FBX

1. Locate `RCS_Primary_Loop.fbx` in `Assets/Models/RCS/`
2. Select it and configure in Inspector:

**Model Tab:**
| Setting | Value |
|---------|-------|
| Scale Factor | 1 |
| Convert Units | ✅ |
| Preserve Hierarchy | ✅ |
| Read/Write | ✅ |
| Generate Lightmap UVs | ✅ |

**Rig Tab:**
| Setting | Value |
|---------|-------|
| Animation Type | None |

**Materials Tab:**
| Setting | Value |
|---------|-------|
| Material Creation Mode | Import via MaterialDescription |
| Location | Use External Materials |

3. Click **Apply**

### Step 3: Extract and Configure Materials

1. Go to **Materials** tab
2. Click **Extract Materials...**
3. Select `Assets/Materials/RCS/`
4. Configure each material for URP (see `Blender_Unity_Export_Manual.md`)

### Step 4: Create Layer for Visualization

1. **Edit > Project Settings > Tags and Layers**
2. Add new Layer: `RCSVisualization` (e.g., Layer 8)

---

## 5. Part C: Create the UI Canvas

### Step 1: Create Screen Canvas

1. In Hierarchy, right-click: **UI > Canvas**
2. Rename to `RCSPrimaryLoopCanvas`
3. Configure Canvas component:
   - Render Mode: **Screen Space - Overlay**
   - UI Scale Mode: **Scale With Screen Size**
   - Reference Resolution: **1920 x 1080**
   - Match: **0.5** (width-height balance)

### Step 2: Add Canvas Group

1. Select the Canvas
2. Add Component: **Canvas Group**
3. This allows fading/toggling the entire screen

### Step 3: Create Panel Structure

Create this hierarchy under the Canvas:

```
RCSPrimaryLoopCanvas
├── Background (Image - dark background)
├── TitleBar (Panel)
│   ├── TitleText (TextMeshPro)
│   ├── SimTimeDisplay (TextMeshPro)
│   ├── ModeDisplay (TextMeshPro)
│   └── SpeedDisplay (TextMeshPro)
├── LeftPanel (Panel)
│   └── GaugeContainer (Vertical Layout Group)
├── CenterPanel (Panel)
│   └── VisualizationDisplay (RawImage)
├── RightPanel (Panel)
│   └── GaugeContainer (Vertical Layout Group)
└── BottomPanel (Panel)
    ├── RCPControlsContainer (Horizontal Layout Group)
    ├── StatusContainer (Panel)
    └── AlarmContainer (Vertical Layout Group)
```

### Step 4: Configure Panel Anchors and Sizes

**Background:**
- Anchor: Stretch-Stretch (all corners)
- Left/Right/Top/Bottom: 0
- Color: #1A1A1F

**TitleBar:**
- Anchor: Top-Stretch
- Height: 32
- Color: #1E1E28

**LeftPanel:**
- Anchor Min: (0, 0.26)
- Anchor Max: (0.15, 0.97)
- Color: #1E1E28 with slight transparency

**CenterPanel:**
- Anchor Min: (0.15, 0.26)
- Anchor Max: (0.65, 0.97)
- Color: Transparent (shows 3D render)

**RightPanel:**
- Anchor Min: (0.65, 0.26)
- Anchor Max: (1.0, 0.97)
- Color: #1E1E28 with slight transparency

**BottomPanel:**
- Anchor Min: (0, 0)
- Anchor Max: (1.0, 0.26)
- Color: #1E1E28

---

## 6. Part D: Setup the 3D Visualization

### Step 1: Create Render Texture

1. In Project window: **Right-click > Create > Render Texture**
2. Save to: `Assets/RenderTextures/RCS_Visualization_RT`
3. Configure:
   - Size: 960 x 720
   - Color Format: ARGB32
   - Depth Buffer: At least 24 bits
   - Anti-aliasing: 4x

### Step 2: Create Visualization Camera

1. In Hierarchy: **Right-click > Camera**
2. Rename to `RCS_Visualization_Camera`
3. Position at: (1000, 50, -100)
4. Configure Camera component:
   - Clear Flags: Solid Color
   - Background: #1A1A1F
   - Culling Mask: Only `RCSVisualization`
   - Target Texture: `RCS_Visualization_RT`
   - Field of View: 60

### Step 3: Place the 3D Model

1. Drag `RCS_Primary_Loop.fbx` into Hierarchy
2. Rename instance to `RCS_Visual_Instance`
3. Position at: (1000, 0, 0) - far from main scene
4. Set layer to `RCSVisualization` for all children:
   - Select the root
   - In Inspector, click Layer dropdown
   - Select `RCSVisualization`
   - When prompted, click "Yes, change children"

### Step 4: Point Camera at Model

1. Select `RCS_Visualization_Camera`
2. In Scene view, position it to see the model nicely
3. Or set Transform:
   - Position: (1000, 60, -120)
   - Rotation: (25, 0, 0)

### Step 5: Connect Render Texture to UI

1. Select `VisualizationDisplay` (RawImage) in CenterPanel
2. Assign `RCS_Visualization_RT` to Texture property

---

## 7. Part E: Create Gauge Panels

### Step 1: Create Gauge Prefab (if not existing)

Your `MosaicGauge` component should display:
- Label text
- Value display
- Unit display
- Optional bar/needle indicator
- Color bands for warning/danger zones

### Step 2: Add Left Panel Gauges

In `LeftPanel > GaugeContainer`:

1. Add **Vertical Layout Group** component:
   - Spacing: 8
   - Child Alignment: Upper Center
   - Child Force Expand Width: ✅
   - Child Force Expand Height: ❌

2. Create 8 gauge instances:
   - Loop 1 T-hot
   - Loop 2 T-hot
   - Loop 3 T-hot
   - Loop 4 T-hot
   - Loop 1 T-cold
   - Loop 2 T-cold
   - Loop 3 T-cold
   - Loop 4 T-cold

3. For each gauge, configure:
   - Preferred Height: ~80
   - Min Width: 200

### Step 3: Add Right Panel Gauges

In `RightPanel > GaugeContainer`:

1. Add **Vertical Layout Group** (same settings)

2. Create 8 gauge instances:
   - Total RCS Flow
   - Loop 1 Flow
   - Loop 2 Flow
   - Loop 3 Flow
   - Loop 4 Flow
   - Core Thermal Power
   - Core ΔT
   - Average T-avg

---

## 8. Part F: Create RCP Control Panel

### Step 1: Create RCP Panel Prefab

Create a prefab with this structure:
```
RCPControlPanel (Panel)
├── PumpNameLabel (TextMeshPro) - "RCP-1"
├── StatusIndicator (Image) - Green/Yellow/Red circle
├── StatusText (TextMeshPro) - "RUNNING"
├── SpeedDisplay (TextMeshPro) - "1200 rpm"
├── FlowBar (Slider) - Visual flow indicator
├── ButtonContainer (Horizontal Layout)
│   ├── StartButton (Button)
│   │   └── Text - "START"
│   └── StopButton (Button)
│       └── Text - "STOP"
└── (RCPControlPanel script attached)
```

### Step 2: Configure Prefab

1. Set preferred size: 180 x 160
2. Configure colors:
   - Panel background: #2A2A35
   - Running indicator: #00FF00
   - Stopped indicator: #808080
   - Ramping indicator: #FFFF00
   - Buttons: Standard Unity button style

### Step 3: Place 4 RCP Panels

In `BottomPanel > RCPControlsContainer`:

1. Add **Horizontal Layout Group**:
   - Spacing: 16
   - Child Alignment: Middle Center
   - Padding: 16 all sides

2. Instantiate 4 RCP panels

### Step 4: Add Status Section

In `BottomPanel > StatusContainer`:

Create:
- RCP Count Display: "RCPs: 4/4"
- Natural Circ Indicator + Label
- Mode Display: "MODE 3"

### Step 5: Add Alarm List

In `BottomPanel > AlarmContainer`:

1. Add **Vertical Layout Group**
2. Set max height for scrolling
3. Add **Scroll Rect** if needed
4. Create alarm entry prefab (simple text line)

---

## 9. Part G: Wire Up the Scripts

### Step 1: Copy Script Files

Copy these to `Assets/Scripts/UI/Screens/`:
- `RCSPrimaryLoopScreen.cs`

### Step 2: Add Main Script to Canvas

1. Select `RCSPrimaryLoopCanvas`
2. Add Component: `RCSPrimaryLoopScreen`

### Step 3: Assign Inspector References

In the `RCSPrimaryLoopScreen` component, assign:

**Panel References:**
- Left Panel → LeftPanel
- Center Panel → CenterPanel
- Right Panel → RightPanel
- Bottom Panel → BottomPanel

**3D Visualization:**
- RCS Visual Prefab → `RCS_Primary_Loop.fbx`
- Visualization Camera → `RCS_Visualization_Camera`
- Visualization RT → `RCS_Visualization_RT`
- Visualization Display → `VisualizationDisplay` (RawImage)

**Left Panel Gauges:**
- Loop 1 T-hot Gauge → (gauge reference)
- Loop 2 T-hot Gauge → (gauge reference)
- ... (all 8 gauges)

**Right Panel Gauges:**
- Total Flow Gauge → (gauge reference)
- ... (all 8 gauges)

**Bottom Panel:**
- RCP 1 Panel → (RCPControlPanel reference)
- RCP 2 Panel → (RCPControlPanel reference)
- RCP 3 Panel → (RCPControlPanel reference)
- RCP 4 Panel → (RCPControlPanel reference)
- RCP Count Display → (TextMeshPro reference)
- Natural Circ Status Display → (TextMeshPro reference)
- Natural Circ Indicator → (Image reference)
- Mode Display → (TextMeshPro reference)
- Alarm List Container → (Transform reference)
- Alarm Entry Prefab → (prefab reference)

### Step 4: Add RCSVisualController to 3D Model

1. Select `RCS_Visual_Instance`
2. Add Component: `RCSVisualController`
3. Assign references:
   - Reactor Vessel → ReactorVessel child
   - Steam Generators array → SG children
   - RCPs array → RCP children
   - Hot/Cold/Crossover leg renderers
   - Flow arrows

### Step 5: Register with Screen Manager

If using a central `ScreenManager`:

```csharp
// In ScreenManager.cs, add registration
public void RegisterScreen(OperatorScreen screen)
{
    screens[screen.ScreenIndex] = screen;
}
```

---

## 10. Part H: Testing and Validation

### Test 1: Screen Toggle

1. Enter Play mode
2. Press **2** key
3. Verify screen appears
4. Press **2** again
5. Verify screen hides

### Test 2: 3D Visualization

1. Open screen
2. Verify RCS model is visible in center panel
3. Check that model is rendered correctly
4. Test camera auto-rotate if enabled

### Test 3: Gauge Updates

1. Run simulation (or mock data)
2. Verify temperature gauges update
3. Verify flow gauges update
4. Check gauge colors in warning/danger zones

### Test 4: RCP Controls

1. Check RCP status indicators match simulation
2. Test START button (should be blocked if interlocks not met)
3. Test STOP button
4. Verify ramping animation

### Test 5: Alarms

1. Trigger an alarm condition
2. Verify alarm appears in list
3. Check alarm formatting

### Validation Checklist

| Item | Expected | Pass? |
|------|----------|-------|
| Screen toggles with Key 2 | Yes | ☐ |
| 3D model visible | Yes | ☐ |
| Temperature gauges work | Yes | ☐ |
| Flow gauges work | Yes | ☐ |
| RCP indicators update | Yes | ☐ |
| RCP buttons functional | Yes | ☐ |
| Alarm list works | Yes | ☐ |
| No console errors | Yes | ☐ |
| FPS > 60 | Yes | ☐ |

---

## 11. Troubleshooting

### Issue: 3D model not visible

**Solutions:**
1. Check camera's culling mask includes `RCSVisualization` layer
2. Verify model is on correct layer
3. Check render texture is assigned to RawImage
4. Ensure camera position can see the model

### Issue: Gauges not updating

**Solutions:**
1. Verify `HeatupSimEngine` reference is found
2. Check gauge `SetValue()` is being called
3. Ensure gauge min/max ranges are correct

### Issue: RCP buttons not responding

**Solutions:**
1. Check button onClick is wired
2. Verify `RCPControlPanel.Initialize()` was called
3. Check button interactable state

### Issue: Materials look wrong

**Solutions:**
1. Upgrade materials to URP: Edit > Render Pipeline > Upgrade
2. Re-extract materials from FBX
3. Manually recreate materials

### Issue: Screen doesn't toggle

**Solutions:**
1. Check `ToggleKey` returns `KeyCode.Alpha2`
2. Verify `OperatorScreen` base class Update is called
3. Check ScreenManager is active

### Issue: Poor performance

**Solutions:**
1. Reduce render texture resolution
2. Reduce 3D model polygon count
3. Disable anti-aliasing on render texture
4. Reduce gauge update frequency

---

## Quick Reference

### Key Bindings
- **2**: Toggle RCS Primary Loop Screen

### Important Transforms
- 3D Model Position: (1000, 0, 0)
- Camera Position: (1000, 60, -120)

### Layer Setup
- Layer 8: RCSVisualization

### File Locations
```
Assets/
├── Models/RCS/RCS_Primary_Loop.fbx
├── Materials/RCS/*.mat
├── RenderTextures/RCS_Visualization_RT.renderTexture
├── Prefabs/Screens/RCSPrimaryLoop/
│   ├── RCSPrimaryLoopCanvas.prefab
│   └── RCPControlPanel.prefab
└── Scripts/UI/Screens/RCSPrimaryLoopScreen.cs
```

---

**End of Assembly Instructions**

For additional help, refer to:
- `RCS_Technical_Specifications.md` - Technical details
- `Blender_Unity_Export_Manual.md` - Model pipeline
- Unity Documentation: https://docs.unity3d.com
