// ============================================================================
// CRITICAL: Master the Atom - Plant Constants (CVCS)
// PlantConstants.CVCS.cs - Chemical Volume Control System Constants
// ============================================================================
//
// DOMAIN: CVCS flows, letdown/charging, orifice sizing, seal injection,
//         Volume Control Tank, CCP capacity, bubble drain flow rates
//
// SOURCES:
//   - NRC ML11223A214 Section 4.1 — CVCS
//   - NRC IN 93-84 — RCP Seal Injection Requirements
//   - NRC ML11223A342 Section 19.0 — RHR Crossconnect Letdown
//   - NRC ML11223A342 Section 19.2.2 — Bubble Formation Drain Flows
//   - NRC HRTD 4.1 — CCP Capacity
//
// UNITS:
//   Flow: gpm | Volume: gallons or ft³ | Pressure: psia/psig
//   Temperature: °F | Concentration: ppm | Time: seconds (unless noted)
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
        #region Chemical Volume Control System (CVCS)
        
        /// <summary>
        /// Normal letdown flow in gpm.
        /// Source: NRC ML11223A342 Section 19.2.2 — "A letdown flow of 75 gallons per minute"
        /// During heatup, letdown is via RHR crossconnect (heat exchanger provides cooling)
        /// </summary>
        public const float LETDOWN_NORMAL_GPM = 75f;
        
        /// <summary>Maximum letdown flow in gpm</summary>
        public const float LETDOWN_MAX_GPM = 120f;
        
        /// <summary>
        /// Normal charging flow in gpm (total).
        /// Source: NRC ML11223A214 Section 4.1
        /// 55 gpm to RCS via charging line + 32 gpm seal injection = 87 gpm total
        /// </summary>
        public const float CHARGING_NORMAL_GPM = 87f;
        
        /// <summary>
        /// Charging flow to RCS in gpm (excluding seal injection).
        /// Source: NRC ML11223A214 — normal charging line flow
        /// </summary>
        public const float CHARGING_TO_RCS_GPM = 55f;
        
        /// <summary>
        /// Total RCP seal injection flow in gpm (all 4 pumps).
        /// Source: NRC IN 93-84 — 8 gpm per pump × 4 pumps = 32 gpm total
        /// </summary>
        public const float SEAL_INJECTION_TOTAL_GPM = 32f;
        
        /// <summary>
        /// RCP seal injection flow per pump in gpm.
        /// Source: NRC IN 93-84 — "approximately 8 gpm per pump"
        /// Of this, ~5 gpm flows to RCS, ~3 gpm is seal leakoff
        /// </summary>
        public const float SEAL_INJECTION_PER_PUMP_GPM = 8f;
        
        /// <summary>
        /// RCP seal flow to RCS per pump in gpm.
        /// Source: NRC IN 93-84 — of 8 gpm injection, ~5 gpm enters RCS
        /// </summary>
        public const float SEAL_FLOW_TO_RCS_PER_PUMP_GPM = 5f;
        
        /// <summary>
        /// RCP seal leakoff per pump in gpm.
        /// Source: NRC IN 93-84 — seal leakoff is ~3 gpm per pump
        /// </summary>
        public const float SEAL_LEAKOFF_PER_PUMP_GPM = 3f;
        
        /// <summary>Boron transport time in seconds (~10 min)</summary>
        public const float BORON_TRANSPORT_TIME = 600f;
        
        /// <summary>Boric acid concentration in ppm</summary>
        public const float BORIC_ACID_CONC = 7000f;
        
        /// <summary>Boron worth in pcm/ppm (negative = worth)</summary>
        public const float BORON_WORTH = -9f;
        
        #endregion
        
        #region Charging Pump (CCP) — Per NRC HRTD 4.1
        
        // =====================================================================
        // Centrifugal Charging Pump (CCP) Parameters
        // Source: NRC HRTD 4.1 — "CCP capacity is 44 gpm"
        // During Mode 5 without RCPs: full 44 gpm to charging line.
        // With RCPs running: 55 gpm charging + 32 gpm seal injection.
        // =====================================================================
        
        /// <summary>
        /// CCP capacity without RCP seal injection in gpm.
        /// Source: NRC HRTD 4.1 — single CCP output with no seal injection demand.
        /// This is the charging flow available during bubble formation (no RCPs).
        /// </summary>
        public const float CCP_CAPACITY_GPM = 44f;
        
        /// <summary>
        /// CCP total capacity with seal injection in gpm.
        /// Source: NRC HRTD 4.1 — 55 gpm charging + 32 gpm seal = 87 gpm.
        /// Used when RCPs are running and seal injection is required.
        /// </summary>
        public const float CCP_WITH_SEALS_GPM = 87f;
        
        /// <summary>
        /// PZR level at which CCP is started during bubble formation (%).
        /// Source: NRC HRTD 19.2.2 — "A charging pump is started to maintain
        /// pressurizer level constant during bubble formation."
        /// CCP starts when steam displacement drops level below this threshold.
        /// </summary>
        public const float CCP_START_LEVEL = 80f;
        
        #endregion
        
        #region Bubble Formation Drain Flows — Per NRC HRTD 19.2.2
        
        // =====================================================================
        // Drain Flow Rates During Bubble Formation
        // Source: NRC HRTD 19.2.2 / 19.0 / 4.1
        //
        // Primary drain mechanism: Thermodynamic steam displacement.
        // Secondary mechanism: CVCS letdown/charging imbalance.
        // Letdown via RHR crossconnect at 75 gpm throughout Mode 5.
        // Charging starts at 0 gpm, CCP at 44 gpm when level < 80%.
        // =====================================================================
        
        /// <summary>
        /// Letdown flow during bubble formation drain phase (gpm).
        /// Source: NRC HRTD 19.2.2 / 19.0 — letdown via RHR crossconnect at 75 gpm.
        /// During Mode 5 with RHR in service, letdown path is HCV-128 crossconnect.
        /// NOT increased to 120 gpm — that was the previous mechanical drain model.
        /// The real plant relies on steam displacement as primary drain mechanism,
        /// with CVCS providing secondary level trim only.
        /// </summary>
        public const float BUBBLE_DRAIN_LETDOWN_GPM = 75f;
        
        /// <summary>
        /// Initial charging flow during bubble formation drain phase (gpm).
        /// Source: NRC HRTD 19.2.2 — "no charging pump is running" at drain start.
        /// CCP starts later when level drops below CCP_START_LEVEL (80%).
        /// At CCP start: charging = CCP_CAPACITY_GPM (44 gpm).
        /// Net outflow after CCP: 75 - 44 = 31 gpm.
        /// </summary>
        public const float BUBBLE_DRAIN_CHARGING_INITIAL_GPM = 0f;
        
        /// <summary>
        /// Charging flow after CCP starts during bubble formation (gpm).
        /// Source: NRC HRTD 4.1 — CCP capacity = 44 gpm without seal injection.
        /// Net outflow = LETDOWN - CHARGING = 75 - 44 = 31 gpm.
        /// </summary>
        public const float BUBBLE_DRAIN_CHARGING_CCP_GPM = 44f;
        
        #endregion
        
        #region Volume Control Tank (VCT)
        
        // =====================================================================
        // VCT Design Parameters
        // Source: Westinghouse 4-Loop FSAR, NRC ML11223A214 Section 4.1
        // =====================================================================
        
        /// <summary>VCT total capacity in gallons</summary>
        public const float VCT_CAPACITY_GAL = 4000f;
        
        /// <summary>VCT normal operating band low limit (%)</summary>
        public const float VCT_LEVEL_NORMAL_LOW = 40f;
        
        /// <summary>VCT normal operating band high limit (%)</summary>
        public const float VCT_LEVEL_NORMAL_HIGH = 70f;
        
        /// <summary>VCT high-high level alarm/trip (%)</summary>
        public const float VCT_LEVEL_HIGH_HIGH = 90f;
        
        /// <summary>VCT high level alarm — divert to BRS (%)</summary>
        public const float VCT_LEVEL_HIGH = 73f;
        
        /// <summary>VCT low level alarm — start auto makeup (%)</summary>
        public const float VCT_LEVEL_MAKEUP_START = 25f;
        
        /// <summary>VCT low level alarm (%)</summary>
        public const float VCT_LEVEL_LOW = 17f;
        
        /// <summary>VCT low-low level — swap to RWST suction (%)</summary>
        public const float VCT_LEVEL_LOW_LOW = 5f;
        
        /// <summary>
        /// RCP seal return flow (controlled bleedoff) in gpm.
        /// Source: NRC IN 93-84 — seal leakoff returns to VCT
        /// 4 pumps × 3 gpm leakoff = 12 gpm total
        /// </summary>
        public const float SEAL_RETURN_NORMAL_GPM = 12f;
        
        /// <summary>
        /// Controlled bleedoff (CBO) loss rate in gpm.
        /// Small continuous loss from seal system that doesn't return to VCT
        /// </summary>
        public const float CBO_LOSS_GPM = 1f;
        
        /// <summary>
        /// Auto makeup flow rate in gpm.
        /// Flow from blending system when VCT level drops below setpoint
        /// </summary>
        public const float AUTO_MAKEUP_FLOW_GPM = 35f;
        
        /// <summary>
        /// Maximum makeup flow rate in gpm.
        /// Emergency makeup from RWST
        /// </summary>
        public const float MAX_MAKEUP_FLOW_GPM = 150f;
        
        /// <summary>
        /// VCT mixing time constant in seconds.
        /// Time for complete mixing of added water in VCT
        /// </summary>
        public const float VCT_MIXING_TAU = 120f;
        
        #endregion
        
        #region CVCS Operational Parameters — Per NRC HRTD Sections 4.1, 10.3, 19.0
        
        /// <summary>
        /// Letdown backpressure regulator setpoint (PCV-131) in psig.
        /// Source: NRC HRTD Section 4.1 — maintains upstream pressure to prevent flashing
        /// </summary>
        public const float LETDOWN_BACKPRESSURE_PSIG = 340f;
        
        /// <summary>
        /// Letdown backpressure in psia (= 340 + 14.7 = 354.7 psia)
        /// </summary>
        public const float LETDOWN_BACKPRESSURE_PSIA = 354.7f;
        
        /// <summary>
        /// Orifice flow coefficient K (gpm per sqrt(psi delta-P)).
        /// Sized so K * sqrt(2235 - 340) = 75 gpm at normal operating conditions.
        /// K = 75 / sqrt(1895) = 75 / 43.53 = 1.723
        /// Source: NRC HRTD Section 4.1 / CVCS analysis
        /// </summary>
        public const float ORIFICE_FLOW_COEFF_75 = 1.723f;
        
        /// <summary>
        /// RHR-to-CVCS crossconnect letdown flow in gpm (HCV-128).
        /// Source: NRC HRTD Section 19.0 — primary letdown path at low RCS pressure
        /// </summary>
        public const float RHR_CROSSCONNECT_FLOW_GPM = 75f;
        
        /// <summary>
        /// RHR letdown isolation temperature in °F.
        /// Source: NRC HRTD Section 19.0 — RHR isolated at 350°F, transition to orifice path
        /// </summary>
        public const float RHR_LETDOWN_ISOLATION_TEMP_F = 350f;
        
        /// <summary>
        /// PZR low-level letdown isolation setpoint (%).
        /// Source: NRC HRTD Section 10.3 — auto-close letdown isolation valves AND kill heaters
        /// </summary>
        public const float PZR_LOW_LEVEL_ISOLATION = 17f;
        
        /// <summary>
        /// PZR level program minimum setpoint (%) at no-load Tavg.
        /// Source: NRC HRTD Section 10.3, Figure 10.3-2
        /// </summary>
        public const float PZR_LEVEL_PROGRAM_MIN = 25f;
        
        /// <summary>
        /// PZR level program maximum setpoint (%) at full-load Tavg.
        /// Source: NRC HRTD Section 10.3, Figure 10.3-2
        /// </summary>
        public const float PZR_LEVEL_PROGRAM_MAX = 61.5f;
        
        /// <summary>
        /// PZR level program low Tavg (no-load) in °F.
        /// Source: NRC HRTD Section 10.3 — auctioneered-high Tavg at no-load
        /// </summary>
        public const float PZR_LEVEL_PROGRAM_TAVG_LOW = 557f;
        
        /// <summary>
        /// PZR level program high Tavg (full-load) in °F.
        /// Source: NRC HRTD Section 10.3 — auctioneered-high Tavg at full load
        /// </summary>
        public const float PZR_LEVEL_PROGRAM_TAVG_HIGH = 584.7f;
        
        /// <summary>
        /// PZR level program slope in %/°F.
        /// = (61.5 - 25) / (584.7 - 557) = 36.5 / 27.7 = 1.318
        /// Source: NRC HRTD Section 10.3
        /// </summary>
        public const float PZR_LEVEL_PROGRAM_SLOPE = 1.318f;
        
        /// <summary>
        /// Backup heater actuation offset above PZR level program in %.
        /// Source: NRC HRTD Section 10.3 — anticipate P drop from cool insurge
        /// </summary>
        public const float PZR_BACKUP_HEATER_LEVEL_OFFSET = 5f;
        
        /// <summary>
        /// PI level controller proportional gain (Kp) for PZR level control.
        /// Empirical: gpm per % level error
        /// </summary>
        public const float CVCS_LEVEL_KP = 3.0f;
        
        /// <summary>
        /// PI level controller integral gain (Ki) for PZR level control.
        /// Empirical: gpm per %-sec level error
        /// </summary>
        public const float CVCS_LEVEL_KI = 0.05f;
        
        /// <summary>
        /// PI level controller integral windup limit in gpm.
        /// Prevents excessive integral action during large transients
        /// </summary>
        public const float CVCS_LEVEL_INTEGRAL_LIMIT = 30f;
        
        /// <summary>
        /// Excess volume diverted to BRS during cold-to-hot heatup in gallons.
        /// Source: NRC HRTD Section 19.0 — ~30,000 gallons excess
        /// </summary>
        public const float HEATUP_EXCESS_VOLUME_GAL = 30000f;
        
        /// <summary>
        /// VCT divert setpoint (adjustable, operator-set level for LCV-112A) in %.
        /// Source: NRC HRTD Section 4.1 — proportional divert begins at this level
        /// </summary>
        public const float VCT_DIVERT_SETPOINT = 70f;
        
        /// <summary>
        /// VCT divert valve proportional band in %.
        /// LCV-112A ramps from 0% to 100% divert over this range above setpoint
        /// Source: NRC HRTD Section 4.1 — LCV-112A proportional 3-way valve
        /// </summary>
        public const float VCT_DIVERT_PROP_BAND = 20f;
        
        #endregion
        
        #region Letdown Orifice Lineup — Per NRC HRTD 4.1, WCAP Letdown Analysis
        
        // =====================================================================
        // Letdown Orifice Configuration
        // Source: NRC HRTD Section 4.1, Westinghouse CVCS Design
        //
        // The CVCS has three parallel letdown orifices:
        //   - Two 75-gpm orifices (at normal operating ΔP of ~1895 psi)
        //   - One 45-gpm orifice (at normal operating ΔP of ~1895 psi)
        //
        // Normal lineup: one 75-gpm orifice open (75 gpm at 2235 psig)
        // During heatup: operator opens additional orifices to manage
        // thermal expansion volume removal as RCS heats up.
        //
        // Ion exchanger downstream limit: 120 gpm max continuous flow
        // through the mixed-bed demineralizers. This limits practical
        // letdown to ~120 gpm even with all orifices open.
        //
        // Orifice flow coefficient for 45-gpm orifice:
        //   K_45 = 45 / sqrt(1895) = 45 / 43.53 = 1.034
        // =====================================================================
        
        /// <summary>
        /// Number of 75-gpm letdown orifices available.
        /// Source: NRC HRTD Section 4.1
        /// </summary>
        public const int LETDOWN_ORIFICE_75GPM_COUNT = 2;
        
        /// <summary>
        /// Number of 45-gpm letdown orifices available.
        /// Source: NRC HRTD Section 4.1
        /// </summary>
        public const int LETDOWN_ORIFICE_45GPM_COUNT = 1;
        
        /// <summary>
        /// 45-gpm orifice rated flow at normal operating ΔP (gpm).
        /// Source: NRC HRTD Section 4.1
        /// </summary>
        public const float LETDOWN_ORIFICE_45GPM_FLOW = 45f;
        
        /// <summary>
        /// Orifice flow coefficient for 45-gpm orifice.
        /// K = 45 / sqrt(2235 - 340) = 45 / sqrt(1895) = 45 / 43.53 = 1.034
        /// Source: Derived from rated flow and operating ΔP
        /// </summary>
        public const float ORIFICE_FLOW_COEFF_45 = 1.034f;
        
        /// <summary>
        /// Maximum letdown flow through ion exchangers (gpm).
        /// Source: NRC HRTD Section 4.1 — mixed-bed demineralizer flow limit.
        /// Even with all three orifices open (75+75+45 = 195 gpm at rated ΔP),
        /// total letdown is capped at 120 gpm to protect ion exchangers.
        /// </summary>
        public const float LETDOWN_ION_EXCHANGER_MAX_GPM = 120f;
        
        /// <summary>
        /// PZR level error threshold to open 45-gpm orifice (%).
        /// When PZR level exceeds setpoint by this amount, operator opens
        /// the 45-gpm orifice to increase letdown capacity.
        /// Source: Operational procedure — operator action based on level trend
        /// </summary>
        public const float ORIFICE_OPEN_45_LEVEL_ERROR = 5f;
        
        /// <summary>
        /// PZR level error threshold to open second 75-gpm orifice (%).
        /// When PZR level exceeds setpoint by this amount, operator opens
        /// a second 75-gpm orifice for maximum letdown capacity.
        /// Source: Operational procedure — operator action based on level trend
        /// </summary>
        public const float ORIFICE_OPEN_2ND75_LEVEL_ERROR = 10f;
        
        /// <summary>
        /// PZR level error hysteresis for closing additional orifices (%).
        /// Additional orifices are closed when level error drops below
        /// (open threshold - hysteresis) to prevent hunting.
        /// Source: Operational practice — avoid valve cycling
        /// </summary>
        public const float ORIFICE_CLOSE_HYSTERESIS = 3f;
        
        #endregion
        
        #region CVCS Flow Calculation Methods
        
        /// <summary>
        /// Convert VCT level (%) to volume (gallons)
        /// </summary>
        public static float VCTLevelToVolume(float level_percent)
        {
            return VCT_CAPACITY_GAL * level_percent / 100f;
        }
        
        /// <summary>
        /// Convert VCT volume (gallons) to level (%)
        /// </summary>
        public static float VCTVolumeToLevel(float volume_gal)
        {
            return 100f * volume_gal / VCT_CAPACITY_GAL;
        }
        
        /// <summary>
        /// Calculate letdown flow through a single 75-gpm orifice based on RCS pressure.
        /// Flow = K_75 * sqrt(P_rcs_psig - P_backpressure_psig).
        /// At 2235 psig: 75 gpm. At 400 psig: ~13 gpm. At 100 psig: 0 gpm.
        /// Source: NRC HRTD Section 4.1
        /// </summary>
        public static float CalculateOrificeLetdownFlow(float rcs_pressure_psig)
        {
            float deltaP = rcs_pressure_psig - LETDOWN_BACKPRESSURE_PSIG;
            if (deltaP <= 0f) return 0f;
            return ORIFICE_FLOW_COEFF_75 * (float)Math.Sqrt(deltaP);
        }
        
        /// <summary>
        /// Calculate letdown flow through a single 45-gpm orifice based on RCS pressure.
        /// Flow = K_45 * sqrt(P_rcs_psig - P_backpressure_psig).
        /// At 2235 psig: 45 gpm. At 400 psig: ~8 gpm. At 100 psig: 0 gpm.
        /// Source: NRC HRTD Section 4.1 / CVCS analysis
        /// </summary>
        public static float CalculateOrifice45LetdownFlow(float rcs_pressure_psig)
        {
            float deltaP = rcs_pressure_psig - LETDOWN_BACKPRESSURE_PSIG;
            if (deltaP <= 0f) return 0f;
            return ORIFICE_FLOW_COEFF_45 * (float)Math.Sqrt(deltaP);
        }
        
        /// <summary>
        /// v4.4.0: Calculate total orifice letdown flow for a given lineup.
        /// Sums flow from each open orifice at the current ΔP, then caps
        /// at the ion exchanger limit (120 gpm).
        /// 
        /// Per NRC HRTD 4.1: Three parallel orifices (2×75 + 1×45 gpm).
        /// Each orifice’s flow scales with sqrt(ΔP) independently.
        /// Total is capped by downstream ion exchanger capacity.
        /// </summary>
        /// <param name="rcs_pressure_psig">RCS pressure in psig</param>
        /// <param name="num75Open">Number of 75-gpm orifices open (0-2)</param>
        /// <param name="open45">True if the 45-gpm orifice is open</param>
        /// <returns>Total orifice letdown flow (gpm), capped at ion exchanger limit</returns>
        public static float CalculateOrificeLineupFlow(float rcs_pressure_psig, int num75Open, bool open45)
        {
            float deltaP = rcs_pressure_psig - LETDOWN_BACKPRESSURE_PSIG;
            if (deltaP <= 0f) return 0f;
            
            float sqrtDP = (float)Math.Sqrt(deltaP);
            float flow75 = num75Open * ORIFICE_FLOW_COEFF_75 * sqrtDP;
            float flow45 = open45 ? ORIFICE_FLOW_COEFF_45 * sqrtDP : 0f;
            float total = flow75 + flow45;
            
            // Cap at ion exchanger downstream limit
            return Math.Min(total, LETDOWN_ION_EXCHANGER_MAX_GPM);
        }
        
        /// <summary>
        /// Determine total letdown flow based on plant conditions and orifice lineup.
        /// Below 350°F: RHR crossconnect (75 gpm via HCV-128) or orifices, whichever higher.
        /// Above 350°F: Normal orifice path (pressure-dependent, lineup-dependent).
        /// Source: NRC HRTD Section 19.0
        /// 
        /// v4.4.0: Updated to accept separate orifice lineup parameters instead
        /// of a simple multiplier. This models the real plant’s mixed orifice
        /// sizes (2×75 + 1×45 gpm) with ion exchanger flow limit.
        /// </summary>
        /// <param name="T_rcs_F">RCS temperature (°F)</param>
        /// <param name="rcs_pressure_psia">RCS pressure (psia)</param>
        /// <param name="numOrificesOpen">Legacy: number of 75-gpm orifices (backward compat)</param>
        /// <param name="num75Open">Number of 75-gpm orifices open (0-2), -1 = use legacy</param>
        /// <param name="open45">True if the 45-gpm orifice is open</param>
        /// <returns>Total letdown flow (gpm)</returns>
        public static float CalculateTotalLetdownFlow(
            float T_rcs_F, float rcs_pressure_psia,
            int numOrificesOpen = 1,
            int num75Open = -1, bool open45 = false)
        {
            float rcs_pressure_psig = rcs_pressure_psia - PSIG_TO_PSIA;
            
            // v4.4.0: If num75Open is specified (>= 0), use the new lineup model
            // Otherwise fall back to legacy behavior for backward compatibility
            bool useNewModel = (num75Open >= 0);
            
            if (T_rcs_F < RHR_LETDOWN_ISOLATION_TEMP_F)
            {
                float rhrFlow = RHR_CROSSCONNECT_FLOW_GPM;
                float orificeFlow;
                if (useNewModel)
                    orificeFlow = CalculateOrificeLineupFlow(rcs_pressure_psig, num75Open, open45);
                else
                    orificeFlow = CalculateOrificeLetdownFlow(rcs_pressure_psig) * numOrificesOpen;
                return Math.Max(rhrFlow, orificeFlow);
            }
            else
            {
                float orificeFlow;
                if (useNewModel)
                    orificeFlow = CalculateOrificeLineupFlow(rcs_pressure_psig, num75Open, open45);
                else
                    orificeFlow = CalculateOrificeLetdownFlow(rcs_pressure_psig) * numOrificesOpen;
                // Legacy LETDOWN_MAX_GPM cap is superseded by ion exchanger limit
                // in the new model (already applied inside CalculateOrificeLineupFlow).
                // Keep legacy cap for backward compat path.
                return useNewModel ? orificeFlow : Math.Min(orificeFlow, LETDOWN_MAX_GPM);
            }
        }
        
        #endregion
    }
}
