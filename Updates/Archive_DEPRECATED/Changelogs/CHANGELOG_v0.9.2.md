# Changelog v0.9.2 — Application Exit Freeze Fix

**Date:** 2026-02-07  
**Type:** Patch (Bug Fix)  
**Priority:** HIGH  
**Scope:** Application Lifecycle Management

---

## Summary

Fixed application freeze when attempting to exit the simulator via X key, ALT+F4, or window close.

---

## Problem

When pressing X to exit or closing the application, it would freeze indefinitely due to:
1. Race conditions between `isRunning = false` and `StopAllCoroutines()`
2. Unity's `Application.Quit()` waiting for cleanup that could hang

---

## Solution

### HeatupSimEngine.cs

- Added centralized `ForceStop()` method that calls `StopAllCoroutines()` first, then sets `isRunning = false`
- Added `OnDisable()` lifecycle hook
- Updated `OnApplicationQuit()`, `OnDestroy()`, and `StopSimulation()` to use `ForceStop()`

### HeatupValidationVisual.cs

- Added `ForceQuit()` method using `System.Environment.Exit(0)` for immediate termination in builds
- Updated X key handler to use `ForceQuit()`

---

## Files Modified

| File | Changes |
|------|---------|
| `HeatupSimEngine.cs` | Added `ForceStop()`, `OnDisable()`, updated lifecycle methods |
| `HeatupValidationVisual.cs` | Added `ForceQuit()`, updated X key handler |
