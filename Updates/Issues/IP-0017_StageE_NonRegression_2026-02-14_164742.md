# IP-0017 Stage E Non-Regression Report - 2026-02-14 16:47:42

- IP ID: `IP-0017`
- DP ID: `DP-0005`
- Mode: GOVERNANCE/VALIDATION (no physics/engine code changes)
- Scope CS: `CS-0001`, `CS-0002`, `CS-0003`, `CS-0004`, `CS-0005`, `CS-0008`, `CS-0013`
- Explicitly excluded closed CS: `CS-0050`, `CS-0051`, `CS-0052`

## Run A Summary
Artifacts:
- `Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_164359.md`
- `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt`
- `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_034_8.25hr.txt`
- `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_035_8.50hr.txt`
- `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_036_8.75hr.txt`
- `HeatupLogs/IP-0017_RunA_20260214_164402/Unity_StageE_IP0017_RunA_20260214_164347.log`

Gate results:
- Stage E overall: `PASS` (`Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_164359.md`)
- Conservation gate: `PASS (14.6 lbm)` (`HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt:18`)
- RTCC counters: `PASS`, assertion failures `0` (`HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt:20`)
- PBOC pairing counters: `PASS`, failures `0` (`HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt:22`)

Required window checks (Run A):
- Transition window (8.25 hr): `PASS (1.9 lbm, 0.000%)` (`HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_034_8.25hr.txt:185`)
- Follow-on window (8.50 hr): `PASS (92.3 lbm, 0.010%)` (`HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_035_8.50hr.txt:185`)
- Post-bubble window (8.75 hr): `PASS (92.4 lbm, 0.010%)` (`HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_036_8.75hr.txt:185`)

## Run B Summary
Artifacts:
- `Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_164456.md`
- `HeatupLogs/IP-0017_RunB_20260214_164459/Heatup_Report_20260214_164459.txt`
- `HeatupLogs/IP-0017_RunB_20260214_164459/Heatup_Interval_034_8.25hr.txt`
- `HeatupLogs/IP-0017_RunB_20260214_164459/Heatup_Interval_035_8.50hr.txt`
- `HeatupLogs/IP-0017_RunB_20260214_164459/Heatup_Interval_036_8.75hr.txt`
- `HeatupLogs/IP-0017_RunB_20260214_164459/Unity_StageE_IP0017_RunB_20260214_164445.log`

Gate results:
- Stage E overall: `PASS` (`Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_164456.md`)
- Conservation gate: `PASS (14.6 lbm)` (`HeatupLogs/IP-0017_RunB_20260214_164459/Heatup_Report_20260214_164459.txt:18`)
- RTCC counters: `PASS`, assertion failures `0` (`HeatupLogs/IP-0017_RunB_20260214_164459/Heatup_Report_20260214_164459.txt:20`)
- PBOC pairing counters: `PASS`, failures `0` (`HeatupLogs/IP-0017_RunB_20260214_164459/Heatup_Report_20260214_164459.txt:22`)

## Run A/B Parity Summary
- Detailed parity report: `Updates/Issues/IP-0017_RunA_RunB_SameProcess_2026-02-14_164742.md`
- Numerical parity: `PASS` (Run A and Run B metrics/interval summaries match)
- Same-process proof requirement for `CS-0013`: `FAIL` (runs were separate `-batchmode -quit` invocations)

## Per-CS Closure Decisions
| CS | Closed? | Gate Decision | Evidence |
|---|---|---|---|
| CS-0001 | Y | Explicit canonical authority proof present | `HeatupLogs/IP-0017_RunA_20260214_164402/Unity_StageE_IP0017_RunA_20260214_164347.log:163926`, `HeatupLogs/IP-0017_RunB_20260214_164459/Unity_StageE_IP0017_RunB_20260214_164445.log:163872`, `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_034_8.25hr.txt:209` |
| CS-0002 | Y | Ledger continuity passed across transition/follow-on/post-bubble and repeat-run windows; no freeze signature with active letdown | `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_034_8.25hr.txt:35`, `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_034_8.25hr.txt:209`, `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_035_8.50hr.txt:35`, `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_036_8.75hr.txt:35`, `HeatupLogs/IP-0017_RunB_20260214_164459/Heatup_Interval_035_8.50hr.txt:35` |
| CS-0003 | Y | Accumulator/audit completeness and internal consistency passed | `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt:43`, `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt:44`, `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt:45`, `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt:46`, `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt:36` |
| CS-0004 | Y | No authority overwrite signature at transition; RTCC assert delta = 0 | `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_034_8.25hr.txt:232`, `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_034_8.25hr.txt:234`, `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_034_8.25hr.txt:239` |
| CS-0005 | Y | Single-apply/single-owner invariant passed; PBOC pairing failures = 0 | `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt:22`, `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_035_8.50hr.txt:243`, `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Interval_035_8.50hr.txt:254` |
| CS-0008 | Y | Runtime conservation guardrail evidence remained within thresholds; no conservation regression | `Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_164359.md:21`, `Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_164359.md:28`, `HeatupLogs/IP-0017_RunA_20260214_164402/Heatup_Report_20260214_164402.txt:18` |
| CS-0013 | N | Fail-closed: strict same-process A/B proof not demonstrated | `Updates/Issues/IP-0017_RunA_RunB_SameProcess_2026-02-14_164742.md` |

## Regression Check
- No new conservation regression observed in Run A or Run B relative to IP-0016 baseline gates.
- New CS logged: `None`.

## Governance Outcome
- Eligible closures under IP-0017: `CS-0001`, `CS-0002`, `CS-0003`, `CS-0004`, `CS-0005`, `CS-0008`.
- Not closed under fail-closed policy: `CS-0013` (pending strict same-process validation evidence).

## Addendum (2026-02-14)
- Strict same-process execution was completed later in one continuous Unity process.
- Superseding evidence:
  - `Updates/Issues/IP-0017_RunA_RunB_SameProcess_STRICT_2026-02-14_171456.md`
  - `Updates/Issues/IP-0017_SameProcess_Execution_20260214_171456.md`
- `CS-0013` status updated to `CLOSED` in the authoritative issue governance system.
