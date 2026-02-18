# IP-0046 Stage D Domain Validation (2026-02-17_200400)

- IP: `IP-0046`
- DP: `DP-0011`
- Stage: `D`

## 1) Validation Artifacts
1. Deterministic Stage D run directory:
- `HeatupLogs/IP-0046_StageD_20260217_195532`
2. Primary telemetry artifact:
- `HeatupLogs/IP-0046_StageD_20260217_195532/IP-0046_StageD_SGSampleTelemetry.csv`
3. Stage summary artifact:
- `HeatupLogs/IP-0046_StageD_20260217_195532/IP-0046_StageD_Summary.md`

## 2) Build Gate
`dotnet build Critical.slnx`:
1. `0 Error(s)`
2. warnings present (non-blocking for this IP gate)

Compile gate status: `PASS`.

## 3) Runtime Validation Results

### CS-0082
1. SG boundary mode remained startup-open:
- summary metric: `OPEN/ISOLATED = 7202/0`
- source: `IP-0046_StageD_Summary.md`
2. Interval evidence:
- `HeatupLogs/IP-0046_StageD_20260217_195532/Heatup_Interval_054_13.25hr.txt`
- `HeatupLogs/IP-0046_StageD_20260217_195532/Heatup_Interval_060_14.75hr.txt`

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
- SG pressure at onset +30 min: `17.008 psia` (`floor`)
2. Pressure-source transition evidence:
- first `inventory-derived`: `15.078 hr` at `T_rcs=189.84F`
- reverted to `floor`: `15.506 hr` at `T_rcs=219.29F`
- `P_sat` branch appears only later near boiling transition: `17.067 hr` at `T_rcs=312.87F`
3. Result:
- pressure response is not sustained through warmup and remains floor-dominant for most of startup window.

Disposition: `FAIL`.

## 4) Runtime Integrity Blocker Recheck
The prior PBOC contract exception seen in earlier trial run
(`HeatupLogs/IP-0046_StageD_20260217_194228`) did not recur after branch fix.

Status: `PASS` for runtime execution integrity.

## 5) Stage D Exit
Stage D passes for `CS-0082` and `CS-0057`, and fails for `CS-0078`.
Stage E regression can proceed, but closure eligibility is blocked pending `CS-0078` resolution.
