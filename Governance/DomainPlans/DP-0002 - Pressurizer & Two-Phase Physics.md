---
Identifier: DP-0002
Domain (Canonical): Pressurizer & Two-Phase Physics
Status: Open
Linked Issues: CS-0024, CS-0025, CS-0026, CS-0027, CS-0028, CS-0029, CS-0030, CS-0036, CS-0043, CS-0049, CS-0059, CS-0069, CS-0072, CS-0074, CS-0075
Last Reviewed: 2026-02-15
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# DP-0002 - Pressurizer & Two-Phase Physics

## A) Domain Summary
- Canonical Domain: Pressurizer & Two-Phase Physics
- DP Status: Open
- Total CS Count in Domain: 15

## B) Severity Distribution
| Severity | Count |
|---|---:|
| Critical | 2 |
| High | 7 |
| Medium | 5 |
| Low | 1 |

## C) Ordered Issue Backlog
| CS ID | Title | Severity | Status | Blocking Dependency | Validation Outcome |
|---|---|---|---|---|---|
| CS-0043 | Pressurizer pressure boundary failure during bubble formation â€” runaway depressurization spiral | Critical | READY_FOR_FIX | - | Pending (active issue) |
| CS-0049 | Pressurizer does not recover pressure in two-phase condition under heater pressurize mode | Critical | READY_FOR_FIX | - | Pending (active issue) |
| CS-0026 | Post-bubble pressure escalation magnitude questionable | High | READY_FOR_FIX | - | Pending (active issue) |
| CS-0028 | Bubble flag/state timing inconsistent with saturation and pressure rise | High | READY_FOR_FIX | - | Pending (active issue) |
| CS-0029 | Very high pressure ramp while RCS heat rate near zero | High | READY_FOR_FIX | - | Pending (active issue) |
| CS-0036 | DRAIN phase duration excessive â€” PZR requires ~4 hours to reach 25% level | High | READY_FOR_FIX | - | Pending (active issue) |
| CS-0059 | BubbleFormation initializes CVCS controller during UPDATE phase transition | High | READY_FOR_FIX | - | Pending (active issue) |
| CS-0072 | Validation failure: Bubble draw duration exceeds documented expectation (~165â€“180 min observed vs 30â€“60 min expected) | High | INVESTIGATING | - | Pending (active issue) |
| CS-0074 | Steam displacement declared primary, but DRAIN volumetric reconciliation path de-emphasizes steam mass-to-volume closure | High | INVESTIGATING | - | Pending (active issue) |
| CS-0024 | PZR 100% level, zero steam model may be clamping dynamics | Medium | READY_FOR_FIX | - | Pending (active issue) |
| CS-0027 | Bubble phase labeling inconsistent with observed thermodynamics | Medium | READY_FOR_FIX | - | Pending (active issue) |
| CS-0030 | Nonlinear/inconsistent sensitivity of pressure to CVCS sign changes | Medium | READY_FOR_FIX | - | Pending (active issue) |
| CS-0069 | CondensingHTC uses unsourced coefficient and hard clamps in physics correlation | Medium | READY_FOR_FIX | - | Pending (active issue) |
| CS-0075 | 40-minute DRAIN constant is advisory only; state machine transition is level-threshold driven | Medium | INVESTIGATING | - | Pending (active issue) |
| CS-0025 | Bubble detection threshold aligns with saturation (validation item) | Low | READY_FOR_FIX | - | Pending (active issue) |

## D) Execution Readiness Indicator
**READY FOR AUTHORIZATION**

No blocking Critical issues unresolved outside domain.

## E) Notes / Investigation Links
- Registry consistency synchronized against Governance/IssueRegister/issue_index.json and Governance/IssueRegister/issue_register.json on 2026-02-15.
- 2026-02-15 preliminary investigation for CS-0072/CS-0074/CS-0075 is tracked directly under this approved plan (no non-canonical DP identifiers).

## F) Authoritative Validation Gates (CS-0072/0074/0075)
1. DRAIN start to COMPLETE must be <= 60 minutes (PASS), <= 40 minutes nominal (ADVISORY).
2. During DRAIN, pressure must remain in startup band while level trends toward ~25%.
3. DRAIN progression must not be dominated solely by CVCS net outflow when displacement is intended primary.

## G) Preliminary Findings (No Code Changes)
- DRAIN duration overrun confirmed in `IP-0019_Extended_20260215_081706`: ~165 min from 90.6% to 25.5% (`Heatup_Interval_094_8.50hr.txt`, `Heatup_Interval_105_11.25hr.txt`).
- `BUBBLE_PHASE_DRAIN_HR` exists but does not gate transition; DRAIN exits on level threshold (`Assets/Scripts/Physics/PlantConstants.Pressurizer.cs:155`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:512`).
- DRAIN path uses mass-first water-volume derivation and disables steam-mass reconciliation in this path (`Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:456`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:459`).

## H) Smallest Conceptual Fix Options
1. Add explicit duration gate to DRAIN transition validation.
2. Add displacement-vs-CVCS contribution telemetry as a closure requirement.
3. Require duration + pressure-band + level consistency jointly for DRAIN exit readiness.
