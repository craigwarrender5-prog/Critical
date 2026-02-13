# IMPLEMENTATION PLAN v0.2.2.0
## Regime Transition Logging and Event Traceability

---

## Document Control

| Field | Value |
|-------|-------|
| Version | v0.2.2.0 |
| Plan Severity | **Low** |
| Architectural Domain | Observability — Event Logging / Regime Traceability |
| System Area | HeatupSimEngine (regime selection), HeatupSimEngine.Alarms |
| Discipline | Observability |
| Status | **PLACEHOLDER — Not active until v0.1.0.0 closes** |
| Constitution Reference | PROJECT_CONSTITUTION.md v0.1.0.0, Articles V, III |
| Deferral Authority | Article V Section 5 — issue deferred from v0.1.0.0 scope (different architectural domain) |

**Not active until v0.1.0.0 closes; created to satisfy Article V Section 5 deferral rule.**

---

## Purpose

Add structured event logging for physics regime transitions. Currently, plant mode transitions are logged but regime transitions (Isolated/Blended/Coupled, driven by RCP alpha) are not. This makes debugging mass behavior across regime boundaries dependent on inferring regime from RCP count rather than seeing explicit transition events.

---

## Architectural Domain Definition

| Field | Value |
|-------|-------|
| System Area | HeatupSimEngine — Regime Selection / Event Logging |
| Discipline | Observability |
| Architectural Boundary | Regime transition detection, event log output, diagnostic context enrichment |

This domain is distinct from all other domains. It adds logging only — no physics modifications, no conservation changes, no UI changes.

---

## Issues Assigned

| Issue ID | Title | Severity | Deferral Reason |
|----------|-------|----------|-----------------|
| CS-0012 | No regime transition logging | Low | Outside v0.1.0.0 domain (observability, not primary mass conservation) |

**Plan Severity:** Low (highest assigned issue = CS-0012 Low)

---

## Scope

This plan shall:

- Add edge-detected regime transition logging: "REGIME CHANGE: {old} -> {new} (alpha = {value}, RCPs = {count})"
- Log at the point where alpha crosses regime boundaries (0 to blended, blended to 1)
- Include simTime and key state values (T, P, mass) in transition log entry

This plan shall NOT:

- Modify regime selection logic
- Add new alarms for regime transitions
- Modify physics behavior at regime boundaries
- Add mass continuity assertions at transitions (that's v0.1.0.0 Phase A cross-check territory)

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| v0.1.0.0 must close first | Prerequisite | Regime transition logging is most valuable when canonical mass is active — the log entries can include ledger values for debugging |
| No domain conflicts | Independence | This plan modifies only logging code; no shared files with other active plans |

---

## Exit Criteria (Skeleton)

- [ ] CS-0012: Event log contains regime transition entries at each RCP start/stop that changes alpha across boundaries
- [ ] Log entries include: simTime, old regime, new regime, alpha, RCP count, T_rcs, P, TotalPrimaryMass_lb
- [ ] No physics behavior change (logging only)
- [ ] Issue Registry updated, Changelog written, version increment performed

---

## Known Limitations

| Limitation | Impact |
|-----------|--------|
| Logging only — no structural change | Does not prevent regime transition mass discontinuities; only makes them visible |
| No backward transition detection | Does not guard against invalid regime regression (e.g., Coupled back to Isolated); deferred to architecture hardening |
| Plan is a placeholder | No implementation stages defined; will be elaborated when plan becomes active |
