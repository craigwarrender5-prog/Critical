---
IP ID: IP-0015
DP ID: DP-0003
Title: SG Secondary Physics Stabilization and Heat-up Progression Recovery
Status: CLOSED (Stage E PASS - 2026-02-14)
Severity: Critical
Included CS: CS-0014, CS-0015, CS-0016, CS-0018, CS-0047, CS-0048
Date: 2026-02-14
Mode: IMPLEMENTATION
---

# IP-0015 - DP-0003 SG Secondary Physics

## 1) Header / Frontmatter
- IP ID: `IP-0015`
- DP ID: `DP-0003`
- Title: SG Secondary Physics Stabilization and Heat-up Progression Recovery
- Status: `CLOSED (Stage E PASS - 2026-02-14)`
- Severity: `Critical` (highest included issue severity is Critical)
- Included CS:
  - `CS-0014` - SG "ISOLATED" mode behaves like open inventory pressure boundary
  - `CS-0015` - Steam generation does not accumulate compressible volume/mass
  - `CS-0016` - SG modeled as unrealistically strong heat sink during heatup
  - `CS-0018` - N2 blanket treated as pressure clamp, not compressible cushion
  - `CS-0047` - Heat-up progression stalls during intended startup heat addition
  - `CS-0048` - SG secondary stays near atmospheric and behaves as constant heat sink

## 2) Objective
Restore physically consistent SG secondary startup behavior so heat-up can progress: SG boundary state must transition correctly, pressure must rise with steam generation in the intended startup segment, and SG heat removal must remain bounded so net plant heat does not collapse during heat-up.

## 2.1) Closure Evidence (Stage E PASS)
- `Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_164359.md`
- `Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_164456.md`

## 3) Scope
In scope:
- DP-0003 SG boundary-state and pressure-path behavior needed to clear the startup blocker chain.
- Steam inventory/compressible-volume behavior required to support pressure rise in isolated segment.
- N2 blanket behavior update from fixed clamp style to compressible contribution model.
- SG sink guardrails at SG-RCS interface for startup realism and non-negative heat-up progression.
- Evidence and validation instrumentation needed for Stage E rerun decisions.

Out of scope:
- Pressurizer/two-phase domain fixes (`DP-0002`).
- CVCS/VCT conservation fixes (`DP-0004`/`DP-0005`).
- UI/dashboard refactors.
- Release/versioning/changelog actions.
- No modification of the primary RCS energy balance equation.
- No introduction of new global plant heat terms.
- No artificial source-vs-sink algebraic balancing logic.
- No cross-domain fixes outside DP-0003.

## 4) Work Breakdown
Dependency order:
1. Boundary sealing
2. Steam accumulation
3. N2 model
4. Heat balance caps
5. Validation instrumentation and Stage E rerun package

Task 1 - Boundary sealing activation
- Expected files touched:
  - `Assets/Scripts/Validation/HeatupSimEngine.cs`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs`
- Key variables/branches:
  - `SteamIsolated`
  - `SetSteamIsolation(...)`
  - SG startup state branch selection
- Rationale:
  - Isolation path is present but not activated in runtime control flow; this must be wired first.

Task 2 - Steam accumulation as active pressure source in isolated segment
- Expected files touched:
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs`
- Key variables/branches:
  - `SteamInventory_lb`
  - `SteamSpaceVolume_ft3`
  - `InventoryPressure_psia`
  - `SteamOutflow_lbhr`
- Rationale:
  - Steam inventory is computed but not currently driving pressure in default open path; isolated path behavior must be authoritative where required.

Task 3 - N2 blanket compressible model
- Expected files touched:
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs`
  - `Assets/Scripts/Physics/PlantConstants.SG.cs`
- Key variables/branches:
  - `NitrogenIsolated`
  - `SG_INITIAL_PRESSURE_PSIA`
  - pre-steaming and pressure-floor logic
- Rationale:
  - Fixed clamp behavior must be replaced by bounded compressible contribution to avoid unrealistic pressure pinning.
- Scope Clarification:
  - This IP will implement a minimal compressible gas cushion model sufficient to allow pressure rise during isolated startup.
  - It will not introduce a full multi-phase thermodynamic gas subsystem.
  - Acceptable implementation approaches include:
    - Simplified PV=nRT formulation with fixed nitrogen mass.
    - Equivalent bounded pressure-volume response curve derived from compressible cushion assumption.
  - The objective is startup pressure progression realism, not full nitrogen thermodynamic fidelity.

Task 4 - SG sink guardrails at coupling boundary
- Expected files touched:
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs`
  - `Assets/Scripts/Physics/RCSHeatup.cs`
- Key variables/branches:
  - `TotalHeatAbsorption_MW`
  - `TotalHeatRemoval_MW`
  - `rcsNetHeat_MW`
- Rationale:
  - SG heat removal must remain physically bounded by secondary-side state (pressure, phase regime, compressible volume, and valid heat-transfer coefficients).
  - No artificial clamp may be introduced that directly limits SG heat removal to match or track primary source heat.
  - Any reduction in SG sink strength must arise from corrected boundary behavior (sealed inventory, pressure rise, saturation shift, realistic delta-T), not from an imposed comparison against `rcsNetHeat_MW`.
  - Guardrails must therefore be thermodynamically derived, not algebraically enforced.
- Explicit Constraint:
  - The implementation must not introduce a hard cap of the form: `TotalHeatRemoval_MW = Min(TotalHeatRemoval_MW, SourceHeat)`.
  - The SG model must remain first-law compliant.
  - The plant energy equation in `RCSHeatup.cs` must not be modified in this IP.

Task 5 - Diagnostics and Stage E evidence hooks
- Expected files touched:
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs`
  - `Assets/Scripts/Validation/HeatupSimEngine.cs`
  - `HeatupLogs/*` output formatting paths as needed
- Key variables/branches:
  - branch mode telemetry (`OPEN`/`ISOLATED`)
  - steam inventory and pressure source diagnostics
  - net heat trend diagnostics
- Rationale:
  - Stage E acceptance requires unambiguous branch and metric evidence.

## Architectural Integrity Constraints
- SG boundary corrections must be causal and state-driven.
- No behavior may be corrected by masking a downstream symptom.
- Pressure rise must emerge from steam accumulation and compressible volume behavior.
- Heat balance stabilization must emerge from corrected SG state evolution.
- All changes must preserve canonical mass conservation and existing primary-side validation guarantees.

## 5) Risks and Non-Regression Concerns
- Two-phase interaction risk:
  - SG pressure-path changes may alter boiling onset timing and affect coupled two-phase transitions.
- Heat-balance regression risk:
  - Over-constraining SG sink could underpredict heat transfer if guardrails are too aggressive.
- Startup sequence risk:
  - Boundary-state transitions can create discontinuities if branch handoff is not smooth.
- Validation/telemetry risk:
  - Missing branch diagnostics can make Stage E outcomes ambiguous.
- UI/ops interpretation risk:
  - New telemetry fields must remain readable and consistent with existing operator logs.

## 6) Validation Plan (Stage E-ready)
Scenarios to run:
1. Full cold startup heat-up from wet layup through boiling onset and SG pressurization window.
2. Isolated SG startup segment scenario (branch-forcing diagnostic run).
3. Heat-balance stress scenario with high SG demand and active primary heat sources.

Metrics that must change:
- SG boundary state transitions from open to isolated in expected startup segment.
- `SteamInventory_lb` accumulates during isolated boiling intervals.
- SG secondary pressure rises off near-atmospheric floor during startup pressurization window.
- Net heat input remains positive through intended heat-up progression window.
- RCS temperature resumes positive progression through startup segment.

Pass/fail thresholds:
- Fail if SG remains effectively open for entire startup heat-up where isolated behavior is expected.
- Fail if SG pressure remains pinned near 17 psia during active pressurization segment.
- Fail if steam inventory remains near zero during isolated boiling.
- Fail if sustained net heat input trends to zero/negative during intended heat-up progression.
- Pass when all above criteria hold across Stage E rerun logs.

## 7) Definition of Done
- [x] All included CS issues (`CS-0014`, `CS-0015`, `CS-0016`, `CS-0018`, `CS-0047`, `CS-0048`) have implemented changes mapped to completed tasks.
- [x] Dependency order executed without skipping prerequisite tasks.
- [x] Stage E scenarios completed and archived with reproducible settings.
- [x] Evidence package shows pressure progression, steam accumulation, and positive net heat trend in required windows.
- [x] No new Critical regressions introduced in adjacent DP domains.
- [x] Issue registry updated to closed status for IP-0015 scoped CS with evidence-backed archive snapshots.
- [x] IP status moved from `DRAFT` to `AUTHORIZED` by explicit approval dated 2026-02-14.

## 8) Evidence Links
- Preliminary investigation report:
  - `Updates/Issues/DP-0003_Preliminary_Investigation_Report.md`
- Implementation evidence (this cycle):
  - `Updates/Issues/IP-0015_Implementation_Evidence_2026-02-14.md`
  - `Updates/Issues/IP-0015_Closure_Report_2026-02-14.md`
- Key evidence artifacts:
  - `Technical_Documentation/SG_Secondary_Pressurization_During_Heatup_Research.md:13`
  - `Updates/Archive/IMPLEMENTATION_PLAN_v5.1.0.md:37`
  - `Updates/Archive/IMPLEMENTATION_PLAN_v5.1.0.md:38`
  - `HeatupLogs/Heatup_Interval_001_0.00hr.txt:155`
  - `HeatupLogs/Heatup_Interval_001_0.00hr.txt:160`

## 9) Closure Addendum (2026-02-14)
- Closure status: `CLOSED (Stage E PASS - 2026-02-14)`
- Authoritative Stage E rerun evidence:
  - `Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_164359.md`
  - `Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_164456.md`
- PASS criteria confirmed in both reruns:
  - SG pressure departs atmospheric floor during isolated boiling
  - Steam inventory accumulates while isolated
  - Net plant heat remains positive during startup window
  - RCS heat-up no longer stalls post-boiling
  - No new conservation regressions
- Historical context is preserved; this addendum formalizes closure using the above evidence set.
