# IP-0034 Stage B Design Freeze (2026-02-16_220300)

- IP: `IP-0034`
- DP: `DP-0009`
- Stage: `B`
- Input baseline: `Governance/ImplementationPlans/IP-0034/Reports/IP-0034_StageA_RootCause_2026-02-16_220000.md`

## 1) Scope
- `CS-0088`

## 2) Hot-Path Warning Cadence Freeze
1. Keep warning diagnostics, but gate repeated warning emits using a fixed minimum interval per warning family.
2. Minimum interval is frozen at `60 seconds` (`HOT_PATH_WARNING_MIN_INTERVAL_SEC`).
3. Warning-family cadence is independent for:
- Regime 2 non-convergence
- Regime 3 non-convergence
- Regime 1 mass-audit drift
- PBOC pairing warnings

## 3) Counter and Lifecycle Freeze
1. Add suppression accounting (`hotPathWarningSuppressedCount`) for visibility into throttled warning volume.
2. Reset all warning gate timers and suppression count in run initialization to avoid cross-run carryover.

## 4) Safety and Non-Behavioral Constraints
1. No physics equations, solver branches, or acceptance thresholds are changed in this IP.
2. Structured diagnostic event paths remain active; only direct warning-emit cadence is bounded.
3. Keep first warning visibility and periodic reminder behavior via fixed interval gate.

## 5) Stage B Exit
Design and cadence policy are frozen. Stage C controlled remediation authorized.

