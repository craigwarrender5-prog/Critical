# PWR Simulator Physics Review Summary

## Project Overview

Building a Westinghouse 4-Loop PWR simulator (3411 MWt) with realistic physics. Goal is a simulator that "feels right" to an informed user with systems that fight back realistically.

**Reference Documents:**
- PWR_Master_Development_Plan_v5.docx
- Critical_Phase1_Complete.docx

---

## CRITICAL QUESTION

**Why are we testing a separate standalone set of physics in the validator and not the physics we actually intend to use?**

Currently we have:
- **Physics Engine Files** (intended for actual simulator): `WaterProperties.cs`, `ThermalExpansion.cs`, `CoupledThermo.cs`, `ReactorKinetics.cs`, `HeatTransfer.cs`, `PlantConstants.cs`
- **Validation Dashboard** (`HeatupValidationVisual.cs`): Contains its OWN duplicate physics calculations that are NOT calling the physics engine

This means:
1. We're validating physics that won't be used in the actual simulator
2. Bugs found/fixed in the validator don't fix the actual physics engine
3. The actual physics engine remains untested
4. We could pass all validation tests and still have broken physics in the real simulator

**The validator should be testing the ACTUAL physics modules, not reimplementing them.**

---

## Issues Discovered During Development

### 1. Heat Loss Bug (FIXED in validator, needs verification in physics engine)

**Problem:** Heat loss was a constant 1.5 MW regardless of temperature.

**Reality:** At cold shutdown (100°F), the plant is at thermal equilibrium with containment ambient (~80°F). Heat loss should be nearly zero. Heat loss scales with temperature difference:

```
Q_loss = k × (T_system - T_ambient)
```

Where:
- At T = 80°F (ambient): Loss = 0 MW
- At T = 100°F (cold SD): Loss = 0.06 MW
- At T = 557°F (hot): Loss = 1.5 MW

**Status:** 
- ✅ Fixed in `HeatupValidationVisual.cs` 
- ✅ Fixed in `HeatupValidation.cs`
- ✅ Added to `PlantConstants.cs`
- ✅ Added to `HeatTransfer.cs`
- ❓ NOT TESTED - validator doesn't use the physics engine!

### 2. Separate PZR vs RCS Temperatures

**Problem:** Initial model treated entire RCS as one temperature.

**Reality:** During Phase 1 heatup (PZR heaters only, no RCPs):
- PZR water heats up (at saturation temperature)
- RCS loops stay cold (only conduction through surge line)
- When RCP #1 starts, mixing occurs - temperatures equalize

**Current Implementation:**
- `T_pzr` - Pressurizer water temperature (at saturation)
- `T_rcs` - RCS loop average temperature
- `T_hot`, `T_cold` - Hot/cold leg temperatures
- `T_avg` - Mass-weighted average

### 3. Phase 0 Errors (from Master Development Plan)

These were identified as wrong in Phase 0:

| Parameter | Original (Wrong) | Corrected | Error Factor |
|-----------|------------------|-----------|--------------|
| Steam Compressibility | 3.0×10⁻⁶ /psi | 2.2×10⁻⁴ /psi | 70x too small |
| Vessel Elasticity | 3.18×10⁻⁶ /psi | 1.5×10⁻⁷ /psi | 20x too large |
| RCS Volume | 29,000 ft³ | 11,500 ft³ | 2.5x too large |
| Vessel Volume | 22,500 ft³ | 4,885 ft³ | 4.6x too large |
| HZP Temperature | 495°F | 547°F | 52°F too low |
| Water Density (Operating) | 62.4 lb/ft³ | 45.0 lb/ft³ | 28% too high |
| Water Specific Heat | 1.0 BTU/lb·°F | 1.2 BTU/lb·°F | 20% too low |

**Status:** Need to verify these are correct in the actual physics engine files.

---

## Physics Engine Files Status

### Files in /Assets/Scripts/Physics/

| File | Purpose | Validated? |
|------|---------|------------|
| `PlantConstants.cs` | Reference values (temps, pressures, volumes) | ❓ Needs review |
| `WaterProperties.cs` | Steam tables, saturation, density, enthalpy | ❓ Needs review |
| `ThermalExpansion.cs` | Volume changes with temperature | ❓ Needs review |
| `CoupledThermo.cs` | P-T-V coupling, pressurizer dynamics | ❓ Needs review |
| `ReactorKinetics.cs` | Point kinetics, delayed neutrons, decay heat | ❓ Needs review |
| `HeatTransfer.cs` | LMTD, surge enthalpy, condensation, **heat loss** | ❓ Needs review |
| `FluidFlow.cs` | RCP flow, coastdown, natural circulation | ❓ Needs review |

### Key Constants That Must Be Validated

From `PlantConstants.cs`:
```csharp
THERMAL_POWER_MWT = 3411f
RCS_WATER_VOLUME = 11500f  // ft³
RCS_METAL_MASS = 2200000f  // lb
OPERATING_PRESSURE = 2250f  // psia
T_HOT = 619f  // °F
T_COLD = 558f  // °F
T_AVG = 588.5f  // °F
PZR_TOTAL_VOLUME = 1800f  // ft³
PZR_WATER_VOLUME = 1080f  // ft³ (60%)
PZR_STEAM_VOLUME = 720f  // ft³ (40%)
HEATER_POWER_TOTAL = 1800f  // kW (1.8 MW)
RCP_HEAT_MW = 21f  // Total for all 4 RCPs
```

### Key Functions That Must Be Validated

From `WaterProperties.cs`:
- `SaturationPressure(T)` - P_sat at given temperature
- `SaturationTemperature(P)` - T_sat at given pressure  
- `WaterDensity(T, P)` - ρ at conditions
- `WaterSpecificHeat(T, P)` - Cp at conditions
- `LatentHeat(P)` - h_fg at pressure
- `ThermalExpansionCoeff(T, P)` - β at conditions

From `HeatTransfer.cs`:
- `InsulationHeatLoss_MW(T)` - **NEW** - Temperature-dependent loss
- `SurgeEnthalpyDeficit(T, P)` - Enthalpy difference for insurge
- `SprayCondensationRate(flow, T, P)` - Steam condensation from spray

From `CoupledThermo.cs`:
- `SolveEquilibrium()` - P-T-V coupled solver
- Pressurizer steam bubble dynamics
- Flash evaporation / spray condensation

---

## Validation Approach - NEEDS RESTRUCTURING

### Current (Flawed) Approach:
```
HeatupValidationVisual.cs contains:
  - Own constants (duplicated from PlantConstants)
  - Own physics calculations (duplicated/different from physics engine)
  - Own thermal expansion model
  - Own pressure calculation
  - Own heat loss calculation
  
This validates: The validator itself
This does NOT validate: The actual physics engine
```

### Correct Approach:
```
HeatupValidation.cs should:
  - Import Critical.Physics namespace
  - Call PlantConstants for all constants
  - Call WaterProperties for steam tables
  - Call ThermalExpansion for expansion
  - Call HeatTransfer for heat loss
  - Call CoupledThermo for P-T-V coupling
  
This validates: The actual physics that will be used in the simulator
```

---

## Heatup Scenario Expected Behavior

### Phase 1: PZR Heaters Only (T = 0 to 1 hr)
- Heat input: 1.8 MW (PZR heaters)
- Heat loss: ~0.06 MW (at 100°F)
- Net heat: ~1.7 MW to PZR only
- T_pzr rises rapidly toward saturation
- T_rcs stays near ambient (minimal conduction)
- Pressure rises slowly (following P-T curve)
- RCS FLOW LO annunciator lit

### Phase 2: Sequential RCP Starts (T = 1 hr onward)
- T+1.0 hr: RCP #1 starts, T_pzr and T_rcs begin mixing
- T+1.5 hr: RCP #2 starts
- T+2.0 hr: RCP #3 starts  
- T+2.5 hr: RCP #4 starts (21 MW RCP heat + 1.8 MW heaters)
- Heat loss increases as temperature rises
- Heatup rate limited to 50°F/hr (Tech Spec)

### Expected Timeline:
| Time | T_rcs | T_pzr | Pressure | Mode |
|------|-------|-------|----------|------|
| 0 hr | 100°F | ~250°F | 400 psia | 5 |
| 1 hr | ~105°F | ~400°F | 450 psia | 5 |
| 2 hr | ~180°F | ~420°F | 500 psia | 5 |
| 4 hr | ~280°F | ~350°F | 600 psia | 4 |
| 6 hr | ~380°F | ~420°F | 900 psia | 3 |
| 8 hr | ~480°F | ~520°F | 1500 psia | 3 |
| 10 hr | ~557°F | ~600°F | 2235 psia | 3 |

---

## Questions for Next Session

1. **Are the Phase 0 corrections actually implemented in the physics engine files?**
   - Steam compressibility: Should be 2.2×10⁻⁴ /psi
   - Water density at operating: Should be ~45 lb/ft³
   - Etc.

2. **Does WaterProperties.cs use proper NIST/IAPWS correlations?**
   - Or simplified approximations that may be inaccurate?

3. **Does CoupledThermo.cs properly model the pressurizer?**
   - Three-region model (subcooled water, saturated interface, steam)?
   - Flash evaporation during outsurge?
   - Spray condensation dynamics?

4. **Should we refactor the validator to use the actual physics engine?**
   - This seems critical for meaningful validation

5. **What other physics gaps exist?**
   - Review the 13 gaps from Phase 1 spec
   - Verify each is addressed in the physics engine

---

## Files to Review

All files are in `/mnt/user-data/outputs/`:

**Physics Engine:**
- `PlantConstants.cs`
- `WaterProperties.cs`
- `ThermalExpansion.cs`
- `CoupledThermo.cs`
- `ReactorKinetics.cs`
- `HeatTransfer.cs`

**Validation (needs refactoring):**
- `HeatupValidationVisual.cs`
- `HeatupValidation.cs`

**Test Runner:**
- `Phase1TestRunner.cs`
- `IntegrationTests.cs`

**Reference Documents:**
- `PWR_Master_Development_Plan_v5.docx`
- `Critical_Phase1_Complete.docx`

---

## Summary

The fundamental issue is that we're testing standalone physics in the validator instead of testing the actual physics engine. Any fixes or validations we do in the validator don't verify that the real simulator will work correctly.

**Next steps should be:**
1. Review all physics engine files for correctness
2. Verify Phase 0 corrections are implemented
3. Refactor validator to call the actual physics engine
4. Run validation against the REAL physics
5. Fix any issues found in the REAL physics engine
