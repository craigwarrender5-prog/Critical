// ============================================================================
// CRITICAL: Master the Atom - Plant Constants (Condenser & Feedwater Systems)
// PlantConstants.Condenser.cs - Condenser, Feedwater, and AFW Design Parameters
// ============================================================================
//
// DOMAIN: Main condenser vacuum dynamics, circulating water system, C-9/P-12
//         startup interlocks, hotwell inventory, condensate/feedwater pumps,
//         auxiliary feedwater system, condensate storage tank
//
// PURPOSE:
//   Provides reference constants for the condenser and feedwater return path
//   systems required for accurate SG secondary boundary modeling during startup.
//   The condenser is the primary heat sink for steam dump operations; the
//   feedwater system returns condensed steam to the SGs as a closed-loop
//   mass/energy path.
//
// SOURCES:
//   - NRC HRTD Condenser System Reference (Technical_Documentation/)
//   - NRC HRTD Section 7.2 — Condensate and Feedwater System (ML11223A230)
//   - NRC HRTD Section 5.7 — Auxiliary Feedwater System (ML11223A219)
//   - NRC HRTD Section 11.2 — Steam Dump Control System
//   - Westinghouse 4-Loop PWR FSAR (South Texas, Vogtle, V.C. Summer)
//   - Technical_Documentation/Condenser_Feedwater_Architecture_Specification.md
//
// UNITS:
//   Pressure: psia/psig/in.HgA | Temperature: °F | Flow: gpm
//   Volume: gallons/ft³ | Mass: lb | Power: MW or hp | Time: hours/seconds
//
// NOTE: This is a partial class. All constants are accessed as
//       PlantConstants.Condenser.CONSTANT_NAME or PlantConstants.Feedwater.CONSTANT_NAME
//
// CS REFERENCE: CS-0115
// IP REFERENCE: IP-0046, Stage F
// VERSION: 1.0.0
// GOLD STANDARD: Yes
// ============================================================================

namespace Critical.Physics
{
    public static partial class PlantConstants
    {
        /// <summary>
        /// Nested class containing main condenser system parameters.
        /// Single-pass, three-shell, multipressure, deaerating surface condenser
        /// with titanium tubes.
        /// Source: NRC HRTD Condenser System Reference
        /// </summary>
        public static class Condenser
        {
            #region Condenser Geometry

            // =================================================================
            // Three-shell multipressure condenser: Shells A (LP), B (IP), C (HP)
            // CW flows in series A → B → C (progressively warming).
            // Total ~60,000 titanium tubes, ~900,000 ft² heat transfer area.
            //
            // Source: NRC HRTD Condenser System Reference
            // =================================================================

            /// <summary>Number of condenser shells (LP, IP, HP)</summary>
            public const int SHELL_COUNT = 3;

            /// <summary>Total condenser tube heat transfer area in ft²</summary>
            public const float TOTAL_HT_AREA_FT2 = 900_000f;

            /// <summary>Approximate number of condenser tubes</summary>
            public const int TUBE_COUNT = 60_000;

            /// <summary>Condenser tube outside diameter in inches (titanium)</summary>
            public const float TUBE_OD_IN = 1.25f;

            #endregion

            #region Design Vacuum and Backpressure

            // =================================================================
            // At design conditions (CW running, no/low steam load), the condenser
            // operates near 28 in. Hg vacuum (~1.5 psia backpressure).
            // At full steam dump load (40% rated), backpressure rises to ~3.3 psia.
            //
            // Source: NRC HRTD Condenser System Reference, shell pressure table
            // =================================================================

            /// <summary>
            /// Design condenser vacuum in inches Hg.
            /// Achieved with CW pumps running, air ejectors in service, and
            /// low or no steam load. Varies with CW inlet temperature (25-28 typical).
            /// Source: NRC HRTD Condenser System Reference
            /// </summary>
            public const float DESIGN_VACUUM_INHG = 28.0f;

            /// <summary>
            /// Design backpressure in psia (at design vacuum).
            /// = (ATM_PRESSURE_INHG - DESIGN_VACUUM_INHG) / INHG_PER_PSIA
            /// = (29.92 - 28.0) / 2.036 ≈ 0.94 psia
            /// Rounded to typical plant value for modeling.
            /// Source: Derived from design vacuum
            /// </summary>
            public const float DESIGN_BACKPRESSURE_PSIA = 1.5f;

            /// <summary>
            /// Maximum operating backpressure in psia under full steam dump load.
            /// At 40% rated steam flow through condenser, backpressure rises
            /// due to thermal equilibrium shift.
            /// Source: NRC HRTD Condenser System Reference, hotwell data
            /// </summary>
            public const float MAX_OPERATING_BACKPRESSURE_PSIA = 3.3f;

            /// <summary>
            /// Hotwell water temperature at design vacuum in °F.
            /// T_sat at ~1.5 psia ≈ 115-120°F.
            /// Source: NRC HRTD Condenser System Reference
            /// </summary>
            public const float HOTWELL_DESIGN_TEMP_F = 120f;

            #endregion

            #region C-9 Interlock — Condenser Available

            // =================================================================
            // C-9 is the "Condenser Available" permissive required for steam dump
            // operation. Both conditions must be met simultaneously:
            //   1. Condenser vacuum > 22 in. Hg (2/2 pressure switches)
            //   2. At least 1 CW pump running (1/2 breaker status)
            //
            // When C-9 is false, steam dump valves are blocked from opening;
            // steam must go to atmospheric relief (10% capacity) or SG safeties.
            //
            // Source: NRC HRTD Condenser System Reference, Section 11.2
            // =================================================================

            /// <summary>
            /// Minimum condenser vacuum for C-9 permissive in inches Hg.
            /// Two pressure switches (2/2 logic) must confirm vacuum above this.
            /// Source: NRC HRTD Condenser System Reference
            /// </summary>
            public const float C9_VACUUM_THRESHOLD_INHG = 22.0f;

            /// <summary>
            /// Minimum CW pumps running for C-9 permissive.
            /// At least one CW pump breaker must indicate closed (1/2 logic).
            /// Source: NRC HRTD Condenser System Reference
            /// </summary>
            public const int C9_MIN_CW_PUMPS = 1;

            #endregion

            #region P-12 Interlock — Low-Low Tavg

            // =================================================================
            // P-12 is the Low-Low Tavg interlock. When T_avg drops below the
            // P-12 setpoint, steam dump valves are blocked to prevent excessive
            // RCS cooldown. During startup, P-12 must be deliberately bypassed
            // (per procedure) to allow steam dump arming before reaching 553°F.
            //
            // Source: NRC HRTD Section 11.2 — Steam Dump Control System
            // =================================================================

            /// <summary>
            /// P-12 Low-Low Tavg threshold in °F.
            /// Below this temperature, P-12 is active and blocks steam dump
            /// operation unless bypassed per operator action.
            /// Source: NRC HRTD Section 11.2, PWR Startup State Sequence
            /// </summary>
            public const float P12_LOW_LOW_TAVG_F = 553.0f;

            /// <summary>
            /// Temperature at which P-12 bypass is typically engaged during startup in °F.
            /// Per startup procedure, operator bypasses P-12 when approaching Mode 3
            /// and condenser vacuum is established, to allow steam dump arming.
            /// Source: Operating procedure — typical startup practice
            /// </summary>
            public const float P12_BYPASS_ENGAGE_TEMP_F = 350.0f;

            #endregion

            #region Turbine Trip and Alarm Setpoints

            /// <summary>
            /// Turbine trip on high condenser backpressure in inches Hg absolute.
            /// 7.6 in. HgA ≈ ~22.3 in. Hg vacuum loss.
            /// Source: NRC HRTD Condenser System Reference
            /// </summary>
            public const float TURBINE_TRIP_BACKPRESSURE_INHGA = 7.6f;

            /// <summary>
            /// Turbine trip vacuum threshold in inches Hg (vacuum reading).
            /// When vacuum drops below this, turbine trip initiates.
            /// Source: NRC HRTD Condenser System Reference
            /// </summary>
            public const float TURBINE_TRIP_VACUUM_INHG = 20.0f;

            #endregion

            #region Circulating Water System

            // =================================================================
            // 2-4 CW pumps, each ~150,000 gpm, provide cooling water flow
            // through condenser tube bundles in series (A→B→C).
            // Temperature rise across condenser: 15-25°F typical.
            //
            // Source: NRC HRTD Condenser System Reference
            // =================================================================

            /// <summary>Total number of circulating water pumps</summary>
            public const int CW_PUMP_COUNT = 4;

            /// <summary>
            /// CW flow per pump in gpm.
            /// Source: NRC HRTD Condenser System Reference
            /// </summary>
            public const float CW_PUMP_FLOW_GPM = 150_000f;

            /// <summary>
            /// CW temperature rise across condenser in °F at design conditions.
            /// Source: NRC HRTD Condenser System Reference (15-25°F range)
            /// </summary>
            public const float CW_DELTA_T_F = 20.0f;

            /// <summary>CW pump head in feet</summary>
            public const float CW_PUMP_HEAD_FT = 35f;

            /// <summary>
            /// Maximum heat rejection per CW pump in BTU/hr.
            /// = 150,000 gpm × (500 lb/min/gpm × 60 min/hr) × 20°F
            /// = 150,000 × 500 × 20 = 1.5 × 10^9 BTU/hr per pump
            ///
            /// NOTE: This is theoretical maximum. Actual rejection depends on
            /// condensing load and CW inlet temperature.
            /// Source: Derived from CW pump flow and delta-T
            /// </summary>
            public const float CW_PUMP_MAX_REJECTION_BTUHR = 1.5e9f;

            #endregion

            #region Vacuum Dynamics

            // =================================================================
            // Condenser vacuum modeled as first-order lag system.
            // Backpressure tracks equilibrium with time constant τ=30s.
            //
            // Loss scenarios:
            //   - One CW pump lost: pressure rises over 30-60 seconds
            //   - All CW pumps lost: rapid rise, 10-20 seconds to trip
            //   - Steam dump opening: pressure rises within 2-5 seconds
            //   - Air ejector failure: gradual rise over minutes
            //
            // Source: NRC HRTD Condenser System Reference
            // =================================================================

            /// <summary>
            /// Condenser vacuum dynamics time constant in seconds.
            /// First-order lag: dP/dt = (P_eq - P_current) / τ
            /// Source: NRC HRTD Condenser System Reference
            /// </summary>
            public const float CONDENSER_TAU_SEC = 30f;

            /// <summary>
            /// Condenser vacuum dynamics time constant in hours.
            /// = CONDENSER_TAU_SEC / 3600
            /// </summary>
            public const float CONDENSER_TAU_HR = 30f / 3600f;

            #endregion

            #region Vacuum Pulldown Sequence

            // =================================================================
            // Vacuum establishment sequence during plant startup:
            //   1. Start CW pumps → establish tube-side flow
            //   2. Start hogging ejectors (3 units, 1,200 scfm each, single-stage)
            //      → pull vacuum from atmospheric to ~15-20 in. Hg
            //   3. Transfer to main air ejectors (2 units, 100%, two-stage)
            //   4. Continue to design vacuum (~26-28 in. Hg)
            //   5. C-9 satisfied when vacuum > 22 in. Hg with CW running
            //
            // Source: NRC HRTD Condenser System Reference
            // =================================================================

            /// <summary>
            /// Duration for hogging ejectors to reach transfer point in minutes.
            /// Hogging ejectors pull vacuum from atmospheric to ~18 in. Hg.
            /// Source: NRC HRTD Condenser System Reference
            /// </summary>
            public const float HOGGING_DURATION_MIN = 8f;

            /// <summary>
            /// Vacuum achieved by hogging ejectors before transfer in inches Hg.
            /// Main ejectors take over above this point.
            /// Source: NRC HRTD Condenser System Reference
            /// </summary>
            public const float HOGGING_TARGET_INHG = 18f;

            /// <summary>
            /// Duration for main ejectors to reach design vacuum after transfer in minutes.
            /// Source: NRC HRTD Condenser System Reference
            /// </summary>
            public const float MAIN_EJECTOR_RAMPUP_MIN = 8f;

            /// <summary>
            /// RCS temperature at which condenser vacuum pulldown is initiated in °F.
            /// Per startup procedure, condenser preparation begins approaching Mode 3
            /// to ensure C-9 is satisfied before steam dumps are needed.
            /// Source: Operating procedure — typical startup practice
            /// </summary>
            public const float CONDENSER_PREP_TEMP_F = 325f;

            #endregion

            #region Steam Dump Capacity (Reference)

            // =================================================================
            // Steam dump system can reject up to 40% of full-power steam flow.
            // 12 dump valves in 4 groups of 3. At no-load (1,106 psia), a single
            // valve can pass 895,000 lb/hr.
            //
            // Source: NRC HRTD Condenser System Reference
            // =================================================================

            /// <summary>
            /// Full-power condenser thermal rejection in BTU/hr.
            /// Source: NRC HRTD Condenser System Reference
            /// </summary>
            public const float FULL_POWER_REJECTION_BTUHR = 7.5e9f;

            /// <summary>
            /// Maximum steam dump flow (40% rated) in lb/hr.
            /// Source: NRC HRTD Condenser System Reference
            /// </summary>
            public const float STEAM_DUMP_MAX_FLOW_LBHR = 6_400_000f;

            /// <summary>
            /// Single steam dump valve flow at 1,106 psia in lb/hr.
            /// Source: NRC HRTD Condenser System Reference
            /// </summary>
            public const float STEAM_DUMP_SINGLE_VALVE_LBHR = 895_000f;

            /// <summary>Number of steam dump valves (4 groups × 3)</summary>
            public const int STEAM_DUMP_VALVE_COUNT = 12;

            #endregion

            #region Hotwell Level Control

            // =================================================================
            // Hotwell level control maintains inventory by balancing makeup
            // from CST and reject back to CST. Level is measured in inches.
            //
            // Source: NRC HRTD Condenser System Reference
            // =================================================================

            /// <summary>Normal hotwell level setpoint in inches</summary>
            public const float HOTWELL_NORMAL_LEVEL_IN = 24f;

            /// <summary>Hotwell level at which reject valve begins to open in inches</summary>
            public const float HOTWELL_REJECT_OPEN_IN = 28f;

            /// <summary>Hotwell level at which reject valve is fully open in inches</summary>
            public const float HOTWELL_REJECT_FULL_IN = 40f;

            /// <summary>Hotwell level at which makeup valve begins to open in inches</summary>
            public const float HOTWELL_MAKEUP_OPEN_IN = 21f;

            /// <summary>Hotwell level at which makeup valve is fully open in inches</summary>
            public const float HOTWELL_MAKEUP_FULL_IN = 8f;

            /// <summary>
            /// Hotwell design capacity in lb.
            /// Estimated from condenser geometry. Hotwell cross-section with
            /// ~24 in. normal level holds approximately 50,000-80,000 lb.
            /// Source: Engineering estimate from condenser dimensions
            /// </summary>
            public const float HOTWELL_DESIGN_MASS_LB = 60_000f;

            /// <summary>
            /// Hotwell total height range for level measurement in inches.
            /// Level instruments span 0 to 40 inches.
            /// Source: NRC HRTD Condenser System Reference
            /// </summary>
            public const float HOTWELL_LEVEL_SPAN_IN = 40f;

            #endregion

            #region Atmospheric Pressure Conversion

            /// <summary>
            /// Conversion factor: inches Hg per psia.
            /// 1 psia = 2.036 in. Hg
            /// </summary>
            public const float INHG_PER_PSIA = 2.036f;

            /// <summary>
            /// Standard atmospheric pressure in inches Hg.
            /// Vacuum = ATM_PRESSURE_INHG - (Backpressure_psia × INHG_PER_PSIA)
            /// </summary>
            public const float ATM_PRESSURE_INHG = 29.92f;

            #endregion
        }

        /// <summary>
        /// Nested class containing feedwater and auxiliary feedwater system parameters.
        /// Covers condensate pumps, main feedwater pumps, AFW pumps, CST,
        /// and feedwater temperature profile for startup.
        /// Source: NRC HRTD Section 7.2 and Section 5.7
        /// </summary>
        public static class Feedwater
        {
            #region Condensate Storage Tank (CST)

            // =================================================================
            // The CST is the primary water source for the AFW system and
            // provides hotwell makeup during startup. Tech Spec minimum
            // ensures adequate water for hot standby + cooldown.
            //
            // Source: NRC HRTD Section 5.7 — Auxiliary Feedwater System
            // =================================================================

            /// <summary>
            /// CST total capacity in gallons.
            /// Source: NRC HRTD Section 5.7
            /// </summary>
            public const float CST_TOTAL_CAPACITY_GAL = 450_000f;

            /// <summary>
            /// CST Technical Specification minimum volume in gallons.
            /// Basis: hot standby 2 hours + cooldown to 350°F in 4 hours.
            /// Source: NRC HRTD Section 5.7
            /// </summary>
            public const float CST_TECH_SPEC_MIN_GAL = 239_000f;

            /// <summary>
            /// CST unusable volume in gallons (below suction nozzle).
            /// Source: NRC HRTD Condenser System Reference
            /// </summary>
            public const float CST_UNUSABLE_GAL = 27_700f;

            /// <summary>
            /// CST water density in lb/gal (approximate, ~60-100°F range).
            /// Source: Water properties at storage temperature
            /// </summary>
            public const float CST_WATER_DENSITY_LB_GAL = 8.34f;

            #endregion

            #region Condensate Pumps

            // =================================================================
            // Two condensate pumps, each 70% capacity, eight-stage vertical
            // centrifugal. Suction from condenser B hotwell.
            //
            // Source: NRC HRTD Section 7.2
            // =================================================================

            /// <summary>Number of condensate pumps</summary>
            public const int CONDENSATE_PUMP_COUNT = 2;

            /// <summary>
            /// Condensate pump rated flow in gpm (each, 70% of plant capacity).
            /// Source: NRC HRTD Section 7.2
            /// </summary>
            public const float CONDENSATE_PUMP_FLOW_GPM = 11_000f;

            /// <summary>
            /// Condensate pump discharge head in psi.
            /// Eight-stage pump, 1,100 ft head ≈ 477 psi.
            /// Source: NRC HRTD Section 7.2
            /// </summary>
            public const float CONDENSATE_PUMP_HEAD_PSI = 477f;

            /// <summary>
            /// Condensate pump recirculation opens below this flow in gpm.
            /// Source: NRC HRTD Section 7.2
            /// </summary>
            public const float CONDENSATE_RECIRC_LOW_GPM = 3_500f;

            /// <summary>
            /// Condensate pump recirculation closes above this flow in gpm.
            /// Source: NRC HRTD Section 7.2
            /// </summary>
            public const float CONDENSATE_RECIRC_HIGH_GPM = 7_000f;

            #endregion

            #region Main Feedwater Pumps

            // =================================================================
            // Two MFPs, each 70% capacity, horizontal single-stage centrifugal
            // driven by nine-stage impulse turbine. Not started until ~20% power.
            //
            // Source: NRC HRTD Section 7.2
            // =================================================================

            /// <summary>Number of main feedwater pumps</summary>
            public const int MFP_COUNT = 2;

            /// <summary>
            /// MFP rated flow in gpm (each).
            /// Source: NRC HRTD Section 7.2
            /// </summary>
            public const float MFP_FLOW_GPM = 19_800f;

            /// <summary>
            /// MFP discharge head in feet.
            /// Source: NRC HRTD Section 7.2
            /// </summary>
            public const float MFP_DISCHARGE_HEAD_FT = 2_020f;

            /// <summary>
            /// MFP trip setpoint: low suction pressure in psig.
            /// Source: NRC HRTD Section 7.2
            /// </summary>
            public const float MFP_LOW_SUCTION_TRIP_PSIG = 195f;

            /// <summary>
            /// MFP trip setpoint: high discharge pressure in psig.
            /// Source: NRC HRTD Section 7.2
            /// </summary>
            public const float MFP_HIGH_DISCHARGE_TRIP_PSIG = 1_850f;

            /// <summary>
            /// MFP trip setpoint: turbine overspeed in rpm.
            /// Source: NRC HRTD Section 7.2
            /// </summary>
            public const float MFP_OVERSPEED_RPM = 5_850f;

            #endregion

            #region Auxiliary Feedwater System

            // =================================================================
            // AFW provides emergency and startup feedwater to SGs.
            //   2 motor-driven pumps: 440 gpm each, serve 2 SGs each
            //   1 turbine-driven pump: 880 gpm, serves all 4 SGs
            //   1 startup AFW pump: 1,020 gpm (manual start only)
            //
            // All three safety-grade pumps deliver rated flow within 1 minute.
            // Primary suction: CST. Backup: ESW (emergency service water).
            //
            // Source: NRC HRTD Section 5.7
            // =================================================================

            /// <summary>Number of motor-driven AFW pumps</summary>
            public const int AFW_MOTOR_PUMP_COUNT = 2;

            /// <summary>
            /// Motor-driven AFW pump rated flow in gpm (each).
            /// Source: NRC HRTD Section 5.7
            /// </summary>
            public const float AFW_MOTOR_FLOW_GPM = 440f;

            /// <summary>
            /// Motor-driven AFW pump discharge pressure in psig.
            /// Source: NRC HRTD Section 5.7
            /// </summary>
            public const float AFW_MOTOR_DISCHARGE_PSIG = 1_300f;

            /// <summary>
            /// Turbine-driven AFW pump rated flow in gpm (single pump, all 4 SGs).
            /// Source: NRC HRTD Section 5.7
            /// </summary>
            public const float AFW_TURBINE_FLOW_GPM = 880f;

            /// <summary>
            /// Turbine-driven AFW pump discharge pressure in psig.
            /// Source: NRC HRTD Section 5.7
            /// </summary>
            public const float AFW_TURBINE_DISCHARGE_PSIG = 1_200f;

            /// <summary>
            /// Minimum steam supply pressure for turbine-driven AFW in psig.
            /// Below this, turbine cannot maintain rated speed.
            /// Source: NRC HRTD Section 5.7
            /// </summary>
            public const float AFW_TURBINE_STEAM_MIN_PSIG = 100f;

            /// <summary>
            /// Maximum steam supply pressure for turbine-driven AFW in psig.
            /// Source: NRC HRTD Section 5.7
            /// </summary>
            public const float AFW_TURBINE_STEAM_MAX_PSIG = 1_275f;

            /// <summary>
            /// AFW system design pressure in psig.
            /// All AFW piping and components rated to this pressure.
            /// Source: NRC HRTD Section 5.7
            /// </summary>
            public const float AFW_SYSTEM_DESIGN_PRESSURE_PSIG = 1_650f;

            /// <summary>
            /// Time for AFW pumps to deliver rated flow after start signal in seconds.
            /// All three safety-grade pumps within 1 minute.
            /// Source: NRC HRTD Section 5.7
            /// </summary>
            public const float AFW_START_DELAY_SEC = 60f;

            /// <summary>
            /// Startup AFW pump rated flow in gpm (including 140 gpm recirculation).
            /// Manual start only; maintains 100 psid between discharge and SG-C.
            /// Source: NRC HRTD Section 7.2
            /// </summary>
            public const float STARTUP_AFW_FLOW_GPM = 1_020f;

            /// <summary>
            /// Startup AFW pump discharge head in psi.
            /// Source: NRC HRTD Section 7.2
            /// </summary>
            public const float STARTUP_AFW_HEAD_PSI = 1_472f;

            #endregion

            #region Feedwater Temperature Profile

            // =================================================================
            // Feedwater temperature depends on heater configuration during
            // startup. Pre-heater: ~hotwell temperature. Full heater train
            // raises temperature from 120°F to 440°F across 7 stages.
            //
            // Source: NRC HRTD Section 7.2
            // =================================================================

            /// <summary>
            /// CST water temperature in °F (ambient storage).
            /// Source: Typical CST storage conditions
            /// </summary>
            public const float FW_TEMP_CST_F = 100f;

            /// <summary>
            /// Hotwell condensate temperature in °F (at design vacuum).
            /// Source: NRC HRTD Condenser System Reference
            /// </summary>
            public const float FW_TEMP_HOTWELL_F = 120f;

            /// <summary>
            /// Feedwater temperature after 5 LP heater stages in °F.
            /// Source: NRC HRTD Section 7.2
            /// </summary>
            public const float FW_TEMP_AFTER_LP_HEATERS_F = 360f;

            /// <summary>
            /// Feedwater temperature after 2 HP heater stages (SG inlet) in °F.
            /// Source: NRC HRTD Section 7.2
            /// </summary>
            public const float FW_TEMP_AFTER_HP_HEATERS_F = 440f;

            #endregion

            #region SG Level Control Setpoints

            /// <summary>
            /// SG high-high level trip and FW isolation setpoint in % NR.
            /// 2/3 coincidence on any SG triggers reactor trip and FW isolation.
            /// Source: NRC HRTD Section 7.2
            /// </summary>
            public const float SG_HIGH_LEVEL_TRIP_PCT = 69.0f;

            /// <summary>
            /// SG normal startup level target in % NR.
            /// During startup, SGs are drained from wet layup to ~33% NR
            /// before beginning heatup.
            /// Source: NRC HRTD Section 7.2, startup procedures
            /// </summary>
            public const float SG_NORMAL_STARTUP_LEVEL_PCT = 33.0f;

            #endregion

            #region Water Properties (Reference)

            /// <summary>
            /// Water density at near-ambient conditions in lb/gal.
            /// Used for CST and hotwell volume↔mass conversions.
            /// </summary>
            public const float WATER_DENSITY_LB_PER_GAL = 8.34f;

            /// <summary>
            /// Water specific heat in BTU/(lb·°F).
            /// Approximately constant for condensate/feedwater range (100-440°F).
            /// </summary>
            public const float WATER_CP_BTU_LB_F = 1.0f;

            #endregion
        }
    }
}
