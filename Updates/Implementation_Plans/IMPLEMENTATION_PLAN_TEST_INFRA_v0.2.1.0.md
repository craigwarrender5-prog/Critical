# IMPLEMENTATION PLAN v0.2.1.0
## Runtime Acceptance Test Infrastructure

---

## Document Control

| Field | Value |
|-------|-------|
| Version | v0.2.1.0 |
| Plan Severity | **High** |
| Architectural Domain | Validation Infrastructure — Runtime Test Harness |
| System Area | AcceptanceTests, HeatupSimEngine (test point injection) |
| Discipline | Validation Infrastructure |
| Status | **PLACEHOLDER — Not active until v0.1.0.0 closes** |
| Constitution Reference | PROJECT_CONSTITUTION.md v0.1.0.0, Articles V, III |
| Deferral Authority | Article V Section 5 — issue deferred from v0.1.0.0 scope (different architectural domain) |

**Not active until v0.1.0.0 closes; created to satisfy Article V Section 5 deferral rule.**

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

This domain is distinct from both the Primary Mass Conservation domain (v0.1.0.0) and the SG domain (v0.2.0.0). It reads simulation state but does not modify physics.

---

## Issues Assigned

| Issue ID | Title | Severity | Deferral Reason |
|----------|-------|----------|-----------------|
| CS-0011 | Acceptance tests are formula-only, not simulation-validated | High | Outside v0.1.0.0 domain (test infrastructure, not primary mass conservation) |

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
| v0.1.0.0 must close first | Prerequisite | Canonical mass enforcement must be active for conservation tests (AT-02, AT-03) to produce meaningful results |
| Runtime diagnostics from v0.1.0.0 | Input | The resurrected `UpdatePrimaryMassLedgerDiagnostics()` provides the drift metrics that AT-02 will evaluate |

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
