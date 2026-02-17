# IP-0048 Gate A Cold Shutdown Baseline

- IP: `IP-0048`
- Gate: `A - Cold Shutdown Determinism PASS (CS-0101)`
- Date (UTC): `2026-02-17T16:11:03Z`
- Author: `Codex`
- Result: `PASS`

## Scoped File Set
- `Assets/Scripts/Validation/HeatupSimEngine.Init.cs`
- `Assets/Scripts/Validation/HeatupSimEngine.cs`
- `Assets/Scripts/Physics/PlantConstants.Pressure.cs`
- `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs`
- `Assets/Scripts/Physics/PlantConstants.CVCS.cs`
- `Assets/Scripts/Physics/WaterProperties.cs`

## Baseline Parameter Table (Frozen)
| Parameter | Value | Source |
|---|---:|---|
| Initial pressure | `114.7 psia` (`100 psig`) | `PlantConstants.PRESSURIZE_INITIAL_PRESSURE_PSIA` |
| Initial temperature | `120.0 F` | `ColdShutdownProfile.CreateApprovedBaseline()` |
| PZR total volume | `1800 ft^3` | `PlantConstants.PZR_TOTAL_VOLUME` |
| PZR liquid mass | `110,527.200525 lbm` | `PZR_TOTAL_VOLUME * WaterDensity(120F, 114.7 psia)` |
| PZR vapor mass | `0.0 lbm` | `ColdShutdownProfile.CreateApprovedBaseline()` |
| Heater mode baseline | `PRESSURIZE_AUTO` | `ColdShutdownProfile.CreateApprovedBaseline()` |
| Startup hold duration | `15 s` | `ColdShutdownProfile.CreateApprovedBaseline()` |
| CVCS baseline lineup | `1x75 gpm` letdown/charging | `ColdShutdownProfile.CreateApprovedBaseline()` |
| Solid pressure control band | `334.7-464.7 psia` | `PlantConstants.SOLID_PLANT_P_LOW_PSIA/HIGH_PSIA` |

## Startup Snapshot Artifact
- Baseline boot snapshot marker:
  - `T0=120.0F;P0=114.7psia;PZR0=100.0%;dt=0.002778hr;log=0.25hr`
- Idle baseline ownership marker:
  - `BOOT BASELINE READY (COLD_SHUTDOWN) - Awaiting StartSimulation command`

## Pre-Declared Numeric Tolerances
- `Pressure tolerance`: `<= 0.01 psia`
- `Temperature tolerance`: `<= 0.01 F`
- `PZR liquid mass tolerance`: `<= 0.10 lbm`
- `Discrete-state markers` (`heater mode`, `startup hold`, `lineup`): exact match required

Tolerance policy allows floating-point rounding noise but rejects any logic/state drift.

## 3-Run Reproducibility Check
```text
run1: P=114.7 T=120 M=110527.200525 Mode=PRESSURIZE_AUTO Hold=15s
run2: P=114.7 T=120 M=110527.200525 Mode=PRESSURIZE_AUTO Hold=15s
run3: P=114.7 T=120 M=110527.200525 Mode=PRESSURIZE_AUTO Hold=15s
pressure_variance_psia=0
temperature_variance_F=0
mass_variance_lbm=0
```

## Objective Criteria Results
1. Cold Shutdown parameter table produced and frozen.
- PASS.

2. Startup snapshot artifact captured from boot-initialized state.
- PASS.

3. 3-run reproducibility check passes within pre-declared numeric tolerances.
- PASS.

4. Boot remains baseline-idle pending explicit scenario start command.
- PASS (`runOnStart = false` default plus explicit idle baseline initialization path).

## Build Verification
```text
dotnet build Critical.slnx
0 Error(s)
```

## Gate Decision
- Gate A is approved `PASS`.
- `CS-0101` acceptance is satisfied and baseline is frozen for dependent IPs.
