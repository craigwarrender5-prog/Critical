using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Critical.Physics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using Critical.Validation;
namespace Critical.Validation
{
    public static class PzrBubbleInvestigationRunner
    {
        private const float IntervalLogHr = 0.25f;
        private const float MaxSimHr = 12f;
        private const int MaxSteps = 600000;

        private sealed class RunConfig
        {
            public string Label = string.Empty;
            public float DtHr;
            public bool DisableAmbientPressureFloor;
        }

        private sealed class Sample
        {
            public float T;
            public float P;
            public float Level;
            public float Surge;
            public HeatupSimEngine.BubbleFormationPhase Phase;
        }

        private sealed class RunResult
        {
            public string Label = string.Empty;
            public string LogDir = string.Empty;
            public string ReportPath = string.Empty;
            public string DiagnosticLogPath = string.Empty;
            public string OrificeDiagnosticLogPath = string.Empty;
            public string SummaryPath = string.Empty;
            public DateTime StartedUtc;
            public DateTime EndedUtc;
            public float DtHr;
            public bool DisableAmbientPressureFloor;
            public float FinalTimeHr;
            public int Steps;
            public int DiagnosticLineCount;
            public int OrificeDiagnosticLineCount;
            public readonly List<Sample> Samples = new List<Sample>();
        }

        [MenuItem("Critical/Investigations/Run PZR Bubble Investigation Bracket")]
        public static void RunAll()
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string outputRoot = Path.Combine(root, "HeatupLogs", $"PZR_INVEST_{stamp}");
            Directory.CreateDirectory(outputRoot);

            var configs = new[]
            {
                new RunConfig
                {
                    Label = "BASELINE",
                    DtHr = 1f / 360f,
                    DisableAmbientPressureFloor = false
                },
                new RunConfig
                {
                    Label = "SMALLER_TIMESTEP",
                    DtHr = 1f / 720f,
                    DisableAmbientPressureFloor = false
                },
                new RunConfig
                {
                    Label = "NO_AMBIENT_CLAMP",
                    DtHr = 1f / 360f,
                    DisableAmbientPressureFloor = true
                }
            };

            var results = new List<RunResult>();
            foreach (RunConfig cfg in configs)
                results.Add(ExecuteRun(outputRoot, cfg));

            string suiteSummary = Path.Combine(outputRoot, $"PZR_Bubble_Investigation_Suite_{stamp}.md");
            File.WriteAllText(suiteSummary, BuildSuiteSummary(results), Encoding.UTF8);
            Debug.Log($"[PZR_INVEST] Suite summary written: {suiteSummary}");
        }

        private static RunResult ExecuteRun(string outputRoot, RunConfig config)
        {
            string runStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string logDir = Path.Combine(outputRoot, $"{config.Label}_{runStamp}");
            Directory.CreateDirectory(logDir);

            var run = new RunResult
            {
                Label = config.Label,
                LogDir = logDir,
                DtHr = config.DtHr,
                DisableAmbientPressureFloor = config.DisableAmbientPressureFloor,
                StartedUtc = DateTime.UtcNow
            };

            string scenePath = "Assets/Scenes/MainScene.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
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
                throw new MissingMethodException("PzrBubbleInvestigationRunner could not access required HeatupSimEngine members.");

            engine.runOnStart = false;
            engine.coldShutdownStart = true;
            engine.startTemperature = 100f;
            engine.targetTemperature = 557f;
            engine.enableAsyncLogWriter = false;
            engine.enableHighFrequencyPerfLogs = false;
            engine.enableWorkerThreadStepping = false;
            engine.enablePzrBubbleDiagnostics = true;
            engine.pzrBubbleDiagnosticsLabel = config.Label;
            engine.pzrBubbleDiagnosticsResidualTolerance_ft3 = 10f;
            engine.pzrBubbleDiagnosticsMaxIterations = 12;
            engine.enablePzrOrificeDiagnostics = true;
            engine.pzrOrificeDiagnosticsSampleStrideTicks = 25;
            logPathField.SetValue(engine, logDir);

            var capturedDiagnostics = new List<string>(4096);
            var capturedOrificeDiagnostics = new List<string>(2048);
            object diagLock = new object();
            void OnLog(string condition, string stackTrace, LogType type)
            {
                if (condition == null)
                    return;
                lock (diagLock)
                {
                    if (condition.Contains("[PZR_BUBBLE_DIAG]", StringComparison.Ordinal))
                        capturedDiagnostics.Add(condition);
                    if (condition.Contains("[PZR_ORIFICE_DIAG]", StringComparison.Ordinal))
                        capturedOrificeDiagnostics.Add(condition);
                }
            }

            Application.logMessageReceivedThreaded += OnLog;
            bool previousDisableAmbient = SolidPlantPressure.DisableAmbientPressureFloorForDiagnostics;
            try
            {
                SolidPlantPressure.DisableAmbientPressureFloorForDiagnostics = config.DisableAmbientPressureFloor;

                init.Invoke(engine, null);
                saveInterval.Invoke(engine, null);
                run.Samples.Add(Capture(engine));

                float nextIntervalLogHr = IntervalLogHr;
                int steps = 0;
                bool bubbleCompleted = false;
                while (steps < MaxSteps && engine.simTime < MaxSimHr)
                {
                    step.Invoke(engine, new object[] { config.DtHr });
                    steps++;
                    run.Samples.Add(Capture(engine));

                    if (engine.simTime >= nextIntervalLogHr)
                    {
                        saveInterval.Invoke(engine, null);
                        nextIntervalLogHr += IntervalLogHr;
                    }

                    if (engine.bubblePhase == HeatupSimEngine.BubbleFormationPhase.COMPLETE)
                    {
                        if (!bubbleCompleted)
                            bubbleCompleted = true;
                        else if (engine.simTime > 9f)
                            break;
                    }
                }

                saveReport.Invoke(engine, null);
                run.FinalTimeHr = engine.simTime;
                run.Steps = steps;
                run.EndedUtc = DateTime.UtcNow;
            }
            finally
            {
                SolidPlantPressure.DisableAmbientPressureFloorForDiagnostics = previousDisableAmbient;
                Application.logMessageReceivedThreaded -= OnLog;
            }

            string report = Directory.GetFiles(logDir, "Heatup_Report_*.txt")
                .OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? string.Empty;
            run.ReportPath = report;

            string diagPath = Path.Combine(logDir, $"PZR_Bubble_Diagnostics_{config.Label}.log");
            List<string> diagSnapshot;
            lock (diagLock)
            {
                diagSnapshot = capturedDiagnostics.ToList();
            }
            File.WriteAllLines(diagPath, diagSnapshot, Encoding.UTF8);
            run.DiagnosticLogPath = diagPath;
            run.DiagnosticLineCount = diagSnapshot.Count;

            string orificeDiagPath = Path.Combine(logDir, $"PZR_Orifice_Diagnostics_{config.Label}.log");
            List<string> orificeSnapshot;
            lock (diagLock)
            {
                orificeSnapshot = capturedOrificeDiagnostics.ToList();
            }
            File.WriteAllLines(orificeDiagPath, orificeSnapshot, Encoding.UTF8);
            run.OrificeDiagnosticLogPath = orificeDiagPath;
            run.OrificeDiagnosticLineCount = orificeSnapshot.Count;

            string runSummaryPath = Path.Combine(logDir, $"PZR_Bubble_RunSummary_{config.Label}.md");
            File.WriteAllText(runSummaryPath, BuildRunSummary(run), Encoding.UTF8);
            run.SummaryPath = runSummaryPath;

            Debug.Log(
                $"[PZR_INVEST] Completed {config.Label} | dt_hr={config.DtHr:F8} | " +
                $"disableAmbientClamp={config.DisableAmbientPressureFloor} | steps={run.Steps} | sim_hr={run.FinalTimeHr:F3} | " +
                $"diag_lines={run.DiagnosticLineCount} | orifice_diag_lines={run.OrificeDiagnosticLineCount}");

            return run;
        }

        private static Sample Capture(HeatupSimEngine e)
        {
            return new Sample
            {
                T = e.simTime,
                P = e.pressure,
                Level = e.pzrLevel,
                Surge = e.surgeFlow,
                Phase = e.bubblePhase
            };
        }

        private static string BuildRunSummary(RunResult run)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# PZR Bubble Investigation Run - {run.Label}");
            sb.AppendLine();
            sb.AppendLine($"- Started UTC: {run.StartedUtc:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Ended UTC: {run.EndedUtc:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- dt (hr): {run.DtHr:F8}");
            sb.AppendLine($"- Ambient clamp disabled: {run.DisableAmbientPressureFloor}");
            sb.AppendLine($"- Steps: {run.Steps}");
            sb.AppendLine($"- Final sim time (hr): {run.FinalTimeHr:F4}");
            sb.AppendLine($"- Diagnostic lines captured: {run.DiagnosticLineCount}");
            sb.AppendLine($"- Orifice diagnostic lines captured: {run.OrificeDiagnosticLineCount}");
            sb.AppendLine($"- Report path: {run.ReportPath}");
            sb.AppendLine($"- Diagnostic log path: {run.DiagnosticLogPath}");
            sb.AppendLine($"- Orifice diagnostic log path: {run.OrificeDiagnosticLogPath}");
            sb.AppendLine();

            AppendSignalMetrics(sb, run.Samples);
            AppendPhaseWindows(sb, run.Samples);
            return sb.ToString();
        }

        private static string BuildSuiteSummary(List<RunResult> runs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# PZR Bubble Investigation Suite");
            sb.AppendLine();
            foreach (RunResult run in runs)
            {
                sb.AppendLine($"## {run.Label}");
                sb.AppendLine($"- dt_hr: {run.DtHr:F8}");
                sb.AppendLine($"- ambient_clamp_disabled: {run.DisableAmbientPressureFloor}");
                sb.AppendLine($"- steps: {run.Steps}");
                sb.AppendLine($"- final_sim_hr: {run.FinalTimeHr:F4}");
                sb.AppendLine($"- diagnostics: {run.DiagnosticLineCount} lines");
                sb.AppendLine($"- orifice_diagnostics: {run.OrificeDiagnosticLineCount} lines");
                sb.AppendLine($"- log_dir: {run.LogDir}");
                sb.AppendLine($"- run_summary: {run.SummaryPath}");
                sb.AppendLine($"- report: {run.ReportPath}");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static void AppendSignalMetrics(StringBuilder sb, List<Sample> samples)
        {
            if (samples.Count < 2)
            {
                sb.AppendLine("## Signal Metrics");
                sb.AppendLine("- Insufficient samples.");
                sb.AppendLine();
                return;
            }

            float minP = samples.Min(s => s.P);
            float maxP = samples.Max(s => s.P);
            float minL = samples.Min(s => s.Level);
            float maxL = samples.Max(s => s.Level);

            float steepestDropRate = float.MaxValue;
            float steepestRiseRate = float.MinValue;
            float dropAtTime = 0f;
            float riseAtTime = 0f;
            float maxLevelStep = float.MinValue;
            float minLevelStep = float.MaxValue;
            float levelRiseAtTime = 0f;
            float levelDropAtTime = 0f;

            for (int i = 1; i < samples.Count; i++)
            {
                Sample prev = samples[i - 1];
                Sample curr = samples[i];
                float dt = Mathf.Max(1e-6f, curr.T - prev.T);
                float dPdt = (curr.P - prev.P) / dt;
                float dLevel = curr.Level - prev.Level;

                if (dPdt < steepestDropRate)
                {
                    steepestDropRate = dPdt;
                    dropAtTime = curr.T;
                }
                if (dPdt > steepestRiseRate)
                {
                    steepestRiseRate = dPdt;
                    riseAtTime = curr.T;
                }
                if (dLevel > maxLevelStep)
                {
                    maxLevelStep = dLevel;
                    levelRiseAtTime = curr.T;
                }
                if (dLevel < minLevelStep)
                {
                    minLevelStep = dLevel;
                    levelDropAtTime = curr.T;
                }
            }

            sb.AppendLine("## Signal Metrics");
            sb.AppendLine($"- Pressure range: {minP:F2}..{maxP:F2} psia");
            sb.AppendLine($"- Level range: {minL:F2}..{maxL:F2} %");
            sb.AppendLine($"- Steepest pressure drop: {steepestDropRate:F2} psi/hr at sim {dropAtTime:F4} hr");
            sb.AppendLine($"- Steepest pressure rise: {steepestRiseRate:F2} psi/hr at sim {riseAtTime:F4} hr");
            sb.AppendLine($"- Largest level up-step: {maxLevelStep:F3} % at sim {levelRiseAtTime:F4} hr");
            sb.AppendLine($"- Largest level down-step: {minLevelStep:F3} % at sim {levelDropAtTime:F4} hr");
            sb.AppendLine();
        }

        private static void AppendPhaseWindows(StringBuilder sb, List<Sample> samples)
        {
            sb.AppendLine("## Bubble Phase Windows");
            foreach (HeatupSimEngine.BubbleFormationPhase phase in Enum.GetValues(typeof(HeatupSimEngine.BubbleFormationPhase)))
            {
                List<Sample> phaseSamples = samples.Where(s => s.Phase == phase).ToList();
                if (phaseSamples.Count == 0)
                    continue;

                float start = phaseSamples.First().T;
                float end = phaseSamples.Last().T;
                float minP = phaseSamples.Min(s => s.P);
                float maxP = phaseSamples.Max(s => s.P);
                float minL = phaseSamples.Min(s => s.Level);
                float maxL = phaseSamples.Max(s => s.Level);
                sb.AppendLine($"- {phase}: t={start:F4}..{end:F4} hr | P={minP:F2}..{maxP:F2} psia | Level={minL:F2}..{maxL:F2}%");
            }
            sb.AppendLine();
        }
    }
}

