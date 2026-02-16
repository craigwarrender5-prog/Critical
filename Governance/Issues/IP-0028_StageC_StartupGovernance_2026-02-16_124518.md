# IP-0028 Stage C - Startup Governance Implementation

- IP: `IP-0028`
- DP: `DP-0012`
- Stage: `C`
- Timestamp: `2026-02-16_124518`
- Governing freeze: `Governance/Issues/IP-0028_StageB_DesignFreeze_2026-02-16_124209.md`
- In-scope CS: `CS-0094`, `CS-0096`

## 1) Stage C implementation summary

Implemented startup governance and limiter observability in line with Stage B freeze:

1. Added explicit startup hold authority resolution (`HOLD_LOCKED`) so runtime authority is explicit instead of implicit heater inhibition.
2. Added gated hold-release logic using all frozen gates:
   - minimum hold time,
   - pressure-rate stability window,
   - state-quality (finite-state) gate,
   - periodic blocked-release logging.
3. Added heater authority and limiter telemetry/logging fields so runtime winner/limiter cause is attributable.
4. Added limiter-state exposure from heater control logic (`PRESSURE_RATE_CLAMP`, `HEATER_RAMP_LIMIT`, etc.).
5. Added deterministic triple-run harness for Stage C validation (`RUN_A`, `RUN_B`, `RUN_C`) with event-sequence and limiter-sequence comparison.

## 2) Files changed (Stage C scope only)

1. `Assets/Scripts/Validation/HeatupSimEngine.cs`
   - Startup hold gate variables, authority precedence resolution, hold lifecycle logging, limiter reason derivation.
2. `Assets/Scripts/Validation/HeatupSimEngine.Init.cs`
   - Initialization/reset of new hold-gate and authority telemetry fields.
3. `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs`
   - Interval-log output extensions for authority, limiter, and hold gate states.
4. `Assets/Scripts/Validation/HeatupSimEngine.RuntimePerf.cs`
   - Runtime snapshot alignment with new authority state fields.
5. `Assets/Scripts/Physics/CVCSController.cs`
   - Heater state limiter observability fields (pressure-rate clamp / ramp clamp / target fraction).
6. `Assets/Scripts/UI/Editor/IP0028ExecutionRunner.cs`
   - Stage C deterministic triple-run evidence harness.
7. `Assets/Scripts/UI/Editor/IP0028ExecutionRunner.cs.meta`
   - Unity asset metadata for new runner.

## 3) Frozen-threshold conformance statement

- Thresholds/windows taken from Stage B freeze; no tuning performed.
- Implemented values:
  - hold pressure-rate gate: `|dP/dt| <= 200 psi/hr`
  - hold stability window: `10 sec`
  - blocked-release log interval: `30 sec`
  - startup minimum hold gate uses frozen baseline hold duration from initialization profile (`15 sec`), observed release at first post-threshold sample (`0.008333 hr`, 20 sec at 10 s step granularity).

## 4) Validation runs and evidence

### 4.1 Stage C deterministic triple run

- Unity log: `HeatupLogs/IP0028_StageC_Unity.log`
- Suite root: `HeatupLogs/IP-0028_StageC_20260216_124427`
- Determinism summary: `HeatupLogs/IP-0028_StageC_20260216_124427/IP-0028_StageC_Determinism_20260216_124427.md`

Run outputs:
1. `RUN_A`
   - Step telemetry: `HeatupLogs/IP-0028_StageC_20260216_124427/RUN_A_20260216_124427/IP-0028_RUN_A_StepTelemetry.csv`
   - Event sequence: `HeatupLogs/IP-0028_StageC_20260216_124427/RUN_A_20260216_124427/IP-0028_RUN_A_EventSequence.csv`
2. `RUN_B`
   - Step telemetry: `HeatupLogs/IP-0028_StageC_20260216_124427/RUN_B_20260216_124429/IP-0028_RUN_B_StepTelemetry.csv`
   - Event sequence: `HeatupLogs/IP-0028_StageC_20260216_124427/RUN_B_20260216_124429/IP-0028_RUN_B_EventSequence.csv`
3. `RUN_C`
   - Step telemetry: `HeatupLogs/IP-0028_StageC_20260216_124427/RUN_C_20260216_124431/IP-0028_RUN_C_StepTelemetry.csv`
   - Event sequence: `HeatupLogs/IP-0028_StageC_20260216_124427/RUN_C_20260216_124431/IP-0028_RUN_C_EventSequence.csv`

Determinism result from suite summary:
- Event ordering identical: `PASS`
- Limiter activation sequence identical: `PASS`
- Hold-release gating behavior identical: `PASS`

### 4.2 Hold lifecycle telemetry snapshots (RUN_A)

Source: `HeatupLogs/IP-0028_StageC_20260216_124427/RUN_A_20260216_124427/IP-0028_RUN_A_StepTelemetry.csv`

| Snapshot | time_hr | hold_active | authority_state | limiter_reason | heater_power_mw | hold_gate_time | hold_gate_pressure | hold_gate_state | hold_block_reason |
|---|---:|---:|---|---|---:|---:|---:|---:|---|
| init | 0.000000 | 1 | HOLD_LOCKED | HOLD_LOCKED | 0.000000 | 0 | 0 | 0 | MIN_TIME_NOT_REACHED |
| mid-hold | 0.005556 | 1 | HOLD_LOCKED | HOLD_LOCKED | 0.000000 | 0 | 1 | 1 | MIN_TIME_NOT_REACHED |
| release sample | 0.008333 | 0 | AUTO | NONE | 1.794000 | 1 | 1 | 1 | NONE |

## 5) CS-0096 disposition (mandatory)

Disposition: **Documented approved deviation with bounded behavior and explicit limiter attribution**.

Evidence:
- Runtime checkpoint extraction from `RUN_A` step telemetry at 1.50/1.75/2.00 hr shows:
  - `solid_control_mode = HOLD_SOLID`
  - `solid_in_band = 1`
  - `authority_state = AUTO`
  - `limiter_reason = PRESSURE_RATE_CLAMP`
  - heater power remains sub-max (`0.358800`, `0.598000`, `0.686393 MW`)
- This identifies the primary runtime limiter in the observed hold-band condition as `PRESSURE_RATE_CLAMP` (with intermittent `HEATER_RAMP_LIMIT` transitions in adjacent samples).

Bounded behavior statement:
- Clamp threshold bounded at `100 psi/hr` startup heater-rate limiter with `20%` minimum power floor.
- Runtime now logs and exposes active limiter precedence, removing prior attribution ambiguity.

## 6) Stage C exit criteria

1. Deterministic startup hold/authority behavior across reruns: **PASS**
2. Limiter reason visibility in logs/telemetry: **PASS**
3. >=3 reruns with identical event ordering: **PASS**
4. >=3 reruns with identical limiter activation sequence: **PASS**
5. >=3 reruns with identical hold-release gating behavior: **PASS**
6. CS-0096 disposition recorded with evidence references: **PASS**

