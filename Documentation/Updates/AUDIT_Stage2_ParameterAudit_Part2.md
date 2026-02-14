# Stage 2 Parameter Audit — Part 2: Remaining Items #3–12

**Date:** 2026-02-06  
**Version:** v1.0.5.0  
**Auditor:** Claude (AI-assisted verification)  
**Scope:** WaterProperties.cs polynomials, empirical factors, PI gains, setpoints, relief valves

---

## Audit Summary

| Item | Module/Parameter | Status | Action |
|------|-----------------|--------|--------|
| #3 | WaterProperties polynomials | ✗ ERRORS → FIXED | 5 corrections applied |
| #4 | SYSTEM_DAMPING = 0.18 | ✓ ACCEPTABLE | Empirical, documented |
| #5 | PlantConstants (130+ values) | ✓ VERIFIED | (Part 1, v1.0.4.0) |
| #6 | CVCS PI gains (Kp=3.0, Ki=0.05) | ✓ ACCEPTABLE | Tuning parameters |
| #7 | VCT level setpoints | ✓ VERIFIED | Match NRC HRTD 4.1 |
| #8 | PlantConstantsHeatup | ✓ VERIFIED | (Part 1, v1.0.4.0) |
| #9 | AlarmManager setpoints | ✓ VERIFIED | Match NRC HRTD |
| #10 | SPRAY_EFFICIENCY/HEATER_TAU | ✓ ACCEPTABLE | Within industry ranges |
| #11 | Solid plant PI gains | ✓ ACCEPTABLE | Clamp enforced |
| #12 | Relief valve parameters | ✓ VERIFIED | Match NRC HRTD 19.2.1 |

**Overall: 10/12 items PASS, 2/12 previously verified (Part 1). Item #3 required fixes.**

---

## Item #3: WaterProperties.cs — NIST Steam Table Verification

### Errors Found and Corrected

| Function | Range | Max Error (Old) | Max Error (New) | Fix Applied |
|----------|-------|----------------|-----------------|-------------|
| SaturationTemperature | 1–14.7 psia | +18°F (8.6%) | ±0.04°F | Cubic refit |
| SaturationPressure | 100–212°F | 83% | <0.1% | Cubic refit |
| LatentHeat (low) | 1–800 psia | 5.1% | ±1% | Quadratic refit |
| LatentHeat (mid) | 800–2200 psia | N/A (new) | ±2.2% | New range |
| LatentHeat (high) | 2200–3200 psia | 49% | ±7% | Quadratic + clamp |
| WaterDensity | 500–653°F | 19% | ±2.3% | Cubic refit |
| WaterEnthalpy | Full range | 1.12% | (unchanged) | No fix needed |
| ValidateAgainstNIST | Test 4 target | 465 BTU/lb (WRONG) | 390 BTU/lb | Corrected |

### PWR Operating Point Validation (2250 psia)

| Property | NIST Value | Old Code | New Code |
|----------|-----------|----------|----------|
| Tsat(2250) | 652.7°F | 652.6°F | 652.6°F ✓ |
| Psat(653°F) | 2250 psia | 2251 psia | 2251 psia ✓ |
| hfg(2250) | 390.7 BTU/lb | 445 (+14%) | 397 (+1.8%) ✓ FIXED |
| hf(653°F) | 701 BTU/lb | 697 (-0.6%) | 697 (-0.6%) ✓ |
| ρ(588°F) | 44.4 lb/ft³ | 47.0 (+5.8%) | 44.0 (-0.8%) ✓ FIXED |

---

## Items #4, #6–12: Verification Details

### #4 SYSTEM_DAMPING = 0.18 — ✓ ACCEPTABLE
Empirical factor converting pure β/κ (~64 psi/°F) to realistic dP/dT (~10 psi/°F). Validated against LOFTRAN: ±50 psi for ±5°F Tavg.

### #6 CVCS PI Gains — ✓ ACCEPTABLE
Kp=3.0, Ki=0.05: 45 gpm max correction, 45-min recovery for 1% error, Ti=60s, anti-windup at 30 gpm.

### #7 VCT Setpoints — ✓ VERIFIED
Hi-Hi 90%, High/Divert 73%, Normal High 70%, Normal Low 40%, Makeup 25%, Low 17%, Lo-Lo 5%. All match NRC HRTD 4.1.

### #9 AlarmManager — ✓ VERIFIED
All 9 alarm setpoints consistent with NRC HRTD. PZR Level Low at 20% provides 3% margin above 17% isolation.

### #10 Spray/Heater — ✓ ACCEPTABLE
SPRAY_EFFICIENCY 0.85 (EPRI range 0.7-0.95), HEATER_TAU 20s (NUREG range 15-30s).

### #11 Solid Plant PI — ✓ ACCEPTABLE
Kp=0.5, Ki=0.02: Max 125 gpm command, clamped to 120 gpm by Update(). Ti=25s appropriate for thermal expansion.

### #12 Relief Valve — ✓ VERIFIED
450 psig setpoint (NRC HRTD 19.2.1), 20 psi accumulation (4.4%), 200 gpm capacity, 445 psig reseat.

---

## References

- NIST Chemistry WebBook (IAPWS-IF97): webbook.nist.gov
- NRC HRTD Rev 0408: Chapters 4.1, 10.2, 19.2.1
- EPRI NP-2827, NUREG/CR-3893, Westinghouse LOFTRAN
