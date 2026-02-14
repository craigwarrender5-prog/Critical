# ISSUE REGISTRY (DEPRECATED)

> This file is a preserved historical markdown snapshot and is non-authoritative.
> Authoritative issue records now live in:
> - `Governance/IssueRegister/issue_register.json` (active)
> - `Governance/IssueRegister/issue_archive.json` (closed)
> - `Governance/IssueRegister/issue_index.json` (search index)
## Canonical Issue Registry - Constitution Article III Authority

---

## Registry Control

| Field | Value |
|-------|-------|
| Governing Document | PROJECT_CONSTITUTION.md v0.2.0.0, Article III |
| ID Format | CS-XXXX |
| Source of Truth | This file (`Critical/Updates/ISSUE_REGISTRY.md`) |
| Created | 2026-02-13 |
| Origin | Recovery Audit 2026-02-13 (Stages 0-4) |

No issue may exist outside this registry.

---

## Summary

| Severity | Count |
|----------|-------|
| Critical / BLOCKER | 0 |
| Critical | 10 |
| High | 12 |
| Medium-High | 2 |
| Medium | 15 |
| Low | 9 |
| Low (positive) | 1 |
| **Total** | **51** |

| Status | Count |
|--------|-------|
| Resolved | 16 |
| Fixed (Awaiting Validation) | 1 |
| In Progress | 0 |
| Open - Investigating | 1 |
| Assigned | 26 |
| Deferred | 7 |

---

## Dependency Chain

```
=== Primary Mass Conservation (ALL RESOLVED ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â released as v0.1.0.0) ===

CS-0001  (activate canonical mode)              KEYSTONE .............. RESOLVED v0.1.0.0
    |--- CS-0002  (ledger update in R2/R3) .......................... RESOLVED v0.1.0.0
    |--- CS-0005  (CVCS pre-apply -> ledger) ........................ RESOLVED v0.1.0.0
    |--- CS-0003  (boundary accumulators) ........................... RESOLVED v0.1.0.0
    |       +--- CS-0006  (resurrect diagnostic) .................... RESOLVED v0.1.0.0
    |               +--- CS-0007  (UI row) .......................... RESOLVED v0.1.0.0
    +--- CS-0004  (remove redundant V*rho) .......................... RESOLVED v0.1.0.0
CS-0008  (runtime solver mass check) ............................... RESOLVED v0.1.0.0
CS-0013  (session lifecycle resets) ................................. RESOLVED v0.1.0.0

=== Deferred from Primary Mass Conservation ===

    CS-0009  (SG energy balance validation) ..... DEFERRED -> SG Energy and Pressure Validation
    CS-0010  (SG pressure alarm) ................ DEFERRED -> SG Energy and Pressure Validation
    CS-0011  (acceptance test harness) .......... DEFERRED -> Test Infrastructure
    CS-0012  (regime transition logging) ........ DEFERRED -> Observability
    CS-0037  (surge flow direction + net inventory display) ... ASSIGNED -> Observability

=== SG Secondary Physics [Tier 2, Priority High] ===

CS-0014  (SG ISOLATED mode open boundary) ......... ASSIGNED -> SG Secondary Physics
CS-0015  (no steam accumulation / pressure build) .. ASSIGNED -> SG Secondary Physics
CS-0016  (SG unrealistic heat sink) ................ ASSIGNED -> SG Secondary Physics
    |
    +--- CS-0017  (missing SG pressurize/hold state) ... depends on CS-0014, CS-0015, CS-0018
    |
    +--- CS-0019  (secondary temp locked low) .......... depends on CS-0014, CS-0015, CS-0018
    |
    +--- CS-0020  (secondary inert / wrongly bounded) .. depends on CS-0014ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“CS-0019
CS-0047  (heat-up progression stalls with net heat zero/negative) ... ASSIGNED -> SG Secondary Physics
         Related: CS-0016, CS-0048
CS-0048  (SG remains near atmospheric; behaves as constant heat sink) ASSIGNED -> SG Secondary Physics
         Related: CS-0014, CS-0015, CS-0016

=== Primary Solid Regime (ALL RESOLVED ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â released as v0.2.0.0) ===

CS-0021  (solid-regime pressure decoupled) ......... RESOLVED v0.2.0.0
CS-0022  (early pressurization controller mismatch)  RESOLVED v0.2.0.0
CS-0023  (surge flow rises, pressure flat) ......... RESOLVED v0.2.0.0

=== Bubble Formation and Two-Phase [Tier 2, Priority High] ===

CS-0024  (PZR 100% / zero steam clamping) ......... RESOLVED v0.3.0.0 (Validated-Correct)
CS-0025  (bubble threshold validation) ............. RESOLVED v0.3.0.0 (Validated-Correct)
CS-0026  (post-bubble pressure escalation) ......... ASSIGNED -> Bubble Formation and Two-Phase
CS-0027  (bubble phase labeling inconsistent) ...... ASSIGNED -> Bubble Formation and Two-Phase
CS-0028  (bubble flag timing vs saturation) ........ ASSIGNED -> Bubble Formation and Two-Phase
CS-0029  (high pressure ramp, zero heat rate) ...... ASSIGNED -> Bubble Formation and Two-Phase
CS-0030  (nonlinear pressure-CVCS sensitivity) ..... ASSIGNED -> Bubble Formation and Two-Phase
CS-0036  (DRAIN phase duration excessive ~4 hr) ..... ASSIGNED -> Bubble Formation and Two-Phase
         Diagnostic: net drain rate audit, CVCS opposing flows, unit conversion, level model
CS-0043  (PZR pressure boundary failure ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â runaway depressurization) ... FIXED v0.3.2.0 (Awaiting Validation)
         CRITICAL: Dual energy application in Regime 1 two-phase. Stage E FAILED.
         Fix: Two-phase bypass in IsolatedHeatingStep, Psat override removed, net heater power in DRAIN.
         Related: CS-0036 (same root cause likely), CS-0026, CS-0029
CS-0049  (PZR fails to recover pressure in two-phase) .... ASSIGNED -> Bubble Formation and Two-Phase
         Related: CS-0043, CS-0036

=== Bubble Formation and Two-Phase ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DRAIN Phase [Tier 2, Priority High] ===

CS-0040  (RVLIS stale during PZR drain) .............. ASSIGNED -> Bubble Formation and Two-Phase
         Related: CS-0036 (DRAIN duration excessive)

=== RCS Regime Transition [Tier 2, Priority High] ===

CS-0038  (PZR level spike on RCP start) .............. ASSIGNED -> RCS Energy Balance and Regime Transition
         Related: CS-0031 (RCP thermal inertia)

=== RCP Thermal Inertia [Tier 3, Priority Medium] ===

CS-0031  (RCP heat rate numerically aggressive) .... ASSIGNED -> RCP Thermal Inertia

=== Performance / Runtime Architecture [Tier 0, RESOLVED ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Phases B-D DEFERRED] ===

CS-0032  (UI/Input unresponsive ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â main thread starvation) ... RESOLVED v0.3.1.1
         Phase A performance fixes + flicker fix confirmed stable
CS-0044  (async log writer ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â background I/O) ................ DEFERRED
CS-0045  (physics snapshot boundary) ........................ DEFERRED (depends on CS-0044)
CS-0046  (physics parallelization ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â off-thread sim) ......... DEFERRED (depends on CS-0044, CS-0045)

=== VCT / CVCS Flow Accounting [Tier 2, Priority High] ===

CS-0039  (VCT conservation error growth ~1,700 gal)  ASSIGNED -> CVCS Energy Balance and VCT Flow Accounting
         Related: CS-0035 (CVCS thermal mixing missing)

=== Mass & Energy Conservation - Post-IP-0015 Audit Regression [Tier 1, Priority Critical] ===

CS-0051  (Stage E mass conservation discontinuity at 8.25 hr, solid->two-phase handoff) CLOSED -> Mass & Energy Conservation
         Related: CS-0050, CS-0052, IP-0016 Stage E validation evidence
CS-0050  (persistent plant-wide mass conservation imbalance ~10,000 gal class) CLOSED -> Mass & Energy Conservation
         Related: CS-0039, CS-0041, CS-0052, IP-0016 Stage E validation evidence
CS-0052  (post-RTCC residual long-run conservation divergence after 8.50 hr) CLOSED -> Mass & Energy Conservation
         Related: CS-0050, CS-0051, IP-0016 Stage E validation evidence


=== Validation / Diagnostic Display [Tier 3, Priority Medium] ===

CS-0041  (inventory audit baseline type mismatch) ... ASSIGNED -> Validation and Diagnostic Display
         Related: CS-0007 (ledger drift UI)

=== Operator & Scenario Framework ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Dashboard [Phase 1, Priority Medium] ===

CS-0042  (professional interactive dashboard / uGUI modernization) ... ASSIGNED -> Operator and Scenario Framework
         BLOCKED by Phase 0 exit. Subsumes: CS-0037 (surge flow direction display).
         Related: CS-0041 (inventory display), v5.5.0.0 roadmap entry

=== Core Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â RCS Energy Balance [Tier 2, Priority High] ===

CS-0033  (RCS T_avg rise with RCPs OFF, no forced flow) .. RESOLVED v0.3.1.1
         Finding A resolved (flow-coupled pump heat). Findings B/C spun out to CS-0034/CS-0035.
CS-0034  (no equilibrium ceiling in Regime 0/1) .......... ASSIGNED -> RCS Energy Balance and Regime Transition
         Originated: CS-0033 Finding B. Related: CS-0014ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“CS-0020 (SG heat sink)
CS-0035  (CVCS thermal mixing missing) ................... ASSIGNED -> CVCS Energy Balance and VCT Flow Accounting
         Originated: CS-0033 Finding C. Related: CS-0034

```

---

## CS-0001: CoupledThermo Canonical Mass Mode Never Activated

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0001 |
| **Title** | CoupledThermo canonical mass mode never activated |
| **Severity** | Critical |
| **Status** | CLOSED |
| **Closed Date** | 2026-02-14 |
| **Closed Under** | IP-0017 |
| **Closure Evidence** | Updates/Issues/IP-0017_StageE_NonRegression_2026-02-14_164742.md; HeatupLogs/IP-0017_RunA_20260214_164402/Unity_StageE_IP0017_RunA_20260214_164347.log:163926; HeatupLogs/IP-0017_RunB_20260214_164459/Unity_StageE_IP0017_RunB_20260214_164445.log:163872; HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_034_8.25hr.txt:209 |
| **Detected in Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | CoupledThermo / RCSHeatup / HeatupSimEngine |
| **Discipline** | Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Authority Enforcement |
| **Operational Impact** | Solver runs in LEGACY V*rho mode every timestep in Regime 2/3. Conservation architecture (Rules R1, R3, R5) exists as dead code. All component masses derived from V*rho ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â no conservation-by-construction. |
| **Physics Integrity Impact** | Critical ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â The canonical ledger `TotalPrimaryMass_lb` is decorative. Mass conservation is not provable. The entire v5.4.0 architecture exists only in comments and unreachable code paths. Physics results are reasonable but the system cannot prove its own correctness. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â `BulkHeatupStep()` parameter `totalPrimaryMass_lb` defaults to `0f` (RCSHeatup.cs:114). Neither call site (HeatupSimEngine.cs:1214-1217, 1378-1381) passes the 10th argument. Gate at CoupledThermo.cs:123 (`useCanonicalMass = totalPrimaryMass_lb > 0f`) is always false. |
| **Assigned Implementation Plan** | IP-0017 - DP-0005 Remaining Closure Governance |
| **Validation Outcome** | Closed under IP-0017 governance (2026-02-14): explicit canonical authority proof captured in Run A/Run B logs with canonical-mode activation and canonical mass source continuity. |
| **Related Issues** | Blocks: CS-0002, CS-0003, CS-0005, CS-0006 |
| **Authorization Status** | AUTHORIZED (retroactive) |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Mass & Energy Conservation |
| **Assigned DP ID** | DP-0005 |
| **Assigned IP ID** | IP-0017 |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Authority Enforcement |

---

## CS-0002: TotalPrimaryMass_lb Freezes After First Step in Regime 2/3

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0002 |
| **Title** | TotalPrimaryMass_lb freezes after first step in Regime 2/3 |
| **Severity** | High |
| **Status** | CLOSED |
| **Closed Date** | 2026-02-14 |
| **Closed Under** | IP-0017 |
| **Closure Evidence** | Updates/Issues/IP-0017_StageE_NonRegression_2026-02-14_164742.md; HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_034_8.25hr.txt:35; HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_035_8.50hr.txt:35; HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_036_8.75hr.txt:35; HeatupLogs/IP-0017_RunB_20260214_164459/Heatup_Interval_035_8.50hr.txt:35 |
| **Detected in Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | HeatupSimEngine (mass ledger maintenance) |
| **Discipline** | Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Ledger Integrity |
| **Operational Impact** | Ledger diverges from actual state as CVCS flows accumulate. Any diagnostic reading `TotalPrimaryMass_lb` gets a stale value frozen at the first-step rebase. |
| **Physics Integrity Impact** | High ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â The "Primary Mass Ledger" concept is broken. The ledger field is written once (HeatupSimEngine.cs:1455) and never updated. Inventory audit is unaffected (reads components directly), but any future consumer of the ledger field will get stale data. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Ledger was designed to be maintained by the solver in canonical mode. Canonical mode is never active (CS-0001), so no one updates the ledger after the initial rebase. |
| **Assigned Implementation Plan** | IP-0017 - DP-0005 Remaining Closure Governance |
| **Validation Outcome** | Closed under IP-0017 governance (2026-02-14): ledger continuity evidence passed across transition/follow-on/post-bubble windows and repeat-run window with no freeze signature under active boundary flow. |
| **Related Issues** | Blocked by: CS-0001. Related: CS-0006 (diagnostic reads stale ledger) |
| **Authorization Status** | AUTHORIZED (retroactive) |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Mass & Energy Conservation |
| **Assigned DP ID** | DP-0005 |
| **Assigned IP ID** | IP-0017 |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | Blocked by: CS-0001. Related: CS-0006 (diagnostic reads stale ledger) |
| **Subdomain (Freeform)** | Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Ledger Integrity |

---

## CS-0003: Boundary Flow Accumulators Never Incremented

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0003 |
| **Title** | Boundary flow accumulators never incremented |
| **Severity** | Medium |
| **Status** | CLOSED |
| **Closed Date** | 2026-02-14 |
| **Closed Under** | IP-0017 |
| **Closure Evidence** | Updates/Issues/IP-0017_StageE_NonRegression_2026-02-14_164742.md; HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt:43; HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt:44; HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt:45; HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt:46 |
| **Detected in Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | SystemState (CoupledThermo.cs:785-787) / HeatupSimEngine |
| **Discipline** | Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Boundary Accounting |
| **Operational Impact** | `CumulativeCVCSIn_lb`, `CumulativeCVCSOut_lb`, `CumulativeReliefMass_lb` are permanently 0f. Expected-mass formula in diagnostics computes `expected = initial` regardless of CVCS operations. Interval log lines referencing these fields show zeros. |
| **Physics Integrity Impact** | Medium ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â No runtime impact currently (diagnostic is dead). But if CS-0006 is resolved first, the diagnostic would false-alarm on any net CVCS flow because the expected-mass calculation doesn't account for boundary flows. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Fields declared in v5.3.0 as part of canonical architecture. Increment logic was never added to the CVCS flow paths in `StepSimulation()`. Full codebase grep confirms zero assignment sites. |
| **Assigned Implementation Plan** | IP-0017 - DP-0005 Remaining Closure Governance |
| **Validation Outcome** | Closed under IP-0017 governance (2026-02-14): accumulator/audit totals are populated and internally consistent in Stage E Run A/B evidence. |
| **Related Issues** | Blocked by: CS-0001. Blocks: CS-0006. |
| **Authorization Status** | AUTHORIZED (retroactive) |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Mass & Energy Conservation |
| **Assigned DP ID** | DP-0005 |
| **Assigned IP ID** | IP-0017 |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | Blocked by: CS-0001. Blocks: CS-0006. |
| **Subdomain (Freeform)** | Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Boundary Accounting |

---

## CS-0004: Pre-Solver V*rho PZR Mass Computation Redundant with Solver

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0004 |
| **Title** | Pre-solver V*rho PZR mass computation redundant with solver |
| **Severity** | Low |
| **Status** | CLOSED |
| **Closed Date** | 2026-02-14 |
| **Closed Under** | IP-0017 |
| **Closure Evidence** | Updates/Issues/IP-0017_StageE_NonRegression_2026-02-14_164742.md; HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_034_8.25hr.txt:232; HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_034_8.25hr.txt:234; HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_034_8.25hr.txt:239 |
| **Detected in Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | HeatupSimEngine (Regime 2/3 pre-solver blocks) |
| **Discipline** | Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â State Ownership |
| **Operational Impact** | Engine writes PZR masses from V*rho (HeatupSimEngine.cs:1164-1165, 1318-1319), then solver overwrites them (CoupledThermo.cs:286-287). Spray condensation adjustments (lines 1197-1211, 1361-1375) are applied to pre-computed values that are immediately discarded. Wasted computation, no user-visible effect. |
| **Physics Integrity Impact** | Low ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Functionally harmless in LEGACY mode. With canonical mode active (CS-0001), the pre-computation becomes meaningful (solver reads PZR masses as part of M_total). Spray adjustments would then matter. Behavior change is a side effect of CS-0001 resolution. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â v0.9.6 added pre-sync for solver input. In LEGACY mode, solver ignores PZR mass input and recomputes everything. Creates confusion about field ownership. |
| **Assigned Implementation Plan** | IP-0017 - DP-0005 Remaining Closure Governance |
| **Validation Outcome** | Closed under IP-0017 governance (2026-02-14): transition authority evidence shows no overwrite signature; RTCC assert delta remains 0.000 lbm. |
| **Related Issues** | Behavior depends on: CS-0001 |
| **Authorization Status** | AUTHORIZED (retroactive) |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Mass & Energy Conservation |
| **Assigned DP ID** | DP-0005 |
| **Assigned IP ID** | IP-0017 |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | depends on: CS-0001 |
| **Subdomain (Freeform)** | Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â State Ownership |

---

## CS-0005: CVCS Double-Count Guard Works by Accident

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0005 |
| **Title** | CVCS double-count guard works by accident |
| **Severity** | Medium |
| **Status** | CLOSED |
| **Closed Date** | 2026-02-14 |
| **Closed Under** | IP-0017 |
| **Closure Evidence** | Updates/Issues/IP-0017_StageE_NonRegression_2026-02-14_164742.md; HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt:22; HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_035_8.50hr.txt:243; HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_035_8.50hr.txt:254 |
| **Detected in Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | HeatupSimEngine (CVCS pre-apply) / CoupledThermo (solver) / HeatupSimEngine.CVCS |
| **Discipline** | Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Order of Operations |
| **Operational Impact** | CVCS mass is pre-applied to `RCSWaterMass` (line 1344), solver reads it as part of M_total (line 133), solver overwrites `RCSWaterMass` via V*rho (line 278), guard prevents re-application (line 1351). Net result is correct but mechanism is fragile ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â CVCS effect is captured implicitly via component sum, not explicitly via ledger. |
| **Physics Integrity Impact** | Medium ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Any change to execution order or solver behavior could silently break conservation. The double-count guard masks the underlying fragility. With canonical mode, CVCS must target the ledger instead of a component field. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â v4.4.0 fix applied CVCS before solver to fix PZR level. Guard prevents double-counting. Architecturally correct in LEGACY mode but semantically wrong for canonical mode. |
| **Assigned Implementation Plan** | IP-0017 - DP-0005 Remaining Closure Governance |
| **Validation Outcome** | Closed under IP-0017 governance (2026-02-14): PBOC single-owner/single-apply pairing assertions remain zero-failure through required windows. |
| **Related Issues** | Blocked by: CS-0001. Related: CS-0003 (accumulators track the redirected flow) |
| **Authorization Status** | AUTHORIZED (retroactive) |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Mass & Energy Conservation |
| **Assigned DP ID** | DP-0005 |
| **Assigned IP ID** | IP-0017 |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | Blocked by: CS-0001. Related: CS-0003 (accumulators track the redirected flow) |
| **Subdomain (Freeform)** | Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Order of Operations |

---

## CS-0006: UpdatePrimaryMassLedgerDiagnostics() Never Called (Dead Code)

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0006 |
| **Title** | UpdatePrimaryMassLedgerDiagnostics() never called (dead code) |
| **Severity** | High |
| **Status** | Assigned |
| **Detected in Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | HeatupSimEngine.Logging (method definition) / HeatupSimEngine (missing call site) |
| **Discipline** | Diagnostic Enforcement |
| **Operational Impact** | The most sensitive conservation diagnostic (ledger vs component sum, 0.1%/1.0% thresholds) is silent. All display fields it would populate (`primaryMassStatus`, `primaryMassAlarm`, `primaryMassDrift_lb`, etc.) remain at defaults. UI appears "OK" when it should not. Init.cs:184 has a comment referencing it as if it runs ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â actively misleading. |
| **Physics Integrity Impact** | High ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â False confidence. The diagnostic's existence in the codebase implies the system is monitored. It is not. Solver-vs-ledger divergence is undetectable at runtime. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Method added in v5.3.0 Stage 6 (Logging.cs:470, 114 lines, fully implemented). Call site in `StepSimulation()` was never added. Full codebase grep confirms zero call sites. |
| **Assigned Implementation Plan** | IP-0001 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Primary Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Phase C |
| **Validation Outcome** | Resolved v0.1.0.0 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Call site added at end of `StepSimulation()` after `UpdateInventoryAudit(dt)`. Default status changed from `"OK"` to `"NOT_CHECKED"`. Init reset added for all diagnostic state fields. Diagnostic executes every physics timestep. |
| **Related Issues** | Blocked by: CS-0001, CS-0003. Blocks: CS-0007. |
| **Authorization Status** | AUTHORIZED (retroactive) |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Validation & Diagnostics |
| **Assigned DP ID** | DP-0007 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | Blocked by: CS-0001, CS-0003. Blocks: CS-0007. |
| **Subdomain (Freeform)** | Diagnostic Enforcement |

---

## CS-0007: No UI Display for Primary Ledger Drift

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0007 |
| **Title** | No UI display for primary ledger drift |
| **Severity** | Medium |
| **Status** | Assigned |
| **Detected in Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | HeatupValidationVisual.TabValidation |
| **Discipline** | Diagnostic Enforcement ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Observability |
| **Operational Impact** | Even after CS-0006 resurrects the diagnostic, operator/developer has no visual indicator of ledger drift. The three existing mass checks (massError_lbm, VCT flow imbalance, inventory conservation) don't detect solver-vs-ledger divergence. Engine fields exist but are unused by UI. |
| **Physics Integrity Impact** | Medium ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Conservation errors at the primary level are invisible unless the developer reads log output. No at-a-glance indicator in the validation tab. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â TabValidation.cs:158-242 has 12 PASS/FAIL checks; none reference `primaryMassDrift`. UI was not updated when the diagnostic method was added in v5.3.0. |
| **Assigned Implementation Plan** | IP-0001 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Primary Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Phase C |
| **Validation Outcome** | Resolved v0.1.0.0 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â "Primary Ledger Drift" row added to TabValidation using `DrawCheckRowThreeState` (100 lb warn / 1000 lb error thresholds). Shows "Not checked yet" until first coupled step, then displays drift percentage. |
| **Related Issues** | Blocked by: CS-0006 |
| **Authorization Status** | AUTHORIZED (retroactive) |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Validation & Diagnostics |
| **Assigned DP ID** | DP-0007 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | Blocked by: CS-0006 |
| **Subdomain (Freeform)** | Diagnostic Enforcement ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Observability |

---

## CS-0008: No Runtime Solver Mass Conservation Check

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0008 |
| **Title** | No runtime solver mass conservation check |
| **Severity** | Medium |
| **Status** | CLOSED |
| **Closed Date** | 2026-02-14 |
| **Closed Under** | IP-0017 |
| **Closure Evidence** | Updates/Issues/IP-0017_StageE_NonRegression_2026-02-14_164742.md; Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_164359.md:21; Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_164359.md:28; HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt:18 |
| **Detected in Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | CoupledThermo / RCSHeatup |
| **Discipline** | Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Runtime Validation |
| **Operational Impact** | If the solver introduces mass error (floating-point accumulation, clamping, non-convergence), nobody detects it until downstream `massError_lbm` catches it at the system level (RCS+PZR+VCT+BRS) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â significant delay and reduced diagnostic precision. |
| **Physics Integrity Impact** | Medium ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â In LEGACY mode, `M_total_out` may differ from `M_total_in` because the solver recomputes all masses independently. In canonical mode, the conservation identity holds by construction, but a runtime check provides defense-in-depth. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DEBUG-only conservation check exists in canonical path (CoupledThermo.cs:264-272, dead code). Static `ValidateMassConservation()` test exists (CoupledThermo.cs:516-529, manual runner only). No runtime check after `SolveEquilibrium`. |
| **Assigned Implementation Plan** | IP-0017 - DP-0005 Remaining Closure Governance |
| **Validation Outcome** | Closed under IP-0017 governance (2026-02-14): Stage E non-regression evidence shows conservation guardrail outcomes in-threshold with no new conservation regressions. |
| **Related Issues** | Independent (no dependencies) |
| **Authorization Status** | AUTHORIZED (retroactive) |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Mass & Energy Conservation |
| **Assigned DP ID** | DP-0005 |
| **Assigned IP ID** | IP-0017 |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Runtime Validation |

---

## CS-0009: No SG Secondary Energy Balance Validation

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0009 |
| **Title** | No SG secondary energy balance validation |
| **Severity** | Medium |
| **Status** | Assigned |
| **Detected in Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | SGMultiNodeThermal / HeatupSimEngine |
| **Discipline** | Energy Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â SG Boundary |
| **Operational Impact** | SG heat removal (`TotalHeatRemoval_MW`) is consumed by solver with no bounds check. Could go negative or exceed input power without detection. T_rcs would go unrealistic before primary-side alarms fire. |
| **Physics Integrity Impact** | Medium ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â No cross-check between SG heat removal and primary temperature change. SG model is trusted without validation. Impact increases significantly when cooldown and power ascension are implemented. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â SG model is treated as a trusted physics module with no output validation. |
| **Assigned Implementation Plan** | IP-0007 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â SG Energy and Pressure Validation |
| **Validation Outcome** | Not Tested |
| **Related Issues** | Related: CS-0010 |
| **Deferral Justification** | Outside v0.1.0.0 architectural domain (SG secondary boundary, not primary mass conservation). Per Constitution Article V Section 5. |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Steam Generator Secondary Physics |
| **Assigned DP ID** | DP-0003 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Energy Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â SG Boundary |

---

## CS-0010: No SG Secondary Pressure Alarm

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0010 |
| **Title** | No SG secondary pressure alarm |
| **Severity** | Low |
| **Status** | Assigned |
| **Detected in Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | AlarmManager / HeatupSimEngine.Alarms |
| **Discipline** | Plant Protection ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â SG Boundary |
| **Operational Impact** | SG secondary pressure is computed and displayed but has no alarm. Real SG safety valves lift at ~1085 psia. During isolated boiling, SG pressure can rise without annunciation. |
| **Physics Integrity Impact** | Low ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Primarily an operational realism gap. No conservation impact. Becomes more relevant for abnormal scenarios and cooldown operations. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â AlarmManager built for primary-side alarms. SG secondary alarms were not in scope. |
| **Assigned Implementation Plan** | IP-0007 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â SG Energy and Pressure Validation |
| **Validation Outcome** | Not Tested |
| **Related Issues** | Related: CS-0009 |
| **Deferral Justification** | Outside v0.1.0.0 architectural domain (SG plant protection, not primary mass conservation). Per Constitution Article V Section 5. |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Plant Protection & Limits |
| **Assigned DP ID** | DP-0006 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Plant Protection ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â SG Boundary |

---

## CS-0011: Acceptance Tests Are Formula-Only, Not Simulation-Validated

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0011 |
| **Title** | Acceptance tests are formula-only, not simulation-validated |
| **Severity** | High |
| **Status** | Assigned |
| **Detected in Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | AcceptanceTests_v5_4_0 |
| **Discipline** | Validation Infrastructure |
| **Operational Impact** | All 10 acceptance tests pass by checking calculation correctness. Each contains "REQUIRES SIMULATION" note. `RunAllTests()` returns 10/10 PASSED without running any simulation. The quality gate is satisfied vacuously. |
| **Physics Integrity Impact** | High ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â False confidence. Tests validate Rules R1-R8 mathematically but don't verify the rules are enforced at runtime (they aren't ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â CS-0001). Test suite suggests conservation architecture is validated when it is not. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Tests designed as architectural validation (correct formulas). Simulation validation deferred to manual observation. No end-to-end test harness exists. |
| **Assigned Implementation Plan** | IP-0010 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Test Infrastructure |
| **Validation Outcome** | Not Tested |
| **Related Issues** | Related: CS-0001 (tests validate rules that aren't enforced) |
| **Deferral Justification** | Outside v0.1.0.0 architectural domain (test infrastructure, not primary mass conservation). Requires v0.1.0.0 canonical mode active for conservation tests to produce meaningful results. Per Constitution Article V Section 5. |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Validation & Diagnostics |
| **Assigned DP ID** | DP-0007 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Validation Infrastructure |

---

## CS-0012: No Regime Transition Logging

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0012 |
| **Title** | No regime transition logging |
| **Severity** | Low |
| **Status** | Assigned |
| **Detected in Version** | Pre-v0.1.0.0 (legacy 5.x lineage, recovery audit) |
| **System Area** | HeatupSimEngine (regime selection) |
| **Discipline** | Observability |
| **Operational Impact** | When alpha crosses regime boundaries (0 to blended to 1), no event is logged. Plant mode transitions are logged (Alarms.cs:85-89) but regime transitions are not. Debugging mass issues across regime boundaries requires inferring regime from RCP count. |
| **Physics Integrity Impact** | Low ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Pure observability gap. No conservation or physics impact. Regime transitions are deterministic from RCP state. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Regime is an internal physics concept, not an operator-visible alarm. Logging was not prioritized. |
| **Assigned Implementation Plan** | IP-0012 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Observability |
| **Validation Outcome** | Not Tested |
| **Related Issues** | None |
| **Deferral Justification** | Outside v0.1.0.0 architectural domain (observability, not primary mass conservation). Per Constitution Article V Section 5. |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Validation & Diagnostics |
| **Assigned DP ID** | DP-0007 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Observability |

---

## CS-0013: Session Lifecycle Resets for Canonical Baseline and Solver Log Flag

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0013 |
| **Title** | Session lifecycle resets for canonical baseline and solver log flag |
| **Severity** | Low |
| **Status** | Assigned |
| **Detected in Version** | v0.1.0.0 Phase A (code review during implementation) |
| **System Area** | HeatupSimEngine.Init / CoupledThermo |
| **Discipline** | Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Session Integrity |
| **Operational Impact** | `firstStepLedgerBaselined` (instance field) and `_solverModeLogged` (static field) were never reset in `InitializeSimulation()`. On second simulation run without application restart: (1) ledger rebase skipped ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â stale baseline from prior run corrupts conservation tracking, (2) solver mode log suppressed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â loss of canonical mode visibility. |
| **Physics Integrity Impact** | LowÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“Medium ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Ledger baseline corruption on second run would produce incorrect conservation drift readings. No impact on first run. Solver mode logging is visibility-only. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Phase A introduced `firstStepLedgerBaselined` guard (v0.1.0.0) and `_solverModeLogged` flag (v0.1.0.0). Neither had reset logic in `InitializeSimulation()`. HeatupSimEngine uses `DontDestroyOnLoad`, so instance fields persist across scene reloads. Static fields persist for the entire application lifetime. |
| **Assigned Implementation Plan** | IP-0017 - DP-0005 Remaining Closure Governance |
| **Validation Outcome** | Prior fix implemented, but closure is pending strict same-process A/B evidence. IP-0017 Run A/B parity is numerically matching, but process-identity gate failed (`Updates/Issues/IP-0017_RunA_RunB_SameProcess_2026-02-14_164742.md`). |
| **Related Issues** | Related: CS-0001 (canonical mode activation introduced the flags) |
| **Authorization Status** | AUTHORIZED (retroactive) |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Mass & Energy Conservation |
| **Assigned DP ID** | DP-0005 |
| **Assigned IP ID** | IP-0017 |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | Requires same-process repeat-run parity proof per IP-0017 fail-closed gate before closure. |
| **Subdomain (Freeform)** | Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Session Integrity |

---

## CS-0014: SG "ISOLATED" Mode Behaves Like Open Inventory Pressure Boundary

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0014 |
| **Title** | SG "ISOLATED" mode behaves like open inventory pressure boundary |
| **Severity** | Critical |
| **Status** | Implemented Ã¯Â¿Â½ Pending Validation |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“16.50 hr) |
| **System Area** | SGMultiNodeThermal / HeatupSimEngine (SG interface) |
| **Discipline** | SG Secondary Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Pressure Boundary |
| **Operational Impact** | Secondary boils hard at low pressure (few psig) with no meaningful pressurization. SG becomes an infinite heat sink, preventing progress toward ~550Ãƒâ€šÃ‚Â°F secondary operation. Plant cannot reach operating region. |
| **Physics Integrity Impact** | Critical ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Results dominated by low-pressure boiling. Entire heatup narrative is distorted by SG acting as vented system. No path to pressurized secondary. |
| **Root Cause Status** | Confirmed (domain-localized) - runtime path keeps SteamIsolated=false by default and no runtime caller invokes SetSteamIsolation(...), so SG remains in open-boundary steam outflow behavior. |
| **Evidence Reference(s)** | Assets/Scripts/Physics/SGMultiNodeThermal.cs:765,1418-1428,1546-1553; search evidence: SetSteamIsolation definition only; Updates/Issues/DP-0003_Preliminary_Investigation_Report.md |
| **Assigned Implementation Plan** | IP-0015 - DP-0003 SG Secondary Physics Stabilization and Heat-up Progression Recovery |
| **Validation Outcome** | Implemented Ã¯Â¿Â½ Pending Stage E validation |
| **Related Issues** | Related: CS-0015, CS-0016, CS-0018, CS-0019. Blocks: CS-0020. |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-SG-001 |
| **Authorization Status** | AUTHORIZED (Implementation Phase) |
| **Mode** | IMPLEMENTED/PENDING_VALIDATION |
| **Investigation Report ID(s)** | Updates/Issues/DP-0003_Preliminary_Investigation_Report.md |
| **Domain (Canonical)** | Steam Generator Secondary Physics |
| **Assigned DP ID** | DP-0003 |
| **Assigned IP ID** | IP-0015 |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | Dependency root for SG startup blocker chain: CS-0014 -> CS-0015 -> CS-0018 -> CS-0048 -> CS-0047. |
| **Subdomain (Freeform)** | SG Secondary Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Pressure Boundary |

---

## CS-0015: Steam Generation Does Not Accumulate Compressible Volume/Mass

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0015 |
| **Title** | Steam generation does not accumulate compressible volume/mass (no internal pressure build) |
| **Severity** | Critical |
| **Status** | Implemented Ã¯Â¿Â½ Pending Validation |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“16.50 hr) |
| **System Area** | SGMultiNodeThermal |
| **Discipline** | SG Secondary Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Compressible Volume |
| **Operational Impact** | Boiling occurs but produced vapor effectively "doesn't stay" to build pressure ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â acts like a vented system. Locks Tsat low; permanent boiling; no path to pressurized secondary. |
| **Physics Integrity Impact** | Critical ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Steam mass/energy is generated but not tracked as compressible volume. Violates conservation of mass on the secondary side. |
| **Root Cause Status** | Confirmed (domain-localized) - steam inventory is computed but open branch sets outflow equal to production, collapsing net inventory accumulation while SteamIsolated=false. |
| **Evidence Reference(s)** | Assets/Scripts/Physics/SGMultiNodeThermal.cs:1426-1435,1463-1467; Updates/Issues/DP-0003_Preliminary_Investigation_Report.md |
| **Assigned Implementation Plan** | IP-0015 - DP-0003 SG Secondary Physics Stabilization and Heat-up Progression Recovery |
| **Validation Outcome** | Implemented Ã¯Â¿Â½ Pending Stage E validation |
| **Related Issues** | Related: CS-0014, CS-0018. Blocks: CS-0019, CS-0020. |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-SG-002 |
| **Authorization Status** | AUTHORIZED (Implementation Phase) |
| **Mode** | IMPLEMENTED/PENDING_VALIDATION |
| **Investigation Report ID(s)** | Updates/Issues/DP-0003_Preliminary_Investigation_Report.md |
| **Domain (Canonical)** | Steam Generator Secondary Physics |
| **Assigned DP ID** | DP-0003 |
| **Assigned IP ID** | IP-0015 |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | Blocked by: CS-0014. Precedes: CS-0018, CS-0048, CS-0047. |
| **Subdomain (Freeform)** | SG Secondary Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Compressible Volume |

---

## CS-0016: SG Modeled as Unrealistically Strong Heat Sink During Heatup

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0016 |
| **Title** | SG modeled as unrealistically strong heat sink during heatup |
| **Severity** | Critical |
| **Status** | Implemented Ã¯Â¿Â½ Pending Validation |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“16.50 hr) |
| **System Area** | SGMultiNodeThermal / HeatupSimEngine |
| **Discipline** | SG Secondary Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Heat Balance |
| **Operational Impact** | SG secondary heat removal dominates global heat balance; can drive negative RCS heatup rate even with large primary heat inputs. Breaks the entire heatup narrative. |
| **Physics Integrity Impact** | Critical ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Primary heat balance is invalid. SG removes more heat than is physically possible given secondary conditions (low-pressure, low-temperature). |
| **Root Cause Status** | Suspected - SG sink magnitude can dominate source heat during startup under low-pressure/open-boundary conditions; pressure feedback and sink bounding are insufficient in the observed failure window. |
| **Evidence Reference(s)** | Assets/Scripts/Physics/SGMultiNodeThermal.cs:1141-1142,1476; Assets/Scripts/Physics/RCSHeatup.cs:135; Technical_Documentation/SG_Secondary_Pressurization_During_Heatup_Research.md:13; Updates/Archive/IMPLEMENTATION_PLAN_v5.1.0.md:37-38; Updates/Issues/DP-0003_Preliminary_Investigation_Report.md |
| **Assigned Implementation Plan** | IP-0015 - DP-0003 SG Secondary Physics Stabilization and Heat-up Progression Recovery |
| **Validation Outcome** | Implemented Ã¯Â¿Â½ Pending Stage E validation |
| **Related Issues** | Related: CS-0014, CS-0015. Blocks: CS-0020. Related: CS-0009 (SG energy balance validation). |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-SG-003 |
| **Authorization Status** | AUTHORIZED (Implementation Phase) |
| **Mode** | IMPLEMENTED/PENDING_VALIDATION |
| **Investigation Report ID(s)** | Updates/Issues/DP-0003_Preliminary_Investigation_Report.md |
| **Domain (Canonical)** | Steam Generator Secondary Physics |
| **Assigned DP ID** | DP-0003 |
| **Assigned IP ID** | IP-0015 |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | Parallel contributor with CS-0014/CS-0015; blocks closure of CS-0048 and CS-0047. |
| **Subdomain (Freeform)** | SG Secondary Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Heat Balance |

---

## CS-0017: Missing SG Pressurization/Hold State in Startup Procedure

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0017 |
| **Title** | Missing SG pressurization/hold state in startup procedure |
| **Severity** | High |
| **Status** | Assigned |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“16.50 hr) |
| **System Area** | HeatupSimEngine (SG state machine) / SGMultiNodeThermal |
| **Discipline** | SG Secondary Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Startup Procedure |
| **Operational Impact** | Procedure jumps into low-pressure boiling/vent-like behavior instead of sealed warmup/pressurization. Missing SG_SECONDARY_PRESSURIZE_HOLD state where secondary is sealed, pressure rises, Tsat rises, and boiling is delayed/controlled. |
| **Physics Integrity Impact** | High ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Without a pressurization hold state, the SG cannot transition from cold shutdown to operating conditions in a realistic sequence. The startup logic is fundamentally incomplete. |
| **Root Cause Status** | Suspected ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â SG startup state machine does not include a sealed pressurization phase. State transitions go directly from cold to boiling without intermediate pressurization. |
| **Assigned Implementation Plan** | IP-0006 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â SG Secondary Physics |
| **Validation Outcome** | Not Tested |
| **Related Issues** | Depends on: CS-0014 (sealed boundary), CS-0015 (steam accumulation), CS-0018 (NÃƒÂ¢Ã¢â‚¬Å¡Ã¢â‚¬Å¡ blanket). Related: CS-0016. |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-SG-004 |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Steam Generator Secondary Physics |
| **Assigned DP ID** | DP-0003 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | Depends on: CS-0014 (sealed boundary), CS-0015 (steam accumulation), CS-0018 (NÃƒÂ¢Ã¢â‚¬Å¡Ã¢â‚¬Å¡ blanket). |
| **Subdomain (Freeform)** | SG Secondary Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Startup Procedure |

---

## CS-0018: NÃƒÂ¢Ã¢â‚¬Å¡Ã¢â‚¬Å¡ Blanket Treated as Pressure Clamp, Not Compressible Cushion

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0018 |
| **Title** | NÃƒÂ¢Ã¢â‚¬Å¡Ã¢â‚¬Å¡ blanket treated as pressure clamp, not compressible cushion |
| **Severity** | High |
| **Status** | Implemented Ã¯Â¿Â½ Pending Validation |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“16.50 hr) |
| **System Area** | SGMultiNodeThermal / PlantConstants.SG |
| **Discipline** | SG Secondary Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Gas Volume Model |
| **Operational Impact** | Nitrogen blanket holds pressure near constant rather than compressing with heating. Prevents natural pressure build-up from thermal expansion and steam generation. |
| **Physics Integrity Impact** | High ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Blanket should act as PV=nRT compressible gas coupled to liquid expansion and steam generation. Current clamp behavior prevents the secondary from pressurizing realistically. |
| **Root Cause Status** | Suspected - N2 behavior is represented as isolate flag plus fixed pressure-floor branching, not a full compressible gas state contribution. |
| **Evidence Reference(s)** | Assets/Scripts/Physics/SGMultiNodeThermal.cs:1871-1874,1896-1908; Assets/Scripts/Physics/PlantConstants.SG.cs:663,676,790; Updates/Issues/DP-0003_Preliminary_Investigation_Report.md |
| **Assigned Implementation Plan** | IP-0015 - DP-0003 SG Secondary Physics Stabilization and Heat-up Progression Recovery |
| **Validation Outcome** | Implemented Ã¯Â¿Â½ Pending Stage E validation |
| **Related Issues** | Related: CS-0014, CS-0015. Blocks: CS-0017 (pressurization state depends on compressible blanket). |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-SG-005 |
| **Authorization Status** | AUTHORIZED (Implementation Phase) |
| **Mode** | IMPLEMENTED/PENDING_VALIDATION |
| **Investigation Report ID(s)** | Updates/Issues/DP-0003_Preliminary_Investigation_Report.md |
| **Domain (Canonical)** | Steam Generator Secondary Physics |
| **Assigned DP ID** | DP-0003 |
| **Assigned IP ID** | IP-0015 |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | Blocked by: CS-0015 (and CS-0014). Precedes: CS-0048, CS-0047. |
| **Subdomain (Freeform)** | SG Secondary Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Gas Volume Model |

---

## CS-0019: Secondary Temperature Cannot Progress Toward High-Pressure Saturation Region

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0019 |
| **Title** | Secondary temperature cannot progress toward high-pressure saturation region |
| **Severity** | High |
| **Status** | Assigned |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“16.50 hr) |
| **System Area** | SGMultiNodeThermal |
| **Discipline** | SG Secondary Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Temperature Progression |
| **Operational Impact** | Model stuck near low Tsat (~225Ãƒâ€šÃ‚Â°F-scale), hence constant boiling and no route to 550Ãƒâ€šÃ‚Â°F secondary operation. SG cannot reach normal operating conditions. |
| **Physics Integrity Impact** | High ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Secondary temperature is thermodynamically locked to low-pressure saturation. Without pressure build (CS-0014, CS-0015, CS-0018), Tsat cannot rise, creating a self-reinforcing low-temperature trap. |
| **Root Cause Status** | Suspected ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Downstream consequence of CS-0014 (no sealed boundary), CS-0015 (no steam accumulation), and CS-0018 (NÃƒÂ¢Ã¢â‚¬Å¡Ã¢â‚¬Å¡ blanket clamp behavior). |
| **Assigned Implementation Plan** | IP-0006 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â SG Secondary Physics |
| **Validation Outcome** | Not Tested |
| **Related Issues** | Blocked by: CS-0014, CS-0015, CS-0018. Related: CS-0016. |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-SG-006 |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Steam Generator Secondary Physics |
| **Assigned DP ID** | DP-0003 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | Blocked by: CS-0014, CS-0015, CS-0018. Related: CS-0016. |
| **Subdomain (Freeform)** | SG Secondary Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Temperature Progression |

---

## CS-0020: Secondary Remains Largely Inert or Wrongly Bounded During Primary Heatup

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0020 |
| **Title** | Secondary remains largely inert or wrongly bounded during primary heatup |
| **Severity** | Medium |
| **Status** | Assigned |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“16.50 hr) |
| **System Area** | SGMultiNodeThermal / HeatupSimEngine |
| **Discipline** | SG Secondary Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Dynamic Response |
| **Operational Impact** | Secondary conditions don't evolve realistically during primary heatup / RCP operation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â either too inert or clamped. SG secondary temperatures and pressures are non-responsive to primary-side changes. |
| **Physics Integrity Impact** | Medium ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Secondary side should respond dynamically to primary heat input, RCP operation, and thermal coupling. Current behavior is static or unrealistic. Many symptoms may resolve once SG sealing/pressurization state is implemented correctly (CS-0014 through CS-0019). |
| **Root Cause Status** | Suspected ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Downstream consequence of fundamental SG model defects (CS-0014 through CS-0019). |
| **Assigned Implementation Plan** | IP-0006 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â SG Secondary Physics |
| **Validation Outcome** | Not Tested |
| **Related Issues** | Blocked by: CS-0014, CS-0015, CS-0016, CS-0018, CS-0019. |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-LOG-005 / ISSUE-LOG-010 / ISSUE-LOG-015 (consolidated) |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Steam Generator Secondary Physics |
| **Assigned DP ID** | DP-0003 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | Blocked by: CS-0014, CS-0015, CS-0016, CS-0018, CS-0019. |
| **Subdomain (Freeform)** | SG Secondary Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Dynamic Response |

---

## CS-0021: Solid-Regime Pressure Decoupled from Mass Change

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0021 |
| **Title** | Solid-regime pressure decoupled from mass change |
| **Severity** | High |
| **Status** | Assigned |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0-16.50 hr) |
| **System Area** | SolidPlantPressure / HeatupSimEngine (solid-regime path) |
| **Discipline** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Solid Regime |
| **Operational Impact** | RCS pressure pinned (~365 psia) while CVCS net remains non-zero (often negative) in solid pressurizer regime. Evidence at 5.00 hr: 365.0 psia flat while heating continues. |
| **Physics Integrity Impact** | High ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Invalid wet-layover solid behavior. Masks true compressibility/thermal expansion dynamics. Pressure should respond to both mass change and thermal expansion in a closed, liquid-full system. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â PI controller output (`LetdownAdjustEff`) applied to `LetdownFlow` in same computation step as pressure equation. Zero-delay feedback loop allowed `dV_cvcs` to exactly cancel `dV_thermal` within ~2 minutes, producing `dV_net -> 0 -> dP -> 0`. |
| **Assigned Implementation Plan** | IP-0002 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Primary Solid Regime ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Phase A |
| **Validation Outcome** | Resolved v0.2.0.0 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â CVCS transport delay (60s ring buffer, read-before-write) prevents same-step cancellation. PressureRate > 0 for 100% of HEATER_PRESSURIZE steps. HOLD_SOLID oscillation 11-14 psi P-P (naturally damping). Mass conservation 0.02 lbm at 5 hr. |
| **Related Issues** | Related: CS-0022, CS-0023. |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-LOG-001 |
| **Authorization Status** | AUTHORIZED (retroactive) |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Primary Thermodynamics |
| **Assigned DP ID** | DP-0001 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Solid Regime |

---

## CS-0022: Early Pressurization Response Mismatched with Controller Actuation

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0022 |
| **Title** | Early pressurization response mismatched with controller actuation |
| **Severity** | Medium |
| **Status** | Assigned |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0-16.50 hr) |
| **System Area** | CVCSController / SolidPlantPressure / HeatupSimEngine |
| **Discipline** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Controller Coupling |
| **Operational Impact** | Large pressure changes occur versus apparently limited effective flow/actuation early in run. Suggests scaling, sign, or actuator clamp mismatch between controller output and pressure model input. |
| **Physics Integrity Impact** | Medium ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Pressure response magnitude doesn't match the control action, suggesting a gain or scaling error in the controller-to-physics interface. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â During HEATER_PRESSURIZE, +/-1 gpm authority clamp allowed PI controller to consistently inject ~1 gpm net charging (error always large negative), accelerating pressurization beyond pure thermal expansion. Integral accumulated to 2000 psi-sec limit pointlessly during saturated period. |
| **Assigned Implementation Plan** | IP-0002 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Primary Solid Regime ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Phase A |
| **Validation Outcome** | Resolved v0.2.0.0 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Anti-windup inhibits integral accumulation when actuator saturated (HEATER_PRESSURIZE clamp, slew limiter) or dead-time gap > 0.5 gpm. Transport delay ensures +/-1 gpm bias arrives 60s late, producing modest contribution rather than same-step amplification. Pressurization rate now thermal-expansion-dominated. |
| **Related Issues** | Related: CS-0021, CS-0023. |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-LOG-002 |
| **Authorization Status** | AUTHORIZED (retroactive) |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Primary Thermodynamics |
| **Assigned DP ID** | DP-0001 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Controller Coupling |

---

## CS-0023: Surge Flow Trend Rises While Pressure Remains Flat

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0023 |
| **Title** | Surge flow trend rises while pressure remains flat |
| **Severity** | Medium |
| **Status** | Assigned |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0-16.50 hr) |
| **System Area** | SolidPlantPressure / HeatupSimEngine (solid-regime surge flow) |
| **Discipline** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Solid Regime |
| **Operational Impact** | Surge flow increases across the solid-hold period even while pressure is pinned. Indicates a compensating mechanism (controller/volume bookkeeping) rather than physical response. In a closed system, surge flow and pressure should be coupled. |
| **Physics Integrity Impact** | Medium ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Decoupled surge and pressure suggests volume/mass bookkeeping inconsistency. Surge flow may be computed from thermal expansion while pressure is independently clamped. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Secondary symptom of CS-0021. Surge flow correctly derived from PZR thermal expansion (dV_pzr). Pressure was flat because CVCS instantly cancelled dV_thermal. Trends were physically consistent individually but appeared decoupled due to zero transport delay. |
| **Assigned Implementation Plan** | IP-0002 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Primary Solid Regime ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Phase C (verification only) |
| **Validation Outcome** | Resolved v0.2.0.0 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â No code change to surge flow required (calculation was correct). SurgePressureConsistent diagnostic added: 100% consistency in both HEATER_PRESSURIZE (260/260 steps) and HOLD_SOLID (2712/2712 steps). Resolved naturally by CS-0021 transport delay fix. |
| **Related Issues** | Related: CS-0021, CS-0022. |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-LOG-004 |
| **Authorization Status** | AUTHORIZED (retroactive) |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Primary Thermodynamics |
| **Assigned DP ID** | DP-0001 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Solid Regime |

---

## CS-0024: PZR 100% Level, Zero Steam Model May Be Clamping Dynamics

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0024 |
| **Title** | PZR 100% level, zero steam model may be clamping dynamics |
| **Severity** | Medium |
| **Status** | Assigned |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“16.50 hr) |
| **System Area** | PressurizerPhysics / HeatupSimEngine.BubbleFormation |
| **Discipline** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â PZR State Machine |
| **Operational Impact** | Extended periods with PZR at 100% level with no steam volume while heaters run. Can hide expansion/volume bookkeeping errors and affects realism of the bubble formation transition. |
| **Physics Integrity Impact** | Medium ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â If PZR is water-solid at 100%, thermal expansion must go somewhere. Clamping at 100% without steam volume may absorb expansion errors silently. |
| **Root Cause Status** | Confirmed correct ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â PZR at 100% level with zero steam is physically correct for solid plant operations. The solid-regime pressurizer IS water-full by definition (per NRC HRTD 19.2). Thermal expansion of PZR water exits through the surge line, reflected in rising Surge Flow values (1.2ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢8.2 gpm across solid regime). The expansion is accommodated by CVCS PI-controlled letdown balance (v0.2.0.0 transport delay fix). No clamping error exists. |
| **Assigned Implementation Plan** | IP-0003 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Bubble Formation and Two-Phase ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Phase B |
| **Validation Outcome** | Resolved v0.3.0.0 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Investigation confirms behavior is physically correct. Log evidence: Surge Flow rises monotonically (1.2ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢8.2 gpm) during solid ops, confirming thermal expansion exits through surge line. PZR level at 100% with zero steam is the correct initial condition for solid plant. No code change required. |
| **Related Issues** | Related: CS-0028 (bubble flag timing). |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-LOG-003 |
| **Authorization Status** | AUTHORIZED (retroactive) |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Pressurizer & Two-Phase Physics |
| **Assigned DP ID** | DP-0002 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â PZR State Machine |

---

## CS-0025: Bubble Detection Threshold Alignment (Validation Item)

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0025 |
| **Title** | Bubble detection threshold aligns with saturation (validation item) |
| **Severity** | Low |
| **Status** | Assigned |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“16.50 hr) |
| **System Area** | HeatupSimEngine.BubbleFormation |
| **Discipline** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Bubble Detection |
| **Operational Impact** | Bubble detection occurs near Tsat crossing (positive), but robustness across modes and edge cases should be verified. This is primarily a validation/confidence item. |
| **Physics Integrity Impact** | Low ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Current behavior appears correct in nominal cases. Risk is in edge cases or mode transitions where threshold may not trigger reliably. |
| **Root Cause Status** | Confirmed correct ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Bubble detection triggers at T_pzr = 435.43Ãƒâ€šÃ‚Â°F vs T_sat = 435.83Ãƒâ€šÃ‚Â°F (0.4Ãƒâ€šÃ‚Â°F subcooling margin) at 8.25 hr sim time. Detection occurs within 1Ãƒâ€šÃ‚Â°F of saturation, which is the correct threshold per SolidPlantPressure detection logic. The T_pzr ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ T_sat crossing is clean and produces immediate DETECTION phase transition with no pressure discontinuity (P remains 365.1 psia through detection). |
| **Assigned Implementation Plan** | IP-0003 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Bubble Formation and Two-Phase ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Phase B |
| **Validation Outcome** | Resolved v0.3.0.0 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Investigation confirms detection threshold is correct. Log evidence: T_pzr = 435.43Ãƒâ€šÃ‚Â°F at detection vs T_sat = 435.83Ãƒâ€šÃ‚Â°F (0.4Ãƒâ€šÃ‚Â°F margin). Pressure continuous through detection (365.1 psia before and after). No code change required. |
| **Related Issues** | Related: CS-0028. |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-LOG-007 |
| **Authorization Status** | AUTHORIZED (retroactive) |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Pressurizer & Two-Phase Physics |
| **Assigned DP ID** | DP-0002 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Bubble Detection |

---

## CS-0026: Post-Bubble Pressure Escalation Magnitude Questionable

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0026 |
| **Title** | Post-bubble pressure escalation magnitude questionable |
| **Severity** | Medium-High |
| **Status** | Assigned |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“16.50 hr) |
| **System Area** | CoupledThermo / PressurizerPhysics / HeatupSimEngine |
| **Discipline** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Two-Phase Energy Balance |
| **Operational Impact** | Rapid pressure rise after bubble onset despite strongly negative net CVCS at times. Pressure escalation magnitude appears inconsistent with the energy/mass inputs. |
| **Physics Integrity Impact** | Medium-High ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Suggests two-phase energy balance or compressible volume error. Pressure should respond to the balance of steam generation rate, condensation, and CVCS flows ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â not escalate against negative net flow. |
| **Root Cause Status** | Suspected ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Two-phase pressure coupling in CoupledThermo or PressurizerPhysics may overweight steam generation or underweight mass removal. Alternatively, energy input to PZR may be incorrectly high. |
| **Assigned Implementation Plan** | IP-0003 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Bubble Formation and Two-Phase |
| **Validation Outcome** | Not Tested |
| **Related Issues** | Related: CS-0029, CS-0030. |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-LOG-008 |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Pressurizer & Two-Phase Physics |
| **Assigned DP ID** | DP-0002 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Two-Phase Energy Balance |

---

## CS-0027: Bubble Phase Labeling Inconsistent with Observed Thermodynamics

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0027 |
| **Title** | Bubble phase labeling inconsistent with observed thermodynamics |
| **Severity** | Medium |
| **Status** | Assigned |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“16.50 hr) |
| **System Area** | HeatupSimEngine.BubbleFormation |
| **Discipline** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â State Machine Logic |
| **Operational Impact** | Phase reported as "DRAIN" while pressure and steam behavior indicate pressurization/boiling transition behavior. Misleading telemetry that can hide wrong control branch execution. |
| **Physics Integrity Impact** | Medium ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Incorrect phase label means the control logic may be executing the wrong branch. DRAIN phase should show decreasing PZR water level and controlled letdown; if pressure is rising, the system is actually in PRESSURIZE or transition. |
| **Root Cause Status** | Suspected ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â State machine transition conditions may not check pressure/steam behavior, only water level or timer-based triggers. |
| **Assigned Implementation Plan** | IP-0003 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Bubble Formation and Two-Phase |
| **Validation Outcome** | Not Tested |
| **Related Issues** | Related: CS-0028, CS-0024. |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-LOG-009 |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Pressurizer & Two-Phase Physics |
| **Assigned DP ID** | DP-0002 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â State Machine Logic |

---

## CS-0028: Bubble Flag/State Timing Inconsistent with Saturation and Pressure Rise

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0028 |
| **Title** | Bubble flag/state timing inconsistent with saturation and pressure rise |
| **Severity** | High |
| **Status** | Assigned |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“16.50 hr) |
| **System Area** | HeatupSimEngine.BubbleFormation / HeatupSimEngine |
| **Discipline** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â State Machine Timing |
| **Operational Impact** | Conditions show Tsat behavior and rapid pressurization while "Bubble Formed = NO" persists for some intervals. Wrong state gating leads to wrong control logic path ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â the system may be executing solid-regime logic when it should be in two-phase. |
| **Physics Integrity Impact** | High ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â State gating error means the wrong physics model branch runs. If bubble exists but flag says NO, the engine runs SolidPlantPressure instead of CoupledThermo/PressurizerPhysics, producing incorrect pressure and mass distribution. |
| **Root Cause Status** | Suspected ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Bubble detection may have a latency or hysteresis that delays flag transition while thermodynamic conditions have already crossed saturation. Alternatively, detection may check wrong state variable. |
| **Assigned Implementation Plan** | IP-0003 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Bubble Formation and Two-Phase |
| **Validation Outcome** | Not Tested |
| **Related Issues** | Related: CS-0025, CS-0027, CS-0024. |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-LOG-011 |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Pressurizer & Two-Phase Physics |
| **Assigned DP ID** | DP-0002 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â State Machine Timing |

---

## CS-0029: Very High Pressure Ramp While RCS Heat Rate Near Zero

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0029 |
| **Title** | Very high pressure ramp while RCS heat rate near zero |
| **Severity** | Medium-High |
| **Status** | Assigned |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“16.50 hr) |
| **System Area** | CoupledThermo / PressurizerPhysics / HeatupSimEngine |
| **Discipline** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Two-Phase Energy Balance |
| **Operational Impact** | Pressure climbs strongly while RCS temperature barely moves in certain window. Pressure and temperature should be coupled through saturation properties ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â pressure rising without temperature change suggests energy accounting or compressibility defect. |
| **Physics Integrity Impact** | Medium-High ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Points to energy accounting, compressibility, or state coupling defect. In two-phase equilibrium, pressure and temperature are locked together through Tsat(P). Decoupled behavior indicates a model error. |
| **Root Cause Status** | Suspected ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Pressure may be driven by PZR heater energy alone (steam compression) while RCS temperature response is suppressed by large thermal mass or SG heat removal. Alternatively, pressure model may have a decoupled pathway. |
| **Assigned Implementation Plan** | IP-0003 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Bubble Formation and Two-Phase |
| **Validation Outcome** | Not Tested |
| **Related Issues** | Related: CS-0026, CS-0030. |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-LOG-012 |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Pressurizer & Two-Phase Physics |
| **Assigned DP ID** | DP-0002 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Two-Phase Energy Balance |

---

## CS-0030: Nonlinear/Inconsistent Sensitivity of Pressure to CVCS Sign Changes

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0030 |
| **Title** | Nonlinear/inconsistent sensitivity of pressure to CVCS sign changes |
| **Severity** | Medium |
| **Status** | Assigned |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“16.50 hr) |
| **System Area** | CoupledThermo / HeatupSimEngine (CVCS-to-pressure coupling) |
| **Discipline** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Mass-Pressure Coupling |
| **Operational Impact** | CVCS net flips sign, but pressure response doesn't scale plausibly. Adding mass should raise pressure (in two-phase: compresses steam bubble); removing mass should lower pressure. Response magnitude should be roughly proportional to mass change. |
| **Physics Integrity Impact** | Medium ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Mass-pressure coupling is still weak or overridden in two-phase regime. With canonical mass now active (v0.1.0.0), this coupling should be more explicit, but the symptom suggests the solver's pressure response to mass change is not scaled correctly. |
| **Root Cause Status** | Suspected ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â CoupledThermo pressure iteration may not weight mass change correctly in the convergence loop. The solver may be dominated by thermal effects and insensitive to mass-driven pressure changes. |
| **Assigned Implementation Plan** | IP-0003 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Bubble Formation and Two-Phase |
| **Validation Outcome** | Not Tested |
| **Related Issues** | Related: CS-0026, CS-0029. |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-LOG-013 |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Pressurizer & Two-Phase Physics |
| **Assigned DP ID** | DP-0002 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Mass-Pressure Coupling |

---

## CS-0031: RCS Heat Rate Escalation After RCP Start May Be Numerically Aggressive

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0031 |
| **Title** | RCS heat rate escalation after RCP start may be numerically aggressive |
| **Severity** | Medium |
| **Status** | Assigned |
| **Detected in Version** | v0.1.0.0 (heatup log review, 0ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“16.50 hr) |
| **System Area** | RCSHeatup / HeatupSimEngine (RCP regime transition) / PlantConstants |
| **Discipline** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Thermal Inertia |
| **Operational Impact** | RCS heatup rate jumps dramatically as RCP heat comes online. Evidence at 14.25 hr: high RCS heat rate with RCP heat ~18.9 MW. Could indicate missing thermal mass, wrong Cp, or applying pump heat to too little inventory. |
| **Physics Integrity Impact** | Medium ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â If thermal mass is underestimated, the simulator overpredicts heatup rate and may reach temperature milestones too quickly. NRC reference is ~50Ãƒâ€šÃ‚Â°F/hr with 4 RCPs; deviation beyond 30-80Ãƒâ€šÃ‚Â°F/hr range indicates a model error. |
| **Root Cause Status** | Suspected ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â RCS heat capacity computation may not include full metal thermal mass (piping, vessel, internals). Alternatively, Cp temperature dependence may be missing or water mass used for heat capacity may be too low. |
| **Assigned Implementation Plan** | IP-0005 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â RCP Thermal Inertia |
| **Validation Outcome** | Not Tested |
| **Related Issues** | None |
| **Source Document** | Heatup Master Issue Registry ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ISSUE-LOG-014 |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Primary Thermodynamics |
| **Assigned DP ID** | DP-0001 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Thermal Inertia |

---

## CS-0032: UI/Input Unresponsive After Editor Close (Main Thread Starvation Suspected)

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0032 |
| **Title** | UI/Input unresponsive after Editor close (main thread starvation suspected) |
| **Severity** | Critical / BLOCKER |
| **Status** | Assigned |
| **Resolved In Version** | v0.3.1.1 (Phase A performance fixes + flicker fix confirmed stable) |
| **Detected in Version** | v0.3.0.0 (runtime observation during validation runs) |
| **System Area** | HeatupSimEngine / HeatupValidationVisual / Unity Main Thread |
| **Discipline** | Performance / Runtime Architecture |
| **Operational Impact** | Simulation continues updating (physics loop runs) but selecting/navigating UI becomes unresponsive ("hang" perception). Onset is delayed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â not immediate. Trigger correlates with closing Unity Editor during or after a run. No obvious memory growth. **Blocks overnight validation runs and operator usability.** |
| **Physics Integrity Impact** | None directly ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â physics continues executing. However, inability to interact with the running simulation prevents validation, parameter adjustment, and controlled shutdown. Effectively blocks all long-run acceptance testing (Phase E of any domain plan). |
| **Root Cause Status** | Under investigation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Multiple hypotheses: (1) main-thread starvation from UI rebuild/logging/physics competing for frame budget, (2) message pump not serviced during heavy computation, (3) file I/O stalls from synchronous log writes, (4) GC spikes from per-frame string allocations, (5) frame pacing issues (uncapped FPS or vsync interaction). |
| **Assigned Implementation Plan** | IP-0009 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Performance and Runtime Architecture |
| **Validation Outcome** | Not Tested |
| **Related Issues** | Cross-cutting ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â affects all domain validation runs. Supersedes all non-runtime work in priority. |
| **Priority Override** | **CRITICAL / BLOCKER** ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Must be addressed before further long-run validation (Phase E of any domain). |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Performance & Runtime |
| **Assigned DP ID** | DP-0009 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Performance / Runtime Architecture |

---

## CS-0033: RCS Bulk Temperature Rise with RCPs OFF and No Confirmed Forced Flow

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0033 |
| **Title** | RCS bulk temperature rise with RCPs OFF and no confirmed forced flow |
| **Severity** | High |
| **Status** | Assigned |
| **Detected in Version** | v0.3.0.0 (diagnostic audit) |
| **System Area** | HeatupSimEngine / RCSHeatup / SolidPlantPressure / RHRSystem / HeatTransfer |
| **Discipline** | Core Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Primary Energy Balance |
| **Operational Impact** | During cold shutdown heatup (Regime 0 and Regime 1, rcpCount=0), RCS bulk temperature rises from PZR heater conduction through the surge line and from RHR pump heat. Surge line natural convection is the intended mechanism, but the RHR pump heat (1 MW total) is injected without explicit hydraulic flow validation. Additionally, there is no mechanism for T_avg to reach thermal equilibrium ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â with small constant heat input and no SG heat sink, T_avg rises indefinitely (minus insulation loss). While insulation loss does exist and is correctly applied, it is a weak function at low temperatures (0.063 MW at 100Ãƒâ€šÃ‚Â°F vs 1.0 MW at 400Ãƒâ€šÃ‚Â°F), making early-phase equilibrium behavior unrealistic. |
| **Physics Integrity Impact** | **High** ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Three sub-findings with cumulative impact on physical realism: |
| | **(A) RHR pump heat without flow coupling:** RHR injects 1 MW pump heat into RCS unconditionally whenever Mode ÃƒÂ¢Ã¢â‚¬Â°Ã‚Â  Standby. During `RHRMode.Isolating` ramp-down, pump heat persists with diminishing flow. No validation that suction/discharge valves are aligned or that actual hydraulic flow exists. This violates the principle that mechanical energy transfer requires a valid coupling mechanism. |
| | **(B) No equilibrium behavior:** With all active heat sources except PZR heaters disabled, T_rcs still rises indefinitely via surge line conduction. The insulation loss model (`Q_loss = 0.00314 ÃƒÆ’Ã¢â‚¬â€ (T_rcs - 80)` MW) is linear and correct but too weak at low temperatures to counterbalance 1.8 MW PZR heater power (conducted to RCS at ~0.025-0.14 MW via surge line). Equilibrium would require either a stronger loss model or an SG heat sink ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â neither exists in Regime 0/1. |
| | **(C) Asymptotic equilibrium gap:** Real plant physics: at steady state with no forced flow and heaters on, T_pzr saturates at Tsat(P) and T_rcs equilibrates at T_pzr minus the ÃƒÅ½Ã¢â‚¬ÂT driven by the UA of the surge line. The simulator lacks this equilibrium ceiling because pressure rises continuously via thermal expansion (solid regime) or Psat(T_pzr) (two-phase regime), and T_rcs follows T_pzr through conduction. |
| **Root Cause Status** | Confirmed by code audit ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â three distinct root causes identified (see Diagnostic Audit below). |
| **Diagnostic Audit** | See detailed findings below this table. |
| **Assigned Implementation Plan** | IP-0004 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â RCS Energy Balance and Regime Transition |
| **Validation Outcome** | Pass ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Finding A resolved v0.3.1.1 (flow-coupled pump heat). Finding B deferred to CS-0034. Finding C deferred to CS-0035. |
| **Related Issues** | CS-0031 (RCP thermal inertia ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â affects Regime 2/3 heat rates). CS-0009/CS-0010 (SG validation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â SG heat sink is absent in Regime 0/1). CS-0034 (Finding B spun out). CS-0035 (Finding C spun out). |
| **Acceptance Criteria** | (1) With all active heat sources disabled, RCS T_avg does not increase. (2) With small constant heat input and no SG heat sink, T_avg rises and asymptotically approaches equilibrium. (3) Pump heat (RHR or RCP) affects RCS bulk only when a valid coupling mechanism (flow/alignment or explicitly defined mixing mode) exists. |
| **Authorization Status** | AUTHORIZED (retroactive) |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Primary Thermodynamics |
| **Assigned DP ID** | DP-0001 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Core Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Primary Energy Balance |

### CS-0033 Diagnostic Audit ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Complete Heat Term Inventory

#### RCS dT/dt Energy Balance by Regime

**Regime 0 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Solid Pressurizer (SolidPlantPressure.Update):**

| Heat Term | Source | Direction | Flow-Gated? | Value | Code Location |
|-----------|--------|-----------|-------------|-------|---------------|
| PZR heater ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ PZR water | Electric heaters | IN (+) to PZR | No (always on) | 1.8 MW | SolidPlantPressure.cs:420-421 |
| Surge line conduction PZRÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢RCS | Natural convection | IN (+) to RCS | No (thermal gradient) | 0.025-0.14 MW | SolidPlantPressure.cs:424-427 |
| RCS insulation loss | Conduction to ambient | OUT (-) from RCS | No (always active) | 0.00314ÃƒÆ’Ã¢â‚¬â€(T_rcs-80) MW | SolidPlantPressure.cs:463-464 |
| PZR insulation loss | Conduction to ambient | OUT (-) from PZR | No (always active) | 50kWÃƒÆ’Ã¢â‚¬â€(ÃƒÅ½Ã¢â‚¬ÂT/570) | SolidPlantPressure.cs:430-436 |
| **RHR pump heat** | **Pump friction** | **IN (+) to RCS** | **NO ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â unconditional** | **1.0 MW** | **HeatupSimEngine.cs:1062-1065** |
| RHR HX removal | Heat exchanger | OUT (-) from RCS | Yes (bypass fraction) | Variable | RHRSystem.cs:671 |

**Net RCS equation (Regime 0):**
`dT_rcs/dt = (Q_surge + Q_rhr_net - Q_insulation) / C_rcs`

**Regime 1 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Bubble Exists, No RCPs (IsolatedHeatingStep):**

| Heat Term | Source | Direction | Flow-Gated? | Value | Code Location |
|-----------|--------|-----------|-------------|-------|---------------|
| Surge line conduction PZRÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢RCS | Natural convection | IN (+) to RCS | No (thermal gradient) | 0.025-0.14 MW | RCSHeatup.cs:307 |
| RCS insulation loss | Conduction to ambient | OUT (-) from RCS | No (always active) | 0.00314ÃƒÆ’Ã¢â‚¬â€(T_rcs-80) MW | RCSHeatup.cs:339-341 |
| **RHR pump heat** | **Pump friction** | **IN (+) to RCS** | **NO ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â unconditional** | **1.0 MW (until 585 psig)** | **HeatupSimEngine.cs:1149-1152** |

**Net RCS equation (Regime 1):**
`dT_rcs/dt = (Q_surge - Q_insulation + Q_rhr_net) / C_rcs`

**Regime 2/3 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â RCPs Running (BulkHeatupStep):**

| Heat Term | Source | Direction | Flow-Gated? | Value | Code Location |
|-----------|--------|-----------|-------------|-------|---------------|
| RCP heat | Pump work | IN (+) | YES (rcpCount gated) | 5.25 MW/pump | RCSHeatup.cs:131, 250 |
| PZR heater | Electric heaters | IN (+) | No (always on) | 1.8 MW | RCSHeatup.cs:131 |
| Insulation loss | Conduction to ambient | OUT (-) | No (always active) | 0.00314ÃƒÆ’Ã¢â‚¬â€(T-80) MW | HeatTransfer.cs:310-315 via :336-340 |
| SG heat removal | PrimaryÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢secondary | OUT (-) | YES (RCP flow required) | Variable | RCSHeatup.cs:135 |

**Net RCS equation (Regime 2/3):**
`dT_rcs/dt = (Q_rcp + Q_heaters - Q_insulation - Q_sg) / C_rcs`

#### Non-Zero Hydraulic Coupling Pathways Audit

| Flow Path | Active in Regime 0/1? | Contributes Heat to RCS? | Flow-Verified? |
|-----------|----------------------|--------------------------|----------------|
| CVCS Charging | YES (75 gpm nominal) | No (mass only, no thermal model for charging temperature) | Yes (flow rate tracked) |
| CVCS Letdown | YES (75 gpm nominal) | No (mass only) | Yes (flow rate tracked) |
| RHR Loop | YES (until 585 psig) | **YES ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â 1 MW pump heat** | **NO ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â unconditional on mode** |
| Seal Return | No (no RCPs) | N/A | N/A |
| Surge Line | YES (natural convection) | YES ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â conduction model | N/A (buoyancy-driven, not flow-gated) |
| SG Primary | YES (SG model runs) | Negligible (HTC_NO_RCPS ÃƒÂ¢Ã¢â‚¬Â°Ã‹â€  8) | By design (very low HTC) |

#### Finding Summary

| Finding | Severity | Impact |
|---------|----------|--------|
| **(A) RHR pump heat without flow validation** | **High ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â RESOLVED v0.3.0.0** | Flow-coupled guard added in `RHRSystem.UpdateActive()`: `hydraulicCoupled = SuctionValvesOpen && FlowRate_gpm > 0`. Isolation ramp-down was already flow-coupled (scales with `flowFraction`). Validation tests 9/10 added. |
| **(B) No equilibrium ceiling in Regime 0/1** | **Medium** | T_rcs rises indefinitely via surge conduction minus weak insulation loss. No mechanism caps T_rcs below T_pzr. In reality, T_rcs cannot exceed Tsat(P) - ÃƒÅ½Ã¢â‚¬ÂT_surge_UA. This is by-design for heatup (T_rcs is meant to rise), but the RATE may be unrealistic without proper flow coupling. |
| **(C) CVCS thermal contribution missing** | **Low** | Charging water enters at ~100Ãƒâ€šÃ‚Â°F (VCT temperature) but is modeled as mass-only with no thermal mixing. At 75 gpm with ÃƒÅ½Ã¢â‚¬ÂT of 300Ãƒâ€šÃ‚Â°F, this represents ~0.15 MW of cooling ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â currently ignored. Not critical for heatup accuracy but contributes to the energy balance gap. |

---

## CS-0034: No Equilibrium Ceiling for RCS Temperature in Regime 0/1

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0034 |
| **Title** | No equilibrium ceiling for RCS temperature in Regime 0/1 (pre-RCP) |
| **Severity** | Medium |
| **Status** | Assigned |
| **Detected in Version** | v0.3.0.0 (CS-0033 diagnostic audit, Finding B) |
| **System Area** | RCSHeatup / SolidPlantPressure / HeatupSimEngine |
| **Discipline** | Core Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Primary Energy Balance |
| **Operational Impact** | With all active heat sources except PZR heaters disabled, T_rcs rises indefinitely via surge line conduction. The insulation loss model (`Q_loss = 0.00314 ÃƒÆ’Ã¢â‚¬â€ (T_rcs - 80)` MW) is linear and correct but too weak at low temperatures to counterbalance 1.8 MW PZR heater power (conducted to RCS at ~0.025ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“0.14 MW via surge line). No mechanism caps T_rcs below T_pzr. In reality, T_rcs cannot exceed Tsat(P) ÃƒÂ¢Ã‹â€ Ã¢â‚¬â„¢ ÃƒÅ½Ã¢â‚¬ÂT driven by the surge line UA. |
| **Physics Integrity Impact** | Medium ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Real plant physics: at steady state with no forced flow and heaters on, T_pzr saturates at Tsat(P) and T_rcs equilibrates at T_pzr minus the ÃƒÅ½Ã¢â‚¬ÂT driven by surge line UA. The simulator lacks this equilibrium ceiling because pressure rises continuously via thermal expansion (solid regime) or Psat(T_pzr) (two-phase regime), and T_rcs follows T_pzr through conduction. The heatup RATE may be unrealistic without equilibrium bounding. This is partially by-design for heatup (T_rcs is meant to rise during startup), but the absence of an equilibrium ceiling makes long-duration steady-state scenarios physically incorrect. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Equilibrium requires either a stronger ambient loss model or an active SG heat sink. Neither exists in Regime 0/1. SG heat sink is absent by design (CS-0014ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“CS-0020 SG domain). Insulation loss saturates below surge conduction magnitude at operating temperatures. |
| **Assigned Implementation Plan** | IP-0004 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â RCS Energy Balance and Regime Transition |
| **Validation Outcome** | Not Tested |
| **Related Issues** | Originated from: CS-0033 (Finding B). Related: CS-0014ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“CS-0020 (SG heat sink). Related: CS-0031 (RCP thermal inertia). |
| **Source Document** | CS-0033 Diagnostic Audit ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Finding B |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Primary Thermodynamics |
| **Assigned DP ID** | DP-0001 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Core Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Primary Energy Balance |

---

## CS-0035: CVCS Thermal Mixing Contribution Missing from RCS Energy Balance

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0035 |
| **Title** | CVCS thermal mixing contribution missing from RCS energy balance |
| **Severity** | Low |
| **Status** | Assigned |
| **Detected in Version** | v0.3.0.0 (CS-0033 diagnostic audit, Finding C) |
| **System Area** | HeatupSimEngine.CVCS / RCSHeatup / HeatTransfer |
| **Discipline** | CVCS ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Primary Energy Balance Boundary |
| **Operational Impact** | Charging water enters the RCS at VCT temperature (~100Ãƒâ€šÃ‚Â°F) but is modeled as mass-only with no thermal mixing. At 75 gpm charging with a ÃƒÅ½Ã¢â‚¬ÂT of ~300Ãƒâ€šÃ‚Â°F (RCS at ~400Ãƒâ€šÃ‚Â°F vs charging at ~100Ãƒâ€šÃ‚Â°F), this represents approximately 0.15 MW of cooling that is currently ignored. The omission contributes to a slight overestimation of RCS heatup rate during CVCS operations. |
| **Physics Integrity Impact** | Low ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Not critical for heatup accuracy (0.15 MW vs ~1.0 MW total heat input), but contributes to the cumulative energy balance gap. Becomes more significant at higher RCS temperatures where the charging ÃƒÅ½Ã¢â‚¬ÂT is larger. A complete energy balance should include CVCS enthalpy transport. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â CVCS flows are modeled as mass-only boundary flows (lbm in/out) per the canonical mass conservation architecture (v0.1.0.0). Enthalpy transport was not in scope for mass conservation. Charging temperature is available (VCT model tracks temperature), but the thermal contribution is not computed or applied to the RCS energy equation. |
| **Assigned Implementation Plan** | IP-0008 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â CVCS Energy Balance and VCT Flow Accounting |
| **Validation Outcome** | Not Tested |
| **Related Issues** | Originated from: CS-0033 (Finding C). Related: CS-0034 (equilibrium ceiling ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â CVCS cooling would contribute to equilibrium). |
| **Source Document** | CS-0033 Diagnostic Audit ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Finding C |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | CVCS / Inventory Control |
| **Assigned DP ID** | DP-0004 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | CVCS ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Primary Energy Balance Boundary |

---

## CS-0036: DRAIN Phase Duration Excessive ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â PZR Requires ~4 Hours to Reach 25%

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0036 |
| **Title** | DRAIN phase duration excessive ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â PZR requires ~4 hours to reach 25% level |
| **Severity** | High |
| **Status** | Assigned |
| **Detected in Version** | v0.3.1.1 (runtime observation during validation) |
| **System Area** | HeatupSimEngine.BubbleFormation / HeatupSimEngine.CVCS / CVCSController |
| **Discipline** | Bubble Formation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Phase Controller / CVCS Flow Balance |
| **Operational Impact** | DRAIN phase from bubble detection (~100% PZR level) to target level (25%) takes approximately 4 simulated hours. This distorts the startup schedule: in a real plant, PZR drain to normal operating level takes on the order of 30ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“60 minutes at typical letdown/drain rates. The excessive duration also blocks validation runtime practicality ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â 4-hour DRAIN consumes significant wall-clock time at any simulation acceleration, making Phase E validation runs impractical. |
| **Physics Integrity Impact** | High ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â If the net drain rate is significantly lower than modeled gpm would predict, this indicates either: (1) hidden inflow cancellation (charging/makeup opposing letdown during DRAIN), (2) unit conversion error (gpm vs gph, or seconds vs hours in flow integration), (3) valve coefficient or cap limiting effective drain flow below intended rate, (4) geometric-vs-mass mismatch in PZR level computation causing level to stall. Any of these would represent a mass flow or state computation error. |
| **Root Cause Status** | Under investigation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â requires diagnostic audit |
| **Diagnostic Requirements** | (1) From logs, compute implied net drain flow using ÃƒÅ½Ã¢â‚¬Â(PZR water mass)/ÃƒÅ½Ã¢â‚¬Ât and convert to gpm. Compare to commanded letdown/drain flows. (2) Audit opposing inflows during DRAIN (charging, makeup, seal injection/return) and confirm true net flow to/from PZR. (3) Verify drain path caps/clamps (max gpm, valve coefficients, ramp fractions) and confirm no unit conversion error (sec/hr, gpm/gph). (4) Validate PZR level % computation is consistent with mass/volume model and not stalled by geometric-vs-mass mismatch. |
| **Assigned Implementation Plan** | IP-0003 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Bubble Formation and Two-Phase |
| **Validation Outcome** | Not Tested |
| **Acceptance Criteria** | DRAIN-to-25% duration matches expected order-of-magnitude given modeled gpm and PZR inventory (no hidden cancellations, no unit scaling errors). |
| **Related Issues** | Related: CS-0026 (post-bubble pressure escalation), CS-0027 (bubble phase labeling). Related: CS-0035 (CVCS thermal mixing ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â CVCS flows during DRAIN). |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Pressurizer & Two-Phase Physics |
| **Assigned DP ID** | DP-0002 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Bubble Formation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Phase Controller / CVCS Flow Balance |

---

## CS-0037: Surge Line Flow Direction and Net Inventory Display Enhancement

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0037 |
| **Title** | Surge line flow direction and net inventory display enhancement |
| **Severity** | Low |
| **Status** | Assigned |
| **Detected in Version** | v0.3.1.1 (operator usability observation during validation) |
| **System Area** | HeatupValidationVisual / Operator UI |
| **Discipline** | Observability ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Operator/Visual Enhancement |
| **Operational Impact** | Surge flow display currently shows magnitude only, without explicit direction indicator. During DRAIN and PRESSURIZE phases, ambiguity exists about whether flow is PZRÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢RCS or RCSÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢PZR. Net CVCS flow and derived PZR mass change rate are not displayed, making it difficult to diagnose mass balance during transient phases. Operators must infer flow direction from other parameters. |
| **Physics Integrity Impact** | None ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Pure observability enhancement. No physics equations, conservation logic, or state computation changes. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â UI displays surge flow magnitude without sign convention or directional indicator. No derived net mass change display exists. |
| **Requirements** | (1) Surge flow display must explicitly indicate direction (PZRÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢RCS or RCSÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢PZR). (2) Visual indicator (arrow or color) must reflect sign. (3) Display derived net mass change of PZR and/or net CVCS flow to eliminate ambiguity during drain/pressurization phases. |
| **Assigned Implementation Plan** | IP-0013 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Operator and Scenario Framework (subsumed by CS-0042) |
| **Validation Outcome** | Not Tested |
| **Acceptance Criteria** | (1) Surge flow direction is unambiguous in UI during all bubble formation phases. (2) Net PZR mass change rate or net CVCS flow is displayed. (3) No physics changes ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â validation tab only. |
| **Related Issues** | Subsumed by: CS-0042 (dashboard modernization ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â directional flow indicators). Related: CS-0012 (regime transition logging). Related: CS-0036 (DRAIN duration ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â flow direction clarity aids diagnosis). Related: CS-0027 (bubble phase labeling). |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Operator Interface & Scenarios |
| **Assigned DP ID** | DP-0008 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Observability ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Operator/Visual Enhancement |

---

## CS-0038: PZR Level Spike on RCP Start (Single-Frame Transient)

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0038 |
| **Title** | PZR level spike on RCP start (single-frame transient) |
| **Severity** | High |
| **Status** | Assigned |
| **Detected in Version** | Pre-v0.1.0.0 (legacy 5.x lineage, roadmap technical debt) |
| **System Area** | CoupledThermo / PressurizerPhysics / HeatupSimEngine (regime transition) |
| **Discipline** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Regime Transition Stability |
| **Operational Impact** | Sharp upward transient in pressurizer level when RCPs start. Creates unrealistic plant behavior during startup sequence. Single-timestep PZR level jump exceeds 0.5% per step. |
| **Physics Integrity Impact** | High ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Likely causes: one-frame canonical mass overwrite during regime transition, density ordering issue (pre-solver density vs post-solver density mismatch), or thermal expansion double-counting at the regime boundary. The spike may propagate into conservation diagnostics as a false mass error. |
| **Root Cause Status** | Suspected ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Regime transition from Regime 1 (no RCPs) to Regime 2 (RCPs running) involves switching from IsolatedHeatingStep to BulkHeatupStep. State handoff between these paths may introduce a single-frame discontinuity in PZR volume partitioning or mass redistribution. |
| **Assigned Implementation Plan** | IP-0004 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â RCS Energy Balance and Regime Transition |
| **Validation Outcome** | Not Tested |
| **Acceptance Criteria** | (1) No PZR level spike > 0.5% per timestep on RCP start. (2) Mass conservation remains within tolerance through the regime transition. (3) No single-frame canonical mass overwrite at regime boundary. |
| **Related Issues** | Related: CS-0031 (RCP thermal inertia ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â affects heat rate post-RCP start, distinct from level spike). |
| **Source Document** | FUTURE_ENHANCEMENTS_ROADMAP.md ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â v5.4.0 Issue #4, Technical Debt |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Primary Thermodynamics |
| **Assigned DP ID** | DP-0001 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Primary Thermodynamics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Regime Transition Stability |

---

## CS-0039: VCT Conservation Error Growth (~1,700 gal at 15.75 hr)

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0039 |
| **Title** | VCT conservation error growth (~1,700 gal at 15.75 hr) |
| **Severity** | High |
| **Status** | Assigned |
| **Detected in Version** | Pre-v0.1.0.0 (legacy 5.x lineage, roadmap technical debt) |
| **System Area** | VCTPhysics / HeatupSimEngine.CVCS / CVCSController |
| **Discipline** | CVCS ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â VCT Flow Boundary Accounting |
| **Operational Impact** | Dashboard shows VCT CONS ERR ~1,600ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“1,700 gal (failing) at 15.75 hr simulation time. VCT internal flows may be correct, but the verifier depends on the RCS-side `rcsInventoryChange_gal` term. If primary-side accounting drifts or is overwritten, VCT verification fails even with correct VCT flows. Creates cascading validation failures that mask the true root cause. |
| **Physics Integrity Impact** | High ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â VCT conservation error growth indicates a boundary accounting mismatch between RCS and VCT subsystems. Either (1) VCT flow integration accumulates error over time, (2) the RCS-side inventory change term used in VCT verification drifts due to V*rho intermediate estimates, or (3) seal flow accounting (3 gpm/pump to VCT) creates a systematic offset. The error is monotonically growing, suggesting a rate error rather than a one-time offset. |
| **Root Cause Status** | Suspected ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Multiple hypotheses: (a) VCT verifier uses `rcsInventoryChange_gal` which is mass-derived volume ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â subject to thermal expansion artifacts (same class as CS-0041). (b) Seal return flow accounting may mismatch between RCS boundary and VCT boundary. (c) CVCS flow integration may have a timestep-dependent drift. |
| **Assigned Implementation Plan** | IP-0008 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â CVCS Energy Balance and VCT Flow Accounting |
| **Validation Outcome** | Not Tested |
| **Acceptance Criteria** | (1) VCT conservation error < 10 gal steady-state over multi-hour runs. (2) No monotonic error growth ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â error must be bounded. (3) VCT and RCS boundary flow accounting must be reconciled. |
| **Related Issues** | Related: CS-0035 (CVCS thermal mixing ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â enthalpy transport may affect VCT verification). Related: CS-0041 (inventory audit baseline type mismatch ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â may share root cause in mass-derived vs geometric volume comparison). |
| **Source Document** | FUTURE_ENHANCEMENTS_ROADMAP.md ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â v5.4.0 Issue #5, Technical Debt |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | CVCS / Inventory Control |
| **Assigned DP ID** | DP-0004 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | CVCS ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â VCT Flow Boundary Accounting |

---

## CS-0040: RVLIS Indicator Stale During PZR Drain

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0040 |
| **Title** | RVLIS indicator stale during PZR drain |
| **Severity** | High |
| **Status** | Assigned |
| **Detected in Version** | Pre-v0.1.0.0 (legacy 5.x lineage, roadmap technical debt) |
| **System Area** | HeatupSimEngine / SystemState (RVLIS calculation) |
| **Discipline** | Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Indicator Integrity |
| **Operational Impact** | `RCSWaterMass` is not updated during drain operations, causing the RVLIS (Reactor Vessel Level Indication System) indicator to read stale mass values (~88%). This creates a false indication of stable inventory when liquid is actively being displaced from the PZR into the RCS or removed via letdown. Operators see RVLIS holding steady while PZR level drops, which is misleading. |
| **Physics Integrity Impact** | High ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â RVLIS is a safety-grade level indicator in actual plants. Stale readings during drain represent a fidelity gap in operator-facing instrumentation. The stale value also affects any downstream computation that reads `RCSWaterMass` during the DRAIN phase, potentially including conservation diagnostics. |
| **Root Cause Status** | Suspected ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â During DRAIN phase, liquid mass moves from PZR (via steam displacement) and potentially via CVCS letdown. The `RCSWaterMass` field used for RVLIS may not be recomputed from the canonical mass ledger during the DRAIN phase, or the canonical ledger update may not propagate to RVLIS in the correct order. |
| **Assigned Implementation Plan** | IP-0003 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Bubble Formation and Two-Phase |
| **Validation Outcome** | Not Tested |
| **Acceptance Criteria** | (1) RVLIS indicator responds to mass redistribution during all bubble formation phases (DRAIN, STABILIZE, COMPLETE). (2) No RVLIS drop unless true boundary mass loss occurs. (3) `RCSWaterMass` is derived from canonical ledger at every timestep, not frozen. |
| **Related Issues** | Related: CS-0036 (DRAIN phase duration excessive ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â same phase, different symptom). Related: CS-0026 through CS-0030 (bubble formation and two-phase domain). |
| **Source Document** | FUTURE_ENHANCEMENTS_ROADMAP.md ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â v5.4.0 Issue #3, Technical Debt |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Validation & Diagnostics |
| **Assigned DP ID** | DP-0007 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Mass Conservation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Indicator Integrity |

---

## CS-0041: Inventory Audit Baseline Type Mismatch (Geometric vs Mass-Derived Gallons)

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0041 |
| **Title** | Inventory audit baseline type mismatch (geometric vs mass-derived gallons) |
| **Severity** | Medium |
| **Status** | Assigned |
| **Detected in Version** | Pre-v0.1.0.0 (legacy 5.x lineage, roadmap technical debt) |
| **System Area** | HeatupSimEngine / HeatupSimEngine.CVCS / HeatupValidationVisual.Panels |
| **Discipline** | Validation / Diagnostic Display ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Conservation Metric Integrity |
| **Operational Impact** | Dashboard "SYSTEM TOTAL" and "INV ERROR" displays compare `initialSystemInventory_gal` (geometric volume at t=0) against `totalSystemInventory_gal` (mass-derived volume at runtime). As water heats from 100Ãƒâ€šÃ‚Â°F to 550Ãƒâ€šÃ‚Â°F, density decreases ~15%, producing a phantom "inventory error" of ~13,000+ gallons that is purely thermal expansion ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â not an actual conservation failure. This is a display metric defect, not a physics defect. |
| **Physics Integrity Impact** | Medium ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â The mass conservation architecture (canonical ledger, `TotalPrimaryMass_lb`) is unaffected. However, the misleading display undermines operator confidence in conservation and produces false FAIL indications in the validation tab. The metric compares thermodynamically incompatible quantities (geometric baseline vs mass-derived runtime). |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â `initialSystemInventory_gal` is set at t=0 from `PlantConstants.RCS_WATER_VOLUME` (geometric, temperature-independent). `totalSystemInventory_gal` is computed at runtime from `RCSWaterMass / ÃƒÂÃ‚Â(T,P)` (mass-derived, temperature-dependent). The difference grows with temperature as density decreases. At 400Ãƒâ€šÃ‚Â°F: phantom error ÃƒÂ¢Ã¢â‚¬Â°Ã‹â€  13,474 gallons. |
| **Proposed Fix** | Make inventory validation canonical in MASS (lbm): (1) Store `initialSystemMass_lbm` at init. (2) Compute `totalSystemMass_lbm` at runtime from component sum. (3) Compute mass error. (4) Optionally display equivalent volume at reference density. (5) Keep geometric gallons only for capacity/level displays, never for conservation checks. |
| **Assigned Implementation Plan** | IP-0011 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Validation and Diagnostic Display |
| **Validation Outcome** | Not Tested |
| **Acceptance Criteria** | (1) During heatup with density changes, dashboard inventory error remains bounded (~ÃƒÂ¢Ã¢â‚¬Â°Ã‚Â¤1 lbm). (2) No large gallon swings from thermal expansion appear as errors. (3) Conservation metric uses canonical mass ledger. (4) No false FAIL indications in validation tab due to thermal expansion. (5) Inventory Audit status matches dashboard display. |
| **Related Issues** | Related: CS-0007 (primary ledger drift UI ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â same observability domain). Related: CS-0039 (VCT conservation error ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â may share root cause in mass-derived vs geometric volume comparison). |
| **Source Document** | FUTURE_ENHANCEMENTS_ROADMAP.md ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â v5.4.1 Part B, Technical Debt |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Validation & Diagnostics |
| **Assigned DP ID** | DP-0007 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Validation / Diagnostic Display ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Conservation Metric Integrity |

---

## CS-0042: Professional Interactive Dashboard Visualization Upgrade (uGUI Modernization)

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0042 |
| **Title** | Professional interactive dashboard visualization upgrade (uGUI modernization) |
| **Severity** | Medium |
| **Status** | Assigned |
| **Detected in Version** | v0.3.1.1 (operator usability assessment) |
| **System Area** | HeatupValidationVisual / Operator UI / Telemetry Layer |
| **Discipline** | Operator & Scenario Framework ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Dashboard Visualization |
| **Phase** | Phase 1 (BLOCKED until Phase 0 exit criteria satisfied) |
| **Release** | To be assigned upon Phase 1 scheduling (aligned with v5.5.0.0 roadmap entry) |
| **Operational Impact** | The current dashboard exposes correct telemetry but lacks professional-grade instrumentation, animated transitions, directional clarity, and interactive visualization. Heavy reliance on static numeric text increases cognitive load and slows debugging. Operators must mentally reconstruct flow directions, trend behavior, and alarm states from raw numbers rather than visual cues. |
| **Physics Integrity Impact** | None ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Pure UI/visualization enhancement. No physics equations, conservation logic, state computation, or regime logic changes. All data sourced exclusively from telemetry snapshot layer. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Dashboard was built incrementally as a validation tool (OnGUI text-based). It was never designed as a professional instrumentation interface. The telemetry data is correct; the presentation layer does not exploit it effectively. |
| **Scope** | (1) Arc gauges for pressure, temperature, % power, and similar analog parameters. (2) Strip gauges for level, flow, and heater output. (3) Animated needle/fill transitions with smooth interpolation (no jump-cuts unless alarm trip demands immediate visual snap). (4) Rolling strip charts using buffered telemetry history (ring buffer per channel, default 120s retention). (5) Explicit flow direction indicators (e.g., PZRÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢RCS, RCSÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢PZR) with directional arrows/color. (6) Standardized alarm escalation color semantics: Normal ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ Warn ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ Alarm ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ Trip, each with defined color, border, and animation behavior. (7) Interactive detail panels/tooltips for secondary parameter inspection. (8) Telemetry-only data sourcing ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â no UI component may read physics state objects directly. |
| **Non-Goals** | (1) No physics changes. (2) No regime logic modifications. (3) No architecture refactor (beyond telemetry decoupling if not already present). (4) No performance overhaul (performance is an acceptance criterion, not a separate scope item). |
| **Assigned Implementation Plan** | IP-0013 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Operator and Scenario Framework |
| **Validation Outcome** | Not Tested |
| **Acceptance Criteria** | (1) 60 FPS sustained at 10ÃƒÆ’Ã¢â‚¬â€ sim speed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â no per-frame heap allocations in the render path, chart data points use object pooling, UI update cost under 4 ms/frame. (2) All UI data sourced exclusively from telemetry snapshot ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â no direct physics reads from any UI component. (3) Clear directional visualization of all mass/flow transfers (surge line, CVCS charging/letdown, RHR, seal flow). (4) Validation and conservation metrics visually aligned with canonical mass ledger (`TotalPrimaryMass_lb`). (5) Zero regression in existing validation tests. (6) Animated state transitions for all gauges, indicators, and fill levels (smooth interpolated motion). (7) Clear visual hierarchy ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â no "text wall" panels; at-a-glance visuals (gauges, bars, sparklines) prioritized over dense numeric tables. |
| **Related Issues** | Subsumes: CS-0037 (surge flow direction display ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â directional indicators are a subset of this scope). Related: CS-0041 (inventory audit baseline type mismatch ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â conservation display alignment). Related: CS-0007 (primary ledger drift UI). Related: CS-0012 (regime transition logging ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â observability domain overlap). |
| **Source Document** | FUTURE_ENHANCEMENTS_ROADMAP.md ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â v5.5.0.0 Validation Dashboard Redesign |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Operator Interface & Scenarios |
| **Assigned DP ID** | DP-0008 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Operator & Scenario Framework ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Dashboard Visualization |

## CS-0043: Pressurizer Pressure Boundary Failure During Bubble Formation (Runaway Depressurization)

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0043 |
| **Title** | Pressurizer pressure boundary failure during bubble formation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â runaway depressurization spiral |
| **Severity** | Critical |
| **Status** | Assigned |
| **Detected in Version** | v0.3.1.1 (Stage E long-run validation) |
| **Fixed In Version** | v0.3.2.0 (IP-0014) |
| **System Area** | RCSHeatup.IsolatedHeatingStep, HeatupSimEngine (Regime 1 pressure override), HeatupSimEngine.BubbleFormation (DRAIN phase) |
| **Discipline** | Core Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Two-Phase Pressurizer Energy Partition |
| **Phase** | Phase 0 (critical path ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â blocks Phase E exit and all downstream validation) |
| **Operational Impact** | Pressure collapses from ~368 psia to ~154 psia over ~2 hours during DRAIN phase despite 1.8 MW heaters at full power. Rate of depressurization accelerates: -78 psi/hr at DETECTION, -383 psi/hr during DRAIN. In a real PWR, heaters maintain saturation pressure during bubble formation; pressure should be stable or rising. The DRAIN phase cannot complete correctly under these conditions. |
| **Physics Integrity Impact** | Critical ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Dual energy application violates first law of thermodynamics. Heater power is consumed twice: (1) as sensible heat temperature rise in `IsolatedHeatingStep` (capped and discarded by T_sat clamp), and (2) as latent heat steam generation in `UpdateDrainPhase` via h_fg. The T_sat cap creates a ratchet effect where P_new = Psat(T_pzr) ÃƒÂ¢Ã¢â‚¬Â°Ã‚Â¤ Psat(T_sat(P_old)) ÃƒÂ¢Ã¢â‚¬Â°Ã‚Â¤ P_old, guaranteeing monotonic pressure decline every timestep. |
| **Root Cause Status** | Confirmed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Runaway depressurization feedback loop with four interacting mechanisms: (A) `IsolatedHeatingStep` applies heater energy as subcooled sensible heat dT = Q/(mCp), then caps T_pzr at T_sat(P_old); (B) Two-phase pressure override sets P_new = Psat(T_pzr) where T_pzr ÃƒÂ¢Ã¢â‚¬Â°Ã‚Â¤ T_sat(P_old), so P_new ÃƒÂ¢Ã¢â‚¬Â°Ã‚Â¤ P_old; (C) Next timestep: T_sat(P_new) < T_sat(P_old), cap ratchets lower; (D) `UpdateDrainPhase` independently generates steam from the same heater power via h_fg ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â double-counting the energy. Fundamental error: in two-phase conditions, heater energy should go entirely to latent heat (phase change at constant temperature/pressure), not sensible heat (temperature rise). The `IsolatedHeatingStep` subcooled liquid model is incorrect for two-phase PZR operation. |
| **Scope** | Replace Regime 1 PZR energy model: when two-phase (steam volume > 0), heater energy must drive steam generation via h_fg at constant T_sat(P), not subcooled dT rise. Eliminate T_sat cap ratchet. Ensure single energy application path. Pressure should be set by steam/water mass balance, not by Psat(T_pzr) override on a clamped temperature. |
| **Assigned Implementation Plan** | IP-0003 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Bubble Formation and Two-Phase |
| **Validation Outcome** | Stage E FAILED ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Pressure boundary collapse confirmed in interval logs at 8.00ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“10.00 hr sim time |
| **Acceptance Criteria** | (1) During DRAIN phase with heaters at full power, pressure must be stable or rising (dP/dt ÃƒÂ¢Ã¢â‚¬Â°Ã‚Â¥ 0). (2) No monotonic pressure decline exceeding 5 psi/hr under full heater power. (3) Heater energy applied exactly once per timestep ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â either as sensible heat (subcooled) or latent heat (two-phase), never both. (4) T_pzr remains at or near T_sat(P) during two-phase operation. (5) DRAIN phase completes within expected duration (~1.5ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“2 hr per plant procedure, not 4+ hr). (6) Mass conservation maintained through bubble formation phases. |
| **Related Issues** | CS-0036 (DRAIN duration excessive ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â likely same root cause), CS-0026 (post-bubble pressure escalation), CS-0034 (no equilibrium ceiling Regime 0/1 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â related energy model), CS-0029 (high pressure ramp zero heat rate ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â related pressure model) |
| **Investigation Report** | Updates/Issues/CS-0043_Investigation_Report.md |
| **Authorization Status** | AUTHORIZED ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Implemented v0.3.2.0 |
| **Mode** | IMPLEMENTED (Awaiting Validation) |
| **Investigation Report ID(s)** | Updates/Issues/CS-0043_Investigation_Report.md |
| **Domain (Canonical)** | Pressurizer & Two-Phase Physics |
| **Assigned DP ID** | DP-0002 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Core Physics ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Two-Phase Pressurizer Energy Partition |

## CS-0044: Async Log Writer (Background Thread File I/O)

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0044 |
| **Title** | Async log writer ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â move synchronous file I/O to background thread |
| **Severity** | Low |
| **Status** | Assigned |
| **Detected in Version** | v0.3.1.0 (Performance investigation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Phase B design) |
| **System Area** | HeatupSimEngine.Logging, File I/O |
| **Discipline** | Performance / Runtime Architecture |
| **Operational Impact** | Synchronous `StreamWriter.Write` calls on the main thread can stall for milliseconds during disk flush, antivirus scan, or dense logging periods (e.g., DRAIN phase forensic logging). This manifests as intermittent frame time spikes rather than sustained unresponsiveness. |
| **Physics Integrity Impact** | None ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â logging only, no physics changes. |
| **Root Cause Status** | Identified ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Performance plan Phase B design. Synchronous file I/O on main thread. |
| **Scope** | Replace direct `StreamWriter.Write` with `ConcurrentQueue<string>` + background consumer thread. Consolidate multiple `Debug.Log` calls per timestep into single `StringBuilder` batch. |
| **Assigned Implementation Plan** | IP-0009 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Performance and Runtime Architecture (Phase B ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Deferred) |
| **Validation Outcome** | Not Tested |
| **Acceptance Criteria** | (1) Zero main-thread file I/O bytes per frame. (2) Zero log data loss on clean shutdown (queue fully drained). (3) Debug.Log calls per timestep ÃƒÂ¢Ã¢â‚¬Â°Ã‚Â¤ 3 (consolidated). (4) Frame time during DRAIN phase < 40ms. |
| **Related Issues** | CS-0032 (resolved ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â parent performance issue), CS-0045 (snapshot boundary), CS-0046 (parallelization) |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Performance & Runtime |
| **Assigned DP ID** | DP-0009 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Performance / Runtime Architecture |

## CS-0045: Physics Snapshot Boundary (Compute-Render Decoupling)

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0045 |
| **Title** | Physics snapshot boundary ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â decouple physics compute from UI rendering |
| **Severity** | Low |
| **Status** | Assigned |
| **Detected in Version** | v0.3.1.0 (Performance investigation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Phase C design) |
| **System Area** | HeatupSimEngine, HeatupValidationVisual (all partials) |
| **Discipline** | Performance / Runtime Architecture |
| **Operational Impact** | Currently, physics writes to fields that UI reads on the same frame. This creates implicit coupling where UI must run after physics, and any field access is a potential race condition if physics is later moved off-thread. |
| **Physics Integrity Impact** | None ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â snapshot values are identical to direct reads. Behavioral change: zero. |
| **Root Cause Status** | Identified ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Performance plan Phase C design. Implicit coupling between physics output and UI input. |
| **Scope** | Add `SimSnapshot` struct captured after `StepSimulation()`. All UI partials read from snapshot instead of engine fields directly. Prerequisite for Phase D (parallelization). |
| **Assigned Implementation Plan** | IP-0009 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Performance and Runtime Architecture (Phase C ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Deferred) |
| **Validation Outcome** | Not Tested |
| **Acceptance Criteria** | (1) Zero direct field reads from UI to engine (all via snapshot). (2) Snapshot capture time < 0.1ms. (3) No behavioral change ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â snapshot values identical to direct reads. |
| **Related Issues** | CS-0032 (resolved ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â parent performance issue), CS-0044 (async log writer ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â prerequisite), CS-0046 (parallelization ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â depends on this) |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Performance & Runtime |
| **Assigned DP ID** | DP-0009 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Performance / Runtime Architecture |

## CS-0046: Physics Parallelization (Off-Thread Simulation Step)

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0046 |
| **Title** | Physics parallelization ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â move simulation step to worker thread or Unity Jobs |
| **Severity** | Low |
| **Status** | Assigned |
| **Detected in Version** | v0.3.1.0 (Performance investigation ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Phase D design) |
| **System Area** | HeatupSimEngine, all physics modules |
| **Discipline** | Performance / Runtime Architecture |
| **Operational Impact** | With physics on the main thread, simulation compute competes with UI rendering for frame budget. Moving physics off-thread would free main thread for input/rendering and enable higher simulation speeds. |
| **Physics Integrity Impact** | Must be bit-identical to single-threaded results. Requires Phase C snapshot boundary (CS-0045) as prerequisite. |
| **Root Cause Status** | Identified ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Performance plan Phase D design. Speculative ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â may not be needed if Phases A-C are sufficient. |
| **Scope** | Two options: (A) Unity Jobs + Burst ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â requires `[BurstCompile]` compatible code, no managed allocations, no Unity API calls in physics. (B) Simple worker thread with double-buffered snapshot. Both require CS-0044 (async logging) and CS-0045 (snapshot boundary) as prerequisites. |
| **Assigned Implementation Plan** | IP-0009 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Performance and Runtime Architecture (Phase D ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Deferred) |
| **Validation Outcome** | Not Tested |
| **Acceptance Criteria** | (1) Main thread compute budget for physics < 2ms/frame. (2) Input responsiveness under full load < 100ms. (3) Frame time < 20ms (50 FPS). (4) CPU utilization distributed across ÃƒÂ¢Ã¢â‚¬Â°Ã‚Â¥ 2 cores. (5) Physics correctness bit-identical to single-threaded results. |
| **Related Issues** | CS-0032 (resolved ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â parent performance issue), CS-0044 (async log writer ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â prerequisite), CS-0045 (snapshot boundary ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â prerequisite) |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** |  |
| **Domain (Canonical)** | Performance & Runtime |
| **Assigned DP ID** | DP-0009 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Performance / Runtime Architecture |






---

## CS-0047: Heat-up Progression Stalls During Intended Startup Heat Addition

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0047 |
| **Title** | Heat-up progression stalls during intended startup heat addition |
| **Severity** | Critical |
| **Status** | Implemented Ã¯Â¿Â½ Pending Validation |
| **Detected in Version** | Observed in validated heat-up evidence set (2026-02-14 governance intake) |
| **Objective Behavior Description** | During intended heat-up, RCS temperature progression stalls while heat sources remain active. Net plant heat addition trends to zero or negative, blocking startup progression. |
| **Evidence Reference** | Technical_Documentation/SG_Secondary_Pressurization_During_Heatup_Research.md:13; Updates/Archive/IMPLEMENTATION_PLAN_v5.1.0.md:37-38 |
| **Simulation Phase Detected** | Startup heat-up after SG boiling onset (Mode 4 heat-up progression window) |
| **Systems Involved** | RCSHeatup, SGMultiNodeThermal, SG secondary pressure/boiling boundary, HeatupSimEngine heat balance telemetry |
| **Governing Physical Law / Rule / Logic** | First-law plant heat balance (`Q_net = Q_sources - Q_sinks`) and startup heat-up control intent |
| **Expected vs Simulated** | Expected: positive net heat and rising RCS temperature through startup heat-up. Simulated: net heat collapses to near-zero/negative with heat-up rate collapse/cooldown signatures. |
| **Boundary / Control State Comparison** | Heat sources active (RCP/heater input), but SG-side removal exceeds available source heat in evidence set. |
| **Root Cause Status** | Suspected - preliminary investigation indicates startup heat-up stall is downstream of SG secondary open-boundary and sink-dominance chain. |
| **Evidence Reference(s)** | Technical_Documentation/SG_Secondary_Pressurization_During_Heatup_Research.md:13; Updates/Archive/IMPLEMENTATION_PLAN_v5.1.0.md:37-38; Assets/Scripts/Physics/RCSHeatup.cs:131-136; Updates/Issues/DP-0003_Preliminary_Investigation_Report.md |
| **Probable Cause Domain Statement** | Evidence indicates origin within Steam Generator Secondary Physics. |
| **Severity Rationale** | Critical: blocks heat-up/startup progression; evidence includes RCS cooldown while heat sources are active. |
| **Assigned Implementation Plan** | IP-0015 - DP-0003 SG Secondary Physics Stabilization and Heat-up Progression Recovery |
| **Authorization Status** | AUTHORIZED (Implementation Phase) |
| **Mode** | IMPLEMENTED/PENDING_VALIDATION |
| **Investigation Report ID(s)** | Updates/Issues/CS-0047_Investigation_Report.md; Updates/Issues/DP-0003_Preliminary_Investigation_Report.md |
| **Domain (Canonical)** | Steam Generator Secondary Physics |
| **Assigned DP ID** | DP-0003 |
| **Assigned IP ID** | IP-0015 |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | Blocked by: CS-0048. Related contributor: CS-0016. |
| **Subdomain (Freeform)** | Heat-up Progression - Net Plant Heat Balance |

---

## CS-0048: Steam Generator Secondary Stays Near Atmospheric and Behaves as Constant Heat Sink

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0048 |
| **Title** | Steam generator secondary stays near atmospheric and behaves as constant heat sink during heat-up |
| **Severity** | Critical |
| **Status** | Implemented Ã¯Â¿Â½ Pending Validation |
| **Detected in Version** | Observed in validated heat-up evidence set (2026-02-14 governance intake) |
| **Objective Behavior Description** | SG secondary pressure remains near atmospheric during active heat addition; SG boiling removes nearly all primary heat input and prevents SG pressure/temperature rise needed for startup. |
| **Evidence Reference** | Technical_Documentation/SG_Secondary_Pressurization_During_Heatup_Research.md:13; HeatupLogs/Heatup_Interval_001_0.00hr.txt:155-160 |
| **Simulation Phase Detected** | Startup heat-up with SG boiling active |
| **Systems Involved** | SGMultiNodeThermal, SG secondary pressure state, SG boiling heat-transfer path, RCS-SG coupling |
| **Governing Physical Law / Rule / Logic** | Saturation pressure-temperature coupling and bounded SG heat sink behavior during startup |
| **Expected vs Simulated** | Expected: SG pressure rises with steam production and SG saturation temperature follows pressure. Simulated: SG pressure remains near atmospheric boundary while boiling heat removal dominates. |
| **Boundary / Control State Comparison** | SG evidence includes atmospheric/N2 pressure floor behavior and OPEN-state sink behavior during boiling intervals. |
| **Root Cause Status** | Suspected - preliminary investigation indicates SG pressure path and boundary-state behavior remain near-atmospheric/open in startup boiling window. |
| **Evidence Reference(s)** | Technical_Documentation/SG_Secondary_Pressurization_During_Heatup_Research.md:13; HeatupLogs/Heatup_Interval_001_0.00hr.txt:155-160; Assets/Scripts/Physics/SGMultiNodeThermal.cs:765,1418-1428,1896-1908; Updates/Issues/DP-0003_Preliminary_Investigation_Report.md |
| **Probable Cause Domain Statement** | Evidence indicates origin within Steam Generator Secondary Physics. |
| **Severity Rationale** | Critical: creates physically impossible startup trajectory and directly blocks heat-up progression. |
| **Assigned Implementation Plan** | IP-0015 - DP-0003 SG Secondary Physics Stabilization and Heat-up Progression Recovery |
| **Authorization Status** | AUTHORIZED (Implementation Phase) |
| **Mode** | IMPLEMENTED/PENDING_VALIDATION |
| **Investigation Report ID(s)** | Updates/Issues/CS-0048_Investigation_Report.md; Updates/Issues/DP-0003_Preliminary_Investigation_Report.md |
| **Domain (Canonical)** | Steam Generator Secondary Physics |
| **Assigned DP ID** | DP-0003 |
| **Assigned IP ID** | IP-0015 |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | Blocked by: CS-0014, CS-0015, CS-0018. Related contributor: CS-0016. Precedes: CS-0047. |
| **Subdomain (Freeform)** | SG Secondary Pressure Boundary and Heat Sink Behavior |

---

## CS-0049: Pressurizer Does Not Recover Pressure in Two-Phase Condition Under Heater Pressurize Mode

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0049 |
| **Title** | Pressurizer does not recover pressure in two-phase condition under heater pressurize mode |
| **Severity** | Critical |
| **Status** | Assigned |
| **Detected in Version** | Observed in validated two-phase bubble-formation evidence set (2026-02-14 governance intake) |
| **Objective Behavior Description** | In two-phase operation, pressurizer temperature remains at saturation and pressure fails to increase despite heater pressurize operation; pressure may continue decreasing instead of stabilizing or rising. |
| **Evidence Reference** | Updates/Issues/CS-0043_Investigation_Report.md:18,107-112 |
| **Simulation Phase Detected** | Bubble formation two-phase DRAIN/STABILIZE window |
| **Systems Involved** | RCSHeatup.IsolatedHeatingStep, HeatupSimEngine.BubbleFormation, PressurizerPhysics two-phase pressure path |
| **Governing Physical Law / Rule / Logic** | Two-phase pressurizer saturation behavior and pressure control response under active heater input |
| **Expected vs Simulated** | Expected: pressure stabilizes or rises when heaters add energy in two-phase pressurize mode. Simulated: monotonic pressure decline during DRAIN despite heater power. |
| **Boundary / Control State Comparison** | Evidence table shows BUBBLE_AUTO with active heater input and continuing pressure decline across intervals. |
| **Root Cause Status** | Not asserted at registration. See preliminary report for domain localization only. |
| **Probable Cause Domain Statement** | Evidence indicates origin within Pressurizer & Two-Phase Physics. |
| **Severity Rationale** | Critical: physically impossible pressurizer response and direct startup progression blocker. |
| **Assigned Implementation Plan** | DP-0002 - Pressurizer & Two-Phase Physics |
| **Authorization Status** | NOT AUTHORIZED |
| **Mode** | SPEC/DRAFT |
| **Investigation Report ID(s)** | Updates/Issues/CS-0049_Investigation_Report.md |
| **Domain (Canonical)** | Pressurizer & Two-Phase Physics |
| **Assigned DP ID** | DP-0002 |
| **Assigned IP ID** |  |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Two-Phase Pressurizer Pressure Recovery |

---

## CS-0050: Persistent Plant-Wide Mass Conservation Imbalance (~10,000 gal class) Across Multiple Intervals

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0050 |
| **Title** | Persistent plant-wide mass conservation imbalance (~10,000 gal class) across multiple intervals |
| **Severity** | Critical |
| **Status** | CLOSED |
| **Closed Date** | 2026-02-14 |
| **Closed Under** | IP-0016 |
| **Closure Evidence** | Updates/Issues/IP-0016_StageE_Validation_2026-02-14_141059.md; HeatupLogs/Heatup_Report_20260214_141059.txt; HeatupLogs/Heatup_Interval_034_8.25hr.txt; HeatupLogs/Heatup_Interval_035_8.50hr.txt; HeatupLogs/Heatup_Interval_036_8.75hr.txt; HeatupLogs/Unity_StageE_IP0016_final.log |
| **Detected in Version** | Observed in validated inventory evidence set and reconfirmed during IP-0015/IP-0016 Stage E reruns (2026-02-14); corrected in IP-0016 rerun 2026-02-14 14:10:59 |
| **Objective Behavior Description** | Long-duration cumulative plant inventory imbalance remains on the order of 10,000 gallons class over multiple intervals rather than converging toward zero. |
| **Evidence Reference** | Documentation/PWR_Heatup_Simulation_Analysis_Report.md:116,119-120,171; Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_123200.md; Updates/Issues/IP-0016_StageE_Validation_2026-02-14_133520.md; Updates/Issues/IP-0016_StageE_Validation_2026-02-14_141059.md; HeatupLogs/Heatup_Report_20260214_141059.txt |
| **Simulation Phase Detected** | Long-duration startup heat-up inventory tracking window (including post-IP-0015 Stage E 18.00 hr rerun) |
| **Systems Involved** | Plant inventory audit pipeline, VCTPhysics conservation verifier, HeatupSimEngine inventory accounting and mass error telemetry |
| **Governing Physical Law / Rule / Logic** | Plant-wide mass conservation closure across inventory ledgers and boundary accounting |
| **Expected vs Simulated** | Expected: cumulative boundary error remains bounded near zero (threshold-scale). Simulated (latest): conservation error bounded at 14.6 lbm with interval onset window 8.25/8.50/8.75 hr all in threshold. |
| **Boundary / Control State Comparison** | Latest rerun preserves SG startup behavior and closes conservation drift simultaneously, indicating boundary-accounting correction was effective without SG-side regression. |
| **Root Cause Status** | Confirmed and corrected in IP-0016 via Primary Boundary Ownership Contract (PBOC) single-authority compute/apply order and paired ledger/component boundary application. |
| **Validation Outcome** | Stage E PASS for conservation gate (2026-02-14 14:10:59): final conservation error 14.6 lbm (0.002%), prior onset window at 8.50 hr now passes. |
| **Probable Cause Domain Statement** | Evidence indicates origin within Mass & Energy Conservation (conservation audit integrity), not CVCS control-path logic alone. |
| **Severity Rationale** | Critical: conservation-law failure with persistent large cumulative error. |
| **Assigned Implementation Plan** | IP-0016 - DP-0005 Mass & Energy Conservation |
| **Authorization Status** | AUTHORIZED (Implementation Phase - IP-0016) |
| **Mode** | IMPLEMENTATION/VALIDATION |
| **Investigation Report ID(s)** | Updates/Issues/CS-0050_Investigation_Report.md |
| **Domain (Canonical)** | Mass & Energy Conservation |
| **Assigned DP ID** | DP-0005 |
| **Assigned IP ID** | IP-0016 |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** |  |
| **Subdomain (Freeform)** | Plant-Wide Conservation Audit Integrity |
| **Routing Note** | Previous domain categorization was incorrect (was CVCS). Reclassified on 2026-02-14. |

---

## CS-0051: Stage E Mass Conservation Discontinuity at 8.25 hr during Solid->Two-Phase Handoff

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0051 |
| **Title** | Stage E mass conservation discontinuity at 8.25 hr during solid->two-phase handoff |
| **Severity** | Critical |
| **Status** | CLOSED |
| **Closed Date** | 2026-02-14 |
| **Closed Under** | IP-0016 |
| **Closure Evidence** | Updates/Issues/IP-0016_StageE_Validation_2026-02-14_141059.md; HeatupLogs/Heatup_Report_20260214_141059.txt; HeatupLogs/Heatup_Interval_034_8.25hr.txt; HeatupLogs/Heatup_Interval_035_8.50hr.txt; HeatupLogs/Heatup_Interval_036_8.75hr.txt; HeatupLogs/Unity_StageE_IP0016_final.log |
| **Date Discovered** | 2026-02-14 |
| **Detected in Version** | Stage E rerun evidence set after IP-0015; reconfirmed and corrected during IP-0016 validation (2026-02-14) |
| **Objective Behavior Description** | Legacy behavior: one-step conservation jump at 8.25 hr during solid->two-phase handoff. IP-0016 RTCC now reconciles handoff mass and enforces assert at the transition boundary. |
| **Evidence Reference** | Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_123200.md:21,24-30; Updates/Issues/IP-0016_StageE_Validation_2026-02-14_133520.md; Updates/Issues/IP-0016_StageE_Validation_2026-02-14_141059.md; HeatupLogs/Heatup_Report_20260214_141059.txt; HeatupLogs/Heatup_Interval_033_8.00hr.txt; HeatupLogs/Heatup_Interval_034_8.25hr.txt; HeatupLogs/Heatup_Interval_035_8.50hr.txt; HeatupLogs/Heatup_Interval_036_8.75hr.txt; Updates/Issues/CS-0051_Investigation_Report.md |
| **Simulation Phase Detected** | Stage E startup run at solid->two-phase handoff (bubble DETECTION) |
| **Systems Involved** | HeatupSimEngine bubble transition authority, inventory audit mass-source switching, canonical mass ledger continuity |
| **Governing Physical Law / Rule / Logic** | Plant-wide mass conservation and authority continuity across regime transition |
| **Expected vs Simulated** | Expected: no step discontinuity; handoff preserves total tracked mass. Simulated (IP-0016): transition assert delta = 0.000 lbm at 8.25 hr (PASS); later long-run divergence persists under CS-0050/CS-0052. |
| **Boundary / Control State Comparison** | At onset tick, authority handoff is explicit (CANONICAL_SOLID -> CANONICAL_TWO_PHASE) with RTCC reconciliation and post-handoff assert. Transition-adjacent interval conservation remains in-threshold at 8.25 hr. |
| **Root Cause Status** | Confirmed and corrected for transition boundary class via RTCC reconciliation + assertion infrastructure in IP-0016. |
| **Validation Outcome** | Transition discontinuity class resolved at 8.25 hr and remains stable in rerun; downstream conservation gates now pass under CS-0050/CS-0052 corrections. |
| **Probable Cause Domain Statement** | Evidence indicates origin within Mass & Energy Conservation (regime-transition mass authority continuity). |
| **Severity Rationale** | Critical: conservation-law violation with instantaneous multi-klbm mass discontinuity. |
| **Assigned Implementation Plan** | IP-0016 - DP-0005 Mass & Energy Conservation |
| **Authorization Status** | AUTHORIZED (Implementation Phase - IP-0016) |
| **Mode** | IMPLEMENTATION/VALIDATION |
| **Investigation Report ID(s)** | Updates/Issues/CS-0051_Investigation_Report.md |
| **Domain (Canonical)** | Mass & Energy Conservation |
| **Assigned DP ID** | DP-0005 |
| **Assigned IP ID** | IP-0016 |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | Related: CS-0050 |
| **Subdomain (Freeform)** | Regime-Transition Mass Authority Continuity |

---

## CS-0052: Post-RTCC Residual Long-Run Conservation Divergence After 8.50 hr

| Field | Value |
|-------|-------|
| **Issue ID** | CS-0052 |
| **Title** | Post-RTCC residual long-run conservation divergence after 8.50 hr |
| **Severity** | Critical |
| **Status** | CLOSED |
| **Closed Date** | 2026-02-14 |
| **Closed Under** | IP-0016 |
| **Closure Evidence** | Updates/Issues/IP-0016_StageE_Validation_2026-02-14_141059.md; HeatupLogs/Heatup_Report_20260214_141059.txt; HeatupLogs/Heatup_Interval_034_8.25hr.txt; HeatupLogs/Heatup_Interval_035_8.50hr.txt; HeatupLogs/Heatup_Interval_036_8.75hr.txt; HeatupLogs/Unity_StageE_IP0016_final.log |
| **Date Discovered** | 2026-02-14 |
| **Detected in Version** | IP-0016 Stage E validation run (2026-02-14 13:35:20), corrected in rerun 2026-02-14 14:10:59 |
| **Objective Behavior Description** | Historical behavior: after RTCC transition pass at 8.25 hr, conservation error exited threshold by 8.50 hr. Current IP-0016 rerun confirms onset interval now remains in threshold and long-run divergence class is suppressed. |
| **Evidence Reference** | Updates/Issues/IP-0016_StageE_Validation_2026-02-14_133520.md; Updates/Issues/IP-0016_StageE_Validation_2026-02-14_141059.md; HeatupLogs/Heatup_Interval_035_8.50hr.txt; HeatupLogs/Heatup_Interval_036_8.75hr.txt; HeatupLogs/Heatup_Report_20260214_141059.txt; HeatupLogs/Unity_StageE_IP0016_final.log |
| **Simulation Phase Detected** | Post-transition two-phase startup window (8.50 hr onward, through long-run Stage E) |
| **Systems Involved** | HeatupSimEngine.CVCS boundary accounting, VCTPhysics conservation verifier, inventory audit mass bucket closure |
| **Governing Physical Law / Rule / Logic** | Plant-wide mass conservation closure across all tracked inventories and boundary transfers |
| **Expected vs Simulated** | Expected: interval conservation remains <=500 lbm and <=0.05% after handoff. Simulated (latest): 8.50 hr interval now 92.3 lbm (0.010%); peak observed mass error in rerun evidence 92.50 lbm. |
| **Boundary / Control State Comparison** | Transition RTCC assert remains pass and downstream interval/step conservation remains bounded, confirming residual non-transition accounting defect class is addressed. |
| **Root Cause Status** | Confirmed and corrected by PBOC ownership/order enforcement: single primary-boundary event drives both component and ledger application each tick. |
| **Validation Outcome** | Stage E PASS for conservation gates (2026-02-14 14:10:59); first-fail onset at 8.50 hr no longer fails. |
| **Probable Cause Domain Statement** | Mass & Energy Conservation (plant-wide accounting integrity) |
| **Severity Rationale** | Critical issue class by impact; now corrected and pending formal closure workflow. |
| **Assigned Implementation Plan** | IP-0016 - DP-0005 Mass & Energy Conservation |
| **Authorization Status** | AUTHORIZED (Failure follow-up logging per IP-0016 gate) |
| **Mode** | IMPLEMENTATION/VALIDATION |
| **Investigation Report ID(s)** | Updates/Issues/CS-0052_Investigation_Report.md; Updates/Issues/IP-0016_StageE_Validation_2026-02-14_133520.md; Updates/Issues/IP-0016_StageE_Validation_2026-02-14_141059.md |
| **Domain (Canonical)** | Mass & Energy Conservation |
| **Assigned DP ID** | DP-0005 |
| **Assigned IP ID** | IP-0016 |
| **Deferral/Supersession Reason** |  |
| **Blocking Dependency** | Related: CS-0050, CS-0051 |
| **Subdomain (Freeform)** | Post-Transition Plant-Wide Conservation Divergence |




