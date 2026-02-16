# IP-0033 Stage C Controlled Remediation (2026-02-16_215200)

- IP: `IP-0033`
- DP: `DP-0007`
- Stage: `C`

## 1) Implemented Changes

### CS-0062
1. Replaced aliased telemetry source in `Assets/Scripts/Validation/HeatupSimEngine.cs`:
- `stageE_PrimaryHeatInput_MW` now uses `ComputeStageEPrimaryHeatInput_MW()`.
2. Added explicit source contract helper:
- `ComputeStageEPrimaryHeatInput_MW()` derives primary input from positive primary-side contributors (`rcpHeat`, `pzrHeaterPower`, `rhrNetHeat_MW`).

### CS-0012
1. Added regime-transition tracking state and reset:
- `Assets/Scripts/Validation/HeatupSimEngine.cs`
- `Assets/Scripts/Validation/HeatupSimEngine.Init.cs`
2. Added regime transition logging helpers:
- `GetCurrentPhysicsRegimeId(...)`
- `GetPhysicsRegimeLabel(...)`
- `LogPhysicsRegimeTransitionIfNeeded(...)`
3. Wired transition edge logging in step path:
- `LogPhysicsRegimeTransitionIfNeeded(alpha)` directly after coupling-alpha evaluation.

### CS-0064
1. Extended immutable snapshot contract:
- Added `RhrMode` to `Assets/Scripts/Simulation/Modular/State/PlantState.cs`.
2. Updated single-writer projection path:
- Added `engine.rhrModeString` export in `Assets/Scripts/Simulation/Modular/State/LegacyStateBridge.cs`.
3. Enforced snapshot-first UI reads:
- Added `TryGetPlantStateSnapshot(...)` in `Assets/Scripts/UI/ScreenDataBridge.cs`.
- `GetRHRMode()` and `GetRHRNetHeat_MW()` now consume snapshot contract only.

### CS-0011
1. Added runtime evidence object model:
- `Assets/Scripts/Tests/AcceptanceSimulationEvidence.cs`
2. Updated acceptance tests to reject vacuous pass for runtime-gated cases:
- `Assets/Scripts/Tests/AcceptanceTests_v5_4_0.cs`
- AT-02/AT-03/AT-08 now require runtime evidence and fail closed when absent.
3. Added deterministic evidence runner:
- `Assets/Scripts/UI/Editor/IP0033AcceptanceEvidenceRunner.cs`
- Captures AT-02/03/08 measurements, writes machine-readable CSV, writes governance issue artifact, populates evidence store, then runs acceptance suite.

### Carry-Forward Closures (No New Code Required)
1. `CS-0006`: call path and init defaults already present in branch.
2. `CS-0007`: validation UI drift row already present in branch.
3. `CS-0041`: inventory panel already mass-based in branch.

## 2) Stage C Exit
All scoped remediation and contract updates are implemented. Stage D validation authorized.
