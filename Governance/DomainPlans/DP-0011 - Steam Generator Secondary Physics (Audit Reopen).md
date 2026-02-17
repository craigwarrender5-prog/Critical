---
Identifier: DP-0011
Domain (Canonical): Steam Generator Secondary Physics
Status: Open
Linked Issues: CS-0057, CS-0078, CS-0115
Last Reviewed: 2026-02-17
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# DP-0011 - Steam Generator Secondary Physics (Audit Reopen)

## A) Domain Summary
- Canonical Domain: Steam Generator Secondary Physics
- DP Status: Open
- Total CS Count in Domain: 3

## B) Severity Distribution
| Severity | Count |
|---|---:|
| Critical | 1 |
| High | 2 |
| Medium | 0 |
| Low | 0 |

## C) Ordered Issue Backlog
| CS ID | Title | Severity | Status | Blocking Dependency | Validation Outcome |
|---|---|---|---|---|---|
| CS-0115 | Missing condenser + feedwater return module and startup permissive boundary contract (C-9/P-12) for steam-dump integration | Critical | READY | - | Pending (new blocker) |
| CS-0078 | SG secondary pressure response does not begin at RCP circulation onset; rise is delayed until near-boiling | High | BLOCKED | CS-0115 | Validation fail persists (blocked pending CS-0115) |
| CS-0057 | SG startup draining contract at ~200F is not wired into runtime execution path | High | READY | - | Pending (active issue) |

## D) Execution Readiness Indicator
**BLOCKED BY CRITICAL DEPENDENCY**

CS-0115 is a critical unresolved domain blocker and currently gates CS-0078 closure/remediation sequencing.

## E) Notes / Investigation Links
- Registry consistency synchronized against Governance/IssueRegister/issue_index.json and Governance/IssueRegister/issue_register.json on 2026-02-17.
