# CS-0103 Investigation Report (2026-02-18_131500)

- Issue ID: `CS-0103`
- Title: `Add in-simulator scenario selection overlay with keybind trigger`
- Initial Status at Creation: `INVESTIGATING`
- Investigation State: `Full`
- Investigation Completed: `2026-02-18T13:15:00Z`
- Recommended Next Status: `READY`
- Assigned Domain Plan: `DP-0008 - Operator Interface & Scenarios`

## 1) Problem Statement

Scenario selector overlay must be runtime-accessible via keybind and support in-simulator scenario start without destructive UI behavior.

## 2) Code Evidence Review

### Overlay and start behavior exist
1. Selector visibility APIs are implemented:
   - `Assets/Scripts/Validation/HeatupValidationVisual.cs:557-579`
2. Overlay renders scenario list and START actions:
   - `Assets/Scripts/Validation/HeatupValidationVisual.cs:583-651`
3. START invokes engine scenario bridge:
   - `Assets/Scripts/Validation/HeatupValidationVisual.cs:635-637`
4. Engine supports descriptor listing/start by ID:
   - `Assets/Scripts/Validation/HeatupSimEngine.Scenarios.cs:43-71`

### Keybind accessibility gap remains
1. Scene input routing handles `F2` only in Validator view:
   - `Assets/Scripts/Core/SceneBridge.cs:170-175`
2. Operator Screens branch has no `F2` handling:
   - `Assets/Scripts/Core/SceneBridge.cs:162-168`

## 3) Root Cause

The selector overlay feature was implemented in the validator UI path, but scene-level input routing did not include an Operator Screens `F2` path, leaving default-view runtime accessibility incomplete.

## 4) Disposition

**Disposition: READY (functionality present but acceptance incomplete).**

Overlay and scenario start exist; closure requires full keybind accessibility behavior and regression evidence across view transitions.

## 5) Corrective Scope for IP

1. Finalize keybind routing semantics across both views (with async-safe scene transition behavior).
2. Validate selector lifecycle (open/start/close) from default operator workflow.
3. Verify no regressions to `V`, `Esc`, and `1-8/Tab` view controls.

## 6) Acceptance Criteria

1. Keybind-driven selector flow works from active runtime workflow, not validator-only workaround.
2. Selector lists descriptors and starts selected scenario through engine bridge.
3. Existing scene navigation behavior remains stable.

## 7) Affected Files

- `Assets/Scripts/Core/SceneBridge.cs`
- `Assets/Scripts/Validation/HeatupValidationVisual.cs`
- `Assets/Scripts/Validation/HeatupSimEngine.Scenarios.cs`
- `Assets/Scripts/ScenarioSystem/ScenarioRegistry.cs`
