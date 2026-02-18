# Changelog - v1.2.2.0

## Version 1.2.2.0 - Mode 5 Pre-Heater Pressurization Policy Alignment Closure

**Date:** 2026-02-18  
**Status:** COMPLETE  
**Implementation Plan:** IP-0052  
**Domain Plan:** DP-0012 - Pressurizer & Startup Control  
**CS Resolved:** CS-0109  
**Impact Classification:** PATCH (control-policy correction and sequencing enforcement; no new operator-facing capability)

---

## Summary

IP-0052 established explicit pre-heater CVCS ownership and deterministic heater handoff during Mode 5 solid startup.
The fix aligned startup mechanism and pressure-rate behavior with documented expectations and removed the prior conflated control path.

---

## Changes

### Code Remediation

**Files Modified:**

| File | Description |
|------|-------------|
| `Assets/Scripts/Physics/SolidPlantPressure.cs` | Added explicit `PREHEATER_CVCS` stage, deterministic handoff policy, and pre-heater telemetry fields/diagnostics. |
| `Assets/Scripts/Validation/HeatupSimEngine.cs` | Enforced heater lockout during pre-heater stage and added deterministic solid-control mode transition logging. |
| `Assets/Scripts/Validation/HeatupSimEngine.Init.cs` | Reset solid-control transition logging sentinels at initialization. |
| `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs` | Added pre-heater pressure-rate validation row and telemetry output. |

### Governance Closeout

**Files Updated/Added:**

| File | Description |
|------|-------------|
| `Governance/ImplementationPlans/Closed/IP-0052/IP-0052.md` | Final closed status, closure metadata, and evidence linkage. |
| `Governance/ImplementationPlans/Closed/IP-0052/Reports/IP-0052_StageA_RootCause_2026-02-18_051938.md` | Stage A root-cause evidence. |
| `Governance/ImplementationPlans/Closed/IP-0052/Reports/IP-0052_StageB_DesignFreeze_2026-02-18_051938.md` | Stage B design-freeze evidence. |
| `Governance/ImplementationPlans/Closed/IP-0052/Reports/IP-0052_StageC_ControlledRemediation_2026-02-18_051938.md` | Stage C remediation evidence. |
| `Governance/ImplementationPlans/Closed/IP-0052/Reports/IP-0052_StageD_DomainValidation_2026-02-18_051938.md` | Stage D domain validation evidence. |
| `Governance/ImplementationPlans/Closed/IP-0052/Reports/IP-0052_StageE_SystemRegression_2026-02-18_051938.md` | Stage E system regression evidence. |
| `Governance/ImplementationPlans/Closed/IP-0052/Reports/IP-0052_Closeout_Traceability.md` | Final closeout traceability and registry consistency summary. |
| `Governance/ImplementationReports/IP-0052_Closure_Recommendation_2026-02-18.md` | Formal closure recommendation and transaction record. |
| `Governance/IssueRegister/issue_register.json` | Removed `CS-0109` from active working set and updated active counts. |
| `Governance/IssueRegister/issue_index.json` | Archived `CS-0109` as `CLOSED (FIXED)` and updated counts. |
| `Governance/DomainPlans/DP-0012 - Pressurizer & Startup Control.md` | Updated post-closeout domain status and references. |

---

## Validation Evidence

- `Build/HeatupLogs/IP-0052_StageD_20260218_051938/IP-0052_preheater_probe.csv`
- `Governance/ImplementationPlans/Closed/IP-0052/Reports/IP-0052_StageD_DomainValidation_2026-02-18_051938.md`
- `Governance/ImplementationPlans/Closed/IP-0052/Reports/IP-0052_StageE_SystemRegression_2026-02-18_051938.md`
- Build verification: `dotnet build Critical.slnx` passed with `0 Warning(s)` and `0 Error(s)`.

---

## Version Justification

Classified as `PATCH` because IP-0052 delivers corrective startup-control policy alignment and sequencing fidelity with no intended feature expansion or interface contract change.
Version increment applied from `1.2.1.0` to `1.2.2.0`.

---

## Governance

- `IP-0052`: CLOSED  
- `CS-0109`: CLOSED (FIXED)
