# AUDIT: Stage 1D - Support Systems
**Version:** 1.0.0.0  
**Date:** 2026-02-06  
**Scope:** CVCSController, VCTPhysics, RCSHeatup, RCPSequencer, TimeAcceleration, AlarmManager

---

## EXECUTIVE SUMMARY

### Files Analyzed
| File | Size | Lines | Status |
|------|------|-------|--------|
| CVCSController.cs | 24 KB | ~370 | **GOLD STANDARD** |
| VCTPhysics.cs | 16 KB | ~340 | **GOLD STANDARD** |
| RCSHeatup.cs | 16 KB | ~300 | **GOLD STANDARD** |
| RCPSequencer.cs | 11 KB | ~250 | **GOLD STANDARD** |
| TimeAcceleration.cs | 12 KB | ~260 | **GOLD STANDARD** |
| AlarmManager.cs | 10 KB | ~220 | **GOLD STANDARD** |

### Critical Findings

| Finding | Severity | Action Required |
|---------|----------|-----------------|
| VCTPhysics duplicates constants from PlantConstants | **MEDIUM** | Consolidate — use PlantConstants as single source |
| VCTPhysics.CalculateBalancedChargingForPurification returns 0 | **LOW** | Stub/dead code — remove or implement |
| VCTPhysics boron mixing model is simplified | **INFO** | Acceptable for sim fidelity |
| RCSHeatup.BulkHeatupStep uses CoupledThermo without P_floor/P_ceiling | **MEDIUM** | Verify CoupledThermo default bounds are appropriate |
| RCPSequencer hardcodes timing constants locally | **LOW** | Consider moving to PlantConstants |
| TimeAcceleration uses static mutable state | **INFO** | Acceptable for Unity singleton pattern |

---

## FILE 1: CVCSController.cs (GOLD STANDARD)

### Purpose
PI controller for CVCS charging flow during two-phase (normal) pressurizer operations. Extracted from engine per Audit Fix 7.2.

### Origin
Engine Architecture Audit Fix 7.2 — extracted ~30 lines of inline engine code into physics module. Mirrors the SolidPlantPressure CVCS controller pattern.

### Data Structures

**LetdownPath enum:**
- `RHR_CROSSCONNECT` — via HCV-128 at low temp/pressure
- `ORIFICE` — normal letdown via orifices
- `ISOLATED` — letdown isolated by low PZR level interlock

**LetdownPathState struct:** Path + reasoning fields

**HeaterControlState struct:** Heater enable/disable + proportional/backup status

**SealFlowState struct:** All RCP seal system flow components

**CVCSControllerState struct:** PI controller persistent state (integral error, flows, setpoints)

### Public Methods

| Method | Purpose | Notes |
|--------|---------|-------|
| Initialize(...) | Create controller state for two-phase ops | Called at solid→two-phase transition |
| Update(ref state, level, T_avg, P, rcpCount, dt_hr) | Main PI controller update | Calculates charging from level error |
| UpdateWithLetdown(ref state, level, letdown, rcpCount, dt_hr) | Simplified — letdown externally provided | Same PI logic, external letdown |
| CalculateSealFlows(rcpCount) | All seal system flows | 8/5/3/1 gpm per NRC IN 93-84 |
| GetLetdownPath(T_rcs, P, solidPZR, isolated) | Determine active letdown path | 350°F threshold per HRTD 19.0 |
| CalculateHeaterState(...) | Heater enable/proportional/backup | Low-level interlock at 17% |
| ResetIntegral(ref state) | Clear integral accumulator | Mode transition cleanup |
| GetNetRCSFlow(state) | Charging - Letdown | Positive = RCS gaining |
| GetChargingToRCS(state) | Charging minus seal injection | Actual flow reaching RCS |

### PI Controller Design

```
error = actual_level - setpoint_level (%)
charging = base_charging + Kp×(-error) + Ki×integral(-error)
base_charging = letdown + seal_injection (to maintain balance)
```

| Parameter | Value | Source |
|-----------|-------|--------|
| Kp | 3.0 gpm/% | PlantConstants.CVCS_LEVEL_KP (empirical) |
| Ki | 0.05 gpm/(%·sec) | PlantConstants.CVCS_LEVEL_KI (empirical) |
| Integral limit | 30 gpm | PlantConstants.CVCS_LEVEL_INTEGRAL_LIMIT |
| Min charging | seal_injection | Physical minimum |
| Max charging | 150 gpm | CCP capacity |

### Dependencies
- **PlantConstants** — CVCS_LEVEL_KP, CVCS_LEVEL_KI, CVCS_LEVEL_INTEGRAL_LIMIT, GetPZRLevelProgram(), SEAL_INJECTION_PER_PUMP_GPM, PZR_LOW_LEVEL_ISOLATION, CalculateTotalLetdownFlow(), RHR_LETDOWN_ISOLATION_TEMP_F, PZR_BACKUP_HEATER_LEVEL_OFFSET

### Dependents (Expected)
- HeatupSimEngine (during two-phase operations)

### Validation (7 tests)
1. Initialization produces valid state ✅
2. Low level → increased charging ✅
3. High level → decreased charging ✅
4. Very low level → letdown isolated ✅
5. Integral accumulates over time ✅
6. Charging clamped to minimum (seal injection) ✅
7. Net flow calculation correct ✅

### ⚠️ Issues
**None critical.** Well-structured module properly using PlantConstants as single source of truth.

---

## FILE 2: VCTPhysics.cs (GOLD STANDARD)

### Purpose
Volume Control Tank inventory, level, and boron tracking. Closed-loop buffer between letdown (RCS outlet) and charging (RCS inlet).

### Data Structures

**VCTState struct:** Level, volume, boron concentration/mass, all flow rates, alarm flags, cumulative tracking for mass conservation verification, divert/makeup/RWST status.

### Public Methods

| Method | Purpose | Notes |
|--------|---------|-------|
| InitializeColdShutdown(boron_ppm) | Cold shutdown VCT state | 55% level, 2000 ppm, balanced flows |
| InitializeNormal(level, boron_ppm) | Normal operations init | Configurable level/boron |
| Update(ref state, dt_sec, letdown, charging, sealReturn, rcpCount) | Main timestep update | Volume balance + boron + alarms |
| CalculateChargingForPZRLevel(error, letdown, sealReturn) | Level-based charging calc | Simple proportional controller |
| CalculateBalancedChargingForPurification(letdown) | **RETURNS 0 — STUB** | Dead code, needs fix or removal |
| CalculateRCSInventoryChange(charging, letdown, dt_sec) | Per-step RCS inventory delta | Gallons |
| AccumulateRCSChange(ref state, gal) | Track cumulative RCS change | For mass conservation check |
| VerifyMassConservation(state, rcsChange_gal) | Cross-system conservation check | ΔV_vct + ΔV_rcs = external_net |
| GetTurnoverTime(state) | VCT volume / throughput | Minutes |
| GetStatusString(state) | Human-readable status | NORMAL/DIVERTING/AUTO MAKEUP/etc |
| GetAlarmSeverity(state) | 0-3 severity level | For UI |
| IsLevelNormal(state) | Level in 40-70% band | bool |

### VCT Volume Balance Model

```
Inflows to VCT:
  + Letdown flow (from RCS via heat exchanger/orifices)
  + Seal return flow (RCP #1 seal leakoff → VCT)
  + Makeup flow (from RMS or RWST when level low)

Outflows from VCT:
  - Charging flow (to RCS via charging pump)
  - Divert flow (to BRS holdup tanks via LCV-112A)
  - CBO loss (controlled bleedoff)

Net: dV/dt = (letdown + sealReturn + makeup) - (charging + divert + CBO)
```

### Automatic Actions

| Condition | Action | Setpoint |
|-----------|--------|----------|
| Level > 70% (divert setpoint) | Proportional divert via LCV-112A | Ramps 0-100% over 20% band |
| Level < 70%-3% | Stop divert (hysteresis) | 67% |
| Level ≤ 25% | Auto makeup from RMS | 35 gpm |
| Level > 40% | Stop auto makeup | Normal low |
| Level ≤ 5% | RWST suction swap | 150 gpm emergency |
| Level > 17% | Stop RWST suction | |

### ⚠️ DUPLICATE CONSTANTS ISSUE

VCTPhysics defines its own constants that duplicate PlantConstants:

| VCTPhysics Constant | Value | PlantConstants Equivalent | Match? |
|---------------------|-------|---------------------------|--------|
| CAPACITY_GAL | 4000 | VCT_CAPACITY_GAL = 4000 | ✅ |
| LEVEL_HIGH_HIGH | 90 | VCT_LEVEL_HIGH_HIGH = 90 | ✅ |
| LEVEL_HIGH | 73 | VCT_LEVEL_HIGH = 73 | ✅ |
| LEVEL_NORMAL_HIGH | 70 | VCT_LEVEL_NORMAL_HIGH = 70 | ✅ |
| LEVEL_NORMAL_LOW | 40 | VCT_LEVEL_NORMAL_LOW = 40 | ✅ |
| LEVEL_MAKEUP_START | 25 | VCT_LEVEL_MAKEUP_START = 25 | ✅ |
| LEVEL_LOW | 17 | VCT_LEVEL_LOW = 17 | ✅ |
| LEVEL_LOW_LOW | 5 | VCT_LEVEL_LOW_LOW = 5 | ✅ |
| LETDOWN_NORMAL_GPM | 75 | LETDOWN_NORMAL_GPM = 75 | ✅ |
| CHARGING_NORMAL_GPM | 87 | CHARGING_NORMAL_GPM = 87 | ✅ |
| SEAL_RETURN_NORMAL_GPM | 12 | SEAL_RETURN_NORMAL_GPM = 12 | ✅ |
| CBO_LOSS_GPM | 1 | CBO_LOSS_GPM = 1 | ✅ |
| AUTO_MAKEUP_FLOW_GPM | 35 | AUTO_MAKEUP_FLOW_GPM = 35 | ✅ |
| MAX_MAKEUP_FLOW_GPM | 150 | MAX_MAKEUP_FLOW_GPM = 150 | ✅ |
| BORON_BAT_PPM | 7000 | BORON_BAT_PPM = 7000 | ✅ |
| BORON_RWST_PPM | 2500 | BORON_RWST_PPM = 2500 | ✅ |
| BORON_COLD_SHUTDOWN_PPM | 2000 | BORON_COLD_SHUTDOWN_BOL_PPM = 2000 | ✅ |
| MIXING_TAU_SEC | 120 | VCT_MIXING_TAU = 120 | ✅ |

**All values match** but this is a maintenance risk. If PlantConstants is updated and VCTPhysics is not, they will diverge. VCTPhysics should reference PlantConstants instead of defining its own copies.

### ⚠️ DEAD CODE: CalculateBalancedChargingForPurification

```csharp
public static float CalculateBalancedChargingForPurification(float letdownFlow_gpm)
{
    return 0f;
}
```

This method is a stub that always returns 0. Either implement or remove.

### Boron Mixing Model (Simplified)

The boron tracking uses a simple dilution model:
```csharp
// New concentration = (old_mass + makeup_mass) / total_volume
newConc = (oldConc × (totalVol - makeupVol) + makeupConc × makeupVol) / totalVol
```

This is adequate for the simulation's fidelity level but does not model:
- VCT mixing time constant (MIXING_TAU_SEC = 120s is defined but not used in UpdateBoron)
- Stratification effects
- Imperfect mixing during high-flow transients

### Dependencies
- **PlantConstants** — VCT_DIVERT_SETPOINT, VCT_DIVERT_PROP_BAND (used in Update)
- **UnityEngine** — Mathf.Clamp, Mathf.Clamp01

### Dependents (Expected)
- HeatupSimEngine
- HeatupIntegrationTests

### Validation
No ValidateCalculations() method. Mass conservation is verified externally via VerifyMassConservation(). 

**Recommendation:** Add a ValidateCalculations() method for consistency with other GOLD modules.

---

## FILE 3: RCSHeatup.cs (GOLD STANDARD)

### Purpose
RCS bulk heatup physics for Phase 2 (RCPs running, bubble exists). Extracted from engine per Audit Fix Issue #2.

### Data Structures

**BulkHeatupResult struct:** T_rcs, Pressure, DeltaT, DeltaP, SurgeFlow, NetHeat_MW, HeatupRate, Converged

**IsolatedHeatingResult struct:** T_pzr, T_rcs, Pressure, SurgeFlow, ConductionHeat_MW

### Public Methods

| Method | Purpose | Notes |
|--------|---------|-------|
| BulkHeatupStep(ref state, rcpCount, rcpHeat, heaterPower, rcsCap, pzrCap, dt_hr) | Main Phase 2 heatup step | Uses CoupledThermo for P-T-V |
| Step(ref state, rcpCount, heaterPower, rcsCap, pzrCap, dt_hr) | Simplified — auto-calc RCP heat | Delegates to BulkHeatupStep |
| IsolatedHeatingStep(...) | Phase 1 post-bubble (no RCPs) | PZR heats, conducts to RCS via surge |
| EstimateHeatupRate(rcpCount, heaterPower, T_rcs, totalHeatCap) | Predicted rate | °F/hr |
| EstimateTimeToTarget(currentTemp, targetTemp, rate) | Time estimate | hours |

### Physics: BulkHeatupStep

```
1. Heat Balance:
   Q_net = Q_rcp + Q_heaters - Q_insulation_loss
   
2. Temperature Change:
   ΔT = Q_net_BTU / (C_rcs + C_pzr)
   
3. P-T-V Coupling:
   CoupledThermo.SolveEquilibrium(ref state, ΔT)
   
4. Surge Flow:
   dV = V_rcs × β × ΔT → convert to gpm
```

### ⚠️ CoupledThermo Call Without P_floor/P_ceiling

```csharp
result.Converged = CoupledThermo.SolveEquilibrium(ref state, deltaT);
```

This calls `SolveEquilibrium` without specifying P_floor and P_ceiling. The Phase 2 fix (C3) added parameterized bounds to CoupledThermo, but RCSHeatup uses the default overload. Need to verify what the default bounds are in CoupledThermo — if they default to 1800-2700 psia, this would fail during early Phase 2 when pressure is still ~400-800 psia.

**Action Item:** Verify CoupledThermo's default P_floor/P_ceiling in the overload that RCSHeatup calls. If restrictive, pass appropriate bounds (e.g., 15f for P_floor during heatup).

### Physics: IsolatedHeatingStep (Post-Bubble, No RCPs)

```
1. PZR heated by heaters (capped at T_sat)
2. Heat conducted from PZR to RCS via surge line natural convection
3. RCS gains from conduction, loses from insulation
4. Pressure from PZR thermal expansion (DAMPING_FACTOR = 0.5)
5. Surge flow from PZR expansion
```

### Dependencies
- **CoupledThermo** — SolveEquilibrium, QuickPressureEstimate (SystemState type)
- **HeatTransfer** — NetHeatInput_MW, InsulationHeatLoss_MW, SurgeLineHeatTransfer_MW
- **WaterProperties** — SaturationTemperature
- **ThermalExpansion** — ExpansionCoefficient, PressureChangeFromTemp
- **PlantConstants** — RCS_WATER_VOLUME, RCP_HEAT_MW_EACH

### Validation (6 tests)
1. Heatup rate ~50°F/hr with 4 RCPs (30-80 range) ✅
2. More RCPs → faster heatup ✅
3. Higher temp → more loss → slower heatup ✅
4. Isolated heating increases PZR temp ✅
5. Surge line conduction > 0 when T_pzr > T_rcs ✅
6. Time to target finite with positive rate ✅

---

## FILE 4: RCPSequencer.cs (GOLD STANDARD)

### Purpose
Manages automatic RCP startup sequencing during heatup. Enforces bubble existence, minimum pressure, and sequential start timing.

### Origin
Engine Architecture Audit Fix Issue #6 — extracts RCP startup timing logic from engine.

### Sequencing Logic

```
Requirements (all must be met):
  1. Steam bubble exists in PZR
  2. Pressure ≥ 320 psig (334.7 psia) for NPSH
  3. Time delay from bubble formation

Timing:
  RCP #1: bubbleTime + 1.0 hr
  RCP #2: bubbleTime + 1.5 hr
  RCP #3: bubbleTime + 2.0 hr
  RCP #4: bubbleTime + 2.5 hr
```

### Constants (Hardcoded in Module)

| Constant | Value | Notes |
|----------|-------|-------|
| RCP1_START_DELAY | 1.0 hr | First RCP start delay after bubble |
| RCP_START_INTERVAL | 0.5 hr | Between subsequent RCP starts |
| TOTAL_RCP_COUNT | 4 | |

**Note:** These timing constants are defined locally rather than in PlantConstants. Consider consolidating for consistency. However, they are operational procedure values, not physical plant parameters, so local definition is defensible.

### Public Methods

| Method | Purpose | Notes |
|--------|---------|-------|
| GetTargetRCPCount(bubble, simTime, bubbleTime, P) | Target count for current conditions | Main logic |
| GetState(...) | Full diagnostic state | For display |
| CheckForStartEvent(...) | Detect start this timestep | Returns RCP number or 0 |
| GetScheduledStartTime(rcpNumber, bubbleTime) | When specific RCP should start | hours |
| GetRCPHeat_MW(rcpCount) | Total RCP heat | MW |
| GetHeaterPower_MW(rcpCount, enabled) | Heater power | Always 1.8 MW during heatup |

### Dependencies
- **PlantConstants** — MIN_RCP_PRESSURE_PSIA, RCP_HEAT_MW_EACH, HEATER_POWER_TOTAL

### Validation (8 tests)
1. No RCPs without bubble ✅
2. No RCPs immediately after bubble (within delay) ✅
3. 1 RCP after delay ✅
4. 2 RCPs after interval ✅
5. All 4 eventually ✅
6. Low pressure blocks start ✅
7. Scheduled start times correct ✅
8. RCP heat = 21 MW at 4 pumps ✅

---

## FILE 5: TimeAcceleration.cs (GOLD STANDARD)

### Purpose
MSFS-style dual time tracking with discrete time warp steps. Manages wall-clock time vs. simulation time independently.

### Design
- **Wall-clock time:** Always advances 1:1 with system clock
- **Simulation time:** Advances at selected multiplier
- **Physics safety:** Physics timestep unchanged — more steps executed per frame at higher multiplier

### Speed Steps
| Index | Multiplier | Label |
|-------|-----------|-------|
| 0 | 1x | Real-Time |
| 1 | 2x | 2x |
| 2 | 4x | 4x |
| 3 | 8x | 8x |
| 4 | 10x | 10x |

### Public Properties

| Property | Type | Notes |
|----------|------|-------|
| CurrentSpeedIndex | int | 0-4 |
| CurrentMultiplier | float | 1-10 |
| IsRealTime | bool | True when 1x |
| WallClockTime_Hours | float | Cumulative real time |
| SimulationTime_Hours | float | Cumulative sim time |
| SimDeltaTime_Hours | float | Per-frame sim delta |
| WallDeltaTime_Hours | float | Per-frame wall delta |
| StartRealTime | DateTime | Session start |
| EffectiveMultiplier | float | Total sim/wall ratio |

### Public Methods

| Method | Purpose |
|--------|---------|
| Initialize(startSpeedIndex) | Reset all tracking |
| Tick() | Per-frame update (uses Time.deltaTime) |
| Tick(float realDt) | Manual dt injection for testing |
| SetSpeed(index) | Set speed by index |
| SetSpeedByMultiplier(float) | Set speed by nearest multiplier |
| SpeedUp() / SlowDown() | Step up/down |
| ResetToRealTime() | Return to 1x |
| FormatTime(hours) | HH:MM:SS string |
| FormatTimeCompact(hours) | H:MM string |
| GetStatusString() | "4x WARP" or "REAL-TIME" |
| GetDetailedStatus() | Full time panel string |
| SyncSimTime(hours) | One-time sync from engine |

### Dependencies
- **UnityEngine** — Time.deltaTime, Mathf.Clamp, Mathf.FloorToInt, DateTime

### Dependents (Expected)
- HeatupSimEngine (reads SimDeltaTime_Hours)
- HeatupValidationVisual (reads display properties)

### ⚠️ Static Mutable State
All state is static — this is a singleton pattern. Acceptable for Unity but means only one simulation can run at a time. Not a concern for this application.

### Validation
No ValidateCalculations() method. Simple enough that unit tests in the test runner would suffice. Low priority.

---

## FILE 6: AlarmManager.cs (GOLD STANDARD)

### Purpose
Centralized alarm setpoint checking for all plant annunciators. Extracted from engine per Audit Fix Priority 5.

### Origin
Extracts ~20 inline alarm setpoint checks from HeatupSimEngine into testable module.

### Alarm Setpoints

| Alarm | Setpoint | Source |
|-------|----------|--------|
| PZR Level Low | < 20% | NRC HRTD 10.3 |
| PZR Level High | > 85% | NRC HRTD 10.3 |
| Steam Bubble OK | Bubble + 5-95% level | |
| RCS Flow Low | RCPs = 0 | |
| Pressure Low | < 350 psia | NRC HRTD |
| Pressure High | > 2300 psia | |
| Subcooling Low | < 30°F | NRC HRTD 3.4 |
| SMM Low Margin | < 15°F (> 0°F) | NRC HRTD 3.4 |
| SMM No Margin | ≤ 0°F | NRC HRTD 3.4 |
| RVLIS Level Low | < 90% (Full Range valid) | NRC HRTD 3.3 |
| Heatup In Progress | Rate > 1°F/hr | |
| Seal Injection OK | ≥ 7 gpm/RCP | |

### Public Methods

| Method | Purpose |
|--------|---------|
| CheckAlarms(AlarmInputs) | Evaluate all alarms → AlarmState |
| GetActiveAlarmSummary(AlarmState) | Comma-separated active alarm list |

### Dependencies
- None (pure logic on input values)

### Dependents (Expected)
- HeatupSimEngine / HeatupValidationVisual

### Validation
No ValidateCalculations() method. Simple threshold comparisons — could benefit from basic test coverage.

---

## CROSS-MODULE DEPENDENCY MAP (Stage 1D)

```
┌─────────────────────────────────────────────────────────────────┐
│                        PlantConstants                            │
│                              │                                   │
│    ┌─────────────┬──────────┼──────────┬──────────────┐         │
│    ▼             ▼          ▼          ▼              ▼         │
│ CVCSController  VCTPhysics  RCSHeatup  RCPSequencer  AlarmMgr  │
│    │             │          │                                   │
│    │             │          ├──►CoupledThermo (Gap #1)          │
│    │             │          ├──►HeatTransfer (Gap #10)          │
│    │             │          ├──►ThermalExpansion                 │
│    │             │          └──►WaterProperties                  │
│    │             │                                               │
│    └─────┬───────┘                                               │
│          ▼                                                       │
│   (HeatupSimEngine)  ◄── TimeAcceleration                      │
│          │                                                       │
│          ▼                                                       │
│   HeatupValidationVisual                                         │
└─────────────────────────────────────────────────────────────────┘
```

### Key Integration Points

1. **CVCSController ↔ VCTPhysics**: CVCSController outputs charging/letdown flows; VCTPhysics consumes them as inputs to Update(). These modules don't call each other directly — the engine orchestrates data flow.

2. **RCSHeatup → CoupledThermo**: BulkHeatupStep calls CoupledThermo.SolveEquilibrium to resolve P-T-V coupling after temperature change.

3. **RCPSequencer → PlantConstants**: Uses MIN_RCP_PRESSURE_PSIA for start permissive, RCP_HEAT_MW_EACH for heat calculation.

4. **TimeAcceleration → HeatupSimEngine**: Engine reads SimDeltaTime_Hours each frame to determine how much simulation time to advance.

5. **AlarmManager ← All Systems**: AlarmManager receives plant parameters via AlarmInputs struct; it has no outward dependencies.

---

## VALIDATION SUMMARY

| Module | Method | Tests | Status |
|--------|--------|-------|--------|
| CVCSController | ValidateCalculations() | 7 | ✅ |
| VCTPhysics | *(none)* | 0 | ⚠️ Missing |
| RCSHeatup | ValidateCalculations() | 6 | ✅ |
| RCPSequencer | ValidateCalculations() | 8 | ✅ |
| TimeAcceleration | *(none)* | 0 | Low priority |
| AlarmManager | *(none)* | 0 | Low priority |
| **Total** | | **21** | |

---

## ISSUES REGISTER

### MEDIUM Priority

**1. VCTPhysics Duplicate Constants**
- VCTPhysics defines 18 constants that duplicate PlantConstants values
- All currently match but creates maintenance divergence risk
- **Recommendation:** Replace VCTPhysics local constants with PlantConstants references
- **Impact:** Refactoring only, no physics change

**2. RCSHeatup CoupledThermo P_floor/P_ceiling**
- BulkHeatupStep calls `CoupledThermo.SolveEquilibrium(ref state, deltaT)` — 2-parameter overload
- Need to verify this overload's default P_floor/P_ceiling
- If defaults are 1800-2700 psia, this will fail during early Phase 2 heatup (~400-800 psia)
- **Recommendation:** Verify in Stage 1B CoupledThermo review; if needed, update call to pass explicit bounds

### LOW Priority

**3. VCTPhysics Dead Code**
- `CalculateBalancedChargingForPurification()` returns 0f unconditionally
- Either implement or remove
- **Recommendation:** Remove in Stage 6 refactoring

**4. VCTPhysics Missing ValidateCalculations()**
- All other GOLD modules have self-validation; VCTPhysics does not
- **Recommendation:** Add in Stage 6 refactoring

**5. RCPSequencer Local Constants**
- RCP1_START_DELAY and RCP_START_INTERVAL defined locally
- Defensible as operational procedure vs. physical parameters
- **Recommendation:** Document decision, no change needed

### INFO

**6. VCTPhysics Boron Mixing Simplification**
- MIXING_TAU_SEC defined (120s) but not used in UpdateBoron
- Simple dilution model adequate for simulation fidelity
- **Recommendation:** Document as known simplification

**7. TimeAcceleration Static Mutable State**
- Unity singleton pattern — acceptable for single-simulation application
- No action required

---

## ACTION ITEMS FOR LATER STAGES

### For Stage 2 (Parameter Audit)
1. Verify CVCSController PI gains (Kp=3.0, Ki=0.05) produce realistic level response
2. Verify VCT level setpoints against FSAR Table 9.3-x
3. Verify seal flow values (8/5/3/1 gpm) against NRC IN 93-84
4. Verify AlarmManager setpoints against plant alarm response procedures

### For Stage 4 (Module Integration Audit)
1. **CRITICAL:** Trace data flow: CVCSController → HeatupSimEngine → VCTPhysics
2. Verify HeatupSimEngine calls RCSHeatup.BulkHeatupStep (not duplicate physics)
3. Verify TimeAcceleration.SimDeltaTime_Hours is used as master time source
4. Verify AlarmManager.CheckAlarms receives correct inputs from engine
5. Verify RCPSequencer output drives actual RCP state changes in engine

### For Stage 6 (Refactoring)
1. Consolidate VCTPhysics duplicate constants → use PlantConstants
2. Remove or implement CalculateBalancedChargingForPurification
3. Add ValidateCalculations() to VCTPhysics, TimeAcceleration, AlarmManager

---

## NEXT STEPS

Proceed to **Sub-Stage 1E: Reactor Core Modules** to analyze:
- FuelAssembly.cs (32 KB)
- ReactorSimEngine.cs (30 KB)
- ReactorController.cs (30 KB)
- ControlRodBank.cs (29 KB)
- ReactorCore.cs (24 KB)
- FeedbackCalculator.cs (20 KB)
- PowerCalculator.cs (18 KB)

---

**Document Version:** 1.0.0.0  
**Audit Status:** COMPLETE  
**Files Reviewed:** 6/6  
**Issues Found:** 0 Critical, 2 Medium, 3 Low, 2 Info
