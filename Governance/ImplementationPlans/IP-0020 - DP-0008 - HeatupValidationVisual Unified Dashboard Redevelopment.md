---
IP ID: IP-0020
DP Reference: DP-0008
Title: HeatupValidationVisual Unified Dashboard Redevelopment
Status: CLOSED
Resolution: FAILED
Closed At: 2026-02-15
Date: 2026-02-15
Mode: CLOSED
Source of Scope Truth: Governance/IssueRegister/issue_register.json
Primary Design Reference: Dashboard Proposal.png (provided design image)
---

# IP-0020 - DP-0008 - HeatupValidationVisual Unified Dashboard Redevelopment

## Closure Resolution
- Resolution: `FAILED`
- Closed on: `2026-02-15`
- Closure note: This plan did not meet its execution objectives and is closed without implementation acceptance. Outstanding Phase A-F execution tasks and IP-0020-only evidence targets are canceled; follow-on work must be re-planned under a new or revised IP.

## 1) Governance Header
- DP Reference: `DP-0008 - Operator Interface & Scenarios`
- Authorization state: `CLOSED - FAILED` (closed on 2026-02-15)
- Scope basis: all ACTIVE CS with `assigned_dp = DP-0008` in `Governance/IssueRegister/issue_register.json`
- Implementation intent: full UI redevelopment of `HeatupValidationVisual` to a command-center profile matching the provided design reference

## 2) Full ACTIVE CS Scope (DP-0008)
| CS ID | Title | Severity | Status |
|---|---|---|---|
| CS-0077 | HeatupValidationVisual redesign from tabbed layout to unified instrumentation screen | HIGH | READY_FOR_FIX |
| CS-0037 | Surge line flow direction and net inventory display enhancement | LOW | READY_FOR_FIX |

## 3) Scope Boundary and Hard Constraints
In scope:
- Full redevelopment of `HeatupValidationVisual` presentation architecture and layout.
- Primary operator experience must be a single-screen critical instrumentation profile at `1920x1080` with no scrolling.
- Tab architecture is allowed only if Tab 1 contains all critical telemetry needed for operator decision-making without tab switching.
- Optional secondary tabs may contain historical or expanded trend detail only when required, and only after the first tab remains complete and uncluttered.
- Integrate `CS-0037` requirements directly into the redesigned dashboard (flow direction and net inventory observability).

Out of scope:
- Physics model behavior or equations.
- Validation logic and acceptance-rule logic.
- Plant mode sequencing/state machine behavior.
- Cross-domain remediations outside `DP-0008`.

Non-negotiable constraints:
- UI update rate limited to `<= 10 Hz`.
- Trend data via fixed-size ring buffers.
- No per-frame allocations and no LINQ in update loops.
- No measurable frame-time regression at `10x` simulation speed.

## 4) UI Ownership and Current Technical Baseline
Screen ownership map:
- Scene entrypoint: `Assets/Scenes/Validator.unity` (component `HeatupValidationVisual`)
- Core controller: `Assets/Scripts/Validation/HeatupValidationVisual.cs`
- Current tab partials:
  - `Assets/Scripts/Validation/HeatupValidationVisual.TabOverview.cs`
  - `Assets/Scripts/Validation/HeatupValidationVisual.TabPressurizer.cs`
  - `Assets/Scripts/Validation/HeatupValidationVisual.TabCVCS.cs`
  - `Assets/Scripts/Validation/HeatupValidationVisual.TabSGRHR.cs`
  - `Assets/Scripts/Validation/HeatupValidationVisual.TabRCPElectrical.cs`
  - `Assets/Scripts/Validation/HeatupValidationVisual.TabEventLog.cs`
  - `Assets/Scripts/Validation/HeatupValidationVisual.TabValidation.cs`
  - `Assets/Scripts/Validation/HeatupValidationVisual.TabCritical.cs`
- Shared rendering helpers:
  - `Assets/Scripts/Validation/HeatupValidationVisual.Styles.cs`
  - `Assets/Scripts/Validation/HeatupValidationVisual.Gauges.cs`
  - `Assets/Scripts/Validation/HeatupValidationVisual.Panels.cs`
  - `Assets/Scripts/Validation/HeatupValidationVisual.Graphs.cs`
  - `Assets/Scripts/Validation/HeatupValidationVisual.Annunciators.cs`

Current update/render model:
- UI framework: IMGUI (`OnGUI`) in `Assets/Scripts/Validation/HeatupValidationVisual.cs`.
- Refresh throttle: `refreshRate` with layout-event gating in `OnGUI` (`<= 10 Hz` target).
- Data source: direct reads from public `HeatupSimEngine` fields (no separate telemetry snapshot boundary yet).

Planned boundary rule for redevelopment:
- Introduce an internal read-only telemetry snapshot at UI boundary (captured at throttled refresh cadence) so all rendering code consumes snapshot values, not live scattered engine reads.

## 5) Functional, Visual, and Performance Requirements
Functional requirements:
- Remove dependency on multi-tab hunting for critical telemetry.
- Deliver a single primary dashboard tab that simultaneously shows:
  - Primary (RCS pressure, temperature, trends)
  - Pressurizer (pressure, level, temperature, subcooling, heaters/sprays)
  - Secondary (SG pressure/temperature with worst-case indicators)
  - Inventory/CVCS (charging, letdown, net flow, system mass, VCT level)
  - System mass/conservation with drift indicator
- Alarm strip is top visual priority and limited to `4-6` active alarms.
- Net flow direction and rate are always visible and explicit (value plus directional cue).

Visual requirements:
- Replicate the provided design reference style and hierarchy as the target operator presentation.
- First-tab layout must remain readable at `1920x1080` with no scrolling.
- Use compact micro-sparklines only (uniform `3-minute` lookback) for at-a-glance trends.
- Historical/deeper trend panels are permitted only on secondary tabs if needed.

Performance requirements:
- UI update loop rate-limited to `<= 10 Hz`.
- Fixed-capacity ring buffers for trends.
- Zero allocation behavior in steady-state draw/update paths.
- No LINQ in update loops.
- Must degrade gracefully when telemetry is stale or unavailable.

## 6) Work Breakdown (Execution Phases)
1. Phase A - Baseline and Architectural Freeze
   - Freeze current `DP-0008` scope (`CS-0077`, `CS-0037`) and acceptance gates.
   - Lock IMGUI-first implementation path for this IP (no framework migration).
   - Define telemetry snapshot struct and collection cadence at UI boundary.
2. Phase B - Primary Tab Information Architecture
   - Build command-center wireframe matching provided design reference.
   - Define top alarm strip, core cards, gauge/sparkline placement, and critical typography scale.
   - Ensure no-scroll fit at `1920x1080`.
3. Phase C - HeatupValidationVisual Redevelopment
   - Redevelop `HeatupValidationVisual` tab flow so critical profile is default first tab.
   - Recompose existing partial responsibilities around new primary tab sections.
   - Maintain optional secondary tabs only for non-critical history/expanded detail.
4. Phase D - CS-0037 Integration
   - Add persistent surge/net-flow direction indicators.
   - Add explicit net inventory trend and sign-aware display elements.
5. Phase E - Performance Hardening
   - Implement fixed-size ring buffers for all displayed sparklines.
   - Remove any per-refresh allocations in steady-state paths.
   - Confirm refresh throttling, sampling cadence, and draw-path stability at `10x`.
6. Phase F - Verification and Closeout Readiness
   - Side-by-side visual compliance review against design reference.
   - Under-load runtime check with frame-time comparison.
   - Produce Stage A-E evidence package for closure decision.

## 7) CS-to-Work Mapping
| CS ID | Planned remediation in this IP | Acceptance signal |
|---|---|---|
| CS-0077 | Full command-center redesign of `HeatupValidationVisual` with first-tab critical overview and alarm-first hierarchy; optional history moved to secondary tabs only. | All critical sections visible at once on first tab at 1920x1080, no scrolling, and visual hierarchy matches target reference. |
| CS-0037 | Embed directional surge and net inventory observability directly in first-tab inventory/CVCS and system-mass regions. | Operator can identify net flow direction and magnitude without inference; flow sign and direction remain continuously visible. |

## 8) Stage Gates and Validation Plan
Stage A - Scope and baseline confirmation:
- Confirm in-scope files and baseline UI behavior capture.
- Freeze acceptance checklist before structural changes.

Stage B - Design freeze:
- First-tab wireframe and card hierarchy approved against design reference.
- Required telemetry-to-widget mapping completed.

Stage C - Controlled implementation:
- Redevelopment completed in small reversible commits.
- All UI changes traceable to `CS-0077` or `CS-0037`.

Stage D - Domain validation:
- Validate no-scroll `1920x1080` primary tab.
- Validate alarm strip priority, grouped cards, micro-sparklines, and net-flow direction visibility.

Stage E - Performance and non-regression:
- Validate `<=10 Hz` UI update cadence.
- Validate fixed-size trend buffers and no per-frame allocations.
- Validate no measurable frame-time regression at `10x` sim speed.
- Validate graceful telemetry-unavailable behavior.

## 9) Risk Assessment
| Risk | Impact | Likelihood | Mitigation |
|---|---|---|---|
| Direct engine-field reads remain scattered | Weakens UI boundary and maintainability | High | Enforce single telemetry snapshot ingestion point for rendering. |
| IMGUI draw density causes frame-time rise | Runtime performance regression | Medium | Strict update throttling, ring buffers, cached styles/text, staged profiling. |
| First-tab overcrowding at 1080p | Operator readability degradation | Medium | Freeze card hierarchy early and demote non-critical detail to optional secondary tabs. |
| Scope creep into physics/validation logic | Governance violation | Medium | Restrict edits to `HeatupValidationVisual*` and UI-facing display logic only. |

## 10) Definition of Done
- Both `DP-0008` active CS (`CS-0077`, `CS-0037`) have implemented and validated remediation evidence.
- Primary tab delivers complete critical telemetry view with no required tab switching.
- Design reference replication is judged acceptable in side-by-side review.
- Performance gates pass under `10x` simulation speed.
- No physics, validation logic, or acceptance-rule logic changes were introduced.

## 11) Evidence Targets
- Plan: `Governance/ImplementationPlans/IP-0020 - DP-0008 - HeatupValidationVisual Unified Dashboard Redevelopment.md`
- Stage evidence package: `CANCELED` (IP closed as FAILED on 2026-02-15)
