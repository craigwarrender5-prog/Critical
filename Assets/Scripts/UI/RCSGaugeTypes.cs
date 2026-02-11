// ============================================================================
// CRITICAL: Master the Atom - RCS Gauge Types Extension
// RCSGaugeTypes.cs - Extended Gauge Types for RCS Primary Loop Screen
// ============================================================================
//
// PURPOSE:
//   Extends the base GaugeType enum with RCS-specific gauge types.
//   This file provides a separate enum to avoid modifying the GOLD standard
//   MosaicTypes.cs file.
//
// USAGE:
//   Use RCSGaugeType for RCS screen gauges, or configure MosaicGauge
//   components with custom labels and thresholds via Inspector.
//
// NOTE:
//   In a future refactor, these types could be merged into the main
//   GaugeType enum in MosaicTypes.cs. For now, they exist separately
//   to maintain GOLD standard integrity.
//
// VERSION: 1.0.0
// DATE: 2026-02-09
// CLASSIFICATION: UI — Type Definitions
// ============================================================================

namespace Critical.UI
{
    /// <summary>
    /// Extended gauge types for RCS Primary Loop screen.
    /// These supplement the base GaugeType enum.
    /// </summary>
    public enum RCSGaugeType
    {
        // ====================================================================
        // TEMPERATURE GAUGES (per loop)
        // ====================================================================

        /// <summary>Loop 1 hot leg temperature (°F)</summary>
        Loop1_THot = 100,

        /// <summary>Loop 2 hot leg temperature (°F)</summary>
        Loop2_THot = 101,

        /// <summary>Loop 3 hot leg temperature (°F)</summary>
        Loop3_THot = 102,

        /// <summary>Loop 4 hot leg temperature (°F)</summary>
        Loop4_THot = 103,

        /// <summary>Loop 1 cold leg temperature (°F)</summary>
        Loop1_TCold = 110,

        /// <summary>Loop 2 cold leg temperature (°F)</summary>
        Loop2_TCold = 111,

        /// <summary>Loop 3 cold leg temperature (°F)</summary>
        Loop3_TCold = 112,

        /// <summary>Loop 4 cold leg temperature (°F)</summary>
        Loop4_TCold = 113,

        /// <summary>Average T-avg across all loops (°F)</summary>
        AverageTavg = 120,

        /// <summary>Core delta-T (°F)</summary>
        CoreDeltaT = 121,

        // ====================================================================
        // FLOW GAUGES
        // ====================================================================

        /// <summary>Total RCS flow (gpm)</summary>
        TotalRCSFlow = 200,

        /// <summary>Loop 1 flow rate (gpm)</summary>
        Loop1_Flow = 201,

        /// <summary>Loop 2 flow rate (gpm)</summary>
        Loop2_Flow = 202,

        /// <summary>Loop 3 flow rate (gpm)</summary>
        Loop3_Flow = 203,

        /// <summary>Loop 4 flow rate (gpm)</summary>
        Loop4_Flow = 204,

        // ====================================================================
        // RCP GAUGES
        // ====================================================================

        /// <summary>RCP-1 speed (RPM)</summary>
        RCP1_Speed = 300,

        /// <summary>RCP-2 speed (RPM)</summary>
        RCP2_Speed = 301,

        /// <summary>RCP-3 speed (RPM)</summary>
        RCP3_Speed = 302,

        /// <summary>RCP-4 speed (RPM)</summary>
        RCP4_Speed = 303,

        /// <summary>RCP-1 motor current (Amps)</summary>
        RCP1_Amps = 310,

        /// <summary>RCP-2 motor current (Amps)</summary>
        RCP2_Amps = 311,

        /// <summary>RCP-3 motor current (Amps)</summary>
        RCP3_Amps = 312,

        /// <summary>RCP-4 motor current (Amps)</summary>
        RCP4_Amps = 313,

        // ====================================================================
        // POWER GAUGES
        // ====================================================================

        /// <summary>Core thermal power (MW) - low range for heatup</summary>
        CorePowerMW = 400,

        /// <summary>RCP heat input (MW)</summary>
        RCPHeatInput = 401,

        // ====================================================================
        // PRESSURE GAUGES (for future Pressurizer screen)
        // ====================================================================

        /// <summary>RCS pressure (psia)</summary>
        RCSPressure = 500,

        /// <summary>Pressurizer pressure (psia)</summary>
        PZRPressure = 501,

        /// <summary>Pressurizer level (%)</summary>
        PZRLevel = 502,

        /// <summary>Pressurizer temperature (°F)</summary>
        PZRTemperature = 503
    }

    /// <summary>
    /// Provides specifications for RCS gauge types.
    /// </summary>
    public static class RCSGaugeSpecs
    {
        /// <summary>
        /// Get the display range for a gauge type.
        /// </summary>
        /// <param name="gaugeType">The gauge type</param>
        /// <returns>Tuple of (min, max) values</returns>
        public static (float min, float max) GetRange(RCSGaugeType gaugeType)
        {
            return gaugeType switch
            {
                // Temperature gauges: 100-700°F (wide range RTD)
                RCSGaugeType.Loop1_THot or
                RCSGaugeType.Loop2_THot or
                RCSGaugeType.Loop3_THot or
                RCSGaugeType.Loop4_THot or
                RCSGaugeType.Loop1_TCold or
                RCSGaugeType.Loop2_TCold or
                RCSGaugeType.Loop3_TCold or
                RCSGaugeType.Loop4_TCold or
                RCSGaugeType.AverageTavg => (100f, 700f),

                // Delta-T: 0-80°F
                RCSGaugeType.CoreDeltaT => (0f, 80f),

                // Per-loop flow: 0-120,000 gpm
                RCSGaugeType.Loop1_Flow or
                RCSGaugeType.Loop2_Flow or
                RCSGaugeType.Loop3_Flow or
                RCSGaugeType.Loop4_Flow => (0f, 120000f),

                // Total flow: 0-450,000 gpm
                RCSGaugeType.TotalRCSFlow => (0f, 450000f),

                // RCP speed: 0-1500 RPM
                RCSGaugeType.RCP1_Speed or
                RCSGaugeType.RCP2_Speed or
                RCSGaugeType.RCP3_Speed or
                RCSGaugeType.RCP4_Speed => (0f, 1500f),

                // RCP amps: 0-350 A
                RCSGaugeType.RCP1_Amps or
                RCSGaugeType.RCP2_Amps or
                RCSGaugeType.RCP3_Amps or
                RCSGaugeType.RCP4_Amps => (0f, 350f),

                // Core power MW (low range): 0-50 MW
                RCSGaugeType.CorePowerMW => (0f, 50f),

                // RCP heat: 0-30 MW
                RCSGaugeType.RCPHeatInput => (0f, 30f),

                // RCS/PZR pressure: 0-2500 psia
                RCSGaugeType.RCSPressure or
                RCSGaugeType.PZRPressure => (0f, 2500f),

                // PZR level: 0-100%
                RCSGaugeType.PZRLevel => (0f, 100f),

                // PZR temperature: 0-700°F
                RCSGaugeType.PZRTemperature => (0f, 700f),

                // Default
                _ => (0f, 100f)
            };
        }

        /// <summary>
        /// Get warning and alarm thresholds for a gauge type.
        /// </summary>
        /// <param name="gaugeType">The gauge type</param>
        /// <returns>Tuple of (warningLow, warningHigh, alarmLow, alarmHigh)</returns>
        public static (float warnLow, float warnHigh, float alarmLow, float alarmHigh) GetThresholds(RCSGaugeType gaugeType)
        {
            return gaugeType switch
            {
                // T-hot: warning >620°F, alarm >650°F
                RCSGaugeType.Loop1_THot or
                RCSGaugeType.Loop2_THot or
                RCSGaugeType.Loop3_THot or
                RCSGaugeType.Loop4_THot => (float.MinValue, 620f, float.MinValue, 650f),

                // T-cold: warning >560°F, alarm >580°F
                RCSGaugeType.Loop1_TCold or
                RCSGaugeType.Loop2_TCold or
                RCSGaugeType.Loop3_TCold or
                RCSGaugeType.Loop4_TCold => (float.MinValue, 560f, float.MinValue, 580f),

                // Avg Tavg: warning >595°F, alarm >610°F
                RCSGaugeType.AverageTavg => (float.MinValue, 595f, float.MinValue, 610f),

                // Delta-T: warning >65°F, alarm >70°F
                RCSGaugeType.CoreDeltaT => (float.MinValue, 65f, float.MinValue, 70f),

                // Loop flow: warning <70K, alarm <60K
                RCSGaugeType.Loop1_Flow or
                RCSGaugeType.Loop2_Flow or
                RCSGaugeType.Loop3_Flow or
                RCSGaugeType.Loop4_Flow => (70000f, float.MaxValue, 60000f, float.MaxValue),

                // Total flow: warning <280K, alarm <240K
                RCSGaugeType.TotalRCSFlow => (280000f, float.MaxValue, 240000f, float.MaxValue),

                // Core power: warning >30 MW, alarm >40 MW (during heatup)
                RCSGaugeType.CorePowerMW => (float.MinValue, 30f, float.MinValue, 40f),

                // RCS pressure: warning 2185-2285, alarm 2135-2335 psia
                RCSGaugeType.RCSPressure or
                RCSGaugeType.PZRPressure => (2185f, 2285f, 2135f, 2335f),

                // PZR level: warning 20-70%, alarm 15-75%
                RCSGaugeType.PZRLevel => (20f, 70f, 15f, 75f),

                // Default: no thresholds
                _ => (float.MinValue, float.MaxValue, float.MinValue, float.MaxValue)
            };
        }

        /// <summary>
        /// Get the unit string for a gauge type.
        /// </summary>
        /// <param name="gaugeType">The gauge type</param>
        /// <returns>Unit string</returns>
        public static string GetUnits(RCSGaugeType gaugeType)
        {
            return gaugeType switch
            {
                // Temperatures
                RCSGaugeType.Loop1_THot or
                RCSGaugeType.Loop2_THot or
                RCSGaugeType.Loop3_THot or
                RCSGaugeType.Loop4_THot or
                RCSGaugeType.Loop1_TCold or
                RCSGaugeType.Loop2_TCold or
                RCSGaugeType.Loop3_TCold or
                RCSGaugeType.Loop4_TCold or
                RCSGaugeType.AverageTavg or
                RCSGaugeType.CoreDeltaT or
                RCSGaugeType.PZRTemperature => "°F",

                // Flows
                RCSGaugeType.TotalRCSFlow or
                RCSGaugeType.Loop1_Flow or
                RCSGaugeType.Loop2_Flow or
                RCSGaugeType.Loop3_Flow or
                RCSGaugeType.Loop4_Flow => "gpm",

                // Speeds
                RCSGaugeType.RCP1_Speed or
                RCSGaugeType.RCP2_Speed or
                RCSGaugeType.RCP3_Speed or
                RCSGaugeType.RCP4_Speed => "RPM",

                // Currents
                RCSGaugeType.RCP1_Amps or
                RCSGaugeType.RCP2_Amps or
                RCSGaugeType.RCP3_Amps or
                RCSGaugeType.RCP4_Amps => "A",

                // Power
                RCSGaugeType.CorePowerMW or
                RCSGaugeType.RCPHeatInput => "MW",

                // Pressure
                RCSGaugeType.RCSPressure or
                RCSGaugeType.PZRPressure => "psia",

                // Level
                RCSGaugeType.PZRLevel => "%",

                _ => ""
            };
        }

        /// <summary>
        /// Get the display label for a gauge type.
        /// </summary>
        /// <param name="gaugeType">The gauge type</param>
        /// <returns>Display label string</returns>
        public static string GetLabel(RCSGaugeType gaugeType)
        {
            return gaugeType switch
            {
                RCSGaugeType.Loop1_THot => "LOOP 1 T-HOT",
                RCSGaugeType.Loop2_THot => "LOOP 2 T-HOT",
                RCSGaugeType.Loop3_THot => "LOOP 3 T-HOT",
                RCSGaugeType.Loop4_THot => "LOOP 4 T-HOT",
                RCSGaugeType.Loop1_TCold => "LOOP 1 T-COLD",
                RCSGaugeType.Loop2_TCold => "LOOP 2 T-COLD",
                RCSGaugeType.Loop3_TCold => "LOOP 3 T-COLD",
                RCSGaugeType.Loop4_TCold => "LOOP 4 T-COLD",
                RCSGaugeType.AverageTavg => "AVG T-AVG",
                RCSGaugeType.CoreDeltaT => "CORE ΔT",
                RCSGaugeType.TotalRCSFlow => "TOTAL RCS FLOW",
                RCSGaugeType.Loop1_Flow => "LOOP 1 FLOW",
                RCSGaugeType.Loop2_Flow => "LOOP 2 FLOW",
                RCSGaugeType.Loop3_Flow => "LOOP 3 FLOW",
                RCSGaugeType.Loop4_Flow => "LOOP 4 FLOW",
                RCSGaugeType.RCP1_Speed => "RCP-1 SPEED",
                RCSGaugeType.RCP2_Speed => "RCP-2 SPEED",
                RCSGaugeType.RCP3_Speed => "RCP-3 SPEED",
                RCSGaugeType.RCP4_Speed => "RCP-4 SPEED",
                RCSGaugeType.RCP1_Amps => "RCP-1 AMPS",
                RCSGaugeType.RCP2_Amps => "RCP-2 AMPS",
                RCSGaugeType.RCP3_Amps => "RCP-3 AMPS",
                RCSGaugeType.RCP4_Amps => "RCP-4 AMPS",
                RCSGaugeType.CorePowerMW => "CORE POWER",
                RCSGaugeType.RCPHeatInput => "RCP HEAT",
                RCSGaugeType.RCSPressure => "RCS PRESSURE",
                RCSGaugeType.PZRPressure => "PZR PRESSURE",
                RCSGaugeType.PZRLevel => "PZR LEVEL",
                RCSGaugeType.PZRTemperature => "PZR TEMP",
                _ => "UNKNOWN"
            };
        }
    }
}
