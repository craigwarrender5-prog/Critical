// CRITICAL: Master the Atom - Physics Module
// SGMultiNodeThermal.ControlAPI.cs - Public SG control operations
//
// File: Assets/Scripts/Physics/SGMultiNodeThermal.ControlAPI.cs
// Module: Critical.Physics.SGMultiNodeThermal
// Responsibility: SG operator/control entry points and diagnostics.
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
        /// Start SG draining from wet layup to operating level.
        /// Called by HeatupSimEngine when T_rcs reaches SG_DRAINING_START_TEMP_F.
        /// Draining proceeds automatically until target mass fraction is reached.
        ///
        /// Per NRC HRTD 2.3 / 19.0: "At approximately 200Â°F, SG draining is
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
            bool wasIsolated = state.SteamIsolated;
            state.SteamIsolated = isolated;

            // Reset only on an actual isolated->open transition.
            // Calling this each step with OPEN should not wipe inventory.
            if (wasIsolated && !isolated)
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
                s += $"  {label}: T={state.NodeTemperatures[i]:F1}Â°F  Î”T={dT:F1}Â°F  " +
                     $"Q={state.NodeHeatRates[i] / MW_TO_BTU_HR:F3}MW  " +
                     $"h={state.NodeHTCs[i]:F0}  Af={state.NodeEffectiveAreaFractions[i]:F3}  " +
                     $"[{posStr}]{boilMark}{blendStr}\n";
            }
            return s;
        }

        #endregion
    }
}
