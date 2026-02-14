# Implementation Plan — Multi-Screen Operator GUI System

**Version:** 2.0.0  
**Date:** 2026-02-10  
**Author:** AI Development Assistant  
**Status:** Awaiting Approval  
**Matching Changelog:** `CHANGELOG_v2.0.0.md`

---

## 1. Problem Summary

The simulator requires a comprehensive multi-screen operator interface covering all major PWR systems. Currently:

- **Screen 1 (Reactor Core):** Complete and GOLD STANDARD (`ReactorOperatorScreen.cs`). Does NOT inherit from `OperatorScreen` base class. Uses legacy `Input.GetKeyDown()` for keyboard toggle.
- **Screen 2 (RCS Primary Loop):** Script complete (`RCSPrimaryLoopScreen.cs`), inherits from `OperatorScreen`, but has no builder — the UI hierarchy is not constructed at runtime.
- **Screen Manager:** Complete (`ScreenManager.cs`) — handles keyboard routing and mutual exclusion, but uses legacy `Input.GetKeyDown()`.
- **OperatorScreen base class:** Complete (`OperatorScreen.cs`) — provides common toggle/visibility/layout, uses legacy `Input.GetKeyDown()`.
- **Screens 3–8, Tab:** No scripts or prefabs exist.
- **No prefabs exist in `Assets/Prefabs/Screens/`.**
- **ReactorOperatorScreen is GOLD STANDARD** and does not inherit from `OperatorScreen`. It manages its own keyboard toggle separately.

### Critical Input System Problem

**The project uses Unity's New Input System exclusively** (`activeInputHandler: 1` in ProjectSettings, `com.unity.inputsystem: 1.17.0`). The legacy `UnityEngine.Input` API is **disabled**. All existing calls to `Input.GetKeyDown()` in `ScreenManager.cs`, `OperatorScreen.cs`, and `ReactorOperatorScreen.cs` are **non-functional**. Keyboard screen switching does not work at all in the current build.

The existing `InputSystem_Actions.inputactions` has a Player action map with `Previous` (key 1) and `Next` (key 2) bindings, but no dedicated screen-switching actions.

### What Must Be Fixed

1. **Input System migration** — All screen switching must use the new Input System via an Input Actions asset with dedicated screen-toggle actions.
2. **Screen 2 UI hierarchy** — The RCS screen script exists but has no runtime UI builder.
3. **Screens 3–8, Tab** — Must be created from scratch.

---

## 2. Expectations — Correct Behavior

When complete, the operator should be able to:

1. Press keys `1`–`8` and `Tab` to toggle between screens (via Unity New Input System).
2. Only one screen is visible at a time (mutual exclusion).
3. All gauges display real-time simulator data from physics modules.
4. Indicators reflect system status (alarm, ready, off, running).
5. Controls are visual-only (inactive) at this stage — layout follows the Operator Screen Layout Plan v1.0.0.
6. Each screen follows the standard layout: left gauges, center visualization, right gauges, bottom controls.
7. The 3D RCS model (Blender FBX in `Assets/Models/RCS/`) renders in Screen 2's center panel via RenderTexture.
8. Performance: ≥60 FPS with any screen visible, <2 GB memory.

---

## 3. Proposed Fix — Technical Implementation

### 3.1 Architecture Overview

#### Input System Strategy

A new **`OperatorScreens`** action map will be added to the existing `InputSystem_Actions.inputactions` file (or a dedicated `ScreenInputActions.inputactions` file). Actions:

| Action Name | Binding | Type |
|-------------|---------|------|
| `Screen1` | `<Keyboard>/1` | Button |
| `Screen2` | `<Keyboard>/2` | Button |
| `Screen3` | `<Keyboard>/3` | Button |
| `Screen4` | `<Keyboard>/4` | Button |
| `Screen5` | `<Keyboard>/5` | Button |
| `Screen6` | `<Keyboard>/6` | Button |
| `Screen7` | `<Keyboard>/7` | Button |
| `Screen8` | `<Keyboard>/8` | Button |
| `Overview` | `<Keyboard>/tab` | Button |

The `ScreenManager` will be refactored to subscribe to these Input Actions instead of polling `Input.GetKeyDown()`. The existing `Player` action map bindings for keys `1` and `2` (`Previous`/`Next`) will need to be rebound or removed to avoid conflicts.

**Note:** The `OperatorScreen` base class's fallback keyboard handling (used when no `ScreenManager` is present) will also be migrated to use Input Actions.

#### Screen Registration Strategy

- **Screen 1 (ReactorOperatorScreen):** GOLD STANDARD, not modified. A thin **adapter wrapper** (`ReactorScreenAdapter.cs`) will be placed on the same GameObject to register it with `ScreenManager` and bridge the visibility API. The adapter reads `ScreenID=1` and `ToggleKey=Alpha1` from the existing component and delegates `Show()`/`Hide()` to `SetActive()`. The adapter also suppresses Screen 1's legacy `Input.GetKeyDown()` call (which is already non-functional due to the new input system, so no actual conflict exists — but the adapter ensures clean integration).
- **Screens 2–8, Tab:** Inherit from `OperatorScreen`, self-register with `ScreenManager` on `Start()`.

#### Screen Builder Strategy

- A new **`MultiScreenBuilder.cs`** editor tool (menu: `Critical > Create All Screens`) creates the complete multi-screen Canvas hierarchy.
- Each screen gets its own builder method that creates the full UI hierarchy programmatically.
- Reuses the pattern from `OperatorScreenBuilder.cs`.

#### Data Binding Strategy

- **Available Data Sources (confirmed in codebase):**
  - `ReactorController`: NeutronPower, ThermalPower, Tavg, Thot, Tcold, DeltaT, FuelCenterline, Boron_ppm, Xenon_pcm, TotalReactivity, BankDPosition, Keff, ReactorPeriod, StartupRate_DPM, FlowFraction, SimulationTime, Mode, IsTripped, IsCritical
  - `PressurizerPhysics` (via `PressurizerState`): WaterTemp, Pressure, Level, HeaterEffectivePower, BubbleFormed, SteamVolume, WaterVolume
  - `CVCSController`: ChargingFlow, LetdownFlow, BoronConcentration, VCTLevel
  - `SGSecondaryThermal` / `SGMultiNodeThermal`: SG temperatures, pressures, levels
  - `RCPSequencer`: Pump ramp states, flow fractions
  - `SteamDumpController`: Steam dump flow, valve positions
  - `LoopThermodynamics`: Loop temperature calculations
  - `MosaicBoard`: Central data provider with `GetValue(GaugeType)`, `GetAlarmState()`

- **Gauges that map to existing data:** Bind directly via `MosaicBoard` or `ReactorController`.
- **Gauges with no data source yet:** Use custom thresholds and display placeholder values (`---`) with clear `// PLACEHOLDER` comments, marked for future integration.

### 3.2 File Organization

All new scripts go in `Assets/Scripts/UI/` (existing folder) to avoid breaking assembly references used by the Reactor Core GOLD STANDARD screen.

```
Assets/
├── Scripts/
│   └── UI/                               ← ALL scripts here (existing + new)
│       ├── ScreenManager.cs              ← MODIFIED: New Input System migration
│       ├── OperatorScreen.cs             ← MODIFIED: New Input System migration
│       ├── ReactorOperatorScreen.cs      ← GOLD STANDARD — NO changes
│       ├── RCSPrimaryLoopScreen.cs       ← Existing, no changes
│       ├── ScreenInputActions.cs         ← NEW: Generated C# wrapper for Input Actions
│       ├── ReactorScreenAdapter.cs       ← NEW: Adapter for GOLD Screen 1
│       ├── ScreenDataBridge.cs           ← NEW: Centralizes data access for all screens
│       ├── PressurizerScreen.cs          ← NEW: Screen 3
│       ├── CVCSScreen.cs                 ← NEW: Screen 4
│       ├── SteamGeneratorScreen.cs       ← NEW: Screen 5
│       ├── TurbineGeneratorScreen.cs     ← NEW: Screen 6
│       ├── SecondarySystemsScreen.cs     ← NEW: Screen 7
│       ├── AuxiliarySystemsScreen.cs     ← NEW: Screen 8
│       ├── PlantOverviewScreen.cs        ← NEW: Screen Tab
│       ├── MultiScreenBuilder.cs         ← NEW: Editor tool to create all screens
│       └── ... (all other existing UI scripts unchanged)
├── InputActions/
│   └── ScreenInputActions.inputactions   ← NEW: Dedicated Input Actions for screens
└── Prefabs/
    └── Screens/                          ← Created by MultiScreenBuilder at edit time
```

### 3.3 Implementation Stages

**Priority order per user instruction:** RCS Screen (2) first → Plant Overview (Tab) → then 3, 4, 5, 6, 7, 8 in order.

---

#### **Stage 1: Input System Foundation & ScreenDataBridge** (Infrastructure)

**Files Created:**
- `Assets/InputActions/ScreenInputActions.inputactions` — New Input Actions asset with `OperatorScreens` action map containing 9 button actions (Screen1–Screen8, Overview) bound to keys 1–8 and Tab.
- `Assets/Scripts/UI/ScreenDataBridge.cs` — Singleton MonoBehaviour that provides unified data access for all screens. Finds `ReactorController`, `MosaicBoard`, physics modules (`PressurizerPhysics` state holders, `CVCSController`, `SGMultiNodeThermal`, `SteamDumpController`, `RCPSequencer`). Exposes typed getter methods (e.g., `GetPZRPressure()`, `GetCVCSChargingFlow()`, `GetSGLevel(int sgIndex)`). Returns default/placeholder values when source is null.
- `Assets/Scripts/UI/ReactorScreenAdapter.cs` — MonoBehaviour placed alongside `ReactorOperatorScreen`. Reads `ScreenID=1` and `ToggleKey=Alpha1` from the existing component. Implements a lightweight bridge so `ScreenManager` can register Screen 1 without modifying `ReactorOperatorScreen`. Since legacy `Input.GetKeyDown()` is already non-functional (new input system only), no conflict exists — but the adapter provides clean `Show()`/`Hide()` delegation.

**Files Modified:**
- `Assets/Scripts/UI/ScreenManager.cs` — Replace `Input.GetKeyDown()` polling in `HandleKeyboardInput()` with subscription to `ScreenInputActions` Input Actions. Add `InputActionAsset` reference field. Subscribe to action callbacks in `OnEnable()`, unsubscribe in `OnDisable()`. The `_keyToScreenIndex` dictionary and `KeyCode`-based lookup are replaced with action-name-based lookup.
- `Assets/Scripts/UI/OperatorScreen.cs` — Replace the fallback `Input.GetKeyDown(ToggleKey)` in `Update()` with New Input System equivalent (only used when `ScreenManager` is absent). Note: `ToggleKey` property (KeyCode) is kept for backward compatibility with existing screen classes but is no longer used for polling.

**GOLD STANDARD `ReactorOperatorScreen.cs`:** NOT MODIFIED. Its `Input.GetKeyDown()` call is already non-functional (returns false always on new input system), so it causes no harm. The `ReactorScreenAdapter` handles all input for Screen 1.

**Existing `InputSystem_Actions.inputactions`:** The `Previous` action (bound to key 1) and `Next` action (bound to key 2) in the Player map conflict with screen switching. These bindings will be documented as needing removal/rebinding by the user, or the new OperatorScreens action map will take priority via action map enable/disable logic.

**Validation:** 
- Press `1` — Screen 1 toggles via ScreenManager (through adapter) using new Input System.
- Press `2` — ScreenManager receives the action (no screen built yet, but registration logic works).
- Legacy `Input.GetKeyDown()` calls produce no errors (they just return false).

---

#### **Stage 2: RCS Primary Loop Screen Builder** (Screen 2)

**Files Created:**
- Builder method `CreateRCSScreen()` in `MultiScreenBuilder.cs` that programmatically builds the Screen 2 UI hierarchy:
  - Canvas child panel with `RCSPrimaryLoopScreen` component + `CanvasGroup`
  - Left panel (0–15%): 8 `MosaicGauge` instances with `VerticalLayoutGroup` — Loop 1–4 T-hot, Loop 1–4 T-cold. Each gauge configured with custom labels and thresholds from `RCSGaugeSpecs`.
  - Center panel (15–65%): `RawImage` for 3D RenderTexture display. Reference to FBX model at `Assets/Models/RCS/RCS_Primary_Loop.fbx`. `RCSVisualizationController` wired automatically.
  - Right panel (65–100%): 8 `MosaicGauge` instances — Total RCS Flow, Loop 1–4 Flow, Core Thermal Power, Core ΔT, Average T-avg.
  - Bottom panel (0–26%): 4 `RCPControlPanel` sub-sections (each with TextMeshPro labels for pump number, status, speed, flow, amps; status indicator Image; START/STOP buttons). Status text displays (RCP count, circulation mode, plant mode). Alarm strip container with TextMeshPro entries.
  - All SerializedField references on `RCSPrimaryLoopScreen` wired to created UI elements.
- Start of `MultiScreenBuilder.cs` editor tool (menu: `Critical > Create All Operator Screens`), initially containing Canvas setup + Screen 2 builder only.

**Files NOT Modified:** `RCSPrimaryLoopScreen.cs` already exists and is complete.

**Data Bindings (Screen 2):**
- Loop temperatures → `ReactorController.Thot` / `ReactorController.Tcold` (lumped model — all 4 loops show same values)
- Loop flows → Calculated from `ReactorController.FlowFraction` × `RATED_FLOW_PER_RCP_GPM` per running pump
- Total flow → Sum of running pump flows
- Core power → `ReactorController.ThermalPower_MWt`
- Core ΔT → `ReactorController.DeltaT`
- T-avg → `ReactorController.Tavg`
- RCP states → Tracked internally by `RCSPrimaryLoopScreen` via `RCPSequencer`
- 3D visualization → `RCSVisualizationController` receives temperatures and RCP states

**Validation:** 
- Menu `Critical > Create All Operator Screens` builds Screen 2 hierarchy in scene.
- Press Play → Press `2` → Screen 2 appears with all 16 gauges, 4 RCP panels, 3D model in center.
- Gauge values update in real-time from simulator state.
- RCP start/stop buttons functional (controls already wired in `RCSPrimaryLoopScreen`).
- 3D model shows temperature-colored piping and animated flow arrows.

---

#### **Stage 3: Plant Overview Screen** (Tab — Priority)

**Files Created:**
- `Assets/Scripts/UI/PlantOverviewScreen.cs` — Inherits `OperatorScreen`. Key=`Tab`, Index=100 (matches `ScreenManager.OVERVIEW_INDEX`).
  - **Left gauges (Nuclear/Primary):** Reactor Power (%), T-avg (°F), RCS Pressure (psia), PZR Level (%), Total RCS Flow (gpm), Control Rod Position (steps), Boron Concentration (ppm), Xenon Worth (pcm)
  - **Center:** Simplified plant-wide mimic diagram (procedural 2D UI):
    - Reactor vessel icon (center) with power % overlay
    - 4 RCS loop lines (hot legs in red tones, cold legs in blue tones)
    - Pressurizer icon with pressure/level overlay
    - 4 Steam Generator icons with level indicator bars
    - Turbine-Generator icon with MWe overlay
    - Condenser icon
    - Feedwater train (simplified line)
    - Color-coded based on temperatures where data is available
    - Static schematic with dynamic parameter overlays
  - **Right gauges (Secondary/Output):** SG Level Avg (%), Steam Pressure (psia), Feedwater Flow (lb/hr), Turbine Power (MWe), Generator Output (MWe), Condenser Vacuum (in Hg), Feedwater Temperature (°F), Main Steam Flow (lb/hr)
  - **Bottom panel:** Reactor mode indicator, 4 RCP status indicator lights, turbine status, generator breaker status, major alarm summary (last 4 alarms), simulation time display, time compression display, emergency action buttons (Reactor Trip, Turbine Trip — visual only at this stage)
- Builder method `CreatePlantOverviewScreen()` added to `MultiScreenBuilder.cs`

**Data Bindings:**
- Reactor Power → `ReactorController.ThermalPower` (× 100 for %)
- T-avg → `ReactorController.Tavg`
- RCS Pressure → `ScreenDataBridge.GetPZRPressure()` (from PressurizerState)
- PZR Level → `ScreenDataBridge.GetPZRLevel()` (from PressurizerState)
- Total RCS Flow → Calculated from `ReactorController.FlowFraction` × total rated flow
- Rod Position → `ReactorController.BankDPosition`
- Boron → `ReactorController.Boron_ppm`
- Xenon → `ReactorController.Xenon_pcm`
- SG Level Avg → `ScreenDataBridge.GetSGLevel(0)` (from SGMultiNodeThermal)
- Steam Pressure → `ScreenDataBridge.GetSteamPressure()` (from SG secondary)
- Turbine Power, Generator Output, Condenser Vacuum → PLACEHOLDER (`---`)
- FW Flow, FW Temp, Main Steam Flow → PLACEHOLDER (`---`)
- RCP Status → From `ReactorController.FlowFraction` (infer pump count)
- Reactor Mode → `ReactorController.Mode`
- Sim Time → `ReactorController.SimulationTime` or `Time.time`

**Validation:**
- Press `Tab` → Plant overview screen appears with plant mimic diagram.
- All primary-side gauges show live data.
- RCP indicators light up based on pump state.
- Reactor mode shows correctly.
- Placeholder gauges display `---` with correct labels.

---

#### **Stage 4: Pressurizer Screen** (Screen 3)

**Files Created:**
- `Assets/Scripts/UI/PressurizerScreen.cs` — Inherits `OperatorScreen`. Key=`Alpha3`, Index=3.
  - Left gauges: PZR Pressure, Pressure Setpoint, Pressure Error, Pressure Rate, Heater Power, Spray Flow, Backup Heater Status, PORV Status
  - Center: 2D pressurizer vessel cutaway (procedural UI — Image layers for vessel outline, water level fill via `Image.fillAmount`, heater glow rectangles at bottom, spray indicator at top, steam dome, surge line connection indicator)
  - Right gauges: PZR Level, Level Setpoint, Level Error, Surge Flow, Steam Volume, Water Volume, Total RCS Inventory, Surge Line Temperature
  - Bottom: Heater controls (visual only), spray controls (visual only), pressure setpoint adjustment (visual only), PORV indicator, safety valve indicators, alarm panel
- Builder method `CreatePressurizerScreen()` added to `MultiScreenBuilder.cs`

**Data Bindings:**
- PZR Pressure → `ScreenDataBridge.GetPZRPressure()`
- PZR Level → `ScreenDataBridge.GetPZRLevel()`
- Heater Power → `ScreenDataBridge.GetHeaterPower()`
- Water/Steam Volume → `ScreenDataBridge.GetPZRWaterVolume()` / `GetPZRSteamVolume()`
- Pressure Setpoint → PLACEHOLDER (2235 psia default shown)
- Pressure Rate, Spray Flow, Surge Flow, Surge Line Temp → PLACEHOLDER
- Pressure Error → Calculated (Pressure - Setpoint) if setpoint available, else PLACEHOLDER

**Validation:** Press `3` — Pressurizer screen with animated water level fill, live pressure/level/heater data.

---

#### **Stage 5: CVCS Screen** (Screen 4)

**Files Created:**
- `Assets/Scripts/UI/CVCSScreen.cs` — Inherits `OperatorScreen`. Key=`Alpha4`, Index=4.
  - Left gauges: Charging Flow, Letdown Flow, Seal Injection Flow, Net Inventory Change, VCT Level, VCT Temperature, VCT Pressure, CCP Discharge Pressure
  - Center: 2D CVCS flow diagram (VCT tank with level, charging pumps, letdown line, seal injection branches, boration/dilution paths, flow direction indicators)
  - Right gauges: RCS Boron Concentration, VCT Boron Concentration, Boration Flow, Dilution Flow, Boron Worth, Letdown Temperature, Charging Temperature, Purification Flow
  - Bottom: CCP controls, letdown valve control, boration/dilution mode selector (all visual only), alarm panel
- Builder method `CreateCVCSScreen()` added to `MultiScreenBuilder.cs`

**Data Bindings:**
- Charging/Letdown Flow → `ScreenDataBridge` from `CVCSController`
- VCT Level → `ScreenDataBridge` from `VCTPhysics`
- Boron Concentration → `ReactorController.Boron_ppm`
- Others → PLACEHOLDER

**Validation:** Press `4` — CVCS screen with live boron/VCT data and CVCS flow diagram.

---

#### **Stage 6: Steam Generator Screen** (Screen 5)

**Files Created:**
- `Assets/Scripts/UI/SteamGeneratorScreen.cs` — Key=`Alpha5`, Index=5.
  - Left: SG-A–D Primary Inlet/Outlet Temps
  - Center: Quad-SG 2×2 layout with U-tube schematics and level indicators
  - Right: SG-A–D Level, SG-A–D Steam Pressure
  - Bottom: Feedwater flow, SG blowdown, heat removal rate, alarm panel
- Builder method added to `MultiScreenBuilder.cs`

**Data Bindings:** SG temps from SGMultiNodeThermal; primary temps from ReactorController (lumped → all 4 identical). Per-SG level/pressure → PLACEHOLDER for individual SGs.

---

#### **Stage 7: Turbine-Generator Screen** (Screen 6)

**Files Created:**
- `Assets/Scripts/UI/TurbineGeneratorScreen.cs` — Key=`Alpha6`, Index=6.
  - Left: HP/LP turbine gauges (8 total)
  - Center: Turbine-generator shaft train diagram
  - Right: Generator output gauges (8 total)
  - Bottom: Turbine/generator controls (visual only), alarm panel

**Data Bindings:** Almost entirely PLACEHOLDER — turbine model not implemented.

---

#### **Stage 8: Secondary Systems Screen** (Screen 7)

**Files Created:**
- `Assets/Scripts/UI/SecondarySystemsScreen.cs` — Key=`Alpha7`, Index=7.
  - Left: Feedwater train gauges, Right: Steam system gauges
  - Center: Secondary cycle flow diagram
  - Bottom: Steam dump controls, MSIV controls (visual only)

**Data Bindings:** Steam dump flow from `SteamDumpController`; feedwater data PLACEHOLDER.

---

#### **Stage 9: Auxiliary Systems Screen** (Screen 8)

**Files Created:**
- `Assets/Scripts/UI/AuxiliarySystemsScreen.cs` — Key=`Alpha8`, Index=8.
  - Left: RHR gauges, Right: CCW/SW gauges
  - Center: Auxiliary systems overview diagram
  - Bottom: RHR/CCW/SW pump controls (visual only)

**Data Bindings:** Almost entirely PLACEHOLDER — RHR/CCW/SW not modeled.

---

#### **Stage 10: Integration Testing & Documentation**

- Complete `MultiScreenBuilder.cs` with all screen creation methods
- Test all 9 screen transitions
- Test mutual exclusion, performance, data accuracy
- Create `Assets/Prefabs/Screens/README_Prefabs.md` with prefab save instructions
- Create GUI-to-ScreenLayout mapping summary document
- Write changelog

---

## 4. Unaddressed Issues

### 4.1 Issues Planned for Future Release

| Issue | Reason | Future Version |
|-------|--------|----------------|
| Screen 9 (Safety Systems) | ECCS/SI models not implemented | Phase 7+ |
| Screen 0 (Electrical) | Electrical bus models not implemented | Phase 8+ |
| Individual loop resolution (4 independent loops) | Physics uses lumped single-loop model | Phase 9+ |
| Turbine-Generator detailed model | No turbine physics module yet | Phase 6+ |
| RHR/CCW/SW thermal models | Not yet implemented | v1.3.0+ |
| Active controls (buttons that trigger physics) | This implementation is display-only | Next GUI phase |
| MSIV/Valve position models | No valve models yet | Future |
| Feedwater heater train model | No FW heater physics | Future |
| 3D visualizations for Screens 3–8 | Only Screen 2 uses 3D model; others use procedural 2D | Future if needed |
| Player action map key 1/2 conflict | Keys 1/2 bound to Previous/Next in Player map — needs user rebinding | Stage 1 documentation |

### 4.2 Issues Not Addressed (Out of Scope)

| Issue | Reason |
|-------|--------|
| ReactorOperatorScreen refactor to inherit from OperatorScreen | GOLD STANDARD — not modified. Adapter pattern used instead. |
| ReactorOperatorScreen migration to new Input System | GOLD STANDARD — legacy Input calls are harmless (return false). Adapter handles input. |
| Multi-monitor support | Single 1920×1080 assumed per design doc |
| Touchscreen/VR support | Desktop only per design doc |
| Sound effects for alarms | Audio system not in current scope |

---

## 5. Dependencies and Prerequisites

### 5.1 Required (Already Exist)

- ✅ Unity New Input System (`com.unity.inputsystem: 1.17.0`) — installed and active
- ✅ `InputSystem_Actions.inputactions` — existing asset (Player + UI maps)
- ✅ `activeInputHandler: 1` — New Input System only in ProjectSettings
- ✅ `ScreenManager.cs`, `OperatorScreen.cs`, `ReactorOperatorScreen.cs` (GOLD)
- ✅ `RCSPrimaryLoopScreen.cs`, `MosaicGauge.cs`, `MosaicIndicator.cs`, `MosaicBoard.cs`
- ✅ `RCPControlPanel.cs`, `RCSVisualizationController.cs`, `RCSGaugeTypes.cs`
- ✅ All physics modules: `ReactorController`, `PressurizerPhysics`, `CVCSController`, `SGMultiNodeThermal`, `SteamDumpController`, `RCPSequencer`, `LoopThermodynamics`
- ✅ RCS Blender model: `Assets/Models/RCS/RCS_Primary_Loop.fbx`
- ✅ TextMeshPro package (used by RCS screen components)

### 5.2 GOLD STANDARD Modules — Not Modified

- `ReactorOperatorScreen.cs` — Screen 1 controller
- `ReactorKinetics.cs` — Neutron power calculations
- `ThermalMass.cs` — Coolant and metal heat capacity
- `LoopThermodynamics.cs` — RCS temperature calculations
- `PressurizerPhysics.cs` — Pressure and level control
- `CVCSController.cs` — Charging/letdown control
- `RCPSequencer.cs` — Pump start/stop logic

---

## 6. Files Modified Summary

| File | Change Type | Justification |
|------|-------------|---------------|
| `ScreenManager.cs` | **Modified** | Replace `Input.GetKeyDown()` with New Input System actions. Required because legacy input is disabled project-wide. |
| `OperatorScreen.cs` | **Modified** | Replace fallback `Input.GetKeyDown()` in `Update()` with New Input System. Required because legacy input is disabled. |
| `InputSystem_Actions.inputactions` | **Not modified** | New dedicated `ScreenInputActions.inputactions` created instead, to avoid disrupting existing Player/UI bindings. |
| `ReactorOperatorScreen.cs` | **NOT modified** | GOLD STANDARD. Adapter handles integration. |
| All physics modules | **NOT modified** | Read-only data access via `ScreenDataBridge`. |

---

## 7. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Input Action conflicts with Player map keys 1/2 | Medium | Medium | Dedicated action map with enable/disable control; document rebinding needed |
| Screen 1 adapter causes double-toggle | Low | Medium | Legacy Input.GetKeyDown already returns false; adapter is sole input handler |
| Procedural 2D diagrams take long to build | High | Medium | Start with simple rectangles + labels; iterate fidelity |
| Performance with 9 screens in memory | Low | High | Screens disabled via SetActive(false) when hidden |
| Missing data sources for placeholder gauges | Expected | Low | Clear `// PLACEHOLDER` marking; gauges show `---` |

---

## 8. Success Criteria

1. ✅ All 9 screens (1–8, Tab) accessible via keyboard shortcuts using New Input System
2. ✅ Mutual exclusion — only one screen visible at a time
3. ✅ All gauges with available data sources show live values
4. ✅ Placeholder gauges clearly marked in code and display `---`
5. ✅ Screen layout matches Operator_Screen_Layout_Plan_v1_0_0.md specifications
6. ✅ GOLD STANDARD files unmodified
7. ✅ ≥60 FPS with any screen active
8. ✅ No console errors during normal operation
9. ✅ Editor tool creates complete hierarchy in one click
10. ✅ No legacy `Input.GetKeyDown()` calls in any non-GOLD-STANDARD files

---

## 9. Approval Request

This implementation plan requires explicit user approval before proceeding.

**Key decisions for review:**
1. **10 stages, one at a time** — Foundation → RCS Screen 2 → Plant Overview (Tab) → Screens 3–8 → Integration
2. **New Input System migration** — `ScreenManager.cs` and `OperatorScreen.cs` modified to use Input Actions (required since legacy input is disabled)
3. **Dedicated `ScreenInputActions.inputactions`** — Separate from existing `InputSystem_Actions.inputactions` to avoid disrupting Player/UI maps
4. **Adapter pattern for GOLD STANDARD Screen 1** — `ReactorScreenAdapter.cs` bridges to `ScreenManager` without touching `ReactorOperatorScreen.cs`
5. **All scripts in `Assets/Scripts/UI/`** — Same folder as existing UI scripts
6. **Placeholder data binding** — Gauges show `---` when physics source unavailable
7. **2D procedural diagrams** for all screens except Screen 2 (which uses 3D model)

---

**END OF IMPLEMENTATION PLAN**

**Ready for User Review and Approval**
