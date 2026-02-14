# NRC HRTD Section 9.1 — Excore Nuclear Instrumentation

**Source:** https://www.nrc.gov/docs/ML1122/ML11223A263.pdf  
**Retrieved:** 2026-02-14  
**Revision:** Rev 0403

---

## Overview

This document provides comprehensive technical details on the Westinghouse PWR Excore Nuclear Instrumentation System including:

- Source range, intermediate range, and power range detector design and operation
- Neutron flux monitoring from shutdown to 200% power
- Reactor protection system inputs and trip functions
- Interlocks and permissives (P-6, P-10)
- Axial and radial power distribution monitoring
- Audio count rate and startup rate indication
- Detector calibration procedures

---

## Key Technical Data for Simulator Development

### Excore Nuclear Instrumentation System Overview

**Purpose:**
1. Provide indication of reactor power from shutdown to full power
2. Provide inputs to the reactor protection system during startup and power operation
3. Provide reactor power information to the automatic rod control system
4. Provide axial and radial power distribution information during power operations

**Range Coverage:**
- Three overlapping ranges monitor neutron flux from a few cps up to ~10^15 neutrons/cm²/sec (200% power)
- 12 decades total coverage across all ranges

**Channel Configuration:**
- Source Range: 2 independent channels (N-31, N-32)
- Intermediate Range: 2 independent channels (N-35, N-36)
- Power Range: 4 independent channels (N-41 through N-44)

---

## Source Range Instrumentation

**Detector Type:** BF₃ (Boron Trifluoride) proportional counter

**Location:** 180° apart, outside bottom half of core (near primary/secondary neutron sources)

**Indication Range:** 10⁰ to 10⁶ counts per second (6 decades)

**Startup Rate (SUR) Range:** -0.5 to +5.0 decades per minute (DPM)

**Trip Setpoint:** 10⁵ cps (high neutron flux)

**Key Features:**
- Gamma discrimination via pulse amplitude discrimination
- Audio count rate channel (audible "beeper") for startup monitoring
- High flux at shutdown alarm for reactivity monitoring

**Interlocks:**
- **P-6 Permissive (10⁻¹⁰ amps IR):** When satisfied, operator can manually block SR trip
- When blocked, high voltage is removed from SR detectors (protects from damage at higher flux levels)
- Trip automatically reinstates when both IR channels drop below P-6 setpoint
- Above P-10, SR high voltage cannot be restored (detector protection)

---

## Intermediate Range Instrumentation

**Detector Type:** Compensated ionization chamber (gamma-compensated)

**Location:** 180° apart, at core midplane elevation (same instrument wells as SR detectors)

**Indication Range:** 10⁻¹¹ to 10⁻³ amperes (8 decades)

**Startup Rate (SUR) Range:** -0.5 to +5.0 DPM

**Trip Setpoint:** Current equivalent to 25% power

**Rod Stop Setpoint (C-1):** Current equivalent to 20% power

**P-6 Permissive Setpoint:** 10⁻¹⁰ amps

**Gamma Compensation:**
- Two chambers in one detector housing
- Inner chamber: B-10 coated (neutron + gamma sensitive)
- Outer chamber: Uncoated (gamma only)
- Electrical subtraction: i_total = (i_n + i_γ) - i_γ = i_n
- Operated slightly undercompensated for conservative response

**Interlocks:**
- **P-10 Permissive (10% power range):** When satisfied, operator can block IR trip and C-1 rod stop
- Trip automatically reinstates when 3/4 power range channels drop below P-10

---

## Power Range Instrumentation

**Detector Type:** Uncompensated ionization chamber (dual section)

**Location:** 90° apart (4 quadrants), upper and lower detectors per location

**Configuration:**
- 4 channels, each with upper (A) and lower (B) detector
- Total 8 detectors (4 upper, 4 lower)
- Each detector has 10-foot neutron-sensitive length

**Indication Range:** 0 to 120% of full power (linear)

**No Gamma Compensation Needed:** At power range levels, neutron flux dominates gamma flux

**Trip Setpoints:**
| Function | Setpoint | Coincidence | Blockable |
|----------|----------|-------------|-----------|
| High Flux - Low | 25% | 2/4 | Yes (above P-10) |
| High Flux - High | 109% | 2/4 | No |
| Positive Rate | +5% in 2 sec | 2/4 | No (seals in) |
| Negative Rate | -5% in 2 sec | 2/4 | No (seals in) |

**Rod Stop Setpoint (C-2):** 103% power (1/4 coincidence)

**Permissives Generated:**
| Permissive | Setpoint | Coincidence | Function |
|------------|----------|-------------|----------|
| P-8 | 39% | 2/4 | Enables single-loop low flow trip |
| P-9 | 50% | 2/4 | Enables turbine trip reactor trip (some plants) |
| P-10 | 10% | 2/4 | Nuclear at-power permissive |

**Axial Flux Difference (AFD):**
- Calculated from (upper detector - lower detector) / sum
- Displayed on ΔI meters (±30% scale)
- Used for OTΔT and OPΔT trip setpoint corrections

**Quadrant Power Tilt Ratio (QPTR):**
- Monitored via detector current comparators
- Technical specification limit: 1.02 ratio
- Alarm on deviation >2% between channels

---

## Detector Operation Physics

### BF₃ Proportional Counter (Source Range)
**Nuclear Reaction:**
```
n + ¹⁰B → ⁷Li + α + energy
```

**Operating Principle:**
- Incident neutron causes (n,α) reaction in BF₃ gas
- Charged particles (Li, α) produce ionization
- Applied voltage causes gas amplification (proportional region)
- Large pulse per neutron event
- Gamma pulses are lower amplitude → discrimination possible

### Compensated Ion Chamber (Intermediate Range)
**Operating Principle:**
- Boron-lined inner chamber: neutron + gamma response
- Uncoated outer chamber: gamma only response
- Electrical subtraction cancels gamma contribution
- Net current proportional to neutron flux only

### Uncompensated Ion Chamber (Power Range)
**Operating Principle:**
- Single boron-lined cylindrical chamber
- Neutron flux >> gamma flux at power levels
- Gamma contribution proportional to power (acceptable)
- Linear current output proportional to neutron flux

---

## Audio Count Rate System

**Purpose:** Provide audible indication of neutron flux changes during shutdown and startup

**Input:** Either source range channel (N-31 or N-32), selectable

**Audio Multiplier:** Divides count rate by 10, 100, 1000, or 10,000 for discernible tone rate

**Output:** Speakers in control room and containment

**Function:**
- Alerts personnel to reactivity changes
- Monitors approach to criticality
- Used for inverse count rate ratio (1/M) plots

---

## Startup Rate Calculation

**Definition:** Rate of change of neutron population in decades per minute (DPM)

**Formula:**
```
SUR = dN/dt × (1/N) × (1/ln10) = (1/T) × (1/2.303)
```
Where T = reactor period in minutes

**Indication Range:** -0.5 to +5.0 DPM

**Displayed For:**
- Both source range channels
- Both intermediate range channels
- Selectable meter on control board
- Individual meters for each channel

**Use During Startup:**
- Positive SUR indicates approach to criticality
- SUR = 0 at criticality (steady state)
- Controlled withdrawal targets SUR ~0.5 DPM typical

---

## Calibration

### Power Range Calibration
- Calibrated to indicate percent rated thermal power
- Based on secondary heat balance (calorimetric)
- Performed at stable power conditions
- Gain adjustment on each channel drawer

### Incore-Excore Calibration
- Excore detectors calibrated to match incore flux mapping data
- Corrects for:
  - Neutron leakage geometry effects
  - Axial flux distribution changes
  - Radial flux tilt effects
- Ensures F(ΔI) penalties in OTΔT/OPΔT are accurate

---

## System Interrelationships

### Inputs to Reactor Protection System
| Signal | Purpose |
|--------|---------|
| SR High Flux (10⁵ cps) | Startup excursion protection |
| IR High Flux (25%) | Startup excursion protection |
| PR High Flux Low (25%) | Startup excursion protection |
| PR High Flux High (109%) | Overpower protection |
| Positive Rate (+5%/2s) | Rod ejection protection |
| Negative Rate (-5%/2s) | Dropped rod protection |

### Inputs to Rod Control System
| Signal | Purpose |
|--------|---------|
| Auctioneered High Nuclear Power | Power mismatch circuit (auto rod control) |
| Individual PR outputs | Control-grade rod stops (C-2) |

### Inputs to Steam Dump Control
| Signal | Purpose |
|--------|---------|
| Auctioneered High Nuclear Power | Loss-of-load controller (power mismatch) |

---

## Critical Notes for Simulator

1. **Approach to Criticality:**
   - Monitor source range count rate and SUR
   - Plot 1/M (inverse multiplication) vs rod position
   - Critical when 1/M extrapolates to zero
   - SR trip blocks at P-6 (IR on scale)

2. **Power Ascension:**
   - IR trip blocks at P-10 (10% on power range)
   - PR low setpoint trip blocks at P-10
   - SR detectors de-energized above P-6 (protects from damage)

3. **Power Range Operation:**
   - 109% high flux trip always active
   - Rate trips always active (cannot be blocked)
   - C-2 rod stop at 103% prevents approach to trip

4. **Detector Positioning:**
   - Instrument wells are movable for maintenance
   - Failure to return to correct position causes erroneous indication
   - Position verification required after maintenance

5. **Test Signals:**
   - All test signals are ADDITIVE to detector signals
   - Channel cannot indicate less than actual flux during test
   - Maintains conservative protection during testing

---

## Implementation Priority for Simulator

**Phase 1 (Approach to Criticality):**
- Source range count rate indication (0-10⁶ cps, log scale)
- Source range SUR (-0.5 to +5 DPM)
- Audio count rate (optional visual/audio feedback)
- P-6 permissive logic
- SR high flux trip and alarm

**Phase 2 (Low Power to HZP):**
- Intermediate range indication (10⁻¹¹ to 10⁻³ A, log scale)
- Intermediate range SUR
- IR high flux trip (25%)
- C-1 rod stop (20%)
- P-10 permissive logic

**Phase 3 (Power Operations):**
- Power range indication (0-120%, linear)
- Upper/lower detector outputs
- Axial flux difference indication
- PR high flux trips (25%, 109%)
- Rate trips (+5%/-5% in 2 sec)
- C-2 rod stop (103%)
- P-8, P-9 permissive logic
- Quadrant power tilt monitoring

---

## References

This document should be referenced for:
- Nuclear instrumentation design and range overlap
- Detector physics and gamma compensation
- Trip setpoints and coincidence logic
- Permissive interlock functions
- Calibration procedures
- Approach to criticality monitoring

---
