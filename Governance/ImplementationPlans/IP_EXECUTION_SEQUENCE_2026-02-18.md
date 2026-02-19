# IP Execution Sequence (2026-02-18)

## Purpose
Priority order for implementation plans covering all Domain Plans with open `CS-*` scope in the authoritative register.

Scope source:
- `Governance/IssueRegister/issue_index.json`
- Open CS domains: `DP-0001`, `DP-0012`

## Candidate IP Set
- `IP-0054` (`DP-0001`) - No-RCP Thermal Coupling Fidelity
- `IP-0052` (`DP-0012`) - Mode 5 Pre-Heater Pressurization Policy Alignment

## Priority Order (Severity + Impact + Blocking)
1. `IP-0054` (`DP-0001`)
Reason: Contains `CS-0122` (`HIGH`) with direct primary-thermal fidelity impact. It affects no-RCP baseline behavior and can distort downstream startup policy validation if left unresolved.

2. `IP-0052` (`DP-0012`)
Reason: Contains `CS-0109` (`HIGH`) startup-control policy mismatch. Should follow `IP-0054` so Mode 5 policy validation is performed on corrected primary thermal coupling behavior.

## Intra-IP Dependency Ordering
### `IP-0054`
- `CS-0122` only (single-item critical path).

### `IP-0052`
- `CS-0109` only (single-item critical path).

## Blocking Rules
- `IP-0054` should execute first to establish corrected no-RCP thermal baseline.
- `IP-0052` Stage D/E closeout should not finalize until `IP-0054` thermal baseline correction is validated.

## Parallelization Guidance
- No cross-IP parallelization at initial start.

## Exit Criteria
- All open CS items in `DP-0001` and `DP-0012` transition from `READY` to closed dispositions with stage/report evidence.
- Registry and domain-plan parity remains intact (`issue_index.json`, `issue_register.json`, DP ledgers, and IP sequence).
