# IP-0024 Stage H Deterministic Evidence Suite

- Timestamp: 2026-02-15 19:45:57
- Run stamp: `2026-02-15_194555`

## Run List
- Cold-shutdown equilibrium hold (Stage D reuse): `HeatupLogs/IP-0024_StageD_Hold_2026-02-15_194555`
- Startup + bubble + drain deterministic run: `HeatupLogs/IP-0024_StageH_Suite_2026-02-15_194555`
- Heatup report: `HeatupLogs/IP-0024_StageH_Suite_2026-02-15_194555/Heatup_Report_20260215_194557.txt`
- Startup sequence timeseries: `HeatupLogs/IP-0024_StageH_Suite_2026-02-15_194555/IP-0024_StartupSequence_Timeseries.csv`
- Bubble continuity/phase plot data: `HeatupLogs/IP-0024_StageH_Suite_2026-02-15_194555/IP-0024_PhaseContinuity_Timeseries.csv`
- Solver residual log: `HeatupLogs/IP-0024_StageH_Suite_2026-02-15_194555/IP-0024_SolverAttempts.csv`
- DRAIN causality log: `HeatupLogs/IP-0024_StageH_Suite_2026-02-15_194555/IP-0024_DrainCausality.csv`
- RVLIS integrity log: `HeatupLogs/IP-0024_StageH_Suite_2026-02-15_194555/IP-0024_RVLIS_DrainConsistency.csv`

## Convergence Dashboard
- Attempts: 3660
- Converged: 0
- Convergence %: 0.00%
- Mean / Max |Volume residual|: 6.59690 / 6.76184 ft^3
- Mean / Max |Energy residual|: 438669.10000 / 438712.00000 BTU
- Pattern breakdown: monotonic=0, bounded_oscillatory=0, other=3660
- Failure reason breakdown:
  - NO_VOLUME_BRACKET: 3660

## Startup / Continuity / Causality Checks
- Startup hold gating: PASS | frame1Heater=False, holdRelease=0.0083hr, modePermit=0.0306hr, firstHeaterOn=0.0333hr
- Bubble continuity: PASS | boundary=7.6609hr, maxStep|dP|=2.224 psi, maxStep|dLevel|=0.000%
- DRAIN causality: FAIL | policyAligned=True, no1to3Jump=False, max|flow-hydraulic|=75.000 gpm, smoothEntry=True
- RVLIS consistency: PASS | fullInvalid=0, upperInvalid=0, changeCoverage=0.83, signAlignment=1.00

## Setpoint Fidelity
- PASS | Heater setpoint (operating): baseline=2235.000000, alias=2235.000000, delta=0.000000
- PASS | Heater proportional full ON: baseline=2220.000000, alias=2220.000000, delta=0.000000
- PASS | Heater proportional full ON alias: baseline=2220.000000, alias=2220.000000, delta=0.000000
- PASS | Heater proportional zero: baseline=2250.000000, alias=2250.000000, delta=0.000000
- PASS | Heater backup ON: baseline=2210.000000, alias=2210.000000, delta=0.000000
- PASS | Heater backup OFF: baseline=2217.000000, alias=2217.000000, delta=0.000000
- PASS | Spray start: baseline=2260.000000, alias=2260.000000, delta=0.000000
- PASS | Spray full: baseline=2310.000000, alias=2310.000000, delta=0.000000
- PASS | PORV threshold: baseline=2335.000000, alias=2335.000000, delta=0.000000
- PASS | Level program min: baseline=25.000000, alias=25.000000, delta=0.000000
- PASS | Level program max: baseline=61.500000, alias=61.500000, delta=0.000000
- PASS | Level program Tavg low: baseline=557.000000, alias=557.000000, delta=0.000000
- PASS | Level program Tavg high: baseline=584.700000, alias=584.700000, delta=0.000000
- PASS | Level program slope: baseline=1.317689, alias=1.317689, delta=0.000000

## Clean Failure / No-Commit Evidence
- Attempted: True
- Returned converged: False
- No-commit on failure: True
- Failure reason: NO_VOLUME_BRACKET
- Pattern: NO_BRACKET
- Residuals: V=19050100.00000 ft^3, E=403625200000.00000 BTU
- Phase: DRAIN, phaseFraction=0.00000, iterations=0

## CS Pass/Fail Matrix
| CS | Evidence Run(s) | Metrics / Thresholds | Result |
|---|---|---|---|
| CS-0091 | Stage H deterministic run + forced failure check | convergence>=95%, MASS_CONTRACT_RESIDUAL=0, clean fail/no-commit true | FAIL |
| CS-0092 | Stage H DRAIN window | policy=LINEUP_HYDRAULIC_CAUSAL, no 1->3 lineup jump, max|flow-hydraulic|<=1.5 gpm | FAIL |
| CS-0093 | Stage H aggregate | CS-0091 && CS-0092 && bubble continuity && setpoint fidelity | FAIL |
| CS-0094 | Stage H startup sequence | no heater at frame 1, first heater after hold release and mode permit | PASS |
| CS-0040 | Stage H DRAIN RVLIS | RVLIS full/upper valid; causal update coverage/alignment sustained | PASS |
| CS-0081 | Runtime config check | SOLID band 320-400 psig, MIN_RCP=400 psig | PASS |

## Stage H Outcome: FAIL
