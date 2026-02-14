# IP-0015 Implementation Evidence (2026-02-14)

## Scope Implemented
- Task 1: SG boundary sealing activation in runtime startup path.
- Task 2: Steam inventory pressure path activated when SG boundary is isolated.
- Task 3: Minimal compressible N2 cushion model (fixed N2 mass, state-derived pressure).
- Task 4: SG sink guardrail tied to secondary state (pressure + compressible volume), with no source-vs-sink clamp.
- Task 5: Diagnostics for SG boundary mode, pressure-source branch, steam inventory, and signed net plant heat.

## Code Evidence
- `Assets/Scripts/Validation/HeatupSimEngine.cs:1026`
- `Assets/Scripts/Validation/HeatupSimEngine.cs:1261`
- `Assets/Scripts/Validation/HeatupSimEngine.cs:1453`
- `Assets/Scripts/Validation/HeatupSimEngine.cs:1707`
- `Assets/Scripts/Validation/HeatupSimEngine.cs:1713`
- `Assets/Scripts/Validation/HeatupSimEngine.cs:1723`
- `Assets/Scripts/Validation/HeatupSimEngine.cs:1684`
- `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:104`
- `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1125`
- `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1150`
- `Assets/Scripts/Physics/SGMultiNodeThermal.cs:141`
- `Assets/Scripts/Physics/SGMultiNodeThermal.cs:432`
- `Assets/Scripts/Physics/SGMultiNodeThermal.cs:967`
- `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1468`
- `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1904`
- `Assets/Scripts/Physics/SGMultiNodeThermal.cs:2025`
- `Assets/Scripts/Physics/PlantConstants.SG.cs:912`
- `Assets/Scripts/Physics/PlantConstants.SG.cs:919`

## Validation Execution Attempt
- Attempted batch run using Unity Editor:
  - Command target: `Critical.Tests.Phase2UnityTestRunner.RunSmokeTest`
  - Result: blocked by project lock.
  - Error: `It looks like another Unity instance is running with this project open.`

## Stage E Rerun Status
- Stage E rerun is **pending**.
- Required pass criteria remain pending until Unity runtime execution is available:
  - SG boundary enters `ISOLATED` during startup segment.
  - Steam inventory accumulates during isolated boiling.
  - Secondary pressure departs atmospheric floor.
  - Net plant heat remains positive during intended heat-up window.
  - RCS temperature resumes positive progression.
