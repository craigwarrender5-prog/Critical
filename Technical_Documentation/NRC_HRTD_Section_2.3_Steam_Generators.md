# NRC HRTD Section 2.3 — Steam Generators

**Source:** https://www.nrc.gov/docs/ML1125/ML11251A016.pdf  
**Retrieved:** 2026-02-17  
**Revision:** Rev 10/08

---

## Overview

This document describes steam generator design, operation, and instrumentation for Westinghouse PWR plants. Critical for simulator development of SG thermal modeling and startup procedures.

---

## 2.3.1 Introduction

The purposes of the steam generators are to:

1. Produce dry saturated steam for the turbine-generator and its auxiliary systems
2. Act as a heat sink for the RCS during normal, abnormal, and emergency conditions
3. Provide a barrier between the radioactive RCS and the non-radioactive secondary system

---

## 2.3.2 Steam Generator Description

Two identical steam generators are installed in the nuclear steam supply system. Each steam generator is a vertical shell and U-tube heat exchanger and is the heat transfer interface between the RCS and the secondary system.

### Internal Structure — Four Distinct Regions

| Region | Description |
|--------|-------------|
| **Downcomer** | Circular area between tube wrapper and outer shell, from secondary support plate to tubesheet |
| **Evaporator** | Area inside tube wrapper from tubesheet to top of tube bundle |
| **Riser** | Transition area from evaporator to steam drum (top of bundle to bottom of separator support plate) |
| **Steam Drum** | Area inside upper shell from separator support plate to main steam outlet nozzle |

---

## 2.3.3 Steam Generator Flow Paths

### Primary Side Flow
- Hot reactor coolant enters through inlet nozzle into inlet plenum
- Divider plate separates inlet and outlet
- Coolant flows through U-tubes, heat transfers to secondary
- Coolant exits through outlet nozzles to RCPs

### Secondary Side Flow
- Feedwater enters through nozzle in upper downcomer section
- Flows into downcomer via main feed ring (12" diameter torus with J-tubes)
- Mixes with recirculating water from separators/dryers
- Flows down over tubesheet, up through evaporator (tube bundle)
- Heat transfer produces steam-water mixture (~30% quality)
- Mixture rises through riser to steam drum
- Steam separators and dryers remove moisture
- **99.8% quality steam** exits through main steam nozzle

---

## 2.3.4 Design Parameters

### 2.3.4.1 General Information

| Parameter | Value |
|-----------|-------|
| Type | Vertical U-tube |
| ASME Code | Section III, Class A |
| Dry weight | 1,004,000 lb |
| Height | 749 inches (62.4 ft) |
| Upper shell OD | 239.75 inches (20 ft) |
| Lower shell OD | 165 inches (13.75 ft) |

### 2.3.4.2 Primary Side Design

| Parameter | Value |
|-----------|-------|
| Design pressure | 2500 psia |
| Design temperature | 650°F |
| Design thermal power (NSSS) | 2700 MWt |
| Coolant flow | 61 × 10⁶ lbm/hr |
| Normal operating pressure | 2250 psia |
| Coolant volume | 1683 ft³ |
| Inlet nozzle | 1 × 42" ID |
| Outlet nozzles | 2 × 30" ID |
| Primary manways | 2 × 16" ID |

### 2.3.4.3 Secondary Side Design

| Parameter | Value |
|-----------|-------|
| Design pressure | 1000 psia |
| Design temperature | 650°F |
| Normal operating steam pressure (full load) | 850 psia |
| Normal operating steam temperature (full load) | 525.2°F |
| Blowdown flow | 4880 lbm/hr each |
| Steam flow | 5.635 × 10⁶ lbm/hr |
| Feedwater temperature | 431.5°F |

### 2.3.4.4 Tube Bundle

| Parameter | Value |
|-----------|-------|
| Number of tubes | 8,519 per SG |
| Tube OD | 3/4 inch |
| Tube wall thickness | 0.048 inches |
| Material | Inconel |
| Support | Eggcrate supports at ≤3 ft intervals |
| Top bundle support | Bat wing assembly |

### 2.3.4.5 Operating Weights

| Condition | Weight (lb) |
|-----------|-------------|
| Dry | 1,004,000 |
| Flooded | 1,526,700 |
| Operating | 1,218,251 |

---

## 2.3.5 Operating Characteristics

### 2.3.5.1 Heat Transfer

The rate of heat transfer from primary to secondary is determined by:

```
Q̇ = U × A × (T_avg - T_sat)
```

Where:
- Q̇ = Heat transfer rate
- U = Overall heat transfer coefficient
- A = Heat transfer surface area
- T_avg = Average reactor coolant temperature
- T_sat = Saturation temperature of steam generator

**Key relationship:** As steam flow increases, heat removal increases, differential temperature increases, T_sat decreases. To minimize steam pressure decrease at high power, **T_avg is ramped from 532°F to 572.5°F** as power increases.

### 2.3.5.2 Shrink and Swell

**Swell** — Increase in SG level when steam flow increases:
- Increased steam flow → SG pressure decrease
- Steam bubbles grow larger, more water flashes to steam
- Lower T_sat → greater ΔT from primary → additional steam bubbles
- Volumetric expansion of steam/water mixture → level rise (swell)

**Shrink** — Decrease in SG level when steam flow decreases:
- Decreased steam flow → SG pressure increase
- Steam bubbles collapse
- Volume of steam-water mixture decreases → level decrease (shrink)

### 2.3.5.3 Recirculation Ratio

The recirculation ratio varies with power:

| Power Level | Recirculation Ratio |
|-------------|---------------------|
| 5% | 33:1 |
| 100% | 4:1 |

At high power (≥50%), circulation flow remains relatively constant due to increased resistance to flow. Since steam flow rate increases with power but circulation is constant, recirculation ratio decreases.

---

## 2.3.6 Steam Generator Chemistry Control

### Specifications (Table 2.3-1)

| Parameter | Value | Units |
|-----------|-------|-------|
| pH | 9.2 - 9.5 | — |
| Specific Conductivity | ≤4 | μmhos/cm |
| Cation Conductivity | ≤0.8 | μmhos/cm |
| Sodium | ≤20 | ppb |
| Chlorides | ≤80 | ppb |
| Silica | ≤300 | ppb |
| Suspended Solids | ≤1000 | ppb |

### Control Methods

- **pH Control:** Ammonium hydroxide added to condensate system
- **Oxygen Control:** Condensate deaeration + hydrazine injection (10 ppb limit)
- **Conductivity/Chlorides:** Condensate demineralizers (ion exchangers)
- **Solids Control:** Steam generator blowdown + condensate filtration

---

## 2.3.7 Steam Generator Blowdown and Recovery System

### Functions

1. Maintains SG water chemistry by continuous impurity removal
2. Provides primary-to-secondary leak detection via radioactivity sampling
3. Minimizes secondary inventory loss by purifying and returning blowdown to condenser

### System Configuration

| Component | Description |
|-----------|-------------|
| Bottom blowdown nozzle | 2" diameter, connected to ring on tubesheet |
| Surface blowdown nozzle | 1" diameter, header above main feed ring |
| Blowdown tank | 2,350 gallons |
| Normal blowdown rate | **150 gpm** |
| Cooler outlet temperature | ~120°F (compatible with ion exchangers) |

### Flow Paths

- **Normal operation:** Both bottom blowdowns → blowdown tank → coolers → filters → ion exchangers → condenser
- **SG draining:** Ion exchanger outlet → circulating water system
- **High radiation:** Ion exchanger outlet → miscellaneous waste system (automatic)

---

## 2.3.8 Steam Generator Instrumentation

### 2.3.8.1 Level Transmitters

| Type | Quantity | Range | Usage |
|------|----------|-------|-------|
| Narrow Range | 6 | 183.16" (0-100%) | RPS trip (37%), turbine trip (92.5%), level control |
| Wide Range | 4 | 486" | AFW actuation (40%) |

**Tap Locations:**
- Upper narrow range: 614-5/16" from base
- Lower narrow range: 431-5/32" from base (downcomer)
- Upper wide range: Same as narrow range upper
- Lower wide range: Tubesheet elevation

### 2.3.8.2 Pressure Transmitters

- 4 safety-related transmitters per SG
- Connected to upper level transmitter connections
- **ESF actuation (SGIS):** 703 psia
- **Reactor trip:** 703 psia
- Logic: 2 out of 4

### 2.3.8.3 RCS Flow

- 8 differential pressure detectors (4 per loop)
- Taps: hot leg to SG outlet plenum
- **Low RCS flow trip:** 95% (2 out of 4 logic)

---

## 2.3.9 Steam Generator Operations

### 2.3.9.1 Plant Startup

> "When the plant is in a cold shutdown condition, the steam generators are in a wet layup condition. Wet layup consists of filling the steam generator completely with water that has been treated with hydrazine and ammonium hydroxide."

> "Since the steam generators are full, the generators must be drained down to the normal operating level (65%) prior to startup."

> "As the RCS is heated up by the RCP energy, steam production begins in the steam generators. The level in the steam generators will be maintained by AFW."

**Note:** This document describes CE (Combustion Engineering) design with 65% operating level. Westinghouse 4-loop uses 33±5% NR level per NRC HRTD Section 19.0.

### 2.3.9.2 Plant Shutdown and Cooldown

1. Power decreased by boration
2. At ~10% power, turbine taken offline
3. At ~3% power, AFW placed in service, main feedwater pump stopped
4. Control rods inserted (hot standby)
5. Steam dumped to condenser (or atmosphere if condenser unavailable)
6. AFW maintains SG level
7. At ~300°F and ≤260 psia, shutdown cooling system placed in service
8. SGs placed in wet layup using AFW

### 2.3.9.3 Natural Circulation

Three conditions required:
1. **Heat source:** Decay heat from reactor
2. **Heat sink:** Steam generators (level maintained, steam removed)
3. **Elevation difference:** ~35 ft between SG centerline and core centerline

Natural circulation flow driven by density difference between:
- Dense, cooler water in downcomer
- Less dense, heated water/steam mixture in evaporator

---

## 2.3.10 Design Transient Cycles

### Normal Transients (40-year design life)

| Transient | Cycles |
|-----------|--------|
| Heatup/cooldown (100°F/hr, 70-532°F) | 500 |
| Power changes (15-100%, 5%/min) | 15,000 |
| 10% step changes (10-90%, 100-20%) | 2,000 |
| Hydrostatic test (3125 psia) | 10 |
| Leak testing (2500 psia) | 320 |
| Normal variations (±100 psi, ±6°F) | 10⁶ |
| Reactor trips from 100% | 400 |

### Abnormal Transients

| Transient | Cycles |
|-----------|--------|
| Loss of turbine load from 100% (no direct trip) | 40 |
| Total loss of RCS flow at 100% | 40 |
| Loss of secondary system pressure | 5 |

---

## Key Values for Simulator Implementation

| Parameter | Value | Source |
|-----------|-------|--------|
| Tubes per SG | 8,519 | Table 2.3-2 |
| Tube OD | 0.75" | Table 2.3-2 |
| Tube wall | 0.048" | Table 2.3-2 |
| Operating steam pressure | 850 psia | Table 2.3-2 |
| Operating steam temp | 525.2°F | Table 2.3-2 |
| Steam flow (full power) | 5.635 × 10⁶ lbm/hr | Table 2.3-2 |
| Feedwater temp | 431.5°F | Table 2.3-2 |
| Normal blowdown | 150 gpm | Section 2.3.7 |
| Wet layup level | 100% WR | Section 2.3.9.1 |
| Operating level (W 4-loop) | 33±5% NR | NRC HRTD 19.0 |
| Low level trip | 37% NR | Section 2.3.8.1 |
| High level turbine trip | 92.5% NR | Section 2.3.8.1 |
| AFW actuation | 40% WR | Section 2.3.8.1 |
| SGIS/Rx trip pressure | 703 psia | Section 2.3.8.2 |

---

## References

1. NRC HRTD Section 2.3 - Steam Generators (ML11251A016), Rev 10/08
2. NRC HRTD Section 19.0 - Plant Operations (ML11223A342), Rev 0400
3. ASME Boiler and Pressure Vessel Code, Section III
