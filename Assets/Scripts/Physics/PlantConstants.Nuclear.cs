// ============================================================================
// CRITICAL: Master the Atom - Plant Constants (Nuclear)
// PlantConstants.Nuclear.cs - Reactor Core, Reactivity, Rods, Xenon, Boron,
//                             Decay Heat, Turbine Generator
// ============================================================================
//
// DOMAIN: Fuel assembly data, reactivity coefficients, control rods,
//         xenon dynamics, decay heat, boron concentrations, turbine generator
//
// SOURCES:
//   - Westinghouse 4-Loop FSAR Chapter 4 — Reactor Core
//   - NRC ML11223A342 — Boron Concentrations
//   - ANS 5.1-2005 — Decay Heat Standard
//   - Westinghouse FSAR Chapter 9 — Boron Concentrations
//
// UNITS:
//   Temperature: °F | Reactivity: pcm | Concentration: ppm
//   Power: MW/kW | Time: hours (unless noted)
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
        #region Reactor Core
        
        /// <summary>Number of fuel assemblies</summary>
        public const int FUEL_ASSEMBLIES = 193;
        
        /// <summary>Number of fuel rods per assembly</summary>
        public const int RODS_PER_ASSEMBLY = 264;
        
        /// <summary>Total number of fuel rods</summary>
        public const int TOTAL_RODS = 50952;
        
        /// <summary>Active fuel height in ft</summary>
        public const float ACTIVE_HEIGHT = 12f;
        
        /// <summary>Average linear heat rate in kW/ft</summary>
        public const float AVG_LINEAR_HEAT = 5.44f;
        
        /// <summary>Peak linear heat rate in kW/ft</summary>
        public const float PEAK_LINEAR_HEAT = 13f;
        
        #endregion
        
        #region Reactivity Coefficients
        
        /// <summary>
        /// Doppler coefficient in pcm/√°R (negative for safety).
        /// DTC = α_D / (2√T_R). At 588°F (1048°R): DTC = -100/64.7 = -1.55 pcm/°F
        /// Doppler defect HZP→HFP: α × (√T_eff_HFP - √T_HZP) ≈ -1100 pcm
        /// Reference: Westinghouse FSAR Chapter 4, typical BOL value
        /// </summary>
        public const float DOPPLER_COEFF = -100f;
        
        /// <summary>Moderator temperature coefficient at high boron in pcm/°F</summary>
        public const float MTC_HIGH_BORON = 5f;
        
        /// <summary>Moderator temperature coefficient at low boron in pcm/°F</summary>
        public const float MTC_LOW_BORON = -40f;
        
        /// <summary>Boron concentration for MTC transition in ppm</summary>
        public const float MTC_TRANSITION_BORON = 500f;
        
        /// <summary>Total delayed neutron fraction (beta)</summary>
        public const float BETA_DELAYED = 0.0065f;
        
        /// <summary>Prompt neutron generation time in seconds</summary>
        public const float LAMBDA_PROMPT = 2e-5f;
        
        #endregion
        
        #region Control Rods
        
        /// <summary>Number of control rod banks</summary>
        public const int ROD_BANKS = 8;
        
        /// <summary>Rod withdrawal/insertion speed in steps/min</summary>
        public const float ROD_STEPS_PER_MINUTE = 72f;
        
        /// <summary>Total steps per rod</summary>
        public const int ROD_TOTAL_STEPS = 228;
        
        /// <summary>Minimum shutdown margin in pcm</summary>
        public const float SHUTDOWN_MARGIN = 8000f;
        
        #endregion
        
        #region Xenon Dynamics
        
        /// <summary>Equilibrium xenon worth at 100% power in pcm</summary>
        public const float XENON_EQUILIBRIUM_MIN = 2500f;
        
        /// <summary>Equilibrium xenon worth at 100% power in pcm (upper range)</summary>
        public const float XENON_EQUILIBRIUM_MAX = 3000f;
        
        /// <summary>Peak xenon worth after trip in pcm</summary>
        public const float XENON_PEAK_MIN = 3000f;
        
        /// <summary>Peak xenon worth after trip in pcm (upper range)</summary>
        public const float XENON_PEAK_MAX = 4000f;
        
        /// <summary>Time to peak xenon after trip in hours</summary>
        public const float XENON_PEAK_TIME_HOURS = 9f;
        
        /// <summary>Xenon-135 decay constant in 1/hr</summary>
        public const float XENON_DECAY_CONSTANT = 0.0753f;
        
        /// <summary>Iodine-135 decay constant in 1/hr</summary>
        public const float IODINE_DECAY_CONSTANT = 0.1035f;
        
        #endregion
        
        #region Decay Heat (ANS 5.1-2005)
        
        /// <summary>Decay heat at trip as fraction of full power</summary>
        public const float DECAY_HEAT_TRIP = 0.07f;
        
        /// <summary>Decay heat at 1 minute as fraction of full power</summary>
        public const float DECAY_HEAT_1MIN = 0.05f;
        
        /// <summary>Decay heat at 10 minutes as fraction of full power</summary>
        public const float DECAY_HEAT_10MIN = 0.03f;
        
        /// <summary>Decay heat at 1 hour as fraction of full power</summary>
        public const float DECAY_HEAT_1HR = 0.015f;
        
        /// <summary>Decay heat at 1 day as fraction of full power</summary>
        public const float DECAY_HEAT_1DAY = 0.005f;
        
        #endregion
        
        #region Boron Concentrations
        
        // =====================================================================
        // Boron Concentration Reference Values
        // Source: NRC ML11223A342, Westinghouse FSAR Chapter 9
        // =====================================================================
        
        /// <summary>
        /// Boric Acid Tank (BAT) concentration in ppm.
        /// Source: FSAR — concentrated boric acid storage
        /// </summary>
        public const float BORON_BAT_PPM = 7000f;
        
        /// <summary>
        /// Refueling Water Storage Tank (RWST) concentration in ppm.
        /// Source: Tech Specs — borated water for safety injection
        /// </summary>
        public const float BORON_RWST_PPM = 2500f;
        
        /// <summary>
        /// Cold shutdown boron concentration at BOL in ppm.
        /// Source: NRC ML11223A342 — sufficient for 5% shutdown margin
        /// </summary>
        public const float BORON_COLD_SHUTDOWN_BOL_PPM = 2000f;
        
        /// <summary>
        /// Critical boron concentration at HZP BOL in ppm.
        /// Source: Typical Westinghouse value at beginning of life
        /// </summary>
        public const float BORON_CRITICAL_HZP_BOL_PPM = 1500f;
        
        /// <summary>
        /// Critical boron concentration at HFP BOL in ppm.
        /// Source: Power defect requires lower boron than HZP
        /// </summary>
        public const float BORON_CRITICAL_HFP_BOL_PPM = 1200f;
        
        /// <summary>
        /// End of life boron concentration in ppm.
        /// Source: Essentially zero boron at EOL
        /// </summary>
        public const float BORON_EOL_PPM = 10f;
        
        /// <summary>
        /// Maximum boration rate in ppm/hr.
        /// Source: CVCS design — limited by BAT flow and mixing
        /// </summary>
        public const float MAX_BORATION_RATE_PPM_HR = 100f;
        
        /// <summary>
        /// Maximum dilution rate in ppm/hr.
        /// Source: Tech Specs — limited to prevent inadvertent criticality
        /// </summary>
        public const float MAX_DILUTION_RATE_PPM_HR = 10f;
        
        /// <summary>
        /// Emergency boration rate in ppm/min.
        /// Source: Using emergency boration flowpath
        /// </summary>
        public const float EMERGENCY_BORATION_RATE_PPM_MIN = 10f;
        
        /// <summary>
        /// CVCS transit time from VCT to charging nozzle in seconds.
        /// Time for water to flow through charging pump and piping
        /// </summary>
        public const float CVCS_TRANSIT_TIME_SEC = 60f;
        
        /// <summary>
        /// Total boron effect delay in seconds.
        /// From boration initiation to core reactivity effect
        /// VCT mixing (120s) + CVCS transit (60s) + RCS mixing (120s) = 300s
        /// </summary>
        public const float BORON_EFFECT_DELAY_SEC = 300f;
        
        #endregion
        
        #region Turbine Generator
        
        /// <summary>Turbine electrical output in MW</summary>
        public const float TURBINE_OUTPUT_MW = 1150f;
        
        /// <summary>Turbine speed in rpm</summary>
        public const float TURBINE_SPEED = 1800f;
        
        /// <summary>Thermal efficiency (electrical/thermal)</summary>
        public const float TURBINE_EFFICIENCY = 0.34f;
        
        /// <summary>Governor droop (5% = 0.05)</summary>
        public const float GOVERNOR_DROOP = 0.05f;
        
        #endregion
        
        #region Nuclear Calculation Methods
        
        /// <summary>
        /// Calculate boron worth at given concentration.
        /// Worth varies slightly with concentration due to self-shielding.
        /// </summary>
        /// <param name="boron_ppm">Current boron concentration in ppm</param>
        /// <returns>Boron worth in pcm/ppm (negative value)</returns>
        public static float GetBoronWorth(float boron_ppm)
        {
            // Worth decreases slightly at high boron due to self-shielding
            // At BOL (~1500 ppm): ~-10 pcm/ppm
            // At EOL (~100 ppm): ~-8 pcm/ppm
            float factor = 1f - 0.0001f * boron_ppm;
            return BORON_WORTH * factor;
        }
        
        /// <summary>
        /// Calculate MTC at given boron concentration.
        /// MTC is positive at high boron (BOL), negative at low boron (EOL).
        /// </summary>
        /// <param name="boron_ppm">Current boron concentration in ppm</param>
        /// <returns>MTC in pcm/°F</returns>
        public static float GetMTC(float boron_ppm)
        {
            if (boron_ppm >= BORON_CRITICAL_HZP_BOL_PPM)
                return MTC_HIGH_BORON;
            if (boron_ppm <= BORON_EOL_PPM)
                return MTC_LOW_BORON;
            
            float fraction = (boron_ppm - BORON_EOL_PPM) / (BORON_CRITICAL_HZP_BOL_PPM - BORON_EOL_PPM);
            return MTC_LOW_BORON + fraction * (MTC_HIGH_BORON - MTC_LOW_BORON);
        }
        
        #endregion
    }
}
