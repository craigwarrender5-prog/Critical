// CRITICAL: Master the Atom - Phase 1 Core Physics Engine
// CVCSController.cs - CVCS Charging/Letdown Flow Control
//
// Implements: Engine Architecture Audit Fix 7.2
//   - PI controller for charging flow in normal (two-phase) operations
//   - Mirrors SolidPlantPressure CVCS controller pattern for consistency
//   - Extracts ~30 lines of inline engine code into physics module
//
// PHYSICS:
//   The Chemical and Volume Control System (CVCS) maintains pressurizer
//   level by adjusting charging flow relative to letdown flow.
//
//   During two-phase (normal) operations:
//     - Letdown is set by orifice selection and RCS pressure
//     - Charging is modulated by PI controller to maintain PZR level
//     - Seal injection is a fixed demand per running RCP
//
//   PI Controller:
//     error = actual_level - setpoint_level  (% level)
//     charging = base_charging + Kp*(-error) + Ki*integral(-error)
//
//   The controller increases charging when level is LOW (negative error)
//   and decreases charging when level is HIGH (positive error).
//
// Sources:
//   - NRC HRTD Section 4.1 - CVCS Operations
//   - NRC HRTD Section 10.3 - Pressurizer Level Control
//   - NRC HRTD Section 19.2 - Heatup/Cooldown CVCS Lineup
//
// Units: gpm for flow, % for level, hours for time

using System;

namespace Critical.Physics
{
    /// <summary>
    /// Letdown flow path selection.
    /// Per NRC HRTD 19.0: Path depends on RCS temperature and pressure.
    /// </summary>
    public enum LetdownPath
    {
        /// <summary>RHR-CVCS crossconnect via HCV-128 (low temp/pressure)</summary>
        RHR_CROSSCONNECT,
        
        /// <summary>Normal letdown via orifices (high temp/pressure)</summary>
        ORIFICE,
        
        /// <summary>Letdown isolated (low PZR level interlock)</summary>
        ISOLATED
    }
    
    /// <summary>
    /// Letdown path selection result with additional context.
    /// </summary>
    public struct LetdownPathState
    {
        public LetdownPath Path;         // Selected letdown path
        public bool ViaRHR;              // True if using RHR crossconnect
        public bool ViaOrifice;          // True if using normal orifice path
        public bool IsIsolated;          // True if letdown is isolated
        public string Reason;            // Human-readable reason for selection
    }
    
    /// <summary>
    /// Heater operating mode.
    /// Per NRC HRTD 6.1 / 10.2: Heaters transition through distinct modes
    /// during startup, bubble formation, pressurization, and normal operations.
    /// </summary>
    public enum HeaterMode
    {
        /// <summary>All heater groups at full 1800 kW, no feedback. Pre-bubble.</summary>
        STARTUP_FULL_POWER,
        
        /// <summary>Continuously variable with pressure-rate feedback. During bubble drain.</summary>
        BUBBLE_FORMATION_AUTO,
        
        /// <summary>Same auto controller, target >= 320 psig for RCP NPSH.</summary>
        PRESSURIZE_AUTO,
        
        /// <summary>Future: Proportional + backup groups, automatic PID on pressure.</summary>
        AUTOMATIC_PID,
        
        /// <summary>Heaters off (tripped by interlock or not yet energized).</summary>
        OFF
    }
    
    /// <summary>
    /// Pressurizer heater control state.
    /// Per NRC HRTD 6.1 / 10.2: Multi-mode heater controller.
    /// </summary>
    public struct HeaterControlState
    {
        public bool HeatersEnabled;      // True if heaters should be energized
        public float HeaterPower_MW;     // Commanded heater power (MW)
        public float HeaterFraction;     // Power as fraction of maximum (0-1)
        public bool ProportionalOn;      // Proportional heaters active
        public bool BackupOn;            // Backup heaters active (level rise)
        public bool TrippedByInterlock;  // True if tripped by low-level interlock
        public HeaterMode Mode;          // Current operating mode
        public string StatusReason;      // Human-readable status
        
        /// <summary>
        /// v2.0.10: Rate-limited smoothed output for bubble/pressurize modes.
        /// Caller must persist this value between timesteps and pass it back in.
        /// </summary>
        public float SmoothedOutput;
    }
    
    /// <summary>
    /// Seal flow components for RCP seal injection system.
    /// Per NRC IN 93-84: Each RCP requires ~8 gpm seal injection.
    /// </summary>
    public struct SealFlowState
    {
        public float SealInjection;      // Total seal injection to all RCPs (gpm)
        public float SealReturnToVCT;    // Seal leakoff returning to VCT (gpm)
        public float SealReturnToRCS;    // Seal flow returning to RCS (gpm)
        public float CBOLoss;            // Controlled bleedoff loss (gpm)
        public float NetSealDemand;      // Net seal system demand on charging (gpm)
        public int RCPCount;             // Number of RCPs running
    }

    /// <summary>
    /// State container for CVCS controller.
    /// Persists between timesteps to track integral error.
    /// </summary>
    public struct CVCSControllerState
    {
        public float IntegralError;      // Accumulated integral error (gpm equivalent)
        public float LastLevelError;     // Previous level error for derivative (if needed)
        public float ChargingFlow;       // Current charging flow output (gpm)
        public float LetdownFlow;        // Current letdown flow (gpm)
        public float SealInjection;      // Current seal injection demand (gpm)
        public float LevelSetpoint;      // Current level setpoint (%)
        public float LevelError;         // Current level error (%)
        public bool LetdownIsolated;     // True if letdown is isolated
        public bool IsActive;            // True if controller is active (not in solid plant mode)
    }
    
    /// <summary>
    /// State container for Heater PID controller (v1.1.0 Stage 4).
    /// Persists between timesteps to track PID state and valve dynamics.
    /// </summary>
    public struct HeaterPIDState
    {
        /// <summary>Accumulated integral error (fraction-hours)</summary>
        public float Integral;
        
        /// <summary>Previous pressure error for derivative calculation (psi)</summary>
        public float PreviousError;
        
        /// <summary>Raw PID output command (0-1 fraction)</summary>
        public float OutputCommand;
        
        /// <summary>Rate-limited and lagged output (0-1 fraction)</summary>
        public float SmoothedOutput;
        
        /// <summary>Proportional heaters power fraction (0-1)</summary>
        public float ProportionalFraction;
        
        /// <summary>Backup heaters on/off state</summary>
        public bool BackupOn;
        
        /// <summary>True if controller is active</summary>
        public bool IsActive;
        
        /// <summary>True if in deadband (holding steady)</summary>
        public bool InDeadband;
        
        /// <summary>Pressure setpoint (psig)</summary>
        public float PressureSetpoint;
        
        /// <summary>Current pressure error (psi)</summary>
        public float PressureError;
        
        /// <summary>Calculated heater power (MW)</summary>
        public float HeaterPower_MW;
        
        /// <summary>Status message for display</summary>
        public string StatusMessage;
    }
    
    /// <summary>
    /// v4.4.0: Pressurizer spray controller state.
    /// Tracks spray valve position and thermodynamic effects.
    /// Persists between timesteps for valve dynamics.
    ///
    /// Source: NRC HRTD 10.2 — Pressurizer spray system modulates between
    /// 2260 psig (start opening) and 2310 psig (fully open), providing
    /// cold-leg water to condense PZR steam and reduce pressure.
    /// </summary>
    public struct SprayControlState
    {
        /// <summary>Spray valve position: 0.0 (closed) to 1.0 (full open)</summary>
        public float ValvePosition;
        
        /// <summary>Actual spray flow rate (gpm), includes bypass</summary>
        public float SprayFlow_GPM;
        
        /// <summary>Steam condensed this timestep (lbm)</summary>
        public float SteamCondensed_lbm;
        
        /// <summary>Heat removed by spray this timestep (BTU)</summary>
        public float HeatRemoved_BTU;
        
        /// <summary>Spray demand signal from pressure controller (0-1)</summary>
        public float SprayDemand;
        
        /// <summary>True if spray system is enabled (RCPs running)</summary>
        public bool IsEnabled;
        
        /// <summary>True if spray valve is open beyond bypass</summary>
        public bool IsActive;
        
        /// <summary>Delta-T between PZR and spray water (°F)</summary>
        public float SprayDeltaT;
        
        /// <summary>Human-readable status</summary>
        public string StatusMessage;
    }
    
    /// <summary>
    /// CVCS charging flow controller for two-phase (normal) pressurizer operations.
    /// 
    /// This module owns the PI control logic for maintaining pressurizer level
    /// during normal operations when a steam bubble exists.
    /// 
    /// For solid plant operations (no bubble), use SolidPlantPressure.cs instead.
    /// 
    /// The engine should call Initialize() once when transitioning to two-phase,
    /// then Update() each timestep, reading flows from the returned state.
    /// </summary>
    public static class CVCSController
    {
        #region Initialization
        
        /// <summary>
        /// Initialize CVCS controller state for two-phase operations.
        /// Called when transitioning from solid plant to normal (bubble exists).
        /// </summary>
        /// <param name="currentLevel">Current PZR level (%)</param>
        /// <param name="levelSetpoint">Initial level setpoint (%)</param>
        /// <param name="baseCharging">Base charging flow (gpm)</param>
        /// <param name="baseLetdown">Base letdown flow (gpm)</param>
        /// <param name="sealInjection">Seal injection demand (gpm)</param>
        /// <returns>Initialized controller state</returns>
        public static CVCSControllerState Initialize(
            float currentLevel,
            float levelSetpoint,
            float baseCharging = 75f,
            float baseLetdown = 75f,
            float sealInjection = 0f)
        {
            var state = new CVCSControllerState();
            
            state.IntegralError = 0f;
            state.LastLevelError = currentLevel - levelSetpoint;
            state.ChargingFlow = baseCharging;
            state.LetdownFlow = baseLetdown;
            state.SealInjection = sealInjection;
            state.LevelSetpoint = levelSetpoint;
            state.LevelError = state.LastLevelError;
            state.LetdownIsolated = false;
            state.IsActive = true;
            
            return state;
        }
        
        #endregion
        
        #region Main Update
        
        /// <summary>
        /// Update CVCS controller for one timestep.
        /// 
        /// Calculates charging flow based on PI control of pressurizer level.
        /// Letdown flow is calculated based on orifice path and RCS conditions.
        /// 
        /// v4.4.0: Added orifice lineup parameters for multi-orifice letdown model.
        /// When num75Open >= 0, uses the new mixed-orifice model (2×75 + 1×45 gpm)
        /// with ion exchanger flow limit. Legacy callers default to single 75-gpm orifice.
        /// </summary>
        /// <param name="state">Controller state (modified in place)</param>
        /// <param name="currentLevel">Current PZR level (%)</param>
        /// <param name="T_avg">Average RCS temperature (°F)</param>
        /// <param name="pressure_psia">RCS pressure (psia)</param>
        /// <param name="rcpCount">Number of RCPs running</param>
        /// <param name="dt_hr">Timestep in hours</param>
        /// <param name="num75Open">Number of 75-gpm orifices open (0-2), -1 = legacy single orifice</param>
        /// <param name="open45">True if the 45-gpm orifice is open</param>
        public static void Update(
            ref CVCSControllerState state,
            float currentLevel,
            float T_avg,
            float pressure_psia,
            int rcpCount,
            float dt_hr,
            int num75Open = -1,
            bool open45 = false)
        {
            if (!state.IsActive) return;
            
            // ================================================================
            // LEVEL SETPOINT FROM PROGRAM
            // PZR level setpoint varies with T_avg per the level program
            // ================================================================
            // v0.4.0 Issue #2 fix: Was GetPZRLevelProgram (at-power only, clamps to 25% below 557°F).
            // Unified function uses heatup program below 557°F, at-power program above.
            state.LevelSetpoint = PlantConstants.GetPZRLevelSetpointUnified(T_avg);
            
            // ================================================================
            // SEAL INJECTION DEMAND
            // Each running RCP requires seal injection flow
            // ================================================================
            state.SealInjection = rcpCount * PlantConstants.SEAL_INJECTION_PER_PUMP_GPM;
            
            // ================================================================
            // LOW-LEVEL INTERLOCK CHECK
            // If level drops below isolation setpoint, isolate letdown and trip heaters
            // ================================================================
            if (currentLevel < PlantConstants.PZR_LOW_LEVEL_ISOLATION)
            {
                state.LetdownIsolated = true;
            }
            else if (currentLevel > PlantConstants.PZR_LOW_LEVEL_ISOLATION + 5f)
            {
                // Hysteresis: restore letdown when level recovers
                state.LetdownIsolated = false;
            }
            
            // ================================================================
            // LETDOWN FLOW CALCULATION
            // Based on orifice selection and RCS-to-VCT differential pressure
            // ================================================================
            if (state.LetdownIsolated)
            {
                state.LetdownFlow = 0f;
            }
            else
            {
                // v4.4.0: Calculate letdown based on orifice lineup
                // PlantConstants.CalculateTotalLetdownFlow handles orifice sizing
                // and ion exchanger limit for the new multi-orifice model.
                state.LetdownFlow = PlantConstants.CalculateTotalLetdownFlow(
                    T_avg, pressure_psia, numOrificesOpen: 1,
                    num75Open: num75Open, open45: open45);
            }
            
            // ================================================================
            // PI CHARGING CONTROLLER
            // Modulates charging to maintain PZR level at setpoint
            // ================================================================
            state.LevelError = currentLevel - state.LevelSetpoint;
            
            // Proportional term: immediate response to level error
            // Negative error (level low) → increase charging
            // Positive error (level high) → decrease charging
            float pCorrection = PlantConstants.CVCS_LEVEL_KP * (-state.LevelError);
            
            // Integral term: accumulated error drives steady-state correction
            // Convert dt from hours to seconds for consistent units
            float dt_sec = dt_hr * 3600f;
            state.IntegralError += (-state.LevelError) * dt_sec;
            
            // Anti-windup: limit integral accumulation
            float integralLimit = PlantConstants.CVCS_LEVEL_INTEGRAL_LIMIT / PlantConstants.CVCS_LEVEL_KI;
            state.IntegralError = Math.Max(-integralLimit, Math.Min(state.IntegralError, integralLimit));
            
            float iCorrection = PlantConstants.CVCS_LEVEL_KI * state.IntegralError;
            
            // Base charging = letdown + seal injection (to maintain balance)
            float baseCharging = state.LetdownFlow + state.SealInjection;
            
            // Apply PI corrections
            state.ChargingFlow = baseCharging + pCorrection + iCorrection;
            
            // Clamp to physical limits
            // Minimum: must at least provide seal injection
            // Maximum: CCP capacity limit
            state.ChargingFlow = Math.Max(state.SealInjection, Math.Min(state.ChargingFlow, 150f));
            
            state.LastLevelError = state.LevelError;
        }
        
        /// <summary>
        /// Simplified update for cases where letdown is externally provided.
        /// </summary>
        public static void UpdateWithLetdown(
            ref CVCSControllerState state,
            float currentLevel,
            float letdownFlow,
            int rcpCount,
            float dt_hr)
        {
            if (!state.IsActive) return;
            
            state.LetdownFlow = letdownFlow;
            state.SealInjection = rcpCount * PlantConstants.SEAL_INJECTION_PER_PUMP_GPM;
            
            // Low-level isolation check
            state.LetdownIsolated = (currentLevel < PlantConstants.PZR_LOW_LEVEL_ISOLATION);
            if (state.LetdownIsolated)
                state.LetdownFlow = 0f;
            
            // PI controller
            state.LevelError = currentLevel - state.LevelSetpoint;
            
            float pCorrection = PlantConstants.CVCS_LEVEL_KP * (-state.LevelError);
            
            float dt_sec = dt_hr * 3600f;
            state.IntegralError += (-state.LevelError) * dt_sec;
            
            float integralLimit = PlantConstants.CVCS_LEVEL_INTEGRAL_LIMIT / PlantConstants.CVCS_LEVEL_KI;
            state.IntegralError = Math.Max(-integralLimit, Math.Min(state.IntegralError, integralLimit));
            
            float iCorrection = PlantConstants.CVCS_LEVEL_KI * state.IntegralError;
            
            float baseCharging = state.LetdownFlow + state.SealInjection;
            state.ChargingFlow = baseCharging + pCorrection + iCorrection;
            state.ChargingFlow = Math.Max(state.SealInjection, Math.Min(state.ChargingFlow, 150f));
            
            state.LastLevelError = state.LevelError;
        }
        
        #endregion
        
        #region Seal Flow Calculations
        
        /// <summary>
        /// Calculate all RCP seal system flows.
        /// 
        /// Per NRC IN 93-84 and HRTD 3.2:
        ///   - Seal injection: 8 gpm per RCP (from charging)
        ///   - Seal leakoff to VCT: 3 gpm per RCP (#1 seal leakoff)
        ///   - Seal return to RCS: 5 gpm per RCP (past #1 seal to RCS)
        ///   - CBO loss: 1 gpm total when RCPs running
        /// 
        /// The seal injection is supplied by the charging pumps, so it
        /// represents a demand on the CVCS that does not reach the RCS.
        /// </summary>
        /// <param name="rcpCount">Number of RCPs currently running (0-4)</param>
        /// <returns>SealFlowState with all seal system flows</returns>
        public static SealFlowState CalculateSealFlows(int rcpCount)
        {
            var state = new SealFlowState();
            state.RCPCount = rcpCount;
            
            // Per-pump flows from PlantConstants
            state.SealInjection = rcpCount * PlantConstants.SEAL_INJECTION_PER_PUMP_GPM;
            state.SealReturnToVCT = rcpCount * PlantConstants.SEAL_LEAKOFF_PER_PUMP_GPM;
            state.SealReturnToRCS = rcpCount * PlantConstants.SEAL_FLOW_TO_RCS_PER_PUMP_GPM;
            
            // CBO is a small constant loss when any RCPs are running
            state.CBOLoss = rcpCount > 0 ? PlantConstants.CBO_LOSS_GPM : 0f;
            
            // Net seal demand = injection - returns
            // This is the net flow "lost" from charging that doesn't reach RCS
            state.NetSealDemand = state.SealInjection - state.SealReturnToRCS;
            
            return state;
        }
        
        #endregion
        
        #region Letdown Path Selection
        
        /// <summary>
        /// Determine the active letdown flow path based on plant conditions.
        /// 
        /// Per NRC HRTD 19.0 and Section 4.1:
        ///   - At low RCS temperature (< 350°F): Letdown via RHR-CVCS crossconnect (HCV-128)
        ///     because the normal orifice path produces negligible flow at low ΔP
        ///   - At high RCS temperature (≥ 350°F): Letdown via normal orifice path
        ///   - If low-level interlock active: Letdown is isolated regardless of temp
        ///   - During solid PZR ops: RHR path is used (temp will be < 350°F)
        /// 
        /// The 350°F threshold corresponds to RHR letdown isolation temperature
        /// per NRC HRTD Section 19.2.2.
        /// </summary>
        /// <param name="T_rcs">RCS temperature (°F)</param>
        /// <param name="pressure">RCS pressure (psia) - reserved for future use</param>
        /// <param name="solidPressurizer">True if in solid pressurizer operations</param>
        /// <param name="letdownIsolated">True if low-level interlock has isolated letdown</param>
        /// <returns>LetdownPathState with path selection and reasoning</returns>
        public static LetdownPathState GetLetdownPath(
            float T_rcs, 
            float pressure, 
            bool solidPressurizer, 
            bool letdownIsolated)
        {
            var state = new LetdownPathState();
            
            // Priority 1: Low-level interlock isolates letdown
            if (letdownIsolated)
            {
                state.Path = LetdownPath.ISOLATED;
                state.ViaRHR = false;
                state.ViaOrifice = false;
                state.IsIsolated = true;
                state.Reason = "Low PZR level interlock";
                return state;
            }
            
            // Priority 2: Temperature-based path selection
            // Per NRC HRTD 19.0: RHR crossconnect used below 350°F
            bool useRHR = (T_rcs < PlantConstants.RHR_LETDOWN_ISOLATION_TEMP_F);
            
            if (useRHR)
            {
                state.Path = LetdownPath.RHR_CROSSCONNECT;
                state.ViaRHR = true;
                state.ViaOrifice = false;
                state.IsIsolated = false;
                state.Reason = solidPressurizer 
                    ? "Solid PZR ops - RHR crossconnect" 
                    : $"T_rcs < {PlantConstants.RHR_LETDOWN_ISOLATION_TEMP_F}°F";
            }
            else
            {
                state.Path = LetdownPath.ORIFICE;
                state.ViaRHR = false;
                state.ViaOrifice = true;
                state.IsIsolated = false;
                state.Reason = "Normal orifice path";
            }
            
            return state;
        }
        
        #endregion
        
        #region Heater Control
        
        /// <summary>
        /// Calculate pressurizer heater control state based on plant conditions and mode.
        /// 
        /// Multi-mode heater controller per NRC HRTD 6.1 / 10.2:
        /// 
        /// Mode 1 (STARTUP_FULL_POWER): All groups at full 1800 kW, no feedback.
        ///   Used during solid plant ops to heat PZR water to T_sat.
        ///   Per NRC HRTD 19.2.2: "All heater groups energized manually."
        /// 
        /// Mode 2 (BUBBLE_FORMATION_AUTO): Continuously variable with pressure-rate
        ///   feedback. During bubble formation drain. If dP/dt exceeds max rate,
        ///   power is reduced proportionally. Minimum power floor prevents stalling.
        ///   Per design decision v0.2.0: automatic modulation, future adds manual.
        /// 
        /// Mode 3 (PRESSURIZE_AUTO): Same auto controller as Mode 2, but targeting
        ///   >= 320 psig for RCP NPSH. Pressure-rate feedback active.
        /// 
        /// Mode 4 (AUTOMATIC_PID): Future scope — proportional + backup groups
        ///   with automatic PID on pressure at 2235 psig setpoint.
        /// 
        /// Low-level interlock: Trips ALL heaters when level < 17% (all modes).
        /// </summary>
        /// <param name="pzrLevel">Current pressurizer level (%)</param>
        /// <param name="pzrLevelSetpoint">Current level setpoint from program (%)</param>
        /// <param name="letdownIsolated">True if low-level interlock is active</param>
        /// <param name="solidPressurizer">True if in solid pressurizer operations</param>
        /// <param name="baseHeaterPower_MW">Base heater power when enabled (MW)</param>
        /// <param name="mode">Current heater operating mode</param>
        /// <param name="pressureRate_psi_hr">Current pressure rate of change (psi/hr)</param>
        /// <returns>HeaterControlState with heater commands and status</returns>
        public static HeaterControlState CalculateHeaterState(
            float pzrLevel,
            float pzrLevelSetpoint,
            bool letdownIsolated,
            bool solidPressurizer,
            float baseHeaterPower_MW,
            HeaterMode mode = HeaterMode.STARTUP_FULL_POWER,
            float pressureRate_psi_hr = 0f,
            float dt_hr = 0f,
            float smoothedOutput = 1.0f)
        {
            var state = new HeaterControlState();
            
            // Default: heaters on at full power
            state.ProportionalOn = true;
            state.BackupOn = false;
            state.TrippedByInterlock = false;
            state.HeaterPower_MW = baseHeaterPower_MW;
            state.HeaterFraction = 1.0f;
            state.HeatersEnabled = true;
            state.Mode = mode;
            
            // ================================================================
            // PRIORITY 1: Low-level interlock trips ALL heaters (all modes)
            // Per NRC HRTD 10.3: PZR level < 17% trips heaters to protect elements
            // ================================================================
            if (letdownIsolated)
            {
                state.HeatersEnabled = false;
                state.HeaterPower_MW = 0f;
                state.HeaterFraction = 0f;
                state.ProportionalOn = false;
                state.BackupOn = false;
                state.TrippedByInterlock = true;
                state.Mode = HeaterMode.OFF;
                state.StatusReason = "TRIPPED - Low level interlock";
                return state;
            }
            
            // ================================================================
            // MODE-SPECIFIC HEATER CONTROL
            // ================================================================
            switch (mode)
            {
                // ============================================================
                // STARTUP: Full power, no feedback
                // Per NRC HRTD 19.2.2: All groups manually energized
                // ============================================================
                case HeaterMode.STARTUP_FULL_POWER:
                    state.HeaterPower_MW = baseHeaterPower_MW;
                    state.HeaterFraction = 1.0f;
                    state.ProportionalOn = true;
                    state.BackupOn = true;  // All groups energized
                    state.StatusReason = solidPressurizer 
                        ? "Solid PZR - heating to Tsat (all groups)"
                        : "Startup - full power (all groups)";
                    break;
                
                // ============================================================
                // BUBBLE FORMATION: Pressure-rate modulated
                // If dP/dt > max rate, reduce power proportionally.
                // Minimum floor prevents heaters from completely stalling.
                // ============================================================
                case HeaterMode.BUBBLE_FORMATION_AUTO:
                {
                    // v2.0.10: Rate-limited heater control replaces stateless bang-bang.
                    // Calculate target fraction from pressure rate (same formula as before)
                    float targetFraction = 1.0f;
                    float maxRate = PlantConstants.HEATER_STARTUP_MAX_PRESSURE_RATE;
                    float minFraction = PlantConstants.HEATER_STARTUP_MIN_POWER_FRACTION;
                    
                    float absPressureRate = Math.Abs(pressureRate_psi_hr);
                    
                    if (absPressureRate > maxRate && maxRate > 0f)
                    {
                        targetFraction = 1.0f - (absPressureRate - maxRate) / maxRate;
                        targetFraction = Math.Max(minFraction, Math.Min(targetFraction, 1.0f));
                    }
                    
                    // v2.0.10: Rate-limit the output change per timestep.
                    // Max change of 6.0 per hour (matches HEATER_RATE_LIMIT_PER_HR).
                    // At 10-sec timesteps (dt=1/360 hr), max change ≈ 1.67% per step.
                    // Full travel 20%→100% takes ~2.9 minutes — realistic valve travel.
                    float currentSmoothed = smoothedOutput;
                    if (dt_hr > 0f)
                    {
                        float maxChangePerHr = PlantConstants.HEATER_RATE_LIMIT_PER_HR;
                        float maxStep = maxChangePerHr * dt_hr;
                        float delta = targetFraction - currentSmoothed;
                        delta = Math.Max(-maxStep, Math.Min(delta, maxStep));
                        currentSmoothed += delta;
                        currentSmoothed = Math.Max(minFraction, Math.Min(currentSmoothed, 1.0f));
                    }
                    else
                    {
                        // No dt provided — fall back to instantaneous (backward compat)
                        currentSmoothed = targetFraction;
                    }
                    
                    state.HeaterFraction = currentSmoothed;
                    state.HeaterPower_MW = baseHeaterPower_MW * currentSmoothed;
                    state.SmoothedOutput = currentSmoothed;
                    state.ProportionalOn = true;
                    state.BackupOn = (currentSmoothed > 0.5f);  // Backup groups shed first
                    state.StatusReason = currentSmoothed < 0.99f
                        ? $"Bubble auto - {currentSmoothed * 100:F0}% ({absPressureRate:F0} psi/hr)"
                        : "Bubble auto - full power";
                    break;
                }
                
                // ============================================================
                // PRESSURIZE: Same as bubble formation auto
                // Target >= 320 psig for RCP NPSH
                // ============================================================
                case HeaterMode.PRESSURIZE_AUTO:
                {
                    // v2.0.10: Rate-limited heater control (same as BUBBLE_FORMATION_AUTO)
                    float targetFraction = 1.0f;
                    float maxRate = PlantConstants.HEATER_STARTUP_MAX_PRESSURE_RATE;
                    float minFraction = PlantConstants.HEATER_STARTUP_MIN_POWER_FRACTION;
                    
                    float absPressureRate = Math.Abs(pressureRate_psi_hr);
                    
                    if (absPressureRate > maxRate && maxRate > 0f)
                    {
                        targetFraction = 1.0f - (absPressureRate - maxRate) / maxRate;
                        targetFraction = Math.Max(minFraction, Math.Min(targetFraction, 1.0f));
                    }
                    
                    // v2.0.10: Rate-limit the output change per timestep
                    float currentSmoothed = smoothedOutput;
                    if (dt_hr > 0f)
                    {
                        float maxChangePerHr = PlantConstants.HEATER_RATE_LIMIT_PER_HR;
                        float maxStep = maxChangePerHr * dt_hr;
                        float delta = targetFraction - currentSmoothed;
                        delta = Math.Max(-maxStep, Math.Min(delta, maxStep));
                        currentSmoothed += delta;
                        currentSmoothed = Math.Max(minFraction, Math.Min(currentSmoothed, 1.0f));
                    }
                    else
                    {
                        currentSmoothed = targetFraction;
                    }
                    
                    state.HeaterFraction = currentSmoothed;
                    state.HeaterPower_MW = baseHeaterPower_MW * currentSmoothed;
                    state.SmoothedOutput = currentSmoothed;
                    state.ProportionalOn = true;
                    state.BackupOn = (currentSmoothed > 0.5f);
                    state.StatusReason = currentSmoothed < 0.99f
                        ? $"Pressurize auto - {currentSmoothed * 100:F0}% ({absPressureRate:F0} psi/hr)"
                        : "Pressurize auto - full power";
                    break;
                }
                
                // ============================================================
                // AUTOMATIC PID: Future scope (Phase 3+)
                // Proportional + backup group staging at 2235 psig setpoint
                // Constants defined in PlantConstants, logic deferred
                // ============================================================
                case HeaterMode.AUTOMATIC_PID:
                {
                    // Placeholder: proportional heaters only, backup off
                    // Full implementation deferred to Phase 3+
                    float propPower_MW = PlantConstants.HEATER_POWER_PROP / 1000f;
                    state.HeaterPower_MW = propPower_MW;
                    state.HeaterFraction = propPower_MW / baseHeaterPower_MW;
                    state.ProportionalOn = true;
                    state.BackupOn = false;
                    
                    // Backup heater actuation on level rise (existing logic)
                    float backupThreshold = pzrLevelSetpoint + PlantConstants.PZR_BACKUP_HEATER_LEVEL_OFFSET;
                    if (pzrLevel > backupThreshold)
                    {
                        state.BackupOn = true;
                        state.HeaterPower_MW = baseHeaterPower_MW;
                        state.HeaterFraction = 1.0f;
                        state.StatusReason = $"Auto PID - backup ON (level {pzrLevel:F1}%)";
                    }
                    else
                    {
                        state.StatusReason = "Auto PID - proportional only";
                    }
                    break;
                }
                
                // ============================================================
                // OFF: Heaters de-energized
                // ============================================================
                case HeaterMode.OFF:
                    state.HeatersEnabled = false;
                    state.HeaterPower_MW = 0f;
                    state.HeaterFraction = 0f;
                    state.ProportionalOn = false;
                    state.BackupOn = false;
                    state.StatusReason = "Heaters OFF";
                    break;
            }
            
            return state;
        }
        
        #endregion
        
        #region Heater PID Controller — v1.1.0 Stage 4
        
        // =====================================================================
        // PID-based heater controller for normal (two-phase) operations.
        // Replaces bang-bang control with smooth pressure-based modulation.
        //
        // Per NRC HRTD 10.2: "The pressurizer pressure control system maintains
        // RCS pressure at 2235 psig by controlling pressurizer heaters and spray."
        //
        // Features:
        //   - PID control on pressure error
        //   - Deadband to prevent hunting
        //   - Rate limiting for smooth output changes
        //   - First-order lag modeling heater thermal inertia
        //   - Proportional/backup heater staging
        // =====================================================================
        
        /// <summary>
        /// Initialize the Heater PID controller state.
        /// Called when transitioning to HZP operations.
        /// </summary>
        /// <param name="currentPressure_psig">Current RCS pressure (psig)</param>
        /// <returns>Initialized PID state</returns>
        public static HeaterPIDState InitializeHeaterPID(float currentPressure_psig)
        {
            var state = new HeaterPIDState
            {
                Integral = 0f,
                PreviousError = PlantConstants.PZR_OPERATING_PRESSURE_PSIG - currentPressure_psig,
                OutputCommand = 0.5f,  // Start at 50%
                SmoothedOutput = 0.5f,
                ProportionalFraction = 0.5f,
                BackupOn = false,
                IsActive = true,
                InDeadband = false,
                PressureSetpoint = PlantConstants.PZR_OPERATING_PRESSURE_PSIG,
                PressureError = 0f,
                HeaterPower_MW = PlantConstants.HEATER_TOTAL_CAPACITY_KW / 1000f * 0.5f,
                StatusMessage = "PID Heater Control Active"
            };
            
            return state;
        }
        
        /// <summary>
        /// Update the Heater PID controller for one timestep.
        /// 
        /// Implements smooth PID control with:
        /// - Proportional: Immediate response to pressure error
        /// - Integral: Eliminates steady-state offset
        /// - Derivative: Anticipatory action on pressure trends
        /// - Deadband: Prevents hunting near setpoint
        /// - Rate limiting: Smooth output changes
        /// - Thermal lag: Models heater element inertia
        /// </summary>
        /// <param name="state">PID state (modified in place)</param>
        /// <param name="pressure_psig">Current RCS pressure (psig)</param>
        /// <param name="pzrLevel">Current PZR level (%) for interlock check</param>
        /// <param name="dt_hr">Timestep in hours</param>
        /// <returns>Heater power in MW</returns>
        public static float UpdateHeaterPID(
            ref HeaterPIDState state,
            float pressure_psig,
            float pzrLevel,
            float dt_hr)
        {
            if (!state.IsActive)
            {
                state.HeaterPower_MW = 0f;
                state.StatusMessage = "PID Inactive";
                return 0f;
            }
            
            // ================================================================
            // LOW-LEVEL INTERLOCK CHECK
            // Trips heaters if PZR level < 17% to protect heater elements
            // ================================================================
            if (pzrLevel < PlantConstants.PZR_LOW_LEVEL_ISOLATION)
            {
                state.HeaterPower_MW = 0f;
                state.SmoothedOutput = 0f;
                state.OutputCommand = 0f;
                state.ProportionalFraction = 0f;
                state.BackupOn = false;
                state.StatusMessage = "TRIPPED - Low Level Interlock";
                return 0f;
            }
            
            // ================================================================
            // PRESSURE ERROR CALCULATION
            // Positive error = pressure below setpoint = need more heat
            // ================================================================
            state.PressureError = state.PressureSetpoint - pressure_psig;
            
            // ================================================================
            // DEADBAND CHECK
            // Within deadband, hold output steady (integral still accumulates slowly)
            // ================================================================
            state.InDeadband = Math.Abs(state.PressureError) < PlantConstants.HEATER_DEADBAND_PSI;
            
            float pidOutput;
            
            if (state.InDeadband)
            {
                // In deadband: hold current output, slow integral accumulation
                pidOutput = state.OutputCommand;
                
                // Very slow integral action to correct any offset over time
                state.Integral += state.PressureError * dt_hr * 0.1f;  // 10% rate
                state.StatusMessage = $"PID: Deadband ({state.PressureError:+0.0;-0.0;0} psi)";
            }
            else
            {
                // ============================================================
                // FULL PID CALCULATION
                // ============================================================
                
                // Proportional term
                float pTerm = PlantConstants.HEATER_PID_KP * state.PressureError;
                
                // Integral term (with anti-windup)
                state.Integral += state.PressureError * dt_hr;
                state.Integral = Math.Max(-PlantConstants.HEATER_INTEGRAL_LIMIT,
                    Math.Min(state.Integral, PlantConstants.HEATER_INTEGRAL_LIMIT));
                float iTerm = PlantConstants.HEATER_PID_KI * state.Integral;
                
                // Derivative term (on error change)
                float dError = (state.PressureError - state.PreviousError) / dt_hr;
                float dTerm = PlantConstants.HEATER_PID_KD * dError;
                
                // Combine PID terms
                pidOutput = 0.5f + pTerm + iTerm + dTerm;  // Bias at 50%
                
                state.StatusMessage = state.PressureError > 0
                    ? $"PID: Heating (P err={state.PressureError:+0.0} psi)"
                    : $"PID: Reducing (P err={state.PressureError:+0.0;-0.0} psi)";
            }
            
            // Clamp raw output to 0-1
            pidOutput = Math.Max(PlantConstants.HEATER_MIN_OUTPUT, Math.Min(pidOutput, 1.0f));
            state.OutputCommand = pidOutput;
            
            // ================================================================
            // RATE LIMITING
            // Prevent rapid output changes
            // ================================================================
            float maxChange = PlantConstants.HEATER_RATE_LIMIT_PER_HR * dt_hr;
            float delta = pidOutput - state.SmoothedOutput;
            delta = Math.Max(-maxChange, Math.Min(delta, maxChange));
            state.SmoothedOutput += delta;
            
            // ================================================================
            // THERMAL LAG (first-order filter)
            // Models thermal inertia of heater elements
            // ================================================================
            float tau = PlantConstants.HEATER_LAG_TAU_HR;
            if (tau > 0f && dt_hr > 0f)
            {
                float alpha = dt_hr / (tau + dt_hr);
                state.SmoothedOutput = state.SmoothedOutput + alpha * (pidOutput - state.SmoothedOutput);
            }
            
            // Clamp final output
            state.SmoothedOutput = Math.Max(PlantConstants.HEATER_MIN_OUTPUT, 
                Math.Min(state.SmoothedOutput, 1.0f));
            
            // ================================================================
            // HEATER STAGING
            // Proportional heaters: continuously modulated
            // Backup heaters: on/off based on pressure thresholds
            // ================================================================
            
            // Proportional heaters track smoothed output
            state.ProportionalFraction = state.SmoothedOutput;
            
            // Backup heaters: energize if pressure drops significantly below setpoint
            if (pressure_psig < PlantConstants.HEATER_BACKUP_ON_PSIG)
            {
                state.BackupOn = true;
            }
            else if (pressure_psig > PlantConstants.HEATER_BACKUP_OFF_PSIG)
            {
                state.BackupOn = false;
            }
            // Otherwise maintain current state (hysteresis)
            
            // ================================================================
            // CALCULATE TOTAL HEATER POWER
            // ================================================================
            float propPower_MW = (PlantConstants.HEATER_PROPORTIONAL_CAPACITY_KW / 1000f) * state.ProportionalFraction;
            float backupPower_MW = state.BackupOn ? (PlantConstants.HEATER_BACKUP_CAPACITY_KW / 1000f) : 0f;
            
            state.HeaterPower_MW = propPower_MW + backupPower_MW;
            
            // ================================================================
            // HEATER CUTOFF AT HIGH PRESSURE
            // All heaters off if pressure exceeds cutoff
            // ================================================================
            if (pressure_psig > PlantConstants.HEATER_PROP_CUTOFF_PSIG)
            {
                state.HeaterPower_MW = 0f;
                state.ProportionalFraction = 0f;
                state.BackupOn = false;
                state.StatusMessage = "PID: Heaters OFF (High Pressure)";
            }
            
            // Update previous error for next derivative calculation
            state.PreviousError = state.PressureError;
            
            return state.HeaterPower_MW;
        }
        
        /// <summary>
        /// Reset the Heater PID controller integral term.
        /// Call after large transients or mode changes.
        /// </summary>
        public static void ResetHeaterPIDIntegral(ref HeaterPIDState state)
        {
            state.Integral = 0f;
        }
        
        /// <summary>
        /// Enable or disable the Heater PID controller.
        /// </summary>
        public static void SetHeaterPIDActive(ref HeaterPIDState state, bool active)
        {
            state.IsActive = active;
            if (!active)
            {
                state.HeaterPower_MW = 0f;
                state.StatusMessage = "PID Disabled";
            }
        }
        
        /// <summary>
        /// Validate Heater PID controller calculations.
        /// </summary>
        public static bool ValidateHeaterPID()
        {
            bool valid = true;
            float dt = 1f / 360f;  // 10-second timestep
            
            // Test 1: Initialization should produce reasonable state
            var state = InitializeHeaterPID(2235f);
            if (!state.IsActive) valid = false;
            if (state.SmoothedOutput < 0.4f || state.SmoothedOutput > 0.6f) valid = false;
            
            // Test 2: Low pressure should increase heater output
            state = InitializeHeaterPID(2235f);
            UpdateHeaterPID(ref state, 2215f, 60f, dt);  // 20 psi below setpoint
            if (state.OutputCommand <= 0.5f) valid = false;  // Should increase
            
            // Test 3: High pressure should decrease heater output
            state = InitializeHeaterPID(2235f);
            UpdateHeaterPID(ref state, 2250f, 60f, dt);  // 15 psi above setpoint
            if (state.OutputCommand >= 0.5f) valid = false;  // Should decrease
            
            // Test 4: Pressure at setpoint should be in deadband
            state = InitializeHeaterPID(2235f);
            UpdateHeaterPID(ref state, 2235f, 60f, dt);
            if (!state.InDeadband) valid = false;
            
            // Test 5: Low level should trip heaters
            state = InitializeHeaterPID(2235f);
            UpdateHeaterPID(ref state, 2215f, 15f, dt);  // Level below 17%
            if (state.HeaterPower_MW > 0f) valid = false;
            
            // Test 6: Backup heaters should energize at low pressure
            state = InitializeHeaterPID(2235f);
            UpdateHeaterPID(ref state, 2200f, 60f, dt);  // Below 2210 psig
            if (!state.BackupOn) valid = false;
            
            // Test 7: High pressure cutoff
            state = InitializeHeaterPID(2235f);
            UpdateHeaterPID(ref state, 2260f, 60f, dt);  // Above cutoff
            if (state.HeaterPower_MW > 0f) valid = false;
            
            // Test 8: Rate limiting should prevent instant changes
            state = InitializeHeaterPID(2235f);
            state.SmoothedOutput = 0.2f;
            UpdateHeaterPID(ref state, 2200f, 60f, dt);  // Demand high output
            // Output should increase but not jump instantly to 1.0
            if (state.SmoothedOutput > 0.5f) valid = false;  // Rate limited
            
            return valid;
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Reset integral error accumulator.
        /// Call when transitioning modes or after large level disturbances.
        /// </summary>
        public static void ResetIntegral(ref CVCSControllerState state)
        {
            state.IntegralError = 0f;
        }
        
        /// <summary>
        /// Pre-seed PI controller integral for RCP start transition (Bug #4 Fix).
        /// 
        /// Per NRC HRTD 10.3: When RCPs start, the thermal transient causes
        /// rapid PZR level changes. A zero-initialized integral cannot respond
        /// fast enough, causing level excursions. This method pre-loads the
        /// integral with the current steady-state charging offset so the
        /// controller can immediately respond to the transient.
        ///
        /// Physics: At steady state, integral = (charging - base) / Ki
        ///   where base = letdown + seal injection.
        ///   Pre-seeding avoids integral wind-up lag at RCP start.
        /// </summary>
        /// <param name="state">Controller state to pre-seed</param>
        /// <param name="currentLevel">Current PZR level (%)</param>
        /// <param name="T_avg">Current average temperature (°F)</param>
        /// <param name="rcpCount">Number of RCPs about to run</param>
        public static void PreSeedForRCPStart(
            ref CVCSControllerState state,
            float currentLevel,
            float T_avg,
            int rcpCount)
        {
            // Calculate what the steady-state integral should be
            // At equilibrium: charging = letdown + seal, so integral ≈ 0
            // But pre-seed with small positive value to bias toward recovery
            // from the expected level drop at RCP start
            float sealDemand = rcpCount * PlantConstants.SEAL_INJECTION_PER_PUMP_GPM;
            float expectedBase = state.LetdownFlow + sealDemand;
            
            // v0.4.0 Issue #3 Part C: Scale pre-seed with temperature differential.
            // A larger ΔT between PZR (~Tsat) and RCS (~T_avg) means a bigger
            // expected transient from thermal mixing, needing a stronger
            // initial charging bias for faster level recovery.
            // Proxy: At low T_avg the PZR-RCS ΔT is large; at higher T_avg it shrinks.
            // At typical first RCP start (T_avg ~200°F, ΔT ~360°F): ~14 gpm pre-seed.
            // At subsequent starts (T_avg ~300°F, ΔT ~250°F): ~11 gpm pre-seed.
            float deltaT_proxy = Math.Max(0f, 400f - T_avg);  // Rough estimate of PZR-RCS ΔT
            float preSeedCharging_gpm = 5f + 10f * deltaT_proxy / 400f;
            if (PlantConstants.CVCS_LEVEL_KI > 0f)
            {
                state.IntegralError = preSeedCharging_gpm / PlantConstants.CVCS_LEVEL_KI;
            }
            
            // v0.4.0 Issue #2 fix: Was GetPZRLevelProgram (at-power only, clamps to 25% below 557°F).
            // Unified function uses heatup program below 557°F, at-power program above.
            state.LevelSetpoint = PlantConstants.GetPZRLevelSetpointUnified(T_avg);
            state.LastLevelError = currentLevel - state.LevelSetpoint;
        }
        
        /// <summary>
        /// Get net flow to RCS (positive = RCS inventory increasing).
        /// </summary>
        public static float GetNetRCSFlow(CVCSControllerState state)
        {
            return state.ChargingFlow - state.LetdownFlow;
        }
        
        /// <summary>
        /// Get charging flow to RCS (excludes seal injection).
        /// </summary>
        public static float GetChargingToRCS(CVCSControllerState state)
        {
            return Math.Max(0f, state.ChargingFlow - state.SealInjection);
        }
        
        #endregion
        
        #region Pressurizer Spray System (v4.4.0)
        
        // ====================================================================
        // PRESSURIZER SPRAY SYSTEM
        // Source: NRC HRTD 10.2 — Pressurizer Pressure Control
        //
        // Two spray valves fed from cold legs (Loops B and C).
        // Modulated by the master pressure controller:
        //   - Open linearly from 2260 psig (start) to 2310 psig (full open)
        //   - Continuous bypass flow of ~1.5 gpm
        //   - Maximum spray flow ~600 gpm at rated ΔP
        //   - Spray water at T_cold condenses PZR steam, reducing pressure
        //
        // Per NRC HRTD 10.2 Section 10.2.2:
        // "As the pressure in the pressurizer increases above its normal
        //  setpoint, the master controller decreases the output of the
        //  proportional heaters. If the pressure continues to increase,
        //  the master controller output modulates the spray valves open."
        // ====================================================================
        
        /// <summary>
        /// Initialize the spray control state.
        /// Call once when spray system is first enabled (typically when
        /// heater PID activates or when approaching operating pressure).
        /// </summary>
        public static SprayControlState InitializeSpray()
        {
            return new SprayControlState
            {
                ValvePosition = 0f,
                SprayFlow_GPM = PlantConstants.SPRAY_BYPASS_FLOW_GPM,
                SteamCondensed_lbm = 0f,
                HeatRemoved_BTU = 0f,
                SprayDemand = 0f,
                IsEnabled = false,
                IsActive = false,
                SprayDeltaT = 0f,
                StatusMessage = "Spray standby"
            };
        }
        
        /// <summary>
        /// Update pressurizer spray controller.
        /// Computes spray valve demand from pressure, valve dynamics,
        /// spray flow rate, and thermodynamic steam condensation effect.
        ///
        /// Must be called each timestep when the spray system is enabled.
        ///
        /// The spray condensation effect (SteamCondensed_lbm) should be
        /// applied to the PZR state BEFORE the CoupledThermo solver runs,
        /// similar to how CVCS mass drain is pre-applied.
        /// </summary>
        /// <param name="state">Spray controller state (persists between timesteps)</param>
        /// <param name="pressure_psig">Current PZR pressure in psig</param>
        /// <param name="T_pzr">Current PZR temperature (°F) — should be T_sat</param>
        /// <param name="T_cold">Cold leg temperature (°F) — spray water source</param>
        /// <param name="pzrSteamVolume_ft3">Current PZR steam volume (ft³)</param>
        /// <param name="pressure_psia">Current PZR pressure in psia</param>
        /// <param name="rcpCount">Number of running RCPs (spray requires RCPs)</param>
        /// <param name="dt_hr">Timestep in hours</param>
        /// <returns>Updated spray state with condensation results</returns>
        public static void UpdateSpray(
            ref SprayControlState state,
            float pressure_psig,
            float T_pzr,
            float T_cold,
            float pzrSteamVolume_ft3,
            float pressure_psia,
            int rcpCount,
            float dt_hr)
        {
            // Spray requires at least one RCP for ΔP driving force
            state.IsEnabled = (rcpCount > 0);
            
            if (!state.IsEnabled)
            {
                state.ValvePosition = 0f;
                state.SprayFlow_GPM = 0f;
                state.SteamCondensed_lbm = 0f;
                state.HeatRemoved_BTU = 0f;
                state.SprayDemand = 0f;
                state.IsActive = false;
                state.SprayDeltaT = 0f;
                state.StatusMessage = "Spray disabled (no RCPs)";
                return;
            }
            
            // ==============================================================
            // SPRAY VALVE DEMAND
            // Linear modulation between P_SPRAY_START and P_SPRAY_FULL
            // Per NRC HRTD 10.2: spray opens 2260-2310 psig
            // ==============================================================
            if (pressure_psig > PlantConstants.P_SPRAY_START_PSIG)
            {
                state.SprayDemand = (pressure_psig - PlantConstants.P_SPRAY_START_PSIG)
                    / (PlantConstants.P_SPRAY_FULL_PSIG - PlantConstants.P_SPRAY_START_PSIG);
                state.SprayDemand = Math.Max(0f, Math.Min(state.SprayDemand, 1f));
            }
            else
            {
                state.SprayDemand = 0f;
            }
            
            // ==============================================================
            // VALVE DYNAMICS
            // First-order lag models valve travel time (~30 seconds)
            // ==============================================================
            float tau = PlantConstants.SPRAY_VALVE_TAU_HR;
            if (tau > 0f && dt_hr > 0f)
            {
                float alpha = dt_hr / (tau + dt_hr);
                state.ValvePosition += alpha * (state.SprayDemand - state.ValvePosition);
            }
            else
            {
                state.ValvePosition = state.SprayDemand;
            }
            state.ValvePosition = Math.Max(0f, Math.Min(state.ValvePosition, 1f));
            
            // ==============================================================
            // SPRAY FLOW RATE
            // Bypass flow is always present; valve position adds main flow
            // ==============================================================
            state.SprayFlow_GPM = PlantConstants.SPRAY_BYPASS_FLOW_GPM
                + state.ValvePosition * PlantConstants.SPRAY_FULL_OPEN_FLOW_GPM;
            state.IsActive = (state.ValvePosition > 0.01f);
            
            // ==============================================================
            // THERMODYNAMIC STEAM CONDENSATION
            // Spray water enters PZR steam space at T_cold.
            // Energy absorbed heats spray water from T_cold to T_sat,
            // condensing an equivalent mass of steam.
            //
            // Q_absorbed = m_spray × Cp × (T_sat - T_spray) × efficiency
            // m_condensed = Q_absorbed / h_fg
            // ==============================================================
            float T_sat = T_pzr;  // PZR water/steam at saturation
            float deltaT = T_sat - T_cold;
            state.SprayDeltaT = deltaT;
            
            // Safety check: don’t spray if deltaT exceeds thermal shock limit
            // Real plant would alarm but continue; we log and reduce flow
            float effectiveFlow = state.SprayFlow_GPM;
            if (deltaT > PlantConstants.MAX_PZR_SPRAY_DELTA_T)
            {
                // Limit spray to bypass only to prevent thermal shock
                effectiveFlow = PlantConstants.SPRAY_BYPASS_FLOW_GPM;
                state.StatusMessage = $"SPRAY ΔT ALARM: {deltaT:F0}°F > {PlantConstants.MAX_PZR_SPRAY_DELTA_T:F0}°F limit";
            }
            
            // Guard: no condensation if deltaT <= 0 or no steam space
            if (deltaT <= 0f || pzrSteamVolume_ft3 < PlantConstants.PZR_STEAM_MIN)
            {
                state.SteamCondensed_lbm = 0f;
                state.HeatRemoved_BTU = 0f;
                if (pzrSteamVolume_ft3 < PlantConstants.PZR_STEAM_MIN)
                    state.StatusMessage = "Spray inactive (min steam space)";
                else if (!state.IsActive)
                    state.StatusMessage = $"Spray bypass only ({PlantConstants.SPRAY_BYPASS_FLOW_GPM:F1} gpm)";
                else
                    state.StatusMessage = $"Spray: {state.SprayFlow_GPM:F0} gpm (no ΔT)";
                return;
            }
            
            // Spray mass this timestep
            float dt_sec = dt_hr * 3600f;
            float rho_spray = WaterProperties.WaterDensity(T_cold, pressure_psia);
            float sprayMass_lbm = effectiveFlow * dt_sec * PlantConstants.GPM_TO_FT3_SEC * rho_spray;
            
            // Heat absorbed by spray water: Q = m × Cp × ΔT × efficiency
            // Cp ≈ 1.0 BTU/(lbm·°F) for subcooled water (conservative)
            float Q_absorbed = sprayMass_lbm * 1.0f * deltaT * PlantConstants.SPRAY_EFFICIENCY;
            state.HeatRemoved_BTU = Q_absorbed;
            
            // Steam condensed = Q / h_fg
            float h_fg = WaterProperties.LatentHeat(pressure_psia);  // BTU/lbm
            if (h_fg > 10f)  // Sanity check
            {
                state.SteamCondensed_lbm = Q_absorbed / h_fg;
            }
            else
            {
                state.SteamCondensed_lbm = 0f;
            }
            
            // Limit condensation to available steam mass
            float steamMass = pzrSteamVolume_ft3 * WaterProperties.SaturatedSteamDensity(pressure_psia);
            if (state.SteamCondensed_lbm > steamMass * 0.5f)
            {
                // Don’t condense more than half the steam in one timestep
                state.SteamCondensed_lbm = steamMass * 0.5f;
            }
            
            // Status message
            if (state.IsActive)
            {
                state.StatusMessage = $"Spray: {state.SprayFlow_GPM:F0} gpm, valve {state.ValvePosition * 100:F0}%, "
                    + $"ΔT={deltaT:F0}°F, condensed={state.SteamCondensed_lbm:F1} lbm";
            }
            else
            {
                state.StatusMessage = $"Spray bypass ({PlantConstants.SPRAY_BYPASS_FLOW_GPM:F1} gpm)";
            }
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate CVCS controller behavior.
        /// </summary>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: Initialization should produce valid state
            var state = Initialize(50f, 50f, 75f, 75f, 32f);
            if (!state.IsActive) valid = false;
            if (Math.Abs(state.LevelError) > 0.1f) valid = false;
            
            // Test 2: Low level should increase charging
            var stateLow = Initialize(40f, 50f, 75f, 75f, 32f);
            Update(ref stateLow, 40f, 400f, 1500f, 4, 1f/360f);
            if (stateLow.ChargingFlow <= 75f + 32f) valid = false; // Should be above base + seal
            
            // Test 3: High level should decrease charging
            var stateHigh = Initialize(60f, 50f, 75f, 75f, 32f);
            Update(ref stateHigh, 60f, 400f, 1500f, 4, 1f/360f);
            if (stateHigh.ChargingFlow >= 75f + 32f) valid = false; // Should be below base + seal
            
            // Test 4: Very low level should trigger isolation
            var stateIso = Initialize(15f, 50f, 75f, 75f, 32f);
            Update(ref stateIso, 15f, 400f, 1500f, 4, 1f/360f);
            if (!stateIso.LetdownIsolated) valid = false;
            if (stateIso.LetdownFlow != 0f) valid = false;
            
            // Test 5: Integral should accumulate over time
            var stateInt = Initialize(45f, 50f, 75f, 75f, 32f);
            float integral0 = stateInt.IntegralError;
            for (int i = 0; i < 100; i++)
                Update(ref stateInt, 45f, 400f, 1500f, 4, 1f/360f);
            if (Math.Abs(stateInt.IntegralError - integral0) < 0.1f) valid = false; // Should have accumulated
            
            // Test 6: Charging should be clamped to minimum (seal injection)
            var stateMin = Initialize(90f, 50f, 0f, 0f, 32f); // Extreme case
            Update(ref stateMin, 90f, 400f, 1500f, 4, 1f/360f);
            if (stateMin.ChargingFlow < 32f) valid = false; // Must provide seal injection
            
            // Test 7: Net flow calculation
            var stateNet = Initialize(50f, 50f, 100f, 80f, 32f);
            stateNet.ChargingFlow = 100f;
            stateNet.LetdownFlow = 80f;
            float netFlow = GetNetRCSFlow(stateNet);
            if (Math.Abs(netFlow - 20f) > 0.1f) valid = false;
            
            return valid;
        }
        
        #endregion
    }
}
