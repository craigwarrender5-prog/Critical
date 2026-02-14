# UPDATE v1.0.1.6 — Wire Support Module Tests into Phase 1 Runner

**Date:** 2026-02-06
**Version:** 1.0.1.6
**Type:** Test Infrastructure Enhancement
**Backwards Compatible:** Yes

---

## Summary

Wired 35 previously-unwired tests from 5 support modules into Phase1TestRunner. These modules had ValidateCalculations() methods with internal test logic but were never called by the automated exit gate. Each module's internal tests have been decomposed into individual, named Test() calls for granular pass/fail reporting.

Phase 1 exit gate: **121 → 156 tests**.

---

## Changes

### Phase1TestRunner.cs — Modified

**5 new test suites added:**

| Suite | ID Prefix | Tests | Module Under Test | Reference |
|-------|-----------|-------|-------------------|-----------|
| CVCSController | CV-01→07 | 7 | CVCSController.cs | NRC HRTD 10.3 |
| RCSHeatup | RH-01→09 | 9 | RCSHeatup.cs | NRC HRTD 19.2 |
| RCPSequencer | RS-01→08 | 8 | RCPSequencer.cs | Westinghouse RCP Start Criteria |
| LoopThermodynamics | LT-01→06 | 6 | LoopThermodynamics.cs | Westinghouse 4-Loop RCS T-H |
| RVLISPhysics | RV-01→05 | 5 | RVLISPhysics.cs | NRC NUREG-0737 Supp. 1 |
| **Total** | | **35** | | |

### Test Details

**CVCSController (7 tests):**
- CV-01: Initialization produces valid active state
- CV-02: Low level increases charging above base + seal injection
- CV-03: High level decreases charging below base + seal injection
- CV-04: Very low level (15%) triggers letdown isolation
- CV-05: Integral error accumulates over sustained level deviation
- CV-06: Charging clamped to minimum seal injection flow (32 gpm)
- CV-07: Net flow calculation = charging - letdown

**RCSHeatup (9 tests):**
- RH-01: Heatup rate ~50°F/hr with 4 RCPs + 1.8 MW heaters
- RH-02: More RCPs = faster heatup (higher heat input)
- RH-03: Higher temperature = slower heatup (greater ambient losses)
- RH-04: Isolated heating increases PZR temperature
- RH-05: Surge line conduction heats RCS when T_pzr > T_rcs
- RH-06: Time to target is finite and reasonable
- RH-07: PZR heatup rate 40-120°F/hr with 1800 kW heaters (v1.0.3.0 stratified model)
- RH-08: PZR heats even at large ΔT=200°F (PZR-RCS)
- RH-09: RCS nearly static during isolated PZR heating

**RCPSequencer (8 tests):**
- RS-01: No RCPs without bubble formed
- RS-02: No RCPs immediately after bubble (< start delay)
- RS-03: First RCP after start delay (1.0 hr)
- RS-04: Second RCP after interval (0.5 hr)
- RS-05: All 4 RCPs after sufficient time
- RS-06: Low pressure blocks RCP start (NPSH interlock)
- RS-07: Scheduled start times correct (t₁=2.0 hr, t₂=2.5 hr)
- RS-08: 4 RCP heat input = 21 MW

**LoopThermodynamics (6 tests):**
- LT-01: With 4 RCPs: T_hot > T_cold, forced flow detected
- LT-02: With 0 RCPs and T_pzr > T_rcs: natural circulation mode
- LT-03: T_avg ≈ input T_rcs
- LT-04: ΔT at HZP with 4 RCPs = 5-15°F (RCP heat only, no fission)
- LT-05: Natural circulation flow in valid range (12k-23k gpm)
- LT-06: All ValidateCalculations() pass (aggregate)

**RVLISPhysics (5 tests):**
- RV-01: With RCPs: dynamic range valid, full range invalid
- RV-02: Without RCPs: full range valid, dynamic invalid
- RV-03: Full RCS mass → dynamic range > 95%
- RV-04: 80% mass with no RCPs → low level alarm triggered
- RV-05: Dynamic range depressed with no flow (< 50%)

---

## Test Count Summary

| Category | Before | After |
|----------|--------|-------|
| Module unit tests (inline) | 105 | 140 |
| Integration tests | 7 | 7 |
| Heatup integration tests | 9 | 9 |
| **Phase 1 Total** | **121** | **156** |
| Phase 2 (unchanged) | 95 | 95 |
| **Grand Total (both runners)** | **216** | **251** |

---

## Resolves

- **Audit Issue #34 (MEDIUM):** "32 internal ValidateCalculations() tests not wired into test runners"
  - Actual implementation: 35 granular tests (individual ValidateCalculations() assertions were expanded into named tests)
  - Status: **RESOLVED**

---

## Files Modified

| Action | File |
|--------|------|
| MODIFIED | `Assets/Scripts/Tests/Phase1TestRunner.cs` |
| CREATED | `Assets/Documentation/Updates/UPDATE-v1.0.1.6.md` |

---

## No Physics Changes

This update is purely test infrastructure. No physics modules, constants, or calculations were modified.
