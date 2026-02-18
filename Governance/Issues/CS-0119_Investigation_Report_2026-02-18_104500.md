# CS-0119: Operator Screen Updates for Condenser/Feedwater Integration + MultiScreenBuilder Refactoring

**Date:** 2026-02-18  
**Status:** OPEN  
**Priority:** High  
**Category:** UI / Operator Screens / Code Quality  
**Amended:** 2026-02-18 (Screen 1 architecture clarification + Plant Overview analysis)

---

## Summary

This investigation identifies three related issues:

1. **PlantOverviewScreen.cs (Tab)** has gauge text wired to live Condenser/Feedwater data, but mimic diagram components are not animated.

2. **SecondarySystemsScreen.cs (Screen 7)** displays PLACEHOLDER values for Condenser/Feedwater parameters despite the data now being available via `ScreenDataBridge` (IP-0046, CS-0115/CS-0116 implementations).

3. **MultiScreenBuilder.cs** at **215 KB** violates GOLD standard maintainability guidelines and requires refactoring into smaller, focused partial classes or builder modules.

---

## IMPORTANT: Screen 1 Architecture Clarification

**Screen 1 (Reactor Core) is architecturally separate and will NOT be affected by any refactoring.**

From `MultiScreenBuilder.cs` (v4.1.0):

```csharp
// v4.1.0: Delegate to OperatorScreenBuilder.BuildScreen1() — single source of truth
ReactorOperatorScreen screen = OperatorScreenBuilder.BuildScreen1(canvasParent);
screen.StartVisible = true;

// v4.1.0: All BuildScreen1_* methods removed.
// Screen 1 is now built by OperatorScreenBuilder.BuildScreen1() — single source of truth.
// This eliminates ~350 lines of duplicated code and ensures all screens use
// the same TMP fonts, materials, and sprite backgrounds.
```

| Component | Location | Impact |
|-----------|----------|--------|
| Screen 1 (Reactor Core) | `OperatorScreenBuilder.cs` (51 KB) | **NO CHANGES** |
| Blender background/skin | `ReactorOperatorScreenSkin.cs` | **NO CHANGES** |
| Screens 2-8 + helpers | `MultiScreenBuilder.cs` (215 KB) | Refactoring target |

The 215 KB in `MultiScreenBuilder.cs` consists entirely of Screens 2-8, Plant Overview, and shared helper methods. The Reactor screen with its Blender background is completely separate.

---

## Part A: Operator Screen Gap Analysis

### Screens Requiring Updates

Two operator screens need updates for Condenser/Feedwater integration:

| Screen | Key | Current State | Work Required |
|--------|-----|---------------|---------------|
| Plant Overview | Tab | Partial - gauges wired, mimic not animated | **Low** |
| Secondary Systems | 7 | PLACEHOLDERs throughout | **Medium** |

---

### A.1: Plant Overview Screen (Tab)

The Plant Overview screen has **partial IP-0046 integration** - the gauge text fields are wired to live data, but mimic diagram components are not animated.

#### Already Updated (IP-0046 CS-0115):

| Field | Data Source | Status |
|-------|-------------|--------|
| `text_CondenserVacuum` | `_data.GetCondenserVacuum_inHg()` | ✅ Live with C-9 color coding |
| `text_FeedwaterFlow` | `_data.GetFeedwaterReturnFlow_lbhr()` | ✅ Live |
| `text_FeedwaterTemp` | Calculated from backpressure saturation | ✅ Live |

#### Still PLACEHOLDER (No Turbine Model - Expected):

| Field | Current Value | Notes |
|-------|---------------|-------|
| `text_TurbinePower` | "---" | No turbine model exists |
| `text_GeneratorOutput` | "---" | No generator model exists |
| `text_MainSteamFlow` | "---" | No main steam flow model |
| `indicator_TurbineStatus` | COLOR_STOPPED | Expected until turbine model added |
| `text_TurbineStatus` | "TURBINE: OFF" | Expected until turbine model added |

#### Missing Updates (Data Exists, Not Wired):

| Component | Current State | Required Update |
|-----------|---------------|-----------------|
| `mimic_Condenser` | Static gray color | Color blue/green when C-9 satisfied |
| `mimic_FeedwaterLine` | Static gray color | Animate cyan when FW return flow > 0 |
| CST Level | Not displayed | Add to right panel or status section |
| C-9 Indicator | Color on vacuum text only | Consider dedicated LED indicator |
| Steam Dump Status | Not displayed | Add indicator when steam dump active |

#### Required Code Changes for PlantOverviewScreen.cs:

**1. Add condenser/feedwater animation to `UpdateMimicDiagram()`:**

```csharp
private void UpdateMimicDiagram()
{
    UpdateReactorVessel();
    UpdatePressurizer();
    UpdateRCSLoops();
    UpdateSteamGenerators();
    UpdateTurbineGenerator();
    UpdateCondenserFeedwater();  // ADD THIS
}

private void UpdateCondenserFeedwater()
{
    // Condenser color based on C-9 status
    if (mimic_Condenser != null)
    {
        bool c9 = _data.GetC9CondenserAvailable();
        mimic_Condenser.color = c9 ? 
            new Color(0.2f, 0.4f, 0.6f) :  // Blue-gray when vacuum established
            COLOR_STOPPED;
    }
    
    // Feedwater line animation
    if (mimic_FeedwaterLine != null)
    {
        float fwFlow = _data.GetFeedwaterReturnFlow_lbhr();
        bool flowing = !float.IsNaN(fwFlow) && fwFlow > 100f;
        mimic_FeedwaterLine.color = flowing ? 
            new Color(0.2f, 0.6f, 1f, 0.8f) :  // Cyan when flowing
            COLOR_STOPPED;
    }
}
```

**2. (Optional) Add CST level to right panel gauges**

**Effort: Low** — Only requires adding one method call and ~15 lines of code.

---

### A.2: Secondary Systems Screen (Key 7)

#### Current Screen Structure

| Key | Screen Name | Component | Builder Location | Status |
|-----|-------------|-----------|------------------|--------|
| 1 | Reactor Core | `ReactorOperatorScreen` | `OperatorScreenBuilder.cs` | GOLD STANDARD ✓ |
| 2 | RCS Primary Loop | `RCSPrimaryLoopScreen` | `MultiScreenBuilder.cs` | Active |
| Tab | Plant Overview | `PlantOverviewScreen` | `MultiScreenBuilder.cs` | **Partial** |
| 3 | Pressurizer | `PressurizerScreen` | `MultiScreenBuilder.cs` | Active |
| 4 | CVCS | `CVCSScreen` | `MultiScreenBuilder.cs` | Active |
| 5 | Steam Generators | `SteamGeneratorScreen` | `MultiScreenBuilder.cs` | Active |
| 6 | Turbine-Generator | `TurbineGeneratorScreen` | `MultiScreenBuilder.cs` | Placeholder |
| 7 | Secondary Systems | `SecondarySystemsScreen` | `MultiScreenBuilder.cs` | **PARTIAL - Needs Update** |
| 8 | Auxiliary Systems | `AuxiliarySystemsScreen` | `MultiScreenBuilder.cs` | Placeholder |

#### SecondarySystemsScreen.cs Current State

The screen exists and has UI fields defined for feedwater parameters, but `UpdateLeftPanelGauges()` sets all values to PLACEHOLDER:

```csharp
private void UpdateLeftPanelGauges()
{
    // All feedwater train gauges — PLACEHOLDER
    SetPlaceholder(text_HotwellLevel);
    SetPlaceholder(text_CondPumpDischP);
    SetPlaceholder(text_DeaeratorPressure);
    SetPlaceholder(text_DeaeratorLevel);
    SetPlaceholder(text_FWPumpSuctionP);
    SetPlaceholder(text_FWPumpDischP);
    SetPlaceholder(text_FinalFWTemp);
    SetPlaceholder(text_FWFlowTotal);
}
```

#### ScreenDataBridge Already Has Condenser/Feedwater Getters

From `ScreenDataBridge.cs` (Section: IP-0046 CS-0115):

| Getter | Returns | Source |
|--------|---------|--------|
| `GetCondenserVacuum_inHg()` | Condenser vacuum (in. Hg) | `heatupEngine.condenserVacuum_inHg` |
| `GetCondenserBackpressure_psia()` | Condenser backpressure (psia) | `heatupEngine.condenserBackpressure_psia` |
| `GetC9CondenserAvailable()` | C-9 interlock status | `heatupEngine.condenserC9Available` |
| `GetCondenserPulldownPhase()` | Pulldown phase string | `heatupEngine.condenserPulldownPhase` |
| `GetHotwellLevel_pct()` | Hotwell level (%) | `heatupEngine.hotwellLevel_pct` |
| `GetCSTLevel_pct()` | CST level (%) | `heatupEngine.cstLevel_pct` |
| `GetFeedwaterReturnFlow_lbhr()` | FW return flow (lb/hr) | `heatupEngine.feedwaterReturnFlow_lbhr` |
| `GetSteamDumpBridgeState()` | Bridge FSM state | `heatupEngine.steamDumpBridgeState` |
| `GetSteamDumpPermitted()` | Steam dump permissive | `heatupEngine.steamDumpPermitted` |
| `GetPermissiveStatusMessage()` | Status message | `heatupEngine.permissiveStatusMessage` |

#### Required Screen Updates

**SecondarySystemsScreen.cs** needs modification to:

1. **Replace PLACEHOLDER feedwater gauges with live data:**
   - `text_HotwellLevel` → `_data.GetHotwellLevel_pct()`
   - `text_FWFlowTotal` → `_data.GetFeedwaterReturnFlow_lbhr()` (convert to gpm)

2. **Add Condenser section to left or bottom panel:**
   - Condenser vacuum gauge (0-30 in. Hg scale)
   - Condenser backpressure readout
   - C-9 interlock LED
   - CW pump status (requires additional getter)
   - Pulldown phase indicator

3. **Add CST level with Tech Spec alarm:**
   - CST level gauge (prominent - critical for Tech Spec)
   - Visual alarm when `feedwaterState.CST_BelowTechSpec`

4. **Update flow diagram animations:**
   - Condenser block active when vacuum established
   - Condensate pump running indicator
   - FW flow lines active when return flow > 0

5. **Add startup permissives section:**
   - Steam dump permitted LED
   - P-12 bypass status
   - Bridge state display

#### Additional ScreenDataBridge Getters Needed

The following getters are not yet implemented but are available from `HeatupSimEngine`:

| Needed Getter | Source Field |
|---------------|--------------|
| `GetCWPumpsRunning()` | `condenserState.CW_PumpsRunning` |
| `GetCondensatePumpsRunning()` | `feedwaterState.CondensatePumpsRunning` |
| `GetMFPRunning()` | `feedwaterState.MFP_Running` |
| `GetAFWMotorPumpsRunning()` | `feedwaterState.AFW_MotorPumpsRunning` |
| `GetAFWTurbinePumpRunning()` | `feedwaterState.AFW_TurbinePumpRunning` |
| `GetFeedwaterAvailable()` | `feedwaterState.FeedwaterAvailable` |
| `GetCSTBelowTechSpec()` | `feedwaterState.CST_BelowTechSpec` |
| `GetHotwellTemp_F()` | `feedwaterState.HotwellTemp_F` |
| `GetP12Bypassed()` | `permissiveState.P12Bypassed` |

---

## Part B: MultiScreenBuilder.cs Refactoring

### Current State

| File | Size | Content | Status |
|------|------|---------|--------|
| `OperatorScreenBuilder.cs` | 51.16 KB | Screen 1 (Reactor) only | ✓ Separate, no changes needed |
| `MultiScreenBuilder.cs` | 215.33 KB | Screens 2-8 + Overview + helpers | ❌ **Exceeds GOLD limits** |

GOLD standard specifies files should remain maintainable (<50 KB preferred, <100 KB maximum). At 215 KB, MultiScreenBuilder.cs is more than 4× the recommended limit.

### Current Structure Analysis

The 215 KB file contains:
- Color palette constants (shared)
- Infrastructure setup (Canvas, EventSystem, ScreenManager, ScreenDataBridge)
- **8 screen builders** (Screens 2-8 + Plant Overview) — Screen 1 is NOT here
- ~50 shared helper methods (CreatePanel, CreateGauge, CreateButton, etc.)

### Proposed Refactoring (Screen 1 Excluded)

Split into **partial classes** by functional area:

```
MultiScreenBuilder.cs                    (~15 KB) - Main entry + infrastructure
MultiScreenBuilder.Colors.cs             (~3 KB)  - Color palette
MultiScreenBuilder.Helpers.cs            (~25 KB) - Shared helper methods
MultiScreenBuilder.Screen2_RCS.cs        (~25 KB) - RCS Primary Loop screen
MultiScreenBuilder.Screen3_Pressurizer.cs (~20 KB) - Pressurizer screen
MultiScreenBuilder.Screen4_CVCS.cs       (~20 KB) - CVCS screen
MultiScreenBuilder.Screen5_SG.cs         (~20 KB) - Steam Generator screen
MultiScreenBuilder.Screen6_Turbine.cs    (~15 KB) - Turbine-Generator screen
MultiScreenBuilder.Screen7_Secondary.cs  (~25 KB) - Secondary Systems screen
MultiScreenBuilder.Screen8_Auxiliary.cs  (~15 KB) - Auxiliary Systems screen
MultiScreenBuilder.Overview.cs           (~20 KB) - Plant Overview screen
```

**Note:** No `MultiScreenBuilder.Screen1_Reactor.cs` — Screen 1 remains in `OperatorScreenBuilder.cs`.

**Benefits:**
- Each file < 30 KB (well within GOLD limits)
- Clear separation of concerns
- Easier to maintain individual screens
- Enables parallel development
- Reduces merge conflicts
- **Screen 1 (Reactor) completely untouched**

### Refactoring Steps

1. Create partial class structure with shared namespace
2. Extract color constants to `MultiScreenBuilder.Colors.cs`
3. Extract helper methods to `MultiScreenBuilder.Helpers.cs`
4. Move each screen builder (2-8 + Overview) to its own partial class file
5. Verify all `#if UNITY_EDITOR` guards are consistent
6. Test menu item still works correctly
7. Verify Screen 1 delegation to `OperatorScreenBuilder.BuildScreen1()` unchanged
8. Archive original monolithic file

---

## Part C: New Screen Requirement Assessment

**Question:** Is a new operator screen required for Condenser/Feedwater?

**Answer:** **No.** Screen 7 (Secondary Systems) already covers this scope:
- Left panel: Feedwater train gauges
- Center: Secondary cycle flow diagram (includes condenser)
- Right panel: Steam system gauges
- Bottom: Steam dump controls

The screen layout is appropriate; it just needs the PLACEHOLDER values replaced with live data from `ScreenDataBridge`.

However, if scope expands significantly (e.g., full heater drain system, condensate polishing, etc.), a dedicated **Screen 9 (Condenser/Feedwater)** could be considered in the future.

---

## Files Requiring Modification

### Phase 1: Screen Updates (No Builder Changes)
1. `Assets/Scripts/UI/PlantOverviewScreen.cs` — Add condenser/FW mimic animations (**Low effort**)
2. `Assets/Scripts/UI/SecondarySystemsScreen.cs` — Replace PLACEHOLDERs with live data (**Medium effort**)
3. `Assets/Scripts/UI/ScreenDataBridge.cs` — Add 9 missing Condenser/FW pump status getters

### Phase 2: MultiScreenBuilder Refactoring (Screen 1 Untouched)
4. `Assets/Scripts/UI/MultiScreenBuilder.cs` → Split into partial classes
5. Create **9 new partial class files** (Colors, Helpers, Screens 2-8, Overview)
6. `Assets/Scripts/UI/OperatorScreenBuilder.cs` — **NO CHANGES**

### Phase 3: Builder Update for Screen 7 (If New UI Elements Needed)
7. `Assets/Scripts/UI/MultiScreenBuilder.Screen7_Secondary.cs` — Add Condenser/C-9/CST UI elements

---

## Implementation Priority

| Priority | Task | Effort | Screen 1 Impact |
|----------|------|--------|-----------------|
| 1 | Add missing ScreenDataBridge getters | Low | None |
| 2 | Update PlantOverviewScreen.cs mimic animations | **Low** | None |
| 3 | Update SecondarySystemsScreen.cs to use live data | Medium | None |
| 4 | Refactor MultiScreenBuilder.cs into partial classes | High | **None** |
| 5 | Add Condenser/C-9/CST UI elements to Screen 7 builder | Medium | None |

---

## References

- **CS-0118:** Validation Dashboard missing Condenser/Feedwater parameters
- **IP-0046:** Condenser/Feedwater Architecture Implementation
- **CS-0115:** Condenser/Feedwater Module Investigation
- **CS-0116:** Condenser Startup Orchestration
- **NRC HRTD Section 7.2:** Condensate and Feedwater System
- **NRC HRTD Section 11.2:** Steam Dump Control System

---

## GOLD Standard Compliance Notes

- MultiScreenBuilder.cs at 215 KB is a **critical violation** of maintainability standards
- Refactoring is required before any new screen builder code is added
- All new partial class files must include proper headers with version, date, and purpose
- **OperatorScreenBuilder.cs (Screen 1) is compliant at 51 KB and requires no changes**

---

*Investigation by: Claude (2026-02-18)*  
*Amended: 2026-02-18 — Clarified Screen 1 architecture separation*  
*Amended: 2026-02-18 — Added Plant Overview (Tab) gap analysis*
