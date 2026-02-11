# Phase 1 Physics Module → Phases 3-6 Mapping Analysis

## PHASE 1 MODULE INVENTORY (10 Modules, 112 Tests)

### Module 1: PlantConstants.cs (5 tests)
**Purpose:** Westinghouse 4-loop reference values
**Key Constants:**
- RCS: 3411 MWt, 11,500 ft³, 2,200,000 lb metal, 2250 psia, Thot=619°F, Tcold=558°F
- Pressurizer: 1800 ft³ total, 1080 ft³ water, 720 ft³ steam, 200,000 lb wall
- Heaters: 1800 kW (500 proportional + 1300 backup), τ=20s
- Spray: 900 gpm max, 558°F (Tcold)
- Surge line: 10-14" diameter, ~50 ft length
- Setpoints: Normal=2235, Heaters ON<2210, Spray ON>2260, PORV=2335, Safety=2485

**Used By:**
- Phase 3: All pressurizer parameters
- Phase 4: RCP parameters (97,600 gpm/pump, τ=12s coastdown), CVCS flows
- Phase 5: SG parameters (55,000 ft² area, 4 SGs)
- Phase 6: Turbine parameters (1150 MW, 1800 rpm)

---

### Module 2: WaterProperties.cs (18 tests)
**Purpose:** Water/steam thermodynamic properties
**Key Functions:**
- SaturationTemperature(pressure) → °F
- SaturationPressure(temperature) → psia
- LiquidDensity(T, P) → lb/ft³
- SteamDensity(P) → lb/ft³
- LiquidEnthalpy(T) → BTU/lb [GAP #12 fix]
- SteamEnthalpy(P) → BTU/lb [GAP #12 fix]
- LatentHeat(P) → BTU/lb
- SpecificHeat_Cp(T, P) → BTU/lb·°F
- Compressibility(T, P) → 1/psi [GAP #11 fix]

**Used By:**
- Phase 3: Saturation conditions, enthalpy for surge water
- Phase 4: Boron transport calculations (density needed)
- Phase 5: Secondary side saturation, heat transfer calcs
- Phase 6: Steam conditions to turbine

---

### Module 3: SteamThermodynamics.cs (10 tests)
**Purpose:** Steam space behavior
**Key Functions:**
- PolytropicPressure(P1, V1, V2, n=1.3) → P2
- SteamMass(P, V, T) → lb
- VoidFraction(steamVol, totalVol) → fraction [GAP #13 fix]
- SteamQuality(h, P) → quality [GAP #13 fix]
- IdealGasCorrection(P, T) → correction factor

**Used By:**
- Phase 3: Steam compression during surge, void tracking
- Phase 5: Steam dome behavior in SGs

---

### Module 4: ThermalMass.cs (6 tests)
**Purpose:** Heat capacity calculations
**Key Functions:**
- WaterThermalMass(volume, T, P) → BTU/°F
- MetalThermalMass(mass, material) → BTU/°F
- CombinedThermalMass(water, metal) → BTU/°F
- TemperatureChange(Q, thermalMass) → ΔT

**Used By:**
- Phase 3: Pressurizer wall thermal mass (200,000 lb)
- Phase 4: RCS thermal mass for temperature response
- Phase 5: SG tube bundle thermal mass

---

### Module 5: ThermalExpansion.cs (6 tests)
**Purpose:** Volume change with temperature
**Key Functions:**
- ExpansionCoefficient(T, P) → 1/°F
- VolumeChange(V, T1, T2, P) → ΔV
- SurgeVolume(rcsVolume, ΔTavg, P) → ft³

**Used By:**
- Phase 3: Calculates surge flow into/out of pressurizer
- Phase 4: Affects PZR level during temperature transients

---

### Module 6: CoupledThermo.cs (12 tests) [GAP #1 FIX]
**Purpose:** P-T-V iterative solver - THE CRITICAL MODULE
**Key Functions:**
- SolveEquilibrium(state, ΔT, maxIter=10, tol=0.1) → (P, V, T)
- WaterCompressibility(T, P) → compressibility
- SubcoolingMargin(T, P) → °F to saturation
- CalculatePressureChange(state, ΔT, dt) → ΔP

**Key Insight:** RCS is CLOSED. Temperature rise → expansion → BUT volume is FIXED → 
steam compressed → pressure rises → water denser → less expansion. ITERATIVE!

**Used By:**
- Phase 3: EVERY pressure calculation must use this, not simple polytropic
- Phase 4: Temperature changes from flow/boron affect pressure
- Phase 5: Heat removal changes affect RCS pressure

---

### Module 7: PressurizerPhysics.cs (24 tests) [GAPS #2-8 FIX]
**Purpose:** Non-equilibrium pressurizer model - THE MOST COMPLEX MODULE
**Key Functions:**
- **FlashEvaporation(liquidMass, P1, P2, dt)** → steam generated [GAP #2]
  - During outsurge/depressurization, saturated water FLASHES to steam
  - This RETARDS pressure drop (self-regulating)
  
- **SprayCondensation(sprayFlow, sprayTemp, steamTemp, P, dt)** → steam condensed [GAP #3]
  - NOT instantaneous - finite heat transfer time
  - Droplet efficiency ~80-90%, not 100%
  
- **WallCondensation(steamTemp, wallTemp, wallArea, htc, dt)** → steam condensed [GAP #6]
  - Upper walls cooler than steam → condensation
  - Wall thermal mass = 200,000 lb steel
  
- **Rainout(steamTemp, satTemp, steamMass, dt)** → steam condensed [GAP #7]
  - Bulk condensation when vapor space subcools after spray
  
- **HeaterSteamGeneration(heaterPower, P, heaterTemp, dt)** → steam generated [GAP #8]
  - Heater time constant τ = 20 seconds
  - heaterTemp lags setpoint exponentially
  
- **ThreeRegionModel(state)** → (subcooledLiquid, saturatedLiquid, steam) [GAP #4]
  - Subcooled surge water (619°F) ≠ saturated PZR water (653°F)
  - Must track mixing at interface

**Used By:**
- Phase 3: THIS IS THE CORE OF PHASE 3 - all pressurizer dynamics

---

### Module 8: ReactorKinetics.cs (10 tests)
**Purpose:** Neutronics and feedback
**Key Functions:**
- PointKinetics(power, reactivity, dt) → new power
- DopplerFeedback(fuelTemp) → pcm (-2.5 pcm/√°R)
- MTCFeedback(Tavg, boronPPM) → pcm (+5 to -40 pcm/°F)
- BoronReactivity(ppm) → pcm (-9 pcm/ppm)
- DelayedNeutronFraction() → 0.0065
- PromptNeutronLifetime() → ~20 μs

**Used By:**
- Phase 2: Core power calculations
- Phase 4: Boron worth for dilution/boration calculations

---

### Module 9: FluidFlow.cs (8 tests) [GAP #9 FIX]
**Purpose:** Pumps, surge line, natural circulation
**Key Functions:**
- **SurgeLineFlow(ΔP, diameter, length, friction)** → gpm [GAP #9]
  - 10-14" pipe, ~50 ft long
  - Flow resistance and transit delay
  
- PumpFlow(speed, head, characteristics) → gpm
- PumpCoastdown(speed, inertia, friction, dt) → new speed
- NaturalCirculation(ΔT, height, density) → flow
- AffinityLaws(speed1, speed2, Q1, H1) → (Q2, H2)

**Used By:**
- Phase 3: Surge line hydraulics (flow resistance, delay)
- Phase 4: RCP coastdown (τ=12s), natural circulation

---

### Module 10: HeatTransfer.cs (6 tests) [GAP #10 FIX]
**Purpose:** Enthalpy transport and heat transfer
**Key Functions:**
- **EnthalpyTransport(massFlow, h_in, h_out)** → BTU/s [GAP #10]
  - Surge water at 619°F has h ≈ 640 BTU/lb
  - Saturated PZR water at 653°F has h ≈ 700 BTU/lb
  - Energy deficit = 60 BTU/lb must come from PZR liquid
  
- ConvectiveHeatTransfer(htc, area, ΔT) → BTU/s
- LMTD(Th_in, Th_out, Tc_in, Tc_out) → °F
- UACalculation(Q, LMTD) → BTU/hr·°F

**Used By:**
- Phase 3: Enthalpy of surge water affects PZR energy balance
- Phase 5: SG heat transfer (primary to secondary)

---

## PHASE 3 REQUIREMENTS FROM PHASE 1

Phase 3 (Pressurizer) MUST use these Phase 1 modules:

| Phase 1 Module | Phase 3 Usage | Critical Functions |
|----------------|---------------|-------------------|
| PlantConstants | All reference values | PZR dimensions, heater specs, setpoints |
| WaterProperties | Saturation conditions | SaturationTemperature, LiquidEnthalpy, LatentHeat |
| SteamThermodynamics | Steam compression | PolytropicPressure (but via CoupledThermo!) |
| ThermalMass | Wall effects | MetalThermalMass for 200,000 lb wall |
| ThermalExpansion | Surge calculation | SurgeVolume from RCS ΔT |
| **CoupledThermo** | **ALL pressure calcs** | SolveEquilibrium - NEVER bypass this! |
| **PressurizerPhysics** | **ALL dynamics** | Flash, spray, wall, rainout, heater lag |
| FluidFlow | Surge line | SurgeLineFlow for resistance/delay |
| HeatTransfer | Energy balance | EnthalpyTransport for surge water deficit |

### Phase 3 Test Count Derivation:
- PressurizerPhysics integration: 24 tests (mirrors Phase 1)
- CoupledThermo integration: 12 tests
- Surge line hydraulics: 8 tests
- Heater/Spray control: 10 tests
- PORV/Safety valves: 6 tests
- Level control: 6 tests
- Integration/transient: 12 tests
- **TOTAL: ~78 tests**

---

## PHASE 4 REQUIREMENTS FROM PHASE 1

Phase 4 (CVCS & RCPs) MUST use these Phase 1 modules:

| Phase 1 Module | Phase 4 Usage | Critical Functions |
|----------------|---------------|-------------------|
| PlantConstants | RCP specs, CVCS flows | 97,600 gpm/pump, τ=12s, charging/letdown |
| WaterProperties | Boron transport | Density for concentration calcs |
| FluidFlow | **RCP coastdown** | PumpCoastdown, AffinityLaws, NaturalCirculation |
| ReactorKinetics | Boron worth | BoronReactivity (-9 pcm/ppm) |
| HeatTransfer | RCP heat addition | 21 MW per running pump |

### Phase 4 Test Count Derivation:
- CVCS flow balance: 8 tests
- Boron transport delay: 10 tests (5-15 min delay model)
- VCT level control: 6 tests
- RCP coastdown: 12 tests (τ=12s validation)
- Natural circulation: 8 tests
- Flow trip logic: 6 tests
- Integration: 15 tests
- **TOTAL: ~65 tests**

---

## PHASE 5 REQUIREMENTS FROM PHASE 1

Phase 5 (Steam Generators) MUST use these Phase 1 modules:

| Phase 1 Module | Phase 5 Usage | Critical Functions |
|----------------|---------------|-------------------|
| PlantConstants | SG parameters | 55,000 ft², 4 SGs, Model F specs |
| WaterProperties | Secondary saturation | SaturationTemperature(1000 psia) = 545°F |
| ThermalMass | Tube bundle mass | Metal thermal mass for lag |
| HeatTransfer | **Primary-secondary** | LMTD, UACalculation |
| CoupledThermo | RCS response | Temperature changes affect pressure |

### Phase 5 Test Count Derivation:
- Heat transfer model: 12 tests
- Secondary pressure dynamics: 10 tests
- Shrink/swell: 10 tests
- Feedwater control: 8 tests
- Steam dump: 8 tests
- Level control: 8 tests
- MSIV logic: 4 tests
- Integration: 12 tests
- **TOTAL: ~72 tests**

---

## PHASE 6 REQUIREMENTS FROM PHASE 1

Phase 6 (Turbine/Generator) uses Phase 1 modules indirectly through integration:

| Phase 1 Module | Phase 6 Usage | Critical Functions |
|----------------|---------------|-------------------|
| PlantConstants | Turbine specs | 1150 MW, 1800 rpm, efficiency |
| WaterProperties | Steam conditions | Inlet conditions to turbine |
| CoupledThermo | Load following | Load changes propagate to RCS |

### Phase 6 Test Count Derivation:
- Governor control: 10 tests
- Grid sync: 8 tests
- Load following: 12 tests
- Turbine trip: 8 tests
- Generator output: 6 tests
- Integration (full plant): 24 tests
- **TOTAL: ~68 tests**

---

## CRITICAL CONSISTENCY CHECKS

### Check 1: Pressure Calculation Chain
```
Phase 3 Pressurizer:
  - User changes rod position → Tavg changes
  - ThermalExpansion.SurgeVolume() calculates surge
  - FluidFlow.SurgeLineFlow() applies hydraulic resistance [GAP #9]
  - Water enters PZR at 619°F (subcooled vs 653°F saturation)
  - HeatTransfer.EnthalpyTransport() tracks energy deficit [GAP #10]
  - PressurizerPhysics.ThreeRegionModel() handles mixing [GAP #4]
  - CoupledThermo.SolveEquilibrium() calculates new pressure [GAP #1]
  - PressurizerPhysics.FlashEvaporation() if depressurizing [GAP #2]
  - Heaters/Spray respond based on setpoints
  - PressurizerPhysics.HeaterSteamGeneration() with τ=20s [GAP #8]
  - PressurizerPhysics.SprayCondensation() with efficiency [GAP #3]
  - PressurizerPhysics.WallCondensation() ongoing [GAP #6]
  - PressurizerPhysics.Rainout() if vapor subcools [GAP #7]
```

### Check 2: Temperature-Flow-Power Chain
```
Phase 4 → Phase 5 → Phase 6:
  - Turbine governor opens valves (Phase 6)
  - Steam demand increases to SGs (Phase 5)
  - SG heat transfer increases (HeatTransfer.LMTD)
  - Tcold drops, then Thot drops
  - Tavg drops → positive MTC → power rises (ReactorKinetics)
  - OR: Rod control withdraws rods
  - RCS temperature change → pressure change (CoupledThermo)
  - Pressurizer responds (Phase 3)
```

### Check 3: All 13 Gaps Addressed
| Gap | Module | Verified In |
|-----|--------|-------------|
| 1 | CoupledThermo | Phase 3, 4, 5 |
| 2 | PressurizerPhysics | Phase 3 |
| 3 | PressurizerPhysics | Phase 3 |
| 4 | PressurizerPhysics | Phase 3 |
| 5 | PlantConstants | All phases |
| 6 | PressurizerPhysics | Phase 3 |
| 7 | PressurizerPhysics | Phase 3 |
| 8 | PressurizerPhysics | Phase 3 |
| 9 | FluidFlow | Phase 3, 4 |
| 10 | HeatTransfer | Phase 3, 5 |
| 11 | WaterProperties | All phases |
| 12 | WaterProperties | Phase 3, 5 |
| 13 | SteamThermodynamics | Phase 3 |

---

## DOCUMENT GENERATION PLAN

Create 4 new documents:
1. **Critical_Phase3_Pressurizer_Complete.docx** (~78 tests)
2. **Critical_Phase4_CVCS_RCPs_Complete.docx** (~65 tests)
3. **Critical_Phase5_Steam_Generators_Complete.docx** (~72 tests)
4. **Critical_Phase6_Turbine_Generator_Complete.docx** (~68 tests)

Each document will:
1. List Phase 1 module dependencies explicitly
2. Show which Gap Analysis fixes are used
3. Include comprehensive test matrices
4. Include exit gate criteria
5. Follow Phase 1/2 Complete document format
