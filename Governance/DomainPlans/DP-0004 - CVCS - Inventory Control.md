---
Identifier: DP-0004
Domain (Canonical): CVCS / Inventory Control
Status: Open
Linked Issues: CS-0035, CS-0039, CS-0073, CS-0076
Last Reviewed: 2026-02-15
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# DP-0004 - CVCS / Inventory Control

## A) Domain Summary
- Canonical Domain: CVCS / Inventory Control
- DP Status: Open
- Total CS Count in Domain: 4

## B) Severity Distribution
| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 2 |
| Medium | 1 |
| Low | 1 |

## C) Ordered Issue Backlog
| CS ID | Title | Severity | Status | Blocking Dependency | Validation Outcome |
|---|---|---|---|---|---|
| CS-0039 | VCT conservation error growth (~1,700 gal at 15.75 hr) | High | READY_FOR_FIX | - | Pending (active issue) |
| CS-0073 | DRAIN progression is strongly constrained by fixed post-CCP net outflow policy (75 letdown, 44 charging, net 31 gpm) | High | INVESTIGATING | - | Pending (active issue) |
| CS-0076 | Procedure alignment risk: startup reference max-letdown/min-charging intent differs from fixed DRAIN policy (75/0->44) | Medium | INVESTIGATING | - | Pending (active issue) |
| CS-0035 | CVCS thermal mixing contribution missing from RCS energy balance | Low | READY_FOR_FIX | - | Pending (active issue) |

## D) Execution Readiness Indicator
**READY FOR AUTHORIZATION**

No blocking Critical issues unresolved outside domain.

## E) Notes / Investigation Links
- Registry consistency synchronized against Governance/IssueRegister/issue_index.json and Governance/IssueRegister/issue_register.json on 2026-02-15.
- 2026-02-15 preliminary investigation for CS-0073/CS-0076 is tracked directly under this approved plan (no non-canonical DP identifiers).

## F) Authoritative Validation Gates (CS-0073/0076)
1. DRAIN start to COMPLETE must be <= 60 minutes (PASS), <= 40 minutes nominal (ADVISORY).
2. During DRAIN, pressure must remain in startup band while level trends toward ~25%.
3. DRAIN progression must not be dominated solely by CVCS net outflow when displacement is intended primary.

## G) Preliminary Findings (No Code Changes)
- Post-CCP fixed CVCS baseline is 31 gpm net outflow (75 letdown - 44 charging) in DRAIN (`Assets/Scripts/Physics/PlantConstants.CVCS.cs:146`, `Assets/Scripts/Physics/PlantConstants.CVCS.cs:162`).
- Measured implied post-CCP drain rate from logs is ~49.0 gpm (9.00->11.25 hr), indicating baseline limitation plus displacement contribution.
- Procedure alignment risk remains: reference intent is max-letdown/min-charging draw behavior while implementation is fixed 75/0->44 policy during DRAIN.
- Multi-orifice capability is documented and modeled in constants (2x75 + 1x45 with 120 gpm cap), but active DRAIN flow path uses fixed letdown/charging policy rather than dynamic lineup authority (`Assets/Scripts/Physics/PlantConstants.CVCS.cs:398`, `Assets/Scripts/Physics/PlantConstants.CVCS.cs:404`, `Assets/Scripts/Physics/PlantConstants.CVCS.cs:425`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:79`).

## H) Smallest Conceptual Fix Options
1. Procedure-aligned DRAIN flow schedule envelope (bounded by documented limits).
2. Adaptive charging suppression during DRAIN until displacement trend targets are met.
3. Runtime validator comparing implied drain rate to CVCS-only prediction each interval.
4. Explicitly route DRAIN through approved/declared orifice lineup authority (or document a formal fixed-policy waiver with evidence).
