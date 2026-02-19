# CS-0128 Investigation Report (2026-02-18_203000)

**Title:** No-RCP RHR pressurization path over-couples thermal pump energy into bulk RCS during Mode 5 startup  
**Severity:** HIGH  
**Domain:** Primary Thermodynamics  
**Status:** READY  
**Created:** 2026-02-18T20:30:00Z  
**Updated:** 2026-02-18T20:30:00Z  
**Assigned DP:** DP-0001

---

## 1. Problem Summary

During Mode 5 pressurization with no RCP circulation, observed RCS pressure rise is larger than expected from physically plausible no-flow coupling. This appears tied to how RHR-side thermal energy is transferred into RCS bulk state during no-RCP conditions.

---

## 2. Investigation Findings

1. RHR/no-RCP coupling behavior has been corrected once (`CS-0122`) but remains sensitive to transport assumptions under no-forced-flow conditions.
2. Current no-RCP RHR bulk-coupling still relies on tuned factors rather than a fully validated envelope for:
   - natural convection limits,
   - no-circulation stratification persistence,
   - ambient heat losses,
   - large RCS thermal mass inertia.
3. User-observed symptom remains: RCS pressure increase during PZR pressurization that appears stronger than expected when thermal redistribution should be bounded.

---

## 3. Root Cause (Current Best Explanation)

The no-RCP RHR-to-RCS bulk-transfer model is still a reduced-order approximation and has not yet been fully anchored to an accepted startup fidelity envelope for zero/near-zero circulation conditions.

---

## 4. Disposition

**Disposition: READY (HIGH priority follow-on to CS-0122).**

This item should be treated as priority thermal-fidelity work in `DP-0001` before relying on affected startup pressure/temperature behavior for policy acceptance.

---

## 5. Proposed Resolution Direction

1. Define explicit acceptance envelope for no-RCP RHR thermal redistribution (temperature and pressure response bounds).
2. Tighten no-RCP transfer model terms for convection, stratification damping, ambient loss, and RCS mass inertia.
3. Add deterministic telemetry checks for no-RCP RHR transfer contribution and pressure coupling impact.
4. Run dedicated no-RCP baseline scenarios and confirm pressure response remains within defined envelope.

---

## 6. Acceptance Criteria

1. With `RCPs OFF`, RHR operation does not produce excessive bulk RCS warming outside defined no-flow envelope.
2. During PZR pressurization with no forced circulation, pressure rise trend matches approved bounded behavior.
3. Added telemetry can isolate no-RCP RHR contribution to bulk RCS energy each step.
4. Regression checks confirm no degradation of prior `CS-0122` closure behavior.

---

## 7. Evidence and Related References

- `Assets/Scripts/Validation/HeatupSimEngine.cs`
- `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs`
- `Governance/Issues/CS-0122_Investigation_Report_2026-02-18_113000.md`

---

## 8. Tags

- `Primary-Thermodynamics`
- `RHR-No-RCP-Coupling`
- `Natural-Convection`
- `Stratification`
- `Startup-Pressure-Fidelity`
- `High-Priority`
- `User-Request-2026-02-18`
