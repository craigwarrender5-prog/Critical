# CRITICAL: Master the Atom — Changelog

## [0.2.0] — 2026-02-07

### Overview
Major rework of bubble formation physics and pressurizer heater control, replacing
the mechanical drain model with a thermodynamic steam-displacement model and
implementing a multi-mode heater controller with pressure-rate feedback. All changes
per NRC HRTD 19.2.2 / 2.1 / 6.1 / 10.2 and the approved design document
`DESIGN_BubbleFormation_HeaterControl.md`.

This release addresses the two fundamental issues identified in the design review:
1. Bubble formation was driven by CVCS flow imbalance (mechanical) — now driven by
   thermodynamic steam generation at T_sat (heaters supply latent heat, steam
   displaces water downward through surge line).
2. Heaters were fixed 1.8 MW all-on/all-off — now continuously variable with
   pressure-rate feedback during startup phases.

**GOLD STANDARD modules unchanged:** SolidPlantPressure.cs, WaterProperties.cs,
VCTPhysics.cs, and all other GOLD STANDARD modules listed in v0.1.0.

**Design document:** `DESIGN_BubbleFormation_HeaterControl.md` (Updates and Changelog/)

---

### Added

#### Constants — PlantConstants.cs
- **CCP region** (3 constants): `CCP_CAPACITY_GPM` (44 gpm), `CCP_WITH_SEALS_GPM` (87 gpm), `CCP_START_LEVEL` (80%) per NRC HRTD 4.1 / 19.2.2
- **Heater Control region** (10 constants): Startup max pressure rate (100 psi/hr), min power fraction (0.2), proportional/backup heater setpoints (2225/2275/2200/2225 psia), spray start/full setpoints (2260/2310 psig), spray bypass flow (1.5 gpm), ambient heat loss (42.5 kW) per NRC HRTD 6.1 / 10.2
- **Aux Spray Test region** (5 constants): Test duration (45 sec), min/max pressure drop (5/15 psi), aux spray flow (25 gpm), recovery time (150 sec) per NRC HRTD 19.2.2
- Validation checks for CCP capacity ordering, drain charging consistency, CCP start vs drain target ordering, heater control setpoint ordering

#### Heater Control — CVCSController.cs
- `HeaterMode` enum: STARTUP_FULL_POWER, BUBBLE_FORMATION_AUTO, PRESSURIZE_AUTO, AUTOMATIC_PID (future), OFF
- `HeaterFraction` field in `HeaterControlState` — power as fraction of maximum (0-1)
- `Mode` field in `HeaterControlState` — tracks current operating mode
- Multi-mode `CalculateHeaterState()` with pressure-rate feedback:
  - STARTUP_FULL_POWER: All groups at 1800 kW, no modulation
  - BUBBLE_FORMATION_AUTO: Linear power reduction when dP/dt exceeds 100 psi/hr, 20% minimum floor, backup groups shed first
  - PRESSURIZE_AUTO: Same pressure-rate controller, targeting ≥320 psig for RCP NPSH
  - AUTOMATIC_PID: Placeholder for future proportional/backup staging at 2235 psig

#### Bubble Formation State Machine — HeatupSimEngine.cs
- CCP tracking state: `ccpStarted`, `ccpStartTime`, `ccpStartLevel`
- Heater mode tracking: `currentHeaterMode`
- Aux spray test state: `auxSprayActive`, `auxSprayStartTime`, `auxSprayPressureBefore`, `auxSprayPressureDrop`, `auxSprayTestPassed`

---

### Changed

#### Bubble Formation Drain — Mechanical → Thermodynamic (HeatupSimEngine.cs)

**Previous model (v0.1.0):** Letdown set to 120 gpm, charging held at 75 gpm, creating 45 gpm net outflow that mechanically drained the PZR. Steam was a consequence of emptying.

**New model (v0.2.0):** Two-mechanism thermodynamic drain per NRC HRTD 19.2.2 / 2.1:
- **Primary:** Steam generation from heater power. Heaters supply latent heat of vaporization at T_sat. Steam forms at liquid surface and displaces water downward. `dV_steam = (Q_heaters / h_fg) × (1/ρ_steam - 1/ρ_water) × dt`
- **Secondary:** CVCS trim via letdown/charging imbalance. Letdown at 75 gpm (RHR crossconnect, unchanged throughout Phase 2). Charging at 0 gpm initially → 44 gpm when CCP starts at level < 80%.

#### CVCS Flow Sequence — Corrected (HeatupSimEngine.cs)

| Parameter | v0.1.0 (was) | v0.2.0 (now) | Source |
|-----------|-------------|-------------|--------|
| Pre-bubble charging | 75 gpm | 0 gpm (no CCP) | NRC HRTD 19.2.2 |
| CCP start trigger | At drain start | Level < 80% | NRC HRTD 19.2.2 |
| CCP flow rate | 75 gpm | 44 gpm | NRC HRTD 4.1 |
| Letdown during drain | 120 gpm | 75 gpm (RHR crossconnect) | NRC HRTD 19.0 |
| Net outflow (pre-CCP) | 45 gpm | 75 gpm | Calculated |
| Net outflow (post-CCP) | 45 gpm | 31 gpm | Calculated |
| Drain driving force | CVCS only | Steam displacement + CVCS | NRC HRTD 2.1 |

#### Bubble Drain Constants — PlantConstants.cs

- `BUBBLE_DRAIN_LETDOWN_GPM`: 120 → 75 gpm (RHR crossconnect rate, not increased)
- `BUBBLE_DRAIN_CHARGING_GPM` **removed**, replaced by:
  - `BUBBLE_DRAIN_CHARGING_INITIAL_GPM` = 0 gpm (no CCP at drain start)
  - `BUBBLE_DRAIN_CHARGING_CCP_GPM` = 44 gpm (CCP capacity per NRC HRTD 4.1)

#### Aux Spray Verification — Modeled (HeatupSimEngine.cs)

Previous: VERIFICATION phase was a passive 5-minute timer.
Now: Models actual aux spray test per NRC HRTD 19.2.2:
- Spray initiated at start of VERIFICATION, 45-second duration
- Condensation reduces pressure at ~0.25 psi/sec
- Test result logged: PASS if 5-15 psi drop, MARGINAL otherwise
- Pressure recovers naturally via heater action after spray secured

#### Heater Mode Transitions — HeatupSimEngine.cs

- Solid plant ops: STARTUP_FULL_POWER (1800 kW, no feedback)
- DRAIN phase entry: Transitions to BUBBLE_FORMATION_AUTO (pressure-rate feedback)
- STABILIZE → PRESSURIZE: Transitions to PRESSURIZE_AUTO
- Heater control call now passes `currentHeaterMode` and `pressureRate` to CVCSController

#### VCT Flow Tracking — HeatupSimEngine.cs

- VCT now sees actual drain flows (letdown=75, charging=0/44) instead of balanced flows
- VCT level correctly rises during drain (letdown > charging returns water to VCT)
- Previous model routed excess to holdup tanks; new model uses correct 75 gpm RHR rate

---

### Fixed

- **Mechanical drain model replaced** — Root cause: CVCS flow imbalance was sole drain mechanism, not matching NRC procedure where steam displacement is primary. Fixed by implementing thermodynamic steam generation physics (Q_heaters / h_fg → steam volume displacement).
- **CCP start sequence corrected** — CCP was running from drain start at 75 gpm. Now starts at 0 gpm, CCP triggered when PZR level drops below 80% at 44 gpm per NRC HRTD 4.1/19.2.2.
- **Heater control now responsive** — Fixed all-on/all-off behavior. Heaters now modulate with pressure-rate feedback during bubble formation and pressurization phases.
- **Letdown rate corrected during drain** — Was 120 gpm (mechanical drain model). Now 75 gpm (RHR crossconnect throughout Phase 2 per NRC HRTD 19.0).

---

### GOLD Standard Module Status

No GOLD STANDARD modules were modified. All changes confined to:
- PlantConstants.cs (constant additions and corrections — no physics calculations)
- CVCSController.cs (heater control rework — new multi-mode controller)
- HeatupSimEngine.cs (bubble formation state machine rework)

GOLD STANDARD modules confirmed unchanged:
SolidPlantPressure.cs, WaterProperties.cs, VCTPhysics.cs, SteamThermodynamics.cs,
ThermalMass.cs, ThermalExpansion.cs, FluidFlow.cs, ReactorKinetics.cs,
PressurizerPhysics.cs, CoupledThermo.cs, HeatTransfer.cs, RCSHeatup.cs,
and all other modules listed in v0.1.0 GOLD Standard status.

---

### Validation Criteria (from Design Document Section 7)

| Criterion | Target | Notes |
|-----------|--------|-------|
| Drain timeline | 30-60 min | Steam displacement + CVCS trim |
| CCP start | Level < 80% | Automatic trigger |
| CVCS flows | LD=75, CHG=0→44 gpm | Per NRC HRTD 4.1/19.2.2 |
| Pressure rate during drain | 50-150 psi/hr | Heater auto-modulation |
| Drain target | 25% PZR level | Per NRC HRTD 19.2.2 |
| Mass conservation | < 10 gal error | VCT tracking |
| Aux spray test | 5-15 psi drop | 45-sec test, recovery 2-3 min |
| Letdown path | RHR crossconnect throughout | Per NRC HRTD 19.0 |

### Requires Simulation Run

- Full cold shutdown → HZP heatup with thermodynamic drain model
- Verify drain timeline falls within 30-60 minute window
- Verify CCP starts at correct level threshold
- Verify aux spray pressure drop in expected 5-15 psi range
- Verify heater modulation engages during pressure transients
- Verify VCT level rises during drain (correct direction)
- Verify mass conservation error remains < 10 gal

---

### Phase 3 Boundary Notes (unchanged from design document)

Out of scope for v0.2.0:
1. RCP start sequence (Phase 3)
2. Seal injection transition (44 gpm → 55+32 gpm)
3. Letdown path transition (RHR crossconnect → normal orifices)
4. RHR isolation
5. Normal letdown establishment
6. Full automatic pressure control at 2235 psig
7. Manual operator heater control for gameplay

---

## NRC / Industry References (new for v0.2.0)

| Document | Usage in v0.2.0 |
|----------|----------------|
| NRC HRTD 2.1 | Steam displacement mechanism — "Steam formation begins when the pressurizer water temperature is at saturation" |
| NRC HRTD 4.1 | CCP capacity (44 gpm), CVCS flow balance |
| NRC HRTD 6.1 | Heater group capacities, proportional/backup staging |
| NRC HRTD 10.2 | Pressure control setpoints (proportional, backup, spray) |
| NRC HRTD 19.0 | RHR crossconnect letdown path (75 gpm throughout Mode 5) |
| NRC HRTD 19.2.2 | Bubble formation procedure, CCP start, aux spray test, drain sequence |
