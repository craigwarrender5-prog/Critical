# AUDIT Stage 1G: Tests & UI

**Date:** 2026-02-06
**Auditor:** Claude (Stage 1 — File Inventory & Architecture Mapping)
**Scope:** 6 test files (~248 KB) + 9 UI files (~105 KB) = 15 files (~353 KB)

---

## FILES ANALYZED

### Tests (6 files, ~248 KB)

| # | File | Size | Lines (approx) | Status |
|---|------|------|-----------------|--------|
| 32 | Phase1TestRunner.cs | 39 KB | ~620 | GOLD STANDARD |
| 33 | HeatupIntegrationTests.cs | 38 KB | ~530 | GOLD STANDARD |
| 34 | Phase2TestRunner.cs | 35 KB | ~660 | GOLD STANDARD |
| 35 | IntegrationTests.cs | 19 KB | ~380 | GOLD STANDARD |
| 36 | Phase2UnityTestRunner.cs | 11 KB | ~250 | GOLD STANDARD |
| 37 | UnityTestRunner.cs | 0.4 KB | ~15 | GOLD STANDARD (minimal) |

### UI (9 files, ~105 KB)

| # | File | Size | Lines (approx) | Status |
|---|------|------|-----------------|--------|
| 38 | MosaicBoard.cs | 20 KB | ~480 | GOLD STANDARD |
| 39 | MosaicBoardBuilder.cs | 18 KB | ~400 | GOLD STANDARD |
| 40 | MosaicControlPanel.cs | 15 KB | ~320 | GOLD STANDARD |
| 41 | MosaicAlarmPanel.cs | 12 KB | ~280 | GOLD STANDARD |
| 42 | MosaicIndicator.cs | 11 KB | ~260 | GOLD STANDARD |
| 43 | MosaicRodDisplay.cs | 11 KB | ~260 | GOLD STANDARD |
| 44 | MosaicGauge.cs | 11 KB | ~280 | GOLD STANDARD |
| 45 | MosaicBoardSetup.cs | 7 KB | ~180 | GOLD STANDARD |
| 46 | MosaicTypes.cs | 1 KB | ~40 | GOLD STANDARD |

**Total: 15 files, ~353 KB, ~4,955 lines**

---

## TEST INFRASTRUCTURE ANALYSIS

### Test Coverage Summary

| Test Suite | File | Tests | Scope |
|------------|------|-------|-------|
| Phase 1 Physics | Phase1TestRunner.cs | 112 | All 10 physics modules |
| Phase 1 Integration | IntegrationTests.cs | 7 | Cross-module coupling |
| Phase D Heatup Integration | HeatupIntegrationTests.cs | 9 | Heatup cross-module (Phase A/B/C bugs) |
| Phase 2 Reactor Core | Phase2TestRunner.cs | 95 | 5 reactor modules + 4 integration suites |
| **Total** | | **223** | |

### Unity Test Runners

| Runner | Purpose | Trigger |
|--------|---------|---------|
| UnityTestRunner.cs | Phase 1 on Start | MonoBehaviour.Start() |
| Phase2UnityTestRunner.cs | Phase 2 via menu or key | Menu: Critical > Run Phase 2 Tests / F5 |
| Phase2UnityTestRunner.cs | Quick Smoke Test | Menu: Critical > Quick Smoke Test |
| Phase2UnityTestRunner.cs | Reference Validation | Menu: Critical > Validate Reference Values |

---

## FILE-BY-FILE ANALYSIS: TESTS

---

### 32. Phase1TestRunner.cs — GOLD STANDARD

**Purpose:** Master test runner for all Phase 1 physics validation. 121 tests total (112 original + 9 heatup integration). Works in both Unity (Debug.Log) and standalone .NET (Console.WriteLine).

**Test Suites (12 categories):**

| Suite | Tests | Module Under Test | Key Validations |
|-------|-------|-------------------|-----------------|
| PlantConstants | 5 | PlantConstants.cs | T_AVG consistency, RCS flow, PZR volumes, derived constants |
| WaterProperties | 18 | WaterProperties.cs | Psat, Tsat, density, enthalpy, hfg, subcooling, NIST validation |
| SteamThermodynamics | 10 | SteamThermodynamics.cs | Quality, void fraction, two-phase density, phase detection |
| ThermalMass | 6 | ThermalMass.cs | Q=mCpΔT, equilibrium, PZR wall capacity, first-order response |
| ThermalExpansion | 6 | ThermalExpansion.cs | β range, κ range, dP/dT, surge volume, coupled vs uncoupled |
| HeatTransfer | 6 | HeatTransfer.cs | Enthalpy deficit, condensing HTC, LMTD, self-validation |
| FluidFlow | 8 | FluidFlow.cs | RCP flow, coastdown, natural circ, surge flow, affinity laws |
| ReactorKinetics | 10 | ReactorKinetics.cs | β_total, Doppler, MTC, boron, xenon, decay heat, precursors |
| PressurizerPhysics | 24 | PressurizerPhysics.cs | Flash, spray, wall cond, rainout, heater lag, solid state, bubble |
| CoupledThermo | 12 | CoupledThermo.cs | **Gap #1**: 10°F→60-80 psi, mass/volume conservation, convergence |
| Integration | 7 | Cross-module | Insurge/outsurge, feedback, coastdown, mass/energy conservation |
| Heatup Integration | 9 | Cross-module | Phase A/B/C bug regression, full mass balance |

**Architecture:**
- Self-contained with `Test(id, description, Func<bool>)` harness
- Reports pass/fail with formatted console output
- Exit gate: all 121 tests must pass before Phase 2 proceeds
- Delegates to `IntegrationTests.RunAllTests()` and `HeatupIntegrationTests.RunAllTests()` for integration suites

**Dependencies:** Critical.Physics (all modules), Critical.Physics.Tests (IntegrationTests, HeatupIntegrationTests)

**Issues:** None. Comprehensive coverage of all Phase 1 physics modules.

---

### 33. HeatupIntegrationTests.cs — GOLD STANDARD

**Purpose:** Cross-module integration tests targeting the three failure modes discovered during Phase D validation. These tests fill the structural gap between unit-level ValidateCalculations() and overnight simulation runs.

**Bug Classes Covered:**

| Phase | Bug | Root Cause | Tests |
|-------|-----|------------|-------|
| A | RCS mass frozen at 696,136 lb | SolidPlantPressure computed CVCS flows but engine never applied them to RCSWaterMass | HINT-01, HINT-02, HINT-09 |
| B | Surge flow zero during solid plant | SolidPlantPressure calculated temperatures but never computed SurgeFlow from thermal expansion | HINT-03, HINT-04 |
| C | VCT conservation tautological | VerifyMassConservation compared VCT change against itself | HINT-05, HINT-06, HINT-07 |
| All | Full chain verification | Complete SolidPlant→RCS→VCT integration | HINT-08 |

**Architecture:**
- Uses same `TestResult` / `IntegrationTestSummary` pattern as IntegrationTests.cs
- Each test simulates multi-step solid plant operation (100-200 steps at dt=1/360 hr)
- Tests explicitly document the integration step that was missing in the original bugs
- Regression-aware: notes explicitly call out "PHASE A/B/C REGRESSION" when failure patterns match known bugs

**Key Design Insight:** These tests prove that unit tests structurally cannot catch certain integration bugs. The unit test "does letdown flow increase?" passed perfectly — but nobody checked whether that flow actually removed mass from the RCS. This class of bug requires integration testing across module boundaries.

**Dependencies:** SolidPlantPressure, VCTPhysics, WaterProperties, ThermalMass, PlantConstants

**Issues:** None. Exemplary integration test design with clear documentation of each bug's root cause.

---

### 34. Phase2TestRunner.cs — GOLD STANDARD

**Purpose:** Validates all 5 reactor core modules (FuelAssembly, ControlRodBank, PowerCalculator, FeedbackCalculator, ReactorCore) plus 4 integration suites (Startup, Trip, Steady-State, Xenon). 95 tests total.

**Test Suites (9 categories):**

| Suite | Tests | Key Validations |
|-------|-------|-----------------|
| FuelAssembly | 15 | 17×17 geometry, Fink conductivity, gap model, Rowlands T_eff, melting margin |
| ControlRodBank | 15 | 228 steps, 72 steps/min, S-curve worth, trip drop, overlap, sequence |
| PowerCalculator | 12 | Thermal lag (τ=7s), detector lag (τ=0.5s), ranges, overpower, startup rate |
| FeedbackCalculator | 12 | Doppler (-100 pcm/√°R), MTC sign, boron (-9 pcm/ppm), xenon, keff conversion |
| ReactorCore | 15 | HZP init, rod withdrawal, trip, equilibrium at 100%, temperatures, xenon |
| Startup Sequence | 8 | Dilution, rod withdrawal, criticality, power increase, fuel/Tavg response |
| Trip Response | 6 | Subcriticality, rod insertion, power drop, decay heat, trip reset |
| Steady State | 6 | Power ±0.5%, Tavg ±2°F, Thot ±3°F, ΔT ±2°F, ρ ≈ 0 pcm |
| Xenon Transient | 6 | Equilibrium at 100%/50%/0%, rate direction, buildup after trip |

**Notable Test Fixes (v1.0.1.1):**
- **SS-04:** Changed from absolute power threshold (0.1%) to relative check (power increased AND supercritical). Original test was physically unrealistic — reaching 0.1% from source level in 20s requires ~250s at the given reactivity.
- **TR-06:** Increased post-trip cooldown from 28s to 100s. At 28s, delayed neutron precursor Group 1 still at 71% — power ~1% (marginal). At 100s, Group 1 at 28%, power ~0.2% (well below 1% threshold).

**Architecture:**
- `Test()` and `TestValue()` harnesses for boolean and tolerance-based assertions
- Well-documented reference sources per suite (Westinghouse FSAR, NRC HRTD, Fink 2000, FRAPCON-4)
- Explicit tolerances with physics justification in comments

**Dependencies:** All Physics/ and Reactor/ modules

**Issues:** None. SS-04 and TR-06 fixes (v1.0.1.1) corrected tests that were wrong, not physics.

---

### 35. IntegrationTests.cs — GOLD STANDARD

**Purpose:** 7 cross-module integration tests for Phase 1. Verifies critical physical coupling between modules.

**Tests:**

| Test | Name | Modules Tested | Key Criterion |
|------|------|----------------|---------------|
| INT-01 | Coupled Pressure Response | CoupledThermo | 10°F → 60-80 psi (**Gap #1 critical**) |
| INT-02 | Insurge Transient | PressurizerPhysics | Level increases during insurge |
| INT-03 | Outsurge with Flash | PressurizerPhysics | Flash > 0 during depressurization |
| INT-04 | Power Trip Feedback | ReactorKinetics | Doppler dominates (negative total feedback) |
| INT-05 | RCP Coastdown + Nat Circ | FluidFlow | Smooth transition, nat circ 12k-23k gpm |
| INT-06 | Mass Conservation | CoupledThermo | <0.1% error through multiple transients |
| INT-07 | Energy Conservation | PressurizerPhysics | Energy change ≈ heater input (30% tolerance with τ lag) |

**Also defines:** `TestResult` class and `IntegrationTestSummary` class reused by HeatupIntegrationTests.

**Issues:** None.

---

### 36. Phase2UnityTestRunner.cs — GOLD STANDARD

**Purpose:** Unity Editor integration wrapper providing:
1. MonoBehaviour with configurable run-on-start and key trigger (F5)
2. Editor menu: `Critical > Run Phase 2 Tests` (Ctrl+Shift+T)
3. Quick Smoke Test: 6 rapid instantiation checks
4. Reference Value Validation: Prints key Westinghouse parameters for visual verification

**Issues:**
- **ISSUE #34 (LOW):** Uses `FindObjectOfType<>()` — deprecated in Unity 6.3. Same as Issue #23 in ReactorSimEngine.

---

### 37. UnityTestRunner.cs — GOLD STANDARD (Minimal)

**Purpose:** Simplest possible Unity wrapper — attaches to a GameObject and runs Phase1TestRunner on Start.

**15 lines total.** No issues.

---

## TEST COVERAGE ANALYSIS

### Physics Modules vs. Test Coverage

| Module | Unit Tests | Integration Tests | Heatup Integration | Total |
|--------|-----------|-------------------|-------------------|-------|
| PlantConstants | 5 (PC-01→05) | — | — | 5 |
| WaterProperties | 18 (WP-01→18) | — | — | 18 |
| SteamThermodynamics | 10 (ST-01→10) | — | — | 10 |
| ThermalMass | 6 (TM-01→06) | — | — | 6 |
| ThermalExpansion | 6 (TE-01→06) | — | — | 6 |
| HeatTransfer | 6 (HT-01→06) | — | — | 6 |
| FluidFlow | 8 (FF-01→08) | INT-05 | — | 9 |
| ReactorKinetics | 10 (RK-01→10) | INT-04 | — | 11 |
| PressurizerPhysics | 24 (PZ-01→24) | INT-02, INT-03, INT-07 | — | 27 |
| CoupledThermo | 12 (CT-01→12) | INT-01, INT-06 | — | 14 |
| SolidPlantPressure | — | — | HINT-01→04, 08, 09 | 6 |
| VCTPhysics | — | — | HINT-05→08 | 4 |
| CVCSController | — | — | — | **0** |
| RCSHeatup | — | — | — | **0** |
| RCPSequencer | — | — | — | **0** |
| AlarmManager | — | — | — | **0** |
| TimeAcceleration | — | — | — | **0** |
| LoopThermodynamics | — | — | — | **0** |
| RVLISPhysics | — | — | — | **0** |
| **Subtotal Phase 1** | **105** | **7** | **9** | **121** |

| Reactor Module | Unit Tests | Integration Tests | Total |
|----------------|-----------|-------------------|-------|
| FuelAssembly | 15 (FA-01→15) | — | 15 |
| ControlRodBank | 15 (CR-01→15) | — | 15 |
| PowerCalculator | 12 (PC-01→12) | — | 12 |
| FeedbackCalculator | 12 (FC-01→12) | — | 12 |
| ReactorCore | 15 (RC-01→15) | SS-01→08, TR-01→06, ST-01→06, XT-01→06 | 41 |
| **Subtotal Phase 2** | **69** | **26** | **95** |

| **Grand Total** | **174** | **33** | **9** | **216** |

*Note: 7 additional tests come from the Phase1TestRunner calling IntegrationTests, bringing the Phase 1 total to 121 and overall to 223 as counted by the runners.*

### Coverage Gaps

**Modules with ZERO dedicated test coverage in test runners:**

| Module | Has ValidateCalculations()? | Notes |
|--------|---------------------------|-------|
| CVCSController | Yes (7 internal tests) | Tested indirectly via HeatupSimEngine, but no Phase1/Phase2 runner coverage |
| RCSHeatup | Yes (6 internal tests) | Tested indirectly via integration simulation |
| RCPSequencer | Yes (8 internal tests) | Tested indirectly via HeatupSimEngine |
| AlarmManager | No | Pure threshold logic — low risk but no test coverage |
| TimeAcceleration | No | Simple multiplier logic — low risk |
| LoopThermodynamics | Yes (6 internal tests) | Not exercised in any test runner |
| RVLISPhysics | Yes (5 internal tests) | Not exercised in any test runner |

**Important nuance:** Most of these modules DO have self-validation methods (ValidateCalculations()) that are called internally. What's missing is their inclusion in the Phase1TestRunner or Phase2TestRunner execution chains. The internal tests exist but are not wired into the gate exit criteria.

**Recommendation:** Wire the remaining ValidateCalculations() methods into Phase1TestRunner:
- CVCSController.ValidateCalculations() → 7 tests
- RCSHeatup.ValidateCalculations() → 6 tests
- RCPSequencer.ValidateCalculations() → 8 tests
- LoopThermodynamics.ValidateCalculations() → 6 tests
- RVLISPhysics.ValidateCalculations() → 5 tests

This would add 32 tests to the Phase 1 runner (121 → 153) and ensure all modules with self-validation are part of the exit gate.

---

## FILE-BY-FILE ANALYSIS: UI

---

### 38. MosaicBoard.cs — GOLD STANDARD

**Purpose:** Central controller for the reactor control room visual display. Manages all gauge, indicator, and display components. Provides data binding to ReactorController.

**Architecture:**
- Singleton pattern with static `Instance`
- Component registration via `IMosaicComponent` interface — all child components auto-register
- Rate-limited updates (configurable, default 10 Hz)
- Value smoothing via `GetSmoothedValue()` with Lerp
- Data access via `GetValue(GaugeType)` switch expression — maps 14 gauge types to ReactorController properties
- Alarm management with flash state, acknowledge, horn audio
- Color coding with configurable theme (Normal/Warning/Alarm/Trip)
- Control passthrough: WithdrawRods(), InsertRods(), Trip(), etc.

**Alarm Thresholds (hardcoded in GetAlarmState):**

| GaugeType | Warning | Alarm | Source Verification |
|-----------|---------|-------|---------------------|
| NeutronPower | >105% | >109% | ✅ Matches HIGH_FLUX_TRIP=1.09 |
| Tavg | >595°F or <550°F | >605°F or <540°F | ✅ Reasonable for HFP |
| Thot | >630°F | >650°F | ✅ Near Tsat at 2250 psia |
| FuelCenterline | >3500°F | >4000°F | ✅ Below melting (5189°F) |
| TotalReactivity | >100 pcm | >500 pcm | ✅ |
| StartupRate | >1.0 DPM | >2.0 DPM | ✅ Tech Spec compliant |
| ReactorPeriod | <30s | <10s | ✅ |
| BankDPosition | <30 steps | <10 steps | ✅ Rod bottom concern |
| FlowFraction | <90% | <87% | ✅ Matches LOW_FLOW_TRIP |

**Dependencies:** ReactorController, ReactorSimEngine, UnityEngine.UI

**Issues:**
- **ISSUE #35 (LOW):** Uses `FindObjectOfType<>()` in Start() — deprecated in Unity 6.3
- **ISSUE #36 (INFO):** Alarm thresholds hardcoded in MosaicBoard rather than referencing PlantConstants or AlarmManager. These are UI-specific display thresholds (different from safety system setpoints), so keeping them local is defensible. However, they should be cross-checked against AlarmManager setpoints for consistency.

---

### 39. MosaicBoardBuilder.cs — GOLD STANDARD

**Purpose:** Editor tool for automated scene creation. Creates complete Mosaic Board UI hierarchy via Unity menu (`Critical > Create Mosaic Board`). Generates Canvas, sections, gauges, indicators, rod display, alarm panel, and control panel programmatically.

**Architecture:** `#if UNITY_EDITOR` guarded static methods via `[MenuItem]`. Creates Unity GameObjects with proper component wiring.

**Dependencies:** UnityEditor, UnityEngine.UI

**Issues:** None. Editor-only tool with no runtime impact.

---

### 40. MosaicControlPanel.cs — GOLD STANDARD

**Purpose:** Operator control interface with buttons for rod control (withdraw/insert/stop), reactor trip, time compression, boron control, and scenario management. Implements `IMosaicComponent`.

**Dependencies:** MosaicBoard (via IMosaicComponent), ReactorController (via MosaicBoard passthrough), UnityEngine.UI

**Issues:** None. Pure UI with no physics.

---

### 41. MosaicAlarmPanel.cs — GOLD STANDARD

**Purpose:** Scrolling alarm annunciator display with severity color coding, flash animation for unacknowledged alarms, and acknowledge button. Implements `IMosaicComponent` and `IAlarmFlashReceiver`.

**Dependencies:** MosaicBoard, UnityEngine.UI

**Issues:** None.

---

### 42. MosaicIndicator.cs — GOLD STANDARD

**Purpose:** Binary status indicator lights (on/off) with configurable colors and flash mode. Supports 20+ condition types (ReactorTripped, Critical, Subcritical, Overpower, LowFlow, etc.). Implements `IMosaicComponent` and `IAlarmFlashReceiver`.

**Dependencies:** MosaicBoard, ReactorController, UnityEngine.UI

**Issues:** None.

---

### 43. MosaicRodDisplay.cs — GOLD STANDARD

**Purpose:** Control rod bank position visualization with vertical bars, digital readouts, bank labels, and insertion limit markers. Supports single-bank or all-8-bank display mode. Implements `IMosaicComponent` and `IAlarmFlashReceiver`. Reference: Westinghouse Rod Position Indication System (RPIS).

**Dependencies:** MosaicBoard, ReactorController, ControlRodBank, UnityEngine.UI

**Issues:** None.

---

### 44. MosaicGauge.cs — GOLD STANDARD

**Purpose:** Individual gauge display with analog dial, digital readout, color-coded alarm states, and configurable ranges. Supports all 14 GaugeType values. Implements `IMosaicComponent` and `IAlarmFlashReceiver`.

**Dependencies:** MosaicBoard, UnityEngine.UI

**Issues:** None.

---

### 45. MosaicBoardSetup.cs — GOLD STANDARD

**Purpose:** Runtime initialization and wiring for MosaicBoard. Creates ReactorController if missing, initializes to specified scenario (HZP, PowerOperation, ColdShutdown, Startup), and wires all component references.

**Dependencies:** MosaicBoard, ReactorController, ReactorSimEngine, UnityEngine

**Issues:** None.

---

### 46. MosaicTypes.cs — GOLD STANDARD

**Purpose:** Shared type definitions — `GaugeType` enum (14 values) and `AlarmState` enum (Normal, Warning, Alarm, Trip).

**Dependencies:** None.

**Issues:** None.

---

## CROSS-MODULE DEPENDENCY MAP (Stage 1G)

```
╔═══════════════════════════════════════════════════════════════╗
║                        TEST INFRASTRUCTURE                    ║
╠═══════════════════════════════════════════════════════════════╣
║                                                               ║
║  UnityTestRunner ──► Phase1TestRunner                         ║
║                        ├── 10 module validation suites        ║
║                        ├── IntegrationTests (7)               ║
║                        └── HeatupIntegrationTests (9)         ║
║                                                               ║
║  Phase2UnityTestRunner ──► Phase2TestRunner                   ║
║                              ├── 5 module validation suites   ║
║                              └── 4 integration suites         ║
╚═══════════════════════════════════════════════════════════════╝

╔═══════════════════════════════════════════════════════════════╗
║                        UI INFRASTRUCTURE                      ║
╠═══════════════════════════════════════════════════════════════╣
║                                                               ║
║  MosaicBoardBuilder (Editor) ──creates──► Scene hierarchy     ║
║  MosaicBoardSetup (Runtime)  ──wires───► Component refs       ║
║                                                               ║
║  MosaicBoard (Controller)                                     ║
║    ├── MosaicGauge[]        (IMosaicComponent)                ║
║    ├── MosaicIndicator[]    (IMosaicComponent)                ║
║    ├── MosaicRodDisplay     (IMosaicComponent)                ║
║    ├── MosaicAlarmPanel     (IMosaicComponent)                ║
║    ├── MosaicControlPanel   (IMosaicComponent)                ║
║    └── MosaicTypes          (shared enums)                    ║
║         │                                                     ║
║         ▼                                                     ║
║    ReactorController ──► ReactorCore ──► Physics modules      ║
╚═══════════════════════════════════════════════════════════════╝
```

---

## ISSUES REGISTER (Stage 1G)

### MEDIUM Priority (1)

**#34:** **32 internal ValidateCalculations() tests not wired into test runners.** The following modules have self-validation methods that exist in the source but are never called by Phase1TestRunner or Phase2TestRunner: CVCSController (7), RCSHeatup (6), RCPSequencer (8), LoopThermodynamics (6), RVLISPhysics (5). These tests run when called manually but are not part of the automated exit gate. Recommendation: Add a new suite to Phase1TestRunner that calls all remaining ValidateCalculations() methods.

### LOW Priority (2)

**#35:** Phase2UnityTestRunner and MosaicBoard use `FindObjectOfType<>()` — deprecated in Unity 6.3. Should migrate to `FindAnyObjectByType<>()`.

**#36:** MosaicBoard.GetAlarmState() alarm thresholds are independent of AlarmManager setpoints. These serve different purposes (UI display vs. safety system) but should be cross-referenced for consistency.

### INFO (1)

**#37:** MosaicBoard UI architecture is clean. All 9 components implement `IMosaicComponent` interface. Board is a pure data broker — reads from ReactorController, distributes to components. Zero physics in the UI layer.

---

## VALIDATION SUMMARY (ALL STAGES)

### Complete Test Inventory

| Source | Tests |
|--------|-------|
| Phase1TestRunner (module suites) | 105 |
| IntegrationTests | 7 |
| HeatupIntegrationTests | 9 |
| Phase2TestRunner (module + integration) | 95 |
| **Test Runner Total** | **216** |
| Module-internal ValidateCalculations() (not in runners) | ~32 |
| **Grand Total (all known tests)** | **~248** |

### Module-Internal Tests NOT in Runners

| Module | Method | Internal Tests |
|--------|--------|---------------|
| CVCSController | ValidateCalculations() | 7 |
| RCSHeatup | ValidateCalculations() | 6 |
| RCPSequencer | ValidateCalculations() | 8 |
| LoopThermodynamics | ValidateCalculations() | 6 |
| RVLISPhysics | ValidateCalculations() | 5 |
| **Total unwired** | | **32** |

---

## COMPLETE AUDIT PROGRESS (STAGE 1 FINISHED)

| Stage | Scope | Files | Issues | Status |
|-------|-------|-------|--------|--------|
| 1A | Constants & Properties | 6 | 3H, 2M, 2L | ✅ Complete |
| 1B | Heat & Flow Physics | 4 | 0 | ✅ Complete |
| 1C | Pressurizer & Kinetics | 4 | 0 | ✅ Complete |
| 1D | Support Systems | 6 | 2M, 3L, 2I | ✅ Complete |
| 1E | Reactor Core | 7 | 3M, 7L, 8I | ✅ Complete |
| 1F | Validation & Heatup Engine | 4 | 1H, 1M, 2L, 4I | ✅ Complete |
| **1G** | **Tests & UI** | **15** | **1M, 2L, 1I** | **✅ Complete** |
| **TOTAL** | | **46** | **2H, 9M, 16L, 15I** | **STAGE 1 COMPLETE** |

### Files Audited: 46/43 (>100% — includes AlarmManager counted in both 1D and 1F)

**Actual unique source files: 43 as per original inventory. All files audited.**

---

## MASTER ISSUE SUMMARY (ALL STAGES)

### HIGH Priority (2)
| # | Stage | Module | Issue |
|---|-------|--------|-------|
| — | 1A | PlantConstantsHeatup | Conflicting values (MAX_HEATUP_RATE, MIN_RCP_PRESSURE) and potential bug (NORMAL_OPERATING_PRESSURE=2235) |
| 26 | 1F | HeatupValidation.cs | SUPERSEDED legacy file with inline physics — risk of accidental use |

### MEDIUM Priority (9)
| # | Stage | Module | Issue |
|---|-------|--------|-------|
| — | 1A | PlantConstantsHeatup | PZR level program covers different range than PlantConstants |
| 1 | 1D | VCTPhysics | 18 duplicate constants from PlantConstants |
| 2 | 1D | RCSHeatup | CoupledThermo called without P_floor/P_ceiling |
| 9 | 1E | ReactorCore | ReactorKinetics 9-function dependency needs verification |
| 12 | 1E | ControlRodBank | Duplicate constants from PlantConstants |
| 20 | 1E | ReactorController | Power ascension rough linear estimate |
| 27 | 1F | HeatupSimEngine | Two local constants (acceptable) |
| 34 | 1G | Test Infrastructure | 32 internal ValidateCalculations() tests not wired into runners |

### LOW Priority (16)
Various duplicate constants, deprecated Unity APIs, simplified models, minor code quality items.

### INFO (15)
Architecture confirmations, design decisions documented, cross-reference validations.

---

## RECOMMENDED NEXT STEPS

1. **Stage 1 Complete** — proceed to Stage 2 (Parameter Audit) for line-by-line verification of all constants against NRC/FSAR source documents
2. **Quick Win:** Wire remaining 32 ValidateCalculations() tests into Phase1TestRunner (Issue #34)
3. **Quick Win:** Mark HeatupValidation.cs as `[Obsolete]` (Issue #26)
4. **Consolidation:** Plan PlantConstantsHeatup merge into PlantConstants (Stage 1A HIGH issues)

---

**Document Version:** 1.0.0.0
**Audit Status:** STAGE 1 COMPLETE — ALL 43 FILES AUDITED
**Total Tests Identified:** ~248 (216 in runners + 32 module-internal)
**Total Issues:** 42 (2 HIGH, 9 MEDIUM, 16 LOW, 15 INFO)
