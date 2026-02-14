# Implementation Plan v5.0.0 — Startup Sequence Realism Overhaul

**Date:** 2026-02-11 (Revised 2026-02-11)  
**Version:** 5.0.0 (Rev 2 — Open-System SG Model)  
**Type:** MAJOR — Fundamental Physics Model Correction + New Simulation Phases  
**Predecessor:** v4.4.0 (PZR Level/Pressure Control Fix)

---

## Revision History

| Rev | Date | Summary |
|-----|------|---------|
| 1 | 2026-02-11 | Original 6-stage plan based on log analysis of v4.4.0 simulation |
| 2 | 2026-02-11 | **Major revision.** After 2000+ simulation runs across 6 model architectures, proved that no closed-system model can simultaneously hit both 45–55°F/hr heatup rate AND 20–40°F stratification targets. Full review of NRC HRTD 19.0 confirmed the SG secondary transitions from a closed stagnant pool to an **open system** where energy exits as steam. Stages 1 & 2 completely rewritten. Added SG secondary mass tracking. Other stages (3–6) retained with minor updates. |

---

## Problem Summary

### Original Issues (v5.0.0 Rev 1)

Six discrepancies were identified between the simulation and NRC HRTD procedures. All remain valid. Issues #1, #4, #5, #6 are unchanged. Issues #2 and #3 have been recharacterised based on the breakthrough analysis below.

| # | Issue | Severity | Status in Rev 2 |
|---|-------|----------|-----------------|
| 1 | Missing RCS pressurization phase | CRITICAL | Unchanged — Stage 4 |
| 2 | SG secondary heating physically unrealistic | CRITICAL | **Recharacterised** — root cause identified |
| 3 | SG secondary pressure model inconsistent | HIGH | **Recharacterised** — root cause identified |
| 4 | RHR isolation timing too early | HIGH | Unchanged — Stage 5 |
| 5 | Mass conservation error at RCP start | HIGH | Unchanged — Stage 8 |
| 6 | Post-bubble pressure/level control philosophy wrong | MEDIUM | Unchanged — Stage 7 |

### The Breakthrough: Why Every Closed-System Model Failed

After exhaustive parameter sweeps (~2000+ simulation runs across 6 model architectures), we proved that the fundamental approach to SG secondary modeling was wrong:

**What we assumed (wrong):** The SG secondary is a sealed vessel containing 1,660,000 lb of stagnant water. RCP heat transfers through tubes, warming this massive water inventory via sensible heat. All energy stays within the primary + secondary closed system.

**Why it fails:** The secondary thermal mass (1.76M BTU/°F) is 1.80× the primary thermal mass (0.98M BTU/°F). For the primary to heat at 50°F/hr, the SG can only absorb ~5.2 MW. But with 1.76M BTU/°F, the secondary bulk temperature rises at only ~10°F/hr. After 9 hours, T_secondary_avg would still be below 200°F while T_primary reaches 557°F — creating a 350°F+ ΔT that drives Q_sg >> 5.2 MW. **No parameter combination in any closed-system model can satisfy both the 45–55°F/hr heatup rate and 20–40°F stratification constraints simultaneously.**

**What actually happens (from NRC HRTD 19.0 — confirmed):** The heatup has three distinct thermodynamic phases:

1. **Subcooled (100→220°F, ~2.4 hrs):** The stagnant pool phase. This is the only phase where the current model's physics apply. It is SHORT — only ~26% of the temperature range.

2. **Boiling/Saturated (220→557°F, ~6.7 hrs):** Once steam forms, the SG becomes an **open system**. Energy goes into latent heat of vaporization and exits as steam through the open MSIVs. Secondary temperature tracks T_sat(P_secondary), not sensible temperature. The 1.66M lb thermal mass is irrelevant — you're boiling a thin film at tube surfaces, not heating a swimming pool. The "impossible" energy balance is resolved because excess heat simply leaves the system.

3. **Steam Dump Controlled (at 1092 psig):** Steam dumps open in steam pressure mode. T_sat(1092 psig) = 557°F = no-load T_avg. All excess RCP heat is dumped to the condenser as steam. Primary temperature stabilizes.

### Energy Balance Validation

| Parameter | Value | Source |
|-----------|-------|--------|
| RCP total heat input | 21 MW | NRC HRTD |
| Primary thermal mass | ~977,000 BTU/°F | Calculated (713k lb water + 2.2M lb metal) |
| Secondary thermal mass | ~1,756,000 BTU/°F | Calculated (1.66M lb water + 800k lb metal) |
| Q for 50°F/hr primary heatup | 14.3 MW | Primary_mass × 50 / 3.412e6 |
| System losses | ~1.5 MW | Ambient + letdown |
| Q available for SG / steam | 5.2 MW | 21 - 14.3 - 1.5 |
| Total heatup time (100→557°F) | ~9.1 hours | 457°F / 50°F/hr |
| Total steam produced (Regime 2) | ~149,000 lb | 5.2 MW × 6.7 hr / 800 BTU/lb avg h_fg |
| Steam as % of SG inventory | ~9% | 149k / 1,660k |

These numbers are consistent with NRC HRTD stating heatup takes "approximately 9 hours."

---

## Revised Expectations (Correct/Realistic Behavior)

### Three-Regime SG Secondary Behavior

**Regime 1 — Subcooled (T_rcs < ~220°F):**
- SG secondary is a closed stagnant pool at ~17 psia (N₂ blanket)
- Only the upper tube bundle region participates in heat transfer (thermocline model valid)
- Heat goes to sensible heating of secondary water near tube surfaces
- Top-to-bottom stratification develops (20–40°F is realistic for this SHORT phase)
- SG absorbs 0.5–4 MW, growing as ΔT increases
- Duration: ~2.4 hours (100→220°F at 50°F/hr)

**Regime 2 — Boiling / Open System (220°F < T_rcs < 557°F):**
- At ~220°F RCS temperature, steam forms at tube surfaces near the U-bend
- N₂ blanket isolated; MSIVs opened to warm steam lines
- Energy primarily goes to **latent heat** — steam is produced and exits via MSIVs
- Secondary temperature for boiling nodes tracks T_sat(P_secondary)
- P_secondary rises on the saturation curve as energy accumulates
- Nucleate boiling HTC (2,000–10,000 BTU/hr·ft²·°F) replaces stagnant NC HTC (~50)
- SG is an efficient heat rejection path; 50°F/hr primary heatup is easily sustainable
- Steam production rate: ~7,000–26,000 lb/hr depending on pressure/h_fg
- SG draining occurs (~200°F): 100% WR → 33% NR via blowdown
- Duration: ~6.7 hours (220→557°F at 50°F/hr)

**Regime 3 — Steam Dump Controlled (P_sg ≥ 1092 psig):**
- Steam dumps open in steam pressure mode at 1092 psig
- T_sat(1092 psig) = 557°F = no-load T_avg
- Steam dumps modulate to hold P_sg constant
- Q_dump = Q_rcp - Q_losses ≈ 19.5 MW
- Primary temperature stabilizes at 557°F (Hot Standby / HZP)
- This is the existing HZP stabilization model (largely already implemented)

### Other Expectations (Unchanged from Rev 1)

- **RHR Isolation:** At T_avg ≥ 350°F per NRC HRTD 19.0 (not at first RCP start)
- **Initial Pressurization:** From 50–100 psig to 320–400 psig via charging > letdown
- **Post-Bubble Pressure:** Follows T_sat naturally; PID control only at 2235 psig
- **Post-Bubble Level:** 75 gpm constant letdown; charging varies via PI controller
- **Mass Conservation:** < 2% error at all times

---

## Proposed Fix — Detailed Technical Plan

### Revised Stage Order

Stages reordered to reflect the new physics model. Stages 1–3 are the core SG model rewrite. Stages 4–8 are the remaining fixes from Rev 1, renumbered.

| Stage | Description | Effort | Dependencies |
|-------|-------------|--------|--------------|
| 1 | Three-Regime SG Model — Subcooled Phase (refine existing) | 4–6 hr | None |
| 2 | Three-Regime SG Model — Boiling/Open System Phase (NEW) | 10–14 hr | Stage 1 |
| 3 | Three-Regime SG Model — Steam Dump Termination & Integration | 4–6 hr | Stage 2 |
| 4 | SG Secondary Mass & Level Tracking (NEW) | 4–6 hr | Stage 2 |
| 5 | RHR Isolation Timing Correction | 1–2 hr | None |
| 6 | Initial RCS Pressurization Phase | 6–8 hr | None |
| 7 | Post-Bubble Level/Pressure Control Alignment | 4–6 hr | Stages 1–3 |
| 8 | Mass Conservation Audit | 4–6 hr | Stages 1–7 |
| **Total** | | **37–54 hr** | |

---

### Stage 1: Subcooled Phase Refinement (Regime 1)

**Goal:** Refine the existing thermocline model for the subcooled phase (100–220°F) and add the regime detection framework that will be used by all three regimes.

**This is the SHORT phase (~2.4 hours). Imperfect parameters are tolerable here because the regime transition to boiling at ~220°F will take over.**

**1A. Add Regime Enum and Tracking to SGMultiNodeState**

```csharp
public enum SGThermalRegime
{
    Subcooled,     // All nodes below T_sat — closed stagnant pool
    Boiling,       // At least one node at/above T_sat — open system, steam production
    SteamDump      // P_secondary >= steam dump setpoint — heat rejection to condenser
}
```

Add to `SGMultiNodeState`:
- `SGThermalRegime CurrentRegime` — current operating regime
- `float SteamProductionRate_lbhr` — total steam production rate (all 4 SGs)
- `float SteamProductionRate_MW` — steam production as equivalent thermal power
- `float TotalSteamProduced_lb` — cumulative steam mass produced since boiling onset
- `float SecondaryWaterMass_lb` — current total secondary water mass (all 4 SGs)

Add to `SGMultiNodeResult`:
- `SGThermalRegime Regime` — current regime
- `float SteamProductionRate_lbhr` — instantaneous steam rate
- `float SteamProductionRate_MW` — thermal equivalent
- `float SecondaryWaterMass_lb` — current water mass

**1B. Regime Detection Logic**

In `SGMultiNodeThermal.Update()`, before the per-node heat transfer loop:

```
Regime transitions:
  Subcooled → Boiling: when ANY node temperature ≥ T_sat(P_secondary)
  Boiling → SteamDump: when P_secondary ≥ SG_STEAM_DUMP_SETPOINT_PSIG + 14.7
  SteamDump → Boiling: when P_secondary drops below setpoint (unlikely but handled)
  Any → Subcooled: when ALL nodes drop below T_sat (cooldown scenario)
```

**1C. Subcooled Phase Behaviour (Regime == Subcooled)**

Retain the existing thermocline model with these refinements:
- Keep 5-node vertical stratification with thermocline descent
- Keep stagnant NC HTC (50 BTU/hr·ft²·°F with temperature scaling)
- Keep bundle penalty factor (0.55)
- Keep inter-node stagnant conduction (UA = 500 BTU/hr·°F)
- **Remove** the v4.3.0 boiling HTC multiplier logic from this regime (it will be handled properly in Regime 2)
- Steam production rate = 0 in this regime
- Secondary pressure stays at N₂ blanket value (17 psia) until N₂ isolation

**1D. Update PlantConstants.SG.cs**

Add new constants for regime model:
```csharp
/// <summary>
/// Latent heat of vaporization at atmospheric pressure in BTU/lb.
/// Source: Steam tables — h_fg at 14.7 psia (212°F)
/// </summary>
public const float HFG_ATMOSPHERIC_BTU_LB = 970f;

/// <summary>
/// Latent heat of vaporization at steam dump setpoint in BTU/lb.
/// Source: Steam tables — h_fg at 1107 psia (557°F)
/// </summary>
public const float HFG_STEAM_DUMP_BTU_LB = 635f;
```

**Files modified:**
- `SGMultiNodeThermal.cs` — Add regime enum/state, regime detection, isolate subcooled path
- `PlantConstants.SG.cs` — Add regime-related constants

**Validation criteria:**
- [ ] Regime starts as `Subcooled` at initialization
- [ ] Subcooled regime behavior identical to current model (regression test)
- [ ] Regime transitions to `Boiling` when top node reaches T_sat(P_secondary)
- [ ] Steam production rate = 0 during subcooled regime
- [ ] Heatup rate during subcooled phase: 45–55°F/hr (as currently achieved)
- [ ] No changes to pressure or level behavior during subcooled phase

---

### Stage 2: Boiling / Open System Phase (Regime 2) — NEW PHYSICS

**Goal:** Implement the open-system boiling model where energy exits as steam. This is the core breakthrough — the fundamental physics change that resolves the impossible energy balance.

**2A. Per-Node Boiling Logic**

In the per-node heat transfer loop, when `CurrentRegime == Boiling`:

For each node, determine if it is subcooled or boiling:

```
if (T_node >= T_sat(P_secondary)):
    // BOILING NODE — energy goes to latent heat
    h_node = nucleate boiling HTC (2,000–10,000 BTU/hr·ft²·°F)
    Q_node = h_node × A_eff_node × (T_rcs - T_sat)
    m_steam_node = Q_node / h_fg(P_secondary)
    T_node = T_sat(P_secondary)   // clamped to saturation
else:
    // SUBCOOLED NODE — energy goes to sensible heating (as before)
    h_node = stagnant NC HTC (existing model)
    Q_node = h_node × A_eff_node × (T_rcs - T_node)
    dT_node = Q_node × dt / (m_node × cp)
    T_node += dT_node
```

**Key physics point:** Boiling nodes have their temperature **clamped** to T_sat(P_secondary). They don't heat up — they produce steam. The energy goes to latent heat and exits the system. This is what makes the open-system model work.

**2B. Nucleate Boiling HTC**

For nodes at or above T_sat, use a nucleate boiling correlation. The Jens-Lottes or simplified Rohsenow approach:

```csharp
/// <summary>
/// Nucleate boiling HTC for SG tube bundle in BTU/(hr·ft²·°F).
/// At low pressures (0–200 psia): h_boiling ≈ 2,000–5,000
/// At high pressures (500–1100 psia): h_boiling ≈ 5,000–10,000
/// The exact value matters less than getting the regime right,
/// because Q_boiling is ultimately limited by the primary-side
/// delivery rate, not the secondary-side HTC.
///
/// Using pressure-dependent ramp:
///   h_boiling = 2000 + 6000 × (P_secondary / 1200)
/// Clamped to [2000, 8000] BTU/(hr·ft²·°F)
///
/// Source: Incropera & DeWitt Ch. 10, Rohsenow correlation
/// </summary>
```

**Important insight:** In the boiling regime, the secondary-side HTC is very high, so the overall heat transfer is now limited by the **primary-side HTC** and the available ΔT (T_rcs - T_sat). The exact boiling HTC value is not critical — anything in the 2,000–10,000 range produces similar results because the primary side (1,000 BTU/hr·ft²·°F) becomes the bottleneck.

**2C. Steam Production and Energy Exit**

Total steam production across all boiling nodes and all 4 SGs:

```
Q_boiling_total = Σ(Q_boiling_node) for all boiling nodes × 4 SGs
m_dot_steam = Q_boiling_total / h_fg(P_secondary)
```

This steam energy **exits the system** — it is NOT added to secondary thermal mass. This is the key difference from the closed-system model.

Track:
- `SteamProductionRate_lbhr` = m_dot_steam
- `SteamProductionRate_MW` = Q_boiling_total / 3.412e6
- `TotalSteamProduced_lb` += m_dot_steam × dt_hr

**2D. Effective Area in Boiling Regime**

Once boiling starts, the thermocline concept evolves into a **boiling front**:
- Nodes above the boiling front are at T_sat and boiling actively → full area available
- Nodes below are still subcooled → thermocline model still applies
- The boiling front descends as P_secondary rises (T_sat increases, more nodes reach it)

Implementation: keep the existing area fraction arrays, but override effectiveness for boiling nodes:
```
if node is boiling:
    effective_area = geometric_area_fraction × BUNDLE_PENALTY_FACTOR
    // No stagnant effectiveness penalty — boiling provides vigorous agitation
else:
    effective_area = existing thermocline-based calculation
```

**2E. Secondary Pressure Evolution (Open System)**

This replaces the v4.3.0 quasi-static closed-system pressure model.

In an open system with MSIVs open, the pressure is determined by the balance between steam production and steam venting. For a simulation, we model this as:

**Approach: Lagged Saturation Tracking**

```
P_target = P_sat(T_rcs - ΔT_approach)
```

Where `ΔT_approach` is the primary-to-secondary approach temperature, typically 30–60°F during heatup (the pinch point of the SG as a heat exchanger).

However, a more physically grounded approach is to track steam energy balance:

```
dP/dt = (Q_steam_in - Q_steam_out) / (V_steam × dρ/dP)
```

Where:
- `Q_steam_in` = steam produced from boiling (computed in 2C)
- `Q_steam_out` = steam vented through MSIVs + condensation in cold steam lines
- `V_steam` = steam dome volume (above water level)

**Simplified model for implementation:**
- Steam venting provides a "pressure relief" that limits how fast P can rise
- Model as: `dP/dt = (m_dot_steam_produced - m_dot_steam_vented) × h_fg / (V_dome × ρ_steam × cp_steam)`
- `m_dot_steam_vented` is proportional to `P_secondary - P_atmosphere` (flow through open MSIVs)
- This creates the gradual pressure buildup observed in real plants

**Alternative simplified model (recommended for initial implementation):**
- Use `P_secondary = P_sat(T_secondary_bulk_boiling)` where `T_secondary_bulk_boiling` is the mass-weighted average of boiling-zone node temperatures
- Since boiling nodes are clamped to T_sat, and T_sat = f(P), this creates a self-consistent loop
- Add a lag/rate limiter to prevent instantaneous pressure jumps: `dP/dt ≤ MAX_PRESSURE_RATE` (e.g., 50 psi/hr based on typical SG pressure buildup during heatup)
- This avoids the need to model steam line dynamics explicitly

**Pressure rate limit justification:** During a typical 9-hour heatup, secondary pressure rises from ~0 psig to 1092 psig. That's an average of ~121 psig/hr. The rate is slower early (low boiling rate) and faster later. A max rate of 150–200 psi/hr is reasonable.

**2F. Latent Heat h_fg Calculation**

h_fg varies with pressure. Use the WaterProperties module to compute h_fg at the current secondary pressure, or add a polynomial fit:

```csharp
/// <summary>
/// Latent heat of vaporization in BTU/lb at given pressure.
/// Fit to NIST steam tables, valid 14.7–1200 psia.
/// h_fg decreases with increasing pressure (approaches 0 at critical point).
/// </summary>
public static float LatentHeatOfVaporization(float pressure_psia)
```

If WaterProperties doesn't already have this, add it. Key reference points:
| P (psia) | T_sat (°F) | h_fg (BTU/lb) |
|-----------|------------|---------------|
| 14.7 | 212 | 970 |
| 100 | 328 | 888 |
| 250 | 401 | 834 |
| 500 | 467 | 755 |
| 750 | 510 | 686 |
| 1000 | 545 | 650 |
| 1107 | 557 | 635 |

**2G. Inter-Node Behaviour During Boiling**

- Boiling nodes: Temperature clamped to T_sat. No inter-node conduction needed between boiling nodes (they're all at the same temperature).
- Boiling-to-subcooled interface: Enhanced mixing from boiling agitation. Use `INTERNODE_UA_BOILING = 5000` (existing constant) for boundaries where one node is boiling and the adjacent is subcooled.
- Subcooled-to-subcooled: Use `INTERNODE_UA_STAGNANT = 500` (existing constant).

**Files modified:**
- `SGMultiNodeThermal.cs` — Boiling regime path, per-node boiling/subcooled logic, steam production, pressure evolution
- `PlantConstants.SG.cs` — Boiling HTC constants, pressure rate limit, h_fg reference
- `WaterProperties.cs` — Add `LatentHeatOfVaporization(float P_psia)` if not present

**Validation criteria:**
- [ ] When top node reaches T_sat: regime transitions to Boiling
- [ ] Boiling nodes clamped to T_sat(P_secondary) — do NOT heat above saturation
- [ ] Steam production rate > 0 once boiling regime active
- [ ] Energy balance: Q_rcp = Q_primary_heatup + Q_steam_exit + Q_sensible_subcooled_nodes + Q_losses
- [ ] Secondary pressure rises gradually from ~0 psig toward 1092 psig
- [ ] Primary heatup rate remains 45–55°F/hr throughout boiling regime
- [ ] No thermal mass "wall" — primary continues heating smoothly through 220°F transition
- [ ] Boiling front descends as pressure rises (more nodes reach T_sat)

---

### Stage 3: Steam Dump Termination & Integration (Regime 3)

**Goal:** Implement steam dump actuation at 1092 psig to terminate heatup, and integrate all three regimes into a smooth heatup profile.

**3A. Steam Dump Actuation**

When `P_secondary >= SG_STEAM_DUMP_SETPOINT_PSIG + 14.7` (1107 psia):
- Regime transitions to `SteamDump`
- Steam dumps modulate to hold P_secondary at 1092 psig
- Q_dump = Q_rcp - Q_losses (all excess heat removed via steam)
- T_rcs stabilizes at T_sat(1092 psig) = 557°F

**3B. Steam Dump Controller Integration**

The existing `SteamDumpController.cs` operates in T_avg control mode. During heatup, it should operate in **Steam Pressure mode**:

```
if (heatup phase, pre-criticality):
    Steam dump setpoint = 1092 psig (SG header pressure)
    Steam dump output = PID(P_secondary, 1092 psig)
else (post-criticality, at-power):
    Steam dump setpoint = T_avg mode (existing)
```

Wire the SG secondary pressure to the steam dump controller input during heatup.

**3C. Heat Rejection in Steam Dump Regime**

In the steam dump regime:
- All boiling nodes continue producing steam
- Steam dumps remove steam to condenser at the rate needed to hold P constant
- `Q_dump = m_dot_dump × h_fg`
- `m_dot_dump ≈ Q_rcp / h_fg` at steady state
- Primary temperature stabilizes — heatup terminates

**3D. Regime Integration and Smooth Transitions**

Verify that the full heatup profile (100→557°F) is smooth:
- Subcooled→Boiling transition at ~220°F: no cliff in heatup rate
- Boiling→SteamDump transition at 1092 psig: smooth temperature cap
- Energy balance satisfied continuously across all transitions
- Total heatup time: ~9 hours (consistent with NRC HRTD)

**3E. Diagnostic Logging Updates**

Update `GetDiagnosticString()` to show:
- Current regime (Subcooled / Boiling / SteamDump)
- Steam production rate (lb/hr and MW)
- Cumulative steam produced (lb)
- Secondary pressure (psig) and T_sat
- Per-node: boiling/subcooled status, Q_sensible vs Q_latent
- Steam dump valve position (when in Regime 3)

**Files modified:**
- `SGMultiNodeThermal.cs` — Steam dump regime, transition logic
- `SteamDumpController.cs` — Add steam pressure mode for heatup
- `HeatupSimEngine.cs` — Wire SG pressure to steam dump during heatup
- `PlantConstants.SteamDump.cs` — Add/verify steam pressure mode constants

**Validation criteria:**
- [ ] Steam dumps actuate at P_secondary = 1092 psig
- [ ] T_rcs caps at 555–559°F (within 2°F of 557°F target)
- [ ] Heatup rate drops to ~0°F/hr when steam dumps active
- [ ] Q_dump ≈ 19.5 MW at steady state (Q_rcp - Q_losses)
- [ ] Smooth heatup profile: no discontinuities at regime transitions
- [ ] Total heatup time 100→557°F: 8.5–10 hours
- [ ] All diagnostic logging shows correct regime and steam data

---

### Stage 4: SG Secondary Mass & Level Tracking — NEW

**Goal:** Track the secondary water inventory as it changes due to draining and steam production. This feeds into the Screen 5 SG Level display (currently a placeholder).

**4A. Secondary Water Mass Tracking**

Add to SGMultiNodeState / per-timestep update:
```
Mass decreases from:
  1. SG draining via blowdown at ~200°F (100% WR → 33% NR)
     - Rate: 150 gpm × 8.33 lb/gal per SG
     - Duration: until target level reached
  2. Steam boiloff during Regime 2
     - Rate: m_dot_steam (computed in Stage 2)
     - Partially offset by condensate return (if modeled)
  3. Thermal expansion (density decrease as temperature rises)

Mass increases from:
  1. Feed/condensate system makeup (if modeled — may be deferred)
```

Initial mass: 415,000 lb per SG × 4 = 1,660,000 lb total
After draining: 415,000 × 0.55 = 228,250 lb per SG × 4 = 913,000 lb total
After boiling: ~913,000 - 149,000 = ~764,000 lb total

**4B. SG Draining Model Enhancement**

The existing draining model in PlantConstants.SG has the constants but may not be fully wired. Verify:
- Draining starts at T_rcs ≈ 200°F (`SG_DRAINING_START_TEMP_F`)
- Rate: 150 gpm per SG (`SG_DRAINING_RATE_GPM`)
- Target: 55% of wet layup mass (`SG_DRAINING_TARGET_MASS_FRAC`)
- Track per-SG mass and update node mass fractions accordingly

**4C. SG Level Indication**

SG level is a function of water mass, secondary pressure, and void fraction:
- Narrow range level: referenced to operating conditions
- Wide range level: referenced to total SG volume
- During heatup: operators watch wide range level trending down from 100%
- After draining: transition to narrow range at 33% NR

This bridges to Screen 5 placeholder resolution (Future Features Priority 5).

**Files modified:**
- `SGMultiNodeThermal.cs` — Mass tracking, draining integration, level calculation
- `PlantConstants.SG.cs` — Any additional draining/level constants
- `HeatupSimEngine.cs` — Wire draining trigger to SG model

**Validation criteria:**
- [ ] Secondary mass starts at 1,660,000 lb total (4 × 415,000)
- [ ] Draining begins at T_rcs ≈ 200°F
- [ ] Draining rate ≈ 150 gpm per SG
- [ ] Mass reaches ~55% of initial after draining complete
- [ ] Steam boiloff reduces mass gradually during Regime 2
- [ ] Total steam boiled: ~100,000–200,000 lb over full Regime 2
- [ ] SG level trending downward visible in diagnostics

---

### Stage 5: RHR Isolation Timing Correction

**Unchanged from Rev 1 Stage 3.**

**Problem:** Current code isolates RHR when `rcpCount > 0` (first RCP start, T_rcs ~153°F). NRC HRTD 19.0 states RHR is isolated at ~350°F (Mode 3 entry).

**Fix approach:**

1. Change RHR isolation trigger from `rcpCount > 0` to `T_rcs >= 350f`
2. Keep existing pressure interlock at 585 psig as backup
3. Update letdown path: RHR cross-connect (HCV-128) below 350°F, normal orifices above
4. Log letdown path change and ECCS alignment events

**Files modified:**
- `HeatupSimEngine.cs` — RHR isolation condition
- `HeatupSimEngine.CVCS.cs` — Letdown path transition logic

**Validation criteria:**
- [ ] RHR stays connected until T_rcs ≥ 350°F
- [ ] Smooth transition to orifice letdown at 350°F
- [ ] Event log shows correct RHR isolation timing

---

### Stage 6: Initial RCS Pressurization Phase

**Unchanged from Rev 1 Stage 4.**

**Problem:** Simulation starts at 365 psia (350 psig) with heaters already energized. Real plant starts at 50–100 psig.

**Fix approach:**

1. New `PHASE_PRESSURIZE` simulation phase before existing flow
2. Initial conditions: T_rcs = 120°F, P = 115 psia (100 psig), PZR solid, heaters OFF
3. Pressurize via charging > letdown (+20–40 gpm net)
4. Target: 400–425 psig (below RHR suction limit)
5. Transition to heater phase when pressure stable at 320–400 psig
6. Add Inspector toggle: `skipPressurizationPhase` for quick-start testing

**Files modified:**
- `HeatupSimEngine.Init.cs` — New initial conditions
- `HeatupSimEngine.cs` — New PHASE_PRESSURIZE handling
- `SolidPlantPressure.cs` — Low-pressure control band
- `PlantConstants.Pressure.cs` / `PlantConstants.cs` — Pressurization constants

**Validation criteria:**
- [ ] Simulation starts at 100 psig, heaters OFF
- [ ] Pressure rises at ~50–100 psi/hr
- [ ] Stabilizes in 320–400 psig band
- [ ] PZR heaters energize only after pressurization complete
- [ ] Total pressurization time: 1–3 hours
- [ ] No overpressure (below 425 psig)

---

### Stage 7: Post-Bubble Level/Pressure Control Alignment

**Unchanged from Rev 1 Stage 5.**

**Problem:** Ad-hoc charging/letdown adjustment instead of NRC HRTD 10.2/10.3 philosophy.

**Fix approach:**

1. Pressure: follows T_sat naturally during heatup; PID at 2235 psig only
2. Level: constant 75 gpm letdown, variable charging via PI controller
3. Level program: 25% at no-load → 61.5% at full power
4. ~30,000 gallons removed during full heatup via VCT → holdup tanks

**Files modified:**
- `CVCSController.cs` — PI level controller refinement
- `PlantConstants.Pressurizer.cs` — Level program validation
- `HeatupSimEngine.CVCS.cs` — Constant letdown enforcement

**Validation criteria:**
- [ ] Letdown constant at 75 gpm throughout post-bubble heatup
- [ ] Charging varies 20–130 gpm to match programmed level setpoint
- [ ] Level setpoint = 25% at T_avg = 547°F
- [ ] ~30,000 gallons removed during full heatup

---

### Stage 8: Mass Conservation Audit

**Unchanged from Rev 1 Stage 6, with addition of steam mass accounting.**

**Problem:** Mass conservation error at RCP start and phase transitions.

**Fix approach:**

1. Audit mass at each phase transition (solid → two-phase, regime transitions)
2. **New: Include steam mass in conservation accounting**
   - Total mass = RCS_water + PZR_water + PZR_steam + VCT + BRS + **SG_secondary_water + SG_steam_produced**
   - Steam that exits via MSIVs/steam dumps must be tracked as "mass removed from SG"
3. Per-timestep mass audit logging
4. Flag any step where delta exceeds 100 lbm

**Files modified:**
- `HeatupSimEngine.Logging.cs` — Enhanced mass audit with steam tracking
- `HeatupSimEngine.BubbleFormation.cs` — Mass balance at transitions
- `HeatupSimEngine.cs` — Mass continuity checks including SG steam

**Validation criteria:**
- [ ] Mass conservation error < 2% at all times
- [ ] No spikes > 5% at any transition
- [ ] Steam mass production properly accounted in conservation balance
- [ ] SG secondary mass + cumulative steam = initial SG mass (minus draining)

---

## Unaddressed Issues

| Issue | Reason | Future Plan |
|-------|--------|-------------|
| **Mode 5→4 temperature hold at ~200°F** | Separate procedural feature, not a physics fix | Future_Features PRIORITY 3 (already documented) |
| **Per-SG primary loop modeling** | Currently all 4 SGs lumped as single thermal model | Future_Features PRIORITY 13 |
| **Steam line warming thermal model** | MSIVs open and steam warms lines; affects early condensation rate | Deferred — simplified via pressure rate limit in Stage 2 |
| **Feed/condensate makeup to SG during heatup** | SG level drops from draining + boiloff; feed system provides makeup | Deferred to Future_Features (Screen 5 / SG Level resolution) |
| **Hydrogen blanket establishment in VCT** | N₂ purge → H₂ at ~200°F | Cosmetic/procedural — does not affect physics |
| **Oxygen scavenging (hydrazine addition)** | Must be in spec before 250°F | Chemistry model not in scope |
| **Accumulator alignment at 1000/1925 psig** | Open SIT valves, verify ECCS | ECCS model not in scope — Future_Features PRIORITY 14 |
| **Excess letdown path** | Supplemental letdown during heatup | Future_Features PRIORITY 8 (v4.6.0) |
| **Active operator SG pressure management** | ADV control, stepping pressure up with primary | Future_Features PRIORITY 11 |

All unaddressed issues that are planned for future release are confirmed present in `Critical\Updates\Future_Features\FUTURE_ENHANCEMENTS_ROADMAP.md`.

---

## Technical References

| Document | ID | Relevance |
|----------|----|-----------|
| NRC HRTD Section 19.0 — Plant Operations | ML11223A342 | Authoritative startup sequence, three-phase heatup, steam dump termination |
| NRC HRTD Chapter 17.0 — Plant Operations | ML023040268 | Pressurization from 50–100 psig, RCP start, bubble formation |
| NRC HRTD Section 10.2 — PZR Pressure Control | ML11223A287 | PID controller, spray/heater staging, 2235 psig setpoint |
| NRC HRTD Section 10.3 — PZR Level Control | ML11223A290 | Level program, constant letdown / variable charging |
| NRC HRTD Section 4.1 — CVCS | ML11223A214 | Flow balance, orifice lineup |
| NRC HRTD Section 7.1 — Main Steam | ML11223A244 | SG safety valves, steam dump setpoints |
| NRC HRTD Section 11.2 — Steam Dump | ML11223A294 | Steam pressure mode, 1092 psig setpoint |
| Steam Tables (NIST) | — | h_fg, T_sat, P_sat reference data |
| Incropera & DeWitt, 7th ed. | — | Ch. 9 (Natural Convection), Ch. 10 (Boiling) |
| NUREG/CR-5426 | — | SG natural circulation phenomena |
| SG_HEATUP_BREAKTHROUGH_HANDOFF.md | Local | Full analysis of 2000+ runs, NRC procedure sequence |

---

## Estimated Total Effort

| Stage | Description | Effort | Dependencies |
|-------|-------------|--------|--------------|
| 1 | Subcooled Phase Refinement + Regime Framework | 4–6 hr | None |
| 2 | Boiling / Open System Phase (core breakthrough) | 10–14 hr | Stage 1 |
| 3 | Steam Dump Termination & Integration | 4–6 hr | Stage 2 |
| 4 | SG Secondary Mass & Level Tracking | 4–6 hr | Stage 2 |
| 5 | RHR Isolation Timing | 1–2 hr | None |
| 6 | Initial RCS Pressurization Phase | 6–8 hr | None |
| 7 | Post-Bubble Level/Pressure Control | 4–6 hr | Stages 1–3 |
| 8 | Mass Conservation Audit | 4–6 hr | Stages 1–7 |
| **Total** | | **37–54 hr** | |

---

## Implementation Rules

- Implement **one stage at a time**
- **Stop and check** with user after each stage before proceeding
- Run simulation after each stage and produce updated log analysis
- Do not batch stages together, even if they appear small or related
- Maintain GOLD standard on all modified modules
- Document all changes with NRC citation in code headers
- All changes to SGMultiNodeThermal.cs must maintain API compatibility with existing callers
