# Changelog v4.2.2 — Bottom Panel Layout Fix & Annunciator-Style Alarm Panel

## Version: 4.2.2
## Date: 2026-02-11
## Scope: Reactor Operator Screen (Screen 1) Bottom Panel Visual Fixes

---

## Summary

Fixed two visual issues on the Reactor Operator Screen bottom panel: a layout overlap where the RodDisplaySection extended over the AlarmStrip and Blender panel artwork, and replaced the text-list alarm display with an authentic nuclear I&C annunciator tile grid matching the Heatup Validation Visual standard.

---

## Changes

### Bug Fix: RodDisplaySection Layout Overlap

**File:** `OperatorScreenBuilder.cs`

| | Before | After |
|---|--------|-------|
| RodDisplaySection anchorMin | `(0.16, 0.05)` | `(0.16, 0.55)` |
| RodDisplaySection anchorMax | `(0.45, 0.95)` | `(0.45, 0.95)` |

- **Root Cause:** `CreateRodDisplaySection()` anchored the "BANK POSITIONS" panel from `y: 0.05` to `y: 0.95`, spanning the full height of the bottom panel. This overlapped directly on top of the AlarmStrip (`y: 0.05 → 0.50`), causing the flat `COLOR_GAUGE_BG` panel to visually bleed over the alarm section and Blender panel artwork.
- **Fix:** Constrained RodDisplaySection to the upper row only (`y: 0.55 → 0.95`), aligning it with all other control sections (Rod Control, Boron, Trip, Time).
- **Impact:** Bank Positions panel no longer obscures the alarm strip. Blender artwork visible in the gap between upper controls and lower annunciator panel.

### Feature: Annunciator Tile Grid Alarm Panel

**File:** `MosaicAlarmPanel.cs` — **Complete rewrite**

Replaced the scrolling text-list alarm display with an annunciator tile grid following the visual standard established in `HeatupValidationVisual.Annunciators.cs`.

**Tile Visual Design:**
- Dark background when inactive (`0.12, 0.13, 0.16`)
- Green illuminated background for normal status indicators (`0.10, 0.35, 0.12`)
- Amber illuminated background for warnings (`0.45, 0.35, 0.00`)
- Red illuminated background for alarms (`0.50, 0.08, 0.08`)
- 1px border on all edges: active color when lit, dim (`0.20, 0.22, 0.26`) when off
- Multi-line centered labels using Electronic Highway Sign instrument font at 9pt
- Muted gray state for acknowledged alarms
- Flash support for unacknowledged alarm tiles (synced with MosaicBoard flash cycle)

**16 Annunciator Tiles (4×4 grid):**

| Index | Label | Type | Condition |
|-------|-------|------|-----------|
| 0 | REACTOR TRIPPED | Alarm (red) | `ReactorController.IsTripped` |
| 1 | NEUTRON POWER HI | Alarm (red) | `NeutronPower > 1.03` |
| 2 | STARTUP RATE HI | Alarm (red) | `StartupRate_DPM > 1.0` |
| 3 | ROD BOTTOM ALARM | Alarm (red) | `BankDPosition < 10 && NeutronPower > 0.05` |
| 4 | PRESS LOW | Alarm (red) | `Pressure < 2185 psia` |
| 5 | PRESS HIGH | Alarm (red) | `Pressure > 2285 psia` |
| 6 | T-AVG LOW | Alarm (red) | `Tavg < 547°F && NeutronPower > 0.02` |
| 7 | T-AVG HIGH | Alarm (red) | `Tavg > 567°F` |
| 8 | SUBCOOL LOW | Alarm (red) | `Subcooling < 20°F && Pressure > 400 psia` |
| 9 | PZR LVL LOW | Alarm (red) | `PzrLevel < 17%` |
| 10 | PZR LVL HIGH | Warning (amber) | `PzrLevel > 92%` |
| 11 | OVERPOWER ΔT | Alarm (red) | `IsTripped && NeutronPower > 1.09` (simplified) |
| 12 | REACTOR CRITICAL | Status (green) | `ReactorController.IsCritical` |
| 13 | AUTO ROD CONTROL | Status (green) | Placeholder (always off) |
| 14 | PZR HTRS ON | Status (green) | `Pressure > 500 psia` (proxy) |
| 15 | LOW FLOW | Alarm (red) | `FlowFraction < 0.90` |

**API Compatibility:**
- Retains `IMosaicComponent` and `IAlarmFlashReceiver` interface implementations
- `Initialize()` / `UpdateData()` lifecycle unchanged
- Acknowledge/Silence button support preserved
- Audio hooks (AlarmSound, AckSound) preserved

**Architecture:**
- `TileDescriptor` struct defines label, alarm/warning/status classification
- `TileUI` class holds references to root GameObject, background Image, 4 border Images, and TextMeshProUGUI label
- `EvaluateTileConditions()` maps ReactorController state to boolean array each update cycle
- `UpdateSingleTileVisual()` applies colors based on active/inactive/acknowledged/flashing state
- `GridLayoutGroup` handles responsive tile arrangement with deferred cell size calculation
- Tiles that cannot be evaluated (missing physics backing) remain dark — no false alarms

### Builder Update: Annunciator Panel Construction

**File:** `OperatorScreenBuilder.cs`

- `CreateAlarmStripSection()` updated to create annunciator infrastructure
- Section label changed from "ALARMS" to "ANNUNCIATOR PANEL"
- Container anchors adjusted for better tile grid utilization (`0.02 → 0.83` vertical)
- `MosaicAlarmPanel.GridColumns`, `TileSpacing`, and `TileContainer` pre-wired
- File header updated with v4.2.2 change note

---

### Blender Panel Artwork Update

**File:** `Assets/Scripts/Blender/create_reactor_panel.py`

- **Bank Positions frame:** Fixed from full-height `(0.16, 0.05)→(0.45, 0.95)` to upper-row only `(0.16, 0.55)→(0.45, 0.95)`, matching the Unity anchor fix. Bar graph recesses adjusted for shorter frame height.
- **Alarm Strip → Annunciator Panel:** Replaced the split alarm strip (partial-width frame under Boron/Trip/Time only + separate Rod Control lower frame) with a single full-width annunciator frame `(0.01, 0.05)→(0.99, 0.50)` spanning under all upper control sections.
- **Annunciator tile recesses:** Added 4×4 grid of recessed display areas within the annunciator frame, with engraved text labels matching the 16 tile definitions. Tile recesses use the `Recess_Display` material (semi-transparent in render) so Unity's dynamic tile illumination shows through.
- **Label:** Section label changed from "ALARMS" to "ANNUNCIATOR PANEL".

**Action Required:** Re-run `create_reactor_panel.py` in Blender 5.0, then re-run `render_panel_textures.py` to generate updated `panel_base_color.png`.

---

## Files Modified

| File | Change Type |
|------|------------|
| `Assets/Scripts/UI/OperatorScreenBuilder.cs` | Bug fix (anchor) + builder update |
| `Assets/Scripts/UI/MosaicAlarmPanel.cs` | Complete rewrite (text list → tile grid) |
| `Assets/Scripts/Blender/create_reactor_panel.py` | Layout fix + annunciator tile grid |

---

## Validation Criteria

- [ ] RodDisplaySection ("BANK POSITIONS") no longer overlaps the alarm strip
- [ ] Blender panel artwork visible between upper controls and annunciator panel
- [ ] Annunciator tiles render in 4×4 grid within the alarm strip area
- [ ] Tiles illuminate with correct colors (green/amber/red) when conditions are met
- [ ] Tiles are dark with dim text/borders when conditions are not met
- [ ] Unacknowledged alarm tiles flash in sync with MosaicBoard alarm flash cycle
- [ ] Acknowledged alarm tiles show muted gray state (no flash)
- [ ] Tiles that lack backing physics (AUTO ROD CONTROL) remain dark
- [ ] "Critical > Create Operator Screen" menu item produces correct layout
- [ ] No compilation errors

---

## References

- Implementation Plan: `IMPLEMENTATION_PLAN_v4.2.2.md`
- Visual Standard: `HeatupValidationVisual.Annunciators.cs` (GOLD STANDARD)
- Color Palette: `HeatupValidationVisual.Styles.cs` — annunciator color constants
- Annunciator Reference: `Manuals/Section_4_Annunciator_Window_Tile.md`
