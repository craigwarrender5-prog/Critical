// ============================================================================
// CRITICAL: Master the Atom - Plant Constants (BRS)
// PlantConstants.BRS.cs - Boron Recycle System Constants
// ============================================================================
//
// DOMAIN: BRS recycle holdup tanks, boric acid evaporator, monitor tanks,
//         return flow paths, BAT/PWST storage, LCV-112A cross-references
//
// SOURCES:
//   - Callaway FSAR Chapter 11 (ML21195A182) — BRS flow rates, holdup tank
//     capacity, evaporator processing rates, decontamination factors
//   - NRC HRTD 4.1 (ML11223A214) — CVCS/BRS system description, Sections
//     4.1.2.6 (BRS description), 4.1.3.1 (LCV-112A), Fig 4.1-3 (BRS flow),
//     Fig 4.1-4 (evaporator detail)
//   - NRC HRTD 15.1 (ML11223A332) — Table 15.1-2 (evaporator capacities,
//     multi-plant comparison: Vogtle, Comanche Peak, McGuire 1-15 gpm range)
//   - Catawba UFSAR (ML19189A302) — Table 12-19 (BRS component dimensions)
//
// UNITS:
//   Volume: gallons | Flow: gpm | Concentration: ppm | Time: days (processing)
//
// NOTE: This is a partial class. All constants are accessed as
//       PlantConstants.CONSTANT_NAME regardless of which file they live in.
//
// CROSS-REFERENCES (existing constants used by BRS, no duplication):
//   VCT_DIVERT_SETPOINT     — PlantConstants.CVCS.cs (70%)
//   VCT_DIVERT_PROP_BAND    — PlantConstants.CVCS.cs (20%)
//   VCT_LEVEL_HIGH          — PlantConstants.CVCS.cs (73%)
//   AUTO_MAKEUP_FLOW_GPM    — PlantConstants.CVCS.cs (35 gpm)
//   BORIC_ACID_CONC         — PlantConstants.CVCS.cs (7000 ppm)
//   BORON_RWST_PPM          — PlantConstants.Nuclear.cs (2500 ppm)
//   HEATUP_EXCESS_VOLUME_GAL— PlantConstants.CVCS.cs (30000 gal)
//
// GOLD STANDARD: Yes
// ============================================================================

using System;

namespace Critical.Physics
{
    public static partial class PlantConstants
    {
        #region BRS — Recycle Holdup Tanks

        // =================================================================
        // RECYCLE HOLDUP TANKS
        // Source: Callaway FSAR Chapter 11 (ML21195A182), Figure 11.1A-2
        //         NRC HRTD 4.1 Section 4.1.2.6 (ML11223A214)
        // =================================================================

        /// <summary>
        /// Recycle holdup tank total capacity in gallons (2 tanks combined).
        /// Source: Callaway FSAR Fig 11.1A-2 — 56,000 gallons total.
        /// Catawba UFSAR Table 12-19 confirms similar sizing for 4-loop plant.
        /// </summary>
        public const float BRS_HOLDUP_TOTAL_CAPACITY_GAL = 56000f;

        /// <summary>
        /// Usable fraction of holdup tank capacity.
        /// Source: Callaway FSAR — 80% usable (reserves for nitrogen cover gas
        /// ullage and instrument tap margins).
        /// </summary>
        public const float BRS_HOLDUP_USABLE_FRACTION = 0.80f;

        /// <summary>
        /// Usable holdup capacity in gallons = 56,000 × 0.80 = 44,800 gal.
        /// Derived from BRS_HOLDUP_TOTAL_CAPACITY_GAL × BRS_HOLDUP_USABLE_FRACTION.
        /// </summary>
        public const float BRS_HOLDUP_USABLE_CAPACITY_GAL = 44800f;

        /// <summary>
        /// Number of recycle holdup tanks.
        /// Source: NRC HRTD 4.1 — "recycle holdup tanks" (plural); Callaway FSAR
        /// shows 2 tanks with recirculation pump to transfer between them.
        /// </summary>
        public const int BRS_HOLDUP_TANK_COUNT = 2;

        /// <summary>
        /// Holdup tank high-level alarm setpoint (% of usable capacity).
        /// Plant-specific. Conservative default for simulation.
        /// </summary>
        public const float BRS_HOLDUP_HIGH_LEVEL_PCT = 90f;

        /// <summary>
        /// Holdup tank low-level alarm / processing stop setpoint (%).
        /// Below this, evaporator feed pump trips to prevent cavitation.
        /// </summary>
        public const float BRS_HOLDUP_LOW_LEVEL_PCT = 10f;

        /// <summary>
        /// Minimum holdup volume to start evaporator batch (gallons).
        /// Prevents cycling the evaporator on small volumes.
        /// Approximate — operator-initiated in real plant.
        /// </summary>
        public const float BRS_EVAPORATOR_MIN_BATCH_GAL = 5000f;

        #endregion

        #region BRS — Boric Acid Evaporator

        // =================================================================
        // BORIC ACID EVAPORATOR
        // Source: Callaway FSAR Fig 11.1A-2 — 21,600 gpd processing rate
        //         NRC HRTD 4.1 Section 4.1.2.6 — evaporator process description
        //         NRC HRTD 15.1 Table 15.1-2 — waste evaporator 1-15 gpm range
        // =================================================================

        /// <summary>
        /// Evaporator processing rate in gpm (continuous operation).
        /// Source: Callaway FSAR — 21,600 gpd ÷ 1440 min/day = 15 gpm.
        /// NRC HRTD 15.1 Table 15.1-2 confirms 1-15 gpm range for similar
        /// Westinghouse 4-loop evaporators (Vogtle, Comanche Peak, McGuire).
        /// </summary>
        public const float BRS_EVAPORATOR_RATE_GPM = 15f;

        /// <summary>
        /// Evaporator processing rate in gallons per day.
        /// Source: Callaway FSAR Figure 11.1A-2 — 21,600 gpd.
        /// </summary>
        public const float BRS_EVAPORATOR_RATE_GPD = 21600f;

        /// <summary>
        /// Time to process one full holdup tank batch at rated capacity (days).
        /// Source: Callaway FSAR — Tp = 0.8 × 56,000 / 21,600 = 2.07 days.
        /// </summary>
        public const float BRS_BATCH_PROCESSING_TIME_DAYS = 2.07f;

        /// <summary>
        /// Evaporator concentrate output boron concentration in ppm.
        /// Source: NRC HRTD 4.1 — "concentrated to approximately 4 weight
        /// percent (7000 ppm)" of boric acid solution.
        /// Cross-reference: PlantConstants.BORIC_ACID_CONC = 7000f
        /// </summary>
        public const float BRS_CONCENTRATE_BORON_PPM = 7000f;

        /// <summary>
        /// Evaporator distillate output boron concentration in ppm.
        /// Source: NRC HRTD 4.1 — condensate passes through demineraliser,
        /// essentially pure water. Modelled as 0 ppm.
        /// </summary>
        public const float BRS_DISTILLATE_BORON_PPM = 0f;

        /// <summary>
        /// Reference distillate fraction (mass basis) at 2000 ppm input.
        /// Mass balance: F = D + C, F×Cf = D×0 + C×7000
        /// D/F = 1 - Cf/7000. At 2000 ppm: D/F = 1 - 2000/7000 = 0.714.
        /// Actual split computed dynamically per-step in BRSPhysics.
        /// This constant is for reference/documentation only.
        /// </summary>
        public const float BRS_DISTILLATE_FRACTION_REF = 0.714f;

        #endregion

        #region BRS — Monitor Tanks (Processed Water Storage)

        // =================================================================
        // MONITOR TANKS
        // Source: Callaway FSAR Fig 11.1A-2 — 2 × monitor tanks
        //         NRC HRTD 4.1 — monitor tanks sampled before discharge
        // =================================================================

        /// <summary>
        /// Monitor tank capacity each in gallons.
        /// Source: Callaway FSAR Figure 11.1A-2 — 100,000 gallons each.
        /// Modelled as single lumped "distillate available" volume for
        /// simplification; 200,000 gal total far exceeds single-heatup needs.
        /// </summary>
        public const float BRS_MONITOR_TANK_CAPACITY_GAL = 100000f;

        /// <summary>
        /// Initial distillate inventory at cold shutdown start (gallons).
        /// Represents processed water available from prior operating cycle.
        /// v0.9.6: Added to ensure VCT makeup can draw from BRS rather than
        /// RMS primary water (which would dilute RCS boron concentration).
        /// Value: 10,000 gal = ~5% of one monitor tank, conservative estimate
        /// for residual inventory after refueling outage processing.
        /// Source: Engineering judgement based on typical plant operations.
        /// </summary>
        public const float BRS_INITIAL_DISTILLATE_GAL = 10000f;

        /// <summary>
        /// Number of monitor tanks.
        /// Source: Callaway FSAR Figure 11.1A-2.
        /// </summary>
        public const int BRS_MONITOR_TANK_COUNT = 2;

        #endregion

        #region BRS — Return Flow Paths

        // =================================================================
        // RETURN FLOW PATHS
        // Source: NRC HRTD 4.1 Section 4.1.2.6 — monitor tank discharge:
        //   (1) Primary water storage tank (PWST)
        //   (2) Lake/river discharge (environmental — not modelled)
        //   (3) Holdup tanks (reprocessing — not modelled as separate path)
        //   (4) Evaporator condensate demineralisers (polishing — not modelled)
        //   (5) Liquid waste system (not modelled)
        //
        // Concentrate return:
        //   Boric acid at ~7000 ppm → concentrates filter → holding tank
        //   → BAT if specs met, else → holdup tanks for reprocessing
        // =================================================================

        /// <summary>
        /// Maximum return flow rate from BRS to VCT/plant systems in gpm.
        /// Matches existing AUTO_MAKEUP_FLOW_GPM (35 gpm) since the RMS
        /// blending system is the common delivery path for makeup water.
        /// Source: Engineering judgement — limited by makeup piping/valve capacity.
        /// </summary>
        public const float BRS_RETURN_FLOW_MAX_GPM = 35f;

        #endregion

        #region BRS — Boric Acid Tanks (BAT)

        // =================================================================
        // Source: NRC HRTD 4.1 (ML11223A214)
        //   "Each boric acid tank... capacity of 24,228 gal"
        //   "concentration of approximately 4 weight percent (7000 ppm)"
        // =================================================================

        /// <summary>
        /// BAT capacity each in gallons.
        /// Source: NRC HRTD 4.1 (ML11223A214).
        /// </summary>
        public const float BRS_BAT_CAPACITY_EACH_GAL = 24228f;

        /// <summary>
        /// Number of boric acid tanks.
        /// Source: NRC HRTD 4.1 — "Two boric acid tanks".
        /// </summary>
        public const int BRS_BAT_COUNT = 2;

        #endregion

        #region BRS — Primary Water Storage Tank (PWST)

        // =================================================================
        // Source: NRC HRTD 4.1 (ML11223A214) — "This 203,000-gal tank may
        //   be filled with water from the plant secondary makeup system or
        //   with distillate from the boric acid evaporators."
        // =================================================================

        /// <summary>
        /// PWST capacity in gallons.
        /// Source: NRC HRTD 4.1 (ML11223A214).
        /// </summary>
        public const float BRS_PWST_CAPACITY_GAL = 203000f;

        /// <summary>
        /// PWST boron concentration in ppm.
        /// Demineralised water storage — essentially 0 ppm boron.
        /// </summary>
        public const float BRS_PWST_BORON_PPM = 0f;

        #endregion
    }
}
