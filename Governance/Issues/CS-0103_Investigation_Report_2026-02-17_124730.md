# CS-0103 Investigation Report (2026-02-17_124730)

- Issue ID: `CS-0103`
- Title: `Add in-simulator scenario selection overlay with keybind trigger`
- Initial Status at Creation: `INVESTIGATING`
- Investigation State: `Preliminary`
- Recommended Domain: `Operator Interface & Scenarios`

## Problem

There is no in-simulator scenario selection mechanism, preventing runtime scenario switching/selection without code-level entrypoint changes.

## Scope

Add a lightweight overlay/menu that is toggled by keybind (for example `F1`) and allows scenario selection/start.

## Non-scope

- No operator screen redesign
- No invasive UI architecture changes
- No non-scenario UI rework

## Acceptance Criteria

1. Keybind toggles scenario overlay visibility.
2. Overlay presents registered scenario list.
3. Selecting a scenario invokes the scenario start path.
4. Overlay is isolated and non-destructive to existing UI stack.

## Risks/Compatibility

- Medium risk of input conflicts with existing UI/input actions.
- Overlay layering/z-order conflicts possible if isolation boundaries are not explicit.

## Verification Evidence

- Input trace showing toggle on/off behavior.
- UI evidence (screen capture/log) showing scenario list rendering.
- Start event evidence confirming scenario invocation from overlay selection.

## Likely Impacted Areas/Files (Best-effort)

- `Assets/Scripts/ScenarioSystem/` (new target folder)
- `Assets/Scripts/UI/ScreenManager.cs`
- `Assets/Scripts/UI/OperatorScreen.cs`
- `Assets/InputSystem_Actions.inputactions`
- `Assets/Prefabs/Screens/` (if overlay prefab container is used)

## Technical Documentation References

- `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md` (scenario progression context)
- `Technical_Documentation/Technical_Documentation_Index.md` (traceability)
