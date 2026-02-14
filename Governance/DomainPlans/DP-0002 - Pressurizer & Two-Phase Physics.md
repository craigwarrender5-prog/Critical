---
Identifier: DP-0002
Domain (Canonical): Pressurizer & Two-Phase Physics
Status: Open
Linked Issues: CS-0043, CS-0049, CS-0026, CS-0028, CS-0029, CS-0036, CS-0024, CS-0027, CS-0030, CS-0025
Last Reviewed: 2026-02-14
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# DP-0002 - Pressurizer & Two-Phase Physics

## A) Domain Summary
- Canonical Domain: Pressurizer & Two-Phase Physics
- DP Status: Open
- Total CS Count in Domain: 10

## B) Severity Distribution
| Severity | Count |
|---|---:|
| Critical | 2 |
| High | 4 |
| Medium | 3 |
| Low | 1 |

## C) Ordered Issue Backlog
| CS ID | Title | Severity | Status | Blocking Dependency | Validation Outcome |
|---|---|---|---|---|---|
| CS-0043 | Pressurizer pressure boundary failure during bubble formation - runaway depressurization spiral | Critical | Assigned | - | Stage E FAILED - Pressure boundary collapse confirmed in interval logs at 8.00-10.00 hr sim time |
| CS-0049 | Pressurizer does not recover pressure in two-phase condition under heater pressurize mode | Critical | Assigned | - | Observed failure confirmed in evidence set; preliminary investigation complete. |
| CS-0026 | Post-bubble pressure escalation magnitude questionable | High | Assigned | - | Not Tested |
| CS-0028 | Bubble flag/state timing inconsistent with saturation and pressure rise | High | Assigned | - | Not Tested |
| CS-0029 | Very high pressure ramp while RCS heat rate near zero | High | Assigned | - | Not Tested |
| CS-0036 | DRAIN phase duration excessive - PZR requires ~4 hours to reach 25% level | High | Assigned | - | Not Tested |
| CS-0024 | PZR 100% level, zero steam model may be clamping dynamics | Medium | Assigned | - | Resolved v0.3.0.0 - Investigation confirms behavior is physically correct. Log evidence: Surge Flow rises monotonically (1.2->8.2 gpm) during solid ops, confirming thermal expansion exits through surge line. PZR level at 100% with zero steam is the correct initial condition for solid plant. No code change required. |
| CS-0027 | Bubble phase labeling inconsistent with observed thermodynamics | Medium | Assigned | - | Not Tested |
| CS-0030 | Nonlinear/inconsistent sensitivity of pressure to CVCS sign changes | Medium | Assigned | - | Not Tested |
| CS-0025 | Bubble detection threshold aligns with saturation (validation item) | Low | Assigned | - | Resolved v0.3.0.0 - Investigation confirms detection threshold is correct. Log evidence: T_pzr = 435.43F at detection vs T_sat = 435.83F (0.4F margin). Pressure continuous through detection (365.1 psia before and after). No code change required. |

## D) Execution Readiness Indicator
**READY FOR AUTHORIZATION**

No blocking Critical issues unresolved outside domain.

## E) Notes / Investigation Links
- Investigation Reports:
  - Updates/Issues/CS-0043_Investigation_Report.md
  - Updates/Issues/CS-0049_Investigation_Report.md
- Prior IP references:
  - IP-0003 - Bubble Formation and Two-Phase
  - IP-0003 - Bubble Formation and Two-Phase - Phase B
- Validation evidence references:
  - CS-0024: Resolved v0.3.0.0 - Investigation confirms behavior is physically correct. Log evidence: Surge Flow rises monotonically (1.2->8.2 gpm) during solid ops, confirming thermal expansion exits through surge line. PZR level at 100% with zero steam is the correct initial condition for solid plant. No code change required.
  - CS-0025: Resolved v0.3.0.0 - Investigation confirms detection threshold is correct. Log evidence: T_pzr = 435.43F at detection vs T_sat = 435.83F (0.4F margin). Pressure continuous through detection (365.1 psia before and after). No code change required.
  - CS-0043: Stage E FAILED - Pressure boundary collapse confirmed in interval logs at 8.00-10.00 hr sim time
