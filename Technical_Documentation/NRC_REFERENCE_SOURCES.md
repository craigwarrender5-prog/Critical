# NRC HRTD Reference Sources — Master Index

**Last Updated:** 2026-02-15  
**Purpose:** Master tracking document for all NRC HRTD sections and technical references used in Critical: Master the Atom simulator development.

---

## Primary Sources (Retrieved and Reviewed)

### 1. NRC HRTD Section 19.0 — Plant Operations (Westinghouse)
- **URL:** https://www.nrc.gov/docs/ML1122/ML11223A342.pdf
- **Revision:** Rev (varies by section)
- **Retrieved:** 2026-02-10
- **Status:** ✅ FULL TEXT RETRIEVED AND REVIEWED
- **Local Document:** N/A (referenced inline)
- **Content:** Complete cold shutdown to power operations procedure
- **Key sections:** 19.2 Plant Heatup, Appendix 19-1 Startup from Cold Shutdown
- **Critical data extracted:** 
  - Hold temperatures (160°F, 200°F, 350°F)
  - RHR operation during heatup
  - SG draining at 200°F
  - Steam formation at 220°F
  - RHR isolation at 350°F
  - 50°F/hr heatup rate
  - Steam dump at 1092 psig

### 2. NRC HRTD Section 2.3 — Steam Generators
- **URL:** https://www.nrc.gov/docs/ML1125/ML11251A016.pdf
- **Revision:** Rev (varies)
- **Retrieved:** 2026-02-10
- **Status:** ✅ FULL TEXT RETRIEVED AND REVIEWED
- **Local Document:** N/A
- **Content:** SG design, flow paths, operating characteristics, blowdown system, instrumentation
- **Critical data extracted:**
  - 8,519 U-tubes, 3/4" OD (Model F)
  - Recirculation ratio 4:1 to 33:1
  - Q = UA(T_avg - T_sat)
  - Blowdown 150 gpm
  - 2,350 gallon blowdown tank

### 3. NRC HRTD Section 3.2 — Reactor Coolant System
- **URL:** https://www.nrc.gov/docs/ML1122/ML11223A213.pdf
- **Revision:** Rev 1203
- **Retrieved:** 2026-02-14
- **Status:** ✅ FULL TEXT RETRIEVED AND REVIEWED
- **Local Document:** `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md`
- **Content:** Complete RCS design parameters, RCPs, pressurizer, SGs, instrumentation, P-T limits
- **Critical data extracted:**
  - RCP heat: ~6 MW per pump (cold water), ~4.5 MW (hot)
  - RCP minimum pressure: 400 psig (seal protection)
  - Seal injection: 8 gpm total per RCP
  - Pressurizer volume: 1,800 ft³ (60% water, 40% steam)
  - SG U-tubes: 3,388 per SG, 0.875" OD
  - Steam pressure variation: ~150 psi from no-load to full load
  - Design cycles: 200 heatup/cooldown at <100°F/hr

### 4. NRC HRTD Section 4.1 — Chemical and Volume Control System
- **URL:** https://www.nrc.gov/docs/ML1122/ML11223A214.pdf
- **Revision:** Rev (varies)
- **Retrieved:** 2026-02-11
- **Status:** ✅ FULL TEXT RETRIEVED AND REVIEWED
- **Local Document:** N/A (referenced in Startup_Pressurization_Reference.md)
- **Content:** Complete CVCS design, letdown path, charging, orifices, VCT, BRS, seal injection
- **Critical data extracted:**
  - Orifice lineup: 75+75+45 gpm
  - FCV-121 charging control
  - 87 gpm normal charging
  - 32 gpm seal injection
  - 120 gpm ion exchanger max
  - VCT divert valve LCV-112A
  - Excess letdown 20 gpm

### 5. NRC HRTD Section 5.1 — Residual Heat Removal System
- **URL:** https://www.nrc.gov/docs/ML1122/ML11223A219.pdf
- **Revision:** Rev (varies)
- **Retrieved:** 2026-02-10
- **Status:** ✅ FULL TEXT RETRIEVED AND REVIEWED
- **Local Document:** `RHR_SYSTEM_RESEARCH_v3.0.0.md`
- **Content:** RHR system design and operation
- **Critical data extracted:**
  - 2 trains (A/B), vertical centrifugal pumps
  - Shell & U-tube HX (RCS tube-side, CCW shell-side)
  - Suction valve interlock ≤425 psig
  - Auto-close at 585 psig
  - Design cooldown 350°F→140°F in 16 hours (both trains)
  - Min-flow bypass at 500 gpm
  - Solid plant ops via HCV-128 letdown
  - RHR pump heat ~0.5 MW each

### 6. NRC HRTD Section 5.7 — Generic Auxiliary Feedwater System
- **URL:** https://www.nrc.gov/docs/ML1122/ML11223A229.pdf
- **Revision:** Rev 0603
- **Retrieved:** 2026-02-14
- **Status:** ✅ FULL TEXT RETRIEVED AND REVIEWED
- **Local Document:** `NRC_HRTD_Section_5.7_Auxiliary_Feedwater_System.md`
- **Content:** AFW system design, pumps, water supplies, automatic start signals, PRA insights
- **Critical data extracted:**
  - 2 motor-driven pumps @ 440 gpm each, 1300 psig
  - 1 turbine-driven pump @ 880 gpm, 1200 psig
  - Steam turbine: 100-1275 psig range, 1100 HP
  - CST reserve: ~280,000 gallons
  - 5 automatic start signals
  - ESW backup on 2/3 low suction pressure

### 7. NRC HRTD Section 7.1 — Main and Auxiliary Steam Systems
- **URL:** https://www.nrc.gov/docs/ML1122/ML11223A244.pdf
- **Revision:** Rev 0101
- **Retrieved:** 2026-02-14
- **Status:** ✅ FULL TEXT RETRIEVED AND REVIEWED
- **Local Document:** `NRC_HRTD_Section_7.1_Main_Auxiliary_Steam.md`
- **Content:** Main steam system design, flow restrictors, PORVs, safety valves, MSIVs, AFW steam supply
- **Critical data extracted:**
  - Full power steam: 15.07×10⁶ lb/hr total (3.77×10⁶ per SG)
  - Steam conditions: 895 psig, 533.3°F, >99.75% quality
  - PORV setpoint: 1125 psig
  - Safety valves: 1170-1230 psig (staggered)
  - Flow restrictor: 9.68×10⁶ lb/hr limit per SG
  - MSIV closure: 5 seconds with 0.5s delay
  - AFW steam supply: 3" lines upstream of MSIVs

### 8. NRC HRTD Section 8.1 — Rod Control System
- **URL:** https://www.nrc.gov/docs/ML1122/ML11223A252.pdf
- **Revision:** Rev 0209
- **Retrieved:** 2026-02-14
- **Status:** ✅ FULL TEXT RETRIEVED AND REVIEWED
- **Local Document:** `NRC_HRTD_Section_8.1_Rod_Control_System.md`
- **Content:** CRDM design, automatic/manual control, bank sequencing, power/temp mismatch circuits
- **Critical data extracted:**
  - Rod step size: 5/8 inch
  - Max stepping rate: 72 steps/min = 45 in/min
  - Bank sequencing: A→B→C→D (withdrawal)
  - Overlap: 100 steps typical
  - Automatic control: 15-100% power only (P-13)
  - Rod speed: 8-72 steps/min based on error
  - Deadband: ±1.5°F with 0.5°F lock-up

### 9. NRC HRTD Section 9.1 — Excore Nuclear Instrumentation
- **URL:** https://www.nrc.gov/docs/ML1122/ML11223A263.pdf
- **Revision:** Rev 0403
- **Retrieved:** 2026-02-14
- **Status:** ✅ FULL TEXT RETRIEVED AND REVIEWED
- **Local Document:** `NRC_HRTD_Section_9.1_Excore_Nuclear_Instrumentation.md`
- **Content:** Source, intermediate, power range detectors; trips; permissives; calibration
- **Critical data extracted:**
  - SR: 10⁰-10⁶ cps, BF₃ proportional counter
  - IR: 10⁻¹¹-10⁻³ A, compensated ion chamber
  - PR: 0-120%, uncompensated ion chamber
  - P-6: 10⁻¹⁰ A (SR block permissive)
  - P-10: 10% power (nuclear at-power)
  - SR trip: 10⁵ cps, IR trip: 25%, PR trips: 25%/109%
  - Rate trips: ±5%/2 sec

### 10. NRC HRTD Section 10.2 — Pressurizer Pressure Control System
- **URL:** https://www.nrc.gov/docs/ML1122/ML11223A287.pdf
- **Revision:** Rev (varies)
- **Retrieved:** 2026-02-11
- **Status:** ✅ FULL TEXT RETRIEVED AND REVIEWED
- **Local Document:** N/A (referenced in Startup_Pressurization_Reference.md)
- **Content:** Master PID pressure controller, heater staging, spray valve control, PORV logic
- **Critical data extracted:**
  - PID setpoint: 2235 psig
  - Proportional heaters: 2220-2250 psig band
  - Backup heaters: ON at 2210, OFF at 2217 psig
  - Spray start: 2260 psig, full open: 2310 psig
  - PORV: 2335 psig
  - High pressure trip: 2385 psig

### 11. NRC HRTD Section 10.3 — Pressurizer Level Control System
- **URL:** https://www.nrc.gov/docs/ML1122/ML11223A290.pdf
- **Revision:** Rev (varies)
- **Retrieved:** 2026-02-11
- **Status:** ✅ FULL TEXT RETRIEVED AND REVIEWED
- **Local Document:** N/A (referenced in Startup_Pressurization_Reference.md)
- **Content:** Level program, charging flow control via FCV-121, letdown
- **Critical data extracted:**
  - Level program: 25% at 557°F to 61.5% at 584.7°F
  - PI controller varies charging only
  - Letdown CONSTANT at 75 gpm
  - Low level isolation: 17%
  - Backup heaters on at level +5%
  - High level trip: 92%

### 12. NRC HRTD Section 11.2 — Steam Dump Control System
- **URL:** https://www.nrc.gov/docs/ML1122/ML11223A294.pdf
- **Revision:** Rev 0403
- **Retrieved:** 2026-02-14
- **Status:** ✅ FULL TEXT RETRIEVED AND REVIEWED
- **Local Document:** `NRC_HRTD_Section_11.2_Steam_Dump_Control.md`
- **Content:** Steam pressure mode, T_avg mode, arming signals, interlocks, trip-open bistables
- **Critical data extracted:**
  - Steam pressure setpoint: 1092 psig → 557°F T_avg
  - 40% steam dump capacity (12 valves)
  - Loss-of-load controller: 5°F deadband
  - Turbine-trip controller: no deadband
  - C-7: loss-of-load arming (seals in)
  - C-8: turbine-trip arming
  - P-12: 553°F low-low T_avg interlock
  - Trip-open bistables: 10.7°F/16.4°F (loss-of-load), 13.8°F/27.7°F (turbine-trip)

### 13. NRC HRTD Section 12.2 — Reactor Protection System (Reactor Trip Signals)
- **URL:** https://www.nrc.gov/docs/ML1122/ML11223A301.pdf
- **Revision:** Rev 0109
- **Retrieved:** 2026-02-14
- **Status:** ✅ FULL TEXT RETRIEVED AND REVIEWED
- **Local Document:** `NRC_HRTD_Section_12.2_Reactor_Protection_System.md`
- **Content:** All reactor trips, OTΔT/OPΔT equations, permissives (P-4 to P-14), interlocks (C-1 to C-11)
- **Critical data extracted:**
  - SR trip: 10⁵ cps (1/2)
  - IR trip: 25% (1/2)
  - PR trips: 25% (2/4, blockable), 109% (2/4, always active)
  - Rate trips: ±5%/2 sec (seals in)
  - OTΔT/OPΔT: variable calculated setpoints
  - PZR low pressure: 1865 psig
  - PZR high pressure: 2385 psig
  - PZR high level: 92%
  - SG low-low level: 11.5%
  - All P-n permissives and C-n interlocks

### 14. NRC HRTD Section 7.2 — Condensate and Feedwater System
- **URL:** https://www.nrc.gov/docs/ML1122/ML11223A246.pdf
- **Revision:** Rev 0403
- **Retrieved:** 2026-02-15
- **Status:** ✅ FULL TEXT RETRIEVED AND REVIEWED
- **Local Document:** `NRC_HRTD_Section_7.2_Condensate_and_Feedwater_System.md`
- **Content:** Condensate system, main feedwater pumps, heater drain system, SG chemistry control
- **Critical data extracted:**
  - Condensate pumps: 2 × 70%, 11,000 gpm each, 1100 ft head
  - MFP: 2 × 70%, turbine-driven, 19,800 gpm each, 2020 ft head
  - LP heaters (5 stages): 120°F → 360°F
  - HP heaters (2 stages): 360°F → 440°F
  - High SG level trip/isolation: 69%
  - FW isolation on low T_avg (564°F) + P-4
  - MFP low suction pressure trip: 195 psig
  - CST minimum: 239,000 gallons

### 15. NRC HRTD Section 10.2 — Pressurizer Pressure Control System (Full)
- **URL:** https://www.nrc.gov/docs/ML1122/ML11223A287.pdf
- **Revision:** Rev 1208
- **Retrieved:** 2026-02-15
- **Status:** ✅ FULL TEXT RETRIEVED AND REVIEWED
- **Local Document:** `NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md`
- **Content:** Complete pressure control system with heater banks, spray valves, PORVs, COPS
- **Critical data extracted:**
  - Normal setpoint: 2235 psig (adjustable 1700-2500 psig)
  - Proportional heaters: 0% at 2250 psig, 100% at 2220 psig
  - Backup heaters: ON at 2210 psig, OFF at 2217 psig
  - Spray start: 2260 psig, full open: 2310 psig
  - PORV PCV-455A: Master controller at ~2335 psig
  - PORV PCV-456: Fixed bistable at 2335 psig
  - COPS PCV-455A: 425 psig (PT-403)
  - COPS PCV-456: 475 psig (PT-405)
  - COPS alarm: 400 psig
  - Low pressure SI: 1807 psig (2/3)
  - P-11 block: 1915 psig (2/3)

### 16. NRC HRTD Section 10.3 — Pressurizer Level Control System (Full)
- **URL:** https://www.nrc.gov/docs/ML1122/ML11223A290.pdf
- **Revision:** Rev 0502
- **Retrieved:** 2026-02-15
- **Status:** ✅ FULL TEXT RETRIEVED AND REVIEWED
- **Local Document:** `NRC_HRTD_Section_10.3_Pressurizer_Level_Control.md`
- **Content:** Complete level control system with level program, charging flow control
- **Critical data extracted:**
  - Level program: 25% at 557°F to 61.5% at 584.7°F
  - Program input: Auctioneered high T_avg
  - Master controller: PI, varies charging flow
  - Letdown constant at 75 gpm
  - Low level isolation: 17%
  - High level alarm: 70%
  - High level trip: 92% (2/3, at-power only P-7)
  - Backup heaters on at level > program + 5%
  - 4 level transmitters: 3 hot-calibrated, 1 cold-calibrated

### 17. NRC HRTD Section 12.3 — Engineered Safety Features Actuation
- **URL:** https://www.nrc.gov/docs/ML1122/ML11223A310.pdf
- **Revision:** Rev 0706
- **Retrieved:** 2026-02-15
- **Status:** ✅ FULL TEXT RETRIEVED AND REVIEWED
- **Local Document:** `NRC_HRTD_Section_12.3_ESFAS.md`
- **Content:** SI actuation, containment spray/isolation, steam line isolation, FW isolation, AFW actuation
- **Critical data extracted:**
  - Low PZR pressure SI: 1807 psig (2/3), blockable at P-11
  - High containment pressure SI: 3.5 psig (2/3)
  - High steam line ΔP SI: 100 psi (any line vs 2 others)
  - High steam flow + low pressure/T_avg SI: Variable/600 psig/553°F, blockable at P-12
  - Containment spray: 30 psig high-high (2/4)
  - Phase A isolation: Any SI actuation
  - Phase B isolation: 30 psig high-high (2/4)
  - Steam line isolation: 30 psig OR high flow combo
  - FW isolation: Low T_avg (564°F) + P-4, High SG level (69%), SI
  - AFW actuation: SI, Low-low SG level (11.5%), ESF bus UV (2560 V)
  - SI reset: 45-60 sec time delay + P-4 + manual

---

## Secondary Sources (Partially Reviewed)

### NRC Shutdown Risk Module
- **URL:** https://www.nrc.gov/docs/ML1216/ML12160A476.pdf
- **Status:** Partial review
- **Key data:** Plant Operating States (POS) definitions; POS 12-14 confirm SGs available above 350°F

### MDPI - Thermal Stratification in SG Isolated Loop
- **URL:** https://www.mdpi.com/1996-1073/15/21/8012
- **Status:** Partial review
- **Key finding:** Richardson number ~27,000 during stratification (strongly stable)

### IAEA TECDOC-1668 — Steam Generators
- **URL:** https://www-pub.iaea.org/MTCD/Publications/PDF/TE_1668_web.pdf
- **Status:** Reference only
- **Content:** Comprehensive SG ageing management, chemistry, blowdown, operations

### IntechOpen — SG Operation and Performance
- **URL:** https://www.intechopen.com/chapters/53747
- **Status:** Reference only
- **Content:** Mathematical correlations for SG heat transfer, RELAP5 modeling

### PCTRAN PWR Simulator
- **URL:** http://microsimtech.com/startup/default.html
- **Status:** Reference only
- **Key data:** 4 MW per RCP, >10 hrs heatup, RHR condition P<400 psig AND T_avg<350°F

---

## Local Technical Documentation

### Research Documents
| File | Content | Version |
|------|---------|---------|
| `RHR_SYSTEM_RESEARCH_v3.0.0.md` | Comprehensive RHR system modeling research | v3.0.0 |
| `SG_THERMAL_MODEL_RESEARCH_v3.0.0.md` | SG thermal physics, stratification, thermocline | v3.0.0 |
| `SG_MODEL_RESEARCH_HANDOFF.md` | SG modeling research handoff notes | N/A |
| `SG_Secondary_Pressurization_During_Heatup_Research.md` | SG pressurization research | N/A |

### Reference Compilations
| File | Content |
|------|---------|
| `NRC_HRTD_Startup_Pressurization_Reference.md` | Consolidated startup/pressurization from Sections 4.1, 10.2, 10.3, 17.0, 19.0 |
| `PZR_Level_Pressure_Deficit_Analysis_v4.4.0.md` | Implementation-specific analysis |

### NRC HRTD Section Documents
| File | Section |
|------|---------|
| `NRC_HRTD_Section_3.1_Reactor_Vessel_and_Internals.md` | 3.1 |
| `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md` | 3.2 |
| `NRC_HRTD_Section_5.1_Residual_Heat_Removal_System.md` | 5.1 |
| `NRC_HRTD_Section_5.7_Auxiliary_Feedwater_System.md` | 5.7 |
| `NRC_HRTD_Section_7.1_Main_Auxiliary_Steam.md` | 7.1 |
| `NRC_HRTD_Section_8.1_Rod_Control_System.md` | 8.1 |
| `NRC_HRTD_Section_9.1_Excore_Nuclear_Instrumentation.md` | 9.1 |
| `NRC_HRTD_Section_9.2_Incore_Instrumentation.md` | 9.2 |
| `NRC_HRTD_Section_10.1_Reactor_Coolant_Instrumentation.md` | 10.1 |
| `NRC_HRTD_Section_11.2_Steam_Dump_Control.md` | 11.2 |
| `NRC_HRTD_Section_12.2_Reactor_Protection_System.md` | 12.2 |
| `NRC_HRTD_Section_7.2_Condensate_and_Feedwater_System.md` | 7.2 |
| `NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md` | 10.2 |
| `NRC_HRTD_Section_10.3_Pressurizer_Level_Control.md` | 10.3 |
| `NRC_HRTD_Section_12.3_ESFAS.md` | 12.3 |

---

## Still Need to Fetch/Review

### High Priority
- [ ] **Plant-Specific P-T Limit Curves** — Technical specifications for heatup/cooldown rate enforcement

### Medium Priority
- [x] ~~**NRC HRTD Section 7.2** — Condensate and Feedwater System~~ (Retrieved 2026-02-15)
- [x] ~~**NRC HRTD Section 10.2** — Pressurizer Pressure Control System (full document)~~ (Retrieved 2026-02-15)
- [x] ~~**NRC HRTD Section 10.3** — Pressurizer Level Control System (full document)~~ (Retrieved 2026-02-15)
- [x] ~~**NRC HRTD Section 12.3** — Engineered Safety Features Actuation System (ESFAS)~~ (Retrieved 2026-02-15)

### Low Priority
- [ ] **EPRI Steam Generator Reference Book** — Advanced tube bundle heat transfer data
- [ ] **Westinghouse NSSS System Description Document** — Integrated system overview

---

## Document Maintenance

**Update Process:**
1. When new section retrieved, add entry to Primary Sources
2. Create local .md document in Technical_Documentation folder
3. Update "Local Technical Documentation" table
4. Remove from "Still Need to Fetch/Review" list
5. Update "Last Updated" date

**Archival:**
- Superseded research documents should be moved to Technical_Documentation/Archive/
- Keep version history in document headers

---
