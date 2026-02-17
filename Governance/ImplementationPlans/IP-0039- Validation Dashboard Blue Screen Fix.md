# IP-0031-A: Validation Dashboard Blue Screen Fix

**Date:** 2026-02-17  
**Parent IP:** IP-0031 (Validation Dashboard Visual Redesign)  
**Status:** IMPLEMENTED  
**Priority:** Critical (dashboard non-functional)  
**Changelog Required:** Yes  

---

## 1. Problem Summary

When pressing V to switch to the new Validation Dashboard, the screen shows only a solid blue background (the MainScene camera's default clear color). The dashboard UI is not visible.

### 1.1 Root Cause

A `Start()` execution order race condition between `ValidationDashboardSceneSetup` and `ValidationDashboardController`.

**Sequence of events:**

1. V key pressed → `SceneBridge.SwitchToValidator()` loads Validator scene additively and hides the operator Canvas.
2. `ValidationDashboardSceneSetup.Start()` runs → calls `SetupNewDashboard()` → calls `ValidationDashboardBuilder.Build(engine)`.
3. `Build()` creates the Canvas and calls `canvasGO.AddComponent<ValidationDashboardController>()` — this queues the controller's `Start()` for a future frame.
4. `SetupNewDashboard()` calls `_newDashboard.SetVisibility(true)` — canvas is now enabled. ✅
5. **Next frame**: `ValidationDashboardController.Start()` runs → calls `Initialize()` → calls `SetVisibility(false)` — canvas is now disabled. ❌

The controller's `Start()` unconditionally hides the dashboard because it was designed for standalone launch (the `ValidationDashboardLauncher` path). When launched via `ValidationDashboardSceneSetup`, the `SetVisibility(false)` call in `Initialize()` overwrites the explicit `SetVisibility(true)` from the scene setup.

**Result:** The dashboard canvas is disabled. The MainScene camera renders with nothing to display except its default clear color (Unity default blue).

---

## 2. Expected Behavior

When pressing V to open the Validator scene:
- The operator Canvas hides (already working).
- The new uGUI Validation Dashboard canvas becomes visible immediately.
- The dashboard's dark background (`#0F1118`) fills the screen.
- Header, tab bar, overview panel, and mini-trends render correctly.

---

## 3. Proposed Fix

**Single-stage implementation.**

### 3.1 Fix: Remove automatic hide from `ValidationDashboardController.Initialize()`

**File:** `Assets/Scripts/UI/ValidationDashboard/ValidationDashboardController.cs`

**Change:** Remove the `SetVisibility(false)` line from `Initialize()`. The controller should not make assumptions about its own visibility. Visibility is the responsibility of the launch path:

- **SceneSetup path** (`ValidationDashboardSceneSetup`): Calls `SetVisibility(true)` after build.
- **Launcher path** (`ValidationDashboardLauncher`): Already controls visibility externally.

---

## 4. Verification

After implementation:

1. Press V from operator screens → Dashboard should appear immediately (dark background, header, Overview tab).
2. Press 1-8 / Esc from dashboard → Should return to operator screens correctly.
3. Press V again → Dashboard should reappear (SceneSetup rebuilds on each scene load per `OnDisable`/`OnEnable` logic).
4. Ctrl+1-7 while dashboard visible → Tab switching should work.
5. Time acceleration (F5-F9, +/-) → Should work while dashboard is visible.

---

## 5. Unaddressed Issues

None. This is a targeted fix for a single initialization bug. All other IP-0031 functionality is unaffected.

---

## 6. Risk Assessment

**Risk:** Low. The change removes one line of code and adds a comment. No logic paths, data flows, or physics modules are affected. The Launcher path already controls visibility externally, so removing the auto-hide does not break that path.

**GOLD Standard Impact:** None. No GOLD modules were modified.
