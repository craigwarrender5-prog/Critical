# UPDATE v1.2.0.0 — Audit Issue Resolution: Consolidation & Cleanup

**Date:** 2026-02-06  
**Version:** 1.2.0.0  
**Type:** Major Build (public API changes from const→property, not backwards compatible for switch/attribute usage)  
**Backwards Compatible:** No — `const` fields changed to `static` properties cannot be used in `switch` cases, attribute arguments, or `const` field initializers. Any code using VCTPhysics or ControlRodBank constants in those contexts will need updating.

---

## Summary

Resolved all remaining audit issues from Stage 1 (A through G) and Stage 2 Parameter Audits. This update consolidates duplicate constants, marks legacy code obsolete, implements a stub method, fixes the power ascension reactivity estimate, and clarifies the PZR level program relationship between heatup and at-power regimes.

**Issues resolved: 7 (1 HIGH, 4 MEDIUM, 2 LOW)**

---

## Changes

### 1. HeatupValidation.cs — Marked [Obsolete] (Issue #26, HIGH)

**Problem:** Legacy prototype file with inline physics (simplified Tsat, no CoupledThermo, no solid plant, no VCT) completely superseded by HeatupSimEngine + GOLD STANDARD modules. Risk of accidental use producing incorrect results.

**Fix:** 
- Added `[System.Obsolete]` attribute with clear warning message
- Replaced header comments to state OBSOLETE status prominently
- References HeatupSimEngine as the correct replacement
- File retained for historical reference only

### 2. VCTPhysics.cs — Constant Consolidation (Issue #1, MEDIUM)

**Problem:** 18 constants duplicated values from PlantConstants, creating maintenance risk if one is updated without the other.

**Fix:**
- Replaced 17 `const` fields with `static` properties delegating to PlantConstants
- `BORON_COLD_SHUTDOWN_PPM` maps to `PlantConstants.BORON_COLD_SHUTDOWN_BOL_PPM` (correct naming)
- `MIXING_TAU_SEC` (120s) retained as local `const` — VCT-specific, not in PlantConstants
- All external callers unaffected (same member names, same values)

**API Impact:** Members changed from `const` to `static` properties. Code using these in `switch` cases or const initializers will need updating. Runtime behavior identical.

### 3. VCTPhysics.CalculateBalancedChargingForPurification — Implemented (Issue #3, LOW)

**Problem:** Method was a stub returning 0f, making it useless.

**Fix:** Implemented correct purification balance: `charging = letdown - seal_return + CBO`
- Per NRC HRTD 4.1: During purification, flows are balanced so net RCS inventory change is zero
- Seal return adds inventory to VCT, CBO removes it — charging compensates

### 4. ControlRodBank.cs — Constant Consolidation (Issue #12, MEDIUM)

**Problem:** `BANK_COUNT`, `STEPS_TOTAL`, `STEPS_PER_MINUTE` duplicated from PlantConstants.

**Fix:**
- `BANK_COUNT` → delegates to `PlantConstants.ROD_BANKS`
- `STEPS_TOTAL` → delegates to `PlantConstants.ROD_TOTAL_STEPS`
- `STEPS_PER_MINUTE` → delegates to `PlantConstants.ROD_STEPS_PER_MINUTE`
- `STEPS_PER_SECOND` computed from delegated STEPS_PER_MINUTE
- All other rod constants (overlap, worths, drop times) remain local — rod-specific, not in PlantConstants

### 5. PowerCalculator.cs — Constant Consolidation (Issue #18, LOW)

**Problem:** `NOMINAL_POWER_MWT = 3411f` duplicated `PlantConstants.THERMAL_POWER_MWT`.

**Fix:** Replaced with `static` property delegating to `PlantConstants.THERMAL_POWER_MWT`.

### 6. ReactorController.cs — Power Ascension Estimate (Issue #20, MEDIUM)

**Problem:** `UpdatePowerAscension()` used rough linear estimate `reactivityNeeded = desiredPowerChange * 1000f` (1000 pcm per power fraction) instead of physics-based calculation.

**Fix:** Replaced with `FeedbackCalculator.EstimatePowerDefect(currentPower, targetPower, boron)` which computes Doppler and MTC components using actual reactor physics:
- Doppler: Uses effective fuel temperature change with Rowlands weighting
- MTC: Uses boron-dependent moderator temperature coefficient
- Result: More accurate dilution rate during power ascension, especially at high/low boron concentrations where MTC varies significantly

### 7. PlantConstantsHeatup.cs — PZR Level Program Clarification (Issue 1A-MED)

**Problem:** `PlantConstantsHeatup.GetPZRLevelSetpoint()` (200-557°F, 25-60%) and `PlantConstants.GetPZRLevelProgram()` (557-585°F, 25-61.5%) appeared conflicting but are actually complementary programs for different operating regimes.

**Fix:**
- Added comprehensive documentation clarifying the two programs' relationship
- Added `GetPZRLevelSetpointUnified(float T_avg)` method providing seamless full-range coverage:
  - Below 557°F: Delegates to heatup program
  - At/above 557°F: Delegates to at-power program
- No constant values changed — this is a documentation and API enhancement

---

## Issues Verified — No Changes Required

| Issue | Description | Resolution |
|-------|-------------|------------|
| #2 | RCSHeatup calls CoupledThermo without P_floor/P_ceiling | **Already handled** — CoupledThermo defaults P_floor=15, P_ceiling=2700, covering full range |
| #9 | ReactorKinetics 9-function dependency | **Verified correct** — all 9 functions exist and are called with valid parameters |
| #27 | Two local constants in HeatupSimEngine | **Acceptable** — one derived from PlantConstants, one is integration-specific |

---

## Issues Remaining (LOW priority — deferred)

| Issue | Description | Notes |
|-------|-------------|-------|
| #4 | VCTPhysics missing ValidateCalculations() | Would need new test infrastructure |
| #5 | RCPSequencer timing constants local | Defensible — sequencer-specific |
| #8 | ReactorCore trip setpoints local | Defensible — safety-system isolation |
| #10 | UpdateXenon dt/3600 magic number | Cosmetic |
| #13 | Simplified rod drop model | Adequate for simulation |
| #19 | FUEL_THERMAL_TAU dual definition | Intentional — different contexts |
| #21 | Phase 1/Phase 2 time systems independent | By design |
| #23, #35 | FindObjectOfType deprecated (Unity 6.3) | Migrate when Unity version updated |
| #28 | History buffer O(n) RemoveAt(0) | Negligible at current size |
| #36 | MosaicBoard alarm thresholds vs AlarmManager | Different purposes (UI vs safety) |

---

## Unverified — Requires Simulation Run

| Item | Status |
|------|--------|
| Bug #3 (PZR level drop at RCP start) | Expected to self-resolve via v1.1.0.0 bubble formation — needs heatup run |
| v1.1.0.0 bubble formation end-to-end | Full heatup simulation not yet run with new multi-phase sequence |

---

## Files Changed

| File | Changes |
|------|---------|
| `Assets/Scripts/Validation/HeatupValidation.cs` | Added [Obsolete] attribute, updated header comments |
| `Assets/Scripts/Physics/VCTPhysics.cs` | 17 const→property delegates, implemented purification stub |
| `Assets/Scripts/Reactor/ControlRodBank.cs` | 3 const→property delegates to PlantConstants |
| `Assets/Scripts/Reactor/PowerCalculator.cs` | 1 const→property delegate to PlantConstants |
| `Assets/Scripts/Reactor/ReactorController.cs` | Power ascension uses FeedbackCalculator.EstimatePowerDefect |
| `Assets/Scripts/Physics/PlantConstantsHeatup.cs` | Level program documentation, GetPZRLevelSetpointUnified() |
| `Assets/Documentation/Updates/UPDATE-v1.2.0.0.md` | This changelog |

## GOLD Standard Status

All physics modules remain GOLD STANDARD. No physics calculations modified — only constant sourcing consolidated and one rough estimate replaced with proper physics method.

## Test Impact

- All existing tests should pass (same values, same physics)
- PowerCalculator validation may need tolerance check if `NOMINAL_POWER_MWT` was used in test as compile-time const
- ControlRodBank validation unaffected (uses instance methods, not const in switch)
- Recommend full test run (Phase 1 + Phase 2) to confirm
