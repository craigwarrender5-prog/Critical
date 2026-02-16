using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Critical.Validation
{
    public static class IP0023CheckpointRunner
    {
        private const float DtHr = 1f / 360f;
        private const float DefaultRunHours = 6f;
        private const int MaxSteps = 120000;
        private const int HashStrideSteps = 30;

        private sealed class RunConfig
        {
            public string Label = string.Empty;
            public bool EnableAsyncLogWriter;
            public bool EnableHighFrequencyPerfLogs;
            public bool EnableWorkerThreadStepping;
            public float MaxSimHr = DefaultRunHours;
            public bool CaptureDeterminismHashes;
            public bool CollectIntervalLogs = true;
        }

        private sealed class RunResult
        {
            public string Label = string.Empty;
            public string LogDir = string.Empty;
            public string ReportPath = string.Empty;
            public readonly List<float> StepMs = new List<float>();
            public readonly List<float> IntervalSaveMs = new List<float>();
            public readonly List<float> LogTimesSec = new List<float>();
            public readonly List<ulong> HashSequence = new List<ulong>();
            public ulong TerminalHash;
            public float FinalSimTimeHr;
            public int Steps;
            public int Gc0Delta;
            public int Gc1Delta;
            public int Gc2Delta;
            public int AsyncDroppedCount;
            public int AsyncErrorCount;
            public int AsyncHighWatermark;
            public float AsyncFlushMs;
            public float AsyncMaxDispatchMs;
            public bool WorkerUsed;
            public bool SnapshotConsistent;
            public string SnapshotReason = string.Empty;
            public float ShutdownLatencyMs;
            public bool ShutdownReturned;
            public int SimLogCount;
        }

        private sealed class Group1Eval
        {
            public bool Cs0032Pass;
            public bool Cs0065Pass;
            public bool Cs0044Pass;
            public string Cs0032Reason = string.Empty;
            public string Cs0065Reason = string.Empty;
            public string Cs0044Reason = string.Empty;
            public bool Overall => Cs0032Pass && Cs0065Pass && Cs0044Pass;
        }

        private sealed class Group2Eval
        {
            public bool Cs0066Pass;
            public bool Cs0067Pass;
            public bool Cs0068Pass;
            public string Cs0066Reason = string.Empty;
            public string Cs0067Reason = string.Empty;
            public string Cs0068Reason = string.Empty;
            public bool Overall => Cs0066Pass && Cs0067Pass && Cs0068Pass;
        }

        private sealed class Group3Eval
        {
            public bool Cs0045Pass;
            public string Cs0045Reason = string.Empty;
            public bool Overall => Cs0045Pass;
        }

        private sealed class Group4Eval
        {
            public bool Cs0046Pass;
            public string Cs0046Reason = string.Empty;
            public bool Overall => Cs0046Pass;
            public string BaselineHash = string.Empty;
            public string WorkerHash = string.Empty;
        }

        private sealed class StageEEval
        {
            public bool Pass;
            public string Reason = string.Empty;
        }

        [MenuItem("Critical/Run IP-0023 All Checkpoints + Recommendation")]
        public static void RunAllCheckpointsAndRecommendation()
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");

            string group1Path = Path.Combine(root, "Governance", "Issues",
                $"IP-0023_Group1_Checkpoint_{stamp}.md");
            string group2Path = Path.Combine(root, "Governance", "Issues",
                $"IP-0023_Group2_Checkpoint_{stamp}.md");
            string group3Path = Path.Combine(root, "Governance", "Issues",
                $"IP-0023_Group3_Checkpoint_{stamp}.md");
            string group4Path = Path.Combine(root, "Governance", "Issues",
                $"IP-0023_Group4_Checkpoint_{stamp}.md");
            string stageDPath = Path.Combine(root, "Governance", "Issues",
                $"IP-0023_StageD_DomainValidation_{stamp}.md");
            string stageEPath = Path.Combine(root, "Governance", "Issues",
                $"IP-0023_StageE_SystemRegression_{stamp}.md");
            string recommendationPath = Path.Combine(root, "Governance", "ImplementationReports",
                $"IP-0023_Closure_Recommendation_{DateTime.Now:yyyy-MM-dd}.md");

            RunResult remediated = ExecuteRun(
                root,
                new RunConfig
                {
                    Label = "REMEDIATED",
                    EnableAsyncLogWriter = true,
                    EnableHighFrequencyPerfLogs = false,
                    EnableWorkerThreadStepping = false,
                    MaxSimHr = DefaultRunHours,
                    CollectIntervalLogs = true,
                    CaptureDeterminismHashes = false
                });

            Group1Eval g1 = EvaluateGroup1(remediated);
            Group2Eval g2 = EvaluateGroup2(remediated, root);
            Group3Eval g3 = EvaluateGroup3(remediated, root);

            RunResult determinismBaseline = ExecuteRun(
                root,
                new RunConfig
                {
                    Label = "DETERMINISM_BASELINE",
                    EnableAsyncLogWriter = false,
                    EnableHighFrequencyPerfLogs = false,
                    EnableWorkerThreadStepping = false,
                    MaxSimHr = 6f,
                    CollectIntervalLogs = false,
                    CaptureDeterminismHashes = true
                });

            RunResult determinismWorker = ExecuteRun(
                root,
                new RunConfig
                {
                    Label = "DETERMINISM_WORKER",
                    EnableAsyncLogWriter = false,
                    EnableHighFrequencyPerfLogs = false,
                    EnableWorkerThreadStepping = true,
                    MaxSimHr = 6f,
                    CollectIntervalLogs = false,
                    CaptureDeterminismHashes = true
                });

            Group4Eval g4 = EvaluateGroup4(determinismBaseline, determinismWorker);

            RunResult legacyApprox = ExecuteRun(
                root,
                new RunConfig
                {
                    Label = "LEGACY_APPROX",
                    EnableAsyncLogWriter = false,
                    EnableHighFrequencyPerfLogs = true,
                    EnableWorkerThreadStepping = false,
                    MaxSimHr = DefaultRunHours,
                    CollectIntervalLogs = true,
                    CaptureDeterminismHashes = false
                });

            StageEEval stageE = EvaluateStageE(legacyApprox, remediated, g4);

            WriteGroup1(group1Path, remediated, g1);
            WriteGroup2(group2Path, remediated, g2);
            WriteGroup3(group3Path, remediated, g3);
            WriteGroup4(group4Path, determinismBaseline, determinismWorker, g4);
            WriteStageD(stageDPath, group1Path, group2Path, group3Path, group4Path, g1, g2, g3, g4);
            WriteStageE(stageEPath, legacyApprox, remediated, g4, stageE, group1Path, group2Path, group3Path, group4Path);
            WriteRecommendation(recommendationPath, stageDPath, stageEPath, g1, g2, g3, g4, stageE);

            bool allPass = g1.Overall && g2.Overall && g3.Overall && g4.Overall && stageE.Pass;
            UnityEngine.Debug.Log($"[IP-0023] Group1={g1.Overall} Group2={g2.Overall} Group3={g3.Overall} Group4={g4.Overall} StageE={stageE.Pass}");
            UnityEngine.Debug.Log($"[IP-0023] Stage D: {stageDPath}");
            UnityEngine.Debug.Log($"[IP-0023] Stage E: {stageEPath}");
            UnityEngine.Debug.Log($"[IP-0023] Recommendation: {recommendationPath}");
            if (!allPass)
                throw new Exception("IP-0023 checkpoint suite failed. See evidence artifacts.");
        }

        private static RunResult ExecuteRun(string root, RunConfig config)
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string logDir = Path.Combine(root, "HeatupLogs", $"IP-0023_{config.Label}_{stamp}");
            PrepareLogDirectory(logDir);

            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
            HeatupSimEngine engine = UnityEngine.Object.FindObjectOfType<HeatupSimEngine>();
            if (engine == null)
                throw new InvalidOperationException("HeatupSimEngine not found in MainScene.");

            Type t = typeof(HeatupSimEngine);
            MethodInfo init = t.GetMethod("InitializeSimulation", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo runStep = t.GetMethod("RunPhysicsStep", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo saveInterval = t.GetMethod("SaveIntervalLog", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo saveReport = t.GetMethod("SaveReport", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo logPathField = t.GetField("logPath", BindingFlags.Instance | BindingFlags.NonPublic);
            if (init == null || runStep == null || saveInterval == null || saveReport == null || logPathField == null)
                throw new MissingMethodException("IP-0023 runner could not access required HeatupSimEngine members.");

            var initDelegate = (Action)Delegate.CreateDelegate(typeof(Action), engine, init);
            var runStepDelegate = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), engine, runStep);
            var saveIntervalDelegate = (Action)Delegate.CreateDelegate(typeof(Action), engine, saveInterval);
            var saveReportDelegate = (Action)Delegate.CreateDelegate(typeof(Action), engine, saveReport);

            engine.runOnStart = false;
            engine.coldShutdownStart = true;
            engine.startTemperature = 100f;
            engine.targetTemperature = 557f;
            engine.enableAsyncLogWriter = config.EnableAsyncLogWriter;
            engine.enableHighFrequencyPerfLogs = config.EnableHighFrequencyPerfLogs;
            engine.enableWorkerThreadStepping = config.EnableWorkerThreadStepping;
            engine.asyncLogDroppedCount = 0;
            engine.asyncLogQueueHighWatermark = 0;
            engine.asyncLogWriteErrorCount = 0;
            engine.asyncLogLastFlushMs = 0f;
            engine.asyncLogMaxDispatchMs = 0f;
            logPathField.SetValue(engine, logDir);

            initDelegate();
            if (config.CollectIntervalLogs)
                saveIntervalDelegate();

            int gc0Start = GC.CollectionCount(0);
            int gc1Start = GC.CollectionCount(1);
            int gc2Start = GC.CollectionCount(2);
            var run = new RunResult
            {
                Label = config.Label,
                LogDir = logDir,
                SnapshotConsistent = true
            };

            object callbackLock = new object();
            float currentSimSec = 0f;
            void OnLog(string condition, string stackTrace, LogType type)
            {
                if (!condition.Contains("Heatup", StringComparison.OrdinalIgnoreCase) &&
                    !stackTrace.Contains("HeatupSimEngine", StringComparison.OrdinalIgnoreCase))
                    return;

                lock (callbackLock)
                {
                    run.SimLogCount++;
                    run.LogTimesSec.Add(currentSimSec);
                }
            }

            Application.logMessageReceivedThreaded += OnLog;
            try
            {
                float nextInterval = 0.25f;
                int steps = 0;
                while (steps < MaxSteps && engine.simTime < config.MaxSimHr)
                {
                    var swStep = Stopwatch.StartNew();
                    runStepDelegate(DtHr);
                    swStep.Stop();
                    run.StepMs.Add((float)swStep.Elapsed.TotalMilliseconds);

                    currentSimSec = engine.simTime * 3600f;
                    HeatupSimEngine.RuntimeTelemetrySnapshot snap = engine.GetTelemetrySnapshot();
                    bool snapshotMatch =
                        Mathf.Abs(snap.SimTime - engine.simTime) <= 1e-5f &&
                        Mathf.Abs(snap.WallClockTime - engine.wallClockTime) <= 1e-5f &&
                        snap.PlantMode == engine.plantMode &&
                        Mathf.Abs(snap.Pressure - engine.pressure) <= 1e-3f &&
                        Mathf.Abs(snap.Trcs - engine.T_rcs) <= 1e-3f;
                    if (!snapshotMatch && run.SnapshotConsistent)
                    {
                        run.SnapshotConsistent = false;
                        run.SnapshotReason = "Snapshot mismatch observed after RunPhysicsStep.";
                    }

                    if (config.CaptureDeterminismHashes && (steps % HashStrideSteps == 0))
                        run.HashSequence.Add(ComputeStateHash(engine));

                    if (config.CollectIntervalLogs && engine.simTime >= nextInterval)
                    {
                        var swInterval = Stopwatch.StartNew();
                        saveIntervalDelegate();
                        swInterval.Stop();
                        run.IntervalSaveMs.Add((float)swInterval.Elapsed.TotalMilliseconds);
                        nextInterval += 0.25f;
                    }

                    steps++;
                }

                saveReportDelegate();
                run.TerminalHash = ComputeStateHash(engine);
                run.FinalSimTimeHr = engine.simTime;
                run.Steps = steps;
            }
            finally
            {
                Application.logMessageReceivedThreaded -= OnLog;
            }

            run.Gc0Delta = GC.CollectionCount(0) - gc0Start;
            run.Gc1Delta = GC.CollectionCount(1) - gc1Start;
            run.Gc2Delta = GC.CollectionCount(2) - gc2Start;
            run.AsyncDroppedCount = engine.asyncLogDroppedCount;
            run.AsyncErrorCount = engine.asyncLogWriteErrorCount;
            run.AsyncHighWatermark = engine.asyncLogQueueHighWatermark;
            run.AsyncMaxDispatchMs = engine.asyncLogMaxDispatchMs;
            run.WorkerUsed = engine.workerThreadSteppingLastStepUsed;

            if (config.EnableAsyncLogWriter)
                engine.FlushAsyncLogWriter(500);
            run.AsyncFlushMs = engine.asyncLogLastFlushMs;

            string report = Directory.GetFiles(logDir, "Heatup_Report_*.txt")
                .OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? string.Empty;
            run.ReportPath = report;

            var swShutdown = Stopwatch.StartNew();
            try
            {
                engine.RequestImmediateShutdown();
                run.ShutdownReturned = true;
            }
            catch
            {
                run.ShutdownReturned = false;
            }
            swShutdown.Stop();
            run.ShutdownLatencyMs = (float)swShutdown.Elapsed.TotalMilliseconds;

            if (string.IsNullOrWhiteSpace(run.SnapshotReason))
                run.SnapshotReason = "Snapshot values remained coherent with live state.";
            return run;
        }

        private static Group1Eval EvaluateGroup1(RunResult run)
        {
            var eval = new Group1Eval();

            float p99Interval = Percentile(run.IntervalSaveMs, 99f);
            float maxDispatch = run.AsyncMaxDispatchMs;

            eval.Cs0032Pass = run.ShutdownReturned && run.ShutdownLatencyMs <= 500f;
            eval.Cs0032Reason =
                $"shutdownReturned={run.ShutdownReturned}, shutdownLatency={run.ShutdownLatencyMs:F2} ms (limit <=500 ms)";

            eval.Cs0065Pass = maxDispatch <= 4.0f && p99Interval <= 8.0f;
            eval.Cs0065Reason =
                $"asyncDispatchMax={maxDispatch:F3} ms (limit <=4.0), intervalSaveP99={p99Interval:F3} ms (hard ceiling <=8.0)";

            bool queueUsed = run.AsyncHighWatermark > 0;
            eval.Cs0044Pass = queueUsed &&
                              run.AsyncDroppedCount == 0 &&
                              run.AsyncErrorCount == 0 &&
                              run.AsyncFlushMs <= 500f;
            eval.Cs0044Reason =
                $"queueUsed={queueUsed}, highWatermark={run.AsyncHighWatermark}, dropped={run.AsyncDroppedCount}, " +
                $"writeErrors={run.AsyncErrorCount}, flush={run.AsyncFlushMs:F2} ms (limit <=500 ms)";

            return eval;
        }

        private static Group2Eval EvaluateGroup2(RunResult run, string root)
        {
            var eval = new Group2Eval();
            string sgFile = Path.Combine(root, "Assets", "Scripts", "Physics", "SGMultiNodeThermal.cs");
            string engineFile = Path.Combine(root, "Assets", "Scripts", "Validation", "HeatupSimEngine.cs");
            string sgText = File.ReadAllText(sgFile);
            string engineText = File.ReadAllText(engineFile);

            bool hasScratchField = sgText.Contains("NodeMixingHeatScratch", StringComparison.Ordinal);
            bool hasConditionalReuse = sgText.Contains("if (mixingHeat == null || mixingHeat.Length != N)", StringComparison.Ordinal) &&
                                       sgText.Contains("Array.Clear(mixingHeat, 0, N);", StringComparison.Ordinal);
            eval.Cs0066Pass = hasScratchField && hasConditionalReuse;
            eval.Cs0066Reason =
                $"scratchField={hasScratchField}, conditionalReuse={hasConditionalReuse} (no per-step transient allocation path)";

            bool queueRemoved = !engineText.Contains("Queue<DynamicIntervalSample>", StringComparison.Ordinal);
            bool toArrayRemoved = !engineText.Contains("Queue.ToArray", StringComparison.Ordinal);
            bool ringWindowPresent = engineText.Contains("stageEDynamicWindow", StringComparison.Ordinal) &&
                                     engineText.Contains("stageEDynamicWindowHead", StringComparison.Ordinal);
            eval.Cs0067Pass = queueRemoved && toArrayRemoved && ringWindowPresent;
            eval.Cs0067Reason =
                $"queueRemoved={queueRemoved}, queueToArrayRemoved={toArrayRemoved}, ringWindowPresent={ringWindowPresent}";

            float simDurationSec = Mathf.Max(1f, run.FinalSimTimeHr * 3600f);
            float logRate = run.SimLogCount / simDurationSec;
            int burst2s = ComputeMaxBurst(run.LogTimesSec, 2f);
            eval.Cs0068Pass = logRate <= 5f && burst2s <= 20;
            eval.Cs0068Reason =
                $"simLogCount={run.SimLogCount}, rate={logRate:F3}/s (limit <=5), maxBurst2s={burst2s} (limit <=20)";

            return eval;
        }

        private static Group3Eval EvaluateGroup3(RunResult run, string root)
        {
            var eval = new Group3Eval();
            string visualFile = Path.Combine(root, "Assets", "Scripts", "Validation", "HeatupValidationVisual.cs");
            string visual = File.ReadAllText(visualFile);
            bool getterUsed = CountOccurrences(visual, "GetTelemetrySnapshot()") >= 2;
            bool snapshotReadsPresent =
                visual.Contains("var snap = _telemetrySnapshot;", StringComparison.Ordinal) &&
                visual.Contains("snap.PlantMode", StringComparison.Ordinal) &&
                visual.Contains("snap.HeatupPhaseDesc", StringComparison.Ordinal) &&
                visual.Contains("snap.SimTime", StringComparison.Ordinal) &&
                visual.Contains("snap.WallClockTime", StringComparison.Ordinal);
            bool snapshotRuntimeOk = run.SnapshotConsistent;

            eval.Cs0045Pass = getterUsed && snapshotReadsPresent && snapshotRuntimeOk;
            eval.Cs0045Reason =
                $"getterUsed={getterUsed}, snapshotReads={snapshotReadsPresent}, runtimeSnapshotConsistency={snapshotRuntimeOk} ({run.SnapshotReason})";
            return eval;
        }

        private static Group4Eval EvaluateGroup4(RunResult baseline, RunResult worker)
        {
            var eval = new Group4Eval();
            bool sameLength = baseline.HashSequence.Count == worker.HashSequence.Count;
            bool sequenceEqual = sameLength;
            if (sameLength)
            {
                for (int i = 0; i < baseline.HashSequence.Count; i++)
                {
                    if (baseline.HashSequence[i] != worker.HashSequence[i])
                    {
                        sequenceEqual = false;
                        break;
                    }
                }
            }

            bool terminalEqual = baseline.TerminalHash == worker.TerminalHash;
            bool workerPathUsed = worker.WorkerUsed;
            eval.Cs0046Pass = sequenceEqual && terminalEqual && workerPathUsed;
            eval.Cs0046Reason =
                $"sequenceEqual={sequenceEqual}, terminalEqual={terminalEqual}, workerPathUsed={workerPathUsed}, " +
                $"sequenceCount={baseline.HashSequence.Count}";
            eval.BaselineHash = $"0x{baseline.TerminalHash:X16}";
            eval.WorkerHash = $"0x{worker.TerminalHash:X16}";
            return eval;
        }

        private static StageEEval EvaluateStageE(RunResult legacy, RunResult remediated, Group4Eval g4)
        {
            var eval = new StageEEval();
            float legacyP95 = Percentile(legacy.StepMs, 95f);
            float legacyP99 = Percentile(legacy.StepMs, 99f);
            float remediatedP95 = Percentile(remediated.StepMs, 95f);
            float remediatedP99 = Percentile(remediated.StepMs, 99f);
            float remediatedLogRate = remediated.SimLogCount / Mathf.Max(1f, remediated.FinalSimTimeHr * 3600f);

            bool noBlockingRegression = remediatedP99 <= Mathf.Max(legacyP99, 8.0f);
            bool gcNonWorse = remediated.Gc0Delta <= legacy.Gc0Delta;
            bool logWithinCeiling = remediatedLogRate <= 5f;
            bool determinismGate = g4.Cs0046Pass;

            eval.Pass = noBlockingRegression && gcNonWorse && logWithinCeiling && determinismGate;
            eval.Reason =
                $"legacyP95={legacyP95:F3} ms legacyP99={legacyP99:F3} ms; remediatedP95={remediatedP95:F3} ms " +
                $"remediatedP99={remediatedP99:F3} ms; gc0 legacy/rem={legacy.Gc0Delta}/{remediated.Gc0Delta}; " +
                $"remLogRate={remediatedLogRate:F3}/s; determinismGate={determinismGate}";
            return eval;
        }

        private static void WriteGroup1(string path, RunResult run, Group1Eval eval)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0023 Group 1 Checkpoint");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run profile: `{run.Label}`");
            sb.AppendLine($"- Steps: {run.Steps}");
            sb.AppendLine($"- Final sim time: {run.FinalSimTimeHr:F3} hr");
            sb.AppendLine($"- Log directory: `{run.LogDir}`");
            if (!string.IsNullOrWhiteSpace(run.ReportPath))
                sb.AppendLine($"- Heatup report: `{run.ReportPath}`");
            sb.AppendLine();
            sb.AppendLine("## CS Results");
            sb.AppendLine($"- CS-0032: {(eval.Cs0032Pass ? "PASS" : "FAIL")} | {eval.Cs0032Reason}");
            sb.AppendLine($"- CS-0065: {(eval.Cs0065Pass ? "PASS" : "FAIL")} | {eval.Cs0065Reason}");
            sb.AppendLine($"- CS-0044: {(eval.Cs0044Pass ? "PASS" : "FAIL")} | {eval.Cs0044Reason}");
            sb.AppendLine();
            sb.AppendLine($"## Group 1 Overall: {(eval.Overall ? "PASS" : "FAIL")}");
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteGroup2(string path, RunResult run, Group2Eval eval)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0023 Group 2 Checkpoint");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run profile: `{run.Label}`");
            sb.AppendLine($"- Steps: {run.Steps}");
            sb.AppendLine($"- Final sim time: {run.FinalSimTimeHr:F3} hr");
            sb.AppendLine();
            sb.AppendLine("## CS Results");
            sb.AppendLine($"- CS-0066: {(eval.Cs0066Pass ? "PASS" : "FAIL")} | {eval.Cs0066Reason}");
            sb.AppendLine($"- CS-0067: {(eval.Cs0067Pass ? "PASS" : "FAIL")} | {eval.Cs0067Reason}");
            sb.AppendLine($"- CS-0068: {(eval.Cs0068Pass ? "PASS" : "FAIL")} | {eval.Cs0068Reason}");
            sb.AppendLine();
            sb.AppendLine($"## Group 2 Overall: {(eval.Overall ? "PASS" : "FAIL")}");
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteGroup3(string path, RunResult run, Group3Eval eval)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0023 Group 3 Checkpoint");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run profile: `{run.Label}`");
            sb.AppendLine($"- Steps: {run.Steps}");
            sb.AppendLine($"- Final sim time: {run.FinalSimTimeHr:F3} hr");
            sb.AppendLine();
            sb.AppendLine("## CS Results");
            sb.AppendLine($"- CS-0045: {(eval.Cs0045Pass ? "PASS" : "FAIL")} | {eval.Cs0045Reason}");
            sb.AppendLine();
            sb.AppendLine($"## Group 3 Overall: {(eval.Overall ? "PASS" : "FAIL")}");
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteGroup4(string path, RunResult baseline, RunResult worker, Group4Eval eval)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0023 Group 4 Checkpoint");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("- Deterministic Replay Validation Gate:");
            sb.AppendLine($"  - Baseline terminal hash: `{eval.BaselineHash}`");
            sb.AppendLine($"  - Worker terminal hash: `{eval.WorkerHash}`");
            sb.AppendLine($"  - Baseline sequence count: {baseline.HashSequence.Count}");
            sb.AppendLine($"  - Worker sequence count: {worker.HashSequence.Count}");
            sb.AppendLine($"  - Worker stepping observed: {worker.WorkerUsed}");
            sb.AppendLine();
            sb.AppendLine("## CS Results");
            sb.AppendLine($"- CS-0046: {(eval.Cs0046Pass ? "PASS" : "FAIL")} | {eval.Cs0046Reason}");
            sb.AppendLine();
            sb.AppendLine($"## Group 4 Overall: {(eval.Overall ? "PASS" : "FAIL")}");
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteStageD(
            string path,
            string group1Path,
            string group2Path,
            string group3Path,
            string group4Path,
            Group1Eval g1,
            Group2Eval g2,
            Group3Eval g3,
            Group4Eval g4)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0023 Stage D - Domain Validation (DP-0009)");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("- Scope: DP-0009 only (8 in-scope CS)");
            sb.AppendLine();
            sb.AppendLine("## Run Artifacts");
            sb.AppendLine($"- Group 1 checkpoint: `{ToWorkspaceRelative(group1Path)}`");
            sb.AppendLine($"- Group 2 checkpoint: `{ToWorkspaceRelative(group2Path)}`");
            sb.AppendLine($"- Group 3 checkpoint: `{ToWorkspaceRelative(group3Path)}`");
            sb.AppendLine($"- Group 4 checkpoint: `{ToWorkspaceRelative(group4Path)}`");
            sb.AppendLine();
            sb.AppendLine("## Per-CS Matrix");
            sb.AppendLine("| CS ID | Result | Evidence |");
            sb.AppendLine("|---|---|---|");
            sb.AppendLine($"| CS-0032 | {(g1.Cs0032Pass ? "PASS" : "FAIL")} | `{ToWorkspaceRelative(group1Path)}` |");
            sb.AppendLine($"| CS-0065 | {(g1.Cs0065Pass ? "PASS" : "FAIL")} | `{ToWorkspaceRelative(group1Path)}` |");
            sb.AppendLine($"| CS-0044 | {(g1.Cs0044Pass ? "PASS" : "FAIL")} | `{ToWorkspaceRelative(group1Path)}` |");
            sb.AppendLine($"| CS-0066 | {(g2.Cs0066Pass ? "PASS" : "FAIL")} | `{ToWorkspaceRelative(group2Path)}` |");
            sb.AppendLine($"| CS-0067 | {(g2.Cs0067Pass ? "PASS" : "FAIL")} | `{ToWorkspaceRelative(group2Path)}` |");
            sb.AppendLine($"| CS-0068 | {(g2.Cs0068Pass ? "PASS" : "FAIL")} | `{ToWorkspaceRelative(group2Path)}` |");
            sb.AppendLine($"| CS-0045 | {(g3.Cs0045Pass ? "PASS" : "FAIL")} | `{ToWorkspaceRelative(group3Path)}` |");
            sb.AppendLine($"| CS-0046 | {(g4.Cs0046Pass ? "PASS" : "FAIL")} | `{ToWorkspaceRelative(group4Path)}` |");
            sb.AppendLine();
            bool overall = g1.Overall && g2.Overall && g3.Overall && g4.Overall;
            sb.AppendLine("## Stage D Outcome");
            sb.AppendLine($"- Domain validation status: {(overall ? "PASS" : "FAIL")}");
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteStageE(
            string path,
            RunResult legacy,
            RunResult remediated,
            Group4Eval g4,
            StageEEval stageE,
            string group1Path,
            string group2Path,
            string group3Path,
            string group4Path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            var sb = new StringBuilder();

            float legacyP95 = Percentile(legacy.StepMs, 95f);
            float legacyP99 = Percentile(legacy.StepMs, 99f);
            float remP95 = Percentile(remediated.StepMs, 95f);
            float remP99 = Percentile(remediated.StepMs, 99f);
            float legacyLogRate = legacy.SimLogCount / Mathf.Max(1f, legacy.FinalSimTimeHr * 3600f);
            float remLogRate = remediated.SimLogCount / Mathf.Max(1f, remediated.FinalSimTimeHr * 3600f);

            sb.AppendLine("# IP-0023 Stage E - System Regression Validation");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("- Scope: runtime/performance convergence for DP-0009");
            sb.AppendLine();
            sb.AppendLine("## Referenced Stage D Artifacts");
            sb.AppendLine($"- Group 1 checkpoint: `{ToWorkspaceRelative(group1Path)}`");
            sb.AppendLine($"- Group 2 checkpoint: `{ToWorkspaceRelative(group2Path)}`");
            sb.AppendLine($"- Group 3 checkpoint: `{ToWorkspaceRelative(group3Path)}`");
            sb.AppendLine($"- Group 4 checkpoint: `{ToWorkspaceRelative(group4Path)}`");
            sb.AppendLine();
            sb.AppendLine("## Legacy-Approx vs Remediated Metrics");
            sb.AppendLine("| Metric | Legacy Approx | Remediated |");
            sb.AppendLine("|---|---:|---:|");
            sb.AppendLine($"| Step P95 (ms) | {legacyP95:F3} | {remP95:F3} |");
            sb.AppendLine($"| Step P99 (ms) | {legacyP99:F3} | {remP99:F3} |");
            sb.AppendLine($"| GC Gen0 count delta | {legacy.Gc0Delta} | {remediated.Gc0Delta} |");
            sb.AppendLine($"| Simulation log rate (/s) | {legacyLogRate:F3} | {remLogRate:F3} |");
            sb.AppendLine($"| Async dispatch max (ms) | {legacy.AsyncMaxDispatchMs:F3} | {remediated.AsyncMaxDispatchMs:F3} |");
            sb.AppendLine($"| Deterministic replay gate | N/A | {(g4.Cs0046Pass ? "PASS" : "FAIL")} |");
            sb.AppendLine();
            sb.AppendLine("## Stage E Outcome");
            sb.AppendLine($"- Result: {(stageE.Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- Basis: {stageE.Reason}");

            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteRecommendation(
            string path,
            string stageDPath,
            string stageEPath,
            Group1Eval g1,
            Group2Eval g2,
            Group3Eval g3,
            Group4Eval g4,
            StageEEval stageE)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            bool closeRecommended = g1.Overall && g2.Overall && g3.Overall && g4.Overall && stageE.Pass;
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0023 Closure Recommendation (DP-0009)");
            sb.AppendLine();
            sb.AppendLine($"- Date: {DateTime.Now:yyyy-MM-dd}");
            sb.AppendLine($"- Recommendation: **{(closeRecommended ? "CLOSE_RECOMMENDED" : "DO_NOT_CLOSE")}**");
            sb.AppendLine();
            sb.AppendLine("## Basis");
            sb.AppendLine($"- CS-0032: {(g1.Cs0032Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- CS-0065: {(g1.Cs0065Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- CS-0044: {(g1.Cs0044Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- CS-0066: {(g2.Cs0066Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- CS-0067: {(g2.Cs0067Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- CS-0068: {(g2.Cs0068Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- CS-0045: {(g3.Cs0045Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- CS-0046: {(g4.Cs0046Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- Stage D report: `{ToWorkspaceRelative(stageDPath)}`");
            sb.AppendLine($"- Stage E report: `{ToWorkspaceRelative(stageEPath)}`");
            sb.AppendLine();
            sb.AppendLine("## Residual Risk");
            sb.AppendLine("- Legacy approximation in Stage E uses current code toggles (async/log controls) rather than historical binary replay.");

            File.WriteAllText(path, sb.ToString());
        }

        private static ulong ComputeStateHash(HeatupSimEngine e)
        {
            ulong h = 1469598103934665603UL;
            h = AddHash(h, e.simTime);
            h = AddHash(h, e.pressure);
            h = AddHash(h, e.T_rcs);
            h = AddHash(h, e.T_avg);
            h = AddHash(h, e.T_pzr);
            h = AddHash(h, e.pzrLevel);
            h = AddHash(h, e.chargingFlow);
            h = AddHash(h, e.letdownFlow);
            h = AddHash(h, e.rcpCount);
            h = AddHash(h, (int)e.bubblePhase);
            h = AddHash(h, e.sgSecondaryPressure_psia);
            h = AddHash(h, e.sgSteamInventory_lb);
            return h;
        }

        private static ulong AddHash(ulong h, int value)
        {
            unchecked
            {
                h ^= (uint)value;
                return h * 1099511628211UL;
            }
        }

        private static ulong AddHash(ulong h, float value)
        {
            return AddHash(h, BitConverter.SingleToInt32Bits(value));
        }

        private static void PrepareLogDirectory(string logDir)
        {
            Directory.CreateDirectory(logDir);
            foreach (string file in Directory.GetFiles(logDir, "*.txt"))
                File.Delete(file);
        }

        private static float Percentile(List<float> values, float percentile)
        {
            if (values == null || values.Count == 0)
                return 0f;
            List<float> ordered = values.OrderBy(v => v).ToList();
            float rank = (percentile / 100f) * (ordered.Count - 1);
            int lo = Mathf.Clamp((int)Mathf.Floor(rank), 0, ordered.Count - 1);
            int hi = Mathf.Clamp((int)Mathf.Ceil(rank), 0, ordered.Count - 1);
            if (lo == hi)
                return ordered[lo];
            float t = rank - lo;
            return Mathf.Lerp(ordered[lo], ordered[hi], t);
        }

        private static int ComputeMaxBurst(List<float> timesSec, float windowSec)
        {
            if (timesSec == null || timesSec.Count == 0)
                return 0;
            List<float> sorted = timesSec.OrderBy(v => v).ToList();
            int max = 1;
            int j = 0;
            for (int i = 0; i < sorted.Count; i++)
            {
                while (j < sorted.Count && sorted[j] - sorted[i] <= windowSec)
                    j++;
                int count = j - i;
                if (count > max)
                    max = count;
            }
            return max;
        }

        private static int CountOccurrences(string text, string needle)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(needle))
                return 0;

            int count = 0;
            int idx = 0;
            while (true)
            {
                idx = text.IndexOf(needle, idx, StringComparison.Ordinal);
                if (idx < 0)
                    break;
                count++;
                idx += needle.Length;
            }

            return count;
        }

        private static string ToWorkspaceRelative(string path)
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string normalizedRoot = root.Replace('\\', '/').TrimEnd('/');
            string normalizedPath = path.Replace('\\', '/');
            if (normalizedPath.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase))
                return normalizedPath.Substring(normalizedRoot.Length + 1);
            return normalizedPath;
        }
    }
}
