# IP-0025 — Validation Dashboard uGUI Complete Redesign

**Date:** 2026-02-16  
**Status:** DRAFT — Awaiting Approval  
**Predecessor:** IP-0020 (CLOSED — FAILED)  
**Changelog Required:** No (per user instruction)
**Future Features Required:** No

---

## 1) Problem Summary

The current Validation Dashboard (`HeatupValidationVisual.*`) suffers from several fundamental issues:

1. **Critical parameters buried in tabs** — The dashboard has 8 tabs (Overview, PZR, CVCS, SG/RHR, RCP, LOG, VALID, CRITICAL). Key information requires tab-switching and scrolling, making it difficult to maintain situational awareness during dynamic heatup phases.

2. **OnGUI/IMGUI rendering system** — The entire dashboard uses Unity's legacy `OnGUI` immediate-mode system which:
   - Cannot support animations, transitions, or smooth visual effects
   - Requires manual GL line drawing for arc gauges (crude, non-anti-aliased)
   - Has no built-in layout system for responsive design
   - Requires extensive per-frame allocation guards and texture caching
   - Cannot use shaders for glow, pulse, or gradient effects

3. **Incomplete parameter coverage** — Many parameters from the simulation engine are not displayed at all, particularly: energy conservation tracking, per-loop data, spray system detail, letdown orifice states, RHR interlocks, and derived control sanity values.

4. **Poor visual design** — Flat IMGUI boxes with monochrome text. No visual hierarchy. No animated gauges. No strip charts. No depth or visual appeal. The dashboard reads like a debug console rather than a nuclear instrumentation system.

---

## 2) Expectations — What the Redesigned Dashboard Should Be

### 2.1 Visual Quality
- **Nuclear instrumentation aesthetic** — Dark background with glowing instruments, reminiscent of modern digital control room displays (Westinghouse Ovation/Emerson DCS)
- **Animated arc gauges** — Smooth needle movement with colored bands (green/amber/red), glow effects on alarm states, anti-aliased rendering via uGUI Image components with custom shaders or procedural meshes
- **Bi-directional gauges** — Center-zero gauges for surge flow, net CVCS, pressure rate (needle sweeps left for negative, right for positive)
- **Strip charts** — Real-time scrolling trend displays (replacing static GL-drawn graphs) with proper line rendering using `UILineRenderer` or `CanvasRenderer`
- **Animated transitions** — Fade/slide when switching between detail panels; pulse effects on alarm activation; smooth value transitions on digital readouts
- **Color-coded status indicators** — LED-style indicators for boolean states, pulsing red for active alarms, steady green for normal

### 2.2 Information Architecture
The primary screen must display ALL critical parameters at a glance on a single 1920×1080 screen with NO scrolling. Secondary detail is accessible via expandable panels or overlay tabs, but the primary view is self-sufficient.

### 2.3 Primary Screen Layout (Conceptual)

```
┌─────────────────────────────────────────────────────────────────────┐
│  HEADER: Mode │ Phase │ Sim/Wall Time │ Speed │ Alarms Summary      │
├─────────┬────────────┬──────────────┬────────────┬──────────────────┤
│         │            │              │            │                  │
│  RCS    │   PZR      │  CVCS/VCT    │  SG/RHR    │  TRENDS          │
│  PRIMARY│   DETAIL   │  FLOWS       │  SECONDARY │  (Always-on      │
│         │            │              │            │   strip charts)  │
│ T_avg   │ Pressure   │ Charging     │ SG Press   │                  │
│ T_hot   │ Level [G]  │ Letdown      │ SG Level   │ RCS Press ────── │
│ T_cold  │ T_pzr      │ Net CVCS     │ SG Temp    │ PZR Level ────── │
│ Core ΔT │ Heaters    │ Surge [BD]   │ Heat Xfer  │ T_avg ────────── │
│ Subcool │ Spray      │ VCT Level[G] │ RHR status │ Heatup Rate ──── │
│ Press   │ Bubble     │ BRS status   │ Steam Dump │ Subcooling ───── │
│ HeatRate│ P error    │ Mass Cons    │ Boiling    │ Net CVCS ─────── │
│ RCPs    │ L error    │              │            │                  │
│         │            │              │            │                  │
├─────────┴────────────┴──────────────┴────────────┼──────────────────┤
│  ALARM ANNUNCIATOR BAR (compact tiles)           │  CONSERVATION    │
│  [PZR HTR][BUBBLE][PRESS LO][SUBCOOL][VCT LO]…  │  Mass err │ E err│
└──────────────────────────────────────────────────┴──────────────────┘

[G] = Arc Gauge    [BD] = Bi-directional gauge    ──── = Strip chart trace
```

### 2.4 Parameter Coverage

The redesigned dashboard must display all parameters from the provided requirements list that are currently exposed by the engine. Parameters not yet modeled in the engine will be noted in Section 7 (Unaddressed Issues) for future implementation.

**Parameters mapped to engine state (AVAILABLE NOW):**

| Category | Parameter | Engine Field | Display Type |
|----------|-----------|-------------|--------------|
| **Sim Health** | Sim time | `simTime` | Digital |
| | Wall time | `wallClockTime` | Digital |
| | Sim rate/speed | `currentSpeedIndex` | Digital + indicator |
| | Running/paused | `isRunning` | LED indicator |
| | Mass conservation error | `massError_lbm` | Digital + strip chart |
| | VCT flow imbalance | `massConservationError` | Digital |
| | Primary ledger drift | `primaryMassDrift_lb` / `primaryMassDrift_pct` | Digital |
| | Total heat (RCP+heaters) | `effectiveRCPHeat` + `pzrHeaterPower` | Digital |
| | SG heat removal | `sgHeatTransfer_MW` | Digital |
| | RHR heat removal | `rhrHXRemoval_MW` | Digital |
| | Steam dump heat | `steamDumpHeat_MW` | Digital |
| | Net plant heat | `netPlantHeat_MW` | Digital + strip chart |
| **RCS Primary** | T_avg | `T_avg` | Arc gauge |
| | T_hot | `T_hot` | Digital + strip chart |
| | T_cold | `T_cold` | Digital + strip chart |
| | Core ΔT | `T_hot - T_cold` | Digital |
| | RCS Pressure | `pressure` | Arc gauge + strip chart |
| | RCS mass/inventory | `rcsWaterMass` | Digital |
| | Subcooling margin | `subcooling` | Arc gauge |
| | Heatup rate | `heatupRate` | Digital + strip chart |
| | Pressure rate | `pressureRate` | Digital + strip chart |
| | RCP status (×4) | `rcpRunning[0-3]` | LED array |
| | RCP count | `rcpCount` | Digital |
| | Effective RCP heat | `effectiveRCPHeat` | Digital |
| | RCP flow fraction | `rcpContribution.TotalFlowFraction` | Digital |
| | Natural circ (when RCPs off) | Derived from physics regime | Text indicator |
| **Pressurizer** | PZR pressure | `pressure` | Arc gauge |
| | PZR level (%) | `pzrLevel` | Arc gauge |
| | PZR liquid temp | `T_pzr` | Digital |
| | T_sat at pressure | `T_sat` | Digital |
| | PZR water volume | `pzrWaterVolume` | Digital |
| | PZR steam volume | `pzrSteamVolume` | Digital |
| | Heater mode | `currentHeaterMode` | Text + LED |
| | Heater power (kW) | `pzrHeaterPower` | Arc gauge or bar |
| | Heater PID output | `heaterPIDOutput` | Bar gauge |
| | Heater PID active | `heaterPIDActive` | LED indicator |
| | Spray active | `sprayActive` | LED indicator |
| | Spray flow (gpm) | `sprayFlow_GPM` | Digital |
| | Spray valve position | `sprayValvePosition` | Bar gauge |
| | Surge flow (signed) | `surgeFlow` | Bi-directional gauge |
| | Bubble state | `solidPressurizer` / `bubbleFormed` | Text + LED |
| | Bubble phase | `bubblePhase` | Text |
| | Pressure error (vs setpoint) | `solidPlantPressureError` | Digital |
| | Level error (vs setpoint) | `pzrLevel - pzrLevelSetpointDisplay` | Digital |
| | Level setpoint | `pzrLevelSetpointDisplay` | Digital |
| **CVCS** | Charging active | `chargingActive` | LED |
| | Charging flow | `chargingFlow` | Digital + strip chart |
| | Charging to RCS | `chargingToRCS` | Digital |
| | Total CCP output | `totalCCPOutput` | Digital |
| | Letdown active | `letdownActive` | LED |
| | Letdown flow | `letdownFlow` | Digital + strip chart |
| | Letdown path (RHR/orifice) | `letdownViaRHR` / `letdownViaOrifice` | Text |
| | Letdown isolated | `letdownIsolatedFlag` | LED alarm |
| | Orifice letdown flow | `orificeLetdownFlow` | Digital |
| | RHR letdown flow | `rhrLetdownFlow` | Digital |
| | Net CVCS flow | `chargingFlow - letdownFlow` | Bi-directional + strip chart |
| | Seal injection OK | `sealInjectionOK` | LED |
| | Divert fraction | `divertFraction` | Digital |
| **VCT** | VCT level (%) | `vctState.Level_percent` | Arc gauge |
| | VCT volume (gal) | `vctState.Volume_gal` | Digital |
| | VCT boron (ppm) | `vctState.BoronConcentration_ppm` | Digital |
| | VCT makeup active | `vctMakeupActive` | LED |
| | VCT divert active | `vctDivertActive` | LED |
| | RWST suction | `vctRWSTSuction` | LED alarm |
| | VCT level low/high | `vctLevelLow` / `vctLevelHigh` | LED alarm |
| **BRS** | BRS holdup level | Via `BRSPhysics.GetHoldupLevelPercent()` | Digital |
| | BRS inflow | `brsState.InFlow_gpm` | Digital |
| | BRS return flow | `brsState.ReturnFlow_gpm` | Digital |
| | BRS processing active | `brsState.ProcessingActive` | LED |
| | BRS distillate available | `brsState.DistillateAvailable_gal` | Digital |
| **RHR** | RHR active | `rhrActive` | LED |
| | RHR mode | `rhrModeString` | Text |
| | RHR net heat (MW) | `rhrNetHeat_MW` | Digital |
| | RHR HX removal (MW) | `rhrHXRemoval_MW` | Digital |
| | RHR pump heat (MW) | `rhrPumpHeat_MW` | Digital |
| **SG Secondary** | SG pressure (psia) | `sgSecondaryPressure_psia` | Arc gauge + strip chart |
| | SG sat temp | `sgSaturationTemp_F` | Digital |
| | SG bulk temp | `T_sg_secondary` | Digital |
| | SG superheat | `sgMaxSuperheat_F` | Digital |
| | SG heat transfer (MW) | `sgHeatTransfer_MW` | Digital |
| | SG boiling active | `sgBoilingActive` | LED |
| | SG boiling intensity | `sgBoilingIntensity` | Bar gauge |
| | SG N₂ isolated | `sgNitrogenIsolated` | LED |
| | SG wide-range level | `sgWideRangeLevel_pct` | Digital |
| | SG narrow-range level | `sgNarrowRangeLevel_pct` | Digital |
| | Steam dump active | `steamDumpActive` | LED |
| | Steam dump heat (MW) | `steamDumpHeat_MW` | Digital |
| | Steam pressure (psig) | `steamPressure_psig` | Digital |
| | SG boundary state | `sgStartupBoundaryState` | Text |
| **HZP** | HZP progress (%) | `hzpProgress` | Bar gauge |
| | HZP stable | `hzpStable` | LED |
| | HZP ready for startup | `hzpReadyForStartup` | LED |
| **Alarms** | All 26 annunciators | Existing `_cachedTiles[]` mapping | Compact tile bar |
| **RVLIS** | Dynamic/Full/Upper ranges | `rvlisDynamic` / `rvlisFull` / `rvlisUpper` | Digital with validity |
| **Trends** | 7 graph categories | Existing history buffers | Strip charts |
| **Validation** | PASS/FAIL checks | Existing logic | Status LEDs |

### 2.5 Technical Requirements

- **uGUI (Canvas-based)** — Replace all OnGUI/IMGUI rendering with Unity's Canvas/RectTransform UI system
- **No per-frame allocations** — Object pooling, cached strings, pre-built UI hierarchy
- **Update rate ≤ 10 Hz** — UI data binding refreshed at configurable rate (not every frame)
- **Ring buffer trend data** — Continue using existing 240-point history buffers from engine
- **1920×1080 primary resolution** — All critical parameters visible without scrolling
- **Keyboard shortcuts preserved** — F1 toggle, Ctrl+number for tab/panel switching, F5-F9 time acceleration

---

## 3) Proposed Architecture

### 3.1 Component Structure

```
ValidationDashboard (Canvas, ScreenSpace-Overlay)
├── HeaderPanel
│   ├── ModeIndicator
│   ├── PhaseText
│   ├── SimTimeDisplay
│   ├── WallTimeDisplay
│   ├── SpeedSelector (F5-F9)
│   └── AlarmSummaryBadge
├── MainContentPanel
│   ├── RCSPanel (left column)
│   ├── PZRPanel
│   ├── CVCSVCTPanel
│   ├── SGRHRPanel
│   └── TrendPanel (right column, always-on strip charts)
├── AlarmBar (bottom)
│   ├── AnnunciatorTileGrid (compact)
│   └── ConservationPanel
└── DetailOverlay (togglable panels for deep-dive data)
    ├── PZRDetailOverlay
    ├── CVCSDetailOverlay
    ├── SGDetailOverlay
    ├── ValidationOverlay
    └── EventLogOverlay
```

### 3.2 Custom UI Components (New Scripts)

| Component | Purpose | Rendering |
|-----------|---------|-----------|
| `ArcGauge` | Animated semicircular gauge with colored bands, needle, digital readout | Procedural mesh via `CanvasRenderer` or `UI.Extensions` `UICircle` |
| `BiDirectionalGauge` | Center-zero arc gauge (±range) | Extension of ArcGauge with center-zero mapping |
| `StripChart` | Real-time scrolling trend line with auto-scaling Y-axis | Custom `Graphic` with `SetVerticesDirty()` on data update |
| `LEDIndicator` | Circular/rectangular status light with on/off/alarm/pulse states | Image + Animator or shader |
| `BarGauge` | Horizontal fill bar with label, value, colored fill, setpoint marker | RectTransform + Image fill |
| `DigitalReadout` | Numeric display with unit, color-coded by threshold | Text (TMPro) with cached formatting |
| `AnnunciatorTile` | Compact alarm tile with background glow on active | Image + Text + animation |
| `DashboardBinder` | Singleton that reads engine state at configurable Hz and pushes to all UI components | MonoBehaviour with coroutine or InvokeRepeating |

### 3.3 Data Flow

```
HeatupSimEngine (physics thread)
    │
    ▼
DashboardBinder.UpdateData() — called at refreshRate Hz
    │  Reads all public engine fields into snapshot struct
    │  Performs derived calculations (Core ΔT, net CVCS, etc.)
    ▼
Per-component Update() — each UI component reads from binder snapshot
    │  Smooth animation interpolation (Lerp needle position, etc.)
    │  Threshold color evaluation
    ▼
Canvas renders at display framerate (vsync)
```

### 3.4 File Organization

```
Assets/Scripts/ValidationDashboard/
├── Core/
│   ├── DashboardBinder.cs          — Engine-to-UI data bridge
│   ├── DashboardManager.cs         — Canvas lifecycle, panel switching
│   ├── DashboardSnapshot.cs        — Struct holding all display-ready values
│   └── DashboardThresholds.cs      — All color threshold definitions
├── Components/
│   ├── ArcGauge.cs                 — Animated arc gauge
│   ├── BiDirectionalGauge.cs       — Center-zero gauge
│   ├── StripChart.cs               — Real-time trend line
│   ├── LEDIndicator.cs             — Boolean status light
│   ├── BarGauge.cs                 — Horizontal fill bar
│   ├── DigitalReadout.cs           — Numeric display
│   └── AnnunciatorTile.cs          — Compact alarm tile
├── Panels/
│   ├── HeaderPanel.cs              — Top bar (mode, phase, time, speed)
│   ├── RCSPanel.cs                 — RCS primary parameters
│   ├── PZRPanel.cs                 — Pressurizer detail
│   ├── CVCSPanel.cs                — CVCS/VCT/BRS
│   ├── SGRHRPanel.cs               — Steam generators & RHR
│   ├── TrendPanel.cs               — Always-on strip charts
│   ├── AlarmBarPanel.cs            — Bottom annunciator bar
│   └── ConservationPanel.cs        — Mass/energy conservation
├── Overlays/
│   ├── DetailOverlayBase.cs        — Base class for expandable overlays
│   ├── PZRDetailOverlay.cs         — Deep PZR data (closure diagnostics)
│   ├── CVCSDetailOverlay.cs        — Detailed CVCS flow breakdown
│   ├── ValidationOverlay.cs        — PASS/FAIL checks
│   └── EventLogOverlay.cs          — Filterable event log
└── Styles/
    ├── DashboardColors.cs          — Centralized color palette (ScriptableObject)
    └── DashboardAnimations.cs      — Animation curves and timing constants
```

---

## 4) Implementation Stages

### Stage 1: Foundation — Canvas Infrastructure + DashboardBinder
- Create the uGUI Canvas hierarchy (ScreenSpace-Overlay, 1920×1080 reference)
- Implement `DashboardBinder.cs` — reads engine state into `DashboardSnapshot` struct at configurable Hz
- Implement `DashboardSnapshot.cs` — all-value snapshot struct with derived calculations
- Implement `DashboardThresholds.cs` — centralized threshold definitions
- Implement `DashboardColors.cs` — ScriptableObject color palette matching current Westinghouse conventions
- Implement `DashboardManager.cs` — Canvas lifecycle, F1 toggle, keyboard bindings
- Create the 5-column + header + alarm bar layout skeleton with placeholder panels
- Verify: canvas appears on F1, data flows from engine, no performance regression

### Stage 2: Core Custom Components
- Implement `ArcGauge.cs` — procedural mesh arc with smooth needle animation (Lerp), colored bands, digital readout below, glow on alarm
- Implement `BiDirectionalGauge.cs` — extending ArcGauge for center-zero (surge flow, net CVCS, pressure rate)
- Implement `BarGauge.cs` — horizontal fill with setpoint marker, label, value text
- Implement `DigitalReadout.cs` — TextMeshPro numeric with threshold coloring, format string, unit text
- Implement `LEDIndicator.cs` — circular image with on/off/alarm states, optional pulse animation
- Implement `AnnunciatorTile.cs` — compact tile with background color change and border glow
- Verify: each component works standalone with test data, animations smooth at 60 FPS

### Stage 3: Strip Chart Component
- Implement `StripChart.cs` — custom `Graphic` that reads from ring buffers
- Multi-trace support (up to 6 traces per chart)
- Auto-scaling Y-axis with grid lines
- Rolling 4-hour window (matching existing 240-minute window)
- Reference lines (Tech Spec limits, setpoints)
- Compact legend with live values
- Verify: strip charts render correctly with engine data, performance acceptable

### Stage 4: Panel Assembly — Primary Screen
- Build `HeaderPanel` — mode indicator, phase text, sim/wall time, speed selector, alarm count badge
- Build `RCSPanel` — T_avg arc gauge, temperature readouts (T_hot/T_cold/ΔT), subcooling gauge, pressure digital, heatup rate, RCP status LEDs
- Build `PZRPanel` — pressure arc gauge, level arc gauge, T_pzr/T_sat readouts, heater bar, spray indicator, surge bidirectional gauge, bubble state, pressure/level error
- Build `CVCSPanel` — charging/letdown digitals, net CVCS bidirectional, VCT level arc gauge, VCT status LEDs, BRS summary, mass conservation
- Build `SGRHRPanel` — SG pressure arc gauge, SG temp/level digitals, heat transfer, boiling/N₂/steam dump indicators, RHR mode/heat
- Build `TrendPanel` — 5-6 always-visible strip charts (RCS pressure, PZR level, T_avg, heatup rate, subcooling, net CVCS)
- Build `AlarmBarPanel` — compact annunciator tile grid (26 tiles in single row or 2 rows)
- Build `ConservationPanel` — mass error, energy tracking, primary ledger drift
- Verify: all parameters visible at 1920×1080, no scrolling needed, all data updates correctly

### Stage 5: Detail Overlays
- Implement `DetailOverlayBase` — slide-in/fade-in panel triggered by clicking section header or keyboard shortcut
- `PZRDetailOverlay` — full bubble formation state machine, closure diagnostics, drain telemetry
- `CVCSDetailOverlay` — detailed flow breakdown (orifice/RHR paths, seal injection, PBOC telemetry)
- `ValidationOverlay` — all PASS/FAIL checks, RVLIS panel, inventory audit
- `EventLogOverlay` — full scrollable event log with severity filtering
- Verify: overlays open/close smoothly, don't obscure critical primary data when open

### Stage 6: Animation & Polish
- Add needle animation smoothing (Lerp/SmoothDamp for all gauges)
- Alarm pulse animations (LEDs flash at 1 Hz when alarming)
- Value color transitions (smooth fade between green→amber→red)
- Strip chart anti-aliased line rendering
- Glow effects on alarm gauges (via Image outline or shader)
- Panel highlight on hover/focus
- Verify: visual quality meets modern DCS aesthetics, no performance degradation

### Stage 7: Migration & Cleanup
- Wire up all keyboard shortcuts from existing dashboard (F1, Ctrl+1-8, F5-F9, +/-)
- Disable old `HeatupValidationVisual` OnGUI rendering (keep files for reference)
- Performance profiling — ensure new dashboard < 2ms per frame at 10 Hz update
- Test at different resolutions (1920×1080, 2560×1440, 3840×2160)
- Document new architecture in code headers
- Verify: complete functional parity with old dashboard + new features

---

## 5) Non-Negotiable Constraints

1. **No physics changes** — This is UI only. No modifications to `HeatupSimEngine` or any `Critical.Physics` module.
2. **No per-frame allocations** — All strings cached, all UI objects pre-instantiated, no LINQ in update loops.
3. **Update rate ≤ 10 Hz** — Data binding throttled. Canvas renders at display rate but data refresh is limited.
4. **Ring buffer trend data** — Continue using existing 240-point history buffers. Do not create new data structures in the engine.
5. **GOLD modules untouched** — All existing GOLD standard scripts remain unmodified.
6. **Existing engine fields only** — Dashboard reads only from existing public fields on `HeatupSimEngine`. No new fields added to the engine in this IP.
7. **Old dashboard preserved** — Old `HeatupValidationVisual.*` files are NOT deleted. They are disabled (component disabled or `dashboardVisible = false` forced) but kept for reference and rollback.
8. **Keyboard shortcuts preserved** — All existing hotkeys (F1, F5-F9, +/-, Ctrl+1-8) must work identically.

---

## 6) Risk Assessment

| Risk | Mitigation |
|------|-----------|
| Procedural mesh performance for arc gauges | Profile early in Stage 2. Fallback: use pre-baked sprite atlas with rotation for needle. |
| Strip chart performance with 240 points × 6 traces | Use `SetVerticesDirty()` only on data update (10 Hz), not every frame. Mesh generation amortized. |
| Canvas overdraw at 1920×1080 with many elements | Use Canvas batching, minimize overlapping transparent elements, profile with Frame Debugger. |
| TextMeshPro string allocation | Use `SetText()` with format args (zero-alloc path) or `StringBuilder` cache. |
| Migration risk (breaking existing workflow) | Stage 7 keeps old dashboard as fallback. Simple inspector toggle to switch between old/new. |

---

## 7) Parameters Not Currently Modeled

The following parameters from the requirements list are not currently exposed by `HeatupSimEngine` and will be **omitted from the dashboard** (displayed as N/A, greyed out, or excluded). The dashboard will only display parameters that have real backing data. No placeholder or dummy values.

**Pre-criticality / Power Operations (not applicable during heatup):**
Reactor power, decay heat, reactivity breakdown (rod worth, boron worth, MTC, Doppler), feedwater flow, turbine steam flow, SG blowdown.

**Multi-loop model (engine uses single equivalent loop):**
Per-loop A/B/C/D hot/cold leg temps, per-loop mass flow, per-loop SG primary inlet/outlet temps.

**RCP electrical detail (engine uses staged ramp model):**
RCP speed (%), torque/amps, pump head ΔP.

**CVCS/VCT detail not tracked:**
Charging pump individual status, charging temperature, charging/letdown line pressure, letdown orifice individual states (internal to CVCSController), letdown hot-side temperature, VCT temperature, VCT pressure/gas blanket, VCT NPSH.

**BRS chemistry:** BRS tank boron concentration (volumes tracked, not per-tank chemistry).

**RHR detail:** Individual isolation valve states, line pressure/ΔP, HX inlet/outlet temps individually, interlock/permissive status, minimum flow protection.

**Other:** Void fraction outside PZR, spray inlet temperature, surge line temperature/ΔP, heater group breakdown, heater/spray/RHR inhibit reason flags, energy conservation error as separate metric, integration stability flags (partial via pzrClosure* diagnostics).

**Derivable from existing fields (will be computed in DashboardSnapshot):**
Core ΔT, net CVCS flow, pressure error, level error, natural circulation indicator, approximate core flow.

---

## 8) Dependencies

- **TextMeshPro** — Required for high-quality text rendering (likely already in project)
- **Unity UI** — Standard uGUI package (built-in)
- **No external packages required** — All custom components built from scratch using uGUI primitives

---

## 9) Estimated Scope

| Stage | Estimated Files | Complexity |
|-------|----------------|------------|
| Stage 1: Foundation | 5 scripts + Canvas prefab | Medium |
| Stage 2: Core Components | 7 scripts | High (procedural mesh) |
| Stage 3: Strip Chart | 1 script | High (custom Graphic) |
| Stage 4: Panel Assembly | 8 scripts | Medium-High (layout) |
| Stage 5: Detail Overlays | 5 scripts | Medium |
| Stage 6: Animation & Polish | Modifications to existing | Medium |
| Stage 7: Migration & Cleanup | Modifications + testing | Low-Medium |

**Total new scripts: ~26**  
**Total implementation stages: 7**  
**Each stage to be completed and approved before proceeding to the next.**

---

## 10) Approval Request

This implementation plan is submitted for review. Please confirm:

1. Is the parameter coverage acceptable, or should any parameters be added/removed from the primary screen?
2. Is the 5-column layout concept appropriate, or do you prefer a different arrangement?
3. Should the old OnGUI dashboard be fully removed after migration, or kept as a toggle?
4. Any specific visual reference (screenshot, DCS product image) you'd like the new dashboard to match?

**Do not implement until explicitly instructed to proceed.**
