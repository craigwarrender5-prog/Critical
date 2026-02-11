# Changelog v0.8.1 — Application Exit Fix

**Date:** 2026-02-07  
**Type:** Patch (Bug Fix)  
**Priority:** HIGH  
**Scope:** Application Lifecycle

---

## Summary

Fixes a critical bug where ALT+F4 caused the application to freeze, requiring a forced shutdown. The X hotkey also failed to close the application cleanly. This patch adds proper Unity lifecycle handlers to ensure clean shutdown.

---

## Problem

After running the simulator:
- **ALT+F4** caused the computer to freeze until the application was force-killed
- **X hotkey** did not reliably close the application
- Window close button (×) also caused hangs

### Root Cause

1. **Missing `OnApplicationQuit()` handler** — When Unity tried to quit, nothing set `isRunning = false`, so the simulation coroutine continued spinning indefinitely.

2. **Infinite loop in coroutine** — The `RunSimulation()` coroutine ends with:
   ```csharp
   while (isRunning)
   {
       yield return null;
   }
   ```
   This loop never terminated because `isRunning` was never set to `false` on quit.

3. **`Application.runInBackground = true`** — This setting keeps Unity processing even during close attempts, which combined with the above issues caused the freeze.

---

## Fix Applied

### HeatupSimEngine.cs

Added two new lifecycle methods after `Start()`:

```csharp
/// <summary>
/// v0.8.1: Called when the application is quitting. Stops all coroutines and
/// sets isRunning = false to allow the simulation loop to exit cleanly.
/// Fixes ALT+F4 freeze issue.
/// </summary>
void OnApplicationQuit()
{
    Debug.Log("[HeatupSimEngine] Application quitting - stopping simulation");
    isRunning = false;
    StopAllCoroutines();
}

/// <summary>
/// v0.8.1: Called when this MonoBehaviour is destroyed. Ensures cleanup even
/// if destroyed before OnApplicationQuit (e.g., scene unload).
/// </summary>
void OnDestroy()
{
    isRunning = false;
}
```

### HeatupValidationVisual.cs

Added quit handler and improved X key handling:

```csharp
/// <summary>
/// v0.8.1: Called when the application is quitting. Ensures the engine
/// stops cleanly even if quit is triggered by ALT+F4 or window close.
/// </summary>
void OnApplicationQuit()
{
    Debug.Log("[HeatupValidationVisual] Application quitting");
    if (engine != null)
    {
        engine.StopSimulation();
    }
}
```

Improved X key handler to stop simulation before quitting:

```csharp
// v0.8.1: Quit application with X key (improved - stop simulation first)
if (kb.xKey.wasPressedThisFrame)
{
    Debug.Log("[HeatupValidationVisual] X key pressed - quitting application");
    // Stop simulation first to ensure clean shutdown
    if (engine != null && engine.isRunning)
    {
        engine.StopSimulation();
    }
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    return;  // Don't process other input this frame
}
```

---

## Files Modified

| File | Changes |
|------|---------|
| `Assets/Scripts/Validation/HeatupSimEngine.cs` | Added `OnApplicationQuit()` and `OnDestroy()` methods |
| `Assets/Scripts/Validation/HeatupValidationVisual.cs` | Added `OnApplicationQuit()`, improved X key handler |

---

## Testing Checklist

After applying this patch, verify:

- [ ] Start simulator, press **X** → Application quits immediately
- [ ] Start simulator, press **ALT+F4** → Application quits immediately (no freeze)
- [ ] Start simulator, click window **×** button → Application quits cleanly
- [ ] Start simulator in Unity Editor, press **X** → Play mode stops
- [ ] Start simulator in Unity Editor, click **Stop** button → Play mode stops cleanly
- [ ] No error messages about coroutines or null references in console

---

## Technical Details

### Unity Lifecycle Order

When Unity quits, the lifecycle methods are called in this order:
1. `OnApplicationQuit()` — Application is about to quit
2. `OnDisable()` — MonoBehaviour is being disabled
3. `OnDestroy()` — MonoBehaviour is being destroyed

By setting `isRunning = false` in `OnApplicationQuit()`, the coroutine's `while (isRunning)` loop can exit on its next iteration, allowing Unity to shut down cleanly.

### Why StopAllCoroutines()?

Even though setting `isRunning = false` should cause the coroutine to exit naturally, calling `StopAllCoroutines()` provides an immediate, guaranteed stop. This is belt-and-suspenders defensive coding for a critical path.

---

## References

- Unity Documentation: [Application.Quit](https://docs.unity3d.com/ScriptReference/Application.Quit.html)
- Unity Documentation: [MonoBehaviour.OnApplicationQuit](https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnApplicationQuit.html)
- Implementation Plan: `Updates and Changelog/IMPL_PLAN_v0.8.1.md`

---

## Notes

- This is a critical patch that should be applied immediately
- No physics or simulation logic was changed
- Changes are purely additive (new lifecycle methods)
- Backward compatible with existing save files and configurations
