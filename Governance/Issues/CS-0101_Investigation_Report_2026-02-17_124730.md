# CS-0101 Investigation Report (2026-02-17_124730)

- Issue ID: `CS-0101`
- Title: `Introduce deterministic Cold Shutdown initialization baseline as default startup state`
- Initial Status at Creation: `INVESTIGATING`
- Investigation State: `Preliminary`
- Recommended Domain: `Pressurizer & Startup Control`

## Problem

Simulator startup currently behaves like validation-run initialization instead of a deterministic plant baseline state, making startup semantics and scenario entry ambiguous.

## Scope

Define a formal default startup baseline for `Cold Shutdown` that is deterministic, observable, and remains idle until a scenario is explicitly started.

## Non-scope

- No scenario-selection UI in this CS
- No new scenario content beyond baseline definition
- No tuning changes unrelated to baseline-state semantics

## Acceptance Criteria

1. On boot, simulator initializes into `Cold Shutdown` baseline state.
2. System remains idle in baseline until scenario start command is issued.
3. Baseline state is observable through explicit state/telemetry indicators.

## Risks/Compatibility

- Medium compatibility risk with existing validation boot assumptions.
- Potential dependence on existing initialization lifecycle ownership; requires explicit handoff rules.

## Verification Evidence

- Startup traces/logs showing default `Cold Shutdown` state on boot.
- Evidence that no scenario-specific run begins until explicit start action.
- Determinism check across repeated boots.

## Likely Impacted Areas/Files (Best-effort)

- `Assets/Scripts/Validation/HeatupSimEngine.Init.cs`
- `Assets/Scripts/Validation/HeatupSimEngine.cs`
- `Assets/Scripts/Physics/RHRSystem.cs`
- `Assets/Scripts/Physics/RCSHeatup.cs`

## Technical Documentation References

- `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md` (startup sequence expectations)
- `Technical_Documentation/NRC_HRTD_Section_5.1_Residual_Heat_Removal_System.md` (cold shutdown operational context)
- `Technical_Documentation/NRC_HRTD_Section_10.1_Reactor_Coolant_Instrumentation.md` (cold calibration/startup observability context)
