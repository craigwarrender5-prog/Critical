> LEGACY IMPLEMENTATION ARTIFACT — archived during Constitution v1.0 governance re-baseline.
> Historical record preserved. No current execution authority.

---
Identifier: IP-0007
Domain: Steam Generator Secondary Side â€” Energy Balance / Pressure Boundary
Status: Open
Priority: Medium
Tier: 3
Linked Issues: CS-0009, CS-0010
Last Reviewed: 2026-02-14
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# IP-0007 â€” SG Energy and Pressure Validation
## Steam Generator Energy and Pressure Validation

---

## Document Control

| Field | Value |
|-------|-------|
| Identifier | **IP-0007** |
| Plan Severity | **Medium** |
| Architectural Domain | Steam Generator Secondary Side â€” Energy Balance / Pressure Boundary |
| System Area | SGMultiNodeThermal, AlarmManager, HeatupSimEngine (SG interface) |
| Discipline | Energy Conservation, Plant Protection |
| Status | **PLACEHOLDER â€” Requires scoping and authorization** |
| Constitution Reference | PROJECT_CONSTITUTION.md, Articles V, III |
| Deferral Authority | Article V Section 7 â€” issues deferred from Primary Mass Conservation plan scope (different architectural domain) |

**Not active until Primary Mass Conservation plan closes; created to satisfy Article V Section 7 deferral rule.**

---

## Purpose

Establish runtime validation of the steam generator secondary side. Currently, the SG model (`SGMultiNodeThermal`) outputs heat removal and secondary pressure values that are consumed by the primary-side solver without any cross-check. This plan adds energy balance validation and pressure protection to the SG boundary.

---

## Architectural Domain Definition

| Field | Value |
|-------|-------|
| System Area | Steam Generator â€” Secondary Side |
| Discipline | Energy Conservation, Plant Protection |
| Architectural Boundary | SG heat removal output (`TotalHeatRemoval_MW`), SG secondary pressure (`sgSecondaryPressure_psia`), SG secondary mass inventory |

This domain is distinct from the Primary Mass Conservation domain. No overlap in modified files except the alarm system (AlarmManager.cs) which is a shared service.

---

## Issues Assigned

| Issue ID | Title | Severity | Deferral Reason |
|----------|-------|----------|-----------------|
| CS-0009 | No SG secondary energy balance validation | Medium | Outside Primary Mass Conservation domain (SG boundary, not primary mass conservation) |
| CS-0010 | No SG secondary pressure alarm | Low | Outside Primary Mass Conservation domain (SG plant protection, not primary mass conservation) |

**Plan Severity:** Medium (highest assigned issue = CS-0009 Medium)

---

## Scope

This plan shall:

- Add runtime bounds checking on SG heat removal output
- Add SG secondary pressure alarm at safety valve setpoint (~1085 psia)
- Add cumulative energy balance cross-check (SG heat removed vs primary energy change)

This plan shall NOT:

- Rewrite the SGMultiNodeThermal physics model
- Modify SG tube heat transfer correlations
- Add SG secondary mass conservation (deferred to future SG fidelity plan)
- Modify primary-side conservation logic

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| Primary Mass Conservation plan must close first | Prerequisite | Primary mass conservation must be established before adding SG energy cross-checks that reference primary temperature changes |
| AlarmManager shared service | Coordination | Adding SG pressure alarm uses the same AlarmManager infrastructure; no conflict expected |

---

## Exit Criteria (Skeleton)

- [ ] CS-0009: Runtime WARNING fires if SG heat removal exceeds 2x gross heat input or goes negative
- [ ] CS-0010: Alarm fires when SG secondary pressure exceeds 1085 psia
- [ ] Cumulative energy balance check logs WARNING if mismatch exceeds 5% over simulation
- [ ] No regression in primary-side conservation metrics
- [ ] Issue Registry updated, Changelog written, version increment performed

---

## Known Limitations

| Limitation | Impact |
|-----------|--------|
| SG secondary mass conservation not addressed | Only energy and pressure validated; mass tracking deferred |
| SG tube fouling / degradation not modeled | Energy balance check assumes clean tube conditions |
| Plan is a placeholder | No implementation stages defined; will be elaborated when plan becomes active |

