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

namespace Critical.Validation
{
    public static class IP0021CheckpointRunner
    {
        private const float DtHr = 1f / 360f;
        private const float IntervalLogHr = 0.25f;
        private const float MaxSimHr = 18f;
        private const int MaxSteps = 260000;

        private sealed class Sample
        {
            public float T;
            public float P;
            public float Prate;
            public float PzrLevel;
            public float TRcs;
            public float TRcsRate;
            public float TPzr;
            public float Tsat;
            public float Charging;
            public float Letdown;
            public float HeaterMW;
            public HeatupSimEngine.BubbleFormationPhase Phase;
            public bool BubbleFormed;
            public bool Solid;
            public float DrainSteam;
            public float DrainCvcs;
            public float DrainDuration;
            public float DrainExitPressure;
            public float DrainExitLevel;
            public bool DrainHardGate;
            public bool DrainPressureBandOK;
            public string DrainTransitionReason = string.Empty;
        }

        private sealed class RunResult
        {
            public readonly List<Sample> Samples = new List<Sample>();
            public string LogDir = string.Empty;
            public string ReportPath = string.Empty;
            public float FinalTimeHr;
            public int Steps;
        }

        [MenuItem("Critical/Run IP-0021 Group1 Checkpoint")]
        public static void RunGroup1Checkpoint()
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string logDir = Path.Combine(root, "HeatupLogs", $"IP-0021_Group1_{stamp}");
            string evidencePath = Path.Combine(root, "Governance", "Issues",
                $"IP-0021_Group1_Checkpoint_{DateTime.Now:yyyy-MM-dd_HHmmss}.md");

            RunResult run = ExecuteDeterministicRun(logDir);
            WriteGroup1Evidence(run, evidencePath);
            Debug.Log($"[IP-0021][Group1] Evidence written: {evidencePath}");
        }

        [MenuItem("Critical/Run IP-0021 Group2 Checkpoint")]
        public static void RunGroup2Checkpoint()
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string logDir = Path.Combine(root, "HeatupLogs", $"IP-0021_Group2_{stamp}");
            string evidencePath = Path.Combine(root, "Governance", "Issues",
                $"IP-0021_Group2_Checkpoint_{DateTime.Now:yyyy-MM-dd_HHmmss}.md");

            RunResult run = ExecuteDeterministicRun(logDir);
            WriteGroup2Evidence(run, evidencePath);
            Debug.Log($"[IP-0021][Group2] Evidence written: {evidencePath}");
        }

        [MenuItem("Critical/Run IP-0021 Group3 Checkpoint")]
        public static void RunGroup3Checkpoint()
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string logDirA = Path.Combine(root, "HeatupLogs", $"IP-0021_Group3A_{stamp}");
            string logDirB = Path.Combine(root, "HeatupLogs", $"IP-0021_Group3B_{stamp}");
            string evidencePath = Path.Combine(root, "Governance", "Issues",
                $"IP-0021_Group3_Checkpoint_{DateTime.Now:yyyy-MM-dd_HHmmss}.md");

            RunResult runA = ExecuteDeterministicRun(logDirA);
            RunResult runB = ExecuteDeterministicRun(logDirB);
            WriteGroup3Evidence(runA, runB, evidencePath);
            Debug.Log($"[IP-0021][Group3] Evidence written: {evidencePath}");
        }

        [MenuItem("Critical/Run IP-0021 Group4 Checkpoint")]
        public static void RunGroup4Checkpoint()
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string logDir = Path.Combine(root, "HeatupLogs", $"IP-0021_Group4_{stamp}");
            string evidencePath = Path.Combine(root, "Governance", "Issues",
                $"IP-0021_Group4_Checkpoint_{DateTime.Now:yyyy-MM-dd_HHmmss}.md");

            RunResult run = ExecuteDeterministicRun(logDir);
            WriteGroup4Evidence(run, evidencePath, root);
            Debug.Log($"[IP-0021][Group4] Evidence written: {evidencePath}");
        }

        [MenuItem("Critical/Run IP-0021 Group5 Disposition")]
        public static void RunGroup5Disposition()
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string logDir = Path.Combine(root, "HeatupLogs", $"IP-0021_Group5_{stamp}");
            string evidencePath = Path.Combine(root, "Governance", "Issues",
                $"IP-0021_Group5_Disposition_{DateTime.Now:yyyy-MM-dd_HHmmss}.md");

            RunResult run = ExecuteDeterministicRun(logDir);
            WriteGroup5Disposition(run, evidencePath);
            Debug.Log($"[IP-0021][Group5] Evidence written: {evidencePath}");
        }

        private static RunResult ExecuteDeterministicRun(string logDir)
        {
            string scenePath = "Assets/Scenes/MainScene.unity";
            PrepareLogDirectory(logDir);

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
                throw new MissingMethodException("IP-0021 runner could not access required HeatupSimEngine members.");

            engine.runOnStart = false;
            engine.coldShutdownStart = true;
            engine.startTemperature = 100f;
            engine.targetTemperature = 557f;
            logPathField.SetValue(engine, logDir);

            init.Invoke(engine, null);
            saveInterval.Invoke(engine, null);

            float nextIntervalLogHr = IntervalLogHr;
            int steps = 0;
            var result = new RunResult { LogDir = logDir };
            result.Samples.Add(Capture(engine));

            while (steps < MaxSteps && engine.simTime < MaxSimHr)
            {
                step.Invoke(engine, new object[] { DtHr });
                steps++;
                result.Samples.Add(Capture(engine));

                if (engine.simTime >= nextIntervalLogHr)
                {
                    saveInterval.Invoke(engine, null);
                    nextIntervalLogHr += IntervalLogHr;
                }

                if (engine.bubbleFormed && engine.rcpCount > 0 && engine.simTime > 9f)
                    break;
            }

            saveReport.Invoke(engine, null);

            string report = Directory.GetFiles(logDir, "Heatup_Report_*.txt")
                .OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? string.Empty;

            result.ReportPath = report;
            result.FinalTimeHr = engine.simTime;
            result.Steps = steps;
            return result;
        }

        private static Sample Capture(HeatupSimEngine e)
        {
            return new Sample
            {
                T = e.simTime,
                P = e.pressure,
                Prate = e.pressureRate,
                PzrLevel = e.pzrLevel,
                TRcs = e.T_rcs,
                TRcsRate = e.rcsHeatRate,
                TPzr = e.T_pzr,
                Tsat = e.T_sat,
                Charging = e.chargingFlow,
                Letdown = e.letdownFlow,
                HeaterMW = e.pzrHeaterPower,
                Phase = e.bubblePhase,
                BubbleFormed = e.bubbleFormed,
                Solid = e.solidPressurizer,
                DrainSteam = e.drainSteamDisplacement_lbm,
                DrainCvcs = e.drainCvcsTransfer_lbm,
                DrainDuration = e.drainDuration_hr,
                DrainExitPressure = e.drainExitPressure_psia,
                DrainExitLevel = e.drainExitLevel_pct,
                DrainHardGate = e.drainHardGateTriggered,
                DrainPressureBandOK = e.drainPressureBandMaintained,
                DrainTransitionReason = e.drainTransitionReason ?? string.Empty
            };
        }

        private static void WriteGroup1Evidence(RunResult run, string evidencePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(evidencePath) ?? ".");
            var sb = new StringBuilder();

            List<Sample> drain = run.Samples
                .Where(s => s.Phase == HeatupSimEngine.BubbleFormationPhase.DRAIN)
                .ToList();
            List<Sample> pressurize = run.Samples
                .Where(s => s.Phase == HeatupSimEngine.BubbleFormationPhase.PRESSURIZE)
                .ToList();

            bool cs0043Pass = false;
            bool cs0049Pass = false;
            string cs0043Reason;
            string cs0049Reason;

            if (drain.Count == 0)
            {
                cs0043Reason = "No DRAIN phase samples observed.";
            }
            else
            {
                float startP = drain.First().P;
                float minP = drain.Min(s => s.P);
                float collapse = startP - minP;
                float minStartupBand = PlantConstants.BUBBLE_DRAIN_MIN_PRESSURE_PSIA - 25f;
                cs0043Pass = collapse <= 60f && minP >= minStartupBand;
                cs0043Reason = $"start={startP:F1} psia, min={minP:F1} psia, collapse={collapse:F1} psi, thresholdMin={minStartupBand:F1} psia";
            }

            if (pressurize.Count == 0)
            {
                cs0049Reason = "No PRESSURIZE phase samples observed.";
            }
            else
            {
                float startP = pressurize.First().P;
                float endP = pressurize.Last().P;
                float maxP = pressurize.Max(s => s.P);
                float minRcpPsia = PlantConstants.MIN_RCP_PRESSURE_PSIG + 14.7f;
                bool nonCollapsing = endP >= (startP - 2f);
                bool reachesThreshold = maxP >= minRcpPsia;
                cs0049Pass = nonCollapsing && reachesThreshold;
                cs0049Reason = $"start={startP:F1} psia, end={endP:F1} psia, max={maxP:F1} psia, threshold={minRcpPsia:F1} psia";
            }

            bool overall = cs0043Pass && cs0049Pass;

            sb.AppendLine("# IP-0021 Group 1 Checkpoint");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run steps: {run.Steps}");
            sb.AppendLine($"- Final sim time: {run.FinalTimeHr:F3} hr");
            sb.AppendLine($"- Log directory: `{run.LogDir}`");
            if (!string.IsNullOrWhiteSpace(run.ReportPath))
                sb.AppendLine($"- Heatup report: `{run.ReportPath}`");
            sb.AppendLine();
            sb.AppendLine("## CS Results");
            sb.AppendLine($"- CS-0043: {(cs0043Pass ? "PASS" : "FAIL")} | {cs0043Reason}");
            sb.AppendLine($"- CS-0049: {(cs0049Pass ? "PASS" : "FAIL")} | {cs0049Reason}");
            sb.AppendLine();
            sb.AppendLine($"## Group 1 Overall: {(overall ? "PASS" : "FAIL")}");

            File.WriteAllText(evidencePath, sb.ToString());
            if (!overall)
                throw new Exception("IP-0021 Group 1 checkpoint failed. See evidence file.");
        }

        private static void WriteGroup2Evidence(RunResult run, string evidencePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(evidencePath) ?? ".");
            var sb = new StringBuilder();

            List<Sample> twoPhase = run.Samples
                .Where(s => s.Phase == HeatupSimEngine.BubbleFormationPhase.DRAIN
                         || s.Phase == HeatupSimEngine.BubbleFormationPhase.STABILIZE
                         || s.Phase == HeatupSimEngine.BubbleFormationPhase.PRESSURIZE)
                .ToList();
            List<Sample> drain = twoPhase.Where(s => s.Phase == HeatupSimEngine.BubbleFormationPhase.DRAIN).ToList();
            List<Sample> stabilize = twoPhase.Where(s => s.Phase == HeatupSimEngine.BubbleFormationPhase.STABILIZE).ToList();
            List<Sample> pressurize = twoPhase.Where(s => s.Phase == HeatupSimEngine.BubbleFormationPhase.PRESSURIZE).ToList();
            var twoPhaseRates = new List<(Sample sample, float dPdt)>();
            for (int i = 1; i < run.Samples.Count; i++)
            {
                Sample prev = run.Samples[i - 1];
                Sample curr = run.Samples[i];
                bool prevIn = prev.Phase == HeatupSimEngine.BubbleFormationPhase.DRAIN
                           || prev.Phase == HeatupSimEngine.BubbleFormationPhase.STABILIZE
                           || prev.Phase == HeatupSimEngine.BubbleFormationPhase.PRESSURIZE;
                bool currIn = curr.Phase == HeatupSimEngine.BubbleFormationPhase.DRAIN
                           || curr.Phase == HeatupSimEngine.BubbleFormationPhase.STABILIZE
                           || curr.Phase == HeatupSimEngine.BubbleFormationPhase.PRESSURIZE;
                if (!prevIn || !currIn)
                    continue;

                float dt = curr.T - prev.T;
                if (dt <= 1e-6f)
                    continue;
                twoPhaseRates.Add((curr, (curr.P - prev.P) / dt));
            }

            bool cs0026Pass;
            bool cs0027Pass;
            bool cs0028Pass;
            bool cs0029Pass;
            bool cs0030Pass;
            string cs0026Reason;
            string cs0027Reason;
            string cs0028Reason;
            string cs0029Reason;
            string cs0030Reason;

            if (drain.Count == 0)
            {
                cs0026Pass = false;
                cs0026Reason = "No DRAIN samples observed.";
            }
            else
            {
                float startP = drain.First().P;
                float maxP = twoPhase.Max(s => s.P);
                float rise = maxP - startP;
                float maxRate = twoPhaseRates.Count > 0 ? twoPhaseRates.Max(x => Mathf.Abs(x.dPdt)) : 0f;
                cs0026Pass = rise >= -1f && rise <= 120f && maxRate <= 120f;
                cs0026Reason = $"start={startP:F1} psia, max={maxP:F1} psia, rise={rise:F1} psi, max|dP/dt|={maxRate:F1} psi/hr";
            }

            int iDetect = run.Samples.FindIndex(s => s.Phase == HeatupSimEngine.BubbleFormationPhase.DETECTION);
            int iVerify = run.Samples.FindIndex(s => s.Phase == HeatupSimEngine.BubbleFormationPhase.VERIFICATION);
            int iDrain = run.Samples.FindIndex(s => s.Phase == HeatupSimEngine.BubbleFormationPhase.DRAIN);
            int iStab = run.Samples.FindIndex(s => s.Phase == HeatupSimEngine.BubbleFormationPhase.STABILIZE);
            int iPress = run.Samples.FindIndex(s => s.Phase == HeatupSimEngine.BubbleFormationPhase.PRESSURIZE);
            int iComplete = run.Samples.FindIndex(s => s.Phase == HeatupSimEngine.BubbleFormationPhase.COMPLETE);
            bool ordered = iDetect >= 0 && iVerify > iDetect && iDrain > iVerify && iStab > iDrain && iPress > iStab && iComplete >= iPress;
            bool bubbleFlagAligned = iComplete >= 0 && run.Samples.Skip(iComplete).All(s => s.BubbleFormed);
            float detectionSatDelta = iDetect >= 0 ? Mathf.Abs(run.Samples[iDetect].TPzr - run.Samples[iDetect].Tsat) : float.MaxValue;
            cs0028Pass = ordered && bubbleFlagAligned && detectionSatDelta <= 2f;
            cs0028Reason = $"ordered={ordered}, bubbleAligned={bubbleFlagAligned}, |T_pzr-T_sat|@DETECTION={detectionSatDelta:F2}F";

            if (drain.Count == 0 || stabilize.Count == 0 || pressurize.Count == 0)
            {
                cs0027Pass = false;
                cs0027Reason = "Missing one or more required phases (DRAIN/STABILIZE/PRESSURIZE).";
            }
            else
            {
                float drainDrop = drain.First().PzrLevel - drain.Last().PzrLevel;
                float stabilizeMeanLevel = stabilize.Average(s => s.PzrLevel);
                float pressurizeMinP = pressurize.Min(s => s.P);
                cs0027Pass = drainDrop >= 10f
                             && stabilizeMeanLevel >= PlantConstants.PZR_LEVEL_AFTER_BUBBLE - 2f
                             && pressurizeMinP >= PlantConstants.BUBBLE_DRAIN_MIN_PRESSURE_PSIA;
                cs0027Reason = $"drainLevelDrop={drainDrop:F1}%, stabilizeMeanLevel={stabilizeMeanLevel:F1}%, pressurizeMinP={pressurizeMinP:F1} psia";
            }

            List<(Sample sample, float dPdt)> lowHeatTwoPhase = twoPhaseRates
                .Where(x => Mathf.Abs(x.sample.TRcsRate) < 1f)
                .ToList();
            if (lowHeatTwoPhase.Count == 0)
            {
                cs0029Pass = false;
                cs0029Reason = "No two-phase low-heat samples (|RCS heat rate| < 1 F/hr).";
            }
            else
            {
                float maxRateLowHeat = lowHeatTwoPhase.Max(x => Mathf.Abs(x.dPdt));
                cs0029Pass = maxRateLowHeat <= 120f;
                cs0029Reason = $"samples={lowHeatTwoPhase.Count}, max|dP/dt|={maxRateLowHeat:F1} psi/hr at |dT_rcs/dt|<1 F/hr";
            }

            List<(Sample sample, float dPdt)> cvcsTwoPhase = twoPhaseRates
                .Where(x => Mathf.Abs(x.sample.Charging - x.sample.Letdown) > 1f)
                .ToList();
            if (cvcsTwoPhase.Count == 0)
            {
                cs0030Pass = false;
                cs0030Reason = "No two-phase CVCS sign-excursion samples.";
            }
            else
            {
                List<(Sample sample, float dPdt)> positiveNet = cvcsTwoPhase
                    .Where(x => (x.sample.Charging - x.sample.Letdown) > 0f)
                    .ToList();
                List<(Sample sample, float dPdt)> negativeNet = cvcsTwoPhase
                    .Where(x => (x.sample.Charging - x.sample.Letdown) < 0f)
                    .ToList();
                if (positiveNet.Count > 0 && negativeNet.Count > 0)
                {
                    float meanPositive = positiveNet.Average(x => x.dPdt);
                    float meanNegative = negativeNet.Average(x => x.dPdt);
                    float gap = meanPositive - meanNegative;
                    cs0030Pass = gap >= -5f;
                    cs0030Reason = $"mean(dP/dt|net>0)={meanPositive:F1}, mean(dP/dt|net<0)={meanNegative:F1}, gap={gap:F1} psi/hr";
                }
                else
                {
                    float mean = cvcsTwoPhase.Average(x => x.dPdt);
                    float variance = cvcsTwoPhase.Average(x => (x.dPdt - mean) * (x.dPdt - mean));
                    float sigma = Mathf.Sqrt(variance);
                    cs0030Pass = sigma <= 60f;
                    cs0030Reason = $"single-sign dataset (positive={positiveNet.Count}, negative={negativeNet.Count}), sigma(dP/dt)={sigma:F1} psi/hr";
                }
            }

            bool overall = cs0026Pass && cs0027Pass && cs0028Pass && cs0029Pass && cs0030Pass;

            sb.AppendLine("# IP-0021 Group 2 Checkpoint");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run steps: {run.Steps}");
            sb.AppendLine($"- Final sim time: {run.FinalTimeHr:F3} hr");
            sb.AppendLine($"- Log directory: `{run.LogDir}`");
            if (!string.IsNullOrWhiteSpace(run.ReportPath))
                sb.AppendLine($"- Heatup report: `{run.ReportPath}`");
            sb.AppendLine();
            sb.AppendLine("## CS Results");
            sb.AppendLine($"- CS-0026: {(cs0026Pass ? "PASS" : "FAIL")} | {cs0026Reason}");
            sb.AppendLine($"- CS-0028: {(cs0028Pass ? "PASS" : "FAIL")} | {cs0028Reason}");
            sb.AppendLine($"- CS-0029: {(cs0029Pass ? "PASS" : "FAIL")} | {cs0029Reason}");
            sb.AppendLine($"- CS-0030: {(cs0030Pass ? "PASS" : "FAIL")} | {cs0030Reason}");
            sb.AppendLine($"- CS-0027: {(cs0027Pass ? "PASS" : "FAIL")} | {cs0027Reason}");
            sb.AppendLine();
            sb.AppendLine($"## Group 2 Overall: {(overall ? "PASS" : "FAIL")}");

            File.WriteAllText(evidencePath, sb.ToString());
            if (!overall)
                throw new Exception("IP-0021 Group 2 checkpoint failed. See evidence file.");
        }

        private static void WriteGroup3Evidence(RunResult runA, RunResult runB, string evidencePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(evidencePath) ?? ".");
            var sb = new StringBuilder();

            Sample a = runA.Samples.LastOrDefault() ?? new Sample();
            Sample b = runB.Samples.LastOrDefault() ?? new Sample();

            float durationMinA = a.DrainDuration * 60f;
            float durationMinB = b.DrainDuration * 60f;
            float repeatDeltaMin = Mathf.Abs(durationMinA - durationMinB);
            float displacementTotalA = a.DrainSteam + a.DrainCvcs;
            float steamFracA = displacementTotalA > 1e-6f ? a.DrainSteam / displacementTotalA : 0f;

            bool cs0036Pass = durationMinA <= 60f;
            bool cs0072Pass = durationMinA <= 60f && durationMinB <= 60f && repeatDeltaMin <= 5f;
            bool cs0074Pass = a.DrainSteam > 0f && a.DrainCvcs > 0f && steamFracA >= 0.20f;

            bool levelPressureTransition = a.DrainTransitionReason == "LEVEL_PRESSURE_READY"
                                           && a.DrainExitPressure >= PlantConstants.BUBBLE_DRAIN_MIN_PRESSURE_PSIA
                                           && a.DrainExitLevel <= PlantConstants.PZR_LEVEL_AFTER_BUBBLE + 0.5f;
            bool timeoutTransition = a.DrainTransitionReason == "HARD_TIMEOUT"
                                     && a.DrainHardGate
                                     && durationMinA <= 60f;
            bool cs0075Pass = levelPressureTransition || timeoutTransition;

            bool overall = cs0036Pass && cs0072Pass && cs0074Pass && cs0075Pass;

            sb.AppendLine("# IP-0021 Group 3 Checkpoint");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run A steps: {runA.Steps}");
            sb.AppendLine($"- Run A final sim time: {runA.FinalTimeHr:F3} hr");
            sb.AppendLine($"- Run A log directory: `{runA.LogDir}`");
            if (!string.IsNullOrWhiteSpace(runA.ReportPath))
                sb.AppendLine($"- Run A heatup report: `{runA.ReportPath}`");
            sb.AppendLine($"- Run B steps: {runB.Steps}");
            sb.AppendLine($"- Run B final sim time: {runB.FinalTimeHr:F3} hr");
            sb.AppendLine($"- Run B log directory: `{runB.LogDir}`");
            if (!string.IsNullOrWhiteSpace(runB.ReportPath))
                sb.AppendLine($"- Run B heatup report: `{runB.ReportPath}`");
            sb.AppendLine();
            sb.AppendLine("## CS Results");
            sb.AppendLine($"- CS-0036: {(cs0036Pass ? "PASS" : "FAIL")} | drainDurationA={durationMinA:F1} min (hard limit <=60)");
            sb.AppendLine($"- CS-0072: {(cs0072Pass ? "PASS" : "FAIL")} | drainDurationB={durationMinB:F1} min, repeatDelta={repeatDeltaMin:F1} min");
            sb.AppendLine($"- CS-0074: {(cs0074Pass ? "PASS" : "FAIL")} | steam={a.DrainSteam:F1} lbm, cvcs={a.DrainCvcs:F1} lbm, steamFrac={steamFracA:F2}");
            sb.AppendLine($"- CS-0075: {(cs0075Pass ? "PASS" : "FAIL")} | transition={a.DrainTransitionReason}, hardGate={a.DrainHardGate}, pressureBandMaintained={a.DrainPressureBandOK}, exitP={a.DrainExitPressure:F1} psia, exitLevel={a.DrainExitLevel:F1}%");
            sb.AppendLine();
            sb.AppendLine($"## Group 3 Overall: {(overall ? "PASS" : "FAIL")}");

            File.WriteAllText(evidencePath, sb.ToString());
            if (!overall)
                throw new Exception("IP-0021 Group 3 checkpoint failed. See evidence file.");
        }

        private static void WriteGroup4Evidence(RunResult run, string evidencePath, string root)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(evidencePath) ?? ".");
            var sb = new StringBuilder();

            string bubbleFile = Path.Combine(root, "Assets", "Scripts", "Validation", "HeatupSimEngine.BubbleFormation.cs");
            string bubbleText = File.Exists(bubbleFile) ? File.ReadAllText(bubbleFile) : string.Empty;
            bool hasUpdateInitCall = bubbleText.Contains("CVCSController.Initialize(");
            bool reachedPostDrainPhases = run.Samples.Any(s =>
                s.Phase == HeatupSimEngine.BubbleFormationPhase.STABILIZE
                || s.Phase == HeatupSimEngine.BubbleFormationPhase.PRESSURIZE
                || s.Phase == HeatupSimEngine.BubbleFormationPhase.COMPLETE);
            bool cs0059Pass = !hasUpdateInitCall && reachedPostDrainPhases;
            string cs0059Reason = $"updateInitCallPresent={hasUpdateInitCall}, reachedPostDrainPhases={reachedPostDrainPhases}";

            float[] pressures = { 100f, 300f, 600f, 1200f, 2250f };
            float[] deltaTs = { 5f, 15f, 40f };
            float[] heights = { 1f, 5f, 20f };
            var htcValues = new List<float>();
            bool finiteAll = true;
            bool boundedAll = true;
            bool monotonicHeight = true;

            foreach (float p in pressures)
            {
                foreach (float dT in deltaTs)
                {
                    float tSat = WaterProperties.SaturationTemperature(p);
                    float hAt1 = HeatTransfer.CondensingHTC(p, tSat - dT, heights[0]);
                    float hAt20 = HeatTransfer.CondensingHTC(p, tSat - dT, heights[2]);
                    if (hAt1 < hAt20)
                        monotonicHeight = false;

                    foreach (float h in heights)
                    {
                        float htc = HeatTransfer.CondensingHTC(p, tSat - dT, h);
                        htcValues.Add(htc);
                        if (float.IsNaN(htc) || float.IsInfinity(htc))
                            finiteAll = false;
                        if (htc < 75f || htc > 2500f)
                            boundedAll = false;
                    }
                }
            }

            float htcMin = htcValues.Count > 0 ? htcValues.Min() : 0f;
            float htcMax = htcValues.Count > 0 ? htcValues.Max() : 0f;
            bool cs0069Pass = finiteAll && boundedAll && monotonicHeight;
            string cs0069Reason = $"finite={finiteAll}, bounded={boundedAll}, monotonicHeight={monotonicHeight}, min={htcMin:F1}, max={htcMax:F1} BTU/(hr*ft^2*F)";

            bool overall = cs0059Pass && cs0069Pass;

            sb.AppendLine("# IP-0021 Group 4 Checkpoint");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run steps: {run.Steps}");
            sb.AppendLine($"- Final sim time: {run.FinalTimeHr:F3} hr");
            sb.AppendLine($"- Log directory: `{run.LogDir}`");
            if (!string.IsNullOrWhiteSpace(run.ReportPath))
                sb.AppendLine($"- Heatup report: `{run.ReportPath}`");
            sb.AppendLine($"- Lifecycle source file: `{bubbleFile}`");
            sb.AppendLine();
            sb.AppendLine("## CS Results");
            sb.AppendLine($"- CS-0059: {(cs0059Pass ? "PASS" : "FAIL")} | {cs0059Reason}");
            sb.AppendLine($"- CS-0069: {(cs0069Pass ? "PASS" : "FAIL")} | {cs0069Reason}");
            sb.AppendLine();
            sb.AppendLine($"## Group 4 Overall: {(overall ? "PASS" : "FAIL")}");

            File.WriteAllText(evidencePath, sb.ToString());
            if (!overall)
                throw new Exception("IP-0021 Group 4 checkpoint failed. See evidence file.");
        }

        private static void WriteGroup5Disposition(RunResult run, string evidencePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(evidencePath) ?? ".");
            var sb = new StringBuilder();

            List<Sample> solid = run.Samples
                .Where(s => s.Solid && s.Phase == HeatupSimEngine.BubbleFormationPhase.NONE)
                .ToList();

            bool hasSolidWindow = solid.Count > 0;
            float minSolidLevel = hasSolidWindow ? solid.Min(s => s.PzrLevel) : 0f;
            bool cs0024NoCode = hasSolidWindow && minSolidLevel >= 99f;
            string cs0024Disposition = cs0024NoCode ? "CLOSE_NO_CODE" : "FIX_REQUIRED";
            string cs0024Reason = cs0024NoCode
                ? $"Solid window confirmed: min PZR level={minSolidLevel:F1}% with solid-regime state active."
                : "Solid-regime evidence does not maintain expected full-level behavior; investigate clamping/pathing.";

            int iDetect = run.Samples.FindIndex(s => s.Phase == HeatupSimEngine.BubbleFormationPhase.DETECTION);
            int iPre = iDetect > 0 ? iDetect - 1 : -1;
            float satDelta = iDetect >= 0 ? Mathf.Abs(run.Samples[iDetect].TPzr - run.Samples[iDetect].Tsat) : float.MaxValue;
            float pressureJump = (iDetect >= 0 && iPre >= 0)
                ? Mathf.Abs(run.Samples[iDetect].P - run.Samples[iPre].P)
                : float.MaxValue;
            bool cs0025NoCode = iDetect >= 0 && satDelta <= 1f && pressureJump <= 2f;
            string cs0025Disposition = cs0025NoCode ? "CLOSE_NO_CODE" : "FIX_REQUIRED";
            string cs0025Reason = cs0025NoCode
                ? $"Detection aligns with saturation: |T_pzr-T_sat|={satDelta:F2}F, pressure jump={pressureJump:F2} psi."
                : $"Detection misalignment observed: |T_pzr-T_sat|={satDelta:F2}F, pressure jump={pressureJump:F2} psi.";

            sb.AppendLine("# IP-0021 Group 5 Disposition");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run steps: {run.Steps}");
            sb.AppendLine($"- Final sim time: {run.FinalTimeHr:F3} hr");
            sb.AppendLine($"- Log directory: `{run.LogDir}`");
            if (!string.IsNullOrWhiteSpace(run.ReportPath))
                sb.AppendLine($"- Heatup report: `{run.ReportPath}`");
            sb.AppendLine();
            sb.AppendLine("## Disposition");
            sb.AppendLine($"- CS-0024: {cs0024Disposition} | {cs0024Reason}");
            sb.AppendLine($"- CS-0025: {cs0025Disposition} | {cs0025Reason}");

            File.WriteAllText(evidencePath, sb.ToString());
        }

        private static void PrepareLogDirectory(string logDir)
        {
            Directory.CreateDirectory(logDir);
            foreach (string f in Directory.GetFiles(logDir, "*.txt"))
                File.Delete(f);
        }
    }
}
