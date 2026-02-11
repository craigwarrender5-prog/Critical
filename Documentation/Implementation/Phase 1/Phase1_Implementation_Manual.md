# CRITICAL: Master the Atom
# Phase 1 Implementation and Assembly Manual

**Document Version:** 1.0  
**Date:** February 4, 2026  
**Reference Plant:** Westinghouse 4-Loop PWR, 3411 MWt  
**Target Platform:** Unity 2022+ / .NET Standard 2.1

---

# Table of Contents

1. [Introduction](#1-introduction)
2. [System Requirements](#2-system-requirements)
3. [Project Structure](#3-project-structure)
4. [Module Dependencies](#4-module-dependencies)
5. [Installation Guide](#5-installation-guide)
6. [Module Reference](#6-module-reference)
   - 6.1 [PlantConstants](#61-plantconstantscs)
   - 6.2 [WaterProperties](#62-waterpropertiescs)
   - 6.3 [SteamThermodynamics](#63-steamthermodynamicscs)
   - 6.4 [ThermalMass](#64-thermalmasscs)
   - 6.5 [ThermalExpansion](#65-thermalexpansioncs)
   - 6.6 [HeatTransfer](#66-heattransfercs)
   - 6.7 [FluidFlow](#67-fluidflowcs)
   - 6.8 [ReactorKinetics](#68-reactorkineticscs)
   - 6.9 [PressurizerPhysics](#69-pressurizerphysicscs)
   - 6.10 [CoupledThermo](#610-coupledthermocs)
7. [Test Framework](#7-test-framework)
8. [Validation Procedures](#8-validation-procedures)
9. [Physics Gap Resolution](#9-physics-gap-resolution)
10. [Troubleshooting](#10-troubleshooting)
11. [API Quick Reference](#11-api-quick-reference)

---

# 1. Introduction

## 1.1 Purpose

This manual provides complete instructions for implementing and assembling Phase 1 of the CRITICAL nuclear power plant simulator. Phase 1 establishes the foundational physics library upon which ALL subsequent phases depend.

## 1.2 Scope

Phase 1 implements:
- **10 Physics Modules** (~4,500 lines of C# code)
- **112 Unit Tests** covering all physics calculations
- **7 Integration Tests** validating cross-module behavior

## 1.3 Critical Success Criteria

The single most important validation criterion is:

> **A 10°F average RCS temperature rise must produce a 60-80 psi pressure increase.**

If this test fails, the entire simulation will produce unrealistic results. This was identified as "Gap #1" in the physics analysis and is resolved by the `CoupledThermo.cs` iterative solver.

## 1.4 Reference Documentation

- Westinghouse 4-Loop PWR FSAR (Final Safety Analysis Report)
- NIST Steam Tables (IAPWS-IF97)
- EPRI NP-2923 (PWR Thermal-Hydraulics)
- ANS 5.1-2005 (Decay Heat Standard)

---

# 2. System Requirements

## 2.1 Development Environment

| Requirement | Specification |
|-------------|---------------|
| Unity Version | 2022.3 LTS or newer |
| .NET Standard | 2.1 |
| C# Version | 9.0+ |
| IDE | Visual Studio 2022 / Rider / VS Code |

## 2.2 Hardware Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| CPU | 4 cores | 8+ cores |
| RAM | 8 GB | 16+ GB |
| Storage | 1 GB | 5 GB |

## 2.3 Dependencies

The Phase 1 physics library has **NO external dependencies** except:
- `System` namespace (standard .NET)
- `UnityEngine.Mathf` (optional - can substitute `System.Math`)

---

# 3. Project Structure

```
Critical/
└── Assets/
    └── Scripts/
        ├── Physics/                    # Core physics modules
        │   ├── PlantConstants.cs       # Reference plant parameters
        │   ├── WaterProperties.cs      # Steam table lookups
        │   ├── SteamThermodynamics.cs  # Two-phase calculations
        │   ├── ThermalMass.cs          # Heat capacity
        │   ├── ThermalExpansion.cs     # Volume changes
        │   ├── HeatTransfer.cs         # Heat exchangers
        │   ├── FluidFlow.cs            # Pumps and flow
        │   ├── ReactorKinetics.cs      # Neutronics
        │   ├── PressurizerPhysics.cs   # Pressurizer model
        │   └── CoupledThermo.cs        # P-T-V solver (CRITICAL)
        │
        └── Tests/                      # Test framework
            ├── IntegrationTests.cs     # Cross-module tests
            └── Phase1TestRunner.cs     # Test runner
```

## 3.1 Namespace Convention

All physics modules use the namespace:
```csharp
namespace Critical.Physics
```

Test modules use:
```csharp
namespace Critical.Physics.Tests
namespace Critical.Tests
```

---

# 4. Module Dependencies

## 4.1 Dependency Graph

```
                    PlantConstants
                          │
            ┌─────────────┼─────────────┐
            │             │             │
            ▼             ▼             ▼
    WaterProperties   ThermalMass   ReactorKinetics
            │             │
            ▼             │
  SteamThermodynamics     │
            │             │
            ├─────────────┤
            ▼             ▼
    ThermalExpansion  HeatTransfer
            │             │
            └──────┬──────┘
                   ▼
              FluidFlow
                   │
                   ▼
         PressurizerPhysics
                   │
                   ▼
            CoupledThermo ◄── DEPENDS ON ALL ABOVE
```

## 4.2 Compilation Order

Files MUST be compiled in this order to resolve dependencies:

1. `PlantConstants.cs` - No dependencies
2. `WaterProperties.cs` - Depends on PlantConstants
3. `SteamThermodynamics.cs` - Depends on WaterProperties
4. `ThermalMass.cs` - Depends on PlantConstants
5. `ThermalExpansion.cs` - Depends on WaterProperties
6. `HeatTransfer.cs` - Depends on WaterProperties, PlantConstants
7. `FluidFlow.cs` - Depends on WaterProperties, PlantConstants
8. `ReactorKinetics.cs` - Depends on PlantConstants
9. `PressurizerPhysics.cs` - Depends on ALL above
10. `CoupledThermo.cs` - Depends on ALL above

---

# 5. Installation Guide

## 5.1 Unity Project Setup

### Step 1: Create Project Structure

```
1. Open Unity Hub
2. Create new 3D project named "Critical"
3. Navigate to Assets folder
4. Create folder structure:
   Assets/
   └── Scripts/
       ├── Physics/
       └── Tests/
```

### Step 2: Import Physics Modules

Copy the 10 .cs files from `Physics/` folder into `Assets/Scripts/Physics/`:

```
PlantConstants.cs
WaterProperties.cs
SteamThermodynamics.cs
ThermalMass.cs
ThermalExpansion.cs
HeatTransfer.cs
FluidFlow.cs
ReactorKinetics.cs
PressurizerPhysics.cs
CoupledThermo.cs
```

### Step 3: Import Test Modules

Copy the 2 .cs files from `Tests/` folder into `Assets/Scripts/Tests/`:

```
IntegrationTests.cs
Phase1TestRunner.cs
```

### Step 4: Verify Compilation

1. Return to Unity Editor
2. Wait for script compilation
3. Check Console for errors
4. All scripts should compile without errors

## 5.2 Standalone .NET Setup (Without Unity)

### Step 1: Create Solution

```bash
dotnet new sln -n Critical
dotnet new classlib -n Critical.Physics
dotnet new console -n Critical.Tests
dotnet sln add Critical.Physics
dotnet sln add Critical.Tests
dotnet add Critical.Tests reference Critical.Physics
```

### Step 2: Copy Source Files

```bash
cp Physics/*.cs Critical.Physics/
cp Tests/*.cs Critical.Tests/
```

### Step 3: Update for Pure .NET

Replace any `Mathf` references with `Math`:

```csharp
// Change:
using UnityEngine;
float result = Mathf.Sqrt(x);

// To:
using System;
float result = (float)Math.Sqrt(x);
```

### Step 4: Build and Run

```bash
dotnet build
dotnet run --project Critical.Tests
```

---

# 6. Module Reference

## 6.1 PlantConstants.cs

### Purpose
Defines all Westinghouse 4-Loop PWR reference parameters from the FSAR.

### Key Constants

| Category | Constant | Value | Unit |
|----------|----------|-------|------|
| **Thermal Power** | THERMAL_POWER_MWT | 3411 | MWt |
| **RCS Temperatures** | T_HOT | 619 | °F |
| | T_COLD | 558 | °F |
| | T_AVG | 588.5 | °F |
| **Pressure** | OPERATING_PRESSURE | 2250 | psia |
| **Flow** | RCS_FLOW_TOTAL | 390,400 | gpm |
| **Pressurizer** | PZR_TOTAL_VOLUME | 1800 | ft³ |
| | PZR_WATER_VOLUME | 1080 | ft³ |
| | PZR_STEAM_VOLUME | 720 | ft³ |
| | HEATER_POWER_TOTAL | 1800 | kW |
| | HEATER_TAU | 20 | sec |
| | SPRAY_FLOW_MAX | 900 | gpm |
| | SPRAY_EFFICIENCY | 0.85 | - |

### Usage Example

```csharp
using Critical.Physics;

// Access plant parameters
float power = PlantConstants.THERMAL_POWER_MWT;
float tAvg = PlantConstants.T_AVG;

// Convert units
float pressure_psia = PlantConstants.PsigToPsia(2235f);

// Validate all constants
bool valid = PlantConstants.ValidateConstants();
```

### Validation Method

```csharp
public static bool ValidateConstants()
```
Returns `true` if all derived constants are mathematically consistent.

---

## 6.2 WaterProperties.cs

### Purpose
Provides NIST Steam Table validated thermodynamic property lookups.

### Key Methods

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `SaturationTemperature` | pressure_psia | °F | Tsat at given pressure |
| `SaturationPressure` | temp_F | psia | Psat at given temperature |
| `LatentHeat` | pressure_psia | BTU/lb | hfg at saturation |
| `WaterDensity` | temp_F, pressure_psia | lb/ft³ | Subcooled water density |
| `WaterEnthalpy` | temp_F, pressure_psia | BTU/lb | Subcooled water enthalpy |
| `SaturatedLiquidEnthalpy` | pressure_psia | BTU/lb | hf at saturation |
| `SaturatedSteamEnthalpy` | pressure_psia | BTU/lb | hg at saturation |
| `SaturatedSteamDensity` | pressure_psia | lb/ft³ | ρg at saturation |
| `WaterSpecificHeat` | temp_F, pressure_psia | BTU/(lb·°F) | Cp of water |
| `SubcoolingMargin` | temp_F, pressure_psia | °F | Tsat - T |
| `IsSubcooled` | temp_F, pressure_psia | bool | True if T < Tsat |
| `SurgeEnthalpyDeficit` | temp_F, pressure_psia | BTU/lb | hf - h(T,P) |

### Usage Example

```csharp
using Critical.Physics;

// Get saturation temperature at operating pressure
float tSat = WaterProperties.SaturationTemperature(2250f);
// Returns: ~653°F

// Get water density at average temperature
float rho = WaterProperties.WaterDensity(588.5f, 2250f);
// Returns: ~46 lb/ft³

// Check subcooling margin
float subcooling = WaterProperties.SubcoolingMargin(619f, 2250f);
// Returns: ~34°F

// Calculate surge enthalpy deficit (Gap #10)
float deficit = WaterProperties.SurgeEnthalpyDeficit(619f, 2250f);
// Returns: ~60 BTU/lb
```

### Validation Criteria

| Property | Expected Value | Tolerance |
|----------|---------------|-----------|
| Tsat(2250 psia) | 653°F | ±3°F |
| hfg(2250 psia) | 465 BTU/lb | ±2% |
| ρ(588°F, 2250 psia) | 46 lb/ft³ | ±2% |
| Subcooling(619°F, 2250 psia) | 34°F | ±5°F |

---

## 6.3 SteamThermodynamics.cs

### Purpose
Two-phase flow and steam quality calculations.

### Key Types

```csharp
public enum PhaseState
{
    SubcooledLiquid,
    TwoPhase,
    SuperheatedSteam
}
```

### Key Methods

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `SteamQuality` | h, pressure_psia | 0-1 | Quality from enthalpy |
| `VoidFraction` | quality, pressure_psia | 0-1 | α from x (Chisholm) |
| `TwoPhaseEnthalpy` | quality, pressure_psia | BTU/lb | h = hf + x·hfg |
| `TwoPhaseDensity` | quality, pressure_psia | lb/ft³ | ρ from quality |
| `DeterminePhase` | temp_F, pressure_psia | PhaseState | Phase identification |
| `PressurizerMasses` | level%, pressure_psia | (Mw, Ms) | Water and steam masses |
| `PressurizerLevel` | waterVolume_ft3 | % | Level from volume |

### Usage Example

```csharp
using Critical.Physics;

// Determine phase state
PhaseState phase = SteamThermodynamics.DeterminePhase(600f, 2250f);
// Returns: SubcooledLiquid

// Calculate void fraction from quality
float alpha = SteamThermodynamics.VoidFraction(0.1f, 2250f);
// Returns: ~0.5 (void > quality due to density ratio)

// Get two-phase mixture density
float rho = SteamThermodynamics.TwoPhaseDensity(0.05f, 2250f);
```

### Void Fraction Correlation

Uses Chisholm slip ratio correlation:
```
S = [1 + x(ρf/ρg - 1)]^0.5
α = 1 / [1 + ((1-x)/x) × (ρg/ρf) × S]
```

---

## 6.4 ThermalMass.cs

### Purpose
Heat capacity calculations for metals and fluids.

### Material Properties

| Material | Specific Heat (BTU/(lb·°F)) |
|----------|----------------------------|
| Carbon Steel | 0.12 |
| Stainless Steel | 0.12 |
| Inconel | 0.11 |
| Zircaloy | 0.07 |
| UO2 | 0.06 |

### Key Methods

| Method | Description |
|--------|-------------|
| `HeatRequired(mass, cp, deltaT)` | Q = m × Cp × ΔT |
| `TemperatureChange(Q, mass, cp)` | ΔT = Q / (m × Cp) |
| `EquilibriumTemperature(...)` | Final T after mixing |
| `PressurizerWallHeatCapacity()` | ~24,000 BTU/°F |
| `FirstOrderResponse(...)` | Thermal lag calculation |

### Usage Example

```csharp
using Critical.Physics;

// Calculate heat required
float Q = ThermalMass.HeatRequired(1000f, 0.12f, 50f);
// Q = 6000 BTU

// Get pressurizer wall thermal capacity
float pzrCapacity = ThermalMass.PressurizerWallHeatCapacity();
// Returns: ~24,000 BTU/°F

// Calculate first-order response
float tNew = ThermalMass.FirstOrderResponse(
    tCurrent: 600f,
    tTarget: 650f,
    tau: 20f,
    dt: 1f);
```

---

## 6.5 ThermalExpansion.cs

### Purpose
Volume changes from temperature and pressure variations.

### Key Methods

| Method | Description |
|--------|-------------|
| `ExpansionCoefficient(T, P)` | β in 1/°F |
| `Compressibility(T, P)` | κ in 1/psi |
| `VolumeChangeFromTemp(...)` | ΔV = V × β × ΔT |
| `VolumeChangeFromPressure(...)` | ΔV = -V × κ × ΔP |
| `PressureCoefficient(T, P)` | dP/dT at constant V |
| `UncoupledSurgeVolume(...)` | Free expansion volume |
| `CoupledSurgeVolume(...)` | Expansion with pressure feedback |

### Usage Example

```csharp
using Critical.Physics;

// Get expansion coefficient
float beta = ThermalExpansion.ExpansionCoefficient(588f, 2250f);
// Returns: ~5e-4 /°F

// Calculate pressure coefficient (IMPORTANT for Gap #1)
float dPdT = ThermalExpansion.PressureCoefficient(588f, 2250f);
// Returns: ~7 psi/°F

// Estimate uncoupled surge volume for 10°F rise
float surge = ThermalExpansion.UncoupledSurgeVolume(11500f, 10f, 588f, 2250f);
// Returns: ~57 ft³
```

### Critical Note

The methods in this module provide **estimates only**. For accurate P-T-V coupled behavior, use `CoupledThermo.SolveEquilibrium()`.

---

## 6.6 HeatTransfer.cs

### Purpose
Heat exchanger and enthalpy transport calculations. Implements Gap #10.

### Key Methods

| Method | Description |
|--------|-------------|
| `LMTD(...)` | Log Mean Temperature Difference |
| `HeatTransferRate(U, A, lmtd)` | Q = U × A × LMTD |
| `EnthalpyTransport(mdot, h_in, h_out)` | Q = ṁ × Δh |
| `SurgeEnthalpyDeficit(...)` | Gap #10: ~60 BTU/lb |
| `SurgeHeatingLoad(...)` | Heat to bring surge to Tsat |
| `SprayHeatingLoad(...)` | Heat absorbed by spray |
| `SprayCondensationRate(...)` | Steam condensed by spray |
| `CondensingHTC(...)` | Film condensation HTC |

### Gap #10 Implementation

```csharp
// Surge water at 619°F is subcooled relative to PZR saturation (653°F)
// This creates an enthalpy deficit that affects pressurizer heat balance

float deficit = HeatTransfer.SurgeEnthalpyDeficit(619f, 2250f);
// Returns: ~60 BTU/lb

// For 500 gpm insurge, calculate heating load
float heatingLoad = HeatTransfer.SurgeHeatingLoad(500f, 619f, 2250f);
// Returns: ~1.5e6 BTU/sec
```

---

## 6.7 FluidFlow.cs

### Purpose
Pump dynamics, natural circulation, and surge line hydraulics. Implements Gap #9.

### Key Methods

| Method | Description |
|--------|-------------|
| `PumpCoastdown(...)` | N(t) = N₀ × exp(-t/τ) |
| `PumpCoastdownStep(...)` | Incremental coastdown |
| `AffinityLaws_Flow(...)` | Q ∝ N |
| `AffinityLaws_Head(...)` | H ∝ N² |
| `AffinityLaws_Power(...)` | P ∝ N³ |
| `TotalRCSFlow(pumpSpeeds[])` | Sum of 4 RCP flows |
| `NaturalCirculationFlow(...)` | Gap #9: 12,000-23,000 gpm |
| `SurgeLineFlow(...)` | Darcy-Weisbach calculation |
| `SurgeLinePressureDrop(...)` | ΔP for given flow |

### RCP Coastdown Model

```csharp
// RCP coastdown time constant = 12 seconds
float tau = PlantConstants.RCP_COASTDOWN_TAU; // 12.0f

// Speed at t = τ
float speedAtTau = FluidFlow.PumpCoastdown(1.0f, tau, tau);
// Returns: 0.368 (37% of initial)

// Speed at t = 3τ
float speedAt3Tau = FluidFlow.PumpCoastdown(1.0f, tau, 3f * tau);
// Returns: 0.05 (5% of initial)
```

### Natural Circulation (Gap #9)

```csharp
// Natural circulation is driven by density difference
float natCirc = FluidFlow.NaturalCirculationFlow(
    deltaT_F: 61f,      // Hot - Cold temperature difference
    elevation_ft: 30f,   // Driving head
    resistance: 1f);     // Flow resistance factor

// Returns: 12,000-23,000 gpm (3-6% of normal)
```

### Surge Line Hydraulics (Gap #9)

```csharp
// Calculate surge flow from pressure difference
float flow = FluidFlow.SurgeLineFlow(
    deltaP_psi: 10f,           // P_RCS - P_PZR
    diameter_in: 14f,          // Surge line ID
    length_ft: 50f,            // Surge line length
    friction: 0.015f,          // Darcy friction factor
    density_lb_ft3: 46f);      // Water density

// Positive flow = insurge (into pressurizer)
// Negative flow = outsurge (out of pressurizer)
```

---

## 6.8 ReactorKinetics.cs

### Purpose
Point kinetics, reactivity feedback, xenon dynamics, and decay heat.

### Delayed Neutron Data

| Group | βᵢ | λᵢ (1/s) |
|-------|-----|----------|
| 1 | 0.000215 | 0.0124 |
| 2 | 0.001424 | 0.0305 |
| 3 | 0.001274 | 0.111 |
| 4 | 0.002568 | 0.301 |
| 5 | 0.000748 | 1.14 |
| 6 | 0.000273 | 3.01 |
| **Total** | **0.0065** | - |

### Reactivity Coefficients

| Coefficient | Value | Notes |
|-------------|-------|-------|
| Doppler | -2.5 pcm/√°R | Always negative |
| MTC (high boron) | +5 pcm/°F | Positive at BOL |
| MTC (low boron) | -40 pcm/°F | Negative at EOL |
| Boron Worth | -9 pcm/ppm | Always negative |

### Key Methods

| Method | Description |
|--------|-------------|
| `PointKinetics(...)` | 6-group point kinetics solver |
| `EquilibriumPrecursors(power)` | Steady-state precursor concentrations |
| `DopplerReactivity(...)` | Doppler feedback |
| `ModeratorTempCoefficient(boron)` | MTC vs boron |
| `ModeratorReactivity(...)` | MTC feedback |
| `BoronReactivity(...)` | Boron worth |
| `ControlRodReactivity(...)` | Rod worth vs position |
| `XenonEquilibrium(power)` | Equilibrium Xe worth |
| `XenonTransient(...)` | Xe transient after power change |
| `DecayHeatFraction(t)` | ANS 5.1-2005 decay heat |
| `DecayHeatPower(t)` | Decay heat in MWt |

### Usage Example

```csharp
using Critical.Physics;

// Calculate Doppler feedback for 100°F fuel temperature rise
float doppler = ReactorKinetics.DopplerReactivity(100f, 1000f);
// Returns: ~-12 pcm (negative = safe)

// Get MTC at current boron concentration
float mtc = ReactorKinetics.ModeratorTempCoefficient(800f);
// Returns: ~-15 pcm/°F

// Calculate decay heat 1 hour after trip
float decayHeat = ReactorKinetics.DecayHeatFraction(3600f);
// Returns: ~0.015 (1.5% of full power)
```

---

## 6.9 PressurizerPhysics.cs

### Purpose
Three-region pressurizer model with flash, spray, heaters, and wall effects.
Implements Gaps #2-8.

### PressurizerState Structure

```csharp
public struct PressurizerState
{
    // Primary state
    public float Pressure;           // psia
    public float PressureRate;       // psi/sec
    public float WaterMass;          // lb
    public float SteamMass;          // lb
    public float WaterVolume;        // ft³
    public float SteamVolume;        // ft³
    
    // Temperatures
    public float WallTemp;           // °F
    public float SteamTemp;          // °F
    
    // Heater state
    public float HeaterEffectivePower; // kW (after lag)
    
    // Diagnostic rates
    public float FlashRate;          // lb/sec
    public float SprayCondRate;      // lb/sec
    public float HeaterSteamRate;    // lb/sec
    public float WallCondRate;       // lb/sec
    public float RainoutRate;        // lb/sec
    
    // Derived
    public float Level => WaterVolume / PZR_TOTAL_VOLUME * 100f;
}
```

### Gap Implementation Summary

| Gap | Description | Method |
|-----|-------------|--------|
| #2 | Flash evaporation | `FlashEvaporationRate()` |
| #3 | Spray efficiency (85%) | `SprayCondensationRate()` |
| #4 | Three-region model | `ThreeRegionUpdate()` |
| #6 | Wall condensation | `WallCondensationRate()` |
| #7 | Rainout | `RainoutRate()` |
| #8 | Heater lag (τ=20s) | `HeaterLagResponse()` |

### Key Methods

| Method | Description |
|--------|-------------|
| `ThreeRegionUpdate(...)` | Main update function |
| `FlashEvaporationRate(...)` | Gap #2: Self-regulating flash |
| `SprayCondensationRate(...)` | Gap #3: η = 85% |
| `HeaterSteamRate(...)` | Steam generation from heaters |
| `HeaterLagResponse(...)` | Gap #8: τ = 20s |
| `WallCondensationRate(...)` | Gap #6: Continuous |
| `RainoutRate(...)` | Gap #7: Bulk condensation |
| `SprayFlowDemand(P)` | Spray controller |
| `HeaterPowerDemand(P)` | Heater controller |
| `InitializeSteadyState(...)` | Create initial state |

### Usage Example

```csharp
using Critical.Physics;

// Initialize pressurizer at 60% level
var state = PressurizerPhysics.InitializeSteadyState(2250f, 60f);

// Simulate one time step
float dt = 0.1f;  // 100 ms time step
PressurizerPhysics.ThreeRegionUpdate(
    ref state,
    surgeFlow_gpm: 100f,      // Insurge
    surgeTemp_F: 619f,        // Hot leg temperature
    sprayFlow_gpm: 0f,        // No spray
    sprayTemp_F: 558f,        // Cold leg temperature
    heaterPower_kW: 500f,     // Partial heater power
    dt_sec: dt);

// Check results
Console.WriteLine($"Level: {state.Level:F1}%");
Console.WriteLine($"Flash rate: {state.FlashRate:F2} lb/sec");
```

### Heater Thermal Lag (Gap #8)

```csharp
// Heater has 20-second time constant
float tau = PlantConstants.HEATER_TAU; // 20 seconds

// At t = τ, effective power = 63.2% of demand
// At t = 3τ, effective power = 95% of demand

float effectivePower = PressurizerPhysics.HeaterLagResponse(
    currentPower_kW: 0f,
    demandPower_kW: 1800f,
    tau_sec: 20f,
    dt_sec: 20f);
// Returns: ~1138 kW (63.2%)
```

---

## 6.10 CoupledThermo.cs

### Purpose
**THE MOST CRITICAL MODULE** - Iterative solver for coupled pressure-temperature-volume behavior. Implements Gap #1.

### Why This Module Matters

In a closed RCS:
1. Temperature increases → Water expands
2. But total volume is fixed → Pressure must increase
3. Higher pressure → Higher density → Less expansion
4. This creates a coupled system requiring iteration

**Without this module:** 10°F rise → 0 psi change (WRONG)  
**With this module:** 10°F rise → 60-80 psi change (CORRECT)

### SystemState Structure

```csharp
public struct SystemState
{
    public float Pressure;        // psia
    public float Temperature;     // °F (RCS average)
    
    public float RCSVolume;       // ft³ (fixed)
    public float RCSWaterMass;    // lb
    
    public float PZRWaterVolume;  // ft³
    public float PZRSteamVolume;  // ft³
    public float PZRWaterMass;    // lb
    public float PZRSteamMass;    // lb
    
    public int IterationsUsed;    // Solver diagnostics
    
    public float TotalMass => RCSWaterMass + PZRWaterMass + PZRSteamMass;
    public float PZRLevel => PZRWaterVolume / PZR_TOTAL_VOLUME * 100f;
}
```

### Key Methods

| Method | Description |
|--------|-------------|
| `SolveEquilibrium(ref state, deltaT)` | Main iterative solver |
| `QuickPressureEstimate(...)` | Fast analytic approximation |
| `SolveTransient(...)` | Time-stepping with heat input |
| `SolveWithPressurizer(...)` | Full system with controls |
| `InitializeAtSteadyState()` | Create initial state |
| `Validate10DegreeTest()` | **CRITICAL VALIDATION** |
| `ValidateAll()` | Run all validation tests |

### Usage Example

```csharp
using Critical.Physics;

// Initialize at normal operating conditions
var state = CoupledThermo.InitializeAtSteadyState();
// Pressure: 2250 psia, Temperature: 588.5°F

float P0 = state.Pressure;

// Apply 10°F temperature rise
bool converged = CoupledThermo.SolveEquilibrium(ref state, 10f);

float deltaP = state.Pressure - P0;
Console.WriteLine($"Converged: {converged}");
Console.WriteLine($"Iterations: {state.IterationsUsed}");
Console.WriteLine($"ΔP: {deltaP:F1} psi");

// Expected output:
// Converged: True
// Iterations: 8-15
// ΔP: 60-80 psi
```

### Solver Algorithm

```
1. Save initial state (P₀, T₀, masses)
2. Estimate new pressure: P_new = P₀ + (dP/dT) × ΔT
3. ITERATE until converged:
   a. Calculate densities at (T_new, P_new)
   b. Calculate RCS volume change from expansion
   c. This volume goes to/from pressurizer
   d. Calculate new PZR water and steam masses
   e. Check mass conservation
   f. Adjust pressure based on mass error
   g. Check convergence criteria
4. Update state if converged, rollback if not
```

### Convergence Criteria

| Criterion | Tolerance |
|-----------|-----------|
| Pressure change | < 0.1 psi |
| Mass error | < 0.1% |
| Max iterations | 50 |

### Critical Validation

```csharp
// This test MUST pass before proceeding to Phase 2
bool valid = CoupledThermo.Validate10DegreeTest();

if (!valid)
{
    Console.WriteLine("CRITICAL FAILURE: 10°F test failed!");
    Console.WriteLine("Pressure response not in 60-80 psi range.");
    Console.WriteLine("DO NOT PROCEED TO PHASE 2.");
}
```

---

# 7. Test Framework

## 7.1 Test Categories

| Category | Tests | Module(s) |
|----------|-------|-----------|
| PC | 5 | PlantConstants |
| WP | 18 | WaterProperties |
| ST | 10 | SteamThermodynamics |
| TM | 6 | ThermalMass |
| TE | 6 | ThermalExpansion |
| HT | 6 | HeatTransfer |
| FF | 8 | FluidFlow |
| RK | 10 | ReactorKinetics |
| PZ | 24 | PressurizerPhysics |
| CT | 12 | CoupledThermo |
| INT | 7 | Integration |
| **Total** | **112** | - |

## 7.2 Running Tests

### In Unity

```csharp
// Create a test runner MonoBehaviour
public class TestRunner : MonoBehaviour
{
    void Start()
    {
        var runner = new Phase1TestRunner();
        runner.RunAllTests();
    }
}
```

### In Console

```csharp
// Run from Main()
Phase1TestRunner.Main(null);
```

## 7.3 Test Output Format

```
╔═══════════════════════════════════════════════════════════════╗
║     CRITICAL: MASTER THE ATOM - PHASE 1 PHYSICS TESTS        ║
╚═══════════════════════════════════════════════════════════════╝

─────────────────────────────────────────────────────────────────
  PlantConstants Tests (5 tests)
─────────────────────────────────────────────────────────────────
  [PASS] PC-01: T_AVG = (T_HOT + T_COLD) / 2
  [PASS] PC-02: CORE_DELTA_T = T_HOT - T_COLD
  ...

═══════════════════════════════════════════════════════════════
  INTEGRATION TESTS (7 tests)
═══════════════════════════════════════════════════════════════
  [PASS] INT-01: Coupled Pressure Response (10°F → 60-80 psi)
  ...

╔═══════════════════════════════════════════════════════════════╗
║                    PHASE 1 TEST SUMMARY                       ║
╠═══════════════════════════════════════════════════════════════╣
║  Total Tests:  112                                            ║
║  Passed:       112                                            ║
║  Failed:         0                                            ║
╠═══════════════════════════════════════════════════════════════╣
║  ✓ ALL TESTS PASSED - PHASE 1 EXIT GATE MET                  ║
╚═══════════════════════════════════════════════════════════════╝
```

---

# 8. Validation Procedures

## 8.1 Pre-Integration Checklist

Before running tests, verify:

- [ ] All 10 physics files compiled without errors
- [ ] All 2 test files compiled without errors
- [ ] Namespace `Critical.Physics` used consistently
- [ ] No Unity-specific code in physics modules (except Mathf)

## 8.2 Module Validation Order

Run module validations in this order:

```csharp
// 1. PlantConstants
Assert(PlantConstants.ValidateConstants());

// 2. WaterProperties
Assert(WaterProperties.ValidateAgainstNIST());

// 3. SteamThermodynamics
Assert(SteamThermodynamics.ValidateCalculations());

// 4. ThermalMass
Assert(ThermalMass.ValidateCalculations());

// 5. ThermalExpansion
Assert(ThermalExpansion.ValidateCalculations());

// 6. HeatTransfer
Assert(HeatTransfer.ValidateCalculations());

// 7. FluidFlow
Assert(FluidFlow.ValidateCalculations());

// 8. ReactorKinetics
Assert(ReactorKinetics.ValidateCalculations());

// 9. PressurizerPhysics
Assert(PressurizerPhysics.ValidateCalculations());

// 10. CoupledThermo (CRITICAL)
Assert(CoupledThermo.ValidateAll());
```

## 8.3 Critical Validation: 10°F Test

This is the single most important test:

```csharp
bool passed = CoupledThermo.Validate10DegreeTest();

// Expected behavior:
// - Starting conditions: 2250 psia, 588.5°F
// - After 10°F rise: 2310-2330 psia
// - ΔP = 60-80 psi
// - Convergence in < 20 iterations
```

If this test fails:
1. Check `WaterProperties` density calculations
2. Verify `ThermalExpansion` coefficients
3. Review `CoupledThermo` solver logic
4. Check mass conservation

## 8.4 Integration Test Descriptions

| Test | Description | Validates |
|------|-------------|-----------|
| INT-01 | 10°F → 60-80 psi | Gap #1 (CRITICAL) |
| INT-02 | Insurge transient | Level rise, mass balance |
| INT-03 | Outsurge with flash | Gap #2 (flash retards dP/dt) |
| INT-04 | Power trip feedback | Doppler + MTC negative |
| INT-05 | RCP coastdown | τ=12s, nat circ 3-6% |
| INT-06 | Mass conservation | < 0.1% error |
| INT-07 | Energy conservation | < 20% error |

---

# 9. Physics Gap Resolution

## 9.1 Gap Summary Table

| Gap # | Issue | Resolution | Module | Validated By |
|-------|-------|------------|--------|--------------|
| 1 | P-T-V coupling | Iterative solver | CoupledThermo | CT-02, INT-01 |
| 2 | Flash evaporation | Self-regulating model | PressurizerPhysics | PZ-01, INT-03 |
| 3 | Spray efficiency | η = 85% | PressurizerPhysics | PZ-03, PZ-21 |
| 4 | Three-region model | Full implementation | PressurizerPhysics | PZ-13 to PZ-19 |
| 5 | Plant constants | FSAR values | PlantConstants | PC-01 to PC-05 |
| 6 | Wall condensation | Continuous model | PressurizerPhysics | PZ-04 |
| 7 | Rainout | Bulk condensation | PressurizerPhysics | PZ-05 |
| 8 | Heater lag | τ = 20s | PressurizerPhysics | PZ-06, PZ-07 |
| 9 | Surge hydraulics | Darcy-Weisbach | FluidFlow | FF-04, FF-05 |
| 10 | Enthalpy transport | ~60 BTU/lb deficit | HeatTransfer | HT-01, WP-13 |
| 11 | Steam table accuracy | NIST validation | WaterProperties | WP-17 |
| 12 | Enthalpy functions | Complete set | WaterProperties | WP-06, WP-07 |
| 13 | Quality/void | Chisholm correlation | SteamThermodynamics | ST-01 to ST-06 |

## 9.2 Gap #1 Deep Dive: P-T-V Coupling

### The Problem

In simple models, temperature and pressure are calculated independently:
- T increases → Calculate new T
- P stays constant → WRONG!

In reality, the RCS is a closed system:
- T increases → Water expands
- Volume is fixed → P must increase
- P increase compresses water → Less expansion

### The Solution

The `CoupledThermo.SolveEquilibrium()` method:

1. **Estimates** initial pressure from dP/dT coefficient
2. **Calculates** water density at new (T, P)
3. **Computes** RCS expansion volume
4. **Transfers** expansion to pressurizer steam space
5. **Checks** mass conservation
6. **Adjusts** pressure based on mass error
7. **Iterates** until convergence

### Validation

The 10°F → 60-80 psi response matches:
- EPRI NP-2923: 5-8 psi/°F typical
- Plant operating experience
- RELAP5 benchmark calculations

---

# 10. Troubleshooting

## 10.1 Compilation Errors

### Error: "The type 'PlantConstants' could not be found"

**Cause:** Missing namespace or file not compiled.

**Solution:**
```csharp
// Ensure using statement
using Critical.Physics;

// Verify file is in correct location
// Assets/Scripts/Physics/PlantConstants.cs
```

### Error: "Mathf does not exist"

**Cause:** Unity namespace not available in standalone .NET.

**Solution:**
```csharp
// Replace Mathf with Math
// Before:
float result = Mathf.Sqrt(x);

// After:
float result = (float)Math.Sqrt(x);
```

## 10.2 Test Failures

### CT-02 Fails: Pressure Response Wrong

**Symptoms:**
- ΔP < 50 psi or > 100 psi
- Solver doesn't converge

**Diagnosis:**
1. Check `WaterProperties.WaterDensity()` returns ~46 lb/ft³
2. Verify `ThermalExpansion.ExpansionCoefficient()` returns ~5e-4
3. Check `ThermalExpansion.Compressibility()` returns ~7e-6

**Common Causes:**
- Wrong units (°C vs °F, MPa vs psia)
- Missing pressure dependence in density
- Expansion coefficient too low/high

### INT-01 Fails: Coupled Response

**Symptoms:**
- Same as CT-02

**Solution:**
- Fix CT-02 first, then INT-01 will pass

### PZ-07 Fails: Heater Lag

**Symptoms:**
- Effective power doesn't reach 63% at τ

**Solution:**
```csharp
// Verify exponential calculation
float alpha = 1f - (float)Math.Exp(-dt/tau);
// At dt = tau: alpha = 0.632
```

### WP-03 Fails: Saturation Temperature

**Symptoms:**
- Tsat(2250) ≠ 653°F

**Solution:**
- Check polynomial coefficients in `SaturationTemperature()`
- Verify input pressure is in psia (not psig)

## 10.3 Runtime Errors

### NaN or Infinity Values

**Cause:** Division by zero or invalid math operation.

**Solution:**
```csharp
// All divisions should be protected
if (denominator < 1e-9f) return 0f;
float result = numerator / denominator;
```

### Solver Doesn't Converge

**Cause:** Initial estimate too far from solution.

**Solution:**
- Increase `MAX_ITERATIONS` (default 50)
- Reduce `RELAXATION_FACTOR` (default 0.5)
- Check initial state is physically reasonable

---

# 11. API Quick Reference

## 11.1 PlantConstants

```csharp
// Temperatures
float T_HOT = 619f;           // °F
float T_COLD = 558f;          // °F
float T_AVG = 588.5f;         // °F

// Pressure
float OPERATING_PRESSURE = 2250f; // psia

// Flow
float RCS_FLOW_TOTAL = 390400f;   // gpm

// Pressurizer
float PZR_TOTAL_VOLUME = 1800f;   // ft³
float HEATER_TAU = 20f;           // seconds
float SPRAY_EFFICIENCY = 0.85f;   // 85%

// Validation
bool valid = PlantConstants.ValidateConstants();
```

## 11.2 WaterProperties

```csharp
// Saturation
float Tsat = WaterProperties.SaturationTemperature(P_psia);
float Psat = WaterProperties.SaturationPressure(T_F);
float hfg = WaterProperties.LatentHeat(P_psia);

// Subcooled
float rho = WaterProperties.WaterDensity(T_F, P_psia);
float h = WaterProperties.WaterEnthalpy(T_F, P_psia);
float cp = WaterProperties.WaterSpecificHeat(T_F, P_psia);
float subcooling = WaterProperties.SubcoolingMargin(T_F, P_psia);

// Saturated
float hf = WaterProperties.SaturatedLiquidEnthalpy(P_psia);
float hg = WaterProperties.SaturatedSteamEnthalpy(P_psia);
float rhog = WaterProperties.SaturatedSteamDensity(P_psia);
```

## 11.3 SteamThermodynamics

```csharp
// Quality and void
float x = SteamThermodynamics.SteamQuality(h, P_psia);
float alpha = SteamThermodynamics.VoidFraction(x, P_psia);

// Phase determination
PhaseState phase = SteamThermodynamics.DeterminePhase(T_F, P_psia);

// Two-phase properties
float h_mix = SteamThermodynamics.TwoPhaseEnthalpy(x, P_psia);
float rho_mix = SteamThermodynamics.TwoPhaseDensity(x, P_psia);
```

## 11.4 FluidFlow

```csharp
// Pump coastdown
float speed = FluidFlow.PumpCoastdown(N0, tau, t);
float[] speeds = { 1f, 1f, 1f, 1f };
float totalFlow = FluidFlow.TotalRCSFlow(speeds);

// Natural circulation
float natCirc = FluidFlow.NaturalCirculationFlow(deltaT, H, R);

// Surge line
float flow = FluidFlow.SurgeLineFlow(deltaP, D, L, f, rho);
```

## 11.5 ReactorKinetics

```csharp
// Reactivity feedback
float rho_doppler = ReactorKinetics.DopplerReactivity(deltaT_fuel, T_fuel);
float mtc = ReactorKinetics.ModeratorTempCoefficient(boron_ppm);
float rho_mod = ReactorKinetics.ModeratorReactivity(deltaT_mod, boron_ppm);
float rho_boron = ReactorKinetics.BoronReactivity(delta_boron);

// Xenon
float xe_eq = ReactorKinetics.XenonEquilibrium(powerFraction);
float xe = ReactorKinetics.XenonTransient(xe0, power, time_hr);

// Decay heat
float frac = ReactorKinetics.DecayHeatFraction(time_sec);
float power = ReactorKinetics.DecayHeatPower(time_sec);
```

## 11.6 PressurizerPhysics

```csharp
// Initialize
var state = PressurizerPhysics.InitializeSteadyState(P_psia, level_pct);

// Update
PressurizerPhysics.ThreeRegionUpdate(
    ref state, surgeFlow, surgeT, sprayFlow, sprayT, heaterPower, dt);

// Phase change rates
float flash = PressurizerPhysics.FlashEvaporationRate(P, dPdt, M_water);
float spray = PressurizerPhysics.SprayCondensationRate(flow, T, P);
float heater = PressurizerPhysics.HeaterSteamRate(demand, effective, P);

// Controllers
float sprayDemand = PressurizerPhysics.SprayFlowDemand(P_psig);
float heaterDemand = PressurizerPhysics.HeaterPowerDemand(P_psig);
```

## 11.7 CoupledThermo

```csharp
// Initialize
var state = CoupledThermo.InitializeAtSteadyState();

// Solve
bool converged = CoupledThermo.SolveEquilibrium(ref state, deltaT);
bool ok = CoupledThermo.SolveTransient(ref state, Q_BTU_sec, dt);

// Quick estimate
float deltaP = CoupledThermo.QuickPressureEstimate(T, P, dT, V_RCS, V_steam);

// Validation
bool valid = CoupledThermo.Validate10DegreeTest();
bool allValid = CoupledThermo.ValidateAll();
```

---

# Document Control

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-02-04 | Claude | Initial release |

---

**END OF MANUAL**
