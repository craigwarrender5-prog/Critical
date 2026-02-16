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
    public static class IP0022CheckpointRunner
    {
        private const float DtHr = 1f / 360f;
        private const float IntervalLogHr = 0.25f;
        private const float MaxSimHr = 18f;
        private const int MaxSteps = 300000;

        private sealed class Sample
        {
            public float T;
            public float P;
            public float TRcs;
            public float PzrLevel;
            public float Charging;
            public float Letdown;
            public float ChargingToRcs;
            public float VctConservationErrGal;
            public HeatupSimEngine.BubbleFormationPhase Phase;
            public bool CcpStarted;
            public float DrainSteamLbm;
            public float DrainCvcsLbm;
            public float DrainDurationHr;
            public string DrainPolicyMode = string.Empty;
            public float DrainLetdownGpm;
            public float DrainChargingGpm;
            public float DrainNetOutflowGpm;
            public float CvcsThermalMixingMW;
            public float CvcsThermalMixingDeltaF;
        }

        private sealed class RunResult
        {
            public readonly List<Sample> Samples = new List<Sample>();
            public string LogDir = string.Empty;
            public string ReportPath = string.Empty;
            public float FinalTimeHr;
            public int Steps;
        }

        private sealed class Group1Eval
        {
            public bool Cs0039Pass;
            public string Cs0039Reason = string.Empty;
            public bool Overall => Cs0039Pass;
        }

        private sealed class Group2Eval
        {
            public bool Cs0073Pass;
            public bool Cs0076Pass;
            public string Cs0073Reason = string.Empty;
            public string Cs0076Reason = string.Empty;
            public bool Overall => Cs0073Pass && Cs0076Pass;
        }

        private sealed class Group3Eval
        {
            public bool Cs0035Pass;
            public string Cs0035Reason = string.Empty;
            public bool Overall => Cs0035Pass;
        }

        [MenuItem("Critical/Run IP-0022 All Checkpoints + Recommendation")]
        public static void RunAllCheckpointsAndRecommendation()
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string logDir = Path.Combine(root, "HeatupLogs", $"IP-0022_All_{stamp}");

            string group1Path = Path.Combine(root, "Governance", "Issues",
                $"IP-0022_Group1_Checkpoint_{DateTime.Now:yyyy-MM-dd_HHmmss}.md");
            string group2Path = Path.Combine(root, "Governance", "Issues",
                $"IP-0022_Group2_Checkpoint_{DateTime.Now:yyyy-MM-dd_HHmmss}.md");
            string group3Path = Path.Combine(root, "Governance", "Issues",
                $"IP-0022_Group3_Checkpoint_{DateTime.Now:yyyy-MM-dd_HHmmss}.md");
            string stageDPath = Path.Combine(root, "Governance", "Issues",
                $"IP-0022_StageD_DomainValidation_{DateTime.Now:yyyy-MM-dd_HHmmss}.md");
            string stageEPath = Path.Combine(root, "Governance", "Issues",
                $"IP-0022_StageE_SystemRegression_{DateTime.Now:yyyy-MM-dd_HHmmss}.md");
            string recommendationPath = Path.Combine(root, "Governance", "ImplementationReports",
                $"IP-0022_Closure_Recommendation_{DateTime.Now:yyyy-MM-dd}.md");

            RunResult run = ExecuteDeterministicRun(logDir);
            Group1Eval g1 = EvaluateGroup1(run);
            Group2Eval g2 = EvaluateGroup2(run);
            Group3Eval g3 = EvaluateGroup3(run);

            WriteGroup1Evidence(run, g1, group1Path);
            WriteGroup2Evidence(run, g2, group2Path);
            WriteGroup3Evidence(run, g3, group3Path);
            WriteStageD(stageDPath, group1Path, group2Path, group3Path, g1, g2, g3);
            WriteStageE(stageEPath, run, group1Path, group2Path, group3Path, g1, g2, g3);
            WriteRecommendation(recommendationPath, stageDPath, stageEPath, g1, g2, g3);

            bool passAll = g1.Overall && g2.Overall && g3.Overall;
            Debug.Log($"[IP-0022] Group1={g1.Overall} Group2={g2.Overall} Group3={g3.Overall}");
            Debug.Log($"[IP-0022] Stage D: {stageDPath}");
            Debug.Log($"[IP-0022] Stage E: {stageEPath}");
            Debug.Log($"[IP-0022] Recommendation: {recommendationPath}");
            if (!passAll)
                throw new Exception("IP-0022 checkpoint suite failed. See evidence artifacts.");
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
                throw new MissingMethodException("IP-0022 runner could not access required HeatupSimEngine members.");

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
                TRcs = e.T_rcs,
                PzrLevel = e.pzrLevel,
                Charging = e.chargingFlow,
                Letdown = e.letdownFlow,
                ChargingToRcs = e.chargingToRCS,
                VctConservationErrGal = e.massConservationError,
                Phase = e.bubblePhase,
                CcpStarted = e.ccpStarted,
                DrainSteamLbm = e.drainSteamDisplacement_lbm,
                DrainCvcsLbm = e.drainCvcsTransfer_lbm,
                DrainDurationHr = e.drainDuration_hr,
                DrainPolicyMode = e.drainCvcsPolicyMode ?? string.Empty,
                DrainLetdownGpm = e.drainLetdownFlow_gpm,
                DrainChargingGpm = e.drainChargingFlow_gpm,
                DrainNetOutflowGpm = e.drainNetOutflowFlow_gpm,
                CvcsThermalMixingMW = e.cvcsThermalMixing_MW,
                CvcsThermalMixingDeltaF = e.cvcsThermalMixingDeltaF
            };
        }

        private static Group1Eval EvaluateGroup1(RunResult run)
        {
            var eval = new Group1Eval();
            List<Sample> window = run.Samples.Where(s => s.T >= 8f).ToList();
            if (window.Count == 0)
            {
                eval.Cs0039Pass = false;
                eval.Cs0039Reason = "No post-bubble window samples available.";
                return eval;
            }

            float maxErr = window.Max(s => s.VctConservationErrGal);
            Sample near1575 = run.Samples
                .OrderBy(s => Mathf.Abs(s.T - 15.75f))
                .FirstOrDefault();
            float err1575 = near1575?.VctConservationErrGal ?? float.MaxValue;

            List<Sample> tail = run.Samples.Where(s => s.T >= 12f).ToList();
            float growthRate = 0f;
            if (tail.Count >= 2)
            {
                Sample first = tail.First();
                Sample last = tail.Last();
                float dt = Mathf.Max(1e-6f, last.T - first.T);
                growthRate = (last.VctConservationErrGal - first.VctConservationErrGal) / dt;
            }

            bool bounded = maxErr <= 300f;
            bool targetWindow = err1575 <= 250f;
            bool nonGrowing = growthRate <= 10f;
            eval.Cs0039Pass = bounded && targetWindow && nonGrowing;
            eval.Cs0039Reason =
                $"maxErr={maxErr:F1} gal, err@15.75hr={err1575:F1} gal, tailGrowthRate={growthRate:F1} gal/hr";
            return eval;
        }

        private static Group2Eval EvaluateGroup2(RunResult run)
        {
            var eval = new Group2Eval();
            List<Sample> drain = run.Samples
                .Where(s => s.Phase == HeatupSimEngine.BubbleFormationPhase.DRAIN)
                .ToList();
            List<Sample> postCcp = drain
                .Where(s => s.CcpStarted || s.DrainChargingGpm > 0.5f)
                .ToList();

            if (drain.Count == 0 || postCcp.Count == 0)
            {
                eval.Cs0073Pass = false;
                eval.Cs0076Pass = false;
                eval.Cs0073Reason = "Missing DRAIN or post-CCP samples.";
                eval.Cs0076Reason = "Missing DRAIN or post-CCP samples.";
                return eval;
            }

            float netMin = postCcp.Min(s => s.DrainNetOutflowGpm);
            float netMax = postCcp.Max(s => s.DrainNetOutflowGpm);
            float netRange = netMax - netMin;
            float steam = run.Samples.Last().DrainSteamLbm;
            float cvcs = run.Samples.Last().DrainCvcsLbm;
            float total = steam + cvcs;
            float steamFrac = total > 1e-6f ? steam / total : 0f;

            bool dynamicNet = netRange >= 10f;
            bool displacementNotCvcsOnly = steamFrac >= 0.20f;
            eval.Cs0073Pass = dynamicNet && displacementNotCvcsOnly;
            eval.Cs0073Reason =
                $"postCcpSamples={postCcp.Count}, netRange={netRange:F1} gpm, steamFrac={steamFrac:F2}, steam={steam:F1} lbm, cvcs={cvcs:F1} lbm";

            bool policyDynamic = drain.Any(s =>
                string.Equals(s.DrainPolicyMode, "PROCEDURE_ALIGNED_DYNAMIC", StringComparison.Ordinal));
            float meanLetdown = postCcp.Average(s => s.DrainLetdownGpm);
            float maxLetdown = drain.Max(s => s.DrainLetdownGpm);
            bool fixedPolicyLike = postCcp.All(s =>
                Mathf.Abs(s.DrainLetdownGpm - 75f) < 1f &&
                Mathf.Abs(s.DrainChargingGpm - 44f) < 1f);

            eval.Cs0076Pass = policyDynamic && meanLetdown >= 80f && maxLetdown >= 100f && !fixedPolicyLike;
            eval.Cs0076Reason =
                $"policyDynamic={policyDynamic}, meanLetdown={meanLetdown:F1} gpm, maxLetdown={maxLetdown:F1} gpm, fixedPolicyLike={fixedPolicyLike}";
            return eval;
        }

        private static Group3Eval EvaluateGroup3(RunResult run)
        {
            var eval = new Group3Eval();
            List<Sample> active = run.Samples
                .Where(s => s.ChargingToRcs > 5f && s.TRcs > 120f)
                .ToList();
            List<Sample> idle = run.Samples
                .Where(s => s.ChargingToRcs < 1f)
                .ToList();

            if (active.Count == 0)
            {
                eval.Cs0035Pass = false;
                eval.Cs0035Reason = "No active charging samples for thermal-mixing evaluation.";
                return eval;
            }

            float meanMw = active.Average(s => s.CvcsThermalMixingMW);
            float minMw = active.Min(s => s.CvcsThermalMixingMW);
            float maxIdleAbsMw = idle.Count > 0 ? idle.Max(s => Mathf.Abs(s.CvcsThermalMixingMW)) : 0f;
            float cumulativeMJ = IntegrateMixingEnergyMJ(run.Samples);

            bool coolingSign = meanMw < -0.01f && minMw < -0.05f;
            bool idleNearZero = maxIdleAbsMw <= 0.05f;
            bool nonTrivial = cumulativeMJ < -10f;
            eval.Cs0035Pass = coolingSign && idleNearZero && nonTrivial;
            eval.Cs0035Reason =
                $"activeSamples={active.Count}, meanMix={meanMw:F3} MW, minMix={minMw:F3} MW, cumulativeMix={cumulativeMJ:F1} MJ, maxIdle|mix|={maxIdleAbsMw:F3} MW";
            return eval;
        }

        private static float IntegrateMixingEnergyMJ(List<Sample> samples)
        {
            if (samples.Count < 2)
                return 0f;

            float total = 0f;
            for (int i = 1; i < samples.Count; i++)
            {
                Sample a = samples[i - 1];
                Sample b = samples[i];
                float dtHr = b.T - a.T;
                if (dtHr <= 1e-7f)
                    continue;
                float avgMw = 0.5f * (a.CvcsThermalMixingMW + b.CvcsThermalMixingMW);
                total += avgMw * dtHr * 3600f;
            }

            return total;
        }

        private static void WriteGroup1Evidence(RunResult run, Group1Eval eval, string evidencePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(evidencePath) ?? ".");
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0022 Group 1 Checkpoint");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run steps: {run.Steps}");
            sb.AppendLine($"- Final sim time: {run.FinalTimeHr:F3} hr");
            sb.AppendLine($"- Log directory: `{run.LogDir}`");
            if (!string.IsNullOrWhiteSpace(run.ReportPath))
                sb.AppendLine($"- Heatup report: `{run.ReportPath}`");
            sb.AppendLine();
            sb.AppendLine("## CS Results");
            sb.AppendLine($"- CS-0039: {(eval.Cs0039Pass ? "PASS" : "FAIL")} | {eval.Cs0039Reason}");
            sb.AppendLine();
            sb.AppendLine($"## Group 1 Overall: {(eval.Overall ? "PASS" : "FAIL")}");
            File.WriteAllText(evidencePath, sb.ToString());
        }

        private static void WriteGroup2Evidence(RunResult run, Group2Eval eval, string evidencePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(evidencePath) ?? ".");
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0022 Group 2 Checkpoint");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run steps: {run.Steps}");
            sb.AppendLine($"- Final sim time: {run.FinalTimeHr:F3} hr");
            sb.AppendLine($"- Log directory: `{run.LogDir}`");
            if (!string.IsNullOrWhiteSpace(run.ReportPath))
                sb.AppendLine($"- Heatup report: `{run.ReportPath}`");
            sb.AppendLine();
            sb.AppendLine("## CS Results");
            sb.AppendLine($"- CS-0073: {(eval.Cs0073Pass ? "PASS" : "FAIL")} | {eval.Cs0073Reason}");
            sb.AppendLine($"- CS-0076: {(eval.Cs0076Pass ? "PASS" : "FAIL")} | {eval.Cs0076Reason}");
            sb.AppendLine();
            sb.AppendLine($"## Group 2 Overall: {(eval.Overall ? "PASS" : "FAIL")}");
            File.WriteAllText(evidencePath, sb.ToString());
        }

        private static void WriteGroup3Evidence(RunResult run, Group3Eval eval, string evidencePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(evidencePath) ?? ".");
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0022 Group 3 Checkpoint");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run steps: {run.Steps}");
            sb.AppendLine($"- Final sim time: {run.FinalTimeHr:F3} hr");
            sb.AppendLine($"- Log directory: `{run.LogDir}`");
            if (!string.IsNullOrWhiteSpace(run.ReportPath))
                sb.AppendLine($"- Heatup report: `{run.ReportPath}`");
            sb.AppendLine();
            sb.AppendLine("## CS Results");
            sb.AppendLine($"- CS-0035: {(eval.Cs0035Pass ? "PASS" : "FAIL")} | {eval.Cs0035Reason}");
            sb.AppendLine();
            sb.AppendLine($"## Group 3 Overall: {(eval.Overall ? "PASS" : "FAIL")}");
            File.WriteAllText(evidencePath, sb.ToString());
        }

        private static void WriteStageD(
            string path,
            string group1Path,
            string group2Path,
            string group3Path,
            Group1Eval g1,
            Group2Eval g2,
            Group3Eval g3)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0022 Stage D - Domain Validation (DP-0004)");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("- Scope: DP-0004 only (4 in-scope CS)");
            sb.AppendLine();
            sb.AppendLine("## Run Artifacts");
            sb.AppendLine($"- Group 1 checkpoint: `{ToRepoRelative(group1Path)}`");
            sb.AppendLine($"- Group 2 checkpoint: `{ToRepoRelative(group2Path)}`");
            sb.AppendLine($"- Group 3 checkpoint: `{ToRepoRelative(group3Path)}`");
            sb.AppendLine();
            sb.AppendLine("## Per-CS Matrix");
            sb.AppendLine("| CS ID | Result | Evidence |");
            sb.AppendLine("|---|---|---|");
            sb.AppendLine($"| CS-0039 | {(g1.Cs0039Pass ? "PASS" : "FAIL")} | `{ToRepoRelative(group1Path)}` |");
            sb.AppendLine($"| CS-0073 | {(g2.Cs0073Pass ? "PASS" : "FAIL")} | `{ToRepoRelative(group2Path)}` |");
            sb.AppendLine($"| CS-0076 | {(g2.Cs0076Pass ? "PASS" : "FAIL")} | `{ToRepoRelative(group2Path)}` |");
            sb.AppendLine($"| CS-0035 | {(g3.Cs0035Pass ? "PASS" : "FAIL")} | `{ToRepoRelative(group3Path)}` |");
            sb.AppendLine();
            bool overall = g1.Overall && g2.Overall && g3.Overall;
            sb.AppendLine($"## Stage D Outcome");
            sb.AppendLine($"- Domain validation status: {(overall ? "PASS" : "FAIL")}");
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteStageE(
            string path,
            RunResult run,
            string group1Path,
            string group2Path,
            string group3Path,
            Group1Eval g1,
            Group2Eval g2,
            Group3Eval g3)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            var sb = new StringBuilder();
            bool overall = g1.Overall && g2.Overall && g3.Overall;
            sb.AppendLine("# IP-0022 Stage E - System Regression Validation (DP-0004 Scoped)");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("- Scope policy: no scope widening outside DP-0004.");
            sb.AppendLine();
            sb.AppendLine("## Regression Execution Evidence");
            sb.AppendLine($"- Deterministic run log directory: `{run.LogDir}`");
            if (!string.IsNullOrWhiteSpace(run.ReportPath))
                sb.AppendLine($"- Heatup report: `{run.ReportPath}`");
            sb.AppendLine($"- Group 1 checkpoint: `{ToRepoRelative(group1Path)}`");
            sb.AppendLine($"- Group 2 checkpoint: `{ToRepoRelative(group2Path)}`");
            sb.AppendLine($"- Group 3 checkpoint: `{ToRepoRelative(group3Path)}`");
            sb.AppendLine();
            sb.AppendLine("## Stage E Outcome");
            sb.AppendLine($"- Within DP-0004 scope, regression status: {(overall ? "NO UNACCEPTABLE REGRESSION SIGNAL" : "REGRESSION RISK PRESENT")}");
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteRecommendation(
            string path,
            string stageDPath,
            string stageEPath,
            Group1Eval g1,
            Group2Eval g2,
            Group3Eval g3)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            bool overall = g1.Overall && g2.Overall && g3.Overall;
            string recommendation = overall ? "CLOSE_RECOMMENDED" : "KEEP_OPEN";

            var sb = new StringBuilder();
            sb.AppendLine("# IP-0022 Closure Recommendation (DP-0004)");
            sb.AppendLine();
            sb.AppendLine($"- Date: {DateTime.Now:yyyy-MM-dd}");
            sb.AppendLine($"- Recommendation: **{recommendation}**");
            sb.AppendLine();
            sb.AppendLine("## Basis");
            sb.AppendLine($"- CS-0039: {(g1.Cs0039Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- CS-0073: {(g2.Cs0073Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- CS-0076: {(g2.Cs0076Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- CS-0035: {(g3.Cs0035Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- Stage D report: `{ToRepoRelative(stageDPath)}`");
            sb.AppendLine($"- Stage E report: `{ToRepoRelative(stageEPath)}`");
            sb.AppendLine();
            sb.AppendLine("## Residual Risk");
            sb.AppendLine("- DP-0001/DP-0002/DP-0003/DP-0005 dedicated runners are not executed in this recommendation cycle due explicit DP-0004 scope boundary.");
            File.WriteAllText(path, sb.ToString());
        }

        private static string ToRepoRelative(string absolutePath)
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            if (absolutePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return absolutePath.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Replace('\\', '/');
            return absolutePath.Replace('\\', '/');
        }

        private static void PrepareLogDirectory(string logDir)
        {
            Directory.CreateDirectory(logDir);
            foreach (string f in Directory.GetFiles(logDir, "*.txt"))
                File.Delete(f);
        }
    }
}
