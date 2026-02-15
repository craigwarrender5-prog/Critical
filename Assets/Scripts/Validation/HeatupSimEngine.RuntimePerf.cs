using UnityEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Critical.Simulation.Modular;

public partial class HeatupSimEngine
{
    private struct PendingLogWrite
    {
        public long SequenceId;
        public string FilePath;
        public string Content;
        public string Source;
    }

    public readonly struct RuntimeTelemetrySnapshot
    {
        public readonly float SimTime;
        public readonly float WallClockTime;
        public readonly int PlantMode;
        public readonly string HeatupPhaseDesc;
        public readonly int CurrentSpeedIndex;
        public readonly bool IsAccelerated;
        public readonly float Pressure;
        public readonly float Tavg;
        public readonly float Trcs;
        public readonly float PzrLevel;
        public readonly int RcpCount;
        public readonly float ChargingFlow;
        public readonly float LetdownFlow;

        public RuntimeTelemetrySnapshot(
            float simTime,
            float wallClockTime,
            int plantMode,
            string heatupPhaseDesc,
            int currentSpeedIndex,
            bool isAccelerated,
            float pressure,
            float tavg,
            float trcs,
            float pzrLevel,
            int rcpCount,
            float chargingFlow,
            float letdownFlow)
        {
            SimTime = simTime;
            WallClockTime = wallClockTime;
            PlantMode = plantMode;
            HeatupPhaseDesc = heatupPhaseDesc ?? string.Empty;
            CurrentSpeedIndex = currentSpeedIndex;
            IsAccelerated = isAccelerated;
            Pressure = pressure;
            Tavg = tavg;
            Trcs = trcs;
            PzrLevel = pzrLevel;
            RcpCount = rcpCount;
            ChargingFlow = chargingFlow;
            LetdownFlow = letdownFlow;
        }
    }

    private readonly object _runtimeSync = new object();
    private RuntimeTelemetrySnapshot _latestSnapshot;

    private readonly object _asyncLogQueueLock = new object();
    private readonly Queue<PendingLogWrite> _asyncLogQueue = new Queue<PendingLogWrite>();
    private AutoResetEvent _asyncLogSignal;
    private Thread _asyncLogThread;
    private volatile bool _asyncLogStopRequested;
    private volatile bool _asyncLogThreadRunning;
    private long _asyncLogSequenceId;
    private float _asyncLogLastDropWarnRealtime;

    private const int ASYNC_LOG_QUEUE_CAPACITY = 8192;
    private const float ASYNC_LOG_DROP_WARN_PERIOD_SEC = 1f;

    [HideInInspector] public int asyncLogDroppedCount = 0;
    [HideInInspector] public int asyncLogQueueHighWatermark = 0;
    [HideInInspector] public int asyncLogWriteErrorCount = 0;
    [HideInInspector] public float asyncLogLastFlushMs = 0f;
    [HideInInspector] public float asyncLogMaxDispatchMs = 0f;
    [HideInInspector] public bool workerThreadSteppingLastStepUsed = false;
    [HideInInspector] public bool modularCoordinatorPathLastStepUsed = false;

    private PlantSimulationCoordinator _plantSimulationCoordinator;

    public object RuntimeSyncRoot => _runtimeSync;

    public RuntimeTelemetrySnapshot GetTelemetrySnapshot()
    {
        lock (_runtimeSync)
        {
            return _latestSnapshot;
        }
    }

    void PublishTelemetrySnapshot()
    {
        lock (_runtimeSync)
        {
            _latestSnapshot = new RuntimeTelemetrySnapshot(
                simTime,
                wallClockTime,
                plantMode,
                heatupPhaseDesc,
                currentSpeedIndex,
                isAccelerated,
                pressure,
                T_avg,
                T_rcs,
                pzrLevel,
                rcpCount,
                chargingFlow,
                letdownFlow);
        }
    }

    void RunPhysicsStep(float dt)
    {
        // Default path: deterministic single-thread execution.
        if (!enableWorkerThreadStepping)
        {
            lock (_runtimeSync)
            {
                workerThreadSteppingLastStepUsed = false;
                ExecutePhysicsStepAuthorityPath(dt);
                PublishTelemetrySnapshot();
            }
            return;
        }

        // IP-0023 CS-0046: explicit worker-thread dispatch path.
        Exception workerException = null;
        using (var done = new ManualResetEventSlim(false))
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    lock (_runtimeSync)
                    {
                        ExecutePhysicsStepAuthorityPath(dt);
                        PublishTelemetrySnapshot();
                    }
                }
                catch (Exception ex)
                {
                    workerException = ex;
                }
                finally
                {
                    done.Set();
                }
            });

            done.Wait();
        }

        workerThreadSteppingLastStepUsed = true;
        if (workerException != null)
            throw workerException;
    }

    private void ExecutePhysicsStepAuthorityPath(float dt)
    {
        bool useCoordinator = enableModularCoordinatorPath || ModularFeatureFlags.EnableCoordinatorPath;
        if (useCoordinator)
        {
            modularCoordinatorPathLastStepUsed = true;
            EnsurePlantSimulationCoordinator();
            _plantSimulationCoordinator.Step(dt);
            return;
        }

        modularCoordinatorPathLastStepUsed = false;
        StepSimulation(dt);
    }

    private void EnsurePlantSimulationCoordinator()
    {
        if (_plantSimulationCoordinator != null)
            return;

        _plantSimulationCoordinator = new PlantSimulationCoordinator(this);
    }

    internal void RunLegacySimulationStepForCoordinator(float dt)
    {
        StepSimulation(dt);
    }

    void WriteTextFileRuntime(string filePath, StringBuilder contentBuilder, bool preferAsync, string source)
    {
        string content = contentBuilder.ToString();
        WriteTextFileRuntime(filePath, content, preferAsync, source);
    }

    void WriteTextFileRuntime(string filePath, string content, bool preferAsync, string source)
    {
        // Do not require coroutine-owned run state; deterministic validation runners
        // invoke StepSimulation/SaveIntervalLog directly and still need async behavior.
        bool useAsync = preferAsync && enableAsyncLogWriter && !_shutdownRequested;
        var dispatchSw = Stopwatch.StartNew();
        if (!useAsync)
        {
            WriteTextFileImmediate(filePath, content);
            dispatchSw.Stop();
            asyncLogMaxDispatchMs = Mathf.Max(asyncLogMaxDispatchMs, (float)dispatchSw.Elapsed.TotalMilliseconds);
            return;
        }

        EnsureAsyncLogWriterStarted();
        lock (_asyncLogQueueLock)
        {
            while (_asyncLogQueue.Count >= ASYNC_LOG_QUEUE_CAPACITY)
            {
                _asyncLogQueue.Dequeue();
                asyncLogDroppedCount++;
            }

            _asyncLogSequenceId++;
            _asyncLogQueue.Enqueue(new PendingLogWrite
            {
                SequenceId = _asyncLogSequenceId,
                FilePath = filePath,
                Content = content,
                Source = source
            });

            if (_asyncLogQueue.Count > asyncLogQueueHighWatermark)
                asyncLogQueueHighWatermark = _asyncLogQueue.Count;
        }

        if (asyncLogDroppedCount > 0)
        {
            float now = Time.realtimeSinceStartup;
            if (now - _asyncLogLastDropWarnRealtime >= ASYNC_LOG_DROP_WARN_PERIOD_SEC)
            {
                _asyncLogLastDropWarnRealtime = now;
                UnityEngine.Debug.LogWarning(
                    $"[IP-0023][ASYNC_LOG] Queue saturation observed, dropped={asyncLogDroppedCount}, " +
                    $"highWatermark={asyncLogQueueHighWatermark}/{ASYNC_LOG_QUEUE_CAPACITY}");
            }
        }

        _asyncLogSignal?.Set();
        dispatchSw.Stop();
        asyncLogMaxDispatchMs = Mathf.Max(asyncLogMaxDispatchMs, (float)dispatchSw.Elapsed.TotalMilliseconds);
    }

    void EnsureAsyncLogWriterStarted()
    {
        if (_asyncLogThreadRunning)
            return;

        lock (_asyncLogQueueLock)
        {
            if (_asyncLogThreadRunning)
                return;

            _asyncLogStopRequested = false;
            if (_asyncLogSignal == null)
                _asyncLogSignal = new AutoResetEvent(false);

            _asyncLogThread = new Thread(AsyncLogWriterLoop)
            {
                IsBackground = true,
                Name = "HeatupSimEngine.AsyncLogWriter"
            };
            _asyncLogThreadRunning = true;
            _asyncLogThread.Start();
        }
    }

    private void AsyncLogWriterLoop()
    {
        while (!_asyncLogStopRequested)
        {
            bool wroteAny = false;
            while (TryDequeueAsyncLogWrite(out PendingLogWrite item))
            {
                wroteAny = true;
                try
                {
                    WriteTextFileImmediate(item.FilePath, item.Content);
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref asyncLogWriteErrorCount);
                }
            }

            if (!wroteAny)
                _asyncLogSignal?.WaitOne(25);
        }

        // Drain residual queue on shutdown request.
        while (TryDequeueAsyncLogWrite(out PendingLogWrite pending))
        {
            try
            {
                WriteTextFileImmediate(pending.FilePath, pending.Content);
            }
            catch (Exception)
            {
                Interlocked.Increment(ref asyncLogWriteErrorCount);
            }
        }

        _asyncLogThreadRunning = false;
    }

    bool TryDequeueAsyncLogWrite(out PendingLogWrite item)
    {
        lock (_asyncLogQueueLock)
        {
            if (_asyncLogQueue.Count > 0)
            {
                item = _asyncLogQueue.Dequeue();
                return true;
            }
        }

        item = default;
        return false;
    }

    void WriteTextFileImmediate(string filePath, string content)
    {
        string dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    public bool FlushAsyncLogWriter(int timeoutMs)
    {
        if (!enableAsyncLogWriter || !_asyncLogThreadRunning)
            return true;

        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            bool empty;
            lock (_asyncLogQueueLock)
            {
                empty = _asyncLogQueue.Count == 0;
            }

            if (empty)
                break;

            _asyncLogSignal?.Set();
            Thread.Sleep(2);
        }

        asyncLogLastFlushMs = (float)sw.Elapsed.TotalMilliseconds;
        bool queueEmptyAfterWait;
        lock (_asyncLogQueueLock)
        {
            queueEmptyAfterWait = _asyncLogQueue.Count == 0;
        }

        if (!queueEmptyAfterWait)
        {
            UnityEngine.Debug.LogWarning(
                $"[IP-0023][ASYNC_LOG] Flush timeout after {asyncLogLastFlushMs:F1}ms; " +
                $"pending writes remain={_asyncLogQueue.Count}");
        }

        _asyncLogStopRequested = true;
        _asyncLogSignal?.Set();
        if (_asyncLogThread != null && _asyncLogThread.IsAlive)
            _asyncLogThread.Join(Mathf.Clamp(timeoutMs, 0, 500));

        _asyncLogThread = null;
        return queueEmptyAfterWait;
    }
}
