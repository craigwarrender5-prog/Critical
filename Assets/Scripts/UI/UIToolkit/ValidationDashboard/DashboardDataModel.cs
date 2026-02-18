// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard
// DashboardDataModel.cs — Data Binding Model
// ============================================================================
//
// PURPOSE:
//   Provides a clean data model for dashboard elements to bind to.
//   Reads from HeatupSimEngine and exposes properties in a format
//   optimized for UI consumption.
//
// ARCHITECTURE:
//   HeatupSimEngine → DashboardDataModel → UI Elements
//   
//   The model is refreshed at 5Hz by the controller. UI elements
//   read from this model rather than directly from the engine.
//
// VERSION: 1.0.0
// DATE: 2026-02-18
// CS: CS-0127 Stage 1
// ============================================================================

using System;
using UnityEngine;
using Critical.Validation;
using Critical.Physics;

namespace Critical.UI.UIToolkit.ValidationDashboard
{
    /// <summary>
    /// Data model for the Validation Dashboard. Provides cleaned and 
    /// formatted data from HeatupSimEngine for UI binding.
    /// </summary>
    public class DashboardDataModel
    {
        // ====================================================================
        // RCS TEMPERATURES
        // ====================================================================
        
        /// <summary>Average RCS temperature (°F).</summary>
        public float T_avg { get; private set; }
        
        /// <summary>Hot leg temperature (°F).</summary>
        public float T_hot { get; private set; }
        
        /// <summary>Cold leg temperature (°F).</summary>
        public float T_cold { get; private set; }
        
        /// <summary>RCS bulk temperature (°F).</summary>
        public float T_rcs { get; private set; }
        
        /// <summary>Heatup rate (°F/hr).</summary>
        public float HeatupRate { get; private set; }
        
        // ====================================================================
        // PRESSURE
        // ====================================================================
        
        /// <summary>RCS pressure (psig).</summary>
        public float Pressure_psig { get; private set; }
        
        /// <summary>Pressure rate of change (psi/min).</summary>
        public float PressureRate { get; private set; }
        
        /// <summary>Subcooling margin (°F).</summary>
        public float Subcooling { get; private set; }
        
        // ====================================================================
        // PRESSURIZER
        // ====================================================================
        
        /// <summary>Pressurizer level (%).</summary>
        public float PZR_Level { get; private set; }
        
        /// <summary>Pressurizer water temperature (°F).</summary>
        public float PZR_Temperature { get; private set; }
        
        /// <summary>Saturation temperature at current pressure (°F).</summary>
        public float T_sat { get; private set; }
        
        /// <summary>Pressurizer water volume (ft³).</summary>
        public float PZR_WaterVolume { get; private set; }
        
        /// <summary>Pressurizer steam volume (ft³).</summary>
        public float PZR_SteamVolume { get; private set; }
        
        /// <summary>Pressurizer heater power (kW).</summary>
        public float PZR_HeaterPower { get; private set; }
        
        /// <summary>Pressurizer spray flow (gpm).</summary>
        public float PZR_SprayFlow { get; private set; }
        
        /// <summary>Surge line flow (gpm, positive = insurge).</summary>
        public float PZR_SurgeFlow { get; private set; }
        
        /// <summary>True if pressurizer has a steam bubble.</summary>
        public bool HasBubble { get; private set; }
        
        /// <summary>Bubble formation phase description.</summary>
        public string BubblePhase { get; private set; }
        
        // ====================================================================
        // RCP STATUS
        // ====================================================================
        
        /// <summary>Number of running RCPs (0-4).</summary>
        public int RCP_Count { get; private set; }
        
        /// <summary>RCP heat addition (MW).</summary>
        public float RCP_Heat { get; private set; }
        
        /// <summary>Individual RCP running status.</summary>
        public bool[] RCP_Running { get; private set; } = new bool[4];
        
        // ====================================================================
        // CVCS
        // ====================================================================
        
        /// <summary>Charging flow (gpm).</summary>
        public float ChargingFlow { get; private set; }
        
        /// <summary>Letdown flow (gpm).</summary>
        public float LetdownFlow { get; private set; }
        
        /// <summary>Net CVCS flow (gpm, positive = charging > letdown).</summary>
        public float NetCVCSFlow => ChargingFlow - LetdownFlow;
        
        /// <summary>VCT level (%).</summary>
        public float VCT_Level { get; private set; }
        
        /// <summary>Mass conservation error (lbm).</summary>
        public float MassError { get; private set; }
        
        /// <summary>BRS (Boron Recovery System) active.</summary>
        public bool BRS_Active { get; private set; }
        
        // ====================================================================
        // STEAM GENERATORS / RHR
        // ====================================================================
        
        /// <summary>SG secondary pressure (psia).</summary>
        public float SG_SecondaryPressure { get; private set; }
        
        /// <summary>SG heat transfer (MW).</summary>
        public float SG_HeatTransfer { get; private set; }
        
        /// <summary>SG boiling active.</summary>
        public bool SG_BoilingActive { get; private set; }
        
        /// <summary>RHR net heat removal (MW).</summary>
        public float RHR_NetHeat { get; private set; }
        
        /// <summary>RHR system active.</summary>
        public bool RHR_Active { get; private set; }
        
        // ====================================================================
        // CONDENSER / SECONDARY
        // ====================================================================
        
        /// <summary>Condenser vacuum (in Hg).</summary>
        public float CondenserVacuum { get; private set; }
        
        /// <summary>C-9 permissive available.</summary>
        public bool C9_Available { get; private set; }
        
        /// <summary>Steam dump permitted (P-12).</summary>
        public bool SteamDumpPermitted { get; private set; }
        
        /// <summary>Hotwell level (%).</summary>
        public float HotwellLevel { get; private set; }
        
        /// <summary>Steam dump heat rejection (MW).</summary>
        public float SteamDumpHeat { get; private set; }
        
        // ====================================================================
        // HZP PROGRESS
        // ====================================================================
        
        /// <summary>HZP progress percentage (0-100).</summary>
        public float HZP_Progress { get; private set; }
        
        /// <summary>HZP conditions stable.</summary>
        public bool HZP_Stable { get; private set; }
        
        /// <summary>Heater PID output (0-1).</summary>
        public float HeaterPIDOutput { get; private set; }
        
        // ====================================================================
        // SIMULATION STATE
        // ====================================================================
        
        /// <summary>Current plant operating mode.</summary>
        public string PlantMode { get; private set; }
        
        /// <summary>Simulation time (seconds).</summary>
        public float SimTime { get; private set; }
        
        /// <summary>Time acceleration index (0-4).</summary>
        public int TimeAccelIndex { get; private set; }
        
        /// <summary>Time acceleration multiplier.</summary>
        public float TimeAccelMultiplier { get; private set; }
        
        /// <summary>Active alarm count.</summary>
        public int AlarmCount { get; private set; }
        
        /// <summary>Timestamp of last update.</summary>
        public DateTime LastUpdate { get; private set; }
        
        // ====================================================================
        // UPDATE METHOD
        // ====================================================================
        
        /// <summary>
        /// Refresh all data from the engine. Called at 5Hz by the controller.
        /// </summary>
        public void UpdateFromEngine(HeatupSimEngine engine)
        {
            if (engine == null) return;
            
            // RCS Temperatures
            T_avg = engine.T_avg;
            T_hot = engine.T_hot;
            T_cold = engine.T_cold;
            T_rcs = engine.T_rcs;
            HeatupRate = engine.heatupRate;
            
            // Pressure
            Pressure_psig = engine.pressure;
            PressureRate = engine.pressureRate;
            Subcooling = engine.subcooling;
            
            // Pressurizer
            PZR_Level = engine.pzrLevel;
            PZR_Temperature = engine.T_pzr;
            T_sat = engine.T_sat;
            PZR_WaterVolume = engine.pzrWaterVolume;
            PZR_SteamVolume = engine.pzrSteamVolume;
            PZR_HeaterPower = engine.pzrHeaterPower;
            PZR_SprayFlow = engine.sprayFlow_GPM;
            PZR_SurgeFlow = engine.surgeFlow;
            HasBubble = !engine.solidPressurizer; // Has bubble when NOT solid
            BubblePhase = engine.bubblePhase.ToString();
            
            // RCPs
            RCP_Count = engine.rcpCount;
            RCP_Heat = engine.rcpHeat;
            for (int i = 0; i < 4; i++)
            {
                RCP_Running[i] = (i < engine.rcpCount);
            }
            
            // CVCS
            ChargingFlow = engine.chargingFlow;
            LetdownFlow = engine.letdownFlow;
            VCT_Level = engine.vctState.Level;
            MassError = engine.massConservationError;
            BRS_Active = engine.brsState.ProcessingActive;
            
            // SG/RHR
            SG_SecondaryPressure = engine.sgSecondaryPressure_psia;
            SG_HeatTransfer = engine.sgHeatTransfer_MW;
            SG_BoilingActive = engine.sgBoilingActive;
            RHR_NetHeat = engine.rhrNetHeat_MW;
            RHR_Active = engine.rhrActive;
            
            // Condenser
            CondenserVacuum = engine.condenserVacuum_inHg;
            C9_Available = engine.condenserC9Available;
            SteamDumpPermitted = engine.steamDumpPermitted;
            HotwellLevel = engine.hotwellLevel_pct;
            SteamDumpHeat = engine.steamDumpHeat_MW;
            
            // HZP
            HZP_Progress = engine.hzpProgress;
            HZP_Stable = engine.hzpStable;
            HeaterPIDOutput = engine.heaterPIDOutput;
            
            // Simulation State
            PlantMode = GetPlantModeString(engine.plantMode);
            SimTime = engine.simTime;
            TimeAccelIndex = engine.currentSpeedIndex;
            TimeAccelMultiplier = TimeAcceleration.SpeedSteps[Mathf.Clamp(engine.currentSpeedIndex, 0, TimeAcceleration.SpeedSteps.Length - 1)];
            
            // Count active alarms (placeholder - implement based on alarm system)
            AlarmCount = CountActiveAlarms(engine);
            
            LastUpdate = DateTime.Now;
        }
        
        /// <summary>
        /// Get plant mode display string from mode index.
        /// </summary>
        private string GetPlantModeString(int modeIndex)
        {
            return modeIndex switch
            {
                0 => "COLD_SHUTDOWN",
                1 => "SOLID_HEATUP",
                2 => "BUBBLE_FORMATION",
                3 => "PRESSURIZATION",
                4 => "RCP_STARTUP",
                5 => "BULK_HEATUP",
                6 => "APPROACH_HZP",
                7 => "HZP_STABLE",
                _ => "UNKNOWN"
            };
        }
        
        /// <summary>
        /// Count active alarms from the engine state.
        /// </summary>
        private int CountActiveAlarms(HeatupSimEngine engine)
        {
            int count = 0;
            
            // Check for alarm conditions
            if (Subcooling < 25f) count++;  // Low subcooling warning
            if (Subcooling < 15f) count++;  // Low subcooling alarm (counts twice for severity)
            if (HeatupRate > 100f) count++; // High heatup rate
            if (PZR_Level < 20f || PZR_Level > 80f) count++; // PZR level abnormal
            if (Pressure_psig < 300f && T_rcs > 212f) count++; // Low pressure at temp
            
            return count;
        }
        
        // ====================================================================
        // FORMATTED ACCESSORS
        // ====================================================================
        
        /// <summary>Get formatted simulation time string (HH:MM:SS).</summary>
        public string GetSimTimeFormatted()
        {
            TimeSpan ts = TimeSpan.FromSeconds(SimTime);
            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
        
        /// <summary>Get time acceleration display string.</summary>
        public string GetTimeAccelFormatted()
        {
            if (TimeAccelMultiplier <= 0f)
                return "PAUSED";
            if (TimeAccelMultiplier == 1f)
                return "1×";
            return $"{TimeAccelMultiplier:F0}×";
        }
        
        /// <summary>Get plant mode display string.</summary>
        public string GetPlantModeFormatted()
        {
            // Clean up enum name for display
            string mode = PlantMode ?? "UNKNOWN";
            return mode.Replace("_", " ").ToUpperInvariant();
        }
        
        /// <summary>Get bubble status string.</summary>
        public string GetBubbleStatusFormatted()
        {
            if (HasBubble)
                return "BUBBLE";
            return "SOLID";
        }
    }
}
