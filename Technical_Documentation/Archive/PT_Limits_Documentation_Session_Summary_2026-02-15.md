# P-T Limits Documentation Session Summary

**Date:** 2026-02-15  
**Session Focus:** Retrieve P-T limit curves for Mode 4 → Mode 3 transition

---

## Objective

Retrieve comprehensive pressure-temperature (P-T) limit curve documentation needed for implementing Mode 5 → Mode 4 → Mode 3 transition sequence in the simulator.

---

## Documentation Retrieved

### Primary Document Created

**RCS_Pressure_Temperature_Limit_Curves_Implementation.md**

**Contents:**
- Complete P-T limit curves with numerical data tables
- Braidwood Unit 1 (57 EFPY) — full heatup and cooldown curves
- LTOP/COPS PORV setpoint curves
- Implementation guidelines for simulator
- Data structures and algorithms
- Operator display requirements
- Test cases for validation

### Reference Data Sources

1. **Braidwood Unit 1 PTLR Revision 9** (ML22293A006)
   - 57 EFPY limits (most conservative available)
   - Complete data tables extracted
   - Limiting material: Nozzle Shell Forging 5P-7056
   - All heatup/cooldown rates (0, 25, 50, 100°F/hr)

2. **Vogtle Unit 1 PTLR Revision 5** (ML14112A519)
   - 36 EFPY limits (previously retrieved)
   - Alternative reference available
   - Limiting material: Intermediate Shell Plate B8805-2

---

## Key Data Provided

### Heatup Limit Curve (100°F/hr)

**Full data table with 30 points covering 60-190°F**

Example key points:
- 60°F: 879 psig
- 100°F: 957 psig
- 150°F: 1422 psig
- 190°F: 2437 psig

### Cooldown Limit Curves

**Four curves provided:**
- Steady-state (0°F/hr)
- 25°F/hr cooldown
- 50°F/hr cooldown
- 100°F/hr cooldown

All with full data tables (60-155°F range)

**Key observation:** Cooldown limits are more restrictive than heatup at same temperature (as expected from thermal stress analysis)

### Criticality Limit Curve

**Purpose:** Additional margin for reactor criticality operations

**Basis:** Inservice hydrostatic test temperature = 135°F

**Data table:** 16 points covering 135-230°F

**Requirement:** For criticality, vessel temperature must be:
- ≥ 135°F (hydrostatic test temperature), AND
- ≥ 40°F above minimum heatup temperature at current pressure

### LTOP/COPS PORV Setpoints

**PCV-455A:**
- 60-300°F: 541 psig
- 400°F: 2335 psig (to prevent inadvertent lift at power)
- Linear interpolation between 300-400°F

**PCV-456:**
- 60-300°F: 618 psig
- 400°F: 2335 psig
- Linear interpolation between 300-400°F

**Enable temperature:** ≤ 350°F

---

## Critical Information for Mode Transitions

### Mode 5 → Mode 4

**Prerequisites:**
- Reactor vessel head installed
- Boltup temperature ≥ 60°F
- P-T limits satisfied
- Solid plant pressurization to 400-425 psig
- RCPs started

### Mode 4 → Mode 3

**Prerequisites:**
- RCS temperature ≥ 350°F (RHR isolation permissive)
- RCS pressure ≤ 425 psig (RHR design limit)
- P-T limits satisfied
- Normal letdown orifices in service
- LTOP disarmed (when T ≥ 350°F)

### Mode 3 (Hot Standby)

**Entry conditions:**
- Temperature ≥ 350°F
- RHR isolated
- Pressure controlled by normal systems
- Ready for reactor startup

**Criticality conditions:**
- Temperature ≥ 551°F (Tech Spec minimum)
- Normal no-load: 557°F @ 2235 psig
- Criticality limit curve satisfied

---

## Implementation Guidance Provided

### Data Structures

```csharp
public struct PTLimitPoint
{
    public float Temperature_F;
    public float Pressure_psig;
}

public class PTLimitCurve
{
    public string CurveName;
    public float MaxRate_F_per_hr;
    public List<PTLimitPoint> Points;
}
```

### Limit Checking Algorithm

Pseudocode provided for:
1. Interpolation between curve points
2. Rate-based curve selection (cooldown)
3. Violation detection (Acceptable/Caution/Alarm)
4. LTOP arming logic

### Operator Displays

Recommended UI elements:
- Real-time P-T point on curve overlay
- Distance to limit indicators
- Active curve identification
- Heatup/cooldown rate display
- Alarm status (approach/violation)

### Test Cases

5 comprehensive test cases provided:
1. Acceptable point verification
2. Approach warning threshold
3. Limit violation detection
4. LTOP boundary testing
5. Criticality limit validation

---

## Technical Background

### Why P-T Limits Exist

**Brittle Fracture Prevention:**
- Reactor vessel steel can fail in brittle (non-ductile) mode at low temperatures
- Nil-Ductility Transition Temperature (RTndt) defines boundary
- Below RTndt, stress from pressure + thermal gradients can propagate cracks
- Limits ensure stress intensity factor KI < fracture toughness KIc

**Heatup vs. Cooldown:**
- **Heatup:** Inner wall compressive (favorable) → Can tolerate higher pressure
- **Cooldown:** Inner wall tensile (unfavorable) → Must maintain lower pressure

**Neutron Embrittlement:**
- RTndt shifts upward with neutron fluence (EFPY)
- Chemistry Factor (CF): Copper and nickel content
- Fluence Factor (FF): Function of integrated neutron exposure
- Shift: ΔRTndt = CF × FF
- Curves become more restrictive with vessel age

### Regulatory Framework

**10 CFR 50, Appendix G:**
- Federal requirement for fracture toughness protection
- Mandates P-T limits based on vessel material properties
- Requires surveillance capsule program

**ASME Code Section XI, Appendix G:**
- Industry standard methodology
- Linear elastic fracture mechanics approach
- Stress intensity factor calculations

**Regulatory Guide 1.99, Rev. 2:**
- Embrittlement prediction methodology
- Chemistry factor and fluence factor correlations
- Margin calculations for uncertainties

---

## Files Created This Session

1. **RCS_Pressure_Temperature_Limit_Curves_Implementation.md** (Main Document)
   - Complete P-T limit curves with data tables
   - LTOP setpoint curves
   - Implementation guidelines
   - Test cases and validation

2. **PT_Limits_Documentation_Session_Summary.md** (This File)
   - Session summary
   - Quick reference
   - Implementation priority

---

## Implementation Priority

**Status:** HIGH — Required for Mode 4 → Mode 3 transition

**Implementation Estimate:** 6-8 hours
- Data structure creation: 1 hour
- Curve interpolation logic: 2 hours
- Limit checking algorithm: 2 hours
- UI display integration: 2 hours
- Testing and validation: 1 hour

**Dependencies:**
- Existing RCS temperature and pressure instrumentation
- Heatup/cooldown rate calculation
- Mode transition logic

**Testing Requirements:**
- Verify all 5 test cases pass
- Test heatup sequence from 120°F to 350°F
- Test cooldown limit selection based on rate
- Verify LTOP arming at 350°F boundary
- Validate alarm generation

---

## Next Steps

### Immediate (Before Mode 3 Implementation)

1. **Create PTLimitChecker.cs module**
   - Load curve data from configuration
   - Implement interpolation functions
   - Implement limit checking logic
   - Return violation status and distance to limit

2. **Integrate with RCS instrumentation**
   - Subscribe to temperature updates (auctioneered low Tc)
   - Subscribe to pressure updates (RCS wide-range)
   - Calculate heatup/cooldown rate

3. **Create operator display**
   - P-T curve overlay with current point
   - Color-coded regions (acceptable/caution/alarm)
   - Numerical distance to limit
   - Active curve identification

4. **Implement alarms**
   - P-T Limit Approach (50 psi or 10°F from limit)
   - P-T Limit Violation (exceeded limit)
   - Heatup/Cooldown Rate High (> 100°F/hr)
   - LTOP Armed status
   - LTOP Setpoint Exceeded

5. **Add to mode transition logic**
   - Mode 4 → Mode 3 interlock: T ≥ 350°F, P ≤ 425 psig, P-T limits OK
   - LTOP automatic arming/disarming at 350°F
   - Prevent RHR isolation if conditions not met

### Future Enhancements (Optional)

1. **Visual P-T diagram**
   - Graphical curve display
   - Historical trace of operating point
   - Zoom/pan capabilities

2. **Predictive warnings**
   - Estimate time to limit at current rate
   - Recommend rate reduction if approaching limit

3. **Procedure integration**
   - Step-by-step heatup/cooldown guidance
   - Automatic rate limiting
   - Mode transition checklists

---

## Conclusion

**Documentation Complete:** ✅

All P-T limit curves and LTOP setpoints have been retrieved from authoritative NRC sources (Braidwood Unit 1 PTLR). Complete numerical data tables are available for implementation.

**Ready for Implementation:** ✅

Comprehensive implementation guidance provided including data structures, algorithms, test cases, and operator display requirements.

**No Blockers:** ✅

All necessary information for Mode 5 → Mode 4 → Mode 3 transition implementation is now available.

---

*Session completed 2026-02-15*  
*All requested P-T limit documentation retrieved and formatted for implementation*
