# CHANGELOG v5.0.0 — Startup Sequence Realism Overhaul

**Date:** 2026-02-11  
**Version:** 5.0.0  
**Type:** MAJOR — Fundamental Physics Model Correction + New Simulation Phases  
**Matching Implementation Plan:** IMPLEMENTATION_PLAN_v5.0.0.md (Rev 2)  
**Predecessor:** v4.4.0 (PZR Level/Pressure Control Fix)

---

## Overview

Version 5.0.0 is the largest single physics overhaul in the project's history, touching the SG secondary thermal model, RCS pressurization, RHR isolation, CVCS control philosophy, and mass conservation accounting. The central breakthrough — after 2000+ simulation runs across 6 model architectures — was identifying that the SG secondary side transitions from a closed stagnant pool to an **open system** where energy exits as steam. No closed-system model could simultaneously satisfy both the 45–55°F/hr heatup rate and 20–40°F stratification targets because the secondary thermal mass (1.76M BTU/°F, 1.82× primary) creates an impossible energy balance when treated as a sealed vessel.

Full review of NRC HRTD 19.0 confirmed that real Westinghouse 4-Loop PWR heatups have three distinct thermodynamic phases, and v5.0.0 implements all three.

---

## Problem Summary

Six discrepancies between the simulation and NRC HRTD procedures:

| # | Issue | Severity | Resolution |
|---|-------|----------|------------|
| 1 | Missing RCS pressurization phase (100→320 psig) | CRITICAL | Stage 6 |
| 2 | SG secondary heating treated as closed system — impossible energy balance | CRITICAL | Stages 1–3 |
| 3 | SG secondary pressure model inconsistent with open-system boiling | HIGH | Stages 2–3 |
| 4 | RHR isolation at first RCP start instead of 350°F | HIGH | Stage 5 |
| 5 | Mass conservation error at RCP start and phase transitions | HIGH | Stage 8 |
| 6 | Post-bubble CVCS control uses variable letdown instead of NRC HRTD 10.3 philosophy | MEDIUM | Stage 7 |

---

## Stage 1: Three-Regime SG Model — Subcooled Phase Refinement

### Summary

Established the three-regime framework in `SGMultiNodeThermal` and refined the subcooled phase (100→220°F). This is the SHORT phase (~2.4 hours) where the SG secondary is a closed stagnant pool at N₂ blanket pressure (~17 psia). The existing thermocline model remains valid for this regime.

### Files Modified

| File | Change |
|------|--------|
| `SGMultiNodeThermal.cs` | Added `SGThermalRegime` enum (Subcooled, Boiling, SteamDump). Added regime state fields to `SGMultiNodeState` (CurrentRegime, SteamProductionRate_lbhr/MW, TotalSteamProduced_lb, SecondaryWaterMass_lb, NodeBoiling[]). Added regime detection logic in `Update()`. Added regime fields to `SGMultiNodeResult`. Isolated subcooled path. Updated `Initialize()` with regime state. |

### Physics

- Regime detection: Subcooled when all nodes < T_sat(P_secondary); Boiling when any node ≥ T_sat; SteamDump when P_secondary ≥ 1092 psig
- Subcooled phase behavior unchanged from v4.3.0 (thermocline model, stagnant NC HTC, bundle penalty)
- Steam production rate = 0 during subcooled regime
- Secondary water mass initialized at 1,660,000 lb (4 × 415,000 lb wet layup)

### Source

NRC HRTD ML11223A342 Section 19.0 — three-phase heatup sequence

---

## Stage 2: Boiling / Open System Phase (Core Breakthrough)

### Summary

Implemented the open-system boiling model where energy exits as steam via open MSIVs. This is the fundamental physics correction that resolves the impossible energy balance. During Regime 2 (220→557°F, ~6.7 hours), boiling nodes are clamped to T_sat(P_secondary) and all heat transfer energy goes to latent heat of vaporization — it does NOT heat the 1.66M lb water inventory.

### Files Modified

| File | Change |
|------|--------|
| `SGMultiNodeThermal.cs` | Per-node boiling/subcooled logic in heat transfer loop. Boiling nodes: Q drives steam production (Q/h_fg), T clamped to T_sat. Subcooled nodes: sensible heating (unchanged). Added `GetBoilingNodeHTC()` with pressure-dependent nucleate boiling HTC (500→700 BTU/hr·ft²·°F overall, primary-side limited). Rate-limited open-system pressure evolution via `UpdateSecondaryPressure()` (max 200 psi/hr). Boiling nodes use full geometric area (no thermocline penalty — boiling agitation). Enhanced inter-node conduction at boiling/subcooled interfaces (UA = 5,000). |
| `PlantConstants.SG.cs` | Added boiling regime constants: `SG_BOILING_HTC_LOW_P` (500), `SG_BOILING_HTC_HIGH_P` (700), `SG_BOILING_HTC_HIGH_P_REF_PSIA` (1200), `SG_MAX_PRESSURE_RATE_PSI_HR` (200), `SG_MIN_STEAMING_PRESSURE_PSIA` (17). |
| `WaterProperties.cs` | Added `LatentHeat(float P_psia)` — polynomial fit to NIST steam tables (h_fg from 970 BTU/lb at 14.7 psia to 635 BTU/lb at 1107 psia). |

### Physics

- Boiling node energy disposition: Q_node = h × A_eff × (T_rcs − T_sat) → m_steam = Q_node / h_fg → steam exits system
- Boiling node temperature: clamped to T_sat(P_secondary), does NOT rise from sensible heating
- Pressure evolution: rate-limited ramp toward P_sat(T_hottest_node), max 200 psi/hr, modeling steam line dynamics and condensation damping
- Energy balance: Q_rcp (21 MW) = Q_primary_heatup (14.3 MW) + Q_steam_exit (5.2 MW) + Q_losses (1.5 MW)
- Steam production: ~7,000–26,000 lb/hr depending on pressure/h_fg; ~149,000 lb total over Regime 2

### Source

NRC HRTD ML11223A342 Section 19.0; Incropera & DeWitt Ch. 10 (Boiling); NIST Steam Tables

---

## Stage 3: Steam Dump Termination & HZP Integration

### Summary

Implemented Regime 3: when SG secondary pressure reaches the steam dump setpoint (1092 psig), steam dumps modulate to hold pressure constant. T_sat(1092 psig) = 557°F = no-load T_avg, terminating the heatup. Wired the multi-node SG model output into the existing HZP stabilization systems.

### Files Modified

| File | Change |
|------|--------|
| `SGMultiNodeThermal.cs` | Pressure cap at steam dump setpoint (1092 psig + 14.7 psia). SteamDump regime detection. |
| `PlantConstants.SG.cs` | Added `SG_STEAM_DUMP_SETPOINT_PSIG` (1092), `SG_SAFETY_VALVE_SETPOINT_PSIG` (1185). |
| `HeatupSimEngine.cs` | Wired `sgSecondaryPressure_psia` from SGMultiNodeThermal result into `steamPressure_psig` for steam dump controller. |
| `HeatupSimEngine.HZP.cs` | Updated steaming check to use multi-node regime instead of old lumped `SGSecondaryThermal.IsSteaming()`. Steam dump auto-enable uses SG multi-node pressure. |

### Physics

- Pressure capped at 1106.7 psia (1092 psig) — steam dumps prevent further pressure rise
- All excess RCP heat (~19.5 MW) rejected to condenser as steam
- T_rcs stabilizes at T_sat(1092 psig) ≈ 557°F (Hot Standby / HZP)
- Smooth Boiling→SteamDump transition with no discontinuities

### Source

NRC HRTD ML11223A294 Section 11.2 — Steam dumps at 1092 psig; NRC HRTD ML11223A244 Section 7.1 — SG safety valves

---

## Stage 4: SG Secondary Mass & Level Tracking

### Summary

Added SG water inventory tracking and level indication as mass changes from draining and steam boiloff. SG draining starts at T_rcs ≈ 200°F via blowdown system (150 gpm/SG), reducing from wet layup (100% WR) to operating level (~33% NR). Both Wide Range and Narrow Range SG level indications are computed from mass fraction.

### Files Modified

| File | Change |
|------|--------|
| `SGMultiNodeThermal.cs` | Added draining state fields to `SGMultiNodeState` (DrainingActive, DrainingComplete, TotalMassDrained_lb, DrainingRate_gpm, WideRangeLevel_pct, NarrowRangeLevel_pct, DrainingStartTime_hr). Added Section 9 (draining model) and Section 10 (level calculation) to `Update()`. Added `StartDraining()` public method. Steam boiloff reduces `SecondaryWaterMass_lb` each timestep. Level fields added to `SGMultiNodeResult`. Updated `GetDiagnosticString()` with drain/level data. |
| `PlantConstants.SG.cs` | Added `SG_DRAINING_START_TEMP_F` (200), `SG_DRAINING_RATE_GPM` (150), `SG_DRAINING_TARGET_MASS_FRAC` (0.55). |
| `HeatupSimEngine.cs` | Wired draining trigger: calls `SGMultiNodeThermal.StartDraining()` when T_rcs ≥ 200°F. |

### Physics

- Initial mass: 1,660,000 lb total (4 × 415,000 lb/SG wet layup)
- Draining removes ~747,000 lb (to 55% of initial = 913,000 lb)
- Steam boiloff removes ~149,000 lb over Regime 2
- WR level: linear with mass fraction (100% = wet layup, 0% = empty)
- NR level: referenced to operating band (100% NR = 55% of wet layup mass)
- During wet layup, NR reads off-scale high (~182%)

### Note

Feedwater/condensate return is NOT modeled — the secondary is an open system where mass only decreases. This is documented in Future Features (Priority 5, Screen 5) for implementation when the full SG system is modeled.

### Source

NRC HRTD 2.3 / 19.0 — SG startup draining procedures; Westinghouse FSAR — SG level instrumentation

---

## Stage 5: RHR Isolation Timing Correction

### Summary

Corrected RHR isolation trigger from `rcpCount > 0` (first RCP start at ~153°F) to `T_rcs >= 350°F` (Mode 3 entry). Per NRC HRTD 19.0, RHR remains connected during the early heatup phase, providing letdown flow via the crossconnect (HCV-128) and supplemental cooling.

### Files Modified

| File | Change |
|------|--------|
| `HeatupSimEngine.cs` | Changed RHR isolation condition from `rcpCount > 0` to `T_avg >= 350f`. Kept 585 psig pressure interlock as backup. Added event logging for RHR isolation and ECCS alignment. |
| `HeatupSimEngine.CVCS.cs` | Updated letdown path logic: RHR crossconnect below 350°F, transition to normal orifice lineup above 350°F. |

### Physics

- RHR stays connected from cold shutdown through ~350°F (Mode 5→4 and early Mode 4)
- RHR HX provides supplemental heat removal during early heatup
- Letdown path via RHR crossconnect provides stable 75 gpm letdown at low pressures where orifice flow would be inadequate
- Smooth transition to orifice-based letdown when RHR isolates

### Source

NRC HRTD ML11223A342 Section 19.0 — RHR isolation at Mode 3 entry (~350°F)

---

## Stage 6: Initial RCS Pressurization Phase

### Summary

Added the Initial RCS Pressurization Phase, modeling the real-plant startup step where the RCS is pressurized from fill/vent conditions (~100 psig) to the 320–400 psig solid plant operating band using CCP charging from the VCT. PZR heaters remain OFF during this phase; pressurization is accomplished purely by hydraulic mass addition (charging > letdown) into the water-solid RCS.

### Files Modified

| File | Change |
|------|--------|
| `PlantConstants.Pressure.cs` | Added pressurization phase constants: `PRESSURIZE_INITIAL_PRESSURE_PSIG` (100), `PRESSURIZE_COMPLETE_PRESSURE_PSIG` (320), `PRESSURIZE_STABILITY_TIME_HR` (5 min), `PRESSURIZE_BASE_LETDOWN_GPM` (40). |
| `HeatupSimEngine.cs` | Added `skipPressurizationPhase` Inspector toggle. Added pressurization state fields. Modified `StepSimulation()` Regime 1: passes 0 kW heater power and reduced letdown during pressurization. Pressurization completion check with 5-minute stability requirement. Heater energization on completion. |
| `HeatupSimEngine.Init.cs` | Conditional initialization: 100 psig/heaters OFF when pressurization enabled; 350 psig/heaters ON when skipped (regression path). |

### Physics

- dP/dt = (dV_thermal − dV_cvcs) / (V_total × κ) — existing `SolidPlantPressure` PI controller handles pressurization without modification
- During pressurization: dV_thermal ≈ 0 (no temperature change), dV_cvcs < 0 (charging 75 gpm > letdown 20–40 gpm)
- Pressure rises at ~50–150 psi/hr depending on compressibility
- Stabilization: 5-minute hold at ≥320 psig before heater energization
- Duration: ~1–3 hours

### Source

NRC HRTD ML023040268 Chapter 17.0 — Pressurization from 50–100 psig; NRC HRTD ML11223A342 Section 19.2.1

---

## Stage 7: Post-Bubble Level/Pressure Control Alignment

### Summary

Aligned post-bubble heatup CVCS control with NRC HRTD 10.2/10.3 philosophy: **constant 75 gpm letdown, variable charging via PI controller**. This replaces the previous approach where both letdown (via adaptive orifice lineup) and charging were variable during heatup.

### Files Modified

| File | Change |
|------|--------|
| `PlantConstants.CVCS.cs` | Added `HEATUP_FIXED_LETDOWN_GPM` (75), `HEATUP_MIN_CHARGING_GPM` (20), `HEATUP_MAX_CHARGING_GPM` (130). |
| `CVCSController.cs` | Added `UpdatePostBubbleHeatup()` method: fixed 75 gpm letdown, variable 20–130 gpm charging via PI controller, level setpoint from unified program. Existing `Update()` preserved for at-power. |
| `HeatupSimEngine.CVCS.cs` | Restructured CVCS dispatch: post-bubble heatup routes to `UpdatePostBubbleHeatup()`; adaptive orifice lineup (`UpdateOrificeLineup()`) restricted to at-power phase only. |

### Physics

- Letdown fixed at 75 gpm throughout post-bubble heatup (was 12–120 gpm depending on pressure/orifice lineup)
- PI controller modulates charging against stable letdown reference → improved control stability
- During thermal expansion: PI reduces charging below 75 gpm → net negative CVCS flow drains excess inventory via VCT → BRS holdup tanks
- ~30,000 gallons removed during full heatup (tracked via `brsState.CumulativeIn_gal`)
- Pressure control unchanged — follows T_sat naturally, PID at 2235 psig (already correct from v4.4.0)

### Note

Validation criterion "Level setpoint = 25% at T_avg = 547°F" not met — current heatup level program gives ~59% at that temperature. Discontinuity at 557°F between heatup program (60%) and at-power program (25%) documented as technical debt in Future Enhancements Roadmap.

### Source

NRC HRTD ML11223A287 Section 10.2 — Pressure control; NRC HRTD ML11223A290 Section 10.3 — Level program, constant letdown / variable charging; NRC HRTD ML11223A214 Section 4.1 — CVCS flow balance

---

## Stage 8: Mass Conservation Audit

### Summary

Enhanced mass conservation accounting to include SG secondary steam mass in the total system mass balance. Steam that exits via MSIVs/steam dumps is tracked as "mass removed from SG" and included in the conservation equation so that mass is properly accounted for across all three SG thermal regimes.

### Files Modified

| File | Change |
|------|--------|
| `HeatupSimEngine.Logging.cs` | Enhanced mass audit to include SG secondary water mass and cumulative steam produced. Total system mass = RCS_water + PZR_water + PZR_steam + VCT + BRS + SG_secondary_water + SG_steam_produced + SG_drained. |
| `HeatupSimEngine.BubbleFormation.cs` | Mass balance snapshot at bubble formation transition includes SG state. |
| `HeatupSimEngine.cs` | Per-timestep mass continuity check includes SG terms. Flag logged for any step where delta exceeds 100 lbm. |

### Physics

- Conservation equation: M_total = M_rcs + M_pzr + M_vct + M_brs + M_sg_water + M_sg_steam_exited + M_sg_drained = constant
- SG steam that exits the system is mass that has left the secondary — must be tracked to close the balance
- SG draining mass also tracked separately
- Target: < 2% mass conservation error at all times, no spikes > 5% at transitions

### Source

First principles — mass conservation across open and closed system boundaries

---

## Cumulative Files Modified

| File | Stages |
|------|--------|
| `SGMultiNodeThermal.cs` | 1, 2, 3, 4 |
| `PlantConstants.SG.cs` | 1, 2, 3, 4 |
| `PlantConstants.Pressure.cs` | 6 |
| `PlantConstants.CVCS.cs` | 7 |
| `WaterProperties.cs` | 2 |
| `CVCSController.cs` | 7 |
| `SteamDumpController.cs` | 3 |
| `HeatupSimEngine.cs` | 3, 4, 5, 6, 8 |
| `HeatupSimEngine.Init.cs` | 6 |
| `HeatupSimEngine.CVCS.cs` | 5, 7 |
| `HeatupSimEngine.HZP.cs` | 3 |
| `HeatupSimEngine.Logging.cs` | 8 |
| `HeatupSimEngine.BubbleFormation.cs` | 8 |

---

## Energy Balance Validation (Full Heatup)

| Parameter | Value | Source |
|-----------|-------|--------|
| RCP total heat input | 21 MW | NRC HRTD |
| Q for 50°F/hr primary heatup | 14.3 MW | Primary mass × 50 / 3.412e6 |
| System losses (ambient + letdown) | ~1.5 MW | Engineering estimate |
| Q available for SG / steam exit | ~5.2 MW | 21 − 14.3 − 1.5 |
| Total heatup time (100→557°F) | ~9.1 hours | 457°F / 50°F/hr |
| Total steam produced (Regime 2) | ~149,000 lb | 5.2 MW × 6.7 hr / ~800 BTU/lb avg h_fg |
| Steam as % of SG inventory | ~9% | 149k / 1,660k |

---

## Heatup Profile (Three Regimes)

| Regime | Temperature Range | Duration | SG Secondary Behavior |
|--------|------------------|----------|-----------------------|
| 1 — Subcooled | 100→220°F | ~2.4 hr | Closed stagnant pool, N₂ blanket at 17 psia. Thermocline model. Sensible heating only. |
| 2 — Boiling | 220→557°F | ~6.7 hr | Open system. Steam produced at tube surfaces exits via MSIVs. Secondary T tracks T_sat(P). P rises 0→1092 psig. |
| 3 — Steam Dump | At 557°F | Continuous | Steam dumps hold 1092 psig. All excess RCP heat dumped to condenser. T_rcs stabilizes at 557°F (HZP). |

---

## Known Limitations

| Item | Description | Tracking |
|------|-------------|----------|
| Secondary mass conservation | No feedwater/condensate return modeled — mass only decreases from draining and boiloff | Future Features Priority 5 (Screen 5) |
| Heatup level program discontinuity | 60% at 557°F (heatup) drops to 25% at 557°F (at-power) — 35% gap | Technical Debt |
| Per-SG modeling | All 4 SGs treated as identical (lumped) | Future Features Priority 13 |
| Steam line warming | Simplified via pressure rate limit rather than explicit thermal model | Deferred |

---

## Technical References

| Document | ID | Relevance |
|----------|----|-----------|
| NRC HRTD Section 19.0 — Plant Operations | ML11223A342 | Three-phase heatup, RHR isolation, SG draining, steam dump termination |
| NRC HRTD Chapter 17.0 — Plant Operations | ML023040268 | Initial pressurization from 50–100 psig |
| NRC HRTD Section 10.2 — PZR Pressure Control | ML11223A287 | PID controller, pressure follows T_sat during heatup |
| NRC HRTD Section 10.3 — PZR Level Control | ML11223A290 | Level program, constant letdown / variable charging |
| NRC HRTD Section 4.1 — CVCS | ML11223A214 | Flow balance, CCP capacity, orifice lineup |
| NRC HRTD Section 11.2 — Steam Dump | ML11223A294 | Steam pressure mode, 1092 psig setpoint |
| NRC HRTD Section 7.1 — Main Steam | ML11223A244 | SG safety valves |
| NRC HRTD Section 2.3 — SG Systems | ML11251A016 | SG wet layup, draining procedures |
| NRC HRTD Section 5.0 — Steam Generators | ML11223A213 | Model F SG design data |
| NUREG/CR-5426 | — | SG natural circulation phenomena |
| Incropera & DeWitt, 7th ed. | — | Ch. 9 (Natural Convection), Ch. 10 (Boiling) |
| NIST Steam Tables | — | h_fg, T_sat, P_sat reference data |
| SG_HEATUP_BREAKTHROUGH_HANDOFF.md | Local | Full analysis of 2000+ simulation runs |
