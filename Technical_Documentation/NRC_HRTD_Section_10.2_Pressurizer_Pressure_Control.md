# NRC HRTD Section 10.2 — Pressurizer Pressure Control System

**Source:** https://www.nrc.gov/docs/ML1122/ML11223A287.pdf  
**Retrieved:** 2026-02-15  
**Revision:** Rev 1208

---

## Overview

This document provides comprehensive technical details on the Westinghouse PWR Pressurizer Pressure Control System including:

- Pressure control system description and operation
- Heater control (proportional and backup banks)
- Spray valve control
- PORV operation and interlocks
- Reactor protection signals from pressure transmitters
- Cold overpressure protection system (COPS)
- Control system setpoints

---

## Learning Objectives

1. List and describe the purposes (bases) of the protective signals provided by the pressurizer pressure transmitters
2. List and describe the purposes of the permissives and interlocks provided by the pressurizer pressure transmitters
3. List in sequence the actions performed by the pressurizer pressure control system during pressure increase or decrease
4. Explain the effect of changing the pressure control setpoint on control and protective functions
5. List the inputs to the cold overpressure protection system, and explain the operation of the system

---

## 10.2.1 Introduction

### System Purpose
Maintains RCS pressure within a narrow band around an operator-selectable setpoint.

### Key Parameters
| Parameter | Value |
|-----------|-------|
| Normal pressure setpoint | 2235 psig |
| Setpoint adjustment range | 1700 - 2500 psig |

### Pressure Control Components
1. **Electrical heater banks** — Increase pressure (flash water to steam)
2. **Pressurizer spray valves** — Decrease pressure (condense steam)
3. **Power-operated relief valves (PORVs)** — Limit overpressure transients

### Master Controller
- Input: Error between actual pressure and setpoint
- Output: Controls heaters, spray valves, and one PORV
- Type: Proportional + Integral + Derivative (PID) controller

---

## 10.2.2 System Description

### Heater Banks

| Bank | Type | Control | Quantity |
|------|------|---------|----------|
| Bank C | Proportional | Variable (% of 10-sec interval energized) | 1 bank |
| Banks A, B, D | Backup | Bistable (on/off) | 3 banks |

### Pressure Control Sequence

#### Decreasing Pressure (Below 2235 psig)
1. Master controller increases proportional heater output
2. If pressure continues to decrease, backup heaters energize
3. Heaters add energy → water flashes to steam → pressure increases

#### Increasing Pressure (Above 2235 psig)
1. Master controller decreases proportional heater output
2. If pressure continues to increase, spray valves modulate open
3. Cold spray water condenses steam → pressure decreases
4. If spray cannot control, PORVs open
5. PORVs discharge steam to pressurizer relief tank (PRT)

### Overpressure Protection Hierarchy
1. Spray valves
2. PORVs (2)
3. High pressure reactor trip (2385 psig)
4. Code safety valves (3) — Final protection for RCPB

### Master Controller Characteristics

**PID Controller Functions:**
- **Proportional:** Output proportional to pressure error magnitude
- **Integral:** Output varies with duration of error (returns pressure to setpoint)
- **Derivative:** Output varies with rate of change (responds to rapid changes)

### Normal Operation

**Steady-State:**
- Heaters and spray valves in automatic
- Proportional heaters energized for small percentage of each 10-sec interval
- Compensates for:
  - Continuous bypass spray flow (~1 gpm)
  - Ambient heat losses

**Enhanced Spray Operation:**
- Many plants manually energize some backup heaters
- Creates additional steam → pressure rises above setpoint
- Controller opens spray valves to counteract
- Maintains pressurizer boron concentration equal to RCS

### Alarms
- **Low pressure alarm:** Alerts operators to potential DNB LCO concerns

### PORV Operation

**Two PORVs with Different Control Logic:**

| PORV | Opening Logic |
|------|---------------|
| PCV-456 | Fixed bistable: 2335 psig (Channel II or IV) + interlock (Channel III) |
| PCV-455A | Master controller output (100 psi error) + interlock (Channel IV) |

**Purpose:**
- Maintain RCS pressure below high pressure reactor trip setpoint
- Prevent/minimize code safety valve lifts
- Discharge to pressurizer relief tank

---

## 10.2.3 Reactor Protection Signals

Four pressure transmitters provide protective functions with required coincidence logic.

### 10.2.3.1 High Pressure Reactor Trip

| Parameter | Value |
|-----------|-------|
| Setpoint | 2385 psig |
| Coincidence | 2/4 |
| Purpose | RCPB protection |
| Blockable | No |

### 10.2.3.2 Low Pressure Reactor Trip

| Parameter | Value |
|-----------|-------|
| Setpoint | 1865 psig |
| Coincidence | 2/4 |
| Purpose | DNB protection |
| Active when | Reactor or turbine power > 10% |
| Characteristic | Rate sensitive |

### 10.2.3.3 Overtemperature ΔT Trip

- Each of 4 RPS channels has dedicated OTΔT calculator
- Pressurizer pressure is input to setpoint calculation
- Provides DNB protection
- Channel I pressure → RPS Channel I calculator, etc.

### 10.2.3.4 Safety Injection Actuation

| Parameter | Value |
|-----------|-------|
| Setpoint | 1807 psig |
| Coincidence | 2/3 (Channels I, II, III) |
| Purpose | LOCA protection |
| Later plants | 2/4 coincidence |

---

## 10.2.4 Permissives and Interlocks

### 10.2.4.1 ESF Actuation Block (P-11)

| Parameter | Value |
|-----------|-------|
| Block permissive | < 1915 psig (2/3) |
| Auto-reinstate | ≥ 1915 psig (2/3) |
| Purpose | Allow normal cooldown without ESF actuation |

**Per IEEE 279-1979:** Protective feature automatically reinstated when plant state requires it.

### 10.2.4.2 Relief Valve Interlocks

**Purpose:** Prevent inadvertent PORV opening (small-break LOCA through pressurizer top)

**Each PORV has interlock:**
- Prevents opening if pressure < 2335 psig
- Requires second bistable confirmation from independent pressure transmitter

**PORV Opening Requirements:**

| PORV | Requirements for Automatic Opening |
|------|-----------------------------------|
| PCV-456 | Channel II or IV ≥ 2335 psig + Channel III ≥ 2335 psig + Switch in AUTO |
| PCV-455A | Channel IV ≥ 2335 psig + Master controller output calls for open + Switch in AUTO |

### Cold Overpressure Protection System (COPS)

**Technical Specification Requirement:**
COPS operable when RCS cold-leg temperature < predetermined value

**Options:**
- Two PORVs
- Combination of relief valves with sufficient capacity
- Suitably large RCS vent

**Operation:**

| Condition | Action |
|-----------|--------|
| At-power | COPS switches in BLOCK position |
| RCS pressure < 375 psig | Operators unblock COPS per procedures |
| Pressure ≥ 400 psig (unblocked) | Control room alarm |

**COPS PORV Setpoints:**
| PORV | Opens at | Pressure Transmitter |
|------|----------|---------------------|
| PCV-455A | 425 psig | PT-403 |
| PCV-456 | 475 psig | PT-405 |

- PORVs close in reverse order as pressure decreases
- Manual opening independent of pressure and COPS status

---

## 10.2.5 System Controls

### 10.2.5.1 Channel Selector Switch

Three-position switch on control board:
- Selects pressure transmitter for control and PORV actuation
- Used during channel testing or transmitter failure
- Channel I or III → Master controller input
- Channel II or IV → PCV-456 opening logic
- Channel III always → PCV-456 interlock
- Channel IV always → PCV-455A interlock

### 10.2.5.2 Master Pressure Controller

- PID controller
- Setpoint adjustable via potentiometer (1700-2500 psig)
- Located "downstream" of isolation amplifiers
- **Setpoint changes affect:** Control component actuation pressures
- **Setpoint changes do NOT affect:** Protection system bistable setpoints

### 10.2.5.3 Relief Valve Controls

Three-position switch per PORV:
- OPEN (manual)
- CLOSE (manual)
- AUTO

### 10.2.5.4 Spray Valve Controls

Auto/Manual controller per spray valve:
- Manual: Direct operator control
- Auto: Master controller modulates position

**Spray Valve Setpoints (Proportional Output Only):**
| Condition | Pressure | Error from Setpoint |
|-----------|----------|-------------------|
| Start opening | 2260 psig | +25 psig |
| Fully open | 2310 psig | +75 psig |

- Linear modulation between setpoints

### 10.2.5.5 Heater Controls

**Proportional Heaters:**
- On-Off switch (Auto or De-energized)
- In Auto: Energized for percentage of each 10-sec interval

**Proportional Heater Setpoints (Proportional Output Only):**
| Condition | Pressure | Error from Setpoint |
|-----------|----------|-------------------|
| Constantly energized | 2220 psig | -15 psig |
| Constantly de-energized | 2250 psig | +15 psig |

- Linear variation between setpoints

**Backup Heaters:**
- Off-On-Auto switch per bank (3 banks)
- In Auto: Bistable control from master controller

**Backup Heater Setpoints:**
| Condition | Pressure | Error from Setpoint |
|-----------|----------|-------------------|
| Energize | 2210 psig | -25 psig |
| De-energize | 2217 psig | -18 psig |

**Remote Shutdown Panel:**
- Local-remote, on-off switches for backup heaters
- Located in auxiliary building
- For control room emergency

---

## 10.2.6 Summary

### Pressure Control
- Maintains RCS at adjustable setpoint (normally 2235 psig)
- Uses proportional heaters, backup heaters, spray valves, and PORVs
- Master controller responds to magnitude, duration, and rate of pressure deviation

### Protective Functions
- High pressure reactor trip (2385 psig) — RCPB protection
- Low pressure reactor trip (1865 psig) — DNB protection
- OTΔT trip input — DNB protection
- Low pressure SI actuation (1807 psig) — LOCA protection

---

## Critical Data for Simulator Development

### Pressure Setpoints Summary

| Function | Pressure (psig) | Notes |
|----------|-----------------|-------|
| Normal operating setpoint | 2235 | Adjustable 1700-2500 |
| Backup heaters ON | 2210 | -25 from setpoint |
| Backup heaters OFF | 2217 | -18 from setpoint |
| Proportional heaters 100% | 2220 | -15 from setpoint |
| Proportional heaters 0% | 2250 | +15 from setpoint |
| Spray valves start open | 2260 | +25 from setpoint |
| Spray valves full open | 2310 | +75 from setpoint |
| PORV PCV-455A open (controller) | ~2335 | 100 psi error |
| PORV PCV-456 open (bistable) | 2335 | Fixed setpoint |
| PORV interlock | 2335 | Both PORVs |
| High pressure reactor trip | 2385 | 2/4 coincidence |
| Low pressure reactor trip | 1865 | 2/4, >10% power |
| P-11 block permissive | 1915 | 2/3 |
| Low pressure SI actuation | 1807 | 2/3 |

### Cold Overpressure Protection Setpoints

| Parameter | Value |
|-----------|-------|
| COPS unblock (per procedures) | < 375 psig |
| COPS alarm | 400 psig |
| PCV-455A opens | 425 psig (PT-403) |
| PCV-456 opens | 475 psig (PT-405) |

### Control Logic

**Pressure Increasing:**
```
Normal → Proportional heaters reduce output → Spray valves open → 
PORV PCV-455A opens → PORV PCV-456 opens → High pressure trip → 
Code safety valves lift
```

**Pressure Decreasing:**
```
Normal → Proportional heaters increase output → Backup heaters energize → 
Low pressure alarm → Low pressure trip → Low pressure SI
```

### Heater Configuration
| Bank | Type | Control Mode |
|------|------|--------------|
| C | Proportional | Variable duty cycle (10-sec interval) |
| A | Backup | Bistable |
| B | Backup | Bistable |
| D | Backup | Bistable |

---

## References

- NRC HRTD Section 10.1 — Reactor Coolant Instrumentation
- NRC HRTD Section 10.3 — Pressurizer Level Control System
- NRC HRTD Section 12.2 — Reactor Protection System
- NRC HRTD Section 2.2 — Power Distribution Limits (DNB)

---
