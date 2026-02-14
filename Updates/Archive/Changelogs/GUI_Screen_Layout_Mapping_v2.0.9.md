# GUI-to-Screen Layout Mapping Summary

**Version:** 2.0.9
**Date:** 2026-02-10
**Source:** `IMPLEMENTATION_PLAN_v2.0.0_MultiScreenGUI.md` + `Operator_Screen_Layout_Plan_v1.0.0.md`

---

## Screen Key Assignments

| Key | Index | Screen Name | Class | Status |
|-----|-------|------------|-------|--------|
| `1` | 1 | Reactor Core | `ReactorOperatorScreen` | ✅ Complete |
| `2` | 2 | RCS Primary Loop | `RCSLoopScreen` | ✅ Complete |
| `3` | 3 | Pressurizer | `PressurizerScreen` | ✅ Complete |
| `4` | 4 | CVCS | `CVCSScreen` | ✅ Complete |
| `5` | 5 | Steam Generators | `SteamGeneratorScreen` | ✅ Complete |
| `6` | 6 | Turbine-Generator | `TurbineGeneratorScreen` | ✅ Complete |
| `7` | 7 | Secondary Systems | `SecondarySystemsScreen` | ✅ Complete |
| `8` | 8 | Auxiliary Systems | `AuxiliarySystemsScreen` | ✅ Complete |
| `Tab` | 0 | Plant Overview | `PlantOverviewScreen` | ✅ Complete |
| `9` | — | Safety Systems | — | ⚪ Future (v3.0.0+) |
| `0` | — | Electrical Distribution | — | ⚪ Future (v3.0.0+) |

---

## Standard Layout Template (All Screens)

```
┌────────────────────────────────────────────────────────────────────┐
│                          1920 x 1080                               │
├──────┬──────────────────────────────────────────────────┬──────────┤
│      │                                                  │          │
│ LEFT │           CENTER PANEL (15-65%)                  │  RIGHT   │
│PANEL │     Central Visual / Diagram / Schematic         │  PANEL   │
│(0-15)│                                                  │ (65-100) │
│      │                                                  │          │
│ 8    │     Title + Subtitle                             │  8       │
│gauges│     Equipment diagrams                           │  gauges  │
│      │     Flow paths                                   │          │
│      │     Status overlays                              │          │
├──────┴──────────────────────────────────────────────────┴──────────┤
│                    BOTTOM PANEL (0-26% height)                     │
│  Controls | Indicators | Status | Sim Time | Alarms               │
└────────────────────────────────────────────────────────────────────┘
```

---

## Per-Screen Center Panel Visualization

| Screen | Center Visual | Description |
|--------|--------------|-------------|
| 1 Reactor Core | Core cross-section | Fuel assemblies, control rods, power distribution |
| 2 RCS Loop | 4-loop schematic | Hot/cold legs, SGs, RCPs, reactor vessel, PZR |
| 3 Pressurizer | Vessel cutaway | Water level fill, steam space, heaters, spray, PORV/SV |
| 4 CVCS | Flow diagram | VCT → CCP → Seal Inj → RCS → Letdown → HX → Demins → VCT |
| 5 Steam Generators | Quad-SG 2×2 | 4 SG cells with tube bundle, level fill, steam dome |
| 6 Turbine-Generator | Shaft train | Steam Admission → HP → MSR → LP-A → LP-B → Generator |
| 7 Secondary Systems | Secondary cycle | Condenser → Pumps → FWH → DA → FWP → HPH → SGs → MSH |
| 8 Auxiliary Systems | Aux overview | RCS → RHR Trains A/B → CCW Header → CCW HXs → SW |
| Tab Plant Overview | Plant mimic | Simplified full-plant (reactor, PZR, SGs, turbine, gen) |

---

## Data Source Summary

| Screen | Live Data Sources | Primary PLACEHOLDER Systems |
|--------|------------------|---------------------------|
| 1 | ReactorKinetics, ControlRodController | Startup rate |
| 2 | LoopThermodynamics, RCPSequencer | Per-loop flow differentiation |
| 3 | PressurizerPhysics | Spray flow, heater output, PORV/SV state |
| 4 | CVCSController, VCTPhysics | Seal injection, boration flow, letdown HX |
| 5 | SGSecondaryThermal, LoopThermodynamics | SG level, FW flow, per-SG differentiation |
| 6 | Steam pressure only | Entire turbine-generator system |
| 7 | SteamDumpController, steam pressure | Feedwater train, MSIVs, condensate |
| 8 | None | RHR, CCW, Service Water (all systems) |
| Tab | Multiple (aggregate) | Secondary output gauges, turbine/generator |

---

## Builder Architecture

**File:** `MultiScreenBuilder.cs` (v2.0.9, ~4500 lines)

**Menu:** Critical > Create All Operator Screens

**Build Order:**
1. Canvas + ScreenManager + EventSystem
2. Screen 1 — Reactor Core (ReactorOperatorScreen)
3. Screen 2 — RCS Primary Loop
4. Screen Tab — Plant Overview
5. Screen 3 — Pressurizer
6. Screen 4 — CVCS
7. Screen 5 — Steam Generators
8. Screen 6 — Turbine-Generator
9. Screen 7 — Secondary Systems
10. Screen 8 — Auxiliary Systems

**Shared Helpers:** CreatePanel, CreateTMPLabel, CreateTMPText, CreateTMPButton, CreateMimicBox, CreateOverviewGaugeItem, CreateTMPSectionLabel

---

## Testing Checklist

### Navigation
- [ ] Key 1 → Reactor Core screen appears
- [ ] Key 2 → RCS Loop screen appears
- [ ] Key 3 → Pressurizer screen appears
- [ ] Key 4 → CVCS screen appears
- [ ] Key 5 → Steam Generator screen appears
- [ ] Key 6 → Turbine-Generator screen appears
- [ ] Key 7 → Secondary Systems screen appears
- [ ] Key 8 → Auxiliary Systems screen appears
- [ ] Tab → Plant Overview screen appears
- [ ] Only one screen visible at a time (mutual exclusion)
- [ ] Pressing active screen key toggles it off
- [ ] Rapid key switching — no visual artifacts

### Data Accuracy
- [ ] All live gauges update at correct rate (10 Hz gauges, 2 Hz diagrams)
- [ ] Placeholder gauges show "---" in gray
- [ ] Status fields (reactor mode, sim time, time compression) update on all screens
- [ ] Temperature color gradients correct (blue → red)
- [ ] Equipment state colors correct (green=running, gray=stopped, red=tripped)

### Performance
- [ ] Maintain ≥60 FPS with any screen visible
- [ ] No memory leaks from repeated screen toggling
- [ ] Screen transitions < 100ms

### Controls (Visual Only)
- [ ] All buttons render correctly
- [ ] All indicators show correct initial state
- [ ] No unintended side effects from button clicks on visual-only controls
