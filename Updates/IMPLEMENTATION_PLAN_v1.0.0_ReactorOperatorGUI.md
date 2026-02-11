# Implementation Plan v1.0.0 — Reactor Operator GUI

**Date:** 2026-02-07  
**Type:** Feature (Major)  
**Version:** 1.0.0.0

---

## Summary

Implementation of the Reactor Operator GUI as specified in the design documents:
- ReactorOperatorGUI_Design_v1_0_0_0.md
- Unity_Implementation_Manual_v1_0_0_0.md

This creates a new Unity scene with a realistic control room interface featuring:
- 193-assembly interactive core mosaic map
- 17 parameter gauges (9 nuclear + 8 thermal-hydraulic)
- Rod control panel with 8-bank position display
- Alarm annunciator strip
- Time compression controls

---

## Implementation Stages

### Stage 1: CoreMapData.cs (Static Data)
Pure C# static class containing:
- 15x15 grid occupancy array (CORE_GRID)
- 193 assembly position mappings
- 53 RCCA bank assignments (SA, SB, SC, SD, D, C, B, A)
- Coordinate lookup utilities
- Validation method

### Stage 2: CoreMosaicMap.cs (MonoBehaviour)
Interactive core map renderer:
- Creates 193 Image GameObjects in cross-pattern
- Display modes: Relative Power, Fuel Temp, Coolant Temp, Rod Bank
- Color gradient mapping (blue-cold to red-hot)
- RCCA overlay with bank colors and insertion indicators
- Hover tooltip system
- Click selection for detail panel
- Bank filter buttons
- 2 Hz update throttle for performance

### Stage 3: AssemblyDetailPanel.cs (MonoBehaviour)
Floating detail panel:
- Appears on assembly selection
- Shows assembly index, grid position, type
- RCCA data (if present): bank, position, worth
- Fuel temperature profile
- Relative power fraction
- Close button / click-away dismiss

### Stage 4: ReactorOperatorScreen.cs (MonoBehaviour)
Master screen controller:
- Key '1' toggle visibility
- Layout zone management
- Component orchestration
- Gauge data binding
- Alarm wiring
- Update loop coordination

### Stage 5: OperatorScreenBuilder.cs (Editor Tool)
Menu item: Critical > Create Operator Screen
- Creates Canvas with 1920x1080 reference
- Creates 5-zone layout hierarchy
- Instantiates 17 gauges with correct types
- Creates core map with 193 cells
- Creates bottom control panel
- Creates alarm strip
- Wires all component references

### Stage 6: Integration & Testing
- Connect to ReactorController
- Verify data flow at HZP/50%/100% power
- Test alarm triggers
- Validate interactions

---

## File Locations

| File | Path |
|------|------|
| CoreMapData.cs | Assets/Scripts/UI/CoreMapData.cs |
| CoreMosaicMap.cs | Assets/Scripts/UI/CoreMosaicMap.cs |
| AssemblyDetailPanel.cs | Assets/Scripts/UI/AssemblyDetailPanel.cs |
| ReactorOperatorScreen.cs | Assets/Scripts/UI/ReactorOperatorScreen.cs |
| OperatorScreenBuilder.cs | Assets/Scripts/UI/OperatorScreenBuilder.cs |

---

## Technical Specifications

### Color Palette (from Design Doc)
| Element | Hex | RGB |
|---------|-----|-----|
| Background | #1A1A1F | (26, 26, 31) |
| Panel | #1E1E28 | (30, 30, 40) |
| Normal Text | #C8D0D8 | (200, 208, 216) |
| Label Text | #8090A0 | (128, 144, 160) |
| Green Accent | #00FF88 | (0, 255, 136) |
| Amber Accent | #FFB830 | (255, 184, 48) |
| Red Accent | #FF3344 | (255, 51, 68) |
| Cyan Accent | #00CCFF | (0, 204, 255) |
| Core Cold | #0044AA | (0, 68, 170) |
| Core Hot | #FF2200 | (255, 34, 0) |

### Layout Zones (1920x1080)
| Zone | AnchorMin | AnchorMax |
|------|-----------|-----------|
| LeftGaugePanel | (0, 0.26) | (0.15, 1) |
| CoreMapPanel | (0.15, 0.26) | (0.65, 1) |
| RightGaugePanel | (0.65, 0.26) | (0.80, 1) |
| DetailPanel | (0.80, 0.26) | (1, 1) |
| BottomPanel | (0, 0) | (1, 0.26) |

### RCCA Bank Configuration
| Bank | Type | Count | Color |
|------|------|-------|-------|
| SA | Shutdown | 8 | #FF6B6B |
| SB | Shutdown | 8 | #4ECDC4 |
| SC | Shutdown | 4 | #45B7D1 |
| SD | Shutdown | 4 | #96CEB4 |
| D | Control | 9 | #FFEAA7 |
| C | Control | 9 | #DDA0DD |
| B | Control | 8 | #98D8C8 |
| A | Control | 4 | #F7DC6F |

### Gauges
**Left Panel (9 Nuclear):**
1. Neutron Power (%)
2. Thermal Power (MWt)
3. Startup Rate (DPM)
4. Reactor Period (sec)
5. Total Reactivity (pcm)
6. k-eff (-)
7. Boron (ppm)
8. Xenon (pcm)
9. RCS Flow (%)

**Right Panel (8 Thermal-Hydraulic):**
1. T-avg (°F)
2. T-hot (°F)
3. T-cold (°F)
4. Delta-T (°F)
5. Fuel Centerline (°F)
6. Hot Channel Factor (-)
7. RCS Pressure (psia)
8. PZR Level (%)

---

## Constraints

1. **GOLD STANDARD modules must NOT be modified:**
   - ReactorCore, ControlRodBank, FuelAssembly
   - PlantConstants, all physics modules
   - Existing MosaicGauge, MosaicBoard, etc.

2. **Performance:**
   - Gauge updates: 10 Hz max
   - Core map color updates: 2 Hz max
   - Pre-instantiate all 193 cells (no dynamic creation)

3. **Architecture:**
   - GUI reads from ReactorController only
   - No physics in GUI layer
   - Commands flow through ReactorController methods

---

## Acceptance Criteria

- [ ] 193 assembly cells render in correct Westinghouse cross-pattern
- [ ] 53 RCCA locations correctly bank-assigned
- [ ] Core map color-coding updates from ReactorCore physics
- [ ] All 17 gauges display correct data with proper units
- [ ] Rod controls function correctly
- [ ] Bank position bars track all 8 banks
- [ ] Alarm tiles trigger and flash correctly
- [ ] Hover shows tooltip, click selects assembly
- [ ] Key '1' toggles screen visibility
- [ ] Time compression controls work

---

## Estimated Effort

| Stage | Complexity | Lines of Code |
|-------|------------|---------------|
| 1 - CoreMapData | Medium | ~300 |
| 2 - CoreMosaicMap | High | ~500 |
| 3 - AssemblyDetailPanel | Medium | ~200 |
| 4 - ReactorOperatorScreen | Medium | ~250 |
| 5 - OperatorScreenBuilder | High | ~600 |
| 6 - Integration | Low | ~50 |
| **Total** | | **~1900** |

---

**Ready to proceed with Stage 1?**
