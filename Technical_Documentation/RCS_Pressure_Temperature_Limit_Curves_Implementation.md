# RCS Pressure-Temperature Limit Curves — Implementation Reference

**Purpose:** P-T limit curves for Mode 4 → Mode 3 transition implementation  
**Date:** 2026-02-15  
**Status:** Ready for simulator implementation

---

## Overview

This document provides the pressure-temperature (P-T) limit curves required for implementing the Mode 5 → Mode 4 → Mode 3 transition sequence in the simulator. These curves define the acceptable combinations of RCS pressure and temperature during heatup and cooldown operations to prevent non-ductile (brittle) fracture of the reactor pressure vessel.

---

## Regulatory Basis

### 10 CFR 50, Appendix G
"Fracture Toughness Requirements" — Federal regulation governing P-T limits

**Key Requirements:**
- Limits based on reference temperature for nil-ductility transition (RTndt)
- Protection against non-ductile failure during pressure-temperature changes
- Accounts for neutron irradiation embrittlement over plant lifetime
- Requires periodic surveillance capsule testing

### ASME Boiler and Pressure Vessel Code, Section XI, Appendix G
"Fracture Toughness Criteria for Protection Against Failure"

**Methodology:**
- Linear elastic fracture mechanics principles
- Stress intensity factor (KI) must remain below fracture toughness (KIc)
- Considers thermal stresses during heatup/cooldown
- Adjusted for neutron fluence and vessel age

### Regulatory Guide 1.99, Revision 2
"Radiation Embrittlement of Reactor Vessel Materials"

**Embrittlement Prediction:**
- Shift in reference temperature: ΔRTndt = CF × FF
- Chemistry Factor (CF): function of copper and nickel content
- Fluence Factor (FF): f^(0.28 - 0.10×log(f))
- Margin accounts for uncertainties

---

## Reference Plant P-T Limit Data

### Two Example Plants Available

Both are Westinghouse 4-loop PWRs with similar design characteristics:

1. **Vogtle Unit 1** (ML14112A519)
   - 36 EFPY limits
   - Limiting material: Intermediate Shell Plate B8805-2
   - Maximum heatup/cooldown rate: 100°F/hr
   - Minimum boltup temperature: 60°F

2. **Braidwood Unit 1** (ML22293A006)
   - 57 EFPY limits
   - Limiting material: Nozzle Shell Forging 5P-7056
   - Maximum heatup/cooldown rate: 100°F/hr
   - Minimum boltup temperature: 60°F

**For simulator implementation, we'll use Braidwood Unit 1 data (more recent, more conservative).**

---

## Braidwood Unit 1 P-T Limits (57 EFPY)

### Limiting Material Properties

| Property | Value |
|----------|-------|
| Limiting Material | Nozzle Shell Forging 5P-7056 |
| Adjusted Reference Temperature (ART) at 1/4T | 75°F |
| Adjusted Reference Temperature (ART) at 3/4T | 61°F |
| Reactor Vessel Wall Thickness | 8.5 inches |
| Surface Fluence at 57 EFPY | 1.13 × 10¹⁹ n/cm² |

### Operating Limits

| Parameter | Value |
|-----------|-------|
| **Maximum Heatup Rate** | 100°F/hr |
| **Maximum Cooldown Rate** | 100°F/hr |
| **Maximum ΔT During Leak Test** | 10°F/hr (above heatup/cooldown curves) |
| **Minimum Boltup Temperature** | 60°F |

---

## Heatup Limit Curve (100°F/hr rate)

### Full Data Table

| Temperature (°F) | Pressure (psig) | Notes |
|-----------------|-----------------|-------|
| 60 | 0 | Minimum acceptable pressure |
| 60 | 879 | Heatup limit starts |
| 65 | 912 | |
| 70 | 921 | |
| 75 | 921 | |
| 80 | 921 | |
| 85 | 923 | |
| 90 | 929 | |
| 95 | 940 | |
| 100 | 957 | |
| 105 | 978 | |
| 110 | 1004 | |
| 115 | 1035 | |
| 120 | 1071 | |
| 125 | 1113 | |
| 130 | 1161 | |
| 135 | 1215 | **Criticality limit starts** |
| 140 | 1277 | |
| 145 | 1345 | |
| 150 | 1422 | |
| 155 | 1508 | |
| 160 | 1604 | |
| 165 | 1710 | |
| 170 | 1827 | |
| 175 | 1957 | |
| 180 | 2102 | |
| 185 | 2261 | |
| 190 | 2437 | Heatup curve ends |

### Criticality Limit Curve

**Purpose:** Additional margin for reactor criticality operations

**Basis:** Inservice hydrostatic test temperature (135°F for 57 EFPY service period)

| Temperature (°F) | Pressure (psig) |
|-----------------|-----------------|
| 135 | 0 |
| 135 | 940 |
| 140 | 957 |
| 145 | 978 |
| 150 | 1004 |
| 155 | 1035 |
| 160 | 1071 |
| 165 | 1113 |
| 170 | 1161 |
| 175 | 1215 |
| 180 | 1277 |
| 185 | 1345 |
| 190 | 1422 |
| 195 | 1508 |
| 200 | 1604 |
| 205 | 1710 |
| 210 | 1827 |
| 215 | 1957 |
| 220 | 2102 |
| 225 | 2261 |
| 230 | 2437 |

**Requirement:** For criticality (except low-power physics testing), reactor vessel temperature must be:
- ≥ Inservice hydrostatic test temperature (135°F), AND
- ≥ 40°F above minimum heatup curve temperature at current pressure

### Leak Test Limit

**Purpose:** Defines limits for inservice hydrostatic and leak testing

| Temperature (°F) | Pressure (psig) |
|-----------------|-----------------|
| 118 | 2000 |
| 135 | 2485 |

**Usage:** At RCS temperature > 400°F and pressure = 2235 psig, leak rate test performed if RCS opened for refueling.

---

## Cooldown Limit Curves

### Steady-State Cooldown (0°F/hr rate)

| Temperature (°F) | Pressure (psig) |
|-----------------|-----------------|
| 60 | 0 |
| 60 | 882 |
| 65 | 912 |
| 70 | 944 |
| 75 | 980 |
| 80 | 1020 |
| 85 | 1063 |
| 90 | 1112 |
| 95 | 1165 |
| 100 | 1224 |
| 105 | 1290 |
| 110 | 1362 |
| 115 | 1442 |
| 120 | 1530 |
| 125 | 1627 |
| 130 | 1735 |
| 135 | 1854 |
| 140 | 1986 |
| 145 | 2131 |
| 150 | 2292 |
| 155 | 2469 |

### 25°F/hr Cooldown Rate

| Temperature (°F) | Pressure (psig) |
|-----------------|-----------------|
| 60 | 0 |
| 60 | 854 |
| 65 | 886 |
| 70 | 923 |
| 75 | 963 |
| 80 | 1007 |
| 85 | 1056 |
| 90 | 1110 |
| 95 | 1165 |
| 100 | 1224 |
| 105 | 1290 |
| 110 | 1362 |
| 115 | 1442 |
| 120 | 1530 |
| 125 | 1627 |
| 130 | 1735 |
| 135 | 1854 |
| 140 | 1986 |
| 145 | 2131 |
| 150 | 2292 |
| 155 | 2469 |

### 50°F/hr Cooldown Rate

| Temperature (°F) | Pressure (psig) |
|-----------------|-----------------|
| 60 | 0 |
| 60 | 828 |
| 65 | 864 |
| 70 | 905 |
| 75 | 950 |
| 80 | 1000 |
| 85 | 1055 |
| 90 | 1110 |
| 95 | 1165 |
| 100 | 1224 |
| 105 | 1290 |
| 110 | 1362 |
| 115 | 1442 |
| 120 | 1530 |
| 125 | 1627 |
| 130 | 1735 |
| 135 | 1854 |
| 140 | 1986 |
| 145 | 2131 |
| 150 | 2292 |
| 155 | 2469 |

### 100°F/hr Cooldown Rate

| Temperature (°F) | Pressure (psig) |
|-----------------|-----------------|
| 60 | 0 |
| 60 | 788 |
| 65 | 835 |
| 70 | 887 |
| 75 | 944 |
| 80 | 1000 |
| 85 | 1055 |
| 90 | 1110 |
| 95 | 1165 |
| 100 | 1224 |
| 105 | 1290 |
| 110 | 1362 |
| 115 | 1442 |
| 120 | 1530 |
| 125 | 1627 |
| 130 | 1735 |
| 135 | 1854 |
| 140 | 1986 |
| 145 | 2131 |
| 150 | 2292 |
| 155 | 2469 |

---

## Cold Overpressure Protection System (COPS) / LTOP

### LTOP System Requirements

**Applicability:** RCS temperature < 350°F

**System Components:**
- Two Pressurizer Power-Operated Relief Valves (PORVs)
- PCV-455A and PCV-456
- Temperature-based setpoint adjustment

### LTOP PORV Setpoints (with Instrumentation Uncertainty)

#### PCV-455A Setpoints

| RCS Temperature (°F) | PORV Setpoint (psig) |
|---------------------|---------------------|
| 60 | 541 |
| 300 | 541 |
| 400 | 2335 |

**Interpolation:** For temperatures 300-400°F, linearly interpolate between 541 psig and 2335 psig.

**Note:** Setpoint extends to 400°F to prevent PORV lift from inadvertent LTOP arming while at power.

#### PCV-456 Setpoints

| RCS Temperature (°F) | PORV Setpoint (psig) |
|---------------------|---------------------|
| 60 | 618 |
| 300 | 618 |
| 400 | 2335 |

**Interpolation:** For temperatures 300-400°F, linearly interpolate between 618 psig and 2335 psig.

### LTOP Enable Temperature

**LTOP SHALL BE ARMED when:** Any RCS cold leg temperature ≤ 350°F

**LTOP SHALL BE DISARMED when:** All RCS cold leg temperatures ≥ 350°F

**Rationale:**
- Below 350°F: Vessel is susceptible to brittle fracture
- PORVs protect against inadvertent overpressure transients
- Above 350°F: Normal pressure control systems adequate

---

## Implementation Guidelines

### For Simulator Programming

#### 1. Data Structures Required

```csharp
// P-T Limit Point
public struct PTLimitPoint
{
    public float Temperature_F;
    public float Pressure_psig;
}

// P-T Limit Curve
public class PTLimitCurve
{
    public string CurveName;
    public float MaxHeatupRate_F_per_hr;  // or MaxCooldownRate
    public List<PTLimitPoint> Points;
}
```

#### 2. Limit Checking Algorithm

```csharp
public enum PTLimitViolation
{
    Acceptable,
    ApproachingLimit,      // Within 50 psi or 10°F
    ViolatingLimit         // Exceeded limit
}

public PTLimitViolation CheckPTLimit(
    float currentTemp_F,
    float currentPressure_psig,
    float currentRate_F_per_hr,
    PTLimitCurve limitCurve)
{
    // 1. Find bounding points in curve
    // 2. Interpolate allowed pressure at current temperature
    // 3. Compare current pressure to limit
    // 4. Account for rate (select appropriate cooldown curve)
    // 5. Return violation status
}
```

#### 3. Mode Transition Interlocks

**Mode 4 → Mode 3 Prerequisites:**
- RCS temperature ≥ 350°F (RHR isolation permissive)
- RCS pressure ≤ 425 psig (RHR isolation requirement)
- P-T limits satisfied for current conditions
- LTOP disarmed (if T ≥ 350°F)

**Mode 5 → Mode 4 Prerequisites:**
- Reactor vessel head installed
- RCS pressure > 0 psig (reactor not vented)
- P-T limits satisfied

### Operator Displays

#### P-T Limit Display Panel

**Elements:**
- Real-time RCS temperature (auctioneered low Tc)
- Real-time RCS pressure
- Current heatup/cooldown rate
- P-T limit curve overlay with:
  - Current operating point
  - Acceptable region (green)
  - Caution region (yellow, approaching limit)
  - Unacceptable region (red)
- Distance to limit (psi and °F)
- Active limit curve (heatup/cooldown rate, criticality)

#### Alarms

| Alarm | Setpoint | Priority |
|-------|----------|----------|
| P-T Limit Approach | 50 psi or 10°F from limit | CAUTION |
| P-T Limit Violation | Exceeded limit curve | ALARM |
| Heatup Rate High | > 100°F/hr | ALARM |
| Cooldown Rate High | > 100°F/hr | ALARM |
| LTOP Armed | T < 350°F and not armed | CAUTION |
| LTOP Setpoint Exceeded | P > LTOP setpoint | ALARM |

### Procedure Integration

#### Heatup Procedure (Mode 5 → Mode 4 → Mode 3)

**Initial Conditions (Mode 5):**
- RCS temperature: ~120°F
- RCS pressure: 50-100 psig
- Reactor vessel head installed

**Step 1: Pressurize to 400-425 psig (still Mode 5)**
- Charging > Letdown
- Check P-T limits continuously
- Solid plant pressure control

**Step 2: Start RCPs (Mode 5 → Mode 4)**
- Pressure maintained 400-425 psig
- All RCPs running
- Heat RCS with RCPs and RHR heat exchangers bypassed

**Step 3: Heat to 350°F**
- Heatup rate ≤ 100°F/hr
- Monitor P-T limits
- LTOP armed below 350°F

**Step 4: Isolate RHR (Mode 4 → Mode 3)**
- RCS temperature ≥ 350°F
- RCS pressure ≤ 425 psig
- Switch to normal letdown orifices
- Disarm LTOP
- Enter Mode 3 (Hot Standby)

---

## Key Temperatures and Pressures for Mode Transitions

### Mode 5 (Cold Shutdown)

| Parameter | Value | Notes |
|-----------|-------|-------|
| Initial T | ~120°F | After refueling cooldown |
| Initial P | 50-100 psig | Solid plant |
| Boltup T | ≥ 60°F | Minimum for head installation |

### Mode 4 (Hot Shutdown)

| Parameter | Value | Notes |
|-----------|-------|-------|
| Entry T | ~250-300°F | RCPs started, bubble drawn |
| Entry P | 400-425 psig | Before RHR isolation |
| RHR Isolation T | ≥ 350°F | Technical Specification limit |
| RHR Isolation P | ≤ 425 psig | RHR design limit |

### Mode 3 (Hot Standby)

| Parameter | Value | Notes |
|-----------|-------|-------|
| Entry T | ≥ 350°F | RHR isolated |
| Entry P | < 2235 psig | Before normal pressure control |
| Criticality T | ≥ 551°F | Technical Specification minimum |
| Normal HZP T | 557°F | No-load Tavg |
| Normal HZP P | 2235 psig | Operating pressure |

---

## Interpolation Methods

### Linear Interpolation Between Points

```
Given two points: (T1, P1) and (T2, P2)
For temperature T where T1 < T < T2:

P(T) = P1 + (P2 - P1) × (T - T1) / (T2 - T1)
```

**Example:** At T = 127.5°F on heatup curve:
- Point 1: (125°F, 1113 psig)
- Point 2: (130°F, 1161 psig)
- P(127.5) = 1113 + (1161 - 1113) × (127.5 - 125) / (130 - 125)
- P(127.5) = 1113 + 48 × 0.5
- P(127.5) = 1137 psig

### Rate Selection for Cooldown

**Algorithm:**
1. Calculate current cooldown rate (dT/dt)
2. Select most restrictive applicable curve:
   - If |rate| ≤ 25°F/hr → Use 25°F/hr curve
   - If 25°F/hr < |rate| ≤ 50°F/hr → Use 50°F/hr curve
   - If 50°F/hr < |rate| ≤ 100°F/hr → Use 100°F/hr curve
   - If |rate| > 100°F/hr → ALARM (exceeds limit)

**Conservative Approach:** When rate is changing or uncertain, use next higher rate curve.

---

## Testing and Validation

### Test Cases for P-T Limit Checker

**Test 1: Acceptable Point**
- Temperature: 150°F
- Pressure: 1000 psig
- Expected: Acceptable (heatup limit at 150°F is 1422 psig)

**Test 2: Approaching Limit**
- Temperature: 150°F
- Pressure: 1400 psig
- Expected: Caution (within 50 psi of 1422 psig limit)

**Test 3: Violating Limit**
- Temperature: 150°F
- Pressure: 1500 psig
- Expected: Alarm (exceeds 1422 psig limit)

**Test 4: LTOP Boundary**
- Temperature: 350°F
- Pressure: 800 psig
- Expected: Acceptable, LTOP should disarm

**Test 5: Criticality Limit**
- Temperature: 140°F
- Pressure: 1000 psig
- Expected: Acceptable for heatup, violates criticality limit (requires 957 psig minimum)

---

## References

1. **Braidwood Unit 1 PTLR Revision 9** (ML22293A006)
   - Source: NRC ADAMS Public Documents
   - Primary data source for P-T limits
   - 57 EFPY limits (conservative)

2. **Vogtle Unit 1 PTLR Revision 5** (ML14112A519)
   - Alternative reference plant
   - 36 EFPY limits

3. **10 CFR 50, Appendix G** — "Fracture Toughness Requirements"
   - Federal regulation
   - URL: https://www.nrc.gov/reading-rm/doc-collections/cfr/part050/part050-appg.html

4. **ASME Boiler and Pressure Vessel Code, Section XI, Appendix G**
   - "Fracture Toughness Criteria for Protection Against Failure"
   - Industry standard methodology

5. **Regulatory Guide 1.99, Revision 2** — "Radiation Embrittlement of Reactor Vessel Materials"
   - NRC guidance document
   - URL: https://www.nrc.gov/reading-rm/doc-collections/reg-guides/

6. **WCAP-14040-A, Revision 4** — "Methodology Used to Develop Cold Overpressure Mitigating System Setpoints and RCS Heatup and Cooldown Limit Curves"
   - Westinghouse methodology
   - NRC approved

7. **NRC HRTD Section 3.2** — Reactor Coolant System (ML11223A213)
   - Generic Westinghouse P-T limit curve figures
   - Background on heatup/cooldown limits

8. **NRC HRTD Section 19.0** — Plant Operations (ML11223A342)
   - Heatup/cooldown procedures
   - Mode transition requirements

---

*Document created 2026-02-15*  
*Ready for simulator implementation*  
*Priority: HIGH (required for Mode 4 → Mode 3)*
