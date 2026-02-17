# IP-0044 Stage B Design Freeze (2026-02-17_182800)

- IP: `IP-0044`
- DP: `DP-0006`
- Stage: `B`

## 1) Frozen Scope Contract
1. `CS-0079`: enforce startup permissive at `>= 400 psig` with no lower-threshold path in authoritative startup sequencing.
2. `CS-0010`: maintain SG secondary pressure high alarm path with deterministic clear behavior and operator-visible annunciation.

## 2) Frozen Technical Decisions
1. Startup permissive constant authority is `PlantConstants.MIN_RCP_PRESSURE_PSIG = 400f`.
2. Startup permissive check remains bubble-gated and pressure-gated via `PlantConstants.CanStartRCP(...)`.
3. SG secondary pressure high alarm setpoint is fixed at `1099.7 psia` (`1085 psig`).
4. Alarm behavior is level-triggered and deterministic:
- Set when SG secondary pressure is greater than setpoint.
- Clear when SG secondary pressure is less than or equal to setpoint.
5. Alarm remains surfaced in alarm summary and dashboard/validation annunciator surfaces.

## 3) Validation Contract Freeze
1. `CS-0079` acceptance requires constant authority, startup gate helper, and sequencer status messaging alignment at `400 psig`.
2. `CS-0010` acceptance requires alarm manager wiring, engine propagation/reset semantics, and operator annunciator visibility.
3. Compile gate for IP closeout is `dotnet build Critical.slnx` with `0` errors.

## 4) Stage B Exit
Stage B design freeze is complete. Stage C controlled remediation authorized.
