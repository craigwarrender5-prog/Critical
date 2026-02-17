// CRITICAL: Master the Atom - Phase 1 Core Physics Engine
// AlarmManager.cs - Plant Alarm and Annunciator Logic
//
// Implements: Engine Architecture Audit Fix - Priority 5
//   Extracts ~20 inline alarm setpoint checks from HeatupSimEngine
//   into a centralized alarm management module.
//
// Per NRC HRTD documentation, alarm setpoints are:
//   - PZR Level Low: < 20%
//   - PZR Level High: > 85%
//   - Pressure Low: < 350 psia
//   - Pressure High: > 2300 psia
//   - SG Secondary Pressure High: > 1085 psig
//   - Subcooling Low: < 30°F
//   - SMM Low Margin: < 15°F
//   - SMM No Margin: ≤ 0°F
//   - RVLIS Level Low: < 90% (when Full Range valid)
//
// Units: %, psia, °F as appropriate

namespace Critical.Physics
{
    /// <summary>
    /// Complete alarm state for all plant annunciators.
    /// Calculated by AlarmManager.CheckAlarms() each timestep.
    /// </summary>
    public struct AlarmState
    {
        // Pressurizer alarms
        public bool PZRLevelLow;         // Level < 20%
        public bool PZRLevelHigh;        // Level > 85%
        public bool SteamBubbleOK;       // Bubble exists and level in safe range
        
        // RCS alarms
        public bool RCSFlowLow;          // No RCPs running
        public bool PressureLow;         // Pressure < 350 psia
        public bool PressureHigh;        // Pressure > 2300 psia
        public bool SGSecondaryPressureHigh; // SG secondary pressure > high alarm setpoint
        
        // Subcooling margin alarms
        public bool SubcoolingLow;       // Subcooling < 30°F
        public bool SMMLowMargin;        // Subcooling < 15°F (but > 0)
        public bool SMMNoMargin;         // Subcooling ≤ 0°F (at saturation)
        
        // RVLIS alarms
        public bool RVLISLevelLow;       // Full range < 90% when valid
        
        // Status indicators
        public bool HeatupInProgress;    // Heatup rate > 1°F/hr
        public bool ModePermissive;      // Conditions OK for mode change
        
        // Equipment status (not alarms, but tracked here for convenience)
        public bool CCWRunning;          // Component cooling water running
        public bool SealInjectionOK;     // Seal injection adequate
        public bool ChargingActive;      // Charging flow > 0
        public bool LetdownActive;       // Letdown flow > 0
    }
    
    /// <summary>
    /// Input parameters for alarm checking.
    /// Consolidates all values needed to evaluate alarm conditions.
    /// </summary>
    public struct AlarmInputs
    {
        public float PZRLevel;           // Pressurizer level (%)
        public float Pressure;           // RCS pressure (psia)
        public float SGSecondaryPressure_psia; // SG secondary pressure (psia)
        public float Subcooling;         // Subcooling margin (°F)
        public float HeatupRate;         // RCS heatup rate (°F/hr)
        public float RVLISFull;          // RVLIS full range (%)
        public bool RVLISFullValid;      // RVLIS full range validity
        public bool BubbleFormed;        // Steam bubble exists
        public bool SolidPressurizer;    // True during solid plant ops (PZR 100% water, no bubble)
        public int RCPCount;             // Number of RCPs running
        public float ChargingFlow;       // Charging flow (gpm)
        public float LetdownFlow;        // Letdown flow (gpm)
        public float SealInjection;      // Seal injection flow (gpm)
    }
    
    /// <summary>
    /// Centralized alarm setpoint checking for all plant annunciators.
    /// Extracts inline alarm logic from engine into testable module.
    /// </summary>
    public static class AlarmManager
    {
        // Alarm setpoints per NRC HRTD documentation
        public const float PZR_LEVEL_LOW_SETPOINT = 20f;          // %
        public const float PZR_LEVEL_HIGH_SETPOINT = 85f;         // %
        public const float PZR_LEVEL_BUBBLE_MIN = 5f;             // %
        public const float PZR_LEVEL_BUBBLE_MAX = 95f;            // %
        public const float PRESSURE_LOW_SETPOINT = 350f;          // psia
        public const float PRESSURE_HIGH_SETPOINT = 2300f;        // psia
        public const float SG_SECONDARY_PRESSURE_HIGH_SETPOINT_PSIA = 1099.7f; // 1085 psig
        public const float SUBCOOLING_LOW_SETPOINT = 30f;         // °F
        public const float SMM_LOW_MARGIN_SETPOINT = 15f;         // °F
        public const float SMM_NO_MARGIN_SETPOINT = 0f;           // °F
        public const float RVLIS_LEVEL_LOW_SETPOINT = 90f;        // %
        public const float HEATUP_IN_PROGRESS_RATE = 1f;          // °F/hr
        public const float MIN_SEAL_INJECTION_PER_RCP = 7f;       // gpm
        
        /// <summary>
        /// Check all alarm conditions and return complete alarm state.
        /// 
        /// This method consolidates all inline alarm setpoint checks from
        /// the engine's UpdateAnnunciators() method into a single call.
        /// </summary>
        /// <param name="inputs">Current plant parameters</param>
        /// <returns>AlarmState with all alarm flags evaluated</returns>
        public static AlarmState CheckAlarms(AlarmInputs inputs)
        {
            var alarms = new AlarmState();
            
            // ================================================================
            // PRESSURIZER ALARMS
            // ================================================================
            
            // PZR Level Low: Per NRC HRTD 10.3
            // Suppressed during solid plant operations — level is always 100%
            alarms.PZRLevelLow = !inputs.SolidPressurizer && (inputs.PZRLevel < PZR_LEVEL_LOW_SETPOINT);
            
            // PZR Level High: Per NRC HRTD 10.3
            // Suppressed during solid plant operations — PZR is intentionally water-solid (100%)
            // Per NRC HRTD 19.2.1: alarm setpoint only active after bubble formation
            alarms.PZRLevelHigh = !inputs.SolidPressurizer && (inputs.PZRLevel > PZR_LEVEL_HIGH_SETPOINT);
            
            // Steam Bubble OK: Bubble exists and level in safe operating range
            alarms.SteamBubbleOK = inputs.BubbleFormed && 
                (inputs.PZRLevel > PZR_LEVEL_BUBBLE_MIN && 
                 inputs.PZRLevel < PZR_LEVEL_BUBBLE_MAX);
            
            // ================================================================
            // RCS ALARMS
            // ================================================================
            
            // RCS Flow Low: No forced circulation
            alarms.RCSFlowLow = (inputs.RCPCount == 0);
            
            // Pressure Low: Per NRC HRTD - minimum for RCP operation
            alarms.PressureLow = (inputs.Pressure < PRESSURE_LOW_SETPOINT);
            
            // Pressure High: Approaching safety valve setpoint
            alarms.PressureHigh = (inputs.Pressure > PRESSURE_HIGH_SETPOINT);

            // SG secondary pressure high: approaching secondary relief domain.
            alarms.SGSecondaryPressureHigh =
                (inputs.SGSecondaryPressure_psia > SG_SECONDARY_PRESSURE_HIGH_SETPOINT_PSIA);
            
            // ================================================================
            // SUBCOOLING MARGIN ALARMS (SMM)
            // Per NRC HRTD Section 3.4 - Subcooling Margin Monitor
            // ================================================================
            
            // Subcooling Low: Below Tech Spec minimum margin
            alarms.SubcoolingLow = (inputs.Subcooling < SUBCOOLING_LOW_SETPOINT);
            
            // SMM Low Margin: Approaching saturation
            alarms.SMMLowMargin = (inputs.Subcooling < SMM_LOW_MARGIN_SETPOINT && 
                                   inputs.Subcooling > SMM_NO_MARGIN_SETPOINT);
            
            // SMM No Margin: At or past saturation (potential voiding)
            alarms.SMMNoMargin = (inputs.Subcooling <= SMM_NO_MARGIN_SETPOINT);
            
            // ================================================================
            // RVLIS ALARMS
            // Per NRC HRTD Section 3.3 - RVLIS
            // ================================================================
            
            // RVLIS Level Low: Only valid when RCPs off (full range valid)
            alarms.RVLISLevelLow = (inputs.RVLISFullValid && 
                                    inputs.RVLISFull < RVLIS_LEVEL_LOW_SETPOINT);
            
            // ================================================================
            // STATUS INDICATORS
            // ================================================================
            
            // Heatup in progress: Significant positive heatup rate
            alarms.HeatupInProgress = (inputs.HeatupRate > HEATUP_IN_PROGRESS_RATE);
            
            // Mode permissive: Conditions allow mode change operations
            // Requires steam bubble, adequate subcooling, and minimum pressure
            alarms.ModePermissive = (alarms.SteamBubbleOK && 
                                     !alarms.SubcoolingLow && 
                                     inputs.Pressure >= PRESSURE_LOW_SETPOINT);
            
            // ================================================================
            // EQUIPMENT STATUS
            // ================================================================
            
            // CCW always running during heatup
            alarms.CCWRunning = true;
            
            // Seal injection OK: Adequate flow for running RCPs
            alarms.SealInjectionOK = (inputs.RCPCount == 0) || 
                (inputs.SealInjection >= inputs.RCPCount * MIN_SEAL_INJECTION_PER_RCP);
            
            // CVCS active indicators
            alarms.ChargingActive = (inputs.ChargingFlow > 0.1f);
            alarms.LetdownActive = (inputs.LetdownFlow > 0.1f);
            
            return alarms;
        }
        
        /// <summary>
        /// Get a summary string of active alarms for logging.
        /// </summary>
        public static string GetActiveAlarmSummary(AlarmState alarms)
        {
            var active = new System.Collections.Generic.List<string>();
            
            if (alarms.PZRLevelLow) active.Add("PZR LVL LO");
            if (alarms.PZRLevelHigh) active.Add("PZR LVL HI");
            if (alarms.PressureLow) active.Add("PRESS LO");
            if (alarms.PressureHigh) active.Add("PRESS HI");
            if (alarms.SGSecondaryPressureHigh) active.Add("SG PRESS HI");
            if (alarms.SubcoolingLow) active.Add("SUBCOOL LO");
            if (alarms.SMMLowMargin) active.Add("SMM LO MARGIN");
            if (alarms.SMMNoMargin) active.Add("SMM NO MARGIN");
            if (alarms.RVLISLevelLow) active.Add("RVLIS LO");
            if (alarms.RCSFlowLow) active.Add("RCS FLOW LO");
            
            return active.Count > 0 ? string.Join(", ", active) : "NO ALARMS";
        }
    }
}
