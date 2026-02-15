# PZR Bubble Behaviour Investigation - 2026-02-15

## Scope
Investigate unusual PZR level behaviour during bubble stabilization/pressurization and determine whether the bubble two-phase closure path shows:
- convergence failure,
- pressure unit inconsistency (psia/psig),
- double-counted mass/volume terms,
- discontinuous regime switching.

## Run Stamp And Artifacts
- Investigation run stamp: `PZR_INVEST_20260215_173006`
- Suite summary: `HeatupLogs/PZR_INVEST_20260215_173006/PZR_Bubble_Investigation_Suite_20260215_173006.md`
- Batch log: `_batch/pzr_investigation_unity.log`

Per-run artifacts:
- Baseline: `HeatupLogs/PZR_INVEST_20260215_173006/BASELINE_20260215_173006/`
- Smaller timestep: `HeatupLogs/PZR_INVEST_20260215_173006/SMALLER_TIMESTEP_20260215_173008/`
- Ambient clamp disabled: `HeatupLogs/PZR_INVEST_20260215_173006/NO_AMBIENT_CLAMP_20260215_173011/`

## Instrumentation Changes (Temporary / Gated)
No functional remediation was implemented. Only diagnostics and test harness support were added.

1. Bubble closure deep diagnostics gate in `Assets/Scripts/Validation/HeatupSimEngine.cs:125`:
- `enablePzrBubbleDiagnostics`
- `pzrBubbleDiagnosticsLabel`
- `pzrBubbleDiagnosticsResidualTolerance_ft3`
- `pzrBubbleDiagnosticsMaxIterations`

2. Per-step and per-iteration closure diagnostics in `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs`:
- Step input log with explicit psia/psig and Tsat(P): `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:763`
- Actual closure update log: `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:816`
- Final step log with residual/tolerance/status: `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:872`
- Residual equation captured explicitly:
  `ResidualEq_ft3=(m_liquid/rho_liquid)+(m_steam/rho_steam)-V_total`
- Shadow iterative convergence trace and status:
  `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:936`

3. Ambient floor clamp test switch in `Assets/Scripts/Physics/SolidPlantPressure.cs:139`:
- `DisableAmbientPressureFloorForDiagnostics`
- Clamp branch at `Assets/Scripts/Physics/SolidPlantPressure.cs:734`

4. Investigation runner (3 bracketed runs) in `Assets/Scripts/UI/Editor/PzrBubbleInvestigationRunner.cs:53`.

## Reproduction Procedure
Batch command used:
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.4f1\Editor\Unity.exe" `
  -batchmode -nographics `
  -projectPath "c:\Users\craig\Projects\Critical" `
  -executeMethod Critical.Validation.PzrBubbleInvestigationRunner.RunAll `
  -logFile "c:\Users\craig\Projects\Critical\_batch\pzr_investigation_unity.log" `
  -quit
```

Runner executes:
- `BASELINE` (`dt=1/360 hr`, ambient clamp enabled)
- `SMALLER_TIMESTEP` (`dt=1/720 hr`, ambient clamp enabled)
- `NO_AMBIENT_CLAMP` (`dt=1/360 hr`, ambient clamp disabled for test)

Batch completion evidence:
- `_batch/pzr_investigation_unity.log:143517`
- `_batch/pzr_investigation_unity.log:417720`
- `_batch/pzr_investigation_unity.log:560639`
- `_batch/pzr_investigation_unity.log:560651`

## State/Phase Where Anomaly Was Captured
Primary anomaly captured during:
- `BUBBLE_STABILIZE_MASS_CLOSURE`
- `BUBBLE_PRESSURIZE_MASS_CLOSURE`

Phase controls involved:
- STABILIZE min water floor uses 25%: `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:252`
- PRESSURIZE min water floor uses 23%: `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:667`

## Key Signal Evidence (Cliff/Overshoot)
From run summaries:
- Baseline (`PZR_Bubble_RunSummary_BASELINE.md`):
  - Steepest pressure drop: `-852.98 psi/hr` at `8.1664 hr`
  - Steepest pressure rise: `726.07 psi/hr` at `9.0165 hr`
  - Largest level down-step: `-2.000%` at `9.0137 hr`
- Smaller timestep (`PZR_Bubble_RunSummary_SMALLER_TIMESTEP.md`):
  - Steepest pressure drop: `-1391.45 psi/hr` at `8.2391 hr`
  - Steepest pressure rise: `1175.52 psi/hr` at `8.1808 hr`
  - Largest level down-step: `-2.000%` at `8.9709 hr`

## Key Log Excerpts (Short)
From baseline diagnostic log `HeatupLogs/PZR_INVEST_20260215_173006/BASELINE_20260215_173006/PZR_Bubble_Diagnostics_BASELINE.log`:

1. STABILIZE step (large closure residual):
```text
[PZR_BUBBLE_DIAG][BASELINE] STEP source=BUBBLE_STABILIZE_MASS_CLOSURE sim_hr=8.8443 ... pressure_used_psia=427.205 pressure_used_psig=412.505 ... V_total_ft3=5797.795 ... ResidualValue_ft3=3997.795
```

2. STABILIZE shadow solver status:
```text
[PZR_BUBBLE_DIAG][BASELINE] SHADOW_STATUS source=BUBBLE_STABILIZE_MASS_CLOSURE sim_hr=9.0082 iterations_used=12 ... status=failed reason=MAX_ITER_REACHED ... final_residual_ft3=4977.0990
```

3. PRESSURIZE step (residual worsens):
```text
[PZR_BUBBLE_DIAG][BASELINE] STEP source=BUBBLE_PRESSURIZE_MASS_CLOSURE sim_hr=9.0110 ... pressure_used_psia=443.872 pressure_used_psig=429.172 ... V_total_ft3=6831.618 ... ResidualValue_ft3=5031.618
```

4. PRESSURIZE shadow solver status:
```text
[PZR_BUBBLE_DIAG][BASELINE] SHADOW_STATUS source=BUBBLE_PRESSURIZE_MASS_CLOSURE sim_hr=9.0110 iterations_used=12 ... status=failed reason=MAX_ITER_REACHED ... final_residual_ft3=4993.6480
```

## Quantitative Bracket Results
1. Convergence behavior:
- Baseline SHADOW_STATUS count: `269`, failed `269`, converged `0`
- Smaller timestep SHADOW_STATUS count: `518`, failed `517`, converged `1`
- NO_AMBIENT_CLAMP SHADOW_STATUS count: `269`, failed `269`, converged `0`

2. Residual magnitude (baseline):
- STEP_FINAL residual range: `23.931 .. 5028.432 ft^3`
- STEP_FINAL residual average: `2620.331 ft^3`
- SHADOW_STATUS by source:
  - DRAIN: max `3948.524 ft^3`
  - STABILIZE: max `4977.099 ft^3`
  - PRESSURIZE: `4993.648 ft^3`

3. Solver step-size cap evidence:
- Max pressure step in baseline shadow updates: `0.277778 psi`
- Max pressure step in smaller-timestep run: `0.138889 psi`
- With residuals in the thousands of ft^3, this cap prevents practical closure to tolerance.

4. Ambient clamp bracket:
- Baseline and NO_AMBIENT_CLAMP diagnostic streams are identical except label (`DIAG_LOGS_IDENTICAL_EXCEPT_LABEL=True`).
- For this scenario, ambient floor clamp did not drive the anomaly.

5. Pressure units consistency check:
- Parsed baseline STEP logs show `(psia - psig) - 14.7` max absolute error `0.001`.
- No evidence of psia/psig mix-up in the instrumented closure path.

## Diagnosis Shortlist (Ranked)
1. Most likely: closure solver not converging under current update constraints.
- Evidence: near-total `SHADOW_STATUS=failed`, large residuals (up to ~5000 ft^3), step cap too small relative to residual scale.

2. Highly likely: closure residual is being bypassed by post-residual volume renormalization.
- Code rescales `(V_liquid, V_steam)` to `PZR_TOTAL_VOLUME` even when residual is large: `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:836`.
- This masks closure error and can create discontinuous level/pressure behavior.

3. Likely contributor: discontinuous phase-level floor switch (25% -> 23%).
- STABILIZE uses 25% floor, PRESSURIZE uses 23% floor (`Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:252`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:667`).
- Observed `-2.000%` one-step level drop aligns with a discrete floor change.

4. Unlikely: psia/psig unit inconsistency in this code path.
- Instrumentation indicates consistent psia/psig relation and Tsat(P) usage.

5. Unlikely in this reproduction: ambient pressure floor clamp causing bubble anomaly.
- Clamp-disabled bracket produced identical diagnostic behavior.

6. Not evidenced as primary: direct mass double-counting in the investigated closure path.
- The stronger issue is unresolved residual plus forced volumetric normalization, not a clear duplicate mass accumulation term in this path.

## Recommended Corrective Actions (Not Implemented)
1. Replace single-step/damped update + renormalization with a bounded iterative closure solve that enforces residual tolerance before accepting state updates.
2. Remove or strictly gate post-residual volume renormalization; treat large residual as solve failure and hold/rollback with explicit operator diagnostics.
3. Unify or smooth min-water-floor policy across STABILIZE/PRESSURIZE transition to remove discrete 2% level jumps.
4. Promote current diagnostics into a permanent debug mode with per-phase convergence KPIs and alert thresholds.
5. Add automated regression checks on:
- residual convergence rate,
- max allowed residual in bubble phases,
- continuity bounds for pressure/level across phase transitions.

## Implementation Status
- Instrumentation and runner added.
- Bracket runs executed and logged.
- No corrective solver behavior changes applied in this investigation step.
