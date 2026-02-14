# SG Heatup Physics — Critical Breakthrough & Handoff Summary

**Date:** 2026-02-11
**Context:** Continuation of Implementation Plan v5.0.0 — SG Secondary Heating Model
**For:** Fresh chat continuation
**Previous Transcripts:**
- `/mnt/transcripts/2026-02-11-15-04-57-sg-heatup-htc-energy-balance-analysis.txt`
- `/mnt/transcripts/2026-02-11-15-56-30-sg-heatup-physics-parameter-sweep.txt`

---

## 1. THE BREAKTHROUGH: We Were Modeling the Wrong Physics

After exhaustive parameter sweeps (~2000+ simulation runs across 6 model architectures), we discovered that the fundamental approach was wrong. **The SG secondary side during heatup is NOT a closed stagnant pool absorbing sensible heat.** It transitions to an **open system** where energy leaves as steam.

### What We Assumed (WRONG)
- SG secondary is a sealed vessel containing 1,660,000 lb of stagnant water
- RCP heat transfers through tubes, warming this massive water inventory
- The 1.76M BTU/°F secondary thermal mass creates an impossible energy balance
- With secondary mass 1.82× primary mass, T_secondary can only rise at ~10°F/hr while T_primary rises at 50°F/hr
- No parameter combination in ANY model (thermocline, mobilized mass, bounded cell, bulk) could simultaneously achieve both 45-55°F/hr heatup rate AND 20-40°F stratification

### What Actually Happens (FROM NRC HRTD 19.0 — CONFIRMED)
The heatup has **distinct thermodynamic phases**, and the secondary transitions from closed to open:

**Phase 1: Subcooled Heatup (100°F → ~220°F) — ~2.4 hours**
- SG secondary IS essentially a closed stagnant pool
- Only the top portion of tube bundle transfers heat effectively (thermocline model correct here)
- Steam formation begins at ~220°F RCS temperature
- This phase is SHORT — only ~120°F of the total ~450°F heatup
- SG draining begins at ~200°F via blowdown system (100% WR → 33% NR)
- N₂ blanket isolated when steam forms at ~220°F

**Phase 2: Saturated/Two-Phase (220°F → 557°F) — ~6.7 hours**
- Once secondary reaches local saturation, **boiling begins at the tube surface**
- MSIVs opened — steam admitted to steam lines for warming
- SG pressure builds naturally following saturation curve
- Energy goes into **latent heat of vaporization**, NOT sensible heating of bulk water
- Steam is vented through steam lines (warming turbine, condensing in condenser)
- Secondary temperature is now controlled by PRESSURE (T = T_sat at P_secondary)
- The massive 1.66M lb water inventory becomes largely irrelevant — you're boiling a thin film, not heating a swimming pool

**Phase 3: Steam Dump Control (at 1092 psig SG pressure)**
- Steam dumps actuate in steam pressure mode at 1092 psig (T_sat = 557°F)
- This TERMINATES the primary heatup — RCS stabilizes at 557°F no-load T_avg
- Any excess RCP heat is dumped as steam to the condenser
- Steam dumps remove excess energy that would drive RCS temperature higher

### Why This Changes Everything
The thermal mass problem that defeated every model only exists during Phase 1 (~2.4 hours). Once boiling starts, the secondary is an open system where:
- Q_sg → latent heat → steam → vented
- T_secondary tracks T_sat(P_secondary), NOT sensible temperature of bulk water
- P_secondary rises naturally as more heat is transferred
- The SG becomes a proper heat exchanger, not a thermal sink

**This is how Westinghouse reactors "overcome" the thermal mass ratio** — they don't try to heat 1.66M lb of water. They boil a thin layer and vent the steam.

---

## 2. WHAT THE MODEL NEEDS

The SG thermal model must handle THREE distinct regimes:

### Regime 1: Subcooled (T_secondary < T_sat at P_secondary)
- Current thermocline model is approximately correct for this phase
- Limited effective area, natural convection only
- Stratification develops (top hot, bottom cold)
- Duration: ~2.4 hours (100→220°F)
- The thermocline/5-node architecture works here
- Q_sg relatively small (1-4 MW), growing as ΔT increases

### Regime 2: Nucleate Boiling / Saturated
- Tube surfaces reach T_sat → nucleate boiling begins
- h_secondary jumps dramatically (natural convection ~50-150 → nucleate boiling ~2000-10,000 BTU/hr·ft²·°F)
- Energy transfer shifts from sensible heat to latent heat
- Q_sg = ṁ_steam × h_fg (steam production rate × latent heat)
- Secondary bulk temperature tracks T_sat(P_secondary)
- P_secondary rises as energy accumulates in steam space
- SG level drops as water boils off (makeup from condensate/feed system)
- Steam vented through open MSIVs to warm steam lines + condenser

### Regime 3: Steam Dump Controlled
- At P_steam = 1092 psig, steam dumps open in pressure mode
- T_sat(1092 psig) = 557°F = no-load T_avg
- Steam dumps modulate to hold pressure constant
- All excess RCP heat dumped to condenser as steam
- Primary temperature stabilizes at 557°F
- This is the steady-state "hot standby" condition

### Key Transitions
| Milestone | T_RCS | P_SG | Action |
|-----------|-------|------|--------|
| Steam formation | ~220°F | ~0 psig | N₂ isolated, first bubbles at tube surface |
| MSIVs open | ~220°F | ~0 psig | Steam admitted to lines for warming |
| SG draining | ~200°F | — | Blowdown to 33% NR from 100% WR |
| RHR isolated | 350°F | ~120 psig | ECCS aligned, Mode 3 entry |
| Accumulators open | — | 1000 psig (RCS) | Cold leg isolation valves opened |
| Steam dumps actuate | — | 1092 psig (SG) | Heatup terminated, T_avg held at 557°F |
| PZR auto control | — | 2235 psig (RCS) | Heaters/sprays in automatic |

---

## 3. PARAMETER SWEEP RESULTS (For Reference)

### What Was Tested
Six model architectures, each with comprehensive parameter sweeps:

1. **v1 — Mobilized Mass (wrong driving ΔT)**: Failed, entrainment never triggered
2. **v2 — Mobilized Mass (corrected)**: Full saturation, M_active → 100%
3. **v3 — Area Scaling with M_active**: Still saturates without de-entrainment
4. **v4 — Bounded Cell (entrainment + de-entrainment)**: Mass dynamics correct, energy balance impossible
5. **v5 — Bulk Temperature Model**: Revealed the fundamental thermal mass problem
6. **v6 — Full bounded cell with UA_leak**: Strat bounded at 35°F but rate stuck at 29°F/hr

### Why Every Model Failed
The fundamental energy balance constraint defeats ALL closed-system models:
- For 50°F/hr: Q_sg must be ~5.4 MW (from Q_rcp - Q_rcs_heatup - Q_losses)
- With 1.76M BTU/°F secondary thermal mass, T_sec rises at only ~10°F/hr
- Primary-secondary ΔT grows continuously to ~350°F by hour 9
- This ΔT drives Q_sg >> 5.4 MW once any significant tube area is engaged
- Reducing area → huge stratification; increasing mixing → rate crashes
- **No closed-system parameter set can satisfy both constraints simultaneously**

### The One Near-Miss (Current Code Architecture)
The existing 5-node thermocline model CAN approximate targets with:
- h_sec = 25-30 BTU/(hr·ft²·°F) (heavily penalized effective bundle HTC)
- Inter-node mixing UA = 500,000-750,000 BTU/(hr·°F) per SG when RCPs run
- Results: rate ≈ 44-47°F/hr, strat ≈ 25-45°F
- **But this is a calibration hack, not physics** — it only works because h_sec=25 is artificially low (Churchill-Chu gives 100-450 for this geometry)

---

## 4. RECOMMENDED IMPLEMENTATION APPROACH

### Option A: Phased Thermodynamic Model (RECOMMENDED)

Implement the three-regime model described in Section 2. This is the physically correct approach and solves the energy balance problem fundamentally.

**Key implementation elements:**

1. **Subcooled phase** (existing thermocline model, minor adjustments):
   - Keep 5-node architecture
   - h_sec ≈ 50-150 (Churchill-Chu with bundle penalty)
   - Thermocline limits effective area
   - Duration: only ~2 hours, so imperfect parameters are tolerable
   - Track when T_node[0] approaches T_sat(P_secondary)

2. **Boiling transition**:
   - When hottest node reaches T_sat → switch to nucleate boiling HTC
   - h_boiling ≈ 2,000-10,000 BTU/(hr·ft²·°F) (Jens-Lottes or Thom correlation)
   - Energy now goes to steam production: Q_sg = ṁ_steam × h_fg
   - Track steam production rate and SG pressure buildup
   - Secondary temperature = T_sat(P_secondary) for boiling nodes

3. **Pressure buildup**:
   - Steam accumulates in SG dome (above water level)
   - P = f(steam mass, dome volume, temperature)
   - Ideal gas / steam tables for pressure calculation
   - MSIVs open → steam flows to lines (adds volume, affects pressure buildup rate)

4. **Steam dump termination**:
   - At 1092 psig: steam dumps modulate open
   - Energy balance: Q_rcp = Q_dump + Q_losses
   - T_avg stabilizes at 557°F

### Option B: Simplified Hybrid (Fallback)

If full three-regime model is too complex for current implementation:

1. Keep existing thermocline model for subcooled phase (100-220°F)
2. At T_sat transition: switch to **effectiveness model**
   - Q_sg = ε × Q_max where ε increases with boiling
   - T_secondary = T_sat(P_secondary)
   - P_secondary ramps on saturation curve
3. At 1092 psig: clamp and dump

This is simpler but less physically rigorous.

---

## 5. VALIDATED CONSTANTS

| Parameter | Value | Source | Status |
|-----------|-------|--------|--------|
| RCS_WATER_VOLUME | 11,500 ft³ | NRC HRTD | ✓ |
| PZR_TOTAL_VOLUME | 1,800 ft³ | NRC HRTD | ✓ |
| SG_SECONDARY_WATER_PER_SG_LB | 415,000 lb | FSAR | ✓ |
| SG_HT_AREA_PER_SG_FT2 | 51,400 ft² | NRC HRTD + geometric calc | **NEEDS UPDATE** (currently 55,000) |
| SG_TUBE_OD_FT | 0.0625 ft (0.75") | WCAP-8530 | ✓ |
| SG_TUBES_PER_SG | 5,626 | FSAR | ✓ |
| RCP_HEAT_TOTAL | 21 MW (4 pumps) | NRC HRTD | ✓ |
| Primary thermal cap | ~0.96-1.05M BTU/°F | Calculated | ✓ |
| Secondary thermal cap | ~1.76M BTU/°F | Calculated | ✓ |
| Steam dump setpoint | 1092 psig | NRC HRTD 19.0 | ✓ |
| No-load T_avg | 557°F | NRC HRTD 19.0 | ✓ |
| SG safety valve range | 1170-1230 psig | NRC HRTD 7.1 | ✓ |
| h_fg at 1 atm | ~970 BTU/lb | Steam tables | ✓ |
| h_fg at 1092 psig | ~650 BTU/lb | Steam tables | ✓ |

---

## 6. NRC HRTD HEATUP SEQUENCE (Complete Reference)

From NRC HRTD Section 19.0 (ML11223A342), confirmed by full document review:

### Mode 5 → Mode 4 (Cold Shutdown → Hot Shutdown)
1. RCS solid, 150-160°F, 320-400 psig
2. SGs at wet layup (100% WR)
3. Energize PZR heaters, draw steam bubble
4. PZR to 428-448°F (T_sat for 320-400 psig), level to 25%
5. Start RCPs one at a time (after bubble drawn, P ≥ 320 psig)
6. Hold T_rcs < 160°F with RHR HX throttling
7. Stop RHR pumps → heatup begins at ~50°F/hr from RCP heat
8. At ~200°F: Begin SG draining via blowdown, chemistry checks
9. At ~200°F: Establish H₂ blanket in VCT
10. At ~220°F: Steam formation in SGs, N₂ isolated
11. Open MSIVs → warm steam lines
12. ~30,000 gallons drained from RCS during heatup (thermal expansion)

### Mode 4 → Mode 3 (Hot Shutdown → Hot Standby)
13. At 350°F: RHR isolated from RCS, ECCS aligned
14. All letdown now through normal CVCS orifices
15. Pressure rises naturally with PZR temperature
16. At 1000 psig RCS: Open accumulator isolation valves
17. At 1925 psig RCS: Verify ECCS alignment
18. **At 1092 psig SG pressure: Steam dumps actuate → heatup terminated**
19. RCS stabilizes at 557°F (no-load T_avg)
20. At 2235 psig RCS: PZR heaters/sprays to automatic

### Key Insight from NRC HRTD 19.0
> "The primary plant heatup is terminated by automatic actuation of the steam dumps (in steam pressure control) when the pressure inside the steam header pressure reaches 1092 psig. The RCS temperature remains constant at 557°F, the steam dumps removing any excess energy that would tend to drive the RCS temperature higher."

This confirms:
- **Steam dumps control the endpoint**, not some thermal equilibrium
- The SG is an **active heat rejection system** during heatup, not a passive sink
- Energy exits as steam throughout the later portion of heatup
- The massive secondary water inventory is NOT the thermal bottleneck we thought

---

## 7. FILES TO REFERENCE

### Source Code (GOLD)
- `C:\Users\craig\Projects\Critical\Assets\Scripts\Physics\SGMultiNodeThermal.cs`
- `C:\Users\craig\Projects\Critical\Assets\Scripts\Physics\PlantConstants.SG.cs`
- `C:\Users\craig\Projects\Critical\Assets\Scripts\Physics\PlantConstants.Heatup.cs`
- `C:\Users\craig\Projects\Critical\Assets\Scripts\Physics\PlantConstants.cs`
- `C:\Users\craig\Projects\Critical\Assets\Scripts\Physics\HeatupSimEngine.cs`

### Technical Documentation
- `C:\Users\craig\Projects\Critical\Technical_Documentation\NRC_HRTD_Startup_Pressurization_Reference.md`
- `C:\Users\craig\Projects\Critical\Technical_Documentation\SG_THERMAL_MODEL_RESEARCH_v3.0.0.md`
- `C:\Users\craig\Projects\Critical\Technical_Documentation\SG_MODEL_RESEARCH_HANDOFF.md`

### Implementation Plans & Changelogs
- `C:\Users\craig\Projects\Critical\Updates\IMPLEMENTATION_PLAN_v5.0.0.md`
- `C:\Users\craig\Projects\Critical\Updates\STAGE1_HANDOFF_SUMMARY.md`
- `C:\Users\craig\Projects\Critical\Updates\Changelogs\` (check for latest)
- `C:\Users\craig\Projects\Critical\Updates\Future_Features\` (check before major update)

---

## 8. NEXT STEPS

1. **Read this handoff document**
2. **Read the GOLD source files** to understand current architecture
3. **Check Future_Features** folder for related planned work
4. **Draft a NEW Implementation Plan** (likely v5.1.0 or v6.0.0) that implements the three-regime SG model:
   - Phase 1: Subcooled thermocline (existing, minor tweaks)
   - Phase 2: Boiling transition + steam production + pressure buildup
   - Phase 3: Steam dump control at 1092 psig
5. **Include SG draining model** (100% WR → 33% NR via blowdown at ~200°F)
6. **Include secondary pressure tracking** (atmospheric → saturation curve → 1092 psig)
7. **Get Craig's approval** before implementing anything
8. **Implement ONE STAGE AT A TIME** with approval between stages

---

## 9. WHY THIS MATTERS

This isn't just a parameter fix — it's a fundamental physics correction that:
- **Solves the impossible energy balance** that defeated 6 model architectures
- **Explains how real plants achieve 50°F/hr** despite massive secondary thermal mass
- **Adds major realism** (steam formation, pressure buildup, steam dump control)
- **Creates natural gameplay milestones** (steam formation at 220°F, MSIV opening, steam dump actuation)
- **Eliminates the need for calibration hacks** (artificial h_sec=25, artificial UA=600k)
- **Aligns the simulation with actual NRC HRTD procedures** operators follow

The secondary boiling/venting regime is THE missing physics that makes everything else work.
