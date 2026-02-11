# CHANGELOG v1.1.0 — HZP Stabilization and Steam Dump Integration

## Version: 1.1.0
## Date: 2026-02-09
## Status: TESTING — Issues Identified

---

## Overview

v1.1.0 introduces comprehensive HZP (Hot Zero Power) stabilization systems including steam dump control, heater PID control, and reactor startup handoff. This release completes the heatup simulation from Cold Shutdown through stable HZP conditions ready for reactor startup.

---

## Stages Implemented

### Stage 1: SG HTC Corrections
**File:** `Assets/Scripts/Physics/SGSecondaryThermal.cs`

- Created SG secondary thermal model with steaming detection
- Calculates SG secondary pressure from temperature
- Provides heat transfer coefficient based on operating regime
- Detects transition to steaming conditions at saturation

### Stage 2: Steam Dump Model
**Files:**
- `Assets/Scripts/Physics/PlantConstants.SteamDump.cs`
- `Assets/Scripts/Physics/SteamDumpController.cs`

- Added steam dump constants (setpoints, valve parameters)
- Implemented SteamDumpController with steam pressure control mode
- Modulating valve position based on pressure error
- Heat removal calculation from steam flow
- Auto-enable logic when approaching HZP

### Stage 3: HZP Stabilization Controller
**File:** `Assets/Scripts/Physics/HZPStabilizationController.cs`

- State machine: INACTIVE → APPROACHING → STABILIZING → STABLE → HANDOFF_READY
- Tracks temperature, pressure, level stabilization
- Calculates stabilization progress percentage
- Checks startup prerequisites
- Manages handoff to reactor operations

### Stage 4: PID Heater Control
**File:** `Assets/Scripts/Physics/CVCSController.cs` (extended)

- Added HeaterPIDState structure
- PI controller for pressure setpoint tracking
- Smooth output with rate limiting
- Replaces bang-bang control at HZP approach
- Coordinates with existing heater mode system

### Stage 5: Inventory Audit Enhancement
**File:** `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs`

- Added InventoryAuditState structure
- Tracks all system volumes and masses
- Cumulative flow tracking (charging, letdown, seal, surge, makeup, CBO)
- Conservation error calculation
- Alarm threshold logic (500 lbm or 0.5%)
- Enhanced interval and final report logging

### Stage 6: Integration and Handoff
**Files:**
- `Assets/Scripts/Validation/HeatupSimEngine.cs`
- `Assets/Scripts/Validation/HeatupSimEngine.HZP.cs` (new)
- `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs`

- Added v1.1.0 HZP state fields to main engine
- Created HZP partial class for stabilization logic
- Integrated UpdateHZPSystems() into main simulation loop
- Integrated UpdateInventoryAudit() into main simulation loop
- Added history buffers for new parameters
- Public methods for reactor startup handoff

### Stage 7: Visual Dashboard Steam Dump Monitoring
**Files:**
- `Assets/Scripts/Validation/HeatupValidationVisual.cs`
- `Assets/Scripts/Validation/HeatupValidationVisual.Gauges.cs`
- `Assets/Scripts/Validation/HeatupValidationVisual.Panels.cs`
- `Assets/Scripts/Validation/HeatupValidationVisual.Graphs.cs`

- Added "HZP" tab to trend graphs
- Added Group 6: HZP STABILIZATION gauge group
- Added HZP STABILIZATION status panel
- Steam dump heat, HZP progress, steam pressure, PID output displays
- Startup prerequisites checklist in panel
- HZP trend graph with dual-axis display

---

## Known Issues (Post-Implementation Testing)

### Issue 1: Heatup Rate Stuck at 25°F/hr
- **Symptom:** Heatup rate remains at ~25°F/hr instead of reaching 50°F/hr
- **Expected:** With 4 RCPs (21 MW) + heaters (1.8 MW), rate should approach Tech Spec limit
- **Investigation Required:** Check RCP heat integration, SG heat transfer model

### Issue 2: Simulation Stops at 24 Hours Without Reaching HZP
- **Symptom:** Simulation terminates at 24-hour limit before T_avg reaches 557°F
- **Expected:** HZP should be reached in ~8-10 hours with full RCP heat
- **Root Cause:** Likely related to Issue 1 (low heatup rate)
- **Investigation Required:** Check simulation termination conditions

### Issue 3: Immediate Inventory Conservation Alert
- **Symptom:** Conservation error alarm triggers immediately at simulation start
- **Expected:** Conservation error should be near zero initially
- **Investigation Required:** Check InitializeInventoryAudit() calculations

---

## Files Created

| File | Purpose |
|------|---------|
| PlantConstants.SteamDump.cs | Steam dump system constants |
| SteamDumpController.cs | Steam dump valve control |
| SGSecondaryThermal.cs | SG secondary side thermal model |
| HZPStabilizationController.cs | HZP state machine |
| HeatupSimEngine.HZP.cs | Engine HZP integration partial |

## Files Modified

| File | Changes |
|------|---------|
| PlantConstants.Pressure.cs | Added HZP-related constants |
| CVCSController.cs | Added HeaterPID methods |
| HeatupSimEngine.cs | Added v1.1.0 state fields, integration calls |
| HeatupSimEngine.Logging.cs | Added inventory audit, history buffers |
| HeatupValidationVisual.cs | Added HZP tab |
| HeatupValidationVisual.Gauges.cs | Added HZP gauge group |
| HeatupValidationVisual.Panels.cs | Added HZP status panel |
| HeatupValidationVisual.Graphs.cs | Added HZP trend graph |

---

## Technical Notes

### HZP Approach Temperature
- HZP systems activate at T_avg ≥ 550°F
- Full HZP conditions: T_avg = 557°F, P = 2235 psia

### Steam Dump Operation
- Auto-enables when SG is steaming and T_avg approaching setpoint
- Steam pressure control mode maintains header pressure
- Heat removal rate depends on valve position and steam conditions

### Heater PID Control
- Activates within 100 psi of operating pressure
- PI gains tuned for stable pressure control
- Replaces startup full-power and bang-bang modes

### Inventory Audit
- Tracks RCS, PZR, VCT, BRS masses
- Conservation law: Total_Mass(t) = Initial + Inflows - Outflows - Losses
- Alarm threshold: 500 lbm absolute OR 0.5% relative

---

## Next Steps

1. Diagnose and fix Issue 1 (heatup rate)
2. Diagnose and fix Issue 3 (inventory calculation)
3. Issue 2 should resolve once Issue 1 is fixed
4. Re-run validation simulation
5. Verify HZP stabilization and handoff functionality
