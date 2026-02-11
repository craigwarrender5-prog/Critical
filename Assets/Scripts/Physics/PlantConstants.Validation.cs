// ============================================================================
// CRITICAL: Master the Atom - Plant Constants (Validation)
// PlantConstants.Validation.cs - Constant Cross-Validation Checks
// ============================================================================
//
// PURPOSE:
//   Validates internal consistency of all plant constants.
//   Checks derived values match base values, ordering constraints,
//   flow balances, and heatup-specific cross-checks.
//   Merges validation from both PlantConstants and PlantConstantsHeatup
//   (the latter removed in v0.3.0).
//
// ARCHITECTURE:
//   Called at startup or test time. Returns false if any check fails.
//   Each section validates constants from a specific domain partial file.
//
// GOLD STANDARD: Yes
// ============================================================================

using System;

namespace Critical.Physics
{
    public static partial class PlantConstants
    {
        #region Validation Methods
        
        /// <summary>
        /// Verify that derived constants are consistent with base values.
        /// Returns true if all validation checks pass.
        /// Covers: RCS, CVCS, VCT, CCP, boron, pressurizer, heatup parameters.
        /// </summary>
        public static bool ValidateConstants()
        {
            bool valid = true;
            
            // ================================================================
            // RCS Core Validation (PlantConstants.cs)
            // ================================================================
            
            // Check T_AVG calculation
            float calcTAvg = (T_HOT + T_COLD) / 2f;
            if (Math.Abs(calcTAvg - T_AVG) > 0.5f)
                valid = false;
            
            // Check CORE_DELTA_T
            float calcDeltaT = T_HOT - T_COLD;
            if (Math.Abs(calcDeltaT - CORE_DELTA_T) > 0.5f)
                valid = false;
            
            // Check RCS_FLOW_TOTAL
            float calcFlow = RCP_COUNT * RCP_FLOW_EACH;
            if (Math.Abs(calcFlow - RCS_FLOW_TOTAL) > 100f)
                valid = false;
            
            // Check RCP heat total
            float calcRcpHeat = RCP_COUNT * RCP_HEAT_MW_EACH;
            if (Math.Abs(calcRcpHeat - RCP_HEAT_MW) > 0.1f)
                valid = false;
            
            // ================================================================
            // Pressurizer Validation (PlantConstants.Pressurizer.cs)
            // ================================================================
            
            // Check PZR volumes
            float calcPzrWater = PZR_TOTAL_VOLUME * 0.6f;
            if (Math.Abs(calcPzrWater - PZR_WATER_VOLUME) > 1f)
                valid = false;
            
            // Check heater control setpoint ordering (normal ops)
            if (P_BACKUP_HEATER_ON >= P_BACKUP_HEATER_OFF)
                valid = false;  // Backup ON must be below OFF (bistable)
            if (P_PROP_HEATER_FULL_ON >= P_PROP_HEATER_ZERO)
                valid = false;  // Full ON must be below zero output
            
            // ================================================================
            // CVCS Validation (PlantConstants.CVCS.cs)
            // ================================================================
            
            // Check CVCS flow balance
            float calcCharging = CHARGING_TO_RCS_GPM + SEAL_INJECTION_TOTAL_GPM;
            if (Math.Abs(calcCharging - CHARGING_NORMAL_GPM) > 1f)
                valid = false;
            
            // Check seal injection total
            float calcSealTotal = RCP_COUNT * SEAL_INJECTION_PER_PUMP_GPM;
            if (Math.Abs(calcSealTotal - SEAL_INJECTION_TOTAL_GPM) > 0.1f)
                valid = false;
            
            // Check seal return total
            float calcSealReturn = RCP_COUNT * SEAL_LEAKOFF_PER_PUMP_GPM;
            if (Math.Abs(calcSealReturn - SEAL_RETURN_NORMAL_GPM) > 0.1f)
                valid = false;
            
            // Check VCT level setpoints are in order
            if (VCT_LEVEL_LOW_LOW >= VCT_LEVEL_LOW ||
                VCT_LEVEL_LOW >= VCT_LEVEL_MAKEUP_START ||
                VCT_LEVEL_MAKEUP_START >= VCT_LEVEL_NORMAL_LOW ||
                VCT_LEVEL_NORMAL_LOW >= VCT_LEVEL_NORMAL_HIGH ||
                VCT_LEVEL_NORMAL_HIGH >= VCT_LEVEL_HIGH ||
                VCT_LEVEL_HIGH >= VCT_LEVEL_HIGH_HIGH)
                valid = false;
            
            // Check CCP capacity consistency
            if (CCP_CAPACITY_GPM >= CCP_WITH_SEALS_GPM)
                valid = false;  // With seals must be greater than without
            if (BUBBLE_DRAIN_CHARGING_CCP_GPM != CCP_CAPACITY_GPM)
                valid = false;  // Drain charging must match CCP capacity
            if (CCP_START_LEVEL <= PZR_LEVEL_AFTER_BUBBLE)
                valid = false;  // CCP must start before drain target
            
            // ================================================================
            // Nuclear Validation (PlantConstants.Nuclear.cs)
            // ================================================================
            
            // Check TOTAL_RODS
            int calcRods = FUEL_ASSEMBLIES * RODS_PER_ASSEMBLY;
            if (calcRods != TOTAL_RODS)
                valid = false;
            
            // Check boron concentrations are in order
            if (BORON_EOL_PPM >= BORON_CRITICAL_HFP_BOL_PPM ||
                BORON_CRITICAL_HFP_BOL_PPM >= BORON_CRITICAL_HZP_BOL_PPM ||
                BORON_CRITICAL_HZP_BOL_PPM >= BORON_COLD_SHUTDOWN_BOL_PPM ||
                BORON_COLD_SHUTDOWN_BOL_PPM >= BORON_RWST_PPM ||
                BORON_RWST_PPM >= BORON_BAT_PPM)
                valid = false;
            
            // ================================================================
            // Heatup Validation (PlantConstants.Heatup.cs)
            // Merged from PlantConstantsHeatup.ValidateConstants()
            // ================================================================
            
            // RCP heat should produce ~50-70°F/hr heatup rate
            // Approximate total heat capacity: water (~780,000 lb × 1.1 Cp) + metal (2,200,000 lb × 0.12 Cp)
            float approxTotalCap = 780000f * 1.1f + 2200000f * 0.12f;
            float approxHeatupRate = (RCP_HEAT_MW * 3412142f) / approxTotalCap;
            if (approxHeatupRate < 40f || approxHeatupRate > 80f)
                valid = false;
            
            // PZR level program should span 25% to 60% over heatup range
            if (GetPZRLevelSetpoint(100f) < 20f || GetPZRLevelSetpoint(100f) > 30f)
                valid = false;
            if (GetPZRLevelSetpoint(557f) < 55f || GetPZRLevelSetpoint(557f) > 65f)
                valid = false;
            
            return valid;
        }
        
        #endregion
    }
}
