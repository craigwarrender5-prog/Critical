// ============================================================================
// CRITICAL: Master the Atom - Plant Constants (Pressurizer)
// PlantConstants.Pressurizer.cs - Pressurizer Geometry, Bubble Formation,
//                                  Heater Control, Aux Spray, Level Program
// ============================================================================
//
// DOMAIN: Pressurizer vessel geometry, bubble formation timing/conditions,
//         heater control setpoints, auxiliary spray test parameters,
//         PZR level program (heatup and at-power)
//
// SOURCES:
//   - NRC HRTD 6.1 — Pressurizer Heaters
//   - NRC HRTD 10.2 — Pressurizer Pressure Control
//   - NRC HRTD 10.3 — Pressurizer Level Control (At-Power Level Program)
//   - NRC HRTD 19.2.1 — Solid Plant Operations
//   - NRC HRTD 19.2.2 — Bubble Formation Procedure, Aux Spray Test
//   - NRC HRTD 4.1 — CVCS / PZR Level Program Reference
//   - Westinghouse FSAR Chapter 7 — Level Program
//
// UNITS:
//   Temperature: °F | Pressure: psia/psig | Volume: ft³
//   Power: kW | Level: % | Time: hours (unless noted)
//
// NOTE: This is a partial class. All constants are accessed as
//       PlantConstants.CONSTANT_NAME regardless of which file they live in.
//
// GOLD STANDARD: Yes
// ============================================================================

using System;

namespace Critical.Physics
{
    public static partial class PlantConstants
    {
        #region PZR Baseline Authority (IP-0024 Stage A)

        // Single authoritative PZR baseline profile.
        // All duplicate families in other PlantConstants partials alias this set.

        public const float PZR_BASELINE_TOTAL_VOLUME_FT3 = 1800f;
        public const float PZR_BASELINE_HEATER_TOTAL_KW = 1794f;
        public const float PZR_BASELINE_HEATER_PROP_KW = 414f;
        public const float PZR_BASELINE_HEATER_BACKUP_KW = 1380f;
        public const float PZR_BASELINE_SPRAY_MAX_GPM = 840f;

        public const float PZR_BASELINE_PRESSURE_SETPOINT_PSIG = 2235f;
        public const float PZR_BASELINE_PROP_HEATER_FULL_ON_PSIG = 2220f;
        public const float PZR_BASELINE_PROP_HEATER_ZERO_PSIG = 2250f;
        public const float PZR_BASELINE_BACKUP_HEATER_ON_PSIG = 2210f;
        public const float PZR_BASELINE_BACKUP_HEATER_OFF_PSIG = 2217f;
        public const float PZR_BASELINE_SPRAY_START_PSIG = 2260f;
        public const float PZR_BASELINE_SPRAY_FULL_PSIG = 2310f;
        public const float PZR_BASELINE_PORV_OPEN_PSIG = 2335f;

        public const float PZR_BASELINE_LEVEL_NO_LOAD_PERCENT = 25f;
        public const float PZR_BASELINE_LEVEL_FULL_POWER_PERCENT = 61.5f;
        public const float PZR_BASELINE_LEVEL_TAVG_NO_LOAD_F = 557f;
        public const float PZR_BASELINE_LEVEL_TAVG_FULL_POWER_F = 584.7f;
        public const float PZR_BASELINE_LEVEL_PROGRAM_SLOPE =
            (PZR_BASELINE_LEVEL_FULL_POWER_PERCENT - PZR_BASELINE_LEVEL_NO_LOAD_PERCENT) /
            (PZR_BASELINE_LEVEL_TAVG_FULL_POWER_F - PZR_BASELINE_LEVEL_TAVG_NO_LOAD_F);

        #endregion

        #region Pressurizer Geometry
        
        /// <summary>Total pressurizer volume in ft³</summary>
        public const float PZR_TOTAL_VOLUME = PZR_BASELINE_TOTAL_VOLUME_FT3;
        
        /// <summary>Normal water volume in ft³ (60% level)</summary>
        public const float PZR_WATER_VOLUME = 1080f;
        
        /// <summary>Normal steam volume in ft³ (40% level)</summary>
        public const float PZR_STEAM_VOLUME = 720f;
        
        /// <summary>Pressurizer height in ft</summary>
        public const float PZR_HEIGHT = 52.75f;
        
        /// <summary>Pressurizer wall mass in lb</summary>
        public const float PZR_WALL_MASS = 200000f;
        
        /// <summary>Pressurizer wall surface area in ft²</summary>
        public const float PZR_WALL_AREA = 600f;
        
        /// <summary>Total heater power in kW</summary>
        public const float HEATER_POWER_TOTAL = PZR_BASELINE_HEATER_TOTAL_KW;
        
        /// <summary>
        /// Proportional heater power in kW.
        /// Source: NRC HRTD 6.1 — 2 banks × 150 kW = 300 kW standard proportional capacity
        /// Corrected per validation issue #2: was 500 kW, should match standard 2×150 kW banks
        /// </summary>
        public const float HEATER_POWER_PROP = PZR_BASELINE_HEATER_PROP_KW;
        
        /// <summary>
        /// Backup heater power in kW.
        /// Source: NRC HRTD 6.1 — corrected to HEATER_POWER_TOTAL - HEATER_POWER_PROP
        /// 1800 - 300 = 1500 kW (5 banks × 300 kW for larger 4-loop plant)
        /// </summary>
        public const float HEATER_POWER_BACKUP = PZR_BASELINE_HEATER_BACKUP_KW;
        
        /// <summary>Heater thermal time constant in seconds</summary>
        public const float HEATER_TAU = 20f;
        
        /// <summary>Maximum spray flow rate in gpm</summary>
        public const float SPRAY_FLOW_MAX = PZR_BASELINE_SPRAY_MAX_GPM;
        
        /// <summary>Spray water temperature in °F (= Tcold)</summary>
        public const float SPRAY_TEMP = 558f;
        
        /// <summary>Spray condensation efficiency (0.85 = 85%)</summary>
        public const float SPRAY_EFFICIENCY = 0.85f;
        
        /// <summary>Minimum steam space volume in ft³</summary>
        public const float PZR_STEAM_MIN = 50f;
        
        /// <summary>Maximum water volume in ft³</summary>
        public const float PZR_WATER_MAX = 1750f;
        
        /// <summary>
        /// Maximum pressurizer heatup rate in °F/hr.
        /// Source: Tech Specs limit to protect vessel thermal stress
        /// </summary>
        public const float MAX_PZR_HEATUP_RATE_F_HR = 100f;
        
        /// <summary>
        /// Maximum spray-to-pressurizer delta-T in °F.
        /// Source: NRC tech specs — thermal shock protection
        /// </summary>
        public const float MAX_PZR_SPRAY_DELTA_T = 320f;
        
        /// <summary>
        /// Target pressurizer level after bubble formation (%).
        /// Source: NRC ML11223A342 Section 19.2.2 — establish level at ~25%
        /// </summary>
        public const float PZR_LEVEL_AFTER_BUBBLE = 25f;
        
        #endregion
        
        #region Bubble Formation (Cold Startup) — Per NRC HRTD 19.2.1 / 19.2.2
        
        // =====================================================================
        // STANDARD PROCEDURE: Solid Plant Operations (NRC HRTD 19.2.1)
        // RCS pressurized to 320-400 psig via charging/letdown flow balance.
        // Bubble forms when PZR water reaches T_sat at system pressure (~430-450°F).
        // This is the default startup procedure for Westinghouse 4-loop PWRs.
        // =====================================================================
        
        /// <summary>
        /// Temperature at which steam bubble forms during standard cold startup in °F.
        /// At solid plant operating pressure (~350 psig / 365 psia), T_sat ≈ 435°F.
        /// Source: NRC HRTD 19.2.2 — bubble forms at saturation temperature at system pressure
        /// </summary>
        public const float BUBBLE_FORMATION_TEMP_F = 435f;
        
        /// <summary>
        /// Pressure at which steam bubble forms during standard cold startup in psig.
        /// Source: NRC HRTD 19.2.2 — bubble formation occurs at solid plant operating pressure
        /// Operator drains PZR to establish ~25% level after bubble appears.
        /// </summary>
        public const float BUBBLE_FORMATION_PRESSURE_PSIG = 350f;
        
        /// <summary>
        /// Duration of bubble detection phase in sim hours.
        /// PZR water reaches T_sat, first steam appears at heater surfaces.
        /// Diagnostic: level drops slightly without corresponding pressure drop.
        /// </summary>
        public const float BUBBLE_PHASE_DETECTION_HR = 5f / 60f;  // 5 minutes
        
        /// <summary>
        /// Duration of bubble verification phase in sim hours.
        /// Operators close PORVs, test with auxiliary spray to confirm compressible gas.
        /// </summary>
        public const float BUBBLE_PHASE_VERIFY_HR = 5f / 60f;  // 5 minutes
        
        /// <summary>
        /// Duration of controlled drain phase in sim hours.
        /// Primary mechanism: Thermodynamic steam displacement (heaters convert
        /// liquid to vapor at T_sat, steam displaces water downward through surge line).
        /// Secondary mechanism: CVCS trim (75 gpm letdown, 0→44 gpm charging).
        /// Steam generation rate depends on heater power and latent heat demand.
        /// Typical procedural time: 30-60 min per NRC HRTD 19.2.2.
        /// </summary>
        public const float BUBBLE_PHASE_DRAIN_HR = 40f / 60f;  // 40 minutes typical
        
        /// <summary>
        /// Duration of stabilization phase in sim hours.
        /// CVCS rebalanced, level control transferred to automatic, PI initialized.
        /// </summary>
        public const float BUBBLE_PHASE_STABILIZE_HR = 10f / 60f;  // 10 minutes
        
        /// <summary>
        /// Minimum pressure required before bubble drain can begin (psia).
        /// PZR heaters must maintain sufficient pressure during drain.
        /// Source: NRC HRTD 19.2.2 — maintain pressure in 320-400 psig band.
        /// </summary>
        public const float BUBBLE_DRAIN_MIN_PRESSURE_PSIA = 334.7f;  // 320 psig

        /// <summary>
        /// Maximum pressure rate of change (psi/hr) for DRAIN→STABILIZE transition advisory.
        /// If pressure rate exceeds this at drain exit, a warning is logged but transition
        /// proceeds (advisory, not blocking — prevents infinite DRAIN).
        /// v0.3.0.0 Phase D (Fix 3.3, CS-0027/CS-0028): Continuity guard.
        /// </summary>
        public const float MAX_DRAIN_EXIT_PRESSURE_RATE = 50f;  // psi/hr
        
        // =====================================================================
        // ALTERNATE PROCEDURE: Nitrogen Bubble Startup (NRC HRTD 19.2.1)
        // RCS vented to atmosphere through PRT. Bubble forms at ~230°F / 6 psig.
        // Retained as alternate option, not the default.
        // =====================================================================
        
        /// <summary>
        /// Bubble formation temperature for nitrogen-bubble startup procedure in °F.
        /// Source: NRC HRTD 19.2.2 — "Steam formation begins at approximately 230°F at 6 psig"
        /// Only applies when RCS is vented to atmosphere (alternate procedure).
        /// </summary>
        public const float BUBBLE_FORMATION_TEMP_N2_F = 230f;
        
        /// <summary>
        /// Bubble formation pressure for nitrogen-bubble startup procedure in psig.
        /// Source: NRC HRTD 19.2.2 — low pressure procedure via PRT venting
        /// </summary>
        public const float BUBBLE_FORMATION_PRESSURE_N2_PSIG = 6f;
        
        #endregion
        
        #region Pressurizer Heater Control — Per NRC HRTD 6.1 / 10.2
        
        // =====================================================================
        // Heater Control Modes and Setpoints
        // Source: NRC HRTD 6.1 (Pressurizer Heaters), 10.2 (PZR Pressure Control)
        //
        // Startup: All groups manually energized at full power.
        // Bubble formation: Automatically modulated with pressure-rate feedback.
        // Pressurization: Same auto controller, target >= 400 psig for RCP startup permissive.
        // Normal ops: Proportional + backup groups, automatic PID on pressure.
        //
        // Design Decision (v0.2.0): Heaters modeled as continuously variable
        // block with automatic pressure-rate feedback. Future release adds
        // manual operator control with separate proportional/backup groups.
        // =====================================================================
        
        /// <summary>
        /// Maximum pressure rate of change during startup in psi/hr.
        /// If dP/dt exceeds this, heater power is reduced to prevent
        /// thermal stress and instrument overshoot.
        /// Source: NRC HRTD 19.0 — typical startup rate 50-100 psi/hr.
        /// </summary>
        public const float HEATER_STARTUP_MAX_PRESSURE_RATE = 100f;
        
        /// <summary>
        /// Minimum heater power fraction during pressure-rate modulation.
        /// Even when reducing power to control pressure rate, heaters
        /// maintain this minimum to prevent stalling the heatup.
        /// Source: Engineering judgment — maintain 20% minimum.
        /// </summary>
        public const float HEATER_STARTUP_MIN_POWER_FRACTION = 0.2f;
        
        /// <summary>
        /// Proportional heaters full ON pressure setpoint in psia.
        /// Source: NRC HRTD 10.2 — proportional heaters modulate between
        /// 2225 psia (full ON) and 2275 psia (zero output).
        /// Note: Normal ops only — defined here for future Phase 3+ scope.
        /// </summary>
        public const float P_PROP_HEATER_FULL_ON = PZR_BASELINE_PROP_HEATER_FULL_ON_PSIG;
        
        /// <summary>
        /// Proportional heaters zero output pressure setpoint in psia.
        /// Source: NRC HRTD 10.2 — zero heater output at 2275 psia.
        /// Note: Normal ops only — defined here for future Phase 3+ scope.
        /// </summary>
        public const float P_PROP_HEATER_ZERO = PZR_BASELINE_PROP_HEATER_ZERO_PSIG;
        
        /// <summary>
        /// Backup heaters ON (bistable) pressure setpoint in psia.
        /// Source: NRC HRTD 10.2 — backup heaters energize at 2200 psia.
        /// Note: Normal ops only — defined here for future Phase 3+ scope.
        /// </summary>
        public const float P_BACKUP_HEATER_ON = PZR_BASELINE_BACKUP_HEATER_ON_PSIG;
        
        /// <summary>
        /// Backup heaters OFF (bistable) pressure setpoint in psia.
        /// Source: NRC HRTD 10.2 — backup heaters de-energize at 2225 psia.
        /// Note: Normal ops only — defined here for future Phase 3+ scope.
        /// </summary>
        public const float P_BACKUP_HEATER_OFF = PZR_BASELINE_BACKUP_HEATER_OFF_PSIG;
        
        /// <summary>
        /// Spray valves start opening pressure setpoint in psig.
        /// Source: NRC HRTD 10.2 — spray valves begin opening at 2260 psig.
        /// Note: Normal ops only — defined here for future Phase 3+ scope.
        /// </summary>
        public const float P_SPRAY_START_PSIG = PZR_BASELINE_SPRAY_START_PSIG;
        
        /// <summary>
        /// Spray valves fully open pressure setpoint in psig.
        /// Source: NRC HRTD 10.2 — spray valves fully open at 2310 psig.
        /// Note: Normal ops only — defined here for future Phase 3+ scope.
        /// </summary>
        public const float P_SPRAY_FULL_PSIG = PZR_BASELINE_SPRAY_FULL_PSIG;
        
        /// <summary>
        /// Spray bypass flow in gpm.
        /// Source: NRC HRTD 10.2 — continuous bypass flow through spray
        /// valves to prevent thermal stratification in spray piping.
        /// </summary>
        public const float SPRAY_BYPASS_FLOW_GPM = 1.5f;
        
        /// <summary>
        /// v4.4.0: Spray valve fully open flow rate at rated ΔP in gpm.
        /// Per NRC HRTD 10.2: Both spray valves fully open at max capacity.
        /// Realistic full-open flow ~400-800 gpm depending on ΔP.
        /// During heatup, ΔP is lower than at-power; 600 gpm is representative.
        /// Source: Westinghouse 4-Loop FSAR, NRC HRTD 10.2
        /// </summary>
        public const float SPRAY_FULL_OPEN_FLOW_GPM = PZR_BASELINE_SPRAY_MAX_GPM;
        
        /// <summary>
        /// v4.4.0: Spray valve position time constant in hours.
        /// Models valve travel time from closed to fully open (~30 seconds).
        /// Source: Typical motor-operated valve travel time for PZR spray valves.
        /// </summary>
        public const float SPRAY_VALVE_TAU_HR = 30f / 3600f;  // 30 seconds
        
        /// <summary>
        /// Pressurizer ambient heat loss in kW.
        /// Source: NRC HRTD 6.1 — proportional heaters compensate for
        /// ambient losses of approximately 42.5 kW during normal operations.
        /// </summary>
        public const float AMBIENT_HEAT_LOSS_KW = 42.5f;
        
        /// <summary>
        /// v4.4.0: Pressure threshold for transitioning heater mode from
        /// PRESSURIZE_AUTO to AUTOMATIC_PID (psia).
        /// 
        /// At ~2200 psia (~2185 psig), PZR pressure is within the operating
        /// band where PID control is appropriate. The PRESSURIZE_AUTO mode’s
        /// 20% minimum power floor would cause overpressure above this point.
        /// 
        /// Source: NRC HRTD 10.2 — normal pressure control at 2235 psig uses
        /// proportional + backup heater groups with PID feedback.
        /// Engineering: Transition ~50 psi below setpoint gives PID time
        /// to stabilize before reaching the 2235 psig target.
        /// </summary>
        public const float HEATER_MODE_TRANSITION_PRESSURE_PSIA = 2200f;
        
        #endregion
        
        #region Auxiliary Spray Test — Per NRC HRTD 19.2.2
        
        // =====================================================================
        // Aux Spray Test During Bubble Formation Verification
        // Source: NRC HRTD 19.2.2 — operators test with aux spray to confirm
        // compressible gas (steam) in pressurizer. A 5-15 psi drop confirms
        // a steam space exists. No drop = still water-solid.
        // =====================================================================
        
        /// <summary>
        /// Duration of auxiliary spray test in seconds.
        /// Source: NRC HRTD 19.2.2 — brief spray test lasting ~45 seconds.
        /// </summary>
        public const float AUX_SPRAY_TEST_DURATION_SEC = 45f;
        
        /// <summary>
        /// Minimum pressure drop expected during aux spray test (psi).
        /// Source: NRC HRTD 19.2.2 — 5-15 psi drop confirms compressible gas.
        /// If drop is less than this, bubble may not be fully established.
        /// </summary>
        public const float AUX_SPRAY_MIN_PRESSURE_DROP = 5f;
        
        /// <summary>
        /// Maximum expected pressure drop during aux spray test (psi).
        /// Source: NRC HRTD 19.2.2 — 5-15 psi range for normal confirmation.
        /// </summary>
        public const float AUX_SPRAY_MAX_PRESSURE_DROP = 15f;
        
        /// <summary>
        /// Aux spray flow rate during verification test in gpm.
        /// Source: NRC HRTD 19.2.2 — aux spray via CCP, approximately 20-30 gpm.
        /// </summary>
        public const float AUX_SPRAY_FLOW_GPM = 25f;
        
        /// <summary>
        /// Expected pressure recovery time after aux spray test in seconds.
        /// Source: NRC HRTD 19.2.2 — pressure recovers within 2-3 minutes
        /// via heater action after aux spray is secured.
        /// </summary>
        public const float AUX_SPRAY_RECOVERY_SEC = 150f;
        
        #endregion
        
        #region PZR Level Program — Heatup Phase (200-557°F)
        
        // =====================================================================
        // HEATUP LEVEL PROGRAM: Covers cold shutdown through Mode 3 entry
        //   Range: 200°F (25%) → 557°F (60%)
        //   Governs CVCS level control during heatup to hot standby.
        //
        // AT-POWER LEVEL PROGRAM: See GetPZRLevelProgram()
        //   Range: 557°F (25%) → 584.7°F (61.5%)
        //   Governs CVCS level control during power operation.
        //   Ref: NRC HRTD 10.3, Figure 10.3-2
        //
        // These are COMPLEMENTARY programs for different operating regimes.
        // Use GetPZRLevelSetpointUnified() for seamless coverage.
        // =====================================================================
        
        /// <summary>
        /// PZR level setpoint at cold conditions in %.
        /// Lower level during cold conditions to allow for expansion.
        /// Source: NRC HRTD 4.1, Westinghouse FSAR Chapter 7
        /// </summary>
        public const float PZR_LEVEL_COLD_PERCENT = 25f;
        
        /// <summary>
        /// PZR level setpoint at hot standby conditions in %.
        /// Source: NRC HRTD 4.1, Westinghouse FSAR Chapter 7
        /// </summary>
        public const float PZR_LEVEL_HOT_PERCENT = 60f;
        
        /// <summary>
        /// Temperature range low end for heatup level program in °F.
        /// Level increases linearly from cold to hot setpoint.
        /// Source: NRC HRTD 4.1
        /// </summary>
        public const float PZR_HEATUP_LEVEL_T_LOW = 200f;
        
        /// <summary>
        /// Temperature range high end for heatup level program in °F.
        /// Source: NRC HRTD 4.1
        /// </summary>
        public const float PZR_HEATUP_LEVEL_T_HIGH = 557f;
        
        #endregion
        
        #region PZR Level Program Methods
        
        /// <summary>
        /// PZR Level Program (At-Power) per NRC HRTD Section 10.3, Figure 10.3-2.
        /// Linear 25% to 61.5% driven by auctioneered-high Tavg over 557-584.7°F range.
        /// Below 557°F: clamped at 25% (no-load minimum).
        /// Above 584.7°F: clamped at 61.5% (full-load maximum).
        /// </summary>
        public static float GetPZRLevelProgram(float T_avg_F)
        {
            if (T_avg_F <= PZR_LEVEL_PROGRAM_TAVG_LOW)
                return PZR_LEVEL_PROGRAM_MIN;
            if (T_avg_F >= PZR_LEVEL_PROGRAM_TAVG_HIGH)
                return PZR_LEVEL_PROGRAM_MAX;
            return PZR_LEVEL_PROGRAM_MIN + PZR_LEVEL_PROGRAM_SLOPE * (T_avg_F - PZR_LEVEL_PROGRAM_TAVG_LOW);
        }
        
        /// <summary>
        /// Get PZR level setpoint for given T_avg during HEATUP (200-557°F).
        /// Linear interpolation from 25% at 200°F to 60% at 557°F.
        /// For at-power operations, use GetPZRLevelProgram().
        /// For seamless full-range coverage, use GetPZRLevelSetpointUnified().
        /// Source: NRC HRTD 4.1, Westinghouse FSAR Chapter 7
        /// </summary>
        public static float GetPZRLevelSetpoint(float T_avg)
        {
            if (T_avg <= PZR_HEATUP_LEVEL_T_LOW)
                return PZR_LEVEL_COLD_PERCENT;
            if (T_avg >= PZR_HEATUP_LEVEL_T_HIGH)
                return PZR_LEVEL_HOT_PERCENT;
            
            float fraction = (T_avg - PZR_HEATUP_LEVEL_T_LOW) /
                           (PZR_HEATUP_LEVEL_T_HIGH - PZR_HEATUP_LEVEL_T_LOW);
            return PZR_LEVEL_COLD_PERCENT + fraction * (PZR_LEVEL_HOT_PERCENT - PZR_LEVEL_COLD_PERCENT);
        }
        
        /// <summary>
        /// Unified PZR level setpoint covering full operating range (200-585°F).
        /// Below 557°F: Uses heatup program (25-60% over 200-557°F).
        /// At/above 557°F: Delegates to at-power program (25-61.5% over 557-584.7°F).
        /// Source: NRC HRTD 4.1, 10.3
        /// </summary>
        public static float GetPZRLevelSetpointUnified(float T_avg)
        {
            if (T_avg < PZR_HEATUP_LEVEL_T_HIGH)
                return GetPZRLevelSetpoint(T_avg);
            else
                return GetPZRLevelProgram(T_avg);
        }
        
        #endregion
    }
}
