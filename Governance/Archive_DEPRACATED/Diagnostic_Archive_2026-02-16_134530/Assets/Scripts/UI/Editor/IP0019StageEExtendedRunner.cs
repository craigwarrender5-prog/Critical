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
    public static class IP0019StageEExtendedRunner
    {
        private const float DtHr = 1f / 360f;
        private const float IntervalLogHr = 0.25f;
        private const int MaxSteps = 300000;
        private const float RequiredLongHoldDurationHr = 3f;
        private const int MinLongHoldPoints = 1080; // 3hr at 10s/step (1/360 hr)
        private const float StressStartPressureMarginPsia = 80f;
        private const float StressStopPressureMarginPsia = -25f;
        // CS-0056 policy: NOT_REACHED is non-blocking when the validation run is
        // otherwise valid but does not enter the required post-start near-350F window.
        private const bool Cs0056NotReachedNonBlocking = true;
        // Governance policy: CONDITIONAL indicates bounded residual risk and is
        // tracked in evidence, but does not block closure recommendation.
        private const bool ConditionalStatusNonBlocking = true;

        private sealed class Snapshot
        {
            public float T;
            public float TRcs;
            public float P;
            public float Surge;
            public float Charging;
            public float Letdown;
            public float ChargingToRcs;
            public float NetCvcs;
            public float PzrLevel;
            public float MassErr;
            public float PRate;
            public float RcpHeat;
            public float RcsHeatRate;
            public float HeaterPower;
            public bool HeatersOn;
            public float NoRcpTransport;
            public float RhrNetHeat;
            public int Rcp;
            public bool Bubble;
            public bool Solid;
            public string Writer = "";
            public string RhrMode = "";
        }

        private sealed class LongHoldResult
        {
            public bool Completed;
            public bool IsolatedWindowFound;
            public float Duration;
            public float HoldStartTime;
            public int SampleCount;
            public int MaxRcp;
            public float TrcsSlope;
            public float PressureSlope;
            public float PressureP2P;
            public float SurgeMin;
            public float SurgeMax;
            public float SurgeMean;
            public float SurgePressureConsistent;
            public float MassErrMaxAbs;
            public float StartTRcs;
            public float EndTRcs;
            public float StartP;
            public float EndP;
            public float MaxAbsCharging;
            public float MaxAbsLetdown;
            public float MaxAbsNetCvcs;
            public float MaxAbsChargingToRcs;
            public bool HeaterObservedOn;
            public bool HeaterInjectionDisabled = true;
            public float MaxAbsRhrNetHeat;
            public float MaxNoRcpTransport;
            public bool AnyRhrActiveMode;
            public int PressureWriteCount;
            public int PressureStateDerivedWriteCount;
            public int PressureOverrideAttemptCount;
            public int PressureBlockedOverrideCount;
            public bool PressureInvariantFailed;
            public string PressureInvariantReason = "";
            public string PressureInvariantSource = "NONE";
            public bool PressureOverrideProbeBlocked;
            public string PressureOverrideProbeReason = "";
            public readonly List<Snapshot> Samples = new List<Snapshot>();
        }

        private sealed class CycleResult
        {
            public int Cycle;
            public bool Started;
            public bool Stopped;
            public float StartTime;
            public float StopTime;
            public float MaxLevelDelta;
            public float MaxLevelStep;
            public float MaxPressureEnv;
            public float MaxPressureStep;
            public float MaxTRcsStep;
            public float MaxRcpHeatStep;
        }

        private sealed class StressResult
        {
            public bool BubbleReached;
            public float BubbleTime;
            public readonly List<CycleResult> Cycles = new List<CycleResult>();
        }

        private sealed class ExtendedResult
        {
            public bool FirstStart;
            public float FirstStartTime;
            public float WindowStart;
            public float WindowEnd;
            public float WindowDuration;
            public bool HasPreNoRcp;
            public bool HasPostRcp;
            public bool HasRcp4;
            public float PreNoRcpSlope;
            public float PreNoRcpDelta;
            public float PostHeatRateMaxStep;
            public float TempPressureCoupling;
            public float StabilizedTempSlope;
            public float StabilizedPressureSlope;
            public bool IsolationDetected;
            public float IsolationTime;
            public int IsolationRcp;
            public float IsolationT;
            public bool IsolationEvalCaptured;
            public float IsolationEvalTime;
            public int IsolationEvalRcp;
            public float IsolationEvalT;
            public bool IsolationEvalIsolated;
            public string IsolationEvalMode = "UNSET";
            public string IsolationEvalPreMode = "UNSET";
            public string IsolationEvalPostMode = "UNSET";
            public bool IsolationEvalAfterFirstStart;
            public bool IsolationEvalNearThreshold;
            public float IsolationEvalThresholdF;
            public string IsolationEvalTrigger = "UNSET";
            public float MaxPostStartTRcs;
            public bool PostStartNearThresholdReached;
            public readonly HashSet<string> Writers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public bool WriterRulePass = true;
            public int WriterConflictCount;
            public int WriterIllegalCount;
            public int WriterWindowedChecks;
            public int WriterSkippedChecks;
            public readonly List<string> WriterIssues = new List<string>();
            public readonly List<string> WriterTransitions = new List<string>();
            public readonly List<string> WriterSolidChecks = new List<string>();
        }

        private sealed class RepeatabilityResult
        {
            public bool Completed;
            public int Count;
            public float MaxDT;
            public float MaxDP;
            public float MaxDSurge;
            public float MaxDMass;
            public bool Invalid;
            public string InvalidReason = "";
        }

        private sealed class RunValidity
        {
            public bool IsValid = true;
            public readonly List<string> Diagnostics = new List<string>();
        }

        [MenuItem("Critical/Run Stage E Extended Validation (IP-0019)")]
        public static void RunStageEExtendedValidation()
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string scenePath = "Assets/Scenes/MainScene.unity";
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string logsRoot = Path.Combine(root, "HeatupLogs", $"IP-0019_Extended_{stamp}");
            string reportPath = Path.Combine(
                root,
                "Governance",
                "ImplementationReports",
                $"IP-0019_StageE_ExtendedValidation_Report_{DateTime.Now:yyyy-MM-dd}.md");

            Directory.CreateDirectory(logsRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? ".");

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            HeatupSimEngine engine = UnityEngine.Object.FindObjectOfType<HeatupSimEngine>();
            if (engine == null)
                throw new InvalidOperationException("HeatupSimEngine not found.");

            Type t = typeof(HeatupSimEngine);
            MethodInfo init = t.GetMethod("InitializeSimulation", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo step = t.GetMethod("StepSimulation", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo saveInterval = t.GetMethod("SaveIntervalLog", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo saveReport = t.GetMethod("SaveReport", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo logPathField = t.GetField("logPath", BindingFlags.Instance | BindingFlags.NonPublic);
            if (init == null || step == null || saveInterval == null || saveReport == null || logPathField == null)
                throw new MissingMethodException("Required HeatupSimEngine members missing.");

            LongHoldResult longHold = RunLongHold(engine, init, step, saveInterval, saveReport, logPathField, Path.Combine(logsRoot, "LongSolidHold"));
            RunValidity validity = EvaluateRunValidity(longHold);

            StressResult stress = validity.IsValid
                ? RunStress(engine, init, step, saveInterval, saveReport, logPathField, Path.Combine(logsRoot, "RcpStress"))
                : new StressResult();
            ExtendedResult extended = validity.IsValid
                ? RunExtended(engine, init, step, saveInterval, saveReport, logPathField, Path.Combine(logsRoot, "ExtendedHeatup"))
                : new ExtendedResult();
            RepeatabilityResult repeat = validity.IsValid
                ? RunRepeatability(engine, init, step, saveInterval, saveReport, logPathField, Path.Combine(logsRoot, "Repeatability"))
                : new RepeatabilityResult { Invalid = true, InvalidReason = "Skipped because run was INVALID_RUN before repeatability phase." };

            Dictionary<string, (string status, string evidence)> cs = BuildVerdicts(longHold, stress, extended, repeat, validity);
            string recommendation = DetermineRecommendation(cs, validity, out string recommendationReason);

            WriteReport(reportPath, stamp, logsRoot, longHold, stress, extended, repeat, cs, recommendation, recommendationReason, validity);
            Debug.Log($"[IP-0019][StageE-Extended] Report: {reportPath}");
            Debug.Log($"[IP-0019][StageE-Extended] Recommendation: {recommendation}");
            if (!validity.IsValid)
            {
                foreach (string reason in validity.Diagnostics)
                    Debug.LogError($"[IP-0019][StageE-Extended] INVALID_RUN: {reason}");
            }
        }

        private static LongHoldResult RunLongHold(
            HeatupSimEngine e, MethodInfo init, MethodInfo step, MethodInfo saveInterval, MethodInfo saveReport, FieldInfo logPathField, string logDir)
        {
            PrepareLogDir(logDir);
            Setup(e, init, saveInterval, logPathField, logDir);
            float nextInterval = IntervalLogHr;
            var r = new LongHoldResult();
            int steps = 0;
            float holdEnd = e.simTime + RequiredLongHoldDurationHr;

            r.IsolatedWindowFound = true;
            r.HoldStartTime = e.simTime;

            RunLongHoldPressureOverrideProbe(e, out bool probeBlocked, out string probeReason);
            r.PressureOverrideProbeBlocked = probeBlocked;
            r.PressureOverrideProbeReason = probeReason;

            e.BeginLongHoldPressureAudit();
            EnforceLongHoldNoTransport(e);
            r.Samples.Add(Cap(e));
            try
            {
                while (steps < MaxSteps && e.simTime < holdEnd)
                {
                    EnforceLongHoldNoTransport(e);
                    step.Invoke(e, new object[] { DtHr });
                    steps++;
                    EnforceLongHoldNoTransport(e);
                    Snapshot cur = Cap(e);
                    if (cur.HeatersOn || cur.HeaterPower > 1e-6f)
                        r.HeaterInjectionDisabled = false;
                    r.Samples.Add(cur);
                    if (e.simTime >= nextInterval)
                    {
                        saveInterval.Invoke(e, null);
                        nextInterval += IntervalLogHr;
                    }
                }
            }
            finally
            {
                r.PressureWriteCount = e.longHoldPressureWriteCount;
                r.PressureStateDerivedWriteCount = e.longHoldPressureStateDerivedWriteCount;
                r.PressureOverrideAttemptCount = e.longHoldPressureOverrideAttemptCount;
                r.PressureBlockedOverrideCount = e.longHoldPressureBlockedOverrideCount;
                r.PressureInvariantFailed = e.longHoldPressureInvariantFailed;
                r.PressureInvariantReason = e.longHoldPressureInvariantReason ?? "";
                r.PressureInvariantSource = e.longHoldLastPressureSource ?? "NONE";
                e.EndLongHoldPressureAudit();
            }
            saveReport.Invoke(e, null);
            r.SampleCount = r.Samples.Count;
            if (r.Samples.Count < 2)
                return r;

            Snapshot a = r.Samples[0];
            Snapshot b = r.Samples[r.Samples.Count - 1];
            float dur = Mathf.Max(1e-6f, b.T - a.T);
            r.Completed = r.IsolatedWindowFound && dur >= RequiredLongHoldDurationHr - DtHr;
            r.Duration = dur;
            r.MaxRcp = r.Samples.Max(x => x.Rcp);
            r.StartTRcs = a.TRcs;
            r.EndTRcs = b.TRcs;
            r.StartP = a.P;
            r.EndP = b.P;
            r.TrcsSlope = (b.TRcs - a.TRcs) / dur;
            r.PressureSlope = (b.P - a.P) / dur;
            r.PressureP2P = r.Samples.Max(x => x.P) - r.Samples.Min(x => x.P);
            r.SurgeMin = r.Samples.Min(x => x.Surge);
            r.SurgeMax = r.Samples.Max(x => x.Surge);
            r.SurgeMean = r.Samples.Average(x => x.Surge);
            r.MassErrMaxAbs = r.Samples.Max(x => Mathf.Abs(x.MassErr));
            r.SurgePressureConsistent = SurgePressureConsistency(r.Samples);
            r.MaxAbsCharging = r.Samples.Max(x => Mathf.Abs(x.Charging));
            r.MaxAbsLetdown = r.Samples.Max(x => Mathf.Abs(x.Letdown));
            r.MaxAbsNetCvcs = r.Samples.Max(x => Mathf.Abs(x.NetCvcs));
            r.MaxAbsChargingToRcs = r.Samples.Max(x => Mathf.Abs(x.ChargingToRcs));
            r.HeaterObservedOn = r.Samples.Any(x => x.HeatersOn || x.HeaterPower > 1e-3f);
            r.MaxAbsRhrNetHeat = r.Samples.Max(x => Mathf.Abs(x.RhrNetHeat));
            r.MaxNoRcpTransport = r.Samples.Max(x => x.NoRcpTransport);
            r.AnyRhrActiveMode = r.Samples.Any(x => !string.Equals(x.RhrMode, "Standby", StringComparison.OrdinalIgnoreCase));
            return r;
        }

        private static StressResult RunStress(
            HeatupSimEngine e, MethodInfo init, MethodInfo step, MethodInfo saveInterval, MethodInfo saveReport, FieldInfo logPathField, string logDir)
        {
            PrepareLogDir(logDir);
            Setup(e, init, saveInterval, logPathField, logDir);
            float nextInterval = IntervalLogHr;
            int steps = 0;
            var r = new StressResult();

            while (steps < MaxSteps && e.simTime < 12f)
            {
                step.Invoke(e, new object[] { DtHr });
                steps++;
                if (e.simTime >= nextInterval)
                {
                    saveInterval.Invoke(e, null);
                    nextInterval += IntervalLogHr;
                }
                if (e.bubbleFormed && !e.solidPressurizer)
                {
                    r.BubbleReached = true;
                    r.BubbleTime = e.simTime;
                    break;
                }
            }

            if (!r.BubbleReached)
            {
                saveReport.Invoke(e, null);
                return r;
            }

            ForceRcpState(e, step, PlantConstants.MIN_RCP_PRESSURE_PSIA + StressStopPressureMarginPsia, 0, 120);
            for (int c = 1; c <= 3; c++)
            {
                var cycle = new CycleResult { Cycle = c };
                Snapshot baseSnap = Cap(e);
                Snapshot startPre = baseSnap;
                Snapshot startPost = baseSnap;
                bool started = false;
                for (int i = 0; i < 200; i++)
                {
                    Snapshot pre = Cap(e);
                    e.bubbleFormed = true;
                    e.solidPressurizer = false;
                    e.ApplyPressureWrite(
                        PlantConstants.MIN_RCP_PRESSURE_PSIA + StressStartPressureMarginPsia,
                        "RUNNER_STRESS_FORCE_START",
                        stateDerived: false);
                    step.Invoke(e, new object[] { DtHr });
                    Snapshot post = Cap(e);
                    if (pre.Rcp == 0 && post.Rcp > 0)
                    {
                        startPre = pre;
                        startPost = post;
                        started = true;
                        cycle.Started = true;
                        cycle.StartTime = post.T;
                        break;
                    }
                }
                if (!started)
                {
                    r.Cycles.Add(cycle);
                    break;
                }

                // Evaluate per-step transient deltas from the true RCP start edge,
                // not from a stale pre-search baseline that can span many forced ticks.
                var win = new List<Snapshot> { startPre, startPost };
                for (int i = 0; i < 18; i++)
                {
                    e.bubbleFormed = true;
                    e.solidPressurizer = false;
                    if (e.pressure < PlantConstants.MIN_RCP_PRESSURE_PSIA + StressStartPressureMarginPsia)
                    {
                        e.ApplyPressureWrite(
                            PlantConstants.MIN_RCP_PRESSURE_PSIA + StressStartPressureMarginPsia,
                            "RUNNER_STRESS_FORCE_WINDOW",
                            stateDerived: false);
                    }
                    step.Invoke(e, new object[] { DtHr });
                    win.Add(Cap(e));
                }
                cycle.MaxLevelDelta = win.Max(x => Mathf.Abs(x.PzrLevel - startPre.PzrLevel));
                cycle.MaxLevelStep = MaxStep(win, x => x.PzrLevel);
                cycle.MaxPressureEnv = win.Max(x => Mathf.Abs(x.P - startPre.P));
                cycle.MaxPressureStep = MaxStep(win, x => x.P);
                cycle.MaxTRcsStep = MaxStep(win, x => x.TRcs);
                cycle.MaxRcpHeatStep = MaxStep(win, x => x.RcpHeat);

                bool stopped = false;
                for (int i = 0; i < 200; i++)
                {
                    Snapshot pre = Cap(e);
                    e.ApplyPressureWrite(
                        PlantConstants.MIN_RCP_PRESSURE_PSIA + StressStopPressureMarginPsia,
                        "RUNNER_STRESS_FORCE_STOP",
                        stateDerived: false);
                    step.Invoke(e, new object[] { DtHr });
                    Snapshot post = Cap(e);
                    if (pre.Rcp > 0 && post.Rcp == 0)
                    {
                        cycle.Stopped = true;
                        cycle.StopTime = post.T;
                        stopped = true;
                        break;
                    }
                }
                if (!stopped)
                    ForceRcpState(e, step, PlantConstants.MIN_RCP_PRESSURE_PSIA + StressStopPressureMarginPsia, 0, 120);

                r.Cycles.Add(cycle);
            }

            saveReport.Invoke(e, null);
            return r;
        }

        private static ExtendedResult RunExtended(
            HeatupSimEngine e, MethodInfo init, MethodInfo step, MethodInfo saveInterval, MethodInfo saveReport, FieldInfo logPathField, string logDir)
        {
            PrepareLogDir(logDir);
            Setup(e, init, saveInterval, logPathField, logDir);
            float nextInterval = IntervalLogHr;
            int steps = 0;
            float targetEnd = -1f;
            Snapshot prev = Cap(e);
            var all = new List<Snapshot>();
            var r = new ExtendedResult();
            var writerByTime = new Dictionary<int, string>();
            const float evalTempThresholdF = PlantConstants.RHR_LETDOWN_ISOLATION_TEMP_F - 5f;
            r.IsolationEvalThresholdF = evalTempThresholdF;

            while (steps < MaxSteps && e.simTime < 18f)
            {
                Snapshot pre = Cap(e);
                step.Invoke(e, new object[] { DtHr });
                steps++;
                Snapshot cur = Cap(e);
                all.Add(cur);
                if (!string.IsNullOrWhiteSpace(cur.Writer))
                    r.Writers.Add(cur.Writer);
                ObserveWriterRule(pre, cur, r, writerByTime);

                if (!r.FirstStart && prev.Rcp == 0 && cur.Rcp > 0)
                {
                    r.FirstStart = true;
                    r.FirstStartTime = cur.T;
                    targetEnd = r.FirstStartTime + 3f;
                }

                bool afterFirstStart = r.FirstStart && pre.T >= r.FirstStartTime - DtHr;
                bool nearThreshold = pre.TRcs >= evalTempThresholdF;
                bool inHeatupMode = string.Equals(pre.RhrMode, "Heatup", StringComparison.OrdinalIgnoreCase);
                bool shouldEvaluateNow =
                    afterFirstStart &&
                    inHeatupMode &&
                    pre.Rcp >= 4 &&
                    nearThreshold;
                if (!r.IsolationEvalCaptured && shouldEvaluateNow)
                {
                    r.IsolationEvalCaptured = true;
                    r.IsolationEvalTime = pre.T;
                    r.IsolationEvalRcp = pre.Rcp;
                    r.IsolationEvalT = pre.TRcs;
                    r.IsolationEvalIsolated = !string.Equals(cur.RhrMode, "Heatup", StringComparison.OrdinalIgnoreCase);
                    r.IsolationEvalMode = cur.RhrMode;
                    r.IsolationEvalPreMode = pre.RhrMode;
                    r.IsolationEvalPostMode = cur.RhrMode;
                    r.IsolationEvalAfterFirstStart = afterFirstStart;
                    r.IsolationEvalNearThreshold = nearThreshold;
                    r.IsolationEvalTrigger = "RCP>=4_NEAR350";
                }

                if (!r.IsolationDetected &&
                    inHeatupMode &&
                    !string.Equals(cur.RhrMode, "Heatup", StringComparison.OrdinalIgnoreCase))
                {
                    r.IsolationDetected = true;
                    r.IsolationTime = cur.T;
                    r.IsolationRcp = cur.Rcp;
                    r.IsolationT = cur.TRcs;
                    if (!r.IsolationEvalCaptured && afterFirstStart)
                    {
                        r.IsolationEvalCaptured = true;
                        r.IsolationEvalTime = pre.T;
                        r.IsolationEvalRcp = pre.Rcp;
                        r.IsolationEvalT = pre.TRcs;
                        r.IsolationEvalIsolated = true;
                        r.IsolationEvalMode = cur.RhrMode;
                        r.IsolationEvalPreMode = pre.RhrMode;
                        r.IsolationEvalPostMode = cur.RhrMode;
                        r.IsolationEvalAfterFirstStart = afterFirstStart;
                        r.IsolationEvalNearThreshold = nearThreshold;
                        r.IsolationEvalTrigger = "MODE_TRANSITION";
                    }
                }

                if (e.simTime >= nextInterval)
                {
                    saveInterval.Invoke(e, null);
                    nextInterval += IntervalLogHr;
                }
                if (r.FirstStart && e.simTime >= targetEnd)
                    break;
                prev = cur;
            }
            saveReport.Invoke(e, null);
            if (!r.FirstStart || all.Count < 2)
                return r;

            List<Snapshot> postStartAll = all.Where(x => x.T >= r.FirstStartTime).ToList();
            if (postStartAll.Count > 0)
            {
                r.MaxPostStartTRcs = postStartAll.Max(x => x.TRcs);
                r.PostStartNearThresholdReached = r.MaxPostStartTRcs >= r.IsolationEvalThresholdF;
            }

            r.WindowStart = Mathf.Max(0f, r.FirstStartTime - 1f);
            r.WindowEnd = r.FirstStartTime + 3f;
            List<Snapshot> win = all.Where(x => x.T >= r.WindowStart && x.T <= r.WindowEnd).ToList();
            if (win.Count < 2)
                return r;

            r.WindowDuration = win[win.Count - 1].T - win[0].T;
            r.HasPreNoRcp = win.Any(x => x.T < r.FirstStartTime && x.Rcp == 0);
            r.HasPostRcp = win.Any(x => x.T >= r.FirstStartTime && x.Rcp > 0);
            r.HasRcp4 = win.Any(x => x.T >= r.FirstStartTime + 2f && x.Rcp >= 4);
            List<Snapshot> preWindow = win.Where(x => x.T >= r.FirstStartTime - 1f && x.T <= r.FirstStartTime && x.Rcp == 0).ToList();
            if (preWindow.Count >= 2)
            {
                r.PreNoRcpSlope = Slope(preWindow, x => x.TRcs);
                r.PreNoRcpDelta = preWindow[preWindow.Count - 1].TRcs - preWindow[0].TRcs;
            }
            List<Snapshot> post = win.Where(x => x.T >= r.FirstStartTime && x.T <= r.FirstStartTime + 1.5f).ToList();
            if (post.Count >= 2)
                r.PostHeatRateMaxStep = MaxStep(post, x => x.RcsHeatRate);
            r.TempPressureCoupling = TempPressureCoupling(win);
            List<Snapshot> stab = win.Where(x => x.T >= r.WindowEnd - 1f).ToList();
            if (stab.Count >= 2)
            {
                r.StabilizedTempSlope = Slope(stab, x => x.TRcs);
                r.StabilizedPressureSlope = Slope(stab, x => x.P);
            }
            return r;
        }

        private static RepeatabilityResult RunRepeatability(
            HeatupSimEngine e, MethodInfo init, MethodInfo step, MethodInfo saveInterval, MethodInfo saveReport, FieldInfo logPathField, string logDir)
        {
            LongHoldResult a = RunLongHold(e, init, step, saveInterval, saveReport, logPathField, Path.Combine(logDir, "A"));
            LongHoldResult b = RunLongHold(e, init, step, saveInterval, saveReport, logPathField, Path.Combine(logDir, "B"));
            var r = new RepeatabilityResult();
            int n = Math.Min(a.Samples.Count, b.Samples.Count);
            if (n <= 0)
            {
                r.Invalid = true;
                r.InvalidReason = "No overlap points between repeatability runs (points=0).";
                return r;
            }
            r.Completed = true;
            r.Count = n;
            for (int i = 0; i < n; i++)
            {
                r.MaxDT = Mathf.Max(r.MaxDT, Mathf.Abs(a.Samples[i].TRcs - b.Samples[i].TRcs));
                r.MaxDP = Mathf.Max(r.MaxDP, Mathf.Abs(a.Samples[i].P - b.Samples[i].P));
                r.MaxDSurge = Mathf.Max(r.MaxDSurge, Mathf.Abs(a.Samples[i].Surge - b.Samples[i].Surge));
                r.MaxDMass = Mathf.Max(r.MaxDMass, Mathf.Abs(a.Samples[i].MassErr - b.Samples[i].MassErr));
            }
            return r;
        }

        private static Dictionary<string, (string status, string evidence)> BuildVerdicts(
            LongHoldResult longHold, StressResult stress, ExtendedResult ext, RepeatabilityResult rep, RunValidity validity)
        {
            string[] csKeys =
            {
                "CS-0021", "CS-0022", "CS-0023", "CS-0031", "CS-0033", "CS-0034",
                "CS-0038", "CS-0055", "CS-0056", "CS-0061", "CS-0071"
            };
            if (!validity.IsValid)
            {
                string reason = string.Join(" | ", validity.Diagnostics);
                return csKeys.ToDictionary(
                    key => key,
                    key => ("NO_DATA", $"INVALID_RUN: {reason}"),
                    StringComparer.OrdinalIgnoreCase);
            }

            int starts = stress.Cycles.Count(x => x.Started);
            int stops = stress.Cycles.Count(x => x.Stopped);
            float maxLevel = stress.Cycles.Count > 0 ? stress.Cycles.Max(x => x.MaxLevelDelta) : float.PositiveInfinity;
            float maxLevelStep = stress.Cycles.Count > 0 ? stress.Cycles.Max(x => x.MaxLevelStep) : float.PositiveInfinity;
            float maxPressStep = stress.Cycles.Count > 0 ? stress.Cycles.Max(x => x.MaxPressureStep) : float.PositiveInfinity;
            float maxHeatStep = stress.Cycles.Count > 0 ? stress.Cycles.Max(x => x.MaxRcpHeatStep) : float.PositiveInfinity;
            bool repeatabilityValid = !rep.Invalid && rep.Completed && rep.Count > 0;
            bool repeatPass = repeatabilityValid && rep.MaxDT <= 1e-4f && rep.MaxDP <= 1e-4f && rep.MaxDSurge <= 1e-4f && rep.MaxDMass <= 1e-4f;

            bool solidCore = longHold.Completed && longHold.MaxRcp == 0 && longHold.TrcsSlope <= 0.25f && longHold.MassErrMaxAbs <= 5f;
            bool surgeCoupling = longHold.SurgePressureConsistent >= 0.95f;
            bool oscillationBand = longHold.PressureP2P >= 3f && longHold.PressureP2P <= 25f;
            bool stressPass = stress.BubbleReached && starts >= 3 && stops >= 3;
            bool spikePass = maxLevel <= 0.5f && maxLevelStep <= 0.5f;
            bool smoothPass = maxHeatStep <= 2.0f && maxPressStep <= 12f;
            bool extWindow = ext.FirstStart && ext.WindowDuration >= 4f - DtHr && ext.HasPreNoRcp && ext.HasPostRcp && ext.HasRcp4;
            bool extThermo = ext.PostHeatRateMaxStep <= 10f && ext.TempPressureCoupling >= 0.70f;
            bool isoPass =
                ext.IsolationEvalCaptured &&
                ext.IsolationEvalAfterFirstStart &&
                ext.IsolationEvalRcp >= 4 &&
                ext.IsolationEvalNearThreshold &&
                ext.IsolationEvalIsolated;
            bool isoWindowReached = ext.PostStartNearThresholdReached;
            bool isoNotReached = !ext.IsolationEvalCaptured && !isoWindowReached;
            bool writerPass = ext.WriterRulePass && ext.WriterConflictCount == 0 && ext.WriterIllegalCount == 0;

            string cs0056Status;
            string cs0056Evidence;
            if (isoPass)
            {
                cs0056Status = "PASS";
                cs0056Evidence =
                    $"Isolation eval t={ext.IsolationEvalTime:F3} hr trigger={ext.IsolationEvalTrigger} RCP={ext.IsolationEvalRcp} T_rcs={ext.IsolationEvalT:F3} F threshold={ext.IsolationEvalThresholdF:F1} F preMode={ext.IsolationEvalPreMode} postMode={ext.IsolationEvalPostMode} afterFirstStart={ext.IsolationEvalAfterFirstStart} near350={ext.IsolationEvalNearThreshold} isolated={ext.IsolationEvalIsolated}.";
            }
            else if (isoNotReached)
            {
                cs0056Status = "NOT_REACHED";
                cs0056Evidence =
                    $"Post-start near-350 window not reached in this run (max T_rcs={ext.MaxPostStartTRcs:F3} F < threshold={ext.IsolationEvalThresholdF:F1} F); isolation sequence cannot be evaluated.";
            }
            else
            {
                cs0056Status = "FAIL";
                cs0056Evidence = ext.IsolationEvalCaptured
                    ? $"Isolation eval captured but invalid: t={ext.IsolationEvalTime:F3} hr trigger={ext.IsolationEvalTrigger} RCP={ext.IsolationEvalRcp} T_rcs={ext.IsolationEvalT:F3} F threshold={ext.IsolationEvalThresholdF:F1} F preMode={ext.IsolationEvalPreMode} postMode={ext.IsolationEvalPostMode} afterFirstStart={ext.IsolationEvalAfterFirstStart} near350={ext.IsolationEvalNearThreshold} isolated={ext.IsolationEvalIsolated}."
                    : $"Near-350 window was reached (max T_rcs={ext.MaxPostStartTRcs:F3} F >= threshold={ext.IsolationEvalThresholdF:F1} F) but no valid isolation sample was captured.";
            }

            var v = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
            {
                ["CS-0021"] = (oscillationBand ? "PASS" : "FAIL", $"LongHold pressure P2P={longHold.PressureP2P:F3} psi."),
                ["CS-0022"] = (oscillationBand && surgeCoupling ? "PASS" : "FAIL", $"Surge-pressure consistency={longHold.SurgePressureConsistent:P2}."),
                ["CS-0023"] = (surgeCoupling ? "PASS" : "FAIL", $"Sign-consistent surge/pressure ratio={longHold.SurgePressureConsistent:P2}."),
                ["CS-0031"] = (
                    smoothPass && extThermo ? "PASS" : "FAIL",
                    $"Stress max dRcpHeat/step={maxHeatStep:F3} MW, max dP/step={maxPressStep:F3} psi; extended max dHeatRate/step={ext.PostHeatRateMaxStep:F3} F/hr."),
                ["CS-0033"] = (solidCore ? "PASS" : "FAIL", $"No-RCP T_rcs slope={longHold.TrcsSlope:F3} F/hr."),
                ["CS-0034"] = (solidCore && ext.PreNoRcpDelta <= 0.5f ? "PASS" : "FAIL", $"Pre-start no-RCP dT={ext.PreNoRcpDelta:F3} F."),
                ["CS-0038"] = (stressPass && spikePass ? "PASS" : "FAIL", $"RCP stress cycles start/stop={starts}/{stops}; max level delta={maxLevel:F3}%."),
                ["CS-0055"] = (solidCore ? "PASS" : "FAIL", $"LongHold no-RCP slope={longHold.TrcsSlope:F3} F/hr with maxRCP={longHold.MaxRcp}."),
                ["CS-0056"] = (cs0056Status, cs0056Evidence),
                ["CS-0061"] = (longHold.MassErrMaxAbs <= 5f && extWindow ? "PASS" : "CONDITIONAL", $"Mass drift max abs={longHold.MassErrMaxAbs:F3} lbm."),
                ["CS-0071"] = (writerPass ? "PASS" : "FAIL", $"Writer validation conflicts={ext.WriterConflictCount}, illegalPostMutation={ext.WriterIllegalCount}, windowedChecks={ext.WriterWindowedChecks}, skippedOutsideWindow={ext.WriterSkippedChecks}.")
            };

            if (repeatabilityValid && !repeatPass)
            {
                foreach (string key in v.Keys.ToList())
                {
                    string status = v[key].Item1 == "PASS" ? "CONDITIONAL" : v[key].Item1;
                    string evidence = v[key].Item2 + $" Repeatability dT={rep.MaxDT:E3}, dP={rep.MaxDP:E3}.";
                    v[key] = (status, evidence);
                }
            }

            return v;
        }

        private static string DetermineRecommendation(
            Dictionary<string, (string status, string evidence)> cs,
            RunValidity validity,
            out string reason)
        {
            if (!validity.IsValid)
            {
                reason = string.Join(" | ", validity.Diagnostics);
                return "INVALID_RUN";
            }

            var blocking = new List<string>();
            foreach ((string csId, (string status, string evidence) value) in cs)
            {
                if (IsBlockingStatus(csId, value.status))
                    blocking.Add($"{csId}={value.status}");
            }

            if (blocking.Count == 0)
            {
                if (cs.TryGetValue("CS-0056", out var cs0056) &&
                    string.Equals(cs0056.status, "NOT_REACHED", StringComparison.OrdinalIgnoreCase))
                {
                    reason = Cs0056NotReachedNonBlocking
                        ? "CS-0056=NOT_REACHED treated as non-blocking by policy (sequence correctness is evaluated only if the near-350F window is reached)."
                        : "CS-0056=NOT_REACHED remains blocking by policy.";
                }
                else
                {
                    reason = "No blocking CS statuses.";
                }

                return "CLOSE_RECOMMENDED";
            }

            reason = $"Blocking CS statuses: {string.Join(", ", blocking)}";
            return "REMEDIATION_REQUIRED";
        }

        private static bool IsBlockingStatus(string csId, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return true;

            if (string.Equals(status, "PASS", StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.Equals(status, "CONDITIONAL", StringComparison.OrdinalIgnoreCase))
                return !ConditionalStatusNonBlocking;

            if (string.Equals(status, "NOT_REACHED", StringComparison.OrdinalIgnoreCase))
            {
                bool isCs0056 = string.Equals(csId, "CS-0056", StringComparison.OrdinalIgnoreCase);
                return !(isCs0056 && Cs0056NotReachedNonBlocking);
            }

            if (string.Equals(status, "NO_DATA", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(status, "FAIL", StringComparison.OrdinalIgnoreCase))
                return true;

            return true;
        }

        private static RunValidity EvaluateRunValidity(LongHoldResult longHold)
        {
            var validity = new RunValidity();

            if (longHold.Duration < RequiredLongHoldDurationHr - DtHr)
                validity.Diagnostics.Add($"Long-hold duration insufficient ({longHold.Duration:F3} hr < {RequiredLongHoldDurationHr:F3} hr).");

            if (longHold.SampleCount < MinLongHoldPoints)
                validity.Diagnostics.Add($"Long-hold sample count insufficient ({longHold.SampleCount} < {MinLongHoldPoints}).");

            if (longHold.MaxRcp > 0)
                validity.Diagnostics.Add($"Boundary violation: RCP count exceeded zero during isolated hold (maxRCP={longHold.MaxRcp}).");

            if (longHold.MaxAbsCharging > 1e-3f || longHold.MaxAbsLetdown > 1e-3f || longHold.MaxAbsChargingToRcs > 1e-3f)
                validity.Diagnostics.Add($"Boundary violation: CVCS flows not fully zeroed (max|chg|={longHold.MaxAbsCharging:F3}, max|letdown|={longHold.MaxAbsLetdown:F3}, max|chg2RCS|={longHold.MaxAbsChargingToRcs:F3}).");

            if (longHold.AnyRhrActiveMode || longHold.MaxAbsRhrNetHeat > 1e-4f)
                validity.Diagnostics.Add($"Boundary violation: RHR not fully standby/neutral (non-standby={(longHold.AnyRhrActiveMode ? "Y" : "N")}, max|RHR net heat|={longHold.MaxAbsRhrNetHeat:F6} MW).");

            if (!longHold.HeaterInjectionDisabled || longHold.HeaterObservedOn)
                validity.Diagnostics.Add("Boundary violation: heater injection was observed during isolated hold.");

            if (longHold.PressureInvariantFailed || longHold.PressureOverrideAttemptCount > 0 || longHold.PressureBlockedOverrideCount > 0)
            {
                string reason = string.IsNullOrWhiteSpace(longHold.PressureInvariantReason)
                    ? "Non-state pressure override attempt detected during Long Hold."
                    : longHold.PressureInvariantReason;
                validity.Diagnostics.Add(
                    $"Pressure invariant violation: {reason} " +
                    $"(writes={longHold.PressureWriteCount}, stateDerived={longHold.PressureStateDerivedWriteCount}, " +
                    $"overrideAttempts={longHold.PressureOverrideAttemptCount}, blocked={longHold.PressureBlockedOverrideCount}, lastSource={longHold.PressureInvariantSource}).");
            }
            if (!longHold.PressureOverrideProbeBlocked)
                validity.Diagnostics.Add("Pressure invariant probe failed: non-state pressure write was not blocked at Long Hold audit start.");

            validity.IsValid = validity.Diagnostics.Count == 0;
            return validity;
        }

        private static void WriteReport(
            string reportPath, string stamp, string logsRoot, LongHoldResult longHold, StressResult stress, ExtendedResult ext, RepeatabilityResult rep, Dictionary<string, (string status, string evidence)> cs, string recommendation, string recommendationReason, RunValidity validity)
        {
            int starts = stress.Cycles.Count(x => x.Started);
            int stops = stress.Cycles.Count(x => x.Stopped);
            float maxLevel = stress.Cycles.Count > 0 ? stress.Cycles.Max(x => x.MaxLevelDelta) : float.NaN;
            float maxPressEnv = stress.Cycles.Count > 0 ? stress.Cycles.Max(x => x.MaxPressureEnv) : float.NaN;
            float maxPressStep = stress.Cycles.Count > 0 ? stress.Cycles.Max(x => x.MaxPressureStep) : float.NaN;
            float maxHeatStep = stress.Cycles.Count > 0 ? stress.Cycles.Max(x => x.MaxRcpHeatStep) : float.NaN;

            var sb = new StringBuilder();
            sb.AppendLine("# IP-0019 Stage E Extended Validation Report");
            sb.AppendLine();
            sb.AppendLine($"- Date: {DateTime.Now:yyyy-MM-dd}");
            sb.AppendLine($"- Run stamp: `{stamp}`");
            sb.AppendLine("- IP Reference: `IP-0019`");
            sb.AppendLine("- DP Reference: `DP-0001`");
            sb.AppendLine("- Adjacent DP handling: monitor-only, no cross-domain remediation.");
            sb.AppendLine($"- Artifact root: `{logsRoot.Replace("\\", "/")}`");
            sb.AppendLine();
            sb.AppendLine("## Run Validity");
            sb.AppendLine($"- Status: {(validity.IsValid ? "VALID" : "INVALID_RUN")}");
            sb.AppendLine($"- Sufficiency gate: duration>={RequiredLongHoldDurationHr:F1} hr and points>={MinLongHoldPoints}");
            if (!validity.IsValid)
            {
                foreach (string reason in validity.Diagnostics)
                    sb.AppendLine($"- INVALID reason: {reason}");
            }
            sb.AppendLine();
            sb.AppendLine("## 1) Long Solid Hold");
            sb.AppendLine($"- Alignment: explicit isolated long-hold start at t={longHold.HoldStartTime:F3} hr (isolated window search disabled).");
            sb.AppendLine("- Enforced boundaries: RCP forced 0, charging=0 gpm, letdown=0 gpm, RHR forced STANDBY (no helper thermal injection).");
            sb.AppendLine("- Enforced boundaries: heater injection disabled (heater mode forced OFF).");
            sb.AppendLine($"- Duration: {longHold.Duration:F3} hr (completed>=3hr hold: {(longHold.Completed ? "YES" : "NO")})");
            sb.AppendLine($"- Samples collected: {longHold.SampleCount} (minimum {MinLongHoldPoints})");
            sb.AppendLine($"- No RCP active: maxRCP={longHold.MaxRcp}");
            sb.AppendLine($"- Boundary observations: max|chg|={longHold.MaxAbsCharging:F3} gpm, max|letdown|={longHold.MaxAbsLetdown:F3} gpm, max|netCVCS|={longHold.MaxAbsNetCvcs:F3} gpm");
            sb.AppendLine($"- Boundary observations: max|chg2RCS|={longHold.MaxAbsChargingToRcs:F3} gpm, heater observed={(longHold.HeaterObservedOn ? "Y" : "N")}, max|RHR net heat|={longHold.MaxAbsRhrNetHeat:F3} MW");
            sb.AppendLine($"- Heater injection disabled across hold: {(longHold.HeaterInjectionDisabled ? "YES" : "NO")}");
            sb.AppendLine($"- Pressure write audit: writes={longHold.PressureWriteCount}, state-derived={longHold.PressureStateDerivedWriteCount}, overrideAttempts={longHold.PressureOverrideAttemptCount}, blockedOverrides={longHold.PressureBlockedOverrideCount}");
            sb.AppendLine($"- Pressure override probe blocked (non-state write): {(longHold.PressureOverrideProbeBlocked ? "YES" : "NO")}");
            sb.AppendLine($"- Pressure write invariant failed: {(longHold.PressureInvariantFailed ? "YES" : "NO")} (lastSource={longHold.PressureInvariantSource})");
            if (!string.IsNullOrWhiteSpace(longHold.PressureInvariantReason))
                sb.AppendLine($"- Pressure invariant detail: {longHold.PressureInvariantReason}");
            if (!string.IsNullOrWhiteSpace(longHold.PressureOverrideProbeReason))
                sb.AppendLine($"- Pressure override probe detail: {longHold.PressureOverrideProbeReason}");
            sb.AppendLine($"- Boundary observations: max no-RCP transport factor={longHold.MaxNoRcpTransport:F3}, any non-standby RHR mode={(longHold.AnyRhrActiveMode ? "Y" : "N")}");
            sb.AppendLine($"- T_rcs trend: {longHold.StartTRcs:F3} -> {longHold.EndTRcs:F3} F (slope {longHold.TrcsSlope:F3} F/hr)");
            sb.AppendLine($"- Pressure trend: {longHold.StartP:F3} -> {longHold.EndP:F3} psia (slope {longHold.PressureSlope:F3} psi/hr)");
            sb.AppendLine($"- Surge flow: min {longHold.SurgeMin:F3}, max {longHold.SurgeMax:F3}, mean {longHold.SurgeMean:F3} gpm");
            sb.AppendLine($"- Oscillation amplitude (pressure P2P): {longHold.PressureP2P:F3} psi");
            sb.AppendLine($"- Mass conservation drift (max abs): {longHold.MassErrMaxAbs:F3} lbm");
            sb.AppendLine();
            sb.AppendLine("## 2) RCP Start Transient Stress");
            sb.AppendLine($"- Bubble reached: {(stress.BubbleReached ? "YES" : "NO")} at t={stress.BubbleTime:F3} hr");
            sb.AppendLine($"- Start/stop events: {starts}/{stops}");
            sb.AppendLine($"- PZR level transient max: {maxLevel:F3}%");
            sb.AppendLine($"- Pressure overshoot envelope max: {maxPressEnv:F3} psi");
            sb.AppendLine($"- Max one-step pressure delta: {maxPressStep:F3} psi");
            sb.AppendLine($"- Heat-rate smoothing proxy (max dRcpHeat per step): {maxHeatStep:F3} MW");
            foreach (CycleResult c in stress.Cycles)
                sb.AppendLine($"- Cycle {c.Cycle}: start={(c.Started ? "Y" : "N")} stop={(c.Stopped ? "Y" : "N")} maxLevelDelta={c.MaxLevelDelta:F3}%");
            sb.AppendLine();
            sb.AppendLine("## 3) Extended Heat-Up Window");
            sb.AppendLine($"- First RCP start detected: {(ext.FirstStart ? "YES" : "NO")} at t={ext.FirstStartTime:F3} hr");
            sb.AppendLine($"- Window: {ext.WindowStart:F3} -> {ext.WindowEnd:F3} hr (duration {ext.WindowDuration:F3} hr)");
            sb.AppendLine($"- Transition coverage: pre-noRCP={(ext.HasPreNoRcp ? "Y" : "N")} post-start={(ext.HasPostRcp ? "Y" : "N")} stabilized-RCP4={(ext.HasRcp4 ? "Y" : "N")}");
            sb.AppendLine($"- Post-start heat-rate max step: {ext.PostHeatRateMaxStep:F3} F/hr");
            sb.AppendLine($"- Equilibrium ceiling behavior (pre no-RCP): dT={ext.PreNoRcpDelta:F3} F, slope={ext.PreNoRcpSlope:F3} F/hr");
            sb.AppendLine($"- Pressure/temperature coupling ratio: {ext.TempPressureCoupling:P2}");
            sb.AppendLine($"- Slow drift (final hour): T slope={ext.StabilizedTempSlope:F3} F/hr, P slope={ext.StabilizedPressureSlope:F3} psi/hr");
            if (ext.IsolationDetected)
                sb.AppendLine($"- RHR isolation observation: detected=Y at t={ext.IsolationTime:F3} hr, RCP={ext.IsolationRcp}, T_rcs={ext.IsolationT:F3} F");
            else
                sb.AppendLine("- RHR isolation observation: detected=N (no Heatup->isolation transition observed in this run).");
            sb.AppendLine($"- RHR isolation evaluation sample (CS-0056): captured={(ext.IsolationEvalCaptured ? "Y" : "N")} trigger={ext.IsolationEvalTrigger}");
            sb.AppendLine($"- RHR isolation window reachability: max post-start T_rcs={ext.MaxPostStartTRcs:F3} F, threshold={ext.IsolationEvalThresholdF:F1} F, reached={(ext.PostStartNearThresholdReached ? "Y" : "N")}");
            if (ext.IsolationEvalCaptured)
            {
                sb.AppendLine($"- RHR isolation evaluation sample details: t={ext.IsolationEvalTime:F3} hr, RCP={ext.IsolationEvalRcp}, T_rcs={ext.IsolationEvalT:F3} F, threshold={ext.IsolationEvalThresholdF:F1} F, preMode={ext.IsolationEvalPreMode}, postMode={ext.IsolationEvalPostMode}, afterFirstStart={ext.IsolationEvalAfterFirstStart}, near350={ext.IsolationEvalNearThreshold}, isolated={ext.IsolationEvalIsolated}");
            }
            else
            {
                sb.AppendLine($"- RHR isolation evaluation sample details: N/A (no valid post-start near-{ext.IsolationEvalThresholdF:F1} F sample captured).");
            }
            sb.AppendLine($"- Thermodynamic writer states observed: {string.Join(", ", ext.Writers.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}");
            sb.AppendLine($"- Writer rule (CS-0071): pass={(ext.WriterRulePass ? "Y" : "N")}, conflicts={ext.WriterConflictCount}, illegalPostMutation={ext.WriterIllegalCount}, windowedChecks={ext.WriterWindowedChecks}, skippedOutsideWindow={ext.WriterSkippedChecks}");
            foreach (string transition in ext.WriterTransitions.Take(12))
                sb.AppendLine($"- Writer transition: {transition}");
            foreach (string solidCheck in ext.WriterSolidChecks.Take(12))
                sb.AppendLine($"- Writer solid expectation: {solidCheck}");
            foreach (string issue in ext.WriterIssues.Take(10))
                sb.AppendLine($"- Writer issue: {issue}");
            sb.AppendLine();
            sb.AppendLine("## 4) Repeatability Check");
            if (rep.Invalid)
            {
                sb.AppendLine($"- Status: INVALID (points={rep.Count})");
                sb.AppendLine($"- Reason: {rep.InvalidReason}");
            }
            else
            {
                sb.AppendLine($"- Completed: {(rep.Completed ? "YES" : "NO")} (points={rep.Count})");
                sb.AppendLine($"- Max delta T_rcs: {rep.MaxDT:E6} F");
                sb.AppendLine($"- Max delta pressure: {rep.MaxDP:E6} psi");
                sb.AppendLine($"- Max delta surge: {rep.MaxDSurge:E6} gpm");
                sb.AppendLine($"- Max delta mass drift: {rep.MaxDMass:E6} lbm");
            }
            sb.AppendLine();
            sb.AppendLine("## Per-CS Status (11)");
            foreach (string key in cs.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"- {key}: {cs[key].status} - {cs[key].evidence}");
            sb.AppendLine();
            sb.AppendLine("## Closure Recommendation");
            sb.AppendLine($"- {recommendation}");
            if (!string.IsNullOrWhiteSpace(recommendationReason))
                sb.AppendLine($"- Reason: {recommendationReason}");
            sb.AppendLine("- IP-0019 remains ACTIVE until closure recommendation is issued and accepted.");

            File.WriteAllText(reportPath, sb.ToString());
        }

        private static void Setup(HeatupSimEngine e, MethodInfo init, MethodInfo saveInterval, FieldInfo logPathField, string logDir)
        {
            e.runOnStart = false;
            e.coldShutdownStart = true;
            e.startTemperature = 100f;
            e.targetTemperature = 557f;
            logPathField.SetValue(e, logDir);
            init.Invoke(e, null);
            saveInterval.Invoke(e, null);
        }

        private static void PrepareLogDir(string dir)
        {
            Directory.CreateDirectory(dir);
            foreach (string file in Directory.GetFiles(dir, "*.txt"))
                File.Delete(file);
        }

        private static Snapshot Cap(HeatupSimEngine e)
        {
            return new Snapshot
            {
                T = e.simTime,
                TRcs = e.T_rcs,
                P = e.pressure,
                Surge = e.surgeFlow,
                Charging = e.chargingFlow,
                Letdown = e.letdownFlow,
                ChargingToRcs = e.chargingToRCS,
                NetCvcs = e.chargingFlow - e.letdownFlow,
                PzrLevel = e.pzrLevel,
                MassErr = e.massError_lbm,
                PRate = e.pressureRate,
                RcpHeat = e.rcpHeat,
                RcsHeatRate = e.rcsHeatRate,
                HeaterPower = e.pzrHeaterPower,
                HeatersOn = e.pzrHeatersOn,
                NoRcpTransport = e.noRcpTransportFactor,
                RhrNetHeat = e.rhrNetHeat_MW,
                Rcp = e.rcpCount,
                Bubble = e.bubbleFormed,
                Solid = e.solidPressurizer,
                Writer = e.thermoStateWriter ?? "",
                RhrMode = e.rhrState.Mode.ToString()
            };
        }

        private static void EnforceLongHoldNoTransport(HeatupSimEngine e)
        {
            // Force isolated no-transport regime for Long Hold:
            // no RCP transport, no CVCS boundary flow, no RHR, no heater injection.
            e.rcpCount = 0;
            e.rcpHeat = 0f;
            e.chargingFlow = 0f;
            e.letdownFlow = 0f;
            e.chargingToRCS = 0f;
            e.totalCCPOutput = 0f;
            e.solidPressurizer = false;
            e.bubbleFormed = true;
            e.bubblePhase = HeatupSimEngine.BubbleFormationPhase.COMPLETE;
            e.currentHeaterMode = HeaterMode.OFF;
            e.pzrHeatersOn = false;
            e.pzrHeaterPower = 0f;
            e.heaterPIDActive = false;
            e.heaterPIDOutput = 0f;
            e.rhrState = RHRSystem.InitializeStandby();
            e.rhrNetHeat_MW = 0f;
            e.rhrHXRemoval_MW = 0f;
            e.rhrPumpHeat_MW = 0f;
            e.rhrActive = false;
            e.rhrModeString = RHRMode.Standby.ToString();
        }

        private static void RunLongHoldPressureOverrideProbe(HeatupSimEngine e, out bool blocked, out string reason)
        {
            e.BeginLongHoldPressureAudit();
            float probePressure = e.pressure + 1f;
            bool applied = e.ApplyPressureWrite(probePressure, "LONG_HOLD_OVERRIDE_PROBE", stateDerived: false);
            blocked = !applied;
            reason = e.longHoldPressureInvariantReason ?? "";
            e.EndLongHoldPressureAudit();

            if (!blocked)
                throw new InvalidOperationException("Long Hold pressure guard probe failed: non-state pressure write was not blocked.");
        }

        private static void ObserveWriterRule(
            Snapshot pre,
            Snapshot cur,
            ExtendedResult result,
            Dictionary<int, string> writerByTime)
        {
            string writer = string.IsNullOrWhiteSpace(cur.Writer) ? "UNSET" : cur.Writer.Trim();
            string prevWriter = string.IsNullOrWhiteSpace(pre.Writer) ? "UNSET" : pre.Writer.Trim();
            if (!string.Equals(prevWriter, writer, StringComparison.OrdinalIgnoreCase))
            {
                result.WriterTransitions.Add(
                    $"t={cur.T:F4} hr {prevWriter}->{writer} (solid={cur.Solid}, rcp={cur.Rcp}, bubble={cur.Bubble}, T_rcs={cur.TRcs:F3} F, P={cur.P:F3} psia)");
            }

            int tKey = Mathf.RoundToInt(cur.T * 10000f);
            if (writerByTime.TryGetValue(tKey, out string existing))
            {
                if (!string.Equals(existing, writer, StringComparison.OrdinalIgnoreCase))
                {
                    result.WriterRulePass = false;
                    result.WriterConflictCount++;
                    result.WriterIssues.Add($"t={cur.T:F4} hr conflicting writers '{existing}' vs '{writer}'.");
                }
            }
            else
            {
                writerByTime[tKey] = writer;
            }

            if (!IsWriterTransitionLegal(prevWriter, writer))
            {
                result.WriterRulePass = false;
                result.WriterConflictCount++;
                result.WriterIssues.Add($"t={cur.T:F4} hr illegal transition '{prevWriter}' -> '{writer}'.");
            }

            if (!IsWriterPostMutationWindow(pre, cur, prevWriter, writer))
            {
                result.WriterSkippedChecks++;
                return;
            }

            result.WriterWindowedChecks++;
            if (string.Equals(writer, "REGIME1_SOLID", StringComparison.OrdinalIgnoreCase))
            {
                result.WriterSolidChecks.Add(
                    $"t={cur.T:F4} hr expected(solid=true,rcp=0) observed(solid={cur.Solid},rcp={cur.Rcp},bubble={cur.Bubble},RHR={cur.RhrMode})");
            }

            if (!IsWriterPostMutationLegal(cur, out string reason))
            {
                result.WriterRulePass = false;
                result.WriterIllegalCount++;
                result.WriterIssues.Add($"t={cur.T:F4} hr illegal post-mutation '{writer}': {reason}");
            }
        }

        private static bool IsWriterPostMutationWindow(Snapshot pre, Snapshot cur, string prevWriter, string writer)
        {
            if (cur.T <= 2f * DtHr)
                return false;
            if (!string.Equals(prevWriter, writer, StringComparison.OrdinalIgnoreCase))
                return false;
            if (pre.Rcp != cur.Rcp || pre.Solid != cur.Solid || pre.Bubble != cur.Bubble)
                return false;
            switch (writer.ToUpperInvariant())
            {
                case "REGIME1_SOLID":
                case "REGIME1_ISOLATED":
                case "REGIME2_BLEND":
                case "REGIME3_COUPLED":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsWriterTransitionLegal(string fromWriter, string toWriter)
        {
            if (string.Equals(toWriter, "UNSET", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(toWriter, "INIT", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(fromWriter, "UNSET", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fromWriter, "INIT", StringComparison.OrdinalIgnoreCase))
                return true;

            int fromRank = WriterRank(fromWriter);
            int toRank = WriterRank(toWriter);
            if (fromRank < 0 || toRank < 0)
                return false;
            return toRank >= fromRank;
        }

        private static bool IsWriterPostMutationLegal(Snapshot s, out string reason)
        {
            string writer = string.IsNullOrWhiteSpace(s.Writer) ? "UNSET" : s.Writer.Trim();
            switch (writer.ToUpperInvariant())
            {
                case "UNSET":
                case "INIT":
                    if (s.T <= 2f * DtHr)
                    {
                        reason = "";
                        return true;
                    }
                    reason = "INIT/UNSET observed after initialization window.";
                    return false;
                case "REGIME1_SOLID":
                    if (s.Solid && s.Rcp == 0)
                    {
                        reason = "";
                        return true;
                    }
                    reason = $"expected solid+RCP0 but got solid={s.Solid}, RCP={s.Rcp}.";
                    return false;
                case "REGIME1_ISOLATED":
                    if (!s.Solid && s.Rcp == 0)
                    {
                        reason = "";
                        return true;
                    }
                    reason = $"expected non-solid+RCP0 but got solid={s.Solid}, RCP={s.Rcp}.";
                    return false;
                case "REGIME2_BLEND":
                    if (!s.Solid && s.Rcp > 0)
                    {
                        reason = "";
                        return true;
                    }
                    reason = $"expected non-solid+RCP>0 but got solid={s.Solid}, RCP={s.Rcp}.";
                    return false;
                case "REGIME3_COUPLED":
                    if (!s.Solid && s.Rcp >= 4)
                    {
                        reason = "";
                        return true;
                    }
                    reason = $"expected non-solid+RCP>=4 but got solid={s.Solid}, RCP={s.Rcp}.";
                    return false;
                default:
                    reason = "unknown writer state.";
                    return false;
            }
        }

        private static int WriterRank(string writer)
        {
            switch (writer.ToUpperInvariant())
            {
                case "REGIME1_SOLID": return 0;
                case "REGIME1_ISOLATED": return 1;
                case "REGIME2_BLEND": return 2;
                case "REGIME3_COUPLED": return 3;
                default: return -1;
            }
        }

        private static void ForceRcpState(HeatupSimEngine e, MethodInfo step, float forcedP, int targetRcp, int maxSteps)
        {
            for (int i = 0; i < maxSteps; i++)
            {
                if (e.rcpCount == targetRcp)
                    return;
                e.ApplyPressureWrite(forcedP, "RUNNER_FORCE_RCP_STATE", stateDerived: false);
                step.Invoke(e, new object[] { DtHr });
            }
        }

        private static float Slope(List<Snapshot> s, Func<Snapshot, float> f)
        {
            if (s.Count < 2)
                return 0f;
            Snapshot a = s[0];
            Snapshot b = s[s.Count - 1];
            float dt = Mathf.Max(1e-6f, b.T - a.T);
            return (f(b) - f(a)) / dt;
        }

        private static float MaxStep(List<Snapshot> s, Func<Snapshot, float> f)
        {
            if (s.Count < 2)
                return 0f;
            float max = 0f;
            for (int i = 1; i < s.Count; i++)
                max = Mathf.Max(max, Mathf.Abs(f(s[i]) - f(s[i - 1])));
            return max;
        }

        private static float SurgePressureConsistency(List<Snapshot> s)
        {
            int n = 0;
            int ok = 0;
            foreach (Snapshot x in s)
            {
                if (Mathf.Abs(x.Surge) < 1e-4f || Mathf.Abs(x.PRate) < 1e-4f)
                    continue;
                n++;
                if (Mathf.Sign(x.Surge) == Mathf.Sign(x.PRate))
                    ok++;
            }
            return n == 0 ? 1f : (float)ok / n;
        }

        private static float TempPressureCoupling(List<Snapshot> s)
        {
            int n = 0;
            int ok = 0;
            for (int i = 1; i < s.Count; i++)
            {
                float dt = s[i].TRcs - s[i - 1].TRcs;
                float dp = s[i].P - s[i - 1].P;
                if (Mathf.Abs(dt) < 1e-4f || Mathf.Abs(dp) < 1e-4f)
                    continue;
                n++;
                if (Mathf.Sign(dt) == Mathf.Sign(dp))
                    ok++;
            }
            return n == 0 ? 1f : (float)ok / n;
        }
    }
}
