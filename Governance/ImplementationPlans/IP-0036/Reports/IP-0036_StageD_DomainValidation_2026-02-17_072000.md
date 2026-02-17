# IP-0036 Stage D Domain Validation (2026-02-17_072000)

- IP: `IP-0036`
- DP: `DP-0001`
- Stage: `D`

## 1) Validation Scope
- `CS-0080`: RCP heat authority alignment to `24 MW total` / `6 MW each` and removal of hard-coded prior authority values from active scope paths.

## 2) Static Validation Results

### CS-0080
1. RCP heat constants are aligned to frozen authority:
- `Assets/Scripts/Physics/PlantConstants.Pressure.cs:199`
- `Assets/Scripts/Physics/PlantConstants.Pressure.cs:205`
2. Runtime heatup descriptor strings now use constant-driven totals:
- `Assets/Scripts/Validation/HeatupSimEngine.cs:1874`
- `Assets/Scripts/Validation/HeatupSimEngine.cs:2029`
3. Per-pump heat helper uses per-pump constant:
- `Assets/Scripts/Physics/FluidFlow.cs:119`
4. Sequencer and loop-thermo validation logic now reference constant authority values:
- `Assets/Scripts/Physics/RCPSequencer.cs:482`
- `Assets/Scripts/Physics/LoopThermodynamics.cs:270`
- `Assets/Scripts/Physics/LoopThermodynamics.cs:283`
5. Phase test suite references updated from hardcoded values to constants:
- `Assets/Scripts/Tests/Phase1TestRunner.cs:789`
- `Assets/Scripts/Tests/Phase1TestRunner.cs:790`
- `Assets/Scripts/Tests/Phase1TestRunner.cs:802`
- `Assets/Scripts/Tests/Phase1TestRunner.cs:820`

Disposition: `PASS`

## 3) Build/Execution Validation Note
`dotnet build Critical.slnx` does not pass in current workspace due a pre-existing unresolved symbol outside this IP scope:
- `Assets/Scripts/Physics/CVCSController.cs:348` (`CS0103: CVCSFlowMath does not exist in the current context`)

This is treated as an environment/baseline build defect not introduced by `IP-0036` scoped changes.

## 4) Stage D Exit
All `IP-0036` Stage D static domain-validation gates pass for scoped remediation. Stage E regression and closure packaging authorized.
