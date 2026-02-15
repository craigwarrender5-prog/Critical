# NRC HRTD Section 4.1 — Chemical and Volume Control System (CVCS)

**Source:** https://www.nrc.gov/docs/ML1122/ML11223A214.pdf  
**Retrieved:** 2026-02-15  
**Revision:** Rev 1208

---

## Overview

The Chemical and Volume Control System (CVCS) is a Seismic Category I system that performs critical functions for RCS chemistry control, inventory management, and emergency core cooling.

---

## System Purposes

1. **Adjust reactor coolant boric acid concentration**
2. **Maintain proper water inventory in the RCS** (with pressurizer level control)
3. **Provide seal water flow to RCP shaft seals**
4. **Add corrosion inhibiting chemicals** to reactor coolant
5. **Purify the reactor coolant** to maintain design activity limits
6. **Provide borated water for emergency core cooling**
7. **Process reactor coolant** for reuse in boron recovery system
8. **Degasify the reactor coolant**
9. **Provide emergency boration** capability

---

## System Description

### Major Flow Paths

#### 1. Letdown Path

**Flow Sequence:**
1. Loop 3 cold leg (intermediate section)
2. Two series letdown isolation valves (LCV-459, LCV-460)
3. Delay pipe (N-16 decay)
4. Regenerative heat exchanger (shell side)
5. Letdown orifice(s) — pressure reduction
6. Containment isolation valve (CV-8152)
7. Letdown heat exchanger — final cooling
8. Letdown pressure control valve (PCV-131) — maintains 340 psig
9. Mixed-bed demineralizers — ionic purification
10. Letdown filter — resin fines removal
11. Volume control tank (VCT)

**Key Functions:**
- **Regenerative HX:** Cools letdown from 550°F to 290°F, preheats charging from 130°F to 500°F
- **Delay pipe:** Allows N-16 decay before exiting containment
- **Orifices:** Control letdown flow rate (2 × 75 gpm, 1 × 45 gpm at 2235 psig)
- **PCV-131:** Prevents flashing upstream of letdown HX (maintains 340 psig)
- **Demineralizers:** Remove ionic impurities (mixed anion/cation resins)

#### 2. Charging Path

**Flow Sequence:**
1. Volume control tank (VCT)
2. Charging pump suction
3. Charging pumps (2 centrifugal + 1 positive displacement)
4. Seal injection header (via HCV-182)
5. RCP seal injection filters (5 micron)
6. Individual RCP seal injection lines (8 gpm each, 32 gpm total)
7. Charging header (via HCV-182)
8. Charging isolation valves (MO-8105, MO-8106)
9. Regenerative heat exchanger (tube side)
10. Normal charging to Loop 1 (CV-8146) OR
11. Alternate charging to Loop 4 (CV-8147) OR
12. Auxiliary spray to pressurizer (CV-8145)

**Flow Control:**
- **Centrifugal pump:** FCV-121 modulates flow (master level controller output)
- **Positive displacement pump:** Variable speed control
- **HCV-182:** Divides flow between seal injection and charging

#### 3. Excess Letdown Path

**Flow Sequence:**
1. Loop 3 cold leg
2. Excess letdown heat exchanger
3. Isolation valves (CV-8153, CV-8154)
4. Control valve (HCV-123)
5. Three-way valve (CV-8143) → CVCS or RCDT

**Capacity:** 20 gpm at full RCS pressure (matches RCP seal leakage to RCS)

**Usage:**
- Low RCS pressure (orifice flow inadequate)
- Normal letdown out of service
- Supplemental letdown during final heatup stages

---

## Normal Flow Balance

### At 2235 psig, Steady-State

| Path | Flow | Notes |
|------|------|-------|
| **Charging pump discharge** | 87 gpm | Total from one CCP |
| Normal charging to Loop 1 | 55 gpm | Via CV-8146 |
| Seal injection to RCPs | 32 gpm | 8 gpm × 4 RCPs |
| Seal return to RCS (via RCP hydraulics) | 20 gpm | 5 gpm × 4 RCPs |
| **Total to RCS** | **75 gpm** | 55 + 20 |
| Seal leakoff to CVCS | 12 gpm | 3 gpm × 4 RCPs |
| **Letdown from RCS** | **75 gpm** | One 75-gpm orifice |
| **Net RCS flow** | **0 gpm** | Balanced |

### VCT Flow Balance

| Into VCT | Flow | Out of VCT | Flow |
|----------|------|------------|------|
| Letdown | 75 gpm | To charging pumps | 87 gpm |
| Seal return | 12 gpm | | |
| **Total In** | **87 gpm** | **Total Out** | **87 gpm** |

**Note:** Actual seal leakoff is slightly less than 12 gpm due to 3 gph/RCP diversion to liquid waste. This small imbalance requires periodic automatic makeup.

---

## Component Descriptions

### Letdown Components

#### Letdown Isolation Valves (LCV-459, LCV-460)

**Function:** Isolate letdown on low pressurizer level (17%)

**Interlock:** Cannot open/close unless all letdown orifice isolation valves closed (prevents letdown piping depressurization)

#### Regenerative Heat Exchanger

**Type:** Tube-and-shell, stainless steel  
**Configuration:** Letdown on shell side, charging through tubes  
**Performance:**
- Letdown: 550°F → 290°F
- Charging: 130°F → 500°F

**Purpose:**
- Energy conservation
- Minimize thermal stress on charging nozzles

#### Letdown Orifices

**Configuration:**
- Two 75-gpm orifices (at 2235 psig)
- One 45-gpm orifice

**Normal Operation:** One 75-gpm orifice in service

**Orifice Isolation Valve Interlocks:**
1. Close on low pressurizer level (17%)
2. Close on Containment Isolation Phase A
3. Require at least one charging pump running to open
4. Require letdown isolation valves (LCV-459, LCV-460) open before opening

**Sequencing:** Start charging pump → Open LCV-459/460 → Open orifice isolation valve

#### Letdown Heat Exchanger

**Type:** Tube-and-shell  
**Configuration:** Letdown through stainless steel tubes, CCW through carbon steel shell  
**Control:** Modulating CCW outlet valve controls letdown outlet temperature  
**Setpoint:** 120°F (normal)

#### Letdown Pressure Control Valve (PCV-131)

**Function:** Maintain 340 psig downstream of letdown heat exchanger

**Purpose:** Prevent flashing to steam before adequate cooling

**Control:** PID controller with adjustable setpoint (normally 340 psig)

#### Temperature Divert Valve (TCV-129)

**Function:** Protect demineralizer resins from high temperature

**Positions:**
- Normal: Letdown flows to demineralizers (T < 137°F)
- Divert: Letdown bypasses demineralizers (T ≥ 137°F)

**Control:** Automatic on temperature, manual override available

#### Mixed-Bed Demineralizers (2 total)

**Capacity:** 30 ft³ resin per vessel  
**Type:** Mixed anion and cation resins  
**Minimum Decontamination Factor:** 10  
**Operation:** One in service, one in standby  
**Resin Types:** Li-OH or H-OH

**Function:** Remove ionic impurities

#### Cation Demineralizer

**Function:**
- Remove excess lithium (from B-10 neutron reaction)
- Remove fission products (especially cesium) if fuel clad failure

**Type:** Cation-only resin

#### Letdown Overpressure Protection

**Two relief valves:**

| Relief Valve | Location | Setpoint | Capacity | Discharge |
|--------------|----------|----------|----------|-----------|
| 1 | Downstream of orifices | 600 psig | 195 gpm | PRT |
| 2 | Downstream of PCV-131 | 285 psig | 195 gpm | VCT |

---

### Volume Control Tank (VCT)

**Functions:**
1. Assists pressurizer with RCS volume changes
2. Interface with reactor makeup system
3. Hydrogen addition to RCS
4. RCS degasification

**Normal Pressure:** 15-75 psig (hydrogen overpressure)

**Hydrogen Addition:**
- Letdown enters via spray nozzle
- Hydrogen dissolves into water
- Returns to RCS via charging
- Scavenges oxygen in reactor core (radiation-induced H₂ + O₂ → H₂O)

**Penetrations:**
1. Nitrogen supply (shares penetration with H₂) — for purging during shutdown
2. VCT vent — to waste gas system (closes at 15 psig to maintain NPSH)
3. VCT relief valve — 75 psig setpoint, discharges to holdup tanks
4. RCP seal return line
5. Letdown inlet (spray nozzle)
6. Outlet to charging pump suction

**Degasification Process:**
1. Open VCT vent to waste gas system
2. Pressure drops, dissolved gases come out of solution
3. Close vent at 15 psig
4. Raise VCT level to compress gases
5. Repeat until desired gas concentration achieved

**Level Control Functions:**
- **Begin divert setpoint:** LCV-112A starts diverting letdown to BRS holdup tanks
- **High level alarm:** Backup controller diverts all letdown to BRS
- **Low level (automatic makeup):** Starts RMS in automatic mode
- **Low-low level:** Closes VCT outlet valves, opens RWST suction valves

---

### Reactor Makeup System (RMS)

**Function:** Supply boric acid, demineralized water, or blended mixture to VCT or charging pump suction

#### Storage Tanks

**Primary Water Storage Tank:**
- Capacity: 203,000 gallons
- Provides demineralized water
- Can be filled from secondary makeup or boric acid evaporators

**Boric Acid Tanks (2 total):**
- Capacity: 24,228 gallons each
- Concentration: ~4 wt% (7000 ppm)
- Room temperature maintained > 65°F (no heat tracing required)
- **Tech Spec Minimum:** 15,900 gallons of 7000 ppm (for 1.0% Δk/k shutdown margin)

**Boric Acid Batch Tank:**
- For mixing makeup boric acid solution
- Process: Heat water > saturation temp → Add crystals → Agitate → Transfer

#### Boric Acid Transfer Pumps (2 total)

**Functions:**
- Transfer boric acid to charging pump suction
- Transfer batch tank contents to boric acid tanks
- Recirculate boric acid tanks (prevent stratification)

**Auto-start:** Emergency boration requires manual valve opening (MO-8104) and pump start

#### Reactor Makeup Control — Operating Modes

**1. BORATE**

**Purpose:** Increase RCS boron concentration

**Process:**
- Set boric acid quantity (batch integrator)
- Set boric acid flow rate (flow controller)
- Select START
- Pumps auto-start, boric acid flows to charging pump suction via blender
- Auto-stops when batch complete

**2. DILUTE**

**Purpose:** Decrease RCS boron concentration

**Process:**
- Set water quantity (batch integrator)
- Set primary water flow rate (flow controller)
- Select START
- Primary water pump auto-starts, water flows to VCT via blender
- Auto-stops when batch complete

**3. ALTERNATE DILUTE**

**Purpose:** Faster dilution effect

**Difference from Dilute:** Primary water flows to both VCT AND charging pump suction

**Caution:** Bypass VCT reduces hydrogen absorption → risk of high RCS oxygen

**4. AUTOMATIC**

**Purpose:** Maintain RCS inventory and boron concentration during normal operation

**Setpoints:**
- Boric acid flow: Set to match RCS boron concentration
- Total blended flow: 80 gpm

**Initiation:** Low VCT level signal

**Termination:** High VCT level signal

**Process:**
- Both boric acid pumps start
- One primary water pump starts
- FCV-110A positions for set boric acid flow
- FCV-111A positions for total flow = 80 gpm
- Primary water flow = 80 gpm - boric acid flow
- Blended flow to charging pump suction

**5. MANUAL**

**Purpose:** Operator-controlled boration/dilution to any destination

**Destinations:**
- CVCS
- RWST
- Holdup tanks
- Other (via temporary connections)

**Note:** Automatic makeup disabled in manual mode

#### Boric Acid Blender

**Design:** Pipe tee with perforated tube insert

**Function:** Mix boric acid and primary water

**Flow:**
- Boric acid through perforated tube
- Primary water enters bottom
- Blended flow exits top

**Limit:** 10 gpm boric acid maximum (for 12 wt% systems)

#### Emergency Boration Path

**Purpose:** Rapid boric acid addition for:
- ATWS (Anticipated Transient Without Scram)
- Stuck rod conditions
- Inadequate shutdown boron concentration

**Primary Path:**
- Motor-operated valve MO-8104
- Flow transmitter
- Direct to charging pump suction

**Alternate Path:** FCV-110A + local manual valve 8439

**Operation:** Start boric acid pump, throttle MO-8104 to desired flow

**Note:** Delivery rate limited by charging pump flow capacity

#### Chemical Addition

**Chemicals Added:**

**Lithium Hydroxide (LiOH):**
- **Purpose:** pH control (maintain pH 6.9-7.4)
- **Reason:** Minimize corrosion, control chemistry

**Hydrogen (H₂):**
- **Purpose:** Oxygen scavenging
- **Reason:** Prevent CRUD formation, maintain reducing environment
- **Addition point:** VCT gas space

**Hydrazine (N₂H₄):**
- **Purpose:** Oxygen scavenging during cold shutdown
- **Reason:** Alternative to H₂ when RCS temperature < saturation

**Addition Method:**
- Chemical mixing tank
- Primary water flush to charging pump suction

---

### Charging Components

#### Charging Pumps (3 total)

**Configuration:**
- 2 × Single-speed centrifugal (vital AC power)
- 1 × Positive displacement with variable speed drive (nonvital AC)

**Materials:** Stainless steel (all wetted parts)

**Flow Control:**
- **Centrifugal:** FCV-121 modulates (controlled by pressurizer level controller)
- **Positive displacement:** Variable pump speed

**Protection:**
- Minimum flow recirculation (centrifugal pumps)
- Relief valve to VCT (positive displacement pump)

**ECCS Function:** Centrifugal charging pumps = High head safety injection pumps

#### RCP Seal Flow Control Valve (HCV-182)

**Function:** Divide flow between seal injection and charging

**Adjustment:** Remote control from control room

**Effect:**
- Throttle closed → Increase seal injection, decrease charging
- Throttle open → Decrease seal injection, increase charging

#### Charging Isolation Valves (MO-8105, MO-8106)

**Function:** Isolate charging header on safety injection actuation signal

**Type:** Motor-operated, series configuration

#### Charging Paths (3 total)

**Normal Charging (CV-8146):**
- To Loop 1 cold leg
- Parallel spring-loaded check valve (opens at 200 psid)
- Check valve relieves volumetric expansion if charging isolated with letdown continuing

**Alternate Charging (CV-8147):**
- To Loop 4 cold leg
- Use if normal charging inoperable

**Pressurizer Auxiliary Spray (CV-8145):**
- From regenerative HX outlet to pressurizer spray line
- Use during cooldown when RCPs not running
- Normal evolution for RHR operation depressurization

---

### RCP Seal Injection and Return

#### Seal Injection Header

**Flow:** 32 gpm total (8 gpm per RCP)

**Components:**
- Seal injection filters (5 micron) — particulate protection for seal faces
- Individual injection lines to each RCP
- Manual throttle valves (locked, for flow balancing)
- Manual isolation valves

#### Seal Return Header

**Flow:** 12 gpm total (3 gpm per RCP)

**Components:**
- Containment isolation valves (MO-8112, MO-8100) — close on Phase A
- Relief valve (150 psig to PRT) — if isolation valves closed
- Seal return filter
- Seal water heat exchanger (CCW cooled)

**Function:** Seal water HX also cools centrifugal charging pump recirculation flow

---

### Excess Letdown

**Capacity:** 20 gpm at normal RCS pressure

**Components:**
- Loop 3 cold leg penetration
- Excess letdown heat exchanger (CCW cooled)
- Isolation valves (CV-8153, CV-8154)
- Control valve (HCV-123)
- Three-way divert valve (CV-8143) → CVCS or RCDT

**Uses:**
1. **Low RCS pressure** — Orifice flow inadequate
2. **Normal letdown out of service** — Balance RCP seal injection
3. **RCS heatup** — Assist in removing expansion volume
4. **Final heatup stages** — Supplement maximum letdown

**Important:** Only provides small flow at low pressures; designed for 20 gpm at 2235 psig

---

## System Interlocks and Automatic Actions

### Letdown Isolation (Low Pressurizer Level 17%)

**Automatic Actions:**
1. LCV-459 closes
2. LCV-460 closes
3. Orifice isolation valves close
4. All pressurizer heaters de-energize

### Containment Isolation Phase A

**CVCS Actions:**
1. Letdown orifice isolation valves close
2. Containment isolation valve (CV-8152) closes
3. Seal return isolation valves (MO-8112, MO-8100) close

### Safety Injection Actuation Signal

**CVCS Actions:**

**Valves Close:**
1. VCT outlet valves (LCV-112B, LCV-112C)
2. Charging isolation valves (MO-8105, MO-8106)

**Valves Open:**
1. RWST suction valves (LCV-112D, LCV-112E)
2. Boron injection tank valves

**Pumps Start:**
1. Both centrifugal charging pumps (auto-start)

**Result:** Charging pump suction switches from VCT to RWST via BIT

### VCT Level Functions

**Level Control:**

| VCT Level | Action |
|-----------|--------|
| Begin-divert setpoint | LCV-112A starts diverting letdown to BRS |
| High level alarm | Backup controller diverts all letdown to BRS |
| Automatic makeup | RMS starts (if in AUTO mode) |
| Low-low level | Close VCT outlet valves, open RWST suction valves |

---

## Boron Recovery System (BRS)

**Purpose:** Collect and process excess borated water for reuse

### Sources of Excess Borated Water

1. **Dilution operations** — Core burnup compensation
2. **Load follow operations** — Power level changes
3. **Heatup operations** — Cold shutdown to hot standby
4. **Refueling operations** — Draining and refilling

### Process Flow

**Collection:**
- Excess letdown diverted to recycle holdup tanks
- Nitrogen cover gas displaced to waste gas decay tanks

**Processing (Batch Operation):**
1. Holdup tank recirculation pump transfers liquid between tanks
2. Boric acid evaporator feed pump moves liquid through:
   - Evaporator feed ion exchangers
   - Filter
3. Preheater
4. Stripper column — Remove dissolved gases (vent to waste gas system)
5. Evaporator section — Separate water vapor and concentrated boric acid
6. Absorption tower — Remove any boric acid carryover
7. Evaporator condenser — Condense water vapor
8. Condensate demineralizer and filter
9. Monitor tanks — Sample before discharge

**Products:**

**Condensate (Evaporator Overhead):**
- Accumulated in monitor tanks
- Sampled
- Discharged to:
  - Primary water storage tank
  - Lake discharge tank
  - Holdup tanks (for reprocessing)
  - Evaporator condensate demineralizers
  - Liquid waste system

**Concentrates (Evaporator Bottoms):**
- Concentrated to ~4 wt% boric acid
- Sampled
- If meets specs → To boric acid tanks via filter and holding tank
- If not → Return to holdup tanks or liquid waste system

---

## Operational Considerations

### Purification During RHR Operation

**Connection:** RHR to CVCS letdown line (via HCV-128)

**Function:** Purify RCS during cold shutdown

**Flow Path:**
- RHR flow → CVCS letdown (via HCV-128)
- Through mixed-bed demineralizer and filter
- To VCT
- Return to RCS via normal charging

### Preventing Temperature Transients in Ion Exchangers

**Problem:** Resin efficiency reduced and lifetime shortened by high temperature

**Solution:** Temperature divert valve (TCV-129)
- Automatic divert at 137°F
- Bypasses demineralizers
- Manual reset required after auto-divert

### Preventing Loss of Charging Pump Suction

**Problem:** VCT level too low → Pump cavitation

**Solution:** VCT low-low level signal
- Closes VCT outlet valves
- Opens RWST suction valves
- Automatically switches suction source

### Preventing VCT Overpressure/Underpressure

**Overpressure Protection:**
- Relief valve at 75 psig (discharges to holdup tanks)

**Underpressure Protection:**
- Vent pressure regulating valve closes at 15 psig
- Maintains adequate NPSH for charging pumps

### Preventing Flashing in Heat Exchangers

**Regenerative HX:**
- Interlock prevents opening orifice isolation valves without charging pump running
- Ensures cooling water available before hot letdown established

**Letdown HX:**
- PCV-131 maintains 340 psig upstream
- Prevents flashing until temperature reduced below saturation

---

## Key Parameters Summary

### Normal Operating Flows (2235 psig)

| Parameter | Value |
|-----------|-------|
| Charging pump discharge | 87 gpm |
| Normal charging to Loop 1 | 55 gpm |
| RCP seal injection (total) | 32 gpm |
| RCP seal return to RCS | 20 gpm |
| RCP seal leakoff to CVCS | 12 gpm |
| Normal letdown | 75 gpm |
| Excess letdown capacity | 20 gpm |

### Letdown Orifice Capacities (2235 psig)

| Orifice | Capacity |
|---------|----------|
| A | 75 gpm |
| B | 75 gpm |
| C | 45 gpm |
| **Maximum total** | **195 gpm** |

**Limitation:** Ion exchanger flow limited to 127 gpm (administrative)

### Temperature Setpoints

| Component | Temperature |
|-----------|-------------|
| Letdown HX outlet | 120°F (normal) |
| Demineralizer bypass | 137°F |
| Regenerative HX (letdown side) | 550°F → 290°F |
| Regenerative HX (charging side) | 130°F → 500°F |

### Pressure Setpoints

| Component | Pressure |
|-----------|----------|
| Letdown pressure control (PCV-131) | 340 psig |
| VCT normal operating range | 15-75 psig |
| VCT vent closure | 15 psig |
| VCT relief valve | 75 psig |
| Seal return relief valve | 150 psig |
| Letdown relief (post-orifice) | 600 psig |
| Letdown relief (post-PCV-131) | 285 psig |

### Boric Acid System

| Parameter | Value |
|-----------|-------|
| Boric acid tank capacity (each) | 24,228 gallons |
| Boric acid concentration | ~4 wt% (7000 ppm) |
| Tech Spec minimum inventory | 15,900 gallons @ 7000 ppm |
| Room temperature requirement | > 65°F |
| Automatic makeup flow (blended) | 80 gpm |
| Emergency boration path | Via MO-8104 |

---

## Critical Implementation Notes for Simulator

### Charging/Letdown Balance Control

**Master Loop:**
1. Pressurizer level error → Master level controller (PI)
2. Master controller output → Charging flow setpoint
3. Charging flow setpoint vs. actual (FT-121) → Flow controller (PI)
4. Flow controller output → FCV-121 position (if centrifugal pump)

**Alternative:** Variable speed control if positive displacement pump

### Seal Injection/Charging Split

**Control:** HCV-182 position determines split
- Total charging pump discharge = 87 gpm
- Seal injection + Normal charging = 87 gpm
- Adjust HCV-182 to change split ratio

### Letdown Orifice Sizing

**Flow calculation:**
```
Q = K × sqrt(P_RCS - P_downstream)

Where:
K = orifice constant (calibrated for design flow at 2235 psig)
P_RCS = RCS pressure
P_downstream = typically 340 psig (PCV-131 setpoint)
```

**For 75-gpm orifice:**
```
75 gpm = K × sqrt(2235 - 340)
K = 75 / sqrt(1895) = 1.723 gpm/sqrt(psi)
```

### VCT Level Control

**Multiple setpoints required:**
1. Begin-divert: Start partial diversion to BRS (proportional)
2. High level alarm: Full diversion to BRS (backup controller)
3. Normal range: All letdown to VCT
4. Automatic makeup: Start RMS in AUTO mode
5. Low-low level: Switch to RWST suction

### Boration/Dilution Calculations

**Boration mass balance:**
```
m_acid × C_acid + m_RCS × C_RCS = (m_acid + m_RCS) × C_final

Where:
m_acid = mass of boric acid added
C_acid = concentration of boric acid (7000 ppm)
m_RCS = RCS mass
C_RCS = current RCS concentration
C_final = desired final concentration
```

---

## References

- NRC HRTD Section 10.3 — Pressurizer Level Control System
- NRC HRTD Section 3.2 — Reactor Coolant System (RCP seals)
- NRC HRTD Section 5.2 — Emergency Core Cooling Systems
- NRC HRTD Section 19.0 — Plant Operations (startup pressurization)

---

*Document retrieved and formatted 2026-02-15*
