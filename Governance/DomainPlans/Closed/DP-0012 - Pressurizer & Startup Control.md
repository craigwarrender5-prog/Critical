---
Identifier: DP-0012
Domain (Canonical): Pressurizer & Startup Control
Status: Closed
Linked Issues: CS-0040, CS-0081, CS-0091, CS-0093, CS-0094, CS-0096
Last Reviewed: 2026-02-16
Authorization Status: COMPLETE
Mode: EXECUTED/CLOSEOUT
Closure IP: IP-0028
Closure Date: 2026-02-16
---

# DP-0012 - Pressurizer & Startup Control

## A) Domain Summary
- Canonical Domain: Pressurizer & Startup Control
- DP Status: Closed
- Total CS Count in Domain: 6

## B) Severity Distribution
| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 4 |
| Medium | 2 |
| Low | 0 |

## C) Ordered Issue Backlog
| CS ID | Title | Severity | Status | Blocking Dependency | Validation Outcome |
|---|---|---|---|---|---|
| CS-0091 | PZR bubble closure path shows persistent non-convergence with large residuals and post-residual renormalization masking. | High | CLOSED | - | PASS (`Governance/Issues/IP-0028_StageD_PressurizerControlValidation_2026-02-16_131529.md`) |
| CS-0093 | Complete Pressurizer (PZR) System Remodel to Align with Technical_Documentation and Replace Current Heuristic Bubble Model | High | CLOSED | - | PASS (`Governance/Issues/IP-0028_StageD_PressurizerControlValidation_2026-02-16_131529.md`) |
| CS-0096 | PZR heaters capped after reaching operating pressure hold band | High | CLOSED | - | PASS (`Governance/Issues/IP-0028_StageC_StartupGovernance_2026-02-16_124518.md`) |
| CS-0040 | RVLIS indicator stale during PZR drain | High | CLOSED | - | PASS (`Governance/Issues/IP-0028_StageE_SystemRegression_2026-02-16_131703.md`) |
| CS-0081 | Solid-plant high pressure control band is configured above documented operating band. | Medium | CLOSED | - | PASS (`Governance/Issues/IP-0028_StageD_PressurizerControlValidation_2026-02-16_131529.md`) |
| CS-0094 | Add Cold-Start Stabilization Hold and PZR Heater Off/Auto/Manual Control to Prevent Immediate Pressurization and Improve Operator Realism | Medium | CLOSED | - | PASS (`Governance/Issues/IP-0028_StageC_StartupGovernance_2026-02-16_124518.md`) |

## D) Execution Readiness Indicator
**COMPLETE**

## E) Notes / Investigation Links
- Closure report: `Governance/ImplementationReports/IP-0028_Closeout_Report.md`
- Changelog: `Governance/Changelogs/CHANGELOG_v0.7.0.0.md`
