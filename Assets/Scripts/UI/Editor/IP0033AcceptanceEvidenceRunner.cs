using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Critical.Physics;
using Critical.Physics.Tests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using Critical.Validation;
namespace Critical.Validation
{
    public static class IP0033AcceptanceEvidenceRunner
    {
        private const float DtHr = 1f / 360f;
        private const float TotalRunHours = 16f;
        private const float At02RequiredWindowHours = 4f;
        private const float At02BalancedFlowBandGpm = 0.25f;
        private const float At02MaxDriftPercent = 0.01f;
        private const float At03MaxDiscontinuityLb = 1f;
        private const float At08MaxSpikePerStepPercent = 0.5f;
        private const int At08EvaluationSteps = 120; // 20 minutes at 10 s steps
        private const int MaxSteps = 12000;
        private const int FixedSeed = 330033;

        [MenuItem("Critical/Run IP-0033 Acceptance Evidence (AT-02/03/08)")]
        public static void Run()
        {
            string repoRoot = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string logDir = Path.Combine(repoRoot, "HeatupLogs", $"IP-0033_AcceptanceEvidence_{stamp}");
            Directory.CreateDirectory(logDir);

            AcceptanceSimulationEvidence evidence = ExecuteScenario();
            evidence.EvidenceId = $"IP-0033-{stamp}";
            evidence.CapturedAtUtc = DateTime.UtcNow;
            evidence.Source = ToRepoRelative(logDir);

            AcceptanceSimulationEvidenceStore.Set(evidence);
            IntegrationTestSummary summary = AcceptanceTests_v5_4_0.RunAllTests();
            string summaryText = AcceptanceTests_v5_4_0.FormatSummary(summary);

            string summaryPath = Path.Combine(logDir, "acceptance_summary.txt");
            File.WriteAllText(summaryPath, summaryText);
            WriteEvidenceCsv(Path.Combine(logDir, "acceptance_evidence.csv"), evidence);

            string issuePath = Path.Combine(
                repoRoot,
                "Governance",
                "Issues",
                $"IP-0033_StageD_AcceptanceEvidence_{stamp}.md");
            WriteIssue(issuePath, stamp, logDir, evidence, summary);

            Debug.Log($"[IP-0033][AT-EVIDENCE] Summary: {ToRepoRelative(summaryPath)}");
            Debug.Log($"[IP-0033][AT-EVIDENCE] Issue: {ToRepoRelative(issuePath)}");

            if (!summary.AllPassed)
            {
                throw new Exception(
                    $"IP-0033 acceptance evidence run failed: {summary.PassedTests}/{summary.TotalTests} passed.");
            }
        }

        private static AcceptanceSimulationEvidence ExecuteScenario()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
            HeatupSimEngine engine = UnityEngine.Object.FindObjectOfType<HeatupSimEngine>();
            if (engine == null)
                throw new InvalidOperationException("HeatupSimEngine not found in MainScene.");

            Type engineType = typeof(HeatupSimEngine);
            MethodInfo initialize = engineType.GetMethod("InitializeSimulation", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo runStep = engineType.GetMethod("RunPhysicsStep", BindingFlags.Instance | BindingFlags.NonPublic);
            if (initialize == null || runStep == null)
                throw new MissingMethodException("Required HeatupSimEngine private methods were not found.");

            ConfigureDeterministicInputs(engine);
            initialize.Invoke(engine, null);

            var evidence = new AcceptanceSimulationEvidence();
            evidence.AT02 = new At02Evidence();
            evidence.AT03 = new At03Evidence();
            evidence.AT08 = new At08Evidence();

            bool at02WindowActive = false;
            float at02WindowStartTimeHr = 0f;
            float at02StartMassLb = 0f;

            bool at08Tracking = false;
            int at08StepsObserved = 0;
            int at08StepsRemaining = 0;
            float at08LastLevelPct = 0f;
            float at08MaxStepDeltaPct = 0f;

            float prevPrimaryLedgerLb = engine.primaryMassLedger_lb;
            int prevRcpCount = engine.rcpCount;
            bool prevSolidPressurizer = engine.solidPressurizer;
            bool prevBubbleFormed = engine.bubbleFormed;

            int totalSteps = Mathf.CeilToInt(TotalRunHours / DtHr);
            if (totalSteps > MaxSteps)
                throw new InvalidOperationException($"Configured run exceeds MaxSteps ({MaxSteps}).");

            for (int step = 1; step <= totalSteps; step++)
            {
                runStep.Invoke(engine, new object[] { DtHr });

                float currentMassLb = engine.primaryMassLedger_lb;
                bool inTwoPhase = engine.bubbleFormed && !engine.solidPressurizer;
                bool balancedCvcs = Mathf.Abs(engine.chargingFlow - engine.letdownFlow) <= At02BalancedFlowBandGpm;
                bool at02Candidate = inTwoPhase && balancedCvcs && engine.primaryMassStatus != "NOT_CHECKED";

                if (!evidence.AT02.Observed)
                {
                    if (at02Candidate)
                    {
                        if (!at02WindowActive)
                        {
                            at02WindowActive = true;
                            at02WindowStartTimeHr = engine.simTime;
                            at02StartMassLb = currentMassLb;
                        }
                        else if (engine.simTime - at02WindowStartTimeHr >= At02RequiredWindowHours)
                        {
                            float absDriftLb = Mathf.Abs(currentMassLb - at02StartMassLb);
                            float absDriftPct = at02StartMassLb > 1f
                                ? absDriftLb * 100f / Mathf.Abs(at02StartMassLb)
                                : float.PositiveInfinity;
                            evidence.AT02.Observed = true;
                            evidence.AT02.WindowHours = engine.simTime - at02WindowStartTimeHr;
                            evidence.AT02.StartMassLb = at02StartMassLb;
                            evidence.AT02.EndMassLb = currentMassLb;
                            evidence.AT02.AbsoluteDriftLb = absDriftLb;
                            evidence.AT02.AbsoluteDriftPercent = absDriftPct;
                            evidence.AT02.Passed = absDriftPct <= At02MaxDriftPercent;
                        }
                    }
                    else
                    {
                        at02WindowActive = false;
                    }
                }

                bool enteredTwoPhase = prevSolidPressurizer && !engine.solidPressurizer && engine.bubbleFormed;
                if (!evidence.AT03.Observed && enteredTwoPhase)
                {
                    float discontinuity = Mathf.Abs(currentMassLb - prevPrimaryLedgerLb);
                    evidence.AT03.Observed = true;
                    evidence.AT03.TransitionDiscontinuityLb = discontinuity;
                    evidence.AT03.Passed = discontinuity <= At03MaxDiscontinuityLb;
                }

                if (!at08Tracking && prevRcpCount == 0 && engine.rcpCount > 0)
                {
                    at08Tracking = true;
                    at08StepsObserved = 0;
                    at08StepsRemaining = At08EvaluationSteps;
                    at08LastLevelPct = engine.pzrLevel;
                    at08MaxStepDeltaPct = 0f;
                }

                if (at08Tracking)
                {
                    float deltaPct = Mathf.Abs(engine.pzrLevel - at08LastLevelPct);
                    if (deltaPct > at08MaxStepDeltaPct)
                        at08MaxStepDeltaPct = deltaPct;

                    at08LastLevelPct = engine.pzrLevel;
                    at08StepsObserved++;
                    at08StepsRemaining--;
                    if (at08StepsRemaining <= 0)
                    {
                        evidence.AT08.Observed = true;
                        evidence.AT08.WindowStepsEvaluated = at08StepsObserved;
                        evidence.AT08.MaxPzrLevelStepDeltaPercent = at08MaxStepDeltaPct;
                        evidence.AT08.Passed = at08MaxStepDeltaPct < At08MaxSpikePerStepPercent;
                        at08Tracking = false;
                    }
                }

                prevPrimaryLedgerLb = currentMassLb;
                prevRcpCount = engine.rcpCount;
                prevSolidPressurizer = engine.solidPressurizer;
                prevBubbleFormed = engine.bubbleFormed;
            }

            if (!evidence.AT08.Observed && at08Tracking)
            {
                evidence.AT08.Observed = at08StepsObserved > 0;
                evidence.AT08.WindowStepsEvaluated = at08StepsObserved;
                evidence.AT08.MaxPzrLevelStepDeltaPercent = at08MaxStepDeltaPct;
                evidence.AT08.Passed = at08MaxStepDeltaPct < At08MaxSpikePerStepPercent;
            }

            if (!evidence.AT03.Observed && prevBubbleFormed && !prevSolidPressurizer)
            {
                // Transition happened before sampling began; mark observed with conservative fail.
                evidence.AT03.Observed = true;
                evidence.AT03.TransitionDiscontinuityLb = float.PositiveInfinity;
                evidence.AT03.Passed = false;
            }

            return evidence;
        }

        private static void ConfigureDeterministicInputs(HeatupSimEngine engine)
        {
            UnityEngine.Random.InitState(FixedSeed);
            engine.enableAsyncLogWriter = false;
            engine.enableHighFrequencyPerfLogs = false;
            engine.enableWorkerThreadStepping = false;
            engine.runOnStart = false;
            engine.enableModularCoordinatorPath = true;
            engine.coldShutdownStart = true;
            engine.startTemperature = 100f;
            engine.startPressure = PlantConstants.PRESSURIZE_INITIAL_PRESSURE_PSIA;
            engine.startPZRLevel = 100f;
        }

        private static void WriteEvidenceCsv(string path, AcceptanceSimulationEvidence evidence)
        {
            var sb = new StringBuilder();
            sb.AppendLine("metric,value");
            sb.AppendLine($"evidence_id,{Csv(evidence.EvidenceId)}");
            sb.AppendLine($"captured_utc,{Csv(evidence.CapturedAtUtc.ToString("O", CultureInfo.InvariantCulture))}");
            sb.AppendLine($"source,{Csv(evidence.Source)}");
            sb.AppendLine($"at02_observed,{evidence.AT02.Observed}");
            sb.AppendLine($"at02_window_hr,{F(evidence.AT02.WindowHours)}");
            sb.AppendLine($"at02_start_mass_lb,{F(evidence.AT02.StartMassLb)}");
            sb.AppendLine($"at02_end_mass_lb,{F(evidence.AT02.EndMassLb)}");
            sb.AppendLine($"at02_abs_drift_lb,{F(evidence.AT02.AbsoluteDriftLb)}");
            sb.AppendLine($"at02_abs_drift_pct,{F(evidence.AT02.AbsoluteDriftPercent)}");
            sb.AppendLine($"at02_pass,{evidence.AT02.Passed}");
            sb.AppendLine($"at03_observed,{evidence.AT03.Observed}");
            sb.AppendLine($"at03_discontinuity_lb,{F(evidence.AT03.TransitionDiscontinuityLb)}");
            sb.AppendLine($"at03_pass,{evidence.AT03.Passed}");
            sb.AppendLine($"at08_observed,{evidence.AT08.Observed}");
            sb.AppendLine($"at08_window_steps,{evidence.AT08.WindowStepsEvaluated}");
            sb.AppendLine($"at08_max_step_delta_pct,{F(evidence.AT08.MaxPzrLevelStepDeltaPercent)}");
            sb.AppendLine($"at08_pass,{evidence.AT08.Passed}");
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteIssue(
            string issuePath,
            string stamp,
            string logDir,
            AcceptanceSimulationEvidence evidence,
            IntegrationTestSummary summary)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0033 Stage D - Acceptance Simulation Evidence (AT-02/03/08)");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run stamp: `{stamp}`");
            sb.AppendLine($"- Deterministic seed: `{FixedSeed}`");
            sb.AppendLine($"- Step size: `{DtHr:F6} hr`");
            sb.AppendLine($"- Result: {(summary.AllPassed ? "PASS" : "FAIL")} ({summary.PassedTests}/{summary.TotalTests})");
            sb.AppendLine();
            sb.AppendLine("## Runtime Evidence");
            sb.AppendLine($"- AT-02 observed: `{evidence.AT02.Observed}` pass: `{evidence.AT02.Passed}`");
            sb.AppendLine($"- AT-02 window: `{evidence.AT02.WindowHours:F3} hr` drift: `{evidence.AT02.AbsoluteDriftPercent:F5}%` ({evidence.AT02.AbsoluteDriftLb:F3} lb)");
            sb.AppendLine($"- AT-03 observed: `{evidence.AT03.Observed}` pass: `{evidence.AT03.Passed}`");
            sb.AppendLine($"- AT-03 discontinuity: `{evidence.AT03.TransitionDiscontinuityLb:F6} lb`");
            sb.AppendLine($"- AT-08 observed: `{evidence.AT08.Observed}` pass: `{evidence.AT08.Passed}`");
            sb.AppendLine($"- AT-08 max PZR delta: `{evidence.AT08.MaxPzrLevelStepDeltaPercent:F6}%` over `{evidence.AT08.WindowStepsEvaluated}` steps");
            sb.AppendLine();
            sb.AppendLine("## Artifacts");
            sb.AppendLine($"- Summary: `{ToRepoRelative(Path.Combine(logDir, "acceptance_summary.txt"))}`");
            sb.AppendLine($"- Evidence CSV: `{ToRepoRelative(Path.Combine(logDir, "acceptance_evidence.csv"))}`");
            sb.AppendLine();
            sb.AppendLine("## Thresholds");
            sb.AppendLine($"- AT-02 drift <= `{At02MaxDriftPercent:F3}%` across `{At02RequiredWindowHours:F1}` hr balanced two-phase window");
            sb.AppendLine($"- AT-03 transition discontinuity <= `{At03MaxDiscontinuityLb:F1}` lb");
            sb.AppendLine($"- AT-08 max frame delta < `{At08MaxSpikePerStepPercent:F1}%`");

            Directory.CreateDirectory(Path.GetDirectoryName(issuePath) ?? ".");
            File.WriteAllText(issuePath, sb.ToString());
        }

        private static string ToRepoRelative(string absolutePath)
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string normalizedRoot = root.Replace('\\', '/').TrimEnd('/');
            string normalizedPath = absolutePath.Replace('\\', '/');
            if (normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                return normalizedPath.Substring(normalizedRoot.Length).TrimStart('/');
            return normalizedPath;
        }

        private static string Csv(string value)
        {
            if (value == null)
                return string.Empty;
            if (value.Contains(",") || value.Contains("\""))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }

        private static string F(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}

