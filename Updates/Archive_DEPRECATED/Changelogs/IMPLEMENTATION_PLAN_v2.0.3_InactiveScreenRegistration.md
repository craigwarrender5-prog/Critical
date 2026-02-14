# Implementation Plan v2.0.3 — Legacy Input Fix & Inactive Screen Registration

## Problem Summary

### Issue 1: Legacy `Input.mousePosition` in CoreMosaicMap.cs
- **Error:** `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class, but you have switched active Input handling to Input System package`
- **Location:** `CoreMosaicMap.UpdateTooltip()` line 663 — `Input.mousePosition`
- **Root Cause:** Same class of bug as v2.0.1 (ReactorOperatorScreen). CoreMosaicMap is GOLD STANDARD and was written before the project switched to `activeInputHandler: 1`.
- **Impact:** Error spams every frame in console. Tooltip positioning broken.

### Issue 2: Screens 2 and Tab Never Register with ScreenManager
- **Symptom:** Pressing key 2 or Tab does nothing. Console shows only Screen 1 ("REACTOR CORE") registered.
- **Root Cause:** `MultiScreenBuilder` calls `screenGO.SetActive(false)` on Screen 2 (RCSPrimaryLoopScreen) and Plant Overview (PlantOverviewScreen) after building them. Unity does not call `Start()` on inactive GameObjects. `OperatorScreen.Start()` is where screens call `ScreenManager.Instance.RegisterScreen(this)`. Since `Start()` never runs, screens 2 and Tab are never registered — ScreenManager doesn't know they exist.
- **Impact:** Keys 2/Tab completely non-functional. Only Screen 1 works (it starts active).

## Expectations

1. No `Input.mousePosition` errors — tooltip tracks mouse correctly using New Input System.
2. All screens (including inactive ones) are registered with ScreenManager at startup.
3. Keys 1, 2, and Tab all switch screens correctly.
4. Console shows all 3 screens registered.

## Proposed Fix

### Fix 1: CoreMosaicMap.cs (GOLD STANDARD) — Replace Legacy Mouse API
- Add `using UnityEngine.InputSystem;`
- Replace `Input.mousePosition` with `Mouse.current != null ? Mouse.current.position.ReadValue() : Vector3.zero`
- Null-check `Mouse.current` for headless/CI environments.

### Fix 2: ScreenManager.cs — Discover Inactive Screens
- Add `RegisterInactiveScreens()` method called from `Start()` after `InitializeInputActions()`.
- Uses `FindObjectsOfType<Canvas>(true)` (the `true` parameter includes inactive objects).
- For each Canvas, calls `GetComponentsInChildren<OperatorScreen>(true)` to find ALL screens including inactive ones.
- Any screen not already in the `_screens` dictionary is registered.
- This runs before `ShowScreen(defaultScreenIndex)` so all screens are known when the default screen is shown.

### Why Not Change the Builder?
- Making screens start active but invisible (via CanvasGroup alpha=0) would also work, but would change visible behavior during the first frame and require coordinating CanvasGroup state with ScreenManager.
- Having ScreenManager discover inactive screens is cleaner, more robust, and works regardless of how screens are created.

### Fix 3: ScreenInputActions.inputactions — Remove Control Scheme Binding Restriction
- **Root Cause:** All 9 bindings had `"groups": "Keyboard&Mouse"`, restricting them to the Keyboard&Mouse control scheme. When ScreenManager enables the action map directly (without a PlayerInput component), no control scheme is resolved, so bindings with a specific `groups` value may silently fail to fire.
- **Fix:** Changed all 9 bindings from `"groups": "Keyboard&Mouse"` to `"groups": ""`. This makes bindings fire regardless of active control scheme.

### Fix 4: OperatorScreen.cs — Prevent Start() From Overriding ScreenManager Visibility
- **Root Cause:** When ScreenManager calls `ShowScreen(2)`, it calls `screen.Show()` → `SetVisible(true)` → `gameObject.SetActive(true)`. This triggers Unity to call `Start()` for the first time on the newly activated screen. `Start()` then calls `SetVisible(startVisible)` where `startVisible = false`, which immediately hides the screen that ScreenManager just showed.
- **Fix:** In `Start()`, check if the screen is already registered with ScreenManager and the GameObject is currently active. If so, skip the `SetVisible(startVisible)` call and just sync `isVisible = true`. This prevents the race condition where `Start()` undoes ScreenManager's `Show()` call.

## Files Modified

| File | Change | Classification |
|------|--------|---------------|
| `CoreMosaicMap.cs` | `Input.mousePosition` → `Mouse.current.position.ReadValue()` | **GOLD STANDARD** (authorized amendment) |
| `ScreenManager.cs` | Added `RegisterInactiveScreens()` using scene root scan | UI Infrastructure |
| `ScreenInputActions.inputactions` | Removed `Keyboard&Mouse` control scheme from all bindings | Input Asset |
| `OperatorScreen.cs` | `Start()` guards against overriding ScreenManager visibility | UI Base Class |

## Unaddressed Issues

- **CoreMapData validation errors** (RCCA count mismatch: expected 53, found 68; Bank SD/D/C count mismatches; fuel-only count mismatch). These are pre-existing data issues in CoreMapData.cs and are **not addressed** in this implementation. They do not block screen switching. Planned for a future data validation pass.
