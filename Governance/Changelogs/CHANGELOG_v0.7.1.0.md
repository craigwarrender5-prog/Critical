# CHANGELOG v0.7.1.0

Date: 2026-02-17
Classification: Patch

## Scope
- Domain Plan: DP-0008 (Operator Interface & Scenarios)
- Implementation Plan: IP-0031

## Behavioral Impact Summary
- Resolved canvas orphan lifecycle preventing proper cleanup on Validator scene unload.
- Ensured EventSystem availability for uGUI pointer events when operator canvas is hidden.
- Eliminated blank-frame flash on tab switch by forcing immediate data refresh on panel activation.
- Removed F1 input conflict; dashboard visibility now exclusively managed by SceneBridge V key.
- Fixed ActiveIndicator positioning conflict with HorizontalLayoutGroup in tab navigation.
- Added singleton event delegate cleanup in OnDestroy to prevent dangling references.
- Added scene reload cycle handling in ValidationDashboardSceneSetup OnEnable/OnDisable.

## Governance Impact Summary
- IP-0031 remains PENDING APPROVAL (visual redesign scope unchanged; these are post-build architecture fixes).
- No CS closures associated with this patch.

## Files Modified
- ValidationDashboardController.cs — Removed F1 toggle, added OnDestroy delegate cleanup, updated header documentation.
- ValidationDashboardSceneSetup.cs — Canvas scene ownership via MoveGameObjectToScene, EnsureEventSystem method, OnEnable/OnDisable lifecycle for scene reload.
- TabNavigationController.cs — ActiveIndicator LayoutElement with ignoreLayout to prevent HorizontalLayoutGroup conflict.
- ValidationPanelBase.cs — Immediate OnUpdateData call in SetVisible when panel becomes active.
- ValidationDashboardLauncher.cs — Header clarified as alternative test launch path, not production flow.

## Validation Evidence
- Manual code review of all 17 panel files confirmed field references valid against HeatupSimEngine.
- Architecture trace confirmed canvas lifecycle, EventSystem resolution, and input ownership correct.

## Version Justification
Classified as Patch because this release addresses post-build defects in the IP-0031 dashboard architecture without introducing new features or breaking interface changes.
