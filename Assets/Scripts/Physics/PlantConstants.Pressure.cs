// ============================================================================
// CRITICAL: Master the Atom - Plant Constants (Pressure & RCP)
// PlantConstants.Pressure.cs - Pressure Setpoints, RCPs, RHR, Solid Plant
// ============================================================================
//
// DOMAIN: Normal/trip/safety pressure setpoints, solid plant pressure control,
//         Reactor Coolant Pumps, Residual Heat Removal system limits
//
// SOURCES:
//   - NRC HRTD 10.2 — Pressurizer Pressure Control
//   - NRC HRTD 10.2.3.2 — Reactor Trip Setpoints
//   - NRC HRTD 19.2.1 — Solid Plant Operations (320-400 psig band)
//   - NRC ML11223A342 Section 19.2.2 — RCP Pressure Requirements
//   - Westinghouse 4-Loop FSAR — RCP Specifications
//
// UNITS:
//   Pressure: psia/psig | Flow: gpm | Speed: rpm
//   Temperature: °F | Time: seconds
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
        #region Pressure Setpoints (psia and psig)
        
        /// <summary>Normal operating pressure in psia</summary>
        public const float P_NORMAL = 2250f;
        
        /// <summary>Normal operating pressure in psig</summary>
        public const float P_NORMAL_PSIG = 2235f;
        
        /// <summary>Heaters full ON setpoint in psig</summary>
        public const float P_HEATERS_ON = 2210f;
        
        /// <summary>Heaters OFF setpoint in psig</summary>
        public const float P_HEATERS_OFF = 2235f;
        
        /// <summary>Spray ON setpoint in psig</summary>
        public const float P_SPRAY_ON = 2260f;
        
        /// <summary>
        /// Spray full flow setpoint in psig.
        /// Source: NRC HRTD 10.2 — "2310 psig (75 psig above setpoint) for spray valves to fully open"
        /// </summary>
        public const float P_SPRAY_FULL = 2310f;
        
        /// <summary>PORV opening setpoint in psig</summary>
        public const float P_PORV = 2335f;
        
        /// <summary>Safety valve opening setpoint in psig</summary>
        public const float P_SAFETY = 2485f;
        
        /// <summary>High pressure reactor trip setpoint in psig</summary>
        public const float P_TRIP_HIGH = 2385f;
        
        /// <summary>
        /// Low pressure reactor trip setpoint in psig.
        /// Source: NRC HRTD 10.2.3.2 — "generated when pressurizer pressure decreases to 1865 psig"
        /// </summary>
        public const float P_TRIP_LOW = 1865f;
        
        #endregion
        
        #region Solid Plant Pressure Control — Per NRC HRTD 19.2.1
        
        // =====================================================================
        // "The pressure in the RCS is controlled between 320 and 400 psig by
        // maintaining a flow balance between the coolant removed from the RCS
        // and the coolant being returned."
        // =====================================================================
        
        /// <summary>
        /// Initial RCS pressure during cold shutdown solid plant operations in psia.
        /// Source: NRC HRTD 19.2.1 — midband of 320-400 psig operating range
        /// </summary>
        public const float SOLID_PLANT_INITIAL_PRESSURE_PSIA = 365f;   // 350 psig
        
        /// <summary>
        /// Solid plant pressure control low limit in psig.
        /// Source: NRC HRTD 19.2.1 — lower bound of CVCS pressure control band
        /// </summary>
        public const float SOLID_PLANT_P_LOW_PSIG = 320f;
        
        /// <summary>
        /// Solid plant pressure control high limit in psig.
        /// Source: NRC HRTD 19.2.1 — RHR relief valve setpoint (must stay below)
        /// </summary>
        public const float SOLID_PLANT_P_HIGH_PSIG = 450f;
        
        /// <summary>
        /// Solid plant pressure control low limit in psia.
        /// = SOLID_PLANT_P_LOW_PSIG + 14.7 = 334.7 psia
        /// </summary>
        public const float SOLID_PLANT_P_LOW_PSIA = 334.7f;
        
        /// <summary>
        /// Solid plant pressure control high limit in psia.
        /// = SOLID_PLANT_P_HIGH_PSIG + 14.7 = 464.7 psia
        /// </summary>
        public const float SOLID_PLANT_P_HIGH_PSIA = 464.7f;
        
        /// <summary>
        /// Solid plant pressure control setpoint in psia (midband).
        /// CVCS charging/letdown balance targets this pressure.
        /// </summary>
        public const float SOLID_PLANT_P_SETPOINT_PSIA = 365f;  // 350 psig
        
        #endregion
        
        #region Reactor Coolant Pumps (RCPs)
        
        /// <summary>Number of RCPs</summary>
        public const int RCP_COUNT = 4;
        
        /// <summary>Flow per RCP in gpm</summary>
        public const float RCP_FLOW_EACH = 97600f;
        
        /// <summary>RCP speed in rpm</summary>
        public const float RCP_SPEED = 1189f;
        
        /// <summary>RCP coastdown time constant in seconds</summary>
        public const float RCP_COASTDOWN_TAU = 12f;
        
        /// <summary>
        /// Total heat from all RCPs in MW.
        /// Source: Industry data — 4 RCPs × ~5.25 MW each = 21 MW total
        /// </summary>
        public const float RCP_HEAT_MW = 21f;
        
        /// <summary>
        /// Heat added by each RCP in MW.
        /// = RCP_HEAT_MW / RCP_COUNT = 21 / 4 = 5.25 MW per pump
        /// </summary>
        public const float RCP_HEAT_MW_EACH = 5.25f;
        
        /// <summary>Low flow trip setpoint (fraction of normal)</summary>
        public const float LOW_FLOW_TRIP = 0.87f;
        
        /// <summary>
        /// Minimum pressure for RCP operation in psig.
        /// Source: NRC ML11223A342 Section 19.2.2 — "pressure must be at least 320 psig
        /// to support running the RCPs"
        /// Required for adequate NPSH margin and seal injection
        /// </summary>
        public const float MIN_RCP_PRESSURE_PSIG = 320f;
        
        /// <summary>
        /// Minimum pressure for RCP operation in psia.
        /// = MIN_RCP_PRESSURE_PSIG + 14.7 = 334.7 psia
        /// </summary>
        public const float MIN_RCP_PRESSURE_PSIA = 334.7f;
        
        #endregion
        
        #region Residual Heat Removal (RHR) System
        
        /// <summary>
        /// Maximum RHR system operating pressure in psig.
        /// Source: NRC ML11223A342 — RHR isolated above this pressure
        /// </summary>
        public const float MAX_RHR_PRESSURE_PSIG = 450f;
        
        /// <summary>
        /// Maximum RHR system operating pressure in psia.
        /// = MAX_RHR_PRESSURE_PSIG + 14.7 = 464.7 psia
        /// </summary>
        public const float MAX_RHR_PRESSURE_PSIA = 464.7f;
        
        /// <summary>
        /// RHR entry temperature in °F.
        /// Source: Mode 4 (Hot Shutdown) upper limit
        /// </summary>
        public const float RHR_ENTRY_TEMP_F = 350f;
        
        #endregion
        
        #region RCP Startup Ramp-Up Timing — Per NRC HRTD 3.2 / 19.2.2

        // =====================================================================
        // RCP Startup Stages: Each pump progresses through 4 stages over
        // ~40 minutes from motor energisation to rated conditions.
        // Source: NRC HRTD 3.2 (RCP Operations), industry startup data.
        //
        // Flow fraction and heat fraction differ because at low speed,
        // pump mechanical energy goes primarily into shaft friction and
        // seal heating rather than bulk coolant heating.
        // =====================================================================

        /// <summary>
        /// Stage 1 duration: motor start to ~30% speed (hours).
        /// Source: NRC HRTD 3.2 — DOL start, shaft accelerating, ~2 min.
        /// </summary>
        public const float RCP_STAGE_1_DURATION_HR = 2f / 60f;

        /// <summary>
        /// Stage 2 duration: low flow development (hours).
        /// Source: NRC HRTD 3.2 — flow patterns establishing, ~7.5 min.
        /// </summary>
        public const float RCP_STAGE_2_DURATION_HR = 7.5f / 60f;

        /// <summary>
        /// Stage 3 duration: moderate flow / thermal mixing (hours).
        /// Source: NRC HRTD 3.2 / 19.2.2 — pressure rising from expansion, ~12.5 min.
        /// </summary>
        public const float RCP_STAGE_3_DURATION_HR = 12.5f / 60f;

        /// <summary>
        /// Stage 4 duration: full speed thermal equilibration (hours).
        /// Source: NRC HRTD 3.2 — rated conditions, ~17.5 min.
        /// </summary>
        public const float RCP_STAGE_4_DURATION_HR = 17.5f / 60f;

        /// <summary>
        /// Total ramp duration per pump (hours). Sum of all 4 stages ≈ 0.66 hr (~40 min).
        /// Source: Derived from individual stage durations.
        /// </summary>
        public const float RCP_TOTAL_RAMP_DURATION_HR =
            RCP_STAGE_1_DURATION_HR + RCP_STAGE_2_DURATION_HR +
            RCP_STAGE_3_DURATION_HR + RCP_STAGE_4_DURATION_HR;

        /// <summary>Flow fraction at end of Stage 1 (10%). Source: NRC HRTD 3.2</summary>
        public const float RCP_STAGE_1_FLOW_FRACTION = 0.10f;
        /// <summary>Flow fraction at end of Stage 2 (30%). Source: NRC HRTD 3.2</summary>
        public const float RCP_STAGE_2_FLOW_FRACTION = 0.30f;
        /// <summary>Flow fraction at end of Stage 3 (70%). Source: NRC HRTD 3.2</summary>
        public const float RCP_STAGE_3_FLOW_FRACTION = 0.70f;
        /// <summary>Flow fraction at end of Stage 4 (100%). Source: NRC HRTD 3.2</summary>
        public const float RCP_STAGE_4_FLOW_FRACTION = 1.00f;

        /// <summary>Heat fraction at end of Stage 1 (5%). Source: NRC HRTD 3.2 — mostly friction.</summary>
        public const float RCP_STAGE_1_HEAT_FRACTION = 0.05f;
        /// <summary>Heat fraction at end of Stage 2 (20%). Source: NRC HRTD 3.2 — flow developing.</summary>
        public const float RCP_STAGE_2_HEAT_FRACTION = 0.20f;
        /// <summary>Heat fraction at end of Stage 3 (60%). Source: NRC HRTD 3.2 — substantial mixing.</summary>
        public const float RCP_STAGE_3_HEAT_FRACTION = 0.60f;
        /// <summary>Heat fraction at end of Stage 4 (100%). Source: NRC HRTD 3.2</summary>
        public const float RCP_STAGE_4_HEAT_FRACTION = 1.00f;

        #endregion

        #region PZR Heater PID Control — Per NRC HRTD 10.2 (v1.1.0 Stage 4)

        // =====================================================================
        // Pressurizer heater PID control parameters for normal (two-phase)
        // operations. Replaces bang-bang control with smooth modulation.
        //
        // Per NRC HRTD 10.2: "The pressurizer pressure control system maintains
        // RCS pressure at 2235 psig by controlling pressurizer heaters and spray."
        //
        // The PID controller modulates heater power to maintain pressure at
        // setpoint with minimal oscillation. Key features:
        //   - Deadband to prevent hunting at setpoint
        //   - Rate limiting to prevent rapid power changes
        //   - First-order lag to model thermal inertia of heater elements
        //   - Heater staging (proportional + backup groups)
        //
        // Sources:
        //   - NRC HRTD 10.2 — Pressurizer Pressure Control
        //   - NRC HRTD 6.1 — Pressurizer Heaters
        //   - Westinghouse 4-Loop PWR I&C Specifications
        // =====================================================================

        /// <summary>
        /// PZR operating pressure setpoint in psig.
        /// Source: NRC HRTD 10.2 — Normal operating pressure
        /// </summary>
        public const float PZR_OPERATING_PRESSURE_PSIG = 2235f;

        /// <summary>
        /// Heater PID proportional gain (fraction per psi error).
        /// 
        /// At 20 psi below setpoint, heaters should be at ~100%.
        /// Kp = 1.0 / 20 = 0.05 per psi
        /// 
        /// Source: Typical PWR heater control tuning per NRC HRTD 10.2
        /// </summary>
        public const float HEATER_PID_KP = 0.05f;

        /// <summary>
        /// Heater PID integral gain (fraction per psi-hour).
        /// 
        /// Provides steady-state error correction. Lower value prevents
        /// overshoot while still eliminating offset.
        /// 
        /// Typical: Ki = Kp / Ti where Ti = 10 minutes = 0.167 hr
        /// Ki = 0.05 / 0.167 ≈ 0.3, but reduced to 0.1 for stability.
        /// 
        /// Source: PWR control system tuning practice
        /// </summary>
        public const float HEATER_PID_KI = 0.1f;

        /// <summary>
        /// Heater PID derivative gain (fraction per psi/hour).
        /// 
        /// Provides anticipatory action on pressure rate changes.
        /// Low value to prevent noise amplification.
        /// 
        /// Source: PWR control system tuning practice
        /// </summary>
        public const float HEATER_PID_KD = 0.01f;

        /// <summary>
        /// Heater control deadband in psi.
        /// 
        /// Within this band around setpoint, heater output is held constant
        /// to prevent continuous hunting. Integral action still accumulates
        /// slowly to correct any offset.
        /// 
        /// Source: NRC HRTD 10.2 — Typical heater control deadband
        /// </summary>
        public const float HEATER_DEADBAND_PSI = 5f;

        /// <summary>
        /// Heater output rate limit (fraction per minute).
        /// 
        /// Limits how fast heater power can change to prevent thermal
        /// shock to heater elements and reduce control system oscillation.
        /// 10% per minute is typical for PWR heater controls.
        /// 
        /// Source: NRC HRTD 6.1 — Heater thermal protection
        /// </summary>
        public const float HEATER_RATE_LIMIT_PER_MIN = 0.10f;

        /// <summary>
        /// Heater output rate limit (fraction per hour).
        /// = HEATER_RATE_LIMIT_PER_MIN × 60 = 6.0 per hour
        /// </summary>
        public const float HEATER_RATE_LIMIT_PER_HR = 6.0f;

        /// <summary>
        /// Heater thermal lag time constant in hours.
        /// 
        /// Models the thermal inertia of heater elements and surrounding
        /// water. Heater power changes don't immediately affect steam
        /// production rate.
        /// 
        /// Typical: 20-40 seconds = 0.0056-0.011 hours
        /// Using 30 seconds = 0.00833 hours
        /// 
        /// Source: Heater element thermal dynamics
        /// </summary>
        public const float HEATER_LAG_TAU_HR = 0.00833f;

        /// <summary>
        /// Heater PID integral limit (fraction).
        /// 
        /// Anti-windup limit on integral term to prevent excessive
        /// accumulation during large transients.
        /// 
        /// Source: Control system anti-windup practice
        /// </summary>
        public const float HEATER_INTEGRAL_LIMIT = 1.0f;

        // =====================================================================
        // Heater Staging Setpoints — Per NRC HRTD 10.2
        //
        // PWR heaters are divided into proportional (continuously modulated)
        // and backup (on/off) groups. The backup heaters provide additional
        // capacity during large pressure transients.
        // =====================================================================

        /// <summary>
        /// Pressure above which proportional heaters cut off (psig).
        /// = setpoint + 15 psi
        /// 
        /// Source: NRC HRTD 10.2 — Heater cutoff setpoint
        /// </summary>
        public const float HEATER_PROP_CUTOFF_PSIG = 2250f;

        /// <summary>
        /// Pressure below which backup heaters energize (psig).
        /// = setpoint - 25 psi
        /// 
        /// Source: NRC HRTD 10.2 — Backup heater actuation
        /// </summary>
        public const float HEATER_BACKUP_ON_PSIG = 2210f;

        /// <summary>
        /// Pressure above which backup heaters de-energize (psig).
        /// = setpoint - 10 psi (hysteresis)
        /// 
        /// Source: NRC HRTD 10.2 — Backup heater reset
        /// </summary>
        public const float HEATER_BACKUP_OFF_PSIG = 2225f;

        /// <summary>
        /// Pressure at which spray starts (psig).
        /// = setpoint + 25 psi
        /// 
        /// Source: NRC HRTD 10.2 — Spray actuation setpoint
        /// </summary>
        public const float SPRAY_START_PSIG = 2260f;

        /// <summary>
        /// Pressure at which spray is at full flow (psig).
        /// = setpoint + 75 psi
        /// 
        /// Source: NRC HRTD 10.2 — "2310 psig for spray valves fully open"
        /// </summary>
        public const float SPRAY_FULL_PSIG = 2310f;

        /// <summary>
        /// Proportional heater capacity in kW.
        /// Continuously modulated group.
        /// 
        /// Source: NRC HRTD 6.1 — ~500 kW proportional
        /// </summary>
        public const float HEATER_PROPORTIONAL_CAPACITY_KW = 500f;

        /// <summary>
        /// Backup heater capacity in kW.
        /// On/off group for large transients.
        /// 
        /// Source: NRC HRTD 6.1 — ~1300 kW backup (1800 - 500)
        /// </summary>
        public const float HEATER_BACKUP_CAPACITY_KW = 1300f;

        /// <summary>
        /// Total heater capacity in kW.
        /// = Proportional + Backup = 500 + 1300 = 1800 kW
        /// 
        /// Source: NRC HRTD 6.1
        /// </summary>
        public const float HEATER_TOTAL_CAPACITY_KW = 1800f;

        /// <summary>
        /// Minimum heater output during normal operations (fraction).
        /// 
        /// Small continuous heat input maintains saturation conditions
        /// and provides rapid response to pressure decreases.
        /// 
        /// Source: Operating practice — ~5% minimum
        /// </summary>
        public const float HEATER_MIN_OUTPUT = 0.05f;

        #endregion

        #region Pressure / RCP / RHR Methods
        
        /// <summary>
        /// Check if RCP can be started at current conditions.
        /// Requires: bubble exists AND pressure >= 320 psig
        /// </summary>
        /// <param name="pressure_psig">Current pressure in psig</param>
        /// <param name="bubbleExists">True if pressurizer has steam bubble</param>
        /// <returns>True if RCP can be started</returns>
        public static bool CanStartRCP(float pressure_psig, bool bubbleExists)
        {
            return bubbleExists && pressure_psig >= MIN_RCP_PRESSURE_PSIG;
        }
        
        /// <summary>
        /// Check if RHR system can be in service.
        /// Requires: pressure &lt;= 450 psig AND temp &lt;= 350°F
        /// </summary>
        /// <param name="pressure_psig">Current pressure in psig</param>
        /// <param name="T_avg_F">Average coolant temperature in °F</param>
        /// <returns>True if RHR can be in service</returns>
        public static bool CanOperateRHR(float pressure_psig, float T_avg_F)
        {
            return pressure_psig <= MAX_RHR_PRESSURE_PSIG && T_avg_F <= RHR_ENTRY_TEMP_F;
        }
        
        #endregion
    }
}
