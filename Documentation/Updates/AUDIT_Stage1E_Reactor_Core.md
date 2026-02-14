# AUDIT Stage 1E: Reactor Core Modules

**Audit Date:** 2026-02-06  
**Auditor:** Claude (AI-assisted)  
**Stage:** 1E of 1A–1G (File Inventory & Architecture Mapping)  
**Scope:** 7 files in `Assets/Scripts/Reactor/` (~182 KB total)  

---

## Files Analyzed

| # | File | Size (approx) | Lines (approx) | Status |
|---|------|---------------|-----------------|--------|
| 1 | ReactorCore.cs | 24 KB | ~470 | GOLD STANDARD |
| 2 | ControlRodBank.cs | 29 KB | ~580 | GOLD STANDARD |
| 3 | FuelAssembly.cs | 32 KB | ~560 | GOLD STANDARD |
| 4 | FeedbackCalculator.cs | 20 KB | ~400 | GOLD STANDARD |
| 5 | PowerCalculator.cs | 18 KB | ~360 | GOLD STANDARD |
| 6 | ReactorController.cs | 30 KB | ~560 | GOLD STANDARD |
| 7 | ReactorSimEngine.cs | 30 KB | ~540 | GOLD STANDARD |

**Total: 7 files, ~182 KB, ~3,470 lines**

---

## 1. ReactorCore.cs — Integrated Reactor Core Model

### Purpose
Master integration module coordinating all reactor physics sub-modules. Owns the complete reactor physics loop: reactivity → point kinetics → fuel temperatures → feedback → iterate.

### Architecture
- **Role:** Physics integrator — orchestrates FuelAssembly, ControlRodBank, PowerCalculator, FeedbackCalculator
- **Pattern:** Composition — owns instances of all sub-modules, exposes via properties
- **Thread Safety:** Single-threaded (Unity main thread)

### Public Interface
- `ReactorCore(initialTavg_F, initialBoron_ppm, rodsWithdrawn)` — Constructor
- `Update(coolantInletTemp_F, flowFraction, dt_sec)` — Main timestep entry point
- `Trip()` / `ResetTrip()` — Trip control
- `SetBoron(ppm)` / `ChangeBoron(delta_ppm)` — Chemistry control
- `WithdrawRods()` / `InsertRods()` / `StopRods()` — Rod commands (delegates to ControlRodBank)
- `GetState() → ReactorCoreState` — Complete snapshot struct
- `InitializeToHZP()` — Hot Zero Power initialization
- `InitializeToEquilibrium(powerFraction)` — Equilibrium power initialization with critical boron search
- Properties: NeutronPower, ThermalPower, Tavg, Thot, Tcold, Boron_ppm, Xenon_pcm, IsCritical, IsSubcritical, IsTripped, Keff

### Key Physics
- **Kinetics subdivision:** dt subdivided to MAX_KINETICS_DT=0.1s for stability, multiple kinetics steps per thermal step
- **Rod reactivity model:** `netRodReactivity = TotalRodReactivity - TOTAL_WORTH_PCM` (deviation from all-rods-out reference)
- **Thermal:** ΔT = CORE_DELTA_T × power / flow; Tavg = (Thot + Tcold) / 2
- **Xenon:** Rate equation integrated per timestep, clamped -5000 to 0 pcm
- **Trip logic:** High flux >109%, overpower ΔT >120% nominal, low flow <87%
- **Equilibrium initialization:** Iterates critical boron search (FeedbackCalculator.EstimateCriticalBoron) with 5 convergence passes

### Constants (Local)
| Constant | Value | Cross-ref |
|----------|-------|-----------|
| SUBCRITICAL_THRESHOLD_PCM | -100 pcm | — |
| CRITICAL_THRESHOLD_PCM | 50 pcm | — |
| OVERPOWER_TRIP | 1.18 | PlantConstants analog absent |
| HIGH_FLUX_TRIP | 1.09 | PlantConstants analog absent |
| LOW_FLOW_TRIP | 0.87 | PlantConstants.LOW_FLOW_TRIP=0.87 |
| MIN_KINETICS_DT | 0.001 s | — |
| MAX_KINETICS_DT | 0.1 s | — |

### Dependencies
- `FuelAssembly` (2 instances: average + hot channel)
- `ControlRodBank` (1 instance)
- `PowerCalculator` (1 instance)
- `FeedbackCalculator` (1 instance)
- `ReactorKinetics` (static: EquilibriumPrecursors, PointKinetics, XenonEquilibrium, XenonRate, DecayHeatPower)
- `PlantConstants` (T_AVG_NO_LOAD, CORE_DELTA_T, T_COLD, T_AVG)

### Validation
6 tests in `ValidateCalculations()`:
1. HZP with rods in → subcritical
2. Rod withdrawal adds reactivity
3. Trip inserts all rods within 3s
4. Equilibrium at 100% → Tavg ≈ 588°F
5. Xenon present at power (< -1000 pcm)
6. Doppler feedback negative at power

### Issues
- **ISSUE #8 (LOW):** OVERPOWER_TRIP=1.18 and HIGH_FLUX_TRIP=1.09 defined locally. These are safety system setpoints that could be centralized in PlantConstants for consistency with AlarmManager. However, keeping trip setpoints local to the tripping module is defensible.
- **ISSUE #9 (MEDIUM):** `ReactorKinetics` dependency not audited yet — referenced for PointKinetics, XenonEquilibrium, XenonRate, EquilibriumPrecursors, DecayHeatPower, DopplerReactivity, ModeratorReactivity, BoronReactivity, ModeratorTempCoefficient. This is a Stage 1C file (Physics/ReactorKinetics.cs) already inventoried but its interface must be verified against all callers in Stage 4.
- **ISSUE #10 (LOW):** `UpdateXenon` converts dt_sec to hours via `dt_sec/3600` — correct but could use a named constant for clarity.
- **ISSUE #11 (INFO):** `_flowFraction` minimum clamped at 0.03 (natural circulation). Matches PlantConstants.NAT_CIRC_PERCENT_MIN.

---

## 2. ControlRodBank.cs — Control Rod Bank Model with S-Curve Worth

### Purpose
Models all 8 control rod banks (4 shutdown SA/SB/SC/SD + 4 control D/C/B/A) with sine-squared integral worth, sequential withdrawal/insertion with overlap, and gravity rod drop dynamics.

### Architecture
- **Role:** Self-contained rod physics module
- **Pattern:** State machine with per-bank position/direction tracking
- **No external dependencies** (pure math, no PlantConstants reference)

### Public Interface
- `ControlRodBank(initiallyWithdrawn)` — Constructor
- `Update(dt_sec)` — Position update (normal motion or trip dynamics)
- `WithdrawBank(bank)` / `InsertBank(bank)` / `StopBank(bank)` — Individual bank commands
- `WithdrawInSequence()` / `InsertInSequence()` — Automatic sequential operation
- `Trip()` / `ResetTrip()` — Trip control
- `SetBankPosition(bank, position)` / `SetAllBankPositions(position)` — Direct set
- `CalculateBankReactivity(bankIndex, position)` — S-curve worth at given position
- `CalculateDifferentialWorth(bankIndex, position)` — Differential worth in pcm/step
- Properties: TotalRodReactivity, BankDPosition, BankAPosition, IsTripped, AllRodsOut, AllRodsIn, RodBottomAlarm, BankSequenceViolation

### Key Physics
- **S-curve worth:** `Worth = TotalWorth × sin²(π × fraction / 2)` — standard sine-squared integral worth model
- **Differential worth:** `d(Worth)/d(position) = TotalWorth × (π/2) × sin(π×x) / STEPS_TOTAL` — peaks at mid-stroke
- **Sequential withdrawal:** Banks withdraw in order SA→SB→...→A with 100-step overlap
- **Rod drop:** Two-phase model: free fall to dashpot (1.2s), then dashpot deceleration (0.8s), total 2.0s
- **Bank worths:** SA=SB=SC=SD=1500, D=1200, C=600, B=A=400 pcm → Total=8600 pcm

### Constants (Local)
| Constant | Value | Cross-ref |
|----------|-------|-----------|
| BANK_COUNT | 8 | PlantConstants.ROD_BANKS=8 |
| STEPS_TOTAL | 228 | PlantConstants.ROD_TOTAL_STEPS=228 |
| STEPS_PER_MINUTE | 72 | PlantConstants.ROD_STEPS_PER_MINUTE=72 |
| BANK_OVERLAP_STEPS | 100 | — (operational procedure) |
| BANK_D_INSERTION_LIMIT | 30 | — (Tech Spec limit) |
| TOTAL_WORTH_PCM | 8600 | ≥ PlantConstants.SHUTDOWN_MARGIN=8000 |
| ROD_DROP_TIME_SEC | 2.0 | — (FSAR Chapter 4 ≤ 2.2s typ.) |
| ROD_DROP_TO_DASHPOT_SEC | 1.2 | — |
| DASHPOT_POSITION | 34 steps | — |
| BANK_WORTH_PCM[] | [1500,1500,1500,1500,1200,600,400,400] | — |

### Dependencies
- None (self-contained)

### Validation
8 tests in `ValidateCalculations()`:
1. Full in → zero reactivity
2. Full out → 8600 pcm
3. S-curve at 60% → ~65.5% worth
4. Half withdrawal → ~50% worth (sin²(45°)=0.5)
5. Differential worth peaks at mid-position
6. Trip inserts all rods
7. Bank sequence check (alarm logic)
8. Sum of BANK_WORTH_PCM = TOTAL_WORTH_PCM

### Issues
- **ISSUE #12 (MEDIUM):** Constants STEPS_TOTAL, STEPS_PER_MINUTE, BANK_COUNT duplicate PlantConstants values (ROD_TOTAL_STEPS, ROD_STEPS_PER_MINUTE, ROD_BANKS). Currently match but maintenance risk. Same pattern as VCTPhysics Issue #1.
- **ISSUE #13 (LOW):** Rod drop model is simplified (linear velocity + dashpot). Real rod drop follows gravity-friction curve. Adequate for simulator fidelity but noted.
- **ISSUE #14 (INFO):** BANK_WORTH_PCM values are typical Westinghouse 4-loop values. Individual bank worths vary by cycle. Acceptable as representative values.

---

## 3. FuelAssembly.cs — Fuel Temperature Model with Radial Profile

### Purpose
Models radial temperature distribution in a PWR fuel rod: Fuel Centerline → Pellet Surface → Gap → Clad Inner → Clad Outer → Coolant. Provides effective fuel temperature for Doppler feedback.

### Architecture
- **Role:** Thermal-hydraulic fuel model
- **Pattern:** Physics module with steady-state target + first-order thermal lag
- **Key Method:** Integral conductivity method (Newton-Raphson) for UO2 temperature profile

### Public Interface
- `FuelAssembly(assemblyIndex, initialCoolantTemp_F, burnup, peakingFactor)` — Constructor
- `Update(powerFraction, coolantTemp_F, flowFraction, dt_sec)` — Main timestep
- `CalculateUO2Conductivity(temp_F)` — Static: Fink (2000) correlation
- `CalculateGapConductance(burnup_MWdMTU)` — Static: Burnup-dependent gap
- `CalculateEffectiveFuelTemp(centerline_F, surface_F)` — Static: Rowlands weighting (0.4)
- `CreateAverageAssembly(temp, burnup)` / `CreateHotChannelAssembly(temp, burnup, Fq)` — Factory methods
- Properties: CenterlineTemp_F, PelletSurfaceTemp_F, CladInnerTemp_F, CladOuterTemp_F, EffectiveFuelTemp_F, LinearHeatRate_kWft, MeltingMargin_F, IsMelted

### Key Physics
- **UO2 Conductivity:** Fink (2000) JNM 279:1-18: `k = 100/(A + Bt + Ct²) + D/t^2.5 × exp(-E/t)` at 95% TD
  - A=7.5408, B=17.692, C=3.6142, D=6400, E=16.35 (SI, converted to BTU)
  - Validated range: 300K–3120K
- **Integral Conductivity Method:** Newton-Raphson solving `∫[Ts→Tcl] k(T)dT = q'/(4π)` per Todreas & Kazimi
  - Simpson's rule integration with 20 panels (41 points)
  - Convergence tolerance: 0.5 BTU/(hr·ft) → <0.5°F error
  - Max 20 iterations (typically converges in 5–8)
- **Gap Conductance:** BOL=500, EOL=1760 BTU/(hr·ft²·°F), linear ramp with burnup (gap closes at 25,000 MWd/MTU)
- **Thermal Lag:** First-order: τ_fuel=7s, τ_clad=0.5s
- **Effective Fuel Temperature:** Rowlands: T_eff = T_surface + 0.4 × (T_centerline − T_surface)

### Constants (Local)
| Constant | Value | Source/Cross-ref |
|----------|-------|-----------------|
| PELLET_RADIUS_FT | 0.01343 | 17×17: 4.095mm radius |
| GAP_WIDTH_FT | 0.000558 | 0.17mm gap |
| CLAD_THICKNESS_FT | 0.00187 | 0.57mm Zr-4 |
| ACTIVE_LENGTH_FT | 12 | PlantConstants.ACTIVE_HEIGHT=12 |
| FINK_A/B/C/D/E | 7.5408/17.692/3.6142/6400/16.35 | Fink (2000) |
| UO2_HEAT_CAPACITY | 48.8 BTU/(ft³·°F) | 10970×300=3.29 MJ/(m³·K) |
| UO2_MELTING_POINT_F | 5189 | 2865°C |
| ZIRC_CONDUCTIVITY | 8.67 BTU/(hr·ft·°F) | 15 W/(m·K) |
| GAP_CONDUCTANCE_BOL | 500 BTU/(hr·ft²·°F) | FRAPCON-4 |
| GAP_CONDUCTANCE_EOL | 1760 BTU/(hr·ft²·°F) | FRAPCON-4 |
| COOLANT_HTC_NOMINAL | 6000 BTU/(hr·ft²·°F) | Dittus-Boelter |
| FUEL_THERMAL_TAU_SEC | 7 | ρcp r²/(4k) |

### Dependencies
- `PlantConstants` (AVG_LINEAR_HEAT, FUEL_ASSEMBLIES)

### Validation
10 tests in `ValidateCalculations()`:
1. Zero power → isothermal
2. Full power → 1500–3000°F centerline
3. Centerline > surface
4. Surface > clad inner
5. Clad inner > clad outer
6. Clad outer > coolant
7. Gap conductance increases with burnup
8. UO2 conductivity decreases with temperature
9. Effective temp between surface and centerline
10. Peaking factor affects linear heat rate

### Issues
- **ISSUE #15 (INFO):** Fink (2000) coefficients and fuel geometry constants are highly specific to this module. Keeping them local (not in PlantConstants) is correct — these are fuel-design parameters, not plant-level constants.
- **ISSUE #16 (INFO):** ACTIVE_LENGTH_FT=12 duplicates PlantConstants.ACTIVE_HEIGHT=12. Minor, but fuel geometry belongs here.

---

## 4. FeedbackCalculator.cs — Combined Reactivity Feedback

### Purpose
Combines all reactivity feedback mechanisms (Doppler, MTC, Boron, Xenon, Rods) into total reactivity for point kinetics. Tracks individual contributions.

### Architecture
- **Role:** Feedback aggregator — wraps ReactorKinetics static functions with state tracking
- **Pattern:** Stateful wrapper around stateless ReactorKinetics functions
- **Key Design:** Feedback = deviation from reference conditions (HZP by default)

### Public Interface
- `FeedbackCalculator(initialFuelTemp_F, initialModTemp_F, initialBoron_ppm)` — Constructor
- `Update(fuelTemp_F, modTemp_F, boron_ppm, xenon_pcm, rodReactivity_pcm)` — Full update
- `UpdateTemperatures()` / `UpdateBoron()` / `UpdateXenon()` / `UpdateRods()` — Partial updates
- `EstimateCriticalBoron(fuelTemp, modTemp, xenon, rodWorth)` — Iterative critical boron search
- `BoronChangeForReactivity(pcm)` — Static utility
- `ReactivityToKeff(pcm)` / `KeffToReactivity(keff)` — Static converters
- Properties: DopplerFeedback_pcm, MTCFeedback_pcm, BoronFeedback_pcm, XenonFeedback_pcm, TotalFeedback_pcm, TotalReactivity_pcm, CurrentMTC, PowerDefect_pcm, IsStabilizing

### Key Physics
- **Doppler:** Delegates to `ReactorKinetics.DopplerReactivity(ΔT_fuel, T_ref)` — uses √T model
- **MTC:** Delegates to `ReactorKinetics.ModeratorTempCoefficient(boron_ppm)` — boron-dependent
- **Boron:** Delegates to `ReactorKinetics.BoronReactivity(Δppm)` — linear α_B = -9 pcm/ppm
- **Xenon:** Pass-through (externally calculated)
- **Total:** Σ(Doppler + MTC + Boron + Xenon) + Rods = TotalReactivity
- **Critical Boron Search:** 5-iteration Newton method accounting for boron-dependent MTC

### Constants (Local)
| Constant | Value | Cross-ref |
|----------|-------|-----------|
| REF_FUEL_TEMP_F | 557 | PlantConstants.T_AVG_NO_LOAD |
| REF_MOD_TEMP_F | 557 | PlantConstants.T_AVG_NO_LOAD |
| REF_BORON_PPM | 1500 | — (typical HZP critical boron BOL) |
| ALPHA_DOPPLER | -100 pcm/√°R | PlantConstants.DOPPLER_COEFF=-100 |
| ALPHA_BORON | -9 pcm/ppm | PlantConstants.BORON_WORTH=-9 |

### Dependencies
- `ReactorKinetics` (static: DopplerReactivity, ModeratorTempCoefficient, ModeratorReactivity, BoronReactivity)
- `PlantConstants` (T_AVG, T_AVG_NO_LOAD — via constants only)

### Validation
8 tests in `ValidateCalculations()`:
1. Reference conditions → zero feedback
2. Fuel temp increase → negative Doppler
3. High boron + mod temp increase → positive MTC
4. Low boron + mod temp increase → negative MTC
5. Boron increase → negative reactivity (~-900 pcm for 100 ppm)
6. Total reactivity includes rod contribution
7. BoronChangeForReactivity: -900 pcm → +100 ppm
8. Keff conversion: 0 pcm → 1.0, 650 pcm → ~1.00654

### Issues
- **ISSUE #17 (INFO):** ALPHA_DOPPLER and ALPHA_BORON duplicate PlantConstants.DOPPLER_COEFF and BORON_WORTH. Currently match. Module-local copies are defensible since FeedbackCalculator is the primary consumer.

---

## 5. PowerCalculator.cs — Neutron Power to Thermal Power Conversion

### Purpose
Models thermal lag between neutron power (instantaneous from kinetics) and thermal power (lagged by fuel heat capacity). Provides nuclear instrumentation range indication and reactor period/startup rate.

### Architecture
- **Role:** Power conversion with instrumentation modeling
- **Pattern:** Three-tier lag chain: Neutron → Thermal (τ=7s) → Indicated (τ=0.5s)

### Public Interface
- `PowerCalculator(initialPower_frac)` — Constructor
- `Update(neutronPower_frac, dt_sec)` — Apply lag chain
- `SetPower(power_frac)` — Direct set (no lag, for initialization)
- Properties: NeutronPower, ThermalPower, IndicatedPower, StartupRate_DPM, ReactorPeriod_sec, OverpowerAlarm, range flags

### Key Physics
- **Fuel thermal lag:** τ=7.0s first-order
- **Detector lag:** τ=0.5s first-order
- **Startup Rate:** SUR = (dP/dt)/(P × ln(10)) × 60 DPM
- **Reactor Period:** T = P/(dP/dt)
- **Range indication:** Source/Intermediate/Power thresholds
- **1/M plot support:** CalculateMultiplication, EstimateCriticalRodPosition

### Constants (Local)
| Constant | Value | Cross-ref |
|----------|-------|-----------|
| FUEL_THERMAL_TAU | 7.0 s | FuelAssembly.FUEL_THERMAL_TAU_SEC=7 |
| NOMINAL_POWER_MWT | 3411 | PlantConstants.THERMAL_POWER_MWT |
| OVERPOWER_LIMIT | 1.18 | ReactorCore.OVERPOWER_TRIP |

### Dependencies
- None (self-contained)

### Validation: 7 tests

### Issues
- **ISSUE #18 (LOW):** NOMINAL_POWER_MWT=3411 duplicates PlantConstants.THERMAL_POWER_MWT
- **ISSUE #19 (LOW):** FUEL_THERMAL_TAU=7.0 duplicates FuelAssembly.FUEL_THERMAL_TAU_SEC=7

---

## 6. ReactorController.cs — Unity MonoBehaviour Interface

### Purpose
Unity MonoBehaviour bridge. Pure coordinator — no physics calculations. Handles time compression, operator interface, automatic rod control, power ascension.

### Architecture
- **Pattern:** MonoBehaviour wrapping ReactorCore
- **Time Compression:** simDt = realDt × TimeCompression, subdivided for stability
- **Tavg Program:** Linear 557°F (0%) → 588°F (100%)
- **Auto Rod Control:** Deadband ±1.5°F
- **Mode State Machine:** ColdShutdown → HZP → PowerAscension → PowerOperation → Tripped

### Dependencies
- `ReactorCore` (owned instance)
- `UnityEngine`

### Validation: 5 tests (Editor only)

### Issues
- **ISSUE #20 (MEDIUM):** Power ascension uses rough 1000 pcm/fraction estimate — should use EstimatePowerDefect()
- **ISSUE #21 (LOW):** TimeCompression (up to 10000×) vs TimeAcceleration (up to 10×) — separate systems
- **ISSUE #22 (INFO):** Phase 1/Phase 2 time systems independent by design

---

## 7. ReactorSimEngine.cs — Scenario Management

### Purpose
Manages scenarios (startup, load follow, trip recovery, free play) with milestone tracking, scoring, completion detection. Pure coordination.

### Dependencies
- `ReactorController` (MonoBehaviour reference)
- `UnityEngine`

### Validation: 3 tests (Editor only)

### Issues
- **ISSUE #23 (LOW):** FindObjectOfType deprecated in Unity 6.3
- **ISSUE #24 (INFO):** Xenon init -2800 pcm within PlantConstants range
- **ISSUE #25 (INFO):** Scoring is game-design, no physics validation needed

---

## Cross-Module Dependency Map

```
ReactorKinetics (static)  ←── ReactorCore, FeedbackCalculator
PlantConstants ←── ReactorCore, FuelAssembly, FeedbackCalculator

ReactorCore ──┬── FuelAssembly (2 instances)
              ├── ControlRodBank (1 instance)
              ├── PowerCalculator (1 instance)
              └── FeedbackCalculator (1 instance)

ReactorController ──── ReactorCore (owned)
ReactorSimEngine ──── ReactorController (referenced)
```

---

## Validation Summary

| Module | Tests |
|--------|-------|
| ReactorCore | 6 |
| ControlRodBank | 8 |
| FuelAssembly | 10 |
| FeedbackCalculator | 8 |
| PowerCalculator | 7 |
| ReactorController | 5 |
| ReactorSimEngine | 3 |
| **Total** | **47** |

---

## Issues Register (Stage 1E)

### MEDIUM (3)
| # | Module | Issue |
|---|--------|-------|
| 9 | ReactorCore | ReactorKinetics 9-function dependency needs interface verification |
| 12 | ControlRodBank | Duplicate constants from PlantConstants |
| 20 | ReactorController | Power ascension linear estimate vs EstimatePowerDefect() |

### LOW (7)
| # | Module | Issue |
|---|--------|-------|
| 8 | ReactorCore | Trip setpoints local (defensible) |
| 10 | ReactorCore | Xenon dt/3600 magic number |
| 13 | ControlRodBank | Simplified rod drop model |
| 18 | PowerCalculator | NOMINAL_POWER_MWT duplicate |
| 19 | PowerCalculator | FUEL_THERMAL_TAU duplicate |
| 21 | ReactorController | Phase 1/2 time systems separate |
| 23 | ReactorSimEngine | Deprecated FindObjectOfType |

### INFO (8)
| # | Module | Note |
|---|--------|------|
| 11 | ReactorCore | Flow min 0.03 matches PlantConstants |
| 14 | ControlRodBank | Bank worths are typical/representative |
| 15 | FuelAssembly | Fuel constants correctly local |
| 16 | FuelAssembly | ACTIVE_LENGTH matches PlantConstants |
| 17 | FeedbackCalculator | ALPHA values match PlantConstants |
| 22 | ReactorController | Independent time systems by design |
| 24 | ReactorSimEngine | Xenon init within range |
| 25 | ReactorSimEngine | Scoring is game-design |

---

## Cumulative Audit Progress

| Stage | Files | Status |
|-------|-------|--------|
| 1A: Constants & Properties | 6 | Complete |
| 1B: Heat & Flow Physics | 4 | Complete |
| 1C: Pressurizer & Kinetics | 4 | Complete |
| 1D: Support Systems | 6 | Complete |
| **1E: Reactor Core** | **7** | **Complete** |
| 1F: UI/Visualization | ~10 | Pending |
| 1G: Tests & Validation | ~6 | Pending |

**Files audited: 27/43 (63%)**  
**Cumulative tests identified: 68**
