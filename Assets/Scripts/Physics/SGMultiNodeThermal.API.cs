// CRITICAL: Master the Atom - Physics Module
// SGMultiNodeThermal.API.cs - Public model entry points
//
// File: Assets/Scripts/Physics/SGMultiNodeThermal.API.cs
// Module: Critical.Physics.SGMultiNodeThermal
// Responsibility: Initialization, update, and operator-facing SG control APIs.
// Standards: GOLD v1.0, SRP/SOLID
// Version: 1.0
// Last Updated: 2026-02-18
using System;
using UnityEngine;
namespace Critical.Physics
{
    public static partial class SGMultiNodeThermal
    {
        #region Public API

        /// <summary>
        /// Initialize state for cold shutdown (wet layup) conditions.
        /// All nodes start at the same temperature as RCS (thermal equilibrium).
        /// Thermocline starts at top of tube bundle (no active area yet).
        /// Called once at simulation start.
        /// </summary>
        /// <param name="initialTempF">Starting temperature (Â°F), same as RCS</param>
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

            // Thermocline starts at top â€” only U-bend is initially active
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

            state.NodeMixingHeatScratch = new float[N];

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

            // NR is off-scale high at wet layup: 100% NR â‰ˆ 55% of wet layup
            // mass, so at 100% mass, NR â‰ˆ 100/0.55 = 182%
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
        /// stabilizes at T_sat(1092 psig) = 557Â°F.
        ///
        /// v5.0.1: Per-node NodeRegimeBlend ramp replaces binary boiling switch.
        /// HTC, effective area, and driving Î”T are blended over ~60 sim-seconds.
        /// Section 8b: Delta clamp limits |Î”Q| to 5 MW/timestep with bypass.
        ///
        /// v5.1.0: Saturation tracking (Stage 1), reversion guard (Stage 2),
        /// steam line condensation energy sink (Stage 3), wall superheat
        /// boiling driver (Stage 4). See IMPLEMENTATION_PLAN_v5.1.0.md.
        /// </summary>
        /// <param name="state">SG state (modified in place)</param>
        /// <param name="T_rcs">Current RCS bulk temperature (Â°F)</param>
        /// <param name="rcpsRunning">Number of RCPs operating (0-4)</param>
        /// <param name="pressurePsia">System pressure (psia)</param>
        /// <param name="dt_hr">Timestep in hours</param>
        /// <returns>Result struct with total heat removal and diagnostics</returns>
        /// <summary>
        /// Backward-compatible Update (assumes condenser sink always available).
        /// </summary>
        public static SGMultiNodeResult Update(
            ref SGMultiNodeState state,
            float T_rcs,
            int rcpsRunning,
            float pressurePsia,
            float dt_hr)
        {
            return Update(ref state, T_rcs, rcpsRunning, pressurePsia, dt_hr,
                condenserSinkAvailable: true);
        }

        /// <summary>
        /// Update the multi-node SG thermal model for one timestep.
        ///
        /// IP-0046 (CS-0116): The condenserSinkAvailable parameter controls
        /// whether steam produced during boiling can exit the SG to the
        /// condenser via dump valves. When false, steam accumulates in the
        /// SG secondary (inventory-derived pressure). When true, steam exits
        /// freely (saturation pressure tracking).
        /// </summary>
        /// <param name="state">SG state (modified in place)</param>
        /// <param name="T_rcs">Current RCS bulk temperature (°F)</param>
        /// <param name="rcpsRunning">Number of RCPs operating (0-4)</param>
        /// <param name="pressurePsia">System pressure (psia)</param>
        /// <param name="dt_hr">Timestep in hours</param>
        /// <param name="condenserSinkAvailable">True if condenser/dump path can accept steam</param>
        /// <returns>Result struct with total heat removal and diagnostics</returns>
        public static SGMultiNodeResult Update(
            ref SGMultiNodeState state,
            float T_rcs,
            int rcpsRunning,
            float pressurePsia,
            float dt_hr,
            bool condenserSinkAvailable)
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
            // z_therm = H_total - âˆš(4 Ã— Î±_eff Ã— t)
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
            //   Subcooled: all nodes below T_sat(P_secondary) â€” closed system
            //   Boiling:   at least one node at/above T_sat â€” open system
            //   SteamDump: P_secondary >= steam dump setpoint â€” heat rejection
            //
            // Stage 2: Boiling regime now has distinct physics path.
            // Steps 5â€“7 are regime-dependent (subcooled vs boiling).
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
            // The energy computed as Q_i does NOT heat the node â€” it produces
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
                // from 0â†’1 over REGIME_BLEND_RAMP_HR (~60 sim-seconds).
                // All three parameters (HTC, area, driving Î”T) are blended
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
                    // Node is subcooled â€” reset blend to 0.0
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
                //   T_wall = T_rcs - Q_prev / (h_primary Ã— A_node)
                // Using previous timestep Q avoids implicit coupling.
                // Clamped: T_sat â‰¤ T_wall â‰¤ T_rcs
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
                    // No RCPs or zero area â€” wall at T_sat (no boiling drive)
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

                // Effective area (ftÂ²) with bundle penalty
                float A_eff = areaFrac * totalArea * PlantConstants.SG_BUNDLE_PENALTY_FACTOR;

                // Heat transfer: Q_i = h_i Ã— A_eff_i Ã— Î”T_i
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
            float[] mixingHeat = state.NodeMixingHeatScratch;
            if (mixingHeat == null || mixingHeat.Length != N)
            {
                mixingHeat = new float[N];
                state.NodeMixingHeatScratch = mixingHeat;
            }
            else
            {
                Array.Clear(mixingHeat, 0, N);
            }
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

                // Mix heat per SG Ã— 4 SGs
                float Q_mix = ua * dT * PlantConstants.SG_COUNT;
                mixingHeat[i] -= Q_mix;      // Upper node loses heat
                mixingHeat[i + 1] += Q_mix;  // Lower node gains heat
            }

            // ================================================================
            // 7. UPDATE NODE TEMPERATURES (regime-dependent)
            // ================================================================
            // v5.0.0 Stage 2: Two paths per node:
            //   SUBCOOLED: dT = Q Ã— dt / (m Ã— cp)  [energy heats water]
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
            // Q_condensation = UA Ã— (T_sat - T_steamLine)
            // dT_steamLine/dt = Q_condensation / (M_metal Ã— Cp)
            // T_steamLine capped at T_sat (cannot exceed steam temperature)
            //
            // Effect: Early boiling â†’ cold lines absorb significant energy â†’
            //         less net steam production â†’ slower node heating â†’
            //         slower pressure rise (physical damping).
            //         As lines warm â†’ condensation drops â†’ pure sat tracking.
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
                    // Condensation heat transfer: Q = UA Ã— Î”T
                    float Q_condensation = PlantConstants.SG_STEAM_LINE_UA * dT_steamLine;

                    // Cannot remove more energy than is available from boiling
                    Q_condensation = Mathf.Min(Q_condensation, totalQ_boiling_BTUhr * 0.95f);

                    // Warm the steam line metal
                    float steamLineHeatCap = PlantConstants.SG_STEAM_LINE_METAL_MASS_LB
                                           * PlantConstants.SG_STEAM_LINE_CP;
                    float dT_metal = Q_condensation * dt_hr / steamLineHeatCap;
                    state.SteamLineTempF += dT_metal;

                    // Cap at T_sat â€” steam line cannot exceed steam temperature
                    state.SteamLineTempF = Mathf.Min(state.SteamLineTempF, T_steam);

                    // Subtract condensation energy from boiling budget
                    totalQ_boiling_BTUhr -= Q_condensation;
                    totalQ_boiling_BTUhr = Mathf.Max(totalQ_boiling_BTUhr, 0f);

                    // Store diagnostic
                    state.SteamLineCondensationRate_BTUhr = Q_condensation;
                }
                else
                {
                    // Steam lines at or above T_sat â€” no condensation
                    state.SteamLineCondensationRate_BTUhr = 0f;
                }
            }
            else
            {
                // Not boiling â€” no condensation energy sink
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
            // Draining starts at T_rcs â‰ˆ 200Â°F and runs at 150 gpm per SG.
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

                // Convert gpm to lb/hr: gpm Ã— (1 min/60 sec Ã— 1 hr/60 min) = gph
                // gpm Ã— 60 = gph, then gph Ã— ftÂ³/gal Ã— Ï = lb/hr
                // At ~200Â°F secondary water: Ï â‰ˆ 60.1 lb/ftÂ³
                float rho_secondary = WaterProperties.WaterDensity(
                    state.BulkAverageTemp_F, state.SecondaryPressure_psia);
                float totalDrainRate_gpm = PlantConstants.SG_DRAINING_RATE_GPM
                                         * PlantConstants.SG_COUNT;
                // gpm Ã— (ftÂ³/7.48052 gal) Ã— (60 min/hr) Ã— Ï (lb/ftÂ³) = lb/hr
                float drainRate_lbhr = totalDrainRate_gpm / 7.48052f * 60f * rho_secondary;
                float drainThisStep_lb = drainRate_lbhr * dt_hr;

                // Donâ€™t drain below target
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
            //   0% NR = tubes uncovered (critical low â€” not modelled here).
            //   33% NR = normal operating level.
            //   During wet layup (100% WR), NR reads off-scale high (~182%).
            //
            // Note: Real SG level is affected by density (temperature) and
            // two-phase effects during steaming. This first-order model uses
            // mass ratio, which is sufficient for heatup tracking. A more
            // detailed collapsed/indicated level model is deferred to the
            // Screen 5 SG display implementation (Future Features Priority 5).
            //
            // Source: Westinghouse FSAR â€” SG level instrumentation
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
            // Steam space volume: V_steam = V_total - (m_water / Ï_water)
            // Steam inventory: dm/dt = SteamProductionRate - SteamOutflow
            // Inventory pressure: P = (m Ã— R Ã— T) / V (ideal gas)
            //
            // Default: SteamIsolated = false (open system, sat tracking).
            // When SteamIsolated = true, inventory-based pressure is used.
            //
            // Source: Implementation Plan v5.4.0 Stage 6
            // ================================================================
            {
                state.SteamSpaceVolume_ft3 = ComputeGasCushionVolumeFt3(
                    state, state.BulkAverageTemp_F, state.SecondaryPressure_psia);

                // Steam outflow rate depends on isolation state and sink availability
                if (state.SteamIsolated)
                {
                    // Isolated SG: no steam outlet (MSIVs closed)
                    state.SteamOutflow_lbhr = 0f;
                }
                else if (!condenserSinkAvailable)
                {
                    // CS-0116: Open system but no condenser sink available.
                    // Steam cannot exit via dump valves — accumulates in SG
                    // secondary. Inventory-derived pressure will govern.
                    // This is the correct behavior during early startup before
                    // condenser vacuum is established (C-9 not yet satisfied).
                    state.SteamOutflow_lbhr = 0f;
                }
                else
                {
                    // Open system with condenser sink: steam exits as fast as produced
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

                // When steam cannot exit (isolated OR no sink available), use
                // inventory-based pressure instead of saturation tracking.
                // CS-0116: This now also applies when the SG boundary is OPEN
                // but the condenser/dump path is blocked (pre-C-9, P-12 blocking).
                // Steam accumulates → inventory pressure rises naturally from floor.
                if (state.SteamIsolated || (!condenserSinkAvailable && state.SteamInventory_lb > 0.1f))
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

        #endregion
    }
}
