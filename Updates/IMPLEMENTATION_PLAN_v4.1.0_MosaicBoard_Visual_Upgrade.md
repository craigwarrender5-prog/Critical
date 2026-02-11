# IMPLEMENTATION PLAN v4.1.0 — Mosaic Board Visual Upgrade

**Version:** 4.1.0  
**Date:** 2026-02-11  
**Scope:** Visual quality upgrade for all Mosaic Board UI elements  
**Dependencies:** v4.0.0 (Panel Skin), TextMesh Pro (already installed)

---

## Problem Summary

The Blender-rendered panel background (v4.0.0) provides professional hardware framing, but the actual
Mosaic Board display elements — gauges, core map cells, buttons, readouts — remain flat colored
rectangles with Unity's legacy `Text` component using `LegacyRuntime.ttf`. The result is a visual
mismatch: professional hardware surrounding DOS-era software.

### Current State
- **Gauges:** Flat dark rectangle + green Arial text. No analog elements, no fill bars.
- **Core map cells:** Flat colored squares, no border/bevel treatment.
- **Buttons:** Flat colored rectangles with plain white text.
- **Readouts:** Same flat rectangle + text pattern.
- **Font:** `LegacyRuntime.ttf` (Unity built-in Arial) — not monospaced, no visual effects.
- **Alarms:** Color swap only, no glow or visual urgency.

---

## Expectations (Target Visual Quality)

### Gauges
- Digital readout in **monospaced instrument font** (Electronic Highway Sign SDF) with subtle **green phosphor glow**
- **Horizontal fill bar** behind the value showing normalized position (0-100% of range)
- Fill bar color matches alarm state (green → amber → red)
- **Label text** in clean sans-serif (LiberationSans SDF) with subtle drop shadow
- Overall: looks like a real LED/LCD instrument readout

### Core Map Cells
- Each cell has a **1px inner border** for visual separation and depth
- Subtle **inner shadow** effect giving each cell a recessed appearance
- Smoother color gradients (existing logic is fine, just needs border treatment)

### Buttons
- **Rounded corner** appearance via 9-slice sprite
- Subtle **inner highlight** on top edge (top-lit bevel illusion)
- Press state shows **depressed** visual (darker, no highlight)

### Digital Readouts (step position, sim time, etc.)
- Same **Electronic Highway Sign** font treatment as gauge values
- Dark recessed background with subtle inner shadow
- Text glow matching value color

### Alarm Panel
- Alarm text gets **pulsing glow** effect via TMP material animation
- Active alarms have **red glow halo** behind text, not just color swap

---

## Proposed Fix — Detailed Technical Plan

### Stage 1: Font & Material Setup (No code changes yet)

**1a. Create TMP Font Materials for Instrument Displays**

Create custom TMP material presets (saved as .mat files) in `Assets/Resources/Fonts & Materials/`:

| Material Name | Base Font | Face Color | Outline | Glow | Underlay |
|---|---|---|---|---|---|
| `Instrument_Green_Glow.mat` | Electronic Highway Sign SDF | #00FF88 | None | Green, power 0.3, offset 0, inner 0.2 | Dark shadow, offset (0.5, -0.5) |
| `Instrument_Amber_Glow.mat` | Electronic Highway Sign SDF | #FFB830 | None | Amber, power 0.3 | Dark shadow |
| `Instrument_Red_Glow.mat` | Electronic Highway Sign SDF | #FF3344 | None | Red, power 0.4 | Dark shadow |
| `Instrument_Cyan.mat` | Electronic Highway Sign SDF | #00CCFF | None | Cyan, power 0.2 | Dark shadow |
| `Label_Standard.mat` | LiberationSans SDF | #8090A0 | None | None | Subtle shadow, offset (0.3, -0.3) |
| `Label_Section.mat` | LiberationSans SDF | #C8D0D8 | Thin dark, width 0.05 | None | Shadow |
| `Alarm_Pulse_Red.mat` | Electronic Highway Sign SDF | #FF3344 | None | Red, power 0.6 | None |

These materials are created programmatically by a setup script at editor time, ensuring reproducibility.

**1b. Generate UI Sprites**

Create procedural sprite textures in `Assets/Resources/Sprites/`:

| Sprite | Size | Description |
|---|---|---|
| `gauge_bg.png` | 256x64 | Horizontal gradient: dark center (#0A0A10) fading to slightly lighter edges (#14141A). Subtle inner shadow. |
| `cell_bg.png` | 32x32 | Square with 1px dark border (#0A0A0F), 1px inner highlight on top/left (#2A2A35), fill area for color. 9-slice with 2px borders. |
| `button_bg.png` | 64x32 | Rounded rect (r=3px), top-edge highlight, bottom-edge shadow. 9-slice with 4px borders. |
| `readout_bg.png` | 128x32 | Recessed rectangle with inner shadow all around, very dark fill. |
| `fill_bar.png` | 256x16 | Simple horizontal fill with subtle gradient and rounded left cap. |

### Stage 2: MosaicGauge Visual Upgrade

**Modify `OperatorScreenBuilder.CreateGauge()`** — replace legacy `Text` with TMP components:

- Replace `Text valueText` → `TextMeshProUGUI valueText` using Electronic Highway Sign SDF
- Replace `Text labelText` → `TextMeshProUGUI labelText` using LiberationSans SDF  
- Add `gauge_bg.png` sprite as background Image (replaces flat color)
- Add fill bar Image child (horizontal filled, anchored to gauge area)
- Add subtle glow Image behind value text (tinted to alarm color)

**Modify `MosaicGauge`** — update to use TMP types:

- Change `public Text ValueText` → `public TMPro.TextMeshProUGUI ValueText`
- Change `public Text LabelText` → `public TMPro.TextMeshProUGUI LabelText`  
- Change `public Text UnitsText` → `public TMPro.TextMeshProUGUI UnitsText`
- Add `public Image FillBarImage` for the horizontal fill indicator
- Add `public Image GlowImage` for the value glow backdrop
- Update `UpdateDigitalDisplay()` to use TMP formatting
- Update `UpdateColors()` to:
  - Swap TMP material preset based on alarm state (green/amber/red glow)
  - Tint fill bar to alarm color
  - Tint glow image to alarm color with alpha pulse
- Add `UpdateFillBarIndicator()` — maps normalized value to fill bar width
- Add `UpdateGlowEffect()` — subtle glow behind readout, intensity varies with alarm state

### Stage 3: Core Map Cell Visual Upgrade

**Modify `CoreMosaicMap`** — update cell creation:

- Replace flat `Image` per cell with `cell_bg.png` sprite (9-sliced)
- Cell background sprite provides border + inner shadow automatically
- Color still applied to Image.color (tints the sprite correctly)
- RCCA text overlay uses `TextMeshProUGUI` with Electronic Highway Sign SDF (tiny size)

### Stage 4: Button & Readout Visual Upgrade

**Modify `OperatorScreenBuilder.CreateButton()`:**

- Replace flat `Image` background with `button_bg.png` sprite (9-sliced)
- Replace `Text` label with `TextMeshProUGUI` using LiberationSans SDF
- Update `ColorBlock` to use slightly different shading for press/hover:
  - Normal: sprite tinted to button color
  - Hover: sprite tinted lighter
  - Pressed: sprite tinted darker (simulates physical depression)

**Modify `OperatorScreenBuilder.CreateDigitalReadout()`:**

- Replace flat `Image` background with `readout_bg.png` sprite
- Replace `Text` with `TextMeshProUGUI` using Electronic Highway Sign SDF + green glow material

**Modify Rod Control Panel, Boron Control, Time Control sections:**

- Update text references from `Text` → `TextMeshProUGUI`
- Apply instrument font + glow materials to readout values

### Stage 5: Alarm Visual Enhancement

**Modify `MosaicBoard.UpdateAlarmFlash()`:**

- When alarm flashing, animate TMP glow power on alarm gauge materials
- Pulse between glow power 0.2 (dim) and 0.8 (bright) at flash rate
- Alarm strip text gets pulsing red glow

**Modify `MosaicAlarmPanel` (if exists):**

- Alarm entries use TMP with red glow material
- Acknowledged alarms switch to amber, unacknowledged pulse

### Stage 6: Sprite Generation Script + Polish

**Create `InstrumentSpriteGenerator.cs`** (Editor script):

- Generates all procedural sprites programmatically (gauge_bg, cell_bg, button_bg, readout_bg, fill_bar)
- Saves to `Assets/Resources/Sprites/`
- Menu item: **Critical > Generate Instrument Sprites**
- This ensures sprites are always reproducible and version-controlled as code

**Create `InstrumentMaterialSetup.cs`** (Editor script):

- Creates all TMP material presets programmatically
- Saves to `Assets/Resources/Fonts & Materials/`
- Menu item: **Critical > Setup Instrument Materials**
- References Electronic Highway Sign SDF and LiberationSans SDF font assets

**Testing:**
- Verify all 17 gauges display correctly with new visuals
- Verify core map 193 cells render with border treatment
- Verify all buttons respond to hover/press with new sprites
- Verify alarm flash produces glow pulsing effect
- Verify rod control panel readouts use instrument font
- Performance check (TMP + sprites should be same or better than legacy Text)

---

## Files Created (New)

| File | Location | Purpose |
|---|---|---|
| `InstrumentSpriteGenerator.cs` | `Assets/Scripts/UI/Editor/` | Procedural sprite generation (editor-time) |
| `InstrumentMaterialSetup.cs` | `Assets/Scripts/UI/Editor/` | TMP material preset creation (editor-time) |
| Procedural sprites (5 PNGs) | `Assets/Resources/Sprites/` | UI sprite textures |
| TMP Materials (7 .mat files) | `Assets/Resources/Fonts & Materials/` | Instrument display materials |

## Files Modified

| File | Status | Changes |
|---|---|---|
| `MosaicGauge.cs` | **GOLD** | `Text` → `TextMeshProUGUI`, add FillBar/Glow references, material swapping |
| `MosaicBoard.cs` | **GOLD** | Alarm flash glow animation |
| `CoreMosaicMap.cs` | **GOLD** | Cell sprite + TMP for RCCA text |
| `OperatorScreenBuilder.cs` | Not GOLD | `CreateGauge()`, `CreateButton()`, `CreateDigitalReadout()` rewritten for TMP + sprites |
| `RodControlPanel.cs` | Not GOLD | `Text` → `TextMeshProUGUI` references |
| `ReactorOperatorScreen.cs` | **GOLD** | `Text` → `TextMeshProUGUI` for status displays |

## Unaddressed Issues (Deferred)

| Issue | Reason | Target |
|---|---|---|
| Analog dial needles on gauges | Requires gauge face sprite design, significant additional work | v4.2.0 |
| CRT scanline overlay shader | Pure cosmetic, diminishing returns vs. effort | v4.2.0 |
| Per-assembly fuel temperature gradient in core cells | Requires physics model per-assembly output (not yet available) | v5.0.0 |
| Screens 2-8 visual upgrade | Same TMP treatment needed, but one screen at a time | v4.2.0+ |

---

## GOLD Standard Impact Assessment

The GOLD standard files modified in this update receive **type-only changes** (legacy `Text` → `TextMeshProUGUI`)
and **additive visual fields** (FillBar, GlowImage references). No physics logic, no data flow changes,
no behavioral modifications. The changes are strictly visual layer upgrades.

`TextMeshProUGUI` is a drop-in replacement for `Text` in Unity UI — it implements the same
`ILayoutElement` interfaces and works identically within Canvas rendering. The TMP package is an
official Unity first-party package that ships with every Unity installation since 2019.
