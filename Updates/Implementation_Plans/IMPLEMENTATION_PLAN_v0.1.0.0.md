# IMPLEMENTATION PLAN v0.1.0.0
## Foundational Authority Enforcement Release

---

## Document Control

| Field | Value |
|-------|-------|
| Version | v0.1.0.0 |
| Plan Severity | **Critical** |
| Architectural Domain | Primary Mass Conservation / Coupled Thermodynamics Solver |
| System Area | CoupledThermo, RCSHeatup, HeatupSimEngine (Regime 2/3 dispatch) |
| Discipline | Mass Conservation, Boundary Accounting, Diagnostic Enforcement |
| Status | DRAFT — Awaiting Approval |
| Constitution Reference | PROJECT_CONSTITUTION.md v0.1.0.0, Articles II, III, V, VII, IX |
| Audit Foundation | Recovery Audit 2026-02-13 (9 documents, Stages 0-4) |

---

## 1. PURPOSE

This release establishes the foundational invariant rules that the simulator must obey going forward. The 5.x lineage is deprecated. We are not patching legacy behavior — we are defining the baseline authority architecture.

The recovery audit (Stage 1-4) revealed that the canonical mass enforcement architecture exists in the code but is **never activated at runtime**. The solver always takes the LEGACY path. The ledger is decorative. The diagnostics are dead. The tests pass on paper.

v0.1.0.0 corrects this by activating the architecture, wiring the diagnostics, and eliminating silent gates — making the simulator **architecturally self-verifying**.

---

## 2. FOUNDATIONAL INVARIANTS

The following are architectural laws. They are not suggestions, optimizations, or preferences. They define what it means for this simulator to be correct.

### INV-1: Single Mass Authority
There shall be exactly one authority for total primary mass in Regime 2 and Regime 3. That authority is `TotalPrimaryMass_lb`. The solver accepts it. The engine maintains it. No other field may override it.

### INV-2: Ledger Mutation by Boundary Flows Only
All boundary flows (CVCS charging, letdown, seal injection, relief) shall enter the primary system through explicit mutation of `TotalPrimaryMass_lb`. No boundary flow may modify component masses directly as a means of updating total system mass.

### INV-3: No V*rho Total Mass Derivation in Coupled Regimes
The CoupledThermo solver shall not derive total mass from V*rho (`M_total = RCS + PZR_w + PZR_s`) during normal Regime 2/3 operations. The solver receives `M_total` from the ledger and distributes it among components. `RCSWaterMass` is computed as the remainder: `M_total - PZR_water - PZR_steam`.

### INV-4: Conservation by Construction
The identity `RCSWaterMass + PZRWaterMass + PZRSteamMass = TotalPrimaryMass_lb` shall hold by construction at the end of every solver call in Regime 2/3. This is not checked — it is guaranteed by the math (Rule R5).

### INV-5: Diagnostic Execution Every Timestep
All registered conservation diagnostics shall execute every physics timestep. No diagnostic may exist in the codebase without a call site. A diagnostic without a call site is a defect.

### INV-6: No Silent Architecture Gates
No code path that enables or disables a physics architecture feature may use a default parameter value. All architecture-level decisions must be explicit at the call site and visible in logs.

---

## 3. ISSUES ASSIGNED

Per Constitution Article III, all work is issue-driven. The following issues from the recovery audit are assigned to this plan:

| Issue ID | Title | Severity | Role in Plan |
|----------|-------|----------|-------------|
| CS-0001 | CoupledThermo canonical mass mode never activated | Critical | Phase A — keystone |
| CS-0002 | TotalPrimaryMass_lb freezes after first step in R2/R3 | High | Phase A — auto-resolved |
| CS-0003 | Boundary flow accumulators never incremented | Medium | Phase B — accounting |
| CS-0004 | Pre-solver V*rho PZR mass computation redundant | Low | Phase D — cleanup |
| CS-0005 | CVCS double-count guard works by accident | Medium | Phase A/B — refactor |
| CS-0006 | UpdatePrimaryMassLedgerDiagnostics() dead code | High | Phase C — resurrection |
| CS-0007 | No UI display for primary ledger drift | Medium | Phase C — wiring |
| CS-0008 | No runtime solver mass conservation check | Medium | Phase B — guard rail |

**Note on ID mapping:** The recovery audit used ISSUE-XXXX format. Under the new Constitution (Article III), the canonical format is CS-XXXX. The mapping is 1:1 (ISSUE-0001 = CS-0001, etc.). The ISSUE_REGISTRY.md will be created as part of this plan's execution with the CS-XXXX format.

**Not assigned to this plan** (out of scope):
- CS-0009 (SG energy balance) — deferred to SG domain plan
- CS-0010 (SG pressure alarm) — deferred to SG domain plan
- CS-0011 (acceptance tests formula-only) — deferred to test infrastructure plan
- CS-0012 (regime transition logging) — deferred to observability plan

---

## 4. SCOPE

### This release SHALL:

1. Activate canonical mass enforcement in CoupledThermo solver for Regime 2 and 3
2. Pass `physicsState.TotalPrimaryMass_lb` explicitly at both `BulkHeatupStep()` call sites
3. Remove the default `0f` parameter pattern — make the canonical argument mandatory via enum
4. Redirect CVCS pre-application from `RCSWaterMass` to `TotalPrimaryMass_lb` (ledger mutation)
5. Implement boundary flow accumulators (`CumulativeCVCSIn_lb`, `CumulativeCVCSOut_lb`, `CumulativeReliefMass_lb`)
6. Add runtime solver mass conservation check after each `SolveEquilibrium` call
7. Resurrect `UpdatePrimaryMassLedgerDiagnostics()` with correct call site placement
8. Wire diagnostic output to validation UI (third check row)
9. Add explicit code comments declaring field authority ownership per regime
10. Document order-of-operations in code comments at each critical section

### This release shall NOT:

- Modify Regime 1 physics (solid operations, isolated heating, bubble formation)
- Rewrite the SG model or add SG validation
- Expand the UI beyond the diagnostic wiring (one new check row)
- Introduce new physics models or subsystems
- Modify acceptance test infrastructure
- Refactor unrelated modules
- Add regime transition logging (deferred)
- Modify alarm setpoints or add new alarms (except diagnostic alarm)

**Containment is mandatory.** Any discovered issue outside this scope shall be logged in the Issue Registry and deferred per Constitution Article V, Section 5.

---

## 5. DEPENDENCIES

| Dependency | Type | Notes |
|-----------|------|-------|
| Recovery audit complete | Prerequisite | All 9 audit documents available as reference |
| Constitution v0.1.0.0 | Governance | All work governed by new Article structure |
| No concurrent plans in this domain | Constitution Art. V Sec. 1 | This is the only active plan for Primary Mass Conservation |

---

## 6. IMPLEMENTATION PHASES

### Phase A — Authority Activation

**Objective:** Establish `TotalPrimaryMass_lb` as the sole mass authority in Regime 2/3. The solver enforces the ledger. The engine maintains it through boundary flows.

**Changes:**

1. **`HeatupSimEngine.cs` — Regime 2 call site (line ~1217)**
   - Pass `physicsState.TotalPrimaryMass_lb` as the 10th argument to `BulkHeatupStep()`
   - Add authority comment: `// INV-1: Ledger is sole mass authority in coupled regimes`

2. **`HeatupSimEngine.cs` — Regime 3 call site (line ~1381)**
   - Pass `physicsState.TotalPrimaryMass_lb` as the 10th argument to `BulkHeatupStep()`
   - Add authority comment: same as above

3. **`HeatupSimEngine.cs` — CVCS pre-application in Regime 2 (line ~1182)**
   - Change: `physicsState.RCSWaterMass += cvcsNetMass_lb` → `physicsState.TotalPrimaryMass_lb += cvcsNetMass_lb`
   - Add comment: `// INV-2: Boundary flows enter through ledger mutation`
   - The solver will distribute the updated ledger to components via INV-3

4. **`HeatupSimEngine.cs` — CVCS pre-application in Regime 3 (line ~1344)**
   - Same change as #3
   - The double-count guard (`regime3CVCSPreApplied`) remains as safety net but is now architecturally correct — CVCS modifies the ledger, not a component

5. **`HeatupSimEngine.cs` — First-step rebase (line ~1455)**
   - Retain the one-time rebase for `InitialPrimaryMass_lb` (baseline reference)
   - Add cross-check: compare rebase value against `TotalPrimaryMassSolid` (Regime 1 value). Log WARNING if delta > 50 lbm.
   - Add comment: `// INV-1: Seed the canonical ledger from component state at regime entry`

6. **`HeatupSimEngine.cs` — Regime 1 solid sync (line ~1085)**
   - No change — Regime 1 correctly maintains `TotalPrimaryMass_lb = TotalPrimaryMassSolid`

7. **`CoupledThermo.cs` — Canonical path (lines 237-272)**
   - No code changes needed — the canonical path is already correctly implemented
   - The path activates automatically when `totalPrimaryMass_lb > 0f`
   - Verify: `RCSWaterMass = M_total - PZR_water - PZR_steam` (Rule R5, line 259)

8. **Verification that LEGACY path is no longer reached in normal operation:**
   - Add a one-time log at `CoupledThermo.cs:123`: if `!useCanonicalMass`, log `"WARNING: CoupledThermo running in LEGACY mode — canonical mass not provided"`

**Acceptance Criteria — Phase A:**
- [ ] Both `BulkHeatupStep()` call sites pass `physicsState.TotalPrimaryMass_lb` explicitly
- [ ] `useCanonicalMass` evaluates `true` every timestep in Regime 2/3
- [ ] Solver takes canonical path (lines 237-272), not LEGACY path (lines 274-288)
- [ ] `RCSWaterMass` is computed as remainder (`M_total - PZR_water - PZR_steam`) — not from V*rho
- [ ] `TotalPrimaryMass_lb` changes when CVCS flows are active (not frozen)
- [ ] Regime 1 behavior is unchanged (solid operations, bubble formation)
- [ ] No LEGACY mode warning appears in log during normal Regime 2/3 operation
- [ ] 4-hour balanced-CVCS heatup: `|componentSum - TotalPrimaryMass_lb| < 0.01 lb` at every logged interval

**Evidence files:**
- `RCSHeatup.cs:104-114` (method signature)
- `CoupledThermo.cs:108-134` (canonical vs LEGACY gate)
- `CoupledThermo.cs:237-272` (canonical post-solve path)
- `HeatupSimEngine.cs:1214-1217, 1378-1381` (call sites)
- `HeatupSimEngine.cs:1182, 1344` (CVCS pre-application)

---

### Phase B — Boundary Accounting Completion

**Objective:** Ensure `TotalPrimaryMass_lb = InitialPrimaryMass_lb + CumulativeCVCSIn_lb - CumulativeCVCSOut_lb - CumulativeReliefMass_lb` holds at all times. All boundary flows tracked.

**Changes:**

1. **`HeatupSimEngine.cs` — Regime 2 CVCS block (lines ~1177-1188)**
   - After ledger mutation (Phase A change): increment accumulators
   ```
   physicsState.CumulativeCVCSIn_lb += Mathf.Max(0f, cvcsNetMass_lb);
   physicsState.CumulativeCVCSOut_lb += Mathf.Max(0f, -cvcsNetMass_lb);
   ```

2. **`HeatupSimEngine.cs` — Regime 3 CVCS block (lines ~1338-1352)**
   - Same accumulator increments as #1

3. **`HeatupSimEngine.cs` — Regime 1 solid CVCS block (lines ~1073-1078)**
   - Same accumulator increments for solid-regime CVCS flows
   - Note: In Regime 1, CVCS modifies `RCSWaterMass` directly (no solver). The ledger (`TotalPrimaryMassSolid`) is updated separately. The accumulators still track boundary flows for the expected-mass calculation.

4. **Relief valve path** (if exists in current code):
   - Increment `CumulativeReliefMass_lb` when relief opens
   - If no relief valve physics exists yet: document as known limitation, accumulator remains at 0f

5. **`RCSHeatup.cs` — Post-solver mass check (after line ~162)**
   - After `SolveEquilibrium` call, compute: `M_out = state.RCSWaterMass + state.PZRWaterMass + state.PZRSteamMass`
   - Compare: `|M_out - M_total| > 10 lb` → log WARNING
   - Compare: `|M_out - M_total| > 100 lb` → log ERROR
   - This is the runtime solver conservation guard (CS-0008)

6. **Remove redundant total-mass recomputation:**
   - `HeatupSimEngine.cs:1164-1165` (Regime 2 pre-solver PZR V*rho) — evaluate whether to keep or remove
   - If canonical mode is active, the solver uses PZR masses as part of M_total (they're included in the ledger). The pre-solver V*rho write to PZR masses becomes an input that affects where the solver "starts" its iteration but does NOT affect the conservation identity. **Decision: keep for now, add comment explaining role. Remove in future cleanup if unnecessary.**
   - Same for `HeatupSimEngine.cs:1318-1319` (Regime 3)

**Acceptance Criteria — Phase B:**
- [ ] `CumulativeCVCSIn_lb > 0` after any charging flow
- [ ] `CumulativeCVCSOut_lb > 0` after any letdown flow
- [ ] During balanced CVCS: `In` and `Out` are both positive and roughly equal
- [ ] `|TotalPrimaryMass_lb - (InitialPrimaryMass_lb + In - Out - Relief)| < 1 lb` during steady-state
- [ ] 8-hour heatup with balanced CVCS: conservation drift < 1 lbm
- [ ] No mass discontinuity at regime transitions (Regime 1 → 2, Regime 2 → 3)
- [ ] Runtime solver check fires WARNING if solver introduces > 10 lb error in a single step

**Evidence files:**
- `CoupledThermo.cs:785-787` (accumulator declarations)
- `HeatupSimEngine.cs:1177-1188, 1338-1352` (CVCS blocks)
- `HeatupSimEngine.cs:1073-1078` (solid CVCS)

---

### Phase C — Diagnostic Enforcement

**Objective:** Ensure conservation is actively monitored every timestep and visible to the operator. No dead diagnostics.

**Changes:**

1. **`HeatupSimEngine.cs` — StepSimulation() Section 9 (after line ~1513)**
   - Add call: `UpdatePrimaryMassLedgerDiagnostics();`
   - Placement: immediately after `UpdateInventoryAudit(dt);`
   - Rationale: all physics complete, all CVCS complete, all boundary flows finalized
   - Per `DIAGNOSTIC_CALLSITE_RECOMMENDATION.md` Section 1

2. **`HeatupSimEngine.Logging.cs` — UpdatePrimaryMassLedgerDiagnostics() corrections**
   - Verify the two-phase branch (line ~492) now works correctly:
     - `primaryMassLedger_lb = TotalPrimaryMass_lb` — now valid (Phase A made ledger live)
     - `primaryMassComponents_lb = RCS + PZR_w + PZR_s` — valid (solver output)
     - `primaryMassExpected_lb = Initial + In - Out - Relief` — now valid (Phase B filled accumulators)
   - If any adjustment is needed for regime-awareness, apply per `DIAGNOSTIC_CALLSITE_RECOMMENDATION.md` Section 2

3. **`HeatupSimEngine.Init.cs` — Correct misleading comment (line ~184)**
   - The comment "read by UpdatePrimaryMassLedgerDiagnostics()" is currently misleading (method was never called). After this phase, the comment becomes accurate. Verify and leave as-is.

4. **`HeatupValidationVisual.TabValidation.cs` — Add third check row**
   - Add to `DrawValidationChecks()`:
     ```
     DrawCheckRowThreeState(ref y, x, w, "Primary Ledger Drift",
         engine.primaryMassDrift_lb, 100f, 1000f,
         $"Drift: {engine.primaryMassDrift_pct:F3}%");
     ```
   - This provides three-tier visual feedback: GREEN (< 100 lb), AMBER (100-1000 lb), RED (> 1000 lb)

5. **`HeatupSimEngine.Init.cs` — Change default `primaryMassStatus`**
   - Set default to `"NOT_CHECKED"` instead of empty string
   - If the diagnostic never runs (shouldn't happen after this fix), the UI shows "NOT_CHECKED" instead of a misleading green state
   - Per RISK-002 mitigation

**Acceptance Criteria — Phase C:**
- [ ] `UpdatePrimaryMassLedgerDiagnostics()` executes every timestep in all regimes
- [ ] `primaryMassStatus` shows "OK" during steady-state balanced CVCS (after Phase A+B)
- [ ] `primaryMassDrift_lb` is < 1 lb during steady-state balanced CVCS
- [ ] Validation tab shows third check row with correct three-state coloring
- [ ] If conservation is artificially broken (e.g., remove CVCS from ledger), diagnostic alarm fires within 1 timestep
- [ ] No false positives at regime transitions (Regime 1 → 2 specifically)
- [ ] Default `primaryMassStatus` is "NOT_CHECKED" before first diagnostic execution

**Evidence files:**
- `HeatupSimEngine.Logging.cs:470-583` (diagnostic method)
- `HeatupValidationVisual.TabValidation.cs:158-242` (validation UI)
- `DIAGNOSTIC_CALLSITE_RECOMMENDATION.md` (full placement rationale)

---

### Phase D — Structural Hardening

**Objective:** Eliminate the class of bug that caused this issue. Make it impossible to accidentally disable canonical mode.

**Changes:**

1. **`RCSHeatup.cs` — Remove default parameter**
   - Change method signature from:
     ```csharp
     float totalPrimaryMass_lb = 0f)  // v5.3.0: Canonical mass constraint
     ```
   - To:
     ```csharp
     float totalPrimaryMass_lb)  // v0.1.0.0: MANDATORY canonical mass (INV-1)
     ```
   - This causes a **compile-time error** if any call site omits the argument
   - Any standalone/test usage that legitimately needs LEGACY mode must pass `0f` explicitly and document why

2. **`CoupledThermo.cs` — Add solver mode announcement**
   - At line ~123, add one-time log on first call:
     ```csharp
     if (useCanonicalMass)
         Debug.Log("CoupledThermo: CANONICAL mode active — ledger authoritative");
     else
         Debug.LogWarning("CoupledThermo: LEGACY mode — V×ρ mass derivation (non-canonical)");
     ```
   - Per RISK-001 mitigation: solver mode is always visible in logs

3. **Authority ownership comments — HeatupSimEngine.cs**
   - At Regime 2 section header (~line 1134): add block comment declaring field ownership
   - At Regime 3 section header (~line 1305): same
   - At CVCS pre-application blocks: add order-of-operations comment
   - Format:
     ```
     // ═══════════════════════════════════════════════════════════════
     // FIELD AUTHORITY — REGIME 3 (Full RCPs)
     // TotalPrimaryMass_lb:  ENGINE (boundary flows via CVCS pre-apply)
     // RCSWaterMass:         SOLVER (remainder = Total - PZR_w - PZR_s)
     // PZRWaterMass:         SOLVER (V×ρ at equilibrium T/P)
     // PZRSteamMass:         SOLVER (V×ρ at equilibrium P)
     // ORDER: CVCS → Ledger → Solver → Diagnostics
     // ═══════════════════════════════════════════════════════════════
     ```

4. **`CoupledThermo.cs` — Comment LEGACY path as explicitly deprecated**
   - At line ~274 (LEGACY else branch):
     ```
     // LEGACY MODE — DEPRECATED as of v0.1.0.0
     // This path exists only for standalone validation tests.
     // During normal Regime 2/3 operation, useCanonicalMass MUST be true.
     // If this path executes during simulation, it indicates a caller bug.
     ```

**Acceptance Criteria — Phase D:**
- [ ] Removing the 10th argument from either call site causes a compile error
- [ ] Log output shows "CANONICAL mode active" on first Regime 2/3 timestep
- [ ] Log output shows "LEGACY mode" WARNING if 0f is passed explicitly (test/validation use)
- [ ] All authority ownership comments present at Regime 2, Regime 3, and CVCS blocks
- [ ] LEGACY path clearly marked as deprecated in CoupledThermo.cs

**Evidence files:**
- `RCSHeatup.cs:104-114` (method signature)
- `CoupledThermo.cs:122-134` (canonical gate)
- `CoupledThermo.cs:274-288` (LEGACY path)

---

## 7. RISK MAPPING

This section maps mitigations to the systemic risks identified in `RISK_REGISTER.md`. Each risk reference points to the full description in the risk register — not duplicated here.

| Risk | Title | Mitigated By | Residual After v0.1.0.0 |
|------|-------|-------------|------------------------|
| RISK-001 | Default Parameter Gate Pattern | Phase D (remove default, compile-time enforcement) | LOW — compiler prevents omission |
| RISK-002 | Dead Diagnostics Create False Confidence | Phase C (resurrect diagnostic, default "NOT_CHECKED") | LOW — diagnostic runs every timestep |
| RISK-003 | Formula-Only Test Suite | NOT addressed in this plan (test infra scope) | HIGH — unchanged, deferred |
| RISK-004 | CVCS Order-of-Operations Fragility | Phase A (CVCS → ledger, not component) + Phase B (solver check) | LOW — CVCS effect is explicit, not implicit |
| RISK-005 | V*rho Last-Writer-Wins | Phase A (solver reads ledger, not component sum) + Phase D (comments) | LOW — solver distributes, doesn't overwrite |
| RISK-006 | Stale Ledger Cascading | Phase A (ledger is live) + Phase C (diagnostic validates) | LOW — ledger maintained by engine + solver |
| RISK-007 | Regime Transition Discontinuity | Phase A (cross-check at rebase) | MEDIUM — monitored but not structurally prevented |
| RISK-008 | Single-Step Rebase | Phase A (cross-check against Regime 1 value) | LOW — rebase becomes verification, not initialization |
| RISK-009 | Boundary Accumulator Incompleteness | Phase B (increment all current boundary paths) | MEDIUM — future paths must add increments |
| RISK-010 | SG Model Trust | NOT addressed in this plan (SG domain scope) | MEDIUM — unchanged, deferred |

**Post-v0.1.0.0 risk posture:** 6 of 10 risks reduced to LOW. 2 remain MEDIUM (regime transition, boundary completeness — mitigated but require ongoing vigilance). 2 remain HIGH/MEDIUM (formula-only tests, SG trust — deferred to future plans).

---

## 8. VERIFICATION PROTOCOL

No release tag without recorded verification. The following protocol defines the runtime verification procedure.

### Test Scenarios

| # | Scenario | What It Tests | Key Metrics |
|---|----------|--------------|-------------|
| V-01 | Cold start, no CVCS, 4-hour heatup | Baseline conservation without boundary flows | Ledger drift, component sum stability |
| V-02 | Cold start, letdown only (-15 gpm) | Ledger decreases correctly with outflow | `TotalPrimaryMass_lb` decreasing, `CumulativeCVCSOut_lb` increasing |
| V-03 | Cold start, charging only (+15 gpm) | Ledger increases correctly with inflow | `TotalPrimaryMass_lb` increasing, `CumulativeCVCSIn_lb` increasing |
| V-04 | Cold start, balanced CVCS (charging = letdown) | Steady-state conservation | Drift < 1 lb over 8 hours |
| V-05 | Bubble formation (NONE → COMPLETE) | Regime 1 → Regime 2 transition mass continuity | Mass delta at transition < 50 lb, diagnostic "OK" |
| V-06 | Full heatup through regime transitions | End-to-end 8-hour heatup with balanced CVCS | All metrics below thresholds, diagnostic "OK" throughout |

### Metrics Recorded Per Scenario

For each scenario, record at 15-minute intervals:

| Metric | Source | Threshold |
|--------|--------|-----------|
| `TotalPrimaryMass_lb` | physicsState | Matches expected (initial + net boundary) |
| `RCSWaterMass + PZRWaterMass + PZRSteamMass` | physicsState | = `TotalPrimaryMass_lb` within 0.01 lb |
| `primaryMassDrift_lb` | diagnostic output | < 1 lb steady-state |
| `primaryMassDrift_pct` | diagnostic output | < 0.001% steady-state |
| `primaryMassStatus` | diagnostic output | "OK" |
| `massError_lbm` (system-level) | CVCS.cs | < 100 lb |
| `CumulativeCVCSIn_lb` | physicsState | Monotonically increasing during charging |
| `CumulativeCVCSOut_lb` | physicsState | Monotonically increasing during letdown |
| `primaryMassBoundaryError_lb` | diagnostic output | < 1 lb (ledger vs expected) |
| Solver mode log | CoupledThermo | "CANONICAL mode active" (no LEGACY warnings) |

### Pass/Fail Criteria

| Criterion | Threshold | Fail Action |
|-----------|-----------|-------------|
| Conservation identity (INV-4) | `|componentSum - ledger| < 0.01 lb` every timestep | BLOCK — cannot release |
| 8-hour drift (V-06) | < 1 lb | BLOCK — cannot release |
| Regime transition mass jump | < 50 lb | BLOCK — investigate |
| Diagnostic false positive | 0 occurrences during V-04, V-06 | BLOCK — fix diagnostic |
| Solver LEGACY warning | 0 occurrences in normal runs | BLOCK — call site not updated |
| Compile without 10th argument | Must fail compilation | BLOCK — default not removed |

---

## 9. EXIT CRITERIA

Per Constitution Article V, Section 6:

- [ ] All assigned issues (CS-0001 through CS-0008) are Closed or Deferred with justification
- [ ] All Phase A-D acceptance criteria pass
- [ ] Verification protocol scenarios V-01 through V-06 recorded with metrics
- [ ] All pass/fail criteria met (no BLOCK conditions)
- [ ] Issue Registry (`Critical/Updates/ISSUE_REGISTRY.md`) created and current
- [ ] Changelog written (`Critical/Updates/Changelogs/CHANGELOG_v0.1.0.0.md`)
- [ ] Version increment performed per Constitution Article II

---

## 10. KNOWN LIMITATIONS

| Limitation | Impact | Disposition |
|-----------|--------|------------|
| Acceptance tests remain formula-only (CS-0011) | Quality gate relies on manual verification | Deferred to test infrastructure plan |
| SG secondary side unvalidated (CS-0009, CS-0010) | SG heat removal errors not caught at primary boundary | Deferred to SG domain plan |
| Regime transition logging absent (CS-0012) | Transition timing not visible in event log | Deferred to observability plan |
| Relief valve accumulator may remain 0f | If no relief physics exists, `CumulativeReliefMass_lb` stays at 0f | Acceptable — accumulator is structurally ready, incremented when relief physics is implemented |
| RISK-003 (formula-only tests) remains HIGH | No runtime acceptance test validation | Acknowledged — v0.1.0.0 adds runtime diagnostics as a partial mitigation |

---

## 11. FILES TO BE MODIFIED

| File | Phase | Nature of Change |
|------|-------|-----------------|
| `Assets/Scripts/Validation/HeatupSimEngine.cs` | A, B | Pass ledger at call sites, redirect CVCS to ledger, add accumulators, add cross-check at rebase |
| `Assets/Scripts/Physics/RCSHeatup.cs` | B, D | Add post-solver mass check, remove default parameter |
| `Assets/Scripts/Physics/CoupledThermo.cs` | D | Add solver mode log, deprecation comments on LEGACY path |
| `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs` | C | Verify diagnostic method correctness post-Phase A/B |
| `Assets/Scripts/Validation/HeatupSimEngine.Init.cs` | C | Change default `primaryMassStatus` to "NOT_CHECKED" |
| `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs` | A | Adjust double-count guard logic if needed after ledger redirection |
| `Assets/Scripts/Validation/HeatupValidationVisual.TabValidation.cs` | C | Add third validation check row |
| `Critical/Updates/ISSUE_REGISTRY.md` | All | Create canonical issue registry (CS-XXXX format) |

---

## 12. NON-GOALS

To be explicit about what this plan does NOT authorize:

- No changes to `SGMultiNodeThermal` or any SG physics
- No changes to `SolidPlantPressure` or Regime 1 physics
- No changes to `BubbleFormation` state machine logic
- No changes to `AlarmManager` setpoints
- No changes to `VCTPhysics` or `BRSPhysics`
- No changes to `CVCSController` PI logic
- No changes to `PlantConstants`
- No changes to `WaterProperties` steam tables
- No UI changes beyond the single diagnostic check row
- No new physics models
- No performance optimization
- No multicore changes
- No acceptance test infrastructure changes

---

## IMPLEMENTATION DIRECTIVE

- Implement one phase at a time (A → B → C → D)
- Stop after each phase
- Present metrics and diff summary
- Confirm before proceeding to next phase
- Write a single changelog after all phases complete and verification passes
- No speculative improvements
- No opportunistic cleanup
- No scope expansion

Per Constitution Article VIII: Claude is an implementation agent. The Constitution is governing authority.

---

## ARCHITECTURAL DECLARATION

> This release marks the moment where the simulator transitions from **physically plausible** to **architecturally self-verifying**.
>
> Before v0.1.0.0: The simulator produced reasonable numbers. Conservation was approximate. The architecture existed in comments and dead code. Trust was assumed.
>
> After v0.1.0.0: The simulator proves its own conservation at every timestep. The ledger is authoritative. The solver enforces it. The diagnostics validate it. The compiler prevents regression. Trust is earned.

---

*Awaiting approval before implementation begins.*
