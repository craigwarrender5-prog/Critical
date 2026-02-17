using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Critical.Physics;
using Critical.Simulation.Modular;
using Critical.Simulation.Modular.State;
using Critical.Simulation.Modular.Transfer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using Critical.Validation;
namespace Critical.Validation
{
    public static class IP0025StageERunner
    {
        private const float DtHr = 1f / 360f;
        private const float RunHours = 1.0f;
        private const int MaxSteps = 100000;
        private const int FixedSeed = 250025;

        private const float PressureTolerancePsia = 1e-3f;
        private const float PzrLevelTolerancePct = 1e-4f;
        private const float HeaterToleranceMw = 1e-4f;
        private const float SprayToleranceGpm = 1e-3f;
        private const float PzrWaterVolumeToleranceFt3 = 1e-3f;
        private const float PzrSteamVolumeToleranceFt3 = 1e-3f;

        private sealed class StepSample
        {
            public int Step;
            public float TimeHr;
            public float PressurePsia;
            public float PzrLevelPct;
            public float HeaterPowerMw;
            public float SprayFlowGpm;
            public float PzrWaterVolumeFt3;
            public float PzrSteamVolumeFt3;
            public float LedgerHeaterMw;
            public float LedgerSprayGpm;
            public float LedgerSurgeGpm;
            public bool UnledgeredMutation;
        }

        private sealed class RunResult
        {
            public string Label = string.Empty;
            public readonly List<StepSample> Samples = new List<StepSample>();
            public bool CoordinatorPathUsedEveryStep = true;
            public int UnledgeredMutationCount;
            public int MissingHeaterIntentCount;
            public int MissingSprayIntentCount;
            public int MissingSurgeIntentCount;
        }

        private sealed class ComparisonResult
        {
            public bool Pass;
            public string FailureReason = string.Empty;
            public float MaxPressureErrorPsia;
            public float MaxPzrLevelErrorPct;
            public float MaxHeaterErrorMw;
            public float MaxSprayErrorGpm;
            public float MaxPzrWaterVolumeErrorFt3;
            public float MaxPzrSteamVolumeErrorFt3;
            public int WorstPressureStep;
            public int WorstPzrLevelStep;
            public int WorstHeaterStep;
            public int WorstSprayStep;
            public int WorstPzrWaterStep;
            public int WorstPzrSteamStep;
        }

        [MenuItem("Critical/Run IP-0025 Stage E PZR Packaging Equivalence")]
        public static void RunStageEPzrPackaging()
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string runstamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string logDir = Path.Combine(root, "HeatupLogs", $"IP-0025_StageE_{runstamp}");
            Directory.CreateDirectory(logDir);

            try
            {
                RunResult baseline = ExecuteScenario(logDir, "LEGACY_PZR", enableModularPzr: false);
                RunResult modular = ExecuteScenario(logDir, "MODULAR_PZR", enableModularPzr: true);
                ComparisonResult comparison = CompareRuns(baseline, modular);

                WriteSamplesCsv(Path.Combine(logDir, "baseline_samples.csv"), baseline.Samples);
                WriteSamplesCsv(Path.Combine(logDir, "modular_samples.csv"), modular.Samples);
                WriteComparisonCsv(Path.Combine(logDir, "comparison.csv"), baseline.Samples, modular.Samples);

                string issuePath = Path.Combine(
                    root,
                    "Governance",
                    "Issues",
                    $"IP-0025_StageE_PZRPackaging_Equivalence_{runstamp}.md");
                WriteIssue(issuePath, runstamp, logDir, baseline, modular, comparison);

                Debug.Log($"[IP-0025][StageE] Artifact: {issuePath}");
                Debug.Log($"[IP-0025][StageE] Logs: {logDir}");

                if (!comparison.Pass)
                    throw new Exception($"IP-0025 Stage E equivalence failed: {comparison.FailureReason}");
            }
            finally
            {
                ModularFeatureFlags.ResetAll();
            }
        }

        private static RunResult ExecuteScenario(string logDir, string label, bool enableModularPzr)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
            HeatupSimEngine engine = UnityEngine.Object.FindObjectOfType<HeatupSimEngine>();
            if (engine == null)
                throw new InvalidOperationException("HeatupSimEngine not found in MainScene.");

            Type engineType = typeof(HeatupSimEngine);
            MethodInfo initialize = engineType.GetMethod("InitializeSimulation", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo runStep = engineType.GetMethod("RunPhysicsStep", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo logPathField = engineType.GetField("logPath", BindingFlags.Instance | BindingFlags.NonPublic);
            if (initialize == null || runStep == null || logPathField == null)
                throw new MissingMethodException("IP-0025 Stage E runner missing required HeatupSimEngine members.");

            ConfigureDeterministicInputs(engine);
            logPathField.SetValue(engine, Path.Combine(logDir, label));

            ModularFeatureFlags.ResetAll();
            ModularFeatureFlags.EnableCoordinatorPath = true;
            ModularFeatureFlags.UseModularPZR = enableModularPzr;
            ModularFeatureFlags.BypassLegacyPZR = enableModularPzr;

            initialize.Invoke(engine, null);

            var result = new RunResult { Label = label };
            int totalSteps = Mathf.CeilToInt(RunHours / DtHr);
            if (totalSteps > MaxSteps)
                throw new InvalidOperationException($"Stage E runner exceeded MaxSteps ({MaxSteps}).");

            for (int step = 1; step <= totalSteps; step++)
            {
                runStep.Invoke(engine, new object[] { DtHr });
                result.CoordinatorPathUsedEveryStep &= engine.modularCoordinatorPathLastStepUsed;

                StepSnapshot snapshot = engine.GetStepSnapshot();
                TransferLedger ledger = snapshot.TransferLedger;
                float ledgerHeater = ledger.SumBySignal(TransferIntentKinds.SignalPzrHeaterPowerMw, TransferQuantityType.EnergyMw);
                float ledgerSpray = ledger.SumBySignal(TransferIntentKinds.SignalSprayFlowGpm, TransferQuantityType.FlowGpm);
                float ledgerSurge = ledger.SumBySignal(TransferIntentKinds.SignalSurgeFlowGpm, TransferQuantityType.FlowGpm);

                if (ledger.UnledgeredMutationDetected)
                    result.UnledgeredMutationCount++;

                if (Mathf.Abs(engine.pzrHeaterPower) > 1e-6f && Mathf.Abs(ledgerHeater) <= 1e-6f)
                    result.MissingHeaterIntentCount++;
                if (Mathf.Abs(engine.sprayFlow_GPM) > 1e-6f && Mathf.Abs(ledgerSpray) <= 1e-6f)
                    result.MissingSprayIntentCount++;
                if (Mathf.Abs(engine.surgeFlow) > 1e-6f && Mathf.Abs(ledgerSurge) <= 1e-6f)
                    result.MissingSurgeIntentCount++;

                result.Samples.Add(new StepSample
                {
                    Step = step,
                    TimeHr = engine.simTime,
                    PressurePsia = engine.pressure,
                    PzrLevelPct = engine.pzrLevel,
                    HeaterPowerMw = engine.pzrHeaterPower,
                    SprayFlowGpm = engine.sprayFlow_GPM,
                    PzrWaterVolumeFt3 = engine.pzrWaterVolume,
                    PzrSteamVolumeFt3 = engine.pzrSteamVolume,
                    LedgerHeaterMw = ledgerHeater,
                    LedgerSprayGpm = ledgerSpray,
                    LedgerSurgeGpm = ledgerSurge,
                    UnledgeredMutation = ledger.UnledgeredMutationDetected
                });
            }

            return result;
        }

        private static ComparisonResult CompareRuns(RunResult baseline, RunResult modular)
        {
            var result = new ComparisonResult { Pass = true };

            if (!baseline.CoordinatorPathUsedEveryStep || !modular.CoordinatorPathUsedEveryStep)
            {
                result.Pass = false;
                result.FailureReason = "Coordinator authority path was not used on every step.";
                return result;
            }

            if (baseline.Samples.Count != modular.Samples.Count)
            {
                result.Pass = false;
                result.FailureReason = $"Step count mismatch baseline={baseline.Samples.Count}, modular={modular.Samples.Count}.";
                return result;
            }

            for (int i = 0; i < baseline.Samples.Count; i++)
            {
                StepSample b = baseline.Samples[i];
                StepSample m = modular.Samples[i];

                TrackError(Mathf.Abs(m.PressurePsia - b.PressurePsia), i + 1, ref result.MaxPressureErrorPsia, ref result.WorstPressureStep);
                TrackError(Mathf.Abs(m.PzrLevelPct - b.PzrLevelPct), i + 1, ref result.MaxPzrLevelErrorPct, ref result.WorstPzrLevelStep);
                TrackError(Mathf.Abs(m.HeaterPowerMw - b.HeaterPowerMw), i + 1, ref result.MaxHeaterErrorMw, ref result.WorstHeaterStep);
                TrackError(Mathf.Abs(m.SprayFlowGpm - b.SprayFlowGpm), i + 1, ref result.MaxSprayErrorGpm, ref result.WorstSprayStep);
                TrackError(Mathf.Abs(m.PzrWaterVolumeFt3 - b.PzrWaterVolumeFt3), i + 1, ref result.MaxPzrWaterVolumeErrorFt3, ref result.WorstPzrWaterStep);
                TrackError(Mathf.Abs(m.PzrSteamVolumeFt3 - b.PzrSteamVolumeFt3), i + 1, ref result.MaxPzrSteamVolumeErrorFt3, ref result.WorstPzrSteamStep);
            }

            bool tolPass =
                result.MaxPressureErrorPsia <= PressureTolerancePsia &&
                result.MaxPzrLevelErrorPct <= PzrLevelTolerancePct &&
                result.MaxHeaterErrorMw <= HeaterToleranceMw &&
                result.MaxSprayErrorGpm <= SprayToleranceGpm &&
                result.MaxPzrWaterVolumeErrorFt3 <= PzrWaterVolumeToleranceFt3 &&
                result.MaxPzrSteamVolumeErrorFt3 <= PzrSteamVolumeToleranceFt3;

            bool ledgerPass =
                modular.UnledgeredMutationCount == 0 &&
                modular.MissingHeaterIntentCount == 0 &&
                modular.MissingSprayIntentCount == 0 &&
                modular.MissingSurgeIntentCount == 0;

            result.Pass = tolPass && ledgerPass;
            if (!result.Pass)
            {
                var reasons = new List<string>();
                if (!tolPass) reasons.Add("tolerance breach");
                if (modular.UnledgeredMutationCount != 0) reasons.Add($"unledgered mutations={modular.UnledgeredMutationCount}");
                if (modular.MissingHeaterIntentCount != 0) reasons.Add($"missing heater intents={modular.MissingHeaterIntentCount}");
                if (modular.MissingSprayIntentCount != 0) reasons.Add($"missing spray intents={modular.MissingSprayIntentCount}");
                if (modular.MissingSurgeIntentCount != 0) reasons.Add($"missing surge intents={modular.MissingSurgeIntentCount}");
                result.FailureReason = string.Join("; ", reasons);
            }

            return result;
        }

        private static void WriteSamplesCsv(string path, List<StepSample> samples)
        {
            var sb = new StringBuilder();
            sb.AppendLine("step,time_hr,pressure_psia,pzr_level_pct,heater_power_mw,spray_flow_gpm,pzr_water_ft3,pzr_steam_ft3,ledger_heater_mw,ledger_spray_gpm,ledger_surge_gpm,unledgered");
            foreach (StepSample s in samples)
            {
                sb.AppendLine(
                    $"{s.Step},{F(s.TimeHr)},{F(s.PressurePsia)},{F(s.PzrLevelPct)}," +
                    $"{F(s.HeaterPowerMw)},{F(s.SprayFlowGpm)},{F(s.PzrWaterVolumeFt3)},{F(s.PzrSteamVolumeFt3)}," +
                    $"{F(s.LedgerHeaterMw)},{F(s.LedgerSprayGpm)},{F(s.LedgerSurgeGpm)},{s.UnledgeredMutation}");
            }

            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteComparisonCsv(string path, List<StepSample> baseline, List<StepSample> modular)
        {
            var sb = new StringBuilder();
            sb.AppendLine("step,time_hr,pressure_abs_error_psia,pzr_level_abs_error_pct,heater_abs_error_mw,spray_abs_error_gpm,pzr_water_abs_error_ft3,pzr_steam_abs_error_ft3");

            int count = Mathf.Min(baseline.Count, modular.Count);
            for (int i = 0; i < count; i++)
            {
                StepSample b = baseline[i];
                StepSample m = modular[i];
                sb.AppendLine(
                    $"{i + 1},{F(b.TimeHr)},{F(Mathf.Abs(m.PressurePsia - b.PressurePsia))},{F(Mathf.Abs(m.PzrLevelPct - b.PzrLevelPct))}," +
                    $"{F(Mathf.Abs(m.HeaterPowerMw - b.HeaterPowerMw))},{F(Mathf.Abs(m.SprayFlowGpm - b.SprayFlowGpm))}," +
                    $"{F(Mathf.Abs(m.PzrWaterVolumeFt3 - b.PzrWaterVolumeFt3))},{F(Mathf.Abs(m.PzrSteamVolumeFt3 - b.PzrSteamVolumeFt3))}");
            }

            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteIssue(
            string path,
            string runstamp,
            string logDir,
            RunResult baseline,
            RunResult modular,
            ComparisonResult comparison)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0025 Stage E - PZR Packaging Equivalence");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run stamp: `{runstamp}`");
            sb.AppendLine($"- Result: {(comparison.Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- Steps compared: `{baseline.Samples.Count}`");
            sb.AppendLine();
            sb.AppendLine("## Deterministic Controls");
            sb.AppendLine($"- Fixed random seed: `{FixedSeed}`");
            sb.AppendLine($"- Fixed timestep: `{DtHr:F6} hr`");
            sb.AppendLine("- Fixed init profile: cold shutdown start with deterministic startup values.");
            sb.AppendLine();
            sb.AppendLine("## Tolerances");
            sb.AppendLine($"- Pressure: `{PressureTolerancePsia:E2} psia`");
            sb.AppendLine($"- PZR level: `{PzrLevelTolerancePct:E2} %`");
            sb.AppendLine($"- Heater power: `{HeaterToleranceMw:E2} MW`");
            sb.AppendLine($"- Spray flow: `{SprayToleranceGpm:E2} gpm`");
            sb.AppendLine($"- PZR water volume: `{PzrWaterVolumeToleranceFt3:E2} ft^3`");
            sb.AppendLine($"- PZR steam volume: `{PzrSteamVolumeToleranceFt3:E2} ft^3`");
            sb.AppendLine();
            sb.AppendLine("## Max Observed Error (Legacy PZR vs Modular PZR)");
            sb.AppendLine($"- Pressure: `{comparison.MaxPressureErrorPsia:E3} psia` at step `{comparison.WorstPressureStep}`");
            sb.AppendLine($"- PZR level: `{comparison.MaxPzrLevelErrorPct:E3} %` at step `{comparison.WorstPzrLevelStep}`");
            sb.AppendLine($"- Heater power: `{comparison.MaxHeaterErrorMw:E3} MW` at step `{comparison.WorstHeaterStep}`");
            sb.AppendLine($"- Spray flow: `{comparison.MaxSprayErrorGpm:E3} gpm` at step `{comparison.WorstSprayStep}`");
            sb.AppendLine($"- PZR water volume: `{comparison.MaxPzrWaterVolumeErrorFt3:E3} ft^3` at step `{comparison.WorstPzrWaterStep}`");
            sb.AppendLine($"- PZR steam volume: `{comparison.MaxPzrSteamVolumeErrorFt3:E3} ft^3` at step `{comparison.WorstPzrSteamStep}`");
            sb.AppendLine();
            sb.AppendLine("## Ledger and Mutation Gates (Modular PZR)");
            sb.AppendLine($"- Unledgered mutation count: `{modular.UnledgeredMutationCount}`");
            sb.AppendLine($"- Missing heater intents: `{modular.MissingHeaterIntentCount}`");
            sb.AppendLine($"- Missing spray intents: `{modular.MissingSprayIntentCount}`");
            sb.AppendLine($"- Missing surge intents: `{modular.MissingSurgeIntentCount}`");
            sb.AppendLine();
            sb.AppendLine("## Artifacts");
            sb.AppendLine($"- Run directory: `{ToRepoRelative(logDir)}`");
            sb.AppendLine($"- Baseline samples: `{ToRepoRelative(Path.Combine(logDir, "baseline_samples.csv"))}`");
            sb.AppendLine($"- Modular samples: `{ToRepoRelative(Path.Combine(logDir, "modular_samples.csv"))}`");
            sb.AppendLine($"- Comparison: `{ToRepoRelative(Path.Combine(logDir, "comparison.csv"))}`");

            if (!comparison.Pass && !string.IsNullOrWhiteSpace(comparison.FailureReason))
            {
                sb.AppendLine();
                sb.AppendLine("## Failure Reason");
                sb.AppendLine($"- {comparison.FailureReason}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllText(path, sb.ToString());
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

        private static string ToRepoRelative(string absolutePath)
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string normalizedRoot = root.Replace('\\', '/').TrimEnd('/');
            string normalizedPath = absolutePath.Replace('\\', '/');
            if (normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                return normalizedPath.Substring(normalizedRoot.Length).TrimStart('/');

            return normalizedPath;
        }

        private static void TrackError(float err, int step, ref float max, ref int maxStep)
        {
            if (err > max)
            {
                max = err;
                maxStep = step;
            }
        }

        private static string F(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}

