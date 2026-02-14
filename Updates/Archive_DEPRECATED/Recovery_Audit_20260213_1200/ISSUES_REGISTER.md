# ISSUES_REGISTER.md — Master Issue List
## Recovery Audit 2026-02-13

---

## Summary

| Severity | Count |
|----------|-------|
| CRITICAL | 1 |
| HIGH | 3 |
| MEDIUM | 5 |
| LOW | 3 |
| **TOTAL** | **12** |

## Dependency Chain (Fix Order)

```
ISSUE-0001  (activate canonical mode)          ← KEYSTONE
    │
    ├─► ISSUE-0002  (ledger update in R2/R3)   ← auto-resolved if R5 enforced
    │
    ├─► ISSUE-0005  (CVCS pre-apply → ledger)  ← refactor target
    │
    ├─► ISSUE-0003  (boundary accumulators)     ← enables expected-mass check
    │       │
    │       └─► ISSUE-0006  (resurrect diagnostic)  ← needs real ledger + real accumulators
    │               │
    │               └─► ISSUE-0007  (UI row for drift) ← display layer
    │
    └─► ISSUE-0004  (remove redundant V×ρ)     ← cleanup, low risk

Independent (no dependency on ISSUE-0001):
    ISSUE-0008  (runtime solver mass check)
    ISSUE-0009  (SG energy balance check)
    ISSUE-0010  (SG pressure alarm)
    ISSUE-0011  (acceptance test harness)
    ISSUE-0012  (regime transition logging)
```

---

## ISSUE-0001: CoupledThermo Canonical Mass Mode Never Activated

- **Classification:** Authority conflict
- **Severity:** CRITICAL
- **Evidence:**
  - `RCSHeatup.cs:114` — parameter `totalPrimaryMass_lb = 0f` (default triggers LEGACY mode)
  - `HeatupSimEngine.cs:1214-1217` — Regime 2 call passes 9 of 10 args; 10th omitted
  - `HeatupSimEngine.cs:1378-1381` — Regime 3 call passes 9 of 10 args; 10th omitted
  - `CoupledThermo.cs:123` — gate: `useCanonicalMass = (totalPrimaryMass_lb > 0f)` → always false
  - `CoupledThermo.cs:278,286-287` — LEGACY path: all masses from V×ρ
  - `CoupledThermo.cs:237-272` — Canonical path: dead code (never reached)
- **Trigger:** Every timestep in Regime 2 and Regime 3 (the entire two-phase coupled simulation)
- **Impact:**
  - Rules R1 (single canonical ledger), R3 (no V×ρ overwrite), R5 (RCS as remainder) are **unenforced**
  - All component masses derived from V×ρ each timestep — no conservation-by-construction
  - `TotalPrimaryMass_lb` field is decorative — written once at first-step rebase, never read by solver
  - Any "mass loss" investigation is working with instruments that don't measure what they claim
  - The entire v5.4.0 canonical architecture exists only as comments and dead code paths
- **Root cause:** The `totalPrimaryMass_lb` parameter was added to `BulkHeatupStep()` in v5.3.0 with a default of `0f` for backward compatibility. The two call sites in `StepSimulation()` were never updated to pass `physicsState.TotalPrimaryMass_lb`.
- **Fix intent:** Pass `physicsState.TotalPrimaryMass_lb` as the 10th argument at both call sites (lines 1217 and 1381). This activates the canonical path in `CoupledThermo.SolveEquilibrium()`, which enforces `RCSWaterMass = M_total - PZR_water - PZR_steam` (conservation by construction).
- **Dependencies:** None — this is the keystone. All other conservation issues depend on this.
- **Blocks:** ISSUE-0002, ISSUE-0003, ISSUE-0005, ISSUE-0006
- **Acceptance criteria:**
  - After fix, `CoupledThermo` takes `useCanonicalMass = true` path every timestep in R2/R3
  - `|componentSum - TotalPrimaryMass_lb| < 0.01 lb` (conservation identity holds by construction)
  - 4-hour balanced-CVCS simulation: ledger drift < 0.01% (< 60 lb on ~600,000 lb system)
- **Notes:** The LEGACY V×ρ path is valid physics (the sim "works"), but it defeats the conservation architecture. Activating canonical mode doesn't change physics results significantly — it changes WHO is authoritative and makes conservation provable.

---

## ISSUE-0002: TotalPrimaryMass_lb Freezes After First Step in Regime 2/3

- **Classification:** Boundary accounting gap
- **Severity:** HIGH
- **Evidence:**
  - `HeatupSimEngine.cs:1455` — one-time rebase: `TotalPrimaryMass_lb = RCS + PZR_w + PZR_s`
  - No write to `TotalPrimaryMass_lb` in Regime 2 (lines 1134-1300) or Regime 3 (lines 1305-1443)
  - CVCS pre-application writes to `RCSWaterMass` (lines 1182, 1344) but not the ledger
- **Trigger:** Always, from second physics step onward in Regime 2/3
- **Impact:**
  - Ledger diverges from actual state as CVCS flows accumulate
  - Any diagnostic reading `TotalPrimaryMass_lb` gets a stale value
  - Inventory audit reads component masses directly (not ledger), so it's unaffected — but the ledger's staleness means the "Primary Mass Ledger" concept is broken
- **Root cause:** The ledger was designed to be maintained by the solver in canonical mode (where `M_total` is fed from the ledger and `RCS = remainder` preserves it). Since canonical mode is never active (ISSUE-0001), no one maintains the ledger.
- **Fix intent:** Auto-resolved by ISSUE-0001. When canonical mode is active, the solver accepts `TotalPrimaryMass_lb` as input and the engine updates it via CVCS boundary flows. Alternatively, if ISSUE-0001 is deferred: update ledger explicitly after solver each timestep as `TotalPrimaryMass_lb = RCS + PZR_w + PZR_s`.
- **Dependencies:** Blocked by ISSUE-0001 (canonical mode fix makes this automatic)
- **Blocked by:** ISSUE-0001
- **Acceptance criteria:**
  - `|TotalPrimaryMass_lb - (RCSWaterMass + PZRWaterMass + PZRSteamMass)| < 0.01 lb` at every timestep
  - Ledger changes when CVCS flows change
- **Notes:** This issue is a direct consequence of ISSUE-0001. It does not require a separate code fix if canonical mode is activated.

---

## ISSUE-0003: Boundary Flow Accumulators Never Incremented

- **Classification:** Boundary accounting gap
- **Severity:** MEDIUM
- **Evidence:**
  - `CoupledThermo.cs:785-787` — declarations: `CumulativeCVCSIn_lb`, `CumulativeCVCSOut_lb`, `CumulativeReliefMass_lb`
  - Full codebase grep: zero assignment sites (no `+=`, no `=` except struct default)
  - `HeatupSimEngine.Logging.cs:481-484, 526-529, 700-703` — read sites compute `expectedMass = Initial + In - Out - Relief` but all accumulators are always 0f
- **Trigger:** Always — values permanently 0f
- **Impact:**
  - `UpdatePrimaryMassLedgerDiagnostics()` (dead code) would compute `expected = initial` regardless of CVCS operations, producing false alarms after any net CVCS flow
  - The "boundary error" check (`|ledger - expected|`) is meaningless without these accumulators
  - Interval log lines referencing these fields show zeros
- **Root cause:** Fields declared as part of v5.3.0 canonical architecture. The increment logic was never added to the CVCS flow paths in `StepSimulation()`.
- **Fix intent:** In Regime 2/3 CVCS pre-application blocks (HeatupSimEngine.cs:1177-1188 and 1338-1352):
  - `physicsState.CumulativeCVCSIn_lb += Mathf.Max(0f, cvcsNetMass_lb)` (net charging)
  - `physicsState.CumulativeCVCSOut_lb += Mathf.Max(0f, -cvcsNetMass_lb)` (net letdown)
  - Similarly for solid ops CVCS path (line 1073-1078)
  - Relief valve path: increment `CumulativeReliefMass_lb` when relief opens
- **Dependencies:** Should be done AFTER ISSUE-0001 (so the ledger is real), before ISSUE-0006 (so the diagnostic has valid expected-mass)
- **Blocked by:** ISSUE-0001 (meaningful only with canonical mode)
- **Blocks:** ISSUE-0006 (diagnostic resurrection needs valid accumulators)
- **Acceptance criteria:**
  - After 4-hour balanced CVCS: `CumulativeCVCSIn_lb > 0` and `CumulativeCVCSOut_lb > 0` and roughly equal
  - `|expectedMass - ledger| < 1 lb` during steady-state balanced CVCS
- **Notes:** Quick implementation (~6 lines of code). Low risk.

---

## ISSUE-0004: Pre-Solver V×ρ PZR Mass Computation Redundant with Solver

- **Classification:** State ownership confusion
- **Severity:** LOW
- **Evidence:**
  - `HeatupSimEngine.cs:1164-1165` (Regime 2) — `PZRWaterMass = vol × ρ`, `PZRSteamMass = vol × ρ`
  - `HeatupSimEngine.cs:1318-1319` (Regime 3) — same pattern
  - `CoupledThermo.cs:286-287` — solver overwrites same fields from V×ρ at new T/P
  - `HeatupSimEngine.cs:1197-1211, 1361-1375` — spray condensation modifies pre-computed values that are then overwritten
- **Trigger:** Every timestep in Regime 2 and 3
- **Impact:**
  - Functionally harmless (solver immediately overwrites)
  - Spray condensation adjustments to PZR masses pre-solver are wasted work — solver recomputes from scratch
  - Creates confusion about who owns PZR mass state (engine or solver?)
  - If canonical mode is activated (ISSUE-0001), the pre-computation becomes the solver's INPUT (not overwritten), so the V×ρ write becomes meaningful again — spray adjustments would actually matter
- **Root cause:** v0.9.6 added the pre-sync to ensure the solver starts from correct PZR state. This was necessary when the solver used PZR state as input. In LEGACY mode, the solver ignores PZR mass input and recomputes everything.
- **Fix intent:** After ISSUE-0001: the pre-computation becomes meaningful (solver reads PZR masses as part of M_total). Keep it, but document that spray condensation pre-solver affects M_total via PZR mass changes. Alternatively, apply spray to the ledger directly and let the solver distribute.
- **Dependencies:** Behavior changes depending on ISSUE-0001 resolution
- **Blocked by:** None (cleanup item)
- **Acceptance criteria:** No functional change; code clarity improved
- **Notes:** Low priority. Defer until after ISSUE-0001 fix stabilizes.

---

## ISSUE-0005: CVCS Double-Count Guard Works by Accident

- **Classification:** Order-of-operations
- **Severity:** MEDIUM
- **Evidence:**
  - `HeatupSimEngine.cs:1344` — CVCS mass pre-applied: `RCSWaterMass += cvcsNetMass_lb`
  - `HeatupSimEngine.cs:1351` — flag set: `regime3CVCSPreApplied = true`
  - `CoupledThermo.cs:133` — solver reads `M_total = RCS + PZR_w + PZR_s` (includes CVCS delta)
  - `CoupledThermo.cs:278` — solver overwrites `RCSWaterMass = V×ρ` (redistributes M_total)
  - `HeatupSimEngine.CVCS.cs:190-194` — CVCS partial skipped when flag set
- **Trigger:** Every timestep in Regime 2 and 3
- **Impact:**
  - Current behavior: CVCS delta enters `M_total` via component sum, solver redistributes via V×ρ, CVCS partial is skipped. Net effect is correct — CVCS flow IS reflected in system mass.
  - **But the mechanism is fragile:** any change to solver behavior, or any code that reads `RCSWaterMass` between pre-apply and solver, sees a transient inconsistency
  - If canonical mode is activated (ISSUE-0001), the CVCS delta should modify the LEDGER (`TotalPrimaryMass_lb`), not `RCSWaterMass`, because the solver will compute `RCS = ledger - PZR` as remainder
- **Root cause:** v4.4.0 fix applied CVCS before solver to fix PZR level calculation. The guard prevents double-counting. This is architecturally correct in legacy mode but semantically wrong for canonical mode.
- **Fix intent:** When ISSUE-0001 is resolved: change CVCS pre-application to modify `TotalPrimaryMass_lb += cvcsNetMass_lb` instead of `RCSWaterMass`. The solver then distributes the updated ledger. The double-count guard logic remains but triggers on ledger modification instead.
- **Dependencies:** Must be coordinated with ISSUE-0001
- **Blocked by:** ISSUE-0001
- **Acceptance criteria:**
  - After 4 hours with -15 gpm net CVCS: `TotalPrimaryMass_lb` decreases by ~75,000 lb (15 gpm × 240 min × 8.34 lb/gal ÷ 7.48 gal/ft³ × ρ)
  - Conservation error < 1 lb throughout
- **Notes:** This is the trickiest fix in the chain. The pre-apply location is correct (solver needs to see correct mass), but the target field must change from RCSWaterMass to the ledger.

---

## ISSUE-0006: UpdatePrimaryMassLedgerDiagnostics() Never Called (Dead Code)

- **Classification:** Boundary accounting gap (diagnostic gap)
- **Severity:** HIGH
- **Evidence:**
  - `HeatupSimEngine.Logging.cs:470` — method definition (114 lines, fully implemented)
  - Full codebase grep: zero call sites
  - `HeatupSimEngine.Init.cs:184` — comment references it as if it runs ("read by UpdatePrimaryMassLedgerDiagnostics()")
  - Display fields `primaryMassStatus`, `primaryMassAlarm`, `primaryMassDrift_lb`, etc. all remain at defaults
- **Trigger:** Never — method is never invoked
- **Impact:**
  - The most sensitive conservation diagnostic (ledger vs component sum) is silent
  - All display fields it would populate show defaults — UI may appear "OK" when it shouldn't
  - False confidence: the diagnostic's existence in the codebase implies it's running
- **Root cause:** Method was added in v5.3.0 Stage 6 but the call site in `StepSimulation()` was never added. The comment at Init.cs:184 suggests the author believed it was wired up.
- **Fix intent:** Add call to `UpdatePrimaryMassLedgerDiagnostics()` in `StepSimulation()` after `UpdateInventoryAudit(dt)` (after line 1513). See `DIAGNOSTIC_CALLSITE_RECOMMENDATION.md` for full placement rationale and regime-aware corrections.
- **Dependencies:**
  - **Blocked by:** ISSUE-0001 (ledger must be real for drift check to be meaningful)
  - **Blocked by:** ISSUE-0003 (accumulators must be real for expected-mass check)
  - **Can be called immediately as a smoke test** — it will alarm because the ledger is stale, proving ISSUE-0001/0002 at runtime
- **Blocks:** ISSUE-0007 (UI row)
- **Acceptance criteria:**
  - Method executes every timestep after inventory audit
  - `primaryMassStatus` shows "OK" during steady-state balanced CVCS (after ISSUE-0001+0003 fixed)
  - Alarm fires correctly when conservation error > 1.0%
- **Notes:** Quick win — single line addition to StepSimulation. But meaningfulness depends on ISSUE-0001 and ISSUE-0003.

---

## ISSUE-0007: No UI Display for Primary Ledger Drift

- **Classification:** Boundary accounting gap (observability)
- **Severity:** MEDIUM
- **Evidence:**
  - `HeatupValidationVisual.TabValidation.cs:158-242` — 12 PASS/FAIL checks, none for `primaryMassDrift`
  - Engine fields `primaryMassDrift_lb`, `primaryMassDrift_pct`, `primaryMassStatus` exist but are unused by UI
- **Trigger:** Always — no display exists
- **Impact:**
  - Even after ISSUE-0006 resurrects the diagnostic, the operator/developer has no visual indicator of ledger drift
  - The three existing mass checks (massError_lbm, VCT flow imbalance, inventory conservation) don't detect solver-vs-ledger divergence
- **Root cause:** UI was not updated when the diagnostic method was added in v5.3.0.
- **Fix intent:** Add a third three-state check row to `DrawValidationChecks()` in TabValidation.cs:
  ```
  DrawCheckRowThreeState(ref y, x, w, "Primary Ledger Drift",
      engine.primaryMassDrift_lb, 100f, 1000f,
      $"Drift: {engine.primaryMassDrift_pct:F3}%");
  ```
- **Dependencies:** Blocked by ISSUE-0006 (diagnostic must run to populate fields)
- **Blocked by:** ISSUE-0006
- **Acceptance criteria:** Validation tab shows three-state indicator for primary ledger drift
- **Notes:** ~5 lines of UI code. Trivial after ISSUE-0006.

---

## ISSUE-0008: No Runtime Solver Mass Conservation Check

- **Classification:** Authority conflict (undetected)
- **Severity:** MEDIUM
- **Evidence:**
  - `CoupledThermo.cs:264-272` — DEBUG-only conservation check exists but only in canonical path (dead code)
  - `CoupledThermo.cs:516-529` — static `ValidateMassConservation()` test (manual runner only)
  - No runtime check of `|M_total_out - M_total_in|` after each `SolveEquilibrium` call
- **Trigger:** Never — no runtime check exists
- **Impact:**
  - If the solver introduces mass error (e.g., from floating-point accumulation, clamping, or non-convergence), nobody detects it until the downstream `massError_lbm` check catches it at the system level
  - In LEGACY mode, `M_total_out` may differ from `M_total_in` because the solver recomputes all masses independently
- **Root cause:** Conservation check only implemented in canonical path and in static tests.
- **Fix intent:** After `SolveEquilibrium` call in `RCSHeatup.BulkHeatupStep()` (line ~162), compute `M_out = RCS + PZR_w + PZR_s` and compare to `M_total`. Log WARNING if `|M_out - M_total| > 10 lb`.
- **Dependencies:** Independent (can be done before or after ISSUE-0001)
- **Blocked by:** None
- **Acceptance criteria:** Runtime log captures any single-step mass error > 10 lb from solver
- **Notes:** Useful diagnostic regardless of canonical vs legacy mode. ~5 lines of code.

---

## ISSUE-0009: No SG Secondary Energy Balance Validation

- **Classification:** Boundary accounting gap
- **Severity:** MEDIUM
- **Evidence:**
  - `SGMultiNodeThermal.Update()` returns `TotalHeatRemoval_MW` — consumed by `BulkHeatupStep` as input
  - No check validates that SG heat removal is physically consistent with primary temperature change
  - No bounds on `sgHeatTransfer_MW` (could go negative or exceed input without detection)
- **Trigger:** Every timestep — SG model runs unchecked
- **Impact:**
  - SG model could output arbitrary heat removal; T_rcs would go unrealistic before any alarm fires
  - Current alarm setpoints (subcooling, pressure) would eventually catch it, but with significant delay
- **Root cause:** SG model is trusted as a physics module with no output validation.
- **Fix intent:** Add sanity check: `sgHeatTransfer_MW >= 0` and `sgHeatTransfer_MW <= grossHeat_MW * 2.0`. Log WARNING if violated.
- **Dependencies:** Independent
- **Blocked by:** None
- **Acceptance criteria:** Runtime WARNING if SG heat removal exceeds 2× gross heat input
- **Notes:** Low-cost guard rail. Not a conservation check per se — more of a physics plausibility check.

---

## ISSUE-0010: No SG Secondary Pressure Alarm

- **Classification:** Boundary accounting gap
- **Severity:** LOW
- **Evidence:**
  - `sgSecondaryPressure_psia` computed and displayed (HeatupSimEngine.cs:1399)
  - No alarm in `AlarmManager.CheckAlarms()` for SG secondary pressure
  - Real SG safety valve setpoint is ~1085 psia
- **Trigger:** When SG secondary pressure is computed but never bounded
- **Impact:**
  - During isolated boiling, SG pressure can rise without any alarm — operator/developer sees pressure in display but no annunciator fires
  - In reality, SG safety valves would lift at ~1085 psia
- **Root cause:** AlarmManager was built for primary-side alarms. SG secondary alarms were not in scope.
- **Fix intent:** Add `SGPressureHigh` alarm to AlarmManager: `sgSecondaryPressure > 1085 psia`. Add corresponding edge detection in Alarms.cs.
- **Dependencies:** Independent
- **Blocked by:** None
- **Acceptance criteria:** Alarm fires when SG secondary pressure exceeds 1085 psia
- **Notes:** Low priority for heatup simulation (SG pressure rarely reaches safety valve setpoint during normal heatup). More relevant for abnormal scenarios.

---

## ISSUE-0011: Acceptance Tests Are Formula-Only, Not Simulation-Validated

- **Classification:** Boundary accounting gap (test coverage)
- **Severity:** HIGH
- **Evidence:**
  - `AcceptanceTests_v5_4_0.cs:58-464` — all 10 tests pass by checking calculation correctness
  - Each test contains "REQUIRES SIMULATION" note (e.g., AT-01 line 89, AT-02 line 131)
  - `RunAllTests()` (line 473) returns 10/10 PASSED without running any simulation
- **Trigger:** Always — tests pass vacuously
- **Impact:**
  - The quality gate "All 10 tests must pass before changelog creation" is satisfied without verifying runtime behavior
  - The tests validate Rules R1-R8 mathematically but don't verify the rules are actually enforced at runtime (they aren't — ISSUE-0001)
  - False confidence: test suite suggests conservation architecture is validated
- **Root cause:** Tests were designed as architectural validation (correct formulas), with simulation validation deferred to manual observation.
- **Fix intent:** Add runtime test points to the simulation loop. At specific simTime milestones, record state snapshots. At simulation end, compare against AT criteria. Minimal approach:
  - AT-02 (no-flow drift): check `|massEnd - massStart| < 60 lb` after 4 hr balanced CVCS
  - AT-03 (transition): record mass at bubble formation and compare solid vs two-phase ledger
  - AT-08 (RCP spike): track max `|Δ(pzrLevel)|` per timestep; fail if > 0.5%
- **Dependencies:** Independent (but more meaningful after ISSUE-0001)
- **Blocked by:** None (can add runtime checks now; they'll be more meaningful post-fix)
- **Acceptance criteria:** At least AT-02, AT-03, AT-08 run against live simulation data and produce PASS/FAIL from actual measurements
- **Notes:** Full end-to-end test harness is a Phase 1 feature (v5.5.x). Minimal runtime checks can be added now.

---

## ISSUE-0012: No Regime Transition Logging

- **Classification:** State ownership confusion (observability)
- **Severity:** LOW
- **Evidence:**
  - `HeatupSimEngine.cs:1009, 1134, 1305` — regime selection based on α
  - Plant mode transitions logged (Alarms.cs:85-89) but regime transitions are not
  - No event logged when α crosses regime boundaries (0→blended→1)
- **Trigger:** Every RCP start/stop that changes α across regime boundaries
- **Impact:**
  - Debugging mass issues across regime boundaries is harder without knowing when transitions occur
  - Log analysis must infer regime from RCP count rather than seeing explicit transitions
- **Root cause:** Regime is an internal physics concept, not an operator-visible alarm. Logging was not prioritized.
- **Fix intent:** Add edge-detected logging: "REGIME CHANGE: {old} → {new} (α = {value}, RCPs = {count})" when regime boundary is crossed.
- **Dependencies:** Independent
- **Blocked by:** None
- **Acceptance criteria:** Event log contains regime transition entries at each RCP start
- **Notes:** Pure observability improvement. No physics impact.
