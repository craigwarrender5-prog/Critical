---
Identifier: DP-0012
Domain (Canonical): Pressurizer & Startup Control
Status: Open
Linked Issues: CS-0109
Last Reviewed: 2026-02-18
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# DP-0012 - Pressurizer & Startup Control

## A) Domain Summary
- Canonical Domain: Pressurizer & Startup Control
- DP Status: Open
- Total CS Count in Domain: 1 (active)
- Last Completed IP: IP-0048 (CS-0098)

## B) Severity Distribution
| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 1 |
| Medium | 0 |
| Low | 0 |

## C) Ordered Issue Backlog
| CS ID | Title | Severity | Status | Blocking Dependency | Validation Outcome |
|---|---|---|---|---|---|
| CS-0109 | PZR Mode 5 pre-heater pressurization: validate net +1 gpm charging imbalance vs documented expectations | High | READY | - | Investigation complete; pending remediation IP |

## D) Execution Readiness Indicator
**AUTHORIZED FOR IP-0052 EXECUTION PREP**

Single READY issue assigned.  
Implementation plan created: `Governance/ImplementationPlans/IP-0052 - DP-0012 - Mode 5 Pre-Heater Pressurization Policy Alignment.md`.  
Recommended sequencing dependency: complete `IP-0054` thermal-baseline correction before final IP-0052 Stage D/E closeout.

## E) Recently Closed Issues
| CS ID | Title | Closed | Resolution | IP |
|---|---|---|---|---|
| CS-0098 | Heaters do not start after startup-hold release during cold-start sequence | 2026-02-17 | FIXED | IP-0048 |

## F) Notes / Investigation Links
- Registry consistency synchronized against `Governance/IssueRegister/issue_index.json` and `Governance/IssueRegister/issue_register.json` on 2026-02-18.
- Investigation artifact (open): `Governance/Issues/CS-0109_Investigation_Report_2026-02-17_190515.md`.
- Successor continuity references: `Governance/Issues/CS-0098_Investigation_Report_2026-02-16_214500.md`, `Governance/ImplementationPlans/Closed/IP-0048/IP-0048.md`.
