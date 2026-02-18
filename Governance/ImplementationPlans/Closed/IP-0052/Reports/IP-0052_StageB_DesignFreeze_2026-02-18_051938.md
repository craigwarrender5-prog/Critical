# IP-0052 Stage B - Design Freeze (2026-02-18_051938)

- IP: `IP-0052`
- DP: `DP-0012`
- Gate: `B`
- Result: `PASS`

## Design Decisions
1. Introduced explicit three-stage solid pressurization policy in `SolidPlantPressure`:
   - `PREHEATER_CVCS`
   - `HEATER_PRESSURIZE`
   - `HOLD_SOLID`
2. Added deterministic pre-heater handoff timer and threshold:
   - threshold: `PRESSURIZE_COMPLETE_PRESSURE_PSIA`
   - dwell: `PRESSURIZE_STABILITY_TIME_HR * 3600`
3. Added pre-heater policy telemetry fields for traceability:
   - `PreHeaterTargetNetCharging_gpm`
   - `PreHeaterEffectiveNetCharging_gpm`
   - `PreHeaterHandoffTimer_sec`
4. Added engine-side heater lockout branch for `PREHEATER_CVCS` to prevent early heater participation.
5. Added solid control-mode transition logging for deterministic handoff event traces.

## Shared-Surface Risk Mitigation
1. Preserved existing PI/transport-delay path for `HEATER_PRESSURIZE` and `HOLD_SOLID`.
2. Kept no-flow hold path unchanged (`ISOLATED_NO_FLOW`).
3. Limited new behavior to solid Mode 5 policy branch and associated diagnostics.
