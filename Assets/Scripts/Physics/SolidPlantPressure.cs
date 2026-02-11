// CRITICAL: Master the Atom - Physics Module
// SolidPlantPressure.cs - Solid Plant Pressure-Volume-Temperature Coupling
//
// Implements: Solid pressurizer operations from cold shutdown through bubble formation
//
// PHYSICS:
//   During cold shutdown the pressurizer is water-solid (no steam bubble).
//   Pressure is controlled by the CVCS charging/letdown flow balance.
//   As PZR heaters warm the water, thermal expansion creates excess volume.
//   The CVCS must remove that excess volume to keep pressure in band.
//   If CVCS cannot keep up, the RHR relief valve opens at 450 psig.
//
//   The fundamental equation is:
//     dP/dt = (dV_thermal/dt - dV_cvcs/dt) / (V_total * kappa)
//   where:
//     dV_thermal/dt = V * beta * dT/dt   (thermal expansion rate)
//     dV_cvcs/dt    = (letdown - charging) / rho  (net volume removal by CVCS)
//     kappa         = isothermal compressibility of water
//
// Sources:
//   - NRC HRTD 19.2.1 - Solid Plant Operations
//   - NRC HRTD 19.2.2 - Bubble Formation
//   - NRC HRTD 4.1    - CVCS Operations
//
// Units: psia for pressure, °F for temperature, ft³ for volume,
//        gpm for flow, lb for mass, seconds for time

using System;

namespace Critical.Physics
{
    /// <summary>
    /// State container for solid plant pressure control.
    /// Tracks all variables needed for the pressure-volume-temperature coupling
    /// during water-solid pressurizer operations.
    /// </summary>
    public struct SolidPlantState
    {
        // Primary state
        public float Pressure;              // psia
        public float T_pzr;                 // Pressurizer water temperature (°F)
        public float T_rcs;                 // RCS bulk temperature (°F)
        
        // Pressurizer thermal state
        public float HeaterEffectivePower;  // kW (after thermal lag)
        public float PzrWaterMass;          // lb
        public float PzrWallTemp;           // °F
        
        // CVCS controller state
        public float ControllerIntegral;    // Integral error accumulator (gpm·sec)
        public float LetdownFlow;           // Current letdown flow (gpm)
        public float ChargingFlow;          // Current charging flow (gpm)
        
        // Relief valve state
        public float ReliefFlow;            // RHR relief valve flow (gpm), 0 if closed
        
        // Calculated rates (for diagnostics and display)
        public float PressureRate;          // psi/hr
        public float PzrHeatRate;           // °F/hr
        public float ThermalExpansionRate;  // ft³/hr (volume rate from thermal expansion)
        public float CVCSRemovalRate;       // ft³/hr (net volume removed by CVCS)
        public float ExcessVolumeRemoved;   // gallons cumulative (sent to VCT/BRS)
        public float SurgeFlow;             // gpm — PZR thermal expansion through surge line
        public float SurgeLineHeat_MW;      // MW — natural convection heat transfer through surge line
        
        // Bubble formation
        public bool BubbleFormed;           // True when T_pzr reaches T_sat
        public float BubbleFormationTemp;   // °F at which bubble formed
        public float T_sat;                 // Current saturation temperature at pressure
        
        // Display helpers
        public float PressureSetpoint;      // psia (CVCS target)
        public float PressureError;         // psi (actual - setpoint)
        public bool InControlBand;          // True if within 320-400 psig
    }
    
    /// <summary>
    /// Solid plant pressure-volume-temperature coupling and CVCS pressure control.
    /// 
    /// This module owns all physics for the solid pressurizer regime:
    ///   - PZR water heating (heaters + surge line conduction - losses)
    ///   - Thermal expansion pressure response (dP/dt from dV_thermal - dV_cvcs)
    ///   - CVCS PI controller for pressure (adjusts letdown/charging balance)
    ///   - RHR relief valve (safety backup above 450 psig)
    ///   - Bubble formation detection (T_pzr reaches T_sat)
    ///   - RCS heating via surge line natural convection
    ///
    /// The engine should call Initialize() once, then Update() each timestep,
    /// and read results from the returned SolidPlantState.
    /// </summary>
    public static class SolidPlantPressure
    {
        #region Constants
        
        // CVCS Pressure Controller Tuning
        // PI controller: adjusts net letdown to hold pressure at setpoint
        // Output is delta-letdown in gpm added to base letdown flow
        
        /// <summary>Proportional gain: gpm per psi of pressure error</summary>
        const float KP_PRESSURE = 0.5f;
        
        /// <summary>Integral gain: gpm per psi·sec of accumulated error</summary>
        const float KI_PRESSURE = 0.02f;
        
        /// <summary>Integral windup limit in gpm</summary>
        const float INTEGRAL_LIMIT_GPM = 40f;
        
        /// <summary>Maximum CVCS can increase letdown above base flow (gpm)</summary>
        const float MAX_LETDOWN_ADJUSTMENT_GPM = 50f;
        
        /// <summary>Minimum letdown flow - cannot go below this (gpm)</summary>
        const float MIN_LETDOWN_GPM = 20f;
        
        /// <summary>Maximum letdown flow via RHR crossconnect (gpm)</summary>
        const float MAX_LETDOWN_GPM = 120f;
        
        // RHR Relief Valve
        // Opens proportionally above setpoint, full open at setpoint + accumulation
        
        /// <summary>RHR relief valve opening setpoint in psig</summary>
        const float RELIEF_SETPOINT_PSIG = 450f;
        
        /// <summary>RHR relief valve full-open accumulation above setpoint in psi</summary>
        const float RELIEF_ACCUMULATION_PSI = 20f;
        
        /// <summary>RHR relief valve full-open capacity in gpm</summary>
        const float RELIEF_CAPACITY_GPM = 200f;
        
        /// <summary>RHR relief valve reseat pressure in psig (below setpoint)</summary>
        const float RELIEF_RESEAT_PSIG = 445f;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize solid plant state for cold shutdown conditions.
        /// </summary>
        /// <param name="pressure_psia">Initial RCS pressure in psia</param>
        /// <param name="T_rcs_F">Initial RCS temperature in °F</param>
        /// <param name="T_pzr_F">Initial PZR temperature in °F (typically = T_rcs at cold shutdown)</param>
        /// <param name="baseLetdown_gpm">Base letdown flow in gpm</param>
        /// <param name="baseCharging_gpm">Base charging flow in gpm</param>
        /// <returns>Initialized solid plant state</returns>
        public static SolidPlantState Initialize(
            float pressure_psia,
            float T_rcs_F,
            float T_pzr_F,
            float baseLetdown_gpm = 75f,
            float baseCharging_gpm = 75f)
        {
            var state = new SolidPlantState();
            
            state.Pressure = pressure_psia;
            state.T_pzr = T_pzr_F;
            state.T_rcs = T_rcs_F;
            
            // PZR water mass at initial conditions
            float rho = WaterProperties.WaterDensity(T_pzr_F, pressure_psia);
            state.PzrWaterMass = PlantConstants.PZR_TOTAL_VOLUME * rho;
            state.PzrWallTemp = T_pzr_F;
            state.HeaterEffectivePower = 0f;
            
            // CVCS starts balanced
            state.LetdownFlow = baseLetdown_gpm;
            state.ChargingFlow = baseCharging_gpm;
            state.ControllerIntegral = 0f;
            
            // Relief valve closed
            state.ReliefFlow = 0f;
            
            // Rates start at zero
            state.PressureRate = 0f;
            state.PzrHeatRate = 0f;
            state.ThermalExpansionRate = 0f;
            state.CVCSRemovalRate = 0f;
            state.ExcessVolumeRemoved = 0f;
            state.SurgeFlow = 0f;
            state.SurgeLineHeat_MW = 0f;
            
            // Bubble not yet formed
            state.BubbleFormed = false;
            state.BubbleFormationTemp = 0f;
            state.T_sat = WaterProperties.SaturationTemperature(pressure_psia);
            
            // Control band
            state.PressureSetpoint = PlantConstants.SOLID_PLANT_P_SETPOINT_PSIA;
            state.PressureError = pressure_psia - state.PressureSetpoint;
            state.InControlBand = (pressure_psia >= PlantConstants.SOLID_PLANT_P_LOW_PSIA &&
                                   pressure_psia <= PlantConstants.SOLID_PLANT_P_HIGH_PSIA);
            
            return state;
        }
        
        #endregion
        
        #region Main Update
        
        /// <summary>
        /// Update solid plant state for one timestep.
        /// 
        /// This is the main entry point called by the engine each physics step.
        /// It calculates:
        ///   1. PZR water temperature change (heaters, surge line, losses)
        ///   2. Thermal expansion volume rate
        ///   3. CVCS pressure controller response (PI on letdown/charging)
        ///   4. RHR relief valve (if pressure exceeds setpoint)
        ///   5. Net pressure change from volume imbalance
        ///   6. Bubble formation check
        ///
        /// Also updates RCS temperature from surge line heat transfer.
        /// </summary>
        /// <param name="state">Current state (modified in place)</param>
        /// <param name="heaterPower_kW">PZR heater demand in kW</param>
        /// <param name="baseLetdown_gpm">Base letdown flow before controller adjustment (gpm)</param>
        /// <param name="baseCharging_gpm">Base charging flow (gpm)</param>
        /// <param name="rcsHeatCapacity_BTU_F">Total RCS heat capacity in BTU/°F (metal + water)</param>
        /// <param name="dt_hr">Timestep in hours</param>
        public static void Update(
            ref SolidPlantState state,
            float heaterPower_kW,
            float baseLetdown_gpm,
            float baseCharging_gpm,
            float rcsHeatCapacity_BTU_F,
            float dt_hr)
        {
            if (state.BubbleFormed) return;
            
            float dt_sec = dt_hr * 3600f;
            float prevT_pzr = state.T_pzr;
            float prevT_rcs = state.T_rcs;
            float prevPressure = state.Pressure;
            
            // ================================================================
            // 1. PZR WATER TEMPERATURE UPDATE
            //    Heaters warm PZR water. Heat conducts to RCS via surge line.
            //    Losses through PZR insulation to containment.
            // ================================================================
            
            // Heater thermal lag (20s time constant)
            state.HeaterEffectivePower = PressurizerPhysics.HeaterLagResponse(
                state.HeaterEffectivePower, heaterPower_kW, PlantConstants.HEATER_TAU, dt_sec);
            
            // Heat input from heaters (BTU/sec)
            float heaterHeat_BTU_sec = state.HeaterEffectivePower * PlantConstants.KW_TO_BTU_SEC;
            
            // Heat conducted from PZR to RCS via surge line (BTU/sec)
            float surgeLineHeat_MW = HeatTransfer.SurgeLineHeatTransfer_MW(
                state.T_pzr, state.T_rcs, state.Pressure);
            float surgeLineHeat_BTU_sec = surgeLineHeat_MW * PlantConstants.MW_TO_BTU_SEC;
            state.SurgeLineHeat_MW = surgeLineHeat_MW;
            
            // PZR insulation loss to containment (BTU/sec)
            float pzrAmbientLoss_BTU_sec = 0f;
            if (state.T_pzr > PlantConstants.AMBIENT_TEMP_F)
            {
                float deltaT_ambient = state.T_pzr - PlantConstants.AMBIENT_TEMP_F;
                float deltaT_ref = 650f - PlantConstants.AMBIENT_TEMP_F;
                if (deltaT_ref > 0f)
                    pzrAmbientLoss_BTU_sec = 50f * PlantConstants.KW_TO_BTU_SEC * (deltaT_ambient / deltaT_ref);
            }
            
            // Net heat to PZR water
            float netPzrHeat_BTU_sec = heaterHeat_BTU_sec - surgeLineHeat_BTU_sec - pzrAmbientLoss_BTU_sec;
            
            // PZR temperature change
            float rho_pzr = WaterProperties.WaterDensity(state.T_pzr, state.Pressure);
            float Cp_pzr = WaterProperties.WaterSpecificHeat(state.T_pzr, state.Pressure);
            float pzrWaterMass = PlantConstants.PZR_TOTAL_VOLUME * rho_pzr;
            float pzrWallCapacity = ThermalMass.PressurizerWallHeatCapacity();
            float pzrEffectiveCapacity = pzrWaterMass * Cp_pzr + pzrWallCapacity;
            
            if (pzrEffectiveCapacity > 0f)
                state.T_pzr += netPzrHeat_BTU_sec * dt_sec / pzrEffectiveCapacity;
            
            state.PzrWallTemp = state.T_pzr;
            state.PzrWaterMass = PlantConstants.PZR_TOTAL_VOLUME * 
                WaterProperties.WaterDensity(state.T_pzr, state.Pressure);
            
            // ================================================================
            // 2. RCS TEMPERATURE UPDATE
            //    Heat enters RCS via surge line conduction.
            //    Heat leaves RCS via insulation losses.
            // ================================================================
            
            float rcsInsulationLoss_MW = HeatTransfer.InsulationHeatLoss_MW(state.T_rcs);
            float rcsInsulationLoss_BTU_sec = rcsInsulationLoss_MW * PlantConstants.MW_TO_BTU_SEC;
            
            float netRcsHeat_BTU_sec = surgeLineHeat_BTU_sec - rcsInsulationLoss_BTU_sec;
            
            if (rcsHeatCapacity_BTU_F > 0f)
                state.T_rcs += netRcsHeat_BTU_sec * dt_sec / rcsHeatCapacity_BTU_F;
            
            // ================================================================
            // 3. THERMAL EXPANSION VOLUME RATE
            //    Both PZR and RCS water expand as they heat up.
            //    In a closed water-solid system, this expansion must be
            //    accommodated by the CVCS or pressure will rise.
            // ================================================================
            
            float pzrDeltaT = state.T_pzr - prevT_pzr;
            float rcsDeltaT = state.T_rcs - prevT_rcs;
            
            // PZR expansion
            float beta_pzr = ThermalExpansion.ExpansionCoefficient(state.T_pzr, state.Pressure);
            float dV_pzr_ft3 = PlantConstants.PZR_TOTAL_VOLUME * beta_pzr * pzrDeltaT;
            
            // RCS expansion
            float beta_rcs = ThermalExpansion.ExpansionCoefficient(state.T_rcs, state.Pressure);
            float dV_rcs_ft3 = PlantConstants.RCS_WATER_VOLUME * beta_rcs * rcsDeltaT;
            
            // Total thermal expansion this timestep (ft³)
            float dV_thermal_ft3 = dV_pzr_ft3 + dV_rcs_ft3;
            
            // ================================================================
            // 4. CVCS PRESSURE CONTROLLER
            //    PI controller adjusts letdown flow to maintain pressure setpoint.
            //    When pressure rises, increase letdown to bleed off volume.
            //    When pressure drops, decrease letdown to retain volume.
            // ================================================================
            
            float pressureError_psi = state.Pressure - state.PressureSetpoint;
            
            // Proportional term: immediate response to pressure error
            float pTerm = KP_PRESSURE * pressureError_psi;
            
            // Integral term: accumulated error drives steady-state correction
            state.ControllerIntegral += pressureError_psi * dt_sec;
            float integralLimit = INTEGRAL_LIMIT_GPM / KI_PRESSURE;
            state.ControllerIntegral = Math.Max(-integralLimit, Math.Min(state.ControllerIntegral, integralLimit));
            float iTerm = KI_PRESSURE * state.ControllerIntegral;
            
            // Controller output: adjustment to letdown flow (positive = more letdown)
            float letdownAdjustment = pTerm + iTerm;
            letdownAdjustment = Math.Max(-MAX_LETDOWN_ADJUSTMENT_GPM, 
                                Math.Min(letdownAdjustment, MAX_LETDOWN_ADJUSTMENT_GPM));
            
            // Apply to letdown flow
            state.LetdownFlow = baseLetdown_gpm + letdownAdjustment;
            state.LetdownFlow = Math.Max(MIN_LETDOWN_GPM, Math.Min(state.LetdownFlow, MAX_LETDOWN_GPM));
            
            // Charging stays at base (CVCS controls pressure via letdown in solid plant)
            state.ChargingFlow = baseCharging_gpm;
            
            // ================================================================
            // 5. RHR RELIEF VALVE
            //    Safety backup - opens if CVCS cannot maintain pressure below 450 psig.
            //    Proportional opening above setpoint.
            // ================================================================
            
            float pressure_psig = state.Pressure - PlantConstants.PSIG_TO_PSIA;
            state.ReliefFlow = CalculateReliefFlow(pressure_psig, state.ReliefFlow > 0f);
            
            // ================================================================
            // 6. NET VOLUME BALANCE AND PRESSURE CHANGE
            //    dP = (dV_thermal - dV_removed) / (V_total * kappa)
            //    where dV_removed includes CVCS net flow and relief valve flow
            // ================================================================
            
            // CVCS net volume removal rate (ft³/sec)
            // Positive = net volume leaving RCS (letdown > charging = pressure decreases)
            float netCVCS_gpm = state.LetdownFlow - state.ChargingFlow + state.ReliefFlow;
            float rho_avg = WaterProperties.WaterDensity(
                (state.T_pzr + state.T_rcs) / 2f, state.Pressure);
            float netCVCS_ft3_sec = netCVCS_gpm * PlantConstants.GPM_TO_FT3_SEC;
            
            // Volume removed by CVCS this timestep
            float dV_cvcs_ft3 = netCVCS_ft3_sec * dt_sec;
            
            // Net volume imbalance (positive = pressure rises)
            float dV_net_ft3 = dV_thermal_ft3 - dV_cvcs_ft3;
            
            // Pressure change from volume imbalance in a closed, water-solid system
            // dP = dV_net / (V_total * kappa)
            // where kappa is isothermal compressibility
            float T_avg = (state.T_pzr * PlantConstants.PZR_TOTAL_VOLUME + 
                          state.T_rcs * PlantConstants.RCS_WATER_VOLUME) /
                         (PlantConstants.PZR_TOTAL_VOLUME + PlantConstants.RCS_WATER_VOLUME);
            float kappa = ThermalExpansion.Compressibility(T_avg, state.Pressure);
            float V_total = PlantConstants.RCS_WATER_VOLUME + PlantConstants.PZR_TOTAL_VOLUME;
            
            float dP_psi = 0f;
            if (kappa > 1e-12f && V_total > 0f)
                dP_psi = dV_net_ft3 / (V_total * kappa);
            
            state.Pressure += dP_psi;
            
            // Hard physical floor: pressure cannot go below atmospheric
            // (this is not a control clamp — it's a physical impossibility)
            state.Pressure = Math.Max(state.Pressure, PlantConstants.P_ATM);
            
            // ================================================================
            // 7. UPDATE DIAGNOSTIC RATES
            // ================================================================
            
            if (dt_hr > 1e-8f)
            {
                state.PressureRate = (state.Pressure - prevPressure) / dt_hr;
                state.PzrHeatRate = (state.T_pzr - prevT_pzr) / dt_hr;
            }
            
            state.ThermalExpansionRate = (dt_hr > 1e-8f) ? dV_thermal_ft3 / dt_hr : 0f;
            state.CVCSRemovalRate = (dt_hr > 1e-8f) ? dV_cvcs_ft3 / dt_hr : 0f;
            
            // Surge flow: PZR thermal expansion drives water through the surge line
            // into the hot leg. Same formula as RCSHeatup.IsolatedHeatingStep:
            //   flow_gpm = dV_pzr (ft³) × FT3_TO_GAL / dt_hr / 60
            // Positive = PZR expanding, water flowing out to RCS
            state.SurgeFlow = (dt_hr > 1e-8f) ? (dV_pzr_ft3 * PlantConstants.FT3_TO_GAL / dt_hr / 60f) : 0f;
            
            // Track cumulative excess volume removed (in gallons for operator display)
            if (dV_cvcs_ft3 > 0f)
                state.ExcessVolumeRemoved += dV_cvcs_ft3 * PlantConstants.FT3_TO_GAL;
            
            // ================================================================
            // 8. UPDATE SATURATION AND CONTROL BAND STATUS
            // ================================================================
            
            state.T_sat = WaterProperties.SaturationTemperature(state.Pressure);
            state.PressureError = state.Pressure - state.PressureSetpoint;
            state.InControlBand = (state.Pressure >= PlantConstants.SOLID_PLANT_P_LOW_PSIA &&
                                   state.Pressure <= PlantConstants.SOLID_PLANT_P_HIGH_PSIA);
            
            // ================================================================
            // 9. BUBBLE FORMATION CHECK
            //    Bubble forms when PZR water reaches saturation temperature
            //    at current system pressure.
            // ================================================================
            
            if (state.T_pzr >= state.T_sat - 2f && 
                state.T_pzr >= PlantConstants.BUBBLE_FORMATION_TEMP_F)
            {
                state.BubbleFormed = true;
                state.BubbleFormationTemp = state.T_pzr;
            }
        }
        
        #endregion
        
        #region Relief Valve
        
        /// <summary>
        /// Calculate RHR relief valve flow based on pressure.
        /// Proportional opening above setpoint with hysteresis on reseat.
        /// </summary>
        /// <param name="pressure_psig">Current pressure in psig</param>
        /// <param name="currentlyOpen">True if valve is currently flowing</param>
        /// <returns>Relief flow in gpm</returns>
        public static float CalculateReliefFlow(float pressure_psig, bool currentlyOpen)
        {
            // Valve opens above setpoint
            if (pressure_psig >= RELIEF_SETPOINT_PSIG)
            {
                float fraction = (pressure_psig - RELIEF_SETPOINT_PSIG) / RELIEF_ACCUMULATION_PSI;
                fraction = Math.Max(0f, Math.Min(fraction, 1f));
                return fraction * RELIEF_CAPACITY_GPM;
            }
            
            // Hysteresis: if already open, don't close until reseat pressure
            if (currentlyOpen && pressure_psig > RELIEF_RESEAT_PSIG)
            {
                // Reduced flow between reseat and setpoint
                float fraction = (pressure_psig - RELIEF_RESEAT_PSIG) / 
                                (RELIEF_SETPOINT_PSIG - RELIEF_RESEAT_PSIG);
                fraction = Math.Max(0f, Math.Min(fraction, 0.3f));
                return fraction * RELIEF_CAPACITY_GPM;
            }
            
            return 0f;
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Estimate time to bubble formation from current conditions.
        /// </summary>
        /// <param name="state">Current solid plant state</param>
        /// <returns>Estimated hours to bubble formation, or 999 if rate is too low</returns>
        public static float EstimateTimeToBubble(SolidPlantState state)
        {
            float tempMargin = state.T_sat - state.T_pzr;
            if (tempMargin <= 0f) return 0f;
            if (state.PzrHeatRate < 0.1f) return 999f;
            return tempMargin / state.PzrHeatRate;
        }
        
        /// <summary>
        /// Get a human-readable status string for the solid plant state.
        /// </summary>
        public static string GetStatusString(SolidPlantState state)
        {
            if (state.BubbleFormed)
                return "BUBBLE FORMED";
            
            float margin = state.T_sat - state.T_pzr;
            float P_psig = state.Pressure - PlantConstants.PSIG_TO_PSIA;
            
            if (state.ReliefFlow > 0.1f)
                return $"SOLID PZR {P_psig:F0}psig - RELIEF VALVE OPEN ({state.ReliefFlow:F0}gpm)";
            
            return $"SOLID PZR {P_psig:F0}psig - {margin:F0}°F to bubble";
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate solid plant pressure physics.
        /// </summary>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: Initialization should produce valid state
            var state = Initialize(365f, 100f, 100f);
            if (state.BubbleFormed) valid = false;
            if (state.Pressure != 365f) valid = false;
            if (state.ReliefFlow != 0f) valid = false;
            
            // Test 2: Heating should raise PZR temperature
            float initialT = state.T_pzr;
            Update(ref state, 1800f, 75f, 75f, 1100000f, 1f/360f);
            if (state.T_pzr <= initialT) valid = false;
            
            // Test 3: Thermal expansion should cause pressure change
            // (not zero — the old bug was zero pressure change)
            // Note: with CVCS active, pressure change will be small but nonzero
            var state2 = Initialize(365f, 100f, 100f);
            float P0 = state2.Pressure;
            // Several steps to accumulate measurable change
            for (int i = 0; i < 100; i++)
                Update(ref state2, 1800f, 75f, 75f, 1100000f, 1f/360f);
            // Pressure should have moved from initial (CVCS is responding but not perfect)
            // Accept any nonzero change as proof the physics is working
            float dP = Math.Abs(state2.Pressure - P0);
            if (dP < 0.01f) valid = false;
            
            // Test 4: Relief valve should open above 450 psig
            float reliefFlow = CalculateReliefFlow(460f, false);
            if (reliefFlow <= 0f) valid = false;
            
            // Test 5: Relief valve should be closed well below setpoint
            float noRelief = CalculateReliefFlow(400f, false);
            if (noRelief != 0f) valid = false;
            
            // Test 6: CVCS should increase letdown when pressure is high
            var stateHigh = Initialize(PlantConstants.SOLID_PLANT_P_HIGH_PSIA, 100f, 100f);
            Update(ref stateHigh, 1800f, 75f, 75f, 1100000f, 1f/360f);
            if (stateHigh.LetdownFlow <= 75f) valid = false; // Should be above base
            
            // Test 7: CVCS should decrease letdown when pressure is low
            var stateLow = Initialize(PlantConstants.SOLID_PLANT_P_LOW_PSIA, 100f, 100f);
            Update(ref stateLow, 1800f, 75f, 75f, 1100000f, 1f/360f);
            if (stateLow.LetdownFlow >= 75f) valid = false; // Should be below base
            
            // Test 8: Bubble formation at T_sat
            var stateBubble = Initialize(365f, 100f, 430f);
            stateBubble.T_pzr = 436f; // Above T_sat at 365 psia (~435°F)
            Update(ref stateBubble, 1800f, 75f, 75f, 1100000f, 1f/360f);
            if (!stateBubble.BubbleFormed) valid = false;
            
            // Test 9: Surge flow should be non-zero when PZR is heating
            // PZR thermal expansion drives water through surge line
            var stateSurge = Initialize(365f, 100f, 100f);
            for (int i = 0; i < 10; i++)
                Update(ref stateSurge, 1800f, 75f, 75f, 1100000f, 1f/360f);
            if (stateSurge.SurgeFlow <= 0f) valid = false;
            
            // Test 10: Surge line heat should be non-zero when T_pzr > T_rcs
            // After a few steps, PZR will be warmer than RCS
            if (stateSurge.SurgeLineHeat_MW <= 0f) valid = false;
            
            return valid;
        }
        
        #endregion
    }
}
