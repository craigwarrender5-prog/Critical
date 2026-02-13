# IMPLEMENTATION PLAN v0.2.0.0
## Steam Generator Energy and Pressure Validation

---

## Document Control

| Field | Value |
|-------|-------|
| Version | v0.2.0.0 |
| Plan Severity | **Medium** |
| Architectural Domain | Steam Generator Secondary Side — Energy Balance / Pressure Boundary |
| System Area | SGMultiNodeThermal, AlarmManager, HeatupSimEngine (SG interface) |
| Discipline | Energy Conservation, Plant Protection |
| Status | **PLACEHOLDER — Not active until v0.1.0.0 closes** |
| Constitution Reference | PROJECT_CONSTITUTION.md v0.1.0.0, Articles V, III |
| Deferral Authority | Article V Section 5 — issues deferred from v0.1.0.0 scope (different architectural domain) |

**Not active until v0.1.0.0 closes; created to satisfy Article V Section 5 deferral rule.**

---

## Purpose

Establish runtime validation of the steam generator secondary side. Currently, the SG model (`SGMultiNodeThermal`) outputs heat removal and secondary pressure values that are consumed by the primary-side solver without any cross-check. This plan adds energy balance validation and pressure protection to the SG boundary.

---

## Architectural Domain Definition

| Field | Value |
|-------|-------|
| System Area | Steam Generator — Secondary Side |
| Discipline | Energy Conservation, Plant Protection |
| Architectural Boundary | SG heat removal output (`TotalHeatRemoval_MW`), SG secondary pressure (`sgSecondaryPressure_psia`), SG secondary mass inventory |

This domain is distinct from the Primary Mass Conservation domain (v0.1.0.0). No overlap in modified files except the alarm system (AlarmManager.cs) which is a shared service.

---

## Issues Assigned

| Issue ID | Title | Severity | Deferral Reason |
|----------|-------|----------|-----------------|
| CS-0009 | No SG secondary energy balance validation | Medium | Outside v0.1.0.0 domain (SG boundary, not primary mass conservation) |
| CS-0010 | No SG secondary pressure alarm | Low | Outside v0.1.0.0 domain (SG plant protection, not primary mass conservation) |

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
- Modify primary-side conservation logic (v0.1.0.0 domain)

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| v0.1.0.0 must close first | Prerequisite | Primary mass conservation must be established before adding SG energy cross-checks that reference primary temperature changes |
| AlarmManager shared service | Coordination | Adding SG pressure alarm uses the same AlarmManager infrastructure; no conflict expected |

---

## Exit Criteria (Skeleton)

- [ ] CS-0009: Runtime WARNING fires if SG heat removal exceeds 2x gross heat input or goes negative
- [ ] CS-0010: Alarm fires when SG secondary pressure exceeds 1085 psia
- [ ] Cumulative energy balance check logs WARNING if mismatch exceeds 5% over simulation
- [ ] No regression in primary-side conservation metrics (v0.1.0.0 verification scenarios still pass)
- [ ] Issue Registry updated, Changelog written, version increment performed

---

## Known Limitations

| Limitation | Impact |
|-----------|--------|
| SG secondary mass conservation not addressed | Only energy and pressure validated; mass tracking deferred |
| SG tube fouling / degradation not modeled | Energy balance check assumes clean tube conditions |
| Plan is a placeholder | No implementation stages defined; will be elaborated when plan becomes active |
