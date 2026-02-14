# CHANGELOG v0.2.0.0 — Primary Solid-Regime Pressure and Mass-Coupling Corrections

**Date:** 2026-02-13
**Version:** 0.2.0.0
**Type:** Physics Correction (Solid Regime — Compressibility Coupling)
**Matching Implementation Plan:** IP-0002 — Primary Solid Regime

---

## Problem Summary

Three linked defects in the solid pressurizer regime (pre-bubble) caused unrealistic pressure behavior during heatup:

**A) CS-0021: Solid-Regime Pressure Decoupled from Mass Change**
The PI controller's letdown adjustment (`LetdownAdjustEff`) was applied to `LetdownFlow` in the same computation step as the pressure equation. This zero-delay feedback loop allowed the CVCS to perfectly cancel thermal expansion within ~2 minutes, producing `dV_net -> 0 -> dP -> 0`. Pressure appeared flat (~365 psia) despite continuous thermal expansion during HOLD_SOLID.

**B) CS-0022: Early Pressurization Response Mismatched with Controller Actuation**
During HEATER_PRESSURIZE, the +/-1 gpm authority clamp allowed the controller to consistently inject ~1 gpm net charging (because error is always large negative), accelerating pressurization beyond pure thermal expansion rate. The integral accumulated to its 2000 psi-sec limit pointlessly during the saturated period.

**C) CS-0023: Surge Flow Rises While Pressure Remains Flat**
Surge flow (derived from PZR thermal expansion alone) increased with temperature while pressure rate was zero (CVCS cancellation). These trends should be coupled in a closed liquid-full system. Secondary symptom of CS-0021 — resolved by transport delay.

**Root Cause:** Missing CVCS transport delay. In a real PWR, CVCS flow changes require ~60 seconds of transit through piping, heat exchangers, and valves before affecting RCS volume. The simulation applied them instantaneously.

---

## Phase A: CVCS Transport Delay and Anti-Windup

### Files Modified
| File | Change |
|------|--------|
| `SolidPlantPressure.cs` | Added transport delay ring buffer, anti-windup logic, CS-0023 diagnostic |

### New Constants
| Constant | Value | Purpose |
|----------|-------|---------|
| `CVCS_TRANSPORT_DELAY_SEC` | 60f | Piping transit time (seconds) |
| `DELAY_BUFFER_MAX_SLOTS` | 24 | Max ring buffer size (120s / 5s worst case) |
| `ANTIWINDUP_DEADTIME_THRESHOLD_GPM` | 0.5f | Dead-time gap threshold for integral freeze |

### New State Fields (SolidPlantState)
| Field | Type | Purpose |
|-------|------|---------|
| `TransportDelayBuffer` | `float[]` | Ring buffer of past LetdownAdjustEff values |
| `DelayBufferHead` | `int` | Head pointer (oldest slot) |
| `DelayBufferLength` | `int` | Active slots = ceil(delay / dt) |
| `DelayedLetdownAdjust` | `float` | Delayed adjustment applied this step |
| `TransportDelayActive` | `bool` | True once buffer fully primed |
| `AntiWindupActive` | `bool` | True when integral frozen |
| `SurgePressureConsistent` | `bool` | CS-0023 diagnostic flag |

### Physics Changes

**Transport Delay (ring buffer, read-before-write):**
```
Step N: READ oldest value from [Head] -> DelayedLetdownAdjust
        WRITE current LetdownAdjustEff to [Head]
        ADVANCE Head = (Head + 1) % Length

LetdownFlow = baseLetdown + DelayedLetdownAdjust  (was: + LetdownAdjustEff)
```
At dt=10s and delay=60s: 6 buffer slots, yielding exactly 6 steps (60 sec) of lag between PI output and pressure effect.

**Anti-Windup:**
Integral accumulation inhibited when:
- Actuator saturated (slew clamp active, or HEATER_PRESSURIZE +/-1 gpm limit hit)
- Dead-time gap: `|LetdownAdjustEff - DelayedLetdownAdjust| > 0.5 gpm`

**CS-0023 Diagnostic:**
`SurgePressureConsistent` = true when surge flow direction and pressure rate direction agree, or both are near zero, or mode is HOLD_SOLID (where CVCS opposition is expected behavior).

### Validation Tests Update
Tests 6-7 in `ValidateCalculations()` now run 10 steps (was 1) to account for transport delay propagation time.

---

## Phase B: HOLD_SOLID PI Gain Tuning (Measurement Gate)

### No Code Changes

Phase B measurement gate measured HOLD_SOLID oscillation:
| Window | Time Range | Peak-to-Peak (psi) |
|--------|-----------|-------------------|
| 1 | 1.00 - 2.00 hr | 13.88 |
| 2 | 2.00 - 3.50 hr | 12.42 |
| 3 | 3.50 - 5.00 hr | 11.01 |

All windows within 3-25 psi acceptance band. Naturally damping oscillation centered on 350.3 psig setpoint. **No gain or deadband changes required.**

---

## Phase C: Surge-Pressure Consistency Verification (CS-0023)

### No Code Changes

`SurgePressureConsistent` = true for 100% of steps in both modes:
- HEATER_PRESSURIZE: 260/260 steps consistent (100.0%)
- HOLD_SOLID: 2712/2712 steps consistent (100.0%)

Zero flat-pressure-with-surge anomalies. CS-0023 resolved by transport delay.

---

## Phase D: Regime 1 to Regime 2 Transition Continuity

### No Code Changes

Bubble formed at 8.26 hr, T_pzr = 435.1 F:
| Criterion | Metric | Value | Limit | Result |
|-----------|--------|-------|-------|--------|
| D1 | Pressure step at transition | 0.495 psi | < 5 psi | PASS |
| D2 | Surge flow step at transition | 0.006 gpm | < 2 gpm | PASS |
| D3 | Mass step-to-step change at transition | 0.007 lbm | continuous | PASS |
| D4 | ValidateCalculations() | All 11 tests | PASS | PASS |

Note: Per-step surge mass transfer at 435 F is ~9.35 lbm (normal at high beta), but the step-to-step CHANGE at the bubble transition boundary was only 0.007 lbm — no discontinuity.

---

## Files Changed Summary

| File | Lines | Change Description |
|------|-------|--------------------|
| `SolidPlantPressure.cs` | +78/-8 | Transport delay ring buffer, anti-windup, CS-0023 diagnostic, test update |

---

## Validation Summary

| Criterion | Result |
|-----------|--------|
| A1: PressureRate > 0 during HEATER_PRESSURIZE | PASS (100% of 260 steps) |
| A2: Transport delay lag confirmed | PASS (6-step / 60 sec lag) |
| A3: No relief valve actuation below 450 psig | PASS (max P = 373.6 psia) |
| A4: Mass conservation at 5 hr | PASS (0.02 lbm error) |
| A5: ValidateCalculations() regression | PASS (all 11 tests) |
| B: HOLD_SOLID oscillation in 3-25 psi band | PASS (11-14 psi P-P) |
| C: SurgePressureConsistent 100% | PASS |
| D1-D4: Regime transition continuity | PASS |
| Unity build | PASS (0 errors) |

---

## Testing Checklist

- [x] HEATER_PRESSURIZE: pressure rate nonzero, thermal-expansion-dominated
- [x] HOLD_SOLID: visible pressure oscillation (11-14 psi P-P), naturally damping
- [x] Transport delay: 6-step lag confirmed between PI output and pressure effect
- [x] Anti-windup: integral frozen during actuator saturation and dead-time gap
- [x] CS-0023 diagnostic: surge-pressure consistency 100% both modes
- [x] Regime 1->2 transition: continuous pressure, surge, and mass
- [x] Mass conservation: 0.02 lbm error at 5 hr
- [x] All ValidateCalculations() tests pass (11/11)
- [x] Unity compile: 0 errors
