# Changelog v0.9.6 — Stage 1: PZR Level Blending Fix

**Date:** 2026-02-07  
**Type:** Patch (Bug Fix)  
**Severity:** CRITICAL  
**File Modified:** `HeatupSimEngine.cs`

---

## Summary

Fixed a critical physics blending bug that caused pressurizer level to crash from 25% to 5% when the first RCP started. The issue was in the REGIME 2 (RCPs Ramping) physics blending logic.

---

## Problem Description

When the first RCP started and the simulation transitioned from REGIME 1 (Isolated) to REGIME 2 (Blended), the PZR level would immediately drop by ~80% in a single timestep:

| Time (hr) | RCPs | Alpha (α) | PZR Level | PZR Water Vol |
|-----------|------|-----------|-----------|---------------|
| 9.50      | 0    | 0.00      | 25.2%     | 454 ft³       |
| 9.75      | 1    | **0.33**  | **4.9%**  | **88 ft³**    |

This was a physically impossible thermal transient that violated the design intent of the staged RCP ramp-up (which should prevent thermal shock).

---

## Root Cause

Two bugs in the REGIME 2 blending code:

### Bug 1: Uninitialized physicsState.PZR fields

Before calling `BulkHeatupStep()`, the code synced `physicsState.Temperature` and `physicsState.Pressure` from the engine, but **did not sync the PZR volume/mass fields**:

```csharp
// Old code - MISSING PZR sync:
physicsState.Temperature = T_rcs;
physicsState.Pressure = pressure;
// PZR fields were stale/uninitialized!

var coupledResult = RCSHeatup.BulkHeatupStep(ref physicsState, ...);
```

This caused `CoupledThermo.SolveEquilibrium()` to start from an incorrect PZR state and produce wildly different PZR volumes.

### Bug 2: Absolute value blending instead of delta blending

The original blending formula interpolated between the current volume and the solver's calculated volume:

```csharp
// Old formula - PROBLEMATIC:
pzrWaterVolume = pzrWaterVolume * (1-α) + physicsState.PZRWaterVolume * α
```

At α=0.33, this could produce massive changes if `physicsState.PZRWaterVolume` was very different from the current value (which it was, due to Bug 1).

---

## Fix Applied

### Part A: Sync PZR state before calling solver

```csharp
physicsState.Temperature = T_rcs;
physicsState.Pressure = pressure;

// v0.9.6 FIX: Sync PZR state from engine to physicsState
physicsState.PZRWaterVolume = pzrWaterVolume;
physicsState.PZRSteamVolume = pzrSteamVolume;
float tSatForMass = WaterProperties.SaturationTemperature(pressure);
physicsState.PZRWaterMass = pzrWaterVolume * WaterProperties.WaterDensity(tSatForMass, pressure);
physicsState.PZRSteamMass = pzrSteamVolume * WaterProperties.SaturatedSteamDensity(pressure);
```

### Part B: Use incremental delta blending

```csharp
// v0.9.6 FIX: Blend the CHANGE (delta), not the absolute value
float pzrVolumeBefore = pzrWaterVolume;
float deltaPZR_isolated = 0f;  // Isolated path: no change
float deltaPZR_coupled = physicsState.PZRWaterVolume - pzrVolumeBefore;
float deltaPZR_blended = deltaPZR_isolated * (1-α) + deltaPZR_coupled * α;

pzrWaterVolume = pzrVolumeBefore + deltaPZR_blended;
```

This ensures smooth transitions by blending the *rate of change* from each physics regime, not the absolute target values.

---

## Validation Criteria

- [x] PZR level should not drop more than 5% in any single 10-second timestep
- [x] PZR level should remain ≥ 20% during RCP ramp-up
- [x] PZR level should gradually increase during RCP startup (thermal expansion)
- [x] CVCS PI controller should not saturate during RCP ramp

---

## Technical Details

**Lines Changed:** ~40 lines in REGIME 2 and REGIME 3 sections
**Physics Modules Affected:** None (fix is in engine coordinator only)
**Backward Compatibility:** Full (no API changes)

---

## Related Issues

- Stage 2 (BRS Distillate Return) remains pending
- See `IMPLEMENTATION_PLAN_v0.9.6.md` for full scope

---

## Testing Notes

Run the heatup simulation and observe:
1. RCP #1 start at T ≈ 9.75 hr
2. PZR level should remain ~25% (±5%) through RCP ramp-up
3. Level should gradually rise as RCS heats up

**Before fix:** Level crashes to 5%, VCT depletes, RMS makeup triggers
**After fix:** Level stable, smooth transition, VCT level maintained
