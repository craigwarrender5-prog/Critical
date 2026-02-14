> LEGACY IMPLEMENTATION ARTIFACT — archived during Constitution v1.0 governance re-baseline.
> Historical record preserved. No current execution authority.

---
Identifier: IP-0008
Domain: CVCS â€” Energy Balance and VCT Flow Boundary Accounting
Status: Open
Priority: Medium
Tier: 3
Linked Issues: CS-0035, CS-0039
Last Reviewed: 2026-02-14
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# IP-0008 â€” CVCS Energy Balance and VCT Flow Accounting
## CVCS Energy Balance and VCT Flow Boundary Accounting

---

## Document Control

| Field | Value |
|-------|-------|
| Identifier | **IP-0008** |
| Plan Severity | **High** |
| Architectural Domain | CVCS â€” Energy Balance and VCT Flow Boundary Accounting |
| System Area | HeatupSimEngine.CVCS, VCTPhysics, CVCSController, RCSHeatup, HeatTransfer |
| Discipline | CVCS â€” Enthalpy Transport and Flow Accounting |
| Status | **PLACEHOLDER â€” Requires scoping and authorization** |
| Constitution Reference | PROJECT_CONSTITUTION.md, Articles V, III, XII |
| Phase | Phase 0 residual (CS-0039 High) / Phase 2 overlap (CS-0035 Low) |

**Not active until Phase 0 critical blockers are resolved and authorization is granted.**

---

## Purpose

Address two related defects in CVCS energy and flow accounting:

1. **CS-0035 â€” CVCS thermal mixing missing:** Charging water enters the RCS at VCT temperature (~100Â°F) but is modeled as mass-only with no thermal mixing. At 75 gpm charging with a Î”T of ~300Â°F, this represents approximately 0.15 MW of cooling currently ignored. The omission contributes to a slight overestimation of RCS heatup rate during CVCS operations.

2. **CS-0039 â€” VCT conservation error growth (~1,700 gal):** Dashboard shows VCT CONS ERR monotonically growing to ~1,600â€“1,700 gal by 15.75 hr. The verifier depends on the RCS-side `rcsInventoryChange_gal` term. If primary-side accounting drifts (mass-derived volume vs geometric volume mismatch), VCT verification fails even with correct VCT flows, creating cascading validation failures.

Both issues involve the boundary between CVCS/VCT flow accounting and RCS-side state.

---

## Architectural Domain Definition

| Field | Value |
|-------|-------|
| System Area | HeatupSimEngine.CVCS, VCTPhysics, CVCSController, RCSHeatup, HeatTransfer |
| Discipline | CVCS â€” Enthalpy Transport, Flow Boundary Accounting |
| Architectural Boundary | CVCS enthalpy transport to/from RCS; VCT flow verification equation; seal flow accounting |

This domain is distinct from:
- Primary Mass Conservation (CS-0001â€“CS-0008): Mass ledger architecture (resolved v0.1.0.0)
- RCS Energy Balance (CS-0034, CS-0038): RCS-side equilibrium and regime transitions
- Validation / Diagnostic Display (CS-0041): Display metric type mismatch (related root cause for CS-0039 but different fix scope)
- SG Secondary Physics (CS-0014â€“CS-0020): Separate subsystem

---

## Issues Assigned

| Issue ID | Title | Severity | Notes |
|----------|-------|----------|-------|
| CS-0035 | CVCS thermal mixing contribution missing from RCS energy balance | Low | Originated from CS-0033 Finding C. ~0.15 MW cooling omission at 75 gpm, Î”T=300Â°F. |
| CS-0039 | VCT conservation error growth (~1,700 gal at 15.75 hr) | High | Monotonically growing error; boundary accounting mismatch between RCS and VCT subsystems. |

**Plan Severity:** High (highest assigned issue = CS-0039 High)

---

## Scope

This plan shall:

- Diagnose the root cause of VCT conservation error growth (CS-0039): audit `rcsInventoryChange_gal` derivation, seal flow accounting (3 gpm/pump), and flow integration timestep sensitivity
- Determine whether VCT verification should use mass-based comparison (lbm) instead of volume-based (gal) â€” cross-reference with CS-0041 findings
- Add CVCS enthalpy transport to the RCS energy balance (CS-0035): compute `Q_cvcs = mdot_charging Ã— Cp Ã— (T_rcs - T_charging)` and apply to RCS dT/dt
- Reconcile VCT and RCS boundary flow accounting to eliminate systematic drift

This plan shall NOT:

- Modify the canonical mass ledger architecture
- Change VCT mass flow rates or controller logic (only the verification/accounting)
- Modify SG physics or PZR physics
- Add new CVCS flow paths (excess letdown, etc. â€” separate scope v5.6.7.0)

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| CS-0041 findings | Informational | CS-0039 and CS-0041 may share the root cause of mass-derived vs geometric volume comparison. CS-0041 fix may inform VCT verifier correction. |
| CS-0032 (Performance/UI blocker) resolved or mitigated | Prerequisite | Long-run validation requires stable runtime |
| Canonical mass ledger active (CS-0001) | Satisfied | Resolved v0.1.0.0 |

---

## Exit Criteria (Skeleton)

- [ ] CS-0039: VCT conservation error < 10 gal steady-state over multi-hour runs
- [ ] CS-0039: No monotonic error growth â€” error must be bounded
- [ ] CS-0039: VCT and RCS boundary flow accounting reconciled
- [ ] CS-0035: CVCS enthalpy transport applied to RCS energy balance
- [ ] CS-0035: Charging temperature contribution (~0.15 MW at 75 gpm, Î”T=300Â°F) visible in heat balance diagnostic
- [ ] No regressions in existing conservation tests
- [ ] Issue Registry updated, Changelog written, version increment performed

---

## Known Limitations

| Limitation | Impact |
|-----------|--------|
| CVCS thermal mixing magnitude is small (~0.15 MW vs ~1 MW total) | May not produce observable heatup rate change at current timestep resolution |
| VCT verifier root cause may require CS-0041 to be resolved first | If the root cause is mass-derived vs geometric volume comparison, the VCT verifier fix depends on the same architectural decision |
| Plan is a placeholder | No implementation stages defined; will be elaborated when plan becomes active |

