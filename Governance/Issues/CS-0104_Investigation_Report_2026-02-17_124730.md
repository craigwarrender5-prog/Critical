# CS-0104 Investigation Report (2026-02-17_124730)

- Issue ID: `CS-0104`
- Title: `Wrap existing validation runner as selectable Validation Scenario`
- Initial Status at Creation: `INVESTIGATING`
- Investigation State: `Preliminary`
- Recommended Domain: `Validation & Diagnostics`

## Problem

The current validation entrypoint is not scenario-selectable, preventing consistent use through the scenario framework.

## Scope

Expose existing validation run behavior through scenario system invocation without changing validation semantics.

## Non-scope

- No validation logic rewrite
- No tuning/result changes
- No acceptance metric redefinition

## Acceptance Criteria

1. Validation runner is available as a selectable scenario.
2. Invoked validation behavior/logs remain equivalent to current baseline path.
3. Scenario wrapping introduces no semantic changes to validation outputs.

## Risks/Compatibility

- High compatibility risk if wrapper changes initialization ordering or timing.
- Requires strict equivalence checks to avoid silent behavior drift.

## Verification Evidence

- Before/after equivalence evidence (event sequence + key metrics/log comparisons).
- Evidence that scenario-triggered validation and legacy validation produce matching outcomes under same conditions.

## Likely Impacted Areas/Files (Best-effort)

- `Assets/Scripts/Validation/HeatupSimEngine.cs`
- `Assets/Scripts/Validation/HeatupSimEngine.Init.cs`
- `Assets/Scripts/ScenarioSystem/` (new target folder)
- `Governance/Issues/` (equivalence evidence artifacts)

## Technical Documentation References

- `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md` (baseline startup/validation flow expectations)
- `Technical_Documentation/Conformance_Audit_Report_2026-02-15.md` (existing validation-governance context)
