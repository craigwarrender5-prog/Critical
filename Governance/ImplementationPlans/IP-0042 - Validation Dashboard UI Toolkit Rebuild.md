# IP-0042: Validation Dashboard — UI Toolkit Rebuild

**Date:** 2026-02-17  
**Domain Plan:** DP-0008 (Operator Interface & Scenarios)  
**Predecessors:** IP-0025 (CLOSED), IP-0029 (CLOSED), IP-0030 (FAILED), IP-0031 (CLOSED), IP-0040 (FAILED), IP-0041 (SUPERSEDED)  
**Status:** DRAFT — Awaiting Approval  
**Priority:** Critical  
**Changelog Required:** Yes  
**Target Version:** v0.8.0.0

---

## 1. Executive Summary

Six previous implementation plans attempted to replace the legacy IMGUI-based `HeatupValidationVisual` dashboard with uGUI. All failed due to:
- Font loading path issues (black rectangles)
- Layout system fights (GridLayoutGroup producing "tiny gauges in empty panels")
- Binary prefab dependencies breaking version control
- Incomplete component implementations

**This IP takes a different approach: Unity UI Toolkit.**

UI Toolkit (formerly UIElements) is Unity's recommended UI system for Unity 6.x. It offers:
- **Flexbox layout** — predictable, CSS-like positioning (no more layout group fights)
- **Native data binding** — direct property binding to engine fields
- **Text-based assets** — UXML/USS files are version-controllable (not binary prefabs)
- **Built-in virtualization** — ListView for efficient event log rendering
- **Simpler font handling** — native font rendering, no TMP resource path issues

---

## 2. Problem Summary

### 2.1 Current State

The legacy `HeatupValidationVisual` (OnGUI) dashboard works but has limitations:
- Manual pixel positioning requires constant maintenance
- No data binding — every field manually read and formatted each frame
- Limited styling options
- OnGUI is deprecated technology

The uGUI `ValidationDashboard` attempts (IP-0025 through IP-0041) failed to deliver:
- Black rectangle readouts (font path issues)
- Missing event log, strip charts, 15 annunciator tiles
- ~20% parameter coverage vs. the 100+ required
- Layout problems (gauges floating in dark voids)

### 2.2 Parameter Gap Analysis

Your comprehensive parameter requirements document specifies monitoring for:

| Category | Required Parameters | Currently Displayed | Gap |
|----------|-------------------|---------------------|-----|
| Global Sim Health | 9 | 4 | 5 missing |
| Reactor/Core | 12 | 0 | Not modeled |
| RCS Primary | 14 | 12 | 2 missing |
| Pressurizer | 19 | 15 | 4 missing |
| CVCS | 15 | 10 | 5 missing |
| VCT | 9 | 5 | 4 missing |
| BRS | 6 | 0 | 6 missing |
| RHR | 11 | 6 | 5 missing |
| Steam Generators | 12 | 8 | 4 missing |
| Safety/Alarms | 12 | 8 | 4 missing |
| Always-On Trends | 10 | 7 | 3 missing |
| **TOTAL** | **129** | **~75** | **~54 missing** |

### 2.3 Root Causes of Previous Failures

| Failure Mode | uGUI Cause | UI Toolkit Solution |
|--------------|------------|---------------------|
| Black rectangles | TMP font Resources.Load path | Native font rendering |
| Layout fights | GridLayoutGroup/LayoutGroup conflicts | Flexbox with explicit flex-grow |
| Binary prefabs | Unity serialization | UXML/USS text files |
| Component bloat | Manual RectTransform setup | Declarative UXML hierarchy |
| Data binding | Manual Update() reads | Native INotifyPropertyChanged |
| Event log perf | Manual culling | Built-in ListView virtualization |

---

## 3. UI Toolkit Architecture

### 3.1 Technology Overview

UI Toolkit uses three core concepts:

1. **UXML** — XML-based layout markup (like HTML)
2. **USS** — Unity Style Sheets (like CSS)
3. **C# VisualElement** — Custom element classes for complex widgets

```
Assets/UI/
├── ValidationDashboard.uxml          # Root layout
├── ValidationDashboard.uss           # Global styles
├── Components/
│   ├── ArcGauge.uxml                 # Arc gauge template
│   ├── LinearGauge.uxml              # Bar gauge template
│   ├── DigitalReadout.uxml           # Numeric display template
│   ├── StatusIndicator.uxml          # LED indicator template
│   ├── AnnunciatorTile.uxml          # ISA-18.1 tile template
│   ├── StripChart.uxml               # Trend graph template
│   └── EventLogEntry.uxml            # Log entry template
├── Panels/
│   ├── OverviewPanel.uxml            # Primary operations surface
│   ├── PrimaryPanel.uxml             # RCS detail tab
│   ├── PressurizerPanel.uxml         # PZR detail tab
│   ├── CVCSPanel.uxml                # CVCS detail tab
│   ├── SGRHRPanel.uxml               # SG/RHR detail tab
│   ├── LogPanel.uxml                 # Annunciators + Event log
│   ├── GraphsPanel.uxml              # Full-width strip charts
│   └── ValidationPanel.uxml          # Mass/energy audit
└── Themes/
    └── NuclearInstrument.uss         # Instrument styling theme
```

### 3.2 Custom VisualElement Classes

These require custom C# classes with `generateVisualContent` for drawing:

| Element | Purpose | Drawing Method |
|---------|---------|----------------|
| `ArcGaugeElement` | 180° sweep gauge with needle | `MeshGenerationContext.Painter2D` arc commands |
| `BidirectionalGaugeElement` | 270° center-zero gauge | `Painter2D` arc with center reference |
| `LinearGaugeElement` | Horizontal/vertical bar | Simple rectangle fill |
| `StripChartElement` | Multi-trace trend graph | `Painter2D` polyline + grid |
| `AnnunciatorTileElement` | ISA-18.1 state machine | Rectangle with border + text |

### 3.3 Data Binding Strategy

UI Toolkit supports two binding approaches:

**Option A: Direct Property Binding (Recommended)**
```csharp
// In ValidationDashboardController
public float T_avg => engine.T_avg;
public float Pressure => engine.pressure;

// In UXML
<ui:Label binding-path="T_avg" />
```

**Option B: Manual Binding with INotifyBindablePropertyChanged**
```csharp
public class DashboardViewModel : INotifyBindablePropertyChanged
{
    private float _tavg;
    public float T_avg 
    { 
        get => _tavg; 
        set { _tavg = value; Notify(); }
    }
}
```

For this project, we'll use **Option A** with a thin ViewModel wrapper that reads from `HeatupSimEngine` at 10Hz.

---

## 4. Primary Operations Surface Layout

The Overview panel uses Flexbox layout with 5 columns + footer:

```
┌────────────────────────────────────────────────────────────────────────────────┐
│  HEADER: [Mode] │ Phase │ Sim Time │ Wall Time │ Speed │ ⚠ Alarms            │
├──────────┬───────────┬───────────┬───────────┬─────────────────────────────────┤
│  RCS     │ PRESSUR-  │ CVCS &    │ SG &      │  ALWAYS-ON TRENDS              │
│  PRIMARY │ IZER      │ VCT       │ RHR       │  (8 mini strip charts)         │
│          │           │           │           │                                │
│  flex:1  │  flex:0.9 │  flex:0.9 │  flex:0.9 │  flex:1.3                      │
├──────────┴───────────┴───────────┴───────────┼─────────────────────────────────┤
│  ANNUNCIATOR PANEL (27 tiles)                 │  EVENT LOG (virtualized list)  │
│  flex:1.5                                     │  flex:1                        │
└───────────────────────────────────────────────┴─────────────────────────────────┘
```

USS Flexbox layout:
```css
.overview-container {
    flex-direction: column;
    flex-grow: 1;
}

.overview-main {
    flex-direction: row;
    flex-grow: 1;
}

.column-rcs { flex-grow: 1; }
.column-pzr { flex-grow: 0.9; }
.column-cvcs { flex-grow: 0.9; }
.column-sgrhr { flex-grow: 0.9; }
.column-trends { flex-grow: 1.3; }

.overview-footer {
    flex-direction: row;
    height: 38%;
}
```

---

## 5. Complete Parameter Inventory

All parameters from IP-0041 Section 4 are retained, plus these additions from your requirements document:

### 5.1 New Parameters (Not in IP-0041)

| Parameter | Engine Field | Location | Display |
|-----------|-------------|----------|---------|
| Integration stability | `pzrClosureConverged` | [TAB] VALID | StatusIndicator |
| Energy conservation | Derived | [TAB] VALID | Digital + trend |
| Total heat in | `rcpHeat + pzrHeaterPower + rhrPumpHeat_MW` | [OV] Footer | Digital |
| Total heat out | `sgHeatTransfer_MW + rhrHXRemoval_MW + steamDumpHeat_MW` | [OV] Footer | Digital |
| Net heat balance | `netPlantHeat_MW` | [OV] Trends | Digital + trend |
| Heater limiter active | `heaterPressureRateClampActive` | [TAB] PZR | StatusIndicator |
| Heater ramp limited | `heaterRampRateClampActive` | [TAB] PZR | StatusIndicator |
| Letdown orifice count | `orifice75Count`, `orifice45Open` | [TAB] CVCS | Digital + text |
| Letdown via RHR/Orifice | `letdownViaRHR`, `letdownViaOrifice` | [TAB] CVCS | StatusIndicator |
| VCT inflow rate | Derived from `vctState` | [TAB] CVCS | Digital gpm |
| VCT outflow rate | Derived from `vctState` | [TAB] CVCS | Digital gpm |
| BRS holdup level | `BRSPhysics.GetHoldupLevelPercent(brsState)` | [TAB] CVCS | ArcGauge |
| BRS inflow | `brsState.InFlow_gpm` | [TAB] CVCS | Digital |
| BRS return | `brsState.ReturnFlow_gpm` | [TAB] CVCS | Digital |
| RHR suction source | `rhrState` | [TAB] SG/RHR | Text |
| RHR discharge path | `rhrState` | [TAB] SG/RHR | Text |
| RHR permissive | Derived | [TAB] SG/RHR | StatusIndicator |
| Primary mass ledger | `primaryMassLedger_lb` | [TAB] VALID | Digital |
| Primary mass drift | `primaryMassDrift_lb` | [TAB] VALID | Digital + alarm |
| PBOC event count | `pbocEventCount` | [TAB] VALID | Digital |

---

## 6. Implementation Stages

### Stage 0: Proof of Concept — Custom Gauge Rendering (VALIDATION GATE)
**Objective:** Validate that UI Toolkit can render custom arc gauges with acceptable performance before committing to full rebuild.

**Tasks:**
1. Create minimal test scene with UI Toolkit `UIDocument`
2. Implement `ArcGaugeElement.cs` using `MeshGenerationContext.Painter2D`
   - 180° sweep arc with configurable min/max
   - Needle animation via `schedule.Execute()`
   - Threshold coloring (green/amber/red bands)
3. Implement `StripChartElement.cs` using `Painter2D.LineTo()`
   - Ring buffer data source
   - Auto-scaling Y axis
   - Grid lines
4. Bind test gauges to live `HeatupSimEngine` data
5. Measure frame time impact (target: <1ms for 10 gauges + 2 charts)

**Deliverables:**
- `Assets/Scripts/UI/UIToolkit/Elements/ArcGaugeElement.cs`
- `Assets/Scripts/UI/UIToolkit/Elements/StripChartElement.cs`
- `Assets/UI/Test/GaugeTest.uxml`
- Performance measurement log

**Exit Criteria:**
- Arc gauge renders smoothly with animated needle
- Strip chart renders 4-hour rolling history with 6 traces
- Frame time < 1ms for test scene
- **Craig approval to proceed** — if gauges look bad or performance is poor, STOP and reassess

**CRITICAL:** Do not proceed to Stage 1 without explicit approval. This gate prevents another 5-IP failure chain.

---

### Stage 1: Core Infrastructure
**Objective:** Establish UI Toolkit foundation and theme.

**Tasks:**
1. Create `Assets/UI/` folder structure per Section 3.1
2. Create `NuclearInstrument.uss` theme:
   - Background colors: `#0a0a12` (dark), `#141420` (panel)
   - Text colors: `#00ff88` (primary), `#ffaa00` (warning), `#ff4444` (alarm)
   - Font sizes: 24px (gauge values), 14px (labels), 12px (secondary)
   - Glow effects via `text-shadow` for alarm states
3. Create `ValidationDashboardDocument.cs` — MonoBehaviour that loads UXML and binds to engine
4. Create `DashboardViewModel.cs` — thin wrapper exposing engine properties for binding
5. Implement remaining custom elements:
   - `LinearGaugeElement.cs`
   - `BidirectionalGaugeElement.cs`
   - `DigitalReadoutElement.cs`
   - `StatusIndicatorElement.cs`
   - `AnnunciatorTileElement.cs`
6. Create UXML templates for each element

**Deliverables:**
- Complete `Assets/UI/` folder structure
- `NuclearInstrument.uss` theme
- 7 custom VisualElement classes
- 7 UXML component templates

**Exit Criteria:**
- All custom elements render correctly in isolation
- Theme colors match legacy dashboard aesthetic
- No console errors

---

### Stage 2: Overview Panel — Primary Operations Surface
**Objective:** Build the main dashboard view with all critical parameters visible.

**Tasks:**
1. Create `OverviewPanel.uxml` with 5-column + footer Flexbox layout
2. Implement `OverviewPanel.cs` VisualElement class:
   - Query all child elements by name
   - Bind to `DashboardViewModel` properties
   - Update at 10Hz via `schedule.Execute()`
3. Populate Column 1 (RCS): 2 ArcGauges (T_avg, Subcooling) + 10 DigitalReadouts
4. Populate Column 2 (PZR): 2 ArcGauges (Pressure, Level) + 2 LinearGauges + 1 BidirectionalGauge + 8 readouts
5. Populate Column 3 (CVCS): 1 ArcGauge (VCT Level) + 1 BidirectionalGauge + 12 readouts + StatusIndicators
6. Populate Column 4 (SG/RHR): 1 ArcGauge (SG Press) + 1 LinearGauge (HZP) + 10 readouts + StatusIndicators
7. Populate Column 5 (Trends): 8 mini StripChartElements
8. Populate Footer Left: 27 AnnunciatorTileElements in 3-row grid
9. Populate Footer Right: ListView for event log with virtualization
10. Wire keyboard shortcuts: F1 toggle, Ctrl+1-8 tabs, F5-F9 speed

**Deliverables:**
- `OverviewPanel.uxml` + `OverviewPanel.cs`
- Complete parameter coverage per Section 4 of IP-0041
- Working event log with severity filtering

**Exit Criteria:**
- All 80+ Overview parameters visible on 1920×1080
- No scrolling required for critical parameters
- 27 annunciator tiles with correct state colors
- Event log auto-scrolls with severity filter buttons
- All keyboard shortcuts functional

---

### Stage 3: Detail Tabs
**Objective:** Build system-specific detail tabs with hero gauges and full strip charts.

**Tasks:**
1. Create tab navigation bar in root UXML
2. Implement `PrimaryPanel.uxml/.cs` — RCS detail + TEMPS/PRESSURE charts
3. Implement `PressurizerPanel.uxml/.cs` — PZR detail + PRESSURE/RATES charts
4. Implement `CVCSPanel.uxml/.cs` — CVCS/VCT/BRS detail + CVCS/VCT-BRS charts
5. Implement `SGRHRPanel.uxml/.cs` — SG/RHR detail + SG/HZP charts
6. Implement `LogPanel.uxml/.cs` — Full annunciator grid + expanded event log
7. Implement `GraphsPanel.uxml/.cs` — 7-category tabbed full-width strip charts
8. Implement `ValidationPanel.uxml/.cs` — Mass/energy audit, RVLIS, conservation checks
9. Add tab transition animations (opacity fade, 150ms)

**Deliverables:**
- 7 detail panel UXML/CS pairs
- Tab navigation with Ctrl+1-8 shortcuts
- Full strip chart implementation with all 7 graph categories

**Exit Criteria:**
- Each tab loads in <100ms
- Strip charts render full 4-hour history
- All parameters from IP-0041 Section 4 visible across tabs
- Smooth tab transitions

---

### Stage 4: Polish and Integration
**Objective:** Final polish, animation, and legacy system deprecation.

**Tasks:**
1. Implement gauge needle animation (SmoothDamp equivalent)
2. Implement annunciator flash rates: 3Hz alerting, 0.7Hz clearing
3. Add alarm glow effects via USS `text-shadow`
4. Add setpoint markers to LinearGauges
5. Performance profile: verify <2ms per frame
6. Test at 1080p, 1440p, 4K
7. Verify no console errors during 30-minute simulation
8. Add USS media queries for different resolutions
9. Disable legacy `HeatupValidationVisual` component (keep files)
10. Update `ScreenManager` to use new dashboard

**Deliverables:**
- Polished, animated dashboard
- Resolution-responsive layout
- Legacy system disabled

**Exit Criteria:**
- Frame time <2ms at 10Hz update
- No console errors in 30-minute run
- Professional appearance matching control room aesthetic
- Craig approval: "This is the dashboard"

---

### Stage 5: Documentation and Changelog
**Objective:** Complete documentation and version release.

**Tasks:**
1. Write changelog (CHANGELOG_v0.8.0.0.md)
2. Update PROJECT_OVERVIEW.md with UI Toolkit architecture
3. Create UI Toolkit developer guide for future element additions
4. Archive uGUI ValidationDashboard code (do not delete)
5. Update Future_Features.md with any deferred items

**Deliverables:**
- CHANGELOG_v0.8.0.0.md
- Updated documentation
- Archived uGUI code

---

## 7. Risk Mitigation

### 7.1 Stage 0 Validation Gate

The critical risk is discovering that UI Toolkit cannot adequately render custom gauges or has unacceptable performance. **Stage 0 exists to validate these assumptions before investing in a full rebuild.**

If Stage 0 fails:
- **Option A:** Fall back to enhanced OnGUI (IP-0043)
- **Option B:** Investigate hybrid approach (UI Toolkit for layout, custom GL for gauges)
- **Option C:** Accept uGUI limitations and fix incrementally

### 7.2 Rollback Strategy

The legacy `HeatupValidationVisual` remains fully functional throughout this IP. At any stage:
- Re-enable `HeatupValidationVisual` component
- Disable `ValidationDashboardDocument`
- No data loss, no physics impact

### 7.3 Performance Monitoring

Each stage includes performance verification:
- Stage 0: <1ms for 10 gauges
- Stage 2: <1.5ms for full Overview
- Stage 4: <2ms total frame budget

If any stage exceeds its budget by >50%, stop and optimize before proceeding.

---

## 8. File Manifest

### New Files

```
Assets/
├── UI/
│   ├── ValidationDashboard.uxml
│   ├── ValidationDashboard.uss
│   ├── Components/
│   │   ├── ArcGauge.uxml
│   │   ├── LinearGauge.uxml
│   │   ├── BidirectionalGauge.uxml
│   │   ├── DigitalReadout.uxml
│   │   ├── StatusIndicator.uxml
│   │   ├── AnnunciatorTile.uxml
│   │   ├── StripChart.uxml
│   │   └── EventLogEntry.uxml
│   ├── Panels/
│   │   ├── OverviewPanel.uxml
│   │   ├── PrimaryPanel.uxml
│   │   ├── PressurizerPanel.uxml
│   │   ├── CVCSPanel.uxml
│   │   ├── SGRHRPanel.uxml
│   │   ├── LogPanel.uxml
│   │   ├── GraphsPanel.uxml
│   │   └── ValidationPanel.uxml
│   └── Themes/
│       └── NuclearInstrument.uss
├── Scripts/
│   └── UI/
│       └── UIToolkit/
│           ├── ValidationDashboardDocument.cs
│           ├── DashboardViewModel.cs
│           └── Elements/
│               ├── ArcGaugeElement.cs
│               ├── LinearGaugeElement.cs
│               ├── BidirectionalGaugeElement.cs
│               ├── DigitalReadoutElement.cs
│               ├── StatusIndicatorElement.cs
│               ├── AnnunciatorTileElement.cs
│               ├── StripChartElement.cs
│               └── EventLogListController.cs
```

### Modified Files

| File | Change |
|------|--------|
| `HeatupValidationVisual.cs` | Disable component (keep code) |
| `ScreenManager.cs` | Add UI Toolkit dashboard toggle |

### Archived Files

The existing `Assets/Scripts/UI/ValidationDashboard/` folder will be moved to `Assets/Scripts/UI/ValidationDashboard_uGUI_Archive/` for reference.

---

## 9. Success Criteria

### Minimum Viable Dashboard (Stage 2 Complete)
- [ ] Overview panel displays 80+ parameters
- [ ] All 27 annunciator tiles functional
- [ ] Event log with severity filtering
- [ ] 8 always-on trend sparklines
- [ ] Keyboard shortcuts working
- [ ] No black rectangles or layout issues

### Full Dashboard (Stage 4 Complete)
- [ ] All 8 tabs functional
- [ ] 100+ parameters displayed across tabs
- [ ] 7-category strip chart system
- [ ] Smooth animations
- [ ] <2ms frame time
- [ ] Professional control room aesthetic

### Project Success (Stage 5 Complete)
- [ ] Craig assessment: "This is the dashboard we needed"
- [ ] Legacy system safely disabled
- [ ] Documentation complete
- [ ] No regressions in simulation functionality

---

## 10. Unaddressed Issues (Future Work)

The following are explicitly **out of scope** for IP-0042:

1. **Reactor/Core parameters** — Requires physics model additions (future IP)
2. **Loop-by-loop temperatures** — Engine currently calculates aggregate only
3. **Charging/letdown line temperatures** — Not currently modeled
4. **VCT gas blanket pressure** — Not currently modeled
5. **Feedwater/steam flow** — SG secondary model incomplete
6. **In-game help system (F1)** — Deferred to Future_Features.md
7. **Scenario selector** — Deferred to Future_Features.md

These items should be added to `Future_Features.md` upon IP-0042 completion.

---

## 11. Approval

- [ ] **Stage 0 approved to begin** — Craig
- [ ] **Stage 0 exit criteria met** — Craig
- [ ] **Proceed to Stage 1** — Craig
- [ ] **Stage 2 complete** — Craig
- [ ] **Stage 4 complete (MVP)** — Craig
- [ ] **IP-0042 closed** — Craig

---

*Implementation Plan prepared by Claude*  
*Date: 2026-02-17*
