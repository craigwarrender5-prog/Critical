# CHANGELOG_v0.5.8.0

Date: 2026-02-15
Version: 0.5.8.0
Type: DP-0004 Closure and CVCS Inventory Control Stabilization Release

## Versioning Basis
- `MAJOR` unchanged (`0`): no platform-wide architectural reset.
- `MINOR` unchanged (`5`): same release stream.
- `PATCH` advanced to `8`: DP-0004 remediation and validated closure under one authorized IP (`IP-0022`).
- `REVISION` reset to `0` for this patch release.

## DP-0004 / IP-0022 Scope Summary
- Domain Plan: `DP-0004 - CVCS / Inventory Control`
- Implementation Plan: `IP-0022`
- CS closure count: `4`
  - `PASS`: `4`

## Key Corrections (Condensed)
- Reconciled CVCS conservation verification terms to a single authoritative accounting basis for VCT and BRS loop transfers.
- Implemented procedure-aligned dynamic DRAIN routing with explicit policy telemetry for letdown, charging, and net outflow.
- Added CVCS thermal mixing contribution into the RCS energy path with bounded runtime telemetry.
- Added deterministic `IP-0022` checkpoint runner and generated Stage D/E closure evidence.

## Evidence
- Stage D matrix:
  - `Governance/Issues/IP-0022_StageD_DomainValidation_2026-02-15_145014.md`
- Stage E regression report:
  - `Governance/Issues/IP-0022_StageE_SystemRegression_2026-02-15_145014.md`
- Closure recommendation:
  - `Governance/ImplementationReports/IP-0022_Closure_Recommendation_2026-02-15.md`
- Closeout report:
  - `Governance/ImplementationReports/IP-0022_Closeout_Report.md`
