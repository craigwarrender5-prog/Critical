# NRC HRTD Section 7.2 — Condensate and Feedwater System (Full Source)

**Source:** https://www.nrc.gov/docs/ml1122/ML11223A246.pdf  
**Document:** Westinghouse Technology Systems Manual Section 7.2  
**Retrieved:** 2026-02-17  
**Revision:** Rev 0403

---

## Document Purpose

This document provides the complete NRC HRTD Section 7.2 technical content for the Westinghouse 4-Loop PWR Condensate and Feedwater System. This serves as an authoritative source for simulator development.

---

## Table of Contents

1. Introduction
2. System Description
   - 2.1 Condensate System
   - 2.2 Feedwater System
   - 2.3 Heater Drain System
   - 2.4 Steam Generator Chemistry Control
3. Component Descriptions
   - 3.1 Condensate System Components
   - 3.2 Feedwater System Components
   - 3.3 Startup Auxiliary Feedwater Pump
   - 3.4 Heater Drain System Components
   - 3.5 Condensate Storage Tank
4. System Operation
5. PRA Insights
6. Summary
7. Data Tables and Specifications

---

## 1. Introduction

### System Purposes

1. **Transfer water** from the main condenser to the steam generators and preheat it
2. **Collect and distribute** heater drains
3. **Purify secondary water** and maintain secondary chemistry control

### Simplified Flow Path

```
Condenser Hotwell → Condensate Pumps → Demineralizers → LP Heaters → 
MFP Suction → Main Feedwater Pumps → HP Heaters → 
FW Regulating Valves → FWIVs → Steam Generators
```

---

## 2. System Description

### 2.1 Condensate System

The condensate system preheats, pressurizes, and purifies the condensate collected in the main condenser and transports it to the suctions of the main feedwater pumps.

**Major Components:**
- Two 70% capacity motor-driven condensate pumps
- Condensate demineralizers (8 vessels)
- Five stages of low pressure feedwater heating

#### Main Condenser

The main condenser condenses and collects steam from:
- Low pressure turbine exhaust
- Steam dump system (Chapter 11.2)

Heat sink provided by circulating water system (Chapter 14.3):
- Circulating water flows through inside of condenser tubes
- Exhaust steam condensed on outside of tubes
- Condensate collected in three hotwell sections (A, B, C)

#### Hotwell Configuration

Each of the three condenser hotwells is divided into A train and B train sections:
- A and B train sections are interconnected
- Trains are cross-connected at B condenser hotwell
- Each condensate pump takes suction on its associated train

#### Condensate Demineralizer System

| Parameter | Value |
|-----------|-------|
| Number of vessels | 8 |
| Vessel diameter | 6 ft |
| Vessel height | 10.8 ft |
| Filter elements per vessel | 420 nylon |
| Capacity per vessel | 4,317 gpm at 134.5°F and 565 psig |

**Normal Operation:**
- 6 vessels with NH₄⁺/OH⁻ resin (ammonia cation/hydroxide anion)
- 1 vessel with H⁺/OH⁻ resin (hydrogen cation/hydroxide anion) - throttled for pH 9.3-9.6
- 1 vessel in standby or undergoing precoat replacement

**Bypass Protection:**
- Demineralizer bypass valves open automatically if ΔP > 60 psid

**Operating Notes:**
- Not normally in continuous service (morpholine exhausts resin rapidly)
- Placed in service for condenser tube leaks or contaminant introduction
- During startup: condensate recirculated through demineralizers for chemistry cleanup

#### Low Pressure Feedwater Heaters

| Parameter | Value |
|-----------|-------|
| Number of stages | 5 |
| Stages 1-2 configuration | 3 parallel paths × 2 heaters each (in condenser necks) |
| Stages 3-5 configuration | Single heaters per train |
| Construction | U-tube heat exchangers (carbon steel shells, stainless steel tubes) |
| Temperature increase | 120°F → 360°F |
| Efficiency gain | ~15% over non-preheating systems |

**Extraction Steam Sources:**

| Stage | LP Turbine Stage |
|-------|-----------------|
| 1st | 11th stage |
| 2nd | 10th stage |
| 3rd | 8th stage |
| 4th | 7th stage |
| 5th | HP turbine exhaust |

**Operating Constraint:** If a heater string is taken out of service, power must be reduced to ≤70%.

### 2.2 Feedwater System

The feedwater system preheats and pressurizes the discharge from the condensate system and heater drain pumps, transporting it to the steam generators.

**System extends from:** MFP suction valves **to:** Steam generator inlets

**Major Components:**
- Two 70% capacity variable-speed turbine-driven pumps
- Two trains of high pressure feedwater heaters (2 heaters per train)

#### High Pressure Feedwater Heaters

| Parameter | Value |
|-----------|-------|
| Configuration | 2 trains × 2 heaters |
| Construction | U-tube (carbon steel shells, stainless steel tubes) |
| Tube-side design pressure | 1,700 psig |
| 6th stage temperature rise | 360°F → 397°F |
| 7th stage temperature rise | 397°F → 440°F |
| 6th stage steam source | HP turbine 4th stage extraction |
| 7th stage steam source | HP turbine 2nd stage extraction |

**Operating Constraint:** One HP heater string can be isolated if power is first reduced to ≤95%.

#### Common Feed Header

- 30-inch header downstream of HP heaters
- Contains PT-508 (pressure indication and MFP speed control input)
- Recirculation line to main condenser (closed during at-power operation)

#### Individual Feed Lines (4 total, one per SG)

Each 14-inch feed line contains:
1. 14-inch feedwater regulating valve
2. 6-inch bypass valve (parallel, manually operated)
3. Feedwater isolation valve (FWIV)
4. Flow venturi with two flow transmitters
5. Check valve (prevents SG inventory loss through upstream break)
6. Connections for shutdown chemistry and AFW entry

#### Seismic Category I Portion

From first piping restraints upstream of FWIVs to SG feedwater inlet.

### 2.3 Heater Drain System

The heater drain system collects condensed steam from MSRs and feedwater heaters and returns it to the condensate/feedwater system.

**Cascading Arrangement:**
```
7th stage → 6th stage → 5th stage → Heater drain tanks → Heater drain pumps → 
                                                         ↓
                                    Condensate cross-connect header (MFP suction)

4th stage → 3rd stage → 2nd stage → 1st stage → Main condenser
```

**Heater Drain Pumps:**
- Two pumps transport drains from heater drain tanks
- At full power: supply ~1/3 of total MFP suction flow
- Plant limited to 90% power with only one heater drain pump

**Efficiency Benefit:**
- Cascading allows repeated use of hot water for feedwater heating
- If each heater drained to condenser, significant energy would be lost
- Nearly all SG heat transfer is latent heat of vaporization (minimal sensible heating needed)

### 2.4 Steam Generator Chemistry Control

**Three Methods:**

1. **Purification** — Condensate demineralizers (ion exchange)
   - Ionic impurities removed on as-needed basis

2. **Chemical Injection** — Into low temperature portion of condensate system
   - Hydrazine (N₂H₄): Oxygen scavenger, decomposes to NH₃ (weak base for pH)
   - Morpholine (C₄H₉NO): pH control (secured when demineralizers in service)
   - Volatility prevents undesirable chemical concentrations in SGs

3. **Steam Generator Blowdown** — Removes concentrated impurities
   - Each SG acts as chemical concentrator
   - Blowdown taps from each SG
   - Flow rate: 100 gpm maximum per SG (manually controlled)
   - Blowdown flashes to steam in blowdown tank
   - Steam routed to: condenser (<50% power), FW heater 3B (>50% power), or atmosphere

---

## 3. Component Descriptions

### 3.1 Condensate System Components

#### 3.1.1 Main Condenser

| Parameter | Value |
|-----------|-------|
| Type | Single-pass, three-shell, multipressure, deaerating, surface condenser |
| Tube material | Titanium |
| Number of tubes | ~60,000 |
| Tube diameter | 1.25 in. |
| Total heat transfer area | ~900,000 ft² |

**Shell Pressures (Full Power):**

| Shell | Pressure (in. HgA) | Purpose |
|-------|-------------------|---------|
| A (Low pressure) | 3.30 | First CW contact, coldest |
| B (Intermediate) | 4.00 | Middle pressure |
| C (High pressure) | 5.11 | Last CW contact, warmest |
| Hotwells | 6.73 | Collection area |

**Tube Lengths (for equal heat transfer capability):**

| Shell | Tube Length | Heat Transfer Reason |
|-------|-------------|---------------------|
| A (LP) | 35 ft | Coldest CW = best ΔT |
| B (Int) | 45 ft | Warmer CW = moderate ΔT |
| C (HP) | 55 ft | Warmest CW = lowest ΔT |

**Circulating Water Flow:**
- Series arrangement: A → B → C shells
- CW heated as it flows through shells
- Different tube lengths provide approximately equal heat transfer per shell

**Air Removal System:**

| Equipment | Purpose | Capacity |
|-----------|---------|----------|
| Hogging air ejectors (3) | Initial vacuum establishment during startup | 1,200 scfm each, single-stage |
| Main air ejectors (2) | Maintain vacuum during operation | 100% capacity each, two-stage |

**Minimum Condenser Vacuum:** 25 in. Hg (maintained by main air ejectors)

**Noncondensible Gas Handling:**
- Gas collecting space at center of each tube bundle
- Gases directed across cold central tubes
- Routed through air off-take piping to air ejector suctions
- Must be removed to prevent reduction in vacuum and turbine efficiency

**Hotwell Heating System:**
- MFP turbine exhaust piped to condenser C lower hotwell first
- Crossover piping to other condenser lower hotwells
- Heating steam from shell C to intermediate hotwells via crossover
- Perforated "heatup device" plates break condensate into droplets

**Hotwell Level Control:**

| Level | Action |
|-------|--------|
| 24 in. | Normal setpoint |
| 28 in. | Condensate reject valve begins to open |
| 40 in. | Reject valve fully open |
| 21 in. | Makeup valve begins to open |
| 8 in. | Makeup valve fully open |

#### 3.1.2 Condensate (Hotwell) Pumps

| Parameter | Value |
|-----------|-------|
| Quantity | 2 |
| Type | Eight-stage, vertical, centrifugal |
| Motor power | 3,950 hp |
| Power supply | 12.47 kVac service buses |
| Capacity | 11,000 gpm each |
| Head | 1,100 ft (~477 psi) |
| Suction | Condenser B hotwell through strainers |

**Recirculation Protection:**

| Condition | Valve Action |
|-----------|-------------|
| Discharge flow < 3,500 gpm | Recirculation valve opens |
| Discharge flow > 7,000 gpm | Recirculation valve closes |
| Feedwater isolation signal | Recirculation valves auto-open |

**Cooling:**
- Thrust and upper guide bearings: Oil lubricated (motor oil coolers by bearing cooling water)
- Lower radial bearing: Grease lubricated, air cooled
- Shaft seals: Cooling water from condensate transfer pumps

### 3.2 Feedwater System Components

#### 3.2.1 Main Feedwater Pumps (MFPs)

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

| Condition | Valve Action |
|-----------|-------------|
| Suction flow < 4,000 gpm | Recirculation valve opens |
| Suction flow > 9,000 gpm | Recirculation valve closes |
| Feedwater isolation signal | Recirculation valves auto-open |

#### 3.2.2 Feedwater Isolation Valves (FWIVs)

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

### 3.3 Startup Auxiliary Feedwater Pump

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
- Manual or automatic mode
- Auto mode: Maintains 100 psid between pump discharge and SG-C

### 3.4 Heater Drain System Components

#### 3.4.1 MSR Drain Tanks (6 total, 3 per MSR)

| Tank | Receives From | Normally Drains To | Alternate Drain |
|------|---------------|-------------------|-----------------|
| 2nd-stage reheater drain tank | MSR 2nd-stage tubes | 7th-stage FW heater | Heater drain tank |
| 1st-stage reheater drain tank | MSR 1st-stage tubes | 6th-stage FW heater | Heater drain tank |
| Moisture separator drain tank | MSR shell | Heater drain tank | Condenser A |

- Moisture separator drain flow: ~1.5 × 10⁶ lbm/hr at 100% load
- Each drain tank has level control system to maintain water seal
- All tanks vented to drain source to remove noncondensibles

#### 3.4.2 High Pressure Heater Drains

**7th-Stage Heaters:**
- Receive: HP turbine 2nd stage extraction + 2nd-stage reheater drains
- Normally drain to: 6th-stage heaters
- Level controlled: Yes
- High level: Alternate drain to heater drain tank
- Very high level: Drain and extraction steam inlets isolate

**6th-Stage Heaters:**
- Receive: HP turbine 4th stage extraction + 1st-stage reheater drains + 7th-stage drains
- Normally drain to: 5th-stage heaters
- Level controlled: Yes
- High level: Alternate drain to heater drain tank
- Very high level: Drain and extraction steam inlets isolate

**5th-Stage Heaters:**
- Receive: HP turbine exhaust + 6th-stage drains
- No drain coolers
- Drain directly to heater drain tanks (no level control)
- Very high level: Drain and extraction steam inlets isolate

#### 3.4.3 Heater Drain Tanks and Pumps

**Heater Drain Tanks:**

| Parameter | Value |
|-----------|-------|
| Type | Horizontal |
| Capacity | 13,120 gallons each |
| Design pressure | 220 psig |
| Design temperature | 400°F |

- Cross-connected by 20-inch line
- Dump valve to Condenser A on high level in either tank

**Heater Drain Pumps:**

| Parameter | Value |
|-----------|-------|
| Quantity | 2 |
| Type | Canned-suction, vertical, centrifugal |
| Capacity | 5,950 gpm each |
| Discharge pressure | 575 psig |
| Power supply | Station service buses |
| Seal cooling | From condensate pumps |

- Level control maintains tank levels via discharge valve control
- Recirculation opens when combined flow < 2,400 gpm
- Discharge to condensate cross-connect header upstream of MFP suctions

#### 3.4.4 Low Pressure Heater Drains

**4th-Stage → 3rd-Stage → 2nd-Stage → 1st-Stage → Condenser**

Each stage:
- Level controlled drain flow
- High level: Alternate drain valve opens
- Very high level: Extraction steam and/or drain inlets isolate

**Special Consideration for 1st/2nd-Stage Heaters:**
- Located in condenser necks (inside condenser shells)
- No extraction steam isolation valves (check valves only)
- Large tube leak could cause water backup into LP turbine
- If drain rate exceeds capacity: Must isolate entire condensate train

### 3.5 Condensate Storage Tank (CST)

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

**Functions:**
- Source of makeup water for condensate/feedwater system
- Primary source of water for AFW system

**Level Instrumentation:**
- Provides indication
- High and low level alarms
- Low level AFW pump trips

---

## 4. System Operation

### 4.1 Startup Sequence

**Early Startup:**
- Startup AFW pump provides feed flow to SGs
- Decay heat and RCP heat transferred to feedwater

**System Preparation:**
1. Fill and vent system (water from CST)
2. Place MFP lube oil systems in service
3. Start one condensate pump, open discharge valve
4. Open MFP recirculation valves (recirculate through idle MFPs)
5. Start gland steam and seal water to MFPs

**Secondary Cleanup:**
1. Open startup recirculation valve
2. Direct flow through condensate demineralizers
3. Continue until all chemistry parameters satisfied
4. FWIVs remain closed during cleanup

**MFP Startup (when reactor adding heat):**
1. Start one MFP
2. Reset feedwater isolation signal
3. Close startup recirculation valve
4. Manually roll MFP with governor control
5. Place in automatic (slaved to master speed controller)
6. Maintain ΔP between feedwater and steam pressures

**Transfer to Normal Operation:**
1. Open bypass valve FWIVs
2. Throttle open bypass valves while closing AFW flow control valves
3. Manually control SG levels via bypass valves and MFP master speed

**Power Ascension (~20% power):**
1. Place MFP master speed controller in automatic
2. Shift SG level control from bypass valves to feedwater regulating valves
3. Place individual SG level controllers in automatic

**Additional Equipment:**

| Power Level | Action |
|-------------|--------|
| Before >70% | Start second condensate pump and MFP |
| ~30% | Start first heater drain pump |
| Before >90% | Start second heater drain pump |

---

## 5. PRA Insights

- Loss of feedwater sequences: Very small fraction of total core damage frequency
  - Sequoyah: 0.6%
  - Zion, Surry, Trojan: Insignificant percentage
- Modifications to power conversion system: Minimal contribution to risk reduction/achievement

---

## 6. Summary

**Condensate System:**
- Multipressure, three-shell condenser receives LP turbine exhaust and steam dumps
- Condensate pumps transfer through demineralizers and LP heaters to MFP suction

**Feedwater System:**
- MFPs take suction from condensate pumps and heater drain pumps
- Transfer through HP heaters, regulating valves, FWIVs to steam generators
- Automatically controlled flow maintains desired SG water levels

**Heater Drain System:**
- Improves efficiency by collecting high-temperature drainage from MSRs and FW heaters
- Returns drains to condensate and feedwater system

**Chemistry Control:**
- Purification via demineralizers
- Chemical injection (hydrazine for O₂, morpholine for pH)
- Steam generator blowdown

---

## 7. Data Tables and Specifications

### 7.1 Temperature Profile Through System

| Location | Temperature |
|----------|-------------|
| Condenser hotwell | ~120°F |
| After LP heaters (5 stages) | 360°F |
| After 6th-stage HP heater | 397°F |
| After 7th-stage HP heater (SG inlet) | 440°F |

### 7.2 Key Setpoints Summary

| Parameter | Setpoint |
|-----------|----------|
| High SG level trip/isolation | 69.0% |
| FW isolation on low T_avg (with P-4) | 564°F |
| MFP low suction pressure trip | 195 psig |
| MFP high discharge pressure trip | 1,850 psig |
| MFP overspeed trip | 5,850 rpm |
| Condenser minimum vacuum | 25 in. Hg |
| Demineralizer bypass on high ΔP | 60 psid |

### 7.3 Flow Capacities

| Component | Capacity |
|-----------|----------|
| Condensate pump | 11,000 gpm (70% each) |
| Main feedwater pump | 19,800 gpm (70% each) |
| Heater drain pump | 5,950 gpm |
| Startup AFW pump | 1,020 gpm |
| SG blowdown | 100 gpm max per SG |

### 7.4 Power Level Constraints

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
