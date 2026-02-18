# CS-0119 Investigation Report

- CS ID: `CS-0119`
- Title: `F2 scenario selection overlay missing for runtime scenario start from Cold Shutdown baseline`
- Domain: `Operator Interface & Scenarios`
- Severity: `MEDIUM`
- Date: `2026-02-18`
- Recommended Next Status: `CLOSED (FIXED under IP-0049 close transaction)`

## Summary

IP-0049 delivered scenario registry/start APIs, but runtime operator flow lacked an `F2`-driven selector surface.  
This prevented list-based scenario launch from the baseline Mode 5 startup screen.

## Evidence

1. `Assets/Scripts/Validation/HeatupSimEngine.Scenarios.cs:37-57`  
   Engine already exposes scenario descriptors and `StartScenarioById`.
2. `Assets/Scripts/Validation/HeatupValidationVisual.cs` (pre-fix path)  
   Dashboard input handled `F1` and tab controls, but no scenario selector on `F2`.
3. `Assets/InputActions/ScreenInputActions.inputactions`  
   No scenario-selector action mapped to `F2`.

## Root Cause

Selector UX/keybind wiring was deferred while bridge APIs were implemented, leaving no in-simulator scenario selection path.

## Corrective Action Implemented Under IP-0049

1. Added validator-view `F2` routing in `SceneBridge.UpdateViewSwitchInput()`.
2. Added `SceneBridge` bridge helper `ToggleValidatorScenarioSelector()` that forwards to `HeatupValidationVisual`.
3. Added modal scenario selector overlay in `HeatupValidationVisual.OnGUI()`.
4. Selector lists `ScenarioDescriptor` entries from registry and starts scenarios via `StartScenarioById()`.
5. Selector disables start while simulation is already running and surfaces status feedback.

## Resolution Evidence

1. `Assets/Scripts/Core/SceneBridge.cs`  
   `F2` key handling in validator view, with forwarding to scenario selector toggle API.
2. `Assets/Scripts/Validation/HeatupValidationVisual.cs`  
   Bridge-driven selector API (`ToggleScenarioSelector`/`SetScenarioSelectorVisible`), overlay state, descriptor refresh, and start-path integration.
3. `Assets/Scripts/ScenarioSystem/ValidationHeatupScenario.cs:37`  
   Scenario start still delegates to canonical `StartSimulation()`, preserving Mode 5 cold-start semantics.
4. `dotnet build Critical.slnx`  
   PASS (`0` errors).

## Dependency Disposition

- `CS-0119` is included in `IP-0049` scope as cross-domain inclusion (`DP-0008` behavior under `DP-0013` execution).
- `CS-0119` does not block `IP-0049` closure after build validation pass.
