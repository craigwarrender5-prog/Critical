# Future Features — Dashboard Parameter Gaps

**Created:** 2026-02-16  
**Source:** IP-0025 Section 7 (Unaddressed Issues)  
**Purpose:** Track simulation engine parameters that are requested for the Validation Dashboard but are not yet modeled in `HeatupSimEngine` or the `Critical.Physics` modules.

---

## Priority: Power Operations Phase (Post-HZP)

These parameters require a fission power model and are not applicable during Cold Shutdown → HZP heatup:

- **Reactor power (% / MWt)** — Requires criticality and neutron kinetics model
- **Decay heat (MW)** — Requires separate decay heat curve model
- **Reactivity breakdown** — Rod worth, rod position, boron worth, moderator temperature coefficient, Doppler feedback
- **Feedwater flow** — Secondary side feed during power ops
- **Steam flow to turbine** — Not steam dump; actual turbine steam

---

## Priority: Multi-Loop Model

Current engine uses a single equivalent loop. These require per-loop modeling:

- **Per-loop hot/cold leg temperatures (A/B/C/D)**
- **Per-loop mass flow**
- **SG primary inlet/outlet temps per loop**
- **Per-loop ΔT**

---

## Priority: RCP Electrical Model

Current RCP model is staged ramp (on/off with timing). Detailed electrical modeling needed for:

- **RCP speed (%)**
- **RCP torque / amps**
- **RCP pump head (ΔP across pump)**

---

## Priority: CVCS Detail Model

Several CVCS parameters lack dedicated tracking:

- **Charging pump individual status** (currently single aggregate flow)
- **Charging temperature** (not tracked)
- **Charging/Letdown line pressure** (not modeled)
- **Letdown orifice individual states** (internal to CVCSController, not exposed)
- **Letdown temperature (hot side)** (not tracked)
- **Heater group breakdown** (currently single aggregate output)

---

## Priority: VCT/BRS Thermal & Chemistry

- **VCT temperature** (assumed constant ~100°F)
- **VCT pressure / gas blanket pressure** (not modeled)
- **VCT NPSH indicator for charging pumps** (not modeled)
- **BRS tank boron concentration** (volumes tracked but not per-tank chemistry)

---

## Priority: RHR Detail Model

- **RHR suction/discharge alignment valve states** (mode string available, not valve lineup)
- **RHR isolation valve individual states**
- **RHR line pressure / ΔP**
- **RHR HX inlet/outlet temperatures individually**
- **RHR interlock / permissive status**
- **RHR minimum flow protection active**

---

## Priority: Interlock & Protection Models

- **Heater inhibited reason flags** (not modeled as discrete flags)
- **Spray inhibited reason flags** (not modeled as discrete flags)
- **RHR unavailable reason flags** (not modeled as discrete flags)
- **Formal dP/dt and dT/dt limit tracking with exceeded indicators** (rates available, formal limit logic needed)

---

## Priority: Thermal Detail

- **Spray inlet temperature** (not tracked)
- **Surge line temperature** (assumed RCS average)
- **Surge line ΔP** (not modeled)
- **PZR subcooling/superheat separated for liquid vs steam spaces**
- **Void fraction outside PZR** (assumed zero during heatup)

---

## Priority: Conservation & Stability

- **Energy conservation error (instant + cumulative)** — Not tracked as separate metric
- **Integration stability flags** — Partially available via `pzrClosure*` diagnostics; need formal divergence/NaN/clamp tracking

---

## Derivable from Existing Fields (Low Effort)

These can be computed in the Dashboard's `DashboardSnapshot` without engine changes:

- **Core ΔT** = `T_hot - T_cold`
- **Net CVCS flow** = `chargingFlow - letdownFlow`
- **PZR net energy** = heater input + surge enthalpy - spray cooling - insulation losses
- **Core flow (approximate)** = `rcpContribution.EffectiveFlow_gpm`
- **Pressure error** = setpoint - `pressure`
- **Level error** = `pzrLevelSetpointDisplay` - `pzrLevel`
- **Natural circulation indicator** = derived from `rcpCount == 0` and `rhrActive`
