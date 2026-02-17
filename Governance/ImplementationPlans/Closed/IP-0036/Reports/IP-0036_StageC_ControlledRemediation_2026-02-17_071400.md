# IP-0036 Stage C Controlled Remediation (2026-02-17_071400)

- IP: `IP-0036`
- DP: `DP-0001`
- Stage: `C`

## 1) Implemented Changes

### Core authority alignment
1. Updated authoritative RCP heat constants to match frozen baseline:
- `Assets/Scripts/Physics/PlantConstants.Pressure.cs:199`
- `Assets/Scripts/Physics/PlantConstants.Pressure.cs:205`
2. Updated constants documentation to explicitly reference Section 3.2 authority:
- `Assets/Scripts/Physics/PlantConstants.Pressure.cs:197`

### Runtime path and status alignment
1. Updated heat descriptor strings to use constant-driven totals (remove hardcoded `21 MW`):
- `Assets/Scripts/Validation/HeatupSimEngine.cs:1874`
- `Assets/Scripts/Validation/HeatupSimEngine.cs:2029`
2. Corrected per-pump heat helper to use per-pump constant:
- `Assets/Scripts/Physics/FluidFlow.cs:119`

### Validation and consistency alignment
1. Updated RCP sequencer validation gate to constant-driven expected value:
- `Assets/Scripts/Physics/RCPSequencer.cs:482`
2. Updated loop thermodynamics validation examples to constant-driven inputs:
- `Assets/Scripts/Physics/LoopThermodynamics.cs:270`
- `Assets/Scripts/Physics/LoopThermodynamics.cs:283`
- `Assets/Scripts/Physics/LoopThermodynamics.cs:287`
- `Assets/Scripts/Physics/LoopThermodynamics.cs:288`
3. Updated Phase 1 test references to constant-driven RCP heat expectation:
- `Assets/Scripts/Tests/Phase1TestRunner.cs:789`
- `Assets/Scripts/Tests/Phase1TestRunner.cs:790`
- `Assets/Scripts/Tests/Phase1TestRunner.cs:802`
- `Assets/Scripts/Tests/Phase1TestRunner.cs:820`

### Commentary/reference coherence in scoped heatup model docs
1. Updated in-code descriptive comments for `24 MW` authority in directly related modules:
- `Assets/Scripts/Physics/RCSHeatup.cs:9`
- `Assets/Scripts/Physics/PlantConstants.Heatup.cs:47`
- `Assets/Scripts/Physics/PlantConstants.SteamDump.cs:11`
- `Assets/Scripts/Physics/SteamDumpController.cs:21`
- `Assets/Scripts/Physics/SGMultiNodeThermal.cs:43`
- `Assets/Scripts/Validation/HeatupValidationVisual.Gauges.cs:467`

## 2) Stage C Exit
Stage C remediation is complete for `CS-0080`. Stage D validation authorized.
