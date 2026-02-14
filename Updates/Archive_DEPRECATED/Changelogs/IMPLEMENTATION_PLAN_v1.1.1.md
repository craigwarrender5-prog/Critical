# IMPLEMENTATION PLAN v1.1.1 — Post-Release Bug Fixes

## Version: 1.1.1
## Date: 2026-02-09
## Status: PENDING APPROVAL

---

## Problem Summary

Three critical issues identified from post-v1.1.0 simulation testing:

### Issue 1: Heatup Rate Stuck at ~26°F/hr (Expected: ~50°F/hr)
**Symptom:** At 12 hours into simulation with 4 RCPs running (21 MW), heatup rate is only 26.73°F/hr instead of expected ~50°F/hr.

**Root Cause Analysis:**
From log at T+12hr:
- Gross Heat Input: 22.80 MW (21 MW RCP + 1.8 MW heaters)
- SG Secondary Loss: 14.44 MW
- Heat Losses: 0.18 MW
- Net Heat Input: 8.18 MW

The SG secondary is absorbing 14.44 MW, which is ~63% of gross heat input. This is excessive.

**Physics Analysis:**
```
Q_sg = U × A × ΔT
Q_sg = 100 BTU/(hr·ft²·°F) × 220,000 ft² × 2.24°F
Q_sg = 49.3 MBTU/hr = 14.4 MW ✓ (matches log)
```

The thermal lag (ΔT = T_RCS - T_SG) is only 2.24°F, but with U=100 and A=220,000 ft², even this small ΔT creates massive heat transfer.

**The Real Problem:** The SG secondary thermal mass model is working correctly, but the heat transfer is too efficient at low temperatures. During early heatup (<200°F), natural convection on the secondary side should be MUCH lower because:
1. Lower temperature differences produce lower Rayleigh numbers
2. Lower Rayleigh numbers produce lower Nusselt numbers
3. Lower Nusselt numbers produce lower HTCs

The current model uses a FIXED HTC of 100 BTU/(hr·ft²·°F) regardless of temperature, which overestimates heat transfer at low temperatures.

**Fix:** Implement temperature-dependent HTC scaling for the SG secondary side.

---

### Issue 2: Simulation Stops at 24 Hours Without Reaching HZP
**Symptom:** Simulation terminates at 24-hour limit with T_avg = 438°F (target: 557°F).

**Root Cause:** Direct consequence of Issue 1. At 26°F/hr:
- Time to reach 557°F from 100°F: (557-100)/26 = 17.6 hours
- Plus 8.5 hours for bubble formation phase
- Total ≈ 26 hours, exceeding 24-hour limit

**Fix:** Resolving Issue 1 will fix Issue 2 automatically.

---

### Issue 3: Immediate Inventory Conservation Alarm (924,296 lbm error)
**Symptom:** Conservation error alarm triggers immediately at T+0.25hr with error = 924,296 lbm.

**Root Cause:** From log at T+0.25hr:
```
Initial Mass:       0 lbm
Expected Mass:      0 lbm
TOTAL MASS:         924296 lbm
Error (absolute):   924295.7 lbm
```

The `Initial_Total_Mass_lbm` is 0 because `InitializeInventoryAudit()` is never called during initialization. The method exists in `HeatupSimEngine.Logging.cs`, but `InitializeCommon()` in `HeatupSimEngine.Init.cs` does not call it.

**Fix:** Add call to `InitializeInventoryAudit()` in `InitializeCommon()`.

---

## Expected Behavior

### Issue 1 (Heatup Rate):
- With 4 RCPs (21 MW) + heaters (1.8 MW) = 22.8 MW gross
- Heat losses at HZP: ~1.5 MW insulation
- SG heat absorption should equilibrate to ~8-9 MW (not 14+ MW)
- Net heat: ~12-13 MW
- Heatup rate: ~45-55°F/hr per NRC HRTD 19.2.2

### Issue 2 (Simulation Duration):
- Cold shutdown (100°F) to HZP (557°F) = 457°F rise
- At 50°F/hr: 457/50 = 9.1 hours of RCP heatup
- Plus ~8.5 hours bubble formation phase
- Total: ~17-18 hours (well within 24-hour limit)

### Issue 3 (Inventory):
- Initial mass should be ~924,000 lbm (RCS + PZR + VCT + BRS)
- Conservation error should start at 0 lbm
- Error should remain <500 lbm throughout simulation

---

## Proposed Fixes

### Fix 1: Temperature-Dependent SG HTC (PlantConstants.Heatup.cs, SGSecondaryThermal.cs)

**Rationale:** Natural convection heat transfer coefficient scales with Rayleigh number, which depends on ΔT and temperature-dependent fluid properties. At low temperatures, the HTC should be significantly lower.

**Engineering Basis:**
Churchill-Chu correlation for natural convection on horizontal cylinders:
```
Nu = [0.6 + 0.387 × Ra^(1/6) / (1 + (0.559/Pr)^(9/16))^(8/27)]²
```

At 100°F (ΔT~10°F): Ra ≈ 10^7, Nu ≈ 30, h ≈ 30 BTU/(hr·ft²·°F)
At 300°F (ΔT~15°F): Ra ≈ 10^8, Nu ≈ 60, h ≈ 60 BTU/(hr·ft²·°F)
At 500°F (ΔT~15°F): Ra ≈ 10^9, Nu ≈ 100, h ≈ 100 BTU/(hr·ft²·°F)

**Implementation:**
1. Add temperature-dependent HTC function to SGSecondaryThermal.cs
2. Replace fixed HTC with temperature-scaled value
3. Scale factor: HTC_effective = HTC_base × f(T_secondary)
4. f(T) = 0.3 at 100°F, 0.6 at 300°F, 1.0 at 500°F (linear interpolation)

### Fix 2: Initialize Inventory Audit (HeatupSimEngine.Init.cs)

**Implementation:**
Add call to `InitializeInventoryAudit()` at end of `InitializeCommon()`.

---

## Implementation Stages

### Stage 1: Inventory Audit Initialization Fix
**File:** `HeatupSimEngine.Init.cs`
**Change:** Add `InitializeInventoryAudit()` call in `InitializeCommon()`
**Risk:** Low — simple addition, no physics changes
**Testing:** Verify Initial_Total_Mass_lbm is ~924,000 lbm at T+0

### Stage 2: Temperature-Dependent SG HTC
**Files:** 
- `PlantConstants.Heatup.cs` — Add HTC scaling constants
- `SGSecondaryThermal.cs` — Implement GetCurrentHTC() temperature scaling

**Change:** 
- Add SG_HTC_TEMP_SCALE_MIN, SG_HTC_TEMP_SCALE_MID constants
- Modify GetCurrentHTC() to scale with secondary temperature
- Scale factor: 0.3 at 100°F → 1.0 at 500°F

**Risk:** Medium — affects heat balance throughout simulation
**Testing:** 
- Verify heatup rate ~50°F/hr at steady state with 4 RCPs
- Verify SG heat absorption ~8-9 MW at equilibrium
- Verify total simulation time ~17-18 hours to HZP

---

## Unaddressed Issues

None — all three reported issues are addressed in this plan.

---

## Validation Criteria

1. **Inventory Audit:** Initial_Total_Mass_lbm ≈ 924,000 lbm (not 0)
2. **Conservation Error:** Starts at 0, remains <500 lbm throughout
3. **Heatup Rate:** 45-55°F/hr with 4 RCPs running at steady state
4. **SG Heat Absorption:** 7-10 MW at equilibrium (not 14+ MW)
5. **Simulation Duration:** Reaches HZP (557°F) in 17-20 hours
6. **No Regressions:** Bubble formation, CVCS, alarms function correctly

---

## Files to Modify

| File | Change |
|------|--------|
| HeatupSimEngine.Init.cs | Add InitializeInventoryAudit() call |
| PlantConstants.Heatup.cs | Add HTC temperature scaling constants |
| SGSecondaryThermal.cs | Implement temperature-dependent HTC |

---

## Approval Required

Please confirm to proceed with implementation, starting with Stage 1.
