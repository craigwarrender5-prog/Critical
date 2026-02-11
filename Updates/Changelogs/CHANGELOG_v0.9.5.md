# Changelog v0.9.5 — Critical Memory Leak & Application Hang Fix

**Date:** 2026-02-07  
**Type:** Patch (Critical Bug Fix)  
**Priority:** CRITICAL  
**Scope:** Memory Management, Application Lifecycle

---

## Summary

Fixed critical issues causing:
1. **~500MB native memory accumulation** vs ~120MB reported
2. **Application hang on exit** (148-second timeout before Windows force-closes)
3. **Graph display delay** (no graphs shown for first 2+ minutes)

---

## Problem Analysis

### Issue 1: Memory Reporting Discrepancy

**Symptom:** Task Manager showed ~500MB memory usage while the in-app profiler reported only ~120MB.

**Root Cause:** `Profiler.GetTotalAllocatedMemoryLong()` only reports managed C# heap memory, excluding:
- Unity native memory allocations
- GPU texture memory
- Graphics driver allocations

**Fix:** Now using `GetTotalReservedMemoryLong() + GetAllocatedMemoryForGraphicsDriver()` for accurate total memory reporting that matches Task Manager.

### Issue 2: Application Hang on Exit

**Symptom:** Windows Event ID 1002 showing TerminationTime=148 seconds before Windows force-closed the application.

**Root Cause Chain:**
1. `RunSimulation()` coroutine runs in `while(isRunning)` loop
2. Setting `isRunning = false` doesn't immediately stop coroutine—waits for next `yield return`
3. If coroutine is mid-physics-step when quit is requested, it continues until reaching yield
4. `SaveReport()` performs file I/O which can block during shutdown
5. Native GL resources (textures, materials) never explicitly destroyed

**Fix:** Multi-layered solution:
- Added `_shutdownRequested` volatile flag checked at multiple points in coroutine
- Added `RequestImmediateShutdown()` method that sets flag BEFORE stopping coroutines
- Skip `SaveReport()` when shutdown is requested to avoid I/O blocking
- Added `CleanupNativeResources()` to explicitly destroy all textures and GL material
- In standalone builds, use `Process.Kill()` for immediate termination

### Issue 3: Graph Display Delay

**Symptom:** Graphs showed "Collecting data..." for first 2+ minutes of simulation.

**Root Cause:**
- History was only sampled every 1 sim-minute
- Graph required `Count >= 2` before rendering
- Initial data point not added at simulation start

**Fix:**
- Added initial history point in `InitializeSimulation()`
- Faster sampling (every physics step) for first 5 sim-minutes
- Lowered graph threshold from `< 2` to `< 1`

---

## Files Modified

| File | Changes |
|------|---------|
| `HeatupSimEngine.cs` | Added `_shutdownRequested` flag, `RequestImmediateShutdown()`, multiple shutdown checks in coroutine loop, skip SaveReport on shutdown, faster history collection at start |
| `HeatupSimEngine.Init.cs` | Added initial `AddHistory()` call in `InitializeCommon()` |
| `HeatupValidationVisual.cs` | Enhanced `ForceQuit()` with Process.Kill(), added `OnDestroy()`, calls `CleanupNativeResources()` |
| `HeatupValidationVisual.Styles.cs` | Added `CleanupNativeResources()` method, `DestroyTex()` helper |
| `HeatupValidationVisual.Panels.cs` | Fixed memory reporting to use total reserved + graphics memory |
| `HeatupValidationVisual.Graphs.cs` | Lowered data threshold from 2 to 1, updated empty state message |

---

## Technical Details

### Shutdown Flag Pattern

```csharp
// v0.9.5: Volatile flag for immediate termination
private volatile bool _shutdownRequested = false;

public void RequestImmediateShutdown()
{
    // Set flag FIRST so coroutine sees it immediately
    _shutdownRequested = true;
    isRunning = false;
    StopAllCoroutines();
}
```

### Multiple Shutdown Check Points

The coroutine now checks `_shutdownRequested` at:
1. Outer while loop condition
2. Before each physics step
3. After each physics step
4. Before yield return

### Native Resource Cleanup

```csharp
internal void CleanupNativeResources()
{
    // Destroy all 25+ cached textures
    DestroyTex(ref _bgTex);
    // ... all other textures ...
    
    // Destroy GL material
    if (_glMat != null)
    {
        DestroyImmediate(_glMat);
        _glMat = null;
    }
    
    _stylesInitialized = false;
}
```

### Memory Reporting Fix

```csharp
// v0.9.5: Total memory including native and GPU
long reservedMem = Profiler.GetTotalReservedMemoryLong();
long graphicsMem = Profiler.GetAllocatedMemoryForGraphicsDriver();
float totalMB = (reservedMem + graphicsMem) / (1024f * 1024f);
```

---

## Validation Criteria

- [x] Memory indicator shows total memory matching Task Manager (±50MB)
- [x] X key exits immediately without freeze
- [x] Window close button exits immediately without "not responding"
- [x] Graphs show data within 10 seconds of simulation start
- [x] Console shows "CleanupNativeResources" on exit
- [x] Console shows "IMMEDIATE SHUTDOWN REQUESTED" on exit
- [x] Console shows "Coroutine exiting cleanly" on normal exit

---

## Windows Event Log Expected Behavior

After this fix, closing the application should:
- **NOT** generate Event ID 1002 (Application Hang)
- Exit within 1-2 seconds
- Show clean shutdown messages in Unity console

---

## Notes

- The `Process.Kill()` approach in builds is aggressive but necessary—Unity's normal cleanup sequence can hang when coroutines are running
- The memory reported may still be less than Task Manager shows due to Unity's internal allocations not exposed through the Profiler API
- Graphs now start with a single data point and build up, rather than waiting for 2 points
