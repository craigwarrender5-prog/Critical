// CRITICAL: Master the Atom - Physics Module
// RVLISPhysics.cs - Reactor Vessel Level Indication System
//
// Implements: Engine Architecture Audit Fix - Issue #5
//   Extracts RVLIS calculation from engine to physics module
//
// PHYSICS:
//   RVLIS uses differential pressure measurements to indicate reactor
//   vessel water level. Three ranges are provided:
//   
//   - Dynamic Range: Valid when RCPs are running (forced flow)
//     Uses ΔP across the core with flow compensation
//   
//   - Full Range: Valid when RCPs are off (no flow)
//     Direct ΔP measurement from bottom to top of vessel
//   
//   - Upper Range: Valid when RCPs are off
//     ΔP from mid-vessel to top, for detecting voiding in upper head
//
// Sources:
//   - NRC HRTD Section 7.5 - Post-Accident Monitoring
//   - NRC Bulletin 79-08 - RCS Inventory Indication
//   - TMI-2 Lessons Learned - Level Indication Requirements
//
// Units: % for level indication, lb for mass, ft³ for volume

using System;

namespace Critical.Physics
{
    /// <summary>
    /// RVLIS state container.
    /// </summary>
    public struct RVLISState
    {
        public float DynamicRange;      // % level (0-100), valid with RCPs
        public float FullRange;         // % level (0-100), valid without RCPs
        public float UpperRange;        // % level (0-100), valid without RCPs
        public bool DynamicValid;       // True when RCPs are running
        public bool FullRangeValid;     // True when RCPs are off
        public bool UpperRangeValid;    // True when RCPs are off
        public bool LevelLowAlarm;      // True when level indication is low
    }
    
    /// <summary>
    /// Reactor Vessel Level Indication System physics.
    /// 
    /// RVLIS provides three level indications based on differential pressure:
    ///   - Dynamic Range: Compensated for RCP flow, valid during normal ops
    ///   - Full Range: Static ΔP, valid only with no RCP flow
    ///   - Upper Range: Upper vessel level, valid only with no RCP flow
    /// 
    /// The engine should call Update() each timestep with current RCS conditions.
    /// </summary>
    public static class RVLISPhysics
    {
        #region Constants
        
        /// <summary>Low level alarm setpoint for Full Range (%)</summary>
        public const float LEVEL_LOW_ALARM = 90f;
        
        /// <summary>Dynamic range reference level at 40% when RCPs off</summary>
        public const float DYNAMIC_RANGE_NO_FLOW_REFERENCE = 40f;

        /// <summary>
        /// Upper display bound used to keep drain-time inventory trends visible.
        /// </summary>
        public const float LEVEL_OVER_RANGE_MAX = 120f;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize RVLIS state for normal conditions.
        /// </summary>
        /// <param name="rcpCount">Number of RCPs running</param>
        /// <returns>Initialized RVLIS state</returns>
        public static RVLISState Initialize(int rcpCount)
        {
            var state = new RVLISState();
            
            state.DynamicValid = (rcpCount > 0);
            state.FullRangeValid = (rcpCount == 0);
            state.UpperRangeValid = (rcpCount == 0);
            
            if (rcpCount > 0)
            {
                state.DynamicRange = 100f;
                state.FullRange = 0f;
                state.UpperRange = 0f;
            }
            else
            {
                state.DynamicRange = DYNAMIC_RANGE_NO_FLOW_REFERENCE;
                state.FullRange = 100f;
                state.UpperRange = 100f;
            }
            
            state.LevelLowAlarm = false;
            
            return state;
        }
        
        #endregion
        
        #region Main Update
        
        /// <summary>
        /// Update RVLIS indications based on current RCS conditions.
        /// </summary>
        /// <param name="state">RVLIS state (modified in place)</param>
        /// <param name="rcsWaterMass">Current RCS water mass (lb)</param>
        /// <param name="T_rcs">RCS temperature (°F)</param>
        /// <param name="pressure">RCS pressure (psia)</param>
        /// <param name="rcpCount">Number of RCPs running</param>
        public static void Update(
            ref RVLISState state,
            float rcsWaterMass,
            float T_rcs,
            float pressure,
            int rcpCount)
        {
            // Update validity based on RCP status
            state.DynamicValid = (rcpCount > 0);
            state.FullRangeValid = (rcpCount == 0);
            state.UpperRangeValid = (rcpCount == 0);
            
            // Calculate current RCS volume from mass and density
            float currentDensity = WaterProperties.WaterDensity(T_rcs, pressure);
            float currentVolume = (currentDensity > 0.1f) ? rcsWaterMass / currentDensity : 0f;
            
            // Volume ratio relative to normal full volume.
            // Keep bounded over-range so drain/fill trends do not pin at 100%.
            float volumeRatio = currentVolume / PlantConstants.RCS_WATER_VOLUME;
            volumeRatio = Math.Max(0f, Math.Min(volumeRatio, LEVEL_OVER_RANGE_MAX / 100f));
            
            if (rcpCount > 0)
            {
                // ============================================================
                // DYNAMIC RANGE - Valid with RCPs running
                // Indicates RCS inventory based on flow-compensated ΔP
                // At normal inventory, reads 100%
                // ============================================================
                state.DynamicRange = 100f * volumeRatio;
                state.DynamicRange = Math.Max(0f, Math.Min(state.DynamicRange, LEVEL_OVER_RANGE_MAX));
                
                // Full and Upper ranges are invalid (reads low due to flow effects)
                state.FullRange = 0f;
                state.UpperRange = 0f;
            }
            else
            {
                // ============================================================
                // STATIC RANGES - Valid without RCPs
                // Direct ΔP measurement without flow compensation needed
                // ============================================================
                
                // Dynamic range reads low (~40%) without flow compensation
                state.DynamicRange = DYNAMIC_RANGE_NO_FLOW_REFERENCE * volumeRatio;
                
                // Full Range: Bottom to top of vessel ΔP
                state.FullRange = 100f * volumeRatio;
                state.FullRange = Math.Max(0f, Math.Min(state.FullRange, LEVEL_OVER_RANGE_MAX));
                
                // Upper Range: Mid-vessel to top ΔP
                // More sensitive to upper head voiding
                state.UpperRange = 100f * volumeRatio;
                state.UpperRange = Math.Max(0f, Math.Min(state.UpperRange, LEVEL_OVER_RANGE_MAX));
            }
            
            // Level low alarm based on valid indication
            state.LevelLowAlarm = (state.FullRangeValid && state.FullRange < LEVEL_LOW_ALARM);
        }
        
        /// <summary>
        /// Simplified update returning just the state.
        /// </summary>
        public static RVLISState Calculate(
            float rcsWaterMass, float T_rcs, float pressure, int rcpCount)
        {
            var state = Initialize(rcpCount);
            Update(ref state, rcsWaterMass, T_rcs, pressure, rcpCount);
            return state;
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Get the currently valid level indication.
        /// </summary>
        public static float GetValidLevel(RVLISState state)
        {
            if (state.DynamicValid)
                return state.DynamicRange;
            if (state.FullRangeValid)
                return state.FullRange;
            return 0f;
        }
        
        /// <summary>
        /// Get status string for display.
        /// </summary>
        public static string GetStatusString(RVLISState state)
        {
            if (state.LevelLowAlarm)
                return "LEVEL LOW";
            if (state.DynamicValid)
                return $"DYNAMIC {state.DynamicRange:F0}%";
            if (state.FullRangeValid)
                return $"FULL {state.FullRange:F0}%";
            return "INVALID";
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate RVLIS calculations.
        /// </summary>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: With RCPs, dynamic should be valid
            var state1 = Calculate(500000f, 557f, 2250f, 4);
            if (!state1.DynamicValid) valid = false;
            if (state1.FullRangeValid) valid = false;
            
            // Test 2: Without RCPs, full range should be valid
            var state2 = Calculate(500000f, 400f, 800f, 0);
            if (state2.DynamicValid) valid = false;
            if (!state2.FullRangeValid) valid = false;
            
            // Test 3: Full RCS should indicate ~100%
            float rho = WaterProperties.WaterDensity(557f, 2250f);
            float fullMass = PlantConstants.RCS_WATER_VOLUME * rho;
            var state3 = Calculate(fullMass, 557f, 2250f, 4);
            if (state3.DynamicRange < 95f) valid = false;
            
            // Test 4: Low mass should trigger alarm when RCPs off
            var state4 = Calculate(fullMass * 0.8f, 400f, 800f, 0);
            if (!state4.LevelLowAlarm) valid = false;
            
            // Test 5: Dynamic range reads ~40% with no flow
            var state5 = Calculate(fullMass, 400f, 800f, 0);
            if (state5.DynamicRange > 50f) valid = false;  // Should be ~40%
            
            return valid;
        }
        
        #endregion
    }
}
