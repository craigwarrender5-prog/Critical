// ============================================================================
// CRITICAL: Master the Atom - Plant Constants (Residual Heat Removal System)
// PlantConstants.RHR.cs - RHR System Design Parameters for Heatup/Cooldown
// ============================================================================
//
// DOMAIN: RHR pump parameters, heat exchanger thermal design, operating
//         interlocks, HX bypass control, CCW interface, letdown cross-connect
//
// SOURCES:
//   - NRC HRTD 5.1 — Residual Heat Removal System (ML11223A219)
//   - NRC HRTD 19.0 — Plant Operations (ML11223A342)
//   - Byron NRC Exam 2019 (ML20054A571) — RHR HX bypass valve operation
//   - Technical_Documentation/RHR_SYSTEM_RESEARCH_v3.0.0.md
//   - Westinghouse 4-Loop PWR FSAR (Byron/Braidwood, South Texas, Vogtle)
//
// UNITS:
//   Flow: gpm | Pressure: psig/psia | Temperature: °F
//   Power: MW | Heat Transfer: BTU/(hr·°F) | Time: hours
//
// NOTE: This is a partial class. All constants are accessed as
//       PlantConstants.CONSTANT_NAME regardless of which file they live in.
//
// RELATED EXISTING CONSTANTS (retained in PlantConstants.Pressure.cs):
//   - MAX_RHR_PRESSURE_PSIG = 450f    (RHR relief valve setpoint)
//   - MAX_RHR_PRESSURE_PSIA = 464.7f
//   - RHR_ENTRY_TEMP_F = 350f         (Mode 4 upper limit)
//   - CanOperateRHR() method
//
// GOLD STANDARD: Yes (v3.0.0)
// ============================================================================

using System;

namespace Critical.Physics
{
    public static partial class PlantConstants
    {
        #region RHR Pump Parameters

        // =====================================================================
        // Westinghouse 4-Loop RHR pumps: vertical, single-stage centrifugal.
        // Two independent trains (A and B), each powered from vital buses.
        //
        // Source: NRC HRTD 5.1 (ML11223A219)
        // =====================================================================

        /// <summary>
        /// Number of RHR pump trains.
        /// Two independent, redundant trains (Train A and Train B).
        /// Source: NRC HRTD 5.1
        /// </summary>
        public const int RHR_PUMP_COUNT = 2;

        /// <summary>
        /// Design flow rate per RHR pump in gpm.
        /// Typical Westinghouse 4-loop: ~3,000 gpm per pump.
        /// Source: NRC HRTD 5.1, W 4-loop typical
        /// </summary>
        public const float RHR_PUMP_FLOW_GPM_EACH = 3000f;

        /// <summary>
        /// Total RHR flow with both trains running in gpm.
        /// = 2 × 3,000 = 6,000 gpm
        /// Source: Derived from per-pump flow
        /// </summary>
        public const float RHR_PUMP_FLOW_GPM_TOTAL = 6000f;

        /// <summary>
        /// Heat input per RHR pump in MW.
        /// Estimated from motor size (~400-700 HP typical).
        /// Pump mechanical energy is converted to heat in the coolant.
        /// Source: Engineering estimate from NRC HRTD 5.1 motor specifications
        /// </summary>
        public const float RHR_PUMP_HEAT_MW_EACH = 0.5f;

        /// <summary>
        /// Total RHR pump heat input in MW (both trains running).
        /// = 2 × 0.5 = 1.0 MW
        /// Significant heat source during cold shutdown before RCPs start.
        /// Source: Derived from per-pump heat
        /// </summary>
        public const float RHR_PUMP_HEAT_MW_TOTAL = 1.0f;

        /// <summary>
        /// Minimum flow for RHR pump protection in gpm.
        /// Below this flow, the min-flow bypass valve opens to prevent
        /// pump deadheading and overheating.
        /// Source: NRC HRTD 5.1
        /// </summary>
        public const float RHR_PUMP_MIN_FLOW_GPM = 500f;

        /// <summary>
        /// Min-flow bypass valve close setpoint in gpm.
        /// Bypass closes when flow increases above this value (hysteresis).
        /// Source: NRC HRTD 5.1
        /// </summary>
        public const float RHR_PUMP_MIN_FLOW_CLOSE_GPM = 1000f;

        #endregion

        #region RHR Heat Exchanger Parameters

        // =====================================================================
        // RHR heat exchangers: shell and U-tube type.
        // Tube side: reactor coolant (borated water)
        // Shell side: component cooling water (CCW)
        //
        // Design basis: Cool RCS from 350°F to 140°F in 16 hours (both trains)
        //
        // UA derivation (from RHR_SYSTEM_RESEARCH_v3.0.0.md):
        //   Total energy: ~150 × 10⁶ BTU over 16 hours
        //   Average heat rate: ~4.7 × 10⁶ BTU/hr per train
        //   Average LMTD: ~130°F
        //   UA per HX ≈ 4.7 × 10⁶ / 130 ≈ 36,000 BTU/(hr·°F)
        //
        // Source: NRC HRTD 5.1, derived from design cooldown requirements
        // =====================================================================

        /// <summary>
        /// Number of RHR heat exchangers (one per train).
        /// Source: NRC HRTD 5.1
        /// </summary>
        public const int RHR_HX_COUNT = 2;

        /// <summary>
        /// Heat transfer UA per RHR heat exchanger in BTU/(hr·°F).
        /// Derived from design basis cooldown: 350°F → 140°F in 16 hours.
        /// See Technical_Documentation/RHR_SYSTEM_RESEARCH_v3.0.0.md Section 3.
        /// Source: Engineering derivation from NRC HRTD 5.1 design requirements
        /// </summary>
        public const float RHR_HX_UA_EACH = 36000f;

        /// <summary>
        /// Total UA for both RHR heat exchangers in BTU/(hr·°F).
        /// = 2 × 36,000 = 72,000 BTU/(hr·°F)
        /// Source: Derived from per-HX UA
        /// </summary>
        public const float RHR_HX_UA_TOTAL = 72000f;

        /// <summary>
        /// RHR HX fouling derating factor (dimensionless).
        /// Accounts for tube fouling in aged heat exchangers.
        /// Typical PWR value: 0.80-0.90.
        /// Source: Industry operating experience, PWR HX surveillance data
        /// </summary>
        public const float RHR_HX_FOULING_FACTOR = 0.85f;

        #endregion

        #region CCW Interface (Simplified)

        // =====================================================================
        // Component Cooling Water interface — simplified as constant
        // temperature for heatup scope. Full CCW system model deferred
        // to v1.3.0.
        //
        // Source: NRC HRTD 5.1, typical design values
        // =====================================================================

        /// <summary>
        /// CCW supply temperature to RHR heat exchangers in °F.
        /// Assumed constant for simplified model. Actual varies by
        /// plant, season, and service water temperature (85-95°F typical).
        /// Source: NRC HRTD 5.1, typical design value
        /// </summary>
        public const float RHR_CCW_INLET_TEMP_F = 95f;

        /// <summary>
        /// CCW flow per RHR heat exchanger in gpm.
        /// Design flow for adequate heat removal at worst-case conditions.
        /// Source: NRC HRTD 5.1, typical design value (3,000-5,000 gpm range)
        /// </summary>
        public const float RHR_CCW_FLOW_GPM_EACH = 4000f;

        #endregion

        #region RHR Operating Limits and Interlocks

        // =====================================================================
        // RHR suction valve interlocks protect the low-pressure RHR piping
        // from RCS overpressurization. These are hard-wired safety interlocks.
        //
        // Source: NRC HRTD 5.1 (ML11223A219)
        // =====================================================================

        /// <summary>
        /// Maximum RCS pressure for opening RHR suction valves (8701/8702) in psig.
        /// Interlock prevents valve opening above this pressure to protect
        /// RHR piping (design pressure 600 psig).
        /// Source: NRC HRTD 5.1
        /// </summary>
        public const float RHR_SUCTION_VALVE_OPEN_LIMIT_PSIG = 425f;

        /// <summary>
        /// RCS pressure at which RHR suction valves automatically close in psig.
        /// Protects RHR piping during unexpected RCS pressurization.
        /// Source: NRC HRTD 5.1
        /// </summary>
        public const float RHR_SUCTION_VALVE_AUTO_CLOSE_PSIG = 585f;

        /// <summary>
        /// RHR piping design pressure in psig.
        /// All RHR piping, valves, and components rated to this pressure.
        /// Source: NRC HRTD 5.1
        /// </summary>
        public const float RHR_DESIGN_PRESSURE_PSIG = 600f;

        #endregion

        #region RHR HX Throttle Control (Heatup/Cooldown Mode)

        // =====================================================================
        // During heatup, the RHR HX bypass valve (HCV-618) is used to
        // divert most RHR flow around the heat exchanger, minimizing
        // heat removal and allowing the RCS to warm up from pump heat
        // and decay heat.
        //
        // During cooldown, the bypass is closed so all flow passes
        // through the HX for maximum heat removal.
        //
        // Source: NRC HRTD 5.1, Byron NRC Exam 2019 (ML20054A571)
        // =====================================================================

        /// <summary>
        /// RHR HX bypass fraction during heatup mode.
        /// 85% of flow bypasses the HX; only 15% passes through for cooling.
        /// This allows the RCS to heat up slowly from pump heat and decay heat.
        /// Operator adjusts this valve to control heatup rate.
        /// Source: NRC HRTD 5.1, Byron NRC Exam 2019
        /// </summary>
        public const float RHR_HX_BYPASS_FRACTION_HEATUP = 0.85f;

        /// <summary>
        /// RHR HX bypass fraction during cooldown mode.
        /// 0% bypass — all flow passes through HX for maximum cooling.
        /// Source: Normal cooldown alignment
        /// </summary>
        public const float RHR_HX_BYPASS_FRACTION_COOLDOWN = 0.0f;

        #endregion

        #region RHR Letdown via CVCS Cross-Connect

        // =====================================================================
        // During solid plant operations (Mode 5), letdown is via the
        // RHR-to-CVCS cross-connect valve HCV-128, not the normal
        // letdown orifices. This provides pressure control when the
        // normal letdown path is isolated.
        //
        // Source: NRC HRTD 19.0 (ML11223A342), Section 19.2.1
        // =====================================================================

        /// <summary>
        /// Letdown flow via RHR cross-connect valve HCV-128 in gpm.
        /// Provides CVCS letdown path during solid plant operations
        /// when normal letdown orifices are isolated.
        /// Source: NRC HRTD 19.0 Section 19.2.1
        /// </summary>
        public const float RHR_CVCS_LETDOWN_FLOW_GPM = 75f;

        #endregion
    }
}
