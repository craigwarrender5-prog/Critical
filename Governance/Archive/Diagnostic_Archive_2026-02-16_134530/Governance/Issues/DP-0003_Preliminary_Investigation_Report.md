---
Report ID: DP-0003-PIR-2026-02-14
Domain Plan: DP-0003
Title: Steam Generator Secondary Physics - Preliminary Investigation
Status: Preliminary Investigation Complete - Awaiting Authorization
Date: 2026-02-14
Mode: SPEC/DRAFT
---

# DP-0003 Preliminary Investigation Report

## Executive Summary
DP-0003 contains a startup-blocking SG secondary behavior chain: SG secondary remains in an effectively open/near-atmospheric operating pattern during heat-up, steam outflow tracks steam production, and SG heat removal can dominate available plant source heat. This keeps SG pressure/temperature progression below expected startup trajectory and collapses net RCS heat input, which directly blocks heat-up progression.

## Investigation Scope and Method
- Read-only code inspection and telemetry/document review only.
- No code modifications.
- Domain localization only; no definitive root-cause closure.

## Findings

### A) Is "ISOLATED" actually sealed?
- Governing logic:
  - A sealed SG should have no steam outflow and should use inventory pressure as active pressure source.
- Expected behavior:
  - `SteamIsolated=true` should be activated by startup/control path when SG is isolated.
- Simulated/implemented behavior:
  - Runtime default is open (`SteamIsolated=false`) and no call sites were found that activate isolation.
  - Open branch sets `SteamOutflow_lbhr = SteamProductionRate_lbhr`.
- Boundary/control state:
  - SG startup path remains open unless isolation is explicitly set, but no caller sets it.
- Evidence:
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:765`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1418`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1427`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1546`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1548`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1552`
  - `Assets/Scripts` search result: only definition of `SetSteamIsolation(...)` found.

### B) Where is steam mass/void/compressible volume tracked?
- Governing law:
  - Steam inventory should obey mass conservation and pressure-volume relation when isolated.
- Expected behavior:
  - Steam inventory/steam space should feed active SG pressure path when sealed.
- Simulated/implemented behavior:
  - Steam inventory, steam space, and inventory pressure are computed.
  - Inventory pressure override is gated behind `SteamIsolated && inBoilingRegime`.
- Boundary/control state:
  - With `SteamIsolated=false`, inventory pressure is diagnostic-only and does not control secondary pressure.
- Evidence:
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1401`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1416`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1432`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1444`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1463`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1465`

### C) How is secondary pressure computed (clamp vs derived)?
- Governing law/control:
  - Pressure should be state-derived and consistent with regime boundary conditions.
- Expected behavior:
  - Pre-steaming to boiling transition should move off initial blanket pressure when heating/boiling state advances.
- Simulated/implemented behavior:
  - Pre-steaming branch sets pressure directly to `SG_INITIAL_PRESSURE_PSIA`.
  - Boiling branch derives pressure from `P_sat(T_hottest)` and clamps with minimum of 17 psia.
  - Inventory-based pressure path only activates when isolated.
- Boundary/control state:
  - Floor-clamped behavior around 17 psia remains part of active open-system branch.
- Evidence:
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1896`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1908`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1931`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1936`
  - `Assets/Scripts/Physics/PlantConstants.SG.cs:663`
  - `Assets/Scripts/Physics/PlantConstants.SG.cs:790`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1463`

### D) How is N2 blanket modeled (clamp vs PV=nRT compressible gas)?
- Governing law:
  - Trapped gas cushion should behave as compressible gas state, not only as fixed pressure clamp.
- Expected behavior:
  - N2 contribution should evolve from gas state variables (mass/volume/temperature relationship).
- Simulated/implemented behavior:
  - N2 behavior is represented by boolean isolation state plus fixed initial pressure floor usage.
  - No dedicated N2 mass/volume state is used in active pressure equation.
- Boundary/control state:
  - Pre-steaming pressure is pinned to `SG_INITIAL_PRESSURE_PSIA` until branch conditions clear.
- Evidence:
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1871`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1896`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1908`
  - `Assets/Scripts/Physics/PlantConstants.SG.cs:663`
  - `Assets/Scripts/Physics/PlantConstants.SG.cs:676`

### E) Why is SG heat removal effectively unbounded?
- Governing law/control:
  - Plant net heat should remain bounded by source/sink state coupling and realistic SG feedback.
- Expected behavior:
  - SG sink should remain physically bounded and should not dominate gross source heat during startup without corresponding pressure/temperature progression.
- Simulated/implemented behavior:
  - SG total heat removal is computed and exported as sink term.
  - RCS net heat subtracts SG sink directly; negative net heat is therefore possible when SG sink dominates.
  - Evidence documents report SG sink around 27 MW against available source around 22.8 MW.
- Boundary/control state:
  - Open boundary and low-pressure behavior allow sustained strong boiling sink class.
- Evidence:
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1141`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1142`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1476`
  - `Assets/Scripts/Physics/RCSHeatup.cs:135`
  - `Technical_Documentation/SG_Secondary_Pressurization_During_Heatup_Research.md:13`
  - `Updates/Archive/IMPLEMENTATION_PLAN_v5.1.0.md:37`
  - `Updates/Archive/IMPLEMENTATION_PLAN_v5.1.0.md:38`

### F) Symptom chain reproduction via telemetry/log evidence (short excerpts)
- Observation chain:
  - SG evidence reports open inventory state (`OPEN`) with low-pressure class conditions.
  - Consolidated research evidence reports SG sink > available source and resulting RCS cooldown.
- Evidence excerpts:
  - `HeatupLogs/Heatup_Interval_001_0.00hr.txt:155`
  - `HeatupLogs/Heatup_Interval_001_0.00hr.txt:160`
  - `Technical_Documentation/SG_Secondary_Pressurization_During_Heatup_Research.md:13`
  - `Updates/Archive/IMPLEMENTATION_PLAN_v5.1.0.md:37`
  - `Updates/Archive/IMPLEMENTATION_PLAN_v5.1.0.md:38`

## Root-Cause Candidates (Ranked by Likelihood)
1. Steam isolation path is not activated in runtime SG control path, leaving SG in open-boundary behavior during heat-up.
2. Pressure-floor/pre-steaming clamp logic keeps SG pressure near initial blanket class in conditions where startup progression should move pressure upward.
3. N2 blanket is modeled as fixed-pressure behavior rather than compressible gas state contribution.
4. SG sink path has no explicit source-availability guard at the plant heat-balance interface, so SG removal can dominate net heat.

## Identified Fix Set (Discrete, Minimal Scope)
- Wire explicit SG isolation control state into runtime path so isolated mode is actually entered when required.
- Promote steam inventory/compressible volume pressure path from diagnostic-only to active control path in isolated startup segment.
- Replace fixed N2 pressure-floor behavior with a bounded compressible gas cushion state contribution.
- Add explicit SG sink bounding/guardrail at SG-RCS coupling interface to prevent startup sink dominance beyond physically available heat.
- Add targeted diagnostics for SG boundary mode (`OPEN` vs `ISOLATED`), steam inventory accumulation, and pressure-source branch selection.

## Proposed Validation Checks and Stage E Rerun Criteria

### Stage E Scenarios
- Scenario 1: Cold startup heat-up from wet layup through boiling onset and SG pressurization window.
- Scenario 2: Isolated SG segment validation (forced isolated boundary path).
- Scenario 3: Heat balance stress case with active RCP and heater sources.

### Required Metrics and Pass/Fail Criteria
- SG boundary mode:
  - Pass: expected startup segment enters `ISOLATED` branch when commanded; not permanently `OPEN`.
- Steam inventory:
  - Pass: `SteamInventory_lb` increases during isolated boiling intervals; not pinned near zero while boiling is active.
- Secondary pressure progression:
  - Pass: SG pressure departs near-atmospheric floor during heat-up and tracks increasing saturation trajectory; no prolonged pinning near 17 psia once boiling/pressurization segment is active.
- Heat balance:
  - Pass: sustained startup window shows positive net heat input trend (no persistent collapse to zero/negative during intended heat-up segment).
- Startup progression:
  - Pass: RCS heat-up does not stall after boiling onset due to SG sink dominance.

### Definition-of-Done Style Completion Criteria
- [ ] All included CS items map to implemented behavior changes in one authorized IP execution.
- [ ] Stage E scenarios run to completion with archived logs and metric table.
- [ ] SG pressure, steam inventory, and net heat metrics meet pass thresholds above.
- [ ] No new Critical conservation or startup-blocking regressions introduced in adjacent domains.

