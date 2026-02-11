# Stage 1 Handoff Summary — SG Secondary Heating Model Correction

**Date:** 2026-02-11  
**Context:** Continuation of Implementation Plan v5.0.0, Stage 1  
**For:** Fresh chat continuation

---

## What We're Doing

Fixing the Steam Generator (SG) secondary side thermal model during RCS heatup in `SGMultiNodeThermal.cs`. The current model produces 280°F stratification between top and bottom SG nodes (top 495°F, bottom 215°F) which is physically unrealistic when 4 RCPs are forcing primary flow through the entire tube bundle.

---

## The Core Problem

The thermocline model in `SGMultiNodeThermal.cs` was designed for **no-RCP stagnant** conditions and correctly limits heat transfer to the top of the tube bundle when there's no primary flow. However, it applies the **same limitation** when RCPs are running, which is wrong — with forced primary flow, the entire tube length participates in heat transfer.

### Current Behavior (with 4 RCPs, corrected tube area)
- **Heatup rate: 48.6°F/hr** — actually within the 45-55°F/hr NRC target ✓
- **Stratification: 280°F** — physically unrealistic with forced primary flow ✗
- **SG absorption: 9.9 MW** — reasonable energy balance ✓
- **Active area: 3-8%** — far too low with RCPs running ✗

### What Should Happen
- Stratification should be **20-40°F** maximum with forced primary circulation
- Heatup rate should remain **45-55°F/hr** (NRC HRTD 19.2.2)
- SG absorption should be **4-10 MW** over the heatup
- Bottom nodes should participate meaningfully in heat transfer

---

## Critical Physics Findings (Validated by Analysis)

### 1. Tube Area Correction Required
- **Current code:** `SG_HT_AREA_PER_SG_FT2 = 55,000` ft² (Westinghouse FSAR "design" value)
- **Should be:** `51,400` ft² (NRC HRTD ML11223A213 + geometric verification from our own tube data: 5626 tubes × π × 0.0625ft × 46.7ft = 51,600 ft²)
- Also update `SG_AREA_EACH` and `SG_TUBE_AREA_TOTAL_FT2`
- Impact: Small (+0.5°F/hr heatup rate), but correctness matters

### 2. HTC Values — What's Right and What's Wrong

**The h_secondary = 800-1500 BTU/hr-ft²-°F claim was WRONG.** That range applies to forced convection (primary side) or nucleate boiling, NOT natural convection on tube exteriors.

**Churchill-Chu for 0.75" tubes in water (secondary side natural convection):**
| T_bulk | ΔT | h_secondary | Overall U (with h_pri=1000) |
|--------|-----|-------------|---------------------------|
| 150°F | 30°F | 199 | 166 |
| 250°F | 30°F | 292 | 226 |
| 350°F | 30°F | 367 | 268 |
| 450°F | 30°F | 442 | 307 |

**Current code:** `SG_MULTINODE_HTC_STAGNANT = 50` BTU/hr-ft²-°F → U ≈ 28-48 (subcooled)
**Churchill-Chu says:** h_sec ≈ 100-450 → U ≈ 91-307

The code underestimates h_secondary by 2-5×. However, this is partially compensated by effectiveness factors.

### 3. The Product U × A_eff Governs Everything

The system self-regulates through ΔT feedback. The heatup rate depends on the product `U × A_eff`, not on U or A_eff independently.

**Target for 50°F/hr at ΔT_eq ≈ 30°F:**
```
Q_sg = 5.9 MW (from energy balance: 22.8 MW input - 15.4 MW to RCS - 1.5 MW losses)
U × A_eff = Q_sg / ΔT_eq = 20.1M BTU/hr / 30°F = 670,000 BTU/hr-°F
```

**Parameter combinations that hit this target:**
| h_sec | U_overall | Required Active Frac | A_eff (ft²) |
|-------|-----------|---------------------|-------------|
| 50 | 48 | 12% | 13,400 |
| 100 | 91 | 6% | 6,700 |
| 150 | 130 | 5% | 5,150 |
| 200 | 167 | 3.5% | 4,000 |

**CRITICAL: Increasing BOTH h AND active fraction simultaneously over-couples the system and crashes heatup to ~25°F/hr.**

### 4. Multi-Node Simulation Results (with current thermocline model)

Ran coupled primary+secondary simulation with 5-node model:

| h_sec_base | Rate | Stratification | Q_sg | Status |
|------------|------|---------------|------|--------|
| 30 | 52.3°F/hr | 290°F | 8.8 MW | Rate OK, strat BAD |
| 50 (current) | 48.6°F/hr | 280°F | 9.9 MW | Rate OK, strat BAD |
| 80 | 44.9°F/hr | 246°F | 11.4 MW | Rate low, strat BAD |
| 100 | 43.1°F/hr | 223°F | 12.1 MW | Rate low, strat BAD |
| 150 | 39.7°F/hr | 177°F | 13.2 MW | Rate low, strat large |

**Key insight:** Simply bumping h_sec makes the rate WORSE because the top node absorbs more heat while the bottom nodes remain cold (thermocline barely moves). The stratification stays huge regardless.

### 5. The Real Fix: Inter-Node Mixing

The problem is NOT the HTC value — it's that the **inter-node mixing** is too weak when RCPs are running. Current values:
- `INTERNODE_UA_STAGNANT = 500` BTU/hr-°F (per SG)
- `INTERNODE_UA_BOILING = 5000` BTU/hr-°F (per SG)

With forced primary flow heating ALL tubes equally, secondary-side natural circulation develops (hot water rises, cooler water descends in downcomer). This should dramatically increase inter-node mixing, compressing stratification from 280°F to 20-40°F.

**The fix needs to:**
1. Increase inter-node mixing UA when RCPs are running (possibly 10-50× increase)
2. Bump h_sec from 50 to ~150 BTU/hr-ft²-°F (Churchill-Chu validated) — **Option B chosen**
3. Let the increased mixing redistribute heat to lower nodes, expanding effective participation
4. The combined effect: more secondary mass participates → higher effective heat capacity → proper energy balance

**The UNSOLVED question:** What inter-node mixing UA value, combined with h_sec=150 and corrected area, produces BOTH 45-55°F/hr heatup rate AND 20-40°F stratification? This needs to be determined through simulation parameter sweep.

---

## Decision Made: Option B

Craig chose **Option B**: Bump h_sec to ~150 BTU/hr-ft²-°F (Churchill-Chu physics) while calibrating the active area/mixing to maintain 50°F/hr. This is more physically grounded than keeping h_sec=50.

---

## Files to Modify

| File | Changes |
|------|---------|
| `PlantConstants.cs` | `SG_AREA_EACH`: 55,000 → 51,400 ft² |
| `PlantConstants.SG.cs` | `SG_HT_AREA_PER_SG_FT2`: 55,000 → 51,400; `SG_MULTINODE_HTC_STAGNANT`: 50 → 150; Add RCP-dependent inter-node UA; May need new mixing constants |
| `PlantConstants.Heatup.cs` | `SG_TUBE_AREA_TOTAL_FT2`: 220,000 → 205,600; `SG_HTC_NATURAL_CONVECTION`: 100 → review |
| `SGMultiNodeThermal.cs` | Add RCP-aware inter-node mixing; Update GetNodeHTC for new h_sec base; Possibly adjust thermocline behavior with RCPs |

---

## What Still Needs to Be Done Before Implementation

1. **Parameter sweep:** Find the inter-node mixing UA value (with h_sec=150, corrected area) that gives BOTH:
   - 45-55°F/hr primary heatup rate
   - 20-40°F top-bottom stratification

2. **Update Implementation Plan v5.0.0 Stage 1** to reflect:
   - Corrected tube area (51,400 ft²)
   - Option B chosen (h_sec=150)
   - Inter-node mixing as the primary fix mechanism (not just active fraction)
   - Validated parameter targets from sweep

3. **Get approval** on updated plan before coding

---

## PlantConstants Currently Validated as Correct

| Parameter | Value | Source | Status |
|-----------|-------|--------|--------|
| RCS_WATER_VOLUME | 11,500 ft³ | NRC HRTD | ✓ |
| PZR_TOTAL_VOLUME | 1,800 ft³ | NRC HRTD | ✓ |
| SG_SECONDARY_WATER_PER_SG_LB | 415,000 lb | FSAR | ✓ |
| SURGE_LINE_DIAMETER | 14 in | FSAR | ✓ |
| SG_TUBE_OD_FT | 0.0625 ft (0.75") | WCAP-8530 | ✓ |
| SG_TUBE_ID_FT | 0.05533 ft (0.664") | Derived | ✓ |
| HEATER_POWER_TOTAL | 1,800 kW | NRC HRTD | ✓ |
| SPRAY_FLOW_MAX | 900 gpm | NRC HRTD | ✓ |
| SG_TUBES_PER_SG | 5,626 | FSAR | ✓ |
| Primary thermal cap | ~1.05M BTU/°F | Calculated | ✓ |
| Secondary thermal cap | ~1.76M BTU/°F | Calculated | ✓ |

---

## Key Reference Documents

- **Implementation Plan:** `C:\Users\craig\Projects\Critical\Updates\IMPLEMENTATION_PLAN_v5.0.0.md`
- **NRC Startup Reference:** `C:\Users\craig\Projects\Critical\Technical_Documentation\NRC_HRTD_Startup_Pressurization_Reference.md`
- **SG Thermal Model:** `C:\Users\craig\Projects\Critical\Assets\Scripts\Physics\SGMultiNodeThermal.cs`
- **SG Constants:** `C:\Users\craig\Projects\Critical\Assets\Scripts\Physics\PlantConstants.SG.cs`
- **Heatup Constants:** `C:\Users\craig\Projects\Critical\Assets\Scripts\Physics\PlantConstants.Heatup.cs`
- **Core Constants:** `C:\Users\craig\Projects\Critical\Assets\Scripts\Physics\PlantConstants.cs`
- **Heatup Logs:** `C:\Users\craig\Projects\Critical\build\HeatupLogs\`

---

## Conversation Transcript

Full analysis details are preserved in: `/mnt/transcripts/2026-02-11-15-04-57-sg-heatup-htc-energy-balance-analysis.txt`

Key analysis sections in transcript:
- Energy balance derivations with thermal mass breakdowns
- Churchill-Chu HTC calculations for 0.75" tubes
- Coupled primary+secondary simulation code
- h_secondary vs U_overall distinction (the source of repeated arithmetic errors)
- Multi-node parameter sweeps showing h_sec vs stratification tradeoffs
- PlantConstants validation against NRC/FSAR reference data
