# Changelog v0.8.2 — Graph X-Axis and Rolling Window Fix

**Date:** 2026-02-07  
**Type:** Patch (Bug Fix)  
**Priority:** HIGH  
**Scope:** Dashboard Visualization

---

## Summary

Fixes broken trend graphs that were showing meaningless data. The X-axis now correctly displays a fixed 240-minute rolling window from "-240" to "NOW", with proper 1-minute sampling intervals.

---

## Problems Fixed

### 1. Wrong Sampling Interval
- **Was:** 5-minute samples (`1f/12f` hours)
- **Now:** 1-minute samples (`1f/60f` hours)

### 2. Wrong Buffer Size
- **Was:** 144 points (would be 144 × 5 = 720 minutes = 12 hours)
- **Now:** 240 points (240 × 1 = 240 minutes = 4 hours)

### 3. Wrong X-Axis Time Range
- **Was:** Dynamic range from `timeData[0]` to `timeData[last]` (constantly expanding/shifting)
- **Now:** Fixed 4-hour window relative to current simulation time

### 4. Wrong X-Axis Labels
- **Was:** Absolute simulation time (e.g., "0:00", "1:30", "3:00")
- **Now:** Relative time showing "-240", "-200", "-160", "-120", "-80", "-40", "NOW"

---

## Changes

### HeatupSimEngine.cs

```csharp
// Changed from:
const int MAX_HISTORY = 144;

// To:
const int MAX_HISTORY = 240;
```

```csharp
// Changed from:
if (historyTimer >= 1f / 12f)  // 5 minutes

// To:
if (historyTimer >= 1f / 60f)  // 1 minute
```

### HeatupValidationVisual.Graphs.cs

**DrawPlotArea()** — Fixed time window:
```csharp
// Changed from:
float tMin = timeData[0];
float tMax = timeData[timeData.Count - 1];

// To:
const float WINDOW_HOURS = 4.0f;  // 240 minutes
float tMax = engine.simTime;       // Current time = right edge (NOW)
float tMin = tMax - WINDOW_HOURS;  // 4 hours ago = left edge (-240)
if (tMin < 0f) tMin = 0f;
```

**DrawXAxisLabels()** — Relative time labels:
```csharp
// Now shows: -240, -200, -160, -120, -80, -40, NOW
// Instead of: 0:00, 0:40, 1:20, 2:00, etc.
```

**DrawTracesOnSecondaryAxis()** — Same fix applied for dual-axis graphs.

---

## Files Modified

| File | Changes |
|------|---------|
| `Assets/Scripts/Validation/HeatupSimEngine.cs` | MAX_HISTORY = 240, sampling interval = 1 minute |
| `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs` | Updated header comments |
| `Assets/Scripts/Validation/HeatupValidationVisual.Graphs.cs` | Fixed X-axis to 240-min window, relative time labels |

---

## Behavior After Fix

- **X-axis:** Shows "-240" on left edge, "NOW" on right edge
- **Window:** Always 4 hours (240 minutes), regardless of simulation duration
- **Sampling:** 1 data point per simulation minute
- **Buffer:** 240 points total (exactly fills the 4-hour window)
- **Scrolling:** Old data scrolls off the left as new data enters on the right
- **Early simulation:** If sim time < 4 hours, left edge shows 0 instead of negative time

---

## Visual Example

```
Before (broken):
  X-axis: 0:00 -------- 0:30 -------- 1:00 -------- 1:30
  (constantly expanding, traces move together)

After (fixed):
  X-axis: -240 ---- -160 ---- -80 ---- NOW
  (fixed window, traces show actual historical values)
```

---

---

## Additional Fix: Log File Naming and Cleanup

Restored proper log file naming convention and added automatic cleanup on startup.

### Problem
- Log filenames changed from `Heatup_Log_XXX_THH:MM.txt` format to `Heatup_Log_XXX_T{simTime:F2}hr.txt`
- Old log files accumulated across sessions

### Solution
- Restored filename format: `Heatup_Log_XXX_THH-MM.txt` (using dash instead of colon for Windows compatibility)
- Added `ClearLogDirectory()` method that deletes all `.txt` files from HeatupLogs folder on startup
- Cleanup runs automatically when simulation starts

### Changes

**HeatupSimEngine.cs:**
- Added `ClearLogDirectory()` method
- Modified `Start()` to call cleanup when log directory exists

**HeatupSimEngine.Logging.cs:**
- Changed filename format from `$"Heatup_Log_{logNum:D3}_T{simTime:F2}hr.txt"`
- To `$"Heatup_Log_{logNum:D3}_T{hours:D2}-{minutes:D2}.txt"`

### Example Filenames
- `Heatup_Log_000_T00-00.txt` (at simulation start)
- `Heatup_Log_004_T01-00.txt` (at 1 hour)
- `Heatup_Log_012_T03-00.txt` (at 3 hours)

---

## Additional Fix: Application Closing Overlay

Added visual feedback when pressing X to quit the application:

### Problem
- X key closes the application but takes time to shut down
- No visual indication that closing is in progress
- Repeated X presses could cause freeze

### Solution
- Added `_isClosing` flag to prevent multiple quit attempts
- Added "APPLICATION CLOSING" overlay with animated dots
- Overlay appears immediately when X is pressed
- Application quits after overlay renders (2-frame delay)

### Changes (HeatupValidationVisual.cs)
- Added `_isClosing` private field
- Modified Update() to check `!_isClosing` before processing X key
- Added `DelayedQuit()` coroutine for clean shutdown with overlay
- Added `DrawClosingOverlay()` method for visual feedback
- Added `DrawBoxBorder()` helper method
- Modified OnGUI() to draw overlay when `_isClosing` is true

---

## References

- Implementation Plan: `Updates and Changelog/IMPL_PLAN_v0.8.2.md`
