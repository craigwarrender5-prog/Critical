---
Identifier: DP-0009
Domain (Canonical): Performance & Runtime
Status: Open
Linked Issues: CS-0032, CS-0044, CS-0045, CS-0046, CS-0065, CS-0066, CS-0067, CS-0068
Last Reviewed: 2026-02-14
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# DP-0009 - Performance & Runtime

## A) Domain Summary
- Canonical Domain: Performance & Runtime
- DP Status: Open
- Total CS Count in Domain: 8

## B) Severity Distribution
| Severity | Count |
|---|---:|
| Critical | 1 |
| High | 2 |
| Medium | 2 |
| Low | 3 |

## C) Ordered Issue Backlog
| CS ID | Title | Severity | Status | Blocking Dependency | Validation Outcome |
|---|---|---|---|---|---|
| CS-0032 | UI/Input unresponsive after Editor close (main thread starvation suspected) | Critical | READY_FOR_FIX | - | Pending (active issue) |
| CS-0065 | PERF THREAD_BLOCKING_RISK: synchronous interval file writes execute on main simulation thread | High | READY_FOR_FIX | - | Pending (active issue) |
| CS-0066 | PERF ALLOCATION_HOT_PATH: SG multi-node update allocates transient mixing array each step | High | READY_FOR_FIX | - | Pending (active issue) |
| CS-0067 | PERF GC_PRESSURE_RISK: Stage E dynamic window analysis allocates via Queue.ToArray in recurring path | Medium | READY_FOR_FIX | - | Pending (active issue) |
| CS-0068 | PERF LOGGING_SPAM: high-frequency Debug.Log calls embedded in simulation substep paths | Medium | READY_FOR_FIX | - | Pending (active issue) |
| CS-0044 | Async log writer â€” move synchronous file I/O to background thread | Low | READY_FOR_FIX | - | Pending (active issue) |
| CS-0045 | Physics snapshot boundary â€” decouple physics compute from UI rendering | Low | READY_FOR_FIX | - | Pending (active issue) |
| CS-0046 | Physics parallelization â€” move simulation step to worker thread or Unity Jobs | Low | READY_FOR_FIX | - | Pending (active issue) |

## D) Execution Readiness Indicator
**READY FOR AUTHORIZATION**

No blocking Critical issues unresolved outside domain.

## E) Notes / Investigation Links
- Registry consistency synchronized against Governance/IssueRegister/issue_index.json and Governance/IssueRegister/issue_register.json on 2026-02-14.
