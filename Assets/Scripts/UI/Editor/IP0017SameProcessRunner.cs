using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Process = System.Diagnostics.Process;

namespace Critical.Validation
{
    /// <summary>
    /// IP-0017 strict same-process runner for CS-0013 closure evidence.
    /// This is editor-only orchestration and does not modify solver behavior.
    /// </summary>
    public static class IP0017SameProcessRunner
    {
        [MenuItem("Critical/Run Stage E Twice Same Process (IP-0017 Strict)")]
        public static void RunStageETwiceSameProcessStrict()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string updatesIssuesDir = Path.Combine(projectRoot, "Updates", "Issues");
            string heatupLogsDir = Path.Combine(projectRoot, "HeatupLogs");
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string runADir = Path.Combine(heatupLogsDir, $"IP-0017_RunA_SameProcess_{stamp}");
            string runBDir = Path.Combine(heatupLogsDir, $"IP-0017_RunB_SameProcess_{stamp}");
            string manifestPath = Path.Combine(
                updatesIssuesDir,
                $"IP-0017_SameProcess_Execution_{stamp}.md");

            int processId = Process.GetCurrentProcess().Id;
            int appDomainId = AppDomain.CurrentDomain.Id;
            string[] cmdArgs = Environment.GetCommandLineArgs();

            Debug.Log($"[IP-0017][STRICT] Process start marker: pid={processId}, appDomain={appDomainId}, stamp={stamp}");
            Debug.Log($"[IP-0017][STRICT] CommandLine: {string.Join(" ", cmdArgs)}");

            string runAEvidence = ExecuteSingleRun("RunA", updatesIssuesDir, heatupLogsDir, runADir);
            string runBEvidence = ExecuteSingleRun("RunB", updatesIssuesDir, heatupLogsDir, runBDir);

            WriteManifest(
                manifestPath,
                stamp,
                processId,
                appDomainId,
                cmdArgs,
                runADir,
                runAEvidence,
                runBDir,
                runBEvidence);

            Debug.Log($"[IP-0017][STRICT] Same-process execution complete. Manifest: {manifestPath}");
        }

        private static string ExecuteSingleRun(
            string runLabel,
            string updatesIssuesDir,
            string heatupLogsDir,
            string runOutputDir)
        {
            Directory.CreateDirectory(runOutputDir);
            string[] before = Directory.GetFiles(updatesIssuesDir, "IP-0015_StageE_Rerun_*.md");

            int processId = Process.GetCurrentProcess().Id;
            int appDomainId = AppDomain.CurrentDomain.Id;
            Debug.Log($"[IP-0017][STRICT] {runLabel} start: pid={processId}, appDomain={appDomainId}");

            StageERunner.RunStageE();

            string[] after = Directory.GetFiles(updatesIssuesDir, "IP-0015_StageE_Rerun_*.md");
            string evidence = after
                .Except(before, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            foreach (string report in Directory.GetFiles(heatupLogsDir, "Heatup_Report_*.txt"))
            {
                File.Copy(report, Path.Combine(runOutputDir, Path.GetFileName(report)), true);
            }

            foreach (string interval in Directory.GetFiles(heatupLogsDir, "Heatup_Interval_*.txt"))
            {
                File.Copy(interval, Path.Combine(runOutputDir, Path.GetFileName(interval)), true);
            }

            if (!string.IsNullOrEmpty(evidence) && File.Exists(evidence))
            {
                File.Copy(evidence, Path.Combine(runOutputDir, Path.GetFileName(evidence)), true);
            }

            Debug.Log(
                $"[IP-0017][STRICT] {runLabel} end: reportCopies={Directory.GetFiles(runOutputDir, "Heatup_Report_*.txt").Length}, " +
                $"intervalCopies={Directory.GetFiles(runOutputDir, "Heatup_Interval_*.txt").Length}, evidence={Path.GetFileName(evidence ?? "<none>")}");

            return evidence ?? string.Empty;
        }

        private static void WriteManifest(
            string manifestPath,
            string stamp,
            int processId,
            int appDomainId,
            string[] cmdArgs,
            string runADir,
            string runAEvidence,
            string runBDir,
            string runBEvidence)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath) ?? ".");

            var sb = new StringBuilder();
            sb.AppendLine("# IP-0017 Strict Same-Process Execution Manifest");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Execution stamp: `{stamp}`");
            sb.AppendLine($"- Process ID: `{processId}`");
            sb.AppendLine($"- AppDomain ID: `{appDomainId}`");
            sb.AppendLine($"- Command line: `{string.Join(" ", cmdArgs)}`");
            sb.AppendLine();
            sb.AppendLine("## Run Outputs");
            sb.AppendLine($"- Run A bundle: `{runADir}`");
            sb.AppendLine($"- Run A evidence markdown: `{(string.IsNullOrEmpty(runAEvidence) ? "<not-detected>" : runAEvidence)}`");
            sb.AppendLine($"- Run B bundle: `{runBDir}`");
            sb.AppendLine($"- Run B evidence markdown: `{(string.IsNullOrEmpty(runBEvidence) ? "<not-detected>" : runBEvidence)}`");

            File.WriteAllText(manifestPath, sb.ToString());
        }
    }
}
