# CS-0105 Investigation Report (2026-02-17_124730)

- Issue ID: `CS-0105`
- Title: `Modularize current single-loop RCS into reusable RCSLoop prefab/module boundary`
- Initial Status at Creation: `INVESTIGATING`
- Investigation State: `Preliminary`
- Recommended Domain: `Primary Thermodynamics`

## Problem

Current RCS implementation behaves as single-loop-oriented, making future four-loop expansion costly without explicit loop-level modular boundaries.

## Scope

Define reusable Loop A module/prefab boundary (`RCSLoop`) including loop-local components and sensors while preserving existing single-loop behavior.

## Non-scope

- No four-loop behavior implementation
- No physics behavior changes
- No parameter/tuning changes except what is required for modular extraction boundaries

## Acceptance Criteria

1. Loop A executes with functionally equivalent behavior to current single-loop baseline.
2. Loop-local components/sensors are grouped behind a reusable module boundary.
3. Boundary is prefab-ready and compatible with future loop replication.

## Risks/Compatibility

- High compatibility risk from extraction boundary errors introducing behavior drift.
- Potential coupling exposure across physics and UI bindings if loop-local vs shared ownership is unclear.

## Verification Evidence

- Baseline equivalence evidence for N=1 before/after modular boundary extraction.
- Structural evidence showing loop-local assets/components grouped under modular boundary.
- Non-regression traces for key RCS metrics.

## Likely Impacted Areas/Files (Best-effort)

- `Assets/Scripts/Systems/RCS/` (new target folder)
- `Assets/Prefabs/Systems/RCS/` (new target folder)
- `Assets/Scripts/Physics/RCSHeatup.cs`
- `Assets/Scripts/Physics/LoopThermodynamics.cs`
- `Assets/Scripts/UI/RCSPrimaryLoopScreen.cs`

## Technical Documentation References

- `Technical_Documentation/NRC_HRTD_Section_3.2_Reactor_Coolant_System.md` (RCS structural expectations)
- `Technical_Documentation/RCS_PT_Limits_and_Steam_Tables_Reference.md` (RCS operating envelope context)
