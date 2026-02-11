// ============================================================================
// CRITICAL: Master the Atom - UI Component
// HeatupValidationVisual.cs - Heatup Validation Dashboard Core
// ============================================================================
//
// PURPOSE:
//   Top-level OnGUI coordinator for the PWR Cold Shutdown → HZP heatup
//   validation dashboard. Manages layout skeleton, engine binding, scroll
//   state, dashboard tab state, time acceleration controls, and partial
//   dispatch to per-tab rendering methods.
//   Contains no rendering logic beyond the layout frame — all visual
//   content is delegated to partial class files.
//
// READS FROM:
//   HeatupSimEngine — all public state fields, history buffers, event log
//
// REFERENCE:
//   Westinghouse 4-Loop PWR control room instrumentation layout
//   NRC HRTD Section 19 — Plant Operations monitoring requirements
//
// ARCHITECTURE:
//   This is the CORE partial. Companion partials (single responsibility):
//     - HeatupValidationVisual.Styles.cs             : Colors, fonts, GUIStyle
//     - HeatupValidationVisual.Gauges.cs              : Gauge arc panels
//     - HeatupValidationVisual.Panels.cs              : Status/info panels
//     - HeatupValidationVisual.Graphs.cs              : Trend graph rendering
//     - HeatupValidationVisual.Annunciators.cs        : Alarm tiles + event log
//     - HeatupValidationVisual.TabOverview.cs         : Tab 1 — Overview
//     - HeatupValidationVisual.TabPressurizer.cs      : Tab 2 — Pressurizer
//     - HeatupValidationVisual.TabCVCS.cs             : Tab 3 — CVCS / Inventory
//     - HeatupValidationVisual.TabSGRHR.cs            : Tab 4 — SG / RHR
//     - HeatupValidationVisual.TabRCPElectrical.cs    : Tab 5 — RCP / Electrical
//     - HeatupValidationVisual.TabEventLog.cs         : Tab 6 — Event Log
//     - HeatupValidationVisual.TabValidation.cs       : Tab 7 — Validation
//
//   v5.0.0 Layout (multi-tab, replacing v0.9.3 3-column):
//     ┌──────────────────────────────────────────────────────┐
//     │  HEADER BAR: Mode │ Phase │ Sim Time │ Time Accel    │
//     ├──────────────────────────────────────────────────────┤
//     │  TAB BAR: OVERVIEW│PZR│CVCS│SG/RHR│RCP│LOG│VALID    │
//     ├──────────────────────────────────────────────────────┤
//     │                                                      │
//     │  TAB CONTENT AREA                                    │
//     │  (layout varies per tab — see Tab* partials)         │
//     │                                                      │
//     └──────────────────────────────────────────────────────┘
//
// GOLD STANDARD: Yes
// v0.9.6 PERF FIX: OnGUI refresh throttle to respect refreshRate setting
// v5.0.0: Multi-tab dashboard redesign (Ctrl+1–7 tab switching)
// ============================================================================

using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Critical.Physics;

/// <summary>
/// Heatup Validation Dashboard — OnGUI coordinator.
/// Reads HeatupSimEngine public state. Contains no physics (G3).
/// All rendering delegated to partials (Styles, Gauges, Panels, Graphs, Annunciators, Tab*).
/// </summary>
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

    // ========================================================================
    // PUBLIC STATE — Toggling, visibility
    // ========================================================================

    [HideInInspector] public bool dashboardVisible = true;

    // ========================================================================
    // LAYOUT CONSTANTS
    // ========================================================================

    // Header height
    const float HEADER_FRAC = 0.06f;

    // v5.0.0: Dashboard tab bar height (px)
    const float TAB_BAR_H = 28f;

    // Minimum dimensions (px) for readability — used by tab layouts
    const float MIN_GAUGE_WIDTH = 180f;
    const float MIN_GRAPH_WIDTH = 400f;

    // ========================================================================
    // INTERNAL STATE — Dashboard tabs, graph tabs, scroll, timing
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
        "VALID"         // Tab 6: Validation / Debug
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

    // Cached screen dimensions for layout
    private float _sw, _sh;

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
        var kb = Keyboard.current;
        if (kb == null) return;

        // Toggle dashboard with F1
        if (kb.f1Key.wasPressedThisFrame)
            dashboardVisible = !dashboardVisible;

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
                int newIndex = (engine.currentSpeedIndex + 1) % 5;  // Wrap: 0→1→2→3→4→0
                engine.SetTimeAcceleration(newIndex);
            }
            
            // Decrement with - key
            if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame)
            {
                int newIndex = (engine.currentSpeedIndex - 1 + 5) % 5;  // Wrap: 4→3→2→1→0→4
                engine.SetTimeAcceleration(newIndex);
            }
        }
    }

    // ========================================================================
    // OnGUI — MAIN DISPATCH
    // v0.9.6 PERF FIX: Throttle to refreshRate (eliminates 94% of redraws)
    // v5.0.0: Multi-tab dispatch replaces 3-column layout
    // ========================================================================

    void OnGUI()
    {
        if (!dashboardVisible || engine == null) return;

        // v2.0.5: Actual implementation of OnGUI refresh throttle.
        // Was documented in v0.9.6 but never coded. Only redraws at refreshRate Hz.
        // Throttle on Layout events (which precede Repaint) — ensures we always
        // honor Repaint when Unity issues it, but skip redundant Layout passes.
        // Uses Time.unscaledTime so throttle works regardless of Time.timeScale.
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
        // v5.0.0: TAB CONTENT AREA — dispatched to per-tab partial methods
        // ================================================================
        float contentY = headerH + TAB_BAR_H;
        float contentH = _sh - contentY;
        Rect contentArea = new Rect(0, contentY, _sw, contentH);

        switch (_dashboardTab)
        {
            case 0: DrawOverviewTab(contentArea);        break;
            case 1: DrawPressurizerTab(contentArea);     break;
            case 2: DrawCVCSTab(contentArea);             break;
            case 3: DrawSGRHRTab(contentArea);            break;
            case 4: DrawRCPElectricalTab(contentArea);    break;
            case 5: DrawEventLogTab(contentArea);         break;
            case 6: DrawValidationTab(contentArea);       break;
        }
    }

    // ========================================================================
    // HEADER BAR — Plant mode, phase, times, time acceleration
    // ========================================================================

    void DrawHeaderBar(Rect area)
    {
        GUI.Box(area, GUIContent.none, _headerBgStyle);

        float pad = 6f;
        float x = pad;
        float y = area.y + pad;
        float h = area.height - pad * 2f;
        float cellH = h;

        // MODE indicator (colored)
        Color modeColor = engine.GetModeColor();
        string modeStr = engine.GetModeString().Replace("\n", " ");
        DrawHeaderCell(ref x, y, 140f, cellH, modeStr, modeColor);

        // Phase description
        string phase = string.IsNullOrEmpty(engine.heatupPhaseDesc) ? "INITIALIZING" : engine.heatupPhaseDesc;
        DrawHeaderCell(ref x, y, 260f, cellH, phase, _cNormalGreen);

        // Sim time
        string simStr = $"SIM: {FormatHours(engine.simTime)}";
        DrawHeaderCell(ref x, y, 140f, cellH, simStr, _cTextPrimary);

        // Wall time
        string wallStr = $"WALL: {FormatHours(engine.wallClockTime)}";
        DrawHeaderCell(ref x, y, 140f, cellH, wallStr, _cTextPrimary);

        // Time acceleration
        string speedLabel = TimeAcceleration.SpeedLabelsShort[engine.currentSpeedIndex];
        string accelStr = $"SPEED: {speedLabel}";
        Color accelColor = engine.isAccelerated ? _cWarningAmber : _cTextPrimary;
        DrawHeaderCell(ref x, y, 100f, cellH, accelStr, accelColor);

        // v5.0.0: Active tab indicator in header (right-aligned)
        string tabHint = $"[Ctrl+1-7] {_dashboardTabLabels[_dashboardTab]}";
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

    // ========================================================================
    // PARTIAL METHOD DECLARATIONS — Tab content (implemented in Tab* partials)
    // v5.0.0: Each tab has its own partial file
    // ========================================================================

    /// <summary>
    /// Tab 1 — Overview: key parameters, primary graphs, critical alarms.
    /// Implemented in HeatupValidationVisual.TabOverview.cs
    /// </summary>
    partial void DrawOverviewTab(Rect area);

    /// <summary>
    /// Tab 2 — Pressurizer: PZR gauges, heater control, bubble state, PZR graphs.
    /// Implemented in HeatupValidationVisual.TabPressurizer.cs
    /// </summary>
    partial void DrawPressurizerTab(Rect area);

    /// <summary>
    /// Tab 3 — CVCS / Inventory: charging/letdown, VCT/BRS, mass conservation.
    /// Implemented in HeatupValidationVisual.TabCVCS.cs
    /// </summary>
    partial void DrawCVCSTab(Rect area);

    /// <summary>
    /// Tab 4 — SG / RHR: steam generator thermal, RHR system, SG graphs.
    /// Implemented in HeatupValidationVisual.TabSGRHR.cs
    /// </summary>
    partial void DrawSGRHRTab(Rect area);

    /// <summary>
    /// Tab 5 — RCP / Electrical: pump status, HZP stabilization, RCP/HZP graphs.
    /// Implemented in HeatupValidationVisual.TabRCPElectrical.cs
    /// </summary>
    partial void DrawRCPElectricalTab(Rect area);

    /// <summary>
    /// Tab 6 — Event Log: annunciator tiles + full scrollable event log.
    /// Implemented in HeatupValidationVisual.TabEventLog.cs
    /// </summary>
    partial void DrawEventLogTab(Rect area);

    /// <summary>
    /// Tab 7 — Validation: RVLIS, inventory audit, PASS/FAIL checks, debug.
    /// Implemented in HeatupValidationVisual.TabValidation.cs
    /// </summary>
    partial void DrawValidationTab(Rect area);

    // ========================================================================
    // PARTIAL METHOD DECLARATIONS — Rendering components (existing partials)
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
    // LAYOUT HELPERS — Used by Tab* partials to build consistent layouts
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

    // NOTE: DrawStatusPanelFrame removed in v5.0.0 cleanup — each Tab* partial
    // handles its own scrollable panel layout inline for cleaner separation.

    // ========================================================================
    // HEIGHT HELPERS — Used by existing partials for scroll sizing
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
    // UTILITY — Shared helpers for all partials
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
