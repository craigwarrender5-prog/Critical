> LEGACY IMPLEMENTATION ARTIFACT — archived during Constitution v1.0 governance re-baseline.
> Historical record preserved. No current execution authority.

---
Identifier: IP-0011
Domain: Validation / Diagnostic Display â€” Conservation Metric Integrity
Status: Open
Priority: Medium
Tier: 3
Linked Issues: CS-0041
Last Reviewed: 2026-02-14
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# IP-0011 â€” Validation and Diagnostic Display
## Validation and Diagnostic Display â€” Conservation Metric Integrity

---

## Document Control

| Field | Value |
|-------|-------|
| Identifier | **IP-0011** |
| Plan Severity | **Medium** |
| Architectural Domain | Validation / Diagnostic Display â€” Conservation Metric Integrity |
| System Area | HeatupSimEngine, HeatupSimEngine.Init, HeatupSimEngine.CVCS, HeatupValidationVisual.Panels |
| Discipline | Validation Display â€” Metric Correctness |
| Status | **PLACEHOLDER â€” Requires scoping and authorization** |
| Constitution Reference | PROJECT_CONSTITUTION.md, Articles V, III, XII |
| Phase | Phase 0 residual |

**Not active until Phase 0 critical blockers are resolved and authorization is granted.**

---

## Purpose

Fix the inventory audit baseline type mismatch that produces phantom conservation "errors" of ~13,000+ gallons during heatup. The defect is in the display metric, not in the physics.

**CS-0041 â€” Inventory audit baseline type mismatch:** The dashboard compares `initialSystemInventory_gal` (geometric volume at t=0, temperature-independent) against `totalSystemInventory_gal` (mass-derived volume at runtime, temperature-dependent). As water heats from 100Â°F to 550Â°F, density decreases ~15%, producing a phantom "error" that is purely thermal expansion â€” not a conservation failure. This undermines operator confidence and produces false FAIL indications in the validation tab.

---

## Architectural Domain Definition

| Field | Value |
|-------|-------|
| System Area | HeatupSimEngine, HeatupSimEngine.Init, HeatupSimEngine.CVCS, HeatupValidationVisual.Panels |
| Discipline | Validation Display â€” Metric Correctness |
| Architectural Boundary | Inventory conservation metric computation and display; validation tab PASS/FAIL logic |

This domain is distinct from:
- Primary Mass Conservation (CS-0001â€“CS-0008): Canonical mass ledger (resolved v0.1.0.0, unaffected by this defect)
- CVCS Energy Balance (CS-0035, CS-0039): VCT flow accounting (related â€” CS-0039 may share root cause)
- Observability (CS-0012, CS-0037): Event logging and flow direction display
- Dashboard Modernization (CS-0042): Visual overhaul (Phase 1, separate scope)

---

## Issues Assigned

| Issue ID | Title | Severity | Notes |
|----------|-------|----------|-------|
| CS-0041 | Inventory audit baseline type mismatch (geometric vs mass-derived gallons) | Medium | Phantom ~13,000 gal error from thermal expansion; display metric defect, not physics defect. |

**Plan Severity:** Medium (highest assigned issue = CS-0041 Medium)

---

## Scope

This plan shall:

- Replace volume-based inventory conservation metric with mass-based metric (lbm)
- Store `initialSystemMass_lbm` at initialization (total primary mass at t=0)
- Compute `totalSystemMass_lbm` at runtime from component mass sum
- Define mass error: `massError_lbm = |totalMass - initialMass - externalNetMass|`
- Update `DrawInventoryPanel()` to display mass-based conservation
- Update validation tab PASS/FAIL logic to use mass-based metric
- Optionally display equivalent volume at reference density for operator familiarity
- Retain geometric gallons only for capacity/level display (never for conservation checks)

This plan shall NOT:

- Modify the canonical mass ledger architecture
- Change physics equations, conservation logic, or state computation
- Modify regime selection or bubble formation logic
- Add new UI components (that is CS-0042 scope)

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| Canonical mass ledger active (CS-0001) | Satisfied | Resolved v0.1.0.0 |
| CS-0039 findings | Informational | VCT conservation error may share the same geometric-vs-mass-derived comparison root cause. This fix may inform CS-0039 VCT verifier correction. |

---

## Exit Criteria (Skeleton)

- [ ] CS-0041: During heatup with density changes, dashboard inventory error remains bounded (~â‰¤1 lbm)
- [ ] CS-0041: No large gallon swings from thermal expansion appear as errors
- [ ] CS-0041: Conservation metric uses canonical mass ledger (`TotalPrimaryMass_lb`)
- [ ] CS-0041: No false FAIL indications in validation tab due to thermal expansion
- [ ] CS-0041: Inventory Audit status matches dashboard display (no mismatch)
- [ ] No regressions in existing conservation tests
- [ ] Issue Registry updated, Changelog written, version increment performed

---

## Known Limitations

| Limitation | Impact |
|-----------|--------|
| Operator familiarity with gallon-based metrics | Operators may expect inventory in gallons; optional reference-density volume display mitigates this |
| CS-0039 cross-dependency | VCT verifier may need the same geometric-to-mass conversion; coordinate with CVCS Energy Balance plan |
| Plan is a placeholder | No implementation stages defined; will be elaborated when plan becomes active |

