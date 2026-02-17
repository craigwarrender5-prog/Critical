# CS-0115 Investigation Report

- CS ID: `CS-0115`
- Title: `Missing condenser + feedwater return module and startup permissive boundary contract (C-9/P-12) for steam-dump integration`
- Domain: `Steam Generator Secondary Physics`
- Severity: `CRITICAL`
- Date: `2026-02-17`
- Disposition: `INCONSISTENT`
- Recommended Next Status: `READY` (corrective scope defined; no implementation under CS)

## Conclusion

Current startup SG/steam-dump behavior is **inconsistent** with the consolidated technical baseline in `Technical_Documentation/`.  
The gap is not a single tuning issue; it is a missing secondary-boundary contract spanning:

1. condenser availability/state (`C-9`, vacuum, CW),
2. dump permissive authority (`P-12` blocking/bypass),
3. closed-loop return path (hotwell/condensate/feedwater/AFW),
4. startup boundary-state sequencing (vents -> MSIV bypass warmup -> MSIV open -> dump bridge).

This inconsistency is sufficient to keep `CS-0078` blocked until corrected.

## Evidence

### Technical Documentation

1. `Technical_Documentation/NRC_HRTD_Condenser_System_Reference.md:48`  
   C-9 condenser-available interlock is required before steam dumps can be available.
2. `Technical_Documentation/NRC_HRTD_Condenser_System_Reference.md:67`  
   C-9 actuation/reset logic is vacuum and CW-pump dependent.
3. `Technical_Documentation/NRC_HRTD_Condenser_System_Reference.md:212`  
   Return-path linkage is explicitly hotwell/condensate/feedwater/AFW coupled.
4. `Technical_Documentation/NRC_HRTD_Section_11.2_Steam_Dump_Control.md:156`  
   Steam dump interlocks include C-9.
5. `Technical_Documentation/NRC_HRTD_Section_11.2_Steam_Dump_Control.md:165`  
   P-12 low-low Tavg blocks dump operation unless bypassed under procedure.
6. `Technical_Documentation/Startup_Boundary_and_SteamDump_Authoritative_Spec.md:25`  
   Startup boundary rule is MSIV closed, MSIV bypass open during line warming.
7. `Technical_Documentation/Startup_Boundary_and_SteamDump_Authoritative_Spec.md:41`  
   Startup dump bridge is explicitly C-9/P-12 gated.
8. `Technical_Documentation/PWR_Startup_State_Sequence.md:320`  
   Dump state machine: Dumps Unavailable / Armed (Closed) / Modulating.
9. `Technical_Documentation/NRC_HRTD_Section_7.2_Condensate_and_Feedwater_System_Full.md:68`  
   Condenser is part of the required sink and feedwater loop.
10. `Technical_Documentation/NRC_HRTD_Section_5.7_Auxiliary_Feedwater_System_Full.md:367`  
    Dump-to-condenser path and hotwell recirculation are documented as closed-loop startup behavior.

### Runtime Logs (`Build/HeatupLogs`)

1. `Build/HeatupLogs/Heatup_Interval_001_0.00hr.txt:184`  
   Startup SG pressure source initializes to `floor` in `OPEN_PREHEAT`.
2. `Build/HeatupLogs/Forensics/SG_Forensics_084_15-42-11_RegimeChange.csv:11`  
   Forensics schema lacks condenser/C-9/P-12 telemetry fields.
3. `Build/HeatupLogs/Forensics/SG_Forensics_084_15-42-11_RegimeChange.csv:12`  
   At `T_rcs ~225F`, SG pressure remains near `17.0-17.1 psia` while regime toggles and steam rate swings, indicating missing startup boundary sink/permissive integration.

### Code Trace

1. `Assets/Scripts/Physics/SteamDumpController.cs:459`  
   `ShouldAutoEnable()` only checks `T_avg` and steam pressure.
2. `Assets/Scripts/Validation/HeatupSimEngine.HZP.cs:176`  
   Auto-enable path calls `ShouldAutoEnable()` directly, with no C-9/P-12 gating inputs.
3. `Assets/Scripts/UI/PlantOverviewScreen.cs:402`  
   Condenser vacuum remains placeholder in runtime UI, aligning with missing modeled condenser state.

## Root Cause

No plant-style condenser + return-path authority module exists in runtime execution.  
As a result, startup pressure-source behavior and steam-dump authority are under-constrained and diverge from documented C-9/P-12 and boundary sequencing requirements.

## Corrective Scope (IP-Ready, No Implementation Under CS)

1. Add `CondenserState` contract:
   - Vacuum/backpressure dynamics,
   - CW availability,
   - C-9 output boolean.
2. Add `FeedwaterReturnState` contract:
   - Hotwell inventory/state,
   - Condensate/feedwater/AFW availability and routing.
3. Implement startup boundary FSM:
   - Vented/preheat,
   - MSIV bypass warmup (MSIV closed),
   - MSIV open/equalized,
   - dump-bridge authority states.
4. Enforce steam-dump authority gating:
   - C-9 true,
   - P-12 not blocking (or explicit bypass).
5. Extend logging/telemetry:
   - condenser vacuum, CW running state,
   - C-9 state,
   - P-12 block/bypass state,
   - return-path flow/inventory indicators.
6. Re-run `CS-0078` validation only after steps 1-5 are integrated.

## Dependency Disposition

- `CS-0078` remains **BLOCKED** by `CS-0115`.
- Corrective action requires execution under `IP-0046`.
- Governance: no implementation changes under this CS without corresponding IP execution authority.
