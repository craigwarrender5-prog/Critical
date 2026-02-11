# Corrected Analysis: Why System Won't Stabilize

## You Were Right - SG Model EXISTS

My apologies - you correctly pointed out that the SG secondary thermal model WAS implemented in v0.8.0. I should have checked the changelogs first.

## What The SG Model Does

From `SGSecondaryThermal.cs`:
```csharp
public static float CalculateHeatTransfer(float T_rcs, float T_sg_secondary, int rcpsRunning)
{
    float htc = GetCurrentHTC(rcpsRunning);  // 200 BTU/(hr·ft²·°F) with RCPs
    float deltaT = T_rcs - T_sg_secondary;
    float heatTransferRate = htc * PlantConstants.SG_TUBE_AREA_TOTAL_FT2 * deltaT;
    return heatTransferRate;
}
```

**This is a PASSIVE model:**
- Heat flows from hot RCS → cooler SG secondary
- Q = U·A·ΔT (classic heat exchanger)
- SG secondary heats up SLOWLY by absorbing this heat
- Creates the thermal lag you see (T_SG = 341°F vs T_RCS = 342°F)

## What The Model DOESN'T Do

The model does NOT include:
1. **Active auxiliary heating** of SG secondary
2. **Operator control** to match SG to RCS temperature
3. **Mode-based logic** to eliminate ΔT for stabilization

## Why This Was Correct For v0.8.0

The v0.8.0 implementation plan stated:

> **PURPOSE**: Correct RCS heatup rate from ~71°F/hr to ~50°F/hr by adding SG secondary as heat sink

**Goal**: Slow down heatup during Mode 5 → Mode 3 transition  
**Result**: ✅ SUCCESS - Heatup rate reduced from 71°F/hr to 26°F/hr

The model was NEVER intended to allow stabilization - it was designed to slow heatup!

## The Current Situation At t=20hr

```
Heat Balance:
  RCP Heat:        21.00 MW
  PZR Heaters:      1.80 MW
  Total IN:        22.80 MW

  Insulation:       0.82 MW
  SG Secondary:    14.38 MW (passively following RCS, 1°F behind)
  Total OUT:       15.20 MW

  NET:             +7.60 MW → Temperature rises at 24°F/hr
```

**Why SG stays 1°F behind:**

The SG has HUGE thermal mass (1.91 MBTU/°F), so:
```
dT_sg/dt = Q / C_sg = (14.38 MW × 3.412) / 1.91 MBTU/°F
         = 49.1 MBTU/hr / 1.91 MBTU/°F
         = 25.7°F/hr
```

Meanwhile RCS heats at:
```
dT_rcs/dt = Net_Heat / C_rcs = 7.6 MW × 3.412 / (RCS + PZR heat capacity)
          ≈ 24°F/hr
```

**The temperatures are rising at nearly the same rate!**

That's why ΔT stays constant around 1-1.5°F - both sides heating together, maintaining the small temperature difference.

## What's Missing: Active SG Heating

To stabilize at Mode 4, you need to ADD to the existing model:

**File: `SGSecondaryThermal.cs`** - Add new function:

```csharp
/// <summary>
/// Calculate auxiliary heating required to match SG temperature to RCS.
/// Used during Mode 3 approach to eliminate heat sink effect.
/// </summary>
/// <param name="T_rcs">Target RCS temperature to match</param>
/// <param name="T_sg_current">Current SG temperature</param>
/// <param name="heatCapacity_BTU_F">SG heat capacity</param>
/// <param name="plantMode">Current plant mode</param>
/// <returns>Auxiliary heat input required (BTU/hr)</returns>
public static float CalculateAuxiliaryHeating(
    float T_rcs,
    float T_sg_current,
    float heatCapacity_BTU_F,
    int plantMode)
{
    // Only apply auxiliary heating when approaching Mode 3
    if (plantMode < 3)
        return 0f;
    
    float tempError = T_rcs - T_sg_current;
    
    // Proportional control: heat faster when error is larger
    const float CATCHUP_RATE_F_HR = 30f;  // Target 30°F/hr catchup
    
    if (tempError > 10f)
    {
        // Large error: add significant heat to catch up
        return heatCapacity_BTU_F * CATCHUP_RATE_F_HR;
    }
    else if (tempError > 2f)
    {
        // Moderate error: proportional heating
        float catchupRate = CATCHUP_RATE_F_HR * (tempError / 10f);
        return heatCapacity_BTU_F * catchupRate;
    }
    else
    {
        // Small error or negative: no auxiliary heat needed
        return 0f;
    }
}
```

**Then modify `UpdateSecondaryTemperature()`:**

```csharp
public static float UpdateSecondaryTemperature(
    float currentTempF,
    float heatTransferRate,
    float pressurePsia,
    float timestepHours,
    int plantMode = 0,           // NEW parameter
    float T_rcs = 0f)            // NEW parameter
{
    float heatCapacity = GetSecondaryHeatCapacity(currentTempF, pressurePsia);
    
    // Calculate auxiliary heating if in Mode 3+ approach
    float auxHeat_BTU_hr = 0f;
    if (plantMode >= 3 && T_rcs > 0f)
    {
        auxHeat_BTU_hr = CalculateAuxiliaryHeating(T_rcs, currentTempF, heatCapacity, plantMode);
    }
    
    // Total heat = passive transfer from primary + auxiliary heating
    float totalHeat_BTU_hr = heatTransferRate + auxHeat_BTU_hr;
    
    float dT_dt = totalHeat_BTU_hr / heatCapacity;
    float newTemperature = currentTempF + (dT_dt * timestepHours);
    
    return newTemperature;
}
```

**Update `RCSHeatup.cs` to pass plant mode:**

```csharp
// In BulkHeatupStep(), around line 90
result.T_sg_secondary = SGSecondaryThermal.UpdateSecondaryTemperature(
    T_sg_secondary, 
    sgHeatTransfer_BTUhr, 
    result.Pressure, 
    dt_hr,
    plantMode,      // ADD THIS
    state.Temperature  // ADD THIS
);
```

**Update simulation engine to track plant mode:**

```csharp
// In HeatupSimEngine.cs
public int plantMode = 5;  // Start in Mode 5 (Cold Shutdown)

// In physics update, determine mode:
void DeterminePlantMode()
{
    if (T_avg < 200f)
        plantMode = 5;  // Cold Shutdown
    else if (T_avg < 350f)
        plantMode = 4;  // Hot Shutdown
    else if (pressure < 2235f)
        plantMode = 3;  // Hot Standby
    else
        plantMode = 2;  // Startup (approaching criticality)
}
```

## Expected Behavior After Fix

### Before Mode 3 (Modes 4-5):
- SG passively follows RCS (current behavior)
- ΔT maintained at 1-2°F
- Continuous heatup

### At Mode 3 Entry (T_rcs ≥ 350°F):
- Auxiliary heating activates
- SG catches up to RCS at 30°F/hr
- ΔT → 0 over ~30-60 minutes
- Heat sink effect eliminated
- System stabilizes!

### Heat Balance After Stabilization:
```
Heat IN:
  RCPs:            21.00 MW
  Heaters:          1.80 MW (but should reduce to pressure control only)
  Total:           22.80 MW

Heat OUT:
  Insulation:       0.82 MW
  SG Secondary:     ~0 MW (ΔT ≈ 0)
  Total:            0.82 MW

NET:               22.00 MW (still positive!)
```

**Still won't fully stabilize because RCP heat >> losses!**

### Complete Stabilization Requires:

1. ✅ **SG auxiliary heating** (eliminates 14 MW sink)
2. ⚠️ **PZR heater reduction** (cut from 1.8 MW to pressure maintenance only)
3. ⚠️ **SG steam dump** (optional - removes excess RCP heat via turbine bypass)

## Summary

**Your SG model is correct and working as designed.**

It successfully:
- ✅ Reduced heatup rate from 71°F/hr → 26°F/hr (as intended in v0.8.0)
- ✅ Models passive heat sink during heatup (physically accurate)
- ✅ Maintains realistic thermal lag

It DOESN'T:
- ❌ Include active auxiliary heating (not in v0.8.0 scope)
- ❌ Allow temperature stabilization (not a design goal)
- ❌ Have mode-based control logic

**To enable Mode 4 stabilization, you need to ADD auxiliary heating capability to the existing model** (not replace it - the passive model is correct during heatup phases).

The code above shows exactly where and how to add this capability.
