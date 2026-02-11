// ============================================================================
// CRITICAL: Master the Atom — Multi-Node Steam Generator Thermal Model
// SGMultiNodeThermal.cs — Thermocline-Stratified SG Secondary Side Model
// ============================================================================
//
// PURPOSE:
//   Models the SG secondary side as N vertical nodes to accurately capture
//   thermal stratification during cold heatup with stagnant secondary.
//   Replaces the lumped-parameter approach in SGSecondaryThermal.cs which
//   required artificial correction factors.
//
// PHYSICS (v3.0.0 — Thermocline Model):
//   The SG secondary side in wet layup is a vertical column of ~415,000 lb
//   water per SG with NO forced circulation. During RCS heatup:
//
//   1. RCS hot water flows INSIDE the U-tubes (primary side, forced by RCPs)
//   2. Heat conducts through Inconel 690 tube walls (negligible resistance)
//   3. On the OUTSIDE of tubes (secondary side), natural convection at tube
//      surfaces heats the local water, which rises to form a hot layer at top
//   4. A thermocline boundary separates the hot upper layer from cold lower
//   5. Only tubes ABOVE the thermocline participate in meaningful heat transfer
//   6. The thermocline descends slowly via thermal diffusion (~0.08 ft²/hr)
//   7. When the top node reaches ~220°F, boiling onset dramatically improves HTC
//
//   Key physics correction from v2.x:
//     Stratification (hot on top, cold on bottom) is gravitationally STABLE
//     (Richardson number >> 1). It does NOT drive "natural circulation" as the
//     v2.x model assumed. True secondary circulation only develops with boiling
//     or at operating conditions with downcomer/riser flow established.
//
//   Per-node heat transfer:
//     Q_i = h_i × A_eff_i × (T_rcs - T_node_i) × BundlePenalty
//     where:
//       h_i      = local HTC (stagnant NC or boiling-enhanced)
//       A_eff_i  = geometric_area_fraction × thermocline_effectiveness
//       BundlePenalty = 0.40 (dense tube bundle correction, P/D = 1.42)
//
//   Thermocline position:
//     z_therm = H_total - √(4 × α_eff × t_elapsed)
//     Starts at top (24 ft), descends toward tubesheet (0 ft)
//     Nodes above z_therm: full effectiveness
//     Nodes in transition zone: linear ramp
//     Nodes below z_therm: residual only (0.02)
//
//   Node temperature update:
//     dT_i/dt = Q_i / (m_i × cp_i)
//
// REPLACES:
//   v2.x circulation-onset model which incorrectly treated stable stratification
//   as a circulation trigger, causing 14-19 MW heat absorption and ~26°F/hr
//   heatup rate (should be ~50°F/hr per NRC HRTD 19.2.2).
//
// VALIDATION TARGETS (v3.0.0):
//   - Early heatup (100-200°F): SG absorbs 0.5-2 MW, top node leads by 5-15°F
//   - Mid heatup (200-350°F): SG absorbs 1.5-4 MW, boiling onset at top ~220°F
//   - Late heatup (350-557°F): SG absorbs 4-10 MW with boiling enhancement
//   - Total heatup rate with 4 RCPs: 45-55°F/hr (NRC HRTD 19.2.2 target)
//   - Thermocline descends 1-3 ft over 8 hr heatup period
//   - Energy balance: RCS heat input = SG absorption + losses + RCS temp rise
//
// SOURCES:
//   - WCAP-8530 / WCAP-12700 — Westinghouse Model F SG design
//   - NRC HRTD ML11223A213 Section 5.0 — Steam Generators
//   - NRC HRTD ML11251A016 — SG wet layup conditions
//   - NRC HRTD ML11223A342 Section 19.2.2 — Heatup rate ~50°F/hr
//   - NUREG/CR-5426 — PWR SG natural circulation phenomena
//   - NRC Bulletin 88-11 — Thermal stratification in PWR systems
//   - Incropera & DeWitt Ch. 5, 9, 10 — Conduction, Natural convection, Boiling
//   - Churchill-Chu correlation for horizontal cylinders
//   - Technical_Documentation/SG_THERMAL_MODEL_RESEARCH_v3.0.0.md
//
// UNITS:
//   Temperature: °F | Heat Rate: BTU/hr | Heat Capacity: BTU/°F
//   Mass: lb | Area: ft² | HTC: BTU/(hr·ft²·°F) | Time: hr
//
// ARCHITECTURE:
//   - Called by: RCSHeatup.BulkHeatupStep(), HeatupSimEngine.StepSimulation()
//   - Delegates to: WaterProperties (density, specific heat)
//   - State owned: SGMultiNodeState struct (per-SG node temperatures)
//   - Uses constants from: PlantConstants, PlantConstants.SG
//
// GOLD STANDARD: Yes (v3.0.0)
// ============================================================================

using System;
using UnityEngine;

namespace Critical.Physics
{
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

        #endregion

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

            // Deprecated fields (API compatibility)
            state.CirculationFraction = 0f;
            state.CirculationActive = false;

            return state;
        }

        /// <summary>
        /// Advance the SG multi-node model by one timestep.
        /// Called every physics step by RCSHeatup or HeatupSimEngine.
        ///
        /// Physics sequence (v4.3.0):
        /// 1. Advance thermocline (if RCPs running)
        /// 2. Update secondary pressure (v4.3.0)
        /// 3. Check boiling onset
        /// 4. Stratification state
        /// 5. Calculate per-node HTC and effective area
        /// 6. Apply inter-node conduction (stagnant diffusion)
        /// 7. Update node temperatures
        /// 8. Compute outputs
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
            // 2. UPDATE SECONDARY PRESSURE (v4.3.0)
            // ================================================================
            // Must be called before boiling check because T_sat depends on
            // the current secondary pressure.
            UpdateSecondaryPressure(ref state, T_rcs);

            // ================================================================
            // 3. BOILING CHECK (v4.3.0: dynamic T_sat)
            // ================================================================
            // v4.3.0: Boiling onset is now determined by comparing the hottest
            // node temperature against T_sat(P_secondary) rather than a fixed
            // 220°F threshold. Any node with T > T_sat has superheat > 0 and
            // is experiencing some degree of boiling. The BoilingActive flag
            // indicates any node is above T_sat; the actual intensity is
            // computed per-node in the heat transfer loop (step 5).
            state.BoilingActive = state.MaxSuperheat_F > 0f;

            // ================================================================
            // 4. STRATIFICATION STATE
            // ================================================================
            state.StratificationDeltaT_F = state.NodeTemperatures[0] - state.NodeTemperatures[N - 1];

            // ================================================================
            // 5. PER-NODE HEAT TRANSFER FROM RCS
            // ================================================================
            float totalQ_BTUhr = 0f;

            // Total tube area for all 4 SGs
            float totalArea = PlantConstants.SG_HT_AREA_PER_SG_FT2 * PlantConstants.SG_COUNT;

            // Track total active area fraction and peak boiling intensity
            float totalActiveAreaFrac = 0f;
            float maxBoilingIntensity = 0f;

            for (int i = 0; i < N; i++)
            {
                // Node ΔT
                float nodeT = state.NodeTemperatures[i];
                float deltaTNode = T_rcs - nodeT;

                if (deltaTNode < MIN_DELTA_T)
                {
                    state.NodeHeatRates[i] = 0f;
                    state.NodeHTCs[i] = 0f;
                    state.NodeEffectiveAreaFractions[i] = 0f;
                    continue;
                }

                // Effective HTC for this node (v4.3.0: includes superheat-based boiling ramp)
                float nodeBoilIntensity;
                float htc = GetNodeHTC(i, N, nodeT, rcpsRunning,
                    state.SaturationTemp_F, out nodeBoilIntensity);
                state.NodeHTCs[i] = htc;
                if (nodeBoilIntensity > maxBoilingIntensity)
                    maxBoilingIntensity = nodeBoilIntensity;

                // Thermocline-based effective area fraction
                float areaFrac = GetNodeEffectiveAreaFraction(i, N, state.ThermoclineHeight_ft);
                state.NodeEffectiveAreaFractions[i] = areaFrac;
                totalActiveAreaFrac += areaFrac;

                // Effective area (ft²) with bundle penalty
                float A_eff = areaFrac * totalArea * PlantConstants.SG_BUNDLE_PENALTY_FACTOR;

                // Heat transfer: Q_i = h_i × A_eff_i × ΔT_i
                float Q_i = htc * A_eff * deltaTNode;
                state.NodeHeatRates[i] = Q_i;
                totalQ_BTUhr += Q_i;
            }

            state.ActiveAreaFraction = totalActiveAreaFrac;

            // ================================================================
            // 6. INTER-NODE CONDUCTION (stagnant thermal diffusion)
            // ================================================================
            // In stagnant stratified conditions, only slow thermal diffusion
            // connects adjacent nodes. If boiling is active in the top node,
            // enhance mixing for nodes adjacent to the boiling zone.
            float[] mixingHeat = new float[N];
            for (int i = 0; i < N - 1; i++)
            {
                float dT = state.NodeTemperatures[i] - state.NodeTemperatures[i + 1];

                // Use enhanced UA only if boiling is active in the upper node
                bool boilingAtInterface = state.BoilingActive && i <= 1;
                float ua = boilingAtInterface ? INTERNODE_UA_BOILING : INTERNODE_UA_STAGNANT;

                // Mix heat per SG × 4 SGs
                float Q_mix = ua * dT * PlantConstants.SG_COUNT;
                mixingHeat[i] -= Q_mix;      // Upper node loses heat
                mixingHeat[i + 1] += Q_mix;  // Lower node gains heat
            }

            // ================================================================
            // 7. UPDATE NODE TEMPERATURES
            // ================================================================
            float waterMassPerSG = PlantConstants.SG_SECONDARY_WATER_PER_SG_LB;
            float metalMassPerSG = PlantConstants.SG_SECONDARY_METAL_PER_SG_LB;

            for (int i = 0; i < N; i++)
            {
                // Node thermal mass (water + metal fraction for this node)
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
                float totalNodeQ = state.NodeHeatRates[i] + mixingHeat[i];

                // Temperature change
                float dT_node = totalNodeQ * dt_hr / nodeHeatCap;
                state.NodeTemperatures[i] += dT_node;
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

            // Deprecated (API compatibility)
            result.CirculationFraction = 0f;
            result.CirculationActive = false;

            return result;
        }

        /// <summary>
        /// Get a summary string for logging/diagnostics.
        /// </summary>
        public static string GetDiagnosticString(SGMultiNodeState state, float T_rcs)
        {
            int N = state.NodeCount;
            string boilStr = state.BoilingActive ? "BOILING" : "subcooled";
            string n2Str = state.NitrogenIsolated ? "N\u2082 ISOLATED" : "N\u2082 BLANKETED";
            float P_psig = state.SecondaryPressure_psia - 14.7f;
            string s = $"SG MultiNode [{N} nodes] | Q_total={state.TotalHeatAbsorption_MW:F2} MW | " +
                       $"Thermocline={state.ThermoclineHeight_ft:F1} ft | " +
                       $"ActiveArea={state.ActiveAreaFraction:P1} | " +
                       $"{boilStr} | t_heat={state.ElapsedHeatupTime_hr:F2} hr\n" +
                       $"  P_sec={P_psig:F0} psig | T_sat={state.SaturationTemp_F:F1}\u00b0F | " +
                       $"Superheat={state.MaxSuperheat_F:F1}\u00b0F | {n2Str}\n";
            for (int i = 0; i < N; i++)
            {
                string label = i == 0 ? "TOP" : (i == N - 1 ? "BOT" : $"N{i}");
                float dT = T_rcs - state.NodeTemperatures[i];
                float nodeBot = PlantConstants.SG_TUBE_TOTAL_HEIGHT_FT - (i + 1) * NODE_HEIGHT_FT;
                float nodeTop = PlantConstants.SG_TUBE_TOTAL_HEIGHT_FT - i * NODE_HEIGHT_FT;
                string posStr = state.ThermoclineHeight_ft < nodeBot ? "BELOW" :
                               (state.ThermoclineHeight_ft > nodeTop ? "ABOVE" : "TRANS");
                s += $"  {label}: T={state.NodeTemperatures[i]:F1}°F  ΔT={dT:F1}°F  " +
                     $"Q={state.NodeHeatRates[i] / MW_TO_BTU_HR:F3}MW  " +
                     $"h={state.NodeHTCs[i]:F0}  Af={state.NodeEffectiveAreaFractions[i]:F3}  " +
                     $"[{posStr}]\n";
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
        /// Update SG secondary pressure based on hottest node temperature.
        ///
        /// Physics: Before steam onset, pressure = initial N₂ blanket pressure.
        /// After steam onset (hottest node ≥ T_sat at current pressure), the
        /// secondary is a closed two-phase vessel and pressure tracks the
        /// saturation curve of the hottest node.
        ///
        /// This quasi-static approach assumes the SG secondary is in
        /// thermodynamic equilibrium — valid because the large water mass
        /// (415,000 lb/SG) changes temperature slowly.
        ///
        /// The pressure creates a self-limiting feedback loop:
        ///   1. Boiling starts at T_sat(P_initial) ≈ 220°F
        ///   2. Steam production pressurizes the closed secondary
        ///   3. Higher pressure raises T_sat, reducing superheat
        ///   4. Reduced superheat reduces boiling intensity
        ///   5. System self-regulates — no cliff, no runaway
        ///
        /// Source: NRC HRTD ML11223A342 Section 19.2.2 — steam onset at ~220°F,
        ///         NRC HRTD ML11223A294 Section 11.2 — steam dumps at 1092 psig
        /// </summary>
        /// <param name="state">SG state (modified in place)</param>
        /// <param name="T_rcs">Current RCS bulk temperature (°F)</param>
        private static void UpdateSecondaryPressure(ref SGMultiNodeState state, float T_rcs)
        {
            // Find the hottest secondary node temperature
            float T_hottest = state.NodeTemperatures[0];
            for (int i = 1; i < state.NodeCount; i++)
            {
                if (state.NodeTemperatures[i] > T_hottest)
                    T_hottest = state.NodeTemperatures[i];
            }

            // Current T_sat at the secondary pressure
            float T_sat = WaterProperties.SaturationTemperature(state.SecondaryPressure_psia);

            // Check for nitrogen isolation (one-time event at ~220°F RCS)
            // Use RCS temperature as the trigger per NRC HRTD 19.2.2
            if (!state.NitrogenIsolated && T_rcs >= PlantConstants.SG_NITROGEN_ISOLATION_TEMP_F)
            {
                state.NitrogenIsolated = true;
            }

            // Pressure evolution
            if (!state.NitrogenIsolated || T_hottest < T_sat)
            {
                // Pre-steaming phase: pressure stays at N₂ blanket value
                // Either N₂ is still connected (open system) or all nodes are
                // subcooled (no steam production to pressurize).
                state.SecondaryPressure_psia = PlantConstants.SG_INITIAL_PRESSURE_PSIA;
            }
            else
            {
                // Pressurization phase: secondary is closed, two-phase vessel.
                // Pressure tracks saturation curve of hottest node (quasi-static).
                float P_sat = WaterProperties.SaturationPressure(T_hottest);

                // Clamp to physical limits:
                //   Lower: cannot go below initial N₂ blanket pressure
                //   Upper: safety valve setpoint + margin (1185 psig = 1199.7 psia)
                float P_min = PlantConstants.SG_INITIAL_PRESSURE_PSIA;
                float P_max = PlantConstants.SG_SAFETY_VALVE_SETPOINT_PSIG + 14.7f;
                state.SecondaryPressure_psia = Mathf.Clamp(P_sat, P_min, P_max);
            }

            // Update derived state
            state.SaturationTemp_F = WaterProperties.SaturationTemperature(
                state.SecondaryPressure_psia);
            state.MaxSuperheat_F = Mathf.Max(0f, T_hottest - state.SaturationTemp_F);
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

            if (valid)
                Debug.Log("[SGMultiNode] All validation tests PASSED (v3.0.0 thermocline model)");
            else
                Debug.LogError("[SGMultiNode] Validation FAILED — check warnings above");

            return valid;
        }

        #endregion
    }
}
