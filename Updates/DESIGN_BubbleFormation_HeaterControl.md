# CRITICAL: Master the Atom — Design Plan
# Corrected Bubble Formation Physics & Pressurizer Heater Control

**Date:** 2026-02-07
**Version:** v1.4.0.0 (Target)
**Type:** Major Build — Bubble Formation & Heater Physics Rework
**Status:** DESIGN APPROVED — Ready for Implementation

---

## 1. Problem Statement

The current bubble formation implementation has two fundamental issues that are tightly coupled:

### 1.1 Bubble Formation Is Mechanically Driven (Should Be Thermodynamic)

**Current code:** Sets letdown to 120 gpm and charging to 75 gpm, creating a forced 45 gpm net drain that mechanically empties the PZR from 100% to 25%. The bubble is a consequence of removing water — "pump the tub dry and steam fills the void."

**Real plant (NRC HRTD 19.2.2 / 2.1):** The bubble forms by thermodynamic action. Heaters bring PZR water to T_sat. Steam forms at the liquid surface. The steam displaces water downward through the surge line. Level drops as mass converts from dense liquid to less-dense vapor. The CVCS role is to *stabilize* the level once the bubble is established, not to create it.

Key NRC quote: *"Steam formation begins when the pressurizer water temperature is at saturation... A charging pump is started to maintain pressurizer level constant during bubble formation."* (HRTD 19.2.2)

### 1.2 Heaters Are "All On" / "All Off" With No Pressure Feedback

**Current code:** Heaters run at fixed 1.8 MW (all groups) from energization through bubble formation and beyond. The only control is the low-level interlock (< 17% trips heaters). No proportional pressure control, no backup heater staging, no operator-like modulation during startup.

**Real plant (NRC HRTD 6.1 / 10.2):** The Westinghouse pressurizer has a sophisticated two-group heater system:

| Group | Capacity | Control | Normal Setpoints |
|-------|----------|---------|------------------|
| Proportional | 2 banks × 150 kW = 300 kW | Proportional to pressure error | Full ON at 2225 psia, zero at 2275 psia |
| Backup | 4 banks × 300 kW = 1200 kW (or 5 × 300 = 1500 kW for 4-loop) | Bistable ON/OFF | ON at 2200 psia, OFF at 2225 psia |

During startup, *all groups are energized manually* to heat PZR water to T_sat as fast as possible (pre-bubble). After the bubble forms and the plant approaches normal operating pressure, heaters and sprays are placed in automatic control at 2235 psig.

---

## 2. Design Objectives

1. **Thermodynamic bubble formation** — Steam generation drives the PZR level change, not CVCS flow imbalance
2. **Realistic heater control** — Automatically modulated as a continuously variable block with pressure-rate feedback during startup; proportional/backup staging structure built for future manual operator mode
3. **Correct CVCS flow sequence** — Charging starts at 0 gpm (no pump), CCP starts when level drops below ~80%, flow rate matches real pump capacity (~44 gpm), role is level stabilization not draining
4. **Proper VCT tracking** — Mass conservation through the entire bubble formation sequence
5. **Aux spray verification** — Model the actual aux spray test that confirms compressible gas in PZR
6. **Maintain GOLD STANDARD** — No changes to validated physics modules (WaterProperties, ThermalExpansion, ThermalMass, SolidPlantPressure, etc.)

---

## 3. NRC Reference Data — Heater Control Modes

Source: NRC HRTD 6.1 (ML11251A021), 10.2 (ML11223A287), 2.1 (ML11251A014), 19.0 (ML11223A342)

### 3.1 Startup Phase: Manual Full Power (Pre-Bubble)

**Mode:** All heater groups energized manually.
**Power:** Full 1800 kW (300 kW proportional + 1500 kW backup).
**Duration:** ~3-4 hours to reach T_sat at initial pressure (~350 psig → T_sat ≈ 436°F).
**Current code status:** ✅ Correctly implemented.

### 3.2 Bubble Formation Phase: Automatic Pressure-Rate Modulated (During Drain)

**Mode:** Automatically modulated continuously variable heater block with pressure-rate feedback.
**Design Decision:** This release models heaters as a continuously variable block handled automatically by the system adjusting to pressure responses. Future release will add manual operator heater control for additional gameplay challenge.
**Current code status:** ❌ Fixed 1.8 MW, no modulation.

### 3.3 Pressurization Phase: Automatic Ramp to RCP NPSH

**Mode:** Same continuously variable pressure-rate controller.
**Target:** ≥320 psig (335 psia) for RCP NPSH.

> **Phase 3 Scope Note:** RCP start sequence modeled in Phase 3. This update ends at "RCP NPSH conditions met."

### 3.4 Normal Operations: Automatic Pressure Control

| Parameter | Setpoint |
|-----------|----------|
| Normal pressure | 2235 psig (2250 psia) |
| Proportional heaters full ON | 2225 psia |
| Proportional heaters zero | 2275 psia |
| Backup heaters ON (bistable) | 2200 psia |
| Backup heaters OFF (bistable) | 2225 psia |
| Spray valves start open | 2260 psig |
| Spray valves full open | 2310 psig |
| Ambient heat loss compensation | ~42.5 kW |

**Current code status:** ❌ Not implemented (beyond Phase 2 scope, constants defined for future).

---

## 4. NRC Reference Data — Bubble Formation Procedure

### 4.1 Real Plant Sequence

**Step 1 — Establish Letdown:** 75 gpm via RHR crossconnect (HCV-128). Charging: 0 gpm.
**Step 2 — Heaters to Tsat:** All groups energized. Solid plant pressure control.
**Step 3 — Steam Formation:** T_pzr reaches T_sat → level drops as mass converts liquid→vapor.
**Step 4 — CCP Starts (Level < 80%):** One CCP at 44 gpm. Net outflow: 31 gpm.
**Step 5 — Drain to 25%:** Primarily steam displacement. Heaters modulated.
**Step 6 — Aux Spray Test:** Brief spray confirms compressible steam (5-15 psi drop).
**Step 7 — Stabilization:** CVCS rebalanced, pressure stabilized.

### 4.2 Key Differences from Current Implementation

| Parameter | Current Code | Real Plant |
|-----------|-------------|------------|
| Pre-bubble charging | 75 gpm | 0 gpm |
| CCP start trigger | At drain start | Level < 80% |
| CCP flow rate | 75 gpm | 44 gpm |
| Letdown during drain | 120 gpm | 75 gpm (RHR crossconnect) |
| Drain driving force | CVCS imbalance | Steam displacement + CVCS trim |
| Heater control | Fixed 1.8 MW | Auto pressure-rate modulated |
| Drain target | 25% ✅ | 25% ✅ |

### 4.3 Charging Flow Deep Dive

Per NRC HRTD 4.1: CCP capacity = 44 gpm. Without RCPs: full 44 gpm to charging. With RCPs: 55 gpm charging + 32 gpm seal injection.

### 4.4 Letdown Path During Bubble Formation

Per NRC HRTD 19.0: Letdown via RHR crossconnect (HCV-128) at 75 gpm throughout Mode 5. Normal orifices available but negligible flow at startup pressures. Transition to normal orifices after RHR isolation at Mode 4→3 (Phase 3 scope).

---

## 5. Proposed Implementation

### 5.1 New Constants (PlantConstants.cs)

```
CCP_CAPACITY_GPM = 44f, CCP_WITH_SEALS_GPM = 87f, CCP_START_LEVEL = 80f
HEATER_STARTUP_MAX_PRESSURE_RATE = 100f, HEATER_STARTUP_MIN_POWER_FRACTION = 0.2f
P_PROP_HEATER_FULL_ON = 2225f, P_PROP_HEATER_ZERO = 2275f
P_BACKUP_HEATER_ON = 2200f, P_BACKUP_HEATER_OFF = 2225f
P_SPRAY_START = 2260f, P_SPRAY_FULL = 2310f
SPRAY_BYPASS_FLOW_GPM = 1.5f, AMBIENT_HEAT_LOSS_KW = 42.5f
AUX_SPRAY_TEST_DURATION_SEC = 45f, AUX_SPRAY_MIN_PRESSURE_DROP = 5f
```

### 5.2 Heater Control Modes

- **Mode 1: STARTUP_FULL_POWER** — 1800 kW, no feedback
- **Mode 2: BUBBLE_FORMATION_AUTO** — Pressure-rate feedback, continuously variable
- **Mode 3: PRESSURIZE_AUTO** — Same controller, target ≥320 psig
- **Mode 4: AUTOMATIC_PID** — Future scope, constants defined
- **Low-level interlock:** < 17% trips all heaters (preserved)

### 5.3 Drain Rework

**Mechanism 1 (primary):** Steam generation displaces water
**Mechanism 2 (secondary):** CVCS trim (75 gpm letdown, 0→44 gpm charging)
**CCP_START sub-phase:** Level < 80% triggers CCP

### 5.4 VCT: Corrected flows tracked. VCT rises during drain (correct).
### 5.5 Aux Spray Test: 45-sec spray, verify 5-15 psi drop, log result.

---

## 6. Files to Modify

| File | Changes |
|------|---------|
| PlantConstants.cs | Add CCP, heater, aux spray constants |
| CVCSController.cs | Multi-mode heater controller |
| HeatupSimEngine.cs | Thermodynamic drain, CCP logic, aux spray, correct CVCS flows |
| VCTPhysics.cs | None expected |
| SolidPlantPressure.cs | GOLD STANDARD — no changes |
| WaterProperties.cs | GOLD STANDARD — no changes |

---

## 7. Validation Criteria

- Timeline: Drain 30-60 min, CCP at <80%, drain to 25%
- CVCS: 75 gpm letdown, 0→44 gpm charging, net 75→31 gpm
- Pressure: 50-150 psi/hr during drain, ≥320 psig end state
- Mass conservation: < 10 gal error
- Aux spray: 5-15 psi drop, recovery in 2-3 min
- Letdown: RHR crossconnect only throughout Phase 2

---

## 8. Implementation Order

1. PlantConstants additions
2. Heater control rework
3. CVCS flow sequence
4. Aux spray verification
5. Thermodynamic drain
6. VCT conservation
7. Integration test

---

## 9. Resolved Design Decisions

| # | Question | Decision | Rationale |
|---|----------|----------|-----------|
| 1 | Drain target | **25%** | Westinghouse 4-loop per NRC HRTD 19.2.2 |
| 2 | Heater control | **Continuous variable, automatic** | Future release adds manual operator control |
| 3 | Aux spray | **Model actual test** | Confirms steam, validates physics |
| 4 | Letdown path | **RHR crossconnect throughout Phase 2** | Per NRC HRTD 19.0, Mode 5 with RHR in service |
| 5 | RCP start | **Phase 3 scope** | Ends at "RCP NPSH conditions met" |

---

## 10. Phase 3 Boundary Notes

Out of scope for v1.4.0.0:
1. RCP start sequence
2. Seal injection transition (44 gpm → 55+32 gpm)
3. Letdown path transition (RHR crossconnect → normal orifices)
4. RHR isolation
5. Normal letdown establishment
6. Full automatic pressure control at 2235 psig
7. Manual operator heater control for gameplay
