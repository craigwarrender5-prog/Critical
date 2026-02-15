# IP-0025 Stage B - Snapshot Fidelity and Immutability

- Timestamp: 2026-02-15 21:23:01
- Run stamp: `2026-02-15_212300`
- Result: PASS
- Steps compared: `360`

## Compared Fields (Direct Legacy vs StepSnapshot.PlantState)
- `simTime -> PlantState.TimeHr` tolerance=`1.00E-006` maxError=`0.00E+000` worstStep=`0`
- `pressure -> PlantState.PressurePsia` tolerance=`1.00E-006` maxError=`0.00E+000` worstStep=`0`
- `T_avg -> PlantState.TavgF` tolerance=`1.00E-006` maxError=`0.00E+000` worstStep=`0`
- `T_rcs -> PlantState.TrcsF` tolerance=`1.00E-006` maxError=`0.00E+000` worstStep=`0`
- `pzrLevel -> PlantState.PzrLevelPct` tolerance=`1.00E-006` maxError=`0.00E+000` worstStep=`0`
- `chargingFlow -> PlantState.ChargingFlowGpm` tolerance=`1.00E-006` maxError=`0.00E+000` worstStep=`0`
- `letdownFlow -> PlantState.LetdownFlowGpm` tolerance=`1.00E-006` maxError=`0.00E+000` worstStep=`0`
- `surgeFlow -> PlantState.SurgeFlowGpm` tolerance=`1.00E-006` maxError=`0.00E+000` worstStep=`0`
- `pzrHeaterPower -> PlantState.PzrHeaterPowerMw` tolerance=`1.00E-006` maxError=`0.00E+000` worstStep=`0`
- `reactorPower -> PlantState.ReactorPowerMw` tolerance=`1.00E-006` maxError=`0.00E+000` worstStep=`0`
- `primaryMassLedger_lb -> PlantState.PrimaryMassLedgerLb` tolerance=`1.00E-006` maxError=`0.00E+000` worstStep=`0`
- `primaryMassBoundaryError_lb -> PlantState.PrimaryMassBoundaryErrorLb` tolerance=`1.00E-006` maxError=`0.00E+000` worstStep=`0`
- `totalSystemMass_lbm -> PlantState.TotalSystemMassLb` tolerance=`1.00E-006` maxError=`0.00E+000` worstStep=`0`
- `plantMode -> PlantState.PlantMode` exactMatch=`True` mismatches=`0` firstMismatchStep=`0`
- `rcpCount -> PlantState.RcpCount` exactMatch=`True` mismatches=`0` firstMismatchStep=`0`
- `primaryMassConservationOK -> PlantState.PrimaryMassConservationOk` exactMatch=`True` mismatches=`0` firstMismatchStep=`0`
- `heatupPhaseDesc -> PlantState.HeatupPhaseDescription` exactMatch=`True` mismatches=`0` firstMismatchStep=`0`

## Snapshot Immutability Proof
- Public writable properties absent (`StepSnapshot` + `PlantState`): `True`
- Snapshot runtime containers immutable/read-only: `True`
- New snapshot object created per publish: `True`
- Prior snapshot values stable after next step: `True`

## Artifacts
- Run directory: `HeatupLogs/IP-0025_StageB_SnapshotFidelity_2026-02-15_212300`
- Fidelity CSV: `HeatupLogs/IP-0025_StageB_SnapshotFidelity_2026-02-15_212300/snapshot_fidelity.csv`
