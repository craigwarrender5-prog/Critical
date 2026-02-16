# IP-0028 Stage E - Indicator Integrity and System Regression

- IP: `IP-0028`
- DP: `DP-0012`
- Stage: `E`
- Timestamp: `2026-02-16_131703`
- In-scope CS for this stage: `CS-0040` (+ regression confirmation for full IP scope)

## 1) Stage E implementation change

1. `Assets/Scripts/Physics/RVLISPhysics.cs`
   - Added bounded over-range display allowance (`LEVEL_OVER_RANGE_MAX = 120f`).
   - Removed hard pinning at 100% by changing volume ratio/display clamps from `[0,100]` equivalent to `[0,120]` bounded over-range.
   - Intent: prevent stale-looking pinned indication during drain/fill transitions while keeping bounded instrumentation output.

## 2) Reproduction and root-cause isolation for CS-0040

Before-fix evidence (reproduced from pre-fix Stage C run):
- `HeatupLogs/IP-0028_StageC_20260216_124427/RUN_A_20260216_124427/IP-0028_RUN_A_StepTelemetry.csv`
- Drain window sample count: `71`
- `pzr_level_pct`: `100.000000 -> 94.299920`
- `rvlis_full`: `100.000000 -> 100.000000` (pinned/stale)

Root cause isolation:
- RVLIS calculation path hard-clamped ratio/output to 100% display ceiling, suppressing visible drain-time trend and presenting stale-looking indication.

After-fix evidence:
- `HeatupLogs/IP-0028_StageC_20260216_131255/RUN_A_20260216_131255/IP-0028_RUN_A_StepTelemetry.csv`
- Drain window sample count: `71`
- `pzr_level_pct`: `100.000000 -> 94.295150`
- `rvlis_full`: `100.943500 -> 100.790300` (now varying, no hard pin)

CS-0040 disposition:
- **PASS** (explicit before/after confirms stale pinned condition removed under the tested runtime path).

## 3) Regression run set and deterministic evidence

Run set:
- Unity log: `HeatupLogs/IP0028_StageDE_Unity.log`
- Suite root: `HeatupLogs/IP-0028_StageC_20260216_131255`
- Summary: `HeatupLogs/IP-0028_StageC_20260216_131255/IP-0028_StageC_Determinism_20260216_131255.md`

Determinism/non-regression checks:
1. Event ordering identical across reruns: PASS
2. Limiter activation sequence identical across reruns: PASS
3. Hold-release gating behavior identical across reruns: PASS
4. Event count stability: `200` events per run (no log storm)
5. Startup hold release timing stable: `0.008333 hr` in all runs

## 4) Full IP CS acceptance mapping (PASS/FAIL)

| CS ID | Requirement summary | Evidence location | PASS/FAIL |
|---|---|---|---|
| CS-0040 | RVLIS stale indicator during drain is resolved with explicit before/after evidence | Before: `HeatupLogs/IP-0028_StageC_20260216_124427/RUN_A_20260216_124427/IP-0028_RUN_A_StepTelemetry.csv`; After: `HeatupLogs/IP-0028_StageC_20260216_131255/RUN_A_20260216_131255/IP-0028_RUN_A_StepTelemetry.csv`; Code: `Assets/Scripts/Physics/RVLISPhysics.cs` | PASS |
| CS-0081 | Control-band behavior aligns with 320-400 psig baseline or documented justified startup-transition deviation | `Governance/Issues/IP-0028_StageD_PressurizerControlValidation_2026-02-16_131529.md` section 4; `HeatupLogs/IP-0028_StageC_20260216_131255/.../IP-0028_RUN_A_StepTelemetry.csv` | PASS |
| CS-0091 | Convergence-gated commit and no silent residual masking | `Governance/Issues/IP-0028_StageD_PressurizerControlValidation_2026-02-16_131529.md` section 5; `HeatupLogs/IP-0028_StageC_20260216_131255/.../IP-0028_RUN_A_EventSequence.csv` | PASS |
| CS-0093 | PZR remediation path deterministic and traceable with bounded continuity | `Governance/Issues/IP-0028_StageD_PressurizerControlValidation_2026-02-16_131529.md` section 6; `HeatupLogs/IP-0028_StageC_20260216_131255/IP-0028_StageC_Determinism_20260216_131255.md` | PASS |
| CS-0094 | Startup hold and authority governance deterministic across reruns | `Governance/Issues/IP-0028_StageC_StartupGovernance_2026-02-16_124518.md`; `HeatupLogs/IP-0028_StageC_20260216_131255/IP-0028_StageC_Determinism_20260216_131255.md` | PASS |
| CS-0096 | Hold-band limiter attribution/disposition explicit with runtime evidence | `Governance/Issues/IP-0028_StageC_StartupGovernance_2026-02-16_124518.md` section 5; `HeatupLogs/IP-0028_StageC_20260216_131255/RUN_A_20260216_131255/IP-0028_RUN_A_StepTelemetry.csv` | PASS |

## 5) Non-regression summary

1. Startup hold lifecycle unchanged and deterministic across reruns.
2. Limiter precedence/event sequence unchanged in deterministic replay checks.
3. Pressurizer drain continuity bounded (`max pressure step = 25 psia` in tested runset).
4. No blocking regressions detected in tested startup/pressurizer flow path.

## 6) Stage E exit criteria

1. All included CS acceptance checks mapped with PASS/FAIL evidence: **PASS**
2. No blocking regressions: **PASS**
3. CS-0040 explicit before/after stale-indicator resolution evidence: **PASS**

## 7) Stage E affected files

1. `Assets/Scripts/Physics/RVLISPhysics.cs` - remove hard 100% pin behavior via bounded over-range display clamp.
