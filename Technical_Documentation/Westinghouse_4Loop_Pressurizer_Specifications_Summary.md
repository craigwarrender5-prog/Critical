# Westinghouse 4-Loop PWR Pressurizer — Technical Specifications Summary

**Compiled:** 2026-02-15  
**Sources:** NRC HRTD Sections 3.2, 10.2, 10.3, ScienceDirect Technical References  
**Purpose:** Quick reference for simulator development

---

## Physical Characteristics

| Specification | Value | Source |
|--------------|-------|--------|
| **Total Volume** | 1800 ft³ (50.96 m³) | NRC HRTD 3.2-2 |
| **Height** | 52 ft 9 in (16.1 m) | ScienceDirect |
| **Diameter** | 7 ft 8 in (2.3 m) | ScienceDirect |
| **Design Pressure** | 2500 psig | NRC HRTD 3.2-2 |
| **Operating Pressure** | 2235 psig | NRC HRTD 10.2 |
| **Design Temperature** | 680°F | NRC HRTD 3.2-2 |

---

## Electrical Heater System

### Total Capacity: 1794 kW

| Bank | Type | Quantity | Capacity | Control Mode |
|------|------|----------|----------|--------------|
| **C** | Proportional | 18 heaters | 414 kW | Variable duty cycle (10-sec intervals) |
| **A** | Backup | 20 heaters | 460 kW | Bistable (on/off) |
| **B** | Backup | 20 heaters | 460 kW | Bistable (on/off) |
| **D** | Backup | 20 heaters | 460 kW | Bistable (on/off) |

**Heatup Rate:** ~55°F/hr (pressurizer and contents from ambient)

---

## Spray System

| Parameter | Value |
|-----------|-------|
| **Maximum Spray Flow** | 840 gpm (420 gpm per valve) |
| **Number of Spray Valves** | 2 (from Loop 2 and Loop 3 cold legs) |
| **Continuous Bypass Flow** | 1 gpm per valve (2 gpm total) |
| **Spray Valve Actuation** | Linear modulation 2260-2310 psig |

---

## Relief and Safety Valves

### Power-Operated Relief Valves (2 total)

| Valve | Opening Logic | Setpoint |
|-------|---------------|----------|
| **PCV-456** | Fixed bistable (Ch II or IV) + Interlock (Ch III) | 2335 psig |
| **PCV-455A** | Master controller output + Interlock (Ch IV) | ~2335 psig (100 psi error) |

### Code Safety Valves (3 total)

| Parameter | Value |
|-----------|-------|
| **Set Pressure** | 2485 psig |
| **Design Accumulation** | +10% (max 2750 psig) |
| **Total Capacity** | Sufficient for 110% of design heat load |

---

## Normal Operating Conditions

| Parameter | Value | Notes |
|-----------|-------|-------|
| **Water Volume (60% level)** | ~1080 ft³ (8075 gallons) | Full power condition |
| **Steam Volume (60% level)** | ~720 ft³ | Full power condition |
| **No-Load Level** | 25% (~450 ft³ water) | T_avg = 557°F |
| **Full-Power Level** | 61.5% (~1107 ft³ water) | T_avg = 584.7°F |
| **Pressure Setpoint** | 2235 psig | Adjustable 1700-2500 psig |

---

## Pressure Control Setpoints (from 2235 psig setpoint)

| Event | Pressure | Error | Action |
|-------|----------|-------|--------|
| Backup heaters ON | 2210 psig | -25 psi | All three backup banks energize |
| Backup heaters OFF | 2217 psig | -18 psi | 7 psi hysteresis |
| Proportional 100% | 2220 psig | -15 psi | Continuously energized |
| **Normal Setpoint** | **2235 psig** | **0** | **Operator adjustable** |
| Proportional 0% | 2250 psig | +15 psi | De-energized |
| Spray valves open | 2260 psig | +25 psi | Begin linear modulation |
| Spray valves full open | 2310 psig | +75 psi | Maximum 840 gpm |
| PORVs open | 2335 psig | +100 psi | Both valves (with interlocks) |
| High pressure trip | 2385 psig | +150 psi | 2/4 coincidence |
| Code safeties lift | 2485 psig | +250 psi | Final overpressure protection |

---

## Cold Overpressure Protection System (COPS)

| Event | Pressure | Notes |
|-------|----------|-------|
| Operator unblocks | < 375 psig | When T_cold < specified value |
| COPS alarm | 400 psig | Control room indication |
| PCV-455A opens | 425 psig | Lower COPS setpoint |
| PCV-456 opens | 475 psig | Higher COPS setpoint |

---

## Level Control Setpoints

| Setpoint | Level | Function |
|----------|-------|----------|
| Low level isolation | 17% | Letdown isolation, heater cutoff |
| No-load program | 25% | T_avg = 557°F |
| Full-power program | 61.5% | T_avg = 584.7°F |
| Backup heater enable | Program + 5% | Anticipatory control |
| High level alarm | 70% | Alarm only |
| High level trip | 92% | 2/3 coincidence, at-power (P-7) |

**Level Program Equation:**
```
Level_program(%) = 25 + [(T_avg - 557) / (584.7 - 557)] × 36.5
Level_program(%) = 25 + [(T_avg - 557) / 27.7] × 36.5
```

---

## Protection System Setpoints

| Protection Function | Setpoint | Coincidence | Notes |
|-------------------|----------|-------------|-------|
| High pressure trip | 2385 psig | 2/4 | RCPB protection |
| Low pressure trip | 1865 psig | 2/4 | DNB protection, active > P-7 |
| Low pressure SI | 1807 psig | 2/3 | LOCA protection |
| P-11 permissive | 1915 psig | 2/3 | ESF actuation block |
| High level trip | 92% | 2/3 | Solid pressurizer protection, active > P-7 |

---

## Key Design Transients

| Transient | Pressurizer Action | Control Response |
|-----------|-------------------|------------------|
| 10% step load increase | Outsurge, level drops | Heaters prevent uncovering |
| 10% step load decrease | Insurge, level rises | Spray prevents PORV lift |
| 50% step load decrease | Large insurge | Spray + rod control, level < 92% |
| Reactor trip (100%) | Large outsurge | Heaters prevent emptying |
| Reactor + turbine trip | Moderate outsurge | No SI actuation |
| Loss of load (100%) | Large insurge | High level trip, no water relief |

---

## Critical Implementation Notes for Simulator

### Must-Have Features

1. **Saturation conditions maintained:** Steam and water always at T_sat for given pressure
2. **Volume factor of 6:** Boiling 1 ft³ water → 6 ft³ steam; condensing reverses
3. **PID pressure control:** Proportional + Integral + Derivative for master controller
4. **PI level control:** Proportional + Integral for charging flow control
5. **10-second heater intervals:** Proportional heaters cycle on/off within 10-sec windows
6. **Spray driving force:** ΔP = P_hot_leg - P_cold_leg + velocity head from scoop design
7. **Bypass spray continuous:** 1 gpm per valve for thermal protection and chemistry

### Physical Constraints

1. **Total volume fixed:** 1800 ft³ cannot be exceeded
2. **Heater uncovering:** Heaters must be below water level or damage occurs
3. **Going solid:** If level reaches 100%, RCS pressure rises uncontrollably
4. **Steam space required:** Minimum steam volume needed for pressure control authority
5. **Surge line sizing:** Limits pressure drop during insurge transients

### Control System Logic

1. **Heater energization sequence:**
   - Proportional heaters modulate first (2220-2250 psig)
   - If pressure continues dropping, backup heaters energize at 2210 psig
   - All three backup banks energize simultaneously (no staging)

2. **Spray actuation sequence:**
   - Spray valves begin opening at 2260 psig
   - Linear modulation to full open at 2310 psig
   - If spray insufficient, PORVs open at 2335 psig

3. **Level-based heater control:**
   - Low level (17%): ALL heaters OFF (protection from steam exposure)
   - High level (Program + 5%): Backup heaters ON (anticipatory for load decrease)

### Instrumentation Channels

1. **Pressure:** 4 independent channels (I, II, III, IV) for control and protection
2. **Level:** 3 hot-calibrated channels + 1 cold-calibrated channel
3. **Channel selection:** Operator can select which channels used for control
4. **Redundancy:** Protection uses 2/3 or 2/4 coincidence logic

---

## Material Construction

| Component | Material |
|-----------|----------|
| Vessel shell | Manganese-molybdenum alloy steel |
| Internal cladding | Austenitic stainless steel |
| Heater sheaths | Hermetically sealed tubular elements |
| Spray lines | Austenitic stainless steel with thermal sleeves |
| Surge line | Austenitic stainless steel with thermal sleeve |

---

## References

1. NRC HRTD Section 3.2 — Reactor Coolant System, Rev 1203 (ML11223A213)
2. NRC HRTD Section 10.2 — Pressurizer Pressure Control, Rev 1208 (ML11223A287)
3. NRC HRTD Section 10.3 — Pressurizer Level Control, Rev 0502 (ML11223A290)
4. ScienceDirect — "Pressuriser" Technical Topics
5. ASME Boiler and Pressure Vessel Code, Section III

---

*Quick reference compiled 2026-02-15 for simulator development*
