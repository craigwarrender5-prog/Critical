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

namespace Critical.Validation
{
    public static class IP0025StageBRunner
    {
        private const float DtHr = 1f / 360f;
        private const float RunHours = 1.0f;
        private const int MaxSteps = 100000;
        private const int FixedSeed = 250025;

        private const float FloatTolerance = 1e-6f;

        private sealed class Metric
        {
            public string Name = string.Empty;
            public float Tolerance;
            public float MaxError;
            public int WorstStep;
        }

        private sealed class DiscreteMetric
        {
            public string Name = string.Empty;
            public int MismatchCount;
            public int FirstMismatchStep;
        }

        private sealed class StageBResult
        {
            public readonly List<Metric> FloatMetrics = new List<Metric>();
            public readonly List<DiscreteMetric> DiscreteMetrics = new List<DiscreteMetric>();
            public readonly List<string> FidelityCsvRows = new List<string>();

            public bool SnapshotImmutabilityProperties;
            public bool SnapshotNoMutableRuntimeContainers;
            public bool SnapshotObjectReplacementObserved;
            public bool PriorSnapshotValuesStableAfterFurtherStep;
            public bool Pass;
            public string FailureReason = string.Empty;
            public int Steps;
        }

        [MenuItem("Critical/Run IP-0025 Stage B Snapshot Fidelity")]
        public static void RunStageBSnapshotFidelity()
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string runstamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string logDir = Path.Combine(root, "HeatupLogs", $"IP-0025_StageB_SnapshotFidelity_{runstamp}");
            Directory.CreateDirectory(logDir);

            StageBResult result = Execute(root, logDir);

            string fidelityCsv = Path.Combine(logDir, "snapshot_fidelity.csv");
            WriteFidelityCsv(fidelityCsv, result);

            string issuePath = Path.Combine(root, "Governance", "Issues", $"IP-0025_StageB_SnapshotFidelity_{runstamp}.md");
            WriteIssue(issuePath, runstamp, logDir, result);

            Debug.Log($"[IP-0025][StageB] Artifact: {issuePath}");
            Debug.Log($"[IP-0025][StageB] Logs: {logDir}");

            if (!result.Pass)
                throw new Exception($"IP-0025 Stage B snapshot fidelity failed: {result.FailureReason}");
        }

        private static StageBResult Execute(string root, string logDir)
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
                throw new MissingMethodException("IP-0025 Stage B runner missing required HeatupSimEngine methods/fields.");

            ConfigureDeterministicInputs(engine);
            logPathField.SetValue(engine, logDir);
            ModularFeatureFlags.ResetAll();
            ModularFeatureFlags.EnableCoordinatorPath = true; // keep Stage A carrier active.
            initialize.Invoke(engine, null);

            var result = new StageBResult();

            Metric mSimTime = CreateFloatMetric("simTime -> PlantState.TimeHr", FloatTolerance);
            Metric mPressure = CreateFloatMetric("pressure -> PlantState.PressurePsia", FloatTolerance);
            Metric mTavg = CreateFloatMetric("T_avg -> PlantState.TavgF", FloatTolerance);
            Metric mTrcs = CreateFloatMetric("T_rcs -> PlantState.TrcsF", FloatTolerance);
            Metric mPzr = CreateFloatMetric("pzrLevel -> PlantState.PzrLevelPct", FloatTolerance);
            Metric mCharging = CreateFloatMetric("chargingFlow -> PlantState.ChargingFlowGpm", FloatTolerance);
            Metric mLetdown = CreateFloatMetric("letdownFlow -> PlantState.LetdownFlowGpm", FloatTolerance);
            Metric mSurge = CreateFloatMetric("surgeFlow -> PlantState.SurgeFlowGpm", FloatTolerance);
            Metric mHeater = CreateFloatMetric("pzrHeaterPower -> PlantState.PzrHeaterPowerMw", FloatTolerance);
            Metric mReactor = CreateFloatMetric("reactorPower -> PlantState.ReactorPowerMw", FloatTolerance);
            Metric mMassLedger = CreateFloatMetric("primaryMassLedger_lb -> PlantState.PrimaryMassLedgerLb", FloatTolerance);
            Metric mMassBoundary = CreateFloatMetric("primaryMassBoundaryError_lb -> PlantState.PrimaryMassBoundaryErrorLb", FloatTolerance);
            Metric mTotalMass = CreateFloatMetric("totalSystemMass_lbm -> PlantState.TotalSystemMassLb", FloatTolerance);

            DiscreteMetric dPlantMode = CreateDiscreteMetric("plantMode -> PlantState.PlantMode");
            DiscreteMetric dRcp = CreateDiscreteMetric("rcpCount -> PlantState.RcpCount");
            DiscreteMetric dConservation = CreateDiscreteMetric("primaryMassConservationOK -> PlantState.PrimaryMassConservationOk");
            DiscreteMetric dPhase = CreateDiscreteMetric("heatupPhaseDesc -> PlantState.HeatupPhaseDescription");

            int totalSteps = Mathf.CeilToInt(RunHours / DtHr);
            if (totalSteps > MaxSteps)
                throw new InvalidOperationException($"Stage B runner exceeded MaxSteps ({MaxSteps}).");

            result.FidelityCsvRows.Add(
                "step,time_hr,pressure_err,tavg_err,trcs_err,pzr_err,charging_err,letdown_err,surge_err,pzr_heater_err,reactor_power_err,primary_ledger_err,primary_boundary_err,total_mass_err");

            for (int step = 1; step <= totalSteps; step++)
            {
                runStep.Invoke(engine, new object[] { DtHr });
                StepSnapshot snapshot = engine.GetStepSnapshot();
                PlantState state = snapshot.PlantState;

                float eTime = Track(mSimTime, step, engine.simTime, state.TimeHr);
                float ePressure = Track(mPressure, step, engine.pressure, state.PressurePsia);
                float eTavg = Track(mTavg, step, engine.T_avg, state.TavgF);
                float eTrcs = Track(mTrcs, step, engine.T_rcs, state.TrcsF);
                float ePzr = Track(mPzr, step, engine.pzrLevel, state.PzrLevelPct);
                float eCharging = Track(mCharging, step, engine.chargingFlow, state.ChargingFlowGpm);
                float eLetdown = Track(mLetdown, step, engine.letdownFlow, state.LetdownFlowGpm);
                float eSurge = Track(mSurge, step, engine.surgeFlow, state.SurgeFlowGpm);
                float eHeater = Track(mHeater, step, engine.pzrHeaterPower, state.PzrHeaterPowerMw);
                float eReactor = Track(mReactor, step, engine.stageE_PrimaryHeatInput_MW, state.ReactorPowerMw);
                float eMassLedger = Track(mMassLedger, step, engine.primaryMassLedger_lb, state.PrimaryMassLedgerLb);
                float eMassBoundary = Track(mMassBoundary, step, engine.primaryMassBoundaryError_lb, state.PrimaryMassBoundaryErrorLb);
                float eTotalMass = Track(mTotalMass, step, engine.totalSystemMass_lbm, state.TotalSystemMassLb);

                TrackDiscrete(dPlantMode, step, engine.plantMode, state.PlantMode);
                TrackDiscrete(dRcp, step, engine.rcpCount, state.RcpCount);
                TrackDiscrete(dConservation, step, engine.primaryMassConservationOK, state.PrimaryMassConservationOk);
                TrackDiscrete(dPhase, step, engine.heatupPhaseDesc ?? string.Empty, state.HeatupPhaseDescription ?? string.Empty);

                result.FidelityCsvRows.Add(
                    $"{step}," +
                    $"{F(engine.simTime)}," +
                    $"{F(ePressure)}," +
                    $"{F(eTavg)}," +
                    $"{F(eTrcs)}," +
                    $"{F(ePzr)}," +
                    $"{F(eCharging)}," +
                    $"{F(eLetdown)}," +
                    $"{F(eSurge)}," +
                    $"{F(eHeater)}," +
                    $"{F(eReactor)}," +
                    $"{F(eMassLedger)}," +
                    $"{F(eMassBoundary)}," +
                    $"{F(eTotalMass)}");
            }

            result.Steps = totalSteps;
            result.FloatMetrics.AddRange(new[] { mSimTime, mPressure, mTavg, mTrcs, mPzr, mCharging, mLetdown, mSurge, mHeater, mReactor, mMassLedger, mMassBoundary, mTotalMass });
            result.DiscreteMetrics.AddRange(new[] { dPlantMode, dRcp, dConservation, dPhase });

            RunImmutabilityChecks(engine, runStep, result);

            bool floatPass = result.FloatMetrics.TrueForAll(m => m.MaxError <= m.Tolerance);
            bool discretePass = result.DiscreteMetrics.TrueForAll(m => m.MismatchCount == 0);
            bool immutabilityPass = result.SnapshotImmutabilityProperties
                                 && result.SnapshotNoMutableRuntimeContainers
                                 && result.SnapshotObjectReplacementObserved
                                 && result.PriorSnapshotValuesStableAfterFurtherStep;

            result.Pass = floatPass && discretePass && immutabilityPass;
            if (!result.Pass)
            {
                var reasons = new List<string>();
                if (!floatPass) reasons.Add("floating-point fidelity tolerance breach");
                if (!discretePass) reasons.Add("discrete field mismatch");
                if (!immutabilityPass) reasons.Add("snapshot immutability proof failure");
                result.FailureReason = string.Join("; ", reasons);
            }

            return result;
        }

        private static void RunImmutabilityChecks(HeatupSimEngine engine, MethodInfo runStep, StageBResult result)
        {
            result.SnapshotImmutabilityProperties =
                HasNoPublicWritableProperties(typeof(StepSnapshot)) &&
                HasNoPublicWritableProperties(typeof(PlantState));

            StepSnapshot before = engine.GetStepSnapshot();
            PlantState beforeState = before.PlantState;
            float beforePressure = beforeState.PressurePsia;
            float beforePzr = beforeState.PzrLevelPct;
            float beforeMass = beforeState.PrimaryMassLedgerLb;

            runStep.Invoke(engine, new object[] { DtHr });
            StepSnapshot after = engine.GetStepSnapshot();

            result.SnapshotObjectReplacementObserved =
                !ReferenceEquals(before, after) &&
                !ReferenceEquals(beforeState, after.PlantState);

            result.PriorSnapshotValuesStableAfterFurtherStep =
                Mathf.Abs(beforeState.PressurePsia - beforePressure) <= FloatTolerance &&
                Mathf.Abs(beforeState.PzrLevelPct - beforePzr) <= FloatTolerance &&
                Mathf.Abs(beforeState.PrimaryMassLedgerLb - beforeMass) <= FloatTolerance;

            result.SnapshotNoMutableRuntimeContainers =
                before.TransferLedger != null &&
                before.TransferLedger.Events is ICollection<TransferEvent> eventsCollection &&
                eventsCollection.IsReadOnly;
        }

        private static bool HasNoPublicWritableProperties(Type type)
        {
            PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo prop in props)
            {
                if (prop.CanWrite)
                    return false;
            }

            return true;
        }

        private static Metric CreateFloatMetric(string name, float tolerance)
        {
            return new Metric { Name = name, Tolerance = tolerance };
        }

        private static DiscreteMetric CreateDiscreteMetric(string name)
        {
            return new DiscreteMetric { Name = name };
        }

        private static float Track(Metric metric, int step, float direct, float projected)
        {
            float err = Mathf.Abs(projected - direct);
            if (err > metric.MaxError)
            {
                metric.MaxError = err;
                metric.WorstStep = step;
            }

            return err;
        }

        private static void TrackDiscrete(DiscreteMetric metric, int step, int direct, int projected)
        {
            if (direct == projected)
                return;

            metric.MismatchCount++;
            if (metric.FirstMismatchStep == 0)
                metric.FirstMismatchStep = step;
        }

        private static void TrackDiscrete(DiscreteMetric metric, int step, bool direct, bool projected)
        {
            if (direct == projected)
                return;

            metric.MismatchCount++;
            if (metric.FirstMismatchStep == 0)
                metric.FirstMismatchStep = step;
        }

        private static void TrackDiscrete(DiscreteMetric metric, int step, string direct, string projected)
        {
            if (string.Equals(direct, projected, StringComparison.Ordinal))
                return;

            metric.MismatchCount++;
            if (metric.FirstMismatchStep == 0)
                metric.FirstMismatchStep = step;
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

        private static void WriteFidelityCsv(string path, StageBResult result)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            var sb = new StringBuilder();
            foreach (string row in result.FidelityCsvRows)
                sb.AppendLine(row);
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteIssue(string path, string runstamp, string logDir, StageBResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# IP-0025 Stage B - Snapshot Fidelity and Immutability");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run stamp: `{runstamp}`");
            sb.AppendLine($"- Result: {(result.Pass ? "PASS" : "FAIL")}");
            sb.AppendLine($"- Steps compared: `{result.Steps}`");
            sb.AppendLine();
            sb.AppendLine("## Compared Fields (Direct Legacy vs StepSnapshot.PlantState)");
            foreach (Metric metric in result.FloatMetrics)
            {
                sb.AppendLine(
                    $"- `{metric.Name}` tolerance=`{metric.Tolerance:E2}` maxError=`{metric.MaxError:E2}` worstStep=`{metric.WorstStep}`");
            }
            foreach (DiscreteMetric metric in result.DiscreteMetrics)
            {
                sb.AppendLine(
                    $"- `{metric.Name}` exactMatch=`{metric.MismatchCount == 0}` mismatches=`{metric.MismatchCount}` firstMismatchStep=`{metric.FirstMismatchStep}`");
            }
            sb.AppendLine();
            sb.AppendLine("## Snapshot Immutability Proof");
            sb.AppendLine($"- Public writable properties absent (`StepSnapshot` + `PlantState`): `{result.SnapshotImmutabilityProperties}`");
            sb.AppendLine($"- Snapshot runtime containers immutable/read-only: `{result.SnapshotNoMutableRuntimeContainers}`");
            sb.AppendLine($"- New snapshot object created per publish: `{result.SnapshotObjectReplacementObserved}`");
            sb.AppendLine($"- Prior snapshot values stable after next step: `{result.PriorSnapshotValuesStableAfterFurtherStep}`");
            sb.AppendLine();
            sb.AppendLine("## Artifacts");
            sb.AppendLine($"- Run directory: `{ToRepoRelative(logDir)}`");
            sb.AppendLine($"- Fidelity CSV: `{ToRepoRelative(Path.Combine(logDir, "snapshot_fidelity.csv"))}`");

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
