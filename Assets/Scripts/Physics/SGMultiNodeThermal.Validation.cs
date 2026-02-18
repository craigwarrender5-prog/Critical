// CRITICAL: Master the Atom - Physics Module
// SGMultiNodeThermal.Validation.cs - Model validation gates
//
// File: Assets/Scripts/Physics/SGMultiNodeThermal.Validation.cs
// Module: Critical.Physics.SGMultiNodeThermal
// Responsibility: Self-check/validation routines for SG thermal model consistency.
// Standards: GOLD v1.0, SRP/SOLID
// Version: 1.0
// Last Updated: 2026-02-18
using System;
using UnityEngine;
namespace Critical.Physics
{
    public static partial class SGMultiNodeThermal
    {
        #region Validation

        /// <summary>
        /// Validate the multi-node SG model produces realistic results.
        /// v3.0.0: Updated tests for thermocline model.
        /// </summary>
        public static bool ValidateModel()
        {
            bool valid = true;

            // Test 1: Initialize at 100Â°F â€” all nodes equal, thermocline at top
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

            // Test 2: One step with T_rcs=120Â°F, 4 RCPs â€” total Q should be < 2 MW
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

            // Test 4: Run 100 steps (~17 min) â€” Q should remain realistic
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

            // Test 5: No RCPs â†’ very low heat transfer
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
            // After 1 hour: descent â‰ˆ âˆš(4 Ã— 0.08 Ã— 1) â‰ˆ 0.57 ft
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
                Debug.LogWarning("[SGMultiNode Validation] Test 9 FAIL: Boiling not detected at 225Â°F top node");
                valid = false;
            }

            // Test 10: Long-term energy absorption check (simulate 2 hours)
            // With 4 RCPs at ~150Â°F, thermocline model should keep Q < 3 MW average
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

            // Test 11: v5.0.0 regime detection â€” starts Subcooled
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
            state.NodeTemperatures[0] = 225f;  // Above T_sat at 17 psia (~220Â°F)
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

            // Test 13: v5.0.0 Stage 2 â€” boiling node produces steam (rate > 0)
            // Test 12 already put us in boiling regime with top node at ~T_sat
            if (state.SteamProductionRate_lbhr <= 0f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 13 FAIL: Steam rate={state.SteamProductionRate_lbhr:F0} lb/hr (expected > 0 in boiling regime)");
                valid = false;
            }

            // Test 14: v5.0.0 Stage 2 â€” boiling node temperature clamped to T_sat
            // The top node was set to 225Â°F (above T_sat ~220Â°F at 17 psia).
            // After Update(), it should be clamped to T_sat, NOT allowed to rise above.
            float expectedTsat = WaterProperties.SaturationTemperature(state.SecondaryPressure_psia);
            if (Math.Abs(state.NodeTemperatures[0] - expectedTsat) > 2f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 14 FAIL: Boiling node T={state.NodeTemperatures[0]:F1}Â°F " +
                                $"(expected T_sat={expectedTsat:F1}Â°F)");
                valid = false;
            }

            // Test 15: v5.0.0 Stage 2 â€” secondary water mass decreases during boiling
            float initialMass = PlantConstants.SG_SECONDARY_WATER_PER_SG_LB * PlantConstants.SG_COUNT;
            if (state.SecondaryWaterMass_lb >= initialMass)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 15 FAIL: Sec mass={state.SecondaryWaterMass_lb:F0} " +
                                $"(should be < initial {initialMass:F0} after steam production)");
                valid = false;
            }

            // Test 16: v5.1.0 â€” pressure tracks saturation directly (no rate limit)
            // Initialize at 100Â°F, force top node to 350Â°F (P_sat â‰ˆ 135 psia).
            // After one timestep, pressure should jump directly to P_sat(350Â°F)
            // because saturation tracking is now instantaneous (v5.1.0 Stage 1).
            state = Initialize(100f);
            state.NitrogenIsolated = true;  // Force Nâ‚‚ isolated
            state.NodeTemperatures[0] = 350f;
            Update(ref state, 400f, 4, 500f, 1f / 360f);
            float P_sat_350 = WaterProperties.SaturationPressure(350f);
            if (Math.Abs(state.SecondaryPressure_psia - P_sat_350) > 5f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 16 FAIL: Pressure={state.SecondaryPressure_psia:F1} psia " +
                                $"(expected P_sat(350Â°F)={P_sat_350:F1} psia â€” saturation tracking)");
                valid = false;
            }

            // Test 17: v5.0.0 Stage 2 â€” energy balance in boiling regime
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
                Debug.LogWarning($"[SGMultiNode Validation] Test 17 FAIL: Top node {state.NodeTemperatures[0]:F1}Â°F >> T_sat {currentTsat:F1}Â°F");
                valid = false;
            }

            // Test 18: v5.0.0 Stage 3 â€” pressure caps at steam dump setpoint
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

            // Test 19: v5.0.0 Stage 3 â€” T_sat at steam dump pressure = ~557Â°F
            float Tsat_at_steamDump = WaterProperties.SaturationTemperature(steamDumpP_psia);
            if (Math.Abs(Tsat_at_steamDump - 557f) > 5f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 19 FAIL: T_sat at steam dump P = {Tsat_at_steamDump:F1}Â°F " +
                                $"(expected ~557Â°F)");
                valid = false;
            }
            // Boiling nodes should be at T_sat = ~557Â°F
            if (Math.Abs(state.NodeTemperatures[0] - Tsat_at_steamDump) > 3f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 19 FAIL: Top node={state.NodeTemperatures[0]:F1}Â°F " +
                                $"(expected T_sat={Tsat_at_steamDump:F1}Â°F)");
                valid = false;
            }

            // Test 20: v5.0.1 â€” NodeRegimeBlend initializes to zero and ramps gradually
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

            // Test 21: v5.0.1 â€” Delta clamp prevents >5 MW/step jumps
            // Start with a large instantaneous Q difference (cold SG, hot RCS)
            // and verify the output is clamped.
            state = Initialize(100f);
            // Run a baseline step to initialize the clamp tracker
            Update(ref state, 120f, 4, 400f, 1f / 360f);
            float baselineQ = state.TotalHeatAbsorption_MW;
            // Now force a massive driving Î”T increase by jumping T_rcs
            var result21 = Update(ref state, 500f, 4, 2235f, 1f / 360f);
            float deltaQ21 = Math.Abs(state.TotalHeatAbsorption_MW - baselineQ);
            if (deltaQ21 > DELTA_Q_CLAMP_MW + 0.1f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 21 FAIL: |Î”Q| = {deltaQ21:F2} MW " +
                                $"(expected â‰¤ {DELTA_Q_CLAMP_MW} MW from delta clamp)");
                valid = false;
            }

            // Test 22: v5.1.0 Stage 2 â€” boilingâ†’subcooled reversion guard
            // Once pressure has risen above Nâ‚‚ blanket (boiling active), a transient
            // dip of T_hottest just below T_sat must NOT snap pressure back to 17 psia.
            // Instead, pressure should continue tracking P_sat(T_hottest) downward.
            state = Initialize(100f);
            state.NitrogenIsolated = true;
            state.NodeTemperatures[0] = 350f;  // Force boiling, P_sat(350) â‰ˆ 135 psia
            Update(ref state, 400f, 4, 500f, 1f / 360f);
            float P_after_boiling = state.SecondaryPressure_psia;  // Should be ~135 psia
            // Now cool the hottest node to just below current T_sat
            // T_sat at 135 psia â‰ˆ 350Â°F, so set to 348Â°F (just below)
            float T_sat_at_current_P = WaterProperties.SaturationTemperature(P_after_boiling);
            state.NodeTemperatures[0] = T_sat_at_current_P - 2f;
            Update(ref state, 400f, 4, 500f, 1f / 360f);
            // Pressure should track P_sat(348Â°F) â‰ˆ ~131 psia, NOT snap to 17 psia
            if (state.SecondaryPressure_psia < 50f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 22 FAIL: Pressure snapped to " +
                                $"{state.SecondaryPressure_psia:F1} psia after T_hottest dipped below T_sat " +
                                $"(expected ~P_sat({T_sat_at_current_P - 2f:F0}Â°F), not 17 psia)");
                valid = false;
            }

            // Test 23: v5.1.0 Stage 3 â€” steam line warming model
            // During boiling, cold steam lines should absorb condensation energy.
            // After several steps, steam line temperature should rise above initial.
            state = Initialize(100f);
            state.NitrogenIsolated = true;
            state.NodeTemperatures[0] = 300f;  // Force boiling
            float steamLineInitial = state.SteamLineTempF;  // Should be 100Â°F
            // Run 50 steps to let condensation warm the steam lines
            for (int step = 0; step < 50; step++)
            {
                Update(ref state, 350f, 4, 500f, 1f / 360f);
            }
            if (state.SteamLineTempF <= steamLineInitial + 1f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 23 FAIL: Steam line did not warm " +
                                $"(initial={steamLineInitial:F1}Â°F, after 50 steps={state.SteamLineTempF:F1}Â°F)");
                valid = false;
            }
            // Steam line should still be below T_sat (cap enforced)
            float T_sat_23 = state.SaturationTemp_F;
            if (state.SteamLineTempF > T_sat_23 + 1f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 23 FAIL: Steam line T={state.SteamLineTempF:F1}Â°F " +
                                $"exceeds T_sat={T_sat_23:F1}Â°F");
                valid = false;
            }

            // Test 24: v5.1.0 Stage 3 â€” steam line initialization
            state = Initialize(150f);
            if (Math.Abs(state.SteamLineTempF - 150f) > 0.01f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 24 FAIL: Steam line init T={state.SteamLineTempF:F1}Â°F " +
                                $"(expected 150Â°F)");
                valid = false;
            }
            if (state.SteamLineCondensationRate_BTUhr != 0f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 24 FAIL: Condensation rate={state.SteamLineCondensationRate_BTUhr:F0} " +
                                $"at init (expected 0)");
                valid = false;
            }

            // Test 25: v5.1.0 Stage 4 â€” wall temperature bounds during boiling
            // T_wall must satisfy: T_sat â‰¤ T_wall â‰¤ T_rcs
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
                Debug.LogWarning($"[SGMultiNode Validation] Test 25 FAIL: T_wall={T_wall_diag:F1}Â°F " +
                                $"< T_sat={T_sat_25:F1}Â°F");
                valid = false;
            }
            if (T_wall_diag > 400.1f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 25 FAIL: T_wall={T_wall_diag:F1}Â°F " +
                                $"> T_rcs=400Â°F");
                valid = false;
            }
            if (T_wall_diag >= 399.9f)
            {
                Debug.LogWarning($"[SGMultiNode Validation] Test 25 WARN: T_wall={T_wall_diag:F1}Â°F â‰ˆ T_rcs " +
                                $"(no wall drop detected)");
            }

            // Test 26: v5.1.0 Stage 4 â€” primary HTC constant sanity
            if (PlantConstants.SG_PRIMARY_FORCED_CONVECTION_HTC <= 0f)
            {
                Debug.LogWarning("[SGMultiNode Validation] Test 26 FAIL: SG_PRIMARY_FORCED_CONVECTION_HTC â‰¤ 0");
                valid = false;
            }

            if (valid)
                Debug.Log("[SGMultiNode] All validation tests PASSED (v5.1.0 â€” saturation + steam line + wall superheat)");
            else
                Debug.LogError("[SGMultiNode] Validation FAILED â€” check warnings above");

            return valid;
        }

        #endregion
    }
}
