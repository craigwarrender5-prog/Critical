# CS-0106 Investigation Report (2026-02-17_124730)

- Issue ID: `CS-0106`
- Title: `Add RCS loop manager/aggregator supporting N loops with N=1 compatibility`
- Initial Status at Creation: `INVESTIGATING`
- Investigation State: `Preliminary`
- Recommended Domain: `Primary Thermodynamics`

## Problem

Downstream systems currently bind to effectively single-loop representations. Future N-loop operation requires stable system-level aggregation APIs and loop indexing.

## Scope

Define an RCS-level loop manager that owns `N` loops and provides aggregate values (for example total flow, averages, indexed loop access), preserving existing N=1 outputs.

## Non-scope

- No full downstream rewiring beyond compatibility guidance
- No N=4 behavior implementation in this CS
- No physics model retuning

## Acceptance Criteria

1. Manager contract defines loop ownership and aggregate outputs.
2. With `N=1`, aggregate outputs match current behavior.
3. Structure supports later expansion to `N=4` with minimal refactor.

## Risks/Compatibility

- High compatibility risk if aggregation semantics alter existing consumers under N=1.
- API contract risk if loop indexing and aggregate definitions are ambiguous.

## Verification Evidence

- N=1 equivalence evidence for aggregate outputs vs current baseline values.
- Contract evidence showing explicit aggregate definitions and loop indexing behavior.
- Forward-compatibility review evidence for N>1 expansion path.

## Likely Impacted Areas/Files (Best-effort)

- `Assets/Scripts/Systems/RCS/` (new target folder)
- `Assets/Scripts/Physics/LoopThermodynamics.cs`
- `Assets/Scripts/Physics/RCSHeatup.cs`
- `Assets/Scripts/UI/ScreenDataBridge.cs`
- `Assets/Scripts/UI/RCSVisualizationController.cs`

## Technical Documentation References

- `Technical_Documentation/NRC_HRTD_Section_3.2_Reactor_Coolant_System.md` (loop/system architecture context)
- `Technical_Documentation/NRC_HRTD_Section_10.1_Reactor_Coolant_Instrumentation.md` (system-level measurement/aggregation context)
