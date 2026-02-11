# Blender 5.0 to Unity 6.3 Export/Import Manual

**Version:** 1.0.0  
**Date:** 2026-02-09  
**Project:** CRITICAL - Nuclear Reactor Simulator  
**Purpose:** Comprehensive guide for exporting 3D models from Blender and importing into Unity

---

## Table of Contents

1. [Overview](#1-overview)
2. [Part A: Blender Model Preparation](#2-part-a-blender-model-preparation)
3. [Part B: FBX Export Settings](#3-part-b-fbx-export-settings)
4. [Part C: Unity Import Configuration](#4-part-c-unity-import-configuration)
5. [Part D: Material Setup in Unity](#5-part-d-material-setup-in-unity)
6. [Part E: Prefab Creation](#6-part-e-prefab-creation)
7. [Part F: Troubleshooting](#7-part-f-troubleshooting)
8. [Appendix: Quick Reference Checklists](#8-appendix-quick-reference-checklists)

---

## 1. Overview

This manual covers the complete workflow for transferring 3D assets from Blender 5.0 to Unity 6.3 using the Universal Render Pipeline (URP). The process involves:

1. Preparing the model in Blender (naming, hierarchy, materials)
2. Exporting as FBX with correct settings
3. Importing into Unity with proper configuration
4. Setting up URP-compatible materials
5. Creating prefabs for use in the simulator

### Key Principles

- **Naming Convention**: Use consistent, descriptive names (e.g., `RCS_HotLeg_Loop1`)
- **Hierarchy**: Organize objects logically; parent animated parts separately
- **Scale**: 1 Blender Unit = 1 Unity Unit = 1 Meter
- **Axis Orientation**: Blender Z-up → Unity Y-up (handled by FBX export)
- **Materials**: PBR workflow in both applications for seamless transfer

---

## 2. Part A: Blender Model Preparation

### 2.1 Naming Conventions

Use clear, consistent naming for all objects:

```
[System]_[Component]_[Variant]_[Index]

Examples:
  RCS_HotLeg_Loop1
  RCS_ColdLeg_Loop2
  RCP_Rotor_1
  Gauge_Temperature_THot
  FlowArrow_HotLeg_1
```

**Rules:**
- No spaces (use underscores)
- No special characters except underscore
- Keep names under 64 characters
- Prefix materials with `MAT_`

### 2.2 Hierarchy Organization

Structure your model hierarchy for Unity:

```
Model_Root (Empty)
├── Static_Geometry
│   ├── Housing
│   ├── Frame
│   └── Labels
├── Animated_Parts (separate for animation)
│   ├── Needle
│   ├── Rotor_1
│   └── Fill
├── Indicators
│   ├── Light_Status
│   └── Light_Warning
└── Colliders (optional, for interaction)
    └── Collider_Button
```

**Best Practices:**
- Group static geometry under one parent
- Keep animated objects as separate children of root
- Use empties for organizational grouping
- Animated parts should have their pivot points set correctly

### 2.3 Pivot Points (Origins)

Set origins correctly for animation:

| Object Type | Pivot Location | Reason |
|-------------|----------------|--------|
| Needle/Pointer | Base/center of rotation | Rotates around pivot |
| Fill/Level | Bottom center | Scales upward from bottom |
| Rotor | Center axis | Rotates around center |
| Button | Surface center | Press animation |
| Door/Hatch | Hinge edge | Opens like a door |

**To Set Origin in Blender:**
1. Enter Edit Mode (Tab)
2. Select vertices at desired pivot location
3. Shift+S → Cursor to Selected
4. Exit Edit Mode (Tab)
5. Object → Set Origin → Origin to 3D Cursor

### 2.4 Apply Transforms

Before export, apply all transforms:

1. Select all objects (A)
2. Ctrl+A → Apply All Transforms

This ensures:
- Location is (0, 0, 0) relative to parent
- Rotation is (0, 0, 0)
- Scale is (1, 1, 1)

**Exception:** Don't apply transforms to objects that need non-default values for animation starting positions.

### 2.5 Material Setup in Blender

Create materials using Principled BSDF for PBR compatibility:

```
Material Settings for Unity URP:
├── Base Color      → _BaseColor in URP
├── Metallic        → _Metallic
├── Roughness       → 1 - Smoothness (inverted in Unity)
├── Normal Map      → _BumpMap
├── Emission Color  → _EmissionColor
└── Alpha           → _BaseColor.a (with transparency)
```

**Material Naming:**
```
MAT_[System]_[Component]_[Variant]

Examples:
  MAT_Gauge_Bezel_Chrome
  MAT_RCS_HotLeg
  MAT_Gauge_Fill_Temperature
```

### 2.6 UV Mapping

Ensure proper UV maps for textured materials:

1. Select object → Edit Mode
2. Select all faces (A)
3. U → Smart UV Project (for simple objects)
4. Or U → Unwrap (for precise control)

**Tips:**
- Keep UV islands within 0-1 space
- Add padding between islands (2-4 pixels at 1024px)
- Name UV maps: `UVMap` (primary), `UVMap_Lightmap` (for baked lighting)

### 2.7 Mesh Optimization

Optimize meshes for real-time rendering:

| Check | Target | How to Fix |
|-------|--------|------------|
| Triangle Count | <10K per object | Decimate modifier |
| N-gons | 0 | Triangulate (Ctrl+T) |
| Non-manifold | 0 | Mesh → Clean Up → Make Manifold |
| Loose Vertices | 0 | Mesh → Clean Up → Delete Loose |
| Duplicate Vertices | 0 | Mesh → Clean Up → Merge by Distance |

**To Check:**
- Select object → Edit Mode
- Select → All by Trait → Non-Manifold
- Mesh → Statistics (see face count)

---

## 3. Part B: FBX Export Settings

### 3.1 Export Dialog

File → Export → FBX (.fbx)

### 3.2 Recommended Settings

#### Include Panel
```
☑ Selected Objects       (export only what's selected)
☐ Active Collection      (usually unchecked)
☑ Object Types:
    ☑ Empty
    ☑ Camera             (only if needed)
    ☑ Lamp               (only if needed)
    ☑ Armature           (only if rigged)
    ☑ Mesh
    ☐ Other              (usually unchecked)
☐ Custom Properties      (usually unchecked)
```

#### Transform Panel
```
Scale:                   1.00
Apply Scalings:          FBX All
Forward:                 -Z Forward
Up:                      Y Up

☑ Apply Unit             (converts Blender units)
☑ Use Space Transform    
☑ Apply Transform        (bakes transforms into mesh)
```

#### Geometry Panel
```
☑ Apply Modifiers        (bakes modifiers)
Smoothing:               Face          (or Edge for hard edges)
☑ Export Subdivision Surface  (if using subdiv)
☐ Apply Modifiers with Subdivision (usually off)
☑ Triangulate Faces      (Unity prefers triangles)
☐ Loose Edges            (usually off)
☑ Tangent Space          (needed for normal maps)
```

#### Armature Panel (if rigged)
```
☐ Add Leaf Bones
Primary Bone Axis:       Y
Secondary Bone Axis:     X
Armature FBX Node Type:  Null
☑ Only Deform Bones
```

#### Animation Panel (if animated)
```
☐ Bake Animation         (check if exporting animations)
Key All Bones:           ☐
Force Start/End Keying:  ☑
Sampling Rate:           1.00
Simplify:                0.00
```

### 3.3 Export Presets

Save your settings as a preset for consistency:

1. Configure all settings
2. Click '+' next to Operator Presets
3. Name: "Unity_URP_Export"
4. Use this preset for all future exports

### 3.4 File Naming

```
[ModelName]_v[Version].fbx

Examples:
  RCS_Primary_Loop_v1.fbx
  Gauge_Arc_Temperature_v2.fbx
  RCP_Assembly_v1.fbx
```

---

## 4. Part C: Unity Import Configuration

### 4.1 Import Location

Place FBX files in organized folders:

```
Assets/
├── Models/
│   ├── RCS/
│   │   ├── RCS_Primary_Loop.fbx
│   │   └── RCS_Primary_Loop.fbx.meta
│   ├── Gauges/
│   │   ├── Gauge_Arc_Temperature.fbx
│   │   └── Gauge_Bar_Level.fbx
│   └── Controls/
│       └── RCP_Panel.fbx
```

### 4.2 Model Tab Settings

Select the FBX in Project window, then in Inspector:

#### Scene
```
Scale Factor:            1
Convert Units:           ☑ (checked)
Bake Axis Conversion:    ☐ (unchecked - let Unity handle)
Import BlendShapes:      ☑ (if using shape keys)
Import Visibility:       ☑
Import Cameras:          ☐ (usually off)
Import Lights:           ☐ (usually off)
```

#### Meshes
```
Mesh Compression:        Off (or Low for final builds)
Read/Write Enabled:      ☑ (needed for runtime modification)
Optimize Mesh:           ☑
Generate Colliders:      ☐ (add manually as needed)
```

#### Geometry
```
Keep Quads:              ☐ (triangulate)
Weld Vertices:           ☑
Index Format:            Auto
Legacy Blend Shape Normals: ☐
Normals:                 Import (or Calculate)
Blend Shape Normals:     Import
Normals Mode:            Area and Angle Weighted
Smoothness Source:       From Smoothing Groups
Smoothing Angle:         60
Tangents:                Calculate Mikktspace
Swap UVs:                ☐
Generate Lightmap UVs:   ☑ (for baked lighting)
```

### 4.3 Rig Tab Settings

For non-rigged models:
```
Animation Type:          None
```

For rigged models:
```
Animation Type:          Generic (or Humanoid for characters)
Avatar Definition:       Create From This Model
Root Node:               <auto>
```

### 4.4 Animation Tab Settings

If no animations:
```
Import Animation:        ☐
```

If importing animations:
```
Import Animation:        ☑
Bake Animations:         ☑
Resample Curves:         ☑
Anim. Compression:       Keyframe Reduction
Rotation Error:          0.5
Position Error:          0.5
Scale Error:             0.5
```

### 4.5 Materials Tab Settings

```
Material Creation Mode:  Import via MaterialDescription (recommended)
    OR
                         Standard (legacy)

sRGB Albedo Colors:      ☑
Location:                Use Embedded Materials
                         OR
                         Use External Materials (Legacy)

Naming:                  From Model's Material
Search:                  Recursive-Up
```

### 4.6 Apply Settings

After configuring:
1. Click **Apply** at bottom of Inspector
2. Wait for import to complete
3. Check Console for warnings/errors

---

## 5. Part D: Material Setup in Unity

### 5.1 Material Extraction

To edit materials:

1. Select FBX in Project window
2. Materials tab → Extract Materials...
3. Choose destination folder: `Assets/Materials/[System]/`
4. Click Extract

### 5.2 URP Lit Shader Setup

For each extracted material:

1. Select material in Project window
2. In Inspector, set Shader: **Universal Render Pipeline/Lit**

#### Surface Options
```
Workflow Mode:           Metallic
Surface Type:            Opaque (or Transparent)
Render Face:             Front (or Both for thin objects)
Alpha Clipping:          ☐ (unless using cutout)
```

#### Surface Inputs
```
Base Map:                [Albedo texture or color]
Metallic Map:            [Metallic texture] or slider 0-1
Smoothness:              [1 - Roughness from Blender]
Normal Map:              [Normal texture]
Height Map:              [Optional displacement]
Occlusion Map:           [AO texture]
Emission:                ☑ [Color and intensity]
```

### 5.3 Common Material Configurations

#### Metallic Surface (Bezel, Chrome)
```
Base Color:              (0.8, 0.8, 0.8)
Metallic:                0.9
Smoothness:              0.7
```

#### Matte Surface (Face, Housing)
```
Base Color:              (0.1, 0.1, 0.12)
Metallic:                0.0
Smoothness:              0.2
```

#### Emissive Surface (Indicators, Zones)
```
Base Color:              (0.2, 0.8, 0.2) [green example]
Metallic:                0.0
Smoothness:              0.4
Emission:                ☑ Enabled
Emission Color:          (0.2, 0.8, 0.2)
Emission Intensity:      2.0
```

#### Transparent Surface (Glass Tube)
```
Surface Type:            Transparent
Blending Mode:           Alpha
Base Color:              (0.6, 0.7, 0.8, 0.3)
Metallic:                0.0
Smoothness:              0.9
```

### 5.4 Material Organization

```
Assets/Materials/
├── RCS/
│   ├── MAT_RCS_HotLeg.mat
│   ├── MAT_RCS_ColdLeg.mat
│   └── MAT_RCS_Vessel.mat
├── Gauges/
│   ├── MAT_Gauge_Bezel.mat
│   ├── MAT_Gauge_Face.mat
│   └── MAT_Gauge_Needle.mat
└── Shared/
    ├── MAT_Metal_Chrome.mat
    └── MAT_Indicator_Green.mat
```

---

## 6. Part E: Prefab Creation

### 6.1 Creating a Prefab

1. Drag FBX from Project into Hierarchy
2. Configure the instance:
   - Add scripts
   - Assign materials
   - Set up colliders
   - Configure child objects
3. Drag from Hierarchy back into Project folder
4. Choose: **Original Prefab** (creates new prefab)

### 6.2 Prefab Organization

```
Assets/Prefabs/
├── RCS/
│   ├── RCS_Primary_Loop.prefab
│   └── RCP_Assembly.prefab
├── Gauges/
│   ├── Gauge_Arc_Temperature.prefab
│   ├── Gauge_Arc_Pressure.prefab
│   └── Gauge_Bar_Level.prefab
└── UI/
    └── RCP_ControlPanel.prefab
```

### 6.3 Prefab Variants

For gauges with different configurations:

1. Create base prefab: `Gauge_Arc_Base.prefab`
2. Right-click → Create → Prefab Variant
3. Name: `Gauge_Arc_Temperature.prefab`
4. Modify variant (labels, thresholds, colors)

### 6.4 Nested Prefabs

For complex assemblies:

```
RCS_Screen_Prefab
├── Panel_Left (nested prefab)
│   ├── Gauge_THot_1 (prefab instance)
│   ├── Gauge_THot_2 (prefab instance)
│   └── ...
├── Panel_Center (nested prefab)
│   └── RCS_Model (prefab instance)
└── Panel_Bottom (nested prefab)
    ├── RCP_Panel_1 (prefab instance)
    └── ...
```

---

## 7. Part F: Troubleshooting

### 7.1 Scale Issues

**Problem:** Model is too large or too small in Unity

**Solutions:**
1. Check Blender export scale (should be 1.0)
2. Check Unity import Scale Factor (should be 1.0)
3. Verify "Apply Unit" is checked in Blender export
4. Ensure "Convert Units" is checked in Unity import

**Quick Fix:** Adjust Scale Factor in Unity Model tab

### 7.2 Orientation Issues

**Problem:** Model is rotated incorrectly

**Solutions:**
1. Blender export: Forward = -Z, Up = Y
2. Apply rotation in Blender before export (Ctrl+A → Rotation)
3. Unity: Check "Bake Axis Conversion" if needed

### 7.3 Material Issues

**Problem:** Materials appear wrong or missing

**Solutions:**

| Issue | Cause | Fix |
|-------|-------|-----|
| Pink/Magenta | Missing shader | Assign URP shader |
| Too dark | Wrong color space | Check sRGB Albedo |
| Too shiny | Roughness inverted | Smoothness = 1 - Roughness |
| No emission | Emission disabled | Enable in material |
| Transparent not working | Wrong surface type | Set Surface Type: Transparent |

### 7.4 Hierarchy Issues

**Problem:** Objects not parented correctly

**Solutions:**
1. Check Blender hierarchy before export
2. Ensure empties are exported (check Include → Empty)
3. Reimport with "Preserve Hierarchy" if available

### 7.5 Animation Issues

**Problem:** Animations don't play or are wrong

**Solutions:**
1. Check Animation tab → Import Animation is enabled
2. Verify clip names and ranges
3. Check if transforms were baked
4. Ensure armature was exported (if rigged)

### 7.6 Performance Issues

**Problem:** Model causes low framerate

**Solutions:**

| Issue | Check | Target |
|-------|-------|--------|
| High poly | Triangle count | <50K per model |
| Many draw calls | Material count | <10 per model |
| Large textures | Texture resolution | 1024-2048 max |
| Overdraw | Transparent materials | Minimize layers |

### 7.7 UV/Texture Issues

**Problem:** Textures appear stretched or wrong

**Solutions:**
1. Check UV mapping in Blender
2. Ensure "Swap UVs" is unchecked in Unity
3. Verify texture import settings (wrap mode, filter)
4. Check texture resolution matches UV layout

---

## 8. Appendix: Quick Reference Checklists

### Pre-Export Checklist (Blender)

```
☐ All objects named correctly
☐ Hierarchy organized (static/animated separated)
☐ Pivot points set for animated objects
☐ Transforms applied (Ctrl+A → All Transforms)
☐ Materials use Principled BSDF
☐ Materials named with MAT_ prefix
☐ UV maps present for textured objects
☐ Mesh cleaned (no n-gons, non-manifold, loose verts)
☐ Triangle count acceptable
☐ Root object selected for export
```

### Export Settings Quick Reference

```
Transform:
  Scale: 1.0
  Forward: -Z Forward
  Up: Y Up
  ☑ Apply Transform

Geometry:
  ☑ Apply Modifiers
  ☑ Triangulate Faces
  ☑ Tangent Space
```

### Import Settings Quick Reference (Unity)

```
Model:
  Scale Factor: 1
  ☑ Convert Units
  ☑ Read/Write Enabled

Geometry:
  ☑ Weld Vertices
  Normals: Import
  ☑ Generate Lightmap UVs

Materials:
  Location: Use Embedded Materials
```

### Material Setup Quick Reference

```
Shader: Universal Render Pipeline/Lit

Opaque:
  Surface Type: Opaque
  
Transparent:
  Surface Type: Transparent
  Blending Mode: Alpha

Emissive:
  ☑ Emission
  Intensity: 1-5 (adjust to taste)
```

---

## Document History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2026-02-09 | Initial release |

---

**End of Manual**
