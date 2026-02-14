# SYSTEM_MAP.md — Execution Architecture & Update Loop Order
## Recovery Audit 2026-02-13

---

## 1. Primary Entrypoint

**File:** `Assets/Scripts/Validation/HeatupSimEngine.cs`
**Class:** `HeatupSimEngine : MonoBehaviour` (partial class, 6 files)
**Entry:** `RunSimulation()` coroutine (line ~620), launched from `Start()` or manual trigger.

The simulation runs as a Unity coroutine. Each frame, a time budget accumulates via `TimeAcceleration`. The inner loop calls `StepSimulation(dt)` repeatedly until the budget is consumed (max 50 steps/frame).

**Timestep:** `dt` is in **hours** (typical value: `1/720 = 0.00139 hr ≈ 5 seconds`).

---

## 2. StepSimulation(dt) — Full Call Order

**File:** `HeatupSimEngine.cs`, line 786
**Called:** Inner loop of `RunSimulation()`, line 689

The function is the **single physics dispatch** for the entire simulator. All subsystem updates happen here, in a fixed order. Below is the **exact call sequence** with file/line evidence:

### Section 1: RCP Startup Sequencing (line 793)
- `RCPSequencer.GetTargetRCPCount(...)` — determines how many RCPs should be running
- `CVCSController.PreSeedForRCPStart(...)` — pre-seeds PI controller at each pump start
- `RCPSequencer.GetEffectiveRCPContribution(...)` — calculates ramped heat contribution

### Section 1B: Heater Control (line 844)
- Mode transition check: `PRESSURIZE_AUTO → AUTOMATIC_PID` at ~2200 psia (line 859)
- **If AUTOMATIC_PID:** `CVCSController.UpdateHeaterPID(...)` (line 885)
- **Else:** `CVCSController.CalculateHeaterState(...)` (line 893)
- Heater power is set **BEFORE** physics so the correct power feeds into thermal steps.

### Section 1B-SPRAY: Pressurizer Spray (line 919)
- `CVCSController.UpdateSpray(...)` — calculates spray flow and steam condensation

### Section 1C: RHR System Update (line 960)
- `RHRSystem.Update(...)` — pump heat, HX removal, isolation logic (v3.0.0)

### Section 2: Physics Calculations — Three-Regime Model (line 980)

**Coupling factor:** `α = min(1.0, rcpContribution.TotalFlowFraction)` (line 1007)

#### REGIME 1 (α = 0): No RCPs — PZR/RCS Thermally Isolated (line 1009)
1. `SGMultiNodeThermal.Update(...)` — SG model with 0 RCPs (line 1018)
2. **If solid PZR or pre-drain:**
   - `SolidPlantPressure.Update(...)` (line 1049) — owns all P-T-V coupling during solid ops
   - Manual RCS mass update from CVCS net flow (line 1073–1078)
   - Canonical ledger sync: `TotalPrimaryMassSolid`, `TotalPrimaryMass_lb` (line 1080–1085)
   - `ProcessBubbleDetection()` — checks for first steam (line 1099)
3. **Else (bubble exists, no RCPs):**
   - `RCSHeatup.IsolatedHeatingStep(...)` (line 1105) — thermal isolation physics
   - RHR heat applied to T_rcs (line 1117–1121)

#### REGIME 2 (0 < α < 1): RCPs Ramping — Blended (line 1134)
1. **Isolated path:** `RCSHeatup.IsolatedHeatingStep(...)` (line 1149)
2. Sync `physicsState` PZR from engine state (line 1156–1165)
3. `SGMultiNodeThermal.Update(...)` (line 1168)
4. **CVCS mass drain pre-applied** before solver (line 1177–1188)
5. **Spray condensation pre-applied** before solver (line 1197–1211)
6. **Coupled path:** `RCSHeatup.BulkHeatupStep(...)` → calls `CoupledThermo.SolveEquilibrium(...)` (line 1214)
7. **Blend results** by α (line 1219–1246)
8. Sync SG display state (line 1248–1272)
9. Sync `physicsState` from blended result (line 1274–1281)

#### REGIME 3 (α = 1): All Pumps Fully Running — Full CoupledThermo (line 1305)
1. Sync `physicsState` from engine state (line 1311–1319)
2. `SGMultiNodeThermal.Update(...)` (line 1322)
3. **CVCS mass drain pre-applied** before solver (line 1338–1352)
4. **Spray condensation pre-applied** before solver (line 1361–1375)
5. `RCSHeatup.BulkHeatupStep(...)` → calls `CoupledThermo.SolveEquilibrium(...)` (line 1378)
6. Sync results to engine state (line 1383–1416)

### Section 3: Final Updates (line 1443)
- **v5.4.2.0 FF-05:** Re-baseline ledger after first physics step (line 1452–1458)
- `LoopThermodynamics.CalculateLoopTemperatures(...)` — T_hot / T_cold (line 1462)
- T_avg = (T_hot + T_cold) / 2 (line 1472)
- Rate calculations: heatupRate, pressureRate, pzrHeatRate (line 1474–1477)
- T_sat, subcooling, plantMode (line 1479–1481)

### Section 4: Bubble Formation State Machine (line 1487)
- `UpdateBubbleFormation(dt)` — 7-phase state machine (BubbleFormation partial)
- Returns `bubbleDrainActive` flag

### Section 5: CVCS, RCS Inventory, VCT (line 1492)
- `UpdateCVCSFlows(dt, bubbleDrainActive)` — CVCS partial, which calls:
  - `CVCSController.CalculateSealFlows(...)`
  - `UpdateOrificeLineup()`
  - PI level controller: `CVCSController.Update(...)`
  - `UpdateRCSInventory(dt, bubbleDrainActive)` — mass conservation for two-phase
  - `UpdateVCT(dt, ...)` — VCT physics, BRS coordination, system mass audit

### Section 6: simTime += dt (line 1494)

### Section 7: RVLIS & Annunciators (line 1498)
- `UpdateRVLIS()`
- `UpdateAnnunciators()`

### Section 8: HZP Systems (line 1507)
- `UpdateHZPSystems(dt)` — steam dump, heater PID, HZP state machine

### Section 9: Inventory Audit (line 1513)
- `UpdateInventoryAudit(dt)` — full mass balance tracking
- `UpdatePrimaryMassLedgerDiagnostics()` — canonical ledger drift check

---

## 3. Partial Class File Map

| File | Responsibility |
|------|---------------|
| `HeatupSimEngine.cs` | Core state, lifecycle, `StepSimulation()`, regime dispatch |
| `HeatupSimEngine.Init.cs` | Cold/warm start initialization |
| `HeatupSimEngine.BubbleFormation.cs` | 7-phase bubble formation state machine |
| `HeatupSimEngine.CVCS.cs` | CVCS flow control, RCS inventory, VCT update |
| `HeatupSimEngine.HZP.cs` | HZP stabilization, steam dump, heater PID |
| `HeatupSimEngine.Logging.cs` | Event log, interval logs, inventory audit |

---

## 4. Physics Module Dependency Map

```
HeatupSimEngine (coordinator)
├── SolidPlantPressure      — Solid PZR P-T-V coupling (Regime 1, solid)
├── RCSHeatup               — Isolated and bulk heatup steps
│   └── CoupledThermo       — Iterative P-T-V equilibrium solver
│       └── WaterProperties  — Steam tables (density, Tsat, Psat, h_fg)
│       └── ThermalExpansion  — β, κ coefficients
├── SGMultiNodeThermal      — Multi-node SG model (stratification, boiling, draining)
├── PressurizerPhysics      — Three-region PZR model (heater/spray/surge)
├── CVCSController          — PI level controller, heater PID, spray, seal flows
├── VCTPhysics              — Volume Control Tank (level, boron, makeup/divert)
├── BRSPhysics              — Boron Recycle System (holdup, evaporator, distillate)
├── RHRSystem               — Residual Heat Removal (pump heat, HX, isolation)
├── LoopThermodynamics      — T_hot/T_cold calculation
├── RCPSequencer            — RCP startup timing, ramp curves
├── HeatTransfer            — Insulation losses, surge line natural convection
├── ThermalMass             — RCS metal + fluid heat capacity
├── RVLISPhysics            — Reactor Vessel Level Indication System
├── AlarmManager            — Annunciator setpoint checking
├── SteamDumpController     — Steam dump for HZP heat removal
├── HZPStabilizationController — HZP approach state machine
├── TimeAcceleration        — Dual-clock time warp
└── PlantConstants          — Westinghouse 4-Loop reference values (partial: .cs, .SG, .Pressure, .CVCS, .BRS, .Heatup, .Nuclear, .Pressurizer)
```

---

## 5. State Ownership & Canonical Mass Rules

### Canonical Mass Ledger
- **Field:** `physicsState.TotalPrimaryMass_lb` (SystemState struct, CoupledThermo.cs:768)
- **Authority:** Sole source of truth for total primary mass (RCS + PZR water + PZR steam)
- **Updated by:** Boundary flows only (CVCS in/out, relief valve losses)
- **Never:** Overwritten by V×ρ recalculation

### Mass Distribution Rules
- **R1:** `TotalPrimaryMass_lb` is the single canonical ledger
- **R3:** Solver must NOT recalculate total from V×ρ
- **R5:** `RCSWaterMass = TotalPrimaryMass_lb - PZRWaterMass - PZRSteamMass` (by construction)

### Regime-Specific State Owners

| Regime | T/P Owner | Mass Owner | Notes |
|--------|-----------|------------|-------|
| Solid PZR (Regime 1, solid) | `SolidPlantPressure` | `TotalPrimaryMassSolid` | No steam, 100% water PZR |
| Bubble pre-drain | `SolidPlantPressure` | Transitional | CVCS flows managed by solid-plant-style |
| Bubble DRAIN | Engine (BubbleFormation partial) | Mass-based transfer | Phase change + CVCS drain |
| Regime 1 (bubble, no RCPs) | `RCSHeatup.IsolatedHeatingStep` | physicsState | Thermally isolated |
| Regime 2 (RCPs ramping) | Blended (isolated × (1-α) + coupled × α) | physicsState | CVCS pre-applied |
| Regime 3 (full RCPs) | `RCSHeatup.BulkHeatupStep` → `CoupledThermo` | physicsState with canonical ledger | CVCS pre-applied |

---

## 6. Key Flags & State Machine Transitions

| Flag | Set When | Cleared When | Affects |
|------|----------|-------------|---------|
| `solidPressurizer` | Init cold start | Bubble DETECTION phase | Regime 1 solid path |
| `bubblePreDrainPhase` | Bubble DETECTION | DRAIN start (VERIFICATION→DRAIN) | CVCS control mode |
| `bubbleFormed` | PRESSURIZE → COMPLETE | Never (persistent) | RCP start gate |
| `bubblePhase` (enum) | NONE→DETECTION→VERIFICATION→DRAIN→STABILIZE→PRESSURIZE→COMPLETE | — | State machine |
| `regime3CVCSPreApplied` | Before solver (Regime 2/3) | After UpdateRCSInventory | Double-count guard |
| `firstStepLedgerBaselined` | After first physics step | Never | Ledger re-baseline |
| `currentHeaterMode` (enum) | Various transitions | — | Heater control strategy |

---

## 7. Unit Conventions (System-Wide)

| Quantity | Unit | Notes |
|----------|------|-------|
| Temperature | °F | All temperatures |
| Pressure | psia | Internal; display converts to psig via -14.7 |
| Mass | lbm | Canonical mass unit |
| Volume | ft³ | RCS, PZR volumes |
| Volume (VCT, BRS) | gallons | Secondary/support systems |
| Flow rate | gpm | CVCS, seal, BRS flows |
| Heat/Power | MW | Thermal power |
| Time (physics) | hours | `dt` parameter to StepSimulation |
| Time (display) | various | TimeAcceleration manages conversion |
| Heat capacity | BTU/°F | RCS and PZR fluid + metal |
| Density | lb/ft³ | From WaterProperties |
| Conversion | 7.48052 gal/ft³ | `PlantConstants.FT3_TO_GAL` |
| Conversion | varies | `PlantConstants.GPM_TO_FT3_SEC` for flow |
