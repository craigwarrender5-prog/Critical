using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Critical.Validation
{
    /// <summary>
    /// Editor-only Stage E runner for IP-0018 threshold validation.
    /// </summary>
    public static class IP0018StageERunner
    {
        private const float DtHr = 1f / 360f;
        private const float IntervalLogHr = 0.25f;
        private const float MaxSimHr = 18f;
        private const int MaxSteps = 250000;

        private sealed class IntervalSample
        {
            public float TimeHr;
            public float PrimaryHeatInputMW;
            public float SGHeatRemovalMW;
            public float PressurePsia;
            public float TsatF;
            public float TopTempF;
            public bool BoilingActive;
            public string StartupState = string.Empty;
            public float HoldPressureDeviationPct;
            public float HoldLeakPct;
        }

        private sealed class CsResult
        {
            public string CsId = string.Empty;
            public string Title = string.Empty;
            public bool Pass;
            public readonly List<string> Failures = new List<string>();
            public readonly List<string> Observations = new List<string>();
            public string SuspectedRootCause = "Unknown";
        }

        [MenuItem("Critical/Run Stage E (IP-0018 Thresholds)")]
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
                $"IP-0018_StageE_Validation_{stamp}.md");

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
                        "Required HeatupSimEngine methods/fields were not found for IP-0018 Stage E.");
                }

                PrepareLogDirectory(logDir);

                engine.runOnStart = false;
                engine.coldShutdownStart = true;
                engine.startTemperature = 100f;
                engine.targetTemperature = 557f;
                logPathField.SetValue(engine, logDir);

                init.Invoke(engine, null);
                saveInterval.Invoke(engine, null);

                var samples = new List<IntervalSample>();
                float nextIntervalHr = IntervalLogHr;
                int steps = 0;

                while (steps < MaxSteps
                    && engine.simTime < MaxSimHr
                    && engine.T_rcs < engine.targetTemperature - 2f)
                {
                    step.Invoke(engine, new object[] { DtHr });
                    steps++;

                    if (engine.simTime >= nextIntervalHr)
                    {
                        saveInterval.Invoke(engine, null);
                        samples.Add(CaptureSample(engine));
                        nextIntervalHr += IntervalLogHr;
                    }
                }

                saveReport.Invoke(engine, null);
                string reportPath = Directory.GetFiles(logDir, "Heatup_Report_*.txt")
                    .OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault() ?? string.Empty;

                CsResult cs0017 = EvaluateCs0017(samples);
                CsResult cs0019 = EvaluateCs0019(samples);
                CsResult cs0020 = EvaluateCs0020(engine);
                CsResult cs0009 = EvaluateCs0009(engine, reportPath);

                var results = new List<CsResult> { cs0017, cs0019, cs0020, cs0009 };
                bool overallPass = results.All(r => r.Pass);

                WriteEvidenceFile(
                    evidencePath,
                    engine,
                    reportPath,
                    steps,
                    samples.Count,
                    results,
                    overallPass);

                Debug.Log($"[IP-0018][StageE] Evidence written: {evidencePath}");

                if (!overallPass)
                {
                    throw new Exception("IP-0018 Stage E criteria failed. See evidence markdown for threshold failures.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IP-0018][StageE] Run failed: {ex.Message}");
                throw;
            }
        }

        private static IntervalSample CaptureSample(HeatupSimEngine engine)
        {
            return new IntervalSample
            {
                TimeHr = engine.simTime,
                PrimaryHeatInputMW = engine.stageE_PrimaryHeatInput_MW,
                SGHeatRemovalMW = engine.stageE_LastSGHeatRemoval_MW,
                PressurePsia = engine.sgSecondaryPressure_psia,
                TsatF = engine.sgSaturationTemp_F,
                TopTempF = engine.sgTopNodeTemp,
                BoilingActive = engine.sgBoilingActive,
                StartupState = engine.sgStartupBoundaryState ?? string.Empty,
                HoldPressureDeviationPct = engine.sgHoldPressureDeviation_pct,
                HoldLeakPct = engine.sgHoldNetLeakage_pct
            };
        }

        private static CsResult EvaluateCs0017(List<IntervalSample> samples)
        {
            var result = new CsResult
            {
                CsId = "CS-0017",
                Title = "Pressurize/Hold startup state enforcement",
                SuspectedRootCause = "HeatupSimEngine SG startup boundary state machine"
            };

            int maxPressurizeRun = GetMaxConsecutive(samples, "PRESSURIZE");
            int maxHoldRun = GetMaxConsecutive(samples, "HOLD");

            result.Observations.Add($"Max PRESSURIZE run: {maxPressurizeRun} intervals");
            result.Observations.Add($"Max HOLD run: {maxHoldRun} intervals");

            if (maxPressurizeRun < 2)
                result.Failures.Add($"Pressurize duration < 2 intervals (observed {maxPressurizeRun}).");
            if (maxHoldRun < 2)
                result.Failures.Add($"Hold duration < 2 intervals (observed {maxHoldRun}).");

            var holdSamples = samples
                .Where(s => string.Equals(s.StartupState, "HOLD", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (holdSamples.Any(s => Mathf.Abs(s.HoldPressureDeviationPct) > 3f))
            {
                float worst = holdSamples.Max(s => Mathf.Abs(s.HoldPressureDeviationPct));
                result.Failures.Add($"Hold pressure drift exceeded +/-3% (observed {worst:F3}%).");
            }

            float maxStepLeakPct = 0f;
            for (int i = 1; i < holdSamples.Count; i++)
            {
                float stepLeak = Mathf.Max(0f, holdSamples[i].HoldLeakPct - holdSamples[i - 1].HoldLeakPct);
                maxStepLeakPct = Mathf.Max(maxStepLeakPct, stepLeak);
            }

            float cumulativeLeakPct = holdSamples.Count > 0
                ? holdSamples.Max(s => s.HoldLeakPct)
                : 0f;
            result.Observations.Add($"Hold max step leak: {maxStepLeakPct:F3}%");
            result.Observations.Add($"Hold cumulative leak: {cumulativeLeakPct:F3}%");

            if (maxStepLeakPct > 0.1f)
                result.Failures.Add($"Hold interval leakage exceeded 0.1% (observed {maxStepLeakPct:F3}%).");
            if (cumulativeLeakPct > 0.2f)
                result.Failures.Add($"Hold cumulative leakage exceeded 0.2% (observed {cumulativeLeakPct:F3}%).");

            result.Pass = result.Failures.Count == 0;
            return result;
        }

        private static CsResult EvaluateCs0019(List<IntervalSample> samples)
        {
            var result = new CsResult
            {
                CsId = "CS-0019",
                Title = "Secondary Tsat progression during pressurization",
                SuspectedRootCause = "HeatupSimEngine pressurization telemetry and SG pressure/Tsat coupling"
            };

            int firstPressurize = samples.FindIndex(
                s => string.Equals(s.StartupState, "PRESSURIZE", StringComparison.OrdinalIgnoreCase));
            if (firstPressurize < 0)
            {
                result.Failures.Add("Pressurization window not detected.");
                result.Pass = false;
                return result;
            }

            int firstBoiling = samples.FindIndex(firstPressurize, s => s.BoilingActive);
            int windowEnd = firstBoiling >= 0 ? firstBoiling : samples.Count - 1;
            if (windowEnd < firstPressurize)
            {
                result.Failures.Add("Pressurization window bounds are invalid.");
                result.Pass = false;
                return result;
            }

            List<IntervalSample> window = samples.GetRange(firstPressurize, windowEnd - firstPressurize + 1);
            if (window.Count < 4)
            {
                result.Failures.Add($"Insufficient pressurization samples (observed {window.Count}, need >= 4).");
                result.Pass = false;
                return result;
            }

            int maxPressureRiseStreak = GetRiseStreak(window, s => s.PressurePsia);
            int maxTsatRiseStreak = GetRiseStreak(window, s => s.TsatF);
            float netPressureGain = window[window.Count - 1].PressurePsia - window[0].PressurePsia;
            float tsatDelta = window[window.Count - 1].TsatF - window[0].TsatF;

            float preBoilApproach = float.MaxValue;
            for (int i = 0; i < window.Count; i++)
            {
                if (window[i].BoilingActive)
                    break;
                float approach = Mathf.Abs(window[i].TopTempF - window[i].TsatF);
                preBoilApproach = Mathf.Min(preBoilApproach, approach);
            }
            if (preBoilApproach == float.MaxValue)
                preBoilApproach = Mathf.Abs(window[window.Count - 1].TopTempF - window[window.Count - 1].TsatF);

            result.Observations.Add($"Max pressure rise streak: {maxPressureRiseStreak}");
            result.Observations.Add($"Max Tsat rise streak: {maxTsatRiseStreak}");
            result.Observations.Add($"Pressurization net dP: {netPressureGain:F3} psia");
            result.Observations.Add($"Pressurization Tsat dT: {tsatDelta:F3} F");
            result.Observations.Add($"Pre-boil |T-Tsat| minimum: {preBoilApproach:F3} F");

            if (maxPressureRiseStreak < 3)
                result.Failures.Add($"Pressure monotonic streak < 3 intervals (observed {maxPressureRiseStreak}).");
            if (maxTsatRiseStreak < 3)
                result.Failures.Add($"Tsat monotonic streak < 3 intervals (observed {maxTsatRiseStreak}).");
            if (preBoilApproach > 15f)
                result.Failures.Add($"Secondary top temperature did not approach within 15F of Tsat (observed {preBoilApproach:F3}F).");
            if (tsatDelta <= 5f)
                result.Failures.Add($"Tsat remained within 5F of pressurization start (observed dT {tsatDelta:F3}F).");
            if (netPressureGain <= 0f)
                result.Failures.Add($"Pressurization showed no net pressure gain (observed dP {netPressureGain:F3} psia).");

            result.Pass = result.Failures.Count == 0;
            return result;
        }

        private static CsResult EvaluateCs0020(HeatupSimEngine engine)
        {
            var result = new CsResult
            {
                CsId = "CS-0020",
                Title = "Dynamic SG secondary response under active heat input",
                SuspectedRootCause = "HeatupSimEngine dynamic-response telemetry and SG coupling path"
            };

            result.Observations.Add($"Primary-rise checks: {engine.stageE_DynamicPrimaryRiseCheckCount}");
            result.Observations.Add($"Primary-rise pass/fail: {engine.stageE_DynamicPrimaryRisePassCount}/{engine.stageE_DynamicPrimaryRiseFailCount}");
            result.Observations.Add($"dT(3 intervals) windows: {engine.stageE_DynamicTempDelta3WindowCount}");
            result.Observations.Add($"dT(3 intervals) last/min/max: {engine.stageE_DynamicTempDelta3Last_F:F3}/{engine.stageE_DynamicTempDelta3Min_F:F3}/{engine.stageE_DynamicTempDelta3Max_F:F3} F");
            result.Observations.Add($"dT(3) >5F count: {engine.stageE_DynamicTempDelta3Above5Count}");
            result.Observations.Add($"dT(3) <2F count: {engine.stageE_DynamicTempDelta3Below2Count}");
            result.Observations.Add($"Pressure flatline(3) count: {engine.stageE_DynamicPressureFlatline3Count}");
            result.Observations.Add($"Hard-clamp hits: {engine.stageE_DynamicHardClampViolationCount}");

            if (engine.stageE_DynamicPrimaryRiseCheckCount <= 0)
                result.Failures.Add("No qualifying primary-heat increase intervals were observed (>2%).");
            if (engine.stageE_DynamicPrimaryRiseFailCount > 0)
                result.Failures.Add($"Pressure did not increase on {engine.stageE_DynamicPrimaryRiseFailCount} qualifying primary-heat rises.");
            if (engine.stageE_DynamicTempDelta3Above5Count <= 0)
                result.Failures.Add("No active-heating 3-interval temperature delta exceeded 5F.");
            if (engine.stageE_DynamicTempDelta3Below2Count > 0)
                result.Failures.Add($"Temperature delta <2F occurred in {engine.stageE_DynamicTempDelta3Below2Count} active-heating 3-interval windows.");
            if (engine.stageE_DynamicPressureFlatline3Count > 0)
                result.Failures.Add($"Pressure flatline (|dP|<=0.1 psia over 3 intervals) occurred {engine.stageE_DynamicPressureFlatline3Count} time(s).");
            if (engine.stageE_DynamicHardClampViolationCount > 0)
                result.Failures.Add($"Hard-clamp signature near Tsat-50F outside HOLD occurred {engine.stageE_DynamicHardClampViolationCount} time(s).");

            result.Pass = result.Failures.Count == 0;
            return result;
        }

        private static CsResult EvaluateCs0009(HeatupSimEngine engine, string reportPath)
        {
            var result = new CsResult
            {
                CsId = "CS-0009",
                Title = "SG energy validation and mismatch threshold enforcement",
                SuspectedRootCause = "HeatupSimEngine energy telemetry integration and SG heat accounting"
            };

            result.Observations.Add($"TotalPrimaryEnergy_MJ: {engine.stageE_TotalPrimaryEnergy_MJ:F3}");
            result.Observations.Add($"TotalSGEnergyRemoved_MJ: {engine.stageE_TotalSGEnergyRemoved_MJ:F3}");
            result.Observations.Add($"PercentMismatch: {engine.stageE_PercentMismatch:F3}%");
            result.Observations.Add($"Energy samples: {engine.stageE_EnergySampleCount}");
            result.Observations.Add($"Negative SG heat violations: {engine.stageE_EnergyNegativeViolationCount}");
            result.Observations.Add($">5% over-primary violations: {engine.stageE_EnergyOverPrimaryViolationCount}");
            result.Observations.Add($"Max over-primary percent: {engine.stageE_EnergyMaxOverPrimaryPct:F3}%");

            if (engine.stageE_EnergySampleCount <= 0)
                result.Failures.Add("No startup-window energy samples were captured.");
            if (engine.stageE_EnergyNegativeViolationCount > 0)
                result.Failures.Add($"Negative SG heat removal observed {engine.stageE_EnergyNegativeViolationCount} time(s).");
            if (engine.stageE_EnergyOverPrimaryViolationCount > 0)
                result.Failures.Add($"SG heat removal exceeded 105% of primary input {engine.stageE_EnergyOverPrimaryViolationCount} time(s).");
            if (engine.stageE_PercentMismatch < -2f || engine.stageE_PercentMismatch > 2f)
            {
                result.Failures.Add(
                    $"Percent mismatch outside +/-2% (observed {engine.stageE_PercentMismatch:F3}%).");
            }

            if (!ReportContainsRequiredEnergyFields(reportPath))
            {
                result.Failures.Add("Required energy artifact fields missing from Heatup report.");
            }

            result.Pass = result.Failures.Count == 0;
            return result;
        }

        private static int GetMaxConsecutive(List<IntervalSample> samples, string state)
        {
            int maxRun = 0;
            int run = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                if (string.Equals(samples[i].StartupState, state, StringComparison.OrdinalIgnoreCase))
                {
                    run++;
                    maxRun = Math.Max(maxRun, run);
                }
                else
                {
                    run = 0;
                }
            }
            return maxRun;
        }

        private static int GetRiseStreak(List<IntervalSample> samples, Func<IntervalSample, float> selector)
        {
            int maxRun = 0;
            int run = 0;
            for (int i = 1; i < samples.Count; i++)
            {
                if (selector(samples[i]) > selector(samples[i - 1]))
                {
                    run++;
                    maxRun = Math.Max(maxRun, run);
                }
                else
                {
                    run = 0;
                }
            }
            return maxRun;
        }

        private static bool ReportContainsRequiredEnergyFields(string reportPath)
        {
            if (string.IsNullOrWhiteSpace(reportPath) || !File.Exists(reportPath))
                return false;

            string text = File.ReadAllText(reportPath);
            return text.Contains("TotalPrimaryEnergy_MJ", StringComparison.Ordinal)
                && text.Contains("TotalSGEnergyRemoved_MJ", StringComparison.Ordinal)
                && text.Contains("PercentMismatch", StringComparison.Ordinal);
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
            string reportPath,
            int steps,
            int intervals,
            List<CsResult> results,
            bool overallPass)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(evidencePath) ?? ".");
            var sb = new StringBuilder();

            sb.AppendLine("# IP-0018 Stage E Validation Evidence");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("- Domain: DP-0003");
            sb.AppendLine($"- Steps executed: {steps}");
            sb.AppendLine($"- Interval samples: {intervals}");
            sb.AppendLine($"- Final sim time: {engine.simTime:F3} hr");
            sb.AppendLine($"- Final T_rcs: {engine.T_rcs:F3} F");
            sb.AppendLine($"- Heatup report: `{reportPath}`");
            sb.AppendLine();
            sb.AppendLine("## CS PASS/FAIL");

            foreach (CsResult r in results)
            {
                sb.AppendLine($"### {r.CsId} - {r.Title}: {(r.Pass ? "PASS" : "FAIL")}");
                sb.AppendLine($"- Suspected root cause area: {r.SuspectedRootCause}");
                foreach (string observation in r.Observations)
                    sb.AppendLine($"- Observed: {observation}");

                if (r.Failures.Count == 0)
                {
                    sb.AppendLine("- Threshold result: all criteria satisfied.");
                }
                else
                {
                    foreach (string failure in r.Failures)
                        sb.AppendLine($"- Failed threshold: {failure}");
                }

                sb.AppendLine();
            }

            sb.AppendLine($"## Overall Stage E Result: {(overallPass ? "PASS" : "FAIL")}");
            if (!overallPass)
            {
                sb.AppendLine();
                sb.AppendLine("## Failure Report");
                foreach (CsResult r in results.Where(x => !x.Pass))
                {
                    foreach (string failure in r.Failures)
                    {
                        sb.AppendLine($"- CS ID: {r.CsId}");
                        sb.AppendLine($"  Failed threshold: {failure}");
                        sb.AppendLine($"  Suspected root cause area: {r.SuspectedRootCause}");
                    }
                }
            }

            File.WriteAllText(evidencePath, sb.ToString());
        }
    }
}
