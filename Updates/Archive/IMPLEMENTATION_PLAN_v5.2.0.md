# Implementation Plan v5.2.0 — Add CRITICAL Tab (At-a-Glance Validation Overview)

**Date:** 2026-02-12  
**Version:** 5.2.0  
**Type:** MINOR — UI Enhancement (Additive Only)  
**Affects:** `HeatupValidationVisual` partial class (UI only — no physics changes)

---

## Problem Summary

The current dashboard has 7 system-specific tabs (Overview, PZR, CVCS, SG/RHR, RCP, LOG, VALID), each providing deep detail on a single subsystem. However, there is no single-screen view that answers the five critical questions simultaneously:

1. Is the RCS heating correctly and is pressure behaving?
2. Is the PZR stable (pressure/level/bubble/heaters/spray)?
3. Is CVCS moving mass (charging/letdown/net) and is inventory stable?
4. Is the VCT stable (level/flows)?
5. Is the SG behaving (pressure/temp/boiling state/ΔT)?

An operator or developer must currently switch between 3–4 tabs to confirm all five. This adds cognitive overhead during critical heatup validation.

---

## Expectations (Correct Behavior)

A new **CRITICAL** tab (Tab 8, index 7) should display 5 system blocks on a single screen with large, readable numeric readouts and minimal color-coded status indicators. No scrolling required at typical monitor resolutions (1920×1080+).

All data comes from existing `HeatupSimEngine` public fields — no new physics, no new logging, no new data sources. Missing values display as "—" with a single one-shot warning log.

---

## Proposed Fix — 3-Stage Implementation

### Stage 1 — Core Infrastructure: New Tab Registration + Skeleton Layout

**Files modified:**
- `HeatupValidationVisual.cs` (Core) — Add tab label, partial declaration, keyboard shortcut, switch case

**Changes:**
1. Add `"CRITICAL"` to `_dashboardTabLabels` array (position 7, index 7)
2. Add `partial void DrawCriticalTab(Rect area);` declaration
3. Add `case 7: DrawCriticalTab(contentArea); break;` to OnGUI switch
4. Update keyboard shortcut block: `Ctrl+8` for the new tab
5. Update header hint to show `[Ctrl+1-8]`

**No existing tab indices change.** The existing VALID tab remains at index 6. The new CRITICAL tab appends at index 7.

---

### Stage 2 — Tab Rendering: 5-Block Grid Layout with All Parameters

**Files created:**
- `HeatupValidationVisual.TabCritical.cs` — New partial class file

**Layout (2-row grid, no scrolling):**
```
┌─────────────────┬──────────────────┬──────────────────┐
│  RCS PRIMARY     │  PRESSURIZER     │  STEAM GENERATOR  │
│  T_avg, T_hot,  │  Pressure, Temp  │  Pressure, T_sat  │
│  T_cold, P_rcs  │  Level, Heaters  │  T_bulk, ΔT, Boil │
│  Press Rate     │  Spray, Bubble   │  Steam Dump       │
│  Heat In, Q_SG  │                  │                    │
├─────────────────┴─────────┬────────┴──────────────────┤
│  CVCS (Flows & Inventory)  │  VCT (Level & Status)      │
│  Charging, Letdown, Net   │  VCT Level, Makeup,        │
│  Total Mass, ΔMass        │  RWST Suction, Divert      │
│  Conservation Error       │  Annunciator Flags          │
└────────────────────────────┴────────────────────────────┘
```

**Displayed parameters per block (reading from existing engine fields):**

#### RCS Block
| Parameter | Engine Field | Unit |
|-----------|-------------|------|
| RCS Avg Temp | `T_avg` | °F |
| Hot Leg Temp | `T_hot` | °F |
| Cold Leg Temp | `T_cold` | °F |
| RCS Pressure (psia) | `pressure` | psia |
| RCS Pressure (psig) | `pressure - 14.696` | psig |
| Pressure Rate | `pressureRate` | psi/hr |
| Total Heat In | `effectiveRCPHeat + pzrHeaterPower/1000` | MW |
| Heat To SG | `sgHeatTransfer_MW` | MW |

#### PZR Block
| Parameter | Engine Field | Unit |
|-----------|-------------|------|
| PZR Pressure | `pressure` | psia |
| PZR Temperature | `T_pzr` | °F |
| PZR Level | `pzrLevel` | % |
| Heater Power | `pzrHeaterPower` | kW |
| Spray Active | `sprayActive` | ON/OFF |
| Bubble State | `solidPressurizer` / `bubbleFormed` | SOLID/FORMING/NORMAL |

#### CVCS Block
| Parameter | Engine Field | Unit |
|-----------|-------------|------|
| Charging Flow | `chargingFlow` | gpm |
| Letdown Flow | `letdownFlow` | gpm |
| Net Flow | `chargingFlow - letdownFlow` | gpm |
| Total System Inventory | `totalSystemInventory_gal` | gal |
| ΔMass (conservation) | `massConservationError` | gal |
| Inventory Error | `systemInventoryError_gal` | gal |

#### VCT Block
| Parameter | Engine Field | Unit |
|-----------|-------------|------|
| VCT Level | `vctState.Level_percent` | % |
| VCT Makeup Active | `vctMakeupActive` | YES/NO |
| VCT Divert Active | `vctDivertActive` | YES/NO |
| RWST Suction | `vctRWSTSuction` | YES/NO |
| VCT Level Low | `vctLevelLow` | alarm flag |
| VCT Level High | `vctLevelHigh` | alarm flag |

#### SG Block
| Parameter | Engine Field | Unit |
|-----------|-------------|------|
| SG Pressure | `sgSecondaryPressure_psia` | psia |
| Saturation Temp | `sgSaturationTemp_F` | °F |
| SG Bulk Temp | `T_sg_secondary` | °F |
| Primary–Secondary ΔT | `T_rcs - T_sg_secondary` | °F |
| Steam Dump Active | `steamDumpActive` | ON/OFF |
| Boiling? | `sgBoilingActive` | YES/NO |

**Rendering approach:**
- Each block uses `DrawSectionHeader()` for the block title
- Large numeric readouts use `_gaugeValueStyle` (24pt font) for top 2–3 values per block
- Secondary values use `_statusValueStyle` (existing 14pt)
- All colors from existing cached palette only — no new `Color` objects
- Helper method `DrawCriticalBigValue()` for the large readout rows
- Helper method `DrawCriticalRow()` for normal-sized rows (wraps `DrawStatusRow` with block-local coordinates)

**Status color coding (placeholder thresholds with TODO markers):**
- RCS pressure rate > 100 psi/hr → warning; > 200 → alarm
- PZR level deviation from setpoint > 10% → warning; > 15% → alarm
- CVCS net flow |net| > 10 gpm → warning; > 20 gpm → alarm
- VCT level outside normal band → warning/alarm (uses existing `PlantConstants.VCT_LEVEL_NORMAL_LOW/HIGH`)
- SG boiling when `T_rcs < 350°F` → warning (boiling too early)

---

### Stage 3 — Changelog & Documentation

**Files created:**
- `Critical\Updates\Changelogs\CHANGELOG_v5.2.0.md`

No physics changes, no GOLD module modifications, no future feature items.

---

## Unaddressed Issues

1. **Mini sparklines** — The spec requests optional sparklines for RCS Avg Temp, RCS Pressure, PZR Level, and SG Pressure. History buffers exist (`tempHistory`, `pressHistory`, `pzrLevelHistory`), but adding inline sparklines to the CRITICAL tab introduces rendering complexity. **Planned for v5.2.1** as an enhancement once the base tab is validated. Will be added to Future_Features.

2. **Tab persistence (remember last selected)** — The `_dashboardTab` field already persists across F1 toggles (it's a class field, not reset). No change needed.

3. **Configurable thresholds** — Threshold constants are hardcoded with TODO markers. A future enhancement could read them from a configuration asset. Not in scope for v5.2.0.

---

## Validation Criteria (Self-Check)

- [ ] RCS temp/pressure, PZR pressure/level, CVCS flows/net, VCT level, SG pressure/temp visible on ONE screen
- [ ] Switching tabs does not break existing screens
- [ ] Missing values appear as "—" (no exceptions, no crashes)
- [ ] No continuous log spam if a binding is missing
- [ ] No new `Color` objects created per-frame (memory safety)
- [ ] Existing tab indices unchanged (VALID still at 6)
- [ ] Ctrl+1–7 shortcuts unchanged, Ctrl+8 added for CRITICAL
