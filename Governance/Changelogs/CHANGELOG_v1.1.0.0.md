# Changelog - v1.1.0.0

## Version 1.1.0.0 - SG Startup Boundary and Secondary-Side Startup Bridge

**Date:** 2026-02-18  
**Status:** COMPLETE  
**Implementation Plan:** IP-0046  
**Domain Plan:** DP-0011 - Steam Generator Secondary Physics  
**CS Resolved:** CS-0057, CS-0078, CS-0082, CS-0115, CS-0116  
**Impact Classification:** MINOR (new condenser/feedwater/permissive subsystem and startup control-path integration)

---

## Summary

IP-0046 delivered a material startup-behavior expansion for SG secondary-side physics. The simulator now includes modeled condenser and feedwater startup boundaries, explicit C-9/P-12 permissive gating for steam-dump authority, and corrected SG pressure-source transition behavior through pre-boil and boiling startup windows.

---

## Changes

### Stage F-G: Secondary Boundary Subsystem Added
**Files Added:**

| File | Description |
|------|-------------|
| `Assets/Scripts/Physics/PlantConstants.Condenser.cs` | Added condenser and feedwater startup constants and thresholds. |
| `Assets/Scripts/Physics/CondenserPhysics.cs` | Added condenser vacuum/backpressure model with C-9 evaluation path. |
| `Assets/Scripts/Physics/FeedwaterSystem.cs` | Added hotwell/feedwater/CST balance and startup return-path behavior. |

### Stage H: Startup Permissive Bridge Added
**Files Added/Modified:**

| File | Description |
|------|-------------|
| `Assets/Scripts/Physics/StartupPermissives.cs` | Added startup permissive evaluation and steam-dump bridge state logic. |
| `Assets/Scripts/Physics/SteamDumpController.cs` | Added permissive-aware dump enable/update gating. |

### Stage I-J: Engine and SG Integration
**Files Modified:**

| File | Description |
|------|-------------|
| `Assets/Scripts/Validation/HeatupSimEngine.cs` | Integrated condenser/feedwater/permissive state updates and orchestration. |
| `Assets/Scripts/Validation/HeatupSimEngine.Init.cs` | Added initialization wiring for new startup modules. |
| `Assets/Scripts/Validation/HeatupSimEngine.HZP.cs` | Routed permissive state into dump auto-enable/update logic. |
| `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs` | Extended telemetry for new startup boundary signals. |
| `Assets/Scripts/Physics/SGMultiNodeThermal.cs` | Added sink-availability aware SG pressure/outflow behavior. |
| `Assets/Scripts/UI/PlantOverviewScreen.cs` | Exposed live condenser/permissive startup indicators. |

### Stage L-N: Orchestration and Pressure-Source Stability Completion

- Completed condenser startup trigger and P-12 bypass orchestration logic needed to unblock CS-0078.
- Completed SG sink-authority coupling path so pressure response transitions correctly as sink availability changes.
- Stabilized pre-boil pressure-source behavior to avoid unintended floor/inventory chatter prior to first boiling sample.

---

## Validation Evidence

- `Governance/ImplementationPlans/Closed/IP-0046/Reports/IP-0046_StageD_DomainValidation_2026-02-17_231000.md`
- `Governance/ImplementationPlans/Closed/IP-0046/Reports/IP-0046_StageE_SystemRegression_2026-02-17_231100.md`
- `Governance/ImplementationPlans/Closed/IP-0046/Reports/IP-0046_Closeout_Traceability.md`
- Build verification: `dotnet build Critical.slnx` passed with `0 Warning(s)` and `0 Error(s)`.

---

## Version Justification

Classified as `MINOR` under constitution rules because IP-0046 introduces substantial new simulator capability (condenser/feedwater startup subsystem plus permissive bridge and SG sink-authority integration), beyond a bounded defect patch.

Version increment applied from `1.0.1.0` to `1.1.0.0` (resetting PATCH and REVISION).

---

## Governance

- `IP-0046`: CLOSED  
- `CS-0057`: CLOSED (FIXED)  
- `CS-0078`: CLOSED (FIXED)  
- `CS-0082`: CLOSED (FIXED)  
- `CS-0115`: CLOSED (FIXED)  
- `CS-0116`: CLOSED (FIXED)
