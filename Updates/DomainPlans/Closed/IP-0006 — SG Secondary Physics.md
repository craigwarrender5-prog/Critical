> LEGACY IMPLEMENTATION ARTIFACT — archived during Constitution v1.0 governance re-baseline.
> Historical record preserved. No current execution authority.

---
Identifier: IP-0006
Domain: Steam Generator Secondary Side â€” Thermodynamic Model / Pressure Boundary / Startup Sequencing
Status: Open
Priority: High
Tier: 2
Linked Issues: CS-0014, CS-0015, CS-0016, CS-0017, CS-0018, CS-0019, CS-0020
Last Reviewed: 2026-02-14
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# IP-0006 â€” SG Secondary Physics
## Steam Generator Secondary Side Physics Rewrite

---

## Document Control

| Field | Value |
|-------|-------|
| Identifier | **IP-0006** |
| Plan Severity | **Critical** |
| Architectural Domain | Steam Generator Secondary Side â€” Thermodynamic Model / Pressure Boundary / Startup Sequencing |
| System Area | SGMultiNodeThermal, HeatupSimEngine (SG interface), PlantConstants.SG |
| Discipline | SG Secondary Physics, Plant Startup Procedure |
| Status | **PLACEHOLDER â€” Requires scoping and authorization** |
| Constitution Reference | PROJECT_CONSTITUTION.md, Articles V, III |

---

## Purpose

Rewrite the steam generator secondary side physics model to correctly represent a sealed, pressurizable secondary system during plant heatup. The current model treats the SG secondary as an effectively open/vented system with no meaningful pressure accumulation, which prevents the secondary from reaching operating conditions and causes it to act as an unrealistically dominant heat sink.

This plan addresses the fundamental SG physics model â€” not just validation. The secondary must be modeled as a sealed compressible volume with nitrogen blanket, steam accumulation, and pressure-temperature coupling.

---

## Architectural Domain Definition

| Field | Value |
|-------|-------|
| System Area | Steam Generator â€” Secondary Side Physics Model |
| Discipline | SG Secondary Thermodynamics, Compressible Volume, Startup Procedure |
| Architectural Boundary | SGMultiNodeThermal physics model, SG secondary pressure/temperature coupling, SG startup state machine, N2 blanket model |

This domain is distinct from:
- Primary Mass Conservation (resolved)
- SG Energy Balance Validation (adds checks to existing model)
- Test Infrastructure
- Observability

The SG Energy Balance Validation plan adds validation to the SG model; this plan rewrites the model itself.

---

## Issues Assigned

| Issue ID | Title | Severity | Source |
|----------|-------|----------|--------|
| CS-0014 | SG "ISOLATED" mode behaves like open inventory pressure boundary | Critical | ISSUE-SG-001 |
| CS-0015 | Steam generation does not accumulate compressible volume/mass | Critical | ISSUE-SG-002 |
| CS-0016 | SG modeled as unrealistically strong heat sink during heatup | Critical | ISSUE-SG-003 |
| CS-0017 | Missing SG pressurization/hold state in startup procedure | High | ISSUE-SG-004 |
| CS-0018 | N2 blanket treated as pressure clamp, not compressible cushion | High | ISSUE-SG-005 |
| CS-0019 | Secondary temperature cannot progress toward high-pressure saturation region | High | ISSUE-SG-006 |
| CS-0020 | Secondary remains largely inert or wrongly bounded during primary heatup | Medium | ISSUE-LOG-005/010/015 |

**Plan Severity:** Critical (highest assigned issue = CS-0014, CS-0015, CS-0016 Critical)

---

## Scope

This plan shall:

- Implement sealed SG secondary volume model with steam accumulation and pressure rise
- Replace the N2 blanket pressure clamp with a compressible gas model (PV=nRT)
- Add SG_SECONDARY_PRESSURIZE_HOLD startup state
- Correct SG heat sink magnitude to be physically bounded
- Enable secondary temperature progression toward high-pressure saturation region
- Ensure secondary conditions evolve realistically during primary heatup

This plan shall NOT:

- Modify primary-side conservation logic
- Add SG energy balance validation checks (separate plan domain)
- Add SG pressure alarms (separate plan domain)
- Modify test infrastructure

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| Primary Mass Conservation plan must be closed | Prerequisite | Primary mass conservation must be stable before SG physics changes |
| SG Energy Balance Validation plan may be superseded | Coordination | SG validation plan may need revision after model rewrite |

---

## Exit Criteria (Skeleton)

- [ ] CS-0014: SG secondary behaves as sealed pressure boundary in ISOLATED mode
- [ ] CS-0015: Steam generated accumulates in compressible volume, building pressure
- [ ] CS-0016: SG heat removal bounded by physical constraints; does not dominate heat balance
- [ ] CS-0017: SG startup includes PRESSURIZE_HOLD state with sealed warmup
- [ ] CS-0018: N2 blanket modeled as compressible gas (PV=nRT), not pressure clamp
- [ ] CS-0019: Secondary temperature can progress to high-pressure saturation (~550 F region)
- [ ] CS-0020: Secondary conditions evolve realistically during primary heatup/RCP operation
- [ ] No regression in primary-side conservation metrics
- [ ] Issue Registry updated, Changelog written, version increment performed

---

## Known Limitations

| Limitation | Impact |
|-----------|--------|
| Plan is a placeholder | No implementation stages defined; will be elaborated when plan becomes active |
| SG secondary mass conservation not addressed | Mass tracking deferred; focus is on thermodynamic model |
| May require SG Energy Balance Validation plan revision | SG validation targets may change after model rewrite |

