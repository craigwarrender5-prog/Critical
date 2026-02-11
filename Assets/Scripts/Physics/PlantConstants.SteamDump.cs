// ============================================================================
// CRITICAL: Master the Atom - Plant Constants (Steam Dump)
// PlantConstants.SteamDump.cs - Steam Dump System Parameters for HZP Control
// ============================================================================
//
// DOMAIN: Steam dump valve control, steam header pressure setpoints,
//         heat removal capacity, valve dynamics
//
// PURPOSE:
//   During Hot Zero Power (HZP) operations, the reactor coolant pumps add
//   ~21 MW of heat to the RCS with no reactor power or turbine load.
//   The steam dump system removes this excess heat by dumping steam from
//   the SG secondary side to the main condenser, maintaining RCS T_avg
//   at the no-load setpoint (557°F).
//
// SOURCES:
//   - NRC HRTD 19.0 — Plant Operations (ML11223A342)
//     "The primary plant heatup is terminated by automatic actuation of
//      the steam dumps (in steam pressure control) when the pressure inside
//      the steam header reaches 1092 psig."
//   - NRC HRTD 11.2 — Steam Dump Control System
//   - Westinghouse 4-Loop PWR Technical Specifications
//
// UNITS:
//   Pressure: psig/psia | Temperature: °F | Power: MW
//   Time: seconds | Flow: fraction (0-1)
//
// NOTE: This is a partial class. All constants are accessed as
//       PlantConstants.SteamDump.CONSTANT_NAME or directly in the
//       PlantConstants namespace.
//
// VERSION: 1.1.0 (Stage 2)
// GOLD STANDARD: Yes
// ============================================================================

using System;

namespace Critical.Physics
{
    public static partial class PlantConstants
    {
        /// <summary>
        /// Nested class containing Steam Dump system parameters.
        /// All constants sourced from NRC HRTD 11.2 and 19.0.
        /// </summary>
        public static class SteamDump
        {
            #region Steam Pressure Setpoints
            
            /// <summary>
            /// Steam header pressure setpoint for no-load temperature control in psig.
            /// 
            /// Per NRC HRTD 19.0: "The pressure inside the steam header reaches 1092 psig"
            /// at which point steam dumps actuate to maintain T_avg = 557°F.
            /// 
            /// 1092 psig corresponds to saturation temperature of ~557°F, matching
            /// the no-load T_avg setpoint.
            /// 
            /// Source: NRC HRTD 19.0, Section 19.2.2
            /// </summary>
            public const float STEAM_PRESSURE_SETPOINT_PSIG = 1092f;
            
            /// <summary>
            /// Steam header pressure setpoint in psia.
            /// = STEAM_PRESSURE_SETPOINT_PSIG + 14.7 = 1106.7 psia
            /// </summary>
            public const float STEAM_PRESSURE_SETPOINT_PSIA = 1106.7f;
            
            /// <summary>
            /// Minimum steam pressure required for steam dump operation in psig.
            /// Below this pressure, insufficient steam is available for effective
            /// heat removal to condenser.
            /// 
            /// Source: NRC HRTD 11.2 — Steam dump low pressure cutoff
            /// </summary>
            public const float STEAM_DUMP_MIN_PRESSURE_PSIG = 900f;
            
            /// <summary>
            /// No-load RCS average temperature setpoint in °F.
            /// Steam dump maintains T_avg at this value during HZP.
            /// 
            /// Source: NRC HRTD 19.0, Westinghouse 4-Loop specifications
            /// </summary>
            public const float HZP_TAVG_SETPOINT_F = 557f;
            
            #endregion
            
            #region Steam Dump Capacity
            
            /// <summary>
            /// Maximum steam dump heat removal capacity in MW thermal.
            /// 
            /// At HZP with 4 RCPs running, the system must reject ~21 MW from
            /// RCP heat plus ~0.5 MW from proportional heaters minus ~1.5 MW
            /// insulation losses = ~20 MW net.
            /// 
            /// Each SG can dump ~6-7 MW of steam to condenser at no-load conditions,
            /// giving a total capacity of ~25-28 MW with all 4 SGs.
            /// 
            /// Source: NRC HRTD 11.2, Westinghouse design specifications
            /// </summary>
            public const float STEAM_DUMP_MAX_MW = 25f;
            
            /// <summary>
            /// Steam dump capacity per SG in MW.
            /// = STEAM_DUMP_MAX_MW / 4 = 6.25 MW per SG
            /// </summary>
            public const float STEAM_DUMP_PER_SG_MW = 6.25f;
            
            #endregion
            
            #region Steam Dump Control Parameters
            
            /// <summary>
            /// Proportional gain for steam dump demand (fraction per psi error).
            /// 
            /// At 20 psi above setpoint (1112 psig), dumps should be ~100% open.
            /// Kp = 1.0 / 20 = 0.05 per psi
            /// 
            /// This provides responsive but stable pressure control without
            /// excessive valve hunting.
            /// 
            /// Source: Typical PWR steam dump tuning per NRC HRTD 11.2
            /// </summary>
            public const float STEAM_DUMP_KP = 0.05f;
            
            /// <summary>
            /// Steam dump valve stroke time in seconds (0 to 100%).
            /// 
            /// Fast-acting valves for responsive temperature control during
            /// load rejection transients. 10-15 seconds typical.
            /// 
            /// Source: NRC HRTD 11.2 — Steam dump valve response
            /// </summary>
            public const float STEAM_DUMP_STROKE_TIME_SEC = 10f;
            
            /// <summary>
            /// Steam dump valve stroke time in hours.
            /// = STEAM_DUMP_STROKE_TIME_SEC / 3600 = 0.00278 hours
            /// </summary>
            public const float STEAM_DUMP_STROKE_TIME_HR = 0.00278f;
            
            /// <summary>
            /// Valve position deadband (fraction).
            /// Below this demand, valves are fully closed.
            /// Prevents hunting at very low demands.
            /// </summary>
            public const float STEAM_DUMP_DEADBAND = 0.02f;
            
            #endregion
            
            #region SG Secondary Side Parameters at HZP
            
            /// <summary>
            /// SG secondary side saturation temperature at 1092 psig in °F.
            /// This is approximately the steam/water temperature in the SG
            /// during HZP steaming conditions.
            /// 
            /// Source: Steam tables — T_sat at 1106.7 psia ≈ 556°F
            /// </summary>
            public const float SG_SECONDARY_TSAT_AT_HZP_F = 556f;
            
            /// <summary>
            /// Steam enthalpy at HZP conditions in BTU/lb.
            /// h_fg (latent heat) at ~1100 psia ≈ 650 BTU/lb
            /// 
            /// Used to calculate steam mass flow from heat removal rate:
            /// m_dot_steam = Q_dump / h_fg
            /// 
            /// Source: Steam tables
            /// </summary>
            public const float STEAM_ENTHALPY_HFG_BTU_LB = 650f;
            
            /// <summary>
            /// Pressure rise rate per MW of unremoved heat in psi/hr/MW.
            /// 
            /// When steam dump cannot remove all excess heat, secondary
            /// pressure rises. This coefficient relates heat imbalance
            /// to pressure change rate.
            /// 
            /// Derived from SG secondary thermal mass and steam properties:
            /// ~5-10 psi/hr per MW of net heat input at HZP conditions.
            /// 
            /// Source: Calculated from Westinghouse SG specifications
            /// </summary>
            public const float SG_PRESSURE_RISE_RATE_PSI_HR_MW = 8f;
            
            #endregion
            
            #region Steam Dump Mode Transition Thresholds
            
            /// <summary>
            /// RCS temperature threshold for approaching HZP in °F.
            /// When T_avg exceeds this, steam dump system should be enabled.
            /// 
            /// Source: Operating procedures — enable steam dump ~7°F below target
            /// </summary>
            public const float HZP_APPROACH_TEMP_F = 550f;
            
            /// <summary>
            /// RCS temperature threshold for HZP stabilization mode in °F.
            /// When T_avg is within this band of setpoint, fine control begins.
            /// </summary>
            public const float HZP_STABILIZATION_BAND_F = 5f;
            
            #endregion
            
            #region Utility Methods
            
            /// <summary>
            /// Calculate the saturation temperature corresponding to a given
            /// steam header pressure for steam dump control purposes.
            /// </summary>
            /// <param name="steamPressure_psig">Steam header pressure in psig</param>
            /// <returns>Saturation temperature in °F</returns>
            public static float GetSteamSaturationTemp(float steamPressure_psig)
            {
                float pressure_psia = steamPressure_psig + 14.7f;
                return WaterProperties.SaturationTemperature(pressure_psia);
            }
            
            /// <summary>
            /// Calculate the steam pressure that corresponds to a given
            /// RCS average temperature for T_avg control mode.
            /// </summary>
            /// <param name="T_avg">RCS average temperature in °F</param>
            /// <returns>Target steam pressure in psig</returns>
            public static float GetSteamPressureForTavg(float T_avg)
            {
                float pressure_psia = WaterProperties.SaturationPressure(T_avg);
                return pressure_psia - 14.7f;
            }
            
            #endregion
        }
    }
}
