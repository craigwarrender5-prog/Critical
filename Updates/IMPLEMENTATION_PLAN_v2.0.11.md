# Implementation Plan v2.0.11 — Scene Integration, Key Fix & RCS Model

**Version:** 2.0.11  
**Date:** 2026-02-10  
**Status:** PENDING APPROVAL  
**Classification:** BUGFIX + ARCHITECTURE — Scene Management, Input System & UI  
**Previous Version:** v2.0.10  
**GOLD Files Affected:** HeatupValidationVisual.cs (minor — key bindings only, no physics)

---

## Problem Summary

### Issue 1: Scenes Are Completely Isolated — No Cross-Scene Access

**Symptom:** Keys 1-8 don't switch operator screens in native builds. Validator and MainScene cannot coexist.

**Root Cause:** `Validator.unity` (build index 0) and `MainScene.unity` (build index 1) are completely independent. There is **zero scene loading code** in the entire project — no `SceneManager.LoadScene`, no additive loading, no `DontDestroyOnLoad`. In native builds, only Validator loads. Operator screens (in MainScene) are unreachable. There is no way to toggle between them.

### Issue 2: Keys 1-5 Conflict Between Time Acceleration and Screen Switching

**Root Cause:** `HeatupValidationVisual.cs` Update() reads keys 1-5 for time acceleration via `Keyboard.current.digitXKey.wasPressedThisFrame`. This conflicts with ScreenManager's InputAction bindings for Screen1-Screen5.

### Issue 3: RCS Screen Has No 3D Model

**Root Cause:** Inspector field `rcsModelPrefab` on `RCSPrimaryLoopScreen` is unwired. FBX exists at `Assets/Models/RCS/RCS_Primary_Loop.fbx`.

---

## Requirements

1. **MainScene loads first** in native builds (operator screens are primary experience)
2. **Press V** from MainScene → shows Validator dashboard overlay
3. **Press 1-8/Tab** from Validator → returns to operator screens
4. **HeatupSimEngine runs continuously** in background regardless of which view is active
5. **HeatupValidationVisual** renders as OnGUI overlay when Validator is visible
6. **Keys 1-5** are exclusively for screen switching; time acceleration uses F5-F9
7. **RCS model** displays in Screen 2 center panel

---

## Architecture Design

### Approach: Additive Scene Loading with Persistent Engine

```
MainScene (always loaded, build index 0)
├── OperatorScreensCanvas (Screens 1-8, Tab)
├── ScreenManager + ScreenInputActions
├── ScreenDataBridge (singleton, finds engine)
├── EventSystem
└── SimulationRoot (DontDestroyOnLoad)
    ├── HeatupSimEngine (runs continuously)
    └── SceneBridge (handles V key, additive loading)

Validator.unity (loaded/unloaded additively, build index 1)
├── HeatupValidationVisual (OnGUI overlay, finds persistent engine)
├── Camera (if needed — or reuse main camera)
└── [NO HeatupSimEngine — uses the persistent one from MainScene]
```

**Key architectural decisions:**
- `HeatupSimEngine` moves to a `DontDestroyOnLoad` GameObject in MainScene so it persists across additive scene loads/unloads
- New `SceneBridge.cs` script handles V key press → additive load Validator, hide Canvas; 1-8/Tab → unload Validator, show Canvas
- `HeatupValidationVisual` already calls `FindObjectOfType<HeatupSimEngine>()` in Start() — it will find the persistent engine automatically
- `ScreenDataBridge` already calls `FindObjectOfType<HeatupSimEngine>()` in ResolveSources() — works with persistent engine
- Validator scene's own `HeatupSimEngine` (if any) must be removed to avoid duplicates
- The OnGUI dashboard renders on top of everything, so no Canvas hiding is strictly needed when Validator is active — but hiding the operator Canvas avoids visual overlap

### Scene State Machine

```
[MainScene Active]  ──── V key ────►  [Validator Overlay Active]
  (Canvas visible)                      (Canvas hidden, OnGUI visible)
  (Engine running)                      (Engine running)
       ▲                                       │
       └──── 1-8/Tab/Esc ─────────────────────┘
```

---

## Proposed Fix

### STAGE 1: Create SceneBridge.cs — Scene Management Controller

**New file:** `Assets/Scripts/Core/SceneBridge.cs`  
**Classification:** New — Core Infrastructure

Creates a lightweight MonoBehaviour that:
- Lives on a `DontDestroyOnLoad` GameObject alongside HeatupSimEngine
- Listens for V key → loads Validator.unity additively, hides operator Canvas
- Listens for 1-8/Tab keys (only when Validator is loaded) → unloads Validator, shows Canvas
- Tracks which view is active (enum: OperatorScreens, Validator)
- Ensures HeatupSimEngine is on the DontDestroyOnLoad object

```csharp
// Pseudocode outline
public class SceneBridge : MonoBehaviour
{
    enum ActiveView { OperatorScreens, Validator }
    private ActiveView currentView = ActiveView.OperatorScreens;
    private bool validatorLoaded = false;
    
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    
    void Update()
    {
        if (kb.vKey.wasPressedThisFrame && currentView == ActiveView.OperatorScreens)
            ShowValidator();
            
        // When in Validator, screen keys return to operator screens
        // (ScreenManager handles the key press AND SceneBridge detects it)
        if (currentView == ActiveView.Validator)
        {
            if (AnyScreenKeyPressed())
                ShowOperatorScreens();
        }
    }
    
    void ShowValidator()
    {
        SceneManager.LoadSceneAsync("Validator", LoadSceneMode.Additive);
        FindOperatorCanvas()?.SetActive(false);  // Hide operator screens
        currentView = ActiveView.Validator;
    }
    
    void ShowOperatorScreens()
    {
        SceneManager.UnloadSceneAsync("Validator");
        FindOperatorCanvas()?.SetActive(true);   // Show operator screens
        currentView = ActiveView.OperatorScreens;
    }
}
```

**Key behaviors:**
- V key only works when viewing operator screens (prevents double-load)
- 1-8, Tab, Esc only trigger scene switch when Validator is active
- Canvas hide/show is instant; scene load is async but fast (Validator is lightweight)
- ESC from Validator returns to operator screens (replaces ForceQuit when not standalone)

---

### STAGE 2: Make HeatupSimEngine Persistent

**File:** `Assets/Scripts/Validation/HeatupSimEngine.cs`  
**Classification:** GOLD — minor modification (lifecycle only, no physics)

**Change:** Add `DontDestroyOnLoad` in Awake() with duplicate prevention.

```csharp
void Awake()
{
    // Singleton + persistence
    if (FindObjectsOfType<HeatupSimEngine>().Length > 1)
    {
        Destroy(gameObject);
        return;
    }
    DontDestroyOnLoad(gameObject);
}
```

**Why this is safe:** The engine is already scene-independent — it references no scene objects, uses no serialized scene references for physics, and all its modules are created programmatically. The `runOnStart` flag and Inspector fields work normally since they're set before the first scene loads.

---

### STAGE 3: Remap Time Acceleration Keys

**File:** `Assets/Scripts/Validation/HeatupValidationVisual.cs`  
**Classification:** GOLD — minor modification (key bindings only, no physics)

**Change:** Replace digit 1-5 → F5-F9 for time acceleration.

**Before:**
```csharp
if (kb.digit1Key.wasPressedThisFrame) engine.SetTimeAcceleration(0);
if (kb.digit2Key.wasPressedThisFrame) engine.SetTimeAcceleration(1);
if (kb.digit3Key.wasPressedThisFrame) engine.SetTimeAcceleration(2);
if (kb.digit4Key.wasPressedThisFrame) engine.SetTimeAcceleration(3);
if (kb.digit5Key.wasPressedThisFrame) engine.SetTimeAcceleration(4);
```

**After:**
```csharp
if (kb.f5Key.wasPressedThisFrame) engine.SetTimeAcceleration(0);  // F5 = 1x Real-Time
if (kb.f6Key.wasPressedThisFrame) engine.SetTimeAcceleration(1);  // F6 = 2x
if (kb.f7Key.wasPressedThisFrame) engine.SetTimeAcceleration(2);  // F7 = 4x
if (kb.f8Key.wasPressedThisFrame) engine.SetTimeAcceleration(3);  // F8 = 8x
if (kb.f9Key.wasPressedThisFrame) engine.SetTimeAcceleration(4);  // F9 = 10x
```

Also update header comments and any UI labels referencing "Keys 1-5".

---

### STAGE 4: Modify Validator Scene — Remove Duplicate Engine

**Scene:** `Assets/Scenes/Validator.unity`  
**Action:** This is a Unity Editor change the user performs.

When Validator is loaded additively, it must NOT contain its own HeatupSimEngine (the persistent one from MainScene is shared). Two options:

**Option A (recommended):** Remove HeatupSimEngine from the Validator scene hierarchy. HeatupValidationVisual's `Start()` already calls `FindObjectOfType<HeatupSimEngine>()` and will find the persistent one.

**Option B:** Keep the engine in Validator but add duplicate detection — the Awake() singleton pattern from Stage 2 handles this automatically (duplicate destroys itself).

**Also:** Remove or disable any Camera in Validator scene if it conflicts with MainScene's camera. The OnGUI dashboard doesn't need its own camera — it renders in screen space.

---

### STAGE 5: Update Build Settings

**Location:** Unity Build Settings (File → Build Settings)

| Before | After |
|--------|-------|
| 0: Validator.unity | 0: MainScene.unity |
| 1: MainScene.unity | 1: Validator.unity |

Both scenes must remain in the build list (Validator is loaded additively at runtime). MainScene must be index 0 so it loads first.

---

### STAGE 6: RCS Model Prefab — Inspector Wiring

**File:** No code change needed.  
**Action:** User wires FBX in Unity Inspector.

1. Select the GameObject with `RCSPrimaryLoopScreen` component
2. Drag `Assets/Models/RCS/RCS_Primary_Loop.fbx` into **"Rcs Model Prefab"** field
3. Ensure **"RCSVisualization"** layer exists (create in Project Settings → Tags and Layers if missing)
4. Adjust Camera Distance/Height if model appears wrong size

**Fallback:** Create prefab from FBX instance if direct FBX reference doesn't work.

---

### STAGE 7: Documentation

- Update header comments in HeatupValidationVisual.cs (key mappings)
- Update header comments in HeatupSimEngine.cs (DontDestroyOnLoad)
- Add header to new SceneBridge.cs
- Create CHANGELOG_v2.0.11.md

---

## Unaddressed Issues

| Issue | Reason | Tracking |
|-------|--------|----------|
| Validator standalone mode (Editor-only) | Can still open Validator.unity directly in Editor and press Play — engine creates locally. Only affects additive mode in builds. | — |
| RCS model materials/texturing | Out of scope — cosmetic | — |
| ScreenDataBridge re-resolution after additive load | ScreenDataBridge.ResolveSources() runs on Start(). May need a manual re-call after engine becomes persistent. SceneBridge can trigger this. | Handled in Stage 1 |
| ESC behavior change in Validator | Currently ForceQuit in builds. Will change to "return to operator screens" when loaded additively. Standalone Validator in Editor keeps ForceQuit. | Stage 1 handles both cases |

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Additive scene load causes duplicate GameObjects | Medium | Singleton patterns + duplicate detection |
| OnGUI dashboard overlaps operator Canvas | Low | Canvas hidden when Validator active |
| Engine state lost during scene transitions | None | DontDestroyOnLoad prevents destruction |
| Validator scene camera conflicts with main camera | Low | Disable/remove Validator camera |
| F5-F9 conflict with other systems | Low | No other system uses F-keys except F1 |

---

## Files Modified

| File | Stage | Type |
|------|-------|------|
| **SceneBridge.cs** (NEW) | 1 | New — Core Infrastructure |
| HeatupSimEngine.cs | 2 | GOLD — lifecycle only (DontDestroyOnLoad) |
| HeatupValidationVisual.cs | 3 | GOLD — key bindings only |

## Editor/Build Changes (No Code)

| Change | Stage | Type |
|--------|-------|------|
| Validator scene — remove duplicate engine | 4 | Unity Editor — scene hierarchy |
| Build Settings — swap scene order | 5 | Unity Editor — build settings |
| RCSPrimaryLoopScreen Inspector field | 6 | Unity Editor — drag FBX into field |

---

**END OF IMPLEMENTATION PLAN v2.0.11**
