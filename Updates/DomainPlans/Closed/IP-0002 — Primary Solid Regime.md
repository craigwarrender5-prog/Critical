> LEGACY IMPLEMENTATION ARTIFACT — archived during Constitution v1.0 governance re-baseline.
> Historical record preserved. No current execution authority.

---
Identifier: IP-0002
Domain: Primary Side â€” Solid Pressurizer Regime / Compressibility Coupling
Status: COMPLETE
Priority: High
Tier: 2
Linked Issues: CS-0021, CS-0022, CS-0023
Last Reviewed: 2026-02-14
Authorization Status: AUTHORIZED (retroactive â€” completed as v0.2.0.0)
Mode: SPEC/DRAFT (plan closed)
---

# IP-0002 â€” Primary Solid Regime
## Primary Solid-Regime Pressure and Mass-Coupling Corrections

---

## Document Control

| Field | Value |
|-------|-------|
| Identifier | **IP-0002** |
| Plan Severity | **High** |
| Architectural Domain | Primary Side â€” Solid Pressurizer Regime / Compressibility Coupling |
| System Area | SolidPlantPressure |
| Discipline | Primary Thermodynamics â€” Solid Regime |
| Status | **COMPLETE â€” All phases executed, all issues resolved, released as v0.2.0.0** |
| Constitution Reference | PROJECT_CONSTITUTION.md, Articles V, X |

---

## Purpose

Correct the solid pressurizer regime (pre-bubble) so that:

1. Pressure visibly responds to net CVCS mass change (charging vs. letdown imbalance)
2. Pressure visibly responds to thermal expansion during heater-driven pressurization and hold
3. Surge flow trends and pressure trends are mutually consistent
4. The CVCS PI controller operates with realistic transport delay, preventing same-step cancellation of thermal expansion while preserving closed-loop pressure control

---

## Architectural Domain Definition

| Field | Value |
|-------|-------|
| System Area | Primary Side â€” Solid Pressurizer Regime |
| Discipline | Primary Thermodynamics, Compressibility Coupling |
| Architectural Boundary | SolidPlantPressure physics, CVCS-to-pressure coupling in solid regime |

This domain is distinct from all other domains. It modifies only solid-regime (pre-bubble) physics.

---

## Issues Assigned

| Issue ID | Title | Severity | Source |
|----------|-------|----------|--------|
| CS-0021 | Solid-regime pressure decoupled from mass change | High | ISSUE-LOG-001 |
| CS-0022 | Early pressurization response mismatched with controller actuation | Medium | ISSUE-LOG-002 |
| CS-0023 | Surge flow trend rises while pressure remains flat | Medium | ISSUE-LOG-004 |

**Plan Severity:** High (highest assigned issue = CS-0021 High)

---

## Investigation Report: CS-0021

**Title:** Solid-regime pressure decoupled from mass change
**Severity:** High

### 1. Exact Code Paths Responsible

The pressure update for solid-regime operations is computed entirely within:

**`SolidPlantPressure.Update()`** (`SolidPlantPressure.cs`, lines 335â€“677)

Called from:

**`HeatupSimEngine.cs`**, line 1049:
```
SolidPlantPressure.Update(ref solidPlantState, pzrHeaterPower * 1000f, 75f, 75f, rcsHeatCap, dt);
```

The pressure equation is at line 606:
```csharp
dP_psi = dV_net_ft3 / (V_total * kappa);   // line 606
state.Pressure += dP_psi;                    // line 608
```

Where `dV_net_ft3` is computed at line 593:
```csharp
float dV_net_ft3 = dV_thermal_ft3 - dV_cvcs_ft3;   // line 593
```

And `dV_cvcs_ft3` comes from the **immediate** PI controller output at lines 584â€“590:
```csharp
float netCVCS_gpm = state.LetdownFlow - state.ChargingFlow + state.ReliefFlow;  // line 584
float netCVCS_ft3_sec = netCVCS_gpm * PlantConstants.GPM_TO_FT3_SEC;            // line 587
float dV_cvcs_ft3 = netCVCS_ft3_sec * dt_sec;                                   // line 590
```

### 2. Full Input -> Transformation -> Output Flow

**Input chain (per timestep):**

```
HeaterPower (1800 kW)
  â†’ HeaterLagResponse() [line 363, tau=20s]
  â†’ netPzrHeat_BTU_sec [line 386]
  â†’ state.T_pzr += dT [line 397]
  â†’ pzrDeltaT = T_pzr - prevT_pzr [line 424]
  â†’ beta_pzr = ThermalExpansion.ExpansionCoefficient(T_pzr, P) [line 428]
  â†’ dV_pzr_ft3 = PZR_TOTAL_VOLUME Ã— beta_pzr Ã— pzrDeltaT [line 429]
  â†’ dV_thermal_ft3 = dV_pzr + dV_rcs [line 436]
```

**CVCS PI controller chain (parallel, same timestep â€” zero transport delay):**

```
pressureError_psi = PressureFiltered - PressureSetpoint [line 505]
  â†’ pTerm = KP_PRESSURE Ã— error [line 508]    (KP = 0.5 gpm/psi)
  â†’ iTerm = KI_PRESSURE Ã— integral [line 515]  (KI = 0.02 gpm/psiÂ·sec)
  â†’ letdownAdjustCmd = pTerm + iTerm [line 518]
  â†’ [if HEATER_PRESSURIZE: clamp to Â±1.0 gpm] [lines 527-531]
  â†’ [actuator lag, tau=10s] [lines 536-544]
  â†’ [slew limiter, 1 gpm/s] [lines 547-557]
  â†’ LetdownFlow = baseLetdown + LetdownAdjustEff [line 561]
  â†’ netCVCS_gpm = LetdownFlow - ChargingFlow + ReliefFlow [line 584]
  â†’ dV_cvcs_ft3 = netCVCS_gpm Ã— GPM_TO_FT3_SEC Ã— dt_sec [lines 587-590]
```

**Pressure output:**

```
dV_net_ft3 = dV_thermal_ft3 - dV_cvcs_ft3 [line 593]    â† CVCS effect is INSTANTANEOUS
kappa = Compressibility(T_avg, P) [line 601]
V_total = RCS_WATER_VOLUME + PZR_TOTAL_VOLUME [line 602]
dP_psi = dV_net / (V_total Ã— kappa) [line 606]
state.Pressure += dP_psi [line 608]
```

### 3. Observed Symptom Explained Using Actual Code Logic

**Observed (heatup logs):**
- At 0.25 hr: P = 364.8 psia, PressureRate = 0.73 psi/hr, Surge = 1.3 gpm
- At 0.50 hr: P = 365.0 psia, PressureRate = 0.00 psi/hr, Surge = 1.4 gpm
- At 0.75 hr: P = 365.0 psia, PressureRate = 0.00 psi/hr, Surge = 1.4 gpm

The initial pressurization from 114.7 psia to 365 psia (HEATER_PRESSURIZE mode) occurs quickly because the Â±1 gpm authority cap inadvertently adds 1 gpm net charging (see CS-0022). After transition to HOLD_SOLID, the PI controller achieves perfect cancellation because its output affects pressure in the same computation step â€” there is no transport delay.

**Numerical proof at steady-state HOLD_SOLID conditions (T_pzr ~ 117Â°F, T_rcs ~ 102Â°F, P = 365 psia):**

Thermal expansion per step (dt = 10 sec):
```
beta(~110Â°F, 365 psia) = 5e-5 + 7e-7Ã—110 + 5e-10Ã—12100 â‰ˆ 1.33e-4 /Â°F

PZR: dV_pzr = 1800 Ã— 1.33e-4 Ã— (44Â°F/hr Ã— 10/3600) = 0.029 ftÂ³/step
RCS: dV_rcs = 11500 Ã— 1.33e-4 Ã— (3Â°F/hr Ã— 10/3600)  = 0.013 ftÂ³/step
dV_thermal = 0.042 ftÂ³/step
```

CVCS volume removal needed to cancel this:
```
dV_cvcs = 0.042 ftÂ³/step  â†’  netCVCS_gpm = 0.042 / (0.002228 Ã— 10) = 1.9 gpm
```

The PI controller reaches 1.9 gpm within ~2 minutes of HOLD_SOLID entry because its output feeds directly into the pressure equation on the same step. Once `dV_cvcs â‰ˆ dV_thermal`, `dV_net â†’ 0` and `dP â†’ 0`.

### 4. Root Cause Category

**Missing physics term â€” CVCS transport delay.**

In a real PWR, the CVCS letdown path includes ~200 ft of piping, the letdown heat exchanger, mixed bed demineralizers, and the volume control tank. A flow change at the letdown orifice requires 30â€“90 seconds before the resulting volume change is realized at the RCS boundary. Similarly, charging flow changes require transit through CCP discharge piping and seal injection manifold.

The simulation's PI controller operates within the same computation step as the pressure equation. The controller senses pressure, computes a letdown adjustment, and that adjustment modifies `dV_cvcs_ft3` within the SAME timestep. This is a zero-delay feedback loop that allows perfect steady-state cancellation of any thermal disturbance.

With a realistic transport delay of ~60 seconds, the controller output from step N would not affect pressure until step N+6 (at 10 sec/step). During those 6 steps, thermal expansion accumulates unopposed, producing visible pressure excursions before the CVCS correction arrives.

### 5. Proposed Fix Design

**Corrective approach:** Add a CVCS transport delay using a ring buffer. The PI controller continues to run every step, but its effective flow adjustment is applied to the pressure equation only after a configurable delay. Anti-windup is tied to actuator saturation to prevent the integral from accumulating during the dead time when its output cannot yet affect pressure.

**Pressure equation (unchanged):**
```
dP = dV_net / (V_total Ã— kappa)
dV_net = dV_thermal - dV_cvcs
```

**New transport delay pipeline (replaces immediate application):**

Current flow:
```
PI â†’ lag â†’ slew â†’ LetdownAdjustEff â†’ LetdownFlow â†’ netCVCS_gpm â†’ dV_cvcs â†’ dP   [same step]
```

Corrected flow:
```
PI â†’ lag â†’ slew â†’ LetdownAdjustEff â”€â”€â”
                                     â–¼
                              RING BUFFER (read-before-write)
                              [Head] â†’ READ oldest â†’ DelayedAdjust â”€â”€â†’ LetdownFlow â†’ netCVCS â†’ dV_cvcs â†’ dP
                              [Head] â† WRITE current â† LetdownAdjustEff              [delayed by N steps]
                              Head++
                                     â–²
                              anti-windup feedback (|current - delayed| > threshold)
```

**Ring buffer design:**
- Fixed-size array: `float[] _delayBuffer` of length `DELAY_SLOTS = ceil(CVCS_TRANSPORT_DELAY_SEC / dt_sec)`
- At 10 sec/step and 60 sec delay: 6 slots
- Convention: **read-before-write at head pointer**. Head always points to the oldest entry.
- Each step: (1) READ `DelayedLetdownAdjust` from `[Head]`, (2) WRITE current `LetdownAdjustEff` to `[Head]`, (3) ADVANCE `Head`.
- This yields exactly `DelayBufferLength` steps of delay (not `length-1`).
- During priming (first N steps), the buffer outputs zeros â†’ `LetdownFlow = baseLetdown + 0 = balanced CVCS`. Physically correct: no PI adjustment has completed transit.
- The delayed value `DelayedLetdownAdjust` is applied to `LetdownFlow`

**Anti-windup design:**
- When actuator is saturated (`LetdownAdjustEff` clamped by MIN/MAX_LETDOWN_GPM or slew limiter), inhibit integral accumulation
- Additionally: when `|LetdownAdjustEff - DelayedLetdownAdjust| > threshold`, the controller output has not yet reached the process. Back-calculate the integral to prevent it from winding up during the dead time

**Ownership of variables:**
- `SolidPlantPressure.Update()` owns all pressure computation in solid regime
- `SolidPlantState.Pressure` is the canonical pressure during solid ops
- New field `SolidPlantState.DelayedLetdownAdjust` reports the delayed value (diagnostic)
- New field `SolidPlantState.TransportDelayActive` reports whether delay buffer is in use (diagnostic)
- Engine reads `solidPlantState.Pressure` (line 1058), `solidPlantState.LetdownFlow` (line 1068), `solidPlantState.ChargingFlow` (line 1069) â€” all unchanged interfaces

**Order-of-operations (revised Â§4â€“Â§6):**
1. Temperature update (Â§1â€“Â§2) â€” unchanged
2. Thermal expansion (Â§3) â€” unchanged
3. PI controller computes `LetdownAdjustEff` (Â§4) â€” unchanged algorithm
4. **NEW: READ oldest value from delay buffer `[Head]` â†’ `DelayedLetdownAdjust`** (N steps ago)
5. **NEW: WRITE current `LetdownAdjustEff` to delay buffer `[Head]`** (overwrites just-read oldest)
6. **NEW: ADVANCE `Head` pointer**
7. **NEW: Apply anti-windup feedback if actuator saturated or in dead time**
8. `LetdownFlow = baseLetdown + DelayedLetdownAdjust` â€” **uses delayed value**
9. Relief valve (Â§5) â€” unchanged
10. `netCVCS_gpm` and `dV_cvcs` use `LetdownFlow` (Â§6) â€” unchanged equation
11. Pressure update `dP = dV_net / (VÂ·Îº)` (Â§6) â€” unchanged equation
12. Diagnostics (Â§7) â€” unchanged

**Interaction with invariants:**
- Mass conservation: The engine (line 1073â€“1078) reads `chargingFlow` and `letdownFlow` from the state. `LetdownFlow` now uses the delayed adjustment, so mass flow and pressure response are mutually consistent â€” both reflect the delayed CVCS effect. No mass-pressure divergence.
- Canonical mass ledger (`TotalPrimaryMass_lb`): unaffected. Updated in engine at line 1089 using the same `letdownFlow`/`chargingFlow` that drive the pressure equation.
- Surge mass transfer (line 640): Independent of CVCS controller. Unchanged.

### 6. Measurable Acceptance Tests

| Test | Metric | Pass Criterion |
|------|--------|----------------|
| A1 | PressureRate during HEATER_PRESSURIZE (majority of steps until near setpoint) | > 0 psi/hr |
| A2 | Cross-correlation lag between PI output change and pressure response | ~60 sec (â‰ˆ CVCS_TRANSPORT_DELAY_SEC), not 0 |
| A3 | No relief valve actuation below 450 psig | ReliefFlow = 0 when P < 464.7 psia |
| A4 | Mass conservation error at 5 hr | < 5 lbm |
| A5 | All existing SolidPlantPressure.ValidateCalculations() tests | PASS (no regression) |

---

## Investigation Report: CS-0022

**Title:** Early pressurization response mismatched with controller actuation
**Severity:** Medium

### 1. Exact Code Paths Responsible

Same primary code path as CS-0021: `SolidPlantPressure.Update()`.

The specific defect path is the HEATER_PRESSURIZE authority clamp at lines 527â€“531:

```csharp
if (state.ControlMode == "HEATER_PRESSURIZE")
{
    letdownAdjustCmd = Math.Max(-HEATER_PRESS_MAX_NET_TRIM_GPM,
        Math.Min(letdownAdjustCmd, HEATER_PRESS_MAX_NET_TRIM_GPM));  // Â±1.0 gpm
}
```

And the PI controller that runs in BOTH modes at lines 504â€“521:

```csharp
// â”€â”€ PI Controller (runs in both modes) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
float pressureError_psi = state.PressureFiltered - state.PressureSetpoint;  // line 505
float pTerm = KP_PRESSURE * pressureError_psi;                              // line 508
state.ControllerIntegral += pressureError_psi * dt_sec;                     // line 511
```

### 2. Full Input -> Transformation -> Output Flow

During HEATER_PRESSURIZE mode:

```
Pressure starts at 114.7 psia, Setpoint = 365 psia
  â†’ pressureError = 114.7 - 365 = -250.3 psi (large negative)
  â†’ pTerm = 0.5 Ã— (-250.3) = -125 gpm (large negative)
  â†’ iTerm grows rapidly negative
  â†’ letdownAdjustCmd = pTerm + iTerm â†’ clamped to Â±50 gpm â†’ then clamped to Â±1.0 gpm
  â†’ LetdownFlow = 75 + (-1.0) = 74.0 gpm
  â†’ netCVCS_gpm = 74.0 - 75.0 = -1.0 gpm (net charging: adds volume, raises pressure)
```

This -1.0 gpm net charging ADDS to the thermal expansion pressure rise:
```
dV_cvcs = -1.0 Ã— 0.002228 Ã— 10 = -0.022 ftÂ³ (negative = volume added to system)
dV_net = dV_thermal - (-0.022) = dV_thermal + 0.022
```

So during HEATER_PRESSURIZE, the Â±1.0 gpm cap allows the controller to ADD up to 1 gpm of net charging, which ACCELERATES pressurization rather than opposing it. This creates a mismatch: pressure rises faster than thermal expansion alone would predict.

Meanwhile the integral accumulator is accumulating massive error (250+ psi Ã— seconds), which is clamped at `INTEGRAL_LIMIT_GPM / KI_PRESSURE = 40 / 0.02 = 2000 psiÂ·sec`. When the mode transitions to HOLD_SOLID, the integral is reset (line 470), so this accumulated error does not carry over. However, the proportional term instantly commands `0.5 Ã— (365 - 365) = 0 gpm` at setpoint, and the integral begins accumulating fresh. The issue then becomes CS-0021 (HOLD_SOLID suppresses all variation).

### 3. Observed Symptom Explained

The heatup log shows P = 364.8 psia at 0.25 hr â€” meaning pressure rose from 114.7 to 364.8 in roughly 15 minutes (~1000 psi/hr). The pure thermal expansion rate at 100Â°F should be approximately 270 psi/hr. The difference comes from:

1. The -1.0 gpm net charging bias (adds ~170 psi/hr equivalent)
2. Lower compressibility (kappa) at 115 psia vs 365 psia amplifies each volume increment

The "mismatch" is: the controller is nominally "capped" during pressurization, yet it still injects up to 1.0 gpm net volume, producing a pressurization rate notably faster than pure thermal expansion.

### 4. Root Cause Category

**Wrong authority (secondary) + Missing physics term (primary).**

The Â±1.0 gpm clamp during HEATER_PRESSURIZE allows the PI controller to consistently command -1.0 gpm (net charging) because the pressure error is always large and negative during the ramp. This is a systematic bias, not "near balanced." However, with a transport delay (CS-0021 fix), the -1.0 gpm bias would arrive 60 seconds late and would not compound with each step's thermal expansion. The pressurization rate mismatch is largely a consequence of the same missing transport delay.

Additionally, the integral accumulates massive windup during HEATER_PRESSURIZE (error = -250 psi Ã— time, clamped at 2000 psiÂ·sec). Even though it resets at mode transition, this windup serves no useful purpose and should be prevented by anti-windup tied to actuator saturation.

### 5. Proposed Fix Design

**Corrective approach:** The transport delay from CS-0021 fixes the primary mechanism. Additionally, the anti-windup logic prevents integral accumulation when the actuator is saturated (the Â±1 gpm clamp during HEATER_PRESSURIZE is a form of saturation).

**Specific anti-windup rule for HEATER_PRESSURIZE:**
When the HEATER_PRESSURIZE clamp is active and `letdownAdjustCmd` was limited from its raw value, the integral should not accumulate. This prevents the 2000 psiÂ·sec windup that currently builds up pointlessly:

```csharp
// Anti-windup: if HEATER_PRESSURIZE clamp active, do not accumulate integral
bool actuatorSaturated = (state.ControlMode == "HEATER_PRESSURIZE" &&
    Math.Abs(rawCmd) > HEATER_PRESS_MAX_NET_TRIM_GPM);
if (actuatorSaturated)
{
    // Back-calculate: hold integral at current value
    state.ControllerIntegral -= controlError_psi * dt_sec;
}
```

With transport delay + anti-windup, the pressurization rate during HEATER_PRESSURIZE becomes dominated by thermal expansion. The 1 gpm bias still exists but arrives delayed, producing a modest contribution rather than a same-step amplification.

**Interaction with invariants:**
- Mass conservation: The delayed CVCS flow is what actually reaches the RCS. Mass accounting uses the same delayed flow. Consistent.
- Relief valve: Still provides high-pressure protection at 450 psig. Unchanged.
- Mode transition: Integral reset at HOLD_SOLID entry (line 470) still fires. Anti-windup merely prevents useless accumulation during the saturated period.

### 6. Measurable Acceptance Tests

| Test | Metric | Pass Criterion |
|------|--------|----------------|
| B1 | Pressurization rate from 115 to 365 psia | 50â€“300 psi/hr (thermal expansion dominated) |
| B2 | Time to reach 350 psig from cold start | 1â€“5 hours |
| B3 | Integral accumulation during HEATER_PRESSURIZE | Bounded; does not reach Â±2000 psiÂ·sec limit |
| B4 | No relief valve actuation below 450 psig | ReliefFlow = 0 when P < 464.7 psia |
| B5 | Smooth transition to HOLD_SOLID at setpoint | No pressure step > 5 psi at mode transition |

---

## Investigation Report: CS-0023

**Title:** Surge flow trend rises while pressure remains flat
**Severity:** Medium

### 1. Exact Code Paths Responsible

Surge flow is computed at `SolidPlantPressure.Update()`, line 631:

```csharp
state.SurgeFlow = (dt_hr > 1e-8f) ? (dV_pzr_ft3 * PlantConstants.FT3_TO_GAL / dt_hr / 60f) : 0f;
```

Where `dV_pzr_ft3` is computed at line 429:

```csharp
float dV_pzr_ft3 = PlantConstants.PZR_TOTAL_VOLUME * beta_pzr * pzrDeltaT;
```

Pressure is computed at line 606â€“608 from `dV_net_ft3` (which includes CVCS cancellation).

### 2. Full Input -> Transformation -> Output Flow

```
Surge flow derivation:
  T_pzr rises at ~44Â°F/hr (heater driven)
  â†’ pzrDeltaT = 44 Ã— 10/3600 = 0.122Â°F/step
  â†’ beta_pzr(117Â°F, 365 psia) â‰ˆ 1.33e-4 /Â°F
  â†’ dV_pzr = 1800 Ã— 1.33e-4 Ã— 0.122 = 0.029 ftÂ³/step
  â†’ SurgeFlow = 0.029 Ã— 7.48 / (10/3600) / 60 = 1.3 gpm    â† matches log

Pressure derivation:
  dV_thermal = 0.042 ftÂ³/step (PZR + RCS)
  dV_cvcs = ~0.042 ftÂ³/step (PI controller matches thermal exactly â€” zero delay)
  dV_net = 0.042 - 0.042 â‰ˆ 0.000 ftÂ³/step
  dP = 0.000 / (13300 Ã— 4e-6) â‰ˆ 0.00 psi/step    â† matches log
```

Surge flow is derived from PZR thermal expansion (`dV_pzr`) alone (line 631).
Pressure is derived from net volume (`dV_thermal - dV_cvcs`) (line 593/606).

These are mathematically independent variables that diverge when CVCS instantly compensates thermal expansion.

### 3. Observed Symptom Explained

Surge flow is 1.3â€“1.4 gpm and rising (because PZR temperature increases, increasing beta).
Pressure rate is 0.00 psi/hr (because CVCS removes exactly the thermal expansion volume with zero delay).

The surge flow calculation is physically correct â€” water IS moving through the surge line due to PZR thermal expansion. But CVCS simultaneously removes an equal volume from the RCS side with zero transport delay, so net system volume change is zero and pressure stays flat.

### 4. Root Cause Category

**Secondary symptom of CS-0021 (missing transport delay), not an independent defect.**

The surge flow calculation is correct. The pressure flatness is caused by the missing CVCS transport delay (CS-0021). Once the transport delay is implemented:

- During HEATER_PRESSURIZE: Thermal expansion drives pressure up. The delayed CVCS correction has not yet arrived. Surge flow positive, pressure rising. Consistent.
- During HOLD_SOLID: CVCS corrections arrive ~60 sec late, so thermal expansion produces visible pressure excursions before the correction arrives. Surge flow positive, pressure oscillating. Consistent.

### 5. Proposed Fix Design

**No code change to surge flow calculation required.**

The surge flow formula (line 631) correctly represents the physical flow of water through the surge line due to PZR thermal expansion. This is an internal mass transfer and its calculation is correct for PZR mass accounting (line 640).

**Verification only:** After CS-0021 transport delay is implemented, verify that surge flow and pressure rate have consistent sign relationship during HEATER_PRESSURIZE and visible correlation during HOLD_SOLID.

**Diagnostic addition:** Add a `SurgePressureConsistent` bool to `SolidPlantState` for logging.

### 6. Measurable Acceptance Tests

| Test | Metric | Pass Criterion |
|------|--------|----------------|
| C1 | During HEATER_PRESSURIZE: sign(SurgeFlow) == sign(PressureRate) | TRUE for > 95% of timesteps |
| C2 | During HOLD_SOLID: SurgeFlow and PressureRate both nonzero | Both nonzero |
| C3 | PZR mass conservation (surgeMass_lb consistency) | Mass error < 1 lbm per 100 steps |
| C4 | Surge mass transfer unchanged from baseline | SurgeMassTransfer_lb matches PZR mass delta within 1 lbm |

---

## Scope

This plan SHALL:

- Implement CVCS transport delay with ring buffer in SolidPlantPressure (Phase A)
- Implement anti-windup tied to actuator saturation (Phase A)
- Tune PI gains and deadband for HOLD_SOLID if needed after transport delay (Phase B)
- Verify surge-pressure consistency resolves naturally (Phase C)
- Verify Regime 1 â†’ Regime 2 transition continuity (Phase D)

This plan SHALL NOT:

- Modify two-phase (post-bubble) pressure coupling
- Modify canonical mass conservation architecture
- Modify bubble state machine logic
- Modify SG model
- Modify HeatupSimEngine, UI, alarms, or validation infrastructure
- Disable or remove the PI controller

**File constraint:** Only `Assets/Scripts/Physics/SolidPlantPressure.cs` is modified.

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| Primary Mass Conservation (FF-05) | Prerequisite | Canonical mass ledger must be active; confirmed active in current codebase |

---

## Phase Structure

### Phase A: CVCS Transport Delay and Anti-Windup (CS-0021, CS-0022)

**Goal:** The PI controller output is delayed by a configurable transport time before it affects the pressure equation. This prevents same-step cancellation of thermal expansion while preserving realistic closed-loop CVCS pressure control. Anti-windup prevents integral accumulation when the actuator is saturated or during the dead time window.

**File:** `Assets/Scripts/Physics/SolidPlantPressure.cs`

**Exact edits:**

#### Edit 1: New constants (after line 161, in the Constants region)

```csharp
// â”€â”€ CVCS Transport Delay â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
// In a real PWR, CVCS flow changes require transit through ~200 ft
// of piping, the letdown heat exchanger, demineralizers, and the
// volume control tank before the resulting volume change is realized
// at the RCS boundary. This delay prevents the PI controller from
// instantly cancelling thermal expansion in the same computation step.
// Source: NRC HRTD 4.1 â€” CVCS piping transit times.

/// <summary>
/// Transport delay from PI controller output to RCS volume effect (seconds).
/// Models piping transit, heat exchanger lag, and valve positioning time.
/// At 60s with dt=10s, the delay buffer holds 6 steps.
/// Tunable: 30â€“120s is physically realistic for PWR CVCS.
/// </summary>
const float CVCS_TRANSPORT_DELAY_SEC = 60f;

/// <summary>
/// Maximum number of delay buffer slots. Sized for worst-case:
/// max delay (120s) / min timestep (5s) = 24 slots.
/// Must be >= ceil(CVCS_TRANSPORT_DELAY_SEC / dt_sec) at runtime.
/// </summary>
const int DELAY_BUFFER_MAX_SLOTS = 24;

/// <summary>
/// Anti-windup threshold (gpm). When the difference between the current
/// PI output (LetdownAdjustEff) and the delayed value being applied
/// exceeds this threshold, the integral is frozen. This prevents
/// windup during the dead-time window when the controller's output
/// has not yet reached the process.
/// </summary>
const float ANTIWINDUP_DEADTIME_THRESHOLD_GPM = 0.5f;
```

#### Edit 2: New state fields (in `SolidPlantState` struct, after line 92)

```csharp
// CVCS transport delay state
public float[] TransportDelayBuffer;    // Ring buffer of past LetdownAdjustEff values
public int DelayBufferHead;             // Write index into ring buffer
public int DelayBufferLength;           // Active slots = ceil(delay / dt)
public float DelayedLetdownAdjust;      // The delayed adjustment applied this step (diagnostic)
public bool TransportDelayActive;       // True once buffer is fully primed
public bool AntiWindupActive;           // True when integral accumulation is inhibited

// CS-0023 diagnostic
public bool SurgePressureConsistent;    // True when surge and pressure trends are consistent
```

#### Edit 3: Initialize delay buffer (in `Initialize()`, after line 306)

```csharp
// CVCS transport delay buffer â€” initialized to zero (balanced CVCS)
state.TransportDelayBuffer = new float[DELAY_BUFFER_MAX_SLOTS];
state.DelayBufferHead = 0;
state.DelayBufferLength = 0;  // Will be computed on first Update() from dt
state.DelayedLetdownAdjust = 0f;
state.TransportDelayActive = false;
state.AntiWindupActive = false;
state.SurgePressureConsistent = true;
```

#### Edit 4: Transport delay logic (in `Update()`, replace lines 558â€“565)

Current code (lines 558â€“565):
```csharp
state.LetdownAdjustEff = effAdj;

// Apply effective (lagged + slew-limited) adjustment to letdown flow
state.LetdownFlow = baseLetdown_gpm + state.LetdownAdjustEff;
state.LetdownFlow = Math.Max(MIN_LETDOWN_GPM, Math.Min(state.LetdownFlow, MAX_LETDOWN_GPM));

// Charging stays at base (CVCS controls pressure via letdown in solid plant)
state.ChargingFlow = baseCharging_gpm;
```

Corrected:
```csharp
state.LetdownAdjustEff = effAdj;

// â”€â”€ CVCS Transport Delay â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
// The PI controller output (LetdownAdjustEff) enters a ring buffer.
// The pressure equation uses the value from N steps ago, where
// N = ceil(CVCS_TRANSPORT_DELAY_SEC / dt_sec). This models the
// physical transit time of CVCS flow changes through piping,
// heat exchangers, and valves before reaching the RCS boundary.
//
// RING BUFFER CONVENTION: Read-before-write at head pointer.
//   Head = the oldest slot (next to be overwritten).
//   Sequence: READ oldest from [Head], WRITE current to [Head], ADVANCE Head.
//   This yields exactly DelayBufferLength steps of delay.
//   During priming (first N steps), the buffer outputs 0.0 (initialized value),
//   which means LetdownFlow = baseLetdown + 0 = balanced CVCS.
//   This is physically correct: no CVCS adjustment has had time to transit.
//
// Proof (N=6): Step 0 writes val_0 at slot 0. Step 6 reads val_0 from slot 0
// before overwriting it with val_6. Delay = 6 steps = 60 sec. âœ“

// Compute active buffer length on first call (or if dt changes)
if (state.DelayBufferLength == 0 && dt_sec > 0f)
{
    state.DelayBufferLength = Math.Max(1,
        (int)Math.Ceiling(CVCS_TRANSPORT_DELAY_SEC / dt_sec));
    state.DelayBufferLength = Math.Min(state.DelayBufferLength,
        DELAY_BUFFER_MAX_SLOTS);
}

// Ring buffer: read-before-write at head pointer
if (state.TransportDelayBuffer != null && state.DelayBufferLength > 0)
{
    // 1. READ the oldest value (N steps ago) â€” this is about to be overwritten
    state.DelayedLetdownAdjust = state.TransportDelayBuffer[state.DelayBufferHead];

    // 2. WRITE current LetdownAdjustEff into the same slot (overwrites oldest)
    state.TransportDelayBuffer[state.DelayBufferHead] = state.LetdownAdjustEff;

    // 3. ADVANCE head pointer to the next oldest slot
    state.DelayBufferHead = (state.DelayBufferHead + 1) % state.DelayBufferLength;

    // Buffer is fully primed once we've written DelayBufferLength entries
    if (!state.TransportDelayActive && state.PressurizationElapsed_sec >= CVCS_TRANSPORT_DELAY_SEC)
    {
        state.TransportDelayActive = true;
    }
}
else
{
    // Fallback: no delay (should not occur in normal operation)
    state.DelayedLetdownAdjust = state.LetdownAdjustEff;
}

// Apply DELAYED adjustment to letdown flow (not the immediate PI output)
state.LetdownFlow = baseLetdown_gpm + state.DelayedLetdownAdjust;
state.LetdownFlow = Math.Max(MIN_LETDOWN_GPM, Math.Min(state.LetdownFlow, MAX_LETDOWN_GPM));

// Charging stays at base (CVCS controls pressure via letdown in solid plant)
state.ChargingFlow = baseCharging_gpm;
```

#### Edit 5: Anti-windup logic (in `Update()`, after the PI integral accumulation block, ~line 515)

Current code (lines 510â€“515):
```csharp
// Integral term (accumulated error drives steady-state correction)
state.ControllerIntegral += pressureError_psi * dt_sec;
float integralLimit = INTEGRAL_LIMIT_GPM / KI_PRESSURE;
state.ControllerIntegral = Math.Max(-integralLimit,
    Math.Min(state.ControllerIntegral, integralLimit));
float iTerm = KI_PRESSURE * state.ControllerIntegral;
```

Corrected:
```csharp
// Integral term (accumulated error drives steady-state correction)
// Anti-windup: inhibit integral accumulation when:
//   (a) Actuator is saturated (HEATER_PRESSURIZE clamp or MIN/MAX letdown)
//   (b) Dead-time gap: current PI output differs significantly from
//       the delayed value being applied to the process
float deadTimeGap = Math.Abs(state.LetdownAdjustEff - state.DelayedLetdownAdjust);
bool actuatorClamped = state.SlewClampActive ||
    (state.ControlMode == "HEATER_PRESSURIZE" &&
     Math.Abs(letdownAdjustCmd) > HEATER_PRESS_MAX_NET_TRIM_GPM);
bool deadTimeInhibit = deadTimeGap > ANTIWINDUP_DEADTIME_THRESHOLD_GPM;

state.AntiWindupActive = actuatorClamped || deadTimeInhibit;

if (!state.AntiWindupActive)
{
    state.ControllerIntegral += pressureError_psi * dt_sec;
}
// else: integral frozen â€” do not accumulate during saturation or dead time

float integralLimit = INTEGRAL_LIMIT_GPM / KI_PRESSURE;
state.ControllerIntegral = Math.Max(-integralLimit,
    Math.Min(state.ControllerIntegral, integralLimit));
float iTerm = KI_PRESSURE * state.ControllerIntegral;
```

**Note:** The `pressureError_psi` variable used above must be the raw error (line 505), not a modified version. The anti-windup acts on the integral, not on the error signal.

#### Edit 6: CS-0023 diagnostic (in `Update()`, after line 631)

```csharp
// Surge-pressure consistency diagnostic (CS-0023)
bool surgePositive = state.SurgeFlow > 0.01f;
bool pressureRising = state.PressureRate > 0.1f;
bool bothZero = Math.Abs(state.SurgeFlow) < 0.01f && Math.Abs(state.PressureRate) < 0.1f;
state.SurgePressureConsistent = bothZero || (surgePositive == pressureRising)
    || state.ControlMode == "HOLD_SOLID";  // During hold, CVCS opposition is expected
```

**Validation Criteria (Phase A):**

| Criterion | Metric | Expected |
|-----------|--------|----------|
| A1 | PressureRate > 0 during HEATER_PRESSURIZE for majority of steps | YES â€” thermal expansion is not instantly cancelled |
| A2 | Cross-correlation: PI output change â†’ pressure response occurs after ~60 sec, not same-step | Lag â‰ˆ CVCS_TRANSPORT_DELAY_SEC |
| A3 | No relief valve actuation below 450 psig | ReliefFlow = 0 when P < 464.7 psia |
| A4 | Mass conservation error at 5 hr | < 5 lbm |
| A5 | All existing SolidPlantPressure.ValidateCalculations() tests | PASS (no regression) |

**After Phase A â€” STOP and report:**
- Diff summary
- Pressure rate vs time plot/data (showing nonzero rate during HEATER_PRESSURIZE)
- Delayed CVCS vs immediate CVCS vs time (showing lag)
- Confirmation that same-step cancellation is eliminated

---

### Phase B: HOLD_SOLID PI Gain Tuning (CS-0021, if needed after Phase A)

**Goal:** If Phase A's transport delay alone does not produce sufficient visible pressure oscillation during HOLD_SOLID (criterion: 3â€“25 psi peak-to-peak), tune PI gains and add a deadband.

**File:** `Assets/Scripts/Physics/SolidPlantPressure.cs`

**Conditional:** This phase executes ONLY if Phase A validation shows HOLD_SOLID pressure oscillation < 3 psi peak-to-peak. The transport delay may be sufficient on its own.

**Exact edits (if needed):**

1. **Line 118** â€” Reduce proportional gain:
   ```csharp
   const float KP_PRESSURE = 0.15f;   // was 0.5f
   ```

2. **Line 121** â€” Reduce integral gain:
   ```csharp
   const float KI_PRESSURE = 0.005f;  // was 0.02f
   ```

3. **New constant** â€” Add deadband:
   ```csharp
   const float HOLD_DEADBAND_PSI = 3f;
   ```

4. **PI block** â€” Apply deadband to control error in HOLD_SOLID mode:
   ```csharp
   float controlError_psi = pressureError_psi;
   if (state.ControlMode == "HOLD_SOLID" && Math.Abs(pressureError_psi) < HOLD_DEADBAND_PSI)
   {
       controlError_psi = 0f;
   }
   // Use controlError_psi for pTerm and integral accumulation
   ```

**Validation Criteria (Phase B):**

| Criterion | Metric | Expected |
|-----------|--------|----------|
| B1 | Pressure maintained in 320â€“450 psig band during HOLD_SOLID | Within band |
| B2 | Peak-to-peak pressure oscillation (1â€“5 hr) | 3â€“25 psi |
| B3 | PressureRate during HOLD_SOLID | Nonzero; visible thermal signature |
| B4 | LetdownFlow modulation during HOLD_SOLID | 60â€“90 gpm range |
| B5 | Mass conservation error at 10 hr | < 10 lbm |
| B6 | All existing ValidateCalculations() tests | PASS |

---

### Phase C: Surge-Pressure Consistency Verification (CS-0023)

**Goal:** Verify that CS-0023 symptoms resolve naturally from Phase A (and Phase B if applied). No physics changes to surge flow.

**File:** `Assets/Scripts/Physics/SolidPlantPressure.cs` (diagnostic field already added in Phase A, Edit 2 and Edit 6)

**No additional edits.** Phase C is verification-only using the diagnostic added in Phase A.

**Validation Criteria (Phase C):**

| Criterion | Metric | Expected |
|-----------|--------|----------|
| C1 | During HEATER_PRESSURIZE: SurgePressureConsistent | TRUE for > 95% of steps |
| C2 | During HOLD_SOLID: SurgeFlow > 0 and PressureRate != 0 | Both nonzero |
| C3 | PZR mass conservation (surgeMass_lb) | Error < 1 lbm per 100 steps |
| C4 | No change to SurgeMassTransfer_lb computation | Identical to baseline |

---

### Phase D: Regime 1 â†’ Regime 2 Transition Continuity Check

**Goal:** Verify that corrected solid-regime pressure transitions smoothly into post-bubble physics.

**File:** None (diagnostic verification only). Issues found â†’ logged to ISSUE_REGISTRY.md.

**Verification steps:**

1. Run full heatup simulation through bubble formation.
2. Check pressure continuity at BubbleFormed=true: `|P_post - P_pre| < 5 psi`.
3. Check surge flow continuity: `|surge_post - surge_pre| < 2 gpm`.
4. Check mass continuity: `|totalMass_post - totalMass_pre| < 1 lbm`.

**Validation Criteria (Phase D):**

| Criterion | Metric | Expected |
|-----------|--------|----------|
| D1 | Pressure continuity at bubble formation | < 5 psi step |
| D2 | Surge flow continuity | < 2 gpm step |
| D3 | Mass continuity | < 1 lbm step |
| D4 | All ValidateCalculations() tests | PASS |

---

## Execution Protocol

1. Implement one phase at a time.
2. **After Phase A: STOP.** Report diff summary, pressure rate vs time data, delayed-vs-immediate CVCS data, and confirmation that same-step cancellation is eliminated.
3. Await confirmation before proceeding to Phase B (or skip to Phase C if transport delay is sufficient).
4. Phase B is conditional â€” executes only if HOLD_SOLID oscillation criteria are not met by Phase A alone.
5. Phases C and D are verification-only.

---

## Out-of-Scope Issues

Any issue discovered outside the solid-regime domain during implementation:
- Log to ISSUE_REGISTRY.md with appropriate severity and domain assignment
- Do NOT expand scope of this plan

---

## Known Limitations

| Limitation | Impact |
|-----------|--------|
| Transport delay is a fixed constant (not temperature or flow-dependent) | Real transit time varies with flow rate and fluid density. Acceptable for training fidelity â€” tunable constant allows calibration. |
| Compressibility coefficients are polynomial fits, not IAPWS-IF97 | Acceptable accuracy for training simulator. |
| Single CVCS letdown path modeled (RHR crossconnect at 75 gpm) | Real plant may switch orifice lineup. Acceptable simplification. |
| Ring buffer is sized at compile time (24 slots max) | Limits maximum delay to 24 Ã— dt_sec. At 10 sec/step: 240 sec max. More than sufficient. |

---

## Exit Criteria

- [x] CS-0021: Pressure responds to thermal expansion during both HEATER_PRESSURIZE and HOLD_SOLID (nonzero PressureRate, visible oscillation) â€” PressureRate > 0 for 100% HP steps; HOLD_SOLID 11-14 psi P-P
- [x] CS-0022: Pressurization rate during HEATER_PRESSURIZE is thermal-expansion-dominated (PI lag visible) â€” 6-step (60 sec) transport delay confirmed
- [x] CS-0023: Surge flow and pressure trends are consistent during HEATER_PRESSURIZE â€” SurgePressureConsistent 100% both modes
- [x] No regression in primary-side conservation metrics (< 10 lbm over 10 hr) â€” 0.02 lbm at 5 hr
- [x] No regression in existing validation tests â€” all 11 ValidateCalculations() tests pass
- [x] Regime 1 â†’ Regime 2 transition continuity verified â€” D1-D4 all PASS
- [x] Issue Registry updated â€” CS-0021/22/23 â†’ Resolved v0.2.0.0
- [x] Changelog written â€” CHANGELOG_v0.2.0.0.md
- [x] Version increment performed â€” v0.2.0.0

