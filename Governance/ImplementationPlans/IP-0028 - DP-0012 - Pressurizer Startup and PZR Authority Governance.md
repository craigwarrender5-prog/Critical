---
IP ID: IP-0028
DP Reference: DP-0012
Title: DP-0012 - Pressurizer Startup and PZR Authority Governance
Status: Draft
Date: 2026-02-16
Mode: SPEC/DRAFT
Source of Scope Truth: Governance/IssueRegister/issue_index.json (active CS assigned to DP-0012)
Constraint: Planning artifact only; no code changes in this step
---

# IP-0028 - DP-0012 - Pressurizer Startup and PZR Authority Governance

## 1) Scope and Included CS

This IP covers all currently active CS items assigned to `DP-0012`:

| CS ID | Severity | Status | Domain | Title |
|---|---|---|---|---|
| CS-0040 | High | READY_FOR_FIX | Validation & Diagnostics | RVLIS indicator stale during PZR drain |
| CS-0081 | Medium | READY_FOR_FIX | Pressurizer & Two-Phase Physics | Solid-plant high pressure control band is configured above documented operating band. |
| CS-0091 | High | READY_FOR_FIX | Pressurizer & Two-Phase Physics | PZR bubble closure path shows persistent non-convergence with large residuals and post-residual renormalization masking. |
| CS-0093 | High | READY_FOR_FIX | Pressurizer & Two-Phase Physics | Complete Pressurizer (PZR) System Remodel to Align with Technical_Documentation and Replace Current Heuristic Bubble Model |
| CS-0094 | Medium | READY_FOR_FIX | Pressurizer & Startup Control | Add Cold-Start Stabilization Hold and PZR Heater Off/Auto/Manual Control to Prevent Immediate Pressurization and Improve Operator Realism |
| CS-0096 | High | READY_FOR_FIX | Pressurizer & Two-Phase Physics | PZR heaters capped after reaching operating pressure hold band |

## 1.1) Investigation-to-Remediation Alignment

The following mapping is mandatory and governs implementation precision for this IP:

| CS ID | Investigation finding baseline | Required remediation direction | Planned stage |
|---|---|---|---|
| CS-0040 | RVLIS stale indication during PZR drain is unresolved in current active scope. | Reproduce under current runtime path, isolate stale update path, and implement deterministic indicator-refresh fix with non-regression evidence. | Stage E |
| CS-0081 | Solid-plant pressure control high-band mismatch risk versus documented 320-400 psig operating band. | Align control-band behavior with approved baseline and treat any higher value as relief/protection-only unless explicitly documented as an intentional deviation. | Stage D |
| CS-0091 | Bubble closure path can propagate unresolved residual states; renormalization can mask non-converged closure error. | Enforce strict convergence-gated state commit; remove masking paths or explicitly bound/document any retained path with convergence limits and diagnostics. | Stage D |
| CS-0093 | Legacy heuristic PZR architecture lacks robust converged mass-volume-energy authority and documentation-aligned behavior. | Execute PZR authority/model remediation with explicit Technical_Documentation traceability and deterministic validation metrics. | Stage D (+ Stage A/B traceability/freeze) |
| CS-0094 | Startup sequence allows early heater authority without explicit startup governance gating. | Implement deterministic startup hold and explicit heater authority governance with reproducible transition evidence. | Stage C |
| CS-0096 | Hold-band heater authority limiting behavior is active and not reconciled to documented intent. | Identify active limiter precedence at runtime and either align implementation to documentation or document approved behavior as intentional with explicit rationale. | Stage C (+ Stage A/B traceability/authority freeze) |

## 2) Hard Constraints

1. Work is limited to `DP-0012` CS scope above.
2. No new CS IDs or DP IDs are created as part of this IP unless constitutionally required by deferral/supersession.
3. No changelog version is pre-selected in this IP.
4. Technical baseline authority must come from `Technical_Documentation/` sources cited in stage evidence.

## 3) Pre-Implementation Baseline Gate (Constitution Article VIII)

Before first implementation activity under this IP:

1. Commit all pre-existing/unrelated working-tree changes in a separate baseline commit.
2. Push the baseline commit to GitHub remote.
3. Record baseline rollback anchor in this IP:
   - Baseline commit hash: `0dd5d4039099e7d872bef7b6caa82952f0061bac`
   - Baseline commit UTC timestamp: `2026-02-16T11:08:40Z`
4. Confirm working tree is clean.

Implementation under IP-0028 MUST NOT start until all four items are complete.

## 4) Stage Plan

### Stage A - Documentation and Runtime Baseline Freeze

Objectives:
1. Freeze documented design intent for startup pressure control, heater authority, hold-band behavior, and spray constraints.
2. Capture current runtime behavior and limiter telemetry for startup and hold transitions.
3. Establish mandatory documentation traceability for all numeric thresholds, operating bands, heater authority behavior, spray constraints, convergence thresholds, and startup pressure targets to specific files under `Technical_Documentation/`.

Evidence artifact:
`Governance/Issues/IP-0028_StageA_BaselineFreeze_<timestamp>.md`

Mandatory Stage A artifact contents:
1. Traceability table covering every governed threshold/behavior in Stage A scope.
2. Source file name references for each traced item under `Technical_Documentation/`.
3. Section references where applicable.
4. Explicit statement of any documented deviations between implementation baseline and documentation baseline.

Exit criteria:
1. Technical_Documentation traceability table is complete.
2. Runtime baseline runs are reproducible and stored with run stamps/log paths.
3. Every threshold/band/authority/spray/convergence/startup-target item in scope has an explicit documentation trace or an explicit documented deviation statement.

### Stage B - Authority and Limiter Design Freeze

Objectives:
1. Define authoritative precedence and limiter order for startup and pressurizer control.
2. Numerically freeze thresholds and release/hold gating behavior.
3. Produce an explicit authority hierarchy table for startup and steady-state control resolution.

Evidence artifact:
`Governance/Issues/IP-0028_StageB_DesignFreeze_<timestamp>.md`

Mandatory Stage B artifact contents:
1. Authority precedence table with explicit priority order.
2. Activation conditions for each authority/limiter state.
3. Override relationships between authority states.
4. Limiter precedence resolution rules.
5. Startup vs steady-state distinctions for authority and limiter behavior.

Exit criteria:
1. No unresolved authority ambiguity.
2. All thresholds and gating rules are explicitly declared.
3. Authority hierarchy and limiter precedence are explicit and complete, with no undefined conflict path.

### Stage C - Startup Governance Implementation

Objectives:
1. Implement startup stabilization and heater authority governance for CS-0094.
2. Implement/verify hold-band heater authority behavior and limiter observability for CS-0096.
3. Validate deterministic replay behavior across a minimum of three independent reruns.
4. For CS-0096, produce explicit limiter attribution and disposition:
   - implementation change to match documented intent, or
   - documented approved deviation with rationale and bounded behavior.

Evidence artifact:
`Governance/Issues/IP-0028_StageC_StartupGovernance_<timestamp>.md`

Exit criteria:
1. Deterministic startup hold/authority behavior across reruns.
2. Limiter reason visibility in logs/telemetry.
3. At least three independent reruns show identical event ordering.
4. At least three independent reruns show identical limiter activation sequence.
5. At least three independent reruns show identical hold-release gating behavior.
6. CS-0096 disposition is explicit (implementation-aligned or documented approved deviation), with evidence references.

### Stage D - Pressurizer Physics and Control Remediation

Objectives:
1. Address pressure-band and two-phase closure issues tied to CS-0081, CS-0091, CS-0093.
2. Enforce convergence-gated commit behavior and prohibit silent residual masking.
3. For CS-0091-related post-residual renormalization or masking logic, require either full removal or explicit documentation with justification and convergence bounds.
4. For CS-0081, explicitly verify control-band limits against documented 320-400 psig operation and separate relief/protection behavior.
5. For CS-0091, include continuity controls for phase-transition floor logic where required to eliminate discrete nonphysical jumps.

Evidence artifact:
`Governance/Issues/IP-0028_StageD_PressurizerControlValidation_<timestamp>.md`

Exit criteria:
1. Pressure-band behavior conforms to frozen documentation baseline or documented justified deviation.
2. Convergence and residual acceptance thresholds are satisfied in validation runs.
3. No silent residual masking paths remain.
4. Control-band vs relief/protection boundaries are explicitly validated and traceable in evidence.
5. Phase-transition continuity checks pass within declared bounds.

### Stage E - Indicator Integrity and System Regression

Objectives:
1. Resolve/validate RVLIS drain indicator behavior (CS-0040).
2. Execute full non-regression suite across affected startup/pressurizer flows.
3. For CS-0040, require reproduction confirmation, root-cause isolation evidence, and post-fix validation in the same artifact set.

Evidence artifact:
`Governance/Issues/IP-0028_StageE_SystemRegression_<timestamp>.md`

Exit criteria:
1. All included CS acceptance checks mapped with PASS/FAIL evidence.
2. No blocking regressions.
3. CS-0040 includes explicit before/after evidence showing stale-indicator condition resolved.

## 5) Acceptance Matrix (IP-Level)

| CS ID | Acceptance Condition | Evidence Minimum |
|---|---|---|
| CS-0040 | RVLIS indicates drain-state behavior correctly in affected scenarios | Before/after run evidence with telemetry correlation |
| CS-0081 | Solid-plant pressure control band behavior aligns with approved baseline | Threshold/config traceability and run validation |
| CS-0091 | Two-phase closure convergence behavior meets defined limits | Residual/convergence reports with deterministic runs |
| CS-0093 | PZR remodel outcomes conform to documented technical intent | Crosswalk from documentation to runtime evidence |
| CS-0094 | Startup hold and heater governance behavior is deterministic and correctly prioritized | Event sequence and telemetry proof across reruns |
| CS-0096 | Hold-band heater authority limits are explained by explicit active limiters and reconciled to documented intent | Limiter attribution logs + runtime snapshots + alignment/deviation decision record |

## 6) Validation and Closure Preconditions

1. Stage A-E artifacts exist and are internally consistent.
2. Each included CS has explicit PASS/FAIL disposition with evidence reference.
3. Issue register/index/archive updates follow constitution at closure time.
4. Changelog/version classification occurs only during closure workflow (Article XI / XII).

## 7) Initial Execution Status

Current status: `Draft`

To move to `Authorized`:
1. Baseline gate in Section 3 must be fully completed.
2. DP execution authorization must be confirmed for DP-0012.
