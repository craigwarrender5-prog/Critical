# Implementation Plan v4.2.2 — Bottom Panel Layout Fix & Annunciator-Style Alarm Panel

## Version: 4.2.2
## Date: 2026-02-11
## Status: AWAITING APPROVAL
## Scope: Reactor Operator Screen (Screen 1) Bottom Panel Visual Fixes

---

## Problem Summary

Two visual issues on the Reactor Operator Screen (Screen 1) bottom panel:

### Issue 1: Panel Overlay Collision — RodDisplaySection Spans Full Height

In `OperatorScreenBuilder.CreateBottomPanel()`, the bottom panel (`y: 0.0 → 0.26`) contains two layout rows:

- **Upper row** (controls): Rod Control, Boron, Trip, Time — anchored at `y: 0.55 → 0.95`
- **Lower row** (alarms): Alarm Strip — anchored at `y: 0.05 → 0.50`

However, the **RodDisplaySection** ("BANK POSITIONS") is anchored at `(0.16, 0.05)` to `(0.45, 0.95)` — it spans the **full height** of the bottom panel, overlapping directly on top of the AlarmStrip. This causes the flat-colored `COLOR_GAUGE_BG` panel for Bank Positions to visually extend over the alarm section and the Blender panel artwork behind it, producing a "cut off" appearance where the UI looks like it bleeds improperly.

### Issue 2: Alarm Panel Uses Text-List Style Instead of Annunciator Tiles

The current `MosaicAlarmPanel.cs` displays alarms as a scrolling text list with severity color coding. This looks like a debug output rather than a professional control room panel.

The Heatup Validation Visual's annunciator panel (`HeatupValidationVisual.Annunciators.cs`) uses proper nuclear I&C annunciator tiles — illuminated rectangular tiles in a grid layout with dark/lit states, color-coded backgrounds (green for normal status, amber for warnings, red for alarms), bordered tiles, and centered multi-line labels. These look authentic to a real Westinghouse control room annunciator window and should be the standard for alarms across the project.

---

## Expectations

### Issue 1 — Correct Layout
The RodDisplaySection should only occupy the upper row of the bottom panel, alongside the other control sections. The AlarmStrip should have its own unobstructed space in the lower row without any overlapping panels. When the Blender panel artwork is active, no flat-colored UI panels should bleed over or obscure the rendered background.

### Issue 2 — Annunciator-Style Alarms
The alarm section on Screen 1 should display alarms as illuminated annunciator tiles in a grid, matching the visual style established in the Heatup Validation Visual. Tiles should be:
- **Dark/off** when the alarm condition is inactive
- **Illuminated with colored background** when active (green for normal status, amber for warnings, red for alarms)
- **Bordered** with the active color when lit, dim border when off
- **Multi-line centered labels** (e.g., "PRESS\nHIGH")
- Arranged in a responsive grid that fills the available space

---

## Proposed Fix

### Stage 1: Fix RodDisplaySection Layout Overlap

**File Modified:** `OperatorScreenBuilder.cs`

**Change:** In `CreateRodDisplaySection()`, change the anchor from spanning the full bottom panel height to only spanning the upper row, consistent with the other control sections.

```
Current:  anchorMin = (0.16, 0.05)  anchorMax = (0.45, 0.95)  ← spans full height
Proposed: anchorMin = (0.16, 0.55)  anchorMax = (0.45, 0.95)  ← upper row only
```

This aligns RodDisplaySection with Rod Control (`0.01–0.15, 0.55–0.95`), Boron Control (`0.46–0.60, 0.55–0.95`), Trip Control (`0.61–0.75, 0.55–0.95`), and Time Control (`0.76–0.99, 0.55–0.95`) — all in the upper row.

**Validation:**
- Bank Positions panel no longer overlaps the alarm strip
- Blender panel artwork visible in the gap between upper controls and lower alarm strip
- All control section labels and content remain readable
- No change to functionality — only anchor positions

### Stage 2: Replace MosaicAlarmPanel with Annunciator Tile Grid

**Files Modified:**
- `MosaicAlarmPanel.cs` — Major rewrite to render annunciator tiles instead of text list

**Approach:** Rewrite `MosaicAlarmPanel` to use a tile-based grid layout modeled directly on the `HeatupValidationVisual.Annunciators.cs` approach, but adapted for the Unity UI Canvas system (using `Image` + `TextMeshProUGUI` components) rather than IMGUI `GUI.Label` calls.

**Tile Design (matching Heatup Visual standard):**
- Each tile is a `RectTransform` with an `Image` background and a `TextMeshProUGUI` label
- **Inactive state:** Background = dark (`_cAnnOff` equivalent: `0.12, 0.13, 0.16`), text = dim secondary, border = dim
- **Active Normal/Status:** Background = dim green (`0.10, 0.35, 0.12`), text = bright green (`0.18, 0.82, 0.25`), border = green
- **Active Warning:** Background = dim amber (`0.45, 0.35, 0.00`), text = bright amber (`1.0, 0.78, 0.0`), border = amber
- **Active Alarm:** Background = dim red (`0.50, 0.08, 0.08`), text = bright red (`1.0, 0.18, 0.18`), border = red
- Border: 1px on all edges, active color when lit, dim (`0.2, 0.22, 0.26`) when off
- Label: Multi-line centered, Electronic Highway Sign font (matching existing instrument aesthetic), ~9-10pt

**Tile Set (initial — matches key reactor screen alarms):**

| # | Label | Source | IsAlarm |
|---|-------|--------|---------|
| 0 | REACTOR\nTRIPPED | ReactorController.IsTripped | true |
| 1 | NEUTRON\nPOWER HI | NeutronPower > 1.03 | true |
| 2 | STARTUP\nRATE HI | StartupRate > 1.0 dpm | true |
| 3 | PRESS\nLOW | Pressure < 2185 psia | true |
| 4 | PRESS\nHIGH | Pressure > 2285 psia | true |
| 5 | T-AVG\nLOW | Tavg < 547°F | true |
| 6 | T-AVG\nHIGH | Tavg > 567°F | true |
| 7 | SUBCOOL\nLOW | Subcooling < 20°F | true |
| 8 | PZR LVL\nLOW | PzrLevel < 17% | true |
| 9 | PZR LVL\nHIGH | PzrLevel > 92% | true |
| 10 | OVERPOWER\nΔT | OverpowerDeltaT trip | true |
| 11 | OVERTEMP\nΔT | OvertempDeltaT trip | true |
| 12 | ROD BOTTOM\nALARM | Any rod at 0 steps | true |
| 13 | REACTOR\nCRITICAL | ReactorCritical | false (status) |
| 14 | AUTO ROD\nCONTROL | AutoRodControl active | false (status) |
| 15 | PZR HTRS\nON | Heaters energized | false (status) |

The exact tile conditions will be bound through the existing `MosaicBoard` alarm infrastructure and `ReactorController` data. Tiles that cannot be evaluated (because the backing physics doesn't exist yet) will remain dark/inactive with no false alarms.

**Grid Layout:**
- Uses `GridLayoutGroup` component for automatic tile arrangement
- Tile size calculated from available panel space (responsive)
- Typically 4 columns × 4 rows for 16 tiles at the current panel dimensions
- Spacing matches Heatup Visual standard: 3px gap

**Retained from Current MosaicAlarmPanel:**
- `IMosaicComponent` and `IAlarmFlashReceiver` interfaces (API compatibility)
- `Initialize()` and `UpdateData()` lifecycle
- Flash support for unacknowledged alarms
- Audio hooks (AlarmSound, AckSound)
- Acknowledge/Silence button support

**Validation:**
- Tiles illuminate correctly based on alarm conditions
- Color coding matches Heatup Visual standard (green/amber/red)
- Grid fills available space without overflow
- Tiles respond to alarm acknowledge (transition from bright to acknowledged state)
- No performance regression (tiles update only when alarm state changes, not every frame)
- Flash behavior works for unacknowledged alarm tiles

### Stage 3: Update OperatorScreenBuilder Alarm Section

**File Modified:** `OperatorScreenBuilder.cs`

**Change:** Update `CreateAlarmStripSection()` to create the tile grid infrastructure needed by the rewritten `MosaicAlarmPanel`:
- Add `GridLayoutGroup` component to the alarm container
- Set cell size, spacing, and constraint mode for consistent tile layout
- Pre-create tile GameObjects with `Image` + `TextMeshProUGUI` children
- Wire tile references to `MosaicAlarmPanel` for runtime binding

**Validation:**
- Menu item "Critical > Create Operator Screen" produces correct tile grid
- Tiles match visual style of Heatup Visual annunciators
- Builder does not break existing Screen 1 if re-run

### Stage 4: Changelog and Documentation

- Write `CHANGELOG_v4.2.2.md`
- Update `FUTURE_ENHANCEMENTS_ROADMAP.md` with completed items
- Update documentation headers in modified files

---

## Unaddressed Issues

### 1. Screens 2–8 Alarm Panels
Other operator screens may also need annunciator-style alarm panels. This is tracked under **v4.2.0+ Deferred Item 4.1.D4** (Screens 2–8 visual upgrade) in the Future Features roadmap. **Not addressed in v4.2.2** — Screen 1 establishes the standard, other screens will follow.

### 2. Alarm Condition Binding Completeness
Some alarm tiles (e.g., OVERPOWER ΔT, OVERTEMP ΔT) depend on physics models that may not yet provide the required data. These tiles will remain dark/inactive until the backing physics is implemented. **Not a bug — by design. Tiles gracefully degrade.**

### 3. Alarm Sound/Acknowledge System
The current acknowledge/silence/clear button system in `MosaicAlarmPanel` is basic. A more sophisticated alarm management system with first-out annunciator logic, reflash on new alarms, and priority queuing is deferred. **Planned for future release.**

### 4. Bottom Panel Blender Artwork Coverage
The Blender panel artwork (`ReactorOperatorScreenSkin`) renders a full-screen background. The bottom panel layout fix (Stage 1) will expose more of this artwork in the gap between the upper control sections and the lower alarm strip. If the artwork doesn't have detail in this region, it may look empty. **Cosmetic — will be addressed when the Blender artwork is updated for the revised layout.**

---

## References

- `OperatorScreenBuilder.cs` — `CreateBottomPanel()`, `CreateRodDisplaySection()`, `CreateAlarmStripSection()`
- `MosaicAlarmPanel.cs` — Current alarm display implementation
- `HeatupValidationVisual.Annunciators.cs` — GOLD STANDARD annunciator tile reference (visual standard)
- `HeatupValidationVisual.Styles.cs` — Color palette and tile layout constants
- `ReactorOperatorScreenSkin.cs` — Blender panel artwork system
- NRC HRTD Section 4 — Annunciator Window Tile conventions
- `Manuals/Section_4_Annunciator_Window_Tile.md` — Project annunciator reference
