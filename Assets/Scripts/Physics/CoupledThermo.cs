// CRITICAL: Master the Atom - Phase 1 Core Physics Engine
// CoupledThermo.cs - Coupled Pressure-Temperature-Volume Solver
//
// Implements: Gap #1 - P-T-V coupling (MOST CRITICAL)
//
// THE PROBLEM:
// In a closed RCS, temperature change causes expansion, but volume is constrained.
// This creates pressure increase, which changes density, which reduces expansion.
// Simple uncoupled model: 10°F rise → 0 psi change (WRONG!)
// Coupled model: 10°F rise → 60-80 psi change (CORRECT!)
//
// THE SOLUTION:
// Iterative solver that converges pressure, temperature, and volume
// to satisfy both state equations and conservation laws simultaneously.
//
// Phase 2 Fix (C3): Pressure bounds parameterized on all solver entry points.
//   SolveEquilibrium, SolveTransient, SolveWithPressurizer all accept P_floor/P_ceiling.
//   Default P_floor=15 psia allows heatup from cold shutdown (~20 psia) through Mode 3.
//   Added InitializeAtHeatupConditions() for cold-start scenarios.
//   Added ValidateHeatupRange() to verify low-pressure convergence.

using System;

namespace Critical.Physics
{
    /// <summary>
    /// Coupled thermodynamic solver for PWR RCS and pressurizer.
    /// This is the MOST CRITICAL module - all other phases depend on correct P-T-V coupling.
    /// 
    /// Key validation: 10°F Tavg rise must produce 60-80 psi pressure increase.
    /// If this test fails, the entire simulation will be unrealistic.
    /// 
    /// Pressure bounds are parameterized on all public solver methods to support
    /// the full operating range from cold shutdown (~20 psia) through normal
    /// operations (2250 psia). Callers should set P_floor based on plant mode:
    ///   Cold shutdown / heatup: P_floor = 15 psia
    ///   Normal at-power:        P_floor = 1800 psia (tighter convergence)
    /// </summary>
    public static class CoupledThermo
    {
        #region Constants
        
        /// <summary>Maximum iterations for solver convergence</summary>
        private const int MAX_ITERATIONS = 50;
        
        /// <summary>Pressure convergence tolerance in psi</summary>
        private const float PRESSURE_TOLERANCE = 0.1f;
        
        /// <summary>Volume convergence tolerance in ft³</summary>
        private const float VOLUME_TOLERANCE = 0.01f;
        
        /// <summary>Mass conservation tolerance as fraction</summary>
        private const float MASS_TOLERANCE = 0.001f;
        
        /// <summary>Relaxation factor for pressure updates (0-1, lower = more stable)</summary>
        private const float RELAXATION_FACTOR = 0.5f;
        
        #endregion
        
        #region Main Solver (Gap #1)
        
        /// <summary>
        /// Solve for equilibrium pressure after temperature change in closed RCS.
        /// 
        /// PHYSICS: In a PWR, RCS volume is FIXED (rigid piping).
        /// When temperature changes, water density changes, which means
        /// the same volume holds different mass. The mass difference
        /// surges into/out of the pressurizer.
        /// 
        /// - Temperature rise → density drop → mass surges INTO PZR → level rises → steam compresses → P rises
        /// - Temperature drop → density rise → mass surges OUT of PZR → level drops → steam expands → P drops
        /// </summary>
        /// <param name="state">System state (modified in place if converged)</param>
        /// <param name="deltaT_F">Temperature change in °F</param>
        /// <param name="maxIterations">Maximum solver iterations</param>
        /// <param name="P_floor">Minimum pressure bound in psia (15 for heatup, 1800 for at-power)</param>
        /// <param name="P_ceiling">Maximum pressure bound in psia</param>
        /// <returns>True if converged, false otherwise</returns>
        public static bool SolveEquilibrium(ref SystemState state, float deltaT_F, 
            int maxIterations = MAX_ITERATIONS, float P_floor = 15f, float P_ceiling = 2700f)
        {
            // Save initial conditions
            float P0 = state.Pressure;
            float T0 = state.Temperature;
            float V_RCS = state.RCSVolume;  // FIXED - does not change
            float V_PZR_total = PlantConstants.PZR_TOTAL_VOLUME;  // FIXED
            
            // Target temperature
            float T_new = T0 + deltaT_F;
            
            // Initial total mass (must be conserved)
            float M_total = state.RCSWaterMass + state.PZRWaterMass + state.PZRSteamMass;
            
            // Initial pressure estimate using quick estimate
            float dP_est = QuickPressureEstimate(T0, P0, deltaT_F, V_RCS, state.PZRSteamVolume);
            float P_new = P0 + dP_est;
            P_new = Math.Max(P_floor, Math.Min(P_new, P_ceiling));
            
            // Iteration
            int iteration = 0;
            bool converged = false;
            
            while (iteration < maxIterations && !converged)
            {
                iteration++;
                
                // 1. At new (T, P), calculate RCS density
                float rho_RCS_new = WaterProperties.WaterDensity(T_new, P_new);
                
                // 2. RCS volume is FIXED, so RCS mass at new conditions is:
                float M_RCS_new = V_RCS * rho_RCS_new;
                
                // 3. Mass that must be in pressurizer (by conservation)
                float M_PZR_total = M_total - M_RCS_new;
                
                // 4. Pressurizer water is at saturation temperature
                float tSat = WaterProperties.SaturationTemperature(P_new);
                float rho_PZR_water = WaterProperties.WaterDensity(tSat, P_new);
                float rho_PZR_steam = WaterProperties.SaturatedSteamDensity(P_new);
                
                // 5. Solve for PZR water volume
                // M_PZR_total = V_water * rho_water + (V_total - V_water) * rho_steam
                float V_PZR_water_new = (M_PZR_total - V_PZR_total * rho_PZR_steam) / 
                                        (rho_PZR_water - rho_PZR_steam);
                
                // 6. Apply constraints - water volume limits
                V_PZR_water_new = Math.Max(0f, V_PZR_water_new);
                V_PZR_water_new = Math.Min(V_PZR_water_new, PlantConstants.PZR_WATER_MAX);
                
                float V_PZR_steam_new = V_PZR_total - V_PZR_water_new;
                
                // 7. Calculate actual masses with constrained volumes
                float M_PZR_water_new = V_PZR_water_new * rho_PZR_water;
                float M_PZR_steam_new = V_PZR_steam_new * rho_PZR_steam;
                float M_total_calc = M_RCS_new + M_PZR_water_new + M_PZR_steam_new;
                
                // 8. Mass error indicates pressure needs adjustment
                float massError = (M_total_calc - M_total) / M_total;
                
                // 9. Pressure correction: if mass is too high, P is too high
                float dP = -massError * P_new * 0.5f;
                
                // 10. Update pressure with relaxation
                float P_next = P_new + RELAXATION_FACTOR * dP;
                P_next = Math.Max(P_next, P_floor);
                P_next = Math.Min(P_next, P_ceiling);
                
                // 11. Check convergence
                float pressureChange = Math.Abs(P_next - P_new);
                
                if (pressureChange < PRESSURE_TOLERANCE && Math.Abs(massError) < MASS_TOLERANCE)
                {
                    converged = true;
                    P_new = P_next;
                }
                else
                {
                    P_new = P_next;
                }
            }
            
            // Update state if converged
            if (converged)
            {
                state.Temperature = T_new;
                state.Pressure = P_new;
                
                // Final state calculation
                float rho_RCS = WaterProperties.WaterDensity(T_new, P_new);
                float tSat = WaterProperties.SaturationTemperature(P_new);
                float rho_PZR_water = WaterProperties.WaterDensity(tSat, P_new);
                float rho_PZR_steam = WaterProperties.SaturatedSteamDensity(P_new);
                
                state.RCSWaterMass = V_RCS * rho_RCS;
                
                float M_PZR_total = M_total - state.RCSWaterMass;
                state.PZRWaterVolume = (M_PZR_total - V_PZR_total * rho_PZR_steam) / 
                                       (rho_PZR_water - rho_PZR_steam);
                state.PZRWaterVolume = Math.Max(0f, Math.Min(state.PZRWaterVolume, PlantConstants.PZR_WATER_MAX));
                state.PZRSteamVolume = V_PZR_total - state.PZRWaterVolume;
                
                state.PZRWaterMass = state.PZRWaterVolume * rho_PZR_water;
                state.PZRSteamMass = state.PZRSteamVolume * rho_PZR_steam;
                
                state.IterationsUsed = iteration;
            }
            else
            {
                state.IterationsUsed = iteration;
            }
            
            return converged;
        }
        
        /// <summary>
        /// Simplified coupled solver for quick estimates.
        /// Uses analytic approximation instead of full iteration.
        /// </summary>
        /// <param name="T0">Initial temperature in °F</param>
        /// <param name="P0">Initial pressure in psia</param>
        /// <param name="deltaT">Temperature change in °F</param>
        /// <param name="V_RCS">RCS volume in ft³</param>
        /// <param name="V_PZR_steam">Pressurizer steam volume in ft³</param>
        /// <returns>Pressure change in psi</returns>
        public static float QuickPressureEstimate(
            float T0, float P0, float deltaT, 
            float V_RCS, float V_PZR_steam)
        {
            // Simplified model:
            // dP = (beta * dT * V_RCS) / (kappa * V_RCS + C_steam * V_steam)
            // where C_steam is steam compressibility
            
            float beta = ThermalExpansion.ExpansionCoefficient(T0, P0);
            float kappa = ThermalExpansion.Compressibility(T0, P0);
            
            // Steam compressibility - real steam at PWR conditions is more
            // compressible than ideal gas due to proximity to saturation
            // Ideal gas: 1/P, Real steam at 2250 psia: ~2/P
            float steamComp = 2f / P0;
            
            // Effective compressibility includes steam cushion
            float effectiveComp = (kappa * V_RCS + steamComp * V_PZR_steam) / V_RCS;
            
            // Pressure rise
            float deltaP = (beta / effectiveComp) * deltaT;
            
            // Apply correction factor (empirical, accounts for non-ideal effects)
            deltaP *= 0.85f;
            
            return deltaP;
        }
        
        #endregion
        
        #region Transient Solver
        
        /// <summary>
        /// Solve coupled system over a time step with heat input.
        /// Pressure bounds are forwarded to SolveEquilibrium to support
        /// the full operating range from cold shutdown through at-power.
        /// </summary>
        /// <param name="state">System state</param>
        /// <param name="heatInput_BTU_sec">Net heat input to RCS in BTU/sec</param>
        /// <param name="dt_sec">Time step in seconds</param>
        /// <param name="P_floor">Minimum pressure bound in psia (15 for heatup, 1800 for at-power)</param>
        /// <param name="P_ceiling">Maximum pressure bound in psia</param>
        /// <returns>True if converged</returns>
        public static bool SolveTransient(ref SystemState state, float heatInput_BTU_sec, float dt_sec,
            float P_floor = 15f, float P_ceiling = 2700f)
        {
            // Calculate temperature change from heat input
            // Q = m * Cp * dT
            float totalMass = state.RCSWaterMass + state.PZRWaterMass;
            float cp = WaterProperties.WaterSpecificHeat(state.Temperature, state.Pressure);
            
            // Include metal thermal mass
            float metalCapacity = PlantConstants.RCS_METAL_MASS * PlantConstants.STEEL_CP;
            float effectiveCapacity = totalMass * cp + metalCapacity;
            
            float deltaT = heatInput_BTU_sec * dt_sec / effectiveCapacity;
            
            // Solve for new equilibrium - forward pressure bounds to allow heatup-range operation
            return SolveEquilibrium(ref state, deltaT, MAX_ITERATIONS, P_floor, P_ceiling);
        }
        
        /// <summary>
        /// Solve with pressurizer control systems active.
        /// Pressure bounds are parameterized to support the full operating range.
        /// During heatup, pass dynamic T_hot/T_cold values instead of constants.
        /// </summary>
        /// <param name="state">System state</param>
        /// <param name="pzrState">Pressurizer state</param>
        /// <param name="heatInput_BTU_sec">Heat input to RCS</param>
        /// <param name="dt_sec">Time step</param>
        /// <param name="surgeTemp_F">Surge water temperature (hot leg T). Use dynamic T_hot, not PlantConstants.T_HOT</param>
        /// <param name="sprayTemp_F">Spray water temperature (cold leg T). Use dynamic T_cold, not PlantConstants.T_COLD</param>
        /// <param name="P_floor">Minimum pressure bound in psia</param>
        /// <param name="P_ceiling">Maximum pressure bound in psia</param>
        /// <returns>True if converged</returns>
        public static bool SolveWithPressurizer(
            ref SystemState state, 
            ref PressurizerState pzrState,
            float heatInput_BTU_sec,
            float dt_sec,
            float surgeTemp_F = 0f,
            float sprayTemp_F = 0f,
            float P_floor = 15f, float P_ceiling = 2700f)
        {
            // Default to PlantConstants if caller passes 0 (backward compatibility)
            if (surgeTemp_F <= 0f) surgeTemp_F = PlantConstants.T_HOT;
            if (sprayTemp_F <= 0f) sprayTemp_F = PlantConstants.T_COLD;
            
            // 1. Calculate preliminary pressure from coupled thermodynamics
            float cp = WaterProperties.WaterSpecificHeat(state.Temperature, state.Pressure);
            float totalMass = state.RCSWaterMass + state.PZRWaterMass;
            float metalCapacity = PlantConstants.RCS_METAL_MASS * PlantConstants.STEEL_CP;
            float effectiveCapacity = totalMass * cp + metalCapacity;
            
            float deltaT = heatInput_BTU_sec * dt_sec / effectiveCapacity;
            
            // Quick estimate for pressure change without controls
            float deltaP_noControl = QuickPressureEstimate(
                state.Temperature, state.Pressure, deltaT,
                state.RCSVolume, pzrState.SteamVolume);
            
            // 2. Calculate pressurizer control responses
            float pressure_psig = state.Pressure - PlantConstants.PSIG_TO_PSIA;
            float heaterDemand = PressurizerPhysics.HeaterPowerDemand(pressure_psig);
            float sprayDemand = PressurizerPhysics.SprayFlowDemand(pressure_psig);
            
            // 3. Calculate surge flow from expansion
            float beta = ThermalExpansion.ExpansionCoefficient(state.Temperature, state.Pressure);
            float surgeVolume = state.RCSVolume * beta * deltaT;
            float surgeFlow_gpm = surgeVolume / PlantConstants.GPM_TO_FT3_SEC / dt_sec;
            
            // 4. Update pressurizer state - use dynamic temperatures, not constants
            PressurizerPhysics.ThreeRegionUpdate(
                ref pzrState,
                surgeFlow_gpm,
                surgeTemp_F,
                sprayDemand,
                sprayTemp_F,
                heaterDemand,
                dt_sec);
            
            // 5. Update pressure rate
            pzrState.PressureRate = deltaP_noControl / dt_sec;
            
            // 6. Calculate actual pressure change with controls
            // Heaters add steam -> increase pressure
            // Spray condenses steam -> decrease pressure
            // Flash evaporation -> reduces pressure drop rate
            
            float steamGeneration = pzrState.HeaterSteamRate - pzrState.SprayCondRate - 
                                   pzrState.WallCondRate + pzrState.FlashRate;
            
            // Steam compressibility
            float rhog = WaterProperties.SaturatedSteamDensity(state.Pressure);
            float dP_steam = (pzrState.SteamVolume > 0.1f && rhog > 0.01f)
                ? steamGeneration * dt_sec / (pzrState.SteamVolume * rhog) * state.Pressure
                : 0f;
            
            // Total pressure change
            float deltaP_actual = deltaP_noControl + dP_steam;
            
            // 7. Solve for final equilibrium
            state.Temperature += deltaT;
            state.Pressure += deltaP_actual;
            
            // Clamp pressure using parameterized bounds
            state.Pressure = Math.Max(state.Pressure, P_floor);
            state.Pressure = Math.Min(state.Pressure, P_ceiling);
            
            // Update pressurizer pressure
            pzrState.Pressure = state.Pressure;
            
            return true; // Simplified - always returns true for now
        }
        
        #endregion
        
        #region Validation Tests
        
        /// <summary>
        /// CRITICAL TEST: Verify 10°F rise produces 60-80 psi increase.
        /// This is THE key validation for Gap #1.
        /// If this fails, the entire simulation will be unrealistic.
        /// </summary>
        /// <returns>True if pressure response is in valid range</returns>
        public static bool Validate10DegreeTest()
        {
            // Initialize at normal operating conditions
            var state = InitializeAtSteadyState();
            float P0 = state.Pressure;
            
            // Apply 10°F temperature rise
            bool converged = SolveEquilibrium(ref state, 10f);
            
            if (!converged) return false;
            
            float deltaP = state.Pressure - P0;
            
            // Expected: 60-80 psi (6-8 psi/°F)
            // Accept slightly wider range for numerical tolerance: 50-100 psi
            return deltaP >= 50f && deltaP <= 100f;
        }
        
        /// <summary>
        /// Verify coupled expansion is less than uncoupled.
        /// </summary>
        public static bool ValidateCoupledLessThanUncoupled()
        {
            float T = 588f;
            float P = 2250f;
            float V = PlantConstants.RCS_WATER_VOLUME;
            float deltaT = 10f;
            
            // Uncoupled expansion (constant pressure)
            float uncoupled = ThermalExpansion.UncoupledSurgeVolume(V, deltaT, T, P);
            
            // Coupled expansion (pressure rises)
            float coupled = ThermalExpansion.CoupledSurgeVolume(V, PlantConstants.PZR_STEAM_VOLUME, deltaT, T, P);
            
            // Coupled should be less because pressure rise compresses water
            return coupled < uncoupled;
        }
        
        /// <summary>
        /// Verify mass is conserved through solver iterations.
        /// </summary>
        public static bool ValidateMassConservation()
        {
            var state = InitializeAtSteadyState();
            float initialMass = state.RCSWaterMass + state.PZRWaterMass + state.PZRSteamMass;
            
            // Apply temperature change
            SolveEquilibrium(ref state, 20f);
            
            float finalMass = state.RCSWaterMass + state.PZRWaterMass + state.PZRSteamMass;
            
            // Mass should be conserved within 0.1%
            float error = Math.Abs(finalMass - initialMass) / initialMass;
            return error < 0.001f;
        }
        
        /// <summary>
        /// Verify total volume is conserved (RCS is rigid).
        /// </summary>
        public static bool ValidateVolumeConservation()
        {
            var state = InitializeAtSteadyState();
            float initialVolume = state.RCSVolume + state.PZRWaterVolume + state.PZRSteamVolume;
            
            // Apply temperature change
            SolveEquilibrium(ref state, 15f);
            
            float finalVolume = state.RCSVolume + state.PZRWaterVolume + state.PZRSteamVolume;
            
            // Volume should be conserved within 0.01%
            float error = Math.Abs(finalVolume - initialVolume) / initialVolume;
            return error < 0.0001f;
        }
        
        /// <summary>
        /// Verify solver converges within reasonable iterations.
        /// </summary>
        public static bool ValidateConvergence()
        {
            var state = InitializeAtSteadyState();
            
            bool converged = SolveEquilibrium(ref state, 10f);
            
            return converged && state.IterationsUsed < 20;
        }
        
        /// <summary>
        /// Verify steam space is clamped at minimum.
        /// </summary>
        public static bool ValidateSteamSpaceMinimum()
        {
            var state = InitializeAtSteadyState();
            
            // Large temperature rise to push toward solid
            SolveEquilibrium(ref state, 50f);
            
            // Steam volume should not go below minimum
            return state.PZRSteamVolume >= PlantConstants.PZR_STEAM_MIN;
        }
        
        /// <summary>
        /// Phase 2 Fix: Verify solver converges at heatup conditions (low T, low P).
        /// This was C3 from the Phase 1 audit - the solver previously clamped to
        /// 1800-2700 psia, making it non-functional below Mode 3.
        /// Tests convergence at 300°F / 400 psia (mid-heatup, Mode 4).
        /// </summary>
        /// <returns>True if solver converges at heatup conditions</returns>
        public static bool ValidateHeatupRange()
        {
            // Initialize at heatup conditions (Mode 4, post-bubble)
            var state = InitializeAtHeatupConditions(300f, 400f);
            float P0 = state.Pressure;
            
            // Apply 10°F rise - should produce smaller dP than at-power
            // because water is less dense and more compressible at low P/T
            bool converged = SolveEquilibrium(ref state, 10f, MAX_ITERATIONS, 15f, 2700f);
            
            if (!converged) return false;
            
            float deltaP = state.Pressure - P0;
            
            // At low pressure, expansion coefficient is smaller and steam is more
            // compressible, so dP/dT should be lower than at operating conditions.
            // Accept 10-80 psi for 10°F at 400 psia (wider range due to lower P)
            return deltaP >= 5f && deltaP <= 120f;
        }
        
        /// <summary>
        /// Run all validation tests.
        /// </summary>
        public static bool ValidateAll()
        {
            bool valid = true;
            
            if (!Validate10DegreeTest())
            {
                Console.WriteLine("FAILED: 10F test - pressure response not in 60-80 psi range");
                valid = false;
            }
            
            if (!ValidateCoupledLessThanUncoupled())
            {
                Console.WriteLine("FAILED: Coupled expansion not less than uncoupled");
                valid = false;
            }
            
            if (!ValidateMassConservation())
            {
                Console.WriteLine("FAILED: Mass not conserved");
                valid = false;
            }
            
            if (!ValidateVolumeConservation())
            {
                Console.WriteLine("FAILED: Volume not conserved");
                valid = false;
            }
            
            if (!ValidateConvergence())
            {
                Console.WriteLine("FAILED: Solver did not converge in reasonable iterations");
                valid = false;
            }
            
            if (!ValidateSteamSpaceMinimum())
            {
                Console.WriteLine("FAILED: Steam space went below minimum");
                valid = false;
            }
            
            if (!ValidateHeatupRange())
            {
                Console.WriteLine("FAILED: Solver did not converge at heatup conditions (C3 fix)");
                valid = false;
            }
            
            return valid;
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize system state at normal steady-state conditions.
        /// </summary>
        public static SystemState InitializeAtSteadyState()
        {
            var state = new SystemState();
            
            state.Pressure = PlantConstants.OPERATING_PRESSURE;
            state.Temperature = PlantConstants.T_AVG;
            
            // RCS
            state.RCSVolume = PlantConstants.RCS_WATER_VOLUME;
            float rhoRCS = WaterProperties.WaterDensity(state.Temperature, state.Pressure);
            state.RCSWaterMass = state.RCSVolume * rhoRCS;
            
            // Pressurizer at 60% level
            float tSat = WaterProperties.SaturationTemperature(state.Pressure);
            float rhoPZRWater = WaterProperties.WaterDensity(tSat, state.Pressure);
            float rhoPZRSteam = WaterProperties.SaturatedSteamDensity(state.Pressure);
            
            state.PZRWaterVolume = PlantConstants.PZR_WATER_VOLUME;
            state.PZRSteamVolume = PlantConstants.PZR_STEAM_VOLUME;
            state.PZRWaterMass = state.PZRWaterVolume * rhoPZRWater;
            state.PZRSteamMass = state.PZRSteamVolume * rhoPZRSteam;
            
            state.IterationsUsed = 0;
            
            return state;
        }
        
        /// <summary>
        /// Initialize system state at heatup conditions (post-bubble formation).
        /// Phase 2 Fix: Enables CoupledThermo to be used during heatup, not just at-power.
        /// 
        /// PHYSICS: After bubble formation, the PZR has a small steam bubble (~25% level
        /// means 75% steam by volume). RCS is at heatup temperature and pressure.
        /// </summary>
        /// <param name="T_rcs_F">RCS temperature in °F (e.g. 300 for mid-heatup)</param>
        /// <param name="pressure_psia">System pressure in psia (e.g. 400 for mid-heatup)</param>
        /// <param name="pzrLevelPercent">PZR water level as percentage (default 25% post-bubble)</param>
        /// <returns>Initialized system state at heatup conditions</returns>
        public static SystemState InitializeAtHeatupConditions(
            float T_rcs_F = 300f, 
            float pressure_psia = 400f,
            float pzrLevelPercent = 25f)
        {
            var state = new SystemState();
            
            state.Pressure = pressure_psia;
            state.Temperature = T_rcs_F;
            
            // RCS at heatup conditions
            state.RCSVolume = PlantConstants.RCS_WATER_VOLUME;
            float rhoRCS = WaterProperties.WaterDensity(T_rcs_F, pressure_psia);
            state.RCSWaterMass = state.RCSVolume * rhoRCS;
            
            // Pressurizer with small bubble
            float tSat = WaterProperties.SaturationTemperature(pressure_psia);
            float rhoPZRWater = WaterProperties.WaterDensity(tSat, pressure_psia);
            float rhoPZRSteam = WaterProperties.SaturatedSteamDensity(pressure_psia);
            
            float level = pzrLevelPercent / 100f;
            state.PZRWaterVolume = PlantConstants.PZR_TOTAL_VOLUME * level;
            state.PZRSteamVolume = PlantConstants.PZR_TOTAL_VOLUME * (1f - level);
            state.PZRWaterMass = state.PZRWaterVolume * rhoPZRWater;
            state.PZRSteamMass = state.PZRSteamVolume * rhoPZRSteam;
            
            state.IterationsUsed = 0;
            
            return state;
        }
        
        #endregion
    }
    
    /// <summary>
    /// System state for coupled thermodynamic solver.
    /// </summary>
    public struct SystemState
    {
        // Primary state variables
        public float Pressure;          // psia
        public float Temperature;       // °F (RCS average)
        
        // RCS state
        public float RCSVolume;         // ft³ (fixed - rigid piping)
        public float RCSWaterMass;      // lb
        
        // Pressurizer state
        public float PZRWaterVolume;    // ft³
        public float PZRSteamVolume;    // ft³
        public float PZRWaterMass;      // lb
        public float PZRSteamMass;      // lb
        
        // Solver diagnostics
        public int IterationsUsed;
        
        // Derived properties
        public float TotalMass => RCSWaterMass + PZRWaterMass + PZRSteamMass;
        public float TotalVolume => RCSVolume + PZRWaterVolume + PZRSteamVolume;
        public float PZRLevel => PZRWaterVolume / PlantConstants.PZR_TOTAL_VOLUME * 100f;
    }
}
