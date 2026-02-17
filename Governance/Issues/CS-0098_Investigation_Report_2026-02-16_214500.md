# CS-0098 Investigation Report (2026-02-16_214500)

- Issue ID: `CS-0098`
- Title: `Heaters do not start after startup-hold release during cold-start sequence`
- Investigation Status: `COMPLETE`
- Assigned DP: `DP-0012 - Pressurizer & Startup Control`
- Recommended Next Status: `READY_FOR_FIX`

## Problem Statement
In cold-start startup-hold scenarios, heater authority is expected to resume automatic pressurization after hold release. Observed behavior indicates heaters can remain effectively off after release, preventing expected startup pressure rise.

## Evidence
1. `HeatupLogs/IP-0024_StageD_Hold_2026-02-15_194555/Heatup_Report_20260215_194556.txt` records `mode=OFF` and final heater power `0.000 MW` in a startup-hold run.
2. `HeatupLogs/IP-0024_StageD_Hold_2026-02-15_194555/Heatup_Interval_002_0.25hr.txt` shows `Heater Mode at PID: PASS (mode=OFF)` while startup sequence remains in early pressurization envelope.
3. `Assets/Scripts/Validation/HeatupSimEngine.cs:2152` shows `ResolveHeaterAuthorityState()` returning `OFF` whenever `currentHeaterMode == HeaterMode.OFF`.
4. `Assets/Scripts/Validation/HeatupSimEngine.cs:2198` shows startup-hold release clearing hold state only; no explicit re-arm to startup heater auto mode is enforced at release.

## Root Cause
If `currentHeaterMode` is `OFF` at the hold-release boundary, releasing startup hold only removes `HOLD_LOCKED` authority. The authority resolver then continues to return `OFF`, and heater demand remains `0 MW`.

## Disposition
- Investigation completeness: `SUFFICIENT FOR IMPLEMENTATION HANDOFF`
- Assigned DP: `DP-0012`
- Recommended remediation direction:
  1. On startup-hold release, deterministically re-arm to `PRESSURIZE_AUTO` when manual disable is not active.
  2. Add a regression assertion that post-release heater power becomes non-zero within a bounded window under valid startup conditions.
