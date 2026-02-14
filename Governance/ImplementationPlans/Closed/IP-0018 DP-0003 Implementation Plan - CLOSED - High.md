# IP-0018 DP-0003 Implementation Plan - CLOSED - High

## 1. Frontmatter / Header
- Plan ID: `IP-0018`
- Domain Plan: `DP-0003`
- Status: `CLOSED`
- Severity: `High` (derived from included outstanding CS severities)
- Created date: `2026-02-14`
- Owner: `Codex (planning)`
- Stage Gate: A-E checklist
  - [x] Stage A - Build/compile sanity (`Updates/ImplementationReports/IP-0018_StageA_2026-02-14.log`)
  - [x] Stage B - Static checks (`Updates/ImplementationReports/IP-0018_StageB_2026-02-14.log`)
  - [x] Stage C - Targeted unit/component checks (`HeatupLogs/Unity_IP0018_StageC_20260214_1918.log`)
  - [x] Stage D - DP-0003 scenario/regression runs (`Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_191359.md`)
  - [x] Stage E - Per-CS acceptance pass/fail (`Updates/Issues/IP-0018_StageE_Validation_2026-02-14_191442.md`)

## 2. Scope
This plan covers all outstanding CS items in DP-0003 as of 2026-02-14.

Included CS items:
- `CS-0009` - No SG secondary energy balance validation
- `CS-0017` - Missing SG pressurization/hold state in startup procedure
- `CS-0019` - Secondary temperature cannot progress toward high-pressure saturation region
- `CS-0020` - Secondary remains largely inert or wrongly bounded during primary heatup
- `CS-0054` - DP-0003 Stage E failure: SG secondary pressure flatline under active heat input

Scope guardrails:
- Source-of-truth scope is `Updates/DomainPlans/DP-0003 - Steam Generator Secondary Physics.md`.
- Closed CS under IP-0015 (`CS-0014`, `CS-0015`, `CS-0016`, `CS-0018`, `CS-0047`, `CS-0048`) are excluded from implementation scope.
- Prior plans `IP-0006` and `IP-0007` are treated as historical placeholders (not authorized), and this plan supersedes their remaining open work items for DP-0003 execution.

## 3. CS-to-Work Mapping Table
| CS ID | Title | Severity | Current status (from register) | Evidence / reproduction reference | Suspected area (files/systems) | Proposed fix strategy (1-3 bullets) | Validation method (specific pass/fail criteria) | Risk / dependencies |
|---|---|---|---|---|---|---|---|---|
| `CS-0009` | No SG secondary energy balance validation | Medium | `CLOSED` (`Governance/IssueRegister/issue_register.json`) | Register observation: `TotalHeatRemoval_MW` consumed without bounds cross-check; no dedicated artifact listed in active register. Legacy reference: `Updates/IssueRegister_DEPRECATED.md` (CS-0009 section). | `SGMultiNodeThermal` / `HeatupSimEngine` (register system area) | - Add SG-side runtime energy validation checks for negative/excessive heat removal. <br> - Add cumulative SG-vs-primary energy consistency check for startup window. <br> - Surface validation state in diagnostics/log output for gate decisions. | PASS if startup scenario logs show: (1) no negative SG heat removal values, (2) SG heat removal remains within defined physical bounds for all intervals, (3) cumulative SG-vs-primary mismatch within agreed threshold. FAIL on any threshold breach or missing telemetry. | Depends on stable SG secondary state behavior from closed chain (`CS-0014/15/16/18`) and remaining startup progression fixes (`CS-0017/19/20`). Threshold tuning risk if baseline scenarios are noisy. |
| `CS-0017` | Missing SG pressurization/hold state in startup procedure | High | `CLOSED` (`Governance/IssueRegister/issue_register.json`) | Legacy register detection: heatup log review 0-16.50 hr and source `ISSUE-SG-004` (`Updates/IssueRegister_DEPRECATED.md`). | `HeatupSimEngine` SG state machine / `SGMultiNodeThermal` | - Define and wire explicit SG pressurize/hold state transition path in startup sequence. <br> - Enforce sealed-boundary behavior and hold criteria during pressurization window. <br> - Add transition telemetry for deterministic state auditing. | PASS if scenario shows ordered startup transitions that include pressurize/hold state, sealed behavior active during hold, and transition exit only after defined criteria met. FAIL if state is skipped, prematurely exited, or non-deterministic across reruns. | Depends on closure behavior from `CS-0014/15/18` (already closed) remaining intact. Regression risk in startup timing and state handoff logic. |
| `CS-0019` | Secondary temperature cannot progress toward high-pressure saturation region | High | `CLOSED` (`Governance/IssueRegister/issue_register.json`) | Legacy register detection: heatup log review 0-16.50 hr and source `ISSUE-SG-006` (`Updates/IssueRegister_DEPRECATED.md`). | `SGMultiNodeThermal` | - Correct pressure-temperature coupling so Tsat increases with pressurization path. <br> - Align boiling/phase progression with pressure rise in startup envelope. <br> - Instrument Tsat and secondary temperature progression checkpoints. | PASS if isolated startup run demonstrates secondary progression away from low-Tsat lock region toward high-pressure saturation trajectory (with monotonic pressure-linked Tsat rise over designated interval). FAIL if Tsat remains effectively clamped near low-pressure values under pressurization conditions. | Blocked by incorrect pressurization behavior (`CS-0017`). Risk of over-correction causing unrealistic heating rate; requires bounded thermodynamic checks. |
| `CS-0020` | Secondary remains largely inert or wrongly bounded during primary heatup | Medium | `CLOSED` (`Governance/IssueRegister/issue_register.json`) | Legacy register detection: heatup log review 0-16.50 hr and consolidated sources `ISSUE-LOG-005/010/015` (`Updates/IssueRegister_DEPRECATED.md`). | `SGMultiNodeThermal` / `HeatupSimEngine` | - Remove residual clamp/inert behaviors that suppress SG response after pressurization path corrections. <br> - Re-balance coupling response so SG pressure/temperature respond to primary heat and RCP state changes. <br> - Add scenario assertions for dynamic response across startup intervals. | PASS if multi-interval startup run shows SG pressure/temperature trends responding to primary-side heat and RCP transitions within expected directionality and range bands. FAIL if SG remains flat/inert or hard-clamped through active coupling intervals. | Dependent on `CS-0017` and `CS-0019` completion. Risk that unresolved validation gaps (`CS-0009`) mask true dynamics without improved checks. |
| `CS-0054` | DP-0003 Stage E failure: SG secondary pressure flatline under active heat input | High | `CLOSED` (`Governance/IssueRegister/issue_register.json`) | Stage E failure evidence: `Updates/Issues/IP-0018_StageE_Validation_2026-02-14_184930.md` (`stageE_DynamicPressureFlatline3Count = 5`). | `HeatupSimEngine` dynamic-response telemetry/windowing and SG pressure coupling path | - Isolate exact failing windows with interval-level pressure and heat-input traces. <br> - Determine artifact vs physics cause (quantization/windowing/state-gating/update-order). <br> - Implement smallest reversible correction driving flatline count to zero without regressing CS-0009/0017/0019. | PASS if Stage E rerun reports `stageE_DynamicPressureFlatline3Count = 0` for active heat intervals (`PrimaryHeatInput_MW > 1.0`) while CS-0009, CS-0017, and CS-0019 remain PASS. FAIL if flatline count is non-zero or any previously passing CS regresses. | Blocks DP-0003 acceptance. Risk of overfitting telemetry logic instead of fixing coupling path; requires regression confirmation against CS-0009/0017/0019 thresholds. |

## 4. Implementation Steps (ordered)
1. Baseline capture and guard setup (`CS-0009`, `CS-0017`, `CS-0019`, `CS-0020`): lock current startup scenario inputs, required telemetry fields, and acceptance thresholds so later deltas are measurable and reversible.
2. Startup state machine correction package (`CS-0017`): implement SG pressurize/hold state entry, hold, and exit criteria with explicit transition logging.
3. Pressure-temperature progression package (`CS-0019`): update SG secondary pressure/Tsat coupling behavior for high-pressure saturation trajectory during pressurization/early boil windows.
4. Dynamic-response correction package (`CS-0020`): remove remaining inert/clamped behavior and align SG response with primary heat and RCP operating state.
5. Package 4b/7 - SG pressure-response flatline correction (`CS-0054`): correct active-heating pressure-response behavior/measurement so rolling 3-interval flatline windows are eliminated with traceable telemetry evidence.
6. SG energy validation package (`CS-0009`): add runtime SG energy balance checks and cumulative mismatch diagnostics aligned to Stage E criteria.
7. Integrated regression and acceptance run package (`CS-0009`, `CS-0017`, `CS-0019`, `CS-0020`, `CS-0054`): execute Stage A-E gates and capture evidence artifacts per CS.

Commit strategy note: execute each package as a small, reversible commit, and gate progression to the next step only after package-level validation passes.

## 5. Validation Plan (Stage Gates)
### Stage A: Build/compile sanity
PASS means project compiles cleanly with no new errors in modified SG and validation paths, and simulation startup launches with required telemetry enabled.

### Stage B: Static checks (lint/analyzers if applicable)
PASS means configured analyzers/lint checks report no new warnings/errors in touched files, and no new high-risk diagnostics are introduced.

### Stage C: Targeted unit/component checks (if present)
PASS means targeted checks for SG secondary state transitions, pressure/Tsat calculations, and SG energy-validation routines execute successfully with deterministic outcomes.

### Stage D: Scenario/regression runs relevant to DP-0003
PASS means DP-0003 startup scenarios (including isolated pressurization windows and heatup progression intervals) complete with reproducible trend outputs and no regressions against closed IP-0015 acceptance behavior.

### Stage E: Full acceptance criteria for each CS (explicit pass/fail)
#### Stage E Threshold Definition Amendment — 2026-02-14
- `CS-0017` PASS: pressurize state persists for `>=2` consecutive intervals; hold state persists for `>=2` consecutive intervals; hold-state pressure remains within `+/-3%` of hold-entry pressure for all hold intervals; sealed-boundary leakage remains `<=0.1%` net SG secondary mass change per hold interval and `<=0.2%` cumulative over the hold segment. FAIL if state is skipped, hold duration is `<2` intervals, pressure drift exceeds `+/-3%`, or leakage exceeds either mass threshold.
- `CS-0019` PASS: during the pressurization window, SG secondary pressure is monotonic increasing across `>=3` consecutive intervals (`P[i+1] > P[i]`), Tsat also increases across those same intervals (no flat-line), and SG secondary temperature reaches within `15 F` of Tsat before boiling transition. FAIL if Tsat remains within `5 F` of its pressurization-window initial value, or if pressure oscillates with no net gain (`P[end] - P[start] <= 0`) over the defined pressurization window.
- `CS-0020` PASS: during active primary heatup intervals (`PrimaryHeatInput_MW > 1.0`), SG secondary pressure increases whenever primary heat input increases by `>=2%` between intervals; SG secondary temperature change over any rolling `3` intervals under active heating is `>5 F`; no hard clamp keeps SG secondary temperature more than `50 F` below Tsat unless SG is in the defined hold state. FAIL if SG secondary temperature change is `<2 F` over `3` intervals during active heat input, or if SG pressure flatlines (`|DeltaP| <= 0.1 psia` across `3` intervals) while `PrimaryHeatInput_MW > 1.0`.
- `CS-0009` PASS: instantaneous SG heat removal is never negative (`SGHeatRemoval_MW >= 0` for every interval); instantaneous SG heat removal never exceeds primary heat input by more than `5%` (`SGHeatRemoval_MW <= 1.05 * PrimaryHeatInput_MW` for every interval); cumulative startup-window energy mismatch remains within `+/-2%` of integrated primary energy input (`PercentMismatch = 100 * (TotalSGEnergyRemoved_MJ - TotalPrimaryEnergy_MJ) / TotalPrimaryEnergy_MJ`, and `-2.0 <= PercentMismatch <= 2.0`); validation artifact logs all of `TotalPrimaryEnergy_MJ`, `TotalSGEnergyRemoved_MJ`, and `PercentMismatch`. FAIL on any threshold violation or missing required artifact field.
- `CS-0054` PASS: flatline criterion from CS-0020 must be zero in Stage E final report (`stageE_DynamicPressureFlatline3Count = 0`) for active heat intervals (`PrimaryHeatInput_MW > 1.0`). FAIL if `stageE_DynamicPressureFlatline3Count > 0` at any value.

## 6. Rollback Plan
- Revert by work-package commit in reverse order (`Step 5` to `Step 2`) to isolate regressions without discarding validated earlier corrections.
- Keep baseline scenario input set and telemetry schema unchanged so rollback validation is directly comparable.
- If integrated regressions appear, roll back only the latest package and rerun Stage C-D before broader rollback.

## 7. Open Questions / Blockers
- None remaining for IP-0018 execution closeout; all Stage A-E gates completed with PASS evidence and CS items moved to `CLOSED`.

## 8. Change Log Placeholder
Changelog/versioning updates are intentionally deferred until implementation and full validation complete.

## Execution Summary
- Package commits executed:
  - `d9c9f12` (package 1)
  - `1f4e99d` (package 2 / CS-0017)
  - `cb8c557` (package 3 / CS-0019)
  - `0bff5de` (package 4 / CS-0020)
  - `aef7611` (package 5 / CS-0009)
  - `90aeec4` (package 6 / Stage E threshold runner)
  - `9806096` (package 7 / CS-0054)
- Stage E final evidence: `Updates/Issues/IP-0018_StageE_Validation_2026-02-14_191442.md`
- Final PASS table:

| CS ID | Result |
|---|---|
| `CS-0009` | PASS |
| `CS-0017` | PASS |
| `CS-0019` | PASS |
| `CS-0020` | PASS |
| `CS-0054` | PASS |



