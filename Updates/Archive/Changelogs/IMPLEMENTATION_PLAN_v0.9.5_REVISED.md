# Implementation Plan v0.9.5 — Critical Memory Leak & Application Hang Fix

**Date:** 2026-02-07  
**Version:** 0.9.5 (Patch)  
**Type:** Critical Bug Fix  
**Status:** READY FOR IMPLEMENTATION  
**Priority:** CRITICAL

---

## Executive Summary

### Problem Statement
1. **Memory Leak (~500 MB native):** Application is consuming ~500MB of native memory (Task Manager) while the managed heap profiler only reports ~120MB. This discrepancy indicates unmanaged/native memory accumulation.
2. **Application Hang on Exit:** When closing via X button or window close command, the application freezes and displays "Program has stopped responding" (Windows Event ID 1002, TerminationTime=148 seconds).

### Root Cause Analysis

#### Issue 1: Native Memory Accumulation (500MB vs 120MB reported)

**Location of Discrepancy:**
- `Profiler.GetTotalAllocatedMemoryLong()` only reports managed C# heap memory
- Missing: Unity native memory, GPU texture memory, graphics driver allocations

**Probable Native Memory Leaks:**

1. **GL Material never destroyed:** `_glMat` in `HeatupValidationVisual.Styles.cs` is created once but never explicitly destroyed. While marked `HideFlags.DontSave`, it may accumulate during domain reloads or if recreated.

2. **Texture2D objects persist:** Although v0.9.4 fixed the per-frame texture allocation, the ~25 cached textures created at startup (each 1×1 RGBA32) total ~100 bytes. However, Unity allocates textures on the GPU with significant overhead. The textures also have `HideFlags.DontSave` but are never explicitly destroyed on shutdown.

3. **History buffer Lists growing unbounded:** While capped at MAX_HISTORY (240 entries), there are **19 separate List<float>** history buffers. With 240 entries × 4 bytes × 19 lists = ~18KB. This is minimal, but the List backing arrays may be larger than needed.

4. **Event log List growth:** `eventLog` is capped at MAX_EVENT_LOG (200 entries), each `EventLogEntry` is ~50 bytes (struct with float + enum + string). ~10KB max.

5. **Potential Unity-side native memory:** Unity's OnGUI/IMGUI system allocates native memory for rendering. With complex UI at 60fps, this can accumulate significantly.

6. **GL.PushMatrix/PopMatrix calls:** Each graph trace rendering calls `GL.PushMatrix()`/`GL.PopMatrix()` twice per trace. With 6 traces × 2 calls × 60fps = 720 GL calls/second. Unity's GL subsystem may accumulate internal state.

#### Issue 2: Application Hang on Exit (148 seconds timeout)

**Root Cause Chain:**

1. **Coroutine continues after quit signal:** `HeatupSimEngine.RunSimulation()` is an `IEnumerator` coroutine that runs in a `while(isRunning)` loop. Setting `isRunning = false` doesn't immediately stop the coroutine - it waits for the next `yield return`.

2. **ForceStop() order issue:** Currently calls `StopAllCoroutines()` then sets `isRunning = false`. If `StopAllCoroutines()` doesn't immediately halt mid-execution, the coroutine may continue.

3. **File I/O during shutdown:** `SaveReport()` and `SaveIntervalLog()` write to disk. If called during shutdown, they may block.

4. **OnApplicationQuit timing:** Unity calls `OnApplicationQuit()` which triggers `StopSimulation()` → `ForceStop()`. If coroutines are mid-execution, they may not stop cleanly.

5. **Environment.Exit(0) not used in builds:** The current `ForceQuit()` uses `System.Environment.Exit(0)` which is aggressive but effective. However, it's only called on X key press, not on window close button.

6. **GL/Material cleanup not performed:** Native GL resources are not explicitly released, potentially blocking the graphics context from closing.

---

## Proposed Solutions

### Stage 1: Fix Memory Reporting (Cosmetic but Important for Diagnostics)

**File:** `HeatupValidationVisual.Panels.cs`

Replace managed-only memory reporting with total process memory:

```csharp
// v0.9.5: Report total memory (managed + native + graphics) for accurate leak detection
long managedMem = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
long reservedMem = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong();
long graphicsMem = UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver();
float totalMB = (reservedMem + graphicsMem) / (1024f * 1024f);
```

**Rationale:** This gives a more accurate picture matching Task Manager, though still may undercount native Unity allocations.

---

### Stage 2: Fix Application Exit Hang

**File:** `HeatupSimEngine.cs`

#### 2A. Add cancellation flag checked inside coroutine loop

```csharp
private volatile bool _shutdownRequested = false;

IEnumerator RunSimulation()
{
    isRunning = true;
    _shutdownRequested = false;
    
    // ... initialization ...
    
    while (isRunning && !_shutdownRequested && simTime < 24f && T_rcs < targetTemperature - 2f)
    {
        // ... existing physics loop ...
        
        // Check for shutdown request at multiple points
        if (_shutdownRequested) break;
        
        yield return null;
        
        if (_shutdownRequested) break;
    }
    
    // Only save report if not shutting down
    if (!_shutdownRequested)
        SaveReport();
    
    // Signal clean completion
    isRunning = false;
}
```

#### 2B. Immediate shutdown without cleanup

```csharp
/// <summary>
/// v0.9.5: Request immediate shutdown. Sets flag before stopping coroutines
/// to ensure the coroutine sees the flag on its next check.
/// </summary>
public void RequestImmediateShutdown()
{
    Debug.Log("[HeatupSimEngine] IMMEDIATE SHUTDOWN REQUESTED");
    _shutdownRequested = true;
    isRunning = false;
    StopAllCoroutines();
}
```

#### 2C. Update OnApplicationQuit and OnDestroy

```csharp
void OnApplicationQuit()
{
    Debug.Log("[HeatupSimEngine] OnApplicationQuit - immediate shutdown");
    RequestImmediateShutdown();
}

void OnDestroy()
{
    Debug.Log("[HeatupSimEngine] OnDestroy - immediate shutdown");
    RequestImmediateShutdown();
}

void OnDisable()
{
    Debug.Log("[HeatupSimEngine] OnDisable - immediate shutdown");
    RequestImmediateShutdown();
}
```

---

**File:** `HeatupValidationVisual.cs`

#### 2D. Enhanced ForceQuit with immediate process termination

```csharp
void ForceQuit()
{
    Debug.Log("[HeatupValidationVisual] ForceQuit - IMMEDIATE TERMINATION");
    
    // Stop time to prevent any further updates
    Time.timeScale = 0f;
    
    // Signal engine to stop immediately
    if (engine != null)
    {
        engine.RequestImmediateShutdown();
    }
    
    // Stop our own coroutines
    StopAllCoroutines();
    
    // Clean up GPU/native resources
    CleanupNativeResources();
    
    #if UNITY_EDITOR
        // In editor, use immediate stop (may still show brief hang)
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        // In build, forcefully terminate the process immediately
        // This bypasses all Unity cleanup which can hang
        System.Diagnostics.Process.GetCurrentProcess().Kill();
    #endif
}
```

#### 2E. Add OnApplicationQuit handler to Visual

```csharp
void OnApplicationQuit()
{
    Debug.Log("[HeatupValidationVisual] OnApplicationQuit");
    
    // Signal engine shutdown
    if (engine != null)
    {
        engine.RequestImmediateShutdown();
    }
    
    // Clean up native resources
    CleanupNativeResources();
}

void OnDestroy()
{
    Debug.Log("[HeatupValidationVisual] OnDestroy");
    CleanupNativeResources();
}
```

---

**File:** `HeatupValidationVisual.Styles.cs`

#### 2F. Add native resource cleanup method

```csharp
/// <summary>
/// v0.9.5: Clean up all native resources (textures, materials) to prevent
/// shutdown hangs and memory leaks. Call from OnDestroy and ForceQuit.
/// </summary>
internal void CleanupNativeResources()
{
    Debug.Log("[HeatupValidationVisual] CleanupNativeResources");
    
    // Destroy all cached textures
    DestroyTex(ref _bgTex);
    DestroyTex(ref _panelTex);
    DestroyTex(ref _headerTex);
    DestroyTex(ref _gaugeBgTex);
    DestroyTex(ref _graphBgTex);
    DestroyTex(ref _whiteTex);
    DestroyTex(ref _greenTex);
    DestroyTex(ref _amberTex);
    DestroyTex(ref _redTex);
    DestroyTex(ref _cyanTex);
    DestroyTex(ref _sectionHeaderTex);
    DestroyTex(ref _blueAccentTex);
    DestroyTex(ref _orangeAccentTex);
    DestroyTex(ref _textSecondaryTex);
    DestroyTex(ref _trace1Tex);
    DestroyTex(ref _trace2Tex);
    DestroyTex(ref _trace3Tex);
    DestroyTex(ref _trace4Tex);
    DestroyTex(ref _trace5Tex);
    DestroyTex(ref _trace6Tex);
    DestroyTex(ref _traceGridTex);
    DestroyTex(ref _tileBorderInactiveTex);
    DestroyTex(ref _annOffTex);
    DestroyTex(ref _annNormalTex);
    DestroyTex(ref _annWarningTex);
    DestroyTex(ref _annAlarmTex);
    
    // Destroy GL material
    if (_glMat != null)
    {
        DestroyImmediate(_glMat);
        _glMat = null;
    }
    
    // Reset initialization flag so resources aren't used after cleanup
    _stylesInitialized = false;
}

/// <summary>Helper to safely destroy a texture and null the reference.</summary>
static void DestroyTex(ref Texture2D tex)
{
    if (tex != null)
    {
        DestroyImmediate(tex);
        tex = null;
    }
}
```

---

### Stage 3: Fix Graph Display Delay

**File:** `HeatupSimEngine.cs`

#### 3A. Add initial data point immediately

In `InitializeSimulation()` (in HeatupSimEngine.Init.cs), add initial history point:

```csharp
// v0.9.5: Add initial data point immediately so graphs have something to display
AddHistory();
```

#### 3B. Faster history collection at simulation start

In `RunSimulation()`, modify the history interval logic:

```csharp
// v0.9.5: Faster history collection at start for responsive graphs
// First 5 sim-minutes: sample every 10 sim-seconds
// After: sample every 1 sim-minute (existing behavior)
float historyInterval = simTime < (5f / 60f) ? (10f / 3600f) : (1f / 60f);
historyTimer += dt;
if (historyTimer >= historyInterval)
{
    AddHistory();
    historyTimer = 0f;
}
```

---

**File:** `HeatupValidationVisual.Graphs.cs`

#### 3C. Lower data threshold for graph display

```csharp
partial void DrawGraphContent(Rect area, int tabIndex)
{
    if (engine == null) return;

    var timeData = engine.timeHistory;
    // v0.9.5: Changed from < 2 to < 1 to show graph immediately with first data point
    if (timeData == null || timeData.Count < 1)
    {
        GUI.Label(area, "  Initializing...", _statusLabelStyle);
        return;
    }
    // ... rest unchanged
}
```

---

## Files To Be Modified

| File | Changes |
|------|---------|
| `HeatupSimEngine.cs` | Add `_shutdownRequested` flag, `RequestImmediateShutdown()`, update lifecycle handlers |
| `HeatupSimEngine.Init.cs` | Add initial history point in `InitializeSimulation()` |
| `HeatupValidationVisual.cs` | Enhanced `ForceQuit()`, add `OnApplicationQuit`, `OnDestroy`, faster history at start |
| `HeatupValidationVisual.Styles.cs` | Add `CleanupNativeResources()`, `DestroyTex()` helper |
| `HeatupValidationVisual.Panels.cs` | Fix memory reporting to include native memory |
| `HeatupValidationVisual.Graphs.cs` | Lower data threshold from 2 to 1 |

---

## Validation Criteria

- [ ] Memory indicator shows ~400-500MB (matching Task Manager)
- [ ] X key exits immediately without freeze
- [ ] Window close button exits immediately without freeze (no "not responding")
- [ ] Graphs show data within 10-20 seconds of simulation start
- [ ] No "Uncached color" warnings in Console
- [ ] Console shows "CleanupNativeResources" message on exit
- [ ] Console shows "IMMEDIATE SHUTDOWN REQUESTED" message on exit

---

## Risk Assessment

| Change | Risk | Mitigation |
|--------|------|------------|
| `Process.Kill()` in builds | LOW | Only used in standalone builds, not editor |
| `_shutdownRequested` volatile flag | LOW | Standard threading pattern |
| `DestroyImmediate()` on textures | MEDIUM | Only called during shutdown, null checks |
| Faster history at start | LOW | Only affects first 5 minutes, then reverts |
| Lower graph threshold | LOW | Just changes empty state behavior |

---

## Implementation Order

1. **Stage 2F** - Add `CleanupNativeResources()` to Styles.cs (no risk, additive)
2. **Stage 2A-2C** - Add shutdown flag to HeatupSimEngine (no risk, additive)
3. **Stage 2D-2E** - Update Visual quit handlers (medium risk, test thoroughly)
4. **Stage 1** - Fix memory reporting (cosmetic, no risk)
5. **Stage 3** - Fix graph delay (low risk, improves UX)

---

**Awaiting user approval to implement.**
