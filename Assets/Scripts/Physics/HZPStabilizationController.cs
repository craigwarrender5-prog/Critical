// ============================================================================
// CRITICAL: Master the Atom - HZP Stabilization Controller
// HZPStabilizationController.cs - Hot Zero Power Stabilization State Machine
// ============================================================================
//
// PURPOSE:
//   Manages the automatic transition from heatup to stable Hot Zero Power (HZP)
//   conditions, and coordinates handoff to the Reactor Operations system.
//
//   At HZP (Mode 3 - Hot Standby), the plant is at normal operating temperature
//   and pressure with all 4 RCPs running, but the reactor is subcritical.
//   The steam dump system maintains RCS temperature by removing RCP heat.
//
// STATE MACHINE:
//   INACTIVE     → During heatup (T_avg < 550°F)
//   APPROACHING  → T_avg > 550°F, steam dump enabled
//   STABILIZING  → T_avg near setpoint, fine-tuning parameters
//   STABLE       → All parameters within tolerance for 5+ minutes
//   HANDOFF_READY → Awaiting operator action to begin reactor startup
//
// TARGET CONDITIONS (Mode 3 - Hot Standby):
//   - T_avg: 557°F ± 2°F
//   - RCS Pressure: 2235 psig ± 10 psi
//   - PZR Level: 60% ± 5%
//   - Steam Header Pressure: 1092 psig ± 20 psi
//   - All 4 RCPs running at 100%
//
// SOURCES:
//   - NRC HRTD 19.0 — Plant Operations (ML11223A342)
//   - NRC HRTD 19.2.2 — HZP conditions and reactor startup prerequisites
//   - Westinghouse 4-Loop PWR Technical Specifications
//
// UNITS:
//   Temperature: °F | Pressure: psig/psia | Level: % | Time: seconds
//
// VERSION: 1.1.0 (Stage 3)
// CLASSIFICATION: Physics — Realism Critical
// GOLD STANDARD: Yes
// ============================================================================

using System;
using UnityEngine;

namespace Critical.Physics
{
    /// <summary>
    /// HZP stabilization state machine states.
    /// </summary>
    public enum HZPState
    {
        /// <summary>Controller inactive during heatup (T_avg &lt; 550°F)</summary>
        INACTIVE,
        
        /// <summary>Approaching HZP (T_avg > 550°F), steam dump enabled</summary>
        APPROACHING,
        
        /// <summary>Near HZP setpoints, fine-tuning control parameters</summary>
        STABILIZING,
        
        /// <summary>All parameters within tolerance for required duration</summary>
        STABLE,
        
        /// <summary>Ready for handoff to Reactor Operations</summary>
        HANDOFF_READY
    }
    
    /// <summary>
    /// HZP stabilization controller state for persistence between timesteps.
    /// </summary>
    public struct HZPStabilizationState
    {
        /// <summary>Current state machine state</summary>
        public HZPState State;
        
        /// <summary>Time spent in current state (seconds)</summary>
        public float StateTime_sec;
        
        /// <summary>Time with all parameters stable (seconds)</summary>
        public float StableTimer_sec;
        
        /// <summary>Simulation time when state was entered (hours)</summary>
        public float StateEntryTime_hr;
        
        /// <summary>Current T_avg setpoint (°F)</summary>
        public float TavgSetpoint_F;
        
        /// <summary>Current pressure setpoint (psig)</summary>
        public float PressureSetpoint_psig;
        
        /// <summary>Current PZR level setpoint (%)</summary>
        public float LevelSetpoint_pct;
        
        /// <summary>T_avg error (actual - setpoint) in °F</summary>
        public float TavgError_F;
        
        /// <summary>Pressure error (actual - setpoint) in psi</summary>
        public float PressureError_psi;
        
        /// <summary>PZR level error (actual - setpoint) in %</summary>
        public float LevelError_pct;
        
        /// <summary>True if T_avg is within tolerance</summary>
        public bool TavgOK;
        
        /// <summary>True if pressure is within tolerance</summary>
        public bool PressureOK;
        
        /// <summary>True if PZR level is within tolerance</summary>
        public bool LevelOK;
        
        /// <summary>True if all 4 RCPs are running</summary>
        public bool AllRCPsRunning;
        
        /// <summary>Status message for display</summary>
        public string StatusMessage;
        
        /// <summary>Detailed status for logging</summary>
        public string DetailedStatus;
    }
    
    /// <summary>
    /// HZP Stabilization Controller.
    /// 
    /// Manages the transition from heatup to stable Hot Zero Power conditions
    /// and coordinates handoff to the Reactor Operations system.
    /// 
    /// Per NRC HRTD 19.0: "The RCS temperature remains constant at 557°F, the
    /// steam dumps removing any excess energy that would tend to drive the RCS
    /// temperature higher."
    /// </summary>
    public static class HZPStabilizationController
    {
        // ============================================================================
        // CONSTANTS — HZP Target Conditions
        // ============================================================================
        
        #region Setpoints
        
        /// <summary>T_avg setpoint at HZP (°F). Source: NRC HRTD 19.0</summary>
        public const float TAVG_SETPOINT_F = 557f;
        
        /// <summary>T_avg tolerance band (°F)</summary>
        public const float TAVG_TOLERANCE_F = 2f;
        
        /// <summary>RCS pressure setpoint at HZP (psig). Source: NRC HRTD 10.2</summary>
        public const float PRESSURE_SETPOINT_PSIG = PlantConstants.PZR_OPERATING_PRESSURE_PSIG;
        
        /// <summary>RCS pressure tolerance band (psi)</summary>
        public const float PRESSURE_TOLERANCE_PSI = 10f;
        
        /// <summary>PZR level setpoint at HZP (%). Source: No-load level program</summary>
        public const float LEVEL_SETPOINT_PCT = 60f;
        
        /// <summary>PZR level tolerance band (%)</summary>
        public const float LEVEL_TOLERANCE_PCT = 5f;
        
        /// <summary>Steam header pressure setpoint (psig). Source: NRC HRTD 19.0</summary>
        public const float STEAM_PRESSURE_SETPOINT_PSIG = 1092f;
        
        /// <summary>Steam header pressure tolerance (psi)</summary>
        public const float STEAM_PRESSURE_TOLERANCE_PSI = 20f;
        
        #endregion
        
        #region State Machine Thresholds
        
        /// <summary>Temperature threshold to enter APPROACHING state (°F)</summary>
        public const float APPROACH_TEMP_F = 550f;
        
        /// <summary>Temperature band around setpoint for STABILIZING state (°F)</summary>
        public const float STABILIZING_BAND_F = 5f;
        
        /// <summary>Time required at stable conditions before declaring STABLE (seconds)</summary>
        public const float STABLE_TIME_REQUIRED_SEC = 300f;  // 5 minutes
        
        /// <summary>Minimum time in STABILIZING before transitioning (seconds)</summary>
        public const float MIN_STABILIZING_TIME_SEC = 60f;  // 1 minute
        
        #endregion
        
        // ============================================================================
        // INITIALIZATION
        // ============================================================================
        
        /// <summary>
        /// Initialize HZP stabilization controller to INACTIVE state.
        /// Called at simulation startup.
        /// </summary>
        /// <returns>Initialized controller state</returns>
        public static HZPStabilizationState Initialize()
        {
            var state = new HZPStabilizationState
            {
                State = HZPState.INACTIVE,
                StateTime_sec = 0f,
                StableTimer_sec = 0f,
                StateEntryTime_hr = 0f,
                TavgSetpoint_F = TAVG_SETPOINT_F,
                PressureSetpoint_psig = PRESSURE_SETPOINT_PSIG,
                LevelSetpoint_pct = LEVEL_SETPOINT_PCT,
                TavgError_F = 0f,
                PressureError_psi = 0f,
                LevelError_pct = 0f,
                TavgOK = false,
                PressureOK = false,
                LevelOK = false,
                AllRCPsRunning = false,
                StatusMessage = "HZP: INACTIVE",
                DetailedStatus = "Heatup in progress"
            };
            
            return state;
        }
        
        // ============================================================================
        // MAIN UPDATE
        // ============================================================================
        
        /// <summary>
        /// Update HZP stabilization state machine for one timestep.
        /// </summary>
        /// <param name="state">Controller state (modified in place)</param>
        /// <param name="T_avg">Current RCS average temperature (°F)</param>
        /// <param name="pressure_psig">Current RCS pressure (psig)</param>
        /// <param name="pzrLevel_pct">Current pressurizer level (%)</param>
        /// <param name="steamPressure_psig">Current steam header pressure (psig)</param>
        /// <param name="rcpCount">Number of RCPs running</param>
        /// <param name="simTime_hr">Current simulation time (hours)</param>
        /// <param name="dt_sec">Timestep in seconds</param>
        /// <returns>True if state changed this timestep</returns>
        public static bool Update(
            ref HZPStabilizationState state,
            float T_avg,
            float pressure_psig,
            float pzrLevel_pct,
            float steamPressure_psig,
            int rcpCount,
            float simTime_hr,
            float dt_sec)
        {
            HZPState previousState = state.State;
            
            // ================================================================
            // CALCULATE ERRORS AND CHECK TOLERANCES
            // ================================================================
            
            state.TavgError_F = T_avg - state.TavgSetpoint_F;
            state.PressureError_psi = pressure_psig - state.PressureSetpoint_psig;
            state.LevelError_pct = pzrLevel_pct - state.LevelSetpoint_pct;
            
            state.TavgOK = Mathf.Abs(state.TavgError_F) <= TAVG_TOLERANCE_F;
            state.PressureOK = Mathf.Abs(state.PressureError_psi) <= PRESSURE_TOLERANCE_PSI;
            state.LevelOK = Mathf.Abs(state.LevelError_pct) <= LEVEL_TOLERANCE_PCT;
            state.AllRCPsRunning = (rcpCount >= 4);
            
            bool allParamsOK = state.TavgOK && state.PressureOK && state.LevelOK;
            
            // ================================================================
            // STATE MACHINE UPDATE
            // ================================================================
            
            state.StateTime_sec += dt_sec;
            
            switch (state.State)
            {
                // ============================================================
                // INACTIVE: Waiting for approach temperature
                // ============================================================
                case HZPState.INACTIVE:
                    state.StatusMessage = "HZP: INACTIVE";
                    state.DetailedStatus = $"Heatup in progress, T_avg={T_avg:F1}°F";
                    
                    if (T_avg >= APPROACH_TEMP_F)
                    {
                        TransitionTo(ref state, HZPState.APPROACHING, simTime_hr);
                        Debug.Log($"[HZP] Transitioning to APPROACHING at T_avg={T_avg:F1}°F");
                    }
                    break;
                
                // ============================================================
                // APPROACHING: Steam dump enabled, approaching setpoint
                // ============================================================
                case HZPState.APPROACHING:
                    state.StatusMessage = "HZP: APPROACHING";
                    state.DetailedStatus = $"T_avg={T_avg:F1}°F → {TAVG_SETPOINT_F:F0}°F";
                    
                    // Check if within stabilization band
                    float tempDelta = Mathf.Abs(T_avg - TAVG_SETPOINT_F);
                    if (tempDelta <= STABILIZING_BAND_F)
                    {
                        TransitionTo(ref state, HZPState.STABILIZING, simTime_hr);
                        Debug.Log($"[HZP] Transitioning to STABILIZING at T_avg={T_avg:F1}°F");
                    }
                    break;
                
                // ============================================================
                // STABILIZING: Fine-tuning parameters
                // ============================================================
                case HZPState.STABILIZING:
                    state.StatusMessage = "HZP: STABILIZING";
                    
                    // Build detailed status showing which parameters are OK
                    string tempStatus = state.TavgOK ? "✓" : $"err={state.TavgError_F:+0.0;-0.0}°F";
                    string pressStatus = state.PressureOK ? "✓" : $"err={state.PressureError_psi:+0;-0}psi";
                    string levelStatus = state.LevelOK ? "✓" : $"err={state.LevelError_pct:+0.0;-0.0}%";
                    string rcpStatus = state.AllRCPsRunning ? "✓" : $"{rcpCount}/4";
                    
                    state.DetailedStatus = $"T:{tempStatus} P:{pressStatus} L:{levelStatus} RCP:{rcpStatus}";
                    
                    // Track stability time
                    if (allParamsOK && state.AllRCPsRunning)
                    {
                        state.StableTimer_sec += dt_sec;
                        state.StatusMessage = $"HZP: STABILIZING ({state.StableTimer_sec:F0}/{STABLE_TIME_REQUIRED_SEC:F0}s)";
                        
                        if (state.StableTimer_sec >= STABLE_TIME_REQUIRED_SEC)
                        {
                            TransitionTo(ref state, HZPState.STABLE, simTime_hr);
                            Debug.Log($"[HZP] Transitioning to STABLE after {state.StableTimer_sec:F0}s stable");
                        }
                    }
                    else
                    {
                        // Reset stability timer if any parameter goes out of band
                        if (state.StableTimer_sec > 0f)
                        {
                            Debug.Log($"[HZP] Stability timer reset: T_OK={state.TavgOK}, P_OK={state.PressureOK}, L_OK={state.LevelOK}, RCPs={rcpCount}");
                        }
                        state.StableTimer_sec = 0f;
                    }
                    
                    // Can regress to APPROACHING if temperature drifts too far
                    if (Mathf.Abs(T_avg - TAVG_SETPOINT_F) > STABILIZING_BAND_F * 2f)
                    {
                        TransitionTo(ref state, HZPState.APPROACHING, simTime_hr);
                        Debug.Log($"[HZP] Regressing to APPROACHING, T_avg drifted to {T_avg:F1}°F");
                    }
                    break;
                
                // ============================================================
                // STABLE: All parameters within tolerance
                // ============================================================
                case HZPState.STABLE:
                    state.StatusMessage = "HZP: STABLE";
                    state.DetailedStatus = $"T={T_avg:F1}°F, P={pressure_psig:F0}psig, L={pzrLevel_pct:F1}%";
                    
                    // Continue monitoring - can regress if parameters drift
                    if (!allParamsOK || !state.AllRCPsRunning)
                    {
                        TransitionTo(ref state, HZPState.STABILIZING, simTime_hr);
                        state.StableTimer_sec = 0f;
                        Debug.Log($"[HZP] Regressing to STABILIZING from STABLE");
                    }
                    break;
                
                // ============================================================
                // HANDOFF_READY: Awaiting operator action
                // ============================================================
                case HZPState.HANDOFF_READY:
                    state.StatusMessage = "HZP: READY FOR STARTUP";
                    state.DetailedStatus = "Awaiting operator command";
                    
                    // Still monitor parameters - can regress if plant drifts
                    if (!allParamsOK || !state.AllRCPsRunning)
                    {
                        TransitionTo(ref state, HZPState.STABILIZING, simTime_hr);
                        state.StableTimer_sec = 0f;
                        Debug.Log($"[HZP] Regressing to STABILIZING from HANDOFF_READY");
                    }
                    break;
            }
            
            return state.State != previousState;
        }
        
        // ============================================================================
        // STATE TRANSITIONS
        // ============================================================================
        
        /// <summary>
        /// Transition to a new state.
        /// </summary>
        private static void TransitionTo(ref HZPStabilizationState state, HZPState newState, float simTime_hr)
        {
            state.State = newState;
            state.StateEntryTime_hr = simTime_hr;
            state.StateTime_sec = 0f;
            
            // Reset stability timer on certain transitions
            if (newState == HZPState.APPROACHING || newState == HZPState.INACTIVE)
            {
                state.StableTimer_sec = 0f;
            }
        }
        
        /// <summary>
        /// Initiate handoff to Reactor Operations.
        /// Called when operator begins reactor startup from the Reactor Operator GUI.
        /// </summary>
        /// <param name="state">Controller state (modified in place)</param>
        /// <param name="simTime_hr">Current simulation time (hours)</param>
        /// <returns>True if handoff was initiated successfully</returns>
        public static bool InitiateHandoff(ref HZPStabilizationState state, float simTime_hr)
        {
            if (state.State == HZPState.STABLE)
            {
                TransitionTo(ref state, HZPState.HANDOFF_READY, simTime_hr);
                Debug.Log($"[HZP] Handoff initiated at T+{simTime_hr:F2}hr");
                return true;
            }
            else
            {
                Debug.LogWarning($"[HZP] Cannot initiate handoff - current state is {state.State}");
                return false;
            }
        }
        
        /// <summary>
        /// Force transition to STABLE state (for testing/debug).
        /// </summary>
        public static void ForceStable(ref HZPStabilizationState state, float simTime_hr)
        {
            TransitionTo(ref state, HZPState.STABLE, simTime_hr);
            state.StableTimer_sec = STABLE_TIME_REQUIRED_SEC;
            Debug.Log($"[HZP] Forced to STABLE state");
        }
        
        // ============================================================================
        // QUERY METHODS
        // ============================================================================
        
        /// <summary>
        /// Check if HZP stabilization is complete (STABLE or HANDOFF_READY).
        /// </summary>
        public static bool IsStable(HZPStabilizationState state)
        {
            return state.State == HZPState.STABLE || state.State == HZPState.HANDOFF_READY;
        }
        
        /// <summary>
        /// Check if HZP controller is active (not INACTIVE).
        /// </summary>
        public static bool IsActive(HZPStabilizationState state)
        {
            return state.State != HZPState.INACTIVE;
        }
        
        /// <summary>
        /// Check if ready for handoff to Reactor Operations.
        /// </summary>
        public static bool IsReadyForHandoff(HZPStabilizationState state)
        {
            return state.State == HZPState.STABLE || state.State == HZPState.HANDOFF_READY;
        }
        
        /// <summary>
        /// Get the state as a display string.
        /// </summary>
        public static string GetStateString(HZPStabilizationState state)
        {
            switch (state.State)
            {
                case HZPState.INACTIVE: return "INACTIVE";
                case HZPState.APPROACHING: return "APPROACHING";
                case HZPState.STABILIZING: return "STABILIZING";
                case HZPState.STABLE: return "STABLE";
                case HZPState.HANDOFF_READY: return "HANDOFF_READY";
                default: return "UNKNOWN";
            }
        }
        
        /// <summary>
        /// Get time remaining until STABLE state (estimated).
        /// Returns 0 if already stable, -1 if cannot estimate.
        /// </summary>
        public static float GetTimeToStable(HZPStabilizationState state)
        {
            if (state.State == HZPState.STABLE || state.State == HZPState.HANDOFF_READY)
                return 0f;
            
            if (state.State == HZPState.STABILIZING && state.TavgOK && state.PressureOK && state.LevelOK)
            {
                return Mathf.Max(0f, STABLE_TIME_REQUIRED_SEC - state.StableTimer_sec);
            }
            
            return -1f;  // Cannot estimate
        }
        
        /// <summary>
        /// Get percentage progress toward STABLE state.
        /// </summary>
        public static float GetStabilizationProgress(HZPStabilizationState state)
        {
            switch (state.State)
            {
                case HZPState.INACTIVE:
                    return 0f;
                    
                case HZPState.APPROACHING:
                    return 25f;
                    
                case HZPState.STABILIZING:
                    // 25-100% based on stability timer
                    float timerProgress = Mathf.Clamp01(state.StableTimer_sec / STABLE_TIME_REQUIRED_SEC);
                    return 25f + (75f * timerProgress);
                    
                case HZPState.STABLE:
                case HZPState.HANDOFF_READY:
                    return 100f;
                    
                default:
                    return 0f;
            }
        }
        
        // ============================================================================
        // PREREQUISITE CHECKS
        // ============================================================================
        
        /// <summary>
        /// Check all prerequisites for reactor startup.
        /// Returns a detailed report of what's OK and what's not.
        /// </summary>
        /// <param name="state">Current HZP state</param>
        /// <param name="T_avg">Current T_avg (°F)</param>
        /// <param name="pressure_psig">Current pressure (psig)</param>
        /// <param name="pzrLevel_pct">Current PZR level (%)</param>
        /// <param name="rcpCount">Number of RCPs running</param>
        /// <param name="boron_ppm">Current boron concentration (ppm)</param>
        /// <returns>Prerequisite check result</returns>
        public static StartupPrerequisites CheckStartupPrerequisites(
            HZPStabilizationState state,
            float T_avg,
            float pressure_psig,
            float pzrLevel_pct,
            int rcpCount,
            float boron_ppm)
        {
            var prereqs = new StartupPrerequisites();
            
            // HZP Stable
            prereqs.HZPStable = IsStable(state);
            prereqs.HZPStableStatus = prereqs.HZPStable 
                ? "HZP Stable" 
                : $"HZP State: {GetStateString(state)}";
            
            // Temperature
            prereqs.TemperatureOK = Mathf.Abs(T_avg - TAVG_SETPOINT_F) <= TAVG_TOLERANCE_F;
            prereqs.TemperatureStatus = prereqs.TemperatureOK
                ? $"T_avg = {T_avg:F1}°F ✓"
                : $"T_avg = {T_avg:F1}°F (need {TAVG_SETPOINT_F}±{TAVG_TOLERANCE_F}°F)";
            
            // Pressure
            prereqs.PressureOK = Mathf.Abs(pressure_psig - PRESSURE_SETPOINT_PSIG) <= PRESSURE_TOLERANCE_PSI;
            prereqs.PressureStatus = prereqs.PressureOK
                ? $"P = {pressure_psig:F0} psig ✓"
                : $"P = {pressure_psig:F0} psig (need {PRESSURE_SETPOINT_PSIG}±{PRESSURE_TOLERANCE_PSI} psig)";
            
            // PZR Level
            prereqs.LevelOK = Mathf.Abs(pzrLevel_pct - LEVEL_SETPOINT_PCT) <= LEVEL_TOLERANCE_PCT;
            prereqs.LevelStatus = prereqs.LevelOK
                ? $"PZR Level = {pzrLevel_pct:F1}% ✓"
                : $"PZR Level = {pzrLevel_pct:F1}% (need {LEVEL_SETPOINT_PCT}±{LEVEL_TOLERANCE_PCT}%)";
            
            // RCPs
            prereqs.RCPsOK = rcpCount >= 4;
            prereqs.RCPsStatus = prereqs.RCPsOK
                ? "All 4 RCPs Running ✓"
                : $"{rcpCount}/4 RCPs Running";
            
            // Boron (minimum for cold shutdown margin, typically > 1000 ppm)
            prereqs.BoronOK = boron_ppm >= 1000f;
            prereqs.BoronStatus = prereqs.BoronOK
                ? $"Boron = {boron_ppm:F0} ppm ✓"
                : $"Boron = {boron_ppm:F0} ppm (need ≥1000 ppm)";
            
            // Overall
            prereqs.AllMet = prereqs.HZPStable && prereqs.TemperatureOK && 
                            prereqs.PressureOK && prereqs.LevelOK && 
                            prereqs.RCPsOK && prereqs.BoronOK;
            
            return prereqs;
        }
        
        // ============================================================================
        // VALIDATION
        // ============================================================================
        
        /// <summary>
        /// Validate HZP stabilization controller logic.
        /// </summary>
        /// <returns>True if all validations pass</returns>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: Initialization should produce INACTIVE state
            var state = Initialize();
            if (state.State != HZPState.INACTIVE) valid = false;
            
            // Test 2: Should transition to APPROACHING when T_avg exceeds threshold
            Update(ref state, 555f, 2235f, 60f, 1092f, 4, 10f, 1f);
            if (state.State != HZPState.APPROACHING) valid = false;
            
            // Test 3: Should transition to STABILIZING when near setpoint
            Update(ref state, 557f, 2235f, 60f, 1092f, 4, 10f, 1f);
            if (state.State != HZPState.STABILIZING) valid = false;
            
            // Test 4: Should track stability timer when all params OK
            for (int i = 0; i < 100; i++)
            {
                Update(ref state, 557f, 2235f, 60f, 1092f, 4, 10f, 1f);
            }
            if (state.StableTimer_sec < 99f) valid = false;
            
            // Test 5: Should reset timer if parameter goes out of band
            float prevTimer = state.StableTimer_sec;
            Update(ref state, 560f, 2235f, 60f, 1092f, 4, 10f, 1f); // T_avg out of band
            if (state.StableTimer_sec != 0f) valid = false;
            
            // Test 6: Should transition to STABLE after sufficient time
            state = Initialize();
            Update(ref state, 555f, 2235f, 60f, 1092f, 4, 10f, 1f); // → APPROACHING
            Update(ref state, 557f, 2235f, 60f, 1092f, 4, 10f, 1f); // → STABILIZING
            for (int i = 0; i < 350; i++) // 350 seconds > 300 required
            {
                Update(ref state, 557f, 2235f, 60f, 1092f, 4, 10f, 1f);
            }
            if (state.State != HZPState.STABLE) valid = false;
            
            // Test 7: Handoff should work from STABLE state
            bool handoffOK = InitiateHandoff(ref state, 15f);
            if (!handoffOK || state.State != HZPState.HANDOFF_READY) valid = false;
            
            // Test 8: IsStable should return true for STABLE and HANDOFF_READY
            if (!IsStable(state)) valid = false;
            
            return valid;
        }
    }
    
    /// <summary>
    /// Startup prerequisites check result.
    /// </summary>
    public struct StartupPrerequisites
    {
        public bool HZPStable;
        public string HZPStableStatus;
        
        public bool TemperatureOK;
        public string TemperatureStatus;
        
        public bool PressureOK;
        public string PressureStatus;
        
        public bool LevelOK;
        public string LevelStatus;
        
        public bool RCPsOK;
        public string RCPsStatus;
        
        public bool BoronOK;
        public string BoronStatus;
        
        public bool AllMet;
    }
}
