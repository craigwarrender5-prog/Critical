# Changelog - v1.2.0.0

## Version 1.2.0.0 - Validation Scenario Integration Bridge and Runtime Selector

**Date:** 2026-02-18  
**Status:** COMPLETE  
**Implementation Plan:** IP-0049  
**Domain Plan:** DP-0013 - Validation & Diagnostics (with DP-0008 cross-domain inclusion)  
**CS Resolved:** CS-0104, CS-0117, CS-0119  
**Impact Classification:** MINOR (new runtime scenario capability and cross-system selector/start integration)

---

## Summary

IP-0049 delivered selectable runtime scenario execution for the validation heatup path and added an in-simulator selection/start surface.
The implementation preserves canonical Mode 5 startup semantics while introducing a non-breaking scenario bridge contract and SceneBridge-routed F2 selector flow.

---

## Changes

### Stage A-C: Scenario Bridge Capability Added

**Files Added:**

| File | Description |
|------|-------------|
| `Assets/Scripts/ScenarioSystem/ISimulationScenario.cs` | Defines scenario contract for registry-based startup surfaces. |
| `Assets/Scripts/ScenarioSystem/ScenarioRegistry.cs` | Adds scenario registration and descriptor lookup/listing. |
| `Assets/Scripts/ScenarioSystem/ValidationHeatupScenario.cs` | Wraps baseline validation run as scenario `validation.heatup.baseline`. |
| `Assets/Scripts/Validation/HeatupSimEngine.Scenarios.cs` | Adds scenario listing and `StartScenarioById()` bridge APIs. |

**Files Modified:**

| File | Description |
|------|-------------|
| `Assets/Scripts/Validation/HeatupSimEngine.cs` | Adds optional scenario startup path with fallback to canonical `StartSimulation()`. |

### Stage D-E + Follow-up: Runtime Selector Integration

**Files Modified:**

| File | Description |
|------|-------------|
| `Assets/Scripts/Core/SceneBridge.cs` | Routes `F2` in validator view and forwards selector toggle to dashboard surface. |
| `Assets/Scripts/Validation/HeatupValidationVisual.cs` | Adds bridge-driven scenario selector APIs and modal overlay start flow. |

### Governance Closeout

**Files Updated:**

| File | Description |
|------|-------------|
| `Governance/ImplementationPlans/Closed/IP-0049/IP-0049.md` | Final closed status, closeout references, and revision update. |
| `Governance/ImplementationPlans/Closed/IP-0049/Reports/IP-0049_Closeout_Traceability.md` | Closure traceability, build evidence, and register consistency summary. |
| `Governance/IssueRegister/issue_register.json` | Removed `CS-0104` and `CS-0119` from active working set. |
| `Governance/IssueRegister/issue_index.json` | Archived `CS-0104` and `CS-0119` with `FIXED` disposition. |

---

## Validation Evidence

- `Governance/ImplementationPlans/Closed/IP-0049/Reports/IP-0049_StageD_DomainValidation_2026-02-18_024700.md`
- `Governance/ImplementationPlans/Closed/IP-0049/Reports/IP-0049_StageE_SystemRegression_2026-02-18_024800.md`
- `Governance/ImplementationPlans/Closed/IP-0049/Reports/IP-0049_Closeout_Traceability.md`
- Build verification: `dotnet build Critical.slnx` passed with `96 Warning(s)` and `0 Error(s)`.

---

## Version Justification

Classified as `MINOR` under constitution rules because IP-0049 adds material new capability (scenario abstraction/registry bridge and runtime scenario selection/start flow) across validation runtime and operator input surfaces, without platform break.

Version increment applied from `1.1.1.0` to `1.2.0.0` (resetting PATCH and REVISION).

---

## Governance

- `IP-0049`: CLOSED  
- `CS-0104`: CLOSED (FIXED)  
- `CS-0117`: CLOSED (CLOSE_NO_CODE)  
- `CS-0119`: CLOSED (FIXED)
