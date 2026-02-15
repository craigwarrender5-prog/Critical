# NRC HRTD Section 12.3 — Engineered Safety Features Actuation Signals

**Source:** https://www.nrc.gov/docs/ML1122/ML11223A310.pdf  
**Retrieved:** 2026-02-15  
**Revision:** Rev 0706

---

## Overview

This document provides comprehensive technical details on the Westinghouse PWR Engineered Safety Features Actuation System (ESFAS) including:

- Safety injection actuation signals and functions
- Containment spray actuation
- Containment isolation (Phase A and Phase B)
- Steam line isolation
- Feedwater isolation
- Auxiliary feedwater actuation
- SI actuation reset logic

---

## Learning Objectives

1. List the engineered safety features (ESF) actuation signals and the accident(s) or conditions which will initiate each one
2. List the systems or components that are actuated or realigned by each ESF actuation signal
3. Describe the effects of resetting a safety injection actuation signal and how the reset signal is removed

---

## 12.3.1 Introduction

### ESF Actuation Purpose
- Actuate or realign safety-related systems, equipment, and components
- Isolate nonsafety-related systems
- Respond to accidents requiring protective action

### ESF Functions
1. Safety injection actuation
2. Containment spray actuation
3. Containment isolation
4. Steam line isolation
5. Feedwater isolation
6. Auxiliary feedwater actuation

### Actuation Logic
- Multiple analog signals compared to bistable setpoints
- When sufficient signals exceed setpoints in appropriate combination → RPS generates protective actions
- Master relays energize slave relays (up to 4 per master relay)
- Slave relays actuate ESF equipment through valve positioners and motor controllers

### Independence and Redundancy
- Protection train "A" controls train "A" ESF equipment
- Protection train "B" controls train "B" ESF equipment
- Single operable train sufficient to mitigate accident consequences
- Either train may operate unassigned components

---

## 12.3.2 Safety Injection Actuation

### Purpose
- Limit consequences of Condition III (infrequent faults) and Condition IV (limiting faults) events
- Reduce potential for significant radioactive release

### SI Actuation Functions
1. Shut down reactor if still operating
2. Maintain reactor in shutdown condition
3. Provide sufficient core cooling to limit cladding/fuel damage
4. Ensure containment integrity
5. Place support systems in post-accident alignments

### 12.3.2.1 Safety Injection Actuation Signals

#### Low Pressurizer Pressure

| Parameter | Value |
|-----------|-------|
| Setpoint | 1807 psig |
| Coincidence | 2/3 |
| Block permissive (P-11) | < 1915 psig (2/3) |
| Purpose | LOCA response, steam line break |

- Responds to pressure drop from inventory loss during LOCA
- Steam line break can also trigger (coolant contraction)
- Manually blocked during normal cooldown/depressurization

#### High Containment Pressure

| Parameter | Value |
|-----------|-------|
| Setpoint | 3.5 psig |
| Coincidence | 2/3 |
| Blockable | No |
| Purpose | High energy line break inside containment |

- Backup signal for any high energy line break (primary or secondary)
- Initiates SI if break large enough to increase containment pressure but not trigger other signals

#### High Steam Line Flow + Low Steam Pressure OR Low-Low T_avg

| Parameter | Value |
|-----------|-------|
| High steam flow setpoint | Varies with turbine impulse pressure |
| Low steam pressure | 600 psig (rate sensitive) |
| Low-low T_avg | 553°F |
| Steam flow coincidence | 1/2 flows on 2/4 steam lines |
| Pressure/temp coincidence | 2/4 steam lines or 2/4 RCS loops |
| Block permissive (P-12) | T_avg < 553°F |
| Purpose | Steam line break downstream of MSIVs |

- Indicates steam line break common to all SGs (downstream of MSIVs and check valves)
- Also initiates steam line isolation
- Can be manually blocked when P-12 permits (allows controlled cooldown)

#### High Steam Line Differential Pressure

| Parameter | Value |
|-----------|-------|
| Setpoint | 100 psi ΔP |
| Coincidence | Any steam line 100 psi lower than at least 2 of remaining 3 |
| Blockable | No |
| Purpose | Steam line break upstream of MSIVs |

- Indicates steam line break upstream of MSIVs and check valves
- Affected steam line pressure drops when check valve seats

#### Manual

| Parameter | Value |
|-----------|-------|
| Coincidence | 1/2 switches on main control board |
| Blockable | No |
| Purpose | Operator backup to automatic actuation |

### 12.3.2.2 Safety Injection Functions

When SI actuation occurs:

1. **Reactor Trip** — Shuts down reactor if not already tripped

2. **ECCS Actuation:**
   - Charging pump discharge realigned from normal CVCS to high-head cold-leg injection
   - Charging pump suctions realigned from VCT to RWST
   - Starts centrifugal charging pumps, SI pumps, RHR pumps
   - RHR and SI systems inject borated water from RWST to RCS cold legs

3. **Containment Isolation Phase A** — Isolates most containment penetrations

4. **Auxiliary Feedwater Actuation** — Provides safety-grade water source to SGs

5. **Main Feedwater Isolation** — Isolates MFW system

6. **Emergency Diesel Generator Startup:**
   - Started by any SI actuation regardless of offsite power status
   - Run unloaded if offsite power maintained
   - Output breakers auto-close onto dead Class 1E buses if offsite power lost

7. **Auxiliary Cooling System Alignment:**
   - SWS and CCW realign to emergency configurations
   - Start signals sent to pumps

8. **Control Room Ventilation Isolation:**
   - Normal supply isolated (prevents smoke/radioactivity entry)
   - Emergency ventilation actuated (recirculates through filters/adsorbers)

9. **Containment Ventilation Isolation:**
   - Purge supply and exhaust isolated
   - Hydrogen vent system isolated

### 12.3.2.3 SI Actuation Reset

#### Retentive Memory
- Automatic SI actuation places retentive memory in "ON" position
- Relay K1 energizes → closes K1 contacts
- One K1 contact powers output relays
- Other K1 contact provides alternate power to K1 (seal-in)
- SI functions continue even if original signal clears

#### Reset Logic Requirements
1. Time delay relay must time out (45-60 seconds)
2. Reactor trip must be present (P-4 permissive)
3. Operator depresses reset pushbutton

**Purpose of Time Delay:**
- Ensures all system/component realignments complete before reset
- Prevents interruption of valid SI actuation
- DBA sequencers need time to complete start sequences

#### Reset Sequence
1. K1 energizes → K1 contact in reset circuit closes → TD relay starts timing
2. TD relay times out → TD contact closes
3. P-4 (reactor trip) closes P4 contact
4. Operator depresses reset pushbutton
5. R1 relay energizes → R1 "a" contact closes (holds reset)
6. R1 "b" contacts open → TD relay de-energizes, K1 de-energizes
7. Output relay power removed → Retentive memory in "OFF" position

#### Important Reset Characteristics
- Resetting does NOT turn off ESF equipment or realign valves
- Only removes the start/actuation signal
- Operators can then control equipment per EOPs
- **All automatic SI actuations blocked after reset**
- Manual SI still available

#### Re-enabling Automatic SI
- Close reactor trip breakers (removes P-4)
- Ensure all SI actuation signals have cleared first
- Otherwise actuation sequence restarts when breakers close

---

## 12.3.3 Containment Spray Actuation

### 12.3.3.1 Containment Spray Actuation Signals

| Signal | Coincidence | Setpoint |
|--------|-------------|----------|
| High-high containment pressure | 2/4 | 30 psig |
| Manual | 2/2 switches simultaneously | — |

**Purpose:** Large high energy line break inside containment (LOCA or secondary break)

### 12.3.3.2 Containment Spray Actuation Functions
- Starts containment spray pumps
- Opens spray header isolation valves
- Aligns spray additive tank to pump suctions
- Reduces containment temperature and pressure

**Note:** Starting spray pumps also requires SI actuation (initiates DBA sequencers)

---

## 12.3.4 Containment Isolation

### 12.3.4.1 Containment Isolation Phase A

#### Phase A Actuation Signals

| Signal | Coincidence |
|--------|-------------|
| Any SI actuation | Per SI actuation signals |
| Manual | 1/2 switches |

#### Phase A Functions
- Shuts redundant isolation valves/dampers in all non-essential penetrations
- One valve inside containment, one outside

**Penetrations Remaining Open:**
- CCW supply/return to RCPs (maintains forced circulation for decay heat removal)
- Main steam lines (steam dump available for decay heat removal)

### 12.3.4.2 Containment Isolation Phase B

#### Phase B Actuation Signals

| Signal | Coincidence | Setpoint |
|--------|-------------|----------|
| High-high containment pressure | 2/4 | 30 psig |
| Manual containment spray actuation | — | — |

**Simultaneous with containment spray actuation**

#### Phase B Functions
- Closes CCW isolation valves to RCPs
- Together with steam line isolation, completes containment isolation for large breaks

---

## 12.3.5 Steam Line Isolation

### 12.3.5.1 Steam Line Isolation Actuation Signals

| Signal | Coincidence | Setpoint |
|--------|-------------|----------|
| High-high containment pressure | 2/4 | 30 psig |
| High steam flow + (low-low T_avg OR low steam pressure) | See SI signals | Variable/600 psig/553°F |

**Note:** Manually blocking high steam flow SI actuation does NOT block steam line isolation

### 12.3.5.2 Steam Line Isolation Functions
- Closes all MSIVs
- Closes all MSIV bypass valves
- Closes all steam line high pressure drain valves

---

## 12.3.6 Feedwater Isolation

### 12.3.6.1 Feedwater Isolation Actuation Signals

| Signal | Coincidence | Setpoint |
|--------|-------------|----------|
| Low T_avg + Reactor Trip (P-4) | 2/4 RCS loops | 564°F |
| High SG water level (P-14) | 2/3 on 1/4 SGs | 69% |
| Any SI actuation | Per SI signals | — |

### Benefits of MFW Isolation

1. **Prevents overcooling** — Large "shrink" after trip causes high feed demand; isolation prevents cold feedwater at high rate
2. **Prevents SG overfill** — Avoids water introduction to steam piping
3. **Isolates non-seismic piping** — Separates non-Seismic Category I feedwater piping from safety equipment

### 12.3.6.2 Feedwater Isolation Functions

**All signals close:**
- 4 main feedwater regulating valves
- 4 bypass regulating valves
- 8 feedwater isolation valves

**High SG level and SI actuation also:**
- Trip main feed pumps
- Trip main turbine

---

## 12.3.7 Auxiliary Feedwater Actuation

### 12.3.7.1 Auxiliary Feedwater Actuation Signals

| Signal | Coincidence | Setpoint |
|--------|-------------|----------|
| SI actuation | Per SI signals | — |
| Low-low SG water level | 2/3 on 1/4 SGs | 11.5% |
| ESF bus undervoltage | 1/2 taken twice | 2560 V, 1.1-sec delay |
| Trip of all main feed pumps | 2/2 | — |

**Note:** AMSAC (ATWS mitigation) also actuates AFW but is not generated by RPS

### 12.3.7.2 Auxiliary Feedwater Actuation Functions

**Train "A" (Steam-Driven) Pump:**
- Opens all 4 steam supply valves (one from each main steam line)
- Opens turbine trip and throttle valve

**Train "B" (Diesel-Driven) Pump:**
- Starts diesel engine
- Unisolates service water line for engine/pump cooling

**Both Trains:**
- Closes all SG blowdown isolation valves
- Closes all SG sampling line isolation valves

---

## 12.3.8 Summary

ESF actuations place plant in most stable, safe shutdown condition following an accident:
- Safety injection actuation
- Containment spray actuation
- Containment isolation (Phase A and Phase B)
- Steam line isolation
- Feedwater isolation
- Auxiliary feedwater actuation

---

## Critical Data for Simulator Development

### ESF Actuation Setpoints Summary

| Function | Signal | Setpoint | Coincidence |
|----------|--------|----------|-------------|
| SI | Low PZR pressure | 1807 psig | 2/3 |
| SI Block (P-11) | Low PZR pressure | 1915 psig | 2/3 |
| SI | High containment pressure | 3.5 psig | 2/3 |
| SI | High steam line ΔP | 100 psi | 1 line vs 2 others |
| SI | Low steam pressure (with high flow) | 600 psig | 2/4 |
| SI | Low-low T_avg (with high flow) | 553°F | 2/4 |
| SI Block (P-12) | Low-low T_avg | 553°F | 2/4 |
| Containment spray | High-high containment pressure | 30 psig | 2/4 |
| Phase B isolation | High-high containment pressure | 30 psig | 2/4 |
| Steam line isolation | High-high containment pressure | 30 psig | 2/4 |
| FW isolation | Low T_avg (with P-4) | 564°F | 2/4 |
| FW isolation | High SG level (P-14) | 69% | 2/3 on 1/4 SGs |
| AFW actuation | Low-low SG level | 11.5% | 2/3 on 1/4 SGs |
| AFW actuation | ESF bus undervoltage | 2560 V | 1/2 × 2, 1.1s delay |

### SI Reset Requirements

| Condition | Requirement |
|-----------|-------------|
| Time delay | 45-60 seconds |
| Reactor trip (P-4) | Must be present |
| Operator action | Depress reset pushbutton |

### Containment Pressure Setpoints

| Function | Setpoint |
|----------|----------|
| SI actuation (high) | 3.5 psig |
| Containment spray (high-high) | 30 psig |
| Phase B isolation (high-high) | 30 psig |
| Steam line isolation (high-high) | 30 psig |

### Equipment Actuated by SI

| System/Component | Action |
|------------------|--------|
| Reactor | Trip |
| Charging pumps | Start, realign to RWST/cold legs |
| SI pumps | Start |
| RHR pumps | Start |
| Diesel generators | Start |
| AFW system | Actuate |
| MFW system | Isolate |
| Containment | Phase A isolation |
| Control room ventilation | Isolate, emergency mode |
| Containment ventilation | Isolate |
| SWS/CCW | Realign to emergency configuration |

---

## References

- NRC HRTD Section 12.1 — Reactor Protection System
- NRC HRTD Section 12.2 — Reactor Protection System - Reactor Trips
- NRC HRTD Section 5.1 — Residual Heat Removal System
- NRC HRTD Section 5.2 — Emergency Core Cooling Systems
- NRC HRTD Section 5.4 — Containment Spray System
- NRC HRTD Section 5.7 — Auxiliary Feedwater System

---
