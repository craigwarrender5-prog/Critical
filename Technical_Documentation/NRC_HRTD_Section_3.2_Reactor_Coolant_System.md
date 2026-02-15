# NRC HRTD Section 3.2 — Reactor Coolant System (Pressurizer Specifications)

**Source:** https://www.nrc.gov/docs/ML1122/ML11223A213.pdf  
**Retrieved:** 2026-02-15  
**Revision:** Rev 1203

---

## Pressurizer Design Parameters (Table 3.2-2)

### Physical Specifications — Westinghouse 4-Loop PWR

| Parameter | Value | Unit |
|-----------|-------|------|
| **Total Volume** | 1800 | ft³ |
| **Overall Height** | 52 ft, 9 in (16.1 m) | |
| **Diameter** | 7 ft, 8 in (2.3 m) | |
| **Design Pressure** | 2500 | psig |
| **Operating Pressure** | 2235 | psig |
| **Design Temperature** | 680 | °F |

### Heater Specifications

| Parameter | Value | Notes |
|-----------|-------|-------|
| **Total Heater Capacity** | 1794 kW | All heaters |
| **Number of Heaters** | 78 | Replaceable, direct-immersion, tubular-sheath type |
| **Proportional Heaters** | 18 heaters, 414 kW | Variable duty cycle (10-sec intervals) |
| **Backup Heaters** | 60 heaters, 1380 kW | Bistable control (on/off) |
| **Heatup Rate Capability** | ~55°F/hr | Pressurizer and contents |

### Heater Breakdown by Bank

| Bank | Type | Capacity | Control Mode | Quantity |
|------|------|----------|--------------|----------|
| C | Proportional | 414 kW | Variable (% of 10-sec interval) | 18 heaters |
| A | Backup | 460 kW | Bistable (on/off) | 20 heaters |
| B | Backup | 460 kW | Bistable (on/off) | 20 heaters |
| D | Backup | 460 kW | Bistable (on/off) | 20 heaters |

### Spray System Specifications

| Parameter | Value | Notes |
|-----------|-------|-------|
| **Maximum Spray Flow** | 840 gpm | Design maximum |
| **Maximum Spray Flow (alternate source)** | 900 gpm | Some references cite 900 gpm |
| **Continuous Bypass Flow** | 1 gpm | Thermal protection and chemistry control |
| **Number of Spray Valves** | 2 | One from each of two cold legs |
| **Spray Lines** | Loop 2 and Loop 3 | Cold leg connections |

### Volume Distribution at Normal Operating Conditions

| Parameter | Value | Notes |
|-----------|-------|-------|
| **Water Volume (at 60% level)** | ~1080 ft³ | Approximately 8075 gallons |
| **Steam Volume (at 60% level)** | ~720 ft³ | Remaining volume |
| **No-load Level Setpoint** | 25% | ~450 ft³ water |
| **Full-power Level Setpoint** | 61.5% | ~1107 ft³ water |

### Relief and Safety Valve Specifications (Table 3.2-4, 3.2-5)

#### Code Safety Valves (3 total)

| Parameter | Value |
|-----------|-------|
| Quantity | 3 |
| Type | Spring-loaded, pop-open, self-actuating |
| Set Pressure | 2485 psig |
| Total Capacity | Sufficient for 110% of design heat load |
| Design Accumulation | +10% (2750 psig maximum) |

#### Power-Operated Relief Valves (2 total)

| PORV | Set Pressure | Actuation Logic | Notes |
|------|--------------|-----------------|-------|
| PCV-456 | 2335 psig | Fixed bistable (Channel II or IV) + Interlock (Channel III) | |
| PCV-455A | Controller output | Master controller (100 psi error) + Interlock (Channel IV) | ~2335 psig |

### Pressurizer Relief Tank (PRT) — Table 3.2-6

| Parameter | Value | Notes |
|-----------|-------|-------|
| **Design Pressure** | 100 psig | With rupture discs at 100 psig |
| **Normal Operating Pressure** | ~3 psig | Nitrogen overpressure |
| **Water Volume** | ~75% full | Near containment ambient temperature (~120°F) |
| **Design Relief Volume** | 110% of volume above 25% level | Can absorb design discharge |
| **Final Temperature After Design Discharge** | 200°F | From initial 120°F |
| **Cooldown Time** | ~1 hour | 200°F → 120°F via spray |
| **Cover Gas** | Nitrogen | Prevents hydrogen accumulation |

---

## Component Descriptions (from NRC HRTD 3.2.3.3)

### Pressurizer Construction

The pressurizer is a vertical, cylindrical vessel with:
- Hemispherical top and bottom heads
- Constructed of manganese-molybdenum alloy steel
- All surfaces exposed to reactor coolant clad with austenitic stainless steel
- Electrical heaters (78) installed through the bottom head
- Spray nozzle, relief, and safety valve connections in the top head

### Four Basic Functions

1. **Pressurizing the RCS during plant heatup**
   - Heaters raise temperature to saturation
   - Steam formation increases pressure

2. **Maintaining normal RCS pressure during steady-state operations**
   - Proportional heaters compensate for ambient heat losses
   - Bypass spray flow (~1 gpm) for chemistry control

3. **Limiting pressure changes during RCS transients**
   - Spray valves for pressure increases
   - Backup heaters for pressure decreases
   - PORVs for overpressure protection

4. **Preventing RCS pressure from exceeding design pressure**
   - PORVs open at 2335 psig
   - Code safety valves open at 2485 psig
   - Design pressure: 2500 psig
   - Maximum allowable: 2750 psig (with 10% accumulation)

### Pressure Control Mechanism

At normal operating conditions:
- ~60% of volume is saturated water
- ~40% of volume is saturated steam
- Maintained at saturation conditions by electrical heaters
- Connected to loop hot leg via surge line
- Since RCS is hydraulically solid, pressurizer pressure = RCS pressure

**Volume Change Effect:**
- Boiling 1 ft³ of water → 6 ft³ of steam (factor of 6 density change)
- Condensing 6 ft³ of steam → 1 ft³ of water
- Steam behaves like ideal gas: pressure ∝ density

### Volume Requirements

The 1800 ft³ volume satisfies all of the following:

1. **Steam + water volume** sufficient for pressure response to programmed system volume changes
2. **Water volume** prevents heater uncovering during 10% step load increase
3. **Steam volume** accommodates 50% load reduction insurge without reaching high level trip (92%)
4. **Doesn't empty** after reactor trip + turbine trip
5. **Prevents water relief** through safety valves after loss of load + high level trip
6. **No SI actuation** following reactor trip + turbine trip

### Heater Details

**Electrical Heaters:**
- Replaceable, direct-immersion, tubular-sheath type
- Hermetically sealed terminals (retain full system pressure if sheath fails)
- Located in lower portion of vessel
- Maintain steam and water at equilibrium conditions
- Ventilation via holes drilled in pressurizer support skirt

**Control Characteristics:**
- **Proportional heaters (Bank C):** Energized for variable percentage of each 10-second interval
  - At 2235 psig setpoint: Energized ~5-15% of time to compensate for losses
  - Linear modulation between 2220 psig (100% on) and 2250 psig (0% on)
  
- **Backup heaters (Banks A, B, D):** Bistable control (fully on or fully off)
  - Energize at 2210 psig (-25 psi from setpoint)
  - De-energize at 2217 psig (-18 psi from setpoint)
  - Hysteresis prevents chattering

### Spray System Details

**Design:**
- Spray water injected through nozzle in top of vessel
- Two automatically controlled, air-operated valves with remote manual override
- Manual throttle valve in parallel with each spray valve (bypass flow)
- Temperature sensors with low temperature alarms on each spray line
- Piping layout forms water seal to prevent steam buildup

**Bypass Flow Purpose:**
- Reduces thermal stresses when spray valves open
- Maintains uniform water chemistry with RCS
- Continuous 1 gpm through each bypass

**Spray Flow Capacity:**
- Maximum 840 gpm (420 gpm per valve)
- Selected to prevent PORV actuation after 10% step load decrease
- Uses ΔP between hot leg (surge line) and cold leg (spray line) as driving force
- Scoop design adds velocity head to differential pressure

**Spray Line Connections:**
- Extend into cold leg piping as "scoop"
- Velocity head of RCS flow adds to spray driving force
- Redundant valves allow spray with one RCP not operating

**Auxiliary Spray:**
- CVCS connection to spray line
- Provides spray during cooldown with RCPs not operating
- Thermal sleeve designed for cold auxiliary spray water

### Surge Line

- Connects bottom of pressurizer to one RCS hot leg
- Sized to limit pressure drop during maximum insurge
- Ensures highest RCS pressure ≤ design pressure during safety valve discharge
- Thermal sleeve at pressurizer end for thermal stress protection
- Temperature sensor with low temperature alarm and indication

### Water Seal Loop Seals

**Code Safety Valves:**
- 6-inch pipes connecting pressurizer nozzles to safety valves shaped as loop seals
- Condensate from normal heat losses accumulates and floods valve seat
- Water seal prevents steam/hydrogen leakage past valve seats
- If pressure exceeds setpoint, water slug discharges during accumulation period
- Slug diversion devices (SDDs) installed downstream to trap water slugs

**Instrumentation:**
- Temperature indicator in discharge manifold (alerts to steam passage)
- Acoustic monitors on each valve (positive indication of leakage/operation)

### PORV Details

**Design:**
- Two PORVs limit RCS pressure and minimize high pressure reactor trip actuation
- Minimize code safety valve operation
- Air operated, can be opened/closed automatically or by remote manual control
- Backup air supply for 10 minutes following loss of instrument air

**Control Logic:**
- PCV-456: Fixed bistable at 2335 psig (Channel II or IV) + Interlock (Channel III)
- PCV-455A: Master controller output (100 psi error) + Interlock (Channel IV)
- Remotely operated block valves isolate PORVs if excessive leakage occurs

**Performance:**
- Designed to limit pressure below high pressure trip setpoint (2385 psig)
- Handles design transients up to 50% step load decrease with full steam dump
- Acoustic monitor detects leakage and/or valve opening
- Also utilized for cold overpressure mitigation with additional actuation logic

---

## Pressure Control Setpoints Summary

| Function | Pressure (psig) | Error from 2235 psig | Coincidence | Notes |
|----------|----------------|---------------------|-------------|-------|
| **Proportional heaters 100% on** | 2220 | -15 | N/A | Continuously energized |
| **Backup heaters energize** | 2210 | -25 | N/A | All three banks |
| **Backup heaters de-energize** | 2217 | -18 | N/A | 7 psi hysteresis |
| **Normal setpoint** | 2235 | 0 | N/A | Operator adjustable 1700-2500 |
| **Proportional heaters 0% on** | 2250 | +15 | N/A | De-energized |
| **Spray valves start opening** | 2260 | +25 | N/A | Linear modulation begins |
| **Spray valves fully open** | 2310 | +75 | N/A | Maximum spray (840 gpm) |
| **PORV PCV-456 opens** | 2335 | +100 | Channel II or IV + Interlock III | Fixed bistable |
| **PORV PCV-455A opens** | ~2335 | +100 | Channel IV interlock + controller | Master controller output |
| **High pressure reactor trip** | 2385 | +150 | 2/4 | RCPB protection |
| **Code safety valves lift** | 2485 | +250 | N/A | 10% accumulation = 2750 psig max |
| **Low pressure reactor trip** | 1865 | -370 | 2/4 | DNB protection, active > P-7 |
| **P-11 block permissive** | 1915 | -320 | 2/3 | Allows normal cooldown |
| **Low pressure SI actuation** | 1807 | -428 | 2/3 | LOCA protection |

### Cold Overpressure Protection System (COPS) Setpoints

| Function | Pressure (psig) | Transmitter | Notes |
|----------|----------------|-------------|-------|
| **Operator unblocks COPS** | < 375 | Per procedures | RCS T_cold < specified value |
| **COPS alarm** | 400 | | Control room alarm |
| **PCV-455A opens (COPS)** | 425 | PT-403 | Lower setpoint |
| **PCV-456 opens (COPS)** | 475 | PT-405 | Higher setpoint |

---

## Material and Construction Standards

### Materials of Construction

- **Vessel shell:** Manganese-molybdenum alloy steel
- **Internal cladding:** Austenitic stainless steel (all surfaces in contact with coolant)
- **Heaters:** Tubular-sheath with hermetically sealed terminals
- **Spray line and thermal sleeves:** Austenitic stainless steel
- **Surge line:** Austenitic stainless steel with thermal sleeve at pressurizer end

### ASME Code Compliance

- Designed per ASME Boiler and Pressure Vessel Code, Section III
- Code safety valves prevent pressure > 110% of design pressure
- Hydrostatic test pressure: 3125 psig (1.25 × design pressure)
- Seismic Category I designation

---

## Thermal and Hydraulic Performance Data

### Steady-State Heat Losses

- Ambient heat losses compensated by proportional heaters
- Typical steady-state heater duty: 5-15% of proportional capacity (~20-60 kW)
- Continuous bypass spray: 1 gpm per line (2 gpm total) removes ~50 kW

### Transient Response

**Insurge (RCS temperature increase):**
1. Coolant expands → Water surges into pressurizer
2. Steam bubble compresses → Pressure increases
3. If P > 2260 psig → Spray valves modulate open
4. Cool spray water condenses steam → Pressure decrease

**Outsurge (RCS temperature decrease):**
1. Coolant contracts → Water surges out of pressurizer
2. Steam bubble expands → Pressure decreases
3. Saturated water flashes to steam (maintains some pressure)
4. If P < 2210 psig → Backup heaters energize
5. Heaters boil water → Steam generation → Pressure increase

### Design Transients

| Transient | Pressurizer Response | Control Action | Result |
|-----------|---------------------|----------------|--------|
| 10% step load increase | Outsurge, level drops | Backup heaters energize | Heaters prevent uncovering |
| 10% step load decrease | Insurge, level rises | Spray valves modulate | Prevents PORV actuation |
| 50% step load decrease | Large insurge | Full spray + automatic rod control | Level < 92% trip setpoint |
| Reactor trip from 100% | Large outsurge | Heaters energize | Pressurizer doesn't empty |
| Reactor + turbine trip | Moderate outsurge | Heaters energize | No SI actuation |
| Loss of load from 100% | Large insurge | High level trip + spray | No water relief through safeties |

---

## References

- NRC HRTD Section 10.2 — Pressurizer Pressure Control System (ML11223A287)
- NRC HRTD Section 10.3 — Pressurizer Level Control System (ML11223A290)
- NRC HRTD Section 12.2 — Reactor Protection System
- ASME Boiler and Pressure Vessel Code, Section III
- IEEE 279-1979 — Criteria for Protection Systems for Nuclear Power Generating Stations

---

## Simulator Implementation Notes

### Critical Design Parameters for Simulator

1. **Volume:** 1800 ft³ (50.96 m³)
2. **Heater capacity:** 1794 kW total (414 kW proportional + 1380 kW backup)
3. **Spray capacity:** 840 gpm maximum (420 gpm per valve)
4. **Level range:** 25% (no-load) to 61.5% (full-power) programmed
5. **Normal operating level:** 60% at full power
6. **Pressure setpoint:** 2235 psig (adjustable 1700-2500 psig)

### Control System Characteristics

1. **Master pressure controller:** PID (Proportional + Integral + Derivative)
2. **Master level controller:** PI (Proportional + Integral)
3. **Heater control:** Proportional (variable duty cycle) + Backup (bistable)
4. **Spray control:** Linear modulation between 2260-2310 psig
5. **PORV control:** Bistable with interlocks + Master controller output

### Instrumentation Requirements

1. **Pressure transmitters:** 4 channels (Channels I, II, III, IV)
2. **Level transmitters:** 3 hot-calibrated + 1 cold-calibrated
3. **Temperature sensors:** Surge line, spray lines, discharge manifold
4. **Position indicators:** PORV stem position switches
5. **Acoustic monitors:** Safety valves and PORVs

---

*Document compiled 2026-02-15 from NRC HRTD Section 3.2, Rev 1203*
