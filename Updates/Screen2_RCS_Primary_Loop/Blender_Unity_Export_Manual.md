# Blender to Unity Export/Import Manual

## RCS Primary Loop 3D Model Pipeline

**Version:** 1.0.0  
**Date:** 2026-02-09  
**For:** Critical Nuclear Reactor Simulator

---

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Blender Model Generation](#2-blender-model-generation)
3. [Blender Export Settings](#3-blender-export-settings)
4. [Unity Import Configuration](#4-unity-import-configuration)
5. [Material Setup in Unity](#5-material-setup-in-unity)
6. [Animation Setup](#6-animation-setup)
7. [Prefab Creation](#7-prefab-creation)
8. [Troubleshooting](#8-troubleshooting)

---

## 1. Prerequisites

### Software Requirements

| Software | Version | Purpose |
|----------|---------|---------|
| Blender | 5.0+ | 3D model creation |
| Unity | 2022.3 LTS | Game engine |
| Universal Render Pipeline (URP) | 14.0+ | Rendering |

### Project Setup

Ensure your Unity project has:
- Universal Render Pipeline (URP) configured
- TextMeshPro installed
- Project structure:
  ```
  Assets/
  ├── Models/
  │   └── RCS/
  ├── Materials/
  │   └── RCS/
  ├── Prefabs/
  │   └── Screens/
  └── Scripts/
      └── UI/
  ```

---

## 2. Blender Model Generation

### Step 1: Open Blender 5.0

1. Launch Blender 5.0
2. Go to **Scripting** workspace (top tabs)
3. Click **+ New** to create a new text file

### Step 2: Load the Script

1. Open `RCS_Primary_Loop_Blender.py` from:
   ```
   Critical\Updates\Screen2_RCS_Primary_Loop\RCS_Primary_Loop_Blender.py
   ```
2. Copy and paste the entire script into the Blender text editor

### Step 3: Run the Script

1. Press **Alt+P** or click **Run Script** button
2. Wait for the console to show "Model created successfully!"
3. The model will appear in the 3D viewport

### Step 4: Verify the Model

Check that the following objects exist in the Outliner:
```
RCS_Primary_Loop (Empty - Root)
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
│   ├── HotLeg_1 through HotLeg_4
│   ├── CrossoverLeg_1 through CrossoverLeg_4
│   ├── ColdLeg_1 through ColdLeg_4
│   └── SurgeLine
├── FlowArrows
│   └── FlowArrow_* (8 arrows)
└── Labels
    └── Label_Loop_* (4 labels)
```

---

## 3. Blender Export Settings

### Step 1: Select Objects to Export

1. In the Outliner, click on **RCS_Primary_Loop** (the root empty)
2. Make sure it's highlighted (selected)

### Step 2: Open Export Dialog

1. Go to **File > Export > FBX (.fbx)**
2. Navigate to: `Critical\Assets\Models\RCS\`
3. Name the file: `RCS_Primary_Loop.fbx`

### Step 3: Configure Export Settings

Use these exact settings:

#### Include Section
| Setting | Value |
|---------|-------|
| Limit to: Selected Objects | ✅ Enabled |
| Visible Objects | ✅ Enabled |
| Active Collection | ❌ Disabled |

#### Transform Section
| Setting | Value |
|---------|-------|
| Scale | 1.00 |
| Apply Scalings | FBX All |
| Forward | -Z Forward |
| Up | Y Up |
| Apply Transform | ❌ Disabled |

#### Geometry Section
| Setting | Value |
|---------|-------|
| Smoothing | Face |
| Export Subdivision Surface | ❌ Disabled |
| Apply Modifiers | ✅ Enabled |
| Loose Edges | ❌ Disabled |
| Tangent Space | ✅ Enabled |

#### Armature Section
(Not used for this model - leave defaults)

#### Animation Section
| Setting | Value |
|---------|-------|
| Bake Animation | ✅ Enabled (if animations exist) |
| NLA Strips | ✅ Enabled |
| All Actions | ✅ Enabled |

### Step 4: Export

Click **Export FBX**

---

## 4. Unity Import Configuration

### Step 1: Locate the Imported Asset

1. Open Unity
2. Navigate to `Assets/Models/RCS/` in the Project window
3. Find `RCS_Primary_Loop.fbx`

### Step 2: Configure Import Settings

Select the FBX file and configure in the Inspector:

#### Model Tab
| Setting | Value |
|---------|-------|
| Scale Factor | 1 |
| Convert Units | ✅ Enabled |
| Import BlendShapes | ❌ Disabled |
| Import Visibility | ✅ Enabled |
| Import Cameras | ❌ Disabled |
| Import Lights | ❌ Disabled |
| Preserve Hierarchy | ✅ Enabled |
| Sort Hierarchy By Name | ✅ Enabled |

#### Meshes Section
| Setting | Value |
|---------|-------|
| Mesh Compression | Off |
| Read/Write | ✅ Enabled |
| Optimize Mesh | ✅ Enabled |
| Generate Colliders | ❌ Disabled |

#### Geometry Section
| Setting | Value |
|---------|-------|
| Keep Quads | ❌ Disabled |
| Weld Vertices | ✅ Enabled |
| Index Format | Auto |
| Legacy Blend Shape Normals | ❌ Disabled |
| Normals | Import |
| Blend Shape Normals | None |
| Normals Mode | Area And Angle Weighted |
| Tangents | Calculate Mikktspace |
| Swap UVs | ❌ Disabled |
| Generate Lightmap UVs | ✅ Enabled |

#### Rig Tab
| Setting | Value |
|---------|-------|
| Animation Type | None |

#### Animation Tab
| Setting | Value |
|---------|-------|
| Import Animation | ✅ Enabled (if needed) |
| Import Constraints | ❌ Disabled |

#### Materials Tab
| Setting | Value |
|---------|-------|
| Material Creation Mode | Import via MaterialDescription (Legacy) |
| Location | Use External Materials (Legacy) |
| Naming | From Model's Material |
| Search | Local Materials Folder |

### Step 3: Apply Import Settings

Click **Apply** at the bottom of the Inspector

---

## 5. Material Setup in Unity

### Step 1: Create URP Materials Folder

Create: `Assets/Materials/RCS/`

### Step 2: Extract Materials

1. Select the FBX in Project window
2. Go to **Materials** tab in Inspector
3. Click **Extract Materials...**
4. Select the `Assets/Materials/RCS/` folder
5. Click **Select Folder**

### Step 3: Configure Each Material

For each extracted material, configure as follows:

#### MAT_ReactorVessel
| Property | Value |
|----------|-------|
| Shader | Universal Render Pipeline/Lit |
| Base Map Color | #708090 (Slate Gray) |
| Metallic | 0.8 |
| Smoothness | 0.7 |

#### MAT_HotLeg
| Property | Value |
|----------|-------|
| Shader | Universal Render Pipeline/Lit |
| Base Map Color | #FF4500 (Orange Red) |
| Metallic | 0.6 |
| Smoothness | 0.65 |
| Emission | ✅ Enabled |
| Emission Color | #FF4500 |
| Emission Intensity | 0.3 |

#### MAT_ColdLeg
| Property | Value |
|----------|-------|
| Shader | Universal Render Pipeline/Lit |
| Base Map Color | #1E90FF (Dodger Blue) |
| Metallic | 0.6 |
| Smoothness | 0.65 |
| Emission | ✅ Enabled |
| Emission Color | #1E90FF |
| Emission Intensity | 0.2 |

#### MAT_CrossoverLeg
| Property | Value |
|----------|-------|
| Shader | Universal Render Pipeline/Lit |
| Base Map Color | #00CCCC (Cyan) |
| Metallic | 0.6 |
| Smoothness | 0.65 |

#### MAT_SteamGenerator
| Property | Value |
|----------|-------|
| Shader | Universal Render Pipeline/Lit |
| Base Map Color | #C0C0C0 (Silver) |
| Metallic | 0.7 |
| Smoothness | 0.65 |

#### MAT_RCP
| Property | Value |
|----------|-------|
| Shader | Universal Render Pipeline/Lit |
| Base Map Color | #4D4D59 (Dark Gray) |
| Metallic | 0.6 |
| Smoothness | 0.6 |

#### MAT_Pressurizer
| Property | Value |
|----------|-------|
| Shader | Universal Render Pipeline/Lit |
| Base Map Color | #D9B321 (Gold) |
| Metallic | 0.7 |
| Smoothness | 0.7 |

#### MAT_FlowArrow
| Property | Value |
|----------|-------|
| Shader | Universal Render Pipeline/Lit |
| Surface Type | Transparent |
| Base Map Color | #00FF4C (Bright Green) |
| Metallic | 0.2 |
| Smoothness | 0.2 |
| Emission | ✅ Enabled |
| Emission Color | #00FF4C |
| Emission Intensity | 2.0 |

---

## 6. Animation Setup

### Creating Flow Arrow Animation in Unity

Since the flow arrows should animate (pulse/move), create an Animation Controller:

### Step 1: Create Animator Controller

1. Right-click in `Assets/Animations/RCS/`
2. **Create > Animator Controller**
3. Name it `FlowArrowAnimator`

### Step 2: Create Animation Clips

Create these animation clips:

#### FlowArrow_Pulse
```
Frame 0:   Scale = (1, 1, 1), Emission = 2.0
Frame 30:  Scale = (1.2, 1.2, 1.2), Emission = 4.0
Frame 60:  Scale = (1, 1, 1), Emission = 2.0
Loop: Yes
```

### Step 3: Assign to Flow Arrows

1. Select each FlowArrow object in the prefab
2. Add **Animator** component
3. Assign `FlowArrowAnimator` controller

### RCP Rotation Animation

For spinning RCP indicator:

1. Create animation clip `RCP_Spin`
2. Rotate the flywheel 360° over 1 second
3. Set to loop
4. Control via script (enable/disable based on RCP running state)

---

## 7. Prefab Creation

### Step 1: Create Instance in Scene

1. Drag `RCS_Primary_Loop.fbx` into the Hierarchy
2. Position at (0, 0, 0)
3. Set rotation to (0, 0, 0)
4. Set scale to (1, 1, 1)

### Step 2: Add Components

Add these components to the root object:

```csharp
// RCSPrimaryLoopVisual.cs - attach to root
[RequireComponent(typeof(Canvas))]
public class RCSPrimaryLoopVisual : MonoBehaviour
{
    [Header("Component References")]
    public Transform reactorVessel;
    public Transform[] steamGenerators = new Transform[4];
    public Transform[] rcps = new Transform[4];
    public Transform pressurizer;
    
    [Header("Piping References")]
    public Transform[] hotLegs = new Transform[4];
    public Transform[] coldLegs = new Transform[4];
    public Transform[] crossoverLegs = new Transform[4];
    public Transform surgeLine;
    
    [Header("Flow Arrows")]
    public Transform[] flowArrows;
    
    [Header("Animation")]
    public Animator[] rcpAnimators;
    public Animator[] arrowAnimators;
}
```

### Step 3: Create Prefab

1. Drag the configured object from Hierarchy to `Assets/Prefabs/Screens/`
2. Name it `RCS_Primary_Loop_Visual`
3. Click **Original Prefab** when prompted

---

## 8. Troubleshooting

### Common Issues and Solutions

#### Issue: Model appears too small/large in Unity
**Solution:** 
- Check Blender export scale (should be 1.0)
- Check Unity import Scale Factor (should be 1)
- Ensure "Convert Units" is enabled in Unity import

#### Issue: Materials appear pink/missing
**Solution:**
- Extract materials (Materials tab > Extract Materials)
- Ensure URP package is installed
- Upgrade materials: Edit > Render Pipeline > Universal Render Pipeline > Upgrade Project Materials

#### Issue: Hierarchy is flat (not nested)
**Solution:**
- Enable "Preserve Hierarchy" in Unity import settings
- Ensure "Selected Objects" was enabled in Blender export
- Check that root empty was selected before export

#### Issue: Normals appear inverted (inside-out)
**Solution:**
- In Blender: Select object > Edit Mode > Mesh > Normals > Recalculate Outside
- Re-export the FBX

#### Issue: Model has wrong orientation
**Solution:**
- In Blender export: Forward = -Z Forward, Up = Y Up
- In Unity: Rotate root object 90° on X axis if needed

#### Issue: Animations don't play
**Solution:**
- Ensure "Bake Animation" was enabled in Blender export
- Check Animator Controller is assigned
- Verify animation clips are in the controller

#### Issue: Flow arrows not glowing
**Solution:**
- Enable Emission on material
- Set Emission Intensity > 0
- Ensure URP has Post Processing with Bloom enabled

---

## Quick Reference Card

### Blender Export Checklist
- [ ] Select root object (RCS_Primary_Loop)
- [ ] File > Export > FBX
- [ ] Selected Objects = ✅
- [ ] Scale = 1.00
- [ ] Apply Scalings = FBX All
- [ ] Forward = -Z Forward
- [ ] Up = Y Up

### Unity Import Checklist
- [ ] Scale Factor = 1
- [ ] Convert Units = ✅
- [ ] Preserve Hierarchy = ✅
- [ ] Read/Write = ✅
- [ ] Generate Lightmap UVs = ✅
- [ ] Extract Materials
- [ ] Configure URP materials
- [ ] Create prefab

---

**End of Manual**

For questions or issues, consult the Unity and Blender documentation or create an issue in the project tracker.
