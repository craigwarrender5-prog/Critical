# Implementation Plan: CS-0127 - Validation Dashboard UI Toolkit Overhaul

**Issue ID:** CS-0127  
**Title:** Validation Dashboard Complete Overhaul Using Unity UI Toolkit  
**Domain:** Operator Interface & Scenarios (DP-0008)  
**Version:** 1.0.0  
**Date:** 2026-02-18  
**Status:** STAGE 1 COMPLETE

---

## 1. Problem Summary

The current validation dashboard implementation has two parallel OnGUI-based systems:
- `HeatupValidationVisual.cs` - Legacy debug dashboard
- `ValidationDashboard.cs` (uGUI) - Newer but still limited

Neither system can achieve the professional animated aesthetic required for an operator training simulator:
- OnGUI cannot render smooth arc gauge animations
- Limited visual effects (no glow, no smooth transitions)
- Maintenance burden of two parallel systems
- Performance concerns with immediate-mode rendering

## 2. Expected Outcome

A single, unified UI Toolkit dashboard with:
- Professional modern aesthetics rivaling commercial training simulators
- Animated arc gauges with smooth needle movement
- Animated visualizations (pressurizer vessel, SG diagrams)
- 5Hz data refresh with smooth visual interpolation
- Tab-based navigation with CRITICAL overview as primary view
- <2ms UI update cost per frame

## 3. Implementation Stages

### Stage 1: Foundation (COMPLETE ✓)

**Objective:** Create core UI Toolkit elements and dashboard infrastructure.

**Files Created:**
```
Assets/Scripts/UI/UIToolkit/ValidationDashboard/
├── Elements/
│   ├── ArcGaugeElement.cs          (270° arc gauge with animated needle)
│   ├── LEDIndicatorElement.cs      (Status LED with glow effect)
│   └── DigitalReadoutElement.cs    (Numeric display with trend arrow)
├── UITKDashboardController.cs      (Main controller with 5Hz refresh)
├── UITKDashboardSceneSetup.cs      (Scene integration helper)
└── UITKDashboardTheme.cs           (Color palette constants)

Assets/UI/UIToolkit/ValidationDashboard/
├── ValidationDashboard.uxml        (Bootstrap layout template)
└── ValidationDashboard.uss         (Master stylesheet)
```

**Features Implemented:**
- ArcGaugeElement with:
  - 270° sweep (135° to 405°)
  - Smooth needle animation via SmoothDamp
  - Track + value arc rendering
  - Threshold-based coloring (normal/warning/alarm)
  - Digital value readout with units
  - Label display
  - USS custom properties for styling

- LEDIndicatorElement with:
  - On/Off state rendering
  - Glow effect for active state
  - Flash animation support
  - Optional label

- DigitalReadoutElement with:
  - Formatted numeric display
  - Trend arrow (▲ ▼ ►)
  - Threshold-based coloring
  - Auto-trend detection

- UITKDashboardController with:
  - 5Hz data refresh from HeatupSimEngine
  - Animation update loop (every frame)
  - Tab bar with 8 tabs (CRITICAL first)
  - Keyboard input (Ctrl+1-8 tabs, F5-F9 time accel)
  - CRITICAL tab with 6 arc gauges, 4 RCP LEDs

- ValidationDashboard.uss with:
  - Complete color palette matching existing theme
  - Panel, section, gauge styling
  - Tab bar styling
  - Utility classes

**Validation:**
- [ ] Files compile without errors
- [ ] Dashboard launches in Validator scene
- [ ] Arc gauges render 270° arc correctly
- [ ] Needles animate smoothly to new values
- [ ] Colors change at thresholds
- [ ] LEDs show on/off states
- [ ] Tab switching works

---

### Stage 2: Critical Tab Polish (PLANNED)

**Objective:** Complete the CRITICAL overview tab with all need-to-know parameters.

**Scope:**
- Refine gauge layout and sizing
- Add plant mode banner with color coding
- Add bubble state indicator with animation
- Add compact alarm summary (top 8 alarms)
- Add simulation time and speed display
- Performance profiling and optimization

---

### Stage 3: Animated Visualizations (PLANNED)

**Objective:** Create animated pressurizer vessel and steam generator diagrams.

**Scope:**
- PressurizerVesselElement:
  - Animated water level
  - Steam space visualization
  - Heater glow effects
  - Spray indication
  
- SteamGeneratorElement:
  - Tube bundle representation
  - Water/steam level
  - Heat transfer indication

---

### Stage 4: Strip Charts (PLANNED)

**Objective:** Implement trend strip charts with ring buffer history.

**Scope:**
- StripChartElement with Painter2D line rendering
- 4-hour history ring buffer
- 8 configurable traces
- Zoom/pan controls
- Legend and axis labels

---

### Stage 5: Remaining Tabs (PLANNED)

**Objective:** Implement RCS, CVCS, SG/RHR, Condenser, and Log tabs.

---

### Stage 6: Polish & Integration (PLANNED)

**Objective:** Final polish, performance optimization, and scene integration.

---

## 4. Dependencies

- Unity 6.3 LTS with UI Toolkit
- HeatupSimEngine for data source
- Existing POC validation (Documentation/UI_Documentation/)

## 5. Related Issues

- **CS-0118:** Condenser/feedwater telemetry gaps → Will be addressed in Stage 5
- **CS-0121:** Dashboard visual bugs → Superseded by this overhaul

## 6. References

- Documentation/UI_Documentation/UIToolkit_Feasibility_Analysis.md
- Documentation/UI_Documentation/UIToolkit_Painter2D_Reference.md
- Documentation/UI_Documentation/UIToolkit_CustomControls.md
- Assets/Scripts/UI/ValidationDashboard/ (existing uGUI reference)

---

*Implementation Plan authored by Claude | CS-0127 Stage 1*
