# AUDIT_INDEX.md — Recovery Audit Progress Tracker
## Recovery Audit 2026-02-13 12:00

---

## Audit Status: STAGES 0–4 COMPLETE — STAGE 5 (CHECKPOINT)

---

## Files Produced

| File | Status | Description |
|------|--------|-------------|
| `PROJECT_TREE_SNAPSHOT.md` | COMPLETE | Categorized listing of all simulation-relevant files |
| `SYSTEM_MAP.md` | COMPLETE | Full update loop execution order with file/line evidence |
| `AUDIT_INDEX.md` | COMPLETE | This file — progress tracker |
| `VALIDATION_MAP.md` | COMPLETE | All validation checks, thresholds, log labels (54 checks, 7 categories) |
| `STAGE3_MASS_AUTHORITY_AUDIT.md` | COMPLETE | Write-path table, verdict per regime, 5 issue cards |
| `DIAGNOSTIC_CALLSITE_RECOMMENDATION.md` | COMPLETE | Resurrection plan for dead diagnostic + dependency chain |
| `SUBSYSTEM_VALIDATION_GAPS.md` | COMPLETE | 10 subsystems mapped: existing checks vs gaps + suggested fixes |
| `ISSUES_REGISTER.md` | COMPLETE | Master issue list: 12 issues (1 CRITICAL, 3 HIGH, 5 MEDIUM, 3 LOW) |
| `RISK_REGISTER.md` | COMPLETE | Systemic risk register: 10 risks with interaction matrix and resolution priority |
| `Subsystems/*.md` | DEFERRED | Per-subsystem deep audit (deferred — key findings covered by Stage 3 focused audits) |

---

## Stages Completed

### Stage 0 — Project Tree Reference
- **Status:** COMPLETE
- **Output:** `PROJECT_TREE_SNAPSHOT.md`
- **Method:** Full recursive file listing, categorized by subsystem role
- **Findings:** ~25 physics .cs files, 6 engine partial class files, 3 visual partial class files, 1 test file, extensive documentation

### Stage 1 — Update Loop & Execution Order
- **Status:** COMPLETE
- **Output:** `SYSTEM_MAP.md`
- **Method:** Deep reading of `HeatupSimEngine.cs:StepSimulation()` (line 786–1514), all 6 partial class files, and core physics modules
- **Key Findings:**
  - Single-threaded physics dispatch via `StepSimulation(dt)` coroutine
  - Three-regime model (Isolated / Blended / Coupled) based on RCP state
  - Canonical mass ledger (`TotalPrimaryMass_lb`) enforced in CoupledThermo solver
  - CVCS mass drain pre-applied before solver in Regime 2/3 (v4.4.0 fix)
  - 7-phase bubble formation state machine with mass-based transfer semantics (v5.4.1)
  - Heater control runs BEFORE physics (v0.4.0 Issue #1 fix)
  - Spray condensation applied before solver (v4.4.0)
  - Inventory audit tracking (InventoryAudit active; PrimaryMassLedgerDiagnostics DEAD CODE — see ISSUE-0006)

---

### Stage 2 — Validation Systems Inventory
- **Status:** COMPLETE
- **Output:** `VALIDATION_MAP.md`
- **Method:** Full keyword sweep (26 keywords) across all .cs in Validation/ and Physics/ folders, plus targeted reads of AlarmManager.cs, TabValidation.cs, Alarms.cs, AcceptanceTests_v5_4_0.cs, VCTPhysics.cs, CoupledThermo.cs, HeatupSimEngine.Logging.cs (UpdateInventoryAudit + UpdatePrimaryMassLedgerDiagnostics)
- **Inventory:** 54 unique checks across 7 categories:
  - 8 mass conservation checks (runtime + dead code)
  - 7 CoupledThermo solver validation tests (static, test-runner only)
  - 10 acceptance tests (v5.4.0, architecture-only, no simulation validation)
  - 14 alarm setpoint checks (AlarmManager, every timestep)
  - 12 UI PASS/FAIL checks (Validation tab)
  - ~10 guards/clamps/bounds (solver, regime, safety)
  - 8 diagnostic logging hooks
- **Critical Findings:**
  1. **DEAD CODE: `UpdatePrimaryMassLedgerDiagnostics()` is never called** (Logging.cs:470, zero call sites confirmed). This is the ledger-vs-components cross-check that would detect solver drift.
  2. **V×ρ overwrite in Regime 2/3:** PZR masses recomputed from volume×density before solver (lines 1163-1165, 1317-1319). Potential conservation drift vector.
  3. **Acceptance tests validate formulas only**, not runtime simulation — all 10 pass by construction.

---

### Stage 3 — Focused Audits (Canonical Mass + Diagnostics + Gaps)
- **Status:** COMPLETE
- **Deliverables:**
  1. `STAGE3_MASS_AUTHORITY_AUDIT.md` — Traced every write to 5 canonical mass fields across all regimes
  2. `DIAGNOSTIC_CALLSITE_RECOMMENDATION.md` — Resurrection plan for `UpdatePrimaryMassLedgerDiagnostics()`
  3. `SUBSYSTEM_VALIDATION_GAPS.md` — Validation coverage for 10 subsystems + acceptance tests
- **CRITICAL FINDING (ISSUE-0001):** CoupledThermo canonical mass enforcement (Rules R1, R3, R5) is correctly implemented but **NEVER ACTIVATED**. Both Regime 2 and 3 call `BulkHeatupStep()` without passing `totalPrimaryMass_lb`. The parameter defaults to `0f`, forcing LEGACY mode where all masses are V×ρ-derived. The ledger is decorative.
- **Supporting Findings:**
  - ISSUE-0002: `TotalPrimaryMass_lb` freezes after first-step rebase (never updated by CVCS flows in R2/R3)
  - ISSUE-0003: `CumulativeCVCSIn_lb`, `CumulativeCVCSOut_lb`, `CumulativeReliefMass_lb` declared but never incremented (always 0f)
  - ISSUE-0004: Pre-solver V×ρ PZR mass computation is redundant with solver output (solver overwrites)
  - ISSUE-0005: CVCS double-count guard works by accident (CVCS delta enters M_total via component sum)
- **Per-subsystem deep audits (Subsystems/*.md) DEFERRED** — the focused audit approach surfaced the architectural issues more efficiently. Individual subsystem files can be produced on request.

---

### Stage 4 — Issues Register + Risk Register
- **Status:** COMPLETE
- **Deliverables:**
  1. `ISSUES_REGISTER.md` — 12 issues (ISSUE-0001 through ISSUE-0012) with dependency chain, severity ratings, evidence, fix intent, and acceptance criteria
  2. `RISK_REGISTER.md` — 10 systemic risks (RISK-001 through RISK-010) with conditions, detection, mitigation, residual risk, and interaction matrix
- **Key Outputs:**
  - **Dependency chain confirmed:** ISSUE-0001 (keystone) → ISSUE-0002 (auto-resolved) → ISSUE-0005 (refactor) → ISSUE-0003 (accumulators) → ISSUE-0006 (resurrect diagnostic) → ISSUE-0007 (UI)
  - **Critical amplification loop identified:** RISK-001 + RISK-002 + RISK-003 form a reinforcing triangle where canonical mode is off, diagnostics are dead, and tests are formula-only — no signal reaches the developer
  - **Resolution priority mapped to version ladder:** Priority 1-6 → v5.4.2.0 (FF-05), Priority 7 → v5.5.0.0, Priority 8 → v5.4.3.0 (FF-07), Priority 10 → v5.6.0.0 (SG)

---

## Stages Remaining

### Stage 5 — Checkpoint / Stop
- **Status:** COMPLETE — this is the checkpoint
- **Summary:** Full audit trail from project tree through physics execution order, validation inventory, mass authority analysis, and issue/risk registers is now complete. All findings are evidence-backed with file:line citations. The audit is ready for use as the foundation for v5.4.2.0 (FF-05 mass conservation) implementation planning.

---

## Open Questions — All Resolved by Stage 3

1. **RESOLVED — Regime 2/3 V×ρ overwrite:** The pre-solver V×ρ write (lines 1164-1165, 1318-1319) is immediately overwritten by the solver in LEGACY mode (CoupledThermo.cs:286-287). It's redundant but harmless. See ISSUE-0004.
2. **RESOLVED — CVCS pre-application timing:** The CVCS mass is added to `RCSWaterMass` before the solver. The solver reads `M_total = RCS + PZR_w + PZR_s` (line 133), so the CVCS delta IS included in M_total. The solver then overwrites `RCSWaterMass = V×ρ` (LEGACY), redistributing the total. The CVCS effect is preserved indirectly via M_total. See ISSUE-0005.
3. **RESOLVED — Regime 2 blending:** The blending only mixes T/P/surgeFlow by α. PZR volumes and masses come from the coupled path. RCSWaterMass is synced from `physicsState` (which was set by solver). This is architecturally reasonable for the blended regime.
4. **RESOLVED — UpdatePrimaryMassLedgerDiagnostics() is DEAD CODE:** Confirmed. Resurrection plan written. See `DIAGNOSTIC_CALLSITE_RECOMMENDATION.md`.

---

## Constraints
- NO edits to existing Roadmap / Changelog / Implementation Plan files
- NO new changelog entries
- Evidence-based findings only (file + line range)
- Findings rated by confidence (High/Med/Low)
