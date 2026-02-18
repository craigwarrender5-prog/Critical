---
Identifier: DP-0012
Domain (Canonical): Pressurizer & Startup Control
Status: Open
Linked Issues: CS-0113, CS-0114, CS-0125
Last Reviewed: 2026-02-18
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# DP-0012 - Pressurizer & Startup Control

## A) Domain Summary
- Canonical Domain: Pressurizer & Startup Control
- DP Status: Open
- Total CS Count in Domain: 3 (active)
- Last Completed IP: IP-0052 (CS-0109)

## B) Severity Distribution
| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 0 |
| Medium | 3 |
| Low | 0 |

## C) Ordered Issue Backlog
| CS ID | Title | Severity | Status | Blocking Dependency | Validation Outcome |
|---|---|---|---|---|---|
| CS-0114 | Pressurizer system audit - PressurizerPhysics engineering-basis and derivative fidelity gaps | Medium | READY | - | Investigation complete; physics-governance remediation pending |
| CS-0113 | Pressurizer system audit - PlantConstants.Pressurizer parameter governance gaps | Medium | READY | CS-0114 (recommended sequencing for shared authority paths) | Investigation complete; constants-governance remediation pending |
| CS-0125 | Rotary selector switch UI control plan viability and integration risk assessment | Medium | DEFERRED | - | On hold by user; clarified as PZR heater mode control (OFF = heaters off with simulation continuing, AUTO = current behavior, MANUAL = future) |

## D) Execution Readiness Indicator
**READY FOR AUTHORIZATION (REOPENED BY ORPHAN-ID TRIAGE)**

Two medium-severity pressurizer-governance issues were restored from orphan investigation artifacts and remain active, and one medium-severity heater-mode UI/control item is deferred.
Recommended sequence: remediate `CS-0114` first (physics derivative/ambient authority), then finalize constant/comment/citation reconciliation in `CS-0113`; resume `CS-0125` after heater authority contract confirmation.

## E) Recently Closed Issues
| CS ID | Title | Closed | Resolution | IP |
|---|---|---|---|---|
| CS-0109 | PZR Mode 5 pre-heater pressurization: validate net +1 gpm charging imbalance vs documented expectations | 2026-02-18 | FIXED | IP-0052 |
| CS-0098 | Heaters do not start after startup-hold release during cold-start sequence | 2026-02-17 | FIXED | IP-0048 |

## F) Notes / Investigation Links
- Registry consistency synchronized against `Governance/IssueRegister/issue_index.json` and `Governance/IssueRegister/issue_register.json` on 2026-02-18.
- Active investigation artifacts:
  - `Governance/Issues/CS-0113_PZR_Audit_PlantConstants_Pressurizer_2026-02-17.md`
  - `Governance/Issues/CS-0114_PZR_Audit_PressurizerPhysics_2026-02-17.md`
  - `Governance/Issues/CS-0125_Investigation_Report_2026-02-18_045211.md`
- Reopen trigger: `Governance/Issues/IssueRegister_Integrity_Audit_2026-02-18_044114.md` orphan-ID follow-up triage.
