# ISSUE REGISTRY
## Canonical Issue Registry — Constitution Article III Authority

---

## Registry Control

| Field | Value |
|-------|-------|
| Governing Document | PROJECT_CONSTITUTION.md v0.1.0.0, Article III |
| ID Format | CS-XXXX |
| Source of Truth | This file (`Critical/Updates/ISSUE_REGISTRY.md`) |
| Created | 2026-02-13 |
| Origin | Recovery Audit 2026-02-13 (Stages 0-4) |

No issue may exist outside this registry.

---

## Summary

| Severity | Count |
|----------|-------|
| Critical | 1 |
| High | 3 |
| Medium | 5 |
| Low | 4 |
| **Total** | **13** |

| Status | Count |
|--------|-------|
| Resolved | 9 |
| In Progress | 0 |
| Assigned | 0 |
| Deferred | 4 |

---

## Dependency Chain

```
CS-0001  (activate canonical mode)              KEYSTONE .............. RESOLVED v0.1.0.0
    |
    |--- CS-0002  (ledger update in R2/R3)       auto-resolved ........ RESOLVED v0.1.0.0
    |
    |--- CS-0005  (CVCS pre-apply -> ledger)     refactor target ...... RESOLVED v0.1.0.0
    |
    |--- CS-0003  (boundary accumulators)         enables expected-mass  RESOLVED v0.1.0.0
    |       |
    |       +--- CS-0006  (resurrect diagnostic)  needs real ledger ... RESOLVED v0.1.0.0
    |               |
    |               +--- CS-0007  (UI row)        display layer ...... RESOLVED v0.1.0.0
    |
    +--- CS-0004  (remove redundant V*rho)        cleanup ............ RESOLVED v0.1.0.0

Independent (no dependency on CS-0001):
    CS-0008  (runtime solver mass check) ............................ RESOLVED v0.1.0.0
    CS-0009  (SG energy balance) ........... DEFERRED -> v0.2.0.0 (SG Domain)
    CS-0010  (SG pressure alarm) ........... DEFERRED -> v0.2.0.0 (SG Domain)
    CS-0011  (acceptance test harness) ..... DEFERRED -> v0.2.1.0 (Test Infra)
    CS-0012  (regime transition logging) ... DEFERRED -> v0.2.2.0 (Observability)

CS-0013  (session lifecycle resets) ............................... RESOLVED v0.1.0.0
```

---

## CS-0001: CoupledThermo Canonical Mass Mode Never Activated

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0001 |
| **Title** | CoupledThermo canonical mass mode never activated |
| **Severity** | Critical |
| **Status** | Resolved |
| **Detected In Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | CoupledThermo / RCSHeatup / HeatupSimEngine |
| **Discipline** | Mass Conservation — Authority Enforcement |
| **Operational Impact** | Solver runs in LEGACY V*rho mode every timestep in Regime 2/3. Conservation architecture (Rules R1, R3, R5) exists as dead code. All component masses derived from V*rho — no conservation-by-construction. |
| **Physics Integrity Impact** | Critical — The canonical ledger `TotalPrimaryMass_lb` is decorative. Mass conservation is not provable. The entire v5.4.0 architecture exists only in comments and unreachable code paths. Physics results are reasonable but the system cannot prove its own correctness. |
| **Root Cause Status** | Confirmed — `BulkHeatupStep()` parameter `totalPrimaryMass_lb` defaults to `0f` (RCSHeatup.cs:114). Neither call site (HeatupSimEngine.cs:1214-1217, 1378-1381) passes the 10th argument. Gate at CoupledThermo.cs:123 (`useCanonicalMass = totalPrimaryMass_lb > 0f`) is always false. |
| **Assigned Implementation Plan** | IMPLEMENTATION_PLAN_v0.1.0.0 — Phase A |
| **Validation Outcome** | Resolved v0.1.0.0 — Both R2/R3 call sites now pass `physicsState.TotalPrimaryMass_lb` as 10th arg. Solver logs "CANONICAL mode active" on first coupled step. Default parameter removed in Phase D (compile-time enforcement). |
| **Related Issues** | Blocks: CS-0002, CS-0003, CS-0005, CS-0006 |

---

## CS-0002: TotalPrimaryMass_lb Freezes After First Step in Regime 2/3

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0002 |
| **Title** | TotalPrimaryMass_lb freezes after first step in Regime 2/3 |
| **Severity** | High |
| **Status** | Resolved |
| **Detected In Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | HeatupSimEngine (mass ledger maintenance) |
| **Discipline** | Mass Conservation — Ledger Integrity |
| **Operational Impact** | Ledger diverges from actual state as CVCS flows accumulate. Any diagnostic reading `TotalPrimaryMass_lb` gets a stale value frozen at the first-step rebase. |
| **Physics Integrity Impact** | High — The "Primary Mass Ledger" concept is broken. The ledger field is written once (HeatupSimEngine.cs:1455) and never updated. Inventory audit is unaffected (reads components directly), but any future consumer of the ledger field will get stale data. |
| **Root Cause Status** | Confirmed — Ledger was designed to be maintained by the solver in canonical mode. Canonical mode is never active (CS-0001), so no one updates the ledger after the initial rebase. |
| **Assigned Implementation Plan** | IMPLEMENTATION_PLAN_v0.1.0.0 — Phase A (auto-resolved when canonical mode activated) |
| **Validation Outcome** | Resolved v0.1.0.0 — Auto-resolved by CS-0001. CVCS boundary flows now mutate `TotalPrimaryMass_lb` every timestep in R2/R3. Ledger tracks live state. |
| **Related Issues** | Blocked by: CS-0001. Related: CS-0006 (diagnostic reads stale ledger) |

---

## CS-0003: Boundary Flow Accumulators Never Incremented

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0003 |
| **Title** | Boundary flow accumulators never incremented |
| **Severity** | Medium |
| **Status** | Resolved |
| **Detected In Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | SystemState (CoupledThermo.cs:785-787) / HeatupSimEngine |
| **Discipline** | Mass Conservation — Boundary Accounting |
| **Operational Impact** | `CumulativeCVCSIn_lb`, `CumulativeCVCSOut_lb`, `CumulativeReliefMass_lb` are permanently 0f. Expected-mass formula in diagnostics computes `expected = initial` regardless of CVCS operations. Interval log lines referencing these fields show zeros. |
| **Physics Integrity Impact** | Medium — No runtime impact currently (diagnostic is dead). But if CS-0006 is resolved first, the diagnostic would false-alarm on any net CVCS flow because the expected-mass calculation doesn't account for boundary flows. |
| **Root Cause Status** | Confirmed — Fields declared in v5.3.0 as part of canonical architecture. Increment logic was never added to the CVCS flow paths in `StepSimulation()`. Full codebase grep confirms zero assignment sites. |
| **Assigned Implementation Plan** | IMPLEMENTATION_PLAN_v0.1.0.0 — Phase B |
| **Validation Outcome** | Resolved v0.1.0.0 — Accumulators incremented at 4 sites: R1 solid ops, R2 pre-solver, R3 pre-solver, CVCS.cs post-physics. Double-count guarded by `regime3CVCSPreApplied`. Relief accumulator documented (no two-phase relief physics → stays 0f by design). |
| **Related Issues** | Blocked by: CS-0001. Blocks: CS-0006. |

---

## CS-0004: Pre-Solver V*rho PZR Mass Computation Redundant with Solver

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0004 |
| **Title** | Pre-solver V*rho PZR mass computation redundant with solver |
| **Severity** | Low |
| **Status** | Resolved |
| **Detected In Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | HeatupSimEngine (Regime 2/3 pre-solver blocks) |
| **Discipline** | Mass Conservation — State Ownership |
| **Operational Impact** | Engine writes PZR masses from V*rho (HeatupSimEngine.cs:1164-1165, 1318-1319), then solver overwrites them (CoupledThermo.cs:286-287). Spray condensation adjustments (lines 1197-1211, 1361-1375) are applied to pre-computed values that are immediately discarded. Wasted computation, no user-visible effect. |
| **Physics Integrity Impact** | Low — Functionally harmless in LEGACY mode. With canonical mode active (CS-0001), the pre-computation becomes meaningful (solver reads PZR masses as part of M_total). Spray adjustments would then matter. Behavior change is a side effect of CS-0001 resolution. |
| **Root Cause Status** | Confirmed — v0.9.6 added pre-sync for solver input. In LEGACY mode, solver ignores PZR mass input and recomputes everything. Creates confusion about field ownership. |
| **Assigned Implementation Plan** | IMPLEMENTATION_PLAN_v0.1.0.0 — Phase D (document ownership, evaluate cleanup) |
| **Validation Outcome** | Resolved v0.1.0.0 — Authority ownership documented at R2/R3 headers and CVCS blocks. Default parameter removed from `BulkHeatupStep` (compile-time enforcement). LEGACY path deprecated with 24-line comment block. Pre-sync computation now meaningful under canonical mode. |
| **Related Issues** | Behavior depends on: CS-0001 |

---

## CS-0005: CVCS Double-Count Guard Works by Accident

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0005 |
| **Title** | CVCS double-count guard works by accident |
| **Severity** | Medium |
| **Status** | Resolved |
| **Detected In Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | HeatupSimEngine (CVCS pre-apply) / CoupledThermo (solver) / HeatupSimEngine.CVCS |
| **Discipline** | Mass Conservation — Order of Operations |
| **Operational Impact** | CVCS mass is pre-applied to `RCSWaterMass` (line 1344), solver reads it as part of M_total (line 133), solver overwrites `RCSWaterMass` via V*rho (line 278), guard prevents re-application (line 1351). Net result is correct but mechanism is fragile — CVCS effect is captured implicitly via component sum, not explicitly via ledger. |
| **Physics Integrity Impact** | Medium — Any change to execution order or solver behavior could silently break conservation. The double-count guard masks the underlying fragility. With canonical mode, CVCS must target the ledger instead of a component field. |
| **Root Cause Status** | Confirmed — v4.4.0 fix applied CVCS before solver to fix PZR level. Guard prevents double-counting. Architecturally correct in LEGACY mode but semantically wrong for canonical mode. |
| **Assigned Implementation Plan** | IMPLEMENTATION_PLAN_v0.1.0.0 — Phase A (redirect CVCS to ledger) |
| **Validation Outcome** | Resolved v0.1.0.0 — CVCS now targets `TotalPrimaryMass_lb` (ledger mutation) instead of `RCSWaterMass`. Guard still prevents double-counting in CVCS.cs. Ownership documented in Phase D comments. Mechanism is now architecturally intentional, not accidental. |
| **Related Issues** | Blocked by: CS-0001. Related: CS-0003 (accumulators track the redirected flow) |

---

## CS-0006: UpdatePrimaryMassLedgerDiagnostics() Never Called (Dead Code)

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0006 |
| **Title** | UpdatePrimaryMassLedgerDiagnostics() never called (dead code) |
| **Severity** | High |
| **Status** | Resolved |
| **Detected In Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | HeatupSimEngine.Logging (method definition) / HeatupSimEngine (missing call site) |
| **Discipline** | Diagnostic Enforcement |
| **Operational Impact** | The most sensitive conservation diagnostic (ledger vs component sum, 0.1%/1.0% thresholds) is silent. All display fields it would populate (`primaryMassStatus`, `primaryMassAlarm`, `primaryMassDrift_lb`, etc.) remain at defaults. UI appears "OK" when it should not. Init.cs:184 has a comment referencing it as if it runs — actively misleading. |
| **Physics Integrity Impact** | High — False confidence. The diagnostic's existence in the codebase implies the system is monitored. It is not. Solver-vs-ledger divergence is undetectable at runtime. |
| **Root Cause Status** | Confirmed — Method added in v5.3.0 Stage 6 (Logging.cs:470, 114 lines, fully implemented). Call site in `StepSimulation()` was never added. Full codebase grep confirms zero call sites. |
| **Assigned Implementation Plan** | IMPLEMENTATION_PLAN_v0.1.0.0 — Phase C |
| **Validation Outcome** | Resolved v0.1.0.0 — Call site added at end of `StepSimulation()` after `UpdateInventoryAudit(dt)`. Default status changed from `"OK"` to `"NOT_CHECKED"`. Init reset added for all diagnostic state fields. Diagnostic executes every physics timestep. |
| **Related Issues** | Blocked by: CS-0001, CS-0003. Blocks: CS-0007. |

---

## CS-0007: No UI Display for Primary Ledger Drift

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0007 |
| **Title** | No UI display for primary ledger drift |
| **Severity** | Medium |
| **Status** | Resolved |
| **Detected In Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | HeatupValidationVisual.TabValidation |
| **Discipline** | Diagnostic Enforcement — Observability |
| **Operational Impact** | Even after CS-0006 resurrects the diagnostic, operator/developer has no visual indicator of ledger drift. The three existing mass checks (massError_lbm, VCT flow imbalance, inventory conservation) don't detect solver-vs-ledger divergence. Engine fields exist but are unused by UI. |
| **Physics Integrity Impact** | Medium — Conservation errors at the primary level are invisible unless the developer reads log output. No at-a-glance indicator in the validation tab. |
| **Root Cause Status** | Confirmed — TabValidation.cs:158-242 has 12 PASS/FAIL checks; none reference `primaryMassDrift`. UI was not updated when the diagnostic method was added in v5.3.0. |
| **Assigned Implementation Plan** | IMPLEMENTATION_PLAN_v0.1.0.0 — Phase C |
| **Validation Outcome** | Resolved v0.1.0.0 — "Primary Ledger Drift" row added to TabValidation using `DrawCheckRowThreeState` (100 lb warn / 1000 lb error thresholds). Shows "Not checked yet" until first coupled step, then displays drift percentage. |
| **Related Issues** | Blocked by: CS-0006 |

---

## CS-0008: No Runtime Solver Mass Conservation Check

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0008 |
| **Title** | No runtime solver mass conservation check |
| **Severity** | Medium |
| **Status** | Resolved |
| **Detected In Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | CoupledThermo / RCSHeatup |
| **Discipline** | Mass Conservation — Runtime Validation |
| **Operational Impact** | If the solver introduces mass error (floating-point accumulation, clamping, non-convergence), nobody detects it until downstream `massError_lbm` catches it at the system level (RCS+PZR+VCT+BRS) — significant delay and reduced diagnostic precision. |
| **Physics Integrity Impact** | Medium — In LEGACY mode, `M_total_out` may differ from `M_total_in` because the solver recomputes all masses independently. In canonical mode, the conservation identity holds by construction, but a runtime check provides defense-in-depth. |
| **Root Cause Status** | Confirmed — DEBUG-only conservation check exists in canonical path (CoupledThermo.cs:264-272, dead code). Static `ValidateMassConservation()` test exists (CoupledThermo.cs:516-529, manual runner only). No runtime check after `SolveEquilibrium`. |
| **Assigned Implementation Plan** | IMPLEMENTATION_PLAN_v0.1.0.0 — Phase B |
| **Validation Outcome** | Resolved v0.1.0.0 — Post-solver guard rail added in `RCSHeatup.BulkHeatupStep()`: computes `M_out = RCS + PZR_water + PZR_steam`, compares to canonical ledger. WARNING at >10 lb delta, ERROR at >100 lb delta. Diagnostics only — does not modify state. |
| **Related Issues** | Independent (no dependencies) |

---

## CS-0009: No SG Secondary Energy Balance Validation

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0009 |
| **Title** | No SG secondary energy balance validation |
| **Severity** | Medium |
| **Status** | Deferred |
| **Detected In Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | SGMultiNodeThermal / HeatupSimEngine |
| **Discipline** | Energy Conservation — SG Boundary |
| **Operational Impact** | SG heat removal (`TotalHeatRemoval_MW`) is consumed by solver with no bounds check. Could go negative or exceed input power without detection. T_rcs would go unrealistic before primary-side alarms fire. |
| **Physics Integrity Impact** | Medium — No cross-check between SG heat removal and primary temperature change. SG model is trusted without validation. Impact increases significantly when cooldown and power ascension are implemented. |
| **Root Cause Status** | Confirmed — SG model is treated as a trusted physics module with no output validation. |
| **Assigned Implementation Plan** | IMPLEMENTATION_PLAN_SG_DOMAIN_v0.2.0.0.md |
| **Validation Outcome** | Not Tested |
| **Related Issues** | Related: CS-0010 |
| **Deferral Justification** | Outside v0.1.0.0 architectural domain (SG secondary boundary, not primary mass conservation). Per Constitution Article V Section 5. |

---

## CS-0010: No SG Secondary Pressure Alarm

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0010 |
| **Title** | No SG secondary pressure alarm |
| **Severity** | Low |
| **Status** | Deferred |
| **Detected In Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | AlarmManager / HeatupSimEngine.Alarms |
| **Discipline** | Plant Protection — SG Boundary |
| **Operational Impact** | SG secondary pressure is computed and displayed but has no alarm. Real SG safety valves lift at ~1085 psia. During isolated boiling, SG pressure can rise without annunciation. |
| **Physics Integrity Impact** | Low — Primarily an operational realism gap. No conservation impact. Becomes more relevant for abnormal scenarios and cooldown operations. |
| **Root Cause Status** | Confirmed — AlarmManager built for primary-side alarms. SG secondary alarms were not in scope. |
| **Assigned Implementation Plan** | IMPLEMENTATION_PLAN_SG_DOMAIN_v0.2.0.0.md |
| **Validation Outcome** | Not Tested |
| **Related Issues** | Related: CS-0009 |
| **Deferral Justification** | Outside v0.1.0.0 architectural domain (SG plant protection, not primary mass conservation). Per Constitution Article V Section 5. |

---

## CS-0011: Acceptance Tests Are Formula-Only, Not Simulation-Validated

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0011 |
| **Title** | Acceptance tests are formula-only, not simulation-validated |
| **Severity** | High |
| **Status** | Deferred |
| **Detected In Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | AcceptanceTests_v5_4_0 |
| **Discipline** | Validation Infrastructure |
| **Operational Impact** | All 10 acceptance tests pass by checking calculation correctness. Each contains "REQUIRES SIMULATION" note. `RunAllTests()` returns 10/10 PASSED without running any simulation. The quality gate is satisfied vacuously. |
| **Physics Integrity Impact** | High — False confidence. Tests validate Rules R1-R8 mathematically but don't verify the rules are enforced at runtime (they aren't — CS-0001). Test suite suggests conservation architecture is validated when it is not. |
| **Root Cause Status** | Confirmed — Tests designed as architectural validation (correct formulas). Simulation validation deferred to manual observation. No end-to-end test harness exists. |
| **Assigned Implementation Plan** | IMPLEMENTATION_PLAN_TEST_INFRA_v0.2.1.0.md |
| **Validation Outcome** | Not Tested |
| **Related Issues** | Related: CS-0001 (tests validate rules that aren't enforced) |
| **Deferral Justification** | Outside v0.1.0.0 architectural domain (test infrastructure, not primary mass conservation). Requires v0.1.0.0 canonical mode active for conservation tests to produce meaningful results. Per Constitution Article V Section 5. |

---

## CS-0012: No Regime Transition Logging

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0012 |
| **Title** | No regime transition logging |
| **Severity** | Low |
| **Status** | Deferred |
| **Detected In Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | HeatupSimEngine (regime selection) |
| **Discipline** | Observability |
| **Operational Impact** | When alpha crosses regime boundaries (0 to blended to 1), no event is logged. Plant mode transitions are logged (Alarms.cs:85-89) but regime transitions are not. Debugging mass issues across regime boundaries requires inferring regime from RCP count. |
| **Physics Integrity Impact** | Low — Pure observability gap. No conservation or physics impact. Regime transitions are deterministic from RCP state. |
| **Root Cause Status** | Confirmed — Regime is an internal physics concept, not an operator-visible alarm. Logging was not prioritized. |
| **Assigned Implementation Plan** | IMPLEMENTATION_PLAN_OBSERVABILITY_v0.2.2.0.md |
| **Validation Outcome** | Not Tested |
| **Related Issues** | None |
| **Deferral Justification** | Outside v0.1.0.0 architectural domain (observability, not primary mass conservation). Per Constitution Article V Section 5. |

---

## CS-0013: Session Lifecycle Resets for Canonical Baseline and Solver Log Flag

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0013 |
| **Title** | Session lifecycle resets for canonical baseline and solver log flag |
| **Severity** | Low |
| **Status** | Resolved |
| **Detected In Version** | v0.1.0.0 Phase A (code review during implementation) |
| **System Area** | HeatupSimEngine.Init / CoupledThermo |
| **Discipline** | Mass Conservation — Session Integrity |
| **Operational Impact** | `firstStepLedgerBaselined` (instance field) and `_solverModeLogged` (static field) were never reset in `InitializeSimulation()`. On second simulation run without application restart: (1) ledger rebase skipped — stale baseline from prior run corrupts conservation tracking, (2) solver mode log suppressed — loss of canonical mode visibility. |
| **Physics Integrity Impact** | Low–Medium — Ledger baseline corruption on second run would produce incorrect conservation drift readings. No impact on first run. Solver mode logging is visibility-only. |
| **Root Cause Status** | Confirmed — Phase A introduced `firstStepLedgerBaselined` guard (v0.1.0.0) and `_solverModeLogged` flag (v0.1.0.0). Neither had reset logic in `InitializeSimulation()`. HeatupSimEngine uses `DontDestroyOnLoad`, so instance fields persist across scene reloads. Static fields persist for the entire application lifetime. |
| **Assigned Implementation Plan** | IMPLEMENTATION_PLAN_v0.1.0.0 — Phase A |
| **Validation Outcome** | Resolved — `firstStepLedgerBaselined = false` added at Init.cs:52; `CoupledThermo.ResetSessionFlags()` added at Init.cs:53 and CoupledThermo.cs:51-54. |
| **Related Issues** | Related: CS-0001 (canonical mode activation introduced the flags) |
