using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Critical.Physics;
using Critical.Simulation.Modular;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Critical.Validation
{
    public static class IP0025StageARunner
    {
        private const float DtHr = 1f / 360f; // 10 s deterministic step
        private const float RunHours = 1.0f;  // small deterministic equivalence window
        private const int MaxSteps = 100000;

        private const int FixedSeed = 250025;

        private const float PressureTolerancePsia = 0.001f;
        private const float PzrLevelTolerancePct = 0.0001f;
        private const float PrimaryMassToleranceLb = 0.001f;

        private sealed class StepSample
        {
            public int Step;
            public float TimeHr;
            public float PressurePsia;
            public float PzrLevelPct;
            public float PrimaryMassLb;
        }

        private sealed class RunResult
        {
            public string Label = string.Empty;
            public string LogDir = string.Empty;
            public readonly List<StepSample> Samples = new List<StepSample>();
            public bool CoordinatorPathUsedEveryStep;
            public bool LegacyPathUsedEveryStep;
        }

        private sealed class ComparisonResult
        {
            public bool Pass;
            public float MaxPressureErrorPsia;
            public float MaxPzrLevelErrorPct;
            public float MaxPrimaryMassErrorLb;
            public int WorstPressureStep;
            public int WorstPzrLevelStep;
            public int WorstPrimaryMassStep;
            public string FailureReason = string.Empty;
        }

        [MenuItem("Critical/Run IP-0025 Stage A Equivalence")]
        public static void RunStageAEquivalence()
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string runstamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string logDir = Path.Combine(root, "HeatupLogs", $"IP-0025_StageA_Equivalence_{runstamp}");
            Directory.CreateDirectory(logDir);

            try
            {
                RunResult baseline = ExecuteScenario(root, logDir, "LEGACY_BASELINE", enableCoordinatorPath: false);
                RunResult coordinator = ExecuteScenario(root, logDir, "COORDINATOR_LEGACY", enableCoordinatorPath: true);
                ComparisonResult comparison = CompareRuns(baseline, coordinator);

                string baselineCsv = Path.Combine(logDir, "baseline_samples.csv");
                string coordinatorCsv = Path.Combine(logDir, "coordinator_samples.csv");
                string compareCsv = Path.Combine(logDir, "comparison.csv");
                WriteSamplesCsv(baselineCsv, baseline.Samples);
                WriteSamplesCsv(coordinatorCsv, coordinator.Samples);
                WriteComparisonCsv(compareCsv, baseline.Samples, coordinator.Samples);

                string issuePath = Path.Combine(root, "Governance", "Issues", $"IP-0025_StageA_Equivalence_{runstamp}.md");
                WriteIssue(issuePath, runstamp, logDir, baseline, coordinator, comparison);

                Debug.Log($"[IP-0025][StageA] Artifact: {issuePath}");
                Debug.Log($"[IP-0025][StageA] Logs: {logDir}");

                if (!comparison.Pass)
                    throw new Exception($"IP-0025 Stage A equivalence failed: {comparison.FailureReason}");
            }
            finally
            {
                ModularFeatureFlags.ResetAll();
            }
        }

        private static RunResult ExecuteScenario(
            string root,
            string parentLogDir,
            string label,
            bool enableCoordinatorPath)
        {
            string scenarioDir = Path.Combine(parentLogDir, label);
            Directory.CreateDirectory(scenarioDir);

            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
            HeatupSimEngine engine = UnityEngine.Object.FindObjectOfType<HeatupSimEngine>();
            if (engine == null)
                throw new InvalidOperationException("HeatupSimEngine not found in MainScene.");

            Type engineType = typeof(HeatupSimEngine);
            MethodInfo initialize = engineType.GetMethod("InitializeSimulation", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo runStep = engineType.GetMethod("RunPhysicsStep", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo logPathField = engineType.GetField("logPath", BindingFlags.Instance | BindingFlags.NonPublic);
            if (initialize == null || runStep == null || logPathField == null)
                throw new MissingMethodException("IP-0025 Stage A runner missing required HeatupSimEngine methods/fields.");

            ConfigureDeterministicInputs(engine);
            logPathField.SetValue(engine, scenarioDir);

            ModularFeatureFlags.ResetAll();
            ModularFeatureFlags.EnableCoordinatorPath = enableCoordinatorPath;

            initialize.Invoke(engine, null);

            var result = new RunResult
            {
                Label = label,
                LogDir = scenarioDir,
                CoordinatorPathUsedEveryStep = true,
                LegacyPathUsedEveryStep = true
            };

            int totalSteps = Mathf.CeilToInt(RunHours / DtHr);
            if (totalSteps > MaxSteps)
                throw new InvalidOperationException($"Stage A runner exceeded MaxSteps ({MaxSteps}).");

            for (int step = 0; step < totalSteps; step++)
            {
                runStep.Invoke(engine, new object[] { DtHr });

                bool coordinatorUsed = engine.modularCoordinatorPathLastStepUsed;
                result.CoordinatorPathUsedEveryStep &= coordinatorUsed;
                result.LegacyPathUsedEveryStep &= !coordinatorUsed;

                result.Samples.Add(new StepSample
                {
                    Step = step + 1,
                    TimeHr = engine.simTime,
                    PressurePsia = engine.pressure,
                    PzrLevelPct = engine.pzrLevel,
                    PrimaryMassLb = engine.primaryMassLedger_lb
                });
            }

            return result;
        }

        private static void ConfigureDeterministicInputs(HeatupSimEngine engine)
        {
            UnityEngine.Random.InitState(FixedSeed);

            engine.enableAsyncLogWriter = false;
            engine.enableHighFrequencyPerfLogs = false;
            engine.enableWorkerThreadStepping = false;
            engine.runOnStart = false;

            engine.coldShutdownStart = true;
            engine.startTemperature = 100f;
            engine.startPressure = PlantConstants.PRESSURIZE_INITIAL_PRESSURE_PSIA;
            engine.startPZRLevel = 100f;
        }

        private static ComparisonResult CompareRuns(RunResult baseline, RunResult coordinator)
        {
            var result = new ComparisonResult { Pass = true };

            if (baseline.Samples.Count != coordinator.Samples.Count)
            {
                result.Pass = false;
                result.FailureReason = $"Step count mismatch baseline={baseline.Samples.Count}, coordinator={coordinator.Samples.Count}";
                return result;
            }

            if (!baseline.LegacyPathUsedEveryStep)
            {
                result.Pass = false;
                result.FailureReason = "Baseline scenario unexpectedly used modular coordinator path.";
                return result;
            }

            if (!coordinator.CoordinatorPathUsedEveryStep)
            {
                result.Pass = false;
                result.FailureReason = "Coordinator scenario did not use modular coordinator path on every step.";
                return result;
            }

            for (int i = 0; i < baseline.Samples.Count; i++)
            {
                StepSample b = baseline.Samples[i];
                StepSample c = coordinator.Samples[i];

                float pressureErr = Mathf.Abs(c.PressurePsia - b.PressurePsia);
                if (pressureErr > result.MaxPressureErrorPsia)
                {
                    result.MaxPressureErrorPsia = pressureErr;
                    result.WorstPressureStep = i + 1;
                }

                float pzrErr = Mathf.Abs(c.PzrLevelPct - b.PzrLevelPct);
                if (pzrErr > result.MaxPzrLevelErrorPct)
                {
                    result.MaxPzrLevelErrorPct = pzrErr;
                    result.WorstPzrLevelStep = i + 1;
                }

                float massErr = Mathf.Abs(c.PrimaryMassLb - b.PrimaryMassLb);
                if (massErr > result.MaxPrimaryMassErrorLb)
                {
                    result.MaxPrimaryMassErrorLb = massErr;
                    result.WorstPrimaryMassStep = i + 1;
                }
            }

            bool pressurePass = result.MaxPressureErrorPsia <= PressureTolerancePsia;
            bool pzrPass = result.MaxPzrLevelErrorPct <= PzrLevelTolerancePct;
            bool massPass = result.MaxPrimaryMassErrorLb <= PrimaryMassToleranceLb;
            result.Pass = pressurePass && pzrPass && massPass;

            if (!result.Pass)
            {
                result.FailureReason =
                    $"Tolerance breach: pressure={result.MaxPressureErrorPsia:F6}/{PressureTolerancePsia:F6}, " +
                    $"pzr={result.MaxPzrLevelErrorPct:F6}/{PzrLevelTolerancePct:F6}, " +
                    $"mass={result.MaxPrimaryMassErrorLb:F6}/{PrimaryMassToleranceLb:F6}";
            }

            return result;
        }

        private static void WriteSamplesCsv(string path, List<StepSample> samples)
        {
            var sb = new StringBuilder();
            sb.AppendLine("step,time_hr,pressure_psia,pzr_level_pct,primary_mass_lb");
            foreach (StepSample sample in samples)
            {
                sb.AppendLine(
                    $"{sample.Step}," +
                    $"{F(sample.TimeHr)}," +
                    $"{F(sample.PressurePsia)}," +
                    $"{F(sample.PzrLevelPct)}," +
                    $"{F(sample.PrimaryMassLb)}");
            }

            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteComparisonCsv(string path, List<StepSample> baseline, List<StepSample> coordinator)
        {
            var sb = new StringBuilder();
            sb.AppendLine("step,time_hr,pressure_abs_error_psia,pzr_level_abs_error_pct,primary_mass_abs_error_lb");

            int count = Mathf.Min(baseline.Count, coordinator.Count);
            for (int i = 0; i < count; i++)
            {
                StepSample b = baseline[i];
                StepSample c = coordinator[i];
                sb.AppendLine(
                    $"{i + 1}," +
                    $"{F(b.TimeHr)}," +
                    $"{F(Mathf.Abs(c.PressurePsia - b.PressurePsia))}," +
                    $"{F(Mathf.Abs(c.PzrLevelPct - b.PzrLevelPct))}," +
                    $"{F(Mathf.Abs(c.PrimaryMassLb - b.PrimaryMassLb))}");
            }

            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteIssue(
            string path,
            string runstamp,
            string logDir,
            RunResult baseline,
            RunResult coordinator,
            ComparisonResult comparison)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0025 Stage A - Deterministic Equivalence");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run stamp: `{runstamp}`");
            sb.AppendLine($"- Result: {(comparison.Pass ? "PASS" : "FAIL")}");
            sb.AppendLine();
            sb.AppendLine("## Deterministic Controls");
            sb.AppendLine($"- Fixed random seed: `{FixedSeed}`");
            sb.AppendLine($"- Fixed timestep: `{DtHr:F6} hr`");
            sb.AppendLine("- Fixed init profile: cold-shutdown baseline (`startTemperature=100F`, `startPressure=PlantConstants.PRESSURIZE_INITIAL_PRESSURE_PSIA`, `startPZRLevel=100%`)");
            sb.AppendLine();
            sb.AppendLine("## Scenarios");
            sb.AppendLine($"- Legacy baseline path steps: `{baseline.Samples.Count}`");
            sb.AppendLine($"- Coordinator+Legacy path steps: `{coordinator.Samples.Count}`");
            sb.AppendLine($"- Baseline used legacy-only path every step: `{baseline.LegacyPathUsedEveryStep}`");
            sb.AppendLine($"- Coordinator path used every step: `{coordinator.CoordinatorPathUsedEveryStep}`");
            sb.AppendLine();
            sb.AppendLine("## Tolerances");
            sb.AppendLine($"- Pressure tolerance: `{PressureTolerancePsia:F6} psia`");
            sb.AppendLine($"- PZR level tolerance: `{PzrLevelTolerancePct:F6} %`");
            sb.AppendLine($"- Primary mass tolerance: `{PrimaryMassToleranceLb:F6} lb`");
            sb.AppendLine();
            sb.AppendLine("## Max Observed Error");
            sb.AppendLine($"- Pressure: `{comparison.MaxPressureErrorPsia:F6} psia` at step `{comparison.WorstPressureStep}`");
            sb.AppendLine($"- PZR level: `{comparison.MaxPzrLevelErrorPct:F6} %` at step `{comparison.WorstPzrLevelStep}`");
            sb.AppendLine($"- Primary mass: `{comparison.MaxPrimaryMassErrorLb:F6} lb` at step `{comparison.WorstPrimaryMassStep}`");
            sb.AppendLine();
            sb.AppendLine("## Artifacts");
            sb.AppendLine($"- Run directory: `{ToRepoRelative(logDir)}`");
            sb.AppendLine($"- Baseline samples: `{ToRepoRelative(Path.Combine(logDir, "baseline_samples.csv"))}`");
            sb.AppendLine($"- Coordinator samples: `{ToRepoRelative(Path.Combine(logDir, "coordinator_samples.csv"))}`");
            sb.AppendLine($"- Comparison samples: `{ToRepoRelative(Path.Combine(logDir, "comparison.csv"))}`");

            if (!comparison.Pass && !string.IsNullOrWhiteSpace(comparison.FailureReason))
            {
                sb.AppendLine();
                sb.AppendLine("## Failure Reason");
                sb.AppendLine($"- {comparison.FailureReason}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllText(path, sb.ToString());
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

        private static string F(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
