# NRC HRTD Section 7.2 — Condensate and Feedwater System

**Source:** https://www.nrc.gov/docs/ML1122/ML11223A246.pdf  
**Retrieved:** 2026-02-15  
**Revision:** Rev 0403

---

## Overview

This document provides comprehensive technical details on the Westinghouse PWR Condensate and Feedwater System including:

- Condensate system (hotwell pumps, demineralizers, low pressure heaters)
- Feedwater system (main feed pumps, high pressure heaters, regulating valves)
- Heater drain system (MSR drains, cascading heater drains)
- Steam generator chemistry control (blowdown, chemical injection)
- Condensate storage tank

---

## Learning Objectives

1. List in proper flowpath order and state the purpose of the condensate and feedwater system components
2. List the components and connections located in the Seismic Category I portion of the feedwater system piping
3. Explain how cascading heater drains increase plant efficiency

---

## 7.2.1 Introduction

### Purposes of the Condensate and Feedwater System

1. Transfer water from main condenser to steam generators and preheat it
2. Collect and distribute heater drains
3. Purify secondary water and maintain secondary chemistry control

### Simplified Flow Path

1. Condensed turbine exhaust steam collected in condenser hotwell
2. Condensate pumps take suction on hotwell
3. Condensate passes through demineralizers (ion exchangers)
4. Condensate passes through low pressure feedwater heaters
5. Main feedwater pumps increase pressure above SG pressure
6. Feedwater passes through high pressure heaters
7. Feedwater passes through regulating valves and isolation valves
8. Feedwater enters steam generators, converts to steam

---

## 7.2.2 System Description

### 7.2.2.1 Condensate System

**Major Components:**
- Two 70% capacity motor-driven condensate pumps
- Condensate demineralizers (8 vessels)
- Five stages of low pressure feedwater heating

**Main Condenser:**
- Condenses steam from LP turbine exhaust and/or steam dump system
- Heat sink provided by circulating water system
- Three hotwell sections (A, B, C), each divided into A and B trains
- Trains interconnected and cross-connected at B condenser hotwell

**Condensate Demineralizer System:**
- 8 demineralizer vessels (6 ft diameter × 10.8 ft height)
- 420 nylon filter elements per vessel coated with ion exchange resin
- Capacity: 4317 gpm at 134.5°F and 565 psig per vessel
- Normal operation: 6 vessels with NH₄⁺/OH⁻ resin, 1 vessel with H⁺/OH⁻ resin (pH control)
- Flow balancing controller ensures equal flow through all demineralizers
- Bypass valves open automatically if ΔP > 60 psid

**Demineralizer Operation:**
- Not normally in continuous service (morpholine exhausts resin rapidly)
- Placed in service for condenser tube leaks or contaminant introduction
- During startup: condensate recirculated through demineralizers for chemistry cleanup

**Low Pressure Heaters:**
- Five stages of heating
- First two stages: 3 parallel paths of 2 heaters each (in condenser necks)
- Stages 3-5: Single heaters per train
- All are U-tube heat exchangers (carbon steel shells, stainless steel tubes)
- Temperature increase: 120°F → 360°F

**Extraction Steam Sources:**
| Heater Stage | Steam Source |
|--------------|--------------|
| 1st stage | LP turbine 11th stage |
| 2nd stage | LP turbine 10th stage |
| 3rd stage | LP turbine 8th stage |
| 4th stage | LP turbine 7th stage |
| 5th stage | HP turbine exhaust |

**Efficiency Gain:** ~15% over non-preheating systems

### 7.2.2.2 Feedwater System

**Major Components:**
- Two 70% capacity variable-speed turbine-driven main feedwater pumps
- Two trains of high pressure feedwater heaters (2 heaters per train)

**High Pressure Heaters:**
- U-tube heat exchangers (carbon steel shells, stainless steel tubes)
- Tube-side design pressure: 1700 psig
- 6th stage: 360°F → 397°F (extraction from HP turbine 4th stage)
- 7th stage: 397°F → 440°F (extraction from HP turbine 2nd stage)

**Common Feed Header:**
- 30-inch header downstream of HP heaters
- Contains PT-508 (pressure indication and MFP speed control input)
- Recirculation line to condenser (closed during at-power operation)

**Individual Feed Lines:**
- Four 14-inch lines to individual steam generators
- Each contains:
  - 14-inch feedwater regulating valve
  - 6-inch bypass valve (parallel, manually operated)
  - Feedwater isolation valve (FWIV)
  - Flow venturi with two flow transmitters
  - Check valve (prevents SG inventory loss through upstream break)
  - Connections for shutdown chemistry and AFW entry

**Seismic Category I Portion:**
- From first piping restraints upstream of FWIVs to SG feedwater inlet

### 7.2.2.3 Heater Drain System

**Function:** Collect condensed steam from MSRs and feedwater heaters, return to condensate/feedwater system

**Cascading Arrangement:**
- 7th stage → 6th stage → 5th stage → Heater drain tanks
- 4th stage → 3rd stage → 2nd stage → 1st stage → Main condenser

**Heater Drain Pumps:**
- Two pumps transport drains from heater drain tanks to condensate cross-connect header
- At full power: heater drain pumps supply ~1/3 of total MFP suction flow
- Plant limited to 90% with only one heater drain pump

**Efficiency Benefit:**
- Cascading allows repeated use of hot water for feedwater heating
- If each heater drained to condenser, significant energy would be lost
- Nearly all SG heat transfer is latent heat of vaporization (minimal sensible heating needed)

### 7.2.2.4 Steam Generator Chemistry Control

**Three Methods:**
1. **Purification** — Condensate demineralizers (ion exchange)
2. **Chemical Injection** — Hydrazine (O₂ scavenger) and Morpholine (pH control)
3. **Steam Generator Blowdown** — Removes concentrated impurities

**Chemical Injection:**
- Hydrazine (N₂H₄): Oxygen control, decomposes to NH₃ (weak base for pH)
- Morpholine (C₄H₉NO): pH control (secured when demineralizers in service)
- Injected into low temperature portion of condensate system

**Steam Generator Blowdown System:**
- Blowdown taps from each SG
- Flow rate: 100 gpm maximum per SG (manually controlled)
- Blowdown flashes to steam in blowdown tank
- Steam routed to condenser (<50% power), FW heater 3B (>50% power), or atmosphere
- Liquid cooled, purified, filtered, discharged to condenser or environment

---

## 7.2.3 Component Descriptions

### 7.2.3.1 Condensate System Components

#### Main Condenser

| Parameter | Value |
|-----------|-------|
| Type | Single-pass, three-shell, multipressure, deaerating, surface condenser |
| Tube material | Titanium |
| Number of tubes | ~60,000 |
| Tube diameter | 1.25 in. |
| Total heat transfer area | ~900,000 ft² |

**Shell Pressures (Full Power):**
| Shell | Pressure |
|-------|----------|
| A (Low pressure) | 3.30 in. Hg |
| B (Intermediate) | 4.00 in. Hg |
| C (High pressure) | 5.11 in. Hg |
| Hotwells | 6.73 in. Hg |

**Tube Lengths (Different for Heat Transfer Area):**
| Shell | Tube Length |
|-------|-------------|
| A (LP) | 35 ft |
| B (Int) | 45 ft |
| C (HP) | 55 ft |

**Circulating Water Flow:** Series through A → B → C shells

**Air Removal:**
- Hogging air ejectors: Initial vacuum establishment during startup (1200 scfm, single-stage)
- Main air ejectors: Maintain minimum 25 in. Hg vacuum during operation (two 100% capacity, two-stage)

#### Condensate (Hotwell) Pumps

| Parameter | Value |
|-----------|-------|
| Quantity | 2 |
| Type | Eight-stage, vertical, centrifugal |
| Motor power | 3,950 hp |
| Power supply | 12.47 kVac service buses |
| Capacity | 11,000 gpm |
| Head | 1,100 ft (~477 psi) |

**Recirculation Protection:**
- Recirculation valve opens at 3,500 gpm discharge flow
- Recirculation valve closes at 7,000 gpm discharge flow
- Valves auto-open on feedwater isolation signal

### 7.2.3.2 Feedwater System Components

#### Main Feedwater Pumps (MFPs)

| Parameter | Value |
|-----------|-------|
| Quantity | 2 |
| Type | Horizontal, single-stage, centrifugal |
| Drive | Nine-stage impulse turbine |
| Capacity | 19,800 gpm (variable) |
| Discharge head | 2,020 ft |
| Load capability | 70% of main turbine load each |

**Steam Supply:**
- Startup/Low load: Main steam bypass header
- Normal operation: Reheated steam from MSR
- Dual control valve system (one from each supply)
- MSR valve opens fully before main steam valve begins to open

**MFP Individual Trips:**
| Condition | Setpoint |
|-----------|----------|
| Low lube oil pressure | 5 psig |
| Turbine overspeed | 5,850 rpm |
| Low turbine exhaust vacuum | 20 in. Hg |
| High turbine exhaust temperature | 230°F |
| Excessive thrust bearing wear | — |
| High discharge pressure | 1,850 psig |
| Suction isolation valve not fully open | — |
| Associated condensate pump trip | — |

**Both MFP Trip Signals:**
- Engineered safety features actuation signal (ESFAS)
- High SG level (69.0%) — 2/3 level detectors on any SG
- Low suction pressure (195 psig)

**Recirculation Protection:**
- Recirculation valve opens at < 4,000 gpm suction flow
- Recirculation valve closes at > 9,000 gpm suction flow
- Valves auto-open on feedwater isolation signal

#### Feedwater Isolation Valves (FWIVs)

| Parameter | Value |
|-----------|-------|
| Location | Downstream of each FW regulating valve and bypass valve |
| Operator | Hydraulic (nitrogen-charged accumulator) |
| Closing time | Maximum 16 seconds |
| Accumulator capacity | One close + one open cycle |

**Feedwater Isolation Signal (Auto-Close):**
- High SG level (69.0%) — 2/3 level detectors on any SG
- Engineered safety features actuation signal (ESFAS)
- Reactor trip (P-4) AND low T_avg (564°F) in 2/4 RCS loops

### 7.2.3.3 Startup Auxiliary Feedwater Pump

| Parameter | Value |
|-----------|-------|
| Purpose | Provide feed flow during startup/shutdown (avoid AFW pump wear) |
| Type | Motor-driven, eight-stage centrifugal |
| Motor power | 1,250 hp |
| Capacity | 1,020 gpm (including 140 gpm recirculation) |
| Discharge head | 3,400 ft (1,472 psi) |
| Automatic start | None |
| Power supply | Plant service bus (can align to DG-A) |
| Suction | Condensate storage tank |
| Discharge | To diesel-driven or turbine-driven AFW pump discharge line |

**Flow Control:**
- Manual or automatic
- Auto mode: Maintains 100 psid between pump discharge and SG-C

### 7.2.3.4 Heater Drain System Components

#### MSR Drain Tanks (6 total, 3 per MSR)

| Tank | Receives From | Normally Drains To | Alternate Drain |
|------|---------------|-------------------|-----------------|
| 2nd-stage reheater drain tank | MSR 2nd-stage tubes | 7th-stage FW heater | Heater drain tank |
| 1st-stage reheater drain tank | MSR 1st-stage tubes | 6th-stage FW heater | Heater drain tank |
| Moisture separator drain tank | MSR shell | Heater drain tank | Condenser A |

- Moisture separator drain flow: ~1.5 × 10⁶ lbm/hr at 100% load

#### Heater Drain Tanks and Pumps

**Heater Drain Tanks:**
- Horizontal, 13,120 gallon capacity
- Design: 220 psig, 400°F
- Cross-connected by 20-inch line
- Dump valve to Condenser A on high level

**Heater Drain Pumps:**
| Parameter | Value |
|-----------|-------|
| Quantity | 2 |
| Type | Canned-suction, vertical, centrifugal |
| Capacity | 5,950 gpm |
| Discharge pressure | 575 psig |
| Power supply | Station service buses |

- Recirculation opens when combined flow < 2,400 gpm

### 7.2.3.5 Condensate Storage Tank (CST)

| Parameter | Value |
|-----------|-------|
| Type | Covered, outdoor storage tank |
| Total capacity | 450,000 gallons |
| Tech Spec minimum | 239,000 gallons |
| Unusable volume | 27,700 gallons |
| Instrument error allowance | 14,400 gallons |

**Minimum Volume Basis:**
- Hot standby for 2 hours
- Cooldown to 350°F in 4 hours

**Hotwell Level Control:**
| Level | Action |
|-------|--------|
| Normal setpoint | 24 in. |
| 28 in. | Condensate reject valve begins to open |
| 40 in. | Reject valve fully open |
| 21 in. | Makeup valve begins to open |
| 8 in. | Makeup valve fully open |

---

## 7.2.4 System Operation

### Startup Sequence

1. **Early Startup:** Startup AFW pump provides feed flow to SGs
2. **System Preparation:**
   - Fill and vent system (water from CST)
   - Place MFP lube oil systems in service
   - Start one condensate pump, open discharge valve
   - Open MFP recirculation valves (recirculate through idle MFPs)
   - Start gland steam and seal water to MFPs

3. **Secondary Cleanup:**
   - Open startup recirculation valve
   - Direct flow through condensate demineralizers
   - Continue until all chemistry parameters satisfied
   - FWIVs remain closed during cleanup

4. **MFP Startup (when reactor adding heat):**
   - Start one MFP
   - Reset feedwater isolation signal
   - Close startup recirculation valve
   - Manually roll MFP with governor control
   - Place in automatic (slaved to master speed controller)
   - Maintain ΔP between feedwater and steam pressures

5. **Transfer to Normal Operation:**
   - Open bypass valve FWIVs
   - Throttle open bypass valves while closing AFW flow control valves
   - Manually control SG levels via bypass valves and MFP master speed

6. **Power Ascension (~20% power):**
   - Place MFP master speed controller in automatic
   - Shift SG level control from bypass valves to feedwater regulating valves
   - Place individual SG level controllers in automatic

7. **Additional Equipment:**
   - Second condensate pump and MFP: Before power exceeds 70%
   - First heater drain pump: ~30% power
   - Second heater drain pump: Before power exceeds 90%

---

## 7.2.5 PRA Insights

- Loss of feedwater sequences: Very small fraction of total core damage frequency
  - Sequoyah: 0.6%
  - Zion, Surry, Trojan: Insignificant percentage
- Modifications to power conversion system: Minimal contribution to risk reduction/achievement

---

## 7.2.6 Summary

### Condensate System
- Multipressure, three-shell condenser receives LP turbine exhaust and steam dumps
- Condensate pumps transfer through demineralizers and LP heaters to MFP suction

### Feedwater System
- MFPs take suction from condensate pumps and heater drain pumps
- Transfer through HP heaters, regulating valves, FWIVs to steam generators
- Automatically controlled flow maintains desired SG water levels

### Heater Drain System
- Improves efficiency by collecting high-temperature drainage from MSRs and FW heaters
- Returns drains to condensate and feedwater system

### Chemistry Control
- Purification via demineralizers
- Chemical injection (hydrazine for O₂, morpholine for pH)
- Steam generator blowdown

---

## Critical Data for Simulator Development

### Temperature Profile Through System
| Location | Temperature |
|----------|-------------|
| Condenser hotwell | ~120°F |
| After LP heaters (5 stages) | 360°F |
| After 6th-stage HP heater | 397°F |
| After 7th-stage HP heater | 440°F |

### Key Setpoints
| Parameter | Setpoint |
|-----------|----------|
| High SG level trip/isolation | 69.0% |
| FW isolation on low T_avg (with P-4) | 564°F |
| MFP low suction pressure trip | 195 psig |
| MFP high discharge pressure trip | 1,850 psig |
| MFP overspeed trip | 5,850 rpm |
| Condenser minimum vacuum | 25 in. Hg |
| Demineralizer bypass on high ΔP | 60 psid |

### Flow Capacities
| Component | Capacity |
|-----------|----------|
| Condensate pump | 11,000 gpm (70% each) |
| Main feedwater pump | 19,800 gpm (70% each) |
| Heater drain pump | 5,950 gpm |
| Startup AFW pump | 1,020 gpm |
| SG blowdown | 100 gpm max per SG |

### Power Level Constraints
| Condition | Power Limit |
|-----------|-------------|
| One LP heater string isolated | ≤70% |
| One HP heater string isolated | ≤95% |
| One heater drain pump | ≤90% |
| One condensate pump | ≤70% |
| One MFP | ≤70% |

---

## References

- NRC HRTD Section 7.1 — Main and Auxiliary Steam
- NRC HRTD Section 11.1 — Steam Generator Water Level Control
- NRC HRTD Section 11.2 — Steam Dump Control
- NRC HRTD Section 5.7 — Auxiliary Feedwater System
- NRC HRTD Section 14.3 — Circulating Water System

---
