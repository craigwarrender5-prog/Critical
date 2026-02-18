---
IP ID: IP-0056
DP Reference: DP-0008
Title: Validation Dashboard UI Toolkit Overhaul and Backlog Consolidation
Status: OPEN
Date: 2026-02-18
Mode: SPEC/DRAFT
Source of Scope Truth: Governance/IssueRegister/issue_index.json
Predecessor: IP-0053 (OPEN/PENDING_CLOSEOUT)
Blocking Dependencies: None
---

# IP-0056 - DP-0008 - Validation Dashboard UI Toolkit Overhaul and Backlog Consolidation

## 1) Governance Header
- DP Reference: `DP-0008 - Operator Interface & Scenarios`
- IP Status: `OPEN`
- Included CS count: `3`

## 2) Included CS Scope
| CS ID | Title | Severity | Current Status | Scope Role |
|---|---|---|---|---|
| `CS-0127` | Validation Dashboard Complete Overhaul Using Unity UI Toolkit | HIGH | READY | Primary implementation scope |
| `CS-0118` | Validation dashboard missing condenser/feedwater telemetry coverage | MEDIUM | READY | Subordinate scope absorbed by `CS-0127` |
| `CS-0121` | Dashboard visual issues: SOLID PZR indicator not lit, alarm symbol incorrectly transcoded | LOW | READY | Subordinate scope absorbed by `CS-0127` |

## 3) Supersession Decision
- Decision: `CS-0127` is the umbrella remediation for the remaining active DP-0008 dashboard backlog.
- `CS-0118` and `CS-0121` are execution-tracked inside this IP as supersession candidates.
- Closure policy:
  - Keep `CS-0118` and `CS-0121` open through Stage D/E validation.
  - If Stage D/E confirms their acceptance criteria are satisfied by the `CS-0127` implementation, close both as `CLOSED` with `resolution_type: CLOSE_NO_CODE` and explicit `Superseded-by-CS-0127` tagging.
  - If any acceptance criterion is not satisfied, keep the affected CS open and break out targeted follow-on work.

## 4) Dependency Hierarchy Analysis
### Prerequisite CS items
- None declared as a hard prerequisite in register metadata.

### Blocking relationships
- `CS-0127 -> CS-0118` acceptance: condenser/feedwater telemetry coverage must be present in UITK dashboard tabs/snapshots.
- `CS-0127 -> CS-0121` acceptance: SOLID PZR indication and alarm glyph correctness must be validated in the new dashboard path.

### Shared files/systems
- `Assets/Scripts/UI/UIToolkit/ValidationDashboard/*`
- `Assets/Scripts/Validation/HeatupSimEngine.cs`
- `Assets/Scripts/Validation/ValidationDashboard*.cs` (legacy reference/deprecation boundary)
- `Assets/Scripts/Validation/HeatupValidationVisual*.cs` (legacy reference/deprecation boundary)

### Interference risk
- Moderate risk while legacy and UITK dashboards coexist during migration.
- Low risk for cross-domain interference because scope remains within DP-0008 UI ownership.

## 5) Execution Order and Critical Path
1. Stage A: freeze acceptance criteria and migration boundaries (legacy vs UITK runtime ownership).
2. Stage B: design freeze for UITK tab architecture, data model, and rendering/update cadence.
3. Stage C1: implement/complete `CS-0127` foundation and core tabs.
4. Stage C2: satisfy telemetry acceptance that subsumes `CS-0118`.
5. Stage C3: satisfy visual correctness acceptance that subsumes `CS-0121`.
6. Stage D: domain validation against full DP-0008 dashboard acceptance matrix.
7. Stage E: regression across scene transitions, keybind workflows, and runtime performance.
8. Closeout: apply final dispositions (`CS-0127` fixed; `CS-0118`/`CS-0121` closed as superseded only if fully covered).

Critical path:
`CS-0127 implementation completeness -> CS-0118/CS-0121 acceptance coverage -> Stage D/E validation -> closure dispositions`

## 6) Enforcement Gates and Exit Criteria
### Gate A - Scope Freeze PASS
- UITK dashboard ownership and legacy dashboard transition strategy are explicitly documented.
- Traceability map from `CS-0118` and `CS-0121` acceptance items to `CS-0127` deliverables is complete.

### Gate B - Implementation PASS
- UITK dashboard exposes required telemetry breadth (including condenser/feedwater/permissives).
- Critical visual correctness paths (solid-PZR indication and alarm symbol rendering) are implemented in production UITK surfaces.

### Gate C - Domain Validation PASS
- All DP-0008 acceptance checks pass in validation scenarios.
- `CS-0118` and `CS-0121` acceptance criteria are objectively satisfied by `CS-0127` outputs.

### Gate D - System Regression PASS
- No regressions in scenario selection, dashboard launch/toggle behavior, and runtime update stability.

### IP Closeout PASS
- `CS-0127` closes with implementation evidence.
- `CS-0118` and `CS-0121` close as superseded only when gate evidence proves full coverage.

## 7) Cross-Domain Notes
- Domain owner remains `DP-0008`.
- Any runtime API additions that touch non-DP-0008 modules must remain additive and non-breaking.

## 8) Revision History
| Revision | Date | Author | Summary of Changes | Reason for Amendment |
|---|---|---|---|---|
| v0.1 | 2026-02-18 | Codex | Initial DP-0008 implementation plan for UITK dashboard overhaul with explicit supersession policy for `CS-0118` and `CS-0121` under `CS-0127`. | Convert active DP-0008 backlog into one dependency-ordered execution plan and formalize supersession decision logic. |
