# IP-0035 Stage B Design Freeze (2026-02-16_223400)

- IP: `IP-0035`
- DP: `DP-0006`
- Stage: `B`

## 1) Frozen Scope Contract
1. `CS-0079`: enforce startup permissive at `>= 400 psig` with no alternate lower-threshold startup path.
2. `CS-0010`: add SG secondary pressure high alarm path with deterministic clear behavior and visible annunciation.

## 2) Frozen Technical Decisions
1. Startup permissive threshold authority is `PlantConstants.MIN_RCP_PRESSURE_PSIG = 400f`.
2. Startup permissive path remains bubble-gated (`bubbleExists` must be true) and pressure-gated (`>= 400 psig`).
3. SG secondary pressure alarm threshold is fixed to `1099.7 psia` (`1085 psig`).
4. Alarm behavior is level-triggered and deterministic:
- Set when SG secondary pressure is greater than setpoint.
- Clear when SG secondary pressure is less than or equal to setpoint.
5. Alarm must surface in both alarm summary text and operator-visible annunciators.

## 3) Validation Contract Freeze
1. `CS-0079` acceptance requires constants, permissive checks, and startup status messaging aligned to `400 psig`.
2. `CS-0010` acceptance requires:
- Alarm state and input wiring in alarm manager.
- Engine-level alarm propagation.
- Dashboard and validation visual annunciator visibility.
3. Build check remains best-effort from terminal environment and may be constrained by missing Unity-generated project files.

## 4) Stage B Exit
Stage B design freeze is complete. Stage C controlled remediation authorized.
