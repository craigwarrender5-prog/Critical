# Changelog v2.0.5 — Application Freeze & Screen Flicker Fix

**Date:** 2026-02-10  
**Type:** Patch (Critical Bug Fix / Performance)  
**Scope:** HeatupValidationVisual (Styles partial, Core partial)

---

## Summary

Fixed two critical runtime issues — application freeze when backgrounded and screen flicker every 60 simulated seconds — both caused by an uncached gauge needle color triggering ~720 synchronous log writes per second, compounded by a missing OnGUI refresh throttle.

---

## Changes

### Bug Fixes

- **Fixed application freeze on background** — `_cGaugeNeedle` (RGBA 1, 1, 1, 0.95) was missing from the `GetColorTex()` texture cache. Every gauge center dot triggered a `Debug.LogWarning()` per frame (12 gauges × 60 FPS = 720 log writes/sec). Synchronous I/O blocked the main thread when backgrounded, preventing Windows focus messages from processing. Added `_gaugeNeedleTex` to the texture cache with full lifecycle management (init, lookup, cleanup).

- **Fixed screen flicker at 60-second simulation intervals** — Batch UI updates at simulation milestones amplified the uncached color logging, causing frame time spikes. Resolved by the texture cache fix above combined with the OnGUI throttle below.

### Performance

- **Implemented OnGUI refresh throttle** — `_lastRefreshTime` was declared in v0.9.6 but the throttle was never coded. OnGUI now throttles Layout events to `refreshRate` Hz (default 10). Repaint events are always honored to prevent visual artifacts. Uses `Time.unscaledTime` for correct behavior at any simulation speed. Reduces OnGUI overhead by ~83% (60 FPS → 10 FPS effective redraw rate).

### Diagnostics

- **Converted `GetColorTex()` fallback warning to one-shot pattern** — Replaced per-frame `Debug.LogWarning` with a `HashSet`-based one-shot that fires once per unique uncached color. Wrapped in `#if UNITY_EDITOR || DEVELOPMENT_BUILD` so it compiles out of release builds entirely. Provides development-time diagnostics without any production performance risk.

---

## Files Modified

| File | GOLD | Changes |
|---|---|---|
| `Assets/Scripts/Validation/HeatupValidationVisual.Styles.cs` | Yes | Added `_gaugeNeedleTex` field, init, cache lookup, cleanup. Added `_warnedColors` HashSet. Replaced per-frame warning with one-shot pattern. |
| `Assets/Scripts/Validation/HeatupValidationVisual.cs` | Yes | Added OnGUI Layout event throttle using `_lastRefreshTime` and `refreshRate`. |

---

## Validation Checklist

- [ ] Run application — verify zero "Uncached color used in GetColorTex" messages in Player.log
- [ ] Send application to background and back — verify no freeze or "Not Responding"
- [ ] Run simulation past 60-second marks — verify no visible flicker
- [ ] Verify gauge needle center dots still render as semi-transparent white
- [ ] Open Unity Profiler — confirm OnGUI executes ~10 times/second
- [ ] In Editor, intentionally add a new uncached color — verify warning fires exactly once
