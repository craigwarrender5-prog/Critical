// CRITICAL: Master the Atom - Phase 1 Core Physics Engine
// CVCSController.Heaters.cs - Heater mode and PID control
//
// File: Assets/Scripts/Physics/CVCSController.Heaters.cs
// Module: Critical.Physics.CVCSController
// Responsibility: Heater mode logic and PID pressure controller.
// Standards: GOLD v1.0, SRP/SOLID
// Version: 1.0
// Last Updated: 2026-02-18
using System;
namespace Critical.Physics
{
    public static partial class CVCSController
    {
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
        ///   >= 400 psig for RCP startup permissive. Pressure-rate feedback active.
        /// 
        /// Mode 4 (AUTOMATIC_PID): Future scope â€” proportional + backup groups
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
            state.PressureRateLimited = false;
            state.RampRateLimited = false;
            state.TargetFraction = 1.0f;
            state.PressureRateAbsPsiHr = Math.Abs(pressureRate_psi_hr);
            
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
                state.TargetFraction = 0f;
                state.PressureRateLimited = false;
                state.RampRateLimited = false;
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
                    state.TargetFraction = 1.0f;
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
                    state.PressureRateAbsPsiHr = absPressureRate;
                    
                    if (absPressureRate > maxRate && maxRate > 0f)
                    {
                        targetFraction = 1.0f - (absPressureRate - maxRate) / maxRate;
                        targetFraction = Math.Max(minFraction, Math.Min(targetFraction, 1.0f));
                    }
                    state.TargetFraction = targetFraction;
                    state.PressureRateLimited = absPressureRate > maxRate && maxRate > 0f;
                    
                    // v2.0.10: Rate-limit the output change per timestep.
                    // Max change of 6.0 per hour (matches HEATER_RATE_LIMIT_PER_HR).
                    // At 10-sec timesteps (dt=1/360 hr), max change â‰ˆ 1.67% per step.
                    // Full travel 20%â†’100% takes ~2.9 minutes â€” realistic valve travel.
                    float currentSmoothed = smoothedOutput;
                    if (dt_hr > 0f)
                    {
                        float maxChangePerHr = PlantConstants.HEATER_RATE_LIMIT_PER_HR;
                        float maxStep = maxChangePerHr * dt_hr;
                        float delta = targetFraction - currentSmoothed;
                        state.RampRateLimited = Math.Abs(delta) > maxStep + 1e-6f;
                        delta = Math.Max(-maxStep, Math.Min(delta, maxStep));
                        currentSmoothed += delta;
                        currentSmoothed = Math.Max(minFraction, Math.Min(currentSmoothed, 1.0f));
                    }
                    else
                    {
                        // No dt provided â€” fall back to instantaneous (backward compat)
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
                // Target >= 400 psig for RCP startup permissive
                // ============================================================
                case HeaterMode.PRESSURIZE_AUTO:
                {
                    // v2.0.10: Rate-limited heater control (same as BUBBLE_FORMATION_AUTO)
                    float targetFraction = 1.0f;
                    float maxRate = PlantConstants.HEATER_STARTUP_MAX_PRESSURE_RATE;
                    float minFraction = PlantConstants.HEATER_STARTUP_MIN_POWER_FRACTION;
                    
                    float absPressureRate = Math.Abs(pressureRate_psi_hr);
                    state.PressureRateAbsPsiHr = absPressureRate;
                    
                    if (absPressureRate > maxRate && maxRate > 0f)
                    {
                        targetFraction = 1.0f - (absPressureRate - maxRate) / maxRate;
                        targetFraction = Math.Max(minFraction, Math.Min(targetFraction, 1.0f));
                    }
                    state.TargetFraction = targetFraction;
                    state.PressureRateLimited = absPressureRate > maxRate && maxRate > 0f;
                    
                    // v2.0.10: Rate-limit the output change per timestep
                    float currentSmoothed = smoothedOutput;
                    if (dt_hr > 0f)
                    {
                        float maxChangePerHr = PlantConstants.HEATER_RATE_LIMIT_PER_HR;
                        float maxStep = maxChangePerHr * dt_hr;
                        float delta = targetFraction - currentSmoothed;
                        state.RampRateLimited = Math.Abs(delta) > maxStep + 1e-6f;
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
                    state.TargetFraction = state.HeaterFraction;
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
                    state.TargetFraction = 0f;
                    state.StatusReason = "Heaters OFF";
                    break;
            }
            
            return state;
        }
        
        #endregion
        #region Heater PID Controller â€” v1.1.0 Stage 4
        
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
    }
}
