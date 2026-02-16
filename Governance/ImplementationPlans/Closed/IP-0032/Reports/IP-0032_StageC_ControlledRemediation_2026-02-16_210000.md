# IP-0032 Stage C Controlled Remediation (2026-02-16_210000)

- IP: `IP-0032`
- DP: `DP-0010`
- Stage: `C`

## 1) Implemented Changes

### CS-0058
1. Added explicit HZP lifecycle management in `Assets/Scripts/Validation/HeatupSimEngine.HZP.cs`:
- `ShouldHZPSystemsBeActive()`
- `UpdateHZPLifecycle()`
- `ResetHZPSystemsLifecycle()`
2. Removed first-time initialization guard path from `UpdateHZPSystems()`.
3. Wired lifecycle/reset hooks:
- `Assets/Scripts/Validation/HeatupSimEngine.cs` now calls `UpdateHZPLifecycle()` before `UpdateHZPSystems(dt)`.
- `Assets/Scripts/Validation/HeatupSimEngine.Init.cs` now calls `ResetHZPSystemsLifecycle()` during run initialization.

### CS-0060
1. Extracted runtime math from constants partials:
- Added `Assets/Scripts/Physics/PlantMath.cs`
- Added `Assets/Scripts/Physics/CVCSFlowMath.cs`
2. Removed runtime methods from:
- `Assets/Scripts/Physics/PlantConstants.cs`
- `Assets/Scripts/Physics/PlantConstants.CVCS.cs`
3. Migrated call sites to `CVCSFlowMath`:
- `Assets/Scripts/Physics/CVCSController.cs`
- `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs`
- `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs`

### CS-0083
1. Added authority-precedence artifact:
- `Technical_Documentation/RCP_Heat_Authority_Decision_2026-02-16.md`
2. Updated index linkage:
- `Technical_Documentation/Technical_Documentation_Index.md`

### CS-0097
1. Applied governance remediation to register artifacts:
- `Governance/IssueRegister/issue_register.json`
- `Governance/IssueRegister/issue_index.json`
- `Governance/IssueRegister/issue_archive.json`
2. Enforced active-register purity and closed-set archive/index normalization.

## 2) Stage C Exit
1. All scoped code/governance remediations implemented.
2. Stage D validation authorized.
