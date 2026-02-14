> LEGACY IMPLEMENTATION ARTIFACT — archived during Constitution v1.0 governance re-baseline.
> Historical record preserved. No current execution authority.

---
Identifier: IP-0013
Domain: Operator & Scenario Framework â€” Dashboard Visualization
Status: Open (BLOCKED â€” Phase 1)
Priority: Medium
Tier: 3
Linked Issues: CS-0042, CS-0037 (subsumed)
Last Reviewed: 2026-02-14
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# IP-0013 â€” Operator and Scenario Framework
## Professional Interactive Dashboard Visualization Upgrade (uGUI Modernization)

---

## Document Control

| Field | Value |
|-------|-------|
| Identifier | **IP-0013** |
| Plan Severity | **Medium** |
| Architectural Domain | Operator & Scenario Framework â€” Dashboard Visualization |
| System Area | HeatupValidationVisual, Operator UI, Telemetry Layer (new) |
| Discipline | Operator & Scenario Framework |
| Status | **BLOCKED â€” Phase 1 (awaiting Phase 0 exit criteria)** |
| Constitution Reference | PROJECT_CONSTITUTION.md, Articles V, III, XII |
| Phase | Phase 1 |
| Release | To be assigned upon Phase 1 scheduling (aligned with v5.5.0.0 roadmap entry) |

**Not active until Phase 0 exit criteria are satisfied. All Phase 0 critical/high issues must be resolved or mitigated.**

---

## Purpose

Deliver a professional, fully animated uGUI instrumentation layer using arc gauges, strip gauges, animated transitions, directional flow indicators, and interactive panels â€” while preserving strict telemetry decoupling from physics.

The current dashboard exposes correct telemetry but lacks professional-grade instrumentation, animated transitions, directional clarity, and interactive visualization. Heavy reliance on static numeric text increases cognitive load and slows debugging.

---

## Architectural Domain Definition

| Field | Value |
|-------|-------|
| System Area | HeatupValidationVisual (current), new uGUI components, Telemetry Layer |
| Discipline | Operator & Scenario Framework â€” Dashboard Visualization |
| Architectural Boundary | UI presentation layer and telemetry bus only; no physics, no conservation, no regime logic |

This domain is distinct from:
- All physics domains (no physics changes of any kind)
- Validation / Diagnostic Display (CS-0041): metric correctness fix (Phase 0 residual, prerequisite for this plan)
- Performance / Runtime Architecture (CS-0032): frame budget is an acceptance criterion, not a separate scope item

---

## Issues Assigned

| Issue ID | Title | Severity | Notes |
|----------|-------|----------|-------|
| CS-0042 | Professional interactive dashboard visualization upgrade (uGUI modernization) | Medium | Full dashboard overhaul: arc gauges, strip gauges, animations, directional indicators, telemetry decoupling |
| CS-0037 | Surge line flow direction and net inventory display enhancement | Low | Subsumed by CS-0042 scope: directional flow indicators are a subset of the dashboard modernization |

**Plan Severity:** Medium (highest assigned issue = CS-0042 Medium)

---

## Scope

This plan shall:

- **Arc gauges:** Pressure, temperature, % power, and similar analog parameters with animated needle sweep
- **Strip gauges:** Level, flow, and heater output with animated fill transitions
- **Animated transitions:** Smooth interpolated motion for all gauges, indicators, and fill levels (no jump-cuts unless alarm trip demands immediate visual snap)
- **Rolling strip charts:** Buffered telemetry history using per-channel ring buffers (default 120s retention at current sampling rate, pre-allocated at init)
- **Flow direction indicators:** Explicit directional visualization for all mass/flow transfers (PZRâ†’RCS, RCSâ†’PZR, CVCS charging/letdown, RHR, seal flow) with arrows and/or color convention (subsumes CS-0037)
- **Alarm escalation semantics:** Standardized four-tier escalation: Normal â†’ Warn â†’ Alarm â†’ Trip, each with defined color, border, and animation behavior
- **Interactive detail panels:** Tooltips and expandable detail views for secondary parameter inspection
- **Telemetry decoupling:** Telemetry bus / observer layer that publishes snapshots; no UI component may read physics state objects directly
- **Net inventory display:** Derived net mass change of PZR and/or net CVCS flow (from CS-0037)

This plan shall NOT:

- Modify physics equations, conservation logic, or state computation
- Modify regime selection or bubble formation logic
- Refactor architecture beyond the telemetry decoupling boundary
- Implement a performance overhaul (performance is an acceptance criterion, not a scope item)

---

## UI/UX Design Principles (Hard Requirements)

1. **Animated state transitions** â€” Gauges, indicators, and fill levels must use smooth interpolated motion
2. **Clear visual hierarchy** â€” Remove "text wall" panels; prioritize at-a-glance visuals (gauges, bars, sparklines) over dense numeric tables
3. **No primary reliance on raw text blocks** â€” Critical parameters displayed via graphical elements first; raw text permitted only as secondary detail
4. **Standardized alarm/state color semantics** â€” All indicators follow Normal â†’ Warn â†’ Alarm â†’ Trip escalation
5. **UI fully decoupled from physics** â€” All data flows through telemetry/observer layer
6. **Telemetry must support buffered history** â€” Per-channel ring buffers for strip charts and trend displays
7. **Performance: 60 FPS at 10Ã— sim speed** â€” No per-frame heap allocations in render path; object pooling for chart data points; UI update cost â‰¤ 4 ms/frame

---

## Telemetry & Data Model Notes

- **Data schema:** Each monitored parameter exposed as a `TelemetryChannel` containing current value plus fixed-length history ring buffer. Channels grouped by subsystem (PZR, RCS, SG, CVCS).
- **Update cadence:** Telemetry snapshots published once per physics tick (not per frame). UI interpolates between snapshots for smooth rendering.
- **Buffering approach:** Ring buffer per channel; default retention = 120 seconds. Buffer size pre-allocated at init; no runtime resizing.
- **Hard rule:** UI reads ONLY the published `TelemetrySnapshot`. No UI code may reference `SystemState`, solver internals, or physics module fields directly.

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| Phase 0 exit criteria satisfied | Prerequisite | All Phase 0 critical/high physics issues resolved |
| CS-0032 (Performance blocker) resolved | Prerequisite | Dashboard must run within frame budget |
| CS-0041 (Inventory audit display) resolved | Informational | Conservation display alignment should be correct before dashboard modernization builds on it |

---

## Exit Criteria (Skeleton)

- [ ] CS-0042: 60 FPS sustained at 10Ã— sim speed
- [ ] CS-0042: All UI data sourced exclusively from telemetry snapshot
- [ ] CS-0042: Clear directional visualization of all mass/flow transfers
- [ ] CS-0042: Validation and conservation metrics visually aligned with canonical mass ledger
- [ ] CS-0042: Zero regression in existing validation tests
- [ ] CS-0042: Animated state transitions for all gauges, indicators, and fill levels
- [ ] CS-0042: Clear visual hierarchy â€” no "text wall" panels
- [ ] CS-0042: Telemetry decoupling verified â€” no direct physics reads from any UI component
- [ ] CS-0042: Performance verified with profiling evidence (frame-time captures at 10Ã— speed)
- [ ] CS-0037: Surge flow direction unambiguous in UI during all bubble formation phases
- [ ] CS-0037: Net PZR mass change rate or net CVCS flow displayed
- [ ] Issue Registry updated, Changelog written, version increment performed

---

## Known Limitations

| Limitation | Impact |
|-----------|--------|
| Phase 1 blocker â€” cannot begin until Phase 0 exits | All Phase 0 critical/high issues must be resolved first |
| Telemetry layer is new infrastructure | Requires design and implementation; no existing telemetry bus exists |
| uGUI component library may need custom shaders | Arc gauges and strip charts may require custom rendering; standard Unity UI may not suffice |
| Plan is a placeholder | No implementation stages defined; will be elaborated when Phase 1 scheduling begins |

