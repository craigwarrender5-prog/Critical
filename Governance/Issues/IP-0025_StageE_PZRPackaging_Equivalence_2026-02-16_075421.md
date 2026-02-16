# IP-0025 Stage E - PZR Packaging Equivalence

- Timestamp: 2026-02-16 07:54:23
- Run stamp: `2026-02-16_075421`
- Result: PASS
- Steps compared: `360`

## Feature Flags / Run Modes
- Baseline run (`LEGACY_PZR`):
  - `ModularFeatureFlags.EnableCoordinatorPath = true`
  - `ModularFeatureFlags.UseModularPZR = false`
  - `ModularFeatureFlags.BypassLegacyPZR = false`
  - `ModularFeatureFlags.EnableComparatorPZR = false`
- Modular authoritative run (`MODULAR_PZR`):
  - `ModularFeatureFlags.EnableCoordinatorPath = true`
  - `ModularFeatureFlags.UseModularPZR = true`
  - `ModularFeatureFlags.BypassLegacyPZR = true`
  - `ModularFeatureFlags.EnableComparatorPZR = false`

## Deterministic Controls
- Fixed random seed: `250025`
- Fixed timestep: `0.002778 hr`
- Fixed init profile: cold shutdown start with deterministic startup values.

## Tolerances
- Pressure: `1.00E-003 psia`
- PZR level: `1.00E-004 %`
- Heater power: `1.00E-004 MW`
- Spray flow: `1.00E-003 gpm`
- PZR water volume: `1.00E-003 ft^3`
- PZR steam volume: `1.00E-003 ft^3`

## Max Observed Error (Legacy PZR vs Modular PZR)
- Pressure: `0.000E+000 psia` at step `0`
- PZR level: `0.000E+000 %` at step `0`
- Heater power: `0.000E+000 MW` at step `0`
- Spray flow: `0.000E+000 gpm` at step `0`
- PZR water volume: `0.000E+000 ft^3` at step `0`
- PZR steam volume: `0.000E+000 ft^3` at step `0`

## Ledger and Mutation Gates (Modular PZR)
- Unledgered mutation count: `0`
- Missing heater intents: `0`
- Missing spray intents: `0`
- Missing surge intents: `0`

## Artifacts
- Run directory: `HeatupLogs/IP-0025_StageE_2026-02-16_075421`
- Baseline samples: `HeatupLogs/IP-0025_StageE_2026-02-16_075421/baseline_samples.csv`
- Modular samples: `HeatupLogs/IP-0025_StageE_2026-02-16_075421/modular_samples.csv`
- Comparison: `HeatupLogs/IP-0025_StageE_2026-02-16_075421/comparison.csv`
