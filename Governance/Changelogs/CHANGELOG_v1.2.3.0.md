# Changelog - v1.2.3.0

## Version 1.2.3.0 - Project Governance Refactor and Closeout

**Date:** 2026-02-18  
**Status:** COMPLETE  
**Implementation Plan:** IP-0055  
**Domain Plan:** DP-0010 - Project Governance  
**CS Resolved:** CS-0124, CS-0126  
**Impact Classification:** PATCH (structural refactor/governance trail hardening; no intended runtime behavior change)

---

## Summary

IP-0055 completed governance-traceability restructuring and staged large-file decomposition across scoped physics/UI modules.
Closeout finalized CS status transitions, archived the IP bundle, and documented a temporary waiver path for remaining oversized `HeatupSimEngine` validation files.

---

## Changes

### Code and Structure Remediation

| File | Description |
|------|-------------|
| `Assets/Scripts/Physics/CVCSController.cs` | Reduced core surface and delegated subsystem logic to partial files. |
| `Assets/Scripts/Physics/CVCSController.Heaters.cs` | New partial for heater-specific controller logic. |
| `Assets/Scripts/Physics/CVCSController.Letdown.cs` | New partial for letdown path logic. |
| `Assets/Scripts/Physics/CVCSController.SealFlow.cs` | New partial for seal-flow logic. |
| `Assets/Scripts/Physics/SolidPlantPressure.cs` | Reduced core surface with split constants/diagnostics support. |
| `Assets/Scripts/Physics/SolidPlantPressure.Constants.cs` | New partial constants decomposition. |
| `Assets/Scripts/Physics/SolidPlantPressure.Diagnostics.cs` | New partial diagnostics decomposition. |
| `Assets/Scripts/Physics/SGMultiNodeThermal.cs` | Reduced core shell for SG thermal model. |
| `Assets/Scripts/Physics/SGMultiNodeThermal.Types.cs` | New SG type/state declarations partial. |
| `Assets/Scripts/Physics/SGMultiNodeThermal.Constants.cs` | New SG constants partial. |
| `Assets/Scripts/Physics/SGMultiNodeThermal.API.cs` | New SG public API and orchestration partial. |
| `Assets/Scripts/Physics/SGMultiNodeThermal.ControlAPI.cs` | New SG control API partial. |
| `Assets/Scripts/Physics/SGMultiNodeThermal.PrivateMethods.cs` | New SG implementation partial. |
| `Assets/Scripts/Physics/SGMultiNodeThermal.Validation.cs` | New SG validation/support partial. |
| `Assets/Scripts/UI/MultiScreenBuilder.cs` | Reduced core shell with screen-specific extraction. |
| `Assets/Scripts/UI/MultiScreenBuilder.Infrastructure.cs` | New UI infrastructure/builder wiring partial. |
| `Assets/Scripts/UI/MultiScreenBuilder.Helpers.cs` | New UI helper/utilities partial. |
| `Assets/Scripts/UI/MultiScreenBuilder.OverviewTab.cs` | New overview-tab construction partial. |
| `Assets/Scripts/UI/MultiScreenBuilder.Screen1.cs` | New screen 1 builder partial. |
| `Assets/Scripts/UI/MultiScreenBuilder.Screen2.cs` | New screen 2 builder partial. |
| `Assets/Scripts/UI/MultiScreenBuilder.Screen3.cs` | New screen 3 builder partial. |
| `Assets/Scripts/UI/MultiScreenBuilder.Screen4.cs` | New screen 4 builder partial. |
| `Assets/Scripts/UI/MultiScreenBuilder.Screen5.cs` | New screen 5 builder partial. |
| `Assets/Scripts/UI/MultiScreenBuilder.Screen6.cs` | New screen 6 builder partial. |
| `Assets/Scripts/UI/MultiScreenBuilder.Screen7.cs` | New screen 7 builder partial. |
| `Assets/Scripts/UI/MultiScreenBuilder.Screen8.cs` | New screen 8 builder partial. |

### Governance Closeout

| File | Description |
|------|-------------|
| `Governance/ImplementationPlans/Closed/IP-0055 Project Governance/IP-0055.md` | Final closed status, closure metadata, and evidence linkage. |
| `Governance/ImplementationPlans/Closed/IP-0055 Project Governance/Reports/IP-0055_Stage1_GovernanceRestructure_2026-02-18_143000.md` | Stage 1 execution evidence. |
| `Governance/ImplementationPlans/Closed/IP-0055 Project Governance/Reports/IP-0055_Stage2_PhysicsRefactor_Partial_2026-02-18_161500.md` | Stage 2 partial execution evidence. |
| `Governance/ImplementationPlans/Closed/IP-0055 Project Governance/Reports/IP-0055_Stage2_PhysicsRefactor_Complete_2026-02-18_170500.md` | Stage 2 completion evidence. |
| `Governance/ImplementationPlans/Closed/IP-0055 Project Governance/Reports/IP-0055_Stage3_UIRefactor_Complete_2026-02-18_172500.md` | Stage 3 completion evidence. |
| `Governance/ImplementationPlans/Closed/IP-0055 Project Governance/Reports/IP-0055_Stage4_ValidationAssessment_2026-02-18_173500.md` | Stage 4 assessment/waiver evidence. |
| `Governance/ImplementationPlans/Closed/IP-0055 Project Governance/Reports/IP-0055_Closeout_Traceability.md` | Final closeout traceability and registry summary. |
| `Governance/ImplementationReports/IP-0055_Closure_Recommendation_2026-02-18.md` | Formal closure recommendation and executed transaction record. |
| `Governance/IssueRegister/issue_register.json` | Removed `CS-0124` and `CS-0126` from active working set and updated active counts. |
| `Governance/IssueRegister/issue_index.json` | Archived `CS-0124` and `CS-0126` as `CLOSED (FIXED)` and updated counts. |
| `Governance/DomainPlans/DP-0010 - Project Governance.md` | Updated post-closeout domain readiness and references. |

---

## Validation Evidence

- `Governance/ImplementationPlans/Closed/IP-0055 Project Governance/Reports/IP-0055_Stage2_PhysicsRefactor_Complete_2026-02-18_170500.md`
- `Governance/ImplementationPlans/Closed/IP-0055 Project Governance/Reports/IP-0055_Stage3_UIRefactor_Complete_2026-02-18_172500.md`
- `Governance/ImplementationPlans/Closed/IP-0055 Project Governance/Reports/IP-0055_Stage4_ValidationAssessment_2026-02-18_173500.md`
- `Governance/ImplementationPlans/Closed/IP-0055 Project Governance/Reports/IP-0055_Closeout_Traceability.md`
- Build verification: `dotnet build Critical.slnx` passed with `0 Warning(s)` and `0 Error(s)`.

---

## Version Justification

Classified as `PATCH` because IP-0055 delivers structural decomposition and governance-traceability hardening with no intended feature expansion.
Version increment applied from `1.2.2.0` to `1.2.3.0`.

---

## Governance

- `IP-0055`: CLOSED  
- `CS-0124`: CLOSED (FIXED)  
- `CS-0126`: CLOSED (FIXED)
