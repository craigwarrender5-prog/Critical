// ============================================================================
// CRITICAL: Master the Atom - Steam Dump Controller
// SteamDumpController.cs - Steam Dump System for HZP Temperature Control
// ============================================================================
//
// PURPOSE:
//   Controls steam dump to main condenser for Hot Zero Power (HZP) temperature
//   stabilization. During HZP, the RCPs add ~21 MW of heat with no reactor
//   power or turbine load. The steam dump system removes this excess heat
//   by dumping steam from SG secondary side to condenser, maintaining
//   RCS T_avg at 557°F.
//
// PHYSICS MODEL:
//   Steam Pressure Control Mode (per NRC HRTD 19.0):
//   - Steam header pressure setpoint: 1092 psig (saturation at 557°F)
//   - Proportional control: dump_demand = Kp × (P_steam - P_setpoint)
//   - Valve dynamics: first-order lag with 10-second stroke time
//   - Heat removal: Q_dump = valve_position × Q_max
//
//   Heat Balance at HZP:
//   - Heat input: 21 MW (4 RCPs) + 0.5 MW (heaters) = 21.5 MW
//   - Heat output: Q_dump (steam dump) + 1.5 MW (insulation losses)
//   - At equilibrium: Q_dump ≈ 20 MW
//
// OPERATING MODES:
//   OFF           - Steam dump inactive (during heatup)
//   STEAM_PRESSURE - Pressure control at 1092 psig (HZP default)
//   TAVG          - Temperature control at 557°F (future implementation)
//
// SOURCES:
//   - NRC HRTD 19.0 — Plant Operations (ML11223A342)
//   - NRC HRTD 11.2 — Steam Dump Control System
//   - Westinghouse 4-Loop PWR Technical Specifications
//
// UNITS:
//   Pressure: psig | Temperature: °F | Power: MW
//   Time: hours | Flow: fraction (0-1)
//
// VERSION: 1.1.0 (Stage 2)
// CLASSIFICATION: Physics — Realism Critical
// GOLD STANDARD: Yes
// ============================================================================

using System;
using UnityEngine;

namespace Critical.Physics
{
    /// <summary>
    /// Steam dump operating mode.
    /// Per NRC HRTD 11.2, steam dumps can operate in multiple control modes.
    /// </summary>
    public enum SteamDumpMode
    {
        /// <summary>Steam dump disabled (during heatup below HZP approach)</summary>
        OFF,
        
        /// <summary>
        /// Steam pressure control mode.
        /// Maintains steam header pressure at 1092 psig (saturation at 557°F).
        /// Default mode for HZP operations per NRC HRTD 19.0.
        /// </summary>
        STEAM_PRESSURE,
        
        /// <summary>
        /// T_avg control mode (future implementation).
        /// Directly controls RCS average temperature at setpoint.
        /// </summary>
        TAVG
    }
    
    /// <summary>
    /// Steam dump controller state for persistence between timesteps.
    /// </summary>
    public struct SteamDumpState
    {
        /// <summary>Current operating mode</summary>
        public SteamDumpMode Mode;
        
        /// <summary>Steam pressure setpoint in psig</summary>
        public float PressureSetpoint_psig;
        
        /// <summary>T_avg setpoint in °F (for TAVG mode)</summary>
        public float TavgSetpoint_F;
        
        /// <summary>Current dump demand (0-1 fraction, controller output)</summary>
        public float DumpDemand;
        
        /// <summary>Current valve position (0-1 fraction, with dynamics)</summary>
        public float ValvePosition;
        
        /// <summary>Current heat removal rate in MW</summary>
        public float HeatRemoval_MW;
        
        /// <summary>Steam pressure error in psi (actual - setpoint)</summary>
        public float PressureError_psi;
        
        /// <summary>True if steam dump is actively removing heat</summary>
        public bool IsActive;
        
        /// <summary>Simulation time when mode was last changed</summary>
        public float ModeChangeTime;
        
        /// <summary>Status message for display</summary>
        public string StatusMessage;
    }
    
    /// <summary>
    /// Steam Dump Controller for HZP Temperature Control.
    /// 
    /// Implements steam pressure control mode per NRC HRTD 19.0:
    /// "The primary plant heatup is terminated by automatic actuation of
    /// the steam dumps (in steam pressure control) when the pressure inside
    /// the steam header reaches 1092 psig."
    /// </summary>
    public static class SteamDumpController
    {
        // ============================================================================
        // INITIALIZATION
        // ============================================================================
        
        /// <summary>
        /// Initialize steam dump controller to OFF state.
        /// Called at simulation startup.
        /// </summary>
        /// <returns>Initialized controller state</returns>
        public static SteamDumpState Initialize()
        {
            var state = new SteamDumpState
            {
                Mode = SteamDumpMode.OFF,
                PressureSetpoint_psig = PlantConstants.SteamDump.STEAM_PRESSURE_SETPOINT_PSIG,
                TavgSetpoint_F = PlantConstants.SteamDump.HZP_TAVG_SETPOINT_F,
                DumpDemand = 0f,
                ValvePosition = 0f,
                HeatRemoval_MW = 0f,
                PressureError_psi = 0f,
                IsActive = false,
                ModeChangeTime = 0f,
                StatusMessage = "Steam Dump OFF"
            };
            
            return state;
        }
        
        // ============================================================================
        // MODE CONTROL
        // ============================================================================
        
        /// <summary>
        /// Enable steam dump in steam pressure control mode.
        /// Called when approaching HZP conditions (T_avg > 550°F).
        /// 
        /// Per NRC HRTD 19.0: Steam dumps actuate automatically when steam
        /// header pressure reaches 1092 psig.
        /// </summary>
        /// <param name="state">Controller state to modify</param>
        /// <param name="simTime">Current simulation time in hours</param>
        public static void EnableSteamPressureMode(ref SteamDumpState state, float simTime)
        {
            if (state.Mode != SteamDumpMode.STEAM_PRESSURE)
            {
                state.Mode = SteamDumpMode.STEAM_PRESSURE;
                state.ModeChangeTime = simTime;
                state.StatusMessage = "Steam Pressure Mode - 1092 psig";
                Debug.Log($"[SteamDump] Enabled STEAM_PRESSURE mode at T+{simTime:F2}hr");
            }
        }
        
        /// <summary>
        /// Enable steam dump in T_avg control mode (future implementation).
        /// </summary>
        /// <param name="state">Controller state to modify</param>
        /// <param name="simTime">Current simulation time in hours</param>
        /// <param name="tavgSetpoint">T_avg setpoint in °F</param>
        public static void EnableTavgMode(ref SteamDumpState state, float simTime, float tavgSetpoint = 557f)
        {
            if (state.Mode != SteamDumpMode.TAVG)
            {
                state.Mode = SteamDumpMode.TAVG;
                state.TavgSetpoint_F = tavgSetpoint;
                state.ModeChangeTime = simTime;
                state.StatusMessage = $"T_avg Mode - {tavgSetpoint:F0}°F";
                Debug.Log($"[SteamDump] Enabled TAVG mode at T+{simTime:F2}hr, setpoint={tavgSetpoint:F1}°F");
            }
        }
        
        /// <summary>
        /// Disable steam dump system.
        /// </summary>
        /// <param name="state">Controller state to modify</param>
        /// <param name="simTime">Current simulation time in hours</param>
        public static void Disable(ref SteamDumpState state, float simTime)
        {
            if (state.Mode != SteamDumpMode.OFF)
            {
                state.Mode = SteamDumpMode.OFF;
                state.ModeChangeTime = simTime;
                state.DumpDemand = 0f;
                // Note: ValvePosition will decay naturally via dynamics
                state.StatusMessage = "Steam Dump OFF";
                Debug.Log($"[SteamDump] Disabled at T+{simTime:F2}hr");
            }
        }
        
        // ============================================================================
        // MAIN UPDATE
        // ============================================================================
        
        /// <summary>
        /// Update steam dump controller for one timestep.
        /// 
        /// Calculates dump demand based on operating mode, applies valve dynamics,
        /// and computes resulting heat removal rate.
        /// </summary>
        /// <param name="state">Controller state (modified in place)</param>
        /// <param name="steamPressure_psig">Current steam header pressure in psig</param>
        /// <param name="T_avg">Current RCS average temperature in °F</param>
        /// <param name="dt_hr">Timestep in hours</param>
        /// <returns>Heat removal rate in MW</returns>
        public static float Update(
            ref SteamDumpState state,
            float steamPressure_psig,
            float T_avg,
            float dt_hr)
        {
            // ================================================================
            // DEMAND CALCULATION (mode-dependent)
            // ================================================================
            
            float demand = 0f;
            
            switch (state.Mode)
            {
                case SteamDumpMode.OFF:
                    demand = 0f;
                    state.PressureError_psi = 0f;
                    state.StatusMessage = "Steam Dump OFF";
                    break;
                    
                case SteamDumpMode.STEAM_PRESSURE:
                    demand = CalculateSteamPressureDemand(
                        steamPressure_psig,
                        state.PressureSetpoint_psig,
                        out float pressureError);
                    state.PressureError_psi = pressureError;
                    state.StatusMessage = demand > 0.01f
                        ? $"Pressure Mode: {steamPressure_psig:F0} psig (err={pressureError:+0;-0;0})"
                        : $"Pressure Mode: {steamPressure_psig:F0} psig (at setpoint)";
                    break;
                    
                case SteamDumpMode.TAVG:
                    demand = CalculateTavgDemand(
                        T_avg,
                        state.TavgSetpoint_F,
                        out float tempError);
                    state.PressureError_psi = tempError; // Reuse field for temp error
                    state.StatusMessage = demand > 0.01f
                        ? $"T_avg Mode: {T_avg:F1}°F (err={tempError:+0.0;-0.0;0})"
                        : $"T_avg Mode: {T_avg:F1}°F (at setpoint)";
                    break;
            }
            
            state.DumpDemand = demand;
            
            // ================================================================
            // VALVE DYNAMICS (first-order lag)
            // ================================================================
            
            state.ValvePosition = ApplyValveDynamics(
                state.ValvePosition,
                demand,
                dt_hr);
            
            // ================================================================
            // HEAT REMOVAL CALCULATION
            // ================================================================
            
            // Below minimum pressure, steam dump ineffective
            if (steamPressure_psig < PlantConstants.SteamDump.STEAM_DUMP_MIN_PRESSURE_PSIG)
            {
                state.HeatRemoval_MW = 0f;
                state.IsActive = false;
            }
            else
            {
                state.HeatRemoval_MW = state.ValvePosition * PlantConstants.SteamDump.STEAM_DUMP_MAX_MW;
                state.IsActive = state.ValvePosition > PlantConstants.SteamDump.STEAM_DUMP_DEADBAND;
            }
            
            return state.HeatRemoval_MW;
        }
        
        // ============================================================================
        // CONTROL ALGORITHMS
        // ============================================================================
        
        /// <summary>
        /// Calculate steam dump demand in steam pressure control mode.
        /// 
        /// Per NRC HRTD 19.0: Proportional control based on steam header pressure.
        /// Dumps open when pressure exceeds 1092 psig setpoint.
        /// </summary>
        /// <param name="steamPressure_psig">Current steam header pressure in psig</param>
        /// <param name="setpoint_psig">Pressure setpoint in psig</param>
        /// <param name="error">Output: pressure error in psi</param>
        /// <returns>Dump demand (0-1 fraction)</returns>
        private static float CalculateSteamPressureDemand(
            float steamPressure_psig,
            float setpoint_psig,
            out float error)
        {
            // Pressure error (positive = above setpoint, need to dump)
            error = steamPressure_psig - setpoint_psig;
            
            // Only dump if pressure is ABOVE setpoint
            if (error <= 0f)
            {
                return 0f;
            }
            
            // Proportional demand
            float demand = error * PlantConstants.SteamDump.STEAM_DUMP_KP;
            
            // Clamp to 0-1 range
            return Mathf.Clamp01(demand);
        }
        
        /// <summary>
        /// Calculate steam dump demand in T_avg control mode.
        /// 
        /// Direct temperature control: dumps open when T_avg exceeds setpoint.
        /// Uses the relationship between T_avg and steam saturation pressure.
        /// </summary>
        /// <param name="T_avg">Current RCS average temperature in °F</param>
        /// <param name="setpoint_F">T_avg setpoint in °F</param>
        /// <param name="error">Output: temperature error in °F</param>
        /// <returns>Dump demand (0-1 fraction)</returns>
        private static float CalculateTavgDemand(
            float T_avg,
            float setpoint_F,
            out float error)
        {
            // Temperature error (positive = above setpoint, need to dump)
            error = T_avg - setpoint_F;
            
            // Only dump if temperature is ABOVE setpoint
            if (error <= 0f)
            {
                return 0f;
            }
            
            // Convert temperature error to equivalent pressure error
            // At HZP conditions, ~1°F T_avg ≈ ~3 psi steam pressure
            // Use a gain of ~0.15 per °F (equivalent to Kp=0.05 per psi × 3 psi/°F)
            float demand = error * 0.15f;
            
            return Mathf.Clamp01(demand);
        }
        
        /// <summary>
        /// Apply first-order lag dynamics to valve position.
        /// 
        /// Models the physical stroke time of steam dump valves.
        /// Prevents instantaneous changes in heat removal.
        /// </summary>
        /// <param name="currentPosition">Current valve position (0-1)</param>
        /// <param name="demand">Demanded valve position (0-1)</param>
        /// <param name="dt_hr">Timestep in hours</param>
        /// <returns>New valve position (0-1)</returns>
        private static float ApplyValveDynamics(
            float currentPosition,
            float demand,
            float dt_hr)
        {
            // Time constant from stroke time (τ ≈ stroke_time / 3 for 95% response)
            float tau_hr = PlantConstants.SteamDump.STEAM_DUMP_STROKE_TIME_HR;
            
            // First-order lag: dV/dt = (demand - V) / τ
            float deltaV = (demand - currentPosition) * dt_hr / tau_hr;
            
            // Apply change
            float newPosition = currentPosition + deltaV;
            
            // Clamp to physical limits
            return Mathf.Clamp01(newPosition);
        }
        
        // ============================================================================
        // QUERY METHODS
        // ============================================================================
        
        /// <summary>
        /// Check if steam dump is enabled (any mode except OFF).
        /// </summary>
        public static bool IsEnabled(SteamDumpState state)
        {
            return state.Mode != SteamDumpMode.OFF;
        }
        
        /// <summary>
        /// Check if steam dump is actively removing heat.
        /// </summary>
        public static bool IsActive(SteamDumpState state)
        {
            return state.IsActive && state.HeatRemoval_MW > 0.1f;
        }
        
        /// <summary>
        /// Get the mode as a display string.
        /// </summary>
        public static string GetModeString(SteamDumpState state)
        {
            switch (state.Mode)
            {
                case SteamDumpMode.OFF:
                    return "OFF";
                case SteamDumpMode.STEAM_PRESSURE:
                    return "STEAM_PRESSURE";
                case SteamDumpMode.TAVG:
                    return "TAVG";
                default:
                    return "UNKNOWN";
            }
        }
        
        /// <summary>
        /// Get steam mass flow rate for a given heat removal rate.
        /// 
        /// m_dot = Q / h_fg
        /// where h_fg is latent heat of vaporization at HZP conditions.
        /// </summary>
        /// <param name="heatRemoval_MW">Heat removal rate in MW</param>
        /// <returns>Steam mass flow in lb/hr</returns>
        public static float GetSteamMassFlow(float heatRemoval_MW)
        {
            // Convert MW to BTU/hr
            float heatRate_BTU_hr = heatRemoval_MW * PlantConstants.MW_TO_BTU_HR;
            
            // Steam mass flow = heat / latent heat
            float massFlow_lb_hr = heatRate_BTU_hr / PlantConstants.SteamDump.STEAM_ENTHALPY_HFG_BTU_LB;
            
            return massFlow_lb_hr;
        }
        
        // ============================================================================
        // AUTOMATIC ENABLE LOGIC
        // ============================================================================
        
        /// <summary>
        /// Check if conditions are met to automatically enable steam dump.
        /// 
        /// Per NRC HRTD 19.0: Steam dumps actuate when approaching HZP.
        /// Conditions: T_avg > 550°F AND steam pressure available
        /// </summary>
        /// <param name="T_avg">Current RCS average temperature in °F</param>
        /// <param name="steamPressure_psig">Current steam header pressure in psig</param>
        /// <returns>True if steam dump should be enabled</returns>
        public static bool ShouldAutoEnable(float T_avg, float steamPressure_psig)
        {
            // Temperature above HZP approach threshold
            bool tempOK = T_avg >= PlantConstants.SteamDump.HZP_APPROACH_TEMP_F;
            
            // Steam pressure above minimum for dump operation
            bool pressureOK = steamPressure_psig >= PlantConstants.SteamDump.STEAM_DUMP_MIN_PRESSURE_PSIG;
            
            return tempOK && pressureOK;
        }
        
        // ============================================================================
        // VALIDATION
        // ============================================================================
        
        /// <summary>
        /// Validate steam dump controller calculations.
        /// </summary>
        /// <returns>True if all validations pass</returns>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: Initialization should produce valid OFF state
            var state = Initialize();
            if (state.Mode != SteamDumpMode.OFF) valid = false;
            if (state.ValvePosition != 0f) valid = false;
            if (state.HeatRemoval_MW != 0f) valid = false;
            
            // Test 2: Mode enable should change mode
            EnableSteamPressureMode(ref state, 10f);
            if (state.Mode != SteamDumpMode.STEAM_PRESSURE) valid = false;
            
            // Test 3: Pressure above setpoint should produce positive demand
            float q = Update(ref state, 1112f, 557f, 1f/360f);  // 20 psi above setpoint
            if (state.DumpDemand <= 0f) valid = false;
            
            // Test 4: Pressure at setpoint should produce zero demand
            state = Initialize();
            EnableSteamPressureMode(ref state, 10f);
            Update(ref state, 1092f, 557f, 1f/360f);  // At setpoint
            if (state.DumpDemand != 0f) valid = false;
            
            // Test 5: Pressure below setpoint should produce zero demand
            state = Initialize();
            EnableSteamPressureMode(ref state, 10f);
            Update(ref state, 1050f, 557f, 1f/360f);  // Below setpoint
            if (state.DumpDemand != 0f) valid = false;
            
            // Test 6: OFF mode should produce no heat removal
            state = Initialize();
            Update(ref state, 1150f, 600f, 1f/360f);  // High pressure but OFF
            if (state.HeatRemoval_MW != 0f) valid = false;
            
            // Test 7: Steam mass flow calculation
            float massFlow = GetSteamMassFlow(20f);  // 20 MW
            // 20 MW × 3.412×10^6 BTU/hr/MW / 650 BTU/lb ≈ 105,000 lb/hr
            if (massFlow < 100000f || massFlow > 110000f) valid = false;
            
            // Test 8: Auto-enable conditions
            if (!ShouldAutoEnable(555f, 1000f)) valid = false;   // Should enable
            if (ShouldAutoEnable(400f, 1000f)) valid = false;    // Temp too low
            if (ShouldAutoEnable(555f, 800f)) valid = false;     // Pressure too low
            
            return valid;
        }
    }
}
