# CHANGELOG_v0.5.7.0

Date: 2026-02-15
Version: 0.5.7.0
Type: DP-0002 Closure and Two-Phase Pressurizer Stabilization Release

## Versioning Basis
- `MAJOR` unchanged (`0`): no platform-wide architectural reset.
- `MINOR` unchanged (`5`): same release stream.
- `PATCH` advanced to `7`: substantial DP-0002 remediation + validated closure under one authorized IP (`IP-0021`).
- `REVISION` reset to `0` for this patch release.

## DP-0002 / IP-0021 Scope Summary
- Domain Plan: `DP-0002 - Pressurizer & Two-Phase Physics`
- Implementation Plan: `IP-0021`
- CS closure count: `15`
  - `PASS`: `13`
  - `CLOSE_NO_CODE`: `2` (`CS-0024`, `CS-0025`)

## Key Corrections (Condensed)
- Two-phase pressurizer routing unified to a single authoritative path; conflicting overrides removed.
- Bubble phase timing/flags aligned to saturation and trajectory behavior with coherent pressure response.
- DRAIN gating hardened (`<=60 min` hard gate) with explicit steam-displacement vs CVCS reconciliation telemetry.
- Lifecycle ownership corrected to prevent UPDATE-phase CVCS initialization leakage.
- Condensing HTC correlation replaced with source-backed, bounded Nusselt-film implementation.

## Evidence
- Stage D matrix:
  - `Governance/Issues/IP-0021_StageD_DomainValidation_2026-02-15_141500.md`
- Stage E regression report:
  - `Governance/Issues/IP-0021_StageE_SystemRegression_2026-02-15_141600.md`
- Closure recommendation:
  - `Governance/ImplementationReports/IP-0021_Closure_Recommendation_2026-02-15.md`
- Closeout report:
  - `Governance/ImplementationReports/IP-0021_Closeout_Report.md`
