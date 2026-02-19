---
Identifier: DP-0001
Domain (Canonical): Primary Thermodynamics
Status: Open
Linked Issues: CS-0128
Last Reviewed: 2026-02-18
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# DP-0001 - Primary Thermodynamics

## A) Domain Summary
- Canonical Domain: Primary Thermodynamics
- DP Status: Open
- Total CS Count in Domain: 1 (active)
- Last Completed IP: IP-0054 (CS-0122)

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
| CS-0128 | No-RCP RHR pressurization path over-couples thermal pump energy into bulk RCS during Mode 5 startup | High | READY | Follow-on to CS-0122 closure evidence (recommended) | Investigation complete; no-RCP RHR fidelity envelope definition and validation pending |

## D) Execution Readiness Indicator
**READY FOR AUTHORIZATION (HIGH PRIORITY FOLLOW-ON)**

`CS-0128` reopens DP-0001 with HIGH-priority no-RCP RHR thermal-coupling fidelity scope.  
This should be treated as follow-on work after `CS-0122` closeout to ensure no-flow startup pressure/temperature behavior remains physically bounded and traceable.

## E) Recently Closed Issues
| CS ID | Title | Closed | Resolution | IP |
|---|---|---|---|---|
| CS-0106 | Add RCS loop manager/aggregator supporting N loops with N=1 compatibility | 2026-02-17 | FIXED | IP-0045 |
| CS-0105 | Modularize current single-loop RCS into reusable RCSLoop prefab/module boundary | 2026-02-17 | FIXED | IP-0045 |
| CS-0080 | RCP heat-input constants do not align with cited cold-water heatup reference basis | 2026-02-17 | FIXED | IP-0045 |
| CS-0122 | RCS temperature incorrectly rising during solid PZR heatup with no RCP flow | 2026-02-18 | FIXED | IP-0054 |

## F) Notes / Investigation Links
- Registry consistency synchronized against `Governance/IssueRegister/issue_index.json` and `Governance/IssueRegister/issue_register.json` on 2026-02-18.
- Closed investigation artifact: `Governance/Issues/CS-0122_Investigation_Report_2026-02-18_113000.md`.
- Active investigation artifact: `Governance/Issues/CS-0128_Investigation_Report_2026-02-18_203000.md`.
- Current closeout references: `Governance/ImplementationPlans/Closed/IP-0054/IP-0054.md`, `Governance/ImplementationPlans/Closed/IP-0054/Reports/IP-0054_Closeout_Traceability.md`.
