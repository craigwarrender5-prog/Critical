# Changelog v2.0.11

**Version:** 2.0.11  
**Date:** 2026-02-10  
**Classification:** BUGFIX + ARCHITECTURE — Scene Management, Input System & UI  
**Previous Version:** v2.0.10

---

## Summary

Resolved three issues preventing operator screen switching in native builds: isolated scene architecture with no cross-scene access, keyboard input conflict between time acceleration and screen switching, and missing RCS 3D model wiring. Introduced additive scene loading architecture enabling the Validator dashboard and Operator Screens to coexist with a shared, persistent simulation engine.

---

## Changes

### NEW: SceneBridge.cs — Scene Management Controller

**File:** `Assets/Scripts/Core/SceneBridge.cs`  
**Type:** New file — Core Infrastructure

- Created `SceneBridge` MonoBehaviour managing view switching between Operator Screens and Validator Dashboard
- **V key** loads Validator.unity additively, hides operator Canvas
- **Keys 1-8, Tab, Esc** unload Validator scene, restores operator Canvas
- Uses `DontDestroyOnLoad` for persistence across scene operations
- Async scene loading/unloading with transition guard (prevents double-press issues)
- Automatically finds and caches OperatorScreensCanvas for hide/show
- Re-resolves `ScreenDataBridge` sources after scene transitions
- Ensures `HeatupSimEngine` persistence via `DontDestroyOnLoad`
- State machine tracks `ActiveView` (OperatorScreens or Validator)

### MODIFIED: HeatupSimEngine.cs — Singleton + Persistence

**File:** `Assets/Scripts/Validation/HeatupSimEngine.cs`  
**Type:** GOLD — minor modification (lifecycle only, no physics changes)

- Added `Awake()` method with singleton enforcement
- Duplicate engines (from additive scene loads) self-destruct automatically
- Root-level GameObjects mark themselves `DontDestroyOnLoad`
- Engine runs continuously regardless of active view or scene operations
- Updated header comments documenting persistence architecture
- **No physics code modified**

### MODIFIED: HeatupValidationVisual.cs — Key Remapping

**File:** `Assets/Scripts/Validation/HeatupValidationVisual.cs`  
**Type:** GOLD — minor modification (key bindings only, no physics)

- Remapped time acceleration keys from digit 1-5 to F5-F9:
  - F5 = 1x Real-Time
  - F6 = 2x
  - F7 = 4x
  - F8 = 8x
  - F9 = 10x
- +/- keys retained as alternative time acceleration controls (unchanged)
- Frees keys 1-5 exclusively for ScreenManager operator screen switching
- **No physics or rendering code modified**

### EDITOR: Build Settings — Scene Order

- MainScene.unity moved to build index 0 (loads first in native builds)
- Validator.unity moved to build index 1 (available for additive loading)

### EDITOR: RCS Model — Inspector Wiring

- FBX file `Assets/Models/RCS/RCS_Primary_Loop.fbx` wired into `RCSPrimaryLoopScreen.rcsModelPrefab` Inspector field
- RCSVisualization layer verified/created

---

## Architecture

```
MainScene (always loaded, build index 0)
├── OperatorScreensCanvas (Screens 1-8, Tab)
├── ScreenManager + ScreenInputActions
├── ScreenDataBridge (singleton)
├── EventSystem
└── SceneBridge (DontDestroyOnLoad)
    └── HeatupSimEngine (runs continuously)

Validator.unity (loaded/unloaded additively via V key)
└── HeatupValidationVisual (OnGUI overlay, finds persistent engine)
```

### Key Bindings (Final)

| Key | Action |
|-----|--------|
| 1-8 | Switch operator screens (ScreenManager) |
| Tab | Plant Overview screen (ScreenManager) |
| V | Toggle Validator dashboard (SceneBridge) |
| Esc | Return from Validator to operator screens |
| F1 | Toggle Validator dashboard visibility (HeatupValidationVisual) |
| F5-F9 | Time acceleration: 1x, 2x, 4x, 8x, 10x |
| +/- | Increment/decrement time acceleration |

---

## Files Modified

| File | Change Type |
|------|-------------|
| `Assets/Scripts/Core/SceneBridge.cs` | NEW |
| `Assets/Scripts/Validation/HeatupSimEngine.cs` | GOLD — Awake() singleton + DontDestroyOnLoad |
| `Assets/Scripts/Validation/HeatupValidationVisual.cs` | GOLD — key remap 1-5 → F5-F9 |

## Editor Changes (No Code)

| Change | Location |
|--------|----------|
| Build order: MainScene index 0, Validator index 1 | File → Build Settings |
| RCS model FBX wired to Inspector field | RCSPrimaryLoopScreen component |
| RCSVisualization layer created | Project Settings → Tags and Layers |
| SceneBridge component added to MainScene | Scene hierarchy |
| HeatupSimEngine removed from Validator scene | Scene hierarchy |

---

## Matching Implementation Plan

`Critical\Updates\IMPLEMENTATION_PLAN_v2.0.11.md`
