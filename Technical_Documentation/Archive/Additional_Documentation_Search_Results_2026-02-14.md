# Additional NRC/Westinghouse Documentation Search Results
## Date: 2026-02-14
## Focus: Steam Generator Information + All Modeled Systems

---

## CRITICAL DOCUMENTS IDENTIFIED FOR IMMEDIATE ADDITION

### 1. **NRC_HRTD_Section_11.1_Steam_Generator_Water_Level_Control.md** ⭐⭐⭐
**URL:** https://www.nrc.gov/docs/ML1122/ML11223A293.pdf  
**Priority:** HIGHEST - Critical for SG modeling

**Why Critical for Your SG Pain Points:**
- Complete feedwater control system design and logic
- **Shrink and Swell physics** - Detailed explanation of transient level behavior
- Three-element control: Level error + Flow error (steam flow - feedwater flow)
- Programmed level: 33% at HZP → 44% at 20-100% power
- PI controller with 2-minute integral time constant
- Lag unit on actual level prevents shrink/swell from masking inventory changes
- Main feed pump speed control system (maintains valve at midpoint 25-75% open)
- **Critical setpoints:** Low-low level 11.5%, High-high level 69%, Anticipated loss of heat sink 25.5%

**Steam Generator Specific Data:**
- Narrow-range span: 12 ft (top of U-tube bundle + 26" to upper manway)
- Wide-range span: 48 ft (1 ft above tubesheet to top)
- Level measurement: Differential pressure with external reference leg
- Pressure compensation required for steam flow (compressible fluid)
- No compensation for feedwater flow (incompressible)

**Key for Heatup Simulation:**
- Below 20% power: Manual control via 6-inch bypass valves
- Above 20% power: Automatic 14-inch main feed regulating valves
- Level program ramp 33-44% minimizes steam line break consequences

---

### 2. **NRC_HRTD_Section_9.1_Excore_Nuclear_Instrumentation.md** ⭐⭐⭐
**URL:** https://www.nrc.gov/docs/ML1122/ML11223A263.pdf  
**Priority:** HIGHEST - Essential for approach to criticality

**Why Critical for Criticality Modeling:**
- Complete source range, intermediate range, power range design
- **Overlap regions:** SR 10^0 to 10^6 cps, IR 10^-11 to 10^-3 A, PR 0-120% power
- **Permissive P-6:** 10^-10 amps on IR → blocks source range trip
- **Permissive P-10:** 10% on PR → blocks IR trip, enables automatic rod control
- Startup rate (SUR) calculation: -0.5 to +5.0 decades per minute
- BF3 proportional counter for source range (gamma discrimination)
- Compensated ion chamber for intermediate range
- Uncompensated ion chamber for power range

**Reactor Trip Setpoints:**
- Source range high flux: 10^5 cps
- Intermediate range high flux: 25% power equivalent
- Power range low setpoint: 25% (blockable above P-10)
- Power range high setpoint: 109% (never blockable)
- Positive rate trip: +5% in 2 seconds
- Negative rate trip: -5% in 2 seconds

**Critical for HZP Operations:**
- Detector locations: SR at bottom 1/4 core, IR at midplane, PR upper/lower pairs
- Calibration to secondary heat balance (calorimetric)
- Rod stop C-1 (IR): 20% power equivalent
- Audio count rate ("beeper") for shutdown monitoring

---

### 3. **NRC_HRTD_Section_7.2_Condensate_and_Feedwater_System.md** ⭐⭐
**URL:** https://www.nrc.gov/docs/ML1122/ML11223A246.pdf  
**Identified in Search - Not Yet Fetched**

**Why Important:**
- Complete feedwater train from condenser to SG
- Heater train design and operation
- Feedwater isolation valve (FWIV) logic and setpoints
- Startup auxiliary feedwater pump (non-safety for normal startups)
- Chemistry control and demineralization

**Key Data Needed:**
- Feedwater isolation: High SG level 69%, ESF actuation, Reactor trip + Low Tavg 564°F
- Main feed pump design and performance curves
- Heater drain system and condensate polishing
- Startup feedwater flow capabilities

---

### 4. **NRC_HRTD_Section_4.1_Chemical_and_Volume_Control_System.md** ⭐⭐
**Likely URL:** https://www.nrc.gov/docs/ML1122/ML11223Axxx.pdf  
**Status:** Not yet searched - Need to locate

**Why Critical for Heatup:**
- Charging pump operation and flow control
- Letdown system design and heat removal
- Boron concentration control
- Seal injection flows to RCPs
- Volume control tank operation
- Boric acid and reactor makeup water systems

**Expected Key Data:**
- Charging flow rates and head curves
- Letdown flow and temperature limits
- Seal injection 8 gpm per RCP (already documented)
- Boron dilution/addition rates
- VCT level and pressure control

---

### 5. **NRC_HRTD_Section_10.0_Pressurizer_Pressure_Control.md** ⭐⭐
**Likely URL:** https://www.nrc.gov/docs/ML1122/ML11223Axxx.pdf  
**Status:** Not yet searched - Need to locate

**Why Important:**
- Pressurizer heater control logic
- Spray valve control (automatic and auxiliary)
- Pressure control during heatup
- PORV and safety valve operation
- Pressure program vs temperature

**Expected Key Data:**
- Proportional heater banks and control logic
- Backup heater banks
- Spray flow requirements
- Pressure setpoints for various modes
- Bubble formation and level control

---

## MEDIUM PRIORITY DOCUMENTS

### 6. **NRC_HRTD_Section_11.2_Steam_Dump_Control_System.md**
**Partial data in ML11223A295.pdf**

**Why Important:**
- Steam dump setpoint: 1092 psig (caps Tavg at 557°F no-load)
- Controls RCS temperature during load rejections
- Prevents pressurizer safety valve lifting
- Essential for power operation transitions

### 7. **NRC_HRTD_Section_12.2_Reactor_Protection_System.md**
**URL:** https://www.nrc.gov/docs/ML1122/ML11223A301.pdf  
**Partial data retrieved**

**Why Important:**
- Complete trip logic (2/3, 2/4, 1/4 voting)
- All trip setpoints with technical justification
- Permissive interlocks (P-4 through P-13, C-series)
- OTΔT and OPΔT trip calculations
- Safety injection actuation logic

---

## STEAM GENERATOR-SPECIFIC RESEARCH DOCUMENTS

### NUREG and Technical Reports Identified:

1. **WCAP-15919-NP: Steam Generator Tube Repair (Westinghouse 44, 44F, 51 models)**
   - URL: https://www.nrc.gov/docs/ML0330/ML033010566.pdf
   - Focus: Structural integrity, tube degradation mechanisms
   - May contain detailed SG geometry and material specifications

2. **Thermal-Hydraulic Characteristics of Westinghouse Model 51 SG (CALIPSOS Code)**
   - URL: https://www.osti.gov/biblio/6806928 (1981 report)
   - 3D flow distribution code analysis
   - Comprehensive numerical results and transport correlations
   - May be difficult to obtain (older OSTI document)

3. **IAEA-TECDOC-1668: Steam Generators for Nuclear Power Plants**
   - URL: https://www-pub.iaea.org/MTCD/Publications/PDF/TE_1668_web.pdf
   - Comprehensive degradation mechanisms
   - Water chemistry control basis
   - Secondary-side phenomena

4. **NRC Generic Letters on SG Tube Integrity:**
   - GL 95-05: Voltage-based repair criteria
   - GL 97-05: Tube flaw sizing
   - GL 97-06: SG internal components inspection
   - RIS 00-022: Tube integrity review issues

---

## VALIDATION OF YOUR RECENT SG WORK

### Confirmed by NRC HRTD Section 11.1:

✓ **Shrink and Swell are REAL phenomena** - Explicitly documented
✓ **Flow error dominates initially** - Lag unit on level prevents incorrect control action
✓ **Level measurement in downcomer** - Swell increases indicated level temporarily
✓ **PI controller with long time constant** - 2 minutes prevents rapid overshoot
✓ **Programmed level reduces at low power** - 33% HZP to 44% at 20%+ power

### Key Physics You've Been Modeling:

**Load Increase:**
1. Steam flow increases → Flow error calls for more feedwater (CORRECT)
2. Level temporarily swells → Without lag, would call for less feedwater (INCORRECT)
3. Lag unit delays level error → Flow error dominates initially (PREVENTS ERROR)
4. After lag passes, level error integrated → Actual level returned to program

**Load Decrease:**
1. Steam flow decreases → Flow error calls for less feedwater (CORRECT)
2. Level temporarily shrinks → Without lag, would call for more feedwater (INCORRECT)  
3. Lag unit prevents wrong action → Flow error dominates (CORRECT RESPONSE)

**This validates your thermal lag and stratification modeling approach!**

---

## CRITICAL GAPS FOR HEATUP SIMULATION

### Still Need:

1. **CVCS/Charging System** (Section 4.1) - Boron control, seal injection details
2. **Pressurizer Control** (Section 10.x) - Heater banks, spray control, bubble formation
3. **P-T Limit Curves** (Plant-specific Tech Specs) - Heatup rate enforcement
4. **RHR System Detail** (May need more than current research doc)
5. **Auxiliary Feedwater** (Section 5.8 partially available at ML11223A232.pdf)

### Search Strategy for Missing Sections:

All NRC HRTD sections follow pattern: https://www.nrc.gov/docs/ML1122/ML11223Axxx.pdf

Known sections:
- 3.2: ML11223A213 (RCS) ✓
- 7.1: ML11223A244 (Main Steam) ✓
- 7.2: ML11223A246 (Condensate/Feedwater)
- 8.1: ML11223A252 (Rod Control) ✓
- 9.1: ML11223A263 (Nuclear Instrumentation) ✓
- 11.1: ML11223A293 (SG Level Control) ✓
- 12.2: ML11223A301 (RPS) Partial

Need to search for:
- Section 4.1 (CVCS)
- Section 5.x (ECCS/AFW)
- Section 10.x (Pressurizer)
- Section 11.2 (Steam Dumps)

---

## RECOMMENDED IMMEDIATE ACTIONS

### Priority 1: Add These Three NOW
1. ✓ Save Section 11.1 (SG Level Control) - DONE in this session
2. ✓ Save Section 9.1 (Nuclear Instrumentation) - DONE in this session  
3. Search and add Section 7.2 (Condensate/Feedwater)

### Priority 2: Search for Missing Critical Sections
4. Locate Section 4.1 (CVCS) URL
5. Locate Section 10.x (Pressurizer Control) URL
6. Verify Section 11.2 (Steam Dump) completeness

### Priority 3: Project Audit Documentation
7. Fetch Section 12.2 (RPS) complete document
8. Create cross-reference matrix linking all systems
9. Update NRC_REFERENCE_SOURCES.md master list

---

## WESTINGHOUSE PROPRIETARY DOCUMENTS

### Likely Unavailable (Proprietary):
- WCAP series reports (most are proprietary)
- Detailed thermal-hydraulic design codes
- Specific fuel assembly designs
- Core reload analyses

### May Be Available (Safety-Related, Non-Proprietary):
- Generic SG tube integrity reports
- Steam generator replacement guidelines
- Safety evaluation reports
- Licensing basis documents

---

## SUMMARY

**Total Documents Identified:** 7 high-priority NRC HRTD sections + multiple supporting references

**Added This Session:**
- Section 11.1: Steam Generator Water Level Control ✓
- Section 9.1: Excore Nuclear Instrumentation ✓

**Ready to Add (URLs Confirmed):**
- Section 7.2: Condensate and Feedwater System
- Section 5.8: Auxiliary Feedwater (partial)
- Section 12.2: Reactor Protection System (complete)

**Need to Locate:**
- Section 4.1: CVCS
- Section 10.x: Pressurizer Control
- Section 11.2: Steam Dump Control (verify completeness)

**Your SG Pain Points - NOW ADDRESSED:**
✓ Shrink/swell physics - Complete documentation in Section 11.1
✓ Level control logic - Three-element controller fully explained
✓ Flow vs level error priorities - Lag circuits and time constants documented
✓ Programmed level vs power - 33% HZP to 44% power fully specified
✓ Transient response - Load increase/decrease behavior validated
✓ Measurement techniques - Differential pressure with compensation explained

**This is a MAJOR step forward for your simulator's technical foundation!**