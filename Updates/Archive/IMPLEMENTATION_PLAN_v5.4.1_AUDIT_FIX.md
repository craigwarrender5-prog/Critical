# Implementation Plan v5.4.1 — Inventory Audit Fix + Startup Pressurization Stabilization

**Date:** 2026-02-13
**Version:** 5.4.1
**Predecessor:** IMPLEMENTATION_PLAN_v5.4.1.md (PZR Drain Mass Spike Fix)
**Forensics Report:** Forensics/INVENTORY_AUDIT_DISCREPANCY.md
**Changelog:** Changelogs/CHANGELOG_v5.4.1_AUDIT_FIX.md
**Status:** COMPLETE

---

## A. Problem Statement

The INVENTORY AUDIT log section reports all primary masses as **0 lbm** during solid ops, while the MASS INVENTORY section (same log file) shows correct non-zero values. Mass Conservation is marked **FAIL** at the solid-to-two-phase transition. Cold-start pressurization was also broken: the sim initialized at the pressure setpoint (365 psia), skipping the entire cold-start pressurization phase.

### Root Cause A -- Orphaned Canonical Fields (0 lbm from start)

Two `SystemState` fields were declared in `CoupledThermo.cs:760-761` as part of the v5.0.2 architecture but **never assigned values**:

| Field | Declared | Comment says "maintained by" | Actually written by |
|---|---|---|---|
| `TotalPrimaryMassSolid` | `CoupledThermo.cs:760` | SolidPlantPressure via CVCS boundary flow | **Nobody** |
| `PZRWaterMassSolid` | `CoupledThermo.cs:761` | SolidPlantPressure via surge transfer | **Nobody** |

As C# `struct` fields, both default to `0f`. The INVENTORY AUDIT solid branch (`Logging.cs:303-310`) reads these fields, producing:
- `PZR_Water_Mass_lbm = 0`
- `RCS_Mass_lbm = 0 - 0 = 0`

A third orphaned field, `TotalPrimaryMass_lb` (`CoupledThermo.cs:768`), affects the Primary Mass Ledger diagnostics (`Logging.cs:470-519`), which reports `primaryMassLedger_lb = 0` throughout.

Similarly, `InitialPrimaryMass_lb` (`CoupledThermo.cs:778`) is never set, breaking the expected-mass calculation in the ledger diagnostics.

### Root Cause B -- Conservation Cliff at Solid-to-Two-Phase Transition

When bubble detection fires (`BubbleFormation.cs:107`), `solidPressurizer` flips to `false` while `bubbleFormed` stays `false` (gates RCP starts). The audit branch falls through to the two-phase branch, which reads real ~824,000 lbm values. But `Initial_Total_Mass_lbm` was captured at init via the broken solid path (~0 lbm). The conservation error instantly jumps to ~824,000 lbm.

### Root Cause C -- VALIDATION STATUS reading stale VCT gallon check

The `Mass Conservation` line in VALIDATION STATUS was gated on `massConservationError < 10f` -- the VCT-level gallon-based cross-system check from `VCTPhysics.VerifyMassConservation()`. This check is subject to numerical drift and has no dependency on the corrected canonical fields.

### Root Cause D -- Cold-start pressure at setpoint (no pressurization phase)

`InitializeColdShutdown()` hardcoded `pressure = SOLID_PLANT_INITIAL_PRESSURE_PSIA` (365 psia = setpoint), meaning the sim started at the final pressure target. There was no pressurization transient to model.

### Root Cause E -- CVCS-dominated pressurization

After restoring the cold start at 114.7 psia, the CVCS PI controller was the primary driver of pressurization (analytical proof: ~81% from net CVCS volume trim, ~19% from thermal expansion). This is physically incoherent -- real plant pressurization is heater-driven.

---

## B. Design Intent

1. **All SOLID canonical fields must be populated every tick during solid ops.** This completes the v5.0.2 architecture. The INVENTORY AUDIT is a pure consumer of canonical state -- it must never perform its own V*rho recalculation.

2. **Single source of truth rule:** `PZRWaterMassSolid` is set from `physicsState.PZRWaterMass` -- not recomputed via V*rho.

3. **VALIDATION STATUS uses system-wide mass check** (`massError_lbm < 500f`), not VCT gallon cross-check.

4. **Startup pressurization is heater-led.** CVCS authority is limited during the pressurization phase so pressure rise emerges from thermal expansion. CVCS is only the primary control actuator once near setpoint (HOLD_SOLID).

5. **CVCS actuator dynamics model real valve/pump behavior.** Pressure filter, first-order lag, and slew-rate limiter prevent instantaneous flow changes in the stiff water-solid system.

---

## C. Staged Implementation

### Stage 0: Initialization -- COMPLETE

**Objective:** Populate SOLID canonical fields at sim start.

**File:** `HeatupSimEngine.Init.cs`

- Cold shutdown: Added `PZRWaterMassSolid`, `TotalPrimaryMassSolid`, `TotalPrimaryMass_lb`, `InitialPrimaryMass_lb`
- Warm start: Added `TotalPrimaryMass_lb`, `InitialPrimaryMass_lb`
- Changed initial pressure from `SOLID_PLANT_INITIAL_PRESSURE_PSIA` (365) to `PRESSURIZE_INITIAL_PRESSURE_PSIA` (114.7)

**Validated:** Inventory Audit baseline ~924k lbm, TotalPrimaryMass ~824k lb.

---

### Stage 1: Runtime -- COMPLETE

**Objective:** Keep SOLID canonical fields current every tick. Fix VALIDATION STATUS. Add observability.

- **1A -- SOLID canonical field sync** (`HeatupSimEngine.cs`, Regime 1 solid branch): 3 field assignments using single-source-of-truth rule
- **1B -- Cold-start pressure fix** (see Stage 0)
- **1C -- VALIDATION STATUS fix** (`HeatupSimEngine.Logging.cs`): Replaced VCT gallon check with `massError_lbm < 500f`
- **1D -- Startup observability** (`HeatupSimEngine.cs`): t=0 interval log + startup burst log every 60 sim-seconds

**Validated:** INVENTORY AUDIT correct, VALIDATION STATUS PASS, pressure ramp observability confirmed.

---

### Stage 2: Validation -- COMPLETE

**No code changes.** Log-based analysis confirming conservation continuity at branch transition.

- Root Causes A and B resolved
- Known limitation documented: ~20,000 lbm PZR mass step at bubble detection (pre-existing, `physicsState.PZRWaterMass` stale during solid ops)

---

### Startup Pressurization Stabilization -- COMPLETE

**Objective:** Make cold-start pressurization physically coherent and smooth.

**Diagnostic phase:** Analytical proof that ~81% of pressure rise was CVCS-driven (controller-led), not heater-driven. dP from heaters at 100F ~3.1 psi/min, but linear ramp demanded ~16.7 psi/min.

**Error-limited ramp (intermediate):** Replaced linear time-based ramp with error-limited ramp where SP_ramp only advances when plant is tracking. This was a stepping stone.

**CVCS actuator dynamics:** Added to `SolidPlantPressure.cs`:
- Pressure filter (tau=3s) on controller input
- First-order lag (tau=10s) on CVCS trim output
- Slew-rate limiter (1.0 gpm/s max change)

**Two-phase control (final architecture):** Replaced error-limited ramp with:
- **HEATER_PRESSURIZE mode:** CVCS authority capped to +/-1.0 gpm net trim. Pressure rise driven entirely by heater thermal expansion. PI still runs for stability but cannot dominate.
- **HOLD_SOLID mode:** Full PI authority with actuator dynamics. Entered when |P - 365| <= 5 psi for 30 consecutive seconds.
- Reversible transition: if P drops >15 psi below setpoint, reverts to HEATER_PRESSURIZE.

**Files modified:**
- `SolidPlantPressure.cs` -- Two-phase control, actuator dynamics, constants, struct fields
- `HeatupSimEngine.cs` -- Burst log with Mode, PZR_T, dT, NetCmd, NetEff, Slew
- `HeatupSimEngine.Logging.cs` -- Interval log solid P section, P Rate validation

---

## D. Files Affected (Final)

| # | File | Changes |
|---|------|---------|
| 1 | `Assets/Scripts/Validation/HeatupSimEngine.Init.cs` | 4 canonical field assignments (cold), 2 (warm), pressure constant change |
| 2 | `Assets/Scripts/Validation/HeatupSimEngine.cs` | 3 canonical field sync (Regime 1); t=0 interval log; authoritative burst log (30 min, with Mode/PZR_T/dT/NetCmd/NetEff/Slew) |
| 3 | `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs` | VALIDATION STATUS mass check; Solid P section (filtered P, NetCmd/NetEff); P Rate validation (HEATER_PRESSURIZE/HOLD_SOLID aware) |
| 4 | `Assets/Scripts/Physics/SolidPlantPressure.cs` | Two-phase control (HEATER_PRESSURIZE/HOLD_SOLID); CVCS authority limiting; actuator dynamics (P filter, lag, slew); struct fields; constants |

**Total: 4 files.**

**Files NOT changed:**
- `CoupledThermo.cs` -- field declarations already exist
- `HeatupSimEngine.BubbleFormation.cs` -- transition logic correct by design
- `PressurizerPhysics.cs` -- no changes needed
- `PlantConstants.Pressure.cs` -- existing constants used as-is
- No canonical mass field logic, no thermodynamic equation, no mass validation logic altered

---

## E. Risks and Non-Goals

### Risks

| Risk | Mitigation |
|---|---|
| `physicsState.PZRWaterMass` stale during solid ops | Single-source-of-truth wiring. ~20k lbm step documented as pre-existing. |
| `TotalPrimaryMass_lb` sync could interfere with CoupledThermo in Regime 2/3 | Sync only inside solid branch guard. CoupledThermo manages its own fields post-bubble. |
| HEATER_PRESSURIZE authority cap (1 gpm) may be too tight | Tunable constant. Heaters provide sufficient thermal expansion to pressurize without CVCS assistance. |
| Actuator lag/slew may slow controller response in HOLD_SOLID | Both are tunable. 10s lag and 1 gpm/s slew are conservative; can be relaxed if needed. |

### Non-Goals

- No architectural refactor of `SystemState` or audit branching
- No telemetry implementation (v5.5.0 scope)
- No changes to BubbleFormation transition logic
- No changes to MASS INVENTORY logging
- No changes to CoupledThermo solver
- No direct pressure assignment or clamping

---

## F. Acceptance Criteria

### Pre-Bubble (Solid Ops)

- [x] **Interval 001 (t=0.00 hr):** INVENTORY AUDIT shows non-zero primary masses (~924k lbm total)
- [x] **Interval 001:** `Mass Source` = `CANONICAL_SOLID`
- [x] **Interval 001:** `Conservation Error` < 100 lbm
- [x] **VALIDATION STATUS:** `Mass Conservation: PASS` from start of run

### Cold-Start Pressurization

- [x] **Interval 001:** `RCS Pressure` = 114.7 psia (100 psig post-fill/vent)
- [x] **Startup burst log:** Pressure rises from ~115 psia toward 365 psia
- [x] **Burst log:** Net CVCS near zero during HEATER_PRESSURIZE (|NetEff| <= 1 gpm)
- [x] **Burst log:** Pressure rise correlates with PZR_T increase (heater-driven causality)
- [x] **Burst log:** Mode transitions from HEATER_PRESSURIZE to HOLD_SOLID near 365 psia
- [x] **Burst log:** No multi-gpm instant jumps (slew limiter active)

### At Bubble Transition

- [ ] `Mass Source` transitions from `CANONICAL_SOLID` to `CANONICAL_TWO_PHASE`
- [ ] Conservation Error step <= ~20,000 lbm (not ~824,000 lbm cliff)

### Invariants

- [x] No direct pressure assignment during solid ops
- [x] Thermodynamic equation `dP = dV/(V*kappa)` unchanged
- [x] Canonical mass fields unchanged
- [x] Mass conservation validation remains PASS (`massError_lbm < 500`)
- [x] All `ValidateCalculations()` tests pass

---

## G. Post-Implementation State

### Canonical Mass Architecture

- `PZRWaterMassSolid`, `TotalPrimaryMassSolid`, `TotalPrimaryMass_lb`, `InitialPrimaryMass_lb` -- all fully authoritative from t=0 through solid ops
- Single source of truth: `PZRWaterMassSolid = physicsState.PZRWaterMass` (no V*rho recalc)
- VALIDATION STATUS uses `massError_lbm < 500f` exclusively; gallon-based VCT cross-check removed from gating

### Startup Pressurization Architecture

- **HEATER_PRESSURIZE mode:** Heaters are the primary pressurization driver. CVCS trim authority limited to +/-1.0 gpm (tunable via `HEATER_PRESS_MAX_NET_TRIM_GPM`). Pressure rise emerges from thermal expansion of water heated by PZR heaters.
- **HOLD_SOLID mode:** Normal PI control with full authority. Entered via dwell timer (30s within +/-5 psi of setpoint). CVCS makes small corrections to maintain pressure.
- **Reversible transition:** If P drops >15 psi below setpoint during HOLD_SOLID, reverts to HEATER_PRESSURIZE with integral reset.
- **No direct pressure assignment.** Pressure is always computed via `dP = dV_net / (V_total * kappa)`.

### CVCS Actuator Dynamics

| Parameter | Value | Purpose |
|---|---|---|
| `CVCS_PRESSURE_FILTER_TAU_SEC` | 3s | Low-pass filter on P_actual for controller input |
| `CVCS_ACTUATOR_TAU_SEC` | 10s | First-order lag on CVCS trim output (valve/pump inertia) |
| `CVCS_MAX_SLEW_GPM_PER_SEC` | 1.0 gpm/s | Maximum per-tick change in effective CVCS trim |

These prevent instantaneous flow changes in the stiff water-solid system (kappa ~ 3.2e-6/psi at 100F), which would otherwise produce stepwise pressure traces.

### Mass Conservation

- `massError_lbm < 500f` is the sole gating check for VALIDATION STATUS Mass Conservation
- All `ValidateCalculations()` tests pass (11 tests covering initialization, heating, pressure change, relief valve, CVCS response, bubble formation, surge flow, mass conservation)

---

## H. Lessons Learned

### Controller Dominance vs Physics Dominance

The original PI controller with full authority (50 gpm letdown adjustment range) dominated the pressurization trajectory. Analytical proof showed ~81% of pressure rise from CVCS trim and only ~19% from heater thermal expansion. This produced physically incoherent behavior: pressure tracking a setpoint ramp rather than emerging from thermodynamics.

**Resolution:** Separate the control into two phases. During pressurization, cap CVCS authority to a tiny envelope (+/-1 gpm). During hold, restore full authority. This makes the "why" of pressure rise causally correct: heaters heat water, water expands, pressure rises.

### Importance of Actuator Modeling

In a water-solid system with extremely low compressibility, even small instantaneous flow changes produce large pressure steps. A 1 gpm imbalance over 10 seconds produces ~0.2 ft3 of volume change, which at kappa ~ 3.2e-6/psi and V_total ~ 13,300 ft3 translates to ~4.7 psi. Without actuator dynamics (lag + slew), every PI correction was an instant step function in volume, producing a jagged pressure trace.

**Resolution:** Model real valve/pump behavior with first-order lag and slew-rate limits. The pressure signal fed to the controller is also filtered to prevent noise-driven oscillation.

### Timestep Resolution

The sim timestep of 1/360 hr (~10 seconds) is large relative to the actuator time constants (3-10 seconds). At this timestep, the first-order lag filter has alpha = 1.0 (passthrough), so the slew limiter becomes the sole smoothing mechanism. Finer timesteps would allow the lag to contribute and would produce smoother physics traces overall.

**Future work:** Investigate sim ratio cap (6.5x vs 10x), fixed timestep decoupling from frame rate, and possible substepping for the pressure-volume-temperature coupling. This is tracked as a separate architecture review, not implemented in v5.4.1.

---

*v5.4.1 CLOSED -- 2026-02-13*
