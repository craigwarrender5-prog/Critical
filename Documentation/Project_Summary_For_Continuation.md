# Critical: Master the Atom - PWR Simulator Project Summary

## PROJECT OVERVIEW

**Project:** Critical: Master the Atom - Westinghouse 4-Loop PWR Simulator  
**Platform:** Unity 6.3  
**Reference Plant:** 3411 MWt Westinghouse 4-Loop PWR  
**Development Approach:** Physics-first, then reactor with auto auxiliaries, then progressive manual conversion

---

## PHASE 1 - CORE PHYSICS ENGINE (COMPLETE DOCUMENT CREATED)

### Duration: 3-4 weeks | Tests: 112

### Critical Gap Analysis Findings (13 Gaps Identified)

The original Phase 1 design had critical physics deficiencies that were identified and corrected:

| Gap | Issue | Severity | Solution Module |
|-----|-------|----------|-----------------|
| 1 | P-T-V coupling missing (expansion affects pressure) | CRITICAL | CoupledThermo |
| 2 | Flash evaporation during outsurge not modeled | CRITICAL | PressurizerPhysics |
| 3 | Spray condensation dynamics missing | CRITICAL | PressurizerPhysics |
| 4 | Non-equilibrium three-region model needed | CRITICAL | PressurizerPhysics |
| 5 | Westinghouse reference values missing | CRITICAL | PlantConstants |
| 6 | Wall condensation not modeled | MAJOR | PressurizerPhysics |
| 7 | Rainout (bulk condensation) missing | MAJOR | PressurizerPhysics |
| 8 | Heater thermal dynamics missing | MAJOR | PressurizerPhysics |
| 9 | Surge line hydraulics missing | MAJOR | FluidFlow |
| 10 | Enthalpy transport not tracked | MAJOR | HeatTransfer |
| 11 | Steam table accuracy near saturation | MINOR | WaterProperties |
| 12 | Missing liquid/steam enthalpy functions | MINOR | WaterProperties |
| 13 | Steam quality/void fraction at interface | MINOR | SteamThermo |

### Phase 1 Module Structure (10 Modules)

```
Critical/Assets/Scripts/Physics/
├── PlantConstants.cs        (5 tests)   - Westinghouse 4-loop reference values
├── WaterProperties.cs       (18 tests)  - Water/steam thermodynamic properties
├── SteamThermodynamics.cs   (10 tests)  - Steam space behavior
├── ThermalMass.cs           (6 tests)   - Heat capacity calculations
├── ThermalExpansion.cs      (6 tests)   - Volume change with temperature
├── CoupledThermo.cs         (12 tests)  - P-T-V iterative solver [GAP #1]
├── PressurizerPhysics.cs    (24 tests)  - Flash, spray, wall, rainout [GAPS #2-8]
├── ReactorKinetics.cs       (10 tests)  - Neutronics and feedback
├── FluidFlow.cs             (8 tests)   - Pumps, surge line, nat circ [GAP #9]
├── HeatTransfer.cs          (6 tests)   - Enthalpy, heat transfer [GAP #10]
└── IntegrationTests.cs      (7 tests)   - End-to-end validation
```

### Key Westinghouse 4-Loop Reference Parameters

**Reactor Coolant System:**
- Thermal Power: 3411 MWt
- RCS Water Volume: 11,500 ft³
- RCS Metal Mass: 2,200,000 lb
- Operating Pressure: 2250 psia
- Thot: 619°F, Tcold: 558°F, Tavg: 588.5°F
- Core ΔT: 61°F
- Total RCS Flow: 390,400 gpm

**Pressurizer:**
- Total Volume: 1800 ft³
- Water Volume (60%): 1080 ft³
- Steam Volume (40%): 720 ft³
- Height: 52.75 ft
- Wall Mass: 200,000 lb
- Heater Power: 1800 kW (500 kW proportional + 1300 kW backup)
- Heater Time Constant: ~20 seconds
- Max Spray Flow: 900 gpm
- Spray Temperature: 558°F (= Tcold)

**Pressure Setpoints (psig):**
- Normal: 2235
- Heaters Full ON: <2210
- Spray ON: >2260
- PORV: 2335
- Safety Valves: 2485
- High Trip: 2385, Low Trip: 1885

### Critical Physics Insight: Coupled Thermodynamics

The RCS is a CLOSED system. When temperature rises:
1. Water wants to expand
2. But total volume is FIXED
3. Expansion compresses PZR steam → pressure RISES
4. Higher pressure → water slightly denser → expansion REDUCED
5. Must iterate until pressure and volume are consistent

**Key Test:** 10°F Tavg rise → 60-80 psi pressure increase (not 0 as simple model would predict)

### Pressurizer Non-Equilibrium Physics

Five phenomena must be modeled:
1. **Flash Evaporation** - During outsurge/depressurization, retards pressure drop
2. **Spray Condensation** - Steam→water, reduces pressure rise
3. **Wall Condensation** - Steam condenses on cooler upper walls
4. **Rainout** - Bulk condensation when steam subcools
5. **Heater Thermal Lag** - ~20 second time constant

---

## PHASE 2 - REACTOR CORE (DOCUMENT CREATED)

### Duration: 3-4 weeks | Tests: 85

**Components:**
- FuelAssembly (15 tests) - 193 assemblies, 264 rods each
- ControlRodBank (12 tests) - 8 banks, rod worth curves
- CoreCoolant (10 tests) - 4-loop flow model
- ReactorCore (18 tests) - Integration
- CoreController (8 tests) - First MonoBehaviour
- PowerCalculator (6 tests)
- FeedbackCalculator (8 tests)
- Integration (8 tests)

**Key Reference Values:**
- 193 fuel assemblies, 264 rods each = 50,952 total
- Control rod banks: D, C, B, A (control) + SA, SB, SC, SD (shutdown)
- Rod speed: 72 steps/minute, 228 steps total
- Doppler: -2.5 pcm/√°R
- MTC: +5 pcm/°F (high boron) to -40 pcm/°F (low boron)
- Boron worth: -9 pcm/ppm
- β (delayed neutron fraction): 0.0065

---

## PHASES 3-6 NEED UPDATED DOCUMENTS

### Phase 3 - Pressurizer System (78 tests expected)
**Uses Phase 1 modules:** PressurizerPhysics, CoupledThermo, HeatTransfer, FluidFlow
- Pressurizer vessel with three-region model
- Heaters (proportional + backup with thermal lag)
- Spray system with condensation model
- Surge line hydraulics
- PORV and safety valves
- Pressure controller (AUTO mode)

### Phase 4 - CVCS & RCPs (65 tests expected)
**Uses Phase 1 modules:** FluidFlow, WaterProperties
- Charging/Letdown flow control
- Boron dilution/boration
- Volume Control Tank
- 4 Reactor Coolant Pumps
- Pump coastdown model
- Natural circulation transition

### Phase 5 - Steam Generators (72 tests expected)
**Uses Phase 1 modules:** WaterProperties, ThermalMass, HeatTransfer
- 4 U-tube steam generators
- Primary-to-secondary heat transfer
- Secondary side level control
- Steam pressure dynamics
- Feedwater control
- Main Steam Isolation Valves

### Phase 6 - Turbine/Generator (68 tests expected)
- High/Low pressure turbines
- Moisture separator reheaters
- Generator and grid connection
- Turbine control valves
- Load following capability
- Complete plant integration

---

## WHAT WAS COMPLETED THIS SESSION

1. ✅ **Critical_Phase1_Complete.docx** - Full Phase 1 document with:
   - All 13 gap analysis fixes integrated
   - 10 modules, 112 tests
   - Complete Westinghouse reference parameters
   - Coupled thermodynamics (P-T-V solver)
   - Pressurizer physics (flash, spray, wall, rainout, heaters)
   - Validation matrices for all modules
   - Exit gate criteria

2. ✅ **Critical_Phase2_Complete.docx** - Full Phase 2 document with:
   - 8 components, 85 tests
   - Reactor core model specifications
   - Control rod configuration
   - Dependencies on Phase 1 modules
   - Exit gate criteria

---

## WHAT STILL NEEDS TO BE DONE

1. **Phase 3 Document** - Pressurizer System
   - Must show how Phase 1 PressurizerPhysics module is used
   - Include flash evaporation, spray condensation, enthalpy transport
   - Heater thermal lag implementation
   - PORV/safety valve logic

2. **Phase 4 Document** - CVCS & RCPs
   - Boron control integration with ReactorKinetics
   - RCP coastdown using FluidFlow module
   - Natural circulation transition

3. **Phase 5 Document** - Steam Generators
   - Heat transfer model
   - Secondary side dynamics
   - Integration with RCS

4. **Phase 6 Document** - Turbine/Generator
   - Complete plant integration
   - Load following
   - Full system coupling

---

## KEY FILES IN /mnt/user-data/outputs/

**New Complete Documents:**
- Critical_Phase1_Complete.docx (16.5 KB) - Phase 1 with all gaps fixed
- Critical_Phase2_Complete.docx (14.2 KB) - Phase 2 reactor core

**Previous Documents (may need updating):**
- Critical_Phase1_Gap_Analysis.docx
- Critical_Phase1_Addendum_CoupledThermo.docx
- Critical_Phase3_Pressurizer.docx through Phase6

---

## CONTINUATION INSTRUCTIONS

In the new chat, say:

"Continue the Critical: Master the Atom PWR simulator project. I need complete Phase 3-6 implementation documents that:
1. Reference the Phase 1 physics modules (especially PressurizerPhysics, CoupledThermo, FluidFlow, HeatTransfer)
2. Include test validation matrices
3. Include exit gate criteria
4. Follow the same format as Phase 1 and Phase 2 complete documents

The Phase 1 document addresses 13 physics gaps including coupled P-T-V thermodynamics and non-equilibrium pressurizer physics. Phase 3 (Pressurizer) should show how these physics modules are integrated into the game system."

---

## TECHNICAL NOTES

- All code is pure C# in Phase 1 (no MonoBehaviours)
- Phase 2 introduces first MonoBehaviour (CoreController)
- Units: °F, psia, ft³, lb, BTU throughout
- Westinghouse 4-loop is the reference plant
- Physics validated against NIST Steam Tables and NRC documentation
