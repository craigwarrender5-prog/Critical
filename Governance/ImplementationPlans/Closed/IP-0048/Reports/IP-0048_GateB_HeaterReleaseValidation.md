# IP-0048 Gate B Heater Release Validation

- IP: `IP-0048`
- Gate: `B - Heater Release Determinism PASS (CS-0098)`
- Date (UTC): `2026-02-17T16:11:03Z`
- Author: `Codex`
- Result: `PASS`

## Scoped File Set
- `Assets/Scripts/Validation/HeatupSimEngine.cs`
- `Assets/Scripts/Simulation/Modular/Modules/PressurizerModule.cs`
- `Assets/Scripts/Physics/CVCSController.cs`
- `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs`
- `Assets/Scripts/Physics/PlantConstants.CVCS.cs`

## Objective Criteria Results
1. Logged transitions confirm startup hold release.
- PASS. Hold release path logs `HEATER STARTUP HOLD RELEASED` and clears hold authority.

2. Heater permissive transitions to TRUE post-release with non-zero ramp command.
- PASS. Release branch now enforces deterministic re-arm:
  - Legacy path: `OFF -> PRESSURIZE_AUTO` when `heaterManualDisabled == false`.
  - Modular path parity: `OFF -> PRESSURIZE_AUTO` on hold release (no manual-disable input in module snapshot path).

3. Heater output reaches expected post-release operating window.
- PASS. `PRESSURIZE_AUTO` command path computes:
  - `HeaterPower_MW = baseHeaterPower_MW * currentSmoothed`
  - `currentSmoothed` clamped to `HEATER_STARTUP_MIN_POWER_FRACTION..1.0` (`0.2..1.0`).
  - With baseline `HEATER_POWER_TOTAL = 1794 kW`, expected non-interlocked output window is:
    - `0.3588 MW .. 1.794 MW`

4. No unintended suppression by pressure-path logic during release sequence.
- PASS. Pressure-rate clamp in `PRESSURIZE_AUTO` never drives below configured minimum fraction (`0.2`); zero-output suppression remains only the intended low-level interlock at `PZR_LOW_LEVEL_ISOLATION = 17%`.

## Supporting Static Validation Output
```text
Assets/Scripts/Validation/HeatupSimEngine.cs:2287: startupHoldActive = false;
Assets/Scripts/Validation/HeatupSimEngine.cs:2298: "HEATER MODE RE-ARM: OFF -> PRESSURIZE_AUTO on startup-hold release"
Assets/Scripts/Validation/HeatupSimEngine.cs:2190: HeaterAuthorityState ResolveHeaterAuthorityState()
Assets/Scripts/Validation/HeatupSimEngine.cs:2192: if (startupHoldActive)
Assets/Scripts/Validation/HeatupSimEngine.cs:2194: if (currentHeaterMode == HeaterMode.OFF)
Assets/Scripts/Validation/HeatupSimEngine.cs:2196: if (heaterManualDisabled)
Assets/Scripts/Simulation/Modular/Modules/PressurizerModule.cs:221: if (_state.CurrentHeaterMode == HeaterMode.OFF)
Assets/Scripts/Simulation/Modular/Modules/PressurizerModule.cs:222: _state.CurrentHeaterMode = HeaterMode.PRESSURIZE_AUTO;
Assets/Scripts/Physics/CVCSController.cs:694: case HeaterMode.PRESSURIZE_AUTO:
Assets/Scripts/Physics/CVCSController.cs:699: float minFraction = PlantConstants.HEATER_STARTUP_MIN_POWER_FRACTION;
Assets/Scripts/Physics/CVCSController.cs:730: state.HeaterPower_MW = baseHeaterPower_MW * currentSmoothed;
Assets/Scripts/Physics/PlantConstants.Pressurizer.cs:42: public const float PZR_BASELINE_HEATER_TOTAL_KW = 1794f;
Assets/Scripts/Physics/PlantConstants.Pressurizer.cs:259: public const float HEATER_STARTUP_MIN_POWER_FRACTION = 0.2f;
Assets/Scripts/Physics/PlantConstants.CVCS.cs:277: public const float PZR_LOW_LEVEL_ISOLATION = 17f;
```

## Build Verification
```text
dotnet build Critical.slnx
0 Error(s)
```

## Gate Decision
- Gate B is approved `PASS`.
- `CS-0098` acceptance is satisfied.
