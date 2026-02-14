# Technical Documentation Analysis and Additions - 2026-02-14

## Current State Analysis

### Existing Documentation Review

The Technical_Documentation folder currently contains 7 files:

1. **NRC_HRTD_Startup_Pressurization_Reference.md** - Comprehensive guide on RCS initial pressurization, post-bubble control philosophy, and startup sequence from Sections 4.1, 10.2, 10.3, 17.0, and 19.0

2. **NRC_REFERENCE_SOURCES.md** - Master list of all NRC HRTD sections retrieved and reviewed, including URLs and critical data extracted from each

3. **PZR_Level_Pressure_Deficit_Analysis_v4.4.0.md** - Analysis of pressurizer level and pressure deficit during specific implementation version

4. **RHR_SYSTEM_RESEARCH_v3.0.0.md** - Residual Heat Removal system research including design data, operation during heatup/cooldown, and throttling behavior

5. **SG_MODEL_RESEARCH_HANDOFF.md** - Research handoff documentation for steam generator modeling

6. **SG_Secondary_Pressurization_During_Heatup_Research.md** - Research on steam generator secondary side pressurization during plant heatup

7. **SG_THERMAL_MODEL_RESEARCH_v3.0.0.md** - Comprehensive physics research on SG thermal modeling including heat transfer, stratification, and thermocline descent

### Project Context

The simulator project "Critical: Master the Atom" is designed to model complete Westinghouse 4-Loop PWR operations from Cold Shutdown through Hot Zero Power stabilization and beyond. The current focus is on accurate physics modeling of the heatup sequence, particularly:

- RCS pressurization from solid plant to bubble formation
- Reactor coolant pump startup and heat addition
- Steam generator thermal lag and stratification
- Pressurizer level and pressure control systems
- Natural convection vs forced circulation transitions

---

## Documentation Additions - 2026-02-14

### 1. NRC_HRTD_Section_3.2_Reactor_Coolant_System.md

**Source:** NRC HRTD Section 3.2 (ML11223A213), Rev 1203

**Key Content:**
- Complete RCS design parameters (piping, pressurizer, steam generators, RCPs)
- Reactor coolant pump detailed design including seal assembly, thermal barrier, flywheel
- Pressurizer operating characteristics (heaters, spray, surge line, relief/safety valves)
- Steam generator thermal-hydraulic performance (heat transfer, shrink/swell, natural circulation)
- RCS instrumentation (temperature, pressure, flow, level)
- Heatup/cooldown P-T limits and design basis
- Natural circulation requirements and capabilities

**Critical Data Extracted:**
- RCP heat input: ~6 MW per pump in cold water (critical for heatup modeling)
- RCP minimum operating pressure: 400 psig (275 psid across seal #1)
- Seal injection flows: 8 gpm total (5 down, 3 up through seals)
- Pressurizer total volume: 1800 ft³ (60% water, 40% steam at full power)
- SG U-tubes: 3,388 per SG, 0.875" OD, recirculation ratio 3:1 to 5:1
- Steam pressure variation: ~150 psi decrease from no-load to full load
- Thermal design cycles: 200 heatup/cooldown cycles at <100°F/hr

**Simulator Implications:**
- RCP heat addition during heatup is a primary heat source (~24 MW total with 4 pumps)
- Cannot start RCPs below 400 psig RCS pressure (seal damage risk)
- Pressurizer spray uses cold leg velocity head via scoop design (important for spray effectiveness)
- SG thermal lag correctly modeled as function of massive secondary inventory
- P-T limits must be enforced to prevent brittle fracture

### 2. NRC_HRTD_Section_7.1_Main_Auxiliary_Steam.md

**Source:** NRC HRTD Section 7.1 (ML11223A244), Rev 0101

**Key Content:**
- Main steam system design from SG outlet to turbine
- Flow restrictors limiting blowdown to 9.68×10⁶ lb/hr per SG
- PORV design and setpoints (1125 psig nominal)
- Safety valve staggered setpoints (1170-1230 psig)
- AFW pump steam supplies with fail-open isolation valves
- MSIV isolation logic and closure characteristics
- Steam line instrumentation (flow, pressure, radiation)
- Auxiliary steam system for startup/shutdown
- Decay heat removal flowpaths with and without offsite power

**Critical Data Extracted:**
- Full power steam flow: 15.07×10⁶ lb/hr total (3.77×10⁶ lb/hr per SG)
- Steam conditions: 895 psig, 533.3°F saturated, >99.75% quality
- PORV capacity: ~10% per SG at no-load pressure
- Safety valve combined capacity: 16,467,380 lb/hr (109% of full-power flow)
- Steam line size: 28 inches
- Flow restrictor limits blowdown during rupture

**Simulator Implications:**
- Steam pressure builds naturally during heatup following saturation curve
- PORV setpoint at 1125 psig controls maximum steam pressure during heatup
- Nitrogen blanket isolated at ~220°F when steam forms
- Steam line warming required before full steam flow
- Decay heat removal requires functional PORVs or steam dumps + AFW

---

## Identified Gaps and Recommended Additional Documentation

### High Priority (Direct Impact on Current Heatup Simulation)

1. **NRC HRTD Section 8.0 - Rod Control System**
   - **Why:** Need control rod positioning during approach to criticality
   - **URL:** https://www.nrc.gov/docs/ML1122/ML11223A271.pdf
   - **Key Data Needed:** Control rod bank positioning, insertion limits, shutdown margin requirements

2. **NRC HRTD Section 9.0 - Excore Nuclear Instrumentation**
   - **Why:** Need source range, intermediate range, and power range detector behavior
   - **URL:** https://www.nrc.gov/docs/ML1122/ML11223A273.pdf
   - **Key Data Needed:** Detector ranges, count rate vs power, overlap regions, bistable setpoints

3. **Westinghouse PWR Pressure-Temperature Limit Curves (Plant-Specific)**
   - **Why:** Need actual P-T curves for heatup/cooldown rate enforcement
   - **Search:** Plant-specific technical specifications for a representative 4-loop Westinghouse plant
   - **Key Data Needed:** Heatup curves for various rates (25, 50, 75, 100°F/hr), cooldown curves, criticality limits

4. **NRC HRTD Section 11.2 - Steam Dump Control**
   - **Why:** Steam dump actuation at 1092 psig caps RCS T_avg at 557°F (no-load)
   - **URL:** https://www.nrc.gov/docs/ML1122/ML11223A295.pdf
   - **Key Data Needed:** Steam dump capacity, setpoints, control modes, interlock logic

### Medium Priority (Future Operational Phases)

5. **NRC HRTD Section 3.1 - Reactor Core and Vessel Construction**
   - **Why:** Need core geometry, fuel assembly design, neutronics parameters
   - **URL:** https://www.nrc.gov/docs/ML1122/ML11223A212.pdf
   - **Key Data Needed:** Core dimensions, fuel enrichment zones, control rod patterns

6. **NRC HRTD Section 5.3 - Auxiliary Feedwater System**
   - **Why:** AFW critical for decay heat removal and cooldown operations
   - **URL:** https://www.nrc.gov/docs/ML1122/ML11223A221.pdf
   - **Key Data Needed:** Pump capacities, actuation logic, steam supply requirements

7. **NRC HRTD Section 12.2 - Reactor Protection System**
   - **Why:** Trip logic, setpoints, and safety functions
   - **URL:** https://www.nrc.gov/docs/ML1122/ML11223A301.pdf
   - **Key Data Needed:** Trip setpoints, logic diagrams, permissive interlocks (P-7, P-11, etc.)

8. **NRC HRTD Section 7.2 - Condensate and Feedwater Systems**
   - **Why:** Feedwater temperature, flow control, and heater operation
   - **URL:** https://www.nrc.gov/docs/ML1122/ML11223A246.pdf
   - **Key Data Needed:** Main feedwater pump capacities, feedwater heater string, temperature rise

### Low Priority (Advanced Features)

9. **EPRI Steam Generator Reference Book**
   - **Why:** Advanced heat transfer correlations and tube bundle performance data
   - **Availability:** May require EPRI membership or purchase
   - **Key Data Needed:** Detailed tube bundle heat transfer coefficients, fouling factors, tube support effects

10. **Westinghouse NSSS System Description Document**
    - **Why:** Integrated system overview with operational sequences
    - **Availability:** May be plant-specific or proprietary
    - **Key Data Needed:** System integration, normal operating procedures, system interactions

---

## Documentation Organization Recommendations

### Current Structure Assessment

The Technical_Documentation folder is well-organized with:
- Topical research documents (RHR, SG thermal modeling)
- Consolidated reference sources (NRC_REFERENCE_SOURCES.md)
- Implementation-specific analyses (PZR level/pressure deficit)

### Recommended Enhancements

1. **Create Subdirectories:**
   ```
   Technical_Documentation/
   ├── NRC_HRTD_Sections/          # Full section documents
   ├── System_Research/             # Topical research (existing docs)
   ├── Implementation_Analysis/     # Version-specific analysis
   └── Reference_Data/              # Quick-reference tables, charts
   ```

2. **Add Quick Reference Documents:**
   - **System_Parameters_Quick_Reference.md** - Single-page table of all critical parameters
   - **Heatup_Sequence_Checklist.md** - Step-by-step operator actions during heatup
   - **Control_Setpoints_Master_List.md** - All control system setpoints in one place

3. **Create Cross-Reference Index:**
   - **Technical_Documentation_Index.md** - Searchable index of all documents with topic tags

---

## Next Steps

### Immediate Actions

1. ✅ **COMPLETED:** Added NRC_HRTD_Section_3.2_Reactor_Coolant_System.md
2. ✅ **COMPLETED:** Added NRC_HRTD_Section_7.1_Main_Auxiliary_Steam.md
3. **RECOMMENDED:** Fetch and add NRC HRTD Section 8.0 (Rod Control)
4. **RECOMMENDED:** Fetch and add NRC HRTD Section 9.0 (Excore Nuclear Instrumentation)
5. **RECOMMENDED:** Fetch and add NRC HRTD Section 11.2 (Steam Dump Control)

### Long-Term Maintenance

1. **Update NRC_REFERENCE_SOURCES.md** with newly added sections
2. **Create cross-links** between related documents
3. **Maintain version control** for implementation-specific analyses
4. **Archive superseded research** when models are updated

---

## Summary

The Technical_Documentation folder now contains comprehensive reference material for:
- Complete RCS design and operating characteristics
- Reactor coolant pump design including seal limitations
- Pressurizer thermal and hydraulic behavior
- Steam generator thermal performance
- Main steam system protection and control
- Decay heat removal capabilities

**Key Documents Added Today:**
1. NRC_HRTD_Section_3.2_Reactor_Coolant_System.md (comprehensive RCS reference)
2. NRC_HRTD_Section_7.1_Main_Auxiliary_Steam.md (steam system design and protection)

**Remaining High-Priority Gaps:**
- Rod control system (Section 8.0)
- Nuclear instrumentation (Section 9.0)
- Steam dump control (Section 11.2)
- Plant-specific P-T limit curves

The documentation is now substantially more complete and provides solid engineering basis for the current heatup simulation implementation and future operational phases of the simulator.