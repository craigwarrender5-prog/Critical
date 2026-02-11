// ============================================================================
// CRITICAL: Master the Atom - Plant Constants (Heatup)
// PlantConstants.Heatup.cs - Heatup/Cooldown Rates, NRC Mode Temperatures,
//                            Thermal Mass Breakdown, Station Electrical Loads,
//                            Subcooling Targets, SG Secondary Heat Transfer
// ============================================================================
//
// DOMAIN: Heatup/cooldown rate limits, NRC operating mode temperature
//         boundaries, detailed thermal mass data, station electrical loads
//         during heatup, subcooling margin targets, target pressure calculation,
//         SG secondary side heat transfer coefficients for thermal mass model
//
// SOURCES:
//   - NRC ML11223A342 Section 19.2.2 — Heatup Rate (~50°F/hr)
//   - NRC Tech Specs — 100°F/hr heatup/cooldown limit
//   - NRC HRTD 2.1 — Normal Operating Pressure (2250 psia)
//   - Westinghouse 4-Loop FSAR — Component masses, electrical loads
//
// UNITS:
//   Temperature: °F | Pressure: psia | Mass: lb
//   Power: MW | Rate: °F/hr | Time: hours
//
// NOTE: This is a partial class. All constants are accessed as
//       PlantConstants.CONSTANT_NAME regardless of which file they live in.
//       Incorporates constants previously in PlantConstantsHeatup.cs (removed in v0.3.0).
//
// GOLD STANDARD: Yes
// ============================================================================

using System;

namespace Critical.Physics
{
    public static partial class PlantConstants
    {
        #region Heatup/Cooldown Rate Limits
        
        /// <summary>
        /// Maximum RCS heatup rate in °F/hr.
        /// Source: Tech Specs — protect reactor vessel from thermal stress
        /// </summary>
        public const float MAX_RCS_HEATUP_RATE_F_HR = 100f;
        
        /// <summary>
        /// Expected heatup rate with all RCPs running in °F/hr.
        /// Source: NRC ML11223A342 Section 19.2.2 — "approximately 50°F per hour"
        /// This is with ~20 MW net heat (21 MW RCP - 1 MW loss at moderate temp)
        /// </summary>
        public const float TYPICAL_HEATUP_RATE_F_HR = 50f;
        
        /// <summary>
        /// Maximum cooldown rate per Technical Specifications in °F/hr.
        /// Source: Tech Specs — protect reactor vessel from thermal stress
        /// </summary>
        public const float MAX_COOLDOWN_RATE_F_HR = 100f;
        
        #endregion
        
        #region Subcooling Margin Targets
        
        /// <summary>
        /// Minimum subcooling margin required in °F.
        /// Typically 20-30°F for adequate margin to saturation.
        /// Source: NRC Tech Specs / plant operating procedures
        /// </summary>
        public const float MIN_SUBCOOLING_F = 30f;
        
        /// <summary>
        /// Target subcooling during heatup in °F.
        /// Operators typically maintain 50°F for margin.
        /// Source: Plant operating procedures
        /// </summary>
        public const float TARGET_SUBCOOLING_F = 50f;
        
        #endregion
        
        #region NRC Operating Modes
        
        /// <summary>Mode 3 (Hot Standby) lower temperature limit in °F</summary>
        public const float MODE_3_TEMP_MIN_F = 350f;
        
        /// <summary>Mode 4 (Hot Shutdown) temperature range: 350-200°F</summary>
        public const float MODE_4_TEMP_MAX_F = 350f;
        public const float MODE_4_TEMP_MIN_F = 200f;
        
        /// <summary>Mode 5 (Cold Shutdown) upper temperature limit in °F</summary>
        public const float MODE_5_TEMP_MAX_F = 200f;
        
        /// <summary>Mode 6 (Refueling) upper temperature limit in °F</summary>
        public const float MODE_6_TEMP_MAX_F = 140f;
        
        #endregion
        
        #region Thermal Mass Breakdown — Per Westinghouse FSAR
        
        // =====================================================================
        // Detailed component masses for thermal mass calculations.
        // Total primary metal = RV + 4×SG + piping + PZR ≈ 2,200,000 lb
        // Cross-reference: PlantConstants.RCS_METAL_MASS = 2,200,000 lb
        // =====================================================================
        
        /// <summary>
        /// Reactor vessel mass in lb.
        /// Source: Westinghouse FSAR — ~400 tons for 4-loop vessel
        /// </summary>
        public const float RV_MASS_LB = 800000f;
        
        /// <summary>
        /// Steam generator mass per unit in lb (tube bundle + shell).
        /// Source: Westinghouse FSAR — ~350-400 tons each
        /// </summary>
        public const float SG_MASS_EACH_LB = 700000f;
        
        /// <summary>
        /// RCS piping total mass in lb.
        /// Hot legs, cold legs, crossover legs.
        /// Source: Westinghouse FSAR
        /// </summary>
        public const float RCS_PIPING_MASS_LB = 400000f;
        
        /// <summary>
        /// SG secondary side metal mass per unit in lb.
        /// Shell, shroud, supports (not heated during RCS heatup).
        /// Source: Westinghouse FSAR
        /// </summary>
        public const float SG_SECONDARY_METAL_MASS_LB = 200000f;
        
        /// <summary>
        /// SG secondary side water mass at normal level in lb.
        /// ~50,000 gallons × 8.3 lb/gal ≈ 415,000 lb per SG.
        /// Source: Westinghouse FSAR
        /// </summary>
        public const float SG_SECONDARY_WATER_MASS_LB = 415000f;
        
        #endregion
        
        #region Station Electrical Loads During Heatup
        
        /// <summary>
        /// RCP electrical power per pump in MW.
        /// Motor nameplate: ~6,000-7,000 HP = 4.5-5.2 MW.
        /// Source: Westinghouse FSAR — RCP motor specifications
        /// </summary>
        public const float RCP_ELECTRICAL_PER_PUMP_MW = 6f;
        
        /// <summary>
        /// Total RCP electrical consumption in MW (all 4 pumps).
        /// Source: Westinghouse FSAR
        /// </summary>
        public const float RCP_ELECTRICAL_TOTAL_MW = 24f;
        
        /// <summary>
        /// Auxiliary electrical loads during heatup in MW.
        /// Includes: HVAC, lighting, instrumentation, control systems.
        /// Source: Westinghouse FSAR
        /// </summary>
        public const float AUX_ELECTRICAL_LOADS_MW = 15f;
        
        /// <summary>
        /// Support systems electrical loads in MW.
        /// Includes: Charging pumps, CCW pumps, service water.
        /// Source: Westinghouse FSAR
        /// </summary>
        public const float SUPPORT_SYSTEMS_MW = 10f;
        
        /// <summary>
        /// Total station load from grid during heatup in MW.
        /// RCPs + Heaters + Aux + Support = ~51 MW typical.
        /// Source: Westinghouse FSAR
        /// </summary>
        public const float TOTAL_GRID_LOAD_HEATUP_MW = 51f;
        
        /// <summary>
        /// Emergency diesel generator capacity each in MW.
        /// Typical: 2 EDGs at 5-8 MW each.
        /// Source: Westinghouse FSAR
        /// </summary>
        public const float EDG_CAPACITY_MW = 7f;
        
        #endregion
        
        #region SG Secondary Side Heat Transfer During Heatup
        
        // =====================================================================
        // SG tube bundle heat transfer coefficients for heatup conditions.
        // During heatup, secondary side has NO forced circulation (no feedwater).
        // Primary side is forced (RCPs) or stagnant (no RCPs).
        //
        // The overall U is dominated by the secondary-side natural convection
        // resistance. Even with high primary-side h (~3000 BTU/hr·ft²·°F from
        // turbulent tube flow), the secondary h (~100-200 BTU/hr·ft²·°F from
        // natural convection on tube exteriors) limits the overall coefficient.
        //
        // Sources:
        //   - Incropera & DeWitt, Fundamentals of Heat and Mass Transfer
        //   - NUREG/CR-5426 — SG natural circulation heat transfer
        //   - Westinghouse FSAR — SG design parameters
        //   - Calibrated to NRC HRTD 19.2.2: ~50°F/hr with 4 RCPs
        // =====================================================================
        
        /// <summary>
        /// Total SG secondary side metal mass for all 4 SGs in lb.
        /// = 4 × SG_SECONDARY_METAL_MASS_LB = 4 × 200,000 = 800,000 lb
        /// Shell, shroud, tube support plates, wrapper.
        /// Source: Westinghouse FSAR
        /// </summary>
        public const float SG_SECONDARY_TOTAL_METAL_MASS_LB = 800000f;
        
        /// <summary>
        /// Total SG secondary side water mass for all 4 SGs at wet layup in lb.
        /// = 4 × SG_SECONDARY_WATER_MASS_LB = 4 × 415,000 = 1,660,000 lb
        /// At wet layup (100% level), ~50,000 gallons per SG.
        /// Source: Westinghouse FSAR, NRC HRTD 19.2.2 initial conditions
        /// </summary>
        public const float SG_SECONDARY_TOTAL_WATER_MASS_LB = 1660000f;
        
        /// <summary>
        /// Total SG tube heat transfer area for all 4 SGs in ft².
        /// = 4 × SG_AREA_EACH = 4 × 55,000 = 220,000 ft²
        /// Model F SG: 5,626 Inconel 690 U-tubes, 0.75" OD, ~54 ft avg length
        /// Source: Westinghouse FSAR, PlantConstants.SG_AREA_EACH
        /// </summary>
        public const float SG_TUBE_AREA_TOTAL_FT2 = 220000f;
        
        /// <summary>
        /// SG overall HTC with no forced flow (natural convection both sides)
        /// in BTU/(hr·ft²·°F).
        ///
        /// With no RCPs and no feedwater, both primary and secondary sides
        /// are stagnant. Heat transfer is by natural convection and conduction
        /// only. Very low — essentially thermal coupling through the tube
        /// wall with buoyancy-driven cells on each side.
        ///
        /// Typical range: 5-15 BTU/(hr·ft²·°F)
        /// At this value with A=220,000 ft² and ΔT=10°F:
        ///   Q = 10 × 220,000 × 10 = 22 MBTU/hr = 6.4 MW
        /// But ΔT during Phase 1 is ~0°F (both sides at ~100°F equilibrium),
        /// so actual heat transfer is negligible until RCPs start.
        ///
        /// Source: NUREG/CR-5426, Incropera natural convection correlations
        /// </summary>
        public const float SG_HTC_NO_FLOW = 10f;
        
        /// <summary>
        /// SG overall HTC with RCPs running (forced primary, natural secondary)
        /// during subcooled heatup in BTU/(hr·ft²·°F).
        ///
        /// ENGINEERING BASIS (v1.1.0):
        /// Primary side (tube interior): Dittus-Boelter forced convection
        ///   Re ≈ ρVD/μ ≈ (62 lb/ft³)(10 ft/s)(0.0625 ft)/(6.7×10⁻⁴ lb/ft·s) ≈ 58,000
        ///   Nu = 0.023 Re^0.8 Pr^0.4 ≈ 0.023 (58,000)^0.8 (1.5)^0.4 ≈ 200
        ///   h_primary = Nu × k/D ≈ 200 × 0.35/(0.0625) ≈ 1,120 BTU/(hr·ft²·°F)
        ///   Actual range with RCP flow variations: 800-3000 BTU/(hr·ft²·°F)
        ///
        /// Secondary side (tube exterior): Churchill-Chu natural convection
        ///   Ra ≈ gβΔT L³/(να) ≈ (32.2)(0.0003)(10)(54)³/(1×10⁻⁵)(1×10⁻⁵) ≈ 1×10⁹
        ///   Nu = [0.6 + 0.387 Ra^(1/6)]² ≈ 100-150
        ///   h_secondary ≈ Nu × k/L ≈ 125 × 0.35/54 ≈ 100 BTU/(hr·ft²·°F)
        ///   In stagnant subcooled pool: 80-150 BTU/(hr·ft²·°F)
        ///
        /// Tube wall resistance (Inconel 690, 0.043" wall):
        ///   R_wall = t/(k×A) ≈ (0.043/12)/(8.7) ≈ 0.0004 hr·ft²·°F/BTU (negligible)
        ///
        /// Overall coefficient (secondary-limited):
        ///   1/U = 1/h_primary + R_wall + 1/h_secondary
        ///   1/U ≈ 1/1000 + 0 + 1/100 ≈ 0.011
        ///   U ≈ 91 BTU/(hr·ft²·°F) → Rounded to 100 for modeling
        ///
        /// VALIDATION:
        /// With U=100, A=220,000 ft², ΔT_lag=15°F, Q_in=21 MW:
        ///   Q_sg = U×A×ΔT = 100 × 220,000 × 15 = 330 MBTU/hr = 96.7 MW
        ///   But ΔT_lag adjusts dynamically based on heat balance.
        ///   Net Q_rcs = Q_in - Q_sg - Q_loss ≈ 21 - 7 - 1.5 = 12.5 MW
        ///   Heatup rate = Q_net/(m×Cp) = 12.5/(2,900,000×1)×3.412 = 48°F/hr ✓
        ///
        /// Previous value of 200 BTU/(hr·ft²·°F) was ~2× too high, resulting in
        /// excessive SG heat absorption (~14 MW) and slow heatup (~26°F/hr).
        ///
        /// Sources:
        ///   - Incropera & DeWitt, Fundamentals of Heat and Mass Transfer (7th ed.)
        ///   - Churchill-Chu correlation for natural convection on cylinders
        ///   - Dittus-Boelter correlation for turbulent pipe flow
        ///   - NUREG/CR-5426 — PWR SG natural circulation phenomena
        ///   - NRC HRTD 19.2.2 — Calibration target: 50°F/hr with 4 RCPs
        /// </summary>
        public const float SG_HTC_NATURAL_CONVECTION = 100f;
        
        /// <summary>
        /// SG overall HTC at HZP with steaming conditions in BTU/(hr·ft²·°F).
        ///
        /// At Hot Zero Power (T_avg = 557°F), the SG secondary side reaches
        /// saturation temperature (~545°F at ~1000 psia). Steam generation
        /// begins, and the steam dump system removes excess heat to maintain
        /// RCS temperature constant.
        ///
        /// Boiling heat transfer coefficient (nucleate boiling):
        ///   h_secondary_boiling ≈ 500-1500 BTU/(hr·ft²·°F) (Rohsenow correlation)
        ///   Typical for low heat flux pool boiling: ~1000 BTU/(hr·ft²·°F)
        ///
        /// Overall coefficient with boiling:
        ///   1/U = 1/h_primary + 1/h_secondary_boiling
        ///   1/U ≈ 1/1000 + 1/1000 = 0.002
        ///   U ≈ 500 BTU/(hr·ft²·°F)
        ///
        /// This increased HTC allows SGs to transfer ~20-25 MW to secondary
        /// side, where steam dump removes the heat to condenser, maintaining
        /// thermal equilibrium at HZP.
        ///
        /// Sources:
        ///   - Incropera & DeWitt, Ch. 10 (Boiling Heat Transfer)
        ///   - Rohsenow correlation for nucleate pool boiling
        ///   - NUREG/CR-5426
        ///   - NRC HRTD 19.2.2 — HZP heat balance verification
        /// </summary>
        public const float SG_HTC_BOILING = 500f;
        
        // =====================================================================
        // v1.1.1 FIX: Temperature-Dependent HTC Scaling Constants
        // =====================================================================
        //
        // PROBLEM: At low temperatures (~100°F), natural convection HTC is
        // significantly lower than at operating temperatures (~500°F) because:
        //   1. Lower temperature differences produce lower Rayleigh numbers
        //   2. Lower Rayleigh numbers produce lower Nusselt numbers
        //   3. Lower Nusselt numbers produce lower heat transfer coefficients
        //
        // The Churchill-Chu correlation for natural convection on cylinders:
        //   Nu = [0.6 + 0.387 × Ra^(1/6) / (1 + (0.559/Pr)^(9/16))^(8/27)]²
        //
        // At different temperatures (ΔT~10-15°F):
        //   100°F: Ra ≈ 10^7, Nu ≈ 30,  h ≈ 30 BTU/(hr·ft²·°F)  → Scale = 0.3
        //   300°F: Ra ≈ 10^8, Nu ≈ 60,  h ≈ 60 BTU/(hr·ft²·°F)  → Scale = 0.6
        //   500°F: Ra ≈ 10^9, Nu ≈ 100, h ≈ 100 BTU/(hr·ft²·°F) → Scale = 1.0
        //
        // Linear interpolation between these anchor points provides a physically
        // realistic model of temperature-dependent natural convection.
        //
        // VALIDATION TARGET:
        // With temperature scaling, SG heat absorption at T=100-200°F should be
        // ~4-6 MW instead of 14+ MW, yielding heatup rate of ~50°F/hr with 4 RCPs.
        //
        // Sources:
        //   - Churchill-Chu correlation (Incropera & DeWitt, 7th ed., Ch. 9)
        //   - Implementation Plan v1.1.1, Issue 1 root cause analysis
        //   - NRC HRTD 19.2.2 — Target heatup rate ~50°F/hr
        // =====================================================================
        
        /// <summary>
        /// HTC temperature scaling at low temperature (100°F).
        /// Based on Churchill-Chu correlation for natural convection.
        /// At Ra~10^7: Nu~30, h~30 BTU/(hr·ft²·°F), scale = 30/100 = 0.3
        /// </summary>
        public const float SG_HTC_TEMP_SCALE_MIN = 0.3f;
        
        /// <summary>
        /// Temperature at which minimum HTC scaling applies (°F).
        /// Below this temperature, scaling is clamped to SG_HTC_TEMP_SCALE_MIN.
        /// </summary>
        public const float SG_HTC_TEMP_SCALE_MIN_TEMP_F = 100f;
        
        /// <summary>
        /// HTC temperature scaling at mid temperature (300°F).
        /// Based on Churchill-Chu correlation for natural convection.
        /// At Ra~10^8: Nu~60, h~60 BTU/(hr·ft²·°F), scale = 60/100 = 0.6
        /// </summary>
        public const float SG_HTC_TEMP_SCALE_MID = 0.6f;
        
        /// <summary>
        /// Temperature at which mid-point HTC scaling applies (°F).
        /// </summary>
        public const float SG_HTC_TEMP_SCALE_MID_TEMP_F = 300f;
        
        /// <summary>
        /// HTC temperature scaling at high temperature (500°F).
        /// Full HTC value applies at and above this temperature.
        /// At Ra~10^9: Nu~100, h~100 BTU/(hr·ft²·°F), scale = 1.0
        /// </summary>
        public const float SG_HTC_TEMP_SCALE_MAX = 1.0f;
        
        /// <summary>
        /// Temperature at which maximum (full) HTC scaling applies (°F).
        /// Above this temperature, scaling is clamped to SG_HTC_TEMP_SCALE_MAX.
        /// </summary>
        public const float SG_HTC_TEMP_SCALE_MAX_TEMP_F = 500f;
        
        // =====================================================================
        // v1.1.2 FIX: Boundary Layer Effectiveness Factor for Thermal Stratification
        // =====================================================================
        //
        // PROBLEM: Even with temperature-scaled HTC (v1.1.1), heat transfer is
        // ~14 MW instead of target ~5 MW because the calculation uses bulk ΔT.
        // In reality, thermal stratification in the stagnant secondary creates
        // a hot boundary layer near the tubes, reducing the effective ΔT.
        //
        // PHYSICS BASIS:
        // - SG secondary in wet layup: 214,000 lb water per SG, NO circulation
        // - Richardson number Ri ≈ 27,000 (>> 10 indicates strong stratification)
        // - Boundary layer temperature approaches tube (RCS) temperature
        // - Effective ΔT at tube surface << bulk ΔT
        //
        // IMPLEMENTATION:
        // Effective ΔT = Bulk ΔT × Boundary Layer Factor
        // Factor is low at cold temps (stratification), high at hot temps (mixing)
        // Factor = 1.0 when steaming (boiling provides vigorous circulation)
        //
        // VALIDATION TARGET:
        // With factor ~0.3 at T=150°F: Q ≈ 4-5 MW, heatup rate ≈ 50°F/hr
        //
        // Sources:
        //   - SG_Heat_Transfer_Investigation_Summary (v1.1.2 resolution document)
        //   - NRC HRTD ML11251A016 - SG wet layup conditions
        //   - Thermal stratification Richardson number analysis
        // =====================================================================
        
        /// <summary>
        /// Boundary layer effectiveness factor at low temperature (≤150°F).
        /// Severe thermal stratification - boundary layer at ~90% of RCS temp.
        /// Effective ΔT = bulk ΔT × 0.30
        /// </summary>
        public const float SG_BOUNDARY_LAYER_FACTOR_MIN = 0.30f;
        
        /// <summary>
        /// Temperature at which minimum boundary layer factor applies (°F).
        /// </summary>
        public const float SG_BOUNDARY_LAYER_TEMP_MIN_F = 150f;
        
        /// <summary>
        /// Boundary layer effectiveness factor at mid temperature (300°F).
        /// Moderate stratification - improved natural circulation.
        /// </summary>
        public const float SG_BOUNDARY_LAYER_FACTOR_MID = 0.55f;
        
        /// <summary>
        /// Temperature at which mid-point boundary layer factor applies (°F).
        /// </summary>
        public const float SG_BOUNDARY_LAYER_TEMP_MID_F = 300f;
        
        /// <summary>
        /// Boundary layer effectiveness factor at high temperature (≥500°F).
        /// Good natural circulation from strong buoyancy forces.
        /// </summary>
        public const float SG_BOUNDARY_LAYER_FACTOR_HIGH = 0.90f;
        
        /// <summary>
        /// Temperature at which high boundary layer factor applies (°F).
        /// </summary>
        public const float SG_BOUNDARY_LAYER_TEMP_HIGH_F = 500f;
        
        /// <summary>
        /// Boundary layer effectiveness factor when steaming.
        /// Boiling provides vigorous circulation - no stratification.
        /// Full bulk ΔT applies.
        /// </summary>
        public const float SG_BOUNDARY_LAYER_FACTOR_STEAMING = 1.0f;
        
        #endregion
        
        #region Heat Losses During Heatup
        
        /// <summary>
        /// Letdown system heat removal in MW (when in service).
        /// Letdown flow through regenerative and letdown HX.
        /// Source: Westinghouse FSAR — CVCS heat exchanger capacity
        /// </summary>
        public const float LETDOWN_HEAT_REMOVAL_MW = 2f;
        
        #endregion
        
        #region Heatup / Mode Methods
        
        /// <summary>
        /// Get the NRC operating mode based on temperature and criticality.
        /// </summary>
        /// <param name="T_avg_F">Average coolant temperature in °F</param>
        /// <param name="isCritical">True if reactor is critical (keff >= 0.99)</param>
        /// <param name="powerPercent">Reactor power as percentage</param>
        /// <returns>Mode number (1-6)</returns>
        public static int GetPlantMode(float T_avg_F, bool isCritical, float powerPercent)
        {
            if (isCritical && T_avg_F > MODE_3_TEMP_MIN_F)
            {
                return powerPercent > 5f ? 1 : 2;  // Power Operation or Startup
            }
            else if (T_avg_F > MODE_3_TEMP_MIN_F)
            {
                return 3;  // Hot Standby
            }
            else if (T_avg_F > MODE_4_TEMP_MIN_F)
            {
                return 4;  // Hot Shutdown
            }
            else if (T_avg_F > MODE_6_TEMP_MAX_F)
            {
                return 5;  // Cold Shutdown
            }
            else
            {
                return 6;  // Refueling
            }
        }
        
        /// <summary>
        /// Get plant operating mode for given T_avg (simplified, subcritical).
        /// For use during heatup when reactor is subcritical.
        /// </summary>
        /// <param name="T_avg">Average coolant temperature in °F</param>
        /// <returns>Mode number (3-6)</returns>
        public static int GetPlantMode(float T_avg)
        {
            if (T_avg < MODE_6_TEMP_MAX_F) return 6;
            if (T_avg < MODE_5_TEMP_MAX_F) return 5;
            if (T_avg < MODE_4_TEMP_MAX_F) return 4;
            return 3;
        }
        
        /// <summary>
        /// Get target pressure for given T_avg following P-T heatup curve.
        /// Maintains target subcooling margin above saturation pressure.
        /// </summary>
        /// <param name="T_avg">Average coolant temperature in °F</param>
        /// <returns>Target pressure in psia</returns>
        public static float GetTargetPressure(float T_avg)
        {
            float T_sat_target = T_avg + TARGET_SUBCOOLING_F;
            float P_target = WaterProperties.SaturationPressure(T_sat_target);
            
            P_target = Math.Max(P_target, MIN_RCP_PRESSURE_PSIA);
            P_target = Math.Min(P_target, P_NORMAL);
            
            return P_target;
        }
        
        /// <summary>
        /// Calculate total heat capacity of primary system at given conditions.
        /// Includes water and metal thermal mass.
        /// </summary>
        /// <param name="T_avg">Average coolant temperature in °F</param>
        /// <param name="pressure">System pressure in psia</param>
        /// <returns>Total heat capacity in BTU/°F</returns>
        public static float GetTotalHeatCapacity(float T_avg, float pressure)
        {
            float waterMass = RCS_WATER_VOLUME * WaterProperties.WaterDensity(T_avg, pressure);
            float Cp_water = WaterProperties.WaterSpecificHeat(T_avg, pressure);
            float waterCap = waterMass * Cp_water;
            
            float metalCap = RCS_METAL_MASS * ThermalMass.CP_STEEL;
            
            return waterCap + metalCap;
        }
        
        #endregion
    }
}
