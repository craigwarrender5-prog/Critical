# IP-0028 Stage D - Pressurizer Physics and Control Remediation

- IP: `IP-0028`
- DP: `DP-0012`
- Stage: `D`
- Timestamp: `2026-02-16_131529`
- In-scope CS: `CS-0081`, `CS-0091`, `CS-0093`
- Governing freeze: `Governance/Issues/IP-0028_StageA_BaselineFreeze_2026-02-16_124043.md`, `Governance/Issues/IP-0028_StageB_DesignFreeze_2026-02-16_124209.md`

## 1) Stage D implementation changes

1. `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs`
   - Added startup-phase continuity-bounded projected-step commit path when solved pressure-step exceeds frozen continuity bound (`25 psia`).
   - Preserved explicit residual/energy bounds for projected/fallback acceptance (`|Vres| <= 5.0 ft^3`, energy residual <= computed tolerance).
   - Preserved explicit fail/alert paths when bounded acceptance cannot be satisfied (no silent commit).
   - Startup ceiling for startup bubble-control phases remains bounded to documented startup transition envelope (`425 psig + 14.7`).

## 2) Technical documentation traceability used in this stage

1. `Technical_Documentation/PZR_Baseline_Profile.md` (`DOC-CF-03`): 320-400 psig operating control baseline.
2. `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md` (startup target envelope): startup transition target behavior (400-425 psig context).
3. `Technical_Documentation/PZR_Level_Pressure_Deficit_Analysis_v4.4.0.md`: residual/non-convergence concerns and masking-risk context for CS-0091/CS-0093.

## 3) Validation run stamps and inputs

Run method:
- Unity execute method: `Critical.Validation.IP0028ExecutionRunner.RunDeterministicTriple`
- Unity log: `HeatupLogs/IP0028_StageDE_Unity.log`
- Suite root: `HeatupLogs/IP-0028_StageC_20260216_131255`
- Config inputs: `dt_hr=0.00277778`, `maxSimHr=12.0`, `intervalLogHr=0.25`, cold-start profile baseline (`HeatupSimEngine.Init.cs`).

Per-run artifacts:
1. `HeatupLogs/IP-0028_StageC_20260216_131255/RUN_A_20260216_131255/IP-0028_RUN_A_StepTelemetry.csv`
2. `HeatupLogs/IP-0028_StageC_20260216_131255/RUN_A_20260216_131255/IP-0028_RUN_A_EventSequence.csv`
3. `HeatupLogs/IP-0028_StageC_20260216_131255/RUN_B_20260216_131256/IP-0028_RUN_B_StepTelemetry.csv`
4. `HeatupLogs/IP-0028_StageC_20260216_131255/RUN_C_20260216_131257/IP-0028_RUN_C_StepTelemetry.csv`
5. Determinism summary: `HeatupLogs/IP-0028_StageC_20260216_131255/IP-0028_StageC_Determinism_20260216_131255.md`

## 4) CS-0081 validation (control-band vs relief/protection separation)

Observed (RUN_A):
- `HOLD_SOLID` with `bubble_phase=NONE`: pressure range `349.0148-370.7239 psia` (within 320-400 psig operating control baseline).
- Startup drain transition (`bubble_phase=DRAIN`) includes bounded projected points up to `439.551 psia` (`~424.851 psig`), matching documented startup transition envelope context.
- Continuity bound enforcement: maximum drain step delta `25 psia` (no step exceeds bound).

Disposition:
- **PASS with documented startup-transition deviation context**.
- Control-band behavior is conformant in steady `HOLD_SOLID` control operation.
- Pressure values above 400 psig observed only in startup transition handling and are explicitly bounded/traceable.

## 5) CS-0091 validation (convergence + residual masking prohibition)

Evidence (all three reruns):
- `closure_converged=1` for all sampled steps in RUN_A/B/C (`nonConv=0`).
- Residual bounds observed in RUN_A: `closure_volume_residual_ft3` range `-2.467896` to `4.022949` (within explicit bounded acceptance).
- Event evidence (per run): `projected step=3`, `fallback=2`, `jumpBlocked=0`.
- All bounded fallback/projected commits are explicit `ALERT` events; no silent path.

Disposition:
- **PASS**.
- No silent residual masking path remains in runtime behavior; bounded non-nominal commits are explicit, logged, and numerically bounded.

## 6) CS-0093 validation (PZR remodel/control remediation behavior)

Deterministic metrics:
- Event ordering identical across RUN_A/RUN_B/RUN_C: PASS.
- Limiter activation sequence identical across RUN_A/RUN_B/RUN_C: PASS.
- Hold-release gate behavior identical across RUN_A/RUN_B/RUN_C: PASS.
- DRAIN continuity bound identical across reruns (`max step=25 psia`).

Traceability and acceptance statement:
- Runtime changes are constrained to documented/Stage B-frozen authority + bounded continuity behavior.
- Residual and continuity acceptance metrics are explicit and repeatable across reruns.

Disposition:
- **PASS** for Stage D gate progression.

## 7) Exit criteria (Stage D)

1. Pressure-band behavior conforms to documentation baseline or has documented justified deviation: **PASS**
2. Convergence and residual acceptance thresholds satisfied in validation runs: **PASS**
3. No silent residual masking paths remain: **PASS**
4. Control-band vs relief/protection boundaries explicitly validated and traceable: **PASS**
5. Phase-transition continuity checks pass within declared bounds: **PASS**

## 8) Stage D affected files

1. `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs` - bounded projected-step continuity commit path with explicit diagnostics and bounded acceptance.
