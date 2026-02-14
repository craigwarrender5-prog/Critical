// ============================================================================
// CRITICAL: Master the Atom — Multi-Node Steam Generator Thermal Model
// SGMultiNodeThermal.cs — Three-Regime SG Secondary Side Model
// ============================================================================
//
// PURPOSE:
//   Models the SG secondary side as N vertical nodes with THREE distinct
//   thermodynamic regimes during heatup. This corrects the fundamental
//   physics error in v3.0.0–v4.3.0 which treated the secondary as a
//   closed system throughout all phases.
//
// PHYSICS BREAKTHROUGH (v5.0.0):
//   After 2000+ simulation runs across 6 model architectures, analysis
//   proved that NO closed-system model can simultaneously hit both the
//   45-55°F/hr heatup rate AND 20-40°F stratification targets. The
//   secondary thermal mass (1.76M BTU/°F, 1.82× primary) creates an
//   impossible energy balance when treated as a sealed pool.
//
//   Full review of NRC HRTD 19.0 confirmed that real Westinghouse plants
//   do NOT try to heat 1.66 million pounds of stagnant water. The heatup
//   has three distinct thermodynamic phases:
//
//   REGIME 1 — SUBCOOLED (100→220°F, ~2.4 hours):
//     SG secondary is a closed stagnant pool at ~17 psia (N₂ blanket).
//     Only upper tube bundle participates (thermocline model). Heat goes
//     to sensible heating of secondary water near tube surfaces.
//     This is the SHORT phase — only 26% of total heatup temperature range.
//
//   REGIME 2 — BOILING / OPEN SYSTEM (220→557°F, ~6.7 hours):
//     Once steam forms at ~220°F, the SG becomes an OPEN system. Energy
//     goes to latent heat of vaporization and EXITS as steam through open
//     MSIVs. Secondary temperature tracks T_sat(P_secondary), NOT sensible
//     temperature. The 1.66M lb thermal mass is irrelevant — you're boiling
//     a thin film at tube surfaces, not heating a swimming pool.
//     THIS IS HOW REAL PLANTS OVERCOME THE THERMAL MASS RATIO.
//
//   REGIME 3 — STEAM DUMP CONTROLLED (at 1092 psig):
//     Steam dumps open in steam pressure mode. T_sat(1092 psig) = 557°F =
//     no-load T_avg. All excess RCP heat dumped to condenser as steam.
//     Primary temperature stabilizes at 557°F (Hot Standby / HZP).
//
//   Energy balance validation:
//     Q_rcp = 21 MW | Q_primary_heatup = 14.3 MW (at 50°F/hr)
//     Q_losses = 1.5 MW | Q_steam_exit = 5.2 MW
//     Total heatup time: ~9.1 hours (consistent with NRC HRTD)
//
// HISTORY:
//   v2.x: Circulation-onset model (WRONG — treated stratification as
//          circulation trigger, caused 14-19 MW absorption, 26°F/hr rate)
//   v3.0.0: Thermocline model (correct for subcooled phase only)
//   v4.3.0: Added secondary pressure tracking (closed-system, quasi-static)
//   v5.0.0: Three-regime model with open-system boiling
//           Stage 1: Regime framework + subcooled phase isolation
//           Stage 2: Boiling/open system — energy exits as steam
//           Stage 3: Steam dump termination + HZP wiring
//   v5.0.1: Regime continuity blend + delta clamp
//           NodeRegimeBlend[] ramps HTC/area/ΔT over 60 sim-seconds
//           Delta clamp: |ΔQ| ≤ 5 MW/timestep with bypass conditions
//           Eliminates MW-scale step changes at regime transitions
//   v5.1.0: Saturation tracking, reversion guard, steam line warming,
//           wall superheat boiling driver
//   v5.4.0: Steam inventory model for isolated SG scenarios (THIS VERSION)
//           Stage 6: Tracks steam mass, volume, inventory-based pressure
//           When SteamIsolated=true, pressure uses P = mRT/V instead of P_sat
//
// SOURCES:
//   - NRC HRTD ML11223A342 Section 19.0 — Three-phase heatup, steam dumps
//   - NRC HRTD ML11223A294 Section 11.2 — Steam dumps at 1092 psig
//   - NRC HRTD ML11223A244 Section 7.1 — SG safety valves
//   - WCAP-8530 / WCAP-12700 — Westinghouse Model F SG design
//   - NRC HRTD ML11223A213 Section 5.0 — Steam Generators
//   - NRC HRTD ML11251A016 — SG wet layup conditions
//   - NUREG/CR-5426 — PWR SG natural circulation phenomena
//   - NRC Bulletin 88-11 — Thermal stratification in PWR systems
//   - Incropera & DeWitt Ch. 5, 9, 10 — Conduction, Natural convection, Boiling
//   - SG_HEATUP_BREAKTHROUGH_HANDOFF.md — 2000+ run analysis
//
// UNITS:
//   Temperature: °F | Heat Rate: BTU/hr | Heat Capacity: BTU/°F
//   Mass: lb | Area: ft² | HTC: BTU/(hr·ft²·°F) | Time: hr
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
    // ========================================================================
    // THERMAL REGIME ENUM (v5.0.0)
    // Identifies the current thermodynamic phase of the SG secondary side.
    // Transitions are one-directional during heatup (Subcooled → Boiling → SteamDump).
    // Reverse transitions handled for cooldown scenarios.
    //
    // Source: NRC HRTD ML11223A342 Section 19.0 — three-phase heatup sequence
    // ========================================================================

    /// <summary>
    /// Thermodynamic regime of the SG secondary side during heatup.
    /// Determines whether energy goes to sensible heat (closed system)
    /// or latent heat / steam production (open system).
    /// </summary>
    public enum SGThermalRegime
    {
        /// <summary>
        /// All secondary nodes below T_sat — closed stagnant pool.
        /// Energy goes to sensible heating of secondary water.
        /// Thermocline model governs effective area and HTC.
        /// Duration: ~2.4 hours (100→220°F at 50°F/hr).
        /// </summary>
        Subcooled,

        /// <summary>
        /// At least one node at or above T_sat — open system.
        /// Energy primarily goes to latent heat of vaporization.
        /// Steam produced at tube surfaces exits via open MSIVs.
        /// Secondary temperature tracks T_sat(P_secondary).
        /// Duration: ~6.7 hours (220→557°F at 50°F/hr).
        /// </summary>
        Boiling,

        /// <summary>
        /// P_secondary ≥ steam dump setpoint (1092 psig).
        /// Steam dumps modulate to hold pressure constant.
        /// All excess RCP heat dumped to condenser as steam.
        /// T_rcs stabilizes at T_sat(1092 psig) = 557°F.
        /// </summary>
        SteamDump
    }

    /// <summary>
    /// Active pressure-source branch for SG secondary pressure.
    /// </summary>
    public enum SGPressureSourceMode
    {
        Floor,
        Saturation,
        InventoryDerived
    }

    // ========================================================================
    // STATE STRUCT
    // Persistent state for all 4 SGs. Owned by engine, passed by ref.
    // ========================================================================

    /// <summary>
    /// Persistent state for the multi-node SG secondary thermal model.
    /// Contains per-node temperatures for all SGs (modeled as identical).
    /// Created by Initialize(), updated by Update(), read by engine.
    /// </summary>
    public struct SGMultiNodeState
    {
        /// <summary>Per-node temperatures (°F), index 0 = top, N-1 = bottom</summary>
        public float[] NodeTemperatures;

        /// <summary>Per-node heat transfer rate (BTU/hr), last computed</summary>
        public float[] NodeHeatRates;

        /// <summary>Per-node effective area fraction, last computed</summary>
        public float[] NodeEffectiveAreaFractions;

        /// <summary>Per-node HTC (BTU/(hr·ft²·°F)), last computed</summary>
        public float[] NodeHTCs;

        /// <summary>Total heat absorption across all 4 SGs (MW)</summary>
        public float TotalHeatAbsorption_MW;

        /// <summary>Total heat absorption (BTU/hr)</summary>
        public float TotalHeatAbsorption_BTUhr;

        /// <summary>Bulk average secondary temperature (°F) — weighted by mass</summary>
        public float BulkAverageTemp_F;

        /// <summary>Top node temperature (°F) — hottest node</summary>
        public float TopNodeTemp_F;

        /// <summary>Bottom node temperature (°F) — coldest node</summary>
        public float BottomNodeTemp_F;

        /// <summary>ΔT between top and bottom nodes (°F)</summary>
        public float StratificationDeltaT_F;

        /// <summary>
        /// Thermocline position in feet from tubesheet (0 = bottom, 24 = top).
        /// Tubes above this height participate in active heat transfer.
        /// </summary>
        public float ThermoclineHeight_ft;

        /// <summary>
        /// Fraction of total tube area that is above the thermocline.
        /// This is the "active" fraction participating in heat transfer.
        /// </summary>
        public float ActiveAreaFraction;

        /// <summary>
        /// Elapsed heatup time in hours (drives thermocline descent).
        /// Reset when RCPs stop; only advances when RCPs are running.
        /// </summary>
        public float ElapsedHeatupTime_hr;

        /// <summary>True if boiling has begun in the top node</summary>
        public bool BoilingActive;

        /// <summary>Number of nodes in the model</summary>
        public int NodeCount;

        // ----- SG Secondary Pressure Model (v4.3.0) -----

        /// <summary>Current SG secondary side pressure in psia</summary>
        public float SecondaryPressure_psia;

        /// <summary>True once nitrogen blanket has been isolated (steam onset)</summary>
        public bool NitrogenIsolated;

        /// <summary>Current saturation temperature at secondary pressure in °F</summary>
        public float SaturationTemp_F;

        /// <summary>Superheat of hottest node above T_sat in °F (0 if subcooled)</summary>
        public float MaxSuperheat_F;

        // ----- Three-Regime Model (v5.0.0) -----

        /// <summary>
        /// Current thermodynamic regime of the SG secondary side.
        /// Subcooled: all nodes below T_sat, closed stagnant pool.
        /// Boiling: at least one node at T_sat, open system, steam production.
        /// SteamDump: P_secondary >= 1092 psig, heat rejection to condenser.
        /// Source: NRC HRTD ML11223A342 Section 19.0
        /// </summary>
        public SGThermalRegime CurrentRegime;

        /// <summary>
        /// Instantaneous steam production rate across all 4 SGs in lb/hr.
        /// Zero during Subcooled regime. Calculated from Q_boiling / h_fg
        /// during Boiling and SteamDump regimes.
        /// </summary>
        public float SteamProductionRate_lbhr;

        /// <summary>
        /// Steam production expressed as equivalent thermal power in MW.
        /// = SteamProductionRate_lbhr × h_fg / 3.412e6
        /// Represents the energy that EXITS the system as steam.
        /// </summary>
        public float SteamProductionRate_MW;

        /// <summary>
        /// Cumulative steam mass produced since boiling onset in lb (all 4 SGs).
        /// Tracks total mass that has exited the SG secondary as steam.
        /// </summary>
        public float TotalSteamProduced_lb;

        /// <summary>
        /// Current total secondary water mass across all 4 SGs in lb.
        /// Starts at 1,660,000 lb (4 × 415,000 wet layup).
        /// Decreases from: SG draining at ~200°F, steam boiloff in Regime 2.
        /// </summary>
        public float SecondaryWaterMass_lb;

        /// <summary>
        /// Per-node boiling state: true if this node is at T_sat and boiling.
        /// During Subcooled regime, all false. During Boiling, nodes at or
        /// above T_sat(P_secondary) are true.
        /// </summary>
        public bool[] NodeBoiling;

        // ----- Regime Continuity Blend (v5.0.1) -----

        /// <summary>
        /// Per-node regime transition blend factor (0 = subcooled, 1 = boiling).
        /// When a node crosses T_sat, this ramps from 0→1 over ~60 sim-seconds
        /// instead of switching instantaneously. Applied to HTC, effective area,
        /// and driving ΔT to eliminate MW-scale step changes at regime transitions.
        ///
        /// v5.0.1: Eliminates the compound step change where HTC jumps ~10×,
        /// effective area jumps 2–5×, and driving ΔT changes basis simultaneously.
        /// Physical basis: real nucleate boiling onset is gradual (Incropera &
        /// DeWitt Ch. 10 — transition from onset of nucleate boiling through
        /// fully developed regime).
        ///
        /// Source: Implementation Plan v5.0.1, Stage 3
        /// </summary>
        public float[] NodeRegimeBlend;

        // ----- SG Draining & Level Model (v5.0.0 Stage 4) -----

        /// <summary>
        /// True if SG blowdown draining is in progress.
        /// Draining starts when T_rcs reaches SG_DRAINING_START_TEMP_F (~200°F)
        /// and continues until mass reaches SG_DRAINING_TARGET_MASS_FRAC of initial.
        /// Source: NRC HRTD 2.3 / 19.0 — SG startup draining procedures
        /// </summary>
        public bool DrainingActive;

        /// <summary>
        /// True once SG draining has completed (target mass fraction reached).
        /// Draining does not restart once complete.
        /// </summary>
        public bool DrainingComplete;

        /// <summary>
        /// Cumulative mass drained from all 4 SGs via blowdown in lb.
        /// = (Initial mass - post-drain mass). Expected ~747,000 lb total
        /// (1,660,000 × 0.45 = 747,000 lb removed to reach 55% target).
        /// </summary>
        public float TotalMassDrained_lb;

        /// <summary>
        /// Current per-SG draining rate in gpm (0 when not draining).
        /// Nominal: 150 gpm per SG via normal blowdown system.
        /// Source: NRC HRTD 2.3 — 150 gpm normal blowdown rate
        /// </summary>
        public float DrainingRate_gpm;

        /// <summary>
        /// SG Wide Range level in percent (0-100%).
        /// References the full SG secondary volume from tubesheet to steam dome.
        /// 100% WR = wet layup (full of water). During heatup, trends down from
        /// 100% as draining removes water, then continues down from steam boiloff.
        /// Source: Westinghouse FSAR — SG level instrumentation
        /// </summary>
        public float WideRangeLevel_pct;

        /// <summary>
        /// SG Narrow Range level in percent (0-100%).
        /// References the operating range around the feedwater nozzle.
        /// 100% NR ≈ 55% of wet layup mass. 33% NR is normal operating level.
        /// During wet layup (100% WR), NR reads off-scale high (~182%).
        /// During heatup draining, NR transitions from off-scale to ~33% at
        /// the end of draining.
        /// Source: Westinghouse FSAR — SG level instrumentation
        /// </summary>
        public float NarrowRangeLevel_pct;

        /// <summary>
        /// Simulation time (hr) when draining started. 999 if not started.
        /// </summary>
        public float DrainingStartTime_hr;

        // ----- Steam Line Warming Model (v5.1.0 Stage 3) -----

        /// <summary>
        /// Current average temperature of the main steam line piping in °F.
        /// Lumped model: all 4 steam lines treated as a single thermal mass.
        ///
        /// During early boiling, cold steam lines condense steam and absorb
        /// latent heat. As the piping warms toward saturation temperature,
        /// condensation rate drops and the energy sink diminishes.
        ///
        /// Initialized at simulation start temperature (same as RCS).
        /// Evolves via: dT/dt = Q_condensation / (M_metal × Cp)
        /// Capped at T_sat(P_secondary) — cannot exceed steam temperature.
        ///
        /// Source: Implementation Plan v5.1.0 Stage 3
        /// </summary>
        public float SteamLineTempF;

        /// <summary>
        /// Current condensation heat rate from steam to steam line metal
        /// in BTU/hr. Zero when steam lines are at or above T_sat.
        /// Diagnostic field for validation logging.
        /// </summary>
        public float SteamLineCondensationRate_BTUhr;

        // ----- Steam Inventory Model (v5.4.0 Stage 6) -----

        /// <summary>
        /// Current steam mass inventory in the SG secondary in lb (all 4 SGs).
        /// v5.4.0 Stage 6: Tracks accumulated steam for inventory-based pressure.
        /// In normal open-system heatup, this represents the instantaneous steam
        /// mass in the steam space (small, ~steady-state). In isolated SG scenarios,
        /// this accumulates as steam is generated with no outlet.
        ///
        /// Initial value: 0 lb (pre-boiling, no steam space).
        /// During boiling: increases from steam generation, decreases from outflow.
        /// </summary>
        public float SteamInventory_lb;

        /// <summary>
        /// Steam outflow rate from the SG secondary in lb/hr (all 4 SGs).
        /// v5.4.0 Stage 6: Tracks steam exiting through MSIVs/steam dumps.
        ///
        /// In normal open-system heatup: SteamOutflow_lbhr ≈ SteamProductionRate_lbhr
        /// (steam exits as fast as it's produced — quasi-steady-state).
        /// In isolated SG: SteamOutflow_lbhr = 0 (MSIVs closed, no steam dump).
        /// </summary>
        public float SteamOutflow_lbhr;

        /// <summary>
        /// Current steam space volume in ft³ (all 4 SGs).
        /// v5.4.0 Stage 6: Computed from water mass and total SG volume.
        ///
        /// V_steam = V_total - (SecondaryWaterMass_lb / ρ_water)
        /// where V_total is the total SG secondary volume.
        ///
        /// Initial value: 0 ft³ (wet layup, SGs full of water).
        /// During boiling: increases as water boils off.
        /// </summary>
        public float SteamSpaceVolume_ft3;

        /// <summary>
        /// True if the SG is operating in isolated mode (no steam outlet).
        /// v5.4.0 Stage 6: When true, steam accumulates and pressure rises
        /// based on steam inventory rather than saturation tracking.
        ///
        /// Default: false (normal open-system behavior for heatup).
        /// Set to true for MSIV-closed or isolated SG scenarios.
        /// </summary>
        public bool SteamIsolated;

        /// <summary>
        /// Pressure computed from steam inventory in psia (diagnostic).
        /// v5.4.0 Stage 6: Uses ideal gas approximation for steam:
        ///   P = (m_steam × R × T) / V_steam
        /// where R is the specific gas constant for steam.
        ///
        /// This is computed for diagnostics even when SteamIsolated=false.
        /// When SteamIsolated=true, this value is used as SecondaryPressure_psia.
        /// </summary>
        public float InventoryPressure_psia;

        /// <summary>
        /// Nitrogen blanket mass inventory in lb (all 4 SGs).
        /// Used by the minimal compressible gas cushion model.
        /// </summary>
        public float NitrogenGasMass_lb;

        /// <summary>
        /// Nitrogen partial pressure in psia from the cushion model.
        /// </summary>
        public float NitrogenPressure_psia;

        /// <summary>
        /// Current pressure-source branch used for SecondaryPressure_psia.
        /// </summary>
        public SGPressureSourceMode PressureSourceMode;

        // ----- DEPRECATED (retained for API compatibility) -----

        /// <summary>[DEPRECATED v3.0.0] Always 0. Use ThermoclineHeight_ft instead.</summary>
        public float CirculationFraction;

        /// <summary>[DEPRECATED v3.0.0] Always false. Use BoilingActive instead.</summary>
        public bool CirculationActive;
    }

    // ========================================================================
    // RESULT STRUCT
    // Returned by Update() for engine consumption.
    // ========================================================================

    /// <summary>
    /// Result of a single timestep update for the multi-node SG model.
    /// Returned by Update(). Engine reads results; module never mutates
    /// engine state directly.
    /// </summary>
    public struct SGMultiNodeResult
    {
        /// <summary>Total heat removed from RCS by all 4 SGs (MW)</summary>
        public float TotalHeatRemoval_MW;

        /// <summary>Total heat removed from RCS (BTU/hr)</summary>
        public float TotalHeatRemoval_BTUhr;

        /// <summary>Bulk average SG secondary temperature (°F)</summary>
        public float BulkAverageTemp_F;

        /// <summary>Top node temperature (°F)</summary>
        public float TopNodeTemp_F;

        /// <summary>RCS - SG_top ΔT (°F)</summary>
        public float RCS_SG_DeltaT_F;

        /// <summary>Thermocline height from tubesheet (ft)</summary>
        public float ThermoclineHeight_ft;

        /// <summary>Fraction of tube area above thermocline</summary>
        public float ActiveAreaFraction;

        /// <summary>True if boiling active in top node</summary>
        public bool BoilingActive;

        // ----- SG Secondary Pressure Model (v4.3.0) -----

        /// <summary>SG secondary pressure in psia</summary>
        public float SecondaryPressure_psia;

        /// <summary>Saturation temperature at current secondary pressure in °F</summary>
        public float SaturationTemp_F;

        /// <summary>True if nitrogen blanket has been isolated</summary>
        public bool NitrogenIsolated;

        /// <summary>Boiling intensity fraction (0 = subcooled, 1 = full boiling)</summary>
        public float BoilingIntensity;

        /// <summary>True when SG steam boundary is isolated (no steam outflow).</summary>
        public bool SteamIsolated;

        /// <summary>Pressure-source branch used this timestep.</summary>
        public SGPressureSourceMode PressureSourceMode;

        /// <summary>Current steam inventory mass in lb.</summary>
        public float SteamInventory_lb;

        // ----- Three-Regime Model (v5.0.0) -----

        /// <summary>Current thermodynamic regime (Subcooled, Boiling, SteamDump)</summary>
        public SGThermalRegime Regime;

        /// <summary>Instantaneous steam production rate across all 4 SGs (lb/hr)</summary>
        public float SteamProductionRate_lbhr;

        /// <summary>Steam production as equivalent thermal power (MW)</summary>
        public float SteamProductionRate_MW;

        /// <summary>Current total secondary water mass across all 4 SGs (lb)</summary>
        public float SecondaryWaterMass_lb;

        // ----- SG Draining & Level (v5.0.0 Stage 4) -----

        /// <summary>True if SG blowdown draining is in progress</summary>
        public bool DrainingActive;

        /// <summary>True once draining is complete</summary>
        public bool DrainingComplete;

        /// <summary>Per-SG draining rate in gpm (0 when not draining)</summary>
        public float DrainingRate_gpm;

        /// <summary>SG Wide Range level (0-100%)</summary>
        public float WideRangeLevel_pct;

        /// <summary>SG Narrow Range level (0-100%)</summary>
        public float NarrowRangeLevel_pct;

        // ----- DEPRECATED (retained for API compatibility) -----

        /// <summary>[DEPRECATED v3.0.0] Always 0.</summary>
        public float CirculationFraction;

        /// <summary>[DEPRECATED v3.0.0] Always false.</summary>
        public bool CirculationActive;
    }

    // ========================================================================
    // MODULE CLASS
    // ========================================================================

    /// <summary>
    /// Multi-node vertically-stratified SG secondary side thermal model.
    /// Models all 4 SGs as identical (single set of nodes × 4).
    ///
    /// v3.0.0: Thermocline-based stratification model replaces broken
    /// circulation-onset model. See file header for physics basis.
    ///
    /// Called by RCSHeatup.BulkHeatupStep() and HeatupSimEngine.
    /// Returns SGMultiNodeResult.
    /// </summary>
    public static class SGMultiNodeThermal
    {
        // ====================================================================
        // CONSTANTS
        // ====================================================================

        #region Constants

        /// <summary>Conversion: MW to BTU/hr</summary>
        private const float MW_TO_BTU_HR = 3.412e6f;

        /// <summary>
        /// Minimum ΔT for heat transfer calculation (°F).
        /// Below this, heat transfer is negligible.
        /// </summary>
        private const float MIN_DELTA_T = 0.01f;

        /// <summary>
        /// HTC when no RCPs are running in BTU/(hr·ft²·°F).
        /// Both primary and secondary sides are stagnant natural convection.
        /// Series resistance: 1/U = 1/h_primary_nc + 1/h_secondary_nc
        /// h_primary_nc ≈ 10-50, h_secondary_nc ≈ 20-50
        /// U ≈ 7-25 BTU/(hr·ft²·°F)
        /// Using 8 BTU/(hr·ft²·°F) (conservative, near-stagnant).
        ///
        /// Source: Incropera & DeWitt Ch. 9, natural convection in tubes
        /// </summary>
        private const float HTC_NO_RCPS = 8f;

        /// <summary>
        /// Inter-node conduction UA in BTU/(hr·°F) for stagnant conditions.
        /// Represents slow thermal diffusion between adjacent nodes.
        /// In stagnant stratified conditions, mixing is suppressed by the
        /// stable density gradient (Richardson number >> 1).
        /// Only thermal diffusion through water and tube metal contributes.
        ///
        /// Estimate: k_eff × A_cross / Δx ≈ 0.4 × 200 / 5 ≈ 16 BTU/(hr·°F·ft)
        /// Per SG, with 4 inter-node boundaries: ~500 BTU/(hr·°F) total
        ///
        /// Source: Thermal diffusivity analysis, SG_THERMAL_MODEL_RESEARCH_v3.0.0.md
        /// </summary>
        private const float INTERNODE_UA_STAGNANT = 500f;

        /// <summary>
        /// Inter-node mixing UA when boiling is active in BTU/(hr·°F).
        /// Boiling in the upper region creates agitation and local circulation
        /// that enhances mixing with adjacent nodes. Much less than the old
        /// v2.x INTERNODE_UA_CIRCULATING (50,000) which was far too high.
        ///
        /// Source: Engineering estimate — boiling enhances local mixing 10×
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
        /// Nucleate boiling HTC at low pressure (~atmospheric) in BTU/(hr·ft²·°F).
        /// At boiling onset (~220°F, ~0 psig), nucleate boiling HTCs are typically
        /// 2,000–5,000 BTU/(hr·ft²·°F) depending on surface condition and geometry.
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
        /// Nucleate boiling HTC at high pressure (~1100 psia) in BTU/(hr·ft²·°F).
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
        /// Time in hours for a node's regime blend to ramp from 0→1 (60 sim-seconds).
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

        // ====================================================================
        // STATIC TRACKING FIELDS (v5.0.1 — delta clamp bypass detection)
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

        #region Public API

        /// <summary>
        /// Initialize state for cold shutdown (wet layup) conditions.
        /// All nodes start at the same temperature as RCS (thermal equilibrium).
        /// Thermocline starts at top of tube bundle (no active area yet).
        /// Called once at simulation start.
        /// </summary>
        /// <param name="initialTempF">Starting temperature (°F), same as RCS</param>
        /// <returns>Initialized SGMultiNodeState</returns>
        public static SGMultiNodeState Initialize(float initialTempF)
        {
            int N = PlantConstants.SG_NODE_COUNT;
            var state = new SGMultiNodeState();
            state.NodeCount = N;
            state.NodeTemperatures = new float[N];
            state.NodeHeatRates = new float[N];
            state.NodeEffectiveAreaFractions = new float[N];
            state.NodeHTCs = new float[N];

            for (int i = 0; i < N; i++)
            {
                state.NodeTemperatures[i] = initialTempF;
                state.NodeHeatRates[i] = 0f;
                state.NodeEffectiveAreaFractions[i] = 0f;
                state.NodeHTCs[i] = 0f;
            }

            state.TotalHeatAbsorption_MW = 0f;
            state.TotalHeatAbsorption_BTUhr = 0f;
            state.BulkAverageTemp_F = initialTempF;
            state.TopNodeTemp_F = initialTempF;
            state.BottomNodeTemp_F = initialTempF;
            state.StratificationDeltaT_F = 0f;

            // Thermocline starts at top — only U-bend is initially active
            state.ThermoclineHeight_ft = PlantConstants.SG_TUBE_TOTAL_HEIGHT_FT;
            state.ActiveAreaFraction = PlantConstants.SG_UBEND_AREA_FRACTION;
            state.ElapsedHeatupTime_hr = 0f;
            state.BoilingActive = false;

            // SG Secondary Pressure Model (v4.3.0)
            state.SecondaryPressure_psia = PlantConstants.SG_INITIAL_PRESSURE_PSIA;
            state.NitrogenIsolated = false;
            state.SaturationTemp_F = WaterProperties.SaturationTemperature(
                PlantConstants.SG_INITIAL_PRESSURE_PSIA);
            state.MaxSuperheat_F = 0f;

            // Three-Regime Model (v5.0.0)
            state.CurrentRegime = SGThermalRegime.Subcooled;
            state.SteamProductionRate_lbhr = 0f;
            state.SteamProductionRate_MW = 0f;
            state.TotalSteamProduced_lb = 0f;
            state.SecondaryWaterMass_lb = PlantConstants.SG_SECONDARY_WATER_PER_SG_LB
                                        * PlantConstants.SG_COUNT;
            state.NodeBoiling = new bool[N];
            for (int i = 0; i < N; i++)
                state.NodeBoiling[i] = false;

            // v5.0.1: Regime continuity blend (all nodes start fully subcooled)
            state.NodeRegimeBlend = new float[N];
            for (int i = 0; i < N; i++)
                state.NodeRegimeBlend[i] = 0f;

            // Reset static delta clamp tracking
            _prevTotalQ_MW = 0f;
            _prevRCPCount = 0;
            _prevRegime = SGThermalRegime.Subcooled;
            _clampInitialized = false;

            // v5.1.0 Stage 3: Steam line warming model initialization
            state.SteamLineTempF = initialTempF;  // Steam lines at ambient (same as RCS)
            state.SteamLineCondensationRate_BTUhr = 0f;

            // v5.0.0 Stage 4: Draining & Level initialization
            state.DrainingActive = false;
            state.DrainingComplete = false;
            state.TotalMassDrained_lb = 0f;
            state.DrainingRate_gpm = 0f;
            state.DrainingStartTime_hr = 999f;

            // Initial level: 100% WR (wet layup, SGs full of water)
            state.WideRangeLevel_pct = 100f;

            // NR is off-scale high at wet layup: 100% NR ≈ 55% of wet layup
            // mass, so at 100% mass, NR ≈ 100/0.55 = 182%
            state.NarrowRangeLevel_pct = 100f / PlantConstants.SG_DRAINING_TARGET_MASS_FRAC;

            // v5.4.0 Stage 6: Steam inventory model initialization
            state.SteamInventory_lb = 0f;              // No steam pre-boiling
            state.SteamOutflow_lbhr = 0f;              // No outflow pre-boiling
            state.SteamSpaceVolume_ft3 = PlantConstants.SG_MIN_GAS_CUSHION_VOLUME_PER_SG_FT3
                                       * PlantConstants.SG_COUNT;
            state.SteamIsolated = false;              // Default: open system
            state.InventoryPressure_psia = PlantConstants.SG_INITIAL_PRESSURE_PSIA;
            state.NitrogenPressure_psia = PlantConstants.SG_INITIAL_PRESSURE_PSIA;
            state.PressureSourceMode = SGPressureSourceMode.Floor;

            float T_init_R = Mathf.Max(1f, initialTempF + PlantConstants.RANKINE_OFFSET);
            float V_initGas_ft3 = Mathf.Max(1f, state.SteamSpaceVolume_ft3);
            state.NitrogenGasMass_lb = (PlantConstants.SG_INITIAL_PRESSURE_PSIA * V_initGas_ft3)
                / (PlantConstants.SG_N2_GAS_CONSTANT_PSIA_FT3_PER_LB_R * T_init_R);

            // Deprecated fields (API compatibility)
            state.CirculationFraction = 0f;
            state.CirculationActive = false;

            return state;
        }

        /// <summary>
        /// Advance the SG multi-node model by one timestep.
        /// Called every physics step by RCSHeatup or HeatupSimEngine.
        ///
        /// Physics sequence (v5.0.0):
        /// 1. Advance thermocline (if RCPs running)
        /// 2. Update secondary pressure
        /// 3. Determine thermal regime (Subcooled / Boiling / SteamDump)
        /// 4. Stratification state
        /// 5. Calculate per-node HTC and effective area (regime-dependent)
        /// 6. Apply inter-node conduction (stagnant diffusion)
        /// 7. Update node temperatures (regime-dependent energy disposition)
        /// 8. Compute outputs (including steam production for Boiling/SteamDump)
        ///
        /// v5.0.0 Stage 2: Boiling regime implemented. When any node reaches
        /// T_sat, energy goes to latent heat (steam production) instead of
        /// sensible heating. Boiling nodes clamped to T_sat(P_secondary).
        /// v5.1.0: Pressure tracks P_sat(T_hottest) directly (no rate limit).
        ///
        /// v5.0.0 Stage 3: SteamDump regime active. Secondary pressure capped
        /// at steam dump setpoint (1092 psig). Steam dump controller in
        /// HeatupSimEngine.HZP handles valve modulation. Primary temperature
        /// stabilizes at T_sat(1092 psig) = 557°F.
        ///
        /// v5.0.1: Per-node NodeRegimeBlend ramp replaces binary boiling switch.
        /// HTC, effective area, and driving ΔT are blended over ~60 sim-seconds.
        /// Section 8b: Delta clamp limits |ΔQ| to 5 MW/timestep with bypass.
        ///
        /// v5.1.0: Saturation tracking (Stage 1), reversion guard (Stage 2),
        /// steam line condensation energy sink (Stage 3), wall superheat
        /// boiling driver (Stage 4). See IMPLEMENTATION_PLAN_v5.1.0.md.
        /// </summary>
        /// <param name="state">SG state (modified in place)</param>
        /// <param name="T_rcs">Current RCS bulk temperature (°F)</param>
        /// <param name="rcpsRunning">Number of RCPs operating (0-4)</param>
        /// <param name="pressurePsia">System pressure (psia)</param>
        /// <param name="dt_hr">Timestep in hours</param>
        /// <returns>Result struct with total heat removal and diagnostics</returns>
        public static SGMultiNodeResult Update(
            ref SGMultiNodeState state,
            float T_rcs,
            int rcpsRunning,
            float pressurePsia,
            float dt_hr)
        {
            var result = new SGMultiNodeResult();
            int N = state.NodeCount;

            // ================================================================
            // 1. ADVANCE THERMOCLINE
            // ================================================================
            // Thermocline only descends when RCPs are driving flow through tubes
            if (rcpsRunning > 0)
            {
                state.ElapsedHeatupTime_hr += dt_hr;
            }

            // Thermocline position: descends from top via thermal diffusion
            // z_therm = H_total - √(4 × α_eff × t)
            float descent = (float)Math.Sqrt(
                4f * PlantConstants.SG_THERMOCLINE_ALPHA_EFF * state.ElapsedHeatupTime_hr);
            state.ThermoclineHeight_ft = PlantConstants.SG_TUBE_TOTAL_HEIGHT_FT - descent;
            state.ThermoclineHeight_ft = Mathf.Max(state.ThermoclineHeight_ft, 0f);

            // ================================================================
            // 2. UPDATE SECONDARY PRESSURE (v5.1.0: saturation tracking)
            // ================================================================
            // Must be called before boiling check because T_sat depends on
            // the current secondary pressure.
            // v5.1.0: Direct saturation tracking replaces rate-limited model.
            // P_secondary = P_sat(T_hottest_boiling_node) during boiling.
            UpdateSecondaryPressure(ref state, T_rcs, dt_hr);

            // ================================================================
            // 3. DETERMINE THERMAL REGIME (v5.0.0)
            // ================================================================
            // Three-regime detection based on NRC HRTD 19.0:
            //   Subcooled: all nodes below T_sat(P_secondary) — closed system
            //   Boiling:   at least one node at/above T_sat — open system
            //   SteamDump: P_secondary >= steam dump setpoint — heat rejection
            //
            // Stage 2: Boiling regime now has distinct physics path.
            // Steps 5–7 are regime-dependent (subcooled vs boiling).
            // ================================================================

            // Check per-node boiling status
            for (int i = 0; i < N; i++)
            {
                state.NodeBoiling[i] = state.NodeTemperatures[i] >= state.SaturationTemp_F;
            }

            // Determine regime
            float steamDumpPressure_psia = PlantConstants.SG_STEAM_DUMP_SETPOINT_PSIG + 14.7f;
            bool anyNodeBoiling = state.MaxSuperheat_F > 0f;

            if (state.SecondaryPressure_psia >= steamDumpPressure_psia)
            {
                state.CurrentRegime = SGThermalRegime.SteamDump;
            }
            else if (anyNodeBoiling)
            {
                state.CurrentRegime = SGThermalRegime.Boiling;
            }
            else
            {
                state.CurrentRegime = SGThermalRegime.Subcooled;
            }

            // Maintain legacy flag for API compatibility
            state.BoilingActive = anyNodeBoiling;

            // ================================================================
            // 4. STRATIFICATION STATE
            // ================================================================
            state.StratificationDeltaT_F = state.NodeTemperatures[0] - state.NodeTemperatures[N - 1];

            // ================================================================
            // 5. PER-NODE HEAT TRANSFER FROM RCS (regime-dependent)
            // ================================================================
            // v5.0.0 Stage 2: Two distinct physics paths:
            //   SUBCOOLED NODE: Q goes to sensible heat (dT), as before
            //   BOILING NODE:   Q goes to latent heat (steam), T clamped to T_sat
            //
            // v5.1.0 Stage 4: Boiling driving force changed from (T_rcs - T_sat)
            // to (T_wall - T_sat), where T_wall is the tube outer wall temperature
            // estimated algebraically from previous timestep Q. T_wall < T_rcs
            // because the primary-side thermal resistance drops T across the wall.
            // This reduces boiling heat transfer to physically correct levels.
            //
            // The energy computed as Q_i does NOT heat the node — it produces
            // steam at rate m_dot = Q_i / h_fg. The steam EXITS the system.
            // This is the fundamental open-system correction.
            // ================================================================
            float totalQ_BTUhr = 0f;
            float totalQ_boiling_BTUhr = 0f;  // Energy going to steam production
            float totalQ_sensible_BTUhr = 0f; // Energy going to subcooled node heating

            // Total tube area for all 4 SGs
            float totalArea = PlantConstants.SG_HT_AREA_PER_SG_FT2 * PlantConstants.SG_COUNT;

            // Track diagnostics
            float totalActiveAreaFrac = 0f;
            float maxBoilingIntensity = 0f;

            // Latent heat at current secondary pressure (for boiling nodes)
            float h_fg = WaterProperties.LatentHeat(state.SecondaryPressure_psia);
            float T_sat = state.SaturationTemp_F;
            bool inBoilingRegime = (state.CurrentRegime == SGThermalRegime.Boiling
                                 || state.CurrentRegime == SGThermalRegime.SteamDump);
            float boilingStateGuardrail = GetBoilingStateGuardrail(state, inBoilingRegime);

            for (int i = 0; i < N; i++)
            {
                float nodeT = state.NodeTemperatures[i];
                bool nodeIsBoiling = state.NodeBoiling[i] && inBoilingRegime;

                // ============================================================
                // v5.0.1: REGIME CONTINUITY BLEND
                // Instead of instantaneously switching from subcooled to boiling
                // physics when a node crosses T_sat, ramp NodeRegimeBlend[i]
                // from 0→1 over REGIME_BLEND_RAMP_HR (~60 sim-seconds).
                // All three parameters (HTC, area, driving ΔT) are blended
                // using this single factor to ensure smooth transition.
                //
                // Physical basis: nucleate boiling onset is gradual. The tube
                // surface transitions from single-phase natural convection
                // through onset of nucleate boiling to fully developed boiling
                // over a finite timescale (Incropera & DeWitt Ch. 10).
                // ============================================================
                float blend = state.NodeRegimeBlend[i];
                if (nodeIsBoiling)
                {
                    // Ramp blend toward 1.0 (full boiling physics)
                    float rampRate = dt_hr / REGIME_BLEND_RAMP_HR;
                    blend = Mathf.Min(blend + rampRate, 1f);
                }
                else
                {
                    // Node is subcooled — reset blend to 0.0
                    // (instant reset on cooldown; reverse ramp deferred to v5.2.0)
                    blend = 0f;
                }
                state.NodeRegimeBlend[i] = blend;

                // Compute BOTH subcooled and boiling parameters, then blend

                // --- Subcooled parameters ---
                float nodeBoilIntensity_sub = 0f;
                float htc_sub = GetNodeHTC(i, N, nodeT, rcpsRunning,
                    T_sat, out nodeBoilIntensity_sub);
                float area_sub = GetNodeEffectiveAreaFraction(i, N, state.ThermoclineHeight_ft);
                float drivingT_sub = nodeT;

                // --- Boiling parameters ---
                float htc_boil = GetBoilingNodeHTC(rcpsRunning, state.SecondaryPressure_psia)
                    * boilingStateGuardrail;
                float area_boil = PlantConstants.SG_NODE_AREA_FRACTIONS[i];

                // v5.1.0 Stage 4: Wall superheat boiling driver
                // Nucleate boiling is driven by T_wall - T_sat, NOT T_rcs - T_sat.
                // T_wall is algebraically estimated from previous timestep Q:
                //   T_wall = T_rcs - Q_prev / (h_primary × A_node)
                // Using previous timestep Q avoids implicit coupling.
                // Clamped: T_sat ≤ T_wall ≤ T_rcs
                float T_wall_boil;
                float Q_prev_node = state.NodeHeatRates[i];  // Previous timestep (BTU/hr)
                float A_node_outer = area_boil * totalArea * PlantConstants.SG_BUNDLE_PENALTY_FACTOR;
                float h_primary_eff = PlantConstants.SG_PRIMARY_FORCED_CONVECTION_HTC
                                    * Mathf.Min(1f, rcpsRunning / 4f);

                if (h_primary_eff > 1f && A_node_outer > 1f)
                {
                    float T_wall_drop = Q_prev_node / (h_primary_eff * A_node_outer);
                    T_wall_boil = T_rcs - T_wall_drop;
                    // Clamp: T_wall cannot be below T_sat or above T_rcs
                    T_wall_boil = Mathf.Clamp(T_wall_boil, T_sat, T_rcs);
                }
                else
                {
                    // No RCPs or zero area — wall at T_sat (no boiling drive)
                    T_wall_boil = T_sat;
                }

                // The driving temperature for boiling is T_wall, not T_rcs.
                // deltaTNode will be computed as T_rcs - drivingT, so for boiling:
                //   deltaTNode = T_rcs - (T_rcs - (T_wall - T_sat))
                // Equivalently, we set drivingT_boil such that:
                //   T_rcs - drivingT_boil = T_wall - T_sat
                //   drivingT_boil = T_rcs - (T_wall - T_sat)
                float drivingT_boil = T_rcs - (T_wall_boil - T_sat);

                // --- Blended values ---
                float htc = Mathf.Lerp(htc_sub, htc_boil, blend);
                float areaFrac = Mathf.Lerp(area_sub, area_boil, blend);
                float drivingT = Mathf.Lerp(drivingT_sub, drivingT_boil, blend);

                // Boiling intensity: blend between subcooled intensity and 1.0
                float nodeBoilIntensity = Mathf.Lerp(nodeBoilIntensity_sub, 1f, blend);

                float deltaTNode = T_rcs - drivingT;

                if (deltaTNode < MIN_DELTA_T)
                {
                    state.NodeHeatRates[i] = 0f;
                    state.NodeHTCs[i] = 0f;
                    state.NodeEffectiveAreaFractions[i] = 0f;
                    continue;
                }

                state.NodeHTCs[i] = htc;
                if (nodeBoilIntensity > maxBoilingIntensity)
                    maxBoilingIntensity = nodeBoilIntensity;

                state.NodeEffectiveAreaFractions[i] = areaFrac;
                totalActiveAreaFrac += areaFrac;

                // Effective area (ft²) with bundle penalty
                float A_eff = areaFrac * totalArea * PlantConstants.SG_BUNDLE_PENALTY_FACTOR;

                // Heat transfer: Q_i = h_i × A_eff_i × ΔT_i
                float Q_i = htc * A_eff * deltaTNode;
                state.NodeHeatRates[i] = Q_i;
                totalQ_BTUhr += Q_i;

                // v5.0.1: Energy categorization proportional to blend
                // blend=0: all sensible (subcooled), blend=1: all latent (boiling)
                totalQ_boiling_BTUhr += Q_i * blend;
                totalQ_sensible_BTUhr += Q_i * (1f - blend);
            }

            state.ActiveAreaFraction = totalActiveAreaFrac;

            // ================================================================
            // 6. INTER-NODE CONDUCTION (stagnant thermal diffusion)
            // ================================================================
            // In stagnant stratified conditions, only slow thermal diffusion
            // connects adjacent nodes. If boiling is active in the upper node,
            // enhance mixing for nodes adjacent to the boiling zone.
            // Boiling-to-boiling: no conduction needed (both at T_sat).
            float[] mixingHeat = new float[N];
            for (int i = 0; i < N - 1; i++)
            {
                // Skip conduction between two boiling nodes (both at T_sat)
                bool upperBoiling = state.NodeBoiling[i] && inBoilingRegime;
                bool lowerBoiling = state.NodeBoiling[i + 1] && inBoilingRegime;
                if (upperBoiling && lowerBoiling)
                    continue;

                float dT = state.NodeTemperatures[i] - state.NodeTemperatures[i + 1];

                // Enhanced UA at boiling/subcooled interface
                bool boilingAtInterface = (upperBoiling || lowerBoiling);
                float ua = boilingAtInterface ? INTERNODE_UA_BOILING : INTERNODE_UA_STAGNANT;

                // Mix heat per SG × 4 SGs
                float Q_mix = ua * dT * PlantConstants.SG_COUNT;
                mixingHeat[i] -= Q_mix;      // Upper node loses heat
                mixingHeat[i + 1] += Q_mix;  // Lower node gains heat
            }

            // ================================================================
            // 7. UPDATE NODE TEMPERATURES (regime-dependent)
            // ================================================================
            // v5.0.0 Stage 2: Two paths per node:
            //   SUBCOOLED: dT = Q × dt / (m × cp)  [energy heats water]
            //   BOILING:   T = T_sat(P_secondary)   [energy produces steam]
            //
            // v5.0.1: Blended nodes get partial sensible heating. The (1-blend)
            // fraction of a node's heat goes to temperature change, while the
            // blend fraction goes to steam production. When blend=1.0, the node
            // is pure boiling (T clamped to T_sat). When blend=0.0, pure subcooled.
            // During the ~60-second ramp, both paths contribute proportionally.
            // ================================================================
            float waterMassPerSG = PlantConstants.SG_SECONDARY_WATER_PER_SG_LB;
            float metalMassPerSG = PlantConstants.SG_SECONDARY_METAL_PER_SG_LB;

            for (int i = 0; i < N; i++)
            {
                float blend = state.NodeRegimeBlend[i];

                if (blend >= 1f)
                {
                    // FULLY BOILING NODE: temperature clamped to T_sat.
                    // All heat transfer energy goes to steam production.
                    state.NodeTemperatures[i] = T_sat;
                }
                else
                {
                    // SUBCOOLED or TRANSITIONING NODE: sensible heating
                    // Only the (1 - blend) fraction of energy heats the node.
                    // The blend fraction goes to steam production (handled in Section 8).
                    float massFrac = PlantConstants.SG_NODE_MASS_FRACTIONS[i];
                    float nodeWaterMass = massFrac * waterMassPerSG;  // Per SG
                    float nodeMetalMass = massFrac * metalMassPerSG;  // Per SG

                    float cpWater = WaterProperties.WaterSpecificHeat(
                        state.NodeTemperatures[i], pressurePsia);

                    // Heat capacity for this node across all 4 SGs
                    float nodeHeatCap = PlantConstants.SG_COUNT * (
                        nodeWaterMass * cpWater +
                        nodeMetalMass * PlantConstants.STEEL_CP);

                    if (nodeHeatCap < 1f) continue;

                    // Total heat input to this node: primary transfer + inter-node mixing
                    // v5.0.1: Only the sensible fraction contributes to temperature change
                    float totalNodeQ = state.NodeHeatRates[i] + mixingHeat[i];
                    float sensibleQ = totalNodeQ * (1f - blend);

                    // Temperature change
                    float dT_node = sensibleQ * dt_hr / nodeHeatCap;
                    state.NodeTemperatures[i] += dT_node;

                    // v5.0.1: If blending, also nudge temperature toward T_sat
                    // proportional to blend. This prevents a discontinuity when
                    // blend reaches 1.0 and temperature snaps to T_sat.
                    if (blend > 0f)
                    {
                        state.NodeTemperatures[i] = Mathf.Lerp(
                            state.NodeTemperatures[i], T_sat, blend * (dt_hr / REGIME_BLEND_RAMP_HR));
                    }
                }
            }

            // ================================================================
            // 8. COMPUTE OUTPUTS
            // ================================================================
            state.TotalHeatAbsorption_BTUhr = totalQ_BTUhr;
            state.TotalHeatAbsorption_MW = totalQ_BTUhr / MW_TO_BTU_HR;

            // Bulk average (mass-weighted)
            float bulkTemp = 0f;
            float totalMassFrac = 0f;
            for (int i = 0; i < N; i++)
            {
                bulkTemp += state.NodeTemperatures[i] * PlantConstants.SG_NODE_MASS_FRACTIONS[i];
                totalMassFrac += PlantConstants.SG_NODE_MASS_FRACTIONS[i];
            }
            state.BulkAverageTemp_F = bulkTemp / totalMassFrac;

            state.TopNodeTemp_F = state.NodeTemperatures[0];
            state.BottomNodeTemp_F = state.NodeTemperatures[N - 1];

            // ================================================================
            // 8b. DELTA CLAMP (v5.0.1)
            // ================================================================
            // Prevent TotalHeatAbsorption_MW from changing by more than
            // DELTA_Q_CLAMP_MW per timestep. This catches any residual
            // step changes not fully smoothed by the regime blend ramp.
            //
            // Bypass conditions (clamp not applied when):
            //   1. First frame (no previous value to compare)
            //   2. RCP count changed (genuine boundary condition change)
            //   3. Steam dump activation edge (regime just became SteamDump)
            //
            // When the clamp fires, the BTU/hr value is also rescaled to
            // maintain consistency between MW and BTU/hr outputs.
            // ================================================================
            if (_clampInitialized)
            {
                bool bypassClamp = false;

                // Bypass 1: RCP count changed
                if (rcpsRunning != _prevRCPCount)
                    bypassClamp = true;

                // Bypass 2: Steam dump activation edge
                if (state.CurrentRegime == SGThermalRegime.SteamDump &&
                    _prevRegime != SGThermalRegime.SteamDump)
                    bypassClamp = true;

                if (!bypassClamp)
                {
                    float deltaQ = state.TotalHeatAbsorption_MW - _prevTotalQ_MW;
                    if (Mathf.Abs(deltaQ) > DELTA_Q_CLAMP_MW)
                    {
                        float clampedQ = _prevTotalQ_MW + Mathf.Sign(deltaQ) * DELTA_Q_CLAMP_MW;
                        clampedQ = Mathf.Max(clampedQ, 0f);  // Q cannot be negative
                        state.TotalHeatAbsorption_MW = clampedQ;
                        state.TotalHeatAbsorption_BTUhr = clampedQ * MW_TO_BTU_HR;
                        // Rescale totalQ_boiling_BTUhr proportionally
                        if (totalQ_BTUhr > 0f)
                        {
                            float scale = state.TotalHeatAbsorption_BTUhr / totalQ_BTUhr;
                            totalQ_boiling_BTUhr *= scale;
                        }
                    }
                }
            }

            // Update tracking for next timestep
            _prevTotalQ_MW = state.TotalHeatAbsorption_MW;
            _prevRCPCount = rcpsRunning;
            _prevRegime = state.CurrentRegime;
            _clampInitialized = true;

            // ================================================================
            // 8c. STEAM LINE WARMING / CONDENSATION ENERGY SINK (v5.1.0 Stage 3)
            // ================================================================
            // During boiling, steam flows into cold main steam line piping.
            // Film condensation on the cold metal absorbs latent heat, warming
            // the piping toward T_sat. This energy is subtracted from the
            // boiling energy budget BEFORE computing steam production.
            //
            // Q_condensation = UA × (T_sat - T_steamLine)
            // dT_steamLine/dt = Q_condensation / (M_metal × Cp)
            // T_steamLine capped at T_sat (cannot exceed steam temperature)
            //
            // Effect: Early boiling → cold lines absorb significant energy →
            //         less net steam production → slower node heating →
            //         slower pressure rise (physical damping).
            //         As lines warm → condensation drops → pure sat tracking.
            //
            // CRITICAL: This model NEVER modifies pressure directly.
            // Pressure is always P_sat(T_hottest). The steam line model
            // only reduces the net boiling energy available for steam.
            //
            // Source: Implementation Plan v5.1.0 Stage 3
            // ================================================================
            if (totalQ_boiling_BTUhr > 0f && inBoilingRegime)
            {
                float T_steam = state.SaturationTemp_F;
                float dT_steamLine = T_steam - state.SteamLineTempF;

                if (dT_steamLine > 0.1f)
                {
                    // Condensation heat transfer: Q = UA × ΔT
                    float Q_condensation = PlantConstants.SG_STEAM_LINE_UA * dT_steamLine;

                    // Cannot remove more energy than is available from boiling
                    Q_condensation = Mathf.Min(Q_condensation, totalQ_boiling_BTUhr * 0.95f);

                    // Warm the steam line metal
                    float steamLineHeatCap = PlantConstants.SG_STEAM_LINE_METAL_MASS_LB
                                           * PlantConstants.SG_STEAM_LINE_CP;
                    float dT_metal = Q_condensation * dt_hr / steamLineHeatCap;
                    state.SteamLineTempF += dT_metal;

                    // Cap at T_sat — steam line cannot exceed steam temperature
                    state.SteamLineTempF = Mathf.Min(state.SteamLineTempF, T_steam);

                    // Subtract condensation energy from boiling budget
                    totalQ_boiling_BTUhr -= Q_condensation;
                    totalQ_boiling_BTUhr = Mathf.Max(totalQ_boiling_BTUhr, 0f);

                    // Store diagnostic
                    state.SteamLineCondensationRate_BTUhr = Q_condensation;
                }
                else
                {
                    // Steam lines at or above T_sat — no condensation
                    state.SteamLineCondensationRate_BTUhr = 0f;
                }
            }
            else
            {
                // Not boiling — no condensation energy sink
                state.SteamLineCondensationRate_BTUhr = 0f;
            }

            // v5.0.0 Stage 2: Steam production from boiling energy
            // totalQ_boiling_BTUhr is the energy that goes to latent heat
            // and EXITS the system as steam via open MSIVs.
            // v5.1.0: Net of steam line condensation (Section 8c above).
            if (totalQ_boiling_BTUhr > 0f && h_fg > 0f)
            {
                state.SteamProductionRate_lbhr = totalQ_boiling_BTUhr / h_fg;
                state.SteamProductionRate_MW = totalQ_boiling_BTUhr / MW_TO_BTU_HR;

                // Accumulate steam produced and reduce secondary water mass
                float steamThisStep_lb = state.SteamProductionRate_lbhr * dt_hr;
                state.TotalSteamProduced_lb += steamThisStep_lb;
                state.SecondaryWaterMass_lb -= steamThisStep_lb;
                state.SecondaryWaterMass_lb = Mathf.Max(state.SecondaryWaterMass_lb, 0f);
            }
            else
            {
                state.SteamProductionRate_lbhr = 0f;
                state.SteamProductionRate_MW = 0f;
            }

            // ================================================================
            // 9. SG DRAINING MODEL (v5.0.0 Stage 4)
            // ================================================================
            // Per NRC HRTD 2.3 / 19.0: SG draining from wet layup (100% WR)
            // to operating level (~33% NR) via normal blowdown system.
            // Draining starts at T_rcs ≈ 200°F and runs at 150 gpm per SG.
            //
            // The draining is triggered externally by the engine (which knows
            // T_rcs). This section only processes the mass removal if draining
            // is active. The engine calls UpdateDraining() to set the flag.
            //
            // Draining reduces secondary mass independently of boiling.
            // Both effects are cumulative on SecondaryWaterMass_lb.
            // ================================================================
            if (state.DrainingActive && !state.DrainingComplete)
            {
                float initialMass = PlantConstants.SG_SECONDARY_WATER_PER_SG_LB
                                  * PlantConstants.SG_COUNT;
                float targetMass = initialMass * PlantConstants.SG_DRAINING_TARGET_MASS_FRAC;

                // Convert gpm to lb/hr: gpm × (1 min/60 sec × 1 hr/60 min) = gph
                // gpm × 60 = gph, then gph × ft³/gal × ρ = lb/hr
                // At ~200°F secondary water: ρ ≈ 60.1 lb/ft³
                float rho_secondary = WaterProperties.WaterDensity(
                    state.BulkAverageTemp_F, state.SecondaryPressure_psia);
                float totalDrainRate_gpm = PlantConstants.SG_DRAINING_RATE_GPM
                                         * PlantConstants.SG_COUNT;
                // gpm × (ft³/7.48052 gal) × (60 min/hr) × ρ (lb/ft³) = lb/hr
                float drainRate_lbhr = totalDrainRate_gpm / 7.48052f * 60f * rho_secondary;
                float drainThisStep_lb = drainRate_lbhr * dt_hr;

                // Don’t drain below target
                float massAfterDrain = state.SecondaryWaterMass_lb - drainThisStep_lb;
                if (massAfterDrain <= targetMass)
                {
                    drainThisStep_lb = state.SecondaryWaterMass_lb - targetMass;
                    state.DrainingComplete = true;
                    state.DrainingActive = false;
                    state.DrainingRate_gpm = 0f;
                }
                else
                {
                    state.DrainingRate_gpm = PlantConstants.SG_DRAINING_RATE_GPM;
                }

                state.SecondaryWaterMass_lb -= drainThisStep_lb;
                state.SecondaryWaterMass_lb = Mathf.Max(state.SecondaryWaterMass_lb, 0f);
                state.TotalMassDrained_lb += drainThisStep_lb;
            }

            // ================================================================
            // 10. SG LEVEL CALCULATION (v5.0.0 Stage 4)
            // ================================================================
            // Wide Range (WR): Linear with mass fraction relative to wet layup.
            //   100% WR = full wet layup (415,000 lb/SG).
            //   0% WR = empty.
            //
            // Narrow Range (NR): References operating band.
            //   100% NR = SG_DRAINING_TARGET_MASS_FRAC of wet layup (55%).
            //   0% NR = tubes uncovered (critical low — not modelled here).
            //   33% NR = normal operating level.
            //   During wet layup (100% WR), NR reads off-scale high (~182%).
            //
            // Note: Real SG level is affected by density (temperature) and
            // two-phase effects during steaming. This first-order model uses
            // mass ratio, which is sufficient for heatup tracking. A more
            // detailed collapsed/indicated level model is deferred to the
            // Screen 5 SG display implementation (Future Features Priority 5).
            //
            // Source: Westinghouse FSAR — SG level instrumentation
            // ================================================================
            {
                float initialMassTotal = PlantConstants.SG_SECONDARY_WATER_PER_SG_LB
                                       * PlantConstants.SG_COUNT;
                float massFraction = (initialMassTotal > 0f)
                    ? state.SecondaryWaterMass_lb / initialMassTotal
                    : 0f;

                // WR: direct mass fraction percentage
                state.WideRangeLevel_pct = massFraction * 100f;

                // NR: 100% NR = SG_DRAINING_TARGET_MASS_FRAC of wet layup
                // massFraction of 0.55 = 100% NR, massFraction of 0 = 0% NR
                float nrFraction = massFraction / PlantConstants.SG_DRAINING_TARGET_MASS_FRAC;
                state.NarrowRangeLevel_pct = nrFraction * 100f;
            }

            // ================================================================
            // 11. STEAM INVENTORY MODEL (v5.4.0 Stage 6)
            // ================================================================
            // Tracks steam mass inventory in the SG secondary for isolated
            // SG scenarios (MSIV closed). In normal open-system operation,
            // steam exits as fast as it's produced (quasi-steady-state).
            // When isolated, steam accumulates and pressure rises based on
            // inventory rather than saturation tracking.
            //
            // Steam space volume: V_steam = V_total - (m_water / ρ_water)
            // Steam inventory: dm/dt = SteamProductionRate - SteamOutflow
            // Inventory pressure: P = (m × R × T) / V (ideal gas)
            //
            // Default: SteamIsolated = false (open system, sat tracking).
            // When SteamIsolated = true, inventory-based pressure is used.
            //
            // Source: Implementation Plan v5.4.0 Stage 6
            // ================================================================
            {
                state.SteamSpaceVolume_ft3 = ComputeGasCushionVolumeFt3(
                    state, state.BulkAverageTemp_F, state.SecondaryPressure_psia);

                // Steam outflow rate depends on isolation state
                if (state.SteamIsolated)
                {
                    // Isolated SG: no steam outlet (MSIVs closed)
                    state.SteamOutflow_lbhr = 0f;
                }
                else
                {
                    // Open system: steam exits as fast as produced
                    state.SteamOutflow_lbhr = state.SteamProductionRate_lbhr;
                }

                // Steam inventory accumulation
                // dm/dt = production - outflow
                float steamNetRate_lbhr = state.SteamProductionRate_lbhr - state.SteamOutflow_lbhr;
                float steamInventoryChange_lb = steamNetRate_lbhr * dt_hr;
                state.SteamInventory_lb += steamInventoryChange_lb;
                state.SteamInventory_lb = Mathf.Max(0f, state.SteamInventory_lb);

                state.InventoryPressure_psia = ComputeInventoryPressurePsia(
                    state,
                    state.SteamSpaceVolume_ft3,
                    state.BulkAverageTemp_F,
                    state.SaturationTemp_F,
                    out float n2Pressure_psia);
                state.NitrogenPressure_psia = n2Pressure_psia;

                // When isolated, use inventory-based pressure instead of sat tracking
                // Note: This overrides the pressure set in UpdateSecondaryPressure()
                // when SteamIsolated = true. Normal open heatup uses sat tracking.
                if (state.SteamIsolated)
                {
                    state.SecondaryPressure_psia = state.InventoryPressure_psia;
                    state.SaturationTemp_F = WaterProperties.SaturationTemperature(
                        state.SecondaryPressure_psia);
                    state.PressureSourceMode = SGPressureSourceMode.InventoryDerived;
                }
            }

            // Deprecated fields (API compatibility)
            state.CirculationFraction = 0f;
            state.CirculationActive = false;

            // Result struct
            result.TotalHeatRemoval_MW = state.TotalHeatAbsorption_MW;
            result.TotalHeatRemoval_BTUhr = state.TotalHeatAbsorption_BTUhr;
            result.BulkAverageTemp_F = state.BulkAverageTemp_F;
            result.TopNodeTemp_F = state.TopNodeTemp_F;
            result.RCS_SG_DeltaT_F = T_rcs - state.TopNodeTemp_F;
            result.ThermoclineHeight_ft = state.ThermoclineHeight_ft;
            result.ActiveAreaFraction = state.ActiveAreaFraction;
            result.BoilingActive = state.BoilingActive;

            // SG Secondary Pressure Model (v4.3.0)
            result.SecondaryPressure_psia = state.SecondaryPressure_psia;
            result.SaturationTemp_F = state.SaturationTemp_F;
            result.NitrogenIsolated = state.NitrogenIsolated;
            result.BoilingIntensity = maxBoilingIntensity;
            result.SteamIsolated = state.SteamIsolated;
            result.PressureSourceMode = state.PressureSourceMode;
            result.SteamInventory_lb = state.SteamInventory_lb;

            // Three-Regime Model (v5.0.0)
            result.Regime = state.CurrentRegime;
            result.SteamProductionRate_lbhr = state.SteamProductionRate_lbhr;
            result.SteamProductionRate_MW = state.SteamProductionRate_MW;
            result.SecondaryWaterMass_lb = state.SecondaryWaterMass_lb;

            // v5.0.0 Stage 4: Draining & Level
            result.DrainingActive = state.DrainingActive;
            result.DrainingComplete = state.DrainingComplete;
            result.DrainingRate_gpm = state.DrainingRate_gpm;
            result.WideRangeLevel_pct = state.WideRangeLevel_pct;
            result.NarrowRangeLevel_pct = state.NarrowRangeLevel_pct;

            // Deprecated (API compatibility)
            result.CirculationFraction = 0f;
            result.CirculationActive = false;

            return result;
        }

        /// <summary>
        /// Start SG draining from wet layup to operating level.
        /// Called by HeatupSimEngine when T_rcs reaches SG_DRAINING_START_TEMP_F.
        /// Draining proceeds automatically until target mass fraction is reached.
        ///
        /// Per NRC HRTD 2.3 / 19.0: "At approximately 200°F, SG draining is
        /// commenced to reduce level from wet layup (100% WR) to approximately
        /// 33% narrow range (operating level)."
        ///
        /// Source: NRC HRTD ML11223A342 Section 19.2.2
        /// </summary>
        /// <param name="state">SG state (modified in place)</param>
        /// <param name="simTime_hr">Current simulation time in hours</param>
        public static void StartDraining(ref SGMultiNodeState state, float simTime_hr)
        {
            if (state.DrainingComplete || state.DrainingActive)
                return;  // Already draining or already done

            state.DrainingActive = true;
            state.DrainingStartTime_hr = simTime_hr;
            state.DrainingRate_gpm = PlantConstants.SG_DRAINING_RATE_GPM;
        }

        /// <summary>
        /// Set the SG isolation state for steam inventory tracking.
        /// v5.4.0 Stage 6: When isolated (MSIVs closed), steam accumulates
        /// and pressure rises based on inventory rather than saturation tracking.
        ///
        /// Default: false (normal open-system behavior for heatup).
        /// Set to true for MSIV-closed or isolated SG scenarios.
        ///
        /// Source: Implementation Plan v5.4.0 Stage 6
        /// </summary>
        /// <param name="state">SG state (modified in place)</param>
        /// <param name="isolated">True to isolate SG (MSIV closed), false for open system</param>
        public static void SetSteamIsolation(ref SGMultiNodeState state, bool isolated)
        {
            state.SteamIsolated = isolated;

            // If transitioning from isolated to open, reset steam inventory
            // (steam "vents" when MSIVs open)
            if (!isolated)
            {
                state.SteamInventory_lb = 0f;
            }
        }

        /// <summary>
        /// Get a summary string for logging/diagnostics.
        /// </summary>
        public static string GetDiagnosticString(SGMultiNodeState state, float T_rcs)
        {
            int N = state.NodeCount;
            string regimeStr = state.CurrentRegime.ToString().ToUpper();
            string n2Str = state.NitrogenIsolated ? "N\u2082 ISOLATED" : "N\u2082 BLANKETED";
            float P_psig = state.SecondaryPressure_psia - 14.7f;
            string pressureSource = GetPressureSourceLabel(state.PressureSourceMode);
            string drainStr = state.DrainingActive ? $"DRAINING {state.DrainingRate_gpm:F0}gpm/SG" :
                              state.DrainingComplete ? "DRAIN COMPLETE" : "PRE-DRAIN";
            // v5.1.0 Stage 2: Compute saturation coupling diagnostics
            float T_hottest_diag = state.NodeTemperatures[0];
            for (int i = 1; i < N; i++)
            {
                if (state.NodeTemperatures[i] > T_hottest_diag)
                    T_hottest_diag = state.NodeTemperatures[i];
            }
            float P_sat_hottest_diag = WaterProperties.SaturationPressure(T_hottest_diag);
            float dT_driving_diag = T_rcs - state.SaturationTemp_F;

            // v5.4.0 Stage 6: Steam inventory diagnostics
            string isolatedStr = state.SteamIsolated ? "ISOLATED" : "OPEN";
            float P_inv_psig = state.InventoryPressure_psia - 14.7f;

            string s = $"SG MultiNode [{N} nodes] | Regime={regimeStr} | " +
                       $"Q_total={state.TotalHeatAbsorption_MW:F2} MW | " +
                       $"Thermocline={state.ThermoclineHeight_ft:F1} ft | " +
                       $"ActiveArea={state.ActiveAreaFraction:P1} | " +
                       $"t_heat={state.ElapsedHeatupTime_hr:F2} hr\n" +
                       $"  P_sec={P_psig:F0} psig | T_sat={state.SaturationTemp_F:F1}\u00b0F | " +
                       $"Superheat={state.MaxSuperheat_F:F1}\u00b0F | {n2Str}\n" +
                       $"  v5.1.0: T_hottest={T_hottest_diag:F1}\u00b0F | P_sat(T_hot)={P_sat_hottest_diag - 14.7f:F0} psig | " +
                       $"\u0394T_driving(T_rcs-T_sat)={dT_driving_diag:F1}\u00b0F\n" +
                       $"  Wall: T_wall={ComputeDiagWallTemp(state, T_rcs):F1}\u00b0F | " +
                       $"\u0394T_wall(Tw-Tsat)={ComputeDiagWallTemp(state, T_rcs) - state.SaturationTemp_F:F1}\u00b0F\n" +
                       $"  SteamLine: T={state.SteamLineTempF:F1}\u00b0F | Q_cond={state.SteamLineCondensationRate_BTUhr / 3.412e6f:F2} MW\n" +
                       $"  Steam: {state.SteamProductionRate_lbhr:F0} lb/hr ({state.SteamProductionRate_MW:F2} MW) | " +
                       $"Cumulative: {state.TotalSteamProduced_lb:F0} lb | " +
                       $"Sec.Mass: {state.SecondaryWaterMass_lb:F0} lb\n" +
                       $"  v5.4.0: Inventory={state.SteamInventory_lb:F0} lb | Outflow={state.SteamOutflow_lbhr:F0} lb/hr | " +
                       $"V_steam={state.SteamSpaceVolume_ft3:F0} ft\u00b3 | P_inv={P_inv_psig:F0} psig | {isolatedStr} | Psrc={pressureSource}\n" +
                       $"  Level: WR={state.WideRangeLevel_pct:F1}% NR={state.NarrowRangeLevel_pct:F1}% | " +
                       $"{drainStr} | Drained: {state.TotalMassDrained_lb:F0} lb\n";
            for (int i = 0; i < N; i++)
            {
                string label = i == 0 ? "TOP" : (i == N - 1 ? "BOT" : $"N{i}");
                float dT = T_rcs - state.NodeTemperatures[i];
                float nodeBot = PlantConstants.SG_TUBE_TOTAL_HEIGHT_FT - (i + 1) * NODE_HEIGHT_FT;
                float nodeTop = PlantConstants.SG_TUBE_TOTAL_HEIGHT_FT - i * NODE_HEIGHT_FT;
                string posStr = state.ThermoclineHeight_ft < nodeBot ? "BELOW" :
                               (state.ThermoclineHeight_ft > nodeTop ? "ABOVE" : "TRANS");
                string boilMark = (state.NodeBoiling != null && state.NodeBoiling[i]) ? " BOIL" : "";
                // v5.0.1: Include blend value in diagnostics
                string blendStr = (state.NodeRegimeBlend != null && state.NodeRegimeBlend.Length > i)
                    ? $"  B={state.NodeRegimeBlend[i]:F2}" : "";
                s += $"  {label}: T={state.NodeTemperatures[i]:F1}°F  ΔT={dT:F1}°F  " +
                     $"Q={state.NodeHeatRates[i] / MW_TO_BTU_HR:F3}MW  " +
                     $"h={state.NodeHTCs[i]:F0}  Af={state.NodeEffectiveAreaFractions[i]:F3}  " +
                     $"[{posStr}]{boilMark}{blendStr}\n";
            }
            return s;
        }

        #endregion

        // ====================================================================
        // PRIVATE METHODS
        // ====================================================================

        #region Private Methods

        /// <summary>
        /// Get the effective HTC for a specific node based on position,
        /// temperature, boiling state, and RCP status.
        ///
        /// v3.0.0: No longer depends on "circulation fraction". The secondary
        /// side HTC is either stagnant natural convection or boiling-enhanced.
        ///
        /// v4.3.0: Boiling enhancement now uses superheat-based smoothstep
        /// relative to the dynamic saturation temperature T_sat(P_secondary),
        /// rather than a step function at a fixed 220°F threshold. This
        /// prevents the cliff behavior where HTC jumps 5× in one timestep.
        ///
        /// With RCPs running:
        ///   1/U = 1/h_primary + R_wall + 1/h_secondary
        ///   h_primary ≈ 800-3000 BTU/(hr·ft²·°F) (forced convection inside tubes)
        ///   R_wall ≈ negligible (thin Inconel, high k)
        ///   h_secondary ≈ 30-60 BTU/(hr·ft²·°F) (stagnant NC with bundle penalty)
        ///   h_secondary ≈ 150-300 if boiling (nucleate boiling enhancement)
        ///   → U is controlled by h_secondary
        ///
        /// Without RCPs:
        ///   h_primary drops to ~10-50 (natural convection inside tubes too)
        ///   → U ≈ 7-25 BTU/(hr·ft²·°F) — negligible heat transfer
        /// </summary>
        /// <param name="nodeIndex">Node index (0 = top)</param>
        /// <param name="nodeCount">Total number of nodes</param>
        /// <param name="nodeTemp_F">Node secondary temperature (°F)</param>
        /// <param name="rcpsRunning">Number of RCPs operating (0-4)</param>
        /// <param name="Tsat_F">Saturation temperature at current SG secondary pressure (°F)</param>
        /// <param name="boilingIntensity">Output: boiling intensity fraction for this node (0-1)</param>
        private static float GetNodeHTC(int nodeIndex, int nodeCount,
            float nodeTemp_F, int rcpsRunning, float Tsat_F, out float boilingIntensity)
        {
            boilingIntensity = 0f;

            // No RCPs → negligible heat transfer (both sides natural convection)
            if (rcpsRunning == 0)
                return HTC_NO_RCPS;

            // Secondary-side HTC: stagnant natural convection on tube exteriors
            float h_secondary = PlantConstants.SG_MULTINODE_HTC_STAGNANT;

            // Temperature effect: at low temperatures, fluid properties
            // reduce natural convection effectiveness (higher viscosity, lower β)
            float tempFactor = GetTemperatureEfficiencyFactor(nodeTemp_F);
            h_secondary *= tempFactor;

            // v4.3.0: Superheat-based boiling enhancement
            // Instead of a step function at a fixed temperature, use a smoothstep
            // ramp based on superheat above the dynamic T_sat(P_secondary).
            // This naturally self-limits as secondary pressure rises with temperature.
            boilingIntensity = GetBoilingIntensityFraction(nodeTemp_F, Tsat_F);
            if (boilingIntensity > 0f)
            {
                // Ramp HTC multiplier from 1.0 to SG_BOILING_HTC_MULTIPLIER
                float htcMultiplier = 1f + boilingIntensity *
                    (PlantConstants.SG_BOILING_HTC_MULTIPLIER - 1f);
                h_secondary *= htcMultiplier;
            }

            // Primary side: forced convection with RCPs — very high h
            // Scale with RCP count (each pump provides ~25% of flow)
            float primaryFactor = Mathf.Min(1f, rcpsRunning / 4f);
            float h_primary = 1000f * primaryFactor + 10f * (1f - primaryFactor);

            // Overall HTC (series resistance)
            float U = 1f / (1f / h_primary + 1f / h_secondary);

            return U;
        }

        /// <summary>
        /// Get the overall HTC for a boiling node in BTU/(hr·ft²·°F).
        ///
        /// v5.0.0: For nodes that are at T_sat and actively boiling, the
        /// secondary-side HTC is dominated by nucleate boiling (h ≈ 2,000–10,000).
        /// The overall U is then limited by the primary-side forced convection:
        ///   1/U = 1/h_primary + 1/h_boiling
        ///
        /// Since h_boiling >> h_primary, U ≈ h_primary.
        /// This means the exact boiling HTC value matters much less than
        /// getting the regime right — the energy transfer is primary-limited.
        ///
        /// HTC increases with pressure (more vigorous nucleation, smaller
        /// departing bubbles, thinner thermal boundary layer). Linear ramp
        /// from SG_BOILING_HTC_LOW_P at atmospheric to SG_BOILING_HTC_HIGH_P
        /// at steam dump pressure.
        ///
        /// Source: Incropera & DeWitt Ch. 10, Rohsenow correlation;
        ///         Implementation Plan v5.0.0 Stage 2 Section 2B
        /// </summary>
        /// <param name="rcpsRunning">Number of RCPs operating (0-4)</param>
        /// <param name="secondaryPressure_psia">Current SG secondary pressure (psia)</param>
        /// <returns>Overall HTC in BTU/(hr·ft²·°F)</returns>
        private static float GetBoilingNodeHTC(int rcpsRunning, float secondaryPressure_psia)
        {
            // No RCPs → no primary-side flow → negligible heat transfer
            // (even though secondary is boiling, no energy delivery from primary)
            if (rcpsRunning == 0)
                return HTC_NO_RCPS;

            // Pressure-dependent boiling HTC: linear interpolation
            // Low P (17 psia): SG_BOILING_HTC_LOW_P (500)
            // High P (1107 psia): SG_BOILING_HTC_HIGH_P (700)
            float P_low = PlantConstants.SG_MIN_STEAMING_PRESSURE_PSIA;
            float P_high = PlantConstants.SG_BOILING_HTC_HIGH_P_REF_PSIA;
            float t = (secondaryPressure_psia - P_low) / (P_high - P_low);
            t = Mathf.Clamp01(t);

            float h_boiling_overall = PlantConstants.SG_BOILING_HTC_LOW_P
                + t * (PlantConstants.SG_BOILING_HTC_HIGH_P - PlantConstants.SG_BOILING_HTC_LOW_P);

            // Scale with RCP count (each pump provides ~25% of flow)
            float primaryFactor = Mathf.Min(1f, rcpsRunning / 4f);
            h_boiling_overall *= primaryFactor;

            // Floor: if no RCPs providing full flow, minimum = HTC_NO_RCPS
            return Mathf.Max(h_boiling_overall, HTC_NO_RCPS);
        }

        /// <summary>
        /// Get the thermocline-based effective area fraction for a node.
        ///
        /// v3.0.0: Replaces the circulation-based effectiveness model.
        ///
        /// Each node occupies a vertical band in the tube bundle. The
        /// thermocline position determines what fraction of each node's
        /// tube area is "active" (above the thermocline in the hot layer).
        ///
        /// Node above thermocline: full geometric area × stagnant effectiveness
        /// Node containing thermocline: linearly interpolated
        /// Node below thermocline: residual only (SG_BELOW_THERMOCLINE_EFF)
        ///
        /// The stagnant effectiveness array (from PlantConstants.SG) is still
        /// used because even above the thermocline, the hot boundary layer
        /// reduces the effective driving ΔT at different vertical positions.
        /// </summary>
        private static float GetNodeEffectiveAreaFraction(int nodeIndex, int nodeCount,
            float thermoclineHeight_ft)
        {
            // Geometric area fraction for this node
            float geomFrac = PlantConstants.SG_NODE_AREA_FRACTIONS[nodeIndex];

            // Node vertical position (top-to-bottom indexing)
            // Node 0: top of bundle, spans (H - nodeHeight) to H
            // Node N-1: bottom, spans 0 to nodeHeight
            float nodeTopElev = PlantConstants.SG_TUBE_TOTAL_HEIGHT_FT - nodeIndex * NODE_HEIGHT_FT;
            float nodeBotElev = nodeTopElev - NODE_HEIGHT_FT;

            // Transition zone boundaries
            float thermTop = thermoclineHeight_ft + PlantConstants.SG_THERMOCLINE_TRANSITION_FT * 0.5f;
            float thermBot = thermoclineHeight_ft - PlantConstants.SG_THERMOCLINE_TRANSITION_FT * 0.5f;

            // Determine what fraction of this node is above the thermocline
            float aboveFraction;

            if (nodeBotElev >= thermTop)
            {
                // Entire node is above thermocline — fully active
                aboveFraction = 1f;
            }
            else if (nodeTopElev <= thermBot)
            {
                // Entire node is below thermocline — residual only
                aboveFraction = 0f;
            }
            else
            {
                // Node straddles the thermocline — linear interpolation
                // Clamp thermocline zone to node boundaries
                float overlapTop = Mathf.Min(nodeTopElev, thermTop);
                float overlapBot = Mathf.Max(nodeBotElev, thermBot);
                float overlapHeight = Mathf.Max(0f, overlapTop - overlapBot);

                // Fraction of node in transition zone
                float transitionFrac = overlapHeight / NODE_HEIGHT_FT;

                // Fraction of node fully above thermocline
                float fullyAbove = Mathf.Max(0f, nodeTopElev - thermTop) / NODE_HEIGHT_FT;
                fullyAbove = Mathf.Min(fullyAbove, 1f);

                // Weighted: fully above gets 1.0, transition gets 0.5, below gets 0
                aboveFraction = fullyAbove + 0.5f * transitionFrac;
                aboveFraction = Mathf.Clamp01(aboveFraction);
            }

            // Stagnant effectiveness for this node position
            float stagnantEff = PlantConstants.SG_NODE_STAGNANT_EFFECTIVENESS[nodeIndex];

            // Blend: above thermocline uses stagnant effectiveness,
            // below thermocline uses residual effectiveness
            float eff = aboveFraction * stagnantEff +
                       (1f - aboveFraction) * PlantConstants.SG_BELOW_THERMOCLINE_EFF;

            return geomFrac * eff;
        }

        /// <summary>
        /// Update SG secondary pressure based on thermal state and regime.
        ///
        /// v5.1.0: Direct saturation tracking replaces v5.0.0 rate-limited model.
        ///
        /// Physics: The secondary pressure depends on the thermodynamic regime:
        ///
        ///   SUBCOOLED (pre-steam): Pressure = N₂ blanket value (17 psia).
        ///     N₂ supply maintains slight positive pressure. No steam production.
        ///
        ///   BOILING (open system): Pressure tracks saturation directly.
        ///     P_secondary = P_sat(T_hottest_boiling_node)
        ///     This is the thermodynamically correct behavior: the steam space
        ///     is in thermal equilibrium with the hottest tube surfaces. As
        ///     boiling raises pressure, T_sat rises, which reduces the boiling
        ///     driving force (T_rcs - T_sat). This self-regulating negative
        ///     feedback prevents runaway heat sink.
        ///
        ///     Physical damping provided by steam line condensation energy sink
        ///     (Section 8c, v5.1.0 Stage 3). Steam line warming absorbs boiling
        ///     energy during early boiling, slowing pressure rise naturally.
        ///
        ///   STEAM DUMP: Pressure capped at steam dump setpoint (1092 psig).
        ///     Steam dumps modulate to hold pressure constant.
        ///
        /// Source: NRC HRTD ML11223A342 Section 19.0 — steam onset at ~220°F,
        ///         NRC HRTD ML11223A294 Section 11.2 — steam dumps at 1092 psig,
        ///         Implementation Plan v5.1.0 Stage 1
        /// </summary>
        /// <param name="state">SG state (modified in place)</param>
        /// <param name="T_rcs">Current RCS bulk temperature (°F)</param>
        /// <param name="dt_hr">Timestep in hours</param>
        private static void UpdateSecondaryPressure(ref SGMultiNodeState state,
            float T_rcs, float dt_hr)
        {
            // Find the hottest secondary node temperature
            float T_hottest = state.NodeTemperatures[0];
            for (int i = 1; i < state.NodeCount; i++)
            {
                if (state.NodeTemperatures[i] > T_hottest)
                    T_hottest = state.NodeTemperatures[i];
            }

            // Check for nitrogen isolation (one-time event at ~220°F RCS)
            if (!state.NitrogenIsolated && T_rcs >= PlantConstants.SG_NITROGEN_ISOLATION_TEMP_F)
            {
                state.NitrogenIsolated = true;
            }

            // Current T_sat at secondary pressure
            float T_sat_current = WaterProperties.SaturationTemperature(state.SecondaryPressure_psia);

            // Compressible gas-cushion pressure from current inventory state.
            float gasVolume_ft3 = ComputeGasCushionVolumeFt3(
                state, state.BulkAverageTemp_F, state.SecondaryPressure_psia);
            state.SteamSpaceVolume_ft3 = gasVolume_ft3;
            state.InventoryPressure_psia = ComputeInventoryPressurePsia(
                state,
                gasVolume_ft3,
                state.BulkAverageTemp_F,
                T_hottest,
                out float n2Pressure_psia);
            state.NitrogenPressure_psia = n2Pressure_psia;

            // Pressure evolution depends on regime
            //
            // v5.1.0 Stage 2: Guard for boiling→subcooled reversion.
            // The condition for pre-steaming requires BOTH:
            //   (a) N₂ is not yet isolated, OR
            //   (b) T_hottest is below T_sat AND current pressure is at or
            //       below the N₂ blanket value.
            //
            // Without guard (b), a transient dip where T_hottest drops just
            // below T_sat would snap pressure from P_sat(T_hottest) back to
            // 17 psia instantly — a potentially large discontinuity mid-heatup.
            //
            // With guard (b), once pressure has risen above the N₂ blanket,
            // the boiling branch continues to track P_sat(T_hottest) downward
            // smoothly. Pressure only returns to the N₂ blanket value when
            // P_sat(T_hottest) naturally falls to that level — which means
            // the secondary has genuinely cooled back to pre-steaming conditions.
            bool presteaming = !state.NitrogenIsolated ||
                               (T_hottest < T_sat_current &&
                                state.SecondaryPressure_psia <= state.InventoryPressure_psia + 0.5f);

            if (presteaming)
            {
                // ============================================================
                // PRE-STEAMING: N₂ blanket pressure
                // ============================================================
                // Either N₂ is still connected (maintains blanket pressure)
                // or all nodes are genuinely subcooled at near-atmospheric
                // conditions (pressure has not risen above the N₂ blanket).
                state.SecondaryPressure_psia = state.InventoryPressure_psia;
                state.PressureSourceMode =
                    (state.SecondaryPressure_psia <= PlantConstants.SG_MIN_STEAMING_PRESSURE_PSIA + 0.1f)
                    ? SGPressureSourceMode.Floor
                    : SGPressureSourceMode.InventoryDerived;
            }
            else
            {
                // ============================================================
                // OPEN-SYSTEM BOILING: Direct saturation tracking (v5.1.0)
                // ============================================================
                // P_secondary = P_sat(T_hottest_boiling_node)
                //
                // The steam space is in thermal equilibrium with the hottest
                // tube surface. Pressure equals saturation pressure at the
                // hottest secondary node temperature. This is thermodynamically
                // exact for a boiling system with steam present.
                //
                // Self-regulating feedback:
                //   Higher T_hottest → higher P_secondary → higher T_sat
                //   → smaller (T_rcs - T_sat) → less boiling heat transfer
                //   → T_hottest rises more slowly
                //
                // No artificial rate limiting. Physical damping provided by
                // steam line condensation energy sink (Section 8c, v5.1.0 Stage 3).
                // ============================================================

                float P_new = WaterProperties.SaturationPressure(T_hottest);

                // Clamp to physical limits:
                //   Lower: cannot go below N₂ blanket / atmospheric
                //   Upper: safety valve ceiling (backup protection)
                float P_min = PlantConstants.SG_MIN_STEAMING_PRESSURE_PSIA;
                float P_safetyValve = PlantConstants.SG_SAFETY_VALVE_SETPOINT_PSIG + 14.7f;
                P_new = Mathf.Clamp(P_new, P_min, P_safetyValve);

                // Cap at steam dump setpoint during normal operation.
                // When SG pressure reaches 1092 psig, steam dumps modulate to
                // hold pressure constant. T_sat(1092 psig) = 557°F = no-load T_avg.
                // The steam dump controller in HeatupSimEngine.HZP handles the
                // actual valve modulation and heat removal calculation.
                float P_steamDump = PlantConstants.SG_STEAM_DUMP_SETPOINT_PSIG + 14.7f;
                if (P_new > P_steamDump)
                {
                    P_new = P_steamDump;
                }

                state.SecondaryPressure_psia = P_new;
                state.PressureSourceMode = SGPressureSourceMode.Saturation;
            }

            if (state.SteamIsolated)
            {
                state.SecondaryPressure_psia = state.InventoryPressure_psia;
                state.PressureSourceMode = SGPressureSourceMode.InventoryDerived;
            }

            // Update derived state
            state.SaturationTemp_F = WaterProperties.SaturationTemperature(
                state.SecondaryPressure_psia);
            state.MaxSuperheat_F = Mathf.Max(0f, T_hottest - state.SaturationTemp_F);
        }

        private static float ComputeGasCushionVolumeFt3(
            SGMultiNodeState state,
            float bulkTemp_F,
            float secondaryPressure_psia)
        {
            float totalVolume_ft3 = PlantConstants.SG_SECONDARY_TOTAL_VOLUME_PER_SG_FT3
                                  * PlantConstants.SG_COUNT;
            float minCushion_ft3 = PlantConstants.SG_MIN_GAS_CUSHION_VOLUME_PER_SG_FT3
                                 * PlantConstants.SG_COUNT;

            float pressureForDensity = Mathf.Max(
                PlantConstants.SG_MIN_STEAMING_PRESSURE_PSIA, secondaryPressure_psia);
            float rhoWater = WaterProperties.WaterDensity(bulkTemp_F, pressureForDensity);
            float waterVolume_ft3 = (rhoWater > 1f)
                ? state.SecondaryWaterMass_lb / rhoWater
                : totalVolume_ft3;

            float inferredGasVolume = totalVolume_ft3 - waterVolume_ft3;
            return Mathf.Clamp(inferredGasVolume, minCushion_ft3, totalVolume_ft3);
        }

        private static float ComputeInventoryPressurePsia(
            SGMultiNodeState state,
            float gasVolume_ft3,
            float gasTemp_F,
            float steamTemp_F,
            out float nitrogenPressure_psia)
        {
            float gasVolume = Mathf.Max(1f, gasVolume_ft3);
            float gasTemp_R = Mathf.Max(1f, gasTemp_F + PlantConstants.RANKINE_OFFSET);
            float steamTemp_R = Mathf.Max(1f, steamTemp_F + PlantConstants.RANKINE_OFFSET);

            nitrogenPressure_psia = 0f;
            if (state.NitrogenGasMass_lb > 0f)
            {
                nitrogenPressure_psia = (state.NitrogenGasMass_lb
                    * PlantConstants.SG_N2_GAS_CONSTANT_PSIA_FT3_PER_LB_R
                    * gasTemp_R) / gasVolume;
            }

            float steamPartialPressure_psia = 0f;
            if (state.SteamInventory_lb > 0.1f)
            {
                steamPartialPressure_psia = (state.SteamInventory_lb
                    * PlantConstants.SG_STEAM_GAS_CONSTANT_PSIA_FT3_PER_LB_R
                    * steamTemp_R) / gasVolume;
            }

            float totalPressure = nitrogenPressure_psia + steamPartialPressure_psia;
            float pMin = PlantConstants.SG_MIN_STEAMING_PRESSURE_PSIA;
            float pMax = PlantConstants.SG_SAFETY_VALVE_SETPOINT_PSIG + 14.7f;
            return Mathf.Clamp(totalPressure, pMin, pMax);
        }

        private static float GetBoilingStateGuardrail(SGMultiNodeState state, bool inBoilingRegime)
        {
            if (!inBoilingRegime)
                return 1f;

            float totalVolume_ft3 = PlantConstants.SG_SECONDARY_TOTAL_VOLUME_PER_SG_FT3
                                  * PlantConstants.SG_COUNT;
            float minCushion_ft3 = PlantConstants.SG_MIN_GAS_CUSHION_VOLUME_PER_SG_FT3
                                 * PlantConstants.SG_COUNT;
            float effectiveGasVolume_ft3 = Mathf.Max(state.SteamSpaceVolume_ft3, minCushion_ft3);
            float gasFraction = Mathf.Clamp01(effectiveGasVolume_ft3 / Mathf.Max(1f, totalVolume_ft3));
            float gasAvailability = Mathf.Lerp(
                0.8f,
                1f,
                Mathf.InverseLerp(minCushion_ft3 / Mathf.Max(1f, totalVolume_ft3), 0.20f, gasFraction));

            float pMin = PlantConstants.SG_MIN_STEAMING_PRESSURE_PSIA;
            float pSteamDump = PlantConstants.SG_STEAM_DUMP_SETPOINT_PSIG + 14.7f;
            float pressureProgress = Mathf.InverseLerp(pMin, pSteamDump, state.SecondaryPressure_psia);
            float pressureFeedback = Mathf.Lerp(0.7f, 1f, pressureProgress);

            return gasAvailability * pressureFeedback;
        }

        private static string GetPressureSourceLabel(SGPressureSourceMode mode)
        {
            switch (mode)
            {
                case SGPressureSourceMode.Floor:
                    return "floor";
                case SGPressureSourceMode.Saturation:
                    return "P_sat";
                default:
                    return "inventory-derived";
            }
        }

        /// <summary>
        /// Calculate boiling intensity fraction for a node based on local
        /// superheat above the dynamic saturation temperature.
        ///
        /// Uses a Hermite smoothstep (3t² - 2t³) to ramp from 0 to 1 over
        /// the superheat range SG_BOILING_SUPERHEAT_RANGE_F (20°F).
        ///
        /// f_boil = smoothstep(ΔT_superheat / SG_BOILING_SUPERHEAT_RANGE_F)
        /// where ΔT_superheat = max(0, T_node - T_sat(P_secondary))
        ///
        /// Returns 0.0 if subcooled (node below T_sat).
        /// Returns 0.5 at 10°F superheat (half of 20°F range).
        /// Returns 1.0 at 20°F+ superheat (fully developed nucleate boiling).
        ///
        /// The key physics insight: as secondary pressure rises with temperature,
        /// T_sat rises too, so the superheat ΔT remains small even as absolute
        /// temperatures increase. This creates the self-limiting feedback that
        /// prevents cliff behavior.
        ///
        /// Source: Incropera & DeWitt Ch. 10 — boiling curve transition,
        ///         onset of nucleate boiling through fully developed regime
        /// </summary>
        /// <param name="nodeTemp_F">Node secondary temperature (°F)</param>
        /// <param name="Tsat_F">Saturation temperature at current SG secondary pressure (°F)</param>
        /// <returns>Boiling intensity fraction (0.0 - 1.0)</returns>
        private static float GetBoilingIntensityFraction(float nodeTemp_F, float Tsat_F)
        {
            float superheat = nodeTemp_F - Tsat_F;
            if (superheat <= 0f)
                return 0f;

            float t = superheat / PlantConstants.SG_BOILING_SUPERHEAT_RANGE_F;
            t = Mathf.Clamp01(t);

            // Hermite smoothstep: 3t² - 2t³
            // Smooth onset and smooth approach to full boiling
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Compute diagnostic wall temperature for the hottest boiling node.
        /// Used by GetDiagnosticString() to report T_wall.
        ///
        /// Uses the same algebraic formula as the main Update() loop:
        ///   T_wall = T_rcs - Q_node_prev / (h_primary × A_node)
        ///   Clamped to [T_sat, T_rcs]
        ///
        /// Returns T_rcs if no nodes are boiling (wall superheat not applicable).
        /// </summary>
        private static float ComputeDiagWallTemp(SGMultiNodeState state, float T_rcs)
        {
            // Find the hottest boiling node
            int hottest = -1;
            float maxQ = 0f;
            for (int i = 0; i < state.NodeCount; i++)
            {
                if (state.NodeBoiling != null && state.NodeBoiling[i] && state.NodeHeatRates[i] > maxQ)
                {
                    maxQ = state.NodeHeatRates[i];
                    hottest = i;
                }
            }
            if (hottest < 0) return T_rcs;  // No boiling nodes

            float totalArea = PlantConstants.SG_HT_AREA_PER_SG_FT2 * PlantConstants.SG_COUNT;
            float area_boil = PlantConstants.SG_NODE_AREA_FRACTIONS[hottest];
            float A_node = area_boil * totalArea * PlantConstants.SG_BUNDLE_PENALTY_FACTOR;
            float h_primary = PlantConstants.SG_PRIMARY_FORCED_CONVECTION_HTC;  // Assumes 4 RCPs

            if (h_primary < 1f || A_node < 1f) return T_rcs;

            float T_wall_drop = maxQ / (h_primary * A_node);
            float T_wall = T_rcs - T_wall_drop;
            return Mathf.Clamp(T_wall, state.SaturationTemp_F, T_rcs);
        }

        /// <summary>
        /// Calculate temperature-dependent efficiency factor for natural convection.
        /// At low temperatures, water viscosity is higher and thermal expansion
        /// coefficient is lower, reducing Rayleigh number and HTC.
        ///
        /// Based on Churchill-Chu correlation property dependence:
        ///   h ~ (gβΔT/να)^(1/4) for laminar, ^(1/3) for turbulent
        ///   At 100°F: ν = 0.739×10^-5 ft²/s, β = 0.00022/°F
        ///   At 300°F: ν = 0.204×10^-5 ft²/s, β = 0.00043/°F
        ///   Ratio of (β/ν²): 300°F is ~16× higher than 100°F
        ///   Ra ratio: ~16, Nu ratio: ~2.5 (1/4 power), h ratio: ~2.5
        ///
        /// Using a smoother scaling: factor ranges from 0.5 at 100°F to 1.0 at 400°F
        /// </summary>
        private static float GetTemperatureEfficiencyFactor(float T_F)
        {
            if (T_F <= 100f) return 0.50f;
            if (T_F >= 400f) return 1.00f;

            // Linear interpolation
            float t = (T_F - 100f) / (400f - 100f);
            return 0.50f + 0.50f * t;
        }

        #endregion

        // ====================================================================
        // VALIDATION
        // ====================================================================

        #region Validation

        /// <summary>
        /// Validate the multi-node SG model produces realistic results.
        /// v3.0.0: Updated tests for thermocline model.
        /// </summary>
        public static bool ValidateModel()
        {
            bool valid = true;

            // Test 1: Initialize at 100°F — all nodes equal, thermocline at top
            var state = Initialize(100f);
            if (state.NodeCount != PlantConstants.SG_NODE_COUNT) valid = false;
            for (int i = 0; i < state.NodeCount; i++)
            {
                if (Math.Abs(state.NodeTemperatures[i] - 100f) > 0.01f) valid = false;
            }
            if (Math.Abs(state.ThermoclineHeight_ft - PlantConstants.SG_TUBE_TOTAL_HEIGHT_FT) > 0.01f)
            {
                Debug.LogWarning("[SGMultiNode Validation] Test 1 FAIL: Thermocline not at top");
                valid = false;
            }

            // Test 2: One step with T_rcs=120°F, 4 RCPs — total Q should be < 2 MW
            // (thermocline model: only ~12% of area active, bundle penalty 0.40)
            var result = Update(ref state, 120f, 4, 400f, 1f / 360f);
            if (result.TotalHeatRemoval_MW < 0.01f || result.TotalHeatRemoval_MW > 3f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 2 FAIL: Q={result.TotalHeatRemoval_MW:F3} MW (expected 0.01-3.0)");
                valid = false;
            }

            // Test 3: Top node should heat faster than bottom
            if (state.NodeTemperatures[0] <= state.NodeTemperatures[state.NodeCount - 1])
            {
                Debug.LogWarning("[SGMultiNode Validation] Test 3 FAIL: Top not hotter than bottom");
                valid = false;
            }

            // Test 4: Run 100 steps (~17 min) — Q should remain realistic
            state = Initialize(100f);
            for (int step = 0; step < 100; step++)
            {
                Update(ref state, 150f, 4, 500f, 1f / 360f);
            }
            // After 100 steps, total Q should be well under 5 MW
            if (state.TotalHeatAbsorption_MW > 5f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 4 FAIL: Q too high: {state.TotalHeatAbsorption_MW:F2} MW (expected < 5)");
                valid = false;
            }
            // Top should be warmer than bottom (stratification)
            if (state.NodeTemperatures[0] <= state.NodeTemperatures[state.NodeCount - 1] + 0.1f)
            {
                Debug.LogWarning("[SGMultiNode Validation] Test 4 FAIL: No stratification after 100 steps");
                valid = false;
            }

            // Test 5: No RCPs → very low heat transfer
            var stateNoRCP = Initialize(100f);
            var resultNoRCP = Update(ref stateNoRCP, 200f, 0, 400f, 1f / 360f);
            if (resultNoRCP.TotalHeatRemoval_MW > 0.5f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 5 FAIL: No-RCP Q too high: {resultNoRCP.TotalHeatRemoval_MW:F3} MW");
                valid = false;
            }

            // Test 6: Area fractions sum to 1.0
            float areaSum = 0f;
            for (int i = 0; i < PlantConstants.SG_NODE_AREA_FRACTIONS.Length; i++)
                areaSum += PlantConstants.SG_NODE_AREA_FRACTIONS[i];
            if (Math.Abs(areaSum - 1.0f) > 0.01f)
            {
                Debug.LogWarning("[SGMultiNode Validation] Test 6 FAIL: Area fractions don't sum to 1.0");
                valid = false;
            }

            // Test 7: Mass fractions sum to 1.0
            float massSum = 0f;
            for (int i = 0; i < PlantConstants.SG_NODE_MASS_FRACTIONS.Length; i++)
                massSum += PlantConstants.SG_NODE_MASS_FRACTIONS[i];
            if (Math.Abs(massSum - 1.0f) > 0.01f)
            {
                Debug.LogWarning("[SGMultiNode Validation] Test 7 FAIL: Mass fractions don't sum to 1.0");
                valid = false;
            }

            // Test 8: Thermocline descends over time
            state = Initialize(100f);
            float initialThermocline = state.ThermoclineHeight_ft;
            for (int step = 0; step < 360; step++)  // 1 hour at 10-sec steps
            {
                Update(ref state, 150f, 4, 500f, 1f / 360f);
            }
            if (state.ThermoclineHeight_ft >= initialThermocline)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 8 FAIL: Thermocline did not descend " +
                                $"(initial={initialThermocline:F2}, after 1hr={state.ThermoclineHeight_ft:F2})");
                valid = false;
            }
            // After 1 hour: descent ≈ √(4 × 0.08 × 1) ≈ 0.57 ft
            float expectedDescent = (float)Math.Sqrt(4f * PlantConstants.SG_THERMOCLINE_ALPHA_EFF * 1f);
            float actualDescent = initialThermocline - state.ThermoclineHeight_ft;
            if (Math.Abs(actualDescent - expectedDescent) > 0.1f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 8 WARN: Descent={actualDescent:F2} ft " +
                                $"(expected ~{expectedDescent:F2} ft)");
                // Warning only, not a failure
            }

            // Test 9: Boiling onset detection
            state = Initialize(100f);
            state.NodeTemperatures[0] = 225f;  // Force top node above boiling onset
            Update(ref state, 250f, 4, 400f, 1f / 360f);
            if (!state.BoilingActive)
            {
                Debug.LogWarning("[SGMultiNode Validation] Test 9 FAIL: Boiling not detected at 225°F top node");
                valid = false;
            }

            // Test 10: Long-term energy absorption check (simulate 2 hours)
            // With 4 RCPs at ~150°F, thermocline model should keep Q < 3 MW average
            state = Initialize(100f);
            float totalEnergy_BTU = 0f;
            int totalSteps = 720;  // 2 hours at 10-sec steps
            for (int step = 0; step < totalSteps; step++)
            {
                var r = Update(ref state, 150f, 4, 500f, 1f / 360f);
                totalEnergy_BTU += r.TotalHeatRemoval_BTUhr * (1f / 360f);
            }
            float avgQ_MW = (totalEnergy_BTU / 2f) / MW_TO_BTU_HR;  // Average over 2 hours
            if (avgQ_MW > 3f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 10 FAIL: 2-hr avg Q={avgQ_MW:F2} MW (expected < 3 MW)");
                valid = false;
            }

            // Test 11: v5.0.0 regime detection — starts Subcooled
            state = Initialize(100f);
            if (state.CurrentRegime != SGThermalRegime.Subcooled)
            {
                Debug.LogWarning("[SGMultiNode Validation] Test 11 FAIL: Initial regime not Subcooled");
                valid = false;
            }
            if (state.SecondaryWaterMass_lb < 1600000f || state.SecondaryWaterMass_lb > 1700000f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 11 FAIL: Initial sec mass={state.SecondaryWaterMass_lb:F0} lb (expected ~1,660,000)");
                valid = false;
            }
            if (state.NodeBoiling == null || state.NodeBoiling.Length != PlantConstants.SG_NODE_COUNT)
            {
                Debug.LogWarning("[SGMultiNode Validation] Test 11 FAIL: NodeBoiling array not initialised");
                valid = false;
            }

            // Test 12: v5.0.0 regime transitions to Boiling when top node >= T_sat
            state = Initialize(100f);
            state.NodeTemperatures[0] = 225f;  // Above T_sat at 17 psia (~220°F)
            Update(ref state, 250f, 4, 400f, 1f / 360f);
            if (state.CurrentRegime != SGThermalRegime.Boiling)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 12 FAIL: Regime={state.CurrentRegime} (expected Boiling)");
                valid = false;
            }
            if (!state.NodeBoiling[0])
            {
                Debug.LogWarning("[SGMultiNode Validation] Test 12 FAIL: Top node not marked as boiling");
                valid = false;
            }

            // Test 13: v5.0.0 Stage 2 — boiling node produces steam (rate > 0)
            // Test 12 already put us in boiling regime with top node at ~T_sat
            if (state.SteamProductionRate_lbhr <= 0f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 13 FAIL: Steam rate={state.SteamProductionRate_lbhr:F0} lb/hr (expected > 0 in boiling regime)");
                valid = false;
            }

            // Test 14: v5.0.0 Stage 2 — boiling node temperature clamped to T_sat
            // The top node was set to 225°F (above T_sat ~220°F at 17 psia).
            // After Update(), it should be clamped to T_sat, NOT allowed to rise above.
            float expectedTsat = WaterProperties.SaturationTemperature(state.SecondaryPressure_psia);
            if (Math.Abs(state.NodeTemperatures[0] - expectedTsat) > 2f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 14 FAIL: Boiling node T={state.NodeTemperatures[0]:F1}°F " +
                                $"(expected T_sat={expectedTsat:F1}°F)");
                valid = false;
            }

            // Test 15: v5.0.0 Stage 2 — secondary water mass decreases during boiling
            float initialMass = PlantConstants.SG_SECONDARY_WATER_PER_SG_LB * PlantConstants.SG_COUNT;
            if (state.SecondaryWaterMass_lb >= initialMass)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 15 FAIL: Sec mass={state.SecondaryWaterMass_lb:F0} " +
                                $"(should be < initial {initialMass:F0} after steam production)");
                valid = false;
            }

            // Test 16: v5.1.0 — pressure tracks saturation directly (no rate limit)
            // Initialize at 100°F, force top node to 350°F (P_sat ≈ 135 psia).
            // After one timestep, pressure should jump directly to P_sat(350°F)
            // because saturation tracking is now instantaneous (v5.1.0 Stage 1).
            state = Initialize(100f);
            state.NitrogenIsolated = true;  // Force N₂ isolated
            state.NodeTemperatures[0] = 350f;
            Update(ref state, 400f, 4, 500f, 1f / 360f);
            float P_sat_350 = WaterProperties.SaturationPressure(350f);
            if (Math.Abs(state.SecondaryPressure_psia - P_sat_350) > 5f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 16 FAIL: Pressure={state.SecondaryPressure_psia:F1} psia " +
                                $"(expected P_sat(350°F)={P_sat_350:F1} psia — saturation tracking)");
                valid = false;
            }

            // Test 17: v5.0.0 Stage 2 — energy balance in boiling regime
            // Boiling node energy should go to steam, not sensible heat.
            // Run 100 steps with top node at boiling. Track steam produced.
            state = Initialize(200f);
            state.NitrogenIsolated = true;
            state.NodeTemperatures[0] = 225f;  // Above T_sat at ~17 psia
            float totalSteamBefore = state.TotalSteamProduced_lb;
            for (int step = 0; step < 100; step++)
            {
                Update(ref state, 300f, 4, 500f, 1f / 360f);
            }
            float steamProduced = state.TotalSteamProduced_lb - totalSteamBefore;
            if (steamProduced <= 0f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 17 FAIL: No steam produced over 100 steps in boiling regime");
                valid = false;
            }
            // Top node should still be at T_sat, not rising freely
            float currentTsat = WaterProperties.SaturationTemperature(state.SecondaryPressure_psia);
            if (state.NodeTemperatures[0] > currentTsat + 5f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 17 FAIL: Top node {state.NodeTemperatures[0]:F1}°F >> T_sat {currentTsat:F1}°F");
                valid = false;
            }

            // Test 18: v5.0.0 Stage 3 — pressure caps at steam dump setpoint
            // Run many steps with high T_rcs to let pressure ramp up.
            // Pressure should never exceed 1092 psig (1106.7 psia).
            state = Initialize(200f);
            state.NitrogenIsolated = true;
            for (int i = 0; i < state.NodeCount; i++)
                state.NodeTemperatures[i] = 500f;  // Well above T_sat at any moderate P
            // Run 3600 steps (10 hours at 10-sec steps) to let pressure ramp fully
            for (int step = 0; step < 3600; step++)
            {
                Update(ref state, 580f, 4, 2235f, 1f / 360f);
            }
            float steamDumpP_psia = PlantConstants.SG_STEAM_DUMP_SETPOINT_PSIG + 14.7f;
            if (state.SecondaryPressure_psia > steamDumpP_psia + 1f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 18 FAIL: P_sec={state.SecondaryPressure_psia:F1} psia " +
                                $"exceeds steam dump cap of {steamDumpP_psia:F1} psia");
                valid = false;
            }
            // Should be in SteamDump regime
            if (state.CurrentRegime != SGThermalRegime.SteamDump)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 18 FAIL: Regime={state.CurrentRegime} (expected SteamDump)");
                valid = false;
            }

            // Test 19: v5.0.0 Stage 3 — T_sat at steam dump pressure = ~557°F
            float Tsat_at_steamDump = WaterProperties.SaturationTemperature(steamDumpP_psia);
            if (Math.Abs(Tsat_at_steamDump - 557f) > 5f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 19 FAIL: T_sat at steam dump P = {Tsat_at_steamDump:F1}°F " +
                                $"(expected ~557°F)");
                valid = false;
            }
            // Boiling nodes should be at T_sat = ~557°F
            if (Math.Abs(state.NodeTemperatures[0] - Tsat_at_steamDump) > 3f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 19 FAIL: Top node={state.NodeTemperatures[0]:F1}°F " +
                                $"(expected T_sat={Tsat_at_steamDump:F1}°F)");
                valid = false;
            }

            // Test 20: v5.0.1 — NodeRegimeBlend initializes to zero and ramps gradually
            // Force a node to boiling and verify blend does NOT jump to 1.0 immediately.
            // With dt = 1/360 hr (10 sec) and ramp = 60/3600 hr, one step adds 10/60 = 0.167.
            // Blend reaches 1.0 after 6 steps (60 sim-seconds). Test uses 11 total steps
            // (1 initial + 10 additional) as a generous margin to confirm convergence.
            state = Initialize(100f);
            if (state.NodeRegimeBlend == null || state.NodeRegimeBlend.Length != PlantConstants.SG_NODE_COUNT)
            {
                Debug.LogWarning("[SGMultiNode Validation] Test 20 FAIL: NodeRegimeBlend not initialized");
                valid = false;
            }
            else
            {
                // All blends should start at 0
                for (int i = 0; i < state.NodeCount; i++)
                {
                    if (state.NodeRegimeBlend[i] != 0f)
                    {
                        Debug.LogWarning($"[SGMultiNode Validation] Test 20 FAIL: NodeRegimeBlend[{i}] = {state.NodeRegimeBlend[i]} at init (expected 0)");
                        valid = false;
                    }
                }

                // Force top node to boiling and run one step
                state.NodeTemperatures[0] = 225f;
                Update(ref state, 250f, 4, 400f, 1f / 360f);
                float expectedBlend = (1f / 360f) / REGIME_BLEND_RAMP_HR;  // ~0.167
                if (state.NodeRegimeBlend[0] < 0.1f || state.NodeRegimeBlend[0] > 0.9f)
                {
                    Debug.LogWarning($"[SGMultiNode Validation] Test 20 FAIL: Blend after 1 step = {state.NodeRegimeBlend[0]:F3} " +
                                    $"(expected ~{expectedBlend:F3}, not 0 or 1)");
                    valid = false;
                }

                // Run 10 more steps (11 total). Blend reaches 1.0 at step 6;
                // steps 7-11 confirm it stays clamped at 1.0.
                for (int step = 0; step < 10; step++)
                {
                    Update(ref state, 250f, 4, 400f, 1f / 360f);
                }
                if (state.NodeRegimeBlend[0] < 0.99f)
                {
                    Debug.LogWarning($"[SGMultiNode Validation] Test 20 FAIL: Blend after 11 steps = {state.NodeRegimeBlend[0]:F3} (expected 1.0; ramp completes at step 6)");
                    valid = false;
                }
            }

            // Test 21: v5.0.1 — Delta clamp prevents >5 MW/step jumps
            // Start with a large instantaneous Q difference (cold SG, hot RCS)
            // and verify the output is clamped.
            state = Initialize(100f);
            // Run a baseline step to initialize the clamp tracker
            Update(ref state, 120f, 4, 400f, 1f / 360f);
            float baselineQ = state.TotalHeatAbsorption_MW;
            // Now force a massive driving ΔT increase by jumping T_rcs
            var result21 = Update(ref state, 500f, 4, 2235f, 1f / 360f);
            float deltaQ21 = Math.Abs(state.TotalHeatAbsorption_MW - baselineQ);
            if (deltaQ21 > DELTA_Q_CLAMP_MW + 0.1f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 21 FAIL: |ΔQ| = {deltaQ21:F2} MW " +
                                $"(expected ≤ {DELTA_Q_CLAMP_MW} MW from delta clamp)");
                valid = false;
            }

            // Test 22: v5.1.0 Stage 2 — boiling→subcooled reversion guard
            // Once pressure has risen above N₂ blanket (boiling active), a transient
            // dip of T_hottest just below T_sat must NOT snap pressure back to 17 psia.
            // Instead, pressure should continue tracking P_sat(T_hottest) downward.
            state = Initialize(100f);
            state.NitrogenIsolated = true;
            state.NodeTemperatures[0] = 350f;  // Force boiling, P_sat(350) ≈ 135 psia
            Update(ref state, 400f, 4, 500f, 1f / 360f);
            float P_after_boiling = state.SecondaryPressure_psia;  // Should be ~135 psia
            // Now cool the hottest node to just below current T_sat
            // T_sat at 135 psia ≈ 350°F, so set to 348°F (just below)
            float T_sat_at_current_P = WaterProperties.SaturationTemperature(P_after_boiling);
            state.NodeTemperatures[0] = T_sat_at_current_P - 2f;
            Update(ref state, 400f, 4, 500f, 1f / 360f);
            // Pressure should track P_sat(348°F) ≈ ~131 psia, NOT snap to 17 psia
            if (state.SecondaryPressure_psia < 50f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 22 FAIL: Pressure snapped to " +
                                $"{state.SecondaryPressure_psia:F1} psia after T_hottest dipped below T_sat " +
                                $"(expected ~P_sat({T_sat_at_current_P - 2f:F0}°F), not 17 psia)");
                valid = false;
            }

            // Test 23: v5.1.0 Stage 3 — steam line warming model
            // During boiling, cold steam lines should absorb condensation energy.
            // After several steps, steam line temperature should rise above initial.
            state = Initialize(100f);
            state.NitrogenIsolated = true;
            state.NodeTemperatures[0] = 300f;  // Force boiling
            float steamLineInitial = state.SteamLineTempF;  // Should be 100°F
            // Run 50 steps to let condensation warm the steam lines
            for (int step = 0; step < 50; step++)
            {
                Update(ref state, 350f, 4, 500f, 1f / 360f);
            }
            if (state.SteamLineTempF <= steamLineInitial + 1f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 23 FAIL: Steam line did not warm " +
                                $"(initial={steamLineInitial:F1}°F, after 50 steps={state.SteamLineTempF:F1}°F)");
                valid = false;
            }
            // Steam line should still be below T_sat (cap enforced)
            float T_sat_23 = state.SaturationTemp_F;
            if (state.SteamLineTempF > T_sat_23 + 1f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 23 FAIL: Steam line T={state.SteamLineTempF:F1}°F " +
                                $"exceeds T_sat={T_sat_23:F1}°F");
                valid = false;
            }

            // Test 24: v5.1.0 Stage 3 — steam line initialization
            state = Initialize(150f);
            if (Math.Abs(state.SteamLineTempF - 150f) > 0.01f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 24 FAIL: Steam line init T={state.SteamLineTempF:F1}°F " +
                                $"(expected 150°F)");
                valid = false;
            }
            if (state.SteamLineCondensationRate_BTUhr != 0f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 24 FAIL: Condensation rate={state.SteamLineCondensationRate_BTUhr:F0} " +
                                $"at init (expected 0)");
                valid = false;
            }

            // Test 25: v5.1.0 Stage 4 — wall temperature bounds during boiling
            // T_wall must satisfy: T_sat ≤ T_wall ≤ T_rcs
            state = Initialize(100f);
            state.NitrogenIsolated = true;
            state.NodeTemperatures[0] = 350f;
            for (int step = 0; step < 5; step++)
            {
                Update(ref state, 400f, 4, 500f, 1f / 360f);
            }
            float T_wall_diag = ComputeDiagWallTemp(state, 400f);
            float T_sat_25 = state.SaturationTemp_F;
            if (T_wall_diag < T_sat_25 - 0.1f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 25 FAIL: T_wall={T_wall_diag:F1}°F " +
                                $"< T_sat={T_sat_25:F1}°F");
                valid = false;
            }
            if (T_wall_diag > 400.1f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 25 FAIL: T_wall={T_wall_diag:F1}°F " +
                                $"> T_rcs=400°F");
                valid = false;
            }
            if (T_wall_diag >= 399.9f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 25 WARN: T_wall={T_wall_diag:F1}°F ≈ T_rcs " +
                                $"(no wall drop detected)");
            }

            // Test 26: v5.1.0 Stage 4 — primary HTC constant sanity
            if (PlantConstants.SG_PRIMARY_FORCED_CONVECTION_HTC <= 0f)
            {
                Debug.LogWarning("[SGMultiNode Validation] Test 26 FAIL: SG_PRIMARY_FORCED_CONVECTION_HTC ≤ 0");
                valid = false;
            }

            if (valid)
                Debug.Log("[SGMultiNode] All validation tests PASSED (v5.1.0 — saturation + steam line + wall superheat)");
            else
                Debug.LogError("[SGMultiNode] Validation FAILED — check warnings above");

            return valid;
        }

        #endregion
    }
}
