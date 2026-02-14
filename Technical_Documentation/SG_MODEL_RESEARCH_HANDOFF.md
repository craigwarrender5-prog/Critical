# SG Multi-Node Thermal Model - Research & Implementation Handoff

> **⚠️ STATUS:** This document may be superseded by `SG_THERMAL_MODEL_RESEARCH_v3.0.0.md`.
> Review that document for current thermal stratification physics and implementation guidance.
> This file retained for historical reference.

## Date: 2025-02-10
## Context: Continuation document for new conversation session

---

## 1. THE PROBLEM

The current SG multi-node thermal model (`SGMultiNodeThermal.cs`) produces unrealistic behavior during PWR heatup from cold shutdown:

- **Initial heatup rate**: ~40-47°F/hr (correct per NRC HRTD 19.2.2 target of ~50°F/hr)
- **After T≈12hr**: Rate crashes to 26°F/hr and locks there
- **Root cause**: SG absorbs 14-19 MW (should be 2-4 MW), creating thermodynamically impossible heat transfer
- **Symptom**: RCS-to-SG_top temperature gap locks at ~7°F (should be growing during heatup)
- **SG bulk heats at 28°F/hr** — impossible with 1.66M lb stagnant water and only U-bend convection

## 2. ROOT CAUSES IDENTIFIED (Previous Analysis)

### Error 1: Circulation Onset Model is Fundamentally Wrong
- Model triggers "natural circulation" when SG top-bottom ΔT exceeds 30°F
- **Reality**: Stratification IS the absence of circulation, not its trigger
- Hot water from U-bends rises and sits on top (stable stratification)
- No mechanism to drive downcomer flow (no cooling on shell side during subcooled heatup)
- True secondary natural circulation only develops when boiling begins (~220°F+ at SG secondary)

### Error 2: Effective Heat Transfer Area Vastly Overstated
- Model uses ~22,000 ft² effective for top node alone (all 4 SGs)
- Realistic U-bend area: ~14,120 ft² geometric, ~4,000-5,000 ft² effective (bundle penalty + stratification)
- Model is using **5× too much effective area**
- When circulation triggers, lower node areas also activate, making it worse

### Error 3: Inter-node Transport Too Aggressive
- Circulating UA ramps to 50,000 BTU/(hr·°F) — creates false mixing
- Stagnant UA of 500 BTU/(hr·°F) is reasonable for pure thermal diffusion
- Diffusion timescale for 21 ft water column: ~73,500 hours (effectively zero during 12-hr heatup)
- **Thermocline progression is the correct model** — slow downward progression of hot layer

## 3. KEY FILES (ALL GOLD STATUS)

```
C:\Users\craig\Projects\Critical\Assets\Scripts\Physics\SGMultiNodeThermal.cs
C:\Users\craig\Projects\Critical\Assets\Scripts\Physics\PlantConstants.SG.cs
C:\Users\craig\Projects\Critical\Assets\Scripts\Physics\HeatupSimEngine.cs
```

### Current Model Architecture (SGMultiNodeThermal.cs):
- 5-node stratification model (nodes 0-4, top to bottom)
- Node 0 (U-bend/upper): 25% area, 15% mass, 0.40 stagnant effectiveness
- Node 1 (upper-mid): 20% area, 20% mass, 0.15 stagnant effectiveness
- Node 2 (middle): 20% area, 25% mass, 0.05 stagnant effectiveness
- Node 3 (lower-mid): 20% area, 25% mass, 0.02 stagnant effectiveness
- Node 4 (bottom): 15% area, 15% mass, 0.01 stagnant effectiveness

### Key Constants (PlantConstants.SG.cs):
- `SG_MULTINODE_HTC_STAGNANT = 50` BTU/(hr·ft²·°F)
- `SG_CIRC_ONSET_DELTA_T_F = 30` (BROKEN — this concept is wrong)
- `SG_CIRC_FULL_DELTA_T_F = 80`
- `SG_CIRC_FULL_EFFECTIVENESS = 0.70`
- `INTERNODE_UA_STAGNANT = 500` BTU/(hr·°F)
- `INTERNODE_UA_CIRCULATING = 50000` BTU/(hr·°F) (BROKEN — way too high)

## 4. WHAT THE USER WANTS (EXPANDED SCOPE)

The user wants a **complete, physics-accurate SG thermal model** that includes:

### 4a. Realistic Thermocline/Stratification Physics
- U-bend convection heats only a thin top layer initially
- Hot cap grows slowly downward (thermocline descent)
- Must model the actual thermocline front velocity
- Lower tube regions only participate as thermocline reaches them
- Need proper thermodynamic formulas for stratified heat transfer in tube bundles

### 4b. RHR System Modeling
- During cold shutdown heatup, RHR provides the primary heat removal path
- RHR is aligned to RCS below 350°F (Mode 5→4 transition)
- At ~350°F, RHR is isolated and SGs take over as heat sink
- The user suspects the current model "pasted over" RHR with SG variables
- Need to model RHR bypass of SG during early heatup

### 4c. SG Blowdown System During Startup
- At ~200°F RCS temp, SG draining begins via normal blowdown system
- SG starts in "wet layup" (100% wide-range level, filled with water)
- Must drain to operating level (~33% ±5% narrow-range) during heatup
- Blowdown system: 150 gpm normal rate, through coolers and ion exchangers
- Blowdown also used for chemistry control during startup

### 4d. SG Pressurization During Heatup
- At ~220°F RCS temp, steam formation begins in SGs
- Nitrogen blanket (corrosion prevention) is isolated when steam forms
- MSIVs are opened to warm steam lines
- SG pressure builds with RCS temperature
- Steam dumps control at 1092 psig (Tsat = 557°F = no-load Tavg)
- **Must heat and pressurize SG simultaneously for realism**

### 4e. Operational Hold Points
- RCS held at ~160°F initially (cold water addition accident limit)
- RHR flow adjusted to maintain this hold
- After RCPs start and RHR stops, heatup proceeds at ~50°F/hr
- Mode 5→4 transition at 200°F (chemistry checks, hydrogen blanket)
- Mode 4→3 transition at 350°F (RHR isolated, ECCS aligned)
- Heatup terminates at 557°F (steam dumps control in pressure mode)

### 4f. Correct Temperature Profile Expectations
Per user's description of expected realistic behavior:
1. **Initially**: Large RCS temperature increase, slow SG increase (limited U-bend area)
2. **Mid-heatup**: Thermocline slowly descends, more tube area participates
3. **Transition**: Smaller RCS increase, larger SG increase as gap narrows
4. **Final**: Both settle at correct operating temperatures and pressures

## 5. RESEARCH COMPLETED (Key Findings from NRC HRTD Documents)

### From NRC HRTD Section 19.0 (Plant Operations) — SAVED IN FULL:
- **Initial conditions**: Cold shutdown Mode 5, RCS 150-160°F, 320-400 psig, SGs at wet layup (100%)
- **Steam bubble formation**: Pressurizer heated to 428-448°F, level reduced to 25%
- **RCP start**: After bubble drawn and pressure at 320 psig, start RCPs one at a time
- **Hold at 160°F**: RHR heat exchangers throttled to maintain temp while RCPs run
- **RHR stopped**: After all 4 RCPs running, RHR pumps stopped
- **Heatup rate**: ~50°F/hr with all 4 RCPs (RCP heat ~4 MW each = 16 MW total, plus decay heat)
- **At ~200°F**: SG draining commenced via blowdown, hydrogen blanket established, MSIVs opened
- **At ~220°F**: Steam formation in SGs, nitrogen isolated
- **At ~350°F**: RHR isolated from RCS, ECCS aligned, Mode 3 entered
- **At 1092 psig steam**: Steam dumps activate, RCS stabilizes at 557°F
- **At 2235 psig**: Pressurizer heaters/sprays go to auto
- **~30,000 gallons**: Drained from RCS during heatup (thermal expansion)

### From NRC HRTD Section 2.3 (Steam Generators):
- SG design: vertical shell, U-tube, 8,519 tubes per SG, 3/4" OD, 0.048" wall
- Tube material: Inconel, supported by eggcrate supports every 3 ft
- Secondary flow: downcomer → tubesheet → up through bundle → separators → dryers → steam outlet
- **Recirculation ratio**: 33:1 at 5% power, 4:1 at 100% power
- Heat transfer: Q = UA(Tavg - Tsat) — standard relationship
- **Blowdown system**: 150 gpm normal, through coolers (~120°F) and ion exchangers
- 2,350 gallon blowdown tank
- Surface and bottom blowdown connections

### From NRC Shutdown Risk Module:
- **POS 12**: "RCS heatup solid and draw bubble" — Mode 5
- **POS 13**: "RCS heatup to 350°F" — Mode 4 (RHR still available)
- **POS 14**: "RCS heatup with SGs available above 350°F" — Mode 2/Startup
- This confirms SGs don't meaningfully participate as heat exchangers until >350°F

### From PCTRAN Simulator Documentation:
- RCP heat input: ~4 MW per pump, 16 MW total for 4-loop
- RCS volume: >10,000 ft³
- Takes >10 hours to heat to operating temperature
- RHR start condition: RC pressure <400 psig AND Tavg <350°F

### From MDPI Research Paper on SG Thermal Stratification:
- Thermal stratification observed in isolated SG loops with cold water present
- Richardson number Ri ≈ 27,000 when stratification occurs (strongly stable)
- Confirms: stratification suppresses vertical mixing in tube bundle region
- Feedwater quickly heated to saturation during NORMAL operation (recirculating flow)
- But in stagnant conditions, convection insufficient to heat cold water at bottom

## 6. RESEARCH STILL NEEDED

Before writing the implementation plan, the following research should be completed:

### 6a. Thermocline Descent Rate in Tube Bundles
- How fast does the hot/cold interface move downward in a stagnant tube bundle?
- What are the governing equations? (Likely Richardson number dependent)
- What is the effective thermal diffusivity considering tube metal as conduction path?
- Consider: tubes act as "thermal wicks" conducting heat downward through metal

### 6b. SG Secondary Pressure During Subcooled Heatup
- What is the SG secondary pressure profile from cold (atmospheric) to steam formation at ~220°F?
- How does nitrogen blanket pressure evolve?
- When exactly does the transition from subcooled to saturated occur?

### 6c. Heat Transfer Correlations for Stagnant Tube Bundle
- Churchill-Chu gives h ≈ 160 BTU/(hr·ft²·°F) for isolated tube
- But tube bundle penalty (tight pitch, boundary layer overlap) reduces this 50-90%
- Need published data for natural convection in densely packed tube bundles
- Stratification further suppresses convection (warm fluid on top = stable)

### 6d. RHR System Parameters for Modeling
- RHR flow rate during heatup (typically 3000-4000 gpm per train)
- RHR heat exchanger capacity
- How RHR interfaces with RCS (suction from hot leg, return to cold leg)
- Control logic for RHR temperature regulation

### 6e. SG Draining Procedure Details
- From 100% wide-range (wet layup) to 33% narrow-range
- Volume of water removed (estimated: ~300,000-350,000 lb per SG from 100% to operating level)
- Timeline for draining relative to RCS temperature milestones
- Does draining affect thermal stratification? (removing cold water from bottom?)

## 7. WHAT THE IMPLEMENTATION PLAN MUST COVER

When ready to write the implementation plan, it needs to address:

1. **Thermocline model** replacing the broken circulation-onset model
2. **RHR system model** for the <350°F heatup phase
3. **SG secondary pressure model** (subcooled → saturated transition)
4. **SG draining model** (wet layup → operating level)
5. **Blowdown system integration** for chemistry and level control
6. **Revised heat transfer correlations** with proper effective area
7. **Mode transition logic** (Mode 5→4→3 with proper system handoffs)
8. **Operational hold points** (160°F, 200°F, 350°F milestones)

## 8. HEATUP LOG DATA (Reference)

Key data points from Build\HeatupLogs showing the problem:

| Time(hr) | T_RCS(°F) | T_SG_top(°F) | T_SG_bulk(°F) | SG_Heat(MW) | Strat_ΔT(°F) | Circ_Frac | Rate(°F/hr) |
|-----------|-----------|--------------|---------------|-------------|--------------|-----------|-------------|
| 10.50 | 112 | 105 | 101 | 2.0 | 4 | 0.000 | 28 |
| 11.00 | 129 | 115 | 104 | 4.1 | 15 | 0.000 | 38 |
| 11.50 | 151 | 132 | 109 | 6.3 | 31 | 0.001 | 47 ✅ |
| 12.00 | 164 | 155 | 122 | 18.7 | 44 | 0.186 | 12 ❌ |
| 12.50 | 175 | 168 | 136 | 14.4 | 43 | 0.152 | 26 |
| 13.00 | 188 | 181 | 150 | 14.6 | 43 | 0.151 | 26 |
| 13.50 | 201 | 194 | 163 | 14.5 | 43 | 0.148 | 26 |
| 14.25 | 220 | 214 | 183 | 14.5 | 42 | 0.143 | 26 |

**The break happens at T≈12hr** when stratification crosses 30°F threshold, triggering the broken circulation onset mechanism.

## 9. THERMODYNAMIC REFERENCE DATA

### SG Physical Parameters (per SG, 4 total):
- Tubes: 8,519 per SG (some refs say ~5,600 for Model F)
- Tube OD: 0.75" (3/4")
- Tube wall: 0.048"
- Tube pitch: 1.063" triangular
- Tube gap: 0.313" (8mm)
- Total tube length: ~46.7 ft average (hot leg + U-bend + cold leg)
- U-bend height: ~3 ft
- Straight leg: ~21 ft each side
- Heat transfer area: ~55,000 ft² per SG (some refs say 48,528 ft²)
- Secondary water mass: ~415,000 lb per SG at operating level
- Dry weight: ~800,000 lb (metal mass)
- SG height: ~62.4 ft (749 inches)

### RCS Parameters:
- Total RCS mass: ~700,000 lb (water)
- RCS volume: ~10,000+ ft³
- RCP heat: ~4 MW per pump (16 MW total with 4)
- Pressurizer heaters: ~1.8 MW total
- No-load Tavg: 557°F
- Operating pressure: 2235 psig
- Hot leg temp (full power): 599.4°F
- Cold leg temp (full power): 544.5°F

### Key Temperatures:
- Cold shutdown: <200°F (Mode 5)
- Hot shutdown: 200-350°F (Mode 4)
- Hot standby: >350°F (Mode 3)
- RHR isolation: 350°F
- Steam formation in SG: ~220°F
- No-load Tavg: 557°F
- Steam dump setpoint: 1092 psig (Tsat = 557°F)
- Criticality minimum: 551°F

## 10. PROJECT RULES REMINDER

- All files accessed from user's computer only
- GOLD modules cannot be altered without explicit approval + documentation
- Implementation plan REQUIRED before any changes
- Implementation proceeds ONE STAGE AT A TIME with user approval between stages
- Changelog in `Critical\Updates\Changelogs`
- Implementation plans in `Critical\Updates`
- Future features in `Critical\Updates\Future_Features`
- Technical documentation in `Critical\Technical_Documentation`
- Check `Future_Features` before any MAJOR update
- Use semantic versioning

## 11. PREVIOUS PATCH HISTORY

Latest completed patch: **v2.0.10**
- Stage 1: HeatupSimEngine.Logging.cs — State-based RCS mass in inventory audit
- Stage 2: CVCSController.cs + HeatupSimEngine.cs — Rate-limited heater control
- Stage 3: RCPSequencer.cs — RCP start delay fix
- Docs: CHANGELOG_v2.0.10.md, IMPLEMENTATION_PLAN_v2.0.10.md

Next version would be: **v2.1.0** (MAJOR - new SG thermal model architecture)

## 12. NEXT STEPS IN NEW CONVERSATION

1. **Read this document first**
2. **Read the GOLD source files** (SGMultiNodeThermal.cs, PlantConstants.SG.cs, HeatupSimEngine.cs)
3. **Complete remaining research** (Section 6 above)
4. **Check Future_Features** folder for any related planned work
5. **Draft Implementation Plan v2.1.0** with full staging
6. **Get user approval** before implementing anything
