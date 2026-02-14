# AUDIT: Stage 1A - Constants & Properties Foundation
**Version:** 1.0.0.0  
**Date:** 2026-02-06  
**Scope:** PlantConstants, PlantConstantsHeatup, WaterProperties, SteamThermodynamics, ThermalMass, ThermalExpansion

---

## EXECUTIVE SUMMARY

### Files Analyzed
| File | Size | Lines | Status |
|------|------|-------|--------|
| PlantConstants.cs | 49 KB | ~1100 | **GOLD STANDARD** |
| PlantConstantsHeatup.cs | 11 KB | ~280 | **DUPLICATE CONCERN** |
| WaterProperties.cs | 27 KB | ~580 | **GOLD STANDARD** |
| SteamThermodynamics.cs | 17 KB | ~420 | **GOLD STANDARD** |
| ThermalMass.cs | 16 KB | ~380 | **GOLD STANDARD** |
| ThermalExpansion.cs | 16 KB | ~360 | **GOLD STANDARD** |

### Critical Findings

| Finding | Severity | Action Required |
|---------|----------|-----------------|
| PlantConstantsHeatup duplicates constants from PlantConstants | **HIGH** | Consolidate or refactor |
| PlantConstantsHeatup has conflicting values | **HIGH** | Validate & reconcile |
| PlantConstantsHeatup.GetPZRLevelSetpoint differs from PlantConstants.GetPZRLevelProgram | **MEDIUM** | Determine authoritative source |
| PlantConstantsHeatup references WaterProperties & ThermalMass directly | **INFO** | Creates coupling |

---

## FILE 1: PlantConstants.cs (GOLD STANDARD)

### Purpose
Static reference constants for Westinghouse 4-Loop PWR (3411 MWt). All values sourced from NRC documentation and FSAR.

### Source Documentation
- NRC ML11223A342 Section 19.0 - Plant Operations
- NRC ML11223A214 Section 4.1 - CVCS
- NRC ML11223A213 Section 3.2 - RCS
- NRC IN 93-84 - RCP Seal Injection Requirements
- Westinghouse 4-Loop FSAR (South Texas, Vogtle, V.C. Summer)

### Constant Categories (130+ constants)

| Region | Count | Key Constants |
|--------|-------|---------------|
| RCS | 10 | THERMAL_POWER_MWT=3411, RCS_WATER_VOLUME=11500, T_HOT=619, T_COLD=558 |
| Pressurizer | 20+ | PZR_TOTAL_VOLUME=1800, HEATER_POWER_TOTAL=1800, HEATER_TAU=20 |
| Bubble Formation | 8 | BUBBLE_FORMATION_TEMP_F=435, SOLID_PLANT_INITIAL_PRESSURE_PSIA=365 |
| Pressure Setpoints | 10 | P_NORMAL=2250, P_PORV=2335, P_SAFETY=2485 |
| RCPs | 8 | RCP_COUNT=4, RCP_FLOW_EACH=97600, RCP_COASTDOWN_TAU=12 |
| Natural Circulation | 4 | NAT_CIRC_FLOW_MIN=12000, NAT_CIRC_FLOW_MAX=23000 |
| CVCS | 25+ | LETDOWN_NORMAL_GPM=75, CHARGING_NORMAL_GPM=87, BORON_WORTH=-9 |
| VCT | 15+ | VCT_CAPACITY_GAL=4000, VCT_LEVEL_NORMAL_LOW=40, VCT_LEVEL_NORMAL_HIGH=70 |
| Boron | 10 | BORON_COLD_SHUTDOWN_BOL_PPM=2000, BORON_CRITICAL_HZP_BOL_PPM=1500 |
| RHR | 4 | MAX_RHR_PRESSURE_PSIG=450, RHR_ENTRY_TEMP_F=350 |
| Steam Generators | 10 | SG_COUNT=4, SG_AREA_EACH=55000, LMTD_100_PERCENT=43 |
| Reactor Core | 6 | FUEL_ASSEMBLIES=193, ACTIVE_HEIGHT=12, AVG_LINEAR_HEAT=5.44 |
| Reactivity | 7 | DOPPLER_COEFF=-100, MTC_HIGH_BORON=+5, MTC_LOW_BORON=-40, BETA_DELAYED=0.0065 |
| Control Rods | 4 | ROD_BANKS=8, ROD_TOTAL_STEPS=228, SHUTDOWN_MARGIN=8000 |
| Xenon | 7 | XENON_EQUILIBRIUM_MIN=2500, XENON_PEAK_TIME_HOURS=9 |
| Decay Heat | 5 | DECAY_HEAT_TRIP=0.07, DECAY_HEAT_1HR=0.015 |
| Turbine | 4 | TURBINE_OUTPUT_MW=1150, TURBINE_EFFICIENCY=0.34 |
| Physical Constants | 5 | STEEL_CP=0.12, GRAVITY=32.174, P_ATM=14.7 |
| Unit Conversions | 9 | GPM_TO_FT3_SEC, KW_TO_BTU_SEC, MW_TO_BTU_HR |
| Surge Line | 3 | SURGE_LINE_DIAMETER=14, SURGE_LINE_LENGTH=50 |
| Insulation Heat Loss | 4 | INSULATION_LOSS_HOT_MW=1.5, HEAT_LOSS_COEFF_MW_PER_F |
| Heatup Parameters | 2 | MAX_RCS_HEATUP_RATE_F_HR=100, TYPICAL_HEATUP_RATE_F_HR=50 |
| NRC Operating Modes | 6 | MODE_3_TEMP_MIN_F=350, MODE_5_TEMP_MAX_F=200 |

### Utility Methods

| Method | Purpose | Returns |
|--------|---------|---------|
| ValidateConstants() | Self-consistency check | bool |
| PsigToPsia(float) | Unit conversion | psia |
| PsiaToPsig(float) | Unit conversion | psig |
| FahrenheitToRankine(float) | Unit conversion | °R |
| VCTLevelToVolume(float) | Level to volume | gallons |
| VCTVolumeToLevel(float) | Volume to level | % |
| CalculateHeatLoss_MW(float) | Insulation loss at temperature | MW |
| GetPlantMode(float, bool, float) | NRC mode from conditions | 1-6 |
| CanStartRCP(float, bool) | RCP start permissive | bool |
| CanOperateRHR(float, float) | RHR in-service check | bool |
| GetPZRLevelProgram(float) | PZR level setpoint from Tavg | % |
| CalculateOrificeLetdownFlow(float) | Orifice flow from pressure | gpm |
| CalculateTotalLetdownFlow(float, float, int) | Total letdown path | gpm |
| GetBoronWorth(float) | Boron worth at concentration | pcm/ppm |
| GetMTC(float) | MTC at boron concentration | pcm/°F |

### Dependencies
- None (static constants only)
- Uses System.Math for utility methods

### Dependents (Expected)
- ALL physics modules should use these constants
- WaterProperties, ThermalMass, ThermalExpansion, CoupledThermo, PressurizerPhysics, etc.

---

## FILE 2: PlantConstantsHeatup.cs (DUPLICATE CONCERN)

### Purpose
"Additional constants for heatup simulation" - supplements PlantConstants.cs

### ⚠️ CRITICAL ISSUES IDENTIFIED

#### Issue 1: Duplicate Constants with Potential Conflicts

| Constant | PlantConstants | PlantConstantsHeatup | Conflict? |
|----------|---------------|---------------------|-----------|
| RCP_HEAT_MW | 21.0 | RCP_HEAT_TOTAL_MW=21.0 | ✅ Match |
| RCP_HEAT_MW_EACH | 5.25 | RCP_HEAT_PER_PUMP_MW=5.25 | ✅ Match |
| RCS_METAL_MASS | 2,200,000 | TOTAL_PRIMARY_METAL_MASS_LB=2,200,000 | ✅ Match |
| SOLID_PLANT_INITIAL_PRESSURE_PSIA | 365 | 365 | ✅ Match |
| MAX_RCS_HEATUP_RATE_F_HR | 100 | MAX_HEATUP_RATE_F_HR=50 | ⚠️ **CONFLICT** |
| MIN_RCP_PRESSURE_PSIA | 334.7 | 350 | ⚠️ **CONFLICT** |
| MODE_6_TEMP_MAX_F | 140 | MODE_6_TEMP_LIMIT=140 | ✅ Match |
| MODE_5_TEMP_MAX_F | 200 | MODE_5_TEMP_LIMIT=200 | ✅ Match |
| MODE_4_TEMP_MAX_F | 350 | MODE_4_TEMP_LIMIT=350 | ✅ Match |
| T_AVG_NO_LOAD | 557 | MODE_3_TAVG_NORMAL=557 | ✅ Match |
| INSULATION_LOSS_HOT_MW | 1.5 | INSULATION_LOSS_MW=1.5 | ✅ Match |

**CONFLICTS REQUIRING RESOLUTION:**
1. **MAX_HEATUP_RATE**: PlantConstants=100°F/hr, PlantConstantsHeatup=50°F/hr
   - PlantConstants comment: "Source: Tech Specs - protect reactor vessel from thermal stress"
   - PlantConstantsHeatup comment: "Typical: 50-100°F/hr, conservative plants use 50°F/hr"
   - **Resolution**: 100°F/hr is the LIMIT, 50°F/hr is TYPICAL. Both may be valid for different uses.

2. **MIN_RCP_PRESSURE**: PlantConstants=334.7 psia (320 psig), PlantConstantsHeatup=350 psia
   - PlantConstants: "MIN_RCP_PRESSURE_PSIG = 320f" → 334.7 psia (correct conversion)
   - PlantConstantsHeatup: "NPSH requirements, typically 350-400 psia"
   - **Resolution**: PlantConstants is authoritative (NRC ML11223A342 Section 19.2.2)

#### Issue 2: Conflicting PZR Level Programs

**PlantConstants.GetPZRLevelProgram()**:
- Range: 557°F → 584.7°F
- Values: 25% → 61.5%
- Source: NRC HRTD Section 10.3, Figure 10.3-2

**PlantConstantsHeatup.GetPZRLevelSetpoint()**:
- Range: 200°F → 557°F
- Values: 25% → 60%
- Source: Not explicitly cited

**Analysis**: These appear to be for DIFFERENT phases:
- PlantConstants: Hot operating conditions (557-585°F) - Mode 1/2
- PlantConstantsHeatup: Cold-to-hot heatup (200-557°F) - Mode 4/5 to Mode 3

**Recommendation**: Document this distinction clearly or consolidate into single function.

#### Issue 3: Additional Constants in PlantConstantsHeatup

| Constant | Value | Notes |
|----------|-------|-------|
| RCP_ELECTRICAL_PER_PUMP_MW | 6.0 | Motor nameplate |
| RCP_ELECTRICAL_TOTAL_MW | 24.0 | Not in PlantConstants |
| AUX_ELECTRICAL_LOADS_MW | 15.0 | Not in PlantConstants |
| SUPPORT_SYSTEMS_MW | 10.0 | Not in PlantConstants |
| TOTAL_GRID_LOAD_HEATUP_MW | 51.0 | Not in PlantConstants |
| EDG_CAPACITY_MW | 7.0 | Not in PlantConstants |
| RV_MASS_LB | 800,000 | Component breakdown |
| SG_MASS_EACH_LB | 700,000 | Component breakdown |
| RCS_PIPING_MASS_LB | 400,000 | Component breakdown |
| SG_SECONDARY_METAL_MASS_LB | 200,000 | Secondary side |
| SG_SECONDARY_WATER_MASS_LB | 415,000 | Secondary side |
| MAX_COOLDOWN_RATE_F_HR | 100 | Cooldown limit |
| MIN_SUBCOOLING_F | 30 | Safety margin |
| TARGET_SUBCOOLING_F | 50 | Operating target |
| LETDOWN_HEAT_REMOVAL_MW | 2.0 | Not in PlantConstants |
| NORMAL_OPERATING_PRESSURE | 2235 | Same as P_NORMAL_PSIG (but units?) |

**Issue with NORMAL_OPERATING_PRESSURE**: Value is 2235 but comment says "psia". This should be 2250 psia (2235 psig). Potential bug.

### Utility Methods

| Method | Purpose | Conflicts? |
|--------|---------|-----------|
| GetPZRLevelSetpoint(float) | Level program 200-557°F | Different range than PlantConstants |
| GetTargetPressure(float) | P-T heatup curve | Calls WaterProperties |
| GetPlantMode(float) | Plant mode from Tavg | Simplified vs PlantConstants version |
| GetTotalHeatCapacity(float, float) | Total RCS heat capacity | Calls WaterProperties, ThermalMass |
| ValidateConstants() | Consistency check | Checks against PlantConstants |

### Dependencies
- **WaterProperties** - for saturation pressure in GetTargetPressure()
- **ThermalMass** - for CP_STEEL in GetTotalHeatCapacity()
- **PlantConstants** - for validation in ValidateConstants()

### ⚠️ RECOMMENDATION

**Option A (Preferred)**: Merge PlantConstantsHeatup into PlantConstants
- Add missing constants to PlantConstants with clear documentation
- Remove duplicate definitions
- Consolidate utility methods

**Option B**: Keep separate but document relationship
- PlantConstants = authoritative reference values
- PlantConstantsHeatup = derived/operational values for heatup scenarios
- Fix conflicts to use PlantConstants as source

---

## FILE 3: WaterProperties.cs (GOLD STANDARD)

### Purpose
NIST Steam Table validated thermodynamic properties for water and steam.

### Source Documentation
- NIST Chemistry WebBook (webbook.nist.gov)
- E.W. Lemmon, M.O. McLinden and D.G. Friend, "Thermophysical Properties of Fluid Systems"

### Implements
- Gap #11 - Water property accuracy
- Gap #12 - Steam property accuracy

### Validated Ranges
- Pressure: 1 - 3000 psia (extended for cold shutdown/LOCA)
- Temperature: 32 - 700°F

### Public Methods

| Method | Parameters | Returns | Validated? |
|--------|------------|---------|-----------|
| **Saturation Properties** ||||
| SaturationTemperature(P) | psia | °F | ✅ ±1°F |
| SaturationPressure(T) | °F | psia | ✅ ±1% |
| LatentHeat(P) | psia | BTU/lb | ✅ ±5% |
| **Liquid Properties** ||||
| WaterDensity(T, P) | °F, psia | lb/ft³ | ✅ |
| WaterEnthalpy(T, P) | °F, psia | BTU/lb | ✅ ±0.5% |
| SaturatedLiquidEnthalpy(P) | psia | BTU/lb | ✅ |
| WaterSpecificHeat(T, P) | °F, psia | BTU/(lb·°F) | ✅ |
| SubcoolingMargin(T, P) | °F, psia | °F | ✅ |
| IsSubcooled(T, P) | °F, psia | bool | ✅ |
| **Steam Properties** ||||
| SteamDensity(T, P) | °F, psia | lb/ft³ | ✅ |
| SaturatedSteamDensity(P) | psia | lb/ft³ | ✅ |
| SteamEnthalpy(T, P) | °F, psia | BTU/lb | ✅ |
| SaturatedSteamEnthalpy(P) | psia | BTU/lb | ✅ |
| SteamSpecificHeat(T, P) | °F, psia | BTU/(lb·°F) | ✅ |
| **Gap #10 - Enthalpy Deficit** ||||
| SurgeEnthalpyDeficit(T_surge, P) | °F, psia | BTU/lb | ✅ 40-80 |
| **Transport Properties** ||||
| ThermalConductivity(T) | °F | BTU/(hr·ft·°F) | ✅ |
| DynamicViscosity(T) | °F | lb/(ft·hr) | ✅ |
| KinematicViscosity(T, P) | °F, psia | ft²/hr | ✅ |
| PrandtlNumber(T, P) | °F, psia | dimensionless | ✅ |
| ThermalExpansionCoeff(T, P) | °F, psia | 1/°F | ✅ |
| ThermalDiffusivity(T, P) | °F, psia | ft²/hr | ✅ |
| **Validation** ||||
| ValidateAgainstNIST() | - | bool | ✅ |

### Polynomial Fit Ranges

| Property | Range 1 | Range 2 | Range 3 | Range 4 |
|----------|---------|---------|---------|---------|
| Tsat(P) | 1-14.7 psia | 14.7-100 | 100-1000 | 1000-3200 |
| Psat(T) | 100-212°F | 212-400 | 400-550 | 550-700 |
| hfg(P) | 14.7-1000 psia | 1000-3200 | - | - |

### Dependencies
- PlantConstants.RANKINE_OFFSET (459.67)

### Dependents
- SteamThermodynamics
- ThermalExpansion
- PlantConstantsHeatup (GetTargetPressure, GetTotalHeatCapacity)
- CoupledThermo (expected)
- PressurizerPhysics (expected)
- HeatTransfer (expected)

### Validation Points (from ValidateAgainstNIST)
1. Tsat(14.7 psia) = 212°F ✅
2. Tsat(2250 psia) = 653°F ✅
3. Psat(653°F) ≈ 2250 psia ✅
4. hfg(2250 psia) ≈ 465 BTU/lb ✅
5. ρ(588°F, 2250 psia) ≈ 46 lb/ft³ ✅
6. Subcooling(619°F, 2250 psia) ≈ 34°F ✅
7. SurgeEnthalpyDeficit ≈ 50-70 BTU/lb ✅

---

## FILE 4: SteamThermodynamics.cs (GOLD STANDARD)

### Purpose
Two-phase thermodynamic calculations for steam-water mixtures.

### Implements
- Gap #13 - Steam quality/void fraction calculations

### Public Methods

| Method | Parameters | Returns | Notes |
|--------|------------|---------|-------|
| **Steam Quality** ||||
| SteamQuality(h, P) | BTU/lb, psia | 0-1 | x = (h-hf)/hfg |
| TwoPhaseEnthalpy(x, P) | 0-1, psia | BTU/lb | h = hf + x·hfg |
| TwoPhaseDensity(x, P) | 0-1, psia | lb/ft³ | Homogeneous model |
| **Void Fraction** ||||
| VoidFraction(x, P) | 0-1, psia | 0-1 | Chisholm slip ratio |
| VoidFractionHomogeneous(x, P) | 0-1, psia | 0-1 | S=1 model |
| QualityFromVoidFraction(α, P) | 0-1, psia | 0-1 | Iterative inverse |
| **Two-Phase Properties** ||||
| TwoPhaseSpecificVolume(x, P) | 0-1, psia | ft³/lb | 1/ρ |
| TwoPhaseInternalEnergy(x, P) | 0-1, psia | BTU/lb | u = h - Pv |
| DeterminePhase(T, P) | °F, psia | PhaseState | Enum |
| DeterminePhaseFromEnthalpy(h, P) | BTU/lb, psia | PhaseState | Enum |
| **Pressurizer-Specific** ||||
| PressurizerMasses(V_w, V_s, P, out m_w, out m_s) | ft³, ft³, psia | lb, lb | Mass from volumes |
| PressurizerLevel(m_w, m_s, P, V_total) | lb, lb, psia, ft³ | 0-1 | Level from masses |
| SteamVolumeFromLevel(%, V_total) | %, ft³ | ft³ | |
| WaterVolumeFromLevel(%, V_total) | %, ft³ | ft³ | |
| **Phase Change** ||||
| EvaporationEnergy(m, P) | lb, psia | BTU | m·hfg |
| CondensationEnergy(m, P) | lb, psia | BTU | m·hfg |
| MassFromPhaseChangeEnergy(Q, P) | BTU, psia | lb | Q/hfg |
| **Validation** ||||
| ValidateCalculations() | - | bool | |

### PhaseState Enum
```csharp
public enum PhaseState
{
    SubcooledLiquid,
    TwoPhase,
    SuperheatedSteam
}
```

### Dependencies
- WaterProperties (all saturation and density calls)

### Dependents
- PressurizerPhysics (expected)
- CoupledThermo (expected)

---

## FILE 5: ThermalMass.cs (GOLD STANDARD)

### Purpose
Heat capacity calculations for metal structures and fluid masses.
Fundamental: Q = m × Cp × ΔT

### Material Constants

| Material | Constant | Value (BTU/(lb·°F)) |
|----------|----------|---------------------|
| Carbon Steel | CP_STEEL | 0.12 |
| Stainless Steel | CP_STAINLESS | 0.12 |
| Inconel | CP_INCONEL | 0.11 |
| Zircaloy | CP_ZIRCALOY | 0.07 |
| UO2 Fuel | CP_UO2 | 0.06 |

### MaterialType Enum
```csharp
public enum MaterialType
{
    CarbonSteel,
    StainlessSteel,
    Inconel,
    Zircaloy,
    UO2
}
```

### Public Methods

| Method | Purpose | Returns |
|--------|---------|---------|
| **Heat Capacity** |||
| MetalHeatCapacity(m, material) | Metal heat capacity | BTU/°F |
| MetalHeatCapacity(m, Cp) | With explicit Cp | BTU/°F |
| FluidHeatCapacity(m, T, P) | Water heat capacity | BTU/°F |
| RCSHeatCapacity(m_metal, m_water, T, P) | Combined RCS | BTU/°F |
| PressurizerWallHeatCapacity() | 200,000 lb steel | BTU/°F |
| **Temperature Change** |||
| TemperatureChange(Q, m, Cp) | ΔT from heat | °F |
| WaterTemperatureChange(Q, m, T, P) | Water ΔT | °F |
| MetalTemperatureChange(Q, m, material) | Metal ΔT | °F |
| **Heat Required** |||
| HeatRequired(m, Cp, ΔT) | Q for ΔT | BTU |
| WaterHeatRequired(m, T, P, ΔT) | Water Q | BTU |
| MetalHeatRequired(m, material, ΔT) | Metal Q | BTU |
| **Heat Rate** |||
| TemperatureRate(P_BTU/s, m, Cp) | dT/dt | °F/sec |
| PowerRequired(m, Cp, dT/dt) | Power for rate | BTU/sec |
| **Equilibrium** |||
| EquilibriumTemperature(...) | Final T for mixing | °F |
| HeatTransferToEquilibrium(...) | Q transferred | BTU |
| **Time Constants** |||
| ThermalTimeConstant(m, Cp, h, A) | τ = mCp/(hA) | hours |
| FirstOrderResponse(T, T_target, τ, dt) | Lag response | °F |
| **Helpers** |||
| GetSpecificHeat(material) | Cp lookup | BTU/(lb·°F) |
| ValidateCalculations() | Self-test | bool |

### Dependencies
- WaterProperties.WaterSpecificHeat()
- PlantConstants.PZR_WALL_MASS

### Dependents
- PlantConstantsHeatup.GetTotalHeatCapacity()
- HeatTransfer (expected)
- CoupledThermo (expected)

---

## FILE 6: ThermalExpansion.cs (GOLD STANDARD)

### Purpose
Thermal expansion and volume change calculations.
Note: For realistic pressure response, use CoupledThermo (Gap #1).

### Public Methods

| Method | Purpose | Returns |
|--------|---------|---------|
| **Coefficients** |||
| ExpansionCoefficient(T, P) | β in 1/°F | ~4-6e-4 at PWR conditions |
| Compressibility(T, P) | κ in 1/psi | ~5-10e-6 at PWR conditions |
| **Volume Change** |||
| VolumeChangeFromTemp(V, T, ΔT, P) | ΔV from ΔT | ft³ |
| VolumeChangeFromPressure(V, T, ΔP, P) | ΔV from ΔP | ft³ |
| NewVolume(V, T, ΔT, ΔP, P) | V after changes | ft³ |
| **Surge Volume** |||
| UncoupledSurgeVolume(V_RCS, ΔT, T, P) | Free expansion | ft³ |
| CoupledSurgeVolume(V_RCS, V_PZR_steam, ΔT, T, P) | With PZR cushion | ft³ |
| ExpansionRate(Q_BTU/s, V, m, T, P) | dV/dt from power | ft³/sec |
| **Pressure Response** |||
| PressureChangeFromTemp(ΔT, T, P) | ΔP from ΔT | psi |
| PressureCoefficient(T, P) | dP/dT | psi/°F |
| TempChangeFromPressure(ΔP, T, P) | ΔT from ΔP | °F |
| **Density-Based** |||
| MassChangeForConstantPressure(V, T1, T2, P) | Δm needed | lb |
| VolumeAtTemperature(m, T, P) | V at new T | ft³ |
| **Validation** |||
| ValidateCalculations() | Self-test | bool |

### Key Design Decision: SYSTEM_DAMPING = 0.18

In PressureCoefficient():
```csharp
// Apply system damping factor to account for pressurizer effect
// In a real PWR, the steam cushion reduces effective dP/dT
// This gives realistic 5-10 psi/°F instead of pure β/κ ≈ 40 psi/°F
const float SYSTEM_DAMPING = 0.18f;
```

This empirical factor accounts for:
- Pressurizer steam cushion absorbing pressure changes
- Mass exchange between RCS and PZR
- Non-ideal behavior near saturation

### Dependencies
- WaterProperties (saturation, density, specific heat)
- PlantConstants.RANKINE_OFFSET (indirect via WaterProperties)

### Dependents
- CoupledThermo (expected - this is the simplified version)

---

## CROSS-MODULE DEPENDENCY MAP

```
┌─────────────────────────────────────────────────────────────┐
│                    PlantConstants                           │
│                   (AUTHORITATIVE)                           │
│                        │                                    │
│     ┌──────────────────┼──────────────────┐                │
│     │                  │                  │                │
│     ▼                  ▼                  ▼                │
│ WaterProperties   ThermalMass    PlantConstantsHeatup      │
│     │                  │               (SHOULD USE)        │
│     │                  │                  │                │
│     ├──────────────────┼──────────────────┤                │
│     │                  │                  │                │
│     ▼                  ▼                  ▼                │
│ SteamThermo    ThermalExpansion    (HeatupSimEngine)       │
│     │                  │                                   │
│     └────────┬─────────┘                                   │
│              │                                             │
│              ▼                                             │
│        CoupledThermo                                       │
│        (Gap #1 - P-T-V)                                    │
└─────────────────────────────────────────────────────────────┘
```

---

## VALIDATION STATUS

### Self-Validation Methods Present
| File | Method | Tests |
|------|--------|-------|
| PlantConstants | ValidateConstants() | 10+ consistency checks |
| WaterProperties | ValidateAgainstNIST() | 7 NIST data points |
| SteamThermodynamics | ValidateCalculations() | 6 property checks |
| ThermalMass | ValidateCalculations() | 5 calculation checks |
| ThermalExpansion | ValidateCalculations() | 6 expansion checks |
| PlantConstantsHeatup | ValidateConstants() | 4 consistency checks |

---

## ACTION ITEMS FOR STAGE 2 (Parameter Audit)

### HIGH PRIORITY
1. **Resolve PlantConstantsHeatup conflicts** - especially MAX_HEATUP_RATE and MIN_RCP_PRESSURE
2. **Verify all PlantConstants values** against NRC source documents line-by-line
3. **Check NORMAL_OPERATING_PRESSURE** in PlantConstantsHeatup (2235 vs 2250)

### MEDIUM PRIORITY
4. **Document PZR level program distinction** between PlantConstants and PlantConstantsHeatup
5. **Verify WaterProperties polynomial coefficients** against NIST source data
6. **Validate SYSTEM_DAMPING = 0.18** in ThermalExpansion against EPRI data

### LOW PRIORITY
7. **Consider consolidating** PlantConstantsHeatup into PlantConstants
8. **Add missing constants** (electrical loads, component masses) to PlantConstants

---

## NEXT STEPS

Proceed to **Sub-Stage 1B: Heat & Flow Physics** to analyze:
- HeatTransfer.cs
- FluidFlow.cs
- LoopThermodynamics.cs
- CoupledThermo.cs (CRITICAL - Gap #1)

---

**Document Version:** 1.0.0.0  
**Audit Status:** COMPLETE  
**Files Reviewed:** 6/6  
**Issues Found:** 3 HIGH, 2 MEDIUM, 2 LOW
