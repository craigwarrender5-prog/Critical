# IP-0040: Validation Dashboard Visual Overhaul

**Date:** 2026-02-17  
**Domain Plan:** DP-0008 (Operator Interface & Scenarios)  
**Parent IP:** IP-0031 (Validation Dashboard Visual Redesign)  
**Status:** FAILED — Closed as superseded by later dashboard recovery plans  
**Closed Date:** 2026-02-17  
**Superseded By:** IP-0041, IP-0042, IP-0043  
**Priority:** High  
**Changelog Required:** Yes  

---

## 1. Executive Summary

IP-0031 delivered the structural foundation for a professional validation dashboard — canvas hierarchy, tab navigation, data binding, and a full library of custom gauge components (ArcGauge, BidirectionalGauge, LinearGauge, DigitalReadout, StatusIndicator, MiniTrendStrip). However, the **visual execution failed catastrophically**. Every Overview section was implemented using bare `ParameterRow` text rows (tiny label + value pairs) instead of the gauge components that were purpose-built for exactly this use. The result is a dark screen with barely-readable scattered text — not the professional, animated, instrument-rich dashboard that was specified.

This IP corrects that failure by:

1. **Gutting the text-row Overview sections** and rebuilding them with real ArcGauges, BidirectionalGauges, DigitalReadouts, and StatusIndicators
2. **Replacing the minimal AlarmTile** with proper annunciator tiles modeled on the proven `MosaicAlarmPanel` style (4-edge borders, dim/lit states, instrument font, alarm flash support)
3. **Activating the gauge glow/animation pipeline** that exists in the components but was never triggered
4. **Restructuring the Overview layout** to give sections appropriate proportions for housing actual instruments
5. **Polishing the detail tab panels** (Pressurizer, CVCS, Primary, SG/RHR) with gauge integration
6. **Verifying the MiniTrends panel** renders actual sparkline traces with proper scaling

No physics code, engine code, or GOLD modules are touched. This is purely a UI visual overhaul.

---

## 2. Problem Summary

### 2.1 Root Cause

The IP-0031 implementation was executed in stages, with Stage 2 building excellent gauge components and Stages 3-4 building the panels that should have *used* those gauges. However, Stages 3-4 took a shortcut: every `OverviewSection_*` class calls `CreateRow()` and `CreateStatusRow()` — helper methods on `OverviewSectionBase` that produce simple `ParameterRow` (label text + value text) and `StatusRow` (label text + tiny colored rectangle) elements. Not a single ArcGauge, BidirectionalGauge, or LinearGauge is instantiated anywhere on the Overview panel.

### 2.2 Specific Visual Failures

| Area | IP-0031 Specification | Actual Delivery |
|------|----------------------|-----------------|
| Overview sections | Arc gauges with glowing arcs, animated needles, digital readouts | Plain text rows: `"PRESSURE"` ... `"470 psia"` |
| Alarm summary | Annunciator tiles with borders, flash/acknowledge, instrument font | `AlarmTile`: flat colored rectangle with 9px text, no borders |
| Pressurizer section | ArcGauge for level, ArcGauge for pressure, BiGauge for surge | 2 `ParameterRow` text rows + 2 `StatusRow` indicators |
| CVCS section | ArcGauges for charging/letdown, BiGauge for net flow | 2 `ParameterRow` text rows |
| Section layout | Responsive grid with room for instruments | `HorizontalLayoutGroup` cramming 4 equal-width text columns |
| Mini-trends | Sparkline traces with value readouts | Components exist and render properly — **this is working** |
| Glow effects | Shader-driven glow on gauges and indicators | No glow shader created; `GlowImage` references are null |
| Tab transitions | 250ms EaseOutCubic panel slide | Instant show/hide with no animation |

### 2.3 What Works

- Canvas hierarchy, F1 toggle, additive scene loading (IP-0039 fixed startup)
- Tab navigation system (clicks work, correct panel shown)
- Header panel (mode, phase, sim time, speed indicator)
- All gauge *components* — ArcGauge, BidirectionalGauge, LinearGauge, DigitalReadout, StatusIndicator — are correctly coded with `OnPopulateMesh` rendering, `SmoothDamp` animation, and factory `Create()` methods. They just aren't used.
- MiniTrendStrip and TrendBuffer: functional ring buffer + line rendering
- AlarmFlashEffect component: exists, functional
- Data binding pipeline: `OnUpdateData()` → `OnUpdateVisuals()` at 10Hz/30Hz

---

## 3. Design Goals

### 3.1 Visual Target

The dashboard should look like a **modern nuclear plant Process Computer display** — dark background, glowing arc gauges, professional instrument-grade typography, annunciator tiles that light up authentically, and real-time sparkline trends. The reference points are:

- Westinghouse common Q ICCMS/RVLIS displays
- The project's own `MosaicAlarmPanel` annunciator tiles (proven, professional design)
- Commercial flight simulator glass cockpit instruments
- Modern SCADA/DCS monitoring displays

### 3.2 Key Visual Principles

1. **Instruments, not text** — Every primary parameter gets a visual gauge, not a text row
2. **Annunciator authenticity** — Alarm tiles match the proven MosaicAlarmPanel: 4-edge borders, instrument font, proper dim/lit/flash states
3. **Visual hierarchy** — Large gauges for critical parameters (pressure, level, Tavg), smaller readouts for secondary ones
4. **Breathing room** — Sections sized to accommodate their instruments rather than cramming everything into equal columns
5. **Color discipline** — Green/amber/red threshold coloring drives attention to what matters
6. **Analog + digital pairing** — Every arc gauge has a digital readout beneath it, matching real nuclear instrument practice (analog for trend/rate-of-change, digital for precision)
7. **Instrument typography** — All numeric readouts use the "Electronic Highway Sign" segment font with TMP glow materials, displayed in recessed dark windows — matching the backlit LED look of real plant instrumentation

---

## 4. Requirements

### 4.1 Overview Panel Requirements

| ID | Requirement | Gauge Type |
|----|-------------|-----------|
| OV-01 | RCS Pressure displayed as arc gauge with animated needle | ArcGauge |
| OV-02 | PZR Level displayed as arc gauge | ArcGauge |
| OV-03 | Tavg displayed as arc gauge | ArcGauge |
| OV-04 | Subcooling displayed as arc gauge | ArcGauge |
| OV-05 | Surge flow displayed as bidirectional gauge | BidirectionalGauge |
| OV-06 | Net CVCS flow displayed as bidirectional gauge | BidirectionalGauge |
| OV-07 | Charging flow displayed as digital readout | DigitalReadout |
| OV-08 | Letdown flow displayed as digital readout | DigitalReadout |
| OV-09 | SG pressure displayed as digital readout | DigitalReadout |
| OV-10 | Net plant heat displayed as digital readout (color-coded sign) | DigitalReadout |
| OV-11 | RCP status displayed as 4× status indicators | StatusIndicator |
| OV-12 | Heater/spray status as status indicators | StatusIndicator |
| OV-13 | Bubble state as status indicator | StatusIndicator |
| OV-14 | Alarm summary as MosaicAlarmPanel-style annunciator grid | Annunciator Tile |
| OV-15 | Mass conservation error as digital readout with alarm coloring | DigitalReadout |

### 4.2 Annunciator Requirements (MosaicAlarmPanel Pattern)

| ID | Requirement |
|----|-------------|
| AN-01 | Each tile has 4 discrete 1px border edges (top, bottom, left, right Image components) |
| AN-02 | Inactive tiles: dark background (`0.12, 0.13, 0.16`), dim text, dim borders |
| AN-03 | Active alarm tiles: red background (`0.50, 0.08, 0.08`), bright red text/borders |
| AN-04 | Active warning tiles: amber background (`0.45, 0.35, 0.00`), bright amber text/borders |
| AN-05 | Active status tiles: green background (`0.10, 0.35, 0.12`), bright green text/borders |
| AN-06 | Instrument font ("Electronic Highway Sign SDF") for tile labels |
| AN-07 | Multi-line centered labels matching nuclear I&C conventions |
| AN-08 | Full ISA-18.1 annunciator sequence: INACTIVE → ALERTING (3 Hz) → ACKNOWLEDGED (steady) → CLEARING (0.7 Hz) → INACTIVE |
| AN-09 | GridLayoutGroup arrangement with auto-sizing cells |
| AN-10 | ACK button acknowledges all alerting tiles (ALERTING → ACKNOWLEDGED) |
| AN-11 | Status tiles (green) are simple on/off — no flash state machine |
| AN-12 | Self-clearing alarms transition ALERTING → CLEARING (slow flash, not instant off) |

### 4.3 Layout Requirements

| ID | Requirement |
|----|-------------|
| LY-01 | Overview top row: 3 sections (Reactor/RCS, Pressurizer, CVCS/SG) with proportional widths |
| LY-02 | Overview bottom row: 2 sections (Status/Health, Alarm Annunciator grid) |
| LY-03 | Each section has enough height/width to display gauges at readable size (≥80px arc diameter) |
| LY-04 | Section backgrounds use `BackgroundSection` color with 4px internal padding |
| LY-05 | Section headers: 11px bold, centered, `TextSecondary` color |

### 4.4 Detail Panel Requirements

| ID | Requirement |
|----|-------------|
| DP-01 | Pressurizer tab: ArcGauge for level, ArcGauge for pressure, DigitalReadout for temperatures |
| DP-02 | CVCS tab: ArcGauges for charging/letdown, BidirectionalGauge for net flow, VCT level LinearGauge |
| DP-03 | Primary tab: DigitalReadouts for loop temperatures, StatusIndicators for RCP states |
| DP-04 | SG/RHR tab: DigitalReadouts for SG parameters, StatusIndicators for system states |

### 4.5 Instrument Readout Requirements

| ID | Requirement |
|----|-------------|
| IR-01 | All gauge value readouts use "Electronic Highway Sign SDF" instrument font |
| IR-02 | Normal state: `Instrument_Green_Glow` TMP material (green backlit LED look) |
| IR-03 | Warning state: `Instrument_Amber_Glow` TMP material |
| IR-04 | Alarm state: `Instrument_Red_Glow` TMP material |
| IR-05 | Material swap only on state change (not per-frame) — same pattern as MosaicGauge |
| IR-06 | Each ArcGauge has a digital readout positioned below the arc (paired analog+digital) |
| IR-07 | Each digital readout has a recessed dark backing rectangle (`BackgroundGraph` color) |
| IR-08 | Standalone DigitalReadout components also use instrument font + glow materials |
| IR-09 | Graceful fallback to default TMP font if instrument font not found at runtime |

### 4.6 Animation Requirements

| ID | Requirement |
|----|-------------|
| AM-01 | ArcGauge needles use SmoothDamp at 100ms (already coded — just needs activation) |
| AM-02 | DigitalReadout color transitions at 5f speed (already coded — needs activation) |
| AM-03 | Annunciator tiles use ISA-18.1 flash rates: 3 Hz alerting, 0.7 Hz clearing, steady when acknowledged |
| AM-04 | Tab panel transitions: 200ms alpha fade (CanvasGroup.alpha lerp) |
| AM-05 | TMP glow power pulses on alarm state (flash between 0.15 and 0.6) — same as MosaicGauge |

---

## 5. Architecture

### 5.1 Files Modified (Existing)

| File | Changes |
|------|---------|
| `OverviewPanel.cs` | Complete rewrite of `BuildLayout()` — new 3+2 section grid, proportional sizing |
| `OverviewSection_GlobalHealth.cs` | Replace ParameterRows with DigitalReadout for mass error & net heat |
| `OverviewSection_ReactorCore.cs` | Replace ParameterRows with ArcGauge for Tavg, DigitalReadouts for Thot/Tcold |
| `OverviewSection_RCS.cs` | Replace ParameterRows with ArcGauge for pressure, ArcGauge for subcooling |
| `OverviewSection_Pressurizer.cs` | Replace ParameterRows with ArcGauge for level, StatusIndicators for bubble/heaters |
| `OverviewSection_CVCS.cs` | Replace ParameterRows with DigitalReadouts for charging/letdown, BiGauge for net |
| `OverviewSection_SGRHR.cs` | Replace ParameterRows with DigitalReadouts for SG pressure, heat transfer |
| `OverviewSection_Alarms.cs` | Complete rewrite — replace AlarmTile with MosaicAlarmPanel-style annunciator tiles |
| `PressurizerPanel.cs` | Add ArcGauges for primary parameters alongside existing DigitalReadouts |
| `CVCSPanel.cs` | Add ArcGauges and BidirectionalGauge |
| `ValidationDashboardBuilder.cs` | Adjust MainContent area proportions for new layout |
| `ValidationDashboardTheme.cs` | Add annunciator color constants from MosaicAlarmPanel palette |
| `TabNavigationController.cs` | Add alpha fade transition on panel switch |

### 5.2 Files Created (New)

| File | Purpose |
|------|---------|
| `DashboardAnnunciatorTile.cs` | Annunciator tile component adapted from MosaicAlarmPanel pattern for HeatupSimEngine data |
| `InstrumentFontHelper.cs` | Shared utility for loading instrument font + TMP glow materials and applying to readouts |

### 5.3 Files Unchanged

| File | Reason |
|------|--------|
| `ArcGauge.cs` | MODIFIED — `Create()` factory updated: instrument font, glow material, recessed backing |
| `BidirectionalGauge.cs` | MODIFIED — `Create()` factory updated: instrument font, glow material, recessed backing |
| `LinearGauge.cs` | Already correct — horizontal/vertical bar with gradient fill |
| `DigitalReadout.cs` | MODIFIED — `Create()` factory updated: instrument font, glow material, recessed backing |
| `StatusIndicator.cs` | Already correct — pill/circle/square with pulse animation |
| `MiniTrendStrip.cs` | Already correct — sparkline line rendering |
| `MiniTrendsPanel.cs` | Already correct — wires trends to engine data |
| `TrendBuffer.cs` | Already correct — ring buffer with auto-scaling |
| `AlarmFlashEffect.cs` | Already correct — flash component |
| `HeaderPanel.cs` | Working correctly |
| `ValidationDashboardController.cs` | Working correctly |
| `GaugeAnimator.cs` | Available for additional animation needs |

### 5.4 Overview Panel New Layout

```
┌────────────────────────────────────────────────────────────────────────────┐
│  TOP ROW (60% height)                                                       │
│ ┌──────────────────────┐┌──────────────────────┐┌──────────────────────┐   │
│ │   REACTOR / RCS      ││    PRESSURIZER       ││     CVCS / SG        │   │
│ │  (flex 1.2)          ││  (flex 1.0)          ││  (flex 1.0)          │   │
│ │                      ││                      ││                      │   │
│ │  [ArcGauge]  [Arc]   ││  [ArcGauge]          ││  [Digital] [Digital] │   │
│ │  RCS Press   Tavg    ││  PZR Level           ││  Charging  Letdown   │   │
│ │                      ││                      ││                      │   │
│ │  [ArcGauge]  [Dig]   ││  [Status] [Status]   ││  [BiGauge]           │   │
│ │  Subcool    ΔT       ││  BUBBLE   HEATERS    ││  Net CVCS            │   │
│ │                      ││                      ││                      │   │
│ │  [Stat][Stat][St][St]││  [Digital] [Digital]  ││  [Digital] [Digital] │   │
│ │  RCP1 RCP2  RCP3 RCP4││  PZR Temp  Surge     ││  SG Press  Net Heat │   │
│ └──────────────────────┘└──────────────────────┘└──────────────────────┘   │
│                                                                             │
│  BOTTOM ROW (40% height)                                                    │
│ ┌──────────────────────────┐┌──────────────────────────────────────────┐   │
│ │   SYSTEM HEALTH          ││        ALARM ANNUNCIATOR GRID            │   │
│ │  (flex 0.8)              ││  (flex 1.5)                              │   │
│ │                          ││  ┌────┬────┬────┬────┬────┬────┐        │   │
│ │  [Digital] Mass Error    ││  │PRESS│PRESS│LVL │LVL │SUB- │ VCT │   │   │
│ │  [Digital] Energy Error  ││  │HIGH │LOW  │HIGH│LOW │COOL │     │   │   │
│ │  [Status]  Mass Cons.    ││  ├────┼────┼────┼────┼────┼────┤        │   │
│ │  [Status]  SG Boiling    ││  │MASS│FLOW │SG P│HTRS│SPRAY│BBLE│   │   │
│ │  [Status]  RHR Active    ││  │CONS│LOW  │HIGH│ ON │ ON  │FORM│   │   │
│ │                          ││  └────┴────┴────┴────┴────┴────┘        │   │
│ └──────────────────────────┘└──────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 6. Annunciator Tile Design

### 6.1 Adopted from MosaicAlarmPanel

The existing `AlarmTile` class is a flat colored rectangle with no borders, no instrument font, and minimal visual character. It will be replaced with `DashboardAnnunciatorTile`, which directly adopts the proven visual pattern from `MosaicAlarmPanel`:

```
Inactive Tile:              Active Alarm Tile:
┌─────────────┐            ┌─────────────┐
│ ░░░░░░░░░░░ │            │ ▓▓▓PRESS▓▓▓ │  ← red bg
│ ░░ PRESS ░░ │  ← dim     │ ▓▓▓ LOW ▓▓▓ │  ← bright red text
│ ░░  LOW  ░░ │    gray    │ ▓▓▓▓▓▓▓▓▓▓▓ │  ← red borders
│ ░░░░░░░░░░░ │            │             │
└─────────────┘            └─────────────┘
  dim borders                red borders
```

### 6.2 Color Palette (from MosaicAlarmPanel)

```csharp
// Tile background states
COLOR_ANN_OFF     = (0.12, 0.13, 0.16)   // Dark when inactive
COLOR_ANN_NORMAL  = (0.10, 0.35, 0.12)   // Green status tile
COLOR_ANN_WARNING = (0.45, 0.35, 0.00)   // Amber warning tile  
COLOR_ANN_ALARM   = (0.50, 0.08, 0.08)   // Red alarm tile

// Text/border active colors
COLOR_TEXT_GREEN   = (0.18, 0.82, 0.25)
COLOR_TEXT_AMBER   = (1.00, 0.78, 0.00)
COLOR_TEXT_RED     = (1.00, 0.18, 0.18)

// Inactive
COLOR_TEXT_DIM     = (0.55, 0.58, 0.65)
COLOR_BORDER_DIM   = (0.20, 0.22, 0.26)
```

### 6.3 ISA-18.1 Annunciator State Machine

The current `AlarmFlashEffect` only does simple on/off pulsing. Real nuclear annunciators follow the ANSI/ISA-18.1 sequence documented in our own NRC HRTD Section 4 reference (`Manuals/Section_4_Annunciator_Window_Tile.md`). Each `DashboardAnnunciatorTile` implements this full 4-state machine:

```
             Condition         ACK           Condition         Auto-Reset
             Triggers         Button         Clears            or Button
  INACTIVE ──────────► ALERTING ──────► ACKNOWLEDGED ──────► CLEARING ──────► INACTIVE
   (dark)              (fast flash     (steady on,          (slow flash       (dark)
                        3 Hz + horn)    horn silenced)       0.7 Hz)
```

| State | Visual | Flash Rate | Horn | Transition Out |
|-------|--------|------------|------|----------------|
| INACTIVE | Dark tile, dim text/borders | None | Off | Condition bool becomes true → ALERTING |
| ALERTING | Lit tile, fast flash | 3 Hz | On (if audio) | ACK button pressed → ACKNOWLEDGED |
| ACKNOWLEDGED | Lit tile, steady on | None | Off | Condition bool becomes false → CLEARING |
| CLEARING | Lit tile, slow flash | 0.7 Hz | Off | Reset button or 5s auto-reset → INACTIVE |

**Key behaviors:**
- If condition clears while still in ALERTING (alarm self-clears before ACK), tile goes directly ALERTING → CLEARING (the flash slows down, operator still sees it happened)
- Status tiles (green, non-alarm) do NOT use the state machine — they are simply on/off with no flash sequence
- Warning tiles use the full state machine but with amber colors instead of red
- The ACK and RESET functions apply to all tiles simultaneously (single ACK button, matching real plant operation)

### 6.4 Tile Construction (per MosaicAlarmPanel.CreateTile)

Each tile consists of:
- Root `GameObject` with `Image` (background)
- 4× border `Image` components (top, bottom, left, right) — 1px each, anchored to edges
- `TextMeshProUGUI` label — instrument font ("Electronic Highway Sign SDF"), 9px bold, centered, multi-line
- `AnnunciatorState` enum field tracking current state in the ISA-18.1 sequence
- Flash timer driven by `Update()` with rate determined by state (3 Hz alerting, 0.7 Hz clearing)

### 6.5 Tile Definitions for Heatup Dashboard

| Index | Label | Type | Engine Source |
|-------|-------|------|--------------|
| 0 | PRESS\nHIGH | Alarm | `engine.pressureHigh` |
| 1 | PRESS\nLOW | Alarm | `engine.pressureLow` |
| 2 | LVL\nHIGH | Warning | `engine.pzrLevelHigh` |
| 3 | LVL\nLOW | Alarm | `engine.pzrLevelLow` |
| 4 | SUBCOOL\nLOW | Alarm | `engine.subcoolingLow` |
| 5 | VCT\nLEVEL | Warning | `engine.vctLevelHigh \|\| engine.vctLevelLow` |
| 6 | MASS\nCONS | Alarm | `engine.primaryMassAlarm` |
| 7 | FLOW\nLOW | Alarm | `engine.rcsFlowLow` |
| 8 | SG PRESS\nHIGH | Warning | `engine.sgSecondaryPressureHigh` |
| 9 | PZR HTRS\nON | Status | `engine.pzrHeatersOn` |
| 10 | SPRAY\nACTIVE | Status | `engine.sprayActive` |
| 11 | BUBBLE\nFORMED | Status | `engine.bubbleFormed` |

---

## 7. Implementation Stages

### Stage 1: Theme & Annunciator Foundation (Est: 2 hours)

**Objective:** Add annunciator palette to theme, create `DashboardAnnunciatorTile` component

**Tasks:**
1. Add MosaicAlarmPanel color constants to `ValidationDashboardTheme.cs`:
   - `AnnunciatorOff`, `AnnunciatorNormal`, `AnnunciatorWarning`, `AnnunciatorAlarm`
   - `AnnunciatorTextGreen`, `AnnunciatorTextAmber`, `AnnunciatorTextRed`
   - `AnnunciatorTextDim`, `AnnunciatorBorderDim`
2. Create `DashboardAnnunciatorTile.cs` in `Gauges/` folder:
   - Struct `AnnunciatorTileDescriptor` (label, isAlarm, isWarning)
   - Class `DashboardAnnunciatorTile` with:
     - Root, Background, 4× Border edges, Label (TMP with instrument font)
     - `AnnunciatorState` field tracking ISA-18.1 state machine position
     - `UpdateCondition(bool active)` — drives state transitions per ISA-18.1 sequence
     - `Acknowledge()` — ALERTING → ACKNOWLEDGED transition
     - `Reset()` — CLEARING → INACTIVE transition
     - `Update()` — drives flash timer (3 Hz alerting / 0.7 Hz clearing / steady acknowledged)
     - Status tiles (non-alarm) bypass state machine: simple on/off
     - Static `Create()` factory method following MosaicAlarmPanel.CreateTile pattern
3. Add tab transition fade helper to `TabNavigationController.cs`
4. Create shared `InstrumentFontHelper.cs` utility class:
   - Static `LoadInstrumentFont()` — loads "Electronic Highway Sign SDF" from Resources with null fallback
   - Static `LoadGlowMaterials()` — loads `Instrument_Green_Glow`, `Instrument_Amber_Glow`, `Instrument_Red_Glow` from Resources
   - Static `ApplyInstrumentStyle(TextMeshProUGUI text, float fontSize)` — applies font + green glow material
   - Static `UpdateGlowForState(TextMeshProUGUI text, AlarmState state)` — swaps TMP material (same pattern as MosaicGauge.UpdateValueMaterial)
   - Static `CreateRecessedBacking(Transform parent, Vector2 size)` — creates dark rectangle behind readout
5. Update `ArcGauge.Create()` — use `InstrumentFontHelper` for valueText
6. Update `BidirectionalGauge.Create()` — use `InstrumentFontHelper` for valueText
7. Update `DigitalReadout.Create()` — use `InstrumentFontHelper` for valueText + add recessed backing

**Deliverables:**
- Updated `ValidationDashboardTheme.cs`
- New `DashboardAnnunciatorTile.cs`
- New `InstrumentFontHelper.cs`
- Updated `TabNavigationController.cs`
- Updated `ArcGauge.cs` (Create() factory only)
- Updated `BidirectionalGauge.cs` (Create() factory only)
- Updated `DigitalReadout.cs` (Create() factory only)

**Exit Criteria:**
- `DashboardAnnunciatorTile.Create()` produces a tile visually matching MosaicAlarmPanel style
- Theme has all annunciator colors accessible as static properties
- Gauge `Create()` methods produce readouts with instrument font + green glow material
- Material swaps to amber/red on threshold crossings

---

### Stage 2: Overview Layout Restructure (Est: 2 hours)

**Objective:** Rebuild Overview panel grid from 4+3 equal columns to 3+2 proportional sections

**Tasks:**
1. Rewrite `OverviewPanel.BuildLayout()`:
   - Top row (60% height): 3 sections with proportional flex weights
     - Reactor/RCS (flex 1.2) — houses pressure, Tavg, subcooling gauges + RCP indicators
     - Pressurizer (flex 1.0) — houses level gauge, bubble/heater indicators, temperatures
     - CVCS/SG (flex 1.0) — houses charging/letdown readouts, net flow gauge, SG data
   - Bottom row (40% height): 2 sections
     - System Health (flex 0.8) — mass error, energy status, system indicators
     - Alarm Annunciator (flex 1.5) — 6×2 annunciator tile grid
   - Section padding 6px, inter-section spacing 8px
2. Update `OverviewSectionBase.CreateSection<T>()` to pass flex weight and minimum height
3. Ensure VerticalLayoutGroup in each section has appropriate spacing for gauge components

**Deliverables:**
- Updated `OverviewPanel.cs`

**Exit Criteria:**
- 5 sections render at appropriate proportions
- Sections have visible background color distinction
- Adequate space within each section for gauge components (verified visually)

---

### Stage 3: Reactor/RCS Section with Gauges (Est: 3 hours)

**Objective:** Replace text rows in Reactor/RCS section with real ArcGauges and StatusIndicators

**Tasks:**
1. Rewrite `OverviewSection_RCS.cs` → rename to `OverviewSection_ReactorRCS.cs` (merged section):
   - Top area: 2×2 grid of ArcGauges
     - RCS Pressure (0-2500 psia, warn 350/2300, alarm 300/2385)
     - T-avg (50-650°F, warn by mode, alarm ±50°F)
     - Subcooling (0-200°F, warn 30, alarm 20)
     - Delta-T (0-80°F) or PZR Pressure as 4th gauge
   - Bottom area: 4× compact StatusIndicators for RCPs (RCP-1, RCP-2, RCP-3, RCP-4)
   - Each ArcGauge uses the existing `ArcGauge.Create()` factory method
2. Remove old `OverviewSection_ReactorCore.cs` (absorbed into merged section)
3. Update `OverviewPanel.InitializeSections()` to use new merged section
4. Wire `UpdateData()` to call `gauge.SetValue()` instead of `row.SetValue()`

**Deliverables:**
- New `OverviewSection_ReactorRCS.cs`
- Deleted `OverviewSection_ReactorCore.cs` and `OverviewSection_RCS.cs`
- Updated `OverviewPanel.cs`

**Exit Criteria:**
- 4 arc gauges render with visible arcs, animated needles, and threshold coloring
- RCP status indicators show on/off state with proper colors
- Values update at 10Hz data refresh rate
- Needles animate smoothly at 30fps

---

### Stage 4: Pressurizer & CVCS/SG Sections with Gauges (Est: 3 hours)

**Objective:** Replace text rows in Pressurizer and CVCS/SG sections with gauges

**Tasks:**
1. Rewrite `OverviewSection_Pressurizer.cs`:
   - Top: ArcGauge for PZR Level (0-100%, warn 17/80, alarm 12/92)
   - Middle: 2× StatusIndicators (BUBBLE, HEATERS) using pill shape
   - Bottom: 2× DigitalReadouts (PZR Temp, Surge Flow — the latter using BidirectionalGauge if space allows)
2. Rewrite `OverviewSection_CVCS.cs` → rename to `OverviewSection_CVCSG.cs` (merged with SG):
   - Top: 2× DigitalReadouts (Charging Flow, Letdown Flow)
   - Middle: BidirectionalGauge for Net CVCS Flow (±75 GPM range)
   - Bottom: 2× DigitalReadouts (SG Secondary Pressure, Net Plant Heat — with sign-based color)
3. Wire all `UpdateData()` methods to gauge SetValue APIs
4. Remove old `OverviewSection_SGRHR.cs` (absorbed into merged section)

**Deliverables:**
- Rewritten `OverviewSection_Pressurizer.cs`
- New `OverviewSection_CVCSG.cs`
- Deleted `OverviewSection_CVCS.cs` and `OverviewSection_SGRHR.cs`
- Updated `OverviewPanel.cs`

**Exit Criteria:**
- PZR Level arc gauge renders correctly with animated needle
- Net CVCS bidirectional gauge shows positive/negative deflection
- All digital readouts display with threshold coloring
- Status indicators pulse on warning/alarm states

---

### Stage 5: System Health & Annunciator Grid (Est: 3 hours)

**Objective:** Rebuild bottom row with proper system health section and MosaicAlarmPanel-style annunciator grid

**Tasks:**
1. Rewrite `OverviewSection_GlobalHealth.cs` → rename to `OverviewSection_SystemHealth.cs`:
   - DigitalReadout for Mass Conservation Error (lbm) with alarm thresholds
   - DigitalReadout for Energy Balance (MW, derived)
   - 3× StatusIndicators: Mass Conservation OK, SG Boiling, RHR Active
   - Vertical layout, compact
2. Complete rewrite of `OverviewSection_Alarms.cs`:
   - Replace `AlarmTile` with `DashboardAnnunciatorTile` grid
   - 6×2 GridLayoutGroup (12 tiles per §6.4 definitions)
   - Each tile constructed via `DashboardAnnunciatorTile.Create()`
   - Wire `UpdateData()` to evaluate each tile condition from engine state
   - Alarm/warning tiles use ISA-18.1 state machine; status tiles use simple on/off
3. Delete the old `AlarmTile` class (embedded in old `OverviewSection_Alarms.cs`)

**Deliverables:**
- New `OverviewSection_SystemHealth.cs`
- Rewritten `OverviewSection_Alarms.cs` (using `DashboardAnnunciatorTile`)
- Deleted old `OverviewSection_GlobalHealth.cs`
- Updated `OverviewPanel.cs`

**Exit Criteria:**
- Annunciator tiles match MosaicAlarmPanel visual style: dark when off, green/amber/red when lit
- Tiles use instrument font with multi-line centered labels
- 4-edge borders are visible and properly colored per state
- Annunciator tiles follow ISA-18.1 sequence: fast flash → steady → slow flash → off
- System health readouts display with proper threshold coloring

---

### Stage 6: Detail Panel Gauge Integration (Est: 2 hours)

**Objective:** Upgrade detail tab panels to use gauge components where they currently use only text

**Tasks:**
1. **PressurizerPanel.cs**: Add ArcGauge for PZR Level and ArcGauge for PZR Pressure as hero gauges at the top of the panel, keeping existing DigitalReadouts for secondary parameters below
2. **CVCSPanel.cs**: Add ArcGauges for Charging Flow and Letdown Flow, BidirectionalGauge for Net Flow as hero gauges, VCT Level as LinearGauge
3. **PrimaryLoopPanel.cs**: Add 4× DigitalReadouts in a loop grid showing Thot/Tcold per loop, with StatusIndicators for each RCP
4. **SGRHRPanel.cs**: Add DigitalReadouts for SG parameters with StatusIndicators for system states
5. Add tab transition fade animation: on tab switch, fade out current panel CanvasGroup alpha over 150ms, fade in new panel over 150ms

**Deliverables:**
- Updated `PressurizerPanel.cs`
- Updated `CVCSPanel.cs`
- Updated `PrimaryLoopPanel.cs`
- Updated `SGRHRPanel.cs`
- Updated `TabNavigationController.cs` (fade transition)

**Exit Criteria:**
- Each detail tab has at least 2 visual gauge components (ArcGauge, BiGauge, or LinearGauge)
- Tab transitions are smooth alpha fades rather than instant show/hide
- All existing data binding continues to work

---

### Stage 7: Visual Polish & Validation (Est: 2 hours)

**Objective:** Final visual pass, performance check, and validation

**Tasks:**
1. Verify all ArcGauge glow rendering — if `GlowImage` is not being used, ensure the arc color segments provide sufficient visual feedback (the `OnPopulateMesh` approach doesn't need a separate glow Image)
2. Tune gauge sizes: ensure arc gauges are at least 80px diameter for readability at 1080p
3. Tune animation parameters: verify SmoothDamp times feel responsive but not jumpy
4. Verify MiniTrends panel is rendering sparkline traces (already functional per code review)
5. Test at 1080p and 1440p resolutions
6. Performance spot-check: verify no frame drops during normal operation
7. Screenshot comparison: before (current) vs after (this IP) for documentation
8. Verify no compile errors, no null reference exceptions in console

**Deliverables:**
- Validated dashboard with all gauges rendering
- Performance confirmation
- Changelog

**Exit Criteria:**
- All OV-01 through OV-15 requirements met
- All AN-01 through AN-09 annunciator requirements met
- All AM-01 through AM-04 animation requirements met
- No console errors during 5-minute heatup simulation run
- Dashboard is visually professional and information-dense

---

## 8. File Structure (Final State)

```
Assets/Scripts/UI/ValidationDashboard/
├── ValidationDashboardBuilder.cs        (modified — layout proportions)
├── ValidationDashboardController.cs     (unchanged)
├── ValidationDashboardLauncher.cs       (unchanged)
├── ValidationDashboardSceneSetup.cs     (unchanged)
├── ValidationDashboardTestSetup.cs      (unchanged)
├── ValidationDashboardTheme.cs          (modified — annunciator colors)
├── TabNavigationController.cs           (modified — fade transitions)
│
├── Gauges/
│   ├── ArcGauge.cs                      (modified — instrument font in Create())
│   ├── BidirectionalGauge.cs            (modified — instrument font in Create())
│   ├── LinearGauge.cs                   (unchanged)
│   ├── DigitalReadout.cs                (modified — instrument font in Create())
│   ├── StatusIndicator.cs               (unchanged)
│   ├── GaugeAnimator.cs                 (unchanged)
│   ├── InstrumentFontHelper.cs          (NEW)
│   └── DashboardAnnunciatorTile.cs      (NEW)
│
├── Panels/
│   ├── ValidationPanelBase.cs           (unchanged)
│   ├── HeaderPanel.cs                   (unchanged)
│   ├── OverviewPanel.cs                 (REWRITTEN — new layout)
│   ├── OverviewSection_ReactorRCS.cs    (NEW — merged from ReactorCore + RCS)
│   ├── OverviewSection_Pressurizer.cs   (REWRITTEN — gauges)
│   ├── OverviewSection_CVCSG.cs         (NEW — merged from CVCS + SGRHR)
│   ├── OverviewSection_SystemHealth.cs  (NEW — replaces GlobalHealth)
│   ├── OverviewSection_Alarms.cs        (REWRITTEN — annunciator grid)
│   ├── PressurizerPanel.cs              (MODIFIED — hero gauges added)
│   ├── CVCSPanel.cs                     (MODIFIED — hero gauges added)
│   ├── PrimaryLoopPanel.cs              (MODIFIED — loop grid)
│   ├── SGRHRPanel.cs                    (MODIFIED — readout upgrade)
│   ├── AlarmsPanel.cs                   (unchanged for now)
│   ├── ValidationPanel.cs              (unchanged for now)
│   └── MiniTrendsPanel.cs              (unchanged)
│
├── Trends/
│   ├── MiniTrendStrip.cs               (unchanged)
│   └── TrendBuffer.cs                  (unchanged)
│
└── Effects/
    └── AlarmFlashEffect.cs             (unchanged)
```

### Files Deleted:
- `OverviewSection_ReactorCore.cs` (merged into ReactorRCS)
- `OverviewSection_RCS.cs` (merged into ReactorRCS)
- `OverviewSection_CVCS.cs` (merged into CVCSG)
- `OverviewSection_SGRHR.cs` (merged into CVCSG)
- `OverviewSection_GlobalHealth.cs` (replaced by SystemHealth)
- `AlarmTile` class (embedded in old OverviewSection_Alarms.cs — replaced by DashboardAnnunciatorTile)

---

## 9. Dependencies

### 9.1 Internal Dependencies
- `HeatupSimEngine` — all data binding (read-only access, no modifications)
- `PlantConstants` — threshold values for gauge ranges
- `ValidationDashboardTheme` — all colors and sizing constants
- Existing gauge component library (ArcGauge, BidirectionalGauge, etc.)

### 9.2 External Dependencies
- TextMeshPro (already included)
- "Electronic Highway Sign SDF" font (already in `Resources/Fonts & Materials/` per MosaicAlarmPanel usage)

### 9.3 GOLD Module Impact
**None.** This IP modifies only UI display code. No physics, simulation, or GOLD modules are touched.

---

## 10. Risks and Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| ArcGauge too small in sections | Med | Med | Enforce minimum 80px diameter; test early in Stage 3 |
| Instrument font not found at runtime | Med | Low | Graceful fallback to default TMP font (same as MosaicAlarmPanel) |
| Too many gauges causing layout overflow | Med | Med | Prioritize 4 key arc gauges; use DigitalReadout for secondary params |
| Performance impact from multiple MaskableGraphic gauges | Low | Low | ArcGauge OnPopulateMesh is lightweight; profile in Stage 7 |
| Merge conflicts with OverviewSection files | Low | Low | Clean delete-and-replace approach; no incremental edits |

---

## 11. Unaddressed Issues

| Issue | Reason | Future Plan |
|-------|--------|-------------|
| Custom glow shader (GlowArc.shader) | ArcGauge threshold segments provide sufficient visual feedback without a separate glow shader; adds complexity without proportional benefit at this stage | Future enhancement if shader pipeline is established |
| Full AlarmsPanel tab annunciator matrix | The Alarms detail tab panel is not reworked in this IP (only the Overview alarm summary) | Can be upgraded to full MosaicAlarmPanel grid in a future IP |
| ValidationPanel tab visual upgrade | Debug/validation telemetry tab is functional for development purposes and does not need visual polish | Low priority cosmetic upgrade |
| Responsive breakpoints for 4K displays | CanvasScaler handles basic scaling; true responsive breakpoints for ultra-high DPI need testing | Test and adjust if 4K users report issues |

---

## 12. Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Arc gauges rendered on Overview | ≥ 4 | Visual count |
| Bidirectional gauges rendered | ≥ 1 | Visual count |
| Digital readouts rendered on Overview | ≥ 6 | Visual count |
| Status indicators on Overview | ≥ 6 | Visual count |
| Annunciator tiles matching MosaicAlarmPanel style | 12 | Visual audit |
| Annunciator tiles with 4-edge borders | 12/12 | Visual audit |
| ISA-18.1 annunciator sequence working | Yes | Runtime test: trigger alarm → fast flash, ACK → steady, clear → slow flash |
| Tab transition animation | Smooth fade | Visual test |
| Console errors during 5-min run | 0 | Runtime test |
| Frame time overhead | < 2ms | Unity Profiler |
| "Does this look professional?" | Yes | Craig's assessment |

---

## 13. Approval

**Prepared By:** Claude (AI Assistant)  
**Date:** 2026-02-17  

**Approval Required From:** Craig (Project Lead)

Before implementation begins, Craig should review and approve:
1. The merged section strategy (combining ReactorCore+RCS, CVCS+SGRHR, GlobalHealth→SystemHealth)
2. The annunciator tile count and definitions (12 tiles in §6.4)
3. The gauge placement choices (which parameters get ArcGauge vs DigitalReadout vs StatusIndicator)
4. The 7-stage implementation sequence

---

## 14. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-02-17 | Claude | Initial draft |
| 1.1 | 2026-02-17 | Claude | Added instrument font digital readout enhancement (§4.5 IR requirements), InstrumentFontHelper utility, ArcGauge/BidirectionalGauge/DigitalReadout Create() updates for paired analog+digital with glow materials |
| 1.2 | 2026-02-17 | Claude | Added ISA-18.1 annunciator state machine (§6.3) — full INACTIVE→ALERTING→ACKNOWLEDGED→CLEARING→INACTIVE sequence per NRC HRTD Section 4 reference. Replaces simple AlarmFlashEffect with proper 4-state machine with tiered flash rates (3 Hz / steady / 0.7 Hz) |
