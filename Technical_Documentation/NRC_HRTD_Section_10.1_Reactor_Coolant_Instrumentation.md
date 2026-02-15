# NRC HRTD Section 10.1 — Reactor Coolant Instrumentation

**Source:** https://www.nrc.gov/docs/ML1122/ML11223A281.pdf  
**Retrieved:** 2026-02-15  
**Revision:** Rev 0505

---

## Overview

This document provides comprehensive technical details on Westinghouse PWR Reactor Coolant System process instrumentation including:

- Loop temperature instrumentation (narrow-range and wide-range RTDs)
- T_avg and ΔT signal generation and processing
- Pressurizer pressure, level, and temperature instrumentation
- RCS loop flow measurement
- Reactor Vessel Level Indicating System (RVLIS)
- Subcooling Margin Monitor (SMM)

---

## Learning Objectives

1. Describe how loop average temperature (T_avg) and temperature difference (ΔT) are derived from the coolant loop narrow-range RTD outputs, and how these signals are used.
2. List the functions of the following temperature monitors:
   - RCS wide-range temperature detectors
   - Pressurizer, pressurizer surge line, and pressurizer spray line detectors
   - Safety and relief valve discharge line detectors
   - Pressurizer relief tank (PRT) detector
   - Reactor vessel flange leak-off detector
3. Explain how the differential pressure (ΔP) cells at RCS piping elbows are used to measure RCS flows.

---

## 10.1.1 Temperature

### 10.1.1.1 Narrow-Range Temperature Detectors

Reactor coolant temperatures are measured by RTDs in the hot and cold legs of the RCS. The outputs of the narrow-range temperature instrumentation (Th and Tc) are further processed to provide:

- **T_avg** = (Th + Tc) / 2 — Average temperature for each loop
- **ΔT** = Th - Tc — Temperature difference (proportional to power when subcooled)

These processed signals are used for:
- Control room indication
- Inputs to various control systems
- Inputs to the reactor protection system (RPS) for protection-grade interlocks and reactor trip signals

**Two Narrow-Range Temperature Measurement Arrangements:**

#### Bypass Manifold Arrangement (Pre-1987)
- Each reactor coolant loop has two RTD manifolds in bypass piping
- One manifold for Th measurement, one for Tc measurement
- Each manifold includes one in-service RTD and an installed spare
- Lower flow velocities allow direct-immersion RTDs
- Three scoops penetrate the hot leg at 120° intervals for representative Th
- Tc measured downstream of RCP (pump mixing ensures representative sample)
- Combined flow returned via penetration in intermediate leg
- **Limitation:** Not accurate during natural circulation (depends on RCP differential pressure)

**Problems with Bypass Manifold Arrangement:**
1. Lack of reliability — 280 ft of piping, 8 manifolds, ~70 valves per 4-loop plant
2. High personnel radiation exposure — crud traps in low-flow areas
3. Estimated 1500 man-rem savings per unit by removal

#### Thermowell Arrangement (Post-1987)
- Bypass piping removed from most Westinghouse plants
- Fast-acting, narrow-range RTDs mounted in thermowells
- Hot leg: Three dual-element RTDs in flow scoops at 120° intervals
- Cold leg: One dual-element RTD downstream of RCP
- Second element of each RTD is calibrated spare

**RTD Calibration Ranges:**
| Location | Range |
|----------|-------|
| Narrow-range cold leg (Tc) | 510 - 630°F |
| Narrow-range hot leg (Th) | 530 - 650°F |
| Calculated T_avg | 530 - 650°F |
| Calculated ΔT | 0 - 150% |

**Signal Processing (Figure 10.1-2):**
- Three hot-leg RTD signals averaged to generate Th_ave for each loop
- Loop T_avg and ΔT calculated and provided to RPS channels
- OT ΔT and OP ΔT trip setpoints calculated from T_avg
- Loop ΔT compared to both OT ΔT and OP ΔT setpoints
- Protection-grade signals separated from control-grade via isolation amplifiers

**Auctioneered Signals to Control Systems:**
- Auctioneered high T_avg → Rod control system, Pressurizer level control, Steam dump control
- Auctioneered high T_avg and ΔT → Rod insertion limit calculators

**T_avg Significance:**
- Indicates margin to saturation
- Indicates heat capacity of reactor coolant
- Deviation from T_ref indicates imbalance between primary and secondary power
- Input to DNB margin determination

**ΔT Significance:**
- When subcooled, ΔT is directly proportional to reactor power
- Used in both control and protection systems as power measure

### 10.1.1.2 Wide-Range Temperature Detectors

- **Range:** 0 - 700°F
- **Location:** Thermowells in reactor coolant piping of each loop
- **Functions:**
  - Indication during heatups and cooldowns
  - Indication during natural circulation operation
  - Inputs to Subcooling Margin Monitor (SMM)
  - Hot-leg wide-range RTDs provide inputs to RVLIS

### 10.1.1.3 Pressurizer, Surge Line, and Spray Line Temperature Detectors

**Pressurizer Temperature Detectors (2):**
- One measures steam temperature
- One measures water temperature
- Under normal conditions (two-phase equilibrium), both are equal
- Unequal temperatures indicate abnormal condition

**Surge Line Temperature Detector:**
- Provides indication and low temperature alarm (< 517°F)
- Low alarm indicates large insurge of cold water or excessive ambient heat losses
- Temperature should remain high due to constant outflow balancing spray bypass flow

**Spray Line Temperature Detectors (one per line):**
- Provides indication and low temperature alarm (< 450°F)
- Normal readings indicate spray bypass flows maintaining temperature
- Low alarm could indicate loss of spray bypass flow or incorrect bypass throttle valve position
- Low temperatures risk thermal shock to spray nozzle when spray demanded

### 10.1.1.4 Safety and Relief Valve Discharge Temperature Detectors

**Configuration (Figure 10.1-3):**
- One detector on discharge line from each pressurizer safety valve
- One detector on common discharge line from both PORVs

**Function:**
- Provides control room indication of valve opening or leakage
- High temperature alarm at > 160°F
- Since detectors are close together, any single valve opening causes temperature increase at all detectors

### 10.1.1.5 Pressurizer Relief Tank Temperature Detector

- High temperature alarm at > 112.5°F
- Alternate method of alerting operator to relief valve opening or leakage

### 10.1.1.6 Reactor Vessel Flange Leakoff Temperature Detector

- Located between leakoff line isolation valve and reactor coolant drain tank
- Alarm setpoint: 20°F above containment ambient temperature
- Alerts operator to leak from reactor vessel flange O-ring seal
- Isolation valve CV-8032 (air-operated, fails closed on loss of instrument air)
- If inner O-ring leaks, outer O-ring can be placed in service via manual valve realignment

---

## 10.1.2 Pressure

### 10.1.2.1 Pressurizer Pressure Detectors

- **Quantity:** 4 transmitters
- **Range:** 1700 - 2500 psig (narrow range)
- **Functions:**
  - Control room indication
  - Inputs to Pressurizer Pressure Control System (Chapter 10.2)
  - Inputs to Reactor Protection System (Chapter 12.2)
- Pressure maintained by heaters (water volume) and spray valves (steam volume)

### 10.1.2.2 Reactor Coolant Loop and PRT Pressure Detectors

**Wide-Range RCS Pressure (PT-403 and PT-405):**
- **Location:** RHR system suction line near hot leg (Loop 4) penetration
- **Range:** 0 - 3000 psig
- **Functions:**
  - Indication during startups and shutdowns
  - Interlock to permit manual opening of RHR suction valves (< 425 psig)
  - Automatic closure of RHR suction valves (> 585 psig)
  - Protects RHR piping from overpressurization

**PRT Pressure Transmitter:**
- Control room indication
- High pressure alarm at 8 psig
- Indicates pressurizer relief valve discharge into PRT

---

## 10.1.3 Pressurizer Level

- **Quantity:** 3 level transmitters (hot-calibrated) + 1 cold-calibrated
- **Hot-calibrated range:** Calibrated for 650°F normal operating temperature
- **Cold-calibrated range:** Calibrated for 80°F (used during heatup, refueling, cold shutdown)
- **Functions:**
  - Control room indication
  - Inputs to Pressurizer Level Control System (Section 10.3)
  - Inputs to Reactor Protection System (Section 12.2)
- Level is direct measure of reactor coolant inventory

---

## 10.1.4 Reactor Coolant Flow

**Measurement Method:** Elbow tap differential pressure (Figures 10.1-1a and 10.1-1b)

**Configuration per Loop:**
- 3 differential pressure (d/P) transmitters
- Located at first bend (elbow) in intermediate leg
- 1 common high pressure (HP) tap at outside radius
- 3 separate low pressure (LP) taps at inside radius

**Operating Principle:**
- HP tap: Static RCS pressure + centrifugal force pressure (proportional to flow²)
- LP tap: Static RCS pressure only
- Flow proportional to √(ΔP between outer and inner radius)

**Failure Modes:**
- HP tap failure: All 3 transmitters fail LOW → reactor trip (conservative)
- Single LP tap failure: Only 1 transmitter fails HIGH → 2 remaining provide proper indication
- Three LP taps provide redundancy and conservative response

---

## 10.1.5 Reactor Vessel Level Indicating System (RVLIS)

### Purpose
Provides reliable water level indication within the reactor vessel under normal and accident conditions per NUREG-0737 requirements.

### Functions
1. Indicates formation of voids in RCS during forced circulation
2. Detects approach to inadequate core cooling
3. Provides information for selecting emergency operating procedures
4. Detects voiding in reactor vessel head
5. Provides accurate measurement during natural circulation
6. Provides information during reactor vessel head vent operation

### System Configuration (Figure 10.1-4)

**Two redundant trains, each with 3 d/P transmitters:**

| Transmitter | Designation | Range | RCPs Running | RCPs Stopped |
|-------------|-------------|-------|--------------|--------------|
| Dynamic Range | ΔPc | 0-120% | Valid (100-110% normal) | Shows ~40% |
| Full Range | ΔPb | 0-120% | Off-scale HIGH (invalid) | Valid (100% = full) |
| Upper Range | ΔPa | 60-120% | Off-scale LOW (invalid) | Valid (100% = full) |

### Sensing Line Penetrations
- **Low pressure tap:** Spare CRDM penetration in vessel head (near center)
- **High pressure tap:** Incore instrument conduit at seal table
- **Additional taps:** Th RTD bypass manifold lines of Loops 3 and 4

### Hydraulic Isolation System
- Sensor bellows units inside containment
- Hydraulic isolators outside containment (act as containment isolation valves)
- Two opposing liquid-filled bellows linked by connecting rod

### Signal Processing (Figure 10.1-5)
Each microprocessor receives:
- 3 differential pressures (ΔPa, ΔPb, ΔPc)
- Wide-range Th (2 inputs)
- Wide-range loop pressure (1 input)
- Capillary tube RTD temperatures (7 inputs) for density compensation

**Density Compensation Areas:**
- Reactor vessel cavity
- Incore thimble tunnel
- Hot-leg penetration area
- Rise above seal table

### Operating Characteristics

**Dynamic Range (ΔPc) — For Forced Circulation:**
- Calibrated for 100%+ indication with RCPs running
- Shows ~40% with RCPs stopped (backup level indication)
- During forced circulation with voids: comparison of measured ΔP to normal single-phase ΔP indicates relative void fraction
- Not a true "level" indication during forced circulation — indicates void content

**Full Range (ΔPb) — For Natural Circulation:**
- Spans total vessel height (~40 ft)
- 100% = vessel completely filled with subcooled coolant
- When RCPs stopped with voids: liquid collapses, steam rises → indicates collapsed water level
- Actual level may be slightly higher due to froth
- Conservative indication of coolant level

**Upper Range (ΔPa) — Vessel Head Monitoring:**
- Spans hot leg to top of vessel head (~15 ft)
- Indication range: 60-120%
- 100% = vessel completely filled
- Provides accurate indication of head voiding
- Useful during reactor vessel head venting
- Confirms level above hot-leg nozzles

### Control Room Display
- Remote display panels show RVLIS outputs
- Displays include RCP status and expected levels for each range
- Outputs sent to plant computer
- No annunciation provided by RVLIS

### Overtravel Alarms
- Hydraulic isolator limit switches detect abnormal bellows deflection
- Alerts operators to potential isolator problems
- Does not necessarily mean RVLIS data is invalid

---

## 10.1.6 Subcooling Margin Monitoring (SMM)

### Purpose
Continuously displays margin to saturation of reactor coolant. Serves as post-accident monitoring instrument in conjunction with RVLIS and CETs per NUREG-0737.

### Inputs to Each SMM Train (Figure 10.1-6)

| Input | Train A | Train B |
|-------|---------|---------|
| RCS wide-range pressure | PT-403 | PT-405 |
| Wide-range Th | Loops 2 and 4 | Loops 1 and 3 |
| Wide-range Tc | Loop 1 | Loop 3 |
| Core-exit thermocouples | 8 (2 per quadrant) | 8 (2 per quadrant) |
| Cold junction RTD | 1 | 1 |

### Outputs
- Remote temperature display panel (°F subcooled or superheated)
- Plant computer output
- Remote shutdown station output
- **Low margin alarm:** 15°F
- **No margin alarm:** 0°F (saturation)

### Display Modes (Operator Selectable)

**Temperature Mode (TEMP):**
- Displays margin in °F with 0.1°F resolution
- Margin = Saturation temperature - Measured temperature
- Uses HIGH-SELECTED temperature from all active inputs
- Compares to saturation temperature calculated from wide-range pressure
- Negative value indicates superheated conditions

**Pressure Mode (PRESS):**
- Displays margin in psi with 1 psi resolution
- Margin = Measured pressure - Saturation pressure
- Saturation pressure calculated from high-selected temperature

### Operability Requirements

**Wide-Range RTDs:**
- Included in original design but NOT required for operability
- If RTD fails, input can be disabled and SMM train remains operable

**Pressure Input:**
- Only one pressure input per train
- Wide-range pressure detector failure makes SMM train INOPERABLE
- No immediate recovery except repair

**Core-Exit Thermocouples:**
- 8 operable CETs required per train (2 per core quadrant)
- If CET fails, alternate CET from same quadrant can be substituted
- 65 total CETs available (32 to one panel, 33 to other)

---

## 10.1.7 Summary

### Temperature Instrumentation
- **Narrow-range RTDs:** 510-650°F, provide T_avg and ΔT for control and protection
- **Wide-range RTDs:** 0-700°F, provide indication during heatup/cooldown and natural circulation
- **Specialized detectors:** Pressurizer, surge line, spray line, safety/relief valve discharge, PRT, vessel flange leakoff

### Pressure Instrumentation
- **Wide-range RCS:** 0-3000 psig at RHR suction (PT-403, PT-405)
- **Pressurizer narrow-range:** 1700-2500 psig (4 transmitters)
- **PRT pressure:** High alarm at 8 psig

### Flow Instrumentation
- **Elbow tap ΔP:** 3 transmitters per loop at intermediate leg elbow
- Flow proportional to √ΔP between outer and inner radius

### Post-Accident Monitoring (NUREG-0737)
- **RVLIS:** Reactor vessel level indication with density compensation
- **SMM:** Subcooling margin monitoring with automatic alarms

---

## Critical Data for Simulator Development

### RTD Ranges and Calibration
| Parameter | Range | Notes |
|-----------|-------|-------|
| Narrow-range Tc | 510-630°F | Cold leg |
| Narrow-range Th | 530-650°F | Hot leg |
| Wide-range T | 0-700°F | All loops |
| Calculated T_avg | 530-650°F | From narrow-range |
| Calculated ΔT | 0-150% | Proportional to power |

### Alarm Setpoints
| Parameter | Setpoint | Function |
|-----------|----------|----------|
| Surge line low temp | < 517°F | Cold insurge or heat loss |
| Spray line low temp | < 450°F | Loss of bypass flow |
| Relief valve discharge high temp | > 160°F | Valve opening/leakage |
| PRT high temp | > 112.5°F | Relief valve discharge |
| PRT high pressure | > 8 psig | Relief valve discharge |
| SMM low margin | 15°F | Approaching saturation |
| SMM no margin | 0°F | At saturation |

### RVLIS Expected Indications
| Condition | Dynamic (ΔPc) | Full (ΔPb) | Upper (ΔPa) |
|-----------|---------------|------------|-------------|
| RCPs running, no voids | 100-110% | Off-scale HIGH | Off-scale LOW |
| RCPs stopped, vessel full | ~40% | 100% | 100% |
| RCPs stopped, vessel with voids | < 40% | < 100% | < 100% |

### Control System Inputs
| Signal | Destination |
|--------|-------------|
| Auctioneered high T_avg | Rod control, PZR level control, Steam dump |
| Auctioneered high ΔT | Rod insertion limit |
| Loop T_avg | OT ΔT / OP ΔT calculation |
| Loop ΔT | Comparison to OT ΔT / OP ΔT setpoints |

---

## References

- NRC HRTD Section 10.2 — Pressurizer Pressure Control System
- NRC HRTD Section 10.3 — Pressurizer Level Control System
- NRC HRTD Section 12.2 — Reactor Protection System
- NRC HRTD Section 3.2 — Reactor Coolant System
- NRC HRTD Section 8.1 — Rod Control System
- NRC HRTD Section 11.2 — Steam Dump Control System
- NUREG-0737 — Post-TMI Requirements

---
