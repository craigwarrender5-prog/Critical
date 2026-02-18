# IP Execution Sequence (2026-02-18)

## Purpose
Priority order for implementation plans covering all Domain Plans with open `CS-*` scope in the authoritative register.

Scope source:
- `Governance/IssueRegister/issue_index.json`
- Open CS domains: `DP-0001`, `DP-0012`, `DP-0008`

## Candidate IP Set
- `IP-0054` (`DP-0001`) - No-RCP Thermal Coupling Fidelity
- `IP-0052` (`DP-0012`) - Mode 5 Pre-Heater Pressurization Policy Alignment
- `IP-0053` (`DP-0008`) - Scenario Runtime Accessibility and Dashboard Visual Correctness

## Priority Order (Severity + Impact + Blocking)
1. `IP-0054` (`DP-0001`)
Reason: Contains `CS-0122` (`HIGH`) with direct primary-thermal fidelity impact. It affects no-RCP baseline behavior and can distort downstream startup policy validation if left unresolved.

2. `IP-0052` (`DP-0012`)
Reason: Contains `CS-0109` (`HIGH`) startup-control policy mismatch. Should follow `IP-0054` so Mode 5 policy validation is performed on corrected primary thermal coupling behavior.

3. `IP-0053` (`DP-0008`)
Reason: Contains one `HIGH` (`CS-0102`) plus medium/low scenario UX and visual correctness work (`CS-0103`, `CS-0120`, `CS-0121`), but overall impact is interaction/display and lower than thermal/policy correctness fixes.

## Intra-IP Dependency Ordering
### `IP-0054`
- `CS-0122` only (single-item critical path).

### `IP-0052`
- `CS-0109` only (single-item critical path).

### `IP-0053`
1. `CS-0102`
2. `CS-0103` (depends on `CS-0102`)
3. `CS-0120` (depends on finalized selector path behavior)
4. `CS-0121` (independent low-risk dashboard visual logic)

## Blocking Rules
- `IP-0054` should execute first to establish corrected no-RCP thermal baseline.
- `IP-0052` Stage D/E closeout should not finalize until `IP-0054` thermal baseline correction is validated.
- Within `IP-0053`, `CS-0102` remains prerequisite to `CS-0103` closeout.

## Parallelization Guidance
- No cross-IP parallelization at initial start.
- `IP-0053` may begin Stage A/B planning in parallel with `IP-0052` execution, but Stage C-E should follow completion of `IP-0054` and `IP-0052` to avoid churn from baseline shifts.

## Exit Criteria
- All open CS items in `DP-0001`, `DP-0012`, and `DP-0008` transition from `READY` to closed dispositions with stage/report evidence.
- Registry and domain-plan parity remains intact (`issue_index.json`, `issue_register.json`, DP ledgers, and IP sequence).
