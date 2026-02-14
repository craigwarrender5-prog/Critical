// ============================================================================
// CRITICAL: Master the Atom - Plant Constants (Steam Generator Design)
// PlantConstants.SG.cs - Westinghouse Model F SG Tube Bundle & Multi-Node
//                        Thermal-Hydraulic Parameters
// ============================================================================
//
// DOMAIN: Steam Generator tube geometry, multi-node thermal model parameters,
//         natural convection correlations, thermocline stratification model,
//         boiling onset, SG draining, secondary pressure tracking.
//
// SOURCES:
//   - Westinghouse Model F SG: WCAP-8530 / WCAP-12700 (SG design report)
//   - NRC HRTD ML11223A213 Section 5.0 — Steam Generators
//   - NRC HRTD ML11251A016 — SG wet layup conditions
//   - NRC HRTD 2.3 / 19.0 — SG Draining and Startup Procedures
//   - Incropera & DeWitt, Fundamentals of Heat and Mass Transfer (7th ed.)
//   - Churchill-Chu correlation for natural convection on cylinders
//   - NUREG/CR-5426 — PWR SG natural circulation phenomena
//   - NRC Bulletin 88-11 — Thermal stratification in PWR systems
//   - Technical_Documentation/SG_THERMAL_MODEL_RESEARCH_v3.0.0.md
//
// UNITS:
//   Temperature: °F | Pressure: psia | Mass: lb | Area: ft²
//   Length: ft or mm as noted | HTC: BTU/(hr·ft²·°F) | Time: hours
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
        #region SG Tube Bundle Geometry — Westinghouse Model F

        // =====================================================================
        // Actual tube data for Westinghouse Model F U-tube steam generators.
        // These values support the multi-node SG thermal model.
        //
        // Sources:
        //   - WCAP-8530, WCAP-12700 (Westinghouse SG design reports)
        //   - NRC HRTD ML11223A213 Section 5.0
        //   - Westinghouse 4-Loop FSAR (South Texas, Vogtle, V.C. Summer)
        // =====================================================================

        /// <summary>
        /// Number of U-tubes per SG.
        /// Westinghouse Model F: 5,626 tubes.
        /// Used in multi-node model for per-tube heat transfer area.
        /// Source: Westinghouse FSAR Table 5.5-1
        /// </summary>
        public const int SG_TUBES_PER_SG = 5626;

        /// <summary>
        /// Tube outer diameter in feet.
        /// Model F: 0.75 in = 0.0625 ft (19.05 mm).
        /// Source: WCAP-8530 / Westinghouse FSAR Table 5.5-1
        /// </summary>
        public const float SG_TUBE_OD_FT = 0.0625f;  // 0.75 in

        /// <summary>
        /// Tube wall thickness in feet.
        /// Model F: 0.043 in = 0.003583 ft (1.09 mm) Inconel 690.
        /// Source: WCAP-8530 / Westinghouse FSAR Table 5.5-1
        /// </summary>
        public const float SG_TUBE_WALL_FT = 0.003583f;  // 0.043 in

        /// <summary>
        /// Tube inner diameter in feet.
        /// = OD - 2 × wall = 0.0625 - 2(0.003583) = 0.05533 ft (0.664 in).
        /// Source: Derived from OD and wall thickness
        /// </summary>
        public const float SG_TUBE_ID_FT = 0.05533f;  // 0.664 in

        /// <summary>
        /// Average U-tube length (one leg, tubesheet to U-bend apex) in feet.
        /// Model F: approximately 23 ft from tubesheet to top of U-bend.
        /// The effective heat transfer length per tube (hot leg + cold leg) is ~46 ft.
        /// For vertical stratification, the relevant dimension is the straight
        /// leg height from tubesheet to start of U-bend: ~21 ft.
        /// Source: NRC HRTD ML11223A213, Westinghouse FSAR
        /// </summary>
        public const float SG_TUBE_STRAIGHT_LEG_FT = 21.0f;

        /// <summary>
        /// U-bend height above straight section in feet.
        /// The U-bend adds ~2-5 ft depending on tube row. Average ~3 ft.
        /// Source: Westinghouse FSAR, geometric analysis
        /// </summary>
        public const float SG_TUBE_UBEND_HEIGHT_FT = 3.0f;

        /// <summary>
        /// Total tube height from tubesheet to U-bend apex in feet.
        /// = Straight leg + U-bend height = 21 + 3 = 24 ft.
        /// This is the vertical extent relevant to stratification modeling.
        /// Source: Derived from leg + U-bend geometry
        /// </summary>
        public const float SG_TUBE_TOTAL_HEIGHT_FT = 24.0f;

        /// <summary>
        /// Inconel 690 thermal conductivity in BTU/(hr·ft·°F).
        /// Range: 8.1-8.7 BTU/(hr·ft·°F) [14-15 W/(m·K)] at 100-500°F.
        /// Using mid-range value.
        /// Source: Inconel 690 material data, Special Metals Corp.
        /// </summary>
        public const float SG_TUBE_THERMAL_CONDUCTIVITY = 8.4f;

        /// <summary>
        /// Tube triangular pitch in feet.
        /// Model F: 1.063 in = 0.08858 ft (27 mm) triangular pitch.
        /// Source: WCAP-8530 / Westinghouse FSAR Table 5.5-1
        /// </summary>
        public const float SG_TUBE_PITCH_FT = 0.08858f;  // 1.063 in

        /// <summary>
        /// Per-tube outer surface area per unit length in ft²/ft.
        /// = π × OD = π × 0.0625 = 0.1963 ft²/ft.
        /// Source: Geometric calculation from tube OD
        /// </summary>
        public const float SG_TUBE_AREA_PER_FT = 0.1963f;  // π × 0.0625

        /// <summary>
        /// Heat transfer area per SG in ft² (cross-check).
        /// = Tubes × π × OD × (2 × straight_leg + π × r_bend)
        /// ≈ 5626 × 0.1963 × (2×21 + π×1.5) ≈ 5626 × 0.1963 × 46.7 ≈ 51,600 ft²
        /// Westinghouse quotes 55,000 ft² including U-bend credit.
        /// Using Westinghouse value from PlantConstants.SG_AREA_EACH (55,000).
        /// Source: Westinghouse FSAR Table 5.5-1, PlantConstants.SG_AREA_EACH
        /// </summary>
        public const float SG_HT_AREA_PER_SG_FT2 = 55000f;

        #endregion

        #region Multi-Node SG Secondary Side Model Parameters

        // =====================================================================
        // Parameters for vertical multi-node thermal stratification model.
        //
        // The SG secondary side is divided into N vertical nodes from
        // tubesheet (bottom) to U-bend apex (top). Each node has:
        //   - A fraction of the total tube heat transfer area
        //   - A fraction of the total secondary water mass
        //   - An independent temperature that evolves with heat input
        //   - An effectiveness factor based on local natural convection
        //
        // During cold stagnant conditions, only the top node(s) receive
        // significant heat because buoyancy drives heated water upward,
        // creating a stratified hot layer at the top of the tube bundle.
        //
        // Sources:
        //   - NRC HRTD ML11251A016 — SG wet layup (stagnant secondary)
        //   - NUREG/CR-5426 — Natural circulation in SG secondary
        //   - NRC Bulletin 88-11 — Thermal stratification in PWR systems
        //   - Incropera & DeWitt Ch. 9 — Natural convection correlations
        // =====================================================================

        /// <summary>
        /// Default number of vertical nodes for SG secondary model.
        /// 5 nodes provides good resolution of stratification profile while
        /// maintaining computational efficiency (5 × 4 SGs = 20 node calcs/step).
        /// Source: Engineering judgment — 5 nodes captures U-bend, upper, mid-upper,
        ///         mid-lower, and lower tube bundle regions.
        /// </summary>
        public const int SG_NODE_COUNT = 5;

        /// <summary>
        /// Secondary side water mass per SG in wet layup in lb.
        /// During cold shutdown wet layup, SGs are 100% filled with water
        /// (no steam space). ~50,000 gallons per SG.
        /// = SG_SECONDARY_WATER_MASS_LB from PlantConstants.Heatup.cs = 415,000 lb per SG
        /// Source: Westinghouse FSAR, NRC HRTD 19.2.2 initial conditions
        /// </summary>
        public const float SG_SECONDARY_WATER_PER_SG_LB = 415000f;

        /// <summary>
        /// Secondary side metal mass per SG in lb (shell, shroud, supports).
        /// = SG_SECONDARY_METAL_MASS_LB from PlantConstants.Heatup.cs = 200,000 lb per SG
        /// Source: Westinghouse FSAR
        /// </summary>
        public const float SG_SECONDARY_METAL_PER_SG_LB = 200000f;

        // =====================================================================
        // Node height fractions — how vertical extent is divided.
        // Node 0 = top (U-bend region), Node N-1 = bottom (near tubesheet).
        //
        // The tube bundle is not uniformly distributed vertically. The U-bend
        // region at the top contains more tube surface per vertical foot
        // than the straight leg section. Node fractions reflect the actual
        // tube area distribution.
        //
        // For 5 nodes (top to bottom):
        //   Node 0 (U-bend + upper):  20% of height, ~25% of area
        //   Node 1 (upper-mid):       20% of height, ~20% of area
        //   Node 2 (middle):          20% of height, ~20% of area
        //   Node 3 (lower-mid):       20% of height, ~20% of area
        //   Node 4 (bottom/tubesheet):20% of height, ~15% of area
        //
        // Area fractions per SG (not total):
        //   These distribute 55,000 ft² per SG among nodes.
        // =====================================================================

        /// <summary>
        /// Tube area fraction for top node (U-bend region).
        /// Higher fraction because U-bend concentrates tube surface.
        /// Source: Geometric analysis of Model F U-tube layout
        /// </summary>
        public const float SG_NODE_AREA_FRAC_TOP = 0.25f;

        /// <summary>
        /// Tube area fraction for upper-mid node.
        /// Source: Geometric analysis of Model F U-tube layout
        /// </summary>
        public const float SG_NODE_AREA_FRAC_UPPER_MID = 0.20f;

        /// <summary>
        /// Tube area fraction for middle node.
        /// Source: Geometric analysis of Model F U-tube layout
        /// </summary>
        public const float SG_NODE_AREA_FRAC_MID = 0.20f;

        /// <summary>
        /// Tube area fraction for lower-mid node.
        /// Source: Geometric analysis of Model F U-tube layout
        /// </summary>
        public const float SG_NODE_AREA_FRAC_LOWER_MID = 0.20f;

        /// <summary>
        /// Tube area fraction for bottom node (near tubesheet).
        /// Lower fraction because fewer tubes extend this far.
        /// Source: Geometric analysis of Model F U-tube layout
        /// </summary>
        public const float SG_NODE_AREA_FRAC_BOTTOM = 0.15f;

        // =====================================================================
        // Heat Transfer Coefficients — Multi-Node Model
        //
        // In the multi-node model, HTC values are applied per-node with
        // effectiveness factors. The overall HTC is dominated by the
        // secondary-side natural convection resistance.
        //
        // Key difference from lumped model: the multi-node approach uses
        // the ACTUAL per-node HTC without artificial "boundary layer factor"
        // correction. The stratification physics is captured by the node
        // structure itself.
        // =====================================================================

        /// <summary>
        /// Stagnant secondary HTC in BTU/(hr·ft²·°F).
        /// This is the natural convection HTC on the OUTSIDE of tubes in a
        /// stagnant pool of subcooled water with NO forced circulation.
        ///
        /// For a horizontal cylinder (tube) in quiescent fluid:
        ///   Churchill-Chu: Nu = [0.6 + 0.387 Ra_D^(1/6) / (1+(0.559/Pr)^(9/16))^(8/27)]²
        ///   At Ra_D ~ 10^4 to 10^6 (typical for 3/4" tube, ΔT=5-20°F):
        ///     Nu ≈ 10-30
        ///     h = Nu × k / D = 20 × 0.35 / 0.0625 ≈ 112 BTU/(hr·ft²·°F)
        ///
        /// BUT in a stagnant stratified pool, the effective convection is much
        /// weaker because:
        ///   1. Heated fluid rises and forms a stable stratified layer
        ///   2. No bulk circulation to sweep fresh cold fluid past tubes
        ///   3. Local ΔT at tube surface is small (boundary layer saturates)
        ///
        /// Conservative value for stagnant conditions: 50 BTU/(hr·ft²·°F)
        /// This accounts for the reduced effectiveness of natural convection
        /// in a confined, stratified tube bundle.
        ///
        /// Source: NUREG/CR-5426, Churchill-Chu with stratification correction
        /// </summary>
        public const float SG_MULTINODE_HTC_STAGNANT = 50f;

        /// <summary>
        /// Active natural convection HTC in BTU/(hr·ft²·°F).
        /// When buoyancy-driven circulation develops within a node (local
        /// Rayleigh number becomes significant), HTC increases.
        ///
        /// At Ra ~ 10^8 with developed natural convection cells:
        ///   Nu ≈ 50-100, h ≈ 150-300 BTU/(hr·ft²·°F)
        /// Using mid-range for the transition regime.
        ///
        /// Source: Incropera & DeWitt Ch. 9, NUREG/CR-5426
        /// </summary>
        public const float SG_MULTINODE_HTC_ACTIVE_NC = 200f;

        /// <summary>
        /// Full natural circulation HTC in BTU/(hr·ft²·°F).
        /// When strong density-driven circulation is established throughout
        /// the SG secondary (significant ΔT between top and bottom nodes),
        /// the circulation provides more effective heat transfer.
        ///
        /// At Ra ~ 10^9-10^10 with established circulation:
        ///   h ≈ 300-500 BTU/(hr·ft²·°F)
        ///
        /// Source: Incropera & DeWitt, NUREG/CR-5426
        /// </summary>
        public const float SG_MULTINODE_HTC_FULL_CIRC = 400f;

        // =====================================================================
        // Natural Convection Effectiveness — Node Position Factors
        //
        // In a stagnant stratified SG secondary, heat transfer effectiveness
        // varies with vertical position. The top node (near U-bend) sees
        // more effective convection because heated water naturally rises.
        // Lower nodes are insulated by the stable stratification above.
        //
        // These factors represent the fraction of the theoretical HTC
        // that is actually effective at each node position during early
        // stagnant conditions (before circulation onset).
        //
        // Source: NUREG/CR-5426, thermal stratification analysis
        // =====================================================================

        /// <summary>
        /// Initial effectiveness factor for top node (U-bend region).
        /// Heated water rises to the top — this node receives the most
        /// effective natural convection. Still reduced because the
        /// boundary layer saturates in stagnant conditions.
        ///
        /// v4.3.0: Increased from 0.40 to 0.70. With the pressure-feedback
        /// model damping boiling intensity, higher effectiveness is tolerable
        /// without SG absorbing too much heat. The higher value produces
        /// realistic primary-secondary ΔT (30-60°F vs prior 150°F).
        ///
        /// Source: NUREG/CR-5426, NRC Bulletin 88-11 stratification analysis
        /// </summary>
        public const float SG_NODE_EFF_TOP_STAGNANT = 0.70f;

        /// <summary>
        /// Initial effectiveness factor for upper-mid node.
        /// Strong buoyancy plumes from tubes below carry heat upward.
        ///
        /// v4.3.0: Increased from 0.15 to 0.40. Same rationale as top node.
        ///
        /// Source: NUREG/CR-5426
        /// </summary>
        public const float SG_NODE_EFF_UPPER_STAGNANT = 0.40f;

        /// <summary>
        /// Initial effectiveness factor for middle node.
        /// Moderate NC, thermocline typically in this region.
        ///
        /// v4.3.0: Increased from 0.05 to 0.15.
        ///
        /// Source: NUREG/CR-5426
        /// </summary>
        public const float SG_NODE_EFF_MID_STAGNANT = 0.15f;

        /// <summary>
        /// Initial effectiveness factor for lower-mid node.
        /// Below thermocline most of heatup.
        ///
        /// v4.3.0: Increased from 0.02 to 0.05.
        ///
        /// Source: NUREG/CR-5426
        /// </summary>
        public const float SG_NODE_EFF_LOWER_STAGNANT = 0.05f;

        /// <summary>
        /// Initial effectiveness factor for bottom node (near tubesheet).
        /// Near tubesheet, stagnant.
        ///
        /// v4.3.0: Increased from 0.01 to 0.02.
        ///
        /// Source: NUREG/CR-5426
        /// </summary>
        public const float SG_NODE_EFF_BOTTOM_STAGNANT = 0.02f;

        // =====================================================================
        // DEPRECATED — Natural Circulation Onset Criteria (v2.x model)
        //
        // These constants were part of the v2.x SG circulation model which
        // triggered "natural circulation" when top-bottom ΔT exceeded 30°F.
        //
        // REASON FOR DEPRECATION (v3.0.0):
        //   Stratification (ΔT top > bottom) is gravitationally STABLE
        //   (Richardson number >> 1). Hot on top, cold on bottom does NOT
        //   drive circulation — it PREVENTS it. True secondary natural
        //   circulation only develops when boiling begins (~220°F+) or when
        //   the SG downcomer/riser flow path is established at operating
        //   conditions. The v2.x model caused the SG to absorb 14-19 MW
        //   during subcooled heatup (thermodynamically impossible for
        //   stagnant water), crashing the heatup rate to ~26°F/hr.
        //
        // REPLACED BY: Thermocline-based stratification model (below).
        //
        // These constants are retained (commented out) for historical
        // reference. They are no longer used by SGMultiNodeThermal.cs.
        // =====================================================================

        // [DEPRECATED v3.0.0] public const float SG_CIRC_ONSET_DELTA_T_F = 30f;
        // [DEPRECATED v3.0.0] public const float SG_CIRC_FULL_DELTA_T_F = 80f;
        // [DEPRECATED v3.0.0] public const float SG_CIRC_FULL_EFFECTIVENESS = 0.70f;

        #endregion

        #region Thermocline Stratification Model (v3.0.0)

        // =====================================================================
        // Thermocline-based SG secondary stratification model.
        //
        // Replaces the broken circulation onset model. The thermocline
        // represents the boundary between the hot upper layer (which has
        // been heated by convection from tubes) and the cold lower layer
        // (stagnant, near initial temperature).
        //
        // Physics basis:
        //   - Hot water rises from tube surfaces and accumulates at the top
        //   - Stratification is gravitationally STABLE (Ri >> 1)
        //   - Only tubes above the thermocline participate in heat transfer
        //   - Thermocline descends slowly via thermal diffusion through
        //     water and tube metal (effective α ≈ 0.08 ft²/hr)
        //   - When top node reaches ~220°F, boiling begins and dramatically
        //     improves heat transfer
        //
        // Thermocline position: z_therm = H - √(4·α_eff·t_elapsed)
        //   Where H = 24 ft (total tube height)
        //   Starts at top (24 ft), descends toward tubesheet (0 ft)
        //
        // Sources:
        //   - Technical_Documentation/SG_THERMAL_MODEL_RESEARCH_v3.0.0.md
        //   - Incropera & DeWitt Ch. 5 — Transient conduction
        //   - NRC Bulletin 88-11 — Thermal stratification
        //   - NUREG/CR-5426 — SG natural circulation phenomena
        // =====================================================================

        /// <summary>
        /// Effective thermal diffusivity for thermocline descent in ft²/hr.
        ///
        /// Enhanced beyond pure water (α_water ≈ 0.021 ft²/hr) because Inconel
        /// tubes act as thermal wicks conducting heat downward through the
        /// bundle (α_inconel ≈ 0.14 ft²/hr). The effective value accounts for
        /// the composite tube-water medium and local convective enhancement
        /// near tube surfaces.
        ///
        /// Thermocline descent rate: x ≈ √(4·α_eff·t)
        ///   At α_eff = 0.08: after 4 hr ≈ 1.1 ft, after 8 hr ≈ 1.6 ft
        ///   This means less than 10% of tube area activates during 8 hr heatup.
        ///
        /// Source: SG_THERMAL_MODEL_RESEARCH_v3.0.0.md Section 2,
        ///         derived from composite tube-water thermal properties
        /// </summary>
        public const float SG_THERMOCLINE_ALPHA_EFF = 0.08f;

        /// <summary>
        /// Tube bundle natural convection penalty factor (dimensionless).
        ///
        /// In a dense tube bundle (P/D = 1.42), boundary layers from adjacent
        /// tubes overlap and convection plumes interfere, reducing the
        /// effective HTC below the single-isolated-tube Churchill-Chu value.
        ///
        /// Published bundle correction factors for triangular pitch at
        /// P/D ≈ 1.4: 0.3-0.7 range depending on Rayleigh number.
        ///
        /// v4.3.0: Increased from 0.40 to 0.55. With forced primary circulation
        /// driving higher tube surface temperatures, the secondary NC is more
        /// vigorous and the effective penalty is less severe. Combined with
        /// increased node effectiveness, this produces realistic primary-secondary
        /// ΔT (~30-60°F) instead of the prior excessive ΔT (~150°F).
        ///
        /// Applied to the effective area fraction for tubes above thermocline.
        ///
        /// Source: SG_THERMAL_MODEL_RESEARCH_v3.0.0.md Section 2,
        ///         Incropera & DeWitt Ch. 7 (external flow over tube banks)
        /// </summary>
        public const float SG_BUNDLE_PENALTY_FACTOR = 0.55f;

        /// <summary>
        /// Thermocline transition zone width in feet.
        ///
        /// The thermocline is not a sharp boundary — there is a gradual
        /// transition zone where temperature drops from the hot upper
        /// layer to the cold lower layer. Tubes within this zone have
        /// partial effectiveness (linearly ramped).
        ///
        /// Source: Engineering estimate from thermal diffusion profile
        /// </summary>
        public const float SG_THERMOCLINE_TRANSITION_FT = 1.5f;

        /// <summary>
        /// Effectiveness factor for tubes below the thermocline.
        ///
        /// Tubes below the thermocline are surrounded by cold stagnant water
        /// at near-initial temperature. Very little ΔT exists between the
        /// tube wall and the local secondary water, so heat transfer is
        /// minimal. This small residual accounts for slow axial conduction
        /// through the tube metal.
        ///
        /// Source: SG_THERMAL_MODEL_RESEARCH_v3.0.0.md
        /// </summary>
        public const float SG_BELOW_THERMOCLINE_EFF = 0.02f;

        /// <summary>
        /// SG secondary temperature for boiling onset in °F.
        ///
        /// When the top SG node reaches this temperature, steam bubbles
        /// begin forming near the U-bend. Boiling dramatically improves
        /// heat transfer by:
        ///   - Providing agitation that disrupts the stagnant boundary layer
        ///   - Creating density differences that drive local circulation
        ///   - Increasing effective HTC by SG_BOILING_HTC_MULTIPLIER
        ///
        /// At atmospheric + nitrogen blanket (~17 psia), Tsat ≈ 220°F.
        ///
        /// Source: Steam tables (Tsat at ~17 psia),
        ///         NRC HRTD 19.0 — nitrogen blanket isolated at steam onset
        /// </summary>
        public const float SG_BOILING_ONSET_TEMP_F = 220f;

        /// <summary>
        /// HTC multiplier when boiling is active in a node.
        ///
        /// Nucleate boiling HTC is significantly higher than single-phase
        /// natural convection. Typical h_boiling / h_natural_conv ≈ 5-10.
        /// Using conservative value of 5.0.
        ///
        /// Applied to the secondary-side HTC for nodes where local
        /// temperature exceeds SG_BOILING_ONSET_TEMP_F.
        ///
        /// Source: Incropera & DeWitt Ch. 10 (Boiling),
        ///         SG_THERMAL_MODEL_RESEARCH_v3.0.0.md
        /// </summary>
        public const float SG_BOILING_HTC_MULTIPLIER = 5.0f;

        /// <summary>
        /// Initial fraction of tube area that is active (above thermocline).
        ///
        /// At heatup start, only the U-bend region at the very top of the
        /// tube bundle participates in heat transfer. This is approximately
        /// the U-bend height (3 ft) divided by total height (24 ft) = 12.5%.
        ///
        /// Source: Geometric analysis — U-bend height / total tube height
        /// </summary>
        public const float SG_UBEND_AREA_FRACTION = 0.12f;

        #endregion

        #region SG Draining Model (v3.0.0)

        // =====================================================================
        // During startup, SGs are drained from 100% WR (wet layup) to
        // operating level (~33% NR) via the normal blowdown system.
        // Draining begins at ~200°F RCS temperature.
        //
        // Source: NRC HRTD 2.3 / 19.0 — SG startup draining procedures
        // =====================================================================

        /// <summary>
        /// RCS temperature at which SG draining begins in °F.
        /// Draining from wet layup (100% WR) toward operating level.
        /// Source: NRC HRTD 19.0 — SG draining commenced at ~200°F
        /// </summary>
        public const float SG_DRAINING_START_TEMP_F = 200f;

        /// <summary>
        /// SG blowdown/draining rate per SG in gpm.
        /// Normal blowdown system rate through coolers and ion exchangers.
        /// Source: NRC HRTD 2.3 — 150 gpm normal blowdown rate
        /// </summary>
        public const float SG_DRAINING_RATE_GPM = 150f;

        /// <summary>
        /// Target secondary mass fraction after draining (relative to wet layup).
        ///
        /// Wet layup: 100% WR ≈ 415,000 lb per SG
        /// Operating level: ~33% NR ≈ ~55% of wet layup mass
        /// (NR and WR are different reference spans; 33% NR ≈ 55% of total)
        ///
        /// Source: NRC HRTD 19.0, Westinghouse FSAR SG level instrumentation
        /// </summary>
        public const float SG_DRAINING_TARGET_MASS_FRAC = 0.55f;

        #endregion

        #region SG Multi-Node Helper Arrays

        /// <summary>
        /// Default node area fractions (top to bottom) for 5-node model.
        /// Index 0 = top node (U-bend), index 4 = bottom node (tubesheet).
        /// Sum must equal 1.0.
        /// Source: Geometric analysis of Model F U-tube layout
        /// </summary>
        public static readonly float[] SG_NODE_AREA_FRACTIONS = new float[]
        {
            SG_NODE_AREA_FRAC_TOP,          // 0.25 — U-bend region
            SG_NODE_AREA_FRAC_UPPER_MID,    // 0.20 — Upper straight
            SG_NODE_AREA_FRAC_MID,          // 0.20 — Middle straight
            SG_NODE_AREA_FRAC_LOWER_MID,    // 0.20 — Lower straight
            SG_NODE_AREA_FRAC_BOTTOM        // 0.15 — Tubesheet region
        };

        /// <summary>
        /// Default stagnant effectiveness factors (top to bottom) for 5-node model.
        /// These represent the fraction of theoretical HTC that is effective
        /// at each node position during stagnant (no-circulation) conditions.
        /// Source: NUREG/CR-5426, stratification analysis
        /// </summary>
        public static readonly float[] SG_NODE_STAGNANT_EFFECTIVENESS = new float[]
        {
            SG_NODE_EFF_TOP_STAGNANT,       // 0.40 — U-bend region
            SG_NODE_EFF_UPPER_STAGNANT,     // 0.15 — Upper straight
            SG_NODE_EFF_MID_STAGNANT,       // 0.05 — Middle straight
            SG_NODE_EFF_LOWER_STAGNANT,     // 0.02 — Lower straight
            SG_NODE_EFF_BOTTOM_STAGNANT     // 0.01 — Tubesheet region
        };

        /// <summary>
        /// Node water mass fractions (top to bottom) for 5-node model.
        /// Water mass is distributed based on the annular volume between
        /// tubes at each elevation. Roughly proportional to height fraction,
        /// with adjustment for the U-bend region (less free volume due to
        /// denser tube packing).
        /// Source: Geometric analysis
        /// </summary>
        public static readonly float[] SG_NODE_MASS_FRACTIONS = new float[]
        {
            0.15f,   // Top — less free volume in U-bend region
            0.20f,   // Upper-mid
            0.25f,   // Middle — largest free volume
            0.25f,   // Lower-mid
            0.15f    // Bottom — tubesheet/flow distribution region
        };

        #endregion

        #region SG Secondary Pressure Model (v4.3.0)

        // =====================================================================
        // SG secondary side pressure tracking during heatup.
        //
        // During cold shutdown, the SG secondary is a nitrogen-blanketed wet
        // layup vessel at ~17 psia. As the primary heats the secondary through
        // the tube walls, the secondary eventually reaches saturation and begins
        // steaming. The nitrogen blanket is isolated and the SG becomes a closed
        // pressurizing vessel. Secondary pressure then tracks the saturation
        // curve of the hottest secondary node (quasi-static equilibrium).
        //
        // This pressure progression is a defining characteristic of the heatup:
        //   - It determines when boiling starts (T_node >= T_sat at P_secondary)
        //   - It determines boiling intensity (superheat = T_node - T_sat)
        //   - It determines heatup termination (steam dumps at 1092 psig)
        //   - It creates a self-limiting feedback loop that prevents cliff behavior
        //
        // Sources:
        //   - NRC HRTD ML11223A342 Section 19.2.2 — Steam onset at ~220°F
        //   - NRC HRTD ML11223A294 Section 11.2 — Steam dumps at 1092 psig
        //   - NRC HRTD ML11223A244 Section 7.1 — Main Steam Safety Valves
        //   - NRC HRTD ML11251A016 — SG wet layup conditions
        // =====================================================================

        /// <summary>
        /// Initial SG secondary pressure in psia during wet layup.
        /// Atmospheric pressure (~14.7 psia) + nitrogen blanket (~2-3 psig).
        /// Nitrogen blanket maintains slight positive pressure to prevent
        /// air in-leakage during cold shutdown.
        ///
        /// Source: NRC HRTD ML11251A016 — SG wet layup conditions,
        ///         NRC HRTD ML11223A342 Section 19.0 — nitrogen blanket
        /// </summary>
        public const float SG_INITIAL_PRESSURE_PSIA = 17f;

        /// <summary>
        /// RCS temperature at which SG nitrogen blanket is isolated in °F.
        /// When RCS reaches ~220°F, steam formation begins in the SGs.
        /// Nitrogen supply is isolated and the secondary becomes a closed
        /// pressurizing vessel.
        ///
        /// Source: NRC HRTD ML11223A342 Section 19.2.2:
        ///   "At approximately 220°F RCS temperature, steam formation begins
        ///    in the steam generators. The nitrogen supply to the steam
        ///    generators is now isolated."
        /// </summary>
        public const float SG_NITROGEN_ISOLATION_TEMP_F = 220f;

        /// <summary>
        /// Steam dump setpoint pressure in psig (steam pressure mode).
        /// The steam dumps actuate when SG header pressure reaches this value,
        /// which terminates the heatup by removing steam to the condenser.
        /// 1092 psig corresponds to T_sat ≈ 557°F, the no-load Tavg setpoint.
        ///
        /// Source: NRC HRTD ML11223A294 Section 11.2:
        ///   "The primary plant heatup is terminated by automatic actuation
        ///    of the steam dumps when the pressure inside the steam header
        ///    reaches 1092 psig."
        /// </summary>
        public const float SG_STEAM_DUMP_SETPOINT_PSIG = 1092f;

        /// <summary>
        /// SG safety valve setpoint in psig (lowest setting).
        /// Model F SGs have 5 code safety valves per SG, with the lowest
        /// setpoint at 1185 psig (±3%). This provides overpressure protection
        /// if steam dumps fail to control secondary pressure.
        ///
        /// Source: NRC HRTD ML11223A244 Section 7.1 — Main Steam Safety Valves,
        ///         Westinghouse FSAR Table 10.3-1
        /// </summary>
        public const float SG_SAFETY_VALVE_SETPOINT_PSIG = 1185f;

        /// <summary>
        /// Superheat range for boiling HTC ramp in °F.
        /// The transition from single-phase natural convection to fully
        /// developed nucleate boiling occurs over a superheat range of
        /// approximately 15-20°F above the local saturation temperature.
        ///
        /// ΔT_superheat = T_node - T_sat(P_secondary)
        /// f_boil = smoothstep(ΔT_superheat / this_value)
        ///
        /// Below 0°F superheat: no boiling enhancement (multiplier = 1.0)
        /// At this_value superheat: full boiling enhancement (multiplier = max)
        ///
        /// Source: Incropera & DeWitt Ch. 10 — Pool boiling curve,
        ///         onset of nucleate boiling through fully developed regime
        /// </summary>
        public const float SG_BOILING_SUPERHEAT_RANGE_F = 20f;

        #endregion

        // =====================================================================
        // v5.0.0 THREE-REGIME MODEL CONSTANTS
        // =====================================================================
        //
        // Constants for the open-system boiling model (Stage 2).
        //
        // Sources:
        //   - Incropera & DeWitt, 7th ed., Ch. 10 — Boiling and Condensation
        //   - Rohsenow correlation for nucleate pool boiling
        //   - NRC HRTD ML11223A342 Section 19.0 — heatup ~9 hours
        //   - SG_HEATUP_BREAKTHROUGH_HANDOFF.md — energy balance analysis
        // =====================================================================

        #region Three-Regime Model (v5.0.0)

        /// <summary>
        /// Base nucleate boiling HTC at low secondary pressure in BTU/(hr·ft²·°F).
        /// At atmospheric pressure, nucleate boiling on tube exteriors produces
        /// h ≈ 2,000–5,000. This is the overall U-value including primary-side
        /// resistance in series (h_primary ≈ 1000 with RCPs).
        ///
        /// In the boiling regime, the secondary-side HTC is very high (2,000+),
        /// so the overall U is limited by the primary side:
        ///   1/U = 1/h_primary + 1/h_secondary ≈ 1/1000 + 1/3000 = 1/750
        ///   U ≈ 750 BTU/(hr·ft²·°F)
        ///
        /// However, the effective U is reduced by bundle geometry effects and
        /// the fact that not all tube surface area is equally effective during
        /// boiling onset. Using 500 as the overall U at low pressure.
        ///
        /// Source: Incropera & DeWitt Ch. 10, Rohsenow correlation;
        ///         series resistance with h_primary = 1000 (Dittus-Boelter)
        /// </summary>
        public const float SG_BOILING_HTC_LOW_P = 500f;

        /// <summary>
        /// Nucleate boiling HTC at steam dump pressure in BTU/(hr·ft²·°F).
        /// At higher pressures, boiling HTC increases (more vigorous nucleation,
        /// smaller bubbles, thinner thermal boundary layer). At 1092 psig:
        ///   h_secondary ≈ 5,000–10,000
        ///   U ≈ 1/(1/1000 + 1/7000) ≈ 875
        ///
        /// Using 700 to account for bundle effects and partial coverage.
        ///
        /// Source: Incropera & DeWitt Ch. 10 — pressure dependence of
        ///         nucleate boiling (Rohsenow Csf correction)
        /// </summary>
        public const float SG_BOILING_HTC_HIGH_P = 700f;

        /// <summary>
        /// Reference pressure for high-pressure boiling HTC in psia.
        /// Corresponds to steam dump setpoint (1092 psig + 14.7 = 1106.7 psia).
        /// Used for linear interpolation of boiling HTC between low and high P.
        /// </summary>
        public const float SG_BOILING_HTC_HIGH_P_REF_PSIA = 1107f;

        // [REMOVED v5.1.0] SG_MAX_PRESSURE_RATE_PSI_HR (200 psi/hr artificial rate clamp)
        // Replaced by direct saturation tracking: P_secondary = P_sat(T_hottest_boiling_node)
        // Physical damping provided by steam line condensation energy sink (Stage 3).
        // See IMPLEMENTATION_PLAN_v5.1.0.md for rationale.

        /// <summary>
        /// Minimum secondary pressure in psia during open-system boiling.
        /// Once N₂ is isolated and steam forms, pressure cannot drop below
        /// atmospheric (14.7 psia). The initial N₂ blanket pressure (17 psia)
        /// is the practical floor.
        ///
        /// Source: Thermodynamic constraint — open to atmosphere via MSIVs
        /// </summary>
        public const float SG_MIN_STEAMING_PRESSURE_PSIA = 17f;

        #endregion

        #region Wall Superheat Model (v5.1.0 Stage 4)

        // =====================================================================
        // Wall superheat boiling driver.
        //
        // In reality, nucleate boiling is driven by the tube WALL superheat:
        //   ΔT_boiling = T_wall - T_sat
        // NOT by the primary bulk temperature:
        //   ΔT_wrong = T_rcs - T_sat
        //
        // The tube wall sits between the primary coolant and the boiling
        // secondary. Its temperature is always T_sat < T_wall < T_rcs,
        // meaning the true boiling driving force is smaller than (T_rcs - T_sat).
        //
        // T_wall is computed algebraically (not a state variable):
        //   T_wall = T_rcs - Q_node_prev / (h_primary × A_node)
        //
        // Using previous-timestep Q avoids implicit coupling and eliminates
        // the need for an iterative solver.
        //
        // Sources:
        //   - Incropera & DeWitt Ch. 8 — Internal forced convection
        //   - Dittus-Boelter correlation: Nu = 0.023 Re^0.8 Pr^0.4
        //   - Westinghouse FSAR — RCS flow conditions
        //   - Implementation Plan v5.1.0 Stage 4
        // =====================================================================

        /// <summary>
        /// Primary-side forced convection HTC in BTU/(hr·ft²·°F).
        ///
        /// With 4 RCPs running, primary coolant flows at ~17 ft/s through
        /// the SG tubes (ID = 0.664 in). At these conditions:
        ///   Re ≈ 300,000–500,000 (turbulent)
        ///   Pr ≈ 1.0–2.5 (water at 300–550°F)
        ///
        /// Dittus-Boelter: Nu = 0.023 × Re^0.8 × Pr^0.4
        ///   Nu ≈ 600–1200
        ///   h = Nu × k / D = 900 × 0.35 / 0.0553 ≈ 5,700 BTU/(hr·ft²·°F)
        ///
        /// However, the effective h_primary for the wall temperature
        /// calculation uses the INNER tube surface area, and the overall
        /// model uses OUTER tube surface area for Q computation. The area
        /// ratio (OD/ID = 0.0625/0.05533 = 1.13) effectively reduces the
        /// apparent h_primary when referenced to outer area.
        ///
        /// Using 1500 BTU/(hr·ft²·°F) referenced to outer tube area.
        /// This is conservative (lower h_primary → larger T_wall drop →
        /// more damping), which is appropriate given that:
        ///   - Tube fouling reduces h below clean-tube Dittus-Boelter
        ///   - Inlet effects and tube-to-tube flow maldistribution
        ///   - U-bend region has different flow characteristics
        ///
        /// Note: This constant is the h_primary referenced to the OUTER
        /// tube surface area (same basis as SG_HT_AREA_PER_SG_FT2).
        ///
        /// Source: Dittus-Boelter correlation, Westinghouse FSAR flow data,
        ///         Incropera & DeWitt Ch. 8
        /// </summary>
        public const float SG_PRIMARY_FORCED_CONVECTION_HTC = 1500f;

        #endregion

        #region Steam Inventory Model (v5.4.0 Stage 6)

        // =====================================================================
        // Steam inventory tracking for isolated SG scenarios.
        //
        // In normal open-system heatup with MSIVs open, steam exits as fast as
        // it's produced and pressure tracks P_sat(T_hottest). However, when
        // the SG is isolated (MSIVs closed), steam accumulates in the steam
        // space and pressure rises based on inventory rather than saturation.
        //
        // The ideal gas approximation is used for inventory-based pressure:
        //   P = (m_steam × R × T) / V_steam
        // where R is the specific gas constant for steam (85.78 ft·lbf/(lb·°R)
        // = 0.1102 psia·ft³/(lb·°R)).
        //
        // Sources:
        //   - Westinghouse FSAR — SG secondary volume
        //   - Steam tables — ideal gas approximation for low-density steam
        //   - Implementation Plan v5.4.0 Stage 6
        // =====================================================================

        /// <summary>
        /// Total SG secondary side volume per SG in ft³ (water + steam space).
        ///
        /// At wet layup (100% full), the secondary contains 415,000 lb of
        /// water at ~100°F. At this temperature, ρ_water ≈ 62.0 lb/ft³.
        ///   V_total ≈ 415,000 / 62.0 ≈ 6,694 ft³ per SG
        ///
        /// Using 6,700 ft³ per SG (rounded). This is the total secondary
        /// volume from the tubesheet to the steam dome.
        ///
        /// Source: Derived from wet layup mass and cold water density,
        ///         Westinghouse FSAR — SG secondary geometry
        /// </summary>
        public const float SG_SECONDARY_TOTAL_VOLUME_PER_SG_FT3 = 6700f;

        /// <summary>
        /// Specific gas constant for steam in psia·ft³/(lb·°R).
        ///
        /// R_steam = R_universal / M_water = 1545.35 / 18.015 = 85.78 ft·lbf/(lb·°R)
        /// Converting to psia·ft³/(lb·°R): 85.78 / 144 = 0.5957 psia·ft³/(lb·°R)
        ///
        /// Wait, let's recalculate:
        /// R_universal = 10.7316 psia·ft³/(lbmol·°R)
        /// M_water = 18.015 lb/lbmol
        /// R_steam = 10.7316 / 18.015 = 0.5957 psia·ft³/(lb·°R)
        ///
        /// Source: Steam tables, thermodynamic properties of water
        /// </summary>
        public const float SG_STEAM_GAS_CONSTANT_PSIA_FT3_PER_LB_R = 0.5957f;

        /// <summary>
        /// Specific gas constant for nitrogen in psia·ft³/(lb·°R).
        /// R_n2 = R_universal / M_n2 = 10.7316 / 28.0134 = 0.3831.
        /// Used for minimal compressible SG gas cushion behavior.
        /// </summary>
        public const float SG_N2_GAS_CONSTANT_PSIA_FT3_PER_LB_R = 0.3831f;

        /// <summary>
        /// Minimum total gas cushion volume per SG in ft³.
        /// This represents the startup steam dome/headspace that remains
        /// compressible during wet layup and early heatup.
        /// </summary>
        public const float SG_MIN_GAS_CUSHION_VOLUME_PER_SG_FT3 = 250f;

        #endregion

        #region Steam Line Warming Model (v5.1.0 Stage 3)

        // =====================================================================
        // Lumped steam line thermal mass model for physical pressure damping.
        //
        // During early boiling, cold steam line piping condenses steam and
        // absorbs latent heat. This removes energy from the boiling system,
        // causing the hottest secondary node temperature (and thus saturation
        // pressure) to rise more slowly than pure saturation tracking would
        // predict. As the steam lines warm toward saturation temperature,
        // condensation rate drops and pressure tracks saturation more closely.
        //
        // This provides the realistic early-boiling pressure lag that the
        // removed 200 psi/hr rate clamp was approximating, but through a
        // physically justified mechanism.
        //
        // CRITICAL: The steam line model acts as an energy sink ONLY.
        // Pressure is always determined by P_sat(T_hottest). The steam line
        // model affects pressure INDIRECTLY by removing energy from the
        // boiling system, which slows node temperature rise.
        //
        // Sources:
        //   - Westinghouse FSAR — Main steam line piping specifications
        //   - NRC HRTD ML11223A294 Section 11.0 — Steam line layout
        //   - ASME B31.1 — Carbon steel pipe properties
        // =====================================================================

        /// <summary>
        /// Total steam line metal mass across all 4 lines in lb.
        ///
        /// Each main steam line: ~30" ID schedule 80 carbon steel, ~100 ft
        /// from SG nozzle to turbine building (including vertical runs, turns,
        /// MSIVs, and associated piping).
        ///
        /// Per line: ~30" Sch 80 → wall thickness ~0.625", OD ~31.25"
        ///   Metal cross-section ≈ π/4 × (31.25² - 30²) = ~60 in²
        ///   Volume per foot: 60 in² × 12 in/ft = 720 in³/ft = 0.417 ft³/ft
        ///   Carbon steel density: 490 lb/ft³
        ///   Mass per foot: ~204 lb/ft
        ///   100 ft per line: ~20,400 lb pipe
        ///   MSIV + check valves: ~10,000 lb per line
        ///   Total per line: ~30,000 lb
        ///   4 lines: ~120,000 lb
        ///
        /// Source: Westinghouse FSAR piping specifications, ASME B31.1
        /// </summary>
        public const float SG_STEAM_LINE_METAL_MASS_LB = 120000f;

        /// <summary>
        /// Specific heat of carbon steel steam line piping in BTU/(lb·°F).
        /// A106 Grade B carbon steel at 200–500°F range.
        ///
        /// Source: ASME material properties, Incropera & DeWitt Appendix A
        /// </summary>
        public const float SG_STEAM_LINE_CP = 0.12f;

        /// <summary>
        /// Overall heat transfer coefficient × area (UA) for steam-to-metal
        /// condensation on cold steam line interior surfaces in BTU/(hr·°F).
        ///
        /// During early boiling, steam flows from the SG steam space into
        /// cold steam lines. Film condensation on the cold metal interior
        /// provides very high local HTCs (h_condensation ≈ 1000–5000
        /// BTU/(hr·ft²·°F) for film condensation on steel).
        ///
        /// Interior surface area per line: π × 2.5ft × 100ft ≈ 785 ft²
        /// 4 lines: ~3,140 ft²
        /// Effective condensation HTC (film, early-boiling low pressure):
        ///   h ≈ 500–1000 BTU/(hr·ft²·°F) (reduced for low steam velocity
        ///   and non-uniform coverage during early startup)
        ///
        /// UA = h × A ≈ 700 × 3140 ≈ 2,200,000 BTU/(hr·°F)
        ///
        /// Tuned to produce ~100–150 psi/hr effective early pressure rise
        /// rate (matching the approximate behavior the rate clamp was
        /// targeting, but through physical energy accounting).
        ///
        /// Source: Incropera & DeWitt Ch. 10 — Film condensation on
        ///         vertical and horizontal surfaces, Nusselt analysis
        /// </summary>
        public const float SG_STEAM_LINE_UA = 2200000f;

        #endregion
    }
}
