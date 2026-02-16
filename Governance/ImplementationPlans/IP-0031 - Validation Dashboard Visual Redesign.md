# IP-0031: Validation Dashboard Visual Redesign

**Date:** 2026-02-16  
**Domain Plan:** DP-0008 (Operator Interface & Scenarios)  
**Status:** PENDING APPROVAL  
**Priority:** High  
**Changelog Required:** No

---

## 1. Executive Summary

This Implementation Plan defines a complete visual redesign of the Heatup Validation Dashboard to replace the current OnGUI-based implementation with a modern Unity uGUI Canvas system. The new dashboard will feature:

- **Primary "At-a-Glance" screen** showing all critical parameters without requiring tab navigation
- **Professional visual design** with arc gauges, bidirectional gauges, strip charts, glow effects, and smooth animations
- **Layered tab structure** for detailed system views and trend data
- **Additive overlay architecture** - not the default startup screen, loads over MainScene operator screens

The goal is to transform the validation dashboard from a utilitarian debugging tool into a visually stunning, professional-grade monitoring interface comparable to commercial flight simulator instrumentation.

---

## 2. Problem Summary

### 2.1 Current State Issues

1. **Information Overload**: Critical parameters buried across 8 tabs requiring navigation
2. **Outdated Visual Design**: OnGUI-based rendering lacks modern polish, animations, and transitions
3. **Limited Visual Feedback**: No glow effects, minimal color animation, static display elements
4. **Missing Parameters**: Many parameters from the traceability report (IP-0030) not yet displayed
5. **Poor Data Hierarchy**: All parameters treated equally rather than prioritizing critical ones
6. **No Trend Persistence**: Always-on trends not visible without navigating to graph tabs

### 2.2 Technical Limitations

- OnGUI doesn't support modern shader effects (glow, bloom, gradients)
- No smooth interpolation or animation framework
- Per-frame string allocations in OnGUI despite caching efforts
- GL.Lines rendering limits arc gauge quality
- No separation of update rate from display rate

---

## 3. Requirements

### 3.1 Functional Requirements

| ID | Requirement | Source |
|----|-------------|--------|
| FR-01 | Display all parameters from IP-0030 Section 5 traceability list | IP-0030 |
| FR-02 | Primary screen shows all critical parameters without tabs | User Request |
| FR-03 | Secondary tabs provide detailed system views | User Request |
| FR-04 | Always-visible mini-trends for key parameters | IP-0030 §5.9 |
| FR-05 | Real-time alarm/limit status indicators | IP-0030 §5.8 |
| FR-06 | Must load as additive overlay, not default startup | User Request |
| FR-07 | Toggle visibility with F1 key (preserve existing) | Existing |

### 3.2 Visual Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| VR-01 | Arc gauges with glow effects and smooth needle movement | Critical |
| VR-02 | Bidirectional gauges for signed values (surge, net CVCS) | Critical |
| VR-03 | Strip charts with configurable time windows | Critical |
| VR-04 | Animated transitions between tabs/views | High |
| VR-05 | Color-coded alarm states with pulsing/glow | High |
| VR-06 | Professional dark theme matching control room aesthetics | High |
| VR-07 | Responsive layout for different screen resolutions | Medium |

### 3.3 Performance Requirements

| ID | Requirement | Target |
|----|-------------|--------|
| PR-01 | Dashboard refresh rate | ≥ 30 Hz visual, 10 Hz data |
| PR-02 | Memory allocation | Zero per-frame allocations |
| PR-03 | CPU overhead | < 2ms per frame at 60 FPS |
| PR-04 | Startup time | < 500ms to first paint |

---

## 4. Architecture Design

### 4.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                    ValidationDashboardController                     │
│  - Scene lifecycle management                                        │
│  - F1 toggle handler                                                 │
│  - Engine reference binding                                          │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       ValidationDashboardCanvas                       │
│  - uGUI Canvas (Screen Space - Overlay)                              │
│  - CanvasScaler (Scale With Screen Size)                             │
│  - GraphicRaycaster                                                   │
└─────────────────────────────────────────────────────────────────────┘
                                    │
        ┌───────────────────────────┼───────────────────────────┐
        ▼                           ▼                           ▼
┌───────────────┐         ┌───────────────┐         ┌───────────────┐
│  HeaderPanel  │         │  MainContent  │         │  MiniTrends   │
│  - Plant Mode │         │  - TabSystem  │         │  - 8 strips   │
│  - Phase      │         │  - ViewPanels │         │  - Always on  │
│  - Time/Accel │         │               │         │               │
└───────────────┘         └───────────────┘         └───────────────┘
```

### 4.2 Panel Hierarchy

```
ValidationDashboardCanvas
├── HeaderPanel (fixed top bar)
│   ├── PlantModeIndicator
│   ├── PhaseDescription
│   ├── SimTimeDisplay
│   ├── WallTimeDisplay
│   └── TimeAccelerationControl
│
├── MainContentArea
│   ├── TabNavigation
│   │   ├── OverviewTab (default)
│   │   ├── PrimaryTab
│   │   ├── PressurizerTab
│   │   ├── CVCSTab
│   │   ├── SGRHRTab
│   │   ├── AlarmsTab
│   │   └── ValidationTab
│   │
│   └── TabContentPanels
│       ├── OverviewPanel (PRIMARY - always available)
│       │   ├── GlobalHealthSection
│       │   ├── ReactorCoreSection
│       │   ├── RCSSection
│       │   ├── PressurizerSection
│       │   ├── CVCSSection
│       │   ├── SGSection
│       │   └── AlarmSummarySection
│       │
│       ├── PrimaryPanel
│       │   ├── LoopADisplay
│       │   ├── LoopBDisplay
│       │   ├── LoopCDisplay
│       │   └── LoopDDisplay
│       │
│       ├── PressurizerPanel
│       │   ├── PZRStateGauges
│       │   ├── HeaterControlDisplay
│       │   ├── SprayControlDisplay
│       │   └── SurgeLineDisplay
│       │
│       ├── CVCSPanel
│       │   ├── ChargingSection
│       │   ├── LetdownSection
│       │   ├── VCTSection
│       │   └── BRSSection
│       │
│       ├── SGRHRPanel
│       │   ├── SG1-4Displays
│       │   └── RHRSystemDisplay
│       │
│       ├── AlarmsPanel
│       │   ├── AnnunciatorMatrix
│       │   └── EventLogScroll
│       │
│       └── ValidationPanel
│           ├── ConservationChecks
│           ├── RVLISDisplay
│           └── DebugTelemetry
│
└── MiniTrendsPanel (fixed right edge)
    ├── PressureTrend
    ├── LevelTrend
    ├── TemperatureTrend
    ├── SubcoolingTrend
    ├── ChargingTrend
    ├── LetdownTrend
    ├── SurgeTrend
    └── MassConsTrend
```

### 4.3 Component Architecture

#### 4.3.1 Gauge Components

| Component | Purpose | Visual Style |
|-----------|---------|--------------|
| `ArcGauge` | Standard 180° sweep gauge | Glowing arc, animated needle |
| `BidirectionalGauge` | ±range center-zero gauge | 270° sweep, dual-color |
| `LinearGauge` | Horizontal/vertical bar | Gradient fill, tick marks |
| `DigitalReadout` | Large numeric display | Segmented font, glow |
| `StatusIndicator` | Boolean on/off state | Pill shape, pulsing glow |
| `MiniTrendStrip` | Compact time series | 5-min rolling window |

#### 4.3.2 Data Binding

```csharp
public interface IEngineDataBinding
{
    void BindToEngine(HeatupSimEngine engine);
    void UpdateFromEngine();  // Called at data refresh rate
    void UpdateVisuals();     // Called at visual refresh rate
}
```

#### 4.3.3 Animation System

```csharp
public class GaugeAnimator : MonoBehaviour
{
    [SerializeField] float smoothTime = 0.1f;
    [SerializeField] AnimationCurve easingCurve;
    
    private float currentValue;
    private float targetValue;
    private float velocity;
    
    public void SetTarget(float value) => targetValue = value;
    
    void Update()
    {
        currentValue = Mathf.SmoothDamp(
            currentValue, targetValue, ref velocity, smoothTime);
        ApplyToGauge(currentValue);
    }
}
```

---

## 5. Parameter Mapping

### 5.1 Global Simulation Health (IP-0030 §5.1)

| Parameter | Display Type | Location | Source Field |
|-----------|--------------|----------|--------------|
| Sim Time (s/hr) | Digital | Header | `simTime` |
| Sim Rate | Dropdown | Header | `currentSpeedIndex` |
| Paused/Running | Indicator | Header | `isRunning` |
| Timestep dt | Digital | Validation | `DP0003_DETERMINISTIC_TIMESTEP_HR` |
| Mass Conservation Error | Gauge + Trend | Overview, Validation | `massError_lbm` |
| Energy Conservation Error | Gauge + Trend | Overview, Validation | (derived) |
| Total Heat Added | Digital | Overview | (RCP + PZR heater) |
| Total Heat Removed | Digital | Overview | `sgHeatTransfer_MW + rhrHXRemoval_MW` |
| Net Heat | BiGauge | Overview | `netPlantHeat_MW` |

### 5.2 Reactor / Core (IP-0030 §5.2)

| Parameter | Display Type | Location | Source Field |
|-----------|--------------|----------|--------------|
| Reactor Power % | ArcGauge | Overview | (future - currently 0) |
| Core Inlet Temp | Digital | Primary | `T_cold` |
| Core Outlet Temp | Digital | Primary | `T_hot` |
| Tcold | ArcGauge | Overview, Primary | `T_cold` |
| Thot | ArcGauge | Overview, Primary | `T_hot` |
| Tavg | ArcGauge | Overview | `T_avg` |
| Core ΔT | Digital | Primary | `T_hot - T_cold` |
| Core Flow | Digital | Primary | (derived from RCP state) |

### 5.3 RCS Primary Loops (IP-0030 §5.3)

| Parameter | Display Type | Location | Source Field |
|-----------|--------------|----------|--------------|
| RCS Pressure | ArcGauge + Trend | Overview, PZR | `pressure` |
| RCS Total Mass | Digital | CVCS | `totalSystemMass_lbm` |
| Subcooling Margin | ArcGauge + Trend | Overview | `subcooling` |
| RCP Status (×4) | StatusIndicator | Primary | `rcpRunning[0-3]` |
| RCP Count | Digital | Overview | `rcpCount` |
| Pump Head | Digital | Primary | (derived) |
| Natural Circ Est. | Digital | Primary | (when RCPs off) |

### 5.4 Pressurizer (IP-0030 §5.4)

| Parameter | Display Type | Location | Source Field |
|-----------|--------------|----------|--------------|
| PZR Pressure | ArcGauge | PZR | `pressure` |
| PZR Level | ArcGauge + Trend | Overview, PZR | `pzrLevel` |
| Liquid Mass | Digital | PZR | `pzrWaterVolume × ρ` |
| Steam Mass | Digital | PZR | `pzrSteamVolume × ρ` |
| PZR Temperature | ArcGauge | PZR | `T_pzr` |
| Tsat | Digital | PZR | `T_sat` |
| Heater Mode | Indicator | PZR | `currentHeaterMode` |
| Heater Demand | LinearGauge | PZR | `pzrHeaterPower` |
| Heater Actual | LinearGauge | PZR | `pzrHeaterPower` |
| Heater Groups | StatusMatrix | PZR | (combined display) |
| Spray Position | LinearGauge | PZR | `sprayValvePosition` |
| Spray Flow | Digital | PZR | `sprayFlow_GPM` |
| Surge Flow | BiGauge + Trend | Overview, PZR | `surgeFlow` |
| Pressure Error | Digital | PZR | `solidPlantPressureError` |
| Level Error | Digital | PZR | `pzrLevel - setpoint` |

### 5.5 CVCS, VCT, BRS (IP-0030 §5.5)

| Parameter | Display Type | Location | Source Field |
|-----------|--------------|----------|--------------|
| Charging Pump Status | StatusIndicator | CVCS | `chargingActive` |
| Charging Flow | ArcGauge + Trend | Overview, CVCS | `chargingFlow` |
| Charging to RCS | Digital | CVCS | `chargingToRCS` |
| Letdown Flow | ArcGauge + Trend | Overview, CVCS | `letdownFlow` |
| Letdown Orifice States | StatusMatrix | CVCS | `orifice75Count`, `orifice45Open` |
| Net CVCS Flow | BiGauge | Overview, CVCS | `chargingFlow - letdownFlow` |
| VCT Level | ArcGauge | Overview, CVCS | `vctState.Level_percent` |
| VCT Boron | Digital | CVCS | `vctState.BoronConcentration_ppm` |
| VCT Makeup Active | Indicator | CVCS | `vctMakeupActive` |
| VCT Divert Active | Indicator | CVCS | `vctDivertActive` |
| BRS Tank Level | ArcGauge | CVCS | `brsState` (computed) |
| BRS Boron Conc | Digital | CVCS | `brsState.HoldupBoron_ppm` |
| BRS-to-VCT Flow | BiGauge | CVCS | `brsState.InFlow - ReturnFlow` |

### 5.6 RHR (IP-0030 §5.6)

| Parameter | Display Type | Location | Source Field |
|-----------|--------------|----------|--------------|
| RHR Mode | Indicator | SG/RHR | `rhrModeString` |
| RHR Active | StatusIndicator | SG/RHR | `rhrActive` |
| RHR Flow | Digital | SG/RHR | `rhrState.FlowRate_gpm` |
| RHR HX Inlet Temp | Digital | SG/RHR | (T_rcs when active) |
| RHR HX Outlet Temp | Digital | SG/RHR | (derived) |
| RHR Heat Removed | Digital | SG/RHR | `rhrHXRemoval_MW` |
| RHR Pump Heat | Digital | SG/RHR | `rhrPumpHeat_MW` |
| RHR Net Heat | BiGauge | SG/RHR | `rhrNetHeat_MW` |

### 5.7 Steam Generators (IP-0030 §5.7)

| Parameter | Display Type | Location | Source Field |
|-----------|--------------|----------|--------------|
| SG Secondary Pressure | ArcGauge | Overview, SG/RHR | `sgSecondaryPressure_psia` |
| SG Level (Wide) | LinearGauge | SG/RHR | `sgWideRangeLevel_pct` |
| SG Level (Narrow) | LinearGauge | SG/RHR | `sgNarrowRangeLevel_pct` |
| SG Secondary Temp | Digital | SG/RHR | `T_sg_secondary` |
| SG Saturation Temp | Digital | SG/RHR | `sgSaturationTemp_F` |
| SG Heat Transfer | Digital | Overview, SG/RHR | `sgHeatTransfer_MW` |
| SG Boiling Active | Indicator | SG/RHR | `sgBoilingActive` |
| Steam Dump Active | Indicator | SG/RHR | `steamDumpActive` |
| Steam Dump Heat | Digital | SG/RHR | `steamDumpHeat_MW` |

### 5.8 Safety / Limits / Alarms (IP-0030 §5.8)

| Parameter | Display Type | Location | Source Field |
|-----------|--------------|----------|--------------|
| High RCS Pressure | Annunciator | Alarms | `pressureHigh` |
| Low RCS Pressure | Annunciator | Alarms | `pressureLow` |
| High PZR Level | Annunciator | Alarms | `pzrLevelHigh` |
| Low PZR Level | Annunciator | Alarms | `pzrLevelLow` |
| Low Subcooling | Annunciator | Alarms | `subcoolingLow` |
| VCT Level High | Annunciator | Alarms | `vctLevelHigh` |
| VCT Level Low | Annunciator | Alarms | `vctLevelLow` |
| Mass Conservation Alarm | Annunciator | Alarms | `primaryMassAlarm` |

### 5.9 Always-On Trends (IP-0030 §5.9)

| Parameter | Strip Chart | Source History |
|-----------|-------------|----------------|
| RCS Pressure | MiniTrend | `pressHistory` |
| PZR Level | MiniTrend | `pzrLevelHistory` |
| Heater Demand vs Actual | MiniTrend | `heaterPIDOutputHistory` |
| Spray Flow | MiniTrend | `sprayFlowHistory` |
| Charging | MiniTrend | `chargingHistory` |
| Letdown | MiniTrend | `letdownHistory` |
| Net CVCS | MiniTrend | (derived) |
| Tavg/Thot/Tcold | MiniTrend | `tempHistory`, `tHotHistory`, `tColdHistory` |
| SG Pressure | MiniTrend | (new buffer needed) |
| Mass/Energy Errors | MiniTrend | (new buffer needed) |

---

## 6. Visual Design Specification

### 6.1 Color Palette

| Color | Hex | Usage |
|-------|-----|-------|
| Background Dark | `#0F1118` | Main background |
| Panel Background | `#171A24` | Panel containers |
| Header Background | `#0C0E14` | Header bar |
| Normal Green | `#2ED940` | Safe/normal state |
| Warning Amber | `#FFC700` | Warning conditions |
| Alarm Red | `#FF2E2E` | Alarm conditions |
| Cyan Info | `#00D9F2` | Informational |
| Blue Accent | `#3380FF` | Active/selected |
| Text Primary | `#EBEDF2` | Main text |
| Text Secondary | `#8C94A6` | Dim labels |
| Gauge Arc Background | `#262933` | Unlit arc |
| Glow Base | `#00FFFF40` | Glow effect base |

### 6.2 Typography

| Element | Font | Size | Weight |
|---------|------|------|--------|
| Header Labels | Roboto Mono | 14px | Bold |
| Gauge Values | Roboto Mono | 24px | Bold |
| Gauge Labels | Roboto | 10px | Regular |
| Section Headers | Roboto | 12px | Bold |
| Status Text | Roboto | 11px | Regular |
| Digital Readout | Seven Segment | 32px | Regular |

### 6.3 Gauge Specifications

#### Arc Gauge (Standard)
- **Sweep:** 180° (left = min, right = max)
- **Arc Width:** 8px
- **Glow Width:** 16px (blur radius)
- **Needle:** Triangle, 3px base, animated at 30fps
- **Digital Readout:** Centered below arc
- **Color Bands:** Green (0-70%), Amber (70-90%), Red (90-100%)

#### Bidirectional Gauge
- **Sweep:** 270° (bottom-left through top to bottom-right)
- **Center Position:** 12 o'clock = zero
- **Positive Color:** Blue accent (right deflection)
- **Negative Color:** Orange accent (left deflection)
- **Zero Tick:** 3px white vertical line at top

#### Linear Gauge (Bar)
- **Orientation:** Horizontal (default) or Vertical
- **Height:** 12px (horizontal) or Width: 20px (vertical)
- **Gradient:** Green → Amber → Red
- **Tick Marks:** Every 25%
- **Current Value Marker:** 2px white vertical line

### 6.4 Animation Specifications

| Element | Property | Duration | Easing |
|---------|----------|----------|--------|
| Needle Position | Rotation | 100ms | EaseOutQuad |
| Value Text | Color | 200ms | Linear |
| Glow Intensity | Alpha | 300ms | EaseInOut |
| Tab Transition | Position | 250ms | EaseOutCubic |
| Panel Fade | Alpha | 150ms | Linear |
| Alarm Pulse | Scale + Alpha | 500ms | Sine loop |

### 6.5 Glow Effect Implementation

```csharp
// Shader: UI/GlowArc
// - Base arc (solid color)
// - Bloom pass (blur + additive blend)
// - Intensity driven by value color

[SerializeField] float glowIntensity = 0.6f;
[SerializeField] float glowRadius = 8f;
[SerializeField] Color glowColor;

// Apply via CanvasRenderer material or custom Image shader
```

---

## 7. Implementation Stages

### Stage 1: Foundation (Estimated: 4 hours)

**Objective:** Create the uGUI canvas structure and core controller

**Tasks:**
1. Create `ValidationDashboardCanvas` prefab with proper hierarchy
2. Implement `ValidationDashboardController.cs` with:
   - Scene load/unload management
   - F1 toggle functionality
   - Engine binding
   - Data refresh timer (10 Hz)
   - Visual refresh timer (30 Hz)
3. Create header panel with existing functionality
4. Set up CanvasScaler for resolution independence
5. Create tab navigation system

**Deliverables:**
- `Assets/Prefabs/UI/ValidationDashboardCanvas.prefab`
- `Assets/Scripts/UI/Validation/ValidationDashboardController.cs`
- `Assets/Scripts/UI/Validation/TabNavigationController.cs`

**Exit Criteria:**
- Dashboard toggles with F1
- Header displays correct sim time, mode, phase
- Tabs switch correctly (empty content)

---

### Stage 2: Gauge Components (Estimated: 6 hours)

**Objective:** Create reusable gauge UI components

**Tasks:**
1. Create `ArcGauge` component with:
   - Configurable min/max/thresholds
   - Animated needle movement
   - Glow shader effect
   - Digital value display
2. Create `BidirectionalGauge` component
3. Create `LinearGauge` component
4. Create `DigitalReadout` component
5. Create `StatusIndicator` component
6. Create gauge prefab variants for common use cases

**Deliverables:**
- `Assets/Scripts/UI/Validation/Gauges/ArcGauge.cs`
- `Assets/Scripts/UI/Validation/Gauges/BidirectionalGauge.cs`
- `Assets/Scripts/UI/Validation/Gauges/LinearGauge.cs`
- `Assets/Scripts/UI/Validation/Gauges/DigitalReadout.cs`
- `Assets/Scripts/UI/Validation/Gauges/StatusIndicator.cs`
- `Assets/Shaders/UI/GlowArc.shader`
- `Assets/Prefabs/UI/Gauges/*.prefab`

**Exit Criteria:**
- All gauge types render correctly
- Animations smooth at 30fps
- Glow effects visible and performant

---

### Stage 3: Overview Panel (Estimated: 6 hours)

**Objective:** Implement the primary at-a-glance panel

**Tasks:**
1. Create `OverviewPanel.cs` with data binding
2. Layout all sections per §5 parameter mapping:
   - Global Health: Mass/energy conservation indicators
   - Reactor Core: Tavg, ΔT
   - RCS: Pressure, RCP count, subcooling
   - Pressurizer: Level, pressure, heater/spray status
   - CVCS: Charging/letdown/net flow
   - Steam Generators: Pressure, heat transfer, boiling
   - Alarm Summary: Critical alarm tiles
3. Implement responsive grid layout
4. Add section dividers and headers

**Deliverables:**
- `Assets/Scripts/UI/Validation/Panels/OverviewPanel.cs`
- `Assets/Scripts/UI/Validation/Panels/OverviewSection_*.cs` (7 files)
- Updated `ValidationDashboardCanvas.prefab`

**Exit Criteria:**
- All §5.1-5.8 parameters visible on Overview
- Correct color coding per thresholds
- Readable at 1920×1080 minimum

---

### Stage 4: Detail Panels (Estimated: 8 hours)

**Objective:** Implement system-specific detail panels

**Tasks:**
1. **PrimaryPanel**: Loop temperatures, per-RCP status, flow visualization
2. **PressurizerPanel**: Full PZR state, heater groups, spray control, surge line
3. **CVCSPanel**: Charging/letdown detail, orifice states, VCT, BRS with flows
4. **SGRHRPanel**: Per-SG display (aggregate), RHR system, thermocline visualization
5. **AlarmsPanel**: Annunciator matrix (4×8), scrollable event log
6. **ValidationPanel**: RVLIS, conservation checks, debug telemetry

**Deliverables:**
- `Assets/Scripts/UI/Validation/Panels/PrimaryPanel.cs`
- `Assets/Scripts/UI/Validation/Panels/PressurizerPanel.cs`
- `Assets/Scripts/UI/Validation/Panels/CVCSPanel.cs`
- `Assets/Scripts/UI/Validation/Panels/SGRHRPanel.cs`
- `Assets/Scripts/UI/Validation/Panels/AlarmsPanel.cs`
- `Assets/Scripts/UI/Validation/Panels/ValidationPanel.cs`

**Exit Criteria:**
- All detail panels functional
- Tab switching shows correct panel
- Data updates at 10 Hz

---

### Stage 5: Mini-Trends and Strip Charts (Estimated: 4 hours)

**Objective:** Implement always-visible trend displays

**Tasks:**
1. Create `MiniTrendStrip` component with:
   - Configurable time window (5 min default)
   - Configurable trace colors
   - Horizontal scroll for longer history
   - Min/max auto-scaling
2. Create `MiniTrendsPanel` (right edge)
3. Implement full strip chart for graph tabs
4. Add history buffer bindings

**Deliverables:**
- `Assets/Scripts/UI/Validation/Trends/MiniTrendStrip.cs`
- `Assets/Scripts/UI/Validation/Trends/MiniTrendsPanel.cs`
- `Assets/Scripts/UI/Validation/Trends/StripChart.cs`

**Exit Criteria:**
- 8 mini-trends always visible
- Traces update smoothly
- Auto-scaling works correctly

---

### Stage 6: Polish and Animation (Estimated: 4 hours)

**Objective:** Add professional visual polish

**Tasks:**
1. Implement tab transition animations
2. Add alarm pulse animations
3. Fine-tune glow shader parameters
4. Add panel fade-in on load
5. Optimize material instances (batching)
6. Add hover tooltips for complex values
7. Final color/contrast adjustments

**Deliverables:**
- Updated all panel scripts with animation hooks
- `Assets/Scripts/UI/Validation/Effects/AlarmPulse.cs`
- `Assets/Scripts/UI/Validation/Effects/PanelTransition.cs`
- `Assets/Scripts/UI/Validation/Effects/TooltipController.cs`

**Exit Criteria:**
- All animations smooth at 60fps
- Visual coherence across all panels
- No frame drops during transitions

---

### Stage 7: Integration and Testing (Estimated: 2 hours)

**Objective:** Verify complete system integration

**Tasks:**
1. Verify additive scene loading
2. Test F1 toggle doesn't interfere with operator screens
3. Verify data accuracy against existing OnGUI dashboard
4. Performance profiling (target < 2ms/frame)
5. Resolution testing (1080p, 1440p, 4K)
6. Memory leak testing (toggle on/off 100 cycles)

**Deliverables:**
- Test report document
- Performance metrics log
- Bug fix commits as needed

**Exit Criteria:**
- All functional requirements met
- Performance targets achieved
- No memory leaks detected

---

## 8. File Structure

```
Assets/
├── Prefabs/
│   └── UI/
│       ├── ValidationDashboardCanvas.prefab
│       └── Gauges/
│           ├── ArcGauge.prefab
│           ├── BidirectionalGauge.prefab
│           ├── LinearGauge.prefab
│           ├── DigitalReadout.prefab
│           ├── StatusIndicator.prefab
│           └── MiniTrendStrip.prefab
│
├── Scripts/
│   └── UI/
│       └── Validation/
│           ├── ValidationDashboardController.cs
│           ├── TabNavigationController.cs
│           ├── IEngineDataBinding.cs
│           │
│           ├── Gauges/
│           │   ├── ArcGauge.cs
│           │   ├── BidirectionalGauge.cs
│           │   ├── LinearGauge.cs
│           │   ├── DigitalReadout.cs
│           │   ├── StatusIndicator.cs
│           │   └── GaugeAnimator.cs
│           │
│           ├── Panels/
│           │   ├── HeaderPanel.cs
│           │   ├── OverviewPanel.cs
│           │   ├── PrimaryPanel.cs
│           │   ├── PressurizerPanel.cs
│           │   ├── CVCSPanel.cs
│           │   ├── SGRHRPanel.cs
│           │   ├── AlarmsPanel.cs
│           │   └── ValidationPanel.cs
│           │
│           ├── Trends/
│           │   ├── MiniTrendStrip.cs
│           │   ├── MiniTrendsPanel.cs
│           │   └── StripChart.cs
│           │
│           └── Effects/
│               ├── AlarmPulse.cs
│               ├── PanelTransition.cs
│               └── TooltipController.cs
│
├── Shaders/
│   └── UI/
│       ├── GlowArc.shader
│       └── GradientBar.shader
│
└── Materials/
    └── UI/
        ├── GaugeGlow.mat
        └── TrendLine.mat
```

---

## 9. Dependencies

### 9.1 Unity Packages
- TextMeshPro (already included)
- UI Toolkit (optional, for future migration)

### 9.2 External Assets (Optional)
- Roboto font family (Google Fonts, OFL license)
- Seven Segment font (for digital readouts)

### 9.3 Internal Dependencies
- `HeatupSimEngine` and all partial classes
- `PlantConstants` for thresholds
- `TimeAcceleration` for display formatting

---

## 10. Risks and Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Performance regression | High | Medium | Profile early, batch draw calls |
| Shader compatibility | Medium | Low | Use built-in shaders as fallback |
| Resolution edge cases | Medium | Medium | Test on multiple displays early |
| Data binding complexity | Medium | Medium | Clear interface contract |

---

## 11. Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Frame time overhead | < 2ms | Unity Profiler |
| Memory allocations | 0 per frame | Memory Profiler |
| Parameter coverage | 100% of IP-0030 | Visual audit |
| User feedback | Positive | Subjective assessment |

---

## 12. Approval

**Prepared By:** Claude (AI Assistant)  
**Date:** 2026-02-16  

**Approval Required From:** Craig (Project Lead)

---

## 13. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-02-16 | Claude | Initial draft |
