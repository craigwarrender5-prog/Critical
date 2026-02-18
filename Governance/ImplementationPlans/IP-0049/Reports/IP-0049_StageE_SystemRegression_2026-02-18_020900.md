# IP-0049 Stage E System Regression (2026-02-18 02:09 UTC)

- IP: `IP-0049`
- Stage: `E - System Regression`
- Result: `BLOCKED`

## Regression Checks
1. Build integrity:
- `dotnet build Critical.slnx` -> `FAIL` due unrelated compile errors in `Assets/Scripts/Validation/ValidationDashboard.Sparklines.cs` (`IDX_NET_HEAT` unresolved).
2. Legacy startup path preservation:
- `runOnStart` retains direct `StartSimulation()` fallback when scenario path disabled or start fails.
3. Shutdown behavior:
- `activeScenarioId` reset on immediate shutdown path.

## Stage Decision
- Stage E is blocked by external parallel-track validation failure outside IP-0049 scope.
- IP-0049 closure cannot proceed until workspace compile baseline is restored.
