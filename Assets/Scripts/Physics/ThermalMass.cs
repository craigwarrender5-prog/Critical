// CRITICAL: Master the Atom - Phase 1 Core Physics Engine
// ThermalMass.cs - Heat Capacity and Thermal Mass Calculations
//
// Fundamental: Q = m × Cp × ΔT
// Units: BTU for energy, lb for mass, °F for temperature

using System;

namespace Critical.Physics
{
    /// <summary>
    /// Heat capacity calculations for metal structures and fluid masses.
    /// Used for thermal inertia modeling in transient analysis.
    /// </summary>
    public static class ThermalMass
    {
        #region Material Specific Heat Constants
        
        /// <summary>
        /// Specific heat of carbon steel in BTU/(lb·°F).
        /// Typical for pressure vessel and piping.
        /// </summary>
        public const float CP_STEEL = 0.12f;
        
        /// <summary>
        /// Specific heat of stainless steel in BTU/(lb·°F).
        /// Used for cladding and some internals.
        /// </summary>
        public const float CP_STAINLESS = 0.12f;
        
        /// <summary>
        /// Specific heat of Inconel in BTU/(lb·°F).
        /// Used for SG tubes and heater sheaths.
        /// </summary>
        public const float CP_INCONEL = 0.11f;
        
        /// <summary>
        /// Specific heat of zircaloy in BTU/(lb·°F).
        /// Fuel cladding material.
        /// </summary>
        public const float CP_ZIRCALOY = 0.07f;
        
        /// <summary>
        /// Specific heat of UO2 fuel in BTU/(lb·°F).
        /// </summary>
        public const float CP_UO2 = 0.06f;
        
        #endregion
        
        #region Heat Capacity Calculations
        
        /// <summary>
        /// Calculate total heat capacity of a metal mass.
        /// Heat capacity = mass × specific heat
        /// </summary>
        /// <param name="mass_lb">Mass in lb</param>
        /// <param name="material">Material type</param>
        /// <returns>Heat capacity in BTU/°F</returns>
        public static float MetalHeatCapacity(float mass_lb, MaterialType material)
        {
            float cp = GetSpecificHeat(material);
            return mass_lb * cp;
        }
        
        /// <summary>
        /// Calculate total heat capacity of a metal mass with explicit Cp.
        /// </summary>
        /// <param name="mass_lb">Mass in lb</param>
        /// <param name="specificHeat">Specific heat in BTU/(lb·°F)</param>
        /// <returns>Heat capacity in BTU/°F</returns>
        public static float MetalHeatCapacity(float mass_lb, float specificHeat)
        {
            return mass_lb * specificHeat;
        }
        
        /// <summary>
        /// Calculate heat capacity of water at given conditions.
        /// Uses temperature-dependent Cp from WaterProperties.
        /// </summary>
        /// <param name="mass_lb">Mass in lb</param>
        /// <param name="temp_F">Temperature in °F</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Heat capacity in BTU/°F</returns>
        public static float FluidHeatCapacity(float mass_lb, float temp_F, float pressure_psia)
        {
            float cp = WaterProperties.WaterSpecificHeat(temp_F, pressure_psia);
            return mass_lb * cp;
        }
        
        /// <summary>
        /// Calculate combined heat capacity of RCS metal and fluid.
        /// Used for plant thermal inertia calculations.
        /// </summary>
        /// <param name="metalMass_lb">RCS metal mass in lb</param>
        /// <param name="waterMass_lb">RCS water mass in lb</param>
        /// <param name="temp_F">Average temperature in °F</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Total heat capacity in BTU/°F</returns>
        public static float RCSHeatCapacity(
            float metalMass_lb, 
            float waterMass_lb, 
            float temp_F, 
            float pressure_psia)
        {
            float metalCap = MetalHeatCapacity(metalMass_lb, MaterialType.CarbonSteel);
            float waterCap = FluidHeatCapacity(waterMass_lb, temp_F, pressure_psia);
            return metalCap + waterCap;
        }
        
        /// <summary>
        /// Calculate pressurizer wall heat capacity.
        /// </summary>
        /// <returns>Heat capacity in BTU/°F</returns>
        public static float PressurizerWallHeatCapacity()
        {
            return MetalHeatCapacity(PlantConstants.PZR_WALL_MASS, MaterialType.CarbonSteel);
        }
        
        #endregion
        
        #region Temperature Change Calculations
        
        /// <summary>
        /// Calculate temperature change from heat addition.
        /// ΔT = Q / (m × Cp)
        /// </summary>
        /// <param name="heat_BTU">Heat added in BTU (positive = heating)</param>
        /// <param name="mass_lb">Mass in lb</param>
        /// <param name="specificHeat">Specific heat in BTU/(lb·°F)</param>
        /// <returns>Temperature change in °F</returns>
        public static float TemperatureChange(float heat_BTU, float mass_lb, float specificHeat)
        {
            if (mass_lb < 1f || specificHeat < 0.01f) return 0f;
            return heat_BTU / (mass_lb * specificHeat);
        }
        
        /// <summary>
        /// Calculate temperature change from heat addition to water.
        /// </summary>
        /// <param name="heat_BTU">Heat added in BTU</param>
        /// <param name="mass_lb">Water mass in lb</param>
        /// <param name="temp_F">Current temperature in °F</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Temperature change in °F</returns>
        public static float WaterTemperatureChange(
            float heat_BTU, 
            float mass_lb, 
            float temp_F, 
            float pressure_psia)
        {
            float cp = WaterProperties.WaterSpecificHeat(temp_F, pressure_psia);
            return TemperatureChange(heat_BTU, mass_lb, cp);
        }
        
        /// <summary>
        /// Calculate temperature change from heat addition to metal.
        /// </summary>
        /// <param name="heat_BTU">Heat added in BTU</param>
        /// <param name="mass_lb">Metal mass in lb</param>
        /// <param name="material">Material type</param>
        /// <returns>Temperature change in °F</returns>
        public static float MetalTemperatureChange(float heat_BTU, float mass_lb, MaterialType material)
        {
            float cp = GetSpecificHeat(material);
            return TemperatureChange(heat_BTU, mass_lb, cp);
        }
        
        #endregion
        
        #region Heat Required Calculations
        
        /// <summary>
        /// Calculate heat required for temperature change.
        /// Q = m × Cp × ΔT
        /// </summary>
        /// <param name="mass_lb">Mass in lb</param>
        /// <param name="specificHeat">Specific heat in BTU/(lb·°F)</param>
        /// <param name="deltaT">Temperature change in °F</param>
        /// <returns>Heat required in BTU</returns>
        public static float HeatRequired(float mass_lb, float specificHeat, float deltaT)
        {
            return mass_lb * specificHeat * deltaT;
        }
        
        /// <summary>
        /// Calculate heat required to raise water temperature.
        /// </summary>
        /// <param name="mass_lb">Water mass in lb</param>
        /// <param name="temp_F">Current temperature in °F</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <param name="deltaT">Desired temperature change in °F</param>
        /// <returns>Heat required in BTU</returns>
        public static float WaterHeatRequired(
            float mass_lb, 
            float temp_F, 
            float pressure_psia, 
            float deltaT)
        {
            float cp = WaterProperties.WaterSpecificHeat(temp_F, pressure_psia);
            return HeatRequired(mass_lb, cp, deltaT);
        }
        
        /// <summary>
        /// Calculate heat required to raise metal temperature.
        /// </summary>
        /// <param name="mass_lb">Metal mass in lb</param>
        /// <param name="material">Material type</param>
        /// <param name="deltaT">Desired temperature change in °F</param>
        /// <returns>Heat required in BTU</returns>
        public static float MetalHeatRequired(float mass_lb, MaterialType material, float deltaT)
        {
            float cp = GetSpecificHeat(material);
            return HeatRequired(mass_lb, cp, deltaT);
        }
        
        #endregion
        
        #region Heat Rate Calculations
        
        /// <summary>
        /// Calculate rate of temperature change from power input.
        /// dT/dt = P / (m × Cp)
        /// </summary>
        /// <param name="power_BTU_sec">Power in BTU/sec</param>
        /// <param name="mass_lb">Mass in lb</param>
        /// <param name="specificHeat">Specific heat in BTU/(lb·°F)</param>
        /// <returns>Temperature rate in °F/sec</returns>
        public static float TemperatureRate(float power_BTU_sec, float mass_lb, float specificHeat)
        {
            if (mass_lb < 1f || specificHeat < 0.01f) return 0f;
            return power_BTU_sec / (mass_lb * specificHeat);
        }
        
        /// <summary>
        /// Calculate power required for given temperature rate.
        /// P = m × Cp × dT/dt
        /// </summary>
        /// <param name="mass_lb">Mass in lb</param>
        /// <param name="specificHeat">Specific heat in BTU/(lb·°F)</param>
        /// <param name="tempRate_F_sec">Temperature rate in °F/sec</param>
        /// <returns>Power in BTU/sec</returns>
        public static float PowerRequired(float mass_lb, float specificHeat, float tempRate_F_sec)
        {
            return mass_lb * specificHeat * tempRate_F_sec;
        }
        
        #endregion
        
        #region Thermal Equilibrium
        
        /// <summary>
        /// Calculate equilibrium temperature when two masses are combined.
        /// T_eq = (m1*Cp1*T1 + m2*Cp2*T2) / (m1*Cp1 + m2*Cp2)
        /// </summary>
        public static float EquilibriumTemperature(
            float mass1_lb, float cp1, float temp1_F,
            float mass2_lb, float cp2, float temp2_F)
        {
            float totalCapacity = mass1_lb * cp1 + mass2_lb * cp2;
            if (totalCapacity < 0.1f) return (temp1_F + temp2_F) / 2f;
            
            return (mass1_lb * cp1 * temp1_F + mass2_lb * cp2 * temp2_F) / totalCapacity;
        }
        
        /// <summary>
        /// Calculate heat transfer between two masses approaching equilibrium.
        /// </summary>
        /// <param name="mass1_lb">Mass 1 in lb</param>
        /// <param name="cp1">Specific heat 1 in BTU/(lb·°F)</param>
        /// <param name="temp1_F">Temperature 1 in °F</param>
        /// <param name="mass2_lb">Mass 2 in lb</param>
        /// <param name="cp2">Specific heat 2 in BTU/(lb·°F)</param>
        /// <param name="temp2_F">Temperature 2 in °F</param>
        /// <returns>Heat transfer from mass 1 to mass 2 in BTU</returns>
        public static float HeatTransferToEquilibrium(
            float mass1_lb, float cp1, float temp1_F,
            float mass2_lb, float cp2, float temp2_F)
        {
            float tEq = EquilibriumTemperature(mass1_lb, cp1, temp1_F, mass2_lb, cp2, temp2_F);
            
            // Heat lost by mass 1 (positive if T1 > T_eq)
            return mass1_lb * cp1 * (temp1_F - tEq);
        }
        
        #endregion
        
        #region Time Constants
        
        /// <summary>
        /// Calculate thermal time constant for a lumped mass.
        /// τ = (m × Cp) / (h × A)
        /// </summary>
        /// <param name="mass_lb">Mass in lb</param>
        /// <param name="specificHeat">Specific heat in BTU/(lb·°F)</param>
        /// <param name="htc">Heat transfer coefficient in BTU/(hr·ft²·°F)</param>
        /// <param name="area_ft2">Surface area in ft²</param>
        /// <returns>Time constant in hours</returns>
        public static float ThermalTimeConstant(
            float mass_lb, 
            float specificHeat, 
            float htc, 
            float area_ft2)
        {
            if (htc < 0.1f || area_ft2 < 0.1f) return float.MaxValue;
            return (mass_lb * specificHeat) / (htc * area_ft2);
        }
        
        /// <summary>
        /// Calculate temperature response with first-order lag.
        /// T(t) = T_target - (T_target - T_0) × exp(-t/τ)
        /// </summary>
        /// <param name="currentTemp_F">Current temperature in °F</param>
        /// <param name="targetTemp_F">Target temperature in °F</param>
        /// <param name="timeConstant_sec">Time constant in seconds</param>
        /// <param name="dt_sec">Time step in seconds</param>
        /// <returns>New temperature in °F</returns>
        public static float FirstOrderResponse(
            float currentTemp_F, 
            float targetTemp_F, 
            float timeConstant_sec, 
            float dt_sec)
        {
            if (timeConstant_sec < 0.1f) return targetTemp_F;
            
            float alpha = 1f - (float)Math.Exp(-dt_sec / timeConstant_sec);
            return currentTemp_F + alpha * (targetTemp_F - currentTemp_F);
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Get specific heat for a material type.
        /// </summary>
        public static float GetSpecificHeat(MaterialType material)
        {
            switch (material)
            {
                case MaterialType.CarbonSteel:
                    return CP_STEEL;
                case MaterialType.StainlessSteel:
                    return CP_STAINLESS;
                case MaterialType.Inconel:
                    return CP_INCONEL;
                case MaterialType.Zircaloy:
                    return CP_ZIRCALOY;
                case MaterialType.UO2:
                    return CP_UO2;
                default:
                    return CP_STEEL;
            }
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate thermal mass calculations.
        /// </summary>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: Heat required calculation
            // 1 lb of water, Cp = 1, ΔT = 1°F should require 1 BTU
            float q1 = HeatRequired(1f, 1f, 1f);
            if (Math.Abs(q1 - 1f) > 0.001f) valid = false;
            
            // Test 2: Temperature change calculation
            // 1 BTU into 1 lb of water (Cp=1) should raise by 1°F
            float dT1 = TemperatureChange(1f, 1f, 1f);
            if (Math.Abs(dT1 - 1f) > 0.001f) valid = false;
            
            // Test 3: Equilibrium temperature
            // Equal masses at 100°F and 200°F should equilibrate at 150°F
            float tEq = EquilibriumTemperature(1f, 1f, 100f, 1f, 1f, 200f);
            if (Math.Abs(tEq - 150f) > 0.1f) valid = false;
            
            // Test 4: Pressurizer wall heat capacity
            // 200,000 lb × 0.12 BTU/lb·°F = 24,000 BTU/°F
            float pzrCap = PressurizerWallHeatCapacity();
            if (Math.Abs(pzrCap - 24000f) > 100f) valid = false;
            
            // Test 5: First order response at t=τ
            // Should be 63.2% of the way to target
            float t0 = 100f;
            float tTarget = 200f;
            float tau = 10f;
            float tNew = FirstOrderResponse(t0, tTarget, tau, tau);
            float expected = t0 + 0.632f * (tTarget - t0);
            if (Math.Abs(tNew - expected) > 1f) valid = false;
            
            return valid;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Enumeration of material types for thermal calculations.
    /// </summary>
    public enum MaterialType
    {
        CarbonSteel,
        StainlessSteel,
        Inconel,
        Zircaloy,
        UO2
    }
}
