# Implementation Plan v4.3.0 — SG Secondary Pressure Model, Dynamic Boiling Onset, and Heat Transfer Recalibration

## Version: 4.3.0
## Date: 2026-02-11
## Status: AWAITING APPROVAL
## Scope: SG Secondary Pressure Tracking, Pressure-Dependent Boiling, HTC Effectiveness Revision
## Supersedes: IMPLEMENTATION_PLAN_v4.2.0.md (Issues 2 & 3 carried forward with expanded scope; Issue 1 unchanged — no fix needed)

---

## Problem Summary

Post-v3.0.0 heatup validation and subsequent research into NRC HRTD Chapter 19 heatup procedures, WCAP-14040 cold overpressure methodology, and supplementary PWR startup literature have revealed that the SG secondary model is missing a fundamental physical parameter: **secondary side pressure**. This omission cascades into two other problems — an unrealistic boiling cliff at 220°F and excessive primary-to-secondary ΔT — that together produce non-physical heatup behavior in the mid-to-late heatup window.

### Three Interconnected Problems

**Problem A: No SG Secondary Pressure Tracking (ROOT CAUSE)**

The SG secondary side has no pressure state variable. During heatup, the real SG secondary transitions from a nitrogen-blanketed wet layup vessel at ~17 psia to a sealed pressurizing system reaching 1092 psig at hot zero power. This pressure progression is a defining characteristic of the heatup — it determines when boiling starts, how intense boiling is, and when heatup terminates (steam dumps actuate at 1092 psig). Our model has none of this.

NRC HRTD ML11223A342 Section 19.2.2: *"At approximately 220°F RCS temperature, steam formation begins in the steam generators. The nitrogen supply to the steam generators is now isolated."*

NRC HRTD ML11223A294 Section 11.2: *"The primary plant heatup is terminated by automatic actuation of the steam dumps when the pressure inside the steam header reaches 1092 psig."*

**Problem B: Fixed Boiling Onset Causes Cliff Behavior (from v4.2.0 Issue 2)**

Boiling onset is hardcoded at `SG_BOILING_ONSET_TEMP_F = 220°F`, which is correct only at ~17 psia (atmospheric + nitrogen blanket). Once boiling begins and the secondary pressurizes, T_sat rises with pressure. By the time RCS reaches 400°F, SG secondary pressure should be ~210 psia (T_sat = 381°F), making the boiling much less intense than our model shows. Instead, our model keeps the 220°F threshold regardless of pressure, causing the 5× HTC multiplier to remain fully active far too early.

The step-function multiplier compounds this: when the top node crosses 220°F, HTC jumps 5× instantly, SG heat absorption triples from 6.5→20.2 MW, and heatup rate crashes from 48→5°F/hr in a single interval.

**Problem C: Excessive Primary-to-Secondary ΔT (NEW)**

Supplementary analysis of PWR heatup procedures confirms that with 4 RCPs providing forced circulation through 55,000 ft² of tube surface per SG, the secondary should track the primary much more closely than our model shows. At 13.75 hr, our model produces Tavg=367°F with SG top node at 210°F — a ΔT of 157°F. In reality, with forced primary circulation and vigorous tube-surface convection, the ΔT should be 20-50°F during heatup.

The excessive ΔT stems from the stagnant effectiveness factors (0.40 top to 0.01 bottom) combined with the bundle penalty (0.40) throttling heat transfer too aggressively. These factors were tuned for the v3.0.0 model to produce the correct heatup rate, but they achieve the right answer for the wrong reason — the SG absorbs the right amount of total heat only because the huge ΔT compensates for the very low effectiveness.

---

## Expected Behavior (Real Plant)

Based on NRC HRTD 19.0, WCAP-14040, and supplementary PWR heatup literature:

### SG Secondary Pressure Progression

| RCS Tavg (°F) | SG Secondary Temp (°F) | SG Pressure (psia) | T_sat (°F) | Phase |
|---|---|---|---|---|
| 100-150 | ~100-150 | ~17 | 220 | Wet layup, N₂ blanket |
| 200 | ~170-190 | ~17 | 220 | Pre-steaming, draining starts |
| 220 | ~200-210 | 17→rising | 220→rising | N₂ isolated, steam onset |
| 300 | ~270-280 | ~50 | 281 | Pressurizing, closed vessel |
| 350 | ~320-330 | ~105 | 338 | Secondary tracking primary |
| 400 | ~370-380 | ~210 | 381 | Moderate pressure |
| 450 | ~420-430 | ~360 | 432 | Approaching operating range |
| 500 | ~470-480 | ~580 | 477 | High pressure |
| 557 | ~540-545 | ~1092 | 557 | No-load equilibrium |

### SG Secondary ΔT Behavior

With forced primary circulation (4 RCPs at ~100,000 gpm/loop):
- Primary-side HTC: ~2000-4000 BTU/(hr·ft²·°F) (turbulent forced convection in tubes)
- Tube wall resistance: negligible (~0.0004 hr·ft²·°F/BTU for 0.043" Inconel)
- Secondary-side HTC: ~50-100 BTU/(hr·ft²·°F) (stagnant NC) rising to ~250-1000 with boiling
- Overall U: dominated by secondary side, ~45-90 subcooled, ~200-500 boiling
- With 55,000 ft²/SG × 4 SGs = 220,000 ft² total area:
  - At U=50 and ΔT=30°F: Q = 50 × 220,000 × 30 = 330 MBTU/hr = 97 MW (way more than needed)
  - At U=50 and ΔT=5°F: Q = 55 MBTU/hr = 16 MW (about right for heatup)
- **Conclusion:** The secondary tracks the primary very closely because even a small ΔT drives enormous heat transfer through the large tube surface area

### Self-Limiting Pressure Feedback

The critical physics insight is that SG secondary pressure creates a natural self-limiting feedback loop:

1. Boiling starts at ~220°F (T_sat at 17 psia)
2. Steam production pressurizes the closed secondary
3. Higher pressure raises T_sat, reducing superheat (T_node - T_sat)
4. Reduced superheat reduces boiling intensity
5. System self-regulates — no cliff, no runaway

This feedback is why real plants don't experience the 48→5°F/hr cliff our model produces.

### Heatup Termination

Steam dumps actuate in steam pressure mode when SG header pressure reaches 1092 psig. This is the mechanism that terminates the heatup at Tavg ≈ 557°F. Without secondary pressure tracking, we have no way to model this critical endpoint.

---

## Proposed Fix — Three-Part Physics Upgrade

### Part 1: SG Secondary Pressure Model

Add `SecondaryPressure_psia` as a state variable in `SGMultiNodeState`. The pressure evolves based on the hottest secondary node temperature and the vessel thermodynamics:

**Pre-steaming phase** (all nodes below T_sat at current pressure):
- Pressure stays at initial value (~17 psia for N₂ blanket)
- Secondary is subcooled liquid, incompressible

**Steam onset** (hottest node reaches T_sat):
- Nitrogen blanket is isolated (one-time flag)
- Secondary becomes a closed, pressurizing vessel
- Small amount of steam produced pressurizes the vapor space

**Pressurization phase** (secondary is two-phase):
- As the hottest secondary node temperature rises, P_secondary tracks the saturation curve: `P_secondary = SaturationPressure(T_hottest_node)`
- This is a simplification that assumes the secondary is in quasi-static two-phase equilibrium — valid because the SG secondary has enormous thermal mass and pressurizes slowly
- The simplification avoids needing a full mass/energy balance on the steam space, which would require tracking steam mass, feedwater, and relief valve flows

**No-load equilibrium**:
- P_secondary reaches 1092 psig (T_sat ≈ 557°F)
- Steam dump controller activates (existing `SteamDumpController.cs`)

**Implementation approach:**
```
if (no nodes above T_sat at current pressure):
    P_secondary = SG_INITIAL_PRESSURE_PSIA  (17 psia, N₂ blanket)
else:
    P_secondary = SaturationPressure(T_hottest_node)
    clamp to range [SG_INITIAL_PRESSURE_PSIA, SG_SAFETY_VALVE_PRESSURE_PSIA]
```

This quasi-static approach is physically valid because:
- The SG secondary thermal mass (~415,000 lb water per SG) changes temperature slowly
- Steam production rate at low superheat is modest
- The SG is a closed vessel (MSIVs open only to warm steam lines, no significant venting)
- Real plant operators manage secondary pressure by tracking it, not fighting it

### Part 2: Dynamic Boiling Onset with Pressure Feedback

Replace the fixed 220°F boiling check with a dynamic threshold based on `T_sat(P_secondary)`:

**Boiling onset per node:** `T_node ≥ T_sat(P_secondary)`

**Boiling intensity** (superheat-based smoothstep):
```
ΔT_superheat = T_node - T_sat(P_secondary)
f_boil = smoothstep(ΔT_superheat / SG_BOILING_SUPERHEAT_RANGE_F)
HTC_multiplier = 1.0 + f_boil × (SG_BOILING_HTC_MULTIPLIER - 1.0)
```

Where `SG_BOILING_SUPERHEAT_RANGE_F` is the superheat range over which boiling HTC ramps from 1× to full multiplier (~15-20°F, based on Incropera & DeWitt Ch. 10 boiling curves for nucleate boiling onset through fully developed nucleate boiling).

**Key difference from v4.2.0 plan:** The v4.2.0 plan used a fixed-temperature smoothstep (195-245°F). This plan uses a **superheat-based** smoothstep where the reference temperature (T_sat) moves with pressure. This means:
- At 17 psia: boiling ramps between 220°F and 240°F (T_sat=220 + 0-20°F superheat)
- At 105 psia: boiling ramps between 338°F and 358°F (T_sat=338 + 0-20°F superheat)
- At 1092 psia: boiling ramps between 557°F and 577°F (T_sat=557 + 0-20°F superheat)

As secondary pressure rises, the boiling threshold rises with it, naturally self-limiting the boiling intensity. This is the dominant mechanism that prevents cliff behavior.

### Part 3: Heat Transfer Effectiveness Recalibration

The stagnant effectiveness factors need revision to produce realistic primary-to-secondary ΔT. Currently, the top node effectiveness is 0.40 with a bundle penalty of 0.40, giving an effective factor of 0.16 for the best-case node. This forces the model to need a ~150°F ΔT to transfer enough heat.

**Revised approach — increase effectiveness, let smaller ΔT drive heat transfer:**

The effectiveness factors were originally set low to prevent the SG from absorbing too much heat (the v2.x problem where 14-19 MW crashed the heatup rate). But that problem was caused by the circulation-onset model incorrectly activating full secondary circulation. With the thermocline model correctly limiting which nodes participate, we can afford higher effectiveness on the active nodes.

**Revised node stagnant effectiveness (top to bottom):**

| Node | Current | Revised | Rationale |
|------|---------|---------|-----------|
| Top (U-bend) | 0.40 | 0.70 | Heated water rises freely to top, good NC around U-bends |
| Upper-mid | 0.15 | 0.40 | Strong buoyancy plumes from tubes below carry heat upward |
| Middle | 0.05 | 0.15 | Moderate NC, thermocline typically in this region |
| Lower-mid | 0.02 | 0.05 | Below thermocline most of heatup |
| Bottom | 0.01 | 0.02 | Near tubesheet, stagnant |

**Bundle penalty factor:** Increase from 0.40 to 0.55. The 0.40 value was conservative for a tube bundle with P/D = 1.42. Published correlations for triangular pitch at this spacing show penalties of 0.3-0.7 depending on Rayleigh number. With forced primary circulation driving higher tube surface temperatures, the secondary NC is more vigorous and the effective penalty is less severe.

**Expected impact:**
- With higher effectiveness × higher bundle penalty, the required ΔT for a given heat transfer rate drops significantly
- Top node effective factor: 0.70 × 0.55 = 0.385 (vs current 0.40 × 0.40 = 0.16)
- This means ~2.4× more heat transfer per degree of ΔT
- The SG secondary will track the primary more closely (ΔT ~30-60°F instead of ~150°F)
- Combined with pressure feedback limiting boiling intensity, the heatup rate should be smoother

**Calibration approach:** The exact effectiveness values will need tuning after the pressure model is implemented. The values above are initial estimates based on the physical reasoning that with correct pressure feedback damping the boiling, higher effectiveness can be tolerated without the SG absorbing too much heat. Stage 4 includes a calibration/tuning pass.

---

## New Constants

### PlantConstants.SG.cs additions:

```csharp
#region SG Secondary Pressure Model (v4.3.0)

/// <summary>
/// Initial SG secondary pressure in psia during wet layup.
/// Atmospheric pressure (~14.7 psia) + nitrogen blanket (~2-3 psig).
/// Nitrogen blanket maintains slight positive pressure to prevent
/// air in-leakage during cold shutdown.
///
/// Source: NRC HRTD ML11251A016 — SG wet layup conditions,
///         NRC HRTD ML11223A342 Section 19.0 — nitrogen blanket
/// </summary>
public const float SG_INITIAL_PRESSURE_PSIA = 17f;

/// <summary>
/// RCS temperature at which SG nitrogen blanket is isolated in °F.
/// When RCS reaches ~220°F, steam formation begins in the SGs.
/// Nitrogen supply is isolated and the secondary becomes a closed
/// pressurizing vessel.
///
/// Source: NRC HRTD ML11223A342 Section 19.2.2:
///   "At approximately 220°F RCS temperature, steam formation begins
///    in the steam generators. The nitrogen supply to the steam
///    generators is now isolated."
/// </summary>
public const float SG_NITROGEN_ISOLATION_TEMP_F = 220f;

/// <summary>
/// Steam dump setpoint pressure in psig (steam pressure mode).
/// The steam dumps actuate when SG header pressure reaches this value,
/// which terminates the heatup by removing steam to the condenser.
/// 1092 psig corresponds to T_sat ≈ 557°F, the no-load Tavg setpoint.
///
/// Source: NRC HRTD ML11223A294 Section 11.2:
///   "The primary plant heatup is terminated by automatic actuation
///    of the steam dumps when the pressure inside the steam header
///    reaches 1092 psig."
/// </summary>
public const float SG_STEAM_DUMP_SETPOINT_PSIG = 1092f;

/// <summary>
/// SG safety valve setpoint in psig (lowest setting).
/// Model F SGs have 5 code safety valves per SG, with the lowest
/// setpoint at 1185 psig (±3%). This provides overpressure protection
/// if steam dumps fail to control secondary pressure.
///
/// Source: NRC HRTD ML11223A244 Section 7.1 — Main Steam Safety Valves,
///         Westinghouse FSAR Table 10.3-1
/// </summary>
public const float SG_SAFETY_VALVE_SETPOINT_PSIG = 1185f;

/// <summary>
/// Superheat range for boiling HTC ramp in °F.
/// The transition from single-phase natural convection to fully
/// developed nucleate boiling occurs over a superheat range of
/// approximately 15-20°F above the local saturation temperature.
///
/// ΔT_superheat = T_node - T_sat(P_secondary)
/// f_boil = smoothstep(ΔT_superheat / this_value)
///
/// Below 0°F superheat: no boiling enhancement (multiplier = 1.0)
/// At this_value superheat: full boiling enhancement (multiplier = max)
///
/// Source: Incropera & DeWitt Ch. 10 — Pool boiling curve,
///         onset of nucleate boiling through fully developed regime
/// </summary>
public const float SG_BOILING_SUPERHEAT_RANGE_F = 20f;

#endregion
```

### Revised Constants:

```csharp
// Revised stagnant effectiveness (see Part 3 rationale)
public const float SG_NODE_EFF_TOP_STAGNANT = 0.70f;      // was 0.40
public const float SG_NODE_EFF_UPPER_STAGNANT = 0.40f;     // was 0.15
public const float SG_NODE_EFF_MID_STAGNANT = 0.15f;       // was 0.05
public const float SG_NODE_EFF_LOWER_STAGNANT = 0.05f;     // was 0.02
public const float SG_NODE_EFF_BOTTOM_STAGNANT = 0.02f;    // was 0.01

// Revised bundle penalty
public const float SG_BUNDLE_PENALTY_FACTOR = 0.55f;       // was 0.40
```

---

## New State Variables

### SGMultiNodeState additions:

```csharp
/// <summary>Current SG secondary side pressure in psia</summary>
public float SecondaryPressure_psia;

/// <summary>True once nitrogen blanket has been isolated (steam onset)</summary>
public bool NitrogenIsolated;

/// <summary>Current saturation temperature at secondary pressure in °F</summary>
public float SaturationTemp_F;

/// <summary>Superheat of hottest node above T_sat in °F (0 if subcooled)</summary>
public float MaxSuperheat_F;
```

### SGMultiNodeResult additions:

```csharp
/// <summary>SG secondary pressure in psia</summary>
public float SecondaryPressure_psia;

/// <summary>Saturation temperature at current secondary pressure in °F</summary>
public float SaturationTemp_F;

/// <summary>True if nitrogen blanket has been isolated</summary>
public bool NitrogenIsolated;

/// <summary>Boiling intensity fraction (0 = subcooled, 1 = full boiling)</summary>
public float BoilingIntensity;
```

### HeatupSimEngine additions:

```csharp
[HideInInspector] public float sgSecondaryPressure_psia;   // SG secondary pressure
[HideInInspector] public float sgSaturationTemp_F;          // T_sat at SG pressure
[HideInInspector] public float sgMaxSuperheat_F;            // Max node superheat
[HideInInspector] public bool  sgNitrogenIsolated;          // N₂ blanket status
[HideInInspector] public float sgBoilingIntensity;          // Boiling ramp fraction (0-1)
```

---

## New Physics Methods

### In SGMultiNodeThermal.cs:

```csharp
/// <summary>
/// Update SG secondary pressure based on hottest node temperature.
/// 
/// Physics: Before steam onset, pressure = initial N₂ blanket pressure.
/// After steam onset (hottest node ≥ T_sat at current pressure), the
/// secondary is a closed two-phase vessel and pressure tracks the
/// saturation curve of the hottest node.
///
/// This quasi-static approach assumes the SG secondary is in
/// thermodynamic equilibrium — valid because the large water mass
/// (415,000 lb/SG) changes temperature slowly.
///
/// Source: NRC HRTD 19.2.2 — steam onset at ~220°F,
///         NRC HRTD 11.2 — steam dumps at 1092 psig
/// </summary>
private static void UpdateSecondaryPressure(ref SGMultiNodeState state, float T_rcs)

/// <summary>
/// Calculate boiling intensity fraction for a node based on local
/// superheat above the dynamic saturation temperature.
///
/// f_boil = smoothstep(ΔT_superheat / SG_BOILING_SUPERHEAT_RANGE_F)
/// where ΔT_superheat = max(0, T_node - T_sat(P_secondary))
///
/// Returns 0.0 if subcooled, 1.0 if fully developed nucleate boiling.
///
/// Source: Incropera & DeWitt Ch. 10 — boiling curve transition
/// </summary>
private static float GetBoilingIntensityFraction(float nodeTemp_F, float Tsat_F)
```

---

## Implementation Stages

### Stage 1: SG Secondary Pressure Model + State Variable Additions
**Files Modified:**
- `PlantConstants.SG.cs` — Add new pressure constants (SG_INITIAL_PRESSURE_PSIA, SG_NITROGEN_ISOLATION_TEMP_F, SG_STEAM_DUMP_SETPOINT_PSIG, SG_SAFETY_VALVE_SETPOINT_PSIG, SG_BOILING_SUPERHEAT_RANGE_F)
- `SGMultiNodeThermal.cs` — Add new fields to SGMultiNodeState and SGMultiNodeResult structs; add UpdateSecondaryPressure() method; initialize pressure in Initialize(); call UpdateSecondaryPressure() in Update() sequence

**Deliverables:**
- Secondary pressure tracks saturation curve of hottest node after steam onset
- Nitrogen isolation flag triggers at ~220°F RCS
- Pressure initializes at 17 psia, rises with temperature
- T_sat computed from WaterProperties.SaturationTemperature() at each step
- New state variables populated and included in result struct

**Validation:**
- At 150°F RCS: P_secondary = 17 psia, T_sat = 220°F
- At 300°F RCS (with SG secondary near ~280°F): P_secondary ≈ 50 psia, T_sat ≈ 281°F
- At 557°F RCS: P_secondary ≈ 1107 psia (1092 psig), T_sat ≈ 557°F
- ValidateModel() updated with pressure checks

### Stage 2: Dynamic Boiling Onset with Superheat-Based Smoothstep
**Files Modified:**
- `SGMultiNodeThermal.cs` — Replace step-function boiling check with dynamic T_sat comparison; add GetBoilingIntensityFraction() method; modify GetNodeHTC() to use superheat-based boiling ramp; update BoilingActive flag to use dynamic threshold
- `PlantConstants.SG.cs` — SG_BOILING_ONSET_TEMP_F retained but documented as initial-condition-only (onset at 17 psia); remove dependency on fixed threshold in runtime code

**Deliverables:**
- Boiling onset per node: T_node ≥ T_sat(P_secondary)
- HTC multiplier ramps from 1.0 to SG_BOILING_HTC_MULTIPLIER over SG_BOILING_SUPERHEAT_RANGE_F (20°F) of superheat
- As pressure rises, T_sat rises, naturally reducing superheat and boiling intensity
- BoilingActive flag set when any node has superheat > 0
- BoilingIntensity field reports peak f_boil fraction

**Validation:**
- At 17 psia (initial): boiling onset at node temps ≥ 220°F (same as before)
- At 105 psia: boiling onset shifts to node temps ≥ 338°F
- At 500 psia: boiling onset shifts to node temps ≥ 467°F
- No cliff behavior — verify heatup rate transitions gradually
- GetBoilingIntensityFraction() returns 0.0 for subcooled, 0.5 at 10°F superheat, 1.0 at 20°F+ superheat

### Stage 3: Heat Transfer Effectiveness Recalibration
**Files Modified:**
- `PlantConstants.SG.cs` — Update SG_NODE_EFF_*_STAGNANT values and SG_BUNDLE_PENALTY_FACTOR with documented rationale
- `SGMultiNodeThermal.cs` — Update ValidateModel() expected ranges for new effectiveness values

**Deliverables:**
- Top node effectiveness: 0.40 → 0.70
- Upper-mid: 0.15 → 0.40
- Middle: 0.05 → 0.15
- Lower-mid: 0.02 → 0.05
- Bottom: 0.01 → 0.02
- Bundle penalty: 0.40 → 0.55
- SG secondary temperature tracks closer to primary (~30-60°F ΔT instead of ~150°F)

**Validation:**
- Run heatup and verify ΔT between Tavg and SG top node stays within 20-80°F range
- Verify heatup rate remains in 40-55°F/hr range through mid-heatup
- Verify total SG heat absorption is in realistic range (2-8 MW during subcooled, stabilizing at no-load)

### Stage 4: Calibration and Tuning Pass
**Files Modified:**
- `PlantConstants.SG.cs` — Adjust effectiveness values and superheat range if needed
- `SGMultiNodeThermal.cs` — Adjust any physics calculations if initial run shows issues

**Deliverables:**
- Run full heatup simulation with all Stage 1-3 changes active
- Compare pressure progression against NRC HRTD expected values (table in "Expected Behavior" section)
- Verify no cliff at boiling onset — heatup rate should vary gradually
- Verify SG secondary pressure reaches ~1092 psig at Tavg ≈ 557°F
- Tune effectiveness values if ΔT is too large or too small
- Tune superheat range if boiling transition is too sharp or too gradual
- Document final tuned values with justification

**Validation Targets (Full Heatup):**

| Metric | Target | Acceptable Range |
|--------|--------|-----------------|
| Heatup rate (100-220°F) | 50°F/hr | 40-55°F/hr |
| Heatup rate (220-400°F) | 45°F/hr | 35-55°F/hr |
| Heatup rate (400-557°F) | 40°F/hr | 30-50°F/hr |
| Max heatup rate change between intervals | <15°F/hr | <20°F/hr (no cliff) |
| SG P_secondary at Tavg=300°F | ~50 psia | 30-70 psia |
| SG P_secondary at Tavg=557°F | ~1107 psia | 1050-1120 psia |
| Primary-secondary ΔT (subcooled) | 30-50°F | 15-80°F |
| Primary-secondary ΔT (boiling) | 10-30°F | 5-50°F |
| SG total absorption (subcooled) | 2-5 MW | 1-8 MW |

### Stage 5: Engine Wiring, ScreenDataBridge, and Logging
**Files Modified:**
- `HeatupSimEngine.cs` — Add new public state fields (sgSecondaryPressure_psia, sgSaturationTemp_F, sgMaxSuperheat_F, sgNitrogenIsolated, sgBoilingIntensity); wire SGMultiNodeResult new fields to engine state after Update() call
- `HeatupSimEngine.Logging.cs` — Add SG pressure, T_sat, superheat, boiling intensity, N₂ isolation to interval log output
- `ScreenDataBridge.cs` — Add new getters for SG pressure data and RHR state (see getter list below)

**New ScreenDataBridge Getters:**
```
// SG Secondary Pressure (v4.3.0)
GetSGSecondaryPressure_psia()    — SG secondary pressure in psia
GetSGSecondaryPressure_psig()    — SG secondary pressure in psig (psia - 14.7)
GetSGSaturationTemp()            — T_sat at current SG secondary pressure (°F)
GetSGMaxSuperheat()              — Max node superheat above T_sat (°F)
GetSGBoilingIntensity()          — Boiling ramp fraction (0.0-1.0)
GetSGNitrogenIsolated()          — N₂ blanket isolation status (bool)
GetSGThermoclineHeight()         — Thermocline position in ft (already may exist)
GetSGActiveAreaFraction()        — Active tube area fraction (already may exist)
GetSGBoilingActive()             — Any node boiling (bool)

// RHR System (v3.0.0 physics exist, getters missing)
GetRHRMode()                     — RHR mode string (Standby/Cooling/Heatup/Isolating)
GetRHRModeEnum()                 — RHR mode as int for color coding
GetRHRNetHeat_MW()               — Net RHR thermal effect (MW)
GetRHRHXRemoval_MW()             — RHR HX heat removal (MW)
GetRHRPumpHeat_MW()              — RHR pump heat input (MW)
GetRHRIsolationProgress()        — Isolation ramp progress (0.0-1.0)
GetRHRSuctionPressure()          — RHR suction pressure (psig)
GetRHRFlow()                     — RHR flow rate (gpm)
GetRHRHXInletTemp()              — RHR HX inlet temp (°F)
GetRHRHXOutletTemp()             — RHR HX outlet temp (°F)
```

**Deliverables:**
- All new SG and RHR state variables accessible via ScreenDataBridge
- Interval logs include: SG P_secondary, T_sat, max superheat, boiling intensity, N₂ isolation status
- All getters follow established NaN-placeholder convention
- RHR getters read from rhrState on HeatupSimEngine (physics already computed by RHRSystem.cs)

**Validation:**
- Verify all getters return correct values during heatup run
- Verify NaN returned when engine not available
- Log files contain all new parameters

### Stage 6: Operator Screen Integration — SG, RHR, and Plant Overview

These are critical monitoring parameters that operators need on their actual screens, not just the validation dashboard. Three existing screens need updates:

**Files Modified:**
- `SteamGeneratorScreen.cs` (Screen 5) — Wire new SG pressure data to existing gauge fields
- `AuxiliarySystemsScreen.cs` (Screen 8) — Wire RHR data to existing placeholder gauge fields
- `PlantOverviewScreen.cs` (Tab) — Add SG pressure and RHR status to overview
- `HeatupValidationVisual.cs` — Add SG/RHR thermal balance panel to validation dashboard

#### Screen 5 — Steam Generators: SG Pressure + Boiling Status

The SG screen already has `text_SGA/B/C/D_SteamPressure` fields that call `_data.GetSteamPressure()`. Currently this returns the steaming-detection pressure (from v1.1.0). With v4.3.0, this should return the new tracked `SecondaryPressure_psia` converted to psig. Additional displays:

| Parameter | Location | Getter | Status |
|-----------|----------|--------|--------|
| SG Steam Pressure (psig) | Right Panel #5-8 | GetSGSecondaryPressure_psig() | Currently placeholder per-SG, will show live lumped value |
| SG Secondary Temp (°F) | Quad viz overlay | GetSGSecondaryTemp() (existing) | Already live |
| SG Boiling Status | Bottom panel (replace CirculationFraction) | GetSGBoilingIntensity() | Replace deprecated "Circulation" display |
| SG T_sat (°F) | Bottom panel (new) | GetSGSaturationTemp() | New — shows boiling threshold |
| SG Heat Removal (MW) | Bottom panel (existing) | GetSGHeatTransfer() (existing) | Already live |
| N₂ Isolation Status | Bottom panel (new) | GetSGNitrogenIsolated() | New — "N₂ ISOLATED" / "N₂ BLANKETED" |

The deprecated `CirculationFraction` display and `text_CirculationFraction` field should be repurposed to show boiling intensity instead, since the circulation model was removed in v3.0.0.

#### Screen 8 — Auxiliary Systems: RHR Live Data

The Auxiliary Systems screen is currently 100% placeholder. With v3.0.0, the RHR physics model exists (`RHRSystem.cs`), and v4.3.0 Stage 5 adds ScreenDataBridge getters. Wire these to the existing placeholder fields:

| Parameter | Field | Getter | Status |
|-----------|-------|--------|--------|
| RHR-A Flow (gpm) | text_RHR_A_Flow | GetRHRFlow() | Was placeholder → now live |
| RHR-B Flow (gpm) | text_RHR_B_Flow | GetRHRFlow() | Same value (lumped model) |
| RHR HX-A Inlet Temp (°F) | text_RHR_HXA_InletTemp | GetRHRHXInletTemp() | Was placeholder → now live |
| RHR HX-A Outlet Temp (°F) | text_RHR_HXA_OutletTemp | GetRHRHXOutletTemp() | Was placeholder → now live |
| RHR HX-B Inlet/Outlet | text_RHR_HXB_* | Same getters | Lumped model, same values |
| RHR Suction Pressure (psig) | text_RHR_SuctionPressure | GetRHRSuctionPressure() | Was placeholder → now live |
| RHR Pump Status | text_RHR_PumpStatus | GetRHRMode() | Was placeholder → now live |
| RHR Status Diagram Text | diagram_RHR_StatusText | GetRHRMode() | Shows mode in center diagram |
| RHR Pump Indicators | indicator_RHR_A/B | GetRHRModeEnum() | Color-coded by mode |

Note: CCW and SW remain placeholder (no physics model). Only the RHR gauges on the left panel get live data.

#### Plant Overview (Tab): SG Pressure + RHR Status in Mimic

The Plant Overview mimic diagram already shows SG bodies with temperature-based coloring and steam pressure labels. Updates:

| Element | Current | v4.3.0 Update |
|---------|---------|---------------|
| SG mimic labels | Show steam pressure from GetSteamPressure() | Update to use GetSGSecondaryPressure_psig() for accurate tracked value |
| SG mimic body color | Based on GetSGSecondaryTemp() | Add boiling intensity glow/pulse when boiling active |
| Right panel "Steam Pressure" | GetSteamPressure() | Update to GetSGSecondaryPressure_psig() |
| Bottom panel (new) | No RHR indicator | Add RHR status indicator (ACTIVE/ISOLATING/SECURED) with color |

#### Heatup Validation Dashboard

Add a compact thermal balance section showing:
- Heat balance: Gross in (RCP + PZR heaters) | SG loss | RHR removal | Net to RCS
- SG pressure trend: Current P_secondary alongside RCS pressure
- Boiling status: SUBCOOLED / TRANSITION / BOILING with intensity bar
- RHR status: Mode + heat removal MW
- N₂ blanket status

**Validation:**
- Visual confirmation all three screens display live data during heatup
- Screen 5 shows SG pressure rising from ~2 psig through 1092 psig
- Screen 8 shows RHR active during early heatup, isolating at ~350°F, secured after
- Plant Overview shows updated SG pressure labels and RHR status indicator
- Dashboard shows thermal balance with all heat sources and sinks
- No placeholder "---" for any parameter that now has physics backing
- No performance impact from additional UI updates

### Stage 7: ValidateModel() Update, Documentation, and Changelog
**Files Modified:**
- `SGMultiNodeThermal.cs` — Comprehensive update of ValidateModel() with pressure-aware tests
- `SGMultiNodeThermal.cs` — Update file header documentation with v4.3.0 physics description
- `PlantConstants.SG.cs` — Update file header with v4.3.0 additions
- Write CHANGELOG_v4.3.0.md
- Update FUTURE_ENHANCEMENTS_ROADMAP.md

**Deliverables:**
- ValidateModel() tests include: pressure initialization, pressure progression, dynamic boiling onset, superheat ramp, no-cliff verification
- File headers updated with v4.3.0 physics description and sources
- Changelog documents all changes
- Roadmap updated with completed items and any new deferrals

---

## Unaddressed Issues

### 1. Mass Conservation Error (~5% at 14 hr)
Growing inventory audit error. Tracked under v1.2.0 in Future Features roadmap. Requires dedicated flow integration audit. **Not addressed in v4.3.0.**

### 2. PZR Level Control Saturation (PI Error at -600 %-hr)
CVCS PI controller saturated throughout post-bubble heatup. Functionally acceptable but may cause windup. **Planned for v1.1.0 HZP Stabilization.**

### 3. SG Draining (wet layup → operating level)
SG at 100% wet layup throughout. Per NRC HRTD 19.0, draining starts at ~200°F. **Tracked in v3.1.0 roadmap items.** Note: draining affects secondary mass and therefore pressure response. This interaction should be considered when v3.1.0 is implemented but is not critical for v4.3.0 (the pressure model will work with full or partial secondary mass).

### 4. Steam Dump Integration
The existing `SteamDumpController.cs` handles steam pressure mode at 1092 psig. The v4.3.0 pressure model provides the input signal (SG secondary pressure) that the steam dump controller needs. Full integration of steam dump actuation to terminate heatup is **tracked under v1.1.0 HZP Stabilization** where the steam dump model was originally scoped. v4.3.0 provides the necessary prerequisite (pressure signal) but does not modify the steam dump controller itself.

### 5. VCT Level Validation Failure
Cosmetic validation issue with divert logic. **Deferred — not addressed in v4.3.0.**

### 6. RCS Rate >50°F/hr Intervals
Several intervals exceed 50°F/hr target. **Deferred to HZP Stabilization — may require heatup rate limiting.**

### 7. Active Operator Pressure Management
The supplementary PWR heatup literature notes that operators actively manage SG secondary pressure by stepping it up to track primary temperature. Our model uses a quasi-static equilibrium approach (pressure = P_sat of hottest node) which is a simplification. In reality, operators use atmospheric dump valves, turbine bypass, and steam line drains to control pressure. This active management is **planned for future release** when operator interaction systems are implemented. The quasi-static model is a reasonable approximation for the automated simulation and will be noted in the Future Features roadmap.

---

## Disposition of v4.2.0 Issues

| v4.2.0 Issue | v4.3.0 Disposition |
|---|---|
| Issue 1: PZR Level vs Pressure | **No change needed** — physically realistic per v4.2.0 analysis |
| Issue 2: Boiling cliff (48→5°F/hr) | **Superseded** — addressed by Stages 1-3 (pressure feedback + dynamic boiling + recalibration) |
| Issue 3: Dashboard enhancement | **Carried forward** to Stage 5 with expanded scope (includes pressure display) |

The v4.2.0 smoothstep-only approach (fixed-temperature ramp at 195-245°F) is **not implemented** because the pressure-dependent superheat ramp is physically superior — it moves the boiling threshold with pressure rather than applying a fixed band. The smoothstep math is the same but the reference temperature is dynamic.

---

## References

- NRC HRTD ML11223A342 Section 19.0-19.2.2 — Plant Operations, Heatup Procedures
- NRC HRTD ML11223A294 Section 11.2 — Steam Dump Control System
- NRC HRTD ML11223A213 Section 5.0 — Steam Generators
- NRC HRTD ML11223A244 Section 7.1 — Main and Auxiliary Steam
- NRC HRTD ML11223A195 Section 1.2 — PWR Control Modes
- NRC HRTD ML11251A016 — SG Wet Layup Conditions
- WCAP-14040-NP-A Rev. 2 — Cold Overpressure Mitigation Methodology
- Incropera & DeWitt, Fundamentals of Heat and Mass Transfer, Ch. 9-10
- NIST Steam Tables (via WaterProperties.cs — SaturationTemperature, SaturationPressure)
- NUREG/CR-5426 — PWR SG Natural Circulation Phenomena
- NRC Bulletin 88-11 — Thermal Stratification in PWR Systems
- Supplementary PWR heatup operational guidance (provided in conversation context)
- Build/HeatupLogs/Heatup_Interval_001 through _117
- SGMultiNodeThermal.cs — v3.0.0 thermocline model
- PlantConstants.SG.cs — SG design parameters
- WaterProperties.cs — NIST-validated steam table correlations
- SteamThermodynamics.cs — Two-phase calculations
- SteamDumpController.cs — Steam pressure mode control
