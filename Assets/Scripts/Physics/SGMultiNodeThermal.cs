// ============================================================================
// CRITICAL: Master the Atom â€” Multi-Node Steam Generator Thermal Model
// SGMultiNodeThermal.cs â€” Three-Regime SG Secondary Side Model
// ============================================================================
//
// File: Assets/Scripts/Physics/SGMultiNodeThermal.cs
// Module: Critical.Physics.SGMultiNodeThermal
// Responsibility: SG secondary thermodynamic regime/state evolution and heat-transfer accounting.
// Standards: GOLD v1.0, SRP/SOLID, Unity Hot-Path Guardrails
// Version: 5.5
// Last Updated: 2026-02-17
// Changes:
//   - 5.5 (2026-02-17): Added GOLD metadata fields and bounded change-history ledger.
//   - 5.4 (2026-02-16): Added steam-inventory pressure mode for isolated SG scenarios.
//   - 5.1 (2026-02-15): Added saturation tracking and steam-line warming support.
//   - 5.0 (2026-02-15): Introduced three-regime open-system boiling model.
//   - 4.3 (2026-02-14): Added secondary pressure tracking for startup validation.
//
// PURPOSE:
//   Models the SG secondary side as N vertical nodes with THREE distinct
//   thermodynamic regimes during heatup. This corrects the fundamental
//   physics error in v3.0.0â€“v4.3.0 which treated the secondary as a
//   closed system throughout all phases.
//
// PHYSICS BREAKTHROUGH (v5.0.0):
//   After 2000+ simulation runs across 6 model architectures, analysis
//   proved that NO closed-system model can simultaneously hit both the
//   45-55Â°F/hr heatup rate AND 20-40Â°F stratification targets. The
//   secondary thermal mass (1.76M BTU/Â°F, 1.82Ã— primary) creates an
//   impossible energy balance when treated as a sealed pool.
//
//   Full review of NRC HRTD 19.0 confirmed that real Westinghouse plants
//   do NOT try to heat 1.66 million pounds of stagnant water. The heatup
//   has three distinct thermodynamic phases:
//
//   REGIME 1 â€” SUBCOOLED (100â†’220Â°F, ~2.4 hours):
//     SG secondary is a closed stagnant pool at ~17 psia (Nâ‚‚ blanket).
//     Only upper tube bundle participates (thermocline model). Heat goes
//     to sensible heating of secondary water near tube surfaces.
//     This is the SHORT phase â€” only 26% of total heatup temperature range.
//
//   REGIME 2 â€” BOILING / OPEN SYSTEM (220â†’557Â°F, ~6.7 hours):
//     Once steam forms at ~220Â°F, the SG becomes an OPEN system. Energy
//     goes to latent heat of vaporization and EXITS as steam through open
//     MSIVs. Secondary temperature tracks T_sat(P_secondary), NOT sensible
//     temperature. The 1.66M lb thermal mass is irrelevant â€” you're boiling
//     a thin film at tube surfaces, not heating a swimming pool.
//     THIS IS HOW REAL PLANTS OVERCOME THE THERMAL MASS RATIO.
//
//   REGIME 3 â€” STEAM DUMP CONTROLLED (at 1092 psig):
//     Steam dumps open in steam pressure mode. T_sat(1092 psig) = 557Â°F =
//     no-load T_avg. All excess RCP heat dumped to condenser as steam.
//     Primary temperature stabilizes at 557Â°F (Hot Standby / HZP).
//
//   Energy balance validation:
//     Q_rcp = 24 MW | Q_primary_heatup = 14.3 MW (at 50°F/hr)
//     Q_losses = 1.5 MW | Q_steam_exit = 8.2 MW
//     Total heatup time: ~9.1 hours (consistent with NRC HRTD)
//
// HISTORY:
//   v2.x: Circulation-onset model (WRONG â€” treated stratification as
//          circulation trigger, caused 14-19 MW absorption, 26Â°F/hr rate)
//   v3.0.0: Thermocline model (correct for subcooled phase only)
//   v4.3.0: Added secondary pressure tracking (closed-system, quasi-static)
//   v5.0.0: Three-regime model with open-system boiling
//           Stage 1: Regime framework + subcooled phase isolation
//           Stage 2: Boiling/open system â€” energy exits as steam
//           Stage 3: Steam dump termination + HZP wiring
//   v5.0.1: Regime continuity blend + delta clamp
//           NodeRegimeBlend[] ramps HTC/area/Î”T over 60 sim-seconds
//           Delta clamp: |Î”Q| â‰¤ 5 MW/timestep with bypass conditions
//           Eliminates MW-scale step changes at regime transitions
//   v5.1.0: Saturation tracking, reversion guard, steam line warming,
//           wall superheat boiling driver
//   v5.4.0: Steam inventory model for isolated SG scenarios (THIS VERSION)
//           Stage 6: Tracks steam mass, volume, inventory-based pressure
//           When SteamIsolated=true, pressure uses P = mRT/V instead of P_sat
//
// SOURCES:
//   - NRC HRTD ML11223A342 Section 19.0 â€” Three-phase heatup, steam dumps
//   - NRC HRTD ML11223A294 Section 11.2 â€” Steam dumps at 1092 psig
//   - NRC HRTD ML11223A244 Section 7.1 â€” SG safety valves
//   - WCAP-8530 / WCAP-12700 â€” Westinghouse Model F SG design
//   - NRC HRTD ML11223A213 Section 5.0 â€” Steam Generators
//   - NRC HRTD ML11251A016 â€” SG wet layup conditions
//   - NUREG/CR-5426 â€” PWR SG natural circulation phenomena
//   - NRC Bulletin 88-11 â€” Thermal stratification in PWR systems
//   - Incropera & DeWitt Ch. 5, 9, 10 â€” Conduction, Natural convection, Boiling
//   - SG_HEATUP_BREAKTHROUGH_HANDOFF.md â€” 2000+ run analysis
//
// UNITS:
//   Temperature: Â°F | Heat Rate: BTU/hr | Heat Capacity: BTU/Â°F
//   Mass: lb | Area: ftÂ² | HTC: BTU/(hrÂ·ftÂ²Â·Â°F) | Time: hr
//
// ARCHITECTURE:
//   - Called by: RCSHeatup.BulkHeatupStep(), HeatupSimEngine.StepSimulation()
//   - Delegates to: WaterProperties (density, specific heat, latent heat)
//   - State owned: SGMultiNodeState struct (per-SG node temperatures)
//   - Uses constants from: PlantConstants, PlantConstants.SG
//
// GOLD STANDARD: Yes (v5.4.0)
// ============================================================================

using System;
using UnityEngine;

namespace Critical.Physics
{
    public static partial class SGMultiNodeThermal
    {
        // ====================================================================
        // CONSTANTS
        // ====================================================================

        // ====================================================================
        // STATIC TRACKING FIELDS (v5.0.1 â€” delta clamp bypass detection)
        // ====================================================================

        /// <summary>Previous timestep TotalHeatAbsorption_MW for delta clamp.</summary>
        private static float _prevTotalQ_MW = 0f;

        /// <summary>Previous timestep RCP count for bypass detection.</summary>
        private static int _prevRCPCount = 0;

        /// <summary>Previous timestep regime for steam dump activation edge detection.</summary>
        private static SGThermalRegime _prevRegime = SGThermalRegime.Subcooled;

        /// <summary>True after first Update() call (skip clamp on first frame).</summary>
        private static bool _clampInitialized = false;

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        // ====================================================================
        // PRIVATE METHODS
        // ====================================================================

        // ====================================================================
        // VALIDATION
        // ====================================================================
    }
}







