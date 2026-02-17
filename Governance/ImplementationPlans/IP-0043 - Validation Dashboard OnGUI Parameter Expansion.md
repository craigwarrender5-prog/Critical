# IP-0043: Validation Dashboard — OnGUI Parameter Expansion

**Date:** 2026-02-17  
**Domain Plan:** DP-0008 (Operator Interface & Scenarios)  
**Predecessors:** IP-0042 (SUPERSEDED)  
**Status:** DRAFT — Awaiting Approval  
**Priority:** High  
**Changelog Required:** Yes  
**Target Version:** v0.8.0.0

---

## 1. Executive Summary

Following the failure of IP-0042 (UI Toolkit rebuild) due to Unity 6's `Painter2D.Arc()` method not rendering correctly, this IP takes a pragmatic approach: **enhance the existing GOLD-standard OnGUI dashboard** rather than attempt another technology migration.

The legacy `HeatupValidationVisual` dashboard:
- Works reliably
- Is GOLD-standard certified
- Uses proven rendering (IMGUI)
- Has comprehensive tab structure

This IP will expand parameter coverage within the existing architecture.

---

## 2. Problem Summary

### 2.1 Current Coverage Gap

The comprehensive parameter requirements document specifies ~129 parameters. Current dashboard displays ~75 prominently. Key gaps:

| Category | Missing Parameters |
|----------|-------------------|
| Global Sim Health | Net plant heat balance, energy conservation error |
| Pressurizer | Heater limiter flags, spray details, heater ramp rate |
| CVCS | Letdown orifice states, individual orifice counts |
| VCT | Temperature, pressure, gas blanket, inflow/outflow rates |
| BRS | Tank level, boron concentration, return flow |
| RHR | Suction source, discharge path details |
| Mass Conservation | Primary mass ledger, drift tracking, boundary errors |

### 2.2 Why OnGUI Enhancement

| Approach | Risk | Effort | Proven |
|----------|------|--------|--------|
| UI Toolkit (IP-0042) | HIGH - Arc rendering broken in Unity 6 | High | No |
| uGUI (IP-0025-0041) | HIGH - 6 failed attempts | High | No |
| **OnGUI Enhancement** | **LOW - existing code works** | **Medium** | **Yes** |

---

## 3. Implementation Strategy

### 3.1 Approach

Enhance the existing `HeatupValidationVisual` partial classes by:
1. Adding missing parameters to appropriate tabs
2. Expanding the CRITICAL tab with additional telemetry
3. Adding a new SYSTEMS tab for BRS/VCT/Mass Conservation details
4. Improving the always-visible parameter density on Overview

### 3.2 File Structure

The dashboard uses partial classes:
```
HeatupValidationVisual.cs           — Core, lifecycle, tab routing
HeatupValidationVisual.TabOverview.cs    — Overview tab
HeatupValidationVisual.TabPrimary.cs     — RCS Primary tab  
HeatupValidationVisual.TabPressurizer.cs — PZR tab
HeatupValidationVisual.TabCVCS.cs        — CVCS tab
HeatupValidationVisual.TabSGRHR.cs       — SG/RHR tab
HeatupValidationVisual.TabCritical.cs    — CRITICAL tab
HeatupValidationVisual.Graphs.cs         — Graph rendering
HeatupValidationVisual.Annunciators.cs   — Annunciator panel
```

---

## 4. Parameter Additions by Tab

### 4.1 CRITICAL Tab Additions

| Parameter | Engine Field | Display Type |
|-----------|-------------|--------------|
| Net Plant Heat | `netPlantHeat_MW` | Gauge + value |
| Heater Limiter Active | `heaterPressureRateClampActive` | LED |
| Heater Ramp Limited | `heaterRampRateClampActive` | LED |
| Heater Limiter Reason | `heaterLimiterReason` | Text |
| Spray Flow | `sprayFlow_GPM` | Value |
| Spray Valve Position | `sprayValvePosition` | Bar gauge |
| Spray Steam Condensed | `spraySteamCondensed_lbm` | Value |
| Primary Mass Drift | `primaryMassDrift_lb` | Value + alarm color |
| Primary Mass Status | `primaryMassStatus` | Text |

### 4.2 New SYSTEMS Tab

Create new tab for auxiliary system details:

**VCT Section:**
| Parameter | Engine Field |
|-----------|-------------|
| VCT Temperature | `vctState.Temperature_F` |
| VCT Pressure | `vctState.Pressure_psia` |
| VCT Gas Blanket | `vctState.GasBlanketPressure_psia` |
| VCT Inflow | `vctState.Inflow_gpm` |
| VCT Outflow | `vctState.Outflow_gpm` |

**BRS Section:**
| Parameter | Engine Field |
|-----------|-------------|
| BRS Holdup Level | `BRSPhysics.GetHoldupLevelPercent(brsState)` |
| BRS Distillate Level | `BRSPhysics.GetDistillateLevelPercent(brsState)` |
| BRS Inflow | `brsState.InFlow_gpm` |
| BRS Return Flow | `brsState.ReturnFlow_gpm` |
| BRS Boron Conc | `brsState.BoronConcentration_ppm` |

**Letdown Orifices Section:**
| Parameter | Engine Field |
|-----------|-------------|
| 75 gpm Orifice Count | `orifice75Count` |
| 45 gpm Orifice Open | `orifice45Open` |
| Orifice Lineup | `orificeLineupDesc` |
| Letdown via RHR | `letdownViaRHR` |
| Letdown via Orifice | `letdownViaOrifice` |

**Mass Conservation Section:**
| Parameter | Engine Field |
|-----------|-------------|
| Primary Mass Ledger | `primaryMassLedger_lb` |
| Primary Mass Expected | `primaryMassExpected_lb` |
| Primary Mass Drift | `primaryMassDrift_lb` |
| Primary Mass Drift % | `primaryMassDrift_pct` |
| Boundary Error | `primaryMassBoundaryError_lb` |
| Conservation OK | `primaryMassConservationOK` |
| Mass Alarm | `primaryMassAlarm` |

### 4.3 Overview Tab Enhancements

Add to footer/status area:
- Net Plant Heat balance indicator
- Mass conservation status LED
- Active alarm count

---

## 5. Implementation Stages

### Stage 1: CRITICAL Tab Expansion
**Objective:** Add heater control details, spray system, and mass conservation to CRITICAL tab.

**Tasks:**
1. Add heater limiter section (3 LEDs + reason text)
2. Add spray system section (flow, valve position bar, condensed mass)
3. Add net plant heat gauge
4. Add primary mass drift with alarm coloring
5. Test all new parameters update correctly

**Files Modified:**
- `HeatupValidationVisual.TabCritical.cs`

**Exit Criteria:** All new parameters display and update at 10Hz.

---

### Stage 2: New SYSTEMS Tab
**Objective:** Create dedicated tab for VCT, BRS, Letdown Orifices, and Mass Conservation.

**Tasks:**
1. Add new tab constant and routing in core file
2. Create `HeatupValidationVisual.TabSystems.cs`
3. Implement VCT detail section
4. Implement BRS detail section
5. Implement Letdown Orifices section
6. Implement Mass Conservation audit section
7. Add keyboard shortcut (Ctrl+8 or similar)

**Files Created:**
- `HeatupValidationVisual.TabSystems.cs`

**Files Modified:**
- `HeatupValidationVisual.cs` (tab routing)

**Exit Criteria:** New tab accessible, all parameters display correctly.

---

### Stage 3: Overview Enhancements
**Objective:** Improve always-visible parameter density.

**Tasks:**
1. Add net plant heat to Overview status bar
2. Add mass conservation status LED
3. Add active alarm count badge
4. Review parameter layout density

**Files Modified:**
- `HeatupValidationVisual.TabOverview.cs`

**Exit Criteria:** Key health indicators visible without tab switching.

---

### Stage 4: Documentation and Cleanup
**Objective:** Complete documentation and remove failed UI Toolkit code.

**Tasks:**
1. Write changelog (CHANGELOG_v0.8.0.0.md)
2. Archive UI Toolkit test files (don't delete, move to `_Archive`)
3. Update IP-0042 with failure notes
4. Update Future_Features.md

**Deliverables:**
- CHANGELOG_v0.8.0.0.md
- Archived UI Toolkit code

---

## 6. File Manifest

### New Files
| File | Purpose |
|------|---------|
| `HeatupValidationVisual.TabSystems.cs` | New SYSTEMS detail tab |

### Modified Files
| File | Changes |
|------|---------|
| `HeatupValidationVisual.cs` | Add SYSTEMS tab routing |
| `HeatupValidationVisual.TabCritical.cs` | Add heater/spray/mass params |
| `HeatupValidationVisual.TabOverview.cs` | Add status indicators |

### Archived Files
| File | Destination |
|------|-------------|
| `Assets/Scripts/UI/UIToolkit/*` | `Assets/Scripts/UI/_Archive/UIToolkit/` |
| `Assets/UI/*` | `Assets/UI/_Archive/` |

---

## 7. Success Criteria

- [ ] CRITICAL tab displays 9 new parameters
- [ ] New SYSTEMS tab displays ~20 parameters across 4 sections
- [ ] Overview shows net plant heat and mass status
- [ ] All parameters update at 10Hz without performance degradation
- [ ] No regressions in existing dashboard functionality
- [ ] Changelog complete

---

## 8. Risk Assessment

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Layout crowding | Medium | Use collapsible sections, smaller fonts for secondary params |
| Performance impact | Low | OnGUI is proven performant; minimal new calculations |
| Missing engine fields | Low | Gap analysis confirmed all fields exist |

---

## 9. Lessons Learned from IP-0042

1. **Unity 6 UI Toolkit is not mature** — `Painter2D.Arc()` doesn't render
2. **Validation gates work** — Stage 0 caught the issue before major investment
3. **Proven technology wins** — OnGUI works, has always worked
4. **Don't fight the framework** — If the rendering API is broken, no amount of code will fix it

---

## 10. Approval

- [ ] **IP-0043 approved to begin** — Craig
- [ ] **Stage 1 complete** — Craig
- [ ] **Stage 2 complete** — Craig
- [ ] **Stage 3 complete** — Craig
- [ ] **IP-0043 closed** — Craig

---

*Implementation Plan prepared by Claude*  
*Date: 2026-02-17*
