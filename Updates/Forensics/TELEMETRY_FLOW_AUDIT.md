# Telemetry Flow Audit — Execution-Flow Analysis for v5.5.0 Decoupling Layer

**Audit Date:** 2026-02-13
**Auditor:** Automated codebase analysis
**Scope:** Simulation tick flow, UI data reads, telemetry insertion points
**Purpose:** Inform safe implementation of v5.5.0 telemetry/observer layer
**Reference:** `FUTURE_ENHANCEMENTS_ROADMAP.md` §v5.5.0 — UI/UX Design Principles

---

## Table of Contents

1. [Simulation Tick Entry Points](#1-simulation-tick-entry-points)
2. [Order-of-Operations for One Sim Tick](#2-order-of-operations-for-one-sim-tick)
3. [UI Simulation Value Reads](#3-ui-simulation-value-reads)
4. [Existing Telemetry-Like Concepts](#4-existing-telemetry-like-concepts)
5. [Recommended Telemetry Insertion Point](#5-recommended-telemetry-insertion-point)
6. [Proposed Telemetry Wiring Plan](#6-proposed-telemetry-wiring-plan)
7. [Appendix A — Tick Flow Diagram](#appendix-a--tick-flow-diagram)
8. [Appendix B — UI Script → Values Read → Source Object](#appendix-b--ui-script--values-read--source-object)
9. [Appendix C — Simulation Update Methods → Subsystems → Phase](#appendix-c--simulation-update-methods--subsystems--phase)

---

## 1. Simulation Tick Entry Points

### 1.1 Primary MonoBehaviour: `HeatupSimEngine`

**File:** `Assets/Scripts/Validation/HeatupSimEngine.cs`
**Type:** `MonoBehaviour` (partial class spanning 6 files)
**Lifecycle:** Coroutine-driven, NOT Update/FixedUpdate

| Entry Point | Type | Location | Purpose |
|---|---|---|---|
| `Start()` | MonoBehaviour | HeatupSimEngine.cs | Calls `StartCoroutine(RunSimulation())` |
| `RunSimulation()` | Coroutine (IEnumerator) | HeatupSimEngine.cs ~line 680 | Main simulation loop; yields once per Unity frame |
| `StepSimulation(dt)` | Instance method | HeatupSimEngine.cs ~line 742 | Single physics tick; called 1-50× per frame depending on time acceleration |

**No `Update()`, `FixedUpdate()`, or `LateUpdate()`** exists on `HeatupSimEngine`. All physics execution is driven by the coroutine's inner loop.

### 1.2 Coroutine Structure

```
Start()
  └─ StartCoroutine(RunSimulation())

RunSimulation():
  ├─ InitializeSimulation()                    [once]
  ├─ LOOP while (simTime < targetTime):
  │   ├─ TimeAcceleration.Tick()               [once per frame]
  │   ├─ Compute stepsThisFrame from multiplier
  │   ├─ INNER LOOP (1..stepsThisFrame, max 50):
  │   │   ├─ dt = 1/360 hr (10 sim-seconds)
  │   │   ├─ StepSimulation(dt)                [MAIN PHYSICS]
  │   │   └─ simTime += dt
  │   ├─ AddHistory()                          [every 10s sim-time, then every 1 min]
  │   ├─ SaveIntervalLog()                     [every 15 sim-minutes]
  │   └─ yield return null                     [YIELD TO UNITY — one frame boundary]
  └─ SaveReport()                              [once at end]
```

**Key insight:** Physics steps happen in a burst (up to 50 per frame at 10× speed), then yield. UI rendering (OnGUI) runs after the yield, seeing the final state of that frame's burst.

### 1.3 Time Acceleration

**File:** `Assets/Scripts/Physics/TimeAcceleration.cs`
**Type:** Static utility class

| Property | Purpose |
|---|---|
| `SpeedSteps` | `{1, 2, 4, 8, 10}` discrete multipliers |
| `SimDeltaTime_Hours` | Per-frame sim delta; engine reads this |
| `Tick()` | Advances wall/sim clocks; called once per frame |

Physics timestep `dt` remains fixed at `1/360 hr` (10 seconds). Speed multiplier affects steps-per-frame, not step size. This preserves numerical stability at all speeds.

### 1.4 UI MonoBehaviour: `HeatupValidationVisual`

**File:** `Assets/Scripts/Validation/HeatupValidationVisual.cs`
**Type:** `MonoBehaviour` (partial class spanning 10+ files)
**Lifecycle:** `OnGUI()` — IMGUI rendering, runs 1-2× per frame after coroutine yield

| Entry Point | Type | Purpose |
|---|---|---|
| `Start()` | MonoBehaviour | Acquires `HeatupSimEngine` reference |
| `OnGUI()` | IMGUI callback | Reads engine state and renders dashboard |

**No `Update()` on this class.** All rendering is in `OnGUI()`.

### 1.5 Operator Screen MonoBehaviours

All operator screens use `Update()` with frame-time throttling:

| Screen | Update Rate (Gauges) | Update Rate (Visuals) | Data Source |
|---|---|---|---|
| `ReactorOperatorScreen` | ~10 Hz via MosaicBoard | ~10 Hz | MosaicBoard → ReactorController |
| `PressurizerScreen` | 10 Hz (0.1s throttle) | 2 Hz (0.5s throttle) | ScreenDataBridge |
| `CVCSScreen` | 10 Hz (0.1s throttle) | 2 Hz (0.5s throttle) | ScreenDataBridge |
| `SteamGeneratorScreen` | 10 Hz (0.1s throttle) | 2 Hz (0.5s throttle) | ScreenDataBridge |

---

## 2. Order-of-Operations for One Sim Tick

Each call to `StepSimulation(dt)` executes **8 sequential sections** in strict order. No parallelism; no out-of-order execution.

### 2.1 Tick Sequence (per `StepSimulation(dt)`)

```
StepSimulation(dt):

  SECTION 1 — RCP Startup                              [~line 749]
    RCPSequencer.GetTargetRCPCount()
    CVCSController.PreSeedForRCPStart()
    RCPSequencer.GetEffectiveRCPContribution()
    → rcpCount, rcpContribution, effectiveRCPHeat

  SECTION 1B — Heater Control (BEFORE physics)          [~line 800]
    Mode transition check (→ PID at 2200 psig)
    CVCSController.UpdateHeaterPID() OR .CalculateHeaterState()
    → pzrHeaterPower (MW)

  SECTION 1B-SPRAY — Spray Control                      [~line 875]
    CVCSController.UpdateSpray()
    → sprayFlow_GPM, sprayValvePosition

  SECTION 1C — RHR System                               [~line 916]
    RHRSystem.Update()
    RHRSystem.BeginIsolation() (if RCP starts)
    → rhrActive, rhrNetHeat_MW

  SECTION 2 — Physics (THREE REGIMES by α factor)       [~line 936]
    α = rcpContribution.TotalFlowFraction

    IF α < 0.001 — REGIME 1: No RCPs
      IF solidPressurizer:
        SolidPlantPressure.Update()
        ProcessBubbleDetection()
      ELSE:
        RCSHeatup.IsolatedHeatingStep()
      SGMultiNodeThermal.Update(α=0)

    ELSE IF α < 0.999 — REGIME 2: RCPs Ramping
      Sync PZR state → physicsState
      [PRE-SOLVER] Apply CVCS drain if active
      [PRE-SOLVER] Apply spray condensation if active
      isolated = RCSHeatup.IsolatedHeatingStep()
      SGMultiNodeThermal.Update(α)
      coupled = RCSHeatup.BulkHeatupStep()
        └─ CoupledThermo.SolveEquilibrium() [≤50 iterations]
      Result = (1-α)*isolated + α*coupled

    ELSE — REGIME 3: All RCPs Running
      Sync PZR state → physicsState
      [PRE-SOLVER] Apply CVCS drain if active
      [PRE-SOLVER] Apply spray condensation if active
      SGMultiNodeThermal.Update(α=1.0)
      RCSHeatup.BulkHeatupStep()
        └─ CoupledThermo.SolveEquilibrium()

    → T_rcs, T_pzr, pressure, PZR masses, SG state

  SECTION 3 — Loop Temps & Rates                        [~line 1368]
    LoopThermodynamics.CalculateLoopTemperatures()
    heatupRate = ΔT_rcs / dt
    pressureRate = ΔP / dt
    plantMode = GetMode(T_rcs)
    → T_hot, T_cold, T_avg, heatupRate, pressureRate

  SECTION 4 — Bubble Formation                          [~line 1400]
    UpdateBubbleFormation(dt)
    → bubblePhase, bubbleDrainActive (7-phase state machine)

  SECTION 5 — CVCS & Inventory                          [~line 1405]
    UpdateCVCSFlows(dt, bubbleDrainActive)
      UpdateOrificeLineup()
      CVCSController.Update() (PI level controller)
      UpdateRCSInventory() (CVCS mass flow → canonical ledger)
      UpdateVCT() (VCT + BRS coordination)
      UpdateLetdownPath()
    → chargingFlow, letdownFlow, vctState, TotalPrimaryMass_lb

  SECTION 6 — Annunciators                              [~line 1410]
    UpdateRVLIS()
      RVLISPhysics.Update()
    UpdateAnnunciators()
      AlarmManager computes alarm booleans
      ProcessAlarmEdges() (rising/falling edge detection)
    → all alarm booleans, rvlisFull, rvlisDynamic

  SECTION 7 — HZP Systems (active at T > 550°F)        [~line 1420]
    UpdateHZPSystems(dt)
      SteamDumpController.Update()
      HZPStabilizationController.Update()
      CheckStartupPrerequisites()
    → hzpStable, steamDumpActive, steamDumpHeat_MW

  SECTION 8 — Inventory Audit                           [~line 1426]
    UpdateInventoryAudit(dt)
      Calculate all compartment masses
      Accumulate boundary flows
      Check conservation error
    → totalSystemMass_lbm, massError_lbm, massConservationError

  simTime += dt
```

### 2.2 Key Ordering Constraints

| Constraint | Reason |
|---|---|
| Heaters BEFORE physics | Heater power is input to thermal solver |
| Spray BEFORE physics | Spray condensation applied as pre-solver mass transfer |
| RHR BEFORE physics | RHR heat removal is input to temperature step |
| Physics BEFORE CVCS | CVCS reads post-physics T, P for level/flow calc |
| CVCS BEFORE alarms | Alarm checks need updated VCT level and flows |
| Alarms BEFORE HZP | HZP prerequisites depend on alarm states |
| Inventory audit LAST | Conservation check uses all final-state values |

---

## 3. UI Simulation Value Reads

### 3.1 Data Access Patterns (Three Distinct Architectures)

**Pattern A — Direct Engine Access (tightest coupling)**
Used by: `HeatupValidationVisual` (all partials)
Mechanism: `engine.fieldName` public field read in `OnGUI()`
Count: ~155 distinct field reads
Frequency: Every OnGUI call (1-2× per frame)

**Pattern B — ScreenDataBridge Singleton (loose coupling)**
Used by: `PressurizerScreen`, `CVCSScreen`, `SteamGeneratorScreen`
Mechanism: `ScreenDataBridge.Instance.GetXxx()` → tries heatupEngine, falls back to reactorController, returns NaN if neither
Count: 51 public getter methods
Frequency: 10 Hz gauges, 2 Hz visuals (frame-time throttled)

**Pattern C — MosaicBoard Component System**
Used by: `ReactorOperatorScreen` via `MosaicGauge` components
Mechanism: `board.GetValue(GaugeType)` → `board.GetSmoothedValue()` → `board.GetAlarmState()`
Count: ~13 gauge types
Frequency: ~10 Hz (board-managed rate limit)

### 3.2 Critical Finding: Two Systems, No Shared Bus

The validation dashboard (`HeatupValidationVisual`) and operator screens (`PressurizerScreen`, etc.) use **completely independent data paths**. Neither is aware of the other's reads. Both read from `HeatupSimEngine` public fields, but via different mechanisms. There is **no shared event bus, no observer pattern, no data snapshot layer**.

---

## 4. Existing Telemetry-Like Concepts

### 4.1 ScreenDataBridge (Closest to Telemetry Layer)

**File:** `Assets/Scripts/UI/ScreenDataBridge.cs`
**Type:** Singleton MonoBehaviour
**Role:** Abstraction layer between operator screens and physics engines

This is the **closest existing concept to a telemetry bus**. It provides:
- Unified getter interface (51 methods)
- Source fallback (heatupEngine → reactorController → default)
- NaN convention for unavailable data
- Clean separation of UI from engine internals

**Limitation:** It still reads live engine fields on every getter call. There is no snapshot, no buffering, no history.

### 4.2 History Buffers (in HeatupSimEngine)

**Method:** `AddHistory()` in `HeatupSimEngine.cs` (~line 694)
**Mechanism:** Rolling 240-point arrays stored as `List<float>` per parameter
**Sampling:** Every 10 sim-seconds initially, then every 1 sim-minute
**Parameters stored:** T_rcs, pressure, pzrLevel, heatupRate, and ~10 others
**Consumers:** `HeatupValidationVisual.Graphs.cs` reads these for strip chart plotting

**Limitation:** History is managed inside the engine class. No external observer can subscribe. Fixed sampling rate. Not ring-buffered (uses `List<T>.Add()` with periodic trimming).

### 4.3 Event Log (in HeatupSimEngine.Logging.cs)

**Mechanism:** `eventLog` (List<EventLogEntry>) with severity-tagged entries
**Consumers:** `HeatupValidationVisual.TabEventLog.cs`
**Pattern:** Append-only log, not a telemetry channel

### 4.4 Alarm Edge Detection (in HeatupSimEngine.Alarms.cs)

**Mechanism:** Table-driven `AlarmEdgeDescriptor` array; `ProcessAlarmEdges()` detects rising/falling edges
**Pattern:** Observer-like (detects state transitions), but outputs to event log, not to subscribers

### 4.5 What Does NOT Exist

| Concept | Status |
|---|---|
| Event bus / message broker | **Does not exist** |
| Observer/subscriber pattern | **Does not exist** |
| TelemetrySnapshot struct | **Does not exist** |
| Ring buffer per channel | **Does not exist** (history uses List<T>) |
| Data-change callbacks | **Does not exist** |
| ScriptableObject data channels | **Does not exist** |
| Double-buffered state | **Does not exist** |

---

## 5. Recommended Telemetry Insertion Point

### 5.1 Publisher Location: End of `StepSimulation()`

**Insert after:** Section 8 (Inventory Audit) and before `simTime += dt`
**Location:** `HeatupSimEngine.cs` ~line 1427
**Rationale:**

1. All physics, CVCS, alarms, HZP, and audit have completed — state is fully consistent
2. No further writes to any state variable will occur until the next `StepSimulation()` call
3. This is the single point where ALL subsystem outputs are finalized
4. The coroutine's inner loop may call `StepSimulation()` multiple times per frame; telemetry should publish after EACH call (not just once per frame) to capture intermediate states at high time acceleration

```
StepSimulation(dt):
  ... Section 1-8 as described ...

  ──► TelemetryPublisher.Publish(this)   ◄── NEW INSERTION POINT

  simTime += dt
```

### 5.2 Consumer Location: `OnGUI()` / `Update()` (after yield)

**For HeatupValidationVisual:** Read the latest `TelemetrySnapshot` at the start of `OnGUI()`.
**For Operator Screens:** Read via modified `ScreenDataBridge` in their existing `Update()` at existing throttle rates (10 Hz / 2 Hz).

Because physics may step 1-50 times per frame but UI renders once, the UI should always read the **most recent** snapshot. Ring buffer history captures all intermediate ticks.

### 5.3 Double-Buffering Consideration

**Current architecture:** Single-threaded. Coroutine yields between frames. No threading.
**Recommendation:** Implement a simple index-swap double buffer even though threading is not currently used:

```
Buffer[0] ← writer (physics fills during StepSimulation)
Buffer[1] ← reader (UI reads during OnGUI/Update)

After each StepSimulation():
  Swap currentWriteIndex ↔ currentReadIndex (atomic int swap)
```

This is **zero-cost insurance** for future threading and eliminates any concern about UI reading partially-written state during a physics burst. The swap is a single `Interlocked.Exchange()` call.

### 5.4 History Ring Buffer Location

**Publish history:** Inside `TelemetryPublisher.Publish()`, append the current value to each channel's ring buffer.
**Ring buffer sizing:** At 10-second physics ticks and 120-second retention → 12 entries per channel minimum. At 10× speed, 120 entries per second of wall time → size ring to 1200 entries (generous) or dynamically based on retention window.

---

## 6. Proposed Telemetry Wiring Plan

### 6.1 Architecture Overview

```
┌──────────────────────────────────────┐
│         HeatupSimEngine              │
│  StepSimulation(dt):                 │
│    Section 1..8 (physics)            │
│    ↓                                 │
│    TelemetryPublisher.Publish(this)  │──► Snapshot[write] filled
│    ↓                                 │    Ring buffers appended
│    Index swap (atomic)               │──► Snapshot[read] now current
│    simTime += dt                     │
└──────────────────────────────────────┘
          │                     │
          │ (1-50× per frame)   │
          ▼                     ▼
┌─────────────────┐   ┌─────────────────────┐
│ TelemetrySnapshot│   │ TelemetryHistory     │
│ (current values) │   │ (per-channel rings)  │
│ .RCS_Tavg        │   │ .GetHistory("Tavg")  │
│ .RCS_Pressure    │   │ → float[1200]        │
│ .PZR_Level       │   │                      │
│ .PZR_Pressure    │   │ 120s retention       │
│ ... (25+ ch)     │   │ 10s sample interval  │
└────────┬────────┘   └──────────┬───────────┘
         │                       │
    ╔════╧═══════════════════════╧════╗
    ║  UI READS ONLY FROM THESE TWO  ║
    ╚════╤═══════════════════════╤════╝
         │                       │
    ┌────┴────────┐    ┌────────┴─────────┐
    │OnGUI() reads│    │Update() reads    │
    │snapshot for │    │snapshot via      │
    │gauges/text  │    │ScreenDataBridge  │
    │             │    │(modified to read │
    │HeatupValid- │    │ snapshot, not    │
    │ationVisual  │    │ engine fields)   │
    └─────────────┘    └──────────────────┘
```

### 6.2 Publisher Location

**Class:** `TelemetryPublisher` (new static class)
**Called from:** `HeatupSimEngine.StepSimulation()` — end of Section 8, before `simTime += dt`
**Frequency:** Once per physics tick (not once per frame)

### 6.3 Consumer Locations

| Consumer | Method | Reads | Rate |
|---|---|---|---|
| `HeatupValidationVisual` (all partials) | `OnGUI()` | `TelemetrySnapshot` (read buffer) | Per-frame |
| `ScreenDataBridge` (modified) | Getter methods | `TelemetrySnapshot` (read buffer) | On demand (10 Hz effective) |
| `HeatupValidationVisual.Graphs` | `OnGUI()` | `TelemetryHistory.GetHistory(channel)` | Per-frame |

### 6.4 Candidate Telemetry Channels (Top 25)

These are the highest-value channels for the CRITICAL dashboard, derived from the UI read audit:

| # | Channel Name | Units | Source Field | Subsystem | Needs History |
|---|---|---|---|---|---|
| 1 | `RCS.Tavg` | °F | `T_rcs` (derived as T_avg) | RCS | Yes |
| 2 | `RCS.Thot` | °F | `T_hot` | RCS | Yes |
| 3 | `RCS.Tcold` | °F | `T_cold` | RCS | Yes |
| 4 | `RCS.Pressure` | psia | `pressure` | RCS | Yes |
| 5 | `RCS.PressureRate` | psi/hr | `pressureRate` | RCS | Yes |
| 6 | `RCS.HeatupRate` | °F/hr | `heatupRate` | RCS | Yes |
| 7 | `PZR.Level` | % | `pzrLevel` | PZR | Yes |
| 8 | `PZR.Pressure` | psia | `pressure` (same as RCS) | PZR | Yes |
| 9 | `PZR.Temperature` | °F | `T_pzr` | PZR | Yes |
| 10 | `PZR.HeaterPower` | MW | `pzrHeaterPower` | PZR | Yes |
| 11 | `PZR.SteamVolume` | ft³ | `pzrSteamVolume` | PZR | No |
| 12 | `PZR.WaterVolume` | ft³ | `pzrWaterVolume` | PZR | No |
| 13 | `PZR.Subcooling` | °F | `subcooling` | PZR | Yes |
| 14 | `PZR.SurgeFlow` | gpm | `surgeFlow` | PZR | Yes |
| 15 | `SG.SecondaryPressure` | psia | `sgSecondaryPressure_psia` | SG | Yes |
| 16 | `SG.BulkTemp` | °F | `T_sg_secondary` | SG | Yes |
| 17 | `SG.HeatTransfer` | MW | `sgHeatTransfer_MW` | SG | Yes |
| 18 | `SG.SaturationTemp` | °F | `sgSaturationTemp_F` | SG | No |
| 19 | `SG.BoilingIntensity` | 0-1 | `sgBoilingIntensity` | SG | Yes |
| 20 | `CVCS.ChargingFlow` | gpm | `chargingFlow` | CVCS | Yes |
| 21 | `CVCS.LetdownFlow` | gpm | `letdownFlow` | CVCS | Yes |
| 22 | `VCT.Level` | % | `vctState.Level_percent` | CVCS | Yes |
| 23 | `MASS.TotalPrimary` | lbm | `totalSystemMass_lbm` | Audit | Yes |
| 24 | `MASS.Error` | lbm | `massError_lbm` | Audit | Yes |
| 25 | `RVLIS.DisplayLevel` | % | `rvlisFull` | RVLIS | No |

**Channels requiring history (strip charts):** 20 of 25
**Channels current-value only:** 5 of 25

### 6.5 Sampling Rate & Retention

| Parameter | Value | Rationale |
|---|---|---|
| Sample interval | Once per physics tick (10 sim-seconds) | Matches existing physics timestep |
| Retention window | 120 seconds wall-clock (per v5.5.0 roadmap) | Dashboard strip chart visible window |
| Ring buffer size | 1,200 entries per channel | 120s × 10×speed = 1,200 ticks max; pre-allocated |
| Total memory (25 ch) | 25 × 1,200 × 4 bytes = **120 KB** | Negligible |
| Runtime resize | **Never** | Pre-allocate at init; fixed size |

### 6.6 Snapshot Struct Concept

```
TelemetrySnapshot:
  SimTime_hr          float
  WallTime_hr         float
  PlantMode           int
  BubblePhase         int

  // RCS (6 channels)
  RCS_Tavg            float
  RCS_Thot            float
  RCS_Tcold           float
  RCS_Pressure_psia   float
  RCS_PressureRate    float
  RCS_HeatupRate      float

  // PZR (8 channels)
  PZR_Level_pct       float
  PZR_Temperature     float
  PZR_HeaterPower_MW  float
  PZR_SteamVolume     float
  PZR_WaterVolume     float
  PZR_Subcooling      float
  PZR_SurgeFlow_gpm   float
  PZR_SprayActive     bool

  // SG (5 channels)
  SG_SecPressure_psia float
  SG_BulkTemp         float
  SG_HeatTransfer_MW  float
  SG_SatTemp          float
  SG_BoilingIntensity float

  // CVCS (3 channels)
  CVCS_ChargingFlow   float
  CVCS_LetdownFlow    float
  VCT_Level_pct       float

  // Mass Conservation (2 channels)
  Mass_Total_lbm      float
  Mass_Error_lbm      float

  // RVLIS (1 channel)
  RVLIS_Display_pct   float

  // Alarm summary
  AlarmFlags          uint  (bitfield)
```

**Size:** ~128 bytes per snapshot. Double-buffered = 256 bytes total.

### 6.7 Migration Path (Incremental)

The telemetry layer can be introduced **incrementally** without breaking existing code:

| Step | Change | Risk |
|---|---|---|
| 1 | Add `TelemetrySnapshot` struct + `TelemetryPublisher` static class | Zero — no existing code modified |
| 2 | Add `Publish(engine)` call at end of `StepSimulation()` | Minimal — one line added to engine |
| 3 | Add ring buffer history alongside existing `AddHistory()` | Zero — parallel to existing history |
| 4 | Modify `ScreenDataBridge` getters to read snapshot instead of engine | Low — same return values, different source |
| 5 | Modify `HeatupValidationVisual` to read snapshot | Medium — touches many partial files |
| 6 | Remove direct engine field reads from UI | Final cleanup — enables full decoupling |

Steps 1-3 can land in a single commit with **zero behavioral change**. Steps 4-6 are the actual UI migration.

---

## Appendix A — Tick Flow Diagram

```
╔══════════════════════════════════════════════════════════════╗
║                    UNITY FRAME N                             ║
╠══════════════════════════════════════════════════════════════╣
║                                                              ║
║  RunSimulation() coroutine resumes after yield               ║
║  │                                                           ║
║  ├─ TimeAcceleration.Tick()                                  ║
║  │   └─ SimDeltaTime, WallDeltaTime updated                 ║
║  │                                                           ║
║  ├─ stepsThisFrame = speed × frameDelta / physicsDt          ║
║  │                                                           ║
║  ├─ FOR step = 1..stepsThisFrame (max 50):                   ║
║  │   │                                                       ║
║  │   └─ StepSimulation(dt = 1/360 hr):                       ║
║  │       │                                                   ║
║  │       ├─ §1  RCP Sequencing ──────────────► rcpCount      ║
║  │       ├─ §1B Heater Control ──────────────► heaterPower   ║
║  │       ├─ §1S Spray Control  ──────────────► sprayFlow     ║
║  │       ├─ §1C RHR System ─────────────────► rhrHeat        ║
║  │       │                                                   ║
║  │       ├─ §2  PHYSICS (regime 1/2/3) ─────► T, P, masses  ║
║  │       │   └─ CoupledThermo.Solve() ──────► P-T-V equil.  ║
║  │       │   └─ SGMultiNodeThermal.Update() ► SG state       ║
║  │       │                                                   ║
║  │       ├─ §3  Loop Temps & Rates ─────────► Thot/Tcold     ║
║  │       ├─ §4  Bubble Formation ───────────► bubblePhase    ║
║  │       ├─ §5  CVCS & Inventory ───────────► flows, VCT     ║
║  │       ├─ §6  Annunciators ───────────────► alarm flags    ║
║  │       ├─ §7  HZP Systems ───────────────► stability       ║
║  │       ├─ §8  Inventory Audit ────────────► mass error     ║
║  │       │                                                   ║
║  │       ├─ ══► TELEMETRY PUBLISH POINT ◄══  (proposed)      ║
║  │       │       Snapshot filled, index swapped              ║
║  │       │       Ring buffers appended                       ║
║  │       │                                                   ║
║  │       └─ simTime += dt                                    ║
║  │                                                           ║
║  ├─ AddHistory() (if interval elapsed)                       ║
║  ├─ SaveIntervalLog() (if 15-min elapsed)                    ║
║  │                                                           ║
║  └─ yield return null ─────────────────────► FRAME BOUNDARY  ║
║                                                              ║
╠══════════════════════════════════════════════════════════════╣
║                                                              ║
║  OnGUI() — HeatupValidationVisual renders                    ║
║  │  └─ Reads TelemetrySnapshot[readIndex] (proposed)         ║
║  │  └─ Reads TelemetryHistory for strip charts (proposed)    ║
║  │                                                           ║
║  Update() — Operator screens (PressurizerScreen, etc.)       ║
║     └─ ScreenDataBridge reads TelemetrySnapshot (proposed)   ║
║                                                              ║
╚══════════════════════════════════════════════════════════════╝
```

---

## Appendix B — UI Script → Values Read → Source Object

### B.1 HeatupValidationVisual (Direct Engine Reads)

| UI Script (Partial) | Value Read | Source Object | Lifecycle |
|---|---|---|---|
| `.cs` (core) | simTime, wallClockTime, heatupPhaseDesc, currentSpeedIndex, isAccelerated, eventLog.Count | `engine.*` | OnGUI |
| `.Panels.cs` — PlantOverview | plantMode, T_rcs, T_pzr, pressure, solidPressurizer, bubbleFormed, timeToBubble | `engine.*` | OnGUI |
| `.Panels.cs` — RCPGrid | rcpRunning[], rcpContribution.*, effectiveRCPHeat | `engine.*` | OnGUI |
| `.Panels.cs` — BubbleState | bubblePhase, bubblePhaseStartTime, pzrLevel, ccpStarted, ccpStartLevel, auxSprayActive, auxSprayTestPassed, auxSprayPressureDrop | `engine.*` | OnGUI |
| `.Panels.cs` — HeaterMode | currentHeaterMode, pzrHeaterPower, pzrHeatersOn, pressureRate | `engine.*` | OnGUI |
| `.Panels.cs` — RVLIS | rvlisDynamic, rvlisDynamicValid, rvlisFull, rvlisFullValid, rvlisUpper, rvlisUpperValid, rvlisLevelLow, rcpCount | `engine.*` | OnGUI |
| `.Panels.cs` — Inventory | rcsWaterMass, pzrWaterVolume, pzrSteamVolume, pzrLevel, vctState.Volume_gal, vctState.Level_percent, brsState.*, totalSystemMass_lbm, initialSystemMass_lbm, massError_lbm, massConservationError | `engine.*` + `WaterProperties.*` | OnGUI |
| `.Panels.cs` — SGRHR | sgSecondaryPressure_psia, sgSaturationTemp_F, sgMaxSuperheat_F, sgBoilingActive, sgBoilingIntensity, sgNitrogenIsolated, sgHeatTransfer_MW, rhrModeString, rhrActive, rhrNetHeat_MW, rhrHXRemoval_MW, rhrPumpHeat_MW | `engine.*` | OnGUI |
| `.Panels.cs` — HZP | hzpReadyForStartup, hzpStable, hzpProgress, steamDumpActive, steamPressure_psig, steamDumpHeat_MW, heaterPIDActive, heaterPIDOutput, GetStartupReadiness() | `engine.*` | OnGUI |
| `.TabCritical.cs` — RCS | T_avg, pressure, T_hot, T_cold, pressureRate, effectiveRCPHeat, pzrHeaterPower, sgHeatTransfer_MW | `engine.*` | OnGUI |
| `.TabCritical.cs` — PZR | pressure, pzrLevel, pzrLevelSetpointDisplay, T_pzr, pzrHeaterPower, pzrHeatersOn, sprayActive, solidPressurizer, bubbleFormed | `engine.*` | OnGUI |
| `.TabCritical.cs` — SG | sgSecondaryPressure_psia, T_rcs, T_sg_secondary, sgSaturationTemp_F, steamDumpActive, sgBoilingActive, sgBoilingIntensity | `engine.*` | OnGUI |
| `.TabCritical.cs` — CVCS | chargingFlow, letdownFlow, totalSystemMass_lbm, massError_lbm, massConservationError | `engine.*` | OnGUI |
| `.TabCritical.cs` — VCT | vctState.Level_percent, vctMakeupActive, vctDivertActive, vctRWSTSuction, vctLevelLow, vctLevelHigh | `engine.*` | OnGUI |
| `.TabValidation.cs` | massConservationError, massError_lbm, heatupRate, subcooling, pzrLevel, pressureRate, letdownActive, letdownIsolatedFlag, vctState.Level_percent, rvlisLevelLow, rcpContribution.AllFullyRunning, GetStartupReadiness() | `engine.*` | OnGUI |

### B.2 Operator Screens (via ScreenDataBridge)

| UI Script | Value Read | ScreenDataBridge Getter | Lifecycle |
|---|---|---|---|
| `PressurizerScreen` | PZR pressure | `GetPZRPressure()` → `engine.pressure` | Update (10Hz) |
| `PressurizerScreen` | PZR level | `GetPZRLevel()` → `engine.pzrLevel` | Update (10Hz) |
| `PressurizerScreen` | PZR water temp | `GetPZRWaterTemp()` → `engine.T_pzr` | Update (10Hz) |
| `PressurizerScreen` | Heater power | `GetHeaterPower()` → `engine.pzrHeaterPower` | Update (10Hz) |
| `PressurizerScreen` | Pressure rate | `GetPressureRate()` → `engine.pressureRate` | Update (10Hz) |
| `PressurizerScreen` | Surge flow | `GetSurgeFlow()` → `engine.surgeFlow` | Update (10Hz) |
| `PressurizerScreen` | Steam/water vol | `GetPZRSteamVolume()` / `GetPZRWaterVolume()` | Update (10Hz) |
| `PressurizerScreen` | Subcooling | `GetSubcooling()` → `engine.subcooling` | Update (10Hz) |
| `CVCSScreen` | Charging flow | `GetChargingFlow()` → `engine.chargingFlow` | Update (10Hz) |
| `CVCSScreen` | Letdown flow | `GetLetdownFlow()` → `engine.letdownFlow` | Update (10Hz) |
| `CVCSScreen` | VCT level | `GetVCTLevel()` → `engine.vctState.Level_percent` | Update (10Hz) |
| `CVCSScreen` | RCS boron | `GetBoronConcentration()` → `engine.rcsBoronConcentration` | Update (10Hz) |
| `CVCSScreen` | VCT boron | `GetVCTBoronConcentration()` → `engine.vctState.BoronConcentration_ppm` | Update (10Hz) |
| `SteamGeneratorScreen` | T_hot / T_cold | `GetThot()` / `GetTcold()` → `engine.T_hot` / `engine.T_cold` | Update (10Hz) |
| `SteamGeneratorScreen` | SG pressure | `GetSGSecondaryPressure_psig()` → `engine.sgSecondaryPressure_psia - 14.7` | Update (10Hz) |
| `SteamGeneratorScreen` | SG heat transfer | `GetSGHeatTransfer()` → `engine.sgHeatTransfer_MW` | Update (10Hz) |
| `SteamGeneratorScreen` | SG boiling | `GetSGBoilingActive()`, `GetSGBoilingIntensity()` | Update (10Hz) |
| `SteamGeneratorScreen` | SG secondary temp | `GetSGSecondaryTemp()` → `engine.T_sg_secondary` | Update (10Hz) |

### B.3 ReactorOperatorScreen (via MosaicBoard)

| UI Script | Gauge Type | Source | Lifecycle |
|---|---|---|---|
| `MosaicGauge` | NeutronPower | `board.GetValue(NeutronPower)` → `ReactorController` | Update (~10Hz) |
| `MosaicGauge` | ThermalPower | `board.GetValue(ThermalPower)` → `ReactorController` | Update (~10Hz) |
| `MosaicGauge` | Tavg | `board.GetValue(Tavg)` → `ReactorController` or `ScreenDataBridge` | Update (~10Hz) |
| `MosaicGauge` | Thot / Tcold | `board.GetValue(Thot/Tcold)` | Update (~10Hz) |
| `MosaicGauge` | FlowFraction | `board.GetValue(FlowFraction)` | Update (~10Hz) |
| `MosaicGauge` | Boron / Xenon | `board.GetValue(Boron/Xenon)` | Update (~10Hz) |
| `ReactorOperatorScreen` | Reactor mode | `_reactor.IsTripped`, `_reactor.NeutronPower` | Update |
| `ReactorOperatorScreen` | Time/compression | `Time.time`, `Time.timeScale` | Update |

---

## Appendix C — Simulation Update Methods → Subsystems → Phase

| Method | Subsystems Updated | Update Phase | Called From | File |
|---|---|---|---|---|
| `TimeAcceleration.Tick()` | Wall/sim clocks | Pre-physics (once/frame) | `RunSimulation()` | TimeAcceleration.cs |
| `RCPSequencer.GetTargetRCPCount()` | RCP startup schedule | §1 (per tick) | `StepSimulation()` | RCPSequencer.cs |
| `RCPSequencer.GetEffectiveRCPContribution()` | RCP ramp fractions, flow | §1 (per tick) | `StepSimulation()` | RCPSequencer.cs |
| `CVCSController.UpdateHeaterPID()` | Heater PID state | §1B (per tick) | `StepSimulation()` | CVCSController.cs |
| `CVCSController.CalculateHeaterState()` | Heater mode/power | §1B (per tick) | `StepSimulation()` | CVCSController.cs |
| `CVCSController.UpdateSpray()` | Spray valve dynamics | §1B-Spray (per tick) | `StepSimulation()` | CVCSController.cs |
| `RHRSystem.Update()` | RHR heat, isolation | §1C (per tick) | `StepSimulation()` | RHRSystem.cs |
| `SolidPlantPressure.Update()` | Solid PZR P-T-V, surge flow, mass | §2 Regime 1 (per tick) | `StepSimulation()` | SolidPlantPressure.cs |
| `RCSHeatup.IsolatedHeatingStep()` | PZR isolated heating | §2 Regime 1/2 (per tick) | `StepSimulation()` | RCSHeatup.cs |
| `SGMultiNodeThermal.Update()` | SG nodes, heat absorption, steam | §2 All regimes (per tick) | `StepSimulation()` | SGMultiNodeThermal.cs |
| `RCSHeatup.BulkHeatupStep()` | RCS bulk T, P; calls CoupledThermo | §2 Regime 2/3 (per tick) | `StepSimulation()` | RCSHeatup.cs |
| `CoupledThermo.SolveEquilibrium()` | P-T-V coupling, canonical mass | §2 (inside BulkHeatupStep) | `BulkHeatupStep()` | CoupledThermo.cs |
| `PressurizerPhysics.ThreeRegionUpdate()` | Two-phase PZR dynamics | §2 (if bubble formed) | `StepSimulation()` | PressurizerPhysics.cs |
| `LoopThermodynamics.CalculateLoopTemperatures()` | T_hot, T_cold, T_avg | §3 (per tick) | `StepSimulation()` | LoopThermodynamics.cs |
| `UpdateBubbleFormation(dt)` | 7-phase bubble state machine | §4 (per tick) | `StepSimulation()` | HeatupSimEngine.BubbleFormation.cs |
| `CVCSController.Update()` | PI level control, charging/letdown | §5 (per tick) | `UpdateCVCSFlows()` | CVCSController.cs |
| `VCTPhysics.Update()` | VCT level, divert, makeup, boron | §5 (per tick) | `UpdateCVCSFlows()` | VCTPhysics.cs |
| `RVLISPhysics.Update()` | Vessel level indication | §6 (per tick) | `UpdateRVLIS()` | RVLISPhysics.cs |
| `AlarmManager` (via UpdateAnnunciators) | All alarm booleans | §6 (per tick) | `StepSimulation()` | AlarmManager.cs |
| `ProcessAlarmEdges()` | Edge detection, event logging | §6 (per tick) | `UpdateAnnunciators()` | HeatupSimEngine.Alarms.cs |
| `SteamDumpController.Update()` | Steam dump demand, heat | §7 (per tick, if T>550°F) | `UpdateHZPSystems()` | SteamDumpController.cs |
| `HZPStabilizationController.Update()` | HZP state machine | §7 (per tick, if T>550°F) | `UpdateHZPSystems()` | HZPStabilizationController.cs |
| `UpdateInventoryAudit(dt)` | Mass conservation check | §8 (per tick) | `StepSimulation()` | HeatupSimEngine.cs |
| `AddHistory()` | Rolling 240-point history arrays | Post-tick (periodic) | `RunSimulation()` | HeatupSimEngine.Logging.cs |

---

*End of Telemetry Flow Audit*
*Document Version: 1.0*
