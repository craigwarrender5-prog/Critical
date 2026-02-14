---
IP ID: IP-0016
DP ID: DP-0005
Title: Mass & Energy Conservation Domain Closure Plan
Status: CLOSED (Stage E PASS - 2026-02-14)
Severity: Critical
Objective: Close DP-0005 in full
Included CS: CS-0051, CS-0050, CS-0052, CS-0001, CS-0002, CS-0003, CS-0005, CS-0008, CS-0004, CS-0013
Deferred CS: None
Date: 2026-02-14
Mode: IMPLEMENTATION/VALIDATION
---

# IP-0016 - DP-0005 Mass & Energy Conservation

## 1) Header / Frontmatter
- IP ID: `IP-0016`
- Domain: `DP-0005`
- Status: `CLOSED (Stage E PASS - 2026-02-14)`
- Severity: `CRITICAL`
- Objective: Close DP-0005 in full
- Included CS:
  - `CS-0051` - Stage E mass conservation discontinuity at 8.25 hr during solid->two-phase handoff
  - `CS-0050` - Persistent plant-wide mass conservation imbalance (~10,000 gal class)
  - `CS-0052` - Post-RTCC residual long-run conservation divergence after 8.50 hr
  - `CS-0001` - CoupledThermo canonical mass mode never activated
  - `CS-0002` - TotalPrimaryMass_lb freezes after first step in Regime 2/3
  - `CS-0003` - Boundary flow accumulators never incremented
  - `CS-0005` - CVCS double-count guard works by accident
  - `CS-0008` - No runtime solver mass conservation check
  - `CS-0004` - Pre-solver V*rho PZR mass computation redundant with solver
  - `CS-0013` - Session lifecycle resets for canonical baseline and solver log flag
- Deferred CS: `None`

## 2) Scope
In scope:
- Full DP-0005 closure scope across all CS currently assigned to DP-0005.
- Conservation closure at regime transitions, inventory audit closure, and canonical authority continuity.
- Required validation, evidence packaging, and issue closure governance updates.

Out of scope:
- SG secondary model structural work in DP-0003.
- Pressurizer thermal/pressure behavioral changes in DP-0002 not required for conservation invariants.
- UI modernization and non-conservation visual redesign.

## 3) Mandatory Prerequisite Gate
- RTCC definition in this IP is mandatory and must be approved before any DP-0005 physics implementation starts.
- No DP-0005 code changes are authorized until RTCC acceptance is recorded in this IP.

## 4) Regime Transition Conservation Contract (RTCC)
### 4.1 Invariant (Mathematical)
At every regime transition boundary:
`TotalTrackedMass_before == TotalTrackedMass_after`
within floating tolerance `epsilon_mass`.

Enforced assertion:
`abs(TotalTrackedMass_after - TotalTrackedMass_before) <= epsilon_mass`

### 4.2 Authority Handoff Definition
- Pre-transition canonical authority must be explicitly named (source regime authority object/ledger).
- Post-transition canonical authority must be explicitly named (destination regime authority object/ledger).
- Authority handoff is valid only if reconciliation step is executed and assertion passes.
- No implicit overwrite of canonical mass buckets is allowed at handoff.

### 4.3 Reconciliation Procedure (Abstract)
1. Capture pre-handoff snapshot of all conserved buckets.
2. Compute destination-regime reconstructed mass terms.
3. Compute handoff delta: reconstructed total minus canonical pre-handoff total.
4. Apply equal/opposite transfer and/or ledger reconciliation so total tracked mass is preserved.
5. Assert post-handoff conservation against `epsilon_mass`.
6. Emit reconciliation telemetry with signed delta and authority-source metadata.

### 4.4 Assertion Requirement
- Every regime transition must execute a conservation assert.
- Every transition must log reconciliation delta, including zero-delta events.
- Transition path must fail fast when `abs(delta) > epsilon_mass`.

### 4.5 Applicability
RTCC applies to:
- Solid -> Two-phase transitions.
- Two-phase -> Solid transitions (if/when enabled).
- Any future regime swap where canonical authority changes.

### 4.6 Validation Linkage (Stage E)
RTCC conformance is a hard gate for Stage E conservation pass:
- Transition-step and interval-level conservation must both pass.
- No regime-boundary discontinuity beyond tolerance is allowed.
- Stage E pass decision is blocked if RTCC telemetry is absent.

## Primary Boundary Ownership Contract (PBOC)
### 1.1 Invariant (tick-level)
For every tick:
`(RCS + PZRw + PZRs + VCT + BRS)_t - (RCS + PZRw + PZRs + VCT + BRS)_0 = ExternalNetMass_t`
within configured Stage E tolerance.

### 1.2 Single Authority Rule
Primary-boundary flow effects are computed once per tick in one canonical event.
The event payload is required to include:
- `dm_RCS_lbm`
- `dm_PZRw_lbm`
- `dm_PZRs_lbm`
- `dm_VCT_lbm`
- `dm_BRS_lbm`
- `dm_external_lbm` (plus supporting gallons and conversion density source)
- metadata: regime, tick index/time, and contributing subflows (letdown, charging-to-primary, seal injection, seal return, divert, makeup, CBO loss)

No other path may apply primary-boundary mass effects to conserved buckets outside the PBOC application step.

### 1.3 Application Order
Per tick, the boundary-flow order is fixed:
1. Compute PBOC event (single source flow capture and signed deltas)
2. Apply event to component masses (single apply)
3. Apply event to ledger/accumulators (single apply from same event values)
4. Snapshot audit totals (`massError_lbm` and interval audit)

Forbidden patterns:
- apply then overwrite
- overwrite then audit

### 1.4 Mapping Table (boundary term to bucket ownership/sign)
| Boundary Term | Definition | RCS | PZRw | PZRs | VCT | BRS | External | Notes |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Charging to primary | `max(0, chargingFlow - sealInjection)` | `+` | `0` | `0` | `-` | `0` | `0` | Internal transfer VCT -> RCS |
| Letdown from primary | `max(0, letdownFlow)` | `-` | `0` | `0` | `+` | `0` | `0` | Internal transfer RCS -> VCT |
| Seal injection | `rcpCount * SEAL_INJECTION_PER_PUMP_GPM` | included in charging split | `0` | `0` | included in charging split | `0` | `0` | Internal to CVCS/primary path |
| Seal return (leakoff return) | `rcpCount * SEAL_LEAKOFF_PER_PUMP_GPM` | `-` (primary outflow term) | `0` | `0` | `+` | `0` | `0` | Internal transfer to VCT |
| Divert to BRS | `vctState.DivertFlow_gpm` | `0` | `0` | `0` | `-` | `+` | `0` | Internal transfer VCT -> BRS |
| Makeup (external) | `vctState.MakeupFlow_gpm` when not BRS-sourced | `0` | `0` | `0` | `+` | `0` | `+` | True plant external IN |
| CBO loss | `PlantConstants.CBO_LOSS_GPM` when RCPs running | `0` | `0` | `0` | `-` | `0` | `-` | True plant external OUT |

## 5) Work Breakdown By CS (Full DP-0005 Coverage)
| CS | Summary | Root Cause | Correction Class | Affected Modules | Validation Requirement | Interaction With Other CS |
|---|---|---|---|---|---|---|
| CS-0051 | 8.25 hr one-step discontinuity at solid->two-phase handoff (~17.4 klbm jump) | Authority swap with handoff overwrite pattern and missing same-tick reconciliation (suspected) | RTCC handoff conservation enforcement and canonical reconciliation | `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs`, `Assets/Scripts/Validation/HeatupSimEngine.cs` | Handoff delta log present, transition assert passes, no jump beyond tolerance | Primary blocker for closing CS-0050; depends on stable canonical authority model from CS-0001/0002/0004 |
| CS-0050 | Persistent long-duration plant-wide conservation imbalance | Conservation accounting/audit integrity defect class (suspected) | Audit equation closure and bucket completeness/sign/order correction | `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs`, `Assets/Scripts/Physics/VCTPhysics.cs` | Stage E mass error remains bounded and convergent; interval audits agree | Includes residual effects from CS-0051; depends on CS-0003/0005/0008 observability and accounting paths |
| CS-0052 | Post-RTCC long-run conservation divergence after 8.50 hr | Residual accounting defect class after transition reconciliation (suspected) | Extended plant-wide accounting closure and residual bucket/sign audit | `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs`, `Assets/Scripts/Physics/VCTPhysics.cs` | Stage E interval/step conservation trends must remain in threshold through full window | Opened from Stage E failure handling; related to CS-0050 |
| CS-0001 | Canonical solver mode was never active in coupled steps | Missing canonical mass argument usage at solver callsites (confirmed historical) | Canonical authority activation and enforcement hardening | `Assets/Scripts/Physics/CoupledThermo.cs`, `Assets/Scripts/Physics/RCSHeatup.cs`, `Assets/Scripts/Validation/HeatupSimEngine.cs` | Canonical mode always active where required; no legacy fallback authority drift | Foundation for CS-0002/0003/0004/0005/0008; must remain non-regressed for CS-0050/0051 closure |
| CS-0002 | Ledger froze after first rebase step in R2/R3 | Ledger maintenance coupled to inactive mode path (confirmed historical) | Continuous ledger mutation by boundary flows with explicit ownership | `Assets/Scripts/Validation/HeatupSimEngine.cs` | Ledger evolves each step under CVCS net flow; no stale baseline behavior | Shares authority chain with CS-0001 and session reset guarantees from CS-0013 |
| CS-0003 | Boundary accumulators did not update | Missing increment sites in runtime paths (confirmed historical) | Boundary accumulator completeness and consistency checks | `Assets/Scripts/Validation/HeatupSimEngine.cs`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs` | In/out accumulators reconcile with expected boundary totals | Supports CS-0050 audit closure and CS-0008 runtime checks |
| CS-0005 | Double-count guard relied on fragile execution order | Pre-apply semantics and solver overwrite interaction (confirmed historical) | Explicit ownership/order contract for CVCS mass application | `Assets/Scripts/Validation/HeatupSimEngine.cs`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs`, `Assets/Scripts/Physics/CoupledThermo.cs` | No double-count and no dropped-count across all regimes | Interacts directly with CS-0003 and CS-0050 accounting closure |
| CS-0008 | No runtime solver conservation guardrail existed | Checks only in dead/manual paths (confirmed historical) | Runtime assertion/telemetry guardrails (diagnostic defense-in-depth) | `Assets/Scripts/Physics/RCSHeatup.cs`, `Assets/Scripts/Physics/CoupledThermo.cs` | Step-level conservation diagnostics emitted at runtime with thresholds | Required for detecting regressions while closing CS-0050/0051 |
| CS-0004 | Pre-solver mass precompute ownership confusion | Legacy V*rho precompute overwritten by solver path (confirmed historical) | Single-source ownership and mass-path consistency enforcement | `Assets/Scripts/Validation/HeatupSimEngine.cs`, `Assets/Scripts/Physics/CoupledThermo.cs` | No ambiguous authority path at regime boundaries | Critical to clean handoff semantics for CS-0051 |
| CS-0013 | Session lifecycle did not reset conservation/session flags | Missing reset logic in persistent engine/static fields (confirmed historical) | Session integrity reset enforcement | `Assets/Scripts/Validation/HeatupSimEngine.Init.cs`, `Assets/Scripts/Physics/CoupledThermo.cs` | Multi-run reproducibility without stale ledger/session flags | Prevents false drift and protects repeatability of CS-0050/0051 validation |

## 6) Dependency and Execution Order
1. RTCC acceptance gate (design complete, approved in this IP).
2. Regime-transition conservation enforcement for CS-0051.
3. Plant-wide audit equation and bucket closure for CS-0050.
4. Non-regression reconfirmation for CS-0001/0002/0003/0005/0008/0004/0013.
5. Stage E rerun evidence package and formal closure updates.

## 7) Validation Plan and Closure Criteria
### 7.1 Stage E Pass Thresholds
- Stage E overall: `PASS`.
- Conservation criterion: `PASS`.
- Regime-transition discontinuity: `abs(DeltaTransitionMass_lbm) <= epsilon_mass`.
- IP-0016 temporary default transition tolerance: `epsilon_mass = 50 lbm`.
- `epsilon_mass` must be sourced from approved validation constants/configuration (not hardcoded in physics logic).

### 7.2 Inventory Audit Thresholds
- Interval inventory agreement must remain bounded with no one-step discontinuity class event.
- Absolute interval conservation error threshold: `<= 500 lbm` at transition-adjacent intervals.
- Percent error threshold at interval summary: `<= 0.05%`.
- Post-RTCC expectation: transition-adjacent discontinuity should collapse near zero; recurring `200-300 lbm` events require separate leakage investigation before closure.

### 7.3 Step-Level Thresholds
- Step-level `massError_lbm` must remain below critical trip threshold and show no structural divergence trend.
- No single-step transition event may exceed `epsilon_mass`.

### 7.4 Closure Conditions
- All included CS are moved to closed/resolved states with evidence.
- Stage E evidence package archived and reproducible.
- RTCC telemetry present for every regime boundary in tested scenarios.
- Registry links updated: CS -> IP-0016 evidence references.

### 7.5 Evidence Artifacts (Required)
- `Updates/Issues/IP-0016_StageE_Validation_<timestamp>.md`
- `HeatupLogs/Heatup_Interval_*.txt` for full run window including all transition intervals
- `HeatupLogs/Heatup_Report_*.txt`
- Transition reconciliation log excerpt per boundary with signed delta and assert result

## 8) Definition of Done
- [x] RTCC approved in this IP before implementation.
- [x] CS-0051 transition discontinuity resolved and closed.
- [x] CS-0050 plant-wide conservation divergence resolved and closed.
- [x] CS-0052 post-RTCC long-run divergence resolved and closed.
- [x] Non-regression checks complete for CS-0001/0002/0003/0005/0008/0004/0013.
- [x] Stage E failure handling completed (new CS logged: CS-0052).
- [x] Stage E conservation passes with archived evidence.
- [x] Registry updated for CS-0050/CS-0051/CS-0052 closure entries.
- [ ] All DP-0005 CS formally closed in registry.

## 9) Latest Execution Outcome (2026-02-14)
- Stage E executed via `Critical.Validation.StageERunner.RunStageE` (batch).
- Evidence package archived:
  - `Updates/Issues/IP-0016_StageE_Validation_2026-02-14_133520.md`
  - `Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_133517.md`
  - `HeatupLogs/Heatup_Report_20260214_133520.txt`
  - `HeatupLogs/Heatup_Interval_*.txt` (full run window)
- Gate summary:
  - RTCC transition boundary gate: `PASS` (`assert delta = 0.000 lbm`, epsilon `50 lbm`)
  - Stage E conservation gate: `FAIL` (`massError_lbm = 43553.0 lbm`)
  - Interval gate: `FAIL` (first fail at `8.50 hr`: `3668.7 lbm`, `0.397%`)
  - Step-level divergence gate: `FAIL` (`max mass error = 57911.69 lbm`)
- Execution note: Stage E failed due to `CS-0052`; RTCC passed; next action is `CS-0052` investigation.

## 10) Latest Execution Outcome (2026-02-14 14:10:59 Rerun)
- Stage E rerun executed via `Critical.Validation.StageERunner.RunStageE` (batch).
- Evidence package archived:
  - `Updates/Issues/IP-0016_StageE_Validation_2026-02-14_141059.md`
  - `Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_141055.md`
  - `HeatupLogs/Heatup_Report_20260214_141059.txt`
  - `HeatupLogs/Heatup_Interval_034_8.25hr.txt`
  - `HeatupLogs/Heatup_Interval_035_8.50hr.txt`
  - `HeatupLogs/Heatup_Interval_036_8.75hr.txt`
  - `HeatupLogs/Unity_StageE_IP0016_final.log`
- Gate summary:
  - Stage E overall: `PASS`
  - Conservation gate: `PASS` (`14.6 lbm`, `0.002%`)
  - Interval gate at prior onset window:
    - `8.25 hr`: `PASS` (`1.9 lbm`, `0.000%`)
    - `8.50 hr`: `PASS` (`92.3 lbm`, `0.010%`)
    - `8.75 hr`: `PASS` (`92.4 lbm`, `0.010%`)
  - RTCC gate: `PASS` (`transition count=1`, `assertion failures=0`, `last assert delta=0.000 lbm`)
  - PBOC gate: `PASS` (`events=6480`, `pairing assertion failures=0`)
- Execution note:
  - `CS-0050`, `CS-0051`, and `CS-0052` are closed under IP-0016 using PASS evidence set `2026-02-14 14:10:59`.

## 11) Closure Summary (2026-02-14)
- IP status: `CLOSED`
- Final conservation error: `14.6 lbm (0.002%)`
- Max mass error observed in PASS rerun evidence: `92.50 lbm`
- Prior onset interval (`8.50 hr`) metric: `92.3 lbm (0.010%)` -> `PASS`
- RTCC counters:
  - Transition count: `1`
  - Assertion failures: `0`
  - Last assert delta: `0.000 lbm`
- PBOC counters:
  - Events recorded: `6480`
  - Pairing assertion failures: `0`
- Scope note:
  - `Temp target: FAIL` remains in `HeatupLogs/Heatup_Report_20260214_141059.txt` and is explicitly out-of-scope for DP-0005/IP-0016 closure.

## 12) Governance Follow-Up
- DP-0005 is not fully closed at the registry level because one legacy CS remains non-closed.
- Remaining DP-0005 CS have been moved to `Updates/ImplementationPlans/IP-0017_DP-0005_Remaining_Closure.md`:
  - `CS-0001`, `CS-0002`, `CS-0003`, `CS-0004`, `CS-0005`, `CS-0008`, `CS-0013`
- IP-0017 execution evidence:
  - `Updates/Issues/IP-0017_StageE_NonRegression_2026-02-14_164742.md`
  - `Updates/Issues/IP-0017_RunA_RunB_SameProcess_2026-02-14_164742.md` (prior non-strict A/B parity)
  - `Updates/Issues/IP-0017_RunA_RunB_SameProcess_STRICT_2026-02-14_171456.md` (strict same-process closure evidence)
  - `Updates/Issues/IP-0017_SameProcess_Execution_20260214_171456.md`
  - Closed under IP-0017: `CS-0001`, `CS-0002`, `CS-0003`, `CS-0004`, `CS-0005`, `CS-0008`, `CS-0013`
- `CS-0050`, `CS-0051`, and `CS-0052` remain closed under IP-0016 and are not reopened by this follow-up routing.
