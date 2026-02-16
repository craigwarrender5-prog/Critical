using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Critical.Physics;

namespace Critical.Validation
{
    /// <summary>
    /// Editor-only Stage E runner for IP-0015 validation.
    /// Executes the real HeatupSimEngine physics step path in a deterministic loop,
    /// writes interval/report logs, and emits a Stage E evidence markdown.
    /// </summary>
    public static class StageERunner
    {
        private const float DtHr = 1f / 360f;         // 10-second sim timestep
        private const float IntervalLogHr = 0.25f;    // 15-minute interval logs
        private const float MaxSimHr = 18f;           // Stage E guard rail
        private const int MaxSteps = 250000;
        private const float FloorDepartureMarginPsia = 1.0f;
        private const float SteamInventoryPassLb = 1.0f;
        private const float MassErrorAlarmLb = 500f;

        [MenuItem("Critical/Run Stage E (IP-0015)")]
        public static void RunStageE()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string scenePath = "Assets/Scenes/MainScene.unity";
            string logDir = Path.Combine(projectRoot, "HeatupLogs");
            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string evidencePath = Path.Combine(
                projectRoot,
                "Updates",
                "Issues",
                $"IP-0015_StageE_Rerun_{stamp}.md");

            try
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                HeatupSimEngine engine = UnityEngine.Object.FindObjectOfType<HeatupSimEngine>();
                if (engine == null)
                    throw new InvalidOperationException("HeatupSimEngine not found in MainScene.");

                Type engineType = typeof(HeatupSimEngine);
                MethodInfo init = engineType.GetMethod(
                    "InitializeSimulation",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo step = engineType.GetMethod(
                    "StepSimulation",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo saveInterval = engineType.GetMethod(
                    "SaveIntervalLog",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo saveReport = engineType.GetMethod(
                    "SaveReport",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo logPathField = engineType.GetField(
                    "logPath",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (init == null || step == null || saveInterval == null || saveReport == null || logPathField == null)
                {
                    throw new MissingMethodException(
                        "Required HeatupSimEngine private methods/fields were not found for Stage E automation.");
                }

                PrepareLogDirectory(logDir);

                // Force deterministic startup profile used by Stage E.
                engine.runOnStart = false;
                engine.coldShutdownStart = true;
                engine.startTemperature = 100f;
                engine.targetTemperature = 557f;
                logPathField.SetValue(engine, logDir);

                init.Invoke(engine, null);
                saveInterval.Invoke(engine, null); // t=0 baseline

                float nextIntervalLogHr = IntervalLogHr;
                int steps = 0;

                float firstBoilingHr = -1f;
                float firstIsolatedHr = -1f;
                float firstPressureRiseHr = -1f;
                float maxPressureIsolatedBoiling_psia = float.MinValue;
                float maxSteamInventoryIsolated_lb = 0f;
                float minNetPlantHeatStartup_MW = float.MaxValue;
                float minRcsHeatRatePostBoiling_Fhr = float.MaxValue;
                float maxMassError_lbm = 0f;

                int isolatedBoilingSamples = 0;
                int startupWindowSamples = 0;
                int postBoilingSamples = 0;

                while (steps < MaxSteps
                    && engine.simTime < MaxSimHr
                    && engine.T_rcs < engine.targetTemperature - 2f)
                {
                    step.Invoke(engine, new object[] { DtHr });
                    steps++;

                    bool isolated = string.Equals(engine.sgBoundaryMode, "ISOLATED", StringComparison.OrdinalIgnoreCase);
                    bool boiling = engine.sgBoilingActive;

                    if (boiling && firstBoilingHr < 0f)
                        firstBoilingHr = engine.simTime;
                    if (isolated && firstIsolatedHr < 0f)
                        firstIsolatedHr = engine.simTime;

                    if (isolated && boiling)
                    {
                        isolatedBoilingSamples++;
                        maxPressureIsolatedBoiling_psia = Mathf.Max(
                            maxPressureIsolatedBoiling_psia, engine.sgSecondaryPressure_psia);
                        maxSteamInventoryIsolated_lb = Mathf.Max(
                            maxSteamInventoryIsolated_lb, engine.sgSteamInventory_lb);

                        if (firstPressureRiseHr < 0f &&
                            engine.sgSecondaryPressure_psia >
                            PlantConstants.SG_INITIAL_PRESSURE_PSIA + FloorDepartureMarginPsia)
                        {
                            firstPressureRiseHr = engine.simTime;
                        }
                    }

                    bool inStartupWindow = engine.T_rcs >= PlantConstants.SG_NITROGEN_ISOLATION_TEMP_F
                        && engine.T_rcs < engine.targetTemperature - 2f;
                    if (inStartupWindow)
                    {
                        startupWindowSamples++;
                        minNetPlantHeatStartup_MW = Mathf.Min(
                            minNetPlantHeatStartup_MW, engine.netPlantHeat_MW);
                    }

                    if (firstBoilingHr >= 0f)
                    {
                        postBoilingSamples++;
                        minRcsHeatRatePostBoiling_Fhr = Mathf.Min(
                            minRcsHeatRatePostBoiling_Fhr, engine.rcsHeatRate);
                    }

                    maxMassError_lbm = Mathf.Max(maxMassError_lbm, engine.massError_lbm);

                    if (engine.simTime >= nextIntervalLogHr)
                    {
                        saveInterval.Invoke(engine, null);
                        nextIntervalLogHr += IntervalLogHr;
                    }
                }

                saveReport.Invoke(engine, null);

                bool passPressureRise = isolatedBoilingSamples > 0 &&
                                        maxPressureIsolatedBoiling_psia >
                                        PlantConstants.SG_INITIAL_PRESSURE_PSIA + FloorDepartureMarginPsia;
                bool passInventory = isolatedBoilingSamples > 0 &&
                                     maxSteamInventoryIsolated_lb > SteamInventoryPassLb;
                bool passNetHeat = startupWindowSamples > 0 &&
                                   minNetPlantHeatStartup_MW > 0f;
                bool passRcsProgress = postBoilingSamples > 0 &&
                                       minRcsHeatRatePostBoiling_Fhr > 0f;
                bool passConservation = maxMassError_lbm <= MassErrorAlarmLb;

                bool overallPass = passPressureRise && passInventory
                    && passNetHeat && passRcsProgress && passConservation;

                WriteEvidenceFile(
                    evidencePath,
                    engine,
                    steps,
                    isolatedBoilingSamples,
                    startupWindowSamples,
                    postBoilingSamples,
                    firstBoilingHr,
                    firstIsolatedHr,
                    firstPressureRiseHr,
                    maxPressureIsolatedBoiling_psia,
                    maxSteamInventoryIsolated_lb,
                    minNetPlantHeatStartup_MW,
                    minRcsHeatRatePostBoiling_Fhr,
                    maxMassError_lbm,
                    passPressureRise,
                    passInventory,
                    passNetHeat,
                    passRcsProgress,
                    passConservation,
                    overallPass);

                Debug.Log($"[StageE] Evidence written: {evidencePath}");

                if (!overallPass)
                {
                    throw new Exception("Stage E criteria failed. See evidence markdown for details.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StageE] Run failed: {ex.Message}");
                throw;
            }
        }

        private static void PrepareLogDirectory(string logDir)
        {
            Directory.CreateDirectory(logDir);
            foreach (string file in Directory.GetFiles(logDir, "*.txt"))
            {
                File.Delete(file);
            }
        }

        private static void WriteEvidenceFile(
            string evidencePath,
            HeatupSimEngine engine,
            int steps,
            int isolatedBoilingSamples,
            int startupWindowSamples,
            int postBoilingSamples,
            float firstBoilingHr,
            float firstIsolatedHr,
            float firstPressureRiseHr,
            float maxPressureIsolatedBoiling_psia,
            float maxSteamInventoryIsolated_lb,
            float minNetPlantHeatStartup_MW,
            float minRcsHeatRatePostBoiling_Fhr,
            float maxMassError_lbm,
            bool passPressureRise,
            bool passInventory,
            bool passNetHeat,
            bool passRcsProgress,
            bool passConservation,
            bool overallPass)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(evidencePath) ?? ".");

            var sb = new StringBuilder();
            sb.AppendLine("# IP-0015 Stage E Rerun Evidence");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Scenario: Full cold startup heat-up (MainScene, deterministic StepSimulation loop)");
            sb.AppendLine($"- Steps executed: {steps}");
            sb.AppendLine($"- Final sim time: {engine.simTime:F2} hr");
            sb.AppendLine($"- Final T_rcs: {engine.T_rcs:F2} F");
            sb.AppendLine($"- Final SG pressure: {engine.sgSecondaryPressure_psia:F2} psia");
            sb.AppendLine();
            sb.AppendLine("## Metrics");
            sb.AppendLine($"- Isolated boiling samples: {isolatedBoilingSamples}");
            sb.AppendLine($"- Startup window samples: {startupWindowSamples}");
            sb.AppendLine($"- Post-boiling samples: {postBoilingSamples}");
            sb.AppendLine($"- First boiling time: {(firstBoilingHr >= 0f ? $"{firstBoilingHr:F2} hr" : "not reached")}");
            sb.AppendLine($"- First isolated time: {(firstIsolatedHr >= 0f ? $"{firstIsolatedHr:F2} hr" : "not reached")}");
            sb.AppendLine($"- First pressure rise above floor: {(firstPressureRiseHr >= 0f ? $"{firstPressureRiseHr:F2} hr" : "not reached")}");
            sb.AppendLine($"- Max SG pressure during isolated boiling: {maxPressureIsolatedBoiling_psia:F2} psia");
            sb.AppendLine($"- Max steam inventory during isolated boiling: {maxSteamInventoryIsolated_lb:F2} lb");
            sb.AppendLine($"- Min net plant heat in startup window: {minNetPlantHeatStartup_MW:F4} MW");
            sb.AppendLine($"- Min RCS heat rate post-boiling: {minRcsHeatRatePostBoiling_Fhr:F4} F/hr");
            sb.AppendLine($"- Max mass error observed: {maxMassError_lbm:F2} lbm");
            sb.AppendLine();
            sb.AppendLine("## Required Criteria");
            sb.AppendLine($"- SG pressure departs atmospheric floor during isolated boiling: {(passPressureRise ? "PASS" : "FAIL")}");
            sb.AppendLine($"- Steam inventory accumulates when isolated: {(passInventory ? "PASS" : "FAIL")}");
            sb.AppendLine($"- Net plant heat remains positive during intended startup segment: {(passNetHeat ? "PASS" : "FAIL")}");
            sb.AppendLine($"- RCS heat-up no longer stalls post-boiling: {(passRcsProgress ? "PASS" : "FAIL")}");
            sb.AppendLine($"- No new conservation regressions: {(passConservation ? "PASS" : "FAIL")}");
            sb.AppendLine();
            sb.AppendLine($"## Overall Stage E Result: {(overallPass ? "PASS" : "FAIL")}");
            sb.AppendLine();
            sb.AppendLine("## Artifacts");
            sb.AppendLine("- Heatup interval logs: `HeatupLogs/*.txt`");
            sb.AppendLine("- Heatup report: `HeatupLogs/Heatup_Report_*.txt`");

            File.WriteAllText(evidencePath, sb.ToString());
        }
    }
}
