# IP-0017 Run A vs Run B Strict Same-Process Report - 2026-02-14 17:14:56

- IP ID: `IP-0017`
- Domain: `DP-0005`
- Scope: `CS-0013` strict same-process parity closure gate
- Execution manifest: `Updates/Issues/IP-0017_SameProcess_Execution_20260214_171456.md`

## Process Identity Proof (Mandatory)
- Unity process start line: `HeatupLogs/Unity_IP0017_SameProcess_STRICT_20260214_171445.log:97` (`Initialize engine version`)
- Same process marker: `HeatupLogs/Unity_IP0017_SameProcess_STRICT_20260214_171445.log:378` (`pid=41056`, `appDomain=1`)
- Run A start/end in same process:
  - `HeatupLogs/Unity_IP0017_SameProcess_STRICT_20260214_171445.log:400`
  - `HeatupLogs/Unity_IP0017_SameProcess_STRICT_20260214_171445.log:396329`
- Run B start/end in same process:
  - `HeatupLogs/Unity_IP0017_SameProcess_STRICT_20260214_171445.log:396341`
  - `HeatupLogs/Unity_IP0017_SameProcess_STRICT_20260214_171445.log:792270`
- Completion marker: `HeatupLogs/Unity_IP0017_SameProcess_STRICT_20260214_171445.log:792282`
- Secondary process-start check: no second `Initialize engine version` entry was found in this log stream.

## Run Bundles
- Run A: `HeatupLogs/IP-0017_RunA_SameProcess_20260214_171456/`
- Run B: `HeatupLogs/IP-0017_RunB_SameProcess_20260214_171456/`
- Unity log copy in both bundles: `Unity_StageE_IP0017_SameProcess_STRICT_20260214_171445.log`

## A/B Parity Table
| Metric | Run A | Run B | Result |
|---|---|---|---|
| Stage E overall | PASS (`Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_171456.md:30`) | PASS (`Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_171500.md:30`) | MATCH |
| Conservation (lbm) | 14.6 (`HeatupLogs/IP-0017_RunA_SameProcess_20260214_171456/Heatup_Report_20260214_171500.txt:18`) | 14.6 (`HeatupLogs/IP-0017_RunB_SameProcess_20260214_171456/Heatup_Report_20260214_171503.txt:18`) | MATCH |
| RTCC assertion failures | 0 (`HeatupLogs/IP-0017_RunA_SameProcess_20260214_171456/Heatup_Report_20260214_171500.txt:20`) | 0 (`HeatupLogs/IP-0017_RunB_SameProcess_20260214_171456/Heatup_Report_20260214_171503.txt:20`) | MATCH |
| PBOC pairing failures | 0 (`HeatupLogs/IP-0017_RunA_SameProcess_20260214_171456/Heatup_Report_20260214_171500.txt:22`) | 0 (`HeatupLogs/IP-0017_RunB_SameProcess_20260214_171456/Heatup_Report_20260214_171503.txt:22`) | MATCH |
| PBOC events | 6480 (`HeatupLogs/IP-0017_RunA_SameProcess_20260214_171456/Heatup_Report_20260214_171500.txt:21`) | 6480 (`HeatupLogs/IP-0017_RunB_SameProcess_20260214_171456/Heatup_Report_20260214_171503.txt:21`) | MATCH |
| 8.25 hr interval | PASS (1.9 lbm, 0.000%) (`HeatupLogs/IP-0017_RunA_SameProcess_20260214_171456/Heatup_Interval_034_8.25hr.txt:185`) | PASS (1.9 lbm, 0.000%) (`HeatupLogs/IP-0017_RunB_SameProcess_20260214_171456/Heatup_Interval_034_8.25hr.txt:185`) | MATCH |
| 8.50 hr interval | PASS (92.3 lbm, 0.010%) (`HeatupLogs/IP-0017_RunA_SameProcess_20260214_171456/Heatup_Interval_035_8.50hr.txt:185`) | PASS (92.3 lbm, 0.010%) (`HeatupLogs/IP-0017_RunB_SameProcess_20260214_171456/Heatup_Interval_035_8.50hr.txt:185`) | MATCH |
| 8.75 hr interval | PASS (92.4 lbm, 0.010%) (`HeatupLogs/IP-0017_RunA_SameProcess_20260214_171456/Heatup_Interval_036_8.75hr.txt:185`) | PASS (92.4 lbm, 0.010%) (`HeatupLogs/IP-0017_RunB_SameProcess_20260214_171456/Heatup_Interval_036_8.75hr.txt:185`) | MATCH |
| Max mass error | 92.50 lbm (`Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_171456.md:21`) | 92.50 lbm (`Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_171500.md:21`) | MATCH |

## Session Reset / Stale-State Check
- Run A and Run B baseline interval logs both show reset baseline state:
  - `Sim Time: 0.00 hr` (`HeatupLogs/IP-0017_RunA_SameProcess_20260214_171456/Heatup_Interval_001_0.00hr.txt:5`; `HeatupLogs/IP-0017_RunB_SameProcess_20260214_171456/Heatup_Interval_001_0.00hr.txt:5`)
  - `Mass Source: CANONICAL_SOLID` (`HeatupLogs/IP-0017_RunA_SameProcess_20260214_171456/Heatup_Interval_001_0.00hr.txt:224`; `HeatupLogs/IP-0017_RunB_SameProcess_20260214_171456/Heatup_Interval_001_0.00hr.txt:224`)
  - `Charging: 0 gal`, `Letdown: 0 gal` (`HeatupLogs/IP-0017_RunA_SameProcess_20260214_171456/Heatup_Interval_001_0.00hr.txt:229`; `HeatupLogs/IP-0017_RunA_SameProcess_20260214_171456/Heatup_Interval_001_0.00hr.txt:230`; `HeatupLogs/IP-0017_RunB_SameProcess_20260214_171456/Heatup_Interval_001_0.00hr.txt:229`; `HeatupLogs/IP-0017_RunB_SameProcess_20260214_171456/Heatup_Interval_001_0.00hr.txt:230`)
  - `Error (absolute): 0.0 lbm` (`HeatupLogs/IP-0017_RunA_SameProcess_20260214_171456/Heatup_Interval_001_0.00hr.txt:239`; `HeatupLogs/IP-0017_RunB_SameProcess_20260214_171456/Heatup_Interval_001_0.00hr.txt:239`)
  - `PBOC Tick / Time: 0 / 0.0000 hr` (`HeatupLogs/IP-0017_RunA_SameProcess_20260214_171456/Heatup_Interval_001_0.00hr.txt:259`; `HeatupLogs/IP-0017_RunB_SameProcess_20260214_171456/Heatup_Interval_001_0.00hr.txt:259`)
- Verdict: no stale-session carryover signature detected.

## CS-0013 Closure Decision
- Same-process execution proof: `PASS`
- Conservation-critical parity gate: `PASS`
- Session reset behavior gate: `PASS`
- Closure outcome: `CS-0013 -> CLOSED`
