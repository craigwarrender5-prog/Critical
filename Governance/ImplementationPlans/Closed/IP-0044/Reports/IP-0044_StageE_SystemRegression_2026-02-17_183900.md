# IP-0044 Stage E System Regression (2026-02-17_183900)

- IP: `IP-0044`
- DP: `DP-0006`
- Stage: `E`

## 1) Stage E Method
Stage E regression executed as:
1. Cross-file static non-regression review for startup permissive and SG alarm paths.
2. Compile regression check via `dotnet build Critical.slnx`.
3. Governance snapshot check before closure transaction.

## 2) CS Acceptance Matrix

| CS ID | Acceptance Requirement | Evidence | PASS/FAIL |
|---|---|---|---|
| CS-0079 | RCP startup permissive authority remains enforced at `400 psig` in constants, startup gate helper, and sequencer messaging. | `Assets/Scripts/Physics/PlantConstants.Pressure.cs`, `Assets/Scripts/Physics/RCPSequencer.cs` | PASS |
| CS-0010 | SG secondary pressure high alarm exists, propagates through engine state, and is visible in operator annunciation surfaces. | `Assets/Scripts/Physics/AlarmManager.cs`, `Assets/Scripts/Validation/HeatupSimEngine.Alarms.cs`, `Assets/Scripts/Validation/HeatupValidationVisual.Annunciators.cs`, `Assets/Scripts/UI/ValidationDashboard/Panels/OverviewSection_Alarms.cs`, `Assets/Scripts/UI/ValidationDashboard/Panels/AlarmsPanel.cs` | PASS |

## 3) Governance Snapshot (Pre-Closure)
1. `issue_register` includes both scoped CS items as active at Stage E snapshot:
- `Governance/IssueRegister/issue_register.json:8`
- `Governance/IssueRegister/issue_register.json:26`
2. Registry count snapshot before closure transaction:
- `issue_register.active_issue_count = 12`
- `issue_index.active_issue_count = 12`
- `issue_index.closed_issue_count = 96`
- `issue_archive.archived_issue_count = 96`

## 4) Non-Regression Summary
1. Startup permissive path remains authoritative in physics/control logic.
2. SG alarm path remains additive and does not remove existing alarm channels.
3. Compile regression check passes with no errors.

## 5) Stage E Exit
Stage E passes for `IP-0044` scoped implementation. Closure recommendation is authorized.
