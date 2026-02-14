using System;
using Critical.Physics;
using UnityEditor;
using UnityEngine;

namespace Critical.Validation
{
    /// <summary>
    /// IP-0018 component-level gate checks.
    /// </summary>
    public static class IP0018Checks
    {
        [MenuItem("Critical/Run Stage C (IP-0018)")]
        public static void RunStageC()
        {
            Debug.Log("[IP-0018][StageC] Running SG component validation checks.");

            SGMultiNodeState state = SGMultiNodeThermal.Initialize(100f);
            const float dtHr = 1f / 360f;
            for (int i = 0; i < 180; i++)
            {
                SGMultiNodeResult result = SGMultiNodeThermal.Update(
                    ref state,
                    T_rcs: 240f,
                    rcpsRunning: 2,
                    pressurePsia: 400f,
                    dt_hr: dtHr);

                if (!IsFinite(result.TotalHeatRemoval_MW) ||
                    !IsFinite(result.SecondaryPressure_psia) ||
                    !IsFinite(result.SaturationTemp_F) ||
                    !IsFinite(result.TopNodeTemp_F))
                {
                    throw new Exception("[IP-0018][StageC] Non-finite SG state detected.");
                }

                if (result.SecondaryPressure_psia < PlantConstants.SG_MIN_STEAMING_PRESSURE_PSIA - 0.1f)
                {
                    throw new Exception(
                        $"[IP-0018][StageC] SG pressure below floor: {result.SecondaryPressure_psia:F3} psia.");
                }
            }

            if (state.SecondaryPressure_psia < PlantConstants.SG_MIN_STEAMING_PRESSURE_PSIA - 0.1f)
            {
                throw new Exception(
                    $"[IP-0018][StageC] Final SG pressure below floor: {state.SecondaryPressure_psia:F3} psia.");
            }

            Debug.Log("[IP-0018][StageC] PASS");
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
