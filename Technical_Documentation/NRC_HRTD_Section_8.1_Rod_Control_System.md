# NRC HRTD Section 8.1 — Rod Control System

**Source:** https://www.nrc.gov/docs/ML1122/ML11223A252.pdf  
**Retrieved:** 2026-02-14  
**Revision:** Rev 0209

---

## Overview

This document provides comprehensive technical details on the Westinghouse PWR Rod Control System including:

- Rod drive mechanism (magnetic jack assembly) design and operation
- Automatic vs manual rod control modes
- Power mismatch and temperature mismatch control circuits
- Bank sequencing and overlap programming
- Logic cabinet (pulser, master cycler, slave cyclers, bank overlap unit)
- Power cabinet operation and thyristor control
- Rod withdrawal stops and interlocks
- Shutdown vs control rod functions

---

## Key Technical Data for Simulator Development

### Rod Categories and Organization

**Shutdown Rods:**
- **Banks:** A, B, C, D
- **Function:** Fully withdrawn during power operation, provide large negative reactivity on reactor trip
- **Banks C & D:** 4 rods each (no group subdivision)
- **Banks A & B:** Subdivided into groups
- **Speed:** 64 steps/min (preset adjustable)

**Control Rods:**
- **Banks:** A, B, C, D
- **Function:** Reactivity control for startup, power operations, and reactor trip
- **Organization:** Each bank subdivided into Group 1 and Group 2
- **Withdrawal Sequence:** A, B, C, D
- **Insertion Sequence:** D, C, B, A (opposite of withdrawal)

---

## Control Rod Drive Mechanism (CRDM)

**Design:** Electromagnetic jack (mag jack) - magnetic jack assembly

**Components:**
- Stationary gripper latches (hold rod when not moving)
- Movable gripper latches (raise/lower rod drive shaft)
- Lift coil (raises movable grippers)
- Stationary gripper coil
- Movable gripper coil
- Rod drive shaft assembly with grooves
- Pressure housing
- Magnetic coil stack

**Step Size:** 5/8 inch per step

**Maximum Stepping Rate:** 72 steps/min = 45 inches/min

**Stepping Sequence Time:** 780 milliseconds per step

---

## Rod Withdrawal Sequence (One Step = 5/8 inch)

1. **Movable Gripper Coil ON** → Movable latches swing into shaft groove
2. **Stationary Gripper Coil OFF** → Stationary latches swing out, load transfers to movable
3. **Lift Coil ON** → Movable assembly rises 5/8", lifting rod
4. **Stationary Gripper Coil ON** → Stationary latches swing in, lift shaft 1/16", take load
5. **Movable Gripper Coil OFF** → Movable latches swing out of groove
6. **Lift Coil OFF** → Movable assembly drops 5/8" to position at next groove

---

## Rod Insertion Sequence (One Step = 5/8 inch)

1. **Lift Coil ON** → Movable grippers raised 5/8" to position at groove
2. **Movable Gripper Coil ON** → Movable latches swing into groove
3. **Stationary Gripper Coil OFF** → Stationary latches swing out
4. **Lift Coil OFF** → Rod drops 5/8" (gravity + spring force)
5. **Stationary Gripper Coil ON** → Stationary latches swing in, lift shaft 1/16", take load
6. **Movable Gripper Coil OFF** → Movable latches swing out

**Reactor Trip:** Remove ALL power from coils → Springs disengage grippers → Rod falls by gravity

---

## Bank Sequencing and Overlap

**Withdrawal Sequence:** A → B → C → D

**Overlap Mechanism:**
- Typical overlap: 100 steps
- Control Bank A reaches 131 steps → Control Bank B starts moving
- Banks A and B move together for A's last 100 steps (overlap)
- Bank A reaches 231 steps (fully withdrawn) → stops
- Bank B continues to 131 steps → Bank C starts
- Process repeats for all banks

**Group Sequencing Within Banks:**
- Groups within same bank staggered (never differ by >1 step)
- Group 1 steps → Group 2 steps → Repeat
- During overlap: Group 1 of both banks move together, then Group 2 of both banks

**Key Design Feature:**
- Produces even reactivity additions per step
- Minimizes effect on core peaking factors
- Maintains consistent rod worth throughout travel

---

## Operating Modes (Bank Selector Switch)

### MANUAL
- Rod motion via IN-HOLD-OUT switch
- Bank sequence and overlap MAINTAINED via Bank Overlap Unit
- Rod speed: Adjustable 8-72 steps/min (typically 48 steps/min)
- Used for reactor startup, manual power adjustments

### AUTOMATIC
- IN-HOLD-OUT switch disabled
- Rod motion controlled by Reactor Control Unit
- Speed and direction determined by temperature/power mismatch
- Bank sequence and overlap MAINTAINED
- **ONLY available when turbine power >15%** (P-13 interlock)
- Control banks A, B, C, D only (automatic does not move shutdown banks)

### INDIVIDUAL BANK SELECT (Shutdown A, B, C, D or Control A, B, C, D)
- Manual control via IN-HOLD-OUT switch
- Bank sequence and overlap OVERRIDDEN
- Speed: 64 steps/min for shutdown banks, 48 steps/min for control banks
- Used for special operations, maintenance, dropped rod recovery

---

## Automatic Rod Control Inputs

**Three Input Signals:**
1. **Auctioneered High T_avg** - Highest of 4 loop T_avg signals
2. **Auctioneered High Nuclear Power** - Highest of 4 power range excore signals
3. **Turbine First Stage Impulse Pressure (P_imp)** - Turbine power indicator

**Two Mismatch Circuits:**

### Power Mismatch Circuit
**Purpose:** Fast, stable response to load changes - ANTICIPATORY control

**Inputs:**
- Auctioneered high nuclear power
- P_imp (turbine power)

**Function:**
- Rate comparator monitors RATE OF CHANGE of difference between nuclear and turbine power
- Ignores steady-state calibration differences
- Larger rate of change → Larger output

**Signal Processing:**
1. **Nonlinear Gain Unit:**
   - Power mismatch <1%: Low gain (0.3°F per 1% power deviation)
   - Power mismatch >1%: High gain (1.5°F per 1% power deviation)

2. **Variable Gain Unit:**
   - 0-50% power: Gain = 2
   - 50-100% power: Gain = 1/(%power) - inversely proportional
   - ≥100% power: Gain = 1
   - **Reason:** Prevent overshoot at high power (avoid licensed power limit violation)

**Output:** Equivalent temperature error signal sent to Summing Unit

### Temperature Mismatch Circuit
**Purpose:** Fine control during steady-state, returns T_avg to T_ref after transient

**Inputs:**
- Auctioneered high T_avg (actual temperature)
- T_ref (reference temperature generated from P_imp)

**Function:**
- Direct comparison: T_ref - T_avg = Temperature error
- No rate component - responds to magnitude only

**Output:** Temperature error signal sent to Summing Unit

---

## Reactor Control Unit - Rod Speed Program

**Input:** Total error signal from Summing Unit (power mismatch + temperature mismatch)

**Deadband:** ±1.5°F (includes 0.5°F lock-up)
- **Lock-up explained:**
  - Withdrawal starts at +1.5°F error
  - Withdrawal stops at +1.0°F error
  - Prevents bistable chattering (continuous on/off cycling)
  - Similar for insertion: -1.5°F start, -1.0°F stop

**Rod Speed Program:**

| Total Error | Rod Speed | Notes |
|-------------|-----------|-------|
| ±1.0°F to ±1.5°F | 0 steps/min | Within deadband (no motion) |
| ±1.5°F to ±3.0°F | 8 steps/min | Minimum speed (prevents hunting/overshoot) |
| ±3.0°F to ±5.0°F | 32 steps/min/°F | Proportional region |
| ≥±5.0°F | 72 steps/min | Maximum speed (CRDM physical limit) |

**Direction:** Sign of error determines IN (negative) or OUT (positive)

---

## Logic Cabinet Components

### Pulser
**Function:** Convert analog signal from Reactor Control Unit to digital pulses

**Operation:**
- Multiplies speed signal by factor of 6
- Outputs 48-432 pulses/min (for 8-72 steps/min rod speed)
- Each pulse advances/reverses Master Cycler by one count

### Master Cycler
**Function:** Convert pulses to "GO" signals for Slave Cyclers

**Operation:**
- Reversible clock/counter: 0 to 5 counts
- Counts once per Pulser pulse (6 pulses = 1 complete cycle)
- **Count 0:** GO pulse to Group 1 slave cyclers (1AC, 1BD)
- **Count 3:** GO pulse to Group 2 slave cyclers (2AC, 2BD)
- Divides Pulser rate by 6 (counteracts Pulser's ×6 multiplication)
- **Reversible:** Last group moved is first to move in opposite direction

**Result:** Two evenly spaced GO pulses per 6 Pulser pulses

### Slave Cyclers
**Function:** Generate current orders to Power Cabinets for one stepping cycle

**Operation:**
- 7-bit binary counter: Counts 0 to 127
- Complete cycle: 780 milliseconds
- Each count point generates specific current orders:
  - **Magnitude:** Full, reduced, or zero current
  - **Duration:** Length of time signal applied
  - **Timing:** When current applied/removed from specific coil
- One Slave Cycler per Power Cabinet
- Receives IN/OUT sequence instruction from Master Cycler
- Resets to 0 after reaching 127, awaits next GO pulse

### Bank Overlap Unit (BOU)
**Function:** Determine sequence and overlap of control banks

**Operation:**
- Receives counts from Master Cycler (one count per complete bank step)
- Reversible counter: 0 to 999 counts
- Thumbwheel switches preset overlap range (typically 100 steps)
- **Example:** Bank A reaches 131 steps → BOU count 131 → Bank B starts
- Tracks total rod position for all banks
- Provides multiplexing signals to Power Cabinets
- Feedback to Master Cycler directs GO pulses to proper Slave Cyclers

---

## Power Cabinet Operation

**Function:** Convert 3-phase AC to amplitude-controlled DC voltage for CRDM coils

**Power Supply:**
- Two parallel motor-generator (MG) sets
- Each: 150 HP induction motor driving 260 VAC generator
- Two separate nonvital 480 VAC 3-phase buses
- Power through TWO SERIES reactor trip breakers (redundant)
- Reactor trip bypass breakers for online RPS testing (admin control)

**Capacity:** Each Power Cabinet serves up to 3 groups (4 rods each, sometimes 5)

**Key Components:**

### Thyristors (Silicon-Controlled Rectifiers - SCRs)
- Current-controlled switches
- Turn on: ~3 microseconds
- Turn off: ~30 microseconds
- Handle tens to hundreds of amperes

### Half-Wave Bridge
- Thyristors (not simple rectifiers) allow variable DC output
- Gating at different phase angles controls current levels
- Produces full, reduced, or zero current as needed

### Multiplexing Circuit
**Function:** Ensure only ONE group moves at a time in each cabinet

**Example:** Power Cabinet 1BD contains:
- Group 1, Control Bank B
- Group 1, Control Bank D
- Group 1, Shutdown Bank B

**Operation:**
- To move Control Bank B Group 1:
  - Movable gripper multiplexing thyristor for Bank B: ON
  - Movable gripper thyristors for Banks D and Shutdown B: OFF
  - Lift coil multiplexing thyristors (4) for Bank B: ON
  - Lift coil thyristors for Banks D and Shutdown B: OFF
  - Stationary grippers for Banks D and Shutdown B: Hold current (not moving)
- Multiplexing signals from Bank Overlap Unit

### Firing Circuits (5 per Power Cabinet)
**Function:** Gate bridge thyristors to proper current levels per Slave Cycler commands

**Types:**
- 3 Stationary Gripper Circuits
- 1 Movable Gripper Circuit
- 1 Lift Coil Circuit

**Operation:**
- Receive signals from Slave Cycler
- Gate (turn on) associated bridge thyristor
- Sample actual coil current for feedback
- Adjust thyristor firing angle to match demanded current
- **Inhibit Signal:** For non-moving banks, BOU sends inhibit → firing circuit gates at reduced current to HOLD rods stationary

### DC Hold Cabinet
**Function:** Maintenance power to prevent dropping rods during Power Cabinet maintenance

**Power Source:**
- 125 VDC for latching
- 70 VDC for holding
- Taken from LOAD SIDE of reactor trip breakers (rods still trip on reactor trip)

**Operation:**
- 3 switches per Power Cabinet (one per group)
- Rotate to LATCH (hold ≥1 second) → Rotate to HOLD
- **Capacity:** Maximum 6 mechanisms (don't overload)
- **Critical:** If reactor trip breakers open, ALL rods trip (including those on hold)

### Lift Coil Disconnect Switches
**Function:** Allow individual rod motion (e.g., dropped rod recovery)

**Operation:**
- One switch per CRDM
- Located in rod disconnect switch panel (control room or nearby)
- Disconnect lift coils of all rods in bank EXCEPT dropped rod
- Allows selective movement of single rod

---

## Rod Withdrawal Stops (Interlocks)

### Manual Rod Withdrawal Stops
1. **Power Range High Flux:** 1/4 channels, power >103%
2. **Intermediate Range High Flux:** 1/2 channels, power >20%
3. **Overtemperature ΔT (OTΔT):** 2/4 loops, ΔT > (OTΔT trip - 3%)
4. **Overpower ΔT (OPΔT):** 2/4 loops, ΔT > (OPΔT trip - 3%)

### Automatic Rod Withdrawal Stops (All Manual stops PLUS:)
5. **Low Power Interlock:** 1/1, turbine power (P_imp) <15%
6. **Control Bank D Withdrawal Interlock:** Bank D position >223 steps

**Key Principle:** Stops prevent OUTWARD motion only - rods can ALWAYS be inserted

---

## Alarm and Failure Modes

### Non-Urgent Failure (ROD CONTROL NON-URGENT FAILURE)
**Causes:**
- Loss of one redundant power supply in Logic Cabinet (3 total)
- Loss of one power supply in Power Cabinet (2 total)

**Effect:**
- Low probability of dropping rod
- Does NOT affect ability to move rods
- Single annunciator for Logic OR Power Cabinet failure

### Urgent Failure (ROD CONTROL URGENT FAILURE)

**Logic Cabinet Urgent Failure Causes:**
- Slave Cycler failure
- Pulser failure
- Removal of any printed circuit board

**Logic Cabinet Urgent Failure Effect:**
- Inhibits ALL automatic and manual rod motion
- Rods moveable ONLY in individual bank select
- Failed Slave Cycler group won't move even in individual bank

**Power Cabinet Urgent Failure Causes:**
- Blown fuse in AC supply line
- Bridge thyristor fault
- Current signal mismatch (actual vs demanded)
- Loss of current to BOTH stationary AND movable grippers simultaneously
- Loss of multiplexing signal

**Power Cabinet Urgent Failure Effect:**
- Inhibits ALL automatic and manual rod motion
- **Protective Action in Affected Cabinet:**
  - ALL power removed from lift coils
  - LOW current applied to BOTH stationary AND movable gripper coils (prevent rod drop)
- Rods moveable in individual bank select (except affected group)

**Recovery from Urgent Failure:**
- Cannot move rods in manual or automatic until problem corrected
- Must reset alarm at BOTH:
  - Affected cabinet
  - Main control board

---

## Design Transient Capabilities

**System designed to handle without relief valve actuation or reactor trip:**

1. **10% Step Load Increase or Decrease**
   - Tavg maintained within ±5°F of program
   
2. **5%/min Ramp Load Increase or Decrease**
   - Tavg maintained within ±5°F of program

3. **50% Step Load Decrease** (with steam dump system aid)
   - Rod control insufficient for rapid response
   - Steam dump removes excess heat until rods reduce Tavg to new program

---

## Critical Notes for Simulator

1. **Startup Sequence:**
   - Shutdown banks fully withdrawn in MANUAL mode before criticality
   - Control banks used for approach to criticality
   - Switch to AUTOMATIC after turbine power >15%

2. **Bank Overlap at Core Midplane:**
   - Ensures relatively constant reactivity addition rate
   - Minimizes peaking factor perturbations
   - Typical overlap: 100 steps (adjustable via thumbwheels)

3. **Reactor Trip Mechanism:**
   - Opening EITHER reactor trip breaker de-energizes ALL CRDMs
   - Springs disengage grippers
   - Rods fall by gravity
   - No power required for trip

4. **Automatic Control Range:**
   - ONLY operates between 15% and 100% power
   - Below 15%: Low power interlock prevents automatic
   - Requires manual rod control for startup

5. **Power Mismatch Anticipatory Function:**
   - Responds to RATE of change (derivative control)
   - Prevents temperature excursions during load transients
   - Works in conjunction with temperature mismatch (proportional control)

6. **Rod Speed Varies with Error:**
   - Small errors (1.5-3°F): Slow speed (8 steps/min) prevents hunting
   - Large errors (≥5°F): Maximum speed (72 steps/min) for rapid correction
   - Proportional region (3-5°F): 32 steps/min/°F

---

## Implementation Priority for Simulator

**Phase 1 (Startup to HZP):**
- Manual rod control with bank sequencing
- Shutdown bank withdrawal
- Control bank withdrawal for approach to criticality
- Rod position indication
- Withdrawal stops (power range, intermediate range)

**Phase 2 (HZP to Power Operations):**
- Automatic rod control implementation
- Power mismatch circuit
- Temperature mismatch circuit
- Reactor control unit with rod speed program
- Automatic/manual switching at 15% power

**Phase 3 (Advanced Features):**
- Individual bank select for dropped rod recovery
- Urgent/non-urgent failure modes
- DC hold cabinet simulation
- Lift coil disconnect capability

---

## References

This document should be referenced for:
- Rod control system architecture and signal flow
- Bank sequencing and overlap algorithms
- Automatic control logic (power/temperature mismatch)
- Rod speed programming and deadband behavior
- CRDM stepping sequences
- Interlocks and withdrawal stops
- Failure modes and protective actions