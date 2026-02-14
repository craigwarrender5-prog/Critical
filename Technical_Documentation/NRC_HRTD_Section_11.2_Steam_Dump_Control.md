# NRC HRTD Section 11.2 — Steam Dump Control System

**Source:** https://www.nrc.gov/docs/ML1122/ML11223A294.pdf  
**Retrieved:** 2026-02-14  
**Revision:** Rev 0403

---

## Overview

This document provides comprehensive technical details on the Westinghouse PWR Steam Dump Control System including:

- Steam pressure mode for startup/cooldown operations
- T_avg mode for load rejection and turbine trip response
- Arming signals and interlocks
- Trip-open bistables for rapid valve actuation
- Valve group sequencing and modulation
- No-load T_avg control at 557°F

---

## Key Technical Data for Simulator Development

### System Purpose

The steam dump system removes excess energy from the reactor coolant system by bypassing steam directly to the main condenser. The system:

1. Limits RCS temperature rise during load reductions exceeding rod control capability
2. Returns plant to no-load conditions after turbine trip without safety valve actuation
3. Controls steam pressure during startup and shutdown operations
4. Provides constant steam flow during turbine synchronization

### Design Capacity

**Typical System:**
- 12 steam dump valves total
- 40% of full-power steam flow capacity
- Allows 50% load rejection without reactor trip (40% dump + 10% rod control)
- Prevents SG safety valve lift after turbine trip from 100% power

**Single Valve Flow Limit:**
- Maximum 895,000 lb/hr per valve at 1106 psia
- Limits overcooling from accidental single valve opening

---

## Operating Modes

### Steam Pressure Mode

**Use Cases:**
- Plant startup and shutdown
- Hot standby operations
- Turbine synchronization
- Manual plant cooldown

**Control Philosophy:**
- Proportional-plus-integral (PI) controller
- Maintains steam pressure at operator-selected setpoint
- Typical setpoint: **1092 psig** → maintains T_avg at ~557°F (no-load)

**Operation During Startup:**
1. Mode selector switch placed in STEAM PRESSURE position
2. Operator selects desired steam pressure setpoint
3. As reactor power increases, steam pressure rises above setpoint
4. Controller modulates dump valves open to maintain setpoint
5. During turbine synchronization, dump valves close as governor valves open
6. Total steam flow remains constant → easier feedwater control

**Operation During Cooldown:**
1. Mode selector in STEAM PRESSURE, controller in AUTOMATIC
2. Operator lowers setpoint → valves open → pressure drops → T_avg drops
3. Or: Controller in MANUAL, operator directly controls valve position
4. Rate of cooldown controlled by valve opening demand

### T_avg Mode

**Use Cases:**
- Normal power operation (>15% power typically)
- Load rejection response
- Turbine trip response

**Two Controllers (automatically selected based on conditions):**

| Controller | Active When | Inputs | Function |
|------------|-------------|--------|----------|
| Loss-of-Load | Mode switch in T_avg, no turbine trip | T_avg - T_ref | Handle load reductions |
| Turbine-Trip | Turbine trip signal present | T_avg - No-load T_avg | Return to no-load after trip |

**Loss-of-Load Controller:**
- 5°F deadband to allow rod control system to respond first
- Opens valves when (T_avg - T_ref) > 5°F
- Valve opening proportional to temperature error above deadband
- System remains armed until manually reset

**Turbine-Trip Controller:**
- No deadband (immediate response needed)
- Opens valves to return T_avg to no-load setpoint (typically 557°F)
- Automatically selected when turbine trip signal received
- Remains armed until turbine is relatched

---

## Valve Configuration and Sequencing

**Valve Groups:**
| Group | Number of Valves | Function |
|-------|------------------|----------|
| 1 | 3 (cooldown valves) | First to open, last to close |
| 2 | 3 | Second to open |
| 3 | 3 | Third to open |
| 4 | 3 | Last to open, first to close |

**Sequencing:**
- Sequential opening: Group 1 fully open → Group 2 opens → etc.
- Reverse closing: Group 4 closes → Group 3 closes → etc.
- Each valve discharges to different condenser shell (3 shells)

**Valve Actuation:**
- Air-operated diaphragm valves
- Each valve has:
  - Valve positioner (converts I/P signal to control air)
  - Two series arming solenoid valves
  - Trip-open solenoid valve (bypasses positioner for rapid opening)

---

## Arming Signals

Steam dump valves will only operate when armed. Three arming signals exist:

| Signal | Mode | Source | Reset |
|--------|------|--------|-------|
| Steam Pressure Mode | Steam Pressure | Mode selector switch | Mode switch position |
| Loss-of-Load (C-7) | T_avg | Turbine impulse pressure | Manual reset required |
| Turbine Trip (C-8) | T_avg | Stop valves closed OR low EHC pressure | Turbine relatched |

**Loss-of-Load Signal (C-7) Actuation:**
- Ramp load decrease > 5%/min, OR
- Step load decrease > 10%
- Sensed from turbine first-stage impulse pressure
- Seals in until manually reset

**Turbine Trip Signal (C-8) Actuation:**
- 4/4 turbine stop valves closed, OR
- 2/3 low EHC trip header pressure (<800 psig for GE turbines)
- 2/3 low auto-stop oil pressure (<45 psig for Westinghouse turbines)
- Resets when turbine is relatched

---

## Interlocks

Three interlocks must be satisfied to allow steam dump operation:

### C-9: Condenser Available
**Requirements:**
- Condenser vacuum > 22 in. Hg (2/2 pressure switches)
- At least one circulating water pump running (1/2 breakers closed)

**Purpose:** Protect condenser from overpressure

**Actuation:** Opens at <22 in. Hg vacuum, closes (blocks dumps) at 7.6 in. Hg backpressure

### P-12: Low-Low T_avg
**Setpoint:** T_avg < 553°F (2/4 loops)

**Purpose:** Prevent inadvertent overcooling from instrument failure

**Operation:**
- Below 553°F, steam dumps interlocked off (except cooldown valves)
- Operator can bypass interlock for cooldown valves only (Group 1)
- Other 9 valves remain blocked below P-12

**Bypass:**
- Two bypass switches (one per protection train)
- Position: OFF/RESET → ON → BYPASS INTERLOCK (spring returns to ON)
- Allows cooldown below 553°F using Group 1 valves only

### Low-Low Steam Generator Water Level
**Setpoint:** <11.5% in any SG (2/3 channels, 1/4 SGs)

**Purpose:** Conserve secondary inventory for turbine-driven AFW pump

**Operation:**
- 5-minute time delay (allows for transients)
- Blocks 9 of 12 valves (all except cooldown group)
- Cooldown valves (Group 1) remain available

---

## Trip-Open Bistables

For rapid response to large load rejections, trip-open (blast-open) bistables bypass the normal modulating control:

**Setpoints:**

| Controller | High Bistable | High-High Bistable |
|------------|---------------|---------------------|
| Loss-of-Load | 10.7°F (T_avg - T_ref) | 16.4°F |
| Turbine-Trip | 13.8°F (T_avg - no-load) | 27.7°F |

**Operation:**
- High bistable trips → Groups 1 and 2 (6 valves) trip open via 100 psig air bypass
- High-high bistable trips → Groups 3 and 4 (6 more valves) trip open
- Valves open fully within 2-3 seconds
- When bistable resets, valves return to modulating control

**Purpose:**
- Large temperature errors indicate rapid power-load mismatch
- Fast valve opening limits RCS temperature excursion
- Bistables only active in T_avg mode

---

## Control System Architecture

**Signal Flow (Steam Pressure Mode):**
```
Steam Header Pressure → Steam Pressure Controller (PI) → I/P Converters → Valve Positioners → Dump Valves
                     ↑
              Operator Setpoint
```

**Signal Flow (T_avg Mode - Loss of Load):**
```
Auctioneered High T_avg ─┐
                         ├→ Loss-of-Load Controller → I/P Converters → Valves
T_ref (from P_imp) ──────┘
     (5°F deadband)
```

**Signal Flow (T_avg Mode - Turbine Trip):**
```
Auctioneered High T_avg ─┐
                         ├→ Turbine-Trip Controller → I/P Converters → Valves
No-Load T_avg Setpoint ──┘
     (no deadband)
```

---

## Critical Operating Parameters

| Parameter | Value | Notes |
|-----------|-------|-------|
| No-load steam pressure setpoint | 1092 psig | Corresponds to ~557°F T_sat |
| No-load T_avg | 557°F | Typical program value |
| Loss-of-load deadband | 5°F | Allows rod control first response |
| Full valve opening (pressure error) | 100 psid | All 4 groups open |
| Valve opening time (trip-open) | 2-3 seconds | Via 100 psig air bypass |
| P-12 interlock setpoint | 553°F | 2/4 loops |
| SG low-low level setpoint | 11.5% | 2/3 per SG, 5-min delay |
| C-7 loss-of-load trigger | >10% step or >5%/min ramp | Seals in |
| C-8 turbine trip trigger | 4/4 stop valves or 2/3 EHC pressure | Auto-resets on relatch |

---

## Critical Notes for Simulator

1. **HZP Stabilization:**
   - Steam dumps maintain no-load T_avg at 557°F
   - Setpoint of 1092 psig (steam pressure mode) achieves this
   - Small reactor power changes handled by modulating dump valves

2. **Mode Selection:**
   - Steam pressure mode for startup, shutdown, hot standby
   - T_avg mode for normal power operation
   - Automatic transition between controllers within T_avg mode

3. **Load Rejection Capability:**
   - 40% steam dump + 10% rod control = 50% load rejection capability
   - Larger transients may require reactor trip

4. **Safety Classification:**
   - Steam dump is NOT safety-related (control-grade)
   - Not required for safe shutdown
   - Not credited in FSAR accident analyses

5. **P-12 Bypass for Cooldown:**
   - Required to use steam dumps below 553°F
   - Only Group 1 (cooldown valves) available
   - Other groups remain blocked as protection against overcooling

---

## Implementation Priority for Simulator

**Phase 1 (HZP Stabilization) - CRITICAL:**
- Steam pressure mode controller
- 1092 psig setpoint → 557°F T_avg control
- Valve modulation response to steam pressure error
- Basic valve group sequencing

**Phase 2 (Power Operations):**
- T_avg mode controller selection logic
- Loss-of-load controller with 5°F deadband
- Turbine-trip controller
- C-7 and C-8 arming signal logic
- Trip-open bistables

**Phase 3 (Full Implementation):**
- All interlocks (C-9, P-12, SG low-low level)
- P-12 bypass capability
- Manual reset logic for C-7
- Detailed valve positioner dynamics

---

## References

This document should be referenced for:
- Steam dump control philosophy
- No-load T_avg maintenance
- Load rejection response
- Interlock logic and setpoints
- Controller tuning parameters
- Arming and reset logic

---
