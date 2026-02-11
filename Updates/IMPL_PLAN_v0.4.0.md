# CRITICAL: Master the Atom — Proposed Implementation Plan v0.4.0

**Document:** IMPL_PLAN_v0.4.0.md
**Date:** 2026-02-07
**Applies to:** Heatup simulation (Cold Shutdown → HZP)
**Current Version:** v0.3.0
**Target Version:** v0.4.0 (Minor — backwards-compatible physics corrections)

---

## Background

Analysis of 25 heatup log files (T0.5hr through T12.5hr) from a full Cold Shutdown → HZP
simulation run revealed three interconnected defects causing unrealistic pressurizer
behaviour during the bubble formation and RCP startup phases. Two are confirmed code
defects where control outputs fail to feed back into the physics calculations. The third
is a design gap in the RCP startup model where pumps are treated as binary on/off devices
rather than modelling the real-world staged ramp-up.

All three defects share a common theme: **the simulation's control systems are disconnected
from its physics**. Heater throttling is logged but never applied. The PZR level program is
present in the codebase but the wrong function is called. RCPs appear instantly at full
capacity with no ramp-up or thermal transition period. Collectively these produce a cascade
of unrealistic transients that would not occur in an actual Westinghouse 4-Loop plant.

---

## Original Observations and Findings

Five observations were raised from the heatup log analysis. Each is answered here with a
finding and a reference to the issue that resolves it.

### Observation 1 — "After bubble forms, heaters throttle to 0.4 MW but T_PZR increases at a HIGHER rate (74–104 °F/hr). Where is the extra energy?"

**Finding:** The extra energy comes from the full, unthrottled 1.8 MW heater input. The
heater controller correctly calculates a reduced power (0.36 MW / 20%) in response to the
pressure rate exceeding 100 psi/hr, but due to execution ordering in `StepSimulation()`,
the throttled value is written *after* the physics step has already consumed 1.8 MW. The
next timestep resets to 1.8 MW before physics runs again. The logged 0.36 MW is real
controller output — it simply never reaches the physics. The "extra energy" is the full
1.44 MW difference between what physics uses (1.8) and what the display shows (0.36).

**Resolution:** Issue #1 — Heater Throttle Output Does Not Reach Physics.

---

### Observation 2 — "VCT drops after bubble forms, then exceeds 80% after RCP4 starts and continues rising"

**Finding:** Two distinct mechanisms produce the two phases of this behaviour:

**VCT drop during bubble formation (T8.0–T9.0hr):** During the DRAIN phase, letdown is
75 gpm via RHR crossconnect while charging starts at 0 gpm (CCP not yet started). The
net 75 gpm outflow from the RCS drains through letdown into the VCT, but the VCT is also
supplying makeup to the CVCS. Once the CCP starts (at PZR level < 80%), charging rises to
44 gpm, reducing net outflow to 31 gpm. The VCT initially drops as the drain begins, then
stabilises once the CCP is running. This part of the behaviour is approximately correct.

**VCT rise to 84% and DIVERT after RCPs start (T10–T12.5hr):** This is entirely caused by
the wrong level setpoint function. With 4 RCPs running and T_avg = 288 °F, the RCS is
thermally expanding and pushing water into the pressurizer through the surge line. The
correct PZR level setpoint at 288 °F is ~33.6% (heatup program), which would accommodate
this expansion. Instead, the controller uses the at-power program which clamps to 25%.
The PI controller sees PZR level at 40% (15% above a 25% setpoint) and responds by
maximising letdown and minimising charging — dumping RCS inventory into the VCT. The
VCT fills past 70%, the proportional divert valve (LCV-112A) opens, and excess water
is diverted to the Boron Recovery System. The system is fighting thermal expansion
instead of letting the programmed level curve accommodate it.

**Resolution:** Issue #2 — Wrong PZR Level Setpoint Function. With the correct unified
function, the setpoint at 288 °F would be ~33.6%, level error drops from +15% to +6.6%,
the PI controller makes minor flow adjustments instead of aggressively dumping, and the
VCT stays within the 30–70% normal operating band.

---

### Observation 3 — "After bubble forms, PZR immediately drops to 7% with slow recovery. Is this normal?"

**Finding:** A PZR level drop at RCP start is expected and normal. When the first RCP
starts, it circulates cold RCS water (~200 °F) past the surge line connection. The hot
pressurizer water (~562 °F, at saturation) is significantly less dense than the cold RCS
water. As flow develops, thermal mixing through the surge line causes a net outsurge of
hot water from the PZR into the cooler RCS, compressing the steam bubble and dropping
the liquid level. In a real Westinghouse 4-Loop plant, this transient is typically:

- **Magnitude:** 5–10% level drop
- **Duration:** 15–30 minutes
- **Recovery:** CVCS PI controller increases charging in response; level recovers as
  temperatures equilibrate

The simulation's 17% drop to 7.9% is **not normal** — it is approximately double the
expected magnitude and dangerously close to the 17% low-level isolation interlock (which
would trip all PZR heaters and isolate letdown). Three compounding factors cause this:

1. The RCP appears at 100% flow/heat instantly (no ramp-up model) — the thermal shock
   is a step function rather than a 30–45 minute gradual development
2. The physics path switches from isolated to fully-coupled in one timestep — the
   CoupledThermo solver tries to equilibrate 362 °F of ΔT at once
3. Bug #1 means the PZR arrives at RCP start hotter and at higher pressure than it
   should (heaters were never actually throttled), making the thermal transient worse
4. Bug #2 means the PI controller fights recovery (setpoint 25%) instead of supporting
   it (setpoint should be ~30% at the T_avg at RCP start)

**Resolution:** Issues #1, #2, and #3. Fixes #1 and #2 reduce the overshoot from the
input side (less stored energy, correct controller response). Fix #3 replaces the binary
RCP model with staged ramp-up, spreading the thermal transient over ~40 minutes per pump.

---

### Observation 4 — "After RCP4 active, PZR sits at 40% (setpoint 25%), letdown high, VCT high. System dumps water rather than using it to increase pressure"

**Finding:** This is the same root cause as Observation 2, viewed from the opposite end.
The system should be using the thermal expansion to raise PZR level along the programmed
curve — higher T_avg means higher setpoint means more water in the PZR means more
insurance against overpressure events. Instead, the flat 25% setpoint from the at-power
function makes the controller interpret the rising level as a fault condition. It
maximises letdown to dump water to the VCT, and minimises charging to stop feeding the
RCS. The net effect is that the ~30,000 gallons of excess coolant volume that should be
managed gradually through the programmed divert process (NRC HRTD 19.0) is instead being
forced out aggressively because the controller thinks the pressurizer is overfilling.

The reason PZR level does not reach the correct setpoint (~33.6% at 288 °F) and instead
overshoots to 40% is compounded by Bug #1: the heaters at full 1.8 MW produce excessive
thermal expansion that pushes water into the PZR faster than the (correctly-functioning)
CVCS could manage even with the right setpoint. With both fixes, thermal expansion would
be slower (heaters throttled) and the setpoint would be higher (33.6% not 25%), so the
level error would be small and manageable.

**Resolution:** Issue #2 (primary) and Issue #1 (contributing).

---

### Observation 5 — "What is the Programmed Level Curve? Do we need it? Have we modelled it? Would it help?"

**Finding:**

#### What is it?

The Programmed Level Curve (also called the PZR Level Program or Temperature-Compensated
Level Setpoint) is an automatic control function in Westinghouse 4-Loop PWRs that adjusts
the pressurizer level setpoint as a function of average coolant temperature (T_avg). Its
purpose is to accommodate the large change in RCS water volume that occurs as the plant
heats from cold shutdown (~200 °F) to full power (~585 °F).

Water expands significantly over this temperature range. If the PZR level setpoint were
fixed, the expanding coolant would either overfill the pressurizer (going solid — losing
the steam bubble and all pressure control) or the CVCS would dump massive amounts of
water to prevent overfill (wasting inventory and stressing the VCT/BRS systems). Neither
is acceptable.

The solution is a setpoint that *rises with temperature*, creating room in the pressurizer
for the expanding coolant. As T_avg increases, the setpoint increases, and the CVCS
controller allows the rising level rather than fighting it.

There are actually **two complementary programs** that cover different operating regimes:

**Heatup Program (200–557 °F):**
```
Setpoint = 25% + (T_avg - 200) / (557 - 200) × (60% - 25%)
         = 25%  at  200 °F  (cold shutdown)
         = 33.6% at 288 °F
         = 44.3% at 400 °F  (Mode 4 / Hot Shutdown)
         = 60%  at  557 °F  (Mode 3 / Hot Standby entry)
```
Source: NRC HRTD 4.1, Westinghouse FSAR Chapter 7. This curve governs level control
from cold shutdown through Mode 3 entry. It is a linear ramp that tracks thermal
expansion. Without it, operators would have to manually adjust the level setpoint
throughout heatup.

**At-Power Program (557–584.7 °F):**
```
Setpoint = 25% + 1.318 × (T_avg - 557)
         = 25%   at 557 °F  (no-load, 0% power)
         = 61.5% at 584.7 °F (full load, 100% power)
```
Source: NRC HRTD 10.3, Figure 10.3-2. This curve governs level control during power
operations. It is driven by the auctioneered-high T_avg signal from the RCS loop RTDs.
It serves two safety purposes:
- Prevents the PZR from going solid on a turbine trip (T_avg drops → setpoint drops
  → controller drains water from PZR → maintains steam bubble)
- Prevents a high-level trip on a 50% load rejection (same mechanism)

The two programs are **complementary**, not overlapping. The heatup program hands off to
the at-power program at 557 °F (Mode 3 entry / no-load hot standby).

#### Do we need it?

**Yes.** Without the programmed level curve, there is no mechanism to accommodate the
~30,000 gallons of excess coolant volume that thermal expansion produces during a
cold-to-hot heatup (NRC HRTD 19.0). The consequences of not having it are exactly
what the simulation logs show: the CVCS fights thermal expansion, the VCT overflows,
the divert valve activates continuously, and the pressurizer level deviates wildly
from setpoint. In a real plant, an operator would manually adjust the setpoint during
heatup if the automatic program failed — but the automatic program is standard equipment
on all Westinghouse 4-Loop plants.

#### Have we modelled it?

**Yes — both programs are fully implemented** in `PlantConstants.Pressurizer.cs`:

| Function | Range | Purpose | Status |
|----------|-------|---------|--------|
| `GetPZRLevelSetpoint(T_avg)` | 200–557 °F | Heatup program only | ✅ Implemented, correct |
| `GetPZRLevelProgram(T_avg)` | 557–584.7 °F | At-power program only | ✅ Implemented, correct |
| `GetPZRLevelSetpointUnified(T_avg)` | 200–584.7 °F | Full range (delegates to appropriate sub-function) | ✅ Implemented, correct |

All three functions produce correct values. The heatup constants (`PZR_LEVEL_COLD_PERCENT`
= 25%, `PZR_LEVEL_HOT_PERCENT` = 60%, `PZR_HEATUP_LEVEL_T_LOW` = 200 °F,
`PZR_HEATUP_LEVEL_T_HIGH` = 557 °F) are sourced from NRC HRTD 4.1. The at-power constants
(`PZR_LEVEL_PROGRAM_MIN` = 25%, `PZR_LEVEL_PROGRAM_MAX` = 61.5%, `PZR_LEVEL_PROGRAM_TAVG_LOW`
= 557 °F, `PZR_LEVEL_PROGRAM_TAVG_HIGH` = 584.7 °F) are sourced from NRC HRTD 10.3.

The problem is not the model — it is that **the wrong function is called**. Four call
sites use `GetPZRLevelProgram()` (at-power, 557–584.7 °F range) instead of
`GetPZRLevelSetpointUnified()` (full range, 200–584.7 °F). Below 557 °F — which is
the *entire heatup* — the at-power function clamps to 25%, making the heatup program
completely invisible.

#### Would it help?

**Yes — it resolves Observations 2 and 4 entirely.** Switching to the unified function
means:

| T_avg | Current Setpoint | Correct Setpoint | Effect |
|-------|-----------------|-----------------|--------|
| 200 °F | 25% | 25% | No change (cold) |
| 288 °F | 25% | 33.6% | Level error drops from +15% to +6.6% |
| 400 °F | 25% | 44.3% | Controller allows thermal expansion |
| 500 °F | 25% | 55.7% | VCT stays in normal band |
| 557 °F | 25% | 60% | Seamless handoff to at-power program |

With the correct programmed curve active, the CVCS works *with* thermal expansion rather
than against it. Charging and letdown stay approximately balanced. The VCT operates in
its normal 30–70% band. The divert valve only activates for the expected gradual excess
volume management, not for emergency inventory dumping.

**Resolution:** Issue #2 — four single-line function substitutions.

---

## Issue #1 — Heater Throttle Output Does Not Reach Physics

### Problem Behaviour

After the steam bubble forms and the DRAIN phase begins (~T8.0hr), the heater controller
correctly calculates a throttled power of 0.36 MW (20% of 1.8 MW) in response to the
pressure rate exceeding the 100 psi/hr limit. This value is logged to file and displayed
on the dashboard. However, the pressurizer temperature continues accelerating at
74–104 °F/hr — *faster* than during solid plant operations when the heaters were
legitimately at full power.

The pressure rate during DRAIN holds at 636 psi/hr (over 6× the 100 psi/hr target that
the controller is designed to enforce), and the steam generation rate in `UpdateDrainPhase()`
proceeds as though full 1.8 MW is being applied.

**Log evidence:**

| Time | Heater Power (logged) | T_pzr Rate | P Rate | Expected T_pzr Rate at 0.36 MW |
|------|-----------------------|------------|--------|-------------------------------|
| T8.5hr | 0.36 MW | 74 °F/hr | 636 psi/hr | ~15 °F/hr |
| T9.0hr | 0.36 MW | 104 °F/hr | 640 psi/hr | ~20 °F/hr |

### Expected Behaviour

When the pressure-rate feedback controller (`BUBBLE_FORMATION_AUTO` mode) reduces heater
power to 20%, the physics calculations should receive and use 0.36 MW — not 1.8 MW. This
would produce:

- PZR temperature rate of ~15–20 °F/hr during DRAIN (not 74–104)
- Pressure rate of ~50–100 psi/hr (not 636) — within the controller's target band
- Slower, more controlled steam generation matching real plant bubble formation
- DRAIN phase duration lengthening proportionally (correct — real plants take ~40 min)

Per NRC HRTD 6.1 and 19.2.2, operators throttle heaters during bubble formation
specifically to control the pressure rate. The feedback loop is the primary mechanism
for this. In a real Westinghouse plant, uncontrolled pressure rise at 636 psi/hr during
bubble formation would trigger operator intervention or automatic safety actuation.

### Root Cause

In `StepSimulation()` (HeatupSimEngine.cs), the execution order is:

```
Section 1: RCP Sequencer
  └── pzrHeaterPower = PZR_HEATER_POWER_MW;     ← RESETS to 1.8 MW every step

Section 2: Physics Calculations
  └── IsolatedHeatingStep(..., pzrHeaterPower, ...)   ← Consumes 1.8 MW
  └── UpdateDrainPhase(...)                            ← Steam generation uses 1.8 MW

Section 5: UpdateCVCSFlows()
  └── CalculateHeaterState(...)                        ← Calculates 0.36 MW
  └── pzrHeaterPower = heaterState.HeaterPower_MW;     ← Overwrites to 0.36 MW (for logging only)

Next timestep:
  └── pzrHeaterPower = PZR_HEATER_POWER_MW;     ← Reset back to 1.8 MW again
```

The heater controller runs *after* physics, so its output is only ever visible to the
logger and dashboard. The physics modules always receive the unconditional 1.8 MW reset.

### Proposed Fix

**Move heater control calculation before the physics step** so the throttled value is the
one consumed by `IsolatedHeatingStep()` and `UpdateDrainPhase()`.

**HeatupSimEngine.cs** — `StepSimulation()`:

1. Remove the hard reset `pzrHeaterPower = PZR_HEATER_POWER_MW;` from Section 1.
2. Insert a new **Section 1B: Heater Control** between the RCP sequencer (Section 1) and
   the physics calculations (Section 2). This section:
   - Determines the current PZR level setpoint (needed by heater interlock logic)
   - Calls `CVCSController.CalculateHeaterState()` with the current mode and the
     `pressureRate` from the *previous* timestep
   - Writes the result to `pzrHeaterPower` before physics uses it
   - One-step lag on pressureRate is physically reasonable (real controllers have sensor
     and signal processing delay; reactor instrumentation typically has 1–3 second lag)

**HeatupSimEngine.CVCS.cs** — `UpdateCVCSFlows()`:

3. Remove the `CalculateHeaterState()` call and its associated block from this method
   (it now runs earlier in the main step). Retain the `pzrHeatersOn` assignment, reading
   from the already-computed state.

**No changes to:** CVCSController.cs, PlantConstants, RCSHeatup.cs, or any physics module.
No API changes. No struct changes. This is a reordering within the existing engine
coordinator, consistent with GOLD G3 (engine coordinates, does not compute).

### Validation Criteria

After fix, re-run the full heatup simulation and confirm:

| Metric | Before Fix | Expected After |
|--------|-----------|----------------|
| T_pzr rate during DRAIN | 74–104 °F/hr | 15–25 °F/hr |
| Pressure rate during DRAIN | 636 psi/hr | 50–100 psi/hr |
| Logged heater power vs physics heater power | Divergent | Identical |
| DRAIN phase duration | ~30 min | ~40–60 min |

---

## Issue #2 — Wrong PZR Level Setpoint Function During Heatup

### Problem Behaviour

After all 4 RCPs are running (T12.5hr), the simulation shows:

- PZR level: 40.2%
- PZR level setpoint: 25%
- Level error: +15.2%
- CVCS response: charging minimised (32 gpm), letdown maximised (75 gpm)
- VCT level: 84.1% — DIVERT valve active, dumping inventory to the BRS
- System behaviour: CVCS fights thermal expansion, treating it as an overfill condition

The simulation is dumping water out of the RCS rather than using the natural thermal
expansion to fill the pressurizer along the programmed level curve. At T_avg = 288 °F,
the PZR level *should* be well above 25%, but the controller sees it as 15% too high
and aggressively drains.

### Expected Behaviour

Per NRC HRTD 4.1 and 10.3, Westinghouse 4-Loop PWRs use a temperature-compensated PZR
level setpoint that increases with T_avg during heatup to accommodate thermal expansion:

**Heatup Level Program (200–557 °F):**
- 25% at 200 °F → 60% at 557 °F (linear)
- Source: NRC HRTD 4.1, Westinghouse FSAR Chapter 7
- Governs CVCS level control from cold shutdown through Mode 3 entry

**At-Power Level Program (557–584.7 °F):**
- 25% at 557 °F → 61.5% at 584.7 °F (linear)
- Source: NRC HRTD Section 10.3, Figure 10.3-2
- Governs CVCS level control during power operations (Tavg auctioneered-high)

At T_avg = 288 °F, the correct heatup program setpoint is:

```
fraction = (288 - 200) / (557 - 200) = 88 / 357 = 0.246
setpoint = 25 + 0.246 × (60 - 25) = 25 + 8.6 = 33.6%
```

With the correct setpoint of ~33.6%, the level error would be +6.6% (actual 40.2% vs
setpoint 33.6%) rather than +15.2%. This is a mild positive error that the PI controller
handles gracefully with minor flow adjustments, not the aggressive inventory dumping
observed. VCT would remain in the normal 30–70% operating band.

Both level programs exist in `PlantConstants.Pressurizer.cs` and work correctly:

| Function | Range | At 288 °F | Purpose |
|----------|-------|-----------|---------|
| `GetPZRLevelProgram()` | 557–584.7 °F | **25%** (clamped min) | At-power operations only |
| `GetPZRLevelSetpoint()` | 200–557 °F | **33.6%** | Heatup operations only |
| `GetPZRLevelSetpointUnified()` | 200–584.7 °F | **33.6%** | Full range — correct for all states |

### Root Cause

Four call sites use the **at-power** function `GetPZRLevelProgram()` instead of the
**unified** function `GetPZRLevelSetpointUnified()`. Below 557 °F (the entire heatup
range), the at-power function clamps to its minimum of 25%, making the heatup level
program invisible to the controller:

| # | File | Method | Current Call (Wrong) |
|---|------|--------|---------------------|
| 1 | `HeatupSimEngine.CVCS.cs` | `UpdateCVCSFlows()` | `GetPZRLevelProgram(T_avg)` |
| 2 | `HeatupSimEngine.BubbleFormation.cs` | `UpdateDrainPhase()` — STABILIZE initialiser | `GetPZRLevelProgram(T_avg)` |
| 3 | `CVCSController.cs` | `Update()` | `GetPZRLevelProgram(T_avg)` |
| 4 | `CVCSController.cs` | `PreSeedForRCPStart()` | `GetPZRLevelProgram(T_avg)` |

### Proposed Fix

Replace all four calls with `GetPZRLevelSetpointUnified(T_avg)`. This is four single-line
substitutions. No other code changes required.

**Call site 1 — HeatupSimEngine.CVCS.cs, `UpdateCVCSFlows()`:**
```csharp
// BEFORE:
pzrLevelSetpoint = PlantConstants.GetPZRLevelProgram(T_avg);
// AFTER:
pzrLevelSetpoint = PlantConstants.GetPZRLevelSetpointUnified(T_avg);
```

**Call site 2 — HeatupSimEngine.BubbleFormation.cs, `UpdateDrainPhase()` STABILIZE init:**
```csharp
// BEFORE:
float initialSetpoint = PlantConstants.GetPZRLevelProgram(T_avg);
// AFTER:
float initialSetpoint = PlantConstants.GetPZRLevelSetpointUnified(T_avg);
```

**Call site 3 — CVCSController.cs, `Update()`:**
```csharp
// BEFORE:
state.LevelSetpoint = PlantConstants.GetPZRLevelProgram(T_avg);
// AFTER:
state.LevelSetpoint = PlantConstants.GetPZRLevelSetpointUnified(T_avg);
```

**Call site 4 — CVCSController.cs, `PreSeedForRCPStart()`:**
```csharp
// BEFORE:
state.LevelSetpoint = PlantConstants.GetPZRLevelProgram(T_avg);
// AFTER:
state.LevelSetpoint = PlantConstants.GetPZRLevelSetpointUnified(T_avg);
```

The at-power function `GetPZRLevelProgram()` remains available for future use by the
power operations simulation. It is not being deleted or modified.

### Validation Criteria

| T_avg (°F) | Before Fix (setpoint) | After Fix (setpoint) | Source |
|------------|----------------------|---------------------|--------|
| 200 | 25% | 25% | Heatup curve start |
| 288 | 25% (clamped) | 33.6% | Heatup curve interpolation |
| 400 | 25% (clamped) | 44.3% | Heatup curve interpolation |
| 500 | 25% (clamped) | 55.7% | Heatup curve interpolation |
| 557 | 25% (at-power min) | 60% | Heatup curve end / at-power transition |
| 570 | 42.1% | 42.1% | At-power curve (both functions agree) |

After fix:
- PZR level should track within ±5% of the programmed setpoint throughout heatup
- VCT level should remain within the 30–70% normal operating band
- DIVERT valve should not activate during normal heatup
- Charging and letdown flows should be approximately balanced (±10 gpm)

---

## Issue #3 — RCP Startup Model Lacks Staged Ramp-Up

### Problem Behaviour

At T9.5hr when RCP #1 starts, the simulation exhibits:

- PZR level crashes from 25% to 7.9% in 30 minutes (17% drop)
- Pressure jumps 311 psi in one interval (843 → 1154 psia)
- The system approaches the 17% low-level isolation interlock
- Recovery takes 30+ minutes before level stabilises

This occurs because the current model treats each RCP as a binary device. The moment
`rcpCount` goes from 0 to 1 in `StepSimulation()`, the engine instantly switches from
the **isolated** physics path (`IsolatedHeatingStep` — separate T_pzr and T_rcs, no
coupling) to the **coupled** physics path (`BulkHeatupStep` — `CoupledThermo.SolveEquilibrium`
— unified T, full pressure-volume equilibration). This is a step change in the physics
model, not a gradual transition.

At the moment of transition:
- T_pzr ≈ 562 °F (saturated, at PZR steam bubble conditions)
- T_rcs ≈ 200 °F (cold, RCPs were off)
- ΔT ≈ 362 °F across the surge line

The coupled solver immediately tries to equilibrate this 362 °F temperature difference
with 100% flow. The resulting mass redistribution drives a sharp pressure spike and
level collapse that would not occur in a real plant.

### Expected Behaviour

In an actual Westinghouse 4-Loop PWR, each RCP startup is a **staged process** that
develops over approximately 30–45 minutes. The pump does not deliver 100% flow and
100% heat input the instant it is energised. The progression follows distinct stages
as the motor accelerates, flow develops through the loop, and thermal mixing between
the hot pressurizer water and cold RCS water occurs gradually through the surge line.

**RCP Startup Stages (per pump):**

| Stage | Motor Speed | RCS Flow Fraction | Thermal Mixing | Duration | Notes |
|-------|-------------|-------------------|----------------|----------|-------|
| 0 — Pre-Start | 0% | 0% | None | — | NPSH verified, seal injection confirmed, breaker open |
| 1 — Initial Start | 0 → 30% | 5–10% | Minimal | 1–2 min | Motor energised (DOL start), shaft accelerating, monitor NPSH |
| 2 — Low Flow | 30 → 60% | 10–30% | Developing | 5–10 min | Flow patterns establishing, seal cooling active, second pump may start |
| 3 — Moderate Flow | 60 → 90% | 30–70% | Substantial | 10–15 min | Pressure rising from thermal expansion, pump approaching rated speed |
| 4 — Full Speed | 100% | 100% | Complete | 15–20 min | Rated conditions, full loop flow, thermal equilibrium established |

**Key point:** The full 5.25 MW heat input and 97,600 gpm flow from each RCP develop
over ~30–45 minutes, not instantaneously. This means the thermal shock to the
pressurizer is spread over time. The surge line sees gradually increasing flow, not
a step change from zero to full. The CoupledThermo solver receives a gradually
increasing heat input and flow coupling, allowing it to converge smoothly rather
than trying to equilibrate a 362 °F step in a single timestep.

The expected PZR level transient at first RCP start is a **5–10% drop over 15–30
minutes** as the cold RCS water begins mixing with hot PZR water through the surge
line. Recovery follows as the CVCS PI controller increases charging in response.
The level should not approach the 17% low-level isolation interlock under normal
startup conditions.

### Root Cause (Compound)

Three factors contribute:

**A. Binary RCP model:** `RCPSequencer.GetTargetRCPCount()` returns an integer count.
The engine switches physics paths based on `rcpCount == 0` vs `rcpCount > 0`.
There is no concept of a partially-started pump or fractional flow contribution.

**B. Abrupt physics path switch:** The `if (rcpCount == 0)` / `else` branch in Section 2
of `StepSimulation()` creates a hard discontinuity. One timestep the PZR and RCS are
thermally isolated with separate temperatures; the next timestep they are fully coupled
through `CoupledThermo.SolveEquilibrium()` which assumes complete mixing.

**C. Amplification by Issues #1 and #2:** Because heater throttling never reaches physics
(Issue #1), the PZR arrives at RCP start hotter and at higher pressure than it should.
Because the level setpoint is stuck at 25% (Issue #2), the PI controller fights level
recovery instead of supporting it.

### Proposed Fix

A two-part fix that replaces the binary RCP model with a staged ramp-up model:

#### Part A — RCP Ramp-Up State Model (RCPSequencer.cs + PlantConstants.Pressure.cs)

Add per-pump ramp-up tracking to `RCPSequencer`. Each pump progresses through startup
stages over time, with its effective flow fraction and heat contribution ramping from
0% to 100% over approximately 30–45 simulated minutes.

**New constants** (PlantConstants.Pressure.cs):

```
RCP Startup Timing Constants — Per stage duration:
  RCP_STAGE_1_DURATION_HR = 2/60       (2 min — motor start to 30% speed)
  RCP_STAGE_2_DURATION_HR = 7.5/60     (7.5 min — low flow development)
  RCP_STAGE_3_DURATION_HR = 12.5/60    (12.5 min — moderate flow / thermal mixing)
  RCP_STAGE_4_DURATION_HR = 17.5/60    (17.5 min — full speed thermal equilibration)
  RCP_TOTAL_RAMP_DURATION_HR ≈ 0.66    (~40 min total ramp per pump)

RCP Fractional Contribution at each stage boundary:
  RCP_STAGE_1_FLOW_FRACTION = 0.10     (10% flow at end of Stage 1)
  RCP_STAGE_2_FLOW_FRACTION = 0.30     (30% flow at end of Stage 2)
  RCP_STAGE_3_FLOW_FRACTION = 0.70     (70% flow at end of Stage 3)
  RCP_STAGE_4_FLOW_FRACTION = 1.00     (100% flow at end of Stage 4)

  RCP_STAGE_1_HEAT_FRACTION = 0.05     (5% heat — mostly friction, little mixing)
  RCP_STAGE_2_HEAT_FRACTION = 0.20     (20% heat — flow developing)
  RCP_STAGE_3_HEAT_FRACTION = 0.60     (60% heat — substantial mixing)
  RCP_STAGE_4_HEAT_FRACTION = 1.00     (100% heat — full thermal contribution)
```

Note: Flow fraction and heat fraction differ because at low speed, the pump's
mechanical energy goes primarily into shaft friction and seal heating rather than
bulk coolant heating. The heat contribution to the RCS develops more slowly than
the volumetric flow.

**New state struct** (RCPSequencer.cs):

```
RCPStartupState:
  int PumpIndex                  (0-3)
  float StartTime_hr             (sim time when start command issued)
  float ElapsedSinceStart_hr     (time since start command)
  int CurrentStage               (0-4)
  float FlowFraction             (0.0-1.0, interpolated within stage)
  float HeatFraction             (0.0-1.0, interpolated within stage)
  bool FullyRunning              (true when Stage 4 complete)
```

**New method** — `GetEffectiveRCPContribution()`:

Returns the aggregate effective flow fraction and heat fraction across all pumps,
accounting for each pump's individual ramp-up progress. For example, if pump 1 is at
Stage 3 (70% heat) and pump 2 is at Stage 1 (5% heat), the total effective heat is
`(0.70 + 0.05) × 5.25 MW = 3.94 MW` rather than `2 × 5.25 = 10.5 MW`.

The interpolation within each stage is **linear** between the stage boundary fractions.
For example, halfway through Stage 2 (which goes from 10% to 30%), the flow fraction
is 20%.

#### Part B — Gradual Physics Coupling (HeatupSimEngine.cs + RCSHeatup.cs)

Replace the binary `if (rcpCount == 0) / else` physics path with a blended model that
uses the aggregate flow fraction to interpolate between isolated and coupled behaviour.

**HeatupSimEngine.cs** — `StepSimulation()` Section 2:

The current hard branch:
```csharp
if (rcpCount == 0)
{
    // Isolated path: IsolatedHeatingStep
}
else
{
    // Coupled path: BulkHeatupStep with CoupledThermo
}
```

Becomes a three-regime model:

```
Regime 1 — No RCPs (aggregate flow fraction = 0):
  Unchanged. Use IsolatedHeatingStep. PZR and RCS thermally decoupled.

Regime 2 — RCPs Ramping (0 < aggregate flow fraction < 1.0):
  Run BOTH physics paths each timestep:
    - IsolatedHeatingStep with (1 - flowFraction) weight
    - BulkHeatupStep with flowFraction weight on the effective heat input

  Blended temperature:
    T_rcs = T_rcs_isolated × (1 - α) + T_rcs_coupled × α
    where α = aggregate flow fraction (0→1 over ~40 min for first pump)

  Blended pressure:
    P = P_isolated × (1 - α) + P_coupled × α

  This smoothly transitions from fully isolated to fully coupled as
  the pumps ramp up. The CoupledThermo solver receives a gradually
  increasing fraction of the heat input, preventing convergence
  discontinuities.

Regime 3 — All Pumps Fully Running (aggregate flow fraction = 1.0 for all started pumps):
  Unchanged. Use BulkHeatupStep with full CoupledThermo. All pumps at rated conditions.
```

**RCSHeatup.cs** — `BulkHeatupStep()`:

The `rcpHeat_MW` and `heaterPower_MW` parameters already accept variable values. No
changes to the internal physics are needed. The engine simply passes `effectiveRCPHeat`
(= aggregate heat fraction × total RCP heat) instead of `rcpCount × RCP_HEAT_MW_EACH`.

**CVCSController.cs** — `PreSeedForRCPStart()`:

Strengthen the PI pre-seed from 5 gpm to a value proportional to the expected transient:

```
preSeedCharging_gpm = 5 + 10 × (T_pzr - T_rcs) / 400
```

This scales the pre-seed with the temperature differential — a larger ΔT (meaning a
bigger expected transient) gets a stronger initial charging bias. At the typical first
RCP start conditions (ΔT ≈ 360 °F), this gives ~14 gpm pre-seed. At subsequent RCP
starts (ΔT smaller because mixing is already underway), the pre-seed is proportionally
less aggressive.

Additionally, call `PreSeedForRCPStart()` at each new pump start event (currently only
called once at bubble formation COMPLETE), since each new pump start changes the thermal
transient characteristics.

### Interaction with Issues #1 and #2

The severity of Issue #3 is amplified by Issues #1 and #2:

- **Issue #1** causes the PZR to arrive at RCP start with excessive temperature and
  pressure (heaters never throttled → more energy stored → bigger transient)
- **Issue #2** causes the PI controller to fight level recovery instead of supporting it
  (setpoint 25% → sees 40% as too high → reduces charging when it should increase)

Fixing Issues #1 and #2 first will substantially reduce the RCP startup transient even
before the staged ramp-up model is implemented. The ramp-up model then further smooths
the remaining transient from "acceptable but abrupt" to "realistic and gradual."

### Validation Criteria

| Metric | Before Fix (all issues) | After Issue #1–2 only | After All Issues Fixed |
|--------|------------------------|-----------------------|------------------------|
| PZR level drop at RCP #1 start | 17% in 30 min | ~10–12% (est.) | < 8% over 30 min |
| Pressure step at RCP #1 start | 311 psi instant | ~150 psi (est.) | < 100 psi over 15 min |
| Time to level recovery | 30+ min | ~20 min (est.) | ~15 min |
| Approach to 17% isolation interlock | 7.9% (near trip) | ~15% (marginal) | > 20% (safe margin) |
| RCP heat input at T+5 min | 5.25 MW (full) | 5.25 MW (full) | ~1.0 MW (Stage 2) |
| RCP heat input at T+20 min | 5.25 MW (full) | 5.25 MW (full) | ~3.7 MW (Stage 3) |
| RCP heat input at T+40 min | 5.25 MW (full) | 5.25 MW (full) | 5.25 MW (Stage 4) |

---

## Implementation Order

```
┌──────────────────────────────────┐
│  Issue #1 — Heater Feedback      │  CRITICAL — upstream of everything
│  (execution order fix)           │
└──────────────┬───────────────────┘
               ▼
┌──────────────────────────────────┐
│  Issue #2 — Level Setpoint       │  CRITICAL — 4 single-line fixes
│  (function substitution)         │
└──────────────┬───────────────────┘
               ▼
┌──────────────────────────────────┐
│  Validation Run                  │  Full 12+ hour heatup, compare logs
│  (confirm #1 and #2 resolved)   │
└──────────────┬───────────────────┘
               ▼
┌──────────────────────────────────┐
│  Issue #3 — RCP Ramp-Up Model    │  MODERATE — new physics model
│  Part A: Constants + Sequencer   │
│  Part B: Blended physics path    │
│  Part C: PI pre-seed scaling     │
└──────────────┬───────────────────┘
               ▼
┌──────────────────────────────────┐
│  Final Validation Run            │  Full heatup, confirm all metrics
└──────────────────────────────────┘
```

**Rationale:** Issues #1 and #2 are simple, surgical fixes (reorder + substitute) that
resolve the two most severe symptoms. Issue #3 is a more substantial model change that
benefits from being built on top of the corrected heater and level control behaviour.
The intermediate validation run between #1–2 and #3 confirms that the foundation is
solid before adding the ramp-up model.

---

## Files Affected

| File | Issue #1 | Issue #2 | Issue #3 | Change Type |
|------|----------|----------|----------|-------------|
| **HeatupSimEngine.cs** | ✅ Reorder heater calc to before physics | — | ✅ Blended physics regime; ramp-up state | Engine coordinator |
| **HeatupSimEngine.CVCS.cs** | ✅ Remove heater calc (now in main step) | ✅ Level function substitution | — | Engine partial |
| **HeatupSimEngine.BubbleFormation.cs** | — | ✅ Level function substitution | — | Engine partial |
| **CVCSController.cs** | — | ✅ Level function substitution (×2) | ✅ Scale PI pre-seed; call at each RCP start | Physics module |
| **RCPSequencer.cs** | — | — | ✅ Per-pump ramp state; staged flow/heat fractions | Physics module |
| **PlantConstants.Pressure.cs** | — | — | ✅ RCP startup stage timing + fraction constants | Constants |
| **PlantConstants.Pressurizer.cs** | — | — | — | No changes |
| **RCSHeatup.cs** | — | — | — | No changes (accepts variable heat input already) |

---

## GOLD Standard Impact

**Issues #1 and #2:** No new files. No API changes. No struct changes. Internal
reordering and single-line function substitutions only. All 10 GOLD criteria remain
satisfied for HeatupSimEngine (6 files) and CVCSController.cs.

**Issue #3:**
- `RCPSequencer.cs` gains a new state struct (`RCPStartupState`) and new methods
  (`GetEffectiveRCPContribution`, staged ramp logic). G4 (result structs) applies.
  New constants require G6 (source citation). File size remains well under 30 KB (G8).
- `PlantConstants.Pressure.cs` gains ~15 new constants. All require NRC/industry
  source citation (G6). File size increase is minor (~1 KB).
- `HeatupSimEngine.cs` gains blended physics regime logic. This is coordination
  (calling two existing physics paths and blending results), not inline physics (G3).
  No new inline calculations.

All modules will require GOLD re-certification in the v0.4.0 changelog entry.

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Issue #1: One-step pressureRate lag introduces oscillation | Low | Low | Real controllers have 1–3s sensor lag; damped system | 
| Issue #2: Level setpoint jump at transition surprises PI controller | Low | Low | Unified function is continuous; no discontinuity at 557 °F |
| Issue #1: DRAIN phase duration changes significantly | Medium | Low | Expected and correct — real plants take ~40 min |
| Issue #3: Blended physics regime causes energy non-conservation | Medium | Medium | Both paths use same energy input; blending is on outputs only |
| Issue #3: Ramp-up timing constants need tuning | Medium | Low | Conservative initial values from plant data; adjustable per-stage |
| Issue #3: Multiple pumps ramping simultaneously overlap | Low | Low | Per-pump state tracking handles overlapping ramps correctly |
| Issue #3: File size growth in RCPSequencer | Low | Low | Estimate +3 KB; well within 30 KB GOLD limit |
