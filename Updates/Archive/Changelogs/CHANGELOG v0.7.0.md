# CRITICAL: Master the Atom — Changelog

## [0.7.0] — 2026-02-07

### Overview
Complete redesign of the Heatup Validation Dashboard from a monolithic single-file
UI into a 6-partial-class architecture with Westinghouse-authentic control room
styling. The previous placeholder dashboard is replaced with a full-featured
monitoring interface: 22 arc/bar gauges, 6 tabbed trend graphs (GL line rendering),
6 status panels, 26 annunciator tiles, and a scrollable operations event log.

**Version type:** Minor (new UI subsystem, zero physics changes)
**Previous version:** 0.6.0 (BRS closed-loop inventory)

---

### Problem Statement

The v0.6.0 dashboard was a functional but rudimentary single-file OnGUI
implementation lacking:

- Proper proportional layout (hard-coded pixel positions)
- Gauge rendering (raw text labels only, no arc/bar instruments)
- Time-series trend graphs (no GL rendering, no history visualisation)
- Annunciator tile matrix (alarm states as text list)
- Westinghouse control room color conventions
- GOLD standard compliance (single 2000+ line file, violated G8 size limit)

---

### Architecture

Six partial class files under `Assets/Scripts/Validation/`:

| File | Responsibility | Size |
|------|---------------|------|
| `HeatupValidationVisual.cs` | Core: layout, lifecycle, dispatch | 20 KB |
| `HeatupValidationVisual.Styles.cs` | Colors, fonts, GUIStyles, GL primitives | 24 KB |
| `HeatupValidationVisual.Gauges.cs` | 22 arc/bar gauge instruments | 21.5 KB |
| `HeatupValidationVisual.Panels.cs` | 6 status information panels | 18.6 KB |
| `HeatupValidationVisual.Graphs.cs` | 6 tabbed trend graph renderer | 27.4 KB |
| `HeatupValidationVisual.Annunciators.cs` | 26 alarm tiles + event log | 8.5 KB |

Layout (proportional, resolution-independent):
- Header 6%: Mode, Phase, Sim/Wall Time, Rate, Subcooling, Time Warp
- Main 62%: Gauges (22%) | Graphs (48%) | Status Panels (30%)
- Footer 32%: Annunciator Tiles (45%) | Event Log (55%)

---

### New / Rewritten Files

**HeatupValidationVisual.cs** — Core scaffold. Proportional 3-column layout,
header bar with plant mode color-coding, scroll state management, keyboard
shortcuts (F1 toggle, 1-5 time warp), `GetThresholdColor()` utilities.
All drawing dispatched to partials via `partial void` methods.

**HeatupValidationVisual.Styles.cs** — 26-color Westinghouse palette, 16 GUIStyles,
10 cached Texture2D, GL primitives (`DrawArcSegment`, `DrawLine`, `DrawFilledRect`),
`DrawGaugeArc()` half-circle renderer, `DrawMiniBar()`, `DrawSectionHeader()`,
`DrawStatusRow()` overloads. All sizing constants for gauges, tiles, status rows.

**HeatupValidationVisual.Gauges.cs** — 22 instruments in 6 groups (TEMPERATURES,
PRESSURIZER, CVCS FLOWS, VCT, BRS, RCP/HEAT). Arc gauges for primary parameters,
mini-bars for secondary. Color thresholds from Tech Spec limits.

**HeatupValidationVisual.Panels.cs** — 6 panels: Plant Overview (mode/phase/timing),
RCP Staged Ramp Grid (per-pump OFF/RAMPING/RATED + aggregate α), Bubble Formation
State Machine (7-phase tracker with progress), Heater Mode (5 modes + interlock),
RVLIS (3 ranges with validity), System Inventory Conservation (component volumes +
error tracking).

**HeatupValidationVisual.Graphs.cs** — 6 tabs: TEMPS (T_rcs/hot/cold + live T_pzr/T_sat),
PRESSURE (dual-axis: psia + PZR level %), CVCS (charging/letdown/surge + zero ref),
VCT/BRS (dual-axis: VCT% + BRS gal + setpoint bands), RATES (dual-axis: heatup rate +
subcooling + Tech Spec 50°F/hr limit), RCP HEAT (rate + live RCP status overlay).
Uses 14 engine history buffers (144-point rolling, 5 sim-min samples). GL LINE_STRIP
rendering with auto-range Y-axis, grid lines, legends with live values.

**HeatupValidationVisual.Annunciators.cs** — 26 tiles in 7 rows mapped to engine
boolean alarm fields. Color logic: dark=off, green=normal active, amber=warning,
red=alarm. Event log: scrollable, color-coded by severity (INFO/ACTION/ALERT/ALARM),
auto-scroll to newest, `[HH:MM:SS] SEV | Message` format.

---

### Engine State Dependencies (Read-Only)

All 6 partials are read-only consumers of `HeatupSimEngine` public state.
No visual file writes to or modifies engine state.

Verified references across all partials:
- 14 history buffers (tempHistory through brsDistillateHistory)
- 22 boolean annunciator fields
- RCPContribution struct, BubbleFormationPhase enum, HeaterMode enum
- BRSPhysics.GetHoldupLevelPercent(), engine.GetModeColor(), TimeAcceleration.FormatTime()
- PlantConstants: VCT_LEVEL_HIGH/LOW, HEATER_STARTUP_MAX_PRESSURE_RATE, PZR_LEVEL_AFTER_BUBBLE, MIN_RCP_PRESSURE_PSIG, BRS_HOLDUP_* constants, bubble phase duration constants

---

### Files Not Changed

All physics modules, engine partials, and PlantConstants files are unchanged.
This is a UI-only release.

---

### GOLD Certification

| File | G1 | G2 | G3 | G4 | G5 | G6 | G7 | G8 | G9 | G10 |
|------|----|----|----|----|----|----|----|----|----|----|
| Core | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ 20KB | ✅ | N/A |
| Styles | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ 24KB | ✅ | N/A |
| Gauges | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ 21.5KB | ✅ | N/A |
| Panels | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ 18.6KB | ✅ | N/A |
| Graphs | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ 27.4KB | ✅ | N/A |
| Annunciators | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ 8.5KB | ✅ | N/A |

G10: UI rendering partials verified by engine field cross-reference rather than
unit tests. All field accesses confirmed against HeatupSimEngine.cs declarations.
