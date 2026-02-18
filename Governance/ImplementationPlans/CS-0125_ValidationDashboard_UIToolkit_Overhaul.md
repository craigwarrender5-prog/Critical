---
CS ID: CS-0125
Title: Complete Overhaul of Validation Dashboard Using UI Toolkit
Type: FEATURE
Severity: HIGH
Status: PROPOSED
Date: 2026-02-18
Domain: DP-0008 - Operator Interface & Scenarios
Related IPs: IP-0040 (original dashboard), IP-0042 (archived UI Toolkit attempt)
Prerequisites: None (standalone UI replacement)
Blocking: Future operator screen integration
Estimated Effort: 40-60 hours
---

# CS-0125: Complete Overhaul of Validation Dashboard Using UI Toolkit

## 1. Executive Summary

The HeatupSimEngine has evolved into a sophisticated PWR Cold Shutdown → HZP simulator with **220+ state variables** spanning 15+ major systems. The current Validation Dashboard, built with Unity's legacy IMGUI and Canvas-based UI, covers only a fraction of this capability and lacks the animated graphics, modern styling, and responsive layout required for effective training and validation.

This change set proposes a **complete overhaul** of the Validation Dashboard using **Unity UI Toolkit**, leveraging the proven animated component library developed in the POC phase (PressurizerVesselPOC, RCSLoopDiagramPOC, etc.).

### Key Objectives
1. Replace legacy Canvas-based panels with UI Toolkit VisualElements
2. Integrate proven animated graphics (pressurizer vessel, RCS loop diagram)
3. Expose all major HeatupSimEngine systems with appropriate visualizations
4. Create a unified, maintainable codebase with declarative UXML/USS styling
5. Enable future operator control interfaces (heater mode, RCP start, orifice lineup)

---

## 2. Current State Analysis

### 2.1 HeatupSimEngine Public State Coverage

| Category | Field Count | Current Dashboard Coverage | Gap |
|----------|-------------|---------------------------|-----|
| Core Parameters | ~15 | ✅ Good | — |
| Detailed Instrumentation | ~40 | ⚠️ Partial | ~25 fields missing |
| CVCS/VCT/BRS | ~30 | ⚠️ Basic | ~20 fields missing |
| Bubble Formation (7-phase) | ~50 | ❌ None | Critical gap |
| HZP Systems | ~25 | ❌ None | Critical gap |
| SG Multi-Node Model | ~20 | ❌ None | Critical gap |
| Condenser/Feedwater | ~15 | ❌ None | Not displayed |
| RHR System | ~10 | ⚠️ Minimal | Mode only |
| Spray System | ~5 | ⚠️ Minimal | Status only |
| Mass Conservation | ~25 | ⚠️ Partial | Error only, no breakdown |
| History Buffers | ~20 | ✅ Good | — |
| Alarm Flags | ~30 | ⚠️ Partial | Static grid |

### 2.2 Current UI Architecture Issues

1. **Legacy Canvas System**: ValidationDashboardBuilder uses Unity Canvas with TextMeshPro, limiting vector graphics and animation capabilities
2. **Static Panels**: Most panels are placeholder or text-only; no animated process graphics
3. **Fragmented Data Binding**: Mix of FindObjectOfType patterns and direct field access
4. **No Operator Controls**: Entirely read-only; cannot interact with heater modes, CVCS lineup, etc.
5. **Missing Critical Visualizations**:
   - Bubble formation state machine (7 phases)
   - SG thermocline and boiling visualization
   - Steam dump/condenser vacuum chain
   - Detailed CVCS flow paths
   - Mass ledger breakdown

### 2.3 Proven POC Components Available

The UI Toolkit POC phase successfully demonstrated:

| Component | File | Capabilities |
|-----------|------|--------------|
| PressurizerVesselPOC | `UIToolkit/POC/PressurizerVesselPOC.cs` | Water/steam levels, heater glow, spray animation, charging/letdown flow |
| RCSLoopDiagramPOC | `UIToolkit/POC/RCSLoopDiagramPOC.cs` | 4-loop schematic, RCP status, flow chevrons, PZR connection, RHR indication |
| ArcGaugePOC | `UIToolkit/POC/ArcGaugePOC.cs` | Circular gauge with colored bands, needle animation |
| LinearGaugePOC | `UIToolkit/POC/LinearGaugePOC.cs` | Vertical/horizontal bar gauge |
| StripChartPOC | `UIToolkit/POC/StripChartPOC.cs` | Rolling trend display |
| AnnunciatorTilePOC | `UIToolkit/POC/AnnunciatorTilePOC.cs` | Alarm tiles with flash effect |
| TankLevelPOC | `UIToolkit/POC/TankLevelPOC.cs` | Tank visualization with level indication |
| StatusLEDPOC | `UIToolkit/POC/StatusLEDPOC.cs` | Status indicator lights |

---

## 3. Proposed Architecture

### 3.1 High-Level Structure

```
Assets/Scripts/UI/ValidationDashboardV2/
├── Core/
│   ├── ValidationDashboardV2.cs           # Main controller, lifecycle, data binding
│   ├── ValidationDashboardV2.uxml         # Root layout definition
│   ├── ValidationDashboardV2.uss          # Master stylesheet
│   └── DashboardDataBridge.cs             # Unified data source abstraction
├── Components/
│   ├── Gauges/
│   │   ├── ArcGauge.cs                    # Production arc gauge (from POC)
│   │   ├── LinearGauge.cs                 # Production linear gauge
│   │   ├── DigitalReadout.cs              # Numeric display
│   │   └── BidirectionalGauge.cs          # Flow/delta display
│   ├── Graphics/
│   │   ├── PressurizerVessel.cs           # Production pressurizer (from POC)
│   │   ├── RCSLoopDiagram.cs              # Production RCS diagram (from POC)
│   │   ├── SteamGeneratorGraphic.cs       # NEW: SG with thermocline visualization
│   │   ├── BubbleFormationFSM.cs          # NEW: 7-phase state machine display
│   │   ├── CVCSFlowDiagram.cs             # NEW: CVCS flow paths with animation
│   │   ├── CondenserVacuumGauge.cs        # NEW: Condenser vacuum with C-9 status
│   │   └── MassLedgerSankey.cs            # NEW: Mass flow Sankey diagram
│   ├── Indicators/
│   │   ├── AnnunciatorTile.cs             # Production alarm tile
│   │   ├── StatusLED.cs                   # Production status LED
│   │   ├── PhaseIndicator.cs              # NEW: Bubble phase indicator
│   │   └── PermissiveChain.cs             # NEW: Startup permissive display
│   └── Trends/
│       ├── StripChart.cs                  # Production strip chart
│       ├── MultiTraceTrend.cs             # NEW: Multi-variable overlay
│       └── TrendSelector.cs               # NEW: Variable selection UI
├── Panels/
│   ├── OverviewPanelV2.cs                 # Enhanced overview with graphics
│   ├── RCSPanelV2.cs                      # RCS loop with animated diagram
│   ├── PressurizerPanelV2.cs              # PZR vessel with bubble FSM
│   ├── CVCSPanelV2.cs                     # CVCS flow diagram and gauges
│   ├── SGRHRPanelV2.cs                    # SG model with thermocline
│   ├── HZPSystemsPanelV2.cs               # NEW: Steam dump, condenser, permissives
│   ├── MassConservationPanelV2.cs         # NEW: Mass ledger with Sankey
│   └── AlarmsPanelV2.cs                   # Enhanced annunciator grid
├── Controls/                              # Future: operator control interfaces
│   ├── HeaterModeSelector.cs              # Heater mode toggle
│   ├── RCPStartPanel.cs                   # RCP start/stop with interlocks
│   └── OrificeLineupSelector.cs           # Letdown orifice configuration
└── Resources/
    ├── Themes/
    │   ├── ValidationDashboardDark.uss    # Dark theme (default)
    │   └── ValidationDashboardLight.uss   # Light theme variant
    └── Icons/
        └── *.png                          # System icons and symbols
```

### 3.2 Tab/Panel Structure

| Tab | Panel | Primary Visualization | Key Metrics |
|-----|-------|----------------------|-------------|
| 0 | Overview | Mini RCS diagram + PZR + Key gauges | T_avg, P, PZR level, Mode, Phase |
| 1 | RCS/Primary | Full RCSLoopDiagram | T_hot/cold per loop, RCP status, flow |
| 2 | Pressurizer | PressurizerVessel + BubbleFSM | Level, P, heaters, spray, bubble phase |
| 3 | CVCS | CVCSFlowDiagram | Charging, letdown, VCT, BRS, boron |
| 4 | SG/RHR | SteamGeneratorGraphic | Thermocline, secondary P/T, RHR mode |
| 5 | HZP Systems | CondenserVacuumGauge + Permissives | Steam dump, condenser, C-9/P-12 |
| 6 | Mass Balance | MassLedgerSankey | Ledger, components, drift, PBOC |
| 7 | Alarms | AnnunciatorGrid | All 30+ alarm flags |
| 8 | Trends | MultiTraceTrend + Selector | Configurable trend display |

### 3.3 Data Binding Strategy

```csharp
public class DashboardDataBridge : MonoBehaviour
{
    // Primary data sources
    public HeatupSimEngine HeatupEngine { get; private set; }
    public ReactorController ReactorController { get; private set; }
    
    // Unified accessors that select appropriate source
    public float Temperature => HeatupEngine?.T_avg ?? ReactorController?.temperature ?? 0f;
    public float Pressure => HeatupEngine?.pressure ?? ReactorController?.pressure ?? 0f;
    public bool IsHeatupPhase => HeatupEngine?.isRunning ?? false;
    
    // Snapshot struct for batch updates
    public DashboardSnapshot GetSnapshot();
    
    // Event for data updates
    public event Action<DashboardSnapshot> OnDataUpdated;
}

public struct DashboardSnapshot
{
    // Core
    public float SimTime, T_avg, T_hot, T_cold, Pressure, PZRLevel;
    public int RCPCount, PlantMode;
    
    // Bubble Formation
    public BubbleFormationPhase BubblePhase;
    public float BubblePhaseProgress;
    public bool SolidPressurizer, BubbleFormed;
    
    // SG Model
    public float SGTopTemp, SGBottomTemp, SGThermoclineHeight;
    public float SGSecondaryPressure, SGSecondaryMass;
    public bool SGBoilingActive, SGDrainingActive;
    
    // HZP Systems
    public float SteamDumpHeat, CondenserVacuum;
    public bool SteamDumpPermitted, C9Available, P12Bypassed;
    
    // Mass Conservation
    public float PrimaryMassLedger, PrimaryMassComponents, MassDrift;
    public bool MassConservationOK, MassAlarm;
    
    // ... (all 220+ fields organized by subsystem)
}
```

---

## 4. Implementation Phases

### Phase 1: Core Infrastructure (8-12 hours)

| Task | Description | Deliverable |
|------|-------------|-------------|
| 1.1 | Create ValidationDashboardV2 folder structure | Folder hierarchy |
| 1.2 | Implement DashboardDataBridge with snapshot system | DashboardDataBridge.cs |
| 1.3 | Create root UXML layout with tab navigation | ValidationDashboardV2.uxml |
| 1.4 | Create master USS stylesheet with theme tokens | ValidationDashboardV2.uss |
| 1.5 | Implement ValidationDashboardV2 controller | ValidationDashboardV2.cs |
| 1.6 | Wire up scene setup and toggle with existing V key | Scene integration |

### Phase 2: Production Components from POC (10-15 hours)

| Task | Description | Source |
|------|-------------|--------|
| 2.1 | Promote PressurizerVesselPOC to production | POC/PressurizerVesselPOC.cs |
| 2.2 | Promote RCSLoopDiagramPOC to production | POC/RCSLoopDiagramPOC.cs |
| 2.3 | Promote ArcGaugePOC to production | POC/ArcGaugePOC.cs |
| 2.4 | Promote LinearGaugePOC to production | POC/LinearGaugePOC.cs |
| 2.5 | Promote StripChartPOC to production | POC/StripChartPOC.cs |
| 2.6 | Promote AnnunciatorTilePOC to production | POC/AnnunciatorTilePOC.cs |
| 2.7 | Promote StatusLEDPOC to production | POC/StatusLEDPOC.cs |
| 2.8 | Promote TankLevelPOC to production | POC/TankLevelPOC.cs |

### Phase 3: New Graphics Components (12-18 hours)

| Task | Description | Complexity |
|------|-------------|------------|
| 3.1 | SteamGeneratorGraphic with thermocline | HIGH - Multi-node visualization |
| 3.2 | BubbleFormationFSM state machine display | MEDIUM - 7-phase indicator |
| 3.3 | CVCSFlowDiagram with animated flows | HIGH - Flow path routing |
| 3.4 | CondenserVacuumGauge with C-9 status | MEDIUM - Vacuum gauge |
| 3.5 | MassLedgerSankey diagram | HIGH - Sankey flow visualization |
| 3.6 | PermissiveChain display | MEDIUM - Status chain |

### Phase 4: Panel Implementation (10-15 hours)

| Panel | Key Components | Priority |
|-------|---------------|----------|
| OverviewPanelV2 | Mini diagrams, key gauges | P1 |
| PressurizerPanelV2 | PressurizerVessel, BubbleFSM | P1 |
| RCSPanelV2 | RCSLoopDiagram, loop temps | P1 |
| CVCSPanelV2 | CVCSFlowDiagram, VCT gauge | P2 |
| SGRHRPanelV2 | SGGraphic, RHR status | P2 |
| HZPSystemsPanelV2 | Condenser, steam dump | P2 |
| MassConservationPanelV2 | Sankey, ledger table | P3 |
| AlarmsPanelV2 | Annunciator grid | P3 |

### Phase 5: Integration & Polish (5-8 hours)

| Task | Description |
|------|-------------|
| 5.1 | Wire all panels to DashboardDataBridge |
| 5.2 | Implement animation update loop (flow chevrons, etc.) |
| 5.3 | Add keyboard shortcuts for tab navigation |
| 5.4 | Create dark/light theme variants |
| 5.5 | Performance optimization (batch updates, dirty checking) |
| 5.6 | Remove legacy ValidationDashboard code paths |

---

## 5. Key Graphics Specifications

### 5.1 PressurizerVessel (Enhanced from POC)

**Inputs from HeatupSimEngine:**
- `pzrLevel` (0-100%)
- `pzrWaterVolume`, `pzrSteamVolume`
- `pzrHeaterPower` (0-1.8 MW)
- `sprayActive`, `sprayFlow_GPM`
- `chargingFlow`, `letdownFlow`
- `bubblePhase`, `bubbleFormed`, `solidPressurizer`
- `pressure`, `T_pzr`, `T_sat`

**Visual Features:**
- Water/steam regions with level indication
- Heater elements with glow effect when energized
- Spray droplets when active
- Charging/letdown flow arrows with animation
- Bubble zone visualization during formation
- Surge line connection
- Level setpoint marker

### 5.2 RCSLoopDiagram (Enhanced from POC)

**Inputs from HeatupSimEngine:**
- `T_hot`, `T_cold`, `T_avg`
- `rcpCount`, `rcpRunning[]`
- `rhrActive`, `rhrModeString`
- `pressure`
- `sgTopNodeTemp`, `sgBottomNodeTemp` (per SG)

**Visual Features:**
- 4-loop schematic with hot/cold leg coloring
- RCP indicators with impeller animation when running
- Steam generator outlines
- Pressurizer on Loop 2 with surge line
- RHR connection indication
- Flow chevrons animated by RCP status
- Temperature-based piping color gradients

### 5.3 SteamGeneratorGraphic (NEW)

**Inputs from HeatupSimEngine:**
- `sgTopNodeTemp`, `sgBottomNodeTemp`
- `sgThermoclineHeight` (ft)
- `sgActiveAreaFraction`
- `sgBoilingActive`, `sgBoilingIntensity`
- `sgSecondaryPressure_psia`, `sgSaturationTemp_F`
- `sgSecondaryMass_lb`, `sgSteamInventory_lb`
- `sgDrainingActive`, `sgDrainingRate_gpm`, `sgWideRangeLevel_pct`
- `sgBoundaryMode`, `sgStartupBoundaryState`

**Visual Features:**
- U-tube bundle representation
- Thermocline line with height indication
- Temperature gradient coloring above/below thermocline
- Boiling bubbles when `sgBoilingActive`
- Secondary water level indication
- Steam dome with inventory visualization
- Draining animation when active

### 5.4 BubbleFormationFSM (NEW)

**Inputs from HeatupSimEngine:**
- `bubblePhase` (NONE, DETECTION, VERIFICATION, DRAIN, STABILIZE, PRESSURIZE, RCP_READY)
- `bubblePhaseStartTime`
- `bubbleDrainStartLevel`
- `drainExitPressure_psia`, `drainExitLevel_pct`
- `drainTransitionReason`
- `startupHoldActive`, `startupHoldReleaseTime_hr`
- `heaterAuthorityState`, `heaterLimiterReason`

**Visual Features:**
- 7-phase horizontal state diagram
- Current phase highlighted with glow
- Phase duration timers
- Gate status indicators (passed/pending/blocked)
- Transition arrows between phases
- Blocked reason display

### 5.5 CVCSFlowDiagram (NEW)

**Inputs from HeatupSimEngine:**
- `chargingFlow`, `letdownFlow`, `surgeFlow`
- `orifice75Count`, `orifice45Open`, `orificeLineupDesc`
- `letdownViaRHR`, `letdownViaOrifice`
- `vctState` (level, boron, volume)
- `brsState` (holdup, distillate, concentrate)
- `cvcsControllerState`
- `chargingToRCS`, `totalCCPOutput`, `divertFraction`
- `cvcsThermalMixing_MW`

**Visual Features:**
- VCT tank with level and boron indication
- CCP output indication
- Charging/letdown flow paths with animation
- Orifice lineup visualization (3 orifices)
- RHR crossconnect path
- BRS holdup and evaporator indication
- Flow rate digital readouts

### 5.6 MassLedgerSankey (NEW)

**Inputs from HeatupSimEngine:**
- `physicsState.TotalPrimaryMass_lb`
- `physicsState.RCSWaterMass`
- `physicsState.PZRWaterMass`, `physicsState.PZRSteamMass`
- `primaryMassLedger_lb`, `primaryMassComponents_lb`
- `primaryMassDrift_lb`, `primaryMassDrift_pct`
- `pbocLastEvent` (boundary flow event)
- `massConservationError`

**Visual Features:**
- Sankey diagram showing mass flows
- RCS, PZR water, PZR steam component boxes
- CVCS boundary flow arrows
- Drift indicator with color threshold
- Ledger vs components comparison
- PBOC event trace

---

## 6. Acceptance Criteria

### 6.1 Functional Requirements

| ID | Requirement | Verification |
|----|-------------|--------------|
| F1 | Dashboard displays all 220+ HeatupSimEngine state fields | Field audit |
| F2 | Animated graphics update at 30fps minimum | Frame rate test |
| F3 | Tab navigation via mouse click and keyboard (1-8) | Input test |
| F4 | All POC components integrated without regression | Visual comparison |
| F5 | Bubble formation 7-phase state machine visualized | Phase transition test |
| F6 | SG thermocline and boiling visualized | Heatup scenario test |
| F7 | Mass conservation ledger displayed with breakdown | Mass audit test |
| F8 | HZP systems (steam dump, condenser) visualized | HZP transition test |
| F9 | V key toggles between old and new dashboard | Toggle test |
| F10 | Responsive layout for 1920x1080 minimum | Resolution test |

### 6.2 Non-Functional Requirements

| ID | Requirement | Verification |
|----|-------------|--------------|
| N1 | < 5ms per frame for dashboard update | Profiler measurement |
| N2 | < 100MB additional memory allocation | Memory profiler |
| N3 | No Unity warnings or errors from dashboard code | Console check |
| N4 | All USS styling via tokens, no hardcoded colors | Code review |
| N5 | Code adheres to project conventions (G1-G4, SOLID) | Code review |

---

## 7. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| UI Toolkit performance on large diagrams | Medium | High | Implement dirty checking, batch updates |
| POC components need significant rework | Low | Medium | POCs already validated; minimal changes expected |
| Data binding complexity with 220+ fields | High | Medium | Use snapshot struct with organized subsections |
| Learning curve for UXML/USS | Medium | Low | Team has Unity UI experience; POCs demonstrate patterns |
| Integration with existing keybinds/navigation | Low | Medium | Preserve V key toggle; add new dashboard as option |

---

## 8. Dependencies

### 8.1 Prerequisites
- HeatupSimEngine state fields stable (v5.5 current)
- UI Toolkit POC components validated
- Unity 2022.3 LTS with UI Toolkit support

### 8.2 Blocking Items
- None for initial implementation
- Operator controls (Phase 6) blocked until dashboard stable

### 8.3 Related Work
- ScreenDataBridge (existing in `Assets/Scripts/UI/ScreenDataBridge.cs`) should be extended or unified with DashboardDataBridge
- May want to deprecate legacy ValidationDashboard after V2 proven

---

## 9. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-02-18 | Claude | Initial CS proposal based on review report |

---

## 10. Appendix: HeatupSimEngine State Field Inventory

### A. Core Parameters (~15 fields)
```
simTime, T_avg, pressure, pzrLevel, T_sat, subcooling, rcpHeat, pzrHeaterPower,
rcpCount, gridEnergy, heatupRate, plantMode, isRunning, statusMessage, wallClockTime
```

### B. Detailed Instrumentation (~40 fields)
```
T_cold, T_hot, T_pzr, T_rcs, T_sg_secondary, sgHeatTransfer_MW,
sgTopNodeTemp, sgBottomNodeTemp, sgStratificationDeltaT, sgThermoclineHeight,
sgActiveAreaFraction, sgBoilingActive, sgSecondaryPressure_psia, sgSaturationTemp_F,
sgMaxSuperheat_F, sgNitrogenIsolated, sgBoilingIntensity, sgBoundaryMode,
sgStartupBoundaryState, sgDrainingActive, sgDrainingRate_gpm, sgSecondaryMass_lb,
sgWideRangeLevel_pct, sgNarrowRangeLevel_pct, rhrState, rhrNetHeat_MW,
rhrHXRemoval_MW, rhrPumpHeat_MW, rhrActive, rhrModeString,
pzrWaterVolume, pzrSteamVolume, pzrTotalEnthalpy_BTU, pzrSpecificEnthalpy_BTU_lb,
pzrClosureConverged, rcsWaterMass, chargingFlow, letdownFlow, surgeFlow, pressureRate
```

### C. CVCS/VCT/BRS (~30 fields)
```
vctState (Level_percent, Volume_gal, BoronConcentration_ppm, etc.),
brsState (HoldupVolume_gal, DistillateAvailable_gal, ConcentrateAvailable_gal, etc.),
rcsBoronConcentration, cvcsControllerState, cvcsIntegralError,
letdownViaRHR, letdownViaOrifice, letdownIsolatedFlag, orificeLetdownFlow,
rhrLetdownFlow, pzrLevelSetpointDisplay, chargingToRCS, totalCCPOutput,
divertFraction, cvcsThermalMixing_MW, orifice75Count, orifice45Open, orificeLineupDesc
```

### D. Bubble Formation (~50 fields)
```
solidPressurizer, bubbleFormed, bubbleFormationTemp, bubbleFormationTime,
bubblePhase, bubblePhaseStartTime, bubbleDrainStartLevel,
coldShutdownProfile, startupHoldActive, startupHoldReleaseTime_hr,
startupHoldTimeGatePassed, startupHoldPressureRateGatePassed,
heaterAuthorityState, heaterAuthorityReason, heaterLimiterReason,
heaterPressureRateClampActive, heaterRampRateClampActive,
ccpStarted, ccpStartTime, currentHeaterMode, bubbleHeaterSmoothedOutput,
auxSprayActive, auxSprayTestPassed, drainSteamDisplacement_lbm,
drainCvcsTransfer_lbm, drainDuration_hr, drainExitPressure_psia,
drainExitLevel_pct, drainTransitionReason, drainLineupDemandIndex,
solidPlantState, solidPlantPressureSetpoint, solidPlantPressureInBand,
bubbleTargetTemp, timeToBubble, heatupPhaseDesc
```

### E. HZP Systems (~25 fields)
```
steamDumpState, steamDumpHeat_MW, steamPressure_psig, steamDumpActive,
hzpState, hzpStable, hzpReadyForStartup, hzpProgress,
heaterPIDState, heaterPIDActive, heaterPIDOutput,
sprayState, sprayFlow_GPM, sprayValvePosition, sprayActive, spraySteamCondensed_lbm,
sgSteaming, sgSecondaryPressure_psig, handoffInitiated, startupPrereqs,
condenserState, condenserVacuum_inHg, condenserC9Available,
feedwaterState, hotwellLevel_pct, permissiveState, steamDumpPermitted
```

### F. Mass Conservation (~25 fields)
```
physicsState.TotalPrimaryMass_lb, physicsState.InitialPrimaryMass_lb,
physicsState.RCSWaterMass, physicsState.PZRWaterMass, physicsState.PZRSteamMass,
primaryMassLedger_lb, primaryMassComponents_lb, primaryMassDrift_lb,
primaryMassDrift_pct, primaryMassExpected_lb, primaryMassBoundaryError_lb,
primaryMassConservationOK, primaryMassAlarm, primaryMassStatus,
totalSystemMass_lbm, externalNetMass_lbm, massError_lbm,
pbocLastEvent, pbocEventCount, pbocPairingAssertionFailures,
rtccTransitionCount, rtccLastTransition
```

### G. Alarm Flags (~30 fields)
```
pzrHeatersOn, pzrLevelLow, pzrLevelHigh, rcsFlowLow,
chargingActive, letdownActive, steamBubbleOK, sealInjectionOK,
ccwRunning, rcpRunning[4], heatupInProgress, pressureLow, pressureHigh,
sgSecondaryPressureHigh, subcoolingLow, modePermissive, smmLowMargin,
smmNoMargin, rvlisLevelLow, vctLevelLow, vctLevelHigh,
vctDivertActive, vctMakeupActive, vctRWSTSuction
```
