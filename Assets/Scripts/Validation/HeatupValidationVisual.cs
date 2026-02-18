// ============================================================================
// CRITICAL: Master the Atom - UI Component
// HeatupValidationVisual.cs - Heatup Validation Dashboard Core
// ============================================================================
//
// File: Assets/Scripts/Validation/HeatupValidationVisual.cs
// Module: Critical.Validation.HeatupValidationVisual
// Responsibility: Legacy OnGUI validation dashboard coordination and tab dispatch.
// Standards: GOLD v1.0, SRP/SOLID, Unity Hot-Path Guardrails
// Version: 5.4
// Last Updated: 2026-02-18
// Changes:
//   - 5.4 (2026-02-18): Added F2 scenario selector overlay for runtime scenario start.
//   - 5.3 (2026-02-17): Added GOLD metadata fields and bounded change-history ledger.
//   - 5.2 (2026-02-16): Added Critical tab and keyboard navigation updates.
//   - 5.1 (2026-02-16): Expanded telemetry snapshot integration and diagnostics.
//   - 5.0 (2026-02-15): Multi-tab dashboard redesign replacing legacy 3-column layout.
//   - 4.9 (2026-02-14): Added performance throttling and dashboard visibility controls.
//
// PURPOSE:
//   Top-level OnGUI coordinator for the PWR Cold Shutdown â†’ HZP heatup
//   validation dashboard. Manages layout skeleton, engine binding, scroll
//   state, dashboard tab state, time acceleration controls, and partial
//   dispatch to per-tab rendering methods.
//   Contains no rendering logic beyond the layout frame â€” all visual
//   content is delegated to partial class files.
//
// READS FROM:
//   HeatupSimEngine â€” all public state fields, history buffers, event log
//
// REFERENCE:
//   Westinghouse 4-Loop PWR control room instrumentation layout
//   NRC HRTD Section 19 â€” Plant Operations monitoring requirements
//
// ARCHITECTURE:
//   This is the CORE partial. Companion partials (single responsibility):
//     - HeatupValidationVisual.Styles.cs             : Colors, fonts, GUIStyle
//     - HeatupValidationVisual.Gauges.cs              : Gauge arc panels
//     - HeatupValidationVisual.Panels.cs              : Status/info panels
//     - HeatupValidationVisual.Graphs.cs              : Trend graph rendering
//     - HeatupValidationVisual.Annunciators.cs        : Alarm tiles + event log
//     - HeatupValidationVisual.TabOverview.cs         : Tab 1 â€” Overview
//     - HeatupValidationVisual.TabPressurizer.cs      : Tab 2 â€” Pressurizer
//     - HeatupValidationVisual.TabCVCS.cs             : Tab 3 â€” CVCS / Inventory
//     - HeatupValidationVisual.TabSGRHR.cs            : Tab 4 â€” SG / RHR
//     - HeatupValidationVisual.TabRCPElectrical.cs    : Tab 5 â€” RCP / Electrical
//     - HeatupValidationVisual.TabEventLog.cs         : Tab 6 â€” Event Log
//     - HeatupValidationVisual.TabValidation.cs       : Tab 7 â€” Validation
//     - HeatupValidationVisual.TabCritical.cs          : Tab 8 â€” Critical (v5.2.0)
//
//   v5.0.0 Layout (multi-tab, replacing v0.9.3 3-column):
//     â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
//     â”‚  HEADER BAR: Mode â”‚ Phase â”‚ Sim Time â”‚ Time Accel    â”‚
//     â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
//     â”‚  TAB BAR: OVâ”‚PZRâ”‚CVCSâ”‚SG/RHRâ”‚RCPâ”‚LOGâ”‚VALâ”‚CRIT     â”‚
//     â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
//     â”‚                                                      â”‚
//     â”‚  TAB CONTENT AREA                                    â”‚
//     â”‚  (layout varies per tab â€” see Tab* partials)         â”‚
//     â”‚                                                      â”‚
//     â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
//
// GOLD STANDARD: Yes
// v0.9.6 PERF FIX: OnGUI refresh throttle to respect refreshRate setting
// v5.0.0: Multi-tab dashboard redesign (Ctrl+1â€“7 tab switching)
// v5.2.0: Added CRITICAL tab (Tab 8, Ctrl+8) for at-a-glance validation
// ============================================================================

using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Critical.Physics;
using Critical.ScenarioSystem;
using Critical.Simulation.Modular.State;
using UnityEngine.Scripting.APIUpdating;


namespace Critical.Validation
{

/// <summary>
/// Heatup Validation Dashboard â€” OnGUI coordinator.
/// Reads HeatupSimEngine public state. Contains no physics (G3).
/// All rendering delegated to partials (Styles, Gauges, Panels, Graphs, Annunciators, Tab*).
/// </summary>
[MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "HeatupValidationVisual")]
public partial class HeatupValidationVisual : MonoBehaviour
{
    // ========================================================================
    // INSPECTOR SETTINGS
    // ========================================================================

    [Header("Engine Reference")]
    [Tooltip("The heatup simulation engine to monitor")]
    public HeatupSimEngine engine;

    [Header("Dashboard Settings")]
    [Tooltip("Dashboard refresh rate in Hz")]
    [Range(2f, 30f)]
    public float refreshRate = 10f;

    [Tooltip("Show dashboard on start")]
    public bool showOnStart = true;

    [Tooltip("Enable periodic frame-time diagnostic logs in Console.")]
    public bool enableFrameTimeDiagnosticsLogs = false;

    // ========================================================================
    // PUBLIC STATE â€” Toggling, visibility
    // ========================================================================

    [HideInInspector] public bool dashboardVisible = true;

    // ========================================================================
    // LAYOUT CONSTANTS
    // ========================================================================

    // Header height
    const float HEADER_FRAC = 0.06f;

    // v5.0.0: Dashboard tab bar height (px)
    const float TAB_BAR_H = 28f;

    // Minimum dimensions (px) for readability â€” used by tab layouts
    const float MIN_GAUGE_WIDTH = 180f;
    const float MIN_GRAPH_WIDTH = 400f;

    // ========================================================================
    // INTERNAL STATE â€” Dashboard tabs, graph tabs, scroll, timing
    // ========================================================================

    // v5.0.0: Dashboard tab selection (0-indexed, persists across F1 toggles)
    private int _dashboardTab;
    private readonly string[] _dashboardTabLabels = new string[]
    {
        "OVERVIEW",     // Tab 0: At-a-glance plant status
        "PZR",          // Tab 1: Pressurizer detail
        "CVCS",         // Tab 2: CVCS / Inventory
        "SG/RHR",       // Tab 3: Steam Generators / RHR
        "RCP",          // Tab 4: RCP / Electrical / HZP
        "LOG",          // Tab 5: Event Log + Annunciators
        "VALID",        // Tab 6: Validation / Debug
        "CRITICAL"      // Tab 7: Critical variables at-a-glance (v5.2.0)
    };

    // Graph tab selection (used within individual dashboard tabs)
    private int _graphTab;
    private readonly string[] _graphTabLabels = new string[]
    {
        "TEMPS",       // T_rcs, T_hot, T_cold, T_pzr, T_sat
        "PRESSURE",    // RCS Pressure, PZR Level
        "CVCS",        // Charging, Letdown, Surge, Net CVCS
        "VCT/BRS",     // VCT Level, BRS Holdup, BRS Distillate
        "RATES",       // Heatup Rate, Pressure Rate, Subcooling
        "RCP HEAT",    // Effective RCP Heat, Heater Power
        "HZP"          // v1.1.0: Steam dump, heater PID, HZP progress
    };

    // Event log scroll
    private Vector2 _eventLogScroll;

    // Gauge scroll (left column) - kept as fallback for small screens
    private Vector2 _gaugeScroll;

    // Status panel scroll (right column)
    private Vector2 _statusScroll;

    // Refresh throttle
    private float _lastRefreshTime;

    // v0.3.0.0 Phase A (CS-0032): Cached header strings â€” avoid per-frame string allocations
    private string _cachedModeStr;
    private string _cachedPhaseStr;
    private string _cachedSimTimeStr;
    private string _cachedWallTimeStr;
    private string _cachedAccelStr;
    private Color _cachedModeColor;
    private Color _cachedAccelColor;
    // Cached comparison values for change detection
    private float _cachedSimTime = -1f;
    private float _cachedWallTime = -1f;
    private int _cachedSpeedIndex = -1;
    private int _cachedPlantMode = -1;
    private string _cachedPhaseDesc;
    private HeatupSimEngine.RuntimeTelemetrySnapshot _telemetrySnapshot;
    private StepSnapshot _stepSnapshot = StepSnapshot.Empty;

    // v0.3.0.0 Phase A (CS-0032): Frame time diagnostic â€” lightweight probe
    private float _frameTimeAccum;
    private int _frameTimeCount;
    private float _frameTimeMax;
    private float _lastFrameTimeDiag;
    private const float FRAME_DIAG_INTERVAL = 5f;  // Report every 5 seconds

    // Cached screen dimensions for layout
    private float _sw, _sh;

    // IP-0049 follow-up: F2 scenario selector overlay state.
    private bool _scenarioMenuVisible;
    private Vector2 _scenarioMenuScroll;
    private string _scenarioMenuStatus = string.Empty;
    private ScenarioDescriptor[] _scenarioDescriptors = Array.Empty<ScenarioDescriptor>();

    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================

    void Start()
    {
        dashboardVisible = showOnStart;

        // Auto-find engine if not assigned
        if (engine == null)
            engine = FindObjectOfType<HeatupSimEngine>();

        if (engine == null)
            Debug.LogError("[HeatupValidationVisual] No HeatupSimEngine found in scene!");
        else
        {
            _telemetrySnapshot = engine.GetTelemetrySnapshot();
            _stepSnapshot = engine.GetStepSnapshot();
            RefreshScenarioSelectorDescriptors();
        }

        // Note: Styles are initialized lazily in OnGUI (GUI.skin requires OnGUI context)
    }

    /// <summary>
    /// v0.9.6 PERF FIX: Stop all GUI work immediately to prevent hang during shutdown.
    /// Unity will clean up textures automatically.
    /// </summary>
    void OnApplicationQuit()
    {
        Debug.Log("[HeatupValidationVisual] OnApplicationQuit");
        
        // v0.9.6 PERF FIX: STOP ALL GUI WORK IMMEDIATELY
        // This prevents the event log and graphs from continuing to draw
        // while the engine is shutting down, which causes 30-60s exit delays
        dashboardVisible = false;
        
        // Signal engine to stop immediately
        if (engine != null)
        {
            engine.RequestImmediateShutdown();
        }
        
        // NOTE: Do NOT call texture cleanup here!
        // Destroying textures during quit can cause crashes if OnGUI is still running.
        // Unity handles texture cleanup automatically on application exit.
    }

    /// <summary>
    /// v0.9.5: Force quit the application immediately without waiting.
    /// Uses Process.Kill() for immediate termination in builds to prevent
    /// Unity's cleanup from hanging.
    /// </summary>
    void ForceQuit()
    {
        Debug.Log("[HeatupValidationVisual] ForceQuit - IMMEDIATE TERMINATION");
        
        // v0.9.6 PERF FIX: Stop GUI work immediately
        dashboardVisible = false;
        
        // Stop time to prevent any further updates
        Time.timeScale = 0f;
        
        // Signal engine to stop immediately
        if (engine != null)
        {
            engine.RequestImmediateShutdown();
        }
        
        // Stop our own coroutines
        StopAllCoroutines();
        
        // NOTE: Do NOT call CleanupNativeResources() here!
        // Process.Kill() will terminate immediately - no cleanup needed.
        
        #if UNITY_EDITOR
            // In editor, use immediate stop
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // In build, forcefully terminate the process immediately
            // This bypasses all Unity cleanup which can hang
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        #endif
    }

    void Update()
    {
        if (engine != null)
        {
            _telemetrySnapshot = engine.GetTelemetrySnapshot();
            _stepSnapshot = engine.GetStepSnapshot();
        }

        // ============================================================
        // v0.3.0.0 Phase A (CS-0032): Frame time diagnostic probe.
        // Logs average and max frame time every 5 seconds to Unity console.
        // Measurement only â€” no behavioral change.
        // ============================================================
        {
            float frameDelta = Time.unscaledDeltaTime * 1000f;  // ms
            _frameTimeAccum += frameDelta;
            _frameTimeCount++;
            if (frameDelta > _frameTimeMax) _frameTimeMax = frameDelta;

            float now = Time.realtimeSinceStartup;
            if (enableFrameTimeDiagnosticsLogs &&
                now - _lastFrameTimeDiag >= FRAME_DIAG_INTERVAL &&
                _frameTimeCount > 0)
            {
                float avg = _frameTimeAccum / _frameTimeCount;
                Debug.Log($"[PERF] Frame time: avg={avg:F1}ms, max={_frameTimeMax:F1}ms, " +
                    $"FPS={1000f / avg:F0}, frames={_frameTimeCount} ({FRAME_DIAG_INTERVAL:F0}s window)");
                _frameTimeAccum = 0f;
                _frameTimeCount = 0;
                _frameTimeMax = 0f;
                _lastFrameTimeDiag = now;
            }
        }

        var kb = Keyboard.current;
        if (kb == null) return;

        // Toggle dashboard with F1
        if (kb.f1Key.wasPressedThisFrame)
            dashboardVisible = !dashboardVisible;

        // F2 scenario selector toggle is routed through SceneBridge while
        // validator view is active to keep scene-level input ownership centralized.

        // Force quit with ESC (in builds only)
        #if !UNITY_EDITOR
        if (kb.escapeKey.wasPressedThisFrame)
            ForceQuit();
        #endif

        // v5.0.0: Dashboard tab switching with Ctrl+1 through Ctrl+7
        if (dashboardVisible)
        {
            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            if (ctrl)
            {
                if (kb.digit1Key.wasPressedThisFrame) _dashboardTab = 0;  // Ctrl+1 = Overview
                if (kb.digit2Key.wasPressedThisFrame) _dashboardTab = 1;  // Ctrl+2 = PZR
                if (kb.digit3Key.wasPressedThisFrame) _dashboardTab = 2;  // Ctrl+3 = CVCS
                if (kb.digit4Key.wasPressedThisFrame) _dashboardTab = 3;  // Ctrl+4 = SG/RHR
                if (kb.digit5Key.wasPressedThisFrame) _dashboardTab = 4;  // Ctrl+5 = RCP
                if (kb.digit6Key.wasPressedThisFrame) _dashboardTab = 5;  // Ctrl+6 = LOG
                if (kb.digit7Key.wasPressedThisFrame) _dashboardTab = 6;  // Ctrl+7 = VALID
                if (kb.digit8Key.wasPressedThisFrame) _dashboardTab = 7;  // Ctrl+8 = CRITICAL (v5.2.0)
            }
        }

        // Time acceleration hotkeys (when dashboard is visible)
        if (dashboardVisible && engine != null)
        {
            // v2.0.11: Direct speed selection with F5-F9 keys
            // (Keys 1-5 freed for ScreenManager operator screen switching)
            if (kb.f5Key.wasPressedThisFrame) engine.SetTimeAcceleration(0);  // F5 = 1x Real-Time
            if (kb.f6Key.wasPressedThisFrame) engine.SetTimeAcceleration(1);  // F6 = 2x
            if (kb.f7Key.wasPressedThisFrame) engine.SetTimeAcceleration(2);  // F7 = 4x
            if (kb.f8Key.wasPressedThisFrame) engine.SetTimeAcceleration(3);  // F8 = 8x
            if (kb.f9Key.wasPressedThisFrame) engine.SetTimeAcceleration(4);  // F9 = 10x
            
            // v0.7.1: Increment/decrement time acceleration with +/- keys
            // Increment with + or = key (same physical key on US keyboard)
            if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame)
            {
                int newIndex = (engine.currentSpeedIndex + 1) % 5;  // Wrap: 0â†’1â†’2â†’3â†’4â†’0
                engine.SetTimeAcceleration(newIndex);
            }
            
            // Decrement with - key
            if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame)
            {
                int newIndex = (engine.currentSpeedIndex - 1 + 5) % 5;  // Wrap: 4â†’3â†’2â†’1â†’0â†’4
                engine.SetTimeAcceleration(newIndex);
            }
        }
    }

    // ========================================================================
    // OnGUI â€” MAIN DISPATCH
    // v0.9.6 PERF FIX: Throttle to refreshRate (eliminates 94% of redraws)
    // v5.0.0: Multi-tab dispatch replaces 3-column layout
    // ========================================================================

    void OnGUI()
    {
        if (engine == null) return;
        if (!dashboardVisible && !_scenarioMenuVisible) return;

        // v0.3.0.0 Phase A (CS-0032): Layout-only throttle.
        // Throttle Layout events at refreshRate Hz to reduce per-frame computation
        // (string formatting, data reads, GUILayout calculations).
        // Repaint is NEVER skipped â€” it must always draw to avoid blue-screen flicker.
        // The header caching (DrawHeaderBar) ensures Repaint is cheap even at full
        // frame rate: cached strings are reused, no allocations on stable values.
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

        // Ensure styles are initialized (handles domain reload)
        if (!_stylesInitialized)
            InitializeStyles();

        if (dashboardVisible)
        {
            // ================================================================
            // FULL-SCREEN BACKGROUND
            // ================================================================
            GUI.DrawTexture(new Rect(0, 0, _sw, _sh), _bgTex, ScaleMode.StretchToFill);

            // ================================================================
            // HEADER BAR
            // ================================================================
            float headerH = Mathf.Max(_sh * HEADER_FRAC, 40f);
            Rect headerRect = new Rect(0, 0, _sw, headerH);
            DrawHeaderBar(headerRect);

            // ================================================================
            // v5.0.0: DASHBOARD TAB BAR
            // ================================================================
            float tabBarY = headerH;
            Rect tabBarRect = new Rect(4f, tabBarY + 2f, _sw - 8f, TAB_BAR_H - 4f);
            _dashboardTab = GUI.Toolbar(tabBarRect, _dashboardTab, _dashboardTabLabels, _tabStyle);

            // ================================================================
            // v5.0.0: TAB CONTENT AREA â€” dispatched to per-tab partial methods
            // ================================================================
            float contentY = headerH + TAB_BAR_H;
            float contentH = _sh - contentY;
            Rect contentArea = new Rect(0, contentY, _sw, contentH);

            switch (_dashboardTab)
            {
                case 0: DrawOverviewTab(contentArea);      break;
                case 1: DrawPressurizerTab(contentArea);   break;
                case 2: DrawCVCSTab(contentArea);          break;
                case 3: DrawSGRHRTab(contentArea);         break;
                case 4: DrawRCPElectricalTab(contentArea); break;
                case 5: DrawEventLogTab(contentArea);      break;
                case 6: DrawValidationTab(contentArea);    break;
                case 7: DrawCriticalTab(contentArea);      break;  // v5.2.0
            }
        }

        if (_scenarioMenuVisible)
        {
            DrawScenarioSelectorOverlay();
        }
    }

    // ========================================================================
    // HEADER BAR â€” Plant mode, phase, times, time acceleration
    // ========================================================================

    void DrawHeaderBar(Rect area)
    {
        var snap = _telemetrySnapshot;
        GUI.Box(area, GUIContent.none, _headerBgStyle);

        float pad = 6f;
        float x = pad;
        float y = area.y + pad;
        float h = area.height - pad * 2f;
        float cellH = h;

        // ============================================================
        // v0.3.0.0 Phase A (CS-0032): Update-on-change header caching.
        // Header strings only reformat when underlying values change
        // beyond display precision. Eliminates ~5 string allocations
        // per OnGUI cycle for stable values.
        // ============================================================

        // MODE indicator â€” changes with plant mode transitions
        int currentPlantMode = snap.PlantMode;
        if (currentPlantMode != _cachedPlantMode)
        {
            _cachedPlantMode = currentPlantMode;
            _cachedModeColor = GetModeColorFromPlantMode(currentPlantMode);
            _cachedModeStr = GetModeStringFromPlantMode(currentPlantMode);
        }
        DrawHeaderCell(ref x, y, 140f, cellH, _cachedModeStr ?? "---", _cachedModeColor);

        // Phase description â€” changes with heatup phase transitions
        string currentPhaseDesc = snap.HeatupPhaseDesc;
        if (currentPhaseDesc != _cachedPhaseDesc)
        {
            _cachedPhaseDesc = currentPhaseDesc;
            _cachedPhaseStr = string.IsNullOrEmpty(currentPhaseDesc) ? "INITIALIZING" : currentPhaseDesc;
        }
        DrawHeaderCell(ref x, y, 260f, cellH, _cachedPhaseStr ?? "INITIALIZING", _cNormalGreen);

        // Sim time â€” changes every physics step, but display only needs ~1s resolution
        float simTimeTrunc = Mathf.Floor(snap.SimTime * 3600f);  // Truncate to 1 sim-second
        if (simTimeTrunc != _cachedSimTime)
        {
            _cachedSimTime = simTimeTrunc;
            _cachedSimTimeStr = $"SIM: {FormatHours(snap.SimTime)}";
        }
        DrawHeaderCell(ref x, y, 140f, cellH, _cachedSimTimeStr ?? "SIM: 0:00:00", _cTextPrimary);

        // Wall time â€” changes every real second
        float wallTimeTrunc = Mathf.Floor(snap.WallClockTime * 3600f);
        if (wallTimeTrunc != _cachedWallTime)
        {
            _cachedWallTime = wallTimeTrunc;
            _cachedWallTimeStr = $"WALL: {FormatHours(snap.WallClockTime)}";
        }
        DrawHeaderCell(ref x, y, 140f, cellH, _cachedWallTimeStr ?? "WALL: 0:00:00", _cTextPrimary);

        // Time acceleration â€” changes only on hotkey press
        if (snap.CurrentSpeedIndex != _cachedSpeedIndex)
        {
            _cachedSpeedIndex = snap.CurrentSpeedIndex;
            string speedLabel = TimeAcceleration.SpeedLabelsShort[Mathf.Clamp(snap.CurrentSpeedIndex, 0, TimeAcceleration.SpeedLabelsShort.Length - 1)];
            _cachedAccelStr = $"SPEED: {speedLabel}";
            _cachedAccelColor = snap.IsAccelerated ? _cWarningAmber : _cTextPrimary;
        }
        DrawHeaderCell(ref x, y, 100f, cellH, _cachedAccelStr ?? "SPEED: 1x", _cachedAccelColor);

        // v5.0.0: Active tab indicator in header (right-aligned)
        string tabHint = $"[Ctrl+1-8] {_dashboardTabLabels[_dashboardTab]}";
        float hintW = 180f;
        Rect hintRect = new Rect(area.width - hintW - pad, area.y + pad, hintW, cellH);
        var prev = GUI.contentColor;
        GUI.contentColor = _cTextSecondary;
        GUI.Label(hintRect, tabHint, _headerLabelStyle);
        GUI.contentColor = prev;
    }

    void DrawHeaderCell(ref float x, float y, float w, float h, string text, Color color)
    {
        var prev = GUI.contentColor;
        GUI.contentColor = color;
        GUI.Label(new Rect(x, y, w, h), text, _headerLabelStyle);
        GUI.contentColor = prev;
        x += w + 6f;
    }

    /// <summary>
    /// Toggle scenario-selector overlay visibility.
    /// Intended to be invoked by scene-level input routing (SceneBridge).
    /// </summary>
    public void ToggleScenarioSelector()
    {
        SetScenarioSelectorVisible(!_scenarioMenuVisible);
    }

    /// <summary>
    /// Force scenario-selector visibility and refresh state on open.
    /// </summary>
    public void SetScenarioSelectorVisible(bool visible)
    {
        _scenarioMenuVisible = visible;
        if (_scenarioMenuVisible)
        {
            RefreshScenarioSelectorDescriptors();
            _scenarioMenuStatus = string.Empty;
        }
    }

    /// <summary>
    /// Exposes current selector visibility for bridge diagnostics.
    /// </summary>
    public bool IsScenarioSelectorVisible => _scenarioMenuVisible;

    /// <summary>
    /// Draw the F2 scenario-selection overlay and start selected scenarios.
    /// </summary>
    void DrawScenarioSelectorOverlay()
    {
        Color prevColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0f, 0f, _sw, _sh), _whiteTex, ScaleMode.StretchToFill);
        GUI.color = prevColor;

        float panelW = Mathf.Min(780f, _sw - 80f);
        float panelH = Mathf.Min(520f, _sh - 80f);
        Rect panel = new Rect((_sw - panelW) * 0.5f, (_sh - panelH) * 0.5f, panelW, panelH);
        GUI.Box(panel, GUIContent.none, _panelBgStyle);

        Rect titleRect = new Rect(panel.x + 16f, panel.y + 10f, panel.width - 32f, 24f);
        GUI.Label(titleRect, "SCENARIO SELECTOR (F2)", _headerLabelStyle);

        Rect hintRect = new Rect(panel.x + 16f, panel.y + 34f, panel.width - 32f, 18f);
        GUI.Label(hintRect, "Select a scenario and press START.", _statusLabelStyle);

        Rect modeRect = new Rect(panel.x + 16f, panel.y + 52f, panel.width - 32f, 18f);
        string modeLine = $"Current Plant Mode: {(engine.plantMode == 5 ? "MODE 5 Cold Shutdown" : $"MODE {engine.plantMode}")}";
        GUI.Label(modeRect, modeLine, _statusLabelStyle);

        Rect listRect = new Rect(panel.x + 16f, panel.y + 76f, panel.width - 32f, panel.height - 142f);
        GUI.Box(listRect, GUIContent.none, _graphBgStyle);

        if (_scenarioDescriptors == null || _scenarioDescriptors.Length == 0)
        {
            Rect emptyRect = new Rect(listRect.x + 10f, listRect.y + 10f, listRect.width - 20f, 24f);
            GUI.Label(emptyRect, "No scenarios are currently registered.", _statusLabelStyle);
        }
        else
        {
            float rowH = 56f;
            Rect viewRect = new Rect(0f, 0f, listRect.width - 20f, _scenarioDescriptors.Length * rowH);
            _scenarioMenuScroll = GUI.BeginScrollView(listRect, _scenarioMenuScroll, viewRect);

            for (int i = 0; i < _scenarioDescriptors.Length; i++)
            {
                ScenarioDescriptor descriptor = _scenarioDescriptors[i];
                Rect rowRect = new Rect(4f, i * rowH + 2f, viewRect.width - 8f, rowH - 6f);
                GUI.Box(rowRect, GUIContent.none, _gaugeBgStyle);

                Rect nameRect = new Rect(rowRect.x + 8f, rowRect.y + 4f, rowRect.width - 180f, 22f);
                GUI.Label(nameRect, descriptor.DisplayName, _headerLabelStyle);

                Rect metaRect = new Rect(rowRect.x + 8f, rowRect.y + 24f, rowRect.width - 180f, 18f);
                GUI.Label(metaRect, $"ID={descriptor.Id}  Domain={descriptor.DomainOwner}", _statusLabelStyle);

                Rect startBtnRect = new Rect(rowRect.x + rowRect.width - 128f, rowRect.y + 12f, 112f, 28f);
                bool previousEnabled = GUI.enabled;
                GUI.enabled = !engine.isRunning;
                if (GUI.Button(startBtnRect, "START"))
                {
                    bool started = engine.StartScenarioById(descriptor.Id);
                    if (started)
                    {
                        _scenarioMenuStatus = $"Started: {descriptor.DisplayName} ({descriptor.Id})";
                        _scenarioMenuVisible = false;
                    }
                    else
                    {
                        _scenarioMenuStatus = $"Failed to start {descriptor.Id}. Check event log for details.";
                    }
                }
                GUI.enabled = previousEnabled;
            }

            GUI.EndScrollView();
        }

        Rect footerRect = new Rect(panel.x + 16f, panel.y + panel.height - 56f, panel.width - 32f, 44f);
        GUI.Box(footerRect, GUIContent.none, _gaugeBgStyle);

        string statusText = string.IsNullOrWhiteSpace(_scenarioMenuStatus)
            ? (engine.isRunning ? "Simulation running: stop current run before starting another scenario." : "Ready.")
            : _scenarioMenuStatus;
        Rect statusRect = new Rect(footerRect.x + 8f, footerRect.y + 12f, footerRect.width - 120f, 20f);
        GUI.Label(statusRect, statusText, _statusValueStyle);

        Rect closeRect = new Rect(footerRect.x + footerRect.width - 96f, footerRect.y + 8f, 84f, 28f);
        if (GUI.Button(closeRect, "CLOSE"))
        {
            _scenarioMenuVisible = false;
            _scenarioMenuStatus = string.Empty;
        }
    }

    void RefreshScenarioSelectorDescriptors()
    {
        if (engine == null)
        {
            _scenarioDescriptors = Array.Empty<ScenarioDescriptor>();
            return;
        }

        _scenarioDescriptors = engine.GetAvailableScenarioDescriptors();
    }

    private static string GetModeStringFromPlantMode(int mode)
    {
        switch (mode)
        {
            case 5: return "MODE 5 Cold Shutdown";
            case 4: return "MODE 4 Hot Shutdown";
            case 3: return "MODE 3 Hot Standby";
            default: return "UNKNOWN";
        }
    }

    private Color GetModeColorFromPlantMode(int mode)
    {
        switch (mode)
        {
            case 5: return _cNormalGreen;
            case 4: return _cWarningAmber;
            case 3: return _cAlarmRed;
            default: return _cTextSecondary;
        }
    }

    // ========================================================================
    // PARTIAL METHOD DECLARATIONS â€” Tab content (implemented in Tab* partials)
    // v5.0.0: Each tab has its own partial file
    // ========================================================================

    /// <summary>
    /// Tab 1 â€” Overview: key parameters, primary graphs, critical alarms.
    /// Implemented in HeatupValidationVisual.TabOverview.cs
    /// </summary>
    partial void DrawOverviewTab(Rect area);

    /// <summary>
    /// Tab 2 â€” Pressurizer: PZR gauges, heater control, bubble state, PZR graphs.
    /// Implemented in HeatupValidationVisual.TabPressurizer.cs
    /// </summary>
    partial void DrawPressurizerTab(Rect area);

    /// <summary>
    /// Tab 3 â€” CVCS / Inventory: charging/letdown, VCT/BRS, mass conservation.
    /// Implemented in HeatupValidationVisual.TabCVCS.cs
    /// </summary>
    partial void DrawCVCSTab(Rect area);

    /// <summary>
    /// Tab 4 â€” SG / RHR: steam generator thermal, RHR system, SG graphs.
    /// Implemented in HeatupValidationVisual.TabSGRHR.cs
    /// </summary>
    partial void DrawSGRHRTab(Rect area);

    /// <summary>
    /// Tab 5 â€” RCP / Electrical: pump status, HZP stabilization, RCP/HZP graphs.
    /// Implemented in HeatupValidationVisual.TabRCPElectrical.cs
    /// </summary>
    partial void DrawRCPElectricalTab(Rect area);

    /// <summary>
    /// Tab 6 â€” Event Log: annunciator tiles + full scrollable event log.
    /// Implemented in HeatupValidationVisual.TabEventLog.cs
    /// </summary>
    partial void DrawEventLogTab(Rect area);

    /// <summary>
    /// Tab 7 â€” Validation: RVLIS, inventory audit, PASS/FAIL checks, debug.
    /// Implemented in HeatupValidationVisual.TabValidation.cs
    /// </summary>
    partial void DrawValidationTab(Rect area);

    /// <summary>
    /// Tab 8 â€” Critical: At-a-glance overview of RCS, PZR, CVCS, VCT, SG.
    /// v5.2.0: Implemented in HeatupValidationVisual.TabCritical.cs
    /// </summary>
    partial void DrawCriticalTab(Rect area);

    // ========================================================================
    // PARTIAL METHOD DECLARATIONS â€” Rendering components (existing partials)
    // These are called BY the Tab* partials to render individual components
    // ========================================================================

    /// <summary>
    /// Draw gauges column content. Implemented in Gauges partial.
    /// </summary>
    partial void DrawGaugeColumnContent(Rect area);

    /// <summary>
    /// Draw status panels column content. Implemented in Panels partial.
    /// </summary>
    partial void DrawStatusColumnContent(Rect area);

    /// <summary>
    /// Draw graph content for a specific tab. Implemented in Graphs partial.
    /// </summary>
    partial void DrawGraphContent(Rect area, int tabIndex);

    /// <summary>
    /// Draw annunciator tiles. Implemented in Annunciators partial.
    /// </summary>
    partial void DrawAnnunciatorContent(Rect area);

    /// <summary>
    /// Draw the footer event log. Implemented in Annunciators partial.
    /// </summary>
    partial void DrawEventLogContent(Rect area);

    // ========================================================================
    // LAYOUT HELPERS â€” Used by Tab* partials to build consistent layouts
    // v5.0.0: Shared framing methods for gauge columns and graph areas
    // ========================================================================

    /// <summary>
    /// Draw a gauge column with header and optional scroll.
    /// Used by Tab* partials that need a left-side gauge column.
    /// </summary>
    void DrawGaugeColumnFrame(Rect area, string title, System.Action<Rect> drawContent, float contentHeight)
    {
        GUI.Box(area, GUIContent.none, _panelBgStyle);

        float labelH = 22f;
        Rect labelRect = new Rect(area.x + 4f, area.y + 2f, area.width - 8f, labelH);
        GUI.Label(labelRect, title, _sectionHeaderStyle);

        float availableH = area.height - labelH - 4f;
        Rect contentRect = new Rect(area.x, area.y + labelH + 2f, area.width, availableH);

        if (contentHeight <= availableH)
        {
            drawContent(contentRect);
        }
        else
        {
            _gaugeScroll = GUI.BeginScrollView(contentRect, _gaugeScroll,
                new Rect(0, 0, area.width - 20f, contentHeight));
            drawContent(new Rect(0, 0, contentRect.width - 20f, contentHeight));
            GUI.EndScrollView();
        }
    }

    /// <summary>
    /// Draw a graph area with the graph tab bar and content.
    /// Used by Tab* partials that embed a graph sub-area.
    /// </summary>
    void DrawGraphAreaFrame(Rect area)
    {
        GUI.Box(area, GUIContent.none, _panelBgStyle);

        float tabH = 26f;
        Rect tabRect = new Rect(area.x + 4f, area.y + 2f, area.width - 8f, tabH);
        _graphTab = GUI.Toolbar(tabRect, _graphTab, _graphTabLabels, _tabStyle);

        Rect graphContent = new Rect(area.x + 4f, area.y + tabH + 4f,
            area.width - 8f, area.height - tabH - 8f);
        DrawGraphContent(graphContent, _graphTab);
    }

    /// <summary>
    /// Draw a stacked pair of graphs (two graph tabs rendered vertically).
    /// Used by system-specific tabs (PZR, CVCS, SG/RHR, RCP) to show
    /// two relevant graph types side by side without the graph tab bar.
    /// </summary>
    void DrawStackedGraphs(Rect area, int topGraphIndex, int bottomGraphIndex)
    {
        GUI.Box(area, GUIContent.none, _panelBgStyle);

        float halfH = area.height / 2f;
        float pad = 4f;

        // Top graph
        Rect topRect = new Rect(area.x + pad, area.y + pad,
            area.width - pad * 2f, halfH - pad * 1.5f);
        DrawGraphContent(topRect, topGraphIndex);

        // Bottom graph
        Rect bottomRect = new Rect(area.x + pad, area.y + halfH + pad * 0.5f,
            area.width - pad * 2f, halfH - pad * 1.5f);
        DrawGraphContent(bottomRect, bottomGraphIndex);
    }

    // NOTE: DrawStatusPanelFrame removed in v5.0.0 cleanup â€” each Tab* partial
    // handles its own scrollable panel layout inline for cleaner separation.

    // ========================================================================
    // HEIGHT HELPERS â€” Used by existing partials for scroll sizing
    // ========================================================================

    /// <summary>
    /// Total height needed for gauge content. Overridden by Gauges partial.
    /// </summary>
    partial void GetGaugeContentHeightPartial(ref float height);

    float GetGaugeContentHeight()
    {
        float h = 2000f;  // Safe default
        GetGaugeContentHeightPartial(ref h);
        return h;
    }

    /// <summary>
    /// Total height needed for status content. Overridden by Panels partial.
    /// </summary>
    partial void GetStatusContentHeightPartial(ref float height);

    float GetStatusContentHeight()
    {
        float h = 1800f;  // Safe default
        GetStatusContentHeightPartial(ref h);
        return h;
    }

    // ========================================================================
    // UTILITY â€” Shared helpers for all partials
    // ========================================================================

    /// <summary>
    /// Get a color blended between green/amber/red based on value vs thresholds.
    /// Used by gauges and panels for continuous color indication.
    /// </summary>
    public static Color GetThresholdColor(float value, float warnLow, float warnHigh,
        float alarmLow, float alarmHigh, Color normal, Color warning, Color alarm)
    {
        if (value < alarmLow || value > alarmHigh) return alarm;
        if (value < warnLow || value > warnHigh) return warning;
        return normal;
    }

    /// <summary>
    /// Get a color blended for a single-sided "low is bad" parameter.
    /// </summary>
    public static Color GetLowThresholdColor(float value, float warn, float alarm,
        Color normalC, Color warningC, Color alarmC)
    {
        if (value < alarm) return alarmC;
        if (value < warn) return warningC;
        return normalC;
    }

    /// <summary>
    /// Get a color blended for a single-sided "high is bad" parameter.
    /// </summary>
    public static Color GetHighThresholdColor(float value, float warn, float alarm,
        Color normalC, Color warningC, Color alarmC)
    {
        if (value > alarm) return alarmC;
        if (value > warn) return warningC;
        return normalC;
    }

    /// <summary>
    /// Format sim hours into HH:MM:SS string.
    /// </summary>
    public static string FormatHours(float hours)
    {
        return TimeAcceleration.FormatTime(hours);
    }
}

}

