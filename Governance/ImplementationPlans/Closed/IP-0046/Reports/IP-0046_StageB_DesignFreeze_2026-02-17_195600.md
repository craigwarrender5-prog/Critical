# IP-0046 Stage B Design Freeze (2026-02-17_195600)

- IP: `IP-0046`
- DP: `DP-0011`
- Stage: `B`

## 1) Stage B Contracts
Implementation and validation contracts were frozen as:

1. `CS-0082` acceptance:
- SG startup boundary shall remain open during startup sequence.
- Evidence source: SG boundary mode telemetry (`OPEN/ISOLATED` counts and interval logs).

2. `CS-0057` acceptance:
- SG draining trigger shall fire once when `T_rcs >= SG_DRAINING_START_TEMP_F`.
- Evidence source: action log event and SG draining state telemetry.

3. `CS-0078` acceptance:
- SG secondary pressure response must begin at startup circulation onset and show sustained non-floor behavior before boiling transition.
- Evidence source: SG pressure/source timeline in `IP-0046_StageD_SGSampleTelemetry.csv`.

4. Runtime execution integrity:
- No PBOC guard exceptions through full Stage D horizon (`20 hr` deterministic run).
- Compile gate: `dotnet build Critical.slnx` with `0` errors.

## 2) Frozen Execution Method
1. Compile validation via `dotnet build Critical.slnx`.
2. Deterministic runtime validation via Unity batch execution of:
- `Critical.Validation.IP0046ValidationRunner.RunStageDValidation`
- `Assets/Scripts/UI/Editor/IP0046ValidationRunner.cs:45`

## 3) Stage B Exit
Stage B design/acceptance contract is frozen.
Stage C remediation authorized.
