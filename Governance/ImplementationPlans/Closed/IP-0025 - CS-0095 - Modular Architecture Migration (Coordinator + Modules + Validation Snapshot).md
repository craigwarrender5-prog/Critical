---
IP ID: IP-0025
DP Reference: NONE (CS-driven exception requested on 2026-02-15)
Primary Driver: CS-0095
Title: CS-0095 - Modular Architecture Migration (Coordinator + Modules + Validation Snapshot)
Status: CLOSED
Date: 2026-02-16
Mode: EXECUTED/CLOSEOUT
Source of Scope Truth: Governance/IssueRegister/issue_register.json (CS-0095)
Constraint: Planning artifact only; no code changes in this step
---

# IP-0025 - CS-0095 - Modular Architecture Migration (Coordinator + Modules + Validation Snapshot)

## Closeout Record (Authoritative)

- Final status: `CLOSED`
- Final commit: `988749b`
- Final tag: `IP-0025-StageE`
- Final evidence: `Governance/Issues/IP-0025_StageE_PZRPackaging_Equivalence_2026-02-16_075421.md`

### Stage tags
- `IP-0025-StageA`
- `IP-0025-StageB`
- `IP-0025-StageC`
- `IP-0025-StageD`
- `IP-0025-StageE`

### Stage artifacts
- `Governance/Issues/IP-0025_StageA_Equivalence_2026-02-15_211605.md`
- `Governance/Issues/IP-0025_StageB_SnapshotFidelity_2026-02-15_212300.md`
- `Governance/Issues/IP-0025_StageC_LedgerGate_2026-02-15_212752.md`
- `Governance/Issues/IP-0025_StageD_LegacyOrderParity_2026-02-15_213315.md`
- `Governance/Issues/IP-0025_StageE_PZRPackaging_Equivalence_2026-02-16_075421.md`

## 1) Intent and Hard Constraints

### Intent
Transition the simulator to a modular architecture using an incremental strangler-fig migration:
- Introduce coordinator + module contract.
- Wrap current simulator behavior in a legacy adapter module.
- Introduce PlantState + StepSnapshot as stable validation/UI surface.
- Introduce PlantBus + TransferLedger for explicit transfer accounting.
- Establish repeatable extraction pattern with feature flags.
- Prepare PZR-first extraction packaging (without changing PZR physics).

### Hard constraints (non-negotiable)
- No big-bang rewrite.
- No physics changes in Stage A through Stage C.
- Stage D introduces extraction pipeline scaffolding only.
- Validation must consume a stable snapshot boundary and avoid module internals.
- Modules do not call each other directly; coupling is only via PlantState and/or PlantBus/TransferLedger.
- Simulator remains runnable after every stage checkpoint.
- Stage A deterministic equivalence runs must freeze seed, timestep, and time-dependent initialization inputs.
- Stage B authority rule: `PlantState` is projection-only and MUST have a single writer (`LegacyStateBridge`) during Stage B.
- Stage B snapshot rule: published `StepSnapshot` must be immutable after publish (immutable records/value DTOs or deep-copied DTO payloads only; no live references into mutable legacy runtime fields).
- Stage C authority rule: `TransferLedger` MUST capture actual transfers from the same mutation path; no inferred/post-hoc decorative ledgering.
- Stage D order rule: coordinator execution order is provisional until legacy `StepSimulation(dt)` causal-order parity is proven and documented.
- Stage D comparator rule: comparator shadow runs MUST be side-effect free and may not mutate legacy or authoritative state.

### Scope source
- Sole driver: `CS-0095`.
- No Domain Plan association for this IP by explicit request.

## 2) Stage 0 - Discovery and Entry-Point Mapping (No Code Changes)

### 2.1 Simulation step entry points

| Path | Class | Method | Role |
|---|---|---|---|
| `Assets/Scripts/Validation/HeatupSimEngine.cs:936` | `HeatupSimEngine` | `IEnumerator RunSimulation()` | Main loop; initializes and repeatedly advances physics by `dt`. |
| `Assets/Scripts/Validation/HeatupSimEngine.cs:993` | `HeatupSimEngine` | `RunPhysicsStep(dt)` callsite | Current step dispatch call from main loop. |
| `Assets/Scripts/Validation/HeatupSimEngine.RuntimePerf.cs:119` | `HeatupSimEngine` | `void RunPhysicsStep(float dt)` | Per-step dispatcher; calls `StepSimulation(dt)` and publishes telemetry snapshot. |
| `Assets/Scripts/Validation/HeatupSimEngine.cs:1093` | `HeatupSimEngine` | `void StepSimulation(float dt)` | Core monolithic physics/state advancement function (legacy seam). |

### 2.2 Validation entry points (stage checks and hooks)

| Path | Class | Method | Role |
|---|---|---|---|
| `Assets/Scripts/UI/Editor/IP0024CheckpointRunner.cs:332` | `Critical.Validation.IP0024CheckpointRunner` | `RunRemainingCloseoutTranche()` | Editor stage-check runner; executes Stage D/Stage H evidence path and writes governance artifacts. |
| `Assets/Scripts/UI/Editor/IP0024CheckpointRunner.cs:525` | `Critical.Validation.IP0024CheckpointRunner` | `EvaluateStageD(...)` | Stage D gate evaluation logic. |
| `Assets/Scripts/UI/Editor/IP0024CheckpointRunner.cs:583` | `Critical.Validation.IP0024CheckpointRunner` | `EvaluateStageH(...)` | Stage H deterministic evidence evaluation logic. |
| `Assets/Scripts/UI/Editor/IP0023CheckpointRunner.cs:105` | `Critical.Validation.IP0023CheckpointRunner` | `RunAllCheckpointsAndRecommendation()` | Multi-checkpoint validation runner used in current governance workflow. |
| `Assets/Scripts/Validation/HeatupValidationVisual.cs:411` | `HeatupValidationVisual` | `DrawValidationTab(contentArea)` dispatch | Runtime validation-tab hook used while simulator runs. |
| `Assets/Scripts/Validation/HeatupValidationVisual.TabValidation.cs:59` | `HeatupValidationVisual` | `DrawValidationTab(Rect area)` | Validation tab renderer entry. |
| `Assets/Scripts/Validation/HeatupValidationVisual.TabValidation.cs:158` | `HeatupValidationVisual` | `DrawValidationChecks(...)` | PASS/FAIL check rendering path. |

### 2.3 UI read points (dashboard pull surface)

| Path | Class | Method | Role |
|---|---|---|---|
| `Assets/Scripts/Validation/HeatupValidationVisual.cs:256` | `HeatupValidationVisual` | `Update()` | Pulls `_telemetrySnapshot = engine.GetTelemetrySnapshot()` each frame. |
| `Assets/Scripts/Validation/HeatupValidationVisual.cs:351` | `HeatupValidationVisual` | `OnGUI()` | Renders dashboard tabs from cached snapshot/engine state. |
| `Assets/Scripts/Validation/HeatupSimEngine.RuntimePerf.cs:90` | `HeatupSimEngine` | `GetTelemetrySnapshot()` | Current typed snapshot API for validation dashboard. |
| `Assets/Scripts/UI/ScreenDataBridge.cs:195` | `ScreenDataBridge` | `ResolveSources()` | Locates `HeatupSimEngine` and other providers for panel data flow. |
| `Assets/Scripts/UI/ScreenDataBridge.cs:226` | `ScreenDataBridge` | `GetTavg()` (representative) | Current pattern: direct reads from `HeatupSimEngine` public fields. |

### 2.4 Top-level call map (current flow)

1. `HeatupSimEngine.RunSimulation()` initializes and enters timestep loop.
2. Loop calls `RunPhysicsStep(dt)`.
3. `RunPhysicsStep(dt)` calls `StepSimulation(dt)` (legacy monolithic step).
4. `RunPhysicsStep(dt)` calls `PublishTelemetrySnapshot()`.
5. `HeatupValidationVisual.Update()` pulls snapshot via `GetTelemetrySnapshot()`.
6. `HeatupValidationVisual.OnGUI()` renders tabs and validation checks.
7. Optional offline/editor validation runs via runner entry methods (for example `IP0024CheckpointRunner.RunRemainingCloseoutTranche()`).

### Stage 0 Exit Criteria
- IP records exact paths and symbols for sim step, validation entry points, and UI read paths.
- Top-level call map is documented.

## 3) Stage A - Modular Shell (Coordinator + Module Contract) - Behavior Unchanged

### Work
1. Add `IPlantModule` contract:
   - `Initialize(...)`
   - `Step(dt, ...)`
   - `Shutdown()`
2. Add `PlantSimulationCoordinator`:
   - Owns timestep loop/module order.
   - Contains no physics.
3. Add `LegacySimulatorModule` adapter:
   - Wraps existing legacy simulator stepping behavior.
   - Coordinator runs only this module in Stage A.
4. Add centralized feature flags (default all `false`):
   - `UseModularPZR`
   - `UseModularCVCS`
   - `UseModularRHR`
   - `UseModularRCP`
   - `UseModularReactor`
   - `UseModularRCS`
   - Optional comparator flags: `EnableComparator_<System>`
5. Add deterministic Stage A equivalence check:
   - Run pre-Stage-A legacy baseline tag/build.
   - Run Stage A coordinator + `LegacySimulatorModule` path.
   - Freeze deterministic controls for both runs: fixed seed(s), fixed timestep/config, and fixed time-dependent initialization inputs.
   - Compare key telemetry over matched timesteps: pressure, PZR level, and mass totals.
   - Fail Stage A if drift exceeds defined tolerances.

### Evidence
- Compile/build success.
- Baseline run log explicitly showing coordinator path active.
- Existing validation still executes and records pass/fail outputs (logic unchanged).
- Deterministic equivalence artifact comparing legacy baseline vs coordinator+legacy path.
- Comparison report includes tolerances and max error for pressure, PZR level, and mass totals.
- Deterministic-controls artifact records exact seed(s), timestep config, and initialization-time inputs used for both compared runs.

### Exit criteria
- Simulator behavior remains equivalent with coordinator + legacy module path.
- Stage A deterministic equivalence check passes all defined tolerances.

### Rollback point
- Commit boundary `IP-0025-StageA`.

## 4) Stage B - PlantState v1 + LegacyStateBridge + StepSnapshot (Behavior Unchanged)

### Work
1. Create `PlantState` v1 with minimum extraction/validation surface:
   - Time/dt.
   - RCS summary signals (pressure, key temperatures, mass if available).
   - PZR summary signals (pressure, level, heater power, spray flow, vapor/liquid mass if available).
   - CVCS summary (charging/letdown).
   - RHR state (aligned/on, flow if available).
   - RCP state (on/off/speed/flow if available).
   - Reactor power/heat input if available.
2. Add `LegacyStateBridge.Export()`:
   - Copies legacy runtime values into `PlantState` after each step.
3. Define and publish `StepSnapshot` each step:
   - `time`, `dt`.
   - `PlantState`.
   - Placeholders for `TransferLedger` and telemetry.
4. Validation adapter wiring:
   - Validation entry points can consume `StepSnapshot` without full validation rewrite in this stage.
5. Enforce Stage B authority guardrails:
   - `PlantState` write path is restricted to `LegacyStateBridge` only.
   - `StepSnapshot` payload is immutable after publish and uses immutable/value DTO payloads or deep-copied DTOs only.
   - `StepSnapshot` must not expose live references to mutable legacy engine fields.
   - No module writes to `PlantState` in Stage B.
6. Add Stage B snapshot-fidelity equivalence check:
   - At identical step boundaries, compare direct legacy reads vs `StepSnapshot` fields used by validation/UI.
   - Require exact match where discrete/equality-safe; otherwise enforce bounded tolerance for floating-point fields.
   - Fail Stage B if snapshot view diverges from direct legacy source for scoped signals.

### Evidence
- Compile/build success.
- Run log showing sample `PlantState` payload values populated.
- Validation still runs.
- Proof artifact (unit test or runtime assertion log) that only `LegacyStateBridge` can mutate `PlantState` in Stage B.
- Snapshot-fidelity artifact showing direct legacy vs `StepSnapshot` parity for scoped signals.
- Snapshot immutability artifact (unit test or runtime assertion log) proving no post-publish mutation and no retained live legacy references.

### Exit criteria
- `StepSnapshot` exists and validation can consume it without direct module internals.
- No writable shadow authority exists for `PlantState` in Stage B.
- `StepSnapshot` immutability boundary is enforced post-publish (no mutable live-reference leakage).
- Stage B snapshot-fidelity equivalence check passes for scoped validation/UI signals.

### Rollback point
- Commit boundary `IP-0025-StageB`.

## 5) Stage C - PlantBus + TransferLedger (Conservation Surface)

### Work
1. Implement `PlantBus` with per-step transfer event collection:
   - Mass transfer events (`from`, `to`, quantity, thermal basis).
   - Heat transfer events (`from`, `to`, watts or joules over `dt`).
2. Implement `TransferLedger` and embed in `StepSnapshot`.
3. Coordinator accounting ownership:
   - Clear bus at step start.
   - Execute module steps.
   - Apply/record transfer reconciliation once per step.
4. Migrate one low-risk conservation validation check to use ledger data.
5. Enforce Stage C ledger authority guardrails:
   - Any legacy state mutation that changes mass/energy/flow in Stage C must emit a corresponding ledger event in the same step.
   - Ledger entries must be sourced from actual transfer/mutation path, not inferred from end-of-step deltas.
   - Add an unledgered-mutation detector (assert/log gate) for scoped migrated paths.

### Evidence
- Compile/build success.
- Run log includes ledger entries.
- Validation output includes the migrated ledger-backed check.
- Ledger parity evidence showing migrated path authority delta matches ledger transfer totals within tolerance.

### Exit criteria
- Ledger is populated and at least one conservation check uses it successfully.
- For migrated path(s), no unledgered transfer mutation events occur.

### Rollback point
- Commit boundary `IP-0025-StageC`.

## 6) Stage D - Extraction Pipeline Scaffolding (PZR-First Readiness)

### Work
1. Add module stubs (no physics moved in this stage):
   - `PressurizerModule`
   - `CVCSModule`
   - `RHRModule`
   - `RCPModule`
   - `ReactorModule`
   - `RCSModule`
2. Produce a legacy-order parity map before locking coordinator order:
   - Document top-level legacy `StepSimulation(dt)` sequence from callsites/comments.
   - Build an order parity matrix: `legacy phase -> proposed module slot`.
   - Flag any ordering mismatches that would alter causal transfer behavior.
3. Establish deterministic coordinator execution order (provisional until parity sign-off):
   - Initial target: `Reactor -> RCP -> RCS -> PZR -> CVCS -> RHR -> transfer finalize -> snapshot publish -> validation hook`
   - If parity analysis shows mismatch, adjust to legacy-equivalent order for first extraction pass.
4. Define extraction recipe in code and in this IP:
   - Feature flag enables extracted module.
   - Legacy bypass disables legacy updates for that subsystem.
   - Optional comparator mode runs shadow compute vs applied compute with tolerances.
   - Comparator shadow compute writes to temporary shadow structures only and is side-effect free.
   - Comparator applies exactly one authoritative path to mutable state per step.

### Evidence
- Compile/build success.
- Documentation of extraction recipe exists in code comments and this IP.
- Legacy-order parity artifact (phase map + decision record) exists and is linked from Stage D notes.

### Exit criteria
- Repo contains coordinator + contracts + state + ledger + module stubs + feature flags ready for PZR extraction.
- Coordinator order is explicitly accepted as legacy-causal-equivalent (or revised accordingly) before extraction activation.

### Rollback point
- Commit boundary `IP-0025-StageD`.

## 7) Stage E - PZR Extraction Packaging

### Decision in this IP
- **Status: DEFERRED** (follow-on execution after scaffolding stages pass).

### Deferred scope note
- PZR remodel physics already exists; extraction must package existing behavior behind `PressurizerModule` boundary only.
- No physics behavior change is allowed in packaging step.
- PZR extraction boundary includes surge as bus-mediated transfer intents, finalized by coordinator to avoid direct RCS coupling.

### Entry condition for deferred Stage E
- Stage A through D evidence complete and accepted.

### Stage E activation discipline (hard gate)
- `PressurizerModule` MUST NOT read legacy mutable fields directly.
- `PressurizerModule` MUST NOT write into legacy state directly.
- `PressurizerModule` MUST NOT mutate RCS state fields directly.
- Surge, spray, and heater-energy effects MUST NOT bypass `TransferLedger`; they must be emitted as transfer intents and finalized by coordinator reconciliation.
- Stage E state path MUST be: local module state -> transfer intents -> coordinator finalize -> `StepSnapshot` publish.
- Any direct mutable legacy-state or direct RCS-state mutation by the module is a Stage E gate failure.

## 8) Repeatable Extraction Pattern (Canonical Recipe)

1. Create/activate target module behind `UseModular<System>` feature flag.
2. Keep legacy path active by default.
3. Add legacy bypass switch for target subsystem when module is active.
4. Optional comparator shadow mode:
   - Run legacy and modular calculations in same step.
   - Shadow run must be side-effect free and write only to temporary comparator structures.
   - Compare selected outputs with defined tolerances.
   - Apply exactly one authoritative path to mutable state.
5. Promote only when:
   - Build compiles.
   - Baseline run remains stable.
   - Validation gate passes.
   - Target module follows the same authority path only: local module state -> transfer intents -> coordinator finalize -> `StepSnapshot` publish.

## 9) File Checklist by Stage (Initial Touch Set)

### Stage 0 (discovery only, no edits)
- `Assets/Scripts/Validation/HeatupSimEngine.cs`
- `Assets/Scripts/Validation/HeatupSimEngine.RuntimePerf.cs`
- `Assets/Scripts/Validation/HeatupValidationVisual.cs`
- `Assets/Scripts/Validation/HeatupValidationVisual.TabValidation.cs`
- `Assets/Scripts/UI/ScreenDataBridge.cs`
- `Assets/Scripts/UI/Editor/IP0024CheckpointRunner.cs`
- `Assets/Scripts/UI/Editor/IP0023CheckpointRunner.cs`

### Stage A
- Add `Assets/Scripts/Simulation/Modular/IPlantModule.cs`
- Add `Assets/Scripts/Simulation/Modular/PlantSimulationCoordinator.cs`
- Add `Assets/Scripts/Simulation/Modular/Modules/LegacySimulatorModule.cs`
- Add `Assets/Scripts/Simulation/Modular/ModularFeatureFlags.cs`
- Modify `Assets/Scripts/Validation/HeatupSimEngine.cs` (delegate stepping to coordinator path)
- Modify `Assets/Scripts/Validation/HeatupSimEngine.RuntimePerf.cs` (coordinator integration seam)

### Stage B
- Add `Assets/Scripts/Simulation/Modular/State/PlantState.cs`
- Add `Assets/Scripts/Simulation/Modular/State/StepSnapshot.cs`
- Add `Assets/Scripts/Simulation/Modular/State/LegacyStateBridge.cs`
- Modify `Assets/Scripts/Validation/HeatupSimEngine.RuntimePerf.cs` (snapshot publish)
- Modify `Assets/Scripts/Validation/HeatupValidationVisual.cs` (snapshot adapter read path)
- Modify `Assets/Scripts/Validation/HeatupValidationVisual.TabValidation.cs` (adapter consumption boundary)

### Stage C
- Add `Assets/Scripts/Simulation/Modular/Transfer/PlantBus.cs`
- Add `Assets/Scripts/Simulation/Modular/Transfer/TransferLedger.cs`
- Add `Assets/Scripts/Simulation/Modular/Transfer/TransferEvent.cs`
- Modify `Assets/Scripts/Simulation/Modular/PlantSimulationCoordinator.cs` (clear/apply/finalize transfer cycle)
- Modify one existing validation check file (selected low-risk check; expected in `Assets/Scripts/Validation/HeatupValidationVisual.TabValidation.cs` or an editor runner)

### Stage D
- Add `Assets/Scripts/Simulation/Modular/Modules/PressurizerModule.cs`
- Add `Assets/Scripts/Simulation/Modular/Modules/CVCSModule.cs`
- Add `Assets/Scripts/Simulation/Modular/Modules/RHRModule.cs`
- Add `Assets/Scripts/Simulation/Modular/Modules/RCPModule.cs`
- Add `Assets/Scripts/Simulation/Modular/Modules/ReactorModule.cs`
- Add `Assets/Scripts/Simulation/Modular/Modules/RCSModule.cs`
- Add `Assets/Scripts/Simulation/Modular/Validation/ModuleComparator.cs`
- Modify `Assets/Scripts/Simulation/Modular/PlantSimulationCoordinator.cs` (deterministic order + comparator orchestration)
- Modify `Assets/Scripts/Simulation/Modular/ModularFeatureFlags.cs` (per-system comparator flags)

### Stage E (deferred)
- Modify `Assets/Scripts/Simulation/Modular/Modules/PressurizerModule.cs` (packaging-only extraction)
- Modify legacy bypass wiring in coordinator and flag map

## 10) How to Run Validation (Existing Repo Scripts/Commands)

### Editor menu paths
- `Critical/Run IP-0024 Remaining Closeout Tranche`
  - Entry: `Critical.Validation.IP0024CheckpointRunner.RunRemainingCloseoutTranche()`
  - File: `Assets/Scripts/UI/Editor/IP0024CheckpointRunner.cs:332`
- `Critical/Run IP-0023 All Checkpoints + Recommendation`
  - Entry: `Critical.Validation.IP0023CheckpointRunner.RunAllCheckpointsAndRecommendation()`
  - File: `Assets/Scripts/UI/Editor/IP0023CheckpointRunner.cs:105`
- `Critical/Run Stage E (IP-0015)`
  - Entry: `Critical.Validation.StageERunner.RunStageE()`
  - File: `Assets/Scripts/UI/Editor/StageERunner.cs:28`

### Batchmode examples
- `"C:\Program Files\Unity\Hub\Editor\6000.3.4f1\Editor\Unity.exe" -batchmode -quit -projectPath "c:\Users\craig\Projects\Critical" -executeMethod Critical.Validation.IP0024CheckpointRunner.RunRemainingCloseoutTranche -logFile "HeatupLogs\Unity_IP0024_Checkpoint.log"`
- `"C:\Program Files\Unity\Hub\Editor\6000.3.4f1\Editor\Unity.exe" -batchmode -quit -projectPath "c:\Users\craig\Projects\Critical" -executeMethod Critical.Validation.IP0023CheckpointRunner.RunAllCheckpointsAndRecommendation -logFile "HeatupLogs\Unity_IP0023_Checkpoints.log"`

### Runtime validation dashboard path
- `HeatupValidationVisual` runtime checks:
  - Snapshot pull: `Assets/Scripts/Validation/HeatupValidationVisual.cs:259`
  - Validation tab dispatch: `Assets/Scripts/Validation/HeatupValidationVisual.cs:411`
  - Validation checks draw: `Assets/Scripts/Validation/HeatupValidationVisual.TabValidation.cs:158`

## 11) Rollback Strategy

### Commit boundaries
- Stage A: commit tag `IP-0025-StageA`.
- Stage B: commit tag `IP-0025-StageB`.
- Stage C: commit tag `IP-0025-StageC`.
- Stage D: commit tag `IP-0025-StageD`.
- Stage E (deferred): separate future commit boundary.

### Runtime rollback switches
- Keep all modular flags OFF to force legacy behavior:
  - `UseModularPZR=false`
  - `UseModularCVCS=false`
  - `UseModularRHR=false`
  - `UseModularRCP=false`
  - `UseModularReactor=false`
  - `UseModularRCS=false`
- Keep comparator flags OFF to eliminate shadow overhead.
- Coordinator remains valid rollback carrier by running `LegacySimulatorModule` only.

### Operational rollback rule
- If any stage fails equivalence or validation gate, revert to previous stage commit boundary and rerun baseline validation before proceeding.

## 12) Stage Gate Summary (Sim Must Stay Runnable)
- Stage A gate: coordinator + legacy module produces baseline-equivalent run, proven by deterministic equivalence against pre-Stage-A legacy baseline with fixed seed/timestep/time-init controls.
- Stage B gate: StepSnapshot boundary available, validation remains executable, `PlantState` is single-writer projection-only, snapshot immutability is enforced post-publish, and direct-vs-snapshot fidelity checks pass.
- Stage C gate: TransferLedger populated, one conservation check migrated, and no unledgered mutation on migrated path(s).
- Stage D gate: extraction scaffolding complete with deterministic order and feature-flag/bypass/comparator recipe, comparator shadow side effects prohibited, and legacy-order parity sign-off.
- Stage E: deferred pending explicit authorization.
- Stage E (when authorized): PZR extraction must pass Stage E activation discipline with no direct mutable legacy or direct RCS coupling writes.
