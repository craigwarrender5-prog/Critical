using System;
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

namespace Critical.Validation
{
    public static class IP0025StageCRunner
    {
        private const float DtHr = 1f / 360f;
        private const float RunHours = 1.0f;
        private const int MaxSteps = 100000;
        private const int FixedSeed = 250025;

        private const float HeaterActiveThresholdMw = 1e-4f;
        private const float HeaterLedgerToleranceMw = 1e-4f;

        private sealed class Result
        {
            public int Steps;
            public int ActiveHeaterSamples;
            public int ActiveHeaterLedgerEventSamples;
            public float MaxHeaterErrorMw;
            public int MaxHeaterErrorStep;
            public int UnledgeredMutationCount;
            public string LastUnledgeredReason = string.Empty;
            public bool Pass;
            public string FailureReason = string.Empty;
        }

        [MenuItem("Critical/Run IP-0025 Stage C Ledger Gate")]
        public static void RunStageCLedgerGate()
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string runstamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string logDir = Path.Combine(root, "HeatupLogs", $"IP-0025_StageC_LedgerGate_{runstamp}");
            Directory.CreateDirectory(logDir);

            var csv = new StringBuilder();
            csv.AppendLine("step,time_hr,heater_legacy_mw,heater_ledger_mw,heater_abs_error_mw,ledger_event_count,unledgered_mutation,unledgered_reason");

            Result result = Execute(logDir, csv);

            string csvPath = Path.Combine(logDir, "ledger_gate.csv");
            File.WriteAllText(csvPath, csv.ToString());

            string issuePath = Path.Combine(root, "Governance", "Issues", $"IP-0025_StageC_LedgerGate_{runstamp}.md");
            WriteIssue(issuePath, runstamp, logDir, result);

            Debug.Log($"[IP-0025][StageC] Artifact: {issuePath}");
            Debug.Log($"[IP-0025][StageC] Logs: {logDir}");

            if (!result.Pass)
                throw new Exception($"IP-0025 Stage C ledger gate failed: {result.FailureReason}");
        }

        private static Result Execute(string logDir, StringBuilder csv)
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
                throw new MissingMethodException("IP-0025 Stage C runner missing required HeatupSimEngine methods/fields.");

            ConfigureDeterministicInputs(engine);
            logPathField.SetValue(engine, logDir);
            ModularFeatureFlags.ResetAll();
            ModularFeatureFlags.EnableCoordinatorPath = true;
            initialize.Invoke(engine, null);

            var result = new Result();
            int totalSteps = Mathf.CeilToInt(RunHours / DtHr);
            if (totalSteps > MaxSteps)
                throw new InvalidOperationException($"Stage C runner exceeded MaxSteps ({MaxSteps}).");

            for (int step = 1; step <= totalSteps; step++)
            {
                runStep.Invoke(engine, new object[] { DtHr });
                StepSnapshot snapshot = engine.GetStepSnapshot();
                TransferLedger ledger = snapshot.TransferLedger;

                float legacyHeater = engine.pzrHeaterPower;
                float ledgerHeater = ledger.SumBySignal("PZR_HEATER_POWER_MW", TransferQuantityType.EnergyMw);
                float heaterErr = Mathf.Abs(legacyHeater - ledgerHeater);

                if (heaterErr > result.MaxHeaterErrorMw)
                {
                    result.MaxHeaterErrorMw = heaterErr;
                    result.MaxHeaterErrorStep = step;
                }

                if (Mathf.Abs(legacyHeater) > HeaterActiveThresholdMw)
                {
                    result.ActiveHeaterSamples++;
                    if (Mathf.Abs(ledgerHeater) > HeaterActiveThresholdMw)
                        result.ActiveHeaterLedgerEventSamples++;
                }

                if (ledger.UnledgeredMutationDetected)
                {
                    result.UnledgeredMutationCount++;
                    result.LastUnledgeredReason = ledger.UnledgeredMutationReason;
                }

                csv.AppendLine(
                    $"{step}," +
                    $"{F(engine.simTime)}," +
                    $"{F(legacyHeater)}," +
                    $"{F(ledgerHeater)}," +
                    $"{F(heaterErr)}," +
                    $"{ledger.Events.Count}," +
                    $"{ledger.UnledgeredMutationDetected}," +
                    $"\"{(ledger.UnledgeredMutationReason ?? string.Empty).Replace("\"", "'")}\"");
            }

            result.Steps = totalSteps;

            bool heaterErrorPass = result.MaxHeaterErrorMw <= HeaterLedgerToleranceMw;
            bool detectorPass = result.UnledgeredMutationCount == 0;
            bool migratedCheckObserved = result.ActiveHeaterSamples > 0 &&
                                         result.ActiveHeaterLedgerEventSamples == result.ActiveHeaterSamples;
            result.Pass = heaterErrorPass && detectorPass && migratedCheckObserved;

            if (!result.Pass)
            {
                var reason = new StringBuilder();
                if (!heaterErrorPass)
                    reason.Append($"heater ledger mismatch max={result.MaxHeaterErrorMw:F6}MW; ");
                if (!detectorPass)
                    reason.Append($"unledgered mutation count={result.UnledgeredMutationCount}; ");
                if (!migratedCheckObserved)
                    reason.Append("migrated heater check did not observe full ledger coverage; ");
                if (result.LastUnledgeredReason.Length > 0)
                    reason.Append($"last reason={result.LastUnledgeredReason}");
                result.FailureReason = reason.ToString().Trim();
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

        private static void WriteIssue(string path, string runstamp, string logDir, Result result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0025 Stage C - Ledger Gate");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run stamp: `{runstamp}`");
            sb.AppendLine($"- Result: {(result.Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- Steps: `{result.Steps}`");
            sb.AppendLine();
            sb.AppendLine("## Migrated Ledger-Backed Check");
            sb.AppendLine("- Check: `PZR_HEATER_POWER_MW` direct legacy value vs transfer-ledger signal sum.");
            sb.AppendLine($"- Active heater samples: `{result.ActiveHeaterSamples}`");
            sb.AppendLine($"- Active samples with ledger event: `{result.ActiveHeaterLedgerEventSamples}`");
            sb.AppendLine($"- Max heater mismatch: `{result.MaxHeaterErrorMw:F6} MW` at step `{result.MaxHeaterErrorStep}`");
            sb.AppendLine($"- Heater tolerance: `{HeaterLedgerToleranceMw:F6} MW`");
            sb.AppendLine();
            sb.AppendLine("## Unledgered Mutation Detector");
            sb.AppendLine($"- Unledgered mutation count: `{result.UnledgeredMutationCount}`");
            sb.AppendLine($"- Last detector reason: `{(string.IsNullOrWhiteSpace(result.LastUnledgeredReason) ? "NONE" : result.LastUnledgeredReason)}`");
            sb.AppendLine();
            sb.AppendLine("## Artifacts");
            sb.AppendLine($"- Run directory: `{ToRepoRelative(logDir)}`");
            sb.AppendLine($"- Ledger gate CSV: `{ToRepoRelative(Path.Combine(logDir, "ledger_gate.csv"))}`");

            if (!result.Pass && !string.IsNullOrWhiteSpace(result.FailureReason))
            {
                sb.AppendLine();
                sb.AppendLine("## Failure Reason");
                sb.AppendLine($"- {result.FailureReason}");
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
