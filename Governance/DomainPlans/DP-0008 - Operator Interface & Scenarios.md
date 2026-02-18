---
Identifier: DP-0008
Domain (Canonical): Operator Interface & Scenarios
Status: Open
Linked Issues: CS-0127, CS-0118, CS-0121
Last Reviewed: 2026-02-18
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# DP-0008 - Operator Interface & Scenarios

## A) Domain Summary
- Canonical Domain: Operator Interface & Scenarios
- DP Status: Open
- Total CS Count in Domain: 3 (active)
- Last Completed IP: IP-0051 (CS-0108, CS-0111)

## B) Severity Distribution
| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 1 |
| Medium | 1 |
| Low | 1 |

## C) Ordered Issue Backlog
| CS ID | Title | Severity | Status | Blocking Dependency | Validation Outcome |
|---|---|---|---|---|---|
| CS-0127 | Validation Dashboard Complete Overhaul Using Unity UI Toolkit | High | READY | - | Primary overhaul scope; supersession candidate owner for CS-0118 and CS-0121 under IP-0056 |
| CS-0118 | Validation dashboard missing condenser/feedwater telemetry coverage | Medium | READY | - | Investigation complete; dashboard telemetry integration pending |
| CS-0121 | Dashboard visual issues: SOLID PZR indicator not lit, alarm symbol incorrectly transcoded | Low | READY | - | Pending |

## D) Execution Readiness Indicator
**READY FOR AUTHORIZATION - IP-0056 EXECUTION CANDIDATE**

Open backlog now includes one active high umbrella overhaul scope (`CS-0127`) plus two subordinate items (`CS-0118`, `CS-0121`).
`IP-0056` defines explicit supersession policy: `CS-0118` and `CS-0121` remain open until Stage D/E evidence confirms full coverage by `CS-0127`.

## E) Recently Closed Issues
| CS ID | Title | Closed | Resolution | IP |
|---|---|---|---|---|
| CS-0120 | F2 scenario selector keybind only functional in Validator view, not Operator Screens view | 2026-02-18 | FIXED | IP-0053 |
| CS-0102 | Establish scenario system framework with registry and scenario abstraction | 2026-02-18 | FIXED | IP-0053 |
| CS-0103 | Add in-simulator scenario selection overlay with keybind trigger | 2026-02-18 | FIXED | IP-0053 |
| CS-0119 | F2 scenario selection overlay missing for runtime scenario start from Cold Shutdown baseline | 2026-02-18 | FIXED | IP-0049 |
| CS-0108 | PZR temperature monitoring missing for bubble formation readiness during cold startup | 2026-02-18 | FIXED | IP-0051 |
| CS-0111 | PZR temperature (T_pzr) lacks primary visualization (no arc gauge or trend display) | 2026-02-18 | FIXED | IP-0051 |

## F) Notes / Investigation Links
- Registry consistency synchronized against `Governance/IssueRegister/issue_index.json` and `Governance/IssueRegister/issue_register.json` on 2026-02-18.
- CS-0037 (Surge line flow direction) was closed as INVALID on 2026-02-15.
- CS-0077 (HeatupValidationVisual redesign) was closed as FAILED on 2026-02-15 (replaced by IP-0043 ValidationDashboard rebuild).
- CS-0042 was withdrawn on 2026-02-15 (resolution: INVALID).
- Investigation artifacts:
  - `Governance/Issues/CS-0127_ValidationDashboard_UIToolkit_Overhaul_2026-02-18.md`
  - `Governance/Issues/CS-0118_Investigation_Report_2026-02-18_103000.md`
  - `Governance/Issues/CS-0121_Investigation_Report_2026-02-18_111500.md`
  - `Governance/ImplementationPlans/IP-0056 - DP-0008 - Validation Dashboard UI Toolkit Overhaul and Backlog Consolidation.md`
  - `Governance/ImplementationPlans/IP-0053/Reports/IP-0053_StageD_DomainValidation_2026-02-18_050900.md`
  - `Governance/ImplementationPlans/IP-0053/Reports/IP-0053_StageE_SystemRegression_2026-02-18_050900.md`
