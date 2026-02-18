# CS-0117 Investigation Report

- CS ID: `CS-0117`
- Title: `Validation dashboard compile regression: unresolved IDX_NET_HEAT blocks IP-0049 Stage E build validation`
- Domain: `Validation & Diagnostics`
- Severity: `HIGH`
- Date: `2026-02-18`
- Recommended Next Status: `OPEN`

## Summary

IP-0049 execution for `CS-0104` completed, but Stage E regression is blocked by a workspace compile failure in dashboard sparkline code.  
`ValidationDashboard.Sparklines.cs` references `IDX_NET_HEAT` without an active definition, preventing system-regression gate completion.

## Evidence

1. `Governance/ImplementationPlans/IP-0049/Reports/IP-0049_StageE_SystemRegression_2026-02-18_020900.md`  
   Initial Stage E recorded build gate failure due to unresolved `IDX_NET_HEAT` in sparklines.
2. `Governance/ImplementationPlans/IP-0049/Reports/IP-0049_StageE_SystemRegression_2026-02-18_022300.md`  
   Stage E rerun recorded PASS with compile baseline restored.
3. `Assets/Scripts/Validation/ValidationDashboard.Sparklines.cs:489-495`  
   Sparkline update path reads `IDX_NET_HEAT`.
4. `Assets/Scripts/Validation/ValidationDashboard.Sparklines.cs:522`  
   Additional `IDX_NET_HEAT` usage appears in metric mapping path.

## Root Cause Hypothesis

A parallel-track change altered metric index constants/mappings, but `IDX_NET_HEAT` callsites were not reconciled.

## Required Corrective Scope (No Implementation Under CS)

1. Reconcile sparkline metric index contract in `ValidationDashboard.Sparklines.cs`:
   - reintroduce missing constant, or
   - migrate callsites to current canonical metric indices.
2. Re-run `IP-0049` Stage E build regression and confirm compile baseline restoration.
3. Close `CS-0117` only after Stage E build gate passes.

## Dependency Disposition

- `CS-0117` blocked closure of `IP-0049` until Stage E rerun.
- `CS-0117` is scoped under `DP-0013`/`IP-0049` for blocking-traceability.

## Resolution Update (2026-02-18 02:23 UTC)

- Stage E rerun evidence (`dotnet build Critical.slnx`) passed with `0` errors.
- `IDX_NET_HEAT` unresolved-symbol compile failure is no longer reproducible in current workspace baseline.
- Disposition: `CLOSED` with `resolution_type = CLOSE_NO_CODE` (baseline restored without additional IP-0049 code changes in this pass).
