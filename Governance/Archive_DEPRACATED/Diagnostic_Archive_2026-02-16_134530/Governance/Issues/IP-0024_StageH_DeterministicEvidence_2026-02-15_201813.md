# IP-0024 Stage H Deterministic Evidence Suite

- Timestamp: 2026-02-15 20:18:14
- Run stamp: `2026-02-15_201813`

## Run List
- Cold-shutdown equilibrium hold (Stage D reuse): `HeatupLogs/IP-0024_StageD_Hold_2026-02-15_201813`
- Startup + bubble + drain deterministic run: `HeatupLogs/IP-0024_StageH_Suite_2026-02-15_201813`
- Heatup report: `HeatupLogs/IP-0024_StageH_Suite_2026-02-15_201813/Heatup_Report_20260215_201814.txt`
- Startup sequence timeseries: `HeatupLogs/IP-0024_StageH_Suite_2026-02-15_201813/IP-0024_StartupSequence_Timeseries.csv`
- Bubble continuity/phase plot data: `HeatupLogs/IP-0024_StageH_Suite_2026-02-15_201813/IP-0024_PhaseContinuity_Timeseries.csv`
- Solver residual log: `HeatupLogs/IP-0024_StageH_Suite_2026-02-15_201813/IP-0024_SolverAttempts.csv`
- DRAIN causality log: `HeatupLogs/IP-0024_StageH_Suite_2026-02-15_201813/IP-0024_DrainCausality.csv`
- DRAIN causality micro-run log: `HeatupLogs/IP-0024_StageH_Suite_2026-02-15_201813/IP-0024_DrainCausality_MicroRun.csv`
- RVLIS integrity log: `HeatupLogs/IP-0024_StageH_Suite_2026-02-15_201813/IP-0024_RVLIS_DrainConsistency.csv`

## Convergence Dashboard
- Attempts: 421
- Converged: 420
- Convergence %: 99.76%
- Mean / Max |Volume residual|: 0.43510 / 0.99927 ft^3
- Mean / Max |Energy residual|: 0.56057 / 4.00000 BTU
- Pattern breakdown: monotonic=351, bounded_oscillatory=70, other=0
- Failure reason breakdown:
  - NONE: 421

## Bracket Search Diagnostics
- Bracket-only probe: attempted=True, found=True, time=7.8331hr, low=407.733 psia, high=409.558 psia, reason=BRACKET_FOUND
- Failure classes observed: NONE=421

## Startup / Continuity / Causality Checks
- Startup hold gating: PASS | frame1Heater=False, holdRelease=0.0083hr, modePermit=0.0306hr, firstHeaterOn=0.0333hr
- Bubble continuity: PASS | boundary=7.6609hr, maxStep|dP|=2.224 psi, maxStep|dLevel|=0.000%
- DRAIN causality: PASS | policyAligned=True, no1to3Jump=True, untriggeredLineupChanges=0, max|flow-hydraulic|=0.000 gpm, smoothEntry=True, microRunPass=True
- DRAIN event causality detail: untriggeredLineupChanges=0, untriggered1to3=0, triggeredLineupChanges=0
- DRAIN jump decomposition (first 1 events):
  - t=7.8359hr lineup 1->1 event=False dAch=75.000 dCap=75.000 dDP=8.759 dRho=52.159 dQ=0.00000 cause=SATURATION_FEEDBACK trigger=NONE
- DRAIN micro-run: executed=True, fixedLineupCapPass=True, explicitEventLogged=True, explicitEventIncreasePass=True
- DRAIN micro-run summary: fixedLineupCapPass=True, fixedMaxAchieved=75.005 gpm, fixedCapacity=75.005 gpm, preEventAchieved=75.005 gpm, postEventAchieved=120.000 gpm, explicitEventIncreasePass=True
- RVLIS consistency: FAIL | fullInvalid=0, upperInvalid=0, changeCoverage=0.02, signAlignment=1.00

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
- Failure reason: INFEASIBLE_ENERGY_FOR_MASS_VOLUME
- Pattern: NO_VALID_POINTS
- Residuals: V=0.00000 ft^3, E=0.00000 BTU
- Phase: DRAIN, phaseFraction=0.00000, iterations=0

## CS Pass/Fail Matrix
| CS | Evidence Run(s) | Metrics / Thresholds | Result |
|---|---|---|---|
| CS-0091 | Stage H deterministic run + forced failure check + bracket probe | convergence>=95%, MASS_CONTRACT_RESIDUAL=0, clean fail/no-commit true, bracket probe finds bracket, reason histogram populated | PASS |
| CS-0092 | Stage H DRAIN window + micro-run | policy=LINEUP_HYDRAULIC_CAUSAL, no untriggered lineup change, no untriggered 1->3 jump, max|flow-hydraulic|<=1.5 gpm, explicit-event micro-run proves causal step | PASS |
| CS-0093 | Stage H aggregate | CS-0091 && CS-0092 && bubble continuity && setpoint fidelity | PASS |
| CS-0094 | Stage H startup sequence | no heater at frame 1, first heater after hold release and mode permit | PASS |
| CS-0040 | Stage H DRAIN RVLIS | RVLIS full/upper valid; causal update coverage/alignment sustained | FAIL |
| CS-0081 | Runtime config check | SOLID band 320-400 psig, MIN_RCP=400 psig | PASS |

## Stage H Outcome: FAIL
