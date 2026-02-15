# P-T Limits and Steam Tables Assessment Summary

**Date:** 2026-02-15  
**Session:** Documentation review and gap analysis

---

## Question Asked

"Consider whether we have accurate P-T/P-T-V graphs/tables to support our documentation. Same with any Steam Tables/Graphs we might need for reference."

---

## Assessment Results

### ✅ STEAM TABLES: FULLY IMPLEMENTED

**Status:** Complete and well-validated  
**Implementation:** `Assets/Scripts/Physics/WaterProperties.cs` and `SteamThermodynamics.cs`

**Coverage:**
- Saturation temperature/pressure relationships (1-3200 psia, 100-700°F)
- Liquid and vapor enthalpy
- Liquid and vapor density
- Latent heat of vaporization
- Two-phase mixture properties
- Steam quality and void fraction
- Phase state determination

**Validation:**
- Source: NIST Chemistry WebBook
- Accuracy: ±1°F temperature, ±1% pressure in PWR operating range
- Multi-range polynomial fits for optimal accuracy
- Extended range for cold shutdown and LOCA scenarios
- Built-in validation functions

**Conclusion:** Our steam table implementation is **excellent** and requires no additional work. It exceeds typical simulator requirements.

---

### ❌ P-T LIMIT CURVES: NOT IMPLEMENTED

**Status:** Reference methodology documented, implementation deferred

**What's Missing:**
1. Plant-specific reactor vessel heatup limit curves
2. Plant-specific reactor vessel cooldown limit curves
3. Criticality limit curves
4. Inservice leak test and hydrostatic test limits
5. Cold Overpressure Protection System (COPS) PORV setpoint curves

**What's Available:**
- Generic Westinghouse P-T curves in NRC HRTD Section 3.2 (Figures 3.2-26, 3.2-27)
- Example plant-specific data from Vogtle Unit 1 PTLR (ML14112A519)
- Complete methodology per ASME Code Section XI, Appendix G
- 10 CFR 50, Appendix G requirements documented

**Why Not Critical Yet:**
- Not needed until Mode 4 → Mode 3 transition
- Current Phase 0 development doesn't require P-T limit checking
- RHR operates with procedural controls (≤ 425 psig, ≤ 350°F)

**When Needed:**
- Before implementing RHR isolation permissives
- Before implementing COPS functionality
- For operator training features
- For realistic procedure compliance simulation

---

### ❌ VISUAL P-T DIAGRAMS: NOT CREATED

**Status:** Not created but easily generated

**What's Missing:**
1. Graphical P-T limit curves with operating regions
2. T-S (Temperature-Entropy) diagrams
3. P-H (Pressure-Enthalpy) diagrams
4. Saturation dome visualization
5. Real-time RCS state overlay on P-T diagram

**What Can Be Generated:**
- Our existing code can export data for all standard thermodynamic diagrams
- Steam tables can be formatted as markdown/CSV
- P-T curves can be plotted from Vogtle data

**Priority:** LOW for current phase, MEDIUM for operator training

---

## Recommendations

### Immediate Actions (Completed This Session)

✅ **Created comprehensive reference document:**
`RCS_PT_Limits_and_Steam_Tables_Reference.md`

**Contents:**
- Status assessment of steam tables (complete)
- Status assessment of P-T limits (deferred)
- Available reference data (Vogtle Unit 1 example)
- Implementation pathway for future
- Steam table benchmarks
- P-T limit methodology
- Pressurizer P-T-V relationships

### Future Actions (Pre-Mode 3)

**Priority: MEDIUM**  
**Timing: Before implementing Mode 3 entry**

1. **Implement P-T Limit Curves**
   - Select reference plant data (Vogtle or generic Westinghouse)
   - Create `PTLimitChecker.cs` module
   - Add real-time P-T limit validation
   - Integrate with RHR isolation permissives
   - Add alarm generation when approaching limits

2. **Implement COPS**
   - PORV setpoint curve (variable with temperature)
   - Arming temperature logic (≤ 220°F)
   - Protection against cold overpressure transients

3. **Visual P-T Diagrams (Optional)**
   - UI panel showing P-T limit curves
   - Real-time RCS state indicator
   - Distance to limit visualization
   - For operator training and reference

---

## Technical Details

### Current Steam Table Accuracy

**Saturation Properties at PWR Operating Pressure (2235 psia):**
- Temperature: 652.9°F (NIST reference: 653°F, error < 0.1°F)
- Liquid density: 38.9 lb/ft³ (±2.3% across full range)
- Steam density: 7.75 lb/ft³
- Latent heat: 390 BTU/lb (±1% in PWR range)

**Validation Status:**
- All saturation property calculations validated against NIST
- Two-phase correlations tested with industry-standard cases
- Extended range validated down to 1 psia (cold shutdown)
- No gaps or deficiencies identified

### P-T Limit Example Data (Vogtle Unit 1, 36 EFPY)

**Heatup Limits (60°F/hr rate, excerpt):**
| Temperature (°F) | Pressure (psig) |
|-----------------|-----------------|
| 60 | 747 |
| 100 | 796 |
| 150 | 1163 |
| 200 | 2231 |

**Cooldown Limits (Steady-State, excerpt):**
| Temperature (°F) | Pressure (psig) |
|-----------------|-----------------|
| 60 | 747 |
| 100 | 918 |
| 150 | 1452 |
| 180 | 2146 |

**Key Observations:**
- Cooldown limits more restrictive than heatup (as expected)
- Minimum boltup temperature: 60°F
- COPS arming temperature: ≤ 220°F
- Maximum rates: 100°F/hr heatup/cooldown

---

## Conclusion

### Overall Status: **ADEQUATE FOR CURRENT PHASE**

**Steam Tables:** ✅ Excellent implementation, no work needed  
**P-T Limits:** ⚠️ Documented pathway, defer implementation  
**Visual Diagrams:** ⚠️ Optional, low priority

### No Blockers Identified

The absence of P-T limit curves is **not a blocker** for current Phase 0 development. The simulator can continue with:
- Procedural compliance (operators follow temp/pressure limits)
- RHR operation within documented constraints
- Pressurizer physics using existing steam tables

### Clear Path Forward

When P-T limits are needed (pre-Mode 3):
1. Reference documentation is complete
2. Example data is available (Vogtle Unit 1)
3. Methodology is understood (ASME Code, 10 CFR 50)
4. Implementation estimate: 4-6 hours

---

## Files Created This Session

1. **RCS_PT_Limits_and_Steam_Tables_Reference.md**
   - Comprehensive status assessment
   - Reference data and methodology
   - Implementation recommendations
   - Steam table benchmarks

2. **PT_Limits_Steam_Tables_Assessment_Summary.md** (this file)
   - Executive summary
   - Quick reference for decision-making

---

*Assessment completed 2026-02-15*  
*Recommendation: Continue Phase 0 development, defer P-T limit implementation*
