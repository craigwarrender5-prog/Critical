# PZR/CVCS Documentation Session — Final Summary

**Date:** 2026-02-15  
**Sessions:** 3 major documentation sessions completed

---

## Complete Documentation Package Created

### Session 1: Pressurizer Specifications
**Documents Created:**
1. `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md` (83 pages)
2. `Westinghouse_4Loop_Pressurizer_Specifications_Summary.md`

**Key Data Retrieved:**
- Pressurizer volume: 1800 ft³
- Heater capacity: 1794 kW (414 kW proportional + 1380 kW backup)
- Spray capacity: 840 gpm
- Complete control setpoints and interlocks
- RCP seal design
- Steam generator specifications

### Session 2: P-T Limits and Steam Tables
**Documents Created:**
1. `RCS_PT_Limits_and_Steam_Tables_Reference.md`
2. `RCS_Pressure_Temperature_Limit_Curves_Implementation.md`

**Key Data Retrieved:**
- Braidwood Unit 1 PTLR (57 EFPY) - complete heatup/cooldown curves
- LTOP/COPS PORV setpoints
- Implementation algorithms and test cases
- Mode transition requirements (Mode 5 → Mode 4 → Mode 3)

**Status Assessment:**
- ✅ Steam tables: Fully implemented with NIST validation
- ✅ P-T limit data: Complete numerical tables ready for implementation

### Session 3: CVCS System Documentation
**Documents Created:**
1. `NRC_HRTD_Section_4.1_Chemical_Volume_Control_System.md`

**Key Data Retrieved:**
- Complete system description and flow paths
- Normal flow balance (87 gpm total, 75 gpm letdown)
- Letdown orifice specifications and calculations
- Charging pump control logic
- RCP seal injection/return flows
- Reactor makeup system (5 operating modes)
- Boric acid system and emergency boration
- VCT functions and level control
- Boron recovery system
- All interlocks and automatic actions

---

## Documentation Now Available

### Pressurizer (PZR) Documentation

**Physical Design:**
- Volume, dimensions, materials
- Heater banks (proportional and backup)
- Spray system specifications
- PORV and code safety valves
- Surge line and instrumentation

**Control Systems:**
- Pressure control (PID controller with heaters/spray)
- Level control (PI controller with charging flow)
- Setpoints, deadbands, and hysteresis
- Interlocks and protection logic

**Operations:**
- Heatup pressurization sequences
- Bubble formation procedures
- Normal at-power operation
- Transient response (insurge/outsurge)

### CVCS Documentation

**System Functions:**
- Boration/dilution capability
- RCS inventory control
- RCP seal water supply
- Chemical addition (LiOH, H₂, hydrazine)
- Purification (demineralizers)
- Emergency core cooling (charging pumps as HHSI)
- Boron recovery

**Major Components:**
- Letdown path (orifices, heat exchangers, demineralizers)
- Volume control tank (VCT)
- Charging pumps and flow control
- Reactor makeup system
- Excess letdown
- RCP seal injection/return

**Flow Balances:**
- Normal steady-state (75 gpm letdown = 75 gpm return)
- Charging pump splits (55 gpm charging + 32 gpm seal injection)
- Seal return (20 gpm to RCS + 12 gpm to CVCS)

**Control Logic:**
- Pressurizer level → Charging flow (FCV-121)
- VCT level → Makeup system (automatic mode)
- Emergency boration paths

### P-T Limits Documentation

**Heatup/Cooldown Curves:**
- Complete numerical data tables
- Multiple cooldown rates (steady-state, 25, 50, 100°F/hr)
- Criticality limit curves
- Leak test limits

**LTOP/COPS:**
- Temperature-dependent PORV setpoints
- Enable temperature (≤ 350°F)
- Two-PORV configuration

**Mode Transitions:**
- Mode 5 → Mode 4: RCPs start at 400-425 psig
- Mode 4 → Mode 3: RHR isolation at ≥ 350°F, ≤ 425 psig
- Criticality: Minimum 551°F

### Steam Tables

**Implementation Status:**
- ✅ NIST-validated (1-3200 psia, 100-700°F)
- ✅ Saturation properties
- ✅ Two-phase flow correlations
- ✅ Built-in validation functions

---

## Cross-Reference Matrix

### Documents Already Present (Referenced)
- `NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md`
- `NRC_HRTD_Section_10.3_Pressurizer_Level_Control.md`
- `NRC_HRTD_Startup_Pressurization_Reference.md`
- `PZR_Level_Pressure_Deficit_Analysis_v4.4.0.md`
- `WaterProperties.cs` (NIST steam tables)
- `SteamThermodynamics.cs` (two-phase calculations)

### New Documents Created (This Session)
- `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md`
- `Westinghouse_4Loop_Pressurizer_Specifications_Summary.md`
- `RCS_PT_Limits_and_Steam_Tables_Reference.md`
- `RCS_Pressure_Temperature_Limit_Curves_Implementation.md`
- `NRC_HRTD_Section_4.1_Chemical_Volume_Control_System.md`

### Coverage by Topic

**Pressurizer:**
- ✅ Physical specifications (Section 3.2, Summary)
- ✅ Pressure control (Section 10.2)
- ✅ Level control (Section 10.3)
- ✅ Startup operations (Startup Reference)
- ✅ Implementation analysis (v4.4.0 Analysis)

**CVCS:**
- ✅ Complete system description (Section 4.1) **NEW**
- ✅ Integration with PZR level control (Section 10.3)
- ✅ Startup pressurization operations (Startup Reference)

**P-T Limits:**
- ✅ Methodology and reference (P-T Reference) **NEW**
- ✅ Implementation data tables (P-T Implementation) **NEW**
- ✅ Mode transition requirements (P-T Implementation) **NEW**

**Steam Tables:**
- ✅ Implementation in code (WaterProperties.cs, SteamThermodynamics.cs)
- ✅ Reference data and benchmarks (P-T Reference) **NEW**

---

## Key Numbers Quick Reference

### Pressurizer
- Volume: 1800 ft³
- Heaters: 1794 kW total
- Spray: 840 gpm max
- Normal level: 60% at power (25% at no-load)
- Normal pressure: 2235 psig

### CVCS Normal Operation
- Letdown: 75 gpm (one orifice at 2235 psig)
- Charging: 87 gpm total
- To RCS: 75 gpm (55 charging + 20 seal return)
- Seal injection: 32 gpm (8 gpm × 4 RCPs)
- Seal leakoff: 12 gpm (3 gpm × 4 RCPs)

### P-T Limits
- Maximum heatup rate: 100°F/hr
- Maximum cooldown rate: 100°F/hr
- Minimum boltup temperature: 60°F
- RHR isolation: ≥ 350°F, ≤ 425 psig
- LTOP enable: ≤ 350°F

### Boric Acid System
- Tank capacity: 24,228 gallons each (2 tanks)
- Concentration: ~4 wt% (7000 ppm)
- Tech Spec minimum: 15,900 gallons @ 7000 ppm
- Emergency boration: Via MO-8104

---

## Outstanding Questions: NONE

All requested PZR/CVCS documentation has been retrieved and formatted for simulator implementation. No gaps identified.

---

## Implementation Priority

**HIGH PRIORITY (Mode 4 → Mode 3):**
1. P-T limit checking module
2. LTOP PORV setpoint curves
3. Mode transition interlocks

**MEDIUM PRIORITY (Phase 0 completion):**
1. CVCS flow balance verification
2. Letdown orifice calculations
3. VCT level control logic
4. Charging flow control (FCV-121)

**LOWER PRIORITY (Future phases):**
1. Reactor makeup system modes
2. Boration/dilution calculations
3. Emergency boration paths
4. Boron recovery system

---

## Files Created This Session

### Main Documentation Files (5 total)
1. `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md` — Comprehensive RCS/PZR specifications
2. `Westinghouse_4Loop_Pressurizer_Specifications_Summary.md` — Quick reference
3. `RCS_PT_Limits_and_Steam_Tables_Reference.md` — Status assessment and methodology
4. `RCS_Pressure_Temperature_Limit_Curves_Implementation.md` — Complete numerical data for implementation
5. `NRC_HRTD_Section_4.1_Chemical_Volume_Control_System.md` — Complete CVCS documentation

### Archive/Summary Files (3 total)
1. `Archive/Pressurizer_Documentation_Research_Summary_2026-02-15.md`
2. `Archive/PT_Limits_Steam_Tables_Assessment_Summary_2026-02-15.md`
3. `Archive/PT_Limits_Documentation_Session_Summary_2026-02-15.md`

---

## Total Documentation: COMPREHENSIVE

**Coverage:** Complete documentation for:
- ✅ Pressurizer physical design and specifications
- ✅ Pressurizer control systems (pressure and level)
- ✅ CVCS system description and flow paths
- ✅ CVCS component specifications and interlocks
- ✅ RCP seal injection and return systems
- ✅ Reactor makeup and boration systems
- ✅ P-T limit curves for all operating modes
- ✅ LTOP/COPS PORV setpoints
- ✅ Steam tables (already implemented)
- ✅ Mode transition requirements (Mode 5 → 4 → 3)

**Status:** Ready for implementation

**No additional PZR/CVCS documentation needed** — all major systems fully documented with authoritative NRC sources.

---

*Documentation package complete 2026-02-15*
