// CRITICAL: Master the Atom - Physics Module
// SGMultiNodeThermal.Constants.cs - SG constants and tuning
//
// File: Assets/Scripts/Physics/SGMultiNodeThermal.Constants.cs
// Module: Critical.Physics.SGMultiNodeThermal
// Responsibility: Constants and immutable tuning parameters for SG model behavior.
// Standards: GOLD v1.0, SRP/SOLID
// Version: 1.0
// Last Updated: 2026-02-18
using System;
using UnityEngine;
namespace Critical.Physics
{
    public static partial class SGMultiNodeThermal
    {
        #region Constants

        /// <summary>Conversion: MW to BTU/hr</summary>
        private const float MW_TO_BTU_HR = 3.412e6f;

        /// <summary>
        /// Minimum Î”T for heat transfer calculation (Â°F).
        /// Below this, heat transfer is negligible.
        /// </summary>
        private const float MIN_DELTA_T = 0.01f;

        /// <summary>
        /// HTC when no RCPs are running in BTU/(hrÂ·ftÂ²Â·Â°F).
        /// Both primary and secondary sides are stagnant natural convection.
        /// Series resistance: 1/U = 1/h_primary_nc + 1/h_secondary_nc
        /// h_primary_nc â‰ˆ 10-50, h_secondary_nc â‰ˆ 20-50
        /// U â‰ˆ 7-25 BTU/(hrÂ·ftÂ²Â·Â°F)
        /// Using 8 BTU/(hrÂ·ftÂ²Â·Â°F) (conservative, near-stagnant).
        ///
        /// Source: Incropera & DeWitt Ch. 9, natural convection in tubes
        /// </summary>
        private const float HTC_NO_RCPS = 8f;

        /// <summary>
        /// Inter-node conduction UA in BTU/(hrÂ·Â°F) for stagnant conditions.
        /// Represents slow thermal diffusion between adjacent nodes.
        /// In stagnant stratified conditions, mixing is suppressed by the
        /// stable density gradient (Richardson number >> 1).
        /// Only thermal diffusion through water and tube metal contributes.
        ///
        /// Estimate: k_eff Ã— A_cross / Î”x â‰ˆ 0.4 Ã— 200 / 5 â‰ˆ 16 BTU/(hrÂ·Â°FÂ·ft)
        /// Per SG, with 4 inter-node boundaries: ~500 BTU/(hrÂ·Â°F) total
        ///
        /// Source: Thermal diffusivity analysis, SG_THERMAL_MODEL_RESEARCH_v3.0.0.md
        /// </summary>
        private const float INTERNODE_UA_STAGNANT = 500f;

        /// <summary>
        /// Inter-node mixing UA when boiling is active in BTU/(hrÂ·Â°F).
        /// Boiling in the upper region creates agitation and local circulation
        /// that enhances mixing with adjacent nodes. Much less than the old
        /// v2.x INTERNODE_UA_CIRCULATING (50,000) which was far too high.
        ///
        /// Source: Engineering estimate â€” boiling enhances local mixing 10Ã—
        /// </summary>
        private const float INTERNODE_UA_BOILING = 5000f;

        /// <summary>
        /// Node vertical height in feet (equal spacing).
        /// Total height (24 ft) / N nodes (5) = 4.8 ft per node.
        /// Node 0 (top) spans 19.2-24.0 ft, Node 4 (bottom) spans 0-4.8 ft.
        /// </summary>
        private const float NODE_HEIGHT_FT = PlantConstants.SG_TUBE_TOTAL_HEIGHT_FT
                                            / PlantConstants.SG_NODE_COUNT;

        // v5.0.0 Stage 2: Boiling regime constants

        /// <summary>
        /// Nucleate boiling HTC at low pressure (~atmospheric) in BTU/(hrÂ·ftÂ²Â·Â°F).
        /// At boiling onset (~220Â°F, ~0 psig), nucleate boiling HTCs are typically
        /// 2,000â€“5,000 BTU/(hrÂ·ftÂ²Â·Â°F) depending on surface condition and geometry.
        /// Using 2,000 as the low-pressure baseline.
        ///
        /// Note: In the boiling regime, secondary-side HTC is so high that
        /// the overall U is limited by the primary side (~1000). The exact
        /// boiling HTC matters less than getting the regime physics right.
        ///
        /// Source: Incropera & DeWitt Ch. 10, Rohsenow pool boiling correlation
        /// </summary>
        private const float HTC_BOILING_LOW_PRESSURE = 2000f;

        /// <summary>
        /// Nucleate boiling HTC at high pressure (~1100 psia) in BTU/(hrÂ·ftÂ²Â·Â°F).
        /// At PWR SG operating pressures, nucleate boiling is very efficient.
        /// Using 8,000 as the high-pressure value.
        ///
        /// Source: Incropera & DeWitt Ch. 10, Thom correlation
        /// </summary>
        private const float HTC_BOILING_HIGH_PRESSURE = 8000f;

        /// <summary>
        /// Reference pressure for boiling HTC interpolation in psia.
        /// h_boiling ramps linearly from HTC_BOILING_LOW_PRESSURE at 14.7 psia
        /// to HTC_BOILING_HIGH_PRESSURE at this value.
        /// </summary>
        private const float HTC_BOILING_PRESSURE_REF_PSIA = 1200f;

        // [REMOVED v5.1.0] MAX_PRESSURE_RATE_PSI_HR (200 psi/hr artificial rate clamp)
        // Replaced by direct saturation tracking: P_secondary = P_sat(T_hottest_boiling_node)
        // See IMPLEMENTATION_PLAN_v5.1.0.md Stage 1 for rationale.

        // v5.0.1: Regime continuity blend constants

        /// <summary>
        /// Time in hours for a node's regime blend to ramp from 0â†’1 (60 sim-seconds).
        /// Physical basis: real nucleate boiling onset is gradual, not instantaneous.
        /// The thermal boundary layer on the tube exterior transitions from single-
        /// phase natural convection to nucleate boiling over a finite time as local
        /// conditions stabilize at T_sat.
        ///
        /// Source: Implementation Plan v5.0.1, Incropera & DeWitt Ch. 10
        /// </summary>
        private const float REGIME_BLEND_RAMP_HR = 60f / 3600f;  // 60 sim-seconds

        /// <summary>
        /// Maximum allowed change in TotalHeatAbsorption_MW per timestep (MW).
        /// Applied after Section 8 output computation. Prevents MW-scale instantaneous
        /// jumps that cannot represent real physical processes within a single timestep.
        /// Bypass conditions: RCP count change, steam dump activation edge.
        ///
        /// Source: Implementation Plan v5.0.1
        /// </summary>
        private const float DELTA_Q_CLAMP_MW = 5.0f;

        #endregion
    }
}
