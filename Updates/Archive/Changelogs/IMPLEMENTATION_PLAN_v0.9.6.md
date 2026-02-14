# Implementation Plan v0.9.6 — BRS Distillate Return & PZR Level Drop Fix

**Date:** 2026-02-07  
**Type:** Minor (Bug Fixes)  
**Priority:** MEDIUM  
**Scope:** BRS/CVCS Integration, RCP Startup Physics Blending

---

## Executive Summary

Two related issues were discovered during heatup simulation at the transition from isolated PZR operations to RCP startup:

1. **BRS Distillate Return Failure** — BRS accumulates diverted letdown but never returns distillate to the VCT when level drops
2. **PZR Level Transient on RCP Start** — Pressurizer level drops from 25% to 5% when RCPs start due to physics blending discontinuity

---

## Issue 1: BRS Distillate Never Returns to VCT

### Observed Behavior

| Time (hr) | VCT Level | BRS Holdup | BRS Distillate | VCT Makeup Source |
|-----------|-----------|------------|----------------|-------------------|
| 7.0-9.5   | 70-75%    | 400→1813 gal | 0 gal       | N/A (diverting)   |
| 9.75      | **59%**   | 1813 gal   | **0 gal**      | N/A               |
| 10.0      | **30%**   | 1813 gal   | **0 gal**      | N/A               |
| 10.25     | **22%**   | 1813 gal   | **0 gal**      | **RMS** ← Wrong!  |
| 10.5      | 37%       | 1813 gal   | **0 gal**      | RMS               |

### Root Cause Analysis

The BRS evaporator **never starts processing** because it requires a minimum batch volume of 5,000 gallons before activation (per `PlantConstants.BRS_EVAPORATOR_MIN_BATCH_GAL`). The holdup tank only accumulates 1,813 gallons during the solid pressurizer phase before divert stops.

**Per BRSPhysics.cs UpdateProcessing():**
```csharp
if (!state.ProcessingActive &&
    state.HoldupVolume_gal >= PlantConstants.BRS_EVAPORATOR_MIN_BATCH_GAL)  // 5000 gal
{
    state.ProcessingActive = true;
}
```

**Result:** Holdup never reaches 5,000 gal → Evaporator never starts → No distillate produced → BRS cannot supply VCT makeup → VCT draws from RMS instead (diluting boron!).

### Technical Specification Validation

Per **NRC HRTD 4.1 Section 4.1.2.6** and **Callaway FSAR Chapter 11**:
- BRS batch processing is operator-initiated (manual) in real plants
- Batch size of 5,000 gal minimum is reasonable for full-scale operations
- However, during a single heatup, typical divert volumes are 1,500-3,000 gal
- Real plants use monitor tank inventory from prior cycles, not fresh distillate

**Realistic Solution:** The simulator should either:
1. Pre-load distillate inventory representing prior cycle processing, OR
2. Reduce minimum batch threshold for simulation demonstration purposes, OR  
3. Add alternate return path (PWST makeup) as backup when distillate unavailable

### Proposed Fix

**Option A (Recommended):** Pre-initialize BRS with distillate inventory at simulation start

In `HeatupSimEngine.Init.cs`, initialize BRS with realistic monitor tank inventory:
```csharp
brsState = BRSPhysics.Initialize(boronConc);
// Pre-load distillate from prior processing cycles (typical plant condition)
brsState.DistillateAvailable_gal = 10000f;  // ~50% of one monitor tank
brsState.DistillateBoron_ppm = PlantConstants.BRS_DISTILLATE_BORON_PPM;  // ~0 ppm
```

**Option B (Alternative):** Lower minimum batch threshold for demonstration

In `PlantConstants.BRS.cs`:
```csharp
// Reduced for simulator demonstration (real plant: 5000 gal)
public const float BRS_EVAPORATOR_MIN_BATCH_GAL = 1000f;
```

---

## Issue 2: PZR Level Drops from 25% to 5% on RCP Start

### Observed Behavior

| Time (hr) | RCP State | Alpha (α) | PZR Level | PZR Water Vol | Physics Regime |
|-----------|-----------|-----------|-----------|---------------|----------------|
| 9.50      | 0 RCPs    | 0.00      | 25.2%     | 454 ft³       | Isolated       |
| 9.75      | 1 RCP (T+0.18hr) | **0.33** | **4.9%** | **88 ft³** | **Blended** |
| 10.00     | 1 RCP (T+0.43hr) | 0.76  | 17.2%     | 310 ft³       | Blended        |
| 10.25     | 2 RCPs    | 1.33      | 25.8%     | 465 ft³       | Blended        |

**The level dropped by 81% (454→88 ft³) in 15 minutes when the first RCP started!**

### Root Cause Analysis

The bug is in the **REGIME 2 blending logic** in `HeatupSimEngine.cs`. When transitioning from isolated to coupled physics, the code blends PZR water volume:

```csharp
// Current blending (PROBLEMATIC):
pzrWaterVolume = pzrWaterVolume * oneMinusAlpha + physicsState.PZRWaterVolume * alpha;
```

**The problem:** `physicsState.PZRWaterVolume` is coming from `CoupledThermo.SolveEquilibrium()`, which recalculates PZR level from scratch based on mass conservation. At low coupling (α=0.33), this produces a drastically different PZR state than the current isolated state.

**Sequence of events:**
1. Pre-RCP: Isolated regime maintains PZR at 454 ft³ (25% level)
2. RCP starts: α jumps from 0 to 0.33 (first 10 seconds of ramp)
3. Coupled solver calculates "equilibrium" PZR volume = ~50 ft³ based on RCS thermal state
4. Blending: `454 × 0.67 + 50 × 0.33 = 304 + 17 = 321 ft³` → **But we see 88 ft³!**

The additional discrepancy comes from the fact that `physicsState.PZRWaterVolume` is **not initialized** from current state before calling `BulkHeatupStep()`. The coupled solver receives stale/zero PZR state and produces an invalid result.

### Technical Specification Validation

Per **NRC HRTD 19.2.2** and **Westinghouse Operating Procedures**:
- RCP startup should cause **gradual level increase** due to RCS thermal expansion
- The staged 40-minute ramp was specifically designed to prevent thermal shock
- Level should rise ~5-10% as RCPs heat RCS, not drop 80%

### Proposed Fix

**Part A:** Initialize `physicsState.PZRWaterVolume` from current engine state before calling the coupled solver:

```csharp
// REGIME 2: Before calling BulkHeatupStep, sync physicsState with engine state
physicsState.Temperature = T_rcs;
physicsState.Pressure = pressure;
physicsState.PZRWaterVolume = pzrWaterVolume;    // ADD THIS
physicsState.PZRSteamVolume = pzrSteamVolume;    // ADD THIS
physicsState.PZRWaterMass = pzrWaterVolume * WaterProperties.WaterDensity(T_pzr, pressure);  // ADD THIS
physicsState.PZRSteamMass = pzrSteamVolume * WaterProperties.SaturatedSteamDensity(pressure);  // ADD THIS
```

**Part B:** Use incremental blending instead of absolute blending for PZR volume:

```csharp
// Current (problematic):
pzrWaterVolume = pzrWaterVolume * oneMinusAlpha + physicsState.PZRWaterVolume * alpha;

// Fixed (incremental change blending):
float deltaPZRVolume_coupled = physicsState.PZRWaterVolume - pzrWaterVolume;  // Change from coupled path
float deltaPZRVolume_isolated = 0f;  // Isolated path doesn't change PZR level
float deltaPZRVolume = deltaPZRVolume_isolated * oneMinusAlpha + deltaPZRVolume_coupled * alpha;
pzrWaterVolume += deltaPZRVolume;
```

---

## Files to Modify

| File | Changes |
|------|---------|
| `HeatupSimEngine.Init.cs` | Pre-initialize BRS distillate inventory (Issue 1) |
| `HeatupSimEngine.cs` | Fix REGIME 2 PZR volume blending (Issue 2) |
| `PlantConstants.BRS.cs` | (Optional) Adjust evaporator minimum batch threshold |

---

## Validation Criteria

### Issue 1 - BRS Distillate Return

- [ ] BRS distillate available > 0 at simulation start (if using Option A)
- [ ] When VCT drops below 20%, `vctState.MakeupFromBRS` should be `true` if distillate available
- [ ] BRS `CumulativeReturned_gal` should increase when VCT makeup draws from BRS
- [ ] VCT boron concentration should remain stable (not dilute from RMS primary water)

### Issue 2 - PZR Level Stability on RCP Start

- [ ] PZR level should not drop more than 5% in any single 10-second physics step
- [ ] PZR level should remain ≥ 20% during RCP ramp-up
- [ ] PZR level should gradually **increase** during RCP startup (thermal expansion)
- [ ] CVCS PI controller should not saturate to maximum charging during RCP ramp

---

## Implementation Stages

### Stage 1: PZR Level Blending Fix (Critical)
- Modify `HeatupSimEngine.cs` REGIME 2 physics to properly initialize physicsState
- Implement incremental PZR volume blending
- **Test:** Run simulation through RCP startup, verify level stays ≥ 20%

### Stage 2: BRS Distillate Initialization
- Modify `HeatupSimEngine.Init.cs` to pre-load distillate inventory
- **Test:** Verify VCT makeup uses BRS when level drops

### Stage 3: Validation & Logging
- Add debug logging for REGIME 2 transitions
- Verify mass conservation through blending transition
- Run full heatup to confirm both fixes work together

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Fix introduces mass conservation error | Medium | High | Add conservation check at REGIME 2 entry/exit |
| Blending change affects thermal behavior | Low | Medium | Verify heatup rate unchanged |
| BRS initialization unrealistic | Low | Low | Document as "prior cycle inventory" |

---

## Approval

**Prepared by:** Claude (Technical Audit)  
**Date:** 2026-02-07  
**Status:** PENDING USER APPROVAL

Confirm to proceed with Stage 1 implementation.
