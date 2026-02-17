# Changelog — IP-0031-A: Validation Dashboard Blue Screen & Runtime Fixes

**Date:** 2026-02-17  
**Parent IP:** IP-0031 (Validation Dashboard Visual Redesign)

---

## [IP-0031-A] — 2026-02-17

### Fixed

- **Blue screen when switching to Validation Dashboard via V key.** A `Start()` execution order race condition between `ValidationDashboardSceneSetup` and `ValidationDashboardController` caused the dashboard canvas to be disabled immediately after being enabled. `ValidationDashboardSceneSetup.Start()` built the dashboard and called `SetVisibility(true)`, but the dynamically-added `ValidationDashboardController.Start()` ran on the next frame and unconditionally called `SetVisibility(false)`, overriding the scene setup's visibility.

- **MissingComponentException spam on "Graph" GameObjects.** `MiniTrendStrip.Create()` added a `MaskableGraphic`-derived component to a new "Graph" GameObject without ensuring `CanvasRenderer` was present first. The `GraphicRaycaster` attempted to raycast against the Graphic before `CanvasRenderer` was registered, causing per-frame `MissingComponentException` errors.

### Changed

- **`ValidationDashboardController.Initialize()`** — Removed the `SetVisibility(false)` call. Visibility is now exclusively controlled by the launch path (`ValidationDashboardSceneSetup` or `ValidationDashboardLauncher`), not by the controller itself.

- **`MiniTrendStrip.Create()`** — Added explicit `CanvasRenderer` component to the "Graph" GameObject before adding `MiniTrendStrip` to prevent the `GraphicRaycaster` race condition.

### Files Modified

| File | Change |
|------|--------|
| `Assets/Scripts/UI/ValidationDashboard/ValidationDashboardController.cs` | Removed `SetVisibility(false)` from `Initialize()`, updated version to 1.0.1 |
| `Assets/Scripts/UI/ValidationDashboard/Trends/MiniTrendStrip.cs` | Added explicit `CanvasRenderer` before `MiniTrendStrip` in `Create()` |

### GOLD Standard Impact

None. No GOLD modules were modified.
