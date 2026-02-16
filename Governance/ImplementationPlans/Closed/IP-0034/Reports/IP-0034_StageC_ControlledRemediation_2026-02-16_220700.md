# IP-0034 Stage C Controlled Remediation (2026-02-16_220700)

- IP: `IP-0034`
- DP: `DP-0009`
- Stage: `C`

## 1) Implemented Changes

### Core hot-path gating implementation
1. Added per-warning-family next-emit timers:
- `nextRegime2ConvergenceWarnTime_hr`
- `nextRegime3ConvergenceWarnTime_hr`
- `nextR1MassAuditWarnTime_hr`
- `nextPbocPairingWarnTime_hr`
2. Added suppression accounting field:
- `hotPathWarningSuppressedCount`
3. Added cadence constant:
- `HOT_PATH_WARNING_MIN_INTERVAL_SEC = 60f`
4. Added centralized gate helper:
- `ShouldEmitHotPathWarning(ref float nextAllowedTime_hr)`

Files:
- `Assets/Scripts/Validation/HeatupSimEngine.cs:765`
- `Assets/Scripts/Validation/HeatupSimEngine.cs:795`
- `Assets/Scripts/Validation/HeatupSimEngine.cs:2552`

### Hot-path warning call-site remediations
1. Regime 2 convergence warning now gated:
- `Assets/Scripts/Validation/HeatupSimEngine.cs:1857`
2. Regime 3 convergence warning now gated:
- `Assets/Scripts/Validation/HeatupSimEngine.cs:2013`
3. Regime 1 mass-audit warning now gated:
- `Assets/Scripts/Validation/HeatupSimEngine.cs:2107`
4. PBOC pairing warning now gated:
- `Assets/Scripts/Validation/HeatupSimEngine.cs:2917`

### Initialization reset updates
1. Added deterministic reset of gate timers and suppression counter:
- `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:132`

## 2) Stage C Exit
Stage C remediation is complete for `CS-0088`. Stage D validation authorized.

