# Changelog — Multi-Screen GUI Implementation

**Implementation Plan:** `IMPLEMENTATION_PLAN_v2.0.0_MultiScreenGUI.md`
**Version:** 2.0.9
**Date:** 2026-02-10

---

## [2.0.9] — 2026-02-10 — Stage 9: Auxiliary Systems Screen

### Added
- `AuxiliarySystemsScreen.cs` — Screen 8 (Key 8, Index 8), entirely PLACEHOLDER
  - Left Panel: 8 RHR system gauges (RHR-A/B flow, HX inlet/outlet temps, suction pressure, pump status)
  - Center Panel: Auxiliary systems diagram (RCS connection, RHR Trains A/B with pumps + HXs, CCW header/HXs/pumps, SW header/pumps, three "NOT MODELED" overlays, warning banner)
  - Right Panel: 8 cooling water gauges (CCW supply/return P, surge tank level, CCW temp, SW flow/temp, thermal barrier flow, CCW heat load)
  - Bottom Panel: RHR pump controls (Train A/B start/stop, indicators), CCW/SW pump controls, status section with RHR entry conditions note, alarm container
- `MultiScreenBuilder.cs` v2.0.9 — Added `CreateAuxiliarySystemsScreen()` + 4 builder methods (~434 lines)
- `FUTURE_ENHANCEMENTS_ROADMAP.md` — Screen 8 placeholder section (16 items)

---

## [2.0.8] — 2026-02-10 — Stage 8: Secondary Systems Screen

### Added
- `SecondarySystemsScreen.cs` — Screen 7 (Key 7, Index 7)
  - Left Panel: 8 feedwater train gauges (all PLACEHOLDER)
  - Center Panel: Secondary cycle flow diagram (Condenser → Cond Pumps → LP FWH 1-3 → Deaerator → MFW Pumps → HP FWH 4-6 → FW to SGs; SGs → MSIVs → Main Steam Header → Steam Dump/Turbine)
  - Right Panel: 8 steam system gauges (Main Steam Pressure live, Steam Dump live, MSIVs assumed OPEN, others PLACEHOLDER)
  - Bottom Panel: Steam dump controls (Auto/Manual, mode, demand, heat — live from SteamDumpController), MSIV controls (Open/Close All), status, alarms
- `MultiScreenBuilder.cs` v2.0.8 — Added `CreateSecondarySystemsScreen()` + 4 builder methods (~380 lines)
- `FUTURE_ENHANCEMENTS_ROADMAP.md` — Screen 7 placeholder section (13 items)

---

## [2.0.7] — 2026-02-10 — Stage 7: Turbine-Generator Screen

### Added
- `TurbineGeneratorScreen.cs` — Screen 6 (Key 6, Index 6)
  - Left Panel: 8 turbine performance gauges (HP Inlet Pressure live, 7 PLACEHOLDER)
  - Center Panel: Shaft train diagram (Steam Admission → HP Turbine → MSR → LP-A → LP-B → Generator, Condenser below, "NOT MODELED" overlay)
  - Right Panel: 8 generator output gauges (all PLACEHOLDER)
  - Bottom Panel: Turbine trip button/indicator (always TRIPPED), generator breaker controls (always OPEN), status, alarms
- `MultiScreenBuilder.cs` v2.0.7 — Added `CreateTurbineGeneratorScreen()` + 4 builder methods (~365 lines)
- `FUTURE_ENHANCEMENTS_ROADMAP.md` — Screen 6 placeholder section (15 items — most placeholder-heavy)

---

## [2.0.6] — 2026-02-10 — Stage 6: Steam Generator Screen

### Added
- `SteamGeneratorScreen.cs` — Screen 5 (Key 5, Index 5)
  - Left Panel: 8 primary side gauges (SG-A/B/C/D inlet/outlet temps — live via T_hot/T_cold, lumped identical)
  - Center Panel: Quad-SG 2×2 layout (shell, tube bundle with temp gradient, water level fill, steam dome, hot/cold leg inlets, FW/steam labels, overlay text per SG)
  - Right Panel: 8 secondary side gauges (SG-A/B/C/D level PLACEHOLDER, steam pressure live — lumped identical)
  - Bottom Panel: Heat transfer section (total heat removal, steaming status, secondary temp, circulation fraction), flow section (FW/steam flow PLACEHOLDER), status, alarms
- `MultiScreenBuilder.cs` v2.0.6 — Added `CreateSteamGeneratorScreen()` + builder methods (~408 lines)
- `FUTURE_ENHANCEMENTS_ROADMAP.md` — Screen 5 placeholder section (6 items)

---

## [2.0.5] — 2026-02-10 — Stage 5: CVCS Screen

### Added
- `CVCSScreen.cs` — Screen 4 (Key 4, Index 4)
  - Left Panel: 8 charging/letdown gauges (charging flow live, letdown flow live, VCT level live, others PLACEHOLDER)
  - Center Panel: CVCS flow diagram (VCT → CCP → Seal Injection → RCS → Letdown → Letdown HX → Demins → VCT, boration/dilution paths)
  - Right Panel: 8 chemistry/boron gauges (boron concentration live, boron worth PLACEHOLDER, others PLACEHOLDER)
  - Bottom Panel: Boration/dilution controls, makeup controls, status, alarms
- `MultiScreenBuilder.cs` v2.0.5 — Added CVCS builder region

---

## [2.0.4] — 2026-02-10 — Stage 4: Pressurizer Screen

### Added
- `PressurizerScreen.cs` — Screen 3 (Key 3, Index 3)
  - Left Panel: 8 pressure gauges (RCS pressure live, PZR pressure live, spray flow PLACEHOLDER, heater output PLACEHOLDER, etc.)
  - Center Panel: Pressurizer vessel visualization (water level fill, steam space, heater zone, spray nozzle, PORV/SV indicators, surge line)
  - Right Panel: 8 level/temp gauges (PZR level live, PZR temp live, subcooling live, others PLACEHOLDER)
  - Bottom Panel: Heater controls, spray controls, PORV controls, status, alarms
- `MultiScreenBuilder.cs` v2.0.4 — Added Pressurizer builder region

---

## [2.0.3] — 2026-02-10 — Stage 3: Plant Overview Screen

### Added
- `PlantOverviewScreen.cs` — Screen Tab (ToggleKey Tab, Index 0)
  - Left Panel: 8 nuclear/primary gauges (reactor power, T-avg, RCS pressure, PZR level, RCS flow, rod position, boron, xenon)
  - Center Panel: Simplified plant mimic diagram (reactor, PZR, 4 SGs, turbine, generator, condenser)
  - Right Panel: 8 secondary/output gauges (SG level PLACEHOLDER, steam pressure, FW flow PLACEHOLDER, turbine/generator power PLACEHOLDER, condenser vacuum PLACEHOLDER, FW temp PLACEHOLDER, steam flow PLACEHOLDER)
  - Bottom Panel: Reactor mode, RCP status, turbine/generator status, alarm summary, trip buttons
- `MultiScreenBuilder.cs` v2.0.3 — Added Plant Overview builder region

---

## [2.0.2] — 2026-02-10 — Stage 2: RCS Primary Loop Screen

### Added
- `RCSLoopScreen.cs` — Screen 2 (Key 2, Index 2)
  - Left Panel: 8 RCS temperature gauges (T-hot, T-cold, T-avg, delta-T, all live)
  - Center Panel: 4-loop schematic (hot legs, cold legs, SGs, RCPs, reactor vessel, pressurizer, color-coded by temperature)
  - Right Panel: 8 RCS flow/pressure gauges (RCS pressure live, PZR level live, flow PLACEHOLDER per-loop)
  - Bottom Panel: 4 RCP controls (start/stop buttons, indicators, status), status section, alarms
- `MultiScreenBuilder.cs` v2.0.2 — Added RCS Loop builder region

---

## [2.0.1] — 2026-02-10 — Stage 1: Screen Management Framework

### Added
- `ScreenManager.cs` — Singleton managing multi-screen navigation (Keys 1-8, Tab)
- `OperatorScreen.cs` — Abstract base class for all operator screens
- `ScreenInputActions.cs` — ScriptableObject for input configuration
- `MultiScreenBuilder.cs` v2.0.1 — Editor menu "Critical > Create All Operator Screens"
- Refactored `ReactorOperatorScreen.cs` to inherit from `OperatorScreen` (Key 1, Index 1)

### Architecture
- Single Canvas hierarchy with CanvasGroup-based visibility
- Mutual exclusion enforced by ScreenManager
- SerializedObject wiring for all Inspector fields
- Consistent 4-panel layout (Left/Center/Right/Bottom) across all screens

---

## Summary

| Screen | Key | File | Live Gauges | Placeholder Gauges |
|--------|-----|------|-------------|-------------------|
| 1. Reactor Core | 1 | ReactorOperatorScreen.cs | ~12 | ~4 |
| 2. RCS Primary Loop | 2 | RCSLoopScreen.cs | ~8 | ~8 |
| 3. Pressurizer | 3 | PressurizerScreen.cs | ~6 | ~10 |
| 4. CVCS | 4 | CVCSScreen.cs | ~5 | ~11 |
| 5. Steam Generators | 5 | SteamGeneratorScreen.cs | ~10 | ~6 |
| 6. Turbine-Generator | 6 | TurbineGeneratorScreen.cs | ~1 | ~15 |
| 7. Secondary Systems | 7 | SecondarySystemsScreen.cs | ~3 | ~13 |
| 8. Auxiliary Systems | 8 | AuxiliarySystemsScreen.cs | 0 | 16 |
| Plant Overview | Tab | PlantOverviewScreen.cs | ~8 | ~8 |
| **Total** | | | **~53** | **~91** |

### Files Created (Stages 1–9)
- `ScreenManager.cs`
- `OperatorScreen.cs`
- `ScreenInputActions.cs`
- `MultiScreenBuilder.cs` (v2.0.1 → v2.0.9)
- `RCSLoopScreen.cs`
- `PlantOverviewScreen.cs`
- `PressurizerScreen.cs`
- `CVCSScreen.cs`
- `SteamGeneratorScreen.cs`
- `TurbineGeneratorScreen.cs`
- `SecondarySystemsScreen.cs`
- `AuxiliarySystemsScreen.cs`

### Files Modified
- `ReactorOperatorScreen.cs` — Refactored to inherit from OperatorScreen
- `FUTURE_ENHANCEMENTS_ROADMAP.md` — Placeholder tracking for all screens
