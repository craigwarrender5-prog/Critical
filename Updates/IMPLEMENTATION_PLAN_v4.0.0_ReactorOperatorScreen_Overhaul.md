# Implementation Plan v4.0.0 — Reactor Operator Screen Visual & Functional Overhaul

**Version:** 4.0.0  
**Date:** 2026-02-10  
**Status:** PENDING APPROVAL  
**Scope:** Major — Blender 5.0 panel artwork + Unity UI functional overhaul  

---

## Problem Summary

The Reactor Operator Screen (Screen 1) has three categories of issues:

### 1. Visual Quality — "Boring"
The current screen is purely functional with flat colored rectangles, no depth, no texture, and no physical presence. It looks like a developer debug screen, not a nuclear control room mimic board. Real mimic boards have beveled panel housings, backlit indicators, engraved labels, textured metal/plastic surfaces, and a sense of physical weight.

### 2. Missing Functional Controls
- **Rod Control Section** — The bottom-left "ROD CONTROL" panel is empty. No WITHDRAW/INSERT/STOP buttons, no bank selector, no step counter display. The `MosaicControlPanel.cs` component has all the logic wired but the `OperatorScreenBuilder` never creates the actual UI elements (buttons, text) inside the Rod Control section.
- **Bank Filter Buttons** — The ALL/SA/SB/.../A buttons only filter the core map display. There is no mechanism to select which bank the rod WITHDRAW/INSERT controls will operate on.

### 3. Bugs
- **Period Gauge Display** — Shows `340282300000000000000000000000000000000 s` instead of `∞` when subcritical. Root cause: `ReactorController.ReactorPeriod` returns `float.MaxValue` (3.40282e+38) when subcritical, but `MosaicBoard.GetFormattedValue()` only checks for `float.IsInfinity()`. The value `float.MaxValue` is not infinity, so it falls through to the `{value:F1} s` format.

---

## Expectations — What "Correct" Looks Like

### Visual
- The static panel chassis (bezels, frames, section dividers, labels, backgrounds) should look like a physical mimic board panel — textured, beveled, with subtle lighting and depth
- Dynamic elements (gauge values, core map cells, button states) remain Unity UI elements layered on top
- The overall aesthetic should evoke a Westinghouse 4-Loop PWR control room panel, not a software debug screen

### Functional
- Rod control section should have: WITHDRAW button, INSERT button, STOP button, bank selector (SA through A), selected bank indicator, and step position readout for the selected bank
- Period gauge should display `∞` when reactor period is extremely large (subcritical/critical with negligible SUR)

### Architecture
- GOLD standard scripts (ReactorOperatorScreen.cs, CoreMosaicMap.cs, MosaicBoard.cs) are modified only for bug fixes or minimal integration points
- Blender assets are static background textures only — all dynamic behavior stays in Unity
- No physics modules are modified

---

## Proposed Fix — Detailed Technical Plan

### Architecture Overview

```
Blender 5.0                          Unity 6000.3.4f1
─────────────                        ──────────────────
Panel artwork model          ──FBX──>  Import as mesh
  - Main panel background              Render to texture or use as
  - Gauge bezels (18x)                   RawImage/Sprite backgrounds
  - Core map frame                     
  - Bottom panel sections              Dynamic UI elements layered on top:
  - Section labels (engraved)            - MosaicGauge value/color text
  - Rod control panel frame              - CoreMosaicMap 193 cells
  - Decorative elements                  - Buttons, sliders, indicators
                                         - New rod control UI elements
```

### Stage 1 — Bug Fixes (Unity only, no Blender)

**1a. Period Gauge Fix**

In `MosaicBoard.cs`, modify `GetFormattedValue()` for ReactorPeriod:

```csharp
// CURRENT (buggy):
GaugeType.ReactorPeriod when float.IsInfinity(value) => "∞",
GaugeType.ReactorPeriod => $"{value:F1} s",

// FIXED:
GaugeType.ReactorPeriod when float.IsInfinity(value) || float.IsNaN(value) || Mathf.Abs(value) > 1e10f => "∞",
GaugeType.ReactorPeriod => $"{value:F1} s",
```

This catches `float.MaxValue`, `float.PositiveInfinity`, `float.NaN`, and any absurdly large period (>10 billion seconds ≈ 317 years).

**Files modified:** `MosaicBoard.cs` (GOLD — minimal bug fix, documented)

### Stage 2 — Rod Control UI (Unity only, no Blender)

Create functional rod control elements inside the existing Rod Control section. The `MosaicControlPanel.cs` already has all the logic — it just needs actual UI elements created and wired.

**2a. New Script: `RodControlPanelBuilder.cs`**

Editor script that creates within the "RodControlSection" panel:
- Bank selector: 8 toggle buttons (SA, SB, SC, SD, D, C, B, A) — selects which bank rod motion commands apply to
- WITHDRAW button (green outline)
- INSERT button (amber outline)  
- STOP button (red, larger)
- Selected bank step position readout (digital display showing 0-228 steps)
- Rod motion indicator (WITHDRAWING / INSERTING / STOPPED / TRIPPED)

**2b. New Script: `RodControlPanel.cs`**

Runtime component that:
- Manages bank selection state
- Sends withdraw/insert/stop commands for the selected bank to ReactorController
- Displays step position for the selected bank
- Shows rod motion status
- Disables controls when reactor is tripped
- Integrates with existing `MosaicControlPanel` or replaces the rod control portion of it

**2c. Modify `OperatorScreenBuilder.cs`**

Update `CreateRodControlSection()` to create the actual UI elements instead of the current empty panel.

**Files created:** `RodControlPanelBuilder.cs`, `RodControlPanel.cs`  
**Files modified:** `OperatorScreenBuilder.cs` (not GOLD)

### Stage 3 — Blender Panel Artwork Creation

**3a. Blender Python Script: `create_reactor_panel.py`**

A Python script that runs inside Blender 5.0 to procedurally create the entire panel model. This avoids you needing to know how to model in Blender — you just run the script.

The script will create:

**Main Panel Background (1920×1080 proportions)**
- Flat panel with subtle brushed-metal material
- Very slight bevel on outer edges
- Dark gunmetal grey (#1A1A1F equivalent but with material depth)

**Left Gauge Panel (9 bezels)**
- 9 rectangular gauge bezel cutouts with raised frames
- Each bezel has a recessed area (dark) where the Unity gauge text will overlay
- Subtle rim lighting on bezel edges
- Engraved section label "NUCLEAR INSTRUMENTATION" at top

**Core Map Frame**
- Large central recessed area with raised border
- Corner radius matching the octagonal cross-section hint
- Display mode button housings at top (4 recessed rectangles)
- Bank filter button housings at bottom (9 recessed rectangles)
- Engraved label "CORE MAP" or "REACTOR CORE"

**Right Gauge Panel (8 bezels)**
- Same bezel style as left panel
- Engraved label "THERMAL-HYDRAULIC"

**Bottom Panel Sections**
- Rod Control section with button bezels and display cutout
- Bank Positions section with 8 vertical bar graph housings
- Boron Control section with button bezels and readout cutout
- Trip Control section with large trip button housing (red ring) and reset button housing
- Time Control section with readout cutouts and button bezels
- Alarm strip section (long horizontal recessed area)
- Section dividers between each area

**Materials:**
- Panel body: Dark brushed metal/matte plastic (PBR — Base Color, Metallic, Roughness, Normal)
- Bezels: Slightly lighter metallic with edge highlight
- Recessed areas: Very dark matte (display areas)
- Labels: Engraved effect (normal map or geometry)
- Accent rings: Subtle colored edge lighting (green for normal, red for trip)

**Camera & Render Setup:**
- Orthographic camera facing the panel straight-on
- Render resolution: 3840×2160 (4K, scaled down to 1920×1080 in Unity for crisp quality)
- Output: PNG with alpha where dynamic overlay areas are transparent
- Additional output: Normal map for fake depth in Unity (optional enhancement)

**3b. Blender Python Script: `render_panel_textures.py`**

Separate script that:
- Sets up the orthographic camera
- Configures Cycles render settings for a clean product render
- Renders the base color texture (with transparent cutouts for dynamic areas)
- Optionally renders a normal map pass
- Saves to `Assets/Textures/ReactorOperatorPanel/`

### Stage 4 — Blender Export & Unity Import (Manual Steps — See Instruction Manual)

This stage is entirely manual on your part — detailed in the Instruction Manual below.

- Export FBX from Blender (or just the rendered textures)
- Import into Unity project
- Configure texture import settings

### Stage 5 — Unity Integration (Overlay Dynamic UI on Panel Artwork)

**5a. New Script: `ReactorOperatorScreenSkin.cs`**

Runtime component that:
- Loads the panel background texture as a RawImage behind all existing UI
- Positions and scales the texture to match the 1920×1080 layout
- Sets existing panel backgrounds to transparent (so the Blender artwork shows through)
- Maintains all existing functionality — this is purely additive

**5b. Modify `OperatorScreenBuilder.cs`**

Update to:
- Add the panel background RawImage as the first child (renders behind everything)
- Set gauge panel Image colors to transparent (bezel artwork provides the visual frame)
- Set button Image colors to transparent where Blender bezels provide the housing
- Keep all interactive elements (buttons, text, gauges) functional

**5c. Update `ReactorOperatorScreen.cs`**

Minimal changes:
- Add reference to the skin component
- Ensure skin is activated/deactivated with screen visibility

**Files created:** `ReactorOperatorScreenSkin.cs`  
**Files modified:** `OperatorScreenBuilder.cs`, `ReactorOperatorScreen.cs` (GOLD — minimal integration only)

### Stage 6 — Polish & Testing

- Verify all 193 core map cells align correctly over the panel artwork
- Verify all 17 gauge readouts sit correctly within bezels
- Verify rod control panel is functional
- Verify period gauge displays `∞` correctly
- Verify all button hover/press states work over transparent backgrounds
- Performance check (4K texture impact on frame rate)
- Visual alignment at different resolutions (CanvasScaler handles this)

---

## Unaddressed Issues

| Issue | Reason | Future Target |
|-------|--------|---------------|
| Per-bank rod position tracking in ReactorController | ReactorController currently only tracks Bank D position; full 8-bank position tracking requires physics module changes | v4.1.0 |
| Rod step demand vs actual position | Real plants have demand/actual mismatch indicators; requires rod control system model | v4.1.0 |
| Gauge analog dial needles | Current gauges are digital-only; adding analog needle overlays on the Blender bezels | v4.1.0 |
| Core map cell shapes (hexagonal vs square) | Real PWR assemblies are square; current rendering is square; no change needed | N/A |
| Screen 2-8 visual overhaul | Same Blender treatment for all other screens | v4.2.0+ |

---

## Blender 5.0 Instruction Manual — Step by Step for Dummies

### Prerequisites

1. **Install Blender 5.0** from https://www.blender.org/download/ (version 5.0.1 or later recommended)
2. Windows: Run the installer, accept defaults, finish
3. Verify installation: Launch Blender, check splash screen says "5.0"

### PART A — Creating the Panel Model (Running the Blender Script)

**Step 1: Launch Blender**
- Double-click the Blender icon on your desktop or Start Menu
- You'll see a splash screen — click anywhere outside it to dismiss it
- You'll see a default scene with a cube, camera, and light

**Step 2: Open the Scripting Workspace**
- Look at the very top of the Blender window — you'll see tabs: "Layout", "Modeling", "Sculpting", etc.
- Click on the **"Scripting"** tab (far right)
- The layout will change to show a text editor on the left and a Python console on the bottom

**Step 3: Delete the Default Scene**
- Before running the script, clear the default objects
- Press **A** (select all) in the 3D viewport (top-right area)
- Press **X** then click **Delete**
- The viewport should now be empty (grey grid only)

**Step 4: Open the Script**
- In the text editor panel (large panel on the left side), click **Open** (at the top of the text editor)
- Navigate to: `C:\Users\craig\Projects\Critical\Assets\Scripts\Blender\create_reactor_panel.py`
- Click **Open Text**
- You should now see the Python code in the text editor

**Step 5: Run the Script**
- With the script visible in the text editor, click the **▶ Play** button (triangle icon at the top of the text editor) or press **Alt+P**
- Wait — this may take 10-30 seconds depending on your PC
- You should see the panel model appear in the 3D viewport
- The bottom-left corner will show a status message when complete
- If you see errors in the console (bottom panel), screenshot them and share with me

**Step 6: Inspect the Model**
- Use **middle mouse button** to orbit the view
- Use **scroll wheel** to zoom in/out
- Press **Numpad 1** to view from the front (straight on)
- Press **Numpad 5** to toggle perspective/orthographic view
- You should see the flat panel with bezels, frames, recessed areas, and labels

**Step 7: Save the Blender File**
- Press **Ctrl+S**
- Navigate to: `C:\Users\craig\Projects\Critical\Assets\Models\ReactorPanel\`
- Create this folder if it doesn't exist (click "Create New Directory" icon or make it in Windows Explorer first)
- Save as: `ReactorOperatorPanel.blend`

### PART B — Rendering the Panel Textures

**Step 8: Open the Render Script**
- Still in the Scripting workspace
- In the text editor, click **Open** again
- Navigate to: `C:\Users\craig\Projects\Critical\Assets\Scripts\Blender\render_panel_textures.py`
- Click **Open Text**

**Step 9: Run the Render Script**
- Click the **▶ Play** button or press **Alt+P**
- This will take longer — potentially 1-5 minutes depending on your GPU
- The script will:
  - Set up an orthographic camera
  - Configure render settings
  - Render the panel to a PNG file
  - Save it automatically
- When complete, you'll see a status message in the console

**Step 10: Verify the Output**
- Open Windows Explorer
- Navigate to: `C:\Users\craig\Projects\Critical\Assets\Textures\ReactorOperatorPanel\`
- You should see: `panel_base_color.png` (and optionally `panel_normal.png`)
- Open the PNG to verify it looks correct — you should see the panel artwork with transparent (checkered) areas where the dynamic UI elements go

### PART C — Importing into Unity

**Step 11: Switch to Unity**
- Alt+Tab back to Unity Editor
- Unity should automatically detect the new files in the Assets folder
- Wait for the import progress bar to complete (bottom of Unity window)

**Step 12: Verify Import**
- In Unity's **Project** window, navigate to `Assets > Textures > ReactorOperatorPanel`
- Click on `panel_base_color.png`
- In the **Inspector** (right side), verify:
  - **Texture Type:** Default
  - **sRGB (Color Texture):** ✅ checked
  - **Alpha Source:** Input Texture Alpha
  - **Alpha Is Transparency:** ✅ checked
- If any of these are wrong, change them and click **Apply** at the bottom of the Inspector

**Step 13: Configure Texture Settings**
- Still with `panel_base_color.png` selected in the Inspector:
- **Max Size:** 4096 (this keeps the 4K quality)
- **Compression:** High Quality
- **Format:** Automatic
- Click **Apply**

**Step 14: If You Also Have a Normal Map (`panel_normal.png`)**
- Click on `panel_normal.png` in the Project window
- In the Inspector:
  - **Texture Type:** Normal map
  - **sRGB:** ❌ unchecked (normal maps are linear)
  - **Max Size:** 4096
  - Click **Apply**

### PART D — Applying the Panel (Automated by Script)

**Step 15: Run the Unity Integration**
- In Unity, go to the menu bar: **Critical > Apply Operator Screen Skin** (this menu item will be created in Stage 5)
- This will automatically:
  - Add the panel texture as a background image
  - Make gauge backgrounds transparent
  - Position everything correctly
- Press **Play** to test

### PART E — Troubleshooting

**Problem: Script fails in Blender with an error**
- Screenshot the error text in the console panel
- Share it with me — I'll fix the script

**Problem: Panel looks too dark/bright in Unity**
- The Blender render uses physically-based lighting; Unity's UI uses sRGB
- In the texture import settings, toggle sRGB on/off to see which looks correct
- The script should handle this, but manual adjustment may be needed

**Problem: Dynamic elements are misaligned with bezels**
- The Blender script and Unity script use the same proportional layout (0-15% left panel, 15-65% core map, etc.)
- If there's a slight offset, the Unity integration script has fine-tuning parameters in the Inspector

**Problem: Blender renders a black image**
- Make sure you're in the Blender file where you ran the creation script (Step 7)
- The render script needs the panel model to be present in the scene
- Try: Open the .blend file from Step 7, then run the render script

**Problem: Unity doesn't detect new files**
- Right-click in the Project window → **Refresh** or **Reimport All**
- Or close and reopen Unity

---

## File Inventory

### New Files Created

| File | Location | Purpose |
|------|----------|---------|
| `create_reactor_panel.py` | `Assets/Scripts/Blender/` | Blender 5.0 script — creates panel model |
| `render_panel_textures.py` | `Assets/Scripts/Blender/` | Blender 5.0 script — renders panel textures |
| `RodControlPanel.cs` | `Assets/Scripts/UI/` | Runtime rod control UI component |
| `RodControlPanelBuilder.cs` | `Assets/Scripts/UI/` | Editor script — creates rod control UI elements |
| `ReactorOperatorScreenSkin.cs` | `Assets/Scripts/UI/` | Runtime — applies panel texture background |
| `ReactorOperatorPanel.blend` | `Assets/Models/ReactorPanel/` | Blender source file (created by you) |
| `panel_base_color.png` | `Assets/Textures/ReactorOperatorPanel/` | Rendered panel texture (created by you) |

### Modified Files

| File | Change | GOLD? |
|------|--------|-------|
| `MosaicBoard.cs` | Period gauge `float.MaxValue` bug fix | Yes — minimal bug fix |
| `OperatorScreenBuilder.cs` | Rod control UI creation + skin integration | No |
| `ReactorOperatorScreen.cs` | Skin reference + rod control reference | Yes — minimal integration |

---

## Implementation Order

```
Stage 1  →  Bug Fixes (Period gauge)                    [Unity code only]
Stage 2  →  Rod Control UI                              [Unity code only]
Stage 3  →  Blender Panel Creation Scripts              [Python scripts only]
Stage 4  →  Manual: Blender Export & Unity Import       [You do this]
Stage 5  →  Unity Integration (overlay on panel art)    [Unity code only]
Stage 6  →  Polish & Testing                            [Both]
```

Each stage will be implemented one at a time. You will be consulted before proceeding to the next stage.

---

## Dependencies

- Blender 5.0.1+ installed on your machine
- Unity 6000.3.4f1 (current project version — no upgrade needed)
- No physics module changes required
- No external packages or plugins required

---

## Estimated Effort

| Stage | Effort (Claude) | Effort (You) |
|-------|-----------------|--------------|
| Stage 1 | 15 min | 0 |
| Stage 2 | 2-3 hr | 0 |
| Stage 3 | 3-4 hr | 0 |
| Stage 4 | 0 | 30-60 min (following manual) |
| Stage 5 | 1-2 hr | 0 |
| Stage 6 | 1 hr | 15 min testing |

---

*Implementation Plan prepared by Claude. Do not implement until explicitly instructed to proceed.*
