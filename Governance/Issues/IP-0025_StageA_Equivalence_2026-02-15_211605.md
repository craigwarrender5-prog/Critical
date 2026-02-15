# IP-0025 Stage A - Deterministic Equivalence

- Timestamp: 2026-02-15 21:16:07
- Run stamp: `2026-02-15_211605`
- Result: PASS

## Deterministic Controls
- Fixed random seed: `250025`
- Fixed timestep: `0.002778 hr`
- Fixed init profile: cold-shutdown baseline (`startTemperature=100F`, `startPressure=PlantConstants.PRESSURIZE_INITIAL_PRESSURE_PSIA`, `startPZRLevel=100%`)

## Scenarios
- Legacy baseline path steps: `360`
- Coordinator+Legacy path steps: `360`
- Baseline used legacy-only path every step: `True`
- Coordinator path used every step: `True`

## Tolerances
- Pressure tolerance: `0.001000 psia`
- PZR level tolerance: `0.000100 %`
- Primary mass tolerance: `0.001000 lb`

## Max Observed Error
- Pressure: `0.000000 psia` at step `0`
- PZR level: `0.000000 %` at step `0`
- Primary mass: `0.000000 lb` at step `0`

## Artifacts
- Run directory: `HeatupLogs/IP-0025_StageA_Equivalence_2026-02-15_211605`
- Baseline samples: `HeatupLogs/IP-0025_StageA_Equivalence_2026-02-15_211605/baseline_samples.csv`
- Coordinator samples: `HeatupLogs/IP-0025_StageA_Equivalence_2026-02-15_211605/coordinator_samples.csv`
- Comparison samples: `HeatupLogs/IP-0025_StageA_Equivalence_2026-02-15_211605/comparison.csv`
