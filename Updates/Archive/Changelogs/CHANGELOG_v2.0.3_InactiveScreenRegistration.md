# Changelog v2.0.3 — Legacy Input Fix & Inactive Screen Registration

## [2.0.3] - 2026-02-10

### Fixed
- **`InvalidOperationException: Input.mousePosition`** — CoreMosaicMap.cs (GOLD STANDARD, authorized amendment): Replaced `Input.mousePosition` with `Mouse.current.position.ReadValue()` using New Input System. Added `using UnityEngine.InputSystem;`. Null-safe for headless environments.
- **Keys 2 and Tab not switching screens** — Two root causes:
  1. Screens built by MultiScreenBuilder start `SetActive(false)`, preventing `Start()` from running and self-registration. Added `RegisterInactiveScreens()` to ScreenManager.Start() using scene root scan.
  2. All 9 bindings in ScreenInputActions.inputactions had `"groups": "Keyboard&Mouse"`, restricting them to a control scheme that ScreenManager never resolves (it uses the action map directly, not PlayerInput). Cleared groups to `""` so bindings fire unconditionally.
  3. `OnEnable()` runs before `Start()` in Unity's lifecycle. `InitializeInputActions()` creates `_screenActionMap` in `Start()`, but `OnEnable()` already ran (and skipped because map was null). The map was never enabled. Added `EnableInputActions()` call at the end of `InitializeInputActions()`.
- **OperatorScreen.Start() activation race** — When ScreenManager shows an inactive screen, `SetActive(true)` triggers `Start()` which called `SetVisible(startVisible=false)`, immediately hiding the screen. Added guard to skip `SetVisible` when ScreenManager has already shown the screen.

### Changed
- **CoreMosaicMap.cs** (GOLD STANDARD) — `UpdateTooltip()`: `Input.mousePosition` → `Mouse.current.position.ReadValue()`. Added CHANGE note v2.0.3 in header.
- **ScreenManager.cs** — Added `RegisterInactiveScreens()` method using scene root scan. Called from `Start()` after `InitializeInputActions()` and before `ShowScreen()`. Updated version to 2.0.3.
- **OperatorScreen.cs** — `Start()` now checks if ScreenManager has already shown this screen before applying `startVisible`. Prevents the race condition where `SetVisible(false)` in `Start()` undoes ScreenManager's `Show()` call on first activation. Updated version to 2.0.3.

### Files Modified
- `Assets/Scripts/UI/CoreMosaicMap.cs` — New Input System mouse position (GOLD STANDARD amendment)
- `Assets/Scripts/UI/ScreenManager.cs` — Inactive screen discovery via scene root scan
- `Assets/InputActions/ScreenInputActions.inputactions` — Removed control scheme from all 9 bindings
- `Assets/Scripts/UI/OperatorScreen.cs` — Start() visibility guard (prevents activation race)

### Not Addressed
- CoreMapData validation errors (RCCA/bank count mismatches) — pre-existing, planned for future data pass

### Verification Checklist
- [ ] No `InvalidOperationException: Input.mousePosition` errors
- [ ] Console shows: `[ScreenManager] Discovered and registered 2 inactive screen(s)`
- [ ] Console shows 3 screens registered (Screen 1, Screen 2, Screen 100/Tab)
- [ ] Key 1 → Reactor Core screen
- [ ] Key 2 → RCS Primary Loop screen
- [ ] Tab → Plant Overview screen
- [ ] Key 1 pressed while Screen 1 active → no change (stays visible)
- [ ] Tooltip follows mouse on Screen 1 core map
