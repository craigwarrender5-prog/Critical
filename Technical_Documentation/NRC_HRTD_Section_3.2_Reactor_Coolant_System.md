# NRC HRTD Section 3.2 — Reactor Coolant System

**Source:** https://www.nrc.gov/docs/ML1122/ML11223A213.pdf  
**Retrieved:** 2026-02-14  
**Revision:** Rev 1203

---

## Overview

This document provides comprehensive technical details on the Westinghouse PWR Reactor Coolant System (RCS) including:

- RCS design parameters and component descriptions
- Pressurizer design, operation, and control  
- Steam generator construction and thermal-hydraulic performance
- Reactor coolant pump design including seal assembly, flywheel, and motor
- System instrumentation (temperature, pressure, flow, level)
- Heatup and cooldown pressure-temperature limits
- Natural circulation capabilities
- Leakage detection systems

---

## Key Technical Data for Simulator Development

### RCS Design Parameters

**Piping:**
- Hot leg ID: 29.0", wall thickness: 2.84"
- Cold leg ID: 27.5", wall thickness: 2.69"  
- RCP suction ID: 31.0", wall thickness: 2.99"
- Design pressure: 2,485 psig
- Design temperature: 650°F

**Pressurizer:**
- Total volume: 1,800 ft³
- Full power water volume: 1,080 ft³ (60%)
- Full power steam volume: 720 ft³ (40%)
- Shell ID: 84.0"
- Heater capacity: 1,794 kW (78 heaters total)
  - Proportional heaters: 18 @ 414 kW
  - Backup heaters: 60 @ 1,380 kW
- Surge line ID: 14.0", wall thickness: 1.40"

**Pressurizer Relief and Safety:**
- Code safety valves: 3 @ 2,485 psig setpoint
- Capacity per safety: 420,000 lb/hr
- PORVs: 2 @ 210,000 lb/hr capacity each
- Accumulation: 3%, Blowdown: 5%

**Steam Generators (Model 51):**
- Number: 4
- Height: 67.75 ft
- Shell OD: 175.75"
- U-tubes: 3,388 tubes
- Tube OD: 0.875", wall thickness: 0.050"
- Design pressure primary: 2,485 psig
- Design pressure secondary: 1,185 psig
- Full load steam flow: 3.77×10⁶ lb/hr per SG
- Full load steam pressure: 895 psig
- Full load steam temp: 533.3°F
- Moisture carryover: <0.25% by weight

**Reactor Coolant Pumps:**
- Number: 4
- Flow per pump: 88,500 gpm
- Design pressure: 2,485 psig
- Speed: 1,200 rpm
- Discharge head: 277 ft
- Motor: 6,000 HP @ 12,500 Vac
- Power (cold RCS): 5,997 kW (~6 MW)
- Power (hot RCS): 4,540 kW (~4.5 MW)
- Flywheel weight: 13,200 lb

---

## RCP Seal Injection System

**Normal Seal Injection Flow:**
- Total injection per RCP: 8 gpm
- Downward through thermal barrier HX to RCS: 5 gpm
- Upward through radial bearing and seals: 3 gpm
- Seal #1 (film-riding): controlled leakage design, 2,200 psi ΔP
- Seal #2 (backup): rubbing face, full RCS pressure capability
- Seal #3 (leakage diversion): rubbing face, low pressure
- Minimum RCS pressure for pump operation: 400 psig (275 psid across seal #1)
- Seal #1 bypass valve opens when P_RCS < 1,500 psig

**Critical for Heatup Simulation:**
- RCPs must not start until RCS pressure ≥ 400 psig
- Seal injection from CVCS provides bearing cooling
- RCP heat input during heatup: ~6 MW per pump (cold water)
- Coast-down time extended 22-30 seconds by flywheel

---

## Pressurizer Operating Characteristics

**Heaters:**
- Can raise pressurizer temperature at ~55°F/hr
- Submerged in lower portion of vessel
- Hermetically sealed terminals retain full pressure if sheath fails

**Spray:**
- Two spray lines from cold legs (redundant)
- Max spray flow: 840 gpm (both valves)
- Design prevents PORV lift on 10% step load decrease
- Bypass flow: 1 gpm continuous (chemistry control + thermal stress prevention)
- Spray driving force: hot leg to cold leg ΔP + cold leg velocity head (scoop design)
- Auxiliary spray connection for cooldown with RCPs off

**Surge Line:**
- Sized to limit ΔP during maximum insurge
- Thermal sleeve at pressurizer end for thermal stress
- Temperature sensor with low-temp alarm

---

## Steam Generator Thermal-Hydraulic Performance

**Heat Transfer:**
Q̇ = U × A × ΔT

Where:
- Q̇ = heat transfer rate (determined by plant load)
- U = heat transfer coefficient (material constant)
- A = heat transfer area (constant if tubes remain covered)
- ΔT ≈ T_avg (RCS) - T_sat (secondary)

**Steam Pressure vs Load:**
- Steam pressure DECREASES with increasing load (~150 psi from zero to full load)
- To increase Q̇, must increase ΔT
- ΔT increase achieved partly by raising T_avg (rod withdrawal)
- Additional ΔT from reducing T_sat → steam pressure decreases

**Natural Circulation:**
- Recirculation ratio: 3:1 to 5:1 at full power
- Downcomer flow = feedwater + recirculation from moisture separators
- Steam-water mixture rises through tube bundle
- Primary separators (swirl vanes) remove bulk moisture
- Secondary separators (chevrons) remove fine mist
- Steam quality: 99.75% minimum (0.25% moisture max)

**Level Characteristics:**
- Mass in SG is HIGHER at no-load than at full power
- Steam bubbles increase volume at higher power → level rises
- Shrink: steam flow decrease → pressure rise → bubble collapse → level drops
- Swell: steam flow increase → pressure drop → bubble formation → level rises

**Blowdown:**
- Continuous operation during plant ops
- Removes water from just above tubesheet
- Monitored for radiation (primary-to-secondary leak detection)

---

## Instrumentation

**Temperature:**
- Wide-range RTDs: 0-700°F (indication only)
- Narrow-range RTDs: hot leg 530-650°F, cold leg 510-630°F
- Narrow-range used for reactor control and protection
- Hot leg: 3 taps 120° apart for representative mixing
- Cold leg: single tap downstream of RCP (pump provides mixing)

**Flow:**
- Elbow taps on intermediate leg (no pressure drop)
- ω/ω₀ = √(ΔP/ΔP₀)
- Accuracy: ±10% absolute, ±1% repeatability
- Low-flow reactor trip uses this signal

**Pressurizer Pressure:**
- Redundant detectors in steam space
- Used for indication, control, and protection

**Pressurizer Level:**
- Differential pressure detectors (reference leg vs actual)
- Redundant channels for indication, control, protection
- Separate channel calibrated for cold shutdown conditions

---

## P-T Limits (Heatup and Cooldown)

**Physical Basis:**
- Prevent brittle fracture of reactor vessel
- Material NDT temperature determined by Charpy V-notch test (30 ft-lb min)
- Radiation exposure increases transition temperature over plant life
- Pressure stress = tensile on vessel wall
- Heatup stress = compressive on inner wall → reduces total stress
- Cooldown stress = tensile on inner wall → ADDS to pressure stress

**Limit Curves:**
- Maximum allowable pressure vs temperature for various rates
- Permissible operation: below and right of curves
- Criticality limit on heatup curve prevents low-temp critical operation
- Typical max rates: 100°F/hr heatup, 100°F/hr cooldown

**Design Cycles (200 each):**
- Heatup/cooldown at <100°F/hr
- Loss of load without trip: 80
- Reactor trips from 100% power: 400
- Leak test at >2,335 psig: 50
- Hydrostatic test >3,107 psig: 5

---

## Natural Circulation

**Requirements:**
1. Heat source (decay heat from shutdown reactor)
2. Heat sink (steam generators with maintained level and steam removal)
3. Elevation difference between heat sink and source

**Design:**
- SG centerline ~35 ft above core centerline
- Thermal driving head established by density difference
- Flow sufficient for decay heat removal ONLY (not power operation)
- Steam removal via PORVs or atmospheric relief valves
- AFW maintains SG level

---

## Leakage Detection

**Methods:**
1. Containment air particulate/gaseous monitors (most sensitive)
2. Makeup rate to pressurizer (increased makeup = leak)
3. Head-to-vessel closure joint leak-off temperature
4. Containment pressure, temperature, humidity
5. Containment sump level
6. Visual and ultrasonic inspection
7. Primary-to-secondary: air ejector and SG blowdown monitors

---

## Critical Notes for Simulator

1. **RCP Heat:** Each RCP adds ~6 MW to RCS when running in cold water
2. **Seal Pressure Limit:** Cannot run RCPs below 400 psig RCS pressure
3. **Pressurizer Function:** Accommodates density changes via steam bubble compression/expansion
4. **SG Thermal Lag:** Massive secondary inventory creates large thermal inertia
5. **P-T Limits:** Must be enforced during heatup/cooldown simulations
6. **Natural Circulation:** Only works with adequate SG level and steam removal path

---

## References

This document should be referenced for:
- Detailed component design parameters
- Operating characteristics and limits
- Instrumentation design and setpoints
- System interrelationships
- Material construction requirements
- PRA insights on critical components