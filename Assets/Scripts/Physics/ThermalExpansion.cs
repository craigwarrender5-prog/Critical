// CRITICAL: Master the Atom - Phase 1 Core Physics Engine
// ThermalExpansion.cs - Thermal Expansion and Volume Change Calculations
//
// PWR RCS is a closed system - temperature changes cause volume changes
// which in turn cause pressure changes (handled by CoupledThermo)
// Units: ft³ for volume, °F for temperature, psia for pressure

using System;

namespace Critical.Physics
{
    /// <summary>
    /// Thermal expansion calculations for RCS and pressurizer.
    /// Critical for understanding insurge/outsurge behavior.
    /// Note: For realistic pressure response, use CoupledThermo (Gap #1).
    /// </summary>
    public static class ThermalExpansion
    {
        #region Expansion Coefficients
        
        /// <summary>
        /// Calculate volumetric thermal expansion coefficient for water.
        /// β = (1/V) × (∂V/∂T)_P
        /// Units: 1/°F
        /// </summary>
        /// <param name="temp_F">Temperature in °F</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Expansion coefficient in 1/°F</returns>
        public static float ExpansionCoefficient(float temp_F, float pressure_psia)
        {
            // Expansion coefficient increases significantly with temperature
            // At 100°F: β ≈ 1e-4 /°F
            // At 400°F: β ≈ 3e-4 /°F
            // At 600°F: β ≈ 6e-4 /°F
            // Near saturation: β increases dramatically
            
            float tSat = WaterProperties.SaturationTemperature(pressure_psia);
            float subcooling = tSat - temp_F;
            
            // Base coefficient (polynomial fit)
            float beta = 5e-5f + 7e-7f * temp_F + 5e-10f * temp_F * temp_F;
            
            // Enhancement near saturation - only when very close to Tsat
            // β increases dramatically only within ~30°F of saturation
            if (subcooling < 30f && subcooling > 0f)
            {
                float factor = 1f + 1.5f * (1f - subcooling / 30f);
                beta *= factor;
            }
            
            return beta;
        }
        
        /// <summary>
        /// Calculate isothermal compressibility of water.
        /// κ = -(1/V) × (∂V/∂P)_T
        /// Units: 1/psi
        /// </summary>
        /// <param name="temp_F">Temperature in °F</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Compressibility in 1/psi</returns>
        public static float Compressibility(float temp_F, float pressure_psia)
        {
            // Water compressibility is small but important for pressure response
            // At 70°F: κ ≈ 3.2e-6 /psi
            // At 400°F: κ ≈ 4.5e-6 /psi
            // At 600°F: κ ≈ 8e-6 /psi
            // Increases dramatically near saturation
            
            float tSat = WaterProperties.SaturationTemperature(pressure_psia);
            float subcooling = tSat - temp_F;
            
            // Base compressibility
            float kappa = 3e-6f + 8e-9f * temp_F + 2e-11f * temp_F * temp_F;
            
            // Enhancement near saturation - only very close to Tsat
            if (subcooling < 20f && subcooling > 0f)
            {
                float factor = 1f + 3f * (1f - subcooling / 20f);
                kappa *= factor;
            }
            
            return kappa;
        }
        
        #endregion
        
        #region Volume Change Calculations
        
        /// <summary>
        /// Calculate volume change due to temperature change at constant pressure.
        /// ΔV = V × β × ΔT
        /// </summary>
        /// <param name="volume_ft3">Initial volume in ft³</param>
        /// <param name="temp_F">Current temperature in °F</param>
        /// <param name="deltaT_F">Temperature change in °F</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Volume change in ft³</returns>
        public static float VolumeChangeFromTemp(
            float volume_ft3, 
            float temp_F, 
            float deltaT_F, 
            float pressure_psia)
        {
            float beta = ExpansionCoefficient(temp_F, pressure_psia);
            return volume_ft3 * beta * deltaT_F;
        }
        
        /// <summary>
        /// Calculate volume change due to pressure change at constant temperature.
        /// ΔV = -V × κ × ΔP
        /// </summary>
        /// <param name="volume_ft3">Initial volume in ft³</param>
        /// <param name="temp_F">Temperature in °F</param>
        /// <param name="deltaP_psi">Pressure change in psi</param>
        /// <param name="pressure_psia">Current pressure in psia</param>
        /// <returns>Volume change in ft³ (negative for pressure increase)</returns>
        public static float VolumeChangeFromPressure(
            float volume_ft3, 
            float temp_F, 
            float deltaP_psi, 
            float pressure_psia)
        {
            float kappa = Compressibility(temp_F, pressure_psia);
            return -volume_ft3 * kappa * deltaP_psi;
        }
        
        /// <summary>
        /// Calculate new volume after temperature and pressure changes.
        /// V_new = V × [1 + β×ΔT - κ×ΔP]
        /// </summary>
        /// <param name="volume_ft3">Initial volume in ft³</param>
        /// <param name="temp_F">Initial temperature in °F</param>
        /// <param name="deltaT_F">Temperature change in °F</param>
        /// <param name="deltaP_psi">Pressure change in psi</param>
        /// <param name="pressure_psia">Initial pressure in psia</param>
        /// <returns>New volume in ft³</returns>
        public static float NewVolume(
            float volume_ft3,
            float temp_F,
            float deltaT_F,
            float deltaP_psi,
            float pressure_psia)
        {
            float beta = ExpansionCoefficient(temp_F, pressure_psia);
            float kappa = Compressibility(temp_F, pressure_psia);
            
            float factor = 1f + beta * deltaT_F - kappa * deltaP_psi;
            return volume_ft3 * factor;
        }
        
        #endregion
        
        #region Surge Volume Calculations
        
        /// <summary>
        /// Calculate uncoupled surge volume for RCS temperature change.
        /// This is the "free expansion" if pressure could remain constant.
        /// In reality, the closed RCS constrains this (see CoupledThermo).
        /// </summary>
        /// <param name="rcsVolume_ft3">RCS water volume in ft³</param>
        /// <param name="deltaT_F">Temperature change in °F</param>
        /// <param name="temp_F">Average temperature in °F</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Surge volume in ft³ (positive = insurge)</returns>
        public static float UncoupledSurgeVolume(
            float rcsVolume_ft3,
            float deltaT_F,
            float temp_F,
            float pressure_psia)
        {
            return VolumeChangeFromTemp(rcsVolume_ft3, temp_F, deltaT_F, pressure_psia);
        }
        
        /// <summary>
        /// Calculate surge volume accounting for pressure feedback.
        /// This is a simplified coupled calculation - for full accuracy use CoupledThermo.
        /// ΔV_surge = ΔV_thermal × (1 / (1 + V×β/(V_pzr×κ)))
        /// </summary>
        /// <param name="rcsVolume_ft3">RCS water volume in ft³</param>
        /// <param name="pzrSteamVolume_ft3">Pressurizer steam volume in ft³</param>
        /// <param name="deltaT_F">Temperature change in °F</param>
        /// <param name="temp_F">Average temperature in °F</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Effective surge volume in ft³ (less than uncoupled)</returns>
        public static float CoupledSurgeVolume(
            float rcsVolume_ft3,
            float pzrSteamVolume_ft3,
            float deltaT_F,
            float temp_F,
            float pressure_psia)
        {
            float beta = ExpansionCoefficient(temp_F, pressure_psia);
            float kappa = Compressibility(temp_F, pressure_psia);
            
            // Uncoupled expansion
            float deltaV_uncoupled = rcsVolume_ft3 * beta * deltaT_F;
            
            // Steam compressibility provides "cushion"
            // Simplified model: steam acts as gas with bulk modulus
            float steamDensity = WaterProperties.SaturatedSteamDensity(pressure_psia);
            float steamCompressibility = 1f / pressure_psia; // Ideal gas approximation
            
            // Coupling factor (0 to 1, where 1 = fully uncoupled)
            float coupling = 1f / (1f + rcsVolume_ft3 * beta / (pzrSteamVolume_ft3 * steamCompressibility + rcsVolume_ft3 * kappa));
            
            return deltaV_uncoupled * coupling;
        }
        
        /// <summary>
        /// Calculate RCS expansion rate from power input.
        /// </summary>
        /// <param name="power_BTU_sec">Net heat input to RCS in BTU/sec</param>
        /// <param name="rcsVolume_ft3">RCS water volume in ft³</param>
        /// <param name="rcsMass_lb">RCS water mass in lb</param>
        /// <param name="temp_F">Average temperature in °F</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Volume expansion rate in ft³/sec</returns>
        public static float ExpansionRate(
            float power_BTU_sec,
            float rcsVolume_ft3,
            float rcsMass_lb,
            float temp_F,
            float pressure_psia)
        {
            // dV/dt = V × β × dT/dt
            // dT/dt = P / (m × Cp)
            
            float cp = WaterProperties.WaterSpecificHeat(temp_F, pressure_psia);
            float dTdt = power_BTU_sec / (rcsMass_lb * cp);
            
            float beta = ExpansionCoefficient(temp_F, pressure_psia);
            return rcsVolume_ft3 * beta * dTdt;
        }
        
        #endregion
        
        #region Pressure Response
        
        /// <summary>
        /// Estimate pressure change from temperature change at constant volume.
        /// This is a simplified calculation - use CoupledThermo for accuracy.
        /// Includes system damping factor for realistic PWR response.
        /// </summary>
        /// <param name="deltaT_F">Temperature change in °F</param>
        /// <param name="temp_F">Current temperature in °F</param>
        /// <param name="pressure_psia">Current pressure in psia</param>
        /// <returns>Estimated pressure change in psi</returns>
        public static float PressureChangeFromTemp(
            float deltaT_F, 
            float temp_F, 
            float pressure_psia)
        {
            // Use PressureCoefficient which includes system damping
            return PressureCoefficient(temp_F, pressure_psia) * deltaT_F;
        }
        
        /// <summary>
        /// Calculate pressure coefficient (dP/dT at constant volume).
        /// For PWR conditions: typically 5-10 psi/°F including system effects.
        /// Note: Pure β/κ gives ~40 psi/°F, but pressurizer steam cushion
        /// reduces this significantly in an actual PWR system.
        /// </summary>
        /// <param name="temp_F">Temperature in °F</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Pressure coefficient in psi/°F</returns>
        public static float PressureCoefficient(float temp_F, float pressure_psia)
        {
            float beta = ExpansionCoefficient(temp_F, pressure_psia);
            float kappa = Compressibility(temp_F, pressure_psia);
            
            if (kappa < 1e-9f) return 0f;
            
            // Apply system damping factor to account for pressurizer effect
            // In a real PWR, the steam cushion reduces effective dP/dT
            // This gives realistic 5-10 psi/°F instead of pure β/κ ≈ 40 psi/°F
            const float SYSTEM_DAMPING = 0.18f;
            
            return (beta / kappa) * SYSTEM_DAMPING;
        }
        
        /// <summary>
        /// Calculate temperature change required to produce pressure change.
        /// ΔT = (κ/β) × ΔP
        /// </summary>
        /// <param name="deltaP_psi">Desired pressure change in psi</param>
        /// <param name="temp_F">Current temperature in °F</param>
        /// <param name="pressure_psia">Current pressure in psia</param>
        /// <returns>Required temperature change in °F</returns>
        public static float TempChangeFromPressure(
            float deltaP_psi,
            float temp_F,
            float pressure_psia)
        {
            float beta = ExpansionCoefficient(temp_F, pressure_psia);
            float kappa = Compressibility(temp_F, pressure_psia);
            
            if (beta < 1e-9f) return 0f;
            
            return (kappa / beta) * deltaP_psi;
        }
        
        #endregion
        
        #region Density-Based Methods
        
        /// <summary>
        /// Calculate mass change required to maintain constant pressure
        /// when temperature changes.
        /// </summary>
        /// <param name="volume_ft3">Fixed volume in ft³</param>
        /// <param name="temp1_F">Initial temperature in °F</param>
        /// <param name="temp2_F">Final temperature in °F</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Mass change in lb (negative = mass must leave)</returns>
        public static float MassChangeForConstantPressure(
            float volume_ft3,
            float temp1_F,
            float temp2_F,
            float pressure_psia)
        {
            float rho1 = WaterProperties.WaterDensity(temp1_F, pressure_psia);
            float rho2 = WaterProperties.WaterDensity(temp2_F, pressure_psia);
            
            float mass1 = volume_ft3 * rho1;
            float mass2 = volume_ft3 * rho2;
            
            return mass2 - mass1;
        }
        
        /// <summary>
        /// Calculate volume occupied by mass at new temperature.
        /// </summary>
        /// <param name="mass_lb">Fixed mass in lb</param>
        /// <param name="temp_F">New temperature in °F</param>
        /// <param name="pressure_psia">Pressure in psia</param>
        /// <returns>Volume in ft³</returns>
        public static float VolumeAtTemperature(
            float mass_lb,
            float temp_F,
            float pressure_psia)
        {
            float rho = WaterProperties.WaterDensity(temp_F, pressure_psia);
            return mass_lb / rho;
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate thermal expansion calculations against expected values.
        /// </summary>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: Expansion coefficient at operating conditions
            // At 588°F, 2250 psia: β should be around 4-6e-4 /°F
            float beta1 = ExpansionCoefficient(588f, 2250f);
            if (beta1 < 3e-4f || beta1 > 8e-4f) valid = false;
            
            // Test 2: Compressibility at operating conditions
            // At 588°F, 2250 psia: κ should be around 5-10e-6 /psi
            float kappa1 = Compressibility(588f, 2250f);
            if (kappa1 < 3e-6f || kappa1 > 15e-6f) valid = false;
            
            // Test 3: Pressure coefficient should be 5-10 psi/°F
            float dPdT = PressureCoefficient(588f, 2250f);
            if (dPdT < 3f || dPdT > 15f) valid = false;
            
            // Test 4: 10°F rise should cause 60-100 psi increase
            float deltaP = PressureChangeFromTemp(10f, 588f, 2250f);
            if (deltaP < 30f || deltaP > 150f) valid = false;
            
            // Test 5: Surge volume should be positive for temperature increase
            float surge = UncoupledSurgeVolume(11500f, 10f, 588f, 2250f);
            if (surge <= 0f) valid = false;
            
            // Test 6: Coupled surge should be less than uncoupled
            float coupledSurge = CoupledSurgeVolume(11500f, 720f, 10f, 588f, 2250f);
            if (coupledSurge >= surge) valid = false;
            
            return valid;
        }
        
        #endregion
    }
}
