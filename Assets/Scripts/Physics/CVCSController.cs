// CRITICAL: Master the Atom - Phase 1 Core Physics Engine
// CVCSController.cs - CVCS Charging/Letdown Flow Control
//
// File: Assets/Scripts/Physics/CVCSController.cs
// Module: Critical.Physics.CVCSController
// Responsibility: CVCS flow/heater control laws and seal-demand accounting.
// Standards: GOLD v1.0, SRP/SOLID
// Version: 2.3
// Last Updated: 2026-02-17
// Changes:
//   - 2.3 (2026-02-17): Added GOLD metadata fields and bounded change history section.
//   - 2.2 (2026-02-17): Confirmed PRESSURIZE_AUTO startup minimum power constraints for IP-0048.
//   - 2.1 (2026-02-16): Expanded startup-hold heater mode support and authority reasons.
//   - 2.0 (2026-02-15): Integrated PI charging/letdown control contract.
//   - 1.0 (2026-02-14): Initial CVCS controller extraction from engine inline logic.
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
        
        /// <summary>Same auto controller, target >= 400 psig for RCP startup permissive.</summary>
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
        public bool PressureRateLimited; // True when pressure-rate limiter is active
        public bool RampRateLimited;     // True when output slew limiter is active
        public float TargetFraction;     // Unsmoothed fraction before ramp limiting
        public float PressureRateAbsPsiHr; // Absolute pressure rate used for limiter checks
        
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
    /// Source: NRC HRTD 10.2 â€” Pressurizer spray system modulates between
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
        
        /// <summary>Delta-T between PZR and spray water (Â°F)</summary>
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
    public static partial class CVCSController
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
        /// When num75Open >= 0, uses the new mixed-orifice model (2Ã—75 + 1Ã—45 gpm)
        /// with ion exchanger flow limit. Legacy callers default to single 75-gpm orifice.
        /// </summary>
        /// <param name="state">Controller state (modified in place)</param>
        /// <param name="currentLevel">Current PZR level (%)</param>
        /// <param name="T_avg">Average RCS temperature (Â°F)</param>
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
            // v0.4.0 Issue #2 fix: Was GetPZRLevelProgram (at-power only, clamps to 25% below 557Â°F).
            // Unified function uses heatup program below 557Â°F, at-power program above.
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
                // CVCSFlowMath.CalculateTotalLetdownFlow handles orifice sizing
                // and ion exchanger limit for the new multi-orifice model.
                state.LetdownFlow = CVCSFlowMath.CalculateTotalLetdownFlow(
                    T_avg, pressure_psia, numOrificesOpen: 1,
                    num75Open: num75Open, open45: open45);
            }
            
            // ================================================================
            // PI CHARGING CONTROLLER
            // Modulates charging to maintain PZR level at setpoint
            // ================================================================
            state.LevelError = currentLevel - state.LevelSetpoint;
            
            // Proportional term: immediate response to level error
            // Negative error (level low) â†’ increase charging
            // Positive error (level high) â†’ decrease charging
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
        
        #region Post-Bubble Heatup Control â€” NRC HRTD 10.2/10.3 Philosophy
        
        /// <summary>
        /// Update CVCS controller for post-bubble heatup phase using NRC HRTD
        /// 10.2/10.3 control philosophy: constant letdown, variable charging.
        ///
        /// Per NRC HRTD 10.3: "Letdown flow is set at a constant 75 gpm.
        /// Charging flow is varied by the PI level controller to maintain
        /// the programmed pressurizer level setpoint."
        ///
        /// This differs from the general Update() method which allows letdown
        /// to vary with orifice lineup and pressure. During post-bubble heatup,
        /// the control philosophy is deliberately simpler:
        ///   - Letdown: FIXED at 75 gpm (via RHR crossconnect below 350Â°F,
        ///     or via operator-adjusted orifice lineup above 350Â°F)
        ///   - Charging: VARIABLE via PI controller (20-130 gpm)
        ///   - Level setpoint: from unified level program
        ///   - Seal injection: added to charging demand
        ///
        /// The ~30,000 gallons of excess inventory from thermal expansion
        /// during heatup (100â†’557Â°F) is removed via the VCT divert system
        /// (LCV-112A â†’ BRS holdup tanks). The PI controller keeps level
        /// on-program by reducing charging below letdown, allowing the net
        /// negative CVCS flow to drain excess inventory through the VCT.
        ///
        /// Source: NRC HRTD Section 10.3 â€” Pressurizer Level Control
        ///         NRC HRTD Section 4.1 â€” CVCS Operations
        /// </summary>
        /// <param name="state">Controller state (modified in place)</param>
        /// <param name="currentLevel">Current PZR level (%)</param>
        /// <param name="T_avg">Average RCS temperature (Â°F)</param>
        /// <param name="pressure_psia">RCS pressure (psia)</param>
        /// <param name="rcpCount">Number of RCPs running</param>
        /// <param name="dt_hr">Timestep in hours</param>
        public static void UpdatePostBubbleHeatup(
            ref CVCSControllerState state,
            float currentLevel,
            float T_avg,
            float pressure_psia,
            int rcpCount,
            float dt_hr)
        {
            if (!state.IsActive) return;
            
            // ================================================================
            // LEVEL SETPOINT FROM UNIFIED PROGRAM
            // Heatup: 25% at 200Â°F â†’ 60% at 557Â°F
            // At-power: 25% at 557Â°F â†’ 61.5% at 584.7Â°F
            // ================================================================
            state.LevelSetpoint = PlantConstants.GetPZRLevelSetpointUnified(T_avg);
            
            // ================================================================
            // SEAL INJECTION DEMAND
            // ================================================================
            state.SealInjection = rcpCount * PlantConstants.SEAL_INJECTION_PER_PUMP_GPM;
            
            // ================================================================
            // LOW-LEVEL INTERLOCK CHECK (with hysteresis)
            // ================================================================
            if (currentLevel < PlantConstants.PZR_LOW_LEVEL_ISOLATION)
            {
                state.LetdownIsolated = true;
            }
            else if (currentLevel > PlantConstants.PZR_LOW_LEVEL_ISOLATION + 5f)
            {
                state.LetdownIsolated = false;
            }
            
            // ================================================================
            // LETDOWN FLOW â€” FIXED at 75 gpm
            // Per NRC HRTD 10.3: Constant letdown during heatup.
            // Below 350Â°F: RHR crossconnect provides 75 gpm.
            // Above 350Â°F: Operator adjusts orifice lineup to maintain ~75 gpm
            // at the current pressure (not modeled in detail â€” held at 75 gpm).
            // ================================================================
            if (state.LetdownIsolated)
            {
                state.LetdownFlow = 0f;
            }
            else
            {
                state.LetdownFlow = PlantConstants.HEATUP_FIXED_LETDOWN_GPM;
            }
            
            // ================================================================
            // PI CHARGING CONTROLLER
            // Variable charging to maintain programmed level setpoint.
            // This is the sole active control mechanism for level.
            // ================================================================
            state.LevelError = currentLevel - state.LevelSetpoint;
            
            // Proportional: negative error (level low) â†’ increase charging
            float pCorrection = PlantConstants.CVCS_LEVEL_KP * (-state.LevelError);
            
            // Integral: accumulated error drives steady-state correction
            float dt_sec = dt_hr * 3600f;
            state.IntegralError += (-state.LevelError) * dt_sec;
            
            // Anti-windup
            float integralLimit = PlantConstants.CVCS_LEVEL_INTEGRAL_LIMIT / PlantConstants.CVCS_LEVEL_KI;
            state.IntegralError = Math.Max(-integralLimit, Math.Min(state.IntegralError, integralLimit));
            
            float iCorrection = PlantConstants.CVCS_LEVEL_KI * state.IntegralError;
            
            // Base charging = fixed letdown + seal injection (maintains balance)
            float baseCharging = state.LetdownFlow + state.SealInjection;
            
            // Apply PI corrections
            state.ChargingFlow = baseCharging + pCorrection + iCorrection;
            
            // Clamp to post-bubble heatup limits
            // Minimum: must provide seal injection OR HEATUP_MIN, whichever is higher
            // Maximum: CCP capacity limit during heatup
            float minCharging = Math.Max(state.SealInjection, PlantConstants.HEATUP_MIN_CHARGING_GPM);
            state.ChargingFlow = Math.Max(minCharging, Math.Min(state.ChargingFlow, PlantConstants.HEATUP_MAX_CHARGING_GPM));
            
            state.LastLevelError = state.LevelError;
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
        /// <param name="T_avg">Current average temperature (Â°F)</param>
        /// <param name="rcpCount">Number of RCPs about to run</param>
        public static void PreSeedForRCPStart(
            ref CVCSControllerState state,
            float currentLevel,
            float T_avg,
            int rcpCount)
        {
            // Calculate what the steady-state integral should be
            // At equilibrium: charging = letdown + seal, so integral â‰ˆ 0
            // But pre-seed with small positive value to bias toward recovery
            // from the expected level drop at RCP start
            float sealDemand = rcpCount * PlantConstants.SEAL_INJECTION_PER_PUMP_GPM;
            float expectedBase = state.LetdownFlow + sealDemand;
            
            // v0.4.0 Issue #3 Part C: Scale pre-seed with temperature differential.
            // A larger Î”T between PZR (~Tsat) and RCS (~T_avg) means a bigger
            // expected transient from thermal mixing, needing a stronger
            // initial charging bias for faster level recovery.
            // Proxy: At low T_avg the PZR-RCS Î”T is large; at higher T_avg it shrinks.
            // At typical first RCP start (T_avg ~200Â°F, Î”T ~360Â°F): ~14 gpm pre-seed.
            // At subsequent starts (T_avg ~300Â°F, Î”T ~250Â°F): ~11 gpm pre-seed.
            float deltaT_proxy = Math.Max(0f, 400f - T_avg);  // Rough estimate of PZR-RCS Î”T
            float preSeedCharging_gpm = 5f + 10f * deltaT_proxy / 400f;
            if (PlantConstants.CVCS_LEVEL_KI > 0f)
            {
                state.IntegralError = preSeedCharging_gpm / PlantConstants.CVCS_LEVEL_KI;
            }
            
            // v0.4.0 Issue #2 fix: Was GetPZRLevelProgram (at-power only, clamps to 25% below 557Â°F).
            // Unified function uses heatup program below 557Â°F, at-power program above.
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
        // Source: NRC HRTD 10.2 â€” Pressurizer Pressure Control
        //
        // Two spray valves fed from cold legs (Loops B and C).
        // Modulated by the master pressure controller:
        //   - Open linearly from 2260 psig (start) to 2310 psig (full open)
        //   - Continuous bypass flow of ~1.5 gpm
        //   - Maximum spray flow ~600 gpm at rated Î”P
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
        /// <param name="T_pzr">Current PZR temperature (Â°F) â€” should be T_sat</param>
        /// <param name="T_cold">Cold leg temperature (Â°F) â€” spray water source</param>
        /// <param name="pzrSteamVolume_ft3">Current PZR steam volume (ftÂ³)</param>
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
            // Spray requires at least one RCP for Î”P driving force
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
            // Q_absorbed = m_spray Ã— Cp Ã— (T_sat - T_spray) Ã— efficiency
            // m_condensed = Q_absorbed / h_fg
            // ==============================================================
            float T_sat = T_pzr;  // PZR water/steam at saturation
            float deltaT = T_sat - T_cold;
            state.SprayDeltaT = deltaT;
            
            // Safety check: donâ€™t spray if deltaT exceeds thermal shock limit
            // Real plant would alarm but continue; we log and reduce flow
            float effectiveFlow = state.SprayFlow_GPM;
            if (deltaT > PlantConstants.MAX_PZR_SPRAY_DELTA_T)
            {
                // Limit spray to bypass only to prevent thermal shock
                effectiveFlow = PlantConstants.SPRAY_BYPASS_FLOW_GPM;
                state.StatusMessage = $"SPRAY Î”T ALARM: {deltaT:F0}Â°F > {PlantConstants.MAX_PZR_SPRAY_DELTA_T:F0}Â°F limit";
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
                    state.StatusMessage = $"Spray: {state.SprayFlow_GPM:F0} gpm (no Î”T)";
                return;
            }
            
            // Spray mass this timestep
            float dt_sec = dt_hr * 3600f;
            float rho_spray = WaterProperties.WaterDensity(T_cold, pressure_psia);
            float sprayMass_lbm = effectiveFlow * dt_sec * PlantConstants.GPM_TO_FT3_SEC * rho_spray;
            
            // Heat absorbed by spray water: Q = m Ã— Cp Ã— Î”T Ã— efficiency
            // Cp â‰ˆ 1.0 BTU/(lbmÂ·Â°F) for subcooled water (conservative)
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
                // Donâ€™t condense more than half the steam in one timestep
                state.SteamCondensed_lbm = steamMass * 0.5f;
            }
            
            // Status message
            if (state.IsActive)
            {
                state.StatusMessage = $"Spray: {state.SprayFlow_GPM:F0} gpm, valve {state.ValvePosition * 100:F0}%, "
                    + $"Î”T={deltaT:F0}Â°F, condensed={state.SteamCondensed_lbm:F1} lbm";
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





