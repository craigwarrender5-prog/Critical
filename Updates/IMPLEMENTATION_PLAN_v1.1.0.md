# Implementation Plan v1.1.0 — Heatup Simulation Physics Refinements

**Date:** 2026-02-08 (Revised)  
**Type:** Minor Release  
**Scope:** Physics accuracy improvements, SG heat transfer correction, steam dump model, HZP stabilization, PZR heater control refinements, Visual Dashboard steam dump monitoring

---

## Executive Summary

Following a comprehensive 20-hour simulated heatup test and subsequent technical analysis, this implementation plan addresses physics accuracy concerns validated against Westinghouse 4-Loop PWR technical specifications. The primary objective is achieving a **stable Hot Zero Power (HZP) condition** that can seamlessly hand off to the Reactor Operations system.

**Key Deliverables:**
1. Corrected SG heat transfer coefficient for realistic 50°F/hr heatup rate
2. Steam dump model for HZP temperature control (per NRC HRTD 19.0)
3. HZP stabilization controller maintaining T_avg = 557°F, P = 2235 psig
4. PZR heater PID control replacing bang-bang oscillation
5. Clean handoff capability to ReactorController for power operations
6. **Visual Dashboard steam dump monitoring (NEW)**

---

## Table of Contents

1. [Design Philosophy](#1-design-philosophy)
2. [Technical Background](#2-technical-background)
3. [Implementation Stages](#3-implementation-stages)
   - [Stage 1: SG Heat Transfer Coefficient Correction](#stage-1-sg-heat-transfer-coefficient-correction-high)
   - [Stage 2: Steam Dump Model](#stage-2-steam-dump-model-high---new)
   - [Stage 3: HZP Stabilization Controller](#stage-3-hzp-stabilization-controller-high---new)
   - [Stage 4: PZR Heater PID Control](#stage-4-pzr-heater-pid-control-critical)
   - [Stage 5: Inventory Audit Enhancement](#stage-5-inventory-audit-enhancement-medium)
   - [Stage 6: Integration and Handoff](#stage-6-integration-and-handoff-medium)
   - [Stage 7: Visual Dashboard Steam Dump Monitoring](#stage-7-visual-dashboard-steam-dump-monitoring-medium---new)
4. [HZP Stabilization Architecture](#4-hzp-stabilization-architecture)
5. [Acceptance Criteria](#5-acceptance-criteria)
6. [References](#6-references)

---

## 1. Design Philosophy

### 1.1 Realism is the Overriding Concern

All implementations must follow actual Westinghouse 4-Loop PWR operating procedures:

| Phase | RCP Configuration | Heat Removal Method | Source |
|-------|-------------------|---------------------|--------|
| Cold Shutdown (Mode 5) | 0 RCPs | RHR system | HRTD 19.2 |
| Heatup (Mode 5→4→3) | 4 RCPs (sequential start) | SG secondary absorption | HRTD 19.2.2 |
| Hot Zero Power (Mode 3) | **4 RCPs running** | **Steam dump to condenser** | HRTD 19.2.2 |
| Power Operations (Mode 1) | 4 RCPs | Turbine load | HRTD 19.4 |

**Critical Finding from NRC HRTD 19.0:**
> *"The primary plant heatup is terminated by automatic actuation of the steam dumps (in steam pressure control) when the pressure inside the steam header reaches 1092 psig. The RCS temperature remains constant at 557°F, the steam dumps removing any excess energy that would tend to drive the RCS temperature higher."*

### 1.2 Operational Flow

```
Cold Shutdown → Heatup (4 RCPs, ~50°F/hr) → HZP Stabilization → Reactor Operations
     ↓                    ↓                        ↓                    ↓
  Mode 5              Mode 5→4→3               Mode 3              Mode 3→2→1
  RHR cooling         SG secondary sink       Steam dump          Power ascension
                                              maintains 557°F      
```

### 1.3 Handoff to Reactor Operations

Upon achieving stable HZP:
- HeatupSimEngine enters `HZP_STABILIZED` state
- All 4 RCPs remain running at full flow
- Steam dump maintains T_avg at 557°F ± 2°F
- Pressure maintained at 2235 psig ± 10 psi
- PZR level at ~60% per no-load program
- **Operator action from Reactor Operator GUI triggers transition to ReactorController**

---

## 2. Technical Background

### 2.1 Heat Balance at HZP

At Hot Zero Power with all 4 RCPs running:

**Heat Sources:**
| Source | Power | Notes |
|--------|-------|-------|
| 4× RCP motors | 21.0 MW | 5.25 MW each at rated flow |
| PZR proportional heaters | 0.5 MW | Continuous for pressure control |
| **Total Input** | **21.5 MW** | |

**Heat Sinks (without steam dump):**
| Sink | Capacity | Notes |
|------|----------|-------|
| Insulation losses | ~1.5 MW | Per PlantConstants |
| SG secondary (subcooled) | ~7-10 MW | Natural convection, no boiling |
| **Total Available** | **~9-12 MW** | **Insufficient!** |

**Heat Balance Gap:** ~10-12 MW excess heat with no steam dump

**Solution:** Steam dump removes excess heat by:
1. SG secondary water reaches saturation (~545°F at 1000 psia)
2. Steam generated in SG shell side
3. Steam dump valves open to reject steam to condenser
4. Feedwater (or condensate return) maintains SG level

### 2.2 Steam Dump Control Mode at HZP

Per NRC HRTD 19.0, steam dumps operate in **Steam Pressure Control Mode**:

- Setpoint: 1092 psig (saturation pressure for 557°F)
- Steam header pressure > 1092 psig → dump valves open
- Steam header pressure < 1092 psig → dump valves close
- This maintains T_avg at no-load value (557°F)

**Control Logic:**
```
P_steam_error = P_steam_actual - P_steam_setpoint (1092 psig)

If P_steam_error > 0:
    Dump_demand = K_p × P_steam_error  (proportional control)
    Q_dump = Dump_demand × Q_dump_max
```

### 2.3 SG Heat Transfer Coefficient Correction

**Current Problem:** SG HTC of 200 BTU/(hr·ft²·°F) is ~2× too high for natural convection conditions during heatup.

**Engineering Analysis:**
```
Overall: 1/U = 1/h_primary + t_wall/k_wall + 1/h_secondary

Primary (Dittus-Boelter):  h_p ≈ 3000 BTU/(hr·ft²·°F)
Secondary (Churchill-Chu): h_s ≈ 100 BTU/(hr·ft²·°F)  ← LIMITING

Result: U ≈ 100 BTU/(hr·ft²·°F)
```

**Impact:**
- Current: SG absorbs ~14 MW → heatup rate ~26°F/hr
- Corrected: SG absorbs ~7 MW → heatup rate ~47°F/hr ✓

---

## 3. Implementation Stages

### Stage 1: SG Heat Transfer Coefficient Correction (HIGH)

**Scope:** Correct over-estimated HTC for realistic heatup rate

**File:** `PlantConstants.Heatup.cs`

**Change:**
```csharp
/// <summary>
/// SG overall HTC with RCPs running (forced primary, natural secondary)
/// in BTU/(hr·ft²·°F).
///
/// ENGINEERING BASIS (v1.1.0):
/// Primary side (tube interior): Dittus-Boelter forced convection
///   h_primary ≈ 3000 BTU/(hr·ft²·°F) at Re~150,000
///
/// Secondary side (tube exterior): Churchill-Chu natural convection  
///   h_secondary ≈ 100 BTU/(hr·ft²·°F) in stagnant subcooled pool
///
/// Overall: 1/U = 1/3000 + 1/100 ≈ 1/100 (secondary-limited)
///
/// At HZP with steaming: Secondary HTC increases due to boiling
///   h_secondary_boiling ≈ 500-1000 BTU/(hr·ft²·°F)
///   Overall U_boiling ≈ 400-500 BTU/(hr·ft²·°F)
///
/// Source: Incropera & DeWitt, NUREG/CR-5426, NRC HRTD calibration
/// </summary>
public const float SG_HTC_NATURAL_CONVECTION = 100f;    // Subcooled heatup
public const float SG_HTC_BOILING = 500f;               // HZP with steaming
```

**Validation:**
- Heatup rate with 4 RCPs: 40-55°F/hr (target 50°F/hr per HRTD)
- SG secondary temperature lag: 10-20°F behind RCS during heatup

**Effort:** 1-2 hours

---

### Stage 2: Steam Dump Model (HIGH - NEW)

**Scope:** Implement steam dump system for HZP temperature control

**New File:** `SteamDumpController.cs`

**Architecture:**
```
SteamDumpController
├── Mode: OFF | STEAM_PRESSURE | TAVG (future)
├── Setpoint: 1092 psig (steam pressure mode)
├── Proportional gain: K_p (tunable)
├── Max dump capacity: ~25 MW (4 SGs × ~6 MW each)
└── Output: Q_dump (MW removed to condenser)
```

**Key Parameters:**
```csharp
// PlantConstants.SteamDump.cs (new file)

/// Steam pressure setpoint for no-load (saturation at 557°F)
public const float STEAM_PRESSURE_SETPOINT_PSIG = 1092f;

/// Proportional gain for steam dump demand
public const float STEAM_DUMP_KP = 0.05f;  // fraction per psi error

/// Maximum steam dump capacity (MW thermal)
/// 4 SGs, each can dump ~6-7 MW to condenser at no-load
public const float STEAM_DUMP_MAX_MW = 25f;

/// Minimum steam pressure for dump operation
public const float STEAM_DUMP_MIN_PRESSURE_PSIG = 900f;

/// Steam dump valve stroke time (seconds)
public const float STEAM_DUMP_STROKE_TIME = 10f;
```

**Physics Model:**
```csharp
public class SteamDumpController
{
    public enum DumpMode { OFF, STEAM_PRESSURE, TAVG }
    
    private DumpMode _mode = DumpMode.OFF;
    private float _dumpDemand = 0f;        // 0-1 fraction
    private float _valvePosition = 0f;     // 0-1 with dynamics
    
    /// <summary>
    /// Calculate steam dump heat removal based on SG secondary conditions.
    /// </summary>
    public float CalculateDumpHeat(float steamPressure_psig, float dt_hr)
    {
        if (_mode == DumpMode.OFF) return 0f;
        
        // Steam pressure error
        float error = steamPressure_psig - STEAM_PRESSURE_SETPOINT_PSIG;
        
        // Proportional demand (only dump if pressure above setpoint)
        _dumpDemand = Mathf.Clamp01(error * STEAM_DUMP_KP);
        
        // Valve dynamics (first-order lag)
        float tau = STEAM_DUMP_STROKE_TIME / 3600f;  // hours
        _valvePosition += (_dumpDemand - _valvePosition) * dt_hr / tau;
        _valvePosition = Mathf.Clamp01(_valvePosition);
        
        // Heat removal (proportional to valve position)
        return _valvePosition * STEAM_DUMP_MAX_MW;
    }
    
    /// <summary>
    /// Activate steam dump in steam pressure control mode.
    /// Called when approaching HZP conditions.
    /// </summary>
    public void EnableSteamPressureMode()
    {
        _mode = DumpMode.STEAM_PRESSURE;
    }
}
```

**Integration with SGSecondaryThermal:**
```csharp
// In SGSecondaryThermal.cs, add steam dump heat removal

public float Update(float T_rcs, float Q_transfer_MW, float dt_hr, 
                    SteamDumpController steamDump)
{
    // Existing subcooled heat absorption...
    
    // At HZP, calculate steam generation
    float T_sat = SaturationTemperature(_secondaryPressure_psia);
    
    if (_secondaryTemp_F >= T_sat - 5f)  // Near saturation
    {
        // Transition to steaming mode
        _isSteaming = true;
        
        // Steam dump removes heat
        float Q_dump = steamDump?.CalculateDumpHeat(
            _secondaryPressure_psia * 0.0689476f - 14.7f,  // psia to psig
            dt_hr) ?? 0f;
        
        // Net heat to secondary = primary transfer - dump removal
        float Q_net = Q_transfer_MW - Q_dump;
        
        // Temperature change based on net heat
        // (simplified: at saturation, excess heat generates steam)
        if (Q_net > 0)
        {
            // Still heating - pressure rises
            _secondaryPressure_psia += Q_net * PRESSURE_RISE_RATE;
        }
    }
    
    return _secondaryTemp_F;
}
```

**Activation Logic in HeatupSimEngine:**
```csharp
// When T_avg approaches HZP target (555°F), enable steam dump
if (T_rcs > 550f && !_steamDumpEnabled)
{
    _steamDump.EnableSteamPressureMode();
    _steamDumpEnabled = true;
    Log("Steam dump enabled - steam pressure control mode");
}
```

**Effort:** 4-6 hours

---

### Stage 3: HZP Stabilization Controller (HIGH - NEW)

**Scope:** Automatic stabilization at Hot Zero Power conditions

**New File:** `HZPStabilizationController.cs`

**Target Conditions (Mode 3 - Hot Standby):**
| Parameter | Setpoint | Tolerance | Control Method |
|-----------|----------|-----------|----------------|
| T_avg | 557°F | ±2°F | Steam dump modulation |
| Pressure | 2235 psig | ±10 psi | PZR heater/spray PID |
| PZR Level | 60% | ±5% | CVCS charging adjustment |
| Steam Pressure | 1092 psig | ±20 psi | Result of T_avg control |

**State Machine:**
```
HEATUP → APPROACHING_HZP → STABILIZING → HZP_STABLE → HANDOFF_READY
                ↓               ↓              ↓              ↓
           T>550°F        T=555-559°F    Params stable   Await operator
           Enable dump    Fine control    for 5 min       action
```

**Architecture:**
```csharp
public class HZPStabilizationController
{
    public enum HZPState
    {
        INACTIVE,           // During heatup
        APPROACHING,        // T_avg > 550°F, steam dump enabled
        STABILIZING,        // Near setpoints, fine-tuning
        STABLE,             // All parameters within tolerance
        HANDOFF_READY       // Ready for ReactorController
    }
    
    private HZPState _state = HZPState.INACTIVE;
    private float _stableTimer = 0f;
    private const float STABLE_TIME_REQUIRED = 300f;  // 5 minutes
    
    // Setpoints
    private const float TAVG_SETPOINT = 557f;
    private const float TAVG_TOLERANCE = 2f;
    private const float PRESSURE_SETPOINT = 2235f;
    private const float PRESSURE_TOLERANCE = 10f;
    private const float LEVEL_SETPOINT = 60f;
    private const float LEVEL_TOLERANCE = 5f;
    
    public HZPState State => _state;
    public bool IsStable => _state == HZPState.STABLE || 
                            _state == HZPState.HANDOFF_READY;
    
    /// <summary>
    /// Update HZP stabilization state machine.
    /// </summary>
    public void Update(float T_avg, float pressure_psig, float pzrLevel_pct,
                       SteamDumpController steamDump, 
                       CVCSController cvcs,
                       float dt_sec)
    {
        switch (_state)
        {
            case HZPState.INACTIVE:
                if (T_avg > 550f)
                {
                    _state = HZPState.APPROACHING;
                    steamDump.EnableSteamPressureMode();
                }
                break;
                
            case HZPState.APPROACHING:
                if (T_avg > 554f && T_avg < 560f)
                {
                    _state = HZPState.STABILIZING;
                }
                break;
                
            case HZPState.STABILIZING:
                // Check all parameters within tolerance
                bool tempOK = Mathf.Abs(T_avg - TAVG_SETPOINT) < TAVG_TOLERANCE;
                bool pressOK = Mathf.Abs(pressure_psig - PRESSURE_SETPOINT) < PRESSURE_TOLERANCE;
                bool levelOK = Mathf.Abs(pzrLevel_pct - LEVEL_SETPOINT) < LEVEL_TOLERANCE;
                
                if (tempOK && pressOK && levelOK)
                {
                    _stableTimer += dt_sec;
                    if (_stableTimer >= STABLE_TIME_REQUIRED)
                    {
                        _state = HZPState.STABLE;
                    }
                }
                else
                {
                    _stableTimer = 0f;  // Reset if any parameter out of band
                }
                break;
                
            case HZPState.STABLE:
                // Continue monitoring - can transition to HANDOFF_READY
                // when operator initiates startup from Reactor Operator GUI
                break;
                
            case HZPState.HANDOFF_READY:
                // Awaiting transition to ReactorController
                break;
        }
    }
    
    /// <summary>
    /// Initiate handoff to Reactor Operations.
    /// Called when operator begins reactor startup.
    /// </summary>
    public void InitiateHandoff()
    {
        if (_state == HZPState.STABLE)
        {
            _state = HZPState.HANDOFF_READY;
        }
    }
}
```

**Effort:** 3-4 hours

---

### Stage 4: PZR Heater PID Control (CRITICAL)

**Scope:** Replace bang-bang heater control with smooth PID controller

**File:** `CVCSController.cs`

**New Constants in PlantConstants.Pressure.cs:**
```csharp
// PZR Heater PID Control (per NRC HRTD 10.2)
public const float HEATER_PID_KP = 0.05f;           // Proportional gain
public const float HEATER_PID_KI = 0.001f;          // Integral gain  
public const float HEATER_PID_KD = 0.01f;           // Derivative gain
public const float HEATER_DEADBAND_PRESSURE = 5f;   // psi
public const float HEATER_RATE_LIMIT = 0.167f;      // fraction/min (10%/min)
public const float HEATER_LAG_TAU = 0.00833f;       // hours (30 sec)

// Heater staging setpoints (from setpoint of 2235 psig)
public const float PROPORTIONAL_HEATER_CUTOFF = 2250f;  // +15 psi: prop off
public const float BACKUP_HEATER_ON = 2210f;            // -25 psi: backup on
public const float SPRAY_START = 2260f;                 // +25 psi: spray starts
public const float SPRAY_FULL = 2310f;                  // +75 psi: spray full
```

**Implementation:**
```csharp
public struct HeaterPIDState
{
    public float integral;
    public float previousError;
    public float outputCommand;
    public float smoothedOutput;
}

private HeaterPIDState _heaterPID;

public float CalculateHeaterPower(float pressure_psig, float dt_hr)
{
    float error = PlantConstants.Pressure.PZR_OPERATING_PRESSURE - pressure_psig;
    
    // Deadband
    if (Mathf.Abs(error) < HEATER_DEADBAND_PRESSURE)
    {
        // Within deadband - hold current output
        return _heaterPID.smoothedOutput * PlantConstants.Pressure.PZR_HEATER_CAPACITY_MW;
    }
    
    // PID calculation
    _heaterPID.integral += error * dt_hr;
    _heaterPID.integral = Mathf.Clamp(_heaterPID.integral, -100f, 100f);
    
    float derivative = (error - _heaterPID.previousError) / dt_hr;
    _heaterPID.previousError = error;
    
    float pidOutput = HEATER_PID_KP * error + 
                      HEATER_PID_KI * _heaterPID.integral + 
                      HEATER_PID_KD * derivative;
    
    _heaterPID.outputCommand = Mathf.Clamp01(pidOutput);
    
    // Rate limiting (10%/min = 0.00167/sec)
    float maxChange = HEATER_RATE_LIMIT * dt_hr * 60f;
    float delta = _heaterPID.outputCommand - _heaterPID.smoothedOutput;
    delta = Mathf.Clamp(delta, -maxChange, maxChange);
    _heaterPID.smoothedOutput += delta;
    
    // First-order lag
    float tau = HEATER_LAG_TAU;
    _heaterPID.smoothedOutput += (_heaterPID.outputCommand - _heaterPID.smoothedOutput) * dt_hr / tau;
    
    return _heaterPID.smoothedOutput * PlantConstants.Pressure.PZR_HEATER_CAPACITY_MW;
}
```

**Validation:**
- Heater power oscillation ≤ 10% amplitude during steady-state
- Minimum 30 sec between direction changes

**Effort:** 2-3 hours

---

### Stage 5: Inventory Audit Enhancement (MEDIUM)

**Scope:** Comprehensive mass balance tracking

**File:** `HeatupSimEngine.Logging.cs`

**New Log Section:**
```
=== SYSTEM INVENTORY AUDIT ===
RCS Water:      712,209 lb → 85,500 gal
PZR Water:       48,000 lb →  5,760 gal  
PZR Steam:        1,200 lb →  1,385 gal (displaced)
VCT:                       →  2,800 gal
BRS Holdup:                →  8,433 gal
---------------------------------
Total System:              → 102,478 gal
Initial:                   → 102,500 gal
Conservation Error:        →     -22 gal ✓
```

**Effort:** 2 hours

---

### Stage 6: Integration and Handoff (MEDIUM)

**Scope:** Connect HZP stabilization to Reactor Operator GUI

**Changes:**

1. **HeatupSimEngine modifications:**
   - Add `HZPStabilizationController` instance
   - Add `SteamDumpController` instance
   - Modify main loop to use stabilization state machine
   - Add event for HZP_STABLE notification

2. **ReactorOperatorScreen integration:**
   - Display HZP stabilization status
   - Add "BEGIN REACTOR STARTUP" button (enabled when HZP_STABLE)
   - Button triggers `HZPStabilizationController.InitiateHandoff()`

3. **Handoff sequence:**
   ```csharp
   // When operator clicks "BEGIN REACTOR STARTUP":
   public void OnBeginReactorStartup()
   {
       if (_hzpController.State == HZPState.STABLE)
       {
           _hzpController.InitiateHandoff();
           
           // Initialize ReactorController to current HZP conditions
           _reactorController.InitializeToHZP();
           _reactorController.SetBoron(_currentBoron_ppm);
           
           // Transfer control
           _heatupSimEngine.Stop();
           _reactorController.enabled = true;
           
           Log("Control transferred to Reactor Operations");
       }
   }
   ```

**Effort:** 2-3 hours

---

### Stage 7: Visual Dashboard Steam Dump Monitoring (MEDIUM - NEW)

**Scope:** Add Steam Dump system monitoring to HeatupValidationVisual dashboard once HZP is obtained

**File Modifications:**

1. **HeatupValidationVisual.Gauges.cs** — Add new gauge group for Steam Dump
2. **HeatupValidationVisual.Panels.cs** — Add Steam Dump status panel
3. **HeatupValidationVisual.Graphs.cs** — Add Steam Dump trend to RATES tab or new tab
4. **HeatupSimEngine.cs** — Expose steam dump state for dashboard binding

**New Gauge Group: STEAM DUMP (appears after HZP approach begins)**

```
┌─────────────────────────────────────────┐
│         STEAM DUMP CONTROL              │
├─────────────────────────────────────────┤
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  │
│  │ STEAM   │  │  DUMP   │  │  HEAT   │  │
│  │ PRESS   │  │ DEMAND  │  │ REMOVAL │  │
│  │1092 psig│  │   85%   │  │ 18.5 MW │  │
│  └─────────┘  └─────────┘  └─────────┘  │
│                                         │
│  Mode: STEAM PRESSURE    Setpoint: 1092 │
│  Valve Pos: 87%          Status: ACTIVE │
└─────────────────────────────────────────┘
```

**Dashboard Additions:**

#### 7.1 New Fields in HeatupSimEngine (Public Accessors)

```csharp
// Steam Dump state exposure for dashboard
public bool steamDumpEnabled => _steamDump?.IsEnabled ?? false;
public string steamDumpMode => _steamDump?.ModeString ?? "OFF";
public float steamDumpDemand => _steamDump?.DumpDemand ?? 0f;
public float steamDumpValvePosition => _steamDump?.ValvePosition ?? 0f;
public float steamDumpHeatRemoval => _steamDump?.HeatRemoval_MW ?? 0f;
public float steamPressure => _sgSecondary?.SteamPressure_psig ?? 0f;
public float steamPressureSetpoint => PlantConstants.SteamDump.STEAM_PRESSURE_SETPOINT_PSIG;

// HZP state exposure
public string hzpState => _hzpController?.State.ToString() ?? "N/A";
public float hzpStableTimer => _hzpController?.StableTimer ?? 0f;
```

#### 7.2 Gauge Group Implementation (HeatupValidationVisual.Gauges.cs)

```csharp
// New gauge group height constant
const float STEAM_DUMP_GROUP_H = GAUGE_GROUP_HEADER_H + GAUGE_ROW_H + 20f * 2 + GAUGE_GROUP_GAP;

// Update TOTAL_GAUGE_H to include new group conditionally
// Note: Only shown when steamDumpEnabled == true

void DrawSteamDumpGauges(float x, ref float y, float w)
{
    // Only render if steam dump is enabled (approaching or at HZP)
    if (!engine.steamDumpEnabled) return;
    
    DrawSectionHeader(new Rect(x, y, w, GAUGE_GROUP_HEADER_H), "STEAM DUMP CONTROL");
    y += GAUGE_GROUP_HEADER_H;
    
    float arcR = GAUGE_ARC_SIZE / 2f;
    float cellW = w / 3f;
    float rowY = y;
    
    // ROW: Steam Pressure, Dump Demand, Heat Removal
    // Steam Pressure (psig) - setpoint 1092
    Color pressColor = GetThresholdColor(engine.steamPressure, 1070f, 1115f, 1050f, 1130f,
        _cNormalGreen, _cWarningAmber, _cAlarmRed);
    DrawGaugeArc(
        new Vector2(x + cellW * 0.5f, rowY + arcR + 14f), arcR,
        engine.steamPressure, 900f, 1200f, pressColor,
        "STM PRESS", $"{engine.steamPressure:F0}", "psig");
    
    // Dump Demand (%)
    Color demandColor = engine.steamDumpDemand > 0.9f ? _cWarningAmber : _cNormalGreen;
    DrawGaugeArc(
        new Vector2(x + cellW * 1.5f, rowY + arcR + 14f), arcR,
        engine.steamDumpDemand * 100f, 0f, 100f, demandColor,
        "DUMP DMD", $"{engine.steamDumpDemand * 100f:F1}", "%");
    
    // Heat Removal (MW)
    DrawGaugeArc(
        new Vector2(x + cellW * 2.5f, rowY + arcR + 14f), arcR,
        engine.steamDumpHeatRemoval, 0f, 30f, _cTrace5,
        "Q DUMP", $"{engine.steamDumpHeatRemoval:F1}", "MW");
    
    y += GAUGE_ROW_H;
    
    // Mini bars: Valve Position, Mode indicator
    DrawMiniBar(x, ref y, w, "VALVE POS", engine.steamDumpValvePosition * 100f, 0f, 100f, "%", _cTrace1);
    DrawMiniBar(x, ref y, w, "SETPOINT", engine.steamPressureSetpoint, 1000f, 1150f, "psig", _cTextSecondary);
    
    y += GAUGE_GROUP_GAP;
}
```

#### 7.3 Status Panel Addition (HeatupValidationVisual.Panels.cs)

```csharp
void DrawSteamDumpStatusPanel(Rect area, ref float y)
{
    // Only show when steam dump enabled
    if (!engine.steamDumpEnabled) return;
    
    DrawPanelHeader(area.x, ref y, area.width, "STEAM DUMP STATUS");
    
    // Mode and status
    Color modeColor = engine.steamDumpMode == "STEAM_PRESSURE" ? _cNormalGreen : _cTextSecondary;
    DrawStatusRow(area.x, ref y, area.width, "Mode:", engine.steamDumpMode, modeColor);
    
    // Valve status
    string valveStr = engine.steamDumpValvePosition > 0.01f ? 
        $"OPEN {engine.steamDumpValvePosition * 100f:F1}%" : "CLOSED";
    Color valveColor = engine.steamDumpValvePosition > 0.01f ? _cNormalGreen : _cTextSecondary;
    DrawStatusRow(area.x, ref y, area.width, "Valve:", valveStr, valveColor);
    
    // Heat removal
    DrawStatusRow(area.x, ref y, area.width, "Heat Removal:", 
        $"{engine.steamDumpHeatRemoval:F1} MW", _cTextPrimary);
    
    // Steam pressure error
    float pressError = engine.steamPressure - engine.steamPressureSetpoint;
    Color errorColor = Mathf.Abs(pressError) < 10f ? _cNormalGreen : _cWarningAmber;
    DrawStatusRow(area.x, ref y, area.width, "Press Error:", 
        $"{pressError:+0.0;-0.0;0} psi", errorColor);
    
    y += 8f;
}
```

#### 7.4 Trend Graph Addition (HeatupValidationVisual.Graphs.cs)

Add steam dump trends to the RATES tab or create new "STEAM DUMP" tab:

**Option A: Add to existing RATES tab**
```csharp
// In DrawRatesGraphs(), add:
if (engine.steamDumpEnabled)
{
    // Steam dump valve position trend (0-100%)
    DrawGraphTrace(graphRect, engine.steamDumpValveHistory, 
        0f, 100f, _cTrace5, "Dump Valve");
    
    // Heat removal trend (0-30 MW)
    DrawGraphTrace(graphRect, engine.steamDumpHeatHistory,
        0f, 30f, _cTrace6, "Q Dump");
}
```

**Option B: New "HZP" tab (recommended for clarity)**
```csharp
// Add new tab to _graphTabLabels:
private readonly string[] _graphTabLabels = new string[]
{
    "TEMPS",
    "PRESSURE",
    "CVCS",
    "VCT/BRS",
    "RATES",
    "RCP HEAT",
    "HZP"        // NEW: Steam dump and HZP parameters
};

// New drawing method:
void DrawHZPGraphs(Rect area)
{
    if (!engine.steamDumpEnabled)
    {
        // Show "Awaiting HZP approach" message
        GUI.Label(area, "Steam dump not yet active\nT_avg must exceed 550°F", 
            _graphLabelStyle);
        return;
    }
    
    // Split area for multiple graphs
    float h3 = area.height / 3f;
    
    // Graph 1: Steam Pressure vs Setpoint
    Rect g1 = new Rect(area.x, area.y, area.width, h3);
    DrawGraphWithSetpoint(g1, engine.steamPressureHistory, 
        engine.steamPressureSetpoint, 1000f, 1150f,
        _cTrace1, "Steam Pressure (psig)");
    
    // Graph 2: Dump Valve Position & Demand
    Rect g2 = new Rect(area.x, area.y + h3, area.width, h3);
    DrawGraph(g2, engine.steamDumpValveHistory, 0f, 100f,
        _cTrace2, "Valve Position (%)");
    
    // Graph 3: Heat Removal
    Rect g3 = new Rect(area.x, area.y + h3 * 2, area.width, h3);
    DrawGraph(g3, engine.steamDumpHeatHistory, 0f, 30f,
        _cTrace5, "Heat Removal (MW)");
}
```

#### 7.5 History Buffer Requirements (HeatupSimEngine)

Add history buffers for trending:

```csharp
// Steam dump history buffers (same size as existing buffers)
public float[] steamPressureHistory = new float[HISTORY_SIZE];
public float[] steamDumpValveHistory = new float[HISTORY_SIZE];
public float[] steamDumpHeatHistory = new float[HISTORY_SIZE];

// In RecordHistory():
if (_steamDump != null)
{
    steamPressureHistory[_historyIndex] = _sgSecondary?.SteamPressure_psig ?? 0f;
    steamDumpValveHistory[_historyIndex] = _steamDump.ValvePosition * 100f;
    steamDumpHeatHistory[_historyIndex] = _steamDump.HeatRemoval_MW;
}
```

**Validation Criteria:**
- Gauges appear only when T_avg > 550°F (steam dump enabled)
- Steam pressure displays with setpoint reference line
- Dump valve position shows real-time modulation
- Heat removal accurately reflects Q_dump calculation
- Trend graphs show historical behavior during HZP approach

**Effort:** 3-4 hours

---

## 4. HZP Stabilization Architecture

### 4.1 System Interaction Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                     HeatupSimEngine (Main Loop)                      │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────────────┐  │
│  │  RCSHeatup   │───→│SGSecondary   │───→│ SteamDumpController  │  │
│  │  (4 RCPs)    │    │Thermal       │    │ (pressure mode)      │  │
│  │  21 MW       │    │              │    │                      │  │
│  └──────────────┘    └──────────────┘    └──────────────────────┘  │
│         │                   │                      │                │
│         ▼                   ▼                      ▼                │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │              HZPStabilizationController                       │  │
│  │  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐          │  │
│  │  │INACTIVE │→ │APPROACH │→ │STABILIZ │→ │ STABLE  │→ HANDOFF │  │
│  │  └─────────┘  └─────────┘  └─────────┘  └─────────┘          │  │
│  └──────────────────────────────────────────────────────────────┘  │
│         │                                          │                │
│         ▼                                          ▼                │
│  ┌──────────────┐                         ┌──────────────────────┐ │
│  │CVCSController│                         │ReactorOperatorScreen │ │
│  │(heater PID)  │                         │"BEGIN STARTUP" button│ │
│  └──────────────┘                         └──────────────────────┘ │
│                                                    │                │
│         ┌──────────────────────────────────────────┘                │
│         ▼                                                           │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │           HeatupValidationVisual (Dashboard)                  │  │
│  │  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐          │  │
│  │  │ GAUGES  │  │ GRAPHS  │  │ PANELS  │  │STEAM DMP│ (Stage 7)│  │
│  │  └─────────┘  └─────────┘  └─────────┘  └─────────┘          │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                    │                │
└────────────────────────────────────────────────────│────────────────┘
                                                     │
                                                     ▼
                                           ┌──────────────────┐
                                           │ReactorController │
                                           │(Power Operations)│
                                           └──────────────────┘
```

### 4.2 Heat Balance at HZP (Stabilized)

```
HEAT INPUT                          HEAT OUTPUT
─────────────                       ───────────
4× RCP:     21.0 MW                 SG→Steam Dump:  ~19.5 MW
PZR Heat:    0.5 MW                 Insulation:      ~1.5 MW
                                    Other losses:    ~0.5 MW
─────────────                       ───────────
Total:      21.5 MW                 Total:          ~21.5 MW

                    ΔQ ≈ 0 (equilibrium)
                    T_avg = 557°F ± 2°F ✓
```

### 4.3 Control Loop Summary

| Parameter | Controller | Setpoint | Method |
|-----------|------------|----------|--------|
| T_avg | SteamDumpController | 557°F (via 1092 psig steam) | Steam dump modulation |
| Pressure | CVCSController (PID) | 2235 psig | Heater/spray control |
| PZR Level | CVCSController | 60% (no-load program) | Charging flow adjustment |

---

## 5. Acceptance Criteria

### 5.1 Heatup Phase Criteria

| Criterion | Target | Validation |
|-----------|--------|------------|
| Heatup rate (4 RCPs) | 45-55°F/hr | Average rate 300-500°F |
| SG temperature lag | 10-20°F | T_RCS - T_SG_secondary |
| Bubble formation | Complete by ~9 hr | Phase transition to COMPLETE |
| RCP sequencing | 4 pumps by ~12 hr | All pumps at 100% |

### 5.2 HZP Stabilization Criteria

| Criterion | Target | Tolerance |
|-----------|--------|-----------|
| T_avg | 557°F | ±2°F |
| RCS Pressure | 2235 psig | ±10 psi |
| PZR Level | 60% | ±5% |
| Steam Header Pressure | 1092 psig | ±20 psi |
| Steam Dump Position | 80-95% open | Modulating |
| Heater Oscillation | N/A | ≤10% amplitude |
| Stability Duration | 5 minutes | Before STABLE state |

### 5.3 Handoff Criteria

| Criterion | Requirement |
|-----------|-------------|
| HZP State | STABLE or HANDOFF_READY |
| All 4 RCPs | Running at 100% |
| ReactorController | Initialized to HZP conditions |
| Mode Display | "MODE 3 - HOT STANDBY" |

### 5.4 Visual Dashboard Criteria (Stage 7)

| Criterion | Requirement |
|-----------|-------------|
| Steam Dump gauges | Visible when T_avg > 550°F |
| Steam Pressure gauge | Shows actual vs setpoint (1092 psig) |
| Dump Demand gauge | 0-100% indication |
| Heat Removal gauge | 0-30 MW range |
| Trend graphs | Steam pressure, valve position, Q_dump histories |
| Panel status | Mode, valve state, error indication |

---

## 6. Implementation Summary

| Stage | Description | Priority | Effort | Dependencies |
|-------|-------------|----------|--------|--------------|
| 1 | SG HTC Correction | HIGH | 1-2 hr | None |
| 2 | Steam Dump Model | HIGH | 4-6 hr | Stage 1 |
| 3 | HZP Stabilization Controller | HIGH | 3-4 hr | Stage 2 |
| 4 | PZR Heater PID Control | CRITICAL | 2-3 hr | None |
| 5 | Inventory Audit Enhancement | MEDIUM | 2 hr | None |
| 6 | Integration and Handoff | MEDIUM | 2-3 hr | Stages 1-4 |
| **7** | **Visual Dashboard Steam Dump** | **MEDIUM** | **3-4 hr** | **Stages 2, 6** |

**Total Estimated Effort:** 17-24 hours

**Recommended Implementation Order:**
1. Stage 1 (SG HTC) — Foundation for correct heatup rate
2. Stage 4 (Heater PID) — Can be done in parallel
3. Stage 2 (Steam Dump) — Requires Stage 1
4. Stage 3 (HZP Controller) — Requires Stage 2
5. Stage 5 (Inventory Audit) — Can be done in parallel
6. Stage 6 (Integration) — Requires Stages 1-4
7. **Stage 7 (Dashboard Monitoring) — Requires Stages 2, 6**

---

## 7. References

### NRC Technical Documents
- **NRC HRTD 19.0** — Plant Operations (ML11223A342) — **Primary source for HZP operations**
- NRC HRTD 4.1 — CVCS Operations
- NRC HRTD 6.1 — Pressurizer Heaters  
- NRC HRTD 10.2 — Pressurizer Pressure Control
- NRC HRTD 11.2 — Steam Dump Control System

### Key Quotes from HRTD 19.0

> *"With all four RCPs operating and the RHR system secured, the reactor coolant begins to heat up at a rate of approximately 50°F per hour."*

> *"The primary plant heatup is terminated by automatic actuation of the steam dumps (in steam pressure control) when the pressure inside the steam header pressure reaches 1092 psig. The RCS temperature remains constant at 557°F, the steam dumps removing any excess energy that would tend to drive the RCS temperature higher."*

> *"The pressurizer heaters and sprays are placed in automatic control when the pressure reaches the normal operating value of 2235 psig."*

### Heat Transfer References
- Incropera & DeWitt — Fundamentals of Heat and Mass Transfer
- Churchill-Chu correlation — Natural convection on cylinders
- Dittus-Boelter correlation — Forced convection in tubes
- NUREG/CR-5426 — SG Heat Transfer

---

## Document History

| Version | Date | Changes |
|---------|------|---------|
| 1.1.0-draft | 2026-02-08 | Initial plan |
| 1.1.0-rev1 | 2026-02-08 | Added SG HTC analysis, HZP gap analysis |
| 1.1.0-rev2 | 2026-02-08 | Added Steam Dump Model, HZP Stabilization Controller, Handoff to Reactor Operations |
| 1.1.0-rev3 | 2026-02-08 | Converted to Markdown format; Added Stage 7: Visual Dashboard Steam Dump Monitoring |

---

## Approval

**Prepared by:** Claude (AI Assistant)  
**Date:** 2026-02-08  
**Status:** AWAITING APPROVAL

---

**Awaiting user approval to proceed with implementation.**
