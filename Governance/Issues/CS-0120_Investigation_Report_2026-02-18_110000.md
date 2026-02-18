# CS-0120 Investigation Report

**Title:** F2 scenario selector keybind only functional in Validator view, not Operator Screens view  
**Severity:** LOW  
**Domain:** Operator Interface & Scenarios  
**Status:** READY  
**Created:** 2026-02-18T11:00:00Z  
**Assigned DP:** DP-0008  

---

## 1. Problem Summary

The F2 keybind for opening the scenario selector overlay only works when the user is in **Validator view** (after pressing V). When the user is on the default **Operator Screens view**, pressing F2 has no effect.

This limits discoverability and usability of the scenario selection feature, though the current impact is minimal given only one scenario (Validation Heatup Baseline) is currently registered.

---

## 2. Root Cause Analysis

### 2.1 Code Location

**File:** `Assets/Scripts/Core/SceneBridge.cs`  
**Method:** `Update()` (lines 122-149)

### 2.2 Findings

The `Update()` method uses a switch statement on `CurrentView` to handle input:

```csharp
switch (CurrentView)
{
    case ActiveView.OperatorScreens:
        // V key → switch to Validator
        if (kb.vKey.wasPressedThisFrame)
        {
            SwitchToValidator();
        }
        // ← NO F2 handling here
        break;

    case ActiveView.Validator:
        // F2 → toggle validator scenario selector overlay
        if (kb.f2Key.wasPressedThisFrame)
        {
            ToggleValidatorScenarioSelector();  // ← Only works here
        }
        // ...
        break;
}
```

F2 handling exists **only** in the `Validator` case. The `OperatorScreens` case has no F2 logic.

### 2.3 Why This Occurred

The F2 scenario selector was added as part of IP-0049 with the assumption that users would access it from the Validator view where the scenario overlay is rendered via `HeatupValidationVisual.DrawScenarioSelectorOverlay()`.

The design decision to route F2 through SceneBridge was correct (centralized input ownership), but the implementation missed the `OperatorScreens` case.

---

## 3. Current Workaround

Users can access the scenario selector by:
1. Press **V** to switch to Validator view
2. Press **F2** to open the scenario selector
3. Select and start a scenario
4. Press **1-8** or **Esc** to return to Operator Screens

---

## 4. Proposed Resolution

### Option A: Enable F2 from Operator Screens (Recommended)

Add F2 handling to the `OperatorScreens` case that:
1. Switches to Validator view (or loads it additively if not loaded)
2. Opens the scenario selector overlay

```csharp
case ActiveView.OperatorScreens:
    if (kb.vKey.wasPressedThisFrame)
    {
        SwitchToValidator();
    }
    // NEW: F2 opens scenario selector from any view
    else if (kb.f2Key.wasPressedThisFrame)
    {
        SwitchToValidator();
        // Note: May need delayed call or callback to open selector after scene load
    }
    break;
```

### Option B: Global F2 Outside Switch

Move F2 handling before/outside the switch statement:

```csharp
// Global F2 regardless of view
if (kb.f2Key.wasPressedThisFrame)
{
    if (CurrentView != ActiveView.Validator)
        SwitchToValidator();
    ToggleValidatorScenarioSelector();
}
```

### Considerations

- Async scene loading may require a callback or delayed invocation to ensure `HeatupValidationVisual` is available before calling `ToggleScenarioSelector()`
- May need to track intent (F2 pressed while loading) and trigger overlay once Validator is ready

---

## 5. Evidence

| File | Line(s) | Issue |
|------|---------|-------|
| `Assets/Scripts/Core/SceneBridge.cs` | 122-149 | F2 only handled in `Validator` case |
| `Assets/Scripts/Validation/HeatupValidationVisual.cs` | 425-428 | `ToggleScenarioSelector()` method exists and is functional |

---

## 6. Impact Assessment

- **User Impact:** LOW — Single workaround available (V then F2)
- **Technical Debt:** LOW — Localized fix in SceneBridge.cs
- **Blocking:** NONE — No other features depend on this

---

## 7. Acceptance Criteria

1. Pressing F2 from Operator Screens view opens the scenario selector
2. Pressing F2 from Validator view continues to toggle the scenario selector as before
3. No regression in V-key, 1-8, Tab, Esc navigation behavior
4. Scenario selection and start functionality preserved

---

## 8. Tags

- `Scenario-Selection`
- `F2-Keybind`
- `SceneBridge`
- `Input-Routing`
- `Low-Priority`
- `User-Request-2026-02-18`
