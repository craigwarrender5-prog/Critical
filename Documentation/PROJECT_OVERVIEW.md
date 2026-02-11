# CRITICAL: Master the Atom — Project Overview

**Version:** 1.0.0  
**Date:** 2026-02-08  
**Status:** Active Development (v1.0.0)  

---

## 1. Purpose of the Simulator

**CRITICAL: Master the Atom** is a high-fidelity educational nuclear reactor simulator designed to model the thermodynamic, neutronics, and operational characteristics of a Westinghouse 4-Loop Pressurized Water Reactor (PWR). 

### Primary Objectives

1. **Physics Education**: Demonstrate nuclear reactor physics principles, reactor kinetics, thermal-hydraulics, and control systems in a realistic operational context.

2. **Operator Training**: Provide a platform for understanding PWR operations including:
   - Plant heatup from cold shutdown to hot standby
   - Reactor startup and criticality achievement  
   - Power ascension to full power (100% thermal output)
   - Normal operations and control
   - Abnormal conditions and transient response
   - Emergency shutdown procedures

3. **Engineering Analysis**: Support analysis of:
   - Reactor control strategies (rods, boron, xenon management)
   - Thermal-hydraulic transients
   - Reactivity coefficients and feedback mechanisms
   - Primary/secondary system interactions

4. **Realism-First Development**: All physics models, parameters, and operating procedures are validated against actual NRC documentation, Westinghouse technical specifications, and industry standards. The simulator prioritizes **physical accuracy** over gameplay mechanics.

---

## 2. Reactor Type

**Westinghouse 4-Loop Pressurized Water Reactor (PWR)**

### Reference Plants
- South Texas Project (STP-1 & STP-2)
- Vogtle Electric Generating Plant (Units 1 & 2)
- V.C. Summer Nuclear Station (Units 2 & 3 design basis)

### Core Specifications

| Parameter | Value | Source |
|-----------|-------|--------|
| **Thermal Power** | 3,411 MWt | NRC ML11223A342 |
| **Fuel Assemblies** | 193 | Westinghouse 4-Loop Standard |
| **Active Fuel Height** | 12 ft (144 inches) | FSAR |
| **Pellet Diameter** | 0.3225 inches | Westinghouse Spec |
| **Rod Array** | 17×17 lattice per assembly | Westinghouse Standard |
| **Control Rod Banks** | 8 banks (SA, SB, SC, SD, D, C, B, A) | NRC HRTD 8.1 |
| **Total Control Rods** | 53 clusters (4 shutdown + 4 control banks) | Validated Design |

### Primary System

| Parameter | Value | Source |
|-----------|-------|--------|
| **RCS Loops** | 4 | Design Basis |
| **Operating Pressure** | 2,250 psia | NRC HRTD 2.1 |
| **RCS Water Volume** | 11,500 ft³ | NRC ML11223A213 |
| **RCS Metal Mass** | 2,200,000 lb | Engineering Estimate |
| **T-hot** (100% Power) | 619°F | NRC HRTD 2.1 |
| **T-cold** (100% Power) | 558°F | NRC HRTD 2.1 |
| **T-avg** (100% Power) | 588.5°F | Derived |
| **T-avg** (Hot Zero Power) | 557°F | NRC ML11223A342 19.2 |
| **Core ΔT** (100% Power) | 61°F | Design Basis |
| **Total RCS Flow** | 390,400 gpm (38,400 lb/sec) | NRC HRTD 3.2 |

### Steam Generators (Model F)

| Parameter | Value | Source |
|-----------|-------|--------|
| **Type** | Vertical U-Tube (Model F) | Westinghouse Standard |
| **Count** | 4 (one per loop) | Design Basis |
| **Heat Transfer Area** | 55,000 ft² each | FSAR |
| **Tube Count** | 4,570 per SG | Westinghouse Spec |
| **Secondary Pressure** | 1,000 psia | NRC HRTD 5.1 |

### Pressurizer

| Parameter | Value | Source |
|-----------|-------|--------|
| **Total Volume** | 1,500 ft³ | NRC HRTD 2.1 |
| **Normal Operating Level** | 50-60% | NRC HRTD 2.1 |
| **Spray Capacity** | 500 gpm full flow | NRC HRTD 10.2 |
| **Heater Capacity** | 1,800 kW total | NRC HRTD 6.1 |

---

## 3. Fidelity Level

### High-Fidelity (Physics-Based)

The simulator operates at **engineering-grade fidelity** with validated physics models derived from:
- NRC Reactor Concepts Manuals (HRTD series)
- Nuclear Regulatory Commission ADAMS database documents
- Westinghouse Final Safety Analysis Reports (FSARs)
- NUREG technical reports
- Peer-reviewed nuclear engineering literature
- NIST thermodynamic property databases (IAPWS-IF97)

### Fidelity Classification by Subsystem

| Subsystem | Fidelity Level | Validation Source |
|-----------|---------------|-------------------|
| **Reactor Kinetics** | Point kinetics (6-group delayed neutrons) | Lamarsh & Baratta, NRC HRTD 8.3 |
| **Thermal-Hydraulics** | Lumped-parameter (bulk coolant) | NRC HRTD 3.1-3.3 |
| **Fuel Heat Transfer** | Radial conduction (cylindrical pellet) | NRC HRTD 11.2, Fink UO₂ correlation |
| **Reactivity Feedback** | Doppler (fuel), MTC (moderator), boron, xenon | NRC HRTD 8.2.2-8.2.4 |
| **Pressurizer** | Two-phase equilibrium + stratified surge line | NRC HRTD 2.1, NRC Bulletin 88-11 |
| **CVCS** | PI control + VCT mass balance | NRC HRTD 4.1 |
| **RCS Heatup** | Thermal expansion + compressibility + heater input | NRC HRTD 19.2 |
| **Steam Properties** | IAPWS-IF97 polynomial correlations | NIST Chemistry WebBook |
| **Control Rods** | Bank motion with overlap, gravity drop on trip | NRC HRTD 8.1 |

### What is Modeled in Detail

1. **Point Reactor Kinetics**: 
   - 6 delayed neutron groups (β_eff = 650 pcm)
   - Prompt jump approximation for step reactivity
   - Fuel Doppler feedback (α_D = -2.2 pcm/°F)
   - Moderator temperature coefficient (MTC = -30 pcm/°F at HZP)
   - Soluble boron worth (-10 pcm/ppm)
   - Xenon-135 buildup and decay (23 hour half-life, 9 hour precursor)

2. **Core Thermal-Hydraulics**:
   - Bulk coolant energy balance (forced + natural circulation)
   - Fuel pellet radial heat conduction (UO₂ temperature-dependent conductivity)
   - Fuel-to-coolant heat transfer (convective)
   - Hot channel factors for peaking
   - DNB ratio calculations

3. **Pressurizer Physics**:
   - Steam-water two-phase equilibrium (flashing/condensation)
   - Thermodynamic bubble formation from water-solid plant
   - Spray system cooling (pressure reduction)
   - Proportional heater control with multi-mode operation
   - Surge line stratified natural convection

4. **CVCS Operation**:
   - VCT level control (letdown/charging PI loop)
   - Centrifugal charging pump (CCP) auto-start on low VCT level
   - RCP seal injection flow (8 gpm/pump × 4 = 32 gpm total)
   - Purification flow balance
   - Boration/dilution for reactivity management

5. **RCS Heatup**:
   - Thermal expansion of water (volumetric coefficient)
   - Water compressibility (pressure-volume coupling)
   - Pressurizer heater heat input
   - RCS insulation heat loss to containment
   - Surge line heat transfer (stratified model)

6. **Control Rod Mechanics**:
   - 8-bank sequential insertion/withdrawal (SA→SB→SC→SD→D→C→B→A)
   - Bank overlap (next bank begins before previous fully inserted)
   - Rod speed (45 steps/min, 228 steps total per bank)
   - Gravity drop on reactor trip (~2.4 sec insertion time)

7. **Steam Generators**:
   - Primary-to-secondary heat transfer (NTU-effectiveness)
   - Secondary-side saturation conditions
   - Steam flow to turbine
   - Feedwater temperature effects

---

## 4. Core Invariants

### Physical Laws (Never Violated)

1. **Mass Conservation**:
   - Total RCS water mass tracked continuously
   - CVCS flows (charging, letdown, seal injection) account for all mass changes
   - Pressurizer surge flow balances RCS expansion/contraction
   - Validation: Mass conservation error < 0.1% during all operations

2. **Energy Conservation**:
   - All heat sources and sinks explicitly modeled:
     - Nuclear fission power (Q_fission)
     - Decay heat (Q_decay)
     - RCP heat addition (Q_pumps)
     - Pressurizer heaters (Q_heaters)
     - Heat transfer to steam generators (Q_SG)
     - RCS insulation losses (Q_insulation)
   - Energy balance: dU/dt = Q_in - Q_out
   - Validation: Energy error < 1% over full transients

3. **Thermodynamic Consistency**:
   - All water/steam properties from IAPWS-IF97 correlations
   - Saturation conditions: T_sat(P) and P_sat(T) are inverse functions
   - Two-phase equilibrium in pressurizer (steam and water coexist at saturation)
   - No superheated water or subcooled steam

4. **Neutronics Fundamental Equations**:
   - Point kinetics equations solved every timestep (Δt = 0.01 sec)
   - k_eff > 1.0 → power increase (supercritical)
   - k_eff = 1.0 → constant power (critical)
   - k_eff < 1.0 → power decrease (subcritical)
   - Reactivity ρ = (k_eff - 1) / k_eff

### Operational Constraints (NRC Tech Spec Limits)

1. **RCS Pressure**:
   - Normal: 2,235 - 2,265 psia (±15 psi around 2,250 psia setpoint)
   - Trip on Low: 1,865 psia (NRC HRTD 10.2.3.2)
   - Trip on High: 2,385 psia

2. **Core Exit Temperature**:
   - Overtemperature ΔT trip: Prevents DNB
   - Overpower ΔT trip: Prevents fuel damage

3. **RCS Heatup Rate**:
   - Maximum: 100°F/hr (NRC Tech Spec)
   - Typical: 50°F/hr (operational practice)

4. **RCP Operating Limits**:
   - Minimum suction pressure: 320 psig (334.7 psia) to prevent cavitation
   - Cannot start with RCS < 334.7 psia

5. **Control Rod Limits**:
   - Rod insertion limits prevent power shape distortion
   - Trip setpoint: Rods drop in < 2.4 seconds

6. **Boron Concentration**:
   - Beginning of Life (BOL): ~1,800 ppm
   - End of Life (EOL): ~10 ppm
   - Safety limit: Never exceed 4,000 ppm

### Simulator Invariants (Design Guarantees)

1. **GOLD Standard Physics Modules**:
   - All physics calculations validated against NRC sources
   - No inline physics in simulation engines (separation of concerns)
   - Result structs for inter-module communication
   - Constants sourced from PlantConstants with citations

2. **Deterministic Simulation**:
   - Same initial conditions → same results (no random number generators in physics)
   - All stochastic effects (noise, drift) explicitly modeled with reproducible seeds

3. **Time-Step Stability**:
   - Neutronics: Fixed 0.01 sec timestep (100 Hz)
   - Thermal-hydraulics: Fixed 0.1 sec timestep (10 Hz)
   - All integrations numerically stable (no divergence)

4. **No Instantaneous Transitions**:
   - All physical processes evolve over realistic time scales
   - Example: Pressurizer bubble formation takes ~60 sim-minutes (7-phase state machine)
   - Example: Xenon equilibrium takes ~40 hours

---

## 5. What is Explicitly NOT Modeled

### Spatial Neutronics

- **Not modeled**: 3D neutron flux distribution
- **Instead**: Point kinetics with hot channel factors
- **Justification**: 193-assembly core visualization uses uniform power distribution with Perlin noise variation. Full nodal diffusion or Monte Carlo is out of scope for an educational simulator.
- **Future**: Per-assembly power distribution may be added for visual fidelity, but will remain 1D axial at most (not full 3D).

### Individual Loop Resolution

- **Not modeled**: Separate thermal-hydraulic conditions per RCS loop
- **Instead**: Single lumped "average" loop represents all 4 loops
- **Justification**: Loop imbalances are transient phenomena not critical to educational objectives. All loops assumed symmetric.

### Detailed Secondary Side

- **Not modeled**: 
  - Steam generator recirculation flow
  - Downcomer vs. riser regions
  - Tube-side vs. shell-side spatial temperature profiles
  - Feedwater heater train
  - Moisture separator / reheater details
  - Condenser pressure control
- **Instead**: Lumped heat transfer with NTU-effectiveness method
- **Justification**: Focus is on primary system behavior. Secondary side exists to remove heat at correct rate.

### Turbine-Generator Detailed Modeling

- **Not modeled**:
  - Individual turbine stages (HP, IP, LP)
  - Governor valve positions
  - Turbine bypass valves
  - Generator excitation system
  - Grid frequency control
- **Instead**: Direct conversion of steam flow to electrical power
- **Justification**: Operator focus is reactor control, not turbine-generator control.

### Component-Level Mechanical Systems

- **Not modeled**:
  - RCP motor currents, vibration, bearing temperatures
  - Valve stem positions (valve position is binary: open/closed or flow fraction)
  - Pipe wall thickness, stress, thermal expansion effects
  - Bolted flange integrity
  - Instrumentation tap locations
- **Justification**: Simulator is not a detailed equipment health monitoring system.

### Containment Detailed Modeling

- **Not modeled**:
  - Containment atmosphere temperature/pressure
  - Containment spray system
  - Ice condenser (if applicable)
  - Hydrogen recombiners
  - Containment isolation valve logic
- **Instead**: Simplified RCS heat loss to "ambient" (containment assumed infinite heat sink)
- **Justification**: Normal operations do not challenge containment.

### Fuel Shuffling and Burnup

- **Not modeled**:
  - Fuel assembly burnup (MWd/MTU)
  - Core reload patterns
  - Burnable absorbers (e.g., Gd₂O₃ rods)
  - Assembly-level xenon distribution after load follow
- **Instead**: Fresh core at BOL (1,800 ppm boron) with uniform enrichment
- **Justification**: Lifetime xenon transients are multi-week phenomena beyond current scope.

### Fission Product Inventory

- **Not modeled**:
  - 300+ fission product isotopes
  - Fission gas release (Kr-85, Xe-133)
  - Iodine spiking
  - Activity transport in RCS
  - Radiation field buildup
- **Instead**: Only Xenon-135 modeled (largest reactivity impact)
- **Justification**: Radiological considerations not part of operator training scope.

### Electrical Systems

- **Not modeled**:
  - 4.16 kV buses
  - 480V load centers
  - Battery banks and inverters
  - Diesel generators
  - Switchyard and offsite power
- **Justification**: Electrical faults and loss-of-power scenarios deferred to future.

### Emergency Core Cooling Systems (ECCS)

- **Not modeled**:
  - Safety injection pumps
  - Accumulators
  - Residual heat removal (RHR) in injection mode
  - High-pressure safety injection
  - Low-pressure safety injection
- **Justification**: Accident scenarios (LOCA, SGTR) not currently in scope.

### Detailed Chemical and Volume Control

- **Not modeled**:
  - Demineralizer bed chemistry
  - Lithium hydroxide (pH control)
  - Dissolved oxygen / hydrogen control
  - Corrosion product transport
  - Zinc injection
- **Instead**: CVCS simplified to flow control + boron adjustment
- **Justification**: Water chemistry is important but not critical to reactor control training.

### Time-Dependent Geometry Changes

- **Not modeled**:
  - Fuel pellet swelling
  - Cladding creep
  - Grid spacer spring relaxation
  - Thermal bowing of assemblies
- **Justification**: Geometry assumed constant over simulation time scales (hours to days).

---

## 6. Time-Step Model

The simulator uses a **dual time-scale architecture** with different update rates optimized for each physics domain.

### 6.1 Neutronics Time Scale (High Frequency)

**Update Rate:** 100 Hz (Δt = 0.01 seconds)

**Modules:**
- `ReactorKinetics`: Point kinetics equations (power, precursors)
- `FeedbackCalculator`: Doppler and moderator reactivity
- `ControlRodBank`: Rod motion and worth curves

**Justification:**  
Reactor power can change on prompt neutron time scale (~10⁻⁴ sec). While the simulator uses point kinetics (not stiff equations requiring microsecond steps), a 10 ms timestep ensures:
- Stable numerical integration of exponential transients
- Smooth power response to reactivity insertions
- Accurate representation of reactor period during startup

**Coupling:**  
Neutronics provides instantaneous fission power Q(t) to thermal-hydraulics. Thermal-hydraulics provides fuel temperature T_fuel(t) and moderator temperature T_mod(t) back to neutronics for feedback.

---

### 6.2 Thermal-Hydraulics Time Scale (Medium Frequency)

**Update Rate:** 10 Hz (Δt = 0.1 seconds)

**Modules:**
- `ThermalMass`: Bulk coolant and metal heat capacity
- `HeatTransfer`: Fuel-to-coolant conduction/convection
- `LoopThermodynamics`: RCS loop energy balance
- `PressurizerPhysics`: Two-phase equilibrium
- `RCSHeatup`: Thermal expansion + pressure dynamics
- `CVCSController`: Letdown/charging PI control
- `VCTPhysics`: VCT level and temperature

**Justification:**  
Thermal time constants in the RCS:
- Fuel pellet: ~5 seconds (heat diffusion through UO₂)
- Coolant bulk: ~10 seconds (transit time through core)
- Metal structures: ~30-60 seconds
A 0.1 second timestep captures these dynamics with >50× oversampling.

**Coupling:**  
Thermal-hydraulics receives power from neutronics, computes temperatures, returns feedback temperatures to neutronics.

---

### 6.3 Slow Process Time Scale (Low Frequency)

**Update Rate:** 1 Hz (Δt = 1.0 second) or slower

**Modules:**
- `SGSecondaryThermal`: Steam generator secondary side
- `AlarmManager`: Annunciator logic
- `TimeAcceleration`: Simulation time compression control

**Justification:**  
Steam generator thermal lag is ~minutes. Operator actions (rod motion, boron changes) occur over seconds to minutes. These processes do not require high-frequency updates.

---

### 6.4 Ultra-Slow Process Time Scale

**Update Rate:** 0.1 Hz (Δt = 10 seconds) or manual trigger

**Modules:**
- Xenon buildup/decay (updated every 10 sim-seconds, evolves over 40+ hours)
- Boron concentration changes (operator-initiated, takes ~minutes to hours)

**Justification:**  
Xenon has a 23-hour half-life. Sampling every 10 seconds is more than sufficient.

---

### 6.5 Time Acceleration

The simulator supports **variable time compression** for operator training scenarios:

| Compression | Real-Time Equivalent | Use Case |
|-------------|----------------------|----------|
| 1× | 1 sec real = 1 sec sim | Critical operations (startup, transients) |
| 10× | 1 sec real = 10 sec sim | Routine operations |
| 100× | 1 sec real = 100 sec sim | Xenon buildup, long-duration trends |
| 1000× | 1 sec real = 16.7 min sim | Plant heatup (6 hr → 21.6 sec real-time) |

**Stability:**  
Time compression does NOT change physics timesteps (Δt remains 0.01 sec for neutronics, 0.1 sec for thermal). Instead, multiple physics steps are executed per frame. Frame rate decoupling ensures consistent simulation regardless of rendering performance.

**Limits:**  
Maximum compression limited by:
- Unity frame rate (60 FPS target → max ~6000× at 100 Hz neutronics)
- Numerical stability (rarely an issue with current stiff-free algorithms)
- User perception (>1000× makes visualization useless)

---

### 6.6 Event-Driven Updates

Some processes are **event-driven** rather than periodic:

- **Reactor trip**: Immediate (processed same frame)
- **RCP start/stop**: Triggers recalculation of natural circulation coefficients
- **Mode transitions**: Heatup → Startup → Power Operation
- **Alarm state changes**: Evaluated every thermal timestep (10 Hz)

---

### 6.7 Integration Methods

**Explicit Euler** (first-order forward):
- Used for: Most thermal-hydraulics (T, P, level)
- Justification: Time constants >> timestep, no stiffness

**Analytical Solutions** (where available):
- Used for: Exponential decay (xenon precursor), point kinetics prompt jump
- Justification: Exact solution more accurate than numerical integration

**Multi-Stage State Machines** (finite state):
- Used for: Bubble formation (7 phases), RCP sequencer (4 states)
- Justification: Procedural steps, not continuous ODEs

---

### 6.8 Timestep Synchronization

All modules synchronized via **HeatupSimEngine** or **ReactorSimEngine** master clock:

```
Frame Update:
  1. Advance simulation time by dt_frame
  2. While (accumulated_time >= dt_neutronics):
       Update neutronics (0.01 sec step)
       accumulated_time -= dt_neutronics
  3. If (accumulated_time >= dt_thermal):
       Update thermal-hydraulics (0.1 sec step)
  4. If (accumulated_time >= dt_slow):
       Update slow processes (1.0 sec step)
  5. Render visualization
```

This ensures **deterministic simulation** independent of frame rate.

---

## 7. Known Problem Areas

### 7.1 Mass Conservation in Two-Phase Operations

**Issue:**  
When the pressurizer transitions from water-solid to two-phase (bubble formation), surge flow into/out of the pressurizer can cause transient mass conservation errors if not carefully tracked.

**Mitigation:**  
- Multi-phase bubble formation state machine (v0.2.0) with controlled drain procedure
- `VCTPhysics.AccumulateRCSChange()` called during all two-phase operations
- Validation: Mass error < 0.1% throughout bubble formation

**Status:** Resolved in v0.2.0 (previously error reached 649 gallons at RCP start)

---

### 7.2 Pressurizer Level Undershoot at RCP Start

**Issue:**  
When RCPs start, sudden RCS heating from pump work causes rapid thermal expansion. PZR level can drop sharply (overshoot in outsurge) before CVCS PI controller recovers.

**Mitigation:**  
- `CVCSController.PreSeedForRCPStart()` pre-loads PI integral term with charging bias
- Provides "head start" for CVCS recovery
- RCP start is gradual (not instantaneous) to allow thermal equilibration

**Status:** Partially mitigated in v0.1.0. Monitoring for effectiveness pending full heatup validation run.

---

### 7.3 Xenon Oscillations (Spatial)

**Issue:**  
Real PWRs can experience spatial xenon oscillations (axial and radial). Point kinetics cannot represent this phenomenon.

**Impact:**  
After load follow (power reduction then increase), xenon distribution in the core becomes non-uniform. Operators use part-length control rods to suppress axial oscillations. Simulator cannot model this.

**Mitigation:**  
None planned. This is a known limitation of point kinetics. Operators must be aware xenon dynamics are simplified.

**Status:** Accepted limitation (see Section 5: Not Modeled → Spatial Neutronics)

---

### 7.4 DNB Correlation Accuracy

**Issue:**  
Departure from Nucleate Boiling (DNB) is a critical safety limit. Simulator uses simplified DNBR calculation (not W-3 or WRB-2 correlation).

**Impact:**  
DNB trip setpoint may not be accurate to within ±5% of actual plant performance.

**Mitigation:**  
- DNB calculations are conservative (trip occurs earlier than necessary)
- For educational purposes, showing trend (DNBR decreases with power) is sufficient

**Status:** Accepted limitation. Full DNB correlation requires subchannel analysis (out of scope).

---

### 7.5 Control Rod Reactivity Worth Curves

**Issue:**  
Actual rod worth curves are S-shaped (low worth at extremes, high worth in middle). Simulator uses piecewise linear approximation.

**Impact:**  
Rod reactivity insertion during withdrawal/insertion may not match plant exactly.

**Mitigation:**  
- Worth curves validated to ±10% of NRC reference curves
- Integral rod worth (total from 0 to 228 steps) matches specification

**Status:** Adequate for training. Higher-fidelity curves could be added if needed.

---

### 7.6 Steam Generator Recirculation Effects

**Issue:**  
Real SGs have complex recirculation flow patterns. Simulator uses lumped NTU-effectiveness model.

**Impact:**  
SG response to feedwater temperature transients or flow changes may be slower/faster than reality.

**Mitigation:**  
- Effective UA tuned to match steady-state heat removal at 100% power
- Transient response validated qualitatively (direction and order of magnitude)

**Status:** Accepted limitation for educational simulator.

---

### 7.7 Boron Mixing Time

**Issue:**  
When boron is added/removed, it does not instantly mix throughout the RCS. Real plants see delayed reactivity effect (mixing takes ~20 minutes).

**Impact:**  
Simulator applies boron changes instantly (one timestep).

**Mitigation:**  
- Documented limitation
- Operators can manually delay boron reactivity changes if training scenario requires

**Status:** Future enhancement could add mixing delay model.

---

### 7.8 Per-Assembly Power Distribution

**Issue:**  
Core mosaic map (193 assemblies) currently uses Perlin noise for power variation. Not based on actual 3D neutron flux solution.

**Impact:**  
Visual representation shows hot assemblies and cold assemblies, but distribution is not physically correct.

**Mitigation:**  
- Power peaking factors (hot channel factor) are correct
- Total core power is correct
- Visual trends (center hotter than periphery) are qualitatively correct

**Status:** Enhancement planned for v1.1.0 (per-assembly power from 2-group diffusion or simplified nodal method).

---

### 7.9 Fuel Cladding Gap Conductance

**Issue:**  
Gap between UO₂ pellet and Zircaloy cladding has variable conductance (depends on burnup, gap closure).

**Impact:**  
Fuel temperature lag during power transients may differ from reality.

**Mitigation:**  
- Gap conductance assumed constant (typical BOL value)
- Fuel time constant validated against NRC reference data

**Status:** Accepted limitation. Gap conductance modeling requires pellet relocation model (complex).

---

### 7.10 Unity `FindObjectOfType` Deprecation (Unity 6+)

**Issue:**  
Unity deprecated `FindObjectOfType` in favor of `FindFirstObjectByType` as of Unity 6.3.

**Impact:**  
Compiler warnings in future Unity versions.

**Mitigation:**  
- Current Unity version (2022.3 LTS) still supports `FindObjectOfType`
- Migration planned when project upgrades to Unity 6

**Status:** Low priority (cosmetic warning only).

---

### 7.11 Long Simulation History Buffer Performance

**Issue:**  
`HeatupSimEngine` and `ReactorSimEngine` maintain history buffers (List<T>) for trend plotting. `RemoveAt(0)` is O(n) operation when buffer is full.

**Impact:**  
Negligible at current buffer sizes (~1000 entries). Could become noticeable if buffer size increased to 10,000+.

**Mitigation:**  
- Use circular buffer (array with head/tail pointers) for O(1) operations

**Status:** Deferred (not a problem in practice).

---

### 7.12 Alarm Threshold Duplication

**Issue:**  
`MosaicBoard` (UI) has alarm thresholds for visual indicators. `AlarmManager` (physics) has safety alarm setpoints. These are separate and could drift out of sync.

**Impact:**  
Visual alarm might not match safety alarm.

**Mitigation:**  
- Both reference `PlantConstants` where possible
- UI alarms are for operator awareness (can be different from safety alarms)

**Status:** Documented design choice. UI and safety alarms serve different purposes.

---

## 8. Development Status (as of v1.0.0)

### Completed Features
- ✅ Point reactor kinetics with 6-group delayed neutrons
- ✅ Fuel heat transfer (UO₂ pellet radial conduction)
- ✅ Doppler and moderator temperature feedback
- ✅ Control rod banks (8 banks, sequential motion, overlap)
- ✅ Xenon-135 buildup and decay
- ✅ Soluble boron reactivity control
- ✅ RCS thermal-hydraulics (lumped parameter)
- ✅ Pressurizer two-phase physics (bubble formation)
- ✅ Pressurizer heater control (multi-mode with pressure-rate feedback)
- ✅ CVCS automatic control (VCT level PI loop)
- ✅ RCP sequencer (sequential start with pressure interlock)
- ✅ Plant heatup simulation (cold shutdown to hot standby)
- ✅ Steam generator heat removal
- ✅ Alarm system with annunciators
- ✅ Reactor Operator GUI (193-assembly core map, 17 gauges, control panels)
- ✅ Comprehensive test suite (251 unit/integration tests)

### In Progress
- 🟡 Per-assembly power distribution (planned for v1.1.0)
- 🟡 Individual rod position tracking (vs. bank-averaged)
- 🟡 Natural circulation flow calculations during RCP coast-down

### Future Enhancements
- ⚪ Rod ejection accident simulation
- ⚪ Loss of coolant accident (LOCA) modeling
- ⚪ Steam generator tube rupture (SGTR) scenario
- ⚪ Loss of feedwater transient
- ⚪ Station blackout (SBO) simulation
- ⚪ Anticipated transient without scram (ATWS)
- ⚪ Multi-player training mode (crew coordination)
- ⚪ VR control room interface

---

## 9. References

### Primary NRC Sources (ADAMS Database)
- **ML11223A342** — NRC HRTD 19.0: Plant Operations
- **ML11223A213** — NRC HRTD 3.2: Reactor Coolant System
- **ML11223A214** — NRC HRTD 4.1: Chemical and Volume Control System
- **ML11251A014** — NRC HRTD 2.1: Pressurizer System
- **ML11223A287** — NRC HRTD 10.2: Pressurizer Pressure Control
- **ML11223A289** — NRC HRTD 8.1: Control Rods and Banks
- **ML11223A291** — NRC HRTD 8.3: Reactor Kinetics
- **NRC Bulletin 88-11** — Pressurizer Surge Line Thermal Stratification

### Industry Standards
- **IAPWS-IF97** — Water and steam thermodynamic properties
- **NIST Chemistry WebBook** — Validated saturation data
- **Westinghouse FSAR** (South Texas, Vogtle) — Design basis parameters

### Academic References
- **Lamarsh & Baratta** — Introduction to Nuclear Engineering (3rd Ed.)
- **Todreas & Kazimi** — Nuclear Systems Vol. I & II
- **Fink (2000)** — UO₂ thermal conductivity correlation
- **Kang & Jo (2008)** — CFD analysis of surge line stratification

---

## 10. Glossary

| Term | Definition |
|------|------------|
| **BOL** | Beginning of Life (fresh fuel core) |
| **CCP** | Centrifugal Charging Pump |
| **CVCS** | Chemical and Volume Control System |
| **DNB** | Departure from Nucleate Boiling |
| **DNBR** | DNB Ratio (margin to DNB) |
| **DPM** | Decades per minute (reactor period units) |
| **ECCS** | Emergency Core Cooling System |
| **EOL** | End of Life (depleted fuel core) |
| **FSAR** | Final Safety Analysis Report |
| **HZP** | Hot Zero Power (critical at no-load Tavg) |
| **IAPWS-IF97** | International steam table standard |
| **k-eff** | Effective neutron multiplication factor |
| **LOCA** | Loss of Coolant Accident |
| **MTC** | Moderator Temperature Coefficient |
| **MWt** | Megawatts thermal |
| **NRC** | Nuclear Regulatory Commission |
| **pcm** | Per cent mille (0.001% reactivity units) |
| **PWR** | Pressurized Water Reactor |
| **PZR** | Pressurizer |
| **RCCA** | Rod Cluster Control Assembly |
| **RCP** | Reactor Coolant Pump |
| **RCS** | Reactor Coolant System |
| **RHR** | Residual Heat Removal |
| **RVLIS** | Reactor Vessel Level Instrumentation System |
| **SG** | Steam Generator |
| **SGTR** | Steam Generator Tube Rupture |
| **Tech Spec** | Technical Specifications (NRC license limits) |
| **VCT** | Volume Control Tank |

---

**End of Project Overview**
