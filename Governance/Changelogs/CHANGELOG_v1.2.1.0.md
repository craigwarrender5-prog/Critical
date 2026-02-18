# Changelog - v1.2.1.0

## Version 1.2.1.0 - No-RCP Thermal Coupling Fidelity Closure

**Date:** 2026-02-18  
**Status:** COMPLETE  
**Implementation Plan:** IP-0054  
**Domain Plan:** DP-0001 - Primary Thermodynamics  
**CS Resolved:** CS-0122  
**Impact Classification:** PATCH (physics fidelity correction, no new operator-facing capability)

---

## Summary

IP-0054 corrected no-RCP thermal coupling behavior that had been forcing non-physical RCS bulk warming during solid-PZR heatup with no forced flow.
The fix removed the unconditional no-RCP transport floor and validated both no-flow envelope fidelity and RCP-on non-regression.

---

## Changes

### Code Remediation

**Files Modified:**

| File | Description |
|------|-------------|
| `Assets/Scripts/Validation/HeatupSimEngine.cs` | Removed unconditional no-RCP baseline coupling floor; no-flow state now uses zero baseline coupling and only applies forced-flow-derived transport when present. |

### Validation Tooling

**Files Added:**

| File | Description |
|------|-------------|
| `Assets/Scripts/UI/Editor/IP0054ValidationRunner.cs` | Deterministic Stage D runner for no-flow envelope and RCP-on coupling regression evidence. |

### Governance Closeout

**Files Updated:**

| File | Description |
|------|-------------|
| `Governance/ImplementationPlans/Closed/IP-0054/IP-0054.md` | Final closed status, closure metadata, and evidence linkage. |
| `Governance/ImplementationPlans/Closed/IP-0054/Reports/IP-0054_StageD_DomainValidation_2026-02-18_045200.md` | Stage D domain validation evidence. |
| `Governance/ImplementationPlans/Closed/IP-0054/Reports/IP-0054_StageE_SystemRegression_2026-02-18_045300.md` | Stage E system regression evidence. |
| `Governance/ImplementationPlans/Closed/IP-0054/Reports/IP-0054_Closeout_Traceability.md` | Final closeout traceability and registry consistency summary. |
| `Governance/ImplementationReports/IP-0054_Closure_Recommendation_2026-02-18.md` | Formal closure recommendation and transaction record. |
| `Governance/IssueRegister/issue_register.json` | Removed `CS-0122` from active working set. |
| `Governance/IssueRegister/issue_index.json` | Archived `CS-0122` as `CLOSED (FIXED)` and updated counts. |
| `Governance/DomainPlans/DP-0001 - Primary Thermodynamics.md` | Updated post-closeout domain status and references. |

---

## Validation Evidence

- `HeatupLogs/IP-0054_StageD_20260218_044748/IP-0054_StageD_Summary.md`
- `Governance/ImplementationPlans/Closed/IP-0054/Reports/IP-0054_StageD_DomainValidation_2026-02-18_045200.md`
- `Governance/ImplementationPlans/Closed/IP-0054/Reports/IP-0054_StageE_SystemRegression_2026-02-18_045300.md`
- Build verification: `dotnet build Critical.slnx` passed with `97 Warning(s)` and `0 Error(s)`.

---

## Version Justification

Classified as `PATCH` because IP-0054 delivers a corrective physics fidelity fix with no intended feature expansion or interface contract change.
Version increment applied from `1.2.0.0` to `1.2.1.0`.

---

## Governance

- `IP-0054`: CLOSED  
- `CS-0122`: CLOSED (FIXED)
