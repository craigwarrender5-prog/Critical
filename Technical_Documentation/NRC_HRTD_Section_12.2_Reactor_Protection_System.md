# NRC HRTD Section 12.2 — Reactor Protection System: Reactor Trip Signals

**Source:** https://www.nrc.gov/docs/ML1122/ML11223A301.pdf  
**Retrieved:** 2026-02-14  
**Revision:** Rev 0109

---

## Overview

This document provides comprehensive technical details on the Westinghouse PWR Reactor Protection System trip signals including:

- All reactor trip functions with setpoints and coincidence logic
- Trip basis and accident/transient protection
- Protection-grade permissives (P-4 through P-14)
- Control-grade interlocks (C-1 through C-11)
- Overtemperature ΔT (OTΔT) and Overpower ΔT (OPΔT) trip equations

---

## Key Technical Data for Simulator Development

### Reactor Trip Mechanism

**Trip Breakers:**
- Two series-connected reactor trip breakers (RTB A and RTB B)
- Each breaker powered by separate protection train (A or B)
- Either breaker opening removes power to control rod drive mechanisms (CRDMs)
- Undervoltage coil de-energizes → mechanical latch releases → spring opens breaker
- Rods fall by gravity when power removed

**Redundancy:**
- Either protection train can initiate trip independently
- Diverse trip signals for same accident scenarios

---

## Manual Reactor Trip

**Actuation:** Two independent manual trip switches in control room

**Logic:** Either switch actuates both trains

**Additional Actions:**
- Initiates turbine trip
- Independent of automatic circuitry

**Purpose:** Operator backup to all automatic trips

---

## Nuclear Flux Trips

### Source Range High Flux Trip
| Parameter | Value |
|-----------|-------|
| Setpoint | 10⁵ cps |
| Coincidence | 1/2 |
| Blockable | Yes, when P-6 satisfied (manual) |
| Auto-Reinstate | When both IR channels < P-6 |

**Purpose:** Protect against startup reactivity excursions

**Events Protected:**
- Uncontrolled rod withdrawal from subcritical
- Inadvertent boron dilution
- Excessive heat removal (steam line break, feedwater addition)

### Intermediate Range High Flux Trip
| Parameter | Value |
|-----------|-------|
| Setpoint | Current equivalent to 25% power |
| Coincidence | 1/2 |
| Blockable | Yes, when P-10 satisfied (manual) |
| Auto-Reinstate | When 3/4 power range channels < P-10 |

**Purpose:** Protect against startup reactivity excursions

### Power Range High Flux Trip - Low Setpoint
| Parameter | Value |
|-----------|-------|
| Setpoint | 25% |
| Coincidence | 2/4 |
| Blockable | Yes, when P-10 satisfied (manual) |
| Auto-Reinstate | When 3/4 power range channels < P-10 |

**Purpose:** Startup excursion protection

### Power Range High Flux Trip - High Setpoint
| Parameter | Value |
|-----------|-------|
| Setpoint | 109% |
| Coincidence | 2/4 |
| Blockable | No (always active) |

**Purpose:** Limit maximum power to value assumed in FSAR analysis

**Events Protected:**
- Excessive load increase
- Excessive heat removal
- Boron dilution
- Inadvertent rod withdrawal
- Rod ejection

### Positive Neutron Flux Rate Trip
| Parameter | Value |
|-----------|-------|
| Setpoint | +5% change with 2-second time constant |
| Coincidence | 2/4 |
| Blockable | No (seals in until manual reset) |

**Purpose:** Rod ejection accident protection

### Negative Neutron Flux Rate Trip
| Parameter | Value |
|-----------|-------|
| Setpoint | -5% change with 2-second time constant |
| Coincidence | 2/4 |
| Blockable | No (seals in until manual reset) |

**Purpose:** Dropped rod protection (prevents power overshoot from subsequent rod withdrawal)

---

## Overtemperature ΔT Trip (OTΔT)

**Purpose:** Protect against departure from nucleate boiling (DNB), ensures DNBR ≥ 1.30

**Setpoint Equation:**
```
OTΔT setpoint = ΔT₀ × [K₁ - K₂((1+τ₁s)/(1+τ₂s))(T - T') + K₃(P - P') - f₁(ΔI)]
```

**Where:**
| Symbol | Description | Typical Value |
|--------|-------------|---------------|
| ΔT₀ | Indicated ΔT at rated power | % power |
| T | Measured RCS T_avg | °F |
| T' | Nominal T_avg at rated power | 584.7°F |
| P | Pressurizer pressure | psig |
| P' | Nominal operating pressure | 2235 psig |
| K₁ | Preset bias (sets trip at rated conditions) | Adjustable |
| K₂ | Temperature gain | Adjustable |
| K₃ | Pressure gain | Adjustable |
| τ₁, τ₂ | Lead-lag time constants | sec |
| f₁(ΔI) | Axial flux difference penalty | Function |

**Coincidence:** 2/4

**Blockable:** No

**Parameter Effects:**
- **T > T':** Setpoint reduced (higher T_avg reduces DNB margin)
- **P < P':** Setpoint reduced (lower pressure reduces DNB margin)
- **Large |ΔI|:** Setpoint reduced (skewed power distribution)

**Rod Stop/Turbine Runback (C-3):**
- Actuates when ΔT within 3% of OTΔT setpoint
- 2/4 coincidence
- Blocks automatic and manual rod withdrawal
- Initiates cyclic turbine runback

**Events Protected:**
- Uncontrolled rod withdrawal at power
- Uncontrolled boron dilution
- Excessive load increase
- RCS depressurization

---

## Overpower ΔT Trip (OPΔT)

**Purpose:** Protect against excessive power density (kW/ft) and fuel centerline melt

**Setpoint Equation:**
```
OPΔT setpoint = ΔT₀ × [K₄ - K₅((τ₃s)/(1+τ₃s))T + K₆(T - T') - f₂(ΔI)]
```

**Where:**
| Symbol | Description |
|--------|-------------|
| ΔT₀ | Indicated ΔT at rated power |
| T | Measured RCS T_avg |
| T' | Nominal T_avg at rated power (584.7°F) |
| K₄ | Preset bias |
| K₅ | Rate gain |
| K₆ | Temperature gain |
| τ₃ | Rate-lag time constant |
| f₂(ΔI) | Axial flux difference penalty |

**Coincidence:** 2/4

**Blockable:** No

**Rod Stop/Turbine Runback (C-4):**
- Actuates when ΔT within 3% of OPΔT setpoint
- 2/4 coincidence

**Events Protected:**
- Uncontrolled rod withdrawal at power
- Uncontrolled boron dilution
- Excessive load increase
- Steam line break

---

## Pressure Trips

### Pressurizer Low Pressure Trip
| Parameter | Value |
|-----------|-------|
| Setpoint | 1865 psig |
| Coincidence | 2/4 |
| Blockable | Yes, below P-7 (auto) |

**Purpose:** Protect against excessive core steam voids, limit OTΔT range

**Events Protected:**
- LOCA
- Steam line break
- SG tube rupture

### Pressurizer High Pressure Trip
| Parameter | Value |
|-----------|-------|
| Setpoint | 2385 psig |
| Coincidence | 2/4 |
| Blockable | No (always active) |

**Purpose:** Protect RCS pressure boundary integrity

**Events Protected:**
- Uncontrolled rod withdrawal
- Loss of electrical load
- Turbine trip

### Pressurizer High Water Level Trip
| Parameter | Value |
|-----------|-------|
| Setpoint | 92% |
| Coincidence | 2/3 |
| Blockable | Yes, below P-7 (auto) |

**Purpose:** Prevent solid-water operation, protect relief/safety valves from liquid discharge

---

## Flow Trips

### Low Reactor Coolant Flow Trip
| Parameter | Value |
|-----------|-------|
| Setpoint | <90% of rated flow per loop |
| Coincidence | 2/3 per loop |
| Above P-8 (39%) | Single loop loss → trip |
| Above P-7 (10%) | Two or more loops → trip |
| Below P-7 | Blocked |

**Purpose:** Ensure adequate flow for core heat removal, DNB protection

### RCP Breaker Position Trip
| Parameter | Value |
|-----------|-------|
| Actuation | 2/4 RCP breakers open |
| Blockable | Yes, below P-7 (auto) |

**Purpose:** Redundant to low flow trip

### RCP Bus Undervoltage Trip
| Parameter | Value |
|-----------|-------|
| Setpoint | 68.6% of nominal bus voltage |
| Coincidence | 1/2 on 2/2 buses |
| Blockable | Yes, below P-7 (auto) |

**Purpose:** Anticipate loss of flow, redundant protection

### RCP Bus Underfrequency Trip
| Parameter | Value |
|-----------|-------|
| Setpoint | 57.7 Hz |
| Coincidence | 1/2 on 2/2 buses |
| Blockable | Yes, below P-7 (auto) |
| Additional Action | Trips RCP breakers to preserve coastdown |

**Purpose:** Protect against reduced pump speed/flow

---

## Steam Generator Trips

### Low-Low Steam Generator Water Level Trip
| Parameter | Value |
|-----------|-------|
| Setpoint | 11.5% (narrow range) |
| Coincidence | 2/3 per SG, in 1/4 SGs |
| Blockable | No |

**Purpose:** Protect against loss of heat sink

### Low Feedwater Flow Trip
| Parameter | Value |
|-----------|-------|
| Level Setpoint | 25.5% |
| Flow Mismatch | 1.5×10⁶ lb/hr (steam > feed) |
| Coincidence | 1/2 mismatch + 1/2 low level, in 1/4 SGs |
| Blockable | No |

**Purpose:** Anticipate loss of heat sink (trips before low-low level)

---

## Other Trips

### Safety Injection Actuation Trip
**Actuation:** Any SI actuation signal

**Coincidence:** 1/2 trains

**Purpose:** Ensure reactor trip during LOCA or steam line break

### Turbine Trip → Reactor Trip
| Parameter | Value |
|-----------|-------|
| Actuation | 4/4 stop valves closed OR 2/3 low EHC pressure |
| EHC Pressure Setpoint | 800 psig (GE) or 45 psig auto-stop oil (W) |
| Blockable | Yes, below P-7 (or P-9 at some plants) |

**Purpose:** Remove heat source when secondary load is lost, minimize thermal transient

---

## Protection-Grade Permissives (P-n)

| Permissive | Setpoint | Coincidence | Function |
|------------|----------|-------------|----------|
| **P-4** | RTB open | Either breaker | Trips turbine, isolates MFW, input to SI reset |
| **P-6** | IR > 10⁻¹⁰ A | 1/2 | Enables SR trip block switches |
| **P-7** | Power < 10% | PR 3/4 < 10% AND turbine (P-13) | Blocks at-power trips |
| **P-8** | PR < 39% | 3/4 | Blocks single-loop low flow trip |
| **P-9** | PR < 50% | 3/4 | Blocks turbine trip reactor trip (some plants) |
| **P-10** | PR > 10% | 2/4 | Enables IR trip block, PR low setpoint block |
| **P-11** | PZR pressure < 1915 psig | 2/3 | Enables low pressure SI block |
| **P-12** | T_avg < 553°F | 2/4 | Enables high steam flow SI block, blocks steam dumps |
| **P-13** | Turbine load < 10% | 2/2 impulse pressure | Input to P-7 |
| **P-14** | SG level > 69% | 2/3 per SG | Trips turbine, MFPs, closes MFW valves |

---

## Control-Grade Interlocks (C-n)

| Interlock | Setpoint | Coincidence | Function |
|-----------|----------|-------------|----------|
| **C-1** | IR > 20% equivalent | 1/2 | Blocks rod withdrawal (auto + manual) |
| **C-2** | PR > 103% | 1/4 | Blocks rod withdrawal (auto + manual) |
| **C-3** | ΔT within 3% of OTΔT | 2/4 | Blocks rod withdrawal, turbine runback |
| **C-4** | ΔT within 3% of OPΔT | 2/4 | Blocks rod withdrawal, turbine runback |
| **C-5** | Turbine load < 15% | 1/1 | Blocks automatic rod withdrawal |
| **C-7** | Load decrease > 10% step or 5%/min | 1/1 | Arms steam dumps (seals in) |
| **C-8** | Turbine tripped | As above | Arms steam dumps, shifts to turbine-trip controller |
| **C-9** | Condenser available | 2/2 vacuum + 1/2 CW pump | Enables steam dump operation |
| **C-11** | Bank D > 223 steps | 1/1 | Blocks automatic rod withdrawal |

---

## Critical Setpoint Summary

| Trip/Interlock | Setpoint | Notes |
|----------------|----------|-------|
| SR High Flux | 10⁵ cps | Startup protection |
| IR High Flux | 25% equivalent | Startup protection |
| PR High Flux - Low | 25% | Blockable above P-10 |
| PR High Flux - High | 109% | Always active |
| Rate Trip | ±5%/2 sec | Seals in |
| PZR Low Pressure | 1865 psig | Blocked below P-7 |
| PZR High Pressure | 2385 psig | Always active |
| PZR High Level | 92% | Blocked below P-7 |
| Low Flow | 90% per loop | Complex logic per P-7, P-8 |
| SG Low-Low Level | 11.5% NR | 2/3 in any SG |
| Low SI | 1807 psig | SI actuation |
| P-6 | 10⁻¹⁰ A IR | SR block permissive |
| P-7 | 10% power | At-power permissive |
| P-10 | 10% PR | Nuclear at-power |
| P-11 | 1915 psig | Low pressure SI block |
| P-12 | 553°F | Steam flow SI block |

---

## Critical Notes for Simulator

1. **Startup Sequence Trips:**
   - SR trip active until blocked via P-6
   - IR trip active until blocked via P-10
   - PR low setpoint active until blocked via P-10
   - All at-power trips blocked below P-7

2. **Power Operation Trips:**
   - 109% high flux always active
   - Rate trips always active
   - OTΔT and OPΔT always active
   - Low pressure, high level blocked below P-7

3. **Trip Bypassing:**
   - Only allowed when permissive is satisfied
   - Automatic reinstatement when permissive clears
   - Manual action required to block (except rate trips)

4. **Rod Stops vs. Trips:**
   - Rod stops (C-1 through C-4) prevent rod withdrawal
   - Trips remove power from rods
   - Rod stops provide margin before trip

5. **Seal-In Functions:**
   - Rate trips seal in until manually reset
   - C-7 seals in until manually reset
   - Important for transient analysis

---

## Implementation Priority for Simulator

**Phase 1 (Startup through HZP):**
- SR, IR, PR high flux trips with blocking logic
- P-6 and P-10 permissives
- Manual trip
- Basic setpoint checking

**Phase 2 (Power Operations):**
- OTΔT and OPΔT trip equations
- Low pressure, high pressure, high level trips
- P-7, P-8 permissives
- Low flow trips with P-7, P-8 logic
- Rod stop interlocks (C-1 through C-4)

**Phase 3 (Advanced Features):**
- Rate trips with seal-in logic
- Turbine trip reactor trip with P-9
- SI actuation trip
- All permissives and interlocks
- SG trips

---

## References

This document should be referenced for:
- Complete trip setpoint tables
- OTΔT/OPΔT trip equations
- Permissive and interlock logic
- Trip coincidence requirements
- Blocking and bypass rules
- Trip basis documentation

---
