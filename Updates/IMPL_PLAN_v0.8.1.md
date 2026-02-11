# Implementation Plan v0.8.1 — Application Exit Fix

**Date:** 2026-02-07  
**Type:** Patch (Bug Fix)  
**Priority:** HIGH  
**Scope:** Application Lifecycle

---

## Problem Summary

After running the simulator, ALT+F4 causes the application to freeze, requiring a forced shutdown. The X hotkey also fails to close the application properly.

### Root Cause Analysis

1. **Missing OnApplicationQuit Handler**: The `HeatupSimEngine` has no `OnApplicationQuit()` or `OnDestroy()` method to properly stop the simulation coroutine when Unity is closing.

2. **Infinite Loop in Coroutine**: The `RunSimulation()` coroutine ends with:
   ```csharp
   while (isRunning)
   {
       yield return null;
   }
   ```
   This loop never terminates because nothing sets `isRunning = false` when the application quits.

3. **Application.runInBackground = true**: This setting keeps Unity processing even during close attempts, which combined with the above issues causes the freeze.

4. **X Key Handler May Not Execute**: If the coroutine is blocking the main thread during physics calculations (in the inner `while (simTimeBudget >= dt)` loop), the `Update()` method with the X key handler may not get called.

---

## Expected Behavior

- ALT+F4 should close the application immediately
- X key should quit the application
- All coroutines should terminate cleanly
- Log files should be saved before exit (if practical)

---

## Proposed Fix

### Stage 1: Add Application Quit Handlers to HeatupSimEngine

**File:** `Assets/Scripts/Validation/HeatupSimEngine.cs`

Add proper lifecycle methods:

```csharp
/// <summary>
/// Called when the application is quitting. Stops all coroutines and
/// sets isRunning = false to allow the simulation loop to exit cleanly.
/// </summary>
void OnApplicationQuit()
{
    Debug.Log("[HeatupSimEngine] Application quitting - stopping simulation");
    isRunning = false;
    StopAllCoroutines();
}

/// <summary>
/// Called when this MonoBehaviour is destroyed. Ensures cleanup even
/// if destroyed before OnApplicationQuit (e.g., scene unload).
/// </summary>
void OnDestroy()
{
    isRunning = false;
}
```

### Stage 2: Add Application Quit Handler to HeatupValidationVisual

**File:** `Assets/Scripts/Validation/HeatupValidationVisual.cs`

Add quit handler for the visual dashboard:

```csharp
void OnApplicationQuit()
{
    Debug.Log("[HeatupValidationVisual] Application quitting");
    // Ensure engine stops if it hasn't already
    if (engine != null)
    {
        engine.StopSimulation();
    }
}
```

### Stage 3: Improve X Key Handling (Optional Enhancement)

The current X key handler in `Update()` should work, but for robustness, ensure it runs even if engine is null:

```csharp
// v0.8.1: Quit application with X key (moved before engine checks)
if (kb.xKey.wasPressedThisFrame)
{
    Debug.Log("[HeatupValidationVisual] X key pressed - quitting application");
    // Stop simulation first if running
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

## Files to Modify

| File | Changes |
|------|---------|
| `Assets/Scripts/Validation/HeatupSimEngine.cs` | Add OnApplicationQuit() and OnDestroy() |
| `Assets/Scripts/Validation/HeatupValidationVisual.cs` | Add OnApplicationQuit(), improve X key handler |

---

## Testing

After implementation:
1. Start the simulator
2. Press X key → Should quit immediately
3. Start the simulator again
4. Press ALT+F4 → Should quit immediately without freezing
5. Verify no error messages in logs about coroutines or null references

---

## Risk Assessment

**Risk Level:** LOW

- Changes are additive (new methods only)
- No physics or logic changes
- Standard Unity lifecycle pattern
- Worst case: no change in behavior (already broken)

---

## Version

This fix will be versioned as **v0.8.1** (patch release).
