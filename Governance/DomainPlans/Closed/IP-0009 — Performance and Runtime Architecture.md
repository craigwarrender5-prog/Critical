> LEGACY IMPLEMENTATION ARTIFACT — archived during Constitution v1.0 governance re-baseline.
> Historical record preserved. No current execution authority.

---
Identifier: IP-0009
Domain: Performance / Runtime Architecture
Status: CLOSED (CS-0032 resolved v0.3.1.1 â€” Phase A validated; Phases B-D deferred as CS-0044, CS-0045, CS-0046)
Priority: Resolved
Tier: 0 (resolved)
Linked Issues: CS-0032, CS-0044, CS-0045, CS-0046
Last Reviewed: 2026-02-14
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# IP-0009 â€” Performance and Runtime Architecture
## Performance / Runtime Architecture â€” UI/Input Responsiveness

---

## Document Control

| Field | Value |
|-------|-------|
| Identifier | **IP-0009** |
| Plan Severity | **Resolved** (was CRITICAL / BLOCKER) |
| Architectural Domain | Performance / Runtime Architecture |
| System Area | HeatupSimEngine, HeatupValidationVisual (all partials), Unity Main Thread, File I/O |
| Discipline | Runtime Performance â€” Main Thread Budget |
| Status | **RESOLVED â€” Phase A validated, CS-0032 confirmed resolved v0.3.1.1** |
| Constitution Reference | PROJECT_CONSTITUTION.md, Articles V, X |
| Resolution Notes | Phase A (frame cap + UI throttle + header caching) implemented in v0.3.1.0 with flicker regression fix in v0.3.1.1. Long-run validation confirmed stable â€” UI responsive during extended simulation runs. Phases B-D remain as optional future enhancements but are no longer blocking. |

---

## Purpose

Diagnose and resolve the UI/input unresponsiveness observed during and after extended simulation runs. The symptom is a progressive loss of interactivity ("hang" perception) where the physics simulation continues executing but the Unity main thread cannot service UI events, input, or editor messages. This blocks overnight validation runs and all Phase E acceptance testing.

---

## Symptom Description

| Aspect | Observation |
|--------|-------------|
| **Primary Symptom** | UI elements (buttons, sliders, selection) become unresponsive |
| **Simulation State** | Physics loop continues â€” values update, logs write |
| **Onset** | Delayed â€” not immediate. Minutes to tens of minutes into run |
| **Trigger Correlation** | Closing Unity Editor during or after a run |
| **Memory** | No obvious memory growth observed |
| **Recovery** | Unknown â€” may require process kill |

---

## Scope

This plan SHALL:

- Investigate root cause of main thread starvation
- Establish instrumentation to measure frame time, GC pressure, and I/O stalls
- Design staged remediation targeting frame budget management
- Define acceptance criteria for input responsiveness

This plan shall NOT:

- Modify physics conservation architecture (v0.1.0.0, v0.2.0.0, v0.3.0.0)
- Change physics model correctness or numerical results
- Alter simulation output/log content (only log delivery mechanism)
- Modify domain-specific logic (bubble formation, SG, CVCS, etc.)

---

# SECTION 1: INVESTIGATION PLAN

## 1.1 Reproduction Steps (Exact)

1. Open Unity Editor with the Critical project
2. Enter Play Mode
3. Start a full heatup simulation (0 â†’ 16.5 hr, 0.25 hr intervals)
4. Allow simulation to run unattended for 30+ minutes real time
5. Attempt to interact with UI (click buttons, adjust sliders, select items)
6. Observe: inputs are delayed or ignored, UI elements do not respond
7. Variant: Close Unity Editor window during or after simulation run â€” observe if responsiveness degrades faster

## 1.2 A/B Toggle Isolation Tests

Each test modifies ONE variable to isolate the root cause. Run the same reproduction scenario and measure frame time + input responsiveness.

### Toggle 1: Cap FPS / VSync

| Setting | Test A (Baseline) | Test B (Capped) |
|---------|-------------------|-----------------|
| Target FPS | Uncapped (Application.targetFrameRate = -1) | 30 FPS cap (Application.targetFrameRate = 30) |
| VSync | Current setting | QualitySettings.vSyncCount = 1 |
| Expected | If FPS is uncapped, engine may burn 100% CPU on rendering/physics, starving message pump | Capping FPS returns time to message pump between frames |

### Toggle 2: Disable UI Updates

| Setting | Test A (Baseline) | Test B (UI Off) |
|---------|-------------------|-----------------|
| HeatupValidationVisual | Active (OnGUI runs every frame) | Disabled (component.enabled = false or early-return in OnGUI) |
| Expected | If OnGUI is the bottleneck (string formatting, layout, texture uploads), disabling it restores input responsiveness |

### Toggle 3: Disable Logging/Writes

| Setting | Test A (Baseline) | Test B (No Writes) |
|---------|-------------------|--------------------|
| Debug.Log calls | Active | Suppress via #if ENABLE_LOGGING or conditional compilation |
| Interval log file writes | Active (synchronous StreamWriter) | Disabled or redirected to null |
| Expected | If synchronous file I/O stalls the main thread (disk flush, antivirus scan, network drive), disabling writes restores responsiveness |

### Toggle 4: Reduce Sim Step Rate

| Setting | Test A (Baseline) | Test B (Throttled) |
|---------|-------------------|-------------------|
| Steps per frame | Current (may be multiple steps per Update) | Max 1 step per frame |
| dt | Current | Unchanged (same physics, fewer steps per frame) |
| Expected | If physics compute exceeds frame budget (16ms at 60fps), throttling returns time to message pump |

## 1.3 Instrumentation Plan (Minimal Code-Only Probes)

These are measurement-only probes. They do NOT change behavior. Implementation requires explicit authorization.

### Probe 1: Frame Time Spike Detector

```
// Add to HeatupSimEngine.Update() or a dedicated MonoBehaviour
float frameStart = Time.realtimeSinceStartup;
// ... existing Update logic ...
float frameDuration_ms = (Time.realtimeSinceStartup - frameStart) * 1000f;
if (frameDuration_ms > 33f)  // >33ms = below 30fps
    Debug.LogWarning($"[PERF] Frame spike: {frameDuration_ms:F1}ms at T+{simTime:F2}hr");
```

**Location:** `HeatupSimEngine.cs` â€” wrapping the main `Update()` or `StepSimulation()` call
**Purpose:** Identify which simulation intervals produce frame spikes
**Files likely affected:** `HeatupSimEngine.cs` (1 file, 4 lines)

### Probe 2: Per-Frame GC Allocation Counter

```
// At start of Update:
long gcBefore = System.GC.GetTotalMemory(false);
// At end of Update:
long gcAfter = System.GC.GetTotalMemory(false);
long allocBytes = gcAfter - gcBefore;
if (allocBytes > 50000)  // >50KB per frame is excessive
    Debug.LogWarning($"[PERF] GC alloc: {allocBytes / 1024}KB in frame at T+{simTime:F2}hr");
```

**Location:** `HeatupSimEngine.cs` â€” wrapping `Update()`
**Purpose:** Detect per-frame allocation pressure that triggers GC pauses
**Files likely affected:** `HeatupSimEngine.cs` (1 file, 6 lines)
**Note:** `GC.GetTotalMemory(false)` is approximate. For precise measurement, use Unity Profiler or `ProfilerRecorder`.

### Probe 3: Watchdog Stall Detector (Optional)

```
// Separate MonoBehaviour with Update()
float lastUpdateTime;
void Update()
{
    float gap = Time.realtimeSinceStartup - lastUpdateTime;
    if (gap > 0.250f && lastUpdateTime > 0f)  // >250ms since last Update = stall
        Debug.LogError($"[PERF WATCHDOG] Main thread stall: {gap * 1000:F0}ms gap detected");
    lastUpdateTime = Time.realtimeSinceStartup;
}
```

**Location:** New lightweight MonoBehaviour (or appended to existing non-simulation object)
**Purpose:** Detect when Unity's main thread is blocked for >250ms (input will feel unresponsive)
**Files likely affected:** 1 new file (~15 lines) or 6 lines appended to existing MonoBehaviour

## 1.4 OS-Side Capture Plan

### Windows Performance Recorder (WPR) / Windows Performance Analyzer (WPA)

**Recording:**
1. Open Windows Performance Recorder (`wpr.exe` or Start Menu â†’ "Windows Performance Recorder")
2. Select profile: **CPU Usage**, **Disk I/O**, **File I/O**
3. Click **Start**
4. Reproduce the hang scenario (run simulation for 15+ minutes, attempt UI interaction)
5. When hang is observed, wait 30 seconds then click **Save**
6. Output: `.etl` file

**Analysis in WPA:**
1. Open the `.etl` file in Windows Performance Analyzer
2. **Key views to inspect:**

| WPA View | What to Look For |
|----------|-----------------|
| **CPU Usage (Sampled) â†’ by Thread** | Is Unity's main thread at 100%? Which function dominates? Look for `OnGUI`, `StepSimulation`, `Debug.Log`, `StreamWriter.Write` |
| **CPU Usage (Sampled) â†’ Call Stacks** | Drill into main thread stacks â€” identify the deepest hot path. Common culprits: `GC.Collect`, `File.Flush`, `GUI.Repaint`, string concatenation |
| **Disk I/O â†’ by File** | Is the log file being written synchronously? Look for high I/O time on `HeatupLog_*.txt` or Unity's `Editor.log` |
| **File I/O â†’ Summary** | Total bytes written per second. If >10MB/s from log writes, I/O is a factor |
| **Generic Events â†’ GC** | Managed GC collection events â€” frequency and duration. >10ms GC pauses correlate with input drops |

**Alternative: Unity Profiler**
1. Window â†’ Analysis â†’ Profiler
2. Record during hang period
3. Inspect: CPU module (main thread timeline), GC.Alloc, Rendering, Scripts
4. Look for: frames >33ms, large GC.Alloc spikes, `OnGUI` dominating frame

---

# SECTION 2: STAGED REMEDIATION PLAN

## Prioritization Rationale

Ordered by fastest high-confidence wins. Each phase is independently valuable â€” partial completion still improves responsiveness.

---

## Phase A: Frame Cap + UI Throttling + Update-on-Change Thresholds â€” VALIDATED

**Confidence: HIGH | Effort: LOW | Impact: HIGH | Status: VALIDATED v0.3.1.1**

This is the fastest path to responsiveness. If the engine runs uncapped, it will consume 100% of one core, leaving zero budget for input processing.

### A.1: Frame Rate Cap

**Files likely to change:**
- `HeatupSimEngine.Init.cs` â€” Add `Application.targetFrameRate = 30;` in `InitializeSimulation()`
- Alternatively: `QualitySettings.vSyncCount = 1;`

**Design:**
```
// In InitializeSimulation() or Awake()
Application.targetFrameRate = 30;
```

This ensures Unity yields to the OS message pump between frames. 30 FPS is sufficient for a simulation display.

### A.2: UI Update Throttling (10 Hz)

**Files likely to change:**
- `HeatupValidationVisual.cs` (base partial) â€” Add throttle gate in `OnGUI()`
- `HeatupValidationVisual.Gauges.cs` â€” Conditional update
- `HeatupValidationVisual.Panels.cs` â€” Conditional update
- `HeatupValidationVisual.Graphs.cs` â€” Conditional update
- `HeatupValidationVisual.TabCritical.cs` â€” Conditional update
- `HeatupValidationVisual.TabValidation.cs` â€” Conditional update

**Design:**
```
// In the base OnGUI or a shared pre-check:
private float _lastUIUpdateTime;
private const float UI_UPDATE_INTERVAL = 0.1f;  // 10 Hz

void OnGUI()
{
    float now = Time.realtimeSinceStartup;
    if (now - _lastUIUpdateTime < UI_UPDATE_INTERVAL)
    {
        // Still repaint cached content (no recalculation)
        RepaintCachedUI();
        return;
    }
    _lastUIUpdateTime = now;
    // Full UI recalculation follows...
}
```

OnGUI is called multiple times per frame (Layout + Repaint events). Full recalculation at 10 Hz avoids per-frame string formatting (the v0.9.6 alloc fix only cached strings â€” the formatting still runs on data change).

### A.3: Update-on-Change Thresholds

**Files likely to change:**
- `HeatupValidationVisual.Panels.cs` â€” Add dirty-flag check
- `HeatupValidationVisual.Gauges.cs` â€” Add dirty-flag check

**Design:**
Only reformat display strings when underlying values change beyond display precision:
```
// Example for pressure display (shown to F0 = integer precision)
if (Mathf.Abs(cachedPressure - engine.pressure) > 0.5f)
{
    cachedPressure = engine.pressure;
    cachedPressureString = $"{engine.pressure:F0} psia";
}
```

This eliminates string allocations for stable values (most parameters change slowly).

### Acceptance Criteria (Phase A)

| Metric | Target |
|--------|--------|
| Input responsiveness | Button clicks register within 200ms |
| Max frame time | < 50ms (20 FPS floor) |
| Average frame time | < 33ms (30 FPS target) |
| CPU utilization (main thread) | < 80% |

---

## Phase B: Async Log Writer (Queue + Background Flush) â€” DEFERRED (CS-0044)

**Confidence: MEDIUM-HIGH | Effort: MEDIUM | Impact: MEDIUM-HIGH | Status: DEFERRED â€” registered as CS-0044**

Synchronous file writes on the main thread can stall for milliseconds (disk flush, antivirus scan, network drive). This is especially problematic during DRAIN phase where forensic logging is dense.

### B.1: Async Log Queue

**Files likely to change:**
- `HeatupSimEngine.Logging.cs` â€” Replace direct `StreamWriter.Write` with queue
- New: `AsyncLogWriter.cs` (or embedded in Logging partial)

**Design:**
```
// Producer (main thread):
private ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();

void WriteLogLine(string line)
{
    _logQueue.Enqueue(line);
}

// Consumer (background thread):
private Thread _logWriterThread;
private bool _logWriterRunning;

void LogWriterLoop()
{
    using (var writer = new StreamWriter(logPath, true))
    {
        while (_logWriterRunning || !_logQueue.IsEmpty)
        {
            while (_logQueue.TryDequeue(out string line))
                writer.WriteLine(line);
            writer.Flush();
            Thread.Sleep(100);  // Flush at 10 Hz
        }
    }
}
```

**Key constraints:**
- Queue is lock-free (`ConcurrentQueue`) â€” no main-thread blocking
- Flush at 10 Hz â€” tolerates 100ms data loss on crash (acceptable for diagnostics)
- Thread must be joined on `OnDestroy()` / `OnApplicationQuit()`
- Queue depth monitor: if >1000 entries queued, log a warning (backpressure signal)

### B.2: Debug.Log Consolidation

**Files likely to change:**
- `HeatupSimEngine.BubbleFormation.cs` â€” Consolidate per-step Debug.Log calls
- `HeatupSimEngine.cs` â€” Consolidate periodic debug output
- `HeatupSimEngine.CVCS.cs` â€” Review logging frequency

**Design:**
Consolidate multiple `Debug.Log` calls per timestep into a single `StringBuilder` â†’ `Debug.Log(sb.ToString())`. Each `Debug.Log` call has overhead (stack capture, timestamp, thread safety). Batching reduces call count by 3-5x.

### Acceptance Criteria (Phase B)

| Metric | Target |
|--------|--------|
| Main thread file I/O | 0 bytes/frame (all writes on background thread) |
| Log data loss on clean shutdown | 0 (queue fully drained) |
| Debug.Log calls per timestep | â‰¤ 3 (consolidated) |
| Frame time during DRAIN phase | < 40ms |

---

## Phase C: Snapshot Boundary (Physics Compute â†’ Snapshot â†’ UI Apply) â€” DEFERRED (CS-0045)

**Confidence: MEDIUM | Effort: MEDIUM | Impact: MEDIUM | Status: DEFERRED â€” registered as CS-0045**

Currently, physics writes to fields that UI reads on the same frame. This creates implicit coupling where UI must run after physics, and any field access is a potential race if physics is later moved off-thread.

### C.1: Physics Snapshot Struct

**Files likely to change:**
- `HeatupSimEngine.cs` â€” Add snapshot capture after `StepSimulation()`
- `HeatupSimEngine.Logging.cs` â€” Snapshot struct definition
- `HeatupValidationVisual.cs` (all partials) â€” Read from snapshot instead of engine fields

**Design:**
```
public struct SimSnapshot
{
    public float SimTime, Pressure, T_pzr, T_rcs, T_avg, T_sat;
    public float PzrLevel, PzrSteamVolume, PressureRate;
    public float TotalPrimaryMass, MassError;
    public float ChargingFlow, LetdownFlow;
    public int RcpCount;
    public BubbleFormationPhase BubblePhase;
    public bool BubbleFormed, SolidPressurizer;
    public string StatusMessage, HeatupPhaseDesc;
    // ... all UI-consumed fields
}

// After StepSimulation():
currentSnapshot = CaptureSnapshot();

// UI reads only from currentSnapshot, never from engine fields directly
```

This decouples physics execution from UI rendering. The snapshot is an immutable copy â€” UI can read it freely without timing concerns.

### Acceptance Criteria (Phase C)

| Metric | Target |
|--------|--------|
| UI-physics coupling | Zero direct field reads from UI to engine (all via snapshot) |
| Snapshot capture time | < 0.1ms |
| Behavioral change | None â€” snapshot values are identical to direct reads |

---

## Phase D: Parallelization Strategy (Jobs/Burst or Worker Thread) â€” DEFERRED (CS-0046)

**Confidence: LOW-MEDIUM | Effort: HIGH | Impact: HIGH (long-term) | Status: DEFERRED â€” registered as CS-0046**

This is the most invasive phase and should only be attempted after A-C are complete and validated.

### D.1: Assessment â€” Unity Main Thread Constraints

Unity requires that:
- All `MonoBehaviour` callbacks (`Update`, `OnGUI`, `OnDestroy`) run on main thread
- All `UnityEngine.Object` access (transforms, components, materials) is main-thread only
- `Debug.Log` is thread-safe but incurs main-thread overhead
- Physics simulation data (plain floats, structs) can be computed off-thread

`StepSimulation()` is a candidate for off-thread execution IF:
- It does not access `UnityEngine.Object` members
- It does not call `Debug.Log` synchronously (Phase B moves logs to queue)
- Results are consumed via snapshot (Phase C provides this boundary)

### D.2: Unity Jobs + Burst (Preferred)

**Files likely to change:**
- `HeatupSimEngine.cs` â€” Convert `StepSimulation()` to `IJob`
- All physics modules â€” Ensure Burst-compatible (no managed allocations, no Unity API calls)
- `HeatupSimEngine.Logging.cs` â€” Log queue is NativeQueue instead of ConcurrentQueue

**Constraints:**
- Burst requires `[BurstCompile]` compatible code â€” no `string`, no `Debug.Log`, no `Mathf` (use `math` from Unity.Mathematics)
- Extensive refactor of physics modules
- Only viable after Phase C snapshot boundary is established

### D.3: Simple Worker Thread (Alternative)

**Files likely to change:**
- `HeatupSimEngine.cs` â€” `StepSimulation()` runs on dedicated thread
- Synchronization via double-buffered snapshot (Phase C)

**Design:**
```
// Worker thread:
while (running)
{
    StepSimulation(dt);
    CaptureSnapshot();  // Write to back buffer
    SwapBuffers();       // Atomic swap
    WaitForNextStep();
}

// Main thread (Update):
var snap = ReadFrontBuffer();  // Always consistent, never blocks
```

**Constraints:**
- Simpler than Jobs/Burst but still requires all `UnityEngine` API calls removed from `StepSimulation`
- `Debug.Log` must be deferred (Phase B)
- Error handling: worker thread exceptions must be surfaced to main thread

### Acceptance Criteria (Phase D)

| Metric | Target |
|--------|--------|
| Main thread compute budget for physics | < 2ms/frame (compute is off-thread) |
| Input responsiveness under full load | < 100ms |
| Frame time | < 20ms (50 FPS) |
| CPU utilization | Distributed across â‰¥ 2 cores |
| Physics correctness | Bit-identical to single-threaded results (regression test) |

---

# SECTION 3: CROSS-CUTTING CONSIDERATIONS

## Impact on In-Progress Work

| Domain | Impact |
|--------|--------|
| Bubble Formation v0.3.0.0 (Phase D complete, Phase E pending) | **BLOCKED** â€” Phase E requires a full 0â†’16.5 hr validation run with interactive observation. CS-0032 prevents this. |
| SG Secondary Physics (CS-0014â€“0020) | Blocked for validation â€” any future domain plan Phase E will require interactive runs. |
| All future domain plans | Phase E validation cannot be executed until CS-0032 is resolved. |

## Files Likely to Change (Summary Across All Phases)

| File | Phase | Change Type |
|------|-------|-------------|
| `HeatupSimEngine.Init.cs` | A | Frame cap setting |
| `HeatupValidationVisual.cs` (base) | A | UI throttle gate |
| `HeatupValidationVisual.Gauges.cs` | A | Change-threshold caching |
| `HeatupValidationVisual.Panels.cs` | A | Change-threshold caching |
| `HeatupValidationVisual.Graphs.cs` | A | Conditional update |
| `HeatupValidationVisual.TabCritical.cs` | A | Conditional update |
| `HeatupValidationVisual.TabValidation.cs` | A | Conditional update |
| `HeatupSimEngine.cs` | A, C | Frame time probe, snapshot capture |
| `HeatupSimEngine.Logging.cs` | B, C | Async queue, snapshot struct |
| `HeatupSimEngine.BubbleFormation.cs` | B | Debug.Log consolidation |
| `HeatupSimEngine.CVCS.cs` | B | Logging review |
| New: `AsyncLogWriter.cs` (or Logging partial) | B | Background writer thread |
| New: `PerfWatchdog.cs` (optional) | Investigation | Stall detector |

---

# SECTION 4: PRIORITY STATEMENT

**CS-0032 RESOLVED â€” Phase A validated v0.3.1.1.**

Phase A (frame cap + UI throttle + header caching) resolved the blocking symptom. Long-run validation runs are now possible. Phases B-D remain as optional future enhancements for further performance improvement but are no longer required for validation workflows.

Previously blocked items now unblocked:
- v0.3.0.0 Bubble Formation Phase E (executed â€” FAILED due to CS-0043, not performance)
- All future domain validation runs
- Overnight unattended testing

---

## Known Limitations

| Limitation | Impact |
|-----------|--------|
| Root cause not yet confirmed | Investigation required before remediation. Phase A is high-confidence even without root cause. |
| Unity Profiler requires Editor | Cannot profile standalone builds without Unity Pro Profiler. WPR/WPA is the fallback. |
| Phase D (parallelization) is speculative | May not be needed if A-C resolve the issue. High effort, defer unless required. |
| Cannot test during Editor close scenario | The trigger (closing Editor) destroys the profiling environment. Must capture pre-close state. |

---

## Out-of-Domain Findings

None â€” this is a new cross-cutting domain with no prior investigation.

