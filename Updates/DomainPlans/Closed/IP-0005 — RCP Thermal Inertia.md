> LEGACY IMPLEMENTATION ARTIFACT — archived during Constitution v1.0 governance re-baseline.
> Historical record preserved. No current execution authority.

---
Identifier: IP-0005
Domain: Primary Side â€” RCP Heat Input / Thermal Mass Coupling
Status: Open
Priority: Medium
Tier: 3
Linked Issues: CS-0031
Last Reviewed: 2026-02-14
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# IP-0005 â€” RCP Thermal Inertia
## RCP Thermal Inertia and Heat Rate Validation

---

## Document Control

| Field | Value |
|-------|-------|
| Identifier | **IP-0005** |
| Plan Severity | **Medium** |
| Architectural Domain | Primary Side â€” RCP Heat Input / Thermal Mass Coupling |
| System Area | RCSHeatup, HeatupSimEngine (RCP regime transition), PlantConstants |
| Discipline | Primary Thermodynamics â€” Thermal Inertia |
| Status | **PLACEHOLDER â€” Requires scoping and authorization** |
| Constitution Reference | PROJECT_CONSTITUTION.md, Articles V, III |

---

## Purpose

Validate and correct the RCS heat rate response to RCP startup. The current model shows a potentially aggressive heatup rate escalation when RCP heat comes online, which may indicate missing thermal mass, incorrect Cp values, or pump heat being applied to too small an inventory fraction.

---

## Architectural Domain Definition

| Field | Value |
|-------|-------|
| System Area | Primary Side â€” RCP Heat Input / Thermal Mass |
| Discipline | RCP Thermodynamics, Thermal Inertia Validation |
| Architectural Boundary | RCP heat injection, RCS heat capacity computation, metal thermal mass inclusion |

This domain is distinct from all other domains. It addresses only the magnitude of RCP thermal effects.

---

## Issues Assigned

| Issue ID | Title | Severity | Source |
|----------|-------|----------|--------|
| CS-0031 | RCS heat rate escalation after RCP start may be numerically aggressive | Medium | ISSUE-LOG-014 |

**Plan Severity:** Medium (highest assigned issue = CS-0031 Medium)

---

## Scope

This plan shall:

- Validate RCS thermal mass (Cp x mass) against reference data
- Verify RCP heat input magnitude and distribution
- Ensure heatup rate with 4 RCPs matches NRC reference (~50 F/hr)

This plan shall NOT:

- Modify RCP startup sequencing logic
- Modify canonical mass conservation (resolved)
- Rewrite SG model (separate domain)

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| Primary Mass Conservation plan must be closed | Prerequisite | Canonical mass enforcement must be active |

---

## Exit Criteria (Skeleton)

- [ ] CS-0031: RCS heatup rate with 4 RCPs is within 30-80 F/hr range per NRC HRTD reference
- [ ] Thermal mass computation accounts for metal mass, water mass, and Cp temperature dependence
- [ ] No regression in primary-side conservation metrics
- [ ] Issue Registry updated, Changelog written, version increment performed

---

## Known Limitations

| Limitation | Impact |
|-----------|--------|
| Plan is a placeholder | No implementation stages defined; will be elaborated when plan becomes active |
| May be validation-only | Current rate may be correct; needs detailed comparison against reference |

