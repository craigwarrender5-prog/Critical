# Implementation Plan — v2.0.10: Inventory Audit, Heater Smoothing, RCP Start Delay

**Version:** 2.0.10  
**Date:** 2026-02-10  
**Status:** IMPLEMENTED  
**Matching Changelog:** `Changelogs/CHANGELOG_v2.0.10.md`

---

## Problem Summary

Three bugs are addressed in this patch, all related to unrealistic simulation behaviour:

### Bug 1: Inventory Conservation Error (Bookkeeping Gap)

The inventory audit in `HeatupSimEngine.Logging.cs` uses a hybrid mass calculation: PZR water mass is state-based (`volume × density`), but RCS water mass reads the flow-integrated field `rcsWaterMass`. During solid pressurizer operations, `UpdateRCSInventory()` is guarded off, so thermal expansion mass flowing from PZR to RCS via the surge line is never credited to `rcsWaterMass`. The result is a monotonically growing conservation error:

| Sim Time | Error (lbm) | Error (%) |
|----------|-------------|-----------|
| 0.25 hr  | 482         | 0.05%     |
| 4.00 hr  | 7,734       | 0.84%     |
| 8.00 hr  | 17,611      | 1.90%     |

### Bug 2: Heater Oscillation During Bubble Formation (Bang-Bang Control)

After CCP starts and during the PRESSURIZE phase, the `BUBBLE_FORMATION_AUTO` and `PRESSURIZE_AUTO` heater modes use a **stateless, instantaneous** pressure-rate throttle in `CVCSController.CalculateHeaterState()`:

```csharp
fraction = 1.0 - (absPressureRate - maxRate) / maxRate;
fraction = Max(minFraction, Min(fraction, 1.0));
```

This recalculates from scratch every timestep with no memory, smoothing, or rate limiting. The result is heaters oscillating between 100% and 20% (the floor) every few timesteps — equivalent to flicking a switch on and off. Realistic behaviour would be heaters throttling to a stable middle value (~60%) and making ±5% adjustments in response to pressure changes.

### Bug 3: Unrealistic 1-Hour RCP Start Delay

`RCPSequencer.RCP1_START_DELAY = 1.0f` (1 hour) has no Westinghouse procedural basis. Per NRC HRTD Section 19.2.2: *"When the pressure exceeds the RCP Net Positive Suction Head (NPSH) requirements, the RCPs can be started."* The only prerequisites are:

1. Steam bubble exists in pressurizer
2. Pressure ≥ 320 psig for adequate NPSH
3. Seal injection available (CCP running)

There is no mandated waiting period. A realistic delay for operator verification activities (checking seal injection flow, aligning breakers, verifying prerequisites) is ~10 minutes, not 60.

---

## Expectations

After this patch:

1. **Inventory audit** conservation error < 0.1% throughout the entire heatup simulation, using consistent state-based mass calculations for all volumes.
2. **Heater power** during bubble formation/pressurization settles to a stable middle value with smooth ±5% adjustments — no wild swings between 20% and 100%.
3. **First RCP starts** ~10 minutes after bubble formation is complete and pressure ≥ 320 psig, matching Westinghouse procedure.

---

## Proposed Fix

### Stage 1: State-Based Inventory Audit

**Files Modified:**
- `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs` — `UpdateInventoryAudit()` method only

**Change:**  
Replace the line that reads the flow-integrated `rcsWaterMass` field:

```csharp
// CURRENT (broken):
inventoryAudit.RCS_Mass_lbm = rcsWaterMass;

// NEW (state-based):
float rcsLoopVolume_ft3 = PlantConstants.RCS_WATER_VOLUME - PlantConstants.PZR_TOTAL_VOLUME;
float rcsWaterDensity_audit = WaterProperties.WaterDensity(T_rcs, pressure);
inventoryAudit.RCS_Mass_lbm = rcsLoopVolume_ft3 * rcsWaterDensity_audit;
```

This mirrors how a real plant computer calculates RCS inventory: fixed geometric volume × water density at current T and P. No flow integration needed.

**Impact:** Only the inventory audit's **reporting** of RCS mass changes. The flow-integrated `rcsWaterMass` field used by the physics engine (CVCS controller, PZR level response, etc.) is not modified.

**GOLD Standard Impact:** `HeatupSimEngine.Logging.cs` is GOLD. Change is limited to a single mass calculation line in `UpdateInventoryAudit()`. No physics behaviour changes.

---

### Stage 2: Smoothed Heater Control During Bubble Formation

**Files Modified:**
- `Assets/Scripts/Physics/CVCSController.cs` — `CalculateHeaterState()` method, `BUBBLE_FORMATION_AUTO` and `PRESSURIZE_AUTO` cases
- `Assets/Scripts/Validation/HeatupSimEngine.cs` — Add `bubbleHeaterSmoothedOutput` field for state persistence

**Change:**  
Add a smoothed output field that persists between timesteps, with rate limiting:

1. **Add state field** in `HeatupSimEngine.cs`:
   ```csharp
   [HideInInspector] public float bubbleHeaterSmoothedOutput = 1.0f;
   ```

2. **Modify `CalculateHeaterState()`** signature to accept the current smoothed output and return the updated value. For the `BUBBLE_FORMATION_AUTO` and `PRESSURIZE_AUTO` cases, instead of snapping to the calculated fraction:

   ```csharp
   // Calculate target fraction from pressure rate (same formula as before)
   float targetFraction = 1.0f;
   if (absPressureRate > maxRate && maxRate > 0f)
   {
       targetFraction = 1.0f - (absPressureRate - maxRate) / maxRate;
       targetFraction = Math.Max(minFraction, Math.Min(targetFraction, 1.0f));
   }

   // Rate-limit the output: max change of 10% per hour (scaled by dt)
   // At 10-sec timesteps (dt=1/360 hr), max change per step = 0.10/360 ≈ 0.028%
   // This means full travel 20%→100% takes ~8 minutes — realistic valve travel
   float maxChangePerHr = 6.0f;  // matches HEATER_RATE_LIMIT_PER_HR
   float maxStep = maxChangePerHr * dt_hr;
   float delta = targetFraction - smoothedOutput;
   delta = Math.Max(-maxStep, Math.Min(delta, maxStep));
   smoothedOutput += delta;
   smoothedOutput = Math.Max(minFraction, Math.Min(smoothedOutput, 1.0f));

   state.HeaterFraction = smoothedOutput;
   state.HeaterPower_MW = baseHeaterPower_MW * smoothedOutput;
   ```

3. **Pass `dt_hr`** to `CalculateHeaterState()` — currently it doesn't receive a timestep. Add an optional `float dt_hr = 0f` parameter (backward compatible). When dt_hr is 0 or the mode is not bubble/pressurize, behaviour is unchanged.

4. **Pass and update `bubbleHeaterSmoothedOutput`** in `StepSimulation()` where `CalculateHeaterState` is called.

**Expected Result:** Heater power smoothly converges to a stable operating point (~60% at typical pressure rates) with ±5% variation — no more bang-bang oscillation.

**GOLD Standard Impact:** `CVCSController.cs` is GOLD. Change adds rate-limiting to existing modes only. No new modes, no formula changes, no physics model changes. `HeatupSimEngine.cs` is GOLD — adds one serialized float field.

---

### Stage 3: Correct RCP Start Delay

**Files Modified:**
- `Assets/Scripts/Physics/RCPSequencer.cs` — `RCP1_START_DELAY` constant only

**Change:**
```csharp
// CURRENT (unrealistic):
public const float RCP1_START_DELAY = 1.0f;  // 1 hour — no procedural basis

// NEW (realistic):
/// <summary>
/// Delay from bubble formation complete to first RCP start (hours).
/// Per NRC HRTD 19.2.2: RCPs can start once bubble exists and P ≥ 320 psig.
/// No mandated wait. 10-minute delay models operator verification activities
/// (checking seal injection, aligning breakers, verifying prerequisites).
/// </summary>
public const float RCP1_START_DELAY = 10f / 60f;  // 10 minutes
```

**Expected Result:** First RCP starts ~10 minutes after bubble formation completes (assuming pressure already ≥ 320 psig). Subsequent RCPs follow at 30-minute intervals (unchanged `RCP_START_INTERVAL = 0.5f`).

**GOLD Standard Impact:** `RCPSequencer.cs` is GOLD. Single constant value change. All logic unchanged. Validation tests in `ValidateCalculations()` will need their expected times updated to match the new delay.

---

## Unaddressed Issues

| Issue | Reason | Future Release |
|-------|--------|----------------|
| Flow-integrated `rcsWaterMass` still stale during solid ops | Physics engine uses it for CVCS/PZR response — fixing requires broader refactor. Audit is now state-based so conservation is correct regardless. | v1.2.0 or later |
| `RCP_START_INTERVAL` (30 min between pumps) not validated against Westinghouse | Current value is reasonable. Could be verified in a future pass. | N/A unless evidence found |
| Heater proportional/backup group separation during bubble formation | Currently modeled as single block. Full group staging deferred to AUTOMATIC_PID mode. | v1.1.0 (HZP scope) |

---

## Files Summary

| File | Change Type | GOLD? |
|------|------------|-------|
| `HeatupSimEngine.Logging.cs` | Modify 1 line in `UpdateInventoryAudit()` | Yes |
| `CVCSController.cs` | Add rate-limiting to `BUBBLE_FORMATION_AUTO` / `PRESSURIZE_AUTO` cases; add `dt_hr` parameter | Yes |
| `HeatupSimEngine.cs` | Add 1 field `bubbleHeaterSmoothedOutput` | Yes |
| `RCPSequencer.cs` | Change 1 constant `RCP1_START_DELAY` from 1.0 to 0.167; update validation test expected values | Yes |
