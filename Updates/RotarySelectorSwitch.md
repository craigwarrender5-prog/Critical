# Implementation Plan v5.1.0 — Rotary Selector Switch

**CRITICAL: Master the Atom**  
Westinghouse 4-Loop PWR Simulator  
Validation Scenario Start Control

**Document Version:** 1.0  
**Target Version:** 5.1.0  
**Date:** 2026-02-18  
**Classification:** UI / 3D Asset / Blender-to-Unity Pipeline

---

## Table of Contents

1. [Problem Summary](#1-problem-summary)
2. [Expectations](#2-expectations)
3. [Technical Design](#3-technical-design)
4. [Implementation Stages](#4-implementation-stages)
5. [Blender Manual](#5-blender-manual)
6. [Unity Integration Manual](#6-unity-integration-manual)
7. [Unaddressed Issues](#7-unaddressed-issues)
8. [Files Created/Modified](#8-files-createdmodified)
9. [Validation Criteria](#9-validation-criteria)

---

## 1. Problem Summary

The validation scenario currently lacks a proper start control mechanism. Users need an intuitive, realistic way to initiate the heatup validation scenario from the Pressurizer Operator Screen (Key 3).

**Current State:**
- No dedicated UI control to start/stop the validation scenario
- Validation runs automatically or requires code changes
- Missing authentic control room aesthetic for scenario management

**Desired State:**
- 3-position industrial rotary selector switch (OFF → AUTO → MANUAL)
- Physically modelled in Blender 5.0 with realistic materials
- Animated rotation on click in Unity
- Placed on the Pressurizer Screen where heatup procedures initiate
- Scalable prefab for easy placement and sizing
- MANUAL position disabled/greyed for future expansion

---

## 2. Expectations

### 2.1 Visual Appearance
The switch should replicate an authentic industrial 3-position selector switch:
- **Base plate:** Dark grey/black metal with engraved position labels
- **Knob:** Black phenolic or bakelite-style with chrome/brass indicator line
- **Positions:** OFF (7 o'clock), AUTO (12 o'clock), MANUAL (5 o'clock)
- **Labels:** Engraved white text on base plate: "OFF", "AUTO", "MANUAL"
- **Indicator:** Chrome or brass pointer/line on knob showing current position
- **Detent feel:** Visual "click" implied by snap-to-position animation

### 2.2 Behaviour
| Position | Rotation | Function |
|----------|----------|----------|
| OFF | -45° from vertical | Validation scenario stopped/paused |
| AUTO | 0° (vertical/12 o'clock) | Validation scenario running automatically |
| MANUAL | +45° from vertical | Reserved for future (disabled, greyed label) |

### 2.3 Interaction
- **Left Click:** Rotate clockwise (OFF → AUTO → MANUAL)
- **Right Click:** Rotate counter-clockwise (MANUAL → AUTO → OFF)
- **Disabled position handling:** When MANUAL is disabled, left-click from AUTO does nothing (or shows tooltip); right-click from OFF does nothing
- **Audio feedback:** Optional click sound on position change (future)
- **Visual feedback:** Smooth animated rotation (~0.15 seconds)

**Interaction Diagram:**
```
            AUTO (12 o'clock)
              ▲
    L-Click ┌─┴─┐ R-Click
            │   │
            ▼   ▼
OFF ◄───────────────────► MANUAL
(7 o'clock)    (5 o'clock)
     R-Click ◄─── L-Click
```

### 2.4 Scalability
- Prefab with configurable scale parameter
- Works at any reasonable UI size
- Maintains visual quality at different scales via proper UV mapping

---

## 3. Technical Design

### 3.1 Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        BLENDER 5.0                              │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ RotarySwitch.blend                                       │   │
│  │  ├─ SwitchRoot (Empty - parent)                         │   │
│  │  │   ├─ BasePlate (Cylinder + engraved labels)          │   │
│  │  │   ├─ Knob (Cylinder with indicator line)             │   │
│  │  │   │   └─ Indicator (Extruded pointer)                │   │
│  │  │   └─ MountingRing (Torus - bezel)                    │   │
│  │  └─ Materials: MAT_BaseMetal, MAT_Knob, MAT_Chrome,     │   │
│  │                 MAT_LabelWhite, MAT_LabelGrey            │   │
│  └─────────────────────────────────────────────────────────┘   │
│                            │                                    │
│                            ▼ Export FBX                         │
└─────────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                         UNITY 6.3                               │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Assets/Models/RotarySwitch.fbx                          │   │
│  │  └─ Import Settings: Scale 1, URP Materials             │   │
│  └─────────────────────────────────────────────────────────┘   │
│                            │                                    │
│                            ▼                                    │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Assets/Prefabs/UI/RotarySelectorSwitch.prefab           │   │
│  │  ├─ RotarySelectorSwitch (Script)                       │   │
│  │  │   ├─ State machine (OFF/AUTO/MANUAL)                 │   │
│  │  │   ├─ Click detection (IPointerClickHandler)          │   │
│  │  │   ├─ Animation (DOTween or coroutine)                │   │
│  │  │   └─ Events: OnStateChanged(SwitchState)             │   │
│  │  └─ Render Texture setup (optional)                     │   │
│  └─────────────────────────────────────────────────────────┘   │
│                            │                                    │
│                            ▼                                    │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ PressurizerScreen.cs Integration                        │   │
│  │  └─ Subscribe to OnStateChanged → Start/Stop Validation │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Blender Model Specifications

| Component | Geometry | Dimensions (Blender Units) | Material |
|-----------|----------|---------------------------|----------|
| BasePlate | Cylinder, 64 verts | Radius: 0.5, Depth: 0.08 | MAT_BaseMetal |
| Knob | Cylinder, 32 verts | Radius: 0.25, Depth: 0.15 | MAT_Knob |
| Indicator | Extruded rectangle | 0.02 × 0.18 × 0.02 | MAT_Chrome |
| MountingRing | Torus | Major: 0.52, Minor: 0.04 | MAT_Chrome |
| Label_OFF | Text → Mesh | Size: 0.06 | MAT_LabelWhite |
| Label_AUTO | Text → Mesh | Size: 0.06 | MAT_LabelWhite |
| Label_MANUAL | Text → Mesh | Size: 0.06 | MAT_LabelGrey |

### 3.3 Unity Components

**RotarySelectorSwitch.cs:**
```csharp
public enum SwitchPosition { Off, Auto, Manual }

public class RotarySelectorSwitch : MonoBehaviour, IPointerClickHandler
{
    [Header("Configuration")]
    public SwitchPosition CurrentPosition = SwitchPosition.Off;
    public bool ManualEnabled = false;  // Greyed out when false
    
    [Header("Angles (degrees, Z-axis rotation)")]
    public float AngleOff = -45f;
    public float AngleAuto = 0f;
    public float AngleManual = 45f;
    
    [Header("Animation")]
    public float RotationDuration = 0.15f;
    
    [Header("References")]
    public Transform KnobTransform;
    
    // Events
    public UnityEvent<SwitchPosition> OnPositionChanged;
    
    // IPointerClickHandler
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            RotateClockwise();
        else if (eventData.button == PointerEventData.InputButton.Right)
            RotateCounterClockwise();
    }
    
    private void RotateClockwise()
    {
        // OFF → AUTO → MANUAL (if enabled)
        switch (CurrentPosition)
        {
            case SwitchPosition.Off:
                SetPosition(SwitchPosition.Auto);
                break;
            case SwitchPosition.Auto:
                if (ManualEnabled)
                    SetPosition(SwitchPosition.Manual);
                // else: do nothing or show tooltip
                break;
            case SwitchPosition.Manual:
                // Already at max clockwise
                break;
        }
    }
    
    private void RotateCounterClockwise()
    {
        // MANUAL → AUTO → OFF
        switch (CurrentPosition)
        {
            case SwitchPosition.Manual:
                SetPosition(SwitchPosition.Auto);
                break;
            case SwitchPosition.Auto:
                SetPosition(SwitchPosition.Off);
                break;
            case SwitchPosition.Off:
                // Already at max counter-clockwise
                break;
        }
    }
}
```

### 3.4 Render Texture Approach

For UI integration on the Pressurizer Screen (Screen Space Overlay canvas), the 3D switch requires:

1. **Dedicated Camera:** Orthographic, viewing only the switch
2. **Render Texture:** 256×256 or 512×512 resolution
3. **RawImage UI Element:** Displays the Render Texture on the canvas
4. **Layer Isolation:** Switch on "SwitchRender" layer, camera culls all else

This is the same approach used for the T_HOT gauge.

---

## 4. Implementation Stages

### Stage 1: Blender Model Creation
**Deliverables:**
- `create_rotary_switch.py` — Blender Python script to generate the complete model
- Executed in Blender 5.0 to create `RotarySwitch.blend`

**Tasks:**
1. Create base plate cylinder with bevelled edge
2. Create knob cylinder with top chamfer
3. Create indicator pointer (chrome line on knob)
4. Create mounting ring (chrome bezel)
5. Create position labels as 3D text, convert to mesh
6. Apply materials (dark metal, black knob, chrome, white/grey text)
7. Parent all to SwitchRoot empty
8. Apply all transforms
9. Save as `RotarySwitch.blend`

---

### Stage 2: Blender Export & Unity Import
**Deliverables:**
- `RotarySwitch.fbx` exported to `Assets/Models/`
- Import settings configured
- Materials extracted and configured for URP

**Tasks:**
1. Export FBX with correct settings (Forward: -Z, Up: Y, Apply Transform)
2. Import into Unity
3. Configure import settings (Scale: 1 or 100 as needed)
4. Extract materials to `Assets/Models/Materials/`
5. Configure URP/Lit materials for each component

---

### Stage 3: Unity Prefab & Script Creation
**Deliverables:**
- `RotarySelectorSwitch.cs` — Main control script
- `RotarySelectorSwitch.prefab` — Configured prefab
- Render Texture setup for UI integration

**Tasks:**
1. Create `RotarySelectorSwitch.cs` with state machine and click handling
2. Create Render Texture asset
3. Create dedicated camera for switch rendering
4. Configure layer isolation
5. Create prefab with all components wired
6. Test click-to-rotate functionality

---

### Stage 4: Pressurizer Screen Integration
**Deliverables:**
- Modified `PressurizerScreen.cs` with switch reference
- Switch positioned on Pressurizer operator screen
- Validation scenario start/stop wired to switch state

**Tasks:**
1. Add RawImage element to PressurizerScreen prefab
2. Position switch in appropriate location (bottom panel area)
3. Wire OnPositionChanged event to validation control
4. Test full integration

---

### Stage 5: Documentation & Polish
**Deliverables:**
- Step-by-step Blender manual (in this document)
- Changelog entry
- Optional: click sound effect integration

**Tasks:**
1. Finalise all documentation
2. Create Changelog v5.1.0
3. Test complete workflow from model to runtime

---

## 5. Blender Manual

### 5.1 Prerequisites
- Blender 5.0.1 installed
- This implementation plan document open for reference

### 5.2 Method: Python Script Execution

Rather than manually modelling (which is error-prone for first-timers), we provide a Python script that generates the entire switch model automatically.

**Step 1:** Open Blender 5.0

**Step 2:** Delete the default cube, camera, and light (press `A` to select all, then `X` to delete)

**Step 3:** Open the Scripting workspace (tab at top of Blender window)

**Step 4:** Click "New" to create a new text block

**Step 5:** Copy and paste the entire contents of `create_rotary_switch.py` (provided in Stage 1)

**Step 6:** Click "Run Script" (play button) or press `Alt+P`

**Step 7:** Switch back to the Layout workspace. You should see the complete switch model.

**Step 8:** Press `Numpad 1` for front view, then `Numpad 5` for orthographic to inspect

**Step 9:** Save as `RotarySwitch.blend` in your project folder

### 5.3 Export to FBX

**Step 1:** Press `A` to select all objects

**Step 2:** Go to `File > Export > FBX (.fbx)`

**Step 3:** Navigate to `C:\Users\craig\Projects\Critical\Assets\Models\`

**Step 4:** Name the file `RotarySwitch.fbx`

**Step 5:** In the export settings panel (right side), configure:

| Setting | Value |
|---------|-------|
| Selected Objects | ✓ ON |
| Scale | 1.0 |
| Apply Scalings | FBX All |
| Forward | -Z Forward |
| Up | Y Up |
| Apply Transform | ✓ ON |
| Mesh > Smoothing | Face |
| Mesh > Apply Modifiers | ✓ ON |
| Armature | ✗ OFF |
| Animation | ✗ OFF |

**Step 6:** Click "Export FBX"

---

## 6. Unity Integration Manual

### 6.1 Import the FBX

**Step 1:** Open Unity and your Critical project

**Step 2:** Unity auto-detects the new FBX in `Assets/Models/`. Wait for import.

**Step 3:** Select `RotarySwitch` in the Project window

**Step 4:** In the Inspector, configure Model Import Settings:

| Setting | Value |
|---------|-------|
| Scale Factor | 1 (adjust to 100 if switch is tiny) |
| Convert Units | ✓ |
| Import Cameras | ✗ |
| Import Lights | ✗ |

**Step 5:** Click Apply

### 6.2 Extract and Configure Materials

**Step 1:** Click the Materials tab in Import Settings

**Step 2:** Set Material Creation Mode to "Import via MaterialDescription"

**Step 3:** Click "Extract Materials" → save to `Assets/Models/Materials/`

**Step 4:** Configure each material in the Inspector:

| Material | Shader | Metallic | Smoothness | Color | Notes |
|----------|--------|----------|------------|-------|-------|
| MAT_BaseMetal | URP/Lit | 0.8 | 0.4 | #2A2A2A | Dark industrial metal |
| MAT_Knob | URP/Lit | 0.1 | 0.3 | #1A1A1A | Black phenolic |
| MAT_Chrome | URP/Lit | 1.0 | 0.85 | #C0C0C0 | Bright chrome |
| MAT_LabelWhite | URP/Lit | 0.0 | 0.5 | #FFFFFF | Emission: 0.3 |
| MAT_LabelGrey | URP/Lit | 0.0 | 0.5 | #606060 | Greyed out (disabled) |

### 6.3 Create the Prefab

**Step 1:** Drag `RotarySwitch` from Project into the Hierarchy

**Step 2:** Rename root object to `RotarySelectorSwitch`

**Step 3:** Find the `Knob` child object (this is what rotates)

**Step 4:** Add the `RotarySelectorSwitch.cs` script to the root object

**Step 5:** In the Inspector, assign the `Knob` transform to the `KnobTransform` field

**Step 6:** Create a new Layer called "SwitchRender"

**Step 7:** Set all child objects of the switch to the "SwitchRender" layer

**Step 8:** Drag from Hierarchy into `Assets/Prefabs/UI/` to create the prefab

### 6.4 Create Render Texture Setup

**Step 1:** Right-click in Project > Create > Render Texture

**Step 2:** Name it `RT_RotarySwitch`

**Step 3:** Set size to 256×256 (or 512×512 for higher quality)

**Step 4:** In Hierarchy, create a new Camera: Right-click > Camera

**Step 5:** Name it `SwitchCamera`

**Step 6:** Configure the camera:

| Setting | Value |
|---------|-------|
| Projection | Orthographic |
| Orthographic Size | 0.6 (adjust to frame the switch) |
| Culling Mask | Only "SwitchRender" |
| Target Texture | RT_RotarySwitch |
| Background Type | Solid Color |
| Background | #00000000 (transparent) |

**Step 7:** Position the camera in front of the switch, looking at it

**Step 8:** Move the camera to the "SwitchRender" layer (or leave on Default with culling set)

### 6.5 Add to Pressurizer Screen

**Step 1:** Open the `PressurizerScreen` prefab

**Step 2:** Add a UI > Raw Image element to the bottom panel area

**Step 3:** Set the Raw Image texture to `RT_RotarySwitch`

**Step 4:** Size and position as desired (scalable—adjust RectTransform)

**Step 5:** The RawImage needs to forward pointer clicks to the switch. Add `RawImageClickForwarder.cs` component to the RawImage:

```csharp
// Forwards UI clicks on RawImage to the 3D switch object
public class RawImageClickForwarder : MonoBehaviour, IPointerClickHandler
{
    public RotarySelectorSwitch TargetSwitch;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (TargetSwitch != null)
            TargetSwitch.OnPointerClick(eventData);
    }
}
```

**Step 6:** Assign the `RotarySelectorSwitch` instance to the forwarder's `TargetSwitch` field

---

## 7. Unaddressed Issues

| Issue | Reason | Future Version |
|-------|--------|----------------|
| Click sound effect | Out of scope for v5.1.0 | v5.2.0 |
| MANUAL position functionality | Requires step-by-step scenario control system | v6.0.0+ |
| Drag-to-rotate interaction | Complexity; click-to-advance is sufficient for now | v5.3.0 |
| Multiple switch instances | Single switch on PZR screen sufficient | As needed |
| Detent "snap" animation | Polish feature | v5.2.0 |

---

## 8. Files Created/Modified

### New Files

| File | Location | Purpose |
|------|----------|---------|
| `create_rotary_switch.py` | `Assets/Scripts/Blender/` | Blender Python script |
| `RotarySwitch.blend` | `Assets/Models/Source/` | Blender source file |
| `RotarySwitch.fbx` | `Assets/Models/` | Exported 3D model |
| `RT_RotarySwitch.asset` | `Assets/RenderTextures/` | Render texture |
| `RotarySelectorSwitch.cs` | `Assets/Scripts/UI/` | Main control script |
| `RotarySelectorSwitch.prefab` | `Assets/Prefabs/UI/` | Configured prefab |

### Modified Files

| File | Changes |
|------|---------|
| `PressurizerScreen.cs` | Add switch reference, wire to validation control |
| `PressurizerScreen.prefab` | Add RawImage element for switch display |

---

## 9. Validation Criteria

### 9.1 Visual Validation
- [ ] Switch renders correctly in Unity with all materials visible
- [ ] Base plate shows engraved position labels (OFF, AUTO, MANUAL)
- [ ] MANUAL label is visibly greyed/disabled
- [ ] Chrome indicator line on knob points to current position
- [ ] Switch appears at correct scale on Pressurizer Screen

### 9.2 Functional Validation
- [ ] Left-click from OFF moves to AUTO
- [ ] Left-click from AUTO does nothing (MANUAL disabled)
- [ ] Right-click from AUTO moves to OFF
- [ ] Right-click from OFF does nothing (already at limit)
- [ ] Knob rotates smoothly with animation (no snap/pop)
- [ ] `OnPositionChanged` event fires on each position change
- [ ] Position persists correctly (state machine works)
- [ ] When ManualEnabled=true, left-click from AUTO moves to MANUAL
- [ ] When ManualEnabled=true, right-click from MANUAL moves to AUTO

### 9.3 Integration Validation
- [ ] Switch visible on Pressurizer Screen (Key 3)
- [ ] Switch state controls validation scenario start/stop
- [ ] OFF position: Validation not running
- [ ] AUTO position: Validation running
- [ ] No errors or warnings in Console during operation

---

## Appendix A: Blender Python Script

The complete `create_rotary_switch.py` script will be provided in Stage 1 of implementation.

---

## Appendix B: RotarySelectorSwitch.cs Reference

The complete Unity C# script will be provided in Stage 3 of implementation.

---

**End of Implementation Plan v5.1.0**
