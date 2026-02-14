> LEGACY IMPLEMENTATION ARTIFACT — archived during Constitution v1.0 governance re-baseline.
> Historical record preserved. No current execution authority.

---
Identifier: IP-0010
Domain: Validation Infrastructure â€” Runtime Test Harness
Status: Open
Priority: High
Tier: 3
Linked Issues: CS-0011
Last Reviewed: 2026-02-14
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# IP-0010 â€” Test Infrastructure
## Runtime Acceptance Test Infrastructure

---

## Document Control

| Field | Value |
|-------|-------|
| Identifier | **IP-0010** |
| Plan Severity | **High** |
| Architectural Domain | Validation Infrastructure â€” Runtime Test Harness |
| System Area | AcceptanceTests, HeatupSimEngine (test point injection) |
| Discipline | Validation Infrastructure |
| Status | **PLACEHOLDER â€” Requires scoping and authorization** |
| Constitution Reference | PROJECT_CONSTITUTION.md, Articles V, III |
| Deferral Authority | Article V Section 7 â€” issue deferred from Primary Mass Conservation plan scope (different architectural domain) |

**Not active until Primary Mass Conservation plan closes; created to satisfy Article V Section 7 deferral rule.**

---

## Purpose

Transform the acceptance test suite from formula-only validation to runtime simulation validation. Currently, all 10 acceptance tests pass by checking mathematical correctness without running the simulator. This plan adds runtime test points that evaluate acceptance criteria against live simulation data.

---

## Architectural Domain Definition

| Field | Value |
|-------|-------|
| System Area | Acceptance Tests / Validation Infrastructure |
| Discipline | Validation Infrastructure |
| Architectural Boundary | Test harness injection points in simulation loop, post-run acceptance evaluation, test result reporting |

This domain is distinct from both the Primary Mass Conservation domain and the SG domain. It reads simulation state but does not modify physics.

---

## Issues Assigned

| Issue ID | Title | Severity | Deferral Reason |
|----------|-------|----------|-----------------|
| CS-0011 | Acceptance tests are formula-only, not simulation-validated | High | Outside Primary Mass Conservation domain (test infrastructure, not primary mass conservation) |

**Plan Severity:** High (highest assigned issue = CS-0011 High)

---

## Scope

This plan shall:

- Add runtime test point recording at simulation milestones (bubble formation, regime transitions, end-of-heatup)
- Implement runtime evaluation for at least AT-02 (conservation drift), AT-03 (transition mass continuity), AT-08 (RCP PZR level spike)
- Report PASS/FAIL from actual simulation measurements, not formula validation
- Update the quality gate to require simulation-validated results

This plan shall NOT:

- Remove the existing formula-only tests (they remain as design validation)
- Build a full automated regression harness (deferred to architecture hardening)
- Modify physics or conservation logic
- Add new acceptance criteria beyond the existing 10

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| Primary Mass Conservation plan must close first | Prerequisite | Canonical mass enforcement must be active for conservation tests (AT-02, AT-03) to produce meaningful results |
| Runtime diagnostics from Primary Mass Conservation plan | Input | The resurrected `UpdatePrimaryMassLedgerDiagnostics()` provides the drift metrics that AT-02 will evaluate |

---

## Exit Criteria (Skeleton)

- [ ] CS-0011: At least AT-02, AT-03, AT-08 produce PASS/FAIL from live simulation data
- [ ] Quality gate updated: "X of 10 acceptance tests pass with SIMULATION data"
- [ ] Existing formula-only tests still run and pass (no regression)
- [ ] Test results are machine-readable and logged
- [ ] Issue Registry updated, Changelog written, version increment performed

---

## Known Limitations

| Limitation | Impact |
|-----------|--------|
| Only 3 of 10 tests targeted for runtime conversion | Remaining 7 tests stay formula-only until further plan |
| No automated regression harness | Tests require manual simulation run; automation deferred |
| Plan is a placeholder | No implementation stages defined; will be elaborated when plan becomes active |

