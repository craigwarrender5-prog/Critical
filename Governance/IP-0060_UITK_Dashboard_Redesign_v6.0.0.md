# IP-0060: UI Toolkit Validation Dashboard — Complete Redesign

**Version:** 6.0.0  
**Date:** 2026-02-18  
**Status:** AWAITING APPROVAL  
**Scope:** Full-screen UI Toolkit dashboard replacement for HeatupValidationVisual OnGUI  

---

## 1. Problem Summary

The current `HeatupValidationVisual` dashboard (OnGUI-based) served well during early development but has several limitations:

1. **Crowded layout** — parameters squeezed into small areas; the dashboard doesn't use available screen space effectively
2. **Stale component library** — the simulator now has hundreds of parameters (SG multi-node, condenser, feedwater, HZP, spray, permissives, mass ledger, etc.) that have no dashboard representation
3. **Static appearance** — no animated gauges, no visual "life" that makes a control room feel alive
4. **Two dashboard systems** — OnGUI `HeatupValidationVisual` and the newer UI Toolkit `UITKDashboardController` coexist, creating confusion

The new UI Toolkit POC work has proven that **stunning, professional components** are achievable: ArcGauges, AnalogGauges, DigitalReadouts, LEDIndicators, EdgewiseMeters, AnnunciatorPanels, TankLevels, PressurizerVessels, RCSLoopDiagrams, StripCharts, BidirectionalGauges, LinearGauges, and RotarySwitches are all built and working.

## 2. Expectations — What the Dashboard Should Be

A **living, breathing nuclear control room** validation dashboard that:

- Uses the **full screen** — generous spacing, nothing cramped
- Displays **every major simulation parameter** organized by system
- Feels **alive** — needles sweep, LEDs pulse, strip charts scroll, tanks fill
- Looks **professional** — matches the dark industrial aesthetic of real control rooms
- Is **tab-navigated** with 8 purpose-built screens, each maximizing its space
- Provides instant **at-a-glance** situational awareness on the CRITICAL tab
- Uses all available UI Toolkit elements created during POC work

## 3. Proposed Design — 8-Tab Dashboard Architecture

### Tab Layout (Full 1920×1080 utilization)

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ CRITICAL: MASTER THE ATOM                    00:45:22  MODE 4  SPEED 4x RT  │
├────┬────┬──────────┬──────┬─────────┬───────────┬────────┬──────────────────┤
│CRIT│ RCS│PRESSURIZER│ CVCS │ SG/RHR  │ CONDENSER │ TRENDS │      LOG        │
├────┴────┴──────────┴──────┴─────────┴───────────┴────────┴──────────────────┤
│                                                                              │
│                        TAB CONTENT AREA                                      │
│                        (Full remaining height)                               │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

### TAB 0: CRITICAL — Plant Overview At-a-Glance

**Philosophy:** A nuclear operator's "Core Flight Deck" — everything needed for instant situational awareness.

```
┌─────────────────────────────────────────────┬──────────────────────────────────┐
│              CORE FLIGHT DECK               │        ANNUNCIATOR WALL          │
│                                             │  ┌───┬───┬───┬───┬───┬───┬───┐  │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐      │  │PZR│HEA│STM│MOD│PRS│PRS│SUB│  │
│  │ ◎ T_AVG │ │ ◎ RCS P │ │ ◎ PZR % │      │  │HTR│TUP│BUB│PER│ LO│ HI│COL│  │
│  │  120.0°F│ │  115psia│ │ 100.0%  │      │  ├───┼───┼───┼───┼───┼───┼───┤  │
│  └─────────┘ └─────────┘ └─────────┘      │  │RCS│CON│CHG│LTD│SEL│RCS│VCT│  │
│                                             │  │FLO│RUN│ACT│ACT│INJ│FLO│ LO│  │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐      │  ├───┼───┼───┼───┼───┼───┼───┤  │
│  │ ◎ PZR T │ │ ◎SUBCOOL│ │ ◎ SG P  │      │  │RCP│RCP│RCP│RCP│SMM│SMM│SG │  │
│  │  120.0°F│ │ 217.3°F │ │  0 psia │      │  │ A │ B │ C │ D │ LO│ NO│PRS│  │
│  └─────────┘ └─────────┘ └─────────┘      │  └───┴───┴───┴───┴───┴───┴───┘  │
│                                             │  [INFO] [WARNING] [ALARM]       │
│  ┌─────────────────────────────────────┐   │                                  │
│  │ [RCP-A ●] [RCP-B ○] [RCP-C ○]      │   │  4 active / 0 alarm             │
│  │ [RCP-D ○]                           │   │                                  │
│  │ Plant Mode: BULK HEATUP             │   │                                  │
│  │ Heatup Phase: ──────────            │   │                                  │
│  │ PZR State: ─────                    │   │                                  │
│  │ Steam Dump: ─────                   │   │                                  │
│  └─────────────────────────────────────┘   │                                  │
├─────────────┬───────────────┬──────────────┤──────────────────────────────────┤
│ STRIP TRENDS│ PROCESS METERS│  OPS LOG     │                                  │
│ ┌─────────┐ │ ┌────────┐   │              │                                  │
│ │ T_avg   │ │ │PRESS RT│   │ 1 active     │                                  │
│ │ P_rcs   │ │ │HEATUP  │   │ alarm(s)     │                                  │
│ │ PZR %   │ │ │SUBCOOL │   │              │                                  │
│ └─────────┘ │ └────────┘   │ MODE PERM... │                                  │
│             │               │              │                                  │
│             │ ┌─VCT──┐     │ [log entries] │                                  │
│             │ │██████│     │              │                                  │
│             │ │██████│     │              │                                  │
│             │ └──────┘     │              │                                  │
│             │ ┌─HOTWELL─┐  │              │                                  │
│             │ │████████│  │              │                                  │
│             │ └─────────┘  │              │                                  │
│             │               │              │                                  │
│             │ NET CVCS ◄──►│              │                                  │
│             │               │              │                                  │
│             │ ═══════════  │              │                                  │
│             │ ═══════════  │              │                                  │
│             │ ═══════════  │              │                                  │
│             │ ═══════════  │              │                                  │
└─────────────┴───────────────┴──────────────┘──────────────────────────────────┘
```

**Components used:**
- 6× AnalogGaugeElement (T_avg, RCS Pressure, PZR Level, PZR Temp, Subcooling, SG Pressure)
- 4× LEDIndicatorElement (RCP A-D running status)
- 3× EdgewiseMeterElement (Pressure Rate, Heatup Rate, Subcooling)
- 1× AnnunciatorPanelElement (3×7 = 21 tiles)
- 1× StripChartPOC (3 traces: T_avg, Pressure, PZR Level)
- 2× TankLevelPOC (VCT Level, Hotwell Level)
- 1× BidirectionalGaugePOC (Net CVCS flow)
- 5× LinearGaugePOC (SG Heat, Charging, Letdown, HZP Progress, Mass Conservation)
- Status text region (Plant Mode, Heatup Phase, PZR State, Steam Dump state)
- ScrollView event log (last 20 entries)

---

### TAB 1: RCS — Primary Loop Detail

**Full-width RCS loop schematic** with live data overlays.

**Components:**
- 1× RCSLoopDiagramPOC (center, large — full animated 4-loop schematic)
- 4× DigitalReadoutElement (T_hot, T_cold, T_avg, Core ΔT)
- 4× ArcGaugeElement (RCS Pressure, PZR Level, Subcooling, Heatup Rate)
- 4× LEDIndicatorElement (RCP A-D with flow fraction display)
- 1× StripChartPOC (T_hot, T_cold, T_avg, T_pzr multi-trace)
- Regime indicator panel showing current physics regime (1/2/3) with coupling alpha
- RCS mass conservation display (ledger vs components)

---

### TAB 2: PRESSURIZER — Detailed PZR Operations

**Full pressurizer cutaway graphic** with level, heater status, spray, and bubble state.

**Components:**
- 1× PressurizerVesselPOC (center, large — animated water/steam level, heater glow)
- 3× ArcGaugeElement (PZR Pressure, PZR Temperature, PZR Level)
- 1× ArcGaugeElement (Spray Flow)
- 2× EdgewiseMeterElement (Pressure Rate, PZR Heat Rate)
- 1× DigitalReadoutElement (Surge Line Flow)
- 6× LEDIndicatorElement (Heater banks ON/OFF, Spray Active, Bubble Formed, Solid PZR, Startup Hold)
- Heater authority panel (mode, state, limiter reason, limiter detail)
- Bubble formation state machine display (7-phase indicator)
- 1× StripChartPOC (Pressure, T_pzr, T_sat, PZR Level)
- PZR closure solver telemetry (convergence %, iteration count, bracket search)

---

### TAB 3: CVCS — Chemical & Volume Control

**Full CVCS flow diagram** with charging, letdown, VCT, BRS paths.

**Components:**
- 2× ArcGaugeElement (Charging Flow, Letdown Flow)
- 1× BidirectionalGaugePOC (Net CVCS to primary)
- 2× TankLevelPOC (VCT Level, BRS Holdup)
- 4× DigitalReadoutElement (Seal Injection, Seal Return, Orifice Lineup, Divert Fraction)
- 3× LEDIndicatorElement (Letdown Path: RHR/Orifice, VCT Divert Active, VCT Makeup Active)
- 1× StripChartPOC (Charging, Letdown, VCT Level, Surge Flow)
- PZR Level setpoint display with dynamic tracking
- Mass conservation panel (Primary mass ledger, drift %, component sum, status)
- CVCS thermal mixing display (MW, ΔF)
- Orifice lineup visual (1×75, 2×75, 1×45 indicators)

---

### TAB 4: SG / RHR — Steam Generators & Residual Heat Removal

**4-SG overview** with secondary pressure, levels, draining, and RHR status.

**Components:**
- 4× ArcGaugeElement (SG Pressure, SG Top Node Temp, SG Bottom Node Temp, SG Heat Transfer)
- 2× DigitalReadoutElement (SG Stratification ΔT, SG Thermocline Height)
- 3× LEDIndicatorElement (SG Boiling Active, SG Nitrogen Isolated, SG Draining Active)
- 1× TankLevelPOC (SG Secondary Level — wide range and narrow range)
- SG Boundary state display (OPEN_PREHEAT / PRESSURIZE / HOLD / ISOLATED_HEATUP)
- SG multi-node thermal model display (thermocline position graphic)
- RHR section:
  - 2× ArcGaugeElement (RHR Net Heat, RHR HX Removal)
  - 2× LEDIndicatorElement (RHR Active, RHR Pumps)
  - RHR mode display string
- 1× StripChartPOC (SG Pressure, SG Top Node, SG Bottom Node, T_rcs)

---

### TAB 5: CONDENSER — Condenser/Feedwater/Permissives/Steam Dump

**Full secondary systems overview.**

**Components:**
- 2× ArcGaugeElement (Condenser Vacuum, Hotwell Level)
- 1× DigitalReadoutElement (Condenser Backpressure)
- 3× LEDIndicatorElement (C-9 Available, P-12 Bypass, Steam Dump Permitted)
- 1× TankLevelPOC (CST Level)
- Steam Dump section:
  - 2× ArcGaugeElement (Steam Dump Heat, Steam Pressure)
  - 1× DigitalReadoutElement (Steam Dump Mode)
  - 1× LEDIndicatorElement (Steam Dump Active)
- HZP Stabilization section:
  - 1× ArcGaugeElement (HZP Progress)
  - 3× LEDIndicatorElement (HZP Stable, HZP Ready, Heater PID Active)
  - 1× DigitalReadoutElement (Net Plant Heat MW)
- Permissive status panel (Bridge FSM state, individual permissive checks)
- 1× StripChartPOC (Condenser Vacuum, Steam Dump Heat, Hotwell Level)

---

### TAB 6: TRENDS — Full-Screen Multi-Trace Strip Charts

**Maximum data visualization** — 4 large strip charts stacked with full history.

**Components:**
- Chart 1: **Temperatures** (T_avg, T_hot, T_cold, T_pzr, T_sat, T_sg_secondary)
- Chart 2: **Pressures** (RCS Pressure, SG Pressure, Pressure Rate)
- Chart 3: **Levels & Flows** (PZR Level, VCT Level, Charging, Letdown, Surge Flow)
- Chart 4: **Thermal Balance** (RCP Heat, SG Heat Transfer, RHR Net, Steam Dump Heat, Net Plant Heat)
- Each chart: 1× StripChartPOC with full-width, ~200px height
- Time axis synced across all 4 charts
- Legend with color-coded trace labels

---

### TAB 7: LOG — Full Event Log

**Scrollable operations event log** with filtering and severity coloring.

**Components:**
- Full-screen ScrollView with monospaced event entries
- Color-coded by severity: INFO (cyan), ACTION (green), ALERT (amber), ALARM (red)
- Filter buttons at top: [ALL] [INFO] [ACTION] [ALERT] [ALARM]
- Auto-scroll with manual override
- Timestamp + severity + message format
- Last N events from engine's eventLog buffer

---

## 4. Implementation Stages

### Stage 1: Foundation — Layout Shell, Header, Tab Bar, Theme (New controller file)
- Create `UITKDashboardV2Controller.cs` — new top-level controller
- Build full-screen VisualElement layout: header, tab bar, content container
- Wire tab switching, keyboard shortcuts (Ctrl+1-8)
- Header shows: sim time, plant mode label (with mode color), speed multiplier
- Dark industrial theme matching UITKDashboardTheme colors
- Wire to HeatupSimEngine for data binding at 5Hz

### Stage 2: CRITICAL Tab — Core Flight Deck (Upper Half)
- Build upper section: 6× AnalogGauge cluster + Annunciator Wall
- RCP LED status row with plant state text
- Wire all 6 gauges to engine data
- Wire all annunciator tiles to engine boolean states
- Status information panel (Plant Mode, Heatup Phase, PZR State, Steam Dump)

### Stage 3: CRITICAL Tab — Lower Half (Trends, Process, Log)
- StripChart with 3 traces (T_avg, Pressure, PZR Level)
- Process Meter Deck: EdgewiseMeters + VCT/Hotwell tanks + CVCS net gauge
- Linear gauges for system health bars
- Compact operations log (last 20 entries)

### Stage 4: RCS Tab — Primary Loop Detail
- RCSLoopDiagramPOC center graphic
- Temperature readouts with ArcGauges
- RCP status with flow fraction indicators
- Physics regime display

### Stage 5: PRESSURIZER Tab
- PressurizerVesselPOC center graphic
- PZR gauges, spray, heater authority display
- Bubble formation state machine visual
- Closure solver telemetry

### Stage 6: CVCS Tab
- Flow gauges, VCT/BRS tank levels
- Orifice lineup visual, mass conservation panel
- CVCS thermal mixing display

### Stage 7: SG/RHR Tab
- SG multi-node display, secondary pressure/level
- RHR section with heat balance
- SG boundary state machine

### Stage 8: CONDENSER Tab + TRENDS Tab + LOG Tab
- Condenser vacuum dynamics display
- Steam dump and HZP stabilization
- Full-screen trends (4 strip charts)
- Full-screen filtered event log

## 5. Unaddressed Issues

| Issue | Reasoning |
|-------|-----------|
| Animated flow arrows on RCS loop diagram | Planned for future visual polish pass |
| Sound effects for annunciator alarms | Future feature — requires audio system |
| Dynamic gauge range adjustment | Future feature — requires operator interaction system |
| Scenario selector integration | Exists separately, dashboard links not in scope |
| In-game F1 help overlay | Tracked in Future_Features, separate IP |

## 6. Technical Notes

- **No GOLD modules modified** — this is entirely new UI code
- **Engine is read-only** — dashboard reads public state, never modifies engine
- **5Hz data refresh** — gauge animation interpolation runs at Update() framerate
- **Painter2D for vector rendering** — all arc gauges, strip charts use GPU-accelerated paths
- **Namespace:** `Critical.UI.UIToolkit.ValidationDashboard` (existing)
- **Elements reused from POC:** All existing `*Element.cs` and `*POC.cs` components
- **File size policy:** Each tab builder is a separate partial class file to stay under GOLD limits

## 7. Files Created/Modified

### New Files:
- `UITKDashboardV2Controller.cs` — Main controller (lifecycle, data binding, tab switching)
- `UITKDashboardV2Controller.CriticalTab.cs` — CRITICAL tab layout builder
- `UITKDashboardV2Controller.CriticalTab.Data.cs` — CRITICAL tab data binding
- `UITKDashboardV2Controller.RCSTab.cs` — RCS tab
- `UITKDashboardV2Controller.PressurizerTab.cs` — Pressurizer tab
- `UITKDashboardV2Controller.CVCSTab.cs` — CVCS tab
- `UITKDashboardV2Controller.SGRHRTab.cs` — SG/RHR tab
- `UITKDashboardV2Controller.CondenserTab.cs` — Condenser tab
- `UITKDashboardV2Controller.TrendsTab.cs` — Trends tab
- `UITKDashboardV2Controller.LogTab.cs` — Log tab

### Modified Files:
- None — existing dashboard code remains untouched for fallback

### Location:
`Assets/Scripts/UI/UIToolkit/ValidationDashboard/`
