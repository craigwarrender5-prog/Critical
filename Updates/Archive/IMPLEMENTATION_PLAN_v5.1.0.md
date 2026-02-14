# Implementation Plan v5.1.0 — SG Pressure–Saturation Coupling Fix

**Version:** v5.1.0
**Date:** 2026-02-12
**Phase:** 0 — Thermal & Conservation Stability (CRITICAL FOUNDATION)
**Priority:** 2 of 19 — Critical Stability

---

## 1. Problem Summary

### 1.1 Root Cause: Artificial Pressure Rate Limiting Creates Unphysical Heat Sink

The current SG secondary pressure model in `SGMultiNodeThermal.cs` uses a **200 psi/hr rate limiter** (`SG_MAX_PRESSURE_RATE_PSI_HR = 200f`) to control how fast secondary pressure rises during the boiling regime. This was implemented in v4.3.0 / v5.0.0 as an engineering estimate to approximate the damping effects of steam line warming, MSIV venting, and condensation.

**The problem:** This artificial rate clamp decouples secondary pressure from the actual thermodynamic state. When RCS temperature rises and boiling begins:

1. The boiling HTC activates (2,000–8,000 BTU/hr·ft²·°F) as soon as any node exceeds T_sat.
2. The driving ΔT for boiling heat transfer is `T_rcs - T_sat(P_secondary)`.
3. But `P_secondary` is rate-limited — it cannot rise to match `P_sat(T_hottest_node)`.
4. This means `T_sat` stays artificially low, creating a **large unphysical ΔT** between T_rcs and T_sat.
5. The large ΔT combined with high boiling HTCs creates a multi-MW heat sink spike.
6. The RCS cools down because the SG is absorbing more energy than RCPs inject.

**In a real PWR plant:** Secondary pressure tracks the saturation curve as boiling occurs. When the tube surface boils, the generated steam raises secondary pressure, which raises T_sat, which *reduces* the boiling driving force. This is a **self-regulating feedback loop** — pressure and temperature are thermodynamically coupled along the saturation curve.

The rate limiter breaks this feedback loop by preventing pressure from rising fast enough, creating an unphysical "free energy" condition where the SG extracts far more heat than thermodynamically justified.

### 1.2 Secondary Issue: Boiling Driven by T_rcs Instead of T_wall

The current boiling heat transfer is driven by `ΔT = T_rcs - T_sat`. In reality, nucleate boiling is driven by **wall superheat**: `ΔT_wall = T_wall - T_sat`, where T_wall is the tube outer wall temperature.

The tube wall acts as a thermal resistance between the primary coolant and the boiling secondary. T_wall is always between T_rcs and T_sat, meaning the actual boiling driving force is smaller than `T_rcs - T_sat`. Using T_rcs directly overpredicts heat transfer during boiling.

### 1.3 Impact

- Runaway SG heat sink during late heatup (27+ MW absorption exceeding 21 MW RCP heat)
- Net RCS cooldown (-16°F/hr) instead of continued heatup
- MW-scale heat transfer spikes at boiling onset
- Unrealistic thermal behavior that prevents smooth Mode 4 → Mode 3 transition

---

## 2. Expected Behavior (Realistic Target)

After implementation, the simulation should exhibit:

| Parameter | Expected Behavior |
|-----------|-------------------|
| SG secondary pressure | Rises smoothly along saturation curve once boiling begins |
| Pressure at late heatup | ~1090 psig (T_sat ≈ 557°F) |
| RCS temperature derivative | Continuous — no sudden reversals or steps |
| ΔT (RCS - SG secondary) | Tens of °F (realistic approach temperature, not hundreds) |
| SG heat absorption | Smooth, bounded by thermodynamic equilibrium — no multi-MW spikes |
| Boiling onset | Gradual, no instantaneous MW jumps |
| Heatup termination | At ~1092 psig SG pressure, steam dumps actuate |

---

## 3. Proposed Fix — Staged Implementation

### Stage 1: Remove Artificial Pressure Rate Limit and Implement Saturation Tracking

**Scope:** Remove the 200 psi/hr rate clamp and replace it with direct saturation pressure tracking during the boiling regime. Stage 1 must produce a **stable, thermodynamically coherent, fully runnable build**. Saturation tracking is not deferred — it is the replacement for the rate clamp and must be present in the same stage to avoid leaving the simulator in a broken intermediate state.

**Files Modified:**
- `SGMultiNodeThermal.cs` — Remove `MAX_PRESSURE_RATE_PSI_HR` constant (line 548) and rewrite the boiling branch of `UpdateSecondaryPressure()` (lines 1564–1623) to track saturation directly
- `PlantConstants.SG.cs` — Remove `SG_MAX_PRESSURE_RATE_PSI_HR` constant (line 794)

**What Changes:**
- The `UpdateSecondaryPressure()` method boiling branch is rewritten. During the boiling regime, secondary pressure is set directly as a derived thermodynamic variable:
  ```
  P_secondary = P_sat(T_secondary_hot_node)
  ```
  Where `T_secondary_hot_node` is the highest temperature among all boiling secondary nodes.
- The `MAX_PRESSURE_RATE_PSI_HR` constant and all rate-clamping logic (`maxDeltaP`, `deltaP` clamping) are removed entirely.
- The `SG_MAX_PRESSURE_RATE_PSI_HR` constant is removed from `PlantConstants.SG.cs`.
- Pressure is still clamped to physical limits: atmospheric floor, steam dump setpoint cap at 1092 psig, safety valve ceiling.

**What Does NOT Change:**
- Pre-steaming logic (N₂ blanket pressure at 17 psia when not isolated or all nodes subcooled)
- Steam dump pressure cap at 1092 psig
- Safety valve setpoint
- Primary mass logic — no modifications
- No steam line warming model yet
- No wall superheat model yet

**Self-Regulating Behavior:**
- As the hottest node temperature rises, pressure rises along the saturation curve
- As pressure rises, T_sat rises, which reduces the boiling driving force (T_rcs - T_sat)
- This creates a **negative feedback loop** that self-limits heat transfer
- When T_sat(P_secondary) approaches T_rcs, boiling driving force → 0 and heat transfer self-limits
- This eliminates the runaway heat sink that the rate clamp was inadvertently causing

**Validation:**
- Simulation compiles and runs to completion
- No pressure rate clamp remains in the codebase
- Secondary pressure tracks `P_sat(T_hottest_boiling_node)` during boiling regime
- No new MW spike introduced at boiling onset (regime blend ramp from v5.0.1 still active)
- No mass conservation drift introduced (pressure changes do not touch mass accounting)

---

### Stage 2: Saturation Tracking Cleanup and Formalization

**Scope:** Review and harden the saturation tracking implementation from Stage 1. Formalize the boiling branch, add guard conditions, improve diagnostic logging of the pressure–temperature coupling.

**Files Modified:**
- `SGMultiNodeThermal.cs` — Refine `UpdateSecondaryPressure()` boiling branch

**Work Items:**
- Verify edge cases: what happens when the last boiling node drops below T_sat (transition back to subcooled regime)
- Ensure pressure transitions smoothly at boiling onset (N₂ blanket → saturation tracking) and at steam dump activation (saturation tracking → steam dump cap)
- Add diagnostic logging: `P_secondary`, `T_hottest`, `T_sat`, `ΔT_driving` per timestep for validation
- Confirm monotonic pressure rise during boiling (saturation is a monotonic function of temperature, so this should hold by construction)

**What Does NOT Change:**
- Pre-steaming nitrogen dome logic
- Steam dump cap at 1092 psig
- Safety valve ceiling
- Primary mass logic

**Validation:**
- Secondary pressure tracks saturation curve smoothly through all regime transitions
- No discontinuities at boiling onset or steam dump activation
- ΔT between RCS and T_sat remains physically reasonable (tens of °F, not hundreds)
- Diagnostic logging confirms correct coupling

---

### Stage 3: Add Steam Line Warming Model (Physical Rate Damping)

**Scope:** Add a **physically justified damping mechanism** — a lumped steam line thermal mass that absorbs latent heat via condensation during early boiling. This provides the realistic early-boiling pressure lag that the removed rate clamp was approximating.

**Critical Constraint:** The steam line warming model acts as an **energy sink and condensation mechanism only**. It must NOT directly modify pressure. Pressure is always and exclusively determined by saturation tracking (`P_secondary = P_sat(T_hottest_boiling_node)`). The steam line warming model affects pressure indirectly by condensing steam, which removes energy from the steam space and reduces the effective temperature of the hottest boiling node or reduces the steam available for pressurization. **No artificial pressure subtraction logic is allowed.**

**Files Modified:**
- `SGMultiNodeThermal.cs` — New `SteamLineThermalMass` fields and condensation energy sink integrated into the boiling energy balance
- `PlantConstants.SG.cs` — New constants for steam line metal mass, initial temperature, specific heat, UA

**Physics Model:**
```
SteamLineThermalMass:
    - Lumped metal mass (4 steam lines × piping + MSIVs)
    - Has temperature state variable (T_steamLine)
    - Starts at ambient temperature (~100°F)

When T_steam > T_steamLine:
    - Steam condenses on cold metal surfaces
    - Q_condensation = UA_steamLine × (T_steam - T_steamLine)
    - Condensation removes energy from the steam/boiling system
    - This reduces the effective energy driving the hottest node temperature
    - Steam line metal warms: dT_steamLine/dt = Q_condensation / (M_metal × Cp_steel)

Pressure determination (UNCHANGED from Stage 1/2):
    - P_secondary = P_sat(T_secondary_hot_node)
    - The steam line model affects T_secondary_hot_node indirectly via energy removal
    - Pressure is NEVER modified directly by the steam line model

Effect:
    - Early in boiling: cold steam lines condense significant steam → node temperatures
      rise more slowly → saturation pressure rises more slowly (natural damping)
    - As steam lines warm: condensation rate drops → temperatures and pressure track
      saturation more closely
    - Fully warm steam lines: negligible condensation → pure saturation tracking
    - Effect fades naturally — no artificial time constant or fixed rate limit
```

**Estimated Constants (to be validated):**
- Steam line metal mass: ~120,000–160,000 lb (4 main steam lines, ~30" diameter, ~100 ft each + MSIVs + piping)
- Steam line Cp: 0.12 BTU/(lb·°F) (carbon steel)
- UA_steamLine: Engineering estimate — will be tuned to produce ~100–150 psi/hr effective rate during early boiling, decaying to near-zero as lines approach saturation temperature

**State Fields Added:**
- `SteamLineTempF` on `SGMultiNodeState` — tracks steam line metal temperature
- Initialized at simulation start temperature (same as RCS)

**What Does NOT Change:**
- Saturation tracking from Stage 1/2 remains the **sole pressure determination mechanism**
- Pre-steaming logic unchanged
- Steam dump and safety valve limits unchanged
- Primary mass logic unchanged

**Validation:**
- Early boiling: pressure rises more slowly than pure saturation tracking (physically damped via energy removal)
- Late boiling: pressure closely tracks saturation (steam lines warm, condensation negligible)
- Pressure is never directly modified by the steam line model — confirm by code inspection
- No artificial constants that don't correspond to physical quantities
- Pressure rise rate is smooth and continuous

---

### Stage 4: Wall Superheat Boiling Driver

**Scope:** Change the boiling heat transfer driving force from `(T_rcs - T_sat)` to `(T_wall - T_sat)`, where T_wall is an algebraic tube wall temperature.

**Files Modified:**
- `SGMultiNodeThermal.cs` — Add wall temperature calculation, modify boiling ΔT computation
- `PlantConstants.SG.cs` — Add primary-side HTC constant

**Physics Model:**
```
Tube wall temperature (algebraic, not a state variable):
    T_wall = T_rcs - Q_node_prev / (h_primary × A_node)

Where:
    h_primary = primary-side forced convection HTC (~1000–2000 BTU/hr·ft²·°F)
    A_node = tube inner surface area for this node
    Q_node_prev = node heat transfer rate from PREVIOUS timestep

Boiling driving force (REPLACES current ΔT = T_rcs - T_sat):
    ΔT_boiling = T_wall - T_sat

This reduces the effective driving ΔT because T_wall < T_rcs.
```

**Stability Constraints:**
- **Use previous timestep Q** for wall temperature estimate. This avoids implicit coupling and eliminates the need for an iterative solver. One-step lag is acceptable given the large secondary thermal mass and small timestep relative to thermal time constants.
- **Clamp wall temperature:** `T_sat ≤ T_wall ≤ T_rcs`. T_wall cannot physically be below T_sat (that would imply heat flowing from secondary to primary through the wall) or above T_rcs (that would violate energy conservation). The clamp prevents numerical artifacts from producing unphysical values.
- **Log T_wall and ΔT_wall** for diagnostics. Both values must appear in the interval log output so wall superheat behavior can be verified during validation.
- **No iterative solver** unless instability is observed during validation. The explicit previous-timestep approach is the baseline. If Stage 5 testing reveals oscillation or divergence, an iterative approach may be introduced as a corrective measure — but not preemptively.

**Effect:**
- At low pressure (early boiling): T_wall ≈ T_rcs - small offset → modest reduction in driving ΔT
- At high pressure (late boiling): T_wall significantly below T_rcs → prevents exaggerated heat spikes
- The wall thermal resistance provides a natural physical limit on heat transfer rate

**What Does NOT Change:**
- Subcooled heat transfer (no wall superheat concept in subcooled regime)
- Saturation tracking (Stages 1/2)
- Steam line warming (Stage 3)
- Primary mass logic

**Validation:**
- No instantaneous MW jumps at boiling onset
- Boiling heat transfer bounded by wall superheat, not bulk primary temperature
- T_wall and ΔT_wall logged in diagnostics and verified
- T_wall remains within clamp bounds: `T_sat ≤ T_wall ≤ T_rcs`

---

### Stage 5: Validation & Integration Testing

**Scope:** Full simulation validation against target criteria.

**Validation Criteria:**
1. **No instantaneous MW jumps at boiling onset** — Q_sg must be continuous
2. **RCS temperature derivative continuous** — no sudden reversals at boiling transition
3. **Secondary pressure rises smoothly along saturation curve** — monotonic during boiling
4. **Late heatup secondary pressure:** Target ~1090 psig, T_sat ~557°F
5. **ΔT between RCS and secondary** remains realistic (tens of °F, not hundreds)
6. **Mass conservation integrity preserved** — no drift introduced by pressure changes
7. **Steam line warming effect visible** — early pressure rise damped, late pressure tracks saturation
8. **Heatup rate maintained** — ~45–55°F/hr target range

**Testing:**
- Full cold startup simulation (100°F → 557°F)
- Verify all validation criteria logged
- Compare against pre-v5.1.0 behavior to confirm improvement
- Check mass conservation audit for any new drift

---

## 4. DO NOT Scope (Explicitly Excluded)

| Item | Reason |
|------|--------|
| Feedwater module | Phase 3 scope (v5.5.0) |
| Primary mass logic modifications | Canonical mass conservation (v5.0.2) must not be touched |
| Secondary mass conservation tracking | Planned for v5.4.4 |
| SG geometry redesign | Phase 5 scope (v6.0.0) |
| Per-SG pressure model | v5.4.7 (currently lumped, 4-SG model) |

---

## 5. Files Affected Summary

| File | Stage | Nature of Change |
|------|-------|-----------------|
| `SGMultiNodeThermal.cs` | 1, 2, 3, 4 | Core logic changes — pressure model, steam line warming, wall superheat |
| `PlantConstants.SG.cs` | 1, 3, 4 | Remove rate constant, add steam line and wall HTC constants |
| `SGMultiNodeState` (within SGMultiNodeThermal.cs) | 3 | Add `SteamLineTempF` state field |

---

## 6. Unaddressed Issues

| Issue | Disposition | Target |
|-------|------------|--------|
| Per-SG pressure modeling (currently 4-SG lumped) | Out of scope — functionally correct for lumped model | v5.4.7 |
| Secondary mass conservation tracking | Requires feedwater system for closure | v5.4.4 |
| SG level impact on boiling area | Requires secondary mass/level model | v5.4.4 |
| Advanced steam line model (distributed, per-line) | Current lumped model sufficient for Phase 0 | v6.0.0 |

---

## 7. Risk Assessment

| Risk | Mitigation |
|------|-----------|
| Saturation tracking causes pressure oscillation | Saturation is a monotonic function of temperature — pressure tracks temperature monotonically by construction. Steam line damping (Stage 3) provides additional physical smoothing. |
| Wall superheat calculation introduces numerical instability | Using previous-timestep Q avoids implicit coupling. T_wall clamped to `[T_sat, T_rcs]`. No iterative solver unless instability observed. |
| Phase 0 mass conservation disrupted | Pressure changes do not modify mass accounting. Mass conservation uses canonical `TotalPrimaryMassSolid` / `RCSWaterMass` which are boundary-flow-only. |
| Boiling onset transition less smooth without rate clamp | Regime blend ramp (v5.0.1, 60 sim-seconds) provides continuity at boiling onset. Wall superheat (Stage 4) further reduces onset transient. |

---

## 8. Roadmap Alignment

Roadmap alignment confirmed in `FUTURE_ENHANCEMENTS_ROADMAP.md` v3.2. No further roadmap edits required at this time.

---

## 9. Implementation Discipline

- Implement **ONE STAGE** at a time.
- Confirm after each stage before proceeding.
- Do NOT begin Stage 2 until Stage 1 is validated.
- The official `CHANGELOG_v5.1.0.md` file will be created only after Stage 5 validation is complete.
- **No changelog file is to exist until full implementation is complete.**

---

## 10. Deliverables

1. ✅ This Implementation Plan (`IMPLEMENTATION_PLAN_v5.1.0.md`)
2. ✅ Stage 1 code modifications — saturation tracking implemented
3. ✅ Stage 2 code modifications — reversion guard + diagnostics
4. ✅ Stage 3 code modifications — steam line warming model
5. ✅ Stage 4 code modifications — wall superheat boiling driver
6. ✅ Stage 5 validation — user confirmed
7. ✅ Changelog (`CHANGELOG_v5.1.0.md`) — created 2026-02-12

---

*Prepared: 2026-02-12*
*Completed: 2026-02-12*
