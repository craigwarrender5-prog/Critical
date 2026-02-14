> LEGACY IMPLEMENTATION ARTIFACT — archived during Constitution v1.0 governance re-baseline.
> Historical record preserved. No current execution authority.

---
Identifier: IP-0004
Domain: Core Physics â€” RCS Energy Balance / Regime Transition Stability
Status: Open
Priority: Medium
Tier: 3
Linked Issues: CS-0034, CS-0038
Last Reviewed: 2026-02-14
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# IP-0004 â€” RCS Energy Balance and Regime Transition
## RCS Energy Balance and Regime Transition Stability

---

## Document Control

| Field | Value |
|-------|-------|
| Identifier | **IP-0004** |
| Plan Severity | **Medium** |
| Architectural Domain | Core Physics â€” RCS Energy Balance / Regime Transition Stability |
| System Area | RCSHeatup, SolidPlantPressure, CoupledThermo, PressurizerPhysics, HeatupSimEngine |
| Discipline | Primary Thermodynamics â€” Energy Balance and Regime Transition |
| Status | **PLACEHOLDER â€” Requires scoping and authorization** |
| Constitution Reference | PROJECT_CONSTITUTION.md, Articles V, III, XII |
| Phase | Phase 0 residual (CS-0034 Medium, CS-0038 High) |

**Not active until Phase 0 critical blockers are resolved and authorization is granted.**

---

## Purpose

Address two related defects in RCS energy balance and regime transition behavior:

1. **CS-0034 â€” No equilibrium ceiling in Regime 0/1:** T_rcs rises indefinitely via surge line conduction minus weak insulation loss. No mechanism caps T_rcs below T_pzr. In reality, T_rcs cannot exceed Tsat(P) âˆ’ Î”T driven by the surge line UA. The insulation loss model is correct but too weak at low temperatures to counterbalance PZR heater power conducted via surge line.

2. **CS-0038 â€” PZR level spike on RCP start:** Sharp single-frame transient in PZR level when RCPs start. Likely caused by one-frame canonical mass overwrite, density ordering mismatch (pre-solver vs post-solver), or thermal expansion double-counting at the regime boundary between IsolatedHeatingStep and BulkHeatupStep.

Both issues involve the integrity of RCS energy and mass accounting during regime transitions and pre-RCP operation.

---

## Architectural Domain Definition

| Field | Value |
|-------|-------|
| System Area | RCSHeatup, SolidPlantPressure, CoupledThermo, PressurizerPhysics, HeatupSimEngine |
| Discipline | Core Physics â€” Primary Energy Balance, Regime Transition |
| Architectural Boundary | RCS dT/dt energy balance in Regime 0/1; state handoff at regime transitions (Regime 1 â†’ Regime 2) |

This domain is distinct from:
- SG Secondary Physics (CS-0014â€“CS-0020): SG heat sink is absent in Regime 0/1 by design
- Bubble Formation and Two-Phase (CS-0026â€“CS-0030, CS-0036): PZR two-phase behavior during drain/stabilize
- CVCS Energy Balance (CS-0035): Charging enthalpy transport (related but separate scope)

---

## Issues Assigned

| Issue ID | Title | Severity | Notes |
|----------|-------|----------|-------|
| CS-0034 | No equilibrium ceiling for RCS temperature in Regime 0/1 | Medium | Originated from CS-0033 Finding B. T_rcs has no upper bound without SG heat sink or stronger ambient loss model. |
| CS-0038 | PZR level spike on RCP start (single-frame transient) | High | Single-frame discontinuity at regime transition (IsolatedHeatingStep â†’ BulkHeatupStep). |

**Plan Severity:** High (highest assigned issue = CS-0038 High)

---

## Scope

This plan shall:

- Diagnose and fix the PZR level spike at RCP start (CS-0038)
- Audit state handoff between IsolatedHeatingStep and BulkHeatupStep for mass/energy continuity
- Implement an equilibrium ceiling mechanism for Regime 0/1 (CS-0034)
- Verify that dT_rcs/dt â†’ 0 as T_rcs approaches equilibrium with no forced flow

This plan shall NOT:

- Modify SG secondary physics (separate domain: CS-0014â€“CS-0020)
- Add CVCS thermal mixing (separate domain: CS-0035)
- Modify bubble formation or drain logic (separate domain: CS-0026â€“CS-0030, CS-0036)
- Change canonical mass ledger architecture

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| CS-0032 (Performance/UI blocker) resolved or mitigated | Prerequisite | Long-run validation requires stable runtime |
| Phase 0 critical items (CS-0032) | Prerequisite | Phase 0 exit criteria must be satisfied for regime transition validation |
| CS-0033 resolved | Satisfied | Finding A resolved v0.3.1.1; Findings B/C spun out to CS-0034/CS-0035 |

---

## Exit Criteria (Skeleton)

- [ ] CS-0038: No PZR level spike > 0.5% per timestep on RCP start
- [ ] CS-0038: Mass conservation remains within tolerance through regime 1 â†’ 2 transition
- [ ] CS-0034: In Regime 0/1 with constant heat input and no forced flow, T_rcs asymptotically approaches equilibrium (dT/dt â†’ 0)
- [ ] CS-0034: Equilibrium temperature is bounded by Tsat(P) âˆ’ Î”T_surge_UA
- [ ] No regressions in existing conservation tests
- [ ] Issue Registry updated, Changelog written, version increment performed

---

## Known Limitations

| Limitation | Impact |
|-----------|--------|
| Equilibrium ceiling is approximate without SG heat sink | True equilibrium in Regime 0/1 depends on ambient losses only; SG heat sink implementation (CS-0014â€“CS-0020) would provide a more realistic equilibrium mechanism |
| RCP start spike diagnosis may reveal architecture-level issues | If the root cause is V*rho intermediate estimate (deferred to v5.7.0.0 architecture hardening), a full fix may require the solver partition redesign |
| Plan is a placeholder | No implementation stages defined; will be elaborated when plan becomes active |

