# Operator Screen Layout System - Implementation Plan

**Version:** 1.0.0  
**Date:** 2026-02-09  
**Author:** AI Development Assistant  
**Status:** Awaiting Approval

---

## 1. Problem Summary

The simulator currently has one complete operator screen (Reactor Core - Key 1) but needs a comprehensive multi-screen interface covering all major PWR systems. The goal is to determine:

1. What screens are needed to monitor and control all simulated systems
2. How to group related systems when combining them into single screens is practical
3. Consistent layout approach following the established pattern (central equipment visual, surrounding gauges, bottom controls)

---

## 2. Expectations - Correct Design

### 2.1 Design Principles

**Multi-Screen Navigation Pattern:**
- Similar to Microsoft Flight Simulator (MSFS) cockpit panel system
- Each screen accessible via numbered keyboard shortcuts (1-9, 0)
- Tab key for system overview screen
- Only one screen visible at a time
- Screens toggle on/off (press same key to hide)

**Consistent Layout Template:**
- **Center:** Interactive equipment visualization (primary component)
- **Left Panel (0-15%):** Monitoring gauges (typically 8-10 gauges)
- **Right Panel (65-80%):** Monitoring gauges (typically 8-10 gauges)
- **Bottom Panel (0-26%):** Control interfaces, status indicators, alarms
- **Optional Detail Panel (80-100%):** Context-sensitive detailed information

**Information Density:**
- Each screen should be self-contained for its operational focus
- Operators should not need to switch screens frequently during normal operations
- Cross-system parameters can be duplicated where operationally relevant
- Alarms and critical status always visible in bottom panel

### 2.2 Current Implementation Status

**Complete:**
- ✅ Screen 1: Reactor Core Operator Screen
  - 193-assembly core map with multiple display modes
  - 9 nuclear instrumentation gauges (left)
  - 8 thermal-hydraulic gauges (right)
  - Rod bank controls and display (bottom)
  - Assembly detail panel (right, context-sensitive)

**Architecture Established:**
- ✅ `ReactorOperatorScreen.cs` base pattern
- ✅ `MosaicBoard` data provider integration
- ✅ `MosaicGauge` reusable gauge component
- ✅ `ScreenZone` layout system
- ✅ Keyboard toggle mechanism

---

## 3. Proposed Screen Layout System

### 3.1 Screen Assignment Overview

| Key | Screen Name | Primary System(s) | Status |
|-----|-------------|-------------------|--------|
| **1** | **Reactor Core** | Reactor, Control Rods, Xenon | ✅ Complete |
| **2** | **RCS Primary Loop** | RCS Loops, RCPs, Flow, Temperatures | 🟡 Planned |
| **3** | **Pressurizer** | PZR Pressure, Level, Heaters, Spray | 🟡 Planned |
| **4** | **CVCS** | Charging, Letdown, VCT, Boron Control | 🟡 Planned |
| **5** | **Steam Generators** | 4× SG Heat Transfer, Levels, Feedwater | 🟡 Planned |
| **6** | **Turbine-Generator** | Turbine, Generator, Condenser, Power Output | 🟡 Planned |
| **7** | **Secondary Systems** | Feedwater, Steam Dump, MSIVs, Condensate | 🟡 Planned |
| **8** | **Auxiliary Systems** | RHR, CVCS, Sampling, Chemical Control | 🟡 Planned |
| **9** | **Safety Systems** | ECCS, Containment, Safety Injection (future) | ⚪ Future |
| **0** | **Electrical** | Buses, Diesel Generators, Loads (future) | ⚪ Future |
| **Tab** | **Overview** | Plant-Wide Mimic, All Critical Parameters | 🟡 Planned |

**Legend:**
- ✅ Complete and validated
- 🟡 Planned for current implementation
- ⚪ Future enhancement (beyond Phase 6)

---

### 3.2 Detailed Screen Specifications

#### 3.2.1 Screen 1: Reactor Core (COMPLETE)

**Toggle Key:** `1`

**Central Visual:** 193-assembly core mosaic map

**Left Gauges (Nuclear Instrumentation):**
1. Neutron Power (0-120%)
2. Thermal Power (0-120%)
3. Startup Rate (DPM)
4. Reactor Period (seconds)
5. Total Reactivity (pcm)
6. K-effective (0.90-1.10)
7. Boron Concentration (ppm)
8. Xenon Worth (pcm)
9. RCS Flow Fraction (0-100%)

**Right Gauges (Thermal-Hydraulic):**
1. T-avg (°F)
2. T-hot (°F)
3. T-cold (°F)
4. Delta-T (°F)
5. Fuel Centerline Temperature (°F)
6. Hot Channel Temperature (°F)
7. RCS Pressure (psia)
8. PZR Level (%)

**Bottom Controls:**
- Rod bank position display (all 8 banks)
- Rod motion controls (withdraw/insert, bank select)
- Reactor trip button
- Display mode buttons (power, fuel temp, coolant temp, rod banks)
- Bank filter buttons (SA, SB, SC, SD, D, C, B, A, All)
- Alarm annunciators

**Status:** Complete and operational

---

#### 3.2.2 Screen 2: RCS Primary Loop

**Toggle Key:** `2`

**Purpose:** Monitor and control primary coolant system loops, reactor coolant pumps, and flow distribution.

**Central Visual:** 
- 4-loop RCS schematic showing:
  - Reactor vessel (center)
  - 4 hot legs radiating outward
  - Steam generator symbols (one per loop)
  - 4 cold legs returning to reactor
  - RCP symbols in cold legs
  - Flow direction arrows (animated)
  - Color-coded temperature gradient (red hot legs, blue cold legs)

**Left Gauges (Loop Temperatures):**
1. Loop 1 T-hot (°F)
2. Loop 2 T-hot (°F)
3. Loop 3 T-hot (°F)
4. Loop 4 T-hot (°F)
5. Loop 1 T-cold (°F)
6. Loop 2 T-cold (°F)
7. Loop 3 T-cold (°F)
8. Loop 4 T-cold (°F)

**Right Gauges (Flow and Power):**
1. Total RCS Flow (gpm)
2. Loop 1 Flow (gpm)
3. Loop 2 Flow (gpm)
4. Loop 3 Flow (gpm)
5. Loop 4 Flow (gpm)
6. Core Thermal Power (MWt)
7. Core ΔT (°F)
8. Average T-avg (°F)

**Bottom Controls:**
- RCP start/stop buttons (4 pumps)
- RCP status indicators (running/stopped, vibration, seal injection)
- RCP speed indicators (rpm)
- Natural circulation mode indicator
- Loop isolation valve positions (if modeled)
- Alarm panel for RCP/RCS alarms

**Notes:**
- Currently, simulator uses single lumped loop model
- Visual will show 4 loops with symmetrical parameters
- Future: Individual loop resolution may be added

---

#### 3.2.3 Screen 3: Pressurizer

**Toggle Key:** `3`

**Purpose:** Monitor and control pressurizer pressure, level, heaters, and spray system.

**Central Visual:**
- Pressurizer vessel cutaway showing:
  - Two-phase region (steam above, water below)
  - Water level indicator (animated)
  - Heater banks at bottom (glow when active)
  - Spray nozzle at top (spray animation when active)
  - Surge line connection to RCS (flow indication)
  - Safety/relief valve symbols at top
  - Pressure boundary outline

**Left Gauges (Pressure System):**
1. Pressurizer Pressure (psia)
2. Pressure Setpoint (psia)
3. Pressure Error (psi)
4. Pressure Rate (psi/min)
5. Heater Power (kW)
6. Spray Flow (gpm)
7. Backup Heater Status (kW)
8. PORV Status (open/closed)

**Right Gauges (Level and Inventory):**
1. PZR Level (%)
2. Level Setpoint (%)
3. Level Error (%)
4. Surge Flow (gpm, in/out indication)
5. Steam Volume (ft³)
6. Water Volume (ft³)
7. Total RCS Inventory (gallons)
8. Surge Line Temperature (°F)

**Bottom Controls:**
- Master heater enable/disable
- Proportional heater control mode selector (auto/manual)
- Backup heater manual control
- Spray valve control (auto/manual)
- Pressure setpoint adjustment (+/- buttons)
- PORV manual open (emergency)
- Safety valve status indicators
- Alarm panel (high/low pressure, high/low level)

**Notes:**
- Bubble formation state machine status shown
- Two-phase transition warnings

---

#### 3.2.4 Screen 4: Chemical and Volume Control System (CVCS)

**Toggle Key:** `4`

**Purpose:** Monitor and control charging/letdown system, VCT level, boron concentration, and seal injection.

**Central Visual:**
- CVCS flow diagram showing:
  - Volume Control Tank (VCT) with level indication
  - Charging pumps (CCPs) with status
  - Letdown line from RCS
  - Charging line to RCS
  - Seal injection lines to RCPs (4 branches)
  - Boration/dilution flow paths
  - Demineralizers
  - Flow direction arrows (animated based on flow)

**Left Gauges (Flow and Control):**
1. Charging Flow (gpm)
2. Letdown Flow (gpm)
3. Seal Injection Flow (gpm)
4. Net Inventory Change (gpm, +/-)
5. VCT Level (%)
6. VCT Temperature (°F)
7. VCT Pressure (psig)
8. CCP Discharge Pressure (psig)

**Right Gauges (Chemistry and Boron):**
1. RCS Boron Concentration (ppm)
2. VCT Boron Concentration (ppm)
3. Boration Flow (gpm)
4. Dilution Flow (gpm)
5. Boron Worth (pcm)
6. Letdown Temperature (°F)
7. Charging Temperature (°F)
8. Purification Flow (gpm)

**Bottom Controls:**
- Charging pump start/stop (CCP-A, CCP-B, CCP-C)
- Letdown valve control (throttle position)
- Letdown flow setpoint (+/- adjustment)
- Boration/dilution mode selector
- Boration/dilution flow rate setpoint
- VCT level setpoint adjustment
- Seal injection flow indication (per pump)
- Alarm panel (VCT level, CCP status, boron out of spec)

**Notes:**
- PI control loop status visualization
- Pre-seed for RCP start indication

---

#### 3.2.5 Screen 5: Steam Generators

**Toggle Key:** `5`

**Purpose:** Monitor heat transfer performance, secondary side levels, and feedwater system for all four steam generators.

**Central Visual:**
- Quad-SG layout (2×2 grid):
  - Each SG shown as U-tube schematic
  - Primary side inlet/outlet (hot leg in, cold leg out)
  - Secondary side steam dome with level indication
  - Feedwater inlet at bottom
  - Steam outlet at top
  - Tube bundle representation
  - Level indicators (narrow range and wide range)
  - Color-coded temperature gradient

**Left Gauges (Primary Side):**
1. SG-A Primary Inlet Temp (°F)
2. SG-B Primary Inlet Temp (°F)
3. SG-C Primary Inlet Temp (°F)
4. SG-D Primary Inlet Temp (°F)
5. SG-A Primary Outlet Temp (°F)
6. SG-B Primary Outlet Temp (°F)
7. SG-C Primary Outlet Temp (°F)
8. SG-D Primary Outlet Temp (°F)

**Right Gauges (Secondary Side):**
1. SG-A Level (%)
2. SG-B Level (%)
3. SG-C Level (%)
4. SG-D Level (%)
5. SG-A Steam Pressure (psia)
6. SG-B Steam Pressure (psia)
7. SG-C Steam Pressure (psia)
8. SG-D Steam Pressure (psia)

**Bottom Controls:**
- Feedwater flow indicators (per SG)
- Steam flow indicators (per SG)
- Feedwater pump status
- Feedwater valve control (per SG)
- Steam generator blowdown indicators
- Heat removal rate display (per SG)
- Alarm panel (level high/low, pressure deviation, tube leak detection)

**Notes:**
- Currently lumped model; visual shows 4 SGs with symmetric behavior
- Future: Individual SG resolution with asymmetric transients (SGTR scenarios)

---

#### 3.2.6 Screen 6: Turbine-Generator

**Toggle Key:** `6`

**Purpose:** Monitor turbine-generator performance, electrical output, condenser, and grid connection.

**Central Visual:**
- Turbine-generator shaft train:
  - HP turbine (high pressure)
  - Moisture separator/reheater
  - LP turbine A (low pressure)
  - LP turbine B
  - Generator symbol
  - Condenser below LP turbines
  - Steam admission valves
  - Extraction steam lines
  - Shaft RPM indicator (3600 rpm nominal)

**Left Gauges (Turbine Performance):**
1. HP Turbine Inlet Pressure (psia)
2. HP Turbine Inlet Temperature (°F)
3. HP Turbine Exhaust Pressure (psia)
4. LP Turbine Exhaust Pressure (in Hg abs)
5. Throttle Steam Flow (lb/hr)
6. Turbine First Stage Pressure (psia)
7. Moisture Separator Pressure (psia)
8. Reheat Steam Temperature (°F)

**Right Gauges (Generator and Output):**
1. Generator Output (MWe)
2. Gross Output (MWe)
3. Auxiliary Load (MWe)
4. Net Output (MWe)
5. Generator Voltage (kV)
6. Generator Current (A)
7. Power Factor
8. Grid Frequency (Hz)

**Bottom Controls:**
- Turbine control valve position
- Turbine bypass valve status
- Generator breaker status (closed/open)
- Load setpoint adjustment
- Turbine trip status
- Generator synchronization controls
- Condenser vacuum indication
- Alarm panel (turbine trip, generator trip, condenser vacuum low)

**Notes:**
- Simplified model: Direct steam flow → electrical power conversion
- Grid dynamics not modeled (constant frequency/voltage assumed)

---

#### 3.2.7 Screen 7: Secondary Systems (Combined)

**Toggle Key:** `7`

**Purpose:** Monitor feedwater heaters, condensate system, steam dump, and main steam isolation.

**Rationale for Combination:**
- These systems are closely related (feedwater train)
- Operators monitor as integrated secondary plant
- Not safety-critical for reactor control (can be combined)

**Central Visual:**
- Simplified secondary cycle flow diagram:
  - Condenser hotwell
  - Condensate pumps
  - Condensate polishing
  - Low-pressure feedwater heaters (LP FWH 1-3)
  - Deaerator
  - Feedwater pumps
  - High-pressure feedwater heaters (HP FWH 4-6)
  - Feedwater to SGs (4 branches)
  - Main steam lines from SGs
  - Steam dump to condenser
  - Main steam isolation valves (MSIVs)

**Left Gauges (Feedwater Train):**
1. Condenser Hotwell Level (inches)
2. Condensate Pump Discharge Pressure (psig)
3. Deaerator Pressure (psig)
4. Deaerator Level (inches)
5. Feedwater Pump Suction Pressure (psig)
6. Feedwater Pump Discharge Pressure (psig)
7. Final Feedwater Temperature (°F)
8. Feedwater Flow Total (lb/hr)

**Right Gauges (Steam System):**
1. Main Steam Header Pressure (psia)
2. Steam Flow to Turbine (lb/hr)
3. Steam Dump Flow (lb/hr)
4. MSIV-A Position (%)
5. MSIV-B Position (%)
6. MSIV-C Position (%)
7. MSIV-D Position (%)
8. Turbine Bypass Valve Position (%)

**Bottom Controls:**
- Condensate pump start/stop
- Feedwater pump start/stop
- Feedwater valve control mode (auto/manual)
- Steam dump mode selector (off/pressure control/temperature control)
- Steam dump setpoint adjustment
- MSIV control (open/close individual valves)
- Deaerator level control
- Alarm panel (low condenser level, FW pump trip, MSIV closed)

**Notes:**
- Detailed feedwater heater extraction steam modeling not included
- Focus on feedwater temperature and flow control

---

#### 3.2.8 Screen 8: Auxiliary Systems (Combined)

**Toggle Key:** `8`

**Purpose:** Monitor auxiliary support systems including residual heat removal (RHR), component cooling, service water, and sampling.

**Rationale for Combination:**
- These are support systems not directly in power generation path
- Lower information density requirements
- Operators check periodically but not continuously

**Central Visual:**
- Auxiliary systems overview:
  - RHR heat exchangers (train A & B)
  - Component Cooling Water (CCW) heat exchangers
  - CCW pumps and headers
  - Service Water (SW) pumps
  - RCP thermal barriers (cooling)
  - Reactor support coolers
  - Sampling system connections

**Left Gauges (RHR System):**
1. RHR Train A Flow (gpm)
2. RHR Train B Flow (gpm)
3. RHR HX A Inlet Temp (°F)
4. RHR HX A Outlet Temp (°F)
5. RHR HX B Inlet Temp (°F)
6. RHR HX B Outlet Temp (°F)
7. RHR Suction Pressure (psig)
8. RHR Pump Status (running/stopped)

**Right Gauges (Cooling Water):**
1. CCW Supply Header Pressure (psig)
2. CCW Return Header Pressure (psig)
3. CCW Surge Tank Level (%)
4. CCW Temperature (°F)
5. Service Water Flow (gpm)
6. Service Water Temperature (°F)
7. RCP Thermal Barrier Flow (gpm per pump)
8. Component Cooling Heat Load (BTU/hr)

**Bottom Controls:**
- RHR pump start/stop (train A & B)
- RHR valve alignment (RCS suction, shutdown cooling)
- CCW pump start/stop
- SW pump start/stop
- Sampling system valve alignment
- RCP thermal barrier flow indicators
- Alarm panel (RHR unavailable, CCW low flow, SW low flow)

**Notes:**
- RHR only used during shutdown cooling (Mode 4-5)
- Not active during power operations (Modes 1-2)

---

#### 3.2.9 Screen 9: Safety Systems (Future Enhancement)

**Toggle Key:** `9`

**Purpose:** Monitor emergency core cooling systems (ECCS), safety injection, accumulators, and containment status.

**Status:** ⚪ Future - Not included in current scope (accident scenarios Phase 7+)

**Planned Content:**
- Central Visual: ECCS lineup (HPSI, LPSI, accumulators)
- Left Gauges: Safety injection flow rates, accumulator pressures/levels
- Right Gauges: Containment pressure, temperature, radiation levels
- Bottom Controls: SI actuation, isolation valve overrides, containment isolation

**Implementation:** Deferred until accident modeling (LOCA, SGTR) is added

---

#### 3.2.10 Screen 0: Electrical Distribution (Future Enhancement)

**Toggle Key:** `0`

**Purpose:** Monitor electrical buses, diesel generators, battery banks, and vital loads.

**Status:** ⚪ Future - Not included in current scope (electrical system modeling Phase 8+)

**Planned Content:**
- Central Visual: One-line electrical diagram (4.16 kV buses, 480V load centers)
- Left Gauges: Bus voltages, diesel generator output, battery charge status
- Right Gauges: Load distribution, vital bus continuity, inverter status
- Bottom Controls: Breaker open/close, bus transfer, diesel start

**Implementation:** Deferred until electrical fault scenarios are added

---

#### 3.2.11 Screen Tab: Plant Overview

**Toggle Key:** `Tab`

**Purpose:** High-level plant-wide mimic showing all major systems and critical parameters at a glance.

**Central Visual:**
- Simplified plant flow diagram (single-page):
  - Reactor core (center)
  - RCS loops (4 hot legs, 4 cold legs)
  - Pressurizer
  - Steam generators (4)
  - Turbine-generator
  - Condenser
  - Feedwater train
  - Critical parameters overlaid on diagram

**Left Gauges (Nuclear/Primary):**
1. Reactor Power (%)
2. T-avg (°F)
3. RCS Pressure (psia)
4. PZR Level (%)
5. Total RCS Flow (gpm)
6. Control Rod Position (steps)
7. Boron Concentration (ppm)
8. Xenon Worth (pcm)

**Right Gauges (Secondary/Output):**
1. SG Level Average (%)
2. Steam Pressure (psia)
3. Feedwater Flow (lb/hr)
4. Turbine Power (MWe)
5. Generator Output (MWe)
6. Condenser Vacuum (in Hg)
7. Feedwater Temperature (°F)
8. Main Steam Flow (lb/hr)

**Bottom Panel:**
- Reactor mode indicator (Mode 1-6)
- RCP status (4 pumps)
- Turbine status
- Generator breaker status
- Major alarm summary (grouped by system)
- Simulation time and time compression
- Emergency action buttons (reactor trip, turbine trip)

**Notes:**
- This is the "at-a-glance" screen for general plant status
- Not intended for detailed control actions
- Operators switch to specific system screens for control

---

## 4. Proposed Fix - Technical Implementation

### 4.1 Architecture Overview

**Screen Management System:**
- `ScreenManager.cs` - Master controller for multi-screen navigation
  - Tracks active screen
  - Handles keyboard input (1-9, 0, Tab)
  - Enforces single-screen-visible rule
  - Manages screen transitions

**Screen Base Class:**
- Abstract `OperatorScreen.cs` base class
  - Common visibility toggle logic
  - Keyboard key assignment
  - Layout zone management
  - Gauge registration system
  - Status display updates

**Individual Screen Classes:**
- `ReactorCoreScreen.cs` (already exists as `ReactorOperatorScreen.cs`)
- `RCSLoopScreen.cs`
- `PressurizerScreen.cs`
- `CVCSScreen.cs`
- `SteamGeneratorScreen.cs`
- `TurbineGeneratorScreen.cs`
- `SecondarySystemsScreen.cs`
- `AuxiliarySystemsScreen.cs`
- `PlantOverviewScreen.cs`

### 4.2 Implementation Stages

**Stage 1: Screen Management Framework**
- Create `ScreenManager` singleton
- Create abstract `OperatorScreen` base class
- Refactor `ReactorOperatorScreen` to inherit from base
- Implement screen registration and keyboard routing
- Test single-screen toggle functionality

**Stage 2: RCS Loop Screen (Key 2)**
- Create `RCSLoopScreen.cs`
- Design 4-loop schematic visual component
- Implement loop temperature and flow gauges
- Add RCP control panel
- Integrate with `RCPSequencer` and `LoopThermodynamics`

**Stage 3: Pressurizer Screen (Key 3)**
- Create `PressurizerScreen.cs`
- Design pressurizer vessel visualization component
- Implement pressure and level gauges
- Add heater and spray control panel
- Integrate with `PressurizerPhysics` and pressure controller

**Stage 4: CVCS Screen (Key 4)**
- Create `CVCSScreen.cs`
- Design CVCS flow diagram component
- Implement charging/letdown flow gauges
- Add boration/dilution control panel
- Integrate with `CVCSController` and `VCTPhysics`

**Stage 5: Steam Generator Screen (Key 5)**
- Create `SteamGeneratorScreen.cs`
- Design quad-SG visualization component
- Implement SG level and pressure gauges
- Add feedwater control panel
- Integrate with `SGSecondaryThermal`

**Stage 6: Turbine-Generator Screen (Key 6)**
- Create `TurbineGeneratorScreen.cs`
- Design turbine-generator shaft train visual
- Implement turbine and generator gauges
- Add load control panel
- Integrate with turbine model (when available)

**Stage 7: Secondary Systems Screen (Key 7)**
- Create `SecondarySystemsScreen.cs`
- Design feedwater/steam dump diagram
- Implement feedwater train gauges
- Add steam dump control panel
- Integrate with feedwater and steam systems

**Stage 8: Auxiliary Systems Screen (Key 8)**
- Create `AuxiliarySystemsScreen.cs`
- Design RHR/CCW overview diagram
- Implement auxiliary system gauges
- Add auxiliary system control panel
- Integrate with support system models

**Stage 9: Plant Overview Screen (Key Tab)**
- Create `PlantOverviewScreen.cs`
- Design plant-wide mimic diagram
- Implement summary gauges (all critical parameters)
- Add major alarm summary display
- Integrate with all physics modules for status

**Stage 10: Testing and Validation**
- Test all screen transitions
- Verify gauge data accuracy
- Validate control actions
- Check alarm propagation
- Performance testing (rendering all screens)

### 4.3 Reusable Components

**Gauge System (Already Implemented):**
- `MosaicGauge.cs` - Generic gauge component
- `GaugeType` enum - Gauge data bindings
- Gauge configuration in `MosaicBoard`

**New Visual Components Needed:**
1. `RCSLoopDiagram.cs` - 4-loop schematic with flow animation
2. `PressurizerVisualization.cs` - Two-phase vessel cutaway
3. `CVCSFlowDiagram.cs` - CVCS process flow with animated flows
4. `SteamGeneratorQuad.cs` - 2×2 SG layout with levels
5. `TurbineShaftTrain.cs` - HP/LP turbine and generator
6. `SecondaryFlowDiagram.cs` - Feedwater and steam paths
7. `AuxiliarySystemDiagram.cs` - RHR and cooling water layout
8. `PlantMimicDiagram.cs` - Simplified full-plant overview

**Animation System:**
- Flow direction arrows (pulsing/moving)
- Rotating shaft indicators (turbine/generator)
- Liquid level animations (PZR, VCT, SG)
- Color-coded temperature gradients
- Valve position indicators
- Pump running indicators

---

## 5. Unaddressed Issues

### 5.1 Issues Planned for Future Release

**Screen 9: Safety Systems**
- **Issue:** ECCS, safety injection, containment modeling not yet implemented
- **Plan:** Phase 7 (Accident Scenarios) will add LOCA and SGTR capability
- **Action:** Document in Future_Features, implement with accident physics

**Screen 0: Electrical Distribution**
- **Issue:** Electrical bus, diesel generator, battery models not implemented
- **Plan:** Phase 8 (Electrical Faults) will add loss of power scenarios
- **Action:** Document in Future_Features, implement with electrical system

**Individual Loop Resolution**
- **Issue:** Currently single lumped loop; Screen 2 shows 4 symmetric loops
- **Impact:** Cannot simulate asymmetric loop transients (one RCP trip, unbalanced SG levels)
- **Plan:** Phase 9 (Advanced Thermal-Hydraulics) may add per-loop resolution
- **Action:** Document limitation; visual shows 4 loops but physics uses averaged values

**Turbine-Generator Detailed Model**
- **Issue:** Simplified steam → electrical power conversion; no turbine stages or governor
- **Impact:** Cannot simulate turbine control transients or turbine trip scenarios
- **Plan:** Phase 6 completion will add basic turbine model
- **Action:** Screen 6 will be implemented with available turbine fidelity

**Secondary Side Detailed Resolution**
- **Issue:** Lumped SG model; no recirculation, no tube-by-tube flow
- **Impact:** SGTR scenarios will have limited fidelity
- **Plan:** Future enhancement if required for training scenarios
- **Action:** Accept limitation; educationally sufficient for power operations

### 5.2 Issues Not Addressed (Out of Scope)

**3D Equipment Visualization**
- **Scope:** This plan uses 2D schematic diagrams (similar to actual plant control room panels)
- **Reason:** 2D is industry standard; 3D adds complexity without operational benefit
- **Status:** Not planned

**Dynamic Piping System Visualization**
- **Scope:** Flow paths are static diagrams with animated indicators
- **Reason:** Full dynamic piping (valve alignment changes) is complex and rarely used
- **Status:** Not planned unless specific training need identified

**Touchscreen Support**
- **Scope:** Keyboard and mouse only
- **Reason:** Desktop simulator; touchscreen not typical for training simulators
- **Status:** Could be added in future if VR/tablet interface developed

**Multi-Monitor Support**
- **Scope:** Single 1920×1080 screen assumed
- **Reason:** Most users have single monitor; multi-monitor is advanced feature
- **Status:** Future enhancement if user demand exists

**Screen Tearing During Transitions**
- **Scope:** Potential visual artifact when toggling screens rapidly
- **Reason:** Unity Canvas rendering optimizations needed
- **Status:** Test during Stage 10; add screen fade transitions if needed

---

## 6. Dependencies and Prerequisites

### 6.1 Existing Systems (Must Be Operational)

✅ **Core Physics Modules (GOLD STANDARD):**
- `ReactorKinetics` - Neutron power calculations
- `ThermalMass` - Coolant and metal heat capacity
- `LoopThermodynamics` - RCS temperature calculations
- `PressurizerPhysics` - Pressure and level control
- `CVCSController` - Charging/letdown control
- `RCPSequencer` - Pump start/stop logic
- `SGSecondaryThermal` - Steam generator heat transfer

✅ **UI Infrastructure:**
- `MosaicBoard` - Data provider and gauge registration
- `MosaicGauge` - Reusable gauge component
- `ReactorOperatorScreen` - Established screen pattern

✅ **Unity Project Setup:**
- Unity 2022.3 LTS
- Universal Render Pipeline (URP)
- TextMeshPro (for text rendering)

### 6.2 New Systems Required

**For Stage 2 (RCS Loop Screen):**
- RCS loop schematic visual component
- Flow animation system
- Color gradient shader (temperature visualization)

**For Stage 3 (Pressurizer Screen):**
- Pressurizer vessel cutaway visual
- Two-phase interface renderer
- Heater glow effect
- Spray animation

**For Stage 4 (CVCS Screen):**
- CVCS flow diagram component
- Valve position indicators
- Pump running indicators
- Flow direction arrows

**For Stage 5 (Steam Generator Screen):**
- Quad-SG layout component
- SG level animation
- U-tube bundle visualization

**For Stage 6 (Turbine-Generator Screen):**
- Turbine shaft train visual
- Rotating shaft animation
- Steam flow indicators

**For Stage 7-8:**
- Generic process flow diagram component
- Reusable piping and valve sprites

### 6.3 Data Sources Required

All gauge data must be available from physics modules:

**Existing (Already Available):**
- Reactor power, k-eff, reactivity
- RCS temperatures (T-hot, T-cold, T-avg)
- RCS pressure
- PZR level, pressure
- VCT level
- Boron concentration
- Xenon worth

**May Need Addition:**
- Per-loop temperatures (if individual loops implemented)
- Per-RCP flow rates
- Per-SG levels and pressures
- Turbine-generator parameters (when turbine model added)
- Feedwater temperatures and flows
- Condenser vacuum
- RHR flow and temperatures

---

## 7. Testing and Validation Strategy

### 7.1 Screen Navigation Testing

**Test Cases:**
1. Verify each numbered key (1-9, 0, Tab) opens correct screen
2. Verify only one screen visible at a time
3. Verify pressing active screen key toggles it off
4. Verify pressing different screen key switches screens
5. Verify screen state persists (data updates continue when hidden)

**Success Criteria:**
- No screen overlap or z-fighting
- Smooth transitions (<100ms)
- Keyboard input always responsive
- No memory leaks from repeated toggles

### 7.2 Gauge Data Accuracy Testing

**Test Cases:**
1. Verify all gauges display correct units
2. Verify gauge ranges match technical specifications
3. Verify gauge update rates (10 Hz for most, 2 Hz for core map)
4. Compare gauge readings to `MosaicBoard` internal values
5. Test gauge response to transients (step change, ramp)

**Success Criteria:**
- <1% error vs. physics module data
- Gauge needles move smoothly (no jitter)
- Digital readouts update at correct frequency
- No gauge sticking or freezing

### 7.3 Control Action Validation

**Test Cases:**
1. Verify all control buttons trigger correct physics actions
2. Test RCP start/stop from Screen 2
3. Test heater/spray control from Screen 3
4. Test boration/dilution from Screen 4
5. Test feedwater valve control from Screen 5

**Success Criteria:**
- All controls functional
- Physics modules respond correctly
- Status indicators update immediately
- Interlocks enforced (e.g., cannot start RCP if pressure low)

### 7.4 Visual Component Testing

**Test Cases:**
1. Verify all schematic diagrams render correctly
2. Test flow animations (smooth, correct direction)
3. Test color gradients (temperature visualization)
4. Test level indicators (PZR, VCT, SG)
5. Performance test (FPS with all visuals active)

**Success Criteria:**
- All visuals render at 60 FPS minimum
- Animations smooth (no stuttering)
- Colors match design specification
- Text readable at 1920×1080 resolution

### 7.5 Alarm System Testing

**Test Cases:**
1. Verify alarms appear on all relevant screens
2. Test alarm acknowledgement from any screen
3. Test alarm priority (critical, warning, info)
4. Test alarm sound (audio cue on new alarm)

**Success Criteria:**
- Alarms visible on all screens (bottom panel)
- Acknowledgement clears visual but logs alarm
- Critical alarms flash/pulse
- Audio cue plays once per alarm

---

## 8. Documentation Requirements

### 8.1 User Documentation

**Operator Manual Updates:**
- Add section: "Multi-Screen Navigation"
- Document keyboard shortcuts for each screen
- Describe purpose and content of each screen
- Explain gauge meanings and control actions

**Quick Reference Card:**
- Single-page keyboard shortcut guide
- Screen layout diagrams
- Critical alarm reference

### 8.2 Technical Documentation

**Architecture Documentation:**
- `ScreenManager` class documentation
- `OperatorScreen` base class documentation
- Visual component API documentation
- Gauge data binding reference

**Implementation Notes:**
- Screen-specific implementation details (per screen class)
- Visual component implementation notes
- Animation system documentation

### 8.3 Validation Reports

**Screen Validation Reports:**
- Validation test results for each screen
- Gauge accuracy comparison tables
- Control action verification logs
- Performance benchmarks (FPS, memory usage)

---

## 9. Success Criteria

### 9.1 Functional Requirements

✅ **All planned screens implemented and operational:**
- Screen 1: Reactor Core ✅ (already complete)
- Screen 2: RCS Loop 🟡 (planned)
- Screen 3: Pressurizer 🟡 (planned)
- Screen 4: CVCS 🟡 (planned)
- Screen 5: Steam Generators 🟡 (planned)
- Screen 6: Turbine-Generator 🟡 (planned)
- Screen 7: Secondary Systems 🟡 (planned)
- Screen 8: Auxiliary Systems 🟡 (planned)
- Screen Tab: Plant Overview 🟡 (planned)

✅ **Navigation system functional:**
- Keyboard shortcuts work for all screens
- Only one screen visible at a time
- Screen transitions smooth and immediate

✅ **All gauges accurate:**
- <1% error vs. physics module data
- Correct units and ranges
- Smooth updates at correct frequency

✅ **All controls functional:**
- Control actions trigger correct physics
- Interlocks enforced
- Status indicators update correctly

✅ **Visual quality:**
- All schematics render correctly
- Animations smooth (60 FPS)
- Text readable
- Color-coded correctly

### 9.2 Performance Requirements

- **Frame Rate:** Maintain ≥60 FPS with any screen visible
- **Memory Usage:** <2 GB total (all screens loaded)
- **Load Time:** <2 seconds to switch screens
- **Update Rate:** Gauges update at 10 Hz (thermal) or 100 Hz (neutronics)

### 9.3 Validation Requirements

- **Gauge Accuracy:** <1% error for all gauges
- **Control Latency:** <100ms from button press to physics action
- **Alarm Latency:** <200ms from alarm condition to visual indication
- **Test Coverage:** 100% of controls tested, ≥95% of gauges validated

---

## 10. Risks and Mitigation

### 10.1 Technical Risks

**Risk:** Performance degradation with multiple screens loaded in memory
- **Likelihood:** Medium
- **Impact:** High (poor FPS = unusable)
- **Mitigation:** Implement screen pooling (instantiate on demand), optimize gauge update loop, profile and optimize hot paths

**Risk:** Visual component complexity (schematic diagrams) takes longer than estimated
- **Likelihood:** High
- **Impact:** Medium (schedule delay)
- **Mitigation:** Start with simple placeholder visuals, iterate to higher fidelity, prioritize functional over aesthetic

**Risk:** Gauge data not available from physics modules (incomplete implementation)
- **Likelihood:** Low (physics modules well-established)
- **Impact:** Medium (screen incomplete)
- **Mitigation:** Identify all required data early, add physics module accessors if needed, use placeholder values during development

### 10.2 Design Risks

**Risk:** Screen layout does not follow established pattern (deviates from Screen 1)
- **Likelihood:** Medium
- **Impact:** Medium (inconsistent UX)
- **Mitigation:** Use `OperatorScreen` base class, enforce layout zones, peer review designs

**Risk:** Too much information density on combined screens (Screens 7-8)
- **Likelihood:** Medium
- **Impact:** Low (can split screens later)
- **Mitigation:** User testing with operators, willingness to split screens if needed

**Risk:** Alarm system overwhelms operators (too many alarms on complex screens)
- **Likelihood:** Low
- **Impact:** Medium (operator confusion)
- **Mitigation:** Alarm prioritization, grouped alarms by system, acknowledgement workflow

### 10.3 Schedule Risks

**Risk:** Implementation takes longer than 10 stages estimated
- **Likelihood:** High (software always takes longer)
- **Impact:** Low (not time-critical)
- **Mitigation:** Implement stages sequentially, user can stop after any stage, prioritize critical screens first

**Risk:** Testing uncovers major bugs requiring rework
- **Likelihood:** Medium
- **Impact:** Medium (schedule delay)
- **Mitigation:** Test early and often, automated unit tests for screen manager, manual integration tests per stage

---

## 11. Approval and Next Steps

### 11.1 Required Approvals

This Implementation Plan requires explicit user approval before proceeding to Stage 1 implementation.

**User should review and approve:**
1. Screen assignment (Keys 1-9, 0, Tab)
2. Screen layout specifications (Sections 3.2.1 - 3.2.11)
3. Implementation stages (Section 4.2)
4. Unaddressed issues and future work (Section 5)

### 11.2 Questions for User

1. **Screen Priority:** Should certain screens be implemented before others? (Suggested order: 1✅, 2, 3, 4, Tab, 5, 6, 7, 8)
2. **Visual Fidelity:** Should initial implementation use simple placeholder visuals or aim for high-fidelity schematics from the start?
3. **Screen 7-8 Split:** Do Screens 7 (Secondary Systems) and 8 (Auxiliary Systems) have the right groupings, or should they be reorganized?
4. **Animation Complexity:** How important are animated flows and rotating shafts vs. static diagrams with numeric indicators?
5. **Testing Depth:** Should each stage be fully validated before proceeding, or implement all stages then test comprehensively?

### 11.3 Post-Approval Actions

Once approved, the following will be created:

1. **Changelog:** `Operator_Screen_Layout_Changelog_v1_0_0.md`
   - Document all changes to UI architecture
   - Track screen additions per stage
   - Version identically to this Implementation Plan

2. **Future Features:** Update `Critical/Updates/Future_Features/Future_Features.md`
   - Add Screen 9 (Safety Systems) with ECCS requirement
   - Add Screen 0 (Electrical) with bus model requirement
   - Add individual loop resolution for Screen 2

3. **Begin Implementation:** Stage 1 (Screen Management Framework)
   - Create `ScreenManager.cs`
   - Create `OperatorScreen.cs` base class
   - Refactor `ReactorOperatorScreen.cs`
   - Test and validate before proceeding to Stage 2

---

**END OF IMPLEMENTATION PLAN**

**Ready for User Review and Approval**

---

## Appendix A: Screen Layout Visual Reference

### Layout Zones Template

```
┌─────────────────────────────────────────────────────────────────────────┐
│ SCREEN TITLE                              SIM TIME    MODE    COMPRESSION│ 
├──────┬─────────────────────────────────────────────────────────┬─────────┤
│      │                                                          │         │
│      │                                                          │         │
│      │                                                          │ DETAIL  │
│ LEFT │              CENTRAL VISUAL                              │ PANEL   │
│GAUGES│            (Equipment Diagram)                           │(Context │
│      │                                                          │  Info)  │
│ 8-10 │                                                          │         │
│Gauges│                                                          │  OR     │
│      │                                                          │         │
│      │                                                          │ RIGHT   │
│      │                                                          │ GAUGES  │
│      │                                                          │         │
│      │                                                          │ 8-10    │
│      │                                                          │ Gauges  │
├──────┴─────────────────────────────────────────────────────────┴─────────┤
│                                                                           │
│                    BOTTOM CONTROL PANEL                                   │
│     Controls | Status Indicators | Alarms | Mode Selectors               │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

**Zone Percentages (1920×1080):**
- Top Status Bar: 0-3% (title, time, mode)
- Left Gauge Panel: 0-15% width, 3-100% height
- Central Visual: 15-65% width, 3-74% height
- Right Panel: 65-100% width, 3-74% height (gauges OR detail)
- Bottom Control Panel: 0-100% width, 74-100% height

---

## Appendix B: Color Theme and Style Guide

### Color Palette

**Background Colors:**
- Primary Background: `#1A1A1F` (dark charcoal)
- Panel Background: `#1E1E28` (slightly lighter)
- Border/Divider: `#2A2A35` (subtle border)

**Gauge Colors:**
- Normal Range: `#00FF00` (green)
- Caution Range: `#FFFF00` (yellow)
- Danger Range: `#FF0000` (red)
- Needle/Pointer: `#FFFFFF` (white)
- Digital Text: `#00FFFF` (cyan)

**Temperature Gradient:**
- Cold (<200°F): `#0000FF` (blue)
- Warm (200-400°F): `#00FFFF` (cyan)
- Hot (400-550°F): `#00FF00` (green)
- Very Hot (550-600°F): `#FFFF00` (yellow)
- Critical (>600°F): `#FF0000` (red)

**Status Indicators:**
- Running/On: `#00FF00` (green)
- Stopped/Off: `#808080` (gray)
- Tripped/Fault: `#FF0000` (red)
- Transitioning: `#FFFF00` (yellow, pulsing)

**Text:**
- Primary Text: `#FFFFFF` (white)
- Secondary Text: `#CCCCCC` (light gray)
- Disabled Text: `#808080` (gray)
- Alarm Text: `#FF0000` (red)

### Typography

- **Title/Headers:** Bold, 18-24pt
- **Gauge Labels:** Regular, 12-14pt
- **Gauge Values:** Bold, 14-16pt
- **Control Labels:** Regular, 10-12pt
- **Font:** Roboto Mono (monospace for numeric values)

### Animation Guidelines

- **Flow Arrows:** Pulse at 2 Hz, move at 50 px/sec
- **Rotating Shafts:** Rotate at RPM/60 rev/sec (3600 RPM = 60 Hz)
- **Level Indicators:** Update at 10 Hz, smooth interpolation
- **Alarm Flashing:** 2 Hz square wave (0.5 sec on, 0.5 sec off)
- **Button Press:** Highlight for 0.2 sec on click

---

**End of Appendix**
