# NRC HRTD Section 5.7 — Auxiliary Feedwater System (Full Source)

**Source:** https://www.nrc.gov/docs/ml1122/ml11223a229.pdf  
**Document:** Westinghouse Technology Systems Manual Section 5.7  
**Retrieved:** 2026-02-17  
**Revision:** Rev 0603

---

## Document Purpose

This document provides the complete NRC HRTD Section 5.7 technical content for the Westinghouse 4-Loop PWR Auxiliary Feedwater System. This serves as an authoritative source for simulator development, particularly for decay heat removal and emergency feedwater supply modeling.

---

## 1. Introduction

### System Purposes

1. **Provide feedwater to steam generators** to maintain heat sink for:
   - Loss of main feedwater (MFW)
   - Unit trip and loss of offsite power
   - Small-break loss-of-coolant accident (LOCA)

2. **Provide feedwater source** during plant startup and shutdown

### Design Basis

- Automatically starts and supplies sufficient feedwater to prevent relief of primary coolant through pressurizer safety valves
- Adequate suction source and flow capacity to:
  - Maintain reactor at hot standby for required period
  - Cool reactor coolant to RHR entry temperature

---

## 2. System Description

### 2.1 Pump Configuration

| Pump Type | Quantity | SGs Supplied |
|-----------|----------|--------------|
| Electric motor-driven | 2 | 2 each (different SGs per pump) |
| Steam turbine-driven | 1 | All 4 |

**Response Time:** All three pumps automatically deliver rated flow within 1 minute upon automatic start signal.

### 2.2 Water Supplies

**Primary Source:** Condensate Storage Tank (CST)
- Required by Technical Specifications to contain minimum water available to AFW

**Backup Source:** Essential Service Water (ESW)
- Separate ESW train feeds each motor-driven pump
- Turbine-driven pump can receive from either ESW train
- Auto-opens on 2/3 low suction pressure AND pump running
- Poor quality water — emergency use only

### 2.3 Design Parameters

| Parameter | Value |
|-----------|-------|
| Feedwater temperature range | 40-120°F |
| Operating pressure range | RHR pressure (~110 psig SG) to highest SG safety valve setpoint (1234 psig) |
| System design pressure | Up to ~1650 psig where necessary |

### 2.4 Redundancy Features

- Complete redundancy in pump capacity and water supply
- Under all credible accident conditions, each unaffected SG receives required feedwater
- Only 2 SGs required operable for any credible accident
- Redundant electrical power and control air supplies
- Motor-driven pumps: Vital AC distribution
- Turbine-driven pump: Steam from either of 2 main steam lines (upstream of MSIVs)

---

## 3. Component Descriptions

### 3.1 Motor-Driven AFW Pumps

| Parameter | Value |
|-----------|-------|
| Quantity | 2 |
| Type | Multistage, horizontal, centrifugal |
| Capacity | 440 gpm each |
| Discharge pressure | ~1,300 psig |
| Power supply | 4.16 kVac Class IE vital distribution |

**Control:**
- Local switches permit local operation
- Control room switch: "Run" / "Stop" / "Pull to Lock"
- Pull-to-lock prevents start even with automatic start signal present

**Automatic Start Signals (Motor-Driven Pumps):**

| Signal | Condition |
|--------|-----------|
| Low-low SG level | Any single SG |
| Loss of 1 MFP | If power > 80% |
| Loss of both MFPs | Any power level |
| Safety injection actuation | — |
| Loss of Class IE power | — |

### 3.2 Turbine-Driven AFW Pump

| Parameter | Value |
|-----------|-------|
| Quantity | 1 |
| Type | Multistage, horizontal, centrifugal |
| Capacity | 880 gpm |
| Discharge pressure | ~1,200 psig |
| Turbine type | Horizontal, noncondensing |
| Turbine rating | 1,100 hp |
| Steam supply pressure range | 100-1,275 psig |
| Steam source | SG-2 and SG-3 (upstream of MSIVs) |
| Exhaust | Directly to atmosphere |

**Steam Supply Valves:**
- Main valve: Air-operated, normally closed (FC-HV-312)
- Bypass valves: For turbine warming, operated from MCB
- Governor valve (HV-313): Controls speed from MCB
- Steam traps provided for moisture removal
- Drains on turbine casing, steam chest, exhaust piping

**Automatic Start Signals (Turbine-Driven Pump):**

| Signal | Condition |
|--------|-----------|
| Low-low SG level | 2 of 4 SGs |
| Loss of 1 MFP | If power > 80% |
| Loss of both MFPs | Any power level |
| Safety injection actuation | — |
| Loss of either Class IE vital distribution | — |

### 3.3 Level Control Valves

| Parameter | Value |
|-----------|-------|
| Quantity | 8 total |
| Motor-driven pump lines | 4 valves (one per SG) |
| Turbine-driven pump lines | 4 valves (one per SG) |
| Normal position | Fully closed |
| Control | Toggle switch for manual open/close |

**Automatic Operation:**
- Upon automatic actuation signal, valves control SG level to preselected setpoint

**Loop Break Protection (Some Plants):**
- Monitor pressure between level control valve and SG
- Low pressure setpoint (~100 psig) closes valve
- Prevents continued feeding of faulted SG during steam/FW break

### 3.4 Water Supply Details

**Condensate Storage Tank:**
- Gravity feed to AFW pump suction header
- Check valve and normally-open isolation valve per pump
- Suction pressure normally indicated in control room

**CST Reserve Protection Methods (Plant-Specific):**
1. Standpipe in supply line to main condenser
2. Level control valve closes at preset level
3. Side tap on CST at height ensuring minimum level

**ESW Automatic Swap:**
- 2-out-of-3 low suction pressure logic
- Coincident with AFW pump running
- Prevents inadvertent ESW injection

**Fire Protection Connection (Some Plants):**
- Blind flange on motor-driven pump discharge
- Spool piece connection for high pressure fire water
- Only usable when SG pressure < 120 psig

---

## 4. System Features and Interrelationships

### 4.1 Safety Classification

- Safety-related portions designed for seismic events
- Meets single failure criteria
- Considers feedwater line rupture as initiating event
- Provides required flow to 2+ SGs regardless of any single active/passive failure

### 4.2 Electrical and Control Design

**Turbine-Driven Pump:**
- Served by both electric and control air subsystems
- Appropriate measures preclude interaction between subsystems
- Control power from 3rd DC channel (distinct from motor-driven pump channels)

### 4.3 Flow Requirements

| Scenario | Required Flow | Basis |
|----------|--------------|-------|
| Loss of site power (AFW available immediately) | 440 gpm to 2 SGs | Prevents PZR safety relief, maintains tube coverage |
| AFW delayed 10 minutes | 880 gpm | Same requirements |

### 4.4 Post-LOCA Function

- Primary-to-secondary heat transfer may be necessary for decay heat removal
- For spectrum of small-break LOCAs, ECCS flow alone not sufficient
- AFW provides required heat sink via SG secondary side

### 4.5 Materials

- Generally carbon steel construction
- CST lined to prevent corrosion
- Other components protected by chemical additions

---

## 5. PRA Insights

### 5.1 Risk Contribution

AFW failure is a **small contributor** to core damage frequency:
- Zion: 1.4%
- Sequoyah: 2.6%
- Surry: 14.8% (requires 2 PORVs for bleed-and-feed)

**Key Reason:** Ability to initiate bleed-and-feed cooling using HPI and PORVs

### 5.2 Accident Categories Involving AFW Failure

**1. Loss of Power System:**
- Loss of offsite power + AFW failure + PORV actuation power loss → Core damage
- Station blackout fails all AC except vital buses from DC inverters
- Only turbine-driven AFW available; fails on battery depletion or hardware
- DC bus failure trips power conversion; partial AFW failure + PORV control loss

**2. Transient-Caused Trip:**
- Trip + power conversion loss + AFW failure
- Bleed-and-feed fails (operator error or hardware)

**3. Loss of Main Feedwater:**
- FW line break affects common water source
- Operator fails alternate sources and bleed-and-feed
- MFW loss + AFW failure + bleed-and-feed failure

**4. Steam Generator Tube Rupture:**
- SGTR + AFW failure
- Coolant lost until RWST depleted
- HPI fails (no recirculation from empty sump)

### 5.3 Risk-Important Failure Modes (Decreasing Order)

1. Turbine-driven pump failure to start or run
2. Motor-driven pump failure to start or run
3. Turbine- or motor-driven pump unavailable (test/maintenance)
4. Valve failures (steam admission, trip/throttle, flow control, discharge, suction)

### 5.4 Plant-Specific Considerations

- Human error: Locked-out pump not restarted, misalignment after test
- Common-mode failures:
  - Undetected flow diversion through cross-connect (multi-unit sites)
  - Steam binding from MFW leakage through check valves

---

## 6. Summary

### System Function

Supplies high pressure feedwater to SGs to maintain water inventory for RCS heat removal by secondary side steam release when:
- Main feedwater inoperable
- During startup/shutdown

### Discharge Pressure

Sufficient to deliver feedwater at any SG pressure up to safety valve setpoint.

### System Capacity

Designed so four SGs will not boil dry, nor will primary side relieve through PZR relief valves following loss of MFW with unit trip.

### Configuration Summary

| Component | Capacity | SGs Served |
|-----------|----------|------------|
| 2 Motor-driven pumps | 440 gpm each | 2 SGs each |
| 1 Turbine-driven pump | 880 gpm | All 4 SGs |

### Automatic Start Signals

**All Pumps:**
- Loss of both MFPs
- Loss of 1 MFP (power > 80%)
- Low-low level in 2+ SGs (turbine-driven) or any SG (motor-driven)
- Safety injection actuation
- Loss of Class IE power

**Motor-Driven Only:**
- Low-low level in any single SG

### Water Sources

| Source | Capacity | Quality |
|--------|----------|---------|
| CST (primary) | Tech Spec min: 280,000 gal | High quality |
| ESW (backup) | Unlimited | Poor quality |
| Fire protection (emergency) | Limited | Raw water |

---

## 7. Design Data Table

### Table 5.7-1: AFW System Design Data

| Parameter | Value |
|-----------|-------|
| **Number of pumps per unit** | 3 |
| Motor-driven | 2 |
| Turbine-driven | 1 |
| | |
| **Design flow rate** | |
| Motor-driven pumps, each | 440 gpm |
| Turbine-driven pump | 880 gpm |
| | |
| **System design pressure** | 1,650 psig |
| **Design feedwater temperature** | 40-120°F |
| | |
| **Design discharge head** | |
| Motor-driven pumps | 1,300 psig |
| Turbine-driven pump | 1,200 psig |

---

## 8. Interface with Condenser/Feedwater System

### 8.1 Normal Suction Path

```
Condensate Storage Tank
         │
         ▼
    AFW Pump Suction Header (gravity feed)
         │
    ┌────┴────┬─────────────┐
    ▼         ▼             ▼
Motor-DRV  Motor-DRV   Turbine-DRV
  Pump A     Pump B      Pump
    │         │             │
    ▼         ▼             ▼
  SG-1,2    SG-3,4      All 4 SGs
```

### 8.2 CST Level Protection

**Minimum Volume Reserved for AFW:**
- Tech Spec requirement ensures water available for:
  - Hot standby (2 hours)
  - Cooldown to 350°F (4 hours)

**Protection Methods:**
1. Standpipe in condenser supply line
2. Level-actuated isolation valve
3. Elevated side tap on CST

### 8.3 Return Path to Secondary Cycle

When AFW operates:
1. CST water pumped to SGs
2. Water boils, creating steam
3. Steam dumped to condenser (if C-9 satisfied) OR atmosphere (relief valves)
4. If to condenser: Condensed in hotwell
5. Hotwell inventory available for condensate pumps (when restored)

**Mass Balance Note:**
- Steam dumped to atmosphere represents permanent mass loss
- Must be made up via CST refill
- Steam dumped to condenser recirculates (closed loop)

---

## References

- NRC HRTD Section 7.1 — Main and Auxiliary Steam
- NRC HRTD Section 7.2 — Condensate and Feedwater System
- NRC HRTD Section 11.2 — Steam Dump Control
- NRC HRTD Section 5.1 — Residual Heat Removal System
- NRC HRTD Section 12.3 — ESFAS

---
