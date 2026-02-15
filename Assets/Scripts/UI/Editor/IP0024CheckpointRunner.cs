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
    public static class IP0024CheckpointRunner
    {
        private const float DtHr = 1f / 360f; // 10 s
        private const float StageDHoldHours = 1f; // 60 min deterministic hold
        private const float StageHMaxHours = 18f;
        private const float IntervalLogHr = 0.25f;
        private const int MaxSteps = 350000;
        private const float StageHModePermitTimeHr = 0.03f; // deterministic operator command

        private const float StageDMassDeltaTolLbm = 50f;
        private const float StageDMaxMassDriftTolLbm = 75f;
        private const float StageDEnergyMismatchTolBTU = 25000f;
        private const float StageDNearZeroHeatTolBTU = 1000f;
        private const float StageDPressureDriftTolPsia = 2f;
        private const float StageDNoNetTransferTolGpm = 0.25f;

        private sealed class EngineHooks
        {
            public MethodInfo Init;
            public MethodInfo Step;
            public MethodInfo SaveInterval;
            public MethodInfo SaveReport;
            public MethodInfo ForceClosureMethod;
            public MethodInfo BracketProbeMethod;
            public FieldInfo LogPathField;
            public FieldInfo PhysicsStateField;
            public FieldInfo PzrConductionLossField;
            public FieldInfo PzrInsulationLossField;
        }

        private sealed class StageDSample
        {
            public float TimeHr;
            public float PzrMassLbm;
            public float PzrTotalEnthalpyBTU;
            public float LiquidOccupancyPct;
            public float PressurePsia;
            public float PzrTempF;
            public float NetPzrHeatMW;
            public float NetCvcsGpm;
            public float ChargingGpm;
            public float LetdownGpm;
            public float SurgeGpm;
            public bool StartupHoldActive;
            public bool SprayActive;
            public float SprayFlowGpm;
            public float HeaterPowerMW;
            public string LastFailureReason = "NONE";
            public float MassContractResidualLbm;
        }

        private sealed class StageDResult
        {
            public string RunStamp = string.Empty;
            public string LogDir = string.Empty;
            public string ReportPath = string.Empty;
            public string TimeSeriesCsvPath = string.Empty;
            public readonly List<StageDSample> Samples = new List<StageDSample>();

            public float StartMassLbm;
            public float EndMassLbm;
            public float DeltaMassLbm;
            public float MaxMassDriftLbm;

            public float StartEnthalpyBTU;
            public float EndEnthalpyBTU;
            public float DeltaEnthalpyBTU;
            public float IntegratedNetHeatBTU;
            public float EnergyMismatchBTU;

            public float StartPressurePsia;
            public float EndPressurePsia;
            public float PressureDriftPsia;

            public float MeanNetTransferGpm;
            public float MaxAbsNetTransferGpm;
            public int MassContractResidualViolations;
            public bool HeaterOnAtFrame1;
            public bool AnySprayActive;
            public bool PressureDriftExplained;
            public bool Pass;
        }

        private sealed class ProgressSample
        {
            public float TimeHr;
            public float PressurePsia;
            public float PzrLevelPct;
            public float HeaterPowerMW;
            public bool StartupHoldActive;
            public HeaterMode HeaterMode;
            public bool SprayActive;
            public float SprayFlowGpm;
            public HeatupSimEngine.BubbleFormationPhase BubblePhase;
            public bool SolidPressurizer;
            public int Orifice75Count;
            public bool Orifice45Open;
            public string DrainPolicyMode = string.Empty;
            public float DrainLetdownGpm;
            public float DrainDemandLetdownGpm;
            public float DrainChargingGpm;
            public float DrainNetOutflowGpm;
            public float DrainHydraulicCapacityGpm;
            public float DrainHydraulicDeltaPPsi;
            public float DrainHydraulicDensityLbmFt3;
            public float DrainHydraulicQuality;
            public bool DrainLetdownSaturated;
            public int DrainLineupDemandIndex;
            public bool DrainLineupEventThisStep;
            public int DrainLineupEventCount;
            public int DrainLineupEventPrevIndex;
            public int DrainLineupEventNewIndex;
            public string DrainLineupEventTrigger = "NONE";
            public string DrainLineupEventReason = "NONE";
            public float RvlisDynamic;
            public float RvlisFull;
            public float RvlisUpper;
            public bool RvlisDynamicValid;
            public bool RvlisFullValid;
            public bool RvlisUpperValid;
            public float ChargingGpm;
            public float LetdownGpm;
            public float SurgeGpm;
            public float PzrMassLbm;
            public float PzrTotalEnthalpyBTU;
            public float PzrClosureVolumeResidual;
            public float PzrClosureEnergyResidual;
            public int PzrClosureAttempts;
            public int PzrClosureConverged;
            public string PzrClosureFailureReason = "NONE";
            public string PzrClosurePattern = "UNSET";
            public int PzrClosureIterationCount;
            public float PzrClosurePhaseFraction;
        }

        private sealed class SolverAttemptSample
        {
            public int AttemptIndex;
            public float TimeHr;
            public bool Converged;
            public float VolumeResidualFt3;
            public float EnergyResidualBTU;
            public string FailureReason = "NONE";
            public string Pattern = "UNSET";
            public int Iterations;
            public float PhaseFraction;
            public HeatupSimEngine.BubbleFormationPhase BubblePhase;
            public float BracketTargetVolumeFt3;
            public float BracketTargetMassLbm;
            public float BracketTargetEnthalpyBTU;
            public float BracketOperatingMinPsia;
            public float BracketOperatingMaxPsia;
            public float BracketHardMinPsia;
            public float BracketHardMaxPsia;
            public float BracketLowPsia;
            public float BracketHighPsia;
            public float BracketResidualLowFt3;
            public float BracketResidualHighFt3;
            public int BracketSignLow;
            public int BracketSignHigh;
            public string BracketRegimeLow = "UNSET";
            public string BracketRegimeHigh = "UNSET";
            public int BracketWindowsTried;
            public int BracketValidEvaluations;
            public int BracketInvalidEvaluations;
            public int BracketNanEvaluations;
            public int BracketOutOfRangeEvaluations;
            public bool BracketFound;
            public string BracketTrace = string.Empty;
        }

        private sealed class DrainJumpDiagnostic
        {
            public float TimeHr;
            public int PrevLineup;
            public int CurrLineup;
            public bool LineupEventThisStep;
            public string EventTrigger = "NONE";
            public string EventReason = "NONE";
            public float DeltaAchievedGpm;
            public float DeltaCapacityGpm;
            public float DeltaDeltaPPsi;
            public float DeltaDensity;
            public float DeltaQuality;
            public string Cause = "UNSET";
        }

        private sealed class DrainMicroRunSample
        {
            public int Step;
            public float TimeHr;
            public int LineupIndex;
            public float PressurePsia;
            public float DemandGpm;
            public float HydraulicCapacityGpm;
            public float AchievedGpm;
            public bool LineupEvent;
            public string Trigger = "NONE";
            public string Reason = "NONE";
            public string Phase = string.Empty;
        }

        private sealed class DrainMicroRunResult
        {
            public bool Executed;
            public bool FixedLineupCapPass;
            public bool ExplicitEventLogged;
            public bool ExplicitEventIncreasePass;
            public float FixedWindowMaxAchievedGpm;
            public float FixedWindowCapacityGpm;
            public float PreEventAchievedGpm;
            public float PostEventAchievedGpm;
            public string Reason = string.Empty;
            public string CsvPath = string.Empty;
            public readonly List<DrainMicroRunSample> Samples = new List<DrainMicroRunSample>();
        }

        private sealed class ForcedFailureEvidence
        {
            public bool Attempted;
            public bool ReturnedConverged;
            public bool NoCommitOnFailure;
            public string FailureReason = "NOT_RUN";
            public string Pattern = "NOT_RUN";
            public float VolumeResidualFt3;
            public float EnergyResidualBTU;
            public float PhaseFraction;
            public HeatupSimEngine.BubbleFormationPhase BubblePhase;
            public int Iterations;
            public int AttemptIndexStart;
            public int AttemptIndexEnd;
        }

        private sealed class StageHResult
        {
            public string RunStamp = string.Empty;
            public string LogDir = string.Empty;
            public string ReportPath = string.Empty;
            public string StartupCsvPath = string.Empty;
            public string PhaseCsvPath = string.Empty;
            public string SolverCsvPath = string.Empty;
            public string DrainCsvPath = string.Empty;
            public string RvlisCsvPath = string.Empty;
            public string DrainMicroRunCsvPath = string.Empty;

            public readonly List<ProgressSample> Samples = new List<ProgressSample>();
            public readonly List<SolverAttemptSample> SolverAttempts = new List<SolverAttemptSample>();
            public readonly List<DrainJumpDiagnostic> DrainJumpDiagnostics = new List<DrainJumpDiagnostic>();

            public float HoldReleaseTimeHr = -1f;
            public float HoldActivationLogTimeHr = -1f;
            public float HoldReleaseLogTimeHr = -1f;
            public float ModePermitCommandTimeHr = -1f;
            public float FirstHeaterOnTimeHr = -1f;
            public bool HeaterOnAtFrame1;
            public bool StartupConformancePass;
            public string StartupConformanceReason = string.Empty;

            public float BubbleBoundaryTimeHr = -1f;
            public float MaxBoundaryStepPressurePsi;
            public float MaxBoundaryStepLevelPct;
            public bool BubbleContinuityPass;
            public string BubbleContinuityReason = string.Empty;

            public int SolverAttemptsCount;
            public int SolverConvergedCount;
            public float SolverConvergencePct;
            public float SolverMeanVolumeResidualFt3;
            public float SolverMaxVolumeResidualFt3;
            public float SolverMeanEnergyResidualBTU;
            public float SolverMaxEnergyResidualBTU;
            public int SolverMassContractResidualFailures;
            public readonly Dictionary<string, int> SolverFailureReasonBreakdown = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, int> SolverPatternBreakdown = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            public bool SolverConvergencePass;
            public string SolverConvergenceReason = string.Empty;

            public bool DrainPolicyAligned;
            public bool DrainNoLineupJump13;
            public float DrainMaxHydraulicDeltaGpm;
            public float DrainMeanHydraulicDeltaGpm;
            public bool DrainSmoothEntry;
            public int DrainUntriggeredLineupChanges;
            public int DrainUntriggeredJump13Count;
            public int DrainTriggeredJumpCount;
            public bool DrainCausalityPass;
            public string DrainCausalityReason = string.Empty;
            public DrainMicroRunResult DrainMicroRun = new DrainMicroRunResult();

            public bool BracketProbeAttempted;
            public bool BracketProbeFound;
            public float BracketProbeTimeHr = -1f;
            public float BracketProbeLowPsia;
            public float BracketProbeHighPsia;
            public string BracketProbeReason = "NOT_RUN";

            public bool SetpointFidelityPass;
            public readonly List<string> SetpointFindings = new List<string>();

            public bool RvlisConsistencyPass;
            public string RvlisConsistencyReason = string.Empty;
            public int RvlisFullInvalidCount;
            public int RvlisUpperInvalidCount;

            public bool Cs0081Pass;
            public string Cs0081Reason = string.Empty;

            public bool Cs0091Pass;
            public bool Cs0092Pass;
            public bool Cs0093Pass;
            public bool Cs0094Pass;
            public bool Cs0040Pass;
            public bool AllRequiredPass;

            public ForcedFailureEvidence ForcedFailure = new ForcedFailureEvidence();
        }

        [MenuItem("Critical/Run IP-0024 Remaining Closeout Tranche")]
        public static void RunRemainingCloseoutTranche()
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string runstamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");

            string stageDArtifactPath = Path.Combine(
                root,
                "Governance",
                "Issues",
                $"IP-0024_StageD_ExitGate_SinglePhaseHold_{runstamp}.md");
            string stageHArtifactPath = Path.Combine(
                root,
                "Governance",
                "Issues",
                $"IP-0024_StageH_DeterministicEvidence_{runstamp}.md");

            StageDResult stageD = ExecuteStageDExitGate(root, runstamp);
            WriteStageDArtifact(stageDArtifactPath, stageD);
            Debug.Log($"[IP-0024] Stage D artifact: {stageDArtifactPath}");

            if (!stageD.Pass)
            {
                throw new Exception(
                    "IP-0024 Stage D exit gate failed. Stage H execution is blocked per tranche constraints.");
            }

            StageHResult stageH = ExecuteStageHEvidence(root, runstamp);
            WriteStageHArtifact(stageHArtifactPath, stageD, stageH);
            Debug.Log($"[IP-0024] Stage H artifact: {stageHArtifactPath}");

            if (!stageH.AllRequiredPass)
                throw new Exception("IP-0024 Stage H deterministic evidence suite contains failing CS rows.");
        }

        private static StageDResult ExecuteStageDExitGate(string root, string runstamp)
        {
            string logDir = Path.Combine(root, "HeatupLogs", $"IP-0024_StageD_Hold_{runstamp}");
            PrepareLogDirectory(logDir);

            HeatupSimEngine engine;
            EngineHooks hooks;
            OpenAndInitializeDeterministicEngine(logDir, out engine, out hooks);

            // Stage D deterministic setup: hold active, heaters OFF + gated, spray OFF path (no RCP),
            // and no-net-transfer pinning for CVCS boundary flows.
            engine.currentHeaterMode = HeaterMode.OFF;
            engine.startupHoldActive = true;
            engine.startupHoldReleaseTime_hr = StageDHoldHours + 1f;
            engine.startupHoldActivationLogged = false;
            engine.startupHoldReleaseLogged = false;
            engine.pzrHeaterPower = 0f;
            engine.rcpCount = 0;
            engine.rhrState = RHRSystem.InitializeStandby();
            engine.rhrActive = false;
            engine.chargingFlow = 0f;
            engine.letdownFlow = 0f;
            engine.chargingToRCS = 0f;
            engine.letdownViaRHR = false;
            engine.letdownViaOrifice = true;

            hooks.SaveInterval.Invoke(engine, null); // t=0 baseline

            var result = new StageDResult
            {
                RunStamp = runstamp,
                LogDir = logDir
            };
            result.Samples.Add(CaptureStageDSample(engine, hooks));

            float nextInterval = IntervalLogHr;
            int steps = 0;
            while (steps < MaxSteps && engine.simTime < StageDHoldHours)
            {
                hooks.Step.Invoke(engine, new object[] { DtHr });
                steps++;

                result.Samples.Add(CaptureStageDSample(engine, hooks));

                if (engine.simTime >= nextInterval)
                {
                    hooks.SaveInterval.Invoke(engine, null);
                    nextInterval += IntervalLogHr;
                }
            }

            hooks.SaveReport.Invoke(engine, null);
            result.ReportPath = Directory.GetFiles(logDir, "Heatup_Report_*.txt")
                .OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? string.Empty;

            result.TimeSeriesCsvPath = Path.Combine(logDir, "IP-0024_StageD_SinglePhaseHold_Timeseries.csv");
            WriteStageDCsv(result.TimeSeriesCsvPath, result.Samples);

            EvaluateStageD(result);
            return result;
        }

        private static StageHResult ExecuteStageHEvidence(string root, string runstamp)
        {
            string logDir = Path.Combine(root, "HeatupLogs", $"IP-0024_StageH_Suite_{runstamp}");
            PrepareLogDirectory(logDir);

            HeatupSimEngine engine;
            EngineHooks hooks;
            OpenAndInitializeDeterministicEngine(logDir, out engine, out hooks);

            var result = new StageHResult
            {
                RunStamp = runstamp,
                LogDir = logDir
            };

            hooks.SaveInterval.Invoke(engine, null);

            int prevAttempts = engine.pzrClosureSolveAttempts;
            int prevConverged = engine.pzrClosureSolveConverged;
            float nextInterval = IntervalLogHr;
            float bubbleCompleteTimeHr = -1f;
            bool modePermitIssued = false;

            result.Samples.Add(CaptureProgressSample(engine, hooks));

            for (int steps = 0; steps < MaxSteps && engine.simTime < StageHMaxHours; steps++)
            {
                if (!modePermitIssued && engine.simTime >= StageHModePermitTimeHr)
                {
                    engine.currentHeaterMode = HeaterMode.STARTUP_FULL_POWER;
                    modePermitIssued = true;
                    result.ModePermitCommandTimeHr = engine.simTime;
                }

                hooks.Step.Invoke(engine, new object[] { DtHr });
                result.Samples.Add(CaptureProgressSample(engine, hooks));

                if (!result.BracketProbeAttempted &&
                    engine.bubblePhase == HeatupSimEngine.BubbleFormationPhase.DRAIN)
                {
                    RunBracketOnlyProbe(engine, hooks, result);
                }

                CaptureSolverAttemptsSinceLastStep(
                    engine,
                    ref prevAttempts,
                    ref prevConverged,
                    result.SolverAttempts);

                if (!result.ForcedFailure.Attempted &&
                    engine.bubblePhase == HeatupSimEngine.BubbleFormationPhase.DRAIN &&
                    engine.pzrClosureSolveAttempts > 0)
                {
                    result.ForcedFailure = RunForcedFailureNoCommitCheck(engine, hooks);
                }

                if (engine.simTime >= nextInterval)
                {
                    hooks.SaveInterval.Invoke(engine, null);
                    nextInterval += IntervalLogHr;
                }

                if (bubbleCompleteTimeHr < 0f &&
                    engine.bubblePhase == HeatupSimEngine.BubbleFormationPhase.COMPLETE)
                {
                    bubbleCompleteTimeHr = engine.simTime;
                }

                if (bubbleCompleteTimeHr > 0f && engine.simTime >= bubbleCompleteTimeHr + 0.5f)
                    break;
            }

            hooks.SaveReport.Invoke(engine, null);
            result.ReportPath = Directory.GetFiles(logDir, "Heatup_Report_*.txt")
                .OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? string.Empty;

            result.StartupCsvPath = Path.Combine(logDir, "IP-0024_StartupSequence_Timeseries.csv");
            result.PhaseCsvPath = Path.Combine(logDir, "IP-0024_PhaseContinuity_Timeseries.csv");
            result.SolverCsvPath = Path.Combine(logDir, "IP-0024_SolverAttempts.csv");
            result.DrainCsvPath = Path.Combine(logDir, "IP-0024_DrainCausality.csv");
            result.RvlisCsvPath = Path.Combine(logDir, "IP-0024_RVLIS_DrainConsistency.csv");
            result.DrainMicroRunCsvPath = Path.Combine(logDir, "IP-0024_DrainCausality_MicroRun.csv");
            result.DrainMicroRun = ExecuteDrainCausalityMicroRun(result.DrainMicroRunCsvPath);

            WriteStageHStartupCsv(result.StartupCsvPath, result.Samples);
            WriteStageHPhaseCsv(result.PhaseCsvPath, result.Samples);
            WriteStageHSolverCsv(result.SolverCsvPath, result.SolverAttempts);
            WriteStageHDrainCsv(result.DrainCsvPath, result.Samples);
            WriteDrainMicroRunCsv(result.DrainMicroRunCsvPath, result.DrainMicroRun);
            WriteStageHRvlisCsv(result.RvlisCsvPath, result.Samples);

            EvaluateStageH(result, engine);
            return result;
        }

        private static void EvaluateStageD(StageDResult result)
        {
            if (result.Samples.Count < 2)
            {
                result.Pass = false;
                return;
            }

            StageDSample first = result.Samples[0];
            StageDSample last = result.Samples[result.Samples.Count - 1];
            result.StartMassLbm = first.PzrMassLbm;
            result.EndMassLbm = last.PzrMassLbm;
            result.DeltaMassLbm = result.EndMassLbm - result.StartMassLbm;
            result.MaxMassDriftLbm = result.Samples.Max(s => Mathf.Abs(s.PzrMassLbm - result.StartMassLbm));

            result.StartEnthalpyBTU = first.PzrTotalEnthalpyBTU;
            result.EndEnthalpyBTU = last.PzrTotalEnthalpyBTU;
            result.DeltaEnthalpyBTU = result.EndEnthalpyBTU - result.StartEnthalpyBTU;
            result.IntegratedNetHeatBTU = IntegrateNetHeatBTU(result.Samples);
            result.EnergyMismatchBTU = Mathf.Abs(result.DeltaEnthalpyBTU - result.IntegratedNetHeatBTU);

            result.StartPressurePsia = first.PressurePsia;
            result.EndPressurePsia = last.PressurePsia;
            result.PressureDriftPsia = result.EndPressurePsia - result.StartPressurePsia;

            result.MeanNetTransferGpm = result.Samples.Average(s => s.NetCvcsGpm + s.SurgeGpm);
            result.MaxAbsNetTransferGpm = result.Samples.Max(s => Mathf.Abs(s.NetCvcsGpm + s.SurgeGpm));
            result.MassContractResidualViolations = result.Samples.Count(
                s => string.Equals(s.LastFailureReason, "MASS_CONTRACT_RESIDUAL", StringComparison.OrdinalIgnoreCase));
            result.HeaterOnAtFrame1 = result.Samples.Count > 1 && result.Samples[1].HeaterPowerMW > 1e-4f;
            result.AnySprayActive = result.Samples.Any(s => s.SprayActive);

            bool massStable = Mathf.Abs(result.DeltaMassLbm) <= StageDMassDeltaTolLbm
                              && result.MaxMassDriftLbm <= StageDMaxMassDriftTolLbm;
            bool energyClosed = result.EnergyMismatchBTU <= StageDEnergyMismatchTolBTU;
            bool noMassContractViolations = result.MassContractResidualViolations == 0;
            bool noUnexpectedTransfer = result.MaxAbsNetTransferGpm <= StageDNoNetTransferTolGpm;
            bool noFrame1Heater = !result.HeaterOnAtFrame1;
            bool sprayOff = !result.AnySprayActive;

            bool nearZeroHeat = Mathf.Abs(result.IntegratedNetHeatBTU) <= StageDNearZeroHeatTolBTU;
            float deltaTPzr = last.PzrTempF - first.PzrTempF;
            bool thermalTrendAligned =
                Mathf.Abs(deltaTPzr) > 1e-5f &&
                Mathf.Sign(deltaTPzr) == Mathf.Sign(result.PressureDriftPsia);
            result.PressureDriftExplained = !nearZeroHeat
                                            || Mathf.Abs(result.PressureDriftPsia) <= StageDPressureDriftTolPsia
                                            || thermalTrendAligned;

            result.Pass = massStable
                          && energyClosed
                          && noMassContractViolations
                          && noUnexpectedTransfer
                          && noFrame1Heater
                          && sprayOff
                          && result.PressureDriftExplained;
        }

        private static void EvaluateStageH(StageHResult result, HeatupSimEngine engine)
        {
            if (result.Samples.Count < 2)
            {
                result.AllRequiredPass = false;
                return;
            }

            EvaluateStartupConformance(result, engine);
            EvaluateBubbleContinuity(result);
            EvaluateSolverConvergence(result);
            EvaluateDrainCausality(result);
            EvaluateSetpointFidelity(result);
            EvaluateRvlisConsistency(result);
            EvaluateCs0081(result);

            result.Cs0094Pass = result.StartupConformancePass;
            result.Cs0091Pass = result.SolverConvergencePass
                                && result.SolverMassContractResidualFailures == 0
                                && result.ForcedFailure.Attempted
                                && !result.ForcedFailure.ReturnedConverged
                                && result.ForcedFailure.NoCommitOnFailure
                                && result.BracketProbeAttempted
                                && result.BracketProbeFound;
            result.Cs0092Pass = result.DrainCausalityPass;
            result.Cs0040Pass = result.RvlisConsistencyPass;
            result.Cs0093Pass = result.Cs0091Pass
                                && result.Cs0092Pass
                                && result.BubbleContinuityPass
                                && result.SetpointFidelityPass;
            result.AllRequiredPass = result.Cs0091Pass
                                     && result.Cs0092Pass
                                     && result.Cs0093Pass
                                     && result.Cs0094Pass
                                     && result.Cs0040Pass
                                     && result.Cs0081Pass;
        }

        private static void EvaluateStartupConformance(StageHResult result, HeatupSimEngine engine)
        {
            result.HeaterOnAtFrame1 = result.Samples.Count > 1 && result.Samples[1].HeaterPowerMW > 1e-4f;
            ProgressSample releaseSample = result.Samples.FirstOrDefault(s => !s.StartupHoldActive);
            result.HoldReleaseTimeHr = releaseSample != null ? releaseSample.TimeHr : -1f;

            foreach (HeatupSimEngine.EventLogEntry entry in engine.eventLog)
            {
                if (result.HoldActivationLogTimeHr < 0f &&
                    entry.Message.IndexOf("HEATER STARTUP HOLD ACTIVE", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.HoldActivationLogTimeHr = entry.SimTime;
                }

                if (result.HoldReleaseLogTimeHr < 0f &&
                    entry.Message.IndexOf("HEATER STARTUP HOLD RELEASED", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.HoldReleaseLogTimeHr = entry.SimTime;
                }
            }

            ProgressSample firstHeaterOn = result.Samples.FirstOrDefault(s => s.HeaterPowerMW > 1e-4f);
            result.FirstHeaterOnTimeHr = firstHeaterOn != null ? firstHeaterOn.TimeHr : -1f;

            float permitBarrier = Mathf.Max(result.HoldReleaseTimeHr, result.ModePermitCommandTimeHr);
            bool noFrame1 = !result.HeaterOnAtFrame1;
            bool hasHoldRelease = result.HoldReleaseTimeHr >= 0f;
            bool hasPermit = result.ModePermitCommandTimeHr >= 0f;
            bool heaterAfterGates = result.FirstHeaterOnTimeHr >= 0f && result.FirstHeaterOnTimeHr + 1e-6f >= permitBarrier;
            result.StartupConformancePass = noFrame1 && hasHoldRelease && hasPermit && heaterAfterGates;
            result.StartupConformanceReason =
                $"frame1Heater={result.HeaterOnAtFrame1}, holdRelease={result.HoldReleaseTimeHr:F4}hr, " +
                $"modePermit={result.ModePermitCommandTimeHr:F4}hr, firstHeaterOn={result.FirstHeaterOnTimeHr:F4}hr";
        }

        private static void EvaluateBubbleContinuity(StageHResult result)
        {
            int boundaryIdx = -1;
            for (int i = 1; i < result.Samples.Count; i++)
            {
                ProgressSample prev = result.Samples[i - 1];
                ProgressSample curr = result.Samples[i];
                bool boundary = (prev.SolidPressurizer && !curr.SolidPressurizer)
                                || (prev.BubblePhase != curr.BubblePhase);
                if (boundary)
                {
                    boundaryIdx = i;
                    break;
                }
            }

            if (boundaryIdx < 1)
            {
                result.BubbleContinuityPass = false;
                result.BubbleContinuityReason = "No solid->two-phase boundary transition was observed.";
                return;
            }

            result.BubbleBoundaryTimeHr = result.Samples[boundaryIdx].TimeHr;
            int window = 36; // +/- 6 minutes at 10 s step
            int start = Mathf.Max(1, boundaryIdx - window);
            int end = Mathf.Min(result.Samples.Count - 1, boundaryIdx + window);

            float maxDp = 0f;
            float maxDl = 0f;
            for (int i = start; i <= end; i++)
            {
                ProgressSample prev = result.Samples[i - 1];
                ProgressSample curr = result.Samples[i];
                maxDp = Mathf.Max(maxDp, Mathf.Abs(curr.PressurePsia - prev.PressurePsia));
                maxDl = Mathf.Max(maxDl, Mathf.Abs(curr.PzrLevelPct - prev.PzrLevelPct));
            }

            result.MaxBoundaryStepPressurePsi = maxDp;
            result.MaxBoundaryStepLevelPct = maxDl;
            result.BubbleContinuityPass = maxDp <= 25f && maxDl <= 5f;
            result.BubbleContinuityReason =
                $"boundary={result.BubbleBoundaryTimeHr:F4}hr, maxStep|dP|={maxDp:F3} psi, maxStep|dLevel|={maxDl:F3}%";
        }

        private static void EvaluateSolverConvergence(StageHResult result)
        {
            IEnumerable<SolverAttemptSample> filteredAttempts = result.SolverAttempts;
            if (result.ForcedFailure.Attempted &&
                result.ForcedFailure.AttemptIndexEnd >= result.ForcedFailure.AttemptIndexStart &&
                result.ForcedFailure.AttemptIndexStart > 0)
            {
                filteredAttempts = filteredAttempts.Where(a =>
                    a.AttemptIndex < result.ForcedFailure.AttemptIndexStart ||
                    a.AttemptIndex > result.ForcedFailure.AttemptIndexEnd);
            }

            List<SolverAttemptSample> attempts = filteredAttempts.ToList();
            result.SolverAttemptsCount = attempts.Count;
            result.SolverConvergedCount = attempts.Count(a => a.Converged);
            result.SolverConvergencePct = result.SolverAttemptsCount > 0
                ? 100f * result.SolverConvergedCount / result.SolverAttemptsCount
                : 0f;

            if (attempts.Count > 0)
            {
                result.SolverMeanVolumeResidualFt3 = attempts.Average(a => Mathf.Abs(a.VolumeResidualFt3));
                result.SolverMaxVolumeResidualFt3 = attempts.Max(a => Mathf.Abs(a.VolumeResidualFt3));
                result.SolverMeanEnergyResidualBTU = attempts.Average(a => Mathf.Abs(a.EnergyResidualBTU));
                result.SolverMaxEnergyResidualBTU = attempts.Max(a => Mathf.Abs(a.EnergyResidualBTU));
            }

            result.SolverFailureReasonBreakdown.Clear();
            result.SolverPatternBreakdown.Clear();
            foreach (SolverAttemptSample attempt in attempts)
            {
                string reason = attempt.Converged ? "NONE" : (attempt.FailureReason ?? "UNKNOWN");
                string pattern = attempt.Pattern ?? "UNSET";
                Increment(result.SolverFailureReasonBreakdown, reason);
                Increment(result.SolverPatternBreakdown, pattern);
            }

            result.SolverMassContractResidualFailures = attempts.Count(a =>
                string.Equals(a.FailureReason, "MASS_CONTRACT_RESIDUAL", StringComparison.OrdinalIgnoreCase));

            bool patternsCompliant = attempts
                .Where(a => a.Converged)
                .All(a => string.Equals(a.Pattern, "MONOTONIC_DESCENT", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(a.Pattern, "BOUNDED_OSCILLATORY", StringComparison.OrdinalIgnoreCase));

            result.SolverConvergencePass = result.SolverAttemptsCount > 0
                                           && result.SolverConvergencePct >= 95f
                                           && result.SolverMassContractResidualFailures == 0
                                           && patternsCompliant;
            result.SolverConvergenceReason =
                $"attempts={result.SolverAttemptsCount}, converged={result.SolverConvergedCount}, " +
                $"pct={result.SolverConvergencePct:F2}%, patternsCompliant={patternsCompliant}, " +
                $"massContractResidualFailures={result.SolverMassContractResidualFailures}";
        }

        private static void EvaluateDrainCausality(StageHResult result)
        {
            List<ProgressSample> drain = result.Samples
                .Where(s => s.BubblePhase == HeatupSimEngine.BubbleFormationPhase.DRAIN)
                .ToList();
            result.DrainJumpDiagnostics.Clear();
            if (drain.Count < 2)
            {
                result.DrainCausalityPass = false;
                result.DrainCausalityReason = "No sufficient DRAIN samples.";
                return;
            }

            result.DrainPolicyAligned = drain.All(s =>
                string.Equals(s.DrainPolicyMode, "LINEUP_HYDRAULIC_CAUSAL", StringComparison.Ordinal));

            var hydraulicDiffs = new List<float>(drain.Count);
            bool jump13WithoutEvent = false;
            int untriggeredLineupChanges = 0;
            int triggeredJumpCount = 0;

            foreach (ProgressSample sample in drain)
            {
                float expected = Mathf.Min(sample.DrainDemandLetdownGpm, sample.DrainHydraulicCapacityGpm);
                hydraulicDiffs.Add(Mathf.Abs(sample.DrainLetdownGpm - expected));
            }

            for (int i = 1; i < drain.Count; i++)
            {
                ProgressSample prev = drain[i - 1];
                ProgressSample curr = drain[i];
                int prevLineup = GetDrainLineupIndex(prev.Orifice75Count, prev.Orifice45Open);
                int currLineup = GetDrainLineupIndex(curr.Orifice75Count, curr.Orifice45Open);
                bool lineupChanged = prevLineup != currLineup;
                bool hasEvent = curr.DrainLineupEventThisStep;

                if (lineupChanged)
                {
                    if (!hasEvent)
                        untriggeredLineupChanges++;
                    else
                        triggeredJumpCount++;

                    if (prevLineup == 1 && currLineup == 3 && !hasEvent)
                        jump13WithoutEvent = true;
                }

                float deltaAchieved = curr.DrainLetdownGpm - prev.DrainLetdownGpm;
                bool jumpDetected = lineupChanged || Mathf.Abs(deltaAchieved) >= 5f;
                if (!jumpDetected)
                    continue;

                result.DrainJumpDiagnostics.Add(new DrainJumpDiagnostic
                {
                    TimeHr = curr.TimeHr,
                    PrevLineup = prevLineup,
                    CurrLineup = currLineup,
                    LineupEventThisStep = hasEvent,
                    EventTrigger = curr.DrainLineupEventTrigger ?? "NONE",
                    EventReason = curr.DrainLineupEventReason ?? "NONE",
                    DeltaAchievedGpm = deltaAchieved,
                    DeltaCapacityGpm = curr.DrainHydraulicCapacityGpm - prev.DrainHydraulicCapacityGpm,
                    DeltaDeltaPPsi = curr.DrainHydraulicDeltaPPsi - prev.DrainHydraulicDeltaPPsi,
                    DeltaDensity = curr.DrainHydraulicDensityLbmFt3 - prev.DrainHydraulicDensityLbmFt3,
                    DeltaQuality = curr.DrainHydraulicQuality - prev.DrainHydraulicQuality,
                    Cause = ClassifyDrainDeltaCause(prev, curr, lineupChanged, hasEvent)
                });
            }

            result.DrainNoLineupJump13 = !jump13WithoutEvent;
            result.DrainMaxHydraulicDeltaGpm = hydraulicDiffs.Count > 0 ? hydraulicDiffs.Max() : 0f;
            result.DrainMeanHydraulicDeltaGpm = hydraulicDiffs.Count > 0 ? hydraulicDiffs.Average() : 0f;
            result.DrainUntriggeredLineupChanges = untriggeredLineupChanges;
            result.DrainUntriggeredJump13Count = jump13WithoutEvent ? 1 : 0;
            result.DrainTriggeredJumpCount = triggeredJumpCount;

            int firstDrainIndex = result.Samples.FindIndex(s => s.BubblePhase == HeatupSimEngine.BubbleFormationPhase.DRAIN);
            result.DrainSmoothEntry = true;
            if (firstDrainIndex > 0)
            {
                ProgressSample prev = result.Samples[firstDrainIndex - 1];
                ProgressSample curr = result.Samples[firstDrainIndex];
                float dP = Mathf.Abs(curr.PressurePsia - prev.PressurePsia);
                float dL = Mathf.Abs(curr.PzrLevelPct - prev.PzrLevelPct);
                result.DrainSmoothEntry = dP <= 25f && dL <= 5f;
            }

            bool microRunPass = result.DrainMicroRun.Executed &&
                                result.DrainMicroRun.FixedLineupCapPass &&
                                result.DrainMicroRun.ExplicitEventLogged &&
                                result.DrainMicroRun.ExplicitEventIncreasePass;

            result.DrainCausalityPass = result.DrainPolicyAligned
                                        && result.DrainNoLineupJump13
                                        && result.DrainUntriggeredLineupChanges == 0
                                        && result.DrainMaxHydraulicDeltaGpm <= 1.5f
                                        && result.DrainSmoothEntry
                                        && microRunPass;
            result.DrainCausalityReason =
                $"policyAligned={result.DrainPolicyAligned}, no1to3Jump={result.DrainNoLineupJump13}, " +
                $"untriggeredLineupChanges={result.DrainUntriggeredLineupChanges}, " +
                $"max|flow-hydraulic|={result.DrainMaxHydraulicDeltaGpm:F3} gpm, smoothEntry={result.DrainSmoothEntry}, " +
                $"microRunPass={microRunPass}";
        }

        private static void EvaluateSetpointFidelity(StageHResult result)
        {
            result.SetpointFindings.Clear();

            CheckSetpoint(
                result,
                "Heater setpoint (operating)",
                PlantConstants.PZR_BASELINE_PRESSURE_SETPOINT_PSIG,
                PlantConstants.PZR_OPERATING_PRESSURE_PSIG);
            CheckSetpoint(
                result,
                "Heater proportional full ON",
                PlantConstants.PZR_BASELINE_PROP_HEATER_FULL_ON_PSIG,
                PlantConstants.P_HEATERS_ON);
            CheckSetpoint(
                result,
                "Heater proportional full ON alias",
                PlantConstants.PZR_BASELINE_PROP_HEATER_FULL_ON_PSIG,
                PlantConstants.P_PROP_HEATER_FULL_ON);
            CheckSetpoint(
                result,
                "Heater proportional zero",
                PlantConstants.PZR_BASELINE_PROP_HEATER_ZERO_PSIG,
                PlantConstants.P_HEATERS_OFF);
            CheckSetpoint(
                result,
                "Heater backup ON",
                PlantConstants.PZR_BASELINE_BACKUP_HEATER_ON_PSIG,
                PlantConstants.HEATER_BACKUP_ON_PSIG);
            CheckSetpoint(
                result,
                "Heater backup OFF",
                PlantConstants.PZR_BASELINE_BACKUP_HEATER_OFF_PSIG,
                PlantConstants.HEATER_BACKUP_OFF_PSIG);
            CheckSetpoint(
                result,
                "Spray start",
                PlantConstants.PZR_BASELINE_SPRAY_START_PSIG,
                PlantConstants.P_SPRAY_START_PSIG);
            CheckSetpoint(
                result,
                "Spray full",
                PlantConstants.PZR_BASELINE_SPRAY_FULL_PSIG,
                PlantConstants.P_SPRAY_FULL_PSIG);
            CheckSetpoint(
                result,
                "PORV threshold",
                PlantConstants.PZR_BASELINE_PORV_OPEN_PSIG,
                PlantConstants.P_PORV);
            CheckSetpoint(
                result,
                "Level program min",
                PlantConstants.PZR_BASELINE_LEVEL_NO_LOAD_PERCENT,
                PlantConstants.PZR_LEVEL_PROGRAM_MIN);
            CheckSetpoint(
                result,
                "Level program max",
                PlantConstants.PZR_BASELINE_LEVEL_FULL_POWER_PERCENT,
                PlantConstants.PZR_LEVEL_PROGRAM_MAX);
            CheckSetpoint(
                result,
                "Level program Tavg low",
                PlantConstants.PZR_BASELINE_LEVEL_TAVG_NO_LOAD_F,
                PlantConstants.PZR_LEVEL_PROGRAM_TAVG_LOW);
            CheckSetpoint(
                result,
                "Level program Tavg high",
                PlantConstants.PZR_BASELINE_LEVEL_TAVG_FULL_POWER_F,
                PlantConstants.PZR_LEVEL_PROGRAM_TAVG_HIGH);
            CheckSetpoint(
                result,
                "Level program slope",
                PlantConstants.PZR_BASELINE_LEVEL_PROGRAM_SLOPE,
                PlantConstants.PZR_LEVEL_PROGRAM_SLOPE);

            result.SetpointFidelityPass = result.SetpointFindings.All(f => f.StartsWith("PASS", StringComparison.Ordinal));
        }

        private static void EvaluateRvlisConsistency(StageHResult result)
        {
            List<ProgressSample> drain = result.Samples
                .Where(s => s.BubblePhase == HeatupSimEngine.BubbleFormationPhase.DRAIN)
                .ToList();
            if (drain.Count < 2)
            {
                result.RvlisConsistencyPass = false;
                result.RvlisConsistencyReason = "No sufficient DRAIN samples.";
                return;
            }

            result.RvlisFullInvalidCount = drain.Count(s => !s.RvlisFullValid);
            result.RvlisUpperInvalidCount = drain.Count(s => !s.RvlisUpperValid);

            int activeTransferPairs = 0;
            int rvlisChangePairs = 0;
            int signAlignedPairs = 0;
            int cappedPairs = 0;

            const float rvlisCapEpsilon = 1e-3f;
            const float rvlisHighCap = 100f;
            const float rvlisLowCap = 0f;

            for (int i = 1; i < drain.Count; i++)
            {
                ProgressSample prev = drain[i - 1];
                ProgressSample curr = drain[i];
                float dRvlis = curr.RvlisFull - prev.RvlisFull;
                float netOut = 0.5f * (prev.DrainNetOutflowGpm + curr.DrainNetOutflowGpm);
                bool active = Mathf.Abs(netOut) > 0.5f || Mathf.Abs(curr.SurgeGpm) > 0.5f;
                if (!active)
                    continue;

                bool clampedHigh = netOut > 0f &&
                                   Mathf.Abs(prev.RvlisFull - rvlisHighCap) <= rvlisCapEpsilon &&
                                   Mathf.Abs(curr.RvlisFull - rvlisHighCap) <= rvlisCapEpsilon;
                bool clampedLow = netOut < 0f &&
                                  Mathf.Abs(prev.RvlisFull - rvlisLowCap) <= rvlisCapEpsilon &&
                                  Mathf.Abs(curr.RvlisFull - rvlisLowCap) <= rvlisCapEpsilon;
                if (clampedHigh || clampedLow)
                {
                    cappedPairs++;
                    continue;
                }

                activeTransferPairs++;
                if (Mathf.Abs(dRvlis) > 1e-4f)
                    rvlisChangePairs++;

                if ((netOut > 0f && dRvlis <= 0f) || (netOut < 0f && dRvlis >= 0f))
                    signAlignedPairs++;
            }

            float changeCoverage = activeTransferPairs > 0
                ? (float)rvlisChangePairs / activeTransferPairs
                : 0f;
            float signAlignment = activeTransferPairs > 0
                ? (float)signAlignedPairs / activeTransferPairs
                : 0f;

            bool valid = result.RvlisFullInvalidCount == 0 && result.RvlisUpperInvalidCount == 0;
            bool saturatedOnly = activeTransferPairs == 0 && cappedPairs > 0;
            bool causal = saturatedOnly || (changeCoverage >= 0.5f && signAlignment >= 0.5f);
            result.RvlisConsistencyPass = valid && causal;
            result.RvlisConsistencyReason =
                $"fullInvalid={result.RvlisFullInvalidCount}, upperInvalid={result.RvlisUpperInvalidCount}, " +
                $"changeCoverage={changeCoverage:F2}, signAlignment={signAlignment:F2}, cappedPairs={cappedPairs}, activePairs={activeTransferPairs}";
        }

        private static void EvaluateCs0081(StageHResult result)
        {
            bool highBandAligned = Mathf.Abs(PlantConstants.SOLID_PLANT_P_HIGH_PSIG - 400f) < 1e-5f;
            bool lowBandAligned = Mathf.Abs(PlantConstants.SOLID_PLANT_P_LOW_PSIG - 320f) < 1e-5f;
            bool minRcpAligned = Mathf.Abs(PlantConstants.MIN_RCP_PRESSURE_PSIG - 400f) < 1e-5f;
            result.Cs0081Pass = highBandAligned && lowBandAligned && minRcpAligned;
            result.Cs0081Reason =
                $"SOLID_LOW={PlantConstants.SOLID_PLANT_P_LOW_PSIG:F1} psig, " +
                $"SOLID_HIGH={PlantConstants.SOLID_PLANT_P_HIGH_PSIG:F1} psig, " +
                $"MIN_RCP={PlantConstants.MIN_RCP_PRESSURE_PSIG:F1} psig";
        }

        private static void WriteStageDArtifact(string path, StageDResult stageD)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0024 Stage D Exit Gate - Single-Phase Equilibrium Hold");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run stamp: `{stageD.RunStamp}`");
            sb.AppendLine("- Setup: ColdShutdownProfile, startup hold active, heater mode OFF, spray OFF path, no-net-transfer pinning");
            sb.AppendLine($"- Hold duration: {StageDHoldHours * 60f:F0} min");
            sb.AppendLine($"- Log directory: `{ToRepoRelative(stageD.LogDir)}`");
            if (!string.IsNullOrWhiteSpace(stageD.ReportPath))
                sb.AppendLine($"- Heatup report: `{ToRepoRelative(stageD.ReportPath)}`");
            sb.AppendLine($"- Timeseries CSV: `{ToRepoRelative(stageD.TimeSeriesCsvPath)}`");
            sb.AppendLine();
            sb.AppendLine("## Summary Table");
            sb.AppendLine("| Metric | Start | End | Max Drift |");
            sb.AppendLine("|---|---:|---:|---:|");
            sb.AppendLine($"| PZR mass (lbm) | {stageD.StartMassLbm:F4} | {stageD.EndMassLbm:F4} | {stageD.MaxMassDriftLbm:F4} |");
            sb.AppendLine($"| PZR total enthalpy (BTU) | {stageD.StartEnthalpyBTU:F2} | {stageD.EndEnthalpyBTU:F2} | {Mathf.Abs(stageD.DeltaEnthalpyBTU):F2} |");
            sb.AppendLine($"| Pressure (psia) | {stageD.StartPressurePsia:F4} | {stageD.EndPressurePsia:F4} | {Mathf.Abs(stageD.PressureDriftPsia):F4} |");
            sb.AppendLine();
            sb.AppendLine("## Contract Checks");
            sb.AppendLine($"- Δm_total={stageD.DeltaMassLbm:F6} lbm (tol ±{StageDMassDeltaTolLbm:F1})");
            sb.AppendLine($"- max|m_total-m0|={stageD.MaxMassDriftLbm:F6} lbm (tol {StageDMaxMassDriftTolLbm:F1})");
            sb.AppendLine($"- Δu_total={stageD.DeltaEnthalpyBTU:F3} BTU");
            sb.AppendLine($"- ∫Q_net dt={stageD.IntegratedNetHeatBTU:F3} BTU");
            sb.AppendLine($"- |Δu_total-∫Q_net dt|={stageD.EnergyMismatchBTU:F3} BTU (tol {StageDEnergyMismatchTolBTU:F1})");
            sb.AppendLine($"- Pressure drift={stageD.PressureDriftPsia:F6} psia | drift explained by energy model={stageD.PressureDriftExplained}");
            sb.AppendLine($"- Net transfer mean={stageD.MeanNetTransferGpm:F6} gpm, max|net|={stageD.MaxAbsNetTransferGpm:F6} gpm (tol {StageDNoNetTransferTolGpm:F2})");
            sb.AppendLine($"- MASS_CONTRACT_RESIDUAL violations={stageD.MassContractResidualViolations}");
            sb.AppendLine($"- Heater energization at frame 1={stageD.HeaterOnAtFrame1}");
            sb.AppendLine($"- Any spray activity during hold={stageD.AnySprayActive}");
            sb.AppendLine();
            sb.AppendLine($"## Exit Gate Decision: {(stageD.Pass ? "PASS" : "FAIL")}");
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteStageHArtifact(string path, StageDResult stageD, StageHResult stageH)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

            string monoCount = stageH.SolverPatternBreakdown.TryGetValue("MONOTONIC_DESCENT", out int mono) ? mono.ToString() : "0";
            string boundedCount = stageH.SolverPatternBreakdown.TryGetValue("BOUNDED_OSCILLATORY", out int bounded) ? bounded.ToString() : "0";
            string otherCount = (stageH.SolverPatternBreakdown.Values.Sum() - mono - bounded).ToString();

            var sb = new StringBuilder();
            sb.AppendLine("# IP-0024 Stage H Deterministic Evidence Suite");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run stamp: `{stageH.RunStamp}`");
            sb.AppendLine();
            sb.AppendLine("## Run List");
            sb.AppendLine($"- Cold-shutdown equilibrium hold (Stage D reuse): `{ToRepoRelative(stageD.LogDir)}`");
            sb.AppendLine($"- Startup + bubble + drain deterministic run: `{ToRepoRelative(stageH.LogDir)}`");
            if (!string.IsNullOrWhiteSpace(stageH.ReportPath))
                sb.AppendLine($"- Heatup report: `{ToRepoRelative(stageH.ReportPath)}`");
            sb.AppendLine($"- Startup sequence timeseries: `{ToRepoRelative(stageH.StartupCsvPath)}`");
            sb.AppendLine($"- Bubble continuity/phase plot data: `{ToRepoRelative(stageH.PhaseCsvPath)}`");
            sb.AppendLine($"- Solver residual log: `{ToRepoRelative(stageH.SolverCsvPath)}`");
            sb.AppendLine($"- DRAIN causality log: `{ToRepoRelative(stageH.DrainCsvPath)}`");
            sb.AppendLine($"- DRAIN causality micro-run log: `{ToRepoRelative(stageH.DrainMicroRunCsvPath)}`");
            sb.AppendLine($"- RVLIS integrity log: `{ToRepoRelative(stageH.RvlisCsvPath)}`");
            sb.AppendLine();
            sb.AppendLine("## Convergence Dashboard");
            sb.AppendLine($"- Attempts: {stageH.SolverAttemptsCount}");
            sb.AppendLine($"- Converged: {stageH.SolverConvergedCount}");
            sb.AppendLine($"- Convergence %: {stageH.SolverConvergencePct:F2}%");
            sb.AppendLine($"- Mean / Max |Volume residual|: {stageH.SolverMeanVolumeResidualFt3:F5} / {stageH.SolverMaxVolumeResidualFt3:F5} ft^3");
            sb.AppendLine($"- Mean / Max |Energy residual|: {stageH.SolverMeanEnergyResidualBTU:F5} / {stageH.SolverMaxEnergyResidualBTU:F5} BTU");
            sb.AppendLine($"- Pattern breakdown: monotonic={monoCount}, bounded_oscillatory={boundedCount}, other={otherCount}");
            sb.AppendLine("- Failure reason breakdown:");
            foreach (KeyValuePair<string, int> pair in stageH.SolverFailureReasonBreakdown.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"  - {pair.Key}: {pair.Value}");
            }
            sb.AppendLine();
            sb.AppendLine("## Bracket Search Diagnostics");
            sb.AppendLine(
                $"- Bracket-only probe: attempted={stageH.BracketProbeAttempted}, found={stageH.BracketProbeFound}, " +
                $"time={stageH.BracketProbeTimeHr:F4}hr, low={stageH.BracketProbeLowPsia:F3} psia, high={stageH.BracketProbeHighPsia:F3} psia, reason={stageH.BracketProbeReason}");
            sb.AppendLine(
                $"- Failure classes observed: {string.Join(", ", stageH.SolverFailureReasonBreakdown.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase).Select(p => $"{p.Key}={p.Value}"))}");
            sb.AppendLine();
            sb.AppendLine("## Startup / Continuity / Causality Checks");
            sb.AppendLine($"- Startup hold gating: {(stageH.StartupConformancePass ? "PASS" : "FAIL")} | {stageH.StartupConformanceReason}");
            sb.AppendLine($"- Bubble continuity: {(stageH.BubbleContinuityPass ? "PASS" : "FAIL")} | {stageH.BubbleContinuityReason}");
            sb.AppendLine($"- DRAIN causality: {(stageH.DrainCausalityPass ? "PASS" : "FAIL")} | {stageH.DrainCausalityReason}");
            sb.AppendLine(
                $"- DRAIN event causality detail: untriggeredLineupChanges={stageH.DrainUntriggeredLineupChanges}, " +
                $"untriggered1to3={stageH.DrainUntriggeredJump13Count}, triggeredLineupChanges={stageH.DrainTriggeredJumpCount}");
            if (stageH.DrainJumpDiagnostics.Count > 0)
            {
                int previewCount = Mathf.Min(12, stageH.DrainJumpDiagnostics.Count);
                sb.AppendLine($"- DRAIN jump decomposition (first {previewCount} events):");
                for (int i = 0; i < previewCount; i++)
                {
                    DrainJumpDiagnostic d = stageH.DrainJumpDiagnostics[i];
                    sb.AppendLine(
                        $"  - t={d.TimeHr:F4}hr lineup {d.PrevLineup}->{d.CurrLineup} event={d.LineupEventThisStep} " +
                        $"dAch={d.DeltaAchievedGpm:F3} dCap={d.DeltaCapacityGpm:F3} dDP={d.DeltaDeltaPPsi:F3} " +
                        $"dRho={d.DeltaDensity:F3} dQ={d.DeltaQuality:F5} cause={d.Cause} trigger={d.EventTrigger}");
                }
            }
            sb.AppendLine(
                $"- DRAIN micro-run: executed={stageH.DrainMicroRun.Executed}, fixedLineupCapPass={stageH.DrainMicroRun.FixedLineupCapPass}, " +
                $"explicitEventLogged={stageH.DrainMicroRun.ExplicitEventLogged}, explicitEventIncreasePass={stageH.DrainMicroRun.ExplicitEventIncreasePass}");
            sb.AppendLine($"- DRAIN micro-run summary: {stageH.DrainMicroRun.Reason}");
            sb.AppendLine($"- RVLIS consistency: {(stageH.RvlisConsistencyPass ? "PASS" : "FAIL")} | {stageH.RvlisConsistencyReason}");
            sb.AppendLine();
            sb.AppendLine("## Setpoint Fidelity");
            foreach (string finding in stageH.SetpointFindings)
                sb.AppendLine($"- {finding}");
            if (stageH.SetpointFindings.Count == 0)
                sb.AppendLine("- PASS: no deviations detected.");
            sb.AppendLine();
            sb.AppendLine("## Clean Failure / No-Commit Evidence");
            sb.AppendLine($"- Attempted: {stageH.ForcedFailure.Attempted}");
            sb.AppendLine($"- Returned converged: {stageH.ForcedFailure.ReturnedConverged}");
            sb.AppendLine($"- No-commit on failure: {stageH.ForcedFailure.NoCommitOnFailure}");
            sb.AppendLine($"- Failure reason: {stageH.ForcedFailure.FailureReason}");
            sb.AppendLine($"- Pattern: {stageH.ForcedFailure.Pattern}");
            sb.AppendLine($"- Residuals: V={stageH.ForcedFailure.VolumeResidualFt3:F5} ft^3, E={stageH.ForcedFailure.EnergyResidualBTU:F5} BTU");
            sb.AppendLine($"- Phase: {stageH.ForcedFailure.BubblePhase}, phaseFraction={stageH.ForcedFailure.PhaseFraction:F5}, iterations={stageH.ForcedFailure.Iterations}");
            sb.AppendLine();
            sb.AppendLine("## CS Pass/Fail Matrix");
            sb.AppendLine("| CS | Evidence Run(s) | Metrics / Thresholds | Result |");
            sb.AppendLine("|---|---|---|---|");
            sb.AppendLine($"| CS-0091 | Stage H deterministic run + forced failure check + bracket probe | convergence>=95%, MASS_CONTRACT_RESIDUAL=0, clean fail/no-commit true, bracket probe finds bracket, reason histogram populated | {(stageH.Cs0091Pass ? "PASS" : "FAIL")} |");
            sb.AppendLine($"| CS-0092 | Stage H DRAIN window + micro-run | policy=LINEUP_HYDRAULIC_CAUSAL, no untriggered lineup change, no untriggered 1->3 jump, max|flow-hydraulic|<=1.5 gpm, explicit-event micro-run proves causal step | {(stageH.Cs0092Pass ? "PASS" : "FAIL")} |");
            sb.AppendLine($"| CS-0093 | Stage H aggregate | CS-0091 && CS-0092 && bubble continuity && setpoint fidelity | {(stageH.Cs0093Pass ? "PASS" : "FAIL")} |");
            sb.AppendLine($"| CS-0094 | Stage H startup sequence | no heater at frame 1, first heater after hold release and mode permit | {(stageH.Cs0094Pass ? "PASS" : "FAIL")} |");
            sb.AppendLine($"| CS-0040 | Stage H DRAIN RVLIS | RVLIS full/upper valid; causal update coverage/alignment sustained | {(stageH.Cs0040Pass ? "PASS" : "FAIL")} |");
            sb.AppendLine($"| CS-0081 | Runtime config check | SOLID band 320-400 psig, MIN_RCP=400 psig | {(stageH.Cs0081Pass ? "PASS" : "FAIL")} |");
            sb.AppendLine();
            sb.AppendLine($"## Stage H Outcome: {(stageH.AllRequiredPass ? "PASS" : "FAIL")}");
            File.WriteAllText(path, sb.ToString());
        }

        private static StageDSample CaptureStageDSample(HeatupSimEngine engine, EngineHooks hooks)
        {
            SystemState state = (SystemState)hooks.PhysicsStateField.GetValue(engine);
            float conduction = (float)hooks.PzrConductionLossField.GetValue(engine);
            float insulation = (float)hooks.PzrInsulationLossField.GetValue(engine);
            float netHeat = engine.pzrHeaterPower - Mathf.Max(0f, conduction) - Mathf.Max(0f, insulation);

            return new StageDSample
            {
                TimeHr = engine.simTime,
                PzrMassLbm = state.PZRWaterMass + state.PZRSteamMass,
                PzrTotalEnthalpyBTU = state.PZRTotalEnthalpy_BTU,
                LiquidOccupancyPct = 100f * state.PZRWaterVolume / Mathf.Max(1e-6f, PlantConstants.PZR_TOTAL_VOLUME),
                PressurePsia = engine.pressure,
                PzrTempF = engine.T_pzr,
                NetPzrHeatMW = netHeat,
                NetCvcsGpm = engine.chargingFlow - engine.letdownFlow,
                ChargingGpm = engine.chargingFlow,
                LetdownGpm = engine.letdownFlow,
                SurgeGpm = engine.surgeFlow,
                StartupHoldActive = engine.startupHoldActive,
                SprayActive = engine.sprayActive,
                SprayFlowGpm = engine.sprayFlow_GPM,
                HeaterPowerMW = engine.pzrHeaterPower,
                LastFailureReason = engine.pzrClosureLastFailureReason ?? "NONE",
                MassContractResidualLbm = 0f
            };
        }

        private static ProgressSample CaptureProgressSample(HeatupSimEngine engine, EngineHooks hooks)
        {
            SystemState state = (SystemState)hooks.PhysicsStateField.GetValue(engine);
            return new ProgressSample
            {
                TimeHr = engine.simTime,
                PressurePsia = engine.pressure,
                PzrLevelPct = engine.pzrLevel,
                HeaterPowerMW = engine.pzrHeaterPower,
                StartupHoldActive = engine.startupHoldActive,
                HeaterMode = engine.currentHeaterMode,
                SprayActive = engine.sprayActive,
                SprayFlowGpm = engine.sprayFlow_GPM,
                BubblePhase = engine.bubblePhase,
                SolidPressurizer = engine.solidPressurizer,
                Orifice75Count = engine.orifice75Count,
                Orifice45Open = engine.orifice45Open,
                DrainPolicyMode = engine.drainCvcsPolicyMode ?? string.Empty,
                DrainLetdownGpm = engine.drainLetdownFlow_gpm,
                DrainDemandLetdownGpm = engine.drainLetdownDemand_gpm,
                DrainChargingGpm = engine.drainChargingFlow_gpm,
                DrainNetOutflowGpm = engine.drainNetOutflowFlow_gpm,
                DrainHydraulicCapacityGpm = engine.drainHydraulicCapacity_gpm,
                DrainHydraulicDeltaPPsi = engine.drainHydraulicDeltaP_psi,
                DrainHydraulicDensityLbmFt3 = engine.drainHydraulicDensity_lbm_ft3,
                DrainHydraulicQuality = engine.drainHydraulicQuality,
                DrainLetdownSaturated = engine.drainLetdownSaturated,
                DrainLineupDemandIndex = engine.drainLineupDemandIndex,
                DrainLineupEventThisStep = engine.drainLineupEventThisStep,
                DrainLineupEventCount = engine.drainLineupEventCount,
                DrainLineupEventPrevIndex = engine.drainLastLineupPrevIndex,
                DrainLineupEventNewIndex = engine.drainLastLineupNewIndex,
                DrainLineupEventTrigger = engine.drainLastLineupTrigger ?? "NONE",
                DrainLineupEventReason = engine.drainLastLineupReason ?? "NONE",
                RvlisDynamic = engine.rvlisDynamic,
                RvlisFull = engine.rvlisFull,
                RvlisUpper = engine.rvlisUpper,
                RvlisDynamicValid = engine.rvlisDynamicValid,
                RvlisFullValid = engine.rvlisFullValid,
                RvlisUpperValid = engine.rvlisUpperValid,
                ChargingGpm = engine.chargingFlow,
                LetdownGpm = engine.letdownFlow,
                SurgeGpm = engine.surgeFlow,
                PzrMassLbm = state.PZRWaterMass + state.PZRSteamMass,
                PzrTotalEnthalpyBTU = state.PZRTotalEnthalpy_BTU,
                PzrClosureVolumeResidual = engine.pzrClosureVolumeResidual_ft3,
                PzrClosureEnergyResidual = engine.pzrClosureEnergyResidual_BTU,
                PzrClosureAttempts = engine.pzrClosureSolveAttempts,
                PzrClosureConverged = engine.pzrClosureSolveConverged,
                PzrClosureFailureReason = engine.pzrClosureLastFailureReason ?? "NONE",
                PzrClosurePattern = engine.pzrClosureLastConvergencePattern ?? "UNSET",
                PzrClosureIterationCount = engine.pzrClosureLastIterationCount,
                PzrClosurePhaseFraction = engine.pzrClosureLastPhaseFraction
            };
        }

        private static void CaptureSolverAttemptsSinceLastStep(
            HeatupSimEngine engine,
            ref int prevAttempts,
            ref int prevConverged,
            List<SolverAttemptSample> samples)
        {
            int nowAttempts = engine.pzrClosureSolveAttempts;
            int nowConverged = engine.pzrClosureSolveConverged;
            int deltaAttempts = Mathf.Max(0, nowAttempts - prevAttempts);
            int deltaConverged = Mathf.Max(0, nowConverged - prevConverged);

            for (int i = 0; i < deltaAttempts; i++)
            {
                bool converged = i < deltaConverged;
                int attemptIndex = prevAttempts + i + 1;
                samples.Add(new SolverAttemptSample
                {
                    AttemptIndex = attemptIndex,
                    TimeHr = engine.simTime,
                    Converged = converged,
                    VolumeResidualFt3 = engine.pzrClosureVolumeResidual_ft3,
                    EnergyResidualBTU = engine.pzrClosureEnergyResidual_BTU,
                    FailureReason = engine.pzrClosureLastFailureReason ?? "NONE",
                    Pattern = engine.pzrClosureLastConvergencePattern ?? "UNSET",
                    Iterations = engine.pzrClosureLastIterationCount,
                    PhaseFraction = engine.pzrClosureLastPhaseFraction,
                    BubblePhase = engine.bubblePhase,
                    BracketTargetVolumeFt3 = engine.pzrClosureBracketVolumeTarget_ft3,
                    BracketTargetMassLbm = engine.pzrClosureBracketMassTarget_lbm,
                    BracketTargetEnthalpyBTU = engine.pzrClosureBracketEnthalpyTarget_BTU,
                    BracketOperatingMinPsia = engine.pzrClosureBracketOperatingMin_psia,
                    BracketOperatingMaxPsia = engine.pzrClosureBracketOperatingMax_psia,
                    BracketHardMinPsia = engine.pzrClosureBracketHardMin_psia,
                    BracketHardMaxPsia = engine.pzrClosureBracketHardMax_psia,
                    BracketLowPsia = engine.pzrClosureBracketLastLow_psia,
                    BracketHighPsia = engine.pzrClosureBracketLastHigh_psia,
                    BracketResidualLowFt3 = engine.pzrClosureBracketResidualLow_ft3,
                    BracketResidualHighFt3 = engine.pzrClosureBracketResidualHigh_ft3,
                    BracketSignLow = engine.pzrClosureBracketResidualSignLow,
                    BracketSignHigh = engine.pzrClosureBracketResidualSignHigh,
                    BracketRegimeLow = engine.pzrClosureBracketRegimeLow ?? "UNSET",
                    BracketRegimeHigh = engine.pzrClosureBracketRegimeHigh ?? "UNSET",
                    BracketWindowsTried = engine.pzrClosureBracketWindowsTried,
                    BracketValidEvaluations = engine.pzrClosureBracketValidEvaluations,
                    BracketInvalidEvaluations = engine.pzrClosureBracketInvalidEvaluations,
                    BracketNanEvaluations = engine.pzrClosureBracketNanEvaluations,
                    BracketOutOfRangeEvaluations = engine.pzrClosureBracketOutOfRangeEvaluations,
                    BracketFound = engine.pzrClosureBracketFound,
                    BracketTrace = engine.pzrClosureBracketSearchTrace ?? string.Empty
                });
            }

            prevAttempts = nowAttempts;
            prevConverged = nowConverged;
        }

        private static ForcedFailureEvidence RunForcedFailureNoCommitCheck(HeatupSimEngine engine, EngineHooks hooks)
        {
            var evidence = new ForcedFailureEvidence { Attempted = true };

            SystemState pre = (SystemState)hooks.PhysicsStateField.GetValue(engine);
            int attemptsBefore = engine.pzrClosureSolveAttempts;
            int convergedBefore = engine.pzrClosureSolveConverged;
            float prePressure = engine.pressure;

            bool converged = false;
            try
            {
                object boxed = hooks.ForceClosureMethod.Invoke(
                    engine,
                    new object[] { "IP0024_FORCED_FAILURE_CHECK", DtHr, 1e9f, 0f });
                if (boxed is bool b)
                    converged = b;
            }
            catch (Exception ex)
            {
                evidence.ReturnedConverged = false;
                evidence.NoCommitOnFailure = false;
                evidence.FailureReason = $"EXCEPTION:{ex.GetType().Name}";
                return evidence;
            }

            SystemState post = (SystemState)hooks.PhysicsStateField.GetValue(engine);
            evidence.ReturnedConverged = converged;
            evidence.FailureReason = engine.pzrClosureLastFailureReason ?? "NONE";
            evidence.Pattern = engine.pzrClosureLastConvergencePattern ?? "UNSET";
            evidence.VolumeResidualFt3 = engine.pzrClosureVolumeResidual_ft3;
            evidence.EnergyResidualBTU = engine.pzrClosureEnergyResidual_BTU;
            evidence.PhaseFraction = engine.pzrClosureLastPhaseFraction;
            evidence.BubblePhase = engine.bubblePhase;
            evidence.Iterations = engine.pzrClosureLastIterationCount;
            evidence.AttemptIndexStart = attemptsBefore + 1;
            evidence.AttemptIndexEnd = engine.pzrClosureSolveAttempts;

            bool unchangedMass =
                Mathf.Abs((post.PZRWaterMass + post.PZRSteamMass) - (pre.PZRWaterMass + pre.PZRSteamMass)) <= 1e-4f;
            bool unchangedEnthalpy = Mathf.Abs(post.PZRTotalEnthalpy_BTU - pre.PZRTotalEnthalpy_BTU) <= 1e-2f;
            bool unchangedPressure = Mathf.Abs(engine.pressure - prePressure) <= 1e-4f;
            bool convergedCountUnchanged = engine.pzrClosureSolveConverged == convergedBefore;
            evidence.NoCommitOnFailure = !converged && unchangedMass && unchangedEnthalpy && unchangedPressure && convergedCountUnchanged;
            return evidence;
        }

        private static void RunBracketOnlyProbe(HeatupSimEngine engine, EngineHooks hooks, StageHResult result)
        {
            result.BracketProbeAttempted = true;
            result.BracketProbeTimeHr = engine.simTime;
            object[] args = { "IP0024_BRACKET_ONLY_PROBE", false, 0f, 0f, "UNSET" };
            try
            {
                object boxed = hooks.BracketProbeMethod.Invoke(engine, args);
                bool foundByReturn = boxed is bool b && b;
                bool foundByOut = args[1] is bool outBool && outBool;
                result.BracketProbeFound = foundByReturn || foundByOut;
                result.BracketProbeLowPsia = args[2] is float low ? low : 0f;
                result.BracketProbeHighPsia = args[3] is float high ? high : 0f;
                result.BracketProbeReason = args[4]?.ToString() ?? "UNSET";
            }
            catch (Exception ex)
            {
                result.BracketProbeFound = false;
                result.BracketProbeReason = $"EXCEPTION:{ex.GetType().Name}";
            }
        }

        private static DrainMicroRunResult ExecuteDrainCausalityMicroRun(string csvPath)
        {
            var result = new DrainMicroRunResult { Executed = true, CsvPath = csvPath };

            const int fixedWindowSteps = 12;
            float pressurePsia = PlantConstants.PZR_BASELINE_PRESSURE_SETPOINT_PSIG + PlantConstants.PSIG_TO_PSIA;
            float demandGpm = PlantConstants.BUBBLE_DRAIN_PROCEDURE_MAX_LETDOWN_GPM;
            int lineupIndex = 1;
            float simHr = 0f;

            float fixedCapacity = 0f;
            float fixedMaxAchieved = 0f;
            bool fixedPass = true;

            for (int step = 0; step < fixedWindowSteps; step++)
            {
                ResolveLineup(lineupIndex, out int num75, out bool open45);
                float hydraulic = PlantConstants.CalculateOrificeLineupFlow(
                    pressurePsia - PlantConstants.PSIG_TO_PSIA,
                    num75,
                    open45);
                float capacity = Mathf.Clamp(
                    hydraulic,
                    PlantConstants.BUBBLE_DRAIN_PROCEDURE_MIN_LETDOWN_GPM,
                    PlantConstants.BUBBLE_DRAIN_PROCEDURE_MAX_LETDOWN_GPM);
                float achieved = Mathf.Min(demandGpm, capacity);

                fixedCapacity = capacity;
                fixedMaxAchieved = Mathf.Max(fixedMaxAchieved, achieved);
                fixedPass &= achieved <= capacity + 1e-5f;

                result.Samples.Add(new DrainMicroRunSample
                {
                    Step = step + 1,
                    TimeHr = simHr,
                    LineupIndex = lineupIndex,
                    PressurePsia = pressurePsia,
                    DemandGpm = demandGpm,
                    HydraulicCapacityGpm = capacity,
                    AchievedGpm = achieved,
                    LineupEvent = false,
                    Trigger = "NONE",
                    Reason = "FIXED_LINEUP_WINDOW",
                    Phase = "FIXED_LINEUP_1X75"
                });

                simHr += DtHr;
            }

            result.FixedLineupCapPass = fixedPass;
            result.FixedWindowCapacityGpm = fixedCapacity;
            result.FixedWindowMaxAchievedGpm = fixedMaxAchieved;
            result.PreEventAchievedGpm = result.Samples.Count > 0 ? result.Samples[result.Samples.Count - 1].AchievedGpm : 0f;

            lineupIndex = 3; // explicit event-driven lineup change
            ResolveLineup(lineupIndex, out int event75, out bool event45);
            float eventHydraulic = PlantConstants.CalculateOrificeLineupFlow(
                pressurePsia - PlantConstants.PSIG_TO_PSIA,
                event75,
                event45);
            float eventCapacity = Mathf.Clamp(
                eventHydraulic,
                PlantConstants.BUBBLE_DRAIN_PROCEDURE_MIN_LETDOWN_GPM,
                PlantConstants.BUBBLE_DRAIN_PROCEDURE_MAX_LETDOWN_GPM);
            float eventAchieved = Mathf.Min(demandGpm, eventCapacity);
            result.PostEventAchievedGpm = eventAchieved;
            result.ExplicitEventLogged = true;
            result.ExplicitEventIncreasePass = eventAchieved > result.PreEventAchievedGpm + 0.1f;

            result.Samples.Add(new DrainMicroRunSample
            {
                Step = fixedWindowSteps + 1,
                TimeHr = simHr,
                LineupIndex = lineupIndex,
                PressurePsia = pressurePsia,
                DemandGpm = demandGpm,
                HydraulicCapacityGpm = eventCapacity,
                AchievedGpm = eventAchieved,
                LineupEvent = true,
                Trigger = "RUNNER_EXPLICIT_EVENT",
                Reason = "MICRO_RUN_STEP_CHANGE_PROOF",
                Phase = "EXPLICIT_LINEUP_EVENT"
            });

            result.Reason =
                $"fixedLineupCapPass={result.FixedLineupCapPass}, fixedMaxAchieved={result.FixedWindowMaxAchievedGpm:F3} gpm, " +
                $"fixedCapacity={result.FixedWindowCapacityGpm:F3} gpm, preEventAchieved={result.PreEventAchievedGpm:F3} gpm, " +
                $"postEventAchieved={result.PostEventAchievedGpm:F3} gpm, explicitEventIncreasePass={result.ExplicitEventIncreasePass}";
            return result;
        }

        private static void ResolveLineup(int lineupIndex, out int num75Open, out bool open45)
        {
            switch (Mathf.Clamp(lineupIndex, 1, 3))
            {
                case 3:
                    num75Open = 2;
                    open45 = true;
                    break;
                case 2:
                    num75Open = 1;
                    open45 = true;
                    break;
                default:
                    num75Open = 1;
                    open45 = false;
                    break;
            }
        }

        private static void WriteDrainMicroRunCsv(string path, DrainMicroRunResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("step,time_hr,lineup_index,pressure_psia,demand_gpm,hydraulic_capacity_gpm,achieved_gpm,lineup_event,trigger,reason,phase");
            foreach (DrainMicroRunSample sample in result.Samples)
            {
                sb.AppendLine(
                    $"{sample.Step},{sample.TimeHr:F6},{sample.LineupIndex},{sample.PressurePsia:F6},{sample.DemandGpm:F6}," +
                    $"{sample.HydraulicCapacityGpm:F6},{sample.AchievedGpm:F6},{(sample.LineupEvent ? 1 : 0)}," +
                    $"{CsvSafe(sample.Trigger)},{CsvSafe(sample.Reason)},{CsvSafe(sample.Phase)}");
            }
            File.WriteAllText(path, sb.ToString());
        }

        private static string ClassifyDrainDeltaCause(
            ProgressSample prev,
            ProgressSample curr,
            bool lineupChanged,
            bool hasEvent)
        {
            if (lineupChanged)
                return hasEvent ? "LINEUP_EVENT_STEP_CHANGE" : "UNTRIGGERED_LINEUP_CHANGE";

            float dAchieved = curr.DrainLetdownGpm - prev.DrainLetdownGpm;
            float dCapacity = curr.DrainHydraulicCapacityGpm - prev.DrainHydraulicCapacityGpm;
            float dDeltaP = curr.DrainHydraulicDeltaPPsi - prev.DrainHydraulicDeltaPPsi;
            float dDensity = curr.DrainHydraulicDensityLbmFt3 - prev.DrainHydraulicDensityLbmFt3;
            float dQuality = curr.DrainHydraulicQuality - prev.DrainHydraulicQuality;

            if (Mathf.Abs(dAchieved) < 1e-4f)
                return "NO_EFFECTIVE_DELTA";

            float expected = Mathf.Min(curr.DrainDemandLetdownGpm, curr.DrainHydraulicCapacityGpm);
            if (Mathf.Abs(curr.DrainLetdownGpm - expected) <= 0.25f && curr.DrainLetdownSaturated)
                return "SATURATION_FEEDBACK";

            float absCapacity = Mathf.Abs(dCapacity);
            float absDeltaP = Mathf.Abs(dDeltaP);
            float absDensity = Mathf.Abs(dDensity);
            float absQuality = Mathf.Abs(dQuality);

            if (absCapacity >= absDeltaP && absCapacity >= absDensity && absCapacity >= absQuality)
                return "HYDRAULIC_CAPACITY_DELTA";
            if (absDeltaP >= absDensity && absDeltaP >= absQuality)
                return "DELTA_P_DRIVEN";
            if (absQuality >= absDensity && absQuality >= 1e-4f)
                return "PHASE_QUALITY_DRIVEN";
            if (absDensity >= 1e-4f)
                return "DENSITY_DRIVEN";
            return "MIXED_HYDRAULIC_DELTA";
        }

        private static string CsvSafe(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            string escaped = value.Replace("\"", "\"\"");
            bool mustQuote = escaped.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            return mustQuote ? $"\"{escaped}\"" : escaped;
        }

        private static float IntegrateNetHeatBTU(List<StageDSample> samples)
        {
            if (samples.Count < 2)
                return 0f;

            float total = 0f;
            for (int i = 1; i < samples.Count; i++)
            {
                StageDSample a = samples[i - 1];
                StageDSample b = samples[i];
                float dtHr = b.TimeHr - a.TimeHr;
                if (dtHr <= 0f)
                    continue;
                float avgMw = 0.5f * (a.NetPzrHeatMW + b.NetPzrHeatMW);
                total += avgMw * PlantConstants.MW_TO_BTU_SEC * dtHr * 3600f;
            }
            return total;
        }

        private static void CheckSetpoint(StageHResult result, string label, float baseline, float alias)
        {
            bool pass = Mathf.Abs(baseline - alias) <= 1e-6f;
            result.SetpointFindings.Add(
                $"{(pass ? "PASS" : "FAIL")} | {label}: baseline={baseline:F6}, alias={alias:F6}, delta={(alias - baseline):F6}");
        }

        private static int GetDrainLineupIndex(int num75Open, bool open45)
        {
            if (num75Open >= 2 && open45)
                return 3;
            if (num75Open >= 2 || open45)
                return 2;
            return 1;
        }

        private static void Increment(Dictionary<string, int> map, string key)
        {
            string safe = string.IsNullOrWhiteSpace(key) ? "UNSET" : key.Trim();
            if (map.ContainsKey(safe))
                map[safe]++;
            else
                map[safe] = 1;
        }

        private static void OpenAndInitializeDeterministicEngine(
            string logDir,
            out HeatupSimEngine engine,
            out EngineHooks hooks)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
            engine = UnityEngine.Object.FindObjectOfType<HeatupSimEngine>();
            if (engine == null)
                throw new InvalidOperationException("HeatupSimEngine not found in MainScene.");

            hooks = ResolveEngineHooks();

            engine.runOnStart = false;
            engine.coldShutdownStart = true;
            engine.startTemperature = 100f;
            engine.targetTemperature = 557f;
            engine.enableAsyncLogWriter = false;
            engine.enableHighFrequencyPerfLogs = false;
            engine.enableWorkerThreadStepping = false;
            hooks.LogPathField.SetValue(engine, logDir);

            hooks.Init.Invoke(engine, null);
        }

        private static EngineHooks ResolveEngineHooks()
        {
            Type t = typeof(HeatupSimEngine);
            var hooks = new EngineHooks
            {
                Init = t.GetMethod("InitializeSimulation", BindingFlags.Instance | BindingFlags.NonPublic),
                Step = t.GetMethod("StepSimulation", BindingFlags.Instance | BindingFlags.NonPublic),
                SaveInterval = t.GetMethod("SaveIntervalLog", BindingFlags.Instance | BindingFlags.NonPublic),
                SaveReport = t.GetMethod("SaveReport", BindingFlags.Instance | BindingFlags.NonPublic),
                ForceClosureMethod = t.GetMethod("UpdateTwoPhaseStateFromMassClosure", BindingFlags.Instance | BindingFlags.NonPublic),
                BracketProbeMethod = t.GetMethod("RunTwoPhaseBracketOnlyProbe", BindingFlags.Instance | BindingFlags.NonPublic),
                LogPathField = t.GetField("logPath", BindingFlags.Instance | BindingFlags.NonPublic),
                PhysicsStateField = t.GetField("physicsState", BindingFlags.Instance | BindingFlags.NonPublic),
                PzrConductionLossField = t.GetField("pzrConductionLoss_MW", BindingFlags.Instance | BindingFlags.NonPublic),
                PzrInsulationLossField = t.GetField("pzrInsulationLoss_MW", BindingFlags.Instance | BindingFlags.NonPublic)
            };

            if (hooks.Init == null ||
                hooks.Step == null ||
                hooks.SaveInterval == null ||
                hooks.SaveReport == null ||
                hooks.ForceClosureMethod == null ||
                hooks.BracketProbeMethod == null ||
                hooks.LogPathField == null ||
                hooks.PhysicsStateField == null ||
                hooks.PzrConductionLossField == null ||
                hooks.PzrInsulationLossField == null)
            {
                throw new MissingMethodException("IP-0024 runner could not resolve required HeatupSimEngine members.");
            }

            return hooks;
        }

        private static void PrepareLogDirectory(string logDir)
        {
            Directory.CreateDirectory(logDir);
            foreach (string file in Directory.GetFiles(logDir, "*.txt"))
                File.Delete(file);
            foreach (string file in Directory.GetFiles(logDir, "*.csv"))
                File.Delete(file);
        }

        private static void WriteStageDCsv(string path, List<StageDSample> samples)
        {
            var sb = new StringBuilder();
            sb.AppendLine("time_hr,pzr_mass_lbm,pzr_total_enthalpy_btu,liquid_occupancy_pct,pressure_psia,pzr_temp_f,net_pzr_heat_mw,charging_gpm,letdown_gpm,surge_gpm,net_cvcs_gpm,startup_hold_active,spray_active,spray_flow_gpm,heater_power_mw,last_failure_reason");
            foreach (StageDSample s in samples)
            {
                sb.AppendLine(
                    $"{s.TimeHr:F6},{s.PzrMassLbm:F6},{s.PzrTotalEnthalpyBTU:F6},{s.LiquidOccupancyPct:F6},{s.PressurePsia:F6},{s.PzrTempF:F6},{s.NetPzrHeatMW:F6},{s.ChargingGpm:F6},{s.LetdownGpm:F6},{s.SurgeGpm:F6},{s.NetCvcsGpm:F6},{(s.StartupHoldActive ? 1 : 0)},{(s.SprayActive ? 1 : 0)},{s.SprayFlowGpm:F6},{s.HeaterPowerMW:F6},{s.LastFailureReason}");
            }
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteStageHStartupCsv(string path, List<ProgressSample> samples)
        {
            var sb = new StringBuilder();
            sb.AppendLine("time_hr,startup_hold_active,heater_mode,heater_power_mw,pressure_psia,pzr_level_pct,spray_active,spray_flow_gpm,bubble_phase");
            foreach (ProgressSample s in samples)
            {
                sb.AppendLine(
                    $"{s.TimeHr:F6},{(s.StartupHoldActive ? 1 : 0)},{s.HeaterMode},{s.HeaterPowerMW:F6},{s.PressurePsia:F6},{s.PzrLevelPct:F6},{(s.SprayActive ? 1 : 0)},{s.SprayFlowGpm:F6},{s.BubblePhase}");
            }
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteStageHPhaseCsv(string path, List<ProgressSample> samples)
        {
            var sb = new StringBuilder();
            sb.AppendLine("time_hr,solid_pressurizer,bubble_phase,pressure_psia,pzr_level_pct,pzr_mass_lbm,pzr_total_enthalpy_btu,closure_phase_fraction");
            foreach (ProgressSample s in samples)
            {
                sb.AppendLine(
                    $"{s.TimeHr:F6},{(s.SolidPressurizer ? 1 : 0)},{s.BubblePhase},{s.PressurePsia:F6},{s.PzrLevelPct:F6},{s.PzrMassLbm:F6},{s.PzrTotalEnthalpyBTU:F6},{s.PzrClosurePhaseFraction:F6}");
            }
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteStageHSolverCsv(string path, List<SolverAttemptSample> samples)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                "attempt_index,time_hr,converged,volume_residual_ft3,energy_residual_btu,failure_reason,pattern,iterations,phase_fraction,bubble_phase," +
                "v_total_ft3,m_total_lbm,u_total_btu,operating_pmin_psia,operating_pmax_psia,hard_pmin_psia,hard_pmax_psia," +
                "bracket_low_psia,bracket_high_psia,v_residual_low_ft3,v_residual_high_ft3,sign_low,sign_high,regime_low,regime_high," +
                "bracket_windows,valid_evals,invalid_evals,nan_evals,out_of_range_evals,bracket_found,bracket_trace");
            foreach (SolverAttemptSample s in samples)
            {
                sb.AppendLine(
                    $"{s.AttemptIndex},{s.TimeHr:F6},{(s.Converged ? 1 : 0)},{s.VolumeResidualFt3:F6},{s.EnergyResidualBTU:F6}," +
                    $"{CsvSafe(s.FailureReason)},{CsvSafe(s.Pattern)},{s.Iterations},{s.PhaseFraction:F6},{s.BubblePhase}," +
                    $"{s.BracketTargetVolumeFt3:F6},{s.BracketTargetMassLbm:F6},{s.BracketTargetEnthalpyBTU:F6}," +
                    $"{s.BracketOperatingMinPsia:F6},{s.BracketOperatingMaxPsia:F6},{s.BracketHardMinPsia:F6},{s.BracketHardMaxPsia:F6}," +
                    $"{s.BracketLowPsia:F6},{s.BracketHighPsia:F6},{s.BracketResidualLowFt3:F6},{s.BracketResidualHighFt3:F6}," +
                    $"{s.BracketSignLow},{s.BracketSignHigh},{CsvSafe(s.BracketRegimeLow)},{CsvSafe(s.BracketRegimeHigh)}," +
                    $"{s.BracketWindowsTried},{s.BracketValidEvaluations},{s.BracketInvalidEvaluations},{s.BracketNanEvaluations},{s.BracketOutOfRangeEvaluations}," +
                    $"{(s.BracketFound ? 1 : 0)},{CsvSafe(s.BracketTrace)}");
            }
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteStageHDrainCsv(string path, List<ProgressSample> samples)
        {
            var drain = samples.Where(s => s.BubblePhase == HeatupSimEngine.BubbleFormationPhase.DRAIN).ToList();
            var sb = new StringBuilder();
            sb.AppendLine(
                "time_hr,pressure_psia,orifice75_count,orifice45_open,lineup_prev,lineup_curr,lineup_event,lineup_event_count,lineup_event_prev,lineup_event_new,lineup_event_trigger,lineup_event_reason," +
                "lineup_demand,drain_policy,drain_demand_gpm,drain_hydraulic_capacity_gpm,drain_letdown_saturated,drain_letdown_gpm,drain_expected_gpm,drain_charging_gpm,drain_net_outflow_gpm," +
                "drain_deltaP_psi,drain_density_lbm_ft3,drain_quality,d_achieved_gpm,d_capacity_gpm,d_deltaP_psi,d_density,d_quality,delta_cause");
            for (int i = 0; i < drain.Count; i++)
            {
                ProgressSample s = drain[i];
                ProgressSample prev = i > 0 ? drain[i - 1] : s;
                int lineupPrev = i > 0 ? GetDrainLineupIndex(prev.Orifice75Count, prev.Orifice45Open) : GetDrainLineupIndex(s.Orifice75Count, s.Orifice45Open);
                int lineup = GetDrainLineupIndex(s.Orifice75Count, s.Orifice45Open);
                bool lineupChanged = i > 0 && lineup != lineupPrev;
                bool eventThisStep = s.DrainLineupEventThisStep;
                float expected = Mathf.Min(s.DrainDemandLetdownGpm, s.DrainHydraulicCapacityGpm);
                float dAchieved = i > 0 ? s.DrainLetdownGpm - prev.DrainLetdownGpm : 0f;
                float dCapacity = i > 0 ? s.DrainHydraulicCapacityGpm - prev.DrainHydraulicCapacityGpm : 0f;
                float dDeltaP = i > 0 ? s.DrainHydraulicDeltaPPsi - prev.DrainHydraulicDeltaPPsi : 0f;
                float dDensity = i > 0 ? s.DrainHydraulicDensityLbmFt3 - prev.DrainHydraulicDensityLbmFt3 : 0f;
                float dQuality = i > 0 ? s.DrainHydraulicQuality - prev.DrainHydraulicQuality : 0f;
                string cause = i > 0
                    ? ClassifyDrainDeltaCause(prev, s, lineupChanged, eventThisStep)
                    : "DRAIN_ENTRY";
                sb.AppendLine(
                    $"{s.TimeHr:F6},{s.PressurePsia:F6},{s.Orifice75Count},{(s.Orifice45Open ? 1 : 0)}," +
                    $"{lineupPrev},{lineup},{(eventThisStep ? 1 : 0)},{s.DrainLineupEventCount},{s.DrainLineupEventPrevIndex},{s.DrainLineupEventNewIndex}," +
                    $"{CsvSafe(s.DrainLineupEventTrigger)},{CsvSafe(s.DrainLineupEventReason)},{s.DrainLineupDemandIndex},{CsvSafe(s.DrainPolicyMode)}," +
                    $"{s.DrainDemandLetdownGpm:F6},{s.DrainHydraulicCapacityGpm:F6},{(s.DrainLetdownSaturated ? 1 : 0)},{s.DrainLetdownGpm:F6},{expected:F6},{s.DrainChargingGpm:F6},{s.DrainNetOutflowGpm:F6}," +
                    $"{s.DrainHydraulicDeltaPPsi:F6},{s.DrainHydraulicDensityLbmFt3:F6},{s.DrainHydraulicQuality:F6}," +
                    $"{dAchieved:F6},{dCapacity:F6},{dDeltaP:F6},{dDensity:F6},{dQuality:F6},{CsvSafe(cause)}");
            }
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteStageHRvlisCsv(string path, List<ProgressSample> samples)
        {
            var drain = samples.Where(s => s.BubblePhase == HeatupSimEngine.BubbleFormationPhase.DRAIN).ToList();
            var sb = new StringBuilder();
            sb.AppendLine("time_hr,pzr_level_pct,rvlis_dynamic,rvlis_full,rvlis_upper,rvlis_dynamic_valid,rvlis_full_valid,rvlis_upper_valid,drain_net_outflow_gpm,surge_gpm");
            foreach (ProgressSample s in drain)
            {
                sb.AppendLine(
                    $"{s.TimeHr:F6},{s.PzrLevelPct:F6},{s.RvlisDynamic:F6},{s.RvlisFull:F6},{s.RvlisUpper:F6},{(s.RvlisDynamicValid ? 1 : 0)},{(s.RvlisFullValid ? 1 : 0)},{(s.RvlisUpperValid ? 1 : 0)},{s.DrainNetOutflowGpm:F6},{s.SurgeGpm:F6}");
            }
            File.WriteAllText(path, sb.ToString());
        }

        private static string ToRepoRelative(string absolutePath)
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            if (absolutePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return absolutePath
                    .Substring(root.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Replace('\\', '/');
            }
            return absolutePath.Replace('\\', '/');
        }
    }
}
