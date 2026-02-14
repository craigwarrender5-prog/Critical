# Changelog v5.2.0 — Add CRITICAL Tab (At-a-Glance Validation Overview)

**Date:** 2026-02-12  
**Version:** 5.2.0  
**Type:** MINOR — UI Enhancement (Additive Only)  
**Implementation Plan:** IMPLEMENTATION_PLAN_v5.2.0.md  

---

## Summary

Adds a new **CRITICAL** tab (Tab 8, Ctrl+8) to the Heatup Validation Dashboard that displays the five most important subsystem summaries on a single screen without scrolling. This allows an operator or developer to confirm plant status at a glance without switching between multiple tabs.

No physics modules were modified. No GOLD standard files were altered beyond the additive changes to `HeatupValidationVisual.cs` (Core), which remains GOLD.

---

## Changes

### Stage 1 — Core Infrastructure: Tab Registration

#### `HeatupValidationVisual.cs` (Core — GOLD)

##### Added
- **`"CRITICAL"` tab label** at index 7 in `_dashboardTabLabels` array
- **`partial void DrawCriticalTab(Rect area)`** declaration alongside existing tab partials
- **`case 7: DrawCriticalTab(contentArea)`** in OnGUI switch dispatch
- **`Ctrl+8` keyboard shortcut** mapping `digit8Key` → `_dashboardTab = 7`
- **Version comment** `v5.2.0` in file header

##### Changed
- **Header hint text** from `[Ctrl+1-7]` to `[Ctrl+1-8]`
- **Tab bar ASCII diagram** in header comment updated to include CRIT
- **Companion partial list** in header comment updated to include TabCritical.cs

##### Not Changed
- All existing tab indices (0–6) remain identical
- All existing keyboard shortcuts (Ctrl+1–7) unchanged
- No rendering logic modified
- No layout constants modified

---

### Stage 2 — Tab Rendering: 5-Block Grid Layout

#### `HeatupValidationVisual.TabCritical.cs` (NEW FILE)

New partial class file implementing the CRITICAL tab with a 2-row, 5-block grid layout.

##### Layout
- **Top row (55% height):** 3 equal-width blocks — RCS, PZR, SG
- **Bottom row (45% height):** 2 equal-width blocks — CVCS, VCT
- No scrolling required at 1920×1080 or higher resolutions

##### RCS PRIMARY Block
| Parameter | Source Field | Display |
|-----------|-------------|---------|
| T_avg | `engine.T_avg` | Big readout, °F, green/amber/red at 545/570°F |
| RCS Pressure | `engine.pressure` | Big readout, psia and psig |
| T_hot | `engine.T_hot` | Small row, °F, orange-red trace color |
| T_cold | `engine.T_cold` | Small row, °F, blue trace color |
| Pressure Rate | `engine.pressureRate` | Small row, psi/hr, warn >100, alarm >200 |
| Heat In | `effectiveRCPHeat + pzrHeaterPower/1000` | Small row, MW |
| Heat To SG | `engine.sgHeatTransfer_MW` | Small row, MW |

##### PRESSURIZER Block
| Parameter | Source Field | Display |
|-----------|-------------|---------|
| PZR Pressure | `engine.pressure` | Big readout, psia |
| PZR Level | `engine.pzrLevel` | Big readout, %, warn/alarm on setpoint deviation |
| PZR Temp | `engine.T_pzr` | Small row, °F, amber trace color |
| Heater Power | `engine.pzrHeaterPower` | Small row, kW |
| Spray | `engine.sprayActive` | Indicator, ON/OFF |
| Bubble State | `solidPressurizer`/`bubbleFormed` | SOLID/FORMING/NORMAL with color |

##### STEAM GENERATOR Block
| Parameter | Source Field | Display |
|-----------|-------------|---------|
| SG Pressure | `engine.sgSecondaryPressure_psia` | Big readout, psia |
| Primary–Secondary ΔT | `T_rcs - T_sg_secondary` | Big readout, °F |
| T_sat (SG) | `engine.sgSaturationTemp_F` | Small row, °F |
| SG Bulk Temp | `engine.T_sg_secondary` | Small row, °F |
| Steam Dump | `engine.steamDumpActive` | Indicator, ACTIVE/OFF |
| Boiling? | `engine.sgBoilingActive` | Indicator, YES/NO, alarm if T_rcs < 350°F |

##### CVCS Block
| Parameter | Source Field | Display |
|-----------|-------------|---------|
| Charging Flow | `engine.chargingFlow` | Big readout, gpm |
| Letdown Flow | `engine.letdownFlow` | Big readout, gpm |
| Net Flow | `chargingFlow - letdownFlow` | Big readout, gpm, signed, warn >10, alarm >20 |
| System Inventory | `engine.totalSystemInventory_gal` | Small row, gal |
| Mass Conservation Error | `engine.massConservationError` | Small row, gal, warn >50, alarm >200 |
| Inventory Error | `engine.systemInventoryError_gal` | Small row, gal, warn >100, alarm >200 |

##### VCT Block
| Parameter | Source Field | Display |
|-----------|-------------|---------|
| VCT Level | `engine.vctState.Level_percent` | Big readout, %, color vs normal band |
| Normal Band | `PlantConstants.VCT_LEVEL_NORMAL_LOW/HIGH` | Reference row |
| Makeup | `engine.vctMakeupActive` | Indicator, ACTIVE/OFF |
| Divert | `engine.vctDivertActive` | Indicator, ACTIVE/OFF |
| RWST Suction | `engine.vctRWSTSuction` | Indicator, YES/NO, red if active |
| VCT Alarms | `vctLevelLow`/`vctLevelHigh` | Conditional alarm rows or "NONE" |

##### Rendering Approach
- Large readouts use existing `_gaugeValueStyle` (24pt font)
- Secondary rows use existing `_statusLabelStyle` / `_statusValueStyle` (14pt)
- All colors from existing cached palette — no new `Color` objects per frame
- Helper methods: `DrawCriticalBigRow()`, `DrawCriticalSmallRow()`, `DrawCriticalIndicator()`
- Block backgrounds use existing `_gaugeBgStyle`
- Section headers use existing `DrawSectionHeader()`

##### Threshold Constants (placeholder, TODO-marked)
- `CRIT_PRESS_RATE_WARN/ALARM` — 100 / 200 psi/hr
- `CRIT_PZR_LEVEL_DEV_WARN/ALARM` — 10 / 15 % deviation from setpoint
- `CRIT_NET_FLOW_WARN/ALARM` — 10 / 20 gpm absolute net flow
- `CRIT_SG_EARLY_BOIL_TEMP` — 350 °F (boiling below this is unexpected)

---

## Files Modified

| File | Action | GOLD? |
|------|--------|-------|
| `Assets/Scripts/Validation/HeatupValidationVisual.cs` | Modified (additive) | Yes |
| `Assets/Scripts/Validation/HeatupValidationVisual.TabCritical.cs` | Created | No |

---

## Deferred Items

| Item | Reason | Tracking |
|------|--------|----------|
| Mini sparklines (T_avg, Pressure, PZR Level, SG Pressure) | Rendering complexity; history buffers exist | Planned v5.2.1 |
| Configurable thresholds | Hardcoded with TODO markers; config asset needed | Future enhancement |

---

## Validation Criteria

- [x] RCS temp/pressure, PZR pressure/level, CVCS flows/net, VCT level, SG pressure/temp visible on ONE screen
- [x] Switching tabs does not break existing screens (existing indices unchanged)
- [x] Missing values show "—" pattern (no crashes; one-shot warning only)
- [x] No continuous log spam from missing bindings
- [x] No new Color objects created per-frame (uses only cached palette)
- [x] Existing tab indices 0–6 unchanged
- [x] Ctrl+1–7 shortcuts unchanged; Ctrl+8 added for CRITICAL
