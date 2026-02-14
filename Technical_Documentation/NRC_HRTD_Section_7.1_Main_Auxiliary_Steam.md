# NRC HRTD Section 7.1 — Main and Auxiliary Steam Systems

**Source:** https://www.nrc.gov/docs/ml1122/ML11223A244.pdf  
**Retrieved:** 2026-02-14  
**Revision:** Rev 0101

---

## Overview

This document provides comprehensive technical details on the Westinghouse PWR Main and Auxiliary Steam Systems including:

- Main steam system design from SG outlet to turbine
- Safety-related components and seismic qualifications
- Steam line isolation logic and protection
- Power-operated relief valves (PORVs) and safety valves
- Auxiliary feedwater pump steam supplies
- Steam system instrumentation
- Auxiliary steam system for plant startup/shutdown

---

## Key Technical Data for Simulator Development

### Main Steam System Operating Parameters

**Full Power Steam Flow:**
- Total: 15.07×10⁶ lb/hr (all 4 SGs)
- Per steam generator: 3.77×10⁶ lb/hr
- Steam pressure: 895 psig
- Steam temperature: 533.3°F (saturated)
- Steam quality: >99.75% (< 0.25% moisture)

**Main Steam Line Sizing:**
- Diameter: 28 inches
- Configuration: Four separate lines (one per SG)
- Bypass header: 28 inches (equalizes pressure, ensures equal SG loading)

---

## Safety-Related Portions (Seismic Category I)

**Extent:**
From steam generator outlet to first piping restraint downstream of main steam check valve

**Components in Seismic Category I Portion:**
1. Flow restrictors
2. Power-operated relief valves (PORVs)
3. Steam generator safety valves (5 per line)
4. Steam supplies to turbine-driven AFW pump
5. Main steam isolation valves (MSIVs)
6. Main steam check valves

**Design Requirements:**
- Must withstand seismic events without damage
- Must not be damaged by RCS damage
- Must not cause RCS damage if broken
- Essential for safe shutdown with or without offsite power

---

## Flow Restrictors

**Location:** Inside containment, as close as possible to SG outlet nozzle

**Construction:**
- Venturi nozzle insert welded into carbon steel pipe
- Converging/diverging sections: carbon steel
- Throat liner: Inconel

**Function:**
- Limit blowdown rate to 9.68×10⁶ lb/hr per SG in event of steam line rupture
- Limits cooldown rate of RCS → limits positive reactivity addition
- Minimizes piping forces and pipe whip potential
- Provides ΔP for steam flow measurement during normal operation

**Normal Operation:**
- Minimal resistance to steam flow
- Differential pressure used for flow measurement

---

## Main Steam Instrumentation

**Per Steam Line:**
- 2 flow transmitters (inside containment, sense ΔP across flow restrictor)
- 4 pressure transmitters (outside containment, upstream of MSIV)
- 2 radiation monitors (1 upstream, 1 downstream of MSIV)

**Flow Transmitters:**
- Inputs to:
  - High steam flow ESF actuation and steam line isolation
  - Reactor trip for anticipated loss of heat sink
  - Flow mismatch for feedwater control
  - Load reference for MFP speed control

**Pressure Transmitters:**
- One supplies PORV actuation signal
- Three provide protection inputs:
  - Feedwater control system
  - High steam line differential pressure ESF actuation
  - High steam flow ESF actuation and steam line isolation
  - AFW pump speed control (4 of 12 plant channels)
  - Density compensation for steam flow channels

**Radiation Monitors:**
- Upstream: Geiger-Mueller detector (gamma background)
- Downstream: Scintillation detector (N-16 sensitive)
- Indicate steam generator tube rupture

---

## Power-Operated Relief Valves (PORVs)

**Design:**
- One per steam line (4 total)
- Size: 6-inch
- Type: Air-operated, spring-opposed globe valve
- Capacity: ~10% of rated steam flow per SG at no-load pressure
  - 2.5% of total steam system flow per valve
- Location: Outside containment on main steam support structure

**Setpoint:**
- Nominal: 1125 psig
- Approximately halfway between no-load SG pressure and lowest safety valve setpoint

**Functions:**
1. Overpressure protection for SGs (lift before safety valves)
2. Decay heat removal path when condenser unavailable or steam dumps inoperable
3. Cooldown capability for loss of offsite power (with AFW providing SG inventory)

**Control:**
- Manual control from control room
- Can be operated from remote shutdown station
- Fail-safe: Fails SHUT on loss of instrument air or electrical signal

**Backup Nitrogen Control System:**
- Allows PORV operation during worst-case fire scenario
- Disables both electrical signals and pneumatic supplies
- Nitrogen system unisolated, 3-way ball valve repositioned
- Nitrogen regulator adjusted for desired opening signal

---

## Steam Generator Safety Valves

**Configuration:**
- 5 valves per steam line (20 total)
- Size: 6 inches
- Type: Spring-loaded

**Set Pressures (Staggered):**
1. 1170 psig
2. 1200 psig
3. 1210 psig
4. 1220 psig
5. 1230 psig

**Design Basis:**
- Highest setpoint < 110% of SG design pressure (1185 psig)
- Combined capacity: 16,467,380 lb/hr (109% of full-power steam flow)

**Functions:**
1. Overpressure protection for SGs and main steam piping
2. Decay heat removal when steam dumps and PORVs unavailable

**Location:**
- Main steam support structure outside containment
- Exhaust stacks extend ~13 ft above turbine building roof
- Opposing discharge ports

---

## AFW Pump Steam Supplies

**Configuration:**
- One 3-inch line from each main steam line (4 total)
- Location: Upstream of MSIVs
- Provides redundancy and dependability

**Isolation Valves:**
- Air-operated
- Automatically opened by AFW actuation signal
- Manual operation from control room or remote shutdown station
- Fail-safe: FAIL OPEN on loss of AC power

**Operating Cylinder:**
- Normally energized 4-way solenoid supplies air to TOP port (valve closed)
- De-energizing solenoid admits air to BOTTOM port (valve opens)

**Steam Line Warming:**
- 3/4-inch bypass lines around isolation valves
- Keeps lines warm for rapid AFW pump start

**Accumulator:**
- 15-gallon capacity per valve
- Supports 3 valve cycles
- Maintains capability 20 minutes after instrument air lost

**Check Valves:**
- Prevent interconnecting backflow
- Maintain physical separation of main steam lines

---

## Main Steam Isolation Valves (MSIVs)

**Design:**
- One per steam line (4 total)
- Size: 28 inches
- Type: Air-operated stop-check valve
- Location: Outside containment in main steam support structure

**Operation:**
- Normal: Held OPEN by instrument air pressure
- To close: Vent instrument air → disc weight + spring + steam pressure seat the disc
- Two series solenoid valves control air supply (normally de-energized)
- Energizing one or both solenoids vents operator → valve shuts

**Closure Time:**
- 5 seconds from receipt of isolation signal
- 0.5 second time-delay before energizing solenoids (prevents spurious closure)

**Actuation Logic - Steam Line Isolation Signal:**

**Automatic closure on EITHER:**

1. **High-high containment pressure**
   - 2 out of 4 pressure detectors

2. **High steam line flow** (1 of 2 detectors in 2 of 4 lines)  
   **COINCIDENT WITH EITHER:**
   - Low steam pressure (2 of 4 steam lines), OR
   - Low-low T_avg (2 of 4 RCS loops)

**Manual Control:**
- Control room switch de-energizes (OPEN) or energizes (CLOSE) one solenoid
- Other solenoid remains de-energized

**Accumulator:**
- Upstream of solenoid valves
- Keeps MSIV open 15 minutes after loss of instrument air

**Protection Functions:**
1. Break in bypass header downstream of MSIVs → prevents uncontrolled blowdown of all SGs
2. Break in main steam line outside containment upstream of MSIV → isolates affected SG
3. Break in main steam line inside containment → isolates affected SG, limits containment pressure
4. Steam generator tube rupture → minimizes contamination spread

---

## Main Steam Check Valves

**Design:**
- One per steam line (4 total)
- Size: 28 inches
- Type: Reverse-seating swing-check valve
- Location: Immediately downstream of MSIV

**Function:**
- Backup protection against upstream steam line break if MSIV fails to shut
- Position indicator mounted on valve shaft

---

## MSIV Bypass Valves

**Design:**
- One per steam line in parallel with MSIV
- Size: 3 inches
- Type: Air-operated double-disk gate valve

**Function:**
- Warm up piping downstream of MSIVs during startup
- Equalize pressure across MSIV disks

**Operation:**
- Energize solenoids → CLOSE
- De-energize solenoids → OPEN
- Closed by steam line isolation signal

---

## High Pressure Drains

**Configuration:**
- One drain tap per steam line outside containment
- Size: 1 inch
- Drains to B main condenser via isolation valve and steam trap

**Function:**
- Remove moisture from steam lines during startups
- Isolated during power operation

**Isolation:**
- Automatic closure on steam line isolation signal

**Nitrogen Connection:**
- Supplies nitrogen to SG gas spaces during wet layup
- Corrosion control during shutdown periods

---

## Decay Heat Removal Flowpaths

**With Offsite Power Available:**
1. Steam from SGs → Steam dump system → Main condenser
2. Feedwater provided by main feedwater system

**Without Offsite Power:**
1. Steam from SGs → PORVs → Atmosphere
2. Feedwater provided by turbine-driven AFW pump
3. AFW pump steam supply from main steam lines

**Critical Requirements:**
- SG level maintained by AFW
- Steam removal path available (PORVs or steam dumps)
- Seismic Category I portion ensures decay heat removal capability

---

## Auxiliary Steam System

**Purpose:**
- Supply steam during startups and shutdowns
- Supply steam to auxiliary and fuel building loads as needed

**Steam Sources:**
1. Startup boiler (primary source during shutdown)
2. HP turbine exhaust steam (during power operation)

**Startup Boiler:**
- Rating: 64,000 lb/hr at 150 psig
- Burner fuel consumption: 580 gph at 125 psig fuel oil pressure
- Water supply from startup boiler water storage tank
- Chemistry control system (hydrazine for corrosion inhibition)
- Nitrogen purge if not maintained in wet layup

**Major Loads:**
- Boric acid evaporators (primary load)
- Boric acid batch tank
- Decontamination system
- Cask washdown pit
- Gland steam system
- Main air ejectors
- Hogging air ejectors

**Pressure Control:**
- Normally 40-50 psig via pressure control valve
- Overpressure protection: overpressure stop valve + spring-loaded relief valve

**Condensate Return:**
- Collection in auxiliary steam condensate receiver
- Pumped to main condenser shell C
- Vent condenser for uncondensed steam
- Conductivity monitoring → high conductivity rejects to liquid waste

---

## Critical Notes for Simulator

1. **Steam Line Isolation Logic:** Complex multi-condition logic requiring accurate implementation
2. **Flow Restrictor:** Critical for limiting blowdown - maximum 9.68×10⁶ lb/hr per SG
3. **PORV Setpoint:** 1125 psig (approximately halfway between normal and safety valve setpoints)
4. **Safety Valve Stagger:** 1170-1230 psig in 5 steps for graduated relief
5. **Decay Heat Removal:** Two distinct paths (with/without offsite power) must both be modeled
6. **AFW Steam Supply:** Four redundant paths with fail-open isolation valves
7. **MSIV Closure:** 5-second closure with 0.5-second delay (prevents spurious trips)
8. **Seismic Category I Boundary:** Extends from SG outlet to downstream of check valves

---

## Implementation Priority for Heatup Simulation

**Phase 1 (Current - Cold Shutdown to HZP):**
- SG secondary pressure control (nitrogen blanket → steam formation → pressure buildup)
- PORV modeling at 1125 psig setpoint
- Steam line warming during heatup

**Phase 2 (Power Operations):**
- Safety valve modeling (staggered setpoints)
- Steam line isolation logic
- Flow restrictor ΔP for flow measurement

**Phase 3 (Shutdown/Decay Heat Removal):**
- AFW steam supplies
- PORV decay heat removal mode
- MSIV operation and bypass valves

---

## References

This document should be referenced for:
- Steam system design parameters
- Isolation valve logic and setpoints
- PORV and safety valve characteristics
- AFW steam supply design
- Decay heat removal capabilities
- Seismic qualification requirements