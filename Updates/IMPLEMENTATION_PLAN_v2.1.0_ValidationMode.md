# Implementation Plan v2.1.0 — Validation Mode (Heatup-Driven Screen Testing)

**Version:** 2.1.0  
**Date:** 2026-02-10  
**Status:** PENDING APPROVAL  
**Classification:** UI — Validation Infrastructure

---

## Problem Summary

The new operator screens (Screens 1–8, Tab/Overview) have been created but have not been tested with live simulation data. The existing HeatupSimEngine provides a complete, validated physics simulation that drives the HeatupValidationVisual OnGUI dashboard. However, there is currently no way to run the heatup simulation while simultaneously viewing the UGUI operator screens.

Additionally, while screen controls (buttons, switches, indicators) are currently non-interactive ("Visual Only"), they do not accurately reflect the current simulation state. For example, the PZR heater buttons should visually show ON/OFF based on whether the engine's heaters are actually energized.

---

## Expectations (Correct Behavior)

### V Key Behavior — View Switching Only (NOT a Start/Stop Toggle)

The V key is a **view switch**, not a simulation toggle. It follows the same pattern as keys 1–8: it selects which view to display. The simulation lifecycle is separate.

**Complete key map during Validation Mode:**

| Key | Action | Simulation Effect |
|-----|--------|------------------|
| **V** (first press) | Starts simulation, shows OnGUI dashboard | Starts engine (one-time only) |
| **V** (subsequent) | Switches view to OnGUI dashboard | None — simulation continues |
| **1–8** | Switches view to operator screen | None — simulation continues |
| **Tab** | Switches view to Plant Overview | None — simulation continues |
| **+/−** | Changes time acceleration | Speed change only — simulation continues |
| **ESC** | Quits application | Stops simulation, exits |

**Critical rule:** After the first V press starts the simulation, the engine runs continuously until application exit. No key press — including V — ever stops the simulation. V simply means "show me the dashboard." Keys 1–8 mean "show me that screen." They are all view switches over the same running engine.

### User Workflow Example

1. Press **V** → Simulation starts from Cold Shutdown. OnGUI dashboard appears showing heatup progress.
2. Press **3** → Dashboard hides. Pressurizer screen appears. Gauges show live PZR data. Heater button shows ON (green). Water level animates in vessel cutaway.
3. Press **4** → Pressurizer screen hides. CVCS screen appears. Charging/letdown flows update live. CCP-A indicator shows RUN.
4. Press **V** → CVCS screen hides. Dashboard reappears. Can compare dashboard data with what screens showed.
5. Press **+** → Time acceleration increases. Both dashboard and screens (when shown) update faster.
6. Press **5** → Dashboard hides. SG screen appears. Secondary temps updating. Stratification delta-T visible.
7. Press **2** → SG hides. RCS Primary Loop screen appears. Watch RCP indicators change from STOPPED → RUNNING as pumps start sequentially.
8. Press **V** → Back to dashboard for full system overview.
9. Press **ESC** → Application exits. Simulation stops.

At no point during steps 2–8 did the simulation pause, restart, or reset. The engine ran continuously in the background.

### Control State Reflection

All screen controls (buttons, switches, indicators), although disabled for manual interaction, must accurately reflect the current simulation state:

- PZR heater buttons → ON/OFF based on `engine.pzrHeatersOn`
- RCP indicators → RUNNING/STOPPED per pump based on `engine.rcpRunning[]`
- CCP indicators → RUN/STANDBY based on `engine.chargingActive`
- Steam dump indicators → OPEN/CLOSED based on `engine.steamDumpActive`
- PORV/SV indicators → respond to pressure thresholds
- Letdown path indicators → reflect `engine.letdownActive`
- Flow line colors → active/inactive based on actual flow values

---

## Proposed Fix — Detailed Technical Plan

### Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    ScreenManager                         │
│  Routes ALL keys: 1-8 → Screens, Tab → Overview,       │
│                   V → Dashboard, +/- → Time Accel       │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────────┐   ┌───────────────────────────────┐  │
│  │ HeatupSim    │   │   Operator Screens (UGUI)     │  │
│  │ Engine       │──▶│   Read via ScreenDataBridge    │  │
│  │ (coroutine   │   │   ┌─────┐ ┌────┐ ┌────┐      │  │
│  │  runs non-   │   │   │Scr 2│ │Sc 3│ │Sc 4│ ...  │  │
│  │  stop)       │   │   └─────┘ └────┘ └────┘      │  │
│  └──────┬───────┘   └───────────────────────────────┘  │
│         │                                               │
│         ▼                                               │
│  ┌──────────────┐                                       │
│  │ Validation   │   ◀── V key shows/hides this         │
│  │ Visual       │                                       │
│  │ (OnGUI)      │                                       │
│  └──────────────┘                                       │
│                                                         │
│  ┌──────────────┐                                       │
│  │ Validation   │   Manages V-key state:                │
│  │ Mode         │   - First V: StartSimulation()        │
│  │ Controller   │   - After: Toggle dashboard visible   │
│  └──────────────┘   - Never stops simulation            │
└─────────────────────────────────────────────────────────┘
```

The key insight: **ScreenDataBridge already reads from HeatupSimEngine.** The screens already update gauges from ScreenDataBridge in their `Update()` methods. What's needed:

1. A **ValidationModeController** script that manages first-start and view switching
2. A **"Validation" action** added to ScreenInputActions for the V key
3. Ensure the HeatupSimEngine and HeatupValidationVisual GameObjects exist in the scene
4. **ScreenDataBridge expansion** — add getters for control states
5. **Screen control state updates** — each screen updates its control visuals from ScreenDataBridge
6. **Key conflict resolution** — number keys go to screens only, +/− for time acceleration

---

### Stage 1: Add V-Key Binding to ScreenInputActions

**Files Modified:**
- `Assets/InputActions/ScreenInputActions.inputactions`

**Changes:**
- Add a new action `"Validation"` to the `OperatorScreens` action map
- Bind it to `<Keyboard>/v`
- This keeps all input routing through one centralized asset

---

### Stage 2: Create ValidationModeController Script

**Files Created:**
- `Assets/Scripts/UI/ValidationModeController.cs`

**State Machine:**

```
┌─────────────┐    V pressed     ┌──────────────────┐
│   INACTIVE   │ ───────────────▶ │  ACTIVE:DASHBOARD │
│ (no engine)  │                  │  engine running    │
└─────────────┘                  │  dashboard visible │
                                  └──────┬───────────┘
                                         │
                              1-8/Tab    │    V pressed
                              pressed    │
                                  ┌──────▼───────────┐
                                  │  ACTIVE:SCREEN    │
                                  │  engine running   │◀──── 1-8/Tab pressed
                                  │  screen visible   │────▶ (switches screen)
                                  │  dashboard hidden │
                                  └──────┬───────────┘
                                         │
                                  V      │
                                  pressed│
                                         ▼
                                  ┌──────────────────┐
                                  │  ACTIVE:DASHBOARD │
                                  │  (back to top)    │
                                  └──────────────────┘

All ACTIVE states: engine.isRunning == true, never changes until ESC/quit.
```

**Responsibilities:**

1. **INACTIVE → ACTIVE transition (first V press only):**
   - Finds HeatupSimEngine in scene
   - Calls `engine.StartSimulation()` — one time only, never called again
   - Sets `visual.dashboardVisible = true`
   - Hides any active operator screen via ScreenManager
   - Sets internal state to `ACTIVE`
   - Logs: `"[ValidationMode] STARTED — simulation running"`

2. **ACTIVE + V pressed (dashboard toggle):**
   - If dashboard is hidden: show dashboard, hide current operator screen
   - If dashboard is visible: hide dashboard (user can then press 1–8 to pick a screen)
   - **Does NOT call StartSimulation() or StopSimulation()** — engine is already running

3. **ACTIVE + 1–8/Tab pressed (screen switch):**
   - This is handled by ScreenManager as normal — no special logic needed
   - Dashboard auto-hides when any operator screen becomes visible
   - Engine continues running — completely unaffected

4. **ACTIVE + +/− pressed (time acceleration):**
   - Routes to `engine.SetTimeAcceleration()` 
   - Works in both dashboard and screen views

5. **Application quit / ESC:**
   - Engine cleanup happens via existing `OnApplicationQuit()` / `OnDestroy()` — no changes needed

**What ValidationModeController does NOT do:**
- It never calls `StopSimulation()`
- It never calls `SetActive(false)` on the engine or visual GameObjects
- It never interferes with ScreenManager's screen switching
- It never intercepts keys 1–8 — those always go to ScreenManager

**OnGUI Indicator:**
- When ACTIVE, draws a small bar at the top of the screen:  
  `"▶ VALIDATION MODE | V: Dashboard | 1-8: Screens | +/-: Speed | ESC: Quit"`
- Uses OnGUI so it appears over both the dashboard and UGUI screens
- Green text on dark background, minimal footprint

---

### Stage 3: Update MultiScreenBuilder to Include Validation Objects

**Files Modified:**
- `Assets/Scripts/UI/MultiScreenBuilder.cs`

**Changes:**
- Add a new method `BuildValidationMode()` called from the main `CreateAllScreens()` menu entry
- Creates the following hierarchy:

```
ValidationMode (GameObject, always active)
├── ValidationModeController (component)
├── HeatupSimEngine_Validation (GameObject, always active)
│   └── HeatupSimEngine (component)
│       - runOnStart = false
│       - coldShutdownStart = true
│       - startTemperature = 100
│       - startPressure = 400
│       - startPZRLevel = 25
└── HeatupValidationVisual_Validation (GameObject, always active)
    └── HeatupValidationVisual (component)
        - engine = (reference to above HeatupSimEngine)
        - showOnStart = false
        - dashboardVisible = false
```

**CRITICAL:** All GameObjects start ACTIVE and remain ACTIVE forever. The simulation doesn't run until `StartSimulation()` is called (because `runOnStart = false`). The dashboard doesn't render until `dashboardVisible = true` (the OnGUI method returns early). This means the objects exist in the scene doing nothing until V is pressed — zero overhead.

**ScreenDataBridge auto-discovery:** The existing `ScreenDataBridge.ResolveSources()` already calls `FindObjectOfType<HeatupSimEngine>()`. Since the engine GameObject is always active, it will be found automatically. No manual wiring needed between ScreenDataBridge and the engine.

---

### Stage 4: Add Validation Action to ScreenManager Routing

**Files Modified:**
- `Assets/Scripts/UI/ScreenManager.cs`

**Changes:**

1. Add to `_actionNameToScreenIndex` dictionary:
   ```csharp
   { "Validation", VALIDATION_INDEX }
   ```
   where `public const int VALIDATION_INDEX = 200;`

2. Modify `OnScreenActionPerformed()` to handle validation routing:
   ```csharp
   private void OnScreenActionPerformed(string actionName)
   {
       if (_actionNameToScreenIndex.TryGetValue(actionName, out int screenIndex))
       {
           if (screenIndex == VALIDATION_INDEX)
           {
               // Route to ValidationModeController instead of screen toggle
               var valCtrl = ValidationModeController.Instance;
               if (valCtrl != null)
                   valCtrl.OnValidationKeyPressed();
           }
           else
           {
               ToggleScreen(screenIndex);
           }
       }
   }
   ```

3. Add a listener so that when any operator screen becomes visible, the dashboard auto-hides:
   ```csharp
   // In OnActiveScreenChanged callback:
   // If a real screen (1-8, Tab) became active, tell ValidationModeController
   // to hide the dashboard (but not stop the simulation)
   ```

This keeps ALL input routing through ScreenManager — no separate input listeners, no conflicts.

---

### Stage 5: Expand ScreenDataBridge with Control State Getters

**Files Modified:**
- `Assets/Scripts/UI/ScreenDataBridge.cs`

**New Getters Added (organized by screen):**

**Pressurizer Controls:**
| Getter | Source | Returns |
|--------|--------|---------|
| `GetHeatersOn()` | `engine.pzrHeatersOn` | bool |
| `GetHeaterMode()` | `engine.currentHeaterMode` | HeaterMode enum |
| `GetHeaterPIDOutput()` | `engine.heaterPIDOutput` | float (0–1) |
| `GetAuxSprayActive()` | `engine.auxSprayActive` | bool |

**RCP Controls:**
| Getter | Source | Returns |
|--------|--------|---------|
| `GetRCPRunning(int index)` | `engine.rcpRunning[index]` | bool |
| `GetRCPRamping()` | `!engine.rcpContribution.AllFullyRunning` | bool |
| `GetEffectiveRCPHeat()` | `engine.effectiveRCPHeat` | float (MW) |

**CVCS Controls:**
| Getter | Source | Returns |
|--------|--------|---------|
| `GetChargingActive()` | `engine.chargingActive` | bool |
| `GetLetdownActive()` | `engine.letdownActive` | bool |
| `GetLetdownIsolated()` | `engine.letdownIsolatedFlag` | bool |
| `GetLetdownViaRHR()` | `engine.letdownViaRHR` | bool |
| `GetLetdownViaOrifice()` | `engine.letdownViaOrifice` | bool |
| `GetVCTDivertActive()` | `engine.vctDivertActive` | bool |
| `GetVCTMakeupActive()` | `engine.vctMakeupActive` | bool |

**Plant Status:**
| Getter | Source | Returns |
|--------|--------|---------|
| `GetSealInjectionOK()` | `engine.sealInjectionOK` | bool |
| `GetCCWRunning()` | `engine.ccwRunning` | bool |
| `GetBubbleFormed()` | `engine.bubbleFormed` | bool |
| `GetSolidPressurizer()` | `engine.solidPressurizer` | bool |
| `GetHeatupInProgress()` | `engine.heatupInProgress` | bool |

All getters follow the established pattern: return engine value if available, return safe default (`false`) if engine is null.

---

### Stage 6: Update Screen Control Visuals to Reflect Simulation State

**Files Modified:**
- `Assets/Scripts/UI/PressurizerScreen.cs`
- `Assets/Scripts/UI/CVCSScreen.cs`
- `Assets/Scripts/UI/SteamGeneratorScreen.cs`
- `Assets/Scripts/UI/RCSPrimaryLoopScreen.cs`
- `Assets/Scripts/UI/SecondarySystemsScreen.cs`
- `Assets/Scripts/UI/AuxiliarySystemsScreen.cs`

**Pattern:** Each screen gets a new `UpdateControlStates()` method called from its existing `Update()` at the same interval as gauge updates. This method reads boolean state from ScreenDataBridge and updates control element visuals (colors, text).

**PressurizerScreen (Screen 3):**

| Control Element | Data Source | Visual Behavior |
|----------------|-------------|-----------------|
| `button_ProportionalHeaters` | `GetHeatersOn()` + power level | Green "ON" when heaters energized, grey "OFF" when not |
| `button_BackupHeaters` | `GetHeaterPower()` > 660 kW | Amber "ENERGIZED" when backup heaters on, grey "STANDBY" otherwise |
| `button_SprayOpen / Close` | Pressure > setpoint+25 | "OPEN" when spray inferred active, "CLOSE" otherwise |
| `vessel_HeaterBars[]` | Already implemented via `UpdateHeaterBars()` | ✓ No change needed |
| `vessel_SprayIndicator` | Already implemented via `UpdateSprayIndicator()` | ✓ No change needed |
| `indicator_PORV_A/B` | Already implemented via `UpdateReliefValveIndicators()` | ✓ No change needed |
| `indicator_SV_1/2/3` | Already implemented via `UpdateReliefValveIndicators()` | ✓ No change needed |

**CVCSScreen (Screen 4):**

| Control Element | Data Source | Visual Behavior |
|----------------|-------------|-----------------|
| `indicator_CCP_A` / `text_CCP_A_Status` | `GetChargingActive()` | Green "RUN" when charging active, grey "STBY" when not |
| `indicator_CCP_B` / `text_CCP_B_Status` | Always STBY during heatup | Grey "STBY" (only 1 CCP needed) |
| `indicator_CCP_C` / `text_CCP_C_Status` | Always STBY during heatup | Grey "STBY" |
| `button_CCP_A_Start/Stop` | `GetChargingActive()` | Start button highlighted when running, Stop when stopped |
| `button_Borate / Dilute` | No model yet | Grey "MANUAL" — unchanged |
| `diagram_LetdownLine` | `GetLetdownActive()` | Already partially implemented | 
| `diagram_ChargingLine` | `GetChargingActive()` | Already partially implemented |

**RCSPrimaryLoopScreen (Screen 2):**

| Control Element | Data Source | Visual Behavior |
|----------------|-------------|-----------------|
| RCP-A/B/C/D indicators | `GetRCPRunning(0..3)` | Green "RUNNING" per pump as they start sequentially |
| Flow arrows | `GetRCPCount()` | Arrow opacity/color scales with number of running pumps |
| T_hot/T_cold indicators | Already implemented | ✓ No change needed |

**SteamGeneratorScreen (Screen 5):**

| Control Element | Data Source | Visual Behavior |
|----------------|-------------|-----------------|
| Steam dump valve indicator | `GetSteamDumpActive()` | Green "OPEN" when steam dump active, grey "CLOSED" |
| SG feed status | No model yet | Placeholder — unchanged |

**SecondarySystemsScreen (Screen 7):**

| Control Element | Data Source | Visual Behavior |
|----------------|-------------|-----------------|
| Steam dump flow display | `GetSteamDumpDemand()` | Updates with live demand value |
| MSIV indicators | Assumed OPEN during heatup | Green "OPEN" — static |

**AuxiliarySystemsScreen (Screen 8):**

| Control Element | Data Source | Visual Behavior |
|----------------|-------------|-----------------|
| RHR pump indicators | Mode-based (active in Mode 5) | Green when T < 350°F, secured above |
| CCW indicators | `GetCCWRunning()` | Green "RUNNING" when CCW active |

**Implementation pattern (same for all screens):**

```csharp
// Added to each screen's Update() method, called at GAUGE_UPDATE_INTERVAL
private void UpdateControlStates()
{
    if (_data == null) return;
    
    // Example: Heater button reflects engine state
    bool heatersOn = _data.GetHeatersOn();
    if (button_ProportionalHeaters != null)
    {
        var colors = button_ProportionalHeaters.colors;
        colors.normalColor = heatersOn ? COLOR_RUNNING : COLOR_STOPPED;
        button_ProportionalHeaters.colors = colors;
        // Update associated text if present
        var btnText = button_ProportionalHeaters.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
            btnText.text = heatersOn ? "PROP HTR: ON" : "PROP HTR: OFF";
    }
}
```

All controls remain `interactable = false` — they are read-only state indicators during validation.

---

### Stage 7: Disable Number-Key Time Acceleration Conflict

**Files Modified:**
- `Assets/Scripts/Validation/HeatupValidationVisual.cs`

**Problem:** The `Update()` method in HeatupValidationVisual currently handles digit keys 1–5 for time acceleration. When ScreenManager is present, keys 1–5 are routed to screen switching. Both systems would respond to the same key press.

**Fix:** Add a guard in `HeatupValidationVisual.Update()`:

```csharp
// In Update(), before digit key handling:
bool screenManagerActive = (Critical.UI.ScreenManager.Instance != null);

if (dashboardVisible && engine != null && !screenManagerActive)
{
    // Existing digit key handlers (1-5 for time acceleration)
    // Only active when running standalone without ScreenManager
    ...
}

// +/- keys for time acceleration — ALWAYS active (no conflict)
if (dashboardVisible && engine != null)
{
    if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame)
        ...
    if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame)
        ...
}
```

Also disable the F1 dashboard toggle when ScreenManager is present (V key handles this now):

```csharp
// F1 toggle — only when standalone
if (!screenManagerActive && kb.f1Key.wasPressedThisFrame)
    dashboardVisible = !dashboardVisible;
```

**Result:**
- Keys 1–5 → always go to ScreenManager for screen switching
- +/− keys → always go to time acceleration
- F1 → disabled when ScreenManager present (V handles dashboard)
- No key conflicts

---

### Stage 8: Verification & Testing Checklist

| # | Test | Expected Result | Pass? |
|---|------|----------------|-------|
| 1 | Menu: `Critical > Create All Operator Screens` | Builds all screens + validation objects without errors | |
| 2 | Press Play, check console | No errors. ScreenManager logs registered screens. ValidationModeController logs "INACTIVE". | |
| 3 | Press **V** | Simulation starts. Dashboard appears. Console: "STARTED — simulation running". Status bar visible. | |
| 4 | Observe dashboard for 10 seconds | Heatup proceeding. Temperature rising. Pressure stable (solid plant). Heaters ON. | |
| 5 | Press **3** | Dashboard hides. Pressurizer screen appears. Gauges update live. Heater buttons show ON. Vessel water level animates. | |
| 6 | Press **4** | PZR screen hides. CVCS screen appears. Charging/letdown flows updating. CCP-A: RUN. VCT level tracking. | |
| 7 | Press **V** | CVCS hides. Dashboard reappears. **Simulation did NOT restart or pause.** Sim time continuous. | |
| 8 | Press **+** twice | Time acceleration increases. Dashboard updates faster. | |
| 9 | Press **5** | SG screen appears. Secondary temps updating. Stratification delta-T visible if applicable. | |
| 10 | Press **2** | RCS screen appears. T_hot, T_cold, T_avg updating. | |
| 11 | Wait for RCP start event (accelerate time if needed) | On Screen 2: RCP indicator changes from STOPPED → RUNNING. On Screen 4: CCP stays RUN. | |
| 12 | Press **3** during RCP ramp-up | PZR screen: heater power value changing. Level responding to thermal expansion. | |
| 13 | Wait for bubble formation | PZR screen: steam volume appears (>0). Vessel steam dome activates (color change). Dashboard: bubble formation logged. | |
| 14 | Press **V**, verify sim time | Dashboard shows continuous sim time — never paused or reset throughout all view switches. | |
| 15 | Press **ESC** | Application exits cleanly. No hang. | |
| 16 | **Regression:** Run heatup WITHOUT operator screens (remove ScreenManager) | HeatupValidationVisual works standalone as before. F1 toggles dashboard. 1-5 control time accel. | |

---

## Implementation Order

| Stage | Description | Files | Estimated Effort |
|-------|-------------|-------|-----------------|
| 1 | Add V-key binding to ScreenInputActions | 1 JSON file | 5 min |
| 2 | Create ValidationModeController | 1 new file | 30 min |
| 3 | Update MultiScreenBuilder | 1 file modified | 20 min |
| 4 | Update ScreenManager routing | 1 file modified | 10 min |
| 5 | Expand ScreenDataBridge getters | 1 file modified | 20 min |
| 6 | Update screen control visuals (6 screens) | 6 files modified | 45 min |
| 7 | Disable digit-key conflict in HeatupValidationVisual | 1 file modified | 10 min |
| 8 | Verification & testing | No files | Manual testing |

**Total estimated effort:** ~2.5 hours

---

## Unaddressed Issues

| Issue | Reason | Disposition |
|-------|--------|-------------|
| Screens with PLACEHOLDER gauges (---) | Physics models don't exist yet | Documented in Future_Features. Screens show "---" for unmodeled parameters — validates the NaN-placeholder convention works correctly under live data. |
| Per-SG differentiation (all 4 SGs identical) | Lumped thermal model | v3.0.0 scope per Future_Features roadmap. All 4 SGs show identical data — correct for lumped model. |
| Turbine-Generator screen (Screen 6, 15 placeholders) | Entire T-G system not modeled | v3.0.0 scope. Screen 6 will show mostly "---" with only steam pressure live. |
| Manual control interaction (clicking buttons) | Controls are visual-only, non-interactive | Future feature. Controls reflect state but cannot be clicked to change state. Added to Future_Features. |
| Restart simulation without quitting | V key only starts once, no restart | Not needed for validation. If user wants a fresh run, quit and relaunch. Could add Shift+V restart in future if needed. |
| Screen 1 (Reactor Core) control state | GOLD STANDARD — ReactorOperatorScreen not modified | Screen 1 has its own data sources. No changes needed or permitted. |

---

## GOLD Standard Impact

| Module | Modified? | Nature of Change |
|--------|-----------|-----------------|
| HeatupSimEngine | **NO** | No changes. Engine runs exactly as before. |
| HeatupValidationVisual | **MINOR** | Stage 7: key conflict guards in `Update()`. No rendering or physics changes. Dashboard visibility now also controlled by ValidationModeController in addition to existing internal toggle. |
| ReactorOperatorScreen | **NO** | No changes. GOLD standard preserved. |
| All physics modules | **NO** | No changes. |

---

## Future_Features Update

The following item should be added to `Critical\Updates\Future_Features\FUTURE_ENHANCEMENTS_ROADMAP.md`:

> ### Manual Operator Controls (Interactive Buttons/Switches)
> **Status:** DEFERRED  
> **Added:** 2026-02-10  
> **Reason:** v2.1.0 adds control state reflection (controls visually track simulation state) but controls remain non-interactive. Future work to make controls clickable requires bidirectional data flow: ScreenDataBridge → Engine (currently read-only).  
> **Scope:** PZR heater manual on/off, RCP start/stop, CVCS charging/letdown valve control, steam dump manual override, boration/dilution initiation.  
> **Dependency:** Requires operator action model and validation against Tech Spec LCOs for each manual action.

---

## Notes

- This is a **validation tool**. The V-key binding and ValidationModeController are permanent additions — they provide ongoing test capability for all future screen development.
- The approach leverages existing architecture: ScreenDataBridge → HeatupSimEngine data flow already works.
- Control state reflection (Stage 5 & 6) is a permanent improvement — controls should always reflect simulation state regardless of whether validation mode exists.
- The simulation continuity guarantee is enforced by architecture: the engine lives on a separate GameObject that is never deactivated, and ValidationModeController never calls `StopSimulation()`.
