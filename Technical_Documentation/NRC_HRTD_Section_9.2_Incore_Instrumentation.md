# NRC HRTD Section 9.2 — Incore Instrumentation System

**Source:** https://www.nrc.gov/docs/ML1122/ML11223A264.pdf  
**Retrieved:** 2026-02-15  
**Revision:** Rev 0404

---

## Overview

This document provides comprehensive technical details on the Westinghouse PWR Incore Instrumentation System including:

- Movable incore neutron flux monitoring system (miniature fission chambers)
- Incore temperature monitoring system (core-exit thermocouples)
- Drive system and transfer device assemblies
- Flux mapping procedures
- Incore-excore calibration
- Computer data collection and processing

---

## Learning Objectives

1. State the purposes of the incore instrumentation system
2. Briefly describe the two types of incore instrumentation and the information available from each
3. Describe the method used to detect flux thimble leakage
4. List the uses of the data obtained from the incore instrumentation

---

## 9.2.1 Introduction

### Purpose
The incore instrumentation system provides information on:
- Neutron flux distribution at selected core locations
- Fuel assembly outlet temperatures at selected core locations

**Important:** The incore instrumentation system provides **data acquisition only** and performs **no protective or plant operational control functions**.

### System Components
1. **Movable Incore Neutron Flux Monitoring System** — miniature fission chambers
2. **Incore Temperature Monitoring System** — fixed core-exit thermocouples (CETs)

### Configuration by Plant Type
| Plant Type | Flux Thimbles | Thermocouples | Movable Detectors |
|------------|---------------|---------------|-------------------|
| 4-Loop | 58 | 65 | 6 |
| 3-Loop | 50 | 51 | 5 |
| 2-Loop | 36 | 39 | 4 |

---

## 9.2.2 System Description

### 9.2.2.1 Incore Neutron Monitoring System

**Detector Type:** Movable miniature fission chambers containing U₃O₈ enriched to >90% U-235

**Uses of Incore Neutron Flux Data:**
1. Verify compliance with power distribution hot channel factors:
   - Heat flux hot channel factor F_Q(Z)
   - Nuclear enthalpy rise hot channel factor F^N_ΔH
2. Calibrate excore power range nuclear instruments for axial flux difference (AFD)
3. Verify control rod positions when rod position indication system is inoperable
4. Verify quadrant power tilt ratio (QPTR) meets technical specification limit (>75% power with inoperable PR channels)

**Operating Principle:**
- Detectors attached to flexible drive cables
- Driven into selected core locations by plant operating staff
- When not in use, stored in shielded concrete vault to minimize radiation exposure
- Retractable detector thimbles are sealed (dry inside), serving as pressure barrier (2500 psig design)
- Thimbles remain stationary during operation, retracted for refueling/maintenance

### 9.2.2.2 Incore Temperature Monitoring System

**Detector Type:** Fixed chromel-alumel thermocouples (core-exit thermocouples, CETs)

**Location:** Positioned at top of upper core plate, measuring core outlet coolant temperatures

**Uses of CET Data:**
1. Provide inputs to subcooling margin monitors (SMM)
2. Provide operators with indications of inadequate core cooling during emergencies
3. Provide inputs to plant computer for enthalpy rise calculations and limited power distribution information

**Advantages:**
- Continuously provide data easily converted to power
- On-line radial power sharing measurements
- Immediately available to operator

**Disadvantages:**
- Measurement uncertainties due to reactor coolant flow mixing patterns
- CETs normalized to incore flux detector data during periodic surveillance tests

---

## 9.2.3 Component Descriptions

### 9.2.3.1 Conduits, Guide Thimbles, Isolation Valves, and Seal Table

**Conduits:**
- Stainless steel, extending from seal table through instrument tunnel to reactor vessel bottom
- Welded to seal table and vessel penetration nozzles (leak-tight RCS pressure boundary)
- Once filled with reactor coolant, become extension of RCPB

**Guide Thimbles:**
- Closed at reactor end, open at seal table end
- Dry inside, serve as pressure boundary (2500 psia design vs atmosphere)
- Flexible stainless steel
- Remain stationary during operation, retracted for refueling
- Require 14 feet of withdrawal clearance

**Seal Table:**
- 3/8-inch thick rectangular stainless steel plate (8'6" × 2'6")
- Mounted over instrument tunnel
- 58 penetrations for guide thimbles + 2 drain holes
- Sealed with high-pressure swagelok fittings (normal operation) or low-pressure fittings (refueling)

**Isolation Valves:**
- Manually operated stainless steel
- One per guide thimble at seal table
- Forms 2500 psia barrier if guide thimble ruptures
- Cannot isolate with detector/drive cable inserted — must withdraw first

### 9.2.3.2 Drive Unit Assemblies

Each movable detector has a drive unit assembly containing:

**1. Gear Motor and Slip Clutch:**
- Two-speed reversible synchronous gear-motor
- 3-phase, 460V, 60Hz, 3600/600 rpm with gear reducer
- Integral brake when not operating
- Capable of starting under maximum load

**2. Drive Box:**
- 5-inch hobbed drive wheel
- Low speed: 12 ft/min
- High speed: 72 ft/min

**3. Storage Reel:**
- Spring-loaded take-up reel with integral locking device
- Accommodates 175 feet of drive cable
- Slip-ring assemblies for electrical signal lead-out while rotating

**4. Position Transmitter:**
- Position encoder: 0000.0 to 9999.9 inches
- Binary-coded-decimal (BCD) output
- Driven proportional to drive cable speed

**5. Withdrawal Limit Switch:**
- Located at inlet of each five-path rotary transfer device
- Functions:
  - Prevents transfer device operation when detector forward of limit
  - Stops automatic withdrawal at limit
  - Actuates cable position lamps on control panel

**6. Safety Switch:**
- Near drive unit outlet
- Prevents withdrawal attempt back over drive wheel

### 9.2.3.3 Transfer Device Assemblies

**Five-Path Rotary Transfer Device:**
- One per drive unit
- Routes detector into one of five possible paths
- S-shaped tube in rotating assembly
- Cam-actuated microswitches for path selection feedback
- Positions: OFF, NORMAL, CALIBRATE, EMERGENCY, COMMON GROUP, STORAGE

**Ten-Path Rotary Transfer Device:**
- Routes detector into any of ten selectable flux thimbles
- Cam-actuated microswitches for path selection feedback
- Detector-actuated path indicator switches for core path verification

**Wye Units:**
- Reduce interconnecting tubing between transfer devices
- Also between five-path devices and calibration path

### 9.2.3.5 Detector and Drive Cable Assemblies

**Incore Flux Detector (Fission Chamber):**
| Parameter | Value |
|-----------|-------|
| Diameter | 0.188 inches |
| Length | 2.1 inches |
| Encapsulation | Bullet-shaped stainless steel shell (0.199 in. OD) |
| Thermal neutron sensitivity | Minimum 1.0 × 10⁻¹⁷ amps/nv |
| Gamma sensitivity | Maximum 3.0 × 10⁻¹⁴ amps/R/hr |

**Drive Cable:**
- Carbon steel, hollow-core helical wrap
- Meshes with hobbed drive wheel
- OD: 0.199 inches, ID: 0.065 inches
- Length: ~175 feet (new)
- Coaxial cable (0.040 in. diameter) threaded through hollow center

### 9.2.3.6 Readout and Control Equipment

**Position Indication:**
- Nixie tube display: 0000.0 to 9999.9 inches
- BCD position signal from encoder

**Operation Selector Switch (6 positions):**
| Position | Function |
|----------|----------|
| OFF | Red light, detector prohibited from moving, 5-path aligned to NORMAL |
| NORMAL | 5-path positioned to normal 10-path transfer device |
| CALIBRATE | 5-path positioned to calibrate path wye units |
| EMERGENCY | 5-path positioned to next sequential 10-path device (A→B, B→C, etc.) |
| COMMON GROUP | 5-path positioned to 10-path transfer device C |
| STORAGE | 5-path positioned to lead-shielded concrete storage area |

**Ten-Path Selector Switch:**
- One per group
- Aligns 10-path transfer device with selected detector thimble

**Drive Motor Control:**
- AUTO mode: INSERT, SCAN, RECORD, WITHDRAW pushbuttons
- MANUAL mode: Speed switch + insert-withdraw toggle
- Drive selector: Single or multiple (all) drives

**Detector Power Supplies:**
- One per detector readout panel
- "Floating" DC voltage, variable to 300V

**Current Readout:**
- Range: 0-50 µA (meter)
- Range switch: 150 µA, 500 µA, 1.5 mA, 5 mA full scale
- Output to recorder and plant computer

**Recorders:**
- Strip-chart recorder per detector
- Chart speed synchronized with low drive speed (1 inch = 10 inches detector movement)
- Started automatically by SCAN or RECORD

### 9.2.3.7 Gas Purge System

- Dry CO₂ gas introduced during detector withdrawal
- Transfer devices in metallic enclosures
- AC solenoid valve opens during RECORD or WITHDRAW operations
- Enclosures rated for 1.0 psig internal pressure
- Maximum leakage: 15.0 ft³/hr at 0.02 inch water pressure

### 9.2.3.8 Leak Detection System

- Drain header connected to 10-path transfer device enclosure
- Liquid level actuated pressure switch
- 1/4-inch AC solenoid-operated drain valve
- Water leak → level rise → alarm + drain valve opens
- Reset silences audible alarm, seals in light
- Level drop → contact opens → drain valve closes, light extinguishes

### 9.2.3.9 Thermocouples

**Specifications (4-Loop Plant):**
| Parameter | Value |
|-----------|-------|
| Quantity | 65 |
| Type | Chromel-alumel |
| Diameter | 1/8 inch (nominal) |
| Sheath | Stainless steel |
| Insulation | Aluminum oxide |
| Termination | Male thermocouple connector |

**Routing:**
- Sensing elements mounted on/above upper core plate at fuel assembly exit
- Leads in stainless steel conduits
- Routed through support columns to upper support plate
- Exit via thermocouple port columns through reactor vessel head penetrations
- 13 thermocouple leads per port column (5 columns total)
- Sealing arrangement is part of RCPB

### 9.2.3.10 Thermocouple Reference Junction Box

- Two reference junction boxes provided
- Controlled temperature: 160°F
- Transition from chromel-alumel to copper instrument wires
- Three platinum RTDs per box:
  - Two connected to plant computer for temperature monitoring
  - One installed spare

### 9.2.3.11 Thermocouple Indicator

- Mounted in flux mapping control console
- Backup readout (normal is via plant computer)
- Double range: 100-400°F or 400-700°F
- Manual selector switches (non-locking toggle)
- Contact closure signal informs computer when thermocouple being monitored

---

## 9.2.4 Operation and System Interrelationships

### 9.2.4.1 Detector Calibration

**Procedure:**
1. Select detector, run into calibration path using INSERT and SCAN
2. Set range switch for peak output between 1/3 and full scale
3. After reaching top-of-core, manually withdraw to highest output point
4. Take voltage vs. meter readings in 10V steps (20-160V)
5. Plot saturation curve, select voltage for center of plateau region
6. Repeat for remaining detectors
7. Normalize each detector's output voltage for consistent readings

### 9.2.4.2 Flux-Mapping Procedure

**Semiautomatic Steps:**
1. Turn operation selector to NORMAL, select paths with 10-path selectors
2. Record reactor power level to ensure stable conditions
3. Set mode switch to AUTO, computer switch to ON
4. Select ALL on drive selector for simultaneous insertion
5. Press INSERT — detectors drive at high speed to bottom-of-core
6. Press SCAN — detectors drive at low speed to top-of-core (recorder starts automatically)
7. Press RECORD — detectors withdraw at low speed through core (data logged to computer)
8. Press WITHDRAW — detectors return at high speed to withdrawal limit
9. Repeat for other paths; include calibration path for normalization

### 9.2.4.3 Incore-Excore Calibration

**Purpose:**
Calibrate excore power range detectors to match incore measurements because:
- Excore detectors rely on leakage neutrons
- Distance and shielding prevent true representation of core flux distribution
- Excore detectors provide reactor protection signals and continuous indication

**AFD Definition:**
```
AFD = (Flux_top - Flux_bottom) / (Flux_top + Flux_bottom) at 100% power
```
Electronically expressed as ΔI (difference in current between upper and lower excore detectors)

**Calibration Triggers:**
- Beginning of each fuel cycle
- Monthly surveillance shows significant difference between adjusted excore AFD and incore AFD

**Procedure:**
1. Induce axial xenon transient (causes axial flux oscillation)
2. Obtain incore and excore measurements during transient
3. Perform full core and quarter core flux maps at various times
4. Record all power range excore detector currents with each incore AFD
5. Plot excore current output vs. incore AFD
6. Calculate linear regression for best-fit line
7. Adjust excore detector isolation amplifier gains

**Calibrated Parameters:**
- F₁(ΔI) penalty to OTΔT trip setpoint
- F₂(ΔI) penalty to OPΔT trip setpoint
- AFD meters on control board
- Output to detector current comparators
- AFD monitor program in plant computer

**Note:** Isolation amplifier gain adjustment affects only protection system output; does not affect raw current from excore detector to summing/level amplifier.

### 9.2.4.5 Incore Data Collection

**Signal Inputs to Computer:**
- Analog signals proportional to flux levels (movable detectors)
- Contact closure signals: detector selection, 5-path position, 10-path position
- Interrupt signals: COMPUTER OFF-ON, SCAN, RECORD

**Data Collection Sequence:**
- SCAN interrupt received when drive energized for top-of-core insertion
- Data collection starts after RECORD interrupt (withdrawal from top-of-core)

**Signal Quality Checks:**
- Three consecutive readings per data point (max 1/15 sec apart)
- Each point compared with preceding and succeeding points
- Large variations normal at core grid locations

**Normalization Corrections:**
1. Reactor power drift during flux mapping period
2. Dissimilarities between individual detectors
3. Different readout scale settings
4. Leakage current (normally at low power levels)

**Power Level Correction:**
- Total reactor power integrated from excore nuclear power channels during each pass
- Changes in subsequent excore readings provide correction ratios

**Thermocouple Computations:**
- Periodic calculation of enthalpy rise at each CET location
- Relative fuel assembly powers
- Core radial tilting factors
- Alarm messages when values exceed limits
- Complete core maps and daily thermocouple history on operator request

---

## 9.2.5 Summary

### Movable Incore Detector System
- Miniature fission chambers remotely positioned in retractable guide thimbles
- Detector welded to helical wrap drive cable and sheathed coaxial instrumentation cable
- Guide thimbles closed at reactor end — form pressure boundary
- Drive assemblies: motor-operated hobbed wheel, take-up reel, position encoders
- Five-path transfer device: selects operating mode
- Ten-path transfer device: routes detector to specific fuel assemblies

### Thermocouple System
- Provides rough approximations of core conditions
- On-line, immediately available to operator
- Guide tubes penetrate vessel head, terminate at fuel assembly flow exits
- Monitored by computer with manual backup at control console

---

## Critical Data for Simulator Development

### Detector Specifications
| Parameter | Value |
|-----------|-------|
| Fission chamber diameter | 0.188 in. |
| Fission chamber length | 2.1 in. |
| Drive cable low speed | 12 ft/min |
| Drive cable high speed | 72 ft/min |
| Position indication resolution | 0.5 in. |
| Drive cable length | ~175 ft |
| Flux thimble OD | 0.300 in. |
| Flux thimble ID | 0.199 in. |

### Thermocouple Specifications
| Parameter | Value |
|-----------|-------|
| Type | Chromel-alumel |
| Quantity (4-loop) | 65 |
| Diameter | 0.111 in. |
| Reference junction temperature | 160°F |
| Indicator range (low) | 100-400°F |
| Indicator range (high) | 400-700°F |

### Hot Channel Factors Verified by Incore Flux Mapping
| Factor | Description |
|--------|-------------|
| F_Q(Z) | Heat flux hot channel factor |
| F^N_ΔH | Nuclear enthalpy rise hot channel factor |
| QPTR | Quadrant power tilt ratio |

### CET Functions for Simulator
1. Input to Subcooling Margin Monitor (SMM) — 8 per train (2 per quadrant)
2. Inadequate core cooling indication
3. Enthalpy rise calculation (ΔT × flow → power)
4. Radial power distribution monitoring

---

## References

- NRC HRTD Section 9.1 — Excore Nuclear Instrumentation
- NRC HRTD Section 10.1 — Reactor Coolant Instrumentation (SMM inputs)
- NRC HRTD Section 12.2 — Reactor Protection System (OTΔT/OPΔT)
- Plant Technical Specifications (F_Q, F_ΔH, QPTR limits)

---
