# READY_FOR_FIX Portfolio Implementation Plan (2026-02-16)

## 1) Purpose and Scope
- Purpose: define execution order for all remaining `READY_FOR_FIX` CS items.
- Scope source: `Governance/IssueRegister/issue_register.json`
- Scope timestamp: `2026-02-16T19:13:27Z`
- Total in scope: `26` CS items

Constitution note:
- This is a portfolio sequencing artifact, not a controlling `IP-XXXX` file.
- Execution must be performed through single-DP IP bundles (one DP per IP) per Article VIII.

## 2) Full READY_FOR_FIX Scope

### DP-0001 (1)
- `CS-0080` - RCP heat-input constants do not align with cited cold-water heatup reference basis.

### DP-0006 (2)
- `CS-0010` - No SG secondary pressure alarm.
- `CS-0079` - RCP startup permissive below documented minimum startup pressure.

### DP-0007 (7)
- `CS-0006` - `UpdatePrimaryMassLedgerDiagnostics()` never called.
- `CS-0007` - No UI display for primary ledger drift.
- `CS-0011` - Acceptance tests are formula-only, not simulation-validated.
- `CS-0012` - No regime transition logging.
- `CS-0041` - Inventory audit baseline type mismatch (geometric vs mass-derived gallons).
- `CS-0062` - Stage E primary-heat telemetry aliased to SG removal.
- `CS-0064` - Heatup engine state exposed as ad-hoc mutable public fields.

### DP-0009 (1)
- `CS-0088` - Hot-path runtime logging patterns can create avoidable overhead.

### DP-0010 (12)
- `CS-0058` - HZP subsystem initialization occurs in UPDATE path.
- `CS-0060` - PlantConstants partials execute runtime calculation logic.
- `CS-0063` - Simulation-facing files exceed GOLD hard size limits.
- `CS-0070` - HeatupSimEngine header missing mandatory GOLD C01 sections.
- `CS-0083` - Baseline A contains conflicting numeric authority for RCP heat.
- `CS-0084` - C# files do not uniformly meet GOLD header metadata.
- `CS-0085` - File-level change history incomplete/missing.
- `CS-0086` - Public APIs lack consistent XML documentation.
- `CS-0087` - Modules exceed responsible file-size/separation limits.
- `CS-0089` - Validation modules remain in global namespace.
- `CS-0090` - Project-level changelog governance missing.
- `CS-0097` - Remediate legacy governance integrity failures.

### DP-0011 (3)
- `CS-0057` - SG startup draining contract at ~200F not wired into runtime path.
- `CS-0078` - SG secondary pressure response delayed until near-boiling.
- `CS-0082` - SG startup boundary modeled isolated vs warmup/open-path references.

## 3) Dependency and Blocking Assessment

### Confirmed hard blocker
- `CS-0006` blocks `CS-0007` (explicit register linkage).

### Likely blockers (should be treated as sequencing gates)
- `CS-0083` should precede `CS-0080` because numeric authority conflict can invalidate constant-selection decisions.
- `CS-0060` should precede `CS-0080` and `CS-0079` because constants-file runtime logic can reintroduce drift and non-authoritative behavior.
- `CS-0082` should precede `CS-0078` and `CS-0057` because boundary model/measurement behavior defines acceptance conditions for SG startup and pressure response.
- `CS-0097` should precede closure activities for all follow-on IPs because cross-register integrity is a closure gate risk.

### Potential blockers / coupling risks
- `CS-0062`, `CS-0012`, and `CS-0011` should be delivered before late-stage validation in DP-0001/DP-0006/DP-0011 to avoid low-confidence acceptance evidence.
- `CS-0058` can alter startup path behavior and should complete before DP-0006 and DP-0011 validation windows are frozen.
- `CS-0088` should be completed before broad Stage E reruns to reduce logging-induced performance noise.

## 4) Ordered Execution Plan

### Wave 0 - Governance and authority preconditions
Goal: stabilize governance integrity and numeric authority before physics tuning.
- DP-0010: `CS-0097`, `CS-0083`, `CS-0060`, `CS-0058`
Exit gate:
- Registry parity clean.
- Single authoritative RCP heat basis selected and documented.
- Runtime constant ownership path no longer routed through constants partial logic.

### Wave 1 - Observability and evidence quality foundation
Goal: make downstream validation trustworthy.
- DP-0007: `CS-0006` then `CS-0007` (hard dependency)
- DP-0007: `CS-0012`, `CS-0062`, `CS-0041`, `CS-0064`, `CS-0011`
- DP-0009: `CS-0088` (can run in parallel with DP-0007)
Exit gate:
- Required diagnostic signals available and unaliased.
- Test and telemetry paths support simulation-validated acceptance.

### Wave 2 - Plant protection and primary constants alignment
Goal: align startup permissives, alarms, and primary heat constants.
- DP-0006: `CS-0079` then `CS-0010`
- DP-0001: `CS-0080` (after Wave 0 authority decisions)
Exit gate:
- Startup permissive and alarm behavior aligned to documented operating intent.
- RCP heat constants aligned to selected authority basis.

### Wave 3 - SG startup and boundary behavior remediation
Goal: restore SG startup dynamic response with documented boundary behavior.
- DP-0011: `CS-0082` then `CS-0078` then `CS-0057`
Exit gate:
- SG boundary model behavior and startup pressure response validated together.
- Draining contract wiring validated under startup scenarios.

### Wave 4 - Governance hardening and maintainability closeout
Goal: close remaining governance debt after behavior changes settle.
- DP-0010: `CS-0063`, `CS-0070`, `CS-0084`, `CS-0085`, `CS-0086`, `CS-0087`, `CS-0089`, `CS-0090`
Exit gate:
- GOLD compliance and changelog/namespace/doc standards satisfied without rework churn from active physics changes.

## 5) Proposed Per-DP IP Bundle Allocation
- `IP-0032` -> `DP-0010` (Wave 0 preconditions)
- `IP-0033` -> `DP-0007` (Wave 1 diagnostics)
- `IP-0034` -> `DP-0009` (Wave 1 performance telemetry noise reduction)
- `IP-0035` -> `DP-0006` (Wave 2 plant protection)
- `IP-0036` -> `DP-0001` (Wave 2 primary constants alignment)
- `IP-0037` -> `DP-0011` (Wave 3 SG startup/boundary)
- `IP-0038` -> `DP-0010` (Wave 4 governance hardening)

## 5.1) Instantiated Artifacts
- `Governance/ImplementationPlans/IP-0032/IP-0032.md`
- `Governance/ImplementationPlans/IP-0033/IP-0033.md`
- `Governance/ImplementationPlans/IP-0034/IP-0034.md`
- `Governance/ImplementationPlans/IP-0035/IP-0035.md`
- `Governance/ImplementationPlans/IP-0036/IP-0036.md`
- `Governance/ImplementationPlans/IP-0037/IP-0037.md`
- `Governance/ImplementationPlans/IP-0038/IP-0038.md`
- `Governance/ImplementationPlans/IP_EXECUTION_SEQUENCE_2026-02-16.md`

## 6) Parallelization Policy
- Allowed in parallel: `DP-0007` and `DP-0009` within Wave 1.
- Conditionally parallel: `DP-0006` and `DP-0001` in Wave 2 only after Wave 0 exit criteria are met.
- Not parallelized: Wave 3 starts only after Wave 2 validation baseline is frozen.

## 7) Completion Criteria
- No in-scope CS remains in `READY_FOR_FIX`.
- Every execution IP has Stage A-E evidence and closure artifacts.
- Registry integrity checks pass (`issue_register`, `issue_index`, `issue_archive`) with no orphan references.
