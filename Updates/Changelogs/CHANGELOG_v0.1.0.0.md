# CHANGELOG v0.1.0.0 — Foundational Authority Enforcement Release

**Date:** 2026-02-13
**Version:** v0.1.0.0
**Type:** Architecture — Mass Conservation Authority Enforcement
**Matching Implementation Plan:** IMPLEMENTATION_PLAN_v0.1.0.0.md
**Governing Document:** PROJECT_CONSTITUTION.md v0.1.0.0
**Issues Resolved:** CS-0001, CS-0002, CS-0003, CS-0004, CS-0005, CS-0006, CS-0007, CS-0008, CS-0013

---

## Release Summary

This release establishes the foundational invariants of primary mass conservation in the coupled physics regimes. The canonical mass architecture (designed in v5.3.0/v5.4.0) existed as dead code — the critical activation gate was never triggered. v0.1.0.0 activates, completes, enforces, and protects that architecture.

**Before v0.1.0.0:** The simulator was physically plausible.
**After v0.1.0.0:** The simulator is architecturally self-verifying.

---

## Phase A: Canonical Mode Activation

### Issues Addressed
- **CS-0001** (Critical): CoupledThermo canonical mass mode never activated
- **CS-0002** (High): TotalPrimaryMass_lb freezes after first step
- **CS-0005** (Medium): CVCS double-count guard works by accident
- **CS-0013** (Low): Session lifecycle resets for canonical baseline and solver log flag

### Files Modified
| File | Change |
|------|--------|
| `HeatupSimEngine.cs` | Added rebase blocks inside R2/R3 (before CVCS); redirected CVCS to `TotalPrimaryMass_lb` mutation; passed 10th arg at both `BulkHeatupStep` call sites; removed obsolete post-physics rebase |
| `CoupledThermo.cs` | Added `_solverModeLogged` flag; added `ResetSessionFlags()` method; added one-time solver mode announcement logging (canonical vs legacy) |
| `HeatupSimEngine.Init.cs` | Added `firstStepLedgerBaselined = false` reset; added `CoupledThermo.ResetSessionFlags()` call |

### Details
- **Keystone fix:** Both R2 (line 1247) and R3 (line 1430) call sites now pass `physicsState.TotalPrimaryMass_lb` to `BulkHeatupStep()`. The gate `useCanonicalMass = (totalPrimaryMass_lb > 0f)` is now true in all coupled steps.
- **Ledger rebase ordering:** Moved from post-physics to inside R2/R3, before CVCS mutation. Ensures ledger starts from clean component-sum baseline on first coupled step.
- **CVCS redirect:** Changed from `physicsState.RCSWaterMass += cvcsNetMass_lb` to `physicsState.TotalPrimaryMass_lb += cvcsNetMass_lb`. CVCS now enters through ledger mutation (INV-2).
- **Lifecycle safety:** `firstStepLedgerBaselined` and `_solverModeLogged` both reset on `InitializeSimulation()`, preventing stale state on second run.

---

## Phase B: Boundary Accounting Completion

### Issues Addressed
- **CS-0003** (Medium): Boundary flow accumulators never incremented
- **CS-0008** (Medium): No runtime solver mass conservation check

### Files Modified
| File | Change |
|------|--------|
| `HeatupSimEngine.cs` | Added accumulator increments at R1 solid ops, R2 pre-solver, R3 pre-solver |
| `HeatupSimEngine.CVCS.cs` | Added accumulator increments at post-physics CVCS (STABILIZE/PRESSURIZE path) |
| `RCSHeatup.cs` | Added post-solver conservation guard rail (>10 lb warn, >100 lb error) |
| `CoupledThermo.cs` | Added relief accumulator documentation comment |

### Details
- **Accumulator pattern:** `CumulativeCVCSIn_lb += Mathf.Max(0f, netMass)` / `CumulativeCVCSOut_lb += Mathf.Max(0f, -netMass)` at all 4 CVCS sites.
- **Double-count guard verified:** `regime3CVCSPreApplied` early-return in CVCS.cs prevents accumulator double-counting with R2/R3 pre-solver sites.
- **Relief accumulator:** Stays 0f by design — no two-phase relief physics exists (only solid-ops 450 psig setpoint in SolidPlantPressure.cs, where relief mass is folded into net CVCS balance).
- **Guard rail:** Post-solver check in `BulkHeatupStep()` computes `M_out = RCS + PZR_water + PZR_steam`, compares to canonical ledger. Diagnostics only — does not modify state.

---

## Phase C: Diagnostic Enforcement + UI Wiring

### Issues Addressed
- **CS-0006** (High): UpdatePrimaryMassLedgerDiagnostics() never called (dead code)
- **CS-0007** (Medium): No UI display for primary ledger drift

### Files Modified
| File | Change |
|------|--------|
| `HeatupSimEngine.cs` | Added `UpdatePrimaryMassLedgerDiagnostics()` call site; changed `primaryMassStatus` default from `"OK"` to `"NOT_CHECKED"` |
| `HeatupSimEngine.Init.cs` | Added diagnostic state reset (status, conservationOK, alarm, previous-state flags) |
| `HeatupValidationVisual.TabValidation.cs` | Added "Primary Ledger Drift" row using `DrawCheckRowThreeState` |

### Details
- **Call site:** Added at end of `StepSimulation()` after `UpdateInventoryAudit(dt)`. Diagnostic executes every physics timestep.
- **Default status:** Changed from `"OK"` to `"NOT_CHECKED"` to prevent false-confidence during pre-coupled phases (INV-6).
- **UI row:** Shows "Not checked yet" before first coupled step, then displays drift percentage with three-state coloring (green/yellow/red at 100/1000 lb thresholds).

---

## Phase D: Structural Hardening / Anti-Regression

### Issues Addressed
- **CS-0004** (Low): Pre-solver V*rho PZR mass computation redundant with solver

### Files Modified
| File | Change |
|------|--------|
| `RCSHeatup.cs` | Removed `= 0f` defaults from `BulkHeatupStep` last 3 parameters; updated `Step()` wrapper to forward `totalPrimaryMass_lb` |
| `CoupledThermo.cs` | Added 24-line LEGACY deprecation comment block; updated warning message to append "(DEPRECATED)" |
| `HeatupSimEngine.cs` | Added authority ownership comments at R2 header, R3 header, R2 CVCS block, R3 CVCS block |

### Details
- **Compile-time enforcement:** `BulkHeatupStep(... float totalPrimaryMass_lb)` has no default. Any caller that omits the argument gets a build error. Silent LEGACY fallback is eliminated.
- **LEGACY deprecation:** Else-branch in `SolveEquilibrium()` now carries explicit deprecation notice documenting when it fires, why it should never fire in production, and which legitimate callers remain (ValidateCalculations, standalone tests).
- **Authority ownership:** Structured comment blocks at R2/R3 headers document field authority (TotalPrimaryMass_lb = ENGINE, RCSWaterMass = SOLVER, PZR = SOLVER) and order of operations. CVCS blocks document sole mutation sites and guard references.

---

## Foundational Invariants Established

| Invariant | Description | Enforcement |
|-----------|-------------|-------------|
| INV-1 | Single Mass Authority | `TotalPrimaryMass_lb` is sole authority in coupled regimes |
| INV-2 | Boundary Mutation Rule | CVCS flows enter through ledger `+=`, never component override |
| INV-3 | No V*rho Authority | Solver accepts canonical mass, does not recompute total |
| INV-4 | Conservation by Construction | RCS = Total - PZR_water - PZR_steam (remainder logic) |
| INV-5 | Diagnostic Execution | Ledger drift evaluated every physics timestep |
| INV-6 | No Silent Gates | Compile-time enforcement prevents accidental LEGACY mode |

---

## Issues Deferred (Per Constitution Article V Section 5)

| Issue | Target Version | Domain |
|-------|---------------|--------|
| CS-0009 | v0.2.0.0 | SG Secondary Energy Balance |
| CS-0010 | v0.2.0.0 | SG Secondary Pressure Alarm |
| CS-0011 | v0.2.1.0 | Runtime Acceptance Test Infrastructure |
| CS-0012 | v0.2.2.0 | Regime Transition Logging |

---

## Full File Manifest

| File | Phases | Type |
|------|--------|------|
| `Assets/Scripts/Physics/CoupledThermo.cs` | A, B, D | Solver — canonical gate, logging, deprecation |
| `Assets/Scripts/Physics/RCSHeatup.cs` | B, D | Physics — guard rail, signature hardening |
| `Assets/Scripts/Validation/HeatupSimEngine.cs` | A, B, C, D | Engine — rebase, CVCS, accumulators, diagnostics, ownership |
| `Assets/Scripts/Validation/HeatupSimEngine.Init.cs` | A, C | Init — lifecycle resets, diagnostic state |
| `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs` | B | CVCS — accumulators |
| `Assets/Scripts/Validation/HeatupValidationVisual.TabValidation.cs` | C | UI — drift row |
| `Updates/ISSUE_REGISTRY.md` | A, B, C, D | Governance — status tracking |
| `PROJECT_CONSTITUTION.md` | Pre-A | Governance — foundational document |
| `Updates/IMPLEMENTATION_PLAN_v0.1.0.0.md` | Pre-A | Plan — 4-phase architecture |
| `Updates/Implementation_Plans/IMPLEMENTATION_PLAN_SG_DOMAIN_v0.2.0.0.md` | Pre-A | Placeholder — deferred |
| `Updates/Implementation_Plans/IMPLEMENTATION_PLAN_TEST_INFRA_v0.2.1.0.md` | Pre-A | Placeholder — deferred |
| `Updates/Implementation_Plans/IMPLEMENTATION_PLAN_OBSERVABILITY_v0.2.2.0.md` | Pre-A | Placeholder — deferred |
