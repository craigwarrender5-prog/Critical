# IP-0049 Stage B Design Freeze (2026-02-18 02:04 UTC)

- IP: `IP-0049`
- Stage: `B - Design Freeze`
- Result: `PASS`

## Frozen Design
1. Add minimal scenario contracts + registry under `Assets/Scripts/ScenarioSystem/`.
2. Add one wrapper scenario ID: `validation.heatup.baseline`.
3. Bridge in `HeatupSimEngine`:
- `GetAvailableScenarioIds()`
- `GetAvailableScenarioDescriptors()`
- `StartScenarioById(string)`
4. Preserve legacy behavior: wrapper start delegates to existing `StartSimulation()`.
5. Keep default startup unchanged (`useScenarioStartPath=false` by default).

## Non-Scope
- No scenario-selection overlay UI.
- No validation logic rewrite.

## Stage Decision
- Proceed to Stage C implementation.
