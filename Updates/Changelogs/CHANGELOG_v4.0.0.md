# CHANGELOG v4.0.0 — Reactor Operator Screen Visual & Functional Overhaul

**Version:** 4.0.0  
**Date:** 2026-02-11  
**Companion:** IMPLEMENTATION_PLAN_v4.0.0_ReactorOperatorScreen_Overhaul.md

---

## Summary

Major visual and functional upgrade to the Reactor Operator Screen (Screen 1). Adds a Blender 5.0
rendered panel background for professional mimic-board aesthetics, populates the previously empty
Rod Control section with functional UI, and fixes the Period gauge display bug.

---

## Changes by Stage

### Stage 1 — Bug Fix: Period Gauge Infinity Display

**File:** `Assets/Scripts/UI/MosaicBoard.cs` (**GOLD** — minimal fix)

- **Fixed:** `GetFormattedValue()` ReactorPeriod case now checks `float.IsInfinity() || float.IsNaN() || Mathf.Abs(value) > 1e10f`
- **Root cause:** `ReactorController.ReactorPeriod` returns `float.MaxValue` (3.4e+38) when subcritical. Previous code only checked `float.IsInfinity()`, which doesn't catch `float.MaxValue`.
- **Result:** Period gauge now displays `∞` instead of `340282300000000000000000000000000000000 s` when subcritical.
- Alarm state logic (`GetAlarmState`) verified — already correctly evaluates `float.MaxValue` as `AlarmState.Normal`. No change needed.

### Stage 2 — Rod Control Panel UI

**File created:** `Assets/Scripts/UI/RodControlPanel.cs` (NEW)

- Runtime component implementing `IMosaicComponent`
- 8 bank selector toggle buttons (SA, SB, SC, SD, D, C, B, A) — defaults to Bank D
- WITHDRAW / INSERT / STOP command buttons
- Digital step position readout (0-228 steps) for selected bank
- Rod motion status indicator (WITHDRAWING / INSERTING / STOPPED / TRIPPED) with color coding
- Controls auto-disable when reactor is tripped
- Sends commands via `ReactorController.WithdrawBank()` / `InsertBank()` / `StopBank()`

**File modified:** `Assets/Scripts/UI/OperatorScreenBuilder.cs` (not GOLD)

- `CreateRodControlSection()` rewritten to create full UI hierarchy:
  - Bank selector row with HorizontalLayoutGroup
  - Info row: selected bank name + step position readout + "STEPS" label
  - Motion status text
  - Command button row: WITHDRAW (green), STOP (red), INSERT (amber)
- MosaicControlPanel rod button references wired to same physical buttons

**File modified:** `Assets/Scripts/Reactor/ReactorController.cs` (**GOLD** — minimal passthroughs)

- Added `GetBankDirection(int bankIndex)` → delegates to `ControlRodBank.GetBankDirection()`
- Added `StopBank(int bankIndex)` → delegates to `ControlRodBank.StopBank()`
- Pure delegation, no physics changes

### Stage 3 — Blender Panel Artwork Scripts

**Files created:**

- `Assets/Scripts/Blender/create_reactor_panel.py` — Blender 5.0 Python script
  - Procedurally creates the full mimic board panel model
  - 7 PBR materials (panel base, bezel, recess, label, divider, green accent, red accent)
  - Main panel body, 9 left gauge bezels, core map frame with mode/filter button housings,
    8 right gauge bezels, detail panel, 5 bottom sections (rod control, bank positions,
    boron control, trip control, time control), alarm strip, section dividers, text labels
  - 3-point lighting setup (key, fill, rim)
  - All objects parented to single "ReactorOperatorPanel" root

- `Assets/Scripts/Blender/render_panel_textures.py` — Blender 5.0 Python script
  - Orthographic camera setup matching panel proportions
  - Cycles renderer: 3840×2160, 128 samples, GPU auto-detection, OpenImageDenoise
  - Transparent film background (RGBA)
  - Semi-transparent recess materials (display areas show through)
  - Output: `panel_base_color.png` to `Assets/Textures/ReactorOperatorPanel/`

### Stage 4 — Manual Blender Export & Unity Import

**User-performed steps:**

- Panel model created in Blender 5.0 and saved as `Assets/Models/ReactorPanel/ReactorOperatorPanel.blend`
- Panel texture rendered and saved as `Assets/Textures/ReactorOperatorPanel/panel_base_color.png`
- Texture imported into Unity with sRGB + Alpha Is Transparency + Max Size 4096 + High Quality compression

### Stage 5 — Unity Integration (Skin Component)

**File created:** `Assets/Scripts/UI/ReactorOperatorScreenSkin.cs` (NEW)

- Auto-loads `panel_base_color.png` from `Assets/Resources/ReactorOperatorPanel/` at runtime
- Creates full-screen `RawImage` as first child (renders behind all UI)
- Makes 5 panel background Image components transparent (left, core, right, detail, bottom)
- Silent fallback if texture not found (flat colored panels remain)
- Supports toggle on/off and manual texture assignment in Inspector
- `raycastTarget = false` — does not block button interaction

**File modified:** `Assets/Scripts/UI/OperatorScreenBuilder.cs` (not GOLD)

- `CreateOperatorScreen()` now adds `ReactorOperatorScreenSkin` component
- Collects all panel background Images into skin's `TransparentPanels` array

**Directories created:**

- `Assets/Scripts/Blender/`
- `Assets/Textures/ReactorOperatorPanel/`
- `Assets/Models/ReactorPanel/`
- `Assets/Resources/ReactorOperatorPanel/`

---

## GOLD Standard Files Modified

| File | Change Type | Justification |
|------|-------------|---------------|
| `MosaicBoard.cs` | Bug fix (1 line) | Period gauge displaying raw float.MaxValue instead of ∞ |
| `ReactorController.cs` | 2 passthrough methods added | `GetBankDirection()` and `StopBank()` — pure delegation to ControlRodBank, no physics |

---

## Files Not Modified

| File | Reason |
|------|--------|
| `ReactorOperatorScreen.cs` (GOLD) | No changes needed — skin activates automatically via component lifecycle |
| `CoreMosaicMap.cs` (GOLD) | No changes in this version |
| `ControlRodBank.cs` (GOLD) | Already had all required methods |
| All physics modules | No physics changes in v4.0.0 |

---

## Known Issues

- Panel texture must be manually copied to `Assets/Resources/ReactorOperatorPanel/` for auto-loading (or assigned in Inspector)
- Rod control currently operates on any bank; real Westinghouse panels have bank permissive interlocks — deferred to v4.1.0
- Per-bank rod position tracking only works for banks that have been individually manipulated; sequential withdraw/insert uses `ReactorController.WithdrawRods()` which operates on the automatic sequence
