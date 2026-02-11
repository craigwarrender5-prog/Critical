# NRC HRTD Reference Sources for SG Model Development

## Primary Sources (Retrieved and Reviewed)

### 1. NRC HRTD Section 19.0 — Plant Operations (Westinghouse)
- **URL**: https://www.nrc.gov/docs/ML1122/ML11223A342.pdf
- **Content**: Complete cold shutdown to power operations procedure
- **Key sections**: 19.2 Plant Heatup, Appendix 19-1 Startup from Cold Shutdown
- **Status**: FULL TEXT RETRIEVED AND REVIEWED
- **Critical data extracted**: Hold temperatures, RHR operation, SG draining at 200°F, steam formation at 220°F, RHR isolation at 350°F, 50°F/hr heatup rate, steam dump at 1092 psig

### 2. NRC HRTD Section 2.3 — Steam Generators
- **URL**: https://www.nrc.gov/docs/ML1125/ML11251A016.pdf
- **Content**: SG design, flow paths, operating characteristics, blowdown system, instrumentation, startup/shutdown operations
- **Status**: FULL TEXT RETRIEVED AND REVIEWED
- **Critical data**: 8,519 U-tubes, 3/4" OD, recirculation ratio 4:1 to 33:1, Q=UA(Tavg-Tsat), blowdown 150 gpm, 2350 gal tank

### 3. NRC HRTD Section 5.1 — Residual Heat Removal System
- **URL**: https://www.nrc.gov/docs/ml1122/ML11223A219.pdf
- **Content**: RHR system design and operation — 2 pumps, 2 HX (shell & U-tube), suction from Loop 4 hot leg, return to all cold legs
- **Status**: FULL TEXT RETRIEVED AND REVIEWED
- **Critical data extracted**: 2 trains (A/B), vertical centrifugal pumps with mechanical seals, shell & U-tube HX (RCS tube-side, CCW shell-side), suction valve interlock ≤425 psig, auto-close at 585 psig, design cooldown 350°F→140°F in 16 hours (both trains), HX designed for 20-hr post-shutdown heat load, min-flow bypass at 500 gpm, solid plant ops via HCV-128 letdown to CVCS, RHR pump heat ~0.5 MW each
- **Research document**: Technical_Documentation/RHR_SYSTEM_RESEARCH_v3.0.0.md

### 4. NRC Shutdown Risk Module
- **URL**: https://www.nrc.gov/docs/ML1216/ML12160A476.pdf
- **Content**: Plant Operating States (POS) definitions
- **Key data**: POS 12-14 confirm SGs available above 350°F, RHR used below 350°F

## Secondary Sources (Partially Reviewed)

### 5. MDPI - Thermal Stratification in SG Isolated Loop
- **URL**: https://www.mdpi.com/1996-1073/15/21/8012
- **Content**: Experimental study of thermal stratification in SG secondary side
- **Key finding**: Richardson number ~27,000 during stratification (strongly stable), confirms stratification suppresses mixing

### 6. IAEA TECDOC-1668 — Steam Generators
- **URL**: https://www-pub.iaea.org/MTCD/Publications/PDF/TE_1668_web.pdf
- **Content**: Comprehensive SG ageing management, includes chemistry, blowdown, operations

### 7. IntechOpen — SG Operation and Performance
- **URL**: https://www.intechopen.com/chapters/53747
- **Content**: Mathematical correlations for SG heat transfer, RELAP5 modeling

### 8. PCTRAN PWR Simulator
- **URL**: http://microsimtech.com/startup/default.html
- **Content**: Training simulator startup procedure
- **Key data**: 4 MW per RCP, >10 hrs heatup, RHR condition P<400 psig AND Tavg<350°F

### 9. NRC HRTD Section 4.1 — Chemical and Volume Control System
- **URL**: https://www.nrc.gov/docs/ML1122/ML11223A214.pdf
- **Content**: Complete CVCS design, letdown path, charging, orifices, VCT, BRS, seal injection
- **Status**: FULL TEXT RETRIEVED AND REVIEWED (2026-02-11)
- **Critical data extracted**: 75+75+45 gpm orifice lineup, FCV-121 charging control, 87 gpm normal charging, 32 gpm seal injection, 120 gpm ion exchanger max, VCT level divert valve LCV-112A, excess letdown 20 gpm

### 10. NRC HRTD Section 10.2 — Pressurizer Pressure Control System
- **URL**: https://www.nrc.gov/docs/ML1122/ML11223A287.pdf
- **Content**: Master PID pressure controller, heater staging, spray valve control, PORV logic
- **Status**: FULL TEXT RETRIEVED AND REVIEWED (2026-02-11)
- **Critical data extracted**: PID controller at 2235 psig setpoint, proportional heaters 2220-2250 psig band, backup heaters on at 2210 off at 2217, spray start at 2260 psig full open at 2310 psig, PORV at 2335 psig, high pressure trip at 2385 psig

### 11. NRC HRTD Section 10.3 — Pressurizer Level Control System
- **URL**: https://www.nrc.gov/docs/ML1122/ML11223A290.pdf
- **Content**: Level program, charging flow control via FCV-121, letdown is CONSTANT (75 gpm)
- **Status**: FULL TEXT RETRIEVED AND REVIEWED (2026-02-11)
- **Critical data extracted**: Level program 25% at 557°F to 61.5% at 584.7°F, PI controller varies charging only, low level isolation at 17%, backup heaters on at level+5%, high level trip at 92%

## Still Need to Fetch/Review

- ~~NRC HRTD Section 5.1 (RHR) — full text needed for RHR modeling~~ **DONE — Retrieved 2026-02-10**
- ~~NRC HRTD Section 4.1 (CVCS) — full text needed for CVCS modeling~~ **DONE — Retrieved 2026-02-11**
- ~~NRC HRTD Section 10.2 (PZR Pressure Control)~~ **DONE — Retrieved 2026-02-11**
- ~~NRC HRTD Section 10.3 (PZR Level Control)~~ **DONE — Retrieved 2026-02-11**
- NRC HRTD Section 3.2 (RCS) — P-T limits curves for heatup
- NRC HRTD Section 7.1 (Main and Auxiliary Steam) — steam line warming, MSIVs
- EPRI Steam Generation Reference Book — if available, for tube bundle heat transfer data
