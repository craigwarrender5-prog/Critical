> LEGACY IMPLEMENTATION ARTIFACT — archived during Constitution v1.0 governance re-baseline.
> Historical record preserved. No current execution authority.

---
Identifier: IP-0012
Domain: Observability â€” Event Logging / Regime Traceability
Status: Open
Priority: Low
Tier: 4
Linked Issues: CS-0012
Last Reviewed: 2026-02-14
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# IP-0012 â€” Observability
## Regime Transition Logging and Event Traceability

---

## Document Control

| Field | Value |
|-------|-------|
| Identifier | **IP-0012** |
| Plan Severity | **Low** |
| Architectural Domain | Observability â€” Event Logging / Regime Traceability |
| System Area | HeatupSimEngine (regime selection), HeatupSimEngine.Alarms |
| Discipline | Observability |
| Status | **PLACEHOLDER â€” Requires scoping and authorization** |
| Constitution Reference | PROJECT_CONSTITUTION.md, Articles V, III |
| Deferral Authority | Article V Section 7 â€” issue deferred from Primary Mass Conservation plan scope (different architectural domain) |

**Not active until Primary Mass Conservation plan closes; created to satisfy Article V Section 7 deferral rule.**

---

## Purpose

Add structured event logging for physics regime transitions. Currently, plant mode transitions are logged but regime transitions (Isolated/Blended/Coupled, driven by RCP alpha) are not. This makes debugging mass behavior across regime boundaries dependent on inferring regime from RCP count rather than seeing explicit transition events.

---

## Architectural Domain Definition

| Field | Value |
|-------|-------|
| System Area | HeatupSimEngine â€” Regime Selection / Event Logging |
| Discipline | Observability |
| Architectural Boundary | Regime transition detection, event log output, diagnostic context enrichment |

This domain is distinct from all other domains. It adds logging only â€” no physics modifications, no conservation changes, no UI changes.

---

## Issues Assigned

| Issue ID | Title | Severity | Deferral Reason |
|----------|-------|----------|-----------------|
| CS-0012 | No regime transition logging | Low | Outside Primary Mass Conservation domain (observability, not primary mass conservation) |

**Note:** CS-0037 (surge flow direction display) was previously linked to this plan but has been **subsumed by CS-0042** (Professional Interactive Dashboard Visualization Upgrade) and reassigned to IP-0013 â€” Operator and Scenario Framework.

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
- Add mass continuity assertions at transitions

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| Primary Mass Conservation plan must close first | Prerequisite | Regime transition logging is most valuable when canonical mass is active â€” the log entries can include ledger values for debugging |
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
| Logging only â€” no structural change | Does not prevent regime transition mass discontinuities; only makes them visible |
| No backward transition detection | Does not guard against invalid regime regression (e.g., Coupled back to Isolated); deferred to architecture hardening |
| Plan is a placeholder | No implementation stages defined; will be elaborated when plan becomes active |

