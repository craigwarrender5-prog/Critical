# UPDATE v1.0.5.0 — WaterProperties NIST Polynomial Corrections

**Date:** 2026-02-06  
**Type:** Major Revision (accuracy corrections)  
**Scope:** WaterProperties.cs, ValidateAgainstNIST()  
**Backwards Compatible:** Yes (API unchanged, improved accuracy)

---

## Summary

Stage 2 Parameter Audit Part 2 identified polynomial coefficient errors in WaterProperties.cs affecting sub-atmospheric ranges and high-pressure/high-temperature regions. All corrections were derived from NIST Chemistry WebBook (IAPWS-IF97) steam table data using least-squares polynomial regression.

## Changes

### FIX 1: Sub-atmospheric Saturation Temperature (1–14.7 psia)
**File:** `WaterProperties.cs` → `SaturationTemperature()`  
**Line:** ~60  
**Error found:** Old quadratic polynomial had max error of +18°F at 14.7 psia  
**Fix:** Replaced with cubic polynomial fitted to 15 NIST data points (1–14.7 psia)  
- **Old:** `3.220f * lnP² + 39.167f * lnP + 101.690f`
- **New:** `0.261765f * lnP³ + 2.0752f * lnP² + 33.5728f * lnP + 101.6966f`
- **Improvement:** Max error reduced from ±18°F to ±0.04°F

### FIX 2: Sub-atmospheric Saturation Pressure (100–212°F)
**File:** `WaterProperties.cs` → `SaturationPressure()`  
**Line:** ~103  
**Error found:** Old quadratic exponential had max error of 83% at low temperatures  
**Fix:** Replaced with cubic exponential fitted to 13 NIST data points (100–212°F)  
- **Old:** `exp(-1.841e-5 * T² + 0.02567 * T - 3.700)`
- **New:** `exp(7.73229e-8 * T³ - 8.172349e-5 * T² + 0.044061 * T - 3.7172)`
- **Improvement:** Max error reduced from 83% to <0.1%

### FIX 3: Latent Heat of Vaporization — Full Range Refit
**File:** `WaterProperties.cs` → `LatentHeat()`  
**Lines:** ~142–163  
**Error found:** Old 2-range polynomial had errors up to 49% at high pressure (2800 psia) and 5% in low range  
**Fix:** Replaced with 3-range polynomial system fitted to 24 NIST saturation data points  
- **Range 1 (1–800 psia):** `hfg = -14.922 * lnP² + 71.681 * lnP + 880.575` (max ±1%)
- **Range 2 (800–2200 psia):** `hfg = -177.786 * lnP² + 2271.281 * lnP - 6548.327` (max ±2.2%)
- **Range 3 (2200–3200 psia):** `hfg = -1860.925 * lnP² + 28086.867 * lnP - 105526.088` (max ±7%, ±2% in 2200–2600)
- Added `Math.Max(hfg, 0f)` floor clamp for near-critical behavior
- **Key correction:** At 2250 psia (PWR operating), old code gave 445 BTU/lb; NIST value is 390.7 BTU/lb. New code gives 397 BTU/lb (+1.8%)

### FIX 4: Liquid Water Density — Cubic Polynomial
**File:** `WaterProperties.cs` → `WaterDensity()`  
**Lines:** ~187–195  
**Error found:** Old quadratic polynomial had errors up to 19% at high temperature (649°F)  
**Fix:** Replaced with cubic polynomial fitted to 21 NIST data points at 2250 psia reference  
- **Old:** `ρ = 62.42 - 0.018*T - 1.5e-5*T²` (quadratic, 14.7 psia ref)
- **New:** `ρ = -8.24913e-8*T³ + 3.978119e-5*T² - 0.030586*T + 65.0399` (cubic, 2250 psia ref)
- Pressure correction reference updated from 14.7 psia to 2250 psia
- **Improvement:** Max error reduced from ±19% to ±2.3%

### FIX 5: Validation Target Correction
**File:** `WaterProperties.cs` → `ValidateAgainstNIST()`  
**Line:** ~497  
**Error found:** Test 4 checked hfg(2250) ≈ 465 BTU/lb — this was incorrect  
**Fix:** Changed to 390 BTU/lb per NIST (hg=1091.1 - hf=700.4 = 390.7)  
- **Old:** `hfg1 - 465f) / 465f)`
- **New:** `hfg1 - 390f) / 390f)`

## Verification Method

All polynomial coefficients were derived using:
1. NIST Chemistry WebBook saturation tables (IAPWS-IF97 formulation)
2. Python numpy.polyfit least-squares regression
3. Point-by-point validation against NIST reference data at 15–24 points per function
4. Cross-checks against SlaythePE pressure tables (US customary units)

## Audit Items Verified (No Changes Required)

The following items were verified as correct during Stage 2 Audit Part 2:

| Item | Parameter | Status | Justification |
|------|-----------|--------|---------------|
| #4 | SYSTEM_DAMPING = 0.18 | ✓ ACCEPTABLE | Matches LOFTRAN dP/dT ~10 psi/°F |
| #5 | PlantConstants 130+ values | ✓ VERIFIED | (v1.0.4.0) |
| #6 | CVCS PI gains Kp=3.0, Ki=0.05 | ✓ ACCEPTABLE | Stable response, 45-min recovery |
| #7 | VCT level setpoints | ✓ VERIFIED | Match NRC HRTD 4.1 / FSAR Ch 9 |
| #8 | PlantConstantsHeatup | ✓ VERIFIED | (v1.0.4.0) |
| #9 | AlarmManager setpoints | ✓ VERIFIED | Match NRC HRTD documentation |
| #10 | SPRAY_EFFICIENCY=0.85, HEATER_TAU=20s | ✓ ACCEPTABLE | Within EPRI/NUREG ranges |
| #11 | Solid plant PI gains Kp=0.5, Ki=0.02 | ✓ ACCEPTABLE | Clamp enforced in code |
| #12 | Relief valve 450 psig | ✓ VERIFIED | Match NRC HRTD 19.2.1 |

## Impact Assessment

- **PWR Operating Range (100–2500 psia, 300–653°F):** All functions now accurate to within ±2.5%
- **Sub-atmospheric (1–14.7 psia, 100–212°F):** Errors reduced from 18–83% to <0.1%
- **Near-critical (>2500 psia):** Errors reduced from 49% to 7% (inherent polynomial limitation near critical point)
- **No API changes:** All method signatures unchanged; drop-in replacement
- **Existing tests:** ValidateAgainstNIST() target corrected; all tests should pass

## Files Changed

| File | Changes |
|------|---------|
| `Assets/Scripts/Physics/WaterProperties.cs` | 5 fixes (polynomials + validation) |
| `Assets/Documentation/Updates/UPDATE-v1.0.5.0.md` | This changelog |
| `Assets/Documentation/Phase 2/AUDIT_Stage2_ParameterAudit_Part2.md` | Audit report |
