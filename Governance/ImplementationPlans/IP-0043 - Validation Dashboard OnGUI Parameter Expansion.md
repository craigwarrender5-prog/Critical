# IP-0043: Validation Dashboard — New OnGUI Dashboard

**Date:** 2026-02-17  
**Domain Plan:** DP-0008 (Operator Interface & Scenarios)  
**Predecessors:** IP-0042 (FAILED)  
**Status:** DRAFT — Awaiting Approval  
**Priority:** High  
**Changelog Required:** Yes  
**Target Version:** v0.8.0.0

---

## 1. Executive Summary

Following the failure of IP-0042 (UI Toolkit rebuild) due to Unity 6's `Painter2D.Arc()` method not rendering correctly, this IP creates a **brand new OnGUI-based dashboard** designed from the ground up with comprehensive parameter coverage.

The existing `HeatupValidationVisual` dashboard remains untouched as a fallback. The new dashboard will be a separate system that can coexist with or replace the legacy dashboard.

---

## 2. Design Goals

1. **Comprehensive parameter coverage** — Display all ~130 parameters from requirements
2. **Primary operations surface** — All critical parameters visible without tab switching
3. **Professional control room aesthetic** — Nuclear instrument look and feel
4. **Clean architecture** — Modular, maintainable, well-documented code
5. **GOLD standard from day one** — Follow all project quality criteria

---

## 3. Architecture

### 3.1 File Structure

```
Assets/Scripts/Validation/Dashboard/
├── ValidationDashboard.cs              — Main MonoBehaviour, lifecycle, input
├── ValidationDashboard.Layout.cs       — Screen layout calculations
├── ValidationDashboard.Styles.cs       — Colors, fonts, GUI styles
├── ValidationDashboard.Gauges.cs       — Arc gauge, bar gauge, LED rendering
├── ValidationDashboard.Panels.cs       — Panel/section rendering helpers
├── Tabs/
│   ├── DashboardTab.cs                 — Base class for tabs
│   ├── OverviewTab.cs                  — Primary operations surface
│   ├── RCSTab.cs                       — RCS Primary details
│   ├── PressurizerTab.cs               — Pressurizer details
│   ├── CVCSTab.cs                      — CVCS/VCT details
│   ├── SGRHRTab.cs                     — SG and RHR details
│   ├── SystemsTab.cs                   — BRS, Orifices, Mass Conservation
│   ├── GraphsTab.cs                    — Strip chart trends
│   └── LogTab.cs                       — Event log and annunciators
```

### 3.2 Class Responsibilities

| Class | Responsibility |
|-------|----------------|
| `ValidationDashboard` | MonoBehaviour entry point, Update loop, keyboard input, tab switching |
| `ValidationDashboard.Layout` | Screen regions, margins, column calculations |
| `ValidationDashboard.Styles` | GUIStyle cache, color palette, fonts |
| `ValidationDashboard.Gauges` | Reusable gauge drawing (arc, bar, LED, digital readout) |
| `ValidationDashboard.Panels` | Section headers, bordered panels, parameter rows |
| `DashboardTab` | Abstract base with Draw() method, engine reference |
| Individual tabs | Specific parameter layouts for each system |

---

## 4. Primary Operations Surface (Overview Tab)

The Overview tab displays ALL critical parameters on a single 1920×1080 screen with NO scrolling.

### 4.1 Layout

```
┌────────────────────────────────────────────────────────────────────────────────┐
│ HEADER: Mode │ Phase │ Sim Time │ Wall Time │ Speed Controls │ Alarm Count    │
├────────────┬────────────┬────────────┬────────────┬────────────────────────────┤
│ RCS        │ PRESSURIZER│ CVCS/VCT   │ SG/RHR     │ TRENDS (8 sparklines)     │
│ PRIMARY    │            │            │            │                            │
│            │            │            │            │ • RCS Pressure             │
│ [ARC]      │ [ARC]      │ Chg ────── │ [ARC]      │ • PZR Level                │
│ T_avg      │ Pressure   │ Ltd ────── │ SG Press   │ • T_avg                    │
│            │            │ Net ══════ │            │ • Heatup Rate              │
│ T_hot      │ [ARC]      │            │ SG Temps   │ • Subcooling               │
│ T_cold     │ Level      │ [ARC]      │ RHR MW     │ • Net CVCS                 │
│ ΔT         │            │ VCT Level  │            │ • SG Pressure              │
│            │ Heater kW  │            │ [BAR]      │ • Net Heat                 │
│ [ARC]      │ Spray %    │ Mass Err   │ HZP Prog   │                            │
│ Subcool    │ Surge ±    │            │            │                            │
│            │            │ BRS Level  │            │                            │
│ Heatup °/hr│ Bubble ●   │            │ HZP Rdy ●  │                            │
│ Press psi/m│            │            │            │                            │
│            │            │            │            │                            │
│ RCPs ●●●●  │            │            │            │                            │
├────────────┴────────────┴────────────┴────────────┼────────────────────────────┤
│ ANNUNCIATORS (27 tiles in 3 rows)                 │ EVENT LOG (last 8 events)  │
│ [HTR][HTUP][BUBL][MODE][PLO][PHI][SCLO][FLOW]    │ 12:15:30 Phase → HEATUP    │
│ [LLO][LHI][RVLIS][CCW][CHG][LTD][SEAL][VLO]     │ 12:15:35 PZR Level Low     │
│ [VHI][DVRT][MKUP][RWST][R1][R2][R3][R4]...      │ 12:16:01 Subcool < 20°F    │
│                                    [ACK] [RESET] │                            │
└──────────────────────────────────────────────────┴────────────────────────────┘
```

### 4.2 Column Widths

| Column | Width % | Content |
|--------|---------|---------|
| RCS | 18% | T_avg arc, subcool arc, temps, rates, RCPs |
| PZR | 16% | Pressure arc, level arc, heater, spray, surge, bubble |
| CVCS | 16% | VCT arc, charging/letdown bars, mass conservation |
| SG/RHR | 16% | SG pressure arc, HZP bar, RHR status |
| Trends | 24% | 8 mini sparkline graphs |
| Footer | 100% | Annunciators (60%) + Event Log (40%) |

---

## 5. Complete Parameter Inventory

### 5.1 Overview Tab (Always Visible)

**RCS Column (18 parameters):**
- T_avg (arc gauge)
- T_hot, T_cold, Core ΔT (digital)
- T_pzr, T_sat (digital)
- Subcooling (arc gauge)
- Heatup rate °F/hr (digital, colored)
- Pressure rate psi/min (digital, colored)
- RCP status ×4 (LEDs)
- RCP count, RCP heat MW (digital)

**Pressurizer Column (16 parameters):**
- Pressure (arc gauge)
- Level (arc gauge)
- Water volume, Steam volume (digital)
- Heater power kW (bar gauge)
- Heater mode (text)
- Spray active (LED)
- Spray flow gpm (digital)
- Spray valve % (bar gauge)
- Surge flow ±gpm (bidirectional bar)
- Bubble state (LED + text)
- Pressure error, Level error (digital)

**CVCS Column (14 parameters):**
- VCT Level (arc gauge)
- Charging flow, Letdown flow (bar gauges)
- Net CVCS (bidirectional bar)
- Charging active, Letdown active (LEDs)
- VCT Makeup, VCT Divert (LEDs)
- Mass error lbm (digital, alarmed)
- BRS Holdup % (digital)
- Seal injection OK (LED)

**SG/RHR Column (12 parameters):**
- SG Secondary Pressure (arc gauge)
- SG Sat Temp, SG Bulk Temp (digital)
- SG Heat Transfer MW (digital)
- SG Boiling (LED)
- Steam Dump Active (LED)
- RHR Active (LED)
- RHR Mode (text)
- RHR Net Heat MW (digital)
- HZP Progress (bar gauge)
- HZP Ready (LED)

**Trends Column (8 sparklines):**
- RCS Pressure
- PZR Level
- T_avg / T_hot / T_cold
- Heatup Rate
- Subcooling
- Net CVCS
- SG Pressure
- Net Plant Heat

**Footer — Annunciators (27 tiles):**
All existing annunciators from legacy dashboard

**Footer — Event Log (8 entries):**
Scrollable with severity colors

### 5.2 Detail Tabs

Each detail tab expands on its system with:
- Larger gauges
- Full parameter list
- Stacked strip charts
- Diagnostic information

---

## 6. Implementation Stages

### Stage 1: Core Infrastructure
**Objective:** Create base classes and rendering primitives.

**Tasks:**
1. Create `ValidationDashboard.cs` — MonoBehaviour shell
2. Create `ValidationDashboard.Layout.cs` — Screen region calculations
3. Create `ValidationDashboard.Styles.cs` — Color palette, GUIStyle factory
4. Create `ValidationDashboard.Gauges.cs` — Arc gauge, bar gauge, LED, digital readout
5. Create `ValidationDashboard.Panels.cs` — Section headers, bordered panels
6. Create `DashboardTab.cs` — Abstract base class
7. Test: Render a test pattern with each gauge type

**Deliverables:**
- Core partial classes
- Working gauge rendering

**Exit Criteria:** Can draw arc gauge, bar gauge, LED, and digital readout on screen.

---

### Stage 2: Overview Tab — Layout Framework
**Objective:** Build the 5-column + footer layout structure.

**Tasks:**
1. Create `OverviewTab.cs`
2. Implement 5-column layout with proper proportions
3. Implement footer split (annunciators left, log right)
4. Add placeholder content in each section
5. Verify layout at 1920×1080 and 2560×1440

**Deliverables:**
- `OverviewTab.cs` with complete layout

**Exit Criteria:** Layout renders correctly at both resolutions.

---

### Stage 3: Overview Tab — RCS and Pressurizer Columns
**Objective:** Populate RCS and PZR columns with live data.

**Tasks:**
1. RCS Column: T_avg arc, subcooling arc, all digital readouts, RCP LEDs
2. PZR Column: Pressure arc, level arc, heater bar, spray bar, surge bar, bubble LED
3. Wire all parameters to engine
4. Implement threshold coloring

**Deliverables:**
- Complete RCS column
- Complete PZR column

**Exit Criteria:** 34 parameters updating live with correct coloring.

---

### Stage 4: Overview Tab — CVCS, SG/RHR, and Trends Columns
**Objective:** Complete remaining columns.

**Tasks:**
1. CVCS Column: VCT arc, charging/letdown bars, net CVCS bar, LEDs, mass error
2. SG/RHR Column: SG pressure arc, HZP bar, RHR status, LEDs
3. Trends Column: 8 mini sparklines from engine history buffers
4. Wire all parameters to engine

**Deliverables:**
- Complete CVCS column
- Complete SG/RHR column
- Complete Trends column

**Exit Criteria:** All 5 columns populated with live data.

---

### Stage 5: Overview Tab — Footer (Annunciators + Event Log)
**Objective:** Complete the footer with annunciators and event log.

**Tasks:**
1. Implement 27-tile annunciator grid (3 rows × 9 columns)
2. Implement annunciator state machine (ISA-18.1)
3. Implement ACK and RESET buttons
4. Implement event log panel (last 8 entries)
5. Implement severity filtering buttons

**Deliverables:**
- Complete annunciator panel
- Complete event log

**Exit Criteria:** Annunciators flash correctly, ACK works, log scrolls.

---

### Stage 6: Detail Tabs (RCS, PZR, CVCS, SG/RHR)
**Objective:** Create system detail tabs.

**Tasks:**
1. Create `RCSTab.cs` — expanded RCS detail + TEMPS/PRESSURE graphs
2. Create `PressurizerTab.cs` — expanded PZR detail + PRESSURE/RATES graphs
3. Create `CVCSTab.cs` — expanded CVCS/VCT detail + CVCS/VCT graphs
4. Create `SGRHRTab.cs` — expanded SG/RHR detail + SG/HZP graphs
5. Implement tab switching (keyboard Ctrl+1-4)

**Deliverables:**
- 4 detail tab classes

**Exit Criteria:** Each tab accessible and displays complete system data.

---

### Stage 7: Systems and Graphs Tabs
**Objective:** Create auxiliary tabs.

**Tasks:**
1. Create `SystemsTab.cs` — BRS detail, Orifice status, Mass conservation audit
2. Create `GraphsTab.cs` — Full-width tabbed strip charts (7 categories)
3. Create `LogTab.cs` — Full annunciator grid + expanded event log
4. Implement keyboard shortcuts (Ctrl+5-7)

**Deliverables:**
- 3 auxiliary tab classes

**Exit Criteria:** All tabs accessible and functional.

---

### Stage 8: Polish and Integration
**Objective:** Final polish and switchover.

**Tasks:**
1. Implement all keyboard shortcuts (F1 toggle, Ctrl+1-8 tabs, F5-F9 speed)
2. Add header with mode/phase/time display
3. Performance optimization pass
4. Test at multiple resolutions
5. Add toggle in ScreenManager to switch between old/new dashboard
6. Write changelog

**Deliverables:**
- Complete, polished dashboard
- ScreenManager integration
- CHANGELOG_v0.8.0.0.md

**Exit Criteria:** Dashboard is production-ready, can replace legacy system.

---

## 7. Technical Specifications

### 7.1 Color Palette

| Use | Color | Hex |
|-----|-------|-----|
| Background | Dark blue-black | #0A0A12 |
| Panel | Dark panel | #141420 |
| Normal | Bright green | #00FF88 |
| Warning | Amber | #FFAA00 |
| Alarm | Red | #FF4444 |
| Info | Cyan | #66CCFF |
| Text | Light gray | #CCCCCC |
| Dim | Dim gray | #666666 |

### 7.2 Gauge Specifications

**Arc Gauge:**
- 180° sweep (9 o'clock to 3 o'clock)
- Configurable min/max
- Threshold color bands
- Animated needle (SmoothDamp)
- Digital value display below

**Bar Gauge:**
- Horizontal or vertical
- Configurable min/max
- Fill color based on thresholds
- Optional setpoint marker

**Bidirectional Bar:**
- Center-zero reference
- Positive/negative fill colors
- Value label

**LED Indicator:**
- On/Off/Warning/Alarm states
- Optional flash for alerting
- Label above or beside

**Digital Readout:**
- Configurable format string
- Threshold-based coloring
- Unit suffix

### 7.3 Performance Targets

- Update rate: 10 Hz
- Frame budget: < 2ms for OnGUI
- Memory: No per-frame allocations (cached styles, pre-formatted strings)

---

## 8. File Manifest

### New Files

| File | Purpose |
|------|---------|
| `ValidationDashboard.cs` | Main MonoBehaviour |
| `ValidationDashboard.Layout.cs` | Layout calculations |
| `ValidationDashboard.Styles.cs` | GUI styles and colors |
| `ValidationDashboard.Gauges.cs` | Gauge rendering |
| `ValidationDashboard.Panels.cs` | Panel helpers |
| `Tabs/DashboardTab.cs` | Tab base class |
| `Tabs/OverviewTab.cs` | Primary surface |
| `Tabs/RCSTab.cs` | RCS detail |
| `Tabs/PressurizerTab.cs` | PZR detail |
| `Tabs/CVCSTab.cs` | CVCS detail |
| `Tabs/SGRHRTab.cs` | SG/RHR detail |
| `Tabs/SystemsTab.cs` | Auxiliary systems |
| `Tabs/GraphsTab.cs` | Strip charts |
| `Tabs/LogTab.cs` | Event log |

### Modified Files

| File | Changes |
|------|---------|
| `ScreenManager.cs` | Add new dashboard toggle |

---

## 9. Success Criteria

- [ ] Overview tab displays 60+ parameters without scrolling
- [ ] All 8 tabs accessible and functional
- [ ] 27 annunciator tiles with correct ISA-18.1 behavior
- [ ] 8 sparkline trends on Overview
- [ ] Full strip charts on Graphs tab
- [ ] Event log with severity filtering
- [ ] All keyboard shortcuts working
- [ ] Performance < 2ms per frame
- [ ] Works at 1080p and 1440p
- [ ] Can toggle between old/new dashboard

---

## 10. Approval

- [ ] **IP-0043 approved to begin** — Craig
- [ ] **Stage 1 complete (Core Infrastructure)** — Craig
- [ ] **Stage 2 complete (Layout Framework)** — Craig
- [ ] **Stage 3 complete (RCS/PZR Columns)** — Craig
- [ ] **Stage 4 complete (CVCS/SG/Trends)** — Craig
- [ ] **Stage 5 complete (Footer)** — Craig
- [ ] **Stage 6 complete (Detail Tabs)** — Craig
- [ ] **Stage 7 complete (Aux Tabs)** — Craig
- [ ] **Stage 8 complete (Polish)** — Craig
- [ ] **IP-0043 closed** — Craig

---

*Implementation Plan prepared by Claude*  
*Date: 2026-02-17*
