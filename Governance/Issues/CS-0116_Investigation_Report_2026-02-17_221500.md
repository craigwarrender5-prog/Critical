# CS-0116 Investigation Report

- CS ID: `CS-0116`
- Title: `Condenser startup orchestration and SG sink-authority coupling incomplete after CS-0115 staged implementation`
- Domain: `Steam Generator Secondary Physics`
- Severity: `CRITICAL`
- Date: `2026-02-17`
- Recommended Next Status: `READY`

## Summary

CS-0115 delivered the condenser/feedwater/permissive module set, but Stage D rerun evidence shows startup orchestration is still incomplete.  
Condenser vacuum pulldown and P-12 bypass are not actuated in runtime sequence, and SG open-boiling outflow remains effectively unconstrained while dump authority is blocked.

## Evidence

1. `HeatupLogs/IP-0046_StageD_20260217_215436/Heatup_Interval_081_20.00hr.txt:93`  
   Condenser vacuum remains `0.0 in.Hg` at run end.
2. `HeatupLogs/IP-0046_StageD_20260217_215436/Heatup_Interval_081_20.00hr.txt:95`  
   `C-9 Available: NO` at run end.
3. `HeatupLogs/IP-0046_StageD_20260217_215436/Heatup_Interval_081_20.00hr.txt:100`  
   `P-12 Active: YES` and blocking persists.
4. `HeatupLogs/IP-0046_StageD_20260217_215436/Heatup_Interval_070_17.25hr.txt:208`  
   SG pressure source is `P_sat` while permissives still report dump unavailable.
5. `HeatupLogs/IP-0046_StageD_20260217_215436/Heatup_Interval_070_17.25hr.txt:249`  
   Large SG steam outflow in OPEN mode despite blocked dump authority.
6. `HeatupLogs/IP-0046_StageD_20260217_215436/IP-0046_StageD_Summary.md:22`  
   `CS-0078` remains `FAIL`.
7. `Assets/Scripts/Physics/CondenserPhysics.cs:223`  
   `StartVacuumPulldown()` exists.
8. `Assets/Scripts/Physics/StartupPermissives.cs:262`  
   `SetP12Bypass()` exists.
9. `Assets/Scripts/Validation/HeatupSimEngine.cs:1517` and `Assets/Scripts/Validation/HeatupSimEngine.cs:1534`  
   Engine updates condenser/permissives each tick, but actuation sequence remains incomplete.

## Root Cause

Runtime orchestration did not fully integrate the new APIs and authority handoff sequence:

1. condenser startup command path is missing/incomplete,
2. P-12 bypass transition policy is missing/incomplete,
3. SG steam outflow/sink behavior is not fully constrained by actual condenser/permissive availability.

## Required Corrective Scope (No Implementation Under CS)

1. Add deterministic condenser startup trigger sequence in engine startup timeline:
   - CW pump lineup,
   - vacuum pulldown initiation,
   - C-9 assertion window before dump-demand window.
2. Implement explicit P-12 bypass transition policy per startup procedure.
3. Couple SG OPEN-boiling outflow/sink authority to permissive bridge state so blocked dumps cannot behave as available sink.
4. Re-run Stage D/E acceptance:
   - C-9 must assert in startup window,
   - P-12 bypass/clear state must transition correctly,
   - CS-0078 must pass.

## Dependency Disposition

- `CS-0116` depends on `CS-0115` staged baseline.
- `CS-0116` blocks `CS-0078` closure and `IP-0046` closure recommendation.
- Governance: no implementation changes under CS without corresponding IP execution authority.
