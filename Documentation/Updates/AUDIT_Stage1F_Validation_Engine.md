# AUDIT Stage 1F: Validation & Heatup Engine

**Date:** 2026-02-06
**Auditor:** Claude (Stage 1 — File Inventory & Architecture Mapping)
**Scope:** 4 files in Validation/ and Physics/ (~148 KB, ~2,490 lines)

---

## FILES ANALYZED

| # | File | Location | Size | Lines | Status |
|---|------|----------|------|-------|--------|
| 28 | HeatupSimEngine.cs | Validation/ | 67 KB | ~1,350 | GOLD STANDARD |
| 29 | HeatupValidationVisual.cs | Validation/ | 53 KB | ~820 | GOLD STANDARD |
| 30 | HeatupValidation.cs | Validation/ | 18 KB | ~310 | LEGACY — SUPERSEDED |
| 31 | AlarmManager.cs | Physics/ | 10 KB | ~170 | GOLD STANDARD |

---

## FILE-BY-FILE ANALYSIS

---

### 28. HeatupSimEngine.cs — GOLD STANDARD

**Purpose:** Complete physics simulation engine for Cold Shutdown → Hot Zero Power heatup. Contains all state management, physics orchestration, data logging, and history buffers. Zero GUI code.

**Architecture:** MonoBehaviour running a coroutine-based simulation loop. Acts as orchestrator — delegates all actual physics to the GOLD STANDARD physics modules audited in Stages 1A–1D. This file is the integration layer that wires them together for the heatup scenario.

**Physics Modules Used (10 dependencies):**
1. WaterProperties — Steam tables (density, Tsat, Psat, enthalpy)
2. CoupledThermo — P-T-V equilibrium solver (via RCSHeatup)
3. ThermalExpansion — Expansion coefficients (via CoupledThermo)
4. ThermalMass — RCS/PZR heat capacity
5. HeatTransfer — Insulation losses
6. VCTPhysics — Volume Control Tank inventory/boron
7. SolidPlantPressure — Solid pressurizer P-T-V during cold start
8. CVCSController — Charging/letdown PI control, seal flows, heater control
9. RCSHeatup — Isolated heating and bulk heatup physics
10. RCPSequencer — RCP startup timing and requirements

**Also uses:** PlantConstants, PlantConstantsHeatup, TimeAcceleration, LoopThermodynamics, RVLISPhysics, AlarmManager

**Key Design Patterns:**

1. **Pure Delegation — No Shadow Physics:**
   The file header explicitly documents all physics modules used. Physics calculations are delegated to modules, not duplicated inline. Comments like "Issue #7 FIX: Use PlantConstants instead of local duplicates" and "Issue #2 FIX: Delegate to RCSHeatup physics module" show systematic refactoring from an earlier version that had inline physics.

2. **Two Operational Phases:**
   - **Solid Pressurizer (Cold Start):** SolidPlantPressure module owns P-T-V coupling, CVCS pressure control, and bubble formation detection. Engine reads results and handles state transitions only.
   - **Two-Phase (Bubble Exists):** Phase 1 (no RCPs) uses RCSHeatup.IsolatedHeatingStep(); Phase 2 (RCPs running) uses RCSHeatup.BulkHeatupStep() with CoupledThermo.

3. **Frame-Decoupled Simulation Loop:**
   - Fixed dt = 1/360 hour (10-second physics steps)
   - TimeAcceleration module feeds sim-time budget
   - Budget capped to prevent runaway after alt-tab/lag
   - Max 50 steps per frame (sufficient for 10× at 30fps)

4. **State Transition: Solid → Two-Phase:**
   On bubble formation: initializes PZR at 25% level, transitions CVCS control from SolidPlantPressure to CVCSController, recalculates all masses and volumes.

5. **Data Logging:**
   - 12 history buffers (144 points, 5-min intervals, ~12 hr rolling window)
   - Interval log files every 30 sim-minutes with exhaustive plant state
   - Event log with edge-detected alarm transitions (rising edge only)
   - Final summary report at end of run

6. **Mass Conservation Tracking:**
   RCS inventory changes from CVCS net flow are explicitly tracked:
   ```
   massChange_lb = netCVCS_gpm × dt_sec × GPM_TO_FT3_SEC × rho_rcs
   physicsState.RCSWaterMass += massChange_lb
   ```
   Cross-checked against VCT cumulative changes via VCTPhysics.VerifyMassConservation().

**Constants:**
- MAX_RATE = 50°F/hr (Tech Spec limit, kept local — appropriate)
- PZR_HEATER_POWER_MW = PlantConstants.HEATER_POWER_TOTAL / 1000f (derived from PlantConstants ✅)
- RCP1_START_TIME = 1.0 hr, RCP_START_INTERVAL = 0.5 hr (delegated to RCPSequencer)
- MAX_HISTORY = 144 (5-min samples × 12 hours)

**Validation:** No dedicated ValidateCalculations() method — this is an integration/orchestration layer. Validation occurs via the HeatupIntegrationTests.cs test suite (Stage 1G). The engine does track validation criteria: subcooling ≥30°F, rate ≤50°F/hr, VCT level normal, mass conservation error.

---

### 29. HeatupValidationVisual.cs — GOLD STANDARD

**Purpose:** Pure GUI dashboard for the heatup simulation. Reads public state from HeatupSimEngine every frame and renders a nuclear control room dark-theme dashboard.

**Architecture:** MonoBehaviour with OnGUI()-based immediate mode rendering. Attached to same GameObject as HeatupSimEngine. Zero physics — confirmed by examination.

**Dashboard Features:**
- 6 arc gauges: T_RCS, T_PZR, Pressure, PZR Level, Subcooling, Rate
- 8 trend graphs with autoscaling axes
- 24-tile annunciator panel with blink animation
- RCP status display (4 pump indicators)
- RVLIS panel (3 range indicators)
- CVCS + VCT operational strips
- Heatup phase tracking with progress bar
- Scrollable operations event log
- Live PASS/FAIL validation checks
- Time acceleration: MSFS-style dropdown + keyboard (R/+/-)
- Dual-clock display (sim time vs wall time)

**Dependencies:** HeatupSimEngine (read-only reference), UnityEngine, UnityEngine.InputSystem, Critical.Physics (for PlantConstants color references only)

**Key Design Pattern:** Engine/View separation is clean. The visual reads only public fields from the engine — no setters, no callbacks, no physics calls. The engine can run headless without the visual attached.

**No issues identified.** This is a presentation layer with no physics responsibility.

---

### 30. HeatupValidation.cs — LEGACY / SUPERSEDED

**Purpose:** Original standalone heatup validation prototype. Self-contained with its own inline physics.

**Status: SUPERSEDED by HeatupSimEngine.cs + physics modules.**

This file was the original prototype before the physics were refactored into dedicated modules. It contains:
- Local duplicate constants (RCS_VOLUME=11500, PZR_VOLUME=1800, METAL_MASS=2200000, etc.)
- Simplified inline physics (no CoupledThermo, no SolidPlantPressure, no VCT)
- Simple polynomial Tsat correlation (less accurate than WaterProperties)
- No solid pressurizer operations
- No CVCS/VCT modeling
- Simple linear thermal expansion (no ThermalExpansion module)
- RCP start logic based on simple timer (no RCPSequencer)

**This file should NOT be used for simulation.** It exists as historical reference/fallback. HeatupSimEngine.cs completely replaces it with proper physics delegation.

**Constants that differ from PlantConstants:**
| Constant | HeatupValidation | PlantConstants | Match? |
|----------|-----------------|----------------|--------|
| RCS_VOLUME | 11,500 ft³ | RCS_WATER_VOLUME | Verify |
| PZR_VOLUME | 1,800 ft³ | PZR_TOTAL_VOLUME | Verify |
| METAL_MASS | 2,200,000 lb | RCS_METAL_MASS | Verify |
| CP_STEEL | 0.12 BTU/lb·°F | — | Verify |
| RCP_HEAT_EACH | 5.25 MW | RCP_HEAT_MW_EACH | Verify |
| HEAT_LOSS_AT_HOT | 1.5 MW | — | Verify |
| MAX_RATE | 50°F/hr | — | ✅ Matches |

---

### 31. AlarmManager.cs — GOLD STANDARD

**Purpose:** Centralized alarm setpoint checking for all plant annunciators. Stateless module — evaluates all alarm conditions from an AlarmInputs struct and returns complete AlarmState.

**Architecture:** Static class in Critical.Physics namespace. Extracted from ~20 inline alarm checks in HeatupSimEngine per audit fix Priority 5.

**Alarm Setpoints (per NRC HRTD):**

| Alarm | Setpoint | Unit | NRC Reference |
|-------|----------|------|---------------|
| PZR Level Low | < 20% | % | HRTD 10.3 |
| PZR Level High | > 85% | % | HRTD 10.3 |
| PZR Bubble Min | > 5% | % | Safe operating range |
| PZR Bubble Max | < 95% | % | Safe operating range |
| Pressure Low | < 350 psia | psia | HRTD — min for RCP |
| Pressure High | > 2300 psia | psia | Near safety valve |
| Subcooling Low | < 30°F | °F | Tech Spec |
| SMM Low Margin | < 15°F | °F | HRTD 3.4 |
| SMM No Margin | ≤ 0°F | °F | HRTD 3.4 |
| RVLIS Level Low | < 90% | % | HRTD 3.3 |
| Heatup In Progress | > 1°F/hr | °F/hr | — |
| Min Seal Inj/RCP | ≥ 7 gpm | gpm | NRC IN 93-84 |

**Public Interface:**
- `CheckAlarms(AlarmInputs)` → AlarmState — Evaluates all alarms in single call
- `GetActiveAlarmSummary(AlarmState)` → string — Comma-separated active alarm names

**Compound Logic:**
- SteamBubbleOK = bubbleFormed AND level in 5–95% range
- ModePermissive = SteamBubbleOK AND subcooling ≥ 30°F AND pressure ≥ 350 psia
- SealInjectionOK = no RCPs OR (seal injection ≥ RCPs × 7 gpm)
- SMMLowMargin = subcooling < 15°F AND > 0°F (excludes SMMNoMargin range)

**Dependencies:** None (pure logic, no imports except System.Collections.Generic for list in GetActiveAlarmSummary)

**Validation:** No dedicated test method, but setpoints are NRC-referenced constants. Stage 1G should verify test coverage in Phase1TestRunner or IntegrationTests.

---

## CROSS-MODULE DEPENDENCY MAP

```
HeatupSimEngine (orchestrator)
├── SolidPlantPressure ← Phase: Solid pressurizer
├── RCSHeatup ← Phase: Isolated heating + Bulk heatup
│   └── CoupledThermo ← P-T-V solver
│       └── ThermalExpansion ← Expansion coefficients
├── ThermalMass ← Heat capacity calculations
├── HeatTransfer ← Insulation losses
├── VCTPhysics ← Volume Control Tank
├── CVCSController ← Charging/letdown control
├── RCPSequencer ← RCP startup timing
├── LoopThermodynamics ← T_hot/T_cold
├── RVLISPhysics ← Vessel level indication
├── AlarmManager ← Alarm setpoint checking
├── WaterProperties ← Steam tables
├── TimeAcceleration ← Time warp
├── PlantConstants ← Reference values
└── PlantConstantsHeatup ← Heatup-specific constants

HeatupValidationVisual (GUI only)
└── HeatupSimEngine ← Reads public state (read-only)

HeatupValidation (LEGACY — standalone, no module dependencies)

AlarmManager (self-contained, no dependencies)
```

**Key Insight:** HeatupSimEngine touches 15 of the 20 Physics/ modules. This is the primary integration test bed for the entire physics engine. Any physics module bug will manifest here first.

---

## ISSUES REGISTER

### HIGH Priority (1)

**#26:** HeatupValidation.cs is a SUPERSEDED legacy file with its own inline physics that differs from the GOLD STANDARD modules. **Risk:** If someone accidentally attaches HeatupValidation instead of HeatupSimEngine, physics will be completely different (no CoupledThermo, no solid plant, simplified thermodynamics). **Recommendation:** Either (a) add clear `[Obsolete]` attribute and rename to `HeatupValidation_LEGACY.cs`, or (b) delete entirely. Stage 6 decision.

### MEDIUM Priority (1)

**#27:** HeatupSimEngine contains two local constants (MAX_RATE=50, PZR_HEATER_POWER_MW) that arguably belong in PlantConstants. MAX_RATE is the Tech Spec heatup rate limit (defensible as local policy), PZR_HEATER_POWER_MW is derived from PlantConstants.HEATER_POWER_TOTAL so this is acceptable. Low maintenance risk.

### LOW Priority (2)

**#28:** HeatupSimEngine uses `List<float>.RemoveAt(0)` for 12 rolling history buffers. This is O(n) per removal. At MAX_HISTORY=144 this is negligible, but circular buffers (or queues) would be more efficient if history size grows. Not a functional issue.

**#29:** HeatupSimEngine comment says `dt = 1f / 360f; // 10-second physics steps`. Verify: 1/360 hours = 10 seconds ✅ (3600/360=10). Comment is correct.

### INFO (4)

**#30:** HeatupValidationVisual has zero physics code — confirmed by full file examination. Clean engine/view separation. ✅

**#31:** AlarmManager.PZR_LEVEL_LOW_SETPOINT = 20%. CVCSController has its own low-level interlock at 17%. These are different alarms (annunciator vs. safety interlock) per NRC HRTD 10.3. Not a conflict. ✅

**#32:** HeatupSimEngine correctly initializes letdownViaRHR based on temperature vs PlantConstants.RHR_LETDOWN_ISOLATION_TEMP_F. Per NRC HRTD 19.0: low-pressure letdown is via RHR crossconnect. ✅

**#33:** AlarmManager located in Physics/ directory, not Validation/. This is correct — it's a general-purpose physics module used by HeatupSimEngine, not specific to validation. ✅

---

## ARCHITECTURE ASSESSMENT

**HeatupSimEngine is the most important integration file in the project.** At 67 KB it's the largest file and touches 15 physics modules. Its primary value is:

1. **Proper delegation pattern:** Every physics calculation delegates to the audited GOLD STANDARD modules. No shadow calculations, no inline approximations. The file's comments explicitly document each refactoring from inline code to module delegation.

2. **State machine for operational phases:** Solid pressurizer → Bubble formation → Isolated PZR heating → RCP startup → Bulk heatup. Each phase uses the appropriate physics module.

3. **Mass conservation enforcement:** RCS inventory tracks CVCS net flow explicitly. Cross-checked against VCT mass balance.

4. **Comprehensive logging:** 30-minute interval logs capture ~70 parameters per snapshot. Event log captures alarm transitions. Final report summarizes validation criteria.

**The legacy HeatupValidation.cs is the only significant concern** — it represents an earlier development phase with completely different (simplified) physics. It should be clearly marked as superseded to prevent confusion.

---

## VALIDATION SUMMARY

| File | Tests | Notes |
|------|-------|-------|
| HeatupSimEngine.cs | 0 direct | Tested via HeatupIntegrationTests.cs (Stage 1G) |
| HeatupValidationVisual.cs | 0 | GUI-only, no physics to test |
| HeatupValidation.cs | 0 | LEGACY — not in test scope |
| AlarmManager.cs | 0 direct | Setpoints verified against NRC HRTD |

**Total tests in Stage 1F: 0 direct** (integration tests in Stage 1G)
**Cumulative: 68 tests** (Stages 1A–1E)

---

## ACTION ITEMS FOR LATER STAGES

**Stage 2 (Parameter Audit):**
- Verify AlarmManager setpoints against FSAR/Tech Spec alarm setpoint sheets
- Verify HeatupValidation.cs legacy constants against PlantConstants for any discrepancies
- Verify bubble formation transition logic against NRC ML11223A342 Section 19.2.2

**Stage 4 (Module Integration Audit):**
- **CRITICAL:** Trace all 15 physics module calls in HeatupSimEngine — verify API contracts match actual module signatures
- Verify state transition from SolidPlantPressure → CVCSController at bubble formation
- Verify RCS mass conservation: track massChange_lb calculation through CVCS net flow
- Verify VCTPhysics.AccumulateRCSChange() is called in both solid and two-phase branches

**Stage 6 (Refactoring):**
- Mark or remove HeatupValidation.cs (Issue #26)
- Consider circular buffers for history (Issue #28 — optional)
