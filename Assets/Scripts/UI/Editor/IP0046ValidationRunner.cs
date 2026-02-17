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

using Critical.Validation;
namespace Critical.Validation
{
    public static class IP0046ValidationRunner
    {
        private const float DtHr = 1f / 360f;      // 10 seconds
        private const float IntervalLogHr = 0.25f; // 15 minutes
        private const float MaxSimHr = 20f;
        private const int MaxSteps = 800000;

        private sealed class Sample
        {
            public float TimeHr;
            public float TRcsF;
            public float PressurePsia;
            public int RcpCount;
            public float ChargingGpm;
            public float LetdownGpm;
            public float NetCvcsGpm;
            public float SgPressurePsia;
            public float SgTopNodeF;
            public float SgTsatF;
            public string SgBoundaryMode = string.Empty;
            public string SgPressureSource = string.Empty;
            public string SgStartupState = string.Empty;
            public bool SgDrainingActive;
            public bool SgDrainingComplete;
            public float SgMassDrainedLb;
            public float SgSecondaryMassLb;
            public bool SgBoilingActive;
            public bool SgNitrogenIsolated;
        }

        [MenuItem("Critical/Run IP-0046 Stage D Validation")]
        public static void RunStageDValidation()
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string runDir = Path.Combine(root, "HeatupLogs", $"IP-0046_StageD_{stamp}");
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
                throw new MissingMethodException("IP0046 runner could not resolve HeatupSimEngine internals.");

            engine.runOnStart = false;
            engine.coldShutdownStart = true;
            engine.startTemperature = 100f;
            engine.targetTemperature = 557f;
            engine.enableAsyncLogWriter = false;
            engine.enableHighFrequencyPerfLogs = false;
            engine.enableWorkerThreadStepping = false;
            logPathField.SetValue(engine, runDir);

            init.Invoke(engine, null);
            saveInterval.Invoke(engine, null);

            var samples = new List<Sample>(9000);
            samples.Add(Capture(engine));

            float nextInterval = IntervalLogHr;
            for (int i = 0; i < MaxSteps && engine.simTime < MaxSimHr; i++)
            {
                step.Invoke(engine, new object[] { DtHr });
                samples.Add(Capture(engine));

                if (engine.simTime >= nextInterval)
                {
                    saveInterval.Invoke(engine, null);
                    nextInterval += IntervalLogHr;
                }
            }

            saveReport.Invoke(engine, null);

            WriteSampleCsv(Path.Combine(runDir, "IP-0046_StageD_SGSampleTelemetry.csv"), samples);
            WriteSummary(Path.Combine(runDir, "IP-0046_StageD_Summary.md"), runDir, samples, engine);

            Debug.Log($"[IP-0046] Stage D validation artifacts: {runDir}");
        }

        private static Sample Capture(HeatupSimEngine e)
        {
            return new Sample
            {
                TimeHr = e.simTime,
                TRcsF = e.T_rcs,
                PressurePsia = e.pressure,
                RcpCount = e.rcpCount,
                ChargingGpm = e.chargingFlow,
                LetdownGpm = e.letdownFlow,
                NetCvcsGpm = e.chargingFlow - e.letdownFlow,
                SgPressurePsia = e.sgSecondaryPressure_psia,
                SgTopNodeF = e.sgTopNodeTemp,
                SgTsatF = e.sgSaturationTemp_F,
                SgBoundaryMode = e.sgBoundaryMode ?? string.Empty,
                SgPressureSource = e.sgPressureSourceBranch ?? string.Empty,
                SgStartupState = e.sgStartupBoundaryState ?? string.Empty,
                SgDrainingActive = e.sgDrainingActive,
                SgDrainingComplete = e.sgDrainingComplete,
                SgMassDrainedLb = e.sgTotalMassDrained_lb,
                SgSecondaryMassLb = e.sgSecondaryMass_lb,
                SgBoilingActive = e.sgBoilingActive,
                SgNitrogenIsolated = e.sgNitrogenIsolated
            };
        }

        private static void WriteSampleCsv(string csvPath, List<Sample> samples)
        {
            var sb = new StringBuilder(samples.Count * 180);
            sb.AppendLine(
                "time_hr,t_rcs_f,pressure_psia,rcp_count,charging_gpm,letdown_gpm,net_cvcs_gpm," +
                "sg_pressure_psia,sg_top_node_f,sg_tsat_f,sg_boundary_mode,sg_pressure_source,sg_startup_state," +
                "sg_draining_active,sg_draining_complete,sg_mass_drained_lb,sg_secondary_mass_lb," +
                "sg_boiling_active,sg_nitrogen_isolated");

            foreach (Sample s in samples)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "{0:F6},{1:F4},{2:F4},{3},{4:F4},{5:F4},{6:F4},{7:F4},{8:F4},{9:F4},{10},{11},{12},{13},{14},{15:F4},{16:F4},{17},{18}\n",
                    s.TimeHr,
                    s.TRcsF,
                    s.PressurePsia,
                    s.RcpCount,
                    s.ChargingGpm,
                    s.LetdownGpm,
                    s.NetCvcsGpm,
                    s.SgPressurePsia,
                    s.SgTopNodeF,
                    s.SgTsatF,
                    CsvSafe(s.SgBoundaryMode),
                    CsvSafe(s.SgPressureSource),
                    CsvSafe(s.SgStartupState),
                    s.SgDrainingActive ? 1 : 0,
                    s.SgDrainingComplete ? 1 : 0,
                    s.SgMassDrainedLb,
                    s.SgSecondaryMassLb,
                    s.SgBoilingActive ? 1 : 0,
                    s.SgNitrogenIsolated ? 1 : 0);
            }

            File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);
        }

        private static void WriteSummary(
            string summaryPath,
            string runDir,
            List<Sample> samples,
            HeatupSimEngine engine)
        {
            float maxTRcs = samples.Max(s => s.TRcsF);
            Sample threshold200 = samples.FirstOrDefault(s => s.TRcsF >= 200f);
            Sample drainStart = samples.FirstOrDefault(s => s.SgDrainingActive || s.SgMassDrainedLb > 0f);
            Sample rcpStart = samples.FirstOrDefault(s => s.RcpCount > 0);

            int openBoundarySamples = samples.Count(s => string.Equals(s.SgBoundaryMode, "OPEN", StringComparison.OrdinalIgnoreCase));
            int isolatedBoundarySamples = samples.Count(s => string.Equals(s.SgBoundaryMode, "ISOLATED", StringComparison.OrdinalIgnoreCase));

            float rcpStartPressure = rcpStart != null ? rcpStart.SgPressurePsia : 0f;
            Sample rcpPlus30 = rcpStart != null
                ? samples.FirstOrDefault(s => s.TimeHr >= rcpStart.TimeHr + 0.5f)
                : null;
            float rcpPlus30Pressure = rcpPlus30 != null ? rcpPlus30.SgPressurePsia : 0f;
            float rcpPressureDelta30 = (rcpStart != null && rcpPlus30 != null)
                ? (rcpPlus30Pressure - rcpStartPressure)
                : 0f;
            Sample firstInventoryDerived = samples.FirstOrDefault(s =>
                string.Equals(s.SgPressureSource, "inventory-derived", StringComparison.OrdinalIgnoreCase));
            Sample firstBoiling = samples.FirstOrDefault(s => s.SgBoilingActive);
            bool inventoryDerivedBeforeBoil = firstInventoryDerived != null &&
                (firstBoiling == null || firstInventoryDerived.TimeHr < firstBoiling.TimeHr);
            bool floorReversionBeforeBoil = false;
            if (inventoryDerivedBeforeBoil)
            {
                floorReversionBeforeBoil = samples.Any(s =>
                    s.TimeHr > firstInventoryDerived.TimeHr &&
                    (firstBoiling == null || s.TimeHr < firstBoiling.TimeHr) &&
                    string.Equals(s.SgPressureSource, "floor", StringComparison.OrdinalIgnoreCase));
            }
            bool cs0078Pass = rcpStart != null &&
                rcpPlus30 != null &&
                rcpPressureDelta30 > 0f &&
                inventoryDerivedBeforeBoil &&
                !floorReversionBeforeBoil;

            int drainEventCount = engine.eventLog.Count(e =>
                (e.Message ?? string.Empty).Contains("SG DRAINING STARTED", StringComparison.Ordinal));
            int startupTransitionEventCount = engine.eventLog.Count(e =>
                (e.Message ?? string.Empty).Contains("SG startup boundary transition:", StringComparison.Ordinal));

            var sb = new StringBuilder();
            sb.AppendLine("# IP-0046 Stage D Validation Summary");
            sb.AppendLine();
            sb.AppendLine($"- Run directory: `{runDir}`");
            sb.AppendLine($"- Sim end time: `{samples[samples.Count - 1].TimeHr:F3} hr`");
            sb.AppendLine($"- Max T_rcs: `{maxTRcs:F2} F`");
            sb.AppendLine($"- SG boundary samples OPEN/ISOLATED: `{openBoundarySamples}/{isolatedBoundarySamples}`");
            sb.AppendLine($"- SG drain event count: `{drainEventCount}`");
            sb.AppendLine($"- SG startup transition event count: `{startupTransitionEventCount}`");
            sb.AppendLine();
            sb.AppendLine("## Milestones");
            sb.AppendLine(threshold200 != null
                ? $"- First `T_rcs >= 200F`: `{threshold200.TimeHr:F3} hr`"
                : "- First `T_rcs >= 200F`: `NOT_REACHED`");
            sb.AppendLine(drainStart != null
                ? $"- First SG draining active/mass change: `{drainStart.TimeHr:F3} hr` (drained `{drainStart.SgMassDrainedLb:F1} lb`)"
                : "- First SG draining active/mass change: `NOT_REACHED`");
            sb.AppendLine(rcpStart != null
                ? $"- First non-zero RCP count: `{rcpStart.TimeHr:F3} hr`"
                : "- First non-zero RCP count: `NOT_REACHED`");
            sb.AppendLine();
            sb.AppendLine("## SG Pressure Around RCP Onset");
            sb.AppendLine(rcpStart != null
                ? $"- SG pressure at RCP onset: `{rcpStartPressure:F3} psia` (source `{rcpStart.SgPressureSource}`)"
                : "- SG pressure at RCP onset: `N/A`");
            sb.AppendLine(rcpPlus30 != null
                ? $"- SG pressure at RCP onset +30 min: `{rcpPlus30Pressure:F3} psia` (delta `{rcpPressureDelta30:F3} psia`)"
                : "- SG pressure at RCP onset +30 min: `N/A`");
            sb.AppendLine(firstInventoryDerived != null
                ? $"- First inventory-derived pressure source: `{firstInventoryDerived.TimeHr:F3} hr`"
                : "- First inventory-derived pressure source: `NOT_REACHED`");
            sb.AppendLine(firstBoiling != null
                ? $"- First boiling-active sample: `{firstBoiling.TimeHr:F3} hr`"
                : "- First boiling-active sample: `NOT_REACHED`");
            sb.AppendLine($"- Floor reversion after inventory-derived (pre-boil): `{(floorReversionBeforeBoil ? "YES" : "NO")}`");
            sb.AppendLine();
            sb.AppendLine("## CS Gate Hints");
            sb.AppendLine($"- CS-0082 (startup open boundary): {(isolatedBoundarySamples == 0 ? "PASS" : "FAIL")}.");
            sb.AppendLine($"- CS-0057 (draining trigger at ~200F): {(threshold200 != null && drainStart != null ? "PASS" : "INCOMPLETE")}.");
            sb.AppendLine($"- CS-0078 (pressure response from circulation onset): {(cs0078Pass ? "PASS" : "FAIL")}.");
            sb.AppendLine();
            sb.AppendLine("## Generated Files");
            sb.AppendLine($"- `IP-0046_StageD_SGSampleTelemetry.csv`");
            sb.AppendLine($"- `Heatup_Report_*.txt`");
            sb.AppendLine($"- `Heatup_Interval_*.txt`");

            File.WriteAllText(summaryPath, sb.ToString(), Encoding.UTF8);
        }

        private static string CsvSafe(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            if (value.Contains(',') || value.Contains('"'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
