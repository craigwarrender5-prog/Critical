---
Identifier: DP-0008
Domain (Canonical): Operator Interface & Scenarios
Status: Open
Linked Issues: CS-0102, CS-0103, CS-0120, CS-0121
Last Reviewed: 2026-02-18
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# DP-0008 - Operator Interface & Scenarios

## A) Domain Summary
- Canonical Domain: Operator Interface & Scenarios
- DP Status: Open
- Total CS Count in Domain: 4 (active)
- Last Completed IP: IP-0051 (CS-0108, CS-0111)

## B) Severity Distribution
| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 1 |
| Medium | 1 |
| Low | 2 |

## C) Ordered Issue Backlog
| CS ID | Title | Severity | Status | Blocking Dependency | Validation Outcome |
|---|---|---|---|---|---|
| CS-0102 | Establish scenario system framework with registry and scenario abstraction | High | READY | - | Pending |
| CS-0103 | Add in-simulator scenario selection overlay with keybind trigger | Medium | READY | CS-0102 | Pending |
| CS-0120 | F2 scenario selector keybind only functional in Validator view, not Operator Screens view | Low | READY | - | Pending |
| CS-0121 | Dashboard visual issues: SOLID PZR indicator not lit, alarm symbol incorrectly transcoded | Low | READY | - | Pending |

## D) Execution Readiness Indicator
**AUTHORIZED FOR IP-0053 EXECUTION PREP**

Open backlog includes scenario framework work (CS-0102/CS-0103) and two runtime UI behavior items (CS-0120/CS-0121).  
Implementation plan created: `Governance/ImplementationPlans/IP-0053 - DP-0008 - Scenario Runtime Accessibility and Solid PZR Indicator Correctness.md`.
Execution priority is after high-impact thermal/policy plans (`IP-0054`, `IP-0052`).

## E) Recently Closed Issues
| CS ID | Title | Closed | Resolution | IP |
|---|---|---|---|---|
| CS-0119 | F2 scenario selection overlay missing for runtime scenario start from Cold Shutdown baseline | 2026-02-18 | FIXED | IP-0049 |
| CS-0108 | PZR temperature monitoring missing for bubble formation readiness during cold startup | 2026-02-18 | FIXED | IP-0051 |
| CS-0111 | PZR temperature (T_pzr) lacks primary visualization (no arc gauge or trend display) | 2026-02-18 | FIXED | IP-0051 |

## F) Notes / Investigation Links
- Registry consistency synchronized against `Governance/IssueRegister/issue_index.json` and `Governance/IssueRegister/issue_register.json` on 2026-02-18.
- CS-0037 (Surge line flow direction) was closed as INVALID on 2026-02-15.
- CS-0077 (HeatupValidationVisual redesign) was closed as FAILED on 2026-02-15 (replaced by IP-0043 ValidationDashboard rebuild).
- CS-0042 was withdrawn on 2026-02-15 (resolution: INVALID).
- Investigation artifacts: `Governance/Issues/CS-0102_Investigation_Report_2026-02-17_124730.md`, `Governance/Issues/CS-0103_Investigation_Report_2026-02-17_124730.md`, `Governance/Issues/CS-0120_Investigation_Report_2026-02-18_110000.md`, `Governance/Issues/CS-0121_Investigation_Report_2026-02-18_111500.md`.
