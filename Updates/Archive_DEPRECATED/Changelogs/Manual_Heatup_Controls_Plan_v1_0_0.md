# Manual Heatup and HZP Stabilization - Implementation Plan

**Version:** 1.0.0  
**Date:** 2026-02-09  
**Author:** AI Development Assistant  
**Status:** Awaiting Approval

---

## 1. Problem Summary

Currently, the simulator has an **automated heatup system** (`HeatupSimEngine.cs`) that automatically progresses the plant from Cold Shutdown (Mode 5) through bubble formation, RCP startup, and toward Hot Zero Power (Mode 3). The automated system makes all control decisions without operator input.

**Goal:** Create a **manual heatup mode** where operators must:
1. Control pressurizer heaters manually
2. Start RCPs when conditions are met
3. Manage CVCS charging/letdown
4. Control boron concentration
5. Monitor plant status and respond to alarms
6. Progress through each phase with proper procedures

This requires determining what **screens and controls** are essential for manual heatup operations, and implementing them in a realistic Westinghouse PWR control room layout.

---

## 2. Analysis: Required Systems for Manual Heatup

### 2.1 Heatup Phase Breakdown

Manual heatup from Cold Shutdown to HZP involves these distinct phases:

| Phase | Mode | Key Activities | Duration | Critical Systems |
|-------|------|----------------|----------|------------------|
| **1. Cold Shutdown** | Mode 5 | Initial conditions check | 0-1 hr | RCS, PZR, CVCS, Alarms |
| **2. Solid Plant Heatup** | Mode 5→4 | PZR heaters ON, water-solid pressurization | 1-8 hr | PZR (heaters), CVCS, RCS |
| **3. Bubble Formation** | Mode 4 | Reach saturation, form steam bubble | 8-9 hr | PZR (critical phase) |
| **4. RCP Preparation** | Mode 4 | Verify pressure > 320 psig, level stable | 9-10 hr | PZR, RCS, CVCS |
| **5. RCP Startup** | Mode 4 | Start RCPs sequentially (4 pumps) | 10-12 hr | RCS Loop, RCPs, CVCS |
| **6. Approach HZP** | Mode 4→3 | Continue heatup to 557°F @ 2235 psig | 12-30 hr | PZR, RCS, SG (heat removal) |
| **7. HZP Stabilization** | Mode 3 | Fine-tune pressure/temperature/level | 30+ hr | All systems |

### 2.2 Critical Control Actions Required

Based on NRC HRTD Section 19.2 (Plant Heatup Procedures), operators must perform:

#### Phase 1-2: Solid Plant Heatup
- ✅ **Enable/disable PZR heaters** (manual control or auto mode)
- ✅ **Adjust heater power level** (20-100%)
- ✅ **Monitor RCS pressure rise rate** (<100 psi/hr per Tech Spec)
- ✅ **Monitor PZR level** (should stay 100% solid until bubble)
- ✅ **Adjust charging/letdown** (maintain RCS inventory)

#### Phase 3: Bubble Formation
- ✅ **Monitor T_sat approach** (pressure → saturation pressure)
- ✅ **Reduce heater power** (prevent overpressure during bubble formation)
- ✅ **Watch PZR level drop** (water → steam conversion expected)
- ✅ **Verify steam bubble established** (level stabilizes 20-30%)

#### Phase 4: RCP Preparation
- ✅ **Verify RCP start criteria:**
  - Pressure ≥ 320 psig (NPSH requirement)
  - Steam bubble established
  - PZR level stable 20-30%
  - Subcooling > 25°F
- ✅ **Ensure charging pumps running**
- ✅ **Verify CVCS auto mode active**

#### Phase 5: RCP Startup
- ✅ **Start RCPs sequentially** (1 → 2 → 3 → 4)
- ✅ **Monitor for:**
  - RCP vibration (each pump)
  - Seal injection flow (each pump)
  - RCS flow increase
  - Temperature changes (mixing)
  - PZR level transient (surge from expansion)
- ✅ **Adjust charging if needed** (compensate for seal flow changes)

#### Phase 6-7: Approach HZP and Stabilization
- ✅ **Control heatup rate** (<50°F/hr typical, <100°F/hr max)
- ✅ **Adjust PZR heater power** (balance heat input vs. removal)
- ✅ **Monitor steam generator heat removal**
- ✅ **Fine-tune pressure** (target 2235 psig)
- ✅ **Fine-tune temperature** (target 557°F)
- ✅ **Stabilize PZR level** (target 50-60%)

### 2.3 Required Monitoring Parameters

Operators must continuously monitor:

**Primary Parameters (always visible):**
- RCS Pressure (psia)
- RCS T-avg (°F)
- PZR Level (%)
- Subcooling Margin (°F)

**Secondary Parameters (check frequently):**
- T-hot, T-cold (per loop if available, or average)
- PZR pressure rate (psi/hr)
- RCS heatup rate (°F/hr)
- Charging flow (gpm)
- Letdown flow (gpm)
- VCT level (%)
- RCP status (running/stopped per pump)
- Seal injection flow (per pump)

**Tertiary Parameters (check periodically):**
- PZR water volume (ft³)
- PZR steam volume (ft³)
- RCS water mass (lb)
- Grid energy consumed (MWh)
- SG heat removal (MW)
- Alarms (any active)

---

## 3. Proposed Screen Requirements for Manual Heatup

### 3.1 Essential Screens

Based on the analysis, **three core screens** are required for manual heatup:

| Screen | Key | Primary Purpose | Critical for Phase |
|--------|-----|-----------------|-------------------|
| **Pressurizer Control** | **3** | Heater control, pressure/level monitoring | All phases (1-7) |
| **RCS Primary Loop** | **2** | RCP start/stop, flow monitoring, temperature | Phases 4-7 (RCP startup onward) |
| **CVCS** | **4** | Charging/letdown balance, VCT level, boron | All phases (1-7) |

**Optional but highly recommended:**
| Screen | Key | Purpose |
|--------|-----|---------|
| **Plant Overview** | **Tab** | See all critical parameters at once, situational awareness |

**Not critical for heatup (can be deferred):**
- Screen 1 (Reactor Core) - Not needed until approaching criticality
- Screen 5 (Steam Generators) - Monitoring only, no active control during heatup
- Screen 6 (Turbine-Generator) - Not online during heatup
- Screens 7-8 (Secondary/Auxiliary) - Not active during heatup

### 3.2 Priority Implementation Order

**Stage 1: Pressurizer Screen (Key 3) - HIGHEST PRIORITY**
- **Why first:** Controls the entire heatup process via heaters
- **Blocks:** Cannot progress past Phase 2 without manual heater control
- **Estimated effort:** 8-12 hours

**Stage 2: CVCS Screen (Key 4) - HIGH PRIORITY**
- **Why second:** Necessary for inventory control once RCPs start
- **Blocks:** Cannot start RCPs safely without CVCS monitoring/control
- **Estimated effort:** 10-14 hours

**Stage 3: RCS Loop Screen (Key 2) - HIGH PRIORITY**
- **Why third:** Enables RCP startup and loop monitoring
- **Blocks:** Cannot progress past Phase 4 without RCP controls
- **Estimated effort:** 10-14 hours

**Stage 4: Plant Overview (Key Tab) - MEDIUM PRIORITY**
- **Why fourth:** Improves situational awareness but not strictly required
- **Does not block:** Can complete heatup without it, just less convenient
- **Estimated effort:** 6-10 hours

---

## 4. Screen 3: Pressurizer Control - Detailed Specification

### 4.1 Layout

**Central Visual:** Pressurizer vessel cutaway
- Vertical cylindrical vessel (proportional to actual 1500 ft³ volume)
- Two-phase region visualization:
  - Water (bottom, blue gradient darker→lighter with depth)
  - Steam (top, light gray/white, subtle animation)
  - Interface line (dynamic, moves with level)
- Heater elements at bottom (4-6 banks, red glow when active)
- Spray nozzles at top (spray animation when active)
- Surge line connection (left side, midpoint, flow arrows in/out)
- Safety/relief valves at top (PORV, SRV symbols)
- Level indicator (vertical bar, 0-100%, with current level highlighted)
- Pressure indicator (digital readout overlaid on vessel)

**Left Gauges (Pressure Control):**
1. **Pressurizer Pressure** (psia)
   - Range: 0-2500 psia
   - Setpoint marker: 2235 psia (HZP target)
   - Alarm bands: <1865 (low), >2385 (high)
   
2. **Pressure Rate** (psi/hr)
   - Range: -200 to +800 psi/hr
   - Zero line marked
   - Alarm: >100 psi/hr (caution), >630 psi/hr (limit during bubble formation)

3. **Pressure Setpoint** (psia, display only)
   - Shows current auto-mode target
   - Updates based on phase (solid plant → bubble formation → pressurize)

4. **Pressure Error** (psi, display only)
   - Actual - Setpoint
   - Color-coded: green (<±5 psi), yellow (±5-20 psi), red (>±20 psi)

5. **Heater Power Total** (kW or MW)
   - Range: 0-1800 kW (0-1.8 MW)
   - Breakdown: Proportional + Backup
   - Real-time power draw

6. **Heater Fraction** (%)
   - Range: 0-100%
   - Shows modulation level
   - Manual mode: user-controlled
   - Auto mode: controller-determined

7. **Spray Flow** (gpm)
   - Range: 0-500 gpm
   - Normally 0 during heatup
   - Activates only if pressure too high

8. **PORV Status**
   - Indicator: CLOSED (green) / OPEN (red)
   - Position: 0-100%

**Right Gauges (Level and Inventory):**
1. **PZR Level** (%)
   - Range: 0-100%
   - Normal band: 20-30% (during bubble), 50-60% (HZP)
   - Alarm: <10% (low-low), >90% (high-high)

2. **Level Setpoint** (%, display only)
   - Auto mode target (not used during heatup)

3. **Level Error** (%, display only)
   - Actual - Setpoint

4. **Surge Flow** (gpm)
   - Range: -200 to +200 gpm
   - Negative = outsurge (PZR → RCS)
   - Positive = insurge (RCS → PZR)
   - Critical during RCP starts and thermal transients

5. **PZR Water Volume** (ft³)
   - Range: 0-1500 ft³
   - Real-time liquid inventory

6. **PZR Steam Volume** (ft³)
   - Range: 0-1500 ft³
   - Real-time vapor inventory
   - Should be 0 until bubble formation

7. **Total RCS Inventory** (gallons or lb)
   - Includes RCS + PZR water (not VCT or BRS)
   - Mass balance validation indicator

8. **Surge Line Temperature** (°F)
   - Range: 0-650°F
   - Indicates thermal stratification
   - Important for bubble formation phase

**Bottom Control Panel (Left → Right):**

**Section 1: Heater Control**
- **Master Heater Enable** (toggle button, large, red/green)
  - OFF (red) / ON (green)
  - Kills all heaters immediately
  
- **Heater Mode Selector** (radio buttons)
  - ⚪ MANUAL - Operator sets power level directly
  - ⚪ AUTO - Controller manages based on pressure
  - ⚪ BUBBLE FORMATION AUTO - Special mode for saturation phase
  - ⚪ PRESSURIZE AUTO - Auto mode for pressurization after bubble

- **Manual Power Slider** (horizontal slider, 20-100%)
  - Active only in MANUAL mode
  - Thumb position shows current setting
  - Numeric readout above slider

- **Power Adjust Buttons** (only in MANUAL mode)
  - **[+10%]** - Increase power by 10%
  - **[+1%]** - Fine adjustment up
  - **[-1%]** - Fine adjustment down
  - **[-10%]** - Decrease power by 10%

**Section 2: Spray Control**
- **Spray Mode** (toggle or radio)
  - ⚪ AUTO (normal)
  - ⚪ MANUAL (override)
  
- **Manual Spray Valve** (slider, 0-100%, only if MANUAL)

**Section 3: PORV Control**
- **PORV Mode** (toggle)
  - ⚪ AUTO (normal, opens at 2335 psia setpoint)
  - ⚪ MANUAL OPEN (emergency manual lift)
  - ⚪ MANUAL CLOSE (block valve, not recommended)

**Section 4: Status and Alarms**
- **Phase Indicator** (text display)
  - Shows current heatup phase (e.g., "SOLID PLANT HEATUP", "BUBBLE FORMATION", "PRESSURIZING")
  
- **Heater Status** (text readout)
  - e.g., "Proportional 45%, Backup OFF"
  - e.g., "Auto PID - 52% (+3 psi error)"

- **Alarm Panel** (5-6 alarm tiles)
  - **PZR PRESSURE HIGH** (>2385 psia)
  - **PZR PRESSURE LOW** (<1865 psia)
  - **PZR LEVEL HIGH** (>90%)
  - **PZR LEVEL LOW** (<10%)
  - **PORV OPEN** (PORV not closed)
  - **HEATER TRIP** (heater breaker open)

**Section 5: Interlocks Display**
- **RCP Start Permissive** (indicator light)
  - GREEN if all criteria met:
    - Pressure ≥ 320 psig ✓
    - Steam bubble established ✓
    - PZR level 20-30% ✓
    - Subcooling > 25°F ✓
  - RED if any criteria not met (text shows which failed)

### 4.2 Control Actions and Feedback

**Manual Heater Control Workflow:**
1. Operator selects "MANUAL" mode
2. Uses slider or +/- buttons to set desired power (20-100%)
3. System immediately applies new power level
4. Heater glow animation updates to match power
5. Pressure rate gauge shows response (increases if power up, decreases if power down)
6. Operator iterates to maintain desired heatup rate

**Auto Heater Control Workflow:**
1. Operator selects appropriate AUTO mode:
   - "BUBBLE FORMATION AUTO" during Phase 3 (approaching saturation)
   - "PRESSURIZE AUTO" during Phases 4-7 (post-bubble pressurization)
2. Controller modulates power automatically based on PID logic
3. Operator monitors pressure rate and error
4. Operator can take over manually if needed

**Spray Control Workflow:**
1. Normally stays in AUTO mode
2. Auto activates spray if pressure exceeds high setpoint
3. Manual override available for testing or emergency cooling

**PORV Control Workflow:**
1. Normally AUTO mode (opens at 2335 psia setpoint)
2. Manual OPEN for emergency depressurization
3. Manual CLOSE to isolate (only if PORV stuck open - abnormal)

### 4.3 Physics Integration

**Data Sources (from existing physics modules):**
- `PressurizerPhysics.PZRState`
  - Pressure_psia
  - WaterVolume_cuft
  - SteamVolume_cuft
  - Level_percent
  - WaterMass_lb
  - SteamMass_lb
  - SurgeFlow_gpm
  - T_pzr_F

- `CVCSController.HeaterControlState`
  - HeaterPower_MW
  - HeaterFraction
  - Mode (enum)
  - StatusReason (string)
  - ProportionalOn (bool)
  - BackupOn (bool)

- `CVCSController.SprayState`
  - SprayFlow_gpm
  - SprayValvePosition (fraction)

- `SolidPlantPressure` or `RCSHeatup`
  - PressureRate_psi_hr

**Control Outputs (to physics modules):**
- `CVCSController.SetHeaterMode(mode)`
- `CVCSController.SetManualHeaterPower(fraction)`
- `CVCSController.SetSprayMode(mode)`
- `CVCSController.SetManualSprayValve(position)`
- `CVCSController.SetPORVMode(mode)` (if PORV control implemented)

### 4.4 Visual Design Notes

**Heater Element Animation:**
- 6 heater banks drawn as horizontal bars at bottom of vessel
- Color intensity based on power level:
  - 0-20%: Dark gray (off/standby)
  - 20-50%: Dull red
  - 50-80%: Bright red
  - 80-100%: Bright orange-red (glowing hot)
- Subtle pulsing animation when active (0.5 Hz)

**Spray Animation:**
- Spray nozzles at top of vessel
- When spray active (flow > 0):
  - Fine mist/droplet particles fall from nozzles
  - Particle intensity proportional to flow rate
  - Particles fade as they fall (heat absorption visual)

**Surge Flow Animation:**
- Surge line pipe on left side of vessel
- Arrows inside pipe:
  - Flow IN (up arrows, green): RCS → PZR (insurge)
  - Flow OUT (down arrows, blue): PZR → RCS (outsurge)
  - Arrow speed proportional to flow rate
- Numeric flow rate displayed next to pipe (+/-XXX gpm)

**Level Interface:**
- Smooth animated water level (interpolated updates at 10 Hz)
- Steam-water interface is not a sharp line (realistic transition zone, ~5% height)
- Slight wave/ripple effect on interface during surge transients

---

## 5. Screen 4: CVCS - Detailed Specification

### 5.1 Layout

**Central Visual:** CVCS flow diagram (simplified)
- Volume Control Tank (VCT) - Large vertical tank, center-left
  - Level indicator (0-100%)
  - Inlet/outlet flow arrows
- Charging Pumps (CCPs) - 3 pumps shown (CCP-A, CCP-B, CCP-C)
  - Pump symbols with status (running=green, stopped=gray)
- Letdown line from RCS (top-right entering diagram)
  - Flow direction arrow
  - Orifice symbol (flow control)
- Charging line to RCS (top-right exiting diagram)
  - Flow direction arrow
- Seal injection branches (4 branches to RCPs)
  - Split off from charging line
  - 4 arrows pointing to "RCP 1-4 Seals"
- Boration/dilution flow paths
  - Borated water addition line (dashed)
  - Demineralized water addition line (dashed)
- Demineralizers (ion exchange beds, shown as filter symbols)
- Flow meters on each major line (gpm indicators)

**Left Gauges (Flow and Inventory):**
1. **Charging Flow to RCS** (gpm)
   - Range: 0-150 gpm
   - Normal: 75 gpm (no RCPs), 110-120 gpm (4 RCPs running)
   
2. **Letdown Flow from RCS** (gpm)
   - Range: 0-150 gpm
   - Normal: 75 gpm steady

3. **Net CVCS Flow** (gpm, +/-)
   - Range: -100 to +100 gpm
   - Positive = inventory INTO system
   - Negative = inventory OUT OF system
   - Zero line marked

4. **Seal Injection Total** (gpm)
   - Range: 0-40 gpm
   - 8 gpm/pump × RCP count
   - Display breakdown: "32 gpm (4 RCPs × 8 gpm)"

5. **VCT Level** (%)
   - Range: 0-100%
   - Normal band: 25-75%
   - Alarm: <10% (low-low), >85% (high-high)

6. **VCT Temperature** (°F)
   - Range: 50-200°F
   - Normal: 100-120°F

7. **VCT Pressure** (psig)
   - Range: 0-100 psig
   - Normal: 15-25 psig

8. **CCP Discharge Pressure** (psig)
   - Range: 0-3000 psig
   - Normal: ~2400 psig (overcome RCS pressure + margin)

**Right Gauges (Chemistry and Boron):**
1. **RCS Boron Concentration** (ppm)
   - Range: 0-2500 ppm
   - Typical during heatup: 1800-2000 ppm

2. **VCT Boron Concentration** (ppm)
   - Range: 0-2500 ppm
   - Should track RCS boron closely

3. **Boron Worth** (pcm, derived)
   - Reactivity contribution from current boron
   - Display: "-18,000 pcm @ 1800 ppm"

4. **Boration Flow** (gpm)
   - Range: 0-50 gpm
   - Active only when adding boron

5. **Dilution Flow** (gpm)
   - Range: 0-50 gpm
   - Active only when diluting

6. **Charging Temperature** (°F)
   - Range: 50-200°F
   - Should be close to letdown temp

7. **Letdown Temperature** (°F)
   - Range: 400-600°F
   - Hot water from RCS, cooled by regenerative HX

8. **Purification Flow** (gpm)
   - Range: 0-150 gpm
   - Flow through demineralizers

**Bottom Control Panel:**

**Section 1: Charging Pump Control**
- **CCP-A** (button, green=running, gray=stopped)
  - [START] / [STOP] buttons
  - Status indicator: "RUNNING 2400 psig" or "STOPPED"
  
- **CCP-B** (same as CCP-A)
  
- **CCP-C** (same as CCP-A)

- **Auto-Start Enable** (toggle)
  - When ON: CCPs auto-start if VCT level < 10%
  - When OFF: Manual start only

**Section 2: Letdown Control**
- **Letdown Valve Position** (slider, 0-100%)
  - Controls letdown orifice opening
  - Determines letdown flow rate
  - Normal: ~60-70% for 75 gpm

- **Letdown Mode** (radio buttons)
  - ⚪ MANUAL - Operator controls valve directly
  - ⚪ AUTO - PI controller maintains VCT level

- **Letdown Flow Setpoint** (numeric input, gpm)
  - Target flow in AUTO mode
  - Default: 75 gpm

**Section 3: Boration/Dilution**
- **Boration/Dilution Mode** (selector)
  - ⚪ OFF (normal during heatup)
  - ⚪ BORATE (add boron)
  - ⚪ DILUTE (add demineralized water)

- **Boration Rate** (slider, gpm, 0-30 gpm)
  - Active only in BORATE mode
  
- **Dilution Rate** (slider, gpm, 0-30 gpm)
  - Active only in DILUTE mode

**Section 4: VCT Level Control**
- **VCT Level Setpoint** (slider or +/- buttons, %)
  - Range: 30-70%
  - Default: 50%
  - PI controller targets this level via letdown adjustment

- **Level Control Mode** (display only)
  - Shows: "AUTO PI" or "MANUAL"

**Section 5: Status and Alarms**
- **Inventory Balance Display** (text)
  - Shows: "Net +5.2 gpm (Into System)"
  - Updates real-time

- **Alarm Panel**
  - **VCT LEVEL HIGH** (>85%)
  - **VCT LEVEL LOW** (<10%)
  - **ALL CCPs STOPPED** (no charging pumps running)
  - **LETDOWN ISOLATED** (letdown flow < 5 gpm)
  - **BORON OUT OF SPEC** (RCS boron deviation from target)

### 5.2 Control Actions

**Charging Pump Startup:**
1. Operator clicks [START] on CCP-A, CCP-B, or CCP-C
2. System checks interlock: VCT level > 5% (minimum for pump suction)
3. If OK: Pump starts, discharge pressure rises to ~2400 psig
4. Charging flow increases immediately
5. Status changes to "RUNNING"

**Charging Pump Stop:**
1. Operator clicks [STOP]
2. Pump immediately stops
3. Discharge pressure drops to 0
4. Charging flow decreases
5. **Warning if all pumps stopped:** "ALL CHARGING PUMPS STOPPED - RCS INVENTORY LOSS"

**Letdown Control (Manual):**
1. Operator selects "MANUAL" mode
2. Uses slider to adjust letdown valve position
3. Flow rate changes proportionally
4. Operator balances manually against charging flow

**Letdown Control (Auto):**
1. Operator selects "AUTO" mode and sets VCT level setpoint
2. PI controller adjusts letdown valve to maintain target VCT level
3. If VCT level > setpoint: Controller increases letdown (more flow out)
4. If VCT level < setpoint: Controller decreases letdown (less flow out, or increases charging if needed)

**Boration:**
1. Operator selects "BORATE" mode
2. Sets boration rate (gpm)
3. Borated water flows into VCT
4. RCS boron concentration increases over time (requires ~1 hour for mixing)
5. Used to increase shutdown margin or control reactivity

**Dilution:**
1. Operator selects "DILUTE" mode
2. Sets dilution rate (gpm)
3. Demineralized water flows into VCT
4. RCS boron concentration decreases over time
5. Used to reduce boron before approach to criticality

### 5.3 Physics Integration

**Data Sources:**
- `VCTPhysics.VCTState`
  - Volume_gal
  - Level_percent
  - Temperature_F
  - Pressure_psig
  - BoronConcentration_ppm
  - NetFlow_gpm
  
- `CVCSController.CVCSState`
  - ChargingFlow_gpm
  - LetdownFlow_gpm
  - SealInjectionTotal_gpm
  - LetdownValvePosition
  - Mode (auto/manual)
  - Setpoint_percent
  
- `CVCSController.BorationState` (if exists, or add)
  - BorationFlow_gpm
  - DilutionFlow_gpm
  - Mode (off/borate/dilute)

**Control Outputs:**
- `CVCSController.StartCCP(pump_id)`
- `CVCSController.StopCCP(pump_id)`
- `CVCSController.SetLetdownMode(manual/auto)`
- `CVCSController.SetLetdownValvePosition(fraction)`
- `CVCSController.SetLetdownFlowSetpoint(gpm)`
- `CVCSController.SetVCTLevelSetpoint(percent)`
- `CVCSController.SetBorationMode(off/borate/dilute)`
- `CVCSController.SetBorationRate(gpm)`
- `CVCSController.SetDilutionRate(gpm)`

---

## 6. Screen 2: RCS Primary Loop - Detailed Specification

### 6.1 Layout

**Central Visual:** 4-Loop RCS Schematic
- Reactor vessel (center, vertical cylinder)
- 4 hot legs radiating outward (red color-coded)
  - Labeled: Loop A, Loop B, Loop C, Loop D (or 1, 2, 3, 4)
- 4 steam generators (one per loop, U-tube symbols)
- 4 cold legs returning to reactor (blue color-coded)
- 4 RCPs in cold legs (pump symbols)
  - Status: RUNNING (green, animated rotation) or STOPPED (gray, static)
- Pressurizer connection (to one hot leg, typically Loop A)
- Flow direction arrows (animated, speed proportional to flow)
- Temperature gradient coloring:
  - Hot legs: Red gradient (darker = hotter)
  - Cold legs: Blue gradient (darker = colder)

**Left Gauges (Loop Temperatures):**
1. **Loop A T-hot** (°F) - Range: 0-700°F
2. **Loop B T-hot** (°F)
3. **Loop C T-hot** (°F)
4. **Loop D T-hot** (°F)
5. **Loop A T-cold** (°F) - Range: 0-700°F
6. **Loop B T-cold** (°F)
7. **Loop C T-cold** (°F)
8. **Loop D T-cold** (°F)

**Right Gauges (Flow and Power):**
1. **Total RCS Flow** (gpm)
   - Range: 0-400,000 gpm
   - Normal: 390,400 gpm (all 4 RCPs)
   
2. **Loop A Flow** (gpm) - Range: 0-100,000 gpm
3. **Loop B Flow** (gpm)
4. **Loop C Flow** (gpm)
5. **Loop D Flow** (gpm)

6. **Core Thermal Power** (MWt)
   - Range: 0-3500 MWt
   - During heatup: 0 MWt (decay heat only)

7. **Core ΔT** (°F)
   - Range: 0-80°F
   - T-hot - T-cold average
   - During heatup: ~0°F (no fission power)

8. **Average T-avg** (°F)
   - Range: 0-700°F
   - (T-hot + T-cold) / 2

**Bottom Control Panel:**

**Section 1: RCP Controls (4 columns, one per pump)**

Each pump has:
- **Pump Label:** "RCP-A" (or 1, 2, 3, 4)
- **Status Indicator:** RUNNING (green) / STOPPED (gray)
- **[START]** button (large, green)
- **[STOP]** button (large, red)
- **Speed Indicator:** "3600 RPM" (when running)
- **Vibration Indicator:** "NORMAL" (green) / "HIGH" (yellow) / "ALARM" (red)
- **Seal Injection:** "8.0 gpm" (flow rate to seals)

**Start Button Logic:**
- Enabled only if:
  - RCS pressure ≥ 320 psig (NPSH) ✓
  - Steam bubble established ✓
  - PZR level 20-30% ✓
  - Subcooling > 25°F ✓
- Grayed out and shows tooltip: "RCP START BLOCKED - [reason]" if criteria not met

**Section 2: Natural Circulation Indicator**
- **Mode Display:**
  - "FORCED CIRCULATION" (any RCPs running)
  - "NATURAL CIRCULATION" (all RCPs stopped, but temperature > 300°F)
  - "STAGNANT" (all stopped, cold shutdown)

- **Natural Circ Flow:** "~5,000 gpm" (estimated, display only)

**Section 3: Loop Status Summary**
- **Total RCPs Running:** "4 / 4"
- **Total Flow:** "390,400 gpm"
- **RCP Power Draw:** "21.0 MW" (calculated, 4 pumps × 5.25 MW)

**Section 4: Alarms**
- **RCP TRIP** (any pump stopped unexpectedly)
- **LOW RCS FLOW** (flow < 80% of expected)
- **HIGH RCP VIBRATION** (abnormal vibration on any pump)
- **SEAL INJECTION LOW** (< 7 gpm on any pump)

### 6.2 Control Actions

**RCP Start Sequence:**
1. Operator verifies start criteria met (permissive light green)
2. Clicks [START] on RCP-A
3. System checks interlocks programmatically
4. If OK: RCP-A starts, speed ramps from 0 → 3600 RPM over ~30 seconds
5. Flow increases from previous total by ~97,600 gpm
6. RCS begins mixing (thermal transient)
7. PZR level may drop transiently (CVCS compensates)
8. After RCP-A stabilizes (~1 minute), start criteria re-check for RCP-B
9. Repeat for RCP-B, RCP-C, RCP-D (sequential, ~30 seconds apart per plant procedures)

**RCP Stop (Emergency):**
1. Operator clicks [STOP] on any running RCP
2. Pump immediately begins coastdown
3. Speed decreases exponentially: 3600 → 1800 → 900 → 0 RPM over ~60 seconds
4. Flow decreases proportionally
5. Alarms activate: "RCP TRIP - VERIFY NO SEAL DAMAGE"
6. Operator must investigate cause before restarting

**Monitoring During RCP Transients:**
- Watch PZR level (should recover within 5 minutes)
- Watch RCS pressure (may spike slightly due to pump work)
- Watch temperatures (mixing causes T-hot/T-cold convergence initially)
- Watch charging flow (should increase to compensate for seal flow)

### 6.3 Physics Integration

**Data Sources:**
- `RCPSequencer.RCPState` (per pump)
  - IsRunning
  - Speed_rpm
  - FlowRate_gpm
  - Vibration (enum: normal, high, alarm)
  - SealInjectionFlow_gpm
  
- `LoopThermodynamics` or equivalent
  - T_hot_A, T_hot_B, T_hot_C, T_hot_D (if individual loops modeled)
  - T_cold_A, T_cold_B, T_cold_C, T_cold_D
  - Or just T_hot, T_cold (averaged)
  
- `FluidFlow` or equivalent
  - TotalFlow_gpm
  - LoopFlow_gpm (per loop, if modeled)

**Control Outputs:**
- `RCPSequencer.RequestStart(pump_id)` - Operator requests RCP start
- `RCPSequencer.RequestStop(pump_id)` - Operator requests RCP stop
- `RCPSequencer.CheckStartCriteria()` - Returns bool + reason string

---

## 7. Screen Tab: Plant Overview - Detailed Specification

### 7.1 Layout

**Central Visual:** Simplified plant mimic diagram (single page)
- Reactor core (center, simple rectangle)
- 4 hot legs radiating outward (red lines)
- 4 steam generators (SG symbols, U-tubes)
- 4 cold legs returning (blue lines)
- Pressurizer (connected to one hot leg)
- 4 RCPs (in cold legs, status color-coded)
- Turbine-generator (right side, grayed out during heatup)
- Condenser (below turbine, grayed out)
- Feedwater train (bottom, grayed out)
- Critical parameters overlaid on diagram as text labels

**Left Gauges (Nuclear/Primary):**
1. **Reactor Power** (%)
   - Range: 0-120%
   - During heatup: 0% (no fission)
   
2. **T-avg** (°F)
   - Range: 0-700°F
   
3. **RCS Pressure** (psia)
   - Range: 0-2500 psia
   
4. **PZR Level** (%)
   - Range: 0-100%
   
5. **Total RCS Flow** (gpm)
   - Range: 0-400,000 gpm
   
6. **Control Rod Position** (steps)
   - Range: 0-228 steps
   - During heatup: 0 steps (fully inserted)
   
7. **Boron Concentration** (ppm)
   - Range: 0-2500 ppm
   
8. **Subcooling Margin** (°F)
   - Range: 0-400°F

**Right Gauges (Secondary/Output):**
1. **SG Average Level** (%)
   - Range: 0-100%
   - Average of 4 SGs
   
2. **Steam Pressure** (psia)
   - Range: 0-1200 psia
   
3. **Feedwater Flow** (lb/hr)
   - Range: 0-15,000,000 lb/hr
   - During heatup: minimal or 0
   
4. **Turbine Power** (MWe)
   - Range: 0-1200 MWe
   - During heatup: 0 MWe (turbine offline)
   
5. **Generator Output** (MWe)
   - Range: 0-1200 MWe
   - During heatup: 0 MWe
   
6. **Condenser Vacuum** (in Hg)
   - Range: 0-30 in Hg
   - During heatup: N/A (condenser not under vacuum)
   
7. **Feedwater Temperature** (°F)
   - Range: 0-500°F
   
8. **Main Steam Flow** (lb/hr)
   - Range: 0-15,000,000 lb/hr

**Bottom Panel:**

**Section 1: Mode and Status**
- **Plant Mode Indicator** (large text)
  - "MODE 5 - COLD SHUTDOWN"
  - "MODE 4 - HOT SHUTDOWN"
  - "MODE 3 - HOT STANDBY (HZP)"
  - "MODE 2 - STARTUP"
  - "MODE 1 - POWER OPERATION"

- **Heatup Phase** (text)
  - "SOLID PLANT HEATUP"
  - "BUBBLE FORMATION"
  - "RCP STARTUP"
  - "APPROACH HZP"
  - "HZP STABILIZATION"

**Section 2: Major Equipment Status**
- **RCPs:** "4 / 4 RUNNING" (green) or "0 / 4 STOPPED" (gray)
- **Turbine:** "OFFLINE" (gray during heatup)
- **Generator:** "OFFLINE" (gray)
- **CVCS:** "AUTO" (green) or "MANUAL" (yellow)
- **PZR Heaters:** "ON" (green) or "OFF" (gray)

**Section 3: Time and Progress**
- **Simulation Time:** "12:34:56" (elapsed since start)
- **Time Compression:** "10x" (current acceleration)
- **Wall Clock:** "14:22:18" (real time of day)
- **Estimated Time to HZP:** "18.5 hours" (calculated based on current heatup rate)

**Section 4: Major Alarm Summary**
- **Alarm tiles grouped by system:**
  - **RCS** (e.g., "PRESSURE HIGH")
  - **PZR** (e.g., "LEVEL LOW")
  - **CVCS** (e.g., "VCT LEVEL LOW")
  - **RCPs** (e.g., "RCP TRIP")
  - **Steam** (none expected during heatup)

- **Total Active Alarms:** "2 ALARMS ACTIVE"

**Section 5: Emergency Actions**
- **[REACTOR TRIP]** (large red button) - Fully insert all rods (not applicable during heatup, grayed out)
- **[TURBINE TRIP]** (red button) - Stop turbine (grayed out during heatup)
- **[PAUSE SIMULATION]** (yellow button) - Freeze time for review

### 7.2 Purpose

**Not for control** - This screen is **read-only** for situational awareness.

Operators use this screen to:
- Get a quick overview of plant state
- See all critical parameters at once without switching screens
- Identify which system needs attention (e.g., if alarm active, see which screen to switch to)
- Monitor overall progress toward HZP
- Confirm all major systems are in expected state

**To take action,** operators must switch to the appropriate control screen (2, 3, or 4).

---

## 8. Implementation Stages and Effort Estimates

### Stage 1: Screen Management Framework
**Already planned** in Operator Screen Layout Plan v1.0.0 - no additional work needed here.

### Stage 2: Screen 3 - Pressurizer Control
**Estimated Effort:** 10-14 hours

**Breakdown:**
- **Visual Component (PZR Vessel Cutaway):** 4-6 hours
  - Draw vessel geometry
  - Implement level animation
  - Heater glow effects
  - Spray particle system
  - Surge line flow arrows

- **Gauges (8 left + 8 right):** 2-3 hours
  - Wire up gauge data bindings
  - Configure ranges, alarm bands, setpoints
  - Test gauge updates

- **Bottom Control Panel:** 3-4 hours
  - Heater control buttons and mode selector
  - Spray control interface
  - PORV control interface
  - Alarm panel
  - Interlock permissive display

- **Physics Integration:** 1-2 hours
  - Connect control inputs to `CVCSController` heater methods
  - Read state from `PressurizerPhysics` and `CVCSController`
  - Test control loop

### Stage 3: Screen 4 - CVCS
**Estimated Effort:** 12-16 hours

**Breakdown:**
- **Visual Component (CVCS Flow Diagram):** 5-7 hours
  - Draw VCT, pumps, piping layout
  - Flow direction arrows (animated)
  - Pump status indicators
  - Valve symbols

- **Gauges (8 left + 8 right):** 2-3 hours

- **Bottom Control Panel:** 4-5 hours
  - CCP start/stop buttons (×3)
  - Letdown valve control
  - Boration/dilution controls
  - VCT level setpoint
  - Alarm panel

- **Physics Integration:** 1-2 hours
  - Connect to `CVCSController` and `VCTPhysics`
  - Test charging/letdown control
  - Test boration (if implemented in physics, or stub for future)

### Stage 4: Screen 2 - RCS Primary Loop
**Estimated Effort:** 10-14 hours

**Breakdown:**
- **Visual Component (4-Loop Schematic):** 4-6 hours
  - Draw reactor, hot legs, SGs, cold legs, RCPs
  - Color-coded temperature gradients
  - Flow direction animations
  - RCP rotation animation

- **Gauges (8 left + 8 right):** 2-3 hours

- **Bottom Control Panel:** 3-4 hours
  - RCP start/stop buttons (×4)
  - Status indicators per pump
  - Natural circulation display
  - Alarm panel

- **Physics Integration:** 1-2 hours
  - Connect to `RCPSequencer`
  - Test RCP start criteria checking
  - Test sequential startup

### Stage 5: Screen Tab - Plant Overview
**Estimated Effort:** 8-12 hours

**Breakdown:**
- **Visual Component (Plant Mimic Diagram):** 4-6 hours
  - Simplified full-plant schematic
  - Component status color-coding
  - Parameter labels overlaid on diagram

- **Gauges (8 left + 8 right):** 2-3 hours

- **Bottom Panel (Status and Alarms):** 2-3 hours
  - Mode indicator
  - Equipment status tiles
  - Alarm summary
  - Emergency action buttons

**No control actions** - read-only screen, no physics control integration needed.

### Stage 6: Manual Mode Toggle and Integration
**Estimated Effort:** 4-6 hours

**Add to simulation engine:**
- **Manual/Auto Mode Selector** (global or per screen)
- **Disable automated logic** when manual mode active
- **Operator control inputs** override automated decisions
- **Safety interlocks** still enforced (e.g., cannot start RCP if pressure too low)

---

## 9. Testing and Validation Strategy

### 9.1 Unit Testing (Per Screen)

**Screen 3 (Pressurizer):**
- Test heater power adjustment (manual mode)
- Test auto mode transitions
- Test spray activation
- Test PORV manual override
- Verify gauge accuracy vs. physics module data

**Screen 4 (CVCS):**
- Test CCP start/stop
- Test letdown valve control (manual and auto)
- Test boration/dilution (if implemented)
- Verify VCT level PI controller response

**Screen 2 (RCS Loop):**
- Test RCP start criteria enforcement
- Test sequential RCP startup
- Test RCP trip (emergency stop)
- Verify flow and temperature display accuracy

**Screen Tab (Overview):**
- Verify all gauges update correctly
- Verify mode progression display
- Verify alarm summary shows active alarms

### 9.2 Integration Testing (Full Manual Heatup)

**Test Case 1: Cold Shutdown → Bubble Formation**
1. Start at Mode 5 (cold, solid PZR)
2. Manually enable PZR heaters at 100%
3. Monitor pressure rise
4. Observe PZR level staying 100% (solid)
5. When pressure reaches ~420-430 psia, switch heaters to "BUBBLE FORMATION AUTO"
6. Watch level drop to 20-30% as steam bubble forms
7. Verify bubble stabilization

**Expected Duration:** 8-10 hours simulated time  
**Success Criteria:**
- Bubble forms at correct pressure (~435 psia)
- Level stabilizes at 20-30%
- No alarms triggered (except expected "PZR LEVEL LOW" cleared after stabilization)

**Test Case 2: RCP Startup**
1. Starting conditions: Bubble established, pressure ~900 psia, level 25%
2. Verify RCP start permissive is GREEN
3. Start RCP-A manually
4. Wait 30 seconds, observe:
   - RCS flow increases
   - PZR level may drop transiently
   - Charging flow increases (CVCS compensates)
5. Repeat for RCP-B, RCP-C, RCP-D

**Expected Duration:** 1-2 hours simulated time  
**Success Criteria:**
- All 4 RCPs running
- Total flow ~390,000 gpm
- PZR level recovers to 25-30%
- No RCP trips or vibration alarms

**Test Case 3: Full Manual Heatup (Cold → HZP)**
1. Start at Cold Shutdown
2. Manually control heaters through all phases
3. Manually start RCPs when ready
4. Monitor and adjust CVCS charging/letdown as needed
5. Progress to HZP (557°F, 2235 psia, 50-60% PZR level)

**Expected Duration:** 25-35 hours simulated time (depends on heatup rate)  
**Success Criteria:**
- Reach HZP conditions
- All parameters within normal bands
- No safety limit violations
- Mass balance conserved (<1% error)

### 9.3 Performance Testing

- **Frame Rate:** Maintain 60 FPS with all screens loaded
- **Screen Switching:** <100ms transition time
- **Gauge Update Rate:** 10 Hz smooth updates
- **Control Latency:** <50ms from button press to physics response

---

## 10. Required Physics Module Additions

Most physics is already implemented in the automated heatup system. However, manual mode requires some new interfaces:

### 10.1 Manual Control Interfaces (CVCSController.cs)

**Add public methods:**
```csharp
// Heater control
public void SetHeaterMode(HeaterMode mode);
public void SetManualHeaterPower(float fraction);  // 0.20 to 1.00

// Spray control
public void SetSprayMode(SprayMode mode);  // auto, manual
public void SetManualSprayValve(float position);  // 0.0 to 1.0

// Letdown control
public void SetLetdownMode(LetdownMode mode);  // auto, manual
public void SetManualLetdownValve(float position);

// Boration/dilution (if not already present)
public void SetBorationMode(BorationMode mode);  // off, borate, dilute
public void SetBorationRate(float gpm);
public void SetDilutionRate(float gpm);

// CCP control
public void StartCCP(int pump_id);  // 0, 1, 2 for CCP-A, B, C
public void StopCCP(int pump_id);
```

### 10.2 RCP Control Interfaces (RCPSequencer.cs)

**Add public methods:**
```csharp
// Manual RCP control
public bool RequestStart(int rcp_id);  // Returns true if start allowed
public void RequestStop(int rcp_id);

// Start criteria checking
public struct RCPStartCriteria
{
    public bool PressureOK;  // >= 320 psig
    public bool BubbleOK;    // Steam bubble established
    public bool LevelOK;     // PZR level 20-30%
    public bool SubcoolingOK; // > 25°F
    public string FailureReason;  // If any criterion failed
}

public RCPStartCriteria CheckStartCriteria(int rcp_id);
```

### 10.3 Mode Management

**Add to HeatupSimEngine or new ManualModeController:**
```csharp
public enum OperatorMode
{
    AUTOMATED,  // Existing auto-heatup logic
    MANUAL      // Operator drives all actions
}

public OperatorMode CurrentMode { get; set; }

// When in MANUAL mode:
// - Disable all automated heatup logic
// - Disable RCP sequencer auto-start
// - Disable heater auto-control (unless operator selects AUTO sub-mode)
// - Still enforce safety interlocks
```

---

## 11. Documentation Requirements

### 11.1 Operator Procedures Manual

Create new document: **Manual_Heatup_Procedures.md**

Contents:
1. **Prerequisites for Heatup**
   - Initial plant conditions
   - Systems required to be operational
   - Personnel requirements

2. **Phase-by-Phase Procedures**
   - Phase 1: Cold Shutdown (initial conditions check)
   - Phase 2: Solid Plant Heatup (heater control)
   - Phase 3: Bubble Formation (critical phase procedures)
   - Phase 4: RCP Preparation (start criteria verification)
   - Phase 5: RCP Startup (sequential startup procedure)
   - Phase 6: Approach HZP (heatup rate control)
   - Phase 7: HZP Stabilization (fine-tuning)

3. **Normal Parameter Ranges**
   - Expected values at each phase
   - Alarm setpoints
   - Action levels

4. **Abnormal Conditions**
   - What to do if heatup rate too fast (>100°F/hr)
   - What to do if PZR level drops unexpectedly
   - What to do if RCP fails to start
   - What to do if bubble formation fails

### 11.2 Screen User Guides

**One guide per screen:**
- Screen 3: Pressurizer Control User Guide
- Screen 4: CVCS User Guide
- Screen 2: RCS Loop and RCP Startup User Guide
- Screen Tab: Plant Overview Guide

Each guide includes:
- Screen layout diagram with labeled zones
- Purpose of each gauge
- Description of each control
- Step-by-step walkthroughs for common tasks
- Warning notes and interlocks

---

## 12. Success Criteria

### 12.1 Functional Requirements

✅ **Manual heatup capability:**
- Operator can progress from Cold Shutdown to HZP without automated system
- All required controls are available and functional
- Safety interlocks prevent unsafe operations

✅ **Three core screens operational:**
- Screen 3 (Pressurizer) - complete and tested
- Screen 4 (CVCS) - complete and tested
- Screen 2 (RCS Loop) - complete and tested

✅ **Optional overview screen:**
- Screen Tab (Overview) - provides situational awareness

✅ **Realistic procedures:**
- Heatup procedures match Westinghouse 4-Loop PWR practices
- Timing and sequencing realistic
- Parameter ranges match technical specifications

### 12.2 Performance Requirements

- **Frame Rate:** 60 FPS maintained during manual operations
- **Control Responsiveness:** <50ms latency from button press to action
- **Gauge Accuracy:** <1% error vs. physics module data
- **Simulation Stability:** No crashes or freezes during 30+ hour simulated heatup

### 12.3 Validation Requirements

- **Complete one full manual heatup** from Cold Shutdown to HZP
- **Mass balance conserved** (<1% error over full heatup)
- **All phases completed** without safety violations
- **Operator procedures documented** and followed successfully

---

## 13. Risks and Mitigation

### 13.1 Technical Risks

**Risk:** Physics modules not designed for manual control (missing interfaces)
- **Likelihood:** Medium
- **Impact:** High (blocks implementation)
- **Mitigation:** Audit physics modules early, add necessary control methods

**Risk:** Control latency too high (>100ms)
- **Likelihood:** Low
- **Impact:** Medium (poor user experience)
- **Mitigation:** Use direct method calls, avoid message queues, profile hot paths

**Risk:** Screen complexity causes performance degradation
- **Likelihood:** Medium
- **Impact:** Medium (poor FPS)
- **Mitigation:** Optimize render loops, use object pooling for animations

### 13.2 Design Risks

**Risk:** Control layout doesn't match actual plant panels
- **Likelihood:** Medium (we're designing from documentation, not photos)
- **Impact:** Medium (training value reduced)
- **Mitigation:** Research actual Westinghouse 4-Loop control room layouts (photos, videos if available)

**Risk:** Too many controls overwhelm operator
- **Likelihood:** Low (we're keeping it minimal)
- **Impact:** Low
- **Mitigation:** Prioritize essential controls only, defer nice-to-haves

### 13.3 Schedule Risks

**Risk:** Implementation takes longer than estimated (40-50 hours)
- **Likelihood:** High (software estimates always optimistic)
- **Impact:** Low (not time-critical)
- **Mitigation:** Implement incrementally, validate each screen before moving to next

---

## 14. Approval and Next Steps

### 14.1 Questions for User

1. **Screen Priority:** Confirm implementation order:
   - Stage 2: Screen 3 (Pressurizer) FIRST
   - Stage 3: Screen 4 (CVCS) SECOND
   - Stage 4: Screen 2 (RCS Loop) THIRD
   - Stage 5: Screen Tab (Overview) FOURTH
   
2. **Visual Fidelity:** Should we aim for high-fidelity schematics from the start, or simple placeholders initially?

3. **Automation Level:** Should we keep automated heatup as an option (selectable mode), or replace it entirely with manual control?

4. **Additional Systems:** Are there any other systems you think are critical for manual heatup that I haven't included?

5. **Westinghouse Reference:** Do you have access to actual Westinghouse 4-Loop control room photos or training materials we should reference?

### 14.2 Post-Approval Actions

Once approved:

1. **Copy Relevant Technical Documentation:**
   - NRC HRTD Section 19.2 (Plant Heatup)
   - NRC HRTD Section 6.1 (Pressurizer Operations)
   - NRC HRTD Section 4.1 (CVCS)
   - NRC HRTD Section 3.2 (RCPs)
   - Copy to `C:\Users\craig\Projects\Critical\Technical_Documentation\`

2. **Create Changelog:** `Manual_Heatup_Changelog_v1_0_0.md`

3. **Begin Implementation:** Start with Stage 2 (Screen 3 - Pressurizer)

---

**END OF IMPLEMENTATION PLAN**

**Ready for User Review and Approval**
