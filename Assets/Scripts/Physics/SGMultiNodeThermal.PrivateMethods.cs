// CRITICAL: Master the Atom - Physics Module
// SGMultiNodeThermal.PrivateMethods.cs - Internal SG computations
//
// File: Assets/Scripts/Physics/SGMultiNodeThermal.PrivateMethods.cs
// Module: Critical.Physics.SGMultiNodeThermal
// Responsibility: Internal helper computations for pressure, boiling, and node thermals.
// Standards: GOLD v1.0, SRP/SOLID
// Version: 1.0
// Last Updated: 2026-02-18
using System;
using UnityEngine;
namespace Critical.Physics
{
    public static partial class SGMultiNodeThermal
    {
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
        /// rather than a step function at a fixed 220Â°F threshold. This
        /// prevents the cliff behavior where HTC jumps 5Ã— in one timestep.
        ///
        /// With RCPs running:
        ///   1/U = 1/h_primary + R_wall + 1/h_secondary
        ///   h_primary â‰ˆ 800-3000 BTU/(hrÂ·ftÂ²Â·Â°F) (forced convection inside tubes)
        ///   R_wall â‰ˆ negligible (thin Inconel, high k)
        ///   h_secondary â‰ˆ 30-60 BTU/(hrÂ·ftÂ²Â·Â°F) (stagnant NC with bundle penalty)
        ///   h_secondary â‰ˆ 150-300 if boiling (nucleate boiling enhancement)
        ///   â†’ U is controlled by h_secondary
        ///
        /// Without RCPs:
        ///   h_primary drops to ~10-50 (natural convection inside tubes too)
        ///   â†’ U â‰ˆ 7-25 BTU/(hrÂ·ftÂ²Â·Â°F) â€” negligible heat transfer
        /// </summary>
        /// <param name="nodeIndex">Node index (0 = top)</param>
        /// <param name="nodeCount">Total number of nodes</param>
        /// <param name="nodeTemp_F">Node secondary temperature (Â°F)</param>
        /// <param name="rcpsRunning">Number of RCPs operating (0-4)</param>
        /// <param name="Tsat_F">Saturation temperature at current SG secondary pressure (Â°F)</param>
        /// <param name="boilingIntensity">Output: boiling intensity fraction for this node (0-1)</param>
        private static float GetNodeHTC(int nodeIndex, int nodeCount,
            float nodeTemp_F, int rcpsRunning, float Tsat_F, out float boilingIntensity)
        {
            boilingIntensity = 0f;

            // No RCPs â†’ negligible heat transfer (both sides natural convection)
            if (rcpsRunning == 0)
                return HTC_NO_RCPS;

            // Secondary-side HTC: stagnant natural convection on tube exteriors
            float h_secondary = PlantConstants.SG_MULTINODE_HTC_STAGNANT;

            // Temperature effect: at low temperatures, fluid properties
            // reduce natural convection effectiveness (higher viscosity, lower Î²)
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

            // Primary side: forced convection with RCPs â€” very high h
            // Scale with RCP count (each pump provides ~25% of flow)
            float primaryFactor = Mathf.Min(1f, rcpsRunning / 4f);
            float h_primary = 1000f * primaryFactor + 10f * (1f - primaryFactor);

            // Overall HTC (series resistance)
            float U = 1f / (1f / h_primary + 1f / h_secondary);

            return U;
        }

        /// <summary>
        /// Get the overall HTC for a boiling node in BTU/(hrÂ·ftÂ²Â·Â°F).
        ///
        /// v5.0.0: For nodes that are at T_sat and actively boiling, the
        /// secondary-side HTC is dominated by nucleate boiling (h â‰ˆ 2,000â€“10,000).
        /// The overall U is then limited by the primary-side forced convection:
        ///   1/U = 1/h_primary + 1/h_boiling
        ///
        /// Since h_boiling >> h_primary, U â‰ˆ h_primary.
        /// This means the exact boiling HTC value matters much less than
        /// getting the regime right â€” the energy transfer is primary-limited.
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
        /// <returns>Overall HTC in BTU/(hrÂ·ftÂ²Â·Â°F)</returns>
        private static float GetBoilingNodeHTC(int rcpsRunning, float secondaryPressure_psia)
        {
            // No RCPs â†’ no primary-side flow â†’ negligible heat transfer
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
        /// Node above thermocline: full geometric area Ã— stagnant effectiveness
        /// Node containing thermocline: linearly interpolated
        /// Node below thermocline: residual only (SG_BELOW_THERMOCLINE_EFF)
        ///
        /// The stagnant effectiveness array (from PlantConstants.SG) is still
        /// used because even above the thermocline, the hot boundary layer
        /// reduces the effective driving Î”T at different vertical positions.
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
                // Entire node is above thermocline â€” fully active
                aboveFraction = 1f;
            }
            else if (nodeTopElev <= thermBot)
            {
                // Entire node is below thermocline â€” residual only
                aboveFraction = 0f;
            }
            else
            {
                // Node straddles the thermocline â€” linear interpolation
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
        ///   SUBCOOLED (pre-steam): Pressure = Nâ‚‚ blanket value (17 psia).
        ///     Nâ‚‚ supply maintains slight positive pressure. No steam production.
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
        /// Source: NRC HRTD ML11223A342 Section 19.0 â€” steam onset at ~220Â°F,
        ///         NRC HRTD ML11223A294 Section 11.2 â€” steam dumps at 1092 psig,
        ///         Implementation Plan v5.1.0 Stage 1
        /// </summary>
        /// <param name="state">SG state (modified in place)</param>
        /// <param name="T_rcs">Current RCS bulk temperature (Â°F)</param>
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

            // Check for nitrogen isolation (one-time event at ~220Â°F RCS)
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
            // v5.1.0 Stage 2: Guard for boilingâ†’subcooled reversion.
            // The condition for pre-steaming requires BOTH:
            //   (a) Nâ‚‚ is not yet isolated, OR
            //   (b) T_hottest is below T_sat AND current pressure is at or
            //       below the Nâ‚‚ blanket value.
            //
            // Without guard (b), a transient dip where T_hottest drops just
            // below T_sat would snap pressure from P_sat(T_hottest) back to
            // 17 psia instantly â€” a potentially large discontinuity mid-heatup.
            //
            // With guard (b), once pressure has risen above the Nâ‚‚ blanket,
            // the boiling branch continues to track P_sat(T_hottest) downward
            // smoothly. Pressure only returns to the Nâ‚‚ blanket value when
            // P_sat(T_hottest) naturally falls to that level â€” which means
            // the secondary has genuinely cooled back to pre-steaming conditions.
            bool presteaming = !state.NitrogenIsolated ||
                               (T_hottest < T_sat_current &&
                                state.SecondaryPressure_psia <= state.InventoryPressure_psia + 0.5f);

            if (presteaming)
            {
                // ============================================================
                // PRE-STEAMING: Nâ‚‚ blanket pressure
                // ============================================================
                // Either Nâ‚‚ is still connected (maintains blanket pressure)
                // or all nodes are genuinely subcooled at near-atmospheric
                // conditions (pressure has not risen above the Nâ‚‚ blanket).
                state.SecondaryPressure_psia = state.InventoryPressure_psia;
                bool nearFloor =
                    state.SecondaryPressure_psia <= PlantConstants.SG_MIN_STEAMING_PRESSURE_PSIA + 0.1f;

                // Avoid pressure-source label chatter at the floor threshold.
                // Once inventory-derived mode has been reached during pre-boil
                // startup, keep it latched until saturation tracking takes over.
                if (nearFloor && state.PressureSourceMode == SGPressureSourceMode.InventoryDerived)
                {
                    state.PressureSourceMode = SGPressureSourceMode.InventoryDerived;
                }
                else
                {
                    state.PressureSourceMode = nearFloor
                        ? SGPressureSourceMode.Floor
                        : SGPressureSourceMode.InventoryDerived;
                }
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
                //   Higher T_hottest â†’ higher P_secondary â†’ higher T_sat
                //   â†’ smaller (T_rcs - T_sat) â†’ less boiling heat transfer
                //   â†’ T_hottest rises more slowly
                //
                // No artificial rate limiting. Physical damping provided by
                // steam line condensation energy sink (Section 8c, v5.1.0 Stage 3).
                // ============================================================

                float P_new = WaterProperties.SaturationPressure(T_hottest);

                // Clamp to physical limits:
                //   Lower: cannot go below Nâ‚‚ blanket / atmospheric
                //   Upper: safety valve ceiling (backup protection)
                float P_min = PlantConstants.SG_MIN_STEAMING_PRESSURE_PSIA;
                float P_safetyValve = PlantConstants.SG_SAFETY_VALVE_SETPOINT_PSIG + 14.7f;
                P_new = Mathf.Clamp(P_new, P_min, P_safetyValve);

                // Cap at steam dump setpoint during normal operation.
                // When SG pressure reaches 1092 psig, steam dumps modulate to
                // hold pressure constant. T_sat(1092 psig) = 557Â°F = no-load T_avg.
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
        /// Uses a Hermite smoothstep (3tÂ² - 2tÂ³) to ramp from 0 to 1 over
        /// the superheat range SG_BOILING_SUPERHEAT_RANGE_F (20Â°F).
        ///
        /// f_boil = smoothstep(Î”T_superheat / SG_BOILING_SUPERHEAT_RANGE_F)
        /// where Î”T_superheat = max(0, T_node - T_sat(P_secondary))
        ///
        /// Returns 0.0 if subcooled (node below T_sat).
        /// Returns 0.5 at 10Â°F superheat (half of 20Â°F range).
        /// Returns 1.0 at 20Â°F+ superheat (fully developed nucleate boiling).
        ///
        /// The key physics insight: as secondary pressure rises with temperature,
        /// T_sat rises too, so the superheat Î”T remains small even as absolute
        /// temperatures increase. This creates the self-limiting feedback that
        /// prevents cliff behavior.
        ///
        /// Source: Incropera & DeWitt Ch. 10 â€” boiling curve transition,
        ///         onset of nucleate boiling through fully developed regime
        /// </summary>
        /// <param name="nodeTemp_F">Node secondary temperature (Â°F)</param>
        /// <param name="Tsat_F">Saturation temperature at current SG secondary pressure (Â°F)</param>
        /// <returns>Boiling intensity fraction (0.0 - 1.0)</returns>
        private static float GetBoilingIntensityFraction(float nodeTemp_F, float Tsat_F)
        {
            float superheat = nodeTemp_F - Tsat_F;
            if (superheat <= 0f)
                return 0f;

            float t = superheat / PlantConstants.SG_BOILING_SUPERHEAT_RANGE_F;
            t = Mathf.Clamp01(t);

            // Hermite smoothstep: 3tÂ² - 2tÂ³
            // Smooth onset and smooth approach to full boiling
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Compute diagnostic wall temperature for the hottest boiling node.
        /// Used by GetDiagnosticString() to report T_wall.
        ///
        /// Uses the same algebraic formula as the main Update() loop:
        ///   T_wall = T_rcs - Q_node_prev / (h_primary Ã— A_node)
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
        ///   h ~ (gÎ²Î”T/Î½Î±)^(1/4) for laminar, ^(1/3) for turbulent
        ///   At 100Â°F: Î½ = 0.739Ã—10^-5 ftÂ²/s, Î² = 0.00022/Â°F
        ///   At 300Â°F: Î½ = 0.204Ã—10^-5 ftÂ²/s, Î² = 0.00043/Â°F
        ///   Ratio of (Î²/Î½Â²): 300Â°F is ~16Ã— higher than 100Â°F
        ///   Ra ratio: ~16, Nu ratio: ~2.5 (1/4 power), h ratio: ~2.5
        ///
        /// Using a smoother scaling: factor ranges from 0.5 at 100Â°F to 1.0 at 400Â°F
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
    }
}
