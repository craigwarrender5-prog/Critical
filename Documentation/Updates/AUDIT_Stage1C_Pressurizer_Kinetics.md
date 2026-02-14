# AUDIT: Stage 1C - Pressurizer & Kinetics
**Version:** 1.0.0.0  
**Date:** 2026-02-06  
**Scope:** PressurizerPhysics, SolidPlantPressure, ReactorKinetics, RVLISPhysics

---

## EXECUTIVE SUMMARY

### Files Analyzed
| File | Size | Lines | Status |
|------|------|-------|--------|
| PressurizerPhysics.cs | 37 KB | ~850 | **GOLD STANDARD** |
| SolidPlantPressure.cs | 28 KB | ~520 | **GOLD STANDARD** |
| ReactorKinetics.cs | 24 KB | ~530 | **GOLD STANDARD** |
| RVLISPhysics.cs | 9 KB | ~220 | **GOLD STANDARD** |

### Critical Findings

| Finding | Severity | Status |
|---------|----------|--------|
| Gaps #2-8 (PZR physics) properly implemented | ✅ | RESOLVED |
| Solid PZR operations added (Phase 2 M1/M2) | ✅ | Complete |
| Two-phase heating extracted (Audit Fix 7.1) | ✅ | Complete |
| Point kinetics uses semi-implicit method | ✅ | Numerically stable |
| RVLIS extracted from engine (Audit Fix #5) | ✅ | Complete |
| CVCS PI controller for solid plant pressure | ✅ | Properly tuned |

### No Critical Issues Found in Sub-Stage 1C

All four modules are well-implemented with proper physics, comprehensive validation, and Phase 2 fixes applied.

---

## FILE 1: PressurizerPhysics.cs (GOLD STANDARD)

### Purpose
Pressurizer thermal-hydraulic calculations. Models three-region (subcooled, saturated, steam) behavior with phase change dynamics.

### Implements
- **Gap #2** - Flash evaporation during outsurge
- **Gap #3** - Spray condensation dynamics (η = 85%)
- **Gap #4** - Non-equilibrium three-region model
- **Gap #6** - Wall condensation
- **Gap #7** - Rainout (bulk condensation)
- **Gap #8** - Heater thermal dynamics (τ = 20s)

### Phase 2 Fixes
- **M1/M2**: Added solid PZR support (IsSolid, BubbleFormed, SolidPressurizerUpdate)
- **Audit Fix 7.1**: Added TwoPhaseHeatingUpdate() for isolated heating

### PressurizerState Struct
```csharp
public struct PressurizerState
{
    // Primary state
    public float Pressure;          // psia
    public float PressureRate;      // psi/sec
    public float WaterMass;         // lb
    public float SteamMass;         // lb
    public float WaterVolume;       // ft³
    public float SteamVolume;       // ft³
    
    // Temperatures
    public float WallTemp;          // °F
    public float SteamTemp;         // °F
    public float WaterTemp;         // °F (Phase 2: tracks during solid ops)
    
    // Heater state
    public float HeaterEffectivePower; // kW (after thermal lag)
    
    // Phase 2: Bubble formation
    public bool BubbleFormed;       // True if steam bubble exists
    
    // Phase change rates (lb/sec)
    public float FlashRate;
    public float SprayCondRate;
    public float HeaterSteamRate;
    public float WallCondRate;
    public float RainoutRate;
    public float NetSteamRate;
    
    // Derived
    public float Level => (WaterVolume / PZR_TOTAL_VOLUME) * 100f;
    public float TotalMass => WaterMass + SteamMass;
    public bool IsSolid => !BubbleFormed;
}
```

### Public Methods

#### Solid PZR Operations (Phase 2)
| Method | Parameters | Returns | Notes |
|--------|------------|---------|-------|
| SolidPressurizerUpdate(ref state, P_kW, T_rcs, P, dt) | ... | void | Water-solid PZR |
| CheckBubbleFormation(state) | PressurizerState | bool | T_pzr ≥ T_sat? |
| FormBubble(ref state, level%) | PressurizerState, % | void | Solid → two-phase |
| InitializeSolidState(P, T) | psia, °F | PressurizerState | Cold shutdown init |

#### Two-Phase Heating (Audit Fix 7.1)
| Method | Parameters | Returns | Notes |
|--------|------------|---------|-------|
| TwoPhaseHeatingUpdate(...) | MW, °F, °F, psia, ft³, BTU/°F, hr | TwoPhaseHeatingResult | Isolated heating |
| ValidateTwoPhaseHeating() | - | bool | 4 validation tests |

**TwoPhaseHeatingResult struct:**
```csharp
public struct TwoPhaseHeatingResult
{
    public float T_pzr;         // Updated PZR temperature (°F)
    public float T_rcs;         // Updated RCS temperature (°F)
    public float Pressure;      // Updated pressure (psia)
    public float SurgeFlow;     // Thermal expansion surge flow (gpm)
    public float dP;            // Pressure change this step (psi)
}
```

**DAMPING_FACTOR = 0.5**: Physics basis documented:
- Steam bubble compressibility absorbs expansion
- Non-equilibrium effects at flash interface
- Wall heat capacity retards temperature swings
- Empirically matches Westinghouse LOFTRAN results

#### Three-Region Model (Gap #4)
| Method | Parameters | Returns | Notes |
|--------|------------|---------|-------|
| ThreeRegionUpdate(ref state, surge_gpm, T_surge, spray_gpm, T_spray, P_kW, dt) | ... | void | Main update |

#### Flash Evaporation (Gap #2)
| Method | Parameters | Returns | Notes |
|--------|------------|---------|-------|
| FlashEvaporationRate(P, dP/dt, m_water) | psia, psi/s, lb | lb/sec | Only during depressurization |
| WaterSuperheat(T_water, P) | °F, psia | °F | Superheat margin |

#### Spray Condensation (Gap #3)
| Method | Parameters | Returns | Notes |
|--------|------------|---------|-------|
| SprayCondensationRate(flow_gpm, T_spray, P) | gpm, °F, psia | lb/sec | η = 85% |
| SprayFlowDemand(P_psig) | psig | gpm | Controller output |

#### Heater Dynamics (Gap #8)
| Method | Parameters | Returns | Notes |
|--------|------------|---------|-------|
| HeaterSteamRate(demand_kW, effective_kW, P) | kW, kW, psia | lb/sec | |
| HeaterLagResponse(current, demand, τ, dt) | kW, kW, sec, sec | kW | τ = 20s |
| HeaterPowerDemand(P_psig) | psig | kW | Controller output |
| ValidateHeaterLag() | - | bool | 63%@τ, 95%@3τ |

#### Wall Condensation (Gap #6)
| Method | Parameters | Returns | Notes |
|--------|------------|---------|-------|
| WallCondensationRate(T_wall, P, A_wall) | °F, psia, ft² | lb/sec | |

#### Rainout (Gap #7)
| Method | Parameters | Returns | Notes |
|--------|------------|---------|-------|
| RainoutRate(T_steam, P, m_steam) | °F, psia, lb | lb/sec | Bulk condensation |

#### Initialization & Utilities
| Method | Parameters | Returns | Notes |
|--------|------------|---------|-------|
| InitializeSteadyState(P, level%) | psia, % | PressurizerState | Two-phase steady state |
| SurgeMassFlowRate(flow_gpm, T_surge, P) | gpm, °F, psia | lb/sec | |
| MassBalanceError(state, m_initial, Δm_in) | ..., lb, lb | fraction | Conservation check |
| TotalEnergy(state) | PressurizerState | BTU | Total PZR energy |

### Dependencies
- WaterProperties (saturation, density, enthalpy, Cp, latent heat)
- HeatTransfer (SurgeLineHeatTransfer, CondensingHTC)
- ThermalMass (PressurizerWallHeatCapacity)
- ThermalExpansion (ExpansionCoefficient, PressureChangeFromTemp)
- PlantConstants (all PZR constants)

### Validation (13 tests)
1. Flash rate > 0 during depressurization ✅
2. Flash rate = 0 during pressurization ✅
3. Spray condensation > 0 with subcooled spray ✅
4. Spray rate 15-30 lb/sec at full flow ✅
5. Heater steam rate > 0 ✅
6. Heater rate 3-6 lb/sec at full power ✅
7. Heater lag: 63% at τ, 95% at 3τ ✅
8. Wall condensation > 0 when wall cold ✅
9. Rainout > 0 when steam subcooled ✅
10. Flash increases with faster depressurization ✅
11. Solid state init: no bubble, no steam ✅
12. Bubble formation triggers at T_sat ✅
13. FormBubble creates steam space at 25% level ✅

---

## FILE 2: SolidPlantPressure.cs (GOLD STANDARD)

### Purpose
Solid plant pressure-volume-temperature coupling and CVCS pressure control during cold shutdown through bubble formation.

### Physics Model
```
During solid PZR (no steam bubble):
  - Pressure controlled by CVCS charging/letdown balance
  - Thermal expansion creates excess volume
  - CVCS removes excess to maintain pressure in 320-400 psig band
  - If CVCS cannot keep up, RHR relief opens at 450 psig

Fundamental equation:
  dP/dt = (dV_thermal/dt - dV_cvcs/dt) / (V_total × κ)
  
where:
  dV_thermal/dt = V × β × dT/dt   (thermal expansion rate)
  dV_cvcs/dt    = (letdown - charging) / ρ  (net volume removal)
  κ             = isothermal compressibility
```

### SolidPlantState Struct
```csharp
public struct SolidPlantState
{
    // Primary state
    public float Pressure;              // psia
    public float T_pzr;                 // PZR water temperature (°F)
    public float T_rcs;                 // RCS bulk temperature (°F)
    
    // PZR thermal state
    public float HeaterEffectivePower;  // kW (after τ=20s lag)
    public float PzrWaterMass;          // lb
    public float PzrWallTemp;           // °F
    
    // CVCS controller state
    public float ControllerIntegral;    // gpm·sec
    public float LetdownFlow;           // gpm
    public float ChargingFlow;          // gpm
    
    // Relief valve
    public float ReliefFlow;            // gpm (0 if closed)
    
    // Calculated rates
    public float PressureRate;          // psi/hr
    public float PzrHeatRate;           // °F/hr
    public float ThermalExpansionRate;  // ft³/hr
    public float CVCSRemovalRate;       // ft³/hr
    public float ExcessVolumeRemoved;   // gallons cumulative
    public float SurgeFlow;             // gpm
    public float SurgeLineHeat_MW;      // MW
    
    // Bubble formation
    public bool BubbleFormed;
    public float BubbleFormationTemp;   // °F
    public float T_sat;                 // °F at current P
    
    // Control status
    public float PressureSetpoint;      // psia
    public float PressureError;         // psi
    public bool InControlBand;          // 320-400 psig?
}
```

### CVCS PI Controller Tuning
| Constant | Value | Notes |
|----------|-------|-------|
| KP_PRESSURE | 0.5 | gpm per psi error |
| KI_PRESSURE | 0.02 | gpm per psi·sec |
| INTEGRAL_LIMIT_GPM | 40 | Anti-windup limit |
| MAX_LETDOWN_ADJUSTMENT_GPM | 50 | Controller output limit |
| MIN_LETDOWN_GPM | 20 | Floor |
| MAX_LETDOWN_GPM | 120 | Ceiling (RHR crossconnect) |

### RHR Relief Valve Parameters
| Constant | Value | Notes |
|----------|-------|-------|
| RELIEF_SETPOINT_PSIG | 450 | Opening setpoint |
| RELIEF_ACCUMULATION_PSI | 20 | Full-open accumulation |
| RELIEF_CAPACITY_GPM | 200 | Full-open capacity |
| RELIEF_RESEAT_PSIG | 445 | Hysteresis reseat |

### Public Methods

| Method | Parameters | Returns | Notes |
|--------|------------|---------|-------|
| Initialize(P, T_rcs, T_pzr, letdown, charging) | psia, °F, °F, gpm, gpm | SolidPlantState | Cold shutdown init |
| Update(ref state, P_kW, letdown, charging, rcsCap, dt_hr) | ..., BTU/°F, hr | void | Main update |
| CalculateReliefFlow(P_psig, currentlyOpen) | psig, bool | gpm | With hysteresis |
| EstimateTimeToBubble(state) | SolidPlantState | hours | Remaining heatup time |
| GetStatusString(state) | SolidPlantState | string | Display text |

### Update Physics Steps
1. **PZR Temperature**: Heaters + surge line conduction - insulation loss
2. **RCS Temperature**: Surge line heat in - insulation loss
3. **Thermal Expansion**: β × V × ΔT for both PZR and RCS
4. **CVCS Controller**: PI on letdown to hold pressure at setpoint
5. **Relief Valve**: Proportional above 450 psig with hysteresis
6. **Pressure Change**: dP = dV_net / (V × κ)
7. **Bubble Formation**: Check T_pzr ≥ T_sat

### Dependencies
- WaterProperties (density, Cp, saturation)
- ThermalExpansion (ExpansionCoefficient, Compressibility)
- HeatTransfer (InsulationHeatLoss, SurgeLineHeatTransfer)
- ThermalMass (PressurizerWallHeatCapacity)
- PressurizerPhysics.HeaterLagResponse()
- PlantConstants (solid plant constants)

### Validation (10 tests)
1. Initialization produces valid state ✅
2. Heating raises PZR temperature ✅
3. Thermal expansion causes nonzero pressure change ✅
4. Relief valve opens above 450 psig ✅
5. Relief valve closed at 400 psig ✅
6. CVCS increases letdown when pressure high ✅
7. CVCS decreases letdown when pressure low ✅
8. Bubble formation at T_sat ✅
9. Surge flow > 0 when PZR heating ✅
10. Surge line heat > 0 when T_pzr > T_rcs ✅

---

## FILE 3: ReactorKinetics.cs (GOLD STANDARD)

### Purpose
Reactor kinetics calculations including point kinetics with 6 delayed neutron groups, reactivity feedback, and xenon dynamics.

### Point Kinetics Model
```
Semi-implicit (prompt-jump) method:
  - Standard in PARCS, RELAP, SIMULATE
  - Analytically eliminates prompt timescale
  - Stable for practical timesteps

Equations:
  dn/dt = (ρ - β)/Λ × n + Σλᵢcᵢ
  dcᵢ/dt = βᵢ/Λ × n - λᵢcᵢ

For ρ < β (delayed supercritical/subcritical):
  Implicit Euler on precursors:
    cᵢ_new = (cᵢ_old + βᵢ/Λ × n × dt) / (1 + λᵢ × dt)
  Prompt-jump power:
    n = Λ × Σλᵢcᵢ_new / (β - ρ)

For ρ ≥ β (prompt supercritical):
  Sub-stepped explicit integration with stability limit
```

### Delayed Neutron Data
| Group | βᵢ | λᵢ (1/s) |
|-------|-----|---------|
| 1 | 0.000215 | 0.0124 |
| 2 | 0.001424 | 0.0305 |
| 3 | 0.001274 | 0.111 |
| 4 | 0.002568 | 0.301 |
| 5 | 0.000748 | 1.14 |
| 6 | 0.000273 | 3.01 |
| **Total** | **0.0065** | - |

**Generation time**: Λ = 2×10⁻⁵ s

### Public Methods

#### Point Kinetics
| Method | Parameters | Returns | Notes |
|--------|------------|---------|-------|
| PointKinetics(power, ρ_pcm, precursors[], dt, out newPrecursors[]) | float, pcm, float[6], sec | float | Main solver |
| EquilibriumPrecursors(power) | float | float[6] | cᵢ = βᵢn/(Λλᵢ) |
| ReactorPeriod(ρ_pcm) | pcm | sec | Period from reactivity |
| PromptJump(P_initial, ρ_pcm) | float, pcm | float | β/(β-ρ) jump |

#### Reactivity Coefficients
| Method | Parameters | Returns | Notes |
|--------|------------|---------|-------|
| DopplerReactivity(ΔT_fuel, T_fuel_initial) | °F, °F | pcm | α_D × (√T₂ - √T₁) |
| ModeratorTempCoefficient(boron_ppm) | ppm | pcm/°F | +5 at BOL, -40 at EOL |
| ModeratorReactivity(ΔT, boron_ppm) | °F, ppm | pcm | MTC × ΔT |
| BoronReactivity(Δboron_ppm) | ppm | pcm | -9 pcm/ppm |
| TotalReactivity(doppler, mod, boron, rods, xenon) | pcm×5 | pcm | Sum |

#### Control Rods
| Method | Parameters | Returns | Notes |
|--------|------------|---------|-------|
| ControlRodReactivity(steps, totalWorth_pcm) | steps, pcm | pcm | S-curve integral worth |
| DifferentialRodWorth(steps, totalWorth_pcm) | steps, pcm | pcm/step | d/dx of above |

#### Xenon Dynamics
| Method | Parameters | Returns | Notes |
|--------|------------|---------|-------|
| XenonEquilibrium(powerFraction) | 0-1 | pcm | -2750 pcm at 100% |
| XenonTransient(xenon_initial, P_final, t_hr) | pcm, 0-1, hr | pcm | Post-change transient |
| XenonRate(xenon_current, powerFraction) | pcm, 0-1 | pcm/hr | Rate of change |

#### Decay Heat (ANS 5.1-2005)
| Method | Parameters | Returns | Notes |
|--------|------------|---------|-------|
| DecayHeatFraction(t_sec) | sec | fraction | P/P₀ |
| DecayHeatPower(t_sec) | sec | MW | fraction × 3411 |

**Decay heat timeline:**
| Time | Fraction |
|------|----------|
| Trip | 7% |
| 1 min | 5% |
| 10 min | 3% |
| 1 hr | 1.5% |
| 1 day | 0.5% |

### Dependencies
- PlantConstants (DOPPLER_COEFF, MTC_*, BORON_WORTH, RANKINE_OFFSET, etc.)

### Validation (8 tests)
1. Σβᵢ = β_total = 0.0065 ✅
2. Doppler < 0 for temperature increase ✅
3. MTC > 0 at high boron (1500 ppm) ✅
4. MTC < 0 at low boron (100 ppm) ✅
5. Boron addition → reactivity decrease ✅
6. Equilibrium xenon ≈ -2750 pcm at 100% ✅
7. Decay heat at 1 min ≈ 5% ✅
8. Equilibrium precursors array length = 6 ✅

---

## FILE 4: RVLISPhysics.cs (GOLD STANDARD)

### Purpose
Reactor Vessel Level Indication System physics. Extracts RVLIS calculation from engine (Audit Fix #5).

### Physics Model
```
Three measurement ranges:

Dynamic Range: Valid with RCPs running
  - Flow-compensated ΔP across core
  - Reads 100% at normal inventory

Full Range: Valid without RCPs
  - Static ΔP from bottom to top of vessel
  - Alarm at <90%

Upper Range: Valid without RCPs
  - ΔP from mid-vessel to top
  - Sensitive to upper head voiding
```

### RVLISState Struct
```csharp
public struct RVLISState
{
    public float DynamicRange;      // % (0-100), valid with RCPs
    public float FullRange;         // % (0-100), valid without RCPs
    public float UpperRange;        // % (0-100), valid without RCPs
    public bool DynamicValid;       // True when RCPs running
    public bool FullRangeValid;     // True when RCPs off
    public bool UpperRangeValid;    // True when RCPs off
    public bool LevelLowAlarm;      // True when level < 90%
}
```

### Constants
| Constant | Value | Notes |
|----------|-------|-------|
| LEVEL_LOW_ALARM | 90% | Full Range alarm setpoint |
| DYNAMIC_RANGE_NO_FLOW_REFERENCE | 40% | Dynamic reads low without flow |

### Public Methods

| Method | Parameters | Returns | Notes |
|--------|------------|---------|-------|
| Initialize(rcpCount) | 0-4 | RVLISState | Initial state |
| Update(ref state, m_rcs, T_rcs, P, rcpCount) | ..., lb, °F, psia, 0-4 | void | Main update |
| Calculate(m_rcs, T_rcs, P, rcpCount) | lb, °F, psia, 0-4 | RVLISState | Combined init+update |
| GetValidLevel(state) | RVLISState | % | Currently valid reading |
| GetStatusString(state) | RVLISState | string | Display text |

### Dependencies
- WaterProperties.WaterDensity()
- PlantConstants.RCS_WATER_VOLUME

### Validation (5 tests)
1. With RCPs: Dynamic valid, Full Range invalid ✅
2. Without RCPs: Dynamic invalid, Full Range valid ✅
3. Full RCS mass → ~100% indication ✅
4. Low mass (80%) triggers alarm when RCPs off ✅
5. Dynamic reads ~40% without flow ✅

---

## CROSS-MODULE DEPENDENCY MAP

```
┌─────────────────────────────────────────────────────────────────┐
│                        PlantConstants                            │
│                              │                                   │
│         ┌────────────────────┼────────────────────┐              │
│         ▼                    ▼                    ▼              │
│  WaterProperties      ThermalExpansion      ThermalMass         │
│         │                    │                    │              │
│         └────────────┬───────┴────────────────────┘              │
│                      │                                           │
│         ┌────────────┼───────────────────────────┐               │
│         ▼            ▼                           ▼               │
│    FluidFlow    HeatTransfer                 ReactorKinetics    │
│         │            │                           │               │
│         └────────────┤                           │               │
│                      ▼                           │               │
│            PressurizerPhysics ◄──────────────────┘               │
│                      │                                           │
│         ┌────────────┤                                           │
│         ▼            ▼                                           │
│  SolidPlantPressure  CoupledThermo                               │
│         │            │                                           │
│         └────────────┼───────────────────────────┐               │
│                      ▼                           ▼               │
│              (HeatupSimEngine)            RVLISPhysics           │
└─────────────────────────────────────────────────────────────────┘
```

---

## GAP RESOLUTION STATUS

| Gap | Description | Module | Status |
|-----|-------------|--------|--------|
| **#2** | Flash evaporation | PressurizerPhysics | ✅ **RESOLVED** |
| **#3** | Spray condensation (η=85%) | PressurizerPhysics | ✅ **RESOLVED** |
| **#4** | Three-region model | PressurizerPhysics | ✅ **RESOLVED** |
| **#6** | Wall condensation | PressurizerPhysics | ✅ **RESOLVED** |
| **#7** | Rainout (bulk condensation) | PressurizerPhysics | ✅ **RESOLVED** |
| **#8** | Heater thermal dynamics (τ=20s) | PressurizerPhysics | ✅ **RESOLVED** |

---

## PHASE 2 FIXES VERIFIED

| Fix ID | Description | Module | Status |
|--------|-------------|--------|--------|
| M1 | IsSolid property added | PressurizerPhysics | ✅ |
| M2 | BubbleFormed field added | PressurizerPhysics | ✅ |
| M1 | SolidPressurizerUpdate() | PressurizerPhysics | ✅ |
| M2 | CheckBubbleFormation() | PressurizerPhysics | ✅ |
| M2 | FormBubble() | PressurizerPhysics | ✅ |
| M1 | InitializeSolidState() | PressurizerPhysics | ✅ |
| 7.1 | TwoPhaseHeatingUpdate() | PressurizerPhysics | ✅ |
| 5 | RVLIS extraction | RVLISPhysics | ✅ |

---

## VALIDATION SUMMARY

### All Modules Have Self-Validation Methods
| Module | Method | Tests |
|--------|--------|-------|
| PressurizerPhysics | ValidateCalculations() | 13 |
| PressurizerPhysics | ValidateTwoPhaseHeating() | 4 |
| PressurizerPhysics | ValidateHeaterLag() | 2 |
| SolidPlantPressure | ValidateCalculations() | 10 |
| ReactorKinetics | ValidateCalculations() | 8 |
| RVLISPhysics | ValidateCalculations() | 5 |
| **Total** | | **42** |

---

## ACTION ITEMS FOR LATER STAGES

### For Stage 2 (Parameter Audit)
1. Verify SPRAY_EFFICIENCY = 0.85 against vendor data
2. Verify HEATER_TAU = 20s against plant response tests
3. Verify CVCS PI gains against plant tuning
4. Verify relief valve parameters against setpoint documentation

### For Stage 4 (Module Integration Audit)
1. Verify HeatupSimEngine calls SolidPlantPressure.Update()
2. Verify transition from solid to two-phase PZR is handled
3. Trace CVCS controller integration with VCTPhysics

---

## NEXT STEPS

Proceed to **Sub-Stage 1D: Support Systems** to analyze:
- CVCSController.cs (24 KB) — Charging/letdown/boron control
- VCTPhysics.cs (16 KB) — Volume Control Tank physics
- RCSHeatup.cs (16 KB) — RCS heatup process control
- RCPSequencer.cs (11 KB) — RCP start/stop sequencing
- TimeAcceleration.cs (12 KB) — Time compression

---

**Document Version:** 1.0.0.0  
**Audit Status:** COMPLETE  
**Files Reviewed:** 4/4  
**Issues Found:** 0 Critical, 0 High, 0 Medium
