using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using Critical.Validation;
namespace Critical.Validation
{
    public static class IP0028ExecutionRunner
    {
        private const float DtHr = 1f / 360f; // 10 s
        private const float MaxSimHr = 12f;
        private const float IntervalLogHr = 0.25f;
        private const int MaxSteps = 600000;

        private sealed class StepSample
        {
            public float TimeHr;
            public float PressurePsia;
            public float PzrLevelPct;
            public float HeaterPowerMw;
            public float PressureRatePsiHr;
            public string HeaterMode = string.Empty;
            public bool HoldActive;
            public float HoldElapsedSec;
            public bool HoldTimeGate;
            public bool HoldPressureGate;
            public bool HoldStateGate;
            public string HoldBlockReason = string.Empty;
            public string AuthorityState = string.Empty;
            public string AuthorityReason = string.Empty;
            public string LimiterReason = string.Empty;
            public string LimiterDetail = string.Empty;
            public bool PressureRateClamp;
            public bool RampRateClamp;
            public string SolidControlMode = string.Empty;
            public bool SolidInBand;
            public bool SprayActive;
            public float SprayFlowGpm;
            public string BubblePhase = string.Empty;
            public bool ClosureConverged;
            public float ClosureVolumeResidualFt3;
            public float ClosureEnergyResidualBtu;
            public float RvlisFull;
            public float RvlisUpper;
            public bool RvlisFullValid;
            public bool RvlisUpperValid;
        }

        private sealed class EventSample
        {
            public float TimeHr;
            public string Severity = string.Empty;
            public string Message = string.Empty;
        }

        private sealed class RunResult
        {
            public string Label = string.Empty;
            public string LogDir = string.Empty;
            public string StepCsvPath = string.Empty;
            public string EventCsvPath = string.Empty;
            public string SummaryPath = string.Empty;
            public string ReportPath = string.Empty;
            public readonly List<StepSample> Steps = new List<StepSample>();
            public readonly List<EventSample> Events = new List<EventSample>();
            public float HoldReleaseTimeHr = -1f;
            public bool HoldReleaseTimeGate;
            public bool HoldReleasePressureGate;
            public bool HoldReleaseStateGate;
        }

        [MenuItem("Critical/Run IP-0028 Deterministic Triple (Stage C)")]
        public static void RunDeterministicTriple()
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string suiteDir = Path.Combine(root, "HeatupLogs", $"IP-0028_StageC_{stamp}");
            Directory.CreateDirectory(suiteDir);

            var runs = new List<RunResult>
            {
                ExecuteRun(suiteDir, "RUN_A"),
                ExecuteRun(suiteDir, "RUN_B"),
                ExecuteRun(suiteDir, "RUN_C")
            };

            bool eventOrderMatch = CompareEventOrdering(runs);
            bool limiterSequenceMatch = CompareLimiterSequence(runs);
            bool holdReleaseMatch = CompareHoldRelease(runs, out string holdReleaseDetail);

            string suiteSummaryPath = Path.Combine(suiteDir, $"IP-0028_StageC_Determinism_{stamp}.md");
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0028 Stage C Deterministic Triple Summary");
            sb.AppendLine();
            sb.AppendLine($"- Stamp: `{stamp}`");
            sb.AppendLine($"- dt_hr: `{DtHr:F8}`");
            sb.AppendLine($"- max_sim_hr: `{MaxSimHr:F2}`");
            sb.AppendLine();
            foreach (RunResult run in runs)
            {
                sb.AppendLine($"## {run.Label}");
                sb.AppendLine($"- Log dir: `{run.LogDir}`");
                sb.AppendLine($"- Step CSV: `{run.StepCsvPath}`");
                sb.AppendLine($"- Event CSV: `{run.EventCsvPath}`");
                sb.AppendLine($"- Summary: `{run.SummaryPath}`");
                sb.AppendLine($"- Report: `{run.ReportPath}`");
                sb.AppendLine($"- Hold release hr: `{run.HoldReleaseTimeHr:F6}`");
                sb.AppendLine();
            }

            sb.AppendLine("## Determinism Checks");
            sb.AppendLine($"- Event ordering identical: {(eventOrderMatch ? "PASS" : "FAIL")}");
            sb.AppendLine($"- Limiter activation sequence identical: {(limiterSequenceMatch ? "PASS" : "FAIL")}");
            sb.AppendLine($"- Hold-release gating behavior identical: {(holdReleaseMatch ? "PASS" : "FAIL")} ({holdReleaseDetail})");
            File.WriteAllText(suiteSummaryPath, sb.ToString(), Encoding.UTF8);

            Debug.Log($"[IP-0028] Stage C deterministic suite summary: {suiteSummaryPath}");

            if (!eventOrderMatch || !limiterSequenceMatch || !holdReleaseMatch)
            {
                throw new Exception("IP-0028 Stage C deterministic triple failed.");
            }
        }

        private static RunResult ExecuteRun(string suiteDir, string label)
        {
            string runStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string runDir = Path.Combine(suiteDir, $"{label}_{runStamp}");
            Directory.CreateDirectory(runDir);

            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
            HeatupSimEngine engine = UnityEngine.Object.FindObjectOfType<HeatupSimEngine>();
            if (engine == null)
                throw new InvalidOperationException("HeatupSimEngine not found in MainScene.");

            Type t = typeof(HeatupSimEngine);
            MethodInfo init = t.GetMethod("InitializeSimulation", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo step = t.GetMethod("StepSimulation", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo saveInterval = t.GetMethod("SaveIntervalLog", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo saveReport = t.GetMethod("SaveReport", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo logPathField = t.GetField("logPath", BindingFlags.Instance | BindingFlags.NonPublic);
            if (init == null || step == null || saveInterval == null || saveReport == null || logPathField == null)
                throw new MissingMethodException("IP0028 runner could not resolve HeatupSimEngine internals.");

            engine.runOnStart = false;
            engine.coldShutdownStart = true;
            engine.startTemperature = 100f;
            engine.targetTemperature = 557f;
            engine.enableAsyncLogWriter = false;
            engine.enableHighFrequencyPerfLogs = false;
            engine.enableWorkerThreadStepping = false;
            engine.enablePzrBubbleDiagnostics = true;
            engine.pzrBubbleDiagnosticsLabel = $"IP0028_{label}";
            logPathField.SetValue(engine, runDir);

            init.Invoke(engine, null);
            saveInterval.Invoke(engine, null);

            var result = new RunResult
            {
                Label = label,
                LogDir = runDir
            };
            result.Steps.Add(CaptureStep(engine));

            float nextInterval = IntervalLogHr;
            for (int i = 0; i < MaxSteps && engine.simTime < MaxSimHr; i++)
            {
                step.Invoke(engine, new object[] { DtHr });
                result.Steps.Add(CaptureStep(engine));

                if (engine.simTime >= nextInterval)
                {
                    saveInterval.Invoke(engine, null);
                    nextInterval += IntervalLogHr;
                }
            }

            saveReport.Invoke(engine, null);
            result.ReportPath = Directory.GetFiles(runDir, "Heatup_Report_*.txt")
                .OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? string.Empty;

            foreach (HeatupSimEngine.EventLogEntry entry in engine.eventLog)
            {
                result.Events.Add(new EventSample
                {
                    TimeHr = entry.SimTime,
                    Severity = entry.Severity.ToString(),
                    Message = entry.Message ?? string.Empty
                });
            }

            for (int i = 1; i < result.Steps.Count; i++)
            {
                StepSample prev = result.Steps[i - 1];
                StepSample curr = result.Steps[i];
                if (prev.HoldActive && !curr.HoldActive)
                {
                    result.HoldReleaseTimeHr = curr.TimeHr;
                    result.HoldReleaseTimeGate = curr.HoldTimeGate;
                    result.HoldReleasePressureGate = curr.HoldPressureGate;
                    result.HoldReleaseStateGate = curr.HoldStateGate;
                    break;
                }
            }

            result.StepCsvPath = Path.Combine(runDir, $"IP-0028_{label}_StepTelemetry.csv");
            result.EventCsvPath = Path.Combine(runDir, $"IP-0028_{label}_EventSequence.csv");
            result.SummaryPath = Path.Combine(runDir, $"IP-0028_{label}_RunSummary.md");
            WriteStepCsv(result.StepCsvPath, result.Steps);
            WriteEventCsv(result.EventCsvPath, result.Events);
            WriteRunSummary(result.SummaryPath, result);

            return result;
        }

        private static StepSample CaptureStep(HeatupSimEngine engine)
        {
            return new StepSample
            {
                TimeHr = engine.simTime,
                PressurePsia = engine.pressure,
                PzrLevelPct = engine.pzrLevel,
                HeaterPowerMw = engine.pzrHeaterPower,
                PressureRatePsiHr = engine.pressureRate,
                HeaterMode = engine.currentHeaterMode.ToString(),
                HoldActive = engine.startupHoldActive,
                HoldElapsedSec = engine.startupHoldElapsedTime_hr * 3600f,
                HoldTimeGate = engine.startupHoldTimeGatePassed,
                HoldPressureGate = engine.startupHoldPressureRateGatePassed,
                HoldStateGate = engine.startupHoldStateQualityGatePassed,
                HoldBlockReason = engine.startupHoldReleaseBlockReason ?? "NONE",
                AuthorityState = engine.heaterAuthorityState ?? "UNSET",
                AuthorityReason = engine.heaterAuthorityReason ?? "UNSET",
                LimiterReason = engine.heaterLimiterReason ?? "NONE",
                LimiterDetail = engine.heaterLimiterDetail ?? "NONE",
                PressureRateClamp = engine.heaterPressureRateClampActive,
                RampRateClamp = engine.heaterRampRateClampActive,
                SolidControlMode = engine.solidPlantState.ControlMode ?? "UNSET",
                SolidInBand = engine.solidPlantPressureInBand,
                SprayActive = engine.sprayActive,
                SprayFlowGpm = engine.sprayFlow_GPM,
                BubblePhase = engine.bubblePhase.ToString(),
                ClosureConverged = engine.pzrClosureConverged,
                ClosureVolumeResidualFt3 = engine.pzrClosureVolumeResidual_ft3,
                ClosureEnergyResidualBtu = engine.pzrClosureEnergyResidual_BTU,
                RvlisFull = engine.rvlisFull,
                RvlisUpper = engine.rvlisUpper,
                RvlisFullValid = engine.rvlisFullValid,
                RvlisUpperValid = engine.rvlisUpperValid
            };
        }

        private static bool CompareEventOrdering(List<RunResult> runs)
        {
            if (runs.Count < 2)
                return true;

            List<string> baseline = runs[0].Events.Select(e => $"{e.Severity}|{e.Message}").ToList();
            for (int i = 1; i < runs.Count; i++)
            {
                List<string> candidate = runs[i].Events.Select(e => $"{e.Severity}|{e.Message}").ToList();
                if (baseline.Count != candidate.Count)
                    return false;
                for (int j = 0; j < baseline.Count; j++)
                {
                    if (!string.Equals(baseline[j], candidate[j], StringComparison.Ordinal))
                        return false;
                }
            }

            return true;
        }

        private static bool CompareLimiterSequence(List<RunResult> runs)
        {
            if (runs.Count < 2)
                return true;

            List<string> baseline = BuildLimiterSequence(runs[0].Steps);
            for (int i = 1; i < runs.Count; i++)
            {
                List<string> candidate = BuildLimiterSequence(runs[i].Steps);
                if (baseline.Count != candidate.Count)
                    return false;
                for (int j = 0; j < baseline.Count; j++)
                {
                    if (!string.Equals(baseline[j], candidate[j], StringComparison.Ordinal))
                        return false;
                }
            }

            return true;
        }

        private static bool CompareHoldRelease(List<RunResult> runs, out string detail)
        {
            detail = "ok";
            if (runs.Count < 2)
                return true;

            RunResult baseline = runs[0];
            for (int i = 1; i < runs.Count; i++)
            {
                RunResult candidate = runs[i];
                if (baseline.HoldReleaseTimeHr < 0f || candidate.HoldReleaseTimeHr < 0f)
                {
                    detail = "missing_hold_release";
                    return false;
                }

                float deltaHr = Mathf.Abs(baseline.HoldReleaseTimeHr - candidate.HoldReleaseTimeHr);
                if (deltaHr > DtHr + 1e-6f)
                {
                    detail = $"release_delta_hr={deltaHr:F6}";
                    return false;
                }

                if (baseline.HoldReleaseTimeGate != candidate.HoldReleaseTimeGate ||
                    baseline.HoldReleasePressureGate != candidate.HoldReleasePressureGate ||
                    baseline.HoldReleaseStateGate != candidate.HoldReleaseStateGate)
                {
                    detail = "gate_boolean_mismatch";
                    return false;
                }
            }

            return true;
        }

        private static List<string> BuildLimiterSequence(List<StepSample> steps)
        {
            var sequence = new List<string>();
            string prev = string.Empty;
            foreach (StepSample sample in steps)
            {
                string current = sample.LimiterReason ?? "NONE";
                if (current == prev)
                    continue;
                sequence.Add(current);
                prev = current;
            }
            return sequence;
        }

        private static void WriteStepCsv(string path, List<StepSample> steps)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                "time_hr,pressure_psia,pzr_level_pct,heater_power_mw,pressure_rate_psi_hr,heater_mode,hold_active,hold_elapsed_s," +
                "hold_gate_time,hold_gate_pressure,hold_gate_state,hold_block_reason,authority_state,authority_reason," +
                "limiter_reason,limiter_detail,pressure_rate_clamp,ramp_rate_clamp,solid_control_mode,solid_in_band,spray_active,spray_flow_gpm," +
                "bubble_phase,closure_converged,closure_volume_residual_ft3,closure_energy_residual_btu,rvlis_full,rvlis_upper,rvlis_full_valid,rvlis_upper_valid");
            foreach (StepSample s in steps)
            {
                sb.AppendLine(
                    $"{s.TimeHr:F6},{s.PressurePsia:F6},{s.PzrLevelPct:F6},{s.HeaterPowerMw:F6},{s.PressureRatePsiHr:F6},{Csv(s.HeaterMode)}," +
                    $"{ToBit(s.HoldActive)},{s.HoldElapsedSec:F3},{ToBit(s.HoldTimeGate)},{ToBit(s.HoldPressureGate)},{ToBit(s.HoldStateGate)}," +
                    $"{Csv(s.HoldBlockReason)},{Csv(s.AuthorityState)},{Csv(s.AuthorityReason)}," +
                    $"{Csv(s.LimiterReason)},{Csv(s.LimiterDetail)},{ToBit(s.PressureRateClamp)},{ToBit(s.RampRateClamp)}," +
                    $"{Csv(s.SolidControlMode)},{ToBit(s.SolidInBand)},{ToBit(s.SprayActive)},{s.SprayFlowGpm:F6}," +
                    $"{Csv(s.BubblePhase)},{ToBit(s.ClosureConverged)},{s.ClosureVolumeResidualFt3:F6},{s.ClosureEnergyResidualBtu:F6}," +
                    $"{s.RvlisFull:F6},{s.RvlisUpper:F6},{ToBit(s.RvlisFullValid)},{ToBit(s.RvlisUpperValid)}");
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static void WriteEventCsv(string path, List<EventSample> eventsList)
        {
            var sb = new StringBuilder();
            sb.AppendLine("time_hr,severity,message");
            foreach (EventSample e in eventsList)
                sb.AppendLine($"{e.TimeHr:F6},{Csv(e.Severity)},{Csv(e.Message)}");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static void WriteRunSummary(string path, RunResult run)
        {
            StepSample win150 = Nearest(run.Steps, 1.50f);
            StepSample win175 = Nearest(run.Steps, 1.75f);
            StepSample win200 = Nearest(run.Steps, 2.00f);
            var sb = new StringBuilder();
            sb.AppendLine($"# {run.Label} Summary");
            sb.AppendLine();
            sb.AppendLine($"- Step CSV: `{run.StepCsvPath}`");
            sb.AppendLine($"- Event CSV: `{run.EventCsvPath}`");
            sb.AppendLine($"- Report: `{run.ReportPath}`");
            sb.AppendLine($"- Hold release time: `{run.HoldReleaseTimeHr:F6}` hr");
            sb.AppendLine($"- Hold release gates: time={run.HoldReleaseTimeGate}, pressure={run.HoldReleasePressureGate}, state={run.HoldReleaseStateGate}");
            sb.AppendLine();
            sb.AppendLine("## CS-0096 Windows");
            AppendWindow(sb, "1.50 hr", win150);
            AppendWindow(sb, "1.75 hr", win175);
            AppendWindow(sb, "2.00 hr", win200);
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static void AppendWindow(StringBuilder sb, string label, StepSample sample)
        {
            sb.AppendLine($"- {label}: mode={sample.SolidControlMode}, inBand={sample.SolidInBand}, heaterMW={sample.HeaterPowerMw:F3}, limiter={sample.LimiterReason}, sprayActive={sample.SprayActive}");
        }

        private static StepSample Nearest(List<StepSample> steps, float targetHr)
        {
            StepSample best = steps[0];
            float bestDelta = Mathf.Abs(best.TimeHr - targetHr);
            for (int i = 1; i < steps.Count; i++)
            {
                float delta = Mathf.Abs(steps[i].TimeHr - targetHr);
                if (delta < bestDelta)
                {
                    best = steps[i];
                    bestDelta = delta;
                }
            }
            return best;
        }

        private static int ToBit(bool value)
        {
            return value ? 1 : 0;
        }

        private static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            string escaped = value.Replace("\"", "\"\"");
            return escaped.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
                ? $"\"{escaped}\""
                : escaped;
        }
    }
}

