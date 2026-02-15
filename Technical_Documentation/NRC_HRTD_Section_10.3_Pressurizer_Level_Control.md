# NRC HRTD Section 10.3 — Pressurizer Level Control System

**Source:** https://www.nrc.gov/docs/ML1122/ML11223A290.pdf  
**Retrieved:** 2026-02-15  
**Revision:** Rev 0502

---

## Overview

This document provides comprehensive technical details on the Westinghouse PWR Pressurizer Level Control System including:

- Level control system description and purpose
- Level transmitter configuration and calibration
- Level program (function of T_avg)
- Charging flow control methods
- Low level interlocks and heater protection
- High level reactor trip

---

## Learning Objectives

1. State the purposes of the pressurizer level control system
2. List and describe the purposes (bases) of the protective signal provided by pressurizer level instrumentation
3. Identify the instrumentation signal used to generate the pressurizer level program, and explain why level is programmed
4. Explain how charging flow is controlled in response to pressurizer level error signals
5. Explain the purposes of the pressurizer low level interlocks

---

## 10.3.1 Introduction

### Purposes of Pressurizer Level Control System

1. Control charging flow to maintain a programmed level in the pressurizer
2. Provide inputs to pressurizer heater control and letdown isolation valves for certain level conditions

---

## 10.3.2 System Description

### Steady-State Operation
- Unchanging pressurizer level indicates balance between:
  - Charging flow INTO RCS
  - Letdown flow FROM RCS to CVCS

### Transient Response
- Level changes as reactor coolant expands or contracts with T_avg changes

### Why Level is Programmed (Not Constant)

**Problem with Constant Level Control:**
1. Temperature increase → Coolant expands → Level rises above setpoint
2. Level control decreases charging flow
3. Letdown flow is constant (75 gpm)
4. Charging < Letdown → VCT level increases
5. VCT high level → Water diverted to holdup tanks
6. Diverted water treated as liquid waste → Burden on radwaste systems

**Problem with Temperature Decrease:**
- Coolant contracts → Large demand on makeup system

**Solution: Programmed Level**
- Level programmed to follow natural expansion/contraction of reactor coolant
- Minimizes demands on liquid waste and CVCS systems

---

## 10.3.3 Component Descriptions

### 10.3.3.1 Level Transmitters

**Measurement Principle:**
- Differential pressure between:
  - External column of known height (reference leg)
  - Variable column inside pressurizer (variable leg)
- d/P converted to level signal (0-100%)

**Transmitter Configuration:**
| Quantity | Calibration | Purpose |
|----------|-------------|---------|
| 3 | Normal operating temperature | Indication, control, protection |
| 1 | Cold conditions | Cold shutdown indication, bubble draw |

**Reference Leg Design:**
- External, bellows-type, sealed reference leg
- Condensate pot at top generates static pressure head

**Temperature Compensation:**
- Water density varies with temperature
- Level instruments calibrated for pressurizer temperature
- Cold-calibrated transmitter for shutdown operations

**Channel Selection:**
- Selector switch on main control board
- Operator selects 2 of 3 hot-calibrated transmitters for control
- One channel: Level control, letdown isolation, heater cutoff
- Other channel: Backup letdown isolation and heater cutoff
- Third channel: Available as replacement during testing/failures
- Separate selector for recording any one of three transmitters

---

## 10.3.4 System Interrelationships

### 10.3.4.1 Control Channel

**Master Pressurizer Level Controller:**
- Input: Error between actual level and programmed reference level
- Type: PI (Proportional + Integral) controller
- Output: Varies CVCS charging flow
- Prevents reaction to small temporary perturbations
- Eliminates steady-state level errors

**Charging Flow Control Methods:**

#### Method 1: Positive Displacement Charging Pump
- Master level controller output → Proportional (P) pump speed controller
- Varies pump speed to control charging flow

#### Method 2: Centrifugal Charging Pump
- Master level controller output → Charging flow setpoint
- Compared to actual flow from FT-121 (downstream of FCV-121)
- Error → PI charging flow controller
- Controls air pressure to FCV-121 operator
- **FCV-121 is air-to-close:** Increasing controller output closes valve

### Level Program

**Program Input:** Auctioneered high T_avg

**Purpose:** Level follows natural expansion characteristics of reactor coolant

**Level Program Limits:**

| Setpoint | Value | Basis |
|----------|-------|-------|
| Low level | 25% | Prevents emptying after reactor trip; ensures 10% step load increase doesn't uncover heaters |
| High level | 61.5% | Natural expansion from no-load to full power T_avg (557°F to 584.7°F) starting at 25% |

**High Level Setpoint (61.5%) Basis:**
1. Low enough to prevent going solid after turbine trip from 100% without direct reactor trip (no operator action, no automatic control response)
2. Low enough that 50% step load reduction insurge doesn't reach high level trip (with proper rod control and steam dump response)

### Anticipatory Heater Control

**Condition:** Level > Program level + 5%
**Action:** Automatically energizes backup heaters

**Rationale:**
- Insurge water is cooler than pressurizer water
- Large insurge ultimately causes pressure DECREASE (despite level increase)
- Energizing heaters anticipates and offsets pressure reduction
- Limits pressure reduction during load decrease transients

**Transient Sequence (Step Load Decrease):**
1. RCS temperature increases (reactor power > secondary load)
2. Cooler water insurge to pressurizer
3. Over time, cooler water would reduce pressure
4. Followed by larger outsurge as rod control brings T_avg to program
5. Backup heaters energized to limit pressure reduction

### Low Level Interlock (17%)

**Actions on 17% Level:**
1. Low level alarm
2. Closes one letdown isolation valve
3. Closes all orifice isolation valves
4. Turns off all pressurizer heaters

**Purposes:**
- Isolating letdown prevents further level decrease
- Heater cutoff protects heaters from damage in steam environment

### 10.3.4.2 Redundant Isolation Channel

Uses second selected level channel with two bistables:

| Bistable | Setpoint | Actions |
|----------|----------|---------|
| High level alarm | 70% | Alarm only |
| Low level isolation | 17% | Closes second letdown isolation valve, redundant orifice isolation close signal, turns off all heaters |

### 10.3.4.3 Pressurizer High Level Reactor Trip

| Parameter | Value |
|-----------|-------|
| Setpoint | 92% |
| Coincidence | 2/3 |
| Active when | Reactor OR turbine power ≥ 10% (P-7) |

**Purposes:**
1. Protect RCS pressure boundary by tripping before pressurizer goes solid
2. Backup to high pressurizer pressure reactor trip
3. Prevent water discharge through pressurizer safety valves (could mechanically damage valves)

---

## 10.3.5 Summary

### Level Control
- Maintains RCS water inventory by varying charging rate
- Programmed level follows natural coolant expansion/contraction

### Low Level Protection
- Isolates letdown on low level (minimizes LOCA effects)
- Turns off heaters (protects from steam environment damage)

### Anticipatory Heating
- Energizes backup heaters if level > program + 5%
- Anticipates pressure decrease during load reduction transients

### High Level Protection
- Reactor trip at 92% prevents solid pressurizer operation
- Provides RCS boundary protection

---

## Critical Data for Simulator Development

### Level Setpoints

| Parameter | Value | Function |
|-----------|-------|----------|
| No-load program level | 25% | Minimum program setpoint |
| Full-power program level | 61.5% | Maximum program setpoint |
| No-load T_avg | 557°F | Level program input |
| Full-power T_avg | 584.7°F | Level program input |
| Low level isolation | 17% | Letdown isolation, heater cutoff |
| High level alarm | 70% | Alarm only |
| High level reactor trip | 92% | 2/3 coincidence, at-power only |
| Backup heater energize | Program + 5% | Anticipatory control |

### Level Program Calculation

**Input:** Auctioneered high T_avg

**Linear Program:**
```
Level_program = 25% + [(T_avg - 557°F) / (584.7°F - 557°F)] × (61.5% - 25%)
Level_program = 25% + [(T_avg - 557°F) / 27.7°F] × 36.5%
```

| T_avg (°F) | Programmed Level (%) |
|------------|---------------------|
| 557 | 25.0 |
| 565 | 35.5 |
| 570 | 42.2 |
| 575 | 48.9 |
| 580 | 55.5 |
| 584.7 | 61.5 |

### Control Logic

**Charging Flow Control (Centrifugal Pump):**
```
Level Error = Actual Level - Program Level
     ↓
Master Level Controller (PI)
     ↓
Flow Setpoint
     ↓
Compare to FT-121 (actual flow)
     ↓
Flow Error
     ↓
Charging Flow Controller (PI)
     ↓
FCV-121 Position (air-to-close)
```

**Level-Based Heater Control:**
| Condition | Action |
|-----------|--------|
| Level > Program + 5% | Energize backup heaters |
| Level < 17% | De-energize all heaters |

### Level Transmitter Configuration

| Channel | Calibration | Control Function | Protection Function |
|---------|-------------|------------------|---------------------|
| I | Hot | Control/Isolation (selectable) | High level trip (2/3) |
| II | Hot | Control/Isolation (selectable) | High level trip (2/3) |
| III | Hot | Backup/Spare (selectable) | High level trip (2/3) |
| IV | Cold | Cold shutdown indication only | None |

### Interlocks Summary

| Setpoint | Actions |
|----------|---------|
| 17% (low) | Close letdown isolation valves, close orifice isolation valves, de-energize heaters, alarm |
| 70% (high) | Alarm |
| 92% (trip) | Reactor trip (if P-7 active) |

---

## References

- NRC HRTD Section 4.1 — Chemical and Volume Control System
- NRC HRTD Section 10.2 — Pressurizer Pressure Control System
- NRC HRTD Section 12.2 — Reactor Protection System

---
