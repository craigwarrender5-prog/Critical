# IP-0019 Stage E Extended Validation Report

- Date: 2026-02-15
- Run stamp: `20260215_085052`
- IP Reference: `IP-0019`
- DP Reference: `DP-0001`
- Adjacent DP handling: monitor-only, no cross-domain remediation.
- Artifact root: `C:/Users/craig/Projects/Critical/HeatupLogs/IP-0019_Extended_20260215_085052`

## Run Validity
- Status: VALID
- Sufficiency gate: duration>=3.0 hr and points>=1080

## 1) Long Solid Hold
- Alignment: explicit isolated long-hold start at t=0.000 hr (isolated window search disabled).
- Enforced boundaries: RCP forced 0, charging=0 gpm, letdown=0 gpm, RHR forced STANDBY (no helper thermal injection).
- Enforced boundaries: heater injection disabled (heater mode forced OFF).
- Duration: 3.000 hr (completed>=3hr hold: YES)
- Samples collected: 1081 (minimum 1080)
- No RCP active: maxRCP=0
- Boundary observations: max|chg|=0.000 gpm, max|letdown|=0.000 gpm, max|netCVCS|=0.000 gpm
- Boundary observations: max|chg2RCS|=0.000 gpm, heater observed=N, max|RHR net heat|=0.000 MW
- Heater injection disabled across hold: YES
- Pressure write audit: writes=1080, state-derived=1080, overrideAttempts=0, blockedOverrides=0
- Pressure override probe blocked (non-state write): YES
- Pressure write invariant failed: NO (lastSource=REGIME1_ISOLATED_RCSHeatup)
- Pressure override probe detail: Non-state pressure write blocked from LONG_HOLD_OVERRIDE_PROBE at T+0.0000hr (114.700->115.700 psia). tick=1 regime=UNSET writer=INIT solid=True bubble=False rcp=0 rhr=Heatup eqBranch=UNSET satUsed=False satPsia=0.000 rho=0.0000 kappa=0.000E+000 dP_model=0.000000
- Boundary observations: max no-RCP transport factor=1.000, any non-standby RHR mode=N
- T_rcs trend: 100.000 -> 99.367 F (slope -0.211 F/hr)
- Pressure trend: 114.700 -> 99.182 psia (slope -5.173 psi/hr)
- Surge flow: min -0.003, max 0.000, mean -0.003 gpm
- Oscillation amplitude (pressure P2P): 15.518 psi
- Mass conservation drift (max abs): 0.000 lbm

## 2) RCP Start Transient Stress
- Bubble reached: YES at t=11.417 hr
- Start/stop events: 3/3
- PZR level transient max: 0.223%
- Pressure overshoot envelope max: 92.500 psi
- Max one-step pressure delta: 10.707 psi
- Heat-rate smoothing proxy (max dRcpHeat per step): 0.017 MW
- Cycle 1: start=Y stop=Y maxLevelDelta=0.223%
- Cycle 2: start=Y stop=Y maxLevelDelta=0.223%
- Cycle 3: start=Y stop=Y maxLevelDelta=0.222%

## 3) Extended Heat-Up Window
- First RCP start detected: YES at t=11.592 hr
- Window: 10.592 -> 14.592 hr (duration 3.995 hr)
- Transition coverage: pre-noRCP=Y post-start=Y stabilized-RCP4=Y
- Post-start heat-rate max step: 3.321 F/hr
- Equilibrium ceiling behavior (pre no-RCP): dT=0.394 F, slope=0.396 F/hr
- Pressure/temperature coupling ratio: 100.00%
- Slow drift (final hour): T slope=60.247 F/hr, P slope=27.016 psi/hr
- RHR isolation observation: detected=N (no Heatup->isolation transition observed in this run).
- RHR isolation evaluation sample (CS-0056): captured=N trigger=UNSET
- RHR isolation window reachability: max post-start T_rcs=230.021 F, threshold=345.0 F, reached=N
- RHR isolation evaluation sample details: N/A (no valid post-start near-345.0 F sample captured).
- Thermodynamic writer states observed: REGIME1_ISOLATED, REGIME1_SOLID, REGIME2_BLEND, REGIME3_COUPLED
- Writer rule (CS-0071): pass=Y, conflicts=0, illegalPostMutation=0, windowedChecks=5244, skippedOutsideWindow=9
- Writer transition: t=0.0028 hr INIT->REGIME1_SOLID (solid=True, rcp=0, bubble=False, T_rcs=100.001 F, P=114.887 psia)
- Writer transition: t=8.0998 hr REGIME1_SOLID->REGIME1_ISOLATED (solid=False, rcp=0, bubble=False, T_rcs=103.239 F, P=363.794 psia)
- Writer transition: t=11.5918 hr REGIME1_ISOLATED->REGIME2_BLEND (solid=False, rcp=1, bubble=True, T_rcs=104.647 F, P=421.855 psia)
- Writer transition: t=13.7503 hr REGIME2_BLEND->REGIME3_COUPLED (solid=False, rcp=4, bubble=True, T_rcs=179.681 F, P=526.984 psia)
- Writer solid expectation: t=0.0083 hr expected(solid=true,rcp=0) observed(solid=True,rcp=0,bubble=False,RHR=Heatup)
- Writer solid expectation: t=0.0111 hr expected(solid=true,rcp=0) observed(solid=True,rcp=0,bubble=False,RHR=Heatup)
- Writer solid expectation: t=0.0139 hr expected(solid=true,rcp=0) observed(solid=True,rcp=0,bubble=False,RHR=Heatup)
- Writer solid expectation: t=0.0167 hr expected(solid=true,rcp=0) observed(solid=True,rcp=0,bubble=False,RHR=Heatup)
- Writer solid expectation: t=0.0194 hr expected(solid=true,rcp=0) observed(solid=True,rcp=0,bubble=False,RHR=Heatup)
- Writer solid expectation: t=0.0222 hr expected(solid=true,rcp=0) observed(solid=True,rcp=0,bubble=False,RHR=Heatup)
- Writer solid expectation: t=0.0250 hr expected(solid=true,rcp=0) observed(solid=True,rcp=0,bubble=False,RHR=Heatup)
- Writer solid expectation: t=0.0278 hr expected(solid=true,rcp=0) observed(solid=True,rcp=0,bubble=False,RHR=Heatup)
- Writer solid expectation: t=0.0306 hr expected(solid=true,rcp=0) observed(solid=True,rcp=0,bubble=False,RHR=Heatup)
- Writer solid expectation: t=0.0333 hr expected(solid=true,rcp=0) observed(solid=True,rcp=0,bubble=False,RHR=Heatup)
- Writer solid expectation: t=0.0361 hr expected(solid=true,rcp=0) observed(solid=True,rcp=0,bubble=False,RHR=Heatup)
- Writer solid expectation: t=0.0389 hr expected(solid=true,rcp=0) observed(solid=True,rcp=0,bubble=False,RHR=Heatup)

## 4) Repeatability Check
- Completed: YES (points=1081)
- Max delta T_rcs: 0.000000E+000 F
- Max delta pressure: 0.000000E+000 psi
- Max delta surge: 0.000000E+000 gpm
- Max delta mass drift: 0.000000E+000 lbm

## Per-CS Status (11)
- CS-0021: PASS - LongHold pressure P2P=15.518 psi.
- CS-0022: PASS - Surge-pressure consistency=100.00%.
- CS-0023: PASS - Sign-consistent surge/pressure ratio=100.00%.
- CS-0031: PASS - Stress max dRcpHeat/step=0.017 MW, max dP/step=10.707 psi; extended max dHeatRate/step=3.321 F/hr.
- CS-0033: PASS - No-RCP T_rcs slope=-0.211 F/hr.
- CS-0034: PASS - Pre-start no-RCP dT=0.394 F.
- CS-0038: PASS - RCP stress cycles start/stop=3/3; max level delta=0.223%.
- CS-0055: PASS - LongHold no-RCP slope=-0.211 F/hr with maxRCP=0.
- CS-0056: NOT_REACHED - Post-start near-350 window not reached in this run (max T_rcs=230.021 F < threshold=345.0 F); isolation sequence cannot be evaluated.
- CS-0061: CONDITIONAL - Mass drift max abs=0.000 lbm.
- CS-0071: PASS - Writer validation conflicts=0, illegalPostMutation=0, windowedChecks=5244, skippedOutsideWindow=9.

## Closure Recommendation
- CLOSE_RECOMMENDED
- Reason: CS-0056=NOT_REACHED treated as non-blocking by policy (sequence correctness is evaluated only if the near-350F window is reached).
- IP-0019 closure recommendation accepted; plan status transitioned to `CLOSED`.
