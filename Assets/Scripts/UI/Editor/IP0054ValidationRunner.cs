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

using Critical.Physics;
using Critical.Validation;

namespace Critical.Validation
{
    public static class IP0054ValidationRunner
    {
        private const float DtHr = 1f / 360f;         // 10 s
        private const float IntervalLogHr = 0.25f;    // 15 min
        private const float NoFlowDurationHr = 2f;    // zero-forced-flow window
        private const float StartupMaxHr = 15f;       // enough to observe first RCP
        private const float PostRcpWindowHr = 0.50f;  // +30 min
        private const int MaxSteps = 800000;

        // Gate tolerances for CS-0122
        private const float NoFlowTransportTol = 0.001f;
        private const float NoFlowSlopeMax_Fhr = 0.50f;
        private const float NoFlowDeltaMax_F = 1.00f;
        private const float NoFlowMaxForcedFlow_gpm = 0.5f;
        private const float RcpCouplingMin = 0.95f;
        private const float RcpTempRiseMin_F = 0.10f;

        private sealed class Sample
        {
            public float TimeHr;
            public float TRcsF;
            public float TPzrF;
            public float PressurePsia;
            public int RcpCount;
            public float PzrHeaterMw;
            public float NoRcpTransport;
            public float RhrFlowGpm;
            public string RhrMode = string.Empty;
            public float RhrNetHeatMw;
            public float HeatupRate_Fhr;
            public bool SolidPressurizer;
            public bool BubbleFormed;
            public int RtccAssertionFailures;
            public int PbocPairingFailures;
        }

        private sealed class RunResult
        {
            public string Label = string.Empty;
            public string RunDir = string.Empty;
            public string CsvPath = string.Empty;
            public string SummaryPath = string.Empty;
            public string ReportPath = string.Empty;
            public readonly List<Sample> Samples = new List<Sample>();
        }

        private sealed class Metrics
        {
            public string Label = string.Empty;
            public int SampleCount;
            public int NoRcpSampleCount;
            public int HeaterActiveNoRcpSamples;
            public float StartTimeHr;
            public float EndTimeHr;
            public float StartTRcsF;
            public float EndTRcsF;
            public float DeltaTRcsF;
            public float SlopeTRcs_Fhr;
            public float MaxAbsNoRcpTransport;
            public float MaxAbsRhrFlow_gpm;
            public float FirstRcpTimeHr = -1f;
            public float FirstRcpTransport = 0f;
            public float FirstRcpTRcsF = 0f;
            public float PostRcpTimeHr = -1f;
            public float PostRcpTransport = 0f;
            public float PostRcpTRcsF = 0f;
            public float PostRcpHeatupRate_Fhr = 0f;
            public float PostRcpRiseF = 0f;
            public int FinalRtccAssertionFailures;
            public int FinalPbocPairingFailures;
            public bool PassNoFlowGate;
            public bool PassRcpGate;
            public bool Pass;
        }

        [MenuItem("Critical/Run IP-0054 Stage D Validation")]
        public static void RunStageDValidation()
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string runDir = Path.Combine(root, "HeatupLogs", $"IP-0054_StageD_{stamp}");
            Directory.CreateDirectory(runDir);

            RunResult noFlowRun = ExecuteRun(runDir, "NO_FLOW", forceNoFlow: true);
            RunResult startupRun = ExecuteRun(runDir, "STARTUP", forceNoFlow: false);

            Metrics noFlow = ComputeMetrics(noFlowRun);
            Metrics startup = ComputeMetrics(startupRun);
            EvaluateGates(noFlow, startup);

            string summaryPath = Path.Combine(runDir, "IP-0054_StageD_Summary.md");
            WriteStageSummary(summaryPath, noFlowRun, startupRun, noFlow, startup);

            Debug.Log($"[IP-0054] Stage D summary: {summaryPath}");

            if (!noFlow.Pass || !startup.Pass)
            {
                throw new Exception("IP-0054 Stage D validation failed. See IP-0054_StageD_Summary.md.");
            }
        }

        private static RunResult ExecuteRun(string suiteDir, string label, bool forceNoFlow)
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
                throw new MissingMethodException("IP0054 runner could not resolve HeatupSimEngine internals.");

            engine.runOnStart = false;
            engine.coldShutdownStart = true;
            engine.startTemperature = 100f;
            engine.targetTemperature = 557f;
            engine.enableAsyncLogWriter = false;
            engine.enableHighFrequencyPerfLogs = false;
            engine.enableWorkerThreadStepping = false;
            logPathField.SetValue(engine, runDir);

            init.Invoke(engine, null);
            if (forceNoFlow)
                ForceNoFlowState(engine);
            saveInterval.Invoke(engine, null);

            var result = new RunResult
            {
                Label = label,
                RunDir = runDir
            };
            result.Samples.Add(Capture(engine));

            float nextInterval = IntervalLogHr;
            float firstRcpTime = -1f;
            for (int i = 0; i < MaxSteps; i++)
            {
                step.Invoke(engine, new object[] { DtHr });
                result.Samples.Add(Capture(engine));

                if (firstRcpTime < 0f && engine.rcpCount > 0)
                    firstRcpTime = engine.simTime;

                if (engine.simTime >= nextInterval)
                {
                    saveInterval.Invoke(engine, null);
                    nextInterval += IntervalLogHr;
                }

                if (forceNoFlow && engine.simTime >= NoFlowDurationHr)
                    break;

                if (!forceNoFlow &&
                    ((firstRcpTime > 0f && engine.simTime >= firstRcpTime + PostRcpWindowHr) ||
                     engine.simTime >= StartupMaxHr))
                    break;
            }

            saveReport.Invoke(engine, null);

            result.CsvPath = Path.Combine(runDir, $"IP-0054_{label}_Telemetry.csv");
            result.SummaryPath = Path.Combine(runDir, $"IP-0054_{label}_Summary.md");
            result.ReportPath = Directory.GetFiles(runDir, "Heatup_Report_*.txt")
                .OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? string.Empty;

            WriteRunCsv(result.CsvPath, result.Samples);
            WriteRunSummary(result.SummaryPath, result, ComputeMetrics(result));
            return result;
        }

        private static void ForceNoFlowState(HeatupSimEngine engine)
        {
            RHRState state = engine.rhrState;
            state.Mode = RHRMode.Standby;
            state.PumpsRunning = false;
            state.PumpsOnline = 0;
            state.FlowRate_gpm = 0f;
            state.SuctionValvesOpen = false;
            state.HXBypassFraction = 1f;
            state.HeatRemoval_MW = 0f;
            state.PumpHeatInput_MW = 0f;
            state.NetHeatEffect_MW = 0f;
            engine.rhrState = state;
            engine.rhrNetHeat_MW = 0f;
        }

        private static Sample Capture(HeatupSimEngine engine)
        {
            return new Sample
            {
                TimeHr = engine.simTime,
                TRcsF = engine.T_rcs,
                TPzrF = engine.T_pzr,
                PressurePsia = engine.pressure,
                RcpCount = engine.rcpCount,
                PzrHeaterMw = engine.pzrHeaterPower,
                NoRcpTransport = engine.noRcpTransportFactor,
                RhrFlowGpm = engine.rhrState.FlowRate_gpm,
                RhrMode = engine.rhrState.Mode.ToString(),
                RhrNetHeatMw = engine.rhrNetHeat_MW,
                HeatupRate_Fhr = engine.heatupRate,
                SolidPressurizer = engine.solidPressurizer,
                BubbleFormed = engine.bubbleFormed,
                RtccAssertionFailures = engine.rtccAssertionFailureCount,
                PbocPairingFailures = engine.pbocPairingAssertionFailures
            };
        }

        private static Metrics ComputeMetrics(RunResult run)
        {
            var m = new Metrics
            {
                Label = run.Label,
                SampleCount = run.Samples.Count
            };

            if (run.Samples.Count == 0)
                return m;

            Sample first = run.Samples[0];
            Sample last = run.Samples[run.Samples.Count - 1];
            m.StartTimeHr = first.TimeHr;
            m.EndTimeHr = last.TimeHr;
            m.StartTRcsF = first.TRcsF;
            m.EndTRcsF = last.TRcsF;
            m.DeltaTRcsF = m.EndTRcsF - m.StartTRcsF;
            m.FinalRtccAssertionFailures = last.RtccAssertionFailures;
            m.FinalPbocPairingFailures = last.PbocPairingFailures;

            List<Sample> noRcp = run.Samples.Where(s => s.RcpCount == 0).ToList();
            List<Sample> noRcpHeater = noRcp.Where(s => s.PzrHeaterMw > 0.001f).ToList();
            List<Sample> envelope = noRcpHeater.Count >= 2 ? noRcpHeater : noRcp;

            m.NoRcpSampleCount = noRcp.Count;
            m.HeaterActiveNoRcpSamples = noRcpHeater.Count;
            m.MaxAbsNoRcpTransport = envelope.Count > 0
                ? envelope.Max(s => Mathf.Abs(s.NoRcpTransport))
                : 0f;
            m.MaxAbsRhrFlow_gpm = envelope.Count > 0
                ? envelope.Max(s => Mathf.Abs(s.RhrFlowGpm))
                : 0f;
            m.SlopeTRcs_Fhr = envelope.Count >= 2
                ? ComputeSlope(envelope)
                : 0f;

            Sample firstRcp = run.Samples.FirstOrDefault(s => s.RcpCount > 0);
            if (firstRcp != null)
            {
                m.FirstRcpTimeHr = firstRcp.TimeHr;
                m.FirstRcpTransport = firstRcp.NoRcpTransport;
                m.FirstRcpTRcsF = firstRcp.TRcsF;

                Sample postRcp = run.Samples.FirstOrDefault(s => s.TimeHr >= firstRcp.TimeHr + PostRcpWindowHr);
                if (postRcp != null)
                {
                    m.PostRcpTimeHr = postRcp.TimeHr;
                    m.PostRcpTransport = postRcp.NoRcpTransport;
                    m.PostRcpTRcsF = postRcp.TRcsF;
                    m.PostRcpHeatupRate_Fhr = postRcp.HeatupRate_Fhr;
                    m.PostRcpRiseF = postRcp.TRcsF - firstRcp.TRcsF;
                }
            }

            return m;
        }

        private static void EvaluateGates(Metrics noFlow, Metrics startup)
        {
            noFlow.PassNoFlowGate =
                noFlow.MaxAbsRhrFlow_gpm <= NoFlowMaxForcedFlow_gpm &&
                noFlow.MaxAbsNoRcpTransport <= NoFlowTransportTol &&
                noFlow.SlopeTRcs_Fhr <= NoFlowSlopeMax_Fhr &&
                noFlow.DeltaTRcsF <= NoFlowDeltaMax_F;

            noFlow.Pass = noFlow.PassNoFlowGate &&
                          noFlow.FinalRtccAssertionFailures == 0 &&
                          noFlow.FinalPbocPairingFailures == 0;

            startup.PassRcpGate =
                startup.FirstRcpTimeHr >= 0f &&
                startup.PostRcpTimeHr >= 0f &&
                startup.FirstRcpTransport >= RcpCouplingMin &&
                startup.PostRcpTransport >= RcpCouplingMin &&
                startup.PostRcpRiseF >= RcpTempRiseMin_F &&
                startup.PostRcpHeatupRate_Fhr > 0f;

            startup.Pass = startup.PassRcpGate &&
                           startup.FinalRtccAssertionFailures == 0 &&
                           startup.FinalPbocPairingFailures == 0;
        }

        private static float ComputeSlope(List<Sample> samples)
        {
            int n = samples.Count;
            if (n < 2)
                return 0f;

            double sumX = 0d;
            double sumY = 0d;
            double sumXY = 0d;
            double sumXX = 0d;
            for (int i = 0; i < n; i++)
            {
                double x = samples[i].TimeHr;
                double y = samples[i].TRcsF;
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumXX += x * x;
            }

            double denom = n * sumXX - sumX * sumX;
            if (Math.Abs(denom) < 1e-9d)
                return 0f;
            return (float)((n * sumXY - sumX * sumY) / denom);
        }

        private static void WriteRunCsv(string path, List<Sample> samples)
        {
            var sb = new StringBuilder(samples.Count * 120);
            sb.AppendLine("time_hr,t_rcs_f,t_pzr_f,pressure_psia,rcp_count,pzr_heater_mw,no_rcp_transport,rhr_flow_gpm,rhr_mode,rhr_net_heat_mw,heatup_rate_fhr,solid_pressurizer,bubble_formed,rtcc_assert_failures,pboc_pairing_failures");
            foreach (Sample s in samples)
            {
                sb.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:F6},{1:F4},{2:F4},{3:F4},{4},{5:F6},{6:F6},{7:F4},{8},{9:F6},{10:F4},{11},{12},{13},{14}",
                    s.TimeHr,
                    s.TRcsF,
                    s.TPzrF,
                    s.PressurePsia,
                    s.RcpCount,
                    s.PzrHeaterMw,
                    s.NoRcpTransport,
                    s.RhrFlowGpm,
                    Csv(s.RhrMode),
                    s.RhrNetHeatMw,
                    s.HeatupRate_Fhr,
                    ToBit(s.SolidPressurizer),
                    ToBit(s.BubbleFormed),
                    s.RtccAssertionFailures,
                    s.PbocPairingFailures));
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static void WriteRunSummary(string path, RunResult run, Metrics m)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# IP-0054 {run.Label} Run Summary");
            sb.AppendLine();
            sb.AppendLine($"- Run dir: `{run.RunDir}`");
            sb.AppendLine($"- Telemetry CSV: `{run.CsvPath}`");
            sb.AppendLine($"- Heatup report: `{run.ReportPath}`");
            sb.AppendLine($"- Samples: `{m.SampleCount}`");
            sb.AppendLine($"- Time window: `{m.StartTimeHr:F3} -> {m.EndTimeHr:F3} hr`");
            sb.AppendLine($"- T_rcs window: `{m.StartTRcsF:F3} -> {m.EndTRcsF:F3} F` (`delta={m.DeltaTRcsF:+0.000;-0.000;0.000} F`)");
            sb.AppendLine($"- No-RCP slope (heater-active set): `{m.SlopeTRcs_Fhr:+0.000;-0.000;0.000} F/hr`");
            sb.AppendLine($"- max|noRcpTransport|: `{m.MaxAbsNoRcpTransport:F6}`");
            sb.AppendLine($"- max|RHR flow|: `{m.MaxAbsRhrFlow_gpm:F4} gpm`");
            sb.AppendLine($"- RTCC assertion failures: `{m.FinalRtccAssertionFailures}`");
            sb.AppendLine($"- PBOC pairing failures: `{m.FinalPbocPairingFailures}`");
            if (m.FirstRcpTimeHr >= 0f)
            {
                sb.AppendLine($"- First RCP sample: `t={m.FirstRcpTimeHr:F3} hr`, `transport={m.FirstRcpTransport:F4}`, `T_rcs={m.FirstRcpTRcsF:F3} F`");
            }
            if (m.PostRcpTimeHr >= 0f)
            {
                sb.AppendLine($"- Post-RCP sample (+30 min): `t={m.PostRcpTimeHr:F3} hr`, `transport={m.PostRcpTransport:F4}`, `T_rcs={m.PostRcpTRcsF:F3} F`, `rise={m.PostRcpRiseF:+0.000;-0.000;0.000} F`");
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static void WriteStageSummary(
            string path,
            RunResult noFlowRun,
            RunResult startupRun,
            Metrics noFlow,
            Metrics startup)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0054 Stage D Validation Summary");
            sb.AppendLine();
            sb.AppendLine("## Artifacts");
            sb.AppendLine($"- No-flow run dir: `{noFlowRun.RunDir}`");
            sb.AppendLine($"- No-flow telemetry: `{noFlowRun.CsvPath}`");
            sb.AppendLine($"- No-flow summary: `{noFlowRun.SummaryPath}`");
            sb.AppendLine($"- Startup run dir: `{startupRun.RunDir}`");
            sb.AppendLine($"- Startup telemetry: `{startupRun.CsvPath}`");
            sb.AppendLine($"- Startup summary: `{startupRun.SummaryPath}`");
            sb.AppendLine();
            sb.AppendLine("## Gate A/B Evidence Snapshot");
            sb.AppendLine($"- NO_FLOW max|RHR flow|: `{noFlow.MaxAbsRhrFlow_gpm:F4} gpm` (tol `{NoFlowMaxForcedFlow_gpm:F1}`)");
            sb.AppendLine($"- NO_FLOW max|noRcpTransport|: `{noFlow.MaxAbsNoRcpTransport:F6}` (tol `{NoFlowTransportTol:F6}`)");
            sb.AppendLine($"- NO_FLOW T_rcs slope: `{noFlow.SlopeTRcs_Fhr:+0.000;-0.000;0.000} F/hr` (tol `<= {NoFlowSlopeMax_Fhr:F2}`)");
            sb.AppendLine($"- NO_FLOW T_rcs delta: `{noFlow.DeltaTRcsF:+0.000;-0.000;0.000} F` (tol `<= {NoFlowDeltaMax_F:F2}`)");
            sb.AppendLine();
            sb.AppendLine("## Gate C/D Evidence Snapshot");
            sb.AppendLine($"- STARTUP first RCP sample: `t={startup.FirstRcpTimeHr:F3} hr`, `transport={startup.FirstRcpTransport:F4}`");
            sb.AppendLine($"- STARTUP +30min sample: `t={startup.PostRcpTimeHr:F3} hr`, `transport={startup.PostRcpTransport:F4}`, `T_rcs rise={startup.PostRcpRiseF:+0.000;-0.000;0.000} F`, `heatupRate={startup.PostRcpHeatupRate_Fhr:+0.000;-0.000;0.000} F/hr`");
            sb.AppendLine($"- STARTUP RTCC assertion failures: `{startup.FinalRtccAssertionFailures}`");
            sb.AppendLine($"- STARTUP PBOC pairing failures: `{startup.FinalPbocPairingFailures}`");
            sb.AppendLine();
            sb.AppendLine("## Gate Results");
            sb.AppendLine($"- Gate C no-RCP envelope (`CS-0122`): {(noFlow.PassNoFlowGate ? "PASS" : "FAIL")}");
            sb.AppendLine($"- Gate C RCP-on coupling non-regression: {(startup.PassRcpGate ? "PASS" : "FAIL")}");
            sb.AppendLine($"- Gate D regression (RTCC/PBOC): {((noFlow.FinalRtccAssertionFailures == 0 && noFlow.FinalPbocPairingFailures == 0 && startup.FinalRtccAssertionFailures == 0 && startup.FinalPbocPairingFailures == 0) ? "PASS" : "FAIL")}");
            sb.AppendLine();
            sb.AppendLine("## Overall");
            sb.AppendLine($"- NO_FLOW run: {(noFlow.Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- STARTUP run: {(startup.Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- Stage D outcome: {((noFlow.Pass && startup.Pass) ? "PASS" : "FAIL")}");

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
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
