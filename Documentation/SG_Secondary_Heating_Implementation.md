# SG Secondary Heating Implementation Guide
## Enabling Mode 4 Temperature Stabilization

**Problem:** System continues heating indefinitely instead of stabilizing at Mode 4 target temperature.

**Root Cause:** SG secondary side acts as a passive heat sink (14.38 MW) with no active heating mechanism.

---

## The Physics Problem

### Current Heat Balance at t=20hr

```
Heat IN:
  RCP Heat:        21.00 MW (from 4 pumps @ 5.25 MW each)
  PZR Heaters:      1.80 MW
  ─────────────
  Total:           22.80 MW

Heat OUT:
  Insulation:       0.82 MW (temperature-dependent losses)
  SG Secondary:    14.38 MW (Q = U·A·ΔT heat transfer)
  ─────────────
  Total:           15.20 MW

NET HEAT:           7.60 MW → TEMPERATURE CONTINUES RISING
```

### Why SG is a Heat Sink

The SG primary-to-secondary heat transfer follows:
```
Q = U · A · ΔT

where:
  U  = 200 BTU/(hr·ft²·°F)  (heat transfer coefficient)
  A  = ~219,000 ft²         (tube surface area)
  ΔT = T_RCS - T_SG         (temperature difference)
```

At t=20hr:
- T_RCS = 342.32°F
- T_SG = 341.20°F  
- ΔT = 1.12°F
- Q = 200 × 219,000 × 1.12 = 49.1 MBTU/hr = **14.38 MW**

**As long as T_SG < T_RCS, heat flows from RCS → SG secondary!**

---

## What Real Plants Do

### Standard Mode 4 → Mode 3 Procedure

Per NRC HRTD Section 19.2.3 - "Heatup to Hot Standby":

1. **Continue RCP operation** (all 4 running for circulation)
2. **Heat SG secondary side** using:
   - Auxiliary steam from plant auxiliary boiler
   - Electric immersion heaters (backup)
   - Reactor coolant pump heat (passive)
3. **Match temperatures**: Bring T_SG → T_RCS
4. **Achieve equilibrium**: When ΔT → 0, Q → 0
5. **Pressurize to 2235 psig** once thermal equilibrium established
6. **Enter Mode 3** (Hot Standby)

### Real Plant Equipment

**Auxiliary Steam System:**
- Small auxiliary boiler (~10-20 MBTU/hr capacity)
- Steam injection into SG secondary side
- Temperature-controlled valves
- Used during startup/shutdown when main steam not available

**Electric Heaters (Backup):**
- Immersion heaters in SG secondary side
- Typically 1-5 MW per SG
- Slower but reliable
- Used if aux steam unavailable

---

## Implementation Options

### Option 1: Full Physical Model (High Fidelity)

**Create new physics module: `SGSecondaryHeating.cs`**

```csharp
namespace Critical.Physics
{
    /// <summary>
    /// Steam Generator Secondary Side Heating System
    /// Models auxiliary steam injection and electric heaters
    /// for SG temperature control during startup/shutdown.
    /// 
    /// Per NRC HRTD 19.2.3 and Westinghouse FSAR Chapter 10.
    /// </summary>
    public static class SGSecondaryHeating
    {
        #region Constants
        
        public const float AUX_STEAM_MAX_FLOW_LBM_HR = 50000f;  // Max aux steam flow
        public const float AUX_STEAM_ENTHALPY_BTU_LB = 1150f;   // ~200 psig saturated
        public const float ELECTRIC_HEATER_POWER_MW_PER_SG = 2.5f;  // 2.5 MW per SG
        public const int NUMBER_OF_SGs = 4;
        
        // SG secondary heat capacity (from plant design)
        // ~450,000 lb water per SG × 1 BTU/(lb·°F) = 450,000 BTU/°F per SG
        public const float SG_SECONDARY_HEAT_CAPACITY_BTU_F = 1910000f;  // All 4 SGs
        
        #endregion
        
        #region State Structure
        
        public struct SGHeatingState
        {
            public float T_SG_Secondary_F;           // Current SG secondary temperature
            public float AuxSteamFlow_lbm_hr;        // Auxiliary steam flow rate
            public float ElectricHeatPower_MW;       // Electric heater power
            public float TotalHeatInput_MW;          // Total heat added to SG secondary
            public bool AuxSteamAvailable;           // Aux boiler operational
            public bool ElectricHeatersEnabled;      // Heaters energized
            public float TargetTemperature_F;        // Control setpoint
            public string ControlMode;               // MANUAL, AUTO_TRACK_RCS, HOLD
        }
        
        #endregion
        
        #region Initialization
        
        public static SGHeatingState Initialize(float initialTemp_F)
        {
            var state = new SGHeatingState();
            state.T_SG_Secondary_F = initialTemp_F;
            state.AuxSteamFlow_lbm_hr = 0f;
            state.ElectricHeatPower_MW = 0f;
            state.TotalHeatInput_MW = 0f;
            state.AuxSteamAvailable = true;  // Assume aux boiler operational
            state.ElectricHeatersEnabled = false;
            state.TargetTemperature_F = initialTemp_F;
            state.ControlMode = "MANUAL";
            return state;
        }
        
        #endregion
        
        #region Update
        
        public static void Update(
            ref SGHeatingState state,
            float T_RCS_F,
            float heatTransferFromPrimary_MW,  // Heat from RCS via tubes
            int plantMode,
            float dt_hr)
        {
            // ==========================================================
            // CONTROL LOGIC
            // ==========================================================
            
            if (state.ControlMode == "AUTO_TRACK_RCS")
            {
                // Automatically heat SG to match RCS temperature
                state.TargetTemperature_F = T_RCS_F;
            }
            
            float tempError_F = state.TargetTemperature_F - state.T_SG_Secondary_F;
            
            // ==========================================================
            // AUXILIARY STEAM CONTROL
            // ==========================================================
            
            if (state.AuxSteamAvailable && tempError_F > 5f)
            {
                // Proportional control on aux steam flow
                // Max flow at 50°F error, zero flow at 5°F error
                float flowFraction = Mathf.Clamp01((tempError_F - 5f) / 45f);
                state.AuxSteamFlow_lbm_hr = AUX_STEAM_MAX_FLOW_LBM_HR * flowFraction;
            }
            else
            {
                state.AuxSteamFlow_lbm_hr = 0f;
            }
            
            // Convert aux steam to heat input
            float auxSteamHeat_MW = (state.AuxSteamFlow_lbm_hr * AUX_STEAM_ENTHALPY_BTU_LB) 
                                    / (1e6f / 0.293f);  // BTU/hr to MW
            
            // ==========================================================
            // ELECTRIC HEATER CONTROL
            // ==========================================================
            
            if (state.ElectricHeatersEnabled && tempError_F > 2f)
            {
                // Backup heating if aux steam insufficient
                float heaterFraction = Mathf.Clamp01((tempError_F - 2f) / 20f);
                state.ElectricHeatPower_MW = ELECTRIC_HEATER_POWER_MW_PER_SG 
                                            * NUMBER_OF_SGs 
                                            * heaterFraction;
            }
            else
            {
                state.ElectricHeatPower_MW = 0f;
            }
            
            // ==========================================================
            // TOTAL HEAT BALANCE
            // ==========================================================
            
            state.TotalHeatInput_MW = auxSteamHeat_MW + state.ElectricHeatPower_MW;
            
            // Net heat to SG secondary
            float netHeat_MW = heatTransferFromPrimary_MW + state.TotalHeatInput_MW;
            
            // Temperature change
            float dT_dt = (netHeat_MW * 3.412e6f) / SG_SECONDARY_HEAT_CAPACITY_BTU_F;
            state.T_SG_Secondary_F += dT_dt * dt_hr;
            
            // ==========================================================
            // MODE-SPECIFIC LOGIC
            // ==========================================================
            
            if (plantMode == 4 && state.ControlMode == "MANUAL")
            {
                // Suggest switching to auto tracking for Mode 4
                state.ControlMode = "AUTO_TRACK_RCS";
                state.TargetTemperature_F = T_RCS_F;
            }
            else if (plantMode >= 3 && Mathf.Abs(tempError_F) < 2f)
            {
                // Close to target, switch to hold mode
                state.ControlMode = "HOLD";
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        public static string GetStatusString(SGHeatingState state)
        {
            if (state.AuxSteamFlow_lbm_hr > 1000f)
                return $"AUX STEAM ACTIVE ({state.AuxSteamFlow_lbm_hr/1000f:F1} klb/hr)";
            if (state.ElectricHeatPower_MW > 0.5f)
                return $"ELECTRIC HEAT ({state.ElectricHeatPower_MW:F1} MW)";
            if (state.ControlMode == "AUTO_TRACK_RCS")
                return "AUTO TRACKING RCS";
            return "STANDBY";
        }
        
        #endregion
    }
}
```

**Integration into HeatupSimEngine:**

```csharp
// In HeatupSimEngine.cs

using Critical.Physics;

public partial class HeatupSimEngine : MonoBehaviour
{
    // Add state variable
    private SGSecondaryHeating.SGHeatingState sgHeatingState;
    
    // In initialization
    void InitializeSystems()
    {
        // ... existing initialization ...
        
        sgHeatingState = SGSecondaryHeating.Initialize(startTemperature);
    }
    
    // In physics update loop
    void UpdatePhysics(float dt_sec)
    {
        // ... existing RCS calculations ...
        
        // Calculate SG heat transfer from primary
        float sgHeatTransfer_MW = CalculateSGHeatTransfer(T_avg, T_sg_secondary);
        
        // Update SG secondary heating
        SGSecondaryHeating.Update(
            ref sgHeatingState,
            T_avg,                    // RCS temperature
            sgHeatTransfer_MW,        // Heat from primary
            plantMode,                // Current plant mode
            dt_sec / 3600f           // dt in hours
        );
        
        // Use updated SG temperature
        T_sg_secondary = sgHeatingState.T_SG_Secondary_F;
        
        // ... rest of physics ...
    }
}
```

---

### Option 2: Simplified Model (Quick Implementation)

**Modify existing SG secondary temperature calculation:**

**File:** `Assets/Scripts/Physics/LoopThermodynamics.cs` (or wherever SG temp is calculated)

```csharp
// Add to the SG temperature update section

public static float UpdateSGSecondaryTemperature(
    float T_SG_current,
    float T_RCS,
    float heatTransferFromPrimary_MW,
    int plantMode,
    float dt_hr)
{
    const float SG_HEAT_CAPACITY_MBTU_F = 1.91f;  // From logs
    
    float T_SG_new;
    
    if (plantMode >= 3)
    {
        // ======================================================
        // MODE 3+ (HOT STANDBY OR HIGHER)
        // Model auxiliary heating to match RCS temperature
        // ======================================================
        
        float tempError = T_RCS - T_SG_current;
        
        if (Mathf.Abs(tempError) > 10f)
        {
            // Large error: Add auxiliary heating to catch up
            // Target: 30°F/hr catchup rate
            float catchupRate_F_hr = 30f;
            float catchupDirection = Mathf.Sign(tempError);
            T_SG_new = T_SG_current + catchupDirection * catchupRate_F_hr * dt_hr;
            
            // Don't overshoot
            if (catchupDirection > 0 && T_SG_new > T_RCS)
                T_SG_new = T_RCS;
            else if (catchupDirection < 0 && T_SG_new < T_RCS)
                T_SG_new = T_RCS;
        }
        else if (Mathf.Abs(tempError) > 2f)
        {
            // Moderate error: Track RCS with slight lag
            float trackingRate_F_hr = 10f;
            float trackingDirection = Mathf.Sign(tempError);
            T_SG_new = T_SG_current + trackingDirection * trackingRate_F_hr * dt_hr;
            
            // Don't overshoot
            if (trackingDirection > 0 && T_SG_new > T_RCS)
                T_SG_new = T_RCS - 1f;  // Maintain 1°F lag
            else if (trackingDirection < 0 && T_SG_new < T_RCS)
                T_SG_new = T_RCS + 1f;
        }
        else
        {
            // Small error: Lock to RCS with minimal offset
            T_SG_new = T_RCS - 1f;  // 1°F below RCS (slight heat transfer maintained)
        }
    }
    else
    {
        // ======================================================
        // MODE 4-5 (BELOW HOT STANDBY)
        // Passive heating only - SG follows RCS via heat transfer
        // ======================================================
        
        // Calculate temperature rise from primary heat transfer
        float dT_dt_F_hr = (heatTransferFromPrimary_MW * 3.412f) / SG_HEAT_CAPACITY_MBTU_F;
        T_SG_new = T_SG_current + dT_dt_F_hr * dt_hr;
    }
    
    return T_SG_new;
}
```

---

### Option 3: Simplest Hack (For Testing)

**Just force SG temperature to match RCS when above certain mode:**

```csharp
// In the main physics update

if (plantMode >= 3)
{
    // Hot Standby or higher: SG is heated to match RCS
    T_sg_secondary = T_avg - 1.0f;  // 1°F lag
    sgHeatTransfer_MW = 0.1f;       // Minimal heat transfer
}
else
{
    // Below Hot Standby: Calculate normally
    T_sg_secondary = CalculateSGTemperaturePassive(...);
}
```

---

## Recommended Approach

**Phase 1 (Immediate - Use Option 3):**
- Quick test to verify equilibrium concept
- Add mode-based SG temperature override
- Validate that system stabilizes

**Phase 2 (Short-term - Use Option 2):**
- Implement simplified model with catchup logic
- More realistic than hack, less complex than full model
- Good balance of fidelity vs. development time

**Phase 3 (Long-term - Use Option 1):**
- Full auxiliary heating system model
- Adds realism for operator training
- Required for detailed procedure validation

---

## Testing Plan

### Test 1: Verify Equilibrium

**Setup:**
- Continue current simulation to t=25hr
- Implement Option 3 (simplest)
- When T_RCS ≥ 350°F, enable SG temperature matching

**Expected Result:**
```
Time    T_RCS    T_SG     Net Heat    Heatup Rate
20hr    342°F    341°F    +7.6 MW     +24°F/hr
21hr    366°F    365°F    +7.6 MW     +24°F/hr
22hr    390°F    389°F    ~0 MW       ~0°F/hr     ← STABILIZES!
23hr    390°F    389°F    ~0 MW       ~0°F/hr     ← HOLDS!
```

### Test 2: Mode 3 Entry

**Setup:**
- With stabilized temperature at 350-400°F
- Continue pressurization to 2235 psig
- Monitor for Mode 3 entry conditions

**Expected Result:**
- T_avg holds at target (±10°F)
- Pressure rises steadily (~40-50 psi/hr)
- PZR level controlled (35-40%)
- System enters Mode 3 when P ≥ 2235 psig

### Test 3: Controller Tuning

**Setup:**
- Implement Option 2 (simplified model)
- Test catchup rates: 10, 20, 30°F/hr

**Expected Result:**
- 10°F/hr: Slow but stable
- 20°F/hr: Good balance (RECOMMENDED)
- 30°F/hr: Fast but may overshoot

---

## Expected System Behavior After Fix

### Heat Balance at Equilibrium (Mode 3)

```
Heat IN:
  RCP Heat:        21.00 MW
  PZR Heaters:      1.80 MW
  Total:           22.80 MW

Heat OUT:
  Insulation:       0.82 MW
  SG Secondary:     ~0 MW    ← NOW NEAR ZERO (T_SG ≈ T_RCS)
  Total:            0.82 MW

NET:              22.00 MW   ← Still positive!
```

**Wait - this is STILL not balanced!**

### The Real Issue: You Need to REDUCE Heat Input

Once SG temperatures match, you still have:
- RCP heat: 21 MW (always present when RCPs run)
- PZR heaters: 1.8 MW

But losses are only ~1 MW!

**The REAL stabilization mechanism:**

1. **Reduce PZR heaters to minimum** (or off)
   - Heaters only needed for pressurization, not temperature
   - At Mode 3, switch heaters to "AUTOMATIC_PID" mode
   - They modulate to maintain pressure, not add heat

2. **RCP heat is INTENTIONAL**
   - Compensates for heat losses
   - Maintains temperature during hold periods
   - In reality, 21 MW RCP heat >> 1 MW losses because...

3. **SG acts as active heat sink in reality**
   - SG secondary is NOT isolated
   - Small continuous steam flow to turbine gland seals
   - Or steam dump to condenser
   - This removes the ~20 MW excess

**So the COMPLETE solution needs:**

✅ **SG secondary heating** (to eliminate the 14 MW passive sink)  
✅ **PZR heater control** (reduce from 1.8 MW to maintain pressure only)  
✅ **SG steam dump** (optional, removes excess RCP heat)  

---

## Summary

**Why system won't stabilize:**
- SG secondary is 14 MW heat sink
- No mechanism to heat SG to match RCS
- Result: Net +7.6 MW keeps heating system

**Fix (minimum):**
- Add SG temperature control (Options 1, 2, or 3 above)
- When SG temp matches RCS, heat sink → 0

**Fix (complete):**
- SG temperature control
- PZR heater reduction to pressure-maintenance only
- Optional: SG steam dump for excess heat

**Recommended immediate action:**
- Use Option 3 (simplest) to test concept
- Verify system stabilizes when T_SG = T_RCS
- Then implement Option 2 for better fidelity
