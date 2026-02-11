# Implementation Plan v2.0.2 — ScreenManager Auto-Wiring & Toggle Guard

## Problem Summary

Two runtime issues discovered after v2.0.1 bugfix:

### Issue 1: ScreenInputActions Asset Not Found
- **Error:** `[ScreenManager] ScreenInputActions InputActionAsset not found! Assign it in the Inspector or place it in a Resources folder. Screen keyboard switching will NOT work.`
- **Root Cause:** `EnsureScreenManager()` in MultiScreenBuilder.cs creates the ScreenManager GameObject and adds the component, but never wires the `screenInputActions` serialized field. The asset exists at `Assets/InputActions/ScreenInputActions.inputactions` but ScreenManager's `InitializeInputActions()` can't find it at runtime because it's not in a Resources folder and no Inspector assignment was made.
- **Impact:** ScreenManager keyboard switching (keys 1/2/Tab) completely non-functional. Only ReactorOperatorScreen's own InputAction (key 1) works.

### Issue 2: Pressing Same Screen Key Hides Active Screen
- **Symptom:** When Screen 1 is displayed, pressing key 1 again sets visibility to false, showing a blank blue screen with no operator screen visible.
- **Root Cause (Part A):** ReactorOperatorScreen.cs (GOLD STANDARD) `_toggleAction` callback calls `ToggleVisibility()` which unconditionally flips `_isVisible` — hiding Screen 1 when it's already visible.
- **Root Cause (Part B):** ScreenManager's `allowNoScreen` field defaults to `true`, allowing the same-key-press to hide the active screen via ScreenManager's own logic.
- **Impact:** User sees empty blue canvas with no way to recover except pressing a different screen key.

## Expectations

1. Running `Critical > Create All Operator Screens` should automatically find and wire the `ScreenInputActions.inputactions` asset to ScreenManager via `AssetDatabase.FindAssets()` — no manual Inspector assignment required.
2. `allowNoScreen` should be set to `false` by the builder, preventing ScreenManager from hiding all screens.
3. ReactorOperatorScreen's own key 1 action should only **show** Screen 1, never hide it. Pressing key 1 when Screen 1 is already visible should be a no-op.
4. ScreenManager coordinates mutual exclusion — pressing key 2 hides Screen 1 and shows Screen 2, etc.

## Proposed Fix

### Fix 1: MultiScreenBuilder.cs — Auto-wire ScreenInputActions Asset
- Modify `EnsureScreenManager()` to use `AssetDatabase.FindAssets("ScreenInputActions t:InputActionAsset")` to locate the asset.
- Wire it to ScreenManager's `screenInputActions` field via `SerializedObject`/`SerializedProperty`.
- Also set `allowNoScreen = false` via SerializedProperty.
- If asset is already wired (re-run), skip wiring.
- If asset not found, log warning with expected path.

### Fix 2: ReactorOperatorScreen.cs (GOLD STANDARD) — Show-Only Toggle
- Change `_toggleAction.performed` callback from `ToggleVisibility()` to a guard:
  - If `!_isVisible` → call `SetVisible(true)`
  - If already visible → no-op
- This prevents the GOLD STANDARD's own InputAction from hiding Screen 1.
- ScreenManager's action map handles all screen switching including hiding Screen 1 when another screen is selected.

## Files Modified

| File | Change | Classification |
|------|--------|---------------|
| `MultiScreenBuilder.cs` | Auto-wire ScreenInputActions + set allowNoScreen=false | Editor Tool |
| `ReactorOperatorScreen.cs` | Toggle callback → show-only guard | **GOLD STANDARD** (authorized amendment) |

## Unaddressed Issues

None. Both reported issues are fully addressed in this plan.
