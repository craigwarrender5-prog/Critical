# Condenser and Feedwater Return System â€” Architecture Specification

**Document ID:** ARCH-SPEC-CS0115
**Version:** 1.0
**Date:** 2026-02-17
**Status:** DRAFT â€” Pending Review
**CS Reference:** CS-0115
**IP Reference:** IP-0046
**DP Reference:** DP-0011 (Steam Generator Secondary Physics)
**Author:** Codex

---

## 1) Purpose and Scope

This document defines the architecture for the Condenser State, Feedwater Return, and Startup Permissive Boundary subsystems required by CS-0115 to unblock CS-0078 (SG secondary pressure response).

### 1.1 In Scope
- CondenserPhysics module (vacuum dynamics, backpressure, CW pumps, C-9 interlock)
- FeedwaterSystem module (hotwell inventory, condensate pumps, AFW, CST tracking)
- StartupPermissives module (C-9/P-12 interlock logic, steam dump authority gating)
- PlantConstants.Condenser and PlantConstants.Feedwater constant partials
- HeatupSimEngine integration (step-loop insertion, state field exposure)
- SteamDumpController modification (C-9/P-12 gating on ShouldAutoEnable)
- PlantOverviewScreen telemetry (condenser vacuum, CW status, return-path indicators)
- Logging/telemetry extension for condenser/feedwater state fields

### 1.2 Out of Scope
- Main turbine model (stays placeholder)
- Turbine-generator electrical output
- Full feedwater heater train thermal model (simplified for startup)
- SG blowdown chemistry
- LP/HP heater detailed cascading drain model
- Main feedwater pump turbine drive model

### 1.3 Design Philosophy
- **Startup-focused:** The model covers Mode 5 through Mode 3 startup and hot standby (HZP). Full-power feedwater train dynamics are deferred.
- **Conservation-compliant:** All mass transfers use lbm. No volume-density overwrites.
- **Static-class pattern:** Follows existing project convention (static physics classes with state structs passed by ref).
- **Minimal coupling:** Each module owns its state struct. Integration occurs through the engine step loop only.

---

## 2) System Architecture Overview

### 2.1 New Modules (4 files)

| File | Class | Responsibility |
|------|-------|----------------|
| `Assets/Scripts/Physics/CondenserPhysics.cs` | `CondenserPhysics` (static) | Condenser vacuum dynamics, backpressure model, air ejector startup, CW pump state, C-9 interlock |
| `Assets/Scripts/Physics/FeedwaterSystem.cs` | `FeedwaterSystem` (static) | Hotwell inventory, condensate pump state, AFW pump state, CST inventory, feedwater return flow |
| `Assets/Scripts/Physics/StartupPermissives.cs` | `StartupPermissives` (static) | C-9/P-12 interlock evaluation, steam dump bridge state machine, operator bypass tracking |
| `Assets/Scripts/Physics/PlantConstants.Condenser.cs` | `PlantConstants` (partial) | Condenser geometry, vacuum setpoints, CW pump parameters, C-9/P-12 thresholds |

### 2.2 Modified Files

| File | Change Summary |
|------|----------------|
| `PlantConstants.cs` | No change (partial root) |
| `SteamDumpController.cs` | Gate `ShouldAutoEnable()` and `Update()` on C-9/P-12 permissive state |
| `HeatupSimEngine.cs` | Add condenser/feedwater/permissive state fields; insert Update calls in step loop |
| `HeatupSimEngine.Init.cs` | Initialize condenser/feedwater/permissive state in cold/warm start |
| `HeatupSimEngine.Logging.cs` | Add condenser/feedwater telemetry to interval logs |
| `HeatupSimEngine.HZP.cs` | Wire C-9/P-12 into steam dump enable path |
| `SGMultiNodeThermal.cs` | Accept condenser sink availability for steam mass balance |
| `PlantOverviewScreen.cs` | Replace condenser vacuum placeholder with live value |

### 2.3 Module Dependency Graph

```
HeatupSimEngine (coordinator)
  â”œâ”€â”€ CondenserPhysics.Update(ref CondenserState, steamLoad, dt)
  â”‚     â””â”€â”€ Outputs: vacuum_inHg, backpressure_psia, C9_available
  â”œâ”€â”€ FeedwaterSystem.Update(ref FeedwaterState, condenserState, sgState, dt)
  â”‚     â””â”€â”€ Outputs: hotwellMass_lb, cstLevel_gal, returnFlow_lbhr
  â”œâ”€â”€ StartupPermissives.Evaluate(ref PermissiveState, condenserState, T_avg)
  â”‚     â””â”€â”€ Outputs: C9, P12_blocking, dumpBridgeState
  â”œâ”€â”€ SteamDumpController.Update(ref SteamDumpState, ..., permissiveState)
  â”‚     â””â”€â”€ Gated by: C9 AND !P12_blocking
  â””â”€â”€ SGMultiNodeThermal.Update(ref SGMultiNodeState, ..., condenserSinkAvailable)
        â””â”€â”€ Uses: condenserSinkAvailable for steam exit path mass balance
```

---

## 3) CondenserPhysics Module

### 3.1 Purpose
Models condenser vacuum as a first-order dynamic system responding to steam heat load (input) versus circulating water heat rejection (output). Provides C-9 interlock output.

### 3.2 State Struct: `CondenserState`

```csharp
public struct CondenserState
{
    // Vacuum state
    public float Vacuum_inHg;              // Current condenser vacuum (0-29.92)
    public float Backpressure_psia;         // Absolute backpressure
    public float HotwellTemp_F;             // Hotwell saturation temperature

    // Equipment state
    public int CW_PumpsRunning;             // 0-4 circulating water pumps
    public bool AirEjectorsRunning;         // Hogging or main air ejectors active
    public bool VacuumEstablished;          // Vacuum > VACUUM_ESTABLISHED_INHG

    // Interlock outputs
    public bool C9_CondenserAvailable;      // C-9: vacuum > 22 inHg AND CW >= 1

    // Heat balance
    public float SteamLoad_BTUhr;           // Current steam heat load on condenser
    public float CW_HeatRejection_BTUhr;    // CW heat removal capacity
    public float EquilibriumBackpressure_psia; // Steady-state backpressure target

    // Startup tracking
    public float VacuumPulldownStartTime_hr; // When vacuum pulldown began
    public bool HoggingComplete;            // Hogging ejectors transferred to main

    // Telemetry
    public string StatusMessage;
}
```

### 3.3 Physics Model

#### 3.3.1 Equilibrium Backpressure
The steady-state backpressure is determined by the heat balance between steam condensation load and CW rejection capacity:

```
Q_steam = SteamMassFlow_lbhr Ã— h_fg_at_backpressure  [BTU/hr]
Q_cw = N_cw_pumps Ã— PUMP_FLOW_GPM Ã— 500 Ã— CW_DELTA_T_F  [BTU/hr]

EquilibriumBackpressure = f(Q_steam / Q_cw)
```

For the startup range (low steam loads), the equilibrium backpressure will be near design vacuum. At full steam dump load (~40% rated steam flow), backpressure rises toward the upper operating range.

Simplified model (valid for startup through HZP):
```
// Ratio of steam load to CW capacity determines backpressure offset
loadFraction = Q_steam / Q_cw_max
P_eq_psia = P_DESIGN_PSIA + (P_MAX_PSIA - P_DESIGN_PSIA) Ã— loadFractionÂ²
```

Where:
- `P_DESIGN_PSIA` = 1.5 psia (~28.9 in. Hg vacuum) â€” design vacuum with CW running, no load
- `P_MAX_PSIA` = 3.3 psia (~23.2 in. Hg vacuum) â€” full steam dump load backpressure

#### 3.3.2 Vacuum Dynamics (First-Order Lag)
```
dP/dt = (P_equilibrium - P_current) / tau

tau = CONDENSER_TIME_CONSTANT_SEC  // 30 seconds
```

At startup with no steam load and CW pumps running + air ejectors:
```
Vacuum pulls down from 0 in. Hg (atmospheric) toward design vacuum over ~5-10 minutes
```

#### 3.3.3 Vacuum Pulldown Sequence
The condenser vacuum establishment follows this startup sequence:

| Phase | Duration | Vacuum Range | Equipment |
|-------|----------|-------------|-----------|
| Pre-start | â€” | 0 in. Hg (atmospheric) | All off |
| CW pumps start | 1-2 min | 0 in. Hg | CW pump(s) started |
| Hogging ejectors | 5-10 min | 0 â†’ 15-20 in. Hg | 3 hogging ejectors |
| Transfer to main | 1 min | 15-20 in. Hg | Switch to 2 main ejectors |
| Design vacuum | 5-10 min | 20 â†’ 26-28 in. Hg | Main ejectors steady |
| C-9 satisfied | â€” | > 22 in. Hg + CW â‰¥ 1 | Interlock met |

For the simulator, vacuum establishment is modeled as a scheduled event sequence triggered by the engine at the appropriate startup phase (typically when T_RCS approaches 350-400Â°F and Mode 3 preparation begins).

#### 3.3.4 C-9 Interlock Logic
```
C9 = (Vacuum_inHg >= C9_VACUUM_THRESHOLD_INHG)    // >= 22 in. Hg
   AND (CW_PumpsRunning >= C9_MIN_CW_PUMPS)        // >= 1
```

C-9 is evaluated every timestep. Loss of vacuum or CW pumps immediately removes C-9, which forces steam dumps closed.

### 3.4 Key Methods

```csharp
public static class CondenserPhysics
{
    // Initialize to atmospheric (pre-startup)
    public static CondenserState Initialize();

    // Start CW pumps and begin vacuum pulldown
    public static void StartVacuumPulldown(ref CondenserState state, float simTime);

    // Main update â€” called every timestep
    public static void Update(ref CondenserState state, float steamLoad_BTUhr, float dt_hr);

    // Convert between vacuum and absolute pressure
    public static float VacuumToAbsolute(float vacuum_inHg);
    public static float AbsoluteToVacuum(float pressure_psia);

    // Saturation temperature at current backpressure
    public static float GetHotwellSatTemp(float backpressure_psia);
}
```

---

## 4) FeedwaterSystem Module

### 4.1 Purpose
Tracks secondary-side water inventory through the condenser/feedwater return path during startup. Provides closed-loop mass accounting: steam dumped to condenser condenses in hotwell, returns via condensate/feedwater pumps (or AFW) to SGs.

### 4.2 Modeling Level
For startup simulation, the feedwater train is modeled at **inventory and flow availability** level, not at full thermal-hydraulic detail. The key question is: "Is there feedwater return capacity available, and what is the mass flow rate?"

The detailed LP/HP heater thermal chain is not modeled. Feedwater temperature is parameterized based on startup phase:
- Pre-heater operation: T_fw â‰ˆ T_hotwell (~120Â°F)
- With heaters online (>20% power): T_fw tracks documented profile (120â†’440Â°F)

### 4.3 State Struct: `FeedwaterState`

```csharp
public struct FeedwaterState
{
    // Hotwell
    public float HotwellMass_lb;            // Current hotwell water inventory
    public float HotwellLevel_in;           // Level indication (0-40 in.)
    public float HotwellTemp_F;             // From condenser backpressure

    // CST (Condensate Storage Tank)
    public float CST_Volume_gal;            // Current CST volume
    public bool CST_BelowTechSpec;          // Below 239,000 gal warning

    // Condensate Pumps
    public int CondensatePumpsRunning;       // 0-2
    public float CondensateFlow_gpm;         // Total condensate flow
    public float CondensateFlow_lbhr;        // Mass flow rate

    // Main Feedwater Pumps (startup context)
    public int MFP_Running;                  // 0-2
    public float FeedwaterFlow_gpm;          // Total FW flow to SGs
    public float FeedwaterFlow_lbhr;         // Mass flow to SGs
    public float FeedwaterTemp_F;            // Temperature at SG inlet

    // Auxiliary Feedwater
    public int AFW_MotorPumpsRunning;        // 0-2 (440 gpm each)
    public bool AFW_TurbinePumpRunning;      // 1 (880 gpm)
    public float AFW_Flow_gpm;              // Total AFW flow
    public float AFW_Flow_lbhr;             // AFW mass flow

    // Derived
    public float TotalReturnFlow_lbhr;       // Total mass returning to SGs
    public float NetSteamLoss_lbhr;          // Steam lost to atmosphere (not condensed)

    // Status
    public bool FeedwaterAvailable;          // At least one return path exists
    public string StatusMessage;
}
```

### 4.4 Physics Model

#### 4.4.1 Hotwell Mass Balance
```
dM_hotwell/dt = m_dot_condensed - m_dot_condensate_pumps - m_dot_makeup + m_dot_cst_makeup

where:
  m_dot_condensed = steam dumped to condenser (from SteamDumpController)
  m_dot_condensate_pumps = flow out to feedwater train
  m_dot_makeup = hotwell makeup from CST (level control)
```

Hotwell level control follows documented setpoints:
- Normal setpoint: 24 in.
- Reject valve opens: 28 in. (returns excess to CST)
- Reject valve full open: 40 in.
- Makeup valve opens: 21 in. (draws from CST)
- Makeup valve full open: 8 in.

#### 4.4.2 CST Inventory
```
dV_cst/dt = V_reject - V_makeup - V_afw_draw

where:
  V_reject = hotwell reject flow to CST (when hotwell > 28 in.)
  V_makeup = CST to hotwell makeup (when hotwell < 21 in.)
  V_afw_draw = AFW pump suction from CST
```

CST starts at full capacity (450,000 gal). Tech Spec minimum is 239,000 gal.

#### 4.4.3 Condensate/Feedwater Pump Model (Simplified)
For startup, pumps are modeled as on/off with rated capacity:

| Pump | Rated Flow | Head | Start Condition |
|------|-----------|------|-----------------|
| Condensate (Ã—2) | 11,000 gpm each | 477 psi | Manual start during startup |
| MFP (Ã—2) | 19,800 gpm each | ~870 psi | Manual start at ~20% power |
| Startup AFW | 1,020 gpm | 1,472 psi | Manual start for SG fill |
| Motor AFW (Ã—2) | 440 gpm each | ~563 psi | Auto on low-low SG level |
| Turbine AFW (Ã—1) | 880 gpm | ~520 psi | Auto on low-low SG level (2/4) |

During startup (Mode 5â†’3), feedwater return to SGs uses:
1. **AFW system** â€” primary SG feed during heatup (condensate/MFP not yet running)
2. **Condensate pumps** â€” started when condenser established
3. **MFPs** â€” started at ~20% power (post-reactor startup)

For the heatup simulation scope, AFW is the primary return path.

#### 4.4.4 Feedwater Temperature
Parameterized by phase (no heater chain model):

| Phase | T_fw (Â°F) | Basis |
|-------|-----------|-------|
| AFW during startup | 80-120 | CST water temperature |
| Condensate pumps (no heaters) | ~120 | Hotwell temperature |
| With LP heaters | 120-360 | Documented heater outlet profile |
| Full heater train | 440 | Documented SG inlet temperature |

#### 4.4.5 Steam Mass Balance Integration
The critical integration point with SGMultiNodeThermal:

```
When SG is steaming (Regime 2/3):
  m_dot_steam_out = SG steam production rate [lb/hr]

Steam exit paths:
  1. To condenser (if C-9 satisfied and dumps open):
     m_dot_to_condenser = SteamDumpController valve position Ã— max dump flow
     â†’ condenses in hotwell â†’ returns via FW/AFW

  2. To atmosphere (atmospheric relief / safety valves):
     m_dot_to_atm = atmospheric dump flow (if SG pressure > relief setpoint)
     â†’ LOST from system (CST depletion)

  3. Steam line warming condensation (early startup):
     m_dot_condensed_in_line = steam line condensation rate (from SGMultiNodeThermal)
     â†’ drains back to SG or drain traps â†’ eventually to condenser

SG secondary mass balance:
  dM_sg_secondary/dt = m_dot_feedwater_return - m_dot_steam_out
```

### 4.5 Key Methods

```csharp
public static class FeedwaterSystem
{
    public static FeedwaterState Initialize(float cstVolume_gal);

    public static void Update(ref FeedwaterState state,
                              float steamToCondenser_lbhr,
                              float steamToAtmosphere_lbhr,
                              float sgDemand_lbhr,
                              float hotwellTemp_F,
                              float dt_hr);

    public static void StartCondensatePump(ref FeedwaterState state, int pumpNumber);
    public static void StopCondensatePump(ref FeedwaterState state, int pumpNumber);

    public static void StartAFWPump(ref FeedwaterState state, AFWPumpType type);
    public static void StopAFWPump(ref FeedwaterState state, AFWPumpType type);

    public static float GetAvailableReturnFlow_lbhr(in FeedwaterState state);
}

public enum AFWPumpType { Motor1, Motor2, TurbineDriven }
```

---

## 5) StartupPermissives Module

### 5.1 Purpose
Evaluates plant startup interlock permissives (C-9, P-12) and manages the steam dump bridge state machine. Provides a single authoritative gating contract consumed by SteamDumpController.

### 5.2 State Struct: `PermissiveState`

```csharp
public struct PermissiveState
{
    // C-9: Condenser Available
    public bool C9_Satisfied;               // Condenser available for steam dump

    // P-12: Low-Low T_avg
    public bool P12_Active;                 // T_avg below P-12 threshold
    public bool P12_Bypassed;               // Operator bypass active
    public bool P12_Blocking;               // P12 active AND not bypassed

    // Steam Dump Bridge State Machine
    public SteamDumpBridgeState BridgeState; // Current bridge state

    // Combined Authority
    public bool SteamDumpPermitted;         // Final authority: C9 AND !P12_blocking

    // Telemetry
    public float P12_Threshold_F;           // Current P-12 setpoint
    public string StatusMessage;
}

public enum SteamDumpBridgeState
{
    DumpsUnavailable,    // !C9 OR (P12 active AND no bypass)
    DumpsArmed,          // C9 AND !P12_blocking AND mode selected; valves closed
    DumpsModulating      // Armed + pressure > setpoint + deadband
}
```

### 5.3 Interlock Logic

#### 5.3.1 C-9 Evaluation
```
C9 = CondenserState.C9_CondenserAvailable
   = (Vacuum_inHg >= 22.0) AND (CW_PumpsRunning >= 1)
```

C-9 is a pass-through from CondenserPhysics. StartupPermissives does not independently evaluate vacuum.

#### 5.3.2 P-12 Evaluation
```
P12_Active = (T_avg < P12_THRESHOLD_F)      // T_avg < 553Â°F

P12_Blocking = P12_Active AND !P12_Bypassed

// P-12 bypass: operator action, tracked in PermissiveState
// During startup, P-12 is typically bypassed once T_avg > ~350Â°F
// to allow steam dump arming before reaching 553Â°F
```

P-12 threshold: 553Â°F (documented low-low Tavg for steam line isolation unblock).

#### 5.3.3 Steam Dump Bridge State Machine
```
Evaluate each timestep:

if (!C9 || P12_Blocking):
    BridgeState = DumpsUnavailable
    SteamDumpPermitted = false

elif (C9 && !P12_Blocking && steamDumpModeSelected):
    if (steamPressure > setpoint + deadband):
        BridgeState = DumpsModulating
        SteamDumpPermitted = true
    else:
        BridgeState = DumpsArmed
        SteamDumpPermitted = true

else:
    BridgeState = DumpsUnavailable
    SteamDumpPermitted = false
```

### 5.4 Startup Timeline for Permissive Sequence

| Event | T_RCS | C-9 | P-12 | Bridge State |
|-------|-------|-----|------|--------------|
| Cold shutdown | <200Â°F | false (no vacuum) | active (T_avg << 553Â°F) | Unavailable |
| CW pumps start, vacuum pulled | ~350-400Â°F | true (>22 in. Hg) | active | Unavailable (P-12 blocking) |
| Operator bypasses P-12 | ~350-400Â°F | true | bypassed | Armed (if mode selected) |
| SG pressure reaches setpoint | 557Â°F | true | inactive (T_avg > 553Â°F) | Modulating |

### 5.5 Key Methods

```csharp
public static class StartupPermissives
{
    public static PermissiveState Initialize();

    public static void Evaluate(ref PermissiveState state,
                                in CondenserState condenserState,
                                float T_avg_F,
                                bool steamDumpModeSelected,
                                float steamPressure_psig,
                                float dumpSetpoint_psig,
                                float deadband_psi);

    public static void SetP12Bypass(ref PermissiveState state, bool bypassed);
}
```

---

## 6) PlantConstants.Condenser â€” Constants Definition

```csharp
public static partial class PlantConstants
{
    public static class Condenser
    {
        // --- Condenser Geometry ---
        public const int SHELL_COUNT = 3;                              // A, B, C shells
        public const float TOTAL_HT_AREA_FT2 = 900_000f;             // Total tube area
        public const int TUBE_COUNT = 60_000;                          // Approximate
        public const float TUBE_OD_IN = 1.25f;                        // Titanium tubes

        // --- Design Operating Points ---
        public const float DESIGN_VACUUM_INHG = 28.0f;                // Design vacuum
        public const float DESIGN_BACKPRESSURE_PSIA = 1.5f;           // At design vacuum
        public const float MAX_OPERATING_BACKPRESSURE_PSIA = 3.3f;    // At full dump load
        public const float HOTWELL_DESIGN_TEMP_F = 120f;              // At design vacuum

        // --- C-9 Interlock ---
        public const float C9_VACUUM_THRESHOLD_INHG = 22.0f;          // Minimum vacuum for C-9
        public const int C9_MIN_CW_PUMPS = 1;                         // Minimum CW pumps for C-9
        public const float TURBINE_TRIP_VACUUM_INHG = 20.0f;          // Turbine trip threshold

        // --- P-12 Interlock ---
        public const float P12_LOW_LOW_TAVG_F = 553.0f;               // P-12 threshold

        // --- Circulating Water ---
        public const int CW_PUMP_COUNT = 4;                           // Total CW pumps
        public const float CW_PUMP_FLOW_GPM = 150_000f;               // Per pump
        public const float CW_DELTA_T_F = 20.0f;                      // Temp rise across condenser
        public const float CW_PUMP_HEAD_FT = 35f;

        // Per-pump heat rejection: 150,000 gpm Ã— 500 lb/min/gpm Ã— 60 min/hr Ã— 20Â°F
        // = 9.0 Ã— 10^10 BTU/hr per pump (this is the max capacity)
        // Realistic startup heat load is << this capacity
        public const float CW_PUMP_REJECTION_BTUHR = 9.0e10f;

        // --- Vacuum Dynamics ---
        public const float CONDENSER_TAU_SEC = 30f;                    // Time constant
        public const float CONDENSER_TAU_HR = 30f / 3600f;            // In hours

        // --- Vacuum Pulldown Timing ---
        public const float HOGGING_DURATION_MIN = 8f;                  // Time for hogging ejectors
        public const float HOGGING_TARGET_INHG = 18f;                  // Hogging achieves ~18 in. Hg
        public const float MAIN_EJECTOR_RAMPUP_MIN = 8f;              // Main ejectors to design

        // --- Heat Load ---
        public const float FULL_POWER_REJECTION_BTUHR = 7.5e9f;      // ~7.5 Ã— 10^9 BTU/hr
        public const float STEAM_DUMP_MAX_FLOW_LBHR = 6_400_000f;    // 40% rated steam flow
        public const float STEAM_DUMP_SINGLE_VALVE_LBHR = 895_000f;  // Per valve at 1,106 psia
        public const int STEAM_DUMP_VALVE_COUNT = 12;                  // 4 groups Ã— 3

        // --- Hotwell ---
        public const float HOTWELL_NORMAL_LEVEL_IN = 24f;
        public const float HOTWELL_REJECT_OPEN_IN = 28f;
        public const float HOTWELL_REJECT_FULL_IN = 40f;
        public const float HOTWELL_MAKEUP_OPEN_IN = 21f;
        public const float HOTWELL_MAKEUP_FULL_IN = 8f;

        // --- Atmospheric Conversion ---
        public const float INHG_PER_PSIA = 2.036f;                    // Conversion factor
        public const float ATM_PRESSURE_INHG = 29.92f;                // Standard atmosphere
    }

    public static class Feedwater
    {
        // --- CST ---
        public const float CST_TOTAL_CAPACITY_GAL = 450_000f;
        public const float CST_TECH_SPEC_MIN_GAL = 239_000f;
        public const float CST_UNUSABLE_GAL = 27_700f;
        public const float CST_WATER_DENSITY_LB_GAL = 8.34f;          // ~60Â°F water

        // --- Condensate Pumps ---
        public const int CONDENSATE_PUMP_COUNT = 2;
        public const float CONDENSATE_PUMP_FLOW_GPM = 11_000f;        // Each, 70% capacity
        public const float CONDENSATE_PUMP_HEAD_PSI = 477f;
        public const float CONDENSATE_RECIRC_LOW_GPM = 3_500f;        // Recirc opens below
        public const float CONDENSATE_RECIRC_HIGH_GPM = 7_000f;       // Recirc closes above

        // --- Main Feedwater Pumps ---
        public const int MFP_COUNT = 2;
        public const float MFP_FLOW_GPM = 19_800f;                    // Each
        public const float MFP_DISCHARGE_HEAD_FT = 2_020f;
        public const float MFP_LOW_SUCTION_TRIP_PSIG = 195f;
        public const float MFP_HIGH_DISCHARGE_TRIP_PSIG = 1_850f;
        public const float MFP_OVERSPEED_RPM = 5_850f;

        // --- Auxiliary Feedwater ---
        public const int AFW_MOTOR_PUMP_COUNT = 2;
        public const float AFW_MOTOR_FLOW_GPM = 440f;                 // Each
        public const float AFW_MOTOR_DISCHARGE_PSIG = 1_300f;

        public const float AFW_TURBINE_FLOW_GPM = 880f;               // Single pump
        public const float AFW_TURBINE_DISCHARGE_PSIG = 1_200f;
        public const float AFW_TURBINE_STEAM_MIN_PSIG = 100f;         // Min steam supply
        public const float AFW_TURBINE_STEAM_MAX_PSIG = 1_275f;       // Max steam supply

        public const float AFW_SYSTEM_DESIGN_PRESSURE_PSIG = 1_650f;
        public const float AFW_START_DELAY_SEC = 60f;                  // Rated flow within 1 min

        // --- Startup AFW Pump ---
        public const float STARTUP_AFW_FLOW_GPM = 1_020f;             // Including 140 gpm recirc
        public const float STARTUP_AFW_HEAD_PSI = 1_472f;

        // --- Feedwater Temperatures ---
        public const float FW_TEMP_CST_F = 100f;                      // CST water
        public const float FW_TEMP_HOTWELL_F = 120f;                  // Hotwell
        public const float FW_TEMP_AFTER_LP_HEATERS_F = 360f;         // After 5 LP stages
        public const float FW_TEMP_AFTER_HP_HEATERS_F = 440f;         // After 2 HP stages (SG inlet)

        // --- SG Level Control ---
        public const float SG_HIGH_LEVEL_TRIP_PCT = 69.0f;            // FW isolation
        public const float SG_NORMAL_LEVEL_PCT = 33.0f;               // NR startup target

        // --- Water Properties ---
        public const float WATER_DENSITY_LB_PER_GAL = 8.34f;          // Approximate
        public const float WATER_CP_BTU_LB_F = 1.0f;                  // Specific heat
    }
}
```

---

## 7) Integration Contract â€” HeatupSimEngine

### 7.1 New State Fields on HeatupSimEngine

```csharp
// Condenser
private CondenserState condenserState;
public float condenserVacuum_inHg;          // Display field
public float condenserBackpressure_psia;    // Display field
public bool c9_CondenserAvailable;          // Display field
public int cwPumpsRunning;                  // Display field
public string condenserStatus;              // Display field

// Feedwater
private FeedwaterState feedwaterState;
public float hotwellLevel_in;              // Display field
public float cstVolume_gal;                // Display field
public float feedwaterReturnFlow_lbhr;     // Display field
public bool feedwaterAvailable;            // Display field
public float afwFlow_gpm;                  // Display field
public string feedwaterStatus;             // Display field

// Permissives
private PermissiveState permissiveState;
public bool c9_satisfied;                  // Display field
public bool p12_blocking;                  // Display field
public bool p12_bypassed;                  // Display field
public bool steamDumpPermitted;            // Display field
public string dumpBridgeState;             // Display field
public string permissiveStatus;            // Display field
```

### 7.2 Initialization (HeatupSimEngine.Init.cs)

In `InitializeColdShutdown()`:
```
condenserState = CondenserPhysics.Initialize();    // Atmospheric, no vacuum
feedwaterState = FeedwaterSystem.Initialize(CST_TOTAL_CAPACITY_GAL);
permissiveState = StartupPermissives.Initialize(); // All blocked
```

### 7.3 Step Loop Insertion (HeatupSimEngine.cs â€” StepSimulation)

The condenser/feedwater/permissive updates insert into the existing step loop after the SG update and before the HZP systems update:

```
Existing Step Order:
  1B  â€” Heater control
  1B  â€” Spray
  RCP â€” Heat smoothing
  1C  â€” RHR update
  Physics dispatch (Regime 1/2/3)
    â†’ SGMultiNodeThermal.Update() [within each regime]

NEW INSERTION POINT (after physics dispatch, before CVCS):
  â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
  â•‘ 2A â€” Condenser Update                                   â•‘
  â•‘   CondenserPhysics.Update(ref condenserState,            â•‘
  â•‘     steamLoad_BTUhr, dt_hr)                              â•‘
  â•‘                                                          â•‘
  â•‘ 2B â€” Feedwater Update                                    â•‘
  â•‘   FeedwaterSystem.Update(ref feedwaterState,             â•‘
  â•‘     steamToCondenser_lbhr, steamToAtm_lbhr,              â•‘
  â•‘     sgDemand_lbhr, hotwellTemp_F, dt_hr)                 â•‘
  â•‘                                                          â•‘
  â•‘ 2C â€” Permissive Evaluation                               â•‘
  â•‘   StartupPermissives.Evaluate(ref permissiveState,       â•‘
  â•‘     condenserState, T_avg,                               â•‘
  â•‘     steamDumpModeSelected, steamPressure_psig,           â•‘
  â•‘     dumpSetpoint_psig, deadband_psi)                     â•‘
  â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

  CVCS update

  HZP systems update (MODIFIED):
    â†’ SteamDumpController now gated by permissiveState.SteamDumpPermitted

  Logging/alarms
  Display field sync (EXTENDED with condenser/FW/permissive fields)
```

### 7.4 SteamDumpController Modification

The existing `ShouldAutoEnable()` must be extended:

```csharp
// CURRENT (line 459):
public static bool ShouldAutoEnable(float T_avg, float steamPressure_psig)
{
    bool tempOK = T_avg >= PlantConstants.SteamDump.HZP_APPROACH_TEMP_F;
    bool pressureOK = steamPressure_psig >= PlantConstants.SteamDump.STEAM_DUMP_MIN_PRESSURE_PSIG;
    return tempOK && pressureOK;
}

// MODIFIED:
public static bool ShouldAutoEnable(float T_avg, float steamPressure_psig,
                                     in PermissiveState permissives)
{
    bool tempOK = T_avg >= PlantConstants.SteamDump.HZP_APPROACH_TEMP_F;
    bool pressureOK = steamPressure_psig >= PlantConstants.SteamDump.STEAM_DUMP_MIN_PRESSURE_PSIG;
    bool permitted = permissives.SteamDumpPermitted;  // C-9 AND !P-12_blocking
    return tempOK && pressureOK && permitted;
}
```

The `Update()` method must also respect the permissive gate:
```csharp
// In Update(): if !permitted, force demand to 0 and close valve
if (!permissives.SteamDumpPermitted)
{
    state.DumpDemand = 0f;
    // Valve dynamics still apply (valve closes at stroke rate)
}
```

### 7.5 SGMultiNodeThermal Integration

The SG module needs to know whether steam has an exit path (condenser sink available). This affects the Boiling regime mass balance:

```csharp
// In SGMultiNodeThermal.Update():
// When Boiling regime and condenser sink NOT available:
//   Steam produced accumulates in SG/steam lines â†’ pressure rises from inventory
//   (existing InventoryDerived pressure source mode)
//
// When Boiling regime and condenser sink available:
//   Steam exits to condenser via dump valves (controlled by SteamDumpController)
//   Pressure tracks P_sat(T_hottest_node) â€” Saturation pressure source mode
//
// The current model already has SteamIsolated/PressureSourceMode logic.
// Integration: pass condenserSinkAvailable as additional input to influence
// the OPEN boundary steam exit calculation.
```

### 7.6 Condenser Vacuum Startup Trigger

The condenser vacuum pulldown is triggered by a multi-path startup policy to avoid circular dependency on a single temperature threshold.

```
In StepSimulation(), start condenser vacuum pulldown when any trigger is true:
  1) Startup state reaches Mode 4 (Mode 5 -> Mode 4 transition)
  2) All required RCPs are running
  3) SG steam onset is observed (boiling/saturation branch active)
  4) Fallback: T_rcs >= CONDENSER_PREP_TEMP_F (~325F)

Action:
  -> CondenserPhysics.StartVacuumPulldown(ref condenserState, simTime)
  -> Start CW pump(s) automatically
  -> Begin hogging ejector sequence
  -> Log startup trigger source(s)
```

This keeps C-9 lead time before steam dump demand while preventing startup deadlock if 325F is delayed by pressure-coupled dynamics.
---

## 8) Telemetry and Logging Extension

### 8.1 Interval Log Fields (HeatupSimEngine.Logging.cs)

Add the following to each interval log entry:

```
--- CONDENSER ---
Condenser Vacuum:        {vacuum_inHg:F1} in. Hg
Backpressure:           {backpressure_psia:F2} psia
Hotwell Temperature:    {hotwellTemp_F:F1} Â°F
CW Pumps Running:       {cwPumps}/4
Air Ejectors:           {ejectorStatus}
C-9 Condenser Available: {c9}

--- FEEDWATER ---
Hotwell Level:          {hotwellLevel:F1} in.
CST Volume:             {cstVolume:F0} gal ({cstPct:F1}%)
Condensate Pumps:       {condensatePumps}/2
MFP Running:            {mfpRunning}/2
AFW Flow:               {afwFlow:F0} gpm
FW Return Flow:         {fwReturn:F0} lb/hr
FW Temperature:         {fwTemp:F1} Â°F

--- STARTUP PERMISSIVES ---
C-9 Satisfied:          {c9}
P-12 Active:            {p12Active}
P-12 Bypassed:          {p12Bypass}
P-12 Blocking:          {p12Blocking}
Steam Dump Permitted:   {dumpPermitted}
Dump Bridge State:      {bridgeState}
```

### 8.2 PlantOverviewScreen Update

Replace the placeholder at line ~402:

```csharp
// CURRENT:
if (text_CondenserVacuum != null)
{
    text_CondenserVacuum.text = "---";
    text_CondenserVacuum.color = COLOR_PLACEHOLDER;
}

// MODIFIED:
if (text_CondenserVacuum != null && engine != null)
{
    float vacuum = engine.condenserVacuum_inHg;
    text_CondenserVacuum.text = $"{vacuum:F1} in. Hg";
    text_CondenserVacuum.color = vacuum >= 22f ? COLOR_NORMAL : COLOR_WARNING;
}
```

Additional display fields for the secondary panel:
- C-9 indicator (green/red)
- P-12 indicator (active/bypassed/inactive)
- Dump bridge state text
- CST level indicator
- AFW flow indicator

---

## 9) Validation Acceptance Criteria

### 9.1 CondenserPhysics Validation
| Criterion | Acceptable Range | Failure |
|-----------|-----------------|---------|
| Vacuum at design (no load, CW running) | 26-28 in. Hg | Model not reaching design |
| Vacuum with full steam dump | 22-25 in. Hg | Excessive backpressure or no response |
| C-9 assert timing | Within 15-20 min of CW+ejector start | Too fast or too slow |
| C-9 loss on CW trip | Within 30-60 seconds | Not responsive |
| Hotwell temp at design vacuum | 115-125Â°F | Inconsistent with steam tables |

### 9.2 FeedwaterSystem Validation
| Criterion | Acceptable Range | Failure |
|-----------|-----------------|---------|
| CST depletion rate (atm dump only) | Consistent with AFW draw | Mass imbalance |
| Hotwell level during steady dump | Stable near 24 in. | Level control unstable |
| AFW flow at rated | 440 gpm (motor), 880 gpm (turbine) | Wrong pump model |
| Secondary mass conservation | Net mass error < 0.1% per hour | Conservation violation |

### 9.3 StartupPermissives Validation
| Criterion | Acceptable Range | Failure |
|-----------|-----------------|---------|
| C-9 false before vacuum | Always false pre-pulldown | Premature C-9 |
| P-12 active below 553Â°F | Always active | P-12 not gating |
| P-12 bypass respected | Dumps armed when bypassed + C-9 | Bypass not wired |
| Bridge state transitions | Correct sequence per spec | Wrong FSM logic |

### 9.4 CS-0078 Re-Validation (Post-Integration)
| Criterion | Acceptable Range | Failure |
|-----------|-----------------|---------|
| SG pressure at 300Â°F RCS | 30-70 psig | Still floor-clamped |
| Approach Î”T at 300Â°F | 30-45Â°F | SG not participating |
| SG pressure at Mode 3 (350Â°F) | 100-130 psig | Pressure not tracking |
| SG pressure at 557Â°F | 1,092 psig | Not reaching setpoint |
| Steam dumps active at 557Â°F | Modulating | Dumps not gated properly |
| Time 160â†’557Â°F | 8-10 hours | Rate inconsistent |

---

## 10) Risk Assessment

### 10.1 High-Risk Areas
| Risk | Mitigation |
|------|-----------|
| Condenser vacuum dynamics interact with SG pressure response | Test condenser in isolation before integration |
| Feedwater return flow creates mass balance coupling | Conservation audit at each integration stage |
| P-12 bypass timing affects dump availability window | Parameterize bypass threshold; validate against spec |
| Existing SGMultiNodeThermal pressure source modes may conflict | Stage integration: condenser first, then FW return |

### 10.2 Interface Risks
| Interface | Risk | Mitigation |
|-----------|------|-----------|
| CondenserPhysics â†” SteamDumpController | Steam load feedback loop | Use previous-timestep values to break circularity |
| FeedwaterSystem â†” SGMultiNodeThermal | Mass balance coupling | Explicit mass ledger with conservation audit |
| StartupPermissives â†” HeatupSimEngine.HZP | Existing auto-enable logic change | Backward-compatible signature with default permissives |

### 10.3 Cross-Domain Considerations
The condenser/feedwater system is architecturally a new domain boundary. However, CS-0115 is assigned to DP-0011 (SG Secondary Physics) because the primary purpose is to provide the missing SG secondary boundary sink. If a dedicated DP for Condenser/Feedwater is established later, these modules would naturally migrate there.

No cross-domain inclusion requests are required at this time since all work falls under DP-0011/IP-0046.

---

## 11) File Summary

### New Files
| File | Purpose |
|------|---------|
| `Assets/Scripts/Physics/CondenserPhysics.cs` | Condenser vacuum dynamics, C-9 |
| `Assets/Scripts/Physics/FeedwaterSystem.cs` | Feedwater return path, hotwell, CST, AFW |
| `Assets/Scripts/Physics/StartupPermissives.cs` | C-9/P-12 evaluation, dump bridge FSM |
| `Assets/Scripts/Physics/PlantConstants.Condenser.cs` | Condenser + feedwater constants |

### Modified Files
| File | Changes |
|------|---------|
| `Assets/Scripts/Physics/SteamDumpController.cs` | ShouldAutoEnable + Update gated by permissives |
| `Assets/Scripts/Validation/HeatupSimEngine.cs` | State fields, step-loop insertion |
| `Assets/Scripts/Validation/HeatupSimEngine.Init.cs` | Initialize condenser/FW/permissive state |
| `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs` | Telemetry fields |
| `Assets/Scripts/Validation/HeatupSimEngine.HZP.cs` | Permissive gating in HZP |
| `Assets/Scripts/Physics/SGMultiNodeThermal.cs` | Condenser sink availability input |
| `Assets/Scripts/UI/PlantOverviewScreen.cs` | Live condenser vacuum display |

