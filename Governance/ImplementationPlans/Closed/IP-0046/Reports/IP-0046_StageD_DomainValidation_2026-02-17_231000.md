# IP-0046 Stage D Domain Validation (2026-02-17_231000)

- IP: `IP-0046`
- DP: `DP-0011`
- Stage: `D`

## 1) Validation Artifacts
1. Deterministic Stage D run directory:
- `HeatupLogs/IP-0046_StageD_20260217_230958`
2. Primary telemetry artifact:
- `HeatupLogs/IP-0046_StageD_20260217_230958/IP-0046_StageD_SGSampleTelemetry.csv`
3. Stage summary artifact:
- `HeatupLogs/IP-0046_StageD_20260217_230958/IP-0046_StageD_Summary.md`

## 2) Build Gate
`dotnet build Critical.slnx`:
1. `0 Error(s)`
2. warnings present (non-blocking for this IP gate)

Compile gate status: `PASS`.

## 3) Runtime Validation Results

### CS-0082
1. SG boundary mode remained startup-open:
- summary metric: `OPEN/ISOLATED = 7202/0`
2. Startup boundary transitions recorded:
- `OPEN_PREHEAT -> PRESSURIZE -> HOLD`

Disposition: `PASS`.

### CS-0057
1. Draining trigger threshold reached and drain initiated:
- first `T_rcs >= 200F`: `15.228 hr`
- first SG draining active/mass change: `15.231 hr`
2. Action event emitted from runtime trigger path:
- `"SG DRAINING STARTED ..."`

Disposition: `PASS`.

### CS-0078
1. Circulation onset evidence:
- first non-zero RCP count at `13.142 hr`
- SG pressure at onset: `17.006 psia` (`floor`)
- SG pressure at onset +30 min: `17.008 psia` (delta `+0.002 psia`)
2. Pressure-source transition evidence:
- first `inventory-derived`: `15.078 hr`
- first boiling-active sample: `17.067 hr`
- floor reversion after first inventory-derived (pre-boil): `NO`
3. Result:
- acceptance gate conditions met in `IP-0046_StageD_Summary.md`.

Disposition: `PASS`.

## 4) Stage D Exit
Stage D passes for `CS-0082`, `CS-0057`, and `CS-0078`.
Stage E regression and closure recommendation packaging are authorized.
