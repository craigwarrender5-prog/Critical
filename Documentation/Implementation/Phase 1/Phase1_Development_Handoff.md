# CRITICAL PWR Simulator - Phase 1 Development Handoff

## PROJECT OVERVIEW

**Project:** CRITICAL - Master the Atom (PWR Nuclear Power Plant Simulator)
**Target Platform:** Unity (C#)
**Reference Design:** Westinghouse 4-Loop PWR (3411 MWt)
**Total Project:** 6 Phases, 480 Tests

## PHASE 1: PHYSICS FOUNDATION

**Duration:** 4-6 weeks
**Tests:** 112 unit tests
**Purpose:** Create validated physics library that ALL other phases depend on

---

## PHASE 1 MODULES TO IMPLEMENT (10 Total)

### 1. PlantConstants.cs
Static reference values - NO calculations, just validated constants.

```
// RCS
THERMAL_POWER = 3411 MWt
RCS_VOLUME = 11,500 ft³
RCS_METAL_MASS = 2,200,000 lb
OPERATING_PRESSURE = 2250 psia (2235 psig)
T_HOT = 619°F
T_COLD = 558°F
T_AVG = 588.5°F
CORE_DELTA_T = 61°F
RCS_FLOW_TOTAL = 390,400 gpm

// Pressurizer
PZR_VOLUME = 1800 ft³
PZR_WATER_VOLUME = 1080 ft³ (60%)
PZR_STEAM_VOLUME = 720 ft³ (40%)
PZR_WALL_MASS = 200,000 lb
HEATER_TOTAL = 1800 kW
HEATER_PROP = 500 kW
HEATER_BACKUP = 1300 kW
HEATER_TAU = 20 seconds
SPRAY_MAX = 900 gpm
SPRAY_TEMP = 558°F
SPRAY_EFFICIENCY = 0.85

// Setpoints (psig)
P_NORMAL = 2235
P_HEATERS_ON = 2210
P_SPRAY_ON = 2260
P_SPRAY_FULL = 2280
P_PORV = 2335
P_SAFETY = 2485
P_TRIP_HIGH = 2385
P_TRIP_LOW = 1885

// RCPs
RCP_COUNT = 4
RCP_FLOW_EACH = 97,600 gpm
RCP_SPEED = 1189 rpm
RCP_COASTDOWN_TAU = 12 seconds
RCP_HEAT = 21 MW per pump
LOW_FLOW_TRIP = 0.87

// Natural Circulation
NAT_CIRC_FLOW_MIN = 12,000 gpm
NAT_CIRC_FLOW_MAX = 23,000 gpm
NAT_CIRC_PERCENT = 0.03 to 0.06

// CVCS
LETDOWN_NORMAL = 75 gpm
CHARGING_NORMAL = 87 gpm
SEAL_INJECTION = 32 gpm
BORON_TRANSPORT_TIME = 600 seconds (~10 min, range 5-15 min)
BORIC_ACID_CONC = 7000 ppm
BORON_WORTH = -9 pcm/ppm

// Steam Generators (Model F)
SG_COUNT = 4
SG_AREA = 55,000 ft² each
SG_TUBES = 5,626
SG_STEAM_FLOW = 3.8e6 lb/hr each (15.2e6 total)
SG_STEAM_PRESSURE = 1000 psia
SG_STEAM_TEMP = 545°F
SG_FW_TEMP = 440°F
LMTD_100_PERCENT = 43°F

// Reactor Core
FUEL_ASSEMBLIES = 193
RODS_PER_ASSEMBLY = 264
TOTAL_RODS = 50,952
ACTIVE_HEIGHT = 12 ft
AVG_LINEAR_HEAT = 5.44 kW/ft
PEAK_LINEAR_HEAT = 13 kW/ft

// Reactivity Coefficients
DOPPLER_COEFF = -2.5 pcm/√°R
MTC_HIGH_BORON = +5 pcm/°F
MTC_LOW_BORON = -40 pcm/°F
BETA_DELAYED = 0.0065

// Control Rods
ROD_BANKS = 8
ROD_STEPS_PER_MINUTE = 72
ROD_TOTAL_STEPS = 228
SHUTDOWN_MARGIN = 8000 pcm

// Xenon
XENON_EQUILIBRIUM = 2500-3000 pcm (at 100% power)
XENON_PEAK = 3000-4000 pcm (after trip)
XENON_PEAK_TIME = 8-10 hours

// Decay Heat (ANS 5.1-2005)
DECAY_HEAT_TRIP = 0.07 (7%)
DECAY_HEAT_1MIN = 0.05 (5%)
DECAY_HEAT_10MIN = 0.03 (3%)
DECAY_HEAT_1HR = 0.015 (1.5%)
DECAY_HEAT_1DAY = 0.005 (0.5%)

// Turbine/Generator
TURBINE_OUTPUT = 1150 MW
TURBINE_SPEED = 1800 rpm
TURBINE_EFFICIENCY = 0.34
GOVERNOR_DROOP = 0.05 (5%)
```

### 2. WaterProperties.cs
NIST Steam Table lookups - validated thermodynamic properties.

```csharp
// Key methods:
float SaturationTemperature(float pressure_psia);  // e.g., 2250 psia → 653°F
float SaturationPressure(float temp_F);
float LiquidDensity(float temp_F, float pressure_psia);
float SteamDensity(float temp_F, float pressure_psia);
float LiquidEnthalpy(float temp_F, float pressure_psia);  // ~640 BTU/lb @ 619°F
float SteamEnthalpy(float temp_F, float pressure_psia);
float LatentHeat(float pressure_psia);  // hfg ~465 BTU/lb @ 2250 psia
float SpecificHeat(float temp_F);  // Cp water ~1.3 BTU/lb·°F at operating conditions
```

### 3. SteamThermodynamics.cs
Two-phase and quality calculations.

```csharp
float VoidFraction(float quality, float pressure_psia);
float SteamQuality(float enthalpy, float pressure_psia);
float TwoPhaseEnthalpy(float quality, float pressure_psia);
float TwoPhaseDensity(float quality, float pressure_psia);
```

### 4. ThermalMass.cs
Heat capacity calculations for metal and fluid masses.

```csharp
// Q = m × Cp × ΔT
float MetalHeatCapacity(float mass_lb, string material);  // steel Cp = 0.12 BTU/lb·°F
float FluidHeatCapacity(float mass_lb, float temp_F);
float TemperatureChange(float heat_BTU, float mass_lb, float Cp);
float HeatRequired(float mass_lb, float Cp, float deltaT);
```

### 5. ThermalExpansion.cs
Volume changes with temperature at constant pressure (and vice versa).

```csharp
float SurgeVolume(float RCS_volume, float deltaT, float pressure);
float ExpansionCoefficient(float temp_F, float pressure_psia);
```

### 6. HeatTransfer.cs
Heat exchanger and conduction/convection calculations.

```csharp
// Q = U × A × LMTD
float LMTD(float T_hot_in, float T_hot_out, float T_cold_in, float T_cold_out);
float HeatTransferRate(float U, float A, float LMTD);
float UACalculation(float Q, float LMTD);
float EnthalpyTransport(float flow_lbm_s, float h_in, float h_out);  // Gap #10
```

### 7. FluidFlow.cs
Pump dynamics, natural circulation, pipe flow.

```csharp
// Pump dynamics
float PumpCoastdown(float current_speed, float dt, float tau);  // τ = 12s
float AffinityLaws_Flow(float speed, float nominal_speed, float nominal_flow);  // Q ∝ N
float AffinityLaws_Head(float speed, float nominal_speed, float nominal_head);  // H ∝ N²

// Natural circulation
float NaturalCirculation(float driving_head, float resistance);  // 12,000-23,000 gpm

// Pipe flow (Darcy-Weisbach)
float SurgeLineFlow(float deltaP, float diameter, float length, float friction);
float PressureDrop(float flow, float diameter, float length, float friction);
```

### 8. ReactorKinetics.cs
Point kinetics and reactivity feedback.

```csharp
// Point kinetics: dn/dt = (ρ - β)/Λ × n + Σλᵢcᵢ
float PowerChange(float reactivity, float beta, float lambda, float power, float dt);

// Reactivity feedback
float DopplerReactivity(float fuel_temp_change);  // -2.5 pcm/√°R
float ModeratorReactivity(float coolant_temp_change, float boron_ppm);  // MTC varies with boron
float BoronReactivity(float boron_ppm_change);  // -9 pcm/ppm
float ControlRodReactivity(float rod_position, float[] worth_curve);
float TotalReactivity(float doppler, float moderator, float boron, float rods, float xenon);

// Xenon dynamics
float XenonConcentration(float power_fraction, float time_since_change);
float XenonReactivity(float xenon_concentration);  // -2500 to -4000 pcm
```

### 9. PressurizerPhysics.cs
The heart of Phase 3 - implements Gaps #2-4, #6-8.

```csharp
// Three-region model (Gap #4)
void ThreeRegionModel(ref PressureState state);  // subcooled, saturated, steam

// Phase change
float FlashEvaporation(float depressurization_rate, float liquid_temp, float sat_temp);  // Gap #2
float HeaterSteamGeneration(float power_kW, float tau, float dt);  // Gap #8, τ=20s
float SprayCondensation(float spray_flow, float spray_temp, float steam_temp, float efficiency);  // Gap #3, η=85%

// Wall effects
float WallCondensation(float wall_temp, float steam_temp, float wall_area);  // Gap #6
float Rainout(float steam_temp, float sat_temp);  // Gap #7
```

### 10. CoupledThermo.cs
**CRITICAL** - Iterative P-T-V solver (Gap #1).

```csharp
// This is the most important module - couples pressure, temperature, volume
// RCS is CLOSED: T↑ → expansion → but V fixed → P↑ → ρ↑ → less expansion
// MUST iterate to convergence

bool SolveEquilibrium(ref SystemState state, float deltaT, int maxIterations = 20);
// Returns true if converged

// Expected behavior:
// 10°F Tavg rise → 60-80 psi pressure increase (NOT 0!)
// This is 6-8 psi/°F, validated against EPRI data
```

---

## TEST CATEGORIES (112 Tests)

| Category | Count | Focus |
|----------|-------|-------|
| PlantConstants | 8 | All values match FSAR |
| WaterProperties | 12 | NIST Steam Table accuracy |
| SteamThermodynamics | 10 | Two-phase calculations |
| ThermalMass | 8 | Heat capacity accuracy |
| ThermalExpansion | 8 | Expansion coefficients |
| HeatTransfer | 12 | LMTD, UA, enthalpy transport |
| FluidFlow | 14 | Pumps, natural circ, Darcy-Weisbach |
| ReactorKinetics | 16 | Point kinetics, feedback, xenon |
| PressurizerPhysics | 12 | Flash, spray, heater, wall effects |
| CoupledThermo | 12 | P-T-V iteration, convergence |
| **TOTAL** | **112** | |

---

## KEY VALIDATION CRITERIA

### CoupledThermo (Gap #1) - MOST CRITICAL
```
TEST: 10°F Tavg rise at constant volume
EXPECTED: 60-80 psi pressure increase
WRONG: 0 psi (if not iterating)
SOURCE: EPRI NP-2923, 5-8 psi/°F typical
```

### PressurizerPhysics
```
Heater τ = 20s (63% response at τ, 95% at 3τ)
Spray η = 85% (not 100%)
Flash evaporation self-regulating on depressurization
```

### FluidFlow
```
RCP coastdown τ = 12 ± 3 seconds
Natural circulation = 12,000-23,000 gpm (3-6% of normal)
Affinity laws: Q ∝ N, H ∝ N²
```

### ReactorKinetics
```
Doppler: -2.5 pcm/√°R (NEGATIVE - increases safety)
MTC: +5 pcm/°F (high boron) to -40 pcm/°F (low boron)
Boron: -9 pcm/ppm
β = 0.0065
```

### WaterProperties (NIST validation)
```
Tsat(2250 psia) = 653°F
hfg(2250 psia) = ~465 BTU/lb
h_liquid(619°F) = ~640 BTU/lb
h_sat_liquid(653°F) = ~700 BTU/lb
Enthalpy deficit = ~60 BTU/lb (Gap #10)
```

---

## GAP ANALYSIS REFERENCE

| Gap | Issue | Module | Fix Required |
|-----|-------|--------|--------------|
| #1 | P-T-V coupling | CoupledThermo | Iterative solver |
| #2 | Flash evaporation | PressurizerPhysics | Self-regulating model |
| #3 | Spray efficiency | PressurizerPhysics | η = 85% |
| #4 | Three-region model | PressurizerPhysics | Subcooled/sat/steam |
| #5 | Plant constants | PlantConstants | FSAR values |
| #6 | Wall condensation | PressurizerPhysics | Continuous |
| #7 | Rainout | PressurizerPhysics | When Tsteam < Tsat |
| #8 | Heater lag | PressurizerPhysics | τ = 20s |
| #9 | Surge line | FluidFlow | Darcy-Weisbach |
| #10 | Enthalpy transport | HeatTransfer | ~60 BTU/lb deficit |
| #11 | Water properties | WaterProperties | NIST tables |
| #12 | Steam properties | WaterProperties | NIST tables |
| #13 | Two-phase | SteamThermodynamics | Quality/void |

---

## PHASE 1 EXIT GATE CRITERIA

1. All 112 unit tests passing
2. 10°F → 60-80 psi verified (Gap #1)
3. All thermodynamic values within 1% of NIST
4. Heater τ = 20s verified
5. Spray η = 85% verified
6. Natural circulation 12,000-23,000 gpm verified
7. All reactivity coefficients match FSAR
8. Code review complete
9. No modifications without re-running all tests

---

## RECOMMENDED DEVELOPMENT ORDER

1. **PlantConstants.cs** - Define all values first
2. **WaterProperties.cs** - Foundation for everything
3. **SteamThermodynamics.cs** - Two-phase depends on #2
4. **ThermalMass.cs** - Simple, standalone
5. **ThermalExpansion.cs** - Depends on #2
6. **HeatTransfer.cs** - Depends on #2
7. **FluidFlow.cs** - Mostly standalone
8. **ReactorKinetics.cs** - Standalone physics
9. **PressurizerPhysics.cs** - Depends on #2, #3, #4, #5
10. **CoupledThermo.cs** - LAST - depends on everything

---

## DATA SOURCES

- Westinghouse 4-Loop FSAR (South Texas, Vogtle, V.C. Summer)
- NIST Steam Tables (webbook.nist.gov)
- EPRI NP-2923 (Pressurizer Thermal-Hydraulics)
- NUREG/CR-5535 (RELAP5/MOD3)
- ANS 5.1-2005 (Decay Heat Standard)

---

## FILES IN PROJECT

### Available Now (from this chat session):
- Critical_Phase3_Complete.docx (78 tests)
- Critical_Phase4_Complete.docx (65 tests)  
- Critical_Phase5_Complete.docx (72 tests)
- Critical_Phase6_Complete.docx (68 tests)
- Critical_Validation_Report.md
- Phase_Mapping_Analysis.md

### Needed for Phase 1:
- Critical_Phase1_Complete.docx (112 tests) - from previous session
- Critical_Phase2_Complete.docx (85 tests) - from previous session

---

## STARTING PHASE 1 IN NEW CHAT

Paste this prompt to start:

```
I'm developing a PWR nuclear power plant simulator called "CRITICAL: Master the Atom" in Unity/C#.

I need to implement Phase 1: Physics Foundation (112 tests).

The 10 modules are:
1. PlantConstants.cs
2. WaterProperties.cs  
3. SteamThermodynamics.cs
4. ThermalMass.cs
5. ThermalExpansion.cs
6. HeatTransfer.cs
7. FluidFlow.cs
8. ReactorKinetics.cs
9. PressurizerPhysics.cs
10. CoupledThermo.cs

Reference: Westinghouse 4-loop PWR, 3411 MWt

Key requirement: CoupledThermo must implement iterative P-T-V solver where 10°F temperature rise produces 60-80 psi pressure increase (not 0).

Let's start with PlantConstants.cs - all the reference values.
```

---

**Document Version:** 1.0
**Created:** February 4, 2026
**Validated Against:** Westinghouse 4-Loop FSAR, NIST Steam Tables
