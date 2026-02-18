// CRITICAL: Master the Atom - Physics Module
// SGMultiNodeThermal.Types.cs - SG regime and state DTOs
//
// File: Assets/Scripts/Physics/SGMultiNodeThermal.Types.cs
// Module: Critical.Physics.SGMultiNodeThermal
// Responsibility: SG thermal regime enums and state/result data structures.
// Standards: GOLD v1.0, SRP/SOLID
// Version: 1.0
// Last Updated: 2026-02-18
using System;
using UnityEngine;
namespace Critical.Physics
{
    // ========================================================================
    // THERMAL REGIME ENUM (v5.0.0)
    // Identifies the current thermodynamic phase of the SG secondary side.
    // Transitions are one-directional during heatup (Subcooled â†’ Boiling â†’ SteamDump).
    // Reverse transitions handled for cooldown scenarios.
    //
    // Source: NRC HRTD ML11223A342 Section 19.0 â€” three-phase heatup sequence
    // ========================================================================

    /// <summary>
    /// Thermodynamic regime of the SG secondary side during heatup.
    /// Determines whether energy goes to sensible heat (closed system)
    /// or latent heat / steam production (open system).
    /// </summary>
    public enum SGThermalRegime
    {
        /// <summary>
        /// All secondary nodes below T_sat â€” closed stagnant pool.
        /// Energy goes to sensible heating of secondary water.
        /// Thermocline model governs effective area and HTC.
        /// Duration: ~2.4 hours (100â†’220Â°F at 50Â°F/hr).
        /// </summary>
        Subcooled,

        /// <summary>
        /// At least one node at or above T_sat â€” open system.
        /// Energy primarily goes to latent heat of vaporization.
        /// Steam produced at tube surfaces exits via open MSIVs.
        /// Secondary temperature tracks T_sat(P_secondary).
        /// Duration: ~6.7 hours (220â†’557Â°F at 50Â°F/hr).
        /// </summary>
        Boiling,

        /// <summary>
        /// P_secondary â‰¥ steam dump setpoint (1092 psig).
        /// Steam dumps modulate to hold pressure constant.
        /// All excess RCP heat dumped to condenser as steam.
        /// T_rcs stabilizes at T_sat(1092 psig) = 557Â°F.
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
        /// <summary>Per-node temperatures (Â°F), index 0 = top, N-1 = bottom</summary>
        public float[] NodeTemperatures;

        /// <summary>Per-node heat transfer rate (BTU/hr), last computed</summary>
        public float[] NodeHeatRates;

        /// <summary>Per-node effective area fraction, last computed</summary>
        public float[] NodeEffectiveAreaFractions;

        /// <summary>Per-node HTC (BTU/(hrÂ·ftÂ²Â·Â°F)), last computed</summary>
        public float[] NodeHTCs;

        /// <summary>Total heat absorption across all 4 SGs (MW)</summary>
        public float TotalHeatAbsorption_MW;

        /// <summary>Total heat absorption (BTU/hr)</summary>
        public float TotalHeatAbsorption_BTUhr;

        /// <summary>Bulk average secondary temperature (Â°F) â€” weighted by mass</summary>
        public float BulkAverageTemp_F;

        /// <summary>Top node temperature (Â°F) â€” hottest node</summary>
        public float TopNodeTemp_F;

        /// <summary>Bottom node temperature (Â°F) â€” coldest node</summary>
        public float BottomNodeTemp_F;

        /// <summary>Î”T between top and bottom nodes (Â°F)</summary>
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

        /// <summary>Current saturation temperature at secondary pressure in Â°F</summary>
        public float SaturationTemp_F;

        /// <summary>Superheat of hottest node above T_sat in Â°F (0 if subcooled)</summary>
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
        /// = SteamProductionRate_lbhr Ã— h_fg / 3.412e6
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
        /// Starts at 1,660,000 lb (4 Ã— 415,000 wet layup).
        /// Decreases from: SG draining at ~200Â°F, steam boiloff in Regime 2.
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
        /// When a node crosses T_sat, this ramps from 0â†’1 over ~60 sim-seconds
        /// instead of switching instantaneously. Applied to HTC, effective area,
        /// and driving Î”T to eliminate MW-scale step changes at regime transitions.
        ///
        /// v5.0.1: Eliminates the compound step change where HTC jumps ~10Ã—,
        /// effective area jumps 2â€“5Ã—, and driving Î”T changes basis simultaneously.
        /// Physical basis: real nucleate boiling onset is gradual (Incropera &
        /// DeWitt Ch. 10 â€” transition from onset of nucleate boiling through
        /// fully developed regime).
        ///
        /// Source: Implementation Plan v5.0.1, Stage 3
        /// </summary>
        public float[] NodeRegimeBlend;

        /// <summary>
        /// IP-0023 (CS-0066): reusable scratch buffer for inter-node mixing heat.
        /// Avoids per-update transient array allocations in hot path.
        /// </summary>
        public float[] NodeMixingHeatScratch;

        // ----- SG Draining & Level Model (v5.0.0 Stage 4) -----

        /// <summary>
        /// True if SG blowdown draining is in progress.
        /// Draining starts when T_rcs reaches SG_DRAINING_START_TEMP_F (~200Â°F)
        /// and continues until mass reaches SG_DRAINING_TARGET_MASS_FRAC of initial.
        /// Source: NRC HRTD 2.3 / 19.0 â€” SG startup draining procedures
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
        /// (1,660,000 Ã— 0.45 = 747,000 lb removed to reach 55% target).
        /// </summary>
        public float TotalMassDrained_lb;

        /// <summary>
        /// Current per-SG draining rate in gpm (0 when not draining).
        /// Nominal: 150 gpm per SG via normal blowdown system.
        /// Source: NRC HRTD 2.3 â€” 150 gpm normal blowdown rate
        /// </summary>
        public float DrainingRate_gpm;

        /// <summary>
        /// SG Wide Range level in percent (0-100%).
        /// References the full SG secondary volume from tubesheet to steam dome.
        /// 100% WR = wet layup (full of water). During heatup, trends down from
        /// 100% as draining removes water, then continues down from steam boiloff.
        /// Source: Westinghouse FSAR â€” SG level instrumentation
        /// </summary>
        public float WideRangeLevel_pct;

        /// <summary>
        /// SG Narrow Range level in percent (0-100%).
        /// References the operating range around the feedwater nozzle.
        /// 100% NR â‰ˆ 55% of wet layup mass. 33% NR is normal operating level.
        /// During wet layup (100% WR), NR reads off-scale high (~182%).
        /// During heatup draining, NR transitions from off-scale to ~33% at
        /// the end of draining.
        /// Source: Westinghouse FSAR â€” SG level instrumentation
        /// </summary>
        public float NarrowRangeLevel_pct;

        /// <summary>
        /// Simulation time (hr) when draining started. 999 if not started.
        /// </summary>
        public float DrainingStartTime_hr;

        // ----- Steam Line Warming Model (v5.1.0 Stage 3) -----

        /// <summary>
        /// Current average temperature of the main steam line piping in Â°F.
        /// Lumped model: all 4 steam lines treated as a single thermal mass.
        ///
        /// During early boiling, cold steam lines condense steam and absorb
        /// latent heat. As the piping warms toward saturation temperature,
        /// condensation rate drops and the energy sink diminishes.
        ///
        /// Initialized at simulation start temperature (same as RCS).
        /// Evolves via: dT/dt = Q_condensation / (M_metal Ã— Cp)
        /// Capped at T_sat(P_secondary) â€” cannot exceed steam temperature.
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
        /// In normal open-system heatup: SteamOutflow_lbhr â‰ˆ SteamProductionRate_lbhr
        /// (steam exits as fast as it's produced â€” quasi-steady-state).
        /// In isolated SG: SteamOutflow_lbhr = 0 (MSIVs closed, no steam dump).
        /// </summary>
        public float SteamOutflow_lbhr;

        /// <summary>
        /// Current steam space volume in ftÂ³ (all 4 SGs).
        /// v5.4.0 Stage 6: Computed from water mass and total SG volume.
        ///
        /// V_steam = V_total - (SecondaryWaterMass_lb / Ï_water)
        /// where V_total is the total SG secondary volume.
        ///
        /// Initial value: 0 ftÂ³ (wet layup, SGs full of water).
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
        ///   P = (m_steam Ã— R Ã— T) / V_steam
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

        /// <summary>Bulk average SG secondary temperature (Â°F)</summary>
        public float BulkAverageTemp_F;

        /// <summary>Top node temperature (Â°F)</summary>
        public float TopNodeTemp_F;

        /// <summary>RCS - SG_top Î”T (Â°F)</summary>
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

        /// <summary>Saturation temperature at current secondary pressure in Â°F</summary>
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
    /// Models all 4 SGs as identical (single set of nodes Ã— 4).
    ///
    /// v3.0.0: Thermocline-based stratification model replaces broken
    /// circulation-onset model. See file header for physics basis.
    ///
    /// Called by RCSHeatup.BulkHeatupStep() and HeatupSimEngine.
    /// Returns SGMultiNodeResult.
    /// </summary>
}

