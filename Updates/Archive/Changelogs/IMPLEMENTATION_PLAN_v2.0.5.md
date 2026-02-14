# Implementation Plan v2.0.5 — Application Freeze & Screen Flicker Fix

**Date:** 2026-02-10  
**Type:** Patch (Critical Bug Fix / Performance)  
**Priority:** CRITICAL  
**Scope:** HeatupValidationVisual (Styles partial, Core partial)  
**GOLD Standard Files Affected:** Yes — HeatupValidationVisual.cs, HeatupValidationVisual.Styles.cs

---

## Executive Summary

Two critical runtime issues have been confirmed through log analysis and code investigation:

1. **Application Freeze on Background** — App becomes permanently unresponsive when sent to background; requires force close.
2. **Screen Flicker Every 60 Simulated Seconds** — Visible frame time spike at 60-second simulation intervals.

Both issues share a single root cause: an **infinite logging loop** caused by an uncached color in `GetColorTex()`, amplified by the **missing OnGUI throttle** that was documented as implemented in v0.9.6 but never actually coded.

---

## Problem Summary

### Root Cause: Uncached `_cGaugeNeedle` Color

The color `_cGaugeNeedle` is defined in `HeatupValidationVisual.Styles.cs` as:

```csharp
internal Color _cGaugeNeedle = new Color(1.00f, 1.00f, 1.00f, 0.95f);
```

This color is **not present in the `GetColorTex()` cache lookup**. Every call to `DrawFilledRect()` with `_cGaugeNeedle` triggers:

```
[HeatupValidationVisual] Uncached color used in GetColorTex: RGBA(1.000, 1.000, 1.000, 0.950) - using white fallback
```

### Where `_cGaugeNeedle` Is Used

In `HeatupValidationVisual.Gauges.cs`, every gauge's center dot calls:

```csharp
DrawFilledRect(new Rect(center.x - 2f, center.y - 2f, 4f, 4f), _cGaugeNeedle);
```

This is called by:
- `DrawGaugeArc()` — used by **11 standard arc gauges** across 6 gauge groups
- `DrawGaugeArcBidirectional()` — used by **1 bidirectional arc gauge** (BRS Flow)

**Total: 12 gauge center dots × every OnGUI frame = 12 warning logs per frame.**

### Why This Causes the Freeze

- `Debug.LogWarning()` is synchronous — blocks the main thread until the log write completes.
- At 60 FPS with no throttle: **720 log writes per second**.
- When backgrounded, file I/O slows dramatically. Main thread blocks on log writes.
- Cannot process Windows focus messages → permanent "Not Responding" state.

### Why This Causes the Flicker

- At 60-second simulation marks, the engine triggers batch UI updates (phase transitions, alarm checks).
- More draw calls → more uncached `GetColorTex()` lookups → more logging.
- Frame time spikes → visible flicker/stutter.

### Missing OnGUI Throttle

The core file `HeatupValidationVisual.cs` declares:

```csharp
private float _lastRefreshTime;  // Declared but never used
```

Comments state `v0.9.6 PERF FIX: Throttle to refreshRate (eliminates 94% of redraws)` but **no throttling code exists in OnGUI()**. The method runs every frame (~60 FPS), meaning all rendering and all `GetColorTex()` calls execute at full frame rate instead of the intended 10 Hz refresh rate.

---

## Investigation Results

| Investigation Item | Status | Finding |
|---|---|---|
| **Find 95% alpha white usage** | ✅ Complete | `_cGaugeNeedle` in Styles.cs line ~29. Used in `DrawGaugeArc()` and `DrawGaugeArcBidirectional()` center dots. |
| **Is 0.95 alpha intentional?** | ✅ Yes | Represents a gauge needle/pointer — semi-transparent white is a deliberate aesthetic choice for control room instrument styling. |
| **Other uncached colors?** | ✅ Complete — None found | Full audit of all `DrawFilledRect()` calls across all 5 partials. All other colors passed to `DrawFilledRect` are properly cached. |
| **OnGUI throttle implemented?** | ✅ Confirmed missing | `_lastRefreshTime` declared, never used. No throttle code in `OnGUI()`. |
| **Player.log review** | ⚠️ Cannot access | Log file is outside allowed directory. Not critical for fix. |

---

## Expectations (Correct Behavior After Fix)

1. **No log spam** — `_cGaugeNeedle` hits the texture cache on every call. Zero `Debug.LogWarning` output during normal operation.
2. **No freeze on background** — With logging eliminated and OnGUI throttled, the main thread never blocks on I/O.
3. **No 60-second flicker** — With OnGUI throttled to 10 Hz, frame time remains stable regardless of simulation events.
4. **Visual appearance unchanged** — Gauge needles and center dots retain their semi-transparent white appearance (cached texture matches `_cGaugeNeedle` exactly).

---

## Proposed Fix — 3 Stages

### Stage 1: Cache `_cGaugeNeedle` Texture

**File:** `HeatupValidationVisual.Styles.cs` (GOLD)

**1a. Add texture field** in the `#region Textures` section, after the existing annunciator textures:

```csharp
internal Texture2D _gaugeNeedleTex;  // v2.0.5: Cache gauge needle color (0.95 alpha white)
```

**1b. Initialize texture** in `InitializeStyles()`, after `_annAlarmTex`:

```csharp
_gaugeNeedleTex = MakeTex(_cGaugeNeedle);  // v2.0.5: Fix infinite logging loop
```

**1c. Add cache lookup** in `GetColorTex()`, before the final fallback warning. Add after the annunciator alarm check:

```csharp
if (ColorsEqual(color, _cGaugeNeedle)) return _gaugeNeedleTex;
```

**1d. Add cleanup** in `CleanupNativeResources()`, after `_annAlarmTex` cleanup:

```csharp
DestroyTex(ref _gaugeNeedleTex);
```

**Validation:** Run application, verify zero instances of "Uncached color used in GetColorTex" in Player.log.

---

### Stage 2: Implement OnGUI Refresh Throttle

**File:** `HeatupValidationVisual.cs` (GOLD)

**2a. Add throttle logic** at the start of `OnGUI()`, immediately after the early return checks. Replace the existing comment block with actual implementation:

```csharp
void OnGUI()
{
    if (!dashboardVisible || engine == null) return;

    // v2.0.5: Actual implementation of OnGUI refresh throttle (was documented
    // in v0.9.6 but never coded). Only redraws at refreshRate Hz.
    // Event.current.type check ensures we always process Repaint when it's our
    // turn, but skip redundant Layout events between refreshes.
    if (Event.current.type == EventType.Layout)
    {
        float interval = 1f / refreshRate;
        if (Time.unscaledTime - _lastRefreshTime < interval)
            return;
        _lastRefreshTime = Time.unscaledTime;
    }

    // Cache screen dimensions
    _sw = Screen.width;
    _sh = Screen.height;
    // ... rest of OnGUI unchanged
```

**Design Note:** We throttle on `Layout` events (which precede `Repaint`) rather than on `Repaint` directly. This ensures that when Unity issues a `Repaint` event, we always honor it (preventing visual artifacts), but we skip the `Layout` pass that would trigger a subsequent `Repaint`. Using `Time.unscaledTime` ensures the throttle works correctly regardless of `Time.timeScale` or simulation speed.

**Validation:** Open Unity Profiler, confirm OnGUI executes ~10 times/second instead of ~60.

---

### Stage 3: Convert Warning Log to Conditional/One-Shot

**File:** `HeatupValidationVisual.Styles.cs` (GOLD)

**3a. Replace the unconditional `Debug.LogWarning` in `GetColorTex()` fallback** with a one-shot warning that only fires once per unique color. This provides diagnostic value during development without risking performance in production:

```csharp
// v2.0.5: One-shot warning per unique color (replaces per-frame spam)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
private static readonly System.Collections.Generic.HashSet<string> _warnedColors 
    = new System.Collections.Generic.HashSet<string>();
#endif

Texture2D GetColorTex(Color color)
{
    // ... existing cache lookups ...

    // v2.0.5: CRITICAL - DO NOT create new textures! This causes massive memory leak.
    // One-shot warning per unique color (was per-frame spam causing freeze)
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    string colorKey = $"{color.r:F3},{color.g:F3},{color.b:F3},{color.a:F3}";
    if (_warnedColors.Add(colorKey))
    {
        Debug.LogWarning($"[HeatupValidationVisual] Uncached color used in GetColorTex: {color} - using white fallback. Add this color to the cache!");
    }
    #endif
    return _whiteTex;
}
```

**Design Note:** The `HashSet.Add()` returns `false` if the element already exists, making this a clean one-shot pattern. Wrapped in `#if` so it compiles out of release builds entirely.

**Validation:** Run in Editor, verify warning appears exactly once. Run in build, verify zero log output from `GetColorTex`.

---

## Unaddressed Issues

| Issue | Reason | Disposition |
|---|---|---|
| **Player.log history review** | Log file is outside the allowed filesystem directory (`AppData\LocalLow`). | Low priority — not needed for fix. Can be reviewed manually by user. |
| **VSync interaction at 60-second marks** | The OnGUI throttle + logging fix should eliminate the flicker. VSync interaction is only relevant if flicker persists after this fix. | Deferred — test after v2.0.5 deployment. If flicker persists, add to Future_Features. |
| **Unity Profiler validation** | Requires running in Unity Editor with Profiler attached. Cannot be done from this environment. | User should validate after implementation. |

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|---|---|---|
| Throttle causes visual lag | Low | 10 Hz refresh is well above human perception threshold for instrument readings. `refreshRate` is exposed in Inspector for tuning. |
| Throttle breaks scroll/click interaction | Low | Layout events still process; only redundant layout passes are skipped. All GUI interaction events (click, scroll) still process normally since they are not `Layout` type. |
| Missed uncached color | Very Low | Full audit confirmed `_cGaugeNeedle` is the only uncached color. One-shot warning in Stage 3 provides safety net. |

---

## Files Modified

| File | GOLD | Changes |
|---|---|---|
| `Assets/Scripts/Validation/HeatupValidationVisual.Styles.cs` | Yes | Add `_gaugeNeedleTex` field, initialization, cache lookup, cleanup. Convert warning to one-shot. |
| `Assets/Scripts/Validation/HeatupValidationVisual.cs` | Yes | Implement OnGUI refresh throttle using `_lastRefreshTime`. |

---

## Implementation Order

1. **Stage 1** — Cache `_cGaugeNeedle` (eliminates root cause)
2. **Stage 2** — Implement OnGUI throttle (6x performance improvement)
3. **Stage 3** — Convert log warning to one-shot (safety net for future)

**Do not implement until explicitly instructed to proceed.**
