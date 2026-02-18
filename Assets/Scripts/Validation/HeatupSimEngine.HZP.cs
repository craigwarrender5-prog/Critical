// ============================================================================
// CRITICAL: Master the Atom - Simulation Engine (HZP Partial)
// HeatupSimEngine.HZP.cs - Hot Zero Power Stabilization and Handoff
// ============================================================================
//
// PURPOSE:
//   v1.1.0 Stage 6 â€” Integration of HZP stabilization systems:
//     - Steam Dump Controller: Removes excess RCP heat to maintain T_avg
//     - HZP Stabilization State Machine: Manages transition to stable HZP
//     - Heater PID Controller: Smooth pressure control at HZP
//     - Handoff to Reactor Operations: Clean transition for startup
//
// PHYSICS MODULES USED:
//   - SteamDumpController    : Steam pressure control, heat removal
//   - HZPStabilizationController : State machine for HZP approach
//   - CVCSController.HeaterPID : PID-based pressure control
//   - SGSecondaryThermal     : SG secondary steaming detection
//   - PlantConstants.SteamDump : Steam dump setpoints and parameters
//
// ARCHITECTURE:
//   Partial class of HeatupSimEngine. This file owns:
//     - InitializeHZPSystems() â€” Called when approaching HZP
//     - UpdateHZPSystems(dt) â€” Called each timestep during HZP approach
//     - InitiateReactorStartup() â€” Called by Reactor Operator GUI
//     - GetStartupReadiness() â€” Returns prerequisites check
//
// SOURCES:
//   - NRC HRTD 19.0 â€” Plant Operations at HZP
//   - NRC HRTD 10.2 â€” Pressurizer Pressure Control
//   - NRC HRTD 11.2 â€” Steam Dump System
//
// GOLD STANDARD: Yes
// VERSION: 1.1.0 Stage 6
// ============================================================================

using UnityEngine;
using System;
using Critical.Physics;


namespace Critical.Validation
{

public partial class HeatupSimEngine
{
    // ========================================================================
    // HZP SYSTEM INITIALIZATION
    // ========================================================================
    
    /// <summary>
    /// Initialize HZP stabilization systems.
    /// Called when T_avg approaches HZP approach temperature (550Â°F).
    /// </summary>
    void InitializeHZPSystems()
    {
        // Initialize Steam Dump Controller
        steamDumpState = SteamDumpController.Initialize();
        steamDumpHeat_MW = 0f;
        steamDumpActive = false;
        
        // Initialize HZP Stabilization State Machine
        hzpState = HZPStabilizationController.Initialize();
        hzpStable = false;
        hzpReadyForStartup = false;
        hzpProgress = 0f;
        
        // Initialize Heater PID Controller
        heaterPIDState = CVCSController.InitializeHeaterPID(pressure - 14.7f);  // Convert to psig
        heaterPIDActive = false;
        heaterPIDOutput = 0.5f;
        
        // Initialize secondary steaming state
        sgSteaming = false;
        sgSecondaryPressure_psig = 0f;
        
        // Handoff state
        handoffInitiated = false;
        
        LogEvent(EventSeverity.ACTION, "HZP SYSTEMS INITIALIZED - Approaching Hot Zero Power");
        Debug.Log($"[T+{simTime:F2}hr] HZP Systems initialized at T_avg={T_avg:F1}Â°F");
    }
    
    // Flag to track if HZP systems have been initialized
    private bool hzpSystemsInitialized = false;
    private bool hzpLifecycleActive = false;

    bool ShouldHZPSystemsBeActive()
    {
        return (T_avg >= PlantConstants.SteamDump.HZP_APPROACH_TEMP_F) &&
               bubbleFormed &&
               !solidPressurizer &&
               (rcpCount >= 4);
    }

    void ResetHZPSystemsLifecycle()
    {
        hzpSystemsInitialized = false;
        hzpLifecycleActive = false;
        hzpStable = false;
        hzpReadyForStartup = false;
        hzpProgress = 0f;
        handoffInitiated = false;
        heaterPIDActive = false;
        heaterPIDOutput = 0.5f;
        steamDumpHeat_MW = 0f;
        steamDumpActive = false;
        sgSteaming = false;
        sgSecondaryPressure_psig = 0f;
        steamPressure_psig = 0f;
        startupPrereqs = default;

        // Ensure run start cannot inherit HZP state from a prior session.
        steamDumpState = SteamDumpController.Initialize();
        hzpState = HZPStabilizationController.Initialize();
        heaterPIDState = CVCSController.InitializeHeaterPID(pressure - 14.7f);
    }

    void UpdateHZPLifecycle()
    {
        bool shouldBeActive = ShouldHZPSystemsBeActive();

        if (shouldBeActive && !hzpLifecycleActive)
        {
            InitializeHZPSystems();
            hzpSystemsInitialized = true;
            hzpLifecycleActive = true;
            return;
        }

        if (!shouldBeActive && hzpLifecycleActive)
        {
            hzpLifecycleActive = false;
            LogEvent(EventSeverity.INFO, $"HZP systems standby - T_avg={T_avg:F1}Â°F, RCPs={rcpCount}");
        }
    }
    
    // ========================================================================
    // HZP SYSTEM UPDATE â€” Called each timestep
    // ========================================================================
    
    /// <summary>
    /// Update HZP stabilization systems for one timestep.
    /// Called from StepSimulation when T_avg >= HZP approach temperature.
    /// </summary>
    /// <param name="dt_hr">Timestep in hours</param>
    void UpdateHZPSystems(float dt_hr)
    {
        bool shouldBeActive = ShouldHZPSystemsBeActive();
        if (!shouldBeActive || !hzpSystemsInitialized)
        {
            return;
        }
        
        float dt_sec = dt_hr * 3600f;
        float pressure_psig = pressure - 14.7f;
        
        // ================================================================
        // 1. SG SECONDARY STEAMING CHECK
        // v5.0.0 Stage 3: Use multi-node SG model pressure instead of
        // the old lumped SGSecondaryThermal model. The multi-node model
        // tracks regime (Subcooled/Boiling/SteamDump) and provides the
        // correct secondary pressure from the rate-limited open-system model.
        // ================================================================
        // sgSecondaryPressure_psia is already set from the SGMultiNodeThermal
        // result in StepSimulation() (Regime 1/2/3 paths).
        sgSecondaryPressure_psig = sgSecondaryPressure_psia - 14.7f;
        sgSteaming = sgMultiNodeState.CurrentRegime != SGThermalRegime.Subcooled;
        
        // Steam header pressure for steam dump controller
        steamPressure_psig = sgSecondaryPressure_psig;
        
        // ================================================================
        // 2. STEAM DUMP CONTROLLER
        // Auto-enable when approaching HZP and SG is steaming
        // IP-0046 (CS-0115): Gated by startup permissives (C-9/P-12)
        // ================================================================
        if (SteamDumpController.ShouldAutoEnable(T_avg, steamPressure_psig, in permissiveState) &&
            steamDumpState.Mode == SteamDumpMode.OFF)
        {
            SteamDumpController.EnableSteamPressureMode(ref steamDumpState, simTime);
            LogEvent(EventSeverity.ACTION, $"STEAM DUMP AUTO-ENABLED at T_avg={T_avg:F1}Â°F, P_steam={steamPressure_psig:F0} psig");
        }
        
        // Update steam dump controller with permissive gating (IP-0046 CS-0115)
        steamDumpHeat_MW = SteamDumpController.Update(
            ref steamDumpState,
            steamPressure_psig,
            T_avg,
            dt_hr,
            steamDumpPermitted);
        
        steamDumpActive = steamDumpState.IsActive && steamDumpHeat_MW > 0.1f;
        
        // ================================================================
        // 3. HEATER PID CONTROLLER (at HZP)
        // v4.4.0: PID activation and update now handled in Section 1B of
        // StepSimulation() via the PRESSURIZE_AUTO â†’ AUTOMATIC_PID mode
        // transition at 2200 psia. This section is retained as a fallback
        // for edge cases where HZP systems initialize before Section 1B
        // has triggered the transition (should not normally occur).
        // ================================================================
        if (!heaterPIDActive)
        {
            bool nearHZPPressure = Math.Abs(pressure_psig - PlantConstants.PZR_OPERATING_PRESSURE_PSIG) < 100f;
            if (nearHZPPressure)
            {
                // Fallback: Activate PID if Section 1B hasn't already
                heaterPIDActive = true;
                currentHeaterMode = HeaterMode.AUTOMATIC_PID;
                heaterPIDState = CVCSController.InitializeHeaterPID(pressure_psig);
                CVCSController.SetHeaterPIDActive(ref heaterPIDState, true);
                LogEvent(EventSeverity.ACTION, $"HEATER PID CONTROL ACTIVATED (HZP fallback) at P={pressure_psig:F0} psig");
            }
        }
        // Note: PID update runs in Section 1B each timestep.
        // No duplicate update here to prevent double-stepping the PID.
        
        // ================================================================
        // 4. HZP STABILIZATION STATE MACHINE
        // Track progress toward stable HZP conditions
        // ================================================================
        bool stateChanged = HZPStabilizationController.Update(
            ref hzpState,
            T_avg,
            pressure_psig,
            pzrLevel,
            steamPressure_psig,
            rcpCount,
            simTime,
            dt_sec);
        
        if (stateChanged)
        {
            LogEvent(EventSeverity.ACTION, $"HZP STATE: {HZPStabilizationController.GetStateString(hzpState)}");
        }
        
        hzpStable = HZPStabilizationController.IsStable(hzpState);
        hzpReadyForStartup = HZPStabilizationController.IsReadyForHandoff(hzpState);
        hzpProgress = HZPStabilizationController.GetStabilizationProgress(hzpState);
        
        // ================================================================
        // 5. UPDATE STATUS MESSAGE
        // ================================================================
        if (hzpStable)
        {
            statusMessage = "HZP STABLE - READY FOR REACTOR STARTUP";
            heatupPhaseDesc = "HOT ZERO POWER - STABLE";
        }
        else if (hzpState.State == HZPState.STABILIZING)
        {
            float timeToStable = HZPStabilizationController.GetTimeToStable(hzpState);
            if (timeToStable > 0f)
            {
                statusMessage = $"HZP STABILIZING - {timeToStable:F0}s to stable";
            }
            else
            {
                statusMessage = $"HZP STABILIZING - {hzpProgress:F0}%";
            }
            heatupPhaseDesc = "APPROACHING HZP - STABILIZING";
        }
        else if (hzpState.State == HZPState.APPROACHING)
        {
            statusMessage = $"APPROACHING HZP - T_avg={T_avg:F1}Â°F â†’ 557Â°F";
            heatupPhaseDesc = "APPROACHING HOT ZERO POWER";
        }
        
        // ================================================================
        // 6. UPDATE STARTUP PREREQUISITES
        // ================================================================
        startupPrereqs = HZPStabilizationController.CheckStartupPrerequisites(
            hzpState,
            T_avg,
            pressure_psig,
            pzrLevel,
            rcpCount,
            rcsBoronConcentration);
    }
    
    // ========================================================================
    // REACTOR STARTUP HANDOFF â€” Called by Reactor Operator GUI
    // ========================================================================
    
    /// <summary>
    /// Initiate handoff to Reactor Operations.
    /// Called when operator clicks "BEGIN REACTOR STARTUP" button.
    /// </summary>
    /// <returns>True if handoff was successful</returns>
    public bool InitiateReactorStartup()
    {
        if (!hzpReadyForStartup)
        {
            LogEvent(EventSeverity.ALERT, "REACTOR STARTUP BLOCKED - HZP not stable");
            Debug.LogWarning($"[T+{simTime:F2}hr] Reactor startup blocked - HZP state: {hzpState.State}");
            return false;
        }
        
        // Check all prerequisites
        if (!startupPrereqs.AllMet)
        {
            string missing = "";
            if (!startupPrereqs.HZPStable) missing += "HZP not stable, ";
            if (!startupPrereqs.TemperatureOK) missing += "T_avg out of band, ";
            if (!startupPrereqs.PressureOK) missing += "Pressure out of band, ";
            if (!startupPrereqs.LevelOK) missing += "PZR level out of band, ";
            if (!startupPrereqs.RCPsOK) missing += "Not all RCPs running, ";
            if (!startupPrereqs.BoronOK) missing += "Boron too low, ";
            
            LogEvent(EventSeverity.ALERT, $"REACTOR STARTUP BLOCKED - Prerequisites not met: {missing}");
            return false;
        }
        
        // Initiate handoff
        bool success = HZPStabilizationController.InitiateHandoff(ref hzpState, simTime);
        
        if (success)
        {
            handoffInitiated = true;
            LogEvent(EventSeverity.ACTION, "=== REACTOR STARTUP INITIATED ===");
            LogEvent(EventSeverity.INFO, $"  T_avg={T_avg:F1}Â°F, P={pressure - 14.7f:F0} psig, Level={pzrLevel:F1}%");
            LogEvent(EventSeverity.INFO, $"  All 4 RCPs running, Boron={rcsBoronConcentration:F0} ppm");
            LogEvent(EventSeverity.INFO, "  Handoff to ReactorController ready");
            
            Debug.Log($"[T+{simTime:F2}hr] === REACTOR STARTUP INITIATED === T_avg={T_avg:F1}Â°F");
        }
        
        return success;
    }
    
    /// <summary>
    /// Get detailed startup readiness information.
    /// Called by Reactor Operator GUI to display prerequisites panel.
    /// </summary>
    public StartupPrerequisites GetStartupReadiness()
    {
        return startupPrereqs;
    }
    
    /// <summary>
    /// Get HZP status string for display.
    /// </summary>
    public string GetHZPStatusString()
    {
        if (!hzpSystemsInitialized)
        {
            return $"HEATUP IN PROGRESS - T_avg={T_avg:F1}Â°F";
        }
        
        return hzpState.StatusMessage;
    }
    
    /// <summary>
    /// Get detailed HZP status for display.
    /// </summary>
    public string GetHZPDetailedStatus()
    {
        if (!hzpSystemsInitialized)
        {
            float tempToHZP = PlantConstants.SteamDump.HZP_APPROACH_TEMP_F - T_avg;
            if (tempToHZP > 0f && heatupRate > 0.5f)
            {
                float timeToHZP = tempToHZP / heatupRate;
                return $"~{timeToHZP:F1} hr to HZP approach";
            }
            return "Heatup in progress";
        }
        
        return hzpState.DetailedStatus;
    }
    
    /// <summary>
    /// Check if HZP systems are active and controlling.
    /// </summary>
    public bool IsHZPActive()
    {
        return hzpSystemsInitialized && hzpState.State != HZPState.INACTIVE;
    }
    
    /// <summary>
    /// Get steam dump status for display.
    /// </summary>
    public string GetSteamDumpStatus()
    {
        if (!hzpSystemsInitialized || steamDumpState.Mode == SteamDumpMode.OFF)
        {
            return "OFF";
        }
        
        return steamDumpState.StatusMessage;
    }
    
    /// <summary>
    /// Get heater PID status for display.
    /// </summary>
    public string GetHeaterPIDStatus()
    {
        if (!heaterPIDActive)
        {
            return currentHeaterMode.ToString();
        }
        
        return heaterPIDState.StatusMessage;
    }
}


}

