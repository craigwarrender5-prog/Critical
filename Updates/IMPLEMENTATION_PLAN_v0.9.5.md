# Implementation Plan v0.9.5 — Critical Bug Fixes

**Date:** 2026-02-07  
**Version:** 0.9.5 (Patch)  
**Type:** Critical Bug Fix  
**Status:** READY FOR IMPLEMENTATION

---

## Critical Issues Identified

### Issue 1: Memory Reporting Discrepancy (502 MB actual vs 120 MB reported)

**Root Cause:** `Profiler.GetTotalAllocatedMemoryLong()` only reports managed heap memory, not:
- Native Unity memory
- GPU texture memory  
- Unmanaged allocations

**Fix:** Use `Profiler.GetTotalReservedMemoryLong()` + `Profiler.GetAllocatedMemoryForGraphicsDriver()` for accurate total.

### Issue 2: Application Exit Freeze

**Root Cause:** Multiple competing issues:
1. `ForceQuit()` uses `EditorApplication.isPlaying = false` which triggers normal shutdown sequence
2. Coroutines may still be running during cleanup
3. File I/O may be blocking (log file writes)
4. GL material/texture cleanup may be hanging

**Fix:** Aggressive shutdown with immediate process termination.

### Issue 3: Graphs Not Rendering (Shows "Collecting data...")

**Root Cause:** History is added every 1 sim-minute, but the screenshot shows only 30 sim-seconds elapsed. The data threshold `timeHistory.Count < 2` means graphs won't show until 2+ minutes of sim time.

**Fix:** Lower the threshold and/or add data more frequently at simulation start.

---

## Stage 1: Fix Memory Reporting

**File:** `HeatupValidationVisual.Panels.cs`

Change the memory display to show total process memory:

```csharp
// v0.9.5: Use total reserved memory + graphics memory for accurate reporting
long managedMem = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
long reservedMem = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong();
long graphicsMem = UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver();
float totalMB = (reservedMem + graphicsMem) / (1024f * 1024f);
```

---

## Stage 2: Fix Application Exit

**File:** `HeatupValidationVisual.cs`

Replace the existing ForceQuit with immediate termination:

```csharp
void ForceQuit()
{
    Debug.Log("[HeatupValidationVisual] ForceQuit - IMMEDIATE TERMINATION");
    
    // v0.9.5: Stop ALL Unity activity immediately
    Time.timeScale = 0f;
    
    // Stop simulation coroutines
    if (engine != null)
    {
        engine.StopAllCoroutines();
        engine.isRunning = false;
    }
    StopAllCoroutines();
    
    // Destroy textures to free GPU memory
    CleanupTextures();
    
    #if UNITY_EDITOR
        // In editor, use immediate stop
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        // In build, use immediate process exit
        System.Diagnostics.Process.GetCurrentProcess().Kill();
    #endif
}
```

**Add texture cleanup method to Styles.cs:**

```csharp
void CleanupTextures()
{
    // Destroy all cached textures
    if (_bgTex != null) { DestroyImmediate(_bgTex); _bgTex = null; }
    if (_panelTex != null) { DestroyImmediate(_panelTex); _panelTex = null; }
    // ... etc for all textures
    
    // Destroy GL material
    if (_glMat != null) { DestroyImmediate(_glMat); _glMat = null; }
    
    _stylesInitialized = false;
}
```

---

## Stage 3: Fix Graph Display Delay

**File:** `HeatupSimEngine.cs`

Reduce initial history interval from 1 minute to 10 seconds for first 5 minutes:

```csharp
// v0.9.5: Faster history collection at start for responsive graphs
float historyInterval = simTime < (5f / 60f) ? (10f / 3600f) : (1f / 60f);  // 10 sec early, 1 min later
historyTimer += dt;
if (historyTimer >= historyInterval)
{
    AddHistory();
    historyTimer = 0f;
}
```

**File:** `HeatupValidationVisual.Graphs.cs`

Lower the data threshold:

```csharp
partial void DrawGraphContent(Rect area, int tabIndex)
{
    if (engine == null) return;

    var timeData = engine.timeHistory;
    // v0.9.5: Changed from < 2 to < 1 to show graph frame immediately
    if (timeData == null || timeData.Count < 1)
    {
        GUI.Label(area, "  Initializing...", _statusLabelStyle);
        return;
    }
    // ...
}
```

---

## Files To Be Modified

| File | Changes |
|------|---------|
| `HeatupValidationVisual.cs` | Enhanced ForceQuit(), add OnDestroy cleanup |
| `HeatupValidationVisual.Styles.cs` | Add CleanupTextures() method |
| `HeatupValidationVisual.Panels.cs` | Fix memory reporting |
| `HeatupValidationVisual.Graphs.cs` | Lower data threshold |
| `HeatupSimEngine.cs` | Faster initial history collection |

---

## Validation Criteria

- [ ] Memory indicator matches Task Manager within ~50MB
- [ ] X key exits immediately without freeze
- [ ] Graphs show data within 10-20 seconds of simulation start
- [ ] No "Uncached color" warnings in Console

---

## Risk Assessment

| Change | Risk | Mitigation |
|--------|------|------------|
| Process.Kill() in builds | LOW | Only in non-editor builds |
| Faster history at start | LOW | Only affects first 5 minutes |
| Lower graph threshold | LOW | Just changes empty state message |
| Texture cleanup | MEDIUM | Test for null before destroy |

---

**Awaiting user approval to implement.**
