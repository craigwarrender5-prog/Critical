# Implementation Plan v5.0.0 — Startup Sequence Realism Overhaul

**Date:** 2026-02-11  
**Version:** 5.0.0  
**Type:** MAJOR — Physics Model Correction + New Simulation Phases  
**Predecessor:** v4.4.0 (PZR Level/Pressure Control Fix)

---

## Problem Summary

Analysis of heatup simulation logs (0.25hr–18.25hr) against NRC HRTD Section 19.0 (ML11223A342) and Section 17.0 (ML023040268) revealed **five critical discrepancies** between the simulation's startup sequence and real Westinghouse 4-Loop PWR procedures. These affect physical realism from the very start of the simulation through HZP stabilization.

### Issues Identified

| # | Issue | Severity | Current Behavior | Expected Behavior (NRC) |
|---|-------|----------|------------------|------------------------|
| 1 | **Missing RCS pressurization phase** | CRITICAL | Sim starts at 365 psia (350 psig) already established | RCS starts at 50–100 psig after fill/vent; must pressurize to 320–400 psig via charging > letdown BEFORE PZR heaters |
| 2 | **SG secondary heating physically unrealistic** | CRITICAL | 326°F stratification (top 494°F, bottom 168°F) at 18.25hr with 4 RCPs forcing primary flow | With forced primary circulation, SG secondary should heat relatively uniformly; top-bottom ΔT should be 20–40°F, not 326°F |
| 3 | **SG secondary pressure model inconsistent** | HIGH | Pressure drops from 118 psia at 15hr to 17 psia at 18.25hr despite top node at 494°F | Pressure should follow saturation curve as secondary heats; steam dumps actuate at 1092 psig (557°F T_sat) to cap heatup |
| 4 | **RHR isolation timing too early** | HIGH | RHR isolates when `rcpCount > 0` (first RCP start at T_rcs ~153°F) | NRC: RHR isolated at ~350°F when RCS enters Mode 3, shifted to ECCS lineup |
| 5 | **Mass conservation error — 9% spike at RCP start** | HIGH | 73,104 lbm (9.0%) error at 10hr coinciding with RCP startup and bubble formation | Error should remain < 1% throughout; likely double-counting during solid→two-phase transition |
| 6 | **Post-bubble pressure/level control philosophy wrong** | MEDIUM | Pressure and level controlled by ad-hoc charging/letdown manipulation | NRC HRTD 10.2/10.3: Pressure follows T_sat naturally during heatup; level AUTO (charging varies, letdown constant 75 gpm); spray/heater AUTO only at 2235 psig |

### Prioritized Fix Order

Fixes are ordered to build on each other — each stage establishes foundations the next stage requires:

1. **Stage 1:** SG secondary heating model fix (most impactful physics error)
2. **Stage 2:** SG secondary pressure model fix (depends on correct SG thermal state)
3. **Stage 3:** RHR isolation timing correction (straightforward, independent)
4. **Stage 4:** Initial RCS pressurization phase (new simulation phase)
5. **Stage 5:** Post-bubble level/pressure control philosophy (NRC HRTD 10.2/10.3 alignment)
6. **Stage 6:** Mass conservation audit and fix (diagnostic, builds on all prior fixes)

---

## Expectations (Correct/Realistic Behavior)

### SG Secondary During Heatup (per NRC HRTD 19.0)

- SG secondary starts in wet layup: filled to 100% with water at ambient temperature
- With 4 RCPs forcing ~90,000 gpm through primary tubes, the **entire tube bundle** transfers heat
- Secondary water heats via natural convection on tube outer surfaces
- At ~220°F RCS, steam formation begins in SGs; nitrogen supply isolated
- SG secondary pressure builds from atmospheric following saturation curve
- Steam dumps actuate at 1092 psig (T_sat = 557°F) to cap RCS heatup at no-load T_avg
- Top-to-bottom stratification with forced primary flow: **20–40°F maximum** (not 326°F)

### RHR Isolation (per NRC HRTD 19.0)

- RHR remains connected through early heatup for letdown path and cleanup
- At T_avg ≥ 350°F: RHR isolated from RCS, shifted to ECCS (at-power) lineup
- Normal letdown orifices take over from RHR cross-connect (HCV-128)
- RHR isolation pressure interlock at 585 psig is backup, not primary trigger

### Initial Pressurization (per NRC HRTD 17.0)

- After fill/vent: RCS at 50–100 psig, pressurizer solid (100% water), T_avg ~120°F
- Operator increases charging > letdown to pressurize the incompressible system
- Target: 400–425 psig (satisfies RCP NPSH; below 425 psig RHR suction limit)
- Rate: approximately +20–40 gpm net, taking 1–3 hours depending on net flow
- Throttle charging once at target pressure to maintain 320–400 psig band
- Then energize PZR heaters and begin PZR heatup to saturation

### Post-Bubble Pressure Control (per NRC HRTD 10.2)

- Pressure is NOT actively controlled to a target during heatup
- Pressure rises naturally as PZR temperature (and T_sat) increases from heatup
- Heaters maintain PZR water at saturation; pressure follows T_sat curve
- Spray/heater automatic control engages only when pressure reaches 2235 psig
- PID master controller: proportional heaters modulate around 2235 psig, spray opens at +25 psig, backup heaters at -25 psig

### Post-Bubble Level Control (per NRC HRTD 10.3)

- Level controller in AUTO with programmed setpoint based on T_avg (auctioneered high)
- Letdown constant at 75 gpm (one 75-gpm orifice normally in service)
- Charging flow varies via PI controller (FCV-121 or PD pump speed)
- Level program: 25% at no-load → 61.5% at full power (follows RCS thermal expansion)
- Excess RCS inventory (from thermal expansion) drains to VCT → holdup tanks
- ~30,000 gallons removed during full cold→hot heatup

---

## Proposed Fix — Detailed Technical Plan

### Stage 1: SG Secondary Heating Model Correction

**Problem:** SGMultiNodeThermal.cs uses a thermocline model where only the top node gets meaningful HTC (200 BTU/hr-ft²-°F), while bottom 4 nodes stay at stagnant HTC (8 BTU/hr-ft²-°F). ActiveArea stays at 3–8%. This was appropriate for **no-RCP stagnant secondary conditions**, but is completely wrong once RCPs are driving forced primary flow through the full tube bundle.

**Root cause:** The thermocline model does not distinguish between no-RCP and RCP-running conditions. With forced primary circulation, the full tube height participates in heat transfer — there is no thermocline limitation because primary-side flow is forced, not relying on secondary natural circulation.

**Fix approach:**

1. **Add RCP-aware heat transfer mode to SGMultiNodeThermal:**
   - When `rcpCount == 0`: Keep current thermocline model (stagnant secondary, only top participates)
   - When `rcpCount > 0`: Switch to **forced-primary/natural-secondary** mode:
     - All nodes participate with full geometric area (not limited by thermocline)
     - HTC for each node based on natural convection on tube exterior (Churchill-Chu for horizontal cylinder), driven by `(T_rcs - T_node_i)` local ΔT
     - As secondary heats, ΔT decreases → HTC and Q decrease naturally
     - When any node reaches T_sat (at local pressure): boiling HTC takes over (Rohsenow/Chen)

2. **Implement secondary-side natural circulation mixing:**
   - With forced primary flow heating ALL tubes uniformly, the secondary develops buoyancy-driven flow:
     - Hot water rises from tube bundle region
     - Cooler water descends in downcomer (annular gap between tube bundle and shell)
     - This creates mixing that limits stratification to 20–40°F
   - Model as inter-node mixing coefficient proportional to Grashof number
   - Mixing rate increases with ΔT between nodes → self-limiting stratification

3. **Recalibrate validation targets:**
   - Early heatup (100–200°F): SG absorbs 2–5 MW total (4 SGs), top-bottom ΔT < 30°F
   - Mid heatup (200–350°F): SG absorbs 3–8 MW, boiling onset at top ~220°F
   - Late heatup (350–557°F): SG absorbs 5–12 MW with boiling enhancement
   - Net RCS heatup rate: 45–55°F/hr with 21 MW RCP input (NRC HRTD target)

**Files modified:**
- `SGMultiNodeThermal.cs` — Add forced-primary mode, inter-node mixing
- `PlantConstants.SG.cs` — Add mixing coefficients, forced-primary HTC parameters
- `HeatupSimEngine.cs` — No changes needed (already passes rcpCount to SG model)

**Validation criteria:**
- [ ] With 4 RCPs: top-bottom ΔT < 40°F at all times
- [ ] SG secondary average temperature tracks within 100°F of T_rcs during heatup
- [ ] Net RCS heatup rate 45–55°F/hr
- [ ] Boiling onset in SG secondary at T_rcs ≈ 220°F
- [ ] Energy balance: RCP heat - SG absorption - losses ≈ RCS thermal mass × heatup rate

---

### Stage 2: SG Secondary Pressure Model Fix

**Problem:** SG secondary pressure inconsistent — drops from 118 psia at 15hr to 17 psia at 18.25hr despite top node at 494°F. The v4.3.0 model has a deficiency where pressure calculation breaks down when node temperatures diverge wildly (a symptom of Stage 1's stratification error).

**Fix approach:**

1. **Pressure tracks bulk average saturation state:**
   - With Stage 1 fix providing realistic uniform heating, use bulk average secondary temperature to compute saturation pressure
   - Below boiling point: atmospheric + N₂ blanket pressure (~17 psia as currently modeled)
   - At boiling: P_secondary = P_sat(T_bulk_secondary) with vapor generation driving pressure up
   - N₂ blanket isolated at ~220°F RCS temperature per NRC procedure

2. **Steam dump actuation at 1092 psig:**
   - When SG secondary pressure reaches 1092 psig (T_sat = 557°F), steam dumps modulate open
   - This caps RCS heatup at no-load T_avg (547–557°F)
   - Existing SteamDumpController.cs can be wired to SG pressure (currently uses T_avg)
   - Mode: Steam Pressure Control (not T_avg control during heatup)

3. **Steam line warming:**
   - MSIVs open when steam available during heatup
   - Steam admitted to individual steam lines up to turbine stop valves
   - Condensate drained to condenser via drain traps

**Files modified:**
- `SGMultiNodeThermal.cs` — Fix pressure calculation to use bulk average
- `SteamDumpController.cs` — Add Steam Pressure control mode for heatup
- `PlantConstants.SteamDump.cs` — Add STEAM_PRESSURE_SETPOINT_PSIG = 1092
- `HeatupSimEngine.cs` — Wire steam dump to SG pressure during heatup

**Validation criteria:**
- [ ] SG pressure follows saturation curve as secondary heats
- [ ] Below boiling: P_secondary ≈ 17 psia (N₂ blanket + atmospheric)
- [ ] N₂ isolated at T_rcs ≈ 220°F
- [ ] Steam dumps actuate at 1092 psig
- [ ] RCS T_avg caps at 547–557°F when steam dumps active

---

### Stage 3: RHR Isolation Timing Correction

**Problem:** Current code isolates RHR when `rcpCount > 0` (first RCP start, T_rcs ~153°F). NRC HRTD 19.0 states RHR is isolated at ~350°F (Mode 3 entry).

**Fix approach:**

1. **Change RHR isolation trigger:**
   - Remove: `if (rcpCount > 0 && rhrState.Mode == RHRMode.Heatup)`
   - Replace with: `if (T_rcs >= 350f && rhrState.Mode == RHRMode.Heatup)`
   - Keep existing pressure interlock at 585 psig as backup safety interlock

2. **Letdown path transition:**
   - When T_rcs < 350°F: Letdown via RHR cross-connect (HCV-128), even with RCPs running
   - At T_rcs ≥ 350°F: Isolate RHR, shift to normal letdown orifices
   - Log letdown path change event

3. **ECCS lineup:**
   - On RHR isolation: Log "RHR isolated — aligned for ECCS (at-power lineup)"
   - This is display/logging only; ECCS model is out of scope

**Files modified:**
- `HeatupSimEngine.cs` — Change RHR isolation condition in StepSimulation section 1C
- `HeatupSimEngine.CVCS.cs` — Update letdown path transition logic (if tied to RHR isolation)

**Validation criteria:**
- [ ] RHR stays connected until T_rcs ≥ 350°F
- [ ] Letdown via RHR path during early heatup with RCPs running
- [ ] Smooth transition to orifice letdown at 350°F
- [ ] Event log shows correct timing for RHR isolation

---

### Stage 4: Initial RCS Pressurization Phase

**Problem:** Simulation starts at 365 psia (350 psig) with heaters already energized. The real plant starts at 50–100 psig after fill/vent and must pressurize to 320–400 psig via charging/letdown imbalance before PZR heaters are energized.

**Fix approach:**

1. **New simulation phase: PHASE_PRESSURIZE**
   - Add new `StartupPhase` enum state before the existing PHASE_SOLID flow
   - Initial conditions: T_rcs = 120°F, P = 115 psia (100 psig), PZR solid, heaters OFF
   - One CCP running (from RWST via VCT), letdown via RHR cross-connect

2. **Pressurization physics:**
   - Net charging flow: +20–40 gpm (charging ~95 gpm, letdown ~60–75 gpm via HCV-128)
   - Pressure response: ΔP/Δt = (net_flow × ρ) / (V_rcs × β_compressibility)
   - Target: 400 psig (415 psia) — satisfies RCP NPSH
   - Must stay below 425 psig (RHR suction relief valve setpoint)
   - SolidPlantPressure module already handles P-V coupling for incompressible water

3. **Operator control band:**
   - Once pressure reaches 400 psig: throttle net charging to maintain 320–400 psig band
   - Existing SolidPlantPressure controller can be adapted for this

4. **Transition to heater phase:**
   - When pressure stable at 320–400 psig: LOG "PRESSURIZATION COMPLETE"
   - Energize PZR heaters (transition to existing PHASE_SOLID / bubble formation)
   - Begin PZR heatup toward saturation (existing code)

5. **Update initial conditions:**
   - `startPressure` default: 115f (100 psig) instead of 400f
   - `startTemperature` default: 120f (cold shutdown T_avg per NRC HRTD 17.0)
   - Add Inspector toggle: `skipPressurizationPhase` for quick-start testing

**Files modified:**
- `HeatupSimEngine.Init.cs` — New initial conditions, pressurization phase setup
- `HeatupSimEngine.cs` — New PHASE_PRESSURIZE handling in StepSimulation
- `SolidPlantPressure.cs` — Adapt for low-pressure pressurization control band
- `PlantConstants.Pressure.cs` — Add initial pressurization constants
- `PlantConstants.cs` — Update default initial conditions

**Validation criteria:**
- [ ] Simulation starts at 100 psig, heaters OFF
- [ ] Pressure rises at ~50–100 psi/hr from net +20–40 gpm charging
- [ ] Pressure stabilizes in 320–400 psig band
- [ ] PZR heaters energize only after pressurization complete
- [ ] Total pressurization time: 1–3 hours (depending on net flow rate)
- [ ] No RCS overpressure (stays below 425 psig)

---

### Stage 5: Post-Bubble Level/Pressure Control Alignment

**Problem:** Current simulation uses ad-hoc charging/letdown adjustment for pressure and level control. NRC HRTD 10.2/10.3 describe a clear separation: pressure is controlled by heaters/spray, level is controlled by charging/letdown.

**Fix approach:**

1. **Pressure control philosophy (NRC HRTD 10.2 alignment):**
   - Post-bubble, pre-2235 psig: Pressure follows T_sat naturally as PZR heats
   - Heaters keep PZR at saturation; no active pressure setpoint control
   - At 2235 psig: Switch to AUTOMATIC_PID mode (existing v4.4.0 code)
   - This is mostly correct in current code; document and validate

2. **Level control philosophy (NRC HRTD 10.3 alignment):**
   - Post-bubble: Level controller in AUTO with programmed setpoint
   - Level program: f(T_avg) from 25% (no-load) to 61.5% (full power)
   - Letdown constant at 75 gpm (one orifice)
   - Charging varies via PI controller to match level setpoint
   - When level rises above setpoint (RCS expansion): controller reduces charging
   - Excess water flows: RCS → VCT → holdup tanks → ~30,000 gal total

3. **Validate existing `GetPZRLevelSetpointUnified()` against NRC program:**
   - Must return 25% at T_avg = 547°F (no-load)
   - Must return 61.5% at T_avg = 588.5°F (full power)
   - Linear interpolation between these points
   - Below no-load: floor at 25%

4. **Orifice lineup management:**
   - Normal: one 75-gpm orifice
   - During rapid expansion (high positive level trend): open second 75-gpm orifice
   - Existing v4.4.0 orifice lineup logic should be reviewed against this

**Files modified:**
- `CVCSController.cs` — Validate/refine PI level controller against NRC 10.3
- `PlantConstants.Pressurizer.cs` — Validate level program constants
- `HeatupSimEngine.CVCS.cs` — Ensure letdown stays constant, only charging varies

**Validation criteria:**
- [ ] Letdown constant at 75 gpm throughout post-bubble heatup
- [ ] Charging varies 20–130 gpm to maintain level at programmed setpoint
- [ ] Level setpoint = 25% at T_avg = 547°F
- [ ] Level setpoint = 61.5% at T_avg = 588.5°F
- [ ] ~30,000 gallons removed during full heatup (VCT → holdup → BRS)
- [ ] Pressure naturally follows T_sat curve (no target-seeking before 2235 psig)

---

### Stage 6: Mass Conservation Audit

**Problem:** 73,104 lbm (9.0%) mass conservation error at 10hr RCP start, partially recovering to 13,181 lbm (1.63%) by 18.25hr. Likely caused by accounting errors during solid→two-phase transition.

**Fix approach:**

1. **Audit mass balance at each phase transition:**
   - Solid plant → bubble formation: Track total mass before/after
   - Bubble drain phase: Verify drained mass equals VCT received mass
   - RCP start (regime transitions): Verify physicsState.RCSWaterMass continuity
   - Regime 1→2→3 transitions: Verify no mass creation/destruction

2. **Instrument mass conservation logging:**
   - Add per-timestep mass audit to log files: `RCS_mass + PZR_water + PZR_steam + VCT + BRS = constant`
   - Flag any step where delta exceeds 100 lbm
   - Track cumulative CVCS flow vs mass change

3. **Known suspect areas from v4.4.0:**
   - `regime3CVCSPreApplied` flag: Verify this correctly prevents double-counting
   - Solid plant → two-phase transition in BubbleFormation state machine
   - PZR volume calculation during bubble drain (level 100% → 25%)

**Note:** This stage is primarily diagnostic. Stages 1–5 may resolve some mass conservation issues by fixing the SG model (which currently creates unrealistic thermal loads) and the CVCS control philosophy. Run this stage AFTER Stages 1–5 to assess remaining errors.

**Files modified:**
- `HeatupSimEngine.Logging.cs` — Enhanced mass audit logging
- `HeatupSimEngine.BubbleFormation.cs` — Mass balance at phase transitions
- `HeatupSimEngine.cs` — Mass continuity checks at regime transitions

**Validation criteria:**
- [ ] Mass conservation error < 2% at all times during simulation
- [ ] No mass conservation spikes > 5% at any transition
- [ ] Cumulative CVCS flow matches RCS + VCT + BRS mass changes
- [ ] Per-timestep mass audit logged for diagnostic analysis

---

## Unaddressed Issues

| Issue | Reason | Future Plan |
|-------|--------|-------------|
| **Mode 5→4 temperature hold at ~200°F** | Separate procedural feature, not a physics fix | Future_Features PRIORITY 3 (already documented) |
| **Per-SG primary loop modeling** | Currently all 4 SGs lumped as single thermal model | Requires per-loop T-H model — Future_Features PRIORITY 13 |
| **SG wet layup drain procedure** | NRC: "At ~200°F, SG draining commenced through normal blowdown" | Future — requires SG inventory/level model |
| **Hydrogen blanket establishment in VCT** | NRC HRTD 17.0: N₂ purge → H₂ at ~200°F | Cosmetic/procedural — does not affect physics |
| **Oxygen scavenging (hydrazine addition)** | NRC: Must be in spec before 250°F | Chemistry model not in scope |
| **Accumulator alignment at 1000/1925 psig** | NRC: Open SIT valves at 1000 psig, verify ECCS at 1925 psig | ECCS model not in scope — Future_Features PRIORITY 14 |
| **Steam line warming (MSIV opening)** | Partially addressed in Stage 2 (steam dump) | Full steam line model deferred to turbine-generator scope |
| **Excess letdown path** | Future_Features PRIORITY 8 (v4.6.0) | Already planned |

All unaddressed issues that are planned for future release are confirmed present in `Critical\Updates\Future_Features\FUTURE_ENHANCEMENTS_ROADMAP.md` or will be added during changelog creation.

---

## Technical References

| Document | ID | Relevance |
|----------|----|-----------|
| NRC HRTD Section 19.0 — Plant Operations | ML11223A342 | Authoritative startup sequence, heatup procedure, RHR isolation timing |
| NRC HRTD Chapter 17.0 — Plant Operations (older) | ML023040268 | Detailed pressurization from 50–100 psig, RCP start sequence, bubble formation |
| NRC HRTD Section 10.2 — PZR Pressure Control | ML11223A287 | PID master controller, spray/heater staging, PORV setpoints, 2235 psig setpoint |
| NRC HRTD Section 10.3 — PZR Level Control | ML11223A290 | Level program (25–61.5%), constant letdown / variable charging, PI controller |
| NRC HRTD Section 4.1 — CVCS | ML11223A214 | Flow balance (87 gpm charging, 75 gpm letdown, 32 gpm seal), 3 orifice lineup |
| NRC HRTD Section 3.2 — RCS | ML11223A213 | Pressurizer design, safety valves, spray thermal sleeve |

---

## Estimated Effort

| Stage | Description | Effort | Dependencies |
|-------|-------------|--------|--------------|
| 1 | SG secondary heating model fix | 8–12 hr | None |
| 2 | SG secondary pressure model fix | 4–6 hr | Stage 1 |
| 3 | RHR isolation timing | 1–2 hr | None |
| 4 | Initial RCS pressurization phase | 6–8 hr | None (but benefits from Stage 3) |
| 5 | Post-bubble level/pressure control | 4–6 hr | Stage 1, 3 |
| 6 | Mass conservation audit | 4–6 hr | Stages 1–5 |
| **Total** | | **27–40 hr** | |

---

## Implementation Rules

- Implement **one stage at a time**
- **Stop and check** with user after each stage before proceeding
- Run simulation after each stage and produce updated log analysis
- Do not batch stages together
- Maintain GOLD standard on all modified modules
- Document all changes with NRC citation in code headers
