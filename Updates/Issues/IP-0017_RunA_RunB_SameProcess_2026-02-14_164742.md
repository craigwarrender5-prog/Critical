# IP-0017 Run A vs Run B Same-Process Comparison - 2026-02-14 16:47:42

- IP ID: `IP-0017`
- Domain: `DP-0005`
- Scope: Same-process repeat-run parity gate for `CS-0013`

## Run Artifacts
- Run A evidence markdown: `Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_164359.md`
- Run A preserved bundle: `HeatupLogs/IP-0017_RunA_20260214_164402/`
- Run B evidence markdown: `Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_164456.md`
- Run B preserved bundle: `HeatupLogs/IP-0017_RunB_20260214_164459/`

## Process-Identity Check (Mandatory for CS-0013)
- Run A Unity log shows a distinct batch invocation at `HeatupLogs/IP-0017_RunA_20260214_164402/Unity_StageE_IP0017_RunA_20260214_164347.log:8` and command-line batch flags at `HeatupLogs/IP-0017_RunA_20260214_164402/Unity_StageE_IP0017_RunA_20260214_164347.log:12` and `HeatupLogs/IP-0017_RunA_20260214_164402/Unity_StageE_IP0017_RunA_20260214_164347.log:13`.
- Run B Unity log shows a second distinct batch invocation at `HeatupLogs/IP-0017_RunB_20260214_164459/Unity_StageE_IP0017_RunB_20260214_164445.log:8` and batch flags at `HeatupLogs/IP-0017_RunB_20260214_164459/Unity_StageE_IP0017_RunB_20260214_164445.log:12` and `HeatupLogs/IP-0017_RunB_20260214_164459/Unity_StageE_IP0017_RunB_20260214_164445.log:13`.
- Result: runs were executed as separate `-batchmode -quit` process invocations.

Same-process gate verdict:
- `FAIL` for strict same-process proof requirement.
- Consequence: `CS-0013` remains non-closed under fail-closed policy.

## A/B Numerical Parity (Informational)
| Metric | Run A | Run B | Result |
|---|---|---|---|
| Stage E result | PASS | PASS | MATCH |
| Mass conservation gate | PASS (14.6 lbm) | PASS (14.6 lbm) | MATCH |
| RTCC assertion failures | 0 | 0 | MATCH |
| PBOC pairing failures | 0 | 0 | MATCH |
| PBOC events | 6480 | 6480 | MATCH |
| 8.25 hr interval conservation | 1.9 lbm (0.000%) | 1.9 lbm (0.000%) | MATCH |
| 8.50 hr interval conservation | 92.3 lbm (0.010%) | 92.3 lbm (0.010%) | MATCH |
| 8.75 hr interval conservation | 92.4 lbm (0.010%) | 92.4 lbm (0.010%) | MATCH |

Parity evidence anchors:
- Run A report gates: `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt:18`
- Run B report gates: `HeatupLogs/IP-0017_RunB_20260214_164459/Heatup_Report_20260214_164459.txt:18`
- Run A interval windows: `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_034_8.25hr.txt:185`, `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_035_8.50hr.txt:185`, `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_036_8.75hr.txt:185`
- Run B interval windows: `HeatupLogs/IP-0017_RunB_20260214_164459/Heatup_Interval_034_8.25hr.txt:185`, `HeatupLogs/IP-0017_RunB_20260214_164459/Heatup_Interval_035_8.50hr.txt:185`, `HeatupLogs/IP-0017_RunB_20260214_164459/Heatup_Interval_036_8.75hr.txt:185`

## Closure Decision for CS-0013
- Required same-process A/B parity proof: `NOT SATISFIED`.
- Closure status outcome: `CS-0013 remains OPEN/Assigned`.

## Addendum (2026-02-14)
- This report remains valid as prior evidence for non-strict execution.
- Strict same-process evidence that satisfies the gate is recorded in:
  - `Updates/Issues/IP-0017_RunA_RunB_SameProcess_STRICT_2026-02-14_171456.md`
  - `Updates/Issues/IP-0017_SameProcess_Execution_20260214_171456.md`
