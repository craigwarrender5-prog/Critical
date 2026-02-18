---
IP ID: IP-0052
DP Reference: DP-0012
Title: Mode 5 Pre-Heater Pressurization Policy Alignment
Status: OPEN
Date: 2026-02-18
Mode: PLANNED
Source of Scope Truth: Governance/IssueRegister/issue_index.json
Predecessor: IP-0048 (CLOSED)
Blocking Dependencies: IP-0054 no-RCP thermal-coupling baseline freeze (recommended before Stage D closeout)
---

# IP-0052 - DP-0012 - Mode 5 Pre-Heater Pressurization Policy Alignment

## 1) Governance Header
- DP Reference: `DP-0012 - Pressurizer & Startup Control`
- IP Status: `OPEN`
- Included CS count: `1`

## 2) Included CS Scope
| CS ID | Title | Severity | Status |
|---|---|---|---|
| `CS-0109` | PZR Mode 5 pre-heater pressurization: validate net +1 gpm charging imbalance vs documented expectations | HIGH | READY |

## 3) Dependency Hierarchy Analysis
### Prerequisite CS items
- None declared in-register for this DP scope.

### Blocking relationships
- This IP should follow `IP-0054` baseline correction so no-RCP thermal coupling fidelity is stabilized before final Mode 5 policy validation.
- This IP remains higher-impact than DP-0008 UX refinement and should complete before `IP-0053` closeout.

### Shared files/systems
- `Assets/Scripts/Physics/SolidPlantPressure.cs`
- `Assets/Scripts/Validation/HeatupSimEngine.cs`
- `Assets/Scripts/Physics/CVCSController.cs`
- `Build/HeatupLogs/*.txt` (validation evidence)

### Interference risk between fixes
- High risk if CVCS imbalance authority and heater sequencing remain coupled during pre-heater stage.
- Moderate risk of regression in startup hold/transition behavior if pressure policy boundaries are not explicit.

## 4) Execution Order and Critical Path
1. Stage A: confirm documented acceptance envelope and freeze quantitative targets for Mode 5 pre-heater segment.
2. Stage B: design explicit pre-heater policy boundary (CVCS imbalance envelope + heater handoff criteria).
3. Stage C: implement control-policy separation and parameterized envelope.
4. Stage D: domain validation against documented mechanism/rate expectations and startup sequence.
5. Stage E: full startup regression pass to ensure no destabilization of closed DP-0012 behaviors.

Critical path:
`Policy Target Freeze -> Control-Boundary Refactor -> Quantitative Validation -> Regression`

## 5) Enforcement Gates and Exit Criteria
### Gate A - Technical Envelope Freeze PASS
- Numeric target envelope for pre-heater period is declared and traceable to technical references.
- Explicit handoff boundary from CVCS-dominant pressurization to heater-led segment is documented.

### Gate B - Control Policy Implementation PASS
- Code evidence shows pre-heater CVCS imbalance authority and heater participation are not conflated.
- No hidden fallback path reintroduces +/-1 gpm-only envelope where documentation requires broader authority.

### Gate C - Domain Validation PASS
- Validation logs demonstrate Mode 5 pre-heater behavior is within frozen envelope.
- Transition event to heater-led stage occurs at defined condition with deterministic trace output.

### Gate D - System Regression PASS
- No regression of startup baseline/hold/release behavior previously closed under `IP-0048`.

### IP Closeout PASS
- Stage evidence present for A-D gates.
- `CS-0109` disposition is explicitly updated with traceable closure evidence.

## 6) Cross-Domain Notes
- Domain owner remains `DP-0012`.
- Outputs are consumed by scenario-driven runtime validation flows; completion is prioritized ahead of DP-0008 backlog closeout to avoid validating UX against unstable startup policy behavior.

## 7) Revision History
| Revision | Date | Author | Summary of Changes | Reason for Amendment |
|---|---|---|---|---|
| v0.1 | 2026-02-18 | Codex | Initial DP-0012 implementation plan for CS-0109 corrective scope. | Convert completed investigation into executable governance plan with objective gates. |
| v0.2 | 2026-02-18 | Codex | Added upstream dependency alignment to `IP-0054` for no-RCP thermal baseline fidelity before final DP-0012 validation closeout. | Prevent policy tuning/acceptance against distorted primary thermal behavior. |
