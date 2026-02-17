# IP-0035 Stage E System Regression (2026-02-16_224900)

- IP: `IP-0035`
- DP: `DP-0006`
- Stage: `E`

## 1) Stage E Method
Stage E regression executed as:
1. Cross-file static non-regression review of all in-scope startup permissive and alarm paths.
2. Contract check for deterministic alarm reset semantics in initialization.
3. Governance snapshot check to confirm no unintended registry mutation during implementation-stage execution.

## 2) CS Acceptance Matrix

| CS ID | Acceptance Requirement | Evidence | PASS/FAIL |
|---|---|---|---|
| CS-0079 | RCP startup permissive uses authoritative minimum startup pressure threshold at 400 psig across constants and startup gate messaging. | `Assets/Scripts/Physics/PlantConstants.Pressure.cs`, `Assets/Scripts/Physics/RCPSequencer.cs` | PASS |
| CS-0010 | SG secondary pressure high alarm exists, propagates through engine alarm path, and is visible on operator annunciators. | `Assets/Scripts/Physics/AlarmManager.cs`, `Assets/Scripts/Validation/HeatupSimEngine.Alarms.cs`, `Assets/Scripts/Validation/HeatupValidationVisual.Annunciators.cs`, `Assets/Scripts/UI/ValidationDashboard/Panels/OverviewSection_Alarms.cs` | PASS |

## 3) Governance Snapshot
1. `issue_register` keeps both scoped CS items active until closure approval:
- `Governance/IssueRegister/issue_register.json:9`
- `Governance/IssueRegister/issue_register.json:267`
2. `issue_index` keeps both scoped CS items active until closure approval:
- `Governance/IssueRegister/issue_index.json:151`
- `Governance/IssueRegister/issue_index.json:1285`
3. No registry files were modified by IP-0035 Stage C remediation.

## 4) Non-Regression Summary
1. Startup threshold alignment changes do not alter bubble-gate ownership semantics.
2. SG secondary pressure alarm integration is additive to existing alarm matrix and does not remove existing alarm channels.
3. Engine initialization now explicitly resets SG secondary alarm state and edge detector state.
4. Full Unity compile/runtime validation remains an editor-side follow-up due missing generated project files in this terminal workspace.

## 5) Stage E Exit
Stage E passes for `IP-0035` scoped remediation. Closure recommendation is authorized.
