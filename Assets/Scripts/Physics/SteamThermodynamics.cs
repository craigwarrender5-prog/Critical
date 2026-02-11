// CRITICAL: Master the Atom - Phase 1 Core Physics Engine
// SteamThermodynamics.cs - Two-Phase Flow and Quality Calculations
//
// Implements: Gap #13 - Steam quality/void fraction calculations
// Units: Standard PWR units (°F, psia, BTU/lb, lb/ft³)

using System;

namespace Critical.Physics
{
    /// <summary>
    /// Two-phase thermodynamic calculations for steam-water mixtures.
    /// Handles quality, void fraction, and two-phase properties.
    /// Critical for pressurizer steam space modeling.
    /// </summary>
    public static class SteamThermodynamics
    {
        #region Steam Quality
        
        /// <summary>
        /// Calculate steam quality from enthalpy.
        /// Quality x = (h - hf) / hfg, where 0 ≤ x ≤ 1.
        /// </summary>
        /// <param name="enthalpy_BTU_lb">Specific enthalpy in BTU/lb</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Steam quality (0 = saturated liquid, 1 = saturated vapor)</returns>
        public static float SteamQuality(float enthalpy_BTU_lb, float pressure_psia)
        {
            float hf = WaterProperties.SaturatedLiquidEnthalpy(pressure_psia);
            float hfg = WaterProperties.LatentHeat(pressure_psia);
            
            if (hfg < 1f) return 1f; // Near critical point
            
            float x = (enthalpy_BTU_lb - hf) / hfg;
            
            // Clamp to valid range
            return Math.Max(0f, Math.Min(x, 1f));
        }
        
        /// <summary>
        /// Calculate enthalpy from steam quality.
        /// h = hf + x * hfg
        /// </summary>
        /// <param name="quality">Steam quality (0-1)</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Specific enthalpy in BTU/lb</returns>
        public static float TwoPhaseEnthalpy(float quality, float pressure_psia)
        {
            quality = Math.Max(0f, Math.Min(quality, 1f));
            
            float hf = WaterProperties.SaturatedLiquidEnthalpy(pressure_psia);
            float hfg = WaterProperties.LatentHeat(pressure_psia);
            
            return hf + quality * hfg;
        }
        
        /// <summary>
        /// Calculate two-phase mixture density from quality.
        /// Uses homogeneous equilibrium model: 1/ρ = (1-x)/ρf + x/ρg
        /// </summary>
        /// <param name="quality">Steam quality (0-1)</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Mixture density in lb/ft³</returns>
        public static float TwoPhaseDensity(float quality, float pressure_psia)
        {
            quality = Math.Max(0f, Math.Min(quality, 1f));
            
            float tSat = WaterProperties.SaturationTemperature(pressure_psia);
            float rhoF = WaterProperties.WaterDensity(tSat, pressure_psia);
            float rhoG = WaterProperties.SaturatedSteamDensity(pressure_psia);
            
            // Specific volume model
            float vf = 1f / rhoF;
            float vg = 1f / rhoG;
            float v = (1f - quality) * vf + quality * vg;
            
            return 1f / v;
        }
        
        #endregion
        
        #region Void Fraction
        
        /// <summary>
        /// Calculate void fraction (volume fraction of vapor) from quality.
        /// Uses slip ratio correlation for vertical flow.
        /// α = x * vg / (x * vg + (1-x) * vf * S)
        /// where S is the slip ratio.
        /// </summary>
        /// <param name="quality">Steam quality (0-1)</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Void fraction (0-1)</returns>
        public static float VoidFraction(float quality, float pressure_psia)
        {
            if (quality <= 0f) return 0f;
            if (quality >= 1f) return 1f;
            
            float tSat = WaterProperties.SaturationTemperature(pressure_psia);
            float rhoF = WaterProperties.WaterDensity(tSat, pressure_psia);
            float rhoG = WaterProperties.SaturatedSteamDensity(pressure_psia);
            
            // Slip ratio using Chisholm correlation
            // S = (ρf/ρg)^0.25 for vertical upward flow
            float S = (float)Math.Pow(rhoF / rhoG, 0.25);
            
            // Void fraction formula
            float vf = 1f / rhoF;
            float vg = 1f / rhoG;
            
            float alpha = (quality * vg) / (quality * vg + (1f - quality) * vf * S);
            
            return Math.Max(0f, Math.Min(alpha, 1f));
        }
        
        /// <summary>
        /// Calculate void fraction using homogeneous model (no slip).
        /// Simpler but less accurate for separated flows.
        /// </summary>
        /// <param name="quality">Steam quality (0-1)</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Void fraction (0-1)</returns>
        public static float VoidFractionHomogeneous(float quality, float pressure_psia)
        {
            if (quality <= 0f) return 0f;
            if (quality >= 1f) return 1f;
            
            float tSat = WaterProperties.SaturationTemperature(pressure_psia);
            float rhoF = WaterProperties.WaterDensity(tSat, pressure_psia);
            float rhoG = WaterProperties.SaturatedSteamDensity(pressure_psia);
            
            // Homogeneous void fraction (slip ratio S = 1)
            float alpha = 1f / (1f + (1f - quality) / quality * rhoG / rhoF);
            
            return Math.Max(0f, Math.Min(alpha, 1f));
        }
        
        /// <summary>
        /// Calculate steam quality from void fraction (inverse of VoidFraction).
        /// Uses iterative solution.
        /// </summary>
        /// <param name="voidFraction">Void fraction (0-1)</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Steam quality (0-1)</returns>
        public static float QualityFromVoidFraction(float voidFraction, float pressure_psia)
        {
            if (voidFraction <= 0f) return 0f;
            if (voidFraction >= 1f) return 1f;
            
            // Use bisection method for robustness
            float xLow = 0f;
            float xHigh = 1f;
            float xMid = 0.5f;
            
            for (int i = 0; i < 20; i++)
            {
                xMid = (xLow + xHigh) / 2f;
                float alphaMid = VoidFraction(xMid, pressure_psia);
                
                if (Math.Abs(alphaMid - voidFraction) < 1e-6f)
                    break;
                
                if (alphaMid < voidFraction)
                    xLow = xMid;
                else
                    xHigh = xMid;
            }
            
            return xMid;
        }
        
        #endregion
        
        #region Two-Phase Flow Properties
        
        /// <summary>
        /// Calculate two-phase specific volume.
        /// v = (1-x)*vf + x*vg
        /// </summary>
        /// <param name="quality">Steam quality (0-1)</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Specific volume in ft³/lb</returns>
        public static float TwoPhaseSpecificVolume(float quality, float pressure_psia)
        {
            return 1f / TwoPhaseDensity(quality, pressure_psia);
        }
        
        /// <summary>
        /// Calculate two-phase internal energy from quality.
        /// u = uf + x * ufg
        /// </summary>
        /// <param name="quality">Steam quality (0-1)</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Specific internal energy in BTU/lb</returns>
        public static float TwoPhaseInternalEnergy(float quality, float pressure_psia)
        {
            quality = Math.Max(0f, Math.Min(quality, 1f));
            
            // u = h - Pv
            // For saturated states: uf = hf - P*vf, ug = hg - P*vg
            float hf = WaterProperties.SaturatedLiquidEnthalpy(pressure_psia);
            float hg = WaterProperties.SaturatedSteamEnthalpy(pressure_psia);
            
            float tSat = WaterProperties.SaturationTemperature(pressure_psia);
            float vf = 1f / WaterProperties.WaterDensity(tSat, pressure_psia);
            float vg = 1f / WaterProperties.SaturatedSteamDensity(pressure_psia);
            
            // Convert P*v to BTU/lb (1 psia·ft³ = 0.185 BTU)
            float Pv_factor = pressure_psia * 0.185f;
            
            float uf = hf - Pv_factor * vf;
            float ug = hg - Pv_factor * vg;
            float ufg = ug - uf;
            
            return uf + quality * ufg;
        }
        
        /// <summary>
        /// Determine phase state at given conditions.
        /// </summary>
        /// <param name="temp_F">Temperature in °F</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Phase state enumeration</returns>
        public static PhaseState DeterminePhase(float temp_F, float pressure_psia)
        {
            float tSat = WaterProperties.SaturationTemperature(pressure_psia);
            float tolerance = 0.5f; // °F
            
            if (temp_F < tSat - tolerance)
                return PhaseState.SubcooledLiquid;
            else if (temp_F > tSat + tolerance)
                return PhaseState.SuperheatedSteam;
            else
                return PhaseState.TwoPhase;
        }
        
        /// <summary>
        /// Determine phase state from enthalpy.
        /// </summary>
        /// <param name="enthalpy_BTU_lb">Specific enthalpy in BTU/lb</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Phase state enumeration</returns>
        public static PhaseState DeterminePhaseFromEnthalpy(float enthalpy_BTU_lb, float pressure_psia)
        {
            float hf = WaterProperties.SaturatedLiquidEnthalpy(pressure_psia);
            float hg = WaterProperties.SaturatedSteamEnthalpy(pressure_psia);
            float tolerance = 1f; // BTU/lb
            
            if (enthalpy_BTU_lb < hf - tolerance)
                return PhaseState.SubcooledLiquid;
            else if (enthalpy_BTU_lb > hg + tolerance)
                return PhaseState.SuperheatedSteam;
            else
                return PhaseState.TwoPhase;
        }
        
        #endregion
        
        #region Pressurizer-Specific Methods
        
        /// <summary>
        /// Calculate mass of water and steam in pressurizer given level.
        /// </summary>
        /// <param name="waterVolume_ft3">Water volume in ft³</param>
        /// <param name="steamVolume_ft3">Steam volume in ft³</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <param name="waterMass">Output: water mass in lb</param>
        /// <param name="steamMass">Output: steam mass in lb</param>
        public static void PressurizerMasses(
            float waterVolume_ft3, 
            float steamVolume_ft3,
            float pressure_psia,
            out float waterMass,
            out float steamMass)
        {
            float tSat = WaterProperties.SaturationTemperature(pressure_psia);
            float rhoF = WaterProperties.WaterDensity(tSat, pressure_psia);
            float rhoG = WaterProperties.SaturatedSteamDensity(pressure_psia);
            
            waterMass = waterVolume_ft3 * rhoF;
            steamMass = steamVolume_ft3 * rhoG;
        }
        
        /// <summary>
        /// Calculate pressurizer level from masses.
        /// </summary>
        /// <param name="waterMass_lb">Water mass in lb</param>
        /// <param name="steamMass_lb">Steam mass in lb</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <param name="totalVolume_ft3">Total pressurizer volume in ft³</param>
        /// <returns>Water level as fraction (0-1)</returns>
        public static float PressurizerLevel(
            float waterMass_lb,
            float steamMass_lb,
            float pressure_psia,
            float totalVolume_ft3)
        {
            float tSat = WaterProperties.SaturationTemperature(pressure_psia);
            float rhoF = WaterProperties.WaterDensity(tSat, pressure_psia);
            float rhoG = WaterProperties.SaturatedSteamDensity(pressure_psia);
            
            float waterVolume = waterMass_lb / rhoF;
            float steamVolume = steamMass_lb / rhoG;
            
            // Level is water volume fraction
            return waterVolume / (waterVolume + steamVolume);
        }
        
        /// <summary>
        /// Calculate steam space volume from level percentage.
        /// </summary>
        /// <param name="levelPercent">Level as percentage (0-100)</param>
        /// <param name="totalVolume_ft3">Total pressurizer volume in ft³</param>
        /// <returns>Steam volume in ft³</returns>
        public static float SteamVolumeFromLevel(float levelPercent, float totalVolume_ft3)
        {
            float level = Math.Max(0f, Math.Min(levelPercent, 100f)) / 100f;
            return totalVolume_ft3 * (1f - level);
        }
        
        /// <summary>
        /// Calculate water volume from level percentage.
        /// </summary>
        /// <param name="levelPercent">Level as percentage (0-100)</param>
        /// <param name="totalVolume_ft3">Total pressurizer volume in ft³</param>
        /// <returns>Water volume in ft³</returns>
        public static float WaterVolumeFromLevel(float levelPercent, float totalVolume_ft3)
        {
            float level = Math.Max(0f, Math.Min(levelPercent, 100f)) / 100f;
            return totalVolume_ft3 * level;
        }
        
        #endregion
        
        #region Phase Change Energetics
        
        /// <summary>
        /// Calculate energy required to completely evaporate liquid.
        /// </summary>
        /// <param name="mass_lb">Mass of liquid in lb</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Energy required in BTU</returns>
        public static float EvaporationEnergy(float mass_lb, float pressure_psia)
        {
            float hfg = WaterProperties.LatentHeat(pressure_psia);
            return mass_lb * hfg;
        }
        
        /// <summary>
        /// Calculate energy released by condensing steam.
        /// </summary>
        /// <param name="mass_lb">Mass of steam in lb</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Energy released in BTU</returns>
        public static float CondensationEnergy(float mass_lb, float pressure_psia)
        {
            return EvaporationEnergy(mass_lb, pressure_psia);
        }
        
        /// <summary>
        /// Calculate mass evaporated/condensed from energy input.
        /// </summary>
        /// <param name="energy_BTU">Energy in BTU (positive = evaporation)</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Mass change in lb (positive = evaporation)</returns>
        public static float MassFromPhaseChangeEnergy(float energy_BTU, float pressure_psia)
        {
            float hfg = WaterProperties.LatentHeat(pressure_psia);
            if (hfg < 1f) return 0f; // Near critical
            
            return energy_BTU / hfg;
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate steam thermodynamics calculations.
        /// </summary>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: Quality = 0 should give void = 0
            float alpha0 = VoidFraction(0f, 2250f);
            if (alpha0 > 0.001f) valid = false;
            
            // Test 2: Quality = 1 should give void = 1
            float alpha1 = VoidFraction(1f, 2250f);
            if (alpha1 < 0.999f) valid = false;
            
            // Test 3: Void fraction > quality always (for low pressures)
            float alpha50 = VoidFraction(0.5f, 100f);
            if (alpha50 <= 0.5f) valid = false;
            
            // Test 4: Two-phase enthalpy at x=0 equals hf
            float hf = WaterProperties.SaturatedLiquidEnthalpy(2250f);
            float h0 = TwoPhaseEnthalpy(0f, 2250f);
            if (Math.Abs(h0 - hf) > 1f) valid = false;
            
            // Test 5: Two-phase enthalpy at x=1 equals hg
            float hg = WaterProperties.SaturatedSteamEnthalpy(2250f);
            float h1 = TwoPhaseEnthalpy(1f, 2250f);
            if (Math.Abs(h1 - hg) > 1f) valid = false;
            
            // Test 6: Quality from void fraction inverse
            float x_test = 0.3f;
            float alpha_test = VoidFraction(x_test, 2250f);
            float x_back = QualityFromVoidFraction(alpha_test, 2250f);
            if (Math.Abs(x_back - x_test) > 0.01f) valid = false;
            
            return valid;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Enumeration of thermodynamic phase states.
    /// </summary>
    public enum PhaseState
    {
        SubcooledLiquid,
        TwoPhase,
        SuperheatedSteam
    }
}
