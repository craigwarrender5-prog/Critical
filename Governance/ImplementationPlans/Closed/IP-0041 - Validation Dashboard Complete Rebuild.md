# IP-0041: Validation Dashboard — Complete Rebuild

**Date:** 2026-02-17
**Domain Plan:** DP-0008 (Operator Interface & Scenarios)
**Predecessors:** IP-0025 (CLOSED), IP-0029 (CLOSED), IP-0030 (FAILED), IP-0031 (CLOSED), IP-0040 (FAILED)
**Status:** SUPERSEDED — Closed in favor of IP-0042 (later redirected to IP-0043)
**Closed Date:** 2026-02-17
**Superseded By:** IP-0042, IP-0043
**Priority:** Critical
**Changelog Required:** Yes

---

## 1. Problem Summary

Five previous implementation plans attempted to replace the legacy IMGUI-based `HeatupValidationVisual` dashboard with a modern uGUI system. All failed to deliver a functional, professional result. The current state of the uGUI ValidationDashboard:

| Failure | Detail |
|---------|--------|
| **Black rectangle readouts** | Instrument font loaded from wrong Resources path — all DigitalReadouts render as opaque black boxes |
| **No event log** | The legacy system has a dedicated LOG tab with severity filtering (ALL/INFO/ALERT/ALARM) and scrollable timestamped entries. The new dashboard has nothing |
| **No graphs or strip charts** | The legacy system has 7 graph categories (TEMPS, PRESSURE, CVCS, VCT/BRS, RATES, RCP HEAT, HZP). The new dashboard has only mini sparklines in a side panel |
| **12 annunciator tiles vs 27** | Missing 15 tiles including RVLIS LOW, CCW RUNNING, SEAL INJ OK, all 4 RCPs, VCT DIVERT/MAKEUP, RWST SUCTION, SMM margin tiles, HEATUP IN PROG, MODE PERMISSIVE |
| **~20% parameter coverage** | The comprehensive requirements list specifies 100+ parameters. The new dashboard shows fewer than 25 |
| **No bar gauges** | IP-0025 specified bar gauges for heater demand, spray valve position, HZP progress, SG boiling intensity. None exist |
| **No full-size StripChart component** | IP-0025 Stage 3 specified a multi-trace, auto-scaling, grid-lined strip chart. Never built |
| **Tiny gauges in empty panels** | GridLayoutGroup with fixed 130px cells in sections that are 300+ pixels tall — gauges float in dark voids |
| **No interactivity** | Annunciator ACK button exists in code but no visible clickable interface |
| **No visual excitement** | No glow effects, no meaningful animations, no visual hierarchy, no professional instrument aesthetic |
| **Missing tabs** | No RCP/Electrical tab, no CRITICAL tab, no dedicated Event Log tab |

### What Works (Keep)

The low-level gauge components are correctly coded and functional:

- `ArcGauge.cs` — 180° sweep with OnPopulateMesh, SmoothDamp needle, threshold coloring
- `BidirectionalGauge.cs` — 270° center-zero with dual-color arcs
- `LinearGauge.cs` — Horizontal/vertical bar with threshold coloring
- `DigitalReadout.cs` — Value animation, color transitions, threshold-based coloring
- `StatusIndicator.cs` — On/off/warning/alarm states, pulse animation
- `MiniTrendStrip.cs` — Sparkline rendering via OnPopulateMesh
- `TrendBuffer.cs` — Ring buffer with auto-scaling
- `DashboardAnnunciatorTile.cs` — ISA-18.1 state machine, 4-edge borders
- `GaugeAnimator.cs` — Easing functions, pulse/glow helpers
- `ValidationDashboardTheme.cs` — Color palette, sizing constants

---

## 2. Design Philosophy

This is attempt #6. The following principles govern this IP to prevent repeating past failures:

1. **Feature parity first** — Before any visual polish, the new dashboard must display every parameter the legacy system displays, plus the expanded parameter set. No parameter left behind.
2. **The primary surface is king** — All critical parameters visible on one 1920×1080 screen with NO tab switching and NO scrolling. Tabs exist for detail, not for finding critical data.
3. **Instruments, not text rows** — Arc gauges for primary parameters, bar gauges for control outputs, bidirectional gauges for signed flows, digital readouts for secondary values, LED indicators for booleans. ParameterRow text rows are banned from the Overview.
4. **The event log is not optional** — Operators need to see what happened. A scrollable, severity-filtered, timestamped event log is a core feature.
5. **Annunciator completeness** — All 27 tiles from the legacy system, with ISA-18.1 state machine and clickable ACK.
6. **Graphs are not optional** — The legacy system's 7 graph categories feed from engine history buffers. The new system must provide equivalent trend visibility.
7. **Font must work** — Fix the instrument font path before anything else. If the font can't load, use the default TMP font with green coloring. No black rectangles.

---

## 3. Primary Operations Surface Layout

The primary screen (Overview tab) uses a 5-column + footer layout. ALL critical parameters are visible without tab switching.

```
┌────────────────────────────────────────────────────────────────────────────────┐
│  HEADER: [Mode LED] MODE 5 │ SOLID PZR - HEATING TO TSAT │ SIM: 2:15:30      │
│  WALL: 0:08:12 │ SPEED: [1x][2x][4x][8x][10x] │ ⚠ 2 ALARMS ACTIVE          │
├──────────┬───────────┬───────────┬───────────┬─────────────────────────────────┤
│  RCS     │ PRESSUR-  │ CVCS &    │ SG &      │  ALWAYS-ON TRENDS              │
│  PRIMARY │ IZER      │ VCT       │ RHR       │                                │
│          │           │           │           │  ┌─ RCS Pressure ────────────┐  │
│ [ARC]    │ [ARC]     │ Chg  ──── │ [ARC]     │  └──────────────────────────┘  │
│ T_avg    │ PZR Press │ Ltd  ──── │ SG Press  │  ┌─ PZR Level ──────────────┐  │
│          │           │ [BD]      │           │  └──────────────────────────┘  │
│ T_hot    │ [ARC]     │ Net CVCS  │ SG Temps  │  ┌─ T_avg / T_hot / T_cold ─┐  │
│ T_cold   │ PZR Level │           │ SG Heat   │  └──────────────────────────┘  │
│ Core ΔT  │           │ [ARC]     │ SG Boil ○ │  ┌─ Heatup Rate ────────────┐  │
│ T_pzr    │ T_pzr     │ VCT Level │ N₂ iso  ○ │  └──────────────────────────┘  │
│ T_sat    │ T_sat     │ VCT Boron │ Stm Dump○ │  ┌─ Subcooling ────────────┐   │
│          │           │ VCT LED ○ │           │  └──────────────────────────┘  │
│ [ARC]    │ [BAR]     │           │ RHR mode  │  ┌─ Net CVCS ──────────────┐   │
│ Subcool  │ Heater kW │ Mass Err  │ RHR MW    │  └──────────────────────────┘  │
│          │ [BAR]     │ Mass lbm  │           │  ┌─ SG Pressure ────────────┐  │
│ Heatup   │ Spray %   │           │ [BAR]     │  └──────────────────────────┘  │
│ Rate     │ [BD]      │ BRS %     │ HZP Prog  │  ┌─ Net Plant Heat ─────────┐  │
│ Press    │ Surge     │ BRS flow  │ HZP rdy ○ │  └──────────────────────────┘  │
│ Rate     │           │           │           │                                │
│ RCPs:    │ Bubble: ○ │ Seal OK ○ │           │                                │
│ ○○○○     │ P err     │ RWST    ○ │           │                                │
│ Flow α   │ L err     │           │           │                                │
│ RCP MW   │ L setpt   │           │           │                                │
├──────────┴───────────┴───────────┴───────────┼─────────────────────────────────┤
│  ANNUNCIATOR PANEL (27 tiles, 2-3 rows)       │  EVENT LOG (scrollable)         │
│  [HTR ON][HEATUP][BUBBLE][MODE][P LO][P HI]  │  12:15:30 INFO  Phase → HEATUP │
│  [SC LO][FLOW][LVL LO][LVL HI][RVLIS][CCW]  │  12:15:35 ALERT PZR Level Low  │
│  [CHG][LTD][SEAL][VCT LO][VCT HI][DVRT]...  │  12:16:01 ALARM Subcool < 20°F │
│                               [ACK] [RESET]  │  [ALL] [INFO] [ALERT] [ALARM]  │
└───────────────────────────────────────────────┴─────────────────────────────────┘
```

**Column widths (flex):** RCS 1.0 | PZR 0.9 | CVCS 0.9 | SG/RHR 0.9 | Trends 1.3
**Row split:** Top 62% (5 columns) | Bottom 38% (Annunciators + Event Log)

---

## 4. Complete Parameter Inventory

Every parameter listed below MUST appear on the dashboard. Parameters marked [OV] appear on the Overview primary surface. Parameters marked [TAB] appear on detail tabs only.

### 4.1 Global Simulation Health
| Parameter | Engine Field | Display | Location |
|-----------|-------------|---------|----------|
| Sim time | `simTime` | Digital HH:MM:SS | [OV] Header |
| Wall time | `wallClockTime` | Digital HH:MM:SS | [OV] Header |
| Speed | `currentSpeedIndex` | Button array | [OV] Header |
| Plant mode | `plantMode` | Mode LED + text | [OV] Header |
| Phase | `heatupPhaseDesc` | Text | [OV] Header |
| Active alarm count | Derived from tiles | Badge | [OV] Header |
| Mass conservation error | `massError_lbm` | Digital + trend | [OV] CVCS col |
| System total mass | `totalSystemMass_lbm` | Digital | [OV] CVCS col |
| Net plant heat | `netPlantHeat_MW` | Digital + trend | [OV] Trends |

### 4.2 RCS Primary [OV Column 1]
| Parameter | Engine Field | Display |
|-----------|-------------|---------|
| T_avg | `T_avg` | **ArcGauge** (50-650°F) |
| T_hot | `T_hot` | Digital |
| T_cold | `T_cold` | Digital |
| Core ΔT | `T_hot - T_cold` | Digital |
| T_pzr | `T_pzr` | Digital |
| T_sat | `T_sat` | Digital |
| Subcooling | `subcooling` | **ArcGauge** (0-200°F) |
| RCS Pressure | `pressure` | Digital (also PZR gauge) |
| Heatup rate | `heatupRate` | Digital, colored |
| Pressure rate | `pressureRate` | Digital, colored |
| RCP status ×4 | `rcpRunning[0-3]` | 4× StatusIndicator |
| RCP count | `rcpCount` | Digital |
| Effective RCP heat | `effectiveRCPHeat` | Digital MW |
| RCP flow fraction | `rcpContribution.TotalFlowFraction` | Digital |

### 4.3 Pressurizer [OV Column 2]
| Parameter | Engine Field | Display |
|-----------|-------------|---------|
| PZR Pressure | `pressure` | **ArcGauge** (0-2500 psia) |
| PZR Level | `pzrLevel` | **ArcGauge** (0-100%) |
| PZR liquid temp | `T_pzr` | Digital |
| T_sat at pressure | `T_sat` | Digital |
| PZR water volume | `pzrWaterVolume` | Digital ft³ |
| PZR steam volume | `pzrSteamVolume` | Digital ft³ |
| Heater power | `pzrHeaterPower * 1000` | **LinearGauge** (bar) kW |
| Heater PID output | `heaterPIDOutput` | **LinearGauge** (bar) 0-100% |
| Heater mode | `currentHeaterMode` | Text |
| Heaters on | `pzrHeatersOn` | StatusIndicator |
| Spray active | `sprayActive` | StatusIndicator |
| Spray flow | `sprayFlow_GPM` | Digital gpm |
| Spray valve position | `sprayValvePosition` | **LinearGauge** (bar) 0-100% |
| Surge flow | `surgeFlow` | **BidirectionalGauge** (±50 gpm) |
| Bubble state | `solidPressurizer` / `bubbleFormed` | StatusIndicator + text |
| Bubble phase | `bubblePhase` | Text |
| Pressure error | `solidPlantPressureError` | Digital psi |
| Level error | Derived: `pzrLevel - pzrLevelSetpointDisplay` | Digital % |
| Level setpoint | `pzrLevelSetpointDisplay` | Digital % |

### 4.4 CVCS & VCT [OV Column 3]
| Parameter | Engine Field | Display |
|-----------|-------------|---------|
| Charging active | `chargingActive` | StatusIndicator |
| Charging flow | `chargingFlow` | Digital gpm |
| Letdown active | `letdownActive` | StatusIndicator |
| Letdown flow | `letdownFlow` | Digital gpm |
| Net CVCS | `chargingFlow - letdownFlow` | **BidirectionalGauge** (±75 gpm) |
| Letdown path | `letdownViaRHR` / `letdownViaOrifice` | Text |
| Letdown isolated | `letdownIsolatedFlag` | StatusIndicator (alarm) |
| Seal injection OK | `sealInjectionOK` | StatusIndicator |
| VCT level | `vctState.Level_percent` | **ArcGauge** (0-100%) |
| VCT volume | `vctState.Volume_gal` | Digital gal |
| VCT boron | `vctState.BoronConcentration_ppm` | Digital ppm |
| VCT makeup active | `vctMakeupActive` | StatusIndicator |
| VCT divert active | `vctDivertActive` | StatusIndicator |
| RWST suction | `vctRWSTSuction` | StatusIndicator (alarm) |
| Mass conservation error | `massError_lbm` | Digital, colored |
| System total mass | `totalSystemMass_lbm` | Digital lbm |
| BRS holdup level | `BRSPhysics.GetHoldupLevelPercent(brsState)` | Digital % |
| BRS inflow | `brsState.InFlow_gpm` | Digital gpm |
| BRS return flow | `brsState.ReturnFlow_gpm` | Digital gpm |

### 4.5 SG & RHR [OV Column 4]
| Parameter | Engine Field | Display |
|-----------|-------------|---------|
| SG secondary pressure | `sgSecondaryPressure_psia` | **ArcGauge** |
| SG sat temp | `sgSaturationTemp_F` | Digital °F |
| SG bulk temp | `T_sg_secondary` | Digital °F |
| SG heat transfer | `sgHeatTransfer_MW` | Digital MW |
| SG boiling active | `sgBoilingActive` | StatusIndicator |
| SG boiling intensity | `sgBoilingIntensity` | **LinearGauge** (bar) |
| SG N₂ isolated | `sgNitrogenIsolated` | StatusIndicator |
| SG wide-range level | `sgWideRangeLevel_pct` | Digital % |
| Steam dump active | `steamDumpActive` | StatusIndicator |
| Steam dump heat | `steamDumpHeat_MW` | Digital MW |
| RHR active | `rhrActive` | StatusIndicator |
| RHR mode | `rhrModeString` | Text |
| RHR net heat | `rhrNetHeat_MW` | Digital MW |
| RHR HX removal | `rhrHXRemoval_MW` | Digital MW |
| HZP progress | `hzpProgress` | **LinearGauge** (bar) 0-100% |
| HZP stable | `hzpStable` | StatusIndicator |
| HZP ready | `hzpReadyForStartup` | StatusIndicator |

### 4.6 Always-On Trends [OV Column 5]
| Trend | Engine Source | Color |
|-------|-------------|-------|
| RCS Pressure | `pressure` + `pressHistory` | Green |
| PZR Level | `pzrLevel` + `pzrLevelHistory` | Amber |
| T_avg / T_hot / T_cold | `T_avg` + `tempHistory` etc. | Orange/Blue |
| Heatup Rate | `heatupRate` + `heatRateHistory` | Cyan |
| Subcooling | `subcooling` + `subcoolHistory` | Blue |
| Net CVCS | Derived from `chargingHistory - letdownHistory` | Purple |
| SG Pressure | `sgSecondaryPressure_psia` | Cyan |
| Net Plant Heat | `netPlantHeat_MW` | Magenta |

### 4.7 Annunciator Tiles (27 tiles)
All 27 tiles from the legacy `HeatupValidationVisual.Annunciators.cs`:

| # | Label | Type | Engine Source |
|---|-------|------|--------------|
| 0 | PZR HTRS ON | Status (green) | `pzrHeatersOn` |
| 1 | HEATUP IN PROG | Status (green) | `heatupInProgress` |
| 2 | STEAM BUBBLE OK | Status (green) | `steamBubbleOK` |
| 3 | MODE PERMISSIVE | Status (green) | `modePermissive` |
| 4 | PRESS LOW | Alarm (red) | `pressureLow` |
| 5 | PRESS HIGH | Alarm (red) | `pressureHigh` |
| 6 | SUBCOOL LOW | Alarm (red) | `subcoolingLow` |
| 7 | RCS FLOW LOW | Alarm (red) | `rcsFlowLow` |
| 8 | PZR LVL LOW | Alarm (red) | `pzrLevelLow` |
| 9 | PZR LVL HIGH | Warning (amber) | `pzrLevelHigh` |
| 10 | RVLIS LOW | Alarm (red) | `rvlisLevelLow` |
| 11 | CCW RUNNING | Status (green) | `ccwRunning` |
| 12 | CHARGING ACTIVE | Status (green) | `chargingActive` |
| 13 | LETDOWN ACTIVE | Status (green) | `letdownActive` |
| 14 | SEAL INJ OK | Status (green) | `sealInjectionOK` |
| 15 | VCT LVL LOW | Alarm (red) | `vctLevelLow` |
| 16 | VCT LVL HIGH | Warning (amber) | `vctLevelHigh` |
| 17 | VCT DIVERT | Status (green) | `vctDivertActive` |
| 18 | VCT MAKEUP | Status (green) | `vctMakeupActive` |
| 19 | RWST SUCTION | Alarm (red) | `vctRWSTSuction` |
| 20 | RCP #1 RUN | Status (green) | `rcpRunning[0]` |
| 21 | RCP #2 RUN | Status (green) | `rcpRunning[1]` |
| 22 | RCP #3 RUN | Status (green) | `rcpRunning[2]` |
| 23 | RCP #4 RUN | Status (green) | `rcpRunning[3]` |
| 24 | SMM LOW MARGIN | Warning (amber) | `smmLowMargin` |
| 25 | SMM NO MARGIN | Alarm (red) | `smmNoMargin` |
| 26 | SG PRESS HIGH | Alarm (red) | `sgSecondaryPressureHigh` |

---

## 5. Tab Structure

| Tab | Name | Content |
|-----|------|---------|
| 0 | OVERVIEW | Primary operations surface (Section 3 layout) — all critical parameters, no scrolling |
| 1 | PRIMARY | Detailed RCS/core data, loop temperatures, RCP detail, 2× stacked graphs (TEMPS + PRESSURE) |
| 2 | PZR | Full pressurizer detail, bubble diagnostics, heater/spray control detail, 2× graphs (PRESSURE + RATES) |
| 3 | CVCS | Detailed CVCS flows, VCT state, BRS state, orifice/RHR letdown breakdown, 2× graphs (CVCS + VCT/BRS) |
| 4 | SG/RHR | SG secondary detail, RHR system, heat balance, steam dump, 2× graphs (SG + HZP) |
| 5 | LOG | Full 27-tile annunciator grid (top 35%) + severity-filtered event log (bottom 65%) |
| 6 | GRAPHS | Full-width tabbed strip charts — all 7 graph categories from legacy system |
| 7 | VALIDATION | RVLIS data, mass/energy audit, PASS/FAIL checks, debug telemetry |

---

## 6. New Components Required

### 6.1 StripChart.cs (NEW — Critical Missing Component)
Full-size multi-trace trend chart with:
- Custom `Graphic` subclass using `OnPopulateMesh`
- Up to 6 traces per chart
- Auto-scaling Y-axis with grid lines
- Time axis with labels (rolling 4-hour window)
- Compact color-coded legend with live values
- Reference lines for Tech Spec limits / setpoints
- Reads from engine `List<float>` history buffers + `timeHistory`

### 6.2 EventLogPanel.cs (NEW — Critical Missing Feature)
Scrollable event log panel with:
- Reads from `engine.eventLog` (List<EventLogEntry>)
- Severity color coding (INFO=gray, ACTION=cyan, ALERT=amber, ALARM=red)
- Filter buttons: ALL / INFO / ALERT / ALARM
- Auto-scroll to newest entry
- Visible-only rendering for performance (same pattern as legacy)
- Pre-formatted strings from `entry.FormattedLine` (zero allocation)

### 6.3 BarGauge.cs (NEW — Needed for heater/spray/HZP)
If `LinearGauge.cs` doesn't adequately serve as a horizontal bar with label + value + setpoint marker, a dedicated BarGauge component may be needed. Evaluate LinearGauge first — it may be sufficient with proper factory method parameters.

---

## 7. Critical Fixes (Before Any Layout Work)

### 7.1 Instrument Font Path
**Problem:** `InstrumentFontHelper` loads from `Resources/Fonts & Materials/Electronic Highway Sign SDF`. The font asset actually lives at `TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset`.

**Fix:** Copy the font asset (and its atlas texture + material) from the TMP Examples Resources folder into the project's main `Assets/Resources/Fonts & Materials/` folder. This ensures `Resources.Load<TMP_FontAsset>("Fonts & Materials/Electronic Highway Sign SDF")` works reliably regardless of whether TMP Examples & Extras is imported.

**Fallback:** If the font still fails to load at runtime, `InstrumentFontHelper` must apply a visible fallback: default TMP font with green coloring and NO recessed backing that could mask the text. The current failure mode (black rectangle) is unacceptable.

### 7.2 Recessed Backing Z-Order
**Problem:** `CreateRecessedBacking` uses `SetAsFirstSibling()` to place the backing behind the text, but the backing may be rendering ON TOP of the text in certain layout configurations, producing black rectangles.

**Fix:** Verify the sibling order is correct. If the backing is covering the text, either fix the hierarchy or remove the backing and use a simple background color on the text's parent instead.

---

## 8. Implementation Stages

### Stage 1: Font Fix + StripChart + EventLog Components (Foundation)
**Objective:** Fix the black rectangle issue, build the two critical missing components.

**Tasks:**
1. Copy Electronic Highway Sign SDF font assets to `Assets/Resources/Fonts & Materials/`
2. Fix `InstrumentFontHelper.cs` fallback — if font is null, use default TMP font with green color, NO black backing
3. Fix `CreateRecessedBacking` z-order issue
4. Build `StripChart.cs` — full multi-trace chart component based on `OnPopulateMesh`
5. Build `EventLogPanel.cs` — standalone scrollable event log component with severity filtering
6. Verify: DigitalReadout renders visible text (green on dark), StripChart renders traces, EventLogPanel scrolls

**Deliverables:** Fixed InstrumentFontHelper, StripChart.cs, EventLogPanel.cs
**Exit Criteria:** No black rectangles anywhere. Strip chart renders test data. Event log renders engine events.

### Stage 2: Overview Primary Surface — 5-Column Layout
**Objective:** Rebuild the Overview panel as the definitive primary operations surface.

**Tasks:**
1. Rewrite `OverviewPanel.cs` with 5-column + footer layout per Section 3
2. Build `OverviewColumn_RCS.cs` — all §4.2 parameters with ArcGauges for T_avg + Subcooling
3. Build `OverviewColumn_PZR.cs` — all §4.3 parameters with ArcGauges for Pressure + Level, LinearGauges for Heater/Spray, BidirectionalGauge for Surge
4. Build `OverviewColumn_CVCS.cs` — all §4.4 parameters with ArcGauge for VCT Level, BidirectionalGauge for Net CVCS
5. Build `OverviewColumn_SGRHR.cs` — all §4.5 parameters with ArcGauge for SG Pressure, LinearGauge for HZP Progress
6. Build `OverviewTrendsColumn.cs` — 8 MiniTrendStrip sparklines per §4.6
7. Build `OverviewFooter.cs` — split into annunciator panel (left, 27 tiles) + event log (right)
8. Wire all data from engine at 10Hz refresh rate
9. Verify: ALL parameters from §4.1-4.7 visible on 1920×1080 screen. No scrolling. No empty space.

**Deliverables:** Complete Overview primary surface
**Exit Criteria:** Parameter count ≥ 80 visible on Overview. All gauges render at readable size (≥80px). All digital readouts show values in instrument font. All StatusIndicators show correct state. 27 annunciator tiles visible. Event log scrolling with live entries.

### Stage 3: Detail Tabs — PRIMARY, PZR, CVCS, SG/RHR
**Objective:** Build the 4 system-specific detail tabs with hero gauges + stacked graphs.

**Tasks:**
1. Build `PrimaryDetailPanel.cs` — RCS detail with hero ArcGauges (T_avg, Subcool, Pressure), loop temperature readouts, RCP detail, 2× StripChart (TEMPS + PRESSURE)
2. Build `PZRDetailPanel.cs` — full PZR detail with hero gauges, bubble diagnostics, heater/spray control detail, 2× StripChart (PRESSURE + RATES)
3. Build `CVCSDetailPanel.cs` — CVCS flow breakdown (orifice/RHR paths), VCT detail, BRS detail, 2× StripChart (CVCS + VCT/BRS)
4. Build `SGRHRDetailPanel.cs` — SG secondary detail, RHR system, heat balance, 2× StripChart (SG data + HZP)
5. Each detail tab has: hero gauge row (2-3 large gauges), parameter grid below, 2× stacked strip charts on right half

**Deliverables:** 4 detail tab panels
**Exit Criteria:** Each tab has ≥2 hero arc gauges, ≥2 strip charts, complete parameter coverage for its system. Tab transitions are smooth fades.

### Stage 4: LOG Tab + GRAPHS Tab + VALIDATION Tab
**Objective:** Build remaining tabs for event monitoring, full trend access, and validation.

**Tasks:**
1. Build LOG tab — full 27-tile annunciator grid (top 35%) + full EventLogPanel (bottom 65%) with severity filter
2. Build GRAPHS tab — full-width tabbed strip charts for all 7 graph categories, matching legacy graph system. Tab bar: TEMPS | PRESSURE | CVCS | VCT/BRS | RATES | RCP HEAT | HZP
3. Build VALIDATION tab — RVLIS data panel, mass/energy conservation audit, PASS/FAIL check grid, debug telemetry
4. Wire ACK and RESET buttons for annunciator tiles (clickable)

**Deliverables:** LOG, GRAPHS, VALIDATION tabs
**Exit Criteria:** Event log shows all engine events with correct severity coloring. Graph tab renders all 7 categories. ACK button transitions ALERTING tiles to ACKNOWLEDGED. VALIDATION tab shows conservation data.

### Stage 5: Visual Polish + Animation + Keyboard Shortcuts
**Objective:** Make it look professional. Make it feel alive.

**Tasks:**
1. Verify all gauge animations are smooth (SmoothDamp at ≤100ms)
2. Verify all color transitions use Lerp (not instant swap)
3. Verify annunciator flash rates: 3Hz alerting, 0.7Hz clearing
4. Add tab fade transitions (150ms alpha)
5. Verify all keyboard shortcuts: F1 toggle, Ctrl+1-8 tabs, F5-F9 speed, +/- speed
6. Performance profile: verify < 2ms per frame at 10Hz data update
7. Test at 1080p and 1440p
8. Verify no console errors during 10-minute simulation run
9. Disable legacy `HeatupValidationVisual` (keep files, disable component)
10. Write changelog

**Deliverables:** Polished, animated, fully functional dashboard
**Exit Criteria:** Craig's assessment: "This looks like an
