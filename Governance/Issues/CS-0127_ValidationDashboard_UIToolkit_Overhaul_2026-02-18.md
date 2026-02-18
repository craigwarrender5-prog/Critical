# CS-0127: Validation Dashboard Complete Overhaul - UI Toolkit Implementation

**Issue ID:** CS-0127  
**Title:** Validation Dashboard Complete Overhaul Using Unity UI Toolkit  
**Domain:** Operator Interface & Scenarios  
**Severity:** HIGH  
**Status:** READY  
**Created:** 2026-02-18T14:00:00Z  
**Assigned DP:** DP-0008  

---

## Problem Summary

The current validation dashboard implementation exists in two parallel OnGUI-based systems:

1. **HeatupValidationVisual** (legacy) - Multi-partial OnGUI dashboard with 8 tabs
2. **ValidationDashboard** (v1.0) - Newer OnGUI implementation with sparklines and annunciator system

Both systems suffer from:
- **Visual limitations**: OnGUI cannot achieve professional, modern aesthetics
- **No animations**: Static displays lack visual feedback and engagement
- **Performance concerns**: OnGUI redraws entire screen each frame
- **Maintenance burden**: Two parallel dashboard codebases
- **Limited graphics**: No vector drawing, gradients, or smooth animations

## Expected Behavior

A single, unified validation dashboard built on Unity UI Toolkit that:

1. **Critical Need-to-Know Information** on primary tab - immediate plant status at a glance
2. **Professional, stunning appearance** - modern control room aesthetic with:
   - Animated arc gauges for key parameters
   - Real-time strip charts with smooth trace rendering
   - Pulsing/flashing annunciator tiles
   - Animated pressurizer visualization (water level, steam bubble, heaters)
   - Animated SG diagram (tube bundle, water level, steam flow)
   - Animated RCS loop flow indicators
3. **Functional drill-down tabs** for detailed system views
4. **Consistent 5Hz update rate** with smooth visual interpolation

## Technical Approach

### Architecture: UI Toolkit + Painter2D

Per existing POC documentation (`Documentation/UI_Documentation/`):
- **Arc gauges**: Painter2D `Arc()` with track + value arcs + needle
- **Strip charts**: Painter2D `MoveTo()`/`LineTo()` with ring buffer data
- **Animations**: USS transitions + C# property animations
- **Data binding**: `[CreateProperty]` runtime binding to snapshot data

### Tab Structure

| Tab | Content | Priority |
|-----|---------|----------|
| **CRITICAL** | Primary "need-to-know-now" parameters, key gauges, active alarms | P0 |
| **RCS** | T_avg/T_hot/T_cold gauges, RCP status, loop flow animation | P1 |
| **PRESSURIZER** | Animated PZR vessel (level, bubble), heater status, spray | P1 |
| **CVCS** | Charging/letdown bars, VCT level, mass conservation | P1 |
| **SG/RHR** | Animated SG diagram, RHR status, secondary pressure | P1 |
| **CONDENSER** | Vacuum gauge, C-9/P-12 permissives, feedwater status | P2 |
| **TRENDS** | 8 configurable strip charts with 4-hour history | P1 |
| **LOG** | Event log with severity filtering, annunciator grid | P1 |

### Animated Visualizations

#### 1. Pressurizer Vessel Animation
```
┌─────────────────┐
│    ░░░░░░░░░    │  ← Steam space (animated shimmer when steaming)
│    ░░░░░░░░░    │
├─────────────────┤  ← Water surface (animated wave effect)
│    ▓▓▓▓▓▓▓▓▓    │
│    ▓▓▓▓▓▓▓▓▓    │  ← Water (color indicates temp: blue→cyan→green→yellow)
│    ▓▓▓▓▓▓▓▓▓    │
│   [HTR][HTR]    │  ← Heater indicators (glow when ON)
└─────────────────┘
      ↕ Surge
```

#### 2. Steam Generator Animation
```
     ↑ Steam outlet (animated particles when steaming)
┌─────────────────┐
│   ┃┃┃┃┃┃┃┃┃┃   │  ← Tube bundle (temperature gradient)
│   ┃┃┃┃┃┃┃┃┃┃   │
│~~~~~~~~~~~~~~~~~~~~~│  ← Water level (animated surface)
│   ▓▓▓▓▓▓▓▓▓▓   │  ← Secondary water
└─────────────────┘
      ↑ Feedwater
```

#### 3. Arc Gauge Animation
- Smooth needle movement (interpolated)
- Color transition through threshold bands
- Glow effect at alarm thresholds
- Value readout with trend arrow

### Parameters to Expose (from HeatupSimEngine analysis)

#### Critical Tab (Need-to-Know-Now)
| Parameter | Source Field | Display Type |
|-----------|--------------|--------------|
| Plant Mode | `plantMode` | Mode indicator with color |
| T_avg | `T_avg` | Arc gauge |
| RCS Pressure | `pressure` | Arc gauge |
| PZR Level | `pzrLevel` | Arc gauge |
| Subcooling | `subcooling` | Arc gauge |
| Heatup Rate | `heatupRate` | Digital + trend |
| RCP Count | `rcpCount` | 4x LED indicators |
| Active Alarms | Annunciator system | Alarm tiles |
| Bubble Status | `bubbleFormed`, `solidPressurizer` | Status indicator |
| Phase | `heatupPhaseDesc` | Text banner |
| Sim/Wall Time | `simTime`, `wallClockTime` | Digital clocks |

#### Additional Parameters by System
- **RCS**: T_hot, T_cold, T_sat, pressureRate, rcpHeat, effectiveRCPHeat, rcpContribution
- **PZR**: T_pzr, pzrWaterVolume, pzrSteamVolume, pzrHeaterPower, sprayFlow, surgeFlow, bubblePhase
- **CVCS**: chargingFlow, letdownFlow, vctState.Level, massConservationError, brsState
- **SG/RHR**: sgSecondaryPressure_psia, sgHeatTransfer_MW, sgBoilingActive, rhrNetHeat_MW, rhrActive
- **Condenser**: condenserVacuum_inHg, condenserC9Available, steamDumpPermitted, hotwellLevel_pct
- **HZP**: hzpProgress, hzpStable, steamDumpHeat_MW, heaterPIDOutput

### File Structure

```
Assets/
├── Scripts/
│   └── UI/
│       └── UIToolkit/
│           └── ValidationDashboard/
│               ├── ValidationDashboardController.cs    # Main controller
│               ├── DashboardDataModel.cs               # Snapshot/binding model
│               ├── Elements/
│               │   ├── ArcGaugeElement.cs              # Animated arc gauge
│               │   ├── StripChartElement.cs            # Multi-trace chart
│               │   ├── AnnunciatorTileElement.cs       # Flashing alarm tile
│               │   ├── PressurizerVesselElement.cs     # Animated PZR
│               │   ├── SteamGeneratorElement.cs        # Animated SG
│               │   ├── LEDIndicatorElement.cs          # Status LED
│               │   ├── DigitalReadoutElement.cs        # Numeric display
│               │   └── BidirectionalBarElement.cs      # ±value bar
│               └── Tabs/
│                   ├── CriticalTab.cs
│                   ├── RCSTab.cs
│                   ├── PressurizerTab.cs
│                   ├── CVCSTab.cs
│                   ├── SGRHRTab.cs
│                   ├── CondenserTab.cs
│                   ├── TrendsTab.cs
│                   └── LogTab.cs
├── UI/
│   └── UIToolkit/
│       └── ValidationDashboard/
│           ├── ValidationDashboard.uxml                # Main layout
│           ├── ValidationDashboard.uss                 # Main styles
│           ├── Elements/
│           │   ├── ArcGauge.uss
│           │   ├── StripChart.uss
│           │   └── ...
│           └── Tabs/
│               ├── CriticalTab.uxml
│               └── ...
```

## Implementation Stages

### Stage 1: Foundation (Core Elements)
- Create `ValidationDashboardController.cs` with UIDocument setup
- Implement `ArcGaugeElement.cs` with Painter2D arc rendering
- Implement `LEDIndicatorElement.cs` with color states
- Implement `DigitalReadoutElement.cs` with formatting
- Create base USS theming (colors matching nuclear control room aesthetic)
- Bind to HeatupSimEngine at 5Hz

### Stage 2: Critical Tab
- Layout the "need-to-know-now" primary tab
- 6 arc gauges: T_avg, Pressure, PZR Level, Subcooling, Heatup Rate, SG Pressure
- RCP status LEDs (4x)
- Plant mode/phase banner
- Bubble/solid state indicator
- Sim time display
- Compact annunciator row (top 8 active alarms)

### Stage 3: Animated Visualizations
- `PressurizerVesselElement.cs` with animated water level, steam space, heaters
- `SteamGeneratorElement.cs` with animated tube bundle, water level
- `AnnunciatorTileElement.cs` with flashing states

### Stage 4: Strip Charts
- `StripChartElement.cs` with ring buffer and Painter2D line rendering
- 8 configurable traces
- Zoom/pan controls
- Legend with current values

### Stage 5: Remaining Tabs
- RCS tab with loop diagram
- CVCS tab with flow diagram
- SG/RHR tab with SG visualization
- Condenser tab with vacuum/permissive indicators
- Log tab with filterable event list

### Stage 6: Polish & Integration
- Smooth animations and transitions
- Performance optimization (<2ms per frame)
- Scene integration (replace legacy dashboards)
- Documentation

## Success Criteria

1. **Visual Quality**: Dashboard looks professional and modern - not like a programmer's debug UI
2. **Animations**: Gauges, vessels, and indicators animate smoothly
3. **Information Hierarchy**: Critical info immediately visible; details accessible via drill-down
4. **Performance**: Maintains 60fps with <2ms UI update cost
5. **Completeness**: All parameters currently exposed in legacy dashboards are accessible
6. **Single Source**: Replaces both HeatupValidationVisual and ValidationDashboard

## Dependencies

- UI Toolkit POC validation (COMPLETE - see `Documentation/UI_Documentation/`)
- HeatupSimEngine telemetry snapshot interface (EXISTS)
- Unity 6.3 UI Toolkit runtime (AVAILABLE)

## Related Issues

- CS-0118: Dashboard missing condenser/feedwater telemetry (will be addressed)
- CS-0121: Dashboard visual issues (will be superseded)

## References

- `Documentation/UI_Documentation/UIToolkit_Feasibility_Analysis.md`
- `Documentation/UI_Documentation/UIToolkit_Painter2D_Reference.md`
- `Documentation/UI_Documentation/UIToolkit_CustomControls.md`
- `Assets/Scripts/Validation/HeatupSimEngine.cs` - Source of all parameters
- `Assets/Scripts/Validation/ValidationDashboard.cs` - Current tab structure reference
- NRC HRTD Section 19 - Plant Operations monitoring requirements

---

*Investigation prepared: 2026-02-18*
