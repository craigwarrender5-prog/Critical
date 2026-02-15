# NRC HRTD Section 5.1 — Residual Heat Removal System

**Source:** https://www.nrc.gov/docs/ML1122/ML11223A219.pdf  
**Retrieved:** 2026-02-15  
**Revision:** Rev 0201

---

## Overview

This document provides comprehensive technical details on the Westinghouse PWR Residual Heat Removal (RHR) System including:

- System purposes and functions
- Component descriptions (pumps, heat exchangers, valves)
- Plant cooldown operations
- Solid plant operations
- Refueling operations
- Emergency core cooling function
- PRA insights and intersystem LOCA

---

## Learning Objectives

1. State the purposes of the Residual Heat Removal (RHR) System
2. Describe the RHR system flow path including suction supplies, discharge points and major components during decay heat removal
3. Describe the normal, at-power lineup of the RHR system
4. Explain why RCS pressure and temperature limits are placed on the initiation of RHR cooldown
5. Explain how the RHR system is protected against overpressurization
6. Explain how an intersystem LOCA is initiated in the residual heat removal system and what effect it can have on long-term core cooling

---

## 5.1.1 Introduction

### Purposes of the RHR System

1. **Decay Heat Removal** — Removes decay heat from the core and reduces RCS temperature during the second phase of plant cooldown
2. **Low Pressure ECCS Injection** — Serves as the low pressure injection portion of the Emergency Core Cooling System (ECCS) following a LOCA
3. **Refueling Water Transfer** — Transfers refueling water between the RWST and refueling cavity before and after refueling

### Cooldown Phases

| Phase | System | Temperature Range |
|-------|--------|-------------------|
| First Phase | AFW + Steam Dumps + Steam Generators | Operating → 350°F |
| Second Phase | RHR System | 350°F → < 200°F (Cold Shutdown) |

### Key Functions
- Transfers heat from RCS to Component Cooling Water (CCW) system
- Maintains RCS temperature during cold shutdown until plant restart
- Part of ECCS during injection and recirculation phases of LOCA

---

## 5.1.2 System Description

### Major Components
- 2 RHR heat exchangers
- 2 RHR pumps
- Associated piping, valves, and instrumentation

### Flow Path During Cooldown
1. **Suction:** Hot leg of Loop 4 → through series isolation valves (8701 & 8702)
2. **Pumps:** RHR pumps (inside containment)
3. **Heat Exchangers:** Tube side of RHR heat exchangers
4. **Discharge:** Return to each RCS cold leg (also serves as ECCS low pressure injection lines)

### Isolation Configuration

**Suction Line:**
- 2 series motor-operated valves (8701 & 8702)
- Relief valve downstream of isolation valves
- All located inside containment

**Discharge Line:**
- 2 check valves (inside containment)
- 2 normally open motor-operated valves (8809A & 8809B) outside containment
- MOVs receive confirmatory open signal from ESFAS

### Single Train Operation
- If one pump or one heat exchanger is not operable, safe cooldown is NOT compromised
- However, time required for cooldown is extended

### Power Supplies
- Two RHR pumps powered from **separate vital electrical buses**
- Each vital bus automatically transfers to separate emergency diesel on loss of offsite power
- Prolonged loss of offsite power does NOT adversely affect RHR operation

### At-Power Lineup
- RHR system normally aligned to perform its safety function
- **No valves required to change position** for safety function
- RHR pumps start on ESFAS
- System functions when RCS pressure drops below RHR pump discharge pressure

### Materials
- All parts in contact with borated water: austenitic stainless steel or equivalent corrosion resistant material
- Fabrication per applicable ASME code requirements

---

## 5.1.3 Component Descriptions

### 5.1.3.1 RHR Pumps

| Parameter | Value/Description |
|-----------|-------------------|
| Quantity | 2 |
| Type | Vertical, centrifugal |
| Seals | Mechanical shaft seals |
| Seal cooling | CCW or service water (plant-specific) |
| Materials | Austenitic stainless steel (wetted surfaces) |
| Sizing | Designed for plant cooldown requirements |

**Minimum Flow Protection:**
- Bypass lines protect against overheating and loss of suction flow
- Control valves (MO-610, 611) in each minimum flow line
- Regulated by flow transmitter signal from pump discharge header
- **Open:** When discharge flow < 500 gpm AND pump running
- **Close:** When flow > 1000 gpm OR pump not running

**Instrumentation:**
- Pressure sensor in each pump header
- Main control board indicator
- High pressure annunciator alarm

### 5.1.3.2 RHR Heat Exchangers

| Parameter | Value/Description |
|-----------|-------------------|
| Quantity | 2 |
| Type | Shell and U-tube |
| Tube side | Reactor coolant |
| Shell side | Component cooling water |
| Tubes | Welded to tube sheet (prevents leakage) |

**Design Basis:**
- Based on heat load and temperature differences at **20 hours after shutdown**
- This is the point of **minimum ΔT** between RCS and CCW
- Therefore, minimum heat transfer capability (conservative design)

### 5.1.3.3 RHR System Valves

**Modulating Valves:**
- Two stem packing glands
- Intermediate leak-off connection to drain header

**Manual and Motor-Operated Valves:**
- Backseats for repacking and stem leakage limitation when open
- Leakage connections where required by size and fluid conditions

**Suction Line Relief Valve:**
- Sized to relieve combined flow of all charging pumps at relief valve setpoint
- Provides overpressure protection for RCS during solid plant operations

**Discharge Line Relief Valves:**
- One per line
- Sized to relieve maximum possible backleakage through check valves
- Protects RHR from RCS overpressurization

**Suction Isolation Valve Interlocks (8701 & 8702):**

| Function | Setpoint | Action |
|----------|----------|--------|
| Open Permissive | < 425 psig | Valves can be manually opened |
| Auto-Close | > 585 psig | Valves automatically close |

- Each valve interlocked with one of two independent RCS pressure transmitters
- Ensures RHR system is not overpressurized

---

## 5.1.4 System Features and Interrelationships

### 5.1.4.1 Plant Cooldown

**Initiation Conditions:**
- Approximately 4 hours after reactor shutdown
- RCS temperature: ~350°F
- RCS pressure: ~425 psig

**Design Performance (Both Trains Operating):**
- Cool RCS from 350°F to 140°F within 16 hours
- CCW at design flow rate and temperature

**Heat Load Sources:**
1. Residual and decay heat from core
2. Reactor coolant pump heat

**Design Heat Load Basis:**
- Decay heat fraction at 20 hours following shutdown from extended full power operation

**Startup Procedure:**
1. **Warmup Period:** Limited coolant flow through heat exchangers to minimize thermal shock
2. **Manual Control:** Heat removal rate controlled by regulating coolant flow through RHR heat exchangers
3. **CCW:** Supplied at constant flow rate
4. **Temperature Control:** Manual adjustment of HX outlet control valves (606, 607)
5. **Flow Control:** HX bypass valve (HCV-618) adjusted to maintain constant RHR train flow

**Rate Limitations:**
- Equipment cooldown rates based on allowable stress limits
- CCW system operating temperature limits affect available cooldown rate

**Cooldown Progression:**
- As RCS temperature decreases, RHR flow through heat exchangers is gradually increased
- Adjusted by control valve in each HX outlet line

**Letdown Path During RHR Cooldown:**
- Portion of RHR flow may be diverted to CVCS low pressure letdown line
- Via downstream of RHR heat exchangers
- For cleanup and/or pressure control

### 5.1.4.2 Solid Plant Operations

**Definition:** No steam bubble in pressurizer — RCS completely filled with liquid coolant

**Application:** Generally limited to system refill and venting operations during cold shutdown (< 200°F)

**RHR Configuration:**
- Circulates reactor coolant from Loop 4 hot leg to each loop cold leg
- RHR system operates as extension of RCS (completely filled with reactor coolant)

**Pressure Control Methods:**

| Method | Effectiveness |
|--------|---------------|
| Temperature change | NOT effective — slow response, large pressure changes for small ΔT |
| Mass/Volume change | PREFERRED — fast response, controllable pressure changes |

**Flow Path for Pressure Control:**
1. Portion of RHR flow diverted to CVCS through **HCV-128**
2. Flow controlled by **PCV-131** (backpressure regulating valve, downstream of letdown HX)
3. Charging rate controlled by **HCV-182** (charging flow control valve, manual positioning)
4. VCT acts as buffer/surge volume for pressure control

**Pressure Control Logic:**
- Maintain constant charging rate
- Vary letdown flow rate (PCV-131) to CVCS
- If charging > letdown → RCS pressure increases
- If letdown > charging → RCS pressure decreases
- For constant pressure: charging rate = letdown rate

**PCV-131 Operation:**
- Normally in automatic mode
- Controls RCS pressure at desired setpoint
- VCT absorbs flow rate mismatches

**Why Pressure Control is Required:**
- Maintain RCS pressure within range dictated by **fracture prevention criteria** (P-T limits) of reactor vessel

### 5.1.4.3 Refueling

**Cavity Fill:**
1. Close RCS inlet isolation valves (8701, 8702)
2. Open RWST isolation valve (8812)
3. Both RHR pumps pump borated water from RWST
4. Water enters reactor vessel through normal RHR return lines
5. Water fills refueling cavity through open reactor vessel
6. Reactor vessel head gradually raised as water level increases
7. After reaching normal refueling level: open RCS inlet isolation valves, close RWST supply valve

**During Refueling:**
- RHR system maintained in service
- Number of pumps and heat exchangers based on heat load and Technical Specification minimum flow requirements

**Cavity Drain:**
1. RHR returns water from refueling cavity to RWST via manual isolation valve
2. Water level brought down to reactor vessel flange
3. Remainder removed through drains in bottom of refueling canal

### 5.1.4.4 Emergency Core Cooling

**Injection Phase:**
- RHR functions with higher pressure ECCS portions
- Injects borated water from RWST into RCS cold legs
- RHR aligned per normal at-power lineup

**Recirculation Phase:**
- Provides long-term recirculation capability for core cooling
- Alignment:
  - Open valves 8811A & 8811B (containment sump suction)
  - Close valves 8700A & 8700B and 8812 (RWST isolation)
- Water from containment sump → RHR heat exchangers → RCS cold legs

**High Pressure Recirculation:**
- If RCS pressure > RHR pump discharge pressure:
- RHR pump discharge serves as suction source for:
  - Centrifugal charging pumps (via 8804A)
  - Safety injection pumps (via 8804B)

**Fission Product Containment:**
- Parts of RHR exterior to containment may circulate fission products post-LOCA
- If RHR pump seal fails: water spills in shielded compartment
- Each pump in separate shielded room
- No interconnections between trains during recirculation
- If one room floods, no effect on other train

**Spillage Handling:**
- Drains to sump with dual pumps
- Level instrumentation provided
- Spillage pumped to waste disposal system

---

## 5.1.5 PRA Insights

### Intersystem LOCA (Event V)

**Definition:** LOCA where coolant is lost outside containment; water not available for containment sump recirculation

**Contribution to Core Damage Frequency:**
| Plant | Contribution |
|-------|-------------|
| Surry | 4% |
| Zion | 0.1% |
| Sequoyah | 0.4% |

**Initiating Failure:** Check valve failure in low pressure injection system (RHR)

**Probable Causes:**
1. Transfer open of one check valve followed by rupture of second interface valve
2. Failure of one valve to close on re-pressurization followed by rupture of second valve
3. Rupture of interface valve
4. Operator failure to isolate interfacing valve

**NUREG-1150 Importance Measures:**
- Event V is contributor to **risk achievement**
- Very minor contributor to **risk reduction**
- Large increase in check valve rupture probability would significantly increase CDF:
  - Factor of 30 at Sequoyah
  - Factor of 270 at Surry
- Reducing Event V initiator probability has minimal effect on risk reduction

---

## 5.1.6 Summary

### Normal Plant Functions
1. Transfer heat from RCS to CCW during shutdown operations (second phase cooldown starting at 350°F)
2. Remove decay heat until plant restart
3. Provide solid plant pressure control in conjunction with CVCS

### Accident Functions
1. **Injection Phase:** Supply water from RWST to RCS cold legs
2. **Recirculation Phase:** Containment sump as water source, RHR heat exchangers cool water, return to RCS

### Refueling Functions
1. Remove decay heat during refueling
2. Transfer water between RWST and refueling cavity

---

## Critical Data for Simulator Development

### Operating Limits and Interlocks

| Parameter | Value | Function |
|-----------|-------|----------|
| RHR suction valve open permissive | < 425 psig | Prevents overpressurization |
| RHR suction valve auto-close | > 585 psig | Protects RHR piping |
| RHR entry temperature | ≤ 350°F | Mode 4 boundary |
| Cold shutdown temperature | < 200°F | Mode 5 definition |
| Min-flow bypass opens | < 500 gpm | Pump protection |
| Min-flow bypass closes | > 1000 gpm | Normal operation |

### Design Performance

| Parameter | Value |
|-----------|-------|
| Cooldown time (350°F → 140°F) | 16 hours |
| Trains required | Both (2 pumps, 2 HX) |
| Design basis heat load timing | 20 hours after shutdown |
| RHR placed in service | ~4 hours after shutdown |

### Key Valves

| Valve | Function |
|-------|----------|
| 8701, 8702 | RHR suction isolation (series, from Loop 4 hot leg) |
| 8809A, 8809B | RHR discharge isolation (normally open, ESFAS confirmatory) |
| 606, 607 | RHR HX outlet flow control |
| HCV-618 | RHR HX bypass valve |
| HCV-128 | RHR to CVCS cross-connect (letdown during solid plant) |
| PCV-131 | Letdown backpressure regulating valve |
| HCV-182 | Charging flow control valve |
| 8812 | RWST supply to RHR |
| 8811A, 8811B | Containment sump to RHR |
| 8700A, 8700B | RWST/sump alignment |
| MO-610, 611 | RHR pump minimum flow bypass |

### Flow Paths

**Cooldown Mode:**
Loop 4 Hot Leg → 8701 → 8702 → RHR Pumps → RHR HX (tube side) → Each Cold Leg

**Solid Plant Letdown:**
RHR discharge → HCV-128 → CVCS letdown HX → PCV-131 → VCT

**ECCS Injection:**
RWST → 8812 → RHR Pumps → RHR HX → 8809A/B → Check Valves → Cold Legs

**ECCS Recirculation:**
Containment Sump → 8811A/B → RHR Pumps → RHR HX → Cold Legs (or via charging/SI pumps if high pressure)

---

## References

- NRC HRTD Section 5.2 — Emergency Core Cooling Systems
- NRC HRTD Section 5.7 — Auxiliary Feedwater System
- NRC HRTD Section 11.2 — Steam Dump Control System
- NRC HRTD Section 4.1 — Chemical and Volume Control System
- NUREG-1150 — Severe Accident Risks Assessment

---
