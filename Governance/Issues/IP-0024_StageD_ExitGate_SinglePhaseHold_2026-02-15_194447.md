# IP-0024 Stage D Exit Gate - Single-Phase Equilibrium Hold

- Timestamp: 2026-02-15 19:44:48
- Run stamp: `2026-02-15_194447`
- Setup: ColdShutdownProfile, startup hold active, heater mode OFF, spray OFF path, no-net-transfer pinning
- Hold duration: 60 min
- Log directory: `HeatupLogs/IP-0024_StageD_Hold_2026-02-15_194447`
- Heatup report: `HeatupLogs/IP-0024_StageD_Hold_2026-02-15_194447/Heatup_Report_20260215_194448.txt`
- Timeseries CSV: `HeatupLogs/IP-0024_StageD_Hold_2026-02-15_194447/IP-0024_StageD_SinglePhaseHold_Timeseries.csv`

## Summary Table
| Metric | Start | End | Max Drift |
|---|---:|---:|---:|
| PZR mass (lbm) | 110527.2000 | 110527.2000 | 0.0000 |
| PZR total enthalpy (BTU) | 10120390.00 | 10120390.00 | 0.00 |
| Pressure (psia) | 114.7000 | 102.0414 | 12.6586 |

## Contract Checks
- Δm_total=0.000000 lbm (tol ±50.0)
- max|m_total-m0|=0.000000 lbm (tol 75.0)
- Δu_total=0.000 BTU
- ∫Q_net dt=0.000 BTU
- |Δu_total-∫Q_net dt|=0.000 BTU (tol 25000.0)
- Pressure drift=-12.658600 psia | drift explained by energy model=False
- Net transfer mean=-0.002690 gpm, max|net|=0.002698 gpm (tol 0.25)
- MASS_CONTRACT_RESIDUAL violations=0
- Heater energization at frame 1=False
- Any spray activity during hold=True

## Exit Gate Decision: FAIL
