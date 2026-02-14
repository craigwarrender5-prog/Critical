# NRC HRTD Section 5.7 — Generic Auxiliary Feedwater System

**Source:** https://www.nrc.gov/docs/ML1122/ML11223A229.pdf  
**Retrieved:** 2026-02-14  
**Revision:** Rev 0603

---

## Overview

This document provides comprehensive technical details on the Westinghouse PWR Auxiliary Feedwater (AFW) System including:

- System purpose and design basis
- Pump configurations (motor-driven and turbine-driven)
- Water supply sources (CST and ESW backup)
- Automatic start signals
- Level control valves and loop-break protection
- PRA insights and risk-important failure modes

---

## Key Technical Data for Simulator Development

### System Purpose

1. Provide feedwater to steam generators to maintain heat sink for:
   - Loss of main feedwater (MFW)
   - Unit trip with loss of offsite power
   - Small-break LOCA

2. Provide feedwater source during plant startup and shutdown

### Design Requirements

- Automatically start and supply sufficient flow to prevent:
  - Relief of primary coolant through pressurizer safety valves
  - Steam generator dryout (maintain minimum tube coverage)
- Adequate capacity to:
  - Maintain reactor at hot standby
  - Cool reactor coolant to RHR entry temperature (<350°F)

---

## System Configuration

### Pump Configuration
| Pump Type | Quantity | Flow Rate | Discharge Pressure | SGs Supplied |
|-----------|----------|-----------|-------------------|--------------|
| Motor-Driven | 2 | 440 gpm each | ~1300 psig | 2 SGs each |
| Turbine-Driven | 1 | 880 gpm | ~1200 psig | All 4 SGs |

**Total System Capacity:** 1760 gpm (both motor-driven + turbine-driven)

### Pump Details

**Motor-Driven Pumps:**
- Multistage, horizontal, centrifugal
- Powered from 4.16 kVac Class 1E vital distribution
- Local and control room operation
- Control switch positions: RUN, STOP, PULL-TO-LOCK
- Pull-to-lock prevents auto start even with actuation signal

**Turbine-Driven Pump:**
- Multistage, horizontal, centrifugal
- Steam turbine rated at 1100 HP
- Steam supply pressure range: 100 - 1275 psig
- Steam from SG #2 and #3, upstream of MSIVs
- Each supply line has main valve + bypass valve (for warming)
- Exhaust to atmosphere (non-condensing)
- Governor valve (HV-313) controls turbine speed from control room

---

## Automatic Start Signals

### Motor-Driven Pumps (Both Start)
1. Low-low water level in **any single** steam generator
2. Loss of **one** main feed pump if power > 80%
3. Loss of **both** main feed pumps at any power level
4. Safety injection actuation signal
5. Loss of power to Class 1E vital distribution system

### Turbine-Driven Pump
1. Low-low water level in **2 of 4** steam generators
2. Loss of **one** main feed pump if power > 80%
3. Loss of **both** main feed pumps at any power level
4. Safety injection actuation signal
5. Loss of power to **either** Class 1E vital distribution system

**Note:** Motor-driven pumps are more sensitive to single SG level loss (1/4 vs 2/4)

---

## Water Supplies

### Primary Supply: Condensate Storage Tank (CST)
- Gravity feed to all AFW pumps
- Technical Specification minimum: ~280,000 gallons reserved
- Suction header with check valves and isolation valves
- Standpipe or level control valve ensures minimum reserve

### Backup Supply: Essential Service Water (ESW)
- Seismically qualified emergency supply
- Separate ESW train feeds each motor-driven pump
- Turbine-driven pump can receive from either ESW train
- **Automatic Switchover:**
  - 2/3 low suction pressure switches per pump
  - Coincident with pump running
  - Opens ESW supply valve automatically

**Caution:** ESW is poor quality water — used only in emergencies

### Emergency Fire Water Connection
- Blind flange connection on each motor-driven pump discharge
- Spool piece allows tie-in to high pressure fire system
- SG pressure must be < 120 psig for fire water use
- Last resort backup

---

## Level Control Valves

**Configuration:**
- 8 level control valves total
- 4 on motor-driven pump lines (one per SG)
- 4 on turbine-driven pump lines (one per SG)

**Operation:**
- Normally fully closed
- Toggle switch: manual OPEN or CLOSE
- Auto control: maintains SG level at preselected setpoint on actuation

**Loop-Break Protection (some plants):**
- Monitors pressure between level control valve and SG
- If pressure drops to ~100 psig (indicates break):
  - Signal closes level control valve
  - Prevents continued feed to faulted SG

---

## Design Data Summary

| Parameter | Value |
|-----------|-------|
| Number of pumps | 3 (2 motor, 1 turbine) |
| Motor-driven pump flow | 440 gpm each |
| Turbine-driven pump flow | 880 gpm |
| System design pressure | 1650 psig (where necessary) |
| Design feedwater temperature | 40 - 120°F |
| Motor-driven discharge head | ~1300 psig |
| Turbine-driven discharge head | ~1200 psig |
| Steam turbine rating | 1100 HP |
| Steam turbine pressure range | 100 - 1275 psig |
| CST minimum reserve | ~280,000 gallons (Tech Spec) |

---

## Flow Requirements

### Design Basis
- 440 gpm to **two** steam generators prevents:
  - Relief of primary coolant via PZR safety valves
  - SG water level dropping below minimum for tube coverage

### Delayed Response Scenario
- If AFW delayed 10 minutes after demand:
  - 880 gpm required to meet same criteria
  - Higher flow compensates for delayed response

---

## Safety Classification

**Safety-Related Portions:**
- Both safety-grade pumps and associated valves
- Designed for seismic events
- Meet single-failure criteria
- Provide required flow to 2+ SGs regardless of any single active or passive failure

**Redundancy:**
- Two motor-driven pumps powered from different vital buses
- Turbine-driven pump is ac power independent
- Control power from third dc channel (distinct from motor-driven pump channels)
- Separate control air subsystems serve each pump train

**Seismic Category I Components:**
- Most AFW system components
- **Exceptions (Category II):**
  - Pump recirculation line
  - Condensate storage tank (at some plants)
  - Electric AFW pump with associated piping (at some plants)

---

## System Features

### LOCA Support
- During small-break LOCA, primary-to-secondary heat transfer may be necessary
- Decay heat removal via steam generators + AFW system
- Core cooling flow (ECCS injection + break flow) may not be sufficient alone

### Redundant Power and Air
- Motor-driven pumps: separate vital ac buses
- Turbine-driven pump: steam from multiple SGs (upstream of MSIVs)
- Valves: both electric and pneumatic control available
- Appropriate measures prevent interaction between subsystems

### Material Construction
- Generally carbon steel
- CST lined to prevent corrosion
- Chemical treatment protects other components

---

## PRA Insights

### Contribution to Core Damage
- AFW system loss is relatively **small** contributor to core damage frequency
- Zion: 1.4%
- Sequoyah: 2.6%
- Surry: 14.8% (two PORVs required for bleed-and-feed)

### Why Contribution is Limited
- Availability of bleed-and-feed cooling using:
  - High pressure injection
  - Pressurizer PORVs
- At plants requiring 2 PORVs for bleed-and-feed, AFW is more critical

### Risk-Important Failure Modes (in order of importance)
1. Turbine-driven pump failure to start or run
2. Motor-driven pump failure to start or run
3. Pump unavailable due to testing or maintenance
4. Valve failures (steam admission, trip/throttle, flow control, discharge, suction)
5. Valves in testing or maintenance

### Common-Mode Failures (Plant-Specific)
- Undetected flow diversion through cross-connect (multi-unit sites)
- Steam binding from MFW leakage through check valves
- Failure to unlock locked-out pump
- Failure to realign after testing/maintenance

---

## Accident Sequence Categories

### 1. Loss of Power System
- Loss of offsite power + AFW failure
- PORVs cannot open (loss of actuating power)
- No bleed-and-feed → core damage

- Station blackout fails all ac except vital 1E inverters
- Only turbine-driven AFW available
- Battery depletion or hardware failure → AFW loss → core damage

### 2. Transient-Caused Trip
- Trip + loss of power conversion system + AFW failure
- Bleed-and-feed failure (operator error or hardware) → core damage

### 3. Loss of Main Feedwater
- Feedwater line break affects common water source
- Operators fail to provide alternate sources
- Fail to initiate bleed-and-feed → core damage

### 4. Steam Generator Tube Rupture
- SGTR + AFW failure
- Coolant lost until RWST depleted
- HPI fails (no recirculation from empty sump) → core damage

---

## Critical Notes for Simulator

1. **Startup/Shutdown Use:**
   - AFW provides feedwater when MFW system not operating
   - Maintains SG inventory during low-power operations
   - Level control to maintain operating level

2. **Loss of Offsite Power:**
   - Motor-driven pumps fail (loss of ac)
   - Turbine-driven pump remains available (steam-powered)
   - Critical for decay heat removal without ac power

3. **Steam Supply Diversity:**
   - Turbine-driven pump steam from SG #2 and #3
   - Upstream of MSIVs → available even with main steam isolated
   - Can operate with steam pressure as low as 110 psig

4. **Suction Source Priority:**
   - CST is primary source (gravity feed)
   - ESW is automatic backup on low suction pressure
   - Fire water is last resort (manual connection)

5. **Single-Failure Considerations:**
   - Feedwater line rupture may be initiating event
   - System designed to supply 2+ SGs regardless of single failure
   - Only 2 SGs required for any credible accident

---

## Implementation Priority for Simulator

**Phase 1 (Basic Capability):**
- Motor-driven pump start/run simulation
- Turbine-driven pump start/run simulation
- CST suction source
- Flow to steam generators
- Basic level control valve operation

**Phase 2 (Automatic Functions):**
- Automatic start signal logic (all 5 signals)
- ESW automatic switchover on low suction pressure
- SG level control in automatic mode
- Turbine speed control via governor valve

**Phase 3 (Advanced Features):**
- Loop-break protection logic
- Steam supply valve operation and warming
- Pull-to-lock interlock logic
- Detailed flow modeling with system pressure

---

## References

This document should be referenced for:
- AFW system design parameters
- Automatic start signal logic
- Pump and suction source specifications
- Safety classification and redundancy
- PRA insights and risk-important failures
- Decay heat removal flowpath with AFW

---
