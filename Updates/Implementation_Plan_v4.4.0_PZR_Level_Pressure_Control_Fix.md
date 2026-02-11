# Implementation Plan v4.4.0 — Pressurizer Level/Pressure Runaway Fix

**Version:** 4.4.0  
**Date:** 2026-02-11  
**Status:** COMPLETE  
**Priority:** CRITICAL — Simulation non-functional beyond ~14 hours sim time  
**Analysis Document:** `Technical_Documentation/PZR_Level_Pressure_Deficit_Analysis_v4.4.0.md`

---

## Problem Summary

The heatup simulation experiences pressurizer overfill (97.2%), pressure runaway (920 psi/hr at 2432 psia), and steam bubble elimination (50 ft³) after ~14 hours of sim time. At T+17:47, heaters are still on at 20% despite pressure being 200+ psi above the operating target. The root cause analysis compared our simulator against NRC HRTD Sections 4.1, 10.2, 10.3, and 19.0 and identified three engineering deficits, plus a missing subsystem:

1. **CoupledThermo solver ignores CVCS mass drain** — solves for PZR level using stale total mass, making thermal expansion appear unconstrained
2. **Only one letdown orifice modeled** — real plant has 3 orifices (75+75+45 gpm), operator opens more during heatup. Our letdown is 30-50% too low during mid-heatup
3. **No heater mode transition** — `PRESSURIZE_AUTO` with 20% minimum floor runs forever, never hands off to `AUTOMATIC_PID` which would de-energize heaters above 2250 psig
4. **No pressurizer spray system** — real plant's primary overpressure control mechanism above 2260 psig is completely absent. Without spray, there is no active pressure reduction capability once heaters are de-energized.

---

## Expected Realistic Behavior (per NRC HRTD 19.0)

During heatup from 350°F to 557°F with 4 RCPs running:

1. PZR level follows the heatup level program, controlled by CVCS charging modulation via FCV-121
2. As pressure rises and orifice flow increases, the operator closes orifices as needed to keep letdown ≤120 gpm (ion exchanger limit)
3. Approximately 30,000 gallons of excess RCS volume is diverted to holdup tanks through normal VCT→BRS path
4. When pressure reaches 2235 psig, heaters and spray are placed in automatic (master pressure PID controller)
5. The PID controller maintains 2235 psig by modulating proportional heaters (2220-2250 psig band) and spray (2260-2310 psig band)
6. Spray condenses PZR steam when pressure rises above 2260 psig, directly reducing pressure
7. Pressure rate during heatup is moderate (controlled by steam cushion compressibility and spray response)

---

## Proposed Fix — 5 Stages

### Stage 1: CVCS Mass Drain Integration into CoupledThermo Solver

**Root cause addressed:** Deficit #1 — solver doesn't know about CVCS draining mass from the primary system.

**Files modified:** `HeatupSimEngine.cs` (Regime 3 section)

**What changes:**

The Regime 3 simulation loop currently does:
1. Calculate CVCS flows (charging, letdown, seal)
2. Call `CoupledThermo.SolveEquilibrium()` with current mass
3. Call `UpdateRCSInventory()` to adjust mass for CVCS flows

This ordering means the solver always sees the FULL mass (before CVCS drain), then the drain is applied AFTER — but the solver already set the PZR level based on too-much mass.

**Fix:** Reverse the order — apply the net CVCS mass change to `physicsState.RCSWaterMass` BEFORE calling the solver, and guard `UpdateRCSInventory()` against double-counting.

Specifically:
```
// BEFORE solver (new):
float netCVCS_gpm = chargingToRCS - letdownFlow;  // negative = draining
float netCVCS_lbm = netCVCS_gpm * waterDensity * dt_hr * 60.0 / 7.48;
physicsState.RCSWaterMass += netCVCS_lbm;

// Solver now sees correct total mass:
CoupledThermo.SolveEquilibrium(ref physicsState, ...);

// AFTER solver: Remove the manual mass adjustment from UpdateRCSInventory()
// since it's already been applied above
```

**Physics justification:** NRC HRTD 4.1: "A flow balance diagram of the CVCS is provided in Figure 4.1-2. Normally operators establish a letdown flow of 75 gpm. The pressurizer level control system (Chapter 10.3) balances this rate of coolant removal from the RCS by maintaining a charging pump discharge flow of 87 gpm." The CVCS flows and the thermodynamic state are part of the same physical system — they must be coupled, not sequential.

**Validation:** After this stage, PZR level should track closer to setpoint because the solver sees less total mass when the CVCS is draining. The improvement may be partial — orifice flow is still low with one orifice.

---

### Stage 2: Letdown Orifice Lineup Management

**Root cause addressed:** Deficit #2 — only one orifice modeled, real plant uses 2-3 during heatup.

**Files modified:** `HeatupSimEngine.cs` (or `HeatupSimEngine.CVCS.cs`), `PlantConstants.CVCS.cs`

**What changes:**

Add orifice lineup tracking and automatic management per NRC HRTD 4.1 and 19.0:

**New state variable:** `int orificesOpen` (1-3), tracked in `HeatupSimEngine`

**New constants in `PlantConstants.CVCS.cs`:**
```csharp
/// <summary>
/// Number of 75-gpm letdown orifices available.
/// Per NRC HRTD 4.1: "Two of these orifices pass 75 gpm each."
/// </summary>
public const int LETDOWN_ORIFICE_75GPM_COUNT = 2;

/// <summary>
/// 45-gpm orifice capacity at design pressure (2235 psig).
/// Per NRC HRTD 4.1: "The third orifice is rated at 45 gpm."
/// </summary>
public const float LETDOWN_ORIFICE_45GPM_FLOW = 45f;

/// <summary>
/// 45-gpm orifice flow coefficient: K = 45/sqrt(1895) = 1.034
/// </summary>
public const float ORIFICE_FLOW_COEFF_45 = 1.034f;

/// <summary>
/// Maximum letdown flow through ion exchangers (administrative limit).
/// Per NRC HRTD 4.1: "Ion exchanger flow limitations (127 gpm maximum)"
/// Per NRC HRTD 19.0 App B: "maintain letdown flow at a maximum of 120 gpm"
/// </summary>
public const float LETDOWN_ION_EXCHANGER_MAX_GPM = 120f;

/// <summary>
/// PZR level error threshold to open additional orifice (%).
/// Simulates operator action to increase letdown when level trending high.
/// </summary>
public const float ORIFICE_OPEN_LEVEL_ERROR = 5.0f;

/// <summary>
/// PZR level error threshold to close additional orifice (%).
/// Simulates operator action to reduce letdown when level controlled.
/// </summary>
public const float ORIFICE_CLOSE_LEVEL_ERROR = 2.0f;
```

**Orifice management logic (in HeatupSimEngine, each timestep after RHR isolation):**
```
// Open 45-gpm orifice if PZR level is > setpoint + 5%
if (pzrLevel > levelSetpoint + ORIFICE_OPEN_LEVEL_ERROR && orificesOpen == 1)
    orificesOpen = 2;  // Add 45-gpm orifice (0.6× flow factor)

// Open second 75-gpm orifice if level still rising with 2 open
if (pzrLevel > levelSetpoint + ORIFICE_OPEN_LEVEL_ERROR + 5 && orificesOpen == 2)
    orificesOpen = 3;

// Close orifices back down when level is within band
if (pzrLevel < levelSetpoint + ORIFICE_CLOSE_LEVEL_ERROR && orificesOpen > 1)
    orificesOpen--;

// Enforce 120 gpm ion exchanger limit
letdownFlow = min(CalculateMultiOrificeFlow(pressure, orificesOpen), 120);
```

**Update `CalculateTotalLetdownFlow`** to accept and use orifice count, computing flow for mixed orifice types (75+45 gpm sizing).

**Physics justification:** NRC HRTD 4.1: "If extra purification flow is desired, or additional letdown flow for boron concentration changes is required, the 45-gpm orifice may be placed in service." NRC HRTD 19.0 Appendix B: "closing the letdown orifice isolation valves as necessary to maintain letdown flow below the allowed maximum of 120 gpm." The orifice lineup changes during heatup are explicit operating procedures.

**Validation:** With 2 orifices open during mid-heatup, letdown should approximately double at intermediate pressures, giving the CVCS much more drain authority. Combined with Stage 1 (solver sees the drain), PZR level should track setpoint.

---

### Stage 3: Heater Mode Transition to AUTOMATIC_PID

**Root cause addressed:** Deficit #3 — PRESSURIZE_AUTO with 20% minimum floor never transitions to proper pressure PID control.

**Files modified:** `HeatupSimEngine.cs` (Section 1B heater control), `PlantConstants.Pressurizer.cs`

**What changes:**

Add a pressure-based mode transition that simulates the operator placing heaters in automatic per NRC HRTD 19.0:

**New constant:**
```csharp
/// <summary>
/// Pressure at which heaters transition from PRESSURIZE_AUTO to AUTOMATIC_PID.
/// Per NRC HRTD 19.0: "The pressurizer heaters and sprays are placed in 
/// automatic control when the pressure reaches the normal operating value 
/// of 2235 psig."
/// Transition slightly below to allow PID to stabilize.
/// </summary>
public const float HEATER_MODE_TRANSITION_PRESSURE_PSIA = 2200f; // ~2185 psig
```

**Transition logic (in HeatupSimEngine, heater control section):**
```
if (currentHeaterMode == HeaterMode.PRESSURIZE_AUTO 
    && pressure_psia >= HEATER_MODE_TRANSITION_PRESSURE_PSIA)
{
    currentHeaterMode = HeaterMode.AUTOMATIC_PID;
    heaterPIDState = CVCSController.InitializeHeaterPID(pressure_psia - 14.7f);
    heaterPIDActive = true;
    // Log: "Heater mode transition: PRESSURIZE_AUTO → AUTOMATIC_PID at P={pressure}"
}
```

When `AUTOMATIC_PID` is active, call `CVCSController.UpdateHeaterPID()` instead of `CalculateHeaterState()` with `PRESSURIZE_AUTO` mode. The existing PID controller has:
- Proportional heaters: full on at 2220 psig, full off at 2250 psig
- Backup heaters: on below 2210 psig, off above 2217 psig
- High-pressure cutoff: all heaters off above `HEATER_PROP_CUTOFF_PSIG`

**Physics justification:** NRC HRTD 10.2: "The pressurizer pressure control system maintains reactor coolant system pressure within a narrow band around an operator selectable setpoint. The setpoint span is 1700 psig to 2500 psig, and the setpoint is normally set to 2235 psig." This system is only active AFTER the operator transfers to automatic — our simulator needs the transition trigger.

**Validation:** Once active, the PID should de-energize heaters when pressure is above ~2250 psig, preventing the 20% floor problem. Combined with Stages 1-2 (level/pressure under control), the PID should maintain stable pressure near 2235 psig.

---

### Stage 4: Pressurizer Spray System

**Root cause addressed:** Deficit #4 (from analysis) — no active pressure reduction mechanism above 2235 psig. Completes the master pressure controller triad (heaters / spray / PID coordinator).

**Files modified:** `CVCSController.cs` (new spray calculation methods), `PlantConstants.Pressurizer.cs` (new constants), `HeatupSimEngine.cs` (spray integration into Regime 3 loop), `CoupledThermo.cs` (spray mass/energy coupling)

**What this implements:**

Per NRC HRTD 10.2, the pressurizer spray system consists of:
- Two spray valves fed from cold legs (Loops B and C)
- Modulated by the master pressure controller output
- Open linearly from 2260 psig (start) to 2310 psig (full open)
- Continuous bypass flow of ~1.5 gpm for thermal stratification prevention
- Maximum combined spray flow of ~800 gpm (design, actual varies with ΔP)
- Spray water at cold-leg temperature (~558°F at no-load) condenses PZR steam

**4A. New Constants in `PlantConstants.Pressurizer.cs`:**

Most spray constants already exist. Add/verify these:
```csharp
/// <summary>
/// Spray valve fully open flow rate at rated ΔP in gpm.
/// Per NRC HRTD 10.2: Both spray valves fully open at max capacity.
/// Realistic full-open flow ~400-800 gpm depending on ΔP.
/// During heatup, ΔP is lower, flow is ~200-400 gpm.
/// Use 600 gpm as representative full-flow value at normal conditions.
/// </summary>
public const float SPRAY_FULL_OPEN_FLOW_GPM = 600f;

/// <summary>
/// Spray valve position time constant in hours.
/// Models valve travel time from closed to fully open (~30 seconds).
/// </summary>
public const float SPRAY_VALVE_TAU_HR = 30f / 3600f;  // 30 seconds

/// <summary>
/// Spray condensation effectiveness factor.
/// Fraction of spray enthalpy that goes to condensing steam vs. mixing.
/// Per thermodynamic analysis: ~0.85 for direct-contact condensation.
/// Already defined: SPRAY_EFFICIENCY = 0.85f
/// </summary>

/// <summary>
/// Spray bypass flow in gpm. Already defined: SPRAY_BYPASS_FLOW_GPM = 1.5f
/// Per NRC HRTD 10.2: "approximately one gpm" continuous bypass.
/// </summary>
```

**4B. New Spray Calculation Method in `CVCSController.cs`:**

```csharp
/// <summary>
/// Pressurizer Spray Controller State.
/// Persists between timesteps to track valve position dynamics.
/// </summary>
public struct SprayControlState
{
    public float ValvePosition;       // 0.0 (closed) to 1.0 (full open)
    public float SprayFlow_GPM;       // Actual spray flow rate (gpm)
    public float SteamCondensed_lbm;  // Steam condensed this timestep (lbm)
    public float HeatRemoved_BTU;     // Heat removed by spray this timestep (BTU)
    public bool IsActive;             // True if spray system enabled
    public string StatusMessage;      // Human-readable status
}
```

**Spray valve position logic (per NRC HRTD 10.2):**
```
// Spray demand from pressure error (linear between start and full-open setpoints)
float pressure_psig = pressure_psia - 14.7f;
float sprayDemand = 0;
if (pressure_psig > P_SPRAY_START_PSIG)  // 2260 psig
{
    sprayDemand = (pressure_psig - P_SPRAY_START_PSIG) 
                / (P_SPRAY_FULL_PSIG - P_SPRAY_START_PSIG);  // 2310 psig
    sprayDemand = clamp(sprayDemand, 0, 1);
}

// Rate-limit valve travel (first-order lag, ~30 sec time constant)
float alpha = dt_hr / (SPRAY_VALVE_TAU_HR + dt_hr);
valvePosition += alpha * (sprayDemand - valvePosition);

// Spray flow = bypass + valve-position × full-open capacity
sprayFlow = SPRAY_BYPASS_FLOW_GPM + valvePosition * SPRAY_FULL_OPEN_FLOW_GPM;
```

**4C. Spray Thermodynamic Effect (in HeatupSimEngine Regime 3 loop):**

Spray water enters the PZR steam space at cold-leg temperature (T_cold) and condenses steam. The physics:

```
// Spray mass entering PZR this timestep
float sprayMass_lbm = sprayFlow_GPM * waterDensity_at_Tcold * dt_hr * 60.0 / 7.48;

// Energy balance: spray water absorbs latent heat from steam
// Q_absorbed = m_spray × Cp × (T_sat - T_spray) × efficiency
float T_sat = SteamTable.GetSaturationTemp(pressure_psia);
float T_spray = T_cold;  // Cold leg temperature
float deltaT = T_sat - T_spray;

// Limit spray if deltaT exceeds thermal shock limit (320°F)
if (deltaT > MAX_PZR_SPRAY_DELTA_T)
    // Log warning but continue — real plant would alarm

float Q_absorbed_BTU = sprayMass_lbm * 1.0 * deltaT * SPRAY_EFFICIENCY;

// Steam condensed = Q_absorbed / h_fg at PZR pressure
float h_fg = SteamTable.GetLatentHeat(pressure_psia);  // BTU/lbm
float steamCondensed_lbm = Q_absorbed_BTU / h_fg;

// Apply to PZR: steam mass decreases, water mass increases
// This is EQUIVALENT to a pressure reduction
// ΔP ≈ -steamCondensed × (specific_vol_steam - specific_vol_water) / V_steam × (∂P/∂ρ)
// BUT: Simpler approach — adjust PZR water/steam volumes before solver
physicsState.PZRWaterMass += steamCondensed_lbm;
physicsState.PZRSteamMass -= steamCondensed_lbm;

// Also: spray mass comes FROM the cold legs (RCS inventory transfer to PZR)
// Net effect: RCS loop mass decreases slightly, PZR water mass increases
// The solver will recalculate equilibrium P from the new mass distribution
```

**4D. Integration with Master Pressure Controller:**

The spray valve demand is computed from the SAME master pressure controller output that drives heaters. Per NRC HRTD 10.2, the controller output is a single signal that:
- Below setpoint: increases heater output (proportional, then backup)
- Above setpoint: decreases heater output, then opens spray
- Well above setpoint: full spray + PORV opening logic

Since our `UpdateHeaterPID()` already computes a pressure error and PID output, we extend it:
```
// In UpdateHeaterPID, after computing pidOutput for heaters:
// The spray demand is the "mirror image" — positive error = below setpoint = no spray
// Negative error = above setpoint = spray needed
float sprayDemand = 0f;
if (pressure_psig > P_SPRAY_START_PSIG)
{
    sprayDemand = (pressure_psig - P_SPRAY_START_PSIG) 
                / (P_SPRAY_FULL_PSIG - P_SPRAY_START_PSIG);
    sprayDemand = Math.Clamp(sprayDemand, 0f, 1f);
}
// Return spray demand alongside heater output
```

**Physics justification:** NRC HRTD 10.2 Section 10.2.2: "As the pressure in the pressurizer increases above its normal setpoint, the master controller decreases the output of the proportional heaters. If the pressure continues to increase, the master controller output modulates the spray valves open. Opening the spray valves allows reactor coolant to flow from the RCS cold legs through the spray nozzle into the pressurizer. The relatively cool water spraying into the steam space of the pressurizer condenses some of the steam, which in turn lowers pressurizer pressure."

**Validation after Stage 4:**
- Spray should activate if pressure exceeds 2260 psig (2274.7 psia)
- Spray flow should be proportional between 2260-2310 psig
- Pressure should be controllable within ±25 psi of 2235 psig with spray active
- Bypass flow (~1.5 gpm) should be present at all times when spray system is enabled

---

### Stage 5: Validation, Mass Conservation Reconciliation, and Logging

**Files modified:** `HeatupSimEngine.cs`, logging sections

**What changes:**

1. **Verify mass conservation improves** after Stage 1 — target < 1% error throughout heatup
2. **Add orifice lineup to interval logs** — "Orifices Open: 2 (75+45 gpm)"
3. **Add heater mode and spray status to logs** — confirm transition timing and spray activation
4. **Add spray flow to interval logs** — "Spray: 0.0 gpm (valve 0%) / Spray: 45.2 gpm (valve 7.5%)"
5. **Add validation checks:**
   - PZR level within ±10% of setpoint throughout heatup (PASS/FAIL)
   - Pressure rate < 200 psi/hr during steady-state heatup (PASS/FAIL)
   - Heater mode = AUTOMATIC_PID before pressure reaches 2250 psia (PASS/FAIL)
   - Spray activates if pressure exceeds 2275 psia (PASS/FAIL)
   - Final pressure at T_avg=557°F within 2235 ± 50 psia (PASS/FAIL)
6. **Verify full heatup** reaches 557°F / 2235 psig without runaway

---

## Unaddressed Issues (with disposition)

| Issue | Disposition |
|-------|-------------|
| **PORVs (Power-Operated Relief Valves)** | Future feature. PORVs open at 2335 psig as backup overpressure protection beyond spray. Not needed if heater PID + spray controls pressure within normal band. NRC HRTD 10.2 Section 10.2.2. Tracked for future release. |
| **Excess letdown path** | Future feature. The 3-orifice model (Stage 2) provides adequate letdown authority. Excess letdown (20 gpm supplemental) adds realism but is not needed for basic level control. |
| **PCV-131 backpressure regulator adjustability** | Future feature. Fixed 340 psig is adequate for the orifice flow model. Operator adjustment is a refinement for more realistic low-pressure operations. |
| **SG secondary stagnation** | Not related to PZR/pressure issue. Tracked in Future Features v3.1.0. |
| **BRS return-to-VCT not activating** | Will re-evaluate after v4.4.0. If CVCS draining works properly, VCT levels should be more realistic, and BRS return may not be needed during heatup. |
| **Spray ΔP dependency on RCP status** | The actual spray flow depends on differential pressure between cold legs and PZR, which requires RCPs running. For now, spray flow is calculated from valve position × rated capacity. Future refinement can add ΔP modeling. |

**All future items will be added to the Future Features roadmap after implementation.**

---

## Implementation Order

1. **Stage 1** — CVCS mass drain into solver (addresses primary root cause)
2. **Stage 2** — Orifice lineup management (addresses letdown shortfall)
3. **Stage 3** — Heater mode transition (addresses 20% minimum floor)
4. **Stage 4** — Pressurizer spray system (completes master pressure controller)
5. **Stage 5** — Validation and logging

**Each stage implemented individually with stop-and-check before proceeding.**

---

## Files Modified

| File | Stage | Change Type |
|------|-------|-------------|
| `HeatupSimEngine.cs` | 1, 2, 3, 4, 5 | CVCS-before-solver, orifice mgmt, heater transition, spray integration, validation |
| `HeatupSimEngine.CVCS.cs` | 1 | Guard against double mass counting |
| `PlantConstants.CVCS.cs` | 2 | Orifice constants, flow coefficients |
| `PlantConstants.Pressurizer.cs` | 3, 4 | Heater transition pressure, spray constants |
| `CVCSController.cs` | 4 | SprayControlState struct, spray calculation methods, spray demand in PID |
| `CoupledThermo.cs` | 4 | Spray mass/energy coupling (PZR steam condensation) |

---

## Validation Criteria (Full Heatup Run)

1. PZR level follows heatup setpoint within ±10% throughout
2. Pressure rate stays below 200 psi/hr during Regime 3 heatup
3. Pressure stabilizes near 2235 psia (±50 psi) at T_avg = 557°F
4. Heater mode transitions to AUTOMATIC_PID before 2250 psia
5. Spray activates if pressure overshoots above 2275 psia
6. Mass conservation error < 1% throughout
7. No PZR overfill (level should not exceed ~70%)
8. Steam bubble maintained at >30% volume during steady heatup
9. VCT level oscillates within normal band (40-70%)
10. ~30,000 gallons total diverted to BRS over full heatup (per NRC HRTD 19.0)
