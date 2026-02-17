# CS-0102 Investigation Report (2026-02-17_124730)

- Issue ID: `CS-0102`
- Title: `Establish scenario system framework with registry and scenario abstraction`
- Initial Status at Creation: `INVESTIGATING`
- Investigation State: `Preliminary`
- Recommended Domain: `Operator Interface & Scenarios`

## Problem

Validation execution is currently hardwired, limiting extensibility and making multi-scenario lifecycle control difficult.

## Scope

Define a minimal scenario abstraction and scenario registry that can register and start multiple scenarios through a clean orchestration boundary.

## Non-scope

- No heavy content pipeline
- No broad architecture rewrite outside minimal scenario boundary
- No scenario-specific tuning work

## Acceptance Criteria

1. Scenario abstraction contract is defined and implementation-ready.
2. Registry can register at least baseline scenario entries.
3. Scenario start handoff path is defined and deterministic.

## Risks/Compatibility

- Medium risk of lifecycle overlap with existing validation loop if ownership boundaries are unclear.
- Requires compatibility with current initialization and update cadence.

## Verification Evidence

- Scenario registry listing evidence (at least one registered scenario).
- Start-path evidence showing deterministic scenario activation handoff.
- Non-regression evidence that existing simulator loop remains intact when no scenario is started.

## Likely Impacted Areas/Files (Best-effort)

- `Assets/Scripts/ScenarioSystem/` (new target folder)
- `Assets/Scripts/Validation/HeatupSimEngine.cs`
- `Assets/Scripts/Validation/HeatupSimEngine.Init.cs`
- `Assets/Scripts/Core/` (orchestration boundary if required)

## Technical Documentation References

- `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md` (scenario flow expectations for startup progression)
- `Technical_Documentation/Technical_Documentation_Index.md` (traceability to existing technical references)
