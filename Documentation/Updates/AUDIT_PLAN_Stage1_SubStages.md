# Stage 1: Complete File Inventory & Architecture Mapping
## Sub-Stage Plan

**Version:** 1.0.0.0
**Date:** 2026-02-06
**Purpose:** Break Stage 1 into manageable sessions due to context window limits

---

## FILE INVENTORY SUMMARY

### Total Source Files: 43 .cs files (~983 KB total)

| Directory | Files | Combined Size | Priority |
|-----------|-------|---------------|----------|
| Physics/ | 20 | 413 KB | **GOLD STANDARD - PRIMARY** |
| Reactor/ | 7 | 182 KB | **GOLD STANDARD - PRIMARY** |
| Validation/ | 3 | 138 KB | Integration Layer |
| Tests/ | 6 | 143 KB | Test Infrastructure |
| UI/ | 9 | 105 KB | Presentation Layer |

---

## STAGE 1 SUB-STAGES

### Sub-Stage 1A: Constants & Properties Foundation (Session 1)
**Scope:** Core reference values and thermodynamic property modules
**Files (6 files, ~158 KB):**

| File | Size | Description |
|------|------|-------------|
| PlantConstants.cs | 49 KB | Westinghouse 4-Loop reference values |
| PlantConstantsHeatup.cs | 11 KB | Heatup-specific constants (POTENTIAL DUPLICATE) |
| WaterProperties.cs | 27 KB | NIST water/steam property lookups |
| SteamThermodynamics.cs | 17 KB | Two-phase calculations |
| ThermalMass.cs | 16 KB | Heat capacity calculations |
| ThermalExpansion.cs | 16 KB | Volume change with temperature |

**Key Questions:**
- What's the relationship between PlantConstants and PlantConstantsHeatup?
- Are there duplicate constants between files?
- Do WaterProperties and SteamThermodynamics use consistent formulations?

---

### Sub-Stage 1B: Heat & Flow Physics (Session 2)
**Scope:** Heat transfer and fluid flow modules
**Files (4 files, ~99 KB):**

| File | Size | Description |
|------|------|-------------|
| HeatTransfer.cs | 28 KB | LMTD, UA, enthalpy transport |
| FluidFlow.cs | 19 KB | Pump dynamics, surge line, natural circulation |
| LoopThermodynamics.cs | 13 KB | RCS loop calculations |
| CoupledThermo.cs | 28 KB | **CRITICAL** P-T-V iterative solver |

**Key Questions:**
- Does CoupledThermo properly iterate (Gap #1)?
- Does HeatTransfer handle enthalpy deficit correctly (Gap #10)?
- Does FluidFlow model surge line resistance (Gap #9)?

---

### Sub-Stage 1C: Pressurizer & Kinetics (Session 3)
**Scope:** Pressurizer dynamics and reactor kinetics
**Files (4 files, ~117 KB):**

| File | Size | Description |
|------|------|-------------|
| PressurizerPhysics.cs | 37 KB | Flash, spray, heater, wall effects (Gaps #2-8) |
| SolidPlantPressure.cs | 28 KB | Solid water operations (heatup) |
| ReactorKinetics.cs | 24 KB | Point kinetics and feedback |
| RVLISPhysics.cs | 9 KB | Reactor Vessel Level Indication |

**Key Questions:**
- Does PressurizerPhysics implement all 7 gap fixes?
- How does SolidPlantPressure interact with PressurizerPhysics?
- Are reactivity coefficients validated against FSAR?

---

### Sub-Stage 1D: Support Systems (Session 4)
**Scope:** CVCS, RCP, VCT, and time management
**Files (5 files, ~75 KB):**

| File | Size | Description |
|------|------|-------------|
| CVCSController.cs | 24 KB | Charging/letdown/boron control |
| VCTPhysics.cs | 16 KB | Volume Control Tank physics |
| RCSHeatup.cs | 16 KB | RCS heatup process control |
| RCPSequencer.cs | 11 KB | RCP start/stop sequencing |
| TimeAcceleration.cs | 12 KB | Time compression for simulation |

**Key Questions:**
- Does CVCS properly model boron transport delay (5-15 min)?
- How does VCT track mass/level during solid plant ops?
- Does RCSHeatup call physics modules or duplicate calculations?

---

### Sub-Stage 1E: Reactor Core Modules (Session 5)
**Scope:** Core reactor simulation modules
**Files (7 files, ~182 KB):**

| File | Size | Description |
|------|------|-------------|
| FuelAssembly.cs | 32 KB | Fuel thermal profiles |
| ReactorSimEngine.cs | 30 KB | Scenario/sequence manager |
| ReactorController.cs | 30 KB | Unity interface with time compression |
| ControlRodBank.cs | 29 KB | 8-bank control rod system |
| ReactorCore.cs | 24 KB | Central core state manager |
| FeedbackCalculator.cs | 20 KB | Temperature/power feedback |
| PowerCalculator.cs | 18 KB | Neutron to thermal conversion |

**Key Questions:**
- Does ReactorCore properly integrate all physics modules?
- Does ReactorSimEngine duplicate physics or properly delegate?
- Is the control rod S-curve worth calculation validated?

---

### Sub-Stage 1F: Validation & Heatup Engine (Session 6)
**Scope:** Integration validation layer
**Files (4 files, ~157 KB):**

| File | Size | Description |
|------|------|-------------|
| HeatupSimEngine.cs | 67 KB | **LARGEST FILE** - Heatup simulation orchestrator |
| HeatupValidationVisual.cs | 53 KB | Runtime validation display |
| HeatupValidation.cs | 18 KB | Validation checkpoint logic |
| AlarmManager.cs | 10 KB | Alarm state management |

**Key Questions:**
- Does HeatupSimEngine properly delegate to physics modules?
- Or does it contain duplicate/shadow calculations?
- How does data flow between HeatupSimEngine and VCTPhysics?

---

### Sub-Stage 1G: Tests & UI (Session 7)
**Scope:** Test infrastructure and UI components
**Files (15 files, ~248 KB):**

**Tests (6 files, ~143 KB):**
| File | Size | Description |
|------|------|-------------|
| Phase2TestRunner.cs | 39 KB | 95 reactor core tests |
| HeatupIntegrationTests.cs | 38 KB | 9 integration tests |
| Phase1TestRunner.cs | 35 KB | 112 physics tests |
| IntegrationTests.cs | 19 KB | Cross-module tests |
| Phase2UnityTestRunner.cs | 11 KB | Unity menu integration |
| UnityTestRunner.cs | 0.4 KB | Basic test harness |

**UI (9 files, ~105 KB):**
| File | Size | Description |
|------|------|-------------|
| MosaicBoard.cs | 20 KB | Main control board |
| MosaicBoardBuilder.cs | 18 KB | Board construction |
| MosaicControlPanel.cs | 15 KB | Control panel widgets |
| MosaicAlarmPanel.cs | 12 KB | Alarm display |
| MosaicIndicator.cs | 11 KB | Status indicators |
| MosaicRodDisplay.cs | 11 KB | Rod position display |
| MosaicGauge.cs | 11 KB | Analog gauges |
| MosaicBoardSetup.cs | 7 KB | Setup utilities |
| MosaicTypes.cs | 1 KB | Type definitions |

**Key Questions:**
- Do tests properly exercise all physics modules?
- Are there gaps in test coverage?
- Does UI correctly reflect physics state?

---

## KNOWN ISSUES TO INVESTIGATE

### 1. Potential Duplicate Constants
- `PlantConstants.cs` (49 KB) vs `PlantConstantsHeatup.cs` (11 KB)
- Need line-by-line comparison for conflicts

### 2. Potential Calculation Duplication
- `HeatupSimEngine.cs` (67 KB) is suspiciously large
- May contain shadow physics calculations instead of delegating

### 3. Integration Layer Concerns
- How does `HeatupSimEngine` connect to:
  - `SolidPlantPressure`
  - `VCTPhysics`
  - `CVCSController`
  - `RCSHeatup`

### 4. Physics Module Dependencies
Need to map which modules call which:
- Does `ReactorCore` use `ReactorKinetics`?
- Does `PressurizerPhysics` use `CoupledThermo`?
- Is `WaterProperties` the single source for all thermodynamics?

---

## SESSION WORKFLOW

Each sub-stage session should:

1. **Read all files in scope** (complete file contents)
2. **Document for each file:**
   - Public interface (methods, properties)
   - Dependencies (what it imports/calls)
   - Dependents (what calls it)
   - Constants/parameters defined
   - Any hardcoded values (potential validation targets)
3. **Identify issues:**
   - Duplications
   - Inconsistencies
   - Missing integrations
   - Hardcoded values that should be in PlantConstants
4. **Create mapping artifact:**
   - Call graph for that sub-stage
   - Cross-references to other stages

---

## OUTPUT ARTIFACTS

At the end of Stage 1 (all sub-stages complete):

1. **AUDIT_Stage1A_Constants_Properties.md** - Constants inventory
2. **AUDIT_Stage1B_Heat_Flow.md** - Heat/flow module analysis
3. **AUDIT_Stage1C_Pressurizer_Kinetics.md** - Pressurizer analysis
4. **AUDIT_Stage1D_Support_Systems.md** - CVCS/VCT/RCP analysis
5. **AUDIT_Stage1E_Reactor_Core.md** - Core module analysis
6. **AUDIT_Stage1F_Validation_Engine.md** - Integration analysis
7. **AUDIT_Stage1G_Tests_UI.md** - Test coverage analysis
8. **AUDIT_Stage1_MASTER_ARCHITECTURE.md** - Complete system map

---

## READY TO BEGIN

When you're ready, say "Begin Sub-Stage 1A" and I will:
1. Read all 6 files in Sub-Stage 1A
2. Document their complete interfaces
3. Map dependencies
4. Identify issues
5. Produce AUDIT_Stage1A_Constants_Properties.md

