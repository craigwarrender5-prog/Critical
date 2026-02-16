# IP-0030 - DP-0008 - Validation Dashboard Complete Redesign

Date: 2026-02-16
Status: FAILED
Domain Plan: DP-0008 - Operator Interface and Scenarios
Supersedes: IP-0025, IP-0029
Changelog Required: Yes (required at closeout per constitutional closure workflow)
Future Features Required: No
Primary Code Scope: `Assets/Scripts/Validation`

---

## 1. Purpose

Replace the current Validation Dashboard under `Assets/Scripts/Validation` with a full uGUI-based redesign that:

1. Presents critical plant and simulation health data on a single primary surface with no scrolling.
2. Layers additional detail in structured tabs/panels without burying critical information.
3. Supports modern visual behavior (animated gauges, transitions, strip charts, alarm emphasis).
4. Covers the full required monitoring parameter set provided by project instruction.
5. Is explicit enough for Codex to implement with minimal interpretation drift.

---

## 2. Gaps Resolved From IP-0025 and IP-0029

IP-0030 resolves incompleteness and ambiguity in prior drafts by freezing:

1. One canonical stage sequence and exit gates.
2. Full parameter inventory as mandatory scope (not optional wishlist).
3. Clear disposition rules for unavailable telemetry (no silent omission).
4. Explicit changelog-at-closeout and no-future-features execution constraints.
5. Required evidence artifacts per stage for traceability and audit.

---

## 3. Non-Negotiable Constraints

1. UI and observability work only. No simulator physics behavior changes.
2. No redesign of unrelated systems outside validation dashboard scope.
3. Changelog/version entry is required at closeout; no pre-selection of version before closure evidence is complete.
4. No Future Features records for this effort. Unresolved scope is handled inside this IP and must be explicitly dispositioned.
5. Existing `HeatupValidationVisual` implementation remains available as fallback until Stage E completion.
6. All major execution decisions and outcomes must be recorded in stage evidence artifacts under `Governance/Issues`.

---

## 4. Information Architecture (Target)

### 4.1 Primary Operations Surface (always visible, no tab switch)

The primary dashboard surface must continuously show:

1. Global simulation state and stability.
2. RCS pressure and thermal state.
3. PZR pressure, level, heater state, spray state.
4. Net CVCS behavior and VCT health.
5. SG and RHR high-level state.
6. Active safety/limit alarms and inhibit reasons.
7. Always-on trend strips for critical variables.

### 4.2 Layered Detail Surfaces

Detail tabs/panels are required for deeper diagnostics:

1. Reactor and Core
2. RCS Loop Detail
3. Pressurizer
4. CVCS and VCT
5. BRS
6. RHR
7. Steam Generators
8. Safety and Limits
9. Validation and Event Log
10. Expanded Trends

---

## 5. Mandatory Parameter Scope

All listed parameters are in scope for monitoring presentation. Each parameter must be classified in Stage A as:

1. `Native` (directly available)
2. `Derived` (computed from existing data)
3. `Unavailable` (not currently modeled or not exposed)

`Unavailable` parameters are not silently dropped. They must be represented in the UI with clear "N/A - Not Modeled/Exposed" status and documented source gap.

### 5.1 Global Simulation Health

- Sim time (s/hr), sim rate, paused/running
- Fixed/variable timestep, current dt
- Integration stability flags (divergence, clamp hits, NaN/Inf)
- Mass conservation error (instant and cumulative)
- Energy conservation error (instant and cumulative)
- Total heat added, total heat removed, net heat

### 5.2 Reactor and Core

- Reactor power (% and MWt)
- Decay heat (if modeled separately)
- Core inlet temp, core outlet temp
- Tcold, Thot, Tavg
- Core delta-T
- Core flow
- Core heat generation rate
- Reactivity total and breakdown (rod worth/position, boron worth, moderator feedback, Doppler feedback)

### 5.3 RCS (Primary Loops)

- RCS pressure
- RCS total mass/inventory
- Bulk average density
- Subcooling margin
- Per-loop A/B/C/D hot leg temp, cold leg temp, loop delta-T, loop mass flow, SG primary inlet/outlet temps
- RCP status, speed, torque/amps (if available), pump head
- Natural circulation estimate (RCPs off)
- Void fraction outside PZR

### 5.4 Pressurizer (PZR)

- PZR pressure
- PZR level, liquid mass, steam mass (or volumes)
- Liquid and steam temperatures
- Saturation temperature at pressure
- Subcooling/superheat in PZR regions
- Heater mode/state
- Heater demand and actual output
- Heater groups active
- Heater ramp rate and limiter flags
- Spray valve position, spray flow, spray inlet temperature
- Surge line flow (signed), temperature, delta-P
- Pressure error and level error
- Net energy into PZR

### 5.5 CVCS, VCT, and BRS

- Charging pump status, total flow, to-RCS flow, charging temperature, charging pressure
- Letdown total flow, per-orifice states/flows, letdown temperature, letdown pressure
- Net CVCS flow and integrated mass effect
- VCT level, mass/volume, temperature, pressure, gas blanket pressure
- VCT inflow/outflow rates
- VCT heater/cooling status (if modeled)
- Charging suction margin / NPSH indicator (if modeled)
- BRS tank level, concentration, transfer/makeup status, BRS-to-VCT flow, BRS-to-RCS/CVCS flow, boron mass balance (if available)

### 5.6 RHR

- Suction source, discharge destination, key isolation valve states
- Pump status, flow, line pressure/delta-P
- HX inlet/outlet temperatures
- Heat removed by RHR
- Interlocks and permissive states
- Minimum flow protection status

### 5.7 Steam Generators (Per SG/Loop)

- Secondary pressure
- SG level (narrow/wide where available)
- Feedwater flow
- Steam flow
- Steam dump/relief position or flow
- Secondary temperature (steam or saturation)
- Primary-to-secondary heat transfer
- Blowdown flow

### 5.8 Safety, Limits, and Alarms

- High/low RCS pressure alarms
- High/low PZR level alarms
- Heater inhibited reasons
- Spray inhibited reasons
- RHR unavailable reasons
- Max dP/dt exceeded
- Max dT/dt exceeded
- Conservation error threshold exceeded

### 5.9 Always-On Trends

For each trend, show instant value and short history:

- RCS pressure
- PZR pressure
- PZR level
- Heater demand vs actual output
- Spray flow
- Charging, letdown, net CVCS
- Tavg, Thot, Tcold, core delta-T
- SG pressure (per loop where available)
- Mass and energy conservation errors

---

## 6. Technical Design Requirements

1. Use Unity uGUI + TextMeshPro for all new dashboard rendering.
2. Use componentized widgets:
   - Arc gauges
   - Bi-directional gauges
   - Strip charts
   - Digital readouts
   - LED/alarm indicators
   - Bar gauges
3. Use smooth transitions for needles, color state changes, and panel visibility.
4. Preserve keyboard/operator workflow parity where applicable.
5. Avoid per-frame allocation patterns in refresh loops.
6. Refresh data at controlled rate (target: 5-10 Hz), render at frame rate.
7. Primary screen target resolution: 1920x1080 with no vertical scrolling.
8. Trend pipelines must use preallocated fixed-size ring buffers (no runtime list growth in refresh path).
9. Primary surface alarm readability takes precedence over secondary metric density.

---

## 7. Stage Plan and Exit Gates

Execution order is strict. Stage N+1 does not start until Stage N is marked PASS.

### Stage A - Repository Reality and Telemetry Traceability Freeze

Deliverable:
`Governance/Issues/IP-0030_StageA_RealityTraceability_<timestamp>.md`

Required work:

1. Map current validation entry points and wiring under `Assets/Scripts/Validation`.
2. Build a traceability matrix for every parameter in Section 5:
   - Parameter
   - Source file and field/function path
   - Units
   - Native/Derived/Unavailable disposition
   - Notes on any ambiguity
3. Identify existing tab structure and critical visibility gaps.
4. Freeze baseline screenshots/log references for current dashboard behavior.

Exit criteria:

1. 100 percent of Section 5 parameters are dispositioned.
2. Entry-point map is explicit and reproducible.
3. Baseline visibility and usability gaps are documented.

### Stage B - UX and Data Contract Design Freeze

Deliverable:
`Governance/Issues/IP-0030_StageB_DesignFreeze_<timestamp>.md`

Required work:

1. Freeze primary layout and detail-tab information architecture.
2. Freeze widget standards (gauge ranges, color semantics, alarm behavior, trend windows).
3. Freeze data-binding contract:
   - Snapshot schema
   - Update cadence
   - Threshold evaluation semantics
4. Freeze treatment for `Unavailable` parameters (visible N/A with reason).
5. Freeze primary-surface density contract:
   - Explicit list of parameters allowed on primary surface
   - Maximum widget count allowed on primary surface
   - Minimum visual footprint for critical gauges/alarms/trends
6. Freeze `Unavailable` visual standard:
   - Dimmed neutral styling
   - Compact N/A footprint
   - Tooltip or inline reason code
   - Rule for whether N/A appears on primary surface vs detail-only surfaces

Exit criteria:

1. No unresolved layout ambiguity.
2. No unresolved data contract ambiguity.
3. Critical primary-surface list is explicit and complete.
4. Primary-surface density limits are numerically declared and unambiguous.
5. `Unavailable` representation rules are explicit and consistent across panels.

### Stage C - Framework and Primary Surface Implementation

Deliverable:
`Governance/Issues/IP-0030_StageC_PrimarySurface_<timestamp>.md`

Required work:

1. Implement dashboard framework (canvas lifecycle, binder, shared components).
2. Implement primary operations surface with always-visible critical metrics.
3. Implement always-on trend panel for critical trend set.
4. Integrate alarm summary and inhibit-reason visibility.
5. Preserve fallback access to legacy dashboard.
6. Implement trend/history storage using preallocated ring buffers only.
7. Capture allocation profile evidence for refresh path and trend updates.

Exit criteria:

1. Primary surface renders and updates correctly at runtime.
2. All critical metrics/trends from Section 4.1 are visible without tab switching.
3. No blocking regressions in simulation runtime behavior.
4. Stage C evidence confirms no recurring GC allocations from dashboard refresh/trend paths under steady runtime.

### Stage D - Full Parameter Coverage and Visual Completion

Deliverable:
`Governance/Issues/IP-0030_StageD_FullCoverage_<timestamp>.md`

Required work:

1. Implement detail tabs/panels to cover full Section 5 scope.
2. Implement advanced visual behavior (animations/transitions/gauge polish).
3. Implement explicit displays for all `Unavailable` parameters with reason codes.
4. Complete safety/limits panel and full alarm reason presentation.

Exit criteria:

1. Every Section 5 parameter is visible as Native, Derived, or explicit N/A.
2. No silent omissions.
3. Visual polish meets design freeze requirements.

### Stage E - Validation, Regression, and Readiness

Deliverable:
`Governance/Issues/IP-0030_StageE_SystemValidation_<timestamp>.md`

Required work:

1. Run validation scenarios (minimum):
   - Baseline startup progression
   - RCP-off/low-flow regime
   - CVCS and PZR active control regime
   - RHR/SG interaction regime
2. Verify parameter correctness and trend integrity.
3. Verify alarm/inhibit reason correctness and no log/event spam.
4. Verify performance budget and no significant allocation spikes.
5. Produce before/after screenshots and run stamp references.

Exit criteria:

1. All stage acceptance checks PASS or have explicit approved disposition.
2. No blocking regressions.
3. Dashboard is execution-ready for closure workflow.

---

## 8. Required File and Folder Targets

Primary implementation root:
`Assets/Scripts/ValidationDashboard/`

Minimum structure:

1. `Assets/Scripts/ValidationDashboard/Core/`
2. `Assets/Scripts/ValidationDashboard/Components/`
3. `Assets/Scripts/ValidationDashboard/Panels/`
4. `Assets/Scripts/ValidationDashboard/Overlays/`
5. `Assets/Scripts/ValidationDashboard/Styles/`

Expected integration touchpoints:

1. `Assets/Scripts/Validation/HeatupValidationVisual.cs` (fallback/toggle wiring only)
2. Optional lightweight integration/bootstrap files in `Assets/Scripts/Validation/` if needed for scene lifecycle

Hard boundary:

No physics-model functional changes in `HeatupSimEngine*` files unless strictly required to expose already-existing telemetry without changing model behavior.

---

## 9. Codex Execution Precision Rules

1. Do not skip stages.
2. Do not implement beyond the current authorized stage.
3. At each stage end, provide:
   - Artifact path
   - PASS/FAIL status against exit criteria
   - Files changed with reason per file
   - Run stamps/log paths
4. Any discovered out-of-scope dependency is logged and routed, not absorbed.
5. Create changelog/version entry only during closure workflow after validation evidence is complete.

---

## 10. Completion Criteria for IP-0030

IP-0030 is complete when:

1. Primary operations surface delivers critical-at-a-glance monitoring.
2. Full required parameter set is represented with explicit disposition.
3. Trend and alarm behavior are operational and deterministic.
4. Validation scenarios pass with no blocking regressions.
5. Stage artifacts A-E exist with traceable evidence.

---

## 11. Authorization Prompt

Upon approval, execution proceeds Stage A through Stage E in strict order under this plan.
