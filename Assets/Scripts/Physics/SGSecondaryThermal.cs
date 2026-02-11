// ============================================================================
// CRITICAL: Master the Atom — Steam Generator Secondary Side Thermal Model
// SGSecondaryThermal.cs — Lumped-Parameter SG Secondary Side Heat Sink Model
// ============================================================================
//
// PURPOSE:
//   Models the thermal mass and heat transfer characteristics of the SG
//   secondary side during reactor coolant system heatup operations.
//
// PHYSICS MODEL:
//   - Lumped-parameter representation of all 4 SG secondary sides combined
//   - Secondary side in wet layup (100% filled with water) during cold shutdown
//   - Heat transfer from RCS primary through SG tubes to secondary water
//   - Dynamic heat transfer coefficient based on RCP flow conditions
//
// SIGNIFICANCE:
//   The SG secondary side represents the largest single thermal mass in the
//   system (~1.66 million lb of water + 800,000 lb of metal). During heatup,
//   energy transferred to the secondary side acts as a massive heat sink,
//   slowing RCS temperature rise. Without this model, simulated heatup rates
//   are ~42% too fast (71°F/hr vs 50°F/hr expected).
//
// SCOPE:
//   - Cold shutdown through hot standby (Modes 5→3)
//   - Secondary side subcooled (no steam generation)
//   - Natural circulation on secondary side (no secondary pumps)
//   - Turbine isolated (not modeled, not relevant during heatup)
//
// VALIDATION TARGETS:
//   - RCS heatup rate with 4 RCPs: ~45-55°F/hr (target: ~50°F/hr per NRC HRTD)
//   - SG secondary temperature lags RCS by ~10-20°F (realistic thermal lag)
//   - Minimal coupling when RCPs stopped (natural convection only)
//
// SOURCES:
//   - NRC HRTD 19.2.2: Reactor Coolant System Heatup
//   - Westinghouse 4-Loop PWR Technical Specifications
//   - Implementation Plan v0.8.0, v1.1.0 (Stage 1: SG HTC correction)
//
// UNITS:
//   Temperature: °F | Heat Rate: BTU/hr | Heat Capacity: BTU/°F
//   Mass: lb | Area: ft² | HTC: BTU/(hr·ft²·°F) | Time: hr
//
// VERSION: 1.1.2 (Thermal Stratification Boundary Layer Model)
// CLASSIFICATION: Physics — Realism Critical
// GOLD STANDARD: Yes
// ============================================================================

using System;
using UnityEngine;

namespace Critical.Physics
{
    /// <summary>
    /// Steam Generator Secondary Side Thermal Model.
    /// Lumped-parameter representation of all 4 SGs during heatup operations.
    /// All constants sourced from PlantConstants for consistency.
    /// </summary>
    public static class SGSecondaryThermal
    {
        // ============================================================================
        // PUBLIC METHODS
        // ============================================================================
        
        /// <summary>
        /// Calculates the total heat capacity of the SG secondary side at current temperature.
        /// 
        /// Heat capacity includes both metal and water components:
        /// C_total = (M_metal × Cp_steel) + (M_water × Cp_water(T))
        /// 
        /// The water specific heat varies with temperature.
        /// </summary>
        /// <param name="temperatureF">Current SG secondary temperature (°F)</param>
        /// <param name="pressurePsia">System pressure (psia) - affects water properties</param>
        /// <returns>Total heat capacity (BTU/°F)</returns>
        public static float GetSecondaryHeatCapacity(float temperatureF, float pressurePsia)
        {
            // Metal component (constant) - uses PlantConstants
            float metalHeatCapacity = PlantConstants.SG_SECONDARY_TOTAL_METAL_MASS_LB * PlantConstants.STEEL_CP;
            
            // Water component (temperature-dependent)
            float cpWater = WaterProperties.WaterSpecificHeat(temperatureF, pressurePsia);
            float waterHeatCapacity = PlantConstants.SG_SECONDARY_TOTAL_WATER_MASS_LB * cpWater;
            
            return metalHeatCapacity + waterHeatCapacity;
        }
        
        /// <summary>
        /// Calculates heat transfer rate from RCS primary to SG secondary side.
        /// 
        /// Uses classic heat exchanger equation with boundary layer correction:
        /// Q = U × A × ΔT_effective
        /// 
        /// Where:
        /// - U = overall heat transfer coefficient (BTU/hr·ft²·°F)
        /// - A = total tube surface area (ft²)
        /// - ΔT_effective = bulk ΔT × boundary layer factor (°F)
        /// 
        /// The HTC (U) depends on RCP operating status AND secondary temperature (v1.1.1):
        /// - No RCPs: Natural convection only (~10 BTU/hr·ft²·°F)
        /// - RCPs running at low temp: Scaled HTC (~30-60 BTU/hr·ft²·°F)
        /// - RCPs running at high temp: Full HTC (~100 BTU/hr·ft²·°F)
        /// - HZP with steaming: Nucleate boiling secondary (~500 BTU/hr·ft²·°F)
        /// 
        /// Positive Q means heat flows from primary to secondary (normal during heatup).
        /// 
        /// v1.1.1 FIX: Now uses temperature-dependent HTC scaling to properly model
        /// reduced natural convection at low temperatures per Churchill-Chu correlation.
        /// 
        /// v1.1.2 FIX: Now applies boundary layer effectiveness factor to account for
        /// thermal stratification in stagnant wet-layup secondary. This reduces
        /// effective ΔT at cold temps, reducing heat absorption from ~14 MW to ~4-5 MW.
        /// </summary>
        /// <param name="T_rcs">RCS average temperature (°F)</param>
        /// <param name="T_sg_secondary">SG secondary average temperature (°F)</param>
        /// <param name="rcpsRunning">Number of RCPs currently operating (0-4)</param>
        /// <returns>Heat transfer rate to secondary side (BTU/hr), positive = heating secondary</returns>
        public static float CalculateHeatTransfer(float T_rcs, float T_sg_secondary, int rcpsRunning)
        {
            // v1.1.1 FIX: Use temperature-scaled HTC for realistic low-temperature behavior
            // Select appropriate heat transfer coefficient based on RCP status AND temperature
            float htc = GetCurrentHTC(rcpsRunning, T_sg_secondary, isSteaming: false);
            
            // Bulk temperature difference
            float bulkDeltaT = T_rcs - T_sg_secondary;
            
            // v1.1.2 FIX: Apply boundary layer effectiveness factor
            // Thermal stratification in stagnant secondary reduces effective ΔT
            float boundaryFactor = GetBoundaryLayerFactor(T_sg_secondary, isSteaming: false);
            float effectiveDeltaT = bulkDeltaT * boundaryFactor;
            
            // Heat transfer rate: Q = U × A × ΔT_effective
            float heatTransferRate = htc * PlantConstants.SG_TUBE_AREA_TOTAL_FT2 * effectiveDeltaT;
            
            return heatTransferRate;
        }
        
        /// <summary>
        /// Updates the SG secondary temperature based on heat transfer from primary.
        /// 
        /// Uses lumped capacitance method:
        /// dT/dt = Q / C
        /// 
        /// Where:
        /// - Q = heat transfer rate from primary (BTU/hr)
        /// - C = secondary side heat capacity (BTU/°F)
        /// - dT/dt = rate of temperature change (°F/hr)
        /// 
        /// Integrates over the timestep using forward Euler method (simple, stable for
        /// small timesteps with large thermal mass).
        /// </summary>
        /// <param name="currentTempF">Current SG secondary temperature (°F)</param>
        /// <param name="heatTransferRate">Heat being added from primary (BTU/hr)</param>
        /// <param name="pressurePsia">System pressure (psia)</param>
        /// <param name="timestepHours">Simulation timestep (hours)</param>
        /// <returns>New SG secondary temperature (°F)</returns>
        public static float UpdateSecondaryTemperature(
            float currentTempF,
            float heatTransferRate,
            float pressurePsia,
            float timestepHours)
        {
            // Get current heat capacity
            float heatCapacity = GetSecondaryHeatCapacity(currentTempF, pressurePsia);
            
            // Calculate rate of temperature change (°F/hr)
            float dT_dt = heatTransferRate / heatCapacity;
            
            // Integrate over timestep
            float newTemperature = currentTempF + (dT_dt * timestepHours);
            
            return newTemperature;
        }
        
        /// <summary>
        /// Initializes SG secondary temperature at start of simulation.
        /// 
        /// During cold shutdown, the entire plant is in thermal equilibrium at ambient
        /// temperature. The SG secondary side should start at the same temperature as
        /// the RCS to reflect this equilibrium state.
        /// 
        /// Per NRC HRTD 19.2.2 initial conditions: "Steam generators filled to wet-layup (100%)"
        /// with RCS and SG at same cold shutdown temperature (~70-100°F).
        /// </summary>
        /// <param name="rcsStartTemperatureF">RCS starting temperature (°F)</param>
        /// <returns>Initial SG secondary temperature (°F) - same as RCS</returns>
        public static float InitializeSecondaryTemperature(float rcsStartTemperatureF)
        {
            return rcsStartTemperatureF;
        }
        
        // ============================================================================
        // DIAGNOSTIC METHODS
        // ============================================================================
        
        /// <summary>
        /// Gets the effective thermal mass contribution to the overall system.
        /// 
        /// This is used for diagnostic purposes to understand how much the SG secondary
        /// affects overall system thermal inertia. The actual coupling is dynamic through
        /// heat transfer, but this gives a sense of scale.
        /// </summary>
        /// <param name="temperatureF">Current temperature (°F)</param>
        /// <param name="pressurePsia">System pressure (psia)</param>
        /// <returns>SG secondary heat capacity (BTU/°F)</returns>
        public static float GetThermalMassContribution(float temperatureF, float pressurePsia)
        {
            return GetSecondaryHeatCapacity(temperatureF, pressurePsia);
        }
        
        /// <summary>
        /// Gets diagnostic information about current operating regime.
        /// Useful for validation logging and debugging.
        /// </summary>
        /// <param name="rcpsRunning">Number of RCPs operating</param>
        /// <returns>String describing current heat transfer regime</returns>
        public static string GetOperatingRegime(int rcpsRunning)
        {
            if (rcpsRunning == 0)
                return "Natural Convection (No RCPs)";
            else if (rcpsRunning <= 2)
                return $"Forced Primary ({rcpsRunning} RCPs)";
            else
                return $"Forced Primary ({rcpsRunning} RCPs, Full Flow)";
        }
        
        /// <summary>
        /// Gets the current heat transfer coefficient being used.
        /// Useful for validation and debugging.
        /// 
        /// Returns appropriate HTC based on RCP status and steaming condition:
        /// - No RCPs: Natural convection only (~10 BTU/hr·ft²·°F)
        /// - RCPs running, subcooled: Forced primary, natural secondary (~100 BTU/hr·ft²·°F)
        /// - RCPs running, steaming: Forced primary, boiling secondary (~500 BTU/hr·ft²·°F)
        /// 
        /// NOTE: This overload returns the BASE HTC without temperature scaling.
        /// For actual heat transfer calculations, use GetCurrentHTC(rcpsRunning, T_secondary)
        /// which applies temperature-dependent scaling per v1.1.1 fix.
        /// </summary>
        /// <param name="rcpsRunning">Number of RCPs operating</param>
        /// <param name="isSteaming">True if secondary side is at saturation (steaming)</param>
        /// <returns>Base heat transfer coefficient (BTU/hr·ft²·°F) without temperature scaling</returns>
        public static float GetCurrentHTC(int rcpsRunning, bool isSteaming = false)
        {
            if (rcpsRunning == 0)
                return PlantConstants.SG_HTC_NO_FLOW;
            else if (isSteaming)
                return PlantConstants.SG_HTC_BOILING;
            else
                return PlantConstants.SG_HTC_NATURAL_CONVECTION;
        }
        
        /// <summary>
        /// Gets the temperature-scaled heat transfer coefficient (v1.1.1 FIX).
        /// 
        /// At low temperatures, natural convection HTC is significantly lower because
        /// lower temperatures produce lower Rayleigh numbers, which produce lower
        /// Nusselt numbers and thus lower heat transfer coefficients.
        /// 
        /// The scaling is based on Churchill-Chu correlation analysis:
        ///   100°F: Ra ≈ 10^7, scale = 0.3 (HTC ≈ 30 BTU/hr·ft²·°F)
        ///   300°F: Ra ≈ 10^8, scale = 0.6 (HTC ≈ 60 BTU/hr·ft²·°F)
        ///   500°F: Ra ≈ 10^9, scale = 1.0 (HTC ≈ 100 BTU/hr·ft²·°F)
        /// 
        /// Linear interpolation is used between anchor points.
        /// 
        /// This fix reduces SG heat absorption at low temperatures from ~14 MW
        /// to ~4-6 MW, increasing heatup rate from ~26°F/hr to ~50°F/hr.
        /// </summary>
        /// <param name="rcpsRunning">Number of RCPs operating</param>
        /// <param name="T_secondary">SG secondary temperature (°F)</param>
        /// <param name="isSteaming">True if secondary side is at saturation (steaming)</param>
        /// <returns>Temperature-scaled heat transfer coefficient (BTU/hr·ft²·°F)</returns>
        public static float GetCurrentHTC(int rcpsRunning, float T_secondary, bool isSteaming = false)
        {
            // Get base HTC for current operating regime
            float baseHTC = GetCurrentHTC(rcpsRunning, isSteaming);
            
            // No temperature scaling for:
            // - No-flow conditions (natural convection both sides, already very low)
            // - Steaming conditions (boiling HTC dominates, not temperature-limited)
            if (rcpsRunning == 0 || isSteaming)
                return baseHTC;
            
            // Apply temperature-dependent scaling for subcooled natural convection
            float scale = GetHTCTemperatureScale(T_secondary);
            
            return baseHTC * scale;
        }
        
        /// <summary>
        /// Calculate HTC temperature scaling factor based on Churchill-Chu correlation.
        /// 
        /// Uses piecewise linear interpolation between anchor points:
        ///   T ≤ 100°F:        scale = 0.3 (clamped minimum)
        ///   100°F < T < 300°F: scale = lerp(0.3, 0.6)
        ///   300°F < T < 500°F: scale = lerp(0.6, 1.0)
        ///   T ≥ 500°F:        scale = 1.0 (full HTC)
        /// </summary>
        /// <param name="T_secondary">SG secondary temperature (°F)</param>
        /// <returns>Scaling factor (0.3 to 1.0)</returns>
        public static float GetHTCTemperatureScale(float T_secondary)
        {
            // Clamp to minimum at low temperatures
            if (T_secondary <= PlantConstants.SG_HTC_TEMP_SCALE_MIN_TEMP_F)
                return PlantConstants.SG_HTC_TEMP_SCALE_MIN;
            
            // Full HTC at high temperatures
            if (T_secondary >= PlantConstants.SG_HTC_TEMP_SCALE_MAX_TEMP_F)
                return PlantConstants.SG_HTC_TEMP_SCALE_MAX;
            
            // Piecewise linear interpolation
            if (T_secondary <= PlantConstants.SG_HTC_TEMP_SCALE_MID_TEMP_F)
            {
                // Interpolate between MIN and MID (100°F to 300°F)
                float t = (T_secondary - PlantConstants.SG_HTC_TEMP_SCALE_MIN_TEMP_F) /
                          (PlantConstants.SG_HTC_TEMP_SCALE_MID_TEMP_F - PlantConstants.SG_HTC_TEMP_SCALE_MIN_TEMP_F);
                return PlantConstants.SG_HTC_TEMP_SCALE_MIN +
                       t * (PlantConstants.SG_HTC_TEMP_SCALE_MID - PlantConstants.SG_HTC_TEMP_SCALE_MIN);
            }
            else
            {
                // Interpolate between MID and MAX (300°F to 500°F)
                float t = (T_secondary - PlantConstants.SG_HTC_TEMP_SCALE_MID_TEMP_F) /
                          (PlantConstants.SG_HTC_TEMP_SCALE_MAX_TEMP_F - PlantConstants.SG_HTC_TEMP_SCALE_MID_TEMP_F);
                return PlantConstants.SG_HTC_TEMP_SCALE_MID +
                       t * (PlantConstants.SG_HTC_TEMP_SCALE_MAX - PlantConstants.SG_HTC_TEMP_SCALE_MID);
            }
        }
        
        /// <summary>
        /// Calculate boundary layer effectiveness factor for thermal stratification.
        /// 
        /// During cold heatup with stagnant secondary, thermal stratification
        /// causes the tube boundary layer temperature to approach the RCS temperature.
        /// This reduces the effective ΔT driving heat transfer.
        /// 
        /// The physics:
        /// - SG secondary in wet layup: 214,000 lb water per SG, NO circulation
        /// - Richardson number Ri ≈ 27,000 (>> 10 indicates strong stratification)
        /// - Hot boundary layer forms near tubes, cold bulk remains stratified
        /// - Effective ΔT at tube surface << bulk ΔT
        /// 
        /// Factor values:
        ///   T ≤ 150°F:  0.30 (severe stratification, Ri >> 10)
        ///   T = 300°F:  0.55 (moderate stratification, improved buoyancy)
        ///   T ≥ 500°F:  0.90 (good natural circulation from strong buoyancy)
        ///   Steaming:   1.00 (boiling provides vigorous circulation)
        /// 
        /// v1.1.2 FIX: Accounts for thermal stratification in wet layup secondary.
        /// This reduces SG heat absorption from ~14 MW to ~4-5 MW at cold temps,
        /// increasing heatup rate from ~26°F/hr to ~50°F/hr.
        /// </summary>
        /// <param name="T_secondary">SG secondary bulk temperature (°F)</param>
        /// <param name="isSteaming">True if secondary is at saturation (steaming)</param>
        /// <returns>Boundary layer effectiveness factor (0.30 to 1.0)</returns>
        public static float GetBoundaryLayerFactor(float T_secondary, bool isSteaming)
        {
            // Steaming conditions: boiling provides vigorous natural circulation
            // No thermal stratification - full bulk ΔT applies
            if (isSteaming)
                return PlantConstants.SG_BOUNDARY_LAYER_FACTOR_STEAMING;
            
            // Clamp to minimum at low temperatures (severe stratification)
            if (T_secondary <= PlantConstants.SG_BOUNDARY_LAYER_TEMP_MIN_F)
                return PlantConstants.SG_BOUNDARY_LAYER_FACTOR_MIN;
            
            // High factor at elevated temperatures (good buoyancy-driven mixing)
            if (T_secondary >= PlantConstants.SG_BOUNDARY_LAYER_TEMP_HIGH_F)
                return PlantConstants.SG_BOUNDARY_LAYER_FACTOR_HIGH;
            
            // Piecewise linear interpolation
            if (T_secondary <= PlantConstants.SG_BOUNDARY_LAYER_TEMP_MID_F)
            {
                // Interpolate between MIN and MID (150°F to 300°F)
                float t = (T_secondary - PlantConstants.SG_BOUNDARY_LAYER_TEMP_MIN_F) /
                          (PlantConstants.SG_BOUNDARY_LAYER_TEMP_MID_F - PlantConstants.SG_BOUNDARY_LAYER_TEMP_MIN_F);
                return PlantConstants.SG_BOUNDARY_LAYER_FACTOR_MIN +
                       t * (PlantConstants.SG_BOUNDARY_LAYER_FACTOR_MID - PlantConstants.SG_BOUNDARY_LAYER_FACTOR_MIN);
            }
            else
            {
                // Interpolate between MID and HIGH (300°F to 500°F)
                float t = (T_secondary - PlantConstants.SG_BOUNDARY_LAYER_TEMP_MID_F) /
                          (PlantConstants.SG_BOUNDARY_LAYER_TEMP_HIGH_F - PlantConstants.SG_BOUNDARY_LAYER_TEMP_MID_F);
                return PlantConstants.SG_BOUNDARY_LAYER_FACTOR_MID +
                       t * (PlantConstants.SG_BOUNDARY_LAYER_FACTOR_HIGH - PlantConstants.SG_BOUNDARY_LAYER_FACTOR_MID);
            }
        }
        
        // ============================================================================
        // HZP STEAMING METHODS — v1.1.0 Stage 2
        // ============================================================================
        
        /// <summary>
        /// Result structure for HZP secondary side update with steam dump.
        /// </summary>
        public struct HZPSecondaryResult
        {
            /// <summary>New SG secondary temperature (°F)</summary>
            public float Temperature_F;
            
            /// <summary>SG secondary pressure (psia)</summary>
            public float Pressure_psia;
            
            /// <summary>SG secondary pressure (psig)</summary>
            public float Pressure_psig;
            
            /// <summary>Heat transfer from primary to secondary (MW)</summary>
            public float HeatTransfer_MW;
            
            /// <summary>True if secondary is at saturation (steaming)</summary>
            public bool IsSteaming;
            
            /// <summary>Net heat to secondary after steam dump (MW)</summary>
            public float NetHeat_MW;
        }
        
        /// <summary>
        /// Check if SG secondary side has reached steaming conditions.
        /// 
        /// Secondary side begins steaming when temperature reaches saturation
        /// temperature for the current secondary pressure. During heatup,
        /// secondary pressure is controlled by the steam header / steam dump system.
        /// 
        /// At HZP, secondary pressure is ~1092 psig (1107 psia), corresponding
        /// to saturation temperature of ~556°F.
        /// </summary>
        /// <param name="T_sg_secondary">SG secondary temperature (°F)</param>
        /// <param name="secondaryPressure_psia">SG secondary pressure (psia)</param>
        /// <returns>True if secondary side is at or above saturation</returns>
        public static bool IsSteaming(float T_sg_secondary, float secondaryPressure_psia)
        {
            float T_sat = WaterProperties.SaturationTemperature(secondaryPressure_psia);
            // Consider steaming when within 2°F of saturation (allows for model tolerance)
            return T_sg_secondary >= T_sat - 2f;
        }
        
        /// <summary>
        /// Calculate SG secondary side pressure based on temperature.
        /// 
        /// During heatup, secondary pressure follows saturation pressure
        /// corresponding to secondary temperature. At HZP, pressure is
        /// controlled by steam dump system at ~1092 psig.
        /// </summary>
        /// <param name="T_sg_secondary">SG secondary temperature (°F)</param>
        /// <returns>SG secondary pressure (psia)</returns>
        public static float GetSecondaryPressure(float T_sg_secondary)
        {
            // Secondary pressure equals saturation pressure at current temperature
            // (assuming saturated conditions when steaming)
            return WaterProperties.SaturationPressure(T_sg_secondary);
        }
        
        /// <summary>
        /// Update SG secondary side for HZP conditions with steam dump.
        /// 
        /// At HZP, the SG secondary side is at saturation, generating steam.
        /// The steam dump system removes excess heat by dumping steam to condenser.
        /// Heat balance: Q_primary_transfer = Q_steam_dump + Q_to_raise_secondary_temp
        /// 
        /// When steam dump is active, secondary temperature and pressure are
        /// controlled by modulating steam dump valve position.
        /// </summary>
        /// <param name="T_rcs">RCS average temperature (°F)</param>
        /// <param name="T_sg_secondary">Current SG secondary temperature (°F)</param>
        /// <param name="rcpsRunning">Number of RCPs operating</param>
        /// <param name="steamDumpHeat_MW">Heat removed by steam dump (MW)</param>
        /// <param name="pressurePsia">RCS/primary pressure (psia)</param>
        /// <param name="timestepHours">Simulation timestep (hours)</param>
        /// <returns>HZP secondary result with updated state</returns>
        public static HZPSecondaryResult UpdateHZPSecondary(
            float T_rcs,
            float T_sg_secondary,
            int rcpsRunning,
            float steamDumpHeat_MW,
            float pressurePsia,
            float timestepHours)
        {
            var result = new HZPSecondaryResult();
            
            // Current secondary pressure (from temperature)
            float secondaryPressure_psia = GetSecondaryPressure(T_sg_secondary);
            result.Pressure_psia = secondaryPressure_psia;
            result.Pressure_psig = secondaryPressure_psia - 14.7f;
            
            // Check if steaming
            result.IsSteaming = IsSteaming(T_sg_secondary, secondaryPressure_psia);
            
            // v1.1.1 FIX: Get temperature-scaled HTC based on steaming condition
            float htc = GetCurrentHTC(rcpsRunning, T_sg_secondary, result.IsSteaming);
            
            // Bulk temperature difference
            float bulkDeltaT = T_rcs - T_sg_secondary;
            
            // v1.1.2 FIX: Apply boundary layer effectiveness factor
            float boundaryFactor = GetBoundaryLayerFactor(T_sg_secondary, result.IsSteaming);
            float effectiveDeltaT = bulkDeltaT * boundaryFactor;
            
            // Heat transfer from primary to secondary (BTU/hr)
            float heatTransfer_BTU_hr = htc * PlantConstants.SG_TUBE_AREA_TOTAL_FT2 * effectiveDeltaT;
            
            // Convert to MW for reporting
            result.HeatTransfer_MW = heatTransfer_BTU_hr / PlantConstants.MW_TO_BTU_HR;
            
            // Net heat to secondary = primary transfer - steam dump removal
            // Convert steam dump from MW to BTU/hr for calculation
            float steamDump_BTU_hr = steamDumpHeat_MW * PlantConstants.MW_TO_BTU_HR;
            float netHeat_BTU_hr = heatTransfer_BTU_hr - steamDump_BTU_hr;
            result.NetHeat_MW = netHeat_BTU_hr / PlantConstants.MW_TO_BTU_HR;
            
            // Get heat capacity
            float heatCapacity = GetSecondaryHeatCapacity(T_sg_secondary, secondaryPressure_psia);
            
            // Calculate temperature change
            float dT_dt = netHeat_BTU_hr / heatCapacity;
            result.Temperature_F = T_sg_secondary + (dT_dt * timestepHours);
            
            // At steaming conditions, temperature is clamped near saturation
            // (excess heat generates more steam rather than raising temperature)
            if (result.IsSteaming)
            {
                float T_sat = WaterProperties.SaturationTemperature(secondaryPressure_psia);
                
                // If net heat positive and at saturation, steam is generated
                // Temperature rises slowly as pressure rises
                if (netHeat_BTU_hr > 0)
                {
                    // Pressure rises when heat exceeds steam dump capacity
                    float pressureChange_psi_hr = (result.NetHeat_MW) * 
                        PlantConstants.SteamDump.SG_PRESSURE_RISE_RATE_PSI_HR_MW;
                    float newPressure_psia = secondaryPressure_psia + (pressureChange_psi_hr * timestepHours);
                    result.Pressure_psia = newPressure_psia;
                    result.Pressure_psig = newPressure_psia - 14.7f;
                    
                    // Temperature follows new saturation
                    result.Temperature_F = WaterProperties.SaturationTemperature(newPressure_psia);
                }
                else
                {
                    // Steam dump exceeds heat input: pressure/temp decrease
                    float pressureChange_psi_hr = (result.NetHeat_MW) * 
                        PlantConstants.SteamDump.SG_PRESSURE_RISE_RATE_PSI_HR_MW;
                    float newPressure_psia = secondaryPressure_psia + (pressureChange_psi_hr * timestepHours);
                    
                    // Don't let pressure drop below practical minimum
                    newPressure_psia = Mathf.Max(newPressure_psia, 
                        PlantConstants.SteamDump.STEAM_DUMP_MIN_PRESSURE_PSIG + 14.7f);
                    
                    result.Pressure_psia = newPressure_psia;
                    result.Pressure_psig = newPressure_psia - 14.7f;
                    result.Temperature_F = WaterProperties.SaturationTemperature(newPressure_psia);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Simplified heat transfer calculation for HZP conditions.
        /// Returns heat transfer in MW (convenient for heat balance calculations).
        /// 
        /// v1.1.1 FIX: Now uses temperature-dependent HTC scaling.
        /// v1.1.2 FIX: Now applies boundary layer effectiveness factor.
        /// </summary>
        /// <param name="T_rcs">RCS average temperature (°F)</param>
        /// <param name="T_sg_secondary">SG secondary temperature (°F)</param>
        /// <param name="rcpsRunning">Number of RCPs operating</param>
        /// <param name="isSteaming">True if secondary is at saturation</param>
        /// <returns>Heat transfer rate in MW (positive = to secondary)</returns>
        public static float CalculateHeatTransfer_MW(
            float T_rcs,
            float T_sg_secondary,
            int rcpsRunning,
            bool isSteaming = false)
        {
            // v1.1.1 FIX: Use temperature-scaled HTC
            float htc = GetCurrentHTC(rcpsRunning, T_sg_secondary, isSteaming);
            
            // Bulk temperature difference
            float bulkDeltaT = T_rcs - T_sg_secondary;
            
            // v1.1.2 FIX: Apply boundary layer effectiveness factor
            float boundaryFactor = GetBoundaryLayerFactor(T_sg_secondary, isSteaming);
            float effectiveDeltaT = bulkDeltaT * boundaryFactor;
            
            float heatTransfer_BTU_hr = htc * PlantConstants.SG_TUBE_AREA_TOTAL_FT2 * effectiveDeltaT;
            return heatTransfer_BTU_hr / PlantConstants.MW_TO_BTU_HR;
        }
    }
}
