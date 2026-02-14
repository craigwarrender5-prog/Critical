# CHANGELOG v4.4.0 — PZR Level/Pressure Control Fix

**Date:** 2026-02-11  
**Version:** 4.4.0  
**Type:** Critical Bug Fix + Feature Addition  
**Matching Implementation Plan:** Implementation_Plan_v4.4.0.md

---

## Problem Summary

Simulation became non-functional beyond ~14 hours of simulated time:
- PZR level runaway to 97.2% (overfill)
- Pressure runaway at 920 psi/hr reaching 2432 psia
- Steam bubble elimination (50 ft³ → 0)
- Heaters stuck at 20% minimum despite 200+ psi overpressure

## Root Causes Identified

1. **CoupledThermo solver ignores CVCS mass drain** — solver computed equilibrium with full RCS mass, then CVCS drain applied afterward (too late)
2. **Only one letdown orifice modeled** — real plant uses 3 orifices (2×75 + 1×45 gpm), limiting drain capacity
3. **No heater mode transition** — PRESSURIZE_AUTO mode's 20% minimum floor prevented heater de-energization above setpoint
4. **No pressurizer spray system** — primary overpressure control mechanism entirely absent

---

## Stage 1: CVCS Mass Drain Integration into CoupledThermo Solver

### Files Modified
| File | Change |
|------|--------|
| `HeatupSimEngine.cs` | Regime 2 & 3: CVCS net mass applied to `physicsState.RCSWaterMass` BEFORE `BulkHeatupStep()` call |

### Physics Fix
- Added `regime3CVCSPreApplied` flag to prevent double-counting
- In both Regime 2 and Regime 3, before `BulkHeatupStep`: calculate net CVCS flow (charging - letdown), convert to mass change, apply to `physicsState.RCSWaterMass`, feed VCT tracking, set flag
- `UpdateRCSInventory()` checks flag and skips mass adjustment if already pre-applied

### Technical Reference
Per NRC HRTD 4.1: CVCS flows and thermodynamic state are part of the same physical system and must be coupled, not sequential.

---

## Stage 2: Letdown Orifice Lineup Management

### Files Modified
| File | Change |
|------|--------|
| `PlantConstants.CVCS.cs` | Added 3-orifice constants: `LETDOWN_ORIFICE_75_GPM`, `LETDOWN_ORIFICE_45_GPM`, `LETDOWN_ION_EXCHANGER_LIMIT_GPM` (120 gpm), `CalculateTotalLetdownFlow()` method |
| `CVCSController.cs` | Orifice lineup management with hysteresis-based auto-opening/closing |
| `HeatupSimEngine.cs` | Added orifice state fields (`orifice75Count`, `orifice45Open`, `orificeLineupDesc`) |
| `HeatupSimEngine.CVCS.cs` | Operator orifice management — monitors PZR level error, opens/closes additional orifices |

### Physics Model
- Three parallel orifices: 2×75 + 1×45 gpm (per NRC HRTD 4.1)
- Each orifice flow scales independently with √ΔP
- Total capped at 120 gpm (ion exchanger downstream limit)
- Normal lineup: 1×75 gpm; Max lineup: 2×75 + 1×45 gpm
- Operator simulation with hysteresis to prevent hunting

---

## Stage 3: Heater Mode Transition (PRESSURIZE_AUTO → AUTOMATIC_PID)

### Files Modified
| File | Change |
|------|--------|
| `PlantConstants.Pressurizer.cs` | Added `HEATER_MODE_TRANSITION_PRESSURE_PSIA = 2200f` |
| `HeatupSimEngine.cs` | Section 1B: mode transition check when pressure ≥ 2200 psia |
| `HeatupSimEngine.HZP.cs` | Removed duplicate `UpdateHeaterPID()` call |

### Physics Effect
When pressure reaches ~2200 psia during heatup, heaters transition from rate-modulated controller (20% minimum power floor) to PID control. PID can reduce heater output to near zero when pressure exceeds setpoint, and cuts off completely above 2250 psig (`HEATER_PROP_CUTOFF_PSIG`).

### Technical Reference
Per NRC HRTD 10.2: Normal pressure control at 2235 psig uses proportional + backup heater groups with PID feedback. Transition ~50 psi below setpoint gives PID time to stabilize.

---

## Stage 4: Pressurizer Spray System

### Files Modified
| File | Change |
|------|--------|
| `PlantConstants.Pressurizer.cs` | Added `SPRAY_FULL_OPEN_FLOW_GPM = 600f`, `SPRAY_VALVE_TAU_HR = 30s` |
| `CVCSController.cs` | Added `SprayControlState` struct + `InitializeSpray()` + `UpdateSpray()` methods (~170 lines) |
| `HeatupSimEngine.cs` | Added 5 spray state fields, Section 1B-SPRAY update call, spray condensation in Regime 2 & 3 |
| `HeatupSimEngine.Init.cs` | Spray state initialization in `InitializeCommon()` |

### Physics Implementation
- **Spray valve demand:** Linear modulation 2260–2310 psig (per NRC HRTD 10.2)
- **Valve dynamics:** First-order lag τ = 30 seconds
- **Flow:** Bypass 1.5 gpm always present + up to 600 gpm at full valve open
- **Steam condensation:** `Q = m_spray × Cp × ΔT × η(0.85)`, then `m_condensed = Q / h_fg`
- **Safety guards:** Thermal shock limit (320°F ΔT), min steam space guard, max 50% steam condensation per step
- **Solver coupling:** Spray condensation applied to `physicsState` (PZRSteamMass/PZRWaterMass) BEFORE CoupledThermo solver, same pattern as CVCS mass drain fix

### Technical Reference
Per NRC HRTD 10.2: Two spray valves fed from cold legs (Loops B and C), modulated by master pressure controller. Spray water at T_cold condenses PZR steam, directly reducing pressure.

---

## Stage 5: Validation and Logging

### Files Modified
| File | Change |
|------|--------|
| `HeatupSimEngine.cs` | Added `sprayFlowHistory` list declaration |
| `HeatupSimEngine.Logging.cs` | Enhanced interval logs (orifice lineup, PZR pressure control section with PID + spray status), v4.4.0 validation checks in interval and final reports, spray flow history buffer |

### Logging Additions
- **Orifice lineup:** 75-gpm valve count, 45-gpm valve state, lineup description
- **PZR Pressure Control section:** Heater mode, PID output/setpoint/error/status, prop fraction, backup heaters, heater power, spray enabled/active/valve position/flow/ΔT/steam condensed/status
- **v4.4.0 Validation Checks (interval):**
  - PZR Level within ±10% of setpoint: PASS/FAIL
  - Pressure rate < 200 psi/hr: PASS/FAIL
  - Heater mode = AUTOMATIC_PID before reaching 2250 psia: PASS/FAIL
  - Spray active if pressure > 2275 psia: PASS/FAIL
- **v4.4.0 Final Report Validation:**
  - PZR Level within ±10%: PASS/FAIL
  - Final pressure within 2250 ± 50 psia (at T_avg ≥ 550°F): PASS/FAIL
  - Heater mode = AUTOMATIC_PID: PASS/N/A
  - Spray system functional: PASS/N/A
  - Final spray flow and heater power

### History Buffer
- Added `sprayFlowHistory` for graph rendering of spray flow over time

---

## Expected Behavior After v4.4.0

1. **PZR level** should track within ±10% of the level program setpoint throughout heatup
2. **Pressure** should be controllable within ±25 psi of 2235 psig at operating temperature
3. **Pressure rate** should remain below 200 psi/hr during steady-state heatup
4. **Heater mode** transitions to AUTOMATIC_PID at ~2200 psia; PID reduces heaters to near-zero above setpoint
5. **Spray** activates if pressure exceeds 2260 psig, providing up to 600 gpm of cold-leg water to condense PZR steam
6. **Full heatup** should reach 557°F / 2235 psig without runaway

## Unaddressed Issues

None — all four root causes addressed. Future refinements may include:
- Spray flow scaling with actual RCP ΔP (currently uses fixed 600 gpm at full valve)
- PORV opening logic above 2335 psig (not needed for normal heatup)
- Safety valve modeling above 2485 psig (design basis, not expected in normal operations)
