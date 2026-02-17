// ============================================================================
// CRITICAL: Master the Atom - Plant Constants (Core)
// PlantConstants.cs - Westinghouse 4-Loop PWR Core Reference Values (3411 MWt)
// ============================================================================
//
// PURPOSE:
//   Core plant reference constants for a Westinghouse 4-Loop PWR.
//   Reactor Coolant System geometry, steam generators, natural circulation,
//   surge line, insulation heat loss, physical constants, and unit conversions.
//
// SOURCES:
//   - NRC ML11223A342 Section 19.0 — Plant Operations
//   - NRC ML11223A213 Section 3.2 — RCS
//   - Westinghouse 4-Loop FSAR (South Texas, Vogtle, V.C. Summer)
//
// UNITS:
//   Temperature: °F | Pressure: psia | Flow: gpm | Volume: ft³
//   Mass: lb | Power: MW or kW | Time: hours (unless noted)
//
// ARCHITECTURE:
//   This is a partial class. All constants are accessed as PlantConstants.NAME
//   regardless of which file they reside in.
//
//   Partial files:
//     PlantConstants.cs              — Core RCS, SGs, physical constants, conversions
//     PlantConstants.CVCS.cs         — CVCS flows, VCT, CCP, seal flows
//     PlantConstants.Pressurizer.cs  — PZR geometry, bubble formation, heater control
//     PlantConstants.Pressure.cs     — Pressure setpoints, RCPs, RHR
//     PlantConstants.Nuclear.cs      — Core, reactivity, rods, xenon, boron, turbine
//     PlantConstants.Heatup.cs       — Heatup rates, mode temps, thermal mass
//     PlantConstants.Validation.cs   — ValidateConstants()
//
// NO CALCULATIONS IN THIS FILE — Static reference values and unit conversions only
//
// GOLD STANDARD: Yes
// ============================================================================

namespace Critical.Physics
{
    /// <summary>
    /// Static reference constants for Westinghouse 4-Loop PWR (3411 MWt).
    /// All values from NRC documentation and FSAR.
    /// Units: °F for temperature, psia for pressure, ft³ for volume, lb for mass, BTU for energy
    /// </summary>
    public static partial class PlantConstants
    {
        #region Reactor Coolant System (RCS)
        
        /// <summary>Core thermal power in MWt</summary>
        public const float THERMAL_POWER_MWT = 3411f;
        
        /// <summary>Core thermal power in BTU/hr (3411 MWt × 3.412e6 BTU/MWh)</summary>
        public const float THERMAL_POWER_BTU_HR = 3411f * 3.412e6f;
        
        /// <summary>Total RCS water volume in ft³</summary>
        public const float RCS_WATER_VOLUME = 11500f;
        
        /// <summary>Total RCS metal mass in lb (vessel, piping, SG tubes)</summary>
        public const float RCS_METAL_MASS = 2200000f;
        
        /// <summary>Normal operating pressure in psia</summary>
        public const float OPERATING_PRESSURE = PZR_BASELINE_PRESSURE_SETPOINT_PSIG + PSIG_TO_PSIA;
        
        /// <summary>Hot leg temperature in °F at 100% power</summary>
        public const float T_HOT = 619f;
        
        /// <summary>Cold leg temperature in °F at 100% power</summary>
        public const float T_COLD = 558f;
        
        /// <summary>Average coolant temperature in °F at 100% power</summary>
        public const float T_AVG = 588.5f;
        
        /// <summary>
        /// No-Load Tavg (Hot Zero Power) in °F.
        /// Source: NRC ML11223A342 Section 19.2 — "no-load Tavg of approximately 557°F"
        /// </summary>
        public const float T_AVG_NO_LOAD = 557f;
        
        /// <summary>Core temperature rise in °F at 100% power</summary>
        public const float CORE_DELTA_T = 61f;
        
        /// <summary>Total RCS flow rate in gpm (4 loops)</summary>
        public const float RCS_FLOW_TOTAL = 390400f;
        
        /// <summary>Total RCS flow rate in lb/sec</summary>
        public const float RCS_FLOW_LBM_SEC = 38400f;
        
        #endregion
        
        #region Steam Generators (Model F)
        
        /// <summary>Number of steam generators</summary>
        public const int SG_COUNT = 4;
        
        /// <summary>Heat transfer area per SG in ft²</summary>
        public const float SG_AREA_EACH = 55000f;
        
        /// <summary>Number of tubes per SG</summary>
        public const int SG_TUBES_EACH = 5626;
        
        /// <summary>Steam flow per SG in lb/hr</summary>
        public const float SG_STEAM_FLOW_EACH = 3.8e6f;
        
        /// <summary>Total steam flow in lb/hr</summary>
        public const float SG_STEAM_FLOW_TOTAL = 15.2e6f;
        
        /// <summary>Steam pressure in psia</summary>
        public const float SG_STEAM_PRESSURE = 1000f;
        
        /// <summary>Steam temperature in °F</summary>
        public const float SG_STEAM_TEMP = 545f;
        
        /// <summary>Feedwater temperature in °F</summary>
        public const float SG_FW_TEMP = 440f;
        
        /// <summary>Log mean temperature difference at 100% power in °F</summary>
        public const float LMTD_100_PERCENT = 43f;
        
        #endregion
        
        #region Natural Circulation
        
        /// <summary>Minimum natural circulation flow in gpm</summary>
        public const float NAT_CIRC_FLOW_MIN = 12000f;
        
        /// <summary>Maximum natural circulation flow in gpm</summary>
        public const float NAT_CIRC_FLOW_MAX = 23000f;
        
        /// <summary>Natural circulation as fraction of normal flow (minimum)</summary>
        public const float NAT_CIRC_PERCENT_MIN = 0.03f;
        
        /// <summary>Natural circulation as fraction of normal flow (maximum)</summary>
        public const float NAT_CIRC_PERCENT_MAX = 0.06f;
        
        #endregion
        
        #region Surge Line
        
        /// <summary>Surge line diameter in inches</summary>
        public const float SURGE_LINE_DIAMETER = 14f;
        
        /// <summary>Surge line length in ft</summary>
        public const float SURGE_LINE_LENGTH = 50f;
        
        /// <summary>Surge line friction factor (Darcy)</summary>
        public const float SURGE_LINE_FRICTION = 0.015f;
        
        #endregion
        
        #region Insulation Heat Loss
        
        /// <summary>
        /// Heat loss through RCS insulation at hot operating conditions in MW.
        /// This is the reference value at T_AVG ≈ 557°F.
        /// Includes losses from piping, vessel, SG primary side, pressurizer.
        /// </summary>
        public const float INSULATION_LOSS_HOT_MW = 1.5f;
        
        /// <summary>
        /// Containment ambient temperature in °F.
        /// Typical value during normal operation.
        /// </summary>
        public const float AMBIENT_TEMP_F = 80f;
        
        /// <summary>
        /// Reference temperature for heat loss calculation in °F.
        /// Heat loss scales linearly with (T_system - T_ambient).
        /// At cold shutdown (~100°F), loss is nearly zero (near thermal equilibrium).
        /// At hot operating (557°F), loss is 1.5 MW.
        /// </summary>
        public const float HEAT_LOSS_REF_TEMP_F = 557f;
        
        /// <summary>
        /// Heat loss coefficient in MW/°F.
        /// Calculated as INSULATION_LOSS_HOT_MW / (HEAT_LOSS_REF_TEMP_F - AMBIENT_TEMP_F)
        /// = 1.5 / (557 - 80) = 0.00314 MW/°F
        /// At T=100°F: Q_loss = 0.00314 × 20 = 0.063 MW
        /// At T=557°F: Q_loss = 0.00314 × 477 = 1.5 MW
        /// </summary>
        public const float HEAT_LOSS_COEFF_MW_PER_F = 1.5f / (557f - 80f);
        
        #endregion
        
        #region Physical Constants
        
        /// <summary>Steel specific heat in BTU/(lb·°F)</summary>
        public const float STEEL_CP = 0.12f;
        
        /// <summary>Water reference specific heat in BTU/(lb·°F)</summary>
        public const float WATER_CP_REF = 1.0f;
        
        /// <summary>Gravitational constant in ft/s²</summary>
        public const float GRAVITY = 32.174f;
        
        /// <summary>Atmospheric pressure in psia</summary>
        public const float P_ATM = 14.7f;
        
        /// <summary>Conversion: psig to psia</summary>
        public const float PSIG_TO_PSIA = 14.7f;
        
        #endregion
        
        #region Unit Conversions
        
        /// <summary>Conversion: gpm to ft³/sec</summary>
        public const float GPM_TO_FT3_SEC = 1f / 448.831f;
        
        /// <summary>Conversion: gpm to lb/sec (at ~46 lb/ft³)</summary>
        public const float GPM_TO_LBM_SEC = 46f / 448.831f;
        
        /// <summary>Conversion: kW to BTU/sec</summary>
        public const float KW_TO_BTU_SEC = 0.9478f;
        
        /// <summary>Conversion: MW to BTU/hr</summary>
        public const float MW_TO_BTU_HR = 3.412e6f;
        
        /// <summary>Conversion: MW to BTU/sec</summary>
        public const float MW_TO_BTU_SEC = 947.8f;
        
        /// <summary>Conversion: Rankine offset (°R = °F + 459.67)</summary>
        public const float RANKINE_OFFSET = 459.67f;
        
        /// <summary>Conversion: gallons to ft³</summary>
        public const float GAL_TO_FT3 = 0.133681f;
        
        /// <summary>Conversion: ft³ to gallons</summary>
        public const float FT3_TO_GAL = 7.48052f;
        
        #endregion
    }
}
