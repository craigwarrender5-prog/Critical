# Screen 2: RCS Primary Loop - Implementation Package

## Overview

This folder contains everything needed to implement the RCS Primary Loop operator screen (Key 2) for the Critical nuclear reactor simulator.

**Date Created:** 2026-02-09  
**Status:** Ready for Implementation

---

## Contents

| File | Description | Size |
|------|-------------|------|
| `README.md` | This file | - |
| `RCS_Technical_Specifications.md` | Technical specs from NRC HRTD | ~12 KB |
| `RCS_Primary_Loop_Blender.py` | Blender 5.0 script to generate 3D model | ~25 KB |
| `Blender_Unity_Export_Manual.md` | How to export from Blender and import to Unity | ~15 KB |
| `RCSPrimaryLoopScreen.cs` | Unity C# script for the screen | ~30 KB |
| `Screen2_Assembly_Instructions.md` | Step-by-step assembly guide | ~18 KB |

---

## Implementation Order

### Phase 1: 3D Model Creation (Blender)
1. Read `RCS_Technical_Specifications.md` for reference
2. Open Blender 5.0
3. Run `RCS_Primary_Loop_Blender.py`
4. Export as FBX following `Blender_Unity_Export_Manual.md`

### Phase 2: Unity Setup
1. Import FBX to Unity
2. Extract and configure materials (URP)
3. Create render texture and visualization camera
4. Follow `Screen2_Assembly_Instructions.md` Part A-D

### Phase 3: UI Construction
1. Create Canvas and panel structure
2. Add gauges (reuse MosaicGauge component)
3. Create RCP control panels
4. Follow `Screen2_Assembly_Instructions.md` Part E-F

### Phase 4: Script Integration
1. Copy `RCSPrimaryLoopScreen.cs` to project
2. Attach to Canvas
3. Wire up all references
4. Follow `Screen2_Assembly_Instructions.md` Part G

### Phase 5: Testing
1. Verify screen toggle (Key 2)
2. Test gauge updates
3. Test RCP controls
4. Validate against specs
5. Follow `Screen2_Assembly_Instructions.md` Part H

---

## Technical Specifications Summary

### Westinghouse 4-Loop PWR RCS

| Component | Specification |
|-----------|---------------|
| Hot Leg ID | 29.0 inches |
| Cold Leg ID | 27.5 inches |
| Crossover Leg ID | 31.0 inches |
| Flow per RCP | 88,500 gpm |
| RCP Speed | 1,200 rpm |
| Operating Pressure | 2,235 psig |
| T-hot (full power) | 618-620°F |
| T-cold (full power) | 555-558°F |

### Screen Layout

```
┌────────────────────────────────────────────────────────────┐
│  Title Bar (3%)                                            │
├────────┬────────────────────────────────┬──────────────────┤
│        │                                │                  │
│  Left  │       Center Panel             │   Right Panel    │
│ Panel  │    (3D Visualization)          │   (Flow Gauges)  │
│ (Temp  │                                │                  │
│ Gauges)│       50% width                │    35% width     │
│        │                                │                  │
│  15%   │                                │                  │
│ width  │                                │                  │
│        │                                │                  │
├────────┴────────────────────────────────┴──────────────────┤
│  Bottom Panel (26% height)                                 │
│  RCP Controls | Status | Alarms                            │
└────────────────────────────────────────────────────────────┘
```

---

## Dependencies

### Required Components (must exist in project)
- `MosaicGauge.cs` - Gauge display component
- `OperatorScreen.cs` - Base screen class
- `ScreenManager.cs` - Screen toggle management
- `HeatupSimEngine.cs` - Simulation data source
- `RCPSequencer.cs` - RCP state machine
- `PlantConstants.cs` - System constants

### Unity Packages
- Universal Render Pipeline (URP)
- TextMeshPro

### Software
- Blender 5.0+ (for model creation)
- Unity 2022.3 LTS

---

## Key Features

### 3D Visualization
- Blender-generated 4-loop RCS model
- Color-coded temperature gradient on piping
- Animated RCP rotors
- Pulsing flow arrows
- Rendered to texture for UI display

### Gauges (16 total)
**Left Panel (8):**
- Loop 1-4 T-hot (°F)
- Loop 1-4 T-cold (°F)

**Right Panel (8):**
- Total RCS Flow (gpm)
- Loop 1-4 Flow (gpm)
- Core Thermal Power (MWt)
- Core ΔT (°F)
- Average T-avg (°F)

### RCP Controls
- Start/Stop buttons per pump
- Status indicators (Running/Stopped/Ramping/Tripped)
- Speed display (rpm)
- Flow fraction bar
- Interlock checking

### Status Displays
- RCP count (X/4)
- Natural circulation indicator
- Operating mode

### Alarm Panel
- Scrolling alarm list
- Timestamped entries
- Auto-trim old alarms

---

## Validation Checklist

Before marking complete, verify:

- [ ] Screen toggles with Key 2
- [ ] 3D model renders correctly
- [ ] All 16 gauges update from simulation
- [ ] RCP status indicators match sim state
- [ ] RCP Start blocked when interlocks not met
- [ ] RCP Stop functional
- [ ] Alarms display correctly
- [ ] No console errors
- [ ] Performance > 60 FPS
- [ ] Consistent with Screen 1 (Reactor Core) style

---

## Notes

1. **Lumped Loop Model**: The current simulation uses a lumped single-loop model. The visual shows 4 loops with symmetric parameters. Future enhancement may add per-loop resolution.

2. **RCP Control**: Currently, RCPs start automatically based on simulation conditions. The manual controls are prepared for future manual operation mode.

3. **Natural Circulation**: Indicator shows when RCPs are stopped but temperature is high enough for natural circulation.

4. **Layer Setup**: The 3D visualization uses a separate layer (`RCSVisualization`) to avoid interference with the main scene camera.

---

## Support

For issues or questions:
1. Check `Screen2_Assembly_Instructions.md` troubleshooting section
2. Review `RCS_Technical_Specifications.md` for physics questions
3. Consult Unity/Blender documentation for tool-specific issues

---

**Ready for Implementation**
