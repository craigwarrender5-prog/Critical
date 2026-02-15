# CHANGELOG_v0.6.0.0

Date: 2026-02-15
Version: 0.6.0.0
Type: Minor - IP-0024 Unified PZR Authority-First Remodel Closeout

## Versioning Basis
- `MAJOR` unchanged (`0`): no platform-wide architectural reset or governance break.
- `MINOR` advanced to `6`: foundational pressurizer physics remodel and causality-contract expansion under `IP-0024` materially broaden simulator behavior without introducing an incompatible break.
- `PATCH` reset to `0` for this minor release.
- `REVISION` reset to `0` for this minor release.

## Scope Summary
- Implementation Plan: `IP-0024 - Unified PZR Implementation Plan (Authority-First)`
- CS closure scope:
  - `CS-0091` (PZR closure convergence/bracketing reliability)
  - `CS-0092` (DRAIN lineup/hydraulic causality)
  - `CS-0093` (umbrella PZR remodel closure)
  - `CS-0094` (startup stabilization/causality gating)
  - `CS-0040` (RVLIS DRAIN consistency)
  - `CS-0081` (solid-plant pressure band fidelity)

## Behavioral Impact Summary
- Two-phase closure now executes with deterministic feasibility diagnostics and explicit failure classification instead of generic bracket-failure behavior.
- DRAIN flow behavior is enforced as lineup/hydraulic-causal with explicit event-driven lineup transitions and saturation feedback.
- Startup hold/cause ordering, continuity diagnostics, and deterministic micro-run validation are integrated into final closure evidence.
- Setpoint and indicator fidelity checks are captured in the final deterministic acceptance suite.

## Governance Impact Summary
- Issue closeout metadata written for all six closure-scope CS entries in `Governance/IssueRegister/issue_register.json` with reference `IP-0024`.
- `IP-0024` marked `CLOSED` and moved to `Governance/ImplementationPlans/Closed/`.
- Final PASS artifacts are marked authoritative; interim failed runstamp remains historical and superseded.

## Validation Evidence
- Final run stamp: `2026-02-15_201932`
- Stage D gate artifact: `Governance/Issues/IP-0024_StageD_ExitGate_SinglePhaseHold_2026-02-15_201932.md`
- Stage H deterministic evidence artifact: `Governance/Issues/IP-0024_StageH_DeterministicEvidence_2026-02-15_201932.md`
- Closeout runtime log: `HeatupLogs/Unity_IP0024_Closeout_20260215_201917.log`
- Historical interim (superseded) artifact: `Governance/Issues/IP-0024_StageH_DeterministicEvidence_2026-02-15_194447.md`

## Version Justification
- Impact class selected per `PROJECT_CONSTITUTION.md` Article XI Section 6: `Minor`.
- Rationale: this closure package is not clerical and not a narrow defect-only patch; it materially broadens validated PZR runtime behavior and causality enforcement while preserving compatibility.
