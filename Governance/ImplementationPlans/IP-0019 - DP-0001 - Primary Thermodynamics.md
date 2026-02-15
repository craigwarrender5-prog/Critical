---
IP ID: IP-0019
DP Reference: DP-0001
Title: Primary Thermodynamics Implementation Plan
Changelog Version Association: 0.5.6.0
Status: CLOSED
Date: 2026-02-14
Mode: CLOSED
Source of Scope Truth: Governance/IssueRegister/issue_register.json
Closure Date: 2026-02-15
Final Run Stamp: 20260215_085052
Final Stage E Report: Governance/ImplementationReports/IP-0019_StageE_ExtendedValidation_Report_2026-02-15.md
---

# IP-0019 - DP-0001 - Primary Thermodynamics

## 1) Governance Header
- DP Reference: `DP-0001 - Primary Thermodynamics`
- Version Association: `0.5.6.0` (future changelog linkage only)
- IP Status: `CLOSED`
- Authorization State: `CLOSED AFTER VALIDATION (2026-02-15)`
- Scope Basis: all ACTIVE CS with `assigned_dp = DP-0001` in `Governance/IssueRegister/issue_register.json`
- Final validation run: `20260215_085052`
- Final validation report: `Governance/ImplementationReports/IP-0019_StageE_ExtendedValidation_Report_2026-02-15.md`

## 2) Governance Constraints
- This document is now in closed state; execution constraints remain recorded for traceability.
- Simulation/physics/runtime code modifications are authorized only for in-scope DP-0001 CS under this IP.
- Do not create or modify changelog content until IP closure conditions are fully satisfied.
- Do not alter issue severity.
- Do not reassign CS to other DPs.
- Changelog entry for version `0.5.6.0` shall be created only after successful IP closure.
- No cross-domain scope expansion permitted.

## 3) Full ACTIVE CS Scope (DP-0001)
| CS ID | Title | Severity | Status |
|---|---|---|---|
| CS-0021 | Solid-regime pressure decoupled from mass change | HIGH | READY_FOR_FIX |
| CS-0022 | Early pressurization response mismatched with controller actuation | MEDIUM | READY_FOR_FIX |
| CS-0023 | Surge flow trend rises while pressure remains flat | MEDIUM | READY_FOR_FIX |
| CS-0031 | RCS heat rate escalation after RCP start may be numerically aggressive | MEDIUM | READY_FOR_FIX |
| CS-0033 | RCS bulk temperature rise with RCPs OFF and no confirmed forced flow | HIGH | READY_FOR_FIX |
| CS-0034 | No equilibrium ceiling for RCS temperature in Regime 0/1 (pre-RCP) | MEDIUM | READY_FOR_FIX |
| CS-0038 | PZR level spike on RCP start (single-frame transient) | HIGH | READY_FOR_FIX |
| CS-0055 | RCS temperature rises during isolated PZR heating with no RCP active | CRITICAL | READY_FOR_FIX |
| CS-0056 | RHR isolation initiates on first RCP start instead of post-4-RCP near-350F sequence | HIGH | READY_FOR_FIX |
| CS-0061 | Primary boundary mass transfer uses fixed 100F atmospheric density instead of runtime state | CRITICAL | READY_FOR_FIX |
| CS-0071 | Coordinator owns multiple writer paths for T_rcs/pressure with post-module mutation | HIGH | READY_FOR_FIX |

## 4) Severity Summary
- Total ACTIVE CS in scope: `11`
- Highest severity in scope: `CRITICAL`
- Distribution:
  - `CRITICAL`: 2
  - `HIGH`: 5
  - `MEDIUM`: 4
  - `LOW`: 0

## 5) Execution Sequencing (Logical Grouping Within DP-0001)
1. Group 1 - Primary Ownership and Boundary Correctness
   - CS-0061, CS-0071
   - Goal: establish single-writer and physically correct boundary mass transfer foundation before behavioral tuning.
2. Group 2 - Solid/No-RCP Thermodynamic Integrity
   - CS-0055, CS-0021, CS-0033, CS-0034
   - Goal: enforce physically plausible pre-RCP thermal and pressure behavior.
3. Group 3 - Controller and Coupling Response Consistency
   - CS-0022, CS-0023
   - Goal: align controller actuation, surge behavior, and pressure response after Group 1-2 stabilization.
4. Group 4 - Transition and Sequencing Stability
   - CS-0038, CS-0056, CS-0031
   - Goal: harden RCP start transitions, RHR isolation sequencing, and post-start thermal ramp behavior.
5. Group 5 - Final Domain Convergence
   - Execute integrated validation and close residual cross-CS interactions within DP-0001 only.

## 6) Stage Gates

### Stage A - Root Cause Confirmation
- Objective:
  - confirm current reproducibility and root-cause evidence for each scoped CS using current baseline.
  - for `CS-0021`, `CS-0022`, and `CS-0023`, explicitly determine whether current status is due to true regression versus historically incomplete resolution.
- Required outputs:
  - per-CS root-cause confirmation record
  - dependency map across CS in this IP
  - baseline metrics captured for later A/B comparison
  - explicit disposition memo for `CS-0021`/`CS-0022`/`CS-0023`: `REGRESSION_CONFIRMED` or `PRIOR_RESOLUTION_INCOMPLETE`
- Exit criteria:
  - all 11 CS have confirmed root-cause statements and reproducible trigger conditions
  - `CS-0021`/`CS-0022`/`CS-0023` include explicit regression-vs-incomplete-resolution determination before any corrective implementation is authorized
  - no scope change outside DP-0001

### Stage B - Design Correction Strategy
- Objective:
  - define correction architecture and sequencing for each CS cluster.
- Required outputs:
  - correction design package per execution group
  - validation metric definitions and acceptance thresholds (frozen before Stage C)
  - regression watchlist for adjacent domains (monitor-only, no cross-domain remediation)
- Exit criteria:
  - strategy approved for all groups with explicit mapping CS -> design action -> validation check

### Stage C - Controlled Remediation
- Objective:
  - implement approved DP-0001 corrections in controlled sequence.
- Required outputs:
  - change log of remediation actions by CS and group
  - traceability matrix mapping each change to a CS and Stage B design item
- Exit criteria:
  - all planned remediation actions completed for in-scope CS
  - no prohibited actions executed (no severity change, no reassignment, no cross-domain expansion)

### Stage D - Domain Validation
- Objective:
  - verify DP-0001 behavior against CS-specific validation criteria.
- Required outputs:
  - per-CS validation report and pass/fail disposition
  - integrated DP-0001 domain validation report
- Exit criteria:
  - all CS-specific criteria pass, or any remaining failures are retained within DP-0001 with documented corrective loop

### Stage E - System Regression Validation
- Objective:
  - run system-level regression to confirm no unacceptable regressions from DP-0001 remediation.
- Required outputs:
  - system regression evidence package
  - explicit non-regression summary for adjacent domains
  - `DP-0002` observation log capturing pressurizer/two-phase symptom changes as monitor-only findings
- Exit criteria:
  - system regression gates pass
  - any `DP-0002` symptom observed during Stage E is recorded but not remediated under this IP unless causality is proven to originate from DP-0001-scoped defects
  - DP-0001 closure recommendation issued with evidence links

## 7) Risk Assessment
| Risk | Impact | Likelihood | Mitigation |
|---|---|---|---|
| Hidden coupling between boundary transfer and state ownership (CS-0061, CS-0071) | Can invalidate multiple downstream fixes | High | Resolve ownership and boundary correctness first (Group 1) and gate later stages on this pass |
| Over-correction in no-RCP thermal behavior (CS-0055, CS-0033, CS-0034) | Non-physical stabilization or false damping | Medium | Freeze thermodynamic acceptance metrics in Stage B before remediation |
| Transition instability at RCP start (CS-0038, CS-0031) | Spikes and numerical artifacts in startup window | Medium | Isolate transition tests and require transient-shape acceptance checks in Stage D |
| Sequencing logic drift for RHR isolation (CS-0056) | Procedure realism and operational logic regression | Medium | Add explicit sequence assertions and timing gates in Stage D/E |
| Controller-response mismatch persisting after base fixes (CS-0022, CS-0023) | Pressure/flow behavior remains inconsistent | Medium | Defer controller tuning until ownership/boundary fixes complete; run coupled-response validation |

## 8) Validation Criteria Per CS
| CS ID | Validation Criteria (Pass Conditions) |
|---|---|
| CS-0055 | With no RCP active and isolated PZR heating conditions, RCS bulk temperature trend does not show non-physical sustained rise attributable to invalid coupling path. |
| CS-0061 | Primary boundary transfer uses runtime thermodynamic state inputs (not fixed 100F atmospheric density) and mass-accounting consistency is maintained in validation outputs. |
| CS-0021 | Solid-regime pressure responds consistently to mass and thermal expansion changes; pressure is no longer pinned under non-zero net driving conditions. |
| CS-0033 | RCS bulk temperature behavior with RCPs OFF is physically consistent with no confirmed forced-flow pathway in the modeled regime. |
| CS-0034 | Regime 0/1 pre-RCP model exhibits a defined equilibrium/limiting behavior rather than unbounded rise under equivalent boundary conditions. |
| CS-0038 | RCP-start transient no longer produces unacceptable single-frame PZR level spikes beyond defined Stage B acceptance threshold. |
| CS-0056 | RHR isolation trigger sequence aligns with intended logic (post-4-RCP near-350F sequence) rather than first-RCP initiation. |
| CS-0071 | Thermodynamic state ownership for `T_rcs` and pressure is governed by explicit single-writer arbitration with no conflicting post-module mutation paths. |
| CS-0022 | Early pressurization response magnitude and sign are consistent with controller actuation authority, limits, and expected transport timing. |
| CS-0023 | Surge flow and pressure trends remain coupled and physically coherent across solid-regime and transition windows. |
| CS-0031 | Post-RCP-start RCS heat-rate trajectory remains numerically stable and within Stage B-defined physical plausibility bounds. |

Note:
- Numerical thresholds and scenario windows are frozen in Stage B and applied unchanged in Stage D/E.

## 9) DP-Level Closure Criteria
- All 11 in-scope ACTIVE CS are moved from `READY_FOR_FIX` to closed/resolved status with evidence.
- Stage A through Stage E gates are all passed and documented.
- Traceability is complete for every CS: root cause -> design strategy -> remediation -> validation evidence.
- No cross-domain scope expansion occurred during execution.
- No issue severity changes were made.
- No CS reassignment to other DPs was performed.
- Changelog work remains deferred until closure; only after successful IP closure may changelog version `0.5.6.0` entry be created.

## 10) Authorization Statement
IP-0019 execution completed under staged gate controls (A-E) with strict DP-0001 scope containment and monitor-only handling for DP-0002 symptoms.

## 11) Execution Evidence Package
- Stage A-E evidence and traceability package:
  - `Governance/ImplementationReports/IP-0019_Execution_Report_2026-02-14.md`
- Closure recommendation status:
  - `Governance/ImplementationReports/IP-0019_Closure_Recommendation_2026-02-14.md`
- Final Stage E Extended validation:
  - `Governance/ImplementationReports/IP-0019_StageE_ExtendedValidation_Report_2026-02-15.md`
