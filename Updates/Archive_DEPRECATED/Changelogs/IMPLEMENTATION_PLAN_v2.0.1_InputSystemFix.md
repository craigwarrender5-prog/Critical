# Implementation Plan v2.0.1 — Input System Fix & Singleton Teardown Guard

**Date:** 2026-02-10  
**Version:** 2.0.1  
**Classification:** Bugfix  
**Status:** IMPLEMENTED

---

## Problem Summary

Two runtime errors during testing of v2.0.0 Multi-Screen GUI:

1. **`InvalidOperationException`** — `ReactorOperatorScreen.Update()` (line 226) calls `Input.GetKeyDown(ToggleKey)` which is illegal under `activeInputHandler: 1` (New Input System only). Throws every frame, breaking Screen 1 entirely.

2. **"Some objects were not cleaned up"** — `ScreenManager` singleton auto-creates a new GameObject when accessed from `OnDestroy()` during scene teardown. The new GameObject outlives the scene, triggering Unity's cleanup warning.

---

## Expectations

- No `InvalidOperationException` from `UnityEngine.Input` calls anywhere in the UI codebase
- Screen 1 (Reactor Core) toggles correctly with Key 1 via New Input System
- No orphaned GameObjects when closing scenes or exiting Play mode
- All singleton access safe during application shutdown

---

## Proposed Fix

### Fix 1: ReactorOperatorScreen.cs (GOLD STANDARD — amended with user authorization)

- Add `using UnityEngine.InputSystem;`
- Add private `InputAction _toggleAction` field
- In `Start()`: Create an `InputAction` bound to `<Keyboard>/1`, subscribe to `performed` → `ToggleVisibility()`, enable it
- In `Update()`: Remove the `Input.GetKeyDown(ToggleKey)` call entirely
- Add `OnDestroy()`: Disable and dispose the `InputAction`

### Fix 2: ScreenManager.cs — Application quit guard

- Add `private static bool _applicationQuitting = false;`
- Add `OnApplicationQuit()` → sets flag to `true`
- Modify `Instance` getter: if `_applicationQuitting`, return `null` immediately

### Fix 3: ScreenDataBridge.cs — Same pattern

- Same `_applicationQuitting` guard as ScreenManager (both are auto-creating singletons)

### Fix 4: ReactorScreenAdapter.cs — Comment update

- Update header to reflect that ReactorOperatorScreen now has its own working InputAction

---

## Unaddressed Issues

None. All issues in this bugfix are fully resolved.

---

## Testing Checklist

- [ ] Press Play — no `InvalidOperationException` in Console
- [ ] Press `1` — Screen 1 (Reactor Core) toggles on/off
- [ ] Press `2` — Screen 2 (RCS Primary Loop) shows, Screen 1 hides
- [ ] Press `Tab` — Plant Overview shows
- [ ] Exit Play mode — no "objects were not cleaned up" warning
- [ ] Close scene — no orphaned ScreenManager or ScreenDataBridge objects
