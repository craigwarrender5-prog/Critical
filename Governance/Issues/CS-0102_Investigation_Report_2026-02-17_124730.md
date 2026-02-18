# CS-0102 Investigation Report (2026-02-18_131500)

- Issue ID: `CS-0102`
- Title: `Establish scenario system framework with registry and scenario abstraction`
- Initial Status at Creation: `INVESTIGATING`
- Investigation State: `Full`
- Investigation Completed: `2026-02-18T13:15:00Z`
- Recommended Next Status: `READY`
- Assigned Domain Plan: `DP-0008 - Operator Interface & Scenarios`

## 1) Problem Statement

A durable scenario-system boundary is required so runtime scenario selection/start does not depend on hardwired validation-only entry points.

## 2) Code Evidence Review

### Scenario contract and registry seam exists
1. Contract exists with deterministic start interface:
   - `Assets/Scripts/ScenarioSystem/ISimulationScenario.cs:44-50`
2. Central registry exists with ID lookup and descriptor listing:
   - `Assets/Scripts/ScenarioSystem/ScenarioRegistry.cs:14-74`
3. Engine bridge exposes list/start APIs:
   - `Assets/Scripts/Validation/HeatupSimEngine.Scenarios.cs:37-71`

### Deterministic start handoff exists
1. Scenario start path routes to `StartScenarioById` when enabled:
   - `Assets/Scripts/Validation/HeatupSimEngine.cs:940-949`
2. Baseline scenario wrapper delegates to canonical `StartSimulation()`:
   - `Assets/Scripts/ScenarioSystem/ValidationHeatupScenario.cs:21-39`

## 3) Residual Gap Keeping CS Open

The framework is implemented, but domain ownership and extensibility boundaries remain incomplete for DP-0008 closure:
1. Registry bootstrap currently hard-registers only one validation scenario:
   - `Assets/Scripts/Validation/HeatupSimEngine.Scenarios.cs:81-84`
2. Built-in scenario descriptor reports domain owner `DP-0013`, not DP-0008 scenario-system ownership:
   - `Assets/Scripts/ScenarioSystem/ValidationHeatupScenario.cs:19`

This leaves DP-0008 without finalized governance evidence that the scenario framework is domain-neutral and reusable beyond a validation-specific bootstrap.

## 4) Root Cause

Initial implementation solved the minimum runtime bridge but anchored scenario registration/ownership to a validation-specific baseline wrapper, leaving DP-0008 framework governance acceptance incomplete.

## 5) Disposition

**Disposition: READY (partial implementation present, closure hardening required).**

Implementation exists and is functional; remaining work is closure hardening in DP-0008 scope (ownership boundary, registration model, and acceptance evidence).

## 6) Corrective Scope for IP

1. Freeze DP-0008 ownership contract for scenario abstraction/registry behavior.
2. Define domain-ownership semantics in scenario descriptors that align with DP-0008 governance.
3. Provide closeout evidence that scenario registration/start remains deterministic and non-regressive.

## 7) Acceptance Criteria

1. Scenario abstraction and registry boundary are explicitly documented and validated in runtime path.
2. Scenario descriptor ownership semantics are governance-consistent with DP-0008 scope.
3. Start-path behavior is deterministic and preserves canonical startup fallback.

## 8) Affected Files

- `Assets/Scripts/ScenarioSystem/ISimulationScenario.cs`
- `Assets/Scripts/ScenarioSystem/ScenarioRegistry.cs`
- `Assets/Scripts/ScenarioSystem/ValidationHeatupScenario.cs`
- `Assets/Scripts/Validation/HeatupSimEngine.Scenarios.cs`
- `Assets/Scripts/Validation/HeatupSimEngine.cs`
