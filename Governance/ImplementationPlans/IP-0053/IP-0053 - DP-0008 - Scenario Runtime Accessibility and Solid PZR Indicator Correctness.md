---
IP ID: IP-0053
DP Reference: DP-0008
Title: Scenario Runtime Accessibility and Dashboard Visual Correctness
Status: OPEN
Date: 2026-02-18
Mode: EXECUTED/PENDING_CLOSEOUT
Source of Scope Truth: Governance/IssueRegister/issue_index.json
Predecessor: IP-0049 (CLOSED), IP-0051 (CLOSED)
Blocking Dependencies: CS-0102 before CS-0103 closeout
Execution Start: 2026-02-18T05:00:00Z
Execution Complete: 2026-02-18T05:09:00Z
Stage A Report: Governance/ImplementationPlans/IP-0053/Reports/IP-0053_StageA_RootCause_2026-02-18_050900.md
Stage B Report: Governance/ImplementationPlans/IP-0053/Reports/IP-0053_StageB_DesignFreeze_2026-02-18_050900.md
Stage C Report: Governance/ImplementationPlans/IP-0053/Reports/IP-0053_StageC_ControlledRemediation_2026-02-18_050900.md
Stage D Report: Governance/ImplementationPlans/IP-0053/Reports/IP-0053_StageD_DomainValidation_2026-02-18_050900.md
Stage E Report: Governance/ImplementationPlans/IP-0053/Reports/IP-0053_StageE_SystemRegression_2026-02-18_050900.md
---

# IP-0053 - DP-0008 - Scenario Runtime Accessibility and Dashboard Visual Correctness

## 1) Governance Header
- DP Reference: `DP-0008 - Operator Interface & Scenarios`
- IP Status: `OPEN`
- Included CS count: `4`

## 2) Included CS Scope
| CS ID | Title | Severity | Status |
|---|---|---|---|
| `CS-0102` | Establish scenario system framework with registry and scenario abstraction | HIGH | READY |
| `CS-0103` | Add in-simulator scenario selection overlay with keybind trigger | MEDIUM | READY |
| `CS-0120` | F2 scenario selector keybind only functional in Validator view, not Operator Screens view | LOW | READY |
| `CS-0121` | Dashboard visual issues: SOLID PZR indicator not lit, alarm symbol incorrectly transcoded | LOW | READY |

## 3) Dependency Hierarchy Analysis
### Prerequisite CS items
- `CS-0102` is prerequisite for clean closeout of `CS-0103` because selector behavior depends on a stable scenario contract/registry boundary.

### Blocking relationships
- `CS-0102 -> CS-0103` (DP backlog dependency).
- `CS-0120` depends on selector flow availability and should execute after `CS-0103` path freeze.
- `CS-0121` is isolated and can run late in this IP with low interference risk.

### Shared files/systems
- `Assets/Scripts/ScenarioSystem/ISimulationScenario.cs`
- `Assets/Scripts/ScenarioSystem/ScenarioRegistry.cs`
- `Assets/Scripts/Validation/HeatupSimEngine.Scenarios.cs`
- `Assets/Scripts/Core/SceneBridge.cs`
- `Assets/Scripts/Validation/HeatupValidationVisual.cs`
- `Assets/Scripts/Validation/Tabs/OverviewTab.cs`

### Interference risk between fixes
- Moderate if input routing changes race with additive scene load sequencing.
- Low for `CS-0121` because it is display-logic-localized.

## 4) Execution Order and Critical Path
1. Stage A: reconfirm scenario contract and runtime selector acceptance boundaries from current code state.
2. Stage B: freeze DP-0008 ownership boundaries for scenario start path and selector invocation behavior.
3. Stage C1: complete `CS-0102` hardening/closure evidence.
4. Stage C2: complete `CS-0103` selector-flow closeout evidence.
5. Stage C3: implement `CS-0120` operator-view F2 routing with safe scene-load handling.
6. Stage C4: implement `CS-0121` dashboard visual fixes (SOLID PZR illumination logic + alarm symbol encoding correction).
7. Stage D: domain validation across scenario launch, view transitions, and indicator-state consistency.
8. Stage E: regression for keyboard navigation and validation dashboard behavior.

Critical path:
`CS-0102 boundary freeze -> CS-0103 selector closure -> CS-0120 input routing -> Stage D/E regression`

## 5) Enforcement Gates and Exit Criteria
### Gate A - Scenario Contract/Registry Closure PASS (`CS-0102`)
- Scenario abstraction and registry behavior are verified deterministic in runtime path.
- Descriptor listing and scenario start handoff produce objective evidence.

### Gate B - Selector Runtime Flow PASS (`CS-0103`)
- Overlay toggle and scenario start flow are validated under expected operator workflow.
- Selector behavior is isolated and non-destructive to baseline UI state.

### Gate C - Operator-View F2 Routing PASS (`CS-0120`)
- F2 from Operator Screens reaches selector flow without requiring manual pre-switch to Validator.
- No regressions in `V`, `Esc`, or `1-8/Tab` navigation semantics.

### Gate D - SOLID PZR Indicator Consistency PASS (`CS-0121`)
- Overview indicator state matches `SolidPressurizer` telemetry and phase/header messaging.
- Bubble vs solid transitions illuminate correct indicator states.
- Header alarm symbol renders as intended (no transcoding/garbled glyph output).

### IP Closeout PASS
- All four CS entries have explicit closure evidence and final dispositions.

## 6) Cross-Domain Notes
- Domain owner remains `DP-0008`.
- IP assumes startup-policy behavior from `DP-0012` remains the authoritative baseline during validation runs.
- Execution priority is after high-impact thermal/policy corrections (`IP-0054`, `IP-0052`), as this IP is primarily interaction/display scope.

## 7) Revision History
| Revision | Date | Author | Summary of Changes | Reason for Amendment |
|---|---|---|---|---|
| v0.1 | 2026-02-18 | Codex | Initial DP-0008 implementation plan covering open scenario/runtime UI issues and solid-indicator correctness. | Convert open-CS investigation set into an executable, dependency-ordered plan. |
| v0.2 | 2026-02-18 | Codex | Added sequencing note to execute after DP-0001/DP-0012 high-impact thermal/policy plans. | Preserve priority alignment by severity and cross-domain impact. |
| v0.3 | 2026-02-18 | Codex | Executed Stage A-E remediation package for CS-0102/CS-0103/CS-0120/CS-0121 with build regression evidence. | Complete implementation execution and produce closure-ready stage artifacts. |

## 8) Execution Update (Completed - Pending Closure Approval)

### Implemented Remediation
- `CS-0102` scenario framework closure hardening:
  - `Assets/Scripts/ScenarioSystem/ScenarioRegistry.cs`: added factory-based bootstrap surface (`RegisterFactory`, `BootstrapFromFactories`) and default-factory seeding.
  - `Assets/Scripts/Validation/HeatupSimEngine.Scenarios.cs`: replaced engine-local hardcoded scenario registration with registry bootstrap invocation.
  - `Assets/Scripts/ScenarioSystem/ValidationHeatupScenario.cs`: aligned descriptor ownership to `DP-0008` governance surface.
- `CS-0103` and `CS-0120` selector accessibility and keybind routing:
  - `Assets/Scripts/Core/SceneBridge.cs`: added operator-view `F2` handling and queued selector-open execution after validator load completion.
- `CS-0121` dashboard visual correctness:
  - `Assets/Scripts/Validation/Tabs/OverviewTab.cs`: solid/bubble status LED now lights for either solid-PZR or bubble-formed state.
  - `Assets/Scripts/Validation/ValidationDashboard.cs`: replaced header alarm mojibake marker with ASCII-safe `[!]`.

### Validation Evidence
- Stage A: `PASS` (`Governance/ImplementationPlans/IP-0053/Reports/IP-0053_StageA_RootCause_2026-02-18_050900.md`)
- Stage B: `PASS` (`Governance/ImplementationPlans/IP-0053/Reports/IP-0053_StageB_DesignFreeze_2026-02-18_050900.md`)
- Stage C: `PASS` (`Governance/ImplementationPlans/IP-0053/Reports/IP-0053_StageC_ControlledRemediation_2026-02-18_050900.md`)
- Stage D: `PASS` (`Governance/ImplementationPlans/IP-0053/Reports/IP-0053_StageD_DomainValidation_2026-02-18_050900.md`)
- Stage E: `PASS` (`Governance/ImplementationPlans/IP-0053/Reports/IP-0053_StageE_SystemRegression_2026-02-18_050900.md`)
- Build regression:
  - Command: `dotnet build Critical.slnx`
  - Result: `0` errors (`97` warnings, non-blocking/pre-existing).
