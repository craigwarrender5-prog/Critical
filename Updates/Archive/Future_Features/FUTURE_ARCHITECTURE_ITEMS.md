# Future Architecture Items
## Critical: Master the Atom -- NSSS Simulator

**Created:** 2026-02-13
**Template:** `FUTURE_FEATURE_TEMPLATE.md`

This document contains two future feature entries conforming to the standard template. These items are sequenced: Item 1 must substantially complete before Item 2 begins.

**Absorption notice:** Architecture Hardening (Item 1, v5.7.0.0) absorbs FF-09 (CVCS Actuator Dynamics & Control Coupling) and FF-10 (Regime Dispatch Hardening) from `TRIAGE_v5.4.1_PostStabilization.md`. Detailed code-location issue descriptions and per-issue acceptance criteria are maintained in TRIAGE. This document defines structural refactoring intent and architectural acceptance criteria only.

---

# Thermal / Mass / Flow Architecture Hardening

## Status

`Planned`

## Priority

`Medium`

## Motivation

The simulator has grown through iterative patch cycles (v5.0.0 through v5.4.1), each fixing specific physics or accounting bugs. While each patch was correct in isolation, the cumulative result is an architecture with duplicate state, implicit module coupling, inconsistent units, branch-based regime dispatch, and low testability. Further physics work (Phase 2+) will compound these issues unless the structural foundation is strengthened.

## Problem Statement

Five specific architectural limitations have been identified:

- **Duplicate state:** Multiple representations of the same physical quantity exist (e.g., `physicsState.PZRWaterMass` vs `solidPlantState.PzrWaterMass` -- the former is stale during solid ops, the latter is current). The v5.4.1 single-source-of-truth wiring was a tactical fix; a structural fix would eliminate the duplication entirely.

- **Implicit module coupling:** Multiple modules modify overlapping state without explicit ownership contracts. The boundary between "who owns what" is defined by convention and comments, not by interface contracts. Specific code-level instances documented in TRIAGE FF-09 (dual pressure/level control between CVCSController and SolidPlantPressure).

- **Inconsistent units:** Some modules work in ft3, others in gallons; some in lbm, others in lbm/ft3 with implicit volume. Unit conversions are scattered across call sites rather than centralized at module boundaries.

- **Branch-based regime dispatch:** The main simulation loop uses nested boolean chains to dispatch to different physics paths. Adding new regimes or transition states requires threading new booleans through the entire chain. Specific code-level instances documented in TRIAGE FF-10 (duplicate phase flags, magic-number thresholds, fragile inter-step state communication).

- **Low testability:** Physics modules are tightly coupled to `SystemState` (a large mutable struct). Unit testing requires constructing the entire struct. There are no interface abstractions for testing modules in isolation.

## Scope

| Goal | Description |
|------|-------------|
| **Eliminate duplicate state** | Each physical quantity has exactly one authoritative storage location. All other references are reads, not independent copies. The `physicsState.PZRWaterMass` / `solidPlantState.PzrWaterMass` split is the canonical example to resolve. |
| **Define explicit module boundaries** | Each physics module has a defined input/output contract. Inputs are read-only snapshots; outputs are explicit delta or state-update structs. No module reaches into another module's state. Resolves FF-09 dual-control issue (see TRIAGE). |
| **Centralize unit handling** | Unit conversions happen at module boundaries, not scattered through logic. Consider typed wrappers (e.g., `MassLbm`, `VolumeFt3`, `PressurePsia`) at critical interfaces to prevent unit mismatch bugs at compile time. |
| **Regime dispatch via state machine** | Replace boolean-chain regime dispatch with an explicit state machine. Each regime is a named state with defined entry/exit criteria, an `Update()` method, and a clear owner for each physics module call. Transitions are first-class events, not side effects of boolean flips. Resolves FF-10 dispatch issues (see TRIAGE). |
| **Improve testability** | Physics modules accept input structs and return output structs. No global or singleton state. Unit tests can construct minimal inputs and verify outputs without building the full SystemState. |
| **Consistent naming conventions** | Standardize field naming across all modules: `_lbm` suffix for mass, `_psia` for pressure, `_ft3` for volume, `_gpm` for flow rate, `_degF` for temperature. |

## Non-Goals

- **No physics changes.** This is a structural refactor. The thermodynamic equations, control algorithms, and mass accounting logic must produce identical results before and after. Any behavioral difference is a regression.
- **No new features.** No new regimes, no new physics modules, no new UI elements.
- **No performance optimization.** Performance work is a separate item (see Multicore / Performance below). This refactor may incidentally improve cache behavior, but performance is not a goal.
- **Not a rewrite.** This is incremental restructuring of existing code, not a clean-room reimplementation. Each step should be independently testable and deployable.

## Technical Considerations

- **System boundaries:** All physics modules, the main simulation loop (HeatupSimEngine and partial classes), and SystemState are affected. See TRIAGE FF-09 and FF-10 for specific module/file/line references.
- **Data ownership:** Current ownership is implicit. Target: explicit ownership via input/output structs or documented module contracts. Each field in SystemState must have exactly one writer.
- **Thread safety:** Not directly applicable to this item, but the input/output contract pattern established here is a prerequisite for safe parallelization in the multicore item.
- **Determinism requirements:** Refactoring must not change execution order of floating-point operations. Regression tests must verify bit-identical results.
- **Performance impact:** Expected neutral. Struct-of-arrays vs array-of-structs considerations may arise but are not a primary concern. The typed wrapper approach (MassLbm, etc.) should use value types to avoid heap allocation.
- **Testing strategy:** Record baseline simulation traces (pressure, temperature, mass, flow at every interval log) BEFORE beginning refactor. Post-refactor traces must match within tight tolerances (0.01% for mass, 0.1 psi for pressure). Each refactoring PR is independently regression-tested.

## Risks

| Risk | Mitigation |
|------|------------|
| Behavioral regression from structural changes | Implement comprehensive regression test suite BEFORE beginning refactor. Record baseline traces and verify post-refactor traces match within tight tolerances. |
| Scope creep into physics fixes | Strict rule: if a refactoring step reveals a physics bug, document it as a separate issue. Do not fix it in the same commit. The refactor must be behavior-preserving. |
| Large merge conflicts with concurrent work | Perform refactoring in small, self-contained PRs. Each PR touches one module boundary or one state deduplication. Avoid large "big bang" restructurings. |
| Loss of institutional knowledge | Each structural change must update the corresponding implementation plan or architecture document. The "why" of every convention must be captured, not just the "what." |

## Dependencies

- v5.4.1 complete and validated (current baseline — stable)
- Comprehensive regression test suite must exist before beginning (baseline simulation traces for comparison)
- Phase 0 physics stabilization complete (v5.4.2.0 mass conservation residuals resolved)

## Acceptance Criteria

- [ ] No physical quantity has more than one authoritative storage location
- [ ] Each physics module has a documented input/output contract (can be comments or a lightweight interface)
- [ ] Unit conversions occur only at module boundaries, not within physics logic
- [ ] Regime dispatch uses an explicit state machine with named states and documented transitions
- [ ] At least one physics module (SolidPlantPressure recommended) has standalone unit tests that construct inputs and verify outputs without SystemState
- [ ] Regression test suite passes: baseline simulation traces match within defined tolerances
- [ ] No behavioral changes: all existing validation checks (INVENTORY AUDIT, VALIDATION STATUS, Mass Conservation, ValidateCalculations) produce identical results

## Notes

- **Estimated scheduling:** Post-Phase 2, assigned as v5.7.0.0.
- **Absorbs:** FF-09 (CVCS Actuator Dynamics & Control Coupling) and FF-10 (Regime Dispatch Hardening) from `TRIAGE_v5.4.1_PostStabilization.md`. Per-issue code locations, individual acceptance criteria, and risk mitigations are maintained in TRIAGE.
- **Incremental approach:** Recommended order: (1) document current ownership in SystemState comments, (2) deduplicate PZR mass fields, (3) extract SolidPlantPressure input/output contract, (4) add unit tests for SolidPlantPressure, (5) extract regime state machine, (6) centralize units. Each step is a separate PR.
- **Roadmap reference:** Technical Debt entry in `FUTURE_ENHANCEMENTS_ROADMAP.md`.

---

# Multicore / Performance and Determinism

## Status

`Planned`

## Priority

`Medium`

## Motivation

The simulator currently runs single-threaded on the Unity main thread. The simulation loop, physics calculations, logging, and UI updates all share the same frame budget. At high sim-speed ratios (target: 10x), this creates pressure on frame time:

- **Sim ratio cap:** Current effective maximum is ~6.5x real-time before frame drops. The target is 10x.
- **Timestep coupling:** The physics timestep is tied to the simulation speed and frame rate (`dt = simSpeed * Time.deltaTime`, capped). Physics fidelity degrades at high sim speeds and on slower hardware.
- **Single-thread bottleneck:** Physics modules execute sequentially even when they have no data dependencies within a timestep.
- **Future scaling concern:** As more physics modules are added (Phase 2: PORV/SV valves, CCW, heat exchangers, BRS, excess letdown), per-frame cost will increase. Without parallelism, the sim ratio cap will decrease.

The v5.4.1 actuator dynamics work highlighted a concrete example: at the current ~10s timestep, first-order lag filters with tau=10s have alpha=1.0 (passthrough). Finer timesteps (1s or 0.1s) would enable these filters to contribute meaningfully, but would require 10-100x more physics ticks per frame -- impossible without parallelism or a decoupled timestep.

## Problem Statement

The sim cannot reach 10x real-time at 60 FPS. The physics timestep is frame-rate-dependent, which means simulation fidelity varies with hardware performance and sim speed. There is no fixed-timestep accumulator, no substep budget, and no parallelism. These limitations will worsen as more physics modules are added in Phase 2+.

## Scope

| Goal | Description |
|------|-------------|
| **Fixed physics timestep** | Decouple the physics timestep from the frame rate. Physics runs at a fixed dt (e.g., 1s or 0.5s sim-time) using a substep accumulator. The frame rate determines how many substeps execute per frame, not the dt. Ensures numerical stability and reproducibility. |
| **Deterministic simulation** | Given identical initial conditions and inputs, the simulation produces bit-identical results on any hardware, at any frame rate, at any sim speed. Required for regression testing, save/load, and replay. |
| **Unity Job System evaluation** | Evaluate whether Unity's Job System (Burst-compiled, multi-threaded) can be used for independent physics modules. Identify which modules have no intra-tick data dependencies and can run in parallel. Candidates: SG thermal mass, RHR heat exchanger, VCT level tracking. |
| **Thread safety of shared state** | Define a threading model for SystemState access. Options: (a) copy-on-read with merge-on-write, (b) double-buffered state (read from tick N-1, write to tick N), (c) strict owner model where each module owns a partition of state. Choose one and enforce it. |
| **Profiling-driven optimization** | All performance work must be motivated by profiling data, not intuition. Establish a profiling baseline (frame time breakdown by module) before and after each optimization. No speculative optimization. |
| **Substep budget management** | Define a maximum number of substeps per frame to prevent spiral-of-death. If physics cannot keep up, reduce sim speed gracefully rather than dropping frames. |

## Non-Goals

- **No physics changes.** Multicore execution must produce results identical to single-threaded execution within defined tolerances. This is the hardest constraint.
- **No GPU compute.** The physics are branchy, stateful, and low-parallelism per module. GPU compute shaders are not appropriate. Stick to CPU parallelism via Job System.
- **No async/await patterns.** Unity's Job System is the preferred parallelism mechanism. Do not introduce C# Task/async patterns that conflict with Unity's threading model.
- **Not a premature optimization.** This work should only begin after physics are stable, profiling confirms physics cost is the bottleneck, and architecture hardening has established clean module boundaries.

## Technical Considerations

- **System boundaries:** The main simulation loop (`HeatupSimEngine.cs`), all physics modules, and the SystemState struct are affected. The logging and UI subsystems must be decoupled from physics tick execution.
- **Data ownership:** The threading model chosen (double-buffer, copy-on-read, or strict partition) determines how SystemState is accessed during parallel execution. The architecture hardening item (input/output contracts) is a prerequisite.
- **Thread safety:** Unity's Job System provides safety checks (NativeArray, DeallocateOnJobCompletion) to catch unsafe access at edit-time. No raw shared memory. All physics hot-path code must use only value types and static methods (Burst requirement).
- **Determinism requirements:** Critical. Floating-point results must be identical regardless of thread scheduling. This requires strict operation ordering within each module and no cross-module accumulation where order varies. Verified via replay test: run the same scenario twice and compare all state fields at every substep.
- **Performance impact:** Target: 10x real-time at stable 60 FPS. Current: ~6.5x. The gap is ~35% improvement needed, which may be achievable through fixed timestep alone (reduced overhead from variable-dt clamping and per-frame work) before parallelism is even needed.
- **Testing strategy:** Replay determinism test (bit-identical state traces from two runs). Profiling before/after each optimization step. Regression test suite against single-threaded baseline (mass: 0.01 lbm, pressure: 0.01 psi, temperature: 0.01 degF tolerance per substep).

## Risks

| Risk | Mitigation |
|------|------------|
| Non-determinism from floating-point ordering | Use strict operation ordering within each module. Avoid reduce-style operations where accumulation order varies. Verify determinism with replay test. |
| Race conditions on shared state | Enforce strict ownership model. Use Unity Job System safety checks. No raw shared memory. |
| Debugging difficulty with parallel execution | Maintain single-threaded execution path as compile-time option (`#if SINGLE_THREAD_PHYSICS`). All debugging and validation runs use single-threaded mode. |
| Spiral-of-death at high sim speed | Cap substeps per frame (e.g., max 20). If cap is hit, reduce sim speed by one notch and log a warning. |
| Job System overhead for small workloads | Profile before committing. If per-module workload is small enough that scheduling overhead dominates, keep those modules on main thread. |
| Burst compilation restrictions | Burst does not support managed types (strings, classes, virtual dispatch). Physics modules must use value types and static methods in hot path. Aligns with architecture hardening goal. |

## Dependencies

- **Architecture Hardening (Item 1) substantially complete.** Clean module boundaries, explicit input/output contracts, and eliminated duplicate state are prerequisites for safe parallelization. Attempting to parallelize the current tightly-coupled architecture would be fragile.
- Phase 0 physics stabilization complete (v5.4.2.0 mass conservation residuals resolved)
- Comprehensive regression test suite exists (baseline traces for comparison)
- Profiling data confirms physics cost is the performance bottleneck (not UI, not logging)

## Acceptance Criteria

- [ ] Physics runs at a fixed timestep independent of frame rate
- [ ] Simulation is deterministic: replay test produces bit-identical state traces
- [ ] Sim speed of 10x achieves stable 60 FPS on target hardware (define target hardware spec)
- [ ] Profiling baseline established: per-module frame-time breakdown documented before and after
- [ ] No physics result changes: regression test suite passes against single-threaded baseline within tolerances (mass: 0.01 lbm, pressure: 0.01 psi, temperature: 0.01 degF per substep)
- [ ] Single-threaded execution path preserved as compile-time option
- [ ] Substep budget management: sim speed reduces gracefully when physics cannot keep up
- [ ] Thread safety enforced via Job System safety checks (no raw shared memory)
- [ ] **Single-thread canonical:** Single-threaded execution is the canonical baseline for all validation and regression testing. Multicore mode is an optimization, not a replacement.
- [ ] **Parity-lock enforcement:** Multicore mode must match single-threaded baseline within defined tolerances (mass: 0.01 lbm, pressure: 0.01 psi, temperature: 0.01 degF per substep). If any state field diverges beyond tolerance during a validation run, multicore mode auto-disables and falls back to single-threaded execution with a logged warning.
- [ ] **Auto-disable on mismatch:** Runtime parity check runs at configurable intervals (default: every 100 substeps). If mismatch detected, multicore is disabled for the remainder of the session and the divergence is logged with full state dump for forensics.

## Notes

- **Estimated scheduling:** After Architecture Hardening (v5.7.0.0) is substantially complete, assigned as v5.7.1.0. Classified as Patch within the Architecture domain per VERSIONING_POLICY.md — single-thread remains canonical; multicore is parity-locked.
- **Fixed timestep may be sufficient.** The 10x target may be achievable through fixed timestep decoupling alone, without parallelism. Profiling will determine whether parallelism is needed.
- **Substepping as first step:** The most impactful single change is likely the fixed-timestep accumulator. This can be implemented and profiled independently of Job System parallelism.
- **Roadmap references:** Technical Debt entry "Simulation Timestep & Throughput Architecture" in `FUTURE_ENHANCEMENTS_ROADMAP.md`.

---

*Document created 2026-02-13. Conforms to FUTURE_FEATURE_TEMPLATE.md v1.0. No code changes associated with this document.*
