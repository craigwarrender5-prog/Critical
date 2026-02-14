# Changelog v2.0.1 — Input System Fix & Singleton Teardown Guard

**Date:** 2026-02-10  
**Version:** 2.0.1  
**Classification:** Bugfix  
**Matching Implementation Plan:** IMPLEMENTATION_PLAN_v2.0.1_InputSystemFix.md

---

## Summary

Fixes two runtime errors encountered during testing of the v2.0.0 Multi-Screen GUI system:

1. `InvalidOperationException` from legacy `Input.GetKeyDown()` call in ReactorOperatorScreen.cs (GOLD STANDARD)
2. `Some objects were not cleaned up when closing the scene` — ScreenManager singleton spawned during `OnDestroy()` callbacks

---

## Changes

### ReactorOperatorScreen.cs (GOLD STANDARD — amended with authorization)

- **Added** `using UnityEngine.InputSystem;`
- **Added** private `InputAction _toggleAction` field
- **Replaced** `Input.GetKeyDown(ToggleKey)` in `Update()` with New Input System `InputAction` created in `Start()`
  - Binding: `<Keyboard>/1`
  - Triggered via `performed` callback → `ToggleVisibility()`
- **Added** `OnDestroy()` to dispose the InputAction cleanly
- **Removed** the legacy `Input.GetKeyDown()` call entirely (was line 226)
- **Updated** header comments to reflect New Input System migration and CHANGE note

### ScreenManager.cs

- **Added** `private static bool _applicationQuitting` flag
- **Added** `OnApplicationQuit()` to set the flag on shutdown
- **Modified** `Instance` getter to return `null` when `_applicationQuitting` is true — prevents creating new GameObjects during scene teardown

### ScreenDataBridge.cs

- **Added** `private static bool _applicationQuitting` flag
- **Added** `OnApplicationQuit()` to set the flag on shutdown
- **Modified** `Instance` getter to return `null` when `_applicationQuitting` is true — same pattern as ScreenManager

### ReactorScreenAdapter.cs

- **Updated** header comments to reflect that ReactorOperatorScreen now uses its own New Input System InputAction (no longer inert)

---

## Files Modified

| File | Type | Change |
|------|------|--------|
| `Assets/Scripts/UI/ReactorOperatorScreen.cs` | GOLD STANDARD | Migrated `Input.GetKeyDown()` → New Input System `InputAction` |
| `Assets/Scripts/UI/ScreenManager.cs` | Infrastructure | Added `_applicationQuitting` guard to singleton |
| `Assets/Scripts/UI/ScreenDataBridge.cs` | Infrastructure | Added `_applicationQuitting` guard to singleton |
| `Assets/Scripts/UI/ReactorScreenAdapter.cs` | Adapter | Header comment update only |

## Files NOT Modified

All other files from v2.0.0 remain unchanged.

---

## Root Cause Analysis

### Error 1: `InvalidOperationException`

The project's Player Settings have `activeInputHandler: 1` (New Input System only). This disables the entire `UnityEngine.Input` class. `ReactorOperatorScreen.Update()` called `Input.GetKeyDown(ToggleKey)` which threw `InvalidOperationException` every frame.

**Fix:** Replaced with a programmatic `InputAction` bound to `<Keyboard>/1`, triggered via the `performed` callback. This is the same pattern used by `OperatorScreen` base class for its fallback input.

### Error 2: Singleton spawned during teardown

When Unity closes a scene, `OnDestroy()` is called on all MonoBehaviours in undefined order. `OperatorScreen.OnDestroy()` accesses `ScreenManager.Instance`, which auto-creates a new ScreenManager if the singleton has already been destroyed. This new GameObject survives scene cleanup, causing Unity's warning.

**Fix:** Added `OnApplicationQuit()` → sets `_applicationQuitting = true`. The `Instance` getter returns `null` during shutdown, preventing GameObject creation. The existing null check in `OperatorScreen.OnDestroy()` (`if (ScreenManager.Instance != null)`) now correctly skips unregistration during teardown.
