# UPDATE v1.0.4.0 — Stage 2 Parameter Audit: NRC Setpoint Corrections

**Date:** 2026-02-06  
**Version:** 1.0.4.0  
**Type:** Parameter Corrections (Stage 2 Audit)  
**Backwards Compatible:** No — P_SPRAY_FULL changes from 2280→2310, P_TRIP_LOW changes from 1885→1865, PlantConstantsHeatup.NORMAL_OPERATING_PRESSURE changes from 2235→2250. Any code caching these values or comparing against hardcoded expected values will see different results.

---

## Summary

Stage 2 Parameter Audit verified all 130+ constants in PlantConstants.cs and PlantConstantsHeatup.cs line-by-line against NRC HRTD source documents. Found 2 incorrect pressure setpoints in PlantConstants.cs and 3 incorrect/misleading values in PlantConstantsHeatup.cs. All issues corrected.

---

## Root Cause

Values were originally set from secondary sources or approximations. NRC HRTD Section 10.2 (Pressurizer Pressure Control System, ML11223A287) provides the authoritative setpoint diagram (Figure 10.2-3) with exact values that differ from the original implementation.

---

## Files Changed

### PlantConstants.cs

**P_SPRAY_FULL: 2280 → 2310 psig**
- Old: 2280 psig (45 psig above setpoint — incorrect)
- New: 2310 psig (75 psig above setpoint)
- Source: NRC HRTD 10.2 — "2310 psig (75 psig above setpoint) for spray valves to fully open"
- Impact: Spray system now reaches full flow at a higher pressure, giving more margin before PORV actuation at 2335 psig. This creates a 25 psig gap (2310→2335) rather than the previous 55 psig gap (2280→2335), which is the correct 25 psig documented in the NRC setpoint diagram.

**P_TRIP_LOW: 1885 → 1865 psig**
- Old: 1885 psig (350 psig below setpoint)
- New: 1865 psig (370 psig below setpoint)
- Source: NRC HRTD 10.2.3.2 — "generated when pressurizer pressure decreases to 1865 psig"
- Impact: Low pressure trip now occurs 20 psig lower, providing slightly more margin against spurious trips during normal depressurization transients.

### PlantConstantsHeatup.cs

**MAX_HEATUP_RATE_F_HR → TYPICAL_HEATUP_RATE_F_HR (renamed, value unchanged at 50°F/hr)**
- The constant was named "MAX" but held the TYPICAL rate (50°F/hr)
- The actual Tech Spec maximum is 100°F/hr (PlantConstants.MAX_RCS_HEATUP_RATE_F_HR)
- Renamed to avoid confusion; value remains 50°F/hr

**MIN_RCP_PRESSURE_PSIA: 350 → 334.7 psia**
- Old: 350 psia (not NRC-documented)
- New: 334.7 psia (= 320 psig + 14.7)
- Source: NRC ML11223A342 19.2.2 — "pressure must be at least 320 psig"
- Now matches PlantConstants.MIN_RCP_PRESSURE_PSIA

**NORMAL_OPERATING_PRESSURE: 2235 → 2250 (psia)**
- Old: 2235 — this is the psig value, but the constant name and usage context implied psia
- New: 2250 psia (= 2235 psig + 14.7)
- Source: NRC HRTD 2.1 — "2250 psia"
- Now matches PlantConstants.P_NORMAL = 2250 psia
- Impact: GetTargetPressure() upper clamp now correctly limits to 2250 psia instead of 2235 psia

---

## Verification

- No other source files reference the changed constants by name (confirmed via project-wide search)
- PlantConstantsHeatup.cs is self-contained; no external references to renamed TYPICAL_HEATUP_RATE_F_HR
- All unchanged constants verified as correct against NRC source documents
- Self-validation methods (ValidateConstants()) unaffected — none check the changed setpoint values

---

## Full Audit Report

See: `AUDIT_Stage2_ParameterAudit_Part1.md` for complete line-by-line verification of all 130+ constants with NRC source document cross-references.

---

## NRC Source Documents Referenced

| Document | ADAMS # | Content |
|----------|---------|---------|
| HRTD 10.2 PZR Pressure Control | ML11223A287 | Authoritative setpoint diagram |
| HRTD 19.0 Plant Operations | ML11223A342 | Heatup limits, RCP pressure requirements |
| HRTD 2.1 PZR System | ML11251A014 | Operating pressure (2250 psia) |
| NRC IN 93-84 | — | RCP seal flow verification |
