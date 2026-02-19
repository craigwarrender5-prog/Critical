// CRITICAL: Master the Atom - Physics Module
// SolidPlantPressure.Constants.cs - Solid pressure control constants
//
// File: Assets/Scripts/Physics/SolidPlantPressure.Constants.cs
// Module: Critical.Physics.SolidPlantPressure
// Responsibility: Constant policy/tuning values for solid pressurizer control.
// Standards: GOLD v1.0, SRP/SOLID
// Version: 1.0
// Last Updated: 2026-02-18
using System;
namespace Critical.Physics
{
    public static partial class SolidPlantPressure
    {
        #region Constants
        
        // CVCS Pressure Controller Tuning
        // PI controller: adjusts net letdown to hold pressure at setpoint
        // Output is delta-letdown in gpm added to base letdown flow
        
        /// <summary>Proportional gain: gpm per psi of pressure error</summary>
        const float KP_PRESSURE = 0.5f;
        
        /// <summary>Integral gain: gpm per psiÂ·sec of accumulated error</summary>
        const float KI_PRESSURE = 0.02f;
        
        /// <summary>Integral windup limit in gpm</summary>
        const float INTEGRAL_LIMIT_GPM = 40f;
        
        /// <summary>Maximum CVCS can increase letdown above base flow (gpm)</summary>
        const float MAX_LETDOWN_ADJUSTMENT_GPM = 50f;
        
        /// <summary>Minimum letdown flow - cannot go below this (gpm)</summary>
        const float MIN_LETDOWN_GPM = 20f;

        // â”€â”€ CVCS Actuator Dynamics â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Real valves/pumps cannot change flow instantly. These constants
        // model the physical lag and slew-rate limits of the CVCS trim
        // actuators, preventing stepwise volume injections that produce
        // jagged pressure traces in the stiff water-solid system.

        /// <summary>
        /// First-order lag time constant for CVCS letdown adjustment (seconds).
        /// Models valve/pump inertia. Raw PI command is filtered through:
        ///   eff += (cmd - eff) * (dt / tau)
        /// Tunable: larger tau = smoother but slower response.
        /// </summary>
        const float CVCS_ACTUATOR_TAU_SEC = 10f;

        /// <summary>
        /// Maximum slew rate for effective letdown adjustment (gpm/sec).
        /// Caps the per-tick change in effective CVCS trim after the lag filter.
        /// Prevents pathological step changes even when the lag would allow them.
        /// 1.0 gpm/s â†’ max 60 gpm change over 1 minute (generous but bounded).
        /// </summary>
        const float CVCS_MAX_SLEW_GPM_PER_SEC = 1.0f;

        /// <summary>
        /// First-order filter time constant for pressure measurement fed to
        /// the PI controller (seconds). Models sensor response time and
        /// provides additional noise rejection. Only affects the controller
        /// input; displayed pressure remains unfiltered.
        /// Tunable: 0 = no filter, 2â€“5s = light filtering.
        /// </summary>
        const float CVCS_PRESSURE_FILTER_TAU_SEC = 3f;

        // â”€â”€ CVCS Transport Delay â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // In a real PWR, CVCS flow changes require transit through ~200 ft
        // of piping, the letdown heat exchanger, demineralizers, and the
        // volume control tank before the resulting volume change is realized
        // at the RCS boundary. This delay prevents the PI controller from
        // instantly cancelling thermal expansion in the same computation step.
        // Source: NRC HRTD 4.1 â€” CVCS piping transit times.

        /// <summary>
        /// Transport delay from PI controller output to RCS volume effect (seconds).
        /// Models piping transit, heat exchanger lag, and valve positioning time.
        /// At 60s with dt=10s, the delay buffer holds 6 steps.
        /// Tunable: 30â€“120s is physically realistic for PWR CVCS.
        /// </summary>
        const float CVCS_TRANSPORT_DELAY_SEC = 60f;

        /// <summary>
        /// Maximum number of delay buffer slots. Sized for worst-case:
        /// max delay (120s) / min timestep (5s) = 24 slots.
        /// Must be >= ceil(CVCS_TRANSPORT_DELAY_SEC / dt_sec) at runtime.
        /// </summary>
        const int DELAY_BUFFER_MAX_SLOTS = 24;

        /// <summary>
        /// Anti-windup threshold (gpm). When the difference between the current
        /// PI output (LetdownAdjustEff) and the delayed value being applied
        /// exceeds this threshold, the integral is frozen. This prevents
        /// windup during the dead-time window when the controller's output
        /// has not yet reached the process.
        /// </summary>
        const float ANTIWINDUP_DEADTIME_THRESHOLD_GPM = 0.5f;

        // Solid startup control policy:
        //   PREHEATER_CVCS     -> CVCS-dominant pressurization (heaters locked out in engine)
        //   HEATER_PRESSURIZE  -> heater-led rise with bounded CVCS trim
        //   HOLD_SOLID         -> PI hold near setpoint

        /// <summary>
        /// Target pre-heater pressure-rate envelope low bound (psi/hr).
        /// </summary>
        const float PREHEATER_TARGET_RATE_MIN_PSI_HR = 50f;

        /// <summary>
        /// Target pre-heater pressure-rate envelope high bound (psi/hr).
        /// </summary>
        const float PREHEATER_TARGET_RATE_MAX_PSI_HR = 100f;

        /// <summary>
        /// Pre-heater net charging lower bound (gpm).
        /// Calibrated so the current compressibility model tracks the
        /// 50-100 psi/hr startup pressure-rate envelope.
        /// </summary>
        const float PREHEATER_EFFECTIVE_MIN_NET_CHARGING_GPM = 0.35f;

        /// <summary>
        /// Pre-heater net charging upper bound (gpm).
        /// </summary>
        const float PREHEATER_EFFECTIVE_MAX_NET_CHARGING_GPM = 0.70f;

        /// <summary>
        /// Pressure threshold (psia) for handoff from PREHEATER_CVCS to heater-led stage.
        /// </summary>
        const float PREHEATER_HANDOFF_PRESSURE_PSIA = PlantConstants.PRESSURIZE_COMPLETE_PRESSURE_PSIA;

        /// <summary>
        /// Dwell time above handoff threshold before enabling heater-led stage.
        /// </summary>
        const float PREHEATER_HANDOFF_DWELL_SEC = PlantConstants.PRESSURIZE_STABILITY_TIME_HR * 3600f;

        /// <summary>
        /// Maximum net CVCS trim (gpm) allowed during HEATER_PRESSURIZE.
        /// Limits the PI controller's effective authority so that pressure
        /// rise is dominated by thermal expansion, not volume injection.
        /// Positive = net letdown reduction (adds volume, raises P).
        /// Â±1.0 gpm keeps CVCS near balanced during heatup.
        /// </summary>
        const float HEATER_PRESS_MAX_NET_TRIM_GPM = 1.0f;

        /// <summary>
        /// Band around final setpoint (psia) to qualify for HOLD_SOLID entry.
        /// |P_actual - P_setpoint| must stay within this band for the
        /// hold-entry dwell period before transitioning.
        /// </summary>
        const float HOLD_ENTRY_BAND_PSI = 5f;

        /// <summary>
        /// Dwell time (seconds) that pressure must remain within
        /// HOLD_ENTRY_BAND_PSI of setpoint before entering HOLD_SOLID.
        /// Prevents chatter at the boundary. 30s = reasonable for a
        /// slowly-changing solid plant system.
        /// </summary>
        const float HOLD_ENTRY_DWELL_SEC = 30f;

        /// <summary>
        /// If pressure drops more than this below setpoint while in
        /// HOLD_SOLID, revert to HEATER_PRESSURIZE (optional safety net).
        /// Set generously to avoid nuisance re-entries.
        /// </summary>
        const float HOLD_EXIT_DROP_PSI = 15f;
        
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
    }
}
