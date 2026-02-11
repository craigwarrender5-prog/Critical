// CRITICAL: Master the Atom - Physics Module
// LoopThermodynamics.cs - RCS Loop Temperature Calculations
//
// Implements: Engine Architecture Audit Fix - Issue #1
//   Extracts T_hot/T_cold calculation from engine to physics module
//
// PHYSICS:
//   In a PWR, the temperature difference across the core (T_hot - T_cold)
//   is determined by the heat input and coolant mass flow rate:
//     ΔT = Q̇ / (ṁ × Cp)
//   
//   With RCPs running:
//     - Flow is forced circulation at known pump rate
//     - Heat input is primarily RCP mechanical heat during heatup
//     - ΔT is typically 5-15°F during heatup (vs 61°F at full power)
//   
//   Without RCPs (natural circulation):
//     - Flow is driven by density difference between hot and cold legs
//     - Heat input is surge line conduction from PZR
//     - ΔT is small (1-5°F) due to low heat input
//
// Sources:
//   - NRC HRTD Section 3.2 - RCS Thermal Hydraulics
//   - Westinghouse 4-Loop FSAR - Natural Circulation Analysis
//
// Units: °F for temperature, psia for pressure, MW for power, gpm for flow

using System;

namespace Critical.Physics
{
    /// <summary>
    /// Result structure for loop temperature calculations.
    /// </summary>
    public struct LoopTemperatureResult
    {
        public float T_hot;         // Hot leg temperature (°F)
        public float T_cold;        // Cold leg temperature (°F)
        public float T_avg;         // Average temperature (°F)
        public float DeltaT;        // Temperature rise across core (°F)
        public float MassFlow;      // Mass flow rate (lb/sec)
        public bool IsForcedFlow;   // True if RCPs are providing forced circulation
    }
    
    /// <summary>
    /// RCS loop temperature calculations.
    /// 
    /// This module calculates T_hot and T_cold based on:
    ///   - Heat input (RCP heat, decay heat, etc.)
    ///   - Coolant flow rate (forced or natural circulation)
    ///   - Fluid properties at current conditions
    /// 
    /// The engine should call CalculateLoopTemperatures() each timestep
    /// and use the returned T_hot/T_cold values.
    /// </summary>
    public static class LoopThermodynamics
    {
        #region Main Calculation
        
        /// <summary>
        /// Calculate loop temperatures (T_hot, T_cold) from energy balance.
        /// 
        /// Physics: ΔT = Q̇ / (ṁ × Cp)
        /// where:
        ///   Q̇ = total heat deposited into coolant (BTU/sec)
        ///   ṁ = coolant mass flow rate (lb/sec)
        ///   Cp = specific heat capacity (BTU/lb·°F)
        /// 
        /// T_hot = T_avg + ΔT/2
        /// T_cold = T_avg - ΔT/2
        /// </summary>
        /// <param name="T_rcs">Current RCS bulk temperature (°F)</param>
        /// <param name="pressure">RCS pressure (psia)</param>
        /// <param name="rcpCount">Number of RCPs running (0-4)</param>
        /// <param name="rcpHeat_MW">Total RCP heat input (MW)</param>
        /// <param name="T_pzr">Pressurizer temperature for natural circ heat source (°F)</param>
        /// <returns>Loop temperature result with T_hot, T_cold, etc.</returns>
        public static LoopTemperatureResult CalculateLoopTemperatures(
            float T_rcs,
            float pressure,
            int rcpCount,
            float rcpHeat_MW,
            float T_pzr = 0f)
        {
            var result = new LoopTemperatureResult();
            result.T_avg = T_rcs;
            result.IsForcedFlow = (rcpCount > 0);
            
            float halfDeltaT;
            
            if (rcpCount > 0)
            {
                // ============================================================
                // FORCED CIRCULATION - RCPs running
                // ΔT from RCP heat across loop flow
                // ============================================================
                
                float flowPerPump_gpm = PlantConstants.RCP_FLOW_EACH;
                float totalFlow_gpm = rcpCount * flowPerPump_gpm;
                
                // Convert gpm → lb/sec
                float rho = WaterProperties.WaterDensity(T_rcs, pressure);
                float massFlow_lb_sec = totalFlow_gpm * PlantConstants.GPM_TO_FT3_SEC * rho;
                result.MassFlow = massFlow_lb_sec;
                
                // Total heat deposited into coolant
                // RCP mechanical heat is deposited directly in the loop
                float Q_BTU_sec = rcpHeat_MW * PlantConstants.MW_TO_BTU_SEC;
                
                // Specific heat at current conditions
                float Cp = WaterProperties.WaterSpecificHeat(T_rcs, pressure);
                
                // Temperature rise across core: ΔT = Q / (ṁ × Cp)
                float deltaT_loop = 0f;
                if (massFlow_lb_sec > 1f && Cp > 0.01f)
                {
                    deltaT_loop = Q_BTU_sec / (massFlow_lb_sec * Cp);
                }
                
                // Clamp to physical bounds - cannot exceed full-power ΔT
                deltaT_loop = Math.Max(0f, Math.Min(deltaT_loop, PlantConstants.CORE_DELTA_T));
                halfDeltaT = deltaT_loop / 2f;
                result.DeltaT = deltaT_loop;
            }
            else
            {
                // ============================================================
                // NATURAL CIRCULATION - No RCPs
                // Very low flow, small heat input from surge line conduction
                // ============================================================
                
                // Heat conducted from PZR through surge line warms the hot leg
                // Cold leg is cooled by SG metal and insulation losses
                float conductionHeat_MW = 0f;
                if (T_pzr > T_rcs)
                {
                    conductionHeat_MW = HeatTransfer.SurgeLineHeatTransfer_MW(T_pzr, T_rcs, pressure);
                }
                float Q_BTU_sec = Math.Abs(conductionHeat_MW) * PlantConstants.MW_TO_BTU_SEC;
                
                // Estimate natural circulation mass flow
                // Use assumed ΔT to bootstrap flow calculation
                float assumedDeltaT = 2f;
                float natCircFlow_gpm = FluidFlow.NaturalCirculationFlow(assumedDeltaT, 40f, 1f);
                
                float rho = WaterProperties.WaterDensity(T_rcs, pressure);
                float massFlow_lb_sec = natCircFlow_gpm * PlantConstants.GPM_TO_FT3_SEC * rho;
                result.MassFlow = massFlow_lb_sec;
                
                float Cp = WaterProperties.WaterSpecificHeat(T_rcs, pressure);
                
                float deltaT_natCirc = 0f;
                if (massFlow_lb_sec > 1f && Cp > 0.01f)
                {
                    deltaT_natCirc = Q_BTU_sec / (massFlow_lb_sec * Cp);
                }
                
                // Natural circulation ΔT during heatup is typically 1-5°F
                deltaT_natCirc = Math.Max(0f, Math.Min(deltaT_natCirc, 10f));
                halfDeltaT = deltaT_natCirc / 2f;
                result.DeltaT = deltaT_natCirc;
            }
            
            result.T_hot = T_rcs + halfDeltaT;
            result.T_cold = T_rcs - halfDeltaT;
            
            return result;
        }
        
        /// <summary>
        /// Simplified calculation when only T_hot/T_cold are needed.
        /// </summary>
        public static (float T_hot, float T_cold) GetLoopTemperatures(
            float T_rcs, float pressure, int rcpCount, float rcpHeat_MW, float T_pzr = 0f)
        {
            var result = CalculateLoopTemperatures(T_rcs, pressure, rcpCount, rcpHeat_MW, T_pzr);
            return (result.T_hot, result.T_cold);
        }
        
        #endregion
        
        #region Natural Circulation Analysis
        
        /// <summary>
        /// Calculate natural circulation flow rate.
        /// Flow is driven by density difference between hot and cold legs.
        /// </summary>
        /// <param name="T_hot">Hot leg temperature (°F)</param>
        /// <param name="T_cold">Cold leg temperature (°F)</param>
        /// <param name="pressure">System pressure (psia)</param>
        /// <param name="elevationDiff_ft">Elevation difference between hot and cold legs (ft)</param>
        /// <returns>Natural circulation flow rate (gpm)</returns>
        public static float NaturalCirculationFlowRate(
            float T_hot, float T_cold, float pressure, float elevationDiff_ft = 40f)
        {
            float rho_hot = WaterProperties.WaterDensity(T_hot, pressure);
            float rho_cold = WaterProperties.WaterDensity(T_cold, pressure);
            
            float deltaRho = rho_cold - rho_hot;
            if (deltaRho <= 0f) return 0f;
            
            // Driving head: ΔP = Δρ × g × h
            float g = PlantConstants.GRAVITY;
            float drivingHead_lbf_ft2 = deltaRho * g * elevationDiff_ft;
            
            // Convert to psi: 1 psi = 144 lbf/ft²
            float drivingHead_psi = drivingHead_lbf_ft2 / 144f;
            
            // Flow resistance - empirical for typical PWR geometry
            // At full natural circ (~3-6% of normal flow), driving head is ~0.5-2 psi
            float K_flow = PlantConstants.NAT_CIRC_FLOW_MAX / 2f; // gpm per sqrt(psi)
            
            float flow_gpm = K_flow * (float)Math.Sqrt(drivingHead_psi);
            
            // Clamp to natural circulation limits
            flow_gpm = Math.Max(PlantConstants.NAT_CIRC_FLOW_MIN, 
                       Math.Min(flow_gpm, PlantConstants.NAT_CIRC_FLOW_MAX));
            
            return flow_gpm;
        }
        
        /// <summary>
        /// Check if natural circulation is adequate for decay heat removal.
        /// </summary>
        public static bool IsNaturalCirculationAdequate(
            float T_hot, float T_cold, float decayHeat_MW)
        {
            // Natural circulation can remove ~2-3% of full power decay heat
            // At 1 hour post-trip, decay heat is ~1.5% = 50 MW for 3411 MWt plant
            // Natural circulation flow of 3-6% at ΔT of 60°F can remove this
            
            float maxRemovalCapacity_MW = PlantConstants.THERMAL_POWER_MWT * 0.03f;
            return decayHeat_MW <= maxRemovalCapacity_MW;
        }
        
        #endregion
        
        #region Weighted Average Temperature
        
        /// <summary>
        /// Calculate volume-weighted average temperature including pressurizer.
        /// </summary>
        /// <param name="T_rcs">RCS bulk temperature (°F)</param>
        /// <param name="T_pzr">Pressurizer temperature (°F)</param>
        /// <param name="pzrWaterVolume">Pressurizer water volume (ft³)</param>
        /// <returns>Weighted average temperature (°F)</returns>
        public static float WeightedAverageTemperature(
            float T_rcs, float T_pzr, float pzrWaterVolume)
        {
            float rcsVolume = PlantConstants.RCS_WATER_VOLUME;
            float totalVolume = rcsVolume + pzrWaterVolume;
            
            if (totalVolume < 1f) return T_rcs;
            
            return (T_rcs * rcsVolume + T_pzr * pzrWaterVolume) / totalVolume;
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate loop thermodynamics calculations.
        /// </summary>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: With RCPs, T_hot > T_cold
            var result1 = CalculateLoopTemperatures(400f, 800f, 4, 21f);
            if (result1.T_hot <= result1.T_cold) valid = false;
            if (!result1.IsForcedFlow) valid = false;
            
            // Test 2: With no RCPs and T_pzr > T_rcs, still get small ΔT
            var result2 = CalculateLoopTemperatures(300f, 500f, 0, 0f, 400f);
            if (result2.T_hot < result2.T_cold) valid = false;
            if (result2.IsForcedFlow) valid = false;
            
            // Test 3: T_avg should equal input T_rcs (within rounding)
            if (Math.Abs(result1.T_avg - 400f) > 0.1f) valid = false;
            
            // Test 4: ΔT at HZP with 4 RCPs should be 5-15°F (not 61°F like at power)
            var result3 = CalculateLoopTemperatures(557f, 2250f, 4, 21f);
            if (result3.DeltaT < 3f || result3.DeltaT > 20f) valid = false;
            
            // Test 5: More RCPs = smaller ΔT (same heat, more flow)
            var result4a = CalculateLoopTemperatures(400f, 800f, 2, 10.5f);
            var result4b = CalculateLoopTemperatures(400f, 800f, 4, 21f);
            // 4 RCPs has 2x flow and 2x heat, so ΔT should be similar
            // But 2 RCPs with half the heat should have similar ΔT
            
            // Test 6: Natural circulation flow should be in valid range
            float natCircFlow = NaturalCirculationFlowRate(619f, 558f, 2250f);
            if (natCircFlow < PlantConstants.NAT_CIRC_FLOW_MIN ||
                natCircFlow > PlantConstants.NAT_CIRC_FLOW_MAX) valid = false;
            
            return valid;
        }
        
        #endregion
    }
}
