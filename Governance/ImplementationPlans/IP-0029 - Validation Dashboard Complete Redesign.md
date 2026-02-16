# IP-0029 — Validation Dashboard Complete Redesign (uGUI)

**Date:** 2026-02-16  
**Status:** DRAFT — Awaiting Approval  
**Predecessor:** IP-0025 (DRAFT — Superseded by this IP)  
**Changelog Required:** No  
**Future Features Required:** No  
**Implemented By:** Codex (autonomous agent — extra implementation specificity required)

---

## 1) Problem Summary

The current Validation Dashboard (`HeatupValidationVisual.*`, 13 partial class files) has the following deficiencies:

1. **Critical parameters buried in 8 tabs** — Operators must switch between OVERVIEW, PZR, CVCS, SG/RHR, RCP, LOG, VALID, and CRITICAL tabs to locate key information. No single view shows all critical parameters simultaneously.

2. **Legacy OnGUI/IMGUI rendering** — The entire dashboard uses Unity's immediate-mode `OnGUI` system which cannot support animations, transitions, smooth gauge movements, anti-aliased rendering, shader-based glow effects, or responsive layout. All arc gauges are drawn with crude GL line primitives. All text uses built-in `GUIStyle` with no typographic quality.

3. **Poor information density** — Gauge groups occupy large vertical space with low data density. Status panels are plain text rows with no visual hierarchy. Strip charts are static GL plots with no interactivity.

4. **Incomplete parameter coverage** — Many parameters from the attached requirements list (the document in the user's message) are not displayed, including: energy conservation tracking, per-system heat balance breakdown, several spray system fields, detailed SG secondary state, and comprehensive safety/limits data.

---

## 2) Expectations — What the Redesigned Dashboard Must Be

### 2.1 Visual Quality Target
- **Nuclear DCS aesthetic** — Dark background (near-black with blue tint), glowing instruments, colored status bands matching Westinghouse Ovation/Emerson DCS conventions
- **Animated arc gauges** — Smooth needle movement via `Mathf.Lerp`/`SmoothDamp`, colored sweep bands (green/amber/red), digital readout below, optional glow outline on alarm
- **Bi-directional gauges** — Center-zero arc gauges for surge flow, net CVCS, pressure rate (left = negative, right = positive)
- **Strip charts** — Real-time scrolling trend lines using custom `Graphic` subclass with `SetVerticesDirty()`, multi-trace, auto-scaling Y-axis, grid, reference lines, compact legend
- **Animated transitions** — Smooth color transitions on threshold crossings (green→amber→red via `Color.Lerp`), LED pulse on alarm, smooth panel slide for overlays
- **LED indicators** — Circular Image components with on/off/alarm/pulse states for all boolean parameters
- **Bar gauges** — Horizontal fill bars with colored fill, setpoint marker line, label, and value

### 2.2 Information Architecture
The **primary screen** must display ALL critical parameters at a glance on a single 1920×1080 screen with **NO scrolling**. Additional deep-dive data is accessible via overlay panels triggered by clicking section headers or keyboard shortcuts. The primary view is self-sufficient for monitoring — overlays are optional.

### 2.3 Primary Screen Layout

```
┌────────────────────────────────────────────────────────────────────────────┐
│  HEADER BAR                                                                │
│  [Mode LED] MODE 4 Hot Shutdown │ Phase: RCS HEATUP │ SIM: 2:15:30       │
│  WALL: 0:08:12 │ SPEED: 10x [F5][F6][F7][F8][F9] │ ⚠ 2 ALARMS          │
├────────┬───────────┬──────────────┬───────────┬────────────────────────────┤
│        │           │              │           │                            │
│  RCS   │ PRESSUR-  │   CVCS &     │  SG &     │  ALWAYS-ON STRIP CHARTS   │
│ PRIMARY│ IZER      │   VCT/BRS    │  RHR      │                            │
│        │           │              │           │  ┌──────────────────────┐  │
│ [ARC]  │ [ARC]     │ Charging  ── │ [ARC]     │  │ RCS Pressure         │  │
│ T_avg  │ PZR Press │ Letdown   ── │ SG Press  │  │ ───────────────────  │  │
│        │           │ Net CVCS [BD]│           │  └──────────────────────┘  │
│ T_hot  │ [ARC]     │              │ SG T_sat  │  ┌──────────────────────┐  │
│ T_cold │ PZR Level │ [ARC]        │ SG T_bulk │  │ PZR Level            │  │
│ Core ΔT│           │ VCT Level    │ SG ΔT     │  │ ───────────────────  │  │
│ T_pzr  │ T_pzr     │              │           │  └──────────────────────┘  │
│ T_sat  │ T_sat     │ VCT Boron    │ SG Heat   │  ┌──────────────────────┐  │
│ Sub-   │ Heater kW │ VCT Makeup ○ │ Boiling ○ │  │ T_avg / Heatup Rate  │  │
│ cooling│ Heater PID│ VCT Divert ○ │ N₂ iso  ○ │  │ ───────────────────  │  │
│        │ Spray ○   │ RWST suct  ○ │ Stm Dump○ │  └──────────────────────┘  │
│ [ARC]  │ Spray gpm │              │           │  ┌──────────────────────┐  │
│ Subcool│ [BD]      │ Mass Cons Err│ RHR mode  │  │ Subcooling           │  │
│        │ Surge Flow│ Sys Mass lbm │ RHR net MW│  │ ───────────────────  │  │
│ Heatup │           │              │ RHR HX MW │  └──────────────────────┘  │
│ Rate   │ Bubble: ○ │ BRS holdup % │           │  ┌──────────────────────┐  │
│ Press  │ P error   │ BRS inflow   │ Stm press │  │ Net CVCS / Charging  │  │
│ Rate   │ L error   │ BRS return   │ Stm dump  │  │ ───────────────────  │  │
│        │ L setpt   │              │ MW removed│  └──────────────────────┘  │
│ RCPs:  │           │              │           │  ┌──────────────────────┐  │
│ ○○○○   │           │              │ HZP prog %│  │ SG Pressure          │  │
│ Flow α │           │              │ HZP ready○│  │ ───────────────────  │  │
│ Eff MW │           │              │           │  └──────────────────────┘  │
├────────┴───────────┴──────────────┴───────────┼────────────────────────────┤
│  ANNUNCIATOR BAR (compact 2-row tile grid)     │  CONSERVATION & SIM HEALTH│
│  [HTR ON][BUBBLE][P LO][SC LO][VCT LO]...     │  Mass: ±XX lbm  Net: X MW │
└────────────────────────────────────────────────┴────────────────────────────┘

[ARC] = Arc Gauge    [BD] = Bi-directional gauge    ○ = LED indicator
── = Trend line (mini inline sparkline or digital)
```

### 2.4 Complete Parameter Mapping

Every parameter listed in the user's requirements document, mapped to its engine field and display type. Parameters grouped by dashboard panel.

#### HEADER BAR
| Parameter | Engine Field | Display |
|-----------|-------------|---------|
| Sim time (s/hr) | `simTime` | Digital HH:MM:SS |
| Sim rate | `currentSpeedIndex` | Button array [1x][2x][4x][8x][10x] |
| Paused/running | `isRunning` | LED |
| Wall time | `wallClockTime` | Digital HH:MM:SS |
| Plant mode | `plantMode` | Colored text + LED |
| Phase description | `heatupPhaseDesc` | Text |
| Active alarm count | Count of active `_cachedTiles` where `Active && IsAlarm` | Badge number |

#### RCS PRIMARY PANEL (Column 1)
| Parameter | Engine Field | Display |
|-----------|-------------|---------|
| T_avg | `T_avg` | **Arc Gauge** (50–600°F) |
| T_hot | `T_hot` | Digital readout, colored |
| T_cold | `T_cold` | Digital readout, colored |
| Core ΔT | `T_hot - T_cold` (derived) | Digital readout |
| T_pzr | `T_pzr` | Digital readout |
| T_sat | `T_sat` | Digital readout |
| Subcooling margin | `subcooling` | **Arc Gauge** (0–200°F), LOW=bad |
| Heatup rate | `heatupRate` | Digital + color (warn >40, alarm >50) |
| Pressure rate | `pressureRate` | Digital + color |
| RCS pressure | `pressure` | Digital (shown also in PZR panel arc) |
| RCS mass | `rcsWaterMass` | Digital |
| RCP status (×4) | `rcpRunning[0–3]` | 4 LED indicators in a row |
| RCP count | `rcpCount` | Digital |
| Effective RCP heat | `effectiveRCPHeat` | Digital MW |
| RCP flow fraction | `rcpContribution.TotalFlowFraction` | Digital |
| Coupling α | `min(1, rcpContribution.TotalFlowFraction)` | Digital |

#### PRESSURIZER PANEL (Column 2)
| Parameter | Engine Field | Display |
|-----------|-------------|---------|
| PZR pressure | `pressure` | **Arc Gauge** (0–2500 psia) |
| PZR level | `pzrLevel` | **Arc Gauge** (0–100%) |
| PZR liquid temp | `T_pzr` | Digital |
| T_sat at pressure | `T_sat` | Digital |
| PZR water volume | `pzrWaterVolume` | Digital ft³ |
| PZR steam volume | `pzrSteamVolume` | Digital ft³ |
| Heater mode | `currentHeaterMode` | Text + color |
| Heater power | `pzrHeaterPower * 1000` | Digital kW + bar |
| Heater demand/PID output | `heaterPIDOutput` | Bar gauge (0–100%) |
| Heater PID active | `heaterPIDActive` | LED |
| Heaters on | `pzrHeatersOn` | LED |
| Spray active | `sprayActive` | LED |
| Spray flow | `sprayFlow_GPM` | Digital gpm |
| Spray valve position | `sprayValvePosition` | Bar gauge (0–100%) |
| Surge flow (signed) | `surgeFlow` | **Bi-directional gauge** (±50 gpm) |
| Bubble state | `solidPressurizer` / `bubbleFormed` | Text + LED |
| Bubble phase | `bubblePhase` | Text |
| Pressure error | `solidPlantPressureError` | Digital psi |
| Level error | `pzrLevel - pzrLevelSetpointDisplay` (derived) | Digital % |
| Level setpoint | `pzrLevelSetpointDisplay` | Digital % |

#### CVCS & VCT/BRS PANEL (Column 3)
| Parameter | Engine Field | Display |
|-----------|-------------|---------|
| Charging active | `chargingActive` | LED |
| Charging flow | `chargingFlow` | Digital gpm |
| Charging to RCS | `chargingToRCS` | Digital gpm |
| Total CCP output | `totalCCPOutput` | Digital gpm |
| Letdown active | `letdownActive` | LED |
| Letdown flow | `letdownFlow` | Digital gpm |
| Letdown path | `letdownViaRHR` / `letdownViaOrifice` | Text (RHR XCONN / ORIFICE) |
| Letdown isolated | `letdownIsolatedFlag` | LED alarm (red) |
| Orifice letdown flow | `orificeLetdownFlow` | Digital gpm |
| RHR letdown flow | `rhrLetdownFlow` | Digital gpm |
| Net CVCS flow | `chargingFlow - letdownFlow` (derived) | **Bi-directional gauge** or digital |
| Seal injection OK | `sealInjectionOK` | LED |
| Divert fraction | `divertFraction` | Digital |
| VCT level | `vctState.Level_percent` | **Arc Gauge** (0–100%) |
| VCT volume | `vctState.Volume_gal` | Digital gal |
| VCT boron | `vctState.BoronConcentration_ppm` | Digital ppm |
| VCT makeup active | `vctMakeupActive` | LED |
| VCT divert active | `vctDivertActive` | LED |
| RWST suction | `vctRWSTSuction` | LED alarm (red) |
| VCT level low | `vctLevelLow` | LED alarm |
| VCT level high | `vctLevelHigh` | LED alarm |
| BRS holdup level | `BRSPhysics.GetHoldupLevelPercent(brsState)` | Digital % |
| BRS inflow | `brsState.InFlow_gpm` | Digital gpm |
| BRS return flow | `brsState.ReturnFlow_gpm` | Digital gpm |
| BRS processing active | `brsState.ProcessingActive` | LED |
| BRS distillate | `brsState.DistillateAvailable_gal` | Digital gal |
| System total mass | `totalSystemMass_lbm` | Digital lbm |
| Mass conservation error | `massError_lbm` | Digital lbm, colored |
| VCT flow imbalance | `massConservationError` | Digital gal, colored |

#### SG & RHR PANEL (Column 4)
| Parameter | Engine Field | Display |
|-----------|-------------|---------|
| SG secondary pressure | `sgSecondaryPressure_psia` | **Arc Gauge** |
| SG sat temp | `sgSaturationTemp_F` | Digital °F |
| SG bulk temp | `T_sg_secondary` | Digital °F |
| SG superheat | `sgMaxSuperheat_F` | Digital °F |
| Primary–secondary ΔT | `T_rcs - T_sg_secondary` (derived) | Digital °F |
| SG heat transfer | `sgHeatTransfer_MW` | Digital MW |
| SG boiling active | `sgBoilingActive` | LED |
| SG boiling intensity | `sgBoilingIntensity` | Bar gauge (0–100%) |
| SG N₂ isolated | `sgNitrogenIsolated` | LED |
| SG wide-range level | `sgWideRangeLevel_pct` | Digital % |
| SG narrow-range level | `sgNarrowRangeLevel_pct` | Digital % |
| Steam dump active | `steamDumpActive` | LED |
| Steam dump heat MW | `steamDumpHeat_MW` | Digital MW |
| Steam pressure | `steamPressure_psig` | Digital psig |
| RHR active | `rhrActive` | LED |
| RHR mode | `rhrModeString` | Text |
| RHR net heat | `rhrNetHeat_MW` | Digital MW, signed |
| RHR HX removal | `rhrHXRemoval_MW` | Digital MW |
| RHR pump heat | `rhrPumpHeat_MW` | Digital MW |
| HZP progress | `hzpProgress` | Bar gauge (0–100%) |
| HZP stable | `hzpStable` | LED |
| HZP ready for startup | `hzpReadyForStartup` | LED |

#### ALWAYS-ON STRIP CHARTS (Column 5, right side)
Six vertically stacked mini strip charts, each showing 4-hour rolling window from existing history buffers:

| Chart | Traces | Engine History Buffers |
|-------|--------|----------------------|
| RCS Pressure | RCS Pressure + PZR Level (dual axis) | `pressHistory`, `pzrLevelHistory` |
| PZR Level | PZR Level only (larger view) | `pzrLevelHistory` |
| Temperatures | T_avg, T_hot, T_cold | `tempHistory`, `tHotHistory`, `tColdHistory` |
| Subcooling | Subcooling + 30°F warn line + 15°F alarm line | `subcoolHistory` |
| CVCS Flows | Charging, Letdown, Surge | `chargingHistory`, `letdownHistory`, `surgeFlowHistory` |
| SG Pressure | SG secondary pressure | (need new history buffer OR derive from existing data) |

#### ANNUNCIATOR BAR (Bottom left)
All 26 existing annunciator tiles from `_cachedTiles[]`, displayed in a compact 2-row grid. Same boolean mappings as current `UpdateAnnunciatorTiles()`.

#### CONSERVATION & SIM HEALTH (Bottom right)
| Parameter | Engine Field | Display |
|-----------|-------------|---------|
| Mass conservation error | `massError_lbm` | Digital lbm, colored |
| VCT flow imbalance | `massConservationError` | Digital gal, colored |
| Primary ledger drift | `primaryMassDrift_lb` | Digital lbm |
| Primary mass status | `primaryMassConservationOK` | LED |
| Total heat added (sum) | `effectiveRCPHeat + pzrHeaterPower + rhrPumpHeat_MW` (derived) | Digital MW |
| Total heat removed (sum) | `sgHeatTransfer_MW + steamDumpHeat_MW + rhrHXRemoval_MW + insulation` (derived) | Digital MW |
| Net plant heat | `netPlantHeat_MW` | Digital MW, signed, colored |
| Memory usage | `Profiler.GetTotalReservedMemoryLong() + GetAllocatedMemoryForGraphicsDriver()` | Digital MB |

#### SAFETY / LIMITS / ALARMS (Integrated into panels above via color thresholds)
All alarm states are indicated by LED color and gauge band color. No separate alarm panel needed — alarms are contextually embedded. The annunciator bar provides the consolidated alarm view.

#### RVLIS (Accessible via overlay, not primary screen)
| Parameter | Engine Field |
|-----------|-------------|
| RVLIS Dynamic | `rvlisDynamic` + `rvlisDynamicValid` |
| RVLIS Full Range | `rvlisFull` + `rvlisFullValid` |
| RVLIS Upper Range | `rvlisUpper` + `rvlisUpperValid` |
| RVLIS Low Level | `rvlisLevelLow` |

#### EVENT LOG (Accessible via overlay)
Full scrollable event log from `engine.eventLog`, with severity color-coding, accessible via Ctrl+6 or clicking LOG tab-equivalent button.

### 2.5 Parameters NOT Currently Modeled (Excluded)

These parameters from the requirements document have no backing engine field and will NOT be displayed:

- **Reactor power / decay heat / reactivity breakdown** — Pre-criticality, not modeled during heatup
- **Per-loop A/B/C/D data** — Engine uses single equivalent loop model
- **RCP speed %, torque/amps, pump head ΔP** — Engine uses staged ramp abstraction
- **Individual charging pump status** — Single aggregate pump model
- **Charging/letdown line temperatures and pressures** — Not tracked
- **Individual letdown orifice states** — Internal to CVCSController (orifice75Count/orifice45Open are exposed but as aggregate)
- **VCT temperature, pressure, gas blanket pressure, NPSH** — Not tracked
- **BRS tank boron concentration** — Volumes tracked, not per-tank chemistry
- **Individual RHR valve states, line pressure/ΔP, HX inlet/outlet temps, interlock/permissive, min flow protection** — Not tracked
- **Void fraction outside PZR** — Not tracked
- **Spray inlet temperature** — Not tracked (uses T_cold as proxy)
- **Surge line temperature/ΔP** — Not tracked
- **Individual heater group breakdown** — Single aggregate heater model
- **Heater/spray/RHR inhibit reason flags** — Not tracked as structured data
- **Integration stability flags, NaN/Inf detection, fixed/variable timestep** — Internal to engine loop
- **Energy conservation error as separate tracked metric** — Only mass conservation tracked; net heat is derived
- **Feedwater flow, steam flow, SG blowdown** — Secondary loop detail not modeled

---

## 3) Technical Architecture

### 3.1 Technology Stack
- **Unity 6** (6000.3.4f1) — current project version
- **uGUI 2.0.0** (`com.unity.ugui`) — Canvas, RectTransform, Image, LayoutGroups
- **TextMeshPro** — High-quality text rendering (already in project)
- **Input System 1.17.0** — Keyboard shortcuts (already in use)
- **URP 17.3.0** — Standard render pipeline (no custom shaders required for UI overlay)
- **No external packages** — All custom components built from uGUI primitives

### 3.2 Canvas Setup
- **Canvas**: ScreenSpace-Overlay, Sort Order 100 (above game UI)
- **CanvasScaler**: Scale With Screen Size, Reference Resolution 1920×1080, Match Width Or Height = 0.5
- **Raycaster**: GraphicRaycaster for click-to-expand overlays
- **Canvas Group** on root: Alpha toggle for F1 show/hide with optional fade

### 3.3 Component Architecture

```
Assets/Scripts/ValidationDashboard/
├── Core/
│   ├── ValidationDashboardManager.cs    — Canvas lifecycle, F1 toggle, keyboard bindings, overlay management
│   ├── DashboardDataBinder.cs           — Reads engine state at configurable Hz into DashboardSnapshot
│   ├── DashboardSnapshot.cs             — Readonly struct of all display-ready values + derived calculations
│   ├── DashboardThresholds.cs           — Static class: all alarm/warning threshold constants
│   └── DashboardColorPalette.cs         — ScriptableObject: centralized color definitions (Westinghouse palette)
├── Components/
│   ├── ArcGauge.cs                      — Procedural mesh semicircular gauge with animated needle
│   ├── BiDirectionalGauge.cs            — Center-zero arc gauge extending ArcGauge
│   ├── StripChart.cs                    — Custom Graphic for real-time trend lines from ring buffers
│   ├── LEDIndicator.cs                  — Image-based boolean indicator with on/off/alarm/pulse states
│   ├── BarGauge.cs                      — Horizontal fill bar with setpoint marker
│   ├── DigitalReadout.cs                — TMP text with threshold coloring, format, unit
│   └── AnnunciatorTile.cs               — Compact alarm tile with glow border
├── Panels/
│   ├── HeaderPanel.cs                   — Mode, phase, sim/wall time, speed, alarm badge
│   ├── RCSPrimaryPanel.cs              — T_avg gauge, temps, subcooling gauge, heatup/pressure rates, RCP LEDs
│   ├── PressurizerPanel.cs             — Pressure gauge, level gauge, heater, spray, surge, bubble
│   ├── CVCSVCTPanel.cs                 — Charging/letdown, VCT level gauge, BRS, mass conservation
│   ├── SGRHRPanel.cs                   — SG pressure gauge, SG temps, heat transfer, RHR, HZP, steam dump
│   ├── TrendPanel.cs                   — 6 always-on strip charts
│   ├── AnnunciatorBar.cs               — Compact 26-tile grid
│   └── ConservationPanel.cs            — Mass/energy conservation, sim health, memory
├── Overlays/
│   ├── OverlayBase.cs                  — Base class for slide-in overlay panels
│   ├── RVLISOverlay.cs                 — RVLIS 3-range display
│   ├── BubbleFormationOverlay.cs       — 7-phase state machine tracker
│   ├── ValidationChecksOverlay.cs      — PASS/FAIL checks, inventory audit
│   └── EventLogOverlay.cs             — Scrollable filtered event log
└── Editor/
    └── DashboardColorPaletteEditor.cs  — Custom inspector for color palette preview
```

### 3.4 Data Flow

```
HeatupSimEngine (physics — runs in coroutine at 10-second sim timestep)
    │
    ▼
DashboardDataBinder.Refresh()  — called at refreshRate Hz (default 10 Hz) via InvokeRepeating
    │  Reads all public engine fields
    │  Computes derived values (Core ΔT, net CVCS, level error, total heat in/out, alarm count)
    │  Populates DashboardSnapshot struct (stack-allocated, no heap alloc)
    ▼
Each UI component reads from DashboardDataBinder.Current (the snapshot)
    │  ArcGauge: Lerps needle toward target, evaluates color bands
    │  DigitalReadout: Updates TMP text via SetText() (zero-alloc), evaluates threshold color
    │  LEDIndicator: Sets Image.color based on bool state
    │  StripChart: Reads history List<float> directly from engine, calls SetVerticesDirty() on data change
    │  AnnunciatorTile: Updates background/border color based on alarm bool
    ▼
Canvas renders at display framerate (vsync)
```

### 3.5 Key Design Decisions for Codex

1. **DashboardSnapshot is a `struct`** — Allocated on stack, no GC pressure. Contains only value types and string references (strings are cached, not allocated per-frame).

2. **String caching in DigitalReadout** — Each `DigitalReadout` maintains a `_cachedValue` float and `_cachedText` string. Only calls `TMP_Text.SetText()` when value changes beyond display precision. This eliminates ~100 string allocations per frame.

3. **ArcGauge uses UI.MaskableGraphic** — Extends `MaskableGraphic`, overrides `OnPopulateMesh(VertexHelper)` to draw a procedural semicircular arc. The arc is drawn as a triangle strip. The needle is a separate `Image` child rotated via `RectTransform.localEulerAngles.z`. This gives anti-aliased rendering via Canvas batching.

4. **StripChart uses Graphic.OnPopulateMesh** — Each strip chart is a custom `Graphic` that reads directly from the engine's `List<float>` history buffers. It generates a line strip as a thin quad strip (2 triangles per segment). Calls `SetVerticesDirty()` only when the binder refreshes (10 Hz), not every frame.

5. **No per-frame allocations** — No `new` in Update loops, no LINQ, no string concatenation. All TMP updates via `SetText(string, float)` overloads or cached `StringBuilder`.

6. **Old dashboard preserved** — `HeatupValidationVisual` component is disabled (not destroyed) on the engine GameObject. The old files remain for reference. A simple inspector toggle on `ValidationDashboardManager` switches between old and new.

---

## 4) Implementation Stages

### Stage 1: Foundation — Canvas + Data Binder + Color Palette + Manager

**Objective:** Create the uGUI Canvas infrastructure, the data binding pipeline, and the dashboard lifecycle manager. At completion, a blank dark canvas appears on F1, receives data from the engine, and keyboard shortcuts work.

**Files to create:**

1. **`Assets/Scripts/ValidationDashboard/Core/DashboardColorPalette.cs`**
   - `[CreateAssetMenu(fileName = "DashboardColorPalette", menuName = "Critical/Dashboard Color Palette")]`
   - `ScriptableObject` with public Color fields matching existing palette:
     - `bgDark`, `bgPanel`, `bgHeader`, `bgGauge`, `bgGraph` (backgrounds)
     - `normalGreen`, `warningAmber`, `alarmRed`, `tripMagenta` (functional)
     - `cyanInfo`, `blueAccent`, `orangeAccent` (informational)
     - `textPrimary`, `textSecondary`, `textDark` (text)
     - `gaugeArcBg`, `gaugeNeedle`, `gaugeTick` (gauge-specific)
     - `annOff`, `annNormal`, `annWarning`, `annAlarm` (annunciator)
     - `trace1` through `trace6`, `traceGrid` (graph traces)
   - Values copied exactly from `HeatupValidationVisual.Styles.cs` color definitions

2. **`Assets/Scripts/ValidationDashboard/Core/DashboardThresholds.cs`**
   - `static class DashboardThresholds`
   - All threshold constants for color evaluation, organized by system:
     - RCS: `T_AVG_WARN = 545f`, `T_AVG_ALARM = 570f`, `T_HOT_WARN = 600f`, `T_HOT_ALARM = 630f`
     - PZR: `LEVEL_DEV_WARN = 10f`, `LEVEL_DEV_ALARM = 15f`, `PRESS_RATE_WARN = 100f`, `PRESS_RATE_ALARM = 200f`
     - CVCS: `NET_FLOW_WARN = 10f`, `NET_FLOW_ALARM = 20f`, `MASS_ERR_WARN = 100f`, `MASS_ERR_ALARM = 500f`
     - VCT: Reference `PlantConstants.VCT_LEVEL_*` values
     - SUBCOOL: `WARN = 30f`, `ALARM = 15f`
     - HEATUP_RATE: `WARN = 40f`, `ALARM = 50f` (Tech Spec)
   - Static helper: `Color EvaluateThreshold(float value, float warnLow, float warnHigh, float alarmLow, float alarmHigh, DashboardColorPalette palette)`

3. **`Assets/Scripts/ValidationDashboard/Core/DashboardSnapshot.cs`**
   - `public struct DashboardSnapshot`
   - All display-ready values as public fields (float, bool, string, int)
   - Organized in regions matching panel layout: Header, RCS, PZR, CVCS, VCT, BRS, SG, RHR, HZP, Conservation, Alarms
   - Derived values computed once:
     - `CoreDeltaT = T_hot - T_cold`
     - `NetCVCS = chargingFlow - letdownFlow`
     - `LevelError = pzrLevel - pzrLevelSetpointDisplay`
     - `TotalHeatIn = effectiveRCPHeat + pzrHeaterPower + rhrPumpHeat_MW`
     - `TotalHeatOut = sgHeatTransfer_MW + steamDumpHeat_MW + rhrHXRemoval_MW`
     - `AlarmCount` = count of active alarm tiles
   - ~80 float fields, ~30 bool fields, ~10 string fields, 1 int field
   - No methods, no constructor — populated entirely by DashboardDataBinder

4. **`Assets/Scripts/ValidationDashboard/Core/DashboardDataBinder.cs`**
   - `MonoBehaviour`, attached to same GameObject as `ValidationDashboardManager`
   - `[SerializeField] float refreshRate = 10f` (Hz)
   - `[SerializeField] HeatupSimEngine engine` (assigned in inspector or found via `FindFirstObjectByType`)
   - Public property: `DashboardSnapshot Current { get; private set; }`
   - `OnEnable()`: `InvokeRepeating("Refresh", 0f, 1f / refreshRate)`
   - `OnDisable()`: `CancelInvoke("Refresh")`
   - `Refresh()` method:
     - Reads all public engine fields into a new `DashboardSnapshot` struct
     - Computes all derived values (CoreDeltaT, NetCVCS, LevelError, TotalHeatIn, TotalHeatOut, AlarmCount)
     - Assigns to `Current`
   - `public event System.Action OnRefresh` — raised after `Current` is updated so panels can respond
   - Zero heap allocations per refresh (struct copy only)

5. **`Assets/Scripts/ValidationDashboard/Core/ValidationDashboardManager.cs`**
   - `MonoBehaviour`, master controller for the dashboard
   - `[SerializeField] DashboardColorPalette palette`
   - `[SerializeField] bool useNewDashboard = true` (inspector toggle, switches old/new)
   - `[SerializeField] KeyCode toggleKey = KeyCode.F1`
   - References: `DashboardDataBinder binder`, `Canvas dashboardCanvas`, `CanvasGroup canvasGroup`
   - `Awake()`: Creates Canvas programmatically if not assigned:
     - Canvas: ScreenSpace-Overlay, sortingOrder 100
     - CanvasScaler: ScaleWithScreenSize, 1920×1080, matchWidthOrHeight 0.5
     - GraphicRaycaster
     - CanvasGroup: alpha = 1, interactable = true
     - Root panel: dark background image (`palette.bgDark`)
   - `Update()`: Checks `toggleKey` press → toggles `canvasGroup.alpha` and `canvasGroup.blocksRaycasts`
   - `Update()`: Checks F5–F9 for time acceleration (delegates to engine)
   - `Update()`: Checks Ctrl+1 through Ctrl+8 for overlay toggle
   - Public methods: `ShowOverlay(OverlayBase overlay)`, `HideAllOverlays()`
   - On enable: disables `HeatupValidationVisual` component if `useNewDashboard` is true
   - On disable: re-enables `HeatupValidationVisual` if it exists

6. **`Assets/Scripts/ValidationDashboard/Editor/DashboardColorPaletteEditor.cs`**
   - Custom editor for `DashboardColorPalette` ScriptableObject
   - Draws color swatches in grouped layout (Background, Functional, Info, Text, Gauge, Annunciator, Traces)
   - Preview button to show all colors in a compact grid

**Stage 1 Verification:**
- F1 toggles a dark canvas overlay on/off
- `DashboardDataBinder.Current` contains valid engine data at 10 Hz
- No errors in console
- No performance regression (< 0.5ms per refresh)
- Old dashboard can be re-enabled via inspector toggle

---

### Stage 2: Core Custom UI Components

**Objective:** Build all reusable gauge/indicator components. At completion, each component can be instantiated standalone and fed test values.

**Files to create:**

1. **`Assets/Scripts/ValidationDashboard/Components/ArcGauge.cs`**
   - Extends `MaskableGraphic`
   - Config: `minValue`, `maxValue`, `startAngle = 225f`, `endAngle = -45f` (270° sweep), `arcWidth = 8f`, `segmentCount = 64`
   - Color bands: `List<ColorBand>` where `ColorBand { float min, max; Color color; }` — defaults to green/amber/red from palette
   - Overrides `OnPopulateMesh(VertexHelper vh)`: Draws arc as triangle strip (inner/outer radius), colored per band
   - Needle: Child `Image` (white, thin triangle), rotated via `RectTransform.localEulerAngles.z` using `Mathf.LerpAngle` toward target angle
   - Digital readout: Child `TMP_Text` centered below arc, shows current value with unit
   - Label: Child `TMP_Text` above arc
   - `public void SetValue(float value)` — sets target (needle lerps in Update)
   - `Update()`: `needleAngle = Mathf.SmoothDampAngle(current, target, ref velocity, 0.15f)`
   - `public void SetColorBands(params ColorBand[] bands)` — reconfigures arc colors

2. **`Assets/Scripts/ValidationDashboard/Components/BiDirectionalGauge.cs`**
   - Extends `ArcGauge`
   - Center-zero: range is `[-maxAbsolute, +maxAbsolute]`, 12 o'clock = zero
   - Arc sweep: left half = negative (amber→red), right half = positive (amber→red), center band = green
   - Needle starts at 12 o'clock, deflects left or right proportional to value
   - Signed digital readout (+/−) below arc

3. **`Assets/Scripts/ValidationDashboard/Components/BarGauge.cs`**
   - Uses `Image` with `fillMethod = Horizontal`
   - Config: `minValue`, `maxValue`, `warnThreshold`, `alarmThreshold`
   - Fill color transitions: green → amber → red based on thresholds
   - Optional setpoint marker: vertical `Image` line positioned at setpoint fraction
   - Label `TMP_Text` left, value `TMP_Text` right
   - `public void SetValue(float value)`

4. **`Assets/Scripts/ValidationDashboard/Components/DigitalReadout.cs`**
   - `MonoBehaviour` with `TMP_Text` reference
   - Config: `string format = "F1"`, `string unit = ""`, `float warnLow`, `float warnHigh`, `float alarmLow`, `float alarmHigh`
   - `DashboardColorPalette palette` reference
   - Private: `float _cachedValue = float.NaN`, `string _cachedText = ""`
   - `public void SetValue(float value)`: Compares to `_cachedValue` within epsilon (0.01). If changed, formats new string, updates `TMP_Text.text`, evaluates threshold color via `DashboardThresholds.EvaluateThreshold()`
   - Zero allocation when value unchanged

5. **`Assets/Scripts/ValidationDashboard/Components/LEDIndicator.cs`**
   - `MonoBehaviour` with `Image` reference (circular sprite)
   - Config: `Color onColor`, `Color offColor`, `Color alarmColor`, `bool pulseOnAlarm = true`
   - States: Off, On, Alarm, Pulse
   - `public void SetState(bool active, bool alarm = false)`
   - Pulse: When alarm, `Image.color` oscillates between `alarmColor` and `offColor` at 1 Hz via `Mathf.PingPong`
   - Glow: Optional `Outline` component enabled when alarm active

6. **`Assets/Scripts/ValidationDashboard/Components/AnnunciatorTile.cs`**
   - `MonoBehaviour` with `Image background`, `TMP_Text label`, `Image border`
   - Config: `string tileName`, palette reference
   - `public void SetState(bool active, bool isAlarm)`:
     - Inactive: `palette.annOff` background, dim text
     - Active normal: `palette.annNormal` background, bright text
     - Active alarm: `palette.annAlarm` background, flash border at 1 Hz
   - Compact size: ~70×30 px to fit 26 tiles in 2 rows

**Stage 2 Verification:**
- Each component can be placed on a test Canvas and fed values manually
- ArcGauge needle animates smoothly
- LED pulses correctly at 1 Hz
- DigitalReadout only updates TMP when value changes
- No GC allocations in Update (verify with Profiler)

---

### Stage 3: Strip Chart Component

**Objective:** Build the real-time trend line component. At completion, strip charts render engine history data with proper scaling.

**Files to create:**

1. **`Assets/Scripts/ValidationDashboard/Components/StripChart.cs`**
   - Extends `Graphic` (not MaskableGraphic — simpler, no masking needed)
   - Config per trace: `StripChartTrace { string name; Color color; float lineWidth; System.Func<List<float>> dataSource; float yMin; float yMax; bool autoScale; }`
   - Up to 4 traces per chart
   - Grid: Horizontal and vertical grid lines drawn as thin quads, color `palette.traceGrid`
   - Reference lines: Optional horizontal markers (e.g., 30°F subcooling warning) drawn as dashed quads
   - Y-axis labels: `TMP_Text` children at min/max/mid, auto-positioned
   - X-axis: Fixed 4-hour window (240 samples), newest data on right
   - `OnPopulateMesh(VertexHelper vh)`:
     - Clear, draw background rect
     - Draw grid lines
     - Draw reference lines
     - For each trace: iterate data list, compute normalized position, emit quad strip (2 triangles per segment) with `lineWidth` thickness
   - Refresh: `SetVerticesDirty()` called by panel when binder raises `OnRefresh`
   - Legend: Small `TMP_Text` children in top-right with trace name + color swatch

**Stage 3 Verification:**
- Strip chart renders with real engine history data
- Multiple traces visible with distinct colors
- Grid and reference lines render correctly
- Y-axis auto-scales when enabled
- Performance: < 0.5ms per chart at 240 data points

---

### Stage 4: Panel Assembly — Primary Screen

**Objective:** Assemble all panels into the 5-column layout. At completion, the full primary screen displays all parameters at 1920×1080 with no scrolling.

**Files to create:**

1. **`Assets/Scripts/ValidationDashboard/Panels/HeaderPanel.cs`**
   - Horizontal layout: Mode LED + text | Phase text | Sim time HH:MM:SS | Wall time HH:MM:SS | Speed buttons [F5–F9] | Alarm badge
   - Subscribes to `binder.OnRefresh`, reads `snapshot.PlantMode`, `snapshot.PhaseDescription`, `snapshot.SimTime`, etc.
   - Speed buttons: 5 `Button` children labeled 1x/2x/4x/8x/10x, active button highlighted with `palette.blueAccent`
   - Alarm badge: `Image` circle with `TMP_Text` count, color = red if any alarms, hidden if zero
   - Height: 48 px fixed

2. **`Assets/Scripts/ValidationDashboard/Panels/RCSPrimaryPanel.cs`**
   - Column 1 content, width ~290 px
   - Contains: ArcGauge (T_avg), 6 DigitalReadouts (T_hot, T_cold, CoreΔT, T_pzr, T_sat, RCS pressure), ArcGauge (subcooling), 2 DigitalReadouts (heatup rate, pressure rate), 4 LEDIndicators (RCPs), 2 DigitalReadouts (flow α, effective MW), DigitalReadout (RCS mass)
   - Subscribes to `binder.OnRefresh`, distributes snapshot values to child components
   - Layout: Vertical, tight spacing, gauges sized ~120×120 px, readouts ~16 px height

3. **`Assets/Scripts/ValidationDashboard/Panels/PressurizerPanel.cs`**
   - Column 2 content, width ~260 px
   - Contains: ArcGauge (pressure), ArcGauge (level), DigitalReadouts (T_pzr, T_sat, water vol, steam vol), DigitalReadout (heater kW) + BarGauge (PID output), LEDs (heater on, PID active, spray active), DigitalReadout (spray gpm), BarGauge (spray valve), BiDirectionalGauge (surge flow), LED (bubble) + text (bubble phase), DigitalReadouts (pressure error, level error, level setpoint)
   - Subscribes to `binder.OnRefresh`

4. **`Assets/Scripts/ValidationDashboard/Panels/CVCSVCTPanel.cs`**
   - Column 3 content, width ~280 px
   - Contains: LEDs + DigitalReadouts for charging (active, flow, to RCS, total CCP), letdown (active, flow, path text, isolated LED, orifice flow, RHR flow), BiDirectionalGauge or DigitalReadout (net CVCS), LED (seal injection), ArcGauge (VCT level), DigitalReadouts (VCT volume, boron), LEDs (makeup, divert, RWST, level low/high), DigitalReadouts (mass error, system mass, flow imbalance), section divider, DigitalReadouts (BRS holdup %, inflow, return, distillate), LED (BRS processing)
   - Subscribes to `binder.OnRefresh`

5. **`Assets/Scripts/ValidationDashboard/Panels/SGRHRPanel.cs`**
   - Column 4 content, width ~260 px
   - Contains: ArcGauge (SG pressure), DigitalReadouts (SG T_sat, T_bulk, superheat, pri-sec ΔT), DigitalReadout (SG heat MW), LEDs (boiling, N₂ isolated, steam dump), BarGauge (boiling intensity), DigitalReadouts (SG wide/narrow level), section divider, LED (RHR active) + text (RHR mode), DigitalReadouts (RHR net MW, HX MW, pump MW), DigitalReadout (steam pressure, steam dump MW), BarGauge (HZP progress), LEDs (HZP stable, HZP ready)
   - Subscribes to `binder.OnRefresh`

6. **`Assets/Scripts/ValidationDashboard/Panels/TrendPanel.cs`**
   - Column 5 content, width ~430 px (remaining space)
   - Contains: 6 StripChart instances stacked vertically, each ~130 px height
   - Charts: RCS Pressure (dual trace: pressure + PZR level), PZR Level (single), Temperatures (3 traces: T_avg, T_hot, T_cold), Subcooling (single + 2 reference lines at 30°F and 15°F), CVCS Flows (3 traces: charging, letdown, surge), SG Pressure (single)
   - Each chart configured with appropriate data source lambdas pointing to engine history buffers
   - Subscribes to `binder.OnRefresh`, calls `SetVerticesDirty()` on each chart

7. **`Assets/Scripts/ValidationDashboard/Panels/AnnunciatorBar.cs`**
   - Bottom-left panel, height ~70 px
   - Contains: 26 `AnnunciatorTile` instances in a 2-row `GridLayoutGroup` (13 per row)
   - Tile names and mapping match existing `_cachedTiles[]` from `HeatupValidationVisual.Annunciators.cs`
   - Subscribes to `binder.OnRefresh`, updates each tile's state from snapshot alarm booleans

8. **`Assets/Scripts/ValidationDashboard/Panels/ConservationPanel.cs`**
   - Bottom-right panel, height ~70 px, width ~430 px (aligns with trend column)
   - Contains: DigitalReadouts for mass error, flow imbalance, primary ledger drift, LED (primary mass OK), DigitalReadouts for total heat in, total heat out, net plant heat, memory usage MB
   - Compact 2-column layout
   - Subscribes to `binder.OnRefresh`

**Stage 4 Verification:**
- All 5 columns visible at 1920×1080 with no scrolling
- All parameters from Section 2.4 are displayed and updating
- Gauge needles animate smoothly
- Strip charts show real trend data
- Annunciator tiles reflect alarm states
- Layout does not break at target resolution
- No overlapping text or clipped content

---

### Stage 5: Detail Overlays

**Objective:** Build slide-in overlay panels for deep-dive data. Overlays open on top of primary screen without replacing it.

**Files to create:**

1. **`Assets/Scripts/ValidationDashboard/Overlays/OverlayBase.cs`**
   - Abstract `MonoBehaviour`, base class for all overlays
   - `CanvasGroup` reference for fade in/out
   - `RectTransform` anchored to right edge, slides in from right
   - `public void Show()`: Enables GO, lerps `anchoredPosition.x` from +width to 0 over 0.2s
   - `public void Hide()`: Lerps out, then disables GO
   - `public bool IsVisible { get; }`
   - Width: 500 px (overlaps trend panel when open)
   - Background: `palette.bgPanel` with slight transparency
   - Close button: top-right X button + Escape key

2. **`Assets/Scripts/ValidationDashboard/Overlays/RVLISOverlay.cs`**
   - Extends `OverlayBase`
   - Displays: RVLIS Dynamic (value + valid LED), RVLIS Full Range (value + valid LED), RVLIS Upper Range (value + valid LED), RVLIS Low Level (alarm LED)
   - Visual: Simplified vessel cross-section diagram with level indicators (can be drawn with Image sprites or procedural Graphic)
   - Keyboard shortcut: Ctrl+1

3. **`Assets/Scripts/ValidationDashboard/Overlays/BubbleFormationOverlay.cs`**
   - Extends `OverlayBase`
   - Displays: 7-phase state machine as vertical step list with LED for each phase
   - Current phase highlighted with `palette.blueAccent`
   - Completed phases: `palette.normalGreen`
   - Future phases: `palette.textSecondary` (dimmed)
   - Shows: phase name, entry criteria, current values vs. thresholds
   - Keyboard shortcut: Ctrl+2

4. **`Assets/Scripts/ValidationDashboard/Overlays/ValidationChecksOverlay.cs`**
   - Extends `OverlayBase`
   - Displays: List of validation checks with PASS/FAIL/WARN LED per check
   - Checks include: mass conservation within tolerance, pressure within expected range, heatup rate within Tech Spec, subcooling adequate, VCT level stable, PZR level tracking setpoint, SG heat transfer reasonable
   - Inventory audit section: RCS mass, VCT volume, BRS holdup, total system mass, ledger drift
   - Keyboard shortcut: Ctrl+3

5. **`Assets/Scripts/ValidationDashboard/Overlays/EventLogOverlay.cs`**
   - Extends `OverlayBase`
   - Displays: Scrollable list of `engine.eventLog` entries
   - Each entry: timestamp (sim time), severity LED (info=cyan, warning=amber, error=red), message text
   - Filter buttons: All / Info / Warning / Error
   - Auto-scroll to newest entry on open
   - Scroll view using Unity `ScrollRect` + `VerticalLayoutGroup` + pooled entry prefab
   - Max visible: 50 entries (pooled, recycled on scroll)
   - Keyboard shortcut: Ctrl+4

**Stage 5 Verification:**
- Each overlay opens/closes smoothly with slide animation
- Overlays display correct data from engine
- Keyboard shortcuts work (Ctrl+1 through Ctrl+4)
- Escape key closes any open overlay
- Only one overlay open at a time (opening one closes others)
- Primary screen data remains visible behind overlay

---

### Stage 6: Animation & Polish

**Objective:** Add visual refinements for professional nuclear DCS aesthetic.

**Modifications to existing files:**

1. **ArcGauge needle animation** — Verify `SmoothDamp` tuning (0.15s response). Add subtle overshoot dampening.

2. **Alarm pulse** — All `LEDIndicator` in alarm state flash at 1 Hz (0.5s on, 0.5s off) using `Mathf.PingPong(Time.unscaledTime, 1f)`.

3. **Color transitions** — `DigitalReadout` threshold color changes use `Color.Lerp` over 0.3s rather than instant snap. Track `_currentColor` and `_targetColor`.

4. **Strip chart anti-aliasing** — Increase quad strip line width to 2px minimum. Add alpha fadeout at endpoints.

5. **Glow effects on alarm gauges** — When `ArcGauge` value is in alarm band, add pulsing `Outline` component (color = alarm red, alpha oscillates 0.3–0.8 at 1 Hz).

6. **Panel hover highlight** — Each panel's background `Image` brightens slightly (alpha +0.05) on pointer enter, returns on pointer exit. Uses `IPointerEnterHandler`/`IPointerExitHandler`.

7. **Annunciator tile first-out indicator** — The first annunciator to activate gets a brighter border to indicate first-out alarm (standard nuclear convention).

8. **Header alarm badge animation** — Badge pulses (scale 1.0→1.15→1.0) when alarm count increases.

**Stage 6 Verification:**
- Animations are smooth at 60 FPS
- Alarm pulsing is visible and consistent
- Color transitions feel natural (no abrupt flashing)
- Glow effects render correctly under URP
- Overall visual quality matches modern DCS aesthetic
- No performance degradation (total dashboard < 2ms per frame)

---

### Stage 7: Migration & Cleanup

**Objective:** Complete the transition from old to new dashboard. Final validation.

**Tasks:**

1. **Wire all keyboard shortcuts:**
   - F1: Toggle dashboard visibility
   - F5–F9: Time acceleration (1x, 2x, 4x, 8x, 10x)
   - +/−: Fine adjust time acceleration
   - Ctrl+1–4: Overlay toggles (RVLIS, Bubble, Validation, Event Log)
   - Escape: Close active overlay

2. **Disable old dashboard:**
   - `ValidationDashboardManager` disables `HeatupValidationVisual` component on startup when `useNewDashboard = true`
   - Old component NOT destroyed — can be re-enabled via inspector for comparison
   - Old partial class files remain in `Assets/Scripts/Validation/` untouched (GOLD standard preserved)

3. **Performance profiling:**
   - Target: < 2ms total dashboard overhead per frame at 10 Hz refresh
   - Profile with Unity Profiler: Canvas rebuild time, `DashboardDataBinder.Refresh()`, total managed allocations
   - Optimize if any component exceeds 0.5ms individually

4. **Resolution testing:**
   - Verify layout at 1920×1080 (primary target)
   - Verify CanvasScaler handles 2560×1440 and 1366×768 gracefully (scaled, no clipping)
   - Verify all text remains legible at minimum supported resolution

5. **Documentation:**
   - Add XML doc comments to all public methods and classes
   - Create `Assets/Scripts/ValidationDashboard/README.md` with architecture overview, component catalog, and keyboard shortcut reference

**Stage 7 Verification:**
- All keyboard shortcuts function correctly
- Old dashboard can be toggled on/off via inspector
- Performance meets < 2ms target
- Layout correct at 1920×1080
- No console errors or warnings
- Full functional parity with old dashboard PLUS new features (always-on trends, overlays, animated gauges)

---

## 5) Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Canvas rebuild cost too high with many Graphic components | Medium | Medium | Use canvas sub-grouping (separate Canvas for static vs dynamic elements), minimize SetVerticesDirty calls |
| ArcGauge procedural mesh complexity causes batching issues | Low | Low | Keep segment count at 64, verify draw calls with Frame Debugger |
| Strip chart performance with 6 charts × 240 points | Medium | Medium | Only call SetVerticesDirty on binder refresh (10 Hz), not every frame |
| Layout breaks at non-standard resolutions | Low | Medium | CanvasScaler handles scaling; test at 1366×768 minimum |
| Codex agent drifts from specification | High | High | Extreme detail in this IP; each file fully specified with field names, types, and behavior |

---

## 6) Unaddressed Issues

| Issue | Reason |
|-------|--------|
| Parameters not currently modeled (see Section 2.5) | Engine would need new fields; out of scope for dashboard redesign |
| Multi-monitor support | Future feature — current design targets single 1920×1080 display |
| User-configurable layout | Future feature — panels are fixed-position in this version |
| Touch/mouse interaction with gauges | Future feature — current version is display-only (no click-to-adjust) |
| Trend data export | Future feature — no CSV/clipboard export in this version |
| Dark/light theme switching | Future feature — single dark theme only |

All items above are candidates for future implementation and should be recorded in `Critical\Updates\Future_Features` if approved.

---

## 7) Estimated Scope

| Stage | Files | Estimated Lines | Complexity |
|-------|-------|----------------|------------|
| 1 — Foundation | 6 | ~800 | Medium |
| 2 — Core Components | 6 | ~1200 | High (procedural mesh) |
| 3 — Strip Chart | 1 | ~400 | High (custom Graphic) |
| 4 — Panel Assembly | 8 | ~1600 | Medium (layout + wiring) |
| 5 — Overlays | 5 | ~800 | Medium |
| 6 — Animation & Polish | 0 (modifications) | ~300 | Low |
| 7 — Migration & Cleanup | 0 (modifications + docs) | ~200 | Low |
| **Total** | **26 new files** | **~5,300 lines** | |

---

## 8) Approval Request

This Implementation Plan is ready for review. Upon approval:

1. Implementation will proceed **one stage at a time**, with user verification after each stage.
2. No GOLD standard modules will be modified.
3. No new fields will be added to `HeatupSimEngine`.
4. The old `HeatupValidationVisual` dashboard will be preserved and toggleable.
5. A Changelog (CL-0029) will be created upon completion of all stages.

**Awaiting approval to proceed with Stage 1.**