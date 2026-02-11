# Changelog v2.0.2 — ScreenManager Auto-Wiring & Toggle Guard

## [2.0.2] - 2026-02-10

### Fixed
- **ScreenInputActions asset not found at runtime** — `MultiScreenBuilder.EnsureScreenManager()` now automatically locates the `ScreenInputActions.inputactions` asset via `AssetDatabase.FindAssets()` and wires it to ScreenManager's `screenInputActions` field. No manual Inspector assignment required.
- **Pressing same screen key hides active screen (blue screen)** — Two-part fix:
  - `MultiScreenBuilder` now sets ScreenManager's `allowNoScreen = false`, preventing ScreenManager from hiding all screens when the active screen's key is pressed again.
  - ReactorOperatorScreen.cs (GOLD STANDARD, authorized amendment): `_toggleAction` callback changed from `ToggleVisibility()` to show-only guard — pressing key 1 when Screen 1 is already visible is now a no-op.

### Changed
- **MultiScreenBuilder.cs** — `EnsureScreenManager()` rewritten: now uses `SerializedObject` to wire InputActionAsset and set `allowNoScreen=false`. Also updates existing ScreenManager if re-run. Header updated to v2.0.2.
- **ReactorOperatorScreen.cs** (GOLD STANDARD) — `_toggleAction.performed` callback: `ToggleVisibility()` → `if (!_isVisible) SetVisible(true)`. Added CHANGE note v2.0.2 in header.

### Files Modified
- `Assets/Scripts/UI/MultiScreenBuilder.cs` — Auto-wire logic + allowNoScreen
- `Assets/Scripts/UI/ReactorOperatorScreen.cs` — Show-only toggle guard (GOLD STANDARD amendment)

### Verification Checklist
- [ ] Delete existing OperatorScreensCanvas + ScreenManager from scene
- [ ] Run Critical > Create All Operator Screens
- [ ] Confirm console shows: `[MultiScreenBuilder] Wired ScreenInputActions from Assets/InputActions/ScreenInputActions.inputactions`
- [ ] Press Play — Screen 1 visible by default
- [ ] Press 1 again — Screen 1 stays visible (no blue screen)
- [ ] Press 2 — Screen 2 shows, Screen 1 hides
- [ ] Press 1 — Screen 1 shows, Screen 2 hides
- [ ] Press Tab — Plant Overview shows
- [ ] No `ScreenInputActions InputActionAsset not found` error in console
