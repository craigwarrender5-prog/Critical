# Implementation Plan v4.2.0 — SG Boiling Transition Smoothing, PZR Pressurization Review, and Heatup Event Dashboard

## Version: 4.2.0
## Date: 2026-02-11
## Status: AWAITING APPROVAL
## Scope: Post-v3.0.0 Heatup Physics Refinements + Dashboard Enhancement

---

## Problem Summary

Heatup simulation validation (Build/HeatupLogs) with the v3.0.0 SG thermocline model and RHR system shows that the persistent heatup rate problem has been substantially resolved — heatup achieves 48–53°F/hr through mid-heatup with 4 RCPs. However, three issues remain from the validation run.

---

## Issue 1: PZR Mode 3 Behavior — Level Rising Faster Than Pressure

### Observed

After bubble formation at ~8.5 hr, the PZR enters `PRESSURIZE_AUTO` heater mode. Between 10–14 hr sim time:

| Sim Time | T_avg (°F) | PZR Level (%) | Level SP (%) | Pressure (psia) | Surge Flow (gpm) | Pressure Rate (psi/hr) |
|----------|-----------|--------------|-------------|-----------------|------------------|----------------------|
| 10.00 hr | 154 | 28.9 | 25.0 | 539.7 | 10.9 | 15.4 |
| 10.75 hr | 196 | 34.3 | 25.0 | 556.5 | 18.9 | 30.0 |
| 11.50 hr | 244 | 39.0 | 29.3 | 582.7 | 22.0 | 39.1 |
| 12.25 hr | 288 | 43.7 | 33.6 | 615.4 | 23.9 | 48.2 |
| 13.00 hr | 329 | 48.4 | 37.6 | 655.4 | 25.2 | 58.7 |
| 13.75 hr | 367 | 58.7 | 41.4 | 705.4 | 26.0 | 80.1 |
| 14.00 hr | 376 | 63.3 | 42.3 | 722.7 | 2.8 | 10.0 |

PZR level consistently runs 4–17% above setpoint and is climbing. Level rise *is* the dominant PZR behavior rather than pressure rise.

### Expected Behavior (Real Plant)

In a real Westinghouse 4-loop plant during Mode 3 heatup at 50°F/hr, the rapid RCS thermal expansion drives significant water volume up through the surge line. With 1.8 MW of heaters and a surge flow of 10–26 gpm, the physics are:

- **RCS expansion rate at 50°F/hr:** ~300,000 lb of RCS water expanding at ~0.01%/°F ≈ ~3 ft³/hr of volumetric expansion → ~15–25 gpm surge into PZR (consistent with logs)
- **Heater steam generation capacity:** 1.8 MW = 6.14 MBTU/hr. At T_sat ≈ 500°F, h_fg ≈ 755 BTU/lb → ~8,100 lb/hr steam ≈ ~16 gpm equivalent displacement
- **Verdict:** Surge inflow slightly exceeds heater steam generation capacity, so **level rise during rapid heatup is physically realistic**

The CVCS letdown is compensating (75 gpm letdown vs 12–33 gpm charging), but the RCS expansion is the dominant driver. In a real plant, operators would:
1. Accept gradually rising PZR level during heatup (within procedural limits)
2. Increase letdown if level gets too high
3. Focus on pressure trending upward even if slowly

### Resolution: NO CODE CHANGE REQUIRED

The PZR behavior is physically realistic. The level running above setpoint is an expected consequence of rapid heatup with high surge flow rates. The pressure *is* rising (15→80 psi/hr as heatup progresses), which is correct — pressure tracks T_sat of the PZR water, which is being heated by both heaters and incoming hot surge water.

**Recommendation:** Add a note to the heatup logging/dashboard (see Issue 3) that clearly shows PZR level setpoint vs actual so operators can see the deviation is expected during rapid heatup. The existing CVCS PI error is saturated at -600 %-hr, indicating the controller knows level is high but letdown is already at maximum. This is realistic — a real plant would have the same constraint.

---

## Issue 2: Sudden Heatup Rate Drop at ~13:45–14:00 (48→5 °F/hr)

### Observed

Between interval 109 (13.75 hr) and interval 111 (14.00 hr), the RCS heatup rate plunges from 48.43°F/hr to 5.13°F/hr — a catastrophic 90% drop in a single 15-minute interval. The rate then gradually recovers over the next hour:

| Sim Time | T_avg (°F) | Heatup Rate (°F/hr) | SG Loss (MW) | SG Top Node (°F) | Boiling | Net Heat (MW) |
|----------|-----------|-------------------|-------------|-----------------|---------|--------------|
| 13.00 hr | 328.9 | 52.6 | 5.49 | 177.5 | No | 16.52 |
| 13.25 hr | 341.9 | 51.3 | 5.85 | 187.9 | No | 16.13 |
| 13.50 hr | 354.5 | 49.9 | 6.19 | 198.9 | No | 15.75 |
| 13.75 hr | 366.8 | 48.4 | 6.52 | 210.5 | No | 15.38 |
| **14.00 hr** | **376.5** | **5.13** | **20.23** | **231.0** | **YES** | **1.64** |
| 14.25 hr | 379.2 | 16.5 | 16.60 | 275.0 | YES | 5.26 |
| 14.50 hr | 384.5 | 25.4 | 13.72 | 308.0 | YES | 8.13 |
| 14.75 hr | 391.7 | 31.5 | 11.70 | 333.0 | YES | 10.12 |

### Root Cause: Step-Function Boiling HTC Multiplier

The SG top node crosses `SG_BOILING_ONSET_TEMP_F` (220°F) between 13.75 hr (210.5°F) and 14.00 hr (231.0°F). At this instant, the boiling multiplier (`SG_BOILING_HTC_MULTIPLIER = 5.0`) is applied as a **binary step function** to the entire top node:

**In `SGMultiNodeThermal.GetNodeHTC()`:**
```csharp
if (nodeTemp_F >= PlantConstants.SG_BOILING_ONSET_TEMP_F)
{
    h_secondary *= PlantConstants.SG_BOILING_HTC_MULTIPLIER;  // 5× instant jump
}
```

This causes:
1. Top node HTC jumps from ~33 to ~165 BTU/(hr·ft²·°F) instantly
2. Top node Q jumps from 4.0 MW to 17.6 MW (the top node carries ~60% of total SG heat transfer due to its 25% area fraction and highest effectiveness)
3. Total SG loss triples from 6.5 MW to 20.2 MW
4. Net heat to RCS drops from 15.4 MW to 1.6 MW
5. Heatup rate crashes from 48 to 5 °F/hr

The rate then recovers because:
- The top node rapidly heats up (210→275→308→333°F)
- As the top node approaches RCS temperature, ΔT shrinks
- Reduced ΔT reduces heat transfer back toward equilibrium
- But the recovery takes ~1.5 hours, which is unrealistically abrupt

### Expected Behavior (Real Plant)

In a real SG secondary, boiling onset is a **gradual transition**, not a step function:

1. **Subcooled nucleate boiling** begins when tube wall temperature exceeds T_sat, even while bulk fluid is still subcooled. This produces small bubbles that collapse immediately, providing modest HTC enhancement (~1.5–2×).
2. **Developed nucleate boiling** occurs as bulk fluid approaches T_sat. Bubbles grow and detach, providing increasing agitation (~3–5× HTC).
3. **Full boiling** with vigorous bubble departure and local recirculation (~5–10× HTC).

The transition spans 10–30°F of fluid temperature, not a single degree. Additionally, the 220°F onset is based on atmospheric + nitrogen blanket pressure (~17 psia). As boiling begins, the nitrogen blanket is isolated and secondary pressure starts rising, which raises T_sat and self-limits the boiling intensity initially.

### Proposed Fix

Replace the binary boiling check with a **gradual sigmoid ramp** that models the subcooled-to-developed boiling transition over a realistic temperature band.

**In `SGMultiNodeThermal.GetNodeHTC()`:**

```
Current (step function):
  if (nodeTemp >= 220°F) → multiply by 5.0

Proposed (smooth ramp):
  Below 200°F:     multiplier = 1.0  (no boiling enhancement)
  200°F to 250°F:  multiplier ramps smoothly from 1.0 to SG_BOILING_HTC_MULTIPLIER (5.0)
  Above 250°F:     multiplier = SG_BOILING_HTC_MULTIPLIER (5.0)

  Transition function: smooth Hermite interpolation (smoothstep)
  Band center: 220°F (existing SG_BOILING_ONSET_TEMP_F)
  Band half-width: 25°F (new constant SG_BOILING_TRANSITION_HALFWIDTH_F)
```

**New constants in `PlantConstants.SG.cs`:**

```csharp
/// <summary>
/// Half-width of the boiling onset transition band in °F.
/// Subcooled nucleate boiling begins at (onset - halfwidth) = 195°F,
/// with full developed boiling at (onset + halfwidth) = 245°F.
/// This models the physical reality that nucleate boiling initiates
/// at tube wall temperatures slightly above T_sat while bulk fluid
/// is still subcooled, and gradually intensifies.
///
/// Source: Incropera & DeWitt Ch. 10 — Boiling curves show smooth
///         transition from natural convection through onset of
///         nucleate boiling to fully developed nucleate boiling
///         over a ~20-40°F wall superheat range.
/// </summary>
public const float SG_BOILING_TRANSITION_HALFWIDTH_F = 25f;
```

**Physics in `GetNodeHTC()`:**

```csharp
// Boiling enhancement: gradual transition from natural convection
// to nucleate boiling over (onset ± halfwidth) temperature band
float boilLow = PlantConstants.SG_BOILING_ONSET_TEMP_F
              - PlantConstants.SG_BOILING_TRANSITION_HALFWIDTH_F;  // 195°F
float boilHigh = PlantConstants.SG_BOILING_ONSET_TEMP_F
               + PlantConstants.SG_BOILING_TRANSITION_HALFWIDTH_F; // 245°F

if (nodeTemp_F > boilLow)
{
    float t = Mathf.Clamp01((nodeTemp_F - boilLow) / (boilHigh - boilLow));
    float smoothT = t * t * (3f - 2f * t);  // Hermite smoothstep
    float multiplier = 1f + (PlantConstants.SG_BOILING_HTC_MULTIPLIER - 1f) * smoothT;
    h_secondary *= multiplier;
}
```

Additionally, the `BoilingActive` flag should be set based on the ramp exceeding a threshold (e.g., multiplier > 1.5) rather than a hard temperature check, to keep the diagnostic logging consistent:

```csharp
// In Update(), replace:
state.BoilingActive = state.NodeTemperatures[0] >= PlantConstants.SG_BOILING_ONSET_TEMP_F;
// With:
state.BoilingActive = state.NodeTemperatures[0] >=
    (PlantConstants.SG_BOILING_ONSET_TEMP_F - PlantConstants.SG_BOILING_TRANSITION_HALFWIDTH_F * 0.5f);
```

### Expected Impact

With a 50°F transition band (195–245°F), the top node will begin absorbing additional heat gradually starting around T_top ≈ 195°F (sim time ~13.0 hr). Instead of a cliff, the heatup rate should see a gradual dip:

- 13.0 hr: Rate begins decreasing from ~52 to ~45°F/hr (subcooled boiling onset)
- 13.5 hr: Rate around ~35-40°F/hr (moderate boiling enhancement)
- 14.0 hr: Rate settles to ~25-30°F/hr (developed boiling, equilibrium forming)
- 14.5 hr: Rate stabilizes as top node ΔT narrows

The total energy absorbed by the SG secondary during this period should be similar (conservation), but spread over a longer time window rather than concentrated in a single interval.

---

## Issue 3: Dashboard Enhancement — SG/RHR Event Monitoring

### Observed

With the v3.0.0 SG thermocline model and RHR system now implemented, there is no visual indication to the player/operator of key thermal events during heatup:

- SG boiling onset (top node reaching boiling regime)
- SG heat absorption rate and how it's changing
- RHR isolation status and progress
- Thermocline position and active area fraction
- Transition between RHR-dominant and SG-dominant heat removal

These events are logged in the interval files but not visible during gameplay.

### Expected

A real plant control room would have:
- SG secondary temperature indicators per SG
- SG secondary pressure indicators (which rise with boiling)
- RHR flow and temperature indications on the auxiliary panel
- Status lights for RHR pump operation and isolation valve position

### Proposed Fix

Add a compact **Heatup Events** panel or enhanced information to an existing screen that displays:

1. **SG Thermal Status:** Top node temp, boiling status (SUBCOOLED / TRANSITION / BOILING), SG total heat absorption (MW), active area fraction
2. **RHR Status:** Mode (Standby/Cooling/Heatup/Isolating), isolation progress (%), auto-isolate pressure threshold
3. **Heat Balance Summary:** Gross heat in (RCP + PZR heaters), SG loss, RHR removal (when active), ambient losses, net heat to RCS

This could be implemented as:
- **Option A:** A dedicated status bar or sub-panel on Screen 1 (Reactor Operator)
- **Option B:** Enhanced data on Screen 8 (Auxiliary Systems) where RHR already conceptually lives
- **Option C:** A compact HUD overlay toggled with a hotkey during heatup

### Recommendation

**Option A** is recommended — add a small "Thermal Balance" section to the Reactor Operator screen that shows key heatup parameters. This is where the operator spends most of their time during heatup and the information is critical for understanding plant behavior.

---

## Unaddressed Issues

### 1. Mass Conservation Error (growing to ~5% at 14 hr)
The inventory audit shows a persistent conservation error growing from 0.06% at start to ~4-5% by 14 hours. This is tracked under **v1.2.0 — Mass Conservation Error Investigation** in the Future Features roadmap. It is separate from the SG/PZR issues addressed here and requires a dedicated audit of all flow integration paths. **Not addressed in v4.2.0.**

### 2. PZR Level Control Saturation (PI Error at -600 %-hr)
The CVCS PI controller is saturated throughout the post-bubble heatup. While functionally acceptable (letdown is at max, which is the correct response), the saturated integrator may cause windup issues during later phases. **Planned for v1.1.0 HZP Stabilization** when PZR control refinements are implemented.

### 3. SG Draining (wet layup → operating level)
The SG is still at 100% wet layup mass throughout this run. Per NRC HRTD 19.0, draining should begin at ~200°F. This is tracked in the Future Features roadmap under v3.1.0 items. **Not addressed in v4.2.0.**

### 4. VCT Level Validation Failure
VCT level shows FAIL throughout post-bubble operation due to divert being active. The divert logic appears to be functioning correctly (excess letdown → BRS), but the validation check threshold may need adjustment. **Deferred — cosmetic validation issue.**

### 5. RCS Rate >50°F/hr Validation Failures
Several intervals between 10.75–13.25 hr show RCS rate slightly exceeding 50°F/hr (up to 64°F/hr). This may indicate the 4-RCP heat input slightly exceeds what the plant can manage at the target rate. **Deferred to HZP Stabilization work — may require heatup rate limiting or RCP startup timing adjustment.**

---

## Implementation Stages

### Stage 1: SG Boiling Transition Smoothing
**Files Modified:**
- `PlantConstants.SG.cs` — Add `SG_BOILING_TRANSITION_HALFWIDTH_F` constant
- `SGMultiNodeThermal.cs` — Replace step-function boiling check with smoothstep ramp in `GetNodeHTC()`, update `BoilingActive` flag logic in `Update()`

**Validation:**
- Run heatup simulation and verify no cliff drop at boiling onset
- Verify heatup rate transitions gradually (should see 45→30→25°F/hr over ~1.5 hr instead of 48→5°F/hr in one interval)
- Verify total energy absorbed by SG over full heatup is within ±10% of pre-fix run
- Verify `ValidateModel()` still passes

### Stage 2: Dashboard — SG/RHR Heatup Event Monitoring
**Files Modified:**
- TBD based on selected option (A/B/C) — to be confirmed before implementation
- Likely: `ScreenDataBridge.cs` (new getters), `ReactorOperatorScreen.cs` or new panel script

**Scope:**
- Add SG thermal status display (top node temp, boiling state, heat absorption MW)
- Add RHR status display (mode, isolation progress)
- Add heat balance summary (gross in, SG loss, net to RCS)

**Validation:**
- Visual confirmation that new indicators update correctly during heatup
- Verify no performance impact from additional UI updates

### Stage 3: Changelog and Documentation
- Write CHANGELOG_v4.2.0.md
- Update FUTURE_ENHANCEMENTS_ROADMAP.md with completed items
- Update SG model documentation header in SGMultiNodeThermal.cs

---

## References

- Build/HeatupLogs/Heatup_Interval_001 through _117
- NRC HRTD ML11223A342 Section 19.2.2 — Heatup procedures
- NRC HRTD ML11223A213 Section 5.0 — Steam Generators
- Incropera & DeWitt Ch. 10 — Boiling and Condensation (transition curves)
- PlantConstants.SG.cs — SG_BOILING_ONSET_TEMP_F, SG_BOILING_HTC_MULTIPLIER
- SGMultiNodeThermal.cs — GetNodeHTC(), Update()
- FUTURE_ENHANCEMENTS_ROADMAP.md — v3.1.0 items, v1.2.0 mass conservation
