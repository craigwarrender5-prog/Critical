# Pressurizer Documentation Research Session Summary

**Date:** 2026-02-15  
**Objective:** Retrieve authoritative NRC/Westinghouse documentation on Westinghouse 4-loop PWR pressurizer specifications, behavior, operations, and data.

---

## Documents Retrieved and Created

### 1. NRC_HRTD_Section_3.2_Reactor_Coolant_System.md
**Source:** NRC HRTD Section 3.2, Rev 1203 (ML11223A213)  
**Content:** Comprehensive reactor coolant system design with detailed pressurizer specifications
**Size:** 83 pages of technical data extracted

**Key Information:**
- Complete pressurizer design parameters (1800 ft³, 1794 kW heaters, 840 gpm spray)
- Heater bank breakdown: Bank C (414 kW proportional) + Banks A, B, D (1380 kW backup)
- Spray system specifications and driving force calculations
- PORV and code safety valve specifications with actuation logic
- Pressurizer relief tank (PRT) design and capacity
- Control system characteristics (PID pressure, PI level)
- All pressure control setpoints with hysteresis values
- Material construction details
- Design transient requirements
- RCP seal package design and cooling requirements
- Steam generator Model 51 specifications
- P-T limit curves for heatup/cooldown

### 2. Westinghouse_4Loop_Pressurizer_Specifications_Summary.md
**Purpose:** Quick reference document for simulator development  
**Content:** Condensed summary of critical pressurizer specifications

**Key Tables:**
- Physical characteristics (volume, dimensions, design pressure)
- Heater system breakdown by bank with control modes
- Spray system specifications
- Relief and safety valve configurations
- Normal operating conditions (water/steam volumes at various levels)
- Complete pressure control setpoint table with errors from 2235 psig setpoint
- COPS setpoints
- Level control setpoints with program equation
- Protection system setpoints
- Design transients and control responses
- Critical implementation notes for simulator
- Material construction

---

## Critical Specifications Retrieved

### Pressurizer Physical Design
| Specification | Value | Authority |
|--------------|-------|-----------|
| Total Volume | 1800 ft³ (50.96 m³) | NRC HRTD 3.2-2 |
| Height | 52 ft 9 in (16.1 m) | ScienceDirect/NRC |
| Diameter | 7 ft 8 in (2.3 m) | ScienceDirect/NRC |
| Design Pressure | 2500 psig | NRC HRTD 3.2-2 |
| Operating Pressure | 2235 psig | NRC HRTD 10.2 |
| Design Temperature | 680°F | NRC HRTD 3.2-2 |

### Heater System (Total: 1794 kW)
| Bank | Type | Heaters | Capacity | Control |
|------|------|---------|----------|---------|
| C | Proportional | 18 | 414 kW | Variable duty cycle (10-sec intervals) |
| A | Backup | 20 | 460 kW | Bistable (on/off) |
| B | Backup | 20 | 460 kW | Bistable (on/off) |
| D | Backup | 20 | 460 kW | Bistable (on/off) |

### Spray System
- Maximum Flow: 840 gpm (420 gpm per valve)
- Number of Valves: 2 (Loop 2 and Loop 3 cold legs)
- Continuous Bypass: 1 gpm per valve
- Actuation: Linear modulation 2260-2310 psig

### Relief and Safety Valves
**PORVs (2 total):**
- PCV-456: Fixed bistable at 2335 psig (Channel II or IV) + Interlock (Channel III)
- PCV-455A: Master controller output at ~2335 psig + Interlock (Channel IV)

**Code Safety Valves (3 total):**
- Set Pressure: 2485 psig
- Design Accumulation: +10% (max 2750 psig)

### Pressure Control Setpoints (from 2235 psig setpoint)
| Event | Pressure | Error | Action |
|-------|----------|-------|--------|
| Backup heaters ON | 2210 psig | -25 psi | All three banks |
| Backup heaters OFF | 2217 psig | -18 psi | 7 psi hysteresis |
| Proportional 100% | 2220 psig | -15 psi | Continuously on |
| Proportional 0% | 2250 psig | +15 psi | De-energized |
| Spray start | 2260 psig | +25 psi | Linear modulation begins |
| Spray full open | 2310 psig | +75 psi | Maximum 840 gpm |
| PORVs open | 2335 psig | +100 psi | With interlocks |
| High pressure trip | 2385 psig | +150 psi | 2/4 coincidence |
| Code safeties lift | 2485 psig | +250 psi | Final protection |

### Level Control Program
**Equation:**
```
Level_program(%) = 25 + [(T_avg - 557) / 27.7] × 36.5
```

**Key Setpoints:**
- No-load (557°F): 25%
- Full-power (584.7°F): 61.5%
- Low level isolation: 17%
- High level trip: 92% (2/3 coincidence, at-power)

---

## Integration with Existing Documentation

### Documents Already Present
These documents provided context and were cross-referenced:
- `NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md` — Pressure control system operation
- `NRC_HRTD_Section_10.3_Pressurizer_Level_Control.md` — Level control system operation
- `NRC_HRTD_Startup_Pressurization_Reference.md` — Consolidated startup/pressurization procedures
- `PZR_Level_Pressure_Deficit_Analysis_v4.4.0.md` — Implementation analysis

### New Information Added
The new Section 3.2 documentation provides:
1. **Physical specifications** that were previously implicit or estimated
2. **Heater bank breakdown** with specific power allocations
3. **Spray system details** including driving force calculations
4. **PORV interlock logic** with specific channel assignments
5. **Code safety valve specifications** with accumulation limits
6. **PRT design parameters** for relief discharge handling
7. **Material construction details** for all components
8. **Design transient requirements** that establish volume sizing rationale

### Resolved Questions
The documentation resolved several key questions:
1. **Exact heater capacity:** 1794 kW (not 1800 kW as approximated)
2. **Heater bank distribution:** 414 kW proportional + 1380 kW backup (3 banks × 460 kW)
3. **Spray capacity:** 840 gpm total (420 gpm per valve)
4. **Volume basis:** 1800 ft³ satisfies 6 specific design requirements for transient accommodation
5. **PORV logic:** Specific channel assignments and interlock requirements
6. **Control hysteresis:** 7 psi for backup heaters (2210 on, 2217 off)

---

## Key Findings for Simulator Development

### Must-Have Features Confirmed
1. **Saturation equilibrium:** Steam and water always at T_sat for given pressure
2. **Volume factor of 6:** Boiling 1 ft³ water → 6 ft³ steam
3. **PID pressure control:** Proportional + Integral + Derivative with master controller
4. **PI level control:** Proportional + Integral for charging flow
5. **10-second heater intervals:** Proportional heaters cycle within 10-sec windows
6. **Spray driving force:** ΔP = (P_hot_leg - P_cold_leg) + velocity head

### Physical Constraints Confirmed
1. **Total volume fixed:** 1800 ft³ maximum
2. **Heater uncovering:** Heaters must be submerged or damage occurs
3. **Going solid:** Level = 100% causes uncontrollable pressure rise
4. **Minimum steam volume:** Required for pressure control authority
5. **Surge line sizing:** Limits pressure drop during insurge

### Control Logic Confirmed
1. **Heater sequence:** Proportional modulates first → Backup banks at 2210 psig (all simultaneously)
2. **Spray sequence:** Linear modulation 2260-2310 psig → PORVs at 2335 psig if insufficient
3. **Level protection:** ALL heaters off at 17% (steam exposure protection)
4. **Anticipatory control:** Backup heaters on at Program + 5% level

---

## References Compiled

### Primary Sources
1. NRC HRTD Section 3.2 — Reactor Coolant System, Rev 1203 (ML11223A213)
   - URL: https://www.nrc.gov/docs/ML1122/ML11223A213.pdf
   - Retrieved: 2026-02-15

2. NRC HRTD Section 10.2 — Pressurizer Pressure Control, Rev 1208 (ML11223A287)
   - Previously retrieved

3. NRC HRTD Section 10.3 — Pressurizer Level Control, Rev 0502 (ML11223A290)
   - Previously retrieved

### Supporting References
4. ScienceDirect — "Pressuriser" Technical Topics
   - Confirmed physical dimensions and heater capacity

5. ASME Boiler and Pressure Vessel Code, Section III
   - Cited for design pressure and safety valve requirements

---

## Search Results Summary

### Successful Retrievals
- Full text of NRC HRTD Section 3.2 (83 pages)
- Pressurizer design parameters table
- Heater specifications and bank assignments
- Spray system specifications
- PORV and code safety valve data
- PRT specifications
- Control setpoints with hysteresis values
- Material construction details

### Authoritative Sources Confirmed
- NRC Human Resources Training Division (HRTD) manuals are the gold standard
- Westinghouse Technology Systems Manual is the vendor design reference
- All specifications cross-referenced between multiple NRC sections

### Information Gaps Addressed
- Exact heater power allocation by bank
- Spray flow capacity and valve count
- PORV channel assignments and interlock logic
- Code safety valve accumulation limits
- PRT volume and cooling capacity
- Design transient requirements

---

## Next Steps Recommendations

### For Simulator Implementation
1. Update `PlantConstants.Pressurizer.cs` with exact specifications:
   - Total volume: 1800 ft³
   - Heater capacity: 414 kW proportional + 1380 kW backup
   - Spray capacity: 840 gpm maximum

2. Implement heater bank control logic:
   - Bank C (proportional): Variable duty cycle in 10-sec intervals
   - Banks A, B, D (backup): Simultaneous bistable at 2210/2217 psig

3. Implement spray control:
   - Linear modulation between 2260-2310 psig
   - Continuous 1 gpm bypass per valve

4. Verify PORV logic:
   - PCV-456: Channel II or IV ≥ 2335 + Channel III interlock
   - PCV-455A: Channel IV ≥ 2335 + Master controller output

### For Documentation
1. Cross-reference new specifications with existing control system docs
2. Update any implementation analysis documents with confirmed values
3. Create test cases based on design transient requirements

---

## Files Created/Updated

### New Files
1. `Technical_Documentation/NRC_HRTD_Section_3.2_Reactor_Coolant_System.md`
   - 83-page comprehensive reference document
   - Tables 3.2-2 through 3.2-6 with design parameters
   - Complete component descriptions
   - Material specifications

2. `Technical_Documentation/Westinghouse_4Loop_Pressurizer_Specifications_Summary.md`
   - Quick reference for implementation
   - Condensed tables and equations
   - Critical design notes

### Updated Files
3. `Technical_Documentation/Technical_Documentation_Index.md`
   - Added new documents to inventory
   - Updated tags and cross-references
   - Added pressurizer design parameters to quick reference
   - Updated document count and recent changes section

---

## Conclusion

This research session successfully retrieved comprehensive, authoritative specifications for the Westinghouse 4-loop PWR pressurizer from NRC HRTD documentation. The specifications are now available in two formats:

1. **Comprehensive reference** (Section 3.2) for detailed engineering analysis
2. **Quick reference summary** for day-to-day implementation work

All specifications are traceable to authoritative NRC sources with revision numbers and retrieval dates documented. The information resolves all outstanding questions about pressurizer design parameters and provides a solid foundation for accurate simulator implementation.

---

**Session Completed:** 2026-02-15  
**Documentation Status:** Complete and integrated
